// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
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
                    throw new ArgumentNullException(nameof(rgbHash));

                rgbHash = AdjustHashSizeIfNecessary(rgbHash);
                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    unsafe
                    {
                        byte[] signature = CngCommon.SignHash(keyHandle, rgbHash, AsymmetricPaddingMode.None, null, rgbHash.Length * 2);
                        return signature;
                    }
                }
            }

            public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
            {
                if (rgbHash == null)
                    throw new ArgumentNullException(nameof(rgbHash));

                if (rgbSignature == null)
                    throw new ArgumentNullException(nameof(rgbSignature));

                rgbHash = AdjustHashSizeIfNecessary(rgbHash);

                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    unsafe
                    {
                        bool verified = CngCommon.VerifyHash(keyHandle, rgbHash, rgbSignature, AsymmetricPaddingMode.None, null);
                        return verified;
                    }
                }
            }

            private byte[] AdjustHashSizeIfNecessary(byte[] hash)
            {
                Debug.Assert(hash != null);

                int qLength = ComputeQLength();

                // Note to review (from steve harter) -- code commented out because SHA1 encodes in 20 bytes
                //  but default for v2 struct is 32 (SHA256) bytes so qLength (32) can be > hash.Length (20) 
                //  so v2 would not support SHA1. Also remove this code in .net desktop?
                // Sample code that will fail in .net desktop:
                //  new DSACng(2048).SignData(new ASCIIEncoding().GetBytes("Hello"), new HashAlgorithmName("SHA1"));

                //if (qLength > hash.Length)
                //    throw new PlatformNotSupportedException("todo:SR.Cryptography_DSA_HashTooShort");

                Array.Resize(ref hash, qLength);
                return hash;
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
