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
    using System.Security.Permissions;
    using System.IO;
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    [ComVisible(true)]
    public sealed class Mutex : WaitHandle
    {
        static bool dummyBool;

        public class MutexSecurity
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public Mutex(bool initiallyOwned, String name, out bool createdNew)
            : this(initiallyOwned, name, out createdNew, (MutexSecurity)null)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public unsafe Mutex(bool initiallyOwned, String name, out bool createdNew, MutexSecurity mutexSecurity)
        {
            if (name == string.Empty)
            {
                // Empty name is treated as an unnamed mutex. Set to null, and we will check for null from now on.
                name = null;
            }
#if !PLATFORM_UNIX
            if (name != null && System.IO.Path.MaxPath < name.Length)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", Path.MaxPath), nameof(name));
            }
#endif
            Contract.EndContractBlock();
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;

            CreateMutexWithGuaranteedCleanup(initiallyOwned, name, out createdNew, secAttrs);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal Mutex(bool initiallyOwned, String name, out bool createdNew, Win32Native.SECURITY_ATTRIBUTES secAttrs) 
        {
            if (name == string.Empty)
            {
                // Empty name is treated as an unnamed mutex. Set to null, and we will check for null from now on.
                name = null;
            }
#if !PLATFORM_UNIX
            if (name != null && System.IO.Path.MaxPath < name.Length)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", Path.MaxPath), nameof(name));
            }
#endif
            Contract.EndContractBlock();

            CreateMutexWithGuaranteedCleanup(initiallyOwned, name, out createdNew, secAttrs);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
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
            bool m_initiallyOwned;
            MutexCleanupInfo m_cleanupInfo;
            internal bool m_newMutex;
            String m_name;
            Win32Native.SECURITY_ATTRIBUTES m_secAttrs;
            Mutex m_mutex;

            [PrePrepareMethod]
            internal MutexTryCodeHelper(bool initiallyOwned,MutexCleanupInfo cleanupInfo, String name, Win32Native.SECURITY_ATTRIBUTES secAttrs, Mutex mutex)
            {
                Debug.Assert(name == null || name.Length != 0);

                m_initiallyOwned = initiallyOwned;
                m_cleanupInfo = cleanupInfo;
                m_name = name;
                m_secAttrs = secAttrs;
                m_mutex = mutex;
            }

            [PrePrepareMethod]
            internal void MutexTryCode(object userData)
            {  
                SafeWaitHandle mutexHandle = null;
                // try block                
                RuntimeHelpers.PrepareConstrainedRegions();
                try 
                {
                }
                finally 
                {
                    if (m_initiallyOwned) 
                    {
                        m_cleanupInfo.inCriticalRegion = true;
                    }
                }

                int errorCode = 0;
                RuntimeHelpers.PrepareConstrainedRegions();
                try 
                {
                }
                finally 
                {
                    errorCode = CreateMutexHandle(m_initiallyOwned, m_name, m_secAttrs, out mutexHandle);
                }                    

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
                                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", Interop.Sys.MaxName), "name");
#endif

                            case Win32Native.ERROR_INVALID_HANDLE:
                                throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", m_name));
                        }
                    }
                    __Error.WinIOError(errorCode, m_name);
                }
                m_newMutex = errorCode != Win32Native.ERROR_ALREADY_EXISTS;
                m_mutex.SetHandleInternal(mutexHandle);

                m_mutex.hasThreadAffinity = true;

            }
        }

        [PrePrepareMethod]
        private void MutexCleanupCode(Object userData, bool exceptionThrown)
        {
            MutexCleanupInfo cleanupInfo = (MutexCleanupInfo) userData;
            
            // If hasThreadAffinity isn't true, we've thrown an exception in the above try, and we must free the mutex 
            // on this OS thread before ending our thread affninity.
            if(!hasThreadAffinity) {
                if (cleanupInfo.mutexHandle != null && !cleanupInfo.mutexHandle.IsInvalid) {
                    if( cleanupInfo.inCriticalRegion) {
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

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public Mutex(bool initiallyOwned, String name) : this(initiallyOwned, name, out dummyBool) {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public Mutex(bool initiallyOwned) : this(initiallyOwned, null, out dummyBool)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public Mutex() : this(false, null, out dummyBool)
        {
        }
        
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private Mutex(SafeWaitHandle handle)
        {
            SetHandleInternal(handle);
            hasThreadAffinity = true;
        }

        public static Mutex OpenExisting(string name)
        {
            return OpenExisting(name, (MutexRights) 0);
        }

        public enum MutexRights
        {
        }

        public static Mutex OpenExisting(string name, MutexRights rights)
        {
            Mutex result;
            switch (OpenExistingWorker(name, rights, out result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();

                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));

                case OpenExistingResult.PathNotFound:
                    __Error.WinIOError(Win32Native.ERROR_PATH_NOT_FOUND, name);
                    return result; //never executes

                default:
                    return result;
            }
        }

        public static bool TryOpenExisting(string name, out Mutex result)
        {
            return OpenExistingWorker(name, (MutexRights)0, out result) == OpenExistingResult.Success;
        }

        public static bool TryOpenExisting(string name, MutexRights rights, out Mutex result)
        {
            return OpenExistingWorker(name, rights, out result) == OpenExistingResult.Success;
        }

        private static OpenExistingResult OpenExistingWorker(string name, MutexRights rights, out Mutex result)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), Environment.GetResourceString("ArgumentNull_WithParamName"));
            }

            if(name.Length  == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), nameof(name));
            }
#if !PLATFORM_UNIX
            if(System.IO.Path.MaxPath < name.Length)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", Path.MaxPath), nameof(name));
            }
#endif
            Contract.EndContractBlock();

            result = null;

            // To allow users to view & edit the ACL's, call OpenMutex
            // with parameters to allow us to view & edit the ACL.  This will
            // fail if we don't have permission to view or edit the ACL's.  
            // If that happens, ask for less permissions.
            SafeWaitHandle myHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);

            int errorCode = 0;
            if (myHandle.IsInvalid)
            {
                errorCode = Marshal.GetLastWin32Error();

#if PLATFORM_UNIX
                if (name != null && errorCode == Win32Native.ERROR_FILENAME_EXCED_RANGE)
                {
                    // On Unix, length validation is done by CoreCLR's PAL after converting to utf-8
                    throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", Interop.Sys.MaxName), nameof(name));
                }
#endif

                if(Win32Native.ERROR_FILE_NOT_FOUND == errorCode || Win32Native.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Win32Native.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;

                // this is for passed through Win32Native Errors
                __Error.WinIOError(errorCode,name);
            }

            result = new Mutex(myHandle);
            return OpenExistingResult.Success;
        }

        // Note: To call ReleaseMutex, you must have an ACL granting you
        // MUTEX_MODIFY_STATE rights (0x0001).  The other interesting value
        // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void ReleaseMutex()
        {
            if (Win32Native.ReleaseMutex(safeWaitHandle))
            {
            }
            else
            {
                throw new Exception(Environment.GetResourceString("Arg_SynchronizationLockException"));
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        static int CreateMutexHandle(bool initiallyOwned, String name, Win32Native.SECURITY_ATTRIBUTES securityAttribute, out SafeWaitHandle mutexHandle)
        {
            int errorCode;

            while (true)
            {
                mutexHandle = Win32Native.CreateMutex(securityAttribute, initiallyOwned, name);
                errorCode = Marshal.GetLastWin32Error();
                if (!mutexHandle.IsInvalid) break;

                if (errorCode != Win32Native.ERROR_ACCESS_DENIED) break;

                // If a mutex with the name already exists, OS will try to open it with FullAccess.
                // It might fail if we don't have enough access. In that case, we try to open the mutex will modify and synchronize access.
                RuntimeHelpers.PrepareConstrainedRegions();

                mutexHandle = Win32Native.OpenMutex(
                    Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE,
                    false,
                    name);

                errorCode = !mutexHandle.IsInvalid ? Win32Native.ERROR_ALREADY_EXISTS : Marshal.GetLastWin32Error();

                // There could be a race condition here, the other owner of the mutex can free the mutex,
                // We need to retry creation in that case.
                if (errorCode != Win32Native.ERROR_FILE_NOT_FOUND)
                {
                    if (errorCode == Win32Native.ERROR_SUCCESS) errorCode = Win32Native.ERROR_ALREADY_EXISTS;
                    break;
                }
            }
            return errorCode;
        }
    }
}
