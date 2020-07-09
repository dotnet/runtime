// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

                ValidateHandle(handle, name, out createdNew);

                Semaphore semaphore = new Semaphore(initialCount, maximumCount);
                SafeWaitHandle old = semaphore.SafeWaitHandle;
                semaphore.SafeWaitHandle = handle;
                old.Dispose();

                return semaphore;
            }
        }

        private static void ValidateHandle(SafeWaitHandle handle, string? name, out bool createdNew)
        {
            int errorCode = Marshal.GetLastWin32Error();

            if (handle.IsInvalid)
            {
                if (!string.IsNullOrEmpty(name) && errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                {
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                }

                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
        }
    }
}
