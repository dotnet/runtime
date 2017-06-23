// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.OpenSsl.Tests
{
    public static class RsaOpenSslTests
    {
        [Fact]
        public static void VerifyDuplicateKey_ValidHandle()
        {
            byte[] data = ByteUtils.RepeatByte(0x71, 11);

            using (RSAOpenSsl first = new RSAOpenSsl())
            using (SafeEvpPKeyHandle firstHandle = first.DuplicateKeyHandle())
            {
                using (RSA second = new RSAOpenSsl(firstHandle))
                {
                    byte[] signed = second.SignData(data, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
                    Assert.True(first.VerifyData(data, signed, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1));
                }
            }
        }

        [Fact]
        public static void VerifyDuplicateKey_DistinctHandles()
        {
            using (RSAOpenSsl first = new RSAOpenSsl())
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
            RSA second;

            using (RSAOpenSsl first = new RSAOpenSsl())
            using (SafeEvpPKeyHandle firstHandle = first.DuplicateKeyHandle())
            {
                signature = first.SignData(data, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1);
                second = new RSAOpenSsl(firstHandle);
            }

            // Now show that second still works, despite first and firstHandle being Disposed.
            using (second)
            {
                Assert.True(second.VerifyData(data, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1));
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
            using (RSAOpenSsl rsa = new RSAOpenSsl())
            {
                SafeEvpPKeyHandle pkey = rsa.DuplicateKeyHandle();

                using (pkey)
                {
                }

                AssertExtensions.Throws<ArgumentException>("pkeyHandle", () => new RSAOpenSsl(pkey));
            }
        }

        [Fact]
        public static void VerifyDuplicateKey_NeverValidHandle()
        {
            using (SafeEvpPKeyHandle pkey = new SafeEvpPKeyHandle(IntPtr.Zero, false))
            {
                AssertExtensions.Throws<ArgumentException>("pkeyHandle", () => new RSAOpenSsl(pkey));
            }
        }

        [Fact]
        public static void VerifyDuplicateKey_ECDsaHandle()
        {
            using (ECDsaOpenSsl ecdsa = new ECDsaOpenSsl())
            using (SafeEvpPKeyHandle pkey = ecdsa.DuplicateKeyHandle())
            {
                Assert.ThrowsAny<CryptographicException>(() => new RSAOpenSsl(pkey));
            }
        }
    }
}
