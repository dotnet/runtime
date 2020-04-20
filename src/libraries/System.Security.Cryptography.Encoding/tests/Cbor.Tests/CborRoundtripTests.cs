// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Linq;
using Test.Cryptography;
using Xunit;
#if CBOR_PROPERTY_TESTS 
using FsCheck.Xunit;
#endif

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborRoundtripTests
    {

#if CBOR_PROPERTY_TESTS
        private const string ReplaySeed = "(0,0)"; // set a seed for deterministic runs
        private const int MaxTests = 10_000;
#endif

#if CBOR_PROPERTY_TESTS
        [Property(Replay = ReplaySeed, MaxTest = MaxTests)]
#else
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(23)]
        [InlineData(24)]
        [InlineData(25)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1000000)]
        [InlineData(1000000000000)]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(-100)]
        [InlineData(-1000)]
        [InlineData(byte.MaxValue)]
        [InlineData(byte.MaxValue + 1)]
        [InlineData(-1 - byte.MaxValue)]
        [InlineData(-2 - byte.MaxValue)]
        [InlineData(ushort.MaxValue)]
        [InlineData(ushort.MaxValue + 1)]
        [InlineData(-1 - ushort.MaxValue)]
        [InlineData(-2 - ushort.MaxValue)]
        [InlineData(uint.MaxValue)]
        [InlineData((long)uint.MaxValue + 1)]
        [InlineData(-1 - uint.MaxValue)]
        [InlineData(-2 - uint.MaxValue)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
#endif
        public static void Roundtrip_Int64(long input)
        {
            using var writer = new CborWriter();
            writer.WriteInt64(input);
            byte[] encoding = writer.ToArray();

            var reader = new CborReader(encoding);
            long result = reader.ReadInt64();
            Assert.Equal(input, result);
        }

#if CBOR_PROPERTY_TESTS
        [Property(Replay = ReplaySeed, MaxTest = MaxTests)]
#else
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(23)]
        [InlineData(24)]
        [InlineData(25)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1000000)]
        [InlineData(1000000000000)]
        [InlineData(byte.MaxValue)]
        [InlineData(byte.MaxValue + 1)]
        [InlineData(ushort.MaxValue)]
        [InlineData(ushort.MaxValue + 1)]
        [InlineData(uint.MaxValue)]
        [InlineData((long)uint.MaxValue + 1)]
        [InlineData(long.MaxValue)]
        [InlineData(ulong.MaxValue)]
#endif
        public static void Roundtrip_UInt64(ulong input)
        {
            using var writer = new CborWriter();
            writer.WriteUInt64(input);
            byte[] encoding = writer.ToArray();

            var reader = new CborReader(encoding);
            ulong result = reader.ReadUInt64();
            Assert.Equal(input, result);
        }

#if CBOR_PROPERTY_TESTS
        [Property(Replay = ReplaySeed, MaxTest = MaxTests)]
        public static void Roundtrip_ByteString(byte[]? input)
        {
#else
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("01020304")]
        [InlineData("ffffffffffffffffffffffffffff")]
        public static void Roundtrip_ByteString(string? hexInput)
        {
            byte[]? input = hexInput?.HexToByteArray();
#endif
            using var writer = new CborWriter();
            writer.WriteByteString(input);
            byte[] encoding = writer.ToArray();

            var reader = new CborReader(encoding);
            byte[] result = reader.ReadByteString();
            AssertHelper.HexEqual(input ?? Array.Empty<byte>(), result);
        }

#if CBOR_PROPERTY_TESTS
        [Property(Replay = ReplaySeed, MaxTest = MaxTests)]
#else
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("IETF")]
        [InlineData("\"\\")]
        [InlineData("\u00fc")]
        [InlineData("\u6c34")]
        [InlineData("\ud800\udd51")]
#endif
        public static void Roundtrip_TextString(string? input)
        {
            using var writer = new CborWriter();
            writer.WriteTextString(input);
            byte[] encoding = writer.ToArray();

            var reader = new CborReader(encoding);
            string result = reader.ReadTextString();
            Assert.Equal(input ?? "", result);
        }

#if CBOR_PROPERTY_TESTS
        [Property(Replay = ReplaySeed, MaxTest = MaxTests)]
        public static void ByteString_Encoding_ShouldContainInputBytes(byte[]? input)
        {
#else
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("01020304")]
        [InlineData("ffffffffffffffffffffffffffff")]
        public static void ByteString_Encoding_ShouldContainInputBytes(string? hexInput)
        {
            byte[]? input = hexInput?.HexToByteArray();
#endif
            using var writer = new CborWriter();
            writer.WriteByteString(input);
            byte[] encoding = writer.ToArray();

            int length = input?.Length ?? 0;
            int lengthEncodingLength = GetLengthEncodingLength(length);

            Assert.Equal(lengthEncodingLength + length, encoding.Length);
            AssertHelper.HexEqual(input ?? Array.Empty<byte>(), encoding.Skip(lengthEncodingLength).ToArray());

            static int GetLengthEncodingLength(int length)
            {
                return length switch
                {
                    _ when (length < 24) => 1,
                    _ when (length < byte.MaxValue) => 1 + sizeof(byte),
                    _ when (length < ushort.MaxValue) => 1 + sizeof(ushort),
                    _ => 1 + sizeof(uint)
                };
            }
        }
    }
}
