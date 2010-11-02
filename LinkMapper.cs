using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Blogger2Jekyll
{
    class LinkMapper
    {
        // Map year -> month -> filenames
        private Dictionary<string, Dictionary<string, List<string>>> postmap;
        private string Path;
        private string CommentPath;
        private PermalinkBuilder Linker;
        public MatchEvaluator Replacer;

        public LinkMapper(string path, string cpath, string format, string[] pages)
        {
            this.Linker = new PermalinkBuilder(path, format);
            this.Path = path;
            this.CommentPath = cpath;
            this.postmap = new Dictionary<string, Dictionary<string, List<string>>>();
            this.Replacer = new MatchEvaluator(this.ResolveLink);
            foreach (string page in pages)
                this.AddPageToMap(page);
        }

        private void AddPageToMap(string page)
        {
            string[] parts = page.Split('-');
            if (!postmap.ContainsKey(parts[0]))
                postmap.Add(parts[0], new Dictionary<string, List<string>>());
            this.AddPageToSubMap(postmap[parts[0]], parts[1], page);
        }

        private void AddPageToSubMap(Dictionary<string, List<string>> map, string key, string file)
        {
            if (!map.ContainsKey(key))
                map.Add(key, new List<string>());
            map[key].Add(file);
        }

        // Levenshtein distance
        private static int CalculateDistance(string first, string second)
        {
            int n = first.Length;
            int m = second.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
                return m;
            if (m == 0)
                return n;

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++) {
                for (int j = 1; j <= m; j++) {
                    int cost = (second[j - 1] == first[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        public string ResolveLink(Match m)
        {
            string link = m.Groups[1].Value;
            string result = null;
            if (Regex.IsMatch(link, @"^\d{4}/\d{2}/.+#c"))
                result = this.ResolveCommentLink(link);
            else if (Regex.IsMatch(link, @"^\d{4}/\d{2}/"))
                result = this.ResolvePostLink(link);
            else if (link.Substring(0, 12) == "search/label")
                result = this.ResolveCategoryLink(link);
            else
                Program.Log("\t\t" + link + " -> Not a valid-looking link");
            if (result != null)
                Program.Log("\t\t" + link + " -> " + result);
            else
                Program.Log("\t\t" + link + " -> Could not resolve");
            return result;
        }

        private string GetPermalink(string file)
        {
            string[] parts = file.Split('-');
            return this.Linker.CreatePermalink(parts[0], parts[1], parts[2], parts[3]);
        }

        private string ResolvePostLink(string link)
        {
            string[] parts = link.Split('/');
            string[] candidates = this.GetCandidates(parts[0], parts[1]);
            Dictionary<string, int> scores = new Dictionary<string, int>();
            foreach (string candidate in candidates)
                scores.Add(candidate, CalculateDistance(parts[2], candidate.Split('-')[3]));
            int min = 1000;
            string best = "";
            foreach (KeyValuePair<string, int> score in scores) {
                if (score.Value < min) {
                    min = score.Value;
                    best = score.Key;
                }
            }
            if (min == 1000) {
                Program.Log("\t\t\tNo candidates");
                return null;
            }
            return this.GetPermalink(best);
        }

        private string[] GetCandidates(string year, string month)
        {
            if (!this.postmap.ContainsKey(year))
                return null;
            Dictionary<string, List<string>> yearposts = this.postmap[year];
            if (!yearposts.ContainsKey(month))
                return null;
            return yearposts[month].ToArray();
        }

        private string ResolveCommentLink(string link)
        {
            string[] parts = link.Split('?');
            string post = this.ResolvePostLink(parts[0]);
            string[] args = parts[1].Split('#');

            return post + "#" + args[1].Substring(1);
        }

        private string ResolveCategoryLink(string link)
        {
            string[] parts = link.Split('/');
            return this.CommentPath + "/" + parts[2] + ".html";
        }

        private class PermalinkBuilder
        {
            private string format;
            private string urlbase;

            public PermalinkBuilder(string urlbase, string format)
            {
                this.format = format;
                this.urlbase = urlbase;
            }

            public string CreatePermalink(string year, string month, string day, string title)
            {
                string s = format.Replace(":year", year);
                s = s.Replace(":month", month);
                s = s.Replace(":day", day);
                return this.urlbase + s.Replace(":title", title);
            }
        }
    }
}
