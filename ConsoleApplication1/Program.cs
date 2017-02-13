using System;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using WebRole1;
using HtmlAgilityPack;

namespace ConsoleApplication1
{
    class Program
    {

        private static CloudQueue queue;
        private static CloudTable table;

        static void Main(string[] args)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                 CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference("myurls");

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("sum");
            //table.DeleteIfExists();  
            table.CreateIfNotExists();

            Numbers n = new Numbers(77, 77, 77);
            TableOperation insertOperation = TableOperation.Insert(n);
            table.Execute(insertOperation);


            CloudQueueMessage message = queue.GetMessage(TimeSpan.FromMinutes(5));
            if (message != null)
            {
                Console.WriteLine(message.AsString);
                //queue.DeleteMessage(message);
                crawl(message);
            }
        }

        public static void crawl(String message)
        {
            // web crawler
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load("http://www.cnn.com/");
            Console.WriteLine("1");

            // ParseErrors is an ArrayList containing any errors from the Load statement
            //if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Count() > 0)
            if (htmlDoc.DocumentNode != null)
            {
                HtmlAgilityPack.HtmlNode bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
                if (bodyNode != null)
                {
                    Console.WriteLine(bodyNode.InnerText);
                }
            }

            HtmlNode[] nodes = htmlDoc.DocumentNode.SelectNodes("//a").ToArray();
            foreach (HtmlNode item in nodes)
            {
                Console.WriteLine(item.InnerHtml);
            }

            Console.ReadLine();
        }
    }
}
