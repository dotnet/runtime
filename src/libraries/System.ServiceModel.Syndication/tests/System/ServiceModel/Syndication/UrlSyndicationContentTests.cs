// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class UrlSyndicationContentTests
    {
        public static IEnumerable<object[]> Ctor_Uri_String_TestData()
        {
            yield return new object[] { new Uri("http://microsoft.com"), null };
            yield return new object[] { new Uri("/relative", UriKind.Relative), "" };
            yield return new object[] { new Uri("http://microsoft.com"), "mediaType" };
        }

        [Theory]
        [MemberData(nameof(Ctor_Uri_String_TestData))]
        public void Ctor_Uri_String(Uri url, string mediaType)
        {
            var content = new UrlSyndicationContent(url, mediaType);
            Assert.Empty(content.AttributeExtensions);
            Assert.Equal(url, content.Url);
            Assert.Equal(mediaType, content.Type);
        }

        [Fact]
        public void Ctor_NullUrl_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("url", () => new UrlSyndicationContent(null, "mediaType"));
        }

        [Fact]
        public void Ctor_UrlSyndicationContent_Full()
        {
            var original = new UrlSyndicationContent(new Uri("http://microsoft.com"), "mediaType");
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");

            var clone = new UrlSyndicationContentSubclass(original);
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.Equal("mediaType", clone.Type);
            Assert.Equal(new Uri("http://microsoft.com"), clone.Url);
        }

        [Fact]
        public void Ctor_UrlSyndicationContent_Empty()
        {
            var original = new UrlSyndicationContent(new Uri("http://microsoft.com"), "mediaType");
            var clone = new UrlSyndicationContentSubclass(original);
            Assert.Empty(clone.AttributeExtensions);
            Assert.Equal("mediaType", clone.Type);
            Assert.Equal(new Uri("http://microsoft.com"), clone.Url);
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => new UrlSyndicationContentSubclass(null));
        }

        [Fact]
        public void Clone_Full_ReturnsExpected()
        {
            var original = new UrlSyndicationContent(new Uri("http://microsoft.com"), "mediaType");
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");

            UrlSyndicationContent clone = Assert.IsType<UrlSyndicationContent>(original.Clone());
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.Equal("mediaType", clone.Type);
            Assert.Equal(new Uri("http://microsoft.com"), clone.Url);
        }

        [Fact]
        public void Clone_Empty_ReturnsExpected()
        {
            var original = new UrlSyndicationContent(new Uri("http://microsoft.com"), "mediaType");
            UrlSyndicationContent clone = Assert.IsType<UrlSyndicationContent>(original.Clone());
            Assert.Empty(clone.AttributeExtensions);
            Assert.Equal("mediaType", clone.Type);
            Assert.Equal(new Uri("http://microsoft.com"), clone.Url);
        }

        private class UrlSyndicationContentSubclass : UrlSyndicationContent
        {
            public UrlSyndicationContentSubclass(UrlSyndicationContent source) : base(source) { }
        }
    }
}
