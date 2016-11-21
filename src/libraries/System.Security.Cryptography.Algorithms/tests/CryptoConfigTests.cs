// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Security.Cryptography.CryptoConfigTests
{
    public static class CryptoConfigTests
    {
        [Fact]
        public static void AllowOnlyFipsAlgorithms()
        {
            Assert.False(CryptoConfig.AllowOnlyFipsAlgorithms);
        }

        [Fact]
        public static void AddOID_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => CryptoConfig.AddOID(string.Empty, string.Empty));
        }

        [Fact]
        public static void AddAlgorithm_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => CryptoConfig.AddAlgorithm(typeof(CryptoConfigTests), string.Empty));
        }

        [Fact]
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
            VerifyStaticCreateResult(Rijndael.Create(typeof(RijndaelManaged).FullName), typeof(RijndaelManaged));
            Assert.Null(RSA.Create(string.Empty));
            Assert.Null(SHA1.Create(string.Empty));
            VerifyStaticCreateResult(SHA256.Create(typeof(SHA256Managed).FullName), typeof(SHA256Managed));
            VerifyStaticCreateResult(SHA384.Create(typeof(SHA384Managed).FullName), typeof(SHA384Managed));
            VerifyStaticCreateResult(SHA512.Create(typeof(SHA512Managed).FullName), typeof(SHA512Managed));
        }

        private static void VerifyStaticCreateResult(object obj, Type expectedType)
        {
           Assert.NotNull(obj);
           Assert.IsType(expectedType, obj);
        }

        [Fact]
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
            Assert.Equal(null, CryptoConfig.MapNameToOID("NOT_A_VALID_OID"));
        }

        [Fact]
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
                // Random number generator
                yield return new object[] { "RandomNumberGenerator", "System.Security.Cryptography.RNGCryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.RandomNumberGenerator", "System.Security.Cryptography.RNGCryptoServiceProvider", false };

                // Hash functions
                yield return new object[] { "SHA", "System.Security.Cryptography.SHA1CryptoServiceProvider", false };
                yield return new object[] { "SHA1", "System.Security.Cryptography.SHA1CryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.SHA1", "System.Security.Cryptography.SHA1CryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.HashAlgorithm", "System.Security.Cryptography.SHA1CryptoServiceProvider", false };
                yield return new object[] { "MD5", "System.Security.Cryptography.MD5CryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.MD5", "System.Security.Cryptography.MD5CryptoServiceProvider", false };
                yield return new object[] { "SHA256", typeof(SHA256Managed).FullName, true };
                yield return new object[] { "SHA-256", typeof(SHA256Managed).FullName, true };
                yield return new object[] { "System.Security.Cryptography.SHA256", typeof(SHA256Managed).FullName, true };
                yield return new object[] { "SHA384", typeof(SHA384Managed).FullName, true };
                yield return new object[] { "SHA-384", typeof(SHA384Managed).FullName, true };
                yield return new object[] { "System.Security.Cryptography.SHA384", typeof(SHA384Managed).FullName, true };
                yield return new object[] { "SHA512", typeof(SHA512Managed).FullName, true };
                yield return new object[] { "SHA-512", typeof(SHA512Managed).FullName, true };
                yield return new object[] { "System.Security.Cryptography.SHA512", typeof(SHA512Managed).FullName, true };

                // Keyed Hash Algorithms
                yield return new object[] { "System.Security.Cryptography.HMAC", "System.Security.Cryptography.HMACSHA1", true };
                yield return new object[] { "System.Security.Cryptography.KeyedHashAlgorithm", "System.Security.Cryptography.HMACSHA1", true };
                yield return new object[] { "HMACMD5", "System.Security.Cryptography.HMACMD5", true };
                yield return new object[] { "System.Security.Cryptography.HMACMD5", null , true };
                yield return new object[] { "HMACSHA1", "System.Security.Cryptography.HMACSHA1", true };
                yield return new object[] { "System.Security.Cryptography.HMACSHA1", null , true };
                yield return new object[] { "HMACSHA256", "System.Security.Cryptography.HMACSHA256", true };
                yield return new object[] { "System.Security.Cryptography.HMACSHA256", null, true };
                yield return new object[] { "HMACSHA384", "System.Security.Cryptography.HMACSHA384", true };
                yield return new object[] { "System.Security.Cryptography.HMACSHA384", null , true };
                yield return new object[] { "HMACSHA512", "System.Security.Cryptography.HMACSHA512", true };
                yield return new object[] { "System.Security.Cryptography.HMACSHA512", null , true };

                // Asymmetric algorithms
                yield return new object[] { "RSA", "System.Security.Cryptography.RSACryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.RSA", "System.Security.Cryptography.RSACryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.AsymmetricAlgorithm", "System.Security.Cryptography.RSACryptoServiceProvider", false };
                yield return new object[] { "DSA", "System.Security.Cryptography.DSACryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.DSA", "System.Security.Cryptography.DSACryptoServiceProvider", false };
                yield return new object[] { "ECDsa", "System.Security.Cryptography.ECDsaCng", false };
                yield return new object[] { "ECDsaCng", "System.Security.Cryptography.ECDsaCng", false };
                yield return new object[] { "System.Security.Cryptography.ECDsaCng", null , false };
                yield return new object[] { "DES", "System.Security.Cryptography.DESCryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.DES", "System.Security.Cryptography.DESCryptoServiceProvider", false };
                yield return new object[] { "3DES", "System.Security.Cryptography.TripleDESCryptoServiceProvider", false };
                yield return new object[] { "TripleDES", "System.Security.Cryptography.TripleDESCryptoServiceProvider", false };
                yield return new object[] { "Triple DES", "System.Security.Cryptography.TripleDESCryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.TripleDES", "System.Security.Cryptography.TripleDESCryptoServiceProvider", false };
                yield return new object[] { "RC2", "System.Security.Cryptography.RC2CryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.RC2", "System.Security.Cryptography.RC2CryptoServiceProvider", false };
                yield return new object[] { "Rijndael", typeof(RijndaelManaged).FullName, true };
                yield return new object[] { "System.Security.Cryptography.Rijndael", typeof(RijndaelManaged).FullName, true };
                yield return new object[] { "System.Security.Cryptography.SymmetricAlgorithm", typeof(RijndaelManaged).FullName, true };
                yield return new object[] { "AES", "System.Security.Cryptography.AesCryptoServiceProvider", false };
                yield return new object[] { "AesCryptoServiceProvider", "System.Security.Cryptography.AesCryptoServiceProvider", false };
                yield return new object[] { "System.Security.Cryptography.AesCryptoServiceProvider", "System.Security.Cryptography.AesCryptoServiceProvider", false };
                yield return new object[] { "AesManaged", typeof(AesManaged).FullName, true };
                yield return new object[] { "System.Security.Cryptography.AesManaged", typeof(AesManaged).FullName, true };

                // Xml Dsig/ Enc Hash algorithms
                yield return new object[] { "http://www.w3.org/2000/09/xmldsig#sha1", "System.Security.Cryptography.SHA1CryptoServiceProvider", false };
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#sha256", typeof(SHA256Managed).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#sha512", typeof(SHA512Managed).FullName, true };

                // Xml Encryption symmetric keys
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#des-cbc", "System.Security.Cryptography.DESCryptoServiceProvider", false };
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#tripledes-cbc", "System.Security.Cryptography.TripleDESCryptoServiceProvider", false };
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#kw-tripledes", "System.Security.Cryptography.TripleDESCryptoServiceProvider", false };
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#aes128-cbc", typeof(RijndaelManaged).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#kw-aes128", typeof(RijndaelManaged).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#aes192-cbc", typeof(RijndaelManaged).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#kw-aes192", typeof(RijndaelManaged).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#aes256-cbc", typeof(RijndaelManaged).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmlenc#kw-aes256", typeof(RijndaelManaged).FullName, true };

                // Xml Dsig HMAC URIs from http://www.w3.org/TR/xmldsig-core/
                yield return new object[] { "http://www.w3.org/2000/09/xmldsig#hmac-sha1", typeof(HMACSHA1).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmldsig-more#sha384", typeof(SHA384Managed).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmldsig-more#hmac-md5", typeof(HMACMD5).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", typeof(HMACSHA256).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmldsig-more#hmac-sha384", typeof(HMACSHA384).FullName, true };
                yield return new object[] { "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512", typeof(HMACSHA512).FullName, true };

                // X509
                yield return new object[] { "2.5.29.10", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension", true };
                yield return new object[] { "2.5.29.19", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension", true };
                yield return new object[] { "2.5.29.14", "System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension", true };
                yield return new object[] { "2.5.29.15", "System.Security.Cryptography.X509Certificates.X509KeyUsageExtension", true };
                yield return new object[] { "2.5.29.37", "System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension", true };
                yield return new object[] { "X509Chain", "System.Security.Cryptography.X509Certificates.X509Chain", true };

                // PKCS9 attributes
                yield return new object[] { "1.2.840.113549.1.9.3", "System.Security.Cryptography.Pkcs.Pkcs9ContentType", false };
                yield return new object[] { "1.2.840.113549.1.9.4", "System.Security.Cryptography.Pkcs.Pkcs9MessageDigest", false };
                yield return new object[] { "1.2.840.113549.1.9.5", "System.Security.Cryptography.Pkcs.Pkcs9SigningTime", false };
                yield return new object[] { "1.3.6.1.4.1.311.88.2.1", "System.Security.Cryptography.Pkcs.Pkcs9DocumentName", false };
                yield return new object[] { "1.3.6.1.4.1.311.88.2.2", "System.Security.Cryptography.Pkcs.Pkcs9DocumentDescription", false };
            }
        }


        [Theory, MemberData(nameof(AllValidNames))]
        public static void CreateFromName_AllValidNames(string name, string typeName, bool supportsUnixMac)
        {
            if (supportsUnixMac || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                object obj = CryptoConfig.CreateFromName(name);
                Assert.NotNull(obj);

                if (typeName == null)
                {
                    typeName = name;
                }

                Assert.Equal(typeName, obj.GetType().FullName);

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
        public static void CreateFromName_CtorArguments()
        {
            string className = typeof(ClassWithCtorArguments).FullName + ", System.Security.Cryptography.Algorithms.Tests";

            // Pass int instead of string
            Assert.Throws<MissingMethodException>(() => CryptoConfig.CreateFromName(className, 1));

            // Valid case
            object obj = CryptoConfig.CreateFromName(className, "Hello");
            Assert.NotNull(obj);
            Assert.IsType(typeof(ClassWithCtorArguments), obj);

            ClassWithCtorArguments ctorObj = (ClassWithCtorArguments)obj;
            Assert.Equal("Hello", ctorObj.MyString);
        }

        private static void VerifyCreateFromName<TExpected>(string name)
        {
            object obj = CryptoConfig.CreateFromName(name);
            Assert.NotNull(obj);
            Assert.IsType(typeof(TExpected), obj);
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
