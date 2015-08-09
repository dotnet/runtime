// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace StreamReaderTests
{
    public class StreamReader_ctorTests
    {
        [Fact]
        public static void StreamReaderNullPath()
        {
            Assert.Throws<ArgumentNullException>(() => new StreamReader((Stream)null, true));
        }
        [Fact]
        public static void InputStreamClosed()
        {
            var ms2 = new MemoryStream();
            ms2.Dispose();

            Assert.Throws<ArgumentException>(() => new StreamReader(ms2, false));
        }

        [Fact]
        public static void CreationFromMemoryStreamWithEncodingFalse()
        {
            var ms2 = new MemoryStream();
            ms2.Write(new Byte[] { 65, 66, 67, 68 }, 0, 4);
            ms2.Position = 0;
            var sr2 = new StreamReader(ms2, false);

            Assert.Equal("ABCD", sr2.ReadToEnd());
            sr2.Dispose();
        }

        [Fact]
        public static void CreationFromMemoryStreamWithEncodingTrue()
        {
            var ms2 = new MemoryStream();
            ms2.Write(new Byte[] { 65, 66, 67, 68 }, 0, 4);
            ms2.Position = 0;
            var sr2 = new StreamReader(ms2, false);

            Assert.Equal("ABCD", sr2.ReadToEnd());
            sr2.Dispose();
        }
    }
}
