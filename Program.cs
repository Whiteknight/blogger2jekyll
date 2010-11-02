using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;

namespace Blogger2Jekyll
{
    class Program
    {
        private const string LOG_FILE = "blogger2jekyll.txt";
        private const string OUTPUT_DIR = "_posts";
        private const string DRAFT_DIR = "_drafts";
        private const string LINK_BASE = "/";
        private const string CAT_BASE = "/categories";
        private const string OLDBLOG_BASE = "http://wknight8111.blogspot.com/";

        static void Main(string[] args)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(args[0]);
            Log("Converting blog from dump file " + args[0] + " at " + DateTime.Now.ToString());
            XmlNode root = doc.DocumentElement;
            Dictionary<string, Post> posts = new Dictionary<string, Post>();
            Dictionary<string, Comment> comments = new Dictionary<string, Comment>();
            List<string> pages = new List<string>();

            Log("Step 1: Reading entries");
            foreach (XmlNode entry in root.ChildNodes) {
                if (entry.LocalName != "entry")
                    continue;
                if (entry["thr:in-reply-to"] != null) {
                    Comment c = new Comment(entry);
                    string id = c.PostId;
                    if (posts.ContainsKey(id)) {
                        posts[id].AddComment(c);
                        comments.Add(c.CommentId, c);
                    } else
                        Log("No post associated with comment " + c.ToString());
                } else {
                    Post p = new Post(entry);
                    if (!p.ValidPost)
                        continue;
                    posts.Add(p.Id, p);
                    pages.Add(p.FileName);
                }
            }
            Log("\tNumber of entries: " + posts.Count.ToString());

            Log("Step 2: Updating links");
            LinkMapper mapper = new LinkMapper(LINK_BASE, CAT_BASE, pages.ToArray());
            foreach (KeyValuePair<string, Post> kvp in posts)
                kvp.Value.UpdateAllInternalLinks(OLDBLOG_BASE, mapper.Replacer);

            Log("Step 3: Writing jekyll files");
            if (!Directory.Exists(OUTPUT_DIR))
                Directory.CreateDirectory(OUTPUT_DIR);
            if (!Directory.Exists(DRAFT_DIR))
                Directory.CreateDirectory(DRAFT_DIR);
            foreach (KeyValuePair<string, Post> kvp in posts)
                kvp.Value.WriteFile(OUTPUT_DIR, DRAFT_DIR);
        }

        public static void Log(string msg)
        {
            //Console.WriteLine(msg);
            File.AppendAllText(LOG_FILE, msg + "\n");
        }
    }
}
