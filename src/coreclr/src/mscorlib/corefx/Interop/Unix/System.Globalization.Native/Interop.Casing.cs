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
        internal unsafe static extern void ChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bIsUpper, bool bTurkishCasing);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern void ChangeCase(string src, int srcLen, StringBuilder dstBuffer, int dstBufferCapacity, bool bIsUpper, bool bTurkishCasing);
    }
}
