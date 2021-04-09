// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Algorithms.Tests;
using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class DSASignatureFormatTests : DsaFamilySignatureFormatTests
    {
        protected override bool SupportsSha2 => DSAFactory.SupportsFips186_3;
        protected override string HashParameterName => "rgbHash";
        protected override string SignatureParameterName => "rgbSignature";

        private static KeyDescription CreateKey(int keySize)
        {
            DSA dsa = DSAFactory.Create(keySize);
            int fieldSize;

            if (keySize <= 1024)
            {
                fieldSize = 160;
            }
            else if (keySize == 3072)
            {
                fieldSize = 256;
            }
            else
            {
                fieldSize = dsa.ExportParameters(false).Q.Length * 8;
            }

            return new KeyDescription(
                dsa,
                $"{keySize}-bit random key",
                fieldSize);
        }

        private static KeyDescription OpenKey(in DSAParameters dsaParameters)
        {
            return new KeyDescription(
                DSAFactory.Create(dsaParameters),
                $"{dsaParameters.Y.Length * 8}-bit static key",
                dsaParameters.Q.Length * 8);
        }

        protected static IEnumerable<KeyDescription> LocalGenerateTestKeys()
        {
            if (DSAFactory.SupportsKeyGeneration)
            {
                yield return CreateKey(1024);

                if (DSAFactory.SupportsFips186_3)
                {
                    yield return CreateKey(2048);
                }
            }

            if (DSAFactory.SupportsFips186_3)
            {
                yield return OpenKey(DSATestData.GetDSA2048Params());
            }

            yield return OpenKey(DSATestData.GetDSA1024Params());
        }
    }

    public sealed class DsaArraySignatureFormatTests : DSASignatureFormatTests
    {
        private static readonly KeyDescription[] s_keys = LocalGenerateTestKeys().ToArray();

        protected override KeyDescription[] GenerateTestKeys() => s_keys;
        protected override bool IsArrayBased => true;

        protected override byte[] SignHash(
            KeyDescription key,
            byte[] hash,
            DSASignatureFormat signatureFormat)
        {
            return ((DSA)key.Key).CreateSignature(hash, signatureFormat);
        }

        protected override bool VerifyHash(
            KeyDescription key,
            byte[] hash,
            byte[] signature,
            DSASignatureFormat signatureFormat)
        {
            return ((DSA)key.Key).VerifySignature(hash, signature, signatureFormat);
        }

        protected override byte[] SignData(
            KeyDescription key,
            byte[] data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            return ((DSA)key.Key).SignData(data, hashAlgorithm, signatureFormat);
        }

        protected override bool VerifyData(
            KeyDescription key,
            byte[] data,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            return ((DSA)key.Key).VerifyData(data, signature, hashAlgorithm, signatureFormat);
        }
    }

    public sealed class DsaArrayOffsetSignatureFormatTests : DSASignatureFormatTests
    {
        private static readonly KeyDescription[] s_keys = LocalGenerateTestKeys().ToArray();

        protected override KeyDescription[] GenerateTestKeys() => s_keys;
        protected override bool IsArrayBased => true;

        protected override byte[] SignHash(
            KeyDescription key,
            byte[] hash,
            DSASignatureFormat signatureFormat)
        {
            return ((DSA)key.Key).CreateSignature(hash, signatureFormat);
        }

        protected override bool VerifyHash(
            KeyDescription key,
            byte[] hash,
            byte[] signature,
            DSASignatureFormat signatureFormat)
        {
            return ((DSA)key.Key).VerifySignature(hash, signature, signatureFormat);
        }

        protected override byte[] SignData(
            KeyDescription key,
            byte[] data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            int offset = 0;
            int count = 0;

            if (data != null)
            {
                offset = 2;
                count = data.Length;
                byte[] bigger = new byte[count + 7];
                Buffer.BlockCopy(data, 0, bigger, offset, count);
                data = bigger;
            }

            return ((DSA)key.Key).SignData(data, offset, count, hashAlgorithm, signatureFormat);
        }

        protected override bool VerifyData(
            KeyDescription key,
            byte[] data,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            int offset = 0;
            int count = 0;

            if (data != null)
            {
                offset = 2;
                count = data.Length;
                byte[] bigger = new byte[count + 7];
                Buffer.BlockCopy(data, 0, bigger, offset, count);
                data = bigger;
            }

            return ((DSA)key.Key).VerifyData(data, offset, count, signature, hashAlgorithm, signatureFormat);
        }

        [Fact]
        public void OffsetAndCountOutOfRange()
        {
            KeyDescription keyDescription = GetKey();
            DSA key = (DSA)keyDescription.Key;

            HashAlgorithmName hash = HashAlgorithmName.SHA1;
            byte[] buffer = new byte[10];

            foreach (DSASignatureFormat format in Enum.GetValues(typeof(DSASignatureFormat)))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "offset",
                    () => key.SignData(buffer, -1, buffer.Length, hash, format));

                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "offset",
                    () => key.SignData(buffer, buffer.Length + 1, 0, hash, format));

                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "offset",
                    () => key.VerifyData(buffer, -1, buffer.Length, buffer, hash, format));

                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "offset",
                    () => key.VerifyData(buffer, buffer.Length + 1, 0, buffer, hash, format));

                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "count",
                    () => key.SignData(buffer, 1, buffer.Length, hash, format));

                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "count",
                    () => key.SignData(buffer, 0, buffer.Length + 1, hash, format));

                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "count",
                    () => key.SignData(buffer, buffer.Length, 1, hash, format));

                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "count",
                    () => key.VerifyData(buffer, 1, buffer.Length, buffer, hash, format));

                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "count",
                    () => key.VerifyData(buffer, 0, buffer.Length + 1, buffer, hash, format));

                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "count",
                    () => key.VerifyData(buffer, buffer.Length, 1, buffer, hash, format));
            }
        }
    }

    public sealed class DsaSpanSignatureFormatTests : DSASignatureFormatTests
    {
        private static readonly KeyDescription[] s_keys = LocalGenerateTestKeys().ToArray();

        protected override KeyDescription[] GenerateTestKeys() => s_keys;
        protected override bool IsArrayBased => false;

        protected override byte[] SignHash(
            KeyDescription key,
            byte[] hash,
            DSASignatureFormat signatureFormat)
        {
            DSA dsa = (DSA)key.Key;
            byte[] predictedMax = new byte[dsa.GetMaxSignatureSize(signatureFormat)];

            Assert.True(
                dsa.TryCreateSignature(hash, predictedMax, signatureFormat, out int written),
                "TryCreateSignature with a GetMaxSignatureSize buffer");

            if (signatureFormat == DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            {
                // GetMaxSignatureSize should be exactly accurate for P1363.
                Assert.Equal(predictedMax.Length, written);
            }

            if (written == predictedMax.Length)
            {
                return predictedMax;
            }

            return predictedMax.AsSpan(0, written).ToArray();
        }

        protected override bool VerifyHash(
            KeyDescription key,
            byte[] hash,
            byte[] signature,
            DSASignatureFormat signatureFormat)
        {
            ReadOnlySpan<byte> readOnlyHash = hash;
            ReadOnlySpan<byte> readOnlySignature = signature;
            return ((DSA)key.Key).VerifySignature(readOnlyHash, readOnlySignature, signatureFormat);
        }

        protected override byte[] SignData(
            KeyDescription key,
            byte[] data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            DSA dsa = (DSA)key.Key;
            byte[] predictedMax = new byte[dsa.GetMaxSignatureSize(signatureFormat)];

            Assert.True(
                dsa.TrySignData(data, predictedMax, hashAlgorithm, signatureFormat, out int written),
                "TrySignData with a GetMaxSignatureSize buffer");

            if (signatureFormat == DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            {
                // GetMaxSignatureSize should be exactly accurate for P1363.
                Assert.Equal(predictedMax.Length, written);
            }

            if (written == predictedMax.Length)
            {
                return predictedMax;
            }

            return predictedMax.AsSpan(0, written).ToArray();
        }

        protected override bool VerifyData(
            KeyDescription key,
            byte[] data,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            ReadOnlySpan<byte> readOnlyData = data;
            ReadOnlySpan<byte> readOnlySignature = signature;

            return ((DSA)key.Key).VerifyData(readOnlyData, readOnlySignature, hashAlgorithm, signatureFormat);
        }

        private static int GetExpectedSize(int fieldSizeInBits)
        {
            // Assuming FieldSizeInBits is byte-aligned, the odds of a padding byte being required is 50%.
            // Two 50% things means that we expect one of them and not the other (on average).
            // They only shrink a byte in 1/256, but gain it back 50% of the time, so they shrink in 1/512.
            // That's low enough it isn't expected.
            // So we expect each of (r) and (s) to be (fieldSizeInBits / 8) + 0.5, then add structure.
            // Because DSA is limited to small field sizes (256 bit), the structure overhead is always 6 bytes.
            // So, fieldSizeInBits / 4
            return fieldSizeInBits / 4 + 7;
        }

        [Fact]
        public void Rfc23279TrySignHashUnderMax()
        {
            KeyDescription keyDescription = GetKey();
            const int RetryCount = 10;
            DSA key = (DSA)keyDescription.Key;

            const DSASignatureFormat SignatureFormat = DSASignatureFormat.Rfc3279DerSequence;
            byte[] hash = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            int expectedSize = GetExpectedSize(keyDescription.FieldSizeInBits);
            int maxSize = key.GetMaxSignatureSize(DSASignatureFormat.Rfc3279DerSequence);
            Assert.True(expectedSize < maxSize, "expectedSize < maxSize");
            byte[] signature = new byte[expectedSize];

            for (int i = 0; i < RetryCount; i++)
            {
                if (key.TryCreateSignature(hash, signature, SignatureFormat, out int written))
                {
                    return;
                }

                Assert.Equal(0, written);
            }

            Assert.True(false, $"TryCreateSignature eventually succeeds with a {expectedSize}/{maxSize}-byte destination");
        }

        [Fact]
        public void Rfc23279TrySignDataUnderMax()
        {
            KeyDescription keyDescription = GetKey();
            DSA key = (DSA)keyDescription.Key;

            const DSASignatureFormat SignatureFormat = DSASignatureFormat.Rfc3279DerSequence;
            const int RetryCount = 10;
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;

            int expectedSize = GetExpectedSize(keyDescription.FieldSizeInBits);
            int maxSize = key.GetMaxSignatureSize(DSASignatureFormat.Rfc3279DerSequence);
            Assert.True(expectedSize < maxSize, "expectedSize < maxSize");
            byte[] signature = new byte[expectedSize];

            for (int i = 0; i < RetryCount; i++)
            {
                if (key.TrySignData(Array.Empty<byte>(), signature, hashAlgorithm, SignatureFormat, out int written))
                {
                    return;
                }

                Assert.Equal(0, written);
            }

            Assert.True(false, $"TrySignData eventually succeeds with a {expectedSize}/{maxSize}-byte destination");
        }
    }
}
