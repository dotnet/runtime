// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa
    {
        /// <summary>
        ///     Computes the signature of a hash that was produced by the hash algorithm specified by "hashAlgorithm."
        /// </summary>
        public override byte[] SignHash(byte[] hash)
        {
            ArgumentNullException.ThrowIfNull(hash);

            int estimatedSize = GetMaxSignatureSize(DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            unsafe
            {
                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    byte[] signature = keyHandle.SignHash(hash, AsymmetricPaddingMode.None, null, estimatedSize);
                    return signature;
                }
            }
        }

        public override bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return TrySignHashCore(
                source,
                destination,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation,
                out bytesWritten);
        }

        protected override unsafe bool TrySignHashCore(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                if (!keyHandle.TrySignHash(hash, destination, AsymmetricPaddingMode.None, null, out bytesWritten))
                {
                    bytesWritten = 0;
                    return false;
                }
            }

            if (signatureFormat == DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            {
                return true;
            }

            if (signatureFormat != DSASignatureFormat.Rfc3279DerSequence)
            {
                Debug.Fail($"Missing internal implementation handler for signature format {signatureFormat}");
                throw new CryptographicException(
                    SR.Cryptography_UnknownSignatureFormat,
                    signatureFormat.ToString());
            }

            return AsymmetricAlgorithmHelpers.TryConvertIeee1363ToDer(
                destination.Slice(0, bytesWritten),
                destination,
                out bytesWritten);
        }

        /// <summary>
        ///     Verifies that alleged signature of a hash is, in fact, a valid signature of that hash.
        /// </summary>
        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            ArgumentNullException.ThrowIfNull(hash);
            ArgumentNullException.ThrowIfNull(signature);

            return VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature) =>
            VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        protected override bool VerifyHashCore(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            DSASignatureFormat signatureFormat)
        {
            if (signatureFormat != DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            {
                signature = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);
            }

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                unsafe
                {
                    return keyHandle.VerifyHash(hash, signature, AsymmetricPaddingMode.None, null);
                }
            }
        }
    }
}
