// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
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
        public static void SkipValue_InvalidFormat_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.SkipValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("61ff")]
        [InlineData("62f090")]
        public static void SkipValue_InvalidUtf8_LaxConformance_ShouldSucceed(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, CborConformanceMode.Lax);

            reader.SkipValue();

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax)]
        public static void SkipValue_ValidationEnabled_InvalidUtf8_LaxConformance_ShouldSucceed(CborConformanceMode conformanceMode)
        {
            byte[] encoding = "62f090".HexToByteArray();
            var reader = new CborReader(encoding, conformanceMode);

            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceMode.Strict)]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        public static void SkipValue_ValidationEnabled_InvalidUtf8_StrictConformance_ShouldThrowCborContentException(CborConformanceMode conformanceMode)
        {
            byte[] encoding = "62f090".HexToByteArray();
            var reader = new CborReader(encoding, conformanceMode);

            CborContentException exn = Assert.Throws<CborContentException>(() => reader.SkipValue());
            Assert.NotNull(exn.InnerException);
            Assert.IsType<DecoderFallbackException>(exn.InnerException);

            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [MemberData(nameof(NonConformingSkipValueEncodings))]
        public static void SkipValue_ValidationDisabled_NonConformingValues_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);

            reader.SkipValue(disableConformanceModeChecks: true);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [MemberData(nameof(NonConformingSkipValueEncodings))]
        public static void SkipValue_ValidationEnabled_NonConformingValues_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);

            Assert.Throws<CborContentException>(() => reader.SkipValue());
        }

        public static IEnumerable<object[]> NonConformingSkipValueEncodings =>
            new (CborConformanceMode Mode, string Encoding)[]
            {
                (CborConformanceMode.Ctap2Canonical, "1801"), // non-canonical integer representation
                (CborConformanceMode.Canonical, "5fff"), // indefinite-length byte string
                (CborConformanceMode.Canonical, "7fff"), // indefinite-length text string
                (CborConformanceMode.Canonical, "9fff"), // indefinite-length array
                (CborConformanceMode.Canonical, "bfff"), // indefinite-length map
                (CborConformanceMode.Strict, "a201020103"), // duplicate keys in map
                (CborConformanceMode.Canonical, "a201020103"), // duplicate keys in map
                (CborConformanceMode.Ctap2Canonical, "a202020101"), // unsorted keys in map
                (CborConformanceMode.Ctap2Canonical, "c001"), // tagged value
                (CborConformanceMode.Strict, "f81f"), // non-canonical simple value
            }.Select(l => new object[] { l.Mode, l.Encoding });

        [Fact]
        public static void SkipValue_SkippedValueFollowedByNonConformingValue_ShouldThrowCborContentException()
        {
            byte[] encoding = "827fff7fff".HexToByteArray();
            var reader = new CborReader(encoding, CborConformanceMode.Ctap2Canonical);

            reader.ReadStartArray();
            reader.SkipValue(disableConformanceModeChecks: true);
            Assert.Throws<CborContentException>(() => reader.ReadTextString());
        }

        [Fact]
        public static void SkipValue_NestedCborContentException_ShouldPreserveOriginalReaderState()
        {
            string hexEncoding = "820181bf01ff"; // [1, [ {_ 1 : <missing value> } ]]
            var reader = new CborReader(hexEncoding.HexToByteArray());

            reader.ReadStartArray();
            reader.ReadInt64();

            // capture current state
            int bytesRemaining = reader.BytesRemaining;

            // make failing call
            Assert.Throws<CborContentException>(() => reader.SkipValue());

            // ensure reader state has reverted to original
            Assert.Equal(reader.BytesRemaining, bytesRemaining);

            // ensure we can read every value up to the format error
            Assert.Equal(CborReaderState.StartArray, reader.PeekState());
            reader.ReadStartArray();
            Assert.Equal(CborReaderState.StartMap, reader.PeekState());
            reader.ReadStartMap();
            Assert.Equal(CborReaderState.UnsignedInteger, reader.PeekState());
            reader.ReadUInt64();
            Assert.Throws<CborContentException>(() => reader.PeekState());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void SkipToParent_SimpleArray_HappyPath(int skipOffset)
        {
            byte[] encoding = "83010203".HexToByteArray(); // [1, 2, 3]
            var reader = new CborReader(encoding);

            reader.ReadStartArray();
            for (int i = 0; i < skipOffset; i++)
            {
                reader.ReadInt32();
            }

            reader.SkipToParent();

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void SkipToParent_NestedArray_HappyPath(int skipOffset)
        {
            byte[] encoding = "8283010203a0".HexToByteArray(); // [[1, 2, 3], { }]
            var reader = new CborReader(encoding);

            reader.ReadStartArray();
            reader.ReadStartArray();
            for (int i = 0; i < skipOffset; i++)
            {
                reader.ReadInt32();
            }

            reader.SkipToParent();
            Assert.Equal(CborReaderState.StartMap, reader.PeekState());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void SkipToParent_NestedKey_HappyPath(int skipOffset)
        {
            byte[] encoding = "a17f616161626163ff80".HexToByteArray(); // { (_ "a", "b", "c") : [] }
            var reader = new CborReader(encoding);

            reader.ReadStartMap();
            reader.ReadStartIndefiniteLengthTextString();
            for (int i = 0; i < skipOffset; i++)
            {
                reader.ReadTextString();
            }

            reader.SkipToParent();
            Assert.Equal(CborReaderState.StartArray, reader.PeekState());
        }

        [Fact]
        public static void SkipToParent_RootContext_ShouldThrowInvalidOperationException()
        {
            byte[] encoding = "01".HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.SkipToParent());
            reader.ReadInt32();
            Assert.Throws<InvalidOperationException>(() => reader.SkipToParent());
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

            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = depth });

            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [MemberData(nameof(TruncatedCborInputs))]
        public static void TrySkipValue_NotFinalBlock_TruncatedValue_ReturnsFalseAndPreservesState(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, LaxOptions, isFinalBlock: false);

            Assert.False(reader.TrySkipValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
            Assert.Equal(0, reader.CurrentDepth);
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void TrySkipValue_NotFinalBlock_AllSplitPoints_SucceedsAfterSlideData(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();

            for (int split = 0; split < encoding.Length; split++)
            {
                var reader = new CborReader(encoding.AsMemory(0, split), LaxOptions, isFinalBlock: false);

                Assert.False(reader.TrySkipValue());
                Assert.Equal(split, reader.BytesRemaining); // reader state was restored

                reader.SlideData(encoding, isFinalBlock: true);
                Assert.True(reader.TrySkipValue());
                Assert.Equal(CborReaderState.Finished, reader.PeekState());
            }
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void TrySkipToParent_NotFinalBlock_AllSplitPoints_SucceedsAfterSlideData(string hexEncoding)
        {
            byte[] encoding = ("8301" + hexEncoding + "03").HexToByteArray(); // [1, <value>, 3]

            for (int split = 2; split < encoding.Length; split++)
            {
                var reader = new CborReader(encoding.AsMemory(0, split), LaxOptions, isFinalBlock: false);
                reader.ReadStartArray();
                Helpers.VerifyValue(reader, 1);

                int bytesRemaining = reader.BytesRemaining;
                Assert.False(reader.TrySkipToParent());
                Assert.Equal(bytesRemaining, reader.BytesRemaining); // reader state was restored
                Assert.Equal(1, reader.CurrentDepth);

                reader.SlideData(encoding.AsMemory(split - reader.BytesRemaining), isFinalBlock: true);
                Assert.True(reader.TrySkipToParent());
                Assert.Equal(0, reader.CurrentDepth);
                Assert.Equal(CborReaderState.Finished, reader.PeekState());
            }
        }

        [Theory]
        [MemberData(nameof(SkipTestInputs))]
        public static void TrySkipValue_FinalBlock_HappyPath(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.True(reader.TrySkipValue());
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [MemberData(nameof(SkipTestInvalidCborInputs))]
        public static void TrySkipValue_FinalBlock_InvalidValue_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.TrySkipValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("bf01ff")] // indefinite-length map key missing a value
        [InlineData("daffffffffff")] // tag followed by break byte
        [InlineData("1c")] // reserved additional information value
        [InlineData("ff")] // break byte at the root context
        public static void TrySkipValue_NotFinalBlock_MalformedData_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, LaxOptions, isFinalBlock: false);

            Assert.Throws<CborContentException>(() => reader.TrySkipValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [MemberData(nameof(NonConformingSkipValueEncodings))]
        public static void TrySkipValue_ValidationDisabled_NonConformingValues_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);

            Assert.True(reader.TrySkipValue(disableConformanceModeChecks: true));
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void TrySkipToParent_RootContext_ShouldThrowInvalidOperationException()
        {
            var reader = new CborReader("01".HexToByteArray(), LaxOptions, isFinalBlock: false);
            Assert.Throws<InvalidOperationException>(() => reader.TrySkipToParent());
        }

        [Fact]
        public static void TrySkipValue_NotAtStartOfValue_ShouldThrowInvalidOperationException()
        {
            // at the end of a definite-length collection
            var reader = new CborReader("8101".HexToByteArray(), LaxOptions, isFinalBlock: false); // [1]
            reader.ReadStartArray();
            Helpers.VerifyValue(reader, 1);
            Assert.Equal(CborReaderState.EndArray, reader.PeekState());
            Assert.Throws<InvalidOperationException>(() => reader.TrySkipValue());

            // at the end of the document
            reader = new CborReader("01".HexToByteArray(), LaxOptions, isFinalBlock: false);
            Helpers.VerifyValue(reader, 1);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.Throws<InvalidOperationException>(() => reader.TrySkipValue());
        }

        [Fact]
        public static void TrySkipValue_NotFinalBlock_AfterReadTag_ShouldRestoreTagContext()
        {
            byte[] encoding = "c1820102".HexToByteArray(); // 1([1, 2])
            var reader = new CborReader(encoding.AsMemory(0, 3), LaxOptions, isFinalBlock: false);

            Assert.Equal((CborTag)1, reader.ReadTag());

            // the failed skip enters the truncated array before restoring,
            // which exercises checkpoint restoration of the pending tag context
            Assert.False(reader.TrySkipValue());
            Assert.Equal(2, reader.BytesRemaining);
            Assert.Equal(CborReaderState.StartArray, reader.PeekState());

            reader.SlideData(encoding.AsMemory(1), isFinalBlock: true);
            Assert.True(reader.TrySkipValue());
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void TrySkipValue_NotFinalBlock_TruncatedNestedTags_ShouldRestoreTagContext()
        {
            byte[] encoding = "c1c201".HexToByteArray(); // 1(2(1))
            var reader = new CborReader(encoding.AsMemory(0, 2), LaxOptions, isFinalBlock: false);

            // the failed skip consumes both tags before restoring,
            // which exercises checkpoint restoration of the pending tag context
            Assert.False(reader.TrySkipValue());
            Assert.Equal(2, reader.BytesRemaining);
            Assert.Equal(CborReaderState.Tag, reader.PeekState());

            reader.SlideData(encoding, isFinalBlock: true);
            Assert.True(reader.TrySkipValue());
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void TrySkipValue_NotFinalBlock_TruncatedMapValue_ReturnsFalseAndResumesAfterSlideData()
        {
            byte[] encoding = "a26161820102616203".HexToByteArray(); // {"a": [1, 2], "b": 3}
            var reader = new CborReader(encoding.AsMemory(0, 5), LaxOptions, isFinalBlock: false);

            reader.ReadStartMap();
            Assert.Equal("a", reader.ReadTextString());

            // the failed skip at a value position enters the truncated array before restoring,
            // which exercises checkpoint restoration of the map frame's key/value bookkeeping
            Assert.False(reader.TrySkipValue());
            Assert.Equal(2, reader.BytesRemaining);
            Assert.Equal(1, reader.CurrentDepth);
            Assert.Equal(CborReaderState.StartArray, reader.PeekState());

            reader.SlideData(encoding.AsMemory(3), isFinalBlock: true);
            Assert.True(reader.TrySkipValue());
            Assert.Equal("b", reader.ReadTextString());
            Helpers.VerifyValue(reader, 3);
            reader.ReadEndMap();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [MemberData(nameof(TruncatedCborInputs))]
        public static void SkipValue_NotFinalBlock_TruncatedValue_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, LaxOptions, isFinalBlock: false);

            Assert.Throws<CborContentException>(() => reader.SkipValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [MemberData(nameof(SampleValuesAndChunkSizes))]
        public static void TrySkipValue_NotFinalBlock_ChunkedReading_HappyPath(string hexEncoding, int chunkSize)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var feeder = new ChunkedFeeder(encoding, chunkSize);
            var reader = new CborReader(ReadOnlyMemory<byte>.Empty, LaxOptions, isFinalBlock: false);

            while (!reader.TrySkipValue())
            {
                feeder.Slide(reader);
            }

            Assert.Equal(0, reader.BytesRemaining);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void TrySkipToParent_NotFinalBlock_NestedContexts_SucceedsAfterSlideData()
        {
            byte[] encoding = "a16161820102".HexToByteArray(); // {"a": [1, 2]}

            for (int split = 4; split < encoding.Length; split++)
            {
                var reader = new CborReader(encoding.AsMemory(0, split), LaxOptions, isFinalBlock: false);
                reader.ReadStartMap();
                reader.ReadTextString();
                reader.ReadStartArray();
                Assert.Equal(2, reader.CurrentDepth);

                Assert.False(reader.TrySkipToParent());
                Assert.Equal(2, reader.CurrentDepth);

                reader.SlideData(encoding.AsMemory(split - reader.BytesRemaining), isFinalBlock: true);
                Assert.True(reader.TrySkipToParent()); // exits the array
                Assert.Equal(1, reader.CurrentDepth);
                Assert.True(reader.TrySkipToParent()); // exits the map
                Assert.Equal(0, reader.CurrentDepth);
                Assert.Equal(CborReaderState.Finished, reader.PeekState());
            }
        }

        public static IEnumerable<object[]> SkipTestInputs => SampleCborValues.Select(x => new [] { x });
        public static IEnumerable<object[]> SkipTestInvalidCborInputs => InvalidCborValues.Select(x => new[] { x });
        public static IEnumerable<object[]> TruncatedCborInputs => TruncatedCborValues.Select(x => new object[] { x });
    }
}
