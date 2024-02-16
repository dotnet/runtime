// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.ExtensionsTests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public static class SubjectKeyIdentifierTests
    {
        private const string EcPublicKey = "1.2.840.10045.2.1";
        private static ReadOnlySpan<byte> NistP256r1 => [0x06, 0x08, 0x2a, 0x86, 0x48, 0xce, 0x3d, 0x03, 0x01, 0x07];
        private static ReadOnlySpan<byte> BrainpoolP256r1 => [0x06, 0x09, 0x2b, 0x24, 0x03, 0x03, 0x02, 0x08, 0x01, 0x01, 0x07];

        [Fact]
        public static void DefaultConstructor()
        {
            X509SubjectKeyIdentifierExtension e = new X509SubjectKeyIdentifierExtension();

            string oidValue = e.Oid.Value;
            Assert.Equal("2.5.29.14", oidValue);

            Assert.Empty(e.RawData);

            string skid = e.SubjectKeyIdentifier;
            Assert.Null(skid);

            Assert.Throws<CryptographicException>(() => e.SubjectKeyIdentifierBytes);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void EncodeFromBytes(bool fromSpan)
        {
            byte[] sk = { 1, 2, 3, 4 };
            X509SubjectKeyIdentifierExtension e;

            if (fromSpan)
            {
                e = new X509SubjectKeyIdentifierExtension(new ReadOnlySpan<byte>(sk), false);
            }
            else
            {
                e = new X509SubjectKeyIdentifierExtension(sk, false);
            }

            byte[] rawData = e.RawData;
            Assert.Equal("040401020304".HexToByteArray(), rawData);

            if (fromSpan)
            {
                e = new X509SubjectKeyIdentifierExtension(
                    new AsnEncodedData(new ReadOnlySpan<byte>(rawData)),
                    false);
            }
            else
            {
                e = new X509SubjectKeyIdentifierExtension(new AsnEncodedData(rawData), false);
            }

            string skid = e.SubjectKeyIdentifier;
            Assert.Equal("01020304", skid);

            AssertExtensions.SequenceEqual(sk, e.SubjectKeyIdentifierBytes.Span);
        }

        [Fact]
        public static void EncodeFromString()
        {
            string sk = "01ABcd";
            X509SubjectKeyIdentifierExtension e = new X509SubjectKeyIdentifierExtension(sk, false);

            byte[] rawData = e.RawData;
            Assert.Equal("040301abcd".HexToByteArray(), rawData);

            e = new X509SubjectKeyIdentifierExtension(new AsnEncodedData(rawData), false);
            string skid = e.SubjectKeyIdentifier;
            Assert.Equal("01ABCD", skid);
            Assert.Equal(skid, Convert.ToHexString(e.SubjectKeyIdentifierBytes.Span));

            ReadOnlyMemory<byte> ski1 = e.SubjectKeyIdentifierBytes;
            ReadOnlyMemory<byte> ski2 = e.SubjectKeyIdentifierBytes;
            Assert.True(ski1.Span == ski2.Span, "Two calls to SubjectKeyIdentifierBytes return the same buffer");
        }

        [Fact]
        public static void EncodeFromPublicKey()
        {
            PublicKey pk;

            using (var cert = new X509Certificate2(TestData.MsCertificate))
            {
                pk = cert.PublicKey;
            }

            X509SubjectKeyIdentifierExtension e = new X509SubjectKeyIdentifierExtension(pk, false);

            byte[] rawData = e.RawData;
            Assert.Equal("04145971a65a334dda980780ff841ebe87f9723241f2".HexToByteArray(), rawData);

            e = new X509SubjectKeyIdentifierExtension(new AsnEncodedData(rawData), false);
            string skid = e.SubjectKeyIdentifier;
            Assert.Equal("5971A65A334DDA980780FF841EBE87F9723241F2", skid);
            Assert.Equal(skid, Convert.ToHexString(e.SubjectKeyIdentifierBytes.Span));
        }

        [Fact]
        public static void EncodeDecode_Sha1()
        {
            EncodeDecode(
                TestData.MsCertificate,
                X509SubjectKeyIdentifierHashAlgorithm.Sha1,
                false,
                "04145971a65a334dda980780ff841ebe87f9723241f2".HexToByteArray(),
                "5971A65A334DDA980780FF841EBE87F9723241F2");
        }

        [Fact]
        public static void EncodeDecode_ShortSha1()
        {
            EncodeDecode(
                TestData.MsCertificate,
                X509SubjectKeyIdentifierHashAlgorithm.ShortSha1,
                false,
                "04084ebe87f9723241f2".HexToByteArray(),
                "4EBE87F9723241F2");
        }

        [Fact]
        public static void EncodeDecode_CapiSha1()
        {
            EncodeDecode(
                TestData.MsCertificate,
                X509SubjectKeyIdentifierHashAlgorithm.CapiSha1,
                false,
                "0414a260a870be1145ed71e2bb5aa19463a4fe9dcc41".HexToByteArray(),
                "A260A870BE1145ED71E2BB5AA19463A4FE9DCC41");
        }

        [Fact]
        public static void DecodeFromBER()
        {
            // Extensions encoded inside PKCS#8 on Windows may use BER encoding that would be invalid DER.
            // Ensure that no exception is thrown and the value is decoded correctly.
            X509SubjectKeyIdentifierExtension ext;
            byte[] rawData = "0481145971a65a334dda980780ff841ebe87f9723241f2".HexToByteArray();
            ext = new X509SubjectKeyIdentifierExtension(new AsnEncodedData(rawData), false);
            string skid = ext.SubjectKeyIdentifier;
            Assert.Equal("5971A65A334DDA980780FF841EBE87F9723241F2", skid);
            Assert.Equal(skid, Convert.ToHexString(ext.SubjectKeyIdentifierBytes.Span));
        }

        [Theory]
        [MemberData(nameof(Rfc7093Examples))]
        public static void EncodeDecode_Rfc7093_Examples(
            byte[] subjectPublicKeyInfo,
            X509SubjectKeyIdentifierHashAlgorithm algorithm,
            string expectedDer,
            string expectedIdentifier)
        {
            EncodeDecodeSubjectPublicKeyInfo(
                subjectPublicKeyInfo,
                algorithm,
                false,
                Convert.FromHexString(expectedDer),
                expectedIdentifier);
        }

        [Theory]
        [MemberData(nameof(Rfc7093Vectors))]
        public static void EncodeDecode_Rfc7093_TestVectors(
            byte[] key,
            byte[] parameters,
            X509SubjectKeyIdentifierHashAlgorithm algorithm,
            string expectedDer,
            string expectedIdentifier)
        {
            EncodeDecodePublicKey(
                new PublicKey(new Oid("1.2.3.4"), new AsnEncodedData(parameters), new AsnEncodedData(key)),
                algorithm,
                false,
                Convert.FromHexString(expectedDer),
                expectedIdentifier);
        }

        [Theory]
        [InlineData(X509SubjectKeyIdentifierHashAlgorithm.ShortSha256)]
        [InlineData(X509SubjectKeyIdentifierHashAlgorithm.ShortSha384)]
        [InlineData(X509SubjectKeyIdentifierHashAlgorithm.ShortSha512)]
        public static void Rfc7093_Truncated_SubjectPublicKeyOnly(X509SubjectKeyIdentifierHashAlgorithm algorithm)
        {
            ReadOnlySpan<byte> ecKey =
            [
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
            ];

            PublicKey nistP256Key = new PublicKey(new Oid(EcPublicKey), new AsnEncodedData(NistP256r1), new AsnEncodedData(ecKey));
            PublicKey brainboolP256Key = new PublicKey(new Oid(EcPublicKey), new AsnEncodedData(BrainpoolP256r1), new AsnEncodedData(ecKey));

            X509SubjectKeyIdentifierExtension nistP256Extension = new(nistP256Key, algorithm, critical: false);
            X509SubjectKeyIdentifierExtension brainpoolP256Extension = new(brainboolP256Key, algorithm, critical: false);

            // Although the PublicKeys have different parameters by their curve, the key material is the same, so the
            // hash should not differ.
            AssertExtensions.SequenceEqual(
                nistP256Extension.SubjectKeyIdentifierBytes.Span,
                brainpoolP256Extension.SubjectKeyIdentifierBytes.Span);
        }

        [Theory]
        [InlineData(X509SubjectKeyIdentifierHashAlgorithm.Sha256)]
        [InlineData(X509SubjectKeyIdentifierHashAlgorithm.Sha384)]
        [InlineData(X509SubjectKeyIdentifierHashAlgorithm.Sha512)]
        public static void Rfc7093_SubjectPublicKeyInfo(X509SubjectKeyIdentifierHashAlgorithm algorithm)
        {
            ReadOnlySpan<byte> ecKey =
            [
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
            ];

            PublicKey nistP256Key = new PublicKey(new Oid(EcPublicKey), new AsnEncodedData(NistP256r1), new AsnEncodedData(ecKey));
            PublicKey brainboolP256Key = new PublicKey(new Oid(EcPublicKey), new AsnEncodedData(BrainpoolP256r1), new AsnEncodedData(ecKey));

            X509SubjectKeyIdentifierExtension nistP256Extension = new(nistP256Key, algorithm, critical: false);
            X509SubjectKeyIdentifierExtension brainpoolP256Extension = new(brainboolP256Key, algorithm, critical: false);

            // Although the PublicKeys have the same key, their parameters are different, thus should produce different
            // hashes.
            AssertExtensions.SequenceNotEqual(
                nistP256Extension.SubjectKeyIdentifierBytes.Span,
                brainpoolP256Extension.SubjectKeyIdentifierBytes.Span);
        }

        public static IEnumerable<object[]> Rfc7093Examples()
        {
            byte[] example =
            [
                0x30, 0x59,
                    0x30, 0x13,
                        0x06, 0x07, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x02, 0x01,
                        0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07,
                    0x03, 0x42, 0x00,
                        0x04,
                        0x7F, 0x7F, 0x35, 0xA7, 0x97, 0x94, 0xC9, 0x50, 0x06, 0x0B, 0x80, 0x29, 0xFC, 0x8F, 0x36, 0x3A,
                        0x28, 0xF1, 0x11, 0x59, 0x69, 0x2D, 0x9D, 0x34, 0xE6, 0xAC, 0x94, 0x81, 0x90, 0x43, 0x47, 0x35,
                        0xF8, 0x33, 0xB1, 0xA6, 0x66, 0x52, 0xDC, 0x51, 0x43, 0x37, 0xAF, 0xF7, 0xF5, 0xC9, 0xC7, 0x5D,
                        0x67, 0x0C, 0x01, 0x9D, 0x95, 0xA5, 0xD6, 0x39, 0xB7, 0x27, 0x44, 0xC6, 0x4A, 0x91, 0x28, 0xBB,
            ];

            // Method 1 example from RFC 7093
            yield return new object[]
            {
                example,
                X509SubjectKeyIdentifierHashAlgorithm.ShortSha256,
                "0414BF37B3E5808FD46D54B28E846311BCCE1CAD2E1A",
                "BF37B3E5808FD46D54B28E846311BCCE1CAD2E1A",
            };

            // Method 4 example from RFC 7093
            yield return new object[]
            {
                example,
                X509SubjectKeyIdentifierHashAlgorithm.Sha256,
                "04206D20896AB8BD833B6B66554BD59B20225D8A75A296088148399D7BF763D57405",
                "6D20896AB8BD833B6B66554BD59B20225D8A75A296088148399D7BF763D57405",
            };
        }

        public static IEnumerable<object[]> Rfc7093Vectors()
        {
            byte[] key = [1, 2, 3, 4];
            byte[] parameters = [4, 4, 5, 6, 7, 8];

            yield return new object[]
            {
                key,
                parameters,
                X509SubjectKeyIdentifierHashAlgorithm.Sha256,
                "04200B710654AEB48CE6A1FF80C0F3E83FAD8DB63B7E1004BE8F3EEC10E95CF3C620",
                "0B710654AEB48CE6A1FF80C0F3E83FAD8DB63B7E1004BE8F3EEC10E95CF3C620",
            };

            yield return new object[]
            {
                key,
                parameters,
                X509SubjectKeyIdentifierHashAlgorithm.Sha384,
                "0430150E43FE7ACE471CDB3910809145AD44B5B7E641A0364D608A1C106C9AD47963BAFE05E431B7782D791DE1B7E25F69DA",
                "150E43FE7ACE471CDB3910809145AD44B5B7E641A0364D608A1C106C9AD47963BAFE05E431B7782D791DE1B7E25F69DA",
            };

            yield return new object[]
            {
                key,
                parameters,
                X509SubjectKeyIdentifierHashAlgorithm.Sha512,
                "0440493CF4FC4B5CF15C17D5FCF4F85CFD1CFBCF29BEA538B8063733922A43693FEECAE70A11BEE932E23C32350C1F624DB16962A6AE6EF4B29BB3BFAD838048006F",
                "493CF4FC4B5CF15C17D5FCF4F85CFD1CFBCF29BEA538B8063733922A43693FEECAE70A11BEE932E23C32350C1F624DB16962A6AE6EF4B29BB3BFAD838048006F",
            };

            yield return new object[]
            {
                key,
                parameters,
                X509SubjectKeyIdentifierHashAlgorithm.ShortSha256,
                "04149F64A747E1B97F131FABB6B447296C9B6F0201E7",
                "9F64A747E1B97F131FABB6B447296C9B6F0201E7",
            };

            yield return new object[]
            {
                key,
                parameters,
                X509SubjectKeyIdentifierHashAlgorithm.ShortSha384,
                "04145A667D62430A8C253EBAE433333904DC6E1D41DC",
                "5A667D62430A8C253EBAE433333904DC6E1D41DC",
            };

            yield return new object[]
            {
                key,
                parameters,
                X509SubjectKeyIdentifierHashAlgorithm.ShortSha512,
                "0414A7C976DB1723ADB41274178DC82E9B777941AB20",
                "A7C976DB1723ADB41274178DC82E9B777941AB20",
            };
        }

        private static void EncodeDecode(
            byte[] certBytes,
            X509SubjectKeyIdentifierHashAlgorithm algorithm,
            bool critical,
            byte[] expectedDer,
            string expectedIdentifier)
        {
            using (var cert = new X509Certificate2(certBytes))
            {
                EncodeDecodePublicKey(cert.PublicKey, algorithm, critical, expectedDer, expectedIdentifier);
            }
        }

        private static void EncodeDecodeSubjectPublicKeyInfo(
            byte[] spkiBytes,
            X509SubjectKeyIdentifierHashAlgorithm algorithm,
            bool critical,
            byte[] expectedDer,
            string expectedIdentifier)
        {
            PublicKey publicKey = PublicKey.CreateFromSubjectPublicKeyInfo(spkiBytes, out _);
            EncodeDecodePublicKey(publicKey, algorithm, critical, expectedDer, expectedIdentifier);
        }


        private static void EncodeDecodePublicKey(
            PublicKey publicKey,
            X509SubjectKeyIdentifierHashAlgorithm algorithm,
            bool critical,
            byte[] expectedDer,
            string expectedIdentifier)
        {
            X509SubjectKeyIdentifierExtension ext = new X509SubjectKeyIdentifierExtension(publicKey, algorithm, critical);

            byte[] rawData = ext.RawData;
            Assert.Equal(expectedDer, rawData);

            ext = new X509SubjectKeyIdentifierExtension(new AsnEncodedData(rawData), critical);
            Assert.Equal(expectedIdentifier, ext.SubjectKeyIdentifier);
            Assert.Equal(expectedIdentifier, Convert.ToHexString(ext.SubjectKeyIdentifierBytes.Span));

            ReadOnlyMemory<byte> ski1 = ext.SubjectKeyIdentifierBytes;
            ReadOnlyMemory<byte> ski2 = ext.SubjectKeyIdentifierBytes;
            Assert.True(ski1.Span == ski2.Span, "Two calls to SubjectKeyIdentifierBytes return the same buffer");
        }
    }
}
