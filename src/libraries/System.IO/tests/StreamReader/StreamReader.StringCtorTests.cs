// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class StreamReader_StringCtorTests
    {
        [Fact]
        public static void NullArgs_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("path", () => new StreamReader((string)null));
            Assert.Throws<ArgumentNullException>("path", () => new StreamReader((string)null, null));
            Assert.Throws<ArgumentNullException>("path", () => new StreamReader((string)null, null, true));
            Assert.Throws<ArgumentNullException>("path", () => new StreamReader((string)null, null, true, -1));
            Assert.Throws<ArgumentNullException>("encoding", () => new StreamReader("", null));
            Assert.Throws<ArgumentNullException>("encoding", () => new StreamReader("", null, true));
            Assert.Throws<ArgumentNullException>("encoding", () => new StreamReader("", null, true, -1));
        }

        [Fact]
        public static void EmptyPath_ThrowsArgumentException()
        {
            // No argument name for the empty path exception
            Assert.Throws<ArgumentException>(() => new StreamReader(""));
            Assert.Throws<ArgumentException>(() => new StreamReader("", Encoding.UTF8));
            Assert.Throws<ArgumentException>(() => new StreamReader("", Encoding.UTF8, true));
            Assert.Throws<ArgumentException>(() => new StreamReader("", Encoding.UTF8, true, -1));
        }

        [Fact]
        public static void NegativeBufferSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () => new StreamReader("path", Encoding.UTF8, true, -1));
            Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () => new StreamReader("path", Encoding.UTF8, true, 0));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ReadToEnd_detectEncodingFromByteOrderMarks(bool detectEncodingFromByteOrderMarks)
        {
            string testfile = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(testfile, new byte[] { 65, 66, 67, 68 });
                using (var sr2 = new StreamReader(testfile, detectEncodingFromByteOrderMarks))
                {
                    Assert.Equal("ABCD", sr2.ReadToEnd());
                }
            }
            finally
            {
                File.Delete(testfile);
            }
        }
    }
}
