// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.ServiceModel.Syndication.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xunit;
    using Microsoft.ServiceModel.Syndication;
    using System.Xml;
    using System.IO;

    public static class SampleTest
    {

        [Fact]
        public static void SyndicationFeed_RSS20FeedFormatter_CustomParser()
        {
            //this test will override the default date parser and assign just a new date to the feed
            Rss20FeedFormatter rssformater = new Rss20FeedFormatter();

            rssformater.DateParser = delegate (string date, XmlReader xmlr)
            {
                return new DateTimeOffset(DateTime.Now);
            };


            XmlReader reader = XmlReader.Create("feed.xml");
            SyndicationFeed sf = SyndicationFeed.Load(reader, rssformater);

            Assert.True(sf != null);
        }

        [Fact]
        public static void SyndicationFeed_CreateNewFeed()
        {
            // *** SETUP *** \\
            SyndicationFeed sf = new SyndicationFeed("First feed on .net core ever!!", "This is the first feed on .net core ever!", new Uri(" https://github.com/dotnet/wcf"));

            XmlWriter xmlw = XmlWriter.Create("FirstFeedEver.xml");
            Rss20FeedFormatter rssf = new Rss20FeedFormatter(sf);

            // *** EXECUTE *** \\
            rssf.WriteTo(xmlw);
        
            // *** VALIDATE *** \\
            Assert.True(true);

            // *** CLEANUP *** \\
            xmlw.Close();
        }

        [Fact]
        public static void SyndicationFeed_Load_Write_Feed()
        {
            // *** SETUP *** \\\
            XmlReader xmlr = XmlReader.Create("feedatom.xml");
            SyndicationFeed sf = SyndicationFeed.Load(xmlr);


            // *** EXECUTE *** \\
            //Write the same feed that was read.
            XmlWriter xmlw = XmlWriter.Create("read.xml");
            Atom10FeedFormatter rs = new Atom10FeedFormatter(sf);
            rs.WriteTo(xmlw);
            xmlw.Close();

            // *** VALIDATE *** \\
            Assert.True(sf != null);
            Assert.True(File.Exists("read.xml"));

            // *** CLEANUP *** \\
            
        }

        [Fact]
        public static void SyndicationFeed_Load_RSS_Write_RSS()
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
        public static void SyndicationFeed_Write_RSS_Atom()
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
            
            Assert.True(true);
        }

        
        [Fact]
        public static void SyndicationFeed_Load_FeedFromInternet()
        {
            XmlReader xmlr = XmlReader.Create("MicrosoftError.xml");
            SyndicationFeed sf = SyndicationFeed.Load(xmlr);
            Assert.True(sf != null);
        }

        
        [Fact]
        public static void SyndicationFeed_Load_FeedWithWrongDateFormat()
        {
            XmlReader xmlr = XmlReader.Create("topstories.xml");
            SyndicationFeed sf = SyndicationFeed.Load(xmlr);
        }
    }
}
