#region Copyright © 2018 Aver. All rights reserved.
/*
=====================================================
 AverFrameWork v1.0
 Filename:    SocketServer.cs
 Author:      Zeng Zhiwei
 Time:        2019/8/22 17:50:27
=====================================================
*/
#endregion

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

public class SocketServer
{
    private static SocketServer _instance;
    public static SocketServer Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new SocketServer();
            }
            return _instance;
        }
    }

    private Socket serverSocket;
    private static byte[] result = new byte[1024];

    public void InitSocket(string host, int port)
    {
        if(string.IsNullOrEmpty(host))
        {
            Console.WriteLine("【SocketServer.InitSocket】host is null");
            return;
        }
        IPEndPoint ipEndPoint = null;

        bool isMatch = IsRightIP(host);
        if(isMatch)
        {
            ipEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
        }
        else
        {
            ipEndPoint = new IPEndPoint(IPAddress.Parse(GetIPAddress()), port);
        }

        serverSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            serverSocket.Bind(ipEndPoint);//绑定IP和端口          
            serverSocket.Listen(1000);//设置监听数量  
            Console.WriteLine("启动监听{0}成功", serverSocket.LocalEndPoint.ToString());
            //InitTreadAndQueue();
            //在新线程中监听客户端的连接
            Thread thread = new Thread(ClientConnectListen);
            thread.Start();
            Console.ReadLine();
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    /// <summary>
    /// 客户端连接请求监听
    /// </summary>
    private void ClientConnectListen()
    {
        while(true)
        {
            //为新的客户端连接创建一个Socket对象
            Socket clientSocket = serverSocket.Accept();
            Console.WriteLine("客户端{0}成功连接", clientSocket.RemoteEndPoint.ToString());
            //向连接的客户端发送连接成功的数据
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteString("Connected Server");
            clientSocket.Send(WriteMessage(buffer.ToBytes()));
            //每个客户端连接创建一个线程来接受该客户端发送的消息
            Thread thread = new Thread(RecieveMessage);
            thread.Start(clientSocket);
        }
    }


    /// <summary>
    /// 数据转换，网络发送需要两部分数据，一是数据长度，二是主体数据
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private static byte[] WriteMessage(byte[] message)
    {
        MemoryStream ms = null;
        using(ms = new MemoryStream())
        {
            ms.Position = 0;
            BinaryWriter writer = new BinaryWriter(ms);
            ushort msglen = (ushort)message.Length;
            writer.Write(msglen);
            writer.Write(message);
            writer.Flush();
            return ms.ToArray();
        }
    }

    /// <summary>
    /// 接收指定客户端Socket的消息
    /// </summary>
    /// <param name="clientSocket"></param>
    private static void RecieveMessage(object clientSocket)
    {
      
        Socket mClientSocket = (Socket)clientSocket;
        while(true)
        {
            try
            {
                int receiveNumber = mClientSocket.Receive(result);
                Console.WriteLine("接收客户端{0}消息， 长度为{1}", mClientSocket.RemoteEndPoint.ToString(), receiveNumber);
                ByteBuffer buff = new ByteBuffer(result);
                //数据长度
                int len = buff.ReadShort();
                //数据内容
                string data = buff.ReadString();
                Console.WriteLine("数据内容：{0}", data);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                mClientSocket.Shutdown(SocketShutdown.Both);
                mClientSocket.Close();
                break;
            }
        }
    }

    /// <summary>
    /// 获取ip地址
    /// </summary>
    /// <returns></returns>
    private static string GetIPAddress()
    {
        string strLocalIP = "";
        string strPcName = Dns.GetHostName();
        IPHostEntry ipEntry = Dns.GetHostEntry(strPcName);
        foreach(IPAddress address in ipEntry.AddressList)
        {
            if(IsRightIP(address.ToString()))
            {
                strLocalIP = address.ToString();
                return strLocalIP;
            }
        }
        return null;
    }

    /// <summary>
    /// 判断是否为正确的IP地址
    /// </summary>
    /// <param name="strIPadd">需要判断的字符串</param>
    /// <returns>true = 是 false = 否</returns>
    private static bool IsRightIP(string strIPadd)
    {
        //利用正则表达式判断字符串是否符合IPv4格式
        if(Regex.IsMatch(strIPadd, "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}"))
        {
            string[] ips = strIPadd.Split('.');
            if(ips.Length == 4 || ips.Length == 6)
            {
                if(Int32.Parse(ips[0]) < 256 && Int32.Parse(ips[1]) < 256 & Int32.Parse(ips[2]) < 256 & Int32.Parse(ips[3]) < 256)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        else
            return false;
    }

}

