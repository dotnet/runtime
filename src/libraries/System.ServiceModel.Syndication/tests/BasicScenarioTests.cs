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
        [ActiveIssue(25080)]
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
        [ActiveIssue(25080)]
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
        [ActiveIssue(25080)]
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

        [Fact]
        public static void AtomEntryPositiveTest()
        {
            string filePath = @"brief-entry-noerror.xml";
            string serializeFilePath = Path.GetTempFileName();

            try
            {
                SyndicationItem feedObjct = null;
                using (XmlReader reader = XmlReader.Create(filePath))
                {
                    feedObjct = SyndicationItem.Load(reader);
                    reader.Close();
                }

                using (XmlWriter writer = XmlWriter.Create(serializeFilePath))
                {
                    Atom10ItemFormatter atomformatter = new Atom10ItemFormatter(feedObjct);
                    atomformatter.WriteTo(writer);
                    writer.Close();
                }
                // compare file filePath and serializeFilePath
                XmlDiff diff = new XmlDiff()
                {
                    Option = XmlDiffOption.IgnoreComments | XmlDiffOption.IgnorePrefix | XmlDiffOption.IgnoreWhitespace | XmlDiffOption.IgnoreChildOrder | XmlDiffOption.IgnoreAttributeOrder
                };
                Assert.True(diff.Compare(filePath, serializeFilePath));
            }
            finally
            {
                File.Delete(serializeFilePath);
            }
        }

        [Fact]
        public static void AtomEntryPositiveTest_write()
        {
            string filePath = @"AtomEntryTest.xml";
            string serializeFilePath = Path.GetTempFileName();

            SyndicationItem item = new SyndicationItem("SyndicationFeed released for .net Core", "A lot of text describing the release of .net core feature", new Uri("http://contoso.com/news/path"));
            item.Id = "uuid:43481a10-d881-40d1-adf2-99b438c57e21;id=1";
            item.LastUpdatedTime = new DateTimeOffset(Convert.ToDateTime("2017-10-11T11:25:55Z")).UtcDateTime;

            try
            {
                using (XmlWriter writer = XmlWriter.Create(serializeFilePath))
                {
                    Atom10ItemFormatter f = new Atom10ItemFormatter(item);
                    f.WriteTo(writer);
                    writer.Close();
                }

                XmlDiff diff = new XmlDiff()
                {
                    Option = XmlDiffOption.IgnoreComments | XmlDiffOption.IgnorePrefix | XmlDiffOption.IgnoreWhitespace | XmlDiffOption.IgnoreChildOrder | XmlDiffOption.IgnoreAttributeOrder
                };
                Assert.True(diff.Compare(filePath, serializeFilePath));
            }
            finally
            {
                File.Delete(serializeFilePath);
            }
        }

        [Fact]
        [ActiveIssue(25058)]
        public static void AtomFeedPositiveTest_WsFeedIcon()
        {
            string file = "ws-feed-icon.xml";
            AtomFeedPositiveTest(file);
        }

        [Fact]
        public static void AtomFeedPositiveTest()
        {
            string dataFile = @"atom_feeds.dat";
            List<string> fileList = GetTestFilesForFeedTest(dataFile);

            foreach (string file in fileList)
            {
                AtomFeedPositiveTest(file);
            }
        }

        private static void AtomFeedPositiveTest(string file)
        {
            string serializeFilePath = Path.GetTempFileName();
            bool toDeletedFile = true;
            try
            {
                SyndicationFeed feedObjct;

                using (XmlReader reader = XmlReader.Create(file))
                {
                    feedObjct = SyndicationFeed.Load(reader);
                    reader.Close();
                }

                using (XmlWriter writer = XmlWriter.Create(serializeFilePath))
                {
                    Atom10FeedFormatter f = new Atom10FeedFormatter(feedObjct);
                    f.WriteTo(writer);
                    writer.Close();
                }

                CompareHelper ch = new CompareHelper
                {
                    Diff = new XmlDiff()
                    {
                        Option = XmlDiffOption.IgnoreComments | XmlDiffOption.IgnorePrefix | XmlDiffOption.IgnoreWhitespace | XmlDiffOption.IgnoreChildOrder | XmlDiffOption.IgnoreAttributeOrder
                    },
                    AllowableDifferences = GetAtomFeedPositiveTestAllowableDifferences()
                };

                if (!ch.Compare(file, serializeFilePath))
                {
                    toDeletedFile = false;
                    Assert.True(false, $"The generated file was different from the baseline file{Environment.NewLine}Baseline: {file}{Environment.NewLine}Actual: {serializeFilePath}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine("------------------------------");
                Console.WriteLine($"Failed File Name:{file}");
                throw e;
            }
            finally
            {
                if (toDeletedFile)
                    File.Delete(serializeFilePath);
            }
        }

        private static List<AllowableDifference> GetAtomFeedPositiveTestAllowableDifferences()
        {
            return new List<AllowableDifference>(new AllowableDifference[]
            {
                new AllowableDifference("<content xmlns=\"http://www.w3.org/2005/Atom\" />","<content type=\"text\" xmlns=\"http://www.w3.org/2005/Atom\" />"),
                new AllowableDifference("<content>","<content type=\"text\">"),
                new AllowableDifference("<content src=\"http://example.org/2003/12/13/atom03\" xmlns=\"http://www.w3.org/2005/Atom\" />","<content src=\"http://example.org/2003/12/13/atom03\" type=\"text\" xmlns=\"http://www.w3.org/2005/Atom\" />"),
                new AllowableDifference("<content src=\"  http://www.example.com/doc.pdf\" type=\"application/pdf\" xmlns=\"http://www.w3.org/2005/Atom\" />","<content src=\"http://www.example.com/doc.pdf\" type=\"application/pdf\" xmlns=\"http://www.w3.org/2005/Atom\" />"),
                new AllowableDifference("2003-12-13T08:29:29-04:00","2003-12-13T12:29:29Z"),
                new AllowableDifference("2003-12-13T18:30:02+01:00","2003-12-13T17:30:02Z"),
                new AllowableDifference("2002-12-31T19:20:30+01:00","2002-12-31T18:20:30Z"),
                new AllowableDifference("2004-12-27T11:12:01-05:00","2004-12-27T16:12:01Z"),
                new AllowableDifference("<title>","<title type=\"text\">"),
                new AllowableDifference("<subtitle>","<subtitle type=\"text\">"),
                new AllowableDifference("<subtitle xmlns=\"http://www.w3.org/2005/Atom\" />","<subtitle type=\"text\" xmlns=\"http://www.w3.org/2005/Atom\" />"),
                new AllowableDifference("<title xmlns=\"http://www.w3.org/2005/Atom\" />","<title type=\"text\" xmlns=\"http://www.w3.org/2005/Atom\" />"),
                new AllowableDifference("<title />","<title type=\"text\" xmlns=\"http://www.w3.org/2005/Atom\" />"),
                new AllowableDifference("<atom:title>","<title type=\"text\">"),
                new AllowableDifference("<summary>","<summary type=\"text\">"),
                new AllowableDifference("<atom:summary>","<summary type=\"text\">"),
                new AllowableDifference("<generator uri=\"http://www.example.com/\" version=\"1.0\">", "<generator>"),
                new AllowableDifference("<generator uri=\"/generator\" xmlns=\"http://www.w3.org/2005/Atom\" />", "<generator xmlns=\"http://www.w3.org/2005/Atom\" />"),
                new AllowableDifference("<generator uri=\"/generator\">", "<generator>"),
                new AllowableDifference("<generator uri=\"misc/Colophon\">", "<generator>"),
                new AllowableDifference("<generator uri=\"http://www.example.com/ \" version=\"1.0\">", "<generator>"),
                new AllowableDifference("<rights>","<rights type=\"text\">"),
                new AllowableDifference("<link href=\"http://example.com\" xmlns=\"http://www.w3.org/2005/Atom\" />","<link href=\"http://example.com/\" xmlns=\"http://www.w3.org/2005/Atom\" />"),
                new AllowableDifference("<link href=\"  http://example.org/  \" xmlns=\"http://www.w3.org/2005/Atom\" />","<link href=\"http://example.org/\" xmlns=\"http://www.w3.org/2005/Atom\" />"),
                new AllowableDifference("<feed xml:lang=\"\" xmlns=\"http://www.w3.org/2005/Atom\">","<feed xmlns=\"http://www.w3.org/2005/Atom\">"),
                new AllowableDifference("<entry xmlns:xh=\"http://www.w3.org/1999/xhtml\">","<entry>"),
                new AllowableDifference("<xh:div>","<xh:div xmlns:xh=\"http://www.w3.org/1999/xhtml\">"),
                new AllowableDifference("<summary type=\"xhtml\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">","<summary type=\"xhtml\">"),
                new AllowableDifference("<xhtml:a href=\"http://example.com/\">","<xhtml:a href=\"http://example.com/\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">"),
                new AllowableDifference("<feed xmlns:trackback=\"http://madskills.com/public/xml/rss/module/trackback/\" xmlns=\"http://www.w3.org/2005/Atom\">","<feed xmlns=\"http://www.w3.org/2005/Atom\">"),
                new AllowableDifference("<trackback:ping>", "<trackback:ping xmlns:trackback=\"http://madskills.com/public/xml/rss/module/trackback/\">"),
                new AllowableDifference("<feed xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:foaf=\"http://xmlns.com/foaf/0.1/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns=\"http://www.w3.org/2005/Atom\">", "<feed xmlns=\"http://www.w3.org/2005/Atom\">"),
                new AllowableDifference("<author rdf:parseType=\"Resource\">", "<author xmlns:a=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" a:parseType=\"Resource\">"),
                new AllowableDifference("<foaf:homepage rdf:resource=\"http://jondoe.example.org/\">", "<foaf:homepage xmlns:foaf=\"http://xmlns.com/foaf/0.1/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" rdf:resource=\"http://jondoe.example.org/\">"),
                new AllowableDifference("<foaf:weblog rdf:resource=\"http://jondoe.example.org/blog/\">", "<foaf:weblog xmlns:foaf=\"http://xmlns.com/foaf/0.1/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" rdf:resource=\"http://jondoe.example.org/blog/\">"),
                new AllowableDifference("<foaf:workplaceHomepage rdf:resource=\"http://DoeCorp.example.com/\">", "<foaf:workplaceHomepage xmlns:foaf=\"http://xmlns.com/foaf/0.1/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" rdf:resource=\"http://DoeCorp.example.com/\">"),
                new AllowableDifference("<dc:description>", "<dc:description xmlns:dc=\"http://purl.org/dc/elements/1.1/\">"),
                new AllowableDifference("<entry rdf:parseType=\"Resource\">", "<entry xmlns:a=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" a:parseType=\"Resource\">"),
                new AllowableDifference("<foaf:primaryTopic rdf:parseType=\"Resource\">", "<foaf:primaryTopic xmlns:foaf=\"http://xmlns.com/foaf/0.1/\" rdf:parseType=\"Resource\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">"),
                new AllowableDifference("<link href=\"http://example.org/\" rdf:resource=\"http://example.org/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns=\"http://www.w3.org/2005/Atom\" />", "<link xmlns:a=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" href=\"http://example.org/\" a:resource=\"http://example.org/\" xmlns=\"http://www.w3.org/2005/Atom\" />"),
                new AllowableDifference("<feed xmlns:creativeCommons=\"http://backend.userland.com/creativeCommonsRssModule\" xmlns=\"http://www.w3.org/2005/Atom\">", "<feed xmlns=\"http://www.w3.org/2005/Atom\">"),
                new AllowableDifference("<creativeCommons:license>", "<creativeCommons:license xmlns:creativeCommons=\"http://backend.userland.com/creativeCommonsRssModule\">"),
                new AllowableDifference("<feed xmlns:a=\"http://www.example.com/extension-a\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns=\"http://www.w3.org/2005/Atom\">", "<feed xmlns=\"http://www.w3.org/2005/Atom\">"),
                new AllowableDifference("<a:simple-value>", "<a:simple-value xmlns:a=\"http://www.example.com/extension-a\">"),
                new AllowableDifference("<a:structured-xml>", "<a:structured-xml xmlns:a=\"http://www.example.com/extension-a\">"),
                new AllowableDifference("<dc:title>", "<dc:title xmlns:dc=\"http://purl.org/dc/elements/1.1/\">"),
                new AllowableDifference("<simple-value>", "<simple-value xmlns=\"\">"),
                new AllowableDifference("<feed xmlns:xhtml=\"http://www.w3.org/1999/xhtml\" xmlns=\"http://www.w3.org/2005/Atom\">", "<feed xmlns=\"http://www.w3.org/2005/Atom\">"),
                new AllowableDifference("<feed xmlns:trackback=\"http://madskills.com/public/xml/rss/module/trackback/\" xmlns=\"http://www.w3.org/2005/Atom\">", "<feed xmlns=\"http://www.w3.org/2005/Atom\">"),
                new AllowableDifference("<trackback:about>", "<trackback:about xmlns:trackback=\"http://madskills.com/public/xml/rss/module/trackback/\">"),
                new AllowableDifference("<xhtml:img src=\"http://example.com/image.jpg\">", "<xhtml:img src=\"http://example.com/image.jpg\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">"),
                new AllowableDifference("<feed xml:base=\"http://example.org\" xmlns=\"http://www.w3.org/2005/Atom\">", "<feed xml:base=\"http://example.org/\" xmlns=\"http://www.w3.org/2005/Atom\">"),
                new AllowableDifference("<link href=\"http://creativecommons.org/licenses/by-nc/2.5/\" xmlns:lic=\"http://web.resource.org/cc/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" rel=\"http://www.snellspace.com/atom/extensions/proposed/license\" rdf:resource=\"http://creativecommons.org/licenses/by-nc/2.5/\" type=\"text/html\" rdf:type=\"http://web.resource.org/cc/license\">", "<link xmlns:a=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" href=\"http://creativecommons.org/licenses/by-nc/2.5/\" rel=\"http://www.snellspace.com/atom/extensions/proposed/license\" a:resource=\"http://creativecommons.org/licenses/by-nc/2.5/\" type=\"text/html\" a:type=\"http://web.resource.org/cc/license\">"),
            });
        }

        private static List<string> GetTestFilesForFeedTest(string dataFile)
        {
            List<string> fileList = new List<string>();

            string file;
            using (StreamReader sr = new StreamReader(dataFile))
            {
                while (!string.IsNullOrEmpty(file = sr.ReadLine()))
                {
                    if (!file.StartsWith("#"))
                    {
                        file = file.Trim();
                        if (File.Exists(file))
                        {
                            fileList.Add(Path.GetFullPath(file));
                        }
                        else
                        {
                            throw new FileNotFoundException($"File `{file}` was not found!");
                        }
                    }
                }
            }
            return fileList;
        }
    }
}
