// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Not supported on Browser/iOS/tvOS/MacCatalyst")]
    public sealed class DSASignVerify_Array : DSASignVerify
    {
        public override byte[] SignData(DSA dsa, byte[] data, HashAlgorithmName hashAlgorithm) =>
            dsa.SignData(data, hashAlgorithm);
        public override bool VerifyData(DSA dsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm) =>
            dsa.VerifyData(data, signature, hashAlgorithm);

        protected override void UseAfterDispose(DSA dsa, byte[] data, byte[] sig)
        {
            base.UseAfterDispose(dsa, data, sig);
            byte[] hash = new byte[20];

            Assert.Throws<ObjectDisposedException>(() => dsa.CreateSignature(hash));
            Assert.Throws<ObjectDisposedException>(() => dsa.VerifySignature(hash, sig));
        }

        [Fact]
        public void InvalidStreamArrayArguments_Throws()
        {
            using (DSA dsa = DSAFactory.Create(1024))
            {
                AssertExtensions.Throws<ArgumentNullException>("rgbHash", () => dsa.CreateSignature(null));

                AssertExtensions.Throws<ArgumentNullException>("data", () => dsa.SignData((byte[])null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("data", () => dsa.SignData(null, 0, 0, HashAlgorithmName.SHA1));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => dsa.SignData(new byte[1], -1, 0, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => dsa.SignData(new byte[1], 2, 0, HashAlgorithmName.SHA1));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => dsa.SignData(new byte[1], 0, -1, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => dsa.SignData(new byte[1], 0, 2, HashAlgorithmName.SHA1));

                AssertExtensions.Throws<ArgumentNullException>("data", () => dsa.VerifyData((byte[])null, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("data", () => dsa.VerifyData(null, 0, 0, null, HashAlgorithmName.SHA1));

                AssertExtensions.Throws<ArgumentNullException>("signature", () => dsa.VerifyData(new byte[1], null, HashAlgorithmName.SHA1));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => dsa.VerifyData(new byte[1], -1, 0, new byte[1], HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => dsa.VerifyData(new byte[1], 2, 0, new byte[1], HashAlgorithmName.SHA1));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => dsa.VerifyData(new byte[1], 0, -1, new byte[1], HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => dsa.VerifyData(new byte[1], 0, 2, new byte[1], HashAlgorithmName.SHA1));
            }
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Not supported on Browser/iOS/tvOS/MacCatalyst")]
    public sealed class DSASignVerify_Stream : DSASignVerify
    {
        public override byte[] SignData(DSA dsa, byte[] data, HashAlgorithmName hashAlgorithm) =>
            dsa.SignData(new MemoryStream(data), hashAlgorithm);
        public override bool VerifyData(DSA dsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm) =>
            dsa.VerifyData(new MemoryStream(data), signature, hashAlgorithm);

        [Fact]
        public void InvalidArrayArguments_Throws()
        {
            using (DSA dsa = DSAFactory.Create(1024))
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => dsa.SignData((Stream)null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("data", () => dsa.VerifyData((Stream)null, null, HashAlgorithmName.SHA1));

                AssertExtensions.Throws<ArgumentNullException>("signature", () => dsa.VerifyData(new MemoryStream(), null, HashAlgorithmName.SHA1));
            }
        }
    }

#if NETCOREAPP
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Not supported on Browser/iOS/tvOS/MacCatalyst")]
    public sealed class DSASignVerify_Span : DSASignVerify
    {
        public override byte[] SignData(DSA dsa, byte[] data, HashAlgorithmName hashAlgorithm) =>
            TryWithOutputArray(dest => dsa.TrySignData(data, dest, hashAlgorithm, out int bytesWritten) ? (true, bytesWritten) : (false, 0));

        public override bool VerifyData(DSA dsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm) =>
            dsa.VerifyData((ReadOnlySpan<byte>)data, (ReadOnlySpan<byte>)signature, hashAlgorithm);

        protected override void UseAfterDispose(DSA dsa, byte[] data, byte[] sig)
        {
            base.UseAfterDispose(dsa, data, sig);
            byte[] hash = new byte[20];

            Assert.Throws<ObjectDisposedException>(() => dsa.TryCreateSignature(hash, sig, out _));
            Assert.Throws<ObjectDisposedException>(() => dsa.VerifySignature(hash.AsSpan(), sig.AsSpan()));
        }

        private static byte[] TryWithOutputArray(Func<byte[], (bool, int)> func)
        {
            for (int length = 1; ; length = checked(length * 2))
            {
                var result = new byte[length];
                var (success, bytesWritten) = func(result);
                if (success)
                {
                    Array.Resize(ref result, bytesWritten);
                    return result;
                }
            }
        }
    }
#endif
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Not supported on Browser/iOS/tvOS/MacCatalyst")]
    public abstract partial class DSASignVerify
    {
        public abstract byte[] SignData(DSA dsa, byte[] data, HashAlgorithmName hashAlgorithm);
        public abstract bool VerifyData(DSA dsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm);

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public void InvalidKeySize_DoesNotInvalidateKey()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                byte[] signature = SignData(dsa, DSATestData.HelloBytes, HashAlgorithmName.SHA1);

                // A 2049-bit key is hard to describe, none of the providers support it.
                Assert.ThrowsAny<CryptographicException>(() => dsa.KeySize = 2049);

                Assert.True(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA1));
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public void UseAfterDispose_NewKey()
        {
            UseAfterDispose(false);
        }

        [Fact]
        public void UseAfterDispose_ImportedKey()
        {
            UseAfterDispose(true);
        }

        private void UseAfterDispose(bool importKey)
        {
            DSA key = importKey ? DSAFactory.Create(DSATestData.GetDSA1024Params()) : DSAFactory.Create(1024);
            byte[] data = { 1 };
            byte[] sig;

            // Ensure the key is populated, then dispose it.
            using (key)
            {
                sig = SignData(key, data, HashAlgorithmName.SHA1);
            }

            key.Dispose();

            UseAfterDispose(key, data, sig);

            Assert.Throws<ObjectDisposedException>(() => key.ImportParameters(DSATestData.GetDSA1024Params()));

            // Either set_KeySize or SignData should throw.
            Assert.Throws<ObjectDisposedException>(
                () =>
                {
                    try
                    {
                        key.KeySize = 576;
                    }
                    catch (CryptographicException)
                    {
                        // DSACryptoServiceProvider on Android only supports 1024 and does an early check for legal
                        // key sizes, since it is more restrictive than the wrapped implementation. It will throw
                        // CryptographicException. SignData should still throw ObjectDisposedException.
                        if (!PlatformDetection.IsAndroid)
                        {
                            throw;
                        }
                    }

                    SignData(key, data, HashAlgorithmName.SHA1);
                });
        }

        protected virtual void UseAfterDispose(DSA dsa, byte[] data, byte[] sig)
        {
            Assert.Throws<ObjectDisposedException>(
                () => SignData(dsa, data, HashAlgorithmName.SHA1));

            Assert.Throws<ObjectDisposedException>(
                () => VerifyData(dsa, data, sig, HashAlgorithmName.SHA1));
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public void SignAndVerifyDataNew1024()
        {
            using (DSA dsa = DSAFactory.Create(1024))
            {
                byte[] signature = SignData(dsa, DSATestData.HelloBytes, new HashAlgorithmName("SHA1"));
                bool signatureMatched = VerifyData(dsa, DSATestData.HelloBytes, signature, new HashAlgorithmName("SHA1"));
                Assert.True(signatureMatched);
            }
        }

        [Fact]
        public void VerifyKnown_512()
        {
            byte[] signature = (
                // r:
                "E21F20B0B5E553137F6649DDC58F5E4AB7D4E6DE" +
                // s:
                "C37534CC7D9630339936C581690E832BD85C6C79").HexToByteArray();

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.Dsa512Parameters);
                Assert.True(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA1));
            }
        }

        [Fact]
        public void VerifyKnown_576()
        {
            byte[] signature = (
                // r:
                "490AEFA5A4F28B35183BBA3BE2536514AB13A088" +
                // s:
                "3F883FE96524D4CC596F67B64A3382E794C8D65B").HexToByteArray();

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.Dsa576Parameters);
                Assert.True(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA1));
            }
        }

        [Fact]
        public void PublicKey_CannotSign()
        {
            DSAParameters keyParameters = DSATestData.GetDSA1024Params();
            keyParameters.X = null;

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(keyParameters);

                Assert.ThrowsAny<CryptographicException>(
                    () => SignData(dsa, DSATestData.HelloBytes, HashAlgorithmName.SHA1));
            }
        }

        [Fact]
        public void SignAndVerifyDataExplicit1024()
        {
            SignAndVerify(DSATestData.HelloBytes, "SHA1", DSATestData.GetDSA1024Params(), 40);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public void SignAndVerifyDataExplicit2048()
        {
            SignAndVerify(DSATestData.HelloBytes, "SHA256", DSATestData.GetDSA2048Params(), 64);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public void VerifyKnown_2048_SHA256()
        {
            byte[] signature =
            {
                0x92, 0x06, 0x0B, 0x57, 0xF1, 0x35, 0x20, 0x28,
                0xC6, 0x54, 0x4A, 0x0F, 0x08, 0x48, 0x5F, 0x5D,
                0x55, 0xA8, 0x42, 0xFB, 0x05, 0xA7, 0x3E, 0x32,
                0xCA, 0xC6, 0x91, 0x77, 0x70, 0x0A, 0x68, 0x44,
                0x60, 0x63, 0xF7, 0xE7, 0x96, 0x54, 0x8F, 0x4A,
                0x6D, 0x47, 0x10, 0xEE, 0x9A, 0x9F, 0xC2, 0xC8,
                0xDD, 0x74, 0xAE, 0x1A, 0x68, 0xF3, 0xA9, 0xB8,
                0x62, 0x14, 0x50, 0xA3, 0x01, 0x1D, 0x2A, 0x22,
            };

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA2048Params());
                Assert.True(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA256));
                Assert.False(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA384));
                Assert.False(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA512));
            }
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public void VerifyKnown_2048_SHA384()
        {
            byte[] signature =
            {
                0x56, 0xBA, 0x70, 0x48, 0x18, 0xBA, 0xE3, 0x43,
                0xF0, 0x7F, 0x25, 0xFE, 0xEA, 0xF1, 0xDB, 0x49,
                0x37, 0x15, 0xD3, 0xD0, 0x5B, 0x9D, 0x57, 0x19,
                0x73, 0x44, 0xDA, 0x70, 0x8D, 0x44, 0x7D, 0xBA,
                0x83, 0xDB, 0x8E, 0x8F, 0x39, 0x0F, 0x83, 0xD5,
                0x0B, 0x73, 0x81, 0x77, 0x3D, 0x9B, 0x8D, 0xA4,
                0xAD, 0x94, 0x3C, 0xAB, 0x7A, 0x6C, 0x81, 0x48,
                0x2F, 0xCF, 0x50, 0xE3, 0x34, 0x0B, 0xEC, 0xF0,
            };

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA2048Params());
                Assert.True(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA384));
                Assert.False(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA256));
                Assert.False(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA512));
            }
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public void VerifyKnown_2048_SHA512()
        {
            byte[] signature =
            {
                0x6F, 0x44, 0x68, 0x1F, 0x74, 0xF7, 0x90, 0x2F,
                0x38, 0x43, 0x9B, 0x00, 0x15, 0xDA, 0xF6, 0x8F,
                0x97, 0xB4, 0x4A, 0x52, 0xF7, 0xC1, 0xEC, 0x21,
                0xE2, 0x44, 0x48, 0x71, 0x0F, 0xEC, 0x5E, 0xB3,
                0xA1, 0xCB, 0xE4, 0x42, 0xC8, 0x1E, 0xCD, 0x3C,
                0xA8, 0x15, 0x51, 0xDE, 0x0C, 0xCC, 0xAE, 0x4D,
                0xEB, 0x2A, 0xE9, 0x13, 0xBB, 0x7F, 0x3C, 0xFB,
                0x69, 0x8A, 0x8E, 0x0F, 0x80, 0x87, 0x2E, 0xA6,
            };

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA2048Params());
                Assert.True(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA512));
                Assert.False(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA256));
                Assert.False(VerifyData(dsa, DSATestData.HelloBytes, signature, HashAlgorithmName.SHA384));
            }
        }

        [Fact]
        public void VerifyKnownSignature()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                byte[] data;
                byte[] signature;
                DSAParameters dsaParameters;
                DSATestData.GetDSA1024_186_2(out dsaParameters, out signature, out data);

                dsa.ImportParameters(dsaParameters);
                Assert.True(VerifyData(dsa, data, signature, HashAlgorithmName.SHA1));

                // Negative case
                signature[signature.Length - 1] ^= 0xff;
                Assert.False(VerifyData(dsa, data, signature, HashAlgorithmName.SHA1));
            }
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public void Sign2048WithSha1()
        {
            byte[] data = { 1, 2, 3, 4 };

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA2048Params());

                byte[] signature = SignData(dsa, data, HashAlgorithmName.SHA1);

                Assert.True(VerifyData(dsa, data, signature, HashAlgorithmName.SHA1));
            }
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public void Verify2048WithSha1()
        {
            byte[] data = { 1, 2, 3, 4 };

            byte[] signature = (
                "28DC05B452C8FC0E0BFE9DA067D11147D31B1F3C63E5CF95046A812417C64844868D04D3A1D23" +
                "13E5DD07DE757B3A836E70A1C85DDC90CB62DE2E44746C760F2").HexToByteArray();

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA2048Params());

                Assert.True(VerifyData(dsa, data, signature, HashAlgorithmName.SHA1), "Untampered data verifies");

                data[0] ^= 0xFF;
                Assert.False(VerifyData(dsa, data, signature, HashAlgorithmName.SHA1), "Tampered data verifies");

                data[0] ^= 0xFF;
                signature[signature.Length - 1] ^= 0xFF;
                Assert.False(VerifyData(dsa, data, signature, HashAlgorithmName.SHA1), "Tampered signature verifies");
            }
        }

        private void SignAndVerify(byte[] data, string hashAlgorithmName, DSAParameters dsaParameters, int expectedSignatureLength)
        {
            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(dsaParameters);
                byte[] signature = SignData(dsa, data, new HashAlgorithmName(hashAlgorithmName));
                Assert.Equal(expectedSignatureLength, signature.Length);
                bool signatureMatched = VerifyData(dsa, data, signature, new HashAlgorithmName(hashAlgorithmName));
                Assert.True(signatureMatched);
            }
        }

        internal static bool SupportsFips186_3
        {
            get
            {
                return DSAFactory.SupportsFips186_3;
            }
        }
        public static bool SupportsKeyGeneration => DSAFactory.SupportsKeyGeneration;
    }
}
