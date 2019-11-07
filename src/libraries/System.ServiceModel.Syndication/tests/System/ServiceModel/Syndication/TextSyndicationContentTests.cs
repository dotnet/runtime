// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class TextSyndicationContentTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("text")]
        public void Ctor_String(string text)
        {
            var content = new TextSyndicationContent(text);
            Assert.Empty(content.AttributeExtensions);
            Assert.Equal(text, content.Text);
            Assert.Equal("text", content.Type);
        }

        [Theory]
        [InlineData(null, TextSyndicationContentKind.Html, "html")]
        [InlineData("", TextSyndicationContentKind.Plaintext, "text")]
        [InlineData("text", TextSyndicationContentKind.XHtml, "xhtml")]
        public void Ctor_String_TextSyndicationContentKind(string text, TextSyndicationContentKind textKind, string type)
        {
            var content = new TextSyndicationContent(text, textKind);
            Assert.Empty(content.AttributeExtensions);
            Assert.Equal(text, content.Text);
            Assert.Equal(type, content.Type);
        }

        [Theory]
        [InlineData(TextSyndicationContentKind.Plaintext - 1)]
        [InlineData(TextSyndicationContentKind.XHtml + 1)]
        public void Ctor_InvalidTextKind_ThrowsArgumentOutOfRangeException(TextSyndicationContentKind textKind)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("textKind", () => new TextSyndicationContent(null, textKind));
        }

        [Fact]
        public void Ctor_TextSyndicationContent_Full()
        {
            var original = new TextSyndicationContent("content", TextSyndicationContentKind.XHtml);
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");

            var clone = new TextSyndicationContentSubclass(original);
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.Equal("content", clone.Text);
            Assert.Equal("xhtml", clone.Type);
        }

        [Fact]
        public void Ctor_TextSyndicationContent_Empty()
        {
            var original = new TextSyndicationContent("content");
            var clone = new TextSyndicationContentSubclass(original);
            Assert.Empty(clone.AttributeExtensions);
            Assert.Equal("content", clone.Text);
            Assert.Equal("text", clone.Type);
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => new TextSyndicationContentSubclass(null));
        }

        [Fact]
        public void Clone_Full_ReturnsExpected()
        {
            var original = new TextSyndicationContent("content", TextSyndicationContentKind.XHtml);
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");

            TextSyndicationContent clone = Assert.IsType<TextSyndicationContent>(original.Clone());
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.Equal("content", clone.Text);
            Assert.Equal("xhtml", clone.Type);
        }

        [Fact]
        public void Clone_Empty_ReturnsExpected()
        {
            var original = new TextSyndicationContent("content");
            TextSyndicationContent clone = Assert.IsType<TextSyndicationContent>(original.Clone());
            Assert.Empty(clone.AttributeExtensions);
            Assert.Equal("content", clone.Text);
            Assert.Equal("text", clone.Type);
        }

        private class TextSyndicationContentSubclass : TextSyndicationContent
        {
            public TextSyndicationContentSubclass(TextSyndicationContent source) : base(source)
            {
            }
        }
    }
}
