using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace testUKAD
{
    class Program
    {
        // Собираем все ссылки с 1 html страницы в список ссылок
        public static void PickLinksFirstPage(string address, string html, out List<string> url)
        {
            url = new List<string>();

            var regexpATag = new Regex(@"<a[^<>]*>[^<]*<\/a>", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var regexpHref = new Regex(@"href\s*=\s*[""'](.*?)[""']", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var matches = regexpATag.Matches(html);

            var links = new List<string>();

            foreach (Match match in matches)
            {
                var link = regexpHref.Match(match.Value);
                if (link.Success) links.Add(link.Groups[1].Value);
            };

            links = links.Distinct().OrderBy(el => el).ToList();

            foreach (var item in links)
            {
                url.Add($@"{address}{item}");
            }
        }
        // Собираем все ссылки с html страниц в единый список ссылок
        public static void PickLinks(string html, out List<string> links)
        {
            links = new List<string>();

            var regexpATag = new Regex(@"<a[^<>]*>[^<]*<\/a>", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var regexpHref = new Regex(@"href\s*=\s*[""'](.*?)[""']", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var matches = regexpATag.Matches(html);

            foreach (Match match in matches)
            {
                var link = regexpHref.Match(match.Value);
                if (link.Success) links.Add(link.Groups[1].Value);
            };

            links = links.Distinct().OrderBy(el => el).ToList();
        }
        public static void CleanLink(string address, List<string> links, out List<string> allCorr)
        {
            List<string> linkss = new List<string>();
            List<string> correctL = new List<string>();
            List<string> correct = new List<string>();
            List<string> correctLinks = new List<string>();
            allCorr = new List<string>();
            foreach (var item in links)
            {
                if (!item.Contains($"{address}") && !item.Contains("https://") || item.Contains("#"))
                {
                    linkss.Add($"{address}{item}");
                }
                else if (item.Contains("https://"))
                {
                    linkss.Add($"{item}");
                }
            }
           
            foreach (var item in linkss)
            {
                if (item.Substring(8).Contains($"{address}"))
                {
                    correctL.Add($"https://{item.Substring(8).Replace($"{address}","")}");
                }
                if (item.Substring(8).Contains("//"))
                {
                    correctL.Add($"https://{item.Substring(8).Replace("//", "/")}");
                }
                else
                {
                    correctL.Add(item);
                }
            }
            foreach (var item in correctL)
            {
                if (item.Substring(8).Contains("/ /"))
                {
                    correct.Add($"https://{item.Substring(8).Replace("/ /", "/")}");
                }
                else
                {
                    correct.Add(item);
                }
            }
            string nameSite = address.Substring(8);
            foreach (var item in correct)
            {
                if (item.Contains("///"))
                {
                    correctLinks.Add(item.Replace("///", "//"));
                }
                if (item.Contains($"https:/{nameSite}"))
                {
                   correctLinks.Add($"{address}{item.Substring(address.Length).Replace($"https:/{nameSite}", "")}");
                }
                else
                {
                    correctLinks.Add(item);
                }
                allCorr = correctLinks.Distinct().ToList();
            }
        }
        // парсим карту сайта выход список ссылок из карты 
        public static void MapPageLinks(string responseRobot, string mainAdress,out List<string> links)
        {
            string nameMapSite = String.Empty;
            links = new List<string>();
            char[] delimiterChars = { ' ', '*', '\t', '\n', '\r' };

            string[] words = responseRobot.Split(delimiterChars);
      
            foreach (var item in words)
            {
                if (item.Contains("sitemap"))
                {
                    nameMapSite = item;

                    XmlTextReader readerPage = new XmlTextReader(nameMapSite);
                    while (readerPage.Read())
                    {
                        switch (readerPage.NodeType)
                        {
                            case XmlNodeType.Text:
                                if (readerPage.Value.Contains($"{mainAdress}"))
                                {
                                    links.Add(readerPage.Value);
                                }
                                break;
                        }
                    }
                }
            } 
        }
        public static void TimingPage(string address, out string timing)
        {
            string html = String.Empty;

            timing = String.Empty;

            Stopwatch sw = new Stopwatch();

            sw.Start();

            try
            {
                WebRequest request = HttpWebRequest.Create(address);
                WebResponse response = request.GetResponse();
                HttpWebResponse resHttp = (HttpWebResponse)response;
                Stream data = response.GetResponseStream();
                if (!((int)resHttp.StatusCode > 226 ||
                     resHttp.StatusCode == HttpStatusCode.NotFound))
                {
                    using (StreamReader streamReader = new StreamReader(data))
                    {
                        html = streamReader.ReadToEnd();
                    }
                }
            }
            catch (WebException wex)
            {
               //  Console.WriteLine(" WEX - " + wex.Message); 
            }
            catch (Exception ex)
            {
               //  Console.WriteLine(" EX - " + ex.Message); 
            }

            sw.Stop();

            TimeSpan timeToLoad = sw.Elapsed;

            timing = $"{timeToLoad.Milliseconds}";

        }
        public static void CreateDictionary(List<string> resultLinksSite)
        {
            List<string> links = resultLinksSite.Distinct().ToList();
          
            Dictionary<string, int> timingPages = new Dictionary<string, int>();

            for (int i = 0; i < links.Count; i++)
            {
                TimingPage(links[i], out string timing);

                timingPages.Add($"{links[i]}", Convert.ToInt32(timing, 10)); 
                
            }
            timingPages.OrderBy(x => x.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

            foreach (var item in timingPages.OrderBy(k => k.Value))
            {
                Console.WriteLine($"{item.Key} - {item.Value} ms.");
            }

        }

        static readonly HttpClient client = new HttpClient();
        static async Task Main()
        {
            string address = String.Empty;
            string nameMapSite = String.Empty;
            string nameRobotTxt = String.Empty;

            Console.WriteLine("Input URL :");

            address = Console.ReadLine();

            nameRobotTxt = $@"{address}/robots.txt";

            List<string> linkPage = new List<string>();
            List<string> allSiteLinks = new List<string>();
            List<string> allLinksMap = new List<string>();

            try
            {
                string responseBody = await client.GetStringAsync($"{address}");
            
                // Собираем все ссылки с html страницы в список ссылок
                PickLinksFirstPage(address, responseBody, out List<string> url);

                int countSymbol = address.Length;
              
                url.ForEach(l => linkPage.Add(l.Substring(countSymbol)));// обрезали ссылку на главный адрес получили чистый адрес
          
                CleanLink(address, linkPage, out List<string> correctLinks);// все ссылки с первой страницы

                CreateDictionary(correctLinks); // проверили время звпроса с первой страницы

                try
                {
                    //Собираем ссылки на каждой найденной странице
                    string responseChildPage = string.Empty;
                    foreach (var item in correctLinks)
                    {
                        if (!item.Equals($"{address}/#"))
                        {
                            responseChildPage = await client.GetStringAsync($"{item}");
                            // Собираем все ссылки с html страницы в список ссылок
                            PickLinks(responseChildPage, out List<string> linkss);

                            if (linkss.Count == 0)
                            {
                                break;
                            }

                            foreach (var l in linkss)
                            {
                                if (l.Equals($"{address}/#"))
                                {
                                    break;
                                }
                                if (!l.Contains($"{address}"))
                                {
                                    allSiteLinks.Add($"{address}{l}");
                                }
                                else
                                {
                                    allSiteLinks.Add(l);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                }
 
                CleanLink(address, allSiteLinks, out List<string> correctLinksD);
         
                string responseRobot = await client.GetStringAsync($"{nameRobotTxt}");

                    MapPageLinks(responseRobot, address, out List<string> links);

                    XmlDocument xDoc = new XmlDocument();

                    foreach (var item in links)
                    {
                        xDoc.Load($"{item}");

                        XmlElement xRoot = xDoc.DocumentElement;
                        if (xRoot != null)
                        {
                            foreach (XmlElement xnode in xRoot)
                            {
                                XmlNode attr = xnode.Attributes.GetNamedItem("url");

                                foreach (XmlNode childnode in xnode.ChildNodes)
                                {
                                    if (childnode.Name == "loc")
                                    {
                                        allLinksMap.Add($"{childnode.InnerText}");

                                    }
                                }
                            }
                        }
                    }
             
                List<string> mapLinks = allLinksMap.Distinct().ToList();
             
                Console.WriteLine($"URL-addresses site   = {correctLinksD.Count()}");
                Console.WriteLine($"  - - - - - - - - - - - - - ");

                Console.WriteLine($"URL-addresses sitemap = {mapLinks.Count()}");
                Console.WriteLine($"  - - - - - - - - - - - - - ");
                // соединяем списки
                List<string> SiteAndMapLinks = correctLinksD.Concat(mapLinks).ToList();

                Console.WriteLine($"URL-addresses site + sitemap. = {SiteAndMapLinks.Count()}");
                Console.WriteLine($"  - - - - - - - - - - - - - ");

                var matchfound = mapLinks.Any(x => correctLinksD.Contains(x));

                if (matchfound)
                {
                    List<string> result = mapLinks.Where(a => correctLinksD.Contains(a)).ToList();

                    mapLinks.RemoveAll(i => result.Contains(i));

                    Console.WriteLine($" URL  SITEMAP.XML - {mapLinks.Count}");
                    CreateDictionary(mapLinks);
                    Console.WriteLine($"  - - - - - - - - - - - - - ");

                    Console.WriteLine($" URL  WEB SITE - {correctLinksD.Count}");
                    correctLinksD.RemoveAll(i => result.Contains(i));
                    CreateDictionary(correctLinksD);
                }
                else
                {
                    Console.WriteLine($" URL  SITEMAP.XML - {mapLinks.Count}");
                    CreateDictionary(mapLinks);
                    Console.WriteLine($"  - - - - - - - - - - - - - ");

                    Console.WriteLine($" URL  WEB SITE - {correctLinksD.Count}");
                    CreateDictionary(correctLinksD);
                }
               
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

        }
    }
}
