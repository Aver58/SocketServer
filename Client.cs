using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// 用来与客户端通信
    /// </summary>
    class Client
    {
        Socket serverSocket;            //接收到连接上的客户端
        string message = "";            //消息
        byte[] data = new byte[1024];   //消息BYTE
        Thread t;                       //线程，用来无限接收信息

        public Client(Socket s)
        {
            serverSocket = s;
            t = new Thread(ReceiveMessage);
            t.Start();
        }

        /// <summary>
        /// 收
        /// </summary>
        /// <param name="soc"></param>
        void ReceiveMessage()
        {
            while (true)//响应时间，读取
            {
                if (serverSocket.Poll(10, SelectMode.SelectRead))//如果客户端断开，就出循环
                {
                    Console.WriteLine(serverSocket.GetType().Name + "失去连接");
                    serverSocket.Close();
                    break;
                }
                int length = serverSocket.Receive(data);
                message = Encoding.UTF8.GetString(data, 0, length);

                Console.WriteLine("收到消息" + message);
                //if (listBox1.InvokeRequired)//线程访问问题
                //{
                //    listBox1.Invoke(new Action<string>(s => { listBox1.Items.Add("收到消息" + s); }), message);
                //    Console.WriteLine("收到消息" + message);
                //}
                //todo分发给客户端

                //Program.BroadcastMessage(message);
            }
        }

        public void SendMessage(string s)
        {
            byte[] datas = Encoding.UTF8.GetBytes(s);
            serverSocket.Send(datas);
        }

        //返回客户端连接状态
        public bool Connected()
        {
            return serverSocket.Connected;
        }
    }
}
