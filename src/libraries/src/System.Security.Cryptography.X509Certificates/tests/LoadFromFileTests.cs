// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            byte[] expectedSerial = "b00000000100dd9f3bd08b0aaf11b000000033".HexToByteArray();

            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                byte[] serial = c.GetSerialNumber();
                Assert.Equal(expectedSerial, serial);
            }
        }

        [Fact]
        public static void TestThumbprint()
        {
            byte[] expectedThumbPrint = "108e2ba23632620c427c570b6d9db51ac31387fe".HexToByteArray();

            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                byte[] thumbPrint = c.GetCertHash();
                Assert.Equal(expectedThumbPrint, thumbPrint);
            }
        }

        [Fact]
        public static void TestGetFormat()
        {
            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                string format = c.GetFormat();
                Assert.Equal("X509", format);  // Only one format is supported so this is very predicatable api...
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
            byte[] expectedPublicKey = (
                "3082010a0282010100e8af5ca2200df8287cbc057b7fadeeeb76ac28533f3adb" +
                "407db38e33e6573fa551153454a5cfb48ba93fa837e12d50ed35164eef4d7adb" +
                "137688b02cf0595ca9ebe1d72975e41b85279bf3f82d9e41362b0b40fbbe3bba" +
                "b95c759316524bca33c537b0f3eb7ea8f541155c08651d2137f02cba220b10b1" +
                "109d772285847c4fb91b90b0f5a3fe8bf40c9a4ea0f5c90a21e2aae3013647fd" +
                "2f826a8103f5a935dc94579dfb4bd40e82db388f12fee3d67a748864e162c425" +
                "2e2aae9d181f0e1eb6c2af24b40e50bcde1c935c49a679b5b6dbcef9707b2801" +
                "84b82a29cfbfa90505e1e00f714dfdad5c238329ebc7c54ac8e82784d37ec643" +
                "0b950005b14f6571c50203010001").HexToByteArray();

            using (X509Certificate2 c = LoadCertificateFromFile())
            {
                byte[] publicKey = c.GetPublicKey();
                Assert.Equal(expectedPublicKey, publicKey);
            }
        }

        [Fact]
        [ActiveIssue(2910, PlatformID.AnyUnix)]
        [ActiveIssue(2667, PlatformID.Windows)]
        public static void TestLoadSignedFile()
        {
            // X509Certificate2 can also extract the certificate from a signed file.

            string path = Path.Combine("TestData", "Windows6.1-KB3004361-x64.msu");
            if (!File.Exists(path))
                throw new Exception(string.Format("Test infrastructure failure: Expected to find file \"{0}\".", path));

            using (X509Certificate2 c = new X509Certificate2(path))
            {
                string issuer = c.Issuer;
                Assert.Equal(
                    "CN=Microsoft Code Signing PCA, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    issuer);

                string subject = c.Subject;
                Assert.Equal(
                    "CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    subject);

                byte[] expectedThumbprint = "67b1757863e3eff760ea9ebb02849af07d3a8080".HexToByteArray();
                byte[] actualThumbprint = c.GetCertHash();
                Assert.Equal(expectedThumbprint, actualThumbprint);
            }
        }

        private static X509Certificate2 LoadCertificateFromFile()
        {
            string path = Path.Combine("TestData", "MS.cer");
            if (!File.Exists(path))
                throw new Exception(string.Format("Test infrastructure failure: Expected to find file \"{0}\".", path));
            byte[] data = File.ReadAllBytes(path);
            Assert.Equal(TestData.MsCertificate, data);

            return new X509Certificate2(path);
        }
    }
}
