// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(X25519DiffieHellmanNotSupportedTests), nameof(X25519DiffieHellmanNotSupportedTests.IsNotSupported))]
    public static class X25519DiffieHellmanNotSupportedTests
    {
        public static bool IsNotSupported => !X25519DiffieHellman.IsSupported;

        [Fact]
        public static void Generate_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => X25519DiffieHellman.GenerateKey());
        }

        [Fact]
        public static void ImportPrivateKey_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportPrivateKey(new byte[X25519DiffieHellman.PrivateKeySizeInBytes]));

            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportPrivateKey(new ReadOnlySpan<byte>(new byte[X25519DiffieHellman.PrivateKeySizeInBytes])));
        }

        [Fact]
        public static void ImportPublicKey_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportPublicKey(new byte[X25519DiffieHellman.PublicKeySizeInBytes]));

            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportPublicKey(new ReadOnlySpan<byte>(new byte[X25519DiffieHellman.PublicKeySizeInBytes])));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_NotSupported()
        {
            // A minimal valid SPKI for X25519
            byte[] spki = Convert.FromHexString(
                "302a300506032b656e032100" +
                "0000000000000000000000000000000000000000000000000000000000000000");

            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportSubjectPublicKeyInfo(spki));

            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportSubjectPublicKeyInfo(new ReadOnlySpan<byte>(spki)));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_NotSupported()
        {
            // A minimal valid PKCS#8 for X25519
            byte[] pkcs8 = Convert.FromHexString(
                "302e020100300506032b656e04220420" +
                "0000000000000000000000000000000000000000000000000000000000000000");

            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8));

            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(pkcs8)));
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_NotSupported()
        {
            // Use an encrypted PKCS#8 blob. The implementation should throw PlatformNotSupportedException
            // before attempting decryption.
            byte[] pkcs8 = Convert.FromHexString(
                "302e020100300506032b656e04220420" +
                "0000000000000000000000000000000000000000000000000000000000000000");

            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey("password", pkcs8));

            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey("password".AsSpan(), pkcs8));

            Assert.Throws<PlatformNotSupportedException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey("password"u8, pkcs8));
        }

        [Fact]
        public static void ImportFromPem_NotSupported()
        {
            string pem = """
            -----BEGIN THING-----
            Should throw before even attempting to read the PEM
            -----END THING-----
            """;
            Assert.Throws<PlatformNotSupportedException>(() => X25519DiffieHellman.ImportFromPem(pem));
            Assert.Throws<PlatformNotSupportedException>(() => X25519DiffieHellman.ImportFromPem(pem.AsSpan()));
        }
    }
}
