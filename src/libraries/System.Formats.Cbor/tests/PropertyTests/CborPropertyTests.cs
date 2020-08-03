// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor.Tests.DataModel;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public static class CborPropertyTests
    {
        private const string? ReplaySeed = "(42,42)"; // set a seed for deterministic runs, null for randomized runs
        private const int MaxTests = 10_000;

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void Roundtrip_Int64(CborConformanceMode mode, long input)
        {
            var writer = new CborWriter(mode);
            writer.WriteInt64(input);
            byte[] encoding = writer.Encode();

            var reader = new CborReader(encoding, mode);
            long result = reader.ReadInt64();
            Assert.Equal(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void Roundtrip_UInt64(CborConformanceMode mode, ulong input)
        {
            var writer = new CborWriter(mode);
            writer.WriteUInt64(input);
            byte[] encoding = writer.Encode();

            var reader = new CborReader(encoding, mode);
            ulong result = reader.ReadUInt64();
            Assert.Equal(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void Roundtrip_NegativeInteger(CborConformanceMode mode, ulong input)
        {
            var writer = new CborWriter(mode);
            writer.WriteCborNegativeIntegerRepresentation(input);
            byte[] encoding = writer.Encode();

            var reader = new CborReader(encoding, mode);
            ulong result = reader.ReadCborNegativeIntegerRepresentation();
            Assert.Equal(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void Roundtrip_ByteString(CborConformanceMode mode, byte[] input)
        {
            var writer = new CborWriter(mode);
            writer.WriteByteString(input);
            byte[] encoding = writer.Encode();

            var reader = new CborReader(encoding, mode);
            byte[] result = reader.ReadByteString();
            AssertHelper.HexEqual(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void Roundtrip_TextString(CborConformanceMode mode, string input)
        {
            var writer = new CborWriter(mode);
            writer.WriteTextString(input);
            byte[] encoding = writer.Encode();

            var reader = new CborReader(encoding, mode);
            string result = reader.ReadTextString();
            Assert.Equal(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void Roundtrip_IndefiniteByteString(CborConformanceMode mode, byte[][] chunks)
        {
            bool convertIndefiniteLengthEncodings = mode is CborConformanceMode.Canonical or CborConformanceMode.Ctap2Canonical;
            var writer = new CborWriter(convertIndefiniteLengthEncodings: convertIndefiniteLengthEncodings);

            writer.WriteStartIndefiniteLengthByteString();
            foreach (byte[] chunk in chunks)
            {
                writer.WriteByteString(chunk);
            }
            writer.WriteEndIndefiniteLengthByteString();

            byte[] encoding = writer.Encode();

            var reader = new CborReader(encoding);
            byte[] expected = chunks.SelectMany(ch => ch).ToArray();
            byte[] result = reader.ReadByteString();
            AssertHelper.HexEqual(expected, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void Roundtrip_IndefiniteTextString(CborConformanceMode mode, string[] chunks)
        {
            bool convertIndefiniteLengthEncodings = mode is CborConformanceMode.Canonical or CborConformanceMode.Ctap2Canonical;
            var writer = new CborWriter(convertIndefiniteLengthEncodings: convertIndefiniteLengthEncodings);

            writer.WriteStartIndefiniteLengthTextString();
            foreach (string chunk in chunks)
            {
                writer.WriteTextString(chunk);
            }
            writer.WriteEndIndefiniteLengthTextString();

            byte[] encoding = writer.Encode();

            var reader = new CborReader(encoding);
            string expected = String.Concat(chunks);
            string result = reader.ReadTextString();
            Assert.Equal(expected, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void Roundtrip_Half(CborConformanceMode mode, Half input)
        {
            var writer = new CborWriter(mode);
            writer.WriteHalf(input);
            byte[] encoding = writer.Encode();

            var reader = new CborReader(encoding, mode);
            Half result = reader.ReadHalf();
            Assert.Equal(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void Roundtrip_Double(CborConformanceMode mode, double input)
        {
            var writer = new CborWriter();
            writer.WriteDouble(input);
            byte[] encoding = writer.Encode();

            var reader = new CborReader(encoding);
            double result = reader.ReadDouble();
            Assert.Equal(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void Roundtrip_Decimal(CborConformanceMode mode, decimal input)
        {
            var writer = new CborWriter();
            writer.WriteDecimal(input);
            byte[] encoding = writer.Encode();

            var reader = new CborReader(encoding);
            decimal result = reader.ReadDecimal();
            Assert.Equal(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void ByteString_Encoding_ShouldContainInputBytes(CborConformanceMode mode, byte[] input)
        {
            var writer = new CborWriter(mode);
            writer.WriteByteString(input);
            byte[] encoding = writer.Encode();

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

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void PropertyTest_Roundtrip(CborPropertyTestContext input)
        {
            byte[] encoding = CborDocumentSerializer.encode(input);

            CborDocument[] expectedResults = CborPropertyTestContextHelper.getExpectedRoundtripValues(input);
            CborDocument[] roundtrippedDocuments = CborDocumentSerializer.decode(input, encoding);
            Assert.Equal(expectedResults, roundtrippedDocuments);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void PropertyTest_SkipValue(CborPropertyTestContext input)
        {
            int length = input.RootDocuments.Length;
            input.RootDocuments = new[] { CborDocument.NewArray(_isDefiniteLength: true, input.RootDocuments) };
            byte[] encoding = CborDocumentSerializer.encode(input);

            CborReader reader = CborDocumentSerializer.createReader(input, encoding);
            reader.ReadStartArray();
            for (int i = 0; i < length; i++)
            {
                reader.SkipValue();
            }
            reader.ReadEndArray();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests, Arbitrary = new[] { typeof(CborRandomGenerators) })]
        public static void PropertyTest_SkipToParent(CborPropertyTestContext input)
        {
            input.RootDocuments = new[] { CborDocument.NewArray(_isDefiniteLength: true, input.RootDocuments) };
            byte[] encoding = CborDocumentSerializer.encode(input);

            CborReader reader = CborDocumentSerializer.createReader(input, encoding);
            reader.ReadStartArray();
            reader.SkipToParent();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }
    }
}
