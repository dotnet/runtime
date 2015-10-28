// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class LibCurl
    {
        // Class for constants defined for the enum CURLcode in curl.h
        internal static partial class CURLcode
        {
            internal const int CURLE_OK = 0;
            internal const int CURLE_UNSUPPORTED_PROTOCOL  =  1;
            internal const int CURLE_NOT_BUILT_IN = 4;
            internal const int CURLE_COULDNT_RESOLVE_HOST  =  6;
            internal const int CURLE_UNKNOWN_OPTION = 48;
        }
    }
}
