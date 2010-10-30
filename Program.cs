using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace BlogFormatter
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(args[0]);
            XmlNode root = doc.DocumentElement;
            Dictionary<string, Post> posts = new Dictionary<string, Post>();
            foreach (XmlNode entry in root.ChildNodes) {
                if (entry.LocalName != "entry")
                    continue;
                if (entry["thr:in-reply-to"] != null) {
                    Comment c = new Comment(entry);
                    string id = c.PostId;
                    if (posts.ContainsKey(id))
                        posts[id].AddComment(c);
                    else
                        Console.WriteLine("No post associated with comment " + c.ToString());
                }
                else {
                    Post p = new Post(entry);
                    if (!p.ValidPost)
                        continue;
                    posts.Add(p.Id, p);
                }
            }

            Console.WriteLine("Entries: " + posts.Count.ToString());

            foreach (KeyValuePair<string, Post> kvp in posts)
                kvp.Value.WriteFile("entries/");

            Console.ReadKey();
        }

    }
}
