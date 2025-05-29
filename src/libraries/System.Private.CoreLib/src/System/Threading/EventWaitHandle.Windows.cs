// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public partial class EventWaitHandle
    {
        private const uint AccessRights = (uint)Interop.Kernel32.MAXIMUM_ALLOWED | Interop.Kernel32.SYNCHRONIZE | Interop.Kernel32.EVENT_MODIFY_STATE;

#if TARGET_WINDOWS
        // Can't use MAXIMUM_ALLOWED in an access control entry (ACE)
        private const int CurrentUserOnlyAceRights =
            Interop.Kernel32.STANDARD_RIGHTS_REQUIRED | Interop.Kernel32.SYNCHRONIZE | Interop.Kernel32.EVENT_MODIFY_STATE;
#endif

        private EventWaitHandle(SafeWaitHandle handle)
        {
            SafeWaitHandle = handle;
        }

        private unsafe void CreateEventCore(bool initialState, EventResetMode mode)
        {
            ValidateMode(mode);

            uint flags = initialState ? Interop.Kernel32.CREATE_EVENT_INITIAL_SET : 0;
            if (mode == EventResetMode.ManualReset)
                flags |= Interop.Kernel32.CREATE_EVENT_MANUAL_RESET;
            SafeWaitHandle handle = Interop.Kernel32.CreateEventEx(lpSecurityAttributes: 0, name: null, flags, AccessRights);
            if (handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastPInvokeError();
                handle.SetHandleAsInvalid();
                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }

            SafeWaitHandle = handle;
        }

        private unsafe void CreateEventCore(
            bool initialState,
            EventResetMode mode,
            string? name,
            NamedWaitHandleOptionsInternal options,
            out bool createdNew)
        {
            ValidateMode(mode);

#if !TARGET_WINDOWS
            if (name != null)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
            }
#endif

            void* securityAttributesPtr = null;
            SafeWaitHandle handle;
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
                uint eventFlags = initialState ? Interop.Kernel32.CREATE_EVENT_INITIAL_SET : 0;
                if (mode == EventResetMode.ManualReset)
                    eventFlags |= Interop.Kernel32.CREATE_EVENT_MANUAL_RESET;
                handle = Interop.Kernel32.CreateEventEx((nint)securityAttributesPtr, name, eventFlags, AccessRights);
                errorCode = Marshal.GetLastPInvokeError();

                if (handle.IsInvalid)
                {
                    handle.SetHandleAsInvalid();
                    if (!string.IsNullOrEmpty(name) && errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                        throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
                }
#if TARGET_WINDOWS

                if (errorCode == Interop.Errors.ERROR_ALREADY_EXISTS && securityAttributesPtr != null)
                {
                    try
                    {
                        if (!Thread.CurrentUserSecurityDescriptorInfo.IsSecurityDescriptorCompatible(
                                securityDescriptorInfo.TokenUser,
                                handle,
                                Interop.Kernel32.EVENT_MODIFY_STATE))
                        {
                            throw new WaitHandleCannotBeOpenedException(SR.Format(SR.NamedWaitHandles_ExistingObjectIncompatibleWithCurrentUserOnly, name));
                        }
                    }
                    catch
                    {
                        handle.Dispose();
                        throw;
                    }
                }
            }
#endif

            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
            SafeWaitHandle = handle;
        }

        private static OpenExistingResult OpenExistingWorker(
            string name,
            NamedWaitHandleOptionsInternal options,
            out EventWaitHandle? result)
        {
#if TARGET_WINDOWS
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (options.WasSpecified)
            {
                name = options.GetNameWithSessionPrefix(name);
            }

            SafeWaitHandle myHandle = Interop.Kernel32.OpenEvent(AccessRights, false, name);

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

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
            }

            if (options.WasSpecified && options.CurrentUserOnly)
            {
                try
                {
                    if (!Thread.CurrentUserSecurityDescriptorInfo.IsValidSecurityDescriptor(
                            myHandle,
                            Interop.Kernel32.EVENT_MODIFY_STATE))
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

            result = new EventWaitHandle(myHandle);
            return OpenExistingResult.Success;
#else
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
#endif
        }

        public bool Reset()
        {
            bool res = Interop.Kernel32.ResetEvent(SafeWaitHandle);
            if (!res)
                throw Win32Marshal.GetExceptionForLastWin32Error();
            return res;
        }

        public bool Set()
        {
            bool res = Interop.Kernel32.SetEvent(SafeWaitHandle);
            if (!res)
                throw Win32Marshal.GetExceptionForLastWin32Error();
            return res;
        }

        internal static bool Set(SafeWaitHandle waitHandle)
        {
            return Interop.Kernel32.SetEvent(waitHandle);
        }
    }
}
