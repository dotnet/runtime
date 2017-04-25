// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public partial class ECDsaTests : ECDsaTestsBase
    {
#if netcoreapp
        [Fact]
        public void KeySizeProp()
        {
            using (ECDsa e = ECDsaFactory.Create())
            {
                e.KeySize = 384;
                Assert.Equal(384, e.KeySize);
                ECParameters p384 = e.ExportParameters(false);
                Assert.True(p384.Curve.IsNamed);
                p384.Validate();

                e.KeySize = 521;
                Assert.Equal(521, e.KeySize);
                ECParameters p521 = e.ExportParameters(false);
                Assert.True(p521.Curve.IsNamed);
                p521.Validate();

                // Ensure the key was regenerated
                Assert.NotEqual(p384.Curve.Oid.FriendlyName, p521.Curve.Oid.FriendlyName);
            }
        }

        [Theory, MemberData(nameof(TestNewCurves))]
        public void TestRegenKeyExplicit(CurveDef curveDef)
        {
            ECParameters param, param2;
            ECDsa ec, newEc;

            using (ec = ECDsaFactory.Create(curveDef.Curve))
            {
                param = ec.ExportExplicitParameters(true);
                Assert.NotEqual(null, param.D);
                using (newEc = ECDsaFactory.Create())
                {
                    newEc.ImportParameters(param);

                    // The curve name is not flowed on explicit export\import (by design) so this excercises logic
                    // that regenerates based on current curve values
                    newEc.GenerateKey(param.Curve);
                    param2 = newEc.ExportExplicitParameters(true);

                    // Only curve should match
                    ComparePrivateKey(param, param2, false);
                    ComparePublicKey(param.Q, param2.Q, false);
                    CompareCurve(param.Curve, param2.Curve);

                    // Specify same curve name
                    newEc.GenerateKey(curveDef.Curve);
                    Assert.Equal(curveDef.KeySize, newEc.KeySize);
                    param2 = newEc.ExportExplicitParameters(true);

                    // Only curve should match
                    ComparePrivateKey(param, param2, false);
                    ComparePublicKey(param.Q, param2.Q, false);
                    CompareCurve(param.Curve, param2.Curve);

                    // Specify different curve than current
                    if (param.Curve.IsPrime)
                    {
                        if (curveDef.Curve.Oid.FriendlyName != ECCurve.NamedCurves.nistP256.Oid.FriendlyName)
                        {
                            // Specify different curve (nistP256) by explicit value
                            newEc.GenerateKey(ECCurve.NamedCurves.nistP256);
                            Assert.Equal(256, newEc.KeySize);
                            param2 = newEc.ExportExplicitParameters(true);
                            // Keys should should not match
                            ComparePrivateKey(param, param2, false);
                            ComparePublicKey(param.Q, param2.Q, false);
                            // P,X,Y (and others) should not match
                            Assert.True(param2.Curve.IsPrime);
                            Assert.NotEqual(param.Curve.Prime, param2.Curve.Prime);
                            Assert.NotEqual(param.Curve.G.X, param2.Curve.G.X);
                            Assert.NotEqual(param.Curve.G.Y, param2.Curve.G.Y);

                            // Reset back to original
                            newEc.GenerateKey(param.Curve);
                            Assert.Equal(curveDef.KeySize, newEc.KeySize);
                            ECParameters copyOfParam1 = newEc.ExportExplicitParameters(true);
                            // Only curve should match
                            ComparePrivateKey(param, copyOfParam1, false);
                            ComparePublicKey(param.Q, copyOfParam1.Q, false);
                            CompareCurve(param.Curve, copyOfParam1.Curve);

                            // Set back to nistP256
                            newEc.GenerateKey(param2.Curve);
                            Assert.Equal(256, newEc.KeySize);
                            param2 = newEc.ExportExplicitParameters(true);
                            // Keys should should not match
                            ComparePrivateKey(param, param2, false);
                            ComparePublicKey(param.Q, param2.Q, false);
                            // P,X,Y (and others) should not match
                            Assert.True(param2.Curve.IsPrime);
                            Assert.NotEqual(param.Curve.Prime, param2.Curve.Prime);
                            Assert.NotEqual(param.Curve.G.X, param2.Curve.G.X);
                            Assert.NotEqual(param.Curve.G.Y, param2.Curve.G.Y);
                        }
                    }
                    else if (param.Curve.IsCharacteristic2)
                    {
                        if (curveDef.Curve.Oid.Value != ECDSA_Sect193r1_OID_VALUE)
                        {
                            if (ECDsaFactory.IsCurveValid(new Oid(ECDSA_Sect193r1_OID_VALUE)))
                            {
                                // Specify different curve by name
                                newEc.GenerateKey(ECCurve.CreateFromValue(ECDSA_Sect193r1_OID_VALUE));
                                Assert.Equal(193, newEc.KeySize);
                                param2 = newEc.ExportExplicitParameters(true);
                                // Keys should should not match
                                ComparePrivateKey(param, param2, false);
                                ComparePublicKey(param.Q, param2.Q, false);
                                // Polynomial,X,Y (and others) should not match
                                Assert.True(param2.Curve.IsCharacteristic2);
                                Assert.NotEqual(param.Curve.Polynomial, param2.Curve.Polynomial);
                                Assert.NotEqual(param.Curve.G.X, param2.Curve.G.X);
                                Assert.NotEqual(param.Curve.G.Y, param2.Curve.G.Y);
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestCurves))]
        public void TestRegenKeyNamed(CurveDef curveDef)
        {
            ECParameters param, param2;
            ECDsa ec;

            using (ec = ECDsaFactory.Create(curveDef.Curve))
            {
                param = ec.ExportParameters(true);
                Assert.NotEqual(param.D, null);
                param.Validate();

                ec.GenerateKey(param.Curve);
                param2 = ec.ExportParameters(true);
                param2.Validate();

                // Only curve should match
                ComparePrivateKey(param, param2, false);
                ComparePublicKey(param.Q, param2.Q, false);
                CompareCurve(param.Curve, param2.Curve);
            }
        }

        [ConditionalFact(nameof(ECExplicitCurvesSupported))]
        public void TestRegenKeyNistP256()
        {
            ECParameters param, param2;
            ECDsa ec;

            using (ec = ECDsaFactory.Create(256))
            {
                param = ec.ExportExplicitParameters(true);
                Assert.NotEqual(param.D, null);

                ec.GenerateKey(param.Curve);
                param2 = ec.ExportExplicitParameters(true);

                // Only curve should match
                ComparePrivateKey(param, param2, false);
                ComparePublicKey(param.Q, param2.Q, false);
                CompareCurve(param.Curve, param2.Curve);
            }
        }

        [Theory]
        [MemberData(nameof(TestCurves))]
        public void TestChangeFromNamedCurveToKeySize(CurveDef curveDef)
        {
            using (ECDsa ec = ECDsaFactory.Create(curveDef.Curve))
            {
                ECParameters param = ec.ExportParameters(false);

                // Avoid comparing against same key as in curveDef
                if (ec.KeySize != 384 && ec.KeySize != 521)
                {
                    ec.KeySize = 384;
                    ECParameters param384 = ec.ExportParameters(false);
                    Assert.NotEqual(param.Curve.Oid.FriendlyName, param384.Curve.Oid.FriendlyName);
                    Assert.Equal(384, ec.KeySize);

                    ec.KeySize = 521;
                    ECParameters param521 = ec.ExportParameters(false);
                    Assert.NotEqual(param384.Curve.Oid.FriendlyName, param521.Curve.Oid.FriendlyName);
                    Assert.Equal(521, ec.KeySize);
                }
            }
        }

        [ConditionalFact(nameof(ECExplicitCurvesSupported))]
        public void TestPositive256WithExplicitParameters()
        {
            using (ECDsa ecdsa = ECDsaFactory.Create())
            {
                ecdsa.ImportParameters(ECDsaTestData.GetNistP256ExplicitTestData());
                Verify256(ecdsa, true);
            }
        }

        [Fact]
        public void TestNegative256WithRandomKey()
        {
            using (ECDsa ecdsa = ECDsaFactory.Create(ECCurve.NamedCurves.nistP256))
            {
                Verify256(ecdsa, false); // will not match because of randomness
            }
        }
#endif // netcoreapp

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArray_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("data",
                () => ecdsa.SignData((byte[])null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArray_DefaultHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.SignData(new byte[0], default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("data",
                () => ecdsa.SignData(null, -1, -1, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_NegativeOffset_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset",
                () => ecdsa.SignData(new byte[0], -1, -1, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_OffsetGreaterThanCount_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset",
                () => ecdsa.SignData(new byte[0], 2, 1, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_NegativeCount_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count",
                () => ecdsa.SignData(new byte[0], 0, -1, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_CountGreaterThanLengthMinusOffset_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count",
                () => ecdsa.SignData(new byte[0], 0, 1, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_DefaultHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.SignData(new byte[0], 0, 0, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataByteArraySpan_EmptyHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.SignData(new byte[10], 0, 10, new HashAlgorithmName("")));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataStream_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("data",
                () => ecdsa.SignData((Stream)null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void SignDataStream_DefaultHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.SignData(new MemoryStream(), default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArray_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("data",
                () => ecdsa.VerifyData((byte[])null, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArray_NullSignature_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("signature",
                () => ecdsa.VerifyData(new byte[0], null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArray_DefaultHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.VerifyData(new byte[0], new byte[0], default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("data",
                () => ecdsa.VerifyData((byte[])null, -1, -1, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_NegativeOffset_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset",
                () => ecdsa.VerifyData(new byte[0], -1, -1, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_OffsetGreaterThanCount_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset",
                () => ecdsa.VerifyData(new byte[0], 2, 1, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_NegativeCount_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count",
                () => ecdsa.VerifyData(new byte[0], 0, -1, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_CountGreaterThanLengthMinusOffset_ThrowsArgumentOutOfRangeException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count",
                () => ecdsa.VerifyData(new byte[0], 0, 1, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_NullSignature_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("signature",
                () => ecdsa.VerifyData(new byte[0], 0, 0, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataByteArraySpan_EmptyHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.VerifyData(new byte[10], 0, 10, new byte[0], new HashAlgorithmName("")));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataStream_NullData_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("data",
                () => ecdsa.VerifyData((Stream)null, null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataStream_NullSignature_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>("signature",
                () => ecdsa.VerifyData(new MemoryStream(), null, default(HashAlgorithmName)));
        }

        [Theory, MemberData(nameof(AllImplementations))]
        public void VerifyDataStream_DefaultHashAlgorithm_ThrowsArgumentException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm",
                () => ecdsa.VerifyData(new MemoryStream(), new byte[0], default(HashAlgorithmName)));
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static IEnumerable<object[]> AllImplementations()
        {
            return new[] {
                new ECDsa[] { ECDsaFactory.Create() },
                new ECDsa[] { new ECDsaStub() },
            };
        }

        public static IEnumerable<object[]> RealImplementations()
        {
            return new[] { 
                new ECDsa[] { ECDsaFactory.Create() },
            };
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void SignHash_NullHash_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "hash",
                () => ecdsa.SignHash(null));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyHash_NullHash_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "hash",
                () => ecdsa.VerifyHash(null, null));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyHash_NullSignature_ThrowsArgumentNullException(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentNullException>(
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

        [Theory]
        [MemberData(nameof(RealImplementations))]
        public void SignData_MaxOffset_ZeroLength_NoThrow(ECDsa ecdsa)
        {
            // Explicitly larger than Array.Empty
            byte[] data = new byte[10];
            byte[] signature = ecdsa.SignData(data, data.Length, 0, HashAlgorithmName.SHA256);

            Assert.True(ecdsa.VerifyData(Array.Empty<byte>(), signature, HashAlgorithmName.SHA256));
        }

        [Theory]
        [MemberData(nameof(RealImplementations))]
        public void VerifyData_MaxOffset_ZeroLength_NoThrow(ECDsa ecdsa)
        {
            // Explicitly larger than Array.Empty
            byte[] data = new byte[10];
            byte[] signature = ecdsa.SignData(Array.Empty<byte>(), HashAlgorithmName.SHA256);
            
            Assert.True(ecdsa.VerifyData(data, data.Length, 0, signature, HashAlgorithmName.SHA256));
        }

        [Theory]
        [MemberData(nameof(RealImplementations))]
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

#if netcoreapp // uses ECParameters not available in desktop.
        [Fact]
        public void PublicKey_CannotSign()
        {
            using (ECDsa ecdsaPriv = ECDsaFactory.Create())
            using (ECDsa ecdsa = ECDsaFactory.Create())
            {
                ECParameters keyParameters = ecdsaPriv.ExportParameters(false);
                ecdsa.ImportParameters(keyParameters);

                Assert.ThrowsAny<CryptographicException>(
                    () => ecdsa.SignData(new byte[] { 1, 2, 3, 4, 5 }, HashAlgorithmName.SHA256));
            }
        }
#endif

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

        [Theory]
        [MemberData(nameof(InteroperableSignatureConfigurations))]
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
                    h = unchecked((h << 5) + h) ^ b.GetHashCode();
                }

                return h;
           }
        }
    }
}
