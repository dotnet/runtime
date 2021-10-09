// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class Pkcs12InfoTests
    {
        private static ReadOnlyMemory<byte> PadContents(ReadOnlyMemory<byte> contents, int trailingByteCount)
        {
            if (trailingByteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(trailingByteCount));

            if (trailingByteCount == 0)
            {
                return contents;
            }

            byte[] tmp = new byte[contents.Length + trailingByteCount];
            contents.Span.CopyTo(tmp);
            tmp.AsSpan(contents.Length).Fill(0xA5);
            return tmp;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public static void ReadEmptyPfx(int trailingByteCount)
        {
            ReadOnlyMemory<byte> source = PadContents(Pkcs12Documents.EmptyPfx, trailingByteCount);

            Pkcs12Info info =
                Pkcs12Info.Decode(source, out int bytesRead, skipCopy: true);

            Assert.Equal(Pkcs12Documents.EmptyPfx.Length, bytesRead);
            Assert.Equal(Pkcs12IntegrityMode.Password, info.IntegrityMode);

            Assert.False(info.VerifyMac("hello"), "Wrong password");
            Assert.True(info.VerifyMac(ReadOnlySpan<char>.Empty), "null password (ReadOnlySpan)");
            Assert.True(info.VerifyMac(null), "null password (string)");
            Assert.False(info.VerifyMac("".AsSpan()), "empty password (ReadOnlySpan)");
            Assert.False(info.VerifyMac(""), "empty password (string)");
            Assert.False(info.VerifyMac("hello".AsSpan(5)), "sliced out");
            Assert.False(info.VerifyMac("hello".AsSpan(0, 0)), "zero-sliced");
            Assert.False(info.VerifyMac(new char[0]), "empty array");
            Assert.False(info.VerifyMac((new char[1]).AsSpan(1)), "sliced out array");
            Assert.False(info.VerifyMac((new char[1]).AsSpan(0, 0)), "zero-sliced array");

            ReadOnlyCollection<Pkcs12SafeContents> safes = info.AuthenticatedSafe;
            Assert.Equal(0, safes.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public static void ReadIndefiniteEncodingNoMac(int trailingByteCount)
        {
            ReadOnlyMemory<byte> source = PadContents(Pkcs12Documents.IndefiniteEncodingNoMac, trailingByteCount);

            Pkcs12Info info = Pkcs12Info.Decode(
                source,
                out int bytesRead,
                skipCopy: true);

            Assert.Equal(Pkcs12Documents.IndefiniteEncodingNoMac.Length, bytesRead);
            Assert.Equal(Pkcs12IntegrityMode.None, info.IntegrityMode);

            ReadOnlyCollection<Pkcs12SafeContents> safes = info.AuthenticatedSafe;
            Assert.Equal(2, safes.Count);

            Pkcs12SafeContents firstSafe = safes[0];
            Pkcs12SafeContents secondSafe = safes[1];

            Assert.Equal(Pkcs12ConfidentialityMode.None, firstSafe.ConfidentialityMode);
            Assert.Equal(Pkcs12ConfidentialityMode.None, secondSafe.ConfidentialityMode);

            Assert.True(firstSafe.IsReadOnly, "firstSafe.IsReadOnly");
            Assert.True(secondSafe.IsReadOnly, "secondSafe.IsReadOnly");

            Pkcs12SafeBag[] firstContents = firstSafe.GetBags().ToArray();
            Pkcs12SafeBag[] secondContents = secondSafe.GetBags().ToArray();

            Assert.Equal(1, firstContents.Length);
            Assert.Equal(1, secondContents.Length);

            Pkcs12KeyBag keyBag = Assert.IsType<Pkcs12KeyBag>(firstContents[0]);
            Pkcs12CertBag certBag = Assert.IsType<Pkcs12CertBag>(secondContents[0]);

            CryptographicAttributeObjectCollection keyBagAttrs = keyBag.Attributes;
            CryptographicAttributeObjectCollection certBagAttrs = certBag.Attributes;

            Assert.Equal(2, keyBagAttrs.Count);
            Assert.Equal(2, certBagAttrs.Count);

            Assert.Equal(Oids.FriendlyName, keyBagAttrs[0].Oid.Value);
            Assert.Equal(1, keyBagAttrs[0].Values.Count);
            Assert.Equal(Oids.LocalKeyId, keyBagAttrs[1].Oid.Value);
            Assert.Equal(1, keyBagAttrs[1].Values.Count);

            Pkcs9AttributeObject keyFriendlyName =
                Assert.IsAssignableFrom<Pkcs9AttributeObject>(keyBagAttrs[0].Values[0]);

            Pkcs9LocalKeyId keyKeyId = Assert.IsType<Pkcs9LocalKeyId>(keyBagAttrs[1].Values[0]);

            Assert.Equal(Oids.FriendlyName, certBagAttrs[0].Oid.Value);
            Assert.Equal(1, certBagAttrs[0].Values.Count);
            Assert.Equal(Oids.LocalKeyId, certBagAttrs[1].Oid.Value);
            Assert.Equal(1, certBagAttrs[1].Values.Count);

            Pkcs9AttributeObject certFriendlyName =
                Assert.IsAssignableFrom<Pkcs9AttributeObject>(certBagAttrs[0].Values[0]);

            Pkcs9LocalKeyId certKeyId = Assert.IsType<Pkcs9LocalKeyId>(certBagAttrs[1].Values[0]);

            // This PFX gave a friendlyName value of "cert" to both the key and the cert.
            Assert.Equal("1E080063006500720074", keyFriendlyName.RawData.ByteArrayToHex());
            Assert.Equal(keyFriendlyName.RawData, certFriendlyName.RawData);

            // The private key (KeyBag) and the public key (CertBag) are matched from their keyId value.
            Assert.Equal("0414EDF3D122CF623CF0CFC9CD226261E8415A83E630", keyKeyId.RawData.ByteArrayToHex());
            Assert.Equal("EDF3D122CF623CF0CFC9CD226261E8415A83E630", keyKeyId.KeyId.ByteArrayToHex());
            Assert.Equal(keyKeyId.RawData, certKeyId.RawData);

            using (X509Certificate2 cert = certBag.GetCertificate())
            using (RSA privateKey = RSA.Create())
            using (RSA publicKey = cert.GetRSAPublicKey())
            {
                privateKey.ImportPkcs8PrivateKey(keyBag.Pkcs8PrivateKey.Span, out _);

                Assert.Equal(
                    publicKey.ExportSubjectPublicKeyInfo().ByteArrayToHex(),
                    privateKey.ExportSubjectPublicKeyInfo().ByteArrayToHex());
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public static void ReadOracleWallet(int trailingByteCount)
        {
            ReadOnlyMemory<byte> source = PadContents(Pkcs12Documents.SimpleOracleWallet, trailingByteCount);

            Pkcs12Info info = Pkcs12Info.Decode(
                source,
                out int bytesRead,
                skipCopy: true);

            Assert.Equal(Pkcs12Documents.SimpleOracleWallet.Length, bytesRead);
            Assert.Equal(Pkcs12IntegrityMode.Password, info.IntegrityMode);
            Assert.False(info.VerifyMac(ReadOnlySpan<char>.Empty), "VerifyMac(no password)");
            Assert.False(info.VerifyMac(""), "VerifyMac(empty password)");
            Assert.True(info.VerifyMac(Pkcs12Documents.OracleWalletPassword), "VerifyMac(correct password)");

            ReadOnlyCollection<Pkcs12SafeContents> authSafes = info.AuthenticatedSafe;
            Assert.Equal(1, authSafes.Count);

            Pkcs12SafeContents authSafe = authSafes[0];
            Assert.Equal(Pkcs12ConfidentialityMode.Password, authSafe.ConfidentialityMode);
            // Wrong password
            Assert.ThrowsAny<CryptographicException>(() => authSafe.Decrypt(ReadOnlySpan<char>.Empty));
            authSafe.Decrypt(Pkcs12Documents.OracleWalletPassword);

            Assert.Equal(Pkcs12ConfidentialityMode.None, authSafe.ConfidentialityMode);

            List<Pkcs12SafeBag> bags = authSafe.GetBags().ToList();
            Assert.Equal(4, bags.Count);

            CheckOracleSecretBag(
                bags[0],
                "oracle.security.client.connect_string1",
                "a_prod_db",
                "E6B652DD0000000400000000000000060000000300000000");

            CheckOracleSecretBag(
                bags[1],
                "a@#3#@b", "{pwd_cred_type}@#4#@NEVER_EXPIRE@#5#@c@#111#@d",
                "E6B652DD0000000400000000000000060000000200000000");

            CheckOracleSecretBag(
                bags[2],
                "oracle.security.client.username1",
                "a_test_user",
                "E6B652DD0000000400000000000000060000000100000000");

            CheckOracleSecretBag(
                bags[3],
                "oracle.security.client.password1",
                "potatos are tasty",
                "E6B652DD0000000400000000000000060000000000000000");
        }

        private static void CheckOracleSecretBag(
            Pkcs12SafeBag safeBag,
            string key,
            string value,
            string keyId)
        {
            Pkcs12SecretBag secretBag = Assert.IsType<Pkcs12SecretBag>(safeBag);
            Assert.Equal("1.2.840.113549.1.16.12.12", secretBag.GetSecretType().Value);

            Assert.Equal(
                MakeOracleKeyValuePairHex(key, value),
                secretBag.SecretValue.ByteArrayToHex());

            CryptographicAttributeObjectCollection attrs = secretBag.Attributes;
            Assert.Equal(1, attrs.Count);
            CryptographicAttributeObject firstAttr = attrs[0];
            Assert.Equal(Oids.LocalKeyId, firstAttr.Oid.Value);
            Assert.Equal(1, firstAttr.Values.Count);
            Pkcs9LocalKeyId localKeyId = Assert.IsType<Pkcs9LocalKeyId>(firstAttr.Values[0]);
            Assert.Equal(keyId, localKeyId.KeyId.ByteArrayToHex());
        }

        private static string MakeOracleKeyValuePairHex(string key, string value)
        {
            Span<byte> outputBuf = stackalloc byte[129];
            outputBuf[0] = 0x30;
            // Two children, one byte tag, one byte length.  Add content length as it comes.
            ref byte totalContents = ref outputBuf[1];
            totalContents = 0x04;

            outputBuf[2] = 0x0C;
            ref byte keyLen = ref outputBuf[3];
            Span<byte> keyDest = outputBuf.Slice(4);
            keyLen = (byte)Encoding.UTF8.GetBytes(key, keyDest);
            totalContents += keyLen;

            keyDest[keyLen] = 0x0C;
            ref byte valLen = ref keyDest[keyLen + 1];
            Span<byte> valDest = keyDest.Slice(keyLen + 2);
            valLen = (byte)Encoding.UTF8.GetBytes(value, valDest);
            totalContents += valLen;

            Assert.InRange(totalContents, 4, 127);
            return outputBuf.Slice(0, totalContents + 2).ByteArrayToHex();
        }

        [Fact]
        public static void VerifyMacWithNoMac()
        {
            const string FullyEmptyHex =
                "3016020103301106092A864886F70D010701A00404023000";

            Pkcs12Info info = Pkcs12Info.Decode(FullyEmptyHex.HexToByteArray(), out _, skipCopy: true);
            Assert.Equal(Pkcs12IntegrityMode.None, info.IntegrityMode);

            Assert.Throws<InvalidOperationException>(() => info.VerifyMac("hi"));
        }

        [Theory]
        [MemberData(nameof(DecodeWithHighPbeIterationsData))]
        public static void DecodeWithHighPbeIterations(
            PbeEncryptionAlgorithm encryptionAlgorithm,
            HashAlgorithmName hashAlgorithmName,
            int iterations)
        {
            string pw = nameof(CreateAndSealPkcs12);
            byte[] data = CreateAndSealPkcs12(encryptionAlgorithm, hashAlgorithmName, iterations);
            Pkcs12Info info = Pkcs12Info.Decode(data, out _, skipCopy: true);
            info.AuthenticatedSafe.Single().Decrypt(pw);
        }

        [Fact]
        public static void DecodeWithTooManyPbeIterations()
        {
            const string pw = "test";
            Pkcs12Info info = Pkcs12Info.Decode(Pkcs12Documents.HighPbeIterationCount3Des, out _, skipCopy: true);
            foreach (Pkcs12SafeContents contents in info.AuthenticatedSafe)
            {
                if (contents.ConfidentialityMode == Pkcs12ConfidentialityMode.None)
                {
                    continue;
                }

                Assert.ThrowsAny<CryptographicException>(() => contents.Decrypt(pw));
            }
        }

        public static IEnumerable<object[]> DecodeWithHighPbeIterationsData
        {
            get
            {
                // On Android, the tests run out of memory when running DecodeWithHighPbeIterations and causes a crash in CI
                // Instead of disabling the whole suite, we opted to reduce the number of iterations to help the tests pass
                yield return new object[] { PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, OperatingSystem.IsAndroid() ? 7000 : 700_000 };
                yield return new object[] { PbeEncryptionAlgorithm.Aes192Cbc, HashAlgorithmName.SHA256, OperatingSystem.IsAndroid() ? 7000 : 700_000 };
                yield return new object[] { PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, OperatingSystem.IsAndroid() ? 7000 : 700_000 };
                yield return new object[] { PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, OperatingSystem.IsAndroid() ? 6000 : 600_000 };
            }
        }

        private static byte[] CreateAndSealPkcs12(
            PbeEncryptionAlgorithm encryptionAlgorithm,
            HashAlgorithmName hashAlgorithmName,
            int iterations)
        {
            PbeParameters pbeParameters = new PbeParameters(
                encryptionAlgorithm,
                hashAlgorithmName,
                iterations);

            using RSA rsa = RSA.Create();
            string pw = nameof(CreateAndSealPkcs12);
            Pkcs12Builder builder = new Pkcs12Builder();
            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            contents.AddShroudedKey(rsa, pw, pbeParameters);
            builder.AddSafeContentsEncrypted(contents, pw, pbeParameters);
            builder.SealWithMac(pw, hashAlgorithmName, iterations);

            return builder.Encode();
        }
    }
}
