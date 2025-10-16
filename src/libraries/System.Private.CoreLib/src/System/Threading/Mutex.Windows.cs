// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    /// Synchronization primitive that can also be used for interprocess synchronization
    /// </summary>
    public sealed partial class Mutex : WaitHandle
    {
        private const uint AccessRights =
            (uint)Interop.Kernel32.MAXIMUM_ALLOWED | Interop.Kernel32.SYNCHRONIZE | Interop.Kernel32.MUTEX_MODIFY_STATE;

        // Can't use MAXIMUM_ALLOWED in an access control entry (ACE)
        private const int CurrentUserOnlyAceRights =
            Interop.Kernel32.STANDARD_RIGHTS_REQUIRED | Interop.Kernel32.SYNCHRONIZE | Interop.Kernel32.MUTEX_MODIFY_STATE;

        private void CreateMutexCore(bool initiallyOwned)
        {
            uint flags = initiallyOwned ? Interop.Kernel32.CREATE_MUTEX_INITIAL_OWNER : 0;
            SafeWaitHandle handle = Interop.Kernel32.CreateMutexEx(lpMutexAttributes: 0, name: null, flags, AccessRights);
            if (handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastPInvokeError();
                handle.SetHandleAsInvalid();
                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }

            SafeWaitHandle = handle;
        }

        private unsafe void CreateMutexCore(
            bool initiallyOwned,
            string? name,
            NamedWaitHandleOptionsInternal options,
            out bool createdNew)
        {
            Thread.CurrentUserSecurityDescriptorInfo securityDescriptorInfo = default;
            Interop.Kernel32.SECURITY_ATTRIBUTES securityAttributes = default;
            Interop.Kernel32.SECURITY_ATTRIBUTES* securityAttributesPtr = null;
            if (!string.IsNullOrEmpty(name) && options.WasSpecified)
            {
                name = options.GetNameWithSessionPrefix(name);
                if (options.CurrentUserOnly)
                {
                    securityDescriptorInfo = new(CurrentUserOnlyAceRights);
                    securityAttributes.nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES);
                    securityAttributes.lpSecurityDescriptor = (void*)securityDescriptorInfo.SecurityDescriptor;
                    securityAttributesPtr = &securityAttributes;
                }
            }

            SafeWaitHandle mutexHandle;
            int errorCode;
            using (securityDescriptorInfo)
            {
                uint mutexFlags = initiallyOwned ? Interop.Kernel32.CREATE_MUTEX_INITIAL_OWNER : 0;
                mutexHandle = Interop.Kernel32.CreateMutexEx((nint)securityAttributesPtr, name, mutexFlags, AccessRights);
                errorCode = Marshal.GetLastPInvokeError();

                if (mutexHandle.IsInvalid)
                {
                    mutexHandle.SetHandleAsInvalid();
                    if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                        throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
                }

                if (errorCode == Interop.Errors.ERROR_ALREADY_EXISTS && securityAttributesPtr != null)
                {
                    try
                    {
                        if (!Thread.CurrentUserSecurityDescriptorInfo.IsSecurityDescriptorCompatible(
                                securityDescriptorInfo.TokenUser,
                                mutexHandle,
                                Interop.Kernel32.MUTEX_MODIFY_STATE))
                        {
                            throw new WaitHandleCannotBeOpenedException(SR.Format(SR.NamedWaitHandles_ExistingObjectIncompatibleWithCurrentUserOnly, name));
                        }
                    }
                    catch
                    {
                        mutexHandle.Dispose();
                        throw;
                    }
                }
            }

            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
            SafeWaitHandle = mutexHandle;
        }

        private static OpenExistingResult OpenExistingWorker(
            string name,
            NamedWaitHandleOptionsInternal options,
            out Mutex? result)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (options.WasSpecified)
            {
                name = options.GetNameWithSessionPrefix(name);
            }

            // To allow users to view & edit the ACL's, call OpenMutex
            // with parameters to allow us to view & edit the ACL.  This will
            // fail if we don't have permission to view or edit the ACL's.
            // If that happens, ask for less permissions.
            SafeWaitHandle myHandle = Interop.Kernel32.OpenMutex(AccessRights, false, name);

            if (myHandle.IsInvalid)
            {
                result = null;
                int errorCode = Marshal.GetLastPInvokeError();

                myHandle.Dispose();

                if (Interop.Errors.ERROR_FILE_NOT_FOUND == errorCode || Interop.Errors.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Interop.Errors.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (Interop.Errors.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
            }

            if (options.WasSpecified && options.CurrentUserOnly)
            {
                try
                {
                    if (!Thread.CurrentUserSecurityDescriptorInfo.IsValidSecurityDescriptor(
                            myHandle,
                            Interop.Kernel32.MUTEX_MODIFY_STATE))
                    {
                        myHandle.Dispose();
                        result = null;
                        return OpenExistingResult.ObjectIncompatibleWithCurrentUserOnly;
                    }
                }
                catch
                {
                    myHandle.Dispose();
                    throw;
                }
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
    }
}
