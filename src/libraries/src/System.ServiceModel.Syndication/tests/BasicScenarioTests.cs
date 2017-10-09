// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel.Syndication;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
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
                CancellationToken ct = new CancellationToken();
                rssf.WriteToAsync(xmlw, ct).GetAwaiter().GetResult();
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
                CancellationToken ct = new CancellationToken();

                // *** EXECUTE *** \\
                //Write the same feed that was read.
                XmlWriter xmlw = XmlWriter.Create(path);
                Rss20FeedFormatter atomFeed = new Rss20FeedFormatter(sf);
                atomFeed.WriteToAsync(xmlw, ct).GetAwaiter().GetResult();
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
        public static void SyndicationFeed_Load_Write_RSS_Feed_Async()
        {
            string path = Path.GetTempFileName();

            try
            {
                // *** SETUP *** \\\
                XmlReaderSettings settingsReader = new XmlReaderSettings();
                settingsReader.Async = true;
                XmlReader xmlr = XmlReader.Create(@"rssSpecExample.xml", settingsReader);
                SyndicationFeed sf;
                Task<SyndicationFeed> rss = null;
                CancellationToken ct = new CancellationToken();
                rss = SyndicationFeed.LoadAsync(xmlr, ct);

                Task.WhenAll(rss);
                sf = rss.Result;
                Assert.True(sf != null);

                // *** EXECUTE *** \\
                //Write the same feed that was read.
                XmlWriterSettings settingsWriter = new XmlWriterSettings();
                settingsWriter.Async = true;
                XmlWriter xmlw = XmlWriter.Create(path, settingsWriter);
                Rss20FeedFormatter atomFeed = new Rss20FeedFormatter(sf);
                Task write = atomFeed.WriteToAsync(xmlw, ct);

                Task.WhenAll(write);

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
                setting.Async = true;
                XmlReader xmlr = XmlReader.Create(@"SimpleAtomFeed.xml", setting);
                SyndicationFeed sf = SyndicationFeed.Load(xmlr);
                Assert.True(sf != null);
                CancellationToken ct = new CancellationToken();

                // *** EXECUTE *** \\
                //Write the same feed that was read.
                XmlWriter xmlw = XmlWriter.Create(path);
                Atom10FeedFormatter atomFeed = new Atom10FeedFormatter(sf);
                atomFeed.WriteToAsync(xmlw, ct).GetAwaiter().GetResult();
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
        public static void SyndicationFeed_Load_Write_Atom_Feed_Async()
        {
            string path = Path.GetTempFileName();

            try
            {
                // *** SETUP *** \\\
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.Async = true;
                XmlReader xmlr = XmlReader.Create(@"atom_spec_example.xml", readerSettings);
                CancellationToken ct = new CancellationToken();
                Task<SyndicationFeed> rss = SyndicationFeed.LoadAsync(xmlr, ct);
                SyndicationFeed sf = rss.Result;
                Assert.True(sf != null);

                // *** EXECUTE *** \\
                //Write the same feed that was read.
                XmlWriterSettings writerSettings = new XmlWriterSettings();
                writerSettings.Async = true;

                XmlWriter xmlw = XmlWriter.Create(path, writerSettings);
                Atom10FeedFormatter atomFeed = new Atom10FeedFormatter(sf);
                Task write = atomFeed.WriteToAsync(xmlw, ct);

                Task.WhenAll(write);
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
                CancellationToken ct = new CancellationToken();

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

                // Write to XML > rss

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Async = true;

                XmlWriter xmlwRss = XmlWriter.Create(RssPath, settings);
                Rss20FeedFormatter rssff = new Rss20FeedFormatter(feed);

                // Write to XML > atom

                XmlWriter xmlwAtom = XmlWriter.Create(AtomPath);
                Atom10FeedFormatter atomf = new Atom10FeedFormatter(feed);


                // *** EXECUTE *** \\
                Task rss = rssff.WriteToAsync(xmlwRss, ct);
                Task.WaitAll(rss);

                xmlwRss.Close();

                atomf.WriteToAsync(xmlwAtom, ct).GetAwaiter().GetResult(); ;
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
        public static void SyndicationFeed_RSS20_Load_customImageDataInFeed()
        {
            // *** SETUP *** \\
            XmlReader reader = XmlReader.Create(@"RssFeedWithCustomImageName.xml");

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
            string resultPath = Path.GetTempFileName();

            sf.Title = new TextSyndicationContent(feedTitle);
            sf.ImageTitle = new TextSyndicationContent(imageTitle);
            sf.ImageLink = new Uri("http://myimage.com");
            sf.ImageUrl = new Uri("http://www.myownimagesrc.com");
            XmlWriter writer = XmlWriter.Create(resultPath);
            Rss20FeedFormatter rssff = sf.GetRss20Formatter();
            CancellationToken ct = new CancellationToken();

            try
            {
                // *** EXECUTE *** \\
                rssff.WriteToAsync(writer, ct).GetAwaiter().GetResult(); ;
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
        public static async Task SyndicationFeed_LoadAsync_Rss()
        {
            // *** SETUP *** \\
            XmlReaderSettings setting = new XmlReaderSettings();
            setting.Async = true;
            XmlReader reader = null;
            Task<SyndicationFeed> rss = null;
            CancellationToken ct = new CancellationToken();

            try
            {
                // *** EXECUTE *** \\
                reader = XmlReader.Create(@"rssSpecExample.xml", setting);
                rss = SyndicationFeed.LoadAsync(reader, ct);
                await Task.WhenAll(rss);

                // *** ASSERT *** \\
            }
            finally
            {
                // *** CLEANUP *** \\
                Assert.True(rss.Result.Items != null);
                reader.Close();
            }
        }

        [Fact]
        public static async Task SyndicationFeed_LoadAsync_Atom()
        {
            // *** SETUP *** \\
            XmlReaderSettings setting = new XmlReaderSettings();
            setting.Async = true;
            XmlReader reader = null;
            CancellationToken ct = new CancellationToken();

            try
            {
                reader = XmlReader.Create(@"atom_spec_example.xml", setting);
                // *** EXECUTE *** \\
                Task<SyndicationFeed> atom = SyndicationFeed.LoadAsync(reader, ct);
                await Task.WhenAll(atom);
                // *** ASSERT *** \\
                Assert.True(atom.Result.Items != null);
            }
            finally
            {
                // *** CLEANUP *** \\
                reader.Close();
            }
        }

        [Fact]
        public static void SyndicationFeed_Rss_TestDisjointItems()
        {
            // *** SETUP *** \\
            XmlReader reader = XmlReader.Create(@"RssDisjointItems.xml");

            try
            {
                // *** EXECUTE *** \\
                SyndicationFeed sf = SyndicationFeed.Load(reader);

                // *** ASSERT *** \\
                int count = 0;
                foreach (var item in sf.Items)
                {
                    count++;
                }

                Assert.True(count == 2);
            }
            catch
            {
                // *** CLEANUP *** \\
                reader.Close();
            }
        }


        [Fact]
        public static void SyndicationFeed_Atom_TestDisjointItems()
        {
            // *** SETUP *** \\
            XmlReader reader = XmlReader.Create(@"AtomDisjointItems.xml");

            try
            {
                // *** EXECUTE *** \\
                SyndicationFeed sf = SyndicationFeed.Load(reader);

                // *** ASSERT *** \\
                int count = 0;
                foreach (var item in sf.Items)
                {
                    count++;
                }

                Assert.True(count == 2);
            }
            finally
            {
                // *** CLEANUP *** \\
                reader.Close();
            }
        }

        [Fact]
        public static async Task SyndicationFeed_RSS_Optional_Documentation()
        {
            // *** SETUP *** \\
            XmlReaderSettings setting = new XmlReaderSettings();
            setting.Async = true;
            XmlReader reader = null;
            Task<SyndicationFeed> rss = null;
            CancellationToken ct = new CancellationToken();

            try
            {
                // *** EXECUTE *** \\
                reader = XmlReader.Create(@"rssSpecExample.xml", setting);
                rss = SyndicationFeed.LoadAsync(reader, ct);
                await Task.WhenAll(rss);

                // *** ASSERT *** \\
                Assert.True(rss.Result.Documentation.GetAbsoluteUri().ToString() == "http://blogs.law.harvard.edu/tech/rss");
            }
            finally
            {
                // *** CLEANUP *** \\
                Assert.True(rss.Result.Items != null);
                reader.Close();
            }
        }

        [Fact]
        public static async Task SyndicationFeed_RSS_Optional_TimeToLiveTag()
        {
            // *** SETUP *** \\
            XmlReaderSettings setting = new XmlReaderSettings();
            setting.Async = true;
            XmlReader reader = null;
            Task<SyndicationFeed> rss = null;
            CancellationToken ct = new CancellationToken();

            try
            {
                // *** EXECUTE *** \\
                reader = XmlReader.Create(@"rssSpecExample.xml", setting);
                rss = SyndicationFeed.LoadAsync(reader, ct);
                await Task.WhenAll(rss);

                // *** ASSERT *** \\
                Assert.True(rss.Result.TimeToLive == 60);
            }
            finally
            {
                // *** CLEANUP *** \\
                Assert.True(rss.Result.Items != null);
                reader.Close();
            }
        }

        [Fact]
        public static async Task SyndicationFeed_RSS_Optional_SkipHours()
        {
            // *** SETUP *** \\
            XmlReaderSettings setting = new XmlReaderSettings();
            setting.Async = true;
            XmlReader reader = null;
            Task<SyndicationFeed> rss = null;
            CancellationToken ct = new CancellationToken();

            try
            {
                // *** EXECUTE *** \\
                reader = XmlReader.Create(@"rssSpecExample.xml", setting);
                rss = SyndicationFeed.LoadAsync(reader, ct);
                await Task.WhenAll(rss);

                // *** ASSERT *** \\
                Assert.True(rss.Result.SkipHours.Count == 3);
            }
            finally
            {
                // *** CLEANUP *** \\
                Assert.True(rss.Result.Items != null);
                reader.Close();
            }
        }

        [Fact]
        public static async Task SyndicationFeed_RSS_Optional_SkipDays()
        {
            // *** SETUP *** \\
            XmlReaderSettings setting = new XmlReaderSettings();
            setting.Async = true;
            XmlReader reader = null;
            Task<SyndicationFeed> rss = null;
            CancellationToken ct = new CancellationToken();

            try
            {
                // *** EXECUTE *** \\
                reader = XmlReader.Create(@"rssSpecExample.xml", setting);
                rss = SyndicationFeed.LoadAsync(reader, ct);
                await Task.WhenAll(rss);

                // *** ASSERT *** \\
                Assert.True(rss.Result.SkipDays.Count == 2);
                Assert.True(rss.Result.SkipDays[0] == "Saturday");
                Assert.True(rss.Result.SkipDays[1] == "Sunday");
            }
            finally
            {
                // *** CLEANUP *** \\
                Assert.True(rss.Result.Items != null);
                reader.Close();
            }
        }

        [Fact]
        public static async Task SyndicationFeed_RSS_Optional_TextInput()
        {
            // *** SETUP *** \\
            XmlReaderSettings setting = new XmlReaderSettings();
            setting.Async = true;
            XmlReader reader = null;
            Task<SyndicationFeed> rss = null;
            CancellationToken ct = new CancellationToken();

            try
            {
                // *** EXECUTE *** \\
                reader = XmlReader.Create(@"rssSpecExample.xml", setting);
                rss = SyndicationFeed.LoadAsync(reader, ct);
                await Task.WhenAll(rss);

                // *** ASSERT *** \\
                Assert.True(rss.Result.TextInput.Description == "Search Online");
                Assert.True(rss.Result.TextInput.title == "Search");
                Assert.True(rss.Result.TextInput.name == "input Name");
                Assert.True(rss.Result.TextInput.link.GetAbsoluteUri().ToString() == "http://www.contoso.no/search?");
            }
            finally
            {
                // *** CLEANUP *** \\
                Assert.True(rss.Result.Items != null);
                reader.Close();
            }
        }

        [Fact]
        public static async Task SyndicationFeed__Atom_Optional_Icon()
        {
            // *** SETUP *** \\
            XmlReaderSettings setting = new XmlReaderSettings();
            setting.Async = true;
            XmlReader reader = null;
            CancellationToken ct = new CancellationToken();

            try
            {
                reader = XmlReader.Create(@"atom_spec_example.xml", setting);
                // *** EXECUTE *** \\
                Task<SyndicationFeed> atom = SyndicationFeed.LoadAsync(reader, ct);
                await Task.WhenAll(atom);
                // *** ASSERT *** \\
                Assert.True(atom.Result.IconImage.AbsoluteUri == "https://avatars0.githubusercontent.com/u/9141961");
            }
            finally
            {
                // *** CLEANUP *** \\
                reader.Close();
            }
        }

        [Fact]
        public static void SyndicationFeed_Rss_TestCustomParsing()
        {
            // *** SETUP *** \\
            Rss20FeedFormatter rssformatter = new Rss20FeedFormatter();

            rssformatter.StringParser = (val, name, ns) =>
            {
                Assert.False(string.IsNullOrEmpty(name));
                switch (name)
                {
                    case "ttl":
                    case "hour":
                        return "5";
                    case "link":
                    case "image":
                    case "url":
                        return "http://customparsedlink.com";
                    case "title":
                        return "new title";
                    default:
                        return "Custom Text";
                }
            };

            XmlReader reader = XmlReader.Create(@"rssSpecExample.xml");
            CancellationToken ct = new CancellationToken();

            // *** EXECUTE *** \\
            Task<SyndicationFeed> task = SyndicationFeed.LoadAsync(reader, rssformatter, ct);
            Task.WhenAll(task);
            SyndicationFeed res = task.Result;

            // *** ASSERT *** \\
            Assert.True(res.Title.Text == "new title");
            foreach (int hour in res.SkipHours)
            {
                Assert.True(hour == 5);
            }
        }

        [Fact]
        public static void SyndicationFeed_Atom_TestCustomParsing()
        {
            // *** SETUP *** \\
            Atom10FeedFormatter atomformatter = new Atom10FeedFormatter();

            atomformatter.stringParser = (val, name, ns) =>
            {
                Assert.False(string.IsNullOrEmpty(name));
                switch (name)
                {
                    case Atom10Constants.IdTag:
                        return "No id!";
                    case Atom10Constants.NameTag:
                        return "new name";
                    case Atom10Constants.TitleTag:
                        return "new title";
                    default:
                        return "Custom Text";
                }
            };

            XmlReader reader = XmlReader.Create(@"atom_spec_example.xml");
            CancellationToken ct = new CancellationToken();

            // *** EXECUTE *** \\
            Task<SyndicationFeed> task = SyndicationFeed.LoadAsync(reader, atomformatter, ct);
            Task.WhenAll(task);
            SyndicationFeed res = task.Result;

            // *** ASSERT *** \\
            Assert.True(res.Id == "No id!");
            Assert.True(res.Title.Text == "new title");
        }

        [Fact]
        public static void SyndicationFeed_Rss_TestWrongSkipDays()
        {
            // *** SETUP *** \\
            Rss20FeedFormatter rssformatter = new Rss20FeedFormatter();

            XmlReader reader = XmlReader.Create(@"rssSpecExampleWrongSkipDays.xml");
            CancellationToken ct = new CancellationToken();

            // *** EXECUTE *** \\
            Task<SyndicationFeed> task = SyndicationFeed.LoadAsync(reader, ct);
            Task.WhenAll(task);
            SyndicationFeed res = task.Result;

            // *** ASSERT *** \\
            Assert.True(res.SkipDays.Count == 2);
            Assert.True(res.SkipDays[0] == "Saturday");
            Assert.True(res.SkipDays[1] == "Sunday");
        }

        [Fact]
        public static void SyndicationFeed_Rss_WrongDateFormat()
        {
            // *** SETUP *** \\
            Rss20FeedFormatter rssformatter = new Rss20FeedFormatter();

            XmlReader reader = XmlReader.Create(@"rssSpecExampleWrongDateFormat.xml");
            CancellationToken ct = new CancellationToken();

            // *** EXECUTE *** \\
            Task<SyndicationFeed> task = SyndicationFeed.LoadAsync(reader, ct);
            Task.WhenAll(task);
            SyndicationFeed res = task.Result;

            // *** ASSERT *** \\
            Assert.True(!res.LastUpdatedTime.Equals(new DateTimeOffset()));
        }
    }
}
