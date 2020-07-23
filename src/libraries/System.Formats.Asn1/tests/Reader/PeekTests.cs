// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class PeekTests
    {
        [Fact]
        public static void ReaderPeekTag_Valid()
        {
            // SEQUENCE(NULL)
            byte[] data = { 0x30, 0x02, 0x05, 0x00 };
            AsnReader reader = new AsnReader(data, AsnEncodingRules.BER);
            Asn1Tag tag = reader.PeekTag();

            Assert.Equal((int)UniversalTagNumber.Sequence, tag.TagValue);
            Assert.True(tag.IsConstructed, "tag.IsConstructed");
            Assert.Equal(TagClass.Universal, tag.TagClass);
        }

        [Fact]
        public static void ReaderPeekTag_Invalid()
        {
            // (UNIVERSAL [continue into next byte])
            byte[] data = { 0x1F };
            AsnReader reader = new AsnReader(data, AsnEncodingRules.BER);

            Assert.Throws<AsnContentException>(() => reader.PeekTag());
        }

        [Fact]
        public static void PeekEncodedValue_Primitive()
        {
            const string EncodedContents = "010203040506";
            const string EncodedValue = "0406" + EncodedContents;

            byte[] data = (EncodedValue + "0500").HexToByteArray();

            AsnReader reader = new AsnReader(data, AsnEncodingRules.BER);
            Assert.Equal(EncodedValue, reader.PeekEncodedValue().ByteArrayToHex());

            // It's Peek, so it's reproducible.
            Assert.Equal(EncodedValue, reader.PeekEncodedValue().ByteArrayToHex());
        }

        [Fact]
        public static void PeekEncodedValue_Indefinite()
        {
            const string EncodedContents = "040101" + "04050203040506";
            const string EncodedValue = "2480" + EncodedContents + "0000";

            byte[] data = (EncodedValue + "0500").HexToByteArray();

            AsnReader reader = new AsnReader(data, AsnEncodingRules.BER);
            Assert.Equal(EncodedValue, reader.PeekEncodedValue().ByteArrayToHex());

            // It's Peek, so it's reproducible.
            Assert.Equal(EncodedValue, reader.PeekEncodedValue().ByteArrayToHex());
        }

        [Fact]
        public static void PeekEncodedValue_Corrupt_Throws()
        {
            const string EncodedContents = "040101" + "04050203040506";
            // Constructed bit isn't set, so indefinite length is invalid.
            const string EncodedValue = "0480" + EncodedContents + "0000";

            byte[] data = (EncodedValue + "0500").HexToByteArray();

            Assert.Throws<AsnContentException>(
                () =>
                {
                    AsnReader reader = new AsnReader(data, AsnEncodingRules.BER);
                    reader.PeekEncodedValue();
                });
        }

        [Fact]
        public static void PeekContentSpan_Primitive()
        {
            const string EncodedContents = "010203040506";
            const string EncodedValue = "0406" + EncodedContents;

            byte[] data = (EncodedValue + "0500").HexToByteArray();

            AsnReader reader = new AsnReader(data, AsnEncodingRules.BER);
            Assert.Equal(EncodedContents, reader.PeekContentBytes().ByteArrayToHex());

            // It's Peek, so it's reproducible.
            Assert.Equal(EncodedValue, reader.PeekEncodedValue().ByteArrayToHex());
        }

        [Fact]
        public static void PeekContentSpan_Indefinite()
        {
            const string EncodedContents = "040101" + "04050203040506";
            const string EncodedValue = "2480" + EncodedContents + "0000";

            byte[] data = (EncodedValue + "0500").HexToByteArray();

            AsnReader reader = new AsnReader(data, AsnEncodingRules.BER);
            Assert.Equal(EncodedContents, reader.PeekContentBytes().ByteArrayToHex());

            // It's Peek, so it's reproducible.
            Assert.Equal(EncodedValue, reader.PeekEncodedValue().ByteArrayToHex());
        }

        [Fact]
        public static void PeekContentSpan_Corrupt_Throws()
        {
            const string EncodedContents = "040101" + "04050203040506";
            // Constructed bit isn't set, so indefinite length is invalid.
            const string EncodedValue = "0480" + EncodedContents + "0000";

            byte[] data = (EncodedValue + "0500").HexToByteArray();

            Assert.Throws<AsnContentException>(
                () =>
                {
                    AsnReader reader = new AsnReader(data, AsnEncodingRules.BER);
                    reader.PeekContentBytes();
                });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void PeekContentSpan_ExtremelyNested(bool fullArray)
        {
            byte[] dataBytes = new byte[4 * 16384];

            // For a full array this will build 2^14 nested indefinite length values.
            // PeekContentBytes should return dataBytes.Slice(2, dataBytes.Length - 4)
            //
            // For what it's worth, the initial algorithm succeeded at 1650, and StackOverflowed with 1651.
            //
            // With the counter-and-no-recursion algorithm a nesting depth of 534773759 was verified,
            // at a cost of 10 minutes of execution and a 2139095036 byte array.
            // (The size was "a little bit less than int.MaxValue, since that OOMed my 32-bit process")
            int end = dataBytes.Length / 2;
            int expectedLength = dataBytes.Length - 4;

            if (!fullArray)
            {
                // Use 3/4 of what's available, just to prove we're not counting from the end.
                // So with "full" being a nesting value 16384 this will use 12288
                end = end / 4 * 3;
                expectedLength = 2 * end - 4;
            }

            for (int i = 0; i < end; i += 2)
            {
                // Context-Specific 0 [Constructed]
                dataBytes[i] = 0xA0;
                // Indefinite length
                dataBytes[i + 1] = 0x80;
            }

            AsnReader reader = new AsnReader(dataBytes, AsnEncodingRules.BER);
            ReadOnlyMemory<byte> contents = reader.PeekContentBytes();
            Assert.Equal(expectedLength, contents.Length);
            Assert.True(Unsafe.AreSame(ref dataBytes[2], ref MemoryMarshal.GetReference(contents.Span)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void PeekEncodedValue_ExtremelyNested(bool fullArray)
        {
            byte[] dataBytes = new byte[4 * 16384];

            // For a full array this will build 2^14 nested indefinite length values.
            // PeekEncodedValue should return the whole array.
            int end = dataBytes.Length / 2;
            int expectedLength = dataBytes.Length;

            if (!fullArray)
            {
                // Use 3/4 of what's available, just to prove we're not counting from the end.
                // So with "full" being a nesting value 16384 this will use 12288, and
                // PeekEncodedValue should give us back 48k, not 64k.
                end = end / 4 * 3;
                expectedLength = 2 * end;
            }

            for (int i = 0; i < end; i += 2)
            {
                // Context-Specific 0 [Constructed]
                dataBytes[i] = 0xA0;
                // Indefinite length
                dataBytes[i + 1] = 0x80;
            }

            AsnReader reader = new AsnReader(dataBytes, AsnEncodingRules.BER);
            ReadOnlyMemory<byte> contents = reader.PeekEncodedValue();
            Assert.Equal(expectedLength, contents.Length);
            Assert.True(Unsafe.AreSame(ref dataBytes[0], ref MemoryMarshal.GetReference(contents.Span)));

            Assert.True(
                AsnDecoder.TryReadEncodedValue(
                    dataBytes,
                    AsnEncodingRules.BER,
                    out Asn1Tag tag,
                    out int contentOffset,
                    out int contentLength,
                    out int bytesConsumed));

            Assert.Equal(2, contentOffset);
            Assert.Equal(expectedLength - 4, contentLength);
            Assert.Equal(expectedLength, bytesConsumed);
            Assert.Equal(new Asn1Tag(TagClass.ContextSpecific, 0, true), tag);
        }

        [Fact]
        public static void PeekEncodedValue_InvalidLength()
        {
            byte[] badLength = "04040203".HexToByteArray();

            AsnReader reader = new AsnReader(badLength, AsnEncodingRules.BER);

            Assert.Throws<AsnContentException>(() => reader.PeekEncodedValue());
            Assert.Throws<AsnContentException>(() => reader.ReadEncodedValue());

            Assert.False(
                AsnDecoder.TryReadEncodedValue(
                    badLength,
                    AsnEncodingRules.BER,
                    out Asn1Tag tag,
                    out int contentOffset,
                    out int contentLength,
                    out int bytesConsumed));

            Assert.Equal(0, contentOffset);
            Assert.Equal(0, contentLength);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(default(Asn1Tag), tag);
        }

        [Fact]
        public static void PeekContentBytes_InvalidLength()
        {
            byte[] badLength = "04040203".HexToByteArray();

            AsnReader reader = new AsnReader(badLength, AsnEncodingRules.BER);

            Assert.Throws<AsnContentException>(() => reader.PeekContentBytes());
        }
    }
}
