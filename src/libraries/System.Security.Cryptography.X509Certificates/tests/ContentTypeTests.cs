// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class ContentTypeTests
    {
        [Theory]
        [InlineData("My.pfx", X509ContentType.Pkcs12)]
        [InlineData("My.cer", X509ContentType.Cert)]
        public static void TestFileContentType(string fileName, X509ContentType contentType)
        {
            string fullPath = Path.Combine("TestData", fileName);
            X509ContentType fileType = X509Certificate2.GetCertContentType(fullPath);
            Assert.Equal(contentType, fileType);
        }

        [Theory]
        [MemberData(nameof(GetContentBlobsWithType))]
        public static void TestBlobContentType(byte[] blob, X509ContentType contentType)
        {
            X509ContentType blobType = X509Certificate2.GetCertContentType(blob);
            Assert.Equal(contentType, blobType);
        }

        public static IEnumerable<object[]> GetContentBlobsWithType()
        {
            return new[]
            {
                new object[] { TestData.MsCertificate, X509ContentType.Cert }, 
                new object[] { TestData.MsCertificatePemBytes, X509ContentType.Cert },
                new object[] { TestData.Pkcs7ChainDerBytes, X509ContentType.Pkcs7 },
                new object[] { TestData.Pkcs7ChainPemBytes, X509ContentType.Pkcs7 },
                new object[] { TestData.Pkcs7EmptyDerBytes, X509ContentType.Pkcs7 },
                new object[] { TestData.Pkcs7EmptyPemBytes, X509ContentType.Pkcs7 },
                new object[] { TestData.Pkcs7SingleDerBytes, X509ContentType.Pkcs7 },
                new object[] { TestData.Pkcs7SinglePemBytes, X509ContentType.Pkcs7 },
                new object[] { TestData.PfxData, X509ContentType.Pkcs12 },
                new object[] { TestData.EmptyPfx, X509ContentType.Pkcs12 },
                new object[] { TestData.MultiPrivateKeyPfx, X509ContentType.Pkcs12 },
                new object[] { TestData.ChainPfxBytes, X509ContentType.Pkcs12 },
            };
        }
    }
}
