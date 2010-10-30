using System;
using System.Xml;

namespace BlogFormatter
{
    class Author
    {
        public string Name { get; set; }
        public string Uri { get; set; }
        public string Email { get; set; }

        public Author(XmlNode node)
        {
            this.Name = node["name"].InnerText;
            XmlNode uri = node["uri"];
            if (uri != null)
                this.Uri = uri.InnerText;

            this.Email = node["email"].InnerText;
        }

        public string SignatureHtml()
        {
            string sig = "<span class='blogger-author-sig'>";
            if (this.Uri != null)
                sig += "<a class='blogger-author-uri' href='" + this.Uri + "'>" + this.Name + "</a>\n";
            else
                sig += this.Name;
            return sig;
        }
    }
}
