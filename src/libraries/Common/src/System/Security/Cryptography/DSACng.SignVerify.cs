// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
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
            public override byte[] CreateSignature(byte[] rgbHash)
            {
                if (rgbHash == null)
                {
                    throw new ArgumentNullException(nameof(rgbHash));
                }

                ReadOnlySpan<byte> source = rgbHash;
                byte[] arrayToReturnToArrayPool = AdjustHashSizeIfNecessaryWithArrayPool(ref source);
                try
                {
                    using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                    {
                        unsafe
                        {
                            return CngCommon.SignHash(keyHandle, source, AsymmetricPaddingMode.None, null, source.Length * 2);
                        }
                    }
                }
                finally
                {
                    if (arrayToReturnToArrayPool != null)
                    {
                        Array.Clear(arrayToReturnToArrayPool, 0, source.Length);
                        ArrayPool<byte>.Shared.Return(arrayToReturnToArrayPool);
                    }
                }
            }

            public override unsafe bool TryCreateSignature(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
            {
                byte[] arrayToReturnToArrayPool = AdjustHashSizeIfNecessaryWithArrayPool(ref source);
                try
                {
                    using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                    {
                        return CngCommon.TrySignHash(keyHandle, source, destination, AsymmetricPaddingMode.None, null, out bytesWritten);
                    }
                }
                finally
                {
                    if (arrayToReturnToArrayPool != null)
                    {
                        Array.Clear(arrayToReturnToArrayPool, 0, source.Length);
                        ArrayPool<byte>.Shared.Return(arrayToReturnToArrayPool);
                    }
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

            public override bool VerifySignature(ReadOnlySpan<byte> rgbHash, ReadOnlySpan<byte> rgbSignature)
            {
                byte[] arrayToReturnToArrayPool = AdjustHashSizeIfNecessaryWithArrayPool(ref rgbHash);
                try
                {
                    using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                    {
                        unsafe
                        {
                            return CngCommon.VerifyHash(keyHandle, rgbHash, rgbSignature, AsymmetricPaddingMode.None, null);
                        }
                    }
                }
                finally
                {
                    if (arrayToReturnToArrayPool != null)
                    {
                        Array.Clear(arrayToReturnToArrayPool, 0, rgbHash.Length);
                        ArrayPool<byte>.Shared.Return(arrayToReturnToArrayPool);
                    }
                }
            }

            private byte[] AdjustHashSizeIfNecessaryWithArrayPool(ref ReadOnlySpan<byte> hash)
            {
                // Windows CNG requires that the hash output and q match sizes, but we can better
                // interoperate with other FIPS 186-3 implementations if we perform truncation
                // here, before sending it to CNG. Since this is a scenario presented in the
                // CAVP reference test suite, we can confirm our implementation.
                //
                // If, on the other hand, Q is too big, we need to left-pad the hash with zeroes
                // (since it gets treated as a big-endian number). Since this is also a scenario
                // presented in the CAVP reference test suite, we can confirm our implementation.

                int qLength = ComputeQLength();

                if (qLength == hash.Length)
                {
                    return null;
                }
                else if (qLength < hash.Length)
                {
                    hash = hash.Slice(0, qLength);
                    return null;
                }
                else
                {
                    byte[] arrayPoolPaddedHash = ArrayPool<byte>.Shared.Rent(qLength);
                    hash.CopyTo(new Span<byte>(arrayPoolPaddedHash, qLength - hash.Length, hash.Length));
                    hash = new ReadOnlySpan<byte>(arrayPoolPaddedHash, 0, qLength);
                    return arrayPoolPaddedHash;
                }
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
