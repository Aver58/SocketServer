#region Copyright © 2018 Aver. All rights reserved.
/*
=====================================================
 AverFrameWork v1.0
 Filename:    Class1.cs
 Author:      Zeng Zhiwei
 Time:        2019/11/15 16:59:27
=====================================================
*/
#endregion

// State object for reading client data asynchronously
using System.Net.Sockets;
using System.Text;

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

    public RequestHandler reqhandler = new RequestHandler();
}
