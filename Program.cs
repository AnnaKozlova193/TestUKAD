using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        public static void PickLinks(string address,string html, out List<string> links, out List<string> allSiteMaps)
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

            allSiteMaps = new List<string>();

            foreach (var item in links)
            {
                if (item.Contains("sitemap.xml"))
                {
                    allSiteMaps.Add(item);
                }
            }
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
                if (!(item.Contains("www.")))
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
        public static void MapPageLinks(string name,out List<string> links)
        {
            links = new List<string>();

            char[] delimiterChars = { ' ', '*', '\t', '\n', '\r' };

            string[] words = name.Split(delimiterChars);
            
            XmlDocument doc = new XmlDocument();
            
            try
            {
                WebRequest request = HttpWebRequest.Create(name);
                WebResponse response = request.GetResponse();
                HttpWebResponse resHttp = (HttpWebResponse)response;
                Stream data = response.GetResponseStream();
                if (!((int)resHttp.StatusCode > 226 ||
                     resHttp.StatusCode == HttpStatusCode.NotFound))
                {
                    using (StreamReader streamReader = new StreamReader(data))
                    {
                        doc.LoadXml(streamReader.ReadToEnd());

                        XmlElement xRoot = doc.DocumentElement;
                       
                        if (xRoot != null)
                        {
                            foreach (XmlElement xnode in xRoot)
                            {
                                XmlNode attr = xnode.Attributes.GetNamedItem("url");

                                foreach (XmlNode childnode in xnode.ChildNodes)
                                {
                                    if (childnode.Name == "loc")
                                    {
                                        links.Add($"{childnode.InnerText}");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Page not exist ! - 404 - !");
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
        public static void CreateDictionary(List<string> resultLinksSite, string address)
        {
            List<string> lns = new List<string>();
            string l = string.Empty;
            foreach (var item in resultLinksSite)
            {
                if (item.Contains(address))
                {
                    if (item.Contains("///"))
                    {
                        l = item.Replace("///", "//");
                    }
                    else
                    {
                        l = item;
                    }
                    lns.Add(l);
                }
            }
            List<string> links = lns.Distinct().ToList();
          
            Dictionary<string, int> timingPages = new Dictionary<string, int>();

            for (int i = 0; i < links.Count; i++)
            {
                TimingPage(l, out string timing);

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
      
            Console.WriteLine("Input URL :");

            address = Console.ReadLine();

            string nameMap = $@"{address}/sitemap.xml";
      
            List<string> linkPage = new List<string>();
            List<string> allMaps = new List<string>();
            List<string> allSiteLinks = new List<string>();
            List<string> allLinksMap = new List<string>();

            try
            {
                string responseBody = await client.GetStringAsync($"{address}");
                // Собираем все ссылки с html страницы в список ссылок
                PickLinksFirstPage(address, responseBody, out List<string> url);

                int countSymbol = address.Length;
              
                url.ForEach(l => linkPage.Add(l.Substring(countSymbol)));
          
                CleanLink(address, linkPage, out List<string> correctLinks);// все ссылки с первой страницы

                try
                {   //Собираем ссылки на каждой найденной странице
                    string responseChildPage = string.Empty;
                    foreach (var item in correctLinks)
                    {
                        if (!item.Equals($"{address}/#"))
                        {
                            responseChildPage = await client.GetStringAsync($"{item}");
                            // Собираем все ссылки с html страницы в список ссылок
                            PickLinks(address,responseChildPage, out List<string> linkss, out List<string> allSiteMaps);

                            if (linkss.Count == 0)
                            {
                                break;
                            }
                            if (allSiteMaps.Count != 0)
                            {
                                foreach (var i in allSiteMaps)
                                {
                                    allLinksMap.Add(i);
                                }
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

                MapPageLinks(nameMap, out List<string> links);

                allMaps.AddRange(links);

                List<string> mapLinks = allMaps.Distinct().ToList();

                Console.WriteLine($"URL-addresses site   = {correctLinksD.Count()}");
                Console.WriteLine($"  - - - - - - - - - - - - - ");

                Console.WriteLine($"URL-addresses sitemap = {mapLinks.Count()}");
                Console.WriteLine($"  - - - - - - - - - - - - - ");
                // Сединяем списки
                List<string> SiteAndMapLinks = correctLinksD.Concat(mapLinks).ToList();

                Console.WriteLine($"URL-addresses site + sitemap. = {SiteAndMapLinks.Count()}");
                Console.WriteLine($"  - - - - - - - - - - - - - ");

                var matchfound = mapLinks.Any(x => correctLinksD.Contains(x));

                if (matchfound)
                {
                    List<string> result = mapLinks.Where(a => correctLinksD.Contains(a)).ToList();

                    mapLinks.RemoveAll(i => result.Contains(i));

                    Console.WriteLine($" URL  SITEMAP.XML - {mapLinks.Count}");
                    CreateDictionary(mapLinks,address);
                    Console.WriteLine($"  - - - - - - - - - - - - - ");

                    Console.WriteLine($" URL  WEB SITE - {correctLinksD.Count}");
                    correctLinksD.RemoveAll(i => result.Contains(i));
                    CreateDictionary(correctLinksD,address);
                }
                else
                {
                    Console.WriteLine($" URL  SITEMAP.XML - {mapLinks.Count}");
                    CreateDictionary(mapLinks,address);
                    Console.WriteLine($"  - - - - - - - - - - - - - ");

                    Console.WriteLine($" URL  WEB SITE - {correctLinksD.Count}");
                    CreateDictionary(correctLinksD,address);
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
/*   
 *   
https://atbmarket.com/   
https://posad.com.ua/      
https://itstep.kh.ua/
https://eva.ua/
https://prostor.ua/
https://docs.microsoft.com/   

 */
