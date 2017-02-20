using System;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using HtmlAgilityPack;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace ConsoleApplication1
{
    class Program
    {
        //private static CloudQueue xmlQueue;
        private static CloudQueue htmlQueue;
        private static List<String> xmlList;
        private static List<String> robotXmlList;
        //private static CloudTable table;
        private static string baseUrl;
        private static DateTime cutOffDate;
        private static HashSet<string> disallows;

        static void Main(string[] args)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                 CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            Console.WriteLine("START");
            xmlList = new List<String>();
            robotXmlList = new List<String>();
            htmlQueue = queueClient.GetQueueReference("myhtml");
            htmlQueue.CreateIfNotExists();
            disallows = new HashSet<string>();
            cutOffDate = new DateTime(2016, 12, 1); // 12/1/2016
            baseUrl = "http://www.cnn.com";
            string robotsUrl = baseUrl + "/robots.txt";

            //parseRobot(robotsUrl);
            robotXmlList.Add("http://www.cnn.com/sitemaps/sitemap-index.xml");

            // parseHTML("http://www.cnn.com/");
            // parseXML("http://www.cnn.com/sitemaps/sitemap-index.xml");
            // parseXML("http://www.cnn.com/sitemaps/sitemap-video-2017-02.xml"); //no lastmod

            //Console.WriteLine(xmlList.Count()); //285

            //iterate through each sitemap link in robots.txt
            //iterate through each xml link in the first layer sitemap
            for (int i = 0; i < robotXmlList.Count; i++)
            {
                //Console.WriteLine(robotXmlList[i]);
                parseXML(robotXmlList[i]);
                for (int j = 0; j < xmlList.Count; j++)
                {
                    Console.WriteLine("MOO");
                    parseXML(xmlList[j]);
                }
            }
            xmlList.ForEach(Console.WriteLine);


            //once all xmls are parsed, go through html queue and crawl page
            CloudQueueMessage message = new CloudQueueMessage("");
            while (message != null)
            {
                Console.WriteLine("COW");
                message = htmlQueue.GetMessage(TimeSpan.FromMinutes(1));
                if (message != null)
                {
                    Console.WriteLine(message.AsString);
                    htmlQueue.DeleteMessage(message);
                    parseHTML(message.AsString);
                }
            }

            Console.WriteLine("DONE");
            Console.ReadLine();
        }


        public static void parseXML(string url)
        {
            Console.WriteLine("parseXML(" + url + ")");
            XmlTextReader reader = new XmlTextReader(url);
            string tag = "";
            Boolean dateAllowed = true;
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: //tag types
                        tag = reader.Name;
                        break;
                    case XmlNodeType.Text: //text within tags
                        // if vs elseif
                        // for cases where lastmod tag doesn't exist
                        if (tag == "sitemap" || tag == "url")
                        {
                            dateAllowed = true; 
                        }
                        if (tag == "lastmod")
                        {
                            string date = reader.Value.Substring(0, 10); //format: 2017-02-17 
                            DateTime dateTime = Convert.ToDateTime(date);
                            int compare = DateTime.Compare(dateTime, cutOffDate);
                            if (compare >= 0)
                            {
                                dateAllowed = true;
                            } else
                            {
                                dateAllowed = false;
                            }
                        }
                        if (tag == "loc")
                        {
                            string link = reader.Value;

                            //check if the date is allowed and robot.txt link is allowed
                            if (dateAllowed && !disallows.Contains(link))
                            {
                                if (link.Substring(link.Length - 4) == ".xml")
                                {
                                    xmlList.Add(link);
                                    //Console.WriteLine("XML: " + link);
                                }
                                else
                                {
                                    CloudQueueMessage htmlLink = new CloudQueueMessage(reader.Value);
                                    //assuming the type is .html or .htm
                                    htmlQueue.AddMessage(htmlLink);
                                    Console.WriteLine("HTML: " + htmlLink.AsString);
                                }
                            }
                        }
                        break;
                }
            }
        }


        public static void parseHTML(string link)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(link); //check if link exists and is an html document

            // ParseErrors is an ArrayList containing any errors from the Load statement
            //if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Count() > 0)
            if (htmlDoc.DocumentNode != null)
            {
                HtmlAgilityPack.HtmlNode bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
                string title = "" + htmlDoc.DocumentNode.SelectSingleNode("//head/title");
                // insert webpage into table (FIND CODE)
                // WORK ON
            }
            HtmlNode[] nodes = htmlDoc.DocumentNode.SelectNodes("//a[@href]").ToArray();
            foreach (HtmlNode item in nodes)
            {
                // insert into html queue
                string hrefValue = item.GetAttributeValue("href", string.Empty).Trim();
                string correctUrl = "";
                if (hrefValue.Length > 2)
                {
                    if (hrefValue.Substring(0, 2) == "//")
                    {
                        correctUrl = "http://" + hrefValue.Substring(2);
                    }
                    else if (hrefValue.Substring(0, 1) == "/")
                    {
                        correctUrl = baseUrl + hrefValue;
                    }
                    else if (hrefValue.Substring(0, 4) == "http")
                    {
                        correctUrl = hrefValue;
                    }

                    //insert into html queue
                    if (!disallows.Contains(correctUrl) && correctUrl.Contains("cnn.com")) // or "bleacherreport.com"
                    {
                        CloudQueueMessage htmlLink = new CloudQueueMessage(correctUrl);
                        Console.WriteLine(correctUrl);
                        htmlQueue.AddMessage(htmlLink);
                    }
                }
            }
        }


        public static void parseRobot(string url)
        {
            WebResponse response;
            WebRequest request = WebRequest.Create(url);
            response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            using (reader)
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    // Console.WriteLine(line);

                    if (line.StartsWith("Disallow:"))
                    {
                        string item = line.Substring(10);
                        disallows.Add(baseUrl + item);
                    }
                    else if (line.StartsWith("Sitemap:"))
                    {
                        string item = line.Substring(9);
                        robotXmlList.Add(item);
                    }
                }
            }
            //string output = string.Join("\r\n", disallows.ToArray());
            //string output2 = string.Join("\r\n", xmlList.ToArray());
            //Console.WriteLine(output);
            //checkSitemap(url, reader);
            //WORK ON
        }
    }
}