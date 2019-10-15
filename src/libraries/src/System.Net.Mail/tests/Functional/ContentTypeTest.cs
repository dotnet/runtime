// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Net.Mime.Tests
{
    public class ContentTypeTest
    {
        [Fact]
        public static void DefaultCtor_ExpectedDefaultPropertyValues()
        {
            var ct = new ContentType();
            Assert.Null(ct.Boundary);
            Assert.Null(ct.CharSet);
            Assert.Equal("application/octet-stream", ct.MediaType);
            Assert.Empty(ct.Parameters);
            Assert.Null(ct.Name);
            Assert.Equal("application/octet-stream", ct.ToString());
        }

        [Theory]
        [InlineData("text/plain", "text/plain", null, null, null)]
        [InlineData("text/plain; charset=us-ascii", "text/plain", "us-ascii", null, null)]
        [InlineData("text/plain; charset=us-ascii; boundary=hello", "text/plain", "us-ascii", "hello", null)]
        [InlineData("text/plain; boundary=hello; charset=us-ascii; name=world", "text/plain", "us-ascii", "hello", "world")]
        [InlineData("text/plain; charset=us-ascii; name=world", "text/plain", "us-ascii", null, "world")]
        public static void Ctor_ContentString_ParsedValueMatchesExpected(
            string contentType, string expectedMediaType, string expectedCharSet, string expectedBoundary, string expectedName)
        {
            var ct = new ContentType(contentType);
            Assert.Equal(expectedMediaType, ct.MediaType);
            Assert.Equal(expectedCharSet, ct.CharSet);
            Assert.Equal(expectedBoundary, ct.Boundary);
            Assert.Equal(expectedName, ct.Name);

            Assert.Equal(
                (expectedCharSet != null ? 1 : 0) + (expectedBoundary != null ? 1 : 0) + (expectedName != null ? 1 : 0),
                ct.Parameters.Count);
            Assert.Equal(expectedCharSet, ct.Parameters["charset"]);
            Assert.Equal(expectedBoundary, ct.Parameters["boundary"]);
            Assert.Equal(expectedName, ct.Parameters["name"]);
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), null)]
        [InlineData(typeof(ArgumentException), "")]
        [InlineData(typeof(FormatException), "  ")]
        [InlineData(typeof(FormatException), "text/plain\x4F1A")]
        [InlineData(typeof(FormatException), "text/plain ,")]
        [InlineData(typeof(FormatException), "text/plain,")]
        [InlineData(typeof(FormatException), "text/plain; charset=utf-8 ,")]
        [InlineData(typeof(FormatException), "text/plain; charset=utf-8,")]
        [InlineData(typeof(FormatException), "textplain")]
        [InlineData(typeof(IndexOutOfRangeException), "text/")]
        [InlineData(typeof(FormatException), ",, , ,,text/plain; charset=iso-8859-1; q=1.0,\r\n */xml; charset=utf-8; q=0.5,,,")]
        [InlineData(typeof(FormatException), "text/plain; charset=iso-8859-1; q=1.0, */xml; charset=utf-8; q=0.5")]
        [InlineData(typeof(FormatException), " , */xml; charset=utf-8; q=0.5 ")]
        [InlineData(typeof(FormatException), "text/plain; charset=iso-8859-1; q=1.0 , ")]
        public static void Ctor_InvalidContentType_Throws(Type exceptionType, string contentType)
        {
            Assert.Throws(exceptionType, () => new ContentType(contentType));
        }

        [Fact]
        public static void Parameters_Roundtrip()
        {
            var ct = new ContentType();

            Assert.Empty(ct.Parameters);
            Assert.Same(ct.Parameters, ct.Parameters);

            ct.Parameters.Add("hello", "world");
            Assert.Equal("world", ct.Parameters["hello"]);
        }

        [Fact]
        public static void Boundary_Roundtrip()
        {
            var ct = new ContentType();

            Assert.Null(ct.Boundary);
            Assert.Empty(ct.Parameters);

            ct.Boundary = "hello";
            Assert.Equal("hello", ct.Boundary);
            Assert.Equal(1, ct.Parameters.Count);

            ct.Boundary = "world";
            Assert.Equal("world", ct.Boundary);
            Assert.Equal(1, ct.Parameters.Count);

            ct.Boundary = null;
            Assert.Null(ct.Boundary);
            Assert.Empty(ct.Parameters);

            ct.Boundary = string.Empty;
            Assert.Null(ct.Boundary);
            Assert.Empty(ct.Parameters);
        }

        [Fact]
        public static void CharSet_Roundtrip()
        {
            var ct = new ContentType();

            Assert.Null(ct.CharSet);
            Assert.Empty(ct.Parameters);

            ct.CharSet = "hello";
            Assert.Equal("hello", ct.CharSet);
            Assert.Equal(1, ct.Parameters.Count);

            ct.CharSet = "world";
            Assert.Equal("world", ct.CharSet);
            Assert.Equal(1, ct.Parameters.Count);

            ct.CharSet = null;
            Assert.Null(ct.CharSet);
            Assert.Empty(ct.Parameters);

            ct.CharSet = "";
            Assert.Null(ct.CharSet);
            Assert.Empty(ct.Parameters);
        }

        [Fact]
        public static void Name_Roundtrip()
        {
            var ct = new ContentType();

            Assert.Null(ct.Name);
            Assert.Empty(ct.Parameters);

            ct.Name = "hello";
            Assert.Equal("hello", ct.Name);
            Assert.Equal(1, ct.Parameters.Count);

            ct.Name = "world";
            Assert.Equal("world", ct.Name);
            Assert.Equal(1, ct.Parameters.Count);

            ct.Name = null;
            Assert.Null(ct.Name);
            Assert.Empty(ct.Parameters);

            ct.Name = "";
            Assert.Null(ct.Name);
            Assert.Empty(ct.Parameters);
        }

        [Fact]
        public static void MediaType_Set_InvalidArgs_Throws()
        {
            var ct = new ContentType();
            AssertExtensions.Throws<ArgumentNullException>("value", () => ct.MediaType = null);
            AssertExtensions.Throws<ArgumentException>("value", () => ct.MediaType = "");
        }

        [Fact]
        public static void MediaType_Roundtrip()
        {
            var ct = new ContentType("text/plain; charset=us-ascii");
            Assert.Equal("text/plain", ct.MediaType);
            Assert.Equal("us-ascii", ct.CharSet);

            ct.MediaType = "application/xml";
            Assert.Equal("application/xml", ct.MediaType);
            Assert.Equal("us-ascii", ct.CharSet);
        }
    }
}
