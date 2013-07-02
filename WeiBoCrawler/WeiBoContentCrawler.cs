using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Data.SqlClient;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Net;
//using System.Threading;

namespace WeiBoCrawler
{
    class WeiBoContentCrawler
    {
        ConcurrentQueue<string> urlQueue;
        ConcurrentQueue<CommentCrawlJob> commentCrawQueue;
        CookieCollection cookies;
        Uri proxyUri;
        HttpRequest request;
        Random rnd = new Random();

        public WeiBoContentCrawler(ConcurrentQueue<string> urlQueue, ConcurrentQueue<CommentCrawlJob> commentCrawQueue, CookieCollection cookies, Uri proxyUri)
        {
            this.urlQueue = urlQueue;
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

            string pageUrl = null;
            while (true)
            {
                while (!urlQueue.TryDequeue(out pageUrl)) ;
                if (pageUrl == "exit")
                {
                    System.Console.WriteLine("Thread exit.");
                    return;
                }
                bool firstPageCrawlered = false;
                string existSql = "SELECT COUNT(*) FROM WeiBoList WHERE url = @url and com_finish = false and content is not null";
                int num = Convert.ToInt32(MySqlHelper.ExecuteScalar(existSql, new MySqlParameter[] { 
                            new MySqlParameter("@url",pageUrl)
                          }));
                if (num > 0)
                {
                    Console.WriteLine("First page has been crawled");
                    firstPageCrawlered = true;
                }

                string content = request.GetHttpResponseStr(pageUrl);
                int errorNum = 0;
                while (Ultility.Error(content))
                {
                    errorNum++;
                    System.Threading.Thread.Sleep(30 * 1000 *errorNum);
                    content = request.GetHttpResponseStr(pageUrl);
                }
                if (Ultility.WeiBoNotExist(content))
                {
                    continue;
                }
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(content);
                try
                {
                    HtmlNode contentNode = doc.GetElementbyId("M_");
                    string weiboContent = contentNode.SelectSingleNode("//span[@class=\"ctt\"]").InnerText;
                    var q = from node in contentNode.SelectNodes("//a") where node.InnerText == "原图" select node;

                    bool hasImg = false;
                    if (q.Count() > 0)
                        hasImg = true;


                    //SQL Update
                    string sqlUpdateWeibo = "UPDATE WeiBoList set content = @content, has_img = @has_img where url = @url";
                    MySqlHelper.ExecuteNonQuery(sqlUpdateWeibo, new MySqlParameter[] {
                        new MySqlParameter("@content",weiboContent),
                        new MySqlParameter("@has_img",hasImg),
                        new MySqlParameter("@url",pageUrl)
                    });

                    int comPageNum = doc.DocumentNode.SelectSingleNode("//input[@name=\"mp\"]") == null ? 1 : doc.DocumentNode.SelectSingleNode("//input[@name=\"mp\"]").GetAttributeValue("value", 1);


                    //crawl the comments in the first page
                    q = from node in doc.DocumentNode.SelectNodes("//div[@class='c']") where node.GetAttributeValue("id", "null").StartsWith("C_") select node;

                    if (!firstPageCrawlered)
                    {
                        foreach (HtmlNode node in q)
                        {
                            string uid = node.SelectSingleNode("./a").GetAttributeValue("href", "null");
                            uid = Ultility.parseUid(uid);
                            string comContent = node.SelectSingleNode("./span[@class=\"ctt\"]").InnerText;

                            //SQL Update
                            string sql = "INSERT INTO WeiBoCommentList(wid,uid,cuid,content) VALUES(@wid,@uid,@cuid,@content)";
                            MySqlHelper.ExecuteNonQuery(sql, new MySqlParameter[] { 
                            new MySqlParameter("@wid",Ultility.getWidByUrl(pageUrl)),
                            new MySqlParameter("@uid",Ultility.getUidByUrl(pageUrl)),
                            new MySqlParameter("@cuid",uid),
                            new MySqlParameter("@content",comContent)
                        });

                            Console.WriteLine(String.Format("Url:{0} PageId:{1}", pageUrl, 1));
                        }
                    }

                    if (comPageNum > 1)
                    {
                        commentCrawQueue.Enqueue(
                            new CommentCrawlJob() { BeginPage = 2, EndPage = comPageNum, Url = pageUrl }
                        );

                    }
                    else
                    {
                        //SQL Update indicating job finish
                        string finsihSql = "UPDATE WeiBoList SET com_finish = true WHERE url = @url";
                        MySqlHelper.ExecuteNonQuery(finsihSql, new MySqlParameter[]{
                            new MySqlParameter("url",pageUrl)
                        });
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }

                System.Threading.Thread.Sleep(3500 + rnd.Next(5000));
            }
        }

    }
}
