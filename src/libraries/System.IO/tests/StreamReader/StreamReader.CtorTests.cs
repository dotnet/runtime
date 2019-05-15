// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace System.IO.Tests
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

            AssertExtensions.Throws<ArgumentException>(null, () => new StreamReader(ms2, false));
        }

        [Fact]
        public static void CreationFromMemoryStreamWithEncodingFalse()
        {
            var ms2 = new MemoryStream();
            ms2.Write(new byte[] { 65, 66, 67, 68 }, 0, 4);
            ms2.Position = 0;
            var sr2 = new StreamReader(ms2, false);

            Assert.Equal("ABCD", sr2.ReadToEnd());
            sr2.Dispose();
        }

        [Fact]
        public static void CreationFromMemoryStreamWithEncodingTrue()
        {
            var ms2 = new MemoryStream();
            ms2.Write(new byte[] { 65, 66, 67, 68 }, 0, 4);
            ms2.Position = 0;
            var sr2 = new StreamReader(ms2, false);

            Assert.Equal("ABCD", sr2.ReadToEnd());
            sr2.Dispose();
        }
    }
}
