// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class GlobalizationInterop
    {
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern int CompareString(byte[] localeName, char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len, CompareOptions options);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern int IndexOf(byte[] localeName, string target, char* pSource, int cwSourceLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern int LastIndexOf(byte[] localeName, string target, char* pSource, int cwSourceLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern int IndexOfOrdinalIgnoreCase(string target, int cwTargetLength, char* pSource, int cwSourceLength, bool findLast);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal unsafe static extern bool StartsWith(byte[] localeName, string target, string source, int cwSourceLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal unsafe static extern bool EndsWith(byte[] localeName, string target, string source, int cwSourceLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern int GetSortKey(byte[] localeName, string str, int strLength, byte* sortKey, int sortKeyLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern int CompareStringOrdinalIgnoreCase(char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len);
    }
}
