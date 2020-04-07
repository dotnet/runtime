// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborReaderTests
    {
        [Theory]
        [MemberData(nameof(SampleValues))]
        public static void SkipValue_RootValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [MemberData(nameof(SampleValues))]
        public static void SkipValue_NestedValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = $"8301{hexEncoding}03".HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadStartArray();
            reader.ReadInt64();
            reader.SkipValue();
            reader.ReadInt64();
            reader.ReadEndArray();
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [MemberData(nameof(SampleValues))]
        public static void SkipValue_TaggedValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = $"c2{hexEncoding}".HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadTag();
            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Fact]
        public static void SkipValue_NotAtValue_ShouldThrowInvalidOperationException()
        {
            byte[] encoding = "80".HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadStartArray();
            Assert.Throws<InvalidOperationException>(() => reader.SkipValue());
        }

        [Theory]
        [InlineData("")]
        [InlineData("ff")]
        [InlineData("c2")]
        [InlineData("bf01ff")]
        [InlineData("7f01ff")]
        public static void SkipValue_InvalidFormat_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.SkipValue());
        }

        [Theory]
        [InlineData("61ff")]
        [InlineData("62f090")]
        public static void SkipValue_InvalidUtf8_ShouldDecoderFallbackException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<DecoderFallbackException>(() => reader.SkipValue());
        }

        [Theory]
        [InlineData(50_000)]
        public static void SkipValue_ExtremelyNestedValues_ShouldNotStackOverflow(int depth)
        {
            // Construct a valid CBOR encoding with extreme nesting:
            // defines a tower of `depth` nested singleton arrays,
            // with the innermost array containing zero.
            byte[] encoding = new byte[depth + 1];
            encoding.AsSpan(0, depth).Fill(0x81); // array of length 1
            encoding[depth] = 0;

            var reader = new CborReader(encoding);

            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        public static IEnumerable<object[]> SampleValues =>
            new string[]
            {
                // numeric values
                "01",
                "1a000f4240",
                "3affffffff",
                // byte strings
                "40",
                "4401020304",
                "5f41ab40ff",
                // text strings
                "60",
                "6161",
                "6449455446",
                "7f62616260ff",
                // Arrays
                "80",
                "840120604107",
                "8301820203820405",
                "9f182aff",
                // Maps
                "a0",
                "a201020304",
                "a1a1617802182a",
                "bf01020304ff",
                // tagged values
                "c202",
                "d82076687474703a2f2f7777772e6578616d706c652e636f6d",
                // special values
                "f4",
                "f6",
                "fa47c35000",
            }.Select(x => new object[] { x });
    }
}
