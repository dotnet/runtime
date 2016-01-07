// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDsaOpenSsl.Tests
{
    public static class EcDsaOpenSslTests
    {
        // TODO: Issue #4337.  Temporary workaround for tests to pass on CentOS 
        // where secp224r1 appears to be disabled. 
        private static bool ECDsa224Available
        {
            get
            {
                try
                {
                    using (ECDsaOpenSsl e = new ECDsaOpenSsl(224)) e.Exercise();
                    return true;
                }
                catch (Exception exc)
                {
                    return !exc.Message.Contains("unknown group");
                }
            }
        }

        [Fact]
        public static void DefaultCtor()
        {
            using (ECDsaOpenSsl e = new ECDsaOpenSsl())
            {
                int keySize = e.KeySize;
                Assert.Equal(521, keySize);
                e.Exercise();
            }
        }

        [ConditionalFact("ECDsa224Available")] // Issue #4337
        public static void Ctor224()
        {
            int expectedKeySize = 224;
            using (ECDsaOpenSsl e = new ECDsaOpenSsl(expectedKeySize))
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
            using (ECDsaOpenSsl e = new ECDsaOpenSsl(expectedKeySize))
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
            using (ECDsaOpenSsl e = new ECDsaOpenSsl(expectedKeySize))
            {
                int keySize = e.KeySize;
                Assert.Equal(expectedKeySize, keySize);
                e.Exercise();
            }
        }

        [ConditionalFact("ECDsa224Available")] // Issue #4337
        public static void CtorHandle224()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByCurveName(NID_secp224r1);
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
        public static void CtorHandle384()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByCurveName(NID_secp384r1);
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
        public static void CtorHandle521()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByCurveName(NID_secp521r1);
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
        public static void CtorHandleDuplicate()
        {
            IntPtr ecKey = Interop.Crypto.EcKeyCreateByCurveName(NID_secp521r1);
            Assert.NotEqual(IntPtr.Zero, ecKey);
            int success = Interop.Crypto.EcKeyGenerateKey(ecKey);
            Assert.NotEqual(0, success);

            using (ECDsaOpenSsl e = new ECDsaOpenSsl(ecKey))
            {
                // Make sure ECDsaOpenSsl did his own ref-count bump.
                Interop.Crypto.EcKeyDestroy(ecKey);

                int keySize = e.KeySize;
                Assert.Equal(521, keySize);
                e.Exercise();
            }
        }

        [ConditionalFact("ECDsa224Available")] // Issue #4337
        public static void KeySizeProp()
        {
            using (ECDsaOpenSsl e = new ECDsaOpenSsl())
            {
                e.KeySize = 384;
                Assert.Equal(384, e.KeySize);
                e.Exercise();

                e.KeySize = 224;
                Assert.Equal(224, e.KeySize);
                e.Exercise();
            }
        }

        [Fact]
        public static void VerifyDuplicateKey_ValidHandle()
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
        public static void VerifyDuplicateKey_DistinctHandles()
        {
            using (ECDsaOpenSsl first = new ECDsaOpenSsl())
            using (SafeEvpPKeyHandle firstHandle = first.DuplicateKeyHandle())
            using (SafeEvpPKeyHandle firstHandle2 = first.DuplicateKeyHandle())
            {
                Assert.NotSame(firstHandle, firstHandle2);
            }
        }

        [Fact]
        public static void VerifyDuplicateKey_RefCounts()
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
        public static void VerifyDuplicateKey_NullHandle()
        {
            SafeEvpPKeyHandle pkey = null;
            Assert.Throws<ArgumentNullException>(() => new RSAOpenSsl(pkey));
        }

        [Fact]
        public static void VerifyDuplicateKey_InvalidHandle()
        {
            using (ECDsaOpenSsl ecdsa = new ECDsaOpenSsl())
            {
                SafeEvpPKeyHandle pkey = ecdsa.DuplicateKeyHandle();

                using (pkey)
                {
                }

                Assert.Throws<ArgumentException>(() => new ECDsaOpenSsl(pkey));
            }
        }

        [Fact]
        public static void VerifyDuplicateKey_NeverValidHandle()
        {
            using (SafeEvpPKeyHandle pkey = new SafeEvpPKeyHandle(IntPtr.Zero, false))
            {
                Assert.Throws<ArgumentException>(() => new ECDsaOpenSsl(pkey));
            }
        }

        [Fact]
        public static void VerifyDuplicateKey_RsaHandle()
        {
            using (RSAOpenSsl rsa = new RSAOpenSsl())
            using (SafeEvpPKeyHandle pkey = rsa.DuplicateKeyHandle())
            {
                Assert.ThrowsAny<CryptographicException>(() => new ECDsaOpenSsl(pkey));
            }
        }

        private static void Exercise(this ECDsaOpenSsl e)
        {
            // Make a few calls on this to ensure we aren't broken due to bad/prematurely released handles.

            int keySize = e.KeySize;

            byte[] data = new byte[0x10];
            byte[] sig = e.SignData(data, 0, data.Length, HashAlgorithmName.SHA1);
            bool verified = e.VerifyData(data, sig, HashAlgorithmName.SHA1);
            Assert.True(verified);
            sig[sig.Length - 1]++;
            verified = e.VerifyData(data, sig, HashAlgorithmName.SHA1);
            Assert.False(verified);
        }

        private const int NID_secp224r1 = 713;
        private const int NID_secp384r1 = 715;
        private const int NID_secp521r1 = 716;
    }
}

internal static partial class Interop
{
    internal static class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByCurveName")]
        internal static extern IntPtr EcKeyCreateByCurveName(int nid);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyGenerateKey")]
        internal static extern int EcKeyGenerateKey(IntPtr ecKey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyDestroy")]
        internal static extern void EcKeyDestroy(IntPtr r);
    }
}
