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

        public static IEnumerable<object[]> Pbkdf2_PasswordBytes_Compare_Data()
        {
            foreach (HashAlgorithmName hashAlgorithm in SupportedHashAlgorithms)
            {
                // hashAlgorithm, length, iterations, passwordHex, saltHex
                yield return new object[] { hashAlgorithm.Name, 1, 1, s_passwordBytes.ByteArrayToHex(), s_salt.ByteArrayToHex() };
                yield return new object[] { hashAlgorithm.Name, 1, 1, "", s_salt.ByteArrayToHex() };
                yield return new object[] { hashAlgorithm.Name, 257, 257, "", s_salt.ByteArrayToHex() };
                yield return new object[] { hashAlgorithm.Name, 257, 257, "D8D8D8D8D8D8D8D8", "D8D8D8D8D8D8D8D8" };
                yield return new object[] { hashAlgorithm.Name, 257, 257, "0000000000000000", "0000000000000000" };
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

        private static HashAlgorithmName[] SupportedHashAlgorithms => new []
            {
                HashAlgorithmName.SHA1,
                HashAlgorithmName.SHA256,
                HashAlgorithmName.SHA384,
                HashAlgorithmName.SHA512
            };
    }
}
