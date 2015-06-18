// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class GlobalizationInterop
    {
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern void ToUpperSimple(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern void ToLowerSimple(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern void ToUpperSimpleTurkishAzeri(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern void ToLowerSimpleTurkishAzeri(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);
    }
}
