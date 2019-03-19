// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public partial class SyndicationFeedTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var feed = new SyndicationFeed();
            VerifySyndicationFeed(feed, null, null, null, null, default, null);
        }

        public static IEnumerable<object[]> Ctor_Items_TestData()
        {
            yield return new object[] { new SyndicationItem[0] };
            yield return new object[] { new SyndicationItem[] { new SyndicationItem(), null } };
        }

        [Theory]
        [InlineData(null)]
        [MemberData(nameof(Ctor_Items_TestData))]
        public void Ctor_Items(IEnumerable<SyndicationItem> items)
        {
            var feed = new SyndicationFeed(items);
            VerifySyndicationFeed(feed, null, null, null, null, default, items);
        }

        public static IEnumerable<object[]> Ctor_String_String_Uri_TestData()
        {
            yield return new object[] { null, null, null };
            yield return new object[] { "", "", new Uri("http://microsoft.com") };
            yield return new object[] { "title", "description", new Uri("/relative", UriKind.Relative) };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_String_Uri_TestData))]
        public void Ctor_String_String_Uri(string title, string description, Uri feedAlternateLink)
        {
            var feed = new SyndicationFeed(title, description, feedAlternateLink);
            VerifySyndicationFeed(feed, title, description, feedAlternateLink, null, default, null);
        }
        
        public static IEnumerable<object[]> Ctor_String_String_Uri_Items_TestData()
        {
            yield return new object[] { null, null, null, null };
            yield return new object[] { "", "", new Uri("http://microsoft.com"), new SyndicationItem[0] };
            yield return new object[] { "title", "description", new Uri("/relative", UriKind.Relative), new SyndicationItem[] { new SyndicationItem(), null } };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_String_Uri_Items_TestData))]
        public void Ctor_String_String_Uri_Items(string title, string description, Uri feedAlternateLink, IEnumerable<SyndicationItem> items)
        {
            var feed = new SyndicationFeed(title, description, feedAlternateLink, items);
            VerifySyndicationFeed(feed, title, description, feedAlternateLink, null, default, items);
        }

        public static IEnumerable<object[]> Ctor_String_String_Uri_String_DateTimeOffset_TestData()
        {
            yield return new object[] { null, null, null, null, default(DateTimeOffset) };
            yield return new object[] { "", "", new Uri("http://microsoft.com"), "", DateTimeOffset.Now };
            yield return new object[] { "title", "description", new Uri("/relative", UriKind.Relative), "id", DateTimeOffset.Now.AddDays(2) };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_String_Uri_String_DateTimeOffset_TestData))]
        public void Ctor_String_String_Uri_String_DateTimeOffset(string title, string description, Uri feedAlternateLink, string id, DateTimeOffset lastUpdatedTime)
        {
            var feed = new SyndicationFeed(title, description, feedAlternateLink, id, lastUpdatedTime);
            VerifySyndicationFeed(feed, title, description, feedAlternateLink, id, lastUpdatedTime, null);
        }

        public static IEnumerable<object[]> Ctor_String_String_Uri_String_DateTimeOffset_Items_TestData()
        {
            yield return new object[] { null, null, null, null, default(DateTimeOffset), null };
            yield return new object[] { "", "", new Uri("http://microsoft.com"), "", DateTimeOffset.Now, new SyndicationItem[0] };
            yield return new object[] { "title", "description", new Uri("/relative", UriKind.Relative), "id", DateTimeOffset.Now.AddDays(2), new SyndicationItem[] { new SyndicationItem(), null } };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_String_Uri_String_DateTimeOffset_Items_TestData))]
        public void Ctor_String_String_Uri_String_DateTimeOffset_Items(string title, string description, Uri feedAlternateLink, string id, DateTimeOffset lastUpdatedTime, IEnumerable<SyndicationItem> items)
        {
            var feed = new SyndicationFeed(title, description, feedAlternateLink, id, lastUpdatedTime, items);
            VerifySyndicationFeed(feed, title, description, feedAlternateLink, id, lastUpdatedTime, items);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_SyndicationFeed_Full(bool cloneItems)
        {
            var original = new SyndicationFeed();
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");
            original.Authors.Add(new SyndicationPerson("email", "author", "uri"));
            original.BaseUri = new Uri("http://feed_baseuri.com");
            original.Categories.Add(new SyndicationCategory("category"));
            original.Contributors.Add(new SyndicationPerson("email", "contributor", "uri"));
            original.Copyright = new TextSyndicationContent("copyright");
            original.Description = new TextSyndicationContent("description");
            original.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            original.Generator = "generator";
            original.Id = "id";
            original.ImageUrl = new Uri("http://imageurl.com");
            original.Items = new SyndicationItem[]
            {
                new SyndicationItem("title", "content", null)
            };
            original.Language = "language";
            original.LastUpdatedTime = DateTimeOffset.MinValue.AddTicks(10);
            original.Links.Add(new SyndicationLink(new Uri("http://microsoft.com")));
            original.Title = new TextSyndicationContent("title");

            var clone = new SyndicationFeedSubclass(original, cloneItems);
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.NotSame(clone.Authors, original.Authors);
            Assert.Equal(1, clone.Authors.Count);
            Assert.NotSame(original.Authors[0], clone.Authors[0]);
            Assert.Equal("author", clone.Authors[0].Name);

            Assert.Equal(new Uri("http://feed_baseuri.com/"), clone.BaseUri);

            Assert.NotSame(clone.Categories, original.Categories);
            Assert.Equal(1, clone.Categories.Count);
            Assert.NotSame(original.Categories[0], clone.Categories[0]);
            Assert.Equal("category", clone.Categories[0].Name);

            Assert.NotSame(clone.Contributors, original.Contributors);
            Assert.Equal(1, clone.Contributors.Count);
            Assert.NotSame(original.Contributors[0], clone.Contributors[0]);
            Assert.Equal("contributor", clone.Contributors[0].Name);

            Assert.NotSame(clone.Copyright, original.Copyright);
            Assert.Equal("copyright", clone.Copyright.Text);

            Assert.NotSame(clone.Description, original.Description);
            Assert.Equal("description", clone.Description.Text);

            Assert.NotSame(clone.ElementExtensions, original.ElementExtensions);
            Assert.Equal(1, clone.ElementExtensions.Count);
            Assert.Equal(10, clone.ElementExtensions[0].GetObject<ExtensionObject>().Value);

            Assert.Equal("generator", original.Generator);

            Assert.Equal("id", original.Id);

            Assert.Equal(new Uri("http://imageurl.com"), original.ImageUrl);

            Assert.NotSame(clone.Contributors, original.Items);
            Assert.Equal(1, clone.Items.Count());
            if (cloneItems)
            {
                Assert.NotSame(((IList<SyndicationItem>)original.Items)[0], ((IList<SyndicationItem>)clone.Items)[0]);
                Assert.Equal("title", (((IList<SyndicationItem>)original.Items)[0]).Title.Text);
            }
            else
            {
                Assert.Same(((IList<SyndicationItem>)original.Items)[0], ((IList<SyndicationItem>)clone.Items)[0]);
            }

            Assert.Equal("language", original.Language);

            Assert.Equal(DateTimeOffset.MinValue.AddTicks(10), original.LastUpdatedTime);

            Assert.NotSame(clone.Links, original.Links);
            Assert.Equal(1, clone.Links.Count);
            Assert.NotSame(original.Links[0], clone.Links[0]);
            Assert.Equal(new Uri("http://microsoft.com"), clone.Links[0].Uri);

            Assert.NotSame(clone.Title, original.Title);
            Assert.Equal("title", clone.Title.Text);
        }

        [Fact]
        public void Ctor_SyndicationFeed_Empty()
        {
            var original = new SyndicationFeed();
            var clone = new SyndicationFeedSubclass(original, false);
            Assert.Empty(clone.AttributeExtensions);
            Assert.Empty(clone.Authors);
            Assert.Null(clone.BaseUri);
            Assert.Empty(clone.Categories);
            Assert.Empty(clone.Contributors);
            Assert.Null(clone.Copyright);
            Assert.Null(clone.Description);
            Assert.Empty(clone.ElementExtensions);
            Assert.Null(clone.Generator);
            Assert.Null(clone.Id);
            Assert.Null(clone.ImageUrl);
            Assert.Empty(clone.Items);
            Assert.Null(clone.Language);
            Assert.Equal(default, clone.LastUpdatedTime);
            Assert.Empty(clone.Links);
            Assert.Null(clone.Title);
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            var feed = new SyndicationFeed();
            AssertExtensions.Throws<ArgumentNullException>("source", () => new SyndicationFeedSubclass(null, true));
        }

        [Fact]
        public void Ctor_NullSourceItems_ThrowsInvalidOperationException()
        {
            var feed = new SyndicationFeed();
            Assert.Throws<InvalidOperationException>(() => new SyndicationFeedSubclass(feed, true));
        }

        [Fact]
        public void Ctor_SourceItemsNotIList_ThrowsInvalidOperationException()
        {
            var feed = new SyndicationFeed(new HashSet<SyndicationItem>());
            Assert.Throws<InvalidOperationException>(() => new SyndicationFeedSubclass(feed, true));
        }

        [Fact]
        public void Load_NullReader_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("reader", () => SyndicationFeed.Load(null));
            AssertExtensions.Throws<ArgumentNullException>("reader", () => SyndicationFeed.Load<SyndicationFeed>(null));
        }

        [Fact]
        public void Load_InvalidReader_ThrowsXmlException()
        {
            XmlReader reader = new XElement("invalid").CreateReader();
            Assert.Throws<XmlException>(() => SyndicationFeed.Load(reader));
            Assert.Throws<XmlException>(() => SyndicationFeed.Load<SyndicationFeed>(reader));
        }

        [Fact]
        public void GetAtom10Formatter_Invoke_ReturnsExpected()
        {
            var feed = new SyndicationFeed();
            Atom10FeedFormatter formatter = Assert.IsType<Atom10FeedFormatter>(feed.GetAtom10Formatter());
            Assert.Same(feed, formatter.Feed);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal("Atom10", formatter.Version);
        }

        [Fact]
        public void GetRss20Formatter_Invoke_ReturnsExpected()
        {
            var feed = new SyndicationFeed();
            Rss20FeedFormatter formatter = Assert.IsType<Rss20FeedFormatter>(feed.GetRss20Formatter());
            Assert.Same(feed, formatter.Feed);
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
            var feed = new SyndicationFeed();
            Rss20FeedFormatter formatter = Assert.IsType<Rss20FeedFormatter>(feed.GetRss20Formatter(serializeExtensionsAsAtom));
            Assert.Same(feed, formatter.Feed);
            Assert.True(formatter.PreserveAttributeExtensions);
            Assert.True(formatter.PreserveElementExtensions);
            Assert.Equal(serializeExtensionsAsAtom, formatter.SerializeExtensionsAsAtom);
            Assert.Equal("Rss20", formatter.Version);
        }

        [Fact]
        public void CreateCategory_Invoke_ReturnsExpected()
        {
            var feed = new SyndicationFeedSubclass();
            SyndicationCategory category = feed.CreateCategoryEntryPoint();
            Assert.Empty(category.AttributeExtensions);
            Assert.Empty(category.ElementExtensions);
            Assert.Null(category.Name);
            Assert.Null(category.Scheme);
            Assert.Null(category.Label);
        }

        [Fact]
        public void CreateLink_Invoke_ReturnsExpected()
        {
            var feed = new SyndicationFeedSubclass();
            SyndicationLink link = feed.CreateLinkEntryPoint();
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
            var feed = new SyndicationFeedSubclass();
            SyndicationPerson person = feed.CreatePersonEntryPoint();
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
            var feed = new SyndicationFeedSubclass();
            Assert.False(feed.TryParseAttributeEntryPoint(name, ns, value, version));
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
            var feed = new SyndicationFeedSubclass();
            Assert.False(feed.TryParseElementEntryPoint(reader, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteAttributeExtensions_Invoke_ReturnsExpected(string version)
        {
            var feed = new SyndicationFeedSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => feed.WriteAttributeExtensionsEntryPoint(writer, version));

            feed.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            feed.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            feed.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => feed.WriteAttributeExtensionsEntryPoint(writer, "version"));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var feed = new SyndicationFeedSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => feed.WriteAttributeExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteElementExtensions_Invoke_ReturnsExpected(string version)
        {
            var feed = new SyndicationFeedSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => feed.WriteElementExtensionsEntryPoint(writer, version));

            feed.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            feed.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<SyndicationFeedTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</SyndicationFeedTests.ExtensionObject>
<SyndicationFeedTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</SyndicationFeedTests.ExtensionObject>", writer => feed.WriteElementExtensionsEntryPoint(writer, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var feed = new SyndicationFeedSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => feed.WriteElementExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [MemberData(nameof(Ctor_Items_TestData))]
        public void Items_Set_GetReturnsExpected(IEnumerable<SyndicationItem> value)
        {
            var feed = new SyndicationFeed
            {
                Items = value
            };
            Assert.Same(value, feed.Items);
        }

        [Fact]
        public void Items_SetNull_ThrowsArgumentNullException()
        {
            var feed = new SyndicationFeed();
            AssertExtensions.Throws<ArgumentNullException>("value", () => feed.Items = null);
        }

        private static void VerifySyndicationFeed(SyndicationFeed feed, string title, string description, Uri feedAlternateLink, string id, DateTimeOffset lastUpdatedTime, IEnumerable<SyndicationItem> items)
        {
            Assert.Empty(feed.AttributeExtensions);
            Assert.Empty(feed.Authors);
            Assert.Null(feed.BaseUri);
            Assert.Empty(feed.Categories);
            Assert.Empty(feed.Contributors);
            Assert.Null(feed.Copyright);
            if (description == null)
            {
                Assert.Null(feed.Description);
            }
            else
            {
                Assert.Empty(feed.Description.AttributeExtensions);
                Assert.Equal(description, feed.Description.Text);
                Assert.Equal("text", feed.Description.Type);
            }
            Assert.Empty(feed.ElementExtensions);
            Assert.Null(feed.Generator);
            Assert.Equal(id, feed.Id);
            Assert.Null(feed.ImageUrl);
            if (items == null)
            {
                Assert.Empty(feed.Items);
            }
            else
            {
                Assert.Same(items, feed.Items);
            }
            Assert.Null(feed.Language);
            Assert.Equal(lastUpdatedTime, feed.LastUpdatedTime);
            if (feedAlternateLink == null)
            {
                Assert.Empty(feed.Links);
            }
            else
            {
                SyndicationLink link = Assert.Single(feed.Links);
                Assert.Empty(link.AttributeExtensions);
                Assert.Null(link.BaseUri);
                Assert.Empty(link.ElementExtensions);
                Assert.Equal(0, link.Length);
                Assert.Null(link.MediaType);
                Assert.Equal("alternate", link.RelationshipType);
                Assert.Null(link.Title);
                Assert.Equal(feedAlternateLink, link.Uri);
            }
            if (title == null)
            {
                Assert.Null(feed.Title);
            }
            else
            {
                Assert.Empty(feed.Title.AttributeExtensions);
                Assert.Equal(title, feed.Title.Text);
                Assert.Equal("text", feed.Title.Type);
            }
        }

        private class SyndicationFeedSubclass : SyndicationFeed
        {
            public SyndicationFeedSubclass() : base() { }

            public SyndicationFeedSubclass(SyndicationFeed source, bool cloneItems) : base(source, cloneItems) { }
            
            public SyndicationCategory CreateCategoryEntryPoint() => CreateCategory();

            public SyndicationItem CreateItemEntryPoint() => CreateItem();

            public SyndicationLink CreateLinkEntryPoint() => CreateLink();

            public SyndicationPerson CreatePersonEntryPoint() => CreatePerson();

            public bool TryParseAttributeEntryPoint(string name, string ns, string value, string version) => TryParseAttribute(name, ns, value, version);

            public bool TryParseElementEntryPoint(XmlReader reader, string version) => TryParseElement(reader, version);

            public void WriteAttributeExtensionsEntryPoint(XmlWriter writer, string version) => WriteAttributeExtensions(writer, version);

            public void WriteElementExtensionsEntryPoint(XmlWriter writer, string version) => WriteElementExtensions(writer, version);
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}
