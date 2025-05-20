// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Semaphore
    {
        private const uint AccessRights = (uint)Interop.Kernel32.MAXIMUM_ALLOWED | Interop.Kernel32.SYNCHRONIZE | Interop.Kernel32.SEMAPHORE_MODIFY_STATE;

#if TARGET_WINDOWS
        // Can't use MAXIMUM_ALLOWED in an access control entry (ACE)
        private const int CurrentUserOnlyAceRights =
            Interop.Kernel32.STANDARD_RIGHTS_REQUIRED | Interop.Kernel32.SYNCHRONIZE | Interop.Kernel32.SEMAPHORE_MODIFY_STATE;
#endif

        private Semaphore(SafeWaitHandle handle)
        {
            SafeWaitHandle = handle;
        }

        private void CreateSemaphoreCore(int initialCount, int maximumCount)
        {
            ValidateArguments(initialCount, maximumCount);

            SafeWaitHandle handle =
                Interop.Kernel32.CreateSemaphoreEx(
                    lpSecurityAttributes: 0,
                    initialCount,
                    maximumCount,
                    name: null,
                    flags: 0,
                    AccessRights);
            if (handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastPInvokeError();
                handle.SetHandleAsInvalid();
                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }

            SafeWaitHandle = handle;
        }

        private unsafe void CreateSemaphoreCore(
            int initialCount,
            int maximumCount,
            string? name,
            NamedWaitHandleOptionsInternal options,
            out bool createdNew)
        {
            ValidateArguments(initialCount, maximumCount);

#if !TARGET_WINDOWS
            if (name != null)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
            }
#endif

            void* securityAttributesPtr = null;
            SafeWaitHandle myHandle;
            int errorCode;
#if TARGET_WINDOWS
            Thread.CurrentUserSecurityDescriptorInfo securityDescriptorInfo = default;
            Interop.Kernel32.SECURITY_ATTRIBUTES securityAttributes = default;
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

            using (securityDescriptorInfo)
            {
#endif
                myHandle =
                    Interop.Kernel32.CreateSemaphoreEx(
                        (nint)securityAttributesPtr,
                        initialCount,
                        maximumCount,
                        name,
                        flags: 0,
                        AccessRights);
                errorCode = Marshal.GetLastPInvokeError();

                if (myHandle.IsInvalid)
                {
                    myHandle.SetHandleAsInvalid();

                    if (!string.IsNullOrEmpty(name) && errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    {
                        throw new WaitHandleCannotBeOpenedException(
                            SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
                }
#if TARGET_WINDOWS

                if (errorCode == Interop.Errors.ERROR_ALREADY_EXISTS && securityAttributesPtr != null)
                {
                    try
                    {
                        if (!Thread.CurrentUserSecurityDescriptorInfo.IsSecurityDescriptorCompatible(
                                securityDescriptorInfo.TokenUser,
                                myHandle,
                                Interop.Kernel32.SEMAPHORE_MODIFY_STATE))
                        {
                            throw new WaitHandleCannotBeOpenedException(SR.Format(SR.NamedWaitHandles_ExistingObjectIncompatibleWithCurrentUserOnly, name));
                        }
                    }
                    catch
                    {
                        myHandle.Dispose();
                        throw;
                    }
                }
            }
#endif

            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
            this.SafeWaitHandle = myHandle;
        }

        private static OpenExistingResult OpenExistingWorker(
            string name,
            NamedWaitHandleOptionsInternal options,
            out Semaphore? result)
        {
#if TARGET_WINDOWS
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (options.WasSpecified)
            {
                name = options.GetNameWithSessionPrefix(name);
            }

            // Pass false to OpenSemaphore to prevent inheritedHandles
            SafeWaitHandle myHandle = Interop.Kernel32.OpenSemaphore(AccessRights, false, name);

            if (myHandle.IsInvalid)
            {
                result = null;
                int errorCode = Marshal.GetLastPInvokeError();

                myHandle.Dispose();

                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND || errorCode == Interop.Errors.ERROR_INVALID_NAME)
                    return OpenExistingResult.NameNotFound;
                if (errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND)
                    return OpenExistingResult.PathNotFound;
                if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    return OpenExistingResult.NameInvalid;

                // this is for passed through NativeMethods Errors
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            if (options.WasSpecified && options.CurrentUserOnly)
            {
                try
                {
                    if (!Thread.CurrentUserSecurityDescriptorInfo.IsValidSecurityDescriptor(
                            myHandle,
                            Interop.Kernel32.SEMAPHORE_MODIFY_STATE))
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

            result = new Semaphore(myHandle);
            return OpenExistingResult.Success;
#else
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
#endif
        }

        private int ReleaseCore(int releaseCount)
        {
            if (!Interop.Kernel32.ReleaseSemaphore(SafeWaitHandle!, releaseCount, out int previousCount))
                throw new SemaphoreFullException();

            return previousCount;
        }
    }
}
