using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WeiBoCrawler
{
    class HttpRequest
    {
        private static readonly string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        public CookieCollection Cookies{get;set;}
        public static string UserAgent { get; set; }

        public Uri ProxyAddress {
            get { return proxyUri; }
            set {
            proxy = new WebProxy();
            proxy.Address = value;
            proxyUri = value;
        } }
        WebProxy proxy;
        Uri proxyUri;
        public string GetHttpResponseStr(string url) {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                request.UserAgent = UserAgent == null ? DefaultUserAgent : UserAgent;
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(Cookies);
                if (ConfigManager.IsUseProxy) {
                    request.Proxy = proxy;
                }
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                Stream receiveStream = response.GetResponseStream();
                Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                StreamReader readStream = new StreamReader(receiveStream, encode);
                return readStream.ReadToEnd();
            }
            catch (Exception ex) {
                System.Console.WriteLine(ex.Message);
                return null;
            }
        }

        
    }
}
