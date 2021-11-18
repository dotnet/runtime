// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_IsNormalized", CharSet = CharSet.Unicode)]
        internal static unsafe partial int IsNormalized(NormalizationForm normalizationForm, char* src, int srcLen);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_NormalizeString", CharSet = CharSet.Unicode)]
        internal static unsafe partial int NormalizeString(NormalizationForm normalizationForm, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);
    }
}
