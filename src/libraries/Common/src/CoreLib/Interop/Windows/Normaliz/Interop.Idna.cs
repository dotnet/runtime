// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Normaliz
    {
        //
        //  Idn APIs
        //

        [DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe int IdnToAscii(
                                        uint dwFlags,
                                        char* lpUnicodeCharStr,
                                        int cchUnicodeChar,
                                        char* lpASCIICharStr,
                                        int cchASCIIChar);

        [DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe int IdnToUnicode(
                                        uint dwFlags,
                                        char* lpASCIICharStr,
                                        int cchASCIIChar,
                                        char* lpUnicodeCharStr,
                                        int cchUnicodeChar);

        internal const int IDN_ALLOW_UNASSIGNED = 0x1;
        internal const int IDN_USE_STD3_ASCII_RULES = 0x2;
    }
}
