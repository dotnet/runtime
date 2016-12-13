// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
/*============================================================
**
**
**
** Purpose: Defines the lock that implements 
**          single-writer/multiple-reader semantics
**
**
===========================================================*/

#if FEATURE_RWLOCK
namespace System.Threading {
    using System.Threading;
    using System.Security.Permissions;
    using System.Runtime.Remoting;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    [ComVisible(true)]
    public sealed class ReaderWriterLock: CriticalFinalizerObject
    {
        /*
         * Constructor
         */
        public ReaderWriterLock()
        {
            PrivateInitialize();
        }

        /*
         * Destructor
         */
        ~ReaderWriterLock()
        {
            PrivateDestruct();
        }

        /*
         * Property that returns TRUE if the reader lock is held
         * by the current thread
         */
        public bool IsReaderLockHeld {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get {
                return(PrivateGetIsReaderLockHeld());
                }
        }
         
        /*
         * Property that returns TRUE if the writer lock is held
         * by the current thread
         */
        public bool IsWriterLockHeld {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get {
                return(PrivateGetIsWriterLockHeld());
                }
        }
        
        /*
         * Property that returns the current writer sequence number. 
         * The caller should be a reader or writer for getting 
         * meaningful results
         */
        public int WriterSeqNum {
            get {
                return(PrivateGetWriterSeqNum());
                }
        }
        
        /*
         * Acquires reader lock. The thread will block if a different
         * thread has writer lock.
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void AcquireReaderLockInternal(int millisecondsTimeout);

        public void AcquireReaderLock(int millisecondsTimeout)
        {
            AcquireReaderLockInternal(millisecondsTimeout);
        }


        public void AcquireReaderLock(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long) Int32.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            AcquireReaderLockInternal((int)tm);
        }
        
        /*
         * Acquires writer lock. The thread will block if a different
         * thread has reader lock. It will dead lock if this thread
         * has reader lock. Use UpgardeToWriterLock when you are not
         * sure if the thread has reader lock
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void AcquireWriterLockInternal(int millisecondsTimeout);

        public void AcquireWriterLock(int millisecondsTimeout)
        {
            AcquireWriterLockInternal(millisecondsTimeout);
        }

        public void AcquireWriterLock(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long) Int32.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            AcquireWriterLockInternal((int)tm);
        }
        
        
        /*
         * Releases reader lock. 
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private extern void ReleaseReaderLockInternal();

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public void ReleaseReaderLock()
        {
            ReleaseReaderLockInternal();
        }
        
        /*
         * Releases writer lock. 
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private extern void ReleaseWriterLockInternal();

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public void ReleaseWriterLock()
        {
            ReleaseWriterLockInternal();
        }
        
        /*
         * Upgardes the thread to a writer. If the thread has is a
         * reader, it is possible that the reader lock was 
         * released before writer lock was acquired.
         */
        public LockCookie UpgradeToWriterLock(int millisecondsTimeout)
        {
            LockCookie result = new LockCookie ();
            FCallUpgradeToWriterLock (ref result, millisecondsTimeout);
            return result;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void FCallUpgradeToWriterLock(ref LockCookie result, int millisecondsTimeout);

        public LockCookie UpgradeToWriterLock(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long) Int32.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            return UpgradeToWriterLock((int)tm);
        }
        
        /*
         * Restores the lock status of the thread to the one it was
         * in when it called UpgradeToWriterLock. 
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void DowngradeFromWriterLockInternal(ref LockCookie lockCookie);

        public void DowngradeFromWriterLock(ref LockCookie lockCookie)
        {
            DowngradeFromWriterLockInternal(ref lockCookie);
        }
        
        /*
         * Releases the lock irrespective of the number of times the thread
         * acquired the lock
         */
        public LockCookie ReleaseLock()
        {
            LockCookie result = new LockCookie ();
            FCallReleaseLock (ref result);
            return result;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void FCallReleaseLock(ref LockCookie result);
        
        /*
         * Restores the lock status of the thread to the one it was
         * in when it called ReleaseLock. 
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void RestoreLockInternal(ref LockCookie lockCookie);

        public void RestoreLock(ref LockCookie lockCookie)
        {
            RestoreLockInternal(ref lockCookie);
        }
    
        /*
         * Internal helper that returns TRUE if the reader lock is held
         * by the current thread
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private extern bool PrivateGetIsReaderLockHeld();
        
        /*
         * Internal helper that returns TRUE if the writer lock is held
         * by the current thread
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private extern bool PrivateGetIsWriterLockHeld();
        
        /*
         * Internal helper that returns the current writer sequence 
         * number. The caller should be a reader or writer for getting 
         * meaningful results
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int PrivateGetWriterSeqNum();
        
        /*
         * Returns true if there were intermediate writes since the 
         * sequence number was obtained. The caller should be
         * a reader or writer for getting meaningful results
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern bool AnyWritersSince(int seqNum);
    
        // Initialize state kept inside the lock
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void PrivateInitialize();

        // Destruct resource associated with the lock
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void PrivateDestruct();

        // State
#pragma warning disable 169
#pragma warning disable 414  // These fields are not used from managed.
        private IntPtr _hWriterEvent;
        private IntPtr _hReaderEvent;
        private IntPtr _hObjectHandle;
        private int _dwState = 0;
        private int _dwULockID = 0;
        private int _dwLLockID = 0;
        private int _dwWriterID = 0;
        private int _dwWriterSeqNum = 0;
        private short _wWriterLevel;
#if RWLOCK_STATISTICS
        // WARNING: You must explicitly #define RWLOCK_STATISTICS when you
        // build in both the VM and BCL directories if you want this.
        private int _dwReaderEntryCount = 0;
        private int _dwReaderContentionCount = 0;
        private int _dwWriterEntryCount = 0;
        private int _dwWriterContentionCount = 0;
        private int _dwEventsReleasedCount = 0;
#endif // RWLOCK_STATISTICS
#pragma warning restore 414
#pragma warning restore 169
    }
}
#endif //FEATURE_RWLOCK
