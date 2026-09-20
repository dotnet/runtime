// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
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

#if TARGET_UNIX
        private static bool UnsafeDefaultMutexScopeGlobal { get; } =
            AppContext.TryGetSwitch("System.Threading.Mutex.UnsafeDefaultMutexScopeGlobal", out bool enabled) ? enabled : false;
#endif

        private void CreateMutexCore(bool initiallyOwned, string? name, out bool createdNew)
        {
            uint mutexFlags = initiallyOwned ? Interop.Kernel32.CREATE_MUTEX_INITIAL_OWNER : 0;
#if TARGET_UNIX
            SafeWaitHandle mutexHandle = !string.IsNullOrEmpty(name)
                ? CreateMutexExForScope(
                    IntPtr.Zero,
                    name,
                    mutexFlags,
                    AccessRights,
                    isUserScope: !UnsafeDefaultMutexScopeGlobal)
                : Interop.Kernel32.CreateMutexEx(IntPtr.Zero, name, mutexFlags, AccessRights);
#else
            SafeWaitHandle mutexHandle = Interop.Kernel32.CreateMutexEx(IntPtr.Zero, name, mutexFlags, AccessRights);
#endif
            int errorCode = Marshal.GetLastPInvokeError();

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

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
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
#if TARGET_UNIX
            SafeWaitHandle myHandle =
                OpenMutexForScope(AccessRights, false, name, isUserScope: !UnsafeDefaultMutexScopeGlobal);
#else
            SafeWaitHandle myHandle = Interop.Kernel32.OpenMutex(AccessRights, false, name);
#endif

            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastPInvokeError();

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
            if (!Interop.Kernel32.ReleaseMutex(SafeWaitHandle))
            {
                throw new ApplicationException(SR.Arg_SynchronizationLockException);
            }
        }

#if TARGET_UNIX
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CreateMutexExWForScope", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial SafeWaitHandle CreateMutexExForScope(
            IntPtr mutexAttributes,
            string name,
            uint flags,
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool isUserScope);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "OpenMutexWForScope", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial SafeWaitHandle OpenMutexForScope(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            string name,
            [MarshalAs(UnmanagedType.Bool)] bool isUserScope);
#endif
    }
}
