using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EasyHttp;
using EasyHttp.Http;
using HapCss;
using HtmlAgilityPack;
namespace _91pron
{
    class Program
    {
         static void Main(string[] args)
         {
             var page = 1;
            //1  
            var baseurl = "http://91porn.com/view_video.php?viewkey={0}";
            var pageurl = $"http://91porn.com/v.php?next=watch&page={0}";
            while (page<=1000)
            {
                try
                {
                    var listHttpResponse = Get(string.Format(pageurl,page), "http://91porn.com");
                    var doc = new HtmlDocument();
                    doc.LoadHtml(listHttpResponse.RawText);
                    var listchannel= doc.QuerySelectorAll(".listchannel div:first-child a");
                    if (listchannel.Any())
                    {
                        var list = listchannel.ToList();
                        foreach (var channel in list)
                        {
                            try
                            {
                                //var a = channel.QuerySelector("a");
                                var href = channel.Attributes["href"].Value;
                                //获取webkey
                                var viewkey = href.Split('&')[0].Substring(href.IndexOf("viewkey=", StringComparison.Ordinal) + 8);
                                if (!string.IsNullOrEmpty(viewkey))
                                {
                                    var detailurl = string.Format(baseurl, viewkey);
                                    var detailHttpRespones = Get(detailurl, pageurl, true);
                                    doc = new HtmlDocument();
                                    doc.LoadHtml(detailHttpRespones.RawText);
                                    var title = doc.QuerySelector("#viewvideo-title").InnerText.Replace("\n", "").Trim();
                                    Console.WriteLine($"开始下载{title}");
                                    var vid = doc.QuerySelector("#vid");
                                    var imageurl = vid.Attributes["poster"].Value;
                                    var videourl = vid.ChildNodes["source"].Attributes["src"].Value;
                                    var temp = "download/";
                                    if (!Directory.Exists(temp + viewkey))
                                    {
                                        Directory.CreateDirectory(temp + viewkey);
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                    DownloadFile(imageurl, $"{temp}{viewkey}/1.png");
                                    DownloadFile(videourl, $"{temp}{viewkey}/1.mp4");
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                               
                            }
                           
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                   
                }
                page++;


            }
         }

        private static void DownloadFile(string url, string filename)
        {
            WebClient webClient =new WebClient();
            webClient.Headers["User_Agent"] =
                "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";
            webClient.Headers["Referer"] = "http://91porn.com";
            webClient.DownloadFile(url,filename);
        }

        private static HttpResponse Get(string url,string referer,bool iscontent=false)
        {
            HttpClient httpClient =new HttpClient();
           
            httpClient.Request.Referer= referer;
            httpClient.Request.UserAgent=
             "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";
            httpClient.Request.AddExtraHeader("Session_Language", "cn_CN");
            httpClient.Request.AddExtraHeader("Accept-Language", "zh-CN,zh;q=0.9");
            if (iscontent)
            {
                var temp = GetRadomIp();
                httpClient.Request.AddExtraHeader("X-Forwarded-For", temp);
                httpClient.Request.ContentType= "multipart/form-data";
               
            }
            return httpClient.Get(url);
        }

        private static string GetRadomIp()
        {
            return
                $"{new Random(Guid.NewGuid().GetHashCode()).Next(1, 255)}.{new Random(Guid.NewGuid().GetHashCode()).Next(1, 255)}.{new Random(Guid.NewGuid().GetHashCode()).Next(1, 255)}.{new Random(Guid.NewGuid().GetHashCode()).Next(1, 255)}";
        }

        
    }
}
