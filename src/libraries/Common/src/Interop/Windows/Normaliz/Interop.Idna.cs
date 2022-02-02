// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Normaliz
    {
        //
        //  Idn APIs
        //

        [GeneratedDllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial int IdnToAscii(
                                        uint dwFlags,
                                        char* lpUnicodeCharStr,
                                        int cchUnicodeChar,
                                        char* lpASCIICharStr,
                                        int cchASCIIChar);

        [GeneratedDllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial int IdnToUnicode(
                                        uint dwFlags,
                                        char* lpASCIICharStr,
                                        int cchASCIIChar,
                                        char* lpUnicodeCharStr,
                                        int cchUnicodeChar);

        internal const int IDN_ALLOW_UNASSIGNED = 0x1;
        internal const int IDN_USE_STD3_ASCII_RULES = 0x2;
    }
}
