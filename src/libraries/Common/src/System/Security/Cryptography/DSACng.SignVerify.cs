// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using static Interop.BCrypt;
using static Interop.NCrypt;

namespace System.Security.Cryptography
{
    public sealed partial class DSACng : DSA
    {
        // As of FIPS 186-4 the maximum Q size is 32 bytes.
        //
        // See also: cbGroupSize at
        // https://docs.microsoft.com/en-us/windows/desktop/api/bcrypt/ns-bcrypt-_bcrypt_dsa_key_blob_v2
        private const int WindowsMaxQSize = 32;

        public override byte[] CreateSignature(byte[] rgbHash)
        {
            ArgumentNullException.ThrowIfNull(rgbHash);

            Span<byte> stackBuf = stackalloc byte[WindowsMaxQSize];
            ReadOnlySpan<byte> source = AdjustHashSizeIfNecessary(rgbHash, stackBuf);

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                unsafe
                {
                    return CngCommon.SignHash(keyHandle, source, AsymmetricPaddingMode.None, null, source.Length * 2);
                }
            }
        }

        protected override unsafe bool TryCreateSignatureCore(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            Span<byte> stackBuf = stackalloc byte[WindowsMaxQSize];
            ReadOnlySpan<byte> source = AdjustHashSizeIfNecessary(hash, stackBuf);

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                if (!CngCommon.TrySignHash(keyHandle, source, destination, AsymmetricPaddingMode.None, null, out bytesWritten))
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

        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
        {
            ArgumentNullException.ThrowIfNull(rgbHash);
            ArgumentNullException.ThrowIfNull(rgbSignature);

            return VerifySignatureCore(rgbHash, rgbSignature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        protected override bool VerifySignatureCore(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            DSASignatureFormat signatureFormat)
        {
            Span<byte> stackBuf = stackalloc byte[WindowsMaxQSize];
            ReadOnlySpan<byte> source = AdjustHashSizeIfNecessary(hash, stackBuf);

            if (signatureFormat == DSASignatureFormat.Rfc3279DerSequence)
            {
                // source.Length is the field size, in bytes, so just convert to bits.
                int fieldSizeBits = source.Length * 8;
                signature = this.ConvertSignatureToIeeeP1363(signatureFormat, signature, fieldSizeBits);
            }
            else if (signatureFormat != DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            {
                Debug.Fail($"Missing internal implementation handler for signature format {signatureFormat}");
                throw new CryptographicException(
                    SR.Cryptography_UnknownSignatureFormat,
                    signatureFormat.ToString());
            }

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                unsafe
                {
                    return CngCommon.VerifyHash(keyHandle, source, signature, AsymmetricPaddingMode.None, null);
                }
            }
        }

        private ReadOnlySpan<byte> AdjustHashSizeIfNecessary(ReadOnlySpan<byte> hash, Span<byte> stackBuf)
        {
            Debug.Assert(stackBuf.Length == WindowsMaxQSize);

            // Windows CNG requires that the hash output and q match sizes, but we can better
            // interoperate with other FIPS 186-3 implementations if we perform truncation
            // here, before sending it to CNG. Since this is a scenario presented in the
            // CAVP reference test suite, we can confirm our implementation.
            //
            // If, on the other hand, Q is too big, we need to left-pad the hash with zeroes
            // (since it gets treated as a big-endian number). Since this is also a scenario
            // presented in the CAVP reference test suite, we can confirm our implementation.

            int qLength = ComputeQLength();
            Debug.Assert(qLength <= WindowsMaxQSize);

            if (qLength == hash.Length)
            {
                return hash;
            }

            if (qLength < hash.Length)
            {
                return hash.Slice(0, qLength);
            }

            int zeroByteCount = qLength - hash.Length;
            stackBuf.Slice(0, zeroByteCount).Clear();
            hash.CopyTo(stackBuf.Slice(zeroByteCount));
            return stackBuf.Slice(0, qLength);
        }

        private int ComputeQLength()
        {
            byte[] blob;
            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                blob = this.ExportKeyBlob(false);
            }

            unsafe
            {
                if (blob.Length < sizeof(BCRYPT_DSA_KEY_BLOB_V2))
                {
                    return Sha1HashOutputSize;
                }

                fixed (byte* pBlobBytes = blob)
                {
                    BCRYPT_DSA_KEY_BLOB_V2* pBlob = (BCRYPT_DSA_KEY_BLOB_V2*)pBlobBytes;
                    if (pBlob->Magic != KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC_V2 && pBlob->Magic != KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC_V2)
                    {
                        // This is a V1 BCRYPT_DSA_KEY_BLOB, which hardcodes the Q length to 20 bytes.
                        return Sha1HashOutputSize;
                    }

                    return pBlob->cbGroupSize;
                }
            }
        }
    }
}
