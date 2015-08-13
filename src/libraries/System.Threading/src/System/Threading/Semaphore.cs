// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
    public sealed partial class Semaphore : WaitHandle
    {
        // creates a nameless semaphore object
        // Win32 only takes maximum count of Int32.MaxValue
        [SecuritySafeCritical]
        public Semaphore(int initialCount, int maximumCount) : this(initialCount, maximumCount, null) { }

        [SecurityCritical]
        public Semaphore(int initialCount, int maximumCount, string name)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount", SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            }

            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException("maximumCount", SR.ArgumentOutOfRange_NeedPosNum);
            }

            if (initialCount > maximumCount)
            {
                throw new ArgumentException(SR.Argument_SemaphoreInitialMaximum);
            }

            ValidateNewName(name);

            SafeWaitHandle myHandle = CreateSemaphone(initialCount, maximumCount, name);

            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (null != name && 0 != name.Length && Interop.mincore.Errors.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.WaitHandleCannotBeOpenedException_InvalidHandle, name));

                WinIOError();
            }
            this.SafeWaitHandle = myHandle;
        }

        [SecurityCritical]
        public Semaphore(int initialCount, int maximumCount, string name, out bool createdNew)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount", SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            }

            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException("maximumCount", SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            }

            if (initialCount > maximumCount)
            {
                throw new ArgumentException(SR.Argument_SemaphoreInitialMaximum);
            }

            ValidateNewName(name);

            SafeWaitHandle myHandle;

            myHandle = CreateSemaphone(initialCount, maximumCount, name);

            int errorCode = Marshal.GetLastWin32Error();
            if (myHandle.IsInvalid)
            {
                if (null != name && 0 != name.Length && Interop.mincore.Errors.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.WaitHandleCannotBeOpenedException_InvalidHandle, name));
                WinIOError();
            }
            createdNew = errorCode != Interop.mincore.Errors.ERROR_ALREADY_EXISTS;
            this.SafeWaitHandle = myHandle;
        }

        [SecurityCritical]
        private Semaphore(SafeWaitHandle handle)
        {
            this.SafeWaitHandle = handle;
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
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.WaitHandleCannotBeOpenedException_InvalidHandle, name));
                case OpenExistingResult.PathNotFound:
                    throw new IOException(GetMessage(Interop.mincore.Errors.ERROR_PATH_NOT_FOUND));
                default:
                    return result;
            }
        }

        [SecurityCritical]
        public static bool TryOpenExisting(string name, out Semaphore result)
        {
            return OpenExistingWorker(name, out result) == OpenExistingResult.Success;
        }

        // This exists in WaitHandle, but is oddly ifdefed for some reason...
        private enum OpenExistingResult
        {
            Success,
            NameNotFound,
            PathNotFound,
            NameInvalid
        }

        [SecurityCritical]
        private static OpenExistingResult OpenExistingWorker(string name, out Semaphore result)
        {
            ValidateExistingName(name);
            
            result = null;

            SafeWaitHandle myHandle = OpenSemaphore(name);

            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (Interop.mincore.Errors.ERROR_FILE_NOT_FOUND == errorCode || Interop.mincore.Errors.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Interop.mincore.Errors.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && 0 != name.Length && Interop.mincore.Errors.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;
                //this is for passed through NativeMethods Errors
                WinIOError();
            }
            result = new Semaphore(myHandle);
            return OpenExistingResult.Success;
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
                throw new ArgumentOutOfRangeException("releaseCount", SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            }
            int previousCount;

            //If ReleaseSempahore returns false when the specified value would cause
            //   the semaphore's count to exceed the maximum count set when Semaphore was created
            //Non-Zero return 

            if (!ReleaseSemaphore(SafeWaitHandle, releaseCount, out previousCount))
            {
                throw new SemaphoreFullException();
            }

            return previousCount;
        }

        internal static void WinIOError()
        {
            int errorCode = Marshal.GetLastWin32Error();
            WinIOError(errorCode, String.Empty);
        }

        internal static void WinIOError(string str)
        {
            int errorCode = Marshal.GetLastWin32Error();
            WinIOError(errorCode, str);
        }

        // After calling GetLastWin32Error(), it clears the last error field,
        // so you must save the HResult and pass it to this method.  This method
        // will determine the appropriate exception to throw dependent on your 
        // error, and depending on the error, insert a string into the message 
        // gotten from the ResourceManager.
        internal static void WinIOError(int errorCode, String str)
        {
            throw new IOException(GetMessage(errorCode), MakeHRFromErrorCode(errorCode));
        }

        // Use this to translate error codes like the above into HRESULTs like
        // 0x80070006 for ERROR_INVALID_HANDLE
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            return unchecked(((int)0x80070000) | errorCode);
        }
    }
}
