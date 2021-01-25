// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.EcDiffieHellman.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.OpenSsl.Tests
{
    public class EcDiffieHellmanOpenSslTests : ECDiffieHellmanTests
    {
        [Fact]
        public void DefaultCtor()
        {
            using (var e = new ECDiffieHellmanOpenSsl())
            {
                int keySize = e.KeySize;
                Assert.Equal(521, keySize);
                e.Exercise();
            }
        }

        [Fact]
        public void Ctor256()
        {
            int expectedKeySize = 256;
            using (var e = new ECDiffieHellmanOpenSsl(expectedKeySize))
            {
                int keySize = e.KeySize;
                Assert.Equal(expectedKeySize, keySize);
                e.Exercise();
            }
        }

        [Fact]
        public void Ctor384()
        {
            int expectedKeySize = 384;
            using (var e = new ECDiffieHellmanOpenSsl(expectedKeySize))
            {
                int keySize = e.KeySize;
                Assert.Equal(expectedKeySize, keySize);
                e.Exercise();
            }
        }

        [Fact]
        public void Ctor521()
        {
            int expectedKeySize = 521;
            using (var e = new ECDiffieHellmanOpenSsl(expectedKeySize))
            {
                int keySize = e.KeySize;
                Assert.Equal(expectedKeySize, keySize);
                e.Exercise();
            }
        }

        [ConditionalFact(nameof(ECDsa224Available))]
        public void CtorHandle224()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(ECDSA_P224_OID_VALUE);
            Assert.NotEqual(IntPtr.Zero, ecKey);
            int success = Interop.Crypto.EcKeyGenerateKey(ecKey);
            Assert.NotEqual(0, success);

            using (var e = new ECDiffieHellmanOpenSsl(ecKey))
            {
                int keySize = e.KeySize;
                Assert.Equal(224, keySize);
                e.Exercise();
            }

            Interop.Crypto.EcKeyDestroy(ecKey);
        }

        [Fact]
        public void CtorHandle384()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(ECDSA_P384_OID_VALUE);
            Assert.NotEqual(IntPtr.Zero, ecKey);
            int success = Interop.Crypto.EcKeyGenerateKey(ecKey);
            Assert.NotEqual(0, success);

            using (var e = new ECDiffieHellmanOpenSsl(ecKey))
            {
                int keySize = e.KeySize;
                Assert.Equal(384, keySize);
                e.Exercise();
            }

            Interop.Crypto.EcKeyDestroy(ecKey);
        }

        [Fact]
        public void CtorHandle521()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(ECDSA_P521_OID_VALUE);
            Assert.NotEqual(IntPtr.Zero, ecKey);
            int success = Interop.Crypto.EcKeyGenerateKey(ecKey);
            Assert.NotEqual(0, success);

            using (var e = new ECDiffieHellmanOpenSsl(ecKey))
            {
                int keySize = e.KeySize;
                Assert.Equal(521, keySize);
                e.Exercise();
            }

            Interop.Crypto.EcKeyDestroy(ecKey);
        }

        [Fact]
        public void CtorHandleDuplicate()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(ECDSA_P521_OID_VALUE);
            Assert.NotEqual(IntPtr.Zero, ecKey);
            int success = Interop.Crypto.EcKeyGenerateKey(ecKey);
            Assert.NotEqual(0, success);

            using (var e = new ECDiffieHellmanOpenSsl(ecKey))
            {
                // Make sure ECDsaOpenSsl did its own ref-count bump.
                Interop.Crypto.EcKeyDestroy(ecKey);

                int keySize = e.KeySize;
                Assert.Equal(521, keySize);
                e.Exercise();
            }
        }

        [Fact]
        public void KeySizePropWithExercise()
        {
            using (var e = new ECDiffieHellmanOpenSsl())
            {
                e.KeySize = 384;
                Assert.Equal(384, e.KeySize);
                e.Exercise();
                ECParameters p384 = e.ExportParameters(false);
                Assert.Equal(ECCurve.ECCurveType.Named, p384.Curve.CurveType);

                e.KeySize = 521;
                Assert.Equal(521, e.KeySize);
                e.Exercise();
                ECParameters p521 = e.ExportParameters(false);
                Assert.Equal(ECCurve.ECCurveType.Named, p521.Curve.CurveType);

                // ensure the key was regenerated
                Assert.NotEqual(p384.Curve.Oid.Value, p521.Curve.Oid.Value);
            }
        }

        [Fact]
        public void VerifyDuplicateKey_ValidHandle()
        {
            using (var first = new ECDiffieHellmanOpenSsl())
            using (SafeEvpPKeyHandle firstHandle = first.DuplicateKeyHandle())
            using (ECDiffieHellman second = new ECDiffieHellmanOpenSsl(firstHandle))
            using (ECDiffieHellmanPublicKey firstPublic = first.PublicKey)
            using (ECDiffieHellmanPublicKey secondPublic = second.PublicKey)
            {
                byte[] firstSecond = first.DeriveKeyFromHash(secondPublic, HashAlgorithmName.SHA256);
                byte[] secondFirst = second.DeriveKeyFromHash(firstPublic, HashAlgorithmName.SHA256);
                byte[] firstFirst = first.DeriveKeyFromHash(firstPublic, HashAlgorithmName.SHA256);

                Assert.Equal(firstSecond, secondFirst);
                Assert.Equal(firstFirst, firstSecond);
            }
        }

        [Fact]
        public void VerifyDuplicateKey_DistinctHandles()
        {
            using (var first = new ECDiffieHellmanOpenSsl())
            using (SafeEvpPKeyHandle firstHandle = first.DuplicateKeyHandle())
            using (SafeEvpPKeyHandle firstHandle2 = first.DuplicateKeyHandle())
            {
                Assert.NotSame(firstHandle, firstHandle2);
            }
        }

        [Fact]
        public void VerifyDuplicateKey_RefCounts()
        {
            byte[] derived;
            ECDiffieHellman second;

            using (var first = new ECDiffieHellmanOpenSsl())
            using (SafeEvpPKeyHandle firstHandle = first.DuplicateKeyHandle())
            using (ECDiffieHellmanPublicKey firstPublic = first.PublicKey)
            {
                derived = first.DeriveKeyFromHmac(firstPublic, HashAlgorithmName.SHA384, null);
                second = new ECDiffieHellmanOpenSsl(firstHandle);
            }

            // Now show that second still works, despite first and firstHandle being Disposed.
            using (second)
            using (ECDiffieHellmanPublicKey secondPublic = second.PublicKey)
            {
                byte[] derived2 = second.DeriveKeyFromHmac(secondPublic, HashAlgorithmName.SHA384, null);
                Assert.Equal(derived, derived2);
            }
        }

        [Fact]
        public void VerifyDuplicateKey_NullHandle()
        {
            SafeEvpPKeyHandle pkey = null;
            Assert.Throws<ArgumentNullException>(() => new ECDiffieHellmanOpenSsl(pkey));
        }

        [Fact]
        public void VerifyDuplicateKey_InvalidHandle()
        {
            using (var ecdsa = new ECDiffieHellmanOpenSsl())
            {
                SafeEvpPKeyHandle pkey = ecdsa.DuplicateKeyHandle();

                using (pkey)
                {
                }

                AssertExtensions.Throws<ArgumentException>("pkeyHandle", () => new ECDiffieHellmanOpenSsl(pkey));
            }
        }

        [Fact]
        public void VerifyDuplicateKey_NeverValidHandle()
        {
            using (SafeEvpPKeyHandle pkey = new SafeEvpPKeyHandle(IntPtr.Zero, false))
            {
                AssertExtensions.Throws<ArgumentException>("pkeyHandle", () => new ECDiffieHellmanOpenSsl(pkey));
            }
        }

        [Fact]
        public void VerifyDuplicateKey_RsaHandle()
        {
            using (RSAOpenSsl rsa = new RSAOpenSsl())
            using (SafeEvpPKeyHandle pkey = rsa.DuplicateKeyHandle())
            {
                Assert.ThrowsAny<CryptographicException>(() => new ECDiffieHellmanOpenSsl(pkey));
            }
        }

        [Fact]
        public void LookupCurveByOidValue()
        {
            var ec = new ECDiffieHellmanOpenSsl(ECCurve.CreateFromValue(ECDSA_P256_OID_VALUE)); // Same as nistP256
            ECParameters param = ec.ExportParameters(false);
            param.Validate();
            Assert.Equal(256, ec.KeySize);
            Assert.True(param.Curve.IsNamed);
            Assert.Equal("ECDSA_P256", param.Curve.Oid.FriendlyName);
            Assert.Equal(ECDSA_P256_OID_VALUE, param.Curve.Oid.Value);
        }

        [Fact]
        public void LookupCurveByOidFriendlyName()
        {
            // prime256v1 is alias for nistP256 for OpenSsl
            var ec = new ECDiffieHellmanOpenSsl(ECCurve.CreateFromFriendlyName("prime256v1"));

            ECParameters param = ec.ExportParameters(false);
            param.Validate();
            Assert.Equal(256, ec.KeySize);
            Assert.True(param.Curve.IsNamed);
            Assert.Equal("ECDSA_P256", param.Curve.Oid.FriendlyName); // OpenSsl maps prime256v1 to ECDSA_P256
            Assert.Equal(ECDSA_P256_OID_VALUE, param.Curve.Oid.Value);

            // secp521r1 is same as nistP521; note Windows uses secP521r1 (uppercase P)
            ec = new ECDiffieHellmanOpenSsl(ECCurve.CreateFromFriendlyName("secp521r1"));
            param = ec.ExportParameters(false);
            param.Validate();
            Assert.Equal(521, ec.KeySize);
            Assert.True(param.Curve.IsNamed);
            Assert.Equal("ECDSA_P521", param.Curve.Oid.FriendlyName); // OpenSsl maps secp521r1 to ECDSA_P521
            Assert.Equal(ECDSA_P521_OID_VALUE, param.Curve.Oid.Value);
        }
    }
}
