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
    using System.Threading.Tasks;

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
        public static void SyndicationFeed_Load_Write_RSS_Feed()
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
                Rss20FeedFormatter atomFeed = new Rss20FeedFormatter(sf);
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
        public static void SyndicationFeed_Load_Write_Atom_Feed()
        {
            string path = "SyndicationFeed-Load-Write-Atom.xml";

            try
            {
                // *** SETUP *** \\\
                XmlReader xmlr = XmlReader.Create(@"TestFeeds\SimpleAtomFeed.xml");
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
                //feed.ImageTitle = new TextSyndicationContent("Titulo loco");

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
                //File.Delete(RssPath);
                //File.Delete(AtomPath);
            }
        }
      

        [Fact]
        public static void SyndicationFeed_RSS20_Load_customImageDataInFeed()
        {
            // *** SETUP *** \\
            XmlReader reader = XmlReader.Create(@"TestFeeds\RssFeedWithCustomImageName.xml");

            // *** EXECUTE *** \\
            SyndicationFeed sf = SyndicationFeed.Load(reader);

            // *** ASSERT *** \\
            Assert.True("The title is not the same to the original one" == sf.ImageTitle.Text);
            Assert.True(sf.ImageLink.AbsoluteUri != sf.Links[0].GetAbsoluteUri().AbsoluteUri);

            // *** CLEANUP *** \\
            reader.Close();
        }

        [Fact]
        public static void SyndicationFeed_RSS20_Write_customImageDataInFeed()
        {
            // *** SETUP *** \\
            SyndicationFeed sf = new SyndicationFeed();
            string feedTitle = "Feed title";
            string imageTitle = "Image title";
            string resultPath = "Rss20CustomImageDataFeedWritten.xml";

            sf.Title = new TextSyndicationContent(feedTitle);
            sf.ImageTitle = new TextSyndicationContent(imageTitle);
            sf.ImageLink = new Uri("http://myimage.com");
            sf.ImageUrl = new Uri("http://www.myownimagesrc.com");
            XmlWriter writer = XmlWriter.Create(resultPath);
            Rss20FeedFormatter rssff = sf.GetRss20Formatter();


            try
            {
                // *** EXECUTE *** \\
                rssff.WriteTo(writer);
                writer.Close();

                // *** ASSERT *** \\
                Assert.True(File.Exists(resultPath));

            }
            finally
            {
                // *** CLEANUP *** \\
                File.Delete(resultPath);
            }
        }


        [Fact]
        public static async Task SyndicationFeed_LoadAsync_Rss() {

            XmlReaderSettings setting = new XmlReaderSettings();
            setting.Async = true;
            XmlReader reader2 = XmlReader.Create(@"TestFeeds\rssSpecExample.xml", setting);


            Task<SyndicationFeed> rss = SyndicationFeed.LoadAsync(reader2);

           

            //atom.re

            await Task.WhenAll(rss);

            Assert.True(rss.Result.Items != null);
        }

        [Fact]
        public static async Task SyndicationFeed_LoadAsync_Atom()
        {

            XmlReaderSettings setting = new XmlReaderSettings();
            setting.Async = true;
            XmlReader reader = XmlReader.Create(@"TestFeeds\atom_complex_example.xml", setting);


            Task<SyndicationFeed> atom = SyndicationFeed.LoadAsync(reader);



            //atom.re

            await Task.WhenAll(atom);

            Assert.True(atom.Result.Items != null);
        }

        [Fact]
        public static void SyndicationFeed_Rss_TestDisjointItems()
        {
            // *** SETUP *** \\
            XmlReader reader = XmlReader.Create(@"TestFeeds\RssDisjointItems.xml");
            
            // *** EXECUTE *** \\
            SyndicationFeed sf = SyndicationFeed.Load(reader);


            // *** ASSERT *** \\
            int count = 0;
            foreach(var item in sf.Items)
            {
                count++;
            }

            Assert.True(count == 2);

            // *** CLEANUP *** \\
            reader.Close();
        }

    }
}

#if TagsForTests
// *** SETUP *** \\
// *** EXECUTE *** \\
// *** ASSERT *** \\
// *** CLEANUP *** \\
#endif