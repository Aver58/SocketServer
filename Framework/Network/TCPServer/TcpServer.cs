#region Copyright © 2018 Aver. All rights reserved.

/*
=====================================================
 AverFrameWork v1.0
 Filename:    TeddyServer.cs
 Author:      Zeng Zhiwei
 Time:        2019/12/8 16:21:40
=====================================================
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Protocol;
using TeddyServer.Framework.Utility;


// TCP服务器
// 会话管理
// 心跳检测
// 多线程处理
// 断线处理
// (单播)服务器发送消息给客户端
// (广播)服务器广播消息给所有客户端
public class TcpServer {
    private Socket server;
    private List<NetMsgData> m_msgPool = new List<NetMsgData>();
    private Dictionary<int, Session> sessionMap = new Dictionary<int, Session>();

    public void Start(string ip = "127.0.0.1", int port = 8090) {
        server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        server.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
        //启动监听，并且设置一个最大的队列长度
        server.Listen(100);
        server.BeginAccept(OnOneClientAccepted, server);
        Logger.Log("TCP Server Start Listen on: " + ip + ":" + port.ToString());
        Console.Read();
    }

    public void Stop() {
        foreach (var session in sessionMap) {
            session.Value.socket.Close();
        }

        if (server != null) {
            server.Close();
            server = null;
        }
    }

    // 收到一个客户端请求
    private void OnOneClientAccepted(IAsyncResult result) {
        //这就是客户端的Socket实例，我们后续可以将其保存起来
        Socket client = server.EndAccept(result);

        try {
            //接受多个客户端 ，准备接受下一个客户端请求
            server.BeginAccept(OnOneClientAccepted, server);
            byte[] buffer = new byte[1024];
            //给客户端发送一个欢迎消息

            SendOneMsg(client, ProtoBufUtil.PackNetMsg(new NetMsgData() {
               ID = OpCode.S2C_TestResponse,
               Data = "[Server]Hi there, I accept you request at " + DateTime.Now
            }));


            Session info = new Session();
            var id = client.RemoteEndPoint.GetHashCode();
            info.id = id;
            info.buffer = buffer;
            info.socket = client;;
            sessionMap.Add(client.RemoteEndPoint.GetHashCode(), info);
            Logger.Log($"One Client {client.RemoteEndPoint} connected!!");
            //HeartBeat(client);
            //接收客户端的消息(这个和在客户端实现的方式是一样的）
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceiveMessage), client);
        } catch (Exception ex) {
            Logger.LogError(ex.ToString());
        }
    }

    //接收客户端的消息
    public void OnReceiveMessage(IAsyncResult result) {
        Socket client = result.AsyncState as Socket;
        if (client == null) {
            return;
        }

        var id = client.RemoteEndPoint.GetHashCode();
        if (!sessionMap.ContainsKey(id))
            return;

        try {
            var length = client.EndReceive(result);
            if (length <= 0)
                return;

            var buffer = sessionMap[id].buffer;
            HandleMessage(client, buffer);
            // 接收下一个消息(递归调用，一直接收消息了）
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceiveMessage), client);
        } catch (Exception ex) {
            Logger.LogError(ex.Message);
            client.Disconnect(true);
            var id1 = client.RemoteEndPoint.GetHashCode();
            Logger.Log($"One Client {id1} disconnect");
            sessionMap.Remove(id1);
        }
    }

    private void HandleMessage(Socket client, byte[] buffer) {
        NetMsgData data = ProtoBufUtil.UnpackNetMsg(buffer);
        var protoId = data.ID;
        Logger.Log($"[Receive]{client.RemoteEndPoint} protoId {data.ID} Data {data.Data} time {DateTime.Now}");
        switch (protoId) {
            case OpCode.C2S_TestRequest:
                data.ID = OpCode.S2C_TestResponse;
                data.Data = "Test OK!!";
                SendOneMsg(client, ProtoBufUtil.PackNetMsg(data));
                break;

            default:
                break;
        }
        m_msgPool.Add(data);
    }

    private void SendOneMsg(Socket client, byte[] buffer) {
        if (!client.IsConnected())
            return;
        client.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null);
        Logger.Log($"[Send]ID:{client.RemoteEndPoint},Length:{buffer.Length}");
    }

    // 广播
    private void Broadcast() {
        Thread broadcast = new Thread(() => {
            while (true) {
                if (m_msgPool.Count > 0) {
                    byte[] msg = ProtoBufUtil.PackNetMsg(m_msgPool[0]);
                    foreach (var cs in sessionMap) {
                        var session = cs.Value;
                        var client = session.socket;
                        if (client.Connected) {
                            client.Send(msg, msg.Length, SocketFlags.None);
                        }
                    }

                    m_msgPool.RemoveAt(0);
                }
            }
        });

        broadcast.Start();
    }

    // 多线程
    public void Run(string ip = "127.0.0.1", int port = 8090) {
        Thread serverSocketThraed = new Thread(() => {
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            server.Listen(10);
            server.BeginAccept(new AsyncCallback(OnOneClientAccepted), server);
        });

        serverSocketThraed.Start();
        //Broadcast();
    }

    // 心跳检测
    public void HeartBeat(Socket client) {
        //实现每隔两秒钟给服务器发一个消息
        //这里我们使用了一个定时器
        var timer = new System.Timers.Timer();
        timer.Interval = 2000D;
        timer.Enabled = true;
        timer.Elapsed += (o, a) => {
            //检测客户端的活动状态
            if (client.Connected) {
                try {
                    SendOneMsg(client, ProtoBufUtil.PackNetMsg(new NetMsgData() {
                        ID = 0,
                        Data = "[Server]Hi there, I accept you request at " + DateTime.Now
                    }));

                    Console.WriteLine("[Server]HeartBeat:" + client.GetHost() + DateTime.Now.ToString());
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            } else {
                timer.Stop();
                timer.Enabled = false;
                Console.WriteLine("Client is disconnected, the timer is stop.");
            }
        };
        timer.Start();
    }
}

public class Session {
    public int id;
    public Socket socket;
    public byte[] buffer;
}