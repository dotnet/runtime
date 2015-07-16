// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class GlobalizationInterop
    {
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal static extern int IsNormalized(NormalizationForm normalizationForm, string src, int srcLen);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal static extern int NormalizeString(NormalizationForm normalizationForm, string src, int srcLen, [Out] char[] dstBuffer, int dstBufferCapacity);
    }
}