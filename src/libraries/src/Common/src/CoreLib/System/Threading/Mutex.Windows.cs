// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

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
            SafeWaitHandle mutexHandle = Interop.Kernel32.CreateMutexEx(IntPtr.Zero, name, mutexFlags, AccessRights);
            int errorCode = Marshal.GetLastWin32Error();

            if (mutexHandle.IsInvalid)
            {
                mutexHandle.SetHandleAsInvalid();
#if !PLATFORM_WINDOWS
                if (errorCode == Interop.Errors.ERROR_FILENAME_EXCED_RANGE)
                    // On Unix, length validation is done by CoreCLR's PAL after converting to utf-8
                    throw new ArgumentException(SR.Argument_WaitHandleNameTooLong, nameof(name));
#endif
                if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
            }

            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
            SafeWaitHandle = mutexHandle;
        }

        private static OpenExistingResult OpenExistingWorker(string name, out Mutex? result)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));
            }

            result = null;
            // To allow users to view & edit the ACL's, call OpenMutex
            // with parameters to allow us to view & edit the ACL.  This will
            // fail if we don't have permission to view or edit the ACL's.
            // If that happens, ask for less permissions.
            SafeWaitHandle myHandle = Interop.Kernel32.OpenMutex(AccessRights, false, name);

            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();
#if !PLATFORM_WINDOWS
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

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
            }

            result = new Mutex(myHandle);
            return OpenExistingResult.Success;
        }

        // Note: To call ReleaseMutex, you must have an ACL granting you
        // MUTEX_MODIFY_STATE rights (0x0001). The other interesting value
        // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
        public void ReleaseMutex()
        {
            if (!Interop.Kernel32.ReleaseMutex(SafeWaitHandle!)) // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/2384
            {
                throw new ApplicationException(SR.Arg_SynchronizationLockException);
            }
        }
    }
}
