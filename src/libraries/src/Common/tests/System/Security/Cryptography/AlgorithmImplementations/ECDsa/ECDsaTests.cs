// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public partial class ECDsaTests
    {
        public static IEnumerable<object[]> AllImplementations()
        {
            return new[] { 
                new ECDsa[] { ECDsaFactory.Create() },
                new ECDsa[] { new ECDsaStub() },
            };
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArray_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>("data",
                () => ecdsa.SignData((byte[])null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArray_DefaultHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.SignData(new byte[0], default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>("data",
                () => ecdsa.SignData(null, -1, -1, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_NegativeOffset_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentOutOfRangeException>("offset",
                () => ecdsa.SignData(new byte[0], -1, -1, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_OffsetGreaterThanCount_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentOutOfRangeException>("offset",
                () => ecdsa.SignData(new byte[0], 2, 1, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_NegativeCount_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentOutOfRangeException>("count",
                () => ecdsa.SignData(new byte[0], 0, -1, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_CountGreaterThanLengthMinusOffset_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentOutOfRangeException>("count",
                () => ecdsa.SignData(new byte[0], 0, 1, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_DefaultHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.SignData(new byte[0], 0, 0, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_EmptyHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.SignData(new byte[10], 0, 10, new HashAlgorithmName("")));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataStream_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>("data",
                () => ecdsa.SignData((Stream)null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataStream_DefaultHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.SignData(new MemoryStream(), default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArray_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>("data",
                () => ecdsa.VerifyData((byte[])null, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArray_NullSignature_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>("signature",
                () => ecdsa.VerifyData(new byte[0], null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArray_DefaultHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.VerifyData(new byte[0], new byte[0], default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>("data",
                () => ecdsa.VerifyData((byte[])null, -1, -1, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_NegativeOffset_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentOutOfRangeException>("offset",
                () => ecdsa.VerifyData(new byte[0], -1, -1, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_OffsetGreaterThanCount_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentOutOfRangeException>("offset",
                () => ecdsa.VerifyData(new byte[0], 2, 1, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_NegativeCount_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentOutOfRangeException>("count",
                () => ecdsa.VerifyData(new byte[0], 0, -1, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_CountGreaterThanLengthMinusOffset_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentOutOfRangeException>("count",
                () => ecdsa.VerifyData(new byte[0], 0, 1, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_NullSignature_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>("signature",
                () => ecdsa.VerifyData(new byte[0], 0, 0, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_EmptyHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.VerifyData(new byte[10], 0, 10, new byte[0], new HashAlgorithmName("")));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataStream_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>("data",
                () => ecdsa.VerifyData((Stream)null, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataStream_NullSignature_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>("signature",
                () => ecdsa.VerifyData(new MemoryStream(), null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataStream_DefaultHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.VerifyData(new MemoryStream(), new byte[0], default(HashAlgorithmName)));
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public static IEnumerable<object[]> RealImplementations()
        {
            return new[] { 
                new ECDsa[] { ECDsaFactory.Create() },
            };
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void SignHash_NullHash_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>(
                "hash",
                () => ecdsa.SignHash(null));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyHash_NullHash_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>(
                "hash",
                () => ecdsa.VerifyHash(null, null));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyHash_NullSignature_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            Assert.Throws<ArgumentNullException>(
                "signature",
                () => ecdsa.VerifyHash(new byte[0], null));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [Theory, MemberData(nameof(RealImplementations))]
        public void SignDataByteArray_UnsupportedHashAlgorithm_ThrowsCryptographicException(ECDsa ecdsa)
        {
            Assert.Throws<CryptographicException>(
                () => ecdsa.SignData(new byte[0], new HashAlgorithmName("NOT_A_REAL_HASH_ALGORITHM")));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void SignDataByteArraySpan_UnsupportedHashAlgorithm_ThrowsCryptographicException(ECDsa ecdsa)
        {
            Assert.Throws<CryptographicException>(
                () => ecdsa.SignData(new byte[0], 0, 0, new HashAlgorithmName("NOT_A_REAL_HASH_ALGORITHM")));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void SignDataStream_UnsupportedHashAlgorithm_ThrowsCryptographicException(ECDsa ecdsa)
        {
            Assert.Throws<CryptographicException>(
                () => ecdsa.SignData(new MemoryStream(), new HashAlgorithmName("NOT_A_REAL_HASH_ALGORITHM")));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyDataByteArray_UnsupportedHashAlgorithm_ThrowsCryptographicException(ECDsa ecdsa)
        {
            Assert.Throws<CryptographicException>(
                () => ecdsa.VerifyData(new byte[0], new byte[0], new HashAlgorithmName("NOT_A_REAL_HASH_ALGORITHM")));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyDataByteArraySpan_UnsupportedHashAlgorithm_ThrowsCryptographicException(ECDsa ecdsa)
        {
            Assert.Throws<CryptographicException>(
                () => ecdsa.VerifyData(new byte[0], 0, 0, new byte[0], new HashAlgorithmName("NOT_A_REAL_HASH_ALGORITHM")));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyDataStream_UnsupportedHashAlgorithm_ThrowsCryptographicException(ECDsa ecdsa)
        {
            Assert.Throws<CryptographicException>(
                () => ecdsa.VerifyData(new MemoryStream(), new byte[0], new HashAlgorithmName("NOT_A_REAL_HASH_ALGORITHM")));
        }
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [Theory, MemberData(nameof(RealImplementations))]
        public void SignData_MaxOffset_ZeroLength_NoThrow(ECDsa ecdsa)
        {
            // Explicitly larger than Array.Empty
            byte[] data = new byte[10];
            byte[] signature = ecdsa.SignData(data, data.Length, 0, HashAlgorithmName.SHA256);

            Assert.True(ecdsa.VerifyData(Array.Empty<byte>(), signature, HashAlgorithmName.SHA256));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyData_MaxOffset_ZeroLength_NoThrow(ECDsa ecdsa)
        {
            // Explicitly larger than Array.Empty
            byte[] data = new byte[10];
            byte[] signature = ecdsa.SignData(Array.Empty<byte>(), HashAlgorithmName.SHA256);
            
            Assert.True(ecdsa.VerifyData(data, data.Length, 0, signature, HashAlgorithmName.SHA256));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void Roundtrip_WithOffset(ECDsa ecdsa)
        {
            byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] halfData = { 5, 6, 7, 8, 9 };

            byte[] dataSignature = ecdsa.SignData(data, 5, data.Length - 5, HashAlgorithmName.SHA256);
            byte[] halfDataSignature = ecdsa.SignData(halfData, HashAlgorithmName.SHA256);

            // Cross-feed the VerifyData calls to prove that both offsets work
            Assert.True(ecdsa.VerifyData(data, 5, data.Length - 5, halfDataSignature, HashAlgorithmName.SHA256));
            Assert.True(ecdsa.VerifyData(halfData, dataSignature, HashAlgorithmName.SHA256));
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
                ecdsa.SignData(Array.Empty<byte>(), HashAlgorithmName.SHA256);
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

        [Theory, MemberData(nameof(InteroperableSignatureConfigurations))]
        public void SignVerify_InteroperableSameKeys_RoundTripsUnlessTampered(ECDsa ecdsa, HashAlgorithmName hashAlgorithm)
        {
            byte[] data = Encoding.UTF8.GetBytes("something to repeat and sign");

            // large enough to make hashing work though multiple iterations and not a multiple of 4KB it uses.
            byte[] dataArray = new byte[33333];
            MemoryStream dataStream = new MemoryStream(dataArray, true);

            while (dataStream.Position < dataArray.Length - data.Length)
            {
                dataStream.Write(data, 0, data.Length);
            }

            dataStream.Position = 0;

            byte[] dataArray2 = new byte[dataArray.Length + 2];
            dataArray.CopyTo(dataArray2, 1);
            ArraySegment<byte> dataSpan = new ArraySegment<byte>(dataArray2, 1, dataArray.Length);

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

            signatures.Add(ecdsa.SignData(dataArray, hashAlgorithm));

            signatures.Add(ecdsa.SignData(dataSpan.Array, dataSpan.Offset, dataSpan.Count, hashAlgorithm));

            signatures.Add(ecdsa.SignData(dataStream, hashAlgorithm));
            dataStream.Position = 0;

            signatures.Add(ecdsa.SignHash(halg.ComputeHash(dataArray)));

            signatures.Add(ecdsa.SignHash(halg.ComputeHash(dataSpan.Array, dataSpan.Offset, dataSpan.Count)));

            signatures.Add(ecdsa.SignHash(halg.ComputeHash(dataStream)));
            dataStream.Position = 0;

            foreach (byte[] signature in signatures)
            {
                Assert.True(ecdsa.VerifyData(dataArray, signature, hashAlgorithm), "Verify 1");

                Assert.True(ecdsa.VerifyData(dataSpan.Array, dataSpan.Offset, dataSpan.Count, signature, hashAlgorithm), "Verify 2");

                Assert.True(ecdsa.VerifyData(dataStream, signature, hashAlgorithm), "Verify 3");
                Assert.True(dataStream.Position == dataArray.Length, "Check stream read 3A");
                dataStream.Position = 0;

                Assert.True(ecdsa.VerifyHash(halg.ComputeHash(dataArray), signature), "Verify 4");

                Assert.True(ecdsa.VerifyHash(halg.ComputeHash(dataSpan.Array, dataSpan.Offset, dataSpan.Count), signature), "Verify 5");

                Assert.True(ecdsa.VerifyHash(halg.ComputeHash(dataStream), signature), "Verify 6");
                Assert.True(dataStream.Position == dataArray.Length, "Check stream read 6A");
                dataStream.Position = 0;
            }

            int distinctSignatures = signatures.Distinct(new ByteArrayComparer()).Count();
            Assert.True(distinctSignatures == signatures.Count, "Signing should be randomized");

            foreach (byte[] signature in signatures)
            {
                signature[signature.Length - 1] ^= 0xFF; // flip some bits

                Assert.False(ecdsa.VerifyData(dataArray, signature, hashAlgorithm), "Verify Tampered 1");

                Assert.False(ecdsa.VerifyData(dataSpan.Array, dataSpan.Offset, dataSpan.Count, signature, hashAlgorithm), "Verify Tampered 2");

                Assert.False(ecdsa.VerifyData(dataStream, signature, hashAlgorithm), "Verify Tampered 3");
                Assert.True(dataStream.Position == dataArray.Length, "Check stream read 3B");
                dataStream.Position = 0;

                Assert.False(ecdsa.VerifyHash(halg.ComputeHash(dataArray), signature), "Verify Tampered 4");

                Assert.False(ecdsa.VerifyHash(halg.ComputeHash(dataSpan.Array, dataSpan.Offset, dataSpan.Count), signature), "Verify Tampered 5");

                Assert.False(ecdsa.VerifyHash(halg.ComputeHash(dataStream), signature), "Verify Tampered 6");
                Assert.True(dataStream.Position == dataArray.Length, "Check stream read 6B");
                dataStream.Position = 0;
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
                    h = ((h << 5) + h) ^ b.GetHashCode();
                }

                return h;
           }
        }
    }
}
