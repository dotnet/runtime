// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class Base64TransformsTests
    {
        public static IEnumerable<object[]> TestData_Ascii()
        {
            // Test data taken from RFC 4648 Test Vectors
            yield return new object[] { "", "" };
            yield return new object[] { "f", "Zg==" };
            yield return new object[] { "fo", "Zm8=" };
            yield return new object[] { "foo", "Zm9v" };
            yield return new object[] { "foob", "Zm9vYg==" };
            yield return new object[] { "fooba", "Zm9vYmE=" };
            yield return new object[] { "foobar", "Zm9vYmFy" };
        }

        public static IEnumerable<object[]> TestData_LongBlock_Ascii()
        {
            yield return new object[] { "fooba", "Zm9vYmE=" };
            yield return new object[] { "foobar", "Zm9vYmFy" };
        }

        public static IEnumerable<object[]> TestData_Ascii_NoPadding()
        {
            // Test data without padding
            yield return new object[] { "Zg" };
            yield return new object[] { "Zm9vYg" };
            yield return new object[] { "Zm9vYmE" };
        }

        public static IEnumerable<object[]> TestData_Ascii_Whitespace()
        {
            yield return new object[] { "fo", "\tZ\tm8=\r" };
            yield return new object[] { "fo", "\tZ\tm8=\n" };
            yield return new object[] { "fo", "\tZ\tm8=\r\n" };
            yield return new object[] { "foo", " Z m 9 v" };
        }

        public static IEnumerable<object[]> TestData_Oversize()
        {
            // test data with extra chunks of data outside the selected range
            yield return new object[] { "Zm9v////", 0, 4, "foo" };
            yield return new object[] { "////Zm9v", 4, 4, "foo" };
            yield return new object[] { "////Zm9v////", 4, 4, "foo" };
            yield return new object[] { "Zm9vYmFyYm", 0, 10, "foobar" };
        }

        [Fact]
        public void InvalidInput_ToBase64Transform()
        {
            byte[] data_3bytes = "aaa"u8.ToArray();
            ICryptoTransform transform = new ToBase64Transform();

            AssertExtensions.Throws<ArgumentNullException>("inputBuffer", () => transform.TransformBlock(null, 0, 0, null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => transform.TransformBlock(Array.Empty<byte>(), -1, 0, null, 0));
            AssertExtensions.Throws<ArgumentNullException>("outputBuffer", () => transform.TransformBlock(data_3bytes, 0, 3, null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputCount", () => transform.TransformBlock(Array.Empty<byte>(), 0, 1, null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputCount", () => transform.TransformBlock(data_3bytes, 0, 1, new byte[10], 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputCount", () => transform.TransformBlock(new byte[4], 0, 4, new byte[10], 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("outputBuffer", () => transform.TransformBlock(data_3bytes, 0, 3, new byte[1], 0));
            AssertExtensions.Throws<ArgumentException>(null, () => transform.TransformBlock(Array.Empty<byte>(), 1, 0, null, 0));

            AssertExtensions.Throws<ArgumentNullException>("inputBuffer", () => transform.TransformFinalBlock(null, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => transform.TransformFinalBlock(Array.Empty<byte>(), -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => transform.TransformFinalBlock(Array.Empty<byte>(), -1, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => transform.TransformFinalBlock(Array.Empty<byte>(), 1, 0));
        }

        [Fact]
        public void InvalidInput_FromBase64Transform()
        {
            byte[] data_4bytes = "aaaa"u8.ToArray();
            ICryptoTransform transform = new FromBase64Transform();

            AssertExtensions.Throws<ArgumentNullException>("inputBuffer", () => transform.TransformBlock(null, 0, 0, null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => transform.TransformBlock(Array.Empty<byte>(), -1, 0, null, 0));
            AssertExtensions.Throws<ArgumentNullException>("outputBuffer", () => transform.TransformBlock(data_4bytes, 0, 4, null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputCount", () => transform.TransformBlock(Array.Empty<byte>(), 0, 1, null, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => transform.TransformBlock(Array.Empty<byte>(), 1, 0, null, 0));

            AssertExtensions.Throws<ArgumentNullException>("inputBuffer", () => transform.TransformFinalBlock(null, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => transform.TransformFinalBlock(Array.Empty<byte>(), -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => transform.TransformFinalBlock(Array.Empty<byte>(), -1, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => transform.TransformFinalBlock(Array.Empty<byte>(), 1, 0));

            // These exceptions only thrown in FromBase
            transform.Dispose();
            Assert.Throws<ObjectDisposedException>(() => transform.TransformBlock(data_4bytes, 0, 4, null, 0));
            Assert.Throws<ObjectDisposedException>(() => transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0));
        }

        [Fact]
        public void ToBase64_TransformFinalBlock_MatchesConvert()
        {
            for (int i = 0; i < 100; i++)
            {
                byte[] input = new byte[i];
                Random.Shared.NextBytes(input);

                string expected = Convert.ToBase64String(input);

                using var transform = new ToBase64Transform();
                string actual = string.Concat(transform.TransformFinalBlock(input, 0, input.Length).Select(b => char.ToString((char)b)));

                Assert.Equal(expected, actual);
            }
        }

        [Theory, MemberData(nameof(TestData_Ascii))]
        public static void ValidateToBase64CryptoStream(string data, string encoding)
        {
            using (var transform = new ToBase64Transform())
            {
                ValidateCryptoStream(encoding, data, transform);
            }
        }

        [Theory, MemberData(nameof(TestData_Ascii))]
        public static void ValidateFromBase64CryptoStream(string data, string encoding)
        {
            using (var transform = new FromBase64Transform())
            {
                ValidateCryptoStream(data, encoding, transform);
            }
        }

        [Theory]
        [InlineData(100)]
        [InlineData(101)]
        [InlineData(102)]
        [InlineData(103)]
        public static void RoundtripCryptoStream(int length)
        {
            byte[] expected = RandomNumberGenerator.GetBytes(length);
            var ms = new MemoryStream();

            using (var toBase64 = new ToBase64Transform())
            using (var stream = new CryptoStream(ms, toBase64, CryptoStreamMode.Write, leaveOpen: true))
            {
                stream.Write(expected);
            }

            ms.Position = 0;

            byte[] actual = new byte[expected.Length];
            using (var fromBase64 = new FromBase64Transform())
            using (var stream = new CryptoStream(ms, fromBase64, CryptoStreamMode.Read, leaveOpen: true))
            {
                int totalRead = 0, bytesRead;
                while ((bytesRead = stream.Read(actual.AsSpan(totalRead))) != 0)
                {
                    totalRead += bytesRead;
                }
                Assert.Equal(actual.Length, totalRead);
                AssertExtensions.SequenceEqual(expected, actual);
            }
        }

        private static void ValidateCryptoStream(string expected, string data, ICryptoTransform transform)
        {
            byte[] inputBytes = Text.Encoding.ASCII.GetBytes(data);
            byte[] outputBytes = new byte[100];

            // Verify read mode
            using (var ms = new MemoryStream(inputBytes))
            using (var cs = new CryptoStream(ms, transform, CryptoStreamMode.Read))
            {
                int bytesRead = ReadAll(cs, outputBytes);
                string outputString = Text.Encoding.ASCII.GetString(outputBytes, 0, bytesRead);
                Assert.Equal(expected, outputString);
            }

            // Verify write mode
            using (var ms = new MemoryStream(outputBytes))
            using (var cs = new CryptoStream(ms, transform, CryptoStreamMode.Write))
            {
                cs.Write(inputBytes, 0, inputBytes.Length);
                cs.FlushFinalBlock();
                string outputString = Text.Encoding.ASCII.GetString(outputBytes, 0, (int)ms.Position);
                Assert.Equal(expected, outputString);
            }
        }

        [Theory, MemberData(nameof(TestData_LongBlock_Ascii))]
        public static void ValidateToBase64TransformFinalBlock(string data, string expected)
        {
            using (var transform = new ToBase64Transform())
            {
                byte[] inputBytes = Text.Encoding.ASCII.GetBytes(data);
                Assert.True(inputBytes.Length > 4);

                // Test passing blocks > 4 characters to TransformFinalBlock (supported)
                byte[] outputBytes = transform.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                string outputString = Text.Encoding.ASCII.GetString(outputBytes, 0, outputBytes.Length);
                Assert.Equal(expected, outputString);
            }
        }

        [Theory, MemberData(nameof(TestData_LongBlock_Ascii))]
        public static void ValidateFromBase64TransformFinalBlock(string expected, string encoded)
        {
            using (var transform = new FromBase64Transform())
            {
                byte[] inputBytes = Text.Encoding.ASCII.GetBytes(encoded);
                Assert.True(inputBytes.Length > 4);

                // Test passing blocks > 4 characters to TransformFinalBlock (supported)
                byte[] outputBytes = transform.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                string outputString = Text.Encoding.ASCII.GetString(outputBytes, 0, outputBytes.Length);
                Assert.Equal(expected, outputString);
            }
        }

        [Theory, MemberData(nameof(TestData_LongBlock_Ascii))]
        public static void ValidateFromBase64TransformBlock(string expected, string encoded)
        {
            using (var transform = new FromBase64Transform())
            {
                byte[] inputBytes = Text.Encoding.ASCII.GetBytes(encoded);
                Assert.True(inputBytes.Length > 4);

                byte[] outputBytes = new byte[100];
                int bytesWritten = transform.TransformBlock(inputBytes, 0, inputBytes.Length, outputBytes, 0);
                string outputText = Text.Encoding.ASCII.GetString(outputBytes, 0, bytesWritten);

                Assert.Equal(expected, outputText);
            }
        }

        [Theory, MemberData(nameof(TestData_Ascii_NoPadding))]
        public static void ValidateFromBase64_NoPadding(string data)
        {
            using (var transform = new FromBase64Transform())
            {
                byte[] inputBytes = Text.Encoding.ASCII.GetBytes(data);
                byte[] outputBytes = new byte[100];

                using (var ms = new MemoryStream(inputBytes))
                using (var cs = new CryptoStream(ms, transform, CryptoStreamMode.Read))
                {
                    int bytesRead = ReadAll(cs, outputBytes);

                    // Missing padding bytes not supported (no exception, however)
                    Assert.NotEqual(inputBytes.Length, bytesRead);
                }
            }
        }

        [Theory, MemberData(nameof(TestData_Oversize))]
        public static void ValidateFromBase64_OversizeBuffer(string input, int offset, int count, string expected)
        {
            using (var transform = new FromBase64Transform())
            {
                byte[] inputBytes = Text.Encoding.ASCII.GetBytes(input);
                byte[] outputBytes = new byte[100];

                int bytesWritten = transform.TransformBlock(inputBytes, offset, count, outputBytes, 0);

                string outputText = Text.Encoding.ASCII.GetString(outputBytes, 0, bytesWritten);

                Assert.Equal(expected, outputText);
            }
        }

        [Theory, MemberData(nameof(TestData_Ascii_Whitespace))]
        public static void ValidateWhitespace(string expected, string data)
        {
            byte[] inputBytes = Text.Encoding.ASCII.GetBytes(data);
            byte[] outputBytes = new byte[100];

            // Verify default of FromBase64TransformMode.IgnoreWhiteSpaces
            using (var base64Transform = new FromBase64Transform())
            using (var ms = new MemoryStream(inputBytes))
            using (var cs = new CryptoStream(ms, base64Transform, CryptoStreamMode.Read))
            {
                int bytesRead = ReadAll(cs, outputBytes);
                string outputString = Text.Encoding.ASCII.GetString(outputBytes, 0, bytesRead);
                Assert.Equal(expected, outputString);
            }

            // Verify explicit FromBase64TransformMode.IgnoreWhiteSpaces
            using (var base64Transform = new FromBase64Transform(FromBase64TransformMode.IgnoreWhiteSpaces))
            using (var ms = new MemoryStream(inputBytes))
            using (var cs = new CryptoStream(ms, base64Transform, CryptoStreamMode.Read))
            {
                int bytesRead = ReadAll(cs, outputBytes);
                string outputString = Text.Encoding.ASCII.GetString(outputBytes, 0, bytesRead);
                Assert.Equal(expected, outputString);
            }

            // Verify FromBase64TransformMode.DoNotIgnoreWhiteSpaces
            using (var base64Transform = new FromBase64Transform(FromBase64TransformMode.DoNotIgnoreWhiteSpaces))
            using (var ms = new MemoryStream(inputBytes))
            using (var cs = new CryptoStream(ms, base64Transform, CryptoStreamMode.Read))
            {
                Assert.Throws<FormatException>(() => cs.Read(outputBytes, 0, outputBytes.Length));
            }
        }

        [Fact]
        public void Blocksizes_ToBase64Transform()
        {
            using (var transform = new ToBase64Transform())
            {
                Assert.Equal(3, transform.InputBlockSize);
                Assert.Equal(4, transform.OutputBlockSize);
            }
        }

        [Fact]
        public void Blocksizes_FromBase64Transform()
        {
            using (var transform = new FromBase64Transform())
            {
                Assert.Equal(4, transform.InputBlockSize);
                Assert.Equal(3, transform.OutputBlockSize);
            }
        }

        [Fact]
        public void TransformUsageFlags_ToBase64Transform()
        {
            using (var transform = new ToBase64Transform())
            {
                Assert.True(transform.CanTransformMultipleBlocks);
                Assert.True(transform.CanReuseTransform);
            }
        }

        [Fact]
        public void TransformUsageFlags_FromBase64Transform()
        {
            using (var transform = new FromBase64Transform())
            {
                Assert.True(transform.CanTransformMultipleBlocks);
                Assert.True(transform.CanReuseTransform);
            }
        }

        private static int ReadAll(Stream stream, Span<byte> buffer)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int bytesRead = stream.Read(buffer.Slice(totalRead));
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
            }

            return totalRead;
        }
    }
}
