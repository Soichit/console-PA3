using System;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using WebRole1;
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


            parseHTML("http://www.cnn.com/");
            // parseHTML("http://www.cnn.com/2017/02/16/us/museum-removes-art-from-immigrants-trnd");
            //parseXML("http://www.cnn.com/sitemaps/sitemap-index.xml"); //xmls
            //parseXML("http://www.cnn.com/sitemaps/sitemap-show-2017-02.xml"); //htmls

            Console.WriteLine(xmlList.Count()); //285

            //for (int i = 0; i < robotXmlList.Count; i++)
            //{
            //    //Console.WriteLine(robotXmlList[i]);
            //    parseXML(robotXmlList[i]);
            //    for (int j = 0; j < xmlList.Count; j++)
            //    {
            //        parseXML(xmlList[j]);
            //        //Console.WriteLine(xmlList[j]);
            //    }
            //}
            //xmlList.ForEach(Console.WriteLine);


            Console.WriteLine("DANK");
            ////once all xml is parsed, go through html queue
            CloudQueueMessage message = new CloudQueueMessage("");
            while (message != null)
            {
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
            Console.WriteLine("parseXML()");
            XmlTextReader reader = new XmlTextReader(url);
            string tag = "";
            Boolean dateAllowed = true;
            while (reader.Read())
            {
                // dateAllowed = true; //FIX: for cases where lastmod tag doesn't exist
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // tag types
                        tag = reader.Name;
                        break;
                    case XmlNodeType.Text: // text within tags
                        // add timestamp
                        if (tag == "sitemap")
                        {
                            dateAllowed = true; //FIX: for cases where lastmod tag doesn't exist
                        }
                        if (tag == "url")
                        {
                            dateAllowed = true; //FIX: for cases where lastmod tag doesn't exist
                        }

                        if (tag == "lastmod")
                        {
                            string date = reader.Value.Substring(0, 10); //format: 2017-02-17 
                            DateTime dateTime = Convert.ToDateTime(date);
                            int compare = DateTime.Compare(dateTime, cutOffDate);
                            //Console.WriteLine("compare: " + compare);
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
                            //Console.WriteLine(link.Substring(link.Length - 4));
                            if (link.Substring(link.Length - 4) == ".xml")
                            {
                                // add to xml list
                                //check if it's not in disallowed hashset
                                if (!disallows.Contains(link)) {
                                    //check if the date is allowed
                                    if (dateAllowed)
                                    {
                                        Console.WriteLine(link);
                                        xmlList.Add(link);
                                    }
                                    
                                }
                            }
                            //else if (link.Substring(link.Length - 5) == ".html") //FIX
                            else
                            {
                                //Console.WriteLine("BBBBBBBBBBBBB");
                                //Console.WriteLine(reader.Value);
                                //check if the date is allowed
                                if (dateAllowed)
                                {
                                    CloudQueueMessage htmlLink = new CloudQueueMessage(reader.Value);
                                    //check that type is .html or .htm
                                    var request = HttpWebRequest.Create(htmlLink.AsString) as HttpWebRequest;
                                    if (request != null)
                                    {
                                        
                                        var response = request.GetResponse() as HttpWebResponse;
                                        string contentType = "";
                                        if (response != null)
                                        {
                                            contentType = response.ContentType;
                                            Console.WriteLine(htmlLink.AsString);
                                            //Console.Write("fileType: ");
                                            //Console.WriteLine(contentType);

                                            // add to url queue if html or htm
                                            string type = contentType.Substring(0, 9);
                                            if (type == "text/html")
                                            {
                                                Console.WriteLine("YES");
                                                htmlQueue.AddMessage(htmlLink);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }


        public static void parseHTML(string link)
        {
            // web crawler
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(link);

            // ParseErrors is an ArrayList containing any errors from the Load statement
            //if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Count() > 0)
            if (htmlDoc.DocumentNode != null)
            {
                HtmlAgilityPack.HtmlNode bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
                string title = "" + htmlDoc.DocumentNode.SelectSingleNode("//head/title");
                // insert webpage into table (FIND CODE)
            }
            HtmlNode[] nodes = htmlDoc.DocumentNode.SelectNodes("//a[@href]").ToArray();
            foreach (HtmlNode item in nodes)
            {
                // insert into Queue
                //CloudQueueMessage url = new CloudQueueMessage("" + item);
                string hrefValue = item.GetAttributeValue("href", string.Empty);
                string correctUrl = "";
                if (hrefValue.Length > 2)
                {
                    if (hrefValue.Substring(0, 2) == "//")
                    {
                        correctUrl = "http://" + hrefValue.Substring(2);
                    }
                    else if (hrefValue.Substring(0, 1) == "/")
                    {
                        correctUrl = baseUrl + hrefValue.Substring(1);
                    }
                    else if (hrefValue.Substring(0, 4) == "http")
                    {
                        correctUrl = hrefValue;
                    }

                }
                //Console.WriteLine(correctUrl);
                CloudQueueMessage htmlLink = new CloudQueueMessage(correctUrl);
                htmlQueue.AddMessage(htmlLink);
                //Console.WriteLine(item.InnerHtml);
                //queue.AddMessage(url);
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
        }
    }
}