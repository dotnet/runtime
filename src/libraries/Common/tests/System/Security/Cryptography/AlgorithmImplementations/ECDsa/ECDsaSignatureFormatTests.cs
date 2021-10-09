// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Algorithms.Tests;
using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class ECDsaSignatureFormatTests : DsaFamilySignatureFormatTests
    {
        protected override bool SupportsSha2 => true;

        private static KeyDescription CreateKey(ECCurve curve)
        {
            ECDsa dsa = ECDsaFactory.Create(curve);

            return new KeyDescription(
                dsa,
                $"{dsa.KeySize}-bit random key",
                dsa.KeySize);
        }

        private static KeyDescription OpenKey(in ECParameters ecParameters)
        {
            ECDsa dsa = ECDsaFactory.Create();
            dsa.ImportParameters(ecParameters);

            return new KeyDescription(
                dsa,
                $"{dsa.KeySize}-bit static key",
                dsa.KeySize);
        }

        protected static IEnumerable<KeyDescription> LocalGenerateTestKeys()
        {
            if (ECDsaFactory.IsCurveValid(EccTestData.BrainpoolP160r1Key1.Curve.Oid))
            {
                yield return OpenKey(EccTestData.BrainpoolP160r1Key1);
            }

            if (ECDsaFactory.IsCurveValid(ECCurve.NamedCurves.nistP384.Oid))
            {
                yield return CreateKey(ECCurve.NamedCurves.nistP384);
            }

            yield return OpenKey(EccTestData.GetNistP521DiminishedCoordsParameters());

            if (ECDsaFactory.ExplicitCurvesSupported)
            {
                yield return OpenKey(EccTestData.GetNistP256ReferenceKeyExplicit());
            }
        }
    }

    public sealed class ECDsaArraySignatureFormatTests : ECDsaSignatureFormatTests
    {
        private static readonly KeyDescription[] s_keys = LocalGenerateTestKeys().ToArray();

        protected override KeyDescription[] GenerateTestKeys() => s_keys;
        protected override bool IsArrayBased => true;
        
        protected override byte[] SignHash(
            KeyDescription key,
            byte[] hash,
            DSASignatureFormat signatureFormat)
        {
            return ((ECDsa)key.Key).SignHash(hash, signatureFormat);
        }

        protected override bool VerifyHash(
            KeyDescription key,
            byte[] hash,
            byte[] signature,
            DSASignatureFormat signatureFormat)
        {
            return ((ECDsa)key.Key).VerifyHash(hash, signature, signatureFormat);
        }

        protected override byte[] SignData(
            KeyDescription key,
            byte[] data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            return ((ECDsa)key.Key).SignData(data, hashAlgorithm, signatureFormat);
        }

        protected override bool VerifyData(
            KeyDescription key,
            byte[] data,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            return ((ECDsa)key.Key).VerifyData(data, signature, hashAlgorithm, signatureFormat);
        }
    }

    public sealed class ECDsaArrayOffsetSignatureFormatTests : ECDsaSignatureFormatTests
    {
        private static readonly KeyDescription[] s_keys = LocalGenerateTestKeys().ToArray();

        protected override KeyDescription[] GenerateTestKeys() => s_keys;
        protected override bool IsArrayBased => true;

        protected override byte[] SignHash(
            KeyDescription key,
            byte[] hash,
            DSASignatureFormat signatureFormat)
        {
            return ((ECDsa)key.Key).SignHash(hash, signatureFormat);
        }

        protected override bool VerifyHash(
            KeyDescription key,
            byte[] hash,
            byte[] signature,
            DSASignatureFormat signatureFormat)
        {
            return ((ECDsa)key.Key).VerifyHash(hash, signature, signatureFormat);
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

            return ((ECDsa)key.Key).SignData(data, offset, count, hashAlgorithm, signatureFormat);
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

            return ((ECDsa)key.Key).VerifyData(data, offset, count, signature, hashAlgorithm, signatureFormat);
        }

        [Fact]
        public void OffsetAndCountOutOfRange()
        {
            KeyDescription keyDescription = GetKey();
            ECDsa key = (ECDsa)keyDescription.Key;

            HashAlgorithmName hash = HashAlgorithmName.SHA256;
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

    public sealed class ECDsaSpanSignatureFormatTests : ECDsaSignatureFormatTests
    {
        private static readonly KeyDescription[] s_keys = LocalGenerateTestKeys().ToArray();

        protected override KeyDescription[] GenerateTestKeys() => s_keys;
        protected override bool IsArrayBased => false;

        protected override byte[] SignHash(
            KeyDescription key,
            byte[] hash,
            DSASignatureFormat signatureFormat)
        {
            ECDsa dsa = (ECDsa)key.Key;
            byte[] predictedMax = new byte[dsa.GetMaxSignatureSize(signatureFormat)];

            Assert.True(
                dsa.TrySignHash(hash, predictedMax, signatureFormat, out int written),
                "TrySignHash with a GetMaxSignatureSize buffer");

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
            return ((ECDsa)key.Key).VerifyHash(readOnlyHash, readOnlySignature, signatureFormat);
        }

        protected override byte[] SignData(
            KeyDescription key,
            byte[] data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            ECDsa dsa = (ECDsa)key.Key;
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

            return ((ECDsa)key.Key).VerifyData(readOnlyData, readOnlySignature, hashAlgorithm, signatureFormat);
        }

        private static int GetExpectedSize(int fieldSizeInBits)
        {
            // In ECDSA field sizes aren't always byte-aligned (e.g. secp521r1).
            int fullBytes = Math.DivRem(fieldSizeInBits, 8, out int spareBits) + 1;
            int wiggle = 0;

            if (spareBits == 1)
            {
                // If there's only one spare bit (e.g. 521r1), we have a 50% chance to
                // drop a byte (then a 50% chance to gain it back), predict a byte loss.
                wiggle = -1;
            }
            else if (spareBits == 0)
            {
                // If we're byte aligned, then if the high bit is set (~50%) we gain a padding
                // byte, so predict a byte gain.
                wiggle = 1;

                // Also, as byte aligned, reduce the +1 from the original calculation
                fullBytes--;
            }

            int field1 = 2 + GetDerLengthLength(fullBytes) + fullBytes;
            int field2 = 2 + GetDerLengthLength(fullBytes + wiggle) + fullBytes + wiggle;
            int payload = field1 + field2;
            return 2 + GetDerLengthLength(payload) + payload;
        }

        [Fact]
        public void Rfc23279TrySignHashUnderMax()
        {
            KeyDescription keyDescription = GetKey();
            ECDsa key = (ECDsa)keyDescription.Key;

            const DSASignatureFormat SignatureFormat = DSASignatureFormat.Rfc3279DerSequence;
            // Make secp521r1 (7/16 chance of being smaller) and mod-8 keys (3/4 chance of being smaller)
            // have the same 1-in-a-billion chance of failure.
            int retryCount = keyDescription.FieldSizeInBits % 8 == 1 ? 36 : 15;
            byte[] hash = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            int expectedSize = GetExpectedSize(keyDescription.FieldSizeInBits);
            int maxSize = key.GetMaxSignatureSize(DSASignatureFormat.Rfc3279DerSequence);
            Assert.True(expectedSize < maxSize, "expectedSize < maxSize");
            byte[] signature = new byte[expectedSize];

            for (int i = 0; i < retryCount; i++)
            {
                if (key.TrySignHash(hash, signature, SignatureFormat, out int written))
                {
                    return;
                }

                Assert.Equal(0, written);
            }

            Assert.True(false, $"TrySignHash eventually succeeds with a {expectedSize}/{maxSize}-byte destination");
        }

        [Fact]
        public void Rfc23279TrySignDataUnderMax()
        {
            KeyDescription keyDescription = GetKey();
            ECDsa key = (ECDsa)keyDescription.Key;

            const DSASignatureFormat SignatureFormat = DSASignatureFormat.Rfc3279DerSequence;
            // Make secp521r1 (7/16 chance of being smaller) and mod-8 keys (3/4 chance of being smaller)
            // have the same 1-in-a-billion chance of failure.
            int retryCount = keyDescription.FieldSizeInBits % 8 == 1 ? 36 : 15;
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;

            int expectedSize = GetExpectedSize(keyDescription.FieldSizeInBits);
            int maxSize = key.GetMaxSignatureSize(DSASignatureFormat.Rfc3279DerSequence);
            Assert.True(expectedSize < maxSize, "expectedSize < maxSize");
            byte[] signature = new byte[expectedSize];

            for (int i = 0; i < retryCount; i++)
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
