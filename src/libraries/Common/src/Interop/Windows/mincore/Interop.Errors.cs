// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal partial class Interop
{
    internal partial class mincore
    {
        internal partial class Errors
        {
            internal const int ERROR_SUCCESS = 0x0;
            internal const int ERROR_FILE_NOT_FOUND = 0x2;
            internal const int ERROR_ACCESS_DENIED = 0x5;
            internal const int ERROR_INVALID_HANDLE = 0x6;
            internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
            internal const int ERROR_MORE_DATA = 0xEA;
            internal const int ERROR_DLL_INIT_FAILED = 0x45A;
            internal const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;
        }
    }
}