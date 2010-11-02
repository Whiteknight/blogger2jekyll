using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;

namespace Blogger2Jekyll
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(args[0]);
            XmlNode root = doc.DocumentElement;
            Dictionary<string, Post> posts = new Dictionary<string, Post>();
            Dictionary<string, Comment> comments = new Dictionary<string, Comment>();
            List<string> pages = new List<string>();
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
                        Console.WriteLine("No post associated with comment " + c.ToString());
                } else {
                    Post p = new Post(entry);
                    if (!p.ValidPost)
                        continue;
                    posts.Add(p.Id, p);
                    pages.Add(p.FileName);
                }
            }


            Console.WriteLine("Updating Links:");
            LinkMapper mapper = new LinkMapper("/", pages.ToArray());
            foreach (KeyValuePair<string, Post> kvp in posts)
                kvp.Value.UpdateAllInternalLinks(mapper.Replacer);

            Console.WriteLine("Entries: " + posts.Count.ToString());
            Console.WriteLine("Generated Posts:");
            if (!Directory.Exists("_posts"))
                Directory.CreateDirectory("_posts");
            if (!Directory.Exists("_posts"))
                Directory.CreateDirectory("_posts");
            //foreach (KeyValuePair<string, Post> kvp in posts)
            //    kvp.Value.WriteFile("_posts/", "_drafts/");
            /*
            {
                string links = kvp.Value.GetAllInternalLinks();
                if (links != null)
                    Console.WriteLine(links);
            }
            */
        }
    }
}
