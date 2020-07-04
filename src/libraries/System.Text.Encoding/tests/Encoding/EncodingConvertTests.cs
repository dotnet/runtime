// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class EncodingConvertTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("ASCIIString")]
        [InlineData("a\u1234b")]
        [InlineData("\uD800\uDC00")]
        public void Convert(string source)
        {
            static void convert(Encoding srcEncoding, Encoding dstEncoding, byte[] bytes, int index, int count, byte[] expected)
            {
                if (index == 0 && count == bytes.Length)
                {
                    Assert.Equal(expected, Encoding.Convert(srcEncoding, dstEncoding, bytes));
                }
                Assert.Equal(expected, Encoding.Convert(srcEncoding, dstEncoding, bytes, index, count));
            }

            Encoding[] encodings = new Encoding[] { Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode };
            foreach (Encoding srcEncoding in encodings)
            {
                foreach (Encoding dstEncoding in encodings)
                {
                    byte[] bytes = srcEncoding.GetBytes(source);
                    convert(srcEncoding, dstEncoding, bytes, 0, bytes.Length, dstEncoding.GetBytes(source));
                    convert(srcEncoding, dstEncoding, bytes, 0, 0, new byte[0]);
                }
            }
        }

        [Fact]
        public void Convert_Invalid()
        {
            // Bytes is null
            AssertExtensions.Throws<ArgumentNullException>("bytes", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, null));
            AssertExtensions.Throws<ArgumentNullException>("bytes", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, null, 0, 0));

            // SrcEncoding is null
            AssertExtensions.Throws<ArgumentNullException>("srcEncoding", () => Encoding.Convert(null, Encoding.ASCII, new byte[0]));
            AssertExtensions.Throws<ArgumentNullException>("srcEncoding", () => Encoding.Convert(null, Encoding.ASCII, new byte[0], 0, 0));

            // DstEncoding is null
            AssertExtensions.Throws<ArgumentNullException>("dstEncoding", () => Encoding.Convert(Encoding.ASCII, null, new byte[0]));
            AssertExtensions.Throws<ArgumentNullException>("dstEncoding", () => Encoding.Convert(Encoding.ASCII, null, new byte[0], 0, 0));

            // Invalid index
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, new byte[10], -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bytes", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, new byte[10], 11, 0));

            // Invalid count
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, new byte[10], 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bytes", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, new byte[10], 0, 11));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bytes", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, new byte[10], 1, 10));
        }
    }
}
