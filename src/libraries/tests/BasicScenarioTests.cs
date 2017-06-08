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

    public static class BasicScenarioTests
    {
                
        [Fact]
        public static void SyndicationFeed_CreateNewFeed()
        {

            string filePath = "FirstFeedEver.xml";

            try
            {
                // *** SETUP *** \\
                SyndicationFeed sf = new SyndicationFeed("First feed on .net core ever!!", "This is the first feed on .net core ever!", new Uri("https://github.com/dotnet/wcf"));
                Assert.True(sf != null);

                XmlWriter xmlw = XmlWriter.Create(filePath);
                Rss20FeedFormatter rssf = new Rss20FeedFormatter(sf);

                // *** EXECUTE *** \\
                rssf.WriteTo(xmlw);
                xmlw.Close();

                // *** VALIDATE *** \\
                Assert.True(File.Exists(filePath));
            }
            finally
            {
                // *** CLEANUP *** \\
                File.Delete(filePath);
            }
        }

        [Fact]
        public static void SyndicationFeed_Load_Write_Feed()
        {
            string path = "SyndicationFeed-Load-Write.xml";

            try
            {
                // *** SETUP *** \\\
                XmlReader xmlr = XmlReader.Create(@"TestFeeds\SimpleRssFeed.xml");
                SyndicationFeed sf = SyndicationFeed.Load(xmlr);
                Assert.True(sf != null);

                // *** EXECUTE *** \\
                //Write the same feed that was read.
                XmlWriter xmlw = XmlWriter.Create(path);
                Atom10FeedFormatter atomFeed = new Atom10FeedFormatter(sf);
                atomFeed.WriteTo(xmlw);
                xmlw.Close();

                // *** VALIDATE *** \\
                Assert.True(File.Exists(path));
            }
            finally
            {
                // *** CLEANUP *** \\
                File.Delete(path);
            }
        }


        [Fact]
        public static void SyndicationFeed_Write_RSS_Atom()
        {
            string RssPath = "RssFeedWriteTest.xml";
            string AtomPath = "AtomFeedWriteTest.xml";

            try
            {
                // *** SETUP *** \\
                 SyndicationFeed feed = new SyndicationFeed("Contoso News", "<div>Most recent news from Contoso</div>", new Uri("http://www.Contoso.com/news"), "123FeedID", DateTime.Now);
            
                //Add an author
                SyndicationPerson author = new SyndicationPerson("jerry@Contoso.com");
                feed.Authors.Add(author);
            
                //Create item
                SyndicationItem item1 = new SyndicationItem("SyndicationFeed released for .net Core", "A lot of text describing the release of .net core feature", new Uri("http://Contoso.com/news/path"));
            
                //Add item to feed
                List<SyndicationItem> feedList = new List<SyndicationItem> { item1 };
                feed.Items = feedList;
                feed.ElementExtensions.Add("CustomElement", "", "asd");

                //add an image
                feed.ImageUrl = new Uri("http://2.bp.blogspot.com/-NA5Jb-64eUg/URx8CSdcj_I/AAAAAAAAAUo/eCx0irI0rq0/s1600/bg_Contoso_logo3-20120824073001907469-620x349.jpg");
            
                feed.BaseUri = new Uri("http://mypage.com");
                Console.WriteLine(feed.BaseUri);

                // Write to XML > rss
                
                XmlWriter xmlwRss = XmlWriter.Create(RssPath);
                Rss20FeedFormatter rssff = new Rss20FeedFormatter(feed);

                // Write to XML > atom
               
                XmlWriter xmlwAtom = XmlWriter.Create(AtomPath);
                Atom10FeedFormatter atomf = new Atom10FeedFormatter(feed);


                // *** EXECUTE *** \\
                rssff.WriteTo(xmlwRss);
                xmlwRss.Close();

                atomf.WriteTo(xmlwAtom);
                xmlwAtom.Close();

                // *** ASSERT *** \\
                Assert.True(File.Exists(RssPath));
                Assert.True(File.Exists(AtomPath));
            }
            finally
            {
                // *** CLEANUP *** \\
                File.Delete(RssPath);
                File.Delete(AtomPath);            
            }
        }         
    }
}
