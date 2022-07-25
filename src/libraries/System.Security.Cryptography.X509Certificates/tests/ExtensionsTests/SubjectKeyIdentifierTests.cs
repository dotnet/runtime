// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.ExtensionsTests
{
    public static class SubjectKeyIdentifierTests
    {
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

        private static void EncodeDecode(
            byte[] certBytes,
            X509SubjectKeyIdentifierHashAlgorithm algorithm,
            bool critical,
            byte[] expectedDer,
            string expectedIdentifier)
        {
            PublicKey pk;

            using (var cert = new X509Certificate2(certBytes))
            {
                pk = cert.PublicKey;
            }

            X509SubjectKeyIdentifierExtension ext =
                new X509SubjectKeyIdentifierExtension(pk, algorithm, critical);

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
