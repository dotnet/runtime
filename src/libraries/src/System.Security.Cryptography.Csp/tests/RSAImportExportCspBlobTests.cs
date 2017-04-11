// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSAImportExportCspBlobTests
    {
        [Fact]
        public static void ExportImportPublicOnly()
        {
            byte[] expectedExport = ByteUtils.HexToByteArray(
                "0602000000a40000525341310004000001000100e19a01644b82962a224781d1f60c2cc373b"
                + "798df541343f63c638f45fa96e11049c8d9e88bd56483ec3c2d56e9460d2b1140191841761c1523840221b0e"
                + "b6401dc4d09c54bf75cea25d9e191572fb2ec92c3559b35b3ef3fa695171bb1fddeb469792e49f0d17c769d0"
                + "a37f6a4a6584af39878eb21f9ba9eae8be9c39eac6ae0");

            byte[] exported;

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(TestData.CspTestKey);

                exported = rsa.ExportCspBlob(includePrivateParameters: false);
            }

            Assert.Equal(expectedExport, exported);

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(exported);

                byte[] exported2 = rsa.ExportCspBlob(includePrivateParameters: false);

                Assert.Equal(exported, exported2);

                Assert.ThrowsAny<CryptographicException>(() => rsa.ExportCspBlob(includePrivateParameters: true));
            }
        }

        [Fact]
        public static void ExportImportPublicPrivate()
        {
            // This blob contains the private key of TestData.CspTestKey. The guidelines for the TestData class
            // as to key security/sanity apply to this blob as well.
            byte[] expectedExport = ByteUtils.HexToByteArray(
                "0702000000a40000525341320004000001000100e19a01644b82962a224781d1f60c2cc373b"
                + "798df541343f63c638f45fa96e11049c8d9e88bd56483ec3c2d56e9460d2b1140191841761c1523840221b0e"
                + "b6401dc4d09c54bf75cea25d9e191572fb2ec92c3559b35b3ef3fa695171bb1fddeb469792e49f0d17c769d0"
                + "a37f6a4a6584af39878eb21f9ba9eae8be9c39eac6ae07bb43a9f6e29e584b47303f8ac70384ba4f1a4b7d77"
                + "fb4c931c2a194584b9d6060d39ba798e20698221ac615b083bbdaf2b6f39c05c570276945728800b1aae1531"
                + "511b5878dae8820a178f8cc3cca5426ce761ef3247bce9375318a03c3d5779ed339d2f9d04d6265d0a99057c"
                + "c1af86b656541f4f6b062d8407968aaf794fee33273c0fd4735d688e0e8161f5c9f360c2fc1caed9a2b48a53"
                + "3ea4d26b9ac50a0e7e7ca94c6bd6edfd3fe448650b66fa99c57b50e3737fae9d26300fee06649472a664190e"
                + "a603126718f896bbfe0671401f31414678d173d32c486c8fbb6334fe90c77f7c2a04ee9c3e3ab85d948357f7"
                + "15e5d706031e013f0951eeb1e506c5af71cfec07bbc637d5b7c788fdad21ec5f250ef069d00a5c9bb6e2fe06"
                + "01b91f36121885011cd7186093ee25c2a5dd6b3cfea3d8b1627148ab0a47610b8d99743ac008b62f8a054c18"
                + "4b8b9f862beebc70af40408999bead5a09baec588375be03cfa636b018d7d9948f1abae4d5463c5c5d210a0b"
                + "42589a90a2bc01b1bb027f6c859de82ace0c60237d96574a1752e38b56326c7eae33cf7590da6728ff1de184"
                + "c654fccba0866732e576747107cef935d43aa5f477178aafee834a53a3d14");

            byte[] exported;

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(TestData.CspTestKey);

                exported = rsa.ExportCspBlob(includePrivateParameters: true);
            }

            Assert.Equal(expectedExport, exported);

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(exported);
                byte[] exported2 = rsa.ExportCspBlob(includePrivateParameters: true);
                Assert.Equal<byte>(exported, exported2);
            }
        }

        [Fact]
        public static void RSAParametersToBlob_PublicOnly()
        {
            byte[] blob;

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(TestData.RSA1024Params);
                blob = rsa.ExportCspBlob(false);
            }

            RSAParameters exported;

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(blob);

                Assert.True(rsa.PublicOnly);

                exported = rsa.ExportParameters(false);
            }

            RSAParameters expected = new RSAParameters
            {
                Modulus = TestData.RSA1024Params.Modulus,
                Exponent = TestData.RSA1024Params.Exponent,
            };

            ImportExport.AssertKeyEquals(ref expected, ref exported);
        }

        [Fact]
        public static void RSAParametersToBlob_PublicPrivate()
        {
            byte[] blob;

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(TestData.RSA1024Params);
                blob = rsa.ExportCspBlob(true);
            }

            RSAParameters exported;

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(blob);

                Assert.False(rsa.PublicOnly);

                exported = rsa.ExportParameters(true);
            }

            RSAParameters expected = TestData.RSA1024Params;

            ImportExport.AssertKeyEquals(ref expected, ref exported);
        }
    }
}
