// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Text.Unicode.Tests
{
    public class Utf16UtilityTests
    {
        private unsafe delegate char* GetPointerToFirstInvalidCharDel(char* pInputBuffer, int inputLength, out long utf8CodeUnitCountAdjustment, out int scalarCountAdjustment);
        private static readonly Lazy<GetPointerToFirstInvalidCharDel> _getPointerToFirstInvalidCharFn = CreateGetPointerToFirstInvalidCharFn();

        [Theory]
        [InlineData("", 0, 0)] // empty string is OK
        [InlineData("X", 1, 1)]
        [InlineData("XY", 2, 2)]
        [InlineData("XYZ", 3, 3)]
        [InlineData("<EACU>", 1, 2)]
        [InlineData("X<EACU>", 2, 3)]
        [InlineData("<EACU>X", 2, 3)]
        [InlineData("<EURO>", 1, 3)]
        [InlineData("<GRIN>", 1, 4)]
        [InlineData("X<GRIN>Z", 3, 6)]
        [InlineData("X<0000>Z", 3, 3)] // null chars are allowed
        public void GetIndexOfFirstInvalidUtf16Sequence_WithSmallValidBuffers(string unprocessedInput, int expectedRuneCount, int expectedUtf8ByteCount)
        {
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(unprocessedInput, -1 /* expectedIdxOfFirstInvalidChar */, expectedRuneCount, expectedUtf8ByteCount);
        }

        [Theory]
        [InlineData("<DC00>", 0, 0, 0)] // standalone low surrogate (at beginning of sequence)
        [InlineData("X<DC00>", 1, 1, 1)] // standalone low surrogate (preceded by valid ASCII data)
        [InlineData("<EURO><DC00>", 1, 1, 3)] // standalone low surrogate (preceded by valid non-ASCII data)
        [InlineData("<D800>", 0, 0, 0)] // standalone high surrogate (missing follow-up low surrogate)
        [InlineData("<D800>Y", 0, 0, 0)] // standalone high surrogate (followed by ASCII char)
        [InlineData("<D800><D800>", 0, 0, 0)] // standalone high surrogate (followed by high surrogate)
        [InlineData("<D800><EURO>", 0, 0, 0)] // standalone high surrogate (followed by valid non-ASCII char)
        [InlineData("<DC00><DC00>", 0, 0, 0)] // standalone low surrogate (not preceded by a high surrogate)
        [InlineData("<DC00><D800>", 0, 0, 0)] // standalone low surrogate (not preceded by a high surrogate)
        [InlineData("<GRIN><DC00><DC00>", 2, 1, 4)] // standalone low surrogate (preceded by a valid surrogate pair)
        [InlineData("<GRIN><DC00><D800>", 2, 1, 4)] // standalone low surrogate (preceded by a valid surrogate pair)
        [InlineData("<GRIN><0000><DC00><D800>", 3, 2, 5)] // standalone low surrogate (preceded by a valid null char)
        public void GetIndexOfFirstInvalidUtf16Sequence_WithSmallInvalidBuffers(string unprocessedInput, int idxOfFirstInvalidChar, int expectedRuneCount, int expectedUtf8ByteCount)
        {
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(unprocessedInput, idxOfFirstInvalidChar, expectedRuneCount, expectedUtf8ByteCount);
        }

        [Theory] // chars below presented as hex since Xunit doesn't like invalid UTF-16 string literals
        [InlineData("<2BB4><218C><1BC0><613F><F9E9><B740><DE38><E689>", 6, 6, 18)]
        [InlineData("<1854><C980><012C><4797><DD5A><41D0><A104><5464>", 4, 4, 11)]
        [InlineData("<F1AF><8BD3><5037><BE29><DEFF><3E3A><DD71><6336>", 4, 4, 12)]
        [InlineData("<B978><0F25><DC23><D3BB><7352><4025><0B93><4107>", 2, 2, 6)]
        [InlineData("<887C><C980><012C><4797><DD5A><41D0><A104><5464>", 4, 4, 11)]
        public void GetIndexOfFirstInvalidUtf16Sequence_WithEightRandomCharsContainingUnpairedSurrogates(string unprocessedInput, int idxOfFirstInvalidChar, int expectedRuneCount, int expectedUtf8ByteCount)
        {
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(unprocessedInput, idxOfFirstInvalidChar, expectedRuneCount, expectedUtf8ByteCount);
        }

        [Fact]
        public void GetIndexOfFirstInvalidUtf16Sequence_WithInvalidSurrogateSequences()
        {
            // All ASCII

            char[] chars = Enumerable.Repeat('x', 128).ToArray();
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(chars, -1, expectedRuneCount: 128, expectedUtf8ByteCount: 128);

            // Throw a surrogate pair at the beginning

            chars[0] = '\uD800';
            chars[1] = '\uDFFF';
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(chars, -1, expectedRuneCount: 127, expectedUtf8ByteCount: 130);

            // Throw a surrogate pair near the end

            chars[124] = '\uD800';
            chars[125] = '\uDFFF';
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(chars, -1, expectedRuneCount: 126, expectedUtf8ByteCount: 132);

            // Throw a standalone surrogate code point at the *very* end

            chars[127] = '\uD800'; // high surrogate
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(chars, 127, expectedRuneCount: 125, expectedUtf8ByteCount: 131);

            chars[127] = '\uDFFF'; // low surrogate
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(chars, 127, expectedRuneCount: 125, expectedUtf8ByteCount: 131);

            // Make the final surrogate pair valid

            chars[126] = '\uD800'; // high surrogate
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(chars, -1, expectedRuneCount: 125, expectedUtf8ByteCount: 134);

            // Throw an invalid surrogate sequence in the middle (straddles a vector boundary)

            chars[12] = '\u0080'; // 2-byte UTF-8 sequence
            chars[13] = '\uD800'; // high surrogate
            chars[14] = '\uD800'; // high surrogate
            chars[15] = '\uDFFF'; // low surrogate
            chars[16] = '\uDFFF'; // low surrogate
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(chars, 13, expectedRuneCount: 12, expectedUtf8ByteCount: 16);

            // Correct the surrogate sequence we just added

            chars[14] = '\uDC00'; // low surrogate
            chars[15] = '\uDBFF'; // high surrogate
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(chars, -1, expectedRuneCount: 123, expectedUtf8ByteCount: 139);

            // Corrupt the surrogate pair that's split across a vector boundary

            chars[16] = 'x'; // ASCII char (remember.. chars[15] is a high surrogate char)
            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(chars, 15, expectedRuneCount: 13, expectedUtf8ByteCount: 20);
        }

        [Fact]
        public void GetIndexOfFirstInvalidUtf16Sequence_WithStandaloneLowSurrogateCharAtStart()
        {
            // The input stream will be a vector's worth of ASCII chars, followed by a single standalone low
            // surrogate char, then padded with U+0000 until it's a multiple of the vector size.
            // Using Vector<ushort>.Count here as a stand-in for Vector<char>.Count.

            char[] chars = new char[Vector<ushort>.Count * 2];
            for (int i = 0; i < Vector<ushort>.Count; i++)
            {
                chars[i] = 'x'; // ASCII char
            }

            chars[Vector<ushort>.Count] = '\uDEAD'; // standalone low surrogate char

            for (int i = 0; i <= Vector<ushort>.Count; i++)
            {
                // Expect all ASCII chars to be consumed, low surrogate char to be marked invalid.
                GetIndexOfFirstInvalidUtf16Sequence_Test_Core(chars[(Vector<ushort>.Count - i)..], i, i, i);
            }
        }

        private static void GetIndexOfFirstInvalidUtf16Sequence_Test_Core(string unprocessedInput, int expectedIdxOfFirstInvalidChar, int expectedRuneCount, long expectedUtf8ByteCount)
        {
            char[] processedInput = ProcessInput(unprocessedInput).ToCharArray();

            // Run the test normally

            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(processedInput, expectedIdxOfFirstInvalidChar, expectedRuneCount, expectedUtf8ByteCount);

            // Put a bunch of ASCII data at the beginning (to test the call to ASCIIUtility at method entry)

            processedInput = Enumerable.Repeat('x', 128).Concat(processedInput).ToArray();

            if (expectedIdxOfFirstInvalidChar >= 0)
            {
                expectedIdxOfFirstInvalidChar += 128;
            }
            expectedRuneCount += 128;
            expectedUtf8ByteCount += 128;

            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(processedInput, expectedIdxOfFirstInvalidChar, expectedRuneCount, expectedUtf8ByteCount);

            // Change the first few chars to a mixture of 2-byte and 3-byte UTF-8 sequences
            // This makes sure the vectorized code paths can properly handle these.

            processedInput[0] = '\u0080'; // 2-byte UTF-8 sequence
            processedInput[1] = '\u0800'; // 3-byte UTF-8 sequence
            processedInput[2] = '\u0080'; // 2-byte UTF-8 sequence
            processedInput[3] = '\u8000'; // 3-byte UTF-8 sequence (a negative number, when signed)
            processedInput[4] = '\u0080'; // 2-byte UTF-8 sequence
            processedInput[5] = '\u0800'; // 3-byte UTF-8 sequence
            processedInput[6] = '\u0080'; // 2-byte UTF-8 sequence
            processedInput[7] = '\u8880'; // 3-byte UTF-8 sequence (a negative number, when signed, with 0x0800 and 0x0080 marker bits also set)

            expectedUtf8ByteCount += 12;

            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(processedInput, expectedIdxOfFirstInvalidChar, expectedRuneCount, expectedUtf8ByteCount);

            // Throw some surrogate pairs into the mix to make sure they're also handled properly
            // by the vectorized code paths.

            processedInput[8] = '\u0080'; // 2-byte UTF-8 sequence
            processedInput[9] = '\u0800'; // 3-byte UTF-8 sequence
            processedInput[10] = '\u0080'; // 2-byte UTF-8 sequence
            processedInput[11] = '\u0800'; // 3-byte UTF-8 sequence
            processedInput[12] = '\u0080'; // 2-byte UTF-8 sequence
            processedInput[13] = '\uD800'; // high surrogate
            processedInput[14] = '\uDC00'; // low surrogate
            processedInput[15] = 'z'; // ASCII char

            expectedRuneCount--;
            expectedUtf8ByteCount += 9;

            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(processedInput, expectedIdxOfFirstInvalidChar, expectedRuneCount, expectedUtf8ByteCount);

            // Split the next surrogate pair across the vector boundary (so that we
            // don't inadvertently treat this as a standalone surrogate sequence).

            processedInput[15] = '\uDBFF'; // high surrogate
            processedInput[16] = '\uDFFF'; // low surrogate

            expectedRuneCount--;
            expectedUtf8ByteCount += 2;

            GetIndexOfFirstInvalidUtf16Sequence_Test_Core(processedInput, expectedIdxOfFirstInvalidChar, expectedRuneCount, expectedUtf8ByteCount);
        }

        private static unsafe void GetIndexOfFirstInvalidUtf16Sequence_Test_Core(char[] input, int expectedRetVal, int expectedRuneCount, long expectedUtf8ByteCount)
        {
            // Arrange

            using BoundedMemory<char> boundedMemory = BoundedMemory.AllocateFromExistingData(input);
            boundedMemory.MakeReadonly();

            // Act

            int actualRetVal;
            long actualUtf8CodeUnitCount;
            int actualRuneCount;

            fixed (char* pInputBuffer = &MemoryMarshal.GetReference(boundedMemory.Span))
            {
                char* pFirstInvalidChar = _getPointerToFirstInvalidCharFn.Value(pInputBuffer, input.Length, out long utf8CodeUnitCountAdjustment, out int scalarCountAdjustment);

                long ptrDiff = pFirstInvalidChar - pInputBuffer;
                Assert.True((ulong)ptrDiff <= (uint)input.Length, "ptrDiff was outside expected range.");

                Assert.True(utf8CodeUnitCountAdjustment >= 0, "UTF-16 code unit count adjustment must be non-negative.");
                Assert.True(scalarCountAdjustment <= 0, "Scalar count adjustment must be 0 or negative.");

                actualRetVal = (ptrDiff == input.Length) ? -1 : (int)ptrDiff;

                // The last two 'out' parameters are:
                // a) The number to be added to the "chars processed" return value to come up with the total UTF-8 code unit count, and
                // b) The number to be added to the "total UTF-16 code unit count" value to come up with the total scalar count.

                actualUtf8CodeUnitCount = ptrDiff + utf8CodeUnitCountAdjustment;
                actualRuneCount = (int)ptrDiff + scalarCountAdjustment;
            }

            // Assert

            Assert.Equal(expectedRetVal, actualRetVal);
            Assert.Equal(expectedRuneCount, actualRuneCount);
            Assert.Equal(actualUtf8CodeUnitCount, expectedUtf8ByteCount);
        }

        private static Lazy<GetPointerToFirstInvalidCharDel> CreateGetPointerToFirstInvalidCharFn()
        {
            return new Lazy<GetPointerToFirstInvalidCharDel>(() =>
            {
                Type utf16UtilityType = typeof(Utf8).Assembly.GetType("System.Text.Unicode.Utf16Utility");

                if (utf16UtilityType is null)
                {
                    throw new Exception("Couldn't find Utf16Utility type in System.Private.CoreLib.");
                }

                MethodInfo methodInfo = utf16UtilityType.GetMethod("GetPointerToFirstInvalidChar", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (methodInfo is null)
                {
                    throw new Exception("Couldn't find GetPointerToFirstInvalidChar method on Utf8Utility.");
                }

                return (GetPointerToFirstInvalidCharDel)methodInfo.CreateDelegate(typeof(GetPointerToFirstInvalidCharDel));
            });
        }

        private static string ProcessInput(string input)
        {
            input = input.Replace("<EACU>", "\u00E9", StringComparison.Ordinal); // U+00E9 LATIN SMALL LETTER E WITH ACUTE
            input = input.Replace("<EURO>", "\u20AC", StringComparison.Ordinal); // U+20AC EURO SIGN
            input = input.Replace("<GRIN>", "\U0001F600", StringComparison.Ordinal); //  U+1F600 GRINNING FACE

            // Replace <ABCD> with \uABCD. This allows us to flow potentially malformed
            // UTF-16 strings without Xunit. (The unit testing framework gets angry when
            // we try putting invalid UTF-16 data as inline test data.)

            int idx;
            while ((idx = input.IndexOf('<')) >= 0)
            {
                input = input[..idx] + (char)ushort.Parse(input.Substring(idx + 1, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture) + input[(idx + 6)..];
            }

            return input;
        }
    }
}
