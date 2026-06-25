// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.EcDsa.Tests;
using System.Security.Cryptography.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDsa.OpenSsl.Tests
{
    public class EcDsaOpenSslTests : ECDsaTestsBase
    {
        public static bool SupportsExplicitCurves => ECDsaFactory.ExplicitCurvesSupported || ECDsaFactory.ExplicitCurvesSupportFailOnUseOnly;

        [Fact]
        public void DefaultCtor()
        {
            using (ECDsaOpenSsl e = new ECDsaOpenSsl())
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
            using (ECDsaOpenSsl e = new ECDsaOpenSsl(expectedKeySize))
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
            using (ECDsaOpenSsl e = new ECDsaOpenSsl(expectedKeySize))
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
            using (ECDsaOpenSsl e = new ECDsaOpenSsl(expectedKeySize))
            {
                int keySize = e.KeySize;
                Assert.Equal(expectedKeySize, keySize);
                e.Exercise();
            }
        }

        [ConditionalFact(typeof(EcDsaOpenSslTests), nameof(ECDsa224Available))]
        public void CtorHandle224()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(ECDSA_P224_OID_VALUE);
            Assert.NotEqual(IntPtr.Zero, ecKey);
            int success = Interop.Crypto.EcKeyGenerateKey(ecKey);
            Assert.NotEqual(0, success);

            using (ECDsaOpenSsl e = new ECDsaOpenSsl(ecKey))
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

            using (ECDsaOpenSsl e = new ECDsaOpenSsl(ecKey))
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

            using (ECDsaOpenSsl e = new ECDsaOpenSsl(ecKey))
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

            using (ECDsaOpenSsl e = new ECDsaOpenSsl(ecKey))
            {
                // Make sure ECDsaOpenSsl did its own ref-count bump.
                Interop.Crypto.EcKeyDestroy(ecKey);

                int keySize = e.KeySize;
                Assert.Equal(521, keySize);
                e.Exercise();
            }
        }

        [Theory]
        [InlineData(ECDSA_P256_OID_VALUE, 256)]
        [InlineData(ECDSA_P384_OID_VALUE, 384)]
        [InlineData(ECDSA_P521_OID_VALUE, 521)]
        public void CtorEvpPKeyHandle(string oid, int expectedKeySize)
        {
            int rc = Interop.Crypto.EvpPKeyGenerateByEcCurveOid(out SafeEvpPKeyHandle pkey, oid, out int keySize);

            Assert.Equal(1, rc);
            Assert.False(pkey.IsInvalid);
            Assert.Equal(expectedKeySize, keySize);

            using (pkey)
            using (ECDsaOpenSsl e = new ECDsaOpenSsl(pkey))
            {
                Assert.Equal(expectedKeySize, e.KeySize);
                e.Exercise();
            }
        }

        [Fact]
        public void KeySizePropWithExercise()
        {
            using (ECDsaOpenSsl e = new ECDsaOpenSsl())
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
            byte[] data = ByteUtils.RepeatByte(0x71, 11);

            using (ECDsaOpenSsl first = new ECDsaOpenSsl())
            using (SafeEvpPKeyHandle firstHandle = first.DuplicateKeyHandle())
            {
                using (ECDsa second = new ECDsaOpenSsl(firstHandle))
                {
                    byte[] signed = second.SignData(data, HashAlgorithmName.SHA512);
                    Assert.True(first.VerifyData(data, signed, HashAlgorithmName.SHA512));
                }
            }
        }

        [Fact]
        public void VerifyDuplicateKey_DistinctHandles()
        {
            using (ECDsaOpenSsl first = new ECDsaOpenSsl())
            using (SafeEvpPKeyHandle firstHandle = first.DuplicateKeyHandle())
            using (SafeEvpPKeyHandle firstHandle2 = first.DuplicateKeyHandle())
            {
                Assert.NotSame(firstHandle, firstHandle2);
            }
        }

        [Fact]
        public void VerifyDuplicateKey_RefCounts()
        {
            byte[] data = ByteUtils.RepeatByte(0x74, 11);
            byte[] signature;
            ECDsa second;

            using (ECDsaOpenSsl first = new ECDsaOpenSsl())
            using (SafeEvpPKeyHandle firstHandle = first.DuplicateKeyHandle())
            {
                signature = first.SignData(data, HashAlgorithmName.SHA384);
                second = new ECDsaOpenSsl(firstHandle);
            }

            // Now show that second still works, despite first and firstHandle being Disposed.
            using (second)
            {
                Assert.True(second.VerifyData(data, signature, HashAlgorithmName.SHA384));
            }
        }

        [Fact]
        public void VerifyDuplicateKey_NullHandle()
        {
            SafeEvpPKeyHandle pkey = null;
            Assert.Throws<ArgumentNullException>(() => new ECDsaOpenSsl(pkey));
        }

        [Fact]
        public void VerifyDuplicateKey_InvalidHandle()
        {
            using (ECDsaOpenSsl ecdsa = new ECDsaOpenSsl())
            {
                SafeEvpPKeyHandle pkey = ecdsa.DuplicateKeyHandle();

                using (pkey)
                {
                }

                AssertExtensions.Throws<ArgumentException>("pkeyHandle", () => new ECDsaOpenSsl(pkey));
            }
        }

        [Fact]
        public void VerifyDuplicateKey_NeverValidHandle()
        {
            using (SafeEvpPKeyHandle pkey = new SafeEvpPKeyHandle(IntPtr.Zero, false))
            {
                AssertExtensions.Throws<ArgumentException>("pkeyHandle", () => new ECDsaOpenSsl(pkey));
            }
        }

        [Fact]
        public void VerifyDuplicateKey_RsaHandle()
        {
            using (RSAOpenSsl rsa = new RSAOpenSsl())
            using (SafeEvpPKeyHandle pkey = rsa.DuplicateKeyHandle())
            {
                Assert.ThrowsAny<CryptographicException>(() => new ECDsaOpenSsl(pkey));
            }
        }

        [Fact]
        public void LookupCurveByOidValue()
        {
            ECDsaOpenSsl ec = null;
            ec = new ECDsaOpenSsl(ECCurve.CreateFromValue(ECDSA_P256_OID_VALUE)); // Same as nistP256
            ECParameters param = ec.ExportParameters(false);
            param.Validate();
            Assert.Equal(256, ec.KeySize);
            Assert.True(param.Curve.IsNamed);
            Assert.Equal("ECDSA_P256", param.Curve.Oid.FriendlyName);
            Assert.Equal(ECDSA_P256_OID_VALUE, param.Curve.Oid.Value);
        }

        [Theory]
        [InlineData("ECDSA_P521")]
        [InlineData("ECDSA_P384")]
        [InlineData("ECDSA_P256")]
        public void LookupCurveByOidWindowsFriendlyName(string friendlyName)
        {
            ECDsaOpenSsl ec = new ECDsaOpenSsl(ECCurve.CreateFromFriendlyName(friendlyName));
            ECParameters param = ec.ExportParameters(false);
            param.Validate();
        }

        [Fact]
        public void LookupCurveByOidWithInvalidThrowsPlatformNotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => {
                new ECDsaOpenSsl(ECCurve.CreateFromFriendlyName("Invalid"));
            });
        }

        [Fact]
        public void LookupCurveByOidFriendlyName()
        {
            ECDsaOpenSsl ec = null;

            // prime256v1 is alias for nistP256 for OpenSsl
            ec = new ECDsaOpenSsl(ECCurve.CreateFromFriendlyName("prime256v1"));
            ECParameters param = ec.ExportParameters(false);
            param.Validate();
            Assert.Equal(256, ec.KeySize);
            Assert.True(param.Curve.IsNamed);
            Assert.Equal("ECDSA_P256", param.Curve.Oid.FriendlyName); // OpenSsl maps prime256v1 to ECDSA_P256
            Assert.Equal(ECDSA_P256_OID_VALUE, param.Curve.Oid.Value);

            // secp521r1 is same as nistP521; note Windows uses secP521r1 (uppercase P)
            ec = new ECDsaOpenSsl(ECCurve.CreateFromFriendlyName("secp521r1"));
            param = ec.ExportParameters(false);
            param.Validate();
            Assert.Equal(521, ec.KeySize);
            Assert.True(param.Curve.IsNamed);
            Assert.Equal("ECDSA_P521", param.Curve.Oid.FriendlyName); // OpenSsl maps secp521r1 to ECDSA_P521
            Assert.Equal(ECDSA_P521_OID_VALUE, param.Curve.Oid.Value);
        }

        [Theory]
        [InlineData(ECDSA_P256_OID_VALUE, 256)]
        [InlineData(ECDSA_P384_OID_VALUE, 384)]
        [InlineData(ECDSA_P521_OID_VALUE, 521)]
        public void CtorEcKeyExportMatchesReimport(string oid, int expectedKeySize)
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(oid);
            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                Assert.NotEqual(0, Interop.Crypto.EcKeyGenerateKey(ecKey));

                using (ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey))
                {
                    Assert.Equal(expectedKeySize, ecKeyBacked.KeySize);

                    ECParameters privateParams = ecKeyBacked.ExportParameters(true);

                    using (ECDsa evpBacked = ECDsa.Create(privateParams))
                    {
                        Assert.Equal(expectedKeySize, evpBacked.KeySize);

                        ECParameters evpPrivateParams = evpBacked.ExportParameters(true);

                        ComparePublicKey(privateParams.Q, evpPrivateParams.Q);
                        ComparePrivateKey(privateParams, evpPrivateParams);
                    }
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [Theory]
        [InlineData(ECDSA_P256_OID_VALUE)]
        [InlineData(ECDSA_P384_OID_VALUE)]
        [InlineData(ECDSA_P521_OID_VALUE)]
        public void CtorEcKeySignVerifyCrossCompatible(string oid)
        {
            byte[] data = ByteUtils.RepeatByte(0x42, 64);

            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(oid);
            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                Assert.NotEqual(0, Interop.Crypto.EcKeyGenerateKey(ecKey));

                using (ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey))
                using (ECDsa evpBacked = ECDsa.Create(ecKeyBacked.ExportParameters(true)))
                {
                    byte[] sig1 = ecKeyBacked.SignData(data, HashAlgorithmName.SHA256);
                    Assert.True(evpBacked.VerifyData(data, sig1, HashAlgorithmName.SHA256));

                    byte[] sig2 = evpBacked.SignData(data, HashAlgorithmName.SHA256);
                    Assert.True(ecKeyBacked.VerifyData(data, sig2, HashAlgorithmName.SHA256));
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        private const string ECDSA_WTLS7_OID_VALUE = "2.23.43.1.4.7";

        private static bool IsWtls7Available => ECDsaFactory.IsCurveValid(new Oid(ECDSA_WTLS7_OID_VALUE));

        [Theory]
        [InlineData(ECDSA_P256_OID_VALUE, 256)]
        [InlineData(ECDSA_P384_OID_VALUE, 384)]
        [InlineData(ECDSA_P521_OID_VALUE, 521)]
        public void CtorEcKeyNamedCurveExportIsCorrect(string oid, int expectedKeySize)
        {
            VerifyCtorEcKeyNamedCurveExport(oid, expectedKeySize);
        }

        [ConditionalFact(nameof(IsWtls7Available))]
        public void CtorEcKeyFieldDegreeNotEqualOrderBits()
        {
            // wap-wsg-idm-ecid-wtls7 has field=160 bits but order=161 bits.
            // Verify KeySize uses field degree (160), not EVP_PKEY_bits (161).
            VerifyCtorEcKeyNamedCurveExport(ECDSA_WTLS7_OID_VALUE, 160);
        }

        private static void VerifyCtorEcKeyNamedCurveExport(string oid, int expectedKeySize)
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(oid);
            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                Assert.NotEqual(0, Interop.Crypto.EcKeyGenerateKey(ecKey));

                using (ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey))
                {
                    Assert.Equal(expectedKeySize, ecKeyBacked.KeySize);

                    ECParameters exportedParams = ecKeyBacked.ExportParameters(true);

                    Assert.True(exportedParams.Curve.IsNamed);
                    Assert.Equal(oid, exportedParams.Curve.Oid.Value);
                    Assert.NotNull(exportedParams.Q.X);
                    Assert.NotNull(exportedParams.Q.Y);
                    Assert.NotNull(exportedParams.D);
                    exportedParams.Validate();
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [Fact]
        public void CtorEcKeyNamedCurveImportExportIsCorrect()
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
                using (ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey))
                {
                    Assert.Equal(256, ecKeyBacked.KeySize);

                    ECParameters exported = ecKeyBacked.ExportParameters(true);

                    Assert.True(exported.Curve.IsNamed);
                    Assert.Equal(testData.Curve.Oid.Value, exported.Curve.Oid.Value);

                    ComparePublicKey(testData.Q, exported.Q);
                    ComparePrivateKey(testData, exported);
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [ConditionalFact(nameof(ECExplicitCurvesSupported))]
        public void CtorEcKeyExplicitPrimeCurveExportIsCorrect()
        {
            ECParameters testData = EccTestData.GetNistP256ReferenceKeyExplicit();
            ECCurve curve = testData.Curve;

            IntPtr ecKey = Interop.Crypto.EcKeyCreateByExplicitParameters(
                curve.CurveType,
                testData.Q.X, testData.Q.X!.Length,
                testData.Q.Y, testData.Q.Y!.Length,
                testData.D, testData.D?.Length ?? 0,
                curve.Prime, curve.Prime!.Length,
                curve.A, curve.A!.Length,
                curve.B, curve.B!.Length,
                curve.G.X, curve.G.X!.Length,
                curve.G.Y, curve.G.Y!.Length,
                curve.Order, curve.Order!.Length,
                curve.Cofactor, curve.Cofactor!.Length,
                curve.Seed, curve.Seed?.Length ?? 0);

            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                using (ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey))
                {
                    Assert.Equal(256, ecKeyBacked.KeySize);

                    ECParameters exported = ecKeyBacked.ExportExplicitParameters(true);

                    Assert.True(exported.Curve.IsPrime);
                    Assert.Equal(curve.Prime, exported.Curve.Prime);
                    Assert.Equal(curve.A, exported.Curve.A);
                    Assert.Equal(curve.B, exported.Curve.B);
                    Assert.Equal(curve.G.X, exported.Curve.G.X);
                    Assert.Equal(curve.G.Y, exported.Curve.G.Y);
                    Assert.Equal(curve.Order, exported.Curve.Order);
                    Assert.Equal(curve.Cofactor, exported.Curve.Cofactor);

                    ComparePublicKey(testData.Q, exported.Q);
                    ComparePrivateKey(testData, exported);
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [ConditionalFact(nameof(ECExplicitCurvesSupported))]
        public void CtorEcKeyExplicitChar2CurveExportIsCorrect()
        {
            ECParameters testData = EccTestData.Sect163k1Key1Explicit;

            if (!ECDsaFactory.IsCurveValid(EccTestData.Sect163k1Key1.Curve.Oid))
            {
                return;
            }

            ECCurve curve = testData.Curve;

            IntPtr ecKey = Interop.Crypto.EcKeyCreateByExplicitParameters(
                curve.CurveType,
                testData.Q.X, testData.Q.X!.Length,
                testData.Q.Y, testData.Q.Y!.Length,
                testData.D, testData.D?.Length ?? 0,
                curve.Polynomial, curve.Polynomial!.Length,
                curve.A, curve.A!.Length,
                curve.B, curve.B!.Length,
                curve.G.X, curve.G.X!.Length,
                curve.G.Y, curve.G.Y!.Length,
                curve.Order, curve.Order!.Length,
                curve.Cofactor, curve.Cofactor!.Length,
                null, 0);

            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                using (ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey))
                {
                    Assert.Equal(163, ecKeyBacked.KeySize);

                    ECParameters exported = ecKeyBacked.ExportExplicitParameters(true);

                    Assert.True(exported.Curve.IsCharacteristic2);
                    Assert.Equal(curve.Polynomial, exported.Curve.Polynomial);
                    Assert.Equal(curve.A, exported.Curve.A);
                    Assert.Equal(curve.B, exported.Curve.B);
                    Assert.Equal(curve.G.X, exported.Curve.G.X);
                    Assert.Equal(curve.G.Y, exported.Curve.G.Y);
                    Assert.Equal(curve.Order, exported.Curve.Order);
                    Assert.Equal(curve.Cofactor, exported.Curve.Cofactor);

                    ComparePublicKey(testData.Q, exported.Q);
                    ComparePrivateKey(testData, exported);
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [Fact]
        public void CtorEcKeyNamedCurveImportPublicKey()
        {
            ECParameters testData = EccTestData.GetNistP256ReferenceKey();
            byte[] data = ByteUtils.RepeatByte(0x42, 64);

            byte[] signature;
            using (ECDsa signer = ECDsa.Create(testData))
            {
                signature = signer.SignData(data, HashAlgorithmName.SHA256);
            }

            ECParameters publicOnly = new ECParameters
            {
                Curve = testData.Curve,
                Q = testData.Q,
            };

            IntPtr ecKey;
            int rc = Interop.Crypto.EcKeyCreateByKeyParameters(
                out ecKey,
                publicOnly.Curve.Oid.Value!,
                publicOnly.Q.X, publicOnly.Q.X!.Length,
                publicOnly.Q.Y, publicOnly.Q.Y!.Length,
                null, 0);

            Assert.Equal(1, rc);
            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                using (ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey))
                {
                    Assert.True(ecKeyBacked.VerifyData(data, signature, HashAlgorithmName.SHA256));

                    byte[] wrongData = ByteUtils.RepeatByte(0x43, 64);
                    Assert.False(ecKeyBacked.VerifyData(wrongData, signature, HashAlgorithmName.SHA256));

                    Assert.ThrowsAny<CryptographicException>(() =>
                        ecKeyBacked.SignData(data, HashAlgorithmName.SHA256));
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [Fact]
        public void CtorEcKeyNamedCurveImportPrivateKey()
        {
            ECParameters testData = EccTestData.GetNistP256ReferenceKey();
            byte[] data = ByteUtils.RepeatByte(0x42, 64);

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
                using (ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey))
                {
                    byte[] signature = ecKeyBacked.SignData(data, HashAlgorithmName.SHA256);

                    using (ECDsa verifier = ECDsa.Create(testData))
                    {
                        Assert.True(verifier.VerifyData(data, signature, HashAlgorithmName.SHA256));
                    }
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [Fact]
        public void CtorEcKeyPublicOnlyExportPrivateThrows()
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
                using (ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey))
                {
                    ECParameters publicParams = ecKeyBacked.ExportParameters(false);
                    Assert.NotNull(publicParams.Q.X);
                    Assert.Null(publicParams.D);

                    Assert.ThrowsAny<CryptographicException>(() =>
                        ecKeyBacked.ExportParameters(true));
                }
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [ConditionalFact(nameof(SupportsExplicitCurves))]
        public void ExplicitCurveImportExportProducesSameExplicitParams()
        {
            ECCurve explicitCurve = EccTestData.GetNistP256ExplicitCurve();

            using (ECDsa original = ECDsa.Create(explicitCurve))
            {
                ECParameters explicitParams = original.ExportExplicitParameters(true);

                using (ECDsa reimported = ECDsa.Create(explicitParams))
                {
                    ECParameters reimportedParams = reimported.ExportExplicitParameters(true);

                    ComparePublicKey(explicitParams.Q, reimportedParams.Q);
                    ComparePrivateKey(explicitParams, reimportedParams);
                }
            }
        }

        [ConditionalFact(nameof(ECExplicitCurvesSupported))]
        public void ExplicitCurveImportAndOriginalSignVerifyCrossCompatible()
        {
            byte[] data = ByteUtils.RepeatByte(0x42, 64);
            ECCurve explicitCurve = EccTestData.GetNistP256ExplicitCurve();

            using (ECDsa key1 = ECDsa.Create(explicitCurve))
            {
                ECParameters explicitParams = key1.ExportExplicitParameters(true);

                using (ECDsa key2 = ECDsa.Create(explicitParams))
                {
                    byte[] sig1 = key1.SignData(data, HashAlgorithmName.SHA256);
                    Assert.True(key2.VerifyData(data, sig1, HashAlgorithmName.SHA256));

                    byte[] sig2 = key2.SignData(data, HashAlgorithmName.SHA256);
                    Assert.True(key1.VerifyData(data, sig2, HashAlgorithmName.SHA256));
                }
            }
        }

        [Fact]
        public void GenerateKeyImplicitCurveThrows()
        {
            ECCurve implicitCurve = default;

            using (ECDsa ecdsa = new ECDsaOpenSsl())
            {
                Assert.Throws<PlatformNotSupportedException>(() => ecdsa.GenerateKey(implicitCurve));
            }
        }

        [Fact]
        public void ImportParametersImplicitCurveThrows()
        {
            ECParameters parameters = new ECParameters
            {
                Curve = default,
                D = new byte[32],
            };

            using (ECDsa ecdsa = new ECDsaOpenSsl())
            {
                Assert.Throws<PlatformNotSupportedException>(() => ecdsa.ImportParameters(parameters));
            }
        }
    }
}

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByOid")]
        internal static extern IntPtr EcKeyCreateByOid(string oid);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByKeyParameters")]
        internal static extern int EcKeyCreateByKeyParameters(
            out IntPtr key,
            string oid,
            byte[]? qx, int qxLength,
            byte[]? qy, int qyLength,
            byte[]? d, int dLength);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyGenerateKey")]
        internal static extern int EcKeyGenerateKey(IntPtr ecKey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyDestroy")]
        internal static extern void EcKeyDestroy(IntPtr r);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OpenSslVersionNumber")]
        internal static extern uint OpenSslVersionNumber();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyGenerateByEcCurveOid")]
        internal static extern int EvpPKeyGenerateByEcCurveOid(out SafeEvpPKeyHandle pkey, string oid, out int keySize);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByExplicitParameters")]
        internal static extern IntPtr EcKeyCreateByExplicitParameters(
            ECCurve.ECCurveType curveType,
            byte[]? qx, int qxLength,
            byte[]? qy, int qyLength,
            byte[]? d, int dLength,
            byte[] p, int pLength,
            byte[] a, int aLength,
            byte[] b, int bLength,
            byte[] gx, int gxLength,
            byte[] gy, int gyLength,
            byte[] order, int orderLength,
            byte[] cofactor, int cofactorLength,
            byte[]? seed, int seedLength);
    }
}
