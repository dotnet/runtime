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
    public static class SemaphoreAcl
    {
        /// <summary>Gets or creates an <see cref="Semaphore" /> instance, allowing a <see cref="SemaphoreSecurity " /> instance to be optionally specified to set it during the event creation.</summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be satisfied concurrently.</param>
        /// <param name="maximumCount">The maximum number of requests for the semaphore that can be satisfied concurrently.</param>
        /// <param name="name">Optional argument to create a system semaphore. Set to <see langword="null" /> or <see cref="string.Empty" /> to create a local semaphore.</param>
        /// <param name="createdNew">When this method returns, this argument is always set to <see langword="true" /> if a local semaphore is created; that is, when <paramref name="name" /> is <see langword="null" /> or <see cref="string.Empty" />. If <paramref name="name" /> has a valid, non-empty value, this argument is set to <see langword="true" /> when the system semaphore is created, or it is set to <see langword="false" /> if an existing system semaphore is found with that name. This parameter is passed uninitialized.</param>
        /// <param name="semaphoreSecurity">The optional semaphore access control security to apply.</param>
        /// <returns>An object that represents a system semaphore, if named, or a local semaphore, if nameless.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount" /> is a negative number.
        /// -or-
        /// <paramref name="maximumCount" /> is not a positive number.</exception>
        /// <exception cref="ArgumentException"><paramref name="initialCount" /> is greater than <paramref name="maximumCount" />.</exception>
        /// <exception cref="WaitHandleCannotBeOpenedException">A semaphore handle with the system-wide name '<paramref name="name" />' cannot be created. A semaphore handle of a different type might have the same name.</exception>
        public static unsafe Semaphore Create(int initialCount, int maximumCount, string? name, out bool createdNew, SemaphoreSecurity? semaphoreSecurity)
        {
            if (semaphoreSecurity == null)
            {
                return new Semaphore(initialCount, maximumCount, name, out createdNew);
            }

            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCount), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCount), SR.ArgumentOutOfRange_NeedPosNum);
            }

            if (initialCount > maximumCount)
            {
                throw new ArgumentException(SR.Argument_SemaphoreInitialMaximum);
            }

            fixed (byte* pSecurityDescriptor = semaphoreSecurity.GetSecurityDescriptorBinaryForm())
            {
                var secAttrs = new Interop.Kernel32.SECURITY_ATTRIBUTES
                {
                    nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES),
                    lpSecurityDescriptor = (IntPtr)pSecurityDescriptor
                };

                SafeWaitHandle handle = Interop.Kernel32.CreateSemaphoreEx(
                    (IntPtr)(&secAttrs),
                    initialCount,
                    maximumCount,
                    name,
                    0, // This parameter is reserved and must be 0.
                    (uint)SemaphoreRights.FullControl // Equivalent to SEMAPHORE_ALL_ACCESS
                );

                int errorCode = Marshal.GetLastWin32Error();

                if (handle.IsInvalid)
                {
                    handle.Dispose();

                    if (!string.IsNullOrEmpty(name) && errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    {
                        throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                    }

                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                createdNew = (errorCode != Interop.Errors.ERROR_ALREADY_EXISTS);

                return CreateAndReplaceHandle(handle);
            }
        }

        /// <summary>
        /// Opens a specified named semaphore, if it already exists, applying the desired access rights.
        /// </summary>
        /// <param name="name">The name of the semaphore to be opened. If it's prefixed by "Global", it refers to a machine-wide semaphore. If it's prefixed by "Local", or doesn't have a prefix, it refers to a session-wide semaphore. Both prefix and name are case-sensitive.</param>
        /// <param name="rights">The desired access rights to apply to the returned semaphore.</param>
        /// <returns>An existing named semaphore.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string.</exception>
        /// <exception cref="WaitHandleCannotBeOpenedException">The named semaphore does not exist or is invalid.</exception>
        /// <exception cref="IOException">The path was not found.
        /// -or-
        /// A Win32 error occurred.</exception>
        /// <exception cref="UnauthorizedAccessException">The named semaphore exists, but the user does not have the security access required to use it.</exception>
        public static Semaphore OpenExisting(string name, SemaphoreRights rights)
        {
            switch (OpenExistingWorker(name, rights, out Semaphore? result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();

                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                case OpenExistingResult.PathNotFound:
                    throw new IOException(SR.Format(SR.IO_PathNotFound_Path, name));

                case OpenExistingResult.Success:
                default:
                    Debug.Assert(result != null, "result should be non-null on success");
                    return result;
            }
        }

        /// <summary>
        /// Tries to open a specified named semaphore, if it already exists, applying the desired access rights, and returns a value that indicates whether the operation succeeded.
        /// </summary>
        /// <param name="name">The name of the semaphore to be opened. If it's prefixed by "Global", it refers to a machine-wide semaphore. If it's prefixed by "Local", or doesn't have a prefix, it refers to a session-wide semaphore. Both prefix and name are case-sensitive.</param>
        /// <param name="rights">The desired access rights to apply to the returned semaphore.</param>
        /// <param name="result">When this method returns <see langword="true" />, contains an object that represents the named semaphore if the call succeeded, or <see langword="null" /> otherwise. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true" /> if the named semaphore was opened successfully; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string.</exception>
        /// <exception cref="IOException">A Win32 error occurred.</exception>
        /// <exception cref="UnauthorizedAccessException">The named semaphore exists, but the user does not have the security access required to use it.</exception>
        public static bool TryOpenExisting(string name, SemaphoreRights rights, [NotNullWhen(returnValue: true)] out Semaphore? result) =>
            OpenExistingWorker(name, rights, out result) == OpenExistingResult.Success;

        private static OpenExistingResult OpenExistingWorker(string name, SemaphoreRights rights, out Semaphore? result)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));
            }

            result = null;
            SafeWaitHandle handle = Interop.Kernel32.OpenSemaphore((uint)rights, false, name);

            int errorCode = Marshal.GetLastWin32Error();
            if (handle.IsInvalid)
            {
                handle.Dispose();
                return errorCode switch
                {
                    Interop.Errors.ERROR_FILE_NOT_FOUND or Interop.Errors.ERROR_INVALID_NAME => OpenExistingResult.NameNotFound,
                    Interop.Errors.ERROR_PATH_NOT_FOUND => OpenExistingResult.PathNotFound,
                    Interop.Errors.ERROR_INVALID_HANDLE => OpenExistingResult.NameInvalid,
                    _ => throw Win32Marshal.GetExceptionForLastWin32Error()
                };
            }

            result = CreateAndReplaceHandle(handle);
            return OpenExistingResult.Success;
        }

        private static Semaphore CreateAndReplaceHandle(SafeWaitHandle replacementHandle)
        {
            // The values of initialCount and maximumCount should not matter since we are replacing the
            // handle with one from an existing Semaphore, and disposing the old one
            // We should only make sure that they are valid values
            Semaphore semaphore = new Semaphore(1, 2);

            SafeWaitHandle old = semaphore.SafeWaitHandle;
            semaphore.SafeWaitHandle = replacementHandle;
            old.Dispose();

            return semaphore;
        }
    }
}
