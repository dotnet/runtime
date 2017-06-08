using System;
using Microsoft.ServiceModel.Syndication;

namespace TestingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            SyndicationFeed sf = new SyndicationFeed("First feed on .net core ever!!", "This is the first feed on .net core ever!", new Uri("https://microsoft.com"));

            //xml writter
            XmlWriter xmlw = XmlWriter.Create("FirstFeedEver.xml");
            Rss20FeedFormatter rssf = new Rss20FeedFormatter(sf);
            rssf.WriteTo(xmlw);
            xmlw.Close();
        }
    }
}