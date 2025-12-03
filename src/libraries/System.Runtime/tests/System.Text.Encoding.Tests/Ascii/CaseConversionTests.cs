// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Text.Tests
{
    public static class CaseConversionTests
    {
        private const byte MaxValidAsciiChar = 127;

        [Fact]
        public static void OverlappingBuffers_Throws()
        {
            byte[] byteBuffer = new byte[10];
            char[] charBuffer = new char[10];

            // byte -> byte
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(byteBuffer, byteBuffer, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(byteBuffer.AsSpan(1, 3), byteBuffer.AsSpan(3, 5), out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(byteBuffer, byteBuffer, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(byteBuffer.AsSpan(1, 3), byteBuffer.AsSpan(3, 5), out _));
            // byte -> char
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(byteBuffer, MemoryMarshal.Cast<byte, char>(byteBuffer), out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(byteBuffer, MemoryMarshal.Cast<byte, char>(byteBuffer).Slice(1, 3), out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(byteBuffer, MemoryMarshal.Cast<byte, char>(byteBuffer), out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(byteBuffer, MemoryMarshal.Cast<byte, char>(byteBuffer).Slice(1, 3), out _));
            // char -> char
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(charBuffer, charBuffer, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(charBuffer.AsSpan(1, 3), charBuffer.AsSpan(3, 5), out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(charBuffer, charBuffer, out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(charBuffer.AsSpan(1, 3), charBuffer.AsSpan(3, 5), out _));
            // char -> byte
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(charBuffer, MemoryMarshal.Cast<char, byte>(charBuffer), out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToLower(charBuffer, MemoryMarshal.Cast<char, byte>(charBuffer).Slice(1, 3), out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(charBuffer, MemoryMarshal.Cast<char, byte>(charBuffer), out _));
            Assert.Throws<InvalidOperationException>(() => Ascii.ToUpper(charBuffer, MemoryMarshal.Cast<char, byte>(charBuffer).Slice(1, 3), out _));
        }

        private static void VerifySingleChar<T>(OperationStatus status, int value, T expected, T actual, int written)
        {
            Assert.True(typeof(T) == typeof(char) || typeof(T) == typeof(byte));

            if (value <= MaxValidAsciiChar)
            {
                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(expected, actual);
                Assert.Equal(1, written);
            }
            else
            {
                Assert.Equal(OperationStatus.InvalidData, status);
                Assert.Equal(default, actual);
                Assert.Equal(0, written);
            }
        }

        [Fact]
        public static void SingleByteConversion()
        {
            byte[] destinationByte = new byte[1];
            char[] destinationChar = new char[1];

            for (int i = 0; i <= byte.MaxValue; i++)
            {
                byte expectedToLower = char.IsBetween((char)i, 'A', 'Z') ? (byte)(i - 'A' + 'a') : (byte)i;
                byte expectedToUpper = char.IsBetween((char)i, 'a', 'z') ? (byte)(i + 'A' - 'a') : (byte)i;

                byte[] sourceByte = new byte[1] { (byte)i };

                // byte -> byte
                destinationByte[0] = default;
                VerifySingleChar(Ascii.ToLower(sourceByte, destinationByte, out int written), i, expectedToLower, destinationByte[0], written);
                destinationByte[0] = default;
                VerifySingleChar(Ascii.ToUpper(sourceByte, destinationByte, out written), i, expectedToUpper, destinationByte[0], written);
                // byte -> char
                destinationChar[0] = default;
                VerifySingleChar(Ascii.ToLower(sourceByte, destinationChar, out written), i, (char)expectedToLower, destinationChar[0], written);
                destinationChar[0] = default;
                VerifySingleChar(Ascii.ToUpper(sourceByte, destinationChar, out written), i, (char)expectedToUpper, destinationChar[0], written);
            }
        }

        [Fact]
        public static void SingleCharConversion()
        {
            char[] sourceChar = new char[1], destinationChar = new char[1]; // this test is "optimized" as it performs a LOT of iterations
            byte[] destinationByte = new byte[1];

            for (int i = 0; i <= char.MaxValue; i++)
            {
                char expectedLower = char.IsBetween((char)i, 'A', 'Z') ? (char)(i - 'A' + 'a') : (char)i;
                char expectedUpper = char.IsBetween((char)i, 'a', 'z') ? (char)(i + 'A' - 'a') : (char)i;

                sourceChar[0] = (char)i;

                // char -> char
                destinationChar[0] = default;
                VerifySingleChar(Ascii.ToLower(sourceChar, destinationChar, out int written), i, expectedLower, destinationChar[0], written);
                destinationChar[0] = default;
                VerifySingleChar(Ascii.ToUpper(sourceChar, destinationChar, out written), i, expectedUpper, destinationChar[0], written);
                // char -> byte
                destinationByte[0] = default;
                VerifySingleChar(Ascii.ToLower(sourceChar, destinationByte, out written), i, (byte)expectedLower, destinationByte[0], written);
                destinationByte[0] = default;
                VerifySingleChar(Ascii.ToUpper(sourceChar, destinationByte, out written), i, (byte)expectedUpper, destinationByte[0], written);
            }
        }

        [Theory]
        [InlineData("\u00C0bCDe")] // U+00C0 is not ASCII
        [InlineData("\u00E0bCDe")] // U+00E0 is not ASCII
        public static void InvalidCharacters(string sourceChars)
        {
            char[] destinationChars = new char[sourceChars.Length];
            byte[] sourceBytes = System.Text.Encoding.ASCII.GetBytes(sourceChars);
            byte[] destinationBytes = new byte[sourceBytes.Length];

            if (sourceBytes[0] <= MaxValidAsciiChar)
            {
                sourceBytes[0] = MaxValidAsciiChar + 1; // ensure the first byte is invalid (U+00C0 is mapped to valid ascii char by ASCII.GetBytes)
            }

            // char => char
            VerifyStatus(Ascii.ToLower(sourceChars, destinationChars, out int written), written);
            VerifyStatus(Ascii.ToUpper(sourceChars, destinationChars, out written), written);
            // char => byte
            VerifyStatus(Ascii.ToLower(sourceChars, destinationBytes, out written), written);
            VerifyStatus(Ascii.ToUpper(sourceChars, destinationBytes, out written), written);
            // byte => byte
            VerifyStatus(Ascii.ToLower(sourceBytes, destinationBytes, out written), written);
            VerifyStatus(Ascii.ToUpper(sourceBytes, destinationBytes, out written), written);
            // byte => char
            VerifyStatus(Ascii.ToLower(sourceBytes, destinationChars, out written), written);
            VerifyStatus(Ascii.ToUpper(sourceBytes, destinationChars, out written), written);

            // InPlace(byte)
            VerifyStatus(Ascii.ToLowerInPlace(sourceBytes, out int processed), processed);
            VerifyStatus(Ascii.ToUpperInPlace(sourceBytes, out processed), processed);
            // InPlace(char)
            VerifyStatus(Ascii.ToLowerInPlace(sourceChars.ToCharArray(), out processed), processed);
            VerifyStatus(Ascii.ToUpperInPlace(sourceChars.ToCharArray(), out processed), processed);

            static void VerifyStatus(OperationStatus status, int written)
            {
                Assert.Equal(OperationStatus.InvalidData, status);
                Assert.Equal(0, written);
            }
        }

        public static IEnumerable<object[]> MultipleValidCharacterConversion_Arguments
        {
            get
            {
                yield return new object[] { "", "", "" };
                yield return new object[] { "Hello", "hello", "HELLO" };
                yield return new object[] { "\rHello\n", "\rhello\n", "\rHELLO\n" };
                yield return new object[] { "\0xyz\0", "\0xyz\0", "\0XYZ\0" };
                yield return new object[] { "\0XYZ\0", "\0xyz\0", "\0XYZ\0" };
                yield return new object[] { "AbCdEFgHIJkLmNoPQRStUVwXyZ", "abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWXYZ" };

                // exercise all possible code paths
                for (int i = 1; i <= MaxValidAsciiChar; i++)
                {
                    char expectedLower = char.IsBetween((char)i, 'A', 'Z') ? (char)(i - 'A' + 'a') : (char)i;
                    char expectedUpper = char.IsBetween((char)i, 'a', 'z') ? (char)(i + 'A' - 'a') : (char)i;

                    yield return new object[] { new string((char)i, i), new string(expectedLower, i), new string(expectedUpper, i) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(MultipleValidCharacterConversion_Arguments))]
        public static void MultipleValidCharacterConversion(string sourceChars, string expectedLowerChars, string expectedUpperChars)
        {
            Assert.Equal(sourceChars.Length, expectedLowerChars.Length);
            Assert.Equal(expectedLowerChars.Length, expectedUpperChars.Length);

            byte[] sourceBytes = Encoding.ASCII.GetBytes(sourceChars);
            byte[] expectedLowerBytes = Encoding.ASCII.GetBytes(expectedLowerChars);
            byte[] expectedUpperBytes = Encoding.ASCII.GetBytes(expectedUpperChars);
            char[] destinationChars = new char[expectedLowerChars.Length];
            byte[] destinationBytes = new byte[expectedLowerChars.Length];

            // char -> char
            VerifyStatus<char>(Ascii.ToLower(sourceChars, destinationChars, out int written), expectedLowerChars, destinationChars, written);
            VerifyStatus<char>(Ascii.ToUpper(sourceChars, destinationChars, out written), expectedUpperChars, destinationChars, written);
            // char -> byte
            VerifyStatus<byte>(Ascii.ToLower(sourceChars, destinationBytes, out written), expectedLowerBytes, destinationBytes, written);
            VerifyStatus<byte>(Ascii.ToUpper(sourceChars, destinationBytes, out written), expectedUpperBytes, destinationBytes, written);
            // byte -> byte
            VerifyStatus<byte>(Ascii.ToLower(sourceBytes, destinationBytes, out written), expectedLowerBytes, destinationBytes, written);
            VerifyStatus<byte>(Ascii.ToUpper(sourceBytes, destinationBytes, out written), expectedUpperBytes, destinationBytes, written);
            // byte -> char
            VerifyStatus<char>(Ascii.ToLower(sourceBytes, destinationChars, out written), expectedLowerChars, destinationChars, written);
            VerifyStatus<char>(Ascii.ToUpper(sourceBytes, destinationChars, out written), expectedUpperChars, destinationChars, written);

            // InPlace(byte)
            byte[] sourceBytesCopy = sourceBytes.ToArray();
            VerifyStatus<byte>(Ascii.ToLowerInPlace(sourceBytesCopy, out int processed), expectedLowerBytes, sourceBytesCopy, processed);
            sourceBytesCopy = sourceBytes.ToArray();
            VerifyStatus<byte>(Ascii.ToUpperInPlace(sourceBytesCopy, out processed), expectedUpperBytes, sourceBytesCopy, processed);
            // InPlace(char)
            char[] sourceCharsCopy = sourceChars.ToCharArray();
            VerifyStatus<char>(Ascii.ToLowerInPlace(sourceCharsCopy, out processed), expectedLowerChars.ToCharArray(), sourceCharsCopy, processed);
            sourceCharsCopy = sourceChars.ToCharArray();
            VerifyStatus<char>(Ascii.ToUpperInPlace(sourceCharsCopy, out processed), expectedUpperChars.ToCharArray(), sourceCharsCopy, processed);

            static void VerifyStatus<T>(OperationStatus status, ReadOnlySpan<T> expected, ReadOnlySpan<T> actual, int written)
            {
                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(expected.Length, written);
                Assert.Equal(expected.ToArray(), actual.ToArray());
            }
        }

        [Theory]
        [InlineData("Hello", 4, "hell", "HELL")]
        [InlineData(" AbC ", 3, " ab", " AB")]
        public static void DestinationTooSmall(string sourceChars, int destinationSize, string expectedLowerChars, string expectedUpperChars)
        {
            Assert.NotEqual(sourceChars.Length, destinationSize);
            Assert.Equal(destinationSize, expectedLowerChars.Length);
            Assert.Equal(expectedLowerChars.Length, expectedUpperChars.Length);

            byte[] sourceBytes = Encoding.ASCII.GetBytes(sourceChars);
            byte[] expectedLowerBytes = Encoding.ASCII.GetBytes(expectedLowerChars);
            byte[] expectedUpperBytes = Encoding.ASCII.GetBytes(expectedUpperChars);
            char[] destinationChars = new char[destinationSize];
            byte[] destinationBytes = new byte[destinationSize];

            // char -> char
            Verify<char>(Ascii.ToLower(sourceChars, destinationChars, out int written), expectedLowerChars, destinationChars, written);
            Verify<char>(Ascii.ToUpper(sourceChars, destinationChars, out written), expectedUpperChars, destinationChars, written);
            // char -> byte
            Verify<byte>(Ascii.ToLower(sourceChars, destinationBytes, out written), expectedLowerBytes, destinationBytes, written);
            Verify<byte>(Ascii.ToUpper(sourceChars, destinationBytes, out written), expectedUpperBytes, destinationBytes, written);
            // byte -> byte
            Verify<byte>(Ascii.ToLower(sourceBytes, destinationBytes, out written), expectedLowerBytes, destinationBytes, written);
            Verify<byte>(Ascii.ToUpper(sourceBytes, destinationBytes, out written), expectedUpperBytes, destinationBytes, written);
            // byte -> char
            Verify<char>(Ascii.ToLower(sourceBytes, destinationChars, out written), expectedLowerChars, destinationChars, written);
            Verify<char>(Ascii.ToUpper(sourceBytes, destinationChars, out written), expectedUpperChars, destinationChars, written);

            static void Verify<T>(OperationStatus status, ReadOnlySpan<T> expected, ReadOnlySpan<T> actual, int written)
            {
                Assert.Equal(OperationStatus.DestinationTooSmall, status);
                Assert.Equal(actual.Length, written);
                Assert.Equal(expected.ToArray(), actual.ToArray());
            }
        }
    }
}
