// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using static Interop.BCrypt;
using static Interop.NCrypt;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class DSAImplementation
    {
#endif
        public sealed partial class DSACng : DSA
        {
            // As of FIPS 186-4 the maximum Q size is 32 bytes.
            //
            // See also: cbGroupSize at
            // https://docs.microsoft.com/en-us/windows/desktop/api/bcrypt/ns-bcrypt-_bcrypt_dsa_key_blob_v2
            private const int WindowsMaxQSize = 32;

            public override byte[] CreateSignature(byte[] rgbHash)
            {
                if (rgbHash == null)
                {
                    throw new ArgumentNullException(nameof(rgbHash));
                }

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

            public override unsafe bool TryCreateSignature(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
            {
                Span<byte> stackBuf = stackalloc byte[WindowsMaxQSize];
                ReadOnlySpan<byte> source = AdjustHashSizeIfNecessary(hash, stackBuf);

                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    return CngCommon.TrySignHash(keyHandle, source, destination, AsymmetricPaddingMode.None, null, out bytesWritten);
                }
            }

            public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
            {
                if (rgbHash == null)
                {
                    throw new ArgumentNullException(nameof(rgbHash));
                }
                if (rgbSignature == null)
                {
                    throw new ArgumentNullException(nameof(rgbSignature));
                }

                return VerifySignature((ReadOnlySpan<byte>)rgbHash, (ReadOnlySpan<byte>)rgbSignature);
            }

            public override bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
            {
                Span<byte> stackBuf = stackalloc byte[WindowsMaxQSize];
                ReadOnlySpan<byte> source = AdjustHashSizeIfNecessary(hash, stackBuf);

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
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
