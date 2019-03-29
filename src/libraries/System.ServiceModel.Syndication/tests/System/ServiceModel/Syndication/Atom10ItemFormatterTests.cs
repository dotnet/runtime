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
    public class Atom10ItemFormatterTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var formatter = new Formatter();
            Assert.Null(formatter.Item);
            Assert.Equal(typeof(SyndicationItem), formatter.ItemTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Atom10", formatter.Version);
        }

        [Fact]
        public void Ctor_GenericDefault()
        {
            var formatter = new GenericFormatter<SyndicationItem>();
            Assert.Null(formatter.Item);
            Assert.Equal(typeof(SyndicationItem), formatter.ItemTypeEntryPoint);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Atom10", formatter.Version);
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
            Assert.Equal("Atom10", formatter.Version);
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
            Assert.Equal("Atom10", formatter.Version);
        }

        [Fact]
        public void Ctor_NullItemToWrite_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("itemToWrite", () => new Atom10ItemFormatter((SyndicationItem)null));
            AssertExtensions.Throws<ArgumentNullException>("itemToWrite", () => new Atom10ItemFormatter<SyndicationItem>(null));
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
            Assert.Equal("Atom10", formatter.Version);
        }

        [Fact]
        public void Ctor_NullItemTypeToCreate_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("itemTypeToCreate", () => new Atom10ItemFormatter((Type)null));
        }

        [Fact]
        public void Ctor_InvalidItemTypeToCreate_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("itemTypeToCreate", () => new Atom10ItemFormatter(typeof(int)));
        }

        [Fact]
        public void GetSchema_Invoke_ReturnsNull()
        {
            IXmlSerializable formatter = new Atom10ItemFormatter();
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

            var fullSyndicationCategory = new SyndicationCategory();
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name1"), null);
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name2", "category_namespace"), "");
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name3", "category_namespace"), "category_value");
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name4", "xmlns"), "");

            fullSyndicationCategory.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            fullSyndicationCategory.Label = "category_label";

            fullSyndicationCategory.Name = "category_name";

            fullSyndicationCategory.Scheme = "category_scheme";

            var attributeSyndicationCategory = new SyndicationCategory
            {
                Name = "name",
                Label = "label",
                Scheme = "scheme"
            };
            attributeSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("term"), "term_value");
            attributeSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("label"), "label_value");
            attributeSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("scheme"), "scheme_value");

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
            fullSyndicationItem.Links.Add(attributeSyndicationLink);

            fullSyndicationItem.PublishDate = DateTimeOffset.MinValue.AddTicks(200);

            fullSyndicationItem.Summary = CreateContent("summary");

            fullSyndicationItem.Title = CreateContent("title");

            yield return new object[]
            {
                fullSyndicationItem,
@"<entry xml:base=""/relative"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
    <id>id</id>
    <title type=""html"" title_name1="""" d2p1:title_name2="""" d2p1:title_name3=""title_value"" d1p2:title_name4="""" xmlns:d2p1=""title_namespace"">title_title</title>
    <summary type=""html"" summary_name1="""" d2p1:summary_name2="""" d2p1:summary_name3=""summary_value"" d1p2:summary_name4="""" xmlns:d2p1=""summary_namespace"">summary_title</summary>
    <published>0001-01-01T00:00:00Z</published>
    <updated>0001-01-01T00:00:00Z</updated>
    <author />
    <author author_name1="""" d2p1:author_name2="""" d2p1:author_name3=""author_value"" d1p2:author_name4="""" xmlns:d2p1=""author_namespace"">
        <name>author_name</name>
        <uri>author_uri</uri>
        <email>author_email</email>
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </author>
    <contributor />
    <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"">
        <name>contributor_name</name>
        <uri>contributor_uri</uri>
        <email>contributor_email</email>
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </contributor>
    <link href="""" />
    <link xml:base=""http://link_url.com/"" link_name1="""" d2p1:link_name2="""" d2p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d2p1=""link_namespace"">
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </link>
    <link rel=""rel_value"" type=""type_value"" title=""title_value"" length=""100"" href=""href_value"" />
    <category term="""" />
    <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d2p1=""category_namespace"">
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </category>
    <category term=""term_value"" label=""label_value"" scheme=""scheme_value"" />
    <content type=""html"" content_name1="""" d2p1:content_name2="""" d2p1:content_name3=""content_value"" d1p2:content_name4="""" xmlns:d2p1=""content_namespace"">content_title</content>
    <rights type=""html"" copyright_name1="""" d2p1:copyright_name2="""" d2p1:copyright_name3=""copyright_value"" d1p2:copyright_name4="""" xmlns:d2p1=""copyright_namespace"">copyright_title</rights>
    <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </Atom10ItemFormatterTests.ExtensionObject>
</entry>"
        };
        }

        [Theory]
        [MemberData(nameof(WriteTo_TestData))]
        public void WriteTo_HasItem_SerializesExpected(SyndicationItem item, string expected)
        {
            var formatter = new Atom10ItemFormatter(item);
            CompareHelper.AssertEqualWriteOutput(expected, writer => formatter.WriteTo(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer => item.SaveAsAtom10(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer =>
            {
                writer.WriteStartElement("entry", "http://www.w3.org/2005/Atom");
                ((IXmlSerializable)formatter).WriteXml(writer);
                writer.WriteEndElement();
            });

            var genericFormatter = new Atom10ItemFormatter<SyndicationItem>(item);
            CompareHelper.AssertEqualWriteOutput(expected, writer => formatter.WriteTo(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer => item.SaveAsAtom10(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer =>
            {
                writer.WriteStartElement("entry", "http://www.w3.org/2005/Atom");
                ((IXmlSerializable)genericFormatter).WriteXml(writer);
                writer.WriteEndElement();
            });
        }

        [Fact]
        public void WriteTo_EmptyItem_SerializesExpected()
        {
            var formatter = new Atom10ItemFormatter(new SyndicationItem());
            var stringBuilder = new StringBuilder();

            using (XmlWriter writer = XmlWriter.Create(stringBuilder))
            {
                formatter.WriteTo(writer);
            }

            using (var stringReader = new StringReader(stringBuilder.ToString()))
            {
                XElement element = XElement.Load(stringReader);
                Assert.Equal("entry", element.Name.LocalName);
                Assert.Equal("http://www.w3.org/2005/Atom", element.Attribute("xmlns").Value);

                XElement[] elements = element.Elements().ToArray();
                Assert.Equal(3, elements.Length);
                Assert.Equal("id", elements[0].Name.LocalName);
                Assert.StartsWith("uuid:", elements[0].Value);

                Assert.Equal("title", elements[1].Name.LocalName);
                Assert.Equal("text", elements[1].Attribute("type").Value);
                Assert.Empty(elements[1].Value);

                Assert.Equal("updated", elements[2].Name.LocalName);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                Assert.True(now > DateTimeOffset.ParseExact(elements[2].Value, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            }
        }

        [Fact]
        public void WriteTo_NullWriter_ThrowsArgumentNullException()
        {
            var formatter = new Atom10ItemFormatter(new SyndicationItem());
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteTo(null));
        }

        [Fact]
        public void WriteTo_NoItem_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Atom10ItemFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteTo(writer));
            }
        }

        [Fact]
        public void WriteXml_NullWriter_ThrowsArgumentNullException()
        {
            IXmlSerializable formatter = new Atom10ItemFormatter();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteXml(null));
        }

        [Fact]
        public void WriteXml_NoItem_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                IXmlSerializable formatter = new Atom10ItemFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteXml(writer));
            }
        }

        public static IEnumerable<object[]> CanRead_TestData()
        {
            yield return new object[] { @"<entry />", false };
            yield return new object[] { @"<entry xmlns=""different"" />", false };
            yield return new object[] { @"<different xmlns=""http://www.w3.org/2005/Atom"" />", false };
            yield return new object[] { @"<entry xmlns=""http://www.w3.org/2005/Atom"" />", true };
            yield return new object[] { @"<entry xmlns=""http://www.w3.org/2005/Atom""></entry>", true };
        }

        [Theory]
        [MemberData(nameof(CanRead_TestData))]
        public void CanRead_ValidReader_ReturnsExpected(string xmlString, bool expected)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Atom10ItemFormatter();
                Assert.Equal(expected, formatter.CanRead(reader));
            }
        }

        [Fact]
        public void CanRead_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new Atom10ItemFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.CanRead(null));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Read_FullItem_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            string xmlString =
@"<entry xml:base=""/relative"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
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
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </author>
    <contributor />
    <contributor></contributor>
    <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"">
        <name>contributor_name</name>
        <uri>contributor_uri</uri>
        <email>contributor_email</email>
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </contributor>
    <link />
    <link></link>
    <link href="""" />
    <link xml:base=""http://link_url.com/"" link_name1="""" d2p1:link_name2="""" d2p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d2p1=""link_namespace"">
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </link>
    <category />
    <category></category>
    <category term="""" />
    <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d2p1=""category_namespace"">
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </category>
    <content type=""html"" content_name1="""" d2p1:content_name2="""" d2p1:content_name3=""content_value"" d1p2:content_name4="""" xmlns:d2p1=""content_namespace"">content_title</content>
    <rights type=""html"" copyright_name1="""" d2p1:copyright_name2="""" d2p1:copyright_name3=""copyright_value"" d1p2:copyright_name4="""" xmlns:d2p1=""copyright_namespace"">copyright_title</rights>
    <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </Atom10ItemFormatterTests.ExtensionObject>
</entry>";
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

                Assert.Equal(3, item.Authors.Count);

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

                Assert.Equal(new Uri("/relative", UriKind.Relative), item.BaseUri);

                Assert.Equal(4, item.Categories.Count);
                SyndicationCategory firstCategory = item.Categories[0];
                Assert.Empty(firstCategory.AttributeExtensions);
                Assert.Empty(firstCategory.ElementExtensions);
                Assert.Null(firstCategory.Name);
                Assert.Null(firstCategory.Scheme);
                Assert.Null(firstCategory.Label);

                SyndicationCategory secondCategory = item.Categories[1];
                Assert.Empty(secondCategory.AttributeExtensions);
                Assert.Empty(secondCategory.ElementExtensions);
                Assert.Null(secondCategory.Name);
                Assert.Null(secondCategory.Scheme);
                Assert.Null(secondCategory.Label);

                SyndicationCategory thircategory = item.Categories[2];
                Assert.Empty(thircategory.AttributeExtensions);
                Assert.Empty(thircategory.ElementExtensions);
                Assert.Empty(thircategory.Name);
                Assert.Null(thircategory.Scheme);
                Assert.Null(thircategory.Label);

                SyndicationCategory fourthCategory = item.Categories[3];
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

                SyndicationLink firstLink = item.Links[0];
                Assert.Empty(firstLink.AttributeExtensions);
                Assert.Empty(firstLink.ElementExtensions);
                Assert.Equal(0, firstLink.Length);
                Assert.Null(firstLink.MediaType);
                Assert.Null(firstLink.RelationshipType);
                Assert.Null(firstLink.Title);
                Assert.Null(firstLink.Uri);

                SyndicationLink secondLink = item.Links[1];
                Assert.Empty(secondLink.AttributeExtensions);
                Assert.Empty(secondLink.ElementExtensions);
                Assert.Equal(0, secondLink.Length);
                Assert.Null(secondLink.MediaType);
                Assert.Null(secondLink.RelationshipType);
                Assert.Null(secondLink.Title);
                Assert.Null(secondLink.Uri);

                SyndicationLink thirdLink = item.Links[2];
                Assert.Empty(thirdLink.AttributeExtensions);
                Assert.Empty(thirdLink.ElementExtensions);
                Assert.Equal(0, thirdLink.Length);
                Assert.Null(thirdLink.MediaType);
                Assert.Null(thirdLink.RelationshipType);
                Assert.Null(thirdLink.Title);
                Assert.Empty(thirdLink.Uri.OriginalString);

                SyndicationLink fourthLink = item.Links[3];
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
                    Assert.Equal(10, fourthLink.ElementExtensions[0].GetObject<ExtensionObject>().Value);
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
            });
        }

        [Theory]
        [InlineData(@"<content></content>", true, true)]
        [InlineData(@"<content></content>", false, false)]
        [InlineData(@"<content />", true, true)]
        [InlineData(@"<content />", false, false)]
        [InlineData(@"<content xmlns=""http://www.w3.org/2005/Atom""/>", true, true)]
        [InlineData(@"<content xmlns=""http://www.w3.org/2005/Atom""/>", false, false)]
        public void Read_EmptyContent_ReturnsExpected(string contentXmlString, bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            VerifyRead(@"<entry xmlns=""http://www.w3.org/2005/Atom"">" + contentXmlString + "</entry>", preserveAttributeExtensions, preserveElementExtensions, item =>
            {
                TextSyndicationContent content = Assert.IsType<TextSyndicationContent>(item.Content);
                Assert.Empty(content.AttributeExtensions);
                Assert.Empty(content.Text);
                Assert.Equal("text", content.Type);
            });
        }

        [Theory]
        [InlineData(@"<content src=""http://microsoft.com"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"" xmlns=""http://www.w3.org/2005/Atom""></content>", true, true)]
        [InlineData(@"<content src=""http://microsoft.com"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"" xmlns=""http://www.w3.org/2005/Atom""></content>", false, false)]
        [InlineData(@"<content src=""http://microsoft.com"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"" xmlns=""http://www.w3.org/2005/Atom"" />", true, true)]
        [InlineData(@"<content src=""http://microsoft.com"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"" xmlns=""http://www.w3.org/2005/Atom"" />", false, false)]
        public void Read_UriContent_ReturnsExpected(string contentXmlString, bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            VerifyRead(@"<entry xmlns=""http://www.w3.org/2005/Atom"">" + contentXmlString + "</entry>", preserveAttributeExtensions, preserveElementExtensions, item =>
            {
                UrlSyndicationContent content = Assert.IsType<UrlSyndicationContent>(item.Content);
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, content.AttributeExtensions.Count);
                    Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("item_name1")]);
                    Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("item_name2", "item_namespace")]);
                    Assert.Equal("item_value", content.AttributeExtensions[new XmlQualifiedName("item_name3", "item_namespace")]);
                    Assert.Equal("", content.AttributeExtensions[new XmlQualifiedName("item_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(content.AttributeExtensions);
                }
                Assert.Equal(new Uri("http://microsoft.com"), content.Url);
                Assert.Equal("text", content.Type);
            });
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Read_TryParseTrue_ReturnsExpected(bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            using (var stringReader = new StringReader(
@"<entry xml:base=""/relative"" item_name1="""" d1p1:item_name2="""" d1p1:item_name3=""item_value"" d1p2:item_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""item_namespace"" xmlns=""http://www.w3.org/2005/Atom"">
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
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </author>
    <contributor />
    <contributor></contributor>
    <contributor contributor_name1="""" d2p1:contributor_name2="""" d2p1:contributor_name3=""contributor_value"" d1p2:contributor_name4="""" xmlns:d2p1=""contributor_namespace"">
        <name>contributor_name</name>
        <uri>contributor_uri</uri>
        <email>contributor_email</email>
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </contributor>
    <link href="""" />
    <link xml:base=""http://link_url.com/"" link_name1="""" d2p1:link_name2="""" d2p1:link_name3=""link_value"" d1p2:link_name4="""" rel=""link_relationshipType"" type=""link_mediaType"" title=""link_title"" length=""10"" href=""http://link_uri.com/"" xmlns:d2p1=""link_namespace"">
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </link>
    <category />
    <category term="""" />
    <category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d2p1=""category_namespace"">
        <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </Atom10ItemFormatterTests.ExtensionObject>
    </category>
    <content type=""html"" content_name1="""" d2p1:content_name2="""" d2p1:content_name3=""content_value"" d1p2:content_name4="""" xmlns:d2p1=""content_namespace"">content_title</content>
    <rights type=""html"" copyright_name1="""" d2p1:copyright_name2="""" d2p1:copyright_name3=""copyright_value"" d1p2:copyright_name4="""" xmlns:d2p1=""copyright_namespace"">copyright_title</rights>
    <Atom10ItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </Atom10ItemFormatterTests.ExtensionObject>
</entry>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Atom10ItemFormatter<SyndicationItemTryParseTrueSubclass>()
                {
                    PreserveAttributeExtensions = preserveAttributeExtensions,
                    PreserveElementExtensions = preserveElementExtensions
                };
                formatter.ReadFrom(reader);

                SyndicationItem item = formatter.Item;
                Assert.Empty(item.AttributeExtensions);
                
                Assert.Equal(3, item.Authors.Count);

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
                Assert.Equal("category_label", thirdCategory.Label);

                TextSyndicationContent content = Assert.IsType<TextSyndicationContent>(item.Content);
                Assert.Empty(content.AttributeExtensions);
                Assert.Equal("overriden", content.Text);
                Assert.Equal("text", content.Type);

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

                Assert.Equal(2, item.Links.Count);

                SyndicationLink firstLink = item.Links[0];
                Assert.Empty(firstLink.AttributeExtensions);
                Assert.Empty(firstLink.ElementExtensions);
                Assert.Equal(0, firstLink.Length);
                Assert.Null(firstLink.MediaType);
                Assert.Null(firstLink.RelationshipType);
                Assert.Null(firstLink.Title);
                Assert.Empty(firstLink.Uri.OriginalString);

                SyndicationLink secondLink = item.Links[1];
                if (preserveAttributeExtensions)
                {
                    Assert.Equal(4, secondLink.AttributeExtensions.Count);
                    Assert.Equal("", secondLink.AttributeExtensions[new XmlQualifiedName("link_name1")]);
                    Assert.Equal("", secondLink.AttributeExtensions[new XmlQualifiedName("link_name2", "link_namespace")]);
                    Assert.Equal("link_value", secondLink.AttributeExtensions[new XmlQualifiedName("link_name3", "link_namespace")]);
                    Assert.Equal("", secondLink.AttributeExtensions[new XmlQualifiedName("link_name4", "xmlns")]);
                }
                else
                {
                    Assert.Empty(secondLink.AttributeExtensions);
                }
                Assert.Empty(secondLink.ElementExtensions);
                Assert.Equal(new Uri("http://link_url.com"), secondLink.BaseUri);
                Assert.Equal(10, secondLink.Length);
                Assert.Equal("link_mediaType", secondLink.MediaType);
                Assert.Equal("link_relationshipType", secondLink.RelationshipType);
                Assert.Equal("link_title", secondLink.Title);
                Assert.Equal(new Uri("http://link_uri.com"), secondLink.Uri);

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
            }
        }

        [Theory]
        [InlineData(@"<entry xmlns=""http://www.w3.org/2005/Atom""></entry>", true, true)]
        [InlineData(@"<entry xmlns=""http://www.w3.org/2005/Atom""></entry>", false, false)]
        [InlineData(@"<entry xmlns=""http://www.w3.org/2005/Atom"" />", true, true)]
        [InlineData(@"<entry xmlns=""http://www.w3.org/2005/Atom"" />", false, false)]
        public void Read_EmptyItem_ReturnsExpected(string xmlString, bool preserveAttributeExtensions, bool preserveElementExtensions)
        {
            VerifyRead(xmlString, preserveElementExtensions, preserveElementExtensions, item =>
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
                var formatter = new Atom10ItemFormatter()
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

                var formatter = new Atom10ItemFormatter()
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
                var formatter = new Atom10ItemFormatter(typeof(SyndicationItemSubclass))
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

                var formatter = new Atom10ItemFormatter(typeof(SyndicationItemSubclass))
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
                var formatter = new Atom10ItemFormatter<SyndicationItem>()
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

                var formatter = new Atom10ItemFormatter<SyndicationItem>()
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
                var formatter = new Atom10ItemFormatter<SyndicationItemSubclass>()
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

                var formatter = new Atom10ItemFormatter<SyndicationItemSubclass>()
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
                    SyndicationItem item = SyndicationItem.Load<SyndicationItem>(reader);
                    verifyAction(item);
                }
            }
        }

        [Fact]
        public void ReadFrom_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new Atom10ItemFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadFrom(null));
        }

        [Fact]
        public void ReadFrom_NullCreatedItem_ThrowsArgumentNullException()
        {
            using (var stringReader = new StringReader(@"<entry xmlns=""http://www.w3.org/2005/Atom""></entry>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new NullCreatedItemFormatter();
                AssertExtensions.Throws<ArgumentNullException>("item", () => formatter.ReadFrom(reader));
            }
        }

        [Theory]
        [InlineData(@"<different xmlns=""http://www.w3.org/2005/Atom""></different>")]
        [InlineData(@"<entry xmlns=""different""></entry>")]
        [InlineData(@"<entry></entry>")]
        [InlineData(@"<entry />")]
        public void ReadFrom_CantRead_ThrowsXmlException(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Atom10ItemFormatter();
                Assert.Throws<XmlException>(() => formatter.ReadFrom(reader));
            }
        }

        [Theory]
        [InlineData("<entry></entry>")]
        [InlineData(@"<app:entry xmlns:app=""http://www.w3.org/2005/Atom""></app:entry>")]
        [InlineData(@"<entry xmlns=""different""></entry>")]
        [InlineData(@"<different xmlns=""http://www.w3.org/2005/Atom""></different>")]
        public void ReadXml_ValidReader_Success(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new Atom10ItemFormatter();
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
            IXmlSerializable formatter = new Atom10ItemFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadXml(null));
        }

        [Fact]
        public void ReadXml_NullCreatedItem_ThrowsArgumentNullException()
        {
            using (var stringReader = new StringReader(@"<entry xmlns=""http://www.w3.org/2005/Atom""></entry>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                IXmlSerializable formatter = new NullCreatedItemFormatter();
                AssertExtensions.Throws<ArgumentNullException>("item", () => formatter.ReadXml(reader));
            }
        }

        [Fact]
        public void ReadXml_ThrowsArgumentException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new ArgumentException());
            IXmlSerializable formatter = new Atom10ItemFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Fact]
        public void ReadXml_ThrowsFormatException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new FormatException());
            IXmlSerializable formatter = new Atom10ItemFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Custom date parsing added in .NET Core changes this behaviour")]
        public void Read_InvalidLastUpdatedTime_GetThrowsXmlExcepton(string updated)
        {
            using (var stringReader = new StringReader(@"<entry xmlns=""http://www.w3.org/2005/Atom""><updated>" + updated + "</updated></entry>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Atom10ItemFormatter();
                formatter.ReadFrom(reader);
                Assert.Throws<XmlException>(() => formatter.Item.LastUpdatedTime);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Custom date parsing added in .NET Core changes this behaviour")]
        public void Read_InvalidPublishDate_GetThrowsXmlExcepton(string published)
        {
            using (var stringReader = new StringReader(@"<entry xmlns=""http://www.w3.org/2005/Atom""><published>" + published + "</published></entry>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new Atom10ItemFormatter();
                formatter.ReadFrom(reader);
                Assert.Throws<XmlException>(() => formatter.Item.PublishDate);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PreserveAttributeExtensions_Set_GetReturnsExpected(bool preserveAttributeExtensions)
        {
            var formatter = new Atom10ItemFormatter() { PreserveAttributeExtensions = preserveAttributeExtensions };
            Assert.Equal(preserveAttributeExtensions, formatter.PreserveAttributeExtensions);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PreserveElementExtensions_Set_GetReturnsExpected(bool preserveElementExtensions)
        {
            var formatter = new Atom10ItemFormatter() { PreserveElementExtensions = preserveElementExtensions };
            Assert.Equal(preserveElementExtensions, formatter.PreserveElementExtensions);
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

        public class NullCreatedItemFormatter : Atom10ItemFormatter
        {
            protected override SyndicationItem CreateItemInstance() => null;
        }

        public class Formatter : Atom10ItemFormatter
        {
            public Formatter() : base() { }

            public Formatter(SyndicationItem itemToWrite) : base(itemToWrite) { }

            public Formatter(Type itemTypeToCreate) : base(itemTypeToCreate) { }

            public Type ItemTypeEntryPoint => ItemType;

            public SyndicationItem CreateItemInstanceEntryPoint() => CreateItemInstance();
        }

        public class GenericFormatter<T> : Atom10ItemFormatter<T> where T : SyndicationItem, new()
        {
            public GenericFormatter() : base() { }

            public GenericFormatter(T itemToWrite) : base(itemToWrite) { }

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
