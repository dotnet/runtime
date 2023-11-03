// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Test.Cryptography;
using Xunit;

// String factory methods are obsolete. Warning is disabled for the entire file as most tests exercise the obsolete methods
#pragma warning disable SYSLIB0045

namespace System.Security.Cryptography.Tests
{
    public static class CryptoConfigTests
    {
        [Fact]
        public static void DefaultStaticCreateMethods()
        {
#pragma warning disable SYSLIB0007 // These methods are marked as Obsolete
            // .NET Core does not allow the base classes to pick an algorithm.
            Assert.Throws<PlatformNotSupportedException>(() => AsymmetricAlgorithm.Create());
            Assert.Throws<PlatformNotSupportedException>(() => HashAlgorithm.Create());
            Assert.Throws<PlatformNotSupportedException>(() => KeyedHashAlgorithm.Create());
            Assert.Throws<PlatformNotSupportedException>(() => HMAC.Create());
            Assert.Throws<PlatformNotSupportedException>(() => SymmetricAlgorithm.Create());
#pragma warning restore SYSLIB0007
        }

        [Fact]
        public static void NamedCreateMethods_NullInput()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => AsymmetricAlgorithm.Create(null));
            AssertExtensions.Throws<ArgumentNullException>("name", () => HashAlgorithm.Create(null));
            AssertExtensions.Throws<ArgumentNullException>("name", () => KeyedHashAlgorithm.Create(null));
            AssertExtensions.Throws<ArgumentNullException>("name", () => HMAC.Create(null));
            AssertExtensions.Throws<ArgumentNullException>("name", () => SymmetricAlgorithm.Create(null));
        }

        // The returned types on .NET Framework can differ when the machine is in FIPS mode.
        // So check hash algorithms via a more complicated manner.
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        [InlineData("MD5", typeof(MD5))]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#md5", typeof(MD5))]
        [InlineData("System.Security.Cryptography.HashAlgorithm", typeof(SHA1))]
        [InlineData("SHA1", typeof(SHA1))]
        [InlineData("http://www.w3.org/2000/09/xmldsig#sha1", typeof(SHA1))]
        [InlineData("SHA256", typeof(SHA256))]
        [InlineData("SHA-256", typeof(SHA256))]
        [InlineData("http://www.w3.org/2001/04/xmlenc#sha256", typeof(SHA256))]
        [InlineData("SHA384", typeof(SHA384))]
        [InlineData("SHA-384", typeof(SHA384))]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#sha384", typeof(SHA384))]
        [InlineData("SHA512", typeof(SHA512))]
        [InlineData("SHA-512", typeof(SHA512))]
        [InlineData("http://www.w3.org/2001/04/xmlenc#sha512", typeof(SHA512))]
        public static void NamedHashAlgorithmCreate(string identifier, Type baseType)
        {
            using (HashAlgorithm created = HashAlgorithm.Create(identifier))
            {
                Assert.NotNull(created);
                Assert.IsAssignableFrom(baseType, created);

                using (HashAlgorithm equivalent =
                    (HashAlgorithm)baseType.GetMethod("Create", Type.EmptyTypes).Invoke(null, null))
                {
                    byte[] input = { 1, 2, 3, 4, 5 };
                    byte[] equivHash = equivalent.ComputeHash(input);
                    byte[] createdHash = created.ComputeHash(input);
                    Assert.Equal(equivHash, createdHash);
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        [InlineData("System.Security.Cryptography.HMAC", typeof(HMACSHA1))]
        [InlineData("System.Security.Cryptography.KeyedHashAlgorithm", typeof(HMACSHA1))]
        [InlineData("System.Security.Cryptography.HMACSHA1", typeof(HMACSHA1))]
        [InlineData("HMACSHA1", typeof(HMACSHA1))]
        [InlineData("http://www.w3.org/2000/09/xmldsig#hmac-sha1", typeof(HMACSHA1))]
        [InlineData("System.Security.Cryptography.HMACSHA256", typeof(HMACSHA256))]
        [InlineData("HMACSHA256", typeof(HMACSHA256))]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", typeof(HMACSHA256))]
        [InlineData("System.Security.Cryptography.HMACSHA384", typeof(HMACSHA384))]
        [InlineData("HMACSHA384", typeof(HMACSHA384))]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#hmac-sha384", typeof(HMACSHA384))]
        [InlineData("System.Security.Cryptography.HMACSHA512", typeof(HMACSHA512))]
        [InlineData("HMACSHA512", typeof(HMACSHA512))]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#hmac-sha512", typeof(HMACSHA512))]
        [InlineData("System.Security.Cryptography.HMACMD5", typeof(HMACMD5))]
        [InlineData("HMACMD5", typeof(HMACMD5))]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#hmac-md5", typeof(HMACMD5))]
        public static void NamedKeyedHashAlgorithmCreate(string identifier, Type actualType)
        {
            using (KeyedHashAlgorithm kha = KeyedHashAlgorithm.Create(identifier))
            {
                Assert.IsType(actualType, kha);

                // .NET Core only has HMAC keyed hash algorithms, so combine the two tests
                using (HMAC hmac = HMAC.Create(identifier))
                {
                    Assert.IsType(actualType, hmac);
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        [InlineData("AES", typeof(Aes))]
#pragma warning disable SYSLIB0022 // Rijndael types are obsolete
        [InlineData("Rijndael", typeof(Rijndael))]
        [InlineData("System.Security.Cryptography.Rijndael", typeof(Rijndael))]
        [InlineData("http://www.w3.org/2001/04/xmlenc#aes128-cbc", typeof(Rijndael))]
        [InlineData("http://www.w3.org/2001/04/xmlenc#aes192-cbc", typeof(Rijndael))]
        [InlineData("http://www.w3.org/2001/04/xmlenc#aes256-cbc", typeof(Rijndael))]
#pragma warning restore SYSLIB0022
        [InlineData("3DES", typeof(TripleDES))]
        [InlineData("TripleDES", typeof(TripleDES))]
        [InlineData("System.Security.Cryptography.TripleDES", typeof(TripleDES))]
        [InlineData("http://www.w3.org/2001/04/xmlenc#tripledes-cbc", typeof(TripleDES))]
        [InlineData("DES", typeof(DES))]
        [InlineData("System.Security.Cryptography.DES", typeof(DES))]
        [InlineData("http://www.w3.org/2001/04/xmlenc#des-cbc", typeof(DES))]
        public static void NamedSymmetricAlgorithmCreate(string identifier, Type baseType)
        {
            using (SymmetricAlgorithm created = SymmetricAlgorithm.Create(identifier))
            {
                Assert.NotNull(created);
                Assert.IsAssignableFrom(baseType, created);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        [InlineData("RSA", typeof(RSA))]
        [InlineData("System.Security.Cryptography.RSA", typeof(RSA))]
        [InlineData("ECDsa", typeof(ECDsa))]
        public static void NamedAsymmetricAlgorithmCreate(string identifier, Type baseType)
        {
            using (AsymmetricAlgorithm created = AsymmetricAlgorithm.Create(identifier))
            {
                Assert.NotNull(created);
                Assert.IsAssignableFrom(baseType, created);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        [InlineData("DSA", typeof(DSA))]
        [InlineData("System.Security.Cryptography.DSA", typeof(DSA))]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void NamedAsymmetricAlgorithmCreate_DSA(string identifier, Type baseType)
        {
            using (AsymmetricAlgorithm created = AsymmetricAlgorithm.Create(identifier))
            {
                Assert.NotNull(created);
                Assert.IsAssignableFrom(baseType, created);
            }
        }

        [Theory]
        [InlineData("DSA")]
        [InlineData("System.Security.Cryptography.DSA")]
        [PlatformSpecific(PlatformSupport.MobileAppleCrypto)]
        public static void NamedAsymmetricAlgorithmCreate_DSA_NotSupported(string identifier)
        {
            Assert.Null(AsymmetricAlgorithm.Create(identifier));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        public static void NamedCreate_Mismatch()
        {
            Assert.Throws<InvalidCastException>(() => AsymmetricAlgorithm.Create("SHA1"));
            Assert.Throws<InvalidCastException>(() => KeyedHashAlgorithm.Create("SHA1"));
            Assert.Throws<InvalidCastException>(() => HMAC.Create("SHA1"));
            Assert.Throws<InvalidCastException>(() => SymmetricAlgorithm.Create("SHA1"));
            Assert.Throws<InvalidCastException>(() => HashAlgorithm.Create("RSA"));
        }

        [Fact]
        public static void NamedCreate_Unknown()
        {
            const string UnknownAlgorithmName = "XYZZY";
            Assert.Null(AsymmetricAlgorithm.Create(UnknownAlgorithmName));
            Assert.Null(HashAlgorithm.Create(UnknownAlgorithmName));
            Assert.Null(KeyedHashAlgorithm.Create(UnknownAlgorithmName));
            Assert.Null(HMAC.Create(UnknownAlgorithmName));
            Assert.Null(SymmetricAlgorithm.Create(UnknownAlgorithmName));
        }

        [Fact]
        public static void AllowOnlyFipsAlgorithms()
        {
            Assert.False(CryptoConfig.AllowOnlyFipsAlgorithms);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddOID_MapNameToOID_ReturnsMapped()
        {
            CryptoConfig.AddOID("1.3.14.3.2.28", "SHAFancy");
            Assert.Equal("1.3.14.3.2.28", CryptoConfig.MapNameToOID("SHAFancy"));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddOID_EmptyString_Throws()
        {
            AssertExtensions.Throws<ArgumentException>("names", () => CryptoConfig.AddOID(string.Empty, string.Empty));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddOID_EmptyNamesArray()
        {
            CryptoConfig.AddOID("1.3.14.3.2.28", new string[0]);
            // There is no verifiable behavior in this case. We only check that we don't throw.
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddOID_NullOid_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("oid", () => CryptoConfig.AddOID(null, string.Empty));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddOID_NullNames_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("names", () => CryptoConfig.AddOID(string.Empty, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddAlgorithm_CreateFromName_ReturnsMapped()
        {
            CryptoConfig.AddAlgorithm(typeof(AesCryptoServiceProvider), "AESFancy");
            Assert.Equal(typeof(AesCryptoServiceProvider).FullName, CryptoConfig.CreateFromName("AESFancy").GetType().FullName);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddAlgorithm_NonVisibleType()
        {
            AssertExtensions.Throws<ArgumentException>("algorithm", () => CryptoConfig.AddAlgorithm(typeof(AESFancy), "AESFancy"));
        }

        private class AESFancy
        {
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddAlgorithm_EmptyString_Throws()
        {
            AssertExtensions.Throws<ArgumentException>("names", () => CryptoConfig.AddAlgorithm(typeof(CryptoConfigTests), string.Empty));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddAlgorithm_EmptyNamesArray()
        {
            CryptoConfig.AddAlgorithm(typeof(AesCryptoServiceProvider), new string[0]);
            // There is no verifiable behavior in this case. We only check that we don't throw.
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddAlgorithm_NullAlgorithm_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", () => CryptoConfig.AddAlgorithm(null, string.Empty));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void AddAlgorithm_NullNames_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("names", () => CryptoConfig.AddAlgorithm(typeof(CryptoConfigTests), null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void StaticCreateMethods()
        {
            // Ensure static create methods exist and don't throw

            // Some do not have public concrete types (in Algorithms assembly) so in those cases we only check for null\failure.
            VerifyStaticCreateResult(Aes.Create(typeof(AesManaged).FullName), typeof(AesManaged));
            Assert.Null(DES.Create(string.Empty));
            Assert.Null(DSA.Create(string.Empty));
            Assert.Null(ECDsa.Create(string.Empty));
            Assert.Null(MD5.Create(string.Empty));
            Assert.Null(RandomNumberGenerator.Create(string.Empty));
            Assert.Null(RC2.Create(string.Empty));
#pragma warning disable SYSLIB0022 // Rijndael types are obsolete
            VerifyStaticCreateResult(Rijndael.Create(typeof(RijndaelManaged).FullName), typeof(RijndaelManaged));
            Assert.Null(RSA.Create(string.Empty));
            Assert.Null(SHA1.Create(string.Empty));
            VerifyStaticCreateResult(SHA256.Create(typeof(SHA256Managed).FullName), typeof(SHA256Managed));
            VerifyStaticCreateResult(SHA384.Create(typeof(SHA384Managed).FullName), typeof(SHA384Managed));
            VerifyStaticCreateResult(SHA512.Create(typeof(SHA512Managed).FullName), typeof(SHA512Managed));
#pragma warning restore SYSLIB0022 // Rijndael types are obsolete

            static void VerifyStaticCreateResult(object obj, Type expectedType)
            {
                Assert.NotNull(obj);
                Assert.IsType(expectedType, obj);
                (obj as IDisposable)?.Dispose();
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void MapNameToOID()
        {
            Assert.Throws<ArgumentNullException>(() => CryptoConfig.MapNameToOID(null));

            // Test some oids unique to CryptoConfig
            Assert.Equal("1.3.14.3.2.26", CryptoConfig.MapNameToOID("SHA"));
            Assert.Equal("1.3.14.3.2.26", CryptoConfig.MapNameToOID("sha"));
            Assert.Equal("1.2.840.113549.3.7", CryptoConfig.MapNameToOID("TripleDES"));

            // Test fallback to Oid class
            Assert.Equal("1.3.36.3.3.2.8.1.1.8", CryptoConfig.MapNameToOID("brainpoolP256t1"));

            // Invalid oid
            Assert.Null(CryptoConfig.MapNameToOID("NOT_A_VALID_OID"));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void CreateFromName_validation()
        {
            Assert.Throws<ArgumentNullException>(() => CryptoConfig.CreateFromName(null));
            Assert.Throws<ArgumentNullException>(() => CryptoConfig.CreateFromName(null, null));
            Assert.Throws<ArgumentNullException>(() => CryptoConfig.CreateFromName(null, string.Empty));
            Assert.Null(CryptoConfig.CreateFromName(string.Empty, null));
            Assert.Null(CryptoConfig.CreateFromName("SHA", 1, 2));
        }

        public static IEnumerable<object[]> AllValidNames
        {
            get
            {
                // Keyed Hash Algorithms - supported on all platforms
                yield return new object[] { "System.Security.Cryptography.HMAC", "System.Security.Cryptography.HMACSHA1", true };
                yield return new object[] { "System.Security.Cryptography.KeyedHashAlgorithm", "System.Security.Cryptography.HMACSHA1", true };
                yield return new object[] { "HMACSHA1", "System.Security.Cryptography.HMACSHA1", true };
                yield return new object[] { "System.Security.Cryptography.HMACSHA1", null, true };
                yield return new object[] { "HMACSHA256", "System.Security.Cryptography.HMACSHA256", true };
                yield return new object[] { "System.Security.Cryptography.HMACSHA256", null, true };
                yield return new object[] { "HMACSHA384", "System.Security.Cryptography.HMACSHA384", true };
                yield return new object[] { "System.Security.Cryptography.HMACSHA384", null, true };
                yield return new object[] { "HMACSHA512", "System.Security.Cryptography.HMACSHA512", true };
                yield return new object[] { "System.Security.Cryptography.HMACSHA512", null, true };

                if (PlatformDetection.IsBrowser)
                {
                    // Hash functions
                    yield return new object[] { "SHA", typeof(SHA1Managed).FullName, true };
                    yield return new object[] { "SHA1", typeof(SHA1Managed).FullName, true };
                    yield return new object[] { "System.Security.Cryptography.SHA1", typeof(SHA1Managed).FullName, true };
                    yield return new object[] { "SHA256", typeof(SHA256Managed).FullName, true };
                    yield return new object[] { "SHA-256", typeof(SHA256Managed).FullName, true };
                    yield return new object[] { "System.Security.Cryptography.SHA256", typeof(SHA256Managed).FullName, true };
                    yield return new object[] { "SHA384", typeof(SHA384Managed).FullName, true };
                    yield return new object[] { "SHA-384", typeof(SHA384Managed).FullName, true };
                    yield return new object[] { "System.Security.Cryptography.SHA384", typeof(SHA384Managed).FullName, true };
                    yield return new object[] { "SHA512", typeof(SHA512Managed).FullName, true };
                    yield return new object[] { "SHA-512", typeof(SHA512Managed).FullName, true };
                    yield return new object[] { "System.Security.Cryptography.SHA512", typeof(SHA512Managed).FullName, true };
                }
                else
                {
                    // Random number generator
                    yield return new object[] { "RandomNumberGenerator", "System.Security.Cryptography.RNGCryptoServiceProvider", true };
                    yield return new object[] { "System.Security.Cryptography.RandomNumberGenerator", "System.Security.Cryptography.RNGCryptoServiceProvider", true };

                    // Hash functions
                    yield return new object[] { "SHA", "System.Security.Cryptography.SHA1CryptoServiceProvider", true };
                    yield return new object[] { "SHA1", "System.Security.Cryptography.SHA1CryptoServiceProvider", true };
                    yield return new object[] { "System.Security.Cryptography.SHA1", "System.Security.Cryptography.SHA1CryptoServiceProvider", true };
                    yield return new object[] { "System.Security.Cryptography.HashAlgorithm", "System.Security.Cryptography.SHA1CryptoServiceProvider", true };
                    yield return new object[] { "MD5", "System.Security.Cryptography.MD5CryptoServiceProvider", true };
                    yield return new object[] { "System.Security.Cryptography.MD5", "System.Security.Cryptography.MD5CryptoServiceProvider", true };
                    yield return new object[] { "SHA256", typeof(SHA256Managed).FullName, true };
                    yield return new object[] { "SHA-256", typeof(SHA256Managed).FullName, true };
                    yield return new object[] { "System.Security.Cryptography.SHA256", typeof(SHA256Managed).FullName, true };
                    yield return new object[] { "SHA384", typeof(SHA384Managed).FullName, true };
                    yield return new object[] { "SHA-384", typeof(SHA384Managed).FullName, true };
                    yield return new object[] { "System.Security.Cryptography.SHA384", typeof(SHA384Managed).FullName, true };
                    yield return new object[] { "SHA512", typeof(SHA512Managed).FullName, true };
                    yield return new object[] { "SHA-512", typeof(SHA512Managed).FullName, true };
                    yield return new object[] { "System.Security.Cryptography.SHA512", typeof(SHA512Managed).FullName, true };

                    // Keyed Hash Algorithms - not supported on Browser
                    yield return new object[] { "HMACMD5", "System.Security.Cryptography.HMACMD5", true };
                    yield return new object[] { "System.Security.Cryptography.HMACMD5", null, true };

                    // Asymmetric algorithms
                    yield return new object[] { "RSA", "System.Security.Cryptography.RSACryptoServiceProvider", true };
                    yield return new object[] { "System.Security.Cryptography.RSA", "System.Security.Cryptography.RSACryptoServiceProvider", true };
                    yield return new object[] { "System.Security.Cryptography.AsymmetricAlgorithm", "System.Security.Cryptography.RSACryptoServiceProvider", true };
                    if (!PlatformDetection.UsesMobileAppleCrypto)
                    {
                        yield return new object[] { "DSA", "System.Security.Cryptography.DSACryptoServiceProvider", true };
                        yield return new object[] { "System.Security.Cryptography.DSA", "System.Security.Cryptography.DSACryptoServiceProvider", true };
                    }
                    yield return new object[] { "ECDsa", "System.Security.Cryptography.ECDsaCng", true };
                    yield return new object[] { "ECDsaCng", "System.Security.Cryptography.ECDsaCng", false };
                    yield return new object[] { "System.Security.Cryptography.ECDsaCng", null, false };
                    yield return new object[] { "DES", "System.Security.Cryptography.DESCryptoServiceProvider", true };
                    yield return new object[] { "System.Security.Cryptography.DES", "System.Security.Cryptography.DESCryptoServiceProvider", true };
                    yield return new object[] { "3DES", "System.Security.Cryptography.TripleDESCryptoServiceProvider", true };
                    yield return new object[] { "TripleDES", "System.Security.Cryptography.TripleDESCryptoServiceProvider", true };
                    yield return new object[] { "Triple DES", "System.Security.Cryptography.TripleDESCryptoServiceProvider", true };
                    yield return new object[] { "System.Security.Cryptography.TripleDES", "System.Security.Cryptography.TripleDESCryptoServiceProvider", true };
                    yield return new object[] { "RC2", "System.Security.Cryptography.RC2CryptoServiceProvider", true };
                    yield return new object[] { "System.Security.Cryptography.RC2", "System.Security.Cryptography.RC2CryptoServiceProvider", true };
#pragma warning disable SYSLIB0022 // Rijndael types are obsolete
                    yield return new object[] { "Rijndael", typeof(RijndaelManaged).FullName, true };
                    yield return new object[] { "System.Security.Cryptography.Rijndael", typeof(RijndaelManaged).FullName, true };
                    yield return new object[] { "System.Security.Cryptography.SymmetricAlgorithm", typeof(RijndaelManaged).FullName, true };
#pragma warning restore SYSLIB0022 // Rijndael types are obsolete
                    yield return new object[] { "AES", "System.Security.Cryptography.AesCryptoServiceProvider", true };
                    yield return new object[] { "AesCryptoServiceProvider", "System.Security.Cryptography.AesCryptoServiceProvider", true };
                    yield return new object[] { "System.Security.Cryptography.AesCryptoServiceProvider", "System.Security.Cryptography.AesCryptoServiceProvider", true };
                    yield return new object[] { "AesManaged", typeof(AesManaged).FullName, true };
                    yield return new object[] { "System.Security.Cryptography.AesManaged", typeof(AesManaged).FullName, true };

                    // Xml Dsig/ Enc Hash algorithms
                    yield return new object[] { "http://www.w3.org/2000/09/xmldsig#sha1", "System.Security.Cryptography.SHA1CryptoServiceProvider", true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#sha256", typeof(SHA256Managed).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#sha512", typeof(SHA512Managed).FullName, true };

                    // Xml Encryption symmetric keys
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#des-cbc", "System.Security.Cryptography.DESCryptoServiceProvider", true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#tripledes-cbc", "System.Security.Cryptography.TripleDESCryptoServiceProvider", true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#kw-tripledes", "System.Security.Cryptography.TripleDESCryptoServiceProvider", true };
#pragma warning disable SYSLIB0022 // Rijndael types are obsolete
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#aes128-cbc", typeof(RijndaelManaged).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#kw-aes128", typeof(RijndaelManaged).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#aes192-cbc", typeof(RijndaelManaged).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#kw-aes192", typeof(RijndaelManaged).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#aes256-cbc", typeof(RijndaelManaged).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmlenc#kw-aes256", typeof(RijndaelManaged).FullName, true };
#pragma warning restore SYSLIB0022 // Rijndael types are obsolete

                    // Xml Dsig HMAC URIs from http://www.w3.org/TR/xmldsig-core/
                    yield return new object[] { "http://www.w3.org/2000/09/xmldsig#hmac-sha1", typeof(HMACSHA1).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmldsig-more#sha384", typeof(SHA384Managed).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmldsig-more#hmac-md5", typeof(HMACMD5).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", typeof(HMACSHA256).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmldsig-more#hmac-sha384", typeof(HMACSHA384).FullName, true };
                    yield return new object[] { "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512", typeof(HMACSHA512).FullName, true };

                    // X509
                    yield return new object[] { "1.3.6.1.5.5.7.1.1", "System.Security.Cryptography.X509Certificates.X509AuthorityInformationAccessExtension", true };
                    yield return new object[] { "2.5.29.10", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension", true };
                    yield return new object[] { "2.5.29.19", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension", true };
                    yield return new object[] { "2.5.29.14", "System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension", true };
                    yield return new object[] { "2.5.29.15", "System.Security.Cryptography.X509Certificates.X509KeyUsageExtension", true };
                    yield return new object[] { "2.5.29.17", "System.Security.Cryptography.X509Certificates.X509SubjectAlternativeNameExtension", true };
                    yield return new object[] { "2.5.29.35", "System.Security.Cryptography.X509Certificates.X509AuthorityKeyIdentifierExtension", true };
                    yield return new object[] { "2.5.29.37", "System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension", true };
                    yield return new object[] { "X509Chain", "System.Security.Cryptography.X509Certificates.X509Chain", true };

                    // PKCS9 attributes
                    yield return new object[] { "1.2.840.113549.1.9.3", "System.Security.Cryptography.Pkcs.Pkcs9ContentType", true };
                    yield return new object[] { "1.2.840.113549.1.9.4", "System.Security.Cryptography.Pkcs.Pkcs9MessageDigest", true };
                    yield return new object[] { "1.2.840.113549.1.9.5", "System.Security.Cryptography.Pkcs.Pkcs9SigningTime", true };
                    yield return new object[] { "1.3.6.1.4.1.311.88.2.1", "System.Security.Cryptography.Pkcs.Pkcs9DocumentName", true };
                    yield return new object[] { "1.3.6.1.4.1.311.88.2.2", "System.Security.Cryptography.Pkcs.Pkcs9DocumentDescription", true };
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [MemberData(nameof(AllValidNames))]
        public static void CreateFromName_AllValidNames(string name, string typeName, bool supportsUnixMac)
        {
            bool isWindows = OperatingSystem.IsWindows();

            if (supportsUnixMac || isWindows)
            {
                object obj = CryptoConfig.CreateFromName(name);
                Assert.NotNull(obj);

                if (typeName == null)
                {
                    typeName = name;
                }

                // ECDsa is special on non-Windows
                if (isWindows || name != "ECDsa")
                {
                    Assert.Equal(typeName, obj.GetType().FullName);
                }
                else
                {
                    Assert.NotEqual(typeName, obj.GetType().FullName);
                }

                if (obj is IDisposable)
                {
                    ((IDisposable)obj).Dispose();
                }
            }
            else
            {
                // These will be the Csp types, which currently aren't supported on Mac\Unix
                Assert.Throws<TargetInvocationException> (() => CryptoConfig.CreateFromName(name));
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void CreateFromName_CtorArguments()
        {
            string className = typeof(ClassWithCtorArguments).FullName + ", System.Security.Cryptography.Tests";

            // Pass int instead of string
            Assert.Throws<MissingMethodException>(() => CryptoConfig.CreateFromName(className, 1));

            // Valid case
            object obj = CryptoConfig.CreateFromName(className, "Hello");
            Assert.NotNull(obj);
            Assert.IsType<ClassWithCtorArguments>(obj);

            ClassWithCtorArguments ctorObj = (ClassWithCtorArguments)obj;
            Assert.Equal("Hello", ctorObj.MyString);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void EncodeOID_Validation()
        {
#pragma warning disable SYSLIB0031 // EncodeOID is obsolete
            Assert.Throws<ArgumentNullException>(() => CryptoConfig.EncodeOID(null));
            Assert.Throws<FormatException>(() => CryptoConfig.EncodeOID(string.Empty));
            Assert.Throws<FormatException>(() => CryptoConfig.EncodeOID("BAD.OID"));
            Assert.Throws<FormatException>(() => CryptoConfig.EncodeOID("1.2.BAD.OID"));
            Assert.Throws<OverflowException>(() => CryptoConfig.EncodeOID("1." + uint.MaxValue));
#pragma warning restore SYSLIB0031
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void EncodeOID_Compat()
        {
#pragma warning disable SYSLIB0031 // EncodeOID is obsolete
            string actual = CryptoConfig.EncodeOID("-1.2.-3").ByteArrayToHex();
            Assert.Equal("0602DAFD", actual); // Negative values not checked
#pragma warning restore SYSLIB0031
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void EncodeOID_Length_Boundary()
        {
#pragma warning disable SYSLIB0031 // EncodeOID is obsolete
            string valueToRepeat = "1.1";

            // Build a string like 1.11.11.11. ... .11.1, which has 0x80 separators.
            // The BER/DER encoding of an OID has a minimum number of bytes as the number of separator characters,
            // so this would produce an OID with a length segment of more than one byte, which EncodeOID can't handle.
            string s = new StringBuilder(valueToRepeat.Length * 0x80).Insert(0, valueToRepeat, 0x80).ToString();
            Assert.Throws<CryptographicUnexpectedOperationException>(() => CryptoConfig.EncodeOID(s));

            // Try again with one less separator for the boundary case, but the particular output is really long
            // and would just clutter up this test, so only verify it doesn't throw.
            s = new StringBuilder(valueToRepeat.Length * 0x7f).Insert(0, valueToRepeat, 0x7f).ToString();
            CryptoConfig.EncodeOID(s);
#pragma warning restore SYSLIB0031
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        [InlineData(0x4000, "0603818028")]
        [InlineData(0x200000, "060481808028")]
        [InlineData(0x10000000, "06058180808028")]
        [InlineData(0x10000001, "06058180808029")]
        [InlineData(int.MaxValue, "060127")]
        public static void EncodeOID_Value_Boundary_And_Compat(uint elementValue, string expectedEncoding)
        {
            // Boundary cases in EncodeOID; output may produce the wrong value mathematically due to encoding
            // algorithm semantics but included here for compat reasons.
#pragma warning disable SYSLIB0031 // EncodeOID is obsolete
            byte[] actual = CryptoConfig.EncodeOID("1." + elementValue.ToString());
            byte[] expected = expectedEncoding.HexToByteArray();
            Assert.Equal(expected, actual);
#pragma warning restore SYSLIB0031
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        [InlineData("SHA1", "1.3.14.3.2.26", "06052B0E03021A")]
        [InlineData("DES", "1.3.14.3.2.7", "06052B0E030207")]
        [InlineData("MD5", "1.2.840.113549.2.5", "06082A864886F70D0205")]
        public static void MapAndEncodeOID(string alg, string expectedOid, string expectedEncoding)
        {
#pragma warning disable SYSLIB0031 // EncodeOID is obsolete
            string oid = CryptoConfig.MapNameToOID(alg);
            Assert.Equal(expectedOid, oid);

            byte[] actual = CryptoConfig.EncodeOID(oid);
            byte[] expected = expectedEncoding.HexToByteArray();
            Assert.Equal(expected, actual);
#pragma warning restore SYSLIB0031
        }

        private static void VerifyCreateFromName<TExpected>(string name)
        {
            object obj = CryptoConfig.CreateFromName(name);
            Assert.NotNull(obj);
            Assert.IsType<TExpected>(obj);
        }

        public class ClassWithCtorArguments
        {
            public ClassWithCtorArguments(string s)
            {
                MyString = s;
            }

            public string MyString;
        }
    }
}
