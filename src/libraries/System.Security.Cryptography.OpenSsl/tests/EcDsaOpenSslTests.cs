// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.EcDsa.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDsa.OpenSsl.Tests
{
    public class EcDsaOpenSslTests : ECDsaTestsBase
    {
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
            int rc = Interop.Crypto.EvpPKeyGenerateByEcKeyOid(out SafeEvpPKeyHandle pkey, oid);

            if (rc != 1 || pkey.IsInvalid)
            {
                pkey.Dispose();
                throw new SkipTestException("EVP_PKEY EC generation not supported");
            }

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
        public void EcKeyAndEvpPKeyProduceSameExport(string oid, int expectedKeySize)
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(oid);
            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                Assert.NotEqual(0, Interop.Crypto.EcKeyGenerateKey(ecKey));

                using ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey);
                Assert.Equal(expectedKeySize, ecKeyBacked.KeySize);

                ECParameters privateParams = ecKeyBacked.ExportParameters(true);

                using ECDsa evpBacked = ECDsa.Create(privateParams);
                Assert.Equal(expectedKeySize, evpBacked.KeySize);

                ECParameters evpPrivateParams = evpBacked.ExportParameters(true);

                ComparePublicKey(privateParams.Q, evpPrivateParams.Q);
                ComparePrivateKey(privateParams, evpPrivateParams);
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
        public void EcKeyAndEvpPKeySignVerifyCrossCompatible(string oid)
        {
            byte[] data = ByteUtils.RepeatByte(0x42, 64);

            IntPtr ecKey = Interop.Crypto.EcKeyCreateByOid(oid);
            Assert.NotEqual(IntPtr.Zero, ecKey);

            try
            {
                Assert.NotEqual(0, Interop.Crypto.EcKeyGenerateKey(ecKey));

                using ECDsaOpenSsl ecKeyBacked = new ECDsaOpenSsl(ecKey);
                using ECDsa evpBacked = ECDsa.Create(ecKeyBacked.ExportParameters(true));

                byte[] sig1 = ecKeyBacked.SignData(data, HashAlgorithmName.SHA256);
                Assert.True(evpBacked.VerifyData(data, sig1, HashAlgorithmName.SHA256));

                byte[] sig2 = evpBacked.SignData(data, HashAlgorithmName.SHA256);
                Assert.True(ecKeyBacked.VerifyData(data, sig2, HashAlgorithmName.SHA256));
            }
            finally
            {
                Interop.Crypto.EcKeyDestroy(ecKey);
            }
        }

        [Fact]
        public void ExplicitCurveEcKeyAndEvpPKeyProduceSameExport()
        {
            ECCurve explicitCurve = EccTestData.GetNistP256ExplicitCurve();

            using ECDsa ecKeyBacked = ECDsa.Create(explicitCurve);
            ECParameters explicitParams = ecKeyBacked.ExportExplicitParameters(true);

            using ECDsa evpBacked = ECDsa.Create(explicitParams);
            ECParameters evpExplicitParams = evpBacked.ExportExplicitParameters(true);

            ComparePublicKey(explicitParams.Q, evpExplicitParams.Q);
            ComparePrivateKey(explicitParams, evpExplicitParams);
        }

        [Fact]
        public void ExplicitCurveEcKeyAndEvpPKeySignVerifyCrossCompatible()
        {
            byte[] data = ByteUtils.RepeatByte(0x42, 64);
            ECCurve explicitCurve = EccTestData.GetNistP256ExplicitCurve();

            using ECDsa key1 = ECDsa.Create(explicitCurve);
            ECParameters explicitParams = key1.ExportExplicitParameters(true);

            using ECDsa key2 = ECDsa.Create(explicitParams);

            byte[] sig1 = key1.SignData(data, HashAlgorithmName.SHA256);
            Assert.True(key2.VerifyData(data, sig1, HashAlgorithmName.SHA256));

            byte[] sig2 = key2.SignData(data, HashAlgorithmName.SHA256);
            Assert.True(key1.VerifyData(data, sig2, HashAlgorithmName.SHA256));
        }
    }
}

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByOid")]
        internal static extern IntPtr EcKeyCreateByOid(string oid);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyGenerateKey")]
        internal static extern int EcKeyGenerateKey(IntPtr ecKey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyDestroy")]
        internal static extern void EcKeyDestroy(IntPtr r);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OpenSslVersionNumber")]
        internal static extern uint OpenSslVersionNumber();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyGenerateByEcKeyOid", CharSet = CharSet.Ansi)]
        internal static extern int EvpPKeyGenerateByEcKeyOid(out SafeEvpPKeyHandle pkey, string oid);
    }
}
