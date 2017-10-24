// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel.Syndication;
using System.Xml;
using System.IO;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public static class BasicScenarioTests
    {
        [Fact]
        public static void SyndicationFeed_CreateNewFeed()
        {
            string filePath = Path.GetTempFileName();

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
            string path = Path.GetTempFileName();

            try
            {
                // *** SETUP *** \\\
                XmlReader xmlr = XmlReader.Create(@"SimpleRssFeed.xml");
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
        public static void SyndicationFeed_Load_Write_RSS_Feed_()
        {
            string path = Path.GetTempFileName();

            try
            {
                // *** SETUP *** \\\
                XmlReaderSettings settingsReader = new XmlReaderSettings();
                XmlReader xmlr = XmlReader.Create(@"rssSpecExample.xml", settingsReader);
                SyndicationFeed sf = SyndicationFeed.Load(xmlr);
                Assert.True(sf != null);

                // *** EXECUTE *** \\
                //Write the same feed that was read.
                XmlWriterSettings settingsWriter = new XmlWriterSettings();
                XmlWriter xmlw = XmlWriter.Create(path, settingsWriter);
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
            string path = Path.GetTempFileName();

            try
            {
                // *** SETUP *** \\\
                XmlReaderSettings setting = new XmlReaderSettings();
                XmlReader xmlr = XmlReader.Create(@"SimpleAtomFeed.xml", setting);
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
        public static void SyndicationFeed_Load_Write_Atom_Feed_()
        {
            string path = Path.GetTempFileName();

            try
            {
                // *** SETUP *** \\\
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                XmlReader xmlr = XmlReader.Create(@"atom_spec_example.xml", readerSettings);
                SyndicationFeed sf = SyndicationFeed.Load(xmlr);
                Assert.True(sf != null);

                // *** EXECUTE *** \\
                //Write the same feed that was read.
                XmlWriterSettings writerSettings = new XmlWriterSettings();

                XmlWriter xmlw = XmlWriter.Create(path, writerSettings);
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
            string RssPath = Path.GetTempFileName();
            string AtomPath = Path.GetTempFileName();

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

                // Write to XML > rss
                XmlWriterSettings settings = new XmlWriterSettings();
                XmlWriter xmlwRss = XmlWriter.Create(RssPath, settings);
                Rss20FeedFormatter rssff = new Rss20FeedFormatter(feed);

                // Write to XML > atom

                XmlWriter xmlwAtom = XmlWriter.Create(AtomPath);
                Atom10FeedFormatter atomf = new Atom10FeedFormatter(feed);


                // *** EXECUTE *** \\
                rssff.WriteTo(xmlwRss);
                xmlwRss.Close();

                atomf.WriteTo(xmlwAtom); ;
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
        
        [Fact]
        public static void SyndicationFeed_Load_Rss()
        {
            XmlReaderSettings setting = new XmlReaderSettings();
            using (XmlReader reader = XmlReader.Create(@"rssSpecExample.xml", setting))
            { 
                SyndicationFeed rss = SyndicationFeed.Load(reader);
                Assert.True(rss.Items != null);
            }
        }

        [Fact]
        public static void SyndicationFeed_Load_Atom()
        {
            XmlReaderSettings setting = new XmlReaderSettings();
            using (XmlReader reader = XmlReader.Create(@"atom_spec_example.xml", setting))
            {
                SyndicationFeed atom = SyndicationFeed.Load(reader);
                Assert.True(atom.Items != null);
            }
        }

        [Fact]
        public static void SyndicationFeed_Rss_TestDisjointItems()
        {
            using (XmlReader reader = XmlReader.Create(@"RssDisjointItems.xml"))
            {
                // *** EXECUTE *** \\
                SyndicationFeed sf = SyndicationFeed.Load(reader);

                // *** ASSERT *** \\
                int count = 0;
                foreach (SyndicationItem item in sf.Items)
                {
                    count++;
                }

                Assert.True(count == 2);
            }
        }


        [Fact]
        public static void SyndicationFeed_Atom_TestDisjointItems()
        {
            using (XmlReader reader = XmlReader.Create(@"AtomDisjointItems.xml"))
            {
                // *** EXECUTE *** \\
                SyndicationFeed sf = SyndicationFeed.Load(reader);

                // *** ASSERT *** \\
                int count = 0;
                foreach (SyndicationItem item in sf.Items)
                {
                    count++;
                }

                Assert.True(count == 2);
            }
        }

        [Fact]
        public static void SyndicationFeed_Rss_WrongDateFormat()
        {
            // *** SETUP *** \\
            Rss20FeedFormatter rssformatter = new Rss20FeedFormatter();

            XmlReader reader = XmlReader.Create(@"rssSpecExampleWrongDateFormat.xml");

            // *** EXECUTE *** \\
            SyndicationFeed res = SyndicationFeed.Load(reader);

            // *** ASSERT *** \\
            Assert.True(!res.LastUpdatedTime.Equals(new DateTimeOffset()));
        }
    }
}
