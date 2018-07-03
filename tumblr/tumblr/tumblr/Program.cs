using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace tumblr
{
    class Program
    {
        private static int pagesize = 50;
        private static string path = "temp";
        static void Main(string[] args)
        {
            Cache.BlogsQueue.Enqueue("");
            Task.Factory.StartNew(ThreadFindBlog);
            Task.Factory.StartNew(ThreadDownload);

            Console.Read();
        }


        private  static void ThreadFindBlog()
        {
            while (true)
            {
                var name = "";
                Cache.BlogsQueue.TryDequeue(out name);
               // if (Cache.BlogsQueue.Count > 1000) { Thread.Sleep(60000);}
                if (!string.IsNullOrEmpty(name))
                {
                    Console.WriteLine(name);
                    
                    GetBlogAndPost(name, "video");
                    GetBlogAndPost(name,"photo");
                }
                else
                {
                    Thread.Sleep(10000);
                }
            }
        }

        private static void ThreadDownload()
        {
           
            for (int i = 0; i < 10; i++)
            {
                Thread thread = new Thread(Down);
                thread.Start();
            }
              
            
        }

        private static void Down()
        {
            while (true)
            {
                var url = "";
                Cache.DownloadQueue.TryDequeue(out url);
                if (!string.IsNullOrEmpty(url))
                {
                    var temp = url.Split('|');
                    var basepath = path + Path.DirectorySeparatorChar + temp[0] + Path.DirectorySeparatorChar + temp[1];
                    if (!Directory.Exists(basepath))
                    {
                        Directory.CreateDirectory(basepath);
                    }

                    var filename = temp[3] == "video" ? temp[2].Substring(temp[2].LastIndexOf("/") + 1) + ".mp4" : temp[2]
                        .Substring(temp[2].LastIndexOf("/") + 1);
                    if (File.Exists(basepath + Path.DirectorySeparatorChar + filename))
                    {
                        return;
                    }
                    DownloadFile(temp[2], basepath + Path.DirectorySeparatorChar + filename);
                }
                else
                {
                    Thread.Sleep(10000);
                }
            }
                
        }


        private static void GetBlogAndPost(string name,string type)
        {
            var url = $"https://{name}.tumblr.com/api/read/json?type={type}&start={{0}}&num={pagesize}";
           // Console.WriteLine(url);
            var html = Get(string.Format(url,0));
           // Console.WriteLine(html);
            if(string.IsNullOrEmpty(html)) return;;
            html = html.Replace("var tumblr_api_read =", "").Trim().TrimEnd(';');
            var jobject= JObject.Parse(html);
            var totalcount = jobject["posts-total"].Value<int>();
            if (totalcount==0) return;
            //判断是否往下请求
            var pages = totalcount / pagesize;
            Console.WriteLine($"开始请求{name}_{type}");
            Analyze(name, type, jobject);
            if (totalcount % pagesize > 0)
            {
                pages += 1;
            }

            if(pages==1) return; //第一页已经采集过了
            for (int i = 1; i < pages; i++)
            {
                html = Get(string.Format(url, i* pagesize));
                if(string.IsNullOrEmpty(html))continue;
                html = html.Replace("var tumblr_api_read =", "").Trim().TrimEnd(';');
                Analyze(name, type, JObject.Parse(html));
            }

        }

        private static void Analyze(string name,string type,JObject jObject)
        {
            foreach (var j in jObject["posts"])
            {
                //判断是否本人发帖 如果不是 则加入采集
                if (j["reblogged-from-name"] != null&&j["reblogged-from-name"].Value<string>()!=name) //自己转载自己的不下载
                {
                    
                    Cache.BlogsQueue.Enqueue(j["reblogged-from-name"].Value<string>());
                    Console.WriteLine($"{j["reblogged-from-name"].Value<string>()}加入采集队列");
                }
                var postid = j["id"];
                switch (type)
                {
                    case "video":
                        var video = j["video-player"].Value<string>();
                        video = video.Substring(video.IndexOf("src=")+5);
                        video = video.Substring(0,video.IndexOf("\"")); //这样替换字符串最快了后续优化
                        Cache.DownloadQueue.Enqueue($"{name}|{postid}|{video}|video");
                        break;
                    case "photo":

                        if (j["photos"] != null)
                        {
                            foreach (var photo in j["photos"])
                            {
                                var photoone = photo["photo-url-1280"].Value<string>();
                                Cache.DownloadQueue.Enqueue($"{name}|{postid}|{photoone}|photo");
                            }
                        }
                        else
                        {
                            var photoone = j["photo-url-1280"].Value<string>();
                            Cache.DownloadQueue.Enqueue($"{name}|{postid}|{photoone}|photo");
                        }
                        break;
                    default: continue;
                }
            }
           
        }

        private static string Get(string url)
        {
            WebClient webClient =new WebClient();
            webClient.Headers["User_Agent"] =
                "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";
            //webClient.Headers["Content-Type"] = "application/json;charset=utf-8";
           // webClient.Proxy=new WebProxy("http://127.0.0.1:1080");
           // webClient.Headers["upgrade-insecure-requests"] = "1";
          //  webClient.Headers.Add("Cookie", @"pfg=bca88f5af06600e34134c492b68cb7dda0b5f81ce27135bf78ff9985cc30ab54%23%7B%22eu_resident%22%3A1%2C%22gdpr_is_acceptable_age%22%3A1%2C%22gdpr_consent_core%22%3A1%2C%22gdpr_consent_first_party_ads%22%3A1%2C%22gdpr_consent_third_party_ads%22%3A1%2C%22gdpr_consent_search_history%22%3A1%2C%22exp%22%3A1562140618%2C%22vc%22%3A%22%22%7D%236191825495;");
            // =
            try
            {
                return webClient.DownloadString(url);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "";
            }
           
        }



        private static void DownloadFile(string url, string fileName)
        {
            WebClient webClient =new WebClient();
            webClient.Headers["User_Agent"] =
                "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";
           // webClient.Headers["upgrade-insecure-requests"] = "1";
           // webClient.Headers.Add("Cookie", @"pfg=bca88f5af06600e34134c492b68cb7dda0b5f81ce27135bf78ff9985cc30ab54%23%7B%22eu_resident%22%3A1%2C%22gdpr_is_acceptable_age%22%3A1%2C%22gdpr_consent_core%22%3A1%2C%22gdpr_consent_first_party_ads%22%3A1%2C%22gdpr_consent_third_party_ads%22%3A1%2C%22gdpr_consent_search_history%22%3A1%2C%22exp%22%3A1562140618%2C%22vc%22%3A%22%22%7D%236191825495;");
           // webClient.Proxy = new WebProxy("http://127.0.0.1:1080");
            try
            {
                webClient.DownloadFile(url, fileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
          
        }
    }
}
