// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Tests;

using Xunit;
using Test.Cryptography;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public partial class ECDiffieHellmanTests : EccTestBase
    {
        private static List<object[]> s_everyKeysize;
        private static List<object[]> s_mismatchedKeysizes;

        public static IEnumerable<object[]> EveryKeysize()
        {
            if (s_everyKeysize == null)
            {
                List<object[]> everyKeysize = new List<object[]>();

                using (ECDiffieHellman defaultKeysize = ECDiffieHellmanFactory.Create())
                {
                    foreach (KeySizes keySizes in defaultKeysize.LegalKeySizes)
                    {
                        for (int size = keySizes.MinSize; size <= keySizes.MaxSize; size += keySizes.SkipSize)
                        {
                            everyKeysize.Add(new object[] { size });

                            if (keySizes.SkipSize == 0)
                            {
                                break;
                            }
                        }
                    }
                }

                s_everyKeysize = everyKeysize;
            }

            return s_everyKeysize;
        }

        public static IEnumerable<object[]> MismatchedKeysizes()
        {
            if (s_mismatchedKeysizes == null)
            {
                int firstSize = -1;
                List<object[]> mismatchedKeysizes = new List<object[]>();

                using (ECDiffieHellman defaultKeysize = ECDiffieHellmanFactory.Create())
                {
                    foreach (KeySizes keySizes in defaultKeysize.LegalKeySizes)
                    {
                        for (int size = keySizes.MinSize; size <= keySizes.MaxSize; size += keySizes.SkipSize)
                        {
                            if (firstSize == -1)
                            {
                                firstSize = size;
                            }
                            else if (size != firstSize)
                            {
                                mismatchedKeysizes.Add(new object[] { firstSize, size });
                            }

                            if (keySizes.SkipSize == 0)
                            {
                                break;
                            }
                        }
                    }
                }

                s_mismatchedKeysizes = mismatchedKeysizes;
            }

            return s_mismatchedKeysizes;
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void SupportsKeysize(int keySize)
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create(keySize))
            {
                Assert.Equal(keySize, ecdh.KeySize);
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void PublicKey_NotNull(int keySize)
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey ecdhPubKey = ecdh.PublicKey)
            {
                Assert.NotNull(ecdhPubKey);
            }
        }

        [Fact]
        public static void PublicKeyIsFactory()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey1 = ecdh.PublicKey)
            using (ECDiffieHellmanPublicKey publicKey2 = ecdh.PublicKey)
            {
                Assert.NotSame(publicKey1, publicKey2);
            }
        }

        [Fact]
        public static void PublicKey_TryExportSubjectPublicKeyInfo_TooSmall()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                Span<byte> destination = stackalloc byte[1];
                Assert.False(publicKey.TryExportSubjectPublicKeyInfo(destination, out int written));
                Assert.Equal(0, written);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void UseAfterDispose(bool importKey)
        {
            ECDiffieHellman key = ECDiffieHellmanFactory.Create();
            ECDiffieHellmanPublicKey pubKey;
            HashAlgorithmName hash = HashAlgorithmName.SHA256;

            if (importKey)
            {
                key.ImportParameters(EccTestData.GetNistP256ReferenceKey());
            }

            // Ensure the key is populated, then dispose it.
            using (key)
            {
                pubKey = key.PublicKey;
                key.DeriveKeyFromHash(pubKey, hash);

                pubKey.Dispose();
                Assert.Throws<ObjectDisposedException>(() => key.DeriveKeyFromHash(pubKey, hash));
                Assert.Throws<ObjectDisposedException>(() => key.DeriveKeyFromHmac(pubKey, hash, null));
                Assert.Throws<ObjectDisposedException>(() => key.DeriveKeyFromHmac(pubKey, hash, new byte[3]));
                Assert.Throws<ObjectDisposedException>(() => key.DeriveKeyTls(pubKey, new byte[4], new byte[64]));

                pubKey = key.PublicKey;
            }

            key.Dispose();

            Assert.Throws<ObjectDisposedException>(() => key.DeriveKeyFromHash(pubKey, hash));
            Assert.Throws<ObjectDisposedException>(() => key.DeriveKeyFromHmac(pubKey, hash, null));
            Assert.Throws<ObjectDisposedException>(() => key.DeriveKeyFromHmac(pubKey, hash, new byte[3]));
            Assert.Throws<ObjectDisposedException>(() => key.DeriveKeyTls(pubKey, new byte[4], new byte[64]));
            Assert.Throws<ObjectDisposedException>(() => key.GenerateKey(ECCurve.NamedCurves.nistP256));
            Assert.Throws<ObjectDisposedException>(() => key.ImportParameters(EccTestData.GetNistP256ReferenceKey()));

            // Either set_KeySize or the ExportParameters should throw.
            Assert.Throws<ObjectDisposedException>(
                () =>
                {
                    key.KeySize = 384;
                    key.ExportParameters(false);
                });
        }

#if NETCOREAPP
        private static ECDiffieHellman OpenKnownKey()
        {
            ECParameters ecParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,

                Q =
                {
                    X = (
                        "014AACFCDA18F77EBF11DC0A2D394D3032E86C3AC0B5F558916361163EA6AD3DB27" +
                        "F6476D6C6E5D9C4A77BCCC5C0069D481718DACA3B1B13035AF5D246C4DC0CE0EA").HexToByteArray(),

                    Y = (
                        "00CA500F75537C782E027DE568F148334BF56F7E24C3830792236B5D20F7A33E998" +
                        "62B1744D2413E4C4AC29DBA42FC48D23AE5B916BED73997EC69B3911C686C5164").HexToByteArray(),
                },

                D = (
                    "00202F9F5480723D1ACF15372CE0B99B6CC3E8772FFDDCF828EEEB314B3EAA35B19" +
                    "886AAB1E6871E548C261C7708BF561A4C373D3EED13F0749851F57B86DC049D71").HexToByteArray(),
            };

            ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create();
            ecdh.ImportParameters(ecParams);
            return ecdh;
        }
#endif
    }

    internal static class EcdhTestExtensions
    {
        internal static void Exercise(this ECDiffieHellman e)
        {
            // Make a few calls on this to ensure we aren't broken due to bad/prematurely released handles.
            int keySize = e.KeySize;

            using (ECDiffieHellmanPublicKey publicKey = e.PublicKey)
            {
                byte[] negotiated = e.DeriveKeyFromHash(publicKey, HashAlgorithmName.SHA256);
                Assert.Equal(256 / 8, negotiated.Length);
            }
        }
    }
}
