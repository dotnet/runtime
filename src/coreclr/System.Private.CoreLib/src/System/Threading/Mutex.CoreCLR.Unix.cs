// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    /// Synchronization primitive that can also be used for interprocess synchronization
    /// </summary>
    public sealed partial class Mutex : WaitHandle
    {
        private void CreateMutexCore(bool initiallyOwned, string? name, out bool createdNew)
        {
            SafeWaitHandle mutexHandle = CreateMutexCore(initiallyOwned, name, out int errorCode, out string? errorDetails);
            if (mutexHandle.IsInvalid)
            {
                mutexHandle.SetHandleAsInvalid();
                if (errorCode == Interop.Errors.ERROR_FILENAME_EXCED_RANGE)
                    // On Unix, length validation is done by CoreCLR's PAL after converting to utf-8
                    throw new ArgumentException(SR.Argument_WaitHandleNameTooLong, nameof(name));
                if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name, errorDetails);
            }

            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
            SafeWaitHandle = mutexHandle;
        }

        private static OpenExistingResult OpenExistingWorker(string name, out Mutex? result)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            result = null;
            // To allow users to view & edit the ACL's, call OpenMutex
            // with parameters to allow us to view & edit the ACL.  This will
            // fail if we don't have permission to view or edit the ACL's.
            // If that happens, ask for less permissions.
            SafeWaitHandle myHandle = OpenMutexCore(name, out int errorCode, out string? errorDetails);

            if (myHandle.IsInvalid)
            {
                myHandle.Dispose();

                if (errorCode == Interop.Errors.ERROR_FILENAME_EXCED_RANGE)
                {
                    // On Unix, length validation is done by CoreCLR's PAL after converting to utf-8
                    throw new ArgumentException(SR.Argument_WaitHandleNameTooLong, nameof(name));
                }
                if (Interop.Errors.ERROR_FILE_NOT_FOUND == errorCode || Interop.Errors.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Interop.Errors.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (Interop.Errors.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name, errorDetails);
            }

            result = new Mutex(myHandle);
            return OpenExistingResult.Success;
        }

        // Note: To call ReleaseMutex, you must have an ACL granting you
        // MUTEX_MODIFY_STATE rights (0x0001). The other interesting value
        // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
        public void ReleaseMutex()
        {
            if (!Interop.Kernel32.ReleaseMutex(SafeWaitHandle))
            {
                throw new ApplicationException(SR.Arg_SynchronizationLockException);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Unix-specific implementation

        private const int SystemCallErrorsBufferSize = 256;

        private static unsafe SafeWaitHandle CreateMutexCore(
            bool initialOwner,
            string? name,
            out int errorCode,
            out string? errorDetails)
        {
            byte* systemCallErrors = stackalloc byte[SystemCallErrorsBufferSize];
            SafeWaitHandle mutexHandle = CreateMutex(initialOwner, name, systemCallErrors, SystemCallErrorsBufferSize);

            // Get the error code even if the handle is valid, as it could be ERROR_ALREADY_EXISTS, indicating that the mutex
            // already exists and was opened
            errorCode = Marshal.GetLastPInvokeError();

            errorDetails = mutexHandle.IsInvalid ? GetErrorDetails(systemCallErrors) : null;
            return mutexHandle;
        }

        private static unsafe SafeWaitHandle OpenMutexCore(string name, out int errorCode, out string? errorDetails)
        {
            byte* systemCallErrors = stackalloc byte[SystemCallErrorsBufferSize];
            SafeWaitHandle mutexHandle = OpenMutex(name, systemCallErrors, SystemCallErrorsBufferSize);
            errorCode = mutexHandle.IsInvalid ? Marshal.GetLastPInvokeError() : Interop.Errors.ERROR_SUCCESS;
            errorDetails = mutexHandle.IsInvalid ? GetErrorDetails(systemCallErrors) : null;
            return mutexHandle;
        }

        private static unsafe string? GetErrorDetails(byte* systemCallErrors)
        {
            int systemCallErrorsLength =
                new ReadOnlySpan<byte>(systemCallErrors, SystemCallErrorsBufferSize).IndexOf((byte)'\0');
            if (systemCallErrorsLength > 0)
            {
                try
                {
                    return
                        SR.Format(SR.Unix_SystemCallErrors, Encoding.UTF8.GetString(systemCallErrors, systemCallErrorsLength));
                }
                catch { } // avoid hiding the original error due to an error here
            }

            return null;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PAL_CreateMutexW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial SafeWaitHandle CreateMutex([MarshalAs(UnmanagedType.Bool)] bool initialOwner, string? name, byte* systemCallErrors, uint systemCallErrorsBufferSize);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PAL_OpenMutexW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial SafeWaitHandle OpenMutex(string name, byte* systemCallErrors, uint systemCallErrorsBufferSize);
    }
}
