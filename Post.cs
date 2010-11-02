using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;
using System.IO;

namespace Blogger2Jekyll
{
    class Post
    {
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string FileName { get; private set; }
        public string Text { get; private set; }
        public bool ValidPost { get; private set; }
        public List<string> Categories = new List<string>();
        public List<Comment> Comments = new List<Comment>();
        public DateTime Posted { get; private set; }
        private bool draft;

        public Post(XmlNode node)
        {
            this.GetPostIDNumber(node);
            if (!this.ValidPost)
                return;
            this.Posted = this.GetPostDate(node);
            this.FileName = this.GetPostFileName(node);
            this.Text = this.GetPageText(node);
            this.Title = this.GetPostDisplayTitle(node);
            this.GetCategories(node);
            XmlNode control = node["app:control"];
            if (control != null && control["app:draft"] != null)
                this.draft = control["app:draft"].InnerText == "yes";
        }

        private string GetPageText(XmlNode node)
        {
            string html = HttpUtility.HtmlDecode(node["content"].InnerText);
            return html.Replace("{", "&#123;").Replace("}", "&#125;");
        }

        public void GetCategories(XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes) {
                if (child.LocalName != "category")
                    continue;
                if (child.Attributes["scheme"].InnerText == "http://www.blogger.com/atom/ns#")
                    this.Categories.Add(child.Attributes["term"].InnerText);
            }
        }

        public override string ToString()
        {
            string rep = this.FileName + "\n";
            foreach (Comment c in this.Comments)
                rep = rep + "\t" + c.ToString();
            return rep;
        }

        public void WriteFile(string pubpath, string draftpath)
        {
            string path = this.draft ? draftpath : pubpath;
            string text = this.JekyllYamlFrontMatter() + this.Text;
            if (this.Comments.Count > 0) {
                text += @"
                <div class='old-blogger-comments'>
                    <h2 class='old-comment-header'>Comments</h3>
                ";
                this.Comments.Sort(delegate(Comment c1, Comment c2)
                {
                    return c1.Posted.CompareTo(c2.Posted);
                });
                foreach (Comment c in this.Comments)
                    text += c.OutputHtml();
                text += "</div>\n";
            }
            File.WriteAllText(path + "/" + this.FileName, text);
            Console.Write(path + this.ToString());
        }

        private string JekyllYamlFrontMatter()
        {
            return @"---
layout: bloggerpost
title: " + this.Title + @"
publish: " + (this.draft ? "false" : "true") + @"
categories: " + this.FormatYamlCategories() + @"
---

";
        }

        private string FormatYamlCategories()
        {
            return "[" + String.Join(", ", this.Categories.ToArray()) + "]";
        }

        public void AddComment(Comment c)
        {
            this.Comments.Add(c);
        }

        // tag:blogger.com,1999:blog-2892921175237778338.post-5112767377116292394
        const string match = @"tag:blogger\.com,\d{4}:blog-\d+\.post-(\d+)";

        private void GetPostIDNumber(XmlNode entry)
        {
            XmlNode title = entry["id"];
            string raw = title.InnerText;
            if (!Regex.IsMatch(raw, match)) {
                this.ValidPost = false;
                return;
            }
            Match m = Regex.Match(raw, match);
            this.Id = m.Groups[1].Captures[0].Value;
            this.ValidPost = true;
        }

        private DateTime GetPostDate(XmlNode node)
        {
            string datestr = node["published"].InnerText.Replace('T', ' ');
            DateTime time = DateTime.Parse(datestr);
            return time;
        }

        private string GetPostFileName(XmlNode entry)
        {
            XmlNode title = entry["title"];
            string raw = title.InnerText.ToLower();
            string clean = KillNonTitleChars(raw);
            clean = clean.Replace(' ', '_');
            string datestamp = this.GetDateStamp();
            return datestamp + clean + ".html";
        }

        private string GetDateStamp()
        {
            return String.Format("{0:yyyy-MM-dd-}", this.Posted);
        }

        private string KillNonTitleChars(string s)
        {
            string evil = "!@#$%^*()[]{};:\"'<>,.?/-_=+";
            for (int i = 0; i < evil.Length; i++)
                s = s.Replace(evil.Substring(i, 1), "");
            return s;
        }

        private string GetPostDisplayTitle(XmlNode node)
        {
            string raw = node["title"].InnerText;
            return raw.Replace(":", "&#58;");
        }

        public void UpdateAllInternalLinks(string oldblogbase, MatchEvaluator replacer)
        {
            Program.Log("\tPost: " + this.FileName);
            Regex rx = new Regex(oldblogbase + "([^\"]+)");
            this.Text = rx.Replace(this.Text, replacer);
        }
    }
}
