// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    /// <summary>
    /// Contains CLR specific constants
    /// </summary>
    internal static class Constants
    {
        internal const int CLR_KEYLEN = 1;
        internal const int CLR_PUBLICKEYONLY = 2;
        internal const int CLR_EXPORTABLE = 3;
        internal const int CLR_REMOVABLE = 4;
        internal const int CLR_HARDWARE = 5;
        internal const int CLR_ACCESSIBLE = 6;
        internal const int CLR_PROTECTED = 7;
        internal const int CLR_UNIQUE_CONTAINER = 8;
        internal const int CLR_ALGID = 9;
        internal const int CLR_PP_CLIENT_HWND = 10;
        internal const int CLR_PP_PIN = 11;

        internal const int SIZE_OF_DWORD = 4;
    }
}
