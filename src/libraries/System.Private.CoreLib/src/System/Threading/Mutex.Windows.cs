// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// Synchronization primitive that can also be used for interprocess synchronization
    /// </summary>
    public sealed partial class Mutex : WaitHandle
    {
        private const uint AccessRights =
            (uint)Interop.Kernel32.MAXIMUM_ALLOWED | Interop.Kernel32.SYNCHRONIZE | Interop.Kernel32.MUTEX_MODIFY_STATE;

        private void CreateMutexCore(bool initiallyOwned, string? name, out bool createdNew)
        {
            uint mutexFlags = initiallyOwned ? Interop.Kernel32.CREATE_MUTEX_INITIAL_OWNER : 0;
            SafeWaitHandle mutexHandle = null;
            int errorCode;
            string? systemCallErrors;
            Marshal.BeginTrackingSystemCallErrors();
            try
            {
                mutexHandle = Interop.Kernel32.CreateMutexEx(IntPtr.Zero, name, mutexFlags, AccessRights);
                errorCode = Marshal.GetLastPInvokeError();
            }
            finally
            {
                systemCallErrors =
                    Marshal.EndTrackingSystemCallErrors(getSystemCallErrors: mutexHandle != null && mutexHandle.IsInvalid);
            }

            if (mutexHandle.IsInvalid)
            {
                mutexHandle.SetHandleAsInvalid();
#if TARGET_UNIX || TARGET_BROWSER || TARGET_WASI
                if (errorCode == Interop.Errors.ERROR_FILENAME_EXCED_RANGE)
                    // On Unix, length validation is done by CoreCLR's PAL after converting to utf-8
                    throw new ArgumentException(SR.Argument_WaitHandleNameTooLong, nameof(name));
#endif
                if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                throw GetExceptionForWin32ErrorWithSystemCallError(errorCode, name, systemCallErrors);
            }

            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
            SafeWaitHandle = mutexHandle;
        }

        private static OpenExistingResult OpenExistingWorker(string name, out Mutex? result)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            result = null;
            SafeWaitHandle myHandle = null;
            int errorCode = 0;
            string? systemCallErrors;
            Marshal.BeginTrackingSystemCallErrors();
            try
            {
                // To allow users to view & edit the ACL's, call OpenMutex
                // with parameters to allow us to view & edit the ACL.  This will
                // fail if we don't have permission to view or edit the ACL's.
                // If that happens, ask for less permissions.
                myHandle = Interop.Kernel32.OpenMutex(AccessRights, false, name);
                if (myHandle.IsInvalid)
                {
                    errorCode = Marshal.GetLastPInvokeError();
                }
            }
            finally
            {
                systemCallErrors =
                    Marshal.EndTrackingSystemCallErrors(getSystemCallErrors: myHandle != null && myHandle.IsInvalid);
            }

            if (myHandle.IsInvalid)
            {
                myHandle.Dispose();

#if TARGET_UNIX || TARGET_BROWSER || TARGET_WASI
                if (errorCode == Interop.Errors.ERROR_FILENAME_EXCED_RANGE)
                {
                    // On Unix, length validation is done by CoreCLR's PAL after converting to utf-8
                    throw new ArgumentException(SR.Argument_WaitHandleNameTooLong, nameof(name));
                }
#endif
                if (Interop.Errors.ERROR_FILE_NOT_FOUND == errorCode || Interop.Errors.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Interop.Errors.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (Interop.Errors.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;

                throw GetExceptionForWin32ErrorWithSystemCallError(errorCode, name, systemCallErrors);
            }

            result = new Mutex(myHandle);
            return OpenExistingResult.Success;
        }

        // Note: To call ReleaseMutex, you must have an ACL granting you
        // MUTEX_MODIFY_STATE rights (0x0001). The other interesting value
        // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
        public void ReleaseMutex()
        {
            bool success = true;
            string? systemCallErrors;
            Marshal.BeginTrackingSystemCallErrors();
            try
            {
                success = Interop.Kernel32.ReleaseMutex(SafeWaitHandle);
            }
            finally
            {
                systemCallErrors = Marshal.EndTrackingSystemCallErrors(getSystemCallErrors: !success);
            }

            if (!success)
            {
                SystemException? innerEx =
                    string.IsNullOrEmpty(systemCallErrors) ? null : GetInnerExceptionForSystemCallErrors(systemCallErrors);
                throw new ApplicationException(SR.Arg_SynchronizationLockException, innerEx);
            }
        }

        private static SystemException GetInnerExceptionForSystemCallErrors(string systemCallErrors) =>
            new SystemException(SR.Format(SR.SystemException_SystemCallErrors, systemCallErrors));

        private static Exception GetExceptionForWin32ErrorWithSystemCallError(
            int errorCode,
            string? path,
            string? systemCallErrors)
        {
            Exception ex = Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            if (string.IsNullOrEmpty(systemCallErrors) || ex.GetType() != typeof(IOException))
            {
                return ex;
            }

            return new IOException(ex.Message, GetInnerExceptionForSystemCallErrors(systemCallErrors)) { HResult = ex.HResult };
        }
    }
}
