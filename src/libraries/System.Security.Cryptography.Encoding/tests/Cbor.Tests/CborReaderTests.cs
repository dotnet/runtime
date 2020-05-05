// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborReaderTests
    {
        [Fact]
        public static void Peek_EmptyBuffer_ShouldReturnEof()
        {
            var reader = new CborReader(ReadOnlyMemory<byte>.Empty);
            Assert.Equal(CborReaderState.EndOfData, reader.PeekState());
        }

        [Fact]
        public static void BytesRead_NoReads_ShouldReturnZero()
        {
            var reader = new CborReader(new byte[10]);
            Assert.Equal(0, reader.BytesRead);
        }

        [Fact]
        public static void BytesRemaining_NoReads_ShouldReturnBufferSize()
        {
            var reader = new CborReader(new byte[10]);
            Assert.Equal(10, reader.BytesRemaining);
        }


        [Fact]
        public static void BytesRead_SingleRead_ShouldReturnConsumedBytes()
        {
            var reader = new CborReader(new byte[] { 24, 24 });
            reader.ReadInt64();
            Assert.Equal(2, reader.BytesRead);
        }

        [Fact]
        public static void BytesRemaining_SingleRead_ShouldReturnRemainingBytes()
        {
            var reader = new CborReader(new byte[] { 24, 24 });
            reader.ReadInt64();
            Assert.Equal(0, reader.BytesRemaining);
        }

        [Fact]
        public static void ConformanceLevel_DefaultValue_ShouldEqualLax()
        {
            var reader = new CborReader(Array.Empty<byte>());
            Assert.Equal(CborConformanceLevel.Lax, reader.ConformanceLevel);
        }

        [Theory]
        [InlineData(0, CborReaderState.UnsignedInteger)]
        [InlineData(1, CborReaderState.NegativeInteger)]
        [InlineData(2, CborReaderState.ByteString)]
        [InlineData(3, CborReaderState.TextString)]
        [InlineData(4, CborReaderState.StartArray)]
        [InlineData(5, CborReaderState.StartMap)]
        [InlineData(6, CborReaderState.Tag)]
        [InlineData(7, CborReaderState.SpecialValue)]
        public static void Peek_SingleByteBuffer_ShouldReturnExpectedState(byte majorType, CborReaderState expectedResult)
        {
            ReadOnlyMemory<byte> buffer = new byte[] { (byte)(majorType << 5) };
            var reader = new CborReader(buffer);
            Assert.Equal(expectedResult, reader.PeekState());
        }

        [Fact]
        public static void Read_EmptyBuffer_ShouldThrowFormatException()
        {
            var reader = new CborReader(ReadOnlyMemory<byte>.Empty);
            Assert.Throws<FormatException>(() => reader.ReadInt64());
        }

        [Fact]
        public static void Read_BeyondEndOfFirstValue_ShouldThrowInvalidOperationException()
        {
            var reader = new CborReader("01".HexToByteArray());
            reader.ReadInt64();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());
        }

        [Fact]
        public static void CborReader_ReadingTwoRootLevelValues_ShouldThrowInvalidOperationException()
        {
            ReadOnlyMemory<byte> buffer = new byte[] { 0, 0 };
            var reader = new CborReader(buffer);
            reader.ReadInt64();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Equal(CborReaderState.FinishedWithTrailingBytes, reader.PeekState());
            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(1, 2, "0101")]
        [InlineData(10, 10, "0a0a0a0a0a0a0a0a0a0a")]
        [InlineData(new object[] { 1, 2 }, 3, "820102820102820102")]
        public static void CborReader_MultipleRootValuesAllowed_ReadingMultipleValues_HappyPath(object expectedValue, int repetitions, string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray(), allowMultipleRootLevelValues: true);

            for (int i = 0; i < repetitions; i++)
            {
                Helpers.VerifyValue(reader, expectedValue);
            }

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void CborReader_MultipleRootValuesAllowed_ReadingBeyondEndOfBuffer_ShouldThrowInvalidOperationException()
        {
            string hexEncoding = "810102";
            var reader = new CborReader(hexEncoding.HexToByteArray(), allowMultipleRootLevelValues: true);

            Assert.Equal(CborReaderState.StartArray, reader.PeekState());
            reader.ReadStartArray();
            reader.ReadInt32();
            reader.ReadEndArray();

            Assert.Equal(CborReaderState.UnsignedInteger, reader.PeekState());
            reader.ReadInt32();

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.Throws<InvalidOperationException>(() => reader.ReadInt32());
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void ReadEncodedValue_RootValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            byte[] encodedValue = reader.ReadEncodedValue().ToArray();
            Assert.Equal(hexEncoding, encodedValue.ByteArrayToHex().ToLower());
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void ReadEncodedValue_NestedValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = $"8301{hexEncoding}60".HexToByteArray();

            var reader = new CborReader(encoding);

            reader.ReadStartArray();
            reader.ReadInt64();
            byte[] encodedValue = reader.ReadEncodedValue().ToArray();

            Assert.Equal(hexEncoding, encodedValue.ByteArrayToHex().ToLower());
        }

        [Theory]
        [MemberData(nameof(EncodedValueInvalidInputs))]
        public static void ReadEncodedValue_InvalidCbor_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<FormatException>(() => reader.ReadEncodedValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData((CborConformanceLevel)(-1))]
        public static void InvalidConformanceLevel_ShouldThrowArgumentOutOfRangeException(CborConformanceLevel level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CborReader(Array.Empty<byte>(), conformanceLevel: level));
        }

        public static IEnumerable<object[]> EncodedValueInputs => CborReaderTests.SampleCborValues.Select(x => new[] { x });
        public static IEnumerable<object[]> EncodedValueInvalidInputs => CborReaderTests.InvalidCborValues.Select(x => new[] { x });
    }
}
