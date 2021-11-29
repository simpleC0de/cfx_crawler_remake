using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cfx_crawler_remake
{
    class Program
    {

        private static string safeDir;
        private static bool replace;

        private static int fetchInterval;

        [STAThread]
        static void Main(string[] args)
        {
            browserStart:
            Console.WriteLine("Select the folder where you want to safe the files to");
            FolderBrowserDialog browserDialog = new FolderBrowserDialog();
            browserDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            browserDialog.Description = "Select a folder";
            DialogResult result = browserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                safeDir = browserDialog.SelectedPath;
            }
            else
            {
                Console.Clear();
                goto browserStart;
            }

            Console.Clear();
            Console.WriteLine("[n]Do you want to override existing files? (y/n)");
            string yNo = Console.ReadLine();
            Console.Clear();
            if(yNo.ToLower() == "y")
            {
                replace = true;
                Console.WriteLine("Replacing enabled");
            }
            else
            {
                replace = false;
                Console.WriteLine("Replacing disabled");
            }
            Thread.Sleep(1000);

            setInterval:
            Console.Clear();
            Console.WriteLine("Set fetch interval in minutes:");

            string input = Console.ReadLine();
            try
            { 
                fetchInterval = int.Parse(input);
                fetchInterval = fetchInterval * 60;
            }
            catch (Exception)
            {
                goto setInterval;
            }


            Console.Clear();
            Console.WriteLine("Fetching every " + (fetchInterval / 60) + " minutes");
            Thread.Sleep(2500);
            Console.Clear();
            startCrawling();


        }

        private static Dictionary<string, string> nameUrl = new Dictionary<string, string>();
        private static List<string> pages = new List<string>();
        private static void startCrawling()
        {
            pages.Add("https://forum.cfx.re/c/development/releases/7");
            pages.Add("https://forum.cfx.re/c/development/releases/7/l/latest?order=posts");
            pages.Add("https://forum.cfx.re/c/development/releases/7/l/top");
            pages.Add("https://forum.cfx.re/c/development/releases/7/l/latest?order=activity");
            pages.Add("https://forum.cfx.re/c/development/releases/7/l/top?order=activity");
            recrawl:
            Console.Clear();
            try
            {

                foreach(string page in pages)
                {
                    List<string> fetchUrls = getUrlList(readHtml(page));

                    foreach(string url in fetchUrls.ToList())
                    {
                        Thread.Sleep(25);
                        // Iterate through all founds urls to get only the valid ones
                        string html = readHtml(url);
                        Thread.Sleep(50);
                        getDownloadLink(html, url);
                        Thread.Sleep(50);
                        Console.Clear();
                        Console.WriteLine("Found " + nameUrl.ToList().Count);
                        Thread.Sleep(50);
                    }


                    Console.WriteLine("Downloading resources of page..");
                    Thread.Sleep(1250);
                    Console.Clear();

                    foreach(string str in nameUrl.Keys)
                    {
                        //str -> url
                        //val -> fileName

                        if (str.Contains("citizenfx"))
                            continue;

                        downloadFile(str, nameUrl[str]);
                        Thread.Sleep(250);


                    }

                    nameUrl.Clear();
                    System.GC.Collect();
                }

                Console.Clear();
                System.GC.Collect();
                int fetchCopy = fetchInterval;
                while (fetchCopy > 1)
                {
                    Console.WriteLine("Fetching again in " + fetchCopy + " seconds");
                    Thread.Sleep(10000);
                    fetchCopy -= 10;
                }
                goto recrawl;

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }

        }

        private static string parseGit(string str)
        {
            /*
             * Downloadable link:
             *  https://api.github.com/repos/username/reponame/zipball
             * 
             */

            //normal received link:
            // https://github.com/mat0ta/tota_scooter
            // malformed link:
            // https://github.com/Newtonzz/PassionRP-Public/tree/master/TightThreads

            // https://github.com/FiveM-Scripts/Cops_FiveM/releases/latest

            //    0   1  2              3             4       5        6

            // https://github.com/citizenfx/project-lambdamenu/releases/download/3.13.2017/LAMBDAMENU_3.13.2017.zip

            //https://github.com/gimicze/firescript/wiki/Commands-&amp;-Usage

            try
            {
                string[] split = str.Split('/');
                string username, reponame;
                username = split[3];
                reponame = split[4];
                string baseUri = "https://api.github.com/repos/" + username + "/" + reponame + "/zipball";
                return baseUri;
            }
            catch (IndexOutOfRangeException)
            {
                return "";
            }

        
        }




        private static string readHtml(string url)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            WebRequest webRequest = (WebRequest)httpWebRequest;
            webRequest.Proxy = null;
            WebResponse webResponse = webRequest.GetResponse();
            StreamReader sr = new StreamReader(webResponse.GetResponseStream());
            string htmlStr = sr.ReadToEnd();
            return htmlStr;
        }

        private static List<string> getUrlList(string html)
        {
            string linkedUrl;
            List<string> urlList = new List<string>();

            Regex regexLink = new Regex("(?<=<a\\s*?href=(?:'|\"))[^'\"]*?(?=(?:'|\"))");

            foreach (var match in regexLink.Matches(html))
            {
                if (!urlList.Contains(match.ToString()))
                {
                    linkedUrl = GetLinkedUrl(match.ToString());
                    if (linkedUrl.Contains("/t/"))
                        urlList.Add(linkedUrl);
                }
            }
            return urlList;
        }

        private static string GetLinkedUrl(string url)
        {
            if (!url.Contains("https://"))
            {
                url = "https://forum.cfx.re" + url;
            }
            return url;
        }


        public static void getDownloadLink(string htmlStr, string website)
        {
            try
            {
                string linkedUrl;
                List<string> urlList = new List<string>();

                Regex regexLink = new Regex("(?<=\\s*?href=(?:'|\"))[^'\"]*?(?=(?:'|\"))");

                foreach (var match in regexLink.Matches(htmlStr))
                {
                    if (!urlList.Contains(match.ToString()))
                    {
                        linkedUrl = GetLinkedUrl(match.ToString());
                        if (linkedUrl.Contains("/uploads/short-url/") | linkedUrl.Contains("github.com"))
                        {
                            urlList.Add(linkedUrl);


                            /*
                             * https://forum.cfx.re/t/release-cindys-flower-shop-mlo/1773193
                             *    0   1     2       3     4                               5
                             */
                            string[] linkSplit = website.Split('/');

                            if (!nameUrl.ContainsKey(linkedUrl) && !nameUrl.ContainsValue(linkSplit[4]))
                            {
                                if (linkedUrl.Contains("github"))
                                {
                                    nameUrl.Add(parseGit(linkedUrl), linkSplit[4]);
                                }
                                else
                                {
                                    nameUrl.Add(linkedUrl, linkSplit[4]);
                                }

                            }

                        }

                    }
                }
            }
            catch(Exception ex)
            {

            }
            
        }

        private static void downloadFile(string uri, string filename)
        {
            string extension;
            if (uri.Contains("github"))
                extension = ".zip";
            else
                extension = Path.GetExtension(uri);

            string filePath = safeDir + @"\leak_studio_" + filename + extension;

            if (!replace && File.Exists(filePath))
            {
                Console.WriteLine("[SKIP] File [" + filename + "] already exists");
                return;
            }


            try
            {
                using (var client = new WebClient())
                {
                    Console.WriteLine("Downloading [" + filename + "]");
                    client.Headers.Add("user-agent", "simplec0de");
                    Console.WriteLine(uri);
                    client.DownloadFile(uri, filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Failed to download [" + filename + "]");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Thread.Sleep(1000);
            }


        }

    }
}
