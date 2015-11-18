// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class Http
    {
        // Enum for constants defined for the enum CURLcode in curl.h
        internal enum CURLcode
        {
            CURLE_OK = 0,
            CURLE_UNSUPPORTED_PROTOCOL  =  1,
            CURLE_NOT_BUILT_IN = 4,
            CURLE_COULDNT_RESOLVE_HOST  =  6,
            CURLE_ABORTED_BY_CALLBACK = 42,
            CURLE_UNKNOWN_OPTION = 48,
        }
    }
}
