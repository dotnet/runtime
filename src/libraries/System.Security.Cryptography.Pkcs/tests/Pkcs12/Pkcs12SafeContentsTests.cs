// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class Pkcs12SafeContentsTests
    {
        private static readonly Oid s_zeroOid = new Oid("0.0", "0.0");
        private static readonly ReadOnlyMemory<byte> s_derNull = new byte[] { 0x05, 0x00 };

        private static readonly PbeParameters s_pbeParameters = new PbeParameters(
            PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
            HashAlgorithmName.SHA1,
            2048);

        [Fact]
        public static void StartsInReadWriteNoConfidentialityMode()
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            Assert.Equal(Pkcs12ConfidentialityMode.None, contents.ConfidentialityMode);
            Assert.False(contents.IsReadOnly);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddBagDisallowsNull(bool forReadOnly)
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();

            if (forReadOnly)
            {
                contents = MakeReadonly(contents);
            }

            AssertExtensions.Throws<ArgumentNullException>(
                "safeBag",
                () => contents.AddSafeBag(null));
        }

        [Fact]
        public static void AddBagDisallowedInReadOnly()
        {
            Pkcs12SafeContents contents = MakeReadonly(new Pkcs12SafeContents());
            Pkcs12CertBag certBag = new Pkcs12CertBag(s_zeroOid, s_derNull);

            Assert.True(contents.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => contents.AddSafeBag(certBag));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddCertificateDisallowsNull(bool forReadOnly)
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();

            if (forReadOnly)
            {
                contents = MakeReadonly(contents);
            }

            AssertExtensions.Throws<ArgumentNullException>(
                "certificate",
                () => contents.AddCertificate(null));
        }

        [Fact]
        public static void AddCertificateDisallowedInReadOnly()
        {
            Pkcs12SafeContents contents = MakeReadonly(new Pkcs12SafeContents());
            X509Certificate2 cert = new X509Certificate2(stackalloc byte[0]);

            Assert.Throws<InvalidOperationException>(() => contents.AddCertificate(cert));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddKeyUnencryptedDisallowsNull(bool forReadOnly)
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();

            if (forReadOnly)
            {
                contents = MakeReadonly(contents);
            }

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => contents.AddKeyUnencrypted(null));
        }

        [Fact]
        public static void AddKeyUnencryptedDisallowedInReadOnly()
        {
            Pkcs12SafeContents contents = MakeReadonly(new Pkcs12SafeContents());

            using (RSA rsa = RSA.Create())
            {
                Assert.Throws<InvalidOperationException>(() => contents.AddKeyUnencrypted(rsa));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddNestedContentsDisallowsNull(bool forReadOnly)
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();

            if (forReadOnly)
            {
                contents = MakeReadonly(contents);
            }

            AssertExtensions.Throws<ArgumentNullException>(
                "safeContents",
                () => contents.AddNestedContents(null));
        }

        [Fact]
        public static void AddNestedContentsDisallowedInReadOnly()
        {
            Pkcs12SafeContents outerContents = MakeReadonly(new Pkcs12SafeContents());

            Pkcs12SafeContents innerContents = new Pkcs12SafeContents();

            Assert.Throws<InvalidOperationException>(
                () => outerContents.AddNestedContents(innerContents));
        }

        [Fact]
        public static void AddEncryptedNestedContents()
        {
            Pkcs12Builder builder = new Pkcs12Builder();
            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            contents.AddSecret(s_zeroOid, s_derNull);

            builder.AddSafeContentsEncrypted(contents, "hi", s_pbeParameters);
            builder.SealWithoutIntegrity();

            byte[] encoded = builder.Encode();
            Pkcs12Info info = Pkcs12Info.Decode(encoded, out _, skipCopy: true);
            Assert.Equal(Pkcs12IntegrityMode.None, info.IntegrityMode);
            Assert.Equal(1, info.AuthenticatedSafe.Count);

            Pkcs12SafeContents newContents = new Pkcs12SafeContents();

            AssertExtensions.Throws<ArgumentException>(
                "safeContents",
                () => newContents.AddNestedContents(info.AuthenticatedSafe[0]));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddShroudedKeyWithBytesDisallowsNull(bool forReadOnly)
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();

            if (forReadOnly)
            {
                contents = MakeReadonly(contents);
            }

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => contents.AddShroudedKey(null, ReadOnlySpan<byte>.Empty, s_pbeParameters));

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => contents.AddShroudedKey(null, Array.Empty<byte>(), s_pbeParameters));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddShroudedKeyWithCharsDisallowsNull(bool forReadOnly)
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();

            if (forReadOnly)
            {
                contents = MakeReadonly(contents);
            }

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => contents.AddShroudedKey(null, ReadOnlySpan<char>.Empty, s_pbeParameters));
        }

        [Fact]
        public static void AddShroudedKeyWithBytesDisallowedInReadOnly()
        {
            Pkcs12SafeContents contents = MakeReadonly(new Pkcs12SafeContents());

            using (RSA rsa = RSA.Create())
            {
                Assert.Throws<InvalidOperationException>(
                    () => contents.AddShroudedKey(rsa, ReadOnlySpan<byte>.Empty, s_pbeParameters));
            }
        }

        [Fact]
        public static void AddShroudedKeyWithCharsDisallowedInReadOnly()
        {
            Pkcs12SafeContents contents = MakeReadonly(new Pkcs12SafeContents());

            using (RSA rsa = RSA.Create())
            {
                Assert.Throws<InvalidOperationException>(
                    () => contents.AddShroudedKey(rsa, ReadOnlySpan<char>.Empty, s_pbeParameters));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void DecryptThrowsWhenNotPasswordMode(bool forReadOnly)
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();

            if (forReadOnly)
            {
                contents = MakeReadonly(contents);
            }

            Assert.Equal(Pkcs12ConfidentialityMode.None, contents.ConfidentialityMode);
            Assert.Throws<InvalidOperationException>(() => contents.Decrypt("test"));
        }

        [Fact]
        public static void DecryptThrowsForWrongPassword()
        {
            var loader = (CertLoaderFromRawData)Certificates.RSAKeyTransfer_ExplicitSki;
            ReadOnlyMemory<byte> pfxData = loader.PfxData;
            Pkcs12Info info = Pkcs12Info.Decode(pfxData, out _, skipCopy: true);
            Pkcs12SafeContents firstContents = info.AuthenticatedSafe[0];

            Assert.Equal(
                Pkcs12ConfidentialityMode.Password,
                firstContents.ConfidentialityMode);

            // This password experimentally encounters a PKCS7 padding verification error.
            Assert.ThrowsAny<CryptographicException>(() => firstContents.Decrypt("000"));
            // This password experimentally does not get a PKCS7 padding error,
            // but gets a deserialization error
            Assert.ThrowsAny<CryptographicException>(() => firstContents.Decrypt("0G7"));
        }

        [Fact]
        public static void GetBagsThrowsForConfidentialData()
        {
            var loader = (CertLoaderFromRawData)Certificates.RSAKeyTransfer_ExplicitSki;
            ReadOnlyMemory<byte> pfxData = loader.PfxData;
            Pkcs12Info info = Pkcs12Info.Decode(pfxData, out _, skipCopy: true);
            Pkcs12SafeContents firstContents = info.AuthenticatedSafe[0];

            Assert.Equal(
                Pkcs12ConfidentialityMode.Password,
                firstContents.ConfidentialityMode);

            Assert.Throws<InvalidOperationException>(() => firstContents.GetBags());
        }

        [Fact]
        public static void EncodeGrowsWhenNeeded()
        {
            Pkcs12Builder builder = new Pkcs12Builder();
            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            byte[] bigBuf = new byte[16 * 1024 + 4];
            bigBuf[0] = 0x04;
            bigBuf[1] = 0x82;
            ref byte upper = ref bigBuf[2];
            ref byte lower = ref bigBuf[3];

            foreach (int size in new[] { 1024, 2048, 4096, 8192, 5184, 16 * 1024})
            {
                lower = (byte)size;
                upper = (byte)(size >> 8);
                contents.AddSecret(s_zeroOid, bigBuf.AsMemory(0, 4 + size));
            }

            builder.AddSafeContentsUnencrypted(contents);
            builder.SealWithMac("hi", HashAlgorithmName.SHA1, 120);
            byte[] output = builder.Encode();
            Assert.NotNull(output);
        }

        private static Pkcs12SafeContents MakeReadonly(Pkcs12SafeContents contents)
        {
            Pkcs12Builder builder = new Pkcs12Builder();
            builder.AddSafeContentsUnencrypted(contents);
            builder.SealWithMac(ReadOnlySpan<char>.Empty, HashAlgorithmName.SHA1, 1);
            Pkcs12Info info = Pkcs12Info.Decode(builder.Encode(), out _, skipCopy: true);
            return info.AuthenticatedSafe.Single();
        }
    }
}
