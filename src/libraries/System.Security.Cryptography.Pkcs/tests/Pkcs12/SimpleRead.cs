// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class SimpleRead
    {
        [Fact]
        public static void Test1()
        {
            var loader = (CertLoaderFromRawData)Certificates.RSAKeyTransferCapi1;
            ReadOnlyMemory<byte> pfxData = loader.PfxData;

            Pkcs12Info info = Pkcs12Info.Decode(pfxData, out int bytesConsumed);
            Assert.Equal(pfxData.Length, bytesConsumed);

            Assert.Equal(Pkcs12IntegrityMode.Password, info.IntegrityMode);
            CheckMac(info, loader.Password);

            ReadOnlyCollection<Pkcs12SafeContents> authSafe = info.AuthenticatedSafe;
            Assert.Same(authSafe, info.AuthenticatedSafe);
            Assert.Equal(2, authSafe.Count);

            Assert.Equal(Pkcs12ConfidentialityMode.None, authSafe[0].ConfidentialityMode);
            Assert.Equal(Pkcs12ConfidentialityMode.None, authSafe[1].ConfidentialityMode);

            List<Pkcs12SafeBag> safe0Bags = new List<Pkcs12SafeBag>(authSafe[0].GetBags());

            Assert.Equal(1, safe0Bags.Count);
            Pkcs12ShroudedKeyBag shroudedKeyBag = Assert.IsType<Pkcs12ShroudedKeyBag>(safe0Bags[0]);

            CryptographicAttributeObjectCollection keyBagAttrs = shroudedKeyBag.Attributes;
            Assert.Same(keyBagAttrs, shroudedKeyBag.Attributes);
            Assert.Equal(2, keyBagAttrs.Count);
            Assert.Equal(Oids.LocalKeyId, keyBagAttrs[0].Oid.Value);
            Assert.Equal(1, keyBagAttrs[0].Values.Count);
            Pkcs9LocalKeyId keyKeyId = Assert.IsType<Pkcs9LocalKeyId>(keyBagAttrs[0].Values[0]);
            Assert.Equal("1.3.6.1.4.1.311.17.1", keyBagAttrs[1].Oid.Value);
            Assert.Equal(1, keyBagAttrs[1].Values.Count);
            Pkcs9AttributeObject cspNameAttr = Assert.IsType<Pkcs9AttributeObject>(keyBagAttrs[1].Values[0]);

            byte[] cspNameBytes = Encoding.BigEndianUnicode.GetBytes("Microsoft Strong Cryptographic Provider");

            Assert.Equal(
                $"1E{cspNameBytes.Length:X2}{cspNameBytes.ByteArrayToHex()}",
                cspNameAttr.RawData.ByteArrayToHex());

            List<Pkcs12SafeBag> safe1Bags = new List<Pkcs12SafeBag>(authSafe[1].GetBags());

            Assert.Equal(1, safe0Bags.Count);
            Assert.IsType<Pkcs12CertBag>(safe1Bags[0]);
            Pkcs12CertBag certBag = (Pkcs12CertBag)safe1Bags[0];

            Assert.True(certBag.IsX509Certificate, "certBag.IsX509Certificate");
            Assert.InRange(certBag.EncodedCertificate.Length, loader.CerData.Length + 2, int.MaxValue);

            CryptographicAttributeObjectCollection certBagAttrs = certBag.Attributes;
            Assert.Same(certBagAttrs, certBag.Attributes);
            Assert.Equal(1, certBagAttrs.Count);
            Assert.Equal(Oids.LocalKeyId, certBagAttrs[0].Oid.Value);
            Assert.Equal(1, certBagAttrs[0].Values.Count);
            Pkcs9LocalKeyId certKeyId = Assert.IsType<Pkcs9LocalKeyId>(certBagAttrs[0].Values[0]);

            Assert.Equal(keyKeyId.KeyId.ByteArrayToHex(), certKeyId.KeyId.ByteArrayToHex());

            byte[] data = { 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            byte[] encrypted;

            using (X509Certificate2 fromLoader = loader.GetCertificate())
            using (X509Certificate2 fromBag = certBag.GetCertificate())
            using (RSA loaderPub = fromLoader.GetRSAPublicKey())
            {
                Assert.Equal(fromLoader.RawData, fromBag.RawData);

                encrypted = loaderPub.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
            }

            int bytesRead;

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportEncryptedPkcs8PrivateKey(
                    loader.Password,
                    shroudedKeyBag.EncryptedPkcs8PrivateKey.Span,
                    out bytesRead);

                byte[] dec = rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA1);
                Assert.Equal(data, dec);
            }

            Assert.Equal(shroudedKeyBag.EncryptedPkcs8PrivateKey.Length, bytesRead);
        }

        [Fact]
        public static void ReadWithEncryptedContents()
        {
            var loader = (CertLoaderFromRawData)Certificates.RSAKeyTransfer_ExplicitSki;
            ReadOnlyMemory<byte> pfxData = loader.PfxData;

            Pkcs12Info info = Pkcs12Info.Decode(pfxData, out int bytesConsumed);
            Assert.Equal(pfxData.Length, bytesConsumed);

            Assert.Equal(Pkcs12IntegrityMode.Password, info.IntegrityMode);
            CheckMac(info, loader.Password);

            ReadOnlyCollection<Pkcs12SafeContents> authSafe = info.AuthenticatedSafe;
            Assert.Same(authSafe, info.AuthenticatedSafe);
            Assert.Equal(2, authSafe.Count);

            Assert.Equal(Pkcs12ConfidentialityMode.Password, authSafe[0].ConfidentialityMode);
            Assert.Equal(Pkcs12ConfidentialityMode.None, authSafe[1].ConfidentialityMode);

            Assert.ThrowsAny<CryptographicException>(
                () => authSafe[0].Decrypt(loader.Password.AsSpan().Slice(1)));

            Assert.Equal(Pkcs12ConfidentialityMode.Password, authSafe[0].ConfidentialityMode);
            authSafe[0].Decrypt(loader.Password);
            Assert.Equal(Pkcs12ConfidentialityMode.None, authSafe[0].ConfidentialityMode);

            List<Pkcs12SafeBag> safe0Bags = new List<Pkcs12SafeBag>(authSafe[0].GetBags());
            Assert.Equal(1, safe0Bags.Count);
            Pkcs12CertBag certBag = Assert.IsType<Pkcs12CertBag>(safe0Bags[0]);

            Assert.True(certBag.IsX509Certificate, "certBag.IsX509Certificate");
            Assert.InRange(certBag.EncodedCertificate.Length, loader.CerData.Length + 2, int.MaxValue);

            List<Pkcs12SafeBag> safe1Bags = new List<Pkcs12SafeBag>(authSafe[1].GetBags());
            Assert.Equal(1, safe0Bags.Count);
            Pkcs12ShroudedKeyBag shroudedKeyBag = Assert.IsType<Pkcs12ShroudedKeyBag>(safe1Bags[0]);

            byte[] data = { 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            byte[] encrypted;

            using (X509Certificate2 fromLoader = loader.GetCertificate())
            using (X509Certificate2 fromBag = certBag.GetCertificate())
            using (RSA loaderPub = fromLoader.GetRSAPublicKey())
            {
                Assert.Equal(fromLoader.RawData, fromBag.RawData);

                encrypted = loaderPub.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
            }

            int bytesRead;

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportEncryptedPkcs8PrivateKey(
                    loader.Password,
                    shroudedKeyBag.EncryptedPkcs8PrivateKey.Span,
                    out bytesRead);

                byte[] dec = rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA1);
                Assert.Equal(data, dec);
            }

            Assert.Equal(shroudedKeyBag.EncryptedPkcs8PrivateKey.Length, bytesRead);
        }

        private static void CheckMac(Pkcs12Info info, string password)
        {
            Assert.True(info.VerifyMac(password), "VerifyMac (correct password)");
            Assert.False(info.VerifyMac(ReadOnlySpan<char>.Empty), "VerifyMac (empty password)");
            Assert.False(info.VerifyMac(password + password), "VerifyMac (doubled password)");
            Assert.False(info.VerifyMac(new string('a', 1048)), "VerifyMac (password > 1k)");
        }
    }
}
