using System;
using System.Web;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

namespace Blogger2Jekyll
{
    class Comment
    {
        public Author Author { get; private set; }
        public DateTime Posted { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string PostId { get; set; }
        public string CommentId { get; set; }

        public Comment(XmlNode node)
        {
            this.Author = new Author(node["author"]);
            string time = node["published"].InnerText.Replace('T', ' ');
            this.Posted = DateTime.Parse(time);
            this.Title = node["title"].InnerText;
            this.Text = HttpUtility.HtmlDecode(node["content"].InnerText);
            this.PostId = this.GetCommentPostIDNumber(node);
            this.CommentId = this.GetCommentId(node);
        }

        public override string ToString()
        {
            return this.Posted.ToString() + "> " + this.Title + "\n";
        }

        // tag:blogger.com,1999:blog-2892921175237778338.post-5112767377116292394
        const string match = @"tag:blogger\.com,\d{4}:blog-\d+\.post-(\d+)";

        private string GetCommentId(XmlNode node)
        {
            string raw = node["id"].InnerText;
            Match m = Regex.Match(raw, match);
            string id = m.Groups[1].Captures[0].Value;
            return id;
        }

        private string GetCommentPostIDNumber(XmlNode comment)
        {
            XmlNode post = comment["thr:in-reply-to"];
            string raw = post.Attributes["ref"].InnerText;
            Match m = Regex.Match(raw, match);
            string id = m.Groups[1].Captures[0].Value;
            return id;
        }

        public string OutputHtml()
        {
            return @"
<div class='blogger-comment-div'>
    <a name='" + this.CommentId.ToString() + @"'></a>
    <p class='blogger-comment-body'>
        " + this.Text + @"
    </p>
    <div class='blogger-comment-author-div'>
        " + this.Author.SignatureHtml() + @"
        <span class='blogger-comment-datestamp'>
            <a class='blogger-comment-link' href='{{ post.url }}#" + this.CommentId.ToString() + @"'>
                " + this.Posted.ToString() + @"
            </a>
        </span>
    </div>
</div>
";
        }
    }
}
