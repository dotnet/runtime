// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Normaliz
    {
        [DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool IsNormalizedString(NormalizationForm normForm, string source, int length);

        internal static int NormalizeString(NormalizationForm normForm, string source, int sourceLength, Span<char> destination)
        {
            unsafe
            {
                fixed (char* pSrc = source)
                fixed (char* pDest = &MemoryMarshal.GetReference(destination))
                {
                    return NormalizeString(normForm, pSrc, sourceLength, pDest, destination.Length);
                }
            }
        }

        [DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe int NormalizeString(
                                        NormalizationForm normForm,
                                        char* source,
                                        int sourceLength,
                                        char* destination,
                                        int destinationLength);
    }
}
