// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Security.Cryptography
{
    internal static class Helpers
    {
        public static byte[] CloneByteArray(this byte[] src)
        {
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
                // If a cipher has only one valid key size, MinSize == MaxSize and SkipSize will be 0
                if (legalSizes[i].SkipSize == 0)
                {
                    if (legalSizes[i].MinSize == size)
                    {
                        // Signal that we were validated by a 0-skipsize KeySizes entry. Needed to preserve a very obscure
                        // piece of back-compat behavior.
                        validatedByZeroSkipSizeKeySizes = true;
                        return true;
                    }
                }
                else
                {
                    for (int j = legalSizes[i].MinSize; j <= legalSizes[i].MaxSize; j += legalSizes[i].SkipSize)
                    {
                        if (j == size)
                            return true;
                    }
                }
            }
            return false;
        }
    }
}

