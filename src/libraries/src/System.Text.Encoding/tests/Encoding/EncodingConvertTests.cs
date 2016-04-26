// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Encoding[] encodings = new Encoding[] { Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode };
            foreach (Encoding srcEncoding in encodings)
            {
                foreach (Encoding dstEncoding in encodings)
                {
                    byte[] bytes = srcEncoding.GetBytes(source);
                    Convert(srcEncoding, dstEncoding, bytes, 0, bytes.Length, dstEncoding.GetBytes(source));
                    Convert(srcEncoding, dstEncoding, bytes, 0, 0, new byte[0]);
                }
            }
        }

        public void Convert(Encoding srcEncoding, Encoding dstEncoding, byte[] bytes, int index, int count, byte[] expected)
        {
            if (index == 0 && count == bytes.Length)
            {
                Assert.Equal(expected, Encoding.Convert(srcEncoding, dstEncoding, bytes));
            }
            Assert.Equal(expected, Encoding.Convert(srcEncoding, dstEncoding, bytes, index, count));
        }

        [Fact]
        public void Convert_Invalid()
        {
            // Bytes is null
            Assert.Throws<ArgumentNullException>("bytes", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, null));
            Assert.Throws<ArgumentNullException>("bytes", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, null, 0, 0));

            // SrcEncoding is null
            Assert.Throws<ArgumentNullException>("srcEncoding", () => Encoding.Convert(null, Encoding.ASCII, new byte[0]));
            Assert.Throws<ArgumentNullException>("srcEncoding", () => Encoding.Convert(null, Encoding.ASCII, new byte[0], 0, 0));

            // DstEncoding is null
            Assert.Throws<ArgumentNullException>("dstEncoding", () => Encoding.Convert(Encoding.ASCII, null, new byte[0]));
            Assert.Throws<ArgumentNullException>("dstEncoding", () => Encoding.Convert(Encoding.ASCII, null, new byte[0], 0, 0));

            // Invalid index
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, new byte[10], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, new byte[10], 11, 0));

            // Invalid count
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, new byte[10], 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, new byte[10], 0, 11));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => Encoding.Convert(Encoding.ASCII, Encoding.ASCII, new byte[10], 1, 10));
        }
    }
}
