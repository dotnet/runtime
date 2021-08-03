// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.CryptoConfigTests
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
        [Theory]
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

        [Theory]
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

        [Theory]
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

        [Theory]
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

        [Theory]
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

        [Fact]
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
    }
}
