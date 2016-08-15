// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
    public sealed partial class Semaphore : WaitHandle
    {
        [SecuritySafeCritical]
        public Semaphore(int initialCount, int maximumCount) : this(initialCount, maximumCount, null) { }

        [SecurityCritical]
        public Semaphore(int initialCount, int maximumCount, string name)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException("maximumCount", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }

            if (initialCount > maximumCount)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_SemaphoreInitialMaximum"));
            }

            SafeWaitHandle myHandle = CreateSemaphone(initialCount, maximumCount, name);

            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(
                        Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));

                __Error.WinIOError();
            }
            this.SafeWaitHandle = myHandle;
        }

        [SecurityCritical]
        public Semaphore(int initialCount, int maximumCount, string name, out bool createdNew)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException("maximumCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (initialCount > maximumCount)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_SemaphoreInitialMaximum"));
            }

            SafeWaitHandle myHandle = CreateSemaphone(initialCount, maximumCount, name);

            int errorCode = Marshal.GetLastWin32Error();
            if (myHandle.IsInvalid)
            {
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(
                        Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));
                __Error.WinIOError();
            }
            createdNew = errorCode != Win32Native.ERROR_ALREADY_EXISTS;
            this.SafeWaitHandle = myHandle;
        }

        [SecurityCritical]
        private Semaphore(SafeWaitHandle handle)
        {
            this.SafeWaitHandle = handle;
        }

        [SecurityCritical]
        private static SafeWaitHandle CreateSemaphone(int initialCount, int maximumCount, string name)
        {
            if (name != null)
            {
#if PLATFORM_UNIX
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_NamedSynchronizationPrimitives"));
#else
                if (name.Length > Path.MaxPath)
                    throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", Path.MaxPath), "name");
#endif
            }

            Contract.Assert(initialCount >= 0);
            Contract.Assert(maximumCount >= 1);
            Contract.Assert(initialCount <= maximumCount);

            return Win32Native.CreateSemaphore(null, initialCount, maximumCount, name);
        }

        [SecurityCritical]

        public static Semaphore OpenExisting(string name)
        {
            Semaphore result;
            switch (OpenExistingWorker(name, out result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();
                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));
                case OpenExistingResult.PathNotFound:
                    throw new IOException(Win32Native.GetMessage(Win32Native.ERROR_PATH_NOT_FOUND));
                default:
                    return result;
            }
        }

        [SecurityCritical]
        public static bool TryOpenExisting(string name, out Semaphore result)
        {
            return OpenExistingWorker(name, out result) == OpenExistingResult.Success;
        }

        [SecurityCritical]
        private static OpenExistingResult OpenExistingWorker(string name, out Semaphore result)
        {
#if PLATFORM_UNIX
            throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_NamedSynchronizationPrimitives"));
#else
            if (name == null)
                throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_WithParamName"));
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (name.Length > Path.MaxPath)
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", Path.MaxPath), "name");

            const int SYNCHRONIZE = 0x00100000;
            const int SEMAPHORE_MODIFY_STATE = 0x00000002;

            //Pass false to OpenSemaphore to prevent inheritedHandles
            SafeWaitHandle myHandle = Win32Native.OpenSemaphore(SEMAPHORE_MODIFY_STATE | SYNCHRONIZE, false, name);

            if (myHandle.IsInvalid)
            {
                result = null;

                int errorCode = Marshal.GetLastWin32Error();

                if (Win32Native.ERROR_FILE_NOT_FOUND == errorCode || Win32Native.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Win32Native.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;
                //this is for passed through NativeMethods Errors
                __Error.WinIOError();
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
        [SecuritySafeCritical]
        public int Release(int releaseCount)
        {
            if (releaseCount < 1)
            {
                throw new ArgumentOutOfRangeException("releaseCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
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
