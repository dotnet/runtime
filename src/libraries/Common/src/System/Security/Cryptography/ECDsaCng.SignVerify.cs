// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Microsoft.Win32.SafeHandles;

using Internal.Cryptography;

using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDsaImplementation
    {
#endif
    public sealed partial class ECDsaCng : ECDsa
    {
        /// <summary>
        ///     Computes the signature of a hash that was produced by the hash algorithm specified by "hashAlgorithm."
        /// </summary>
        public override byte[] SignHash(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            int estimatedSize = KeySize switch
            {
                256 => 64,
                384 => 96,
                521 => 132,
                // If we got here, the range of legal key sizes for ECDsaCng was expanded and someone didn't update this switch.
                // Since it isn't a fatal error to miscalculate the estimatedSize, don't throw an exception. Just truck along.
                _ => KeySize / 4,
            };

            unsafe
            {
                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    byte[] signature = keyHandle.SignHash(hash, AsymmetricPaddingMode.None, null, estimatedSize);
                    return signature;
                }
            }
        }

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
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
#else
        public override unsafe bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            ReadOnlySpan<byte> hash = source;
#endif
            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                if (!keyHandle.TrySignHash(hash, destination, AsymmetricPaddingMode.None, null, out bytesWritten))
                {
                    bytesWritten = 0;
                    return false;
                }
            }

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
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
#else
            return true;
#endif
        }

        /// <summary>
        ///     Verifies that alleged signature of a hash is, in fact, a valid signature of that hash.
        /// </summary>
        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
            return VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
#else
            return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature);
#endif
        }

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
        public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature) =>
            VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        protected override bool VerifyHashCore(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            DSASignatureFormat signatureFormat)
#else
        public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
#endif
        {
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
            if (signatureFormat != DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            {
                signature = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);
            }
#endif
            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                unsafe
                {
                    return keyHandle.VerifyHash(hash, signature, AsymmetricPaddingMode.None, null);
                }
            }
        }
    }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
