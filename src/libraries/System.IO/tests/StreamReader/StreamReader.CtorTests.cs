// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var sr2 = new StreamReader(ms2, true);

            Assert.Equal("ABCD", sr2.ReadToEnd());
            sr2.Dispose();
        }

        public static IEnumerable<object[]> EncodingsWithEncodedDataWithPreamble()
        {
            var stringInputs = new[] { string.Empty, "ABCD" };
            var encodingInputs = new[] { Encoding.BigEndianUnicode, Encoding.Unicode, Encoding.UTF8, Encoding.UTF32, new UTF32Encoding(bigEndian: true, byteOrderMark: true) };

            foreach (var encoding in encodingInputs)
            {
                foreach (var stringInput in stringInputs)
                {
                    yield return new object[] { encoding, GetEncodedDataWithPreamble(encoding.GetBytes(stringInput), encoding), stringInput };
                }
            }

            byte[] GetEncodedDataWithPreamble(byte[] data, Encoding encoding)
                => encoding.GetPreamble().Concat(data).ToArray();
        }

        [Theory]
        [MemberData(nameof(EncodingsWithEncodedDataWithPreamble))]
        public static void CreationFromMemoryStreamWithEncodingTrueDetectsCorrectEncodingWhenPreambleAvailable(Encoding expectedEncoding, byte[] encodedData, string expectedOutput)
        {
            var ms2 = new MemoryStream();
            ms2.Write(encodedData, 0, encodedData.Length);
            ms2.Position = 0;
            var sr2 = new StreamReader(ms2, true);
            var streamContent = sr2.ReadToEnd();

            Assert.Equal(expectedOutput, streamContent);
            Assert.Equal(expectedEncoding, sr2.CurrentEncoding);
            sr2.Dispose();
        }

        public static IEnumerable<object[]> EncodingsWithSlightlyOffPatterns()
        {
            yield return new object[] { Encoding.Unicode, new byte[] { 0xFF, 0xFE, 0x00, 0xBB } }; // Possible UTF-32LE, first 2 bytes are the same
            yield return new object[] { Encoding.Unicode, new byte[] { 0xFF, 0xFE, 0xBB, 0x00 } }; // Possible UTF-32LE, first 2 bytes are the same
            yield return new object[] { Encoding.Unicode, new byte[] { 0xFF, 0xFE, 0xFE, 0xFF } }; // Possible UTF-32LE and UTF-32BE, first (last) 2 bytes are the same
            yield return new object[] { Encoding.BigEndianUnicode, new byte[] { 0xFE, 0xFF, 0x00, 0x00 } }; // Possible UTF-32LE, last 2 bytes are the same
            yield return new object[] { Encoding.BigEndianUnicode, new byte[] { 0xFE, 0xFF, 0x00, 0x00, 0xFE, 0xFF } }; // Possible UTF-32BE, last 4 bytes are the preamble of UTF-32BE
            yield return new object[] { Encoding.BigEndianUnicode, new byte[] { 0xFE, 0xFF, 0xFE, 0xFF } }; // Possible UTF-32BE, last 2 bytes are the same
            yield return new object[] { Encoding.UTF32, new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0xFE, 0xFF } }; // Possible UTF-32BE, last 4 bytes are the preamble of UTF-32BE
            yield return new object[] { Encoding.UTF32, new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0xEF, 0xBB, 0xBF } }; // Possible UTF-8, last 3 bytes are the preamble of UTF-8
            yield return new object[] { new UTF32Encoding(bigEndian: true, byteOrderMark: true), new byte[] { 0x00, 0x00, 0xFE, 0xFF } }; // Possible UnicodeBE, last 2 bytes are UnicodeBE
        }

        [Theory]
        [MemberData(nameof(EncodingsWithSlightlyOffPatterns))]
        public static void CreationFromMemoryStreamWithEncodingTrueDetectsCorrectEncodingWithOffPatterns(Encoding expectedEncoding, byte[] encodedData)
        {
            var ms2 = new MemoryStream();
            ms2.Write(encodedData, 0, encodedData.Length);
            ms2.Position = 0;
            var sr2 = new StreamReader(ms2, true);
            var streamContent = sr2.ReadToEnd();

            Assert.Equal(expectedEncoding, sr2.CurrentEncoding);
            sr2.Dispose();
        }

        public static IEnumerable<object[]> EncodingsWithSmallInputs()
        {
            var byteArrayInputs = new byte[][] { new byte[] { }, new byte[] { 0x00 }, new byte[] { 0xEF }, new byte[] { 0xFE }, new byte[] { 0xFF } };
            var encodingInputs = new[] { Encoding.BigEndianUnicode, Encoding.Unicode, Encoding.UTF8, Encoding.UTF32, new UTF32Encoding(bigEndian: true, byteOrderMark: true) };

            foreach (var encoding in encodingInputs)
            {
                foreach (var byteArrayInput in byteArrayInputs)
                {
                    yield return new object[] { encoding, byteArrayInput };
                }
            }
        }

        [Theory]
        [MemberData(nameof(EncodingsWithSmallInputs))]
        public static void CreationFromMemoryStreamWithEncodingTrueDoesNotFailOnSmallInputs(Encoding expectedEncoding, byte[] data)
        {
            var ms2 = new MemoryStream();
            ms2.Write(data, 0, data.Length);
            ms2.Position = 0;
            var sr2 = new StreamReader(ms2, encoding: expectedEncoding, detectEncodingFromByteOrderMarks: true);
            var streamContent = sr2.ReadToEnd();

            Assert.Equal(expectedEncoding, sr2.CurrentEncoding);
            sr2.Dispose();
        }
    }
}
