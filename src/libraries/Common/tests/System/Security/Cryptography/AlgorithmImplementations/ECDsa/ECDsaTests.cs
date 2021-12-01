// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Tests;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public sealed class ECDsaTests_Array : ECDsaTests
    {
        protected override bool VerifyData(ECDsa ecdsa, byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm) =>
            ecdsa.VerifyData(data, offset, count, signature, hashAlgorithm);
        protected override byte[] SignData(ECDsa ecdsa, byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
            ecdsa.SignData(data, offset, count, hashAlgorithm);

        protected override void UseAfterDispose(ECDsa ecdsa, byte[] data, byte[] sig)
        {
            base.UseAfterDispose(ecdsa, data, sig);
            byte[] hash = new byte[32];

            Assert.Throws<ObjectDisposedException>(() => ecdsa.VerifyHash(hash, sig));
            Assert.Throws<ObjectDisposedException>(() => ecdsa.SignHash(hash));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void SignData_InvalidArguments_Throws(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.SignData((byte[])null, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.SignData(null, -1, -1, default(HashAlgorithmName)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ecdsa.SignData(new byte[0], -1, -1, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ecdsa.SignData(new byte[0], 2, 1, default(HashAlgorithmName)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ecdsa.SignData(new byte[0], 0, -1, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ecdsa.SignData(new byte[0], 0, 1, default(HashAlgorithmName)));

            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.SignData(new byte[0], default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.SignData(new byte[0], 0, 0, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.SignData(new byte[0], 0, 0, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.SignData(new byte[10], 0, 10, new HashAlgorithmName("")));

            Assert.ThrowsAny<CryptographicException>(() => ecdsa.SignData(new byte[0], new HashAlgorithmName(Guid.NewGuid().ToString("N"))));
            Assert.ThrowsAny<CryptographicException>(() => ecdsa.SignData(new byte[0], 0, 0, new HashAlgorithmName(Guid.NewGuid().ToString("N"))));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyData_InvalidArguments_Throws(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.VerifyData((byte[])null, null, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.VerifyData(null, -1, -1, null, default(HashAlgorithmName)));

            AssertExtensions.Throws<ArgumentNullException>("signature", () => ecdsa.VerifyData(new byte[0], null, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentNullException>("signature", () => ecdsa.VerifyData(new byte[0], 0, 0, null, default(HashAlgorithmName)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ecdsa.VerifyData(new byte[0], -1, -1, null, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ecdsa.VerifyData(new byte[0], 2, 1, null, default(HashAlgorithmName)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ecdsa.VerifyData(new byte[0], 0, -1, null, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ecdsa.VerifyData(new byte[0], 0, 1, null, default(HashAlgorithmName)));

            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData(new byte[0], new byte[0], default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData(new byte[0], 0, 0, new byte[0], default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData(new byte[10], new byte[0], new HashAlgorithmName("")));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData(new byte[10], 0, 10, new byte[0], new HashAlgorithmName("")));

            Assert.ThrowsAny<CryptographicException>(() => ecdsa.VerifyData(new byte[0], new byte[0], new HashAlgorithmName(Guid.NewGuid().ToString("N"))));
            Assert.ThrowsAny<CryptographicException>(() => ecdsa.VerifyData(new byte[0], 0, 0, new byte[0], new HashAlgorithmName(Guid.NewGuid().ToString("N"))));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void SignHash_InvalidArguments_Throws(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("hash", () => ecdsa.SignHash(null));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyHash_InvalidArguments_Throws(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("hash", () => ecdsa.VerifyHash(null, null));
            AssertExtensions.Throws<ArgumentNullException>("signature", () => ecdsa.VerifyHash(new byte[0], null));
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public sealed class ECDsaTests_Stream : ECDsaTests
    {
        protected override bool VerifyData(ECDsa ecdsa, byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            var stream = new MemoryStream(data, offset, count);
            bool result = ecdsa.VerifyData(stream, signature, hashAlgorithm);
            Assert.Equal(stream.Length, stream.Position);
            return result;
        }

        protected override byte[] SignData(ECDsa ecdsa, byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            var stream = new MemoryStream(data, offset, count);
            byte[] result = ecdsa.SignData(stream, hashAlgorithm);
            Assert.Equal(stream.Length, stream.Position);
            return result;
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void SignData_InvalidArguments_Throws(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.SignData((Stream)null, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.SignData(new MemoryStream(), default(HashAlgorithmName)));
            Assert.ThrowsAny<CryptographicException>(() => ecdsa.SignData(new MemoryStream(), new HashAlgorithmName(Guid.NewGuid().ToString("N"))));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyData_InvalidArguments_Throws(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.VerifyData((Stream)null, null, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentNullException>("signature", () => ecdsa.VerifyData(new MemoryStream(), null, default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData(new MemoryStream(), new byte[0], default(HashAlgorithmName)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData(new MemoryStream(), new byte[0], new HashAlgorithmName("")));
            Assert.ThrowsAny<CryptographicException>(() => ecdsa.VerifyData(new MemoryStream(), new byte[0], new HashAlgorithmName(Guid.NewGuid().ToString("N"))));
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract partial class ECDsaTests : ECDsaTestsBase
    {
        protected bool VerifyData(ECDsa ecdsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm) =>
            VerifyData(ecdsa, data, 0, data.Length, signature, hashAlgorithm);
        protected abstract bool VerifyData(ECDsa ecdsa, byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm);

        protected byte[] SignData(ECDsa ecdsa, byte[] data, HashAlgorithmName hashAlgorithm) =>
            SignData(ecdsa, data, 0, data.Length, hashAlgorithm);
        protected abstract byte[] SignData(ECDsa ecdsa, byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm);

        public static IEnumerable<object[]> RealImplementations() =>
            new[] {
                new ECDsa[] { ECDsaFactory.Create() },
            };

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [Theory]
        [MemberData(nameof(RealImplementations))]
        public void UseAfterDispose_Import(ECDsa ecdsa)
        {
            ecdsa.ImportParameters(EccTestData.GetNistP256ReferenceKey());
            UseAfterDispose(ecdsa);
        }

        [Theory]
        [MemberData(nameof(RealImplementations))]
        public void UseAfterDispose_NewKey(ECDsa ecdsa)
        {
            UseAfterDispose(ecdsa);
        }

        private void UseAfterDispose(ECDsa ecdsa)
        {
            byte[] data = { 1 };
            byte[] sig;

            // Ensure the key is populated, then dispose it.
            using (ecdsa)
            {
                sig = SignData(ecdsa, data, HashAlgorithmName.SHA256);
            }

            ecdsa.Dispose();

            UseAfterDispose(ecdsa, data, sig);

            if (!(PlatformDetection.IsNetFramework && ecdsa.GetType().Name.EndsWith("Cng")))
            {
                Assert.Throws<ObjectDisposedException>(() => ecdsa.GenerateKey(ECCurve.NamedCurves.nistP256));

                Assert.Throws<ObjectDisposedException>(
                    () => ecdsa.ImportParameters(EccTestData.GetNistP256ReferenceKey()));
            }

            // Either set_KeySize or SignData should throw.
            Assert.Throws<ObjectDisposedException>(
                () =>
                {
                    ecdsa.KeySize = 384;
                    SignData(ecdsa, data, HashAlgorithmName.SHA256);
                });
        }

        protected virtual void UseAfterDispose(ECDsa ecdsa, byte[] data, byte[] sig)
        {
            Assert.Throws<ObjectDisposedException>(
                () => SignData(ecdsa, data, HashAlgorithmName.SHA256));

            Assert.Throws<ObjectDisposedException>(
                () => VerifyData(ecdsa, data, sig, HashAlgorithmName.SHA256));
        }

        [Theory]
        [MemberData(nameof(RealImplementations))]
        public void SignData_MaxOffset_ZeroLength_NoThrow(ECDsa ecdsa)
        {
            // Explicitly larger than Array.Empty
            byte[] data = new byte[10];
            byte[] signature = SignData(ecdsa, data, data.Length, 0, HashAlgorithmName.SHA256);

            Assert.True(VerifyData(ecdsa, Array.Empty<byte>(), signature, HashAlgorithmName.SHA256));
        }

        [Theory]
        [MemberData(nameof(RealImplementations))]
        public void VerifyData_MaxOffset_ZeroLength_NoThrow(ECDsa ecdsa)
        {
            // Explicitly larger than Array.Empty
            byte[] data = new byte[10];
            byte[] signature = SignData(ecdsa, Array.Empty<byte>(), HashAlgorithmName.SHA256);

            Assert.True(VerifyData(ecdsa, data, data.Length, 0, signature, HashAlgorithmName.SHA256));
        }

        [Theory]
        [MemberData(nameof(RealImplementations))]
        public void Roundtrip_WithOffset(ECDsa ecdsa)
        {
            byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] halfData = { 5, 6, 7, 8, 9 };

            byte[] dataSignature = SignData(ecdsa, data, 5, data.Length - 5, HashAlgorithmName.SHA256);
            byte[] halfDataSignature = SignData(ecdsa, halfData, HashAlgorithmName.SHA256);

            // Cross-feed the VerifyData calls to prove that both offsets work
            Assert.True(VerifyData(ecdsa, data, 5, data.Length - 5, halfDataSignature, HashAlgorithmName.SHA256));
            Assert.True(VerifyData(ecdsa, halfData, dataSignature, HashAlgorithmName.SHA256));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [Theory]
        [InlineData(256)]
        [InlineData(384)]
        [InlineData(521)]
        public void CreateKey(int keySize)
        {
            using (ECDsa ecdsa = ECDsaFactory.Create())
            {
                // Step 1, don't throw here.
                ecdsa.KeySize = keySize;

                // Step 2, ensure the key was generated without throwing.
                SignData(ecdsa, Array.Empty<byte>(), HashAlgorithmName.SHA256);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static IEnumerable<object[]> InteroperableSignatureConfigurations()
        {
            foreach (HashAlgorithmName hashAlgorithm in new[] {
                HashAlgorithmName.MD5,
                HashAlgorithmName.SHA1,
                HashAlgorithmName.SHA256,
                HashAlgorithmName.SHA384,
                HashAlgorithmName.SHA512 })
            {
                yield return new object[] { ECDsaFactory.Create(), hashAlgorithm };
            }
        }

        [Theory]
        [MemberData(nameof(InteroperableSignatureConfigurations))]
        public void SignVerify_InteroperableSameKeys_RoundTripsUnlessTampered(ECDsa ecdsa, HashAlgorithmName hashAlgorithm)
        {
            byte[] data = Encoding.UTF8.GetBytes("something to repeat and sign");

            // large enough to make hashing work though multiple iterations and not a multiple of 4KB it uses.
            byte[] dataArray = new byte[33333];

            byte[] dataArray2 = new byte[dataArray.Length + 2];
            dataArray.CopyTo(dataArray2, 1);

            HashAlgorithm halg;
            if (hashAlgorithm == HashAlgorithmName.MD5)
                halg = MD5.Create();
            else if (hashAlgorithm == HashAlgorithmName.SHA1)
                halg = SHA1.Create();
            else if (hashAlgorithm == HashAlgorithmName.SHA256)
                halg = SHA256.Create();
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
                halg = SHA384.Create();
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
                halg = SHA512.Create();
            else
                throw new Exception("Hash algorithm not supported.");

            List<byte[]> signatures = new List<byte[]>(6);

            // Compute a signature using each of the SignData overloads.  Then, verify it using each
            // of the VerifyData overloads, and VerifyHash overloads.
            //
            // Then, verify that VerifyHash fails if the data is tampered with.

            signatures.Add(SignData(ecdsa, dataArray, hashAlgorithm));

            signatures.Add(ecdsa.SignHash(halg.ComputeHash(dataArray)));

            foreach (byte[] signature in signatures)
            {
                Assert.True(VerifyData(ecdsa, dataArray, signature, hashAlgorithm), "Verify 1");
                Assert.True(ecdsa.VerifyHash(halg.ComputeHash(dataArray), signature), "Verify 4");
            }

            int distinctSignatures = signatures.Distinct(new ByteArrayComparer()).Count();
            Assert.True(distinctSignatures == signatures.Count, "Signing should be randomized");

            foreach (byte[] signature in signatures)
            {
                signature[signature.Length - 1] ^= 0xFF; // flip some bits
                Assert.False(VerifyData(ecdsa, dataArray, signature, hashAlgorithm), "Verify Tampered 1");
                Assert.False(ecdsa.VerifyHash(halg.ComputeHash(dataArray), signature), "Verify Tampered 4");
            }
        }

        private class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(byte[] obj)
            {
                int h = 5381;

                foreach (byte b in obj)
                {
                    h = unchecked((h << 5) + h) ^ b.GetHashCode();
                }

                return h;
           }
        }
    }
}
