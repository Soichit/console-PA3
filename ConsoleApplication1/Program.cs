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

namespace ConsoleApplication1
{
    class Program
    {
        private static CloudQueue xmlQueue;
        private static CloudQueue htmlQueue;
        private static CloudTable table;

        static void Main(string[] args)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                 CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            xmlQueue = queueClient.GetQueueReference("myXml");
            htmlQueue = queueClient.GetQueueReference("myHtml");


            //CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            //table = tableClient.GetTableReference("sum");
            ////table.DeleteIfExists();  
            //table.CreateIfNotExists();

            //Numbers n = new Numbers(77, 77, 77);
            //TableOperation insertOperation = TableOperation.Insert(n);
            //table.Execute(insertOperation);


            CloudQueueMessage message;
            string url = "http://www.cnn.com/robots.txt";
            //parseRobot(url);
            //crawlPage(url);
            parseXML("http://www.cnn.com/sitemaps/sitemap-index.xml");
            do
            {
                message = xmlQueue.GetMessage(TimeSpan.FromMinutes(5));
                xmlQueue.DeleteMessage(message);
                Console.WriteLine(message.AsString);
                parseXML(message.AsString);
            } while (message != null);

            Console.WriteLine("done");
            Console.ReadLine();
        }

        public static void crawlPage(String url)
        {
            // web crawler
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(url);
            Console.WriteLine("1");

            // ParseErrors is an ArrayList containing any errors from the Load statement
            //if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Count() > 0)
            if (htmlDoc.DocumentNode != null)
            {
                HtmlAgilityPack.HtmlNode bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
                if (bodyNode != null)
                {
                    //Console.WriteLine(bodyNode.InnerText);
                }
            }

            //HtmlNode[] nodes = htmlDoc.DocumentNode.SelectNodes("//a").ToArray();
            //foreach (HtmlNode item in nodes)
            //{
            //    Console.WriteLine(item.InnerHtml);
            //}
            Console.WriteLine("done");
            Console.ReadLine();
        }

        public static void parseRobot(string url)
        {
            string baseUrl = url.Substring(0, url.Length - 11);
            // global variables
            HashSet<string> disallows = new HashSet<string>();
            List<string> sitemaps = new List<string>();

            Console.WriteLine(baseUrl);
            Console.WriteLine("\r\n");

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
                        sitemaps.Add(item);
                    }
                }
            }
            string output = string.Join("\r\n", disallows.ToArray());
            string output2 = string.Join("\r\n", sitemaps.ToArray());
            Console.WriteLine(output2);
            //checkSitemap(url, reader);
        }

        public static void parseXML(string url)
        {
            Console.WriteLine("test");
            XmlTextReader reader = new XmlTextReader(url);
            string tag = "";
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        tag = reader.Name;
                        break;
                    case XmlNodeType.Text: //Display the text in each element.
                        // add timestamp
                        if (tag == "lastmod")
                        {
                            //Console.WriteLine(reader.Value);
                        }
                        if (tag == "loc")
                        {
                            string link = reader.Value;
                            //Console.WriteLine(link.Substring(link.Length - 4));
                            if (link.Substring(link.Length - 4) == ".xml")
                            {
                                // add to xml queue
                                Console.WriteLine(reader.Value);
                                CloudQueueMessage xmlLink = new CloudQueueMessage(reader.Value);
                                xmlQueue.AddMessage(xmlLink); //storage error?
                            } else if (link.Substring(link.Length - 5) == ".html")
                            {
                                // add to url queue
                                Console.WriteLine(reader.Value);
                                CloudQueueMessage htmlLink = new CloudQueueMessage(reader.Value);
                                xmlQueue.AddMessage(htmlLink);
                            }                
                        }
                        break;
                }
            }
        }
    }
}