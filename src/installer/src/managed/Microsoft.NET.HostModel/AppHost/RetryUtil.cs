// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            bool IsWin32FileLockError(int hresult)
            {
                // Error codes are defined in winerror.h
                const int ErrorLockViolation = 33;
                const int ErrorDriveLocked = 108;

                // The error code is stored in the lowest 16 bits of the HResult
                int errorCode = hresult & 0xffff;

                return errorCode == ErrorLockViolation || errorCode == ErrorDriveLocked;
            }

            for (int i = 1; i <= NumberOfRetries; i++)
            {
                try
                {
                    func();
                    break;
                }
                catch (HResultException hrex)
                    when (i < NumberOfRetries && IsWin32FileLockError(hrex.Win32HResult))
                {
                    Thread.Sleep(NumMilliSecondsToWait);
                }
            }
        }
    }
}
