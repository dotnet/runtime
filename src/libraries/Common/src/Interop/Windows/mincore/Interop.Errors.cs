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
            internal const int ERROR_PATH_NOT_FOUND = 0x3;
            internal const int ERROR_ACCESS_DENIED = 0x5;
            internal const int ERROR_INVALID_HANDLE = 0x6;
            internal const int ERROR_SHARING_VIOLATION = 0x20;
            internal const int ERROR_NOT_READY = 21;
            internal const int ERROR_FILE_EXISTS = 0x50;
            internal const int ERROR_INVALID_PARAMETER = 0x57;
            internal const int ERROR_BROKEN_PIPE = 0x6D;
            internal const int ERROR_ALREADY_EXISTS = 0xB7;
            internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
            internal const int ERROR_NO_DATA = 0xE8;
            internal const int ERROR_MORE_DATA = 0xEA;
            internal const int ERROR_LOCK_FAILED = 167;
            internal const int ERROR_BUSY = 170;
            internal const int ERROR_BAD_EXE_FORMAT = 193;
            internal const int ERROR_EXE_MACHINE_TYPE_MISMATCH = 216;
            internal const int ERROR_PARTIAL_COPY = 299;
            internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
            internal const int ERROR_DLL_INIT_FAILED = 0x45A;
            internal const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;
            internal const int EFail = unchecked((int)0x80004005);
        }
    }
}
