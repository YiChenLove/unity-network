

public class HTTPInfo
{
    public JsonData data;
    public int errNo;
    public string errMsg;
    public string errstr;
}

/**
* 错误码
*/
public enum HttpErrCode
{
    defaultError = 9999,
    Error = 1001,
    ConnectionTimedOut = 1002,
    TimedOut = 1003,
    OutMemory = 1004,
    MD5Error = 1005,
}

public class XhrHttp
{
    // 最大重试次数
    private static int HTTP_MAX_TRYTIMES = 5;

    // 超时等待时间(millisecond)
    private static int HTTP_USR_TIMEOUT = 15000;

    /// <summary>
    /// http get 请求
    /// </summary>
    /// <param name="url">请求地址</param>
    /// <param name="exam">额外参数</param>
    /// <returns>返回一个字符串类型的json串</returns>
    public static async Task<HTTPInfo> Get(string url, Dictionary<string, object> exam = null, bool isCheckProxy = true)
    {
        if (!NetMgr.Inst.CheckInternetReachable(url)) return null;

        int retry = HTTP_MAX_TRYTIMES;
        if (exam != null && exam.ContainsKey("retry"))
        {
            retry = Convert.ToInt32(exam["retry"]);
        }

        int timeOut = HTTP_USR_TIMEOUT;
        if (exam != null && exam.ContainsKey("timeOut"))
        {
            timeOut = Convert.ToInt32(exam["timeOut"]);
        }

        async Task<HTTPInfo> doGet()
        {
            string content = string.Empty;
            int statusCode = -1;
            HTTPInfo resultInfo = null;

            HttpClient client = new HttpClient();
            string proxyurl = NAAgent.getProxyUrl();
            int proxyport = NAAgent.getProxyPort();
            if (!string.IsNullOrEmpty(proxyurl) && isCheckProxy)
            {
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.UseProxy = true;
                clientHandler.Proxy = new WebProxy(string.Format("{0}:{1}", proxyurl, 
                    proxyport),false);
                clientHandler.PreAuthenticate = true;
                clientHandler.UseDefaultCredentials = false;
                client = new HttpClient(clientHandler);
            }
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (exam != null && exam.ContainsKey("header"))
            {
                Dictionary<string, string> header = exam["header"] as Dictionary<string, string>;
                foreach (var item in header)
                {
                    client.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }

            client.Timeout = TimeSpan.FromMilliseconds(timeOut);
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                statusCode = (int) response.StatusCode;
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
                
            }
            catch (Exception e)
            {
                if (retry > 0)
                {
                    retry--;
                    await doGet();
                }
                else
                {
                    string act = url.Split("?")[0].Replace(GetBaseUrl(), "");
                    string errDesc = $"http error, act = {act}, statusCode = {statusCode}, Message = {e.Message}";
                    EventDispatcher.instance.DispatchEvent(EventNameDef.INITSCENE_ERROR, errDesc);
                }
            }

            client.Dispose();

            //errorcode拦截处理
            if (content != string.Empty)
            {
                try
                {
                    resultInfo = JsonMapper.ToObject<HTTPInfo>(content);
                    statusCode = resultInfo.errNo;
                }
                catch (Exception e)
                { 
                    string act = url.Split("?")[0].Replace(GetBaseUrl(), "");
                    string errDesc = $"http content error, statusCode = {statusCode}, act = {act}, message = invited json error";
                    EventDispatcher.instance.DispatchEvent(EventNameDef.INITSCENE_ERROR, errDesc);

                    return resultInfo;
                }
            }

            if (statusCode != 0)
            {
                NetMgr.Inst.NetErrorCode = statusCode;
                
                string act = url.Split("?")[0].Replace(GetBaseUrl(), "");
                string errDesc = $"http code error, act = {act}, statusCode = {statusCode}";
                EventDispatcher.instance.DispatchEvent(EventNameDef.INITSCENE_ERROR, errDesc);
            }

            return resultInfo;
        }

        return await doGet();
    }

    /// <summary>
    /// http post 请求
    /// </summary>
    /// <param name="url">请求地址</param>
    /// <param name="data">上传的数据</param>
    /// <param name="exam">额外参数</param>
    /// <returns>返回一个字符串类型的json串</returns>
    public static async Task<HTTPInfo> Post(string url, SortedDictionary<string, object> data,
        Dictionary<string, object> exam)
    {
        if (!NetMgr.Inst.CheckInternetReachable(url)) return null;

        int retry = HTTP_MAX_TRYTIMES;
        if (exam.ContainsKey("retry"))
        {
            retry = Convert.ToInt32(exam["retry"]);
        }
        int timeOut = HTTP_USR_TIMEOUT;
        if (exam != null && exam.ContainsKey("timeOut"))
        {
            timeOut = Convert.ToInt32(exam["timeOut"]);
        }

        async Task<HTTPInfo> doPost()
        {
            string content = string.Empty;
            int statusCode = -1;
            HTTPInfo resultInfo = null;

            HttpClient client = new HttpClient();
            string proxyurl = NAAgent.getProxyUrl();
            int proxyport = NAAgent.getProxyPort();
            if (!string.IsNullOrEmpty(proxyurl))
            {
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.UseProxy = true;
                clientHandler.Proxy = new WebProxy(string.Format("{0}:{1}", proxyurl, 
                    proxyport),false);
                clientHandler.PreAuthenticate = true;
                clientHandler.UseDefaultCredentials = false;
                client = new HttpClient(clientHandler);
            }
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (exam.ContainsKey("header"))
            {
                Dictionary<string, string> header = exam["header"] as Dictionary<string, string>;
                foreach (var item in header)
                {
                    client.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }

            client.Timeout = TimeSpan.FromMilliseconds(timeOut);
            try
            {
                string str = JsonConvert.SerializeObject(data);
                HttpContent hc = new StringContent(str, UnicodeEncoding.UTF8, "application/json");
                hc.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage response = await client.PostAsync(url, hc);
                statusCode = (int) response.StatusCode;
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
                
            }
            catch (Exception e)
            {                
                if (retry > 0)
                {
                    retry--;
                    await doPost();
                }
                else
                {
                    string desc = $"http error, statusCode = {statusCode}, Message = {e.Message}, url = {System.Web.HttpUtility.UrlEncode(url)}";
                
                    string act = url.Split("?")[0].Replace(GetBaseUrl(), "");
                    string errDesc = $"http error, act = {act}, statusCode = {statusCode}, Message = {e.Message}";
                    EventDispatcher.instance.DispatchEvent(EventNameDef.INITSCENE_ERROR, errDesc);
                }
            }

            client.Dispose();

            //errorcode拦截处理
            if (content != string.Empty)
            {
                
                try
                {
                    resultInfo = JsonMapper.ToObject<HTTPInfo>(content);
                    statusCode = resultInfo.errNo;
                }
                catch (Exception e)
                {
                    string desc = $"http content error, statusCode = {statusCode}, message = {e.Message}, url = {System.Web.HttpUtility.UrlEncode(url)}";
                
                    string act = url.Split("?")[0].Replace(GetBaseUrl(), "");
                    string errDesc = $"http content error, act = {act}, statusCode = {statusCode}, message = invited json error";
                    EventDispatcher.instance.DispatchEvent(EventNameDef.INITSCENE_ERROR, errDesc);
                    
                    return resultInfo;
                }
            }

            if (statusCode != 0)
            {
                NetMgr.Inst.NetErrorCode = statusCode;
                
                string desc = $"http code error, statusCode = {statusCode}, url = {System.Web.HttpUtility.UrlEncode(url)}";
                
                string act = url.Split("?")[0].Replace(GetBaseUrl(), "");
                string errDesc = $"http code error, act = {act}, statusCode = {statusCode}";
                EventDispatcher.instance.DispatchEvent(EventNameDef.INITSCENE_ERROR, errDesc);
            }

            return resultInfo;
        }

        return await doPost();
    }

    /// <summary>
    /// 根据给定参数生成带参url
    /// </summary>
    /// <param name="baseurl">url地址</param>
    /// <param name="act">路径</param>
    /// <param name="dic">参数</param>
    /// <returns>带参数的url</returns>
    public static string CrtUrlWithParams(string baseurl, string act, SortedDictionary<string, object> dic)
    {
        string url = baseurl + act;
        if (dic == null)
        {
            return url;
        }

        StringBuilder sb = new StringBuilder(url + "?");
        foreach (var item in dic)
        {
            sb.Append(item.Key).Append("=").Append(item.Value).Append("&");
        }

        sb.Remove(sb.Length - 1, 1);
        return sb.ToString();
    }

    public static async Task<HTTPInfo> GoGet(string act, SortedDictionary<string, object> dic, Dictionary<string, object> exparmDir = null)
    {
        string url = XhrHttp.CrtUrlWithParams(GetBaseUrl(), act, dic);
        Dictionary<string, object> exparm = new Dictionary<string, object>
        {
            {"retry", 5},
        };
        if (exparmDir != null)
        {
            exparm = exparm.Concat(exparmDir).ToDictionary(kv=>kv.Key, kv=>kv.Value);
        }
        return await Get(url, exparm);
    }

    public static async Task<HTTPInfo> GoPost(string act, SortedDictionary<string, object> data, Dictionary<string, object> exparmDir = null)
    {
        SortedDictionary<string, object> query = XhrHttp.GetInterfQuery();
        string url = XhrHttp.CrtUrlWithParams(GetBaseUrl(), act, query);
        Dictionary<string, object> exparm = new Dictionary<string, object>();
        exparm.Add("retry", 5);
        if (exparmDir != null)
        {
            exparm = exparm.Concat(exparmDir).ToDictionary(kv=>kv.Key, kv=>kv.Value);
        }
        return await Post(url, data, exparm);
    }

    public static async Task<HTTPInfo> GoGetByUri(string uri ,string act, SortedDictionary<string, object> dic, bool isCheckProxy = true)
    {
        string url = XhrHttp.CrtUrlWithParams(uri, act, dic);
        Dictionary<string, object> exparm = new Dictionary<string, object>
        {
            {"retry", 5},
        };
        return await Get(url, exparm, isCheckProxy);
    }

    public static SortedDictionary<string, object> GetInterfQuery()
    {
        var timeStamp = Utils.GetUTCTime();
        SortedDictionary<string, object> queryDic = new SortedDictionary<string, object>();
        queryDic.Add("__t__", timeStamp.ToString()); //请求时间（时间戳，毫秒）
#if UNITY_IOS
        queryDic.Add("os", "ios"); //平台，IOS、Android
#elif UNITY_ANDROID
        queryDic.Add("os", "android"); //平台，IOS、Android
#else
        queryDic.Add("os", SystemInfo.deviceType); //平台，IOS、Android
#endif
        queryDic.Add("sysVersion", SystemInfo.operatingSystem); //系统 ios 13.3、android 9
        queryDic.Add("device", SystemInfo.deviceModel); //设备型号 iPhone XR、小米9
        queryDic.Add("appVersion", Application.version); //	APP 版本号 1.1.0、1.2.0
        queryDic.Add("vcname", Application.version); //	APP 版本号 1.1.0、1.2.0

        return queryDic;
    }

    public static string GetBaseUrl()
    { 
        return Release.Host;
    }
}