using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReadMangaDownloader
{
    public class MangaInfo
    {
        public string Name { get; private set; }
        public string Names { get; private set; }
        public string Description { get; private set; }

        private string _url;
        public string Url 
        { 
            get => _url;
            private set 
            {
                if (Regex.IsMatch(value, @"https://readmanga.live/.*"))
                    _url = value;
                else
                    throw new ArgumentException("Некоректый "+ nameof(Url));
            }
        }
        public List<MangaChapter> Chapters { get; set; }

        private WebClient _client = new WebClient() { Encoding = Encoding.UTF8};

        private string _urlString;

        public MangaInfo(string url)
        {
            Url = url;
            try
            {
                _urlString = _client.DownloadString(Url);  
            }
            catch
            {
                throw new ArgumentException("Неккоректный url адресс");
            }
            GetMangaInfo();
        }

        private void GetMangaInfo()
        {
            int indexOfBeginTableChapters;
            int indexOfEndTableChapters;
            if ((indexOfBeginTableChapters = _urlString.IndexOf("<div class=\"leftContent\"")) == -1)
                throw new ArgumentException("Неккоректный url адресс");
            indexOfEndTableChapters = _urlString.IndexOf("</div>", indexOfBeginTableChapters);

            string table_chapters = _urlString.Substring(indexOfBeginTableChapters, indexOfEndTableChapters - indexOfBeginTableChapters);

            string type_title = Regex.Match(table_chapters, @"<h1 class=.names.>([\w\W]*)<span class=.name.").Groups[1].Value;
            type_title = type_title.Replace("\n", "").Replace(" ", "");
            string name_title = Regex.Match(table_chapters, @"<span class=.name.>([\w\W]*)<\/span>").Groups[1].Value;
            if (name_title.IndexOf("eng-name") != -1)
            {
                var index = name_title.IndexOf("</span>");
                name_title = name_title.Substring(0,index);
            }
            string description_title = Regex.Match(table_chapters, @"<meta itemprop=.description. content=.(.*).>").Groups[1].Value;
            
            Name = name_title;
            Names = type_title;
            Description = description_title;
        }

        public void GetChapters()
        {
            if (string.IsNullOrWhiteSpace(Url))
                throw new ArgumentException("Неккоректный url адресс");

            Chapters= new List<MangaChapter>();

            int indexOfBeginTableChapters;
            int indexOfEndTableChapters;
            if ((indexOfBeginTableChapters = _urlString.IndexOf("table table-hover")) == -1)
                throw new ArgumentException("Неккоректный url адресс");
            indexOfEndTableChapters = _urlString.IndexOf("</table>", indexOfBeginTableChapters);

            string table_chapters = _urlString.Substring(indexOfBeginTableChapters, indexOfEndTableChapters - indexOfBeginTableChapters);

            int startIndexChapter = table_chapters.IndexOf("</tr>") + 6;

            string table_chapter;
            (string url, string name) chapter;

            while (true)
            {
                if ((indexOfBeginTableChapters = table_chapters.IndexOf("<tr>", startIndexChapter)) == -1)
                    break;
                indexOfEndTableChapters = table_chapters.IndexOf("</tr>", startIndexChapter);
                startIndexChapter = indexOfEndTableChapters + 6;
                table_chapter = table_chapters.Substring(indexOfBeginTableChapters, indexOfEndTableChapters - indexOfBeginTableChapters);

                var r = Regex.Match(table_chapter, @"<a href=.(.*). title.*>([\w\W]*)\n[\w\W]*<\/a>");

                chapter.url = $"https://readmanga.live{r.Groups[1].Value}";
                chapter.name = Regex.Replace(r.Groups[2].Value, @"[^A-Za-zА-Яа-я0-9-_'()#$&!@^]+", " ").Trim();
                Chapters.Add(new MangaChapter(chapter.url, chapter.name));
            }

            Chapters.Reverse();
        }

        public IEnumerable FromToChapters(int from, int to)
        {
            for (int i = from; i <=to; i++)
            {
                yield return Chapters[i];
            }
        }

        public void SaveFromTo(int from, int to, string directory, int tryDownloadCounts = 1)
        {
            if (directory.Length!=0)
                if (directory[directory.Length - 1] != '\\')
                    directory += '\\';
            directory += $"{Name}\\";

            foreach (MangaChapter chapter in FromToChapters(from, to))
                chapter.Save(directory, tryDownloadCounts);
        }

        public void Save(string directory, int tryDownloadCounts = 1)
        {
            SaveFromTo(0, Chapters.Count - 1, directory, tryDownloadCounts);
        }
    }


    public class MangaChapter
    {
        public string Name { get; private set; }
        public string Url { get; private set; }

        public List<ChapterPage> Pages { get; private set; }

        public MangaChapter(string url, string name)
        {
            Url = url;
            Name = name;
        }

        public void InitPages()
        {
            Pages = new List<ChapterPage>();
            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;

            string url_string = client.DownloadString(Url);
            if (url_string.IndexOf("Если вам больше 18 лет") != -1)
            {
                Url += "?mtr=1";
                url_string = client.DownloadString(Url);
            }

            string stringURLs = Regex.Match(url_string, @"\[(\[.*\])\]").Groups[1].Value;
            if (string.IsNullOrEmpty(stringURLs))
                throw new ArgumentException("Неккоректный url адресс");
            var r = Regex.Matches(stringURLs, "\\[[^\\[]*\\]");

            Match pageUrlMatch;
            foreach (Match item in r)
            {
                pageUrlMatch = Regex.Match(item.Value, "'(.*)','.*',\"((.*\\.\\w{1,8})(\\?t=.*)?)\"");
                Pages.Add(new ChapterPage(pageUrlMatch.Groups[1].Value + pageUrlMatch.Groups[2].Value, pageUrlMatch.Groups[3].Value.Replace('/', '-')));
            }
        }

        public void Save(string directory, int tryDownloadCounts = 1)
        {
            if (Pages == null)
                InitPages();

            if (directory[directory.Length - 1] != '\\')
                directory += '\\';
            directory += $"{Name}\\";

            foreach (ChapterPage page in Pages)
                page.Save(directory, tryDownloadCounts);
        }
    }

    public class ChapterPage
    {
        public string Name { get; private set; }
        public string Url { get; private set; }

        public ChapterPage(string url, string name)
        {
            Url = url;
            Name = name;
        }

        public void Save(string directory, int tryDownloadCounts=1)
        {
            WebClient client = new WebClient();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (directory[directory.Length - 1] != '\\')
                directory += '\\';

            byte tryDownloadCount =1;
            bool isDownload = false;

            while (!isDownload)
            {
                try
                {
                    client.DownloadFile(Url, $@"{directory}{Name}");
                    isDownload = true;
                }
                catch
                {
                   
                    if (++tryDownloadCount >= tryDownloadCounts)
                        break;
                }
            }
        }
    }
}
