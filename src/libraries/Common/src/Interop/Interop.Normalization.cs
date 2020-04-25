// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Globalization
    {
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IsNormalized")]
#endif
        internal static extern unsafe int IsNormalized(NormalizationForm normalizationForm, char* src, int srcLen);

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_NormalizeString")]
#endif
        internal static extern unsafe int NormalizeString(NormalizationForm normalizationForm, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);
    }
}
