// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public class AsciiPreescapedDataTests
    {
        [Fact]
        public void AllowedAsciiCodePointsTestBattery()
        {
            OptimizedInboxTextEncoder._RunAsciiPreescapedDataTestBattery();
        }
    }
}

namespace System.Text.Encodings.Web
{
    internal partial class OptimizedInboxTextEncoder
    {
        private static readonly ulong[] _expected = new ulong[]
        {
            0x01_00_00_00_00_00_00_41, // "A......1" (where . = 0x00)
            0x02_00_00_00_00_00_62_61, // "ab.....2"
            0x03_00_00_00_00_45_44_43, // "CDE....3"
            0x04_00_00_00_66_65_64_63, // "cdef...4"
            0x05_00_00_4B_4A_49_48_47, // "GHIJK..5"
            0x06_00_6C_6B_6A_69_68_67, // "ghijkl.6"
        };

        internal static void _RunAsciiPreescapedDataTestBattery()
        {
            // Arrange
            // Allow only characters that are *not* multiples of 7 (relatively prime to 6, ensuring every index of our test array is hit)

            static bool IsValueAllowed(int value) => (value % 7) != 0;

            var bitmap = new AllowedBmpCodePointsBitmap();
            for (int i = 0; i < 1024; i++) // include C0 controls & characters beyond ASCII range
            {
                if (IsValueAllowed(i)) { bitmap.AllowChar((char)i); }
            }

            using BoundedMemory<AsciiPreescapedData> boundedMemory = BoundedMemory.Allocate<AsciiPreescapedData>(1); // use BoundedMemory to detect out-of-bound accesses
            ref var preescapedData = ref boundedMemory.Span[0];
            preescapedData.PopulatePreescapedData(bitmap, new TestEscaper());
            boundedMemory.MakeReadonly();

            // Assert
            // Try ASCII chars first

            ulong preescapedEntry;
            for (int i = 0; i < 128; i++)
            {
                ulong iterExpected;
                bool mustEscape = char.IsControl((char)i) || !IsValueAllowed(i);
                if (mustEscape)
                {
                    iterExpected = _expected[(uint)i % _expected.Length]; // char must be escaped, look up which value we're expecting
                }
                else
                {
                    iterExpected = (0x01ul << 56) + (uint)i; // 0x01_00_00_00_00_00_00_XX, meaning char can go unescaped
                }
                Assert.True(preescapedData.TryGetPreescapedData((uint)i, out preescapedEntry), "All ASCII code points must return true.");
                Assert.Equal(iterExpected, preescapedEntry);
            }

            // Some known test cases

            Assert.True(preescapedData.TryGetPreescapedData('L' /* = 76 dec, not multiple of 7, allowed */, out preescapedEntry));
            Assert.Equal(0x01_00_00_00_00_00_00_4Cul /* "1......L" */, preescapedEntry);
            Assert.True(preescapedData.TryGetPreescapedData('M' /* = 77 dec, multiple of 7, disallowed (use index 77 % 6 = 5) */, out preescapedEntry));
            Assert.Equal(0x06_00_6C_6B_6A_69_68_67ul /* "ghijkl.6" */, preescapedEntry);
            Assert.True(preescapedData.TryGetPreescapedData('N' /* = 78 dec, not multiple of 7, allowed */, out preescapedEntry));
            Assert.Equal(0x01_00_00_00_00_00_00_4Eul /* "N......1" */, preescapedEntry);

            // And try some non-ASCII edge cases, all of which must return false

            Assert.False(preescapedData.TryGetPreescapedData(128, out _));
            Assert.False(preescapedData.TryGetPreescapedData(256, out _));
            Assert.False(preescapedData.TryGetPreescapedData(char.MaxValue, out _));
            Assert.False(preescapedData.TryGetPreescapedData(char.MaxValue + 1, out _));
            Assert.False(preescapedData.TryGetPreescapedData(int.MaxValue, out _));
            Assert.False(preescapedData.TryGetPreescapedData((uint)int.MaxValue + 1, out _));
            Assert.False(preescapedData.TryGetPreescapedData(uint.MaxValue, out _));
        }

        private class TestEscaper : ScalarEscaperBase
        {
            // tests the different lengths from 0 - 6
            private static readonly string[] _encodings = new string[]
            {
                "A",
                "ab",
                "CDE",
                "cdef",
                "GHIJK",
                "ghijkl",
            };

            internal override int EncodeUtf16(Rune value, Span<char> destination)
            {
                string encoding = _encodings[value.Value % _encodings.Length];
                encoding.AsSpan().CopyTo(destination);
                return encoding.Length;
            }

            internal override int EncodeUtf8(Rune value, Span<byte> destination) => throw new NotImplementedException();
        }
    }
}
