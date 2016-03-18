// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Security.Cryptography
{
    internal static class Helpers
    {
        public static byte[] CloneByteArray(this byte[] src)
        {
            if (src == null)
            {
                return null;
            }

            return (byte[])(src.Clone());
        }

        public static bool IsLegalSize(this int size, KeySizes[] legalSizes)
        {
            bool dontCare;
            return size.IsLegalSize(legalSizes, out dontCare);
        }

        public static bool IsLegalSize(this int size, KeySizes[] legalSizes, out bool validatedByZeroSkipSizeKeySizes)
        {
            validatedByZeroSkipSizeKeySizes = false;
            for (int i = 0; i < legalSizes.Length; i++)
            {
                KeySizes currentSizes = legalSizes[i];
                
                // If a cipher has only one valid key size, MinSize == MaxSize and SkipSize will be 0
                if (currentSizes.SkipSize == 0)
                {
                    if (currentSizes.MinSize == size)
                    {
                        // Signal that we were validated by a 0-skipsize KeySizes entry. Needed to preserve a very obscure
                        // piece of back-compat behavior.
                        validatedByZeroSkipSizeKeySizes = true;
                        return true;
                    }
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

