// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.ExtensionsTests
{
    public static class EnhancedKeyUsageTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            X509EnhancedKeyUsageExtension e = new X509EnhancedKeyUsageExtension();
            string oidValue = e.Oid.Value;
            Assert.Equal("2.5.29.37", oidValue);

            Assert.Empty(e.RawData);

            OidCollection usages = e.EnhancedKeyUsages;
            Assert.Equal(0, usages.Count);
        }

        [Fact]
        public static void EncodeDecode_Empty()
        {
            OidCollection usages = new OidCollection();
            EncodeDecode(usages, false, "3000".HexToByteArray());
        }

        [Fact]
        public static void EncodeDecode_2Oids()
        {
            Oid oid1 = new Oid("1.3.6.1.5.5.7.3.1");
            Oid oid2 = new Oid("1.3.6.1.4.1.311.10.3.1");
            OidCollection usages = new OidCollection();
            usages.Add(oid1);
            usages.Add(oid2);

            EncodeDecode(usages, false, "301606082b06010505070301060a2b0601040182370a0301".HexToByteArray());
        }

        [Theory]
        [InlineData("1")]
        [InlineData("3.0")]
        [InlineData("Invalid Value")]
        public static void Encode_InvalidOid(string invalidOidValue)
        {
            OidCollection oids = new OidCollection
            {
                new Oid(invalidOidValue)
            };

            Assert.ThrowsAny<CryptographicException>(() => new X509EnhancedKeyUsageExtension(oids, false));
        }

        [Fact]
        public static void CollectionPropertyIsolation()
        {
            Oid oid1 = new Oid("1.3.6.1.5.5.7.3.1");
            OidCollection usages = new OidCollection();
            X509EnhancedKeyUsageExtension e = new X509EnhancedKeyUsageExtension(usages, false);
            Assert.Equal(0, e.EnhancedKeyUsages.Count);
            usages.Add(oid1);
            Assert.Equal(0, e.EnhancedKeyUsages.Count);
            e.EnhancedKeyUsages.Add(oid1);
            Assert.Equal(0, e.EnhancedKeyUsages.Count);
            Assert.NotSame(e.EnhancedKeyUsages, e.EnhancedKeyUsages);
        }
        
        private static void EncodeDecode(
            OidCollection usages,
            bool critical,
            byte[] expectedDer)
        {
            X509EnhancedKeyUsageExtension ext = new X509EnhancedKeyUsageExtension(usages, critical);
            byte[] rawData = ext.RawData;
            Assert.Equal(expectedDer, rawData);

            ext = new X509EnhancedKeyUsageExtension(new AsnEncodedData(rawData), critical);
            OidCollection actualUsages = ext.EnhancedKeyUsages;

            Assert.Equal(usages.Count, actualUsages.Count);

            for (int i = 0; i < usages.Count; i++)
            {
                Assert.Equal(usages[i].Value, actualUsages[i].Value);
            }
        }
    }
}
