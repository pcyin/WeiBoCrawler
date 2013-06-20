using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Data.SqlClient;
using System.Net;
using MySql.Data.MySqlClient;
using MySql.Data;

namespace WeiBoCrawler
{
    class WeiBoCommentCrawler
    {
        ConcurrentQueue<CommentCrawlJob> commentCrawQueue;
        CookieCollection cookies;
        Uri proxyUri;
        HttpRequest request;

        public WeiBoCommentCrawler(ConcurrentQueue<CommentCrawlJob> commentCrawQueue, CookieCollection cookies, Uri proxyUri)
        {
            this.commentCrawQueue = commentCrawQueue;
            this.cookies = cookies;
            this.proxyUri = proxyUri;
            request = new HttpRequest();
            request.Cookies = cookies;
            if (ConfigManager.IsUseProxy)
                request.ProxyAddress = proxyUri;
        }

        public void Run()
        {

            CommentCrawlJob job = null;
            while (true)
            {
                while (!commentCrawQueue.TryDequeue(out job))
                    System.Threading.Thread.Sleep(2000);

                if (job.Url == "exit") {
                    return;
                }

                for (int pageId = job.BeginPage; pageId <= job.EndPage; pageId++)
                {
                    string pageUrl = job.Url + "?page=" + pageId + "&st=86e0";

                    string content = request.GetHttpResponseStr(pageUrl);
                    while (Ultility.Error(content))
                    {
                        System.Threading.Thread.Sleep(5 * 1000);
                        content = request.GetHttpResponseStr(pageUrl);
                    }
                    if (Ultility.WeiBoNotExist(content))
                    {
                        continue;
                    }
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(content);  
                    var q = from node in doc.DocumentNode.SelectNodes("//div[@class='c']") where node.GetAttributeValue("id", "null").StartsWith("C_") select node;

                    foreach (HtmlNode node in q)
                    {
                        bool isTop = node.SelectSingleNode("./span[class='kt']") != null;
                        if (isTop && pageId > 1)
                            continue;

                        string uid = node.SelectSingleNode("./a").GetAttributeValue("href", "null");
                        uid = Ultility.parseUid(uid);
                        string comContent = node.SelectSingleNode("./span[@class=\"ctt\"]").InnerText;

                        //SQL Update
                        string sql = "INSERT INTO WeiBoCommentList(wid,uid,cuid,content) VALUES(@wid,@uid,@cuid,@content)";
                        MySqlHelper.ExecuteNonQuery(sql, new MySqlParameter[] { 
                            new MySqlParameter("@wid",Ultility.getWidByUrl(job.Url)),
                            new MySqlParameter("@uid",Ultility.getUidByUrl(job.Url)),
                            new MySqlParameter("@cuid",uid),
                            new MySqlParameter("@content",comContent)
                        });

                        Console.WriteLine(String.Format("Url:{0} PageId:{1}", job.Url, pageId));

                    }

                    System.Threading.Thread.Sleep(2 * 1000);
                }



                //SQL Update indicating job finish
                string finsihSql = "UPDATE WeiBoList SET com_finish = 1 WHERE url = @url";
                MySqlHelper.ExecuteNonQuery(finsihSql, new MySqlParameter[]{
                    new MySqlParameter("url",job.Url)
                });


                /*foreach (var c in commentCrawQueue) {
                    string isql = "INSERT INTO ComJobList(url,begin,end) VALUES(@url,@begin,@end)";
                    MySqlHelper.ExecuteNonQuery(isql, new MySqlParameter[]{
                        new MySqlParameter("@url",c.Url),
                        new MySqlParameter("@begin",c.BeginPage),
                        new MySqlParameter("@end",c.EndPage)
                    });
                }*/

            }
        }

    }
}
