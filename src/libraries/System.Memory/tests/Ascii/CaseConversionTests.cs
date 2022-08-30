// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class CaseConversionTests
    {
        private const byte MaxValidAsciiChar = 127;

        [Fact]
        public void OverlappingBuffers_Throws()
        {
            byte[] byteBuffer = new byte[10];
            char[] charBuffer = new char[10];

            // byte -> byte
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(byteBuffer, byteBuffer, out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(byteBuffer.AsSpan(1, 3), byteBuffer.AsSpan(3, 5), out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(byteBuffer, byteBuffer, out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(byteBuffer.AsSpan(1, 3), byteBuffer.AsSpan(3, 5), out _, out _));
            // byte -> char
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(byteBuffer, MemoryMarshal.Cast<byte, char>(byteBuffer), out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(byteBuffer, MemoryMarshal.Cast<byte, char>(byteBuffer).Slice(3, 5), out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(byteBuffer, MemoryMarshal.Cast<byte, char>(byteBuffer), out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(byteBuffer, MemoryMarshal.Cast<byte, char>(byteBuffer).Slice(3, 5), out _, out _));
            // char -> char
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(charBuffer, charBuffer, out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(charBuffer.AsSpan(1, 3), charBuffer.AsSpan(3, 5), out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(charBuffer, charBuffer, out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(charBuffer.AsSpan(1, 3), charBuffer.AsSpan(3, 5), out _, out _));
            // char -> byte
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(charBuffer, MemoryMarshal.Cast<char, byte>(charBuffer), out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(charBuffer, MemoryMarshal.Cast<char, byte>(charBuffer).Slice(3, 5), out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(charBuffer, MemoryMarshal.Cast<char, byte>(charBuffer), out _, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(charBuffer, MemoryMarshal.Cast<char, byte>(charBuffer).Slice(3, 5), out _, out _));
        }

        private static void VerifySingleChar<T>(OperationStatus status, int value, T expected, T actual, int consumed, int written)
        {
            Assert.True(typeof(T) == typeof(char) || typeof(T) == typeof(byte));

            if (value <= MaxValidAsciiChar)
            {
                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(expected, actual);
                Assert.Equal(1, consumed);
                Assert.Equal(1, written);
            }
            else
            {
                Assert.Equal(OperationStatus.InvalidData, status);
                Assert.Equal(default, actual);
                Assert.Equal(0, consumed);
                Assert.Equal(0, written);
            }
        }

        [Fact]
        public void SingleByteConversion()
        {
            byte[] destinationByte = new byte[1];
            char[] destinationChar = new char[1];

            for (int i = 0; i <= byte.MaxValue; i++)
            {
                byte expectedToLower = char.IsBetween((char)i, 'A', 'Z') ? (byte)(i - 'A' + 'a') : (byte)i;
                byte expectedToUpper= char.IsBetween((char)i, 'a', 'z') ? (byte)(i + 'A' + 'a') : (byte)i;

                byte[] sourceByte = new byte[1] { (byte)i };

                // byte -> byte
                destinationByte[0] = default;
                VerifySingleChar(Ascii.ToLower(sourceByte, destinationByte, out int consumed, out int written), i, expectedToLower, destinationByte[0], consumed, written);
                destinationByte[0] = default;
                VerifySingleChar(Ascii.ToUpper(sourceByte, destinationByte, out consumed, out written), i, expectedToUpper, destinationByte[0], consumed, written);
                // byte -> char
                destinationChar[0] = default;
                VerifySingleChar(Ascii.ToLower(sourceByte, destinationChar, out consumed, out written), i, (char)expectedToLower, destinationChar[0], consumed, written);
                destinationChar[0] = default;
                VerifySingleChar(Ascii.ToUpper(sourceByte, destinationChar, out consumed, out written), i, (char)expectedToUpper, destinationChar[0], consumed, written);
            }
        }

        [Fact]
        public void SingleCharConversion()
        {
            char[] sourceChar = new char[1], destinationChar = new char[1]; // this test is "optimized" as it performs a LOT of iterations
            byte[] destinationByte = new byte[1];

            for (int i = 0; i <= char.MaxValue; i++)
            {
                char expectedLower = char.IsBetween((char)i, 'A', 'Z') ? (char)(i - 'A' + 'a') : (char)i;
                char expectedUpper = char.IsBetween((char)i, 'a', 'z') ? (char)(i + 'A' + 'a') : (char)i;

                sourceChar[0] = (char)i;

                // char -> char
                destinationChar[0] = default;
                VerifySingleChar(Ascii.ToLower(sourceChar, destinationChar, out int consumed, out int written), i, expectedLower, destinationChar[0], consumed, written);
                destinationChar[0] = default;
                VerifySingleChar(Ascii.ToUpper(sourceChar, destinationChar, out consumed, out written), i, expectedUpper, destinationChar[0], consumed, written);
                // char -> byte
                destinationByte[0] = default;
                VerifySingleChar(Ascii.ToLower(sourceChar, destinationByte, out consumed, out written), i, (byte)expectedLower, destinationByte[0], consumed, written);
                destinationByte[0] = default;
                VerifySingleChar(Ascii.ToUpper(sourceChar, destinationByte, out consumed, out written), i, (byte)expectedUpper, destinationByte[0], consumed, written);
            }
        }

        [Theory]
        [InlineData("\u00C0bCDe")] // U+00C0 is not ASCII
        [InlineData("\u00E0bCDe")] // U+00E0 is not ASCII
        public void InvalidCharacters(string sourceChars)
        {
            char[] destinationChars = new char[sourceChars.Length];
            byte[] sourceBytes = System.Text.Encoding.ASCII.GetBytes(sourceChars);
            byte[] destinationBytes = new byte[sourceBytes.Length];

            // char => char
            Verify(Ascii.ToLower(sourceChars, destinationChars, out int consumed, out int written), consumed, written);
            Verify(Ascii.ToUpper(sourceChars, destinationChars, out consumed, out written), consumed, written);
            // char => byte
            Verify(Ascii.ToLower(sourceChars, destinationBytes, out consumed, out written), consumed, written);
            Verify(Ascii.ToUpper(sourceChars, destinationBytes, out consumed, out written), consumed, written);
            // byte => byte
            Verify(Ascii.ToLower(sourceBytes, destinationBytes, out consumed, out written), consumed, written);
            Verify(Ascii.ToUpper(sourceBytes, destinationBytes, out consumed, out written), consumed, written);
            // byte => char
            Verify(Ascii.ToLower(sourceBytes, destinationChars, out consumed, out written), consumed, written);
            Verify(Ascii.ToUpper(sourceBytes, destinationChars, out consumed, out written), consumed, written);

            static void Verify(OperationStatus status, int consumed, int written)
            {
                Assert.Equal(OperationStatus.InvalidData, status);
                Assert.Equal(0, consumed);
                Assert.Equal(0, written);
            }
        }

        [Theory]
        [InlineData("", "", "")]
        [InlineData("Hello", "hello", "HELLO")]
        [InlineData("\rHello\n", "\rhello\n", "\rHELLO\n")]
        [InlineData("\0xyz\0", "\0xyz\0", "\0XYZ\0")]
        [InlineData("\0XYZ\0", "\0xyz\0", "\0XYZ\0")]
        [InlineData("AbCdEFgHIJkLmNoPQRStUVwXyZ", "abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWXYZ")] // should hit vectorized code path
        public void MultipleValidCharacterConversion(string sourceChars, string expectedLowerChars, string expectedUpperChars)
        {
            Assert.Equal(sourceChars.Length, expectedLowerChars.Length);
            Assert.Equal(expectedLowerChars.Length, expectedUpperChars.Length);

            byte[] sourceBytes = System.Text.Encoding.ASCII.GetBytes(sourceChars);
            byte[] expectedLowerBytes = System.Text.Encoding.ASCII.GetBytes(expectedLowerChars);
            byte[] expectedUpperBytes = System.Text.Encoding.ASCII.GetBytes(expectedUpperChars);
            char[] destinationChars = new char[expectedLowerChars.Length];
            byte[] destinationBytes = new byte[expectedLowerChars.Length];

            // char -> char
            Verify<char>(Ascii.ToLower(sourceChars, destinationChars, out int consumed, out int written), expectedLowerChars, destinationChars, consumed, written);
            Verify<char>(Ascii.ToUpper(sourceChars, destinationChars, out consumed, out written), expectedUpperChars, destinationChars, consumed, written);
            // char -> byte
            Verify<byte>(Ascii.ToLower(sourceChars, destinationBytes, out consumed, out written), expectedLowerBytes, destinationBytes, consumed, written);
            Verify<byte>(Ascii.ToUpper(sourceChars, destinationBytes, out consumed, out written), expectedUpperBytes, destinationBytes, consumed, written);
            // byte -> byte
            Verify<byte>(Ascii.ToLower(sourceBytes, destinationBytes, out consumed, out written), expectedLowerBytes, destinationBytes, consumed, written);
            Verify<byte>(Ascii.ToUpper(sourceBytes, destinationBytes, out consumed, out written), expectedUpperBytes, destinationBytes, consumed, written);
            // byte -> char
            Verify<char>(Ascii.ToLower(sourceBytes, destinationChars, out consumed, out written), expectedLowerChars, destinationChars, consumed, written);
            Verify<char>(Ascii.ToUpper(sourceBytes, destinationChars, out consumed, out written), expectedUpperChars, destinationChars, consumed, written);

            static void Verify<T>(OperationStatus status, ReadOnlySpan<T> expected, ReadOnlySpan<T> actual, int consumed, int written)
            {
                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(expected.Length, consumed);
                Assert.Equal(expected.Length, written);
                Assert.Equal(expected.ToArray(), actual.ToArray());
            }
        }

        [Theory]
        [InlineData("Hello", 4, "hell", "HELL")]
        [InlineData(" AbC ", 3, " ab", " AB")]
        public void DestinationTooSmall(string sourceChars, int destinationSize, string expectedLowerChars, string expectedUpperChars)
        {
            Assert.NotEqual(sourceChars.Length, destinationSize);
            Assert.Equal(destinationSize, expectedLowerChars.Length);
            Assert.Equal(expectedLowerChars.Length, expectedUpperChars.Length);

            byte[] sourceBytes = System.Text.Encoding.ASCII.GetBytes(sourceChars);
            byte[] expectedLowerBytes = System.Text.Encoding.ASCII.GetBytes(expectedLowerChars);
            byte[] expectedUpperBytes = System.Text.Encoding.ASCII.GetBytes(expectedUpperChars);
            char[] destinationChars = new char[destinationSize];
            byte[] destinationBytes = new byte[destinationSize];

            // char -> char
            Verify<char>(Ascii.ToLower(sourceChars, destinationChars, out int consumed, out int written), expectedLowerChars, destinationChars, consumed, written);
            Verify<char>(Ascii.ToUpper(sourceChars, destinationChars, out consumed, out written), expectedUpperChars, destinationChars, consumed, written);
            // char -> byte
            Verify<byte>(Ascii.ToLower(sourceChars, destinationBytes, out consumed, out written), expectedLowerBytes, destinationBytes, consumed, written);
            Verify<byte>(Ascii.ToUpper(sourceChars, destinationBytes, out consumed, out written), expectedUpperBytes, destinationBytes, consumed, written);
            // byte -> byte
            Verify<byte>(Ascii.ToLower(sourceBytes, destinationBytes, out consumed, out written), expectedLowerBytes, destinationBytes, consumed, written);
            Verify<byte>(Ascii.ToUpper(sourceBytes, destinationBytes, out consumed, out written), expectedUpperBytes, destinationBytes, consumed, written);
            // byte -> char
            Verify<char>(Ascii.ToLower(sourceBytes, destinationChars, out consumed, out written), expectedLowerChars, destinationChars, consumed, written);
            Verify<char>(Ascii.ToUpper(sourceBytes, destinationChars, out consumed, out written), expectedUpperChars, destinationChars, consumed, written);

            static void Verify<T>(OperationStatus status, ReadOnlySpan<T> expected, ReadOnlySpan<T> actual, int consumed, int written)
            {
                Assert.Equal(OperationStatus.DestinationTooSmall, status);
                Assert.Equal(actual.Length, consumed);
                Assert.Equal(actual.Length, written);
                Assert.Equal(expected.ToArray(), actual.ToArray());
            }
        }
    }
}
