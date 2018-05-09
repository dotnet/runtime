// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.IO;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// Synchronization primitive that can also be used for interprocess synchronization
    /// </summary>
    public sealed class Mutex : WaitHandle
    {
        private const uint AccessRights =
            (uint)Win32Native.MAXIMUM_ALLOWED | Win32Native.SYNCHRONIZE | Win32Native.MUTEX_MODIFY_STATE;

#if PLATFORM_UNIX
        // Maximum file name length on tmpfs file system.
        private const int WaitHandleNameMax = 255;
#endif

        public Mutex(bool initiallyOwned, string name, out bool createdNew)
        {
#if !PLATFORM_UNIX
            VerifyNameForCreate(name);
#endif
            CreateMutexCore(initiallyOwned, name, out createdNew);
        }

        public Mutex(bool initiallyOwned, string name)
        {
#if !PLATFORM_UNIX
            VerifyNameForCreate(name);
#endif
            CreateMutexCore(initiallyOwned, name, out _);
        }

        public Mutex(bool initiallyOwned)
        {
            CreateMutexCore(initiallyOwned, null, out _);
        }

        public Mutex()
        {
            CreateMutexCore(false, null, out _);
        }

        private Mutex(SafeWaitHandle handle)
        {
            SafeWaitHandle = handle;
        }

        public static Mutex OpenExisting(string name)
        {
            switch (OpenExistingWorker(name, out Mutex result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();

                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                case OpenExistingResult.PathNotFound:
                    throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_PATH_NOT_FOUND, name);

                default:
                    return result;
            }
        }

        public static bool TryOpenExisting(string name, out Mutex result) =>
            OpenExistingWorker(name, out result) == OpenExistingResult.Success;

        // Note: To call ReleaseMutex, you must have an ACL granting you
        // MUTEX_MODIFY_STATE rights (0x0001). The other interesting value
        // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
        public void ReleaseMutex()
        {
            if (!Win32Native.ReleaseMutex(safeWaitHandle))
            {
                throw new ApplicationException(SR.Arg_SynchronizationLockException);
            }
        }

#if !PLATFORM_UNIX
        private static void VerifyNameForCreate(string name)
        {
            if (name != null && (Interop.Kernel32.MAX_PATH < name.Length))
            {
                throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, name, Interop.Kernel32.MAX_PATH), nameof(name));
            }
        }
#endif

        private void CreateMutexCore(bool initiallyOwned, string name, out bool createdNew)
        {
#if !PLATFORM_UNIX
            Debug.Assert(name == null || name.Length <= Interop.Kernel32.MAX_PATH);
#endif

            uint mutexFlags = initiallyOwned ? Win32Native.CREATE_MUTEX_INITIAL_OWNER : 0;

            SafeWaitHandle mutexHandle = Win32Native.CreateMutexEx(null, name, mutexFlags, AccessRights);
            int errorCode = Marshal.GetLastWin32Error();

            if (mutexHandle.IsInvalid)
            {
                mutexHandle.SetHandleAsInvalid();
#if PLATFORM_UNIX
                if (errorCode == Interop.Errors.ERROR_FILENAME_EXCED_RANGE)
                    // On Unix, length validation is done by CoreCLR's PAL after converting to utf-8
                    throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, name, WaitHandleNameMax), nameof(name));
#endif
                if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
            }

            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
            SafeWaitHandle = mutexHandle;
        }

        private static OpenExistingResult OpenExistingWorker(string name, out Mutex result)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));
            }

#if !PLATFORM_UNIX
            VerifyNameForCreate(name);
#endif

            result = null;
            // To allow users to view & edit the ACL's, call OpenMutex
            // with parameters to allow us to view & edit the ACL.  This will
            // fail if we don't have permission to view or edit the ACL's.  
            // If that happens, ask for less permissions.
            SafeWaitHandle myHandle = Win32Native.OpenMutex(AccessRights, false, name);

            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

#if PLATFORM_UNIX
                if (name != null && errorCode == Interop.Errors.ERROR_FILENAME_EXCED_RANGE)
                {
                    // On Unix, length validation is done by CoreCLR's PAL after converting to utf-8
                    throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, name, WaitHandleNameMax), nameof(name));
                }
#endif
                if (Interop.Errors.ERROR_FILE_NOT_FOUND == errorCode || Interop.Errors.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Interop.Errors.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && Interop.Errors.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;

                // this is for passed through Win32Native Errors
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
            }

            result = new Mutex(myHandle);

            return OpenExistingResult.Success;
        }
    }
}
