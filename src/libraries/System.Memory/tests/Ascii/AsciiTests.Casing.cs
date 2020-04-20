// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

using static System.FormattableString;

namespace System.Buffers.Text.Tests
{
    public partial class AsciiUnitTests
    {
        private delegate void ChangeCaseInPlaceAction<T>(Span<T> span);
        private delegate int ChangeCaseFunc<T>(ReadOnlySpan<T> source, Span<T> destination);

        public static IEnumerable<(string input, string expectedUpper, string expectedLower)> ChangeCaseBytesTestData()
        {
            yield return ("", "", "");
            yield return ("Hello", "HELLO", "hello");
            yield return ("\rHello\n", "\rHELLO\n", "\rhello\n");
            yield return ("\0xyz\0", "\0XYZ\0", "\0xyz\0");
            yield return ("\0XYZ\0", "\0XYZ\0", "\0xyz\0");
            yield return ("\u00C0bCDe", "\u00C0BCDE", "\u00C0bcde"); // U+00C0 is not ASCII so doesn't change case
            yield return ("\u00E0bCDe", "\u00E0BCDE", "\u00E0bcde"); // U+00E0 is not ASCII so doesn't change case
        }

        public static IEnumerable<(string input, string expectedUpper, string expectedLower)> ChangeCaseCharsTestData()
        {
            yield return ("x\u0150X", "X\u0150X", "x\u0150x"); // U+0150 is not ASCII so doesn't change case
            yield return ("xO\u030BX", "XO\u030BX", "xo\u030Bx"); // base char modified by U+030B is ASCII so will change case
        }

        [Fact]
        public void ToLower_Byte_OverlappingBuffers_Throws()
        {
            byte[] buffer = new byte[10];

            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(buffer, buffer));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(buffer.AsSpan(1, 3), buffer.AsSpan(3, 5)));
        }

        [Fact]
        public void ToLower_Char_OverlappingBuffers_Throws()
        {
            char[] buffer = new char[10];

            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(buffer, buffer));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(buffer.AsSpan(1, 3), buffer.AsSpan(3, 5)));
        }

        [Fact]
        public void ToLower_SingleByte()
        {
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                byte expected = (byte)i;
                if (i >= 'A' && i <= 'Z')
                {
                    expected = (byte)(i - 'A' + 'a');
                }

                byte actual = Ascii.ToLower((byte)i);
                if (actual != expected)
                {
                    throw new AssertActualExpectedException(
                        expected: expected,
                        actual: actual,
                        userMessage: Invariant($"Unexpected result calling Ascii.ToLower((byte)0x{i:X2})."));
                }
            }
        }

        [Fact]
        public void ToLower_SingleChar()
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                char expected = (char)i;
                if (i >= 'A' && i <= 'Z')
                {
                    expected = (char)(i - 'A' + 'a');
                }

                char actual = Ascii.ToLower((char)i);
                if (actual != expected)
                {
                    throw new AssertActualExpectedException(
                        expected: expected,
                        actual: actual,
                        userMessage: Invariant($"Unexpected result calling Ascii.ToLower('\\u{i:X4}')."));
                }
            }
        }

        [Theory]
        [TupleMemberData(nameof(ChangeCaseBytesTestData))]
        public void ToLower_MultiByte(string input, string expectedLower)
        {
            byte[] inputBytes = CharsToAsciiBytesChecked(input);
            byte[] expectedLowerBytes = CharsToAsciiBytesChecked(expectedLower);

            RunChangeCaseInPlaceTest(inputBytes, expectedLowerBytes, Ascii.ToLowerInPlace);
            RunChangeCaseTest(inputBytes, expectedLowerBytes, Ascii.ToLower);
        }

        [Theory]
        [TupleMemberData(nameof(ChangeCaseBytesTestData))]
        [TupleMemberData(nameof(ChangeCaseCharsTestData))]
        public void ToLower_MultiChar(string input, string expectedLower)
        {
            char[] inputChars = input.ToCharArray();
            char[] expectedLowerChars = expectedLower.ToCharArray();

            RunChangeCaseInPlaceTest(inputChars, expectedLowerChars, Ascii.ToLowerInPlace);
            RunChangeCaseTest(inputChars, expectedLowerChars, Ascii.ToLower);
        }

        [Fact]
        public void ToUpper_Byte_OverlappingBuffers_Throws()
        {
            byte[] buffer = new byte[10];

            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(buffer, buffer));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(buffer.AsSpan(1, 3), buffer.AsSpan(3, 5)));
        }

        [Fact]
        public void ToUpper_Char_OverlappingBuffers_Throws()
        {
            char[] buffer = new char[10];

            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(buffer, buffer));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(buffer.AsSpan(1, 3), buffer.AsSpan(3, 5)));
        }

        [Fact]
        public void ToUpper_SingleByte()
        {
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                byte expected = (byte)i;
                if (i >= 'a' && i <= 'z')
                {
                    expected = (byte)(i - 'a' + 'A');
                }

                byte actual = Ascii.ToUpper((byte)i);
                if (actual != expected)
                {
                    throw new AssertActualExpectedException(
                        expected: expected,
                        actual: actual,
                        userMessage: Invariant($"Unexpected result calling Ascii.ToUpper((byte)0x{i:X2})."));
                }
            }
        }

        [Fact]
        public void ToUpper_SingleChar()
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                char expected = (char)i;
                if (i >= 'a' && i <= 'z')
                {
                    expected = (char)(i - 'a' + 'A');
                }

                char actual = Ascii.ToUpper((char)i);
                if (actual != expected)
                {
                    throw new AssertActualExpectedException(
                        expected: expected,
                        actual: actual,
                        userMessage: Invariant($"Unexpected result calling Ascii.ToUpper('\\u{i:X4}')."));
                }
            }
        }

        [Theory]
        [TupleMemberData(nameof(ChangeCaseBytesTestData))]
        public void ToUpper_MultiByte(string input, string expectedUpper)
        {
            byte[] inputBytes = CharsToAsciiBytesChecked(input);
            byte[] expectedUpperBytes = CharsToAsciiBytesChecked(expectedUpper);

            RunChangeCaseInPlaceTest(inputBytes, expectedUpperBytes, Ascii.ToUpperInPlace);
            RunChangeCaseTest(inputBytes, expectedUpperBytes, Ascii.ToUpper);
        }

        [Theory]
        [TupleMemberData(nameof(ChangeCaseBytesTestData))]
        [TupleMemberData(nameof(ChangeCaseCharsTestData))]
        public void ToUpper_MultiChar(string input, string expectedUpper)
        {
            char[] inputChars = input.ToCharArray();
            char[] expectedUpperChars = expectedUpper.ToCharArray();

            RunChangeCaseInPlaceTest(inputChars, expectedUpperChars, Ascii.ToUpperInPlace);
            RunChangeCaseTest(inputChars, expectedUpperChars, Ascii.ToUpper);
        }

        private static void RunChangeCaseInPlaceTest<T>(T[] inputData, T[] expectedResult, ChangeCaseInPlaceAction<T> changeCaseInPlaceAction) where T : unmanaged
        {
            using (BoundedMemory<T> boundedMem = BoundedMemory.AllocateFromExistingData(inputData, PoisonPagePlacement.Before))
            {
                changeCaseInPlaceAction(boundedMem.Span);
                Assert.Equal(expectedResult, boundedMem.Span.ToArray());
            }
            using (BoundedMemory<T> boundedMem = BoundedMemory.AllocateFromExistingData(inputData, PoisonPagePlacement.After))
            {
                changeCaseInPlaceAction(boundedMem.Span);
                Assert.Equal(expectedResult, boundedMem.Span.ToArray());
            }
        }

        private static void RunChangeCaseTest<T>(T[] inputData, T[] expectedResult, ChangeCaseFunc<T> changeCaseFunc) where T : unmanaged
        {
            // First, make sure the method throws an exception if the destination buffer is too small

            if (expectedResult.Length > 0)
            {
                Assert.Throws<ArgumentException>(() => changeCaseFunc(inputData, new T[expectedResult.Length - 1]));
            }

            using BoundedMemory<T> sourceMem = BoundedMemory.AllocateFromExistingData(inputData);
            sourceMem.MakeReadonly();

            using BoundedMemory<T> destMem = BoundedMemory.Allocate<T>(expectedResult.Length + 1);
            Span<T> destSpan = destMem.Span;

            // Then try the operation with an exactly-sized span.

            int actualResultElements = changeCaseFunc(sourceMem.Span, destSpan[1..]);
            Assert.Equal(expectedResult, destSpan.Slice(1, actualResultElements).ToArray());

            // Then try the operation with an overlong span.
            // (Shouldn't overwrite the final element.)

            destSpan[^1] = (T)((IConvertible)0xFF).ToType(typeof(T), null); // = 0xFF or U+00FF
            actualResultElements = changeCaseFunc(sourceMem.Span, destSpan);
            Assert.Equal(expectedResult, destSpan.Slice(0, actualResultElements).ToArray());
            Assert.Equal(0xFF, Convert.ToByte(destSpan[^1])); // validate last element wasn't overwritten
        }
    }
}
