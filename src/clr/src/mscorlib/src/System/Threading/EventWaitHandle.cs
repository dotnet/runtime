// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
/*=============================================================================
**
**
**
** Purpose: Base class for representing Events
**
**
=============================================================================*/


namespace System.Security.AccessControl
{
    internal class EventWaitHandleSecurity
    {
    }
    internal enum EventWaitHandleRights
    {
    }
}

namespace System.Threading
{
    using System;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.IO;
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.AccessControl;
    using System.Diagnostics.Contracts;

    [ComVisibleAttribute(true)]
    public class EventWaitHandle : WaitHandle
    {
        public EventWaitHandle(bool initialState, EventResetMode mode) : this(initialState, mode, null) { }

        public EventWaitHandle(bool initialState, EventResetMode mode, string name)
        {
            if (name != null)
            {
#if PLATFORM_UNIX
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
#else
                if (System.IO.Path.MaxPath < name.Length)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, Path.MaxPath), nameof(name));
                }
#endif
            }
            Contract.EndContractBlock();

            SafeWaitHandle _handle = null;
            switch (mode)
            {
                case EventResetMode.ManualReset:
                    _handle = Win32Native.CreateEvent(null, true, initialState, name);
                    break;
                case EventResetMode.AutoReset:
                    _handle = Win32Native.CreateEvent(null, false, initialState, name);
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidFlag, name));
            };

            if (_handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

                _handle.SetHandleAsInvalid();
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                __Error.WinIOError(errorCode, name);
            }
            SetHandleInternal(_handle);
        }

        public EventWaitHandle(bool initialState, EventResetMode mode, string name, out bool createdNew)
            : this(initialState, mode, name, out createdNew, null)
        {
        }

        internal unsafe EventWaitHandle(bool initialState, EventResetMode mode, string name, out bool createdNew, EventWaitHandleSecurity eventSecurity)
        {
            if (name != null)
            {
#if PLATFORM_UNIX
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
#else
                if (System.IO.Path.MaxPath < name.Length)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, Path.MaxPath), nameof(name));
                }
#endif
            }
            Contract.EndContractBlock();
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;

            SafeWaitHandle _handle = null;
            Boolean isManualReset;
            switch (mode)
            {
                case EventResetMode.ManualReset:
                    isManualReset = true;
                    break;
                case EventResetMode.AutoReset:
                    isManualReset = false;
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidFlag, name));
            };

            _handle = Win32Native.CreateEvent(secAttrs, isManualReset, initialState, name);
            int errorCode = Marshal.GetLastWin32Error();

            if (_handle.IsInvalid)
            {
                _handle.SetHandleAsInvalid();
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                __Error.WinIOError(errorCode, name);
            }
            createdNew = errorCode != Win32Native.ERROR_ALREADY_EXISTS;
            SetHandleInternal(_handle);
        }

        private EventWaitHandle(SafeWaitHandle handle)
        {
            SetHandleInternal(handle);
        }

        public static EventWaitHandle OpenExisting(string name)
        {
            return OpenExisting(name, (EventWaitHandleRights)0);
        }

        internal static EventWaitHandle OpenExisting(string name, EventWaitHandleRights rights)
        {
            EventWaitHandle result;
            switch (OpenExistingWorker(name, rights, out result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();

                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                case OpenExistingResult.PathNotFound:
                    __Error.WinIOError(Win32Native.ERROR_PATH_NOT_FOUND, "");
                    return result; //never executes

                default:
                    return result;
            }
        }

        public static bool TryOpenExisting(string name, out EventWaitHandle result)
        {
            return OpenExistingWorker(name, (EventWaitHandleRights)0, out result) == OpenExistingResult.Success;
        }

        private static OpenExistingResult OpenExistingWorker(string name, EventWaitHandleRights rights, out EventWaitHandle result)
        {
#if PLATFORM_UNIX
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
#else
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), SR.ArgumentNull_WithParamName);
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));
            }

            if (null != name && System.IO.Path.MaxPath < name.Length)
            {
                throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, Path.MaxPath), nameof(name));
            }

            Contract.EndContractBlock();

            result = null;

            SafeWaitHandle myHandle = Win32Native.OpenEvent(Win32Native.EVENT_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);

            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (Win32Native.ERROR_FILE_NOT_FOUND == errorCode || Win32Native.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Win32Native.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;
                //this is for passed through Win32Native Errors
                __Error.WinIOError(errorCode, "");
            }
            result = new EventWaitHandle(myHandle);
            return OpenExistingResult.Success;
#endif
        }
        public bool Reset()
        {
            bool res = Win32Native.ResetEvent(safeWaitHandle);
            if (!res)
                __Error.WinIOError();
            return res;
        }
        public bool Set()
        {
            bool res = Win32Native.SetEvent(safeWaitHandle);

            if (!res)
                __Error.WinIOError();

            return res;
        }
    }
}

