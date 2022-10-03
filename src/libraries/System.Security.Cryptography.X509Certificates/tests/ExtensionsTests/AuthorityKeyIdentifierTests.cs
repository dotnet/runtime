// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.ExtensionsTests
{
    public static class AuthorityKeyIdentifierTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            X509AuthorityKeyIdentifierExtension e = new X509AuthorityKeyIdentifierExtension();
            string oidValue = e.Oid.Value;
            Assert.Equal("2.5.29.35", oidValue);

            Assert.Empty(e.RawData);
            Assert.False(e.KeyIdentifier.HasValue, "e.KeyIdentifier.HasValue");
            Assert.Null(e.NamedIssuer);
            Assert.False(e.RawIssuer.HasValue, "e.RawIssuer.HasValue");
            Assert.False(e.SerialNumber.HasValue, "e.SerialNumber.HasValue");
        }

        [Fact]
        public static void RoundtripFull()
        {
            byte[] encoded = (
                "303C80140235857ED35BD13609F22DE8A71F93DFEBD3F495A11AA41830163114" +
                "301206035504030C0B49737375696E6743657274820852E6DEFA1D32A969").HexToByteArray();

            X509AuthorityKeyIdentifierExtension akid = new X509AuthorityKeyIdentifierExtension(encoded, true);
            Assert.True(akid.Critical, "akid.Critical");
            Assert.True(akid.KeyIdentifier.HasValue, "akid.KeyIdentifier.HasValue");

            Assert.Equal(
                "0235857ED35BD13609F22DE8A71F93DFEBD3F495",
                akid.KeyIdentifier.Value.ByteArrayToHex());

            Assert.True(akid.RawIssuer.HasValue, "akid.RawIssuer.HasValue");
            Assert.NotNull(akid.NamedIssuer);

            Assert.Equal(
                "A11AA41830163114301206035504030C0B49737375696E6743657274",
                akid.RawIssuer.Value.ByteArrayToHex());
            Assert.Equal(
                "30163114301206035504030C0B49737375696E6743657274",
                akid.NamedIssuer.RawData.ByteArrayToHex());

            Assert.True(akid.SerialNumber.HasValue, "akid.SerialNumber.HasValue");
            Assert.Equal("52E6DEFA1D32A969", akid.SerialNumber.Value.ByteArrayToHex());

            X509AuthorityKeyIdentifierExtension akid2 = X509AuthorityKeyIdentifierExtension.Create(
                akid.KeyIdentifier.Value.Span,
                akid.NamedIssuer,
                akid.SerialNumber.Value.Span);

            Assert.False(akid2.Critical, "akid2.Critical");
            AssertExtensions.SequenceEqual(akid.RawData, akid2.RawData);
        }

        [Fact]
        public static void CreateEmptyFromCertificate()
        {
            X509AuthorityKeyIdentifierExtension akid;

            using (X509Certificate2 cert = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            {
                akid = X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                    cert,
                    includeKeyIdentifier: false,
                    includeIssuerAndSerial: false);
            }

            Assert.False(akid.Critical, "akid.Critical");
            Assert.Equal("3000", akid.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void CreateKeyIdOnlyFromCertificate()
        {
            X509AuthorityKeyIdentifierExtension akid;

            using (X509Certificate2 cert = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            {
                akid = X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                    cert,
                    includeKeyIdentifier: true,
                    includeIssuerAndSerial: false);
            }

            Assert.False(akid.Critical, "akid.Critical");
            Assert.Equal("30168014B5760C3011CEC792424D4CC75C2CC8A90CE80B64", akid.RawData.ByteArrayToHex());
            Assert.False(akid.RawIssuer.HasValue, "akid.RawIssuer.HasValue");
            Assert.Null(akid.NamedIssuer);
            Assert.False(akid.SerialNumber.HasValue, "akid.SerialNumber.HasValue");
            Assert.True(akid.KeyIdentifier.HasValue, "akid.KeyIdentifier.HasValue");

            Assert.Equal(
                "B5760C3011CEC792424D4CC75C2CC8A90CE80B64",
                akid.KeyIdentifier.GetValueOrDefault().ByteArrayToHex());
        }

        [Fact]
        public static void CreateIssuerAndSerialFromCertificate()
        {
            X509AuthorityKeyIdentifierExtension akid;
            X500DistinguishedName issuerName;
            ReadOnlyMemory<byte> serial;

            using (X509Certificate2 cert = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            {
                issuerName = cert.IssuerName;
                serial = cert.SerialNumberBytes;

                akid = X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                    cert,
                    includeKeyIdentifier: false,
                    includeIssuerAndSerial: true);
            }

            Assert.False(akid.Critical, "akid.Critical");
            Assert.NotNull(akid.NamedIssuer);
            AssertExtensions.SequenceEqual(issuerName.RawData, akid.NamedIssuer.RawData);
            Assert.True(akid.SerialNumber.HasValue, "akid.SerialNumber.HasValue");
            AssertExtensions.SequenceEqual(serial.Span, akid.SerialNumber.GetValueOrDefault().Span);
            Assert.False(akid.KeyIdentifier.HasValue, "akid.KeyIdentifier.HasValue");

            const string ExpectedHex =
                "3072A15EA45C305A310B300906035504061302494531123010060355040A1309" +
                "42616C74696D6F726531133011060355040B130A437962657254727573743122" +
                "30200603550403131942616C74696D6F7265204379626572547275737420526F" +
                "6F7482100F14965F202069994FD5C7AC788941E2";

            Assert.Equal(ExpectedHex, akid.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void CreateFullFromCertificate()
        {
            X509AuthorityKeyIdentifierExtension akid;
            X500DistinguishedName issuerName;
            ReadOnlyMemory<byte> serial;

            using (X509Certificate2 cert = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            {
                issuerName = cert.IssuerName;
                serial = cert.SerialNumberBytes;

                akid = X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                    cert,
                    includeKeyIdentifier: true,
                    includeIssuerAndSerial: true);
            }

            Assert.False(akid.Critical, "akid.Critical");
            Assert.NotNull(akid.NamedIssuer);
            AssertExtensions.SequenceEqual(issuerName.RawData, akid.NamedIssuer.RawData);
            Assert.True(akid.SerialNumber.HasValue, "akid.SerialNumber.HasValue");
            AssertExtensions.SequenceEqual(serial.Span, akid.SerialNumber.GetValueOrDefault().Span);
            Assert.True(akid.KeyIdentifier.HasValue, "akid.KeyIdentifier.HasValue");

            Assert.Equal(
                "B5760C3011CEC792424D4CC75C2CC8A90CE80B64",
                akid.KeyIdentifier.GetValueOrDefault().ByteArrayToHex());

            const string ExpectedHex =
                "3081888014B5760C3011CEC792424D4CC75C2CC8A90CE80B64A15EA45C305A31" +
                "0B300906035504061302494531123010060355040A130942616C74696D6F7265" +
                "31133011060355040B130A437962657254727573743122302006035504031319" +
                "42616C74696D6F7265204379626572547275737420526F6F7482100F14965F20" +
                "2069994FD5C7AC788941E2";

            Assert.Equal(ExpectedHex, akid.RawData.ByteArrayToHex());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void CreateFullWithNegativeSerialNumber(bool fromArray)
        {
            X509AuthorityKeyIdentifierExtension akid;
            ReadOnlySpan<byte> skid = new byte[] { 0x01, 0x02, 0x04 };
            X500DistinguishedName issuerName = new X500DistinguishedName("CN=Negative");
            ReadOnlySpan<byte> serial = new byte[] { 0x80, 0x02 };

            if (fromArray)
            {
                akid = X509AuthorityKeyIdentifierExtension.Create(
                    skid.ToArray(),
                    issuerName,
                    serial.ToArray());
            }
            else
            {
                akid = X509AuthorityKeyIdentifierExtension.Create(skid, issuerName, serial);
            }

            Assert.False(akid.Critical, "akid.Critical");
            Assert.NotNull(akid.NamedIssuer);
            AssertExtensions.SequenceEqual(issuerName.RawData, akid.NamedIssuer.RawData);
            Assert.True(akid.SerialNumber.HasValue, "akid.SerialNumber.HasValue");
            AssertExtensions.SequenceEqual(serial, akid.SerialNumber.GetValueOrDefault().Span);
            Assert.True(akid.KeyIdentifier.HasValue, "akid.KeyIdentifier.HasValue");
            AssertExtensions.SequenceEqual(skid, akid.KeyIdentifier.GetValueOrDefault().Span);

            const string ExpectedHex =
                "30228003010204A117A41530133111300F060355040313084E65676174697665" +
                "82028002";

            Assert.Equal(ExpectedHex, akid.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void CreateFromKeyIdentifierPreservesLeadingZeros()
        {
            ReadOnlySpan<byte> encoded = new byte[]
            {
                // SEQUENCE( [0](
                0x30, 0x16, 0x80, 0x14,
                // keyId
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x01, 0x00,
            };

            ReadOnlySpan<byte> keyId = encoded.Slice(4);

            X509AuthorityKeyIdentifierExtension akid;

            // From ROSpan
            akid = X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(keyId);
            AssertExtensions.SequenceEqual(encoded, akid.RawData);
            AssertExtensions.SequenceEqual(keyId, akid.KeyIdentifier.GetValueOrDefault().Span);

            // From array
            akid = X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(keyId.ToArray());
            AssertExtensions.SequenceEqual(encoded, akid.RawData);
            AssertExtensions.SequenceEqual(keyId, akid.KeyIdentifier.GetValueOrDefault().Span);

            // From SKI
            var skid = new X509SubjectKeyIdentifierExtension(keyId, critical: false);
            akid = X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(skid);
            AssertExtensions.SequenceEqual(encoded, akid.RawData);
            AssertExtensions.SequenceEqual(keyId, akid.KeyIdentifier.GetValueOrDefault().Span);
        }

        [Fact]
        public static void CreateFullPreservesKeyIdLeadingZeros()
        {
            ReadOnlySpan<byte> encoded = new byte[]
            {
                0x30, 0x26, 0x80, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x80, 0xA1, 0x14, 0xA4,
                0x12, 0x30, 0x10, 0x31, 0x0E, 0x30, 0x0C, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x05, 0x48, 0x65,
                0x6C, 0x6C, 0x6F, 0x82, 0x03, 0x00, 0xEE, 0x7B,
            };

            ReadOnlySpan<byte> keyId = encoded.Slice(4, 9);
            X500DistinguishedName issuerName = new X500DistinguishedName("CN=Hello");
            ReadOnlySpan<byte> serial = new byte[] { 0x00, 0xEE, 0x7B };

            X509AuthorityKeyIdentifierExtension akid;

            // From ROSpan
            akid = X509AuthorityKeyIdentifierExtension.Create(keyId, issuerName, serial);
            AssertExtensions.SequenceEqual(encoded, akid.RawData);
            AssertExtensions.SequenceEqual(keyId, akid.KeyIdentifier.GetValueOrDefault().Span);
            AssertExtensions.SequenceEqual(issuerName.RawData, akid.NamedIssuer.RawData);
            AssertExtensions.SequenceEqual(serial, akid.SerialNumber.GetValueOrDefault().Span);

            // From Arrays
            akid = X509AuthorityKeyIdentifierExtension.Create(keyId.ToArray(), issuerName, serial.ToArray());
            AssertExtensions.SequenceEqual(encoded, akid.RawData);
            AssertExtensions.SequenceEqual(keyId, akid.KeyIdentifier.GetValueOrDefault().Span);
            AssertExtensions.SequenceEqual(issuerName.RawData, akid.NamedIssuer.RawData);
            AssertExtensions.SequenceEqual(serial, akid.SerialNumber.GetValueOrDefault().Span);
        }

        [Fact]
        public static void DecodeWithTwoX500Names()
        {
            // This extension has two separate X500 names, one with CN=Hello, the other with CN=Goodbye
            ReadOnlySpan<byte> encoded = new byte[]
            {
                0x30, 0x3C, 0x80, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x80, 0xA1, 0x2A, 0xA4,
                0x12, 0x30, 0x10, 0x31, 0x0E, 0x30, 0x0C, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x05, 0x48, 0x65,
                0x6C, 0x6C, 0x6F, 0xA4, 0x14, 0x30, 0x12, 0x31, 0x10, 0x30, 0x0E, 0x06, 0x03, 0x55, 0x04, 0x03,
                0x13, 0x07, 0x47, 0x6F, 0x6F, 0x64, 0x62, 0x79, 0x65, 0x82, 0x03, 0x00, 0xEE, 0x7B,
            };

            X509AuthorityKeyIdentifierExtension akid = new X509AuthorityKeyIdentifierExtension(encoded);
            Assert.False(akid.Critical, "akid.Critical");
            Assert.Equal("000000000000000880", akid.KeyIdentifier.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.Equal("00EE7B", akid.SerialNumber.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.Null(akid.NamedIssuer);
            AssertExtensions.SequenceEqual(encoded.Slice(13, 44), akid.RawIssuer.GetValueOrDefault().Span);
        }

        [Fact]
        public static void DecodeWithThreeX500Names()
        {
            // This extension has three separate X500 names: CN=Hello; CN=Middle; CN=Goodbye
            ReadOnlySpan<byte> encoded = new byte[]
            {
                0x30, 0x6F, 0x80, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x80, 0xA1, 0x5D, 0xA4,
                0x12, 0x30, 0x10, 0x31, 0x0E, 0x30, 0x0C, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x05, 0x48, 0x65,
                0x6C, 0x6C, 0x6F, 0xA4, 0x13, 0x30, 0x11, 0x31, 0x0F, 0x30, 0x0D, 0x06, 0x03, 0x55, 0x04, 0x03,
                0x13, 0x06, 0x4D, 0x69, 0x64, 0x64, 0x6C, 0x65, 0xA4, 0x14, 0x30, 0x12, 0x31, 0x10, 0x30, 0x0E,
                0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x07, 0x47, 0x6F, 0x6F, 0x64, 0x62, 0x79, 0x65, 0x86, 0x1C,
                0x68, 0x74, 0x74, 0x70, 0x3A, 0x2F, 0x2F, 0x63, 0x65, 0x72, 0x74, 0x2E, 0x65, 0x78, 0x61, 0x6D,
                0x70, 0x6C, 0x65, 0x2F, 0x63, 0x65, 0x72, 0x74, 0x2E, 0x63, 0x72, 0x74, 0x82, 0x03, 0x00, 0xEE,
                0x7B,
            };

            X509AuthorityKeyIdentifierExtension akid = new X509AuthorityKeyIdentifierExtension(encoded);
            Assert.False(akid.Critical, "akid.Critical");
            Assert.Equal("000000000000000880", akid.KeyIdentifier.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.Equal("00EE7B", akid.SerialNumber.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.Null(akid.NamedIssuer);
            AssertExtensions.SequenceEqual(encoded.Slice(13, 95), akid.RawIssuer.GetValueOrDefault().Span);
        }

        [Fact]
        public static void DecodeWithComplexIssuer()
        {
            // This extension value has
            // * A key identifier
            // * A complex authorityCertIssuer
            //   * One X500 name (CN=Hello)
            //   * One URI (http://cert.example/cert.crt)
            // * A serial number
            ReadOnlySpan<byte> encoded = new byte[]
            {
                0x30, 0x44, 0x80, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x80, 0xA1, 0x32, 0xA4,
                0x12, 0x30, 0x10, 0x31, 0x0E, 0x30, 0x0C, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x05, 0x48, 0x65,
                0x6C, 0x6C, 0x6F, 0x86, 0x1C, 0x68, 0x74, 0x74, 0x70, 0x3A, 0x2F, 0x2F, 0x63, 0x65, 0x72, 0x74,
                0x2E, 0x65, 0x78, 0x61, 0x6D, 0x70, 0x6C, 0x65, 0x2F, 0x63, 0x65, 0x72, 0x74, 0x2E, 0x63, 0x72,
                0x74, 0x82, 0x03, 0x00, 0xEE, 0x7B,
            };

            X509AuthorityKeyIdentifierExtension akid = new X509AuthorityKeyIdentifierExtension(encoded, true);
            Assert.True(akid.Critical, "akid.Critical");
            Assert.Equal("000000000000000880", akid.KeyIdentifier.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.Equal("00EE7B", akid.SerialNumber.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.Equal("CN=Hello", akid.NamedIssuer.Name);
            AssertExtensions.SequenceEqual(encoded.Slice(13, 52), akid.RawIssuer.GetValueOrDefault().Span);
        }

        [Fact]
        public static void DecodeWithNoX500Names()
        {
            // This extension has no X500 names, but has a fetch URI (that no one supports)
            ReadOnlySpan<byte> encoded = new byte[]
            {
                0x30, 0x30, 0x80, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x80, 0xA1, 0x1E, 0x86,
                0x1C, 0x68, 0x74, 0x74, 0x70, 0x3A, 0x2F, 0x2F, 0x63, 0x65, 0x72, 0x74, 0x2E, 0x65, 0x78, 0x61,
                0x6D, 0x70, 0x6C, 0x65, 0x2F, 0x63, 0x65, 0x72, 0x74, 0x2E, 0x63, 0x72, 0x74, 0x82, 0x03, 0x00,
                0xEE, 0x7B,
            };

            X509AuthorityKeyIdentifierExtension akid = new X509AuthorityKeyIdentifierExtension(encoded);
            Assert.False(akid.Critical, "akid.Critical");
            Assert.Equal("000000000000000880", akid.KeyIdentifier.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.Equal("00EE7B", akid.SerialNumber.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.Null(akid.NamedIssuer);
            AssertExtensions.SequenceEqual(encoded.Slice(13, 32), akid.RawIssuer.GetValueOrDefault().Span);
        }

        [Fact]
        public static void DecodeWithSerialNoIssuer()
        {
            // This extension has a keyID and serialNumber, but no issuerName.
            // It's implied as invalid by the RFC and X.509, but the structure allows it.
            ReadOnlySpan<byte> encoded = new byte[]
            {
                0x30, 0x10, 0x80, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x80, 0x82, 0x03, 0x00,
                0xEE, 0x7B,
            };

            X509AuthorityKeyIdentifierExtension akid = new X509AuthorityKeyIdentifierExtension(encoded);
            Assert.False(akid.Critical, "akid.Critical");
            Assert.Equal("000000000000000880", akid.KeyIdentifier.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.Equal("00EE7B", akid.SerialNumber.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.Null(akid.NamedIssuer);
            Assert.False(akid.RawIssuer.HasValue, "akid.RawIssuer.HasValue");
        }

        [Fact]
        public static void DecodeWithIssuerNoSerial()
        {
            // This extension has a keyID and an issuerName, but no serialNumber.
            // It's implied as invalid by the RFC and X.509, but the structure allows it.
            ReadOnlySpan<byte> encoded = new byte[]
            {
                0x30, 0x21, 0x80, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x80, 0xA1, 0x14, 0xA4,
                0x12, 0x30, 0x10, 0x31, 0x0E, 0x30, 0x0C, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x05, 0x48, 0x65,
                0x6C, 0x6C, 0x6F,
            };

            X509AuthorityKeyIdentifierExtension akid = new X509AuthorityKeyIdentifierExtension(encoded);
            Assert.False(akid.Critical, "akid.Critical");
            Assert.Equal("000000000000000880", akid.KeyIdentifier.GetValueOrDefault().Span.ByteArrayToHex());
            Assert.False(akid.SerialNumber.HasValue, "akid.SerialNumber.HasValue");
            Assert.Equal("CN=Hello", akid.NamedIssuer.Name);
            AssertExtensions.SequenceEqual(encoded.Slice(13, 22), akid.RawIssuer.GetValueOrDefault().Span);
        }

        [Fact]
        public static void DecodeInvalid()
        {
            byte[] invalidEncoding = { 0x05, 0x00 };

            Assert.Throws<CryptographicException>(
                () => new X509AuthorityKeyIdentifierExtension(invalidEncoding));

            Assert.Throws<CryptographicException>(
                () => new X509AuthorityKeyIdentifierExtension(new ReadOnlySpan<byte>(invalidEncoding)));

            X509Extension untypedExt = new X509Extension("0.1", invalidEncoding, critical: true);
            X509AuthorityKeyIdentifierExtension ext = new X509AuthorityKeyIdentifierExtension();
            ext.CopyFrom(untypedExt);

            Assert.True(ext.Critical, "ext.Critical");
            Assert.Equal("0.1", ext.Oid.Value);
            Assert.Throws<CryptographicException>(() => ext.KeyIdentifier);
            Assert.Throws<CryptographicException>(() => ext.SerialNumber);
            Assert.Throws<CryptographicException>(() => ext.NamedIssuer);
            Assert.Throws<CryptographicException>(() => ext.RawIssuer);
        }

        [Fact]
        public static void CreateFromCertificateWithNoSki()
        {
            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest req = new CertificateRequest("CN=Hi", key, HashAlgorithmName.SHA256);
                DateTimeOffset now = DateTimeOffset.UnixEpoch;

                using (X509Certificate2 cert = req.CreateSelfSigned(now.AddMinutes(-5), now.AddMinutes(5)))
                {
                    Assert.Throws<CryptographicException>(
                        () => X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                            cert,
                            includeKeyIdentifier: true,
                            includeIssuerAndSerial: false));

                    Assert.Throws<CryptographicException>(
                        () => X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                            cert,
                            includeKeyIdentifier: true,
                            includeIssuerAndSerial: true));

                    // Assert.NoThrow
                    X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                        cert,
                        includeKeyIdentifier: false,
                        includeIssuerAndSerial: true);

                    X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                        cert,
                        includeKeyIdentifier: false,
                        includeIssuerAndSerial: false);
                }
            }
        }

        [Fact]
        public static void CreateFromLargeKeyIdentifier()
        {
            byte[] keyId = new byte[128];

            X509AuthorityKeyIdentifierExtension akid =
                X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(keyId);

            AssertExtensions.SequenceEqual(keyId, akid.KeyIdentifier.GetValueOrDefault().Span);

            byte[] rawData = akid.RawData;

            AssertExtensions.SequenceEqual(keyId, rawData.AsSpan(6));
            Assert.Equal("308183808180", rawData.AsSpan(0, 6).ByteArrayToHex());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void CreateIssuerAndSerial(bool fromArray)
        {
            X509AuthorityKeyIdentifierExtension akid;
            X500DistinguishedName issuerName;
            ReadOnlyMemory<byte> serial;

            using (X509Certificate2 cert = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            {
                issuerName = cert.IssuerName;
                serial = cert.SerialNumberBytes;
            }

            if (fromArray)
            {
                akid = X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(
                    issuerName,
                    serial.Span.ToArray());
            }
            else
            {
                akid = X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(
                    issuerName,
                    serial.Span);
            }

            Assert.False(akid.Critical, "akid.Critical");
            Assert.NotNull(akid.NamedIssuer);
            AssertExtensions.SequenceEqual(issuerName.RawData, akid.NamedIssuer.RawData);
            Assert.True(akid.SerialNumber.HasValue, "akid.SerialNumber.HasValue");
            AssertExtensions.SequenceEqual(serial.Span, akid.SerialNumber.GetValueOrDefault().Span);
            Assert.False(akid.KeyIdentifier.HasValue, "akid.KeyIdentifier.HasValue");

            const string ExpectedHex =
                "3072A15EA45C305A310B300906035504061302494531123010060355040A1309" +
                "42616C74696D6F726531133011060355040B130A437962657254727573743122" +
                "30200603550403131942616C74696D6F7265204379626572547275737420526F" +
                "6F7482100F14965F202069994FD5C7AC788941E2";

            Assert.Equal(ExpectedHex, akid.RawData.ByteArrayToHex());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void CreateIssuerAndNegativeSerial(bool fromArray)
        {
            X509AuthorityKeyIdentifierExtension akid;
            X500DistinguishedName issuerName = new X500DistinguishedName("CN=Negative");
            ReadOnlySpan<byte> serial = new byte[] { 0x80, 0x02 };

            if (fromArray)
            {
                akid = X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(
                    issuerName,
                    serial.ToArray());
            }
            else
            {
                akid = X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(
                    issuerName,
                    serial);
            }

            Assert.False(akid.Critical, "akid.Critical");
            Assert.NotNull(akid.NamedIssuer);
            AssertExtensions.SequenceEqual(issuerName.RawData, akid.NamedIssuer.RawData);
            Assert.True(akid.SerialNumber.HasValue, "akid.SerialNumber.HasValue");
            AssertExtensions.SequenceEqual(serial, akid.SerialNumber.GetValueOrDefault().Span);
            Assert.False(akid.KeyIdentifier.HasValue, "akid.KeyIdentifier.HasValue");

            const string ExpectedHex = "301DA117A41530133111300F060355040313084E6567617469766582028002";

            Assert.Equal(ExpectedHex, akid.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void CreateFromSubjectKeyIdentifier_Validation()
        {
            Assert.Throws<ArgumentNullException>(
                "subjectKeyIdentifier",
                () => X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(
                    (X509SubjectKeyIdentifierExtension)null));

            Assert.Throws<ArgumentNullException>(
                "subjectKeyIdentifier",
                () => X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(
                    (byte[])null));
        }

        [Fact]
        public static void CreateFromIssuerAndSerial_Validation()
        {
            Assert.Throws<ArgumentNullException>(
                "issuerName",
                () => X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(
                    null,
                    Array.Empty<byte>()));

            Assert.Throws<ArgumentNullException>(
                "issuerName",
                () => X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(
                    null,
                    ReadOnlySpan<byte>.Empty));

            X500DistinguishedName dn = new X500DistinguishedName("CN=Hi");

            Assert.Throws<ArgumentNullException>(
                "serialNumber",
                () => X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(
                    dn,
                    (byte[])null));
        }

        [Fact]
        public static void Create_Validation()
        {
            Assert.Throws<ArgumentNullException>(
                "keyIdentifier",
                () => X509AuthorityKeyIdentifierExtension.Create(
                    (byte[])null,
                    null,
                    (byte[])null));

            Assert.Throws<ArgumentNullException>(
                "issuerName",
                () => X509AuthorityKeyIdentifierExtension.Create(
                    Array.Empty<byte>(),
                    null,
                    (byte[])null));

            X500DistinguishedName dn = new X500DistinguishedName("CN=Hi");

            Assert.Throws<ArgumentNullException>(
                "serialNumber",
                () => X509AuthorityKeyIdentifierExtension.Create(
                    Array.Empty<byte>(),
                    dn,
                    (byte[])null));

            Assert.Throws<ArgumentNullException>(
                "issuerName",
                () => X509AuthorityKeyIdentifierExtension.Create(
                    ReadOnlySpan<byte>.Empty, 
                    null,
                    ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void CreateFromCertificate_Validation()
        {
            Assert.Throws<ArgumentNullException>(
                "certificate",
                () => X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                    null,
                    false,
                    false));

            Assert.Throws<ArgumentNullException>(
                "certificate",
                () => X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                    null,
                    false,
                    true));

            Assert.Throws<ArgumentNullException>(
                "certificate",
                () => X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                    null,
                    true,
                    false));

            Assert.Throws<ArgumentNullException>(
                "certificate",
                () => X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                    null,
                    true,
                    true));
        }

        [Fact]
        public static void CopyFrom_ReadKeyIdentifierFirst()
        {
            X509AuthorityKeyIdentifierExtension ext = new X509AuthorityKeyIdentifierExtension();
            ext.CopyFrom(GetFullyValuedExtension());

            ReadOnlyMemory<byte>? keyIdentifier = ext.KeyIdentifier;
            Assert.Equal("000000000000000880", keyIdentifier.GetValueOrDefault().ByteArrayToHex());
        }

        [Fact]
        public static void CopyFrom_ReadRawIssuerFirst()
        {
            X509AuthorityKeyIdentifierExtension ext = new X509AuthorityKeyIdentifierExtension();
            ext.CopyFrom(GetFullyValuedExtension());

            ReadOnlyMemory<byte>? rawIssuer = ext.RawIssuer;

            Assert.Equal(
                "A114A4123010310E300C0603550403130548656C6C6F",
                rawIssuer.GetValueOrDefault().ByteArrayToHex());
        }

        [Fact]
        public static void CopyFrom_ReadNamedIssuerFirst()
        {
            X509AuthorityKeyIdentifierExtension ext = new X509AuthorityKeyIdentifierExtension();
            ext.CopyFrom(GetFullyValuedExtension());

            X500DistinguishedName namedIssuer = ext.NamedIssuer;
            Assert.NotNull(namedIssuer);
            Assert.Equal("CN=Hello", namedIssuer.Name);
        }

        [Fact]
        public static void CopyFrom_ReadSerialNumberFirst()
        {
            X509AuthorityKeyIdentifierExtension ext = new X509AuthorityKeyIdentifierExtension();
            ext.CopyFrom(GetFullyValuedExtension());

            ReadOnlyMemory<byte>? serial = ext.SerialNumber;
            Assert.Equal("00EE7B", serial.GetValueOrDefault().ByteArrayToHex());
        }

        [Fact]
        public static void CreateWithInvalidSerialNumber()
        {
            // This value has 9 leading zero bits, making it an invalid encoding for a BER/DER INTEGER.
            byte[] tooManyZeros = { 0x00, 0x7F };
            byte[] invalidValue = tooManyZeros;
            
            X500DistinguishedName dn = new X500DistinguishedName("CN=Bad Serial");

            // Array
            Assert.Throws<ArgumentException>(
                "serialNumber",
                () => X509AuthorityKeyIdentifierExtension.Create(invalidValue, dn, invalidValue));

            // Span
            Assert.Throws<ArgumentException>(
                "serialNumber",
                () => X509AuthorityKeyIdentifierExtension.Create(
                    new ReadOnlySpan<byte>(invalidValue), dn, new ReadOnlySpan<byte>(invalidValue)));

            // Array
            Assert.Throws<ArgumentException>(
                "serialNumber",
                () => X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(dn, invalidValue));

            // Span
            Assert.Throws<ArgumentException>(
                "serialNumber",
                () => X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(
                    dn, new ReadOnlySpan<byte>(invalidValue)));

            // The leading 9 bits are all one, also invalid.
            byte[] tooManyOnes = { 0xFF, 0x80 };
            invalidValue = tooManyOnes;

            // Array
            Assert.Throws<ArgumentException>(
                "serialNumber",
                () => X509AuthorityKeyIdentifierExtension.Create(invalidValue, dn, invalidValue));

            // Span
            Assert.Throws<ArgumentException>(
                "serialNumber",
                () => X509AuthorityKeyIdentifierExtension.Create(
                    new ReadOnlySpan<byte>(invalidValue), dn, new ReadOnlySpan<byte>(invalidValue)));

            // Array
            Assert.Throws<ArgumentException>(
                "serialNumber",
                () => X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(dn, invalidValue));

            // Span
            Assert.Throws<ArgumentException>(
                "serialNumber",
                () => X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(
                    dn, new ReadOnlySpan<byte>(invalidValue)));
        }

        private static X509Extension GetFullyValuedExtension()
        {
            ReadOnlySpan<byte> encoded = new byte[]
            {
                0x30, 0x26, 0x80, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x80, 0xA1, 0x14, 0xA4,
                0x12, 0x30, 0x10, 0x31, 0x0E, 0x30, 0x0C, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x05, 0x48, 0x65,
                0x6C, 0x6C, 0x6F, 0x82, 0x03, 0x00, 0xEE, 0x7B,
            };

            return new X509Extension("2.5.29.35", encoded, critical: false);
        }
    }
}
