// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.EcDiffieHellman.Tests;
using System.Security.Cryptography.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.OpenSsl.Tests
{
    public static class EcDiffieHellmanOpenSslTests
    {
        public static bool ECDsa224Available => ECDiffieHellmanOpenSslProvider.Instance.IsCurveValid(new Oid(EccTestBase.ECDSA_P224_OID_VALUE));
        public static bool ECExplicitCurvesSupported => ECDiffieHellmanOpenSslProvider.Instance.ExplicitCurvesSupported;
        public static bool SupportsExplicitCurves => ECDiffieHellmanOpenSslProvider.Instance.ExplicitCurvesSupported || ECDiffieHellmanProvider.ExplicitCurvesSupportFailOnUseOnly;

        [Fact]
        public static void DefaultCtor()
        {
            using (var e = new ECDiffieHellmanOpenSsl())
            {
                int keySize = e.KeySize;
                Assert.Equal(521, keySize);
                e.Exercise();
            }
        }

        [Fact]
        public static void Ctor256()
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
        public static void Ctor384()
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
        public static void Ctor521()
        {
            int expectedKeySize = 521;
            using (var e = new ECDiffieHellmanOpenSsl(expectedKeySize))
            {
                int keySize = e.KeySize;
                Assert.Equal(expectedKeySize, keySize);
                e.Exercise();
            }
        }

        [ConditionalFact(typeof(EcDiffieHellmanOpenSslTests), nameof(ECDsa224Available))]
        public static void CtorHandle224()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(EccTestBase.ECDSA_P224_OID_VALUE);
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
        public static void CtorHandle384()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(EccTestBase.ECDSA_P384_OID_VALUE);
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
        public static void CtorHandle521()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(EccTestBase.ECDSA_P521_OID_VALUE);
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
        public static void CtorHandleDuplicate()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(EccTestBase.ECDSA_P521_OID_VALUE);
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

        [Theory]
        [InlineData(EccTestBase.ECDSA_P256_OID_VALUE, 256)]
        [InlineData(EccTestBase.ECDSA_P384_OID_VALUE, 384)]
        [InlineData(EccTestBase.ECDSA_P521_OID_VALUE, 521)]
        public static void CtorEvpPKeyHandle(string oid, int expectedKeySize)
        {
            int rc = Interop.Crypto.EvpPKeyGenerateByEcCurveOid(out SafeEvpPKeyHandle pkey, oid, out int keySize);

            Assert.Equal(1, rc);
            Assert.False(pkey.IsInvalid);
            Assert.Equal(expectedKeySize, keySize);

            using (pkey)
            using (ECDiffieHellmanOpenSsl e = new ECDiffieHellmanOpenSsl(pkey))
            {
                Assert.Equal(expectedKeySize, e.KeySize);
                e.Exercise();
            }
        }

        [Fact]
        public static void KeySizePropWithExercise()
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
        public static void VerifyDuplicateKey_ValidHandle()
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
        public static void VerifyDuplicateKey_DistinctHandles()
        {
            using (var first = new ECDiffieHellmanOpenSsl())
            using (SafeEvpPKeyHandle firstHandle = first.DuplicateKeyHandle())
            using (SafeEvpPKeyHandle firstHandle2 = first.DuplicateKeyHandle())
            {
                Assert.NotSame(firstHandle, firstHandle2);
            }
        }

        [Fact]
        public static void VerifyDuplicateKey_RefCounts()
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
        public static void VerifyDuplicateKey_NullHandle()
        {
            SafeEvpPKeyHandle pkey = null;
            Assert.Throws<ArgumentNullException>(() => new ECDiffieHellmanOpenSsl(pkey));
        }

        [Fact]
        public static void VerifyDuplicateKey_InvalidHandle()
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
        public static void VerifyDuplicateKey_NeverValidHandle()
        {
            using (SafeEvpPKeyHandle pkey = new SafeEvpPKeyHandle(IntPtr.Zero, false))
            {
                AssertExtensions.Throws<ArgumentException>("pkeyHandle", () => new ECDiffieHellmanOpenSsl(pkey));
            }
        }

        [Fact]
        public static void VerifyDuplicateKey_RsaHandle()
        {
            using (RSAOpenSsl rsa = new RSAOpenSsl())
            using (SafeEvpPKeyHandle pkey = rsa.DuplicateKeyHandle())
            {
                Assert.ThrowsAny<CryptographicException>(() => new ECDiffieHellmanOpenSsl(pkey));
            }
        }

        [Fact]
        public static void LookupCurveByOidValue()
        {
            var ec = new ECDiffieHellmanOpenSsl(ECCurve.CreateFromValue(EccTestBase.ECDSA_P256_OID_VALUE)); // Same as nistP256
            ECParameters param = ec.ExportParameters(false);
            param.Validate();
            Assert.Equal(256, ec.KeySize);
            Assert.True(param.Curve.IsNamed);
            Assert.Equal("ECDSA_P256", param.Curve.Oid.FriendlyName);
            Assert.Equal(EccTestBase.ECDSA_P256_OID_VALUE, param.Curve.Oid.Value);
        }

        [Fact]
        public static void LookupCurveByOidFriendlyName()
        {
            // prime256v1 is alias for nistP256 for OpenSsl
            var ec = new ECDiffieHellmanOpenSsl(ECCurve.CreateFromFriendlyName("prime256v1"));

            ECParameters param = ec.ExportParameters(false);
            param.Validate();
            Assert.Equal(256, ec.KeySize);
            Assert.True(param.Curve.IsNamed);
            Assert.Equal("ECDSA_P256", param.Curve.Oid.FriendlyName); // OpenSsl maps prime256v1 to ECDSA_P256
            Assert.Equal(EccTestBase.ECDSA_P256_OID_VALUE, param.Curve.Oid.Value);

            // secp521r1 is same as nistP521; note Windows uses secP521r1 (uppercase P)
            ec = new ECDiffieHellmanOpenSsl(ECCurve.CreateFromFriendlyName("secp521r1"));
            param = ec.ExportParameters(false);
            param.Validate();
            Assert.Equal(521, ec.KeySize);
            Assert.True(param.Curve.IsNamed);
            Assert.Equal("ECDSA_P521", param.Curve.Oid.FriendlyName); // OpenSsl maps secp521r1 to ECDSA_P521
            Assert.Equal(EccTestBase.ECDSA_P521_OID_VALUE, param.Curve.Oid.Value);
        }

        [Theory]
        [InlineData(EccTestBase.ECDSA_P256_OID_VALUE, 256)]
        [InlineData(EccTestBase.ECDSA_P384_OID_VALUE, 384)]
        [InlineData(EccTestBase.ECDSA_P521_OID_VALUE, 521)]
        public static void CtorEcKeyExportMatchesReimport(string oid, int expectedKeySize)
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(oid);
            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                Assert.NotEqual(0, Interop.Crypto.EcKeyGenerateKey(ecKey));

                using (ECDiffieHellmanOpenSsl ecKeyBacked = new ECDiffieHellmanOpenSsl(ecKey))
                {
                    Assert.Equal(expectedKeySize, ecKeyBacked.KeySize);

                    ECParameters privateParams = ecKeyBacked.ExportParameters(true);

                    using (ECDiffieHellman evpBacked = ECDiffieHellman.Create(privateParams))
                    {
                        Assert.Equal(expectedKeySize, evpBacked.KeySize);

                        ECParameters evpPrivateParams = evpBacked.ExportParameters(true);

                        EccTestBase.ComparePublicKey(privateParams.Q, evpPrivateParams.Q);
                        EccTestBase.ComparePrivateKey(privateParams, evpPrivateParams);
                    }
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [Theory]
        [InlineData(EccTestBase.ECDSA_P256_OID_VALUE)]
        [InlineData(EccTestBase.ECDSA_P384_OID_VALUE)]
        [InlineData(EccTestBase.ECDSA_P521_OID_VALUE)]
        public static void CtorEcKeyDeriveCrossCompatibleWithReimport(string oid)
        {
            IntPtr rawKey1 = Interop.Crypto.EcKeyCreateByOid(oid);
            Assert.NotEqual(IntPtr.Zero, rawKey1);
            IntPtr rawKey2 = Interop.Crypto.EcKeyCreateByOid(oid);
            Assert.NotEqual(IntPtr.Zero, rawKey2);

            try
            {
                Assert.NotEqual(0, Interop.Crypto.EcKeyGenerateKey(rawKey1));
                Assert.NotEqual(0, Interop.Crypto.EcKeyGenerateKey(rawKey2));

                using (ECDiffieHellmanOpenSsl ecKeyBacked1 = new ECDiffieHellmanOpenSsl(rawKey1))
                using (ECDiffieHellmanOpenSsl ecKeyBacked2 = new ECDiffieHellmanOpenSsl(rawKey2))
                using (ECDiffieHellman evpBacked1 = ECDiffieHellman.Create(ecKeyBacked1.ExportParameters(true)))
                using (ECDiffieHellman evpBacked2 = ECDiffieHellman.Create(ecKeyBacked2.ExportParameters(true)))
                using (ECDiffieHellmanPublicKey ecPub2 = ecKeyBacked2.PublicKey)
                using (ECDiffieHellmanPublicKey evpPub2 = evpBacked2.PublicKey)
                {
                    byte[] ecEc = ecKeyBacked1.DeriveKeyFromHash(ecPub2, HashAlgorithmName.SHA256);
                    byte[] ecEvp = ecKeyBacked1.DeriveKeyFromHash(evpPub2, HashAlgorithmName.SHA256);
                    byte[] evpEc = evpBacked1.DeriveKeyFromHash(ecPub2, HashAlgorithmName.SHA256);
                    byte[] evpEvp = evpBacked1.DeriveKeyFromHash(evpPub2, HashAlgorithmName.SHA256);

                    Assert.Equal(ecEc, ecEvp);
                    Assert.Equal(ecEc, evpEc);
                    Assert.Equal(ecEc, evpEvp);
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(rawKey1);
                Interop.Crypto.EcKeyDestroy(rawKey2);
            }
        }

        [Fact]
        public static void CtorEcKeyDeriveLeftAndRightSide()
        {
            ECParameters testData = EccTestData.GetNistP256ReferenceKey();

            IntPtr ecKey;
            int rc = Interop.Crypto.EcKeyCreateByKeyParameters(
                out ecKey,
                testData.Curve.Oid.Value!,
                testData.Q.X, testData.Q.X!.Length,
                testData.Q.Y, testData.Q.Y!.Length,
                testData.D, testData.D!.Length);

            Assert.Equal(1, rc);
            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                using (ECDiffieHellmanOpenSsl ecKeyBacked = new ECDiffieHellmanOpenSsl(ecKey))
                using (ECDiffieHellman peer = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
                using (ECDiffieHellmanPublicKey ecKeyPub = ecKeyBacked.PublicKey)
                using (ECDiffieHellmanPublicKey peerPub = peer.PublicKey)
                {
                    byte[] derivedLeft = ecKeyBacked.DeriveKeyFromHash(peerPub, HashAlgorithmName.SHA256);
                    byte[] derivedRight = peer.DeriveKeyFromHash(ecKeyPub, HashAlgorithmName.SHA256);

                    Assert.Equal(derivedLeft, derivedRight);
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [Fact]
        public static void CtorEcKeyPublicOnlyFailsDerive()
        {
            ECParameters testData = EccTestData.GetNistP256ReferenceKey();

            IntPtr ecKey;
            int rc = Interop.Crypto.EcKeyCreateByKeyParameters(
                out ecKey,
                testData.Curve.Oid.Value!,
                testData.Q.X, testData.Q.X!.Length,
                testData.Q.Y, testData.Q.Y!.Length,
                null, 0);

            Assert.Equal(1, rc);
            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                using (ECDiffieHellmanOpenSsl ecKeyBacked = new ECDiffieHellmanOpenSsl(ecKey))
                using (ECDiffieHellman peer = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
                using (ECDiffieHellmanPublicKey peerPub = peer.PublicKey)
                {
                    Assert.ThrowsAny<CryptographicException>(() =>
                        ecKeyBacked.DeriveKeyFromHash(peerPub, HashAlgorithmName.SHA256));
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [ConditionalFact(nameof(SupportsExplicitCurves))]
        public static void ExplicitCurveImportExportProducesSameExplicitParams()
        {
            ECCurve explicitCurve = EccTestData.GetNistP256ExplicitCurve();

            using (ECDiffieHellman original = ECDiffieHellman.Create(explicitCurve))
            {
                ECParameters explicitParams = original.ExportExplicitParameters(true);

                using (ECDiffieHellman reimported = ECDiffieHellman.Create(explicitParams))
                {
                    ECParameters reimportedParams = reimported.ExportExplicitParameters(true);

                    EccTestBase.ComparePublicKey(explicitParams.Q, reimportedParams.Q);
                    EccTestBase.ComparePrivateKey(explicitParams, reimportedParams);
                }
            }
        }

        [ConditionalFact(nameof(ECExplicitCurvesSupported))]
        public static void ExplicitCurveImportAndOriginalDeriveCrossCompatible()
        {
            ECCurve explicitCurve = EccTestData.GetNistP256ExplicitCurve();

            using (ECDiffieHellman key1 = ECDiffieHellman.Create(explicitCurve))
            using (ECDiffieHellman key2 = ECDiffieHellman.Create(explicitCurve))
            {
                ECParameters key1Params = key1.ExportExplicitParameters(true);
                ECParameters key2Params = key2.ExportExplicitParameters(true);

                using (ECDiffieHellman key1Reimported = ECDiffieHellman.Create(key1Params))
                using (ECDiffieHellman key2Reimported = ECDiffieHellman.Create(key2Params))
                using (ECDiffieHellmanPublicKey pub2 = key2.PublicKey)
                using (ECDiffieHellmanPublicKey pub2Reimported = key2Reimported.PublicKey)
                {
                    byte[] derive1 = key1.DeriveKeyFromHash(pub2, HashAlgorithmName.SHA256);
                    byte[] derive2 = key1.DeriveKeyFromHash(pub2Reimported, HashAlgorithmName.SHA256);
                    byte[] derive3 = key1Reimported.DeriveKeyFromHash(pub2, HashAlgorithmName.SHA256);
                    byte[] derive4 = key1Reimported.DeriveKeyFromHash(pub2Reimported, HashAlgorithmName.SHA256);

                    Assert.Equal(derive1, derive2);
                    Assert.Equal(derive1, derive3);
                    Assert.Equal(derive1, derive4);
                }
            }
        }

        [Fact]
        public static void GenerateKeyImplicitCurveThrows()
        {
            ECCurve implicitCurve = default;

            using (ECDiffieHellman ecdh = new ECDiffieHellmanOpenSsl())
            {
                Assert.Throws<PlatformNotSupportedException>(() => ecdh.GenerateKey(implicitCurve));
            }
        }

        [Fact]
        public static void ImportParametersImplicitCurveThrows()
        {
            ECParameters parameters = new ECParameters
            {
                Curve = default,
                D = new byte[32],
            };

            using (ECDiffieHellman ecdh = new ECDiffieHellmanOpenSsl())
            {
                Assert.Throws<PlatformNotSupportedException>(() => ecdh.ImportParameters(parameters));
            }
        }
    }
}
