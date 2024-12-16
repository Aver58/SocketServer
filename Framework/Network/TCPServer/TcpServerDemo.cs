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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Server.Framework.Network;


public class TcpServerDemo : Singleton<TcpServerDemo> {
    private Socket server;
    private Dictionary<Socket, ClientInfo> m_clientPool = new Dictionary<Socket, ClientInfo>();
    private List<NetMsgData> m_msgPool = new List<NetMsgData>();

    public void StartTcpServer(string ip = "127.0.0.1", int port = 8090) {
        //创建一个新的Socket,这里我们使用最常用的基于TCP的Stream Socket（流式套接字）
        server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        server.Bind(new IPEndPoint(IPAddress.Parse(ip), port));

        //启动监听，并且设置一个最大的队列长度
        server.Listen(100);

        //开始接受客户端连接请求
        server.BeginAccept(new AsyncCallback(OnOneClientAccepted), server);

        //Run(ip, port);

        Console.WriteLine("TCP Server Start Listen on: " + ip + ":" + port.ToString());
        Console.Read();
    }

    private void SendOneMsg(Socket clientSocket, byte[] buffer) {
        if (!clientSocket.IsConnected())
            return;
        clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null);
    }

    // 广播
    private void Broadcast() {
        Thread broadcast = new Thread(() => {
            while (true) {
                if (m_msgPool.Count > 0) {
                    byte[] msg = ProtoBufUtil.PackNetMsg(m_msgPool[0]);
                    foreach (KeyValuePair<Socket, ClientInfo> cs in m_clientPool) {
                        // client客户端socket对象
                        Socket client = cs.Key;
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

    // 收到一个客户端请求
    public void OnOneClientAccepted(IAsyncResult result) {
        Socket server = result.AsyncState as Socket;
        //这就是客户端的Socket实例，我们后续可以将其保存起来
        Socket client = server.EndAccept(result);

        try {
            //接受多个客户端 ，准备接受下一个客户端请求
            server.BeginAccept(new AsyncCallback(OnOneClientAccepted), server);

            byte[] buffer = new byte[1024];

            //给客户端发送一个欢迎消息
            SendOneMsg(client, ProtoBufUtil.PackNetMsg(new NetMsgData(0, "[Server]Hi there, I accept you request at " + DateTime.Now.ToString())));
            ClientInfo info = new ClientInfo();
            info.Id = client.RemoteEndPoint;
            info.handle = client.Handle;
            info.buffer = buffer;
            m_clientPool.Add(client, info);

            Console.WriteLine(string.Format("One Client {0} connected!!", client.RemoteEndPoint));

            //HeartBeat(client);

            //接收客户端的消息(这个和在客户端实现的方式是一样的）
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceiveMessage), client);
        } catch (Exception ex) {
            Console.WriteLine("Error :\r\n\t" + ex.ToString());
        }
    }

    //接收客户端的消息
    public void OnReceiveMessage(IAsyncResult result) {
        Socket client = result.AsyncState as Socket;
        //Console.WriteLine($"{client.GetHost()} // {client.GetRemoteHost()}");

        if (client == null || !m_clientPool.ContainsKey(client))
            return;

        try {
            var length = client.EndReceive(result);
            if (length <= 0)
                return;

            byte[] buffer = m_clientPool[client].buffer;

            HandleMessage(client, buffer);

            //接收下一个消息(因为这是一个递归的调用，所以这样就可以一直接收消息了）
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceiveMessage), client);
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            client.Disconnect(true);
            Console.WriteLine("One Client {0} disconnet", m_clientPool[client].Name);
            m_clientPool.Remove(client);
        }
    }

    private void HandleMessage(Socket client, byte[] buffer) {
        NetMsgData data = ProtoBufUtil.UnpackNetMsg(buffer);
        var protoID = data.ID;
        Console.WriteLine("[Receive]{0} | {1} | {2} | {3}", client.RemoteEndPoint, data.ID, data.Data, DateTime.Now);
        switch (protoID) {
            case (OpCode.C2S_TestRequest):
                data.ID = OpCode.S2C_TestResponse;
                data.Data = "Test OK!!";
                SendOneMsg(client, ProtoBufUtil.PackNetMsg(data));
                Console.WriteLine($"[Send]ID:{data.ID},Data:{data.Data}");
                break;

            default:
                break;
        }
        //m_msgPool.Add(data);
    }

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
                    SendOneMsg(client, ProtoBufUtil.PackNetMsg(new NetMsgData(0, "[Server]HeartBeat:" + DateTime.Now.ToString())));

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

public class ClientInfo {
    public byte[] buffer;
    public string NickName { get; set; }
    public EndPoint Id { get; set; }
    public IntPtr handle { get; set; }

    public string Name {
        get {
            if (!string.IsNullOrEmpty(NickName)) {
                return NickName;
            } else {
                return string.Format("{0}#{1}", Id, handle);
            }
        }
    }
}