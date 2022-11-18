// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Text.Unicode.Tests
{
    public class Utf8UtilityTests
    {
        private unsafe delegate byte* GetPointerToFirstInvalidByteDel(byte* pInputBuffer, int inputLength, out int utf16CodeUnitCountAdjustment, out int scalarCountAdjustment);
        private static readonly Lazy<GetPointerToFirstInvalidByteDel> _getPointerToFirstInvalidByteFn = CreateGetPointerToFirstInvalidByteFn();

        private const string X = "58"; // U+0058 LATIN CAPITAL LETTER X, 1 byte
        private const string Y = "59"; // U+0058 LATIN CAPITAL LETTER Y, 1 byte
        private const string Z = "5A"; // U+0058 LATIN CAPITAL LETTER Z, 1 byte
        private const string E_ACUTE = "C3A9"; // U+00E9 LATIN SMALL LETTER E WITH ACUTE, 2 bytes
        private const string EURO_SYMBOL = "E282AC"; // U+20AC EURO SIGN, 3 bytes
        private const string GRINNING_FACE = "F09F9880"; // U+1F600 GRINNING FACE, 4 bytes

        [Theory]
        [InlineData("", 0, 0)] // empty string is OK
        [InlineData(X, 1, 0)]
        [InlineData(X + Y, 2, 0)]
        [InlineData(X + Y + Z, 3, 0)]
        [InlineData(E_ACUTE, 1, 0)]
        [InlineData(X + E_ACUTE, 2, 0)]
        [InlineData(E_ACUTE + X, 2, 0)]
        [InlineData(EURO_SYMBOL, 1, 0)]
        public void GetIndexOfFirstInvalidUtf8Sequence_WithSmallValidBuffers(string input, int expectedRuneCount, int expectedSurrogatePairCount)
        {
            // These test cases are for the "slow processing" code path at the end of GetIndexOfFirstInvalidUtf8Sequence,
            // so inputs should be less than 4 bytes.

            Assert.InRange(input.Length, 0, 6);

            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(input, -1 /* expectedRetVal */, expectedRuneCount, expectedSurrogatePairCount);
        }

        [Theory]
        [InlineData("80", 0, 0, 0)] // sequence cannot begin with continuation character
        [InlineData("8182", 0, 0, 0)] // sequence cannot begin with continuation character
        [InlineData("838485", 0, 0, 0)] // sequence cannot begin with continuation character
        [InlineData(X + "80", 1, 1, 0)] // sequence cannot begin with continuation character
        [InlineData(X + "8182", 1, 1, 0)] // sequence cannot begin with continuation character
        [InlineData("C0", 0, 0, 0)] // [ C0 ] is always invalid
        [InlineData("C080", 0, 0, 0)] // [ C0 ] is always invalid
        [InlineData("C08081", 0, 0, 0)] // [ C0 ] is always invalid
        [InlineData(X + "C1", 1, 1, 0)] // [ C1 ] is always invalid
        [InlineData(X + "C180", 1, 1, 0)] // [ C1 ] is always invalid
        [InlineData("C2", 0, 0, 0)] // [ C2 ] is improperly terminated
        [InlineData(X + "C27F", 1, 1, 0)] // [ C2 ] is improperly terminated
        [InlineData(X + "E282", 1, 1, 0)] // [ E2 82 ] is improperly terminated
        [InlineData("E2827F", 0, 0, 0)] // [ E2 82 ] is improperly terminated
        [InlineData("E09F80", 0, 0, 0)] // [ E0 9F ... ] is overlong
        [InlineData("E0C080", 0, 0, 0)] // [ E0 ] is improperly terminated
        [InlineData("ED7F80", 0, 0, 0)] // [ ED ] is improperly terminated
        [InlineData("EDA080", 0, 0, 0)] // [ ED A0 ... ] is surrogate
        public void GetIndexOfFirstInvalidUtf8Sequence_WithSmallInvalidBuffers(string input, int expectedRetVal, int expectedRuneCount, int expectedSurrogatePairCount)
        {
            // These test cases are for the "slow processing" code path at the end of GetIndexOfFirstInvalidUtf8Sequence,
            // so inputs should be less than 4 bytes.

            Assert.InRange(input.Length, 0, 6);

            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(input, expectedRetVal, expectedRuneCount, expectedSurrogatePairCount);
        }

        [Theory]
        [InlineData(E_ACUTE + "21222324" + "303132333435363738393A3B3C3D3E3F", 21, 0)] // Loop unrolling at end of buffer
        [InlineData(E_ACUTE + "21222324" + "303132333435363738393A3B3C3D3E3F" + "3031323334353637" + E_ACUTE + "38393A3B3C3D3E3F", 38, 0)] // Loop unrolling interrupted by non-ASCII
        [InlineData("212223" + E_ACUTE + "30313233", 8, 0)] // 3 ASCII bytes followed by non-ASCII
        [InlineData("2122" + E_ACUTE + "30313233", 7, 0)] // 2 ASCII bytes followed by non-ASCII
        [InlineData("21" + E_ACUTE + "30313233", 6, 0)] // 1 ASCII byte followed by non-ASCII
        [InlineData(E_ACUTE + E_ACUTE + E_ACUTE + E_ACUTE, 4, 0)] // 4x 2-byte sequences, exercises optimization code path in 2-byte sequence processing
        [InlineData(E_ACUTE + E_ACUTE + E_ACUTE + "5051", 5, 0)] // 3x 2-byte sequences + 2 ASCII bytes, exercises optimization code path in 2-byte sequence processing
        [InlineData(E_ACUTE + "5051", 3, 0)] // single 2-byte sequence + 2 trailing ASCII bytes, exercises draining logic in 2-byte sequence processing
        [InlineData(E_ACUTE + "50" + E_ACUTE + "304050", 6, 0)] // single 2-byte sequences + 1 trailing ASCII byte + 2-byte sequence, exercises draining logic in 2-byte sequence processing
        [InlineData(EURO_SYMBOL + "20", 2, 0)] // single 3-byte sequence + 1 trailing ASCII byte, exercises draining logic in 3-byte sequence processing
        [InlineData(EURO_SYMBOL + "203040", 4, 0)] // single 3-byte sequence + 3 trailing ASCII byte, exercises draining logic and "running out of data" logic in 3-byte sequence processing
        [InlineData(EURO_SYMBOL + EURO_SYMBOL + EURO_SYMBOL, 3, 0)] // 3x 3-byte sequences, exercises "stay within 3-byte loop" logic in 3-byte sequence processing
        [InlineData(EURO_SYMBOL + EURO_SYMBOL + EURO_SYMBOL + EURO_SYMBOL, 4, 0)] // 4x 3-byte sequences, exercises "consume multiple bytes at a time" logic in 3-byte sequence processing
        [InlineData(EURO_SYMBOL + EURO_SYMBOL + EURO_SYMBOL + E_ACUTE, 4, 0)] // 3x 3-byte sequences + single 2-byte sequence, exercises "consume multiple bytes at a time" logic in 3-byte sequence processing
        [InlineData(EURO_SYMBOL + EURO_SYMBOL + E_ACUTE + E_ACUTE + E_ACUTE + E_ACUTE, 6, 0)] // 2x 3-byte sequences + 4x 2-byte sequences, exercises "consume multiple bytes at a time" logic in 3-byte sequence processing
        [InlineData(GRINNING_FACE + GRINNING_FACE, 2, 2)] // 2x 4-byte sequences, exercises 4-byte sequence processing
        [InlineData(GRINNING_FACE + "303132", 4, 1)] // single 4-byte sequence + 3 ASCII bytes, exercises 4-byte sequence processing and draining logic
        [InlineData("F09FA4B8" + "F09F8FBD" + "E2808D" + "E29980" + "EFB88F", 5, 2)] // U+1F938 U+1F3FD U+200D U+2640 U+FE0F WOMAN CARTWHEELING: MEDIUM SKIN TONE, exercising switching between multiple sequence lengths
        public void GetIndexOfFirstInvalidUtf8Sequence_WithLargeValidBuffers(string input, int expectedRuneCount, int expectedSurrogatePairCount)
        {
            // These test cases are for the "fast processing" code which is the main loop of GetIndexOfFirstInvalidUtf8Sequence,
            // so inputs should be less >= 4 bytes.

            Assert.True(input.Length >= 8);

            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(input, -1 /* expectedRetVal */, expectedRuneCount, expectedSurrogatePairCount);
        }

        [Theory]
        [InlineData("3031" + "80" + "202122232425", 2, 2, 0)] // Continuation character at start of sequence should match no bitmask
        [InlineData("3031" + "C080" + "2021222324", 2, 2, 0)] // Overlong 2-byte sequence at start of DWORD
        [InlineData("3031" + "C180" + "2021222324", 2, 2, 0)] // Overlong 2-byte sequence at start of DWORD
        [InlineData("C280" + "C180", 2, 1, 0)] // Overlong 2-byte sequence at end of DWORD
        [InlineData("C27F" + "C280", 0, 0, 0)] // Improperly terminated 2-byte sequence at start of DWORD
        [InlineData("C2C0" + "C280", 0, 0, 0)] // Improperly terminated 2-byte sequence at start of DWORD
        [InlineData("C280" + "C27F", 2, 1, 0)] // Improperly terminated 2-byte sequence at end of DWORD
        [InlineData("C280" + "C2C0", 2, 1, 0)] // Improperly terminated 2-byte sequence at end of DWORD
        [InlineData("C280" + "C280" + "80203040", 4, 2, 0)] // Continuation character at start of sequence, within "stay in 2-byte processing" optimization
        [InlineData("C280" + "C280" + "C180" + "C280", 4, 2, 0)] // Overlong 2-byte sequence at start of DWORD, within "stay in 2-byte processing" optimization
        [InlineData("C280" + "C280" + "C280" + "C180", 6, 3, 0)] // Overlong 2-byte sequence at end of DWORD, within "stay in 2-byte processing" optimization
        [InlineData("3031" + "E09F80" + EURO_SYMBOL + EURO_SYMBOL, 2, 2, 0)] // Overlong 3-byte sequence at start of DWORD
        [InlineData("3031" + "E07F80" + EURO_SYMBOL + EURO_SYMBOL, 2, 2, 0)] // Improperly terminated 3-byte sequence at start of DWORD
        [InlineData("3031" + "E0C080" + EURO_SYMBOL + EURO_SYMBOL, 2, 2, 0)] // Improperly terminated 3-byte sequence at start of DWORD
        [InlineData("3031" + "E17F80" + EURO_SYMBOL + EURO_SYMBOL, 2, 2, 0)] // Improperly terminated 3-byte sequence at start of DWORD
        [InlineData("3031" + "E1C080" + EURO_SYMBOL + EURO_SYMBOL, 2, 2, 0)] // Improperly terminated 3-byte sequence at start of DWORD
        [InlineData("3031" + "EDA080" + EURO_SYMBOL + EURO_SYMBOL, 2, 2, 0)] // Surrogate 3-byte sequence at start of DWORD
        [InlineData("3031" + "E69C88" + "E59B" + "E69C88", 5, 3, 0)] // Incomplete 3-byte sequence surrounded by valid 3-byte sequences
        [InlineData("E78B80" + "80", 3, 1, 0)] // Valid 3-byte sequence followed by standalone continuation byte
        [InlineData("3031" + "F5808080", 2, 2, 0)] // [ F5 ] is always invalid
        [InlineData("3031" + "F6808080", 2, 2, 0)] // [ F6 ] is always invalid
        [InlineData("3031" + "F7808080", 2, 2, 0)] // [ F7 ] is always invalid
        [InlineData("3031" + "F8808080", 2, 2, 0)] // [ F8 ] is always invalid
        [InlineData("3031" + "F9808080", 2, 2, 0)] // [ F9 ] is always invalid
        [InlineData("3031" + "FA808080", 2, 2, 0)] // [ FA ] is always invalid
        [InlineData("3031" + "FB808080", 2, 2, 0)] // [ FB ] is always invalid
        [InlineData("3031" + "FC808080", 2, 2, 0)] // [ FC ] is always invalid
        [InlineData("3031" + "FD808080", 2, 2, 0)] // [ FD ] is always invalid
        [InlineData("3031" + "FE808080", 2, 2, 0)] // [ FE ] is always invalid
        [InlineData("3031" + "FF808080", 2, 2, 0)] // [ FF ] is always invalid
        public void GetIndexOfFirstInvalidUtf8Sequence_WithLargeInvalidBuffers(string input, int expectedRetVal, int expectedRuneCount, int expectedSurrogatePairCount)
        {
            // These test cases are for the "fast processing" code which is the main loop of GetIndexOfFirstInvalidUtf8Sequence,
            // so inputs should be less >= 4 bytes.

            Assert.True(input.Length >= 8);

            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(input, expectedRetVal, expectedRuneCount, expectedSurrogatePairCount);
        }

        [Fact]
        public void GetIndexOfFirstInvalidUtf8Sequence_WithOverlongTwoByteSequences_ReturnsInvalid()
        {
            // [ C0 ] is never a valid byte, indicates overlong 2-byte sequence
            // We'll test that [ C0 ] [ 00..FF ] is treated as invalid

            for (int i = 0; i < 256; i++)
            {
                AssertIsInvalidTwoByteSequence(new byte[] { 0xC0, (byte)i });
            }

            // [ C1 ] is never a valid byte, indicates overlong 2-byte sequence
            // We'll test that [ C1 ] [ 00..FF ] is treated as invalid

            for (int i = 0; i < 256; i++)
            {
                AssertIsInvalidTwoByteSequence(new byte[] { 0xC1, (byte)i });
            }
        }

        [Fact]
        public void GetIndexOfFirstInvalidUtf8Sequence_WithImproperlyTerminatedTwoByteSequences_ReturnsInvalid()
        {
            // Test [ C2..DF ] [ 00..7F ] and [ C2..DF ] [ C0..FF ]

            for (int i = 0xC2; i < 0xDF; i++)
            {
                for (int j = 0; j < 0x80; j++)
                {
                    AssertIsInvalidTwoByteSequence(new byte[] { (byte)i, (byte)j });
                }
                for (int j = 0xC0; j < 0x100; j++)
                {
                    AssertIsInvalidTwoByteSequence(new byte[] { (byte)i, (byte)j });
                }
            }
        }

        [Fact]
        public void GetIndexOfFirstInvalidUtf8Sequence_WithOverlongThreeByteSequences_ReturnsInvalid()
        {
            // [ E0 ] [ 80..9F ] [ 80..BF ] is overlong 3-byte sequence

            for (int i = 0x00; i < 0xA0; i++)
            {
                AssertIsInvalidThreeByteSequence(new byte[] { 0xE0, (byte)i, 0x80 });
            }
        }

        [Fact]
        public void GetIndexOfFirstInvalidUtf8Sequence_WithSurrogateThreeByteSequences_ReturnsInvalid()
        {
            // [ ED ] [ A0..BF ] [ 80..BF ] is surrogate 3-byte sequence

            for (int i = 0xA0; i < 0x100; i++)
            {
                AssertIsInvalidThreeByteSequence(new byte[] { 0xED, (byte)i, 0x80 });
            }
        }

        [Fact]
        public void GetIndexOfFirstInvalidUtf8Sequence_WithImproperlyTerminatedThreeByteSequence_ReturnsInvalid()
        {
            // [ E0..EF ] [ 80..BF ] [ !(80..BF) ] is improperly terminated 3-byte sequence

            for (int i = 0xE0; i < 0xF0; i++)
            {
                for (int j = 0x00; j < 0x80; j++)
                {
                    // Use both '9F' and 'A0' to make sure at least one isn't caught by overlong / surrogate checks
                    AssertIsInvalidThreeByteSequence(new byte[] { (byte)i, 0x9F, (byte)j });
                    AssertIsInvalidThreeByteSequence(new byte[] { (byte)i, 0xA0, (byte)j });
                }
                for (int j = 0xC0; j < 0x100; j++)
                {
                    // Use both '9F' and 'A0' to make sure at least one isn't caught by overlong / surrogate checks
                    AssertIsInvalidThreeByteSequence(new byte[] { (byte)i, 0x9F, (byte)j });
                    AssertIsInvalidThreeByteSequence(new byte[] { (byte)i, 0xA0, (byte)j });
                }
            }
        }

        [Fact]
        public void GetIndexOfFirstInvalidUtf8Sequence_WithOverlongFourByteSequences_ReturnsInvalid()
        {
            // [ F0 ] [ 80..8F ] [ 80..BF ] [ 80..BF ] is overlong 4-byte sequence

            for (int i = 0x00; i < 0x90; i++)
            {
                AssertIsInvalidFourByteSequence(new byte[] { 0xF0, (byte)i, 0x80, 0x80 });
            }
        }

        [Fact]
        public void GetIndexOfFirstInvalidUtf8Sequence_WithOutOfRangeFourByteSequences_ReturnsInvalid()
        {
            // [ F4 ] [ 90..BF ] [ 80..BF ] [ 80..BF ] is out-of-range 4-byte sequence

            for (int i = 0x90; i < 0x100; i++)
            {
                AssertIsInvalidFourByteSequence(new byte[] { 0xF4, (byte)i, 0x80, 0x80 });
            }
        }

        [Fact]
        public void GetIndexOfFirstInvalidUtf8Sequence_WithInvalidFourByteSequence_ReturnsInvalid()
        {
            // [ F0..F4 ] [ !(80..BF) ] [ !(80..BF) ] [ !(80..BF) ] is improperly terminated 4-byte sequence

            for (int i = 0xF0; i < 0xF5; i++)
            {
                for (int j = 0x00; j < 0x80; j++)
                {
                    AssertIsInvalidFourByteSequence(new byte[] { (byte)i, (byte)j, 0x80, 0x80 });

                    // Use both '8F' and '90' to make sure at least one isn't caught by overlong / out-of-range checks
                    AssertIsInvalidFourByteSequence(new byte[] { (byte)i, 0x9F, (byte)j, 0x80 });
                    AssertIsInvalidFourByteSequence(new byte[] { (byte)i, 0xA0, (byte)j, 0x80 });

                    AssertIsInvalidFourByteSequence(new byte[] { (byte)i, 0x9F, 0x80, (byte)j });
                    AssertIsInvalidFourByteSequence(new byte[] { (byte)i, 0xA0, 0x80, (byte)j });
                }
                for (int j = 0xC0; j < 0x100; j++)
                {
                    AssertIsInvalidFourByteSequence(new byte[] { (byte)i, (byte)j, 0x80, 0x80 });

                    // Use both '8F' and '90' to make sure at least one isn't caught by overlong / out-of-range checks
                    AssertIsInvalidFourByteSequence(new byte[] { (byte)i, 0x9F, (byte)j, 0x80 });
                    AssertIsInvalidFourByteSequence(new byte[] { (byte)i, 0xA0, (byte)j, 0x80 });

                    AssertIsInvalidFourByteSequence(new byte[] { (byte)i, 0x9F, 0x80, (byte)j });
                    AssertIsInvalidFourByteSequence(new byte[] { (byte)i, 0xA0, 0x80, (byte)j });
                }
            }
        }

        private static void AssertIsInvalidTwoByteSequence(byte[] invalidSequence)
        {
            Assert.Equal(2, invalidSequence.Length);

            byte[] knownGoodBytes = Utf8Tests.DecodeHex(E_ACUTE);

            byte[] toTest = invalidSequence.Concat(invalidSequence).Concat(knownGoodBytes).ToArray(); // at start of first DWORD
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(toTest, 0, 0, 0);

            toTest = knownGoodBytes.Concat(invalidSequence).Concat(knownGoodBytes).ToArray(); // at end of first DWORD
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(toTest, 2, 1, 0);

            // Run the same tests but with extra data at the beginning so that we're inside one of
            // the 2-byte processing "hot loop" code paths.

            toTest = knownGoodBytes.Concat(knownGoodBytes).Concat(invalidSequence).Concat(knownGoodBytes).ToArray(); // at start of next DWORD
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(toTest, 4, 2, 0);

            toTest = knownGoodBytes.Concat(knownGoodBytes).Concat(knownGoodBytes).Concat(invalidSequence).Concat(knownGoodBytes).ToArray(); // at end of next DWORD
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(toTest, 6, 3, 0);
        }

        private static void AssertIsInvalidThreeByteSequence(byte[] invalidSequence)
        {
            Assert.Equal(3, invalidSequence.Length);

            byte[] knownGoodBytes = Utf8Tests.DecodeHex(EURO_SYMBOL);

            byte[] toTest = invalidSequence.Concat(invalidSequence).Concat(knownGoodBytes).ToArray(); // at start of first DWORD
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(toTest, 0, 0, 0);

            // Run the same tests but with extra data at the beginning so that we're inside one of
            // the 3-byte processing "hot loop" code paths.

            toTest = knownGoodBytes.Concat(invalidSequence).Concat(knownGoodBytes).ToArray(); // straddling first and second DWORDs
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(toTest, 3, 1, 0);

            toTest = knownGoodBytes.Concat(knownGoodBytes).Concat(invalidSequence).Concat(knownGoodBytes).ToArray(); // straddling second and third DWORDs
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(toTest, 6, 2, 0);

            toTest = knownGoodBytes.Concat(knownGoodBytes).Concat(knownGoodBytes).Concat(invalidSequence).Concat(knownGoodBytes).ToArray(); // at end of third DWORD
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(toTest, 9, 3, 0);
        }

        private static void AssertIsInvalidFourByteSequence(byte[] invalidSequence)
        {
            Assert.Equal(4, invalidSequence.Length);

            byte[] knownGoodBytes = Utf8Tests.DecodeHex(GRINNING_FACE);

            byte[] toTest = invalidSequence.Concat(invalidSequence).Concat(knownGoodBytes).ToArray();
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(toTest, 0, 0, 0);

            toTest = knownGoodBytes.Concat(invalidSequence).Concat(knownGoodBytes).ToArray();
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(toTest, 4, 1, 1);
        }

        private static void GetIndexOfFirstInvalidUtf8Sequence_Test_Core(string inputHex, int expectedRetVal, int expectedRuneCount, int expectedSurrogatePairCount)
        {
            byte[] inputBytes = Utf8Tests.DecodeHex(inputHex);

            // Run the test normally
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(inputBytes, expectedRetVal, expectedRuneCount, expectedSurrogatePairCount);

            // Then run the test with a bunch of ASCII data at the beginning (to exercise the vectorized code paths)
            inputBytes = Enumerable.Repeat((byte)'x', 128).Concat(inputBytes).ToArray();
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(inputBytes, (expectedRetVal < 0) ? expectedRetVal : (expectedRetVal + 128), expectedRuneCount + 128, expectedSurrogatePairCount);

            // Then put a few more ASCII bytes at the beginning (to test that offsets are properly handled)
            inputBytes = Enumerable.Repeat((byte)'x', 7).Concat(inputBytes).ToArray();
            GetIndexOfFirstInvalidUtf8Sequence_Test_Core(inputBytes, (expectedRetVal < 0) ? expectedRetVal : (expectedRetVal + 135), expectedRuneCount + 135, expectedSurrogatePairCount);
        }

        private static unsafe void GetIndexOfFirstInvalidUtf8Sequence_Test_Core(byte[] input, int expectedRetVal, int expectedRuneCount, int expectedSurrogatePairCount)
        {
            // Arrange

            using BoundedMemory<byte> boundedMemory = BoundedMemory.AllocateFromExistingData(input);
            boundedMemory.MakeReadonly();

            // Act

            int actualRetVal;
            int actualSurrogatePairCount;
            int actualRuneCount;

            fixed (byte* pInputBuffer = &MemoryMarshal.GetReference(boundedMemory.Span))
            {
                byte* pFirstInvalidByte = _getPointerToFirstInvalidByteFn.Value(pInputBuffer, input.Length, out int utf16CodeUnitCountAdjustment, out int scalarCountAdjustment);

                long ptrDiff = pFirstInvalidByte - pInputBuffer;
                Assert.True((ulong)ptrDiff <= (uint)input.Length, "ptrDiff was outside expected range.");

                Assert.True(utf16CodeUnitCountAdjustment <= 0, "UTF-16 code unit count adjustment must be 0 or negative.");
                Assert.True(scalarCountAdjustment <= 0, "Scalar count adjustment must be 0 or negative.");

                actualRetVal = (ptrDiff == input.Length) ? -1 : (int)ptrDiff;

                // The last two 'out' parameters are:
                // a) The number to be added to the "bytes processed" return value to come up with the total UTF-16 code unit count, and
                // b) The number to be added to the "total UTF-16 code unit count" value to come up with the total scalar count.

                int totalUtf16CodeUnitCount = (int)ptrDiff + utf16CodeUnitCountAdjustment;
                actualRuneCount = totalUtf16CodeUnitCount + scalarCountAdjustment;

                // Surrogate pair count is number of UTF-16 code units less the number of scalars.

                actualSurrogatePairCount = totalUtf16CodeUnitCount - actualRuneCount;
            }

            // Assert

            Assert.Equal(expectedRetVal, actualRetVal);
            Assert.Equal(expectedRuneCount, actualRuneCount);
            Assert.Equal(expectedSurrogatePairCount, actualSurrogatePairCount);
        }

        private static Lazy<GetPointerToFirstInvalidByteDel> CreateGetPointerToFirstInvalidByteFn()
        {
            return new Lazy<GetPointerToFirstInvalidByteDel>(() =>
            {
                Type utf8UtilityType = Type.GetType("System.Text.Unicode.Utf8Utility, System.Private.CoreLib");

                if (utf8UtilityType is null)
                {
                    throw new Exception("Couldn't find Utf8Utility type in System.Private.CoreLib.");
                }

                MethodInfo methodInfo = utf8UtilityType.GetMethod("GetPointerToFirstInvalidByte", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (methodInfo is null)
                {
                    throw new Exception("Couldn't find GetPointerToFirstInvalidByte method on Utf8Utility.");
                }

                return (GetPointerToFirstInvalidByteDel)methodInfo.CreateDelegate(typeof(GetPointerToFirstInvalidByteDel));
            });
        }
    }
}
