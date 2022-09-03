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

        private EventWaitHandle(SafeWaitHandle handle)
        {
            SafeWaitHandle = handle;
        }

        private void CreateEventCore(bool initialState, EventResetMode mode, string? name, out bool createdNew)
        {
#if TARGET_UNIX || TARGET_BROWSER
            if (name != null)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
#endif
            uint eventFlags = initialState ? Interop.Kernel32.CREATE_EVENT_INITIAL_SET : 0;
            if (mode == EventResetMode.ManualReset)
                eventFlags |= (uint)Interop.Kernel32.CREATE_EVENT_MANUAL_RESET;

            SafeWaitHandle handle = Interop.Kernel32.CreateEventEx(IntPtr.Zero, name, eventFlags, AccessRights);

            int errorCode = Marshal.GetLastPInvokeError();
            if (handle.IsInvalid)
            {
                handle.SetHandleAsInvalid();
                if (!string.IsNullOrEmpty(name) && errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
            }
            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
            SafeWaitHandle = handle;
        }

        private static OpenExistingResult OpenExistingWorker(string name, out EventWaitHandle? result)
        {
#if TARGET_WINDOWS
            ArgumentException.ThrowIfNullOrEmpty(name);

            result = null;
            SafeWaitHandle myHandle = Interop.Kernel32.OpenEvent(AccessRights, false, name);

            if (myHandle.IsInvalid)
            {
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
