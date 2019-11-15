using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RequestHandler
{
    private static Dictionary<string, ContentType> ExtContentTypeMap = new Dictionary<string, ContentType>()
        {
            { "htm", new ContentType (){ ContentTypeString = "text/html;charset=utf-8", IsText = true }},
            { "html", new ContentType (){ ContentTypeString = "text/html;charset=utf-8", IsText = true }},
            //{ "sjs", new ContentType (){ ContentTypeString = "text/html;charset=utf-8", IsText = true, ServerScript = new ServerJS() }},
            { "js", new ContentType (){ ContentTypeString = "application/javascript;charset=utf-8", IsText = true }},
            { "jpg", new ContentType (){ ContentTypeString = "image/jpeg;", IsText = false }},
            { "png", new ContentType (){ ContentTypeString = "image/png;", IsText = false }},
            { "gif", new ContentType (){ ContentTypeString = "image/gif;", IsText = false }},
            { "default", new ContentType (){ ContentTypeString = "", IsText = false }},
        };

    internal static string WebRoot = Environment.CurrentDirectory + "\\WWW_Root\\";

    //StringBuilder sb = new StringBuilder();
    MemoryStream m_buff = new MemoryStream();
    Request m_req = new Request();
    bool m_bAllrecived = false;

    public void PushRecived(byte[] buff, int len)
    {
        if (m_req.PushData(buff, len))
        {
            m_bAllrecived = true;
        }
    }

    public bool IsAllRecived()
    {
        return m_bAllrecived;
    }

    public void BeginResponse(SocketAgent sa)
    {
        Console.WriteLine(m_req.method + " " + m_req.path);

        Response rsp = new Response();
        rsp.headers["Server"] = "SharpHttpServer";
        string path = (WebRoot + m_req.path).Replace('/', '\\');
        if (path.LastIndexOf('\\') == path.Length - 1)
        {
            path += "index.html";
        }
        var ct = GetContentType(path);
        {
            try
            {
                var st = File.OpenRead(path);
                rsp.data = new byte[st.Length];
                int readed = 0, offset = 0;
                do
                {
                    readed = st.Read(rsp.data, offset, rsp.data.Length);
                    offset += readed;
                } while (offset < rsp.data.Length);
                st.Close();
                rsp.headers["Content-Type"] = ct.ContentTypeString;

                var rdc = new RequestDataContext()
                {
                    GET = m_req.GetedData,
                    POST = m_req.PostedData,
                    Connect = sa,
                    RequestFile = path,
                    Response = rsp,
                };
                if (ct.HandleSocket)
                {
                    //send by it self
                    if (ct.ServerScript != null)
                    {
                        ct.ServerScript.Execute(rsp.data, rdc);
                    }
                    else
                    {
                        throw new Exception("!!!! Invalid plugin while handle file : " + path);
                    }
                }
                else
                {
                    if (/*ct.IsText && */ct.ServerScript != null)
                    {
                        rsp.data = ct.ServerScript.Execute(rsp.data, rdc);
                    }
                    sa.SendData(rsp.GetResponse(), _ => sa.Close());
                }
                //rsp.headers["Connection"] = "Keep-Alive";
            }
            catch (System.Exception ex)
            {
                rsp.state = "404";
                rsp.state_description = "not found";
                rsp.strdata = ex.ToString();
                sa.SendData(rsp.GetResponse(), _ => sa.Close());
            }
        }
        //return rsp.GetResponse();//Encoding.UTF8.GetBytes(rsp.ToString());
    }

    public byte[] GetResponseData()
    {
        Response rsp = new Response();
        rsp.headers["Server"] = "SharpHttpServer";
        string path = (WebRoot + m_req.path).Replace('/', '\\');
        if (path.LastIndexOf('\\') == path.Length - 1)
        {
            path += "index.html";
        }
        var ct = GetContentType(path);
        {
            try
            {
                var st = File.OpenRead(path);
                rsp.data = new byte[st.Length];
                int readed = 0, offset = 0;
                do
                {
                    readed = st.Read(rsp.data, offset, rsp.data.Length);
                    offset += readed;
                } while (offset < rsp.data.Length);
                rsp.headers["Content-Type"] = ct.ContentTypeString;
                if (/*ct.IsText && */ct.ServerScript != null)
                {
                    rsp.data = ct.ServerScript.Execute(rsp.data, new RequestDataContext()
                    {
                        GET = m_req.GetedData,
                        POST = m_req.PostedData
                    });
                }
                //rsp.headers["Connection"] = "Keep-Alive";
                st.Close();
            }
            catch (System.Exception ex)
            {
                rsp.state = "404";
                rsp.state_description = "not found";
                rsp.strdata = ex.ToString();
            }
        }
        return rsp.GetResponse();//Encoding.UTF8.GetBytes(rsp.ToString());
    }

    public static bool RegisterScript(string ext, ContentType ct)
    {
        bool ret = false;
        if (!ExtContentTypeMap.ContainsKey(ext) && ct != null)
        {
            ExtContentTypeMap.Add(ext, ct);
            ret = true;
        }
        return ret;
    }

    private ContentType GetContentType(string path)
    {
        ContentType ret = null;
        int idx = path.LastIndexOf('.');
        string ext = path.Substring(idx + 1);
        if (ExtContentTypeMap.ContainsKey(ext))
        {
            ret = ExtContentTypeMap[ext];
        }
        else
        {
            ret = ExtContentTypeMap["default"];
        }
        return ret;
    }

    public class Request
    {
        public string method = string.Empty;
        public string path = string.Empty;
        public string version = string.Empty;
        public byte[] data = null;
        public Dictionary<string, string> headers = new Dictionary<string, string>();
        public Dictionary<string, string> GetedData = new Dictionary<string, string>();
        public Dictionary<string, string> PostedData = new Dictionary<string, string>();


        MemoryStream m_buff = new MemoryStream();
        bool m_bHeadEnd = false;
        string m_strMethod = string.Empty;
        int m_nRecivedData = 0;
        int m_nContentLength = 0;//int.MaxValue;
        bool preIsN = false;
        byte lastRN = 0;
        int dataFixOffset = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>返回为true时表明已经接受完毕， 否则为false</returns>
        public bool PushData(byte[] data, int len)
        {
            if (len <= 0)
            {
                return false;
            }
            if (string.IsNullOrEmpty(m_strMethod))
            {
                int idx = 0;
                if (ReciveFirstLine(data, len, out idx))
                {
                    //headers
                    if (idx < len)
                    {
                        if (ReciveHeaders(data, idx, len, out idx))
                        {
                            m_bHeadEnd = true;
                            dataFixOffset = idx > len ? idx - len : 0;
                            if (idx < len)
                            {
                                //data
                                m_buff.Write(data, idx, len - idx);
                                m_nRecivedData += len - idx;
                            }
                        }
                    }
                }
                else
                {
                    //m_buff.Write(data, 0, len);
                }
            }
            else if (!m_bHeadEnd)
            {
                int idx = 0;
                if (ReciveHeaders(data, idx, len, out idx))
                {
                    m_bHeadEnd = true;
                    dataFixOffset = idx > len ? idx - len : 0;
                    if (idx < len)
                    {
                        //data
                        m_buff.Write(data, idx, len - idx);
                        m_nRecivedData += len - idx;
                    }
                }
            }
            else
            {
                if (data.Length > dataFixOffset)
                {
                    m_nRecivedData += len - dataFixOffset;
                    m_buff.Write(data, dataFixOffset, len - dataFixOffset);
                }
                else
                {
                    dataFixOffset -= data.Length;
                }
            }
            if (m_strMethod == "GET")
            {
                if (m_bHeadEnd)
                {
                    return true;
                }
                else
                {
                    return false;
                }
                //return true;
            }
            else if (m_strMethod == "POST" && m_bHeadEnd)
            {
                if (m_nContentLength <= m_nRecivedData)
                {
                    data = m_buff.GetBuffer();
                    if (headers.ContainsKey("Content-Type"))
                    {
                        string ct = headers["Content-Type"].ToLower();
                        if (ct.IndexOf("application") >= 0
                            || ct.IndexOf("text") >= 0)
                        {
                            int idx_c = ct.IndexOf("charset=");
                            if (idx_c >= 0)
                            {
                                string charset = ct.Substring(idx_c + "charset=".Length, ct.IndexOf(';', idx_c)).Trim();
                                var encode = System.Text.Encoding.GetEncoding(charset);
                                if (encode == null)
                                {
                                    encode = Encoding.UTF8;
                                }
                                PostedData = GetParams(System.Web.HttpUtility.UrlDecode(data, 0, m_nContentLength, encode));
                            }
                            else
                            {
                                PostedData = GetParams(System.Web.HttpUtility.UrlDecode(data, 0, m_nContentLength, Encoding.UTF8));
                            }
                        }
                    }
                    m_buff.Close();
                    m_buff = null;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool ReciveFirstLine(byte[] data, int len, out int nextIdx)
        {
#if DEBUG
            string dbgstr = Encoding.UTF8.GetString(data);
#endif
            nextIdx = 0;
            bool ret = false;

            byte c = 0, c2 = 0;
            for (int i = 0; i < len/* - 1*/; i++)
            {
                //c = data[i];
                c2 = data[i/* + 1*/];
                if (/*c == '\r' && */c2 == '\n')
                {
                    ret = true;
                    m_buff.Write(data, 0, i);//首行接收完毕
                    nextIdx = i + (c2 == '\r' ? 2 : 1);
                    if (!ParseFirstLine(Encoding.UTF8.GetString(m_buff.GetBuffer())))
                    {
                        m_buff.Close();
                        m_buff = null;
                        throw new Exception("HTTP/1.1 400 bad request");
                    }
                    m_buff.Close();
                    m_buff = new MemoryStream();
                    break;
                }
            }
            if (!ret)
            {
                m_buff.Write(data, 0, len);
            }
            return ret;
        }

        private bool ReciveHeaders(byte[] data, int startIdx, int len, out int nexIdx)
        {
            bool ret = false;
            //foreach (char c in data)
            byte c = 0;
            nexIdx = startIdx;
            for (int i = startIdx; i < len; i += 2)
            {
                c = data[i];
                if (c == '\r' || c == '\n'/*&& c2 == '\n'*/)
                {
                    if (preIsN)
                    {
                        if (lastRN == '\r' && c == '\n')
                        {
                            lastRN = c;
                            continue;
                        }
                        ret = true;
                        m_buff.Write(data, startIdx, i - startIdx);//头接收完毕
                        nexIdx = i + (c == '\r' ? 2 : 1);
                        m_buff.Seek(0, SeekOrigin.Begin);
                        StreamReader sr = new StreamReader(m_buff);
                        ParseHeader(sr.ReadToEnd());

                        if (m_strMethod == "POST")
                        {
                            //m_nRecivedData = len - i - 1;
                            m_buff.Close();
                            m_buff = new MemoryStream();
                            //m_buff.Write(data, i + 2, len - i - 2);
                        }
                        else
                        {
                            sr.Dispose();
                            m_buff.Close();
                        }
                        break;
                    }
                    else
                    {
                        preIsN = true;
                        lastRN = c;
                    }
                }
                else
                    preIsN = false;
            }
            if (!ret)
            {
                m_buff.Write(data, startIdx, len - startIdx);
            }
            return ret;
        }

        private void ParseHeader(string head)
        {
            string[] ss = head.TrimEnd(new char[] { '\r' }).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in ss)
            {
                string[] _ss = s.Split(new string[] { ": " }, 2, StringSplitOptions.None);
                headers[_ss[0]] = _ss[1];
            }
            if (headers.ContainsKey("Content-Length"))
            {
                m_nContentLength = int.Parse(headers["Content-Length"]);
            }
        }

        private bool ParseFirstLine(string line)
        {
            string[] s = line.Split(new char[] { ' ' });
            if (s.Length != 3)
            {
                return false;
            }
            else
            {
                m_strMethod = method = s[0];
                int _i = s[1].IndexOf('?');
                path = s[1].Substring(0, _i > 0 ? _i : s[1].Length);
                if (_i > 0)
                {
                    GetedData = GetParams(System.Web.HttpUtility.UrlDecode(s[1].Substring(_i + 1), Encoding.UTF8));
                }
                switch (s[0])
                {
                    case "GET":
                        break;
                    case "POST":
                        break;
                    default:
                        return false;
                }
                return true;
            }
        }

        private Dictionary<string, string> GetParams(string paramstr)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            string[] ss = paramstr.Trim().Split(new char[] { '&' });
            foreach (string s in ss)
            {
                string[] _ss = s.Split(new char[] { '=' }, 2);
                if (_ss.Length == 2)
                {
                    ret[_ss[0]] = _ss[1];
                }
            }
            return ret;
        }
    }

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

    public class ContentType
    {
        public string ContentTypeString { get; set; }
        public bool IsText { get; set; }
        public IServerScript ServerScript = null;
        public bool HandleSocket { get; set; }
    }
}
