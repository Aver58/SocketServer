#region Copyright © 2018 Aver. All rights reserved.
/*
=====================================================
 AverFrameWork v1.0
 Filename:    Response.cs
 Author:      Zeng Zhiwei
 Time:        2019/12/8 15:29:33
=====================================================
*/
#endregion


using System.Collections.Generic;
using System.Text;

public class Response : IResponse
{
    public string version = "HTTP/1.1";
    public string state = "200";
    public string state_description = "OK";
    public string strdata
    {
        get
        {
            return Encoding.UTF8.GetString(data);
        }
        set
        {
            data = Encoding.UTF8.GetBytes(value);
        }
    }
    public byte[] data = new byte[0];

    public Dictionary<string, string> headers = new Dictionary<string, string>();

    public override string ToString()
    {
        //return base.ToString();
        StringBuilder sb = new StringBuilder();
        sb.Append(version + " " + state + " " + state_description + "\r\n");
        foreach (var de in headers)
        {
            sb.Append(de.Key + ": " + de.Value + "\r\n");
        }
        if (!string.IsNullOrEmpty(strdata))
        {
            var dat = Encoding.UTF8.GetBytes(strdata);
            sb.Append("Content-Length: " + dat.Length + "\r\n\r\n" + data);
        }
        else
        {
            sb.Append("Content-Length: 0\n\n");
        }

        return sb.ToString();
    }

    public byte[] GetResponse()
    {
        byte[] ret = null;
        StringBuilder sb = new StringBuilder();
        sb.Append(version + " " + state + " " + state_description + "\r\n");
        foreach (var de in headers)
        {
            sb.Append(de.Key + ": " + de.Value + "\r\n");
        }
        sb.Append("Content-Length: " + data.Length + "\r\n\r\n");
        var hdat = Encoding.UTF8.GetBytes(sb.ToString());
        ret = new byte[hdat.Length + data.Length];
        hdat.CopyTo(ret, 0);
        if (data.Length > 0) data.CopyTo(ret, hdat.Length);
        return ret;
    }

    public void ResponseAsync(SocketAgent sa)
    {

    }

    void IResponse.SetHeader(string h, string val)
    {
        //throw new NotImplementedException();
        headers[h] = val;
    }

    string IResponse.GetHeader(string h)
    {
        if (headers.ContainsKey(h))
        {
            return headers[h];
        }
        return string.Empty;
    }

    string IResponse.GetHeaders()
    {
        //throw new NotImplementedException();
        StringBuilder sb = new StringBuilder();
        sb.Append(version + " " + state + " " + state_description + "\r\n");
        foreach (var de in headers)
        {
            sb.Append(de.Key + ": " + de.Value + "\r\n");
        }
        return sb.ToString();
    }

    byte[] IResponse.GetContent()
    {
        //throw new NotImplementedException();
        return data;
    }

    void IResponse.Close()
    {
        //throw new NotImplementedException();
    }

    byte[] IResponse.GetResponseBytes()
    {
        return GetResponse();
    }
}



