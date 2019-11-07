// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Http
    {
        // Enum for constants defined for the enum CURLcode in curl.h
        internal enum CURLcode
        {
            CURLE_OK = 0,
            CURLE_UNSUPPORTED_PROTOCOL  =  1,
            CURLE_FAILED_INIT = 2,
            CURLE_NOT_BUILT_IN = 4,
            CURLE_COULDNT_RESOLVE_HOST  =  6,
            CURLE_OUT_OF_MEMORY = 27,
            CURLE_OPERATION_TIMEDOUT = 28,
            CURLE_ABORTED_BY_CALLBACK = 42,
            CURLE_UNKNOWN_OPTION = 48,
            CURLE_RECV_ERROR = 56,
            CURLE_SEND_FAIL_REWIND = 65
        }
    }
}
