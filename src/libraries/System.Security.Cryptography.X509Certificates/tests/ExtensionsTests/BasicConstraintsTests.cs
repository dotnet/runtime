// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.ExtensionsTests
{
    public static class BasicConstraintsTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            X509BasicConstraintsExtension e = new X509BasicConstraintsExtension();
            string oidValue = e.Oid.Value;
            Assert.Equal("2.5.29.19", oidValue);

            Assert.Empty(e.RawData);

            Assert.False(e.CertificateAuthority);
            Assert.False(e.HasPathLengthConstraint);
            Assert.Equal(0, e.PathLengthConstraint);
        }

        [Theory]
        [MemberData(nameof(BasicConstraintsData))]
        public static void Encode(
            bool certificateAuthority,
            bool hasPathLengthConstraint,
            int pathLengthConstraint,
            bool critical,
            string expectedDerString)
        {
            X509BasicConstraintsExtension ext = new X509BasicConstraintsExtension(
                certificateAuthority,
                hasPathLengthConstraint,
                pathLengthConstraint,
                critical);

            byte[] expectedDer = expectedDerString.HexToByteArray();
            Assert.Equal(expectedDer, ext.RawData);
            Assert.Equal(critical, ext.Critical);

            if (certificateAuthority)
            {
                ext = X509BasicConstraintsExtension.CreateForCertificateAuthority(
                    hasPathLengthConstraint ? pathLengthConstraint : null);

                AssertExtensions.SequenceEqual(expectedDer, ext.RawData);
                Assert.True(ext.Critical, "ext.Critical");
            }
            else if (!hasPathLengthConstraint)
            {
                ext = X509BasicConstraintsExtension.CreateForEndEntity(critical);

                AssertExtensions.SequenceEqual(expectedDer, ext.RawData);
                Assert.Equal(critical, ext.Critical);
            }
        }

        [Theory]
        [MemberData(nameof(BasicConstraintsData))]
        public static void Decode(
            bool certificateAuthority,
            bool hasPathLengthConstraint,
            int pathLengthConstraint,
            bool critical,
            string rawDataString)
        {
            byte[] rawData = rawDataString.HexToByteArray();
            int expectedPathLengthConstraint = hasPathLengthConstraint ? pathLengthConstraint : 0;

            X509BasicConstraintsExtension ext = new X509BasicConstraintsExtension(new AsnEncodedData(rawData), critical);
            Assert.Equal(certificateAuthority, ext.CertificateAuthority);
            Assert.Equal(hasPathLengthConstraint, ext.HasPathLengthConstraint);
            Assert.Equal(expectedPathLengthConstraint, ext.PathLengthConstraint);
        }

        public static object[][] BasicConstraintsData = new object[][]
        {
            new object[] { false, false, 0, false, "3000" },
            new object[] { false, false, 121, false, "3000" },
            new object[] { true, false, 0, false, "30030101ff" },
            new object[] { false, true, 0, false, "3003020100" },
            new object[] { false, true, 7654321, false, "3005020374cbb1" },
            new object[] { true, true, 559, false, "30070101ff0202022f" },
        };

        [Fact]
        public static void DecodeFromBER()
        {
            // Extensions encoded inside PKCS#8 on Windows may use BER encoding that would be invalid DER.
            // Ensure that no exception is thrown and the value is decoded correctly.
            X509BasicConstraintsExtension ext;
            ext = new X509BasicConstraintsExtension(new AsnEncodedData("30800101000201080000".HexToByteArray()), false);
            Assert.False(ext.CertificateAuthority);
            Assert.True(ext.HasPathLengthConstraint);
            Assert.Equal(8, ext.PathLengthConstraint);
        }
    }
}
