// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Dsa.Tests;
using Test.Cryptography;
using Xunit;


namespace System.Security.Cryptography.Algorithms.Tests
{
    public abstract class DsaEcDsaSignatureFormatTests
    {
        private static readonly HashAlgorithmName[] s_hashNames =
        {
            HashAlgorithmName.SHA256,
            HashAlgorithmName.SHA512
        };

        // 1 test case per hash
        private static readonly int NumberOfTestCases = s_hashNames.Length;

        /// <summary>
        /// Is it signing or verifying?
        /// </summary>
        /// <returns>True if signing and False if verifying</returns>
        public abstract bool IsSigner();

        /// <summary>
        /// Is it related to EcDsa or DSA
        /// </summary>
        /// <returns>True for EcDsa and False for DSA</returns>
        public abstract bool IsEcDsa();

        /// <summary>
        /// Is it signing/verifying data or hash?
        /// </summary>
        /// <returns>True for signing/verifying data, False for hash</returns>
        public abstract bool IsOperatingOnData();

        /// <summary>
        /// Does data/hash/signature accept null?
        /// </summary>
        public abstract bool IsAcceptingNullInput();

        /// <summary>
        /// Does API take byte array with offset and length as the input?
        /// </summary>
        public abstract bool IsAcceptingOffsetAndLength();

        /// <summary>
        /// What's the name of the argument?
        /// </summary>
        public virtual string DataOrHashArgumentName() => IsOperatingOnData() ? "data" : "hash";


        protected static readonly HashAlgorithmName[] s_invalidHashAlgorithmNames = {
            new HashAlgorithmName(null),
            new HashAlgorithmName(""),
            new HashAlgorithmName("NonExistingHash")
        };

        protected static int[] GetInvalidOffsets(int length)
        {
            return new int[]
                {
                    -1,
                    length + 1,
                    int.MinValue,
                    int.MaxValue,
                };
        }

        protected static (int, int)[] GetInvalidOffsetsAndCounts(int length)
        {
            return new (int, int)[]
                {
                    (0, length + 1),
                    (1, length),
                    (length, 1),
                    (length - 1, 2),
                };
        }

        public static IEnumerable<object[]> XunitTestCases()
        {
            return TestCases(NumberOfTestCases).Select((tc) => new object[] { tc });
        }

        protected TestData AnyTestCase(int minLength = 0)
        {
            return GetTestData(TestCases(1, minLength).Single());
        }

        private static readonly ECDsa[] s_ecDsaKeys =
        {
            ECDsa.Create(),
            ECDsa.Create(),
            ECDsa.Create(),
            ECDsa.Create(),
        };

        private static readonly DSA[] s_dsaKeys =
        {
            DSA.Create(DSATestData.GetDSA1024Params()),
            DSA.Create(DSATestData.Dsa512Parameters),
            DSA.Create(DSATestData.Dsa576Parameters),
            // If the platform doesn't support FIPS 186-3 (macOS), just use the 1024 key again.
            DSA.Create(DSAFactory.SupportsFips186_3 ? DSATestData.GetDSA2048Params() : DSATestData.GetDSA1024Params()),
        };

        private static AsymmetricAlgorithm GetKey(int keyId, bool isEcDsa)
        {
            Assert.True(keyId >= 0);
            AsymmetricAlgorithm[] keys = isEcDsa ? (AsymmetricAlgorithm[])s_ecDsaKeys : s_dsaKeys;
            return keys[keyId % keys.Length];
        }

        protected TestData GetTestData(TestDataInfo testInfo)
        {
            // Cloning data so that tests can freely modify it while we can pass original reference to test info
            return TestData.Create(
                GetKey(testInfo.KeyId, IsEcDsa()),
                testInfo.Hash,
                testInfo.Data.ToArray(),
                doNotComputeSignatures: IsSigner());
        }

        private static IEnumerable<TestDataInfo> TestCases(int n, int minLength = 0)
        {
            var rand = new Random(n);

            for (int i = 0; i < n; i++)
            {
                byte[] data = new byte[minLength + rand.Next(1024)];
                rand.NextBytes(data);
                HashAlgorithmName hashName = s_hashNames[i % s_hashNames.Length];
                yield return new TestDataInfo(n, data, hashName);
            }
        }

        private static byte[] DSAEcDsaSignHash(AsymmetricAlgorithm key, byte[] hash, DSASignatureFormat signatureFormat)
        {
            if (key is ECDsa ecdsa)
            {
                return ecdsa.SignHash(hash, signatureFormat);
            }
            else if (key is DSA dsa)
            {
                return dsa.CreateSignature(hash, signatureFormat);
            }
            else
            {
                throw new Exception($"Unexpected asymmetric algorithm: {key.GetType()}");
            }
        }

        /// <summary>
        /// Signs data or hash (depending on API being tested)
        /// For verifiers this method will return a corresponding signature from test data
        /// </summary>
        /// <param name="data">Test data</param>
        /// <returns>Signature</returns>
        public virtual byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
        {
            Assert.False(IsSigner(), "This method must be overriden by signers");
            return data.Signature(signatureFormat);
        }

        /// <summary>
        /// Verifies hash or data
        /// </summary>
        /// <param name="test">test data</param>
        /// <param name="signature">signature to verify</param>
        /// <param name="signatureFormat">format of the provided signature</param>
        /// <returns>True, if signature is correctly verified or False otherwise</returns>
        public virtual bool Verify(TestData test, byte[] signature, DSASignatureFormat signatureFormat)
        {
            Assert.True(IsSigner(), "This method must be overriden by verifiers");

            if (test.Key is ECDsa ecdsa)
            {
                if (IsOperatingOnData())
                {
                    return ecdsa.VerifyData(test.Data, signature, test.Hash, signatureFormat);
                }
                else
                {
                    return ecdsa.VerifyHash(test.DataHash, signature, signatureFormat);
                }
            }
            else if (test.Key is DSA dsa)
            {
                if (IsOperatingOnData())
                {
                    return dsa.VerifyData(test.Data, signature, test.Hash, signatureFormat);
                }
                else
                {
                    return dsa.VerifySignature(test.DataHash, signature, signatureFormat);
                }
            }
            else
            {
                throw new Exception($"Unexpected asymmetric algorithm: {test.Key.GetType()}");
            }
        }

        public byte[] GetDataOrHash(TestData test)
        {
            return IsOperatingOnData() ? test.Data : test.DataHash;
        }

        protected void AssertThrowsForInvalidHashAlgorithm(HashAlgorithmName hash, Action action)
        {
            if (string.IsNullOrEmpty(hash.Name))
            {
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", action);
            }
            else
            {
                Assert.ThrowsAny<CryptographicException>(action);
            }
        }

        protected void AssertThrowsForNullDataOrHash(Action action)
        {
            AssertExtensions.Throws<ArgumentNullException>(DataOrHashArgumentName(), action);
        }

        [Theory]
        [MemberData(nameof(XunitTestCases))]
        public void SignatureEncodedAsIeeeP1363CanBeVerified(TestDataInfo testInfo)
        {
            TestData test = GetTestData(testInfo);
            byte[] signature = Sign(test, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            Assert.True(signature.Length % 2 == 0, "Signature length must be an even number");
            Assert.True(Verify(test, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        [Theory]
        [MemberData(nameof(XunitTestCases))]
        public void SignatureEncodedAsRfc3279CanBeVerified(TestDataInfo testInfo)
        {
            TestData test = GetTestData(testInfo);
            byte[] signature = Sign(test, DSASignatureFormat.Rfc3279DerSequence);

            // First byte must ASN.1 sequence tag (0x30)
            Assert.Equal(0x30, signature[0]);

            Assert.True(Verify(test, signature, DSASignatureFormat.Rfc3279DerSequence));
        }

        [Theory]
        [InlineData(DSASignatureFormat.IeeeP1363FixedFieldConcatenation)]
        [InlineData(DSASignatureFormat.Rfc3279DerSequence)]
        public void ValidOffsetAndCountSignsAndVerifies(DSASignatureFormat signatureFormat)
        {
            if (!IsAcceptingOffsetAndLength())
            {
                return;
            }

            Assert.True(IsOperatingOnData());

            TestData test = AnyTestCase(minLength: 5);
            byte[] data = test.Data;

            void SetData(bool useByteArray, int offset, int count)
            {
                if (useByteArray)
                {
                    test.Data = data.AsSpan(offset, count).ToArray();
                    test.DataOffset = 0;
                    test.DataLength = count;
                }
                else
                {
                    test.Data = data;
                    test.DataOffset = offset;
                    test.DataLength = count;
                }
            }

            (int, int)[] offsetsAndCounts = new (int, int)[]
            {
                (1, data.Length - 1),
                (1, 3)
            };

            foreach ((int offset, int count) in offsetsAndCounts)
            {
                SetData(!IsSigner(), offset, count);

                if (!IsSigner())
                {
                    test.UpdateSignature(signatureFormat);
                }

                byte[] sig = Sign(test, signatureFormat);

                SetData(IsSigner(), offset, count);
                Assert.True(Verify(test, sig, signatureFormat));
            }
        }

        /// <summary>
        /// Used for providing different test cases depending on type executing tests: EcDSA/DSA
        /// also used to re-use same keys and speed up the tests.
        /// </summary>
        public class TestDataInfo
        {
            public TestDataInfo(int keyId, byte[] data, HashAlgorithmName hash)
            {
                KeyId = keyId;
                Data = data;
                Hash = hash;
            }

            public int KeyId { get; private set; }
            public byte[] Data { get; private set; }
            public HashAlgorithmName Hash { get; set; }

            public override string ToString()
            {
                return $"{Hash}, KeyId={KeyId} Data={Data.ByteArrayToHex()}";
            }
        }

        public class TestData
        {
            private byte[] _ieeeP1363Signature;
            private byte[] _rfc3279Signature;

            public AsymmetricAlgorithm Key { get; private set; }
            public ECDsa ECDsaKey => (ECDsa)Key;
            public DSA DSAKey => (DSA)Key;
            public HashAlgorithmName Hash { get; set; }
            public byte[] Data { get; set; }
            public byte[] DataHash { get; private set; }
            public int DataOffset { get; set; } = 0;
            public int DataLength { get; set; }

            public static TestData Create(AsymmetricAlgorithm key, HashAlgorithmName hashName, byte[] data, bool doNotComputeSignatures)
            {
                byte[] dataHash = GetHash(hashName, data);

                return new TestData(
                    key,
                    hashName,
                    data,
                    dataHash,
                    doNotComputeSignatures ? null : DSAEcDsaSignHash(key, dataHash, DSASignatureFormat.IeeeP1363FixedFieldConcatenation),
                    doNotComputeSignatures ? null : DSAEcDsaSignHash(key, dataHash, DSASignatureFormat.Rfc3279DerSequence));
            }

            private TestData(AsymmetricAlgorithm key, HashAlgorithmName hash, byte[] data, byte[] dataHash, byte[] ieeeP1363Signature, byte[] rfc3279Signature)
            {
                Key = key;
                Data = data;
                DataHash = dataHash;
                Hash = hash;
                _ieeeP1363Signature = ieeeP1363Signature;
                _rfc3279Signature = rfc3279Signature;

                DataLength = Data == null ? 0 : Data.Length;
            }

            public void UpdateHash()
            {
                DataHash = GetHash(Hash, Data.AsSpan(DataOffset, DataLength).ToArray());
            }

            public void UpdateSignature(DSASignatureFormat signatureFormat)
            {
                UpdateHash();

                switch (signatureFormat)
                {
                    case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
                        _ieeeP1363Signature = DSAEcDsaSignHash(Key, DataHash, signatureFormat);
                        break;
                    case DSASignatureFormat.Rfc3279DerSequence:
                        _rfc3279Signature = DSAEcDsaSignHash(Key, DataHash, signatureFormat);
                        break;
                    default:
                        Assert.True(false, $"Invalid signature format: {signatureFormat}");
                        break;
                }
            }

            public void NullifyDataAndHash()
            {
                Data = null;
                DataHash = null;
            }

            private static byte[] GetHash(HashAlgorithmName hashName, byte[] data)
            {
                using (IncrementalHash hash = IncrementalHash.CreateHash(hashName))
                {
                    hash.AppendData(data);
                    return hash.GetHashAndReset();
                }
            }

            public byte[] Signature(DSASignatureFormat signatureFormat)
            {
                switch (signatureFormat)
                {
                    case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
                        return _ieeeP1363Signature;
                    case DSASignatureFormat.Rfc3279DerSequence:
                        return _rfc3279Signature;
                    default:
                        Assert.True(false, $"Signature format: {signatureFormat} is unknown");
                        throw null;
                }
            }
        }

        public abstract class Signer : DsaEcDsaSignatureFormatTests
        {
            private int _signatureSizeOverride = -1;

            public override bool IsSigner() => true;
            public abstract bool NeedsSignatureSizeEstimate();

            private Action<bool> _judgeTrySignResult = null;

            public int GetMaxSignatureSize(AsymmetricAlgorithm key, DSASignatureFormat signatureFormat)
            {
                Assert.True(NeedsSignatureSizeEstimate(), "GetMaxSignatureSize should not be called when NeedsSignatureSizeEstimate returns false");
                if (_signatureSizeOverride != -1)
                {
                    return _signatureSizeOverride;
                }

                return DefaultGetMaxSignatureSize(key, signatureFormat);
            }

            public void WithMaxSignatureSizeReturning(int maxSignatureSizeResult, Action<bool> judgeTrySignResult, Action action)
            {
                Assert.Equal(-1, _signatureSizeOverride);
                Assert.Null(_judgeTrySignResult);

                bool judgeCalled = false;

                try
                {
                    _signatureSizeOverride = maxSignatureSizeResult;
                    _judgeTrySignResult = (result) =>
                    {
                        judgeCalled = true;
                        judgeTrySignResult(result);
                    };

                    action();
                }
                finally
                {
                    _signatureSizeOverride = -1;
                    _judgeTrySignResult = null;
                    Assert.True(judgeCalled, "TrySign* did not call JudgeTrySignResult");
                }
            }

            protected void JudgeTrySignResult(bool result)
            {
                if  (_judgeTrySignResult != null)
                {
                    _judgeTrySignResult(result);
                }
                else
                {
                    Assert.True(result, "TrySign* unexpectedly returned false");
                }
            }

            private static int DefaultGetMaxSignatureSize(AsymmetricAlgorithm key, DSASignatureFormat signatureFormat)
            {
                if (key is DSA dsaKey)
                {
                    return dsaKey.GetMaxSignatureSize(signatureFormat);
                }
                else if (key is ECDsa ecdsaKey)
                {
                    return ecdsaKey.GetMaxSignatureSize(signatureFormat);
                }
                else
                {
                    Assert.True(false, "invalid key");
                    return -1;
                }
            }

            [Fact]
            public void InvalidHashAlgorithmNameThrows()
            {
                if (!IsOperatingOnData())
                {
                    // Nothing interesting to test here
                    return;
                }

                TestData test = AnyTestCase();
                Action sign = () => Sign(test, DSASignatureFormat.Rfc3279DerSequence);

                foreach (HashAlgorithmName invalidHash in s_invalidHashAlgorithmNames)
                {
                    test.Hash = invalidHash;
                    AssertThrowsForInvalidHashAlgorithm(invalidHash, sign);
                }
            }

            [Fact]
            public void NullDataOrHashThrows()
            {
                if (!IsAcceptingNullInput())
                {
                    return;
                }

                TestData test = AnyTestCase();
                Action sign = () => Sign(test, DSASignatureFormat.Rfc3279DerSequence);

                test.NullifyDataAndHash();

                AssertThrowsForNullDataOrHash(sign);
            }

            [Fact]
            public void InvalidSignatureFormatThrows()
            {
                TestData test = AnyTestCase();
                AssertExtensions.Throws<ArgumentOutOfRangeException>("signatureFormat",
                    () => Sign(test, (DSASignatureFormat)(-1)));
            }

            [Fact]
            public void InvalidOffsetThrows()
            {
                if (!IsAcceptingOffsetAndLength())
                {
                    return;
                }

                TestData test = AnyTestCase();

                foreach (int offset in GetInvalidOffsets(test.DataLength))
                {
                    test.DataOffset = offset;
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("offset",
                        () => Sign(test, DSASignatureFormat.Rfc3279DerSequence));
                }
            }

            [Fact]
            public void InvalidCountThrows()
            {
                if (!IsAcceptingOffsetAndLength())
                {
                    return;
                }

                TestData test = AnyTestCase(minLength: 2);

                foreach ((int offset, int count) in GetInvalidOffsetsAndCounts(test.DataLength))
                {
                    test.DataOffset = offset;
                    test.DataLength = count;
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("count",
                        () => Sign(test, DSASignatureFormat.Rfc3279DerSequence));
                }
            }

            [Theory]
            [MemberData(nameof(XunitTestCases))]
            public void GetMaxSignatureSizeReturnsLargeEnoughNumber(TestDataInfo testInfo)
            {
                if (!NeedsSignatureSizeEstimate())
                {
                    return;
                }

                DSASignatureFormat[] signatureFormats = new DSASignatureFormat[]
                {
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation,
                    DSASignatureFormat.Rfc3279DerSequence,
                };

                TestData test = GetTestData(testInfo);

                // Since signature can be slightly different size each time
                // let's try enough times that we can be confident enough to say we always returned enough from GetMaxSignatureSize
                for (int i = 0; i < 100; i++)
                {
                    foreach (DSASignatureFormat signatureFormat in signatureFormats)
                    {
                        byte[] sig = Sign(test, signatureFormat);
                        Assert.True(Verify(test, sig, signatureFormat), $"Signature did not verify. Signature format: {signatureFormat}");
                    }
                }
            }

            [Theory]
            [InlineData(DSASignatureFormat.IeeeP1363FixedFieldConcatenation)]
            [InlineData(DSASignatureFormat.Rfc3279DerSequence)]
            public void InsufficientSignatureSizeThrows(DSASignatureFormat signatureFormat)
            {
                if (!NeedsSignatureSizeEstimate())
                {
                    return;
                }

                TestData test = AnyTestCase();
                WithMaxSignatureSizeReturning(Math.Max(0, GetMaxSignatureSize(test.Key, signatureFormat) - 20),
                    (result) => Assert.False(result),
                    () =>
                    {
                        byte[] sig = Sign(test, signatureFormat);
                        Assert.Equal(0, sig.Length);
                    });
            }
        }

        public abstract class DsaSigner : Signer
        {
            public override bool IsEcDsa() => false;
        }

        public abstract class EcDsaSigner : Signer
        {
            public override bool IsEcDsa() => true;
        }

        public abstract class Verifier : DsaEcDsaSignatureFormatTests
        {
            public override bool IsSigner() => false;
            public virtual string SignatureArgumentName() => "signature";

            // Verify method is already defined in the base class
            // It will throw for any instance which is not a signer

            [Theory]
            [MemberData(nameof(XunitTestCases))]
            public void SignatureWithDifferentFormatCannotBeVerified1(TestDataInfo testInfo)
            {
                TestData test = GetTestData(testInfo);
                byte[] signature = Sign(test, DSASignatureFormat.Rfc3279DerSequence);
                Assert.False(Verify(test, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
            }

            [Theory]
            [MemberData(nameof(XunitTestCases))]
            public void SignatureWithDifferentFormatCannotBeVerified2(TestDataInfo testInfo)
            {
                TestData test = GetTestData(testInfo);
                byte[] signature = Sign(test, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
                Assert.False(Verify(test, signature, DSASignatureFormat.Rfc3279DerSequence));
            }

            [Theory]
            [MemberData(nameof(XunitTestCases))]
            public void TamperedSignatureRfc3279CannotBeVerified(TestDataInfo testInfo)
            {
                TestData test = GetTestData(testInfo);
                byte[] signature = Sign(test, DSASignatureFormat.Rfc3279DerSequence);

                for (int i = 0; i < signature.Length; i++)
                {
                    // modify byte
                    signature[i] ^= 1;

                    Assert.False(Verify(test, signature, DSASignatureFormat.Rfc3279DerSequence), $"Tampered signature is validated as correct. Tampered byte index: {i}");

                    // revert modification
                    signature[i] ^= 1;
                }

                // Ensure every modification is undone and signature verifies
                Assert.True(Verify(test, signature, DSASignatureFormat.Rfc3279DerSequence), "Signature is not the same as original after undoing tampering");
            }

            [Theory]
            [MemberData(nameof(XunitTestCases))]
            public void TamperedDataOrHashCannotBeVerified(TestDataInfo testInfo)
            {
                TestData test = GetTestData(testInfo);
                byte[] dataOrHash = GetDataOrHash(test);
                byte[] signature = test.Signature(DSASignatureFormat.Rfc3279DerSequence);

                int dataOrHashLength = dataOrHash.Length;

                if (!IsOperatingOnData() && !IsEcDsa())
                {
                    // for DSA hash will be truncated to the Q size
                    dataOrHashLength = Math.Min(test.DSAKey.ExportParameters(false).Q.Length, dataOrHashLength);
                }

                for (int i = 0; i < dataOrHashLength; i++)
                {
                    // modify byte
                    dataOrHash[i] ^= 1;

                    if (IsOperatingOnData())
                    {
                        test.UpdateHash();
                    }

                    Assert.False(Verify(test, signature, DSASignatureFormat.Rfc3279DerSequence), $"Tampered data/hash is validated as correct. Tampered byte index: {i}");

                    // revert modification
                    dataOrHash[i] ^= 1;
                }

                // Ensure every modification is undone and signature verifies
                if (IsOperatingOnData())
                {
                    test.UpdateHash();
                }

                Assert.True(Verify(test, signature, DSASignatureFormat.Rfc3279DerSequence), "Data/hash is not the same as original after undoing tampering");
            }

            [Fact]
            public void InvalidHashAlgorithmNameThrows()
            {
                if (!IsOperatingOnData())
                {
                    // Nothing interesting to test here
                    return;
                }

                TestData test = AnyTestCase();
                byte[] signature = test.Signature(DSASignatureFormat.Rfc3279DerSequence);

                Action verify = () => Verify(test, signature, DSASignatureFormat.Rfc3279DerSequence);

                foreach (HashAlgorithmName invalidHash in s_invalidHashAlgorithmNames)
                {
                    test.Hash = invalidHash;
                    AssertThrowsForInvalidHashAlgorithm(invalidHash, verify);
                }
            }

            [Fact]
            public void NullDataOrHashThrows()
            {
                if (!IsAcceptingNullInput())
                {
                    return;
                }

                TestData test = AnyTestCase();
                Action verify = () => Verify(test, test.Signature(DSASignatureFormat.Rfc3279DerSequence), DSASignatureFormat.Rfc3279DerSequence);

                test.NullifyDataAndHash();
                AssertThrowsForNullDataOrHash(verify);
            }

            [Theory]
            [InlineData(DSASignatureFormat.Rfc3279DerSequence)]
            [InlineData(DSASignatureFormat.IeeeP1363FixedFieldConcatenation)]
            public void NullSignatureThrows(DSASignatureFormat signatureFormat)
            {
                if (!IsAcceptingNullInput())
                {
                    return;
                }

                TestData test = AnyTestCase();
                AssertExtensions.Throws<ArgumentNullException>(SignatureArgumentName(), () => Verify(test, null, signatureFormat));
            }

            [Fact]
            public void InvalidSignatureFormatThrows()
            {
                TestData test = AnyTestCase();
                AssertExtensions.Throws<ArgumentOutOfRangeException>("signatureFormat",
                    () => Verify(test, new byte[0], (DSASignatureFormat)(-1)));
            }

            [Fact]
            public void InvalidOffsetThrows()
            {
                if (!IsAcceptingOffsetAndLength())
                {
                    return;
                }

                TestData test = AnyTestCase();

                foreach (int offset in GetInvalidOffsets(test.DataLength))
                {
                    test.DataOffset = offset;
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("offset",
                        () => Verify(test, test.Signature(DSASignatureFormat.Rfc3279DerSequence), DSASignatureFormat.Rfc3279DerSequence));
                }
            }

            [Fact]
            public void InvalidCountThrows()
            {
                if (!IsAcceptingOffsetAndLength())
                {
                    return;
                }

                TestData test = AnyTestCase(minLength: 2);

                foreach ((int offset, int count) in GetInvalidOffsetsAndCounts(test.DataLength))
                {
                    test.DataOffset = offset;
                    test.DataLength = count;
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("count",
                        () => Verify(test, test.Signature(DSASignatureFormat.Rfc3279DerSequence), DSASignatureFormat.Rfc3279DerSequence));
                }
            }
        }

        public abstract class DsaVerifier : Verifier
        {
            public override bool IsEcDsa() => false;
        }

        public abstract class EcDsaVerifier : Verifier
        {
            public override bool IsEcDsa() => true;
        }

        #region DSA : Sign
        public class DSA_SignData_ByteArrayAndOffset : DsaSigner
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => true;
            public override bool NeedsSignatureSizeEstimate() => false;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                return data.DSAKey.SignData(data.Data, data.DataOffset, data.DataLength, data.Hash, signatureFormat);
            }
        }

        public class DSA_SignData_ByteArray : DsaSigner
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override bool NeedsSignatureSizeEstimate() => false;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                return data.DSAKey.SignData(data.Data, data.Hash, signatureFormat);
            }
        }

        public class DSA_SignData_Stream : DsaSigner
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override bool NeedsSignatureSizeEstimate() => false;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                using (MemoryStream ms = data.Data == null ? null : new MemoryStream(data.Data))
                {
                    return data.DSAKey.SignData(ms, data.Hash, signatureFormat);
                }
            }
        }

        public class DSA_TrySignData_Span : DsaSigner
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => false;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override bool NeedsSignatureSizeEstimate() => true;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                byte[] result = new byte[GetMaxSignatureSize(data.Key, signatureFormat)];
                JudgeTrySignResult(data.DSAKey.TrySignData(data.Data, result, data.Hash, signatureFormat, out int bytesWritten));
                result = result.AsSpan(0, bytesWritten).ToArray();
                return result;
            }
        }

        public class DSA_CreateSignature_ByteArray : DsaSigner
        {
            public override bool IsOperatingOnData() => false;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override bool NeedsSignatureSizeEstimate() => false;
            public override string DataOrHashArgumentName() => "rgbHash";

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                return data.DSAKey.CreateSignature(data.DataHash, signatureFormat);
            }
        }

        public class DSA_TryCreateSignature_Span : DsaSigner
        {
            public override bool IsOperatingOnData() => false;
            public override bool IsAcceptingNullInput() => false;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override bool NeedsSignatureSizeEstimate() => true;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                byte[] result = new byte[GetMaxSignatureSize(data.Key, signatureFormat)];
                JudgeTrySignResult(data.DSAKey.TryCreateSignature(data.DataHash, result, signatureFormat, out int bytesWritten));
                result = result.AsSpan(0, bytesWritten).ToArray();
                return result;
            }
        }
        #endregion

        #region DSA : Verify
        public class DSA_VerifyData_ByteArray : DsaVerifier
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                return data.DSAKey.VerifyData(data.Data, signature, data.Hash, signatureFormat);
            }
        }

        public class DSA_VerifyData_ByteArrayAndOffset : DsaVerifier
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => true;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                return data.DSAKey.VerifyData(data.Data, data.DataOffset, data.DataLength, signature, data.Hash, signatureFormat);
            }
        }

        public class DSA_VerifyData_Stream : DsaVerifier
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                using (MemoryStream ms = data.Data == null ? null : new MemoryStream(data.Data))
                {
                    return data.DSAKey.VerifyData(ms, signature, data.Hash, signatureFormat);
                }
            }
        }

        public class DSA_VerifyData_Span : DsaVerifier
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => false;
            public override bool IsAcceptingOffsetAndLength() => false;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                return data.DSAKey.VerifyData((ReadOnlySpan<byte>)data.Data, (ReadOnlySpan<byte>)signature, data.Hash, signatureFormat);
            }
        }

        public class DSA_VerifySignature_ByteArray : DsaVerifier
        {
            public override bool IsOperatingOnData() => false;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override string DataOrHashArgumentName() => "rgbHash";
            public override string SignatureArgumentName() => "rgbSignature";

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                return data.DSAKey.VerifySignature(data.DataHash, signature, signatureFormat);
            }
        }

        public class DSA_VerifySignature_Span : DsaVerifier
        {
            public override bool IsOperatingOnData() => false;
            public override bool IsAcceptingNullInput() => false;
            public override bool IsAcceptingOffsetAndLength() => false;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                return data.DSAKey.VerifySignature((ReadOnlySpan<byte>)data.DataHash, (ReadOnlySpan<byte>)signature, signatureFormat);
            }
        }
        #endregion

        #region ECDSA : Sign
        public class ECDsa_SignData_ByteArrayAndOffset : EcDsaSigner
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => true;
            public override bool NeedsSignatureSizeEstimate() => false;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                return data.ECDsaKey.SignData(data.Data, data.DataOffset, data.DataLength, data.Hash, signatureFormat);
            }
        }

        public class ECDsa_SignData_ByteArray : EcDsaSigner
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override bool NeedsSignatureSizeEstimate() => false;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                return data.ECDsaKey.SignData(data.Data, data.Hash, signatureFormat);
            }
        }

        public class ECDsa_SignData_Stream : EcDsaSigner
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override bool NeedsSignatureSizeEstimate() => false;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                using (MemoryStream ms = data.Data == null ? null : new MemoryStream(data.Data))
                {
                    return data.ECDsaKey.SignData(ms, data.Hash, signatureFormat);
                }
            }
        }

        public class ECDsa_TrySignData_Span : EcDsaSigner
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => false;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override bool NeedsSignatureSizeEstimate() => true;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                byte[] result = new byte[GetMaxSignatureSize(data.Key, signatureFormat)];
                JudgeTrySignResult(data.ECDsaKey.TrySignData(data.Data, result, data.Hash, signatureFormat, out int bytesWritten));
                result = result.AsSpan(0, bytesWritten).ToArray();
                return result;
            }
        }

        public class ECDsa_SignHash_ByteArray : EcDsaSigner
        {
            public override bool IsOperatingOnData() => false;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override bool NeedsSignatureSizeEstimate() => false;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                return data.ECDsaKey.SignHash(data.DataHash, signatureFormat);
            }
        }

        public class ECDsa_TrySignHash_Span : EcDsaSigner
        {
            public override bool IsOperatingOnData() => false;
            public override bool IsAcceptingNullInput() => false;
            public override bool IsAcceptingOffsetAndLength() => false;
            public override bool NeedsSignatureSizeEstimate() => true;

            public override byte[] Sign(TestData data, DSASignatureFormat signatureFormat)
            {
                byte[] result = new byte[GetMaxSignatureSize(data.Key, signatureFormat)];
                JudgeTrySignResult(data.ECDsaKey.TrySignHash(data.DataHash, result, signatureFormat, out int bytesWritten));
                result = result.AsSpan(0, bytesWritten).ToArray();
                return result;
            }
        }
        #endregion

        #region ECDSA : Verify
        public class ECDsa_VerifyData_ByteArray : EcDsaVerifier
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                return data.ECDsaKey.VerifyData(data.Data, signature, data.Hash, signatureFormat);
            }
        }

        public class ECDsa_VerifyData_ByteArrayAndOffset : EcDsaVerifier
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => true;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                return data.ECDsaKey.VerifyData(data.Data, data.DataOffset, data.DataLength, signature, data.Hash, signatureFormat);
            }
        }

        public class ECDsa_VerifyData_Stream : EcDsaVerifier
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                using (MemoryStream ms = data.Data == null ? null : new MemoryStream(data.Data))
                {
                    return data.ECDsaKey.VerifyData(ms, signature, data.Hash, signatureFormat);
                }
            }
        }

        public class ECDsa_VerifyData_Span : EcDsaVerifier
        {
            public override bool IsOperatingOnData() => true;
            public override bool IsAcceptingNullInput() => false;
            public override bool IsAcceptingOffsetAndLength() => false;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                return data.ECDsaKey.VerifyData((ReadOnlySpan<byte>)data.Data, (ReadOnlySpan<byte>)signature, data.Hash, signatureFormat);
            }
        }

        public class ECDsa_VerifyHash_ByteArray : EcDsaVerifier
        {
            public override bool IsOperatingOnData() => false;
            public override bool IsAcceptingNullInput() => true;
            public override bool IsAcceptingOffsetAndLength() => false;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                return data.ECDsaKey.VerifyHash(data.DataHash, signature, signatureFormat);
            }
        }

        public class ECDsa_VerifyHash_Span : EcDsaVerifier
        {
            public override bool IsOperatingOnData() => false;
            public override bool IsAcceptingNullInput() => false;
            public override bool IsAcceptingOffsetAndLength() => false;

            public override bool Verify(TestData data, byte[] signature, DSASignatureFormat signatureFormat)
            {
                return data.ECDsaKey.VerifyHash((ReadOnlySpan<byte>)data.DataHash, (ReadOnlySpan<byte>)signature, signatureFormat);
            }
        }
        #endregion
    }
}
