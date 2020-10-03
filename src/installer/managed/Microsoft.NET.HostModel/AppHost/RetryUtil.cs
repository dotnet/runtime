// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace Microsoft.NET.HostModel
{
    /// <summary>
    /// HostModel library implements several services for updating the AppHost DLL.
    /// These updates involve multiple file open/close operations.
    /// An Antivirus scanner may intercept in-between and lock the file,
    /// causing the operations to fail with IO-Error.
    /// So, the operations are retried a few times on failures such as
    /// - IOException
    /// - Failure with Win32 errors indicating file-lock
    /// </summary>
    public static class RetryUtil
    {
        public const int NumberOfRetries = 500;
        public const int NumMilliSecondsToWait = 100;

        public static void RetryOnIOError(Action func)
        {
            for (int i = 1; i <= NumberOfRetries; i++)
            {
                try
                {
                    func();
                    break;
                }
                catch (IOException) when (i < NumberOfRetries)
                {
                    Thread.Sleep(NumMilliSecondsToWait);
                }
            }
        }

        public static void RetryOnWin32Error(Action func)
        {
            bool IsKnownIrrecoverableError(int hresult)
            {
                // Error codes are defined in winerror.h
                // The error code is stored in the lowest 16 bits of the HResult

                switch (hresult & 0xffff)
                {
                    case 0x00000001: // ERROR_INVALID_FUNCTION
                    case 0x00000002: // ERROR_FILE_NOT_FOUND
                    case 0x00000003: // ERROR_PATH_NOT_FOUND
                    case 0x00000006: // ERROR_INVALID_HANDLE
                    case 0x00000008: // ERROR_NOT_ENOUGH_MEMORY
                    case 0x0000000B: // ERROR_BAD_FORMAT
                    case 0x0000000E: // ERROR_OUTOFMEMORY
                    case 0x0000000F: // ERROR_INVALID_DRIVE
                    case 0x00000012: // ERROR_NO_MORE_FILES
                    case 0x00000035: // ERROR_BAD_NETPATH
                    case 0x00000057: // ERROR_INVALID_PARAMETER
                    case 0x00000071: // ERROR_NO_MORE_SEARCH_HANDLES
                    case 0x00000072: // ERROR_INVALID_TARGET_HANDLE
                    case 0x00000078: // ERROR_CALL_NOT_IMPLEMENTED
                    case 0x0000007B: // ERROR_INVALID_NAME
                    case 0x0000007C: // ERROR_INVALID_LEVEL
                    case 0x0000007D: // ERROR_NO_VOLUME_LABEL
                    case 0x0000009A: // ERROR_LABEL_TOO_LONG
                    case 0x000000A0: // ERROR_BAD_ARGUMENTS
                    case 0x000000A1: // ERROR_BAD_PATHNAME
                    case 0x000000CE: // ERROR_FILENAME_EXCED_RANGE
                    case 0x000000DF: // ERROR_FILE_TOO_LARGE
                    case 0x000003ED: // ERROR_UNRECOGNIZED_VOLUME
                    case 0x000003EE: // ERROR_FILE_INVALID
                    case 0x00000651: // ERROR_DEVICE_REMOVED
                        return true;

                    default:
                        return false;
                }
            }

            for (int i = 1; i <= NumberOfRetries; i++)
            {
                try
                {
                    func();
                    break;
                }
                catch (HResultException hrex)
                    when (i < NumberOfRetries && !IsKnownIrrecoverableError(hrex.Win32HResult))
                {
                    Thread.Sleep(NumMilliSecondsToWait);
                }
            }
        }
    }
}
