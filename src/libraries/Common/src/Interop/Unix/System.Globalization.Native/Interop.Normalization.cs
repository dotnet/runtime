// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IsNormalized")]
        internal static extern int IsNormalized(NormalizationForm normalizationForm, string src, int srcLen);

        internal static int NormalizeString(NormalizationForm normalizationForm, string src, int srcLen, Span<char> dstBuffer)
        {
            unsafe
            {
                fixed (char* pSrc = src)
                fixed (char* pDest = &MemoryMarshal.GetReference(dstBuffer))
                {
                    return NormalizeString(normalizationForm, pSrc, srcLen, pDest, dstBuffer.Length);
                }
            }
        }

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_NormalizeString")]
        private static extern unsafe int NormalizeString(NormalizationForm normalizationForm, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);
    }
}
