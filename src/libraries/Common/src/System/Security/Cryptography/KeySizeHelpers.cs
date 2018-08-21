// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Security.Cryptography
{
    internal static class KeySizeHelpers
    {
        public static KeySizes[] CloneKeySizesArray(this KeySizes[] src)
        {
            return (KeySizes[])(src.Clone());
        }

        public static bool IsLegalSize(this int size, KeySizes legalSizes)
        {
            return size.IsLegalSize(legalSizes, out _);
        }

        public static bool IsLegalSize(this int size, KeySizes[] legalSizes)
        {
            return size.IsLegalSize(legalSizes, out _);
        }

        public static bool IsLegalSize(this int size, KeySizes legalSizes, out bool validatedByZeroSkipSizeKeySizes)
        {
            validatedByZeroSkipSizeKeySizes = false;

            // If a cipher has only one valid key size, MinSize == MaxSize and SkipSize will be 0
            if (legalSizes.SkipSize == 0)
            {
                if (legalSizes.MinSize == size)
                {
                    // Signal that we were validated by a 0-skipsize KeySizes entry. Needed to preserve a very obscure
                    // piece of back-compat behavior.
                    validatedByZeroSkipSizeKeySizes = true;
                    return true;
                }
            }
            else if (size >= legalSizes.MinSize && size <= legalSizes.MaxSize)
            {
                // If the number is in range, check to see if it's a legal increment above MinSize
                int delta = size - legalSizes.MinSize;

                // While it would be unusual to see KeySizes { 10, 20, 5 } and { 11, 14, 1 }, it could happen.
                // So don't return false just because this one doesn't match.
                if (delta % legalSizes.SkipSize == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsLegalSize(this int size, KeySizes[] legalSizes, out bool validatedByZeroSkipSizeKeySizes)
        {
            for (int i = 0; i < legalSizes.Length; i++)
            {
                if (size.IsLegalSize(legalSizes[i], out validatedByZeroSkipSizeKeySizes))
                {
                    return true;
                }
            }

            validatedByZeroSkipSizeKeySizes = false;
            return false;
        }
    }
}

