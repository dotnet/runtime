// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
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
            yield return new object[] { "fo", "\tZ\tm8=\n" };
            yield return new object[] { "foo", " Z m 9 v" };
        }

        public static IEnumerable<object[]> TestData_Oversize()
        {
            // test data with extra chunks of data outside the selected range
            yield return new object[] { "Zm9v////", 0, 4, "foo" };
            yield return new object[] { "////Zm9v", 4, 4, "foo" };
            yield return new object[] { "////Zm9v////", 4, 4, "foo" };
        }

        [Fact]
        public void InvalidInput_ToBase64Transform()
        {
            byte[] data_5bytes = Text.Encoding.ASCII.GetBytes("aaaaa");

            using (var transform = new ToBase64Transform())
            {
                InvalidInput_Base64Transform(transform);

                // These exceptions only thrown in ToBase
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offsetOut", () => transform.TransformFinalBlock(data_5bytes, 0, 5));
            }
        }

        [Fact]
        public void InvalidInput_FromBase64Transform()
        {
            byte[] data_4bytes = Text.Encoding.ASCII.GetBytes("aaaa");

            ICryptoTransform transform = new FromBase64Transform();
            InvalidInput_Base64Transform(transform);

            // These exceptions only thrown in FromBase
            transform.Dispose();
            Assert.Throws<ObjectDisposedException>(() => transform.TransformBlock(data_4bytes, 0, 4, null, 0));
            Assert.Throws<ObjectDisposedException>(() => transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0));
        }

        private void InvalidInput_Base64Transform(ICryptoTransform transform)
        {
            byte[] data_4bytes = Text.Encoding.ASCII.GetBytes("aaaa");

            AssertExtensions.Throws<ArgumentNullException>("inputBuffer", () => transform.TransformBlock(null, 0, 0, null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => transform.TransformBlock(Array.Empty<byte>(), -1, 0, null, 0));
            AssertExtensions.Throws<ArgumentNullException>("dst", () => transform.TransformBlock(data_4bytes, 0, 4, null, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => transform.TransformBlock(Array.Empty<byte>(), 0, 1, null, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => transform.TransformBlock(Array.Empty<byte>(), 1, 0, null, 0));

            AssertExtensions.Throws<ArgumentNullException>("inputBuffer", () => transform.TransformFinalBlock(null, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => transform.TransformFinalBlock(Array.Empty<byte>(), -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inputOffset", () => transform.TransformFinalBlock(Array.Empty<byte>(), -1, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => transform.TransformFinalBlock(Array.Empty<byte>(), 1, 0));
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

        private static void ValidateCryptoStream(string expected, string data, ICryptoTransform transform)
        {
            byte[] inputBytes = Text.Encoding.ASCII.GetBytes(data);
            byte[] outputBytes = new byte[100];

            // Verify read mode
            using (var ms = new MemoryStream(inputBytes))
            using (var cs = new CryptoStream(ms, transform, CryptoStreamMode.Read))
            {
                int bytesRead = cs.Read(outputBytes, 0, outputBytes.Length);
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

                // Test passing blocks > 4 characters to TransformFinalBlock (not supported)
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offsetOut", () => transform.TransformFinalBlock(inputBytes, 0, inputBytes.Length));
            }
        }

        [Theory, MemberData(nameof(TestData_LongBlock_Ascii))]
        public static void ValidateFromBase64TransformFinalBlock(string expected, string encoding)
        {
            using (var transform = new FromBase64Transform())
            {
                byte[] inputBytes = Text.Encoding.ASCII.GetBytes(encoding);
                Assert.True(inputBytes.Length > 4);

                // Test passing blocks > 4 characters to TransformFinalBlock (supported)
                byte[] outputBytes = transform.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                string outputString = Text.Encoding.ASCII.GetString(outputBytes, 0, outputBytes.Length);
                Assert.Equal(expected, outputString);
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
                    int bytesRead = cs.Read(outputBytes, 0, outputBytes.Length);

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
                int bytesRead = cs.Read(outputBytes, 0, outputBytes.Length);
                string outputString = Text.Encoding.ASCII.GetString(outputBytes, 0, bytesRead);
                Assert.Equal(expected, outputString);
            }

            // Verify explicit FromBase64TransformMode.IgnoreWhiteSpaces
            using (var base64Transform = new FromBase64Transform(FromBase64TransformMode.IgnoreWhiteSpaces))
            using (var ms = new MemoryStream(inputBytes))
            using (var cs = new CryptoStream(ms, base64Transform, CryptoStreamMode.Read))
            {
                int bytesRead = cs.Read(outputBytes, 0, outputBytes.Length);
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
    }
}
