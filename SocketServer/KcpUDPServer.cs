#region Copyright © 2020 Aver. All rights reserved.
/*
=====================================================
 AverFrameWork v1.0
 Filename:    KcpUDPServer.cs
 Author:      Zeng Zhiwei
 Time:        2020\12\22 星期二 22:25:01
=====================================================
*/
#endregion

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class KcpUDPServer :Singleton<KcpUDPServer>
{
    private static Socket m_server;
    private Thread m_thread;
    private object m_lockUdp = new object();
    private Dictionary<Socket, ClientInfo> m_clientPool = new Dictionary<Socket, ClientInfo>();


    public void StartUdpServer(string ip = "127.0.0.1", int port = 8090)
    {
        m_server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //绑定端口号和IP
        m_server.Bind(new IPEndPoint(IPAddress.Parse(ip), port));

        Console.WriteLine("UDP Server Start Listen on: " + ip + ":" + port.ToString());
        //开始接受客户端连接请求
        m_server.BeginAccept(new AsyncCallback(OnOneClientAccepted), m_server);

        m_thread = new Thread(ThreadProcess);
        //Thread t = new Thread(ReciveMsg);//开启接收消息线程
        //t.Start();
        //Thread t2 = new Thread(SendMsg);//开启发送消息线程
        //t2.Start();
    }

    private void ThreadProcess()
    {
        Console.WriteLine("kcp线程启动");
        while(true)
        {
            Thread.Sleep(10);
            //if(State == ConnectState.Disconnected)
            //{
            //    Debug.Log("检测到网络断开，线程挂起");
            //    m_threadResetEvent.WaitOne();
            //    Debug.Log("线程恢复");
            //    return;
            //}
            // 处理接收UDP包 收到的包传入Kcp进行处理
            //DealReceiveUdpData();

            //// 处理UDP发送
            //DealSendBuffToUdp();

            //// kcp处理
            //if(State == ConnectState.Connected)
            //{
            //    // 处理接收KCP包
            //    DealKcpUpdateAndReceive();
            //}
        }
    }

    public void OnOneClientAccepted(IAsyncResult result)
    {
        Socket server = result.AsyncState as Socket;
        //这就是客户端的Socket实例，我们后续可以将其保存起来
        Socket client = server.EndAccept(result);

        try
        {
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
        }
        catch(Exception ex)
        {
            Console.WriteLine("Error :\r\n\t" + ex.ToString());
        }
    }

    static void SendOneMsg(Socket clientSocket, byte[] buffer)
    {
        if(!clientSocket.IsConnected())
            return;
        clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None,null,null);
    }

    //接收客户端的消息
    public void OnReceiveMessage(IAsyncResult result)
    {
        Socket client = result.AsyncState as Socket;
        //Console.WriteLine($"{client.GetHost()} // {client.GetRemoteHost()}");

        if(client == null || !m_clientPool.ContainsKey(client))
            return;

        try
        {
            var length = client.EndReceive(result);
            if(length <= 0)
                return;

            byte[] buffer = m_clientPool[client].buffer;

            HandleMessage(client, buffer);

            //接收下一个消息(因为这是一个递归的调用，所以这样就可以一直接收消息了）
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceiveMessage), client);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            client.Disconnect(true);
            Console.WriteLine("One Client {0} disconnet", m_clientPool[client].Name);
            m_clientPool.Remove(client);
        }
    }

    private void HandleMessage(Socket client, byte[] buffer)
    {
        NetMsgData data = ProtoBufUtil.UnpackNetMsg(buffer);
        var protoID = data.ID;
        Console.WriteLine("[Receive]{0} | {1} | {2} | {3}", client.RemoteEndPoint, data.ID, data.Data, DateTime.Now);
        switch(protoID)
        {
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

    /// <summary>
    /// 向特定ip的主机的端口发送数据报
    /// </summary>
    static void SendMsg()
    {
        EndPoint client = new IPEndPoint(IPAddress.Any, 0);
        while(true)
        {
            string msg = Console.ReadLine();
            m_server.SendTo(Encoding.UTF8.GetBytes(msg), client);
        }
    }

    /// <summary>
    /// 接收发送给本机ip对应端口号的数据报
    /// </summary>
    static void ReciveMsg()
    {
        while(true)
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);//用来保存发送方的ip和端口号
            byte[] buffer = new byte[1024];
            int length = m_server.ReceiveFrom(buffer, ref client);//接收数据报
            string message = Encoding.UTF8.GetString(buffer, 0, length);
            Console.WriteLine(client.ToString() + message);
        }
    }
}