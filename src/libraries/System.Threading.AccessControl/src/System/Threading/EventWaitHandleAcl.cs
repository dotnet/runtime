// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public static class EventWaitHandleAcl
    {
        /// <summary>Gets or creates an <see cref="EventWaitHandle" /> instance, allowing a <see cref="EventWaitHandleSecurity " /> instance to be optionally specified to set it during the event creation.</summary>
        /// <param name="initialState"><see langword="true" /> to set the initial state to signaled if the named event is created as a result of this call; <see langword="false" /> to set it to non-signaled.</param>
        /// <param name="mode">One of the enum values that determines whether the event resets automatically or manually.</param>
        /// <param name="name">The name, if the event is a system-wide synchronization event; otherwise, <see langword="null" /> or an empty string.</param>
        /// <param name="createdNew">When this method returns, this argument is always set to <see langword="true" /> if a local event is created; that is, when <paramref name="name" /> is <see langword="null" /> or <see cref="string.Empty" />. If <paramref name="name" /> has a valid, non-empty value, this argument is set to <see langword="true" /> when the system event is created, or it is set to <see langword="false" /> if an existing system event is found with that name. This parameter is passed uninitialized.</param>
        /// <param name="eventSecurity">The optional Windows access control security to apply.</param>
        /// <returns>An object that represents a system event wait handle, if named, or a local event wait handle, if nameless.</returns>
        /// <exception cref="ArgumentNullException">.NET Framework only: The <paramref name="name" /> length is beyond MAX_PATH (260 characters).</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="mode" /> enum value was out of legal range.</exception>
        /// <exception cref="DirectoryNotFoundException">Could not find a part of the path specified in <paramref name="name" />.</exception>
        /// <exception cref="WaitHandleCannotBeOpenedException">A system-wide synchronization event with the provided <paramref name="name" /> was not found.
        /// -or-
        /// An <see cref="EventWaitHandle" /> with system-wide name <paramref name="name" /> cannot be created. An <see cref="EventWaitHandle" /> of a different type might have the same name.</exception>
        /// <remarks>If a `name` is passed and the system event already exists, the existing event is returned. If `name` is `null` or <see cref="string.Empty" />, a new local event is always created.</remarks>
        public static unsafe EventWaitHandle Create(bool initialState, EventResetMode mode, string? name, out bool createdNew, EventWaitHandleSecurity? eventSecurity)
        {
            if (eventSecurity == null)
            {
                return new EventWaitHandle(initialState, mode, name, out createdNew);
            }

            if (mode != EventResetMode.AutoReset && mode != EventResetMode.ManualReset)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            uint eventFlags = initialState ? Interop.Kernel32.CREATE_EVENT_INITIAL_SET : 0;
            if (mode == EventResetMode.ManualReset)
            {
                eventFlags |= Interop.Kernel32.CREATE_EVENT_MANUAL_RESET;
            }

            fixed (byte* pSecurityDescriptor = eventSecurity.GetSecurityDescriptorBinaryForm())
            {
                var secAttrs = new Interop.Kernel32.SECURITY_ATTRIBUTES
                {
                    nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES),
                    lpSecurityDescriptor = (IntPtr)pSecurityDescriptor
                };

                SafeWaitHandle handle = Interop.Kernel32.CreateEventEx(
                    (IntPtr)(&secAttrs),
                    name,
                    eventFlags,
                    (uint)EventWaitHandleRights.FullControl);

                ValidateHandle(handle, name, out createdNew);

                EventWaitHandle ewh = new EventWaitHandle(initialState, mode);
                SafeWaitHandle old = ewh.SafeWaitHandle;
                ewh.SafeWaitHandle = handle;
                old.Dispose();

                return ewh;
            }
        }

        private static void ValidateHandle(SafeWaitHandle handle, string? name, out bool createdNew)
        {
            int errorCode = Marshal.GetLastWin32Error();

            if (handle.IsInvalid)
            {
                handle.SetHandleAsInvalid();

                if (!string.IsNullOrEmpty(name) && errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.WaitHandleCannotBeOpenedException_InvalidHandle, name));

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
            }

            createdNew = (errorCode != Interop.Errors.ERROR_ALREADY_EXISTS);
        }
    }
}
