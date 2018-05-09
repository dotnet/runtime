// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
    public sealed partial class Semaphore : WaitHandle
    {
        private const uint AccessRights =
            (uint)Win32Native.MAXIMUM_ALLOWED | Win32Native.SYNCHRONIZE | Win32Native.SEMAPHORE_MODIFY_STATE;

        public Semaphore(int initialCount, int maximumCount) : this(initialCount, maximumCount, null) { }

        public Semaphore(int initialCount, int maximumCount, string name)
        {
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

            SafeWaitHandle myHandle = CreateSemaphore(initialCount, maximumCount, name);

            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (null != name && 0 != name.Length && Interop.Errors.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(
                        SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
            this.SafeWaitHandle = myHandle;
        }

        public Semaphore(int initialCount, int maximumCount, string name, out bool createdNew)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCount), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCount), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (initialCount > maximumCount)
            {
                throw new ArgumentException(SR.Argument_SemaphoreInitialMaximum);
            }

            SafeWaitHandle myHandle = CreateSemaphore(initialCount, maximumCount, name);

            int errorCode = Marshal.GetLastWin32Error();
            if (myHandle.IsInvalid)
            {
                if (null != name && 0 != name.Length && Interop.Errors.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(
                        SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
            createdNew = errorCode != Interop.Errors.ERROR_ALREADY_EXISTS;
            this.SafeWaitHandle = myHandle;
        }

        private Semaphore(SafeWaitHandle handle)
        {
            this.SafeWaitHandle = handle;
        }

        private static SafeWaitHandle CreateSemaphore(int initialCount, int maximumCount, string name)
        {
            if (name != null)
            {
#if PLATFORM_UNIX
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
#else
                if (name.Length > Interop.Kernel32.MAX_PATH)
                    throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, name, Interop.Kernel32.MAX_PATH), nameof(name));
#endif
            }

            Debug.Assert(initialCount >= 0);
            Debug.Assert(maximumCount >= 1);
            Debug.Assert(initialCount <= maximumCount);

            return Win32Native.CreateSemaphoreEx(null, initialCount, maximumCount, name, 0, AccessRights);
        }

        public static Semaphore OpenExisting(string name)
        {
            Semaphore result;
            switch (OpenExistingWorker(name, out result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();
                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                case OpenExistingResult.PathNotFound:
                    throw new IOException(Interop.Kernel32.GetMessage(Interop.Errors.ERROR_PATH_NOT_FOUND));
                default:
                    return result;
            }
        }

        public static bool TryOpenExisting(string name, out Semaphore result)
        {
            return OpenExistingWorker(name, out result) == OpenExistingResult.Success;
        }

        private static OpenExistingResult OpenExistingWorker(string name, out Semaphore result)
        {
#if PLATFORM_UNIX
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
#else
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));
            if (name.Length > Interop.Kernel32.MAX_PATH)
                throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, name, Interop.Kernel32.MAX_PATH), nameof(name));

            //Pass false to OpenSemaphore to prevent inheritedHandles
            SafeWaitHandle myHandle = Win32Native.OpenSemaphore(AccessRights, false, name);

            if (myHandle.IsInvalid)
            {
                result = null;

                int errorCode = Marshal.GetLastWin32Error();

                if (Interop.Errors.ERROR_FILE_NOT_FOUND == errorCode || Interop.Errors.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Interop.Errors.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && 0 != name.Length && Interop.Errors.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;
                //this is for passed through NativeMethods Errors
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            result = new Semaphore(myHandle);
            return OpenExistingResult.Success;
#endif   
        }

        public int Release()
        {
            return Release(1);
        }

        // increase the count on a semaphore, returns previous count
        public int Release(int releaseCount)
        {
            if (releaseCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(releaseCount), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            //If ReleaseSempahore returns false when the specified value would cause
            //   the semaphore's count to exceed the maximum count set when Semaphore was created
            //Non-Zero return 

            int previousCount;
            if (!Win32Native.ReleaseSemaphore(SafeWaitHandle, releaseCount, out previousCount))
            {
                throw new SemaphoreFullException();
            }

            return previousCount;
        }
    }
}
