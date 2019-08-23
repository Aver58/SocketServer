#region Copyright © 2018 Aver. All rights reserved.
/*
=====================================================
 AverFrameWork v1.0
 Filename:    AsyncSocketListener.cs
 Author:      Zeng Zhiwei
 Time:        2019/8/22 20:39:25
=====================================================
*/
#endregion

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class AsyncSocketListener
{
    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024 * 64;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();

        //public RequestHandler reqhandler = new RequestHandler();
    }

    //可以通过设置信号来让线程停下来或让线程重新启动
    public static ManualResetEvent allDone = new ManualResetEvent(false);

    public static void StartListening(string listen_ip = "127.0.0.1", int port = 8080)
    {
        IPAddress ipAddress = IPAddress.Parse(listen_ip);
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
        // Create a TCP/IP socket.
        Socket listener = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(100);
            while (true)
            {
                // Set the event to nonsignaled state.
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("Waiting for a connection");

                listener.BeginAccept(new AsyncCallback(AcceptCallback),listener);

                // Wait until a connection is made before continuing.
                allDone.WaitOne();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\nPress ENTER to continue");
        Console.Read();
    }

    /// <summary>
    /// 成功连接回调
    /// </summary>
    /// <param name="result"></param>
    public static void AcceptCallback(IAsyncResult result)
    {
        // 将事件状态设置为有信号，从而允许一个或多个等待线程继续执行。
        allDone.Set();

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

        // Retrieve the state object and the handler socket
        // from the asynchronous state object.
        StateObject state = (StateObject)result.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket. 
        int bytesRead = handler.EndReceive(result);

        //if(bytesRead > 0)
        //{
        //    RequestHandler reqh = state.reqhandler;
        //    reqh.PushRecived(state.buffer, bytesRead);
        //    //Console.WriteLine("Read {0} bytes from socket. ", bytesRead);
        //    if(reqh.IsAllRecived())
        //    {
        //        //Send(handler, reqh.GetResponseData());
        //        reqh.BeginResponse(new SocketAgent(handler));
        //    }
        //    else
        //    {
        //        // Not all data received. Get more.
        //        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        //    }
        //}
    }

}

