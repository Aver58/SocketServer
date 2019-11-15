using System;
using System.Collections.Generic;
using System.Net.Sockets;

public interface IServerScript
{
    byte[] Execute(byte[] pagedata, RequestDataContext DataContext);
}

public interface IResponse
{
    void SetHeader(string h, string val);
    string GetHeader(string h);
    string GetHeaders();
    byte[] GetContent();
    byte[] GetResponseBytes();
    void Close();
}

public class SocketAgent
{
    private Socket socket = null;

    public SocketAgent(Socket s)
    {
        socket = s;
    }

    public void SendData(byte[] data, Action<int> sendcallback = null)
    {
        if (socket != null)
        {
            try
            {
                socket.BeginSend(data, 0, data.Length, 0,
                                new AsyncCallback(SendCallback), sendcallback);
            }
            catch (Exception) { }
        }
    }

    public void Close()
    {
        if (socket != null)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;
            }
            catch (Exception)
            {
            }
        }
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            Action<int> callback = (Action<int>)ar.AsyncState;

            int bytesSent = socket.EndSend(ar);
            if (callback != null)
                callback.Invoke(bytesSent);
            //Console.WriteLine("Sent {0} bytes to client.", bytesSent);
            //handler.Shutdown(SocketShutdown.Both);
            //handler.Close();
        }

        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}

public class RequestDataContext
{
    public Dictionary<string, string> GET = new Dictionary<string, string>();
    public Dictionary<string, string> POST = new Dictionary<string, string>();
    public SocketAgent Connect = null;
    public string RequestFile = string.Empty;
    public IResponse Response = null;
}
