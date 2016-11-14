// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
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
                VerifyDefaultConstructor(c);
            }
        }

        private static void VerifyDefaultConstructor(X509Certificate2 c)
        {
            IntPtr h = c.Handle;
            object ignored;
            Assert.Equal(IntPtr.Zero, h);
            Assert.ThrowsAny<CryptographicException>(() => c.GetCertHash());
            Assert.ThrowsAny<CryptographicException>(() => c.GetKeyAlgorithm());
            Assert.ThrowsAny<CryptographicException>(() => c.GetKeyAlgorithmParameters());
            Assert.ThrowsAny<CryptographicException>(() => c.GetKeyAlgorithmParametersString());
            Assert.ThrowsAny<CryptographicException>(() => c.GetPublicKey());
            Assert.ThrowsAny<CryptographicException>(() => c.GetSerialNumber());
            Assert.ThrowsAny<CryptographicException>(() => ignored = c.Issuer);
            Assert.ThrowsAny<CryptographicException>(() => ignored = c.Subject);
            Assert.ThrowsAny<CryptographicException>(() => ignored = c.RawData);
            Assert.ThrowsAny<CryptographicException>(() => ignored = c.Thumbprint);
            Assert.ThrowsAny<CryptographicException>(() => ignored = c.SignatureAlgorithm);
            Assert.ThrowsAny<CryptographicException>(() => ignored = c.HasPrivateKey);
            Assert.ThrowsAny<CryptographicException>(() => ignored = c.Version);
            Assert.ThrowsAny<CryptographicException>(() => ignored = c.Archived);
            Assert.ThrowsAny<CryptographicException>(() => c.Archived = false);
            Assert.ThrowsAny<CryptographicException>(() => c.FriendlyName = "Hi");
            Assert.ThrowsAny<CryptographicException>(() => ignored = c.SubjectName);
            Assert.ThrowsAny<CryptographicException>(() => ignored = c.IssuerName);
#if netstandard17
            Assert.ThrowsAny<CryptographicException>(() => c.GetCertHashString());
            Assert.ThrowsAny<CryptographicException>(() => c.GetEffectiveDateString());
            Assert.ThrowsAny<CryptographicException>(() => c.GetExpirationDateString());
            Assert.ThrowsAny<CryptographicException>(() => c.GetPublicKeyString());
            Assert.ThrowsAny<CryptographicException>(() => c.GetRawCertData());
            Assert.ThrowsAny<CryptographicException>(() => c.GetRawCertDataString());
            Assert.ThrowsAny<CryptographicException>(() => c.GetSerialNumberString());
#pragma warning disable 0618
            Assert.ThrowsAny<CryptographicException>(() => c.GetIssuerName());
            Assert.ThrowsAny<CryptographicException>(() => c.GetName());
#pragma warning restore 0618
#endif
        }

        [Fact]
        public static void TestConstructor_DER()
        {
            byte[] expectedThumbPrint = new byte[]
            {
                0x10, 0x8e, 0x2b, 0xa2, 0x36, 0x32, 0x62, 0x0c,
                0x42, 0x7c, 0x57, 0x0b, 0x6d, 0x9d, 0xb5, 0x1a,
                0xc3, 0x13, 0x87, 0xfe,
            };

            Action<X509Certificate2> assert = (c) =>
            {
                IntPtr h = c.Handle;
                Assert.NotEqual(IntPtr.Zero, h);
                byte[] actualThumbprint = c.GetCertHash();
                Assert.Equal(expectedThumbPrint, actualThumbprint);
            };

            using (X509Certificate2 c = new X509Certificate2(TestData.MsCertificate))
            {
                assert(c);
#if netstandard17
                using (X509Certificate2 c2 = new X509Certificate2(c))
                {
                    assert(c2);
                }
#endif
            }
        }

        [Fact]
        public static void TestConstructor_PEM()
        {
            byte[] expectedThumbPrint =
            {
                0x10, 0x8e, 0x2b, 0xa2, 0x36, 0x32, 0x62, 0x0c,
                0x42, 0x7c, 0x57, 0x0b, 0x6d, 0x9d, 0xb5, 0x1a,
                0xc3, 0x13, 0x87, 0xfe,
            };

            Action<X509Certificate2> assert = (cert) =>
            {
                IntPtr h = cert.Handle;
                Assert.NotEqual(IntPtr.Zero, h);
                byte[] actualThumbprint = cert.GetCertHash();
                Assert.Equal(expectedThumbPrint, actualThumbprint);
            };

            using (X509Certificate2 c = new X509Certificate2(TestData.MsCertificatePemBytes))
            {
                assert(c);
#if netstandard17
                using (X509Certificate2 c2 = new X509Certificate2(c))
                {
                    assert(c2);
                }
#endif
            }
        }

#if netstandard17
        [Fact]
        public static void TestSerializeDeserialize_DER()
        {
            byte[] expectedThumbPrint = new byte[]
            {
                0x10, 0x8e, 0x2b, 0xa2, 0x36, 0x32, 0x62, 0x0c,
                0x42, 0x7c, 0x57, 0x0b, 0x6d, 0x9d, 0xb5, 0x1a,
                0xc3, 0x13, 0x87, 0xfe,
            };

            Action<X509Certificate2> assert = (c) =>
            {
                IntPtr h = c.Handle;
                Assert.NotEqual(IntPtr.Zero, h);
                byte[] actualThumbprint = c.GetCertHash();
                Assert.Equal(expectedThumbPrint, actualThumbprint);
            };

            using (X509Certificate2 c = new X509Certificate2(TestData.MsCertificate))
            {
                assert(c);
                using (X509Certificate2 c2 = System.Runtime.Serialization.Formatters.Tests.BinaryFormatterHelpers.Clone(c))
                {
                    assert(c2);
                }
            }
        }

        [Fact]
        public static void TestCopyConstructor_NoPal()
        {
            using (var c1 = new X509Certificate2())
            using (var c2 = new X509Certificate2(c1))
            {
                VerifyDefaultConstructor(c1);
                VerifyDefaultConstructor(c2);
            }
        }

        [Fact]
        public static void TestSerializeDeserialize_NoPal()
        {
            using (var c1 = new X509Certificate2())
            using (var c2 = System.Runtime.Serialization.Formatters.Tests.BinaryFormatterHelpers.Clone(c1))
            {
                VerifyDefaultConstructor(c1);
                VerifyDefaultConstructor(c2);
            }
        }

        [Fact]
        public static void TestCopyConstructor_Pal()
        {
            using (var c1 = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            using (var c2 = new X509Certificate2(c1))
            {
                Assert.Equal(c1.GetCertHash(), c2.GetCertHash());
                Assert.Equal(c1.GetKeyAlgorithm(), c2.GetKeyAlgorithm());
                Assert.Equal(c1.GetKeyAlgorithmParameters(), c2.GetKeyAlgorithmParameters());
                Assert.Equal(c1.GetKeyAlgorithmParametersString(), c2.GetKeyAlgorithmParametersString());
                Assert.Equal(c1.GetPublicKey(), c2.GetPublicKey());
                Assert.Equal(c1.GetSerialNumber(), c2.GetSerialNumber());
                Assert.Equal(c1.Issuer, c2.Issuer);
                Assert.Equal(c1.Subject, c2.Subject);
                Assert.Equal(c1.RawData, c2.RawData);
                Assert.Equal(c1.Thumbprint, c2.Thumbprint);
                Assert.Equal(c1.SignatureAlgorithm.Value, c2.SignatureAlgorithm.Value);
                Assert.Equal(c1.HasPrivateKey, c2.HasPrivateKey);
                Assert.Equal(c1.Version, c2.Version);
                Assert.Equal(c1.Archived, c2.Archived);
                Assert.Equal(c1.SubjectName.Name, c2.SubjectName.Name);
                Assert.Equal(c1.IssuerName.Name, c2.IssuerName.Name);
                Assert.Equal(c1.GetCertHashString(), c2.GetCertHashString());
                Assert.Equal(c1.GetEffectiveDateString(), c2.GetEffectiveDateString());
                Assert.Equal(c1.GetExpirationDateString(), c2.GetExpirationDateString());
                Assert.Equal(c1.GetPublicKeyString(), c2.GetPublicKeyString());
                Assert.Equal(c1.GetRawCertData(), c2.GetRawCertData());
                Assert.Equal(c1.GetRawCertDataString(), c2.GetRawCertDataString());
                Assert.Equal(c1.GetSerialNumberString(), c2.GetSerialNumberString());
#pragma warning disable 0618
                Assert.Equal(c1.GetIssuerName(), c2.GetIssuerName());
                Assert.Equal(c1.GetName(), c2.GetName());
#pragma warning restore 0618
            }
        }

        [Fact]
        public static void TestCopyConstructor_Lifetime_Independent()
        {
            var c1 = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword);
            using (var c2 = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                RSA rsa = c2.GetRSAPrivateKey();
                byte[] hash = new byte[20];
                byte[] sig = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                Assert.Equal(TestData.PfxSha1Empty_ExpectedSig, sig);

                c1.Dispose();
                rsa.Dispose();

                // Verify other cert and previous key do not affect cert
                using (rsa = c2.GetRSAPrivateKey())
                {
                    hash = new byte[20];
                    sig = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                    Assert.Equal(TestData.PfxSha1Empty_ExpectedSig, sig);
                }
            }
        }

        [Fact]
        public static void TestCopyConstructor_Lifetime_Cloned()
        {
            var c1 = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword);
            var c2 = new X509Certificate2(c1);
            TestPrivateKey(c1, true);
            TestPrivateKey(c2, true);

            c1.Dispose();
            TestPrivateKey(c1, false);
            TestPrivateKey(c2, true);

            c2.Dispose();
            TestPrivateKey(c2, false);
        }

        [Fact]
        public static void TestCopyConstructor_Lifetime_Cloned_Reversed()
        {
            var c1 = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword);
            var c2 = new X509Certificate2(c1);
            TestPrivateKey(c1, true);
            TestPrivateKey(c2, true);

            c2.Dispose();
            TestPrivateKey(c1, true);
            TestPrivateKey(c2, false);

            c1.Dispose();
            TestPrivateKey(c1, false);
        }

        private static void TestPrivateKey(X509Certificate2 c, bool expectSuccess)
        {
            if (expectSuccess)
            {
                using (RSA rsa = c.GetRSAPrivateKey())
                {
                    byte[] hash = new byte[20];
                    byte[] sig = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                    Assert.Equal(TestData.PfxSha1Empty_ExpectedSig, sig);
                }
            }
            else
            {
                Assert.ThrowsAny<CryptographicException>(() => c.GetRSAPrivateKey());
            }
        }
#endif

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void TestConstructor_SerializedCert_Windows()
        {
            const string ExpectedThumbPrint = "71CB4E2B02738AD44F8B382C93BD17BA665F9914";

            Action<X509Certificate2> assert = (cert) =>
            {
                IntPtr h = cert.Handle;
                Assert.NotEqual(IntPtr.Zero, h);
                Assert.Equal(ExpectedThumbPrint, cert.Thumbprint);
            };

            using (X509Certificate2 c = new X509Certificate2(TestData.StoreSavedAsSerializedCerData))
            {
                assert(c);
#if netstandard17
                using (X509Certificate2 c2 = new X509Certificate2(c))
                {
                    assert(c2);
                }
#endif
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void TestByteArrayConstructor_SerializedCert_Unix()
        {
            Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(TestData.StoreSavedAsSerializedCerData));
        }

        [Fact]
        public static void TestNullConstructorArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new X509Certificate2((string)null));
            Assert.Throws<ArgumentException>(() => new X509Certificate2(IntPtr.Zero));
            Assert.Throws<ArgumentException>(() => new X509Certificate2((byte[])null, (string)null));
            Assert.Throws<ArgumentException>(() => new X509Certificate2(Array.Empty<byte>(), (string)null));
            Assert.Throws<ArgumentException>(() => new X509Certificate2((byte[])null, (string)null, X509KeyStorageFlags.DefaultKeySet));
            Assert.Throws<ArgumentException>(() => new X509Certificate2(Array.Empty<byte>(), (string)null, X509KeyStorageFlags.DefaultKeySet));

            // A null string password does not throw
            using (new X509Certificate2(TestData.MsCertificate, (string)null)) { }
            using (new X509Certificate2(TestData.MsCertificate, (string)null, X509KeyStorageFlags.DefaultKeySet)) { }

#if netstandard17
            Assert.Throws<ArgumentNullException>(() => X509Certificate.CreateFromCertFile(null));
            Assert.Throws<ArgumentNullException>(() => X509Certificate.CreateFromSignedFile(null));
            Assert.Throws<ArgumentNullException>("cert", () => new X509Certificate2((X509Certificate2)null));
            Assert.Throws<ArgumentException>("handle", () => new X509Certificate2(IntPtr.Zero));

            // A null SecureString password does not throw
            using (new X509Certificate2(TestData.MsCertificate, (SecureString)null)) { }
            using (new X509Certificate2(TestData.MsCertificate, (SecureString)null, X509KeyStorageFlags.DefaultKeySet)) { }
#endif

            // For compat reasons, the (byte[]) constructor (and only that constructor) treats a null or 0-length array as the same
            // as calling the default constructor.
            {
                using (X509Certificate2 c = new X509Certificate2((byte[])null))
                {
                    IntPtr h = c.Handle;
                    Assert.Equal(IntPtr.Zero, h);
                    Assert.ThrowsAny<CryptographicException>(() => c.GetCertHash());
                }
            }

            {
                using (X509Certificate2 c = new X509Certificate2(Array.Empty<byte>()))
                {
                    IntPtr h = c.Handle;
                    Assert.Equal(IntPtr.Zero, h);
                    Assert.ThrowsAny<CryptographicException>(() => c.GetCertHash());
                }
            }
        }

        [Fact]
        public static void InvalidCertificateBlob()
        {
            CryptographicException ex = Assert.ThrowsAny<CryptographicException>(
                () => new X509Certificate2(new byte[] { 0x01, 0x02, 0x03 }));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(unchecked((int)0x80092009), ex.HResult);
                // TODO (3233): Test that Message is also set correctly
                //Assert.Equal("Cannot find the requested object.", ex.Message);
            }
            else // Any Unix
            {
                Assert.Equal(0x0D07803A, ex.HResult);
                Assert.Equal("error:0D07803A:asn1 encoding routines:ASN1_ITEM_EX_D2I:nested asn1 error", ex.Message);
            }
        }

        [Fact]
        public static void InvalidStorageFlags()
        {
            byte[] nonEmptyBytes = new byte[1];

            Assert.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate(nonEmptyBytes, string.Empty, (X509KeyStorageFlags)0xFF));

            Assert.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate(string.Empty, string.Empty, (X509KeyStorageFlags)0xFF));

            Assert.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate2(nonEmptyBytes, string.Empty, (X509KeyStorageFlags)0xFF));

            Assert.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate2(string.Empty, string.Empty, (X509KeyStorageFlags)0xFF));

            // No test is performed here for the ephemeral flag failing downlevel, because the live
            // binary is always used by default, meaning it doesn't know EphemeralKeySet doesn't exist.
        }

#if netcoreapp11
        [Fact]
        public static void InvalidStorageFlags_PersistedEphemeral()
        {
            const X509KeyStorageFlags PersistedEphemeral =
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.PersistKeySet;

            byte[] nonEmptyBytes = new byte[1];

            Assert.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate(nonEmptyBytes, string.Empty, PersistedEphemeral));

            Assert.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate(string.Empty, string.Empty, PersistedEphemeral));

            Assert.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate2(nonEmptyBytes, string.Empty, PersistedEphemeral));

            Assert.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate2(string.Empty, string.Empty, PersistedEphemeral));
        }
#endif
    }
}
