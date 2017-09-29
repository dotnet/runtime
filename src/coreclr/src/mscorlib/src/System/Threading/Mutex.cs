// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
/*=============================================================================
**
**
**
** Purpose: synchronization primitive that can also be used for interprocess synchronization
**
**
=============================================================================*/

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
    using System.Security;
    using System.Diagnostics;

    public sealed class Mutex : WaitHandle
    {
        private const uint AccessRights =
            (uint)Win32Native.MAXIMUM_ALLOWED | Win32Native.SYNCHRONIZE | Win32Native.MUTEX_MODIFY_STATE;

        private static bool dummyBool;

        internal class MutexSecurity
        {
        }

        public Mutex(bool initiallyOwned, String name, out bool createdNew)
            : this(initiallyOwned, name, out createdNew, (MutexSecurity)null)
        {
        }

        internal unsafe Mutex(bool initiallyOwned, String name, out bool createdNew, MutexSecurity mutexSecurity)
        {
            if (name == string.Empty)
            {
                // Empty name is treated as an unnamed mutex. Set to null, and we will check for null from now on.
                name = null;
            }
#if PLATFORM_WINDOWS
            if (name != null && System.IO.Path.MaxPath < name.Length)
            {
                throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, Path.MaxPath), nameof(name));
            }
#endif // PLATFORM_WINDOWS
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;

            CreateMutexWithGuaranteedCleanup(initiallyOwned, name, out createdNew, secAttrs);
        }

        internal void CreateMutexWithGuaranteedCleanup(bool initiallyOwned, String name, out bool createdNew, Win32Native.SECURITY_ATTRIBUTES secAttrs)
        {
            RuntimeHelpers.CleanupCode cleanupCode = new RuntimeHelpers.CleanupCode(MutexCleanupCode);
            MutexCleanupInfo cleanupInfo = new MutexCleanupInfo(null, false);
            MutexTryCodeHelper tryCodeHelper = new MutexTryCodeHelper(initiallyOwned, cleanupInfo, name, secAttrs, this);
            RuntimeHelpers.TryCode tryCode = new RuntimeHelpers.TryCode(tryCodeHelper.MutexTryCode);
            RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(
                tryCode,
                cleanupCode,
                cleanupInfo);
            createdNew = tryCodeHelper.m_newMutex;
        }

        internal class MutexTryCodeHelper
        {
            private bool m_initiallyOwned;
            private MutexCleanupInfo m_cleanupInfo;
            internal bool m_newMutex;
            private String m_name;
            private Win32Native.SECURITY_ATTRIBUTES m_secAttrs;
            private Mutex m_mutex;

            internal MutexTryCodeHelper(bool initiallyOwned, MutexCleanupInfo cleanupInfo, String name, Win32Native.SECURITY_ATTRIBUTES secAttrs, Mutex mutex)
            {
                Debug.Assert(name == null || name.Length != 0);

                m_initiallyOwned = initiallyOwned;
                m_cleanupInfo = cleanupInfo;
                m_name = name;
                m_secAttrs = secAttrs;
                m_mutex = mutex;
            }

            internal void MutexTryCode(object userData)
            {
                // try block                
                if (m_initiallyOwned)
                {
                    m_cleanupInfo.inCriticalRegion = true;
                }

                uint mutexFlags = m_initiallyOwned ? Win32Native.CREATE_MUTEX_INITIAL_OWNER : 0;
                SafeWaitHandle mutexHandle = Win32Native.CreateMutexEx(m_secAttrs, m_name, mutexFlags, AccessRights);

                int errorCode = Marshal.GetLastWin32Error();
                if (mutexHandle.IsInvalid)
                {
                    mutexHandle.SetHandleAsInvalid();
                    if (m_name != null)
                    {
                        switch (errorCode)
                        {
#if PLATFORM_UNIX
                            case Win32Native.ERROR_FILENAME_EXCED_RANGE:
                                // On Unix, length validation is done by CoreCLR's PAL after converting to utf-8
                                throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, Interop.Sys.MaxName), "name");
#endif

                            case Win32Native.ERROR_INVALID_HANDLE:
                                throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, m_name));
                        }
                    }
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, m_name);
                }
                m_newMutex = errorCode != Win32Native.ERROR_ALREADY_EXISTS;
                m_mutex.SetHandleInternal(mutexHandle);

                m_mutex.hasThreadAffinity = true;
            }
        }

        private void MutexCleanupCode(Object userData, bool exceptionThrown)
        {
            MutexCleanupInfo cleanupInfo = (MutexCleanupInfo)userData;

            // If hasThreadAffinity isn't true, we've thrown an exception in the above try, and we must free the mutex 
            // on this OS thread before ending our thread affninity.
            if (!hasThreadAffinity)
            {
                if (cleanupInfo.mutexHandle != null && !cleanupInfo.mutexHandle.IsInvalid)
                {
                    if (cleanupInfo.inCriticalRegion)
                    {
                        Win32Native.ReleaseMutex(cleanupInfo.mutexHandle);
                    }
                    cleanupInfo.mutexHandle.Dispose();
                }
            }
        }

        internal class MutexCleanupInfo
        {
            internal SafeWaitHandle mutexHandle;
            internal bool inCriticalRegion;
            internal MutexCleanupInfo(SafeWaitHandle mutexHandle, bool inCriticalRegion)
            {
                this.mutexHandle = mutexHandle;
                this.inCriticalRegion = inCriticalRegion;
            }
        }

        public Mutex(bool initiallyOwned, String name) : this(initiallyOwned, name, out dummyBool)
        {
        }

        public Mutex(bool initiallyOwned) : this(initiallyOwned, null, out dummyBool)
        {
        }

        public Mutex() : this(false, null, out dummyBool)
        {
        }

        private Mutex(SafeWaitHandle handle)
        {
            SetHandleInternal(handle);
            hasThreadAffinity = true;
        }

        public static Mutex OpenExisting(string name)
        {
            return OpenExisting(name, (MutexRights)0);
        }

        internal enum MutexRights
        {
        }

        internal static Mutex OpenExisting(string name, MutexRights rights)
        {
            Mutex result;
            switch (OpenExistingWorker(name, rights, out result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();

                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));

                case OpenExistingResult.PathNotFound:
                    throw Win32Marshal.GetExceptionForWin32Error(Win32Native.ERROR_PATH_NOT_FOUND, name);

                default:
                    return result;
            }
        }

        public static bool TryOpenExisting(string name, out Mutex result)
        {
            return OpenExistingWorker(name, (MutexRights)0, out result) == OpenExistingResult.Success;
        }

        private static OpenExistingResult OpenExistingWorker(string name, MutexRights rights, out Mutex result)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), SR.ArgumentNull_WithParamName);
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));
            }
#if !PLATFORM_UNIX
            if (System.IO.Path.MaxPath < name.Length)
            {
                throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, Path.MaxPath), nameof(name));
            }
#endif

            result = null;

            // To allow users to view & edit the ACL's, call OpenMutex
            // with parameters to allow us to view & edit the ACL.  This will
            // fail if we don't have permission to view or edit the ACL's.  
            // If that happens, ask for less permissions.
            SafeWaitHandle myHandle = Win32Native.OpenMutex(AccessRights, false, name);

            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

#if PLATFORM_UNIX
                if (name != null && errorCode == Win32Native.ERROR_FILENAME_EXCED_RANGE)
                {
                    // On Unix, length validation is done by CoreCLR's PAL after converting to utf-8
                    throw new ArgumentException(SR.Format(SR.Argument_WaitHandleNameTooLong, Interop.Sys.MaxName), nameof(name));
                }
#endif

                if (Win32Native.ERROR_FILE_NOT_FOUND == errorCode || Win32Native.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Win32Native.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;

                // this is for passed through Win32Native Errors
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
            }

            result = new Mutex(myHandle);
            return OpenExistingResult.Success;
        }

        // Note: To call ReleaseMutex, you must have an ACL granting you
        // MUTEX_MODIFY_STATE rights (0x0001).  The other interesting value
        // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
        public void ReleaseMutex()
        {
            if (Win32Native.ReleaseMutex(safeWaitHandle))
            {
            }
            else
            {
                throw new ApplicationException(SR.Arg_SynchronizationLockException);
            }
        }
    }
}
