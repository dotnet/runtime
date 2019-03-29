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
    public class Rss20ItemFormatterTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var formatter = new Formatter();
            Assert.Null(formatter.Item);
            Assert.Equal(typeof(SyndicationItem), formatter.ItemTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void Ctor_GenericDefault()
        {
            var formatter = new GenericFormatter<SyndicationItem>();
            Assert.Null(formatter.Item);
            Assert.Equal(typeof(SyndicationItem), formatter.ItemTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void Ctor_SyndicationItem()
        {
            var item = new SyndicationItem();
            var formatter = new Formatter(item);
            Assert.Equal(typeof(SyndicationItem), formatter.ItemTypeEntryPoint);
            Assert.Same(item, formatter.Item);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_SyndicationItem_Bool(bool serializeExtensionsAsAtom)
        {
            var item = new SyndicationItem();
            var formatter = new Formatter(item, serializeExtensionsAsAtom);
            Assert.Equal(typeof(SyndicationItem), formatter.ItemTypeEntryPoint);
            Assert.Same(item, formatter.Item);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void Ctor_GenericSyndicationItem()
        {
            var item = new SyndicationItem();
            var formatter = new GenericFormatter<SyndicationItem>(item);
            Assert.Same(item, formatter.Item);
            Assert.Equal(typeof(SyndicationItem), formatter.ItemTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_GenericSyndicationItem_Bool(bool serializeExtensionsAsAtom)
        {
            var item = new SyndicationItem();
            var formatter = new GenericFormatter<SyndicationItem>(item, serializeExtensionsAsAtom);
            Assert.Equal(typeof(SyndicationItem), formatter.ItemTypeEntryPoint);
            Assert.Same(item, formatter.Item);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void Ctor_NullItemToWrite_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("itemToWrite", () => new Rss20ItemFormatter((SyndicationItem)null));
            AssertExtensions.Throws<ArgumentNullException>("itemToWrite", () => new Rss20ItemFormatter<SyndicationItem>(null));
        }

        [Theory]
        [InlineData(typeof(SyndicationItem))]
        [InlineData(typeof(SyndicationItemSubclass))]
        public void Ctor_Type(Type itemTypeToCreate)
        {
            var formatter = new Formatter(itemTypeToCreate);
            Assert.Null(formatter.Item);
            Assert.Equal(itemTypeToCreate, formatter.ItemTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void Ctor_NullItemTypeToCreate_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("itemTypeToCreate", () => new Rss20ItemFormatter((Type)null));
        }

        [Fact]
        public void Ctor_InvalidItemTypeToCreate_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("itemTypeToCreate", () => new Rss20ItemFormatter(typeof(int)));
        }

        [Fact]
        public void GetSchema_Invoke_ReturnsNull()
        {
            IXmlSerializable formatter = new Rss20ItemFormatter();
            Assert.Null(formatter.GetSchema());
        }

        public static IEnumerable<object[]> WriteTo_TestData()
        {
            yield return new object[]
            {
                new SyndicationItem(),
                true,
@"<item>
    <description />
</item>"
            };

            yield return new object[]
            {
                new SyndicationItem(),
                false,
@"<item>
    <description />
</item>"
            };

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

            var fullSyndicationCategory = new SyndicationCategory();
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name1"), null);
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name2", "category_namespace"), "");
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name3", "category_namespace"), "category_value");
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name4", "xmlns"), "");

            fullSyndicationCategory.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            fullSyndicationCategory.Label = "category_label";

            fullSyndicationCategory.Name = "category_name";

            fullSyndicationCategory.Scheme = "category_scheme";

            var attributeSyndicationCategory = new SyndicationCategory();
            attributeSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("term"), "term_value");
            attributeSyndicationCategory.Name = "name";

            var fullSyndicationLink = new SyndicationLink();
            fullSyndicationLink.AttributeExtensions.Add(new XmlQualifiedName("link_name1"), null);
            fullSyndicationLink.AttributeExtensions.Add(new XmlQualifiedName("link_name2", "link_namespace"), "");
            fullSyndicationLink.AttributeExtensions.Add(new XmlQualifiedName("link_name3", "link_namespace"), "link_value");
            fullSyndicationLink.AttributeExtensions.Add(new XmlQualifiedName("link_name4", "xmlns"), "");

            fullSyndicationLink.BaseUri = new Uri("http://link_url.com");

            fullSyndicationLink.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            fullSyndicationLink.Length = 10;

            fullSyndicationLink.MediaType = "link_mediaType";

            fullSyndicationLink.RelationshipType = "link_relationshipType";

            fullSyndicationLink.Title = "link_title";

            fullSyndicationLink.Uri = new Uri("http://link_uri.com");

            var fullEnclosureLink = new SyndicationLink();
            fullEnclosureLink.AttributeExtensions.Add(new XmlQualifiedName("enclosure_name1"), null);
            fullEnclosureLink.AttributeExtensions.Add(new XmlQualifiedName("enclosure_name2", "enclosure_namespace"), "");
            fullEnclosureLink.AttributeExtensions.Add(new XmlQualifiedName("enclosure_name3", "enclosure_namespace"), "item_value");
            fullEnclosureLink.AttributeExtensions.Add(new XmlQualifiedName("enclosure_name4", "xmlns"), "");

            fullEnclosureLink.BaseUri = new Uri("http://link_url.com");

            fullEnclosureLink.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            fullEnclosureLink.Length = 10;

            fullEnclosureLink.MediaType = "enclosure_mediaType";

            fullEnclosureLink.RelationshipType = "enclosure";

            fullEnclosureLink.Title = "enclosure_title";

            fullEnclosureLink.Uri = new Uri("http://enclosure_uri.com");

            var fullAlternateLink = new SyndicationLink();
            fullAlternateLink.AttributeExtensions.Add(new XmlQualifiedName("alternate_name1"), null);
            fullAlternateLink.AttributeExtensions.Add(new XmlQualifiedName("alternate_name2", "alternate_namespace"), "");
            fullAlternateLink.AttributeExtensions.Add(new XmlQualifiedName("alternate_name3", "alternate_namespace"), "alternate_value");
            fullAlternateLink.AttributeExtensions.Add(new XmlQualifiedName("alternate_name4", "xmlns"), "");

            fullAlternateLink.BaseUri = new Uri("http://alternate_url.com");

            fullAlternateLink.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            fullAlternateLink.Length = 10;

            fullAlternateLink.MediaType = "alternate_mediaType";

            fullAlternateLink.RelationshipType = "alternate";

            fullAlternateLink.Title = "alternate_title";

            fullAlternateLink.Uri = new Uri("http://alternate_uri.com");

            var attributeSyndicationLink = new SyndicationLink
            {
                Uri = new Uri("http://link_uri.com")
            };
            attributeSyndicationLink.AttributeExtensions.Add(new XmlQualifiedName("href"), "link_href");

            var fullSyndicationItem = new SyndicationItem();
            fullSyndicationItem.AttributeExtensions.Add(new XmlQualifiedName("item_name1"), null);
            fullSyndicationItem.AttributeExtensions.Add(new XmlQualifiedName("item_name2", "item_namespace"), "");
            fullSyndicationItem.AttributeExtensions.Add(new XmlQualifiedName("item_name3", "item_namespace"), "item_value");
            fullSyndicationItem.AttributeExtensions.Add(new XmlQualifiedName("item_name4", "xmlns"), "");

            fullSyndicationItem.Authors.Add(new SyndicationPerson());
            fullSyndicationItem.Authors.Add(CreatePerson("author"));

            fullSyndicationItem.BaseUri = new Uri("/relative", UriKind.Relative);

            fullSyndicationItem.Categories.Add(new SyndicationCategory());
            fullSyndicationItem.Categories.Add(fullSyndicationCategory);
            fullSyndicationItem.Categories.Add(attributeSyndicationCategory);

            fullSyndicationItem.Content = CreateContent("content");

            fullSyndicationItem.Contributors.Add(new SyndicationPerson());
            fullSyndicationItem.Contributors.Add(CreatePerson("contributor"));

            fullSyndicationItem.Copyright = CreateContent("copyright");

            fullSyndicationItem.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            fullSyndicationItem.Id = "id";

            fullSyndicationItem.LastUpdatedTime = DateTimeOffset.MinValue.AddTicks(100);

            fullSyndicationItem.Links.Add(new SyndicationLink());
            fullSyndicationItem.Links.Add(fullSyndicationLink);
            fullSyndicationItem.Links.Add(fullEnclosureLink);
            fullSyndicationItem.Links.Add(new SyndicationLink { RelationshipType = "enclosure" });
            fullSyndicationItem.Links.Add(new SyndicationLink { RelationshipType = "alternate" });
            fullSyndicationItem.Links.Add(fullAlternateLink);
            fullSyndicationItem.Links.Add(attributeSyndicationLink);

            fullSyndicationItem.PublishDate = DateTimeOffset.MinValue.AddTicks(200);

            fullSyndicationItem.Summary = CreateContent("summary");

            fullSyndicationItem.Title = CreateContent("title");

            yield return new object[]
            {
                fullSyndicationItem,
                true,
@"<item xml:base=""/relative"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"">
    <guid isPermaLink=""false"">id</guid>
    <link />
    <author xmlns=""http://www.w3.org/2005/Atom"" />
    <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d2p1=""author_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <name>author_name</name>
        <uri>author_uri</uri>
        <email>author_email</email>
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </author>
    <category />
    <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" domain=""category_scheme"" xmlns:d2p1=""category_namespace"">category_name</category>
    <category term=""term_value"">name</category>
    <title>title_title</title>
    <description>summary_title</description>
    <pubDate>Mon, 01 Jan 0001 00:00:00 Z</pubDate>
    <link href="""" xmlns=""http://www.w3.org/2005/Atom"" />
    <link xml:base=""http://link_url.com/"" link_name1="""" d2p1:link_name2="""" d2p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d2p1=""link_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </link>
    <enclosure xml:base=""http://link_url.com/"" enclosure_name1="""" d2p1:enclosure_name2="""" d2p1:enclosure_name3=""item_value"" d1p2:enclosure_name4="""" url=""http://enclosure_uri.com/"" type=""enclosure_mediaType"" length=""10"" xmlns:d2p1=""enclosure_namespace"" />
    <link rel=""enclosure"" href="""" xmlns=""http://www.w3.org/2005/Atom"" />
    <link xml:base=""http://alternate_url.com/"" alternate_name1="""" d2p1:alternate_name2="""" d2p1:alternate_name3=""alternate_value"" d1p2:alternate_name4="""" rel=""alternate"" type=""alternate_mediaType"" title=""alternate_title"" length=""10"" href=""http://alternate_uri.com/"" xmlns:d2p1=""alternate_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </link>
    <link href=""link_href"" xmlns=""http://www.w3.org/2005/Atom"" />
    <updated xmlns=""http://www.w3.org/2005/Atom"">0001-01-01T00:00:00Z</updated>
    <rights type=""html"" copyright_name1="""" d2p1:copyright_name2="""" d2p1:copyright_name3=""copyright_value"" d1p2:copyright_name4="""" xmlns:d2p1=""copyright_namespace"" xmlns=""http://www.w3.org/2005/Atom"">copyright_title</rights>
    <content type=""html"" content_name1="""" d2p1:content_name2="""" d2p1:content_name3=""content_value"" d1p2:content_name4="""" xmlns:d2p1=""content_namespace"" xmlns=""http://www.w3.org/2005/Atom"">content_title</content>
    <contributor xmlns=""http://www.w3.org/2005/Atom"" />
    <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <name>contributor_name</name>
        <uri>contributor_uri</uri>
        <email>contributor_email</email>
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </contributor>
    <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </Rss20ItemFormatterTests.ExtensionObject>
</item>"
            };

            yield return new object[]
            {
                fullSyndicationItem,
                false,
@"<item xml:base=""/relative"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"">
    <guid isPermaLink=""false"">id</guid>
    <link />
    <category />
    <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" domain=""category_scheme"" xmlns:d2p1=""category_namespace"">category_name</category>
    <category term=""term_value"">name</category>
    <title>title_title</title>
    <description>summary_title</description>
    <pubDate>Mon, 01 Jan 0001 00:00:00 Z</pubDate>
    <enclosure xml:base=""http://link_url.com/"" enclosure_name1="""" d2p1:enclosure_name2="""" d2p1:enclosure_name3=""item_value"" d1p2:enclosure_name4="""" url=""http://enclosure_uri.com/"" type=""enclosure_mediaType"" length=""10"" xmlns:d2p1=""enclosure_namespace"" />
    <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </Rss20ItemFormatterTests.ExtensionObject>
</item>"
            };

            foreach (string email in new string[] { null, "" })
            {
                var oneAuthorWithInvalidEmail = new SyndicationItem();
                oneAuthorWithInvalidEmail.Authors.Add(new SyndicationPerson(email));

                yield return new object[]
                {
                    oneAuthorWithInvalidEmail,
                    true,
@"<item>
    <author xmlns=""http://www.w3.org/2005/Atom"" />
    <description />
</item>"
                };

                yield return new object[]
                {
                    oneAuthorWithInvalidEmail,
                    false,
@"<item>
    <description />
</item>"
                };
            }

            var oneAuthorWithValidEmail = new SyndicationItem();
            oneAuthorWithValidEmail.Authors.Add(CreatePerson("author"));

            foreach (bool serializeExtensionsAsAtom in new bool[] { true, false })
            {
                yield return new object[]
                {
                    oneAuthorWithValidEmail,
                    serializeExtensionsAsAtom,
@"<item>
    <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d2p2:author_name4="""" xmlns:d2p2=""xmlns"" xmlns:d2p1=""author_namespace"">author_email</author>
    <description />
</item>"
                };
            };

            var attributeEnclosureLink = new SyndicationLink();
            attributeEnclosureLink.AttributeExtensions.Add(new XmlQualifiedName("length"), "100");
            attributeEnclosureLink.AttributeExtensions.Add(new XmlQualifiedName("type"), "custom_type");
            attributeEnclosureLink.AttributeExtensions.Add(new XmlQualifiedName("url"), "http://custom_url.com");
            attributeEnclosureLink.AttributeExtensions.Add(new XmlQualifiedName("enclosure_name4", "xmlns"), "");

            attributeEnclosureLink.Length = 10;

            attributeEnclosureLink.MediaType = "enclosure_mediaType";

            attributeEnclosureLink.RelationshipType = "enclosure";

            attributeEnclosureLink.Uri = new Uri("http://enclosure_uri.com");

            var attributeEnclosureItem = new SyndicationItem();
            attributeEnclosureItem.Links.Add(attributeEnclosureLink);

            foreach (bool serializeExtensionsAsAtom in new bool[] { true, false })
            {
                yield return new object[]
                {
                    attributeEnclosureItem,
                    serializeExtensionsAsAtom,
@"<item>
    <description />
    <enclosure length=""100"" type=""custom_type"" url=""http://custom_url.com"" d2p1:enclosure_name4="""" xmlns:d2p1=""xmlns"" />
</item>"
                };
            }

            var fullFeed = new SyndicationFeed();
            fullFeed.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");
            fullFeed.Authors.Add(new SyndicationPerson("email", "author", "uri"));
            fullFeed.BaseUri = new Uri("http://feed_baseuri.com");
            fullFeed.Categories.Add(new SyndicationCategory("category"));
            fullFeed.Contributors.Add(new SyndicationPerson("email", "contributor", "uri"));
            fullFeed.Copyright = new TextSyndicationContent("copyright");
            fullFeed.Description = new TextSyndicationContent("description");
            fullFeed.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            fullFeed.Generator = "generator";
            fullFeed.Id = "id";
            fullFeed.ImageUrl = new Uri("http://imageurl.com");
            fullFeed.Items = new SyndicationItem[]
            {
                new SyndicationItem("title", "content", null)
            };
            fullFeed.Language = "language";
            fullFeed.LastUpdatedTime = DateTimeOffset.MinValue.AddTicks(10);
            fullFeed.Links.Add(new SyndicationLink(new Uri("http://microsoft.com")));
            fullFeed.Title = new TextSyndicationContent("title");

            var itemWithSourceFeed = new SyndicationItem() { SourceFeed = fullFeed };
            foreach (bool serializeExtensionsAsAtom in new bool[] { true, false })
            {
                yield return new object[]
                {
                    itemWithSourceFeed,
                    serializeExtensionsAsAtom,
@"<item>
    <description />
    <source name=""value"">title</source>
</item>"
                };
            };

            var itemWithEmptyFeed = new SyndicationItem() { SourceFeed = new SyndicationFeed() };
            foreach (bool serializeExtensionsAsAtom in new bool[] { true, false })
            {
                yield return new object[]
                {
                    itemWithEmptyFeed,
                    serializeExtensionsAsAtom,
@"<item>
    <description />
    <source />
</item>"
                };
            };

            var selfLinkFeed = new SyndicationFeed();
            selfLinkFeed.Links.Add(new SyndicationLink() { RelationshipType = "self", Uri = new Uri("http://microsoft.com") });

            var itemWithSelfLinkFeed = new SyndicationItem() { SourceFeed = selfLinkFeed };
            foreach (bool serializeExtensionsAsAtom in new bool[] { true, false })
            {
                yield return new object[]
                {
                    itemWithSelfLinkFeed,
                    serializeExtensionsAsAtom,
@"<item>
    <description />
    <source url=""http://microsoft.com/"" />
</item>"
                };
            };

            var selfAttributeLinkFeed = new SyndicationFeed();
            selfAttributeLinkFeed.AttributeExtensions.Add(new XmlQualifiedName("url"), "url_value");
            selfAttributeLinkFeed.Links.Add(new SyndicationLink() { RelationshipType = "self", Uri = new Uri("http://microsoft.com") });

            var itemWithSelfAttributeLinkFeed = new SyndicationItem() { SourceFeed = selfAttributeLinkFeed };
            foreach (bool serializeExtensionsAsAtom in new bool[] { true, false })
            {
                yield return new object[]
                {
                    itemWithSelfAttributeLinkFeed,
                    serializeExtensionsAsAtom,
@"<item>
    <description />
    <source url=""url_value"" />
</item>"
                };
            };
        }

        [Theory]
        [MemberData(nameof(WriteTo_TestData))]
        public void WriteTo_Invoke_SerializesExpected(SyndicationItem item, bool serializeExtensionsAsAtom, string expected)
        {
            var formatter = new Rss20ItemFormatter(item) { SerializeExtensionsAsAtom = serializeExtensionsAsAtom };
            CompareHelper.AssertEqualWriteOutput(expected, writer => formatter.WriteTo(writer));
            if (serializeExtensionsAsAtom)
            {
                CompareHelper.AssertEqualWriteOutput(expected, writer => item.SaveAsRss20(writer));
            }
            CompareHelper.AssertEqualWriteOutput(expected, writer =>
            {
                writer.WriteStartElement("item");
                ((IXmlSerializable)formatter).WriteXml(writer);
                writer.WriteEndElement();
            });

            var genericFormatter = new Rss20ItemFormatter<SyndicationItem>(item) { SerializeExtensionsAsAtom = serializeExtensionsAsAtom };
            CompareHelper.AssertEqualWriteOutput(expected, writer => formatter.WriteTo(writer));
            if (serializeExtensionsAsAtom)
            {
                CompareHelper.AssertEqualWriteOutput(expected, writer => item.SaveAsRss20(writer));
            }
            CompareHelper.AssertEqualWriteOutput(expected, writer =>
            {
                writer.WriteStartElement("item");
                ((IXmlSerializable)genericFormatter).WriteXml(writer);
                writer.WriteEndElement();
            });
        }

        [Fact]
        public void WriteTo_NullWriter_ThrowsArgumentNullException()
        {
            var formatter = new Rss20ItemFormatter(new SyndicationItem());
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteTo(null));
        }

        [Fact]
        public void WriteTo_NoItem_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Rss20ItemFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteTo(writer));
            }
        }

        [Fact]
        public void WriteXml_NullWriter_ThrowsArgumentNullException()
        {
            IXmlSerializable formatter = new Rss20ItemFormatter();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteXml(null));
        }

        [Fact]
        public void WriteXml_NoItem_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                IXmlSerializable formatter = new Rss20ItemFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteXml(writer));
            }
        }

        public static IEnumerable<object[]> CanRead_TestData()
        {
            yield return new object[] { @"<item xmlns=""different"" />", false };
            yield return new object[] { @"<different />", false };
            yield return new object[] { @"<item />", true };
            yield return new object[] { @"<item></item>", true };
        }

        [Theory]
        [MemberData(nameof(CanRead_TestData))]
        public void CanRead_ValidReader_ReturnsExpected(string xmlString, bool expected)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20ItemFormatter();
                Assert.Equal(expected, formatter.CanRead(reader));
            }
        }

        [Fact]
        public void CanRead_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new Rss20ItemFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.CanRead(null));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void ReadFrom_FullItem_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            string xmlString =
@"<item xml:base=""/relative"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"">
    <guid isPermaLink=""false"">id</guid>
    <link />
    <author xmlns=""http://www.w3.org/2005/Atom"" />
    <author xmlns=""http://www.w3.org/2005/Atom""></author>
    <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d2p1=""author_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <name>author_name</name>
        <uri>author_uri</uri>
        <email>author_email</email>
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </author>
    <author />
    <author></author>
    <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d2p1=""author_namespace"">email</author>
    <category />
    <category></category>
    <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" domain=""category_scheme"" xmlns:d2p1=""category_namespace"">category_name</category>
    <title>title_title</title>
    <description>summary_title</description>
    <pubDate />
    <pubDate></pubDate>
    <pubDate>Mon, 01 Jan 0001 00:00:00 Z</pubDate>
    <link href="""" xmlns=""http://www.w3.org/2005/Atom"" />
    <link xml:base=""http://link_url.com/"" link_name1="""" d2p1:link_name2="""" d2p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d2p1=""link_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </link>
    <enclosure xml:base=""http://link_url.com/"" enclosure_name1="""" d2p1:enclosure_name2="""" d2p1:enclosure_name3=""item_value"" d1p2:enclosure_name4="""" url=""http://enclosure_uri.com/"" type=""enclosure_mediaType"" length=""10"" xmlns:d2p1=""enclosure_namespace"" />
    <link rel=""enclosure"" href="""" xmlns=""http://www.w3.org/2005/Atom"" />
    <link xml:base=""http://alternate_url.com/"" alternate_name1="""" d2p1:alternate_name2="""" d2p1:alternate_name3=""alternate_value"" d1p2:alternate_name4="""" rel=""alternate"" type=""alternate_mediaType"" title=""alternate_title"" length=""10"" href=""http://alternate_uri.com/"" xmlns:d2p1=""alternate_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </link>
    <link href=""link_href"" xmlns=""http://www.w3.org/2005/Atom"" />
    <enclosure />
    <enclosure></enclosure>
    <enclosure length="""" />
    <enclosure xmlns=""http://www.w3.org/2005/Atom""/>
    <link />
    <link></link>
    <link xml:ignored=""true"" alternate_name1="""" d2p1:alternate_name2="""" d2p1:alternate_name3=""alternate_value"" d1p2:alternate_name4="""" xmlns:d2p1=""alternate_namespace"">http://microsoft.com</link>
    <updated xmlns=""http://www.w3.org/2005/Atom"">0001-01-01T00:00:00Z</updated>
    <rights type=""html"" copyright_name1="""" d2p1:copyright_name2="""" d2p1:copyright_name3=""copyright_value"" d1p2:copyright_name4="""" xmlns:d2p1=""copyright_namespace"" xmlns=""http://www.w3.org/2005/Atom"">copyright_title</rights>
    <content type=""html"" content_name1="""" d2p1:content_name2="""" d2p1:content_name3=""content_value"" d1p2:content_name4="""" xmlns:d2p1=""content_namespace"" xmlns=""http://www.w3.org/2005/Atom"">content_title</content>
    <contributor xmlns=""http://www.w3.org/2005/Atom"" />
    <contributor xmlns=""http://www.w3.org/2005/Atom""></contributor>
    <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <name>contributor_name</name>
        <uri>contributor_uri</uri>
        <email>contributor_email</email>
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </contributor>
    <contributor />
    <contributor></contributor>
    <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"">email</contributor>
    <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </Rss20ItemFormatterTests.ExtensionObject>
</item>";
            VerifyRead(xmlString, preserveAttributeExtensions, preserveElementExtensions, item =>
            {
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

                Assert.Equal(6, item.Authors.Count);

                SyndicationPerson firstAuthor = item.Authors[0];
                Assert.Empty(firstAuthor.AttributeExtensions);
                Assert.Empty(firstAuthor.ElementExtensions);
                Assert.Null(firstAuthor.Email);
                Assert.Null(firstAuthor.Name);
                Assert.Null(firstAuthor.Uri);

                SyndicationPerson secondAuthor = item.Authors[1];
                Assert.Empty(secondAuthor.AttributeExtensions);
                Assert.Empty(secondAuthor.ElementExtensions);
                Assert.Null(secondAuthor.Email);
                Assert.Null(secondAuthor.Name);
                Assert.Null(secondAuthor.Uri);

                SyndicationPerson thirdAuthor = item.Authors[2];
                Assert.Equal(4, thirdAuthor.AttributeExtensions.Count);
                Assert.Equal("", thirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name1")]);
                Assert.Equal("", thirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name2", "author_namespace")]);
                Assert.Equal("author_value", thirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name3", "author_namespace")]);
                Assert.Equal("", thirdAuthor.AttributeExtensions[new XmlQualifiedName("author_name4", "xmlns")]);
                Assert.Equal(1, thirdAuthor.ElementExtensions.Count);
                Assert.Equal(10, thirdAuthor.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.Equal("author_email", thirdAuthor.Email);
                Assert.Equal("author_name", thirdAuthor.Name);
                Assert.Equal("author_uri", thirdAuthor.Uri);

                SyndicationPerson fourthAuthor = item.Authors[3];
                Assert.Empty(firstAuthor.AttributeExtensions);
                Assert.Empty(firstAuthor.ElementExtensions);
                Assert.Null(firstAuthor.Email);
                Assert.Null(firstAuthor.Name);
                Assert.Null(firstAuthor.Uri);

                SyndicationPerson fifthAuthor = item.Authors[4];
                Assert.Empty(fifthAuthor.AttributeExtensions);
                Assert.Empty(fifthAuthor.ElementExtensions);
                Assert.Empty(fifthAuthor.Email);
                Assert.Null(fifthAuthor.Name);
                Assert.Null(fifthAuthor.Uri);

                SyndicationPerson sixthAuthor = item.Authors[5];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, sixthAuthor.AttributeExtensions.Count);
                    Assert.Equal("", sixthAuthor.AttributeExtensions[new XmlQualifiedName("author_name1")]);
                    Assert.Equal("", sixthAuthor.AttributeExtensions[new XmlQualifiedName("author_name2", "author_namespace")]);
                    Assert.Equal("author_value", sixthAuthor.AttributeExtensions[new XmlQualifiedName("author_name3", "author_namespace")]);
                    Assert.Equal("", sixthAuthor.AttributeExtensions[new XmlQualifiedName("author_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(sixthAuthor.AttributeExtensions);
                }
                Assert.Empty(sixthAuthor.ElementExtensions);
                Assert.Equal("email", sixthAuthor.Email);
                Assert.Null(sixthAuthor.Name);
                Assert.Null(sixthAuthor.Uri);

                Assert.Equal(new Uri("/relative", UriKind.Relative), item.BaseUri);

                SyndicationCategory firstCategory = item.Categories[0];
                Assert.Empty(firstCategory.AttributeExtensions);
                Assert.Empty(firstCategory.ElementExtensions);
                Assert.Null(firstCategory.Name);
                Assert.Null(firstCategory.Scheme);
                Assert.Null(firstCategory.Label);

                SyndicationCategory secondCategory = item.Categories[1];
                Assert.Empty(secondCategory.AttributeExtensions);
                Assert.Empty(secondCategory.ElementExtensions);
                Assert.Empty(secondCategory.Name);
                Assert.Null(secondCategory.Scheme);
                Assert.Null(secondCategory.Label);

                SyndicationCategory thirdCategory = item.Categories[2];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, thirdCategory.AttributeExtensions.Count);
                    Assert.Equal("", thirdCategory.AttributeExtensions[new XmlQualifiedName("category_name1")]);
                    Assert.Equal("", thirdCategory.AttributeExtensions[new XmlQualifiedName("category_name2", "category_namespace")]);
                    Assert.Equal("category_value", thirdCategory.AttributeExtensions[new XmlQualifiedName("category_name3", "category_namespace")]);
                    Assert.Equal("", thirdCategory.AttributeExtensions[new XmlQualifiedName("category_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(thirdCategory.AttributeExtensions);
                }
                Assert.Empty(thirdCategory.ElementExtensions);
                Assert.Equal("category_name", thirdCategory.Name);
                Assert.Equal("category_scheme", thirdCategory.Scheme);
                Assert.Null(thirdCategory.Label);

                TextSyndicationContent content = Assert.IsType<TextSyndicationContent>(item.Content);
                Assert.Equal(4, content.AttributeExtensions.Count);
                Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name1")]);
                Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name2", "content_namespace")]);
                Assert.Equal("content_value", content.AttributeExtensions[new XmlQualifiedName("content_name3", "content_namespace")]);
                Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name4", "xmlns")]);
                Assert.Equal("content_title", content.Text);
                Assert.Equal("html", content.Type);

                Assert.Equal(3, item.Contributors.Count);

                SyndicationPerson firstContributor = item.Contributors[0];
                Assert.Empty(firstContributor.AttributeExtensions);
                Assert.Empty(firstContributor.ElementExtensions);
                Assert.Null(firstContributor.Email);
                Assert.Null(firstContributor.Name);
                Assert.Null(firstContributor.Uri);

                SyndicationPerson secondContributor = item.Contributors[1];
                Assert.Empty(secondContributor.AttributeExtensions);
                Assert.Empty(secondContributor.ElementExtensions);
                Assert.Null(secondContributor.Email);
                Assert.Null(secondContributor.Name);
                Assert.Null(secondContributor.Uri);

                SyndicationPerson thirdContributor = item.Contributors[2];
                Assert.Equal(4, thirdContributor.AttributeExtensions.Count);
                Assert.Equal("", thirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name1")]);
                Assert.Equal("", thirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name2", "contributor_namespace")]);
                Assert.Equal("contributor_value", thirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name3", "contributor_namespace")]);
                Assert.Equal("", thirdContributor.AttributeExtensions[new XmlQualifiedName("contributor_name4", "xmlns")]);
                Assert.Equal(1, thirdContributor.ElementExtensions.Count);
                Assert.Equal(10, thirdContributor.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.Equal("contributor_email", thirdContributor.Email);
                Assert.Equal("contributor_name", thirdContributor.Name);
                Assert.Equal("contributor_uri", thirdContributor.Uri);

                Assert.Equal(4, item.Copyright.AttributeExtensions.Count);
                Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name1")]);
                Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name2", "copyright_namespace")]);
                Assert.Equal("copyright_value", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name3", "copyright_namespace")]);
                Assert.Equal("", item.Copyright.AttributeExtensions[new XmlQualifiedName("copyright_name4", "xmlns")]);
                Assert.Equal("copyright_title", item.Copyright.Text);
                Assert.Equal("html", item.Copyright.Type);

                if (preserveElementExtensions)
                {
                    Assert.Equal(5, item.ElementExtensions.Count);
                    Assert.Equal(10, item.ElementExtensions[item.ElementExtensions.Count - 1].GetObject<ExtensionObject>().Value);
                }
                else
                {
                    Assert.Empty(item.ElementExtensions);
                }

                Assert.Equal("id", item.Id);

                Assert.Equal(DateTimeOffset.MinValue, item.LastUpdatedTime);

                Assert.Equal(13, item.Links.Count);

                SyndicationLink firstLink = item.Links[0];
                Assert.Empty(firstLink.AttributeExtensions);
                Assert.Empty(firstLink.ElementExtensions);
                Assert.Equal(0, firstLink.Length);
                Assert.Null(firstLink.MediaType);
                Assert.Equal("alternate", firstLink.RelationshipType);
                Assert.Null(firstLink.Title);
                Assert.Empty(firstLink.Uri.OriginalString);

                SyndicationLink secondLink = item.Links[1];
                Assert.Empty(secondLink.AttributeExtensions);
                Assert.Empty(secondLink.ElementExtensions);
                Assert.Equal(new Uri("/relative", UriKind.Relative), secondLink.BaseUri);
                Assert.Equal(0, secondLink.Length);
                Assert.Null(secondLink.MediaType);
                Assert.Null(secondLink.RelationshipType);
                Assert.Null(secondLink.Title);
                Assert.Empty(secondLink.Uri.OriginalString);

                SyndicationLink thirdLink = item.Links[2];
                Assert.Equal(4, thirdLink.AttributeExtensions.Count);
                Assert.Equal("", thirdLink.AttributeExtensions[new XmlQualifiedName("link_name1")]);
                Assert.Equal("", thirdLink.AttributeExtensions[new XmlQualifiedName("link_name2", "link_namespace")]);
                Assert.Equal("link_value", thirdLink.AttributeExtensions[new XmlQualifiedName("link_name3", "link_namespace")]);
                Assert.Equal("", thirdLink.AttributeExtensions[new XmlQualifiedName("link_name4", "xmlns")]);
                Assert.Equal(1, thirdLink.ElementExtensions.Count);
                Assert.Equal(10, thirdLink.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.Equal(new Uri("http://link_url.com/"), thirdLink.BaseUri);
                Assert.Equal(10, thirdLink.Length);
                Assert.Equal("link_mediaType", thirdLink.MediaType);
                Assert.Equal("link_relationshipType", thirdLink.RelationshipType);
                Assert.Equal("link_title", thirdLink.Title);
                Assert.Equal(new Uri("http://link_uri.com/"), thirdLink.Uri);

                SyndicationLink fourthLink = item.Links[3];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, fourthLink.AttributeExtensions.Count);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("enclosure_name1")]);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("enclosure_name2", "enclosure_namespace")]);
                    Assert.Equal("item_value", fourthLink.AttributeExtensions[new XmlQualifiedName("enclosure_name3", "enclosure_namespace")]);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("enclosure_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(fourthLink.AttributeExtensions);
                }
                Assert.Empty(fourthLink.ElementExtensions);
                Assert.Equal(new Uri("http://link_url.com/"), fourthLink.BaseUri);
                Assert.Equal(10, fourthLink.Length);
                Assert.Equal("enclosure_mediaType", fourthLink.MediaType);
                Assert.Equal("enclosure", fourthLink.RelationshipType);
                Assert.Null(fourthLink.Title);
                Assert.Equal(new Uri("http://enclosure_uri.com/"), fourthLink.Uri);

                SyndicationLink fifthLink = item.Links[4];
                Assert.Empty(fifthLink.AttributeExtensions);
                Assert.Empty(fifthLink.ElementExtensions);
                Assert.Equal(new Uri("/relative", UriKind.Relative), fifthLink.BaseUri);
                Assert.Equal(0, fifthLink.Length);
                Assert.Null(fifthLink.MediaType);
                Assert.Equal("enclosure", fifthLink.RelationshipType);
                Assert.Null(fifthLink.Title);
                Assert.Empty(fifthLink.Uri.OriginalString);

                SyndicationLink sixthLink = item.Links[5];
                Assert.Equal(4, sixthLink.AttributeExtensions.Count);
                Assert.Equal("", sixthLink.AttributeExtensions[new XmlQualifiedName("alternate_name1")]);
                Assert.Equal("", sixthLink.AttributeExtensions[new XmlQualifiedName("alternate_name2", "alternate_namespace")]);
                Assert.Equal("alternate_value", sixthLink.AttributeExtensions[new XmlQualifiedName("alternate_name3", "alternate_namespace")]);
                Assert.Equal("", sixthLink.AttributeExtensions[new XmlQualifiedName("alternate_name4", "xmlns")]);
                Assert.Equal(1, sixthLink.ElementExtensions.Count);
                Assert.Equal(10, sixthLink.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.Equal(new Uri("http://alternate_url.com/"), sixthLink.BaseUri);
                Assert.Equal(10, sixthLink.Length);
                Assert.Equal("alternate_mediaType", sixthLink.MediaType);
                Assert.Equal("alternate", sixthLink.RelationshipType);
                Assert.Equal("alternate_title", sixthLink.Title);
                Assert.Equal(new Uri("http://alternate_uri.com/"), sixthLink.Uri);

                Assert.Equal(DateTimeOffset.MinValue, item.PublishDate);

                SyndicationLink seventhLink = item.Links[6];
                Assert.Empty(seventhLink.AttributeExtensions);
                Assert.Empty(seventhLink.ElementExtensions);
                Assert.Equal(new Uri("/relative", UriKind.Relative), seventhLink.BaseUri);
                Assert.Equal(0, seventhLink.Length);
                Assert.Null(seventhLink.MediaType);
                Assert.Null(seventhLink.RelationshipType);
                Assert.Null(seventhLink.Title);
                Assert.Equal(new Uri("link_href", UriKind.Relative), seventhLink.Uri);

                SyndicationLink eighthLink = item.Links[7];
                Assert.Empty(eighthLink.AttributeExtensions);
                Assert.Empty(eighthLink.ElementExtensions);
                Assert.Equal(0, eighthLink.Length);
                Assert.Null(eighthLink.MediaType);
                Assert.Equal("enclosure", eighthLink.RelationshipType);
                Assert.Null(eighthLink.Title);
                Assert.Null(eighthLink.Uri);

                SyndicationLink ninthLink = item.Links[8];
                Assert.Empty(ninthLink.AttributeExtensions);
                Assert.Empty(ninthLink.ElementExtensions);
                Assert.Equal(0, ninthLink.Length);
                Assert.Null(ninthLink.MediaType);
                Assert.Equal("enclosure", ninthLink.RelationshipType);
                Assert.Null(ninthLink.Title);
                Assert.Null(ninthLink.Uri);

                SyndicationLink tenthLink = item.Links[9];
                Assert.Empty(tenthLink.AttributeExtensions);
                Assert.Empty(tenthLink.ElementExtensions);
                Assert.Equal(0, tenthLink.Length);
                Assert.Null(tenthLink.MediaType);
                Assert.Equal("enclosure", tenthLink.RelationshipType);
                Assert.Null(tenthLink.Title);
                Assert.Null(tenthLink.Uri);

                SyndicationLink eleventhLink = item.Links[10];
                Assert.Empty(eleventhLink.AttributeExtensions);
                Assert.Empty(eleventhLink.ElementExtensions);
                Assert.Equal(0, eleventhLink.Length);
                Assert.Null(eleventhLink.MediaType);
                Assert.Equal("alternate", eleventhLink.RelationshipType);
                Assert.Null(eleventhLink.Title);
                Assert.Empty(eleventhLink.Uri.OriginalString);

                SyndicationLink twelfthLink = item.Links[11];
                Assert.Empty(twelfthLink.AttributeExtensions);
                Assert.Empty(twelfthLink.ElementExtensions);
                Assert.Equal(0, twelfthLink.Length);
                Assert.Null(twelfthLink.MediaType);
                Assert.Equal("alternate", twelfthLink.RelationshipType);
                Assert.Null(twelfthLink.Title);
                Assert.Empty(twelfthLink.Uri.OriginalString);

                SyndicationLink thirteenthLink = item.Links[12];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(5, thirteenthLink.AttributeExtensions.Count);
                    Assert.Equal("true", thirteenthLink.AttributeExtensions[new XmlQualifiedName("ignored", "http://www.w3.org/XML/1998/namespace")]);
                    Assert.Equal("", thirteenthLink.AttributeExtensions[new XmlQualifiedName("alternate_name1")]);
                    Assert.Equal("", thirteenthLink.AttributeExtensions[new XmlQualifiedName("alternate_name2", "alternate_namespace")]);
                    Assert.Equal("alternate_value", thirteenthLink.AttributeExtensions[new XmlQualifiedName("alternate_name3", "alternate_namespace")]);
                    Assert.Equal("", thirteenthLink.AttributeExtensions[new XmlQualifiedName("alternate_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(thirteenthLink.AttributeExtensions);
                }
                Assert.Empty(thirteenthLink.ElementExtensions);
                Assert.Equal(0, thirteenthLink.Length);
                Assert.Null(thirteenthLink.MediaType);
                Assert.Equal("alternate", thirteenthLink.RelationshipType);
                Assert.Null(thirteenthLink.Title);
                Assert.Equal(new Uri("http://microsoft.com"), thirteenthLink.Uri);

                Assert.Null(item.SourceFeed);

                Assert.Empty(item.Summary.AttributeExtensions);
                Assert.Equal("summary_title", item.Summary.Text);
                Assert.Equal("text", item.Summary.Type);

                Assert.Empty(item.Title.AttributeExtensions);
                Assert.Equal("title_title", item.Title.Text);
                Assert.Equal("text", item.Title.Type);
            });
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void ReadFrom_TryParseReturnsTrue_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            using (var stringReader = new StringReader(
@"<item xml:base=""/relative"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"">
    <guid isPermaLink=""false"">id</guid>
    <link />
    <author xmlns=""http://www.w3.org/2005/Atom"" />
    <author xmlns=""http://www.w3.org/2005/Atom""></author>
    <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d2p1=""author_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <name>author_name</name>
        <uri>author_uri</uri>
        <email>author_email</email>
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </author>
    <author />
    <author></author>
    <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d2p1=""author_namespace"">email</author>
    <category />
    <category></category>
    <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" domain=""category_scheme"" xmlns:d2p1=""category_namespace"">category_name</category>
    <title>title_title</title>
    <description>summary_title</description>
    <pubDate />
    <pubDate></pubDate>
    <pubDate>Mon, 01 Jan 0001 00:00:00 Z</pubDate>
    <link href="""" xmlns=""http://www.w3.org/2005/Atom"" />
    <link xml:base=""http://link_url.com/"" link_name1="""" d2p1:link_name2="""" d2p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d2p1=""link_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </link>
    <enclosure xml:base=""http://link_url.com/"" enclosure_name1="""" d2p1:enclosure_name2="""" d2p1:enclosure_name3=""item_value"" d1p2:enclosure_name4="""" url=""http://enclosure_uri.com/"" type=""enclosure_mediaType"" length=""10"" xmlns:d2p1=""enclosure_namespace"" />
    <link rel=""enclosure"" href="""" xmlns=""http://www.w3.org/2005/Atom"" />
    <link xml:base=""http://alternate_url.com/"" alternate_name1="""" d2p1:alternate_name2="""" d2p1:alternate_name3=""alternate_value"" d1p2:alternate_name4="""" rel=""alternate"" type=""alternate_mediaType"" title=""alternate_title"" length=""10"" href=""http://alternate_uri.com/"" xmlns:d2p1=""alternate_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </link>
    <link href=""link_href"" xmlns=""http://www.w3.org/2005/Atom"" />
    <enclosure />
    <enclosure></enclosure>
    <enclosure length="""" />
    <enclosure xmlns=""http://www.w3.org/2005/Atom""/>
    <link />
    <link></link>
    <link xml:ignored=""true"" alternate_name1="""" d2p1:alternate_name2="""" d2p1:alternate_name3=""alternate_value"" d1p2:alternate_name4="""" xmlns:d2p1=""alternate_namespace"">http://microsoft.com</link>
    <updated xmlns=""http://www.w3.org/2005/Atom"">0001-01-01T00:00:00Z</updated>
    <rights type=""html"" copyright_name1="""" d2p1:copyright_name2="""" d2p1:copyright_name3=""copyright_value"" d1p2:copyright_name4="""" xmlns:d2p1=""copyright_namespace"" xmlns=""http://www.w3.org/2005/Atom"">copyright_title</rights>
    <content type=""html"" content_name1="""" d2p1:content_name2="""" d2p1:content_name3=""content_value"" d1p2:content_name4="""" xmlns:d2p1=""content_namespace"" xmlns=""http://www.w3.org/2005/Atom"">content_title</content>
    <contributor xmlns=""http://www.w3.org/2005/Atom"" />
    <contributor xmlns=""http://www.w3.org/2005/Atom""></contributor>
    <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
        <name>contributor_name</name>
        <uri>contributor_uri</uri>
        <email>contributor_email</email>
        <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Rss20ItemFormatterTests.ExtensionObject>
    </contributor>
    <contributor />
    <contributor></contributor>
    <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"">email</contributor>
    <Rss20ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </Rss20ItemFormatterTests.ExtensionObject>
</item>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20ItemFormatter<SyndicationItemTryParseTrueSubclass>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);

                SyndicationItem item = formatter.Item;

                Assert.Empty(item.AttributeExtensions);

                Assert.Equal(6, item.Authors.Count);

                SyndicationPerson firstAuthor = item.Authors[0];
                Assert.Empty(firstAuthor.AttributeExtensions);
                Assert.Empty(firstAuthor.ElementExtensions);
                Assert.Null(firstAuthor.Email);
                Assert.Null(firstAuthor.Name);
                Assert.Null(firstAuthor.Uri);

                SyndicationPerson secondAuthor = item.Authors[1];
                Assert.Empty(secondAuthor.AttributeExtensions);
                Assert.Empty(secondAuthor.ElementExtensions);
                Assert.Null(secondAuthor.Email);
                Assert.Null(secondAuthor.Name);
                Assert.Null(secondAuthor.Uri);

                SyndicationPerson thirdAuthor = item.Authors[2];
                Assert.Empty(thirdAuthor.AttributeExtensions);
                Assert.Empty(thirdAuthor.ElementExtensions);
                Assert.Equal("author_email", thirdAuthor.Email);
                Assert.Equal("author_name", thirdAuthor.Name);
                Assert.Equal("author_uri", thirdAuthor.Uri);

                SyndicationPerson fourthAuthor = item.Authors[3];
                Assert.Empty(firstAuthor.AttributeExtensions);
                Assert.Empty(firstAuthor.ElementExtensions);
                Assert.Null(firstAuthor.Email);
                Assert.Null(firstAuthor.Name);
                Assert.Null(firstAuthor.Uri);

                SyndicationPerson fifthAuthor = item.Authors[4];
                Assert.Empty(fifthAuthor.AttributeExtensions);
                Assert.Empty(fifthAuthor.ElementExtensions);
                Assert.Empty(fifthAuthor.Email);
                Assert.Null(fifthAuthor.Name);
                Assert.Null(fifthAuthor.Uri);

                SyndicationPerson sixthAuthor = item.Authors[5];
                Assert.Empty(sixthAuthor.AttributeExtensions);
                Assert.Empty(sixthAuthor.ElementExtensions);
                Assert.Equal("email", sixthAuthor.Email);
                Assert.Null(sixthAuthor.Name);
                Assert.Null(sixthAuthor.Uri);

                Assert.Equal(new Uri("/relative", UriKind.Relative), item.BaseUri);

                SyndicationCategory firstCategory = item.Categories[0];
                Assert.Empty(firstCategory.AttributeExtensions);
                Assert.Empty(firstCategory.ElementExtensions);
                Assert.Null(firstCategory.Name);
                Assert.Null(firstCategory.Scheme);
                Assert.Null(firstCategory.Label);

                SyndicationCategory secondCategory = item.Categories[1];
                Assert.Empty(secondCategory.AttributeExtensions);
                Assert.Empty(secondCategory.ElementExtensions);
                Assert.Empty(secondCategory.Name);
                Assert.Null(secondCategory.Scheme);
                Assert.Null(secondCategory.Label);

                SyndicationCategory thirdCategory = item.Categories[2];
                Assert.Empty(thirdCategory.AttributeExtensions);
                Assert.Empty(thirdCategory.ElementExtensions);
                Assert.Equal("category_name", thirdCategory.Name);
                Assert.Equal("category_scheme", thirdCategory.Scheme);
                Assert.Null(thirdCategory.Label);

                TextSyndicationContent content = Assert.IsType<TextSyndicationContent>(item.Content);
                Assert.Equal(4, content.AttributeExtensions.Count);
                Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name1")]);
                Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name2", "content_namespace")]);
                Assert.Equal("content_value", content.AttributeExtensions[new XmlQualifiedName("content_name3", "content_namespace")]);
                Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("content_name4", "xmlns")]);
                Assert.Equal("content_title", content.Text);
                Assert.Equal("html", content.Type);

                Assert.Equal(3, item.Contributors.Count);

                SyndicationPerson firstContributor = item.Contributors[0];
                Assert.Empty(firstContributor.AttributeExtensions);
                Assert.Empty(firstContributor.ElementExtensions);
                Assert.Null(firstContributor.Email);
                Assert.Null(firstContributor.Name);
                Assert.Null(firstContributor.Uri);

                SyndicationPerson secondContributor = item.Contributors[1];
                Assert.Empty(secondContributor.AttributeExtensions);
                Assert.Empty(secondContributor.ElementExtensions);
                Assert.Null(secondContributor.Email);
                Assert.Null(secondContributor.Name);
                Assert.Null(secondContributor.Uri);

                SyndicationPerson thirdContributor = item.Contributors[2];
                Assert.Empty(thirdContributor.AttributeExtensions);
                Assert.Empty(thirdContributor.ElementExtensions);
                Assert.Equal("contributor_email", thirdContributor.Email);
                Assert.Equal("contributor_name", thirdContributor.Name);
                Assert.Equal("contributor_uri", thirdContributor.Uri);
                
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

                Assert.Equal(13, item.Links.Count);

                SyndicationLink firstLink = item.Links[0];
                Assert.Empty(firstLink.AttributeExtensions);
                Assert.Empty(firstLink.ElementExtensions);
                Assert.Equal(0, firstLink.Length);
                Assert.Null(firstLink.MediaType);
                Assert.Equal("alternate", firstLink.RelationshipType);
                Assert.Null(firstLink.Title);
                Assert.Empty(firstLink.Uri.OriginalString);

                SyndicationLink secondLink = item.Links[1];
                Assert.Empty(secondLink.AttributeExtensions);
                Assert.Empty(secondLink.ElementExtensions);
                Assert.Equal(new Uri("/relative", UriKind.Relative), secondLink.BaseUri);
                Assert.Equal(0, secondLink.Length);
                Assert.Null(secondLink.MediaType);
                Assert.Null(secondLink.RelationshipType);
                Assert.Null(secondLink.Title);
                Assert.Empty(secondLink.Uri.OriginalString);

                SyndicationLink thirdLink = item.Links[2];
                Assert.Equal(4, thirdLink.AttributeExtensions.Count);
                Assert.Equal("", thirdLink.AttributeExtensions[new XmlQualifiedName("link_name1")]);
                Assert.Equal("", thirdLink.AttributeExtensions[new XmlQualifiedName("link_name2", "link_namespace")]);
                Assert.Equal("link_value", thirdLink.AttributeExtensions[new XmlQualifiedName("link_name3", "link_namespace")]);
                Assert.Equal("", thirdLink.AttributeExtensions[new XmlQualifiedName("link_name4", "xmlns")]);
                Assert.Empty(thirdLink.ElementExtensions);
                Assert.Equal(new Uri("http://link_url.com/"), thirdLink.BaseUri);
                Assert.Equal(10, thirdLink.Length);
                Assert.Equal("link_mediaType", thirdLink.MediaType);
                Assert.Equal("link_relationshipType", thirdLink.RelationshipType);
                Assert.Equal("link_title", thirdLink.Title);
                Assert.Equal(new Uri("http://link_uri.com/"), thirdLink.Uri);

                SyndicationLink fourthLink = item.Links[3];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, fourthLink.AttributeExtensions.Count);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("enclosure_name1")]);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("enclosure_name2", "enclosure_namespace")]);
                    Assert.Equal("item_value", fourthLink.AttributeExtensions[new XmlQualifiedName("enclosure_name3", "enclosure_namespace")]);
                    Assert.Equal("", fourthLink.AttributeExtensions[new XmlQualifiedName("enclosure_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(fourthLink.AttributeExtensions);
                }
                Assert.Empty(fourthLink.ElementExtensions);
                Assert.Equal(new Uri("http://link_url.com/"), fourthLink.BaseUri);
                Assert.Equal(10, fourthLink.Length);
                Assert.Equal("enclosure_mediaType", fourthLink.MediaType);
                Assert.Equal("enclosure", fourthLink.RelationshipType);
                Assert.Null(fourthLink.Title);
                Assert.Equal(new Uri("http://enclosure_uri.com/"), fourthLink.Uri);

                SyndicationLink fifthLink = item.Links[4];
                Assert.Empty(fifthLink.AttributeExtensions);
                Assert.Empty(fifthLink.ElementExtensions);
                Assert.Equal(new Uri("/relative", UriKind.Relative), fifthLink.BaseUri);
                Assert.Equal(0, fifthLink.Length);
                Assert.Null(fifthLink.MediaType);
                Assert.Equal("enclosure", fifthLink.RelationshipType);
                Assert.Null(fifthLink.Title);
                Assert.Empty(fifthLink.Uri.OriginalString);

                SyndicationLink sixthLink = item.Links[5];
                Assert.Equal(4, sixthLink.AttributeExtensions.Count);
                Assert.Equal("", sixthLink.AttributeExtensions[new XmlQualifiedName("alternate_name1")]);
                Assert.Equal("", sixthLink.AttributeExtensions[new XmlQualifiedName("alternate_name2", "alternate_namespace")]);
                Assert.Equal("alternate_value", sixthLink.AttributeExtensions[new XmlQualifiedName("alternate_name3", "alternate_namespace")]);
                Assert.Equal("", sixthLink.AttributeExtensions[new XmlQualifiedName("alternate_name4", "xmlns")]);
                Assert.Empty(sixthLink.ElementExtensions);
                Assert.Equal(new Uri("http://alternate_url.com/"), sixthLink.BaseUri);
                Assert.Equal(10, sixthLink.Length);
                Assert.Equal("alternate_mediaType", sixthLink.MediaType);
                Assert.Equal("alternate", sixthLink.RelationshipType);
                Assert.Equal("alternate_title", sixthLink.Title);
                Assert.Equal(new Uri("http://alternate_uri.com/"), sixthLink.Uri);

                Assert.Equal(DateTimeOffset.MinValue, item.PublishDate);

                SyndicationLink seventhLink = item.Links[6];
                Assert.Empty(seventhLink.AttributeExtensions);
                Assert.Empty(seventhLink.ElementExtensions);
                Assert.Equal(new Uri("/relative", UriKind.Relative), seventhLink.BaseUri);
                Assert.Equal(0, seventhLink.Length);
                Assert.Null(seventhLink.MediaType);
                Assert.Null(seventhLink.RelationshipType);
                Assert.Null(seventhLink.Title);
                Assert.Equal(new Uri("link_href", UriKind.Relative), seventhLink.Uri);

                SyndicationLink eighthLink = item.Links[7];
                Assert.Empty(eighthLink.AttributeExtensions);
                Assert.Empty(eighthLink.ElementExtensions);
                Assert.Equal(0, eighthLink.Length);
                Assert.Null(eighthLink.MediaType);
                Assert.Equal("enclosure", eighthLink.RelationshipType);
                Assert.Null(eighthLink.Title);
                Assert.Null(eighthLink.Uri);

                SyndicationLink ninthLink = item.Links[8];
                Assert.Empty(ninthLink.AttributeExtensions);
                Assert.Empty(ninthLink.ElementExtensions);
                Assert.Equal(0, ninthLink.Length);
                Assert.Null(ninthLink.MediaType);
                Assert.Equal("enclosure", ninthLink.RelationshipType);
                Assert.Null(ninthLink.Title);
                Assert.Null(ninthLink.Uri);

                SyndicationLink tenthLink = item.Links[9];
                Assert.Empty(tenthLink.AttributeExtensions);
                Assert.Empty(tenthLink.ElementExtensions);
                Assert.Equal(0, tenthLink.Length);
                Assert.Null(tenthLink.MediaType);
                Assert.Equal("enclosure", tenthLink.RelationshipType);
                Assert.Null(tenthLink.Title);
                Assert.Null(tenthLink.Uri);

                SyndicationLink eleventhLink = item.Links[10];
                Assert.Empty(eleventhLink.AttributeExtensions);
                Assert.Empty(eleventhLink.ElementExtensions);
                Assert.Equal(0, eleventhLink.Length);
                Assert.Null(eleventhLink.MediaType);
                Assert.Equal("alternate", eleventhLink.RelationshipType);
                Assert.Null(eleventhLink.Title);
                Assert.Empty(eleventhLink.Uri.OriginalString);

                SyndicationLink twelfthLink = item.Links[11];
                Assert.Empty(twelfthLink.AttributeExtensions);
                Assert.Empty(twelfthLink.ElementExtensions);
                Assert.Equal(0, twelfthLink.Length);
                Assert.Null(twelfthLink.MediaType);
                Assert.Equal("alternate", twelfthLink.RelationshipType);
                Assert.Null(twelfthLink.Title);
                Assert.Empty(twelfthLink.Uri.OriginalString);

                SyndicationLink thirteenthLink = item.Links[12];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(5, thirteenthLink.AttributeExtensions.Count);
                    Assert.Equal("true", thirteenthLink.AttributeExtensions[new XmlQualifiedName("ignored", "http://www.w3.org/XML/1998/namespace")]);
                    Assert.Equal("", thirteenthLink.AttributeExtensions[new XmlQualifiedName("alternate_name1")]);
                    Assert.Equal("", thirteenthLink.AttributeExtensions[new XmlQualifiedName("alternate_name2", "alternate_namespace")]);
                    Assert.Equal("alternate_value", thirteenthLink.AttributeExtensions[new XmlQualifiedName("alternate_name3", "alternate_namespace")]);
                    Assert.Equal("", thirteenthLink.AttributeExtensions[new XmlQualifiedName("alternate_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(thirteenthLink.AttributeExtensions);
                }
                Assert.Empty(thirteenthLink.ElementExtensions);
                Assert.Equal(0, thirteenthLink.Length);
                Assert.Null(thirteenthLink.MediaType);
                Assert.Equal("alternate", thirteenthLink.RelationshipType);
                Assert.Null(thirteenthLink.Title);
                Assert.Equal(new Uri("http://microsoft.com"), thirteenthLink.Uri);

                Assert.Null(item.SourceFeed);

                Assert.Empty(item.Summary.AttributeExtensions);
                Assert.Equal("summary_title", item.Summary.Text);
                Assert.Equal("text", item.Summary.Type);

                Assert.Empty(item.Title.AttributeExtensions);
                Assert.Equal("title_title", item.Title.Text);
                Assert.Equal("text", item.Title.Type);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void ReadFrom_EmptySource_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            VerifyRead(@"<item><source></source></item>", preserveElementExtensions, preserveElementExtensions, item =>
            {
                Assert.Empty(item.AttributeExtensions);
                Assert.Empty(item.Authors);
                Assert.Null(item.BaseUri);
                Assert.Empty(item.Categories);
                Assert.Null(item.Content);
                Assert.Null(item.Copyright);
                Assert.Empty(item.ElementExtensions);
                Assert.Null(item.Id);
                Assert.Equal(default, item.LastUpdatedTime);
                Assert.Empty(item.Links);
                Assert.Equal(default, item.PublishDate);
                Assert.NotNull(item.SourceFeed);
                Assert.Null(item.Summary);
                Assert.Null(item.Title);
            });
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void ReadFrom_EmptyItem_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            VerifyRead(@"<item></item>", preserveElementExtensions, preserveElementExtensions, item =>
            {
                Assert.Empty(item.AttributeExtensions);
                Assert.Empty(item.Authors);
                Assert.Null(item.BaseUri);
                Assert.Empty(item.Categories);
                Assert.Null(item.Content);
                Assert.Null(item.Copyright);
                Assert.Empty(item.ElementExtensions);
                Assert.Null(item.Id);
                Assert.Equal(default, item.LastUpdatedTime);
                Assert.Empty(item.Links);
                Assert.Equal(default, item.PublishDate);
                Assert.Null(item.SourceFeed);
                Assert.Null(item.Summary);
                Assert.Null(item.Title);
            });
        }

        private static void VerifyRead(string xmlString, bool preserveAttributeExtensions, bool preserveElementExtensions, Action<SyndicationItem> verifyAction)
        {
            // ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20ItemFormatter()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);
                verifyAction(formatter.Item);
            }

            // ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Rss20ItemFormatter()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Item);
            }

            // Derived ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20ItemFormatter(typeof(SyndicationItemSubclass))
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);
                verifyAction(formatter.Item);
            }

            // Derived ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Rss20ItemFormatter(typeof(SyndicationItemSubclass))
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Item);
            }

            // Generic ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20ItemFormatter<SyndicationItem>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);
                verifyAction(formatter.Item);
            }

            // Generic ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Rss20ItemFormatter<SyndicationItem>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Item);
            }

            // Generic Derived ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20ItemFormatter<SyndicationItemSubclass>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);
                verifyAction(formatter.Item);
            }

            // Generic Derived ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Rss20ItemFormatter<SyndicationItemSubclass>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Item);
            }

            if (preserveAttributeExtensions && preserveElementExtensions)
            {
                // Load.
                using (var stringReader = new StringReader(xmlString))
                using (XmlReader reader = XmlReader.Create(stringReader))
                {
                    SyndicationItem item = SyndicationItem.Load(reader);
                    verifyAction(item);
                }

                // Generic Load.
                using (var stringReader = new StringReader(xmlString))
                using (XmlReader reader = XmlReader.Create(stringReader))
                {
                    SyndicationItemSubclass item = SyndicationItem.Load<SyndicationItemSubclass>(reader);
                    verifyAction(item);
                }
            }
        }

        [Fact]
        public void ReadFrom_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new Rss20ItemFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadFrom(null));
        }

        [Fact]
        public void ReadFrom_NullCreatedItem_ThrowsArgumentNullException()
        {
            using (var stringReader = new StringReader(@"<item></item>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new NullCreatedDocumentFormatter();
                AssertExtensions.Throws<ArgumentNullException>("item", () => formatter.ReadFrom(reader));
            }
        }

        [Theory]
        [InlineData(@"<different></different>")]
        [InlineData(@"<item xmlns=""different""></item>")]
        public void ReadFrom_CantRead_ThrowsXmlException(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20ItemFormatter();
                Assert.Throws<XmlException>(() => formatter.ReadFrom(reader));
            }
        }

        [Theory]
        [InlineData("<item />")]
        [InlineData("<item></item>")]
        [InlineData(@"<app:item xmlns:app=""http://www.w3.org/2005/Atom""></app:item>")]
        [InlineData(@"<item xmlns=""different""></item>")]
        public void ReadXml_ValidReader_Success(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Rss20ItemFormatter();
                ((IXmlSerializable)formatter).ReadXml(reader);

                SyndicationItem item = formatter.Item;
                Assert.Empty(item.AttributeExtensions);
                Assert.Empty(item.Authors);
                Assert.Null(item.BaseUri);
                Assert.Empty(item.Categories);
                Assert.Null(item.Content);
                Assert.Null(item.Copyright);
                Assert.Empty(item.ElementExtensions);
                Assert.Null(item.Id);
                Assert.Equal(default, item.LastUpdatedTime);
                Assert.Empty(item.Links);
                Assert.Equal(default, item.PublishDate);
                Assert.Null(item.SourceFeed);
                Assert.Null(item.Summary);
                Assert.Null(item.Title);
            }
        }

        [Fact]
        public void ReadXml_NullReader_ThrowsArgumentNullException()
        {
            IXmlSerializable formatter = new Rss20ItemFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadXml(null));
        }

        [Fact]
        public void ReadXml_NullCreatedItem_ThrowsArgumentNullException()
        {
            using (var stringReader = new StringReader(@"<item></item>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                IXmlSerializable formatter = new NullCreatedDocumentFormatter();
                AssertExtensions.Throws<ArgumentNullException>("item", () => formatter.ReadXml(reader));
            }
        }

        [Fact]
        public void ReadXml_ThrowsArgumentException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new ArgumentException());
            IXmlSerializable formatter = new Rss20ItemFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Fact]
        public void ReadXml_ThrowsFormatException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new FormatException());
            IXmlSerializable formatter = new Rss20ItemFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Custom date parsing added in .NET Core changes this behaviour")]
        public void Read_InvalidPublishDate_GetThrowsXmlExcepton()
        {
            using (var stringReader = new StringReader(@"<item><pubDate>invalid</pubDate></item>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Rss20ItemFormatter();
                formatter.ReadFrom(reader);
                Assert.Throws<XmlException>(() => formatter.Item.PublishDate);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PreserveAttributeExtensions_Set_GetReturnsExpected(bool preserveAttributeExtensions)
        {
            var formatter = new Rss20ItemFormatter() { PreserveAttributeExtensions = preserveAttributeExtensions };
            Assert.Equal(preserveAttributeExtensions, formatter.PreserveAttributeExtensions);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PreserveElementExtensions_Set_GetReturnsExpected(bool preserveElementExtensions)
        {
            var formatter = new Rss20ItemFormatter() { PreserveElementExtensions = preserveElementExtensions };
            Assert.Equal(preserveElementExtensions, formatter.PreserveElementExtensions);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SerializeExtensionsAsAtom_Set_GetReturnsExpected(bool serializeExtensionsAsAtom)
        {
            var formatter = new Rss20ItemFormatter() { SerializeExtensionsAsAtom = serializeExtensionsAsAtom };
            Assert.Equal(serializeExtensionsAsAtom, formatter.SerializeExtensionsAsAtom);
        }

        [Fact]
        public void CreateItemInstance_NonGeneric_Success()
        {
            var formatter = new Formatter();
            SyndicationItem item = Assert.IsType<SyndicationItem>(formatter.CreateItemInstanceEntryPoint());
            Assert.Empty(item.AttributeExtensions);
            Assert.Empty(item.Authors);
            Assert.Null(item.BaseUri);
            Assert.Empty(item.Categories);
            Assert.Null(item.Content);
            Assert.Null(item.Copyright);
            Assert.Empty(item.ElementExtensions);
            Assert.Null(item.Id);
            Assert.Equal(default, item.LastUpdatedTime);
            Assert.Empty(item.Links);
            Assert.Equal(default, item.PublishDate);
            Assert.Null(item.SourceFeed);
            Assert.Null(item.Summary);
            Assert.Null(item.Title);

            var typedFormatter = new Formatter(typeof(SyndicationItemSubclass));
            item = Assert.IsType<SyndicationItemSubclass>(typedFormatter.CreateItemInstanceEntryPoint());
            Assert.Empty(item.AttributeExtensions);
            Assert.Empty(item.Authors);
            Assert.Null(item.BaseUri);
            Assert.Empty(item.Categories);
            Assert.Null(item.Content);
            Assert.Null(item.Copyright);
            Assert.Empty(item.ElementExtensions);
            Assert.Null(item.Id);
            Assert.Equal(default, item.LastUpdatedTime);
            Assert.Empty(item.Links);
            Assert.Equal(default, item.PublishDate);
            Assert.Null(item.SourceFeed);
            Assert.Null(item.Summary);
            Assert.Null(item.Title);
        }

        [Fact]
        public void CreateItemInstance_Generic_Success()
        {
            var formatter = new GenericFormatter<SyndicationItem>();
            SyndicationItem item = Assert.IsType<SyndicationItem>(formatter.CreateItemInstanceEntryPoint());
            Assert.Empty(item.AttributeExtensions);
            Assert.Empty(item.Authors);
            Assert.Null(item.BaseUri);
            Assert.Empty(item.Categories);
            Assert.Null(item.Content);
            Assert.Null(item.Copyright);
            Assert.Empty(item.ElementExtensions);
            Assert.Null(item.Id);
            Assert.Equal(default, item.LastUpdatedTime);
            Assert.Empty(item.Links);
            Assert.Equal(default, item.PublishDate);
            Assert.Null(item.SourceFeed);
            Assert.Null(item.Summary);
            Assert.Null(item.Title);

            var typedFormatter = new GenericFormatter<SyndicationItemSubclass>();
            item = Assert.IsType<SyndicationItemSubclass>(typedFormatter.CreateItemInstanceEntryPoint());
            Assert.Empty(item.AttributeExtensions);
            Assert.Empty(item.Authors);
            Assert.Null(item.BaseUri);
            Assert.Empty(item.Categories);
            Assert.Null(item.Content);
            Assert.Null(item.Copyright);
            Assert.Empty(item.ElementExtensions);
            Assert.Null(item.Id);
            Assert.Equal(default, item.LastUpdatedTime);
            Assert.Empty(item.Links);
            Assert.Equal(default, item.PublishDate);
            Assert.Null(item.SourceFeed);
            Assert.Null(item.Summary);
            Assert.Null(item.Title);
        }

        public class SyndicationItemSubclass : SyndicationItem { }

        public class SyndicationItemTryParseTrueSubclass : SyndicationItem
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

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

        public class NullCreatedDocumentFormatter : Rss20ItemFormatter
        {
            protected override SyndicationItem CreateItemInstance() => null;
        }

        public class Formatter : Rss20ItemFormatter
        {
            public Formatter() : base() { }

            public Formatter(SyndicationItem itemToWrite) : base(itemToWrite) { }

            public Formatter(SyndicationItem itemToWrite, bool serializeExtensionsAsAtom) : base(itemToWrite, serializeExtensionsAsAtom) { }

            public Formatter(Type itemTypeToCreate) : base(itemTypeToCreate) { }

            public Type ItemTypeEntryPoint => ItemType;

            public SyndicationItem CreateItemInstanceEntryPoint() => CreateItemInstance();
        }

        public class GenericFormatter<T> : Rss20ItemFormatter<T> where T : SyndicationItem, new()
        {
            public GenericFormatter() : base() { }

            public GenericFormatter(T itemToWrite) : base(itemToWrite) { }

            public GenericFormatter(T itemToWrite, bool serializeExtensionsAsAtom) : base(itemToWrite, serializeExtensionsAsAtom) { }

            public Type ItemTypeEntryPoint => ItemType;

            public SyndicationItem CreateItemInstanceEntryPoint() => CreateItemInstance();
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}
