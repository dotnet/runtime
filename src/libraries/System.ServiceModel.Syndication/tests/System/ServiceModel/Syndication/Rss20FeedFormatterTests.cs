// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public partial class Rss20FeedFormatterTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var formatter = new Formatter();
            Assert.Null(formatter.Feed);
            Assert.Equal(typeof(SyndicationFeed), formatter.FeedTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void Ctor_GenericDefault()
        {
            var formatter = new GenericFormatter<SyndicationFeed>();
            Assert.Null(formatter.Feed);
            Assert.Equal(typeof(SyndicationFeed), formatter.FeedTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void Ctor_SyndicationFeed()
        {
            var feed = new SyndicationFeed();
            var formatter = new Formatter(feed);
            Assert.Same(feed, formatter.Feed);
            Assert.Equal(typeof(SyndicationFeed), formatter.FeedTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void Ctor_GenericSyndicationFeed()
        {
            var feed = new SyndicationFeed();
            var formatter = new GenericFormatter<SyndicationFeed>(feed);
            Assert.Same(feed, formatter.Feed);
            Assert.Equal(typeof(SyndicationFeed), formatter.FeedTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_SyndicationFeed_Bool(bool serializeExtensionsAsAtom)
        {
            var feed = new SyndicationFeed();
            var formatter = new Formatter(feed, serializeExtensionsAsAtom);
            Assert.Same(feed, formatter.Feed);
            Assert.Equal(typeof(SyndicationFeed), formatter.FeedTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_GenericSyndicationFeed_Bool(bool serializeExtensionsAsAtom)
        {
            var feed = new SyndicationFeed();
            var formatter = new GenericFormatter<SyndicationFeed>(feed, serializeExtensionsAsAtom);
            Assert.Same(feed, formatter.Feed);
            Assert.Equal(typeof(SyndicationFeed), formatter.FeedTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void Ctor_NullFeedToWrite_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("feedToWrite", () => new Rss20FeedFormatter((SyndicationFeed)null));
            AssertExtensions.Throws<ArgumentNullException>("feedToWrite", () => new Rss20FeedFormatter<SyndicationFeed>(null));
        }

        [Theory]
        [InlineData(typeof(SyndicationFeed))]
        [InlineData(typeof(SyndicationFeedSubclass))]
        public void Ctor_Type(Type feedTypeToCreate)
        {
            var formatter = new Formatter(feedTypeToCreate);
            Assert.Null(formatter.Feed);
            Assert.Equal(feedTypeToCreate, formatter.FeedTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void Ctor_NullFeedTypeToCreate_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("feedTypeToCreate", () => new Rss20FeedFormatter((Type)null));
        }

        [Fact]
        public void Ctor_InvalidFeedTypeToCreate_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("feedTypeToCreate", () => new Rss20FeedFormatter(typeof(int)));
        }

        [Fact]
        public void GetSchema_Invoke_ReturnsNull()
        {
            IXmlSerializable formatter = new Rss20FeedFormatter();
            Assert.Null(formatter.GetSchema());
        }

        public static IEnumerable<object[]> WriteTo_TestData()
        {
            // Empty item.
            foreach (bool serializeExtensionsAsAtom in new bool[] { true, false })
            {
                yield return new object[]
                {
                    new SyndicationFeed(),
                    serializeExtensionsAsAtom,
@"    <channel>
        <title />
        <description />
    </channel>"
                };
            }

            // Full item.
            SyndicationPerson CreatePerson(string prefix)
            {
                var person = new SyndicationPerson();
                person.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name1"), null);
                person.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name2", prefix + "_namespace"), "");
                person.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name3", prefix + "_namespace"), prefix + "_value");
                person.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name4", "xmlns"), "");

                person.ElementExtensions.Add(new ExtensionObject { Value = 10 });

                person.Email = prefix + "_email";

                person.Name = prefix + "_name";

                person.Uri = prefix + "_uri";

                return person;
            }

            TextSyndicationContent CreateContent(string prefix)
            {
                var content = new TextSyndicationContent(prefix + "_title", TextSyndicationContentKind.Html);

                content.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name1"), null);
                content.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name2", prefix + "_namespace"), "");
                content.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name3", prefix + "_namespace"), prefix + "_value");
                content.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name4", "xmlns"), "");

                return content;
            }

            SyndicationCategory CreateCategory(string prefix)
            {
                var category = new SyndicationCategory();
                category.AttributeExtensions.Add(new XmlQualifiedName(prefix + "category_name1"), null);
                category.AttributeExtensions.Add(new XmlQualifiedName(prefix + "category_name2", prefix + "category_namespace"), "");
                category.AttributeExtensions.Add(new XmlQualifiedName(prefix + "category_name3", prefix + "category_namespace"), prefix + "category_value");
                category.AttributeExtensions.Add(new XmlQualifiedName(prefix + "category_name4", "xmlns"), "");

                category.ElementExtensions.Add(new ExtensionObject { Value = 10 });

                category.Label = prefix + "category_label";

                category.Name = prefix + "category_name";

                category.Scheme = prefix + "category_scheme";

                return category;
            }

            SyndicationLink CreateLink(string prefix)
            {
                var link = new SyndicationLink();
                link.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name1"), null);
                link.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name2", prefix + "_namespace"), "");
                link.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name3", prefix + "_namespace"), prefix + "_value");
                link.AttributeExtensions.Add(new XmlQualifiedName(prefix + "_name4", "xmlns"), "");

                link.BaseUri = new Uri("http://" + prefix + "_url.com");

                link.ElementExtensions.Add(new ExtensionObject { Value = 10 });

                link.Length = 10;

                link.MediaType = prefix + "_mediaType";

                link.RelationshipType = prefix + "_relationshipType";

                link.Title = prefix + "_title";

                link.Uri = new Uri("http://" + prefix +"_uri.com");

                return link;
            }

            var attributeSyndicationCategory = new SyndicationCategory
            {
                Name = "name",
                Label = "label",
                Scheme = "scheme"
            };
            attributeSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("term"), "term_value");
            attributeSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("label"), "label_value");
            attributeSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("scheme"), "scheme_value");

            var attributeSyndicationLink = new SyndicationLink
            {
                RelationshipType = "link_relationshipType",
                MediaType = "link_mediaType",
                Title = "link_title",
                Length = 10,
                Uri = new Uri("http://link_uri.com")
            };
            attributeSyndicationLink.AttributeExtensions.Add(new XmlQualifiedName("rel"), "rel_value");
            attributeSyndicationLink.AttributeExtensions.Add(new XmlQualifiedName("type"), "type_value");
            attributeSyndicationLink.AttributeExtensions.Add(new XmlQualifiedName("title"), "title_value");
            attributeSyndicationLink.AttributeExtensions.Add(new XmlQualifiedName("length"), "100");
            attributeSyndicationLink.AttributeExtensions.Add(new XmlQualifiedName("href"), "href_value");

            var fullSyndicationItem = new SyndicationItem();
            fullSyndicationItem.AttributeExtensions.Add(new XmlQualifiedName("item_name1"), null);
            fullSyndicationItem.AttributeExtensions.Add(new XmlQualifiedName("item_name2", "item_namespace"), "");
            fullSyndicationItem.AttributeExtensions.Add(new XmlQualifiedName("item_name3", "item_namespace"), "item_value");
            fullSyndicationItem.AttributeExtensions.Add(new XmlQualifiedName("item_name4", "xmlns"), "");

            fullSyndicationItem.Authors.Add(new SyndicationPerson());
            fullSyndicationItem.Authors.Add(CreatePerson("author"));

            fullSyndicationItem.BaseUri = new Uri("http://microsoft/relative");

            fullSyndicationItem.Categories.Add(new SyndicationCategory());
            fullSyndicationItem.Categories.Add(CreateCategory(""));
            fullSyndicationItem.Categories.Add(attributeSyndicationCategory);

            fullSyndicationItem.Content = CreateContent("content");

            fullSyndicationItem.Contributors.Add(new SyndicationPerson());
            fullSyndicationItem.Contributors.Add(CreatePerson("contributor"));

            fullSyndicationItem.Copyright = CreateContent("copyright");

            fullSyndicationItem.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            fullSyndicationItem.Id = "id";

            fullSyndicationItem.LastUpdatedTime = DateTimeOffset.MinValue.AddTicks(100);

            fullSyndicationItem.Links.Add(new SyndicationLink());
            fullSyndicationItem.Links.Add(CreateLink("link"));
            fullSyndicationItem.Links.Add(attributeSyndicationLink);

            fullSyndicationItem.PublishDate = DateTimeOffset.MinValue.AddTicks(200);

            fullSyndicationItem.Summary = CreateContent("summary");

            fullSyndicationItem.Title = CreateContent("title");

            var fullSyndicationFeed = new SyndicationFeed();

            fullSyndicationFeed.AttributeExtensions.Add(new XmlQualifiedName("feed_name1"), null);
            fullSyndicationFeed.AttributeExtensions.Add(new XmlQualifiedName("feed_name2", "feed_namespace"), "");
            fullSyndicationFeed.AttributeExtensions.Add(new XmlQualifiedName("feed_name3", "feed_namespace"), "feed_value");
            fullSyndicationFeed.AttributeExtensions.Add(new XmlQualifiedName("feed_name4", "xmlns"), "");

            fullSyndicationFeed.Authors.Add(new SyndicationPerson());
            fullSyndicationFeed.Authors.Add(CreatePerson("feedauthor"));

            fullSyndicationFeed.BaseUri = new Uri("http://microsoft.com");

            fullSyndicationFeed.Categories.Add(new SyndicationCategory());
            fullSyndicationFeed.Categories.Add(CreateCategory("feed"));
            fullSyndicationItem.Categories.Add(attributeSyndicationCategory);

            fullSyndicationFeed.Contributors.Add(new SyndicationPerson());
            fullSyndicationFeed.Contributors.Add(CreatePerson("feedauthor_"));

            fullSyndicationFeed.Copyright = CreateContent("feedcopyright");

            fullSyndicationFeed.Description = CreateContent("feeddescription");

            fullSyndicationFeed.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            fullSyndicationFeed.Generator = "generator";

            fullSyndicationFeed.Id = "id";

            fullSyndicationFeed.ImageUrl = new Uri("http://imageurl.com");

            fullSyndicationFeed.Items = new SyndicationItem[] { new SyndicationItem() { Id = "id", LastUpdatedTime = DateTimeOffset.MinValue.AddTicks(1) }, fullSyndicationItem };

            fullSyndicationFeed.Language = "language";

            fullSyndicationFeed.LastUpdatedTime = DateTimeOffset.MinValue.AddYears(1);

            fullSyndicationFeed.Links.Add(new SyndicationLink());
            fullSyndicationFeed.Links.Add(CreateLink("syndicationlink"));
            fullSyndicationFeed.Links.Add(attributeSyndicationLink);

            fullSyndicationFeed.Title = CreateContent("feedtitle");

            foreach (bool serializeExtensionsAsAtom in new bool[] { true, false })
            {
                yield return new object[]
                {
                    fullSyndicationFeed,
                    serializeExtensionsAsAtom,
@"    <channel xml:base=""http://microsoft.com/"" feed_name1="""" d2p1:feed_name2="""" d2p1:feed_name3=""feed_value"" d2p2:feed_name4="""" xmlns:d2p2=""xmlns"" xmlns:d2p1=""feed_namespace"">
        <title>feedtitle_title</title>
        <description>feeddescription_title</description>
        <language>language</language>
        <copyright>feedcopyright_title</copyright>
        <a10:author />
        <a10:author feedauthor_name1="""" d3p1:feedauthor_name2="""" d3p1:feedauthor_name3=""feedauthor_value"" d2p2:feedauthor_name4="""" xmlns:d3p1=""feedauthor_namespace"">
            <a10:name>feedauthor_name</a10:name>
            <a10:uri>feedauthor_uri</a10:uri>
            <a10:email>feedauthor_email</a10:email>
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </a10:author>
        <lastBuildDate>Tue, 01 Jan 0002 00:00:00 Z</lastBuildDate>
        <category />
        <category feedcategory_name1="""" d3p1:feedcategory_name2="""" d3p1:feedcategory_name3=""feedcategory_value"" d2p2:feedcategory_name4="""" domain=""feedcategory_scheme"" xmlns:d3p1=""feedcategory_namespace"">feedcategory_name</category>
        <generator>generator</generator>
        <a10:contributor />
        <a10:contributor feedauthor__name1="""" d3p1:feedauthor__name2="""" d3p1:feedauthor__name3=""feedauthor__value"" d2p2:feedauthor__name4="""" xmlns:d3p1=""feedauthor__namespace"">
            <a10:name>feedauthor__name</a10:name>
            <a10:uri>feedauthor__uri</a10:uri>
            <a10:email>feedauthor__email</a10:email>
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </a10:contributor>
        <image>
            <url>http://imageurl.com/</url>
            <title>feedtitle_title</title>
            <link />
        </image>
        <a10:id>id</a10:id>
        <a10:link href="""" />
        <a10:link xml:base=""http://syndicationlink_url.com/"" syndicationlink_name1="""" d3p1:syndicationlink_name2="""" d3p1:syndicationlink_name3=""syndicationlink_value"" d2p2:syndicationlink_name4="""" rel=""syndicationlink_relationshipType"" type=""syndicationlink_mediaType"" title=""syndicationlink_title"" length=""10"" href=""http://syndicationlink_uri.com/"" xmlns:d3p1=""syndicationlink_namespace"">
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </a10:link>
        <a10:link rel=""rel_value"" type=""type_value"" title=""title_value"" length=""100"" href=""href_value"" />
        <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20FeedFormatterTests.ExtensionObject>
        <item>
            <guid isPermaLink=""false"">id</guid>
            <description />
            <a10:updated>0001-01-01T00:00:00Z</a10:updated>
        </item>
        <item xml:base=""http://microsoft/relative"" item_name1="""" d3p1:item_name2="""" d3p1:item_name3=""item_value"" d2p2:item_name4="""" xmlns:d3p1=""item_namespace"">
            <guid isPermaLink=""false"">id</guid>
            <a10:author />
            <a10:author author_name1="""" d4p1:author_name2="""" d4p1:author_name3=""author_value"" d2p2:author_name4="""" xmlns:d4p1=""author_namespace"">
                <a10:name>author_name</a10:name>
                <a10:uri>author_uri</a10:uri>
                <a10:email>author_email</a10:email>
                <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </Rss20FeedFormatterTests.ExtensionObject>
            </a10:author>
            <category />
            <category category_name1="""" d4p1:category_name2="""" d4p1:category_name3=""category_value"" d2p2:category_name4="""" domain=""category_scheme"" xmlns:d4p1=""category_namespace"">category_name</category>
            <category term=""term_value"" label=""label_value"" scheme=""scheme_value"" domain=""scheme"">name</category>
            <category term=""term_value"" label=""label_value"" scheme=""scheme_value"" domain=""scheme"">name</category>
            <title>title_title</title>
            <description>summary_title</description>
            <pubDate>Mon, 01 Jan 0001 00:00:00 Z</pubDate>
            <a10:link href="""" />
            <a10:link xml:base=""http://link_url.com/"" link_name1="""" d4p1:link_name2="""" d4p1:link_name3=""link_value"" d2p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d4p1=""link_namespace"">
                <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </Rss20FeedFormatterTests.ExtensionObject>
            </a10:link>
            <a10:link rel=""rel_value"" type=""type_value"" title=""title_value"" length=""100"" href=""href_value"" />
            <a10:updated>0001-01-01T00:00:00Z</a10:updated>
            <a10:rights type=""html"" copyright_name1="""" d4p1:copyright_name2="""" d4p1:copyright_name3=""copyright_value"" d2p2:copyright_name4="""" xmlns:d4p1=""copyright_namespace"">copyright_title</a10:rights>
            <a10:content type=""html"" content_name1="""" d4p1:content_name2="""" d4p1:content_name3=""content_value"" d2p2:content_name4="""" xmlns:d4p1=""content_namespace"">content_title</a10:content>
            <a10:contributor />
            <a10:contributor contributor_name1="""" d4p1:contributor_name2="""" d4p1:contributor_name3=""contributor_value"" d2p2:contributor_name4="""" xmlns:d4p1=""contributor_namespace"">
                <a10:name>contributor_name</a10:name>
                <a10:uri>contributor_uri</a10:uri>
                <a10:email>contributor_email</a10:email>
                <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </Rss20FeedFormatterTests.ExtensionObject>
            </a10:contributor>
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </item>
    </channel>"
                };
            }
        }

        [Theory]
        [MemberData(nameof(WriteTo_TestData))]
        public void Write_HasFeed_SerializesExpected(SyndicationFeed feed, bool serializeExtensionsAsAtom, string expected)
        {
            var formatter = new Rss20FeedFormatter(feed);
            string expectedFull = @"<rss xmlns:a10=""http://www.w3.org/2005/Atom"" version=""2.0"">" + Environment.NewLine + expected + Environment.NewLine + "</rss>";
            string expectedSerializable = @"<feed xmlns:a10=""http://www.w3.org/2005/Atom"" version=""2.0"">" + Environment.NewLine + expected + Environment.NewLine + "</feed>";

            CompareHelper.AssertEqualWriteOutput(expectedFull, writer => formatter.WriteTo(writer));
            CompareHelper.AssertEqualWriteOutput(expectedFull, writer => feed.SaveAsRss20(writer));
            CompareHelper.AssertEqualWriteOutput(expectedSerializable, writer =>
            {
                writer.WriteStartElement("feed");
                ((IXmlSerializable)formatter).WriteXml(writer);
                writer.WriteEndElement();
            });

            var genericFormatter = new Rss20FeedFormatter<SyndicationFeed>(feed);
            CompareHelper.AssertEqualWriteOutput(expectedFull, writer => formatter.WriteTo(writer));
            CompareHelper.AssertEqualWriteOutput(expectedFull, writer => feed.SaveAsRss20(writer));
        }

        [Fact]
        public void WriteTo_NullWriter_ThrowsArgumentNullException()
        {
            var formatter = new Rss20FeedFormatter(new SyndicationFeed());
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteTo(null));
        }

        [Fact]
        public void WriteTo_NoItem_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Rss20FeedFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteTo(writer));
            }
        }

        [Fact]
        public void WriteXml_NullWriter_ThrowsArgumentNullException()
        {
            IXmlSerializable formatter = new Rss20FeedFormatter();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteXml(null));
        }

        [Fact]
        public void WriteXml_NoItem_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                IXmlSerializable formatter = new Rss20FeedFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteXml(writer));
            }
        }

        public static IEnumerable<object[]> FeedBaseUri_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Uri("http://microsoft.com") };
            yield return new object[] { new Uri("/relative", UriKind.Relative) };
        }

        [Theory]
        [MemberData(nameof(FeedBaseUri_TestData))]
        public void WriteItem_Invoke_Success(Uri feedBaseUri)
        {
            var formatter = new Formatter();
            var item = new SyndicationItem() { Id = "id", LastUpdatedTime = DateTimeOffset.MinValue.AddTicks(1) };
            CompareHelper.AssertEqualWriteOutput(
@"<item>
    <guid isPermaLink=""false"">id</guid>
    <description />
    <updated xmlns=""http://www.w3.org/2005/Atom"">0001-01-01T00:00:00Z</updated>
</item>", writer => formatter.WriteItemEntryPoint(writer, item, feedBaseUri));
        }

        [Fact]
        public void WriteItem_NullWriter_ThrowsNullReferenceException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Formatter();
                Assert.Throws<NullReferenceException>(() => formatter.WriteItemEntryPoint(null, new SyndicationItem(), new Uri("http://microsoft.com")));
            }
        }

        [Fact]
        public void WriteItem_NullItem_ThrowsNullReferenceException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Formatter();
                Assert.Throws<NullReferenceException>(() => formatter.WriteItemEntryPoint(writer, null, new Uri("http://microsoft.com")));
            }
        }

        [Theory]
        [MemberData(nameof(FeedBaseUri_TestData))]
        public void WriteItems_Invoke_Success(Uri feedBaseUri)
        {
            var formatter = new Formatter();
            var items = new SyndicationItem[]
            {
                new SyndicationItem() { Id = "id1", LastUpdatedTime = DateTimeOffset.MinValue.AddTicks(1) },
                new SyndicationItem() { Id = "id2", LastUpdatedTime = DateTimeOffset.MinValue.AddTicks(1) }
            };
            CompareHelper.AssertEqualWriteOutput(
@"<item>
    <guid isPermaLink=""false"">id1</guid>
    <description />
    <updated xmlns=""http://www.w3.org/2005/Atom"">0001-01-01T00:00:00Z</updated>
</item>
<item>
    <guid isPermaLink=""false"">id2</guid>
    <description />
    <updated xmlns=""http://www.w3.org/2005/Atom"">0001-01-01T00:00:00Z</updated>
</item>", writer => formatter.WriteItemsEntryPoint(writer, items, feedBaseUri));
        }

        [Fact]
        public void WriteItems_NullItems_Nop()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Formatter();
                formatter.WriteItemsEntryPoint(writer, null, new Uri("http://microsoft.com"));
                formatter.WriteItemsEntryPoint(null, null, new Uri("http://microsoft.com"));
            }
        }

        [Fact]
        public void WriteItems_EmptyItems_Nop()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Formatter();
                formatter.WriteItemsEntryPoint(writer, new SyndicationItem[0], new Uri("http://microsoft.com"));
                formatter.WriteItemsEntryPoint(null, new SyndicationItem[0], new Uri("http://microsoft.com"));
            }
        }

        [Fact]
        public void WriteItems_NullWriter_ThrowsNullReferenceException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Formatter();
                var items = new SyndicationItem[] { new SyndicationItem() };
                Assert.Throws<NullReferenceException>(() => formatter.WriteItemsEntryPoint(null, items, new Uri("http://microsoft.com")));
            }
        }

        [Fact]
        public void WriteItems_NullItemInItems_ThrowsNullReferenceException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Formatter();
                var items = new SyndicationItem[] { null };
                Assert.Throws<NullReferenceException>(() => formatter.WriteItemsEntryPoint(writer, items, new Uri("http://microsoft.com")));
            }
        }

        public static IEnumerable<object[]> CanRead_TestData()
        {
            yield return new object[] { @"<rss />", true };
            yield return new object[] { @"<rss xmlns=""different"" />", false };
            yield return new object[] { @"<different xmlns="""" />", false };
            yield return new object[] { @"<rss xmlns="""" />", true };
            yield return new object[] { @"<rss xmlns=""""></rss>", true };
        }

        [Theory]
        [MemberData(nameof(CanRead_TestData))]
        public void CanRead_ValidReader_ReturnsExpected(string xmlString, bool expected)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20FeedFormatter();
                Assert.Equal(expected, formatter.CanRead(reader));
            }
        }

        [Fact]
        public void CanRead_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new Rss20FeedFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.CanRead(null));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Read_FullItem_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            VerifyRead(
@"<rss xmlns:a10=""http://www.w3.org/2005/Atom"" version=""2.0"">
    <channel xml:base=""http://microsoft.com/"" feed_name1="""" d2p1:feed_name2="""" d2p1:feed_name3=""feed_value"" d2p2:feed_name4="""" xmlns:d2p2=""xmlns"" xmlns:d2p1=""feed_namespace"">
        <title>feedtitle_title</title>
        <description>feeddescription_title</description>
        <language>language</language>
        <copyright>feedcopyright_title</copyright>
        <a10:author />
        <a10:author feedauthor_name1="""" d3p1:feedauthor_name2="""" d3p1:feedauthor_name3=""feedauthor_value"" d2p2:feedauthor_name4="""" xmlns:d3p1=""feedauthor_namespace"">
            <a10:name>feedauthor_name</a10:name>
            <a10:uri>feedauthor_uri</a10:uri>
            <a10:email>feedauthor_email</a10:email>
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </a10:author>
        <lastBuildDate>Tue, 01 Jan 0002 00:00:00 Z</lastBuildDate>
        <category />
        <category feedcategory_name1="""" d3p1:feedcategory_name2="""" d3p1:feedcategory_name3=""feedcategory_value"" d2p2:feedcategory_name4="""" domain=""feedcategory_scheme"" xmlns:d3p1=""feedcategory_namespace"">feedcategory_name</category>
        <generator>generator</generator>
        <a10:contributor />
        <a10:contributor feedauthor__name1="""" d3p1:feedauthor__name2="""" d3p1:feedauthor__name3=""feedauthor__value"" d2p2:feedauthor__name4="""" xmlns:d3p1=""feedauthor__namespace"">
            <a10:name>feedauthor__name</a10:name>
            <a10:uri>feedauthor__uri</a10:uri>
            <a10:email>feedauthor__email</a10:email>
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </a10:contributor>
        <image>
            <url>http://imageurl.com/</url>
            <title>feedtitle_title</title>
            <link />
        </image>
        <a10:id>id</a10:id>
        <a10:link href="""" />
        <a10:link xml:base=""http://syndicationlink_url.com/"" syndicationlink_name1="""" d3p1:syndicationlink_name2="""" d3p1:syndicationlink_name3=""syndicationlink_value"" d2p2:syndicationlink_name4="""" rel=""syndicationlink_relationshipType"" type=""syndicationlink_mediaType"" title=""syndicationlink_title"" length=""10"" href=""http://syndicationlink_uri.com/"" xmlns:d3p1=""syndicationlink_namespace"">
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </a10:link>
        <a10:link rel=""rel_value"" type=""type_value"" title=""title_value"" length=""100"" href=""href_value"" />
        <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20FeedFormatterTests.ExtensionObject>
        <item>
            <guid isPermaLink=""false"">id</guid>
            <description />
            <a10:updated>0001-01-01T00:00:00Z</a10:updated>
        </item>
        <item xml:base=""http://microsoft/relative"" item_name1="""" d3p1:item_name2="""" d3p1:item_name3=""item_value"" d2p2:item_name4="""" xmlns:d3p1=""item_namespace"">
            <guid isPermaLink=""false"">id</guid>
            <a10:author />
            <a10:author author_name1="""" d4p1:author_name2="""" d4p1:author_name3=""author_value"" d2p2:author_name4="""" xmlns:d4p1=""author_namespace"">
                <a10:name>author_name</a10:name>
                <a10:uri>author_uri</a10:uri>
                <a10:email>author_email</a10:email>
                <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </Rss20FeedFormatterTests.ExtensionObject>
            </a10:author>
            <category />
            <category category_name1="""" d4p1:category_name2="""" d4p1:category_name3=""category_value"" d2p2:category_name4="""" domain=""category_scheme"" xmlns:d4p1=""category_namespace"">category_name</category>
            <category term=""term_value"" label=""label_value"" scheme=""scheme_value"" domain=""scheme"">name</category>
            <category term=""term_value"" label=""label_value"" scheme=""scheme_value"" domain=""scheme"">name</category>
            <title>title_title</title>
            <description>summary_title</description>
            <pubDate>Mon, 01 Jan 0001 00:00:00 Z</pubDate>
            <a10:link href="""" />
            <a10:link xml:base=""http://link_url.com/"" link_name1="""" d4p1:link_name2="""" d4p1:link_name3=""link_value"" d2p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d4p1=""link_namespace"">
                <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </Rss20FeedFormatterTests.ExtensionObject>
            </a10:link>
            <a10:link rel=""rel_value"" type=""type_value"" title=""title_value"" length=""100"" href=""href_value"" />
            <a10:updated>0001-01-01T00:00:00Z</a10:updated>
            <a10:rights type=""html"" copyright_name1="""" d4p1:copyright_name2="""" d4p1:copyright_name3=""copyright_value"" d2p2:copyright_name4="""" xmlns:d4p1=""copyright_namespace"">copyright_title</a10:rights>
            <a10:content type=""html"" content_name1="""" d4p1:content_name2="""" d4p1:content_name3=""content_value"" d2p2:content_name4="""" xmlns:d4p1=""content_namespace"">content_title</a10:content>
            <a10:contributor />
            <a10:contributor contributor_name1="""" d4p1:contributor_name2="""" d4p1:contributor_name3=""contributor_value"" d2p2:contributor_name4="""" xmlns:d4p1=""contributor_namespace"">
                <a10:name>contributor_name</a10:name>
                <a10:uri>contributor_uri</a10:uri>
                <a10:email>contributor_email</a10:email>
                <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </Rss20FeedFormatterTests.ExtensionObject>
            </a10:contributor>
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </item>
    </channel>
</rss>", preserveAttributeExtensions, preserveElementExtensions, feed =>
            {
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, feed.AttributeExtensions.Count);
                    Assert.Equal(4, feed.AttributeExtensions.Count);
                    Assert.Equal("", feed.AttributeExtensions[new XmlQualifiedName("feed_name1")]);
                    Assert.Equal("", feed.AttributeExtensions[new XmlQualifiedName("feed_name2", "feed_namespace")]);
                    Assert.Equal("feed_value", feed.AttributeExtensions[new XmlQualifiedName("feed_name3", "feed_namespace")]);
                    Assert.Equal("", feed.AttributeExtensions[new XmlQualifiedName("feed_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(feed.AttributeExtensions);
                }

                Assert.Equal(2, feed.Authors.Count);

                SyndicationPerson firstAuthor = feed.Authors[0];
                Assert.Empty(firstAuthor.AttributeExtensions);
                Assert.Empty(firstAuthor.ElementExtensions);
                Assert.Null(firstAuthor.Email);
                Assert.Null(firstAuthor.Name);
                Assert.Null(firstAuthor.Uri);

                SyndicationPerson secondAuthor = feed.Authors[1];
                Assert.Equal(4, secondAuthor.AttributeExtensions.Count);
                Assert.Equal(4, secondAuthor.AttributeExtensions.Count);
                Assert.Equal("", secondAuthor.AttributeExtensions[new XmlQualifiedName("feedauthor_name1")]);
                Assert.Equal("", secondAuthor.AttributeExtensions[new XmlQualifiedName("feedauthor_name2", "feedauthor_namespace")]);
                Assert.Equal("feedauthor_value", secondAuthor.AttributeExtensions[new XmlQualifiedName("feedauthor_name3", "feedauthor_namespace")]);
                Assert.Equal("", secondAuthor.AttributeExtensions[new XmlQualifiedName("feedauthor_name4", "xmlns")]);
                Assert.Equal(1, secondAuthor.ElementExtensions.Count());
                Assert.Equal("feedauthor_email", secondAuthor.Email);
                Assert.Equal("feedauthor_name", secondAuthor.Name);
                Assert.Equal("feedauthor_uri", secondAuthor.Uri);

                Assert.Equal(new Uri("http://microsoft.com"), feed.BaseUri);

                Assert.Equal(2, feed.Categories.Count);
                SyndicationCategory firstCategory = feed.Categories[0];
                Assert.Empty(firstCategory.AttributeExtensions);
                Assert.Empty(firstCategory.ElementExtensions);
                Assert.Null(firstCategory.Name);
                Assert.Null(firstCategory.Scheme);
                Assert.Null(firstCategory.Label);

                SyndicationCategory secondCategory = feed.Categories[1];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, secondCategory.AttributeExtensions.Count);
                    Assert.Equal(4, secondCategory.AttributeExtensions.Count);
                    Assert.Equal("", secondCategory.AttributeExtensions[new XmlQualifiedName("feedcategory_name1")]);
                    Assert.Equal("", secondCategory.AttributeExtensions[new XmlQualifiedName("feedcategory_name2", "feedcategory_namespace")]);
                    Assert.Equal("feedcategory_value", secondCategory.AttributeExtensions[new XmlQualifiedName("feedcategory_name3", "feedcategory_namespace")]);
                    Assert.Equal("", secondCategory.AttributeExtensions[new XmlQualifiedName("feedcategory_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(secondCategory.AttributeExtensions);
                }
                Assert.Empty(secondCategory.ElementExtensions);
                Assert.Equal("feedcategory_name", secondCategory.Name);
                Assert.Equal("feedcategory_scheme", secondCategory.Scheme);
                Assert.Null(secondCategory.Label);

                Assert.Equal(2, feed.Contributors.Count);
                SyndicationPerson firstContributor = feed.Contributors[0];
                Assert.Empty(firstContributor.AttributeExtensions);
                Assert.Empty(firstContributor.ElementExtensions);
                Assert.Null(firstContributor.Email);
                Assert.Null(firstContributor.Name);
                Assert.Null(firstContributor.Uri);

                SyndicationPerson secondContributor = feed.Contributors[1];
                Assert.Equal(4, secondContributor.AttributeExtensions.Count);
                Assert.Equal(4, secondContributor.AttributeExtensions.Count);
                Assert.Equal("", secondContributor.AttributeExtensions[new XmlQualifiedName("feedauthor__name1")]);
                Assert.Equal("", secondContributor.AttributeExtensions[new XmlQualifiedName("feedauthor__name2", "feedauthor__namespace")]);
                Assert.Equal("feedauthor__value", secondContributor.AttributeExtensions[new XmlQualifiedName("feedauthor__name3", "feedauthor__namespace")]);
                Assert.Equal("", secondContributor.AttributeExtensions[new XmlQualifiedName("feedauthor__name4", "xmlns")]);
                Assert.Equal(1, secondContributor.ElementExtensions.Count());
                Assert.Equal("feedauthor__email", secondContributor.Email);
                Assert.Equal("feedauthor__name", secondContributor.Name);
                Assert.Equal("feedauthor__uri", secondContributor.Uri);

                Assert.Empty(feed.Copyright.AttributeExtensions);
                Assert.Equal("feedcopyright_title", feed.Copyright.Text);
                Assert.Equal("text", feed.Copyright.Type);

                Assert.Equal("generator", feed.Generator);

                if (preserveElementExtensions)
                {
                    Assert.Equal(1, feed.ElementExtensions.Count());
                }
                else
                {
                    Assert.Empty(feed.ElementExtensions);
                }

                Assert.Equal("id", feed.Id);

                Assert.Equal(new Uri("http://imageurl.com/"), feed.ImageUrl);

                SyndicationItem[] items = feed.Items.ToArray();
                Assert.Equal(2, items.Length);

                SyndicationItem item = items[1];

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, item.AttributeExtensions.Count);
                    Assert.Equal(4, item.AttributeExtensions.Count);
                    Assert.Equal("", item.AttributeExtensions[new XmlQualifiedName("item_name1")]);
                    Assert.Equal("", item.AttributeExtensions[new XmlQualifiedName("item_name2", "item_namespace")]);
                    Assert.Equal("item_value", item.AttributeExtensions[new XmlQualifiedName("item_name3", "item_namespace")]);
                    Assert.Equal("", item.AttributeExtensions[new XmlQualifiedName("item_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(item.AttributeExtensions);
                }
                if (preserveElementExtensions)
                {
                    Assert.Equal(1, item.ElementExtensions.Count());
                }
                else
                {
                    Assert.Empty(item.ElementExtensions);
                }

                Assert.Equal(2, item.Authors.Count);

                SyndicationPerson itemFirstAuthor = item.Authors[0];
                Assert.Empty(itemFirstAuthor.AttributeExtensions);
                Assert.Empty(itemFirstAuthor.ElementExtensions);
                Assert.Null(itemFirstAuthor.Email);
                Assert.Null(itemFirstAuthor.Name);
                Assert.Null(itemFirstAuthor.Uri);

                SyndicationPerson itemSecondAuthor = item.Authors[1];

                Assert.Equal(4, itemSecondAuthor.AttributeExtensions.Count);
                Assert.Equal(4, itemSecondAuthor.AttributeExtensions.Count);
                Assert.Equal("", itemSecondAuthor.AttributeExtensions[new XmlQualifiedName("author_name1")]);
                Assert.Equal("", itemSecondAuthor.AttributeExtensions[new XmlQualifiedName("author_name2", "author_namespace")]);
                Assert.Equal("author_value", itemSecondAuthor.AttributeExtensions[new XmlQualifiedName("author_name3", "author_namespace")]);
                Assert.Equal("", itemSecondAuthor.AttributeExtensions[new XmlQualifiedName("author_name4", "xmlns")]);
                Assert.Equal(1, itemSecondAuthor.ElementExtensions.Count());
                Assert.Equal("author_email", itemSecondAuthor.Email);
                Assert.Equal("author_name", itemSecondAuthor.Name);
                Assert.Equal("author_uri", itemSecondAuthor.Uri);

                Assert.Equal(new Uri("http://microsoft/relative"), item.BaseUri);

                Assert.Equal(4, item.Categories.Count);
                SyndicationCategory itemFirstCategory = item.Categories[0];
                Assert.Empty(itemFirstCategory.AttributeExtensions);
                Assert.Empty(itemFirstCategory.ElementExtensions);
                Assert.Null(itemFirstCategory.Name);
                Assert.Null(itemFirstCategory.Scheme);
                Assert.Null(itemFirstCategory.Label);

                SyndicationCategory itemSecondCategory = item.Categories[1];

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, itemSecondCategory.AttributeExtensions.Count);
                    Assert.Equal(4, itemSecondCategory.AttributeExtensions.Count);
                    Assert.Equal("", itemSecondCategory.AttributeExtensions[new XmlQualifiedName("category_name1")]);
                    Assert.Equal("", itemSecondCategory.AttributeExtensions[new XmlQualifiedName("category_name2", "category_namespace")]);
                    Assert.Equal("category_value", itemSecondCategory.AttributeExtensions[new XmlQualifiedName("category_name3", "category_namespace")]);
                    Assert.Equal("", itemSecondCategory.AttributeExtensions[new XmlQualifiedName("category_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(itemSecondCategory.AttributeExtensions);
                }
                Assert.Empty(itemSecondCategory.ElementExtensions);
                Assert.Equal("category_name", itemSecondCategory.Name);
                Assert.Equal("category_scheme", itemSecondCategory.Scheme);
                Assert.Null(itemSecondCategory.Label);

                SyndicationCategory itemThirdCategory = item.Categories[2];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(3, itemThirdCategory.AttributeExtensions.Count);
                    Assert.Equal("term_value", itemThirdCategory.AttributeExtensions[new XmlQualifiedName("term")]);
                    Assert.Equal("label_value", itemThirdCategory.AttributeExtensions[new XmlQualifiedName("label")]);
                    Assert.Equal("scheme_value", itemThirdCategory.AttributeExtensions[new XmlQualifiedName("scheme")]);
                }
                else
                {
                    Assert.Empty(itemThirdCategory.AttributeExtensions);
                }
                Assert.Empty(itemThirdCategory.ElementExtensions);
                Assert.Equal("name", itemThirdCategory.Name);
                Assert.Equal("scheme", itemThirdCategory.Scheme);
                Assert.Null(itemThirdCategory.Label);

                SyndicationCategory itemFourthCategory = item.Categories[3];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(3, itemFourthCategory.AttributeExtensions.Count);
                    Assert.Equal("term_value", itemFourthCategory.AttributeExtensions[new XmlQualifiedName("term")]);
                    Assert.Equal("label_value", itemFourthCategory.AttributeExtensions[new XmlQualifiedName("label")]);
                    Assert.Equal("scheme_value", itemFourthCategory.AttributeExtensions[new XmlQualifiedName("scheme")]);
                }
                else
                {
                    Assert.Empty(itemFourthCategory.AttributeExtensions);
                }
                Assert.Empty(itemFourthCategory.ElementExtensions);
                Assert.Equal("name", itemFourthCategory.Name);
                Assert.Equal("scheme", itemFourthCategory.Scheme);
                Assert.Null(itemFourthCategory.Label);

                TextSyndicationContent content = Assert.IsType<TextSyndicationContent>(item.Content);

                Assert.Equal(4, content.AttributeExtensions.Count);
                Assert.Equal(4, content.AttributeExtensions.Count);
                Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name1")]);
                Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name2", "content_namespace")]);
                Assert.Equal("content_value", content.AttributeExtensions[new XmlQualifiedName("content_name3", "content_namespace")]);
                Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name4", "xmlns")]);
                Assert.Equal("content_title", content.Text);
                Assert.Equal("html", content.Type);

                Assert.Equal(2, item.Contributors.Count);

                SyndicationPerson itemFirstContributor = item.Contributors[0];
                Assert.Empty(itemFirstContributor.AttributeExtensions);
                Assert.Empty(itemFirstContributor.ElementExtensions);
                Assert.Null(itemFirstContributor.Email);
                Assert.Null(itemFirstContributor.Name);
                Assert.Null(itemFirstContributor.Uri);

                SyndicationPerson itemSecondContributor = item.Contributors[1];
                Assert.Equal(4, itemSecondContributor.AttributeExtensions.Count);
                Assert.Equal("", itemSecondContributor.AttributeExtensions[new XmlQualifiedName("contributor_name1")]);
                Assert.Equal("", itemSecondContributor.AttributeExtensions[new XmlQualifiedName("contributor_name2", "contributor_namespace")]);
                Assert.Equal("contributor_value", itemSecondContributor.AttributeExtensions[new XmlQualifiedName("contributor_name3", "contributor_namespace")]);
                Assert.Equal("", itemSecondContributor.AttributeExtensions[new XmlQualifiedName("contributor_name4", "xmlns")]);
                Assert.Equal(1, itemSecondContributor.ElementExtensions.Count());
                Assert.Equal("contributor_email", itemSecondContributor.Email);
                Assert.Equal("contributor_name", itemSecondContributor.Name);
                Assert.Equal("contributor_uri", itemSecondContributor.Uri);

                Assert.Equal(4, item.Copyright.AttributeExtensions.Count);
                Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name1")]);
                Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name2", "copyright_namespace")]);
                Assert.Equal("copyright_value", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name3", "copyright_namespace")]);
                Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name4", "xmlns")]);
                Assert.Equal("copyright_title", item.Copyright.Text);
                Assert.Equal("html", item.Copyright.Type);

                if (preserveElementExtensions)
                {
                    Assert.Equal(1, item.ElementExtensions.Count());
                }
                else
                {
                    Assert.Empty(item.ElementExtensions);
                }

                Assert.Equal("id", item.Id);

                Assert.Equal(DateTimeOffset.MinValue, item.LastUpdatedTime);

                Assert.Equal(3, item.Links.Count);

                SyndicationLink itemFirstLink = item.Links[0];
                Assert.Empty(itemFirstLink.AttributeExtensions);
                Assert.Empty(itemFirstLink.ElementExtensions);
                Assert.Equal(0, itemFirstLink.Length);
                Assert.Null(itemFirstLink.MediaType);
                Assert.Null(itemFirstLink.RelationshipType);
                Assert.Null(itemFirstLink.Title);
                Assert.Equal(string.Empty, itemFirstLink.Uri.OriginalString);

                SyndicationLink itemSecondLink = item.Links[1];
                Assert.Equal(4, itemSecondLink.AttributeExtensions.Count);
                Assert.Equal("", itemSecondLink.AttributeExtensions[new XmlQualifiedName("link_name1")]);
                Assert.Equal("", itemSecondLink.AttributeExtensions[new XmlQualifiedName("link_name2", "link_namespace")]);
                Assert.Equal("link_value", itemSecondLink.AttributeExtensions[new XmlQualifiedName("link_name3", "link_namespace")]);
                Assert.Equal("", itemSecondLink.AttributeExtensions[new XmlQualifiedName("link_name4", "xmlns")]);
                Assert.Equal(1, itemSecondLink.ElementExtensions.Count());
                Assert.Equal(new Uri("http://link_url.com"), itemSecondLink.BaseUri);
                Assert.Equal(10, itemSecondLink.Length);
                Assert.Equal("link_mediaType", itemSecondLink.MediaType);
                Assert.Equal("link_relationshipType", itemSecondLink.RelationshipType);
                Assert.Equal("link_title", itemSecondLink.Title);
                Assert.Equal(new Uri("http://link_uri.com"), itemSecondLink.Uri);

                SyndicationLink itemThirdLink = item.Links[2];
                Assert.Empty(itemThirdLink.AttributeExtensions);
                Assert.Empty(itemThirdLink.ElementExtensions);
                Assert.Equal(100, itemThirdLink.Length);
                Assert.Equal("type_value", itemThirdLink.MediaType);
                Assert.Equal("rel_value", itemThirdLink.RelationshipType);
                Assert.Equal("title_value", itemThirdLink.Title);
                Assert.Equal("href_value", itemThirdLink.Uri.OriginalString);

                Assert.Equal(DateTimeOffset.MinValue, item.PublishDate);

                Assert.Null(item.SourceFeed);

                Assert.Empty(item.Summary.AttributeExtensions);
                Assert.Equal("summary_title", item.Summary.Text);
                Assert.Equal("text", item.Summary.Type);

                Assert.Empty(item.Title.AttributeExtensions);
                Assert.Equal("title_title", item.Title.Text);
                Assert.Equal("text", item.Title.Type);

                Assert.Equal("language", feed.Language);

                Assert.Equal(DateTimeOffset.MinValue.AddYears(1), feed.LastUpdatedTime);
                Assert.Equal(3, feed.Links.Count);

                SyndicationLink firstLink = feed.Links[0];
                Assert.Empty(firstLink.AttributeExtensions);
                Assert.Empty(firstLink.ElementExtensions);
                Assert.Equal(0, firstLink.Length);
                Assert.Null(firstLink.MediaType);
                Assert.Null(firstLink.RelationshipType);
                Assert.Null(firstLink.Title);
                Assert.Equal("", firstLink.Uri.OriginalString);

                SyndicationLink secondLink = feed.Links[1];
                Assert.Equal(4, secondLink.AttributeExtensions.Count);
                Assert.Equal("", secondLink.AttributeExtensions[new XmlQualifiedName("syndicationlink_name1")]);
                Assert.Equal("", secondLink.AttributeExtensions[new XmlQualifiedName("syndicationlink_name2", "syndicationlink_namespace")]);
                Assert.Equal("syndicationlink_value", secondLink.AttributeExtensions[new XmlQualifiedName("syndicationlink_name3", "syndicationlink_namespace")]);
                Assert.Equal("", secondLink.AttributeExtensions[new XmlQualifiedName("syndicationlink_name4", "xmlns")]);
                Assert.Equal(1, secondLink.ElementExtensions.Count());
                Assert.Equal(new Uri("http://syndicationlink_url.com/"), secondLink.BaseUri);
                Assert.Equal(10, secondLink.Length);
                Assert.Equal("syndicationlink_mediaType", secondLink.MediaType);
                Assert.Equal("syndicationlink_relationshipType", secondLink.RelationshipType);
                Assert.Equal("syndicationlink_title", secondLink.Title);
                Assert.Equal("http://syndicationlink_uri.com/", secondLink.Uri.OriginalString);

                SyndicationLink thirdLink = feed.Links[2];
                Assert.Empty(thirdLink.AttributeExtensions);
                Assert.Empty(thirdLink.ElementExtensions);
                Assert.Equal(100, thirdLink.Length);
                Assert.Equal("type_value", thirdLink.MediaType);
                Assert.Equal("rel_value", thirdLink.RelationshipType);
                Assert.Equal("title_value", thirdLink.Title);
                Assert.Equal("href_value", thirdLink.Uri.OriginalString);

                Assert.Empty(item.Title.AttributeExtensions);
                Assert.Equal("feedtitle_title", feed.Title.Text);
                Assert.Equal("text", feed.Title.Type);
            });
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Read_TryParseTrue_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            using (var stringReader = new StringReader(
@"<rss xmlns:a10=""http://www.w3.org/2005/Atom"" version=""2.0"">
    <channel xml:base=""http://microsoft.com/"" feed_name1="""" d2p1:feed_name2="""" d2p1:feed_name3=""feed_value"" d2p2:feed_name4="""" xmlns:d2p2=""xmlns"" xmlns:d2p1=""feed_namespace"">
        <title>feedtitle_title</title>
        <description>feeddescription_title</description>
        <language>language</language>
        <copyright>feedcopyright_title</copyright>
        <a10:author />
        <a10:author feedauthor_name1="""" d3p1:feedauthor_name2="""" d3p1:feedauthor_name3=""feedauthor_value"" d2p2:feedauthor_name4="""" xmlns:d3p1=""feedauthor_namespace"">
            <a10:name>feedauthor_name</a10:name>
            <a10:uri>feedauthor_uri</a10:uri>
            <a10:email>feedauthor_email</a10:email>
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </a10:author>
        <lastBuildDate>Tue, 01 Jan 0002 00:00:00 Z</lastBuildDate>
        <category />
        <category feedcategory_name1="""" d3p1:feedcategory_name2="""" d3p1:feedcategory_name3=""feedcategory_value"" d2p2:feedcategory_name4="""" domain=""feedcategory_scheme"" xmlns:d3p1=""feedcategory_namespace"">feedcategory_name</category>
        <generator>generator</generator>
        <a10:contributor />
        <a10:contributor feedauthor__name1="""" d3p1:feedauthor__name2="""" d3p1:feedauthor__name3=""feedauthor__value"" d2p2:feedauthor__name4="""" xmlns:d3p1=""feedauthor__namespace"">
            <a10:name>feedauthor__name</a10:name>
            <a10:uri>feedauthor__uri</a10:uri>
            <a10:email>feedauthor__email</a10:email>
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </a10:contributor>
        <image>
            <url>http://imageurl.com/</url>
            <title>feedtitle_title</title>
            <link />
        </image>
        <a10:id>id</a10:id>
        <a10:link href="""" />
        <a10:link xml:base=""http://syndicationlink_url.com/"" syndicationlink_name1="""" d3p1:syndicationlink_name2="""" d3p1:syndicationlink_name3=""syndicationlink_value"" d2p2:syndicationlink_name4="""" rel=""syndicationlink_relationshipType"" type=""syndicationlink_mediaType"" title=""syndicationlink_title"" length=""10"" href=""http://syndicationlink_uri.com/"" xmlns:d3p1=""syndicationlink_namespace"">
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </a10:link>
        <a10:link rel=""rel_value"" type=""type_value"" title=""title_value"" length=""100"" href=""href_value"" />
        <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20FeedFormatterTests.ExtensionObject>
        <item>
            <guid isPermaLink=""false"">id</guid>
            <description />
            <a10:updated>0001-01-01T00:00:00Z</a10:updated>
        </item>
        <item xml:base=""http://microsoft/relative"" item_name1="""" d3p1:item_name2="""" d3p1:item_name3=""item_value"" d2p2:item_name4="""" xmlns:d3p1=""item_namespace"">
            <guid isPermaLink=""false"">id</guid>
            <a10:author />
            <a10:author author_name1="""" d4p1:author_name2="""" d4p1:author_name3=""author_value"" d2p2:author_name4="""" xmlns:d4p1=""author_namespace"">
                <a10:name>author_name</a10:name>
                <a10:uri>author_uri</a10:uri>
                <a10:email>author_email</a10:email>
                <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </Rss20FeedFormatterTests.ExtensionObject>
            </a10:author>
            <category />
            <category category_name1="""" d4p1:category_name2="""" d4p1:category_name3=""category_value"" d2p2:category_name4="""" domain=""category_scheme"" xmlns:d4p1=""category_namespace"">category_name</category>
            <category term=""term_value"" label=""label_value"" scheme=""scheme_value"" domain=""scheme"">name</category>
            <category term=""term_value"" label=""label_value"" scheme=""scheme_value"" domain=""scheme"">name</category>
            <title>title_title</title>
            <description>summary_title</description>
            <pubDate>Mon, 01 Jan 0001 00:00:00 Z</pubDate>
            <a10:link href="""" />
            <a10:link xml:base=""http://link_url.com/"" link_name1="""" d4p1:link_name2="""" d4p1:link_name3=""link_value"" d2p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d4p1=""link_namespace"">
                <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </Rss20FeedFormatterTests.ExtensionObject>
            </a10:link>
            <a10:link rel=""rel_value"" type=""type_value"" title=""title_value"" length=""100"" href=""href_value"" />
            <a10:updated>0001-01-01T00:00:00Z</a10:updated>
            <a10:rights type=""html"" copyright_name1="""" d4p1:copyright_name2="""" d4p1:copyright_name3=""copyright_value"" d2p2:copyright_name4="""" xmlns:d4p1=""copyright_namespace"">copyright_title</a10:rights>
            <a10:content type=""html"" content_name1="""" d4p1:content_name2="""" d4p1:content_name3=""content_value"" d2p2:content_name4="""" xmlns:d4p1=""content_namespace"">content_title</a10:content>
            <a10:contributor />
            <a10:contributor contributor_name1="""" d4p1:contributor_name2="""" d4p1:contributor_name3=""contributor_value"" d2p2:contributor_name4="""" xmlns:d4p1=""contributor_namespace"">
                <a10:name>contributor_name</a10:name>
                <a10:uri>contributor_uri</a10:uri>
                <a10:email>contributor_email</a10:email>
                <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </Rss20FeedFormatterTests.ExtensionObject>
            </a10:contributor>
            <Rss20FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Rss20FeedFormatterTests.ExtensionObject>
        </item>
    </channel>
</rss>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20FeedFormatter<SyndicationFeedTryParseTrueSubclass>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);

                SyndicationFeed feed = formatter.Feed;
                Assert.Empty(feed.AttributeExtensions);

                Assert.Equal(2, feed.Authors.Count);

                SyndicationPerson firstAuthor = feed.Authors[0];
                Assert.Empty(firstAuthor.AttributeExtensions);
                Assert.Empty(firstAuthor.ElementExtensions);
                Assert.Null(firstAuthor.Email);
                Assert.Null(firstAuthor.Name);
                Assert.Null(firstAuthor.Uri);

                SyndicationPerson secondAuthor = feed.Authors[1];
                Assert.Empty(secondAuthor.AttributeExtensions);
                Assert.Empty(secondAuthor.ElementExtensions);
                Assert.Equal("feedauthor_email", secondAuthor.Email);
                Assert.Equal("feedauthor_name", secondAuthor.Name);
                Assert.Equal("feedauthor_uri", secondAuthor.Uri);

                Assert.Equal(new Uri("http://microsoft.com"), feed.BaseUri);

                Assert.Equal(2, feed.Categories.Count);
                SyndicationCategory firstCategory = feed.Categories[0];
                Assert.Empty(firstCategory.AttributeExtensions);
                Assert.Empty(firstCategory.ElementExtensions);
                Assert.Null(firstCategory.Name);
                Assert.Null(firstCategory.Scheme);
                Assert.Null(firstCategory.Label);

                SyndicationCategory secondCategory = feed.Categories[1];
                Assert.Empty(secondCategory.AttributeExtensions);
                Assert.Empty(secondCategory.ElementExtensions);
                Assert.Equal("feedcategory_name", secondCategory.Name);
                Assert.Equal("feedcategory_scheme", secondCategory.Scheme);
                Assert.Null(secondCategory.Label);

                Assert.Equal(2, feed.Contributors.Count);
                SyndicationPerson firstContributor = feed.Contributors[0];
                Assert.Empty(firstContributor.AttributeExtensions);
                Assert.Empty(firstContributor.ElementExtensions);
                Assert.Null(firstContributor.Email);
                Assert.Null(firstContributor.Name);
                Assert.Null(firstContributor.Uri);

                SyndicationPerson secondContributor = feed.Contributors[1];
                Assert.Empty(secondContributor.AttributeExtensions);
                Assert.Empty(secondContributor.ElementExtensions);
                Assert.Equal("feedauthor__email", secondContributor.Email);
                Assert.Equal("feedauthor__name", secondContributor.Name);
                Assert.Equal("feedauthor__uri", secondContributor.Uri);

                Assert.Empty(feed.Copyright.AttributeExtensions);
                Assert.Equal("feedcopyright_title", feed.Copyright.Text);
                Assert.Equal("text", feed.Copyright.Type);

                Assert.Equal("generator", feed.Generator);

                Assert.Empty(feed.ElementExtensions);

                Assert.Equal("id", feed.Id);

                Assert.Equal(new Uri("http://imageurl.com/"), feed.ImageUrl);

                SyndicationItem[] items = feed.Items.ToArray();
                Assert.Equal(2, items.Length);

                SyndicationItem item = items[1];
                Assert.Empty(item.AttributeExtensions);

                Assert.Equal(2, item.Authors.Count);

                SyndicationPerson itemFirstAuthor = item.Authors[0];
                Assert.Empty(itemFirstAuthor.AttributeExtensions);
                Assert.Empty(itemFirstAuthor.ElementExtensions);
                Assert.Null(itemFirstAuthor.Email);
                Assert.Null(itemFirstAuthor.Name);
                Assert.Null(itemFirstAuthor.Uri);

                SyndicationPerson itemSecondAuthor = item.Authors[1];
                Assert.Empty(itemSecondAuthor.AttributeExtensions);
                Assert.Empty(itemSecondAuthor.ElementExtensions);
                Assert.Equal("author_email", itemSecondAuthor.Email);
                Assert.Equal("author_name", itemSecondAuthor.Name);
                Assert.Equal("author_uri", itemSecondAuthor.Uri);

                Assert.Equal(new Uri("http://microsoft/relative"), item.BaseUri);

                Assert.Equal(4, item.Categories.Count);
                SyndicationCategory itemFirstCategory = item.Categories[0];
                Assert.Empty(itemFirstCategory.AttributeExtensions);
                Assert.Empty(itemFirstCategory.ElementExtensions);
                Assert.Null(itemFirstCategory.Name);
                Assert.Null(itemFirstCategory.Scheme);
                Assert.Null(itemFirstCategory.Label);

                SyndicationCategory itemSecondCategory = item.Categories[1];
                Assert.Empty(itemSecondCategory.AttributeExtensions);
                Assert.Empty(itemSecondCategory.ElementExtensions);
                Assert.Equal("category_name", itemSecondCategory.Name);
                Assert.Equal("category_scheme", itemSecondCategory.Scheme);
                Assert.Null(itemSecondCategory.Label);

                SyndicationCategory itemThirdCategory = item.Categories[2];
                Assert.Empty(itemThirdCategory.AttributeExtensions);
                Assert.Empty(itemThirdCategory.ElementExtensions);
                Assert.Equal("name", itemThirdCategory.Name);
                Assert.Equal("scheme", itemThirdCategory.Scheme);
                Assert.Null(itemThirdCategory.Label);

                SyndicationCategory itemFourthCategory = item.Categories[3];
                Assert.Empty(itemFourthCategory.AttributeExtensions);
                Assert.Empty(itemFourthCategory.ElementExtensions);
                Assert.Equal("name", itemFourthCategory.Name);
                Assert.Equal("scheme", itemFourthCategory.Scheme);
                Assert.Null(itemFourthCategory.Label);

                TextSyndicationContent content = Assert.IsType<TextSyndicationContent>(item.Content);
                Assert.Empty(content.AttributeExtensions);
                Assert.Equal("overriden", content.Text);
                Assert.Equal("text", content.Type);

                Assert.Equal(2, item.Contributors.Count);

                SyndicationPerson itemFirstContributor = item.Contributors[0];
                Assert.Empty(itemFirstContributor.AttributeExtensions);
                Assert.Empty(itemFirstContributor.ElementExtensions);
                Assert.Null(itemFirstContributor.Email);
                Assert.Null(itemFirstContributor.Name);
                Assert.Null(itemFirstContributor.Uri);

                SyndicationPerson itemSecondContributor = item.Contributors[1];
                Assert.Empty(itemSecondContributor.AttributeExtensions);
                Assert.Empty(itemSecondContributor.ElementExtensions);
                Assert.Equal("contributor_email", itemSecondContributor.Email);
                Assert.Equal("contributor_name", itemSecondContributor.Name);
                Assert.Equal("contributor_uri", itemSecondContributor.Uri);

                Assert.Equal(4, item.Copyright.AttributeExtensions.Count);
                Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name1")]);
                Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name2", "copyright_namespace")]);
                Assert.Equal("copyright_value", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name3", "copyright_namespace")]);
                Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name4", "xmlns")]);
                Assert.Equal("copyright_title", item.Copyright.Text);
                Assert.Equal("html", item.Copyright.Type);

                Assert.Empty(item.ElementExtensions);

                Assert.Equal("id", item.Id);

                Assert.Equal(DateTimeOffset.MinValue, item.LastUpdatedTime);

                Assert.Equal(3, item.Links.Count);

                SyndicationLink itemFirstLink = item.Links[0];
                Assert.Empty(itemFirstLink.AttributeExtensions);
                Assert.Empty(itemFirstLink.ElementExtensions);
                Assert.Equal(0, itemFirstLink.Length);
                Assert.Null(itemFirstLink.MediaType);
                Assert.Null(itemFirstLink.RelationshipType);
                Assert.Null(itemFirstLink.Title);
                Assert.Equal(string.Empty, itemFirstLink.Uri.OriginalString);

                SyndicationLink itemSecondLink = item.Links[1];
                Assert.Equal(4, itemSecondLink.AttributeExtensions.Count);
                Assert.Equal("", itemSecondLink.AttributeExtensions[new XmlQualifiedName("link_name1")]);
                Assert.Equal("", itemSecondLink.AttributeExtensions[new XmlQualifiedName("link_name2", "link_namespace")]);
                Assert.Equal("link_value", itemSecondLink.AttributeExtensions[new XmlQualifiedName("link_name3", "link_namespace")]);
                Assert.Equal("", itemSecondLink.AttributeExtensions[new XmlQualifiedName("link_name4", "xmlns")]);
                Assert.Empty(itemSecondLink.ElementExtensions);
                Assert.Equal(new Uri("http://link_url.com"), itemSecondLink.BaseUri);
                Assert.Equal(10, itemSecondLink.Length);
                Assert.Equal("link_mediaType", itemSecondLink.MediaType);
                Assert.Equal("link_relationshipType", itemSecondLink.RelationshipType);
                Assert.Equal("link_title", itemSecondLink.Title);
                Assert.Equal(new Uri("http://link_uri.com"), itemSecondLink.Uri);

                SyndicationLink itemThirdLink = item.Links[2];
                Assert.Empty(itemThirdLink.AttributeExtensions);
                Assert.Empty(itemThirdLink.ElementExtensions);
                Assert.Equal(100, itemThirdLink.Length);
                Assert.Equal("type_value", itemThirdLink.MediaType);
                Assert.Equal("rel_value", itemThirdLink.RelationshipType);
                Assert.Equal("title_value", itemThirdLink.Title);
                Assert.Equal("href_value", itemThirdLink.Uri.OriginalString);

                Assert.Equal(DateTimeOffset.MinValue, item.PublishDate);

                Assert.Null(item.SourceFeed);

                Assert.Empty(item.Summary.AttributeExtensions);
                Assert.Equal("summary_title", item.Summary.Text);
                Assert.Equal("text", item.Summary.Type);

                Assert.Empty(item.Title.AttributeExtensions);
                Assert.Equal("title_title", item.Title.Text);
                Assert.Equal("text", item.Title.Type);

                Assert.Equal("language", feed.Language);

                Assert.Equal(DateTimeOffset.MinValue.AddYears(1), feed.LastUpdatedTime);
                Assert.Equal(3, feed.Links.Count);

                SyndicationLink firstLink = feed.Links[0];
                Assert.Empty(firstLink.AttributeExtensions);
                Assert.Empty(firstLink.ElementExtensions);
                Assert.Equal(0, firstLink.Length);
                Assert.Null(firstLink.MediaType);
                Assert.Null(firstLink.RelationshipType);
                Assert.Null(firstLink.Title);
                Assert.Equal("", firstLink.Uri.OriginalString);

                SyndicationLink secondLink = feed.Links[1];
                Assert.Equal(4, secondLink.AttributeExtensions.Count);
                Assert.Equal("", secondLink.AttributeExtensions[new XmlQualifiedName("syndicationlink_name1")]);
                Assert.Equal("", secondLink.AttributeExtensions[new XmlQualifiedName("syndicationlink_name2", "syndicationlink_namespace")]);
                Assert.Equal("syndicationlink_value", secondLink.AttributeExtensions[new XmlQualifiedName("syndicationlink_name3", "syndicationlink_namespace")]);
                Assert.Equal("", secondLink.AttributeExtensions[new XmlQualifiedName("syndicationlink_name4", "xmlns")]);
                Assert.Empty(secondLink.ElementExtensions);
                Assert.Equal(new Uri("http://syndicationlink_url.com/"), secondLink.BaseUri);
                Assert.Equal(10, secondLink.Length);
                Assert.Equal("syndicationlink_mediaType", secondLink.MediaType);
                Assert.Equal("syndicationlink_relationshipType", secondLink.RelationshipType);
                Assert.Equal("syndicationlink_title", secondLink.Title);
                Assert.Equal("http://syndicationlink_uri.com/", secondLink.Uri.OriginalString);

                SyndicationLink thirdLink = feed.Links[2];
                Assert.Empty(thirdLink.AttributeExtensions);
                Assert.Empty(thirdLink.ElementExtensions);
                Assert.Equal(100, thirdLink.Length);
                Assert.Equal("type_value", thirdLink.MediaType);
                Assert.Equal("rel_value", thirdLink.RelationshipType);
                Assert.Equal("title_value", thirdLink.Title);
                Assert.Equal("href_value", thirdLink.Uri.OriginalString);

                Assert.Empty(item.Title.AttributeExtensions);
                Assert.Equal("feedtitle_title", feed.Title.Text);
                Assert.Equal("text", feed.Title.Type);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Read_EmptyItem_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            VerifyRead(@"<rss version=""2.0""><channel></channel></rss>", preserveElementExtensions, preserveElementExtensions, feed =>
            {
                Assert.Empty(feed.AttributeExtensions);
                Assert.Empty(feed.Authors);
                Assert.Null(feed.BaseUri);
                Assert.Empty(feed.Categories);
                Assert.Empty(feed.Contributors);
                Assert.Null(feed.Copyright);
                Assert.Null(feed.Description);
                Assert.Empty(feed.ElementExtensions);
                Assert.Null(feed.Generator);
                Assert.Null(feed.Id);
                Assert.Null(feed.ImageUrl);
                Assert.Empty(feed.Items);
                Assert.Null(feed.Language);
                Assert.Equal(DateTimeOffset.MinValue, feed.LastUpdatedTime);
                Assert.Empty(feed.Links);
                Assert.Null(feed.Title);
            });
        }

        private static void VerifyRead(string xmlString, bool preserveAttributeExtensions, bool preserveElementExtensions, Action<SyndicationFeed> verifyAction)
        {
            // ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20FeedFormatter()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);
                verifyAction(formatter.Feed);
            }

            // ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Rss20FeedFormatter()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Feed);
            }

            // Derived ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20FeedFormatter(typeof(SyndicationFeedSubclass))
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);
                verifyAction(formatter.Feed);
            }

            // Derived ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Rss20FeedFormatter(typeof(SyndicationFeedSubclass))
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Feed);
            }

            // Generic ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20FeedFormatter<SyndicationFeed>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);
                verifyAction(formatter.Feed);
            }

            // Generic ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Rss20FeedFormatter<SyndicationFeed>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Feed);
            }

            // Generic Derived ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20FeedFormatter<SyndicationFeedSubclass>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);
                verifyAction(formatter.Feed);
            }

            // Generic Derived ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Rss20FeedFormatter<SyndicationFeedSubclass>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Feed);
            }

            if (preserveAttributeExtensions && preserveElementExtensions)
            {
                // Load.
                using (var stringReader = new StringReader(xmlString))
                using (XmlReader reader = XmlReader.Create(stringReader))
                {
                    SyndicationFeed feed = SyndicationFeed.Load(reader);
                    verifyAction(feed);
                }

                // Generic Load.
                using (var stringReader = new StringReader(xmlString))
                using (XmlReader reader = XmlReader.Create(stringReader))
                {
                    SyndicationFeed feed = SyndicationFeed.Load<SyndicationFeed>(reader);
                    verifyAction(feed);
                }
            }
        }

        [Fact]
        public void ReadFrom_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new Rss20FeedFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadFrom(null));
        }

        [Fact]
        public void ReadFrom_NullCreatedFeed_ThrowsArgumentNullException()
        {
            using (var stringReader = new StringReader(@"<feed xmlns=""http://www.w3.org/2005/Atom""></feed>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new NullCreatedFeedFormatter();
                AssertExtensions.Throws<ArgumentNullException>("feed", () => formatter.ReadFrom(reader));
            }
        }

        [Theory]
        [InlineData(@"<different xmlns=""http://www.w3.org/2005/Atom""></different>")]
        [InlineData(@"<rss xmlns=""different""></rss>")]
        [InlineData(@"<rss version=""2.0""></rss>")]
        [InlineData(@"<rss version=""2.0""><title></title></rss>")]
        public void ReadFrom_CantRead_ThrowsXmlException(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20FeedFormatter();
                Assert.Throws<XmlException>(() => formatter.ReadFrom(reader));
            }
        }

        [Theory]
        [InlineData(@"<rss />")]
        [InlineData(@"<rss version=""""></rss>")]
        [InlineData(@"<rss version=""1.0""></rss>")]
        [InlineData(@"<rss version=""2.0.1""></rss>")]
        public void ReadFrom_InvalidVersion_ThrowsNotSupportedException(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20FeedFormatter();
                Assert.Throws<NotSupportedException>(() => formatter.ReadFrom(reader));
            }
        }

        [Theory]
        [InlineData(@"<rss version=""2.0""><channel></channel></rss>")]
        public void ReadXml_ValidReader_Success(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Rss20FeedFormatter();
                ((IXmlSerializable)formatter).ReadXml(reader);

                SyndicationFeed feed = formatter.Feed;
                Assert.Empty(feed.AttributeExtensions);
                Assert.Empty(feed.Authors);
                Assert.Null(feed.BaseUri);
                Assert.Empty(feed.Categories);
                Assert.Empty(feed.Contributors);
                Assert.Null(feed.Copyright);
                Assert.Null(feed.Description);
                Assert.Empty(feed.ElementExtensions);
                Assert.Null(feed.Generator);
                Assert.Null(feed.Id);
                Assert.Null(feed.ImageUrl);
                Assert.Empty(feed.Items);
                Assert.Null(feed.Language);
                Assert.Equal(DateTimeOffset.MinValue, feed.LastUpdatedTime);
                Assert.Empty(feed.Links);
                Assert.Null(feed.Title);
            }
        }

        [Fact]
        public void ReadXml_NullReader_ThrowsArgumentNullException()
        {
            IXmlSerializable formatter = new Rss20FeedFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadXml(null));
        }

        [Fact]
        public void ReadXml_NullCreatedFeed_ThrowsArgumentNullException()
        {
            using (var stringReader = new StringReader(@"<entry xmlns=""http://www.w3.org/2005/Atom""></entry>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                IXmlSerializable formatter = new NullCreatedFeedFormatter();
                AssertExtensions.Throws<ArgumentNullException>("feed", () => formatter.ReadXml(reader));
            }
        }

        [Theory]
        [InlineData(@"<rss version=""2.0""></rss>")]
        [InlineData(@"<app:feed xmlns:app=""http://www.w3.org/2005/Atom"" version=""2.0""></app:feed>")]
        [InlineData(@"<rss xmlns=""different"" version=""2.0""></rss>")]
        [InlineData(@"<different xmlns=""http://www.w3.org/2005/Atom"" version=""2.0""></different>")]
        public void ReadXml_CantRead_ThrowsXmlException(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                IXmlSerializable formatter = new Rss20FeedFormatter();
                Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
            }
        }

        [Fact]
        public void ReadXml_ThrowsArgumentException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new ArgumentException());
            IXmlSerializable formatter = new Rss20FeedFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Fact]
        public void ReadXml_ThrowsFormatException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new FormatException());
            IXmlSerializable formatter = new Rss20FeedFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Fact]
        public void ReadItem_ValidItem_ReturnsExpected()
        {
            using (var stringReader = new StringReader(@"<entry><id xmlns=""http://www.w3.org/2005/Atom"">id</id></entry>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Formatter();
                var feed = new SyndicationFeed();
                SyndicationItem item = formatter.ReadItemEntryPoint(reader, feed);
                Assert.Equal("id", item.Id);
                Assert.Null(item.SourceFeed);
                Assert.Empty(feed.Items);
            }
        }

        [Fact]
        public void ReadItem_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new Formatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadItemEntryPoint(null, new SyndicationFeed()));
        }

        [Fact]
        public void ReadItem_NullFeed_ThrowsArgumentNullException()
        {
            using (var stringReader = new StringReader("<entry></entry>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Formatter();
                AssertExtensions.Throws<ArgumentNullException>("feed", () => formatter.ReadItemEntryPoint(reader, null));
            }
        }

        [Fact]
        public void ReadItems_ValidItems_ReturnsExpected()
        {
            using (var stringReader = new StringReader(
@"<parent>
    <item><guid>id1</guid></item>
    <item><guid>id2</guid></item>
    <unknown></unknown>
    <item><guid>id3</guid></item>
</parent>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();
                reader.ReadStartElement();
                reader.MoveToElement();

                var formatter = new Formatter();
                var feed = new SyndicationFeed();
                SyndicationItem[] items = formatter.ReadItemsEntryPoint(reader, feed, out var areAllItemsRead).ToArray();
                Assert.True(areAllItemsRead);
                Assert.Empty(feed.Items);

                Assert.Equal(2, items.Length);

                Assert.Equal("id1", items[0].Id);
                Assert.Null(items[0].SourceFeed);

                Assert.Equal("id2", items[1].Id);
                Assert.Null(items[1].SourceFeed);
            }
        }

        [Fact]
        public void ReadItems_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new Formatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadItemsEntryPoint(null, new SyndicationFeed(), out var areAllItemsReader));
        }

        [Fact]
        public void ReadItems_NullFeed_ThrowsArgumentNullException()
        {
            using (var stringReader = new StringReader("<entry></entry>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Formatter();
                AssertExtensions.Throws<ArgumentNullException>("feed", () => formatter.ReadItemsEntryPoint(reader, null, out var areAllItemsReader));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PreserveAttributeExtensions_Set_GetReturnsExpected(bool preserveAttributeExtensions)
        {
            var formatter = new Rss20FeedFormatter() { PreserveAttributeExtensions = preserveAttributeExtensions };
            Assert.Equal(preserveAttributeExtensions, formatter.PreserveAttributeExtensions);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PreserveElementExtensions_Set_GetReturnsExpected(bool preserveElementExtensions)
        {
            var formatter = new Rss20FeedFormatter { PreserveElementExtensions = preserveElementExtensions };
            Assert.Equal(preserveElementExtensions, formatter.PreserveElementExtensions);
        }

        [Fact]
        public void CreateFeedInstance_NonGeneric_Success()
        {
            var formatter = new Formatter();
            SyndicationFeed feed = Assert.IsType<SyndicationFeed>(formatter.CreateFeedInstanceEntryPoint());
            Assert.Empty(feed.AttributeExtensions);
            Assert.Empty(feed.Authors);
            Assert.Null(feed.BaseUri);
            Assert.Empty(feed.Categories);
            Assert.Empty(feed.Contributors);
            Assert.Null(feed.Copyright);
            Assert.Null(feed.Description);
            Assert.Empty(feed.ElementExtensions);
            Assert.Null(feed.Generator);
            Assert.Null(feed.Id);
            Assert.Null(feed.ImageUrl);
            Assert.Empty(feed.Items);
            Assert.Null(feed.Language);
            Assert.Equal(DateTimeOffset.MinValue, feed.LastUpdatedTime);
            Assert.Empty(feed.Links);
            Assert.Null(feed.Title);

            var typedFormatter = new Formatter(typeof(SyndicationFeedSubclass));
            feed = Assert.IsType<SyndicationFeedSubclass>(typedFormatter.CreateFeedInstanceEntryPoint());
            Assert.Empty(feed.AttributeExtensions);
            Assert.Empty(feed.Authors);
            Assert.Null(feed.BaseUri);
            Assert.Empty(feed.Categories);
            Assert.Empty(feed.Contributors);
            Assert.Null(feed.Copyright);
            Assert.Null(feed.Description);
            Assert.Empty(feed.ElementExtensions);
            Assert.Null(feed.Generator);
            Assert.Null(feed.Id);
            Assert.Null(feed.ImageUrl);
            Assert.Empty(feed.Items);
            Assert.Null(feed.Language);
            Assert.Equal(DateTimeOffset.MinValue, feed.LastUpdatedTime);
            Assert.Empty(feed.Links);
            Assert.Null(feed.Title);
        }

        [Fact]
        public void CreateItemInstance_Generic_Success()
        {
            var formatter = new GenericFormatter<SyndicationFeed>();
            SyndicationFeed feed = Assert.IsType<SyndicationFeed>(formatter.CreateFeedInstanceEntryPoint());
            Assert.Empty(feed.AttributeExtensions);
            Assert.Empty(feed.Authors);
            Assert.Null(feed.BaseUri);
            Assert.Empty(feed.Categories);
            Assert.Empty(feed.Contributors);
            Assert.Null(feed.Copyright);
            Assert.Null(feed.Description);
            Assert.Empty(feed.ElementExtensions);
            Assert.Null(feed.Generator);
            Assert.Null(feed.Id);
            Assert.Null(feed.ImageUrl);
            Assert.Empty(feed.Items);
            Assert.Null(feed.Language);
            Assert.Equal(DateTimeOffset.MinValue, feed.LastUpdatedTime);
            Assert.Empty(feed.Links);
            Assert.Null(feed.Title);

            var typedFormatter = new GenericFormatter<SyndicationFeedSubclass>();
            feed = Assert.IsType<SyndicationFeedSubclass>(typedFormatter.CreateFeedInstanceEntryPoint());
            Assert.Empty(feed.AttributeExtensions);
            Assert.Empty(feed.Authors);
            Assert.Null(feed.BaseUri);
            Assert.Empty(feed.Categories);
            Assert.Empty(feed.Contributors);
            Assert.Null(feed.Copyright);
            Assert.Null(feed.Description);
            Assert.Empty(feed.ElementExtensions);
            Assert.Null(feed.Generator);
            Assert.Null(feed.Id);
            Assert.Null(feed.ImageUrl);
            Assert.Empty(feed.Items);
            Assert.Null(feed.Language);
            Assert.Equal(DateTimeOffset.MinValue, feed.LastUpdatedTime);
            Assert.Empty(feed.Links);
            Assert.Null(feed.Title);
        }

        public class SyndicationFeedSubclass : SyndicationFeed { }

        public class SyndicationFeedTryParseTrueSubclass : SyndicationFeed
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }

            protected override SyndicationCategory CreateCategory() => new SyndicationCategoryTryParseTrueSubclass();

            protected override SyndicationItem CreateItem() => new SyndicationItemTryParseTrueSubclass();

            protected override SyndicationLink CreateLink() => new SyndicationLinkTryParseTrueSubclass();

            protected override SyndicationPerson CreatePerson() => new SyndicationPersonTryParseTrueSubclass();
        }

        public class SyndicationItemTryParseTrueSubclass : SyndicationItem
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseContent(XmlReader reader, string contentType, string version, out SyndicationContent content)
            {
                reader.Skip();

                content = new TextSyndicationContent("overriden");
                return true;
            }

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }

            protected override SyndicationCategory CreateCategory() => new SyndicationCategoryTryParseTrueSubclass();

            protected override SyndicationPerson CreatePerson() => new SyndicationPersonTryParseTrueSubclass();

            protected override SyndicationLink CreateLink() => new SyndicationLinkTryParseTrueSubclass();
        }

        public class SyndicationCategoryTryParseTrueSubclass : SyndicationCategory
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }
        }

        public class SyndicationPersonTryParseTrueSubclass : SyndicationPerson
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }
        }

        public class SyndicationLinkTryParseTrueSubclass : SyndicationLink
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }
        }

        public class NullCreatedFeedFormatter : Atom10FeedFormatter
        {
            protected override SyndicationFeed CreateFeedInstance() => null;
        }

        public class Formatter : Rss20FeedFormatter
        {
            public Formatter() : base() { }

            public Formatter(SyndicationFeed feedToWrite) : base(feedToWrite) { }

            public Formatter(SyndicationFeed feedToWrite, bool serializeExtensionsAsAtom) : base(feedToWrite, serializeExtensionsAsAtom) { }

            public Formatter(Type feedTypeToCreate) : base(feedTypeToCreate) { }

            public Type FeedTypeEntryPoint => FeedType;

            public SyndicationFeed CreateFeedInstanceEntryPoint() => CreateFeedInstance();

            public void WriteItemEntryPoint(XmlWriter writer, SyndicationItem item, Uri feedBaseUri) => WriteItem(writer, item, feedBaseUri);

            public void WriteItemsEntryPoint(XmlWriter writer, IEnumerable<SyndicationItem> items, Uri feedBaseUri) => WriteItems(writer, items, feedBaseUri);

            public SyndicationItem ReadItemEntryPoint(XmlReader reader, SyndicationFeed feed) => ReadItem(reader, feed);

            public IEnumerable<SyndicationItem> ReadItemsEntryPoint(XmlReader reader, SyndicationFeed feed, out bool areAllItemsRead)
            {
                return ReadItems(reader, feed, out areAllItemsRead);
            }
        }

        public class GenericFormatter<T> : Rss20FeedFormatter<T> where T : SyndicationFeed, new()
        {
            public GenericFormatter() : base() { }

            public GenericFormatter(T feedToWrite) : base(feedToWrite) { }

            public GenericFormatter(T feedToWrite, bool serializeExtensionsAsAtom) : base(feedToWrite, serializeExtensionsAsAtom) { }

            public Type FeedTypeEntryPoint => FeedType;

            public SyndicationFeed CreateFeedInstanceEntryPoint() => CreateFeedInstance();
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}
