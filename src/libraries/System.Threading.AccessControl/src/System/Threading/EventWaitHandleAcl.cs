// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

                int errorCode = Marshal.GetLastWin32Error();

                if (handle.IsInvalid)
                {
                    handle.SetHandleAsInvalid();

                    if (!string.IsNullOrEmpty(name) && errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    {
                        throw new WaitHandleCannotBeOpenedException(SR.Format(SR.WaitHandleCannotBeOpenedException_InvalidHandle, name));
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
                }

                createdNew = (errorCode != Interop.Errors.ERROR_ALREADY_EXISTS);

                return CreateAndReplaceHandle(handle);
            }
        }

        /// <summary>
        /// Opens a specified named event wait handle, if it already exists, applying the desired access rights.
        /// </summary>
        /// <param name="name">The name of the event wait handle to be opened. If it's prefixed by "Global", it refers to a machine-wide event wait handle. If it's prefixed by "Local", or doesn't have a prefix, it refers to a session-wide event wait handle. Both prefix and name are case-sensitive.</param>
        /// <param name="rights">The desired access rights to apply to the returned event wait handle.</param>
        /// <returns>An existing named event wait handle.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string.</exception>
        /// <exception cref="WaitHandleCannotBeOpenedException">The named event wait handle does not exist or is invalid.</exception>
        /// <exception cref="DirectoryNotFoundException">The path was not found.</exception>
        /// <exception cref="IOException">A Win32 error occurred.</exception>
        /// <exception cref="UnauthorizedAccessException">The named event wait handle exists, but the user does not have the security access required to use it.</exception>
        public static EventWaitHandle OpenExisting(string name, EventWaitHandleRights rights)
        {
            switch (OpenExistingWorker(name, rights, out EventWaitHandle? result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();

                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                case OpenExistingResult.PathNotFound:
                    throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, name));

                case OpenExistingResult.Success:
                default:
                    Debug.Assert(result != null, "result should be non-null on success");
                    return result;
            }
        }

        /// <summary>
        /// Tries to open a specified named event wait handle, if it already exists, applying the desired access rights, and returns a value that indicates whether the operation succeeded.
        /// </summary>
        /// <param name="name">The name of the event wait handle to be opened. If it's prefixed by "Global", it refers to a machine-wide event wait handle. If it's prefixed by "Local", or doesn't have a prefix, it refers to a session-wide event wait handle. Both prefix and name are case-sensitive.</param>
        /// <param name="rights">The desired access rights to apply to the returned event wait handle.</param>
        /// <param name="result">When this method returns <see langword="true" />, contains an object that represents the named event wait handle if the call succeeded, or <see langword="null" /> otherwise. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true" /> if the named event wait handle was opened successfully; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string.</exception>
        /// <exception cref="IOException">A Win32 error occurred.</exception>
        /// <exception cref="UnauthorizedAccessException">The named event wait handle exists, but the user does not have the security access required to use it.</exception>
        public static bool TryOpenExisting(string name, EventWaitHandleRights rights, [NotNullWhen(returnValue: true)] out EventWaitHandle? result) =>
            OpenExistingWorker(name, rights, out result) == OpenExistingResult.Success;

        private static OpenExistingResult OpenExistingWorker(string name, EventWaitHandleRights rights, out EventWaitHandle? result)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));
            }

            result = null;
            SafeWaitHandle existingHandle = Interop.Kernel32.OpenEvent((uint)rights, false, name);

            int errorCode = Marshal.GetLastWin32Error();
            if (existingHandle.IsInvalid)
            {
                existingHandle.Dispose();
                return errorCode switch
                {
                    Interop.Errors.ERROR_FILE_NOT_FOUND or Interop.Errors.ERROR_INVALID_NAME => OpenExistingResult.NameNotFound,
                    Interop.Errors.ERROR_PATH_NOT_FOUND => OpenExistingResult.PathNotFound,
                    Interop.Errors.ERROR_INVALID_HANDLE => OpenExistingResult.NameInvalid,
                    _ => throw Win32Marshal.GetExceptionForWin32Error(errorCode, name)
                };
            }

            result = CreateAndReplaceHandle(existingHandle);

            return OpenExistingResult.Success;
        }

        private static EventWaitHandle CreateAndReplaceHandle(SafeWaitHandle replacementHandle)
        {
            // The values of initialState and mode should not matter since we are replacing the
            // handle with one from an existing EventWaitHandle, and disposing the old one
            // We should only make sure that they are valid values
            EventWaitHandle eventWaitHandle = new EventWaitHandle(initialState: default, mode: default);

            SafeWaitHandle old = eventWaitHandle.SafeWaitHandle;
            eventWaitHandle.SafeWaitHandle = replacementHandle;
            old.Dispose();

            return eventWaitHandle;
        }
    }
}
