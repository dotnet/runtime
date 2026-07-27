// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborReaderTests
    {
        private const string MaxDepthMessageSentinel = "maximum allowed depth";

        [Fact]
        public static void SimpleConstructor_HasCorrectDefaults()
        {
            var reader = new CborReader(Array.Empty<byte>());
            Assert.Equal(CborConformanceMode.Strict, reader.ConformanceMode);
            Assert.False(reader.AllowMultipleRootLevelValues);
            Assert.Equal(64, reader.MaxDepth);
        }

        [Theory]
        [InlineData((CborConformanceMode)(-1))]
        public static void InvalidConformanceMode_ShouldThrowArgumentOutOfRangeException(CborConformanceMode mode)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CborReader(Array.Empty<byte>(), conformanceMode: mode));
        }

        [Fact]
        public static void Peek_EmptyBuffer_ShouldThrowCborContentException()
        {
            var reader = new CborReader(ReadOnlyMemory<byte>.Empty);
            Assert.Throws<CborContentException>(() => reader.PeekState());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        public static void Depth_ShouldReturnExpectedValue(int depth)
        {
            byte[] encoding = Enumerable.Repeat<byte>(0x81, depth).Append<byte>(0x01).ToArray();
            var reader = new CborReader(encoding);

            for (int i = 0; i < depth; i++)
            {
                Assert.Equal(i, reader.CurrentDepth);
                reader.ReadStartArray();
            }

            Assert.Equal(depth, reader.CurrentDepth);
            reader.ReadInt32();
            Assert.Equal(depth, reader.CurrentDepth);

            for (int i = depth - 1; i >= 0; i--)
            {
                reader.ReadEndArray();
                Assert.Equal(i, reader.CurrentDepth);
            }

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void Reset_DoesNotAffect_ConformanceMode()
        {
            var reader = new CborReader(Array.Empty<byte>());
            Assert.Equal(CborConformanceMode.Strict, reader.ConformanceMode);

            reader.Reset(Array.Empty<byte>());
            Assert.Equal(CborConformanceMode.Strict, reader.ConformanceMode);

            foreach(var mode in new[] { CborConformanceMode.Canonical, CborConformanceMode.Ctap2Canonical, CborConformanceMode.Lax, CborConformanceMode.Strict })
            {
                reader = new CborReader(Array.Empty<byte>(), mode);
                Assert.Equal(mode, reader.ConformanceMode);

                reader.Reset(Array.Empty<byte>());
                Assert.Equal(mode, reader.ConformanceMode);
            }
        }

        [Fact]
        public static void Reset_DoesNotAffect_AllowMultipleRootLevelValues()
        {
            var reader = new CborReader(Array.Empty<byte>(),  allowMultipleRootLevelValues: false);
            Assert.False(reader.AllowMultipleRootLevelValues);

            reader.Reset(Array.Empty<byte>());
            Assert.False(reader.AllowMultipleRootLevelValues);

            reader = new CborReader(Array.Empty<byte>(), allowMultipleRootLevelValues: true);
            Assert.True(reader.AllowMultipleRootLevelValues);

            reader.Reset(Array.Empty<byte>());
            Assert.True(reader.AllowMultipleRootLevelValues);
        }

        [Fact]
        public static void CborReaderOptions_DefaultValues()
        {
            var options = new CborReaderOptions();
            Assert.Equal(CborConformanceMode.Strict, options.ConformanceMode);
            Assert.False(options.AllowMultipleRootLevelValues);
            Assert.Equal(-1, options.MaxDepth);
        }

        [Fact]
        public static void CborReader_NullOptions_DefaultValues()
        {
            var reader = new CborReader(Array.Empty<byte>(), (CborReaderOptions)null!);
            Assert.Equal(CborConformanceMode.Strict, reader.ConformanceMode);
            Assert.False(reader.AllowMultipleRootLevelValues);
            Assert.Equal(64, reader.MaxDepth);
        }

        [Fact]
        public static void CborReaderOptions_SetConformanceMode()
        {
            var options = new CborReaderOptions { ConformanceMode = CborConformanceMode.Canonical };
            Assert.Equal(CborConformanceMode.Canonical, options.ConformanceMode);
        }

        [Fact]
        public static void CborReaderOptions_InvalidConformanceMode_Throws()
        {
            var options = new CborReaderOptions();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.ConformanceMode = (CborConformanceMode)(-1));
        }

        [Fact]
        public static void CborReaderOptions_SetAllowMultipleRootLevelValues()
        {
            var options = new CborReaderOptions { AllowMultipleRootLevelValues = true };
            Assert.True(options.AllowMultipleRootLevelValues);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(100)]
        public static void CborReaderOptions_SetMaxDepth(int maxDepth)
        {
            var options = new CborReaderOptions { MaxDepth = maxDepth };
            Assert.Equal(maxDepth, options.MaxDepth);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(int.MinValue)]
        public static void CborReaderOptions_SetMaxDepth_TooNegative_Throws(int maxDepth)
        {
            var options = new CborReaderOptions();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = maxDepth);
        }

        [Fact]
        public static void Constructor_WithOptions_DefaultOptions()
        {
            var reader = new CborReader(Array.Empty<byte>(), new CborReaderOptions());
            Assert.Equal(CborConformanceMode.Strict, reader.ConformanceMode);
            Assert.False(reader.AllowMultipleRootLevelValues);
            Assert.Equal(64, reader.MaxDepth);
        }

        [Fact]
        public static void Constructor_WithOptions_CustomValues()
        {
            var options = new CborReaderOptions
            {
                ConformanceMode = CborConformanceMode.Canonical,
                AllowMultipleRootLevelValues = true,
                MaxDepth = 42,
            };

            var reader = new CborReader(Array.Empty<byte>(), options);
            Assert.Equal(CborConformanceMode.Canonical, reader.ConformanceMode);
            Assert.True(reader.AllowMultipleRootLevelValues);
            Assert.Equal(42, reader.MaxDepth);
        }

        [Fact]
        public static void MaxDepth_DefaultValue()
        {
            var reader = new CborReader(Array.Empty<byte>());
            Assert.Equal(64, reader.MaxDepth);
        }

        [Fact]
        public static void MaxDepth_ZeroYieldsZero()
        {
            var reader = new CborReader(Array.Empty<byte>(), new CborReaderOptions { MaxDepth = 0 });
            Assert.Equal(0, reader.MaxDepth);
        }

        [Fact]
        public static void MaxDepth_NegativeOneYieldsDefaultValue()
        {
            var reader = new CborReader(Array.Empty<byte>(), new CborReaderOptions { MaxDepth = -1 });
            Assert.Equal(64, reader.MaxDepth);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static void MaxDepth_CustomValue_ShouldReturnSetValue(int maxDepth)
        {
            var reader = new CborReader(Array.Empty<byte>(), new CborReaderOptions { MaxDepth = maxDepth });
            Assert.Equal(maxDepth, reader.MaxDepth);
        }
        
        [Fact]
        public static void Reset_DoesNotAffect_MaxDepth()
        {
            var reader = new CborReader(Array.Empty<byte>(), new CborReaderOptions { MaxDepth = 42 });
            Assert.Equal(42, reader.MaxDepth);

            reader.Reset(Array.Empty<byte>());
            Assert.Equal(42, reader.MaxDepth);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(10, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(10, true)]
        public static void MaxDepth_GuardsNestedArrays(int maxDepth, bool useIndefiniteLength)
        {
            int depth = maxDepth + 1;
            IEnumerable<byte> head = Enumerable.Repeat(useIndefiniteLength ? (byte)0x9F : (byte)0x81, depth);
            IEnumerable<byte> tail = Enumerable.Repeat((byte)0xFF, useIndefiniteLength ? depth : 0);
            byte[] encoding = head.Append<byte>(0x01).Concat(tail).ToArray();

            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = maxDepth });
            var longerReader = new CborReader(encoding, new CborReaderOptions { MaxDepth = maxDepth + 1 });

            for (int i = 0; i < maxDepth; i++)
            {
                reader.ReadStartArray();
                longerReader.ReadStartArray();
            }

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartArray(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            // The higher depth limit should succeed
            longerReader.ReadStartArray();
            longerReader.ReadInt32();
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(10, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(10, true)]
        public static void MaxDepth_GuardsNestedMaps(int maxDepth, bool useIndefiniteLength)
        {
            int depth = maxDepth + 1;
            // definite: A1 00 A1 00 ... 01
            // indefinite: BF 00 BF 00 ... 01 FF FF ...
            IEnumerable<byte> head = Enumerable.Repeat(useIndefiniteLength ? (byte)0xBF : (byte)0xA1, depth).SelectMany(x => new byte[] { x, 0x00 });
            IEnumerable<byte> tail = Enumerable.Repeat((byte)0xFF, useIndefiniteLength ? depth : 0);
            byte[] encoding = head.Append<byte>(0x01).Concat(tail).ToArray();

            var reader = new CborReader(encoding, new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax, MaxDepth = maxDepth });
            var longerReader = new CborReader(encoding, new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax, MaxDepth = maxDepth + 1 });

            for (int i = 0; i < maxDepth; i++)
            {
                reader.ReadStartMap();
                reader.ReadInt32(); // key

                longerReader.ReadStartMap();
                longerReader.ReadInt32(); // key
            }

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartMap(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            // The higher depth limit should succeed
            longerReader.ReadStartMap();
            longerReader.ReadInt32();

            Assert.Equal(1, longerReader.ReadInt32());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsSkipValue(bool useIndefiniteLength)
        {
            // 3 nested arrays
            byte[] encoding = useIndefiniteLength ?
                new byte[] { 0x9F, 0x9F, 0x9F, 0x01, 0xFF, 0xFF, 0xFF } :
                new byte[] { 0x81, 0x81, 0x81, 0x01 };

            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 2 });
            var longerReader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 3 });

            AssertExtensions.ThrowsContains<CborContentException>(() => reader.SkipValue(), MaxDepthMessageSentinel);
            Assert.Equal(encoding.Length, reader.BytesRemaining);

            // The higher depth limit should succeed
            longerReader.SkipValue();
            Assert.Equal(CborReaderState.Finished, longerReader.PeekState());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsReadEncodedValue(bool useIndefiniteLength)
        {
            // 3 nested arrays
            byte[] encoding = useIndefiniteLength ?
                new byte[] { 0x9F, 0x9F, 0x9F, 0x01, 0xFF, 0xFF, 0xFF } :
                new byte[] { 0x81, 0x81, 0x81, 0x01 };

            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 2 });
            var longerReader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 3 });

            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadEncodedValue(), MaxDepthMessageSentinel);
            Assert.Equal(encoding.Length, reader.BytesRemaining);

            // The higher depth limit should succeed
            ReadOnlyMemory<byte> encodedValue = longerReader.ReadEncodedValue();
            Assert.Equal(CborReaderState.Finished, longerReader.PeekState());
            Assert.True(encodedValue.Span.Overlaps(encoding, out int offset));
            Assert.Equal(0, offset);
            Assert.Equal(encoding.Length, encodedValue.Length);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsMixedArrayAndMapNesting(bool useIndefiniteLength)
        {
            // array(1) -> map(1) { 0: array(1) -> 1 }, maxDepth=2
            byte[] encoding = useIndefiniteLength ?
                new byte[] { 0x9F, 0xBF, 0x00, 0x9F, 0x01, 0xFF, 0xFF, 0xFF } :
                new byte[] { 0x81, 0xA1, 0x00, 0x81, 0x01 };

            var reader = new CborReader(encoding, new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax, MaxDepth = 2 });
            var longerReader = new CborReader(encoding, new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax, MaxDepth = 3 });

            reader.ReadStartArray();
            reader.ReadStartMap();
            reader.ReadInt32();

            longerReader.ReadStartArray();
            longerReader.ReadStartMap();
            longerReader.ReadInt32();

            // Attempting to read another nested array should fail
            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartArray(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            // But the higher limit should succeed.
            longerReader.ReadStartArray();
            Assert.Equal(1, longerReader.ReadInt32());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsIndefiniteByteString_FromArray(bool useIndefiniteLength)
        {
            // array(1) containing indefinite byte string (5F FF)
            byte[] encoding = useIndefiniteLength ?
                new byte[] { 0x9F, 0x5F, 0xFF, 0xFF } :
                new byte[] { 0x81, 0x5F, 0xFF };

            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 1 });
            var longerReader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 2 });

            reader.ReadStartArray();
            longerReader.ReadStartArray();

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartIndefiniteLengthByteString(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            longerReader.ReadStartIndefiniteLengthByteString();
            Assert.Equal(CborReaderState.EndIndefiniteLengthByteString, longerReader.PeekState());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsIndefiniteByteString_FromMap(bool useIndefiniteLength)
        {
            // map(1) { 0: indefinite byte string (5F FF) }
            byte[] encoding = useIndefiniteLength ?
                new byte[] { 0xBF, 0x00, 0x5F, 0xFF, 0xFF } :
                new byte[] { 0xA1, 0x00, 0x5F, 0xFF };

            var reader = new CborReader(encoding, new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax, MaxDepth = 1 });
            var longerReader = new CborReader(encoding, new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax, MaxDepth = 2 });

            reader.ReadStartMap();
            reader.ReadInt32();

            longerReader.ReadStartMap();
            longerReader.ReadInt32();

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartIndefiniteLengthByteString(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            longerReader.ReadStartIndefiniteLengthByteString();
            Assert.Equal(CborReaderState.EndIndefiniteLengthByteString, longerReader.PeekState());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsIndefiniteTextString_FromArray(bool useIndefiniteLength)
        {
            // array(1) containing indefinite text string (7F FF)
            byte[] encoding = useIndefiniteLength ?
                new byte[] { 0x9F, 0x7F, 0xFF, 0xFF } :
                new byte[] { 0x81, 0x7F, 0xFF };

            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 1 });
            var longerReader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 2 });

            reader.ReadStartArray();
            longerReader.ReadStartArray();

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartIndefiniteLengthTextString(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            longerReader.ReadStartIndefiniteLengthTextString();
            Assert.Equal(CborReaderState.EndIndefiniteLengthTextString, longerReader.PeekState());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsIndefiniteTextString_FromMap(bool useIndefiniteLength)
        {
            // map(1) { 0: indefinite text string (7F FF) }
            byte[] encoding = useIndefiniteLength ?
                new byte[] { 0xBF, 0x00, 0x7F, 0xFF, 0xFF } :
                new byte[] { 0xA1, 0x00, 0x7F, 0xFF };

            var reader = new CborReader(encoding, new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax, MaxDepth = 1 });
            var longerReader = new CborReader(encoding, new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax, MaxDepth = 2 });

            reader.ReadStartMap();
            reader.ReadInt32();

            longerReader.ReadStartMap();
            longerReader.ReadInt32();

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartIndefiniteLengthTextString(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            longerReader.ReadStartIndefiniteLengthTextString();
            Assert.Equal(CborReaderState.EndIndefiniteLengthTextString, longerReader.PeekState());
        }

        [Fact]
        public static void MaxDepth_Zero_AllowsPrimitiveValues()
        {
            byte[] encoding = [0x18, 0x2a]; // integer 42
            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 0 });

            Assert.Equal(0, reader.MaxDepth);
            int value = reader.ReadInt32();
            Assert.Equal(42, value);
            Assert.Equal(0, reader.BytesRemaining);
        }

        [Fact]
        public static void MaxDepth_Zero_BlocksArray()
        {
            byte[] encoding = [0x81, 0x01]; // array of length 1 containing integer 1
            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 0 });

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartArray(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);
        }

        [Fact]
        public static void MaxDepth_Zero_BlocksMap()
        {
            byte[] encoding = [0xA1, 0x01, 0x02]; // map of length 1: {1: 2}
            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 0 });

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartMap(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);
        }

        [Fact]
        public static void MaxDepth_IndefiniteByteString_AtLimit()
        {
            // indefinite byte string with one chunk: (_ h'01')
            byte[] encoding = [0x5F, 0x41, 0x01, 0xFF];
            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 0 });

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartIndefiniteLengthByteString(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadByteString(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            AssertExtensions.ThrowsContains<CborContentException>(() => reader.SkipValue(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);
        }

        [Fact]
        public static void MaxDepth_IndefiniteTextString_AtLimit()
        {
            // indefinite text string with one chunk: (_ "a")
            byte[] encoding = [0x7F, 0x61, 0x61, 0xFF];
            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 0 });

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartIndefiniteLengthTextString(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadTextString(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            AssertExtensions.ThrowsContains<CborContentException>(() => reader.SkipValue(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);
        }

        [Fact]
        public static void MaxDepth_IndefiniteArray_AtLimit()
        {
            // indefinite array with one element: [_ 1]
            byte[] encoding = [0x9F, 0x01, 0xFF];
            var reader = new CborReader(encoding, new CborReaderOptions { MaxDepth = 0 });

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartArray(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            AssertExtensions.ThrowsContains<CborContentException>(() => reader.SkipValue(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);
        }

        [Fact]
        public static void MaxDepth_IndefiniteMap_AtLimit()
        {
            // indefinite map with one entry: {_ 1: 2}
            byte[] encoding = [0xBF, 0x01, 0x02, 0xFF];
            var reader = new CborReader(encoding, new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax, MaxDepth = 0 });

            int remaining = reader.BytesRemaining;
            AssertExtensions.ThrowsContains<CborContentException>(() => reader.ReadStartMap(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);

            AssertExtensions.ThrowsContains<CborContentException>(() => reader.SkipValue(), MaxDepthMessageSentinel);
            Assert.Equal(remaining, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        public static void ResetInDepth_ShouldReturnExpectedValue(int depth)
        {
            byte[] encoding = Enumerable.Repeat<byte>(0x81, depth).Append<byte>(0x01).ToArray();

            var reader = new CborReader(encoding);
            for (int i = 0; i < depth; i++)
            {
                Assert.Equal(i, reader.CurrentDepth);
                reader.ReadStartArray();
            }
            
            reader.Reset(encoding);
            Assert.Equal(0, reader.CurrentDepth);
            Assert.Equal(encoding.Length, reader.BytesRemaining);

            for (int i = 0; i < depth; i++)
            {
                Assert.Equal(i, reader.CurrentDepth);
                reader.ReadStartArray();
            }

            Assert.Equal(depth, reader.CurrentDepth);
            reader.ReadInt32();
            Assert.Equal(depth, reader.CurrentDepth);

            for (int i = depth - 1; i >= 0; i--)
            {
                reader.ReadEndArray();
                Assert.Equal(i, reader.CurrentDepth);
            }

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void BytesRemaining_NoReads_ShouldReturnTotalLength()
        {
            var reader = new CborReader(new byte[10]);
            Assert.Equal(10, reader.BytesRemaining);
        }

        [Fact]
        public static void BytesRemaining_OnValueRead_ShouldReturnZero()
        {
            var reader = new CborReader(new byte[] { 24, 24 });
            reader.ReadInt64();
            Assert.Equal(0, reader.BytesRemaining);
        }

        [Fact]
        public static void ConformanceMode_DefaultValue_ShouldEqualStrict()
        {
            var reader = new CborReader(Array.Empty<byte>());
            Assert.Equal(CborConformanceMode.Strict, reader.ConformanceMode);
        }

        [Theory]
        [InlineData(0, CborReaderState.UnsignedInteger)]
        [InlineData(1, CborReaderState.NegativeInteger)]
        [InlineData(2, CborReaderState.ByteString)]
        [InlineData(3, CborReaderState.TextString)]
        [InlineData(4, CborReaderState.StartArray)]
        [InlineData(5, CborReaderState.StartMap)]
        [InlineData(6, CborReaderState.Tag)]
        [InlineData(7, CborReaderState.SimpleValue)]
        public static void Peek_SingleByteBuffer_ShouldReturnExpectedState(byte majorType, CborReaderState expectedResult)
        {
            ReadOnlyMemory<byte> buffer = new byte[] { (byte)(majorType << 5) };
            var reader = new CborReader(buffer);
            Assert.Equal(expectedResult, reader.PeekState());
        }

        [Fact]
        public static void Read_EmptyBuffer_ShouldThrowCborContentException()
        {
            var reader = new CborReader(ReadOnlyMemory<byte>.Empty);
            Assert.Throws<CborContentException>(() => reader.ReadInt64());
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
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.True(reader.BytesRemaining > 0);
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
        public static void CborReader_MultipleRootValuesAllowed_RootLevelBreakByte_ShouldThrowCborContentException()
        {
            string hexEncoding = "018101ff";
            var reader = new CborReader(hexEncoding.HexToByteArray(), allowMultipleRootLevelValues: true);

            reader.ReadInt32();
            reader.ReadStartArray();
            reader.ReadInt32();
            reader.ReadEndArray();

            Assert.Throws<CborContentException>(() => reader.PeekState());
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
        public static void ReadEncodedValue_InvalidCbor_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<CborContentException>(() => reader.ReadEncodedValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        public static IEnumerable<object[]> EncodedValueInputs => CborReaderTests.SampleCborValues.Select(x => new[] { x });
        public static IEnumerable<object[]> EncodedValueInvalidInputs => CborReaderTests.InvalidCborValues.Select(x => new[] { x });

        [Theory]
        [MemberData(nameof(NonConformingSkipValueEncodings))]
        public static void ReadEncodedValue_InvalidConformance_ConformanceCheckEnabled_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            Assert.Throws<CborContentException>(() => reader.ReadEncodedValue(disableConformanceModeChecks: false));
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [MemberData(nameof(NonConformingSkipValueEncodings))]
        public static void ReadEncodedValue_InvalidConformance_ConformanceCheckDisabled_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            ReadOnlyMemory<byte> encodedValue = reader.ReadEncodedValue(disableConformanceModeChecks: true);
            Assert.Equal(encoding, encodedValue);
            Assert.Equal(0, reader.BytesRemaining);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        [InlineData("a501020326200121582065eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d2258201e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c",
                    "65eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d",
                    "1e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c",
                    "SHA256", "ECDSA_P256")]
        [InlineData("a501020338222002215830ed57d8608c5734a5ed5d22026bad8700636823e45297306479beb61a5bd6b04688c34a2f0de51d91064355eef7548bdd22583024376b4fee60ba65db61de54234575eec5d37e1184fbafa1f49d71e1795bba6bda9cbe2ebb815f9b49b371486b38fa1b",
                    "ed57d8608c5734a5ed5d22026bad8700636823e45297306479beb61a5bd6b04688c34a2f0de51d91064355eef7548bdd",
                    "24376b4fee60ba65db61de54234575eec5d37e1184fbafa1f49d71e1795bba6bda9cbe2ebb815f9b49b371486b38fa1b",
                    "SHA384", "ECDSA_P384")]
        [InlineData("a50102033823200321584200b03811bef65e330bb974224ec3ab0a5469f038c92177b4171f6f66f91244d4476e016ee77cf7e155a4f73567627b5d72eaf0cb4a6036c6509a6432d7cd6a3b325c2258420114b597b6c271d8435cfa02e890608c93f5bc118ca7f47bf191e9f9e49a22f8a15962315f0729781e1d78b302970c832db2fa8f7f782a33f8e1514950dc7499035f",
                    "00b03811bef65e330bb974224ec3ab0a5469f038c92177b4171f6f66f91244d4476e016ee77cf7e155a4f73567627b5d72eaf0cb4a6036c6509a6432d7cd6a3b325c",
                    "0114b597b6c271d8435cfa02e890608c93f5bc118ca7f47bf191e9f9e49a22f8a15962315f0729781e1d78b302970c832db2fa8f7f782a33f8e1514950dc7499035f",
                    "SHA512", "ECDSA_P521")]
        [InlineData("a40102200121582065eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d2258201e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c",
                    "65eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d",
                    "1e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c",
                    null, "ECDSA_P256")]
        public static void CoseKeyHelpers_ECDsaParseCosePublicKey_HappyPath(string hexEncoding, string hexExpectedQx, string hexExpectedQy, string? expectedHashAlgorithmName, string curveFriendlyName)
        {
            ECPoint q = new ECPoint() { X = hexExpectedQx.HexToByteArray(), Y = hexExpectedQy.HexToByteArray() };
            (ECDsa ecDsa, HashAlgorithmName? name) = CborCoseKeyHelpers.ParseECDsaPublicKey(hexEncoding.HexToByteArray());

            using ECDsa _ = ecDsa;

            ECParameters ecParams = ecDsa.ExportParameters(includePrivateParameters: false);

            string? expectedCurveFriendlyName = NormalizeCurveForPlatform(curveFriendlyName).Oid.FriendlyName;

            Assert.True(ecParams.Curve.IsNamed);
            Assert.Equal(expectedCurveFriendlyName, ecParams.Curve.Oid.FriendlyName);
            Assert.Equal(q.X, ecParams.Q.X);
            Assert.Equal(q.Y, ecParams.Q.Y);
            Assert.Equal(expectedHashAlgorithmName, name?.Name);

            static ECCurve NormalizeCurveForPlatform(string friendlyName)
            {
                ECCurve namedCurve = ECCurve.CreateFromFriendlyName(friendlyName);
                using ECDsa ecDsa = ECDsa.Create(namedCurve);
                ECParameters platformParams = ecDsa.ExportParameters(includePrivateParameters: false);
                return platformParams.Curve;
            }
        }
    }
}
