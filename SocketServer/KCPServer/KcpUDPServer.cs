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
using KcpProject;

//相对tcp的强链接，udp是弱连接，客户端用udp给服务器发消息的时候，不需要和tcp那样先建立连接（connect）再发送，
//而是直接向服务器地址发送消息即可。
//类似我们发短信，知道对方手机号即可直接发送，而不用关心对方是否开机等信息。
//一种是用Socket对象，另一种是用UdpClient对象。
public class ClientMessage
{
    /// <summary>
    /// 每个客户端的唯一id，即ip地址+端口号
    /// </summary>
    public string clientId;
    /// <summary>
    /// 客户端地址信息
    /// </summary>
    public IPEndPoint clientIPEndPoint;
    /// <summary>
    /// 该客户端发送给服务器的最新消息
    /// </summary>
    public string recieveMessage;

    public ClientMessage(string id, IPEndPoint point, string msg)
    {
        clientId = id;
        clientIPEndPoint = new IPEndPoint(point.Address, point.Port);
        recieveMessage = msg;
    }
}

public class KcpUDPServer :Singleton<KcpUDPServer>
{
    UdpClient m_udpClient;
    IPEndPoint m_clientIpEndPoint;//存放客户端地址
    Thread m_connectThread;//接收客户端消息的线程
    byte[] m_result = new byte[1024];//存放接收到的消息
    int m_sendCount;//发送次数
    private UDPSession m_udpSession;
    byte[] buffer = new byte[1500];
    int recvBytes = 0;
    int sendBytes = 0;

    Dictionary<string, ClientMessage> m_clientMessageDic;//存放客户端信息，key->ip+port

    //初始化
    public void Start()
    {
        IPEndPoint ipEnd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8090);
        m_udpClient = new UdpClient(ipEnd);
        m_clientMessageDic = new Dictionary<string, ClientMessage>();
        m_sendCount = 0;

        //定义客户端
        m_clientIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        Console.WriteLine("UDP 服务器夯起来，等待连接数据，ip：127.0.0.1，port：8090");
        //开启一个线程连接
        m_connectThread = new Thread(new ThreadStart(ReceiveUDP));
        m_connectThread.Start();

        //定时器
        //System.Timers.Timer t = new System.Timers.Timer(3000);//实例化Timer类，设置间隔时间为3000毫秒
        //t.Elapsed += new System.Timers.ElapsedEventHandler(SendToClient);//到达时间的时候执行事件
        //t.AutoReset = true;//设置是执行一次（false）还是一直执行(true)
        //t.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件
    }

    public void StartWithSocket()
    {
        m_udpSession = new UDPSession();
        m_udpSession.AckNoDelay = true;
        m_udpSession.WriteDelay = false;

        m_udpSession.Connect("127.0.0.1", 8090);
        Console.WriteLine("KCP 服务器夯起来，等待连接数据，ip：127.0.0.1，port：8090");

        //开启一个线程连接
        m_connectThread = new Thread(new ThreadStart(Receive));
        m_connectThread.Start();
    }

    private void Receive()
    {
        while(true)
        {
            m_udpSession.Update();

            //if(!stopSend)
            //{
            //    Debug.Log("Write Message...");
            //    var send = connection.Send(buffer, 0, buffer.Length);
            //    if(send < 0)
            //    {
            //        Debug.Log("Write message failed.");
            //        break;
            //    }

            //    if(send > 0)
            //    {
            //        counter++;
            //        sendBytes += buffer.Length;
            //        if(counter >= 500)
            //            stopSend = true;
            //    }
            //}

            var n = m_udpSession.Recv(buffer, 0, buffer.Length);
            if(n == 0)
            {
                Thread.Sleep(10);
                continue;
            }
            else if(n < 0)
            {
                Debug.Log("Receive Message failed.");
                continue;
            }
            else
            {
                recvBytes += n;
                Debug.Log($"{recvBytes} / {sendBytes}");
            }
        }
    }

    public void SendToClient(object source, System.Timers.ElapsedEventArgs e)
    {
        Send(GetAllClientMessage());
    }

    public void Send(string data)
    {
        if(m_clientMessageDic == null || m_clientMessageDic.Count == 0)
            return;

        //try
        //{
        //    NetBufferWriter writer = new NetBufferWriter();
        //    writer.WriteString(data);
        //    byte[] msg = writer.Finish();
        //    foreach(var point in m_clientMessageDic)
        //    {
        //        Console.WriteLine("send to  " + point.Key + "  " + data);
        //        m_udpClient.Send(msg, writer.finishLength, point.Value.clientIPEndPoint);
        //    }
        //    m_sendCount++;
        //}
        //catch(Exception ex)
        //{
        //    Console.WriteLine("send error   " + ex.Message);
        //}
    }

    //服务器接收
    void ReceiveUDP()
    {
        while(true)
        {
            try
            {
                m_result = new byte[1024];
                m_result = m_udpClient.Receive(ref m_clientIpEndPoint);
                string clientId = string.Format("{0}:{1}", m_clientIpEndPoint.Address, m_clientIpEndPoint.Port);

                HandleMessage(m_clientIpEndPoint, m_result);
            }
            catch(Exception ex)
            {
                Console.WriteLine("receive error   " + ex.Message);
            }
        }
    }

    void HandleMessage(IPEndPoint client, byte[] buffer)
    {
        NetMsgData data = ProtoBufUtil.UnpackNetMsg(buffer);
        var protoID = data.ID;
        Console.WriteLine("[Receive]{0} | {1} | {2} | {3}", client, data.ID, data.Data, DateTime.Now);

        switch(protoID)
        {
            case (OpCode.C2S_TestRequest):
                data.ID = OpCode.S2C_TestResponse;
                data.Data = "Test OK!!";
                //SendOneMsg(client, ProtoBufUtil.PackNetMsg(data));
                Console.WriteLine($"[Send]ID:{data.ID},Data:{data.Data}");
                break;

            default:
                break;
        }
    }

    private void SendOneMsg(IPEndPoint endPoint, byte[] data)
    {
        m_udpClient.Send(data, data.Length, endPoint);
    }

    //连接关闭
    void Close()
    {
        //关闭线程
        if(m_connectThread != null)
        {
            m_connectThread.Interrupt();
            //Thread abort is not supported on this platform.
            //m_connectThread.Abort();
            m_connectThread = null;
        }

        m_clientMessageDic.Clear();

        if(m_udpClient != null)
        {
            m_udpClient.Close();
            m_udpClient.Dispose();
        }
        Console.WriteLine("断开连接");
    }

    void AddNewClient(string id, ClientMessage msg)
    {
        if(m_clientMessageDic != null && !m_clientMessageDic.ContainsKey(id))
        {
            m_clientMessageDic.Add(id, msg);
        }
    }

    void RemoveClient(string id)
    {
        if(m_clientMessageDic != null && m_clientMessageDic.ContainsKey(id))
        {
            m_clientMessageDic.Remove(id);
        }
    }

    string GetAllClientMessage()
    {
        string allMsg = "m_sendCount    " + m_sendCount + "\n";
        foreach(var msg in m_clientMessageDic)
        {
            allMsg += (msg.Value.clientId + "->" + msg.Value.recieveMessage + "\n");
        }
        return allMsg;
    }
}