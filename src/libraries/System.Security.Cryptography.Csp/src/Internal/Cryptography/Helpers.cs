// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
        public static KeySizes[] CloneKeySizesArray(this KeySizes[] src)
        {
            return (KeySizes[])(src.Clone());
        }

        public static bool UsesIv(this CipherMode cipherMode)
        {
            return cipherMode != CipherMode.ECB;
        }

        public static byte[] GetCipherIv(this CipherMode cipherMode, byte[] iv)
        {
            if (cipherMode.UsesIv())
            {
                if (iv == null)
                {
                    throw new CryptographicException(SR.Cryptography_MissingIV);
                }

                return iv;
            }

            return null;
        }

        public static byte[] TrimLargeIV(byte[] currentIV, int blockSizeInBits)
        {
            int blockSizeBytes = checked((blockSizeInBits + 7) / 8);

            if (currentIV?.Length > blockSizeBytes)
            {
                byte[] tmp = new byte[blockSizeBytes];
                Buffer.BlockCopy(currentIV, 0, tmp, 0, tmp.Length);
                return tmp;
            }

            return currentIV;
        }

        public static bool IsLegalSize(this int size, KeySizes[] legalSizes)
        {
            for (int i = 0; i < legalSizes.Length; i++)
            {
                KeySizes currentSizes = legalSizes[i];

                // If a cipher has only one valid key size, MinSize == MaxSize and SkipSize will be 0
                if (currentSizes.SkipSize == 0)
                {
                    if (currentSizes.MinSize == size)
                        return true;
                }
                else if (size >= currentSizes.MinSize && size <= currentSizes.MaxSize)
                {
                    // If the number is in range, check to see if it's a legal increment above MinSize
                    int delta = size - currentSizes.MinSize;

                    // While it would be unusual to see KeySizes { 10, 20, 5 } and { 11, 14, 1 }, it could happen.
                    // So don't return false just because this one doesn't match.
                    if (delta % currentSizes.SkipSize == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
