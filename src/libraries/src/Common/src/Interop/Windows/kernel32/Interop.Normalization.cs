// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    internal partial class Kernel32
    {
        //
        //  Normalization APIs
        //

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool IsNormalizedString(int normForm, string source, int length);

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int NormalizeString(
                                        int normForm,
                                        string source,
                                        int sourceLength,
                                        [System.Runtime.InteropServices.OutAttribute()]
                                        char[] destination,
                                        int destinationLength);
    }
}
