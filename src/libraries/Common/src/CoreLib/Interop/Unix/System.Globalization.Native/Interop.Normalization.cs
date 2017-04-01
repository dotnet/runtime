// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class GlobalizationInterop
    {
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IsNormalized")]
        internal static extern int IsNormalized(NormalizationForm normalizationForm, string src, int srcLen);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_NormalizeString")]
        internal static extern int NormalizeString(NormalizationForm normalizationForm, string src, int srcLen, [Out] char[] dstBuffer, int dstBufferCapacity);
    }
}
