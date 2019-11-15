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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

public class SocketServer
{
    private static SocketServer m_Instance;
    public static SocketServer Instance
    {
        get
        {
            if(m_Instance == null)
            {
                m_Instance = new SocketServer();
            }
            return m_Instance;
        }
    }

    //创建套接字   // Create a TCP/IP socket.
    private static Socket m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private static byte[] m_result = new byte[1024];
    public static ManualResetEvent manualResetEvent = new ManualResetEvent(false);

    public void InitSocket(string listen_ip = "127.0.0.1", int port = 8090)
    {
        //Console.WriteLine(string.Format("服务端已启动{0}:{1}，等待连接",host,port));
        //m_socket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
        //m_socket.Listen(100);//设定最多100个排队连接请求   
        //Thread myThread = new Thread(ListenClientConnect);//通过多线程监听客户端连接  
        //myThread.Start();

        IPAddress ipAddress = IPAddress.Parse(listen_ip);//IPAddress.Loopback;//Parse("127.0.0.1");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        // Bind the socket to the local endpoint and listen for incoming connections.
        try
        {
            m_socket.Bind(localEndPoint);
            m_socket.Listen(100);
            while (true)
            {
                // Set the event to nonsignaled state.
                manualResetEvent.Reset();

                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("Waiting for a connection");
                m_socket.BeginAccept(new AsyncCallback(AcceptCallback),m_socket);

                // Wait until a connection is made before continuing.
                manualResetEvent.WaitOne();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\nPress ENTER to continue");
        Console.Read();
    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.
        manualResetEvent.Set();

        // Get the socket that handles the client request.
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
    }

    public static void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;

        // Retrieve the state object and the handler socket
        // from the asynchronous state object.
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket. 
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            RequestHandler reqh = state.reqhandler;
            reqh.PushRecived(state.buffer, bytesRead);
            //Console.WriteLine("Read {0} bytes from socket. ", bytesRead);
            if (reqh.IsAllRecived())
            {
                //Send(handler, reqh.GetResponseData());
                reqh.BeginResponse(new SocketAgent(handler));
            }
            else
            {
                // Not all data received. Get more.
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
        }
    }

    #region Send
    private static void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.
        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    private static void Send(Socket handler, byte[] data)
    {
        // Begin sending the data to the remote device.
        handler.BeginSend(data, 0, data.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = handler.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }

        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
    #endregion

    /// <summary>  
    /// 监听客户端连接  
    /// </summary>  
    private static void ListenClientConnect()
    {
        while (true)
        {
            Socket clientSocket = m_socket.Accept();
            clientSocket.Send(Encoding.UTF8.GetBytes("服务器连接成功"));
            Thread receiveThread = new Thread(ReceiveMessage);
            receiveThread.Start(clientSocket);
        }
    }

    /// <summary>  
    /// 接收消息  
    /// </summary>  
    /// <param name="clientSocket"></param>  
    private static void ReceiveMessage(object clientSocket)
    {
        Socket myClientSocket = (Socket)clientSocket;
        while (true)
        {
            try
            {
                //通过clientSocket接收数据  
                int receiveNumber = myClientSocket.Receive(m_result);
                if (receiveNumber == 0)
                    return;
                Console.WriteLine("接收客户端{0} 的消息：{1}", myClientSocket.RemoteEndPoint.ToString(), Encoding.UTF8.GetString(m_result, 0, receiveNumber));
                //给Client端返回信息
                string sendStr = "已成功接到您发送的消息";
                byte[] bs = Encoding.UTF8.GetBytes(sendStr);    //Encoding.UTF8.GetBytes()不然中文会乱码
                myClientSocket.Send(bs, bs.Length, 0);          //返回信息给客户端
                myClientSocket.Close();                         //发送完数据关闭Socket并释放资源
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                myClientSocket.Shutdown(SocketShutdown.Both);   //禁止发送和上传
                myClientSocket.Close();                         //关闭Socket并释放资源
                break;
            }
        }
    }

}

