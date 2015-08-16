﻿using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

namespace Amz.Scrape
{
    public class Scraper
    {
        private CookieContainer cookies;

        public Scraper(CookieContainer cc)
        {
            cookies = cc;
        }

        public List<int> LoadOverview(string url)
        {
            List<int> result = new List<int>();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = cookies;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            cookies.Add(response.Cookies); // for further requests

            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(sr.ReadToEnd());

                HtmlAgilityPack.HtmlNode node = doc.DocumentNode.SelectSingleNode("//select[@name='orderFilter']");
                if (node != null)
                {
                    foreach (HtmlAgilityPack.HtmlNode option in node.SelectNodes("option[@value]"))
                    {
                        string year = option.Attributes["value"].Value;
                        if (year.StartsWith("year-"))
                        {
                            int n = 0;
                            if (int.TryParse(year.Substring(5), out n))
                                result.Add(n);
                        }
                    }
                }
                else throw new InvalidOperationException("Login failed!");
            }

            return result;
        }

        public void LoadYear(string url, int year)
        {
            int numPages = 0;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(url, 1, year, 0));
            request.CookieContainer = cookies;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            cookies.Add(response.Cookies); // for further requests

            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(sr.ReadToEnd());

                HtmlAgilityPack.HtmlNode node = doc.DocumentNode.SelectSingleNode("//ul[@class='a-pagination']");
                if (node != null)
                {
                    numPages = node.SelectNodes("li").Count-2; // ignore back and forward buttons
                    ScanOrders(doc.DocumentNode.SelectSingleNode("//div[@id='ordersContainer']"));
                }
                else throw new InvalidOperationException("Unknown pagination format!");
            }

            for (int i = 2; i < numPages; i++)
            {
                Console.WriteLine("\tpage {0}...", i);
                request = (HttpWebRequest)WebRequest.Create(string.Format(url, i, year, (i - 1) * 10));
                request.CookieContainer = cookies;

                response = (HttpWebResponse)request.GetResponse();
                cookies.Add(response.Cookies); // for further requests

                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(sr.ReadToEnd());
                    ScanOrders(doc.DocumentNode.SelectSingleNode("//div[@id='ordersContainer']"));
                }
            }
        }

        private void ScanOrders(HtmlAgilityPack.HtmlNode node)
        {
            foreach (HtmlAgilityPack.HtmlNode order in node.SelectNodes(".//div[contains(@class, 'order')]"))
            {
                HtmlAgilityPack.HtmlNode info = order.SelectSingleNode(".//div[contains(@class, 'order-info')]");
                if (info != null)
                {
                    Console.Write("\tOrder: ");
                    HtmlAgilityPack.HtmlNode price = info.SelectSingleNode(".//div[contains(@class, 'a-span2')]//span[contains(@class, 'value')]");
                    if (price != null)
                        Console.WriteLine(price.InnerText.Trim());
                    else
                        Console.WriteLine("not found!");
                }
            }
        }
    }
}