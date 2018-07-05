// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class SyndicationItemTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var item = new SyndicationItem();
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

        public static IEnumerable<object[]> Ctor_String_String_Uri_TestData()
        {
            yield return new object[] { null, null, null };
            yield return new object[] { "", "", new Uri("http://microsoft.com") };
            yield return new object[] { "title", "content", new Uri("/relative", UriKind.Relative) };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_String_Uri_TestData))]
        public void Ctor_String_String_Uri(string title, string content, Uri itemAlternateLink)
        {
            var item = new SyndicationItem(title, content, itemAlternateLink);
            Assert.Empty(item.AttributeExtensions);
            Assert.Empty(item.Authors);
            Assert.Null(item.BaseUri);
            Assert.Empty(item.Categories);
            if (content == null)
            {
                Assert.Null(item.Content);
            }
            else
            {
                TextSyndicationContent textContent = Assert.IsType<TextSyndicationContent>(item.Content);
                Assert.Empty(textContent.AttributeExtensions);
                Assert.Equal(content, textContent.Text);
                Assert.Equal("text", textContent.Type);
            }
            Assert.Null(item.Copyright);
            Assert.Empty(item.ElementExtensions);
            Assert.Null(item.Id);
            Assert.Equal(default, item.LastUpdatedTime);
            if (itemAlternateLink == null)
            {
                Assert.Empty(item.Links);
            }
            else
            {
                SyndicationLink link = Assert.Single(item.Links);
                Assert.Empty(link.AttributeExtensions);
                Assert.Null(link.BaseUri);
                Assert.Empty(link.ElementExtensions);
                Assert.Equal(0, link.Length);
                Assert.Null(link.MediaType);
                Assert.Equal("alternate", link.RelationshipType);
                Assert.Null(link.Title);
                Assert.Equal(itemAlternateLink, link.Uri);
            }
            Assert.Equal(default, item.PublishDate);
            Assert.Null(item.SourceFeed);
            Assert.Null(item.Summary);

            if (title == null)
            {
                Assert.Null(item.Title);
            }
            else
            {
                Assert.Empty(item.Title.AttributeExtensions);
                Assert.Equal(title, item.Title.Text);
                Assert.Equal("text", item.Title.Type);
            }
        }

        public static IEnumerable<object[]> Ctor_String_String_Uri_String_DateTimeOffset_TestData()
        {
            yield return new object[] { null, null, null, null, default(DateTimeOffset) };
            yield return new object[] { "", "", new Uri("http://microsoft.com"), "", DateTimeOffset.Now };
            yield return new object[] { "title", "content", new Uri("/relative", UriKind.Relative), "id", DateTimeOffset.Now.AddDays(2) };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_String_Uri_String_DateTimeOffset_TestData))]
        public void Ctor_String_String_Uri_String_DateTimeOffset(string title, string content, Uri itemAlternateLink, string id, DateTimeOffset lastUpdatedTime)
        {
            var item = new SyndicationItem(title, content, itemAlternateLink, id, lastUpdatedTime);
            Assert.Empty(item.AttributeExtensions);
            Assert.Empty(item.Authors);
            Assert.Null(item.BaseUri);
            Assert.Empty(item.Categories);
            if (content == null)
            {
                Assert.Null(item.Content);
            }
            else
            {
                TextSyndicationContent textContent = Assert.IsType<TextSyndicationContent>(item.Content);
                Assert.Empty(textContent.AttributeExtensions);
                Assert.Equal(content, textContent.Text);
                Assert.Equal("text", textContent.Type);
            }
            Assert.Null(item.Copyright);
            Assert.Empty(item.ElementExtensions);
            Assert.Equal(id, item.Id);
            Assert.Equal(lastUpdatedTime, item.LastUpdatedTime);
            if (itemAlternateLink == null)
            {
                Assert.Empty(item.Links);
            }
            else
            {
                SyndicationLink link = Assert.Single(item.Links);
                Assert.Empty(link.AttributeExtensions);
                Assert.Null(link.BaseUri);
                Assert.Empty(link.ElementExtensions);
                Assert.Equal(0, link.Length);
                Assert.Null(link.MediaType);
                Assert.Equal("alternate", link.RelationshipType);
                Assert.Null(link.Title);
                Assert.Equal(itemAlternateLink, link.Uri);
            }
            Assert.Equal(default, item.PublishDate);
            Assert.Null(item.SourceFeed);
            Assert.Null(item.Summary);

            if (title == null)
            {
                Assert.Null(item.Title);
            }
            else
            {
                Assert.Empty(item.Title.AttributeExtensions);
                Assert.Equal(title, item.Title.Text);
                Assert.Equal("text", item.Title.Type);
            }
        }

        public static IEnumerable<object[]> Ctor_String_SyndicationContent_Uri_String_DateTimeOffset_TestData()
        {
            yield return new object[] { null, null, null, null, default(DateTimeOffset) };
            yield return new object[] { "",  new TextSyndicationContent("text"), new Uri("http://microsoft.com"), "", DateTimeOffset.Now };
            yield return new object[] { "title", new TextSyndicationContent("text", TextSyndicationContentKind.XHtml), new Uri("/relative", UriKind.Relative), "id", DateTimeOffset.Now.AddDays(2) };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_SyndicationContent_Uri_String_DateTimeOffset_TestData))]
        public void Ctor_String_SyndicationContent_Uri_String_DateTimeOffset(string title, SyndicationContent content, Uri itemAlternateLink, string id, DateTimeOffset lastUpdatedTime)
        {
            var item = new SyndicationItem(title, content, itemAlternateLink, id, lastUpdatedTime);
            Assert.Empty(item.AttributeExtensions);
            Assert.Empty(item.Authors);
            Assert.Null(item.BaseUri);
            Assert.Empty(item.Categories);
            Assert.Equal(content, item.Content);
            Assert.Null(item.Copyright);
            Assert.Empty(item.ElementExtensions);
            Assert.Equal(id, item.Id);
            Assert.Equal(lastUpdatedTime, item.LastUpdatedTime);
            if (itemAlternateLink == null)
            {
                Assert.Empty(item.Links);
            }
            else
            {
                SyndicationLink link = Assert.Single(item.Links);
                Assert.Empty(link.AttributeExtensions);
                Assert.Null(link.BaseUri);
                Assert.Empty(link.ElementExtensions);
                Assert.Equal(0, link.Length);
                Assert.Null(link.MediaType);
                Assert.Equal("alternate", link.RelationshipType);
                Assert.Null(link.Title);
                Assert.Equal(itemAlternateLink, link.Uri);
            }
            Assert.Equal(default, item.PublishDate);
            Assert.Null(item.SourceFeed);
            Assert.Null(item.Summary);

            if (title == null)
            {
                Assert.Null(item.Title);
            }
            else
            {
                Assert.Empty(item.Title.AttributeExtensions);
                Assert.Equal(title, item.Title.Text);
                Assert.Equal("text", item.Title.Type);
            }
        }

        [Fact]
        public void Ctor_SyndicationItem_Full()
        {
            var original = new SyndicationItem("title", new TextSyndicationContent("content", TextSyndicationContentKind.Html), new Uri("http://microsoft.com"), "id", DateTimeOffset.MinValue.AddTicks(10));
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");
            original.Authors.Add(new SyndicationPerson("email", "author", "uri"));
            original.BaseUri = new Uri("http://category_baseuri.com");
            original.Categories.Add(new SyndicationCategory("category"));
            original.Contributors.Add(new SyndicationPerson("name", "contributor", "uri"));
            original.Copyright = new TextSyndicationContent("copyright", TextSyndicationContentKind.Plaintext);
            original.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            original.PublishDate = DateTimeOffset.MinValue.AddTicks(11);
            original.SourceFeed = new SyndicationFeed("title", "description", new Uri("http://microsoft.com"));
            original.Summary = new TextSyndicationContent("summary", TextSyndicationContentKind.Html);

            var clone = new SyndicationItemSubclass(original);
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.NotSame(clone.Authors, original.Authors);
            Assert.Equal(1, clone.Authors.Count);
            Assert.NotSame(original.Authors[0], clone.Authors[0]);
            Assert.Equal("author", clone.Authors[0].Name);

            Assert.Equal(new Uri("http://category_baseuri.com"), original.BaseUri);

            Assert.NotSame(clone.Categories, original.Categories);
            Assert.Equal(1, clone.Categories.Count);
            Assert.NotSame(original.Categories[0], clone.Categories[0]);
            Assert.Equal("category", clone.Categories[0].Name);

            Assert.NotSame(clone.Content, original.Content);
            Assert.Equal("content", Assert.IsType<TextSyndicationContent>(clone.Content).Text);

            Assert.NotSame(clone.Contributors, original.Contributors);
            Assert.Equal(1, clone.Contributors.Count);
            Assert.NotSame(original.Contributors[0], clone.Contributors[0]);
            Assert.Equal("contributor", clone.Contributors[0].Name);

            Assert.NotSame(clone.Copyright, original.Copyright);
            Assert.Equal("copyright", clone.Copyright.Text);

            Assert.NotSame(clone.ElementExtensions, original.ElementExtensions);
            Assert.Equal(1, clone.ElementExtensions.Count);
            Assert.Equal(10, clone.ElementExtensions[0].GetObject<ExtensionObject>().Value);

            Assert.Equal("id", original.Id);

            Assert.Equal(DateTimeOffset.MinValue.AddTicks(10), original.LastUpdatedTime);

            Assert.NotSame(clone.Links, original.Links);
            Assert.Equal(1, clone.Links.Count);
            Assert.NotSame(original.Links[0], clone.Links[0]);
            Assert.Equal(new Uri("http://microsoft.com"), clone.Links[0].Uri);

            Assert.Equal(DateTimeOffset.MinValue.AddTicks(11), original.PublishDate);

            Assert.NotSame(clone.SourceFeed, original.SourceFeed);
            Assert.Equal("title", clone.SourceFeed.Title.Text);

            Assert.NotSame(clone.Summary, original.Summary);
            Assert.Equal("summary", clone.Summary.Text);

            Assert.NotSame(clone.Title, original.Title);
            Assert.Equal("title", clone.Title.Text);
        }

        [Fact]
        public void Ctor_SyndicationItem_Empty()
        {
            var original = new SyndicationItem();
            var clone = new SyndicationItemSubclass(original);
            Assert.Empty(clone.AttributeExtensions);
            Assert.Empty(clone.Authors);
            Assert.Null(clone.BaseUri);
            Assert.Empty(clone.Categories);
            Assert.Null(clone.Content);
            Assert.Null(clone.Copyright);
            Assert.Empty(clone.ElementExtensions);
            Assert.Null(clone.Id);
            Assert.Equal(default, clone.LastUpdatedTime);
            Assert.Empty(clone.Links);
            Assert.Equal(default, clone.PublishDate);
            Assert.Null(clone.SourceFeed);
            Assert.Null(clone.Summary);
            Assert.Null(clone.Title);
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => new SyndicationItemSubclass(null));
        }

        [Fact]
        public void AddPermalink_ValidPermalink_AddsToLinks()
        {
            var permalink = new Uri("http://microsoft.com");
            var item = new SyndicationItem();
            item.AddPermalink(permalink);

            SyndicationLink link = Assert.Single(item.Links);
            Assert.Empty(link.AttributeExtensions);
            Assert.Null(link.BaseUri);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(0, link.Length);
            Assert.Null(link.MediaType);
            Assert.Equal("alternate", link.RelationshipType);
            Assert.Null(link.Title);
            Assert.Equal(permalink, link.Uri);
        }

        [Fact]
        public void AddPermalink_NullPermalink_ThrowsArgumentNullException()
        {
            var item = new SyndicationItem();
            AssertExtensions.Throws<ArgumentNullException>("permalink", () => item.AddPermalink(null));
        }

        [Fact]
        public void AddPermalink_RelativePermalink_ThrowsInvalidOperationException()
        {
            var permalink = new Uri("/microsoft", UriKind.Relative);
            var item = new SyndicationItem();
            Assert.Throws<InvalidOperationException>(() => item.AddPermalink(permalink));
        }

        [Fact]
        public void Load_NullReader_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("reader", () => SyndicationItem.Load(null));
            AssertExtensions.Throws<ArgumentNullException>("reader", () => SyndicationItem.Load<SyndicationItem>(null));
        }

        [Fact]
        public void Clone_Full_ReturnsExpected()
        {
            var original = new SyndicationItem("title", new TextSyndicationContent("content", TextSyndicationContentKind.Html), new Uri("http://microsoft.com"), "id", DateTimeOffset.MinValue.AddTicks(10));
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");
            original.Authors.Add(new SyndicationPerson("email", "author", "uri"));
            original.BaseUri = new Uri("http://category_baseuri.com");
            original.Categories.Add(new SyndicationCategory("category"));
            original.Contributors.Add(new SyndicationPerson("name", "contributor", "uri"));
            original.Copyright = new TextSyndicationContent("copyright", TextSyndicationContentKind.Plaintext);
            original.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            original.PublishDate = DateTimeOffset.MinValue.AddTicks(11);
            original.SourceFeed = new SyndicationFeed("title", "description", new Uri("http://microsoft.com"));
            original.Summary = new TextSyndicationContent("summary", TextSyndicationContentKind.Html);

            SyndicationItem clone = original.Clone();
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.NotSame(clone.Authors, original.Authors);
            Assert.Equal(1, clone.Authors.Count);
            Assert.NotSame(original.Authors[0], clone.Authors[0]);
            Assert.Equal("author", clone.Authors[0].Name);

            Assert.Equal(new Uri("http://category_baseuri.com"), original.BaseUri);

            Assert.NotSame(clone.Categories, original.Categories);
            Assert.Equal(1, clone.Categories.Count);
            Assert.NotSame(original.Categories[0], clone.Categories[0]);
            Assert.Equal("category", clone.Categories[0].Name);

            Assert.NotSame(clone.Content, original.Content);
            Assert.Equal("content", Assert.IsType<TextSyndicationContent>(clone.Content).Text);

            Assert.NotSame(clone.Contributors, original.Contributors);
            Assert.Equal(1, clone.Contributors.Count);
            Assert.NotSame(original.Contributors[0], clone.Contributors[0]);
            Assert.Equal("contributor", clone.Contributors[0].Name);

            Assert.NotSame(clone.Copyright, original.Copyright);
            Assert.Equal("copyright", clone.Copyright.Text);

            Assert.NotSame(clone.ElementExtensions, original.ElementExtensions);
            Assert.Equal(1, clone.ElementExtensions.Count);
            Assert.Equal(10, clone.ElementExtensions[0].GetObject<ExtensionObject>().Value);

            Assert.Equal("id", original.Id);

            Assert.Equal(DateTimeOffset.MinValue.AddTicks(10), original.LastUpdatedTime);

            Assert.NotSame(clone.Links, original.Links);
            Assert.Equal(1, clone.Links.Count);
            Assert.NotSame(original.Links[0], clone.Links[0]);
            Assert.Equal(new Uri("http://microsoft.com"), clone.Links[0].Uri);

            Assert.Equal(DateTimeOffset.MinValue.AddTicks(11), original.PublishDate);

            Assert.NotSame(clone.SourceFeed, original.SourceFeed);
            Assert.Equal("title", clone.SourceFeed.Title.Text);

            Assert.NotSame(clone.Summary, original.Summary);
            Assert.Equal("summary", clone.Summary.Text);

            Assert.NotSame(clone.Title, original.Title);
            Assert.Equal("title", clone.Title.Text);
        }

        [Fact]
        public void Clone_Empty_ReturnsExpected()
        {
            var original = new SyndicationItem();
            SyndicationItem clone = original.Clone();
            Assert.Empty(clone.AttributeExtensions);
            Assert.Empty(clone.Authors);
            Assert.Null(clone.BaseUri);
            Assert.Empty(clone.Categories);
            Assert.Null(clone.Content);
            Assert.Null(clone.Copyright);
            Assert.Empty(clone.ElementExtensions);
            Assert.Null(clone.Id);
            Assert.Equal(default, clone.LastUpdatedTime);
            Assert.Empty(clone.Links);
            Assert.Equal(default, clone.PublishDate);
            Assert.Null(clone.SourceFeed);
            Assert.Null(clone.Summary);
            Assert.Null(clone.Title);
        }

        [Fact]
        public void GetAtom10Formatter_Invoke_ReturnsExpected()
        {
            var item = new SyndicationItem();
            Atom10ItemFormatter formatter = Assert.IsType<Atom10ItemFormatter>(item.GetAtom10Formatter());
            Assert.Same(item, formatter.Item);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Atom10", formatter.Version);
        }

        [Fact]
        public void GetRss20Formatter_Invoke_ReturnsExpected()
        {
            var item = new SyndicationItem();
            Rss20ItemFormatter formatter = Assert.IsType<Rss20ItemFormatter>(item.GetRss20Formatter());
            Assert.Same(item, formatter.Item);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.True(formatter.SerializeExtensionsAsAtom);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetRss20Formatter_Invoke_ReturnsExpected(bool serializeExtensionsAsAtom)
        {
            var item = new SyndicationItem();
            Rss20ItemFormatter formatter = Assert.IsType<Rss20ItemFormatter>(item.GetRss20Formatter(serializeExtensionsAsAtom));
            Assert.Same(item, formatter.Item);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal(serializeExtensionsAsAtom, formatter.SerializeExtensionsAsAtom);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void CreateCategory_Invoke_ReturnsExpected()
        {
            var item = new SyndicationItemSubclass();
            SyndicationCategory category = item.CreateCategoryEntryPoint();
            Assert.Empty(category.AttributeExtensions);
            Assert.Empty(category.ElementExtensions);
            Assert.Null(category.Name);
            Assert.Null(category.Scheme);
            Assert.Null(category.Label);
        }

        [Fact]
        public void CreateLink_Invoke_ReturnsExpected()
        {
            var item = new SyndicationItemSubclass();
            SyndicationLink link = item.CreateLinkEntryPoint();
            Assert.Empty(link.AttributeExtensions);
            Assert.Null(link.BaseUri);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(0, link.Length);
            Assert.Null(link.MediaType);
            Assert.Null(link.RelationshipType);
            Assert.Null(link.Title);
            Assert.Null(link.Uri);
        }

        [Fact]
        public void CreatePerson_Invoke_ReturnsExpected()
        {
            var item = new SyndicationItemSubclass();
            SyndicationPerson person = item.CreatePersonEntryPoint();
            Assert.Empty(person.AttributeExtensions);
            Assert.Empty(person.ElementExtensions);
            Assert.Null(person.Email);
            Assert.Null(person.Name);
            Assert.Null(person.Uri);
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("", "", "", "")]
        [InlineData("name", "ns", "value", "version")]
        [InlineData("xmlns", "ns", "value", "version")]
        [InlineData("name", "http://www.w3.org/2000/xmlns/", "value", "version")]
        [InlineData("type", "ns", "value", "version")]
        [InlineData("name", "http://www.w3.org/2001/XMLSchema-instance", "value", "version")]
        public void TryParseAttribute_Invoke_ReturnsFalse(string name, string ns, string value, string version)
        {
            var item = new SyndicationItemSubclass();
            Assert.False(item.TryParseAttributeEntryPoint(name, ns, value, version));
        }

        public static IEnumerable<object[]> TryParseContent_TestData()
        {
            yield return new object[] { null, null, null };
            yield return new object[] { new XElement("name").CreateReader(), "", "" };
            yield return new object[] { new XElement("name").CreateReader(), "contentType", "version" };
        }

        [Theory]
        [MemberData(nameof(TryParseContent_TestData))]
        public void TryParseContent_Invoke_ReturnsFalse(XmlReader reader, string contentType, string version)
        {
            var item = new SyndicationItemSubclass();
            Assert.False(item.TryParseContentEntryPoint(reader, contentType, version, out SyndicationContent content));
            Assert.Null(content);
        }

        public static IEnumerable<object[]> TryParseElement_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new XElement("name").CreateReader(), "" };
            yield return new object[] { new XElement("name").CreateReader(), "version" };
        }

        [Theory]
        [MemberData(nameof(TryParseElement_TestData))]
        public void TryParseElement_Invoke_ReturnsFalse(XmlReader reader, string version)
        {
            var item = new SyndicationItemSubclass();
            Assert.False(item.TryParseElementEntryPoint(reader, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteAttributeExtensions_Invoke_ReturnsExpected(string version)
        {
            var item = new SyndicationItemSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => item.WriteAttributeExtensionsEntryPoint(writer, version));

            item.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            item.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            item.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => item.WriteAttributeExtensionsEntryPoint(writer, "version"));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var item = new SyndicationItemSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => item.WriteAttributeExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteElementExtensions_Invoke_ReturnsExpected(string version)
        {
            var item = new SyndicationItemSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => item.WriteElementExtensionsEntryPoint(writer, version));

            item.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            item.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<SyndicationItemTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</SyndicationItemTests.ExtensionObject>
<SyndicationItemTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</SyndicationItemTests.ExtensionObject>", writer => item.WriteElementExtensionsEntryPoint(writer, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var item = new SyndicationItemSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => item.WriteElementExtensionsEntryPoint(null, "version"));
        }

        private class SyndicationItemSubclass : SyndicationItem
        {
            public SyndicationItemSubclass() : base() { }

            public SyndicationItemSubclass(SyndicationItem source) : base(source) { }

            public SyndicationCategory CreateCategoryEntryPoint() => CreateCategory();

            public SyndicationLink CreateLinkEntryPoint() => CreateLink();

            public SyndicationPerson CreatePersonEntryPoint() => CreatePerson();

            public bool TryParseAttributeEntryPoint(string name, string ns, string value, string version) => TryParseAttribute(name, ns, value, version);

            public bool TryParseContentEntryPoint(XmlReader reader, string contentType, string version, out SyndicationContent content)
            {
                return TryParseContent(reader, contentType, version, out content);
            }

            public bool TryParseElementEntryPoint(XmlReader reader, string version) => TryParseElement(reader, version);

            public void WriteAttributeExtensionsEntryPoint(XmlWriter writer, string version) => WriteAttributeExtensions(writer, version);

            public void WriteElementExtensionsEntryPoint(XmlWriter writer, string version) => WriteElementExtensions(writer, version);
        }

        [DataContract]
        private class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}
