//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------


namespace Microsoft.ServiceModel.Syndication.Tests
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xunit;
    using Microsoft.ServiceModel.Syndication;
    using System.Xml;
    using Microsoft.ServiceModel.Syndication.Lab;
{
    public static class SampleTest
    {
        [Fact]
        public static void PassingTest()
        {
           
        }
        [Fact]
        public static void testUsingCustomParser()
        {
            //this test will override the default date parser and assign just a new date to the feed
            CustomRSSParsers.DateParser = delegate (string data, XmlReader xreader)
            {
                DateTime dt = XmlConvert.ToDateTime(data);
                DateTimeOffset dto = new DateTimeOffset(dt);
                return dto;
            };


            XmlReader reader = XmlReader.Create("feed.xml");
            SyndicationFeed sf = SyndicationFeed.Load(reader);

        }

        [Fact]
        public static void CreateBasicFeed()
        {
            SyndicationFeed sf = new SyndicationFeed("First feed on .net core ever!!", "This is the first feed on .net core ever!", new Uri("https://microsoft.com"));

            XmlWriter xmlw = XmlWriter.Create("FirstFeedEver.xml");
            Rss20FeedFormatter rssf = new Rss20FeedFormatter(sf);
            rssf.WriteTo(xmlw);
            xmlw.Close();
        }

        [Fact]
        public static void readFeedAndReWriteAtom()
        {
            XmlReader xmlr = XmlReader.Create("feedatom.xml");
            SyndicationFeed sf = SyndicationFeed.Load(xmlr);

            Console.WriteLine(sf.Title.ToString());

            //wite the one that was read
            XmlWriter xmlw = XmlWriter.Create("leido.xml");
            Atom10FeedFormatter rs = new Atom10FeedFormatter(sf);
            rs.WriteTo(xmlw);
            xmlw.Close();
        }

        [Fact]
        public static void readFeedAndReWriteRSS()
        {
            XmlReader xmlr = XmlReader.Create("topstories.xml");
            SyndicationFeed sf = SyndicationFeed.Load(xmlr);

            //wite the feed that was read
            XmlWriter xmlw = XmlWriter.Create("RewritenTopStories.xml");
            Rss20FeedFormatter rs = new Rss20FeedFormatter(sf);
            rs.WriteTo(xmlw);
            xmlw.Close();
        }

        [Fact]
        public static void CreateSyndicationFeedWithBothFormats()
        {
            //Test example to syndicate the release of SyndicationFeed to .net core

            SyndicationFeed feed = new SyndicationFeed("Microsoft News", "<div>Most recent news from Microsoft</div>", new Uri("http://www.microsoft.com/news"), "123FeedID", DateTime.Now);
            
            //Add an author
            SyndicationPerson author = new SyndicationPerson("jerry@microsoft.com");
            feed.Authors.Add(author);
            
            //Create item
            SyndicationItem item1 = new SyndicationItem("SyndicationFeed released for .net Core", "A lot of text describing the release of .net core feature", new Uri("http://microsoft.com/news/path"));
            
            //Add item to feed
            List<SyndicationItem> feedList = new List<SyndicationItem> { item1 };
            feed.Items = feedList;
            feed.ElementExtensions.Add("CustomElement", "", "asd");

            //add an image
            feed.ImageUrl = new Uri("http://2.bp.blogspot.com/-NA5Jb-64eUg/URx8CSdcj_I/AAAAAAAAAUo/eCx0irI0rq0/s1600/bg_Microsoft_logo3-20120824073001907469-620x349.jpg");
            
            feed.BaseUri = new Uri("http://mypage.com");
            Console.WriteLine(feed.BaseUri);

            // Write to XML > rss
            XmlWriter xmlw = XmlWriter.Create("feed.xml");
            Rss20FeedFormatter rssff = new Rss20FeedFormatter(feed);
            
            rssff.WriteTo(xmlw);
            xmlw.Close();

            // Write to XML > atom
            xmlw = XmlWriter.Create("feedatom.xml");
            Atom10FeedFormatter atomf = new Atom10FeedFormatter(feed);
            atomf.WriteTo(xmlw);
            xmlw.Close();
            
        }

        [Fact]
        public static void FailingTest()
        {
            Assert.True(false, "This test is expected to fail");
        }

        [Fact]
        public static void readFeedFromOutsideSource()
        {
            XmlReader xmlr = XmlReader.Create("topstories.xml");
            SyndicationFeed sf = SyndicationFeed.Load(xmlr);
            Assert.True(sf != null);
        }

        [Fact]
        public static void resourceTest()
        {
            ResourceTester rt = new ResourceTester();
            rt.test();
        }

        [Fact]
        public static void DateParseError()
        {
            XmlReader xmlr = XmlReader.Create("feedWrongDate.xml");
            SyndicationFeed sf = SyndicationFeed.Load(xmlr);
        }
    }
}
