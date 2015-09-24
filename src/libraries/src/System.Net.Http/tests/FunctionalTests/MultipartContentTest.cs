// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class MultipartContentTest
    {
        [Fact]
        public void Ctor_NullOrEmptySubType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new MultipartContent(null));
            Assert.Throws<ArgumentException>(() => new MultipartContent(""));
            Assert.Throws<ArgumentException>(() => new MultipartContent(" "));
        }

        [Fact]
        public void Ctor_NullOrEmptyBoundary_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", null));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", ""));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", " "));
        }

        [Fact]
        public void Ctor_TooLongBoundary_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MultipartContent("Some",
                "LongerThan70CharactersLongerThan70CharactersLongerThan70CharactersLongerThan70CharactersLongerThan70Characters"));
        }
        
        [Fact]
        public void Ctor_BadBoundary_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "EndsInSpace "));
            
            // Invalid chars CTLs HT < > @ ; \ " [ ] { } ! # $ % & ^ ~ `
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "a\t"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "<"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "@"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "["));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "{"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "!"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "#"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "$"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "%"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "&"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "^"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "~"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "`"));
            Assert.Throws<ArgumentException>(() => new MultipartContent("Some", "\"quoted\""));
        }

        [Fact]
        public void Ctor_GoodBoundary_Success()
        {
            // RFC 2046 Section 5.1.1
            // boundary := 0*69<bchars> bcharsnospace
            // bchars := bcharsnospace / " "
            // bcharsnospace := DIGIT / ALPHA / "'" / "(" / ")" / "+" / "_" / "," / "-" / "." / "/" / ":" / "=" / "?"
            new MultipartContent("some", "09");
            new MultipartContent("some", "az");
            new MultipartContent("some", "AZ");
            new MultipartContent("some", "'");
            new MultipartContent("some", "(");
            new MultipartContent("some", "+");
            new MultipartContent("some", "_");
            new MultipartContent("some", ",");
            new MultipartContent("some", "-");
            new MultipartContent("some", ".");
            new MultipartContent("some", "/");
            new MultipartContent("some", ":");
            new MultipartContent("some", "=");
            new MultipartContent("some", "?");
            new MultipartContent("some", "Contains Space");
            new MultipartContent("some", " StartsWithSpace");
            new MultipartContent("some", Guid.NewGuid().ToString());
        }

        [Fact]
        public void Ctor_Headers_AutomaticallyCreated()
        {
            var content = new MultipartContent("test_subtype", "test_boundary");
            Assert.Equal("multipart/test_subtype", content.Headers.ContentType.MediaType);
            Assert.Equal(1, content.Headers.ContentType.Parameters.Count);
        }

        [Fact]
        public void Dispose_Empty_Sucess()
        {
            var content = new MultipartContent();
            content.Dispose();
        }

        [Fact]
        public void Dispose_InnerContent_InnerContentDisposed()
        {
            var content = new MultipartContent();
            var innerContent = new MockContent();
            content.Add(innerContent);
            content.Dispose();
            Assert.Equal(1, innerContent.DisposeCount);
            content.Dispose();
            // Inner content is discarded after first dispose.
            Assert.Equal(1, innerContent.DisposeCount);
        }

        [Fact]
        public void Dispose_NestedContent_NestedContentDisposed()
        {
            var outer = new MultipartContent();
            var inner = new MultipartContent();
            outer.Add(inner);
            var mock = new MockContent();
            inner.Add(mock);
            outer.Dispose();
            Assert.Equal(1, mock.DisposeCount);
            outer.Dispose();
            // Inner content is discarded after first dispose.
            Assert.Equal(1, mock.DisposeCount);
        }

        #region Helpers

        private class MockContent : HttpContent
        {
            public int DisposeCount { get; private set; }

            public MockContent() { }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                throw new NotImplementedException();
            }

            protected override bool TryComputeLength(out long length)
            {
                throw new NotImplementedException();
            }
        }

        #endregion Helpers
    }
}
