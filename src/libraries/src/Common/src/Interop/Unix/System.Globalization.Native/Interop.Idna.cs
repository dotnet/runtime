// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class GlobalizationNative
    {
        internal const int AllowUnassigned = 0x1;
        internal const int UseStd3AsciiRules = 0x2;

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode)]
        internal static extern int ToAscii(uint flags, string src, int srcLen, char[] dstBuffer, int dstBufferCapacity);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode)]
        internal static extern int ToUnicode(uint flags, string src, int srcLen, char[] dstBuffer, int dstBufferCapacity);
    }
}
