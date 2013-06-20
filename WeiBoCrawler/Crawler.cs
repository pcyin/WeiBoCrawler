using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Data.SqlClient;
//using System.Threading;

namespace WeiBoCrawler
{
    class Crawler
    {
        ConcurrentQueue<string> urlQueue;
        public Crawler(ConcurrentQueue<string> urlQueue)
        {
            this.urlQueue = urlQueue;
        }

        public void Run() {
           
            string pageUrl = null;
            while (true) {
                while (!urlQueue.TryDequeue(out pageUrl)) ;
                if (pageUrl == "exit")
                {
                    System.Console.WriteLine("Thread exit.");
                    return;
                }

                string content = HttpRequest.GetHttpResponseStr(pageUrl);
                while (Ultility.Error(content))
                {
                    System.Threading.Thread.Sleep(5 * 1000);
                    content = HttpRequest.GetHttpResponseStr(pageUrl);
                }
                if (Ultility.WeiBoNotExist(content)) {
                    continue;
                }
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(content);
                
                HtmlNode contentNode = doc.GetElementbyId("M_");
                string weiboContent = contentNode.SelectSingleNode("//span[@class=\"ctt\"]").InnerText;
                var q = from node in contentNode.SelectNodes("//a") where node.InnerText == "原图" select node;
                bool hasImg = false;
                if (q.Count() > 0)
                    hasImg = true;

                //SQL Update
                string sqlUpdateWeibo = "UPDATE [WeiBoList] set content = @content, has_img = @has_img where url = @url";
                SqlHelper.ExecuteNonQuery(sqlUpdateWeibo, new SqlParameter[] {
                    new SqlParameter("@content",weiboContent),
                    new SqlParameter("@has_img",hasImg),
                    new SqlParameter("@url",pageUrl)
                });

                int comPageNum = doc.DocumentNode.SelectSingleNode("//input[@name=\"mp\"]") == null ? 1 : doc.DocumentNode.SelectSingleNode("//input[@name=\"mp\"]").GetAttributeValue("value", 1);
                
                for (int pageId = 1; pageId<=comPageNum;pageId++ ){
                    if (pageId > 1) {
                        string comUrl = pageUrl + "?page=" + pageId + "&st=86e0";
                        content = HttpRequest.GetHttpResponseStr(comUrl);
                        while (String.IsNullOrEmpty(content)||content.Contains("微博广场") || content.Contains("使用明文密码"))
                        {
                            System.Threading.Thread.Sleep(5 * 1000);
                            content = HttpRequest.GetHttpResponseStr(comUrl);
                        }
                        doc.LoadHtml(content);
                    }
                        

                    q = from node in doc.DocumentNode.SelectNodes("//div[@class='c']") where node.GetAttributeValue("id", "null").StartsWith("C_") select node;
                    foreach (HtmlNode node in q)
                    {
                        bool isTop = node.SelectSingleNode("./span[class='kt']") != null;
                        if (isTop && pageId > 1)
                            continue;

                        string uid = node.SelectSingleNode("./a").GetAttributeValue("href", "null");
                        uid = parseUid(uid);
                        string comContent = node.SelectSingleNode("./span[@class=\"ctt\"]").InnerText;

                        //SQL Update
                        string sql = "INSERT INTO [WeiBoCommentList](wid,uid,cuid,content) VALUES(@wid,@uid,@cuid,@content)";
                        SqlHelper.ExecuteNonQuery(sql, new SqlParameter[] { 
                            new SqlParameter("@wid",getWidByUrl(pageUrl)),
                            new SqlParameter("@uid",getUidByUrl(pageUrl)),
                            new SqlParameter("@cuid",uid),
                            new SqlParameter("@content",comContent)
                        });

                        Console.WriteLine(String.Format("Url:{0} PageId:{1}",pageUrl,pageId));
                       
                    }
                    System.Threading.Thread.Sleep(5 * 1000);
                }
                
            }
        }

        string parseUid(string uid){
            if(uid.EndsWith("?st=86e0")){
                uid = uid.Substring(
                    0,
                    uid.Length - 8
                    );
            }
            if(uid.StartsWith("/"))
                uid = uid.Substring(1);
            if(uid.StartsWith("u/")){
                uid = uid.Substring(2);
            }
            return uid;
        }

        string getUidByUrl(string url)
        {
            return url.Substring(17, url.LastIndexOf('/') - 17);
        }
        string getWidByUrl(string url)
        {
            return url.Substring(url.LastIndexOf('/') + 1);
        }

    }
}
