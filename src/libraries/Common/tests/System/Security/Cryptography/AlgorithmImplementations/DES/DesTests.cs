// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static partial class DesTests
    {
        private static readonly byte[] KnownWeakKey = "e0e0e0e0f1f1f1f1".HexToByteArray();
        private static readonly byte[] KnownSemiWeakKey = "1f011f010e010e01".HexToByteArray();
        private static readonly byte[] KnownGoodKey = "87FF0737F868378F".HexToByteArray();
        private static readonly byte[] KnownShortKey = "00".HexToByteArray();

        [Fact]
        public static void DesDefaultCtor()
        {
            using (DES des = DESFactory.Create())
            {
                Assert.Equal(64, des.KeySize);
                Assert.Equal(64, des.BlockSize);
                Assert.Equal(CipherMode.CBC, des.Mode);
                Assert.Equal(PaddingMode.PKCS7, des.Padding);
            }
        }

        [Fact]
        public static void DesKeysValidation()
        {
            Assert.True(DES.IsWeakKey(KnownWeakKey));
            Assert.False(DES.IsWeakKey(KnownGoodKey));
            Assert.Throws<CryptographicException>(() => DES.IsWeakKey(null));
            Assert.Throws<CryptographicException>(() => DES.IsWeakKey(KnownShortKey));

            Assert.True(DES.IsSemiWeakKey(KnownSemiWeakKey));
            Assert.False(DES.IsSemiWeakKey(KnownGoodKey));
            Assert.Throws<CryptographicException>(() => DES.IsSemiWeakKey(null));
            Assert.Throws<CryptographicException>(() => DES.IsSemiWeakKey(KnownShortKey));

            using (DES des = DESFactory.Create())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => des.Key = KnownShortKey);
                Assert.Throws<CryptographicException>(() => des.Key = KnownSemiWeakKey);
                AssertExtensions.Throws<ArgumentNullException>("value", () => des.Key = null);

                des.Key = KnownGoodKey;
                Assert.Equal(KnownGoodKey, des.Key);
            }
        }

        [Fact]
        public static void DESKeySizeValidation()
        {
            using (DES des = DESFactory.Create())
            {
                des.KeySize = 64;
                Assert.Equal(64, des.KeySize);

                Assert.Throws<CryptographicException>(() => des.KeySize = 64 - 8);
                Assert.Throws<CryptographicException>(() => des.KeySize = 64 + 8);
            }
        }

        [Fact]
        public static void DESBlockSizeValidation()
        {
            using (DES des = DESFactory.Create())
            {
                des.BlockSize = 64;
                Assert.Equal(64, des.BlockSize);

                Assert.Throws<CryptographicException>(() => des.BlockSize = 63);
                Assert.Throws<CryptographicException>(() => des.BlockSize = 65);
            }
        }

        [Fact]
        public static void DesTransformBlockValidation()
        {
            using (DES des = DESFactory.Create())
            {
                AssertExtensions.Throws<ArgumentException>("rgbKey", () => des.CreateDecryptor(KnownShortKey, des.IV));
                AssertExtensions.Throws<ArgumentNullException>("rgbKey", () => des.CreateDecryptor(null, des.IV));
                Assert.Throws<CryptographicException>(() => des.CreateDecryptor(KnownWeakKey, des.IV));
                Assert.Throws<CryptographicException>(() => des.CreateDecryptor(KnownSemiWeakKey, des.IV));

                AssertExtensions.Throws<ArgumentException>("rgbKey", () => des.CreateEncryptor(KnownShortKey, des.IV));
                AssertExtensions.Throws<ArgumentNullException>("rgbKey", () => des.CreateEncryptor(null, des.IV));
                Assert.Throws<CryptographicException>(() => des.CreateEncryptor(KnownWeakKey, des.IV));
                Assert.Throws<CryptographicException>(() => des.CreateEncryptor(KnownSemiWeakKey, des.IV));
            }
        }
    }
}
