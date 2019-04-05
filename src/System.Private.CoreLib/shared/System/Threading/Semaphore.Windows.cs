// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class Semaphore
    {
        private const uint AccessRights = (uint)Interop.Kernel32.MAXIMUM_ALLOWED | Interop.Kernel32.SYNCHRONIZE | Interop.Kernel32.SEMAPHORE_MODIFY_STATE;

        private Semaphore(SafeWaitHandle handle)
        {
            SafeWaitHandle = handle;
        }

        private void CreateSemaphoreCore(int initialCount, int maximumCount, string? name, out bool createdNew)
        {
            Debug.Assert(initialCount >= 0);
            Debug.Assert(maximumCount >= 1);
            Debug.Assert(initialCount <= maximumCount);

#if !PLATFORM_WINDOWS
            if (name != null)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
#endif
            SafeWaitHandle myHandle = Interop.Kernel32.CreateSemaphoreEx(IntPtr.Zero, initialCount, maximumCount, name, 0, AccessRights);

            int errorCode = Marshal.GetLastWin32Error();
            if (myHandle.IsInvalid)
            {
                if (name != null && name.Length != 0 && errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    throw new WaitHandleCannotBeOpenedException(
                        SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
            this.SafeWaitHandle = myHandle;
        }

        private static OpenExistingResult OpenExistingWorker(string name, out Semaphore? result)
        {
#if PLATFORM_WINDOWS
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));

            // Pass false to OpenSemaphore to prevent inheritedHandles
            SafeWaitHandle myHandle = Interop.Kernel32.OpenSemaphore(AccessRights, false, name);

            if (myHandle.IsInvalid)
            {
                result = null;
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND || errorCode == Interop.Errors.ERROR_INVALID_NAME)
                    return OpenExistingResult.NameNotFound;
                if (errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND)
                    return OpenExistingResult.PathNotFound;
                if (name != null && name.Length != 0 && errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                    return OpenExistingResult.NameInvalid;
                // this is for passed through NativeMethods Errors
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            result = new Semaphore(myHandle);
            return OpenExistingResult.Success;
#else
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
#endif
        }

        private int ReleaseCore(int releaseCount)
        {
            int previousCount;
            if (!Interop.Kernel32.ReleaseSemaphore(SafeWaitHandle!, releaseCount, out previousCount))
                throw new SemaphoreFullException();

            return previousCount;
        }
    }
}
