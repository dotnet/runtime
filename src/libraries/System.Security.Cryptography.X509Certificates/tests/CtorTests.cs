// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#if HAVE_THUMBPRINT_OVERLOADS
            Assert.ThrowsAny<CryptographicException>(() => c.GetCertHash(HashAlgorithmName.SHA256));
#endif
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
            Assert.ThrowsAny<CryptographicException>(() => c.GetCertHashString());
#if HAVE_THUMBPRINT_OVERLOADS
            Assert.ThrowsAny<CryptographicException>(() => c.GetCertHashString(HashAlgorithmName.SHA256));
#endif
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

#if HAVE_THUMBPRINT_OVERLOADS
            Assert.ThrowsAny<CryptographicException>(
                () => c.TryGetCertHash(HashAlgorithmName.SHA256, Array.Empty<byte>(), out _));
#endif
        }

        [Fact]
        public static void TestConstructor_DER()
        {
            byte[] expectedThumbPrintSha1 =
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
                Assert.Equal(expectedThumbPrintSha1, actualThumbprint);

#if HAVE_THUMBPRINT_OVERLOADS
                byte[] specifiedAlgThumbprint = c.GetCertHash(HashAlgorithmName.SHA1);
                Assert.Equal(expectedThumbPrintSha1, specifiedAlgThumbprint);
#endif
            };

            using (X509Certificate2 c = new X509Certificate2(TestData.MsCertificate))
            {
                assert(c);
                using (X509Certificate2 c2 = new X509Certificate2(c))
                {
                    assert(c2);
                }
            }
        }

        [Fact]
        public static void TestConstructor_PEM()
        {
            byte[] expectedThumbPrintSha1 =
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
                Assert.Equal(expectedThumbPrintSha1, actualThumbprint);

#if HAVE_THUMBPRINT_OVERLOADS
                byte[] specifiedAlgThumbprint = cert.GetCertHash(HashAlgorithmName.SHA1);
                Assert.Equal(expectedThumbPrintSha1, specifiedAlgThumbprint);
#endif
            };

            using (X509Certificate2 c = new X509Certificate2(TestData.MsCertificatePemBytes))
            {
                assert(c);
                using (X509Certificate2 c2 = new X509Certificate2(c))
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
        public static void TestCopyConstructor_Pal()
        {
            using (var c1 = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            using (var c2 = new X509Certificate2(c1))
            {
                Assert.Equal(c1.GetCertHash(), c2.GetCertHash());
#if HAVE_THUMBPRINT_OVERLOADS
                Assert.Equal(c1.GetCertHash(HashAlgorithmName.SHA256), c2.GetCertHash(HashAlgorithmName.SHA256));
#endif
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
#if HAVE_THUMBPRINT_OVERLOADS
                Assert.Equal(c1.GetCertHashString(HashAlgorithmName.SHA256), c2.GetCertHashString(HashAlgorithmName.SHA256));
#endif
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
                byte[] hash = new byte[SHA256.HashSizeInBytes];
                byte[] sig = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                Assert.Equal(TestData.PfxSha256Empty_ExpectedSig, sig);

                c1.Dispose();
                rsa.Dispose();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Verify other cert and previous key do not affect cert
                using (rsa = c2.GetRSAPrivateKey())
                {
                    hash = new byte[SHA256.HashSizeInBytes];
                    sig = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    Assert.Equal(TestData.PfxSha256Empty_ExpectedSig, sig);
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
            GC.Collect();
            GC.WaitForPendingFinalizers();

            TestPrivateKey(c1, false);
            TestPrivateKey(c2, true);

            c2.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();

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
            GC.Collect();
            GC.WaitForPendingFinalizers();

            TestPrivateKey(c1, true);
            TestPrivateKey(c2, false);

            c1.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            TestPrivateKey(c1, false);
        }

        private static void TestPrivateKey(X509Certificate2 c, bool expectSuccess)
        {
            if (expectSuccess)
            {
                using (RSA rsa = c.GetRSAPrivateKey())
                {
                    byte[] hash = new byte[SHA256.HashSizeInBytes];
                    byte[] sig = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    Assert.Equal(TestData.PfxSha256Empty_ExpectedSig, sig);
                }
            }
            else
            {
                Assert.ThrowsAny<CryptographicException>(() => c.GetRSAPrivateKey());
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // StoreSavedAsSerializedCerData not supported on Unix
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
                using (X509Certificate2 c2 = new X509Certificate2(c))
                {
                    assert(c2);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // StoreSavedAsSerializedCerData not supported on Unix
        public static void TestByteArrayConstructor_SerializedCert_Unix()
        {
            Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(TestData.StoreSavedAsSerializedCerData));
        }

        [Fact]
        public static void TestNullConstructorArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new X509Certificate2((string)null));
            AssertExtensions.Throws<ArgumentException>("handle", () => new X509Certificate2(IntPtr.Zero));
            AssertExtensions.Throws<ArgumentException>("rawData", () => new X509Certificate2((byte[])null, (string)null));
            AssertExtensions.Throws<ArgumentException>("rawData", () => new X509Certificate2(Array.Empty<byte>(), (string)null));
            AssertExtensions.Throws<ArgumentException>("rawData", () => new X509Certificate2((byte[])null, (string)null, X509KeyStorageFlags.DefaultKeySet));
            AssertExtensions.Throws<ArgumentException>("rawData", () => new X509Certificate2(Array.Empty<byte>(), (string)null, X509KeyStorageFlags.DefaultKeySet));

            // A null string password does not throw
            using (new X509Certificate2(TestData.MsCertificate, (string)null)) { }
            using (new X509Certificate2(TestData.MsCertificate, (string)null, X509KeyStorageFlags.DefaultKeySet)) { }

            Assert.Throws<ArgumentNullException>(() => X509Certificate.CreateFromCertFile(null));
            Assert.Throws<ArgumentNullException>(() => X509Certificate.CreateFromSignedFile(null));
            AssertExtensions.Throws<ArgumentNullException>("cert", () => new X509Certificate2((X509Certificate2)null));
            AssertExtensions.Throws<ArgumentException>("handle", () => new X509Certificate2(IntPtr.Zero));

            // A null SecureString password does not throw
            using (new X509Certificate2(TestData.MsCertificate, (SecureString)null)) { }
            using (new X509Certificate2(TestData.MsCertificate, (SecureString)null, X509KeyStorageFlags.DefaultKeySet)) { }

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

            CryptographicException defaultException = new CryptographicException();
            Assert.NotEqual(defaultException.Message, ex.Message);

            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(unchecked((int)0x80092009), ex.HResult);
                // TODO (3233): Test that Message is also set correctly
                //Assert.Equal("Cannot find the requested object.", ex.Message);
            }
            else if (PlatformDetection.UsesAppleCrypto)
            {
                Assert.Equal(-25257, ex.HResult);
            }
            else // Any Unix
            {
                Assert.Equal(new CryptographicException("message").HResult, ex.HResult);
            }
        }

#if !NO_EPHEMERALKEYSET_AVAILABLE
        [Fact]
        public static void InvalidStorageFlags()
        {
            byte[] nonEmptyBytes = new byte[1];

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate(nonEmptyBytes, string.Empty, (X509KeyStorageFlags)0xFF));

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate(string.Empty, string.Empty, (X509KeyStorageFlags)0xFF));

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate2(nonEmptyBytes, string.Empty, (X509KeyStorageFlags)0xFF));

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate2(string.Empty, string.Empty, (X509KeyStorageFlags)0xFF));

            // No test is performed here for the ephemeral flag failing downlevel, because the live
            // binary is always used by default, meaning it doesn't know EphemeralKeySet doesn't exist.
        }

        [Fact]
        public static void InvalidStorageFlags_PersistedEphemeral()
        {
            const X509KeyStorageFlags PersistedEphemeral =
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.PersistKeySet;

            byte[] nonEmptyBytes = new byte[1];

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate(nonEmptyBytes, string.Empty, PersistedEphemeral));

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate(string.Empty, string.Empty, PersistedEphemeral));

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate2(nonEmptyBytes, string.Empty, PersistedEphemeral));

            AssertExtensions.Throws<ArgumentException>(
                "keyStorageFlags",
                () => new X509Certificate2(string.Empty, string.Empty, PersistedEphemeral));
        }
#endif
    }
}
