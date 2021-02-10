// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.DeriveBytesTests
{
    [SkipOnMono("Not supported on Browser", TestPlatforms.Browser)]
    public static class Rfc2898OneShotTests
    {
        private const string Password = "tired";

        private static readonly byte[] s_passwordBytes = Encoding.UTF8.GetBytes(Password);
        private static readonly byte[] s_salt = new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16
        };
        private static readonly int s_extractLength = 14;

        [Fact]
        public static void Pbkdf2DeriveBytes_PasswordBytes_NullPassword()
        {
            AssertExtensions.Throws<ArgumentNullException>("password", () =>
                Rfc2898DeriveBytes.Pbkdf2DeriveBytes(
                    password: (byte[])null, s_salt, iterations: 1000, s_extractLength, HashAlgorithmName.SHA256)
            );
        }

        [Fact]
        public static void Pbkdf2DeriveBytes_PasswordBytes_NullSalt()
        {
            AssertExtensions.Throws<ArgumentNullException>("salt", () =>
                Rfc2898DeriveBytes.Pbkdf2DeriveBytes(
                    s_passwordBytes, salt: (byte[])null, iterations: 1000, s_extractLength, HashAlgorithmName.SHA256)
            );
        }

        [Fact]
        public static void Pbkdf2DeriveBytes_PasswordBytes_SaltBytes_TooShort()
        {
            ArgumentOutOfRangeException ex = AssertExtensions.Throws<ArgumentOutOfRangeException>("salt", () =>
                Rfc2898DeriveBytes.Pbkdf2DeriveBytes(
                    s_passwordBytes, salt: new byte[0], iterations: 1000, s_extractLength, HashAlgorithmName.SHA256)
            );

            Assert.Contains("16", ex.Message); // Exception should be formatted with the minimum salt length.
        }

        [Fact]
        public static void Pbkdf2DeriveBytes_PasswordBytes_SaltBytes_IterationsTooLow()
        {
            ArgumentOutOfRangeException ex = AssertExtensions.Throws<ArgumentOutOfRangeException>("iterations", () =>
                Rfc2898DeriveBytes.Pbkdf2DeriveBytes(
                    s_passwordBytes, s_salt, iterations: 999, s_extractLength, HashAlgorithmName.SHA256)
            );

            Assert.Contains("1000", ex.Message); // Exception should be formatted with the minimum iterations.
        }

        [Theory]
        [InlineData(nameof(HashAlgorithmName.SHA1))]
        [InlineData(nameof(HashAlgorithmName.SHA256))]
        [InlineData(nameof(HashAlgorithmName.SHA384))]
        [InlineData(nameof(HashAlgorithmName.SHA512))]
        public static void Pbkdf2DeriveBytes_PasswordBytes_SaltBytes_Success(string hashAlgorithm)
        {
            HashAlgorithmName hashAlgorithmName = new HashAlgorithmName(hashAlgorithm);
            using Rfc2898DeriveBytes instanceKdf = new Rfc2898DeriveBytes(s_passwordBytes, s_salt, iterations: 1000, hashAlgorithmName);
            byte[] key1 = instanceKdf.GetBytes(s_extractLength);
            byte[] key2 = Rfc2898DeriveBytes.Pbkdf2DeriveBytes(
                s_passwordBytes, s_salt, iterations: 1000, s_extractLength, hashAlgorithmName);
            Assert.Equal(key1, key2);
        }
    }
}
