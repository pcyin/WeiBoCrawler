using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Data.SqlClient;
using System.Xml.Linq;
using MySql.Data.MySqlClient;
using MySql.Data;


namespace WeiBoCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigManager.IsUseProxy = false;
            ConcurrentQueue<string> urlQueue = new ConcurrentQueue<string>();
            ConcurrentQueue<CommentCrawlJob> comJobQueue = new ConcurrentQueue<CommentCrawlJob>();
            List<WeiBoContentCrawler> contentCrawlerList = new List<WeiBoContentCrawler>();
            List<WeiBoCommentCrawler> commentCrawlerList = new List<WeiBoCommentCrawler>();

            string exePath = System.IO.Path.Combine(Environment.CurrentDirectory, "appconfig.xml");
            XElement rootNode = XElement.Load(exePath);
            var crawlers = from node in rootNode.Descendants("crawler") select node;
            var database = rootNode.Descendants("database").First();
            string sqlConnString = "Database={0};Data Source={1};User Id={2};Password={3}";
            sqlConnString = String.Format(sqlConnString, database.Descendants("database").First().Value, database.Descendants("host").First().Value, database.Descendants("user").First().Value, database.Descendants("password").First().Value);
            MySqlHelper.ConnectionStringLocalTransaction = sqlConnString;

            foreach (var crawler in crawlers) {
                string id = crawler.Attribute("id").Value;
                string type = crawler.Attribute("type").Value;
                var cookies = from cookie in crawler.Element("cookies").Descendants() select cookie;
                CookieCollection cookieCol = new CookieCollection();
                foreach (var cookie in cookies) {
                    Cookie c = new Cookie(cookie.Attribute("key").Value, cookie.Attribute("value").Value);
                    c.Domain = ".weibo.cn";
                    cookieCol.Add(c);
                }
                Uri proxyUri = new Uri(
                        crawler.Element("proxy").Attribute("url").Value
                    );
                if (type == "ContentCrawler")
                {
                    WeiBoContentCrawler c = new WeiBoContentCrawler(urlQueue, comJobQueue, cookieCol, proxyUri);
                    contentCrawlerList.Add(c);
                }
                else {
                    WeiBoCommentCrawler c = new WeiBoCommentCrawler(comJobQueue, cookieCol, proxyUri);
                    commentCrawlerList.Add(c);
                }
            }

            foreach( WeiBoContentCrawler c in contentCrawlerList ){
                Thread t = new Thread(c.Run);
                t.Start();
            }
            foreach (WeiBoCommentCrawler c in commentCrawlerList)
            {
                Thread t = new Thread(c.Run);
                t.Start();
            }



            //urlQueue.Enqueue("http://weibo.com/1635106672/zAn0lrPAZ");

            string sql = "SELECT * FROM WeiBoList where com_finish = false";
            MySqlDataReader reader = MySqlHelper.ExecuteReader(sql, null);

            while (reader.Read()) {
               // if (!reader.IsDBNull(4))
               //     continue;
                string url = reader.GetString(2);
                urlQueue.Enqueue(url);
            }
            reader.Close();
            urlQueue.Enqueue("exit");



        }
    }
}
