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
            { "js", new ContentType (){ ContentTypeString = "application/javascript;charset=utf-8", IsText = true }},
            { "jpg", new ContentType (){ ContentTypeString = "image/jpeg;", IsText = false }},
            { "png", new ContentType (){ ContentTypeString = "image/png;", IsText = false }},
            { "gif", new ContentType (){ ContentTypeString = "image/gif;", IsText = false }},
            { "default", new ContentType (){ ContentTypeString = "", IsText = false }},
        };

    internal static string WebRoot = Environment.CurrentDirectory + "\\WWW_Root\\";

    MemoryStream m_buff = new MemoryStream();
    Request m_req = new Request();
    bool m_bAllrecived = false;
    
    public bool IsAllRecived()
    {
        return m_bAllrecived;
    }

    // 输入接收到的请求
    public void PushRecived(byte[] buff, int len)
    {
        if (m_req.PushData(buff, len))
        {
            m_bAllrecived = true;
        }
    }

    // 开始处理应答
    public void BeginResponse(SocketAgent agent)
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
                    Connect = agent,
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
                    agent.SendData(rsp.GetResponse(), _ => agent.Close());
                }
                //rsp.headers["Connection"] = "Keep-Alive";
            }
            catch (Exception ex)
            {
                rsp.state = "404";
                rsp.state_description = "not found";
                rsp.strdata = ex.ToString();
                agent.SendData(rsp.GetResponse(), _ => agent.Close());
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

    public class ContentType
    {
        public string ContentTypeString { get; set; }
        public bool IsText { get; set; }
        public IServerScript ServerScript = null;
        public bool HandleSocket { get; set; }
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
}
