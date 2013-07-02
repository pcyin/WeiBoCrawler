using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeiBoCrawler
{
    class Ultility
    {
        public static bool Error(string content) {
            return String.IsNullOrEmpty(content) || content.Contains("微博广场") && content.Contains("随便看看") && content.Contains("名人在说") || content.Contains("使用明文密码") || content.Contains("您浏览的网页出错");
        }
        public static bool WeiBoNotExist(string content) {
            return content.Contains("微博不存在");
        }

        public static string parseUid(string uid)
        {
            if (uid.Contains("?st="))
            {
                uid = uid.Substring(
                    0,
                    uid.Length - 8
                    );
            }
            if (uid.StartsWith("/"))
                uid = uid.Substring(1);
            if (uid.StartsWith("u/"))
            {
                uid = uid.Substring(2);
            }
            return uid;
        }

        public static string getUidByUrl(string url)
        {
            return url.Substring(17, url.LastIndexOf('/') - 17);
        }
        public static string getWidByUrl(string url)
        {
            return url.Substring(url.LastIndexOf('/') + 1);
        }
    }
}
