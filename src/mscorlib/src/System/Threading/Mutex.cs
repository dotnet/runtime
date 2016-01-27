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
    using System.Security.Principal;
    using System.Security;
    using System.Diagnostics.Contracts;
    
#if FEATURE_MACL
    using System.Security.AccessControl;
#endif

    [HostProtection(Synchronization=true, ExternalThreading=true)]
    [ComVisible(true)]
    public sealed class Mutex : WaitHandle
    {
        static bool dummyBool;

#if !FEATURE_MACL
        public class MutexSecurity {
        }
#endif       

        [System.Security.SecurityCritical]  // auto-generated_required
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public Mutex(bool initiallyOwned, String name, out bool createdNew)
            : this(initiallyOwned, name, out createdNew, (MutexSecurity)null)
        {
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public unsafe Mutex(bool initiallyOwned, String name, out bool createdNew, MutexSecurity mutexSecurity)
        {
            if (name != null)
            {
#if PLATFORM_UNIX
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_NamedSynchronizationPrimitives"));
#else
                if (System.IO.Path.MaxPath < name.Length)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", name));
                }
#endif
            }
            Contract.EndContractBlock();
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
#if FEATURE_MACL
            // For ACL's, get the security descriptor from the MutexSecurity.
            if (mutexSecurity != null) {

                secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);

                byte[] sd = mutexSecurity.GetSecurityDescriptorBinaryForm();
                byte* pSecDescriptor = stackalloc byte[sd.Length];
                Buffer.Memcpy(pSecDescriptor, 0, sd, 0, sd.Length);
                secAttrs.pSecurityDescriptor = pSecDescriptor;
            }
#endif

            CreateMutexWithGuaranteedCleanup(initiallyOwned, name, out createdNew, secAttrs);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal Mutex(bool initiallyOwned, String name, out bool createdNew, Win32Native.SECURITY_ATTRIBUTES secAttrs) 
        {
            if (name != null)
            {
#if PLATFORM_UNIX
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_NamedSynchronizationPrimitives"));
#else
                if (System.IO.Path.MaxPath < name.Length)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", name));
                }
#endif
            }
            Contract.EndContractBlock();

            CreateMutexWithGuaranteedCleanup(initiallyOwned, name, out createdNew, secAttrs);
        }

#if FEATURE_LEGACYNETCF
        static string WinCEObjectNameQuirk(string name)
        {
            if (name == null)
                return null;

            // WinCE allowed backslashes in kernel object names, but WinNT does not allow them.
            // Replace all backslashes with a rare unicode character if we are in NetCF compat mode.
            // Mutex was the only named kernel object exposed to phone apps, so we do not have
            // to apply this quirk in other places.
            return name.Replace('\\', '\u2044');
        }
#endif

        [System.Security.SecurityCritical]  // auto-generated_required
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal void CreateMutexWithGuaranteedCleanup(bool initiallyOwned, String name, out bool createdNew, Win32Native.SECURITY_ATTRIBUTES secAttrs)
        {
#if FEATURE_LEGACYNETCF
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                name = WinCEObjectNameQuirk(name);
#endif

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
            [System.Security.SecurityCritical] // auto-generated
            Win32Native.SECURITY_ATTRIBUTES m_secAttrs;
            Mutex m_mutex;

            [System.Security.SecurityCritical]  // auto-generated
            [PrePrepareMethod]
            internal MutexTryCodeHelper(bool initiallyOwned,MutexCleanupInfo cleanupInfo, String name, Win32Native.SECURITY_ATTRIBUTES secAttrs, Mutex mutex)
            {
                m_initiallyOwned = initiallyOwned;
                m_cleanupInfo = cleanupInfo;
                m_name = name;
                m_secAttrs = secAttrs;
                m_mutex = mutex;
            }

            [System.Security.SecurityCritical]  // auto-generated
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
#if !FEATURE_CORECLR
                        Thread.BeginThreadAffinity();
                        Thread.BeginCriticalRegion();
#endif //!FEATURE_CORECLR
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
                    if(null != m_name && 0 != m_name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                        throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", m_name));
                    __Error.WinIOError(errorCode, m_name);
                }
                m_newMutex = errorCode != Win32Native.ERROR_ALREADY_EXISTS;
                m_mutex.SetHandleInternal(mutexHandle);

                m_mutex.hasThreadAffinity = true;

            }
        }

        [System.Security.SecurityCritical]  // auto-generated
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
                    
                if( cleanupInfo.inCriticalRegion) {
#if !FEATURE_CORECLR
                    Thread.EndCriticalRegion();
                    Thread.EndThreadAffinity();
#endif
                }                    
            }
        }

        internal class MutexCleanupInfo
        {
            [System.Security.SecurityCritical] // auto-generated
            internal SafeWaitHandle mutexHandle;
            internal bool inCriticalRegion;
            [System.Security.SecurityCritical]  // auto-generated
            internal MutexCleanupInfo(SafeWaitHandle mutexHandle, bool inCriticalRegion)
            {
                this.mutexHandle = mutexHandle;
                this.inCriticalRegion = inCriticalRegion;
            }
        }

        // For the .NET Compact Framework this constructor was security safe critical.
        // For Windows Phone version 8 (Apollo), all apps will run as fully trusted,
        // meaning the CLR is not considered a trust boundary.  This API could be marked security critical.
        // However for Windows Phone version 7.1 applications, they will still be run
        // as partially trusted applications, with our security transparency model enforced.
        // So we have this peculiar #ifdef that should be enabled only for .NET CF backwards
        // compatibility.
#if FEATURE_LEGACYNETCF
        [System.Security.SecuritySafeCritical]  // auto-generated_required
#else
        [System.Security.SecurityCritical]  // auto-generated_required
#endif //FEATURE_LEGACYNETCF
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public Mutex(bool initiallyOwned, String name) : this(initiallyOwned, name, out dummyBool) {
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public Mutex(bool initiallyOwned) : this(initiallyOwned, null, out dummyBool)
        {
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public Mutex() : this(false, null, out dummyBool)
        {
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private Mutex(SafeWaitHandle handle)
        {
            SetHandleInternal(handle);
            hasThreadAffinity = true;
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public static Mutex OpenExisting(string name)
        {
#if !FEATURE_MACL
            return OpenExisting(name, (MutexRights) 0);
#else // FEATURE_MACL
            return OpenExisting(name, MutexRights.Modify | MutexRights.Synchronize);
#endif // FEATURE_MACL
        }

#if !FEATURE_MACL
        public enum MutexRights
        {
        }
#endif

        [System.Security.SecurityCritical]  // auto-generated_required
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

        [System.Security.SecurityCritical]  // auto-generated_required
        public static bool TryOpenExisting(string name, out Mutex result)
        {
#if !FEATURE_MACL
            return OpenExistingWorker(name, (MutexRights)0, out result) == OpenExistingResult.Success;
#else // FEATURE_MACL
            return OpenExistingWorker(name, MutexRights.Modify | MutexRights.Synchronize, out result) == OpenExistingResult.Success;
#endif // FEATURE_MACL
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public static bool TryOpenExisting(string name, MutexRights rights, out Mutex result)
        {
            return OpenExistingWorker(name, rights, out result) == OpenExistingResult.Success;
        }

        [System.Security.SecurityCritical]
        private static OpenExistingResult OpenExistingWorker(string name, MutexRights rights, out Mutex result)
        {
#if PLATFORM_UNIX
            throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_NamedSynchronizationPrimitives"));
#else
            if (name == null)
            {
                throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_WithParamName"));
            }

            if(name.Length  == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            }
            if(System.IO.Path.MaxPath < name.Length)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong",name));
            }
            Contract.EndContractBlock();

            result = null;

#if FEATURE_LEGACYNETCF
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                name = WinCEObjectNameQuirk(name);
#endif

            // To allow users to view & edit the ACL's, call OpenMutex
            // with parameters to allow us to view & edit the ACL.  This will
            // fail if we don't have permission to view or edit the ACL's.  
            // If that happens, ask for less permissions.
#if FEATURE_MACL
            SafeWaitHandle myHandle = Win32Native.OpenMutex((int) rights, false, name);
#else
            SafeWaitHandle myHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);
#endif

            int errorCode = 0;
            if (myHandle.IsInvalid)
            {
                errorCode = Marshal.GetLastWin32Error();

                if(Win32Native.ERROR_FILE_NOT_FOUND == errorCode || Win32Native.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Win32Native.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode) 
                    return OpenExistingResult.NameInvalid;

                // this is for passed through Win32Native Errors
                __Error.WinIOError(errorCode,name);
            }

            result = new Mutex(myHandle);
            return OpenExistingResult.Success;
#endif
        }

        // Note: To call ReleaseMutex, you must have an ACL granting you
        // MUTEX_MODIFY_STATE rights (0x0001).  The other interesting value
        // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]        
        public void ReleaseMutex()
        {
            if (Win32Native.ReleaseMutex(safeWaitHandle))
            {
#if !FEATURE_CORECLR
                Thread.EndCriticalRegion();
                Thread.EndThreadAffinity();
#endif
            }
            else
            {
#if FEATURE_CORECLR
                throw new Exception(Environment.GetResourceString("Arg_SynchronizationLockException"));
#else
                throw new ApplicationException(Environment.GetResourceString("Arg_SynchronizationLockException"));
#endif // FEATURE_CORECLR
            }                                                               
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        static int CreateMutexHandle(bool initiallyOwned, String name, Win32Native.SECURITY_ATTRIBUTES securityAttribute, out SafeWaitHandle mutexHandle) {            
            int errorCode;  
            bool fAffinity = false;
            
            while(true) {
                mutexHandle = Win32Native.CreateMutex(securityAttribute, initiallyOwned, name);
                errorCode = Marshal.GetLastWin32Error();                                
                if( !mutexHandle.IsInvalid) {
                    break;                
                }

                if( errorCode == Win32Native.ERROR_ACCESS_DENIED) {
                    // If a mutex with the name already exists, OS will try to open it with FullAccess.
                    // It might fail if we don't have enough access. In that case, we try to open the mutex will modify and synchronize access.
                    //
                    
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try 
                    {
                        try 
                        {
                        } 
                        finally 
                        {
#if !FEATURE_CORECLR
                            Thread.BeginThreadAffinity();
#endif
                            fAffinity = true;
                        }
                        mutexHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);
                        if(!mutexHandle.IsInvalid)
                        {
                            errorCode = Win32Native.ERROR_ALREADY_EXISTS;
                        }
                        else
                        {
                            errorCode = Marshal.GetLastWin32Error();
                        }
                    }
                    finally 
                    {
                        if (fAffinity) {
#if !FEATURE_CORECLR
                            Thread.EndThreadAffinity();
#endif
                        }
                    }

                    // There could be a race condition here, the other owner of the mutex can free the mutex,
                    // We need to retry creation in that case.
                    if( errorCode != Win32Native.ERROR_FILE_NOT_FOUND) {
                        if( errorCode == Win32Native.ERROR_SUCCESS) {
                            errorCode =  Win32Native.ERROR_ALREADY_EXISTS;
                        }                        
                        break;
                    }
                }
                else {
                    break;
                }
            }                        
            return errorCode;
        }
        
#if FEATURE_MACL
        [System.Security.SecuritySafeCritical]  // auto-generated
        public MutexSecurity GetAccessControl()
        {
            return new MutexSecurity(safeWaitHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetAccessControl(MutexSecurity mutexSecurity)
        {
            if (mutexSecurity == null)
                throw new ArgumentNullException("mutexSecurity");
            Contract.EndContractBlock();

            mutexSecurity.Persist(safeWaitHandle);
        }
#endif

    }
}
