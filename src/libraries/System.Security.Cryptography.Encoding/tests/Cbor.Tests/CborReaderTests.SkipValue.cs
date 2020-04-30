﻿// Licensed to the .NET Foundation under one or more agreements.
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
        [MemberData(nameof(SkipTestInputs))]
        public static void SkipValue_RootValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [MemberData(nameof(SkipTestInputs))]
        public static void SkipValue_NestedValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = $"8301{hexEncoding}03".HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadStartArray();
            reader.ReadInt64();
            reader.SkipValue();
            reader.ReadInt64();
            reader.ReadEndArray();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [MemberData(nameof(SkipTestInputs))]
        public static void SkipValue_TaggedValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = $"c2{hexEncoding}".HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadTag();
            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void SkipValue_NotAtValue_ShouldThrowInvalidOperationException()
        {
            byte[] encoding = "80".HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadStartArray();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<InvalidOperationException>(() => reader.SkipValue());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Theory]
        [MemberData(nameof(SkipTestInvalidCborInputs))]
        public static void SkipValue_InvalidFormat_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.SkipValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("61ff")]
        [InlineData("62f090")]
        public static void SkipValue_InvalidUtf8_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            FormatException exn = Assert.Throws<FormatException>(() => reader.SkipValue());
            Assert.NotNull(exn.InnerException);
            Assert.IsType<DecoderFallbackException>(exn.InnerException);

            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void SkipValue_NestedFormatException_ShouldPreserveOriginalReaderState()
        {
            string hexEncoding = "820181bf01ff"; // [1, [ {_ 1 : <missing value> } ]]
            var reader = new CborReader(hexEncoding.HexToByteArray());

            reader.ReadStartArray();
            reader.ReadInt64();

            // capture current state
            int currentBytesRead = reader.BytesRead;
            int currentBytesRemaining = reader.BytesRemaining;

            // make failing call
            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<FormatException>(() => reader.SkipValue());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);

            // ensure reader state has reverted to original
            Assert.Equal(reader.BytesRead, currentBytesRead);
            Assert.Equal(reader.BytesRemaining, currentBytesRemaining);

            // ensure we can read every value up to the format error
            Assert.Equal(CborReaderState.StartArray, reader.PeekState());
            reader.ReadStartArray();
            Assert.Equal(CborReaderState.StartMap, reader.PeekState());
            reader.ReadStartMap();
            Assert.Equal(CborReaderState.UnsignedInteger, reader.PeekState());
            reader.ReadUInt64();
            Assert.Equal(CborReaderState.FormatError, reader.PeekState());
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
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        public static IEnumerable<object[]> SkipTestInputs => SampleCborValues.Select(x => new [] { x });
        public static IEnumerable<object[]> SkipTestInvalidCborInputs => InvalidCborValues.Select(x => new[] { x });
    }
}
