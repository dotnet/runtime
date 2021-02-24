// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;
using Test.Cryptography;

namespace System.Security.Cryptography.DeriveBytesTests
{
    [SkipOnMono("Not supported on Browser", TestPlatforms.Browser)]
    public static class Rfc2898OneShotTests
    {
        private const string Password = "tired";

        private static readonly byte[] s_passwordBytes = Encoding.UTF8.GetBytes(Password);
        private static readonly byte[] s_salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        private static readonly int s_extractLength = 14;

        [Fact]
        public static void Pbkdf2_PasswordBytes_NullPassword()
        {
            AssertExtensions.Throws<ArgumentNullException>("password", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    password: (byte[])null, s_salt, iterations: 1, HashAlgorithmName.SHA256, s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordBytes_NullSalt()
        {
            AssertExtensions.Throws<ArgumentNullException>("salt", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    s_passwordBytes, salt: (byte[])null, iterations: 1, HashAlgorithmName.SHA256, s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordBytes_SaltBytes_SaltEmpty()
        {
            byte[] expectedKey = "1E437A1C79D75BE61E91141DAE20".HexToByteArray();
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                new byte[0], salt: new byte[0], iterations: 1, HashAlgorithmName.SHA1, s_extractLength);
            Assert.Equal(expectedKey, key);
        }

        [Fact]
        public static void Pbkdf2_PasswordBytes_SaltBytes_IterationsNegative()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("iterations", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    s_passwordBytes, s_salt, iterations: -1, HashAlgorithmName.SHA256, s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordBytes_SaltBytes_OutputLengthNegative()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("outputLength", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    s_passwordBytes, s_salt, iterations: 1, HashAlgorithmName.SHA256, -1)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordBytes_BogusHash()
        {
            Assert.Throws<CryptographicException>(() =>
                Rfc2898DeriveBytes.Pbkdf2(
                    s_passwordBytes, s_salt, iterations: 1, new HashAlgorithmName("BLAH"), s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordBytes_NullHashName()
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    s_passwordBytes, s_salt, iterations: 1, default(HashAlgorithmName), s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordBytes_EmptyHashName()
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    s_passwordBytes, s_salt, iterations: 1, new HashAlgorithmName(""), s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordString_NullPassword()
        {
            AssertExtensions.Throws<ArgumentNullException>("password", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    password: (string)null, s_salt, iterations: 1, HashAlgorithmName.SHA256, s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordString_NullSalt()
        {
            AssertExtensions.Throws<ArgumentNullException>("salt", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    Password, salt: null, iterations: 1, HashAlgorithmName.SHA256, s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordString_SaltBytes_SaltEmpty()
        {
            byte[] expectedKey = "1E437A1C79D75BE61E91141DAE20".HexToByteArray();
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                password: "", salt: new byte[0], iterations: 1, HashAlgorithmName.SHA1, s_extractLength);
            Assert.Equal(expectedKey, key);
        }

        [Fact]
        public static void Pbkdf2_PasswordString_SaltBytes_IterationsNegative()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("iterations", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    Password, s_salt, iterations: -1, HashAlgorithmName.SHA256, s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordString_SaltBytes_OutputLengthNegative()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("outputLength", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    Password, s_salt, iterations: 1, HashAlgorithmName.SHA256, -1)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordString_BogusHash()
        {
            Assert.Throws<CryptographicException>(() =>
                Rfc2898DeriveBytes.Pbkdf2(
                    Password, s_salt, iterations: 1, new HashAlgorithmName("BLAH"), s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordString_NullHashName()
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    Password, s_salt, iterations: 1, default(HashAlgorithmName), s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordString_EmptyHashName()
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () =>
                Rfc2898DeriveBytes.Pbkdf2(
                    Password, s_salt, iterations: 1, new HashAlgorithmName(""), s_extractLength)
            );
        }

        [Fact]
        public static void Pbkdf2_PasswordString_InvalidUtf8()
        {
            Assert.Throws<EncoderFallbackException>(() =>
                Rfc2898DeriveBytes.Pbkdf2(
                    "\uD800", s_salt, iterations: 1, HashAlgorithmName.SHA256, s_extractLength));
        }

        [Fact]
        public static void Pbkdf2_Password_Salt_Overlapping_Completely()
        {
            Span<byte> buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            byte[] expected = { 0xBE, 0xA4, 0xEE, 0x0E, 0xC3, 0x98, 0xBF, 0x32 };
            Rfc2898DeriveBytes.Pbkdf2(buffer, buffer, buffer, iterations: 1, HashAlgorithmName.SHA256);
            Assert.Equal(expected, buffer.ToArray());
        }

        [Fact]
        public static void Pbkdf2_Password_Salt_Overlapping_Forward()
        {
            Span<byte> buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0xFF };
            Span<byte> output = buffer[1..];
            Span<byte> inputs = buffer[..^1];
            byte[] expected = { 0xBE, 0xA4, 0xEE, 0x0E, 0xC3, 0x98, 0xBF, 0x32 };
            Rfc2898DeriveBytes.Pbkdf2(inputs, inputs, output, iterations: 1, HashAlgorithmName.SHA256);
            Assert.Equal(expected, output.ToArray());
        }

        [Fact]
        public static void Pbkdf2_Password_Salt_Overlapping_Backward()
        {
            Span<byte> buffer = new byte[] { 0xFF, 1, 2, 3, 4, 5, 6, 7, 8 };
            Span<byte> output = buffer[..^1];
            Span<byte> inputs = buffer[1..];
            byte[] expected = { 0xBE, 0xA4, 0xEE, 0x0E, 0xC3, 0x98, 0xBF, 0x32 };
            Rfc2898DeriveBytes.Pbkdf2(inputs, inputs, output, iterations: 1, HashAlgorithmName.SHA256);
            Assert.Equal(expected, output.ToArray());
        }

        [Theory]
        [MemberData(nameof(Pbkdf2_PasswordBytes_Compare_Data))]
        public static void Pbkdf2_PasswordBytes_Compare(
            string hashAlgorithm,
            int length,
            int iterations,
            string passwordHex,
            string saltHex)
        {
            byte[] password = Convert.FromHexString(passwordHex);
            byte[] salt = Convert.FromHexString(saltHex);
            HashAlgorithmName hashAlgorithmName = new HashAlgorithmName(hashAlgorithm);
            byte[] key1;

            using (Rfc2898DeriveBytes instanceKdf = new Rfc2898DeriveBytes(password, salt, iterations, hashAlgorithmName))
            {
                key1 = instanceKdf.GetBytes(length);
            }

            // byte array allocating
            byte[] key2 = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithmName, length);
            Assert.Equal(key1, key2);

            Span<byte> destinationBuffer = new byte[length + 2];
            Span<byte> destination = destinationBuffer.Slice(1, length);
            Rfc2898DeriveBytes.Pbkdf2(password, salt, destination, iterations, hashAlgorithmName);

            Assert.True(key1.AsSpan().SequenceEqual(destination), "key1 == destination");
            Assert.Equal(0, destinationBuffer[^1]); // Make sure we didn't write past the destination
            Assert.Equal(0, destinationBuffer[0]); // Make sure we didn't write before the destination
        }

        [Theory]
        [MemberData(nameof(Pbkdf2_PasswordString_Compare_Data))]
        public static void Pbkdf2_PasswordString_Compare(
            string hashAlgorithm,
            int length,
            int iterations,
            string password,
            string saltHex)
        {
            byte[] salt = Convert.FromHexString(saltHex);
            HashAlgorithmName hashAlgorithmName = new HashAlgorithmName(hashAlgorithm);
            byte[] key1;

            using (Rfc2898DeriveBytes instanceKdf = new Rfc2898DeriveBytes(password, salt, iterations, hashAlgorithmName))
            {
                key1 = instanceKdf.GetBytes(length);
            }

            // byte array allocating
            byte[] key2 = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithmName, length);
            Assert.Equal(key1, key2);

            Span<byte> destinationBuffer = new byte[length + 2];
            Span<byte> destination = destinationBuffer.Slice(1, length);
            Rfc2898DeriveBytes.Pbkdf2(password, salt, destination, iterations, hashAlgorithmName);

            Assert.True(key1.AsSpan().SequenceEqual(destination), "key1 == destination");
            Assert.Equal(0, destinationBuffer[^1]); // Make sure we didn't write past the destination
            Assert.Equal(0, destinationBuffer[0]); // Make sure we didn't write before the destination
        }

        [Theory]
        [MemberData(nameof(Pbkdf2_Rfc6070_Vectors))]
        public static void Pbkdf2_Rfc6070(string password, string salt, int iterations, string expectedHex)
        {
            byte[] expected = expectedHex.HexToByteArray();
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, iterations, HashAlgorithmName.SHA1, expected.Length);
            Assert.Equal(expected, actual);
        }

        [Fact]
        [OuterLoop("Uses a high number of iterations that can take over 20 seconds on some machines")]
        public static void Pbkdf2_Rfc6070_HighIterations()
        {
            string password = "password";
            int iterations = 16777216;
            byte[] expected = "eefe3d61cd4da4e4e9945b3d6ba2158c2634e984".HexToByteArray();
            byte[] salt = Encoding.UTF8.GetBytes("salt");
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA1, expected.Length);
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> Pbkdf2_PasswordBytes_Compare_Data()
        {
            string largeInputHex = new string('A', 8192); // 8192 hex characters = 4096 bytes.

            foreach (HashAlgorithmName hashAlgorithm in SupportedHashAlgorithms)
            {
                // hashAlgorithm, length, iterations, passwordHex, saltHex
                yield return new object[] { hashAlgorithm.Name, 1, 1, s_passwordBytes.ByteArrayToHex(), s_salt.ByteArrayToHex() };
                yield return new object[] { hashAlgorithm.Name, 1, 1, "", s_salt.ByteArrayToHex() };
                yield return new object[] { hashAlgorithm.Name, 257, 257, "", s_salt.ByteArrayToHex() };
                yield return new object[] { hashAlgorithm.Name, 257, 257, "D8D8D8D8D8D8D8D8", "D8D8D8D8D8D8D8D8" };
                yield return new object[] { hashAlgorithm.Name, 257, 257, "0000000000000000", "0000000000000000" };
                yield return new object[] { hashAlgorithm.Name, 257, 257,  largeInputHex, largeInputHex };
            }

            // Test around HMAC SHA1 and SHA256 block boundaries
            for (int blockBoundary = 63; blockBoundary <= 65; blockBoundary++)
            {
                byte[] password = new byte[blockBoundary];
                yield return new object[] { HashAlgorithmName.SHA1.Name, 257, 257, password.ByteArrayToHex(), "0000000000000000" };
                yield return new object[] { HashAlgorithmName.SHA256.Name, 257, 257, password.ByteArrayToHex(), "0000000000000000" };
            }

            // Test around HMAC SHA384 and SHA512 block boundaries
            for (int blockBoundary = 127; blockBoundary <= 129; blockBoundary++)
            {
                byte[] password = new byte[blockBoundary];
                yield return new object[] { HashAlgorithmName.SHA384.Name, 257, 257, password.ByteArrayToHex(), "0000000000000000" };
                yield return new object[] { HashAlgorithmName.SHA512.Name, 257, 257, password.ByteArrayToHex(), "0000000000000000" };
            }
        }

        public static IEnumerable<object[]> Pbkdf2_PasswordString_Compare_Data()
        {
            string largePassword = new string('y', 1024);

            foreach (HashAlgorithmName hashAlgorithm in SupportedHashAlgorithms)
            {
                // hashAlgorithm, length, iterations, password, saltHex
                yield return new object[] { hashAlgorithm.Name, 1, 1, Password, s_salt.ByteArrayToHex() };
                yield return new object[] { hashAlgorithm.Name, 1, 1, "", s_salt.ByteArrayToHex() };
                yield return new object[] { hashAlgorithm.Name, 257, 257, "", s_salt.ByteArrayToHex() };
                yield return new object[] { hashAlgorithm.Name, 257, 257, Password, "D8D8D8D8D8D8D8D8" };
                yield return new object[] { hashAlgorithm.Name, 257, 257, Password, "0000000000000000" };

                // case for password exceeding the stack buffer limit.
                yield return new object[] { hashAlgorithm.Name, 257, 257, largePassword, "0000000000000000" };
            }
        }

        public static IEnumerable<object[]> Pbkdf2_Rfc6070_Vectors()
        {
            // password (P), salt (S), iterations (c), expected (DK)
            yield return new object[] { "password", "salt", 1, "0c60c80f961f0e71f3a9b524af6012062fe037a6" };
            yield return new object[] { "password", "salt", 2, "ea6c014dc72d6f8ccd1ed92ace1d41f0d8de8957" };
            yield return new object[] { "passwordPASSWORDpassword", "saltSALTsaltSALTsaltSALTsaltSALTsalt", 4096, "3d2eec4fe41c849b80c8d83662c0e44a8b291a964cf2f07038" };
            yield return new object[] { "pass\0word", "sa\0lt", 4096, "56fa6aa75548099dcc37d7f03425e0c3" };
        }

        private static HashAlgorithmName[] SupportedHashAlgorithms => new []
            {
                HashAlgorithmName.SHA1,
                HashAlgorithmName.SHA256,
                HashAlgorithmName.SHA384,
                HashAlgorithmName.SHA512
            };
    }
}
