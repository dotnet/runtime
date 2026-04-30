// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor.Tests.DataModel;
using System.Linq;
using FsCheck;
using FsCheck.Fluent;
using Microsoft.FSharp.Core;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public static class CborPropertyTests
    {
        private const int MaxTests = 100;

        private static readonly IArbMap s_arbMap = ArbMap.Default.Merge<CborRandomGenerators>();

        private static Config CreateConfig()
        {
            return Config.QuickThrowOnFailure
                .WithMaxTest(MaxTests);
        }

        private static void CheckProperty<T>(Action<T> test)
        {
            var arb = s_arbMap.ArbFor<T>();
            var prop = FsCheck.FSharp.Prop.ForAll(arb, FuncConvert.FromAction(test));
            Check.One(CreateConfig(), prop);
        }

        private static void CheckProperty<T1, T2>(Action<T1, T2> test)
        {
            var arb1 = s_arbMap.ArbFor<T1>();
            var arb2 = s_arbMap.ArbFor<T2>();
            var prop = FsCheck.FSharp.Prop.ForAll(arb1,
                FuncConvert.FromFunc<T1, Property>(a =>
                    FsCheck.FSharp.Prop.ForAll(arb2,
                        FuncConvert.FromAction<T2>(b => test(a, b)))));
            Check.One(CreateConfig(), prop);
        }

        [Fact]
        public static void Roundtrip_Int64()
        {
            CheckProperty<CborConformanceMode, long>((mode, input) =>
            {
                var writer = new CborWriter(mode);
                writer.WriteInt64(input);
                byte[] encoding = writer.Encode();

                var reader = new CborReader(encoding, mode);
                long result = reader.ReadInt64();
                Assert.Equal(input, result);
            });
        }

        [Fact]
        public static void Roundtrip_UInt64()
        {
            CheckProperty<CborConformanceMode, ulong>((mode, input) =>
            {
                var writer = new CborWriter(mode);
                writer.WriteUInt64(input);
                byte[] encoding = writer.Encode();

                var reader = new CborReader(encoding, mode);
                ulong result = reader.ReadUInt64();
                Assert.Equal(input, result);
            });
        }

        [Fact]
        public static void Roundtrip_NegativeInteger()
        {
            CheckProperty<CborConformanceMode, ulong>((mode, input) =>
            {
                var writer = new CborWriter(mode);
                writer.WriteCborNegativeIntegerRepresentation(input);
                byte[] encoding = writer.Encode();

                var reader = new CborReader(encoding, mode);
                ulong result = reader.ReadCborNegativeIntegerRepresentation();
                Assert.Equal(input, result);
            });
        }

        [Fact]
        public static void Roundtrip_ByteString()
        {
            CheckProperty<CborConformanceMode, byte[]>((mode, input) =>
            {
                var writer = new CborWriter(mode);
                writer.WriteByteString(input);
                byte[] encoding = writer.Encode();

                var reader = new CborReader(encoding, mode);
                byte[] result = reader.ReadByteString();
                AssertHelper.HexEqual(input, result);
            });
        }

        [Fact]
        public static void Roundtrip_TextString()
        {
            CheckProperty<CborConformanceMode, string>((mode, input) =>
            {
                var writer = new CborWriter(mode);
                writer.WriteTextString(input);
                byte[] encoding = writer.Encode();

                var reader = new CborReader(encoding, mode);
                string result = reader.ReadTextString();
                Assert.Equal(input, result);
            });
        }

        [Fact]
        public static void Roundtrip_IndefiniteByteString()
        {
            CheckProperty<CborConformanceMode, byte[][]>((mode, chunks) =>
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
            });
        }

        [Fact]
        public static void Roundtrip_IndefiniteTextString()
        {
            CheckProperty<CborConformanceMode, string[]>((mode, chunks) =>
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
            });
        }

        [Fact]
        public static void Roundtrip_Half()
        {
            CheckProperty<CborConformanceMode, Half>((mode, input) =>
            {
                var writer = new CborWriter(mode);
                writer.WriteHalf(input);
                byte[] encoding = writer.Encode();

                var reader = new CborReader(encoding, mode);
                Half result = reader.ReadHalf();
                Assert.Equal(input, result);
            });
        }

        [Fact]
        public static void Roundtrip_Double()
        {
            CheckProperty<CborConformanceMode, double>((mode, input) =>
            {
                var writer = new CborWriter();
                writer.WriteDouble(input);
                byte[] encoding = writer.Encode();

                var reader = new CborReader(encoding);
                double result = reader.ReadDouble();
                Assert.Equal(input, result);
            });
        }

        [Fact]
        public static void Roundtrip_Decimal()
        {
            CheckProperty<CborConformanceMode, decimal>((mode, input) =>
            {
                var writer = new CborWriter();
                writer.WriteDecimal(input);
                byte[] encoding = writer.Encode();

                var reader = new CborReader(encoding);
                decimal result = reader.ReadDecimal();
                Assert.Equal(input, result);
            });
        }

        [Fact]
        public static void ByteString_Encoding_ShouldContainInputBytes()
        {
            CheckProperty<CborConformanceMode, byte[]>((mode, input) =>
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
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/73150", TestPlatforms.iOS | TestPlatforms.tvOS)]
        public static void CborDocument_Roundtrip()
        {
            CheckProperty<CborPropertyTestContext>((input) =>
            {
                byte[] encoding = CborDocumentSerializer.encode(input);

                CborDocument[] expectedResults = CborPropertyTestContextHelper.getExpectedRoundtripValues(input);
                CborDocument[] roundtrippedDocuments = CborDocumentSerializer.decode(input, encoding);
                Assert.Equal(expectedResults, roundtrippedDocuments);
            });
        }

        [Fact]
        public static void CborDocument_SkipValue()
        {
            CheckProperty<CborPropertyTestContext>((input) =>
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
            });
        }

        [Fact]
        public static void CborDocument_SkipToParent()
        {
            CheckProperty<CborPropertyTestContext>((input) =>
            {
                input.RootDocuments = new[] { CborDocument.NewArray(_isDefiniteLength: true, input.RootDocuments) };
                byte[] encoding = CborDocumentSerializer.encode(input);

                CborReader reader = CborDocumentSerializer.createReader(input, encoding);
                reader.ReadStartArray();
                reader.SkipToParent();
                Assert.Equal(CborReaderState.Finished, reader.PeekState());
            });
        }
    }
}
