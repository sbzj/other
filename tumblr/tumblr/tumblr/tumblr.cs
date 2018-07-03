using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tumblr
{
    public class Tumblr
    {
     


    }



    public class Cache
    {
        public static ConcurrentQueue<string> BlogsQueue = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> DownloadQueue = new ConcurrentQueue<string>();
        public static ConcurrentQueue<Post> PostsQueue = new ConcurrentQueue<Post>();

    }


    public class Blog
    {
        public string name { get; set; }
        public string description { get; set; }
        public string title { get; set; }

        public string avatar { get; set; }
        public int posts_total { get; set; }
    }


    public class Post
    {
        public string id { get; set; }

        public string type { get; set; }

        public string slug { get; set; }

        public long timestamp { get; set; }

        public string title { get; set; }
        public string content { get; set; }
    }
}
