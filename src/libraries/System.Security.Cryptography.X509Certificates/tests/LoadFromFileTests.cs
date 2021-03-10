// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class LoadFromFileTests
    {
        [Fact]
        public static void TestIssuer()
        {
            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                string issuer = c.Issuer;

                Assert.Equal(
                    "CN=Microsoft Code Signing PCA, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    issuer);
            }
        }

        [Fact]
        public static void TestSubject()
        {
            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                string subject = c.Subject;

                Assert.Equal(
                    "CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    subject);
            }
        }

        [Fact]
        public static void TestSerial()
        {
            string expectedSerialHex = "33000000B011AF0A8BD03B9FDD0001000000B0";
            byte[] expectedSerial = "B00000000100DD9F3BD08B0AAF11B000000033".HexToByteArray();

            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                byte[] serial = c.GetSerialNumber();
                Assert.Equal(expectedSerial, serial);
                string serialHex = c.GetSerialNumberString();
                Assert.Equal(expectedSerialHex, serialHex);
                serialHex = c.SerialNumber;
                Assert.Equal(expectedSerialHex, serialHex);
            }
        }

        [Fact]
        public static void TestThumbprint()
        {
            string expectedThumbPrintHex = "108E2BA23632620C427C570B6D9DB51AC31387FE";
            byte[] expectedThumbPrint = expectedThumbPrintHex.HexToByteArray();

            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                byte[] thumbPrint = c.GetCertHash();
                Assert.Equal(expectedThumbPrint, thumbPrint);
                string thumbPrintHex = c.GetCertHashString();
                Assert.Equal(expectedThumbPrintHex, thumbPrintHex);
            }
        }

#if HAVE_THUMBPRINT_OVERLOADS
        [Theory]
        [InlineData("SHA1", false)]
        [InlineData("SHA1", true)]
        [InlineData("SHA256", false)]
        [InlineData("SHA256", true)]
        [InlineData("SHA384", false)]
        [InlineData("SHA384", true)]
        [InlineData("SHA512", false)]
        [InlineData("SHA512", true)]
        public static void TestThumbprint_ByHashAlgorithm(string hashAlgName, bool viaSpan)
        {
            string expectedThumbprintHex;

            switch (hashAlgName)
            {
                case "SHA1":
                    expectedThumbprintHex =
                        "108E2BA23632620C427C570B6D9DB51AC31387FE";
                    break;
                case "SHA256":
                    expectedThumbprintHex =
                        "73FCF982974387FB164C91D0168FE8C3B957DE6526AE239AAD32825C5A63D2A4";
                    break;
                case "SHA384":
                    expectedThumbprintHex =
                        "E6DCEF0840DAB43E1DBE9BE23142182BD05106AB25F7043BDE6A551928DFB4C7082791B86A5FB5E77B0F43DD92B7A3E5";
                    break;
                case "SHA512":
                    expectedThumbprintHex =
                        "8435635A12915A1A9C28BC2BCE7C3CAD08EB723FE276F13CD37D1C3B21416994" +
                        "0661A27B419882DBA643B23A557CA9EBC03ACC3D7EE3D4D591AB4BA0E553B945";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hashAlgName));
            }

            HashAlgorithmName alg = new HashAlgorithmName(hashAlgName);

            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                if (viaSpan)
                {
                    const int WriteOffset = 3;
                    const byte FillByte = 0x55;

                    int expectedSize = expectedThumbprintHex.Length / 2;
                    byte[] thumbPrint = new byte[expectedSize + 10];
                    thumbPrint.AsSpan().Fill(FillByte);

                    Span<byte> writeDest = thumbPrint.AsSpan(WriteOffset);
                    int bytesWritten;

                    // Too small.
                    Assert.False(c.TryGetCertHash(alg, writeDest.Slice(0, expectedSize - 1), out bytesWritten));
                    Assert.Equal(0, bytesWritten);
                    // Still all 0x55s.
                    Assert.Equal(new string('5', thumbPrint.Length * 2), thumbPrint.ByteArrayToHex());

                    // Large enough (+7)
                    Assert.True(c.TryGetCertHash(alg, writeDest, out bytesWritten));
                    Assert.Equal(expectedSize, bytesWritten);

                    Assert.Equal(expectedThumbprintHex, writeDest.Slice(0, bytesWritten).ByteArrayToHex());
                    Assert.Equal(FillByte, thumbPrint[expectedSize + WriteOffset]);

                    // Try again with a perfectly sized value
                    thumbPrint.AsSpan().Fill(FillByte);
                    Assert.True(c.TryGetCertHash(alg, writeDest.Slice(0, expectedSize), out bytesWritten));
                    Assert.Equal(expectedSize, bytesWritten);

                    Assert.Equal(expectedThumbprintHex, writeDest.Slice(0, bytesWritten).ByteArrayToHex());
                    Assert.Equal(FillByte, thumbPrint[expectedSize + WriteOffset]);
                }
                else
                {
                    byte[] thumbPrint = c.GetCertHash(alg);
                    Assert.Equal(expectedThumbprintHex, thumbPrint.ByteArrayToHex());
                    string thumbPrintHex = c.GetCertHashString(alg);
                    Assert.Equal(expectedThumbprintHex, thumbPrintHex);
                }
            }
        }
#endif

        [Fact]
        public static void TestGetFormat()
        {
            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                string format = c.GetFormat();
                Assert.Equal("X509", format);  // Only one format is supported so this is very predictable api...
            }
        }

        [Fact]
        public static void TestGetKeyAlgorithm()
        {
            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                string keyAlgorithm = c.GetKeyAlgorithm();
                Assert.Equal("1.2.840.113549.1.1.1", keyAlgorithm);
            }
        }

        [Fact]
        public static void TestGetKeyAlgorithmParameters()
        {
            string expected = "0500";

            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                byte[] keyAlgorithmParameters = c.GetKeyAlgorithmParameters();
                Assert.Equal(expected.HexToByteArray(), keyAlgorithmParameters);
                string keyAlgorithmParametersString = c.GetKeyAlgorithmParametersString();
                Assert.Equal(expected, keyAlgorithmParametersString);
            }
        }

        [Fact]
        public static void TestGetPublicKey()
        {
            string expectedPublicKeyHex =
                "3082010a0282010100e8af5ca2200df8287cbc057b7fadeeeb76ac28533f3adb" +
                "407db38e33e6573fa551153454a5cfb48ba93fa837e12d50ed35164eef4d7adb" +
                "137688b02cf0595ca9ebe1d72975e41b85279bf3f82d9e41362b0b40fbbe3bba" +
                "b95c759316524bca33c537b0f3eb7ea8f541155c08651d2137f02cba220b10b1" +
                "109d772285847c4fb91b90b0f5a3fe8bf40c9a4ea0f5c90a21e2aae3013647fd" +
                "2f826a8103f5a935dc94579dfb4bd40e82db388f12fee3d67a748864e162c425" +
                "2e2aae9d181f0e1eb6c2af24b40e50bcde1c935c49a679b5b6dbcef9707b2801" +
                "84b82a29cfbfa90505e1e00f714dfdad5c238329ebc7c54ac8e82784d37ec643" +
                "0b950005b14f6571c50203010001";
            byte[] expectedPublicKey = expectedPublicKeyHex.HexToByteArray();

            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                byte[] publicKey = c.GetPublicKey();
                Assert.Equal(expectedPublicKey, publicKey);
                string publicKeyHex = c.GetPublicKeyString();
                Assert.Equal(expectedPublicKeyHex, publicKeyHex, true);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/15073", TestPlatforms.AnyUnix)]
        public static void TestLoadSignedFile()
        {
            // X509Certificate2 can also extract the certificate from a signed file.

            string path = TestFiles.SignedMsuFile;
            if (!File.Exists(path))
                throw new Exception(string.Format("Test infrastructure failure: Expected to find file \"{0}\".", path));

            using (X509Certificate2 c = new X509Certificate2(path))
            {
                string issuer = c.Issuer;
                Assert.Equal(
                    "CN=Microsoft Code Signing PCA, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    issuer);
#pragma warning disable 0618
                Assert.Equal(
                    "C=US, S=Washington, L=Redmond, O=Microsoft Corporation, CN=Microsoft Code Signing PCA",
                    c.GetIssuerName());
#pragma warning restore 0618

                string subject = c.Subject;
                Assert.Equal(
                    "CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    subject);
#pragma warning disable 0618
                Assert.Equal(
                    "C=US, S=Washington, L=Redmond, O=Microsoft Corporation, OU=MOPR, CN=Microsoft Corporation",
                    c.GetName());
#pragma warning restore 0618

                string expectedThumbprintHash = "67B1757863E3EFF760EA9EBB02849AF07D3A8080";
                byte[] expectedThumbprint = expectedThumbprintHash.HexToByteArray();
                byte[] actualThumbprint = c.GetCertHash();
                Assert.Equal(expectedThumbprint, actualThumbprint);
                string actualThumbprintHash = c.GetCertHashString();
                Assert.Equal(expectedThumbprintHash, actualThumbprintHash);
            }
        }

        [Fact]
        public static void TestLoadConcatenatedPemFile()
        {
            using (X509Certificate2 c = new X509Certificate2(TestData.ConcatenatedPemFile))
            {
                string firstCertifiateThumbprint = "3CFD4BEECFB3F8C4DC71AD9E46EC81C2CCE71CE6";
                Assert.Equal(firstCertifiateThumbprint, c.GetCertHashString());
            }
        }

        private static X509Certificate2 LoadCertificateFromFile()
        {
            string path = TestFiles.MsCertificateDerFile;
            if (!File.Exists(path))
                throw new Exception(string.Format("Test infrastructure failure: Expected to find file \"{0}\".", path));
            byte[] data = File.ReadAllBytes(path);
            Assert.Equal(TestData.MsCertificate, data);

            return new X509Certificate2(path);
        }
    }
}
