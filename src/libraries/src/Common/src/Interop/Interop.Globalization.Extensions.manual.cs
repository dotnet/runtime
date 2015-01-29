// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    // These are error codes we get back from the Normalization DLL
    internal const int ERROR_SUCCESS = 0;
    internal const int ERROR_NOT_ENOUGH_MEMORY = 8;
    internal const int ERROR_INVALID_PARAMETER = 87;
    internal const int ERROR_INSUFFICIENT_BUFFER = 122;
    internal const int ERROR_INVALID_NAME = 123;
    internal const int ERROR_NO_UNICODE_TRANSLATION = 1113;

    // The VM can override the last error code with this value in debug builds
    // so this value for us is equivalent to ERROR_SUCCESS
    internal const int LAST_ERROR_TRASH_VALUE = 42424;

    internal partial class mincore
    {
        //
        //  Idn APIs
        //

        [DllImport("api-ms-win-core-localization-l1-2-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int IdnToAscii(
                                        uint dwFlags,
                                        string lpUnicodeCharStr,
                                        int cchUnicodeChar,
                                        [System.Runtime.InteropServices.OutAttribute()]

                                        char[] lpASCIICharStr,
                                        int cchASCIIChar);

        [DllImport("api-ms-win-core-localization-l1-2-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int IdnToUnicode(
                                        uint dwFlags,
                                        string lpASCIICharStr,
                                        int cchASCIIChar,
                                        [System.Runtime.InteropServices.OutAttribute()]

                                        char[] lpUnicodeCharStr,
                                        int cchUnicodeChar);
        //
        //  Normalization APIs
        //

        [DllImport("api-ms-win-core-normalization-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool IsNormalizedString(int normForm, string source, int length);

        [DllImport("api-ms-win-core-normalization-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int NormalizeString(
                                        int normForm,
                                        string source,
                                        int sourceLength,
                                        [System.Runtime.InteropServices.OutAttribute()]
                                        char[] destenation,
                                        int destenationLength);


        internal const int IDN_ALLOW_UNASSIGNED = 0x1;
        internal const int IDN_USE_STD3_ASCII_RULES = 0x2;
    }
}




