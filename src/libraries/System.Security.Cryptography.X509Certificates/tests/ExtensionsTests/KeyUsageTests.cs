// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.ExtensionsTests
{
    public static class KeyUsageTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            X509KeyUsageExtension e = new X509KeyUsageExtension();
            string oidValue = e.Oid.Value;
            Assert.Equal("2.5.29.15", oidValue);
            Assert.Empty(e.RawData);
            X509KeyUsageFlags keyUsages = e.KeyUsages;
            Assert.Equal(X509KeyUsageFlags.None, keyUsages);
        }

        [Fact]
        public static void EncodeDecode_CrlSign()
        {
            EncodeDecode(X509KeyUsageFlags.CrlSign, false, "03020102".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_DataEncipherment()
        {
            EncodeDecode(X509KeyUsageFlags.DataEncipherment, false, "03020410".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_DecipherOnly()
        {
            EncodeDecode(X509KeyUsageFlags.DecipherOnly, false, "0303070080".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_DigitalSignature()
        {
            EncodeDecode(X509KeyUsageFlags.DigitalSignature, false, "03020780".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_EncipherOnly()
        {
            EncodeDecode(X509KeyUsageFlags.EncipherOnly, false, "03020001".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_KeyAgreement()
        {
            EncodeDecode(X509KeyUsageFlags.KeyAgreement, false, "03020308".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_KeyCertSign()
        {
            EncodeDecode(X509KeyUsageFlags.KeyCertSign, false, "03020204".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_KeyEncipherment()
        {
            EncodeDecode(X509KeyUsageFlags.KeyEncipherment, false, "03020520".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_None()
        {
            EncodeDecode(X509KeyUsageFlags.None, false, "030100".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_NonRepudiation()
        {
            EncodeDecode(X509KeyUsageFlags.NonRepudiation, false, "03020640".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_KeyAgreementAndDecipherOnly()
        {
            EncodeDecode(
                X509KeyUsageFlags.KeyAgreement | X509KeyUsageFlags.DecipherOnly,
                false,
                "0303070880".HexToByteArray());
        }

        [Fact]
        public static void DecodeFromBER()
        {
            // Extensions encoded inside PKCS#8 on Windows may use BER encoding that would be invalid DER.
            // Ensure that no exception is thrown and the value is decoded correctly.
            X509KeyUsageExtension ext;
            ext = new X509KeyUsageExtension(new AsnEncodedData("230403020080".HexToByteArray()), false);
            Assert.Equal(X509KeyUsageFlags.DigitalSignature, ext.KeyUsages);
            ext = new X509KeyUsageExtension(new AsnEncodedData("038200020080".HexToByteArray()), false);
            Assert.Equal(X509KeyUsageFlags.DigitalSignature, ext.KeyUsages);
        }

        [Fact]
        public static void DecodeEmptyArray()
        {
            X509KeyUsageExtension keyUsageExtension =
                new X509KeyUsageExtension(new AsnEncodedData(Array.Empty<byte>()), false);

            Assert.ThrowsAny<CryptographicException>(() => keyUsageExtension.KeyUsages);
        }

        private static void EncodeDecode(X509KeyUsageFlags flags, bool critical, byte[] expectedDer)
        {
            X509KeyUsageExtension ext = new X509KeyUsageExtension(flags, critical);
            byte[] rawData = ext.RawData;
            Assert.Equal(expectedDer, rawData);

            // Assert that format doesn't crash
            string s = ext.Format(false);

            // Rebuild it from the RawData.
            ext = new X509KeyUsageExtension(new AsnEncodedData(rawData), critical);
            Assert.Equal(flags, ext.KeyUsages);
        }
    }
}
