// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class CtorTests
    {
        [Fact]
        public static void TestDefaultConstructor()
        {
            using (X509Certificate2 c = new X509Certificate2())
            {
                IntPtr h = c.Handle;
                object ignored;
                Assert.Equal(IntPtr.Zero, h);
                Assert.Throws<CryptographicException>(() => c.GetCertHash());
                Assert.Throws<CryptographicException>(() => c.GetKeyAlgorithm());
                Assert.Throws<CryptographicException>(() => c.GetKeyAlgorithmParameters());
                Assert.Throws<CryptographicException>(() => c.GetKeyAlgorithmParametersString());
                Assert.Throws<CryptographicException>(() => c.GetPublicKey());
                Assert.Throws<CryptographicException>(() => c.GetSerialNumber());
                Assert.Throws<CryptographicException>(() => ignored = c.Issuer);
                Assert.Throws<CryptographicException>(() => ignored = c.Subject);
                Assert.Throws<CryptographicException>(() => ignored = c.RawData);
                Assert.Throws<CryptographicException>(() => ignored = c.Thumbprint);
                Assert.Throws<CryptographicException>(() => ignored = c.SignatureAlgorithm);
                Assert.Throws<CryptographicException>(() => ignored = c.HasPrivateKey);
                Assert.Throws<CryptographicException>(() => ignored = c.Version);
                Assert.Throws<CryptographicException>(() => ignored = c.Archived);
                Assert.Throws<CryptographicException>(() => c.Archived = false);
                Assert.Throws<CryptographicException>(() => c.FriendlyName = "Hi");
                Assert.Throws<CryptographicException>(() => ignored = c.SubjectName);
                Assert.Throws<CryptographicException>(() => ignored = c.IssuerName);
            }
        }

        [Fact]
        public static void TestByteArrayConstructor_DER()
        {
            byte[] expectedThumbPrint = new byte[]
            {
                0x10, 0x8e, 0x2b, 0xa2, 0x36, 0x32, 0x62, 0x0c,
                0x42, 0x7c, 0x57, 0x0b, 0x6d, 0x9d, 0xb5, 0x1a,
                0xc3, 0x13, 0x87, 0xfe,
            };

            using (X509Certificate2 c = new X509Certificate2(TestData.MsCertificate))
            {
                IntPtr h = c.Handle;
                Assert.NotEqual(IntPtr.Zero, h);
                byte[] actualThumbprint = c.GetCertHash();
                Assert.Equal(expectedThumbPrint, actualThumbprint);
            }
        }

        [Fact]
        public static void TestByteArrayConstructor_PEM()
        {
            byte[] expectedThumbPrint =
            {
                0x10, 0x8e, 0x2b, 0xa2, 0x36, 0x32, 0x62, 0x0c,
                0x42, 0x7c, 0x57, 0x0b, 0x6d, 0x9d, 0xb5, 0x1a,
                0xc3, 0x13, 0x87, 0xfe,
            };

            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificatePemBytes))
            {
                IntPtr h = cert.Handle;
                Assert.NotEqual(IntPtr.Zero, h);
                byte[] actualThumbprint = cert.GetCertHash();
                Assert.Equal(expectedThumbPrint, actualThumbprint);
            }
        }

        [Fact]
        public static void TestNullConstructorArguments()
        {
            Assert.Throws<ArgumentException>(() => new X509Certificate2((byte[])null, (String)null));
            Assert.Throws<ArgumentException>(() => new X509Certificate2(new byte[0], (String)null));
            Assert.Throws<ArgumentException>(() => new X509Certificate2((byte[])null, (String)null, X509KeyStorageFlags.DefaultKeySet));
            Assert.Throws<ArgumentException>(() => new X509Certificate2(new byte[0], (String)null, X509KeyStorageFlags.DefaultKeySet));

            // For compat reasons, the (byte[]) constructor (and only that constructor) treats a null or 0-length array as the same
            // as calling the default constructor.
            {
                using (X509Certificate2 c = new X509Certificate2((byte[])null))
                {
                    IntPtr h = c.Handle;
                    Assert.Equal(IntPtr.Zero, h);
                    Assert.Throws<CryptographicException>(() => c.GetCertHash());
                }
            }

            {
                using (X509Certificate2 c = new X509Certificate2(new byte[0]))
                {
                    IntPtr h = c.Handle;
                    Assert.Equal(IntPtr.Zero, h);
                    Assert.Throws<CryptographicException>(() => c.GetCertHash());
                }
            }
        }

        [Fact]
        public static void InvalidCertificateBlob()
        {
            Assert.Throws<CryptographicException>(() => new X509Certificate2(new byte[] { 0x01, 0x02, 0x03 }));
        }
    }
}
