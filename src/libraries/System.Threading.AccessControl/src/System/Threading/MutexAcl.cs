// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public static class MutexAcl
    {
        /// <summary>Gets or creates <see cref="Mutex" /> instance, allowing a <see cref="MutexSecurity" /> to be optionally specified to set it during the mutex creation.</summary>
        /// <param name="initiallyOwned"><see langword="true" /> to give the calling thread initial ownership of the named system mutex if the named system mutex is created as a result of this call; otherwise, <see langword="false" />.</param>
        /// <param name="name">The optional name of the system mutex. If this argument is set to <see langword="null" /> or <see cref="string.Empty" />, a local mutex is created.</param>
        /// <param name="createdNew">When this method returns, this argument is always set to <see langword="true" /> if a local mutex is created; that is, when <paramref name="name" /> is <see langword="null" /> or <see cref="string.Empty" />. If <paramref name="name" /> has a valid non-empty value, this argument is set to <see langword="true" /> when the system mutex is created, or it is set to <see langword="false" /> if an existing system mutex is found with that name. This parameter is passed uninitialized.</param>
        /// <param name="mutexSecurity">The optional mutex access control security to apply.</param>
        /// <returns>An object that represents a system mutex, if named, or a local mutex, if nameless.</returns>
        /// <exception cref="ArgumentException">.NET Framework only: The length of the name exceeds the maximum limit.</exception>
        /// <exception cref="WaitHandleCannotBeOpenedException">A mutex handle with system-wide <paramref name="name" /> cannot be created. A mutex handle of a different type might have the same name.</exception>
        public static unsafe Mutex Create(bool initiallyOwned, string? name, out bool createdNew, MutexSecurity? mutexSecurity)
        {
            if (mutexSecurity == null)
            {
                return new Mutex(initiallyOwned, name, out createdNew);
            }

            uint mutexFlags = initiallyOwned ? Interop.Kernel32.CREATE_MUTEX_INITIAL_OWNER : 0;

            fixed (byte* pSecurityDescriptor = mutexSecurity.GetSecurityDescriptorBinaryForm())
            {
                var secAttrs = new Interop.Kernel32.SECURITY_ATTRIBUTES
                {
                    nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES),
                    lpSecurityDescriptor = (IntPtr)pSecurityDescriptor
                };

                SafeWaitHandle handle = Interop.Kernel32.CreateMutexEx(
                    (IntPtr)(&secAttrs),
                    name,
                    mutexFlags,
                    (uint)MutexRights.FullControl // Equivalent to MUTEX_ALL_ACCESS
                );

                int errorCode = Marshal.GetLastWin32Error();

                if (handle.IsInvalid)
                {
                    handle.SetHandleAsInvalid();

                    if (errorCode == Interop.Errors.ERROR_FILENAME_EXCED_RANGE)
                    {
                        throw new ArgumentException(SR.Argument_WaitHandleNameTooLong, nameof(name));
                    }

                    if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    {
                        throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
                }

                createdNew = (errorCode != Interop.Errors.ERROR_ALREADY_EXISTS);

                return CreateAndReplaceHandle(handle);
            }
        }

        public static Mutex OpenExisting(string name, MutexRights rights)
        {
            switch (OpenExistingWorker(name, rights, out Mutex? result))
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

        public static bool TryOpenExisting(string name, MutexRights rights, out Mutex result) =>
            OpenExistingWorker(name, rights, out result!) == OpenExistingResult.Success;

        private static OpenExistingResult OpenExistingWorker(string name, MutexRights rights, out Mutex? result)
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
            SafeWaitHandle existingHandle = Interop.Kernel32.OpenMutex((uint)rights, false, name);

            int errorCode = Marshal.GetLastWin32Error();
            if (existingHandle.IsInvalid)
            {
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

        private static Mutex CreateAndReplaceHandle(SafeWaitHandle replacementHandle)
        {
            // The value of initiallyOwned should not matter since we are replacing the
            // handle with one from an existing Mutex, and disposing the old one
            // We should only make sure that it is a valid value
            Mutex mutex = new Mutex(initiallyOwned: default);

            SafeWaitHandle old = mutex.SafeWaitHandle;
            mutex.SafeWaitHandle = replacementHandle;
            old.Dispose();

            return mutex;
        }
    }
}
