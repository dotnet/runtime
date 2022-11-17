// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public partial class Atom10FeedFormatterTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var formatter = new Formatter();
            Assert.Null(formatter.Feed);
            Assert.Equal(typeof(SyndicationFeed), formatter.FeedTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Atom10", formatter.Version);
        }

        [Fact]
        public void Ctor_GenericDefault()
        {
            var formatter = new GenericFormatter<SyndicationFeed>();
            Assert.Null(formatter.Feed);
            Assert.Equal(typeof(SyndicationFeed), formatter.FeedTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Atom10", formatter.Version);
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
            Assert.Equal("Atom10", formatter.Version);
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
            Assert.Equal("Atom10", formatter.Version);
        }

        [Fact]
        public void Ctor_NullFeedToWrite_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("feedToWrite", () => new Atom10FeedFormatter((SyndicationFeed)null));
            AssertExtensions.Throws<ArgumentNullException>("feedToWrite", () => new Atom10FeedFormatter<SyndicationFeed>(null));
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
            Assert.Equal("Atom10", formatter.Version);
        }

        [Fact]
        public void Ctor_NullFeedTypeToCreate_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("feedTypeToCreate", () => new Atom10FeedFormatter((Type)null));
        }

        [Fact]
        public void Ctor_InvalidFeedTypeToCreate_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("feedTypeToCreate", () => new Atom10FeedFormatter(typeof(int)));
        }

        [Fact]
        public void GetSchema_Invoke_ReturnsNull()
        {
            IXmlSerializable formatter = new Atom10FeedFormatter();
            Assert.Null(formatter.GetSchema());
        }

        public static IEnumerable<object[]> WriteTo_TestData()
        {
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

            yield return new object[]
            {
                fullSyndicationFeed,
@"<feed xml:lang=""language"" xml:base=""http://microsoft.com/"" feed_name1="""" d1p1:feed_name2="""" d1p1:feed_name3=""feed_value"" d1p2:feed_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""feed_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
    <title type=""html"" feedtitle_name1="""" d2p1:feedtitle_name2="""" d2p1:feedtitle_name3=""feedtitle_value"" d1p2:feedtitle_name4="""" xmlns:d2p1=""feedtitle_namespace"">feedtitle_title</title>
    <subtitle type=""html"" feeddescription_name1="""" d2p1:feeddescription_name2="""" d2p1:feeddescription_name3=""feeddescription_value"" d1p2:feeddescription_name4="""" xmlns:d2p1=""feeddescription_namespace"">feeddescription_title</subtitle>
    <id>id</id>
    <rights type=""html"" feedcopyright_name1="""" d2p1:feedcopyright_name2="""" d2p1:feedcopyright_name3=""feedcopyright_value"" d1p2:feedcopyright_name4="""" xmlns:d2p1=""feedcopyright_namespace"">feedcopyright_title</rights>
    <updated>0002-01-01T00:00:00Z</updated>
    <category term="""" />
    <category feedcategory_name1="""" d2p1:feedcategory_name2="""" d2p1:feedcategory_name3=""feedcategory_value"" d1p2:feedcategory_name4="""" term=""feedcategory_name"" label=""feedcategory_label"" scheme=""feedcategory_scheme"" xmlns:d2p1=""feedcategory_namespace"">
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </category>
    <logo>http://imageurl.com/</logo>
    <author />
    <author feedauthor_name1="""" d2p1:feedauthor_name2="""" d2p1:feedauthor_name3=""feedauthor_value"" d1p2:feedauthor_name4="""" xmlns:d2p1=""feedauthor_namespace"">
        <name>feedauthor_name</name>
        <uri>feedauthor_uri</uri>
        <email>feedauthor_email</email>
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </author>
    <contributor />
    <contributor feedauthor__name1="""" d2p1:feedauthor__name2="""" d2p1:feedauthor__name3=""feedauthor__value"" d1p2:feedauthor__name4="""" xmlns:d2p1=""feedauthor__namespace"">
        <name>feedauthor__name</name>
        <uri>feedauthor__uri</uri>
        <email>feedauthor__email</email>
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </contributor>
    <generator>generator</generator>
    <link href="""" />
    <link xml:base=""http://syndicationlink_url.com/"" syndicationlink_name1="""" d2p1:syndicationlink_name2="""" d2p1:syndicationlink_name3=""syndicationlink_value"" d1p2:syndicationlink_name4="""" rel=""syndicationlink_relationshipType"" type=""syndicationlink_mediaType"" title=""syndicationlink_title"" length=""10"" href=""http://syndicationlink_uri.com/"" xmlns:d2p1=""syndicationlink_namespace"">
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </link>
    <link rel=""rel_value"" type=""type_value"" title=""title_value"" length=""100"" href=""href_value"" />
    <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </Atom10FeedFormatterTests.ExtensionObject>
    <entry>
        <id>id</id>
        <title type=""text"" />
        <updated>0001-01-01T00:00:00Z</updated>
    </entry>
    <entry xml:base=""http://microsoft/relative"" item_name1="""" d2p1:item_name2="""" d2p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d2p1=""item_namespace"">
        <id>id</id>
        <title type=""html"" title_name1="""" d3p1:title_name2="""" d3p1:title_name3=""title_value"" d1p2:title_name4="""" xmlns:d3p1=""title_namespace"">title_title</title>
        <summary type=""html"" summary_name1="""" d3p1:summary_name2="""" d3p1:summary_name3=""summary_value"" d1p2:summary_name4="""" xmlns:d3p1=""summary_namespace"">summary_title</summary>
        <published>0001-01-01T00:00:00Z</published>
        <updated>0001-01-01T00:00:00Z</updated>
        <author />
        <author author_name1="""" d3p1:author_name2="""" d3p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d3p1=""author_namespace"">
            <name>author_name</name>
            <uri>author_uri</uri>
            <email>author_email</email>
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </author>
        <contributor />
        <contributor contributor_name1="""" d3p1:contributor_name2="""" d3p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d3p1=""contributor_namespace"">
            <name>contributor_name</name>
            <uri>contributor_uri</uri>
            <email>contributor_email</email>
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </contributor>
        <link href="""" />
        <link xml:base=""http://link_url.com/"" link_name1="""" d3p1:link_name2="""" d3p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d3p1=""link_namespace"">
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </link>
        <link rel=""rel_value"" type=""type_value"" title=""title_value"" length=""100"" href=""href_value"" />
        <category term="""" />
        <category category_name1="""" d3p1:category_name2="""" d3p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d3p1=""category_namespace"">
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </category>
        <category term=""term_value"" label=""label_value"" scheme=""scheme_value"" />
        <category term=""term_value"" label=""label_value"" scheme=""scheme_value"" />
        <content type=""html"" content_name1="""" d3p1:content_name2="""" d3p1:content_name3=""content_value"" d1p2:content_name4="""" xmlns:d3p1=""content_namespace"">content_title</content>
        <rights type=""html"" copyright_name1="""" d3p1:copyright_name2="""" d3p1:copyright_name3=""copyright_value"" d1p2:copyright_name4="""" xmlns:d3p1=""copyright_namespace"">copyright_title</rights>
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </entry>
</feed>"
        };
        }

        [Theory]
        [MemberData(nameof(WriteTo_TestData))]
        public void Write_HasFeed_SerializesExpected(SyndicationFeed feed, string expected)
        {
            var formatter = new Atom10FeedFormatter(feed);
            CompareHelper.AssertEqualWriteOutput(expected, writer => formatter.WriteTo(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer => feed.SaveAsAtom10(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer =>
            {
                writer.WriteStartElement("feed", "http://www.w3.org/2005/Atom");
                ((IXmlSerializable)formatter).WriteXml(writer);
                writer.WriteEndElement();
            });

            var genericFormatter = new Atom10FeedFormatter<SyndicationFeed>(feed);
            CompareHelper.AssertEqualWriteOutput(expected, writer => formatter.WriteTo(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer => feed.SaveAsAtom10(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer =>
            {
                writer.WriteStartElement("feed", "http://www.w3.org/2005/Atom");
                ((IXmlSerializable)genericFormatter).WriteXml(writer);
                writer.WriteEndElement();
            });
        }

        [Fact]
        public void Write_EmptyFeed_SerializesExpected()
        {
            var formatter = new Atom10FeedFormatter(new SyndicationFeed());
            var stringBuilder = new StringBuilder();

            using (XmlWriter writer = XmlWriter.Create(stringBuilder))
            {
                formatter.WriteTo(writer);
            }

            using (var stringReader = new StringReader(stringBuilder.ToString()))
            {
                XElement element = XElement.Load(stringReader);
                Assert.Equal("feed", element.Name.LocalName);
                Assert.Equal("http://www.w3.org/2005/Atom", element.Attribute("xmlns").Value);

                XElement[] elements = element.Elements().ToArray();
                Assert.Equal(3, elements.Length);

                Assert.Equal("title", elements[0].Name.LocalName);
                Assert.Equal("text", elements[0].Attribute("type").Value);
                Assert.Empty(elements[0].Value);

                Assert.Equal("id", elements[1].Name.LocalName);
                Assert.StartsWith("uuid:", elements[1].Value);

                Assert.Equal("updated", elements[2].Name.LocalName);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                Assert.True(now >= DateTimeOffset.ParseExact(elements[2].Value, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            }
        }

        [Fact]
        public void WriteTo_NullWriter_ThrowsArgumentNullException()
        {
            var formatter = new Atom10FeedFormatter(new SyndicationFeed());
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteTo(null));
        }

        [Fact]
        public void WriteTo_NoItem_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Atom10FeedFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteTo(writer));
            }
        }

        [Fact]
        public void WriteXml_NullWriter_ThrowsArgumentNullException()
        {
            IXmlSerializable formatter = new Atom10FeedFormatter();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteXml(null));
        }

        [Fact]
        public void WriteXml_NoItem_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                IXmlSerializable formatter = new Atom10FeedFormatter();
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
@"<entry xmlns=""http://www.w3.org/2005/Atom"">
    <id>id</id>
    <title type=""text"" />
    <updated>0001-01-01T00:00:00Z</updated>
</entry>", writer => formatter.WriteItemEntryPoint(writer, item, feedBaseUri));
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
@"<entry xmlns=""http://www.w3.org/2005/Atom"">
    <id>id1</id>
    <title type=""text"" />
    <updated>0001-01-01T00:00:00Z</updated>
</entry>
<entry xmlns=""http://www.w3.org/2005/Atom"">
    <id>id2</id>
    <title type=""text"" />
    <updated>0001-01-01T00:00:00Z</updated>
</entry>", writer => formatter.WriteItemsEntryPoint(writer, items, feedBaseUri));
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
            yield return new object[] { @"<feed />", false };
            yield return new object[] { @"<feed xmlns=""different"" />", false };
            yield return new object[] { @"<different xmlns=""http://www.w3.org/2005/Atom"" />", false };
            yield return new object[] { @"<feed xmlns=""http://www.w3.org/2005/Atom"" />", true };
            yield return new object[] { @"<feed xmlns=""http://www.w3.org/2005/Atom""></feed>", true };
        }

        [Theory]
        [MemberData(nameof(CanRead_TestData))]
        public void CanRead_ValidReader_ReturnsExpected(string xmlString, bool expected)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Atom10FeedFormatter();
                Assert.Equal(expected, formatter.CanRead(reader));
            }
        }

        [Fact]
        public void CanRead_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new Atom10FeedFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.CanRead(null));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Read_FullItem_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            VerifyRead(
@"<feed xml:lang=""language"" xml:base=""http://microsoft.com/"" feed_name1="""" d1p1:feed_name2="""" d1p1:feed_name3=""feed_value"" d1p2:feed_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""feed_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
    <title type=""html"" feedtitle_name1="""" d2p1:feedtitle_name2="""" d2p1:feedtitle_name3=""feedtitle_value"" d1p2:feedtitle_name4="""" xmlns:d2p1=""feedtitle_namespace"">feedtitle_title</title>
    <subtitle type=""html"" feeddescription_name1="""" d2p1:feeddescription_name2="""" d2p1:feeddescription_name3=""feeddescription_value"" d1p2:feeddescription_name4="""" xmlns:d2p1=""feeddescription_namespace"">feeddescription_title</subtitle>
    <id>id</id>
    <rights type=""html"" feedcopyright_name1="""" d2p1:feedcopyright_name2="""" d2p1:feedcopyright_name3=""feedcopyright_value"" d1p2:feedcopyright_name4="""" xmlns:d2p1=""feedcopyright_namespace"">feedcopyright_title</rights>
    <updated>0002-01-01T00:00:00Z</updated>
    <category />
    <category></category>
    <category term="""" />
    <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d2p1=""category_namespace"">
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </category>
    <logo>http://imageurl.com/</logo>
    <author />
    <author></author>
    <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d2p1=""author_namespace"">
        <name>author_name</name>
        <uri>author_uri</uri>
        <email>author_email</email>
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </author>
    <contributor />
    <contributor></contributor>
    <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"">
        <name>contributor_name</name>
        <uri>contributor_uri</uri>
        <email>contributor_email</email>
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </contributor>
    <generator>generator</generator>
    <link />
    <link></link>
    <link href="""" />
    <link xml:base=""http://link_url.com/"" link_name1="""" d2p1:link_name2="""" d2p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d2p1=""link_namespace"">
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </link>
    <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </Atom10FeedFormatterTests.ExtensionObject>
    <entry>
        <id>id</id>
        <title type=""text"" />
        <updated>0001-01-01T00:00:00Z</updated>
    </entry>
    <entry xml:base=""/relative"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <id>id</id>
        <title type=""html"" title_name1="""" d2p1:title_name2="""" d2p1:title_name3=""title_value"" d1p2:title_name4="""" xmlns:d2p1=""title_namespace"">title_title</title>
        <summary type=""html"" summary_name1="""" d2p1:summary_name2="""" d2p1:summary_name3=""summary_value"" d1p2:summary_name4="""" xmlns:d2p1=""summary_namespace"">summary_title</summary>
        <published>0001-01-01T00:00:00Z</published>
        <updated>0001-01-01T00:00:00Z</updated>
        <author />
        <author></author>
        <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d2p1=""author_namespace"">
            <name>author_name</name>
            <uri>author_uri</uri>
            <email>author_email</email>
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </author>
        <contributor />
        <contributor></contributor>
        <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"">
            <name>contributor_name</name>
            <uri>contributor_uri</uri>
            <email>contributor_email</email>
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </contributor>
        <link />
        <link></link>
        <link href="""" />
        <link xml:base=""http://link_url.com/"" link_name1="""" d2p1:link_name2="""" d2p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d2p1=""link_namespace"">
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </link>
        <category />
        <category></category>
        <category term="""" />
        <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d2p1=""category_namespace"">
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </category>
        <content type=""html"" content_name1="""" d2p1:content_name2="""" d2p1:content_name3=""content_value"" d1p2:content_name4="""" xmlns:d2p1=""content_namespace"">content_title</content>
        <rights type=""html"" copyright_name1="""" d2p1:copyright_name2="""" d2p1:copyright_name3=""copyright_value"" d1p2:copyright_name4="""" xmlns:d2p1=""copyright_namespace"">copyright_title</rights>
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </entry>
</feed>
", preserveElementExtensions, preserveElementExtensions, feed =>
            {
                if (preserveAttributeExtensions)
                {
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

                Assert.Equal(3, feed.Authors.Count);

                SyndicationPerson firstAuthor = feed.Authors[0];
                Assert.Empty(firstAuthor.AttributeExtensions);
                Assert.Empty(firstAuthor.ElementExtensions);
                Assert.Null(firstAuthor.Email);
                Assert.Null(firstAuthor.Name);
                Assert.Null(firstAuthor.Uri);

                SyndicationPerson secondAuthor = feed.Authors[1];
                Assert.Empty(secondAuthor.AttributeExtensions);
                Assert.Empty(secondAuthor.ElementExtensions);
                Assert.Null(secondAuthor.Email);
                Assert.Null(secondAuthor.Name);
                Assert.Null(secondAuthor.Uri);

                SyndicationPerson thirdAuthor = feed.Authors[2];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, thirdAuthor.AttributeExtensions.Count);
                    Assert.Equal("", thirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name1")]);
                    Assert.Equal("", thirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name2", "author_namespace")]);
                    Assert.Equal("author_value", thirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name3", "author_namespace")]);
                    Assert.Equal("", thirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(thirdAuthor.AttributeExtensions);
                }
                if (preserveElementExtensions)
                {
                    Assert.Equal(1, thirdAuthor.ElementExtensions.Count);
                    Assert.Equal(10, thirdAuthor.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                }
                else
                {
                    Assert.Empty(thirdAuthor.ElementExtensions);
                }
                Assert.Equal("author_email", thirdAuthor.Email);
                Assert.Equal("author_name", thirdAuthor.Name);
                Assert.Equal("author_uri", thirdAuthor.Uri);

                Assert.Equal(new Uri("http://microsoft.com"), feed.BaseUri);

                Assert.Equal(4, feed.Categories.Count);
                SyndicationCategory firstCategory = feed.Categories[0];
                Assert.Empty(firstCategory.AttributeExtensions);
                Assert.Empty(firstCategory.ElementExtensions);
                Assert.Null(firstCategory.Name);
                Assert.Null(firstCategory.Scheme);
                Assert.Null(firstCategory.Label);

                SyndicationCategory secondCategory = feed.Categories[1];
                Assert.Empty(secondCategory.AttributeExtensions);
                Assert.Empty(secondCategory.ElementExtensions);
                Assert.Null(secondCategory.Name);
                Assert.Null(secondCategory.Scheme);
                Assert.Null(secondCategory.Label);

                SyndicationCategory thirCategory = feed.Categories[2];
                Assert.Empty(thirCategory.AttributeExtensions);
                Assert.Empty(thirCategory.ElementExtensions);
                Assert.Empty(thirCategory.Name);
                Assert.Null(thirCategory.Scheme);
                Assert.Null(thirCategory.Label);

                SyndicationCategory fourthCategory = feed.Categories[3];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, fourthCategory.AttributeExtensions.Count);
                    Assert.Equal("", fourthCategory.AttributeExtensions[new XmlQualifiedName("category_name1")]);
                    Assert.Equal("", fourthCategory.AttributeExtensions[new XmlQualifiedName("category_name2", "category_namespace")]);
                    Assert.Equal("category_value", fourthCategory.AttributeExtensions[new XmlQualifiedName("category_name3", "category_namespace")]);
                    Assert.Equal("", fourthCategory.AttributeExtensions[new XmlQualifiedName("category_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(fourthCategory.AttributeExtensions);
                }
                if (preserveElementExtensions)
                {
                    Assert.Equal(1, fourthCategory.ElementExtensions.Count);
                    Assert.Equal(10, fourthCategory.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                }
                else
                {
                    Assert.Empty(fourthCategory.ElementExtensions);
                }
                Assert.Equal("category_name", fourthCategory.Name);
                Assert.Equal("category_scheme", fourthCategory.Scheme);
                Assert.Equal("category_label", fourthCategory.Label);

                Assert.Equal(3, feed.Contributors.Count);
                SyndicationPerson firstContributor = feed.Contributors[0];
                Assert.Empty(firstContributor.AttributeExtensions);
                Assert.Empty(firstContributor.ElementExtensions);
                Assert.Null(firstContributor.Email);
                Assert.Null(firstContributor.Name);
                Assert.Null(firstContributor.Uri);

                SyndicationPerson secondContributor = feed.Contributors[1];
                Assert.Empty(secondContributor.AttributeExtensions);
                Assert.Empty(secondContributor.ElementExtensions);
                Assert.Null(secondContributor.Email);
                Assert.Null(secondContributor.Name);
                Assert.Null(secondContributor.Uri);

                SyndicationPerson thirdContributor = feed.Contributors[2];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, thirdContributor.AttributeExtensions.Count);
                    Assert.Equal("", thirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name1")]);
                    Assert.Equal("", thirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name2", "contributor_namespace")]);
                    Assert.Equal("contributor_value", thirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name3", "contributor_namespace")]);
                    Assert.Equal("", thirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(thirdContributor.AttributeExtensions);
                }
                if (preserveElementExtensions)
                {
                    Assert.Equal(1, thirdContributor.ElementExtensions.Count);
                    Assert.Equal(10, thirdContributor.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                }
                else
                {
                    Assert.Empty(thirdContributor.ElementExtensions);
                }
                Assert.Equal("contributor_email", thirdContributor.Email);
                Assert.Equal("contributor_name", thirdContributor.Name);
                Assert.Equal("contributor_uri", thirdContributor.Uri);

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, feed.Copyright.AttributeExtensions.Count);
                    Assert.Equal("", feed.Copyright.AttributeExtensions[new XmlQualifiedName("feedcopyright_name1")]);
                    Assert.Equal("", feed.Copyright.AttributeExtensions[new XmlQualifiedName("feedcopyright_name2", "feedcopyright_namespace")]);
                    Assert.Equal("feedcopyright_value", feed.Copyright.AttributeExtensions[new XmlQualifiedName("feedcopyright_name3", "feedcopyright_namespace")]);
                    Assert.Equal("", feed.Copyright.AttributeExtensions[new XmlQualifiedName("feedcopyright_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(feed.Copyright.AttributeExtensions);
                }
                Assert.Equal("feedcopyright_title", feed.Copyright.Text);
                Assert.Equal("html", feed.Copyright.Type);

                Assert.Equal("generator", feed.Generator);

                if (preserveElementExtensions)
                {
                    Assert.Equal(1, feed.ElementExtensions.Count);
                    Assert.Equal(10, feed.ElementExtensions[0].GetObject<ExtensionObject>().Value);
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
                    Assert.Equal("", item.AttributeExtensions[new XmlQualifiedName("item_name1")]);
                    Assert.Equal("", item.AttributeExtensions[new XmlQualifiedName("item_name2", "item_namespace")]);
                    Assert.Equal("item_value", item.AttributeExtensions[new XmlQualifiedName("item_name3", "item_namespace")]);
                    Assert.Equal("", item.AttributeExtensions[new XmlQualifiedName("item_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(item.AttributeExtensions);
                }

                Assert.Equal(3, item.Authors.Count);

                SyndicationPerson itemFirstAuthor = item.Authors[0];
                Assert.Empty(itemFirstAuthor.AttributeExtensions);
                Assert.Empty(itemFirstAuthor.ElementExtensions);
                Assert.Null(itemFirstAuthor.Email);
                Assert.Null(itemFirstAuthor.Name);
                Assert.Null(itemFirstAuthor.Uri);

                SyndicationPerson itemSecondAuthor = item.Authors[1];
                Assert.Empty(itemSecondAuthor.AttributeExtensions);
                Assert.Empty(itemSecondAuthor.ElementExtensions);
                Assert.Null(itemSecondAuthor.Email);
                Assert.Null(itemSecondAuthor.Name);
                Assert.Null(itemSecondAuthor.Uri);

                SyndicationPerson itemThirdAuthor = item.Authors[2];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, itemThirdAuthor.AttributeExtensions.Count);
                    Assert.Equal("", itemThirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name1")]);
                    Assert.Equal("", itemThirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name2", "author_namespace")]);
                    Assert.Equal("author_value", itemThirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name3", "author_namespace")]);
                    Assert.Equal("", itemThirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(itemThirdAuthor.AttributeExtensions);
                }
                if (preserveElementExtensions)
                {
                    Assert.Equal(1, itemThirdAuthor.ElementExtensions.Count);
                    Assert.Equal(10, itemThirdAuthor.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                }
                else
                {
                    Assert.Empty(itemThirdAuthor.ElementExtensions);
                }
                Assert.Equal("author_email", itemThirdAuthor.Email);
                Assert.Equal("author_name", itemThirdAuthor.Name);
                Assert.Equal("author_uri", itemThirdAuthor.Uri);

                Assert.Equal(new Uri("http://microsoft.com/relative"), item.BaseUri);

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
                Assert.Null(itemSecondCategory.Name);
                Assert.Null(itemSecondCategory.Scheme);
                Assert.Null(itemSecondCategory.Label);

                SyndicationCategory itemThirdCategory = item.Categories[2];
                Assert.Empty(itemThirdCategory.AttributeExtensions);
                Assert.Empty(itemThirdCategory.ElementExtensions);
                Assert.Empty(itemThirdCategory.Name);
                Assert.Null(itemThirdCategory.Scheme);
                Assert.Null(itemThirdCategory.Label);

                SyndicationCategory itemFourthCategory = item.Categories[3];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, itemFourthCategory.AttributeExtensions.Count);
                    Assert.Equal("", itemFourthCategory.AttributeExtensions[new XmlQualifiedName("category_name1")]);
                    Assert.Equal("", itemFourthCategory.AttributeExtensions[new XmlQualifiedName("category_name2", "category_namespace")]);
                    Assert.Equal("category_value", itemFourthCategory.AttributeExtensions[new XmlQualifiedName("category_name3", "category_namespace")]);
                    Assert.Equal("", itemFourthCategory.AttributeExtensions[new XmlQualifiedName("category_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(itemFourthCategory.AttributeExtensions);
                }
                if (preserveElementExtensions)
                {
                    Assert.Equal(1, itemFourthCategory.ElementExtensions.Count);
                    Assert.Equal(10, itemFourthCategory.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                }
                else
                {
                    Assert.Empty(itemFourthCategory.ElementExtensions);
                }
                Assert.Equal("category_name", itemFourthCategory.Name);
                Assert.Equal("category_scheme", itemFourthCategory.Scheme);
                Assert.Equal("category_label", itemFourthCategory.Label);

                TextSyndicationContent content = Assert.IsType<TextSyndicationContent>(item.Content);
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, content.AttributeExtensions.Count);
                    Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name1")]);
                    Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name2", "content_namespace")]);
                    Assert.Equal("content_value", content.AttributeExtensions[new XmlQualifiedName("content_name3", "content_namespace")]);
                    Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(content.AttributeExtensions);
                }
                Assert.Equal("content_title", content.Text);
                Assert.Equal("html", content.Type);

                Assert.Equal(3, item.Contributors.Count);

                SyndicationPerson itemFirstContributor = item.Contributors[0];
                Assert.Empty(itemFirstContributor.AttributeExtensions);
                Assert.Empty(itemFirstContributor.ElementExtensions);
                Assert.Null(itemFirstContributor.Email);
                Assert.Null(itemFirstContributor.Name);
                Assert.Null(itemFirstContributor.Uri);

                SyndicationPerson itemSecondContributor = item.Contributors[1];
                Assert.Empty(itemSecondContributor.AttributeExtensions);
                Assert.Empty(itemSecondContributor.ElementExtensions);
                Assert.Null(itemSecondContributor.Email);
                Assert.Null(itemSecondContributor.Name);
                Assert.Null(itemSecondContributor.Uri);

                SyndicationPerson itemThirdContributor = item.Contributors[2];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, itemThirdContributor.AttributeExtensions.Count);
                    Assert.Equal("", itemThirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name1")]);
                    Assert.Equal("", itemThirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name2", "contributor_namespace")]);
                    Assert.Equal("contributor_value", itemThirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name3", "contributor_namespace")]);
                    Assert.Equal("", itemThirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(itemThirdContributor.AttributeExtensions);
                }
                if (preserveElementExtensions)
                {
                    Assert.Equal(1, itemThirdContributor.ElementExtensions.Count);
                    Assert.Equal(10, itemThirdContributor.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                }
                else
                {
                    Assert.Empty(itemThirdContributor.ElementExtensions);
                }
                Assert.Equal("contributor_email", itemThirdContributor.Email);
                Assert.Equal("contributor_name", itemThirdContributor.Name);
                Assert.Equal("contributor_uri", itemThirdContributor.Uri);

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, item.Copyright.AttributeExtensions.Count);
                    Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name1")]);
                    Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name2", "copyright_namespace")]);
                    Assert.Equal("copyright_value", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name3", "copyright_namespace")]);
                    Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(item.Copyright.AttributeExtensions);
                }
                Assert.Equal("copyright_title", item.Copyright.Text);
                Assert.Equal("html", item.Copyright.Type);

                if (preserveElementExtensions)
                {
                    Assert.Equal(1, item.ElementExtensions.Count);
                    Assert.Equal(10, item.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                }
                else
                {
                    Assert.Empty(item.ElementExtensions);
                }

                Assert.Equal("id", item.Id);

                Assert.Equal(DateTimeOffset.MinValue, item.LastUpdatedTime);

                Assert.Equal(4, item.Links.Count);

                SyndicationLink itemFirstLink = item.Links[0];
                Assert.Empty(itemFirstLink.AttributeExtensions);
                Assert.Empty(itemFirstLink.ElementExtensions);
                Assert.Equal(0, itemFirstLink.Length);
                Assert.Null(itemFirstLink.MediaType);
                Assert.Null(itemFirstLink.RelationshipType);
                Assert.Null(itemFirstLink.Title);
                Assert.Null(itemFirstLink.Uri);

                SyndicationLink itemSecondLink = item.Links[1];
                Assert.Empty(itemSecondLink.AttributeExtensions);
                Assert.Empty(itemSecondLink.ElementExtensions);
                Assert.Equal(0, itemSecondLink.Length);
                Assert.Null(itemSecondLink.MediaType);
                Assert.Null(itemSecondLink.RelationshipType);
                Assert.Null(itemSecondLink.Title);
                Assert.Null(itemSecondLink.Uri);

                SyndicationLink itemThirdLink = item.Links[2];
                Assert.Empty(itemThirdLink.AttributeExtensions);
                Assert.Empty(itemThirdLink.ElementExtensions);
                Assert.Equal(0, itemThirdLink.Length);
                Assert.Null(itemThirdLink.MediaType);
                Assert.Null(itemThirdLink.RelationshipType);
                Assert.Null(itemThirdLink.Title);
                Assert.Empty(itemThirdLink.Uri.OriginalString);

                SyndicationLink itemFourthLink = item.Links[3];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, itemFourthLink.AttributeExtensions.Count);
                    Assert.Equal("", itemFourthLink.AttributeExtensions[new XmlQualifiedName("link_name1")]);
                    Assert.Equal("", itemFourthLink.AttributeExtensions[new XmlQualifiedName("link_name2", "link_namespace")]);
                    Assert.Equal("link_value", itemFourthLink.AttributeExtensions[new XmlQualifiedName("link_name3", "link_namespace")]);
                    Assert.Equal("", itemFourthLink.AttributeExtensions[new XmlQualifiedName("link_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(itemFourthLink.AttributeExtensions);
                }
                if (preserveElementExtensions)
                {
                    Assert.Equal(1, itemFourthLink.ElementExtensions.Count);
                    Assert.Equal(10, item.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                }
                else
                {
                    Assert.Empty(itemFourthLink.ElementExtensions);
                }
                Assert.Equal(new Uri("http://link_url.com"), itemFourthLink.BaseUri);
                Assert.Equal(10, itemFourthLink.Length);
                Assert.Equal("link_mediaType", itemFourthLink.MediaType);
                Assert.Equal("link_relationshipType", itemFourthLink.RelationshipType);
                Assert.Equal("link_title", itemFourthLink.Title);
                Assert.Equal(new Uri("http://link_uri.com"), itemFourthLink.Uri);

                Assert.Equal(DateTimeOffset.MinValue, item.PublishDate);

                Assert.Null(item.SourceFeed);

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, item.Summary.AttributeExtensions.Count);
                    Assert.Equal("", item.Summary.AttributeExtensions[new XmlQualifiedName("summary_name1")]);
                    Assert.Equal("", item.Summary.AttributeExtensions[new XmlQualifiedName("summary_name2", "summary_namespace")]);
                    Assert.Equal("summary_value", item.Summary.AttributeExtensions[new XmlQualifiedName("summary_name3", "summary_namespace")]);
                    Assert.Equal("", item.Summary.AttributeExtensions[new XmlQualifiedName("summary_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(item.Summary.AttributeExtensions);
                }
                Assert.Equal("summary_title", item.Summary.Text);
                Assert.Equal("html", item.Summary.Type);

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, item.Title.AttributeExtensions.Count);
                    Assert.Equal("", item.Title.AttributeExtensions[new XmlQualifiedName("title_name1")]);
                    Assert.Equal("", item.Title.AttributeExtensions[new XmlQualifiedName("title_name2", "title_namespace")]);
                    Assert.Equal("title_value", item.Title.AttributeExtensions[new XmlQualifiedName("title_name3", "title_namespace")]);
                    Assert.Equal("", item.Title.AttributeExtensions[new XmlQualifiedName("title_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(item.Title.AttributeExtensions);
                }
                Assert.Equal("title_title", item.Title.Text);
                Assert.Equal("html", item.Title.Type);

                Assert.Equal("language", feed.Language);

                Assert.Equal(DateTimeOffset.MinValue.AddYears(1), feed.LastUpdatedTime);
                Assert.Equal(4, feed.Links.Count);

                SyndicationLink firstLink = feed.Links[0];
                Assert.Empty(firstLink.AttributeExtensions);
                Assert.Empty(firstLink.ElementExtensions);
                Assert.Equal(0, firstLink.Length);
                Assert.Null(firstLink.MediaType);
                Assert.Null(firstLink.RelationshipType);
                Assert.Null(firstLink.Title);
                Assert.Null(firstLink.Uri);

                SyndicationLink secondLink = feed.Links[1];
                Assert.Empty(secondLink.AttributeExtensions);
                Assert.Empty(secondLink.ElementExtensions);
                Assert.Equal(0, secondLink.Length);
                Assert.Null(secondLink.MediaType);
                Assert.Null(secondLink.RelationshipType);
                Assert.Null(secondLink.Title);
                Assert.Null(secondLink.Uri);

                SyndicationLink thirdLink = feed.Links[2];
                Assert.Empty(thirdLink.AttributeExtensions);
                Assert.Empty(thirdLink.ElementExtensions);
                Assert.Equal(0, thirdLink.Length);
                Assert.Null(thirdLink.MediaType);
                Assert.Null(thirdLink.RelationshipType);
                Assert.Null(thirdLink.Title);
                Assert.Empty(thirdLink.Uri.OriginalString);

                SyndicationLink fourthLink = feed.Links[3];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, fourthLink.AttributeExtensions.Count);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("link_name1")]);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("link_name2", "link_namespace")]);
                    Assert.Equal("link_value", fourthLink.AttributeExtensions[new XmlQualifiedName("link_name3", "link_namespace")]);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("link_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(fourthLink.AttributeExtensions);
                }
                if (preserveElementExtensions)
                {
                    Assert.Equal(1, fourthLink.ElementExtensions.Count);
                    Assert.Equal(10, item.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                }
                else
                {
                    Assert.Empty(fourthLink.ElementExtensions);
                }
                Assert.Equal(new Uri("http://link_url.com"), fourthLink.BaseUri);
                Assert.Equal(10, fourthLink.Length);
                Assert.Equal("link_mediaType", fourthLink.MediaType);
                Assert.Equal("link_relationshipType", fourthLink.RelationshipType);
                Assert.Equal("link_title", fourthLink.Title);
                Assert.Equal(new Uri("http://link_uri.com"), fourthLink.Uri);

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, feed.Title.AttributeExtensions.Count);
                    Assert.Equal("", feed.Title.AttributeExtensions[new XmlQualifiedName("feedtitle_name1")]);
                    Assert.Equal("", feed.Title.AttributeExtensions[new XmlQualifiedName("feedtitle_name2", "feedtitle_namespace")]);
                    Assert.Equal("feedtitle_value", feed.Title.AttributeExtensions[new XmlQualifiedName("feedtitle_name3", "feedtitle_namespace")]);
                    Assert.Equal("", feed.Title.AttributeExtensions[new XmlQualifiedName("feedtitle_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(item.Title.AttributeExtensions);
                }
                Assert.Equal("feedtitle_title", feed.Title.Text);
                Assert.Equal("html", feed.Title.Type);
            });
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Read_TryParseTrue_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            using (var stringReader = new StringReader(
@"<feed xml:lang=""language"" xml:base=""http://microsoft.com/"" feed_name1="""" d1p1:feed_name2="""" d1p1:feed_name3=""feed_value"" d1p2:feed_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""feed_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
    <title type=""html"" feedtitle_name1="""" d2p1:feedtitle_name2="""" d2p1:feedtitle_name3=""feedtitle_value"" d1p2:feedtitle_name4="""" xmlns:d2p1=""feedtitle_namespace"">feedtitle_title</title>
    <subtitle type=""html"" feeddescription_name1="""" d2p1:feeddescription_name2="""" d2p1:feeddescription_name3=""feeddescription_value"" d1p2:feeddescription_name4="""" xmlns:d2p1=""feeddescription_namespace"">feeddescription_title</subtitle>
    <id>id</id>
    <rights type=""html"" feedcopyright_name1="""" d2p1:feedcopyright_name2="""" d2p1:feedcopyright_name3=""feedcopyright_value"" d1p2:feedcopyright_name4="""" xmlns:d2p1=""feedcopyright_namespace"">feedcopyright_title</rights>
    <updated>0002-01-01T00:00:00Z</updated>
    <category />
    <category></category>
    <category term="""" />
    <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d2p1=""category_namespace"">
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </category>
    <logo>http://imageurl.com/</logo>
    <author />
    <author></author>
    <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d2p1=""author_namespace"">
        <name>author_name</name>
        <uri>author_uri</uri>
        <email>author_email</email>
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </author>
    <contributor />
    <contributor></contributor>
    <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"">
        <name>contributor_name</name>
        <uri>contributor_uri</uri>
        <email>contributor_email</email>
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </contributor>
    <generator>generator</generator>
    <link />
    <link></link>
    <link href="""" />
    <link xml:base=""http://link_url.com/"" link_name1="""" d2p1:link_name2="""" d2p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d2p1=""link_namespace"">
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </link>
    <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </Atom10FeedFormatterTests.ExtensionObject>
    <entry>
        <id>id</id>
        <title type=""text"" />
        <updated>0001-01-01T00:00:00Z</updated>
    </entry>
    <entry xml:base=""/relative"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <id>id</id>
        <title type=""html"" title_name1="""" d2p1:title_name2="""" d2p1:title_name3=""title_value"" d1p2:title_name4="""" xmlns:d2p1=""title_namespace"">title_title</title>
        <summary type=""html"" summary_name1="""" d2p1:summary_name2="""" d2p1:summary_name3=""summary_value"" d1p2:summary_name4="""" xmlns:d2p1=""summary_namespace"">summary_title</summary>
        <published>0001-01-01T00:00:00Z</published>
        <updated>0001-01-01T00:00:00Z</updated>
        <author />
        <author></author>
        <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d2p1=""author_namespace"">
            <name>author_name</name>
            <uri>author_uri</uri>
            <email>author_email</email>
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </author>
        <contributor />
        <contributor></contributor>
        <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"">
            <name>contributor_name</name>
            <uri>contributor_uri</uri>
            <email>contributor_email</email>
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </contributor>
        <link />
        <link></link>
        <link href="""" />
        <link xml:base=""http://link_url.com/"" link_name1="""" d2p1:link_name2="""" d2p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d2p1=""link_namespace"">
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </link>
        <category />
        <category></category>
        <category term="""" />
        <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d2p1=""category_namespace"">
            <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </Atom10FeedFormatterTests.ExtensionObject>
        </category>
        <content type=""html"" content_name1="""" d2p1:content_name2="""" d2p1:content_name3=""content_value"" d1p2:content_name4="""" xmlns:d2p1=""content_namespace"">content_title</content>
        <rights type=""html"" copyright_name1="""" d2p1:copyright_name2="""" d2p1:copyright_name3=""copyright_value"" d1p2:copyright_name4="""" xmlns:d2p1=""copyright_namespace"">copyright_title</rights>
        <Atom10FeedFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10FeedFormatterTests.ExtensionObject>
    </entry>
</feed>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Atom10FeedFormatter<SyndicationFeedTryParseTrueSubclass>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);

                SyndicationFeed feed = formatter.Feed;
                Assert.Empty(feed.AttributeExtensions);

                Assert.Equal(3, feed.Authors.Count);

                SyndicationPerson firstAuthor = feed.Authors[0];
                Assert.Empty(firstAuthor.AttributeExtensions);
                Assert.Empty(firstAuthor.ElementExtensions);
                Assert.Null(firstAuthor.Email);
                Assert.Null(firstAuthor.Name);
                Assert.Null(firstAuthor.Uri);

                SyndicationPerson secondAuthor = feed.Authors[1];
                Assert.Empty(secondAuthor.AttributeExtensions);
                Assert.Empty(secondAuthor.ElementExtensions);
                Assert.Null(secondAuthor.Email);
                Assert.Null(secondAuthor.Name);
                Assert.Null(secondAuthor.Uri);

                SyndicationPerson thirdAuthor = feed.Authors[2];
                Assert.Empty(thirdAuthor.AttributeExtensions);
                Assert.Empty(thirdAuthor.ElementExtensions);
                Assert.Equal("author_email", thirdAuthor.Email);
                Assert.Equal("author_name", thirdAuthor.Name);
                Assert.Equal("author_uri", thirdAuthor.Uri);

                Assert.Equal(new Uri("http://microsoft.com"), feed.BaseUri);

                Assert.Equal(4, feed.Categories.Count);
                SyndicationCategory firstCategory = feed.Categories[0];
                Assert.Empty(firstCategory.AttributeExtensions);
                Assert.Empty(firstCategory.ElementExtensions);
                Assert.Null(firstCategory.Name);
                Assert.Null(firstCategory.Scheme);
                Assert.Null(firstCategory.Label);

                SyndicationCategory secondCategory = feed.Categories[1];
                Assert.Empty(secondCategory.AttributeExtensions);
                Assert.Empty(secondCategory.ElementExtensions);
                Assert.Null(secondCategory.Name);
                Assert.Null(secondCategory.Scheme);
                Assert.Null(secondCategory.Label);

                SyndicationCategory thirCategory = feed.Categories[2];
                Assert.Empty(thirCategory.AttributeExtensions);
                Assert.Empty(thirCategory.ElementExtensions);
                Assert.Empty(thirCategory.Name);
                Assert.Null(thirCategory.Scheme);
                Assert.Null(thirCategory.Label);

                SyndicationCategory fourthCategory = feed.Categories[3];
                Assert.Empty(fourthCategory.AttributeExtensions);
                Assert.Empty(fourthCategory.ElementExtensions);
                Assert.Equal("category_name", fourthCategory.Name);
                Assert.Equal("category_scheme", fourthCategory.Scheme);
                Assert.Equal("category_label", fourthCategory.Label);

                Assert.Equal(3, feed.Contributors.Count);
                SyndicationPerson firstContributor = feed.Contributors[0];
                Assert.Empty(firstContributor.AttributeExtensions);
                Assert.Empty(firstContributor.ElementExtensions);
                Assert.Null(firstContributor.Email);
                Assert.Null(firstContributor.Name);
                Assert.Null(firstContributor.Uri);

                SyndicationPerson secondContributor = feed.Contributors[1];
                Assert.Empty(secondContributor.AttributeExtensions);
                Assert.Empty(secondContributor.ElementExtensions);
                Assert.Null(secondContributor.Email);
                Assert.Null(secondContributor.Name);
                Assert.Null(secondContributor.Uri);

                SyndicationPerson thirdContributor = feed.Contributors[2];
                Assert.Empty(thirdContributor.AttributeExtensions);
                Assert.Empty(thirdContributor.ElementExtensions);
                Assert.Equal("contributor_email", thirdContributor.Email);
                Assert.Equal("contributor_name", thirdContributor.Name);
                Assert.Equal("contributor_uri", thirdContributor.Uri);

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, feed.Copyright.AttributeExtensions.Count);
                    Assert.Equal("", feed.Copyright.AttributeExtensions[new XmlQualifiedName("feedcopyright_name1")]);
                    Assert.Equal("", feed.Copyright.AttributeExtensions[new XmlQualifiedName("feedcopyright_name2", "feedcopyright_namespace")]);
                    Assert.Equal("feedcopyright_value", feed.Copyright.AttributeExtensions[new XmlQualifiedName("feedcopyright_name3", "feedcopyright_namespace")]);
                    Assert.Equal("", feed.Copyright.AttributeExtensions[new XmlQualifiedName("feedcopyright_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(feed.Copyright.AttributeExtensions);
                }
                Assert.Equal("feedcopyright_title", feed.Copyright.Text);
                Assert.Equal("html", feed.Copyright.Type);

                Assert.Equal("generator", feed.Generator);

                Assert.Empty(feed.ElementExtensions);

                Assert.Equal("id", feed.Id);

                Assert.Equal(new Uri("http://imageurl.com/"), feed.ImageUrl);

                SyndicationItem[] items = feed.Items.ToArray();
                Assert.Equal(2, items.Length);

                SyndicationItem item = items[1];
                Assert.Empty(item.AttributeExtensions);

                Assert.Equal(3, item.Authors.Count);

                SyndicationPerson itemFirstAuthor = item.Authors[0];
                Assert.Empty(itemFirstAuthor.AttributeExtensions);
                Assert.Empty(itemFirstAuthor.ElementExtensions);
                Assert.Null(itemFirstAuthor.Email);
                Assert.Null(itemFirstAuthor.Name);
                Assert.Null(itemFirstAuthor.Uri);

                SyndicationPerson itemSecondAuthor = item.Authors[1];
                Assert.Empty(itemSecondAuthor.AttributeExtensions);
                Assert.Empty(itemSecondAuthor.ElementExtensions);
                Assert.Null(itemSecondAuthor.Email);
                Assert.Null(itemSecondAuthor.Name);
                Assert.Null(itemSecondAuthor.Uri);

                SyndicationPerson itemThirdAuthor = item.Authors[2];
                Assert.Empty(itemThirdAuthor.AttributeExtensions);
                Assert.Empty(itemThirdAuthor.ElementExtensions);
                Assert.Equal("author_email", itemThirdAuthor.Email);
                Assert.Equal("author_name", itemThirdAuthor.Name);
                Assert.Equal("author_uri", itemThirdAuthor.Uri);

                Assert.Equal(new Uri("http://microsoft.com/relative"), item.BaseUri);

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
                Assert.Null(itemSecondCategory.Name);
                Assert.Null(itemSecondCategory.Scheme);
                Assert.Null(itemSecondCategory.Label);

                SyndicationCategory itemThirdCategory = item.Categories[2];
                Assert.Empty(itemThirdCategory.AttributeExtensions);
                Assert.Empty(itemThirdCategory.ElementExtensions);
                Assert.Empty(itemThirdCategory.Name);
                Assert.Null(itemThirdCategory.Scheme);
                Assert.Null(itemThirdCategory.Label);

                SyndicationCategory itemFourthCategory = item.Categories[3];
                Assert.Empty(itemFourthCategory.AttributeExtensions);
                Assert.Empty(itemFourthCategory.ElementExtensions);
                Assert.Equal("category_name", itemFourthCategory.Name);
                Assert.Equal("category_scheme", itemFourthCategory.Scheme);
                Assert.Equal("category_label", itemFourthCategory.Label);

                TextSyndicationContent content = Assert.IsType<TextSyndicationContent>(item.Content);
                Assert.Empty(content.AttributeExtensions);
                Assert.Equal("overridden", content.Text);
                Assert.Equal("text", content.Type);

                Assert.Equal(3, item.Contributors.Count);

                SyndicationPerson itemFirstContributor = item.Contributors[0];
                Assert.Empty(itemFirstContributor.AttributeExtensions);
                Assert.Empty(itemFirstContributor.ElementExtensions);
                Assert.Null(itemFirstContributor.Email);
                Assert.Null(itemFirstContributor.Name);
                Assert.Null(itemFirstContributor.Uri);

                SyndicationPerson itemSecondContributor = item.Contributors[1];
                Assert.Empty(itemSecondContributor.AttributeExtensions);
                Assert.Empty(itemSecondContributor.ElementExtensions);
                Assert.Null(itemSecondContributor.Email);
                Assert.Null(itemSecondContributor.Name);
                Assert.Null(itemSecondContributor.Uri);

                SyndicationPerson itemThirdContributor = item.Contributors[2];
                Assert.Empty(itemThirdContributor.AttributeExtensions);
                Assert.Empty(itemThirdContributor.ElementExtensions);
                Assert.Equal("contributor_email", itemThirdContributor.Email);
                Assert.Equal("contributor_name", itemThirdContributor.Name);
                Assert.Equal("contributor_uri", thirdContributor.Uri);

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, item.Copyright.AttributeExtensions.Count);
                    Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name1")]);
                    Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name2", "copyright_namespace")]);
                    Assert.Equal("copyright_value", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name3", "copyright_namespace")]);
                    Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(item.Copyright.AttributeExtensions);
                }
                Assert.Equal("copyright_title", item.Copyright.Text);
                Assert.Equal("html", item.Copyright.Type);

                Assert.Empty(item.ElementExtensions);

                Assert.Equal("id", item.Id);

                Assert.Equal(DateTimeOffset.MinValue, item.LastUpdatedTime);

                Assert.Equal(4, item.Links.Count);

                SyndicationLink itemFirstLink = item.Links[0];
                Assert.Empty(itemFirstLink.AttributeExtensions);
                Assert.Empty(itemFirstLink.ElementExtensions);
                Assert.Equal(0, itemFirstLink.Length);
                Assert.Null(itemFirstLink.MediaType);
                Assert.Null(itemFirstLink.RelationshipType);
                Assert.Null(itemFirstLink.Title);
                Assert.Null(itemFirstLink.Uri);

                SyndicationLink itemSecondLink = item.Links[1];
                Assert.Empty(itemSecondLink.AttributeExtensions);
                Assert.Empty(itemSecondLink.ElementExtensions);
                Assert.Equal(0, itemSecondLink.Length);
                Assert.Null(itemSecondLink.MediaType);
                Assert.Null(itemSecondLink.RelationshipType);
                Assert.Null(itemSecondLink.Title);
                Assert.Null(itemSecondLink.Uri);

                SyndicationLink itemThirdLink = item.Links[2];
                Assert.Empty(itemThirdLink.AttributeExtensions);
                Assert.Empty(itemThirdLink.ElementExtensions);
                Assert.Equal(0, itemThirdLink.Length);
                Assert.Null(itemThirdLink.MediaType);
                Assert.Null(itemThirdLink.RelationshipType);
                Assert.Null(itemThirdLink.Title);
                Assert.Empty(itemThirdLink.Uri.OriginalString);

                SyndicationLink itemFourthLink = item.Links[3];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, itemFourthLink.AttributeExtensions.Count);
                    Assert.Equal("", itemFourthLink.AttributeExtensions[new XmlQualifiedName("link_name1")]);
                    Assert.Equal("", itemFourthLink.AttributeExtensions[new XmlQualifiedName("link_name2", "link_namespace")]);
                    Assert.Equal("link_value", itemFourthLink.AttributeExtensions[new XmlQualifiedName("link_name3", "link_namespace")]);
                    Assert.Equal("", itemFourthLink.AttributeExtensions[new XmlQualifiedName("link_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(itemFourthLink.AttributeExtensions);
                }
                Assert.Empty(itemFourthLink.ElementExtensions);
                Assert.Equal(new Uri("http://link_url.com"), itemFourthLink.BaseUri);
                Assert.Equal(10, itemFourthLink.Length);
                Assert.Equal("link_mediaType", itemFourthLink.MediaType);
                Assert.Equal("link_relationshipType", itemFourthLink.RelationshipType);
                Assert.Equal("link_title", itemFourthLink.Title);
                Assert.Equal(new Uri("http://link_uri.com"), itemFourthLink.Uri);

                Assert.Equal(DateTimeOffset.MinValue, item.PublishDate);

                Assert.Null(item.SourceFeed);

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, item.Summary.AttributeExtensions.Count);
                    Assert.Equal("", item.Summary.AttributeExtensions[new XmlQualifiedName("summary_name1")]);
                    Assert.Equal("", item.Summary.AttributeExtensions[new XmlQualifiedName("summary_name2", "summary_namespace")]);
                    Assert.Equal("summary_value", item.Summary.AttributeExtensions[new XmlQualifiedName("summary_name3", "summary_namespace")]);
                    Assert.Equal("", item.Summary.AttributeExtensions[new XmlQualifiedName("summary_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(item.Summary.AttributeExtensions);
                }
                Assert.Equal("summary_title", item.Summary.Text);
                Assert.Equal("html", item.Summary.Type);

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, item.Title.AttributeExtensions.Count);
                    Assert.Equal("", item.Title.AttributeExtensions[new XmlQualifiedName("title_name1")]);
                    Assert.Equal("", item.Title.AttributeExtensions[new XmlQualifiedName("title_name2", "title_namespace")]);
                    Assert.Equal("title_value", item.Title.AttributeExtensions[new XmlQualifiedName("title_name3", "title_namespace")]);
                    Assert.Equal("", item.Title.AttributeExtensions[new XmlQualifiedName("title_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(item.Title.AttributeExtensions);
                }
                Assert.Equal("title_title", item.Title.Text);
                Assert.Equal("html", item.Title.Type);

                Assert.Equal("language", feed.Language);

                Assert.Equal(DateTimeOffset.MinValue.AddYears(1), feed.LastUpdatedTime);
                Assert.Equal(4, feed.Links.Count);

                SyndicationLink firstLink = feed.Links[0];
                Assert.Empty(firstLink.AttributeExtensions);
                Assert.Empty(firstLink.ElementExtensions);
                Assert.Equal(0, firstLink.Length);
                Assert.Null(firstLink.MediaType);
                Assert.Null(firstLink.RelationshipType);
                Assert.Null(firstLink.Title);
                Assert.Null(firstLink.Uri);

                SyndicationLink secondLink = feed.Links[1];
                Assert.Empty(secondLink.AttributeExtensions);
                Assert.Empty(secondLink.ElementExtensions);
                Assert.Equal(0, secondLink.Length);
                Assert.Null(secondLink.MediaType);
                Assert.Null(secondLink.RelationshipType);
                Assert.Null(secondLink.Title);
                Assert.Null(secondLink.Uri);

                SyndicationLink thirdLink = feed.Links[2];
                Assert.Empty(thirdLink.AttributeExtensions);
                Assert.Empty(thirdLink.ElementExtensions);
                Assert.Equal(0, thirdLink.Length);
                Assert.Null(thirdLink.MediaType);
                Assert.Null(thirdLink.RelationshipType);
                Assert.Null(thirdLink.Title);
                Assert.Empty(thirdLink.Uri.OriginalString);

                SyndicationLink fourthLink = feed.Links[3];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, fourthLink.AttributeExtensions.Count);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("link_name1")]);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("link_name2", "link_namespace")]);
                    Assert.Equal("link_value", fourthLink.AttributeExtensions[new XmlQualifiedName("link_name3", "link_namespace")]);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("link_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(fourthLink.AttributeExtensions);
                }
                Assert.Empty(fourthLink.ElementExtensions);
                Assert.Equal(new Uri("http://link_url.com"), fourthLink.BaseUri);
                Assert.Equal(10, fourthLink.Length);
                Assert.Equal("link_mediaType", fourthLink.MediaType);
                Assert.Equal("link_relationshipType", fourthLink.RelationshipType);
                Assert.Equal("link_title", fourthLink.Title);
                Assert.Equal(new Uri("http://link_uri.com"), fourthLink.Uri);

                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, feed.Title.AttributeExtensions.Count);
                    Assert.Equal("", feed.Title.AttributeExtensions[new XmlQualifiedName("feedtitle_name1")]);
                    Assert.Equal("", feed.Title.AttributeExtensions[new XmlQualifiedName("feedtitle_name2", "feedtitle_namespace")]);
                    Assert.Equal("feedtitle_value", feed.Title.AttributeExtensions[new XmlQualifiedName("feedtitle_name3", "feedtitle_namespace")]);
                    Assert.Equal("", feed.Title.AttributeExtensions[new XmlQualifiedName("feedtitle_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(item.Title.AttributeExtensions);
                }
                Assert.Equal("feedtitle_title", feed.Title.Text);
                Assert.Equal("html", feed.Title.Type);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Read_EmptyItem_ReturnsExpected(bool preserveElementExtensions)
        {
            VerifyRead(@"<feed xmlns=""http://www.w3.org/2005/Atom""></feed>", preserveElementExtensions, preserveElementExtensions, feed =>
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

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Read_CustomReadItems_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            using (var stringReader = new StringReader(@"<feed xmlns=""http://www.w3.org/2005/Atom""><entry></entry><entry></entry></feed>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new CustomAtom10FeedFormatter()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);

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
                Assert.Single(feed.Items);
                Assert.Null(feed.Language);
                Assert.Equal(DateTimeOffset.MinValue, feed.LastUpdatedTime);
                Assert.Empty(feed.Links);
                Assert.Null(feed.Title);
            }
        }

        private class CustomAtom10FeedFormatter : Atom10FeedFormatter
        {
            protected override IEnumerable<SyndicationItem> ReadItems(XmlReader reader, SyndicationFeed feed, out bool areAllItemsRead)
            {
                areAllItemsRead = false;
                return new SyndicationItem[] { new SyndicationItem() };
            }
        }

        private static void VerifyRead(string xmlString, bool preserveAttributeExtensions, bool preserveElementExtensions, Action<SyndicationFeed> verifyAction)
        {
            // ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Atom10FeedFormatter()
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

                var formatter = new Atom10FeedFormatter()
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
                var formatter = new Atom10FeedFormatter(typeof(SyndicationFeedSubclass))
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

                var formatter = new Atom10FeedFormatter(typeof(SyndicationFeedSubclass))
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
                var formatter = new Atom10FeedFormatter<SyndicationFeed>()
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

                var formatter = new Atom10FeedFormatter<SyndicationFeed>()
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
                var formatter = new Atom10FeedFormatter<SyndicationFeedSubclass>()
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

                var formatter = new Atom10FeedFormatter<SyndicationFeedSubclass>()
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
            var formatter = new Atom10FeedFormatter();
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
        [InlineData(@"<feed xmlns=""different""></entry>")]
        [InlineData(@"<feed></feed>")]
        [InlineData(@"<feed/>")]
        [InlineData(@"<feed xmlns=""http://www.w3.org/2005/Atom"" />")]
        public void ReadFrom_CantRead_ThrowsXmlException(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Atom10FeedFormatter();
                Assert.Throws<XmlException>(() => formatter.ReadFrom(reader));
            }
        }

        [Theory]
        [InlineData("<feed></feed>")]
        [InlineData(@"<app:feed xmlns:app=""http://www.w3.org/2005/Atom""></app:feed>")]
        [InlineData(@"<feed xmlns=""different""></feed>")]
        [InlineData(@"<different xmlns=""http://www.w3.org/2005/Atom""></different>")]
        public void ReadXml_ValidReader_Success(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Atom10FeedFormatter();
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
            IXmlSerializable formatter = new Atom10FeedFormatter();
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
        [InlineData("<feed />")]
        [InlineData(@"<feed xmlns:app=""http://www.w3.org/2005/Atom"" />")]
        [InlineData("<different />")]
        public void ReadXml_CantRead_ThrowsXmlException(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                IXmlSerializable formatter = new Atom10FeedFormatter();
                Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
            }
        }

        [Fact]
        public void ReadXml_ThrowsArgumentException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new ArgumentException());
            IXmlSerializable formatter = new Atom10FeedFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Fact]
        public void ReadXml_ThrowsFormatException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new FormatException());
            IXmlSerializable formatter = new Atom10FeedFormatter();
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
    <entry xmlns=""http://www.w3.org/2005/Atom""><id>id1</id></entry>
    <entry xmlns=""http://www.w3.org/2005/Atom""><id>id2</id></entry>
    <unknown></unknown>
    <entry xmlns=""http://www.w3.org/2005/Atom""><id>id3</id></entry>
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
        [InlineData("")]
        [InlineData("invalid")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Custom date parsing added in .NET Core changes this behaviour")]
        public void Read_InvalidLastUpdatedTime_GetThrowsXmlExcepton(string updated)
        {
            using (var stringReader = new StringReader(@"<feed xmlns=""http://www.w3.org/2005/Atom""><updated>" + updated + "</updated></feed>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Atom10FeedFormatter();
                formatter.ReadFrom(reader);
                Assert.Throws<XmlException>(() => formatter.Feed.LastUpdatedTime);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PreserveAttributeExtensions_Set_GetReturnsExpected(bool preserveAttributeExtensions)
        {
            var formatter = new Atom10FeedFormatter() { PreserveAttributeExtensions = preserveAttributeExtensions };
            Assert.Equal(preserveAttributeExtensions, formatter.PreserveAttributeExtensions);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PreserveElementExtensions_Set_GetReturnsExpected(bool preserveElementExtensions)
        {
            var formatter = new Atom10FeedFormatter() { PreserveElementExtensions = preserveElementExtensions };
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

                content = new TextSyndicationContent("overridden");
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

        public class Formatter : Atom10FeedFormatter
        {
            public Formatter() : base() { }

            public Formatter(SyndicationFeed feedToWrite) : base(feedToWrite) { }

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

        public class GenericFormatter<T> : Atom10FeedFormatter<T> where T : SyndicationFeed, new()
        {
            public GenericFormatter() : base() { }

            public GenericFormatter(T feedToWrite) : base(feedToWrite) { }

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
