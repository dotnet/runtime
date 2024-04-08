// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Security.Cryptography.Algorithms.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Not supported on Browser/iOS/tvOS/MacCatalyst")]
    public abstract class DsaFamilySignatureFormatTests
    {
        protected readonly struct KeyDescription
        {
            public readonly AsymmetricAlgorithm Key;
            public readonly string Description;
            public readonly int FieldSizeInBits;

            public KeyDescription(AsymmetricAlgorithm key, string description, int fieldSizeInBits)
            {
                Key = key;
                Description = description;
                FieldSizeInBits = fieldSizeInBits;
            }

            public override string ToString() => Description;
        }

        private readonly KeyDescription[] _testKeys;
        private readonly byte[] _typeNameBytes;
        private readonly int _typeDifferentiator;

        protected DsaFamilySignatureFormatTests()
        {
            _testKeys = GenerateTestKeys();
            string typeName = GetType().Name;
            _typeDifferentiator = typeName.GetHashCode();
            _typeNameBytes = System.Text.Encoding.UTF8.GetBytes(typeName);
        }

        public static IEnumerable<object[]> SignatureFormats { get; } =
            new[]
            {
                new object[] { DSASignatureFormat.Rfc3279DerSequence },
                new object[] { DSASignatureFormat.IeeeP1363FixedFieldConcatenation },
            };

        protected virtual string HashParameterName => "hash";
        protected virtual string SignatureParameterName => "signature";
        protected abstract bool SupportsSha2 { get; }
        protected abstract bool IsArrayBased { get; }
        protected bool IsNotArrayBased => !IsArrayBased;

        protected abstract KeyDescription[] GenerateTestKeys();

        protected abstract byte[] SignHash(
            KeyDescription key,
            byte[] hash,
            DSASignatureFormat signatureFormat);
        protected abstract bool VerifyHash(
            KeyDescription key,
            byte[] hash,
            byte[] signature,
            DSASignatureFormat signatureFormat);
        protected abstract byte[] SignData(
            KeyDescription key,
            byte[] data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat);
        protected abstract bool VerifyData(
            KeyDescription key,
            byte[] data,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat);

        protected KeyDescription GetKey([CallerMemberName]string testMethodName = null)
        {
            int absoluteKeyId = Math.Abs(_typeDifferentiator + testMethodName.GetHashCode());
            int localKeyId = absoluteKeyId % _testKeys.Length;
            return _testKeys[localKeyId];
        }

        protected static int GetDerLengthLength(int payloadLength)
        {
            if (payloadLength < 0)
                throw new ArgumentOutOfRangeException(nameof(payloadLength));

            if (payloadLength <= 0x7F)
                return 0;

            if (payloadLength <= 0xFF)
                return 1;

            if (payloadLength <= 0xFFFF)
                return 2;

            if (payloadLength <= 0xFFFFFF)
                return 3;

            return 4;
        }

        private static void CheckLength(KeyDescription key, byte[] signature, DSASignatureFormat signatureFormat)
        {
            int fieldSizeBytes = (key.FieldSizeInBits + 7) / 8;

            switch (signatureFormat)
            {
                case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
                    
                    Assert.Equal(2 * fieldSizeBytes, signature.Length);
                    break;
                case DSASignatureFormat.Rfc3279DerSequence:
                {
                    // SEQUENCE(INTEGER, INTEGER) has a minimum length of 8 (30 06 02 01 00 02 01 00)
                    // The maximum length is a bit more complicated:
                    int elemSize = fieldSizeBytes + 1;
                    int integerMax = 2 + GetDerLengthLength(elemSize) + elemSize;
                    int integersMax = 2 * integerMax;
                    int sequenceMax = 2 + GetDerLengthLength(integersMax) + integersMax;

                    Assert.InRange(signature.Length, 8, sequenceMax);
                    break;
                }
                default:
                    throw new InvalidOperationException($"No handler for format {signatureFormat}");
            }
        }

        [Theory]
        [MemberData(nameof(SignatureFormats))]
        public void SignHashVerifyHash(DSASignatureFormat signatureFormat)
        {
            KeyDescription key = GetKey();
            byte[] hash = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            byte[] signature = SignHash(key, hash, signatureFormat);
            CheckLength(key, signature, signatureFormat);
            Assert.True(VerifyHash(key, hash, signature, signatureFormat));
        }

        [Theory]
        [MemberData(nameof(SignatureFormats))]
        public void SignDataVerifyData_SHA1(DSASignatureFormat signatureFormat)
        {
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;

            KeyDescription key = GetKey();
            byte[] signature = SignData(key, _typeNameBytes, hashAlgorithm, signatureFormat);
            CheckLength(key, signature, signatureFormat);
            Assert.True(VerifyData(key, _typeNameBytes, signature, hashAlgorithm, signatureFormat));
        }

        [Theory]
        [MemberData(nameof(SignatureFormats))]
        public void SignDataVerifyHash_SHA1(DSASignatureFormat signatureFormat)
        {
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;

            KeyDescription key = GetKey();
            byte[] signature = SignData(key, _typeNameBytes, hashAlgorithm, signatureFormat);
            CheckLength(key, signature, signatureFormat);

            using (IncrementalHash hash = IncrementalHash.CreateHash(hashAlgorithm))
            {
                hash.AppendData(_typeNameBytes);
                Assert.True(VerifyHash(key, hash.GetHashAndReset(), signature, signatureFormat));
            }
        }

        [Theory]
        [MemberData(nameof(SignatureFormats))]
        public void SignDataVerifyData_SHA256(DSASignatureFormat signatureFormat)
        {
            if (!SupportsSha2)
                return;

            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;

            KeyDescription key = GetKey();
            byte[] signature = SignData(key, _typeNameBytes, hashAlgorithm, signatureFormat);
            CheckLength(key, signature, signatureFormat);
            Assert.True(VerifyData(key, _typeNameBytes, signature, hashAlgorithm, signatureFormat));
        }

        [Theory]
        [MemberData(nameof(SignatureFormats))]
        public void SignDataVerifyHash_SHA256(DSASignatureFormat signatureFormat)
        {
            if (!SupportsSha2)
                return;

            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;

            KeyDescription key = GetKey();
            byte[] signature = SignData(key, _typeNameBytes, hashAlgorithm, signatureFormat);
            CheckLength(key, signature, signatureFormat);

            using (IncrementalHash hash = IncrementalHash.CreateHash(hashAlgorithm))
            {
                hash.AppendData(_typeNameBytes);
                Assert.True(VerifyHash(key, hash.GetHashAndReset(), signature, signatureFormat));
            }
        }

        [Fact]
        public void VerifyInvalidRfc3279Signature()
        {
            KeyDescription key = GetKey();
            // This is SEQUENCE(INTEGER(1), INTEGER(0)), except the second integer uses
            // a length value that exceeds the payload length.
            // This ensures that we don't throw exceptions after finding out the leading bytes
            // are valid sequence for the payload length.
            byte[] invalidSignature = { 0x30, 0x06, 0x02, 0x01, 0x01, 0x02, 0x04, 0x00 };

            Assert.False(
                VerifyData(
                    key,
                    _typeNameBytes,
                    invalidSignature,
                    HashAlgorithmName.SHA1,
                    DSASignatureFormat.Rfc3279DerSequence),
                "VerifyData with an illegal DER payload");

            Assert.False(
                VerifyHash(
                    key,
                    _typeNameBytes,
                    invalidSignature,
                    DSASignatureFormat.Rfc3279DerSequence),
                "VerifyHash with an illegal DER payload");
        }

        [Fact]
        public void Rfc3279SignatureValidatesLength()
        {
            KeyDescription key = GetKey();
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;
            const DSASignatureFormat SignatureFormat = DSASignatureFormat.Rfc3279DerSequence;

            byte[] hash = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            byte[] signature = SignHash(key, hash, SignatureFormat);
            byte[] rightPadded = signature.Concat(Enumerable.Repeat((byte)0, 4)).ToArray();

            Assert.True(
                VerifyHash(key, hash, signature, SignatureFormat),
                "VerifyHash with the unmodified signature");

            Assert.False(
                VerifyHash(key, hash, rightPadded, SignatureFormat),
                "VerifyHash with the right-padded signature");

            signature = SignData(key, hash, hashAlgorithm, SignatureFormat);
            rightPadded = signature.Concat(Enumerable.Repeat((byte)0, 4)).ToArray();

            Assert.True(
                VerifyData(key, hash, signature, hashAlgorithm, SignatureFormat),
                "VerifyData with the unmodified signature");

            Assert.False(
                VerifyData(key, hash, rightPadded, hashAlgorithm, SignatureFormat),
                "VerifyData with the right-padded signature");
        }

        [Theory]
        [InlineData(DSASignatureFormat.IeeeP1363FixedFieldConcatenation, DSASignatureFormat.Rfc3279DerSequence)]
        [InlineData(DSASignatureFormat.Rfc3279DerSequence, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)]
        public void SignatureFormatsAreNotCompatible(DSASignatureFormat signFormat, DSASignatureFormat verifyFormat)
        {
            if (!SupportsSha2)
                return;

            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;
            const int RetryCount = 10;

            KeyDescription key = GetKey();
            byte[] hash;

            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                hasher.AppendData(_typeNameBytes);
                hash = hasher.GetHashAndReset();
            }

            for (int i = 0; i < RetryCount; i++)
            {
                byte[] signature = SignData(
                    key,
                    _typeNameBytes,
                    hashAlgorithm,
                    signFormat);

                if (!VerifyData(key, _typeNameBytes, signature, hashAlgorithm, verifyFormat))
                {
                    Assert.False(
                        VerifyHash(key, hash, signature, verifyFormat),
                        $"VerifyHash({verifyFormat}) verifies after VerifyData({verifyFormat}) fails");

                    return;
                }
            }

            Assert.Fail($"{RetryCount} {signFormat} signatures verified as {verifyFormat} signatures");
        }

        [Fact]
        public void BadSignatureFormat()
        {
            KeyDescription key = GetKey();

            const DSASignatureFormat SignatureFormat = (DSASignatureFormat)3;
            byte[] empty = Array.Empty<byte>();

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "signatureFormat",
                () => SignData(key, empty, HashAlgorithmName.SHA1, SignatureFormat));

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "signatureFormat",
                () => VerifyData(key, empty, empty, HashAlgorithmName.SHA1, SignatureFormat));

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "signatureFormat",
                () => SignHash(key, empty, SignatureFormat));

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "signatureFormat",
                () => VerifyHash(key, empty, empty, SignatureFormat));
        }

        [Fact]
        public void EmptyHashAlgorithm()
        {
            KeyDescription key = GetKey();
            byte[] empty = Array.Empty<byte>();

            foreach (DSASignatureFormat format in Enum.GetValues(typeof(DSASignatureFormat)))
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "hashAlgorithm",
                    () => SignData(key, empty, default, format));

                AssertExtensions.Throws<ArgumentNullException>(
                    "hashAlgorithm",
                    () => VerifyData(key, empty, empty, default, format));

                AssertExtensions.Throws<ArgumentException>(
                    "hashAlgorithm",
                    () => SignData(key, empty, new HashAlgorithmName(""), format));

                AssertExtensions.Throws<ArgumentException>(
                    "hashAlgorithm",
                    () => VerifyData(key, empty, empty, new HashAlgorithmName(""), format));
            }
        }

        [Fact]
        public void UnknownHashAlgorithm()
        {
            KeyDescription key = GetKey();
            byte[] empty = Array.Empty<byte>();
            HashAlgorithmName unknown = new HashAlgorithmName(nameof(UnknownHashAlgorithm));

            foreach (DSASignatureFormat format in Enum.GetValues(typeof(DSASignatureFormat)))
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => SignData(key, empty, unknown, format));

                Assert.ThrowsAny<CryptographicException>(
                    () => VerifyData(key, empty, empty, unknown, format));
            }
        }

        [Fact]
        public void NullInputs()
        {
            if (IsNotArrayBased)
                return;

            KeyDescription key = GetKey();

            foreach (DSASignatureFormat format in Enum.GetValues(typeof(DSASignatureFormat)))
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "data",
                    () => SignData(key, null, HashAlgorithmName.SHA1, format));

                AssertExtensions.Throws<ArgumentNullException>(
                    "data",
                    () => VerifyData(key, null, Array.Empty<byte>(), HashAlgorithmName.SHA1, format));

                AssertExtensions.Throws<ArgumentNullException>(
                    "signature",
                    () => VerifyData(key, Array.Empty<byte>(), null, HashAlgorithmName.SHA1, format));

                AssertExtensions.Throws<ArgumentNullException>(
                    HashParameterName,
                    () => SignHash(key, null, format));

                AssertExtensions.Throws<ArgumentNullException>(
                    HashParameterName,
                    () => VerifyHash(key, null, Array.Empty<byte>(), format));

                AssertExtensions.Throws<ArgumentNullException>(
                    SignatureParameterName,
                    () => VerifyHash(key, Array.Empty<byte>(), null, format));
            }
        }
    }
}
