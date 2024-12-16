#region Copyright © 2018 Aver. All rights reserved.
/*
=====================================================
 AverFrameWork v1.0
 Filename:    TeddyServer.cs
 Author:      Zeng Zhiwei
 Time:        2019/8/22 17:50:27
=====================================================
*/
#endregion

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class HttpServer
{
    private static HttpServer m_Instance;
    public static HttpServer Instance
    {
        get
        {
            if(m_Instance == null)
            {
                m_Instance = new HttpServer();
            }
            return m_Instance;
        }
    }

    //创建套接字 Create a TCP/IP socket.socket相当于一个插座
    private static Socket m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private static byte[] m_result = new byte[1024];
    public static ManualResetEvent manualResetEvent = new ManualResetEvent(false);

    public void StartServer(string ip = "127.0.0.1", int port = 8090)
    {
        Console.WriteLine("Listen on: " + ip + ":" + port.ToString());
        Console.WriteLine("Root: " + RequestHandler.WebRoot);
        Console.WriteLine("\nServer Started\n");

        IPAddress ipAddress = IPAddress.Parse(ip);//IPAddress.Loopback;
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        // 绑定socket到指定地址和端口号上、等待连接 
        // Bind the socket to the local endpoint and listen for incoming connections.
        try
        {
            m_socket.Bind(localEndPoint);
            m_socket.Listen(100);
            while (true)
            {
                // 将事件状态设置为非终止，从而导致线程受阻。
                // Set the event to nonsignaled state.
                manualResetEvent.Reset();

                // 开始一个异步socket，等待连接
                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("Waiting for a connection");
                m_socket.BeginAccept(new AsyncCallback(AcceptCallback),m_socket);

                // 阻止当前线程，直到当前 System.Threading.WaitHandle 收到信号。
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

    // 收到消息回调
    public static void AcceptCallback(IAsyncResult result)
    {
        // 将事件状态设置为有信号，从而允许一个或多个等待线程继续执行。
        // Signal the main thread to continue.
        manualResetEvent.Set();

        // 获取处理这个事件的对应handler。异步接受传入的连接尝试，并创建一个新 System.Net.Sockets.Socket 来处理远程主机通信。
        // Get the socket that handles the client request.
        Socket listener = (Socket)result.AsyncState;
        Socket handler = listener.EndAccept(result);

        // Create the state object.
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
    }

    public static void ReadCallback(IAsyncResult result)
    {
        String content = String.Empty;

        // Retrieve the state object and the handler socket from the asynchronous state object.
        StateObject state = (StateObject)result.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket. 
        int bytesRead = handler.EndReceive(result);

        if (bytesRead > 0)
        {
            RequestHandler reqHandler = state.reqHandler;
            reqHandler.PushRecived(state.buffer, bytesRead);
            //Console.WriteLine("Read {0} bytes from socket. ", bytesRead);
            if (reqHandler.IsAllRecived())
            {
                //Send(handler, reqh.GetResponseData());
                reqHandler.BeginResponse(new SocketAgent(handler));
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

