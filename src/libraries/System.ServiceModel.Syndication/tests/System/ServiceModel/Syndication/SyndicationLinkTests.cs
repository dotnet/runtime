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
    public class SyndicationLinkTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var link = new SyndicationLink();
            Assert.Empty(link.AttributeExtensions);
            Assert.Null(link.BaseUri);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(0, link.Length);
            Assert.Null(link.MediaType);
            Assert.Null(link.RelationshipType);
            Assert.Null(link.Title);
            Assert.Null(link.Uri);
        }

        public static IEnumerable<object[]> Ctor_Uri_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Uri("http://microsoft.com") };
            yield return new object[] { new Uri("/relative", UriKind.Relative) };
        }

        public static IEnumerable<object[]> Ctor_Uri_String_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new Uri("http://microsoft.com"), "" };
            yield return new object[] { new Uri("/relative", UriKind.Relative), "mediaType" };
        }

        public static IEnumerable<object[]> Ctor_Uri_String_Long_TestData()
        {
            yield return new object[] { null, null, 0 };
            yield return new object[] { new Uri("http://microsoft.com"), "", 0 };
            yield return new object[] { new Uri("/relative", UriKind.Relative), "mediaType", 10 };
        }

        [Theory]
        [MemberData(nameof(Ctor_Uri_TestData))]
        public void Ctor_Uri(Uri uri)
        {
            var link = new SyndicationLink(uri);
            Assert.Empty(link.AttributeExtensions);
            Assert.Null(link.BaseUri);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(0, link.Length);
            Assert.Null(link.MediaType);
            Assert.Null(link.RelationshipType);
            Assert.Null(link.Title);
            Assert.Same(uri, link.Uri);
        }

        public static IEnumerable<object[]> Ctor_Uri_String_String_String_Long_TestData()
        {
            yield return new object[] { null, null, null, null, 0 };
            yield return new object[] { new Uri("http://microsoft.com"), "", "", "", 0 };
            yield return new object[] { new Uri("/relative", UriKind.Relative), "relationshipType", "title", "mediaType", 10 };
        }

        [Theory]
        [MemberData(nameof(Ctor_Uri_String_String_String_Long_TestData))]
        public void Ctor_Uri_String_String_String_Long(Uri uri, string relationshipType, string title, string mediaType, long length)
        {
            var link = new SyndicationLink(uri, relationshipType, title, mediaType, length);
            Assert.Empty(link.AttributeExtensions);
            Assert.Null(link.BaseUri);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(length, link.Length);
            Assert.Same(mediaType, link.MediaType);
            Assert.Same(relationshipType, link.RelationshipType);
            Assert.Same(title, link.Title);
            Assert.Same(uri, link.Uri);
        }

        [Fact]
        public void Ctor_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new SyndicationLink(null, "", "", "", -1));
        }

        [Fact]
        public void Ctor_SyndicationLink_NoExtensions()
        {
            var link = new SyndicationLink(new Uri("http://microsoft.com"), "relationshipType", "title", "mediaType", 10);
            var clone = new SyndicationLinkSubclass(link);
            Assert.Empty(clone.AttributeExtensions);
            Assert.Null(clone.BaseUri);
            Assert.Empty(clone.ElementExtensions);
            Assert.Equal(link.Length, clone.Length);
            Assert.Same(link.MediaType, clone.MediaType);
            Assert.Same(link.RelationshipType, clone.RelationshipType);
            Assert.Same(link.Title, clone.Title);
            Assert.Same(link.Uri, clone.Uri);
        }

        [Fact]
        public void Ctor_SyndicationLink_Full()
        {
            var original = new SyndicationLink(new Uri("http://microsoft.com"), "relationshipType", "title", "mediaType", 10)
            {
                BaseUri = new Uri("http://baseuri.com")
            };
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");
            original.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            var clone = new SyndicationLinkSubclass(original);
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.Equal(new Uri("http://baseuri.com"), clone.BaseUri);

            Assert.NotSame(clone.ElementExtensions, original.ElementExtensions);
            Assert.Equal(1, clone.ElementExtensions.Count);
            Assert.Equal(10, clone.ElementExtensions[0].GetObject<ExtensionObject>().Value);

            Assert.Equal(10, clone.Length);
            Assert.Equal("mediaType", clone.MediaType);
            Assert.Equal("relationshipType", clone.RelationshipType);
            Assert.Equal("title", clone.Title);
            Assert.Equal(new Uri("http://microsoft.com"), clone.Uri);
        }

        [Fact]
        public void Ctor_SyndicationLink_Empty()
        {
            var original = new SyndicationLink();
            var clone = new SyndicationLinkSubclass(original);
            Assert.Empty(clone.AttributeExtensions);
            Assert.Null(clone.BaseUri);
            Assert.Empty(clone.ElementExtensions);
            Assert.Equal(0, clone.Length);
            Assert.Null(clone.MediaType);
            Assert.Null(clone.RelationshipType);
            Assert.Null(clone.Title);
            Assert.Null(clone.Uri);
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => new SyndicationLinkSubclass(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Length_SetValid_GetReturnsExpected(long length)
        {
            var link = new SyndicationLink
            {
                Length = length
            };
            Assert.Equal(length, link.Length);
        }

        [Fact]
        public void Length_SetNegative_ThrowsArgumentOutOfRangeException()
        {
            var link = new SyndicationLink();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => link.Length = -1);
        }

        [Fact]
        public void Clone_Full_ReturnsExpected()
        {
            var original = new SyndicationLink(new Uri("http://microsoft.com"), "relationshipType", "title", "mediaType", 10)
            {
                BaseUri = new Uri("http://baseuri.com")
            };
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");
            original.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            SyndicationLink clone = original.Clone();
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.Equal(new Uri("http://baseuri.com"), clone.BaseUri);

            Assert.NotSame(clone.ElementExtensions, original.ElementExtensions);
            Assert.Equal(1, clone.ElementExtensions.Count);
            Assert.Equal(10, clone.ElementExtensions[0].GetObject<ExtensionObject>().Value);

            Assert.Equal(10, clone.Length);
            Assert.Equal("mediaType", clone.MediaType);
            Assert.Equal("relationshipType", clone.RelationshipType);
            Assert.Equal("title", clone.Title);
            Assert.Equal(new Uri("http://microsoft.com"), clone.Uri);
        }

        [Fact]
        public void Clone_Empty_ReturnsExpected()
        {
            var original = new SyndicationLink();
            SyndicationLink clone = original.Clone();
            Assert.Empty(clone.AttributeExtensions);
            Assert.Null(clone.BaseUri);
            Assert.Empty(clone.ElementExtensions);
            Assert.Equal(0, clone.Length);
            Assert.Null(clone.MediaType);
            Assert.Null(clone.RelationshipType);
            Assert.Null(clone.Title);
            Assert.Null(clone.Uri);
        }

        [Theory]
        [MemberData(nameof(Ctor_Uri_TestData))]
        public void CreateAlternateLink_Uri_ReturnsExpected(Uri uri)
        {
            SyndicationLink link = SyndicationLink.CreateAlternateLink(uri);
            Assert.Empty(link.AttributeExtensions);
            Assert.Null(link.BaseUri);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(0, link.Length);
            Assert.Null(link.MediaType);
            Assert.Equal("alternate", link.RelationshipType);
            Assert.Null(link.Title);
            Assert.Same(uri, link.Uri);
        }

        [Theory]
        [MemberData(nameof(Ctor_Uri_String_TestData))]
        public void CreateAlternateLink_Uri_String_ReturnsExpected(Uri uri, string mediaType)
        {
            SyndicationLink link = SyndicationLink.CreateAlternateLink(uri, mediaType);
            Assert.Empty(link.AttributeExtensions);
            Assert.Null(link.BaseUri);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(0, link.Length);
            Assert.Same(mediaType, link.MediaType);
            Assert.Equal("alternate", link.RelationshipType);
            Assert.Null(link.Title);
            Assert.Same(uri, link.Uri);
        }

        [Theory]
        [MemberData(nameof(Ctor_Uri_String_Long_TestData))]
        public void CreateMediaEnclosureLink_Invoke_ReturnsExpected(Uri uri, string mediaType, long length)
        {
            SyndicationLink link = SyndicationLink.CreateMediaEnclosureLink(uri, mediaType, length);
            Assert.Empty(link.AttributeExtensions);
            Assert.Null(link.BaseUri);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(length, link.Length);
            Assert.Equal(mediaType, link.MediaType);
            Assert.Equal("enclosure", link.RelationshipType);
            Assert.Null(link.Title);
            Assert.Same(uri, link.Uri);
        }

        [Fact]
        public void CreateMediaEnclosureLink_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => SyndicationLink.CreateMediaEnclosureLink(null, null, -1));
        }

        [Theory]
        [MemberData(nameof(Ctor_Uri_TestData))]
        public void CreateSelfLink_Uri_ReturnsExpected(Uri uri)
        {
            SyndicationLink link = SyndicationLink.CreateSelfLink(uri);
            Assert.Empty(link.AttributeExtensions);
            Assert.Null(link.BaseUri);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(0, link.Length);
            Assert.Null(link.MediaType);
            Assert.Equal("self", link.RelationshipType);
            Assert.Null(link.Title);
            Assert.Same(uri, link.Uri);
        }

        [Theory]
        [MemberData(nameof(Ctor_Uri_String_TestData))]
        public void CreateSelfLink_Uri_String_ReturnsExpected(Uri uri, string mediaType)
        {
            SyndicationLink link = SyndicationLink.CreateSelfLink(uri, mediaType);
            Assert.Empty(link.AttributeExtensions);
            Assert.Null(link.BaseUri);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(0, link.Length);
            Assert.Equal(mediaType, link.MediaType);
            Assert.Equal("self", link.RelationshipType);
            Assert.Null(link.Title);
            Assert.Same(uri, link.Uri);
        }

        public static IEnumerable<object[]> GetAbsoluteUri_TestData()
        {
            yield return new object[] { new SyndicationLink(null), null };
            yield return new object[] { new SyndicationLink(new Uri("http://microsoft.com")), new Uri("http://microsoft.com") };
            yield return new object[] { new SyndicationLink(new Uri("/relative", UriKind.Relative)), null };
            yield return new object[] { new SyndicationLink(new Uri("/relative", UriKind.Relative)) { BaseUri = new Uri("http://microsoft.com") }, new Uri("http://microsoft.com/relative") };
        }

        [Theory]
        [MemberData(nameof(GetAbsoluteUri_TestData))]
        public void GetAbsoluteUri_Invoke_ReturnsExpected(SyndicationLink link, Uri expected)
        {
            Assert.Equal(expected, link.GetAbsoluteUri());
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
            var link = new SyndicationLinkSubclass();
            Assert.False(link.TryParseAttributeEntryPoint(name, ns, value, version));
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
            var link = new SyndicationLinkSubclass();
            Assert.False(link.TryParseElementEntryPoint(reader, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteAttributeExtensions_Invoke_ReturnsExpected(string version)
        {
            var link = new SyndicationLinkSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => link.WriteAttributeExtensionsEntryPoint(writer, version));

            link.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            link.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            link.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => link.WriteAttributeExtensionsEntryPoint(writer, "version"));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var link = new SyndicationLinkSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => link.WriteAttributeExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteElementExtensions_Invoke_ReturnsExpected(string version)
        {
            var link = new SyndicationLinkSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => link.WriteElementExtensionsEntryPoint(writer, version));

            link.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            link.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<SyndicationLinkTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</SyndicationLinkTests.ExtensionObject>
<SyndicationLinkTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</SyndicationLinkTests.ExtensionObject>", writer => link.WriteElementExtensionsEntryPoint(writer, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var link = new SyndicationLinkSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => link.WriteElementExtensionsEntryPoint(null, "version"));
        }

        public class SyndicationLinkSubclass : SyndicationLink
        {
            public SyndicationLinkSubclass() : base() { }

            public SyndicationLinkSubclass(SyndicationLink source) : base(source) { }

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
