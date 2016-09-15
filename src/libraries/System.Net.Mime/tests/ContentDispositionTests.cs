// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Mime.Tests
{
    public class ContentDispositionTests
    {
        [Fact]
        public static void DefaultCtor_ExpectedDefaultPropertyValues()
        {
            var cd = new ContentDisposition();
            Assert.Equal(DateTime.MinValue, cd.CreationDate);
            Assert.Equal("attachment", cd.DispositionType);
            Assert.Null(cd.FileName);
            Assert.False(cd.Inline);
            Assert.Equal(DateTime.MinValue, cd.ModificationDate);
            Assert.Empty(cd.Parameters);
            Assert.Equal(DateTime.MinValue, cd.ReadDate);
            Assert.Equal(-1, cd.Size);
            Assert.Equal("attachment", cd.ToString());
        }

        [Theory]
        [InlineData("inline", "inline")]
        public static void Ctor_ContentDisposition_ParsedValueMatchesExpected(string contentDisposition, string expectedDispositionType)
        {
            var cd = new ContentDisposition(contentDisposition);
            Assert.Equal(expectedDispositionType, cd.DispositionType);
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), null)]
        public static void Ctor_InvalidContentDisposition_Throws(Type exceptionType, string contentDisposition)
        {
            Assert.Throws(exceptionType, () => new ContentDisposition(contentDisposition));
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), null)]
        [InlineData(typeof(ArgumentException), "")]
        public static void Property_InvalidContentDisposition_Throws(Type exceptionType, string contentDisposition)
        {
            Assert.Throws(exceptionType, () => new ContentDisposition().DispositionType = contentDisposition);
        }

        [Fact]
        public static void Filename_Roundtrip()
        {
            var cd = new ContentDisposition();

            Assert.Null(cd.FileName);
            Assert.Empty(cd.Parameters);

            cd.FileName = "hello";
            Assert.Equal("hello", cd.FileName);
            Assert.Equal(1, cd.Parameters.Count);
            Assert.Equal("hello", cd.Parameters["filename"]);
            Assert.Equal("attachment; filename=hello", cd.ToString());

            cd.FileName = "world";
            Assert.Equal("world", cd.FileName);
            Assert.Equal(1, cd.Parameters.Count);
            Assert.Equal("world", cd.Parameters["filename"]);
            Assert.Equal("attachment; filename=world", cd.ToString());

            cd.FileName = null;
            Assert.Null(cd.FileName);
            Assert.Empty(cd.Parameters);

            cd.FileName = string.Empty;
            Assert.Null(cd.FileName);
            Assert.Empty(cd.Parameters);
        }

        [Fact]
        public static void Inline_Roundtrip()
        {
            var cd = new ContentDisposition();
            Assert.False(cd.Inline);

            cd.Inline = true;
            Assert.True(cd.Inline);

            cd.Inline = false;
            Assert.False(cd.Inline);

            Assert.Empty(cd.Parameters);
        }

        [Fact]
        public static void Dates_RoundtripWithoutImpactingOtherDates()
        {
            var cd = new ContentDisposition();

            Assert.Equal(DateTime.MinValue, cd.CreationDate);
            Assert.Equal(DateTime.MinValue, cd.ModificationDate);
            Assert.Equal(DateTime.MinValue, cd.ReadDate);
            Assert.Empty(cd.Parameters);

            DateTime dt1 = DateTime.Now;
            cd.CreationDate = dt1;
            Assert.Equal(1, cd.Parameters.Count);

            DateTime dt2 = DateTime.Now;
            cd.ModificationDate = dt2;
            Assert.Equal(2, cd.Parameters.Count);

            DateTime dt3 = DateTime.Now;
            cd.ReadDate = dt3;
            Assert.Equal(3, cd.Parameters.Count);

            Assert.Equal(dt1, cd.CreationDate);
            Assert.Equal(dt2, cd.ModificationDate);
            Assert.Equal(dt3, cd.ReadDate);

            Assert.Equal(3, cd.Parameters.Count);
        }

        [Fact]
        public static void DispositionType_Roundtrip()
        {
            var cd = new ContentDisposition();

            Assert.Equal("attachment", cd.DispositionType);
            Assert.Empty(cd.Parameters);

            cd.DispositionType = "hello";
            Assert.Equal("hello", cd.DispositionType);

            cd.DispositionType = "world";
            Assert.Equal("world", cd.DispositionType);

            Assert.Equal(0, cd.Parameters.Count);
        }

        [Fact]
        public static void Size_Roundtrip()
        {
            var cd = new ContentDisposition();

            Assert.Equal(-1, cd.Size);
            Assert.Empty(cd.Parameters);

            cd.Size = 42;
            Assert.Equal(42, cd.Size);
            Assert.Equal(1, cd.Parameters.Count);
        }
    }
}
