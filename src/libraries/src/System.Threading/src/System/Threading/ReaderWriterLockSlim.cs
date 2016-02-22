// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics; // for TraceInformation
using System.Threading;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public enum LockRecursionPolicy
    {
        NoRecursion = 0,
        SupportsRecursion = 1,
    }

    //
    // ReaderWriterCount tracks how many of each kind of lock is held by each thread.
    // We keep a linked list for each thread, attached to a ThreadStatic field.
    // These are reused wherever possible, so that a given thread will only
    // allocate N of these, where N is the maximum number of locks held simultaneously
    // by that thread.
    // 
    internal class ReaderWriterCount
    {
        // Which lock does this object belong to?  This is a numeric ID for two reasons:
        // 1) We don't want this field to keep the lock object alive, and a WeakReference would
        //    be too expensive.
        // 2) Setting the value of a long is faster than setting the value of a reference.
        //    The "hot" paths in ReaderWriterLockSlim are short enough that this actually
        //    matters.
        public long lockID;

        // How many reader locks does this thread hold on this ReaderWriterLockSlim instance?
        public int readercount;

        // Ditto for writer/upgrader counts.  These are only used if the lock allows recursion.
        // But we have to have the fields on every ReaderWriterCount instance, because 
        // we reuse it for different locks.
        public int writercount;
        public int upgradecount;

        // Next RWC in this thread's list.
        public ReaderWriterCount next;
    }

    /// <summary>
    /// A reader-writer lock implementation that is intended to be simple, yet very
    /// efficient.  In particular only 1 interlocked operation is taken for any lock 
    /// operation (we use spin locks to achieve this).  The spin lock is never held
    /// for more than a few instructions (in particular, we never call event APIs
    /// or in fact any non-trivial API while holding the spin lock).   
    /// </summary>
    public class ReaderWriterLockSlim : IDisposable
    {
        //Specifying if locked can be reacquired recursively.
        private bool _fIsReentrant;

        // Lock specification for myLock:  This lock protects exactly the local fields associated with this
        // instance of ReaderWriterLockSlim.  It does NOT protect the memory associated with 
        // the events that hang off this lock (eg writeEvent, readEvent upgradeEvent).
        private int _myLock;

        //The variables controlling spinning behavior of Mylock(which is a spin-lock)

        private const int LockSpinCycles = 20;
        private const int LockSpinCount = 10;
        private const int LockSleep0Count = 5;

        // These variables allow use to avoid Setting events (which is expensive) if we don't have to. 
        private uint _numWriteWaiters;        // maximum number of threads that can be doing a WaitOne on the writeEvent 
        private uint _numReadWaiters;         // maximum number of threads that can be doing a WaitOne on the readEvent
        private uint _numWriteUpgradeWaiters;      // maximum number of threads that can be doing a WaitOne on the upgradeEvent (at most 1). 
        private uint _numUpgradeWaiters;

        //Variable used for quick check when there are no waiters.
        private bool _fNoWaiters;

        private int _upgradeLockOwnerId;
        private int _writeLockOwnerId;

        // conditions we wait on. 
        private EventWaitHandle _writeEvent;    // threads waiting to aquire a write lock go here.
        private EventWaitHandle _readEvent;     // threads waiting to aquire a read lock go here (will be released in bulk)
        private EventWaitHandle _upgradeEvent;  // thread waiting to acquire the upgrade lock
        private EventWaitHandle _waitUpgradeEvent;  // thread waiting to upgrade from the upgrade lock to a write lock go here (at most one)

        // Every lock instance has a unique ID, which is used by ReaderWriterCount to associate itself with the lock
        // without holding a reference to it.
        private static long s_nextLockID;
        private long _lockID;

        // See comments on ReaderWriterCount.
        [ThreadStatic]
        private static ReaderWriterCount t_rwc;

        private bool _fUpgradeThreadHoldingRead;

        private const int MaxSpinCount = 20;

        //The uint, that contains info like if the writer lock is held, num of 
        //readers etc.
        private uint _owners;

        //Various R/W masks
        //Note:
        //The Uint is divided as follows:
        //
        //Writer-Owned  Waiting-Writers   Waiting Upgraders     Num-Readers
        //    31          30                 29                 28.......0
        //
        //Dividing the uint, allows to vastly simplify logic for checking if a 
        //reader should go in etc. Setting the writer bit will automatically
        //make the value of the uint much larger than the max num of readers 
        //allowed, thus causing the check for max_readers to fail. 

        private const uint WRITER_HELD = 0x80000000;
        private const uint WAITING_WRITERS = 0x40000000;
        private const uint WAITING_UPGRADER = 0x20000000;

        //The max readers is actually one less then its theoretical max.
        //This is done in order to prevent reader count overflows. If the reader
        //count reaches max, other readers will wait.
        private const uint MAX_READER = 0x10000000 - 2;

        private const uint READER_MASK = 0x10000000 - 1;

        private bool _fDisposed;

        private void InitializeThreadCounts()
        {
            _upgradeLockOwnerId = -1;
            _writeLockOwnerId = -1;
        }

        public ReaderWriterLockSlim()
            : this(LockRecursionPolicy.NoRecursion)
        {
        }

        public ReaderWriterLockSlim(LockRecursionPolicy recursionPolicy)
        {
            if (recursionPolicy == LockRecursionPolicy.SupportsRecursion)
            {
                _fIsReentrant = true;
            }
            InitializeThreadCounts();
            _fNoWaiters = true;
            _lockID = Interlocked.Increment(ref s_nextLockID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsRWEntryEmpty(ReaderWriterCount rwc)
        {
            if (rwc.lockID == 0)
                return true;
            else if (rwc.readercount == 0 && rwc.writercount == 0 && rwc.upgradecount == 0)
                return true;
            else
                return false;
        }

        private bool IsRwHashEntryChanged(ReaderWriterCount lrwc)
        {
            return lrwc.lockID != _lockID;
        }

        /// <summary>
        /// This routine retrieves/sets the per-thread counts needed to enforce the
        /// various rules related to acquiring the lock. 
        /// 
        /// DontAllocate is set to true if the caller just wants to get an existing
        /// entry for this thread, but doesn't want to add one if an existing one
        /// could not be found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReaderWriterCount GetThreadRWCount(bool dontAllocate)
        {
            ReaderWriterCount rwc = t_rwc;
            ReaderWriterCount empty = null;
            while (rwc != null)
            {
                if (rwc.lockID == _lockID)
                    return rwc;

                if (!dontAllocate && empty == null && IsRWEntryEmpty(rwc))
                    empty = rwc;

                rwc = rwc.next;
            }

            if (dontAllocate)
                return null;

            if (empty == null)
            {
                empty = new ReaderWriterCount();
                empty.next = t_rwc;
                t_rwc = empty;
            }

            empty.lockID = _lockID;
            return empty;
        }

        public void EnterReadLock()
        {
            TryEnterReadLock(-1);
        }

        //
        // Common timeout support
        //
        private struct TimeoutTracker
        {
            private int _total;
            private int _start;

            public TimeoutTracker(TimeSpan timeout)
            {
                long ltm = (long)timeout.TotalMilliseconds;
                if (ltm < -1 || ltm > (long)Int32.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(timeout));
                _total = (int)ltm;
                if (_total != -1 && _total != 0)
                    _start = Environment.TickCount;
                else
                    _start = 0;
            }

            public TimeoutTracker(int millisecondsTimeout)
            {
                if (millisecondsTimeout < -1)
                    throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout));
                _total = millisecondsTimeout;
                if (_total != -1 && _total != 0)
                    _start = Environment.TickCount;
                else
                    _start = 0;
            }

            public int RemainingMilliseconds
            {
                get
                {
                    if (_total == -1 || _total == 0)
                        return _total;

                    int elapsed = Environment.TickCount - _start;
                    // elapsed may be negative if TickCount has overflowed by 2^31 milliseconds.
                    if (elapsed < 0 || elapsed >= _total)
                        return 0;

                    return _total - elapsed;
                }
            }

            public bool IsExpired
            {
                get
                {
                    return RemainingMilliseconds == 0;
                }
            }
        }

        public bool TryEnterReadLock(TimeSpan timeout)
        {
            return TryEnterReadLock(new TimeoutTracker(timeout));
        }

        public bool TryEnterReadLock(int millisecondsTimeout)
        {
            return TryEnterReadLock(new TimeoutTracker(millisecondsTimeout));
        }

        private bool TryEnterReadLock(TimeoutTracker timeout)
        {
            return TryEnterReadLockCore(timeout);
        }

        private bool TryEnterReadLockCore(TimeoutTracker timeout)
        {
            if (_fDisposed)
                throw new ObjectDisposedException(null);

            ReaderWriterCount lrwc = null;
            int id = Environment.CurrentManagedThreadId;

            if (!_fIsReentrant)
            {
                if (id == _writeLockOwnerId)
                {
                    //Check for AW->AR
                    throw new LockRecursionException(SR.LockRecursionException_ReadAfterWriteNotAllowed);
                }

                EnterMyLock();

                lrwc = GetThreadRWCount(false);

                //Check if the reader lock is already acquired. Note, we could
                //check the presence of a reader by not allocating rwc (But that 
                //would lead to two lookups in the common case. It's better to keep
                //a count in the structure).
                if (lrwc.readercount > 0)
                {
                    ExitMyLock();
                    throw new LockRecursionException(SR.LockRecursionException_RecursiveReadNotAllowed);
                }
                else if (id == _upgradeLockOwnerId)
                {
                    //The upgrade lock is already held.
                    //Update the global read counts and exit.

                    lrwc.readercount++;
                    _owners++;
                    ExitMyLock();
                    return true;
                }
            }
            else
            {
                EnterMyLock();
                lrwc = GetThreadRWCount(false);
                if (lrwc.readercount > 0)
                {
                    lrwc.readercount++;
                    ExitMyLock();
                    return true;
                }
                else if (id == _upgradeLockOwnerId)
                {
                    //The upgrade lock is already held.
                    //Update the global read counts and exit.
                    lrwc.readercount++;
                    _owners++;
                    ExitMyLock();
                    _fUpgradeThreadHoldingRead = true;
                    return true;
                }
                else if (id == _writeLockOwnerId)
                {
                    //The write lock is already held.
                    //Update global read counts here,
                    lrwc.readercount++;
                    _owners++;
                    ExitMyLock();
                    return true;
                }
            }

            bool retVal = true;

            int spincount = 0;

            for (; ;)
            {
                // We can enter a read lock if there are only read-locks have been given out
                // and a writer is not trying to get in.  

                if (_owners < MAX_READER)
                {
                    // Good case, there is no contention, we are basically done
                    _owners++;       // Indicate we have another reader
                    lrwc.readercount++;
                    break;
                }

                if (spincount < MaxSpinCount)
                {
                    ExitMyLock();
                    if (timeout.IsExpired)
                        return false;
                    spincount++;
                    SpinWait(spincount);
                    EnterMyLock();
                    //The per-thread structure may have been recycled as the lock is acquired (due to message pumping), load again.
                    if (IsRwHashEntryChanged(lrwc))
                        lrwc = GetThreadRWCount(false);
                    continue;
                }

                // Drat, we need to wait.  Mark that we have waiters and wait.  
                if (_readEvent == null)      // Create the needed event 
                {
                    LazyCreateEvent(ref _readEvent, false);
                    if (IsRwHashEntryChanged(lrwc))
                        lrwc = GetThreadRWCount(false);
                    continue;   // since we left the lock, start over. 
                }

                retVal = WaitOnEvent(_readEvent, ref _numReadWaiters, timeout, isWriteWaiter: false);
                if (!retVal)
                {
                    return false;
                }
                if (IsRwHashEntryChanged(lrwc))
                    lrwc = GetThreadRWCount(false);
            }

            ExitMyLock();
            return retVal;
        }

        public void EnterWriteLock()
        {
            TryEnterWriteLock(-1);
        }

        public bool TryEnterWriteLock(TimeSpan timeout)
        {
            return TryEnterWriteLock(new TimeoutTracker(timeout));
        }

        public bool TryEnterWriteLock(int millisecondsTimeout)
        {
            return TryEnterWriteLock(new TimeoutTracker(millisecondsTimeout));
        }

        private bool TryEnterWriteLock(TimeoutTracker timeout)
        {
            return TryEnterWriteLockCore(timeout);
        }

        private bool TryEnterWriteLockCore(TimeoutTracker timeout)
        {
            if (_fDisposed)
                throw new ObjectDisposedException(null);

            int id = Environment.CurrentManagedThreadId;
            ReaderWriterCount lrwc;
            bool upgradingToWrite = false;

            if (!_fIsReentrant)
            {
                if (id == _writeLockOwnerId)
                {
                    //Check for AW->AW
                    throw new LockRecursionException(SR.LockRecursionException_RecursiveWriteNotAllowed);
                }
                else if (id == _upgradeLockOwnerId)
                {
                    //AU->AW case is allowed once.
                    upgradingToWrite = true;
                }

                EnterMyLock();
                lrwc = GetThreadRWCount(true);

                //Can't acquire write lock with reader lock held. 
                if (lrwc != null && lrwc.readercount > 0)
                {
                    ExitMyLock();
                    throw new LockRecursionException(SR.LockRecursionException_WriteAfterReadNotAllowed);
                }
            }
            else
            {
                EnterMyLock();
                lrwc = GetThreadRWCount(false);

                if (id == _writeLockOwnerId)
                {
                    lrwc.writercount++;
                    ExitMyLock();
                    return true;
                }
                else if (id == _upgradeLockOwnerId)
                {
                    upgradingToWrite = true;
                }
                else if (lrwc.readercount > 0)
                {
                    //Write locks may not be acquired if only read locks have been
                    //acquired.
                    ExitMyLock();
                    throw new LockRecursionException(SR.LockRecursionException_WriteAfterReadNotAllowed);
                }
            }

            int spincount = 0;
            bool retVal = true;

            for (; ;)
            {
                if (IsWriterAcquired())
                {
                    // Good case, there is no contention, we are basically done
                    SetWriterAcquired();
                    break;
                }

                //Check if there is just one upgrader, and no readers.
                //Assumption: Only one thread can have the upgrade lock, so the 
                //following check will fail for all other threads that may sneak in 
                //when the upgrading thread is waiting.

                if (upgradingToWrite)
                {
                    uint readercount = GetNumReaders();

                    if (readercount == 1)
                    {
                        //Good case again, there is just one upgrader, and no readers.
                        SetWriterAcquired();    // indicate we have a writer.
                        break;
                    }
                    else if (readercount == 2)
                    {
                        if (lrwc != null)
                        {
                            if (IsRwHashEntryChanged(lrwc))
                                lrwc = GetThreadRWCount(false);

                            if (lrwc.readercount > 0)
                            {
                                //This check is needed for EU->ER->EW case, as the owner count will be two.
                                Debug.Assert(_fIsReentrant);
                                Debug.Assert(_fUpgradeThreadHoldingRead);

                                //Good case again, there is just one upgrader, and no readers.
                                SetWriterAcquired();   // indicate we have a writer.
                                break;
                            }
                        }
                    }
                }

                if (spincount < MaxSpinCount)
                {
                    ExitMyLock();
                    if (timeout.IsExpired)
                        return false;
                    spincount++;
                    SpinWait(spincount);
                    EnterMyLock();
                    continue;
                }

                if (upgradingToWrite)
                {
                    if (_waitUpgradeEvent == null)   // Create the needed event
                    {
                        LazyCreateEvent(ref _waitUpgradeEvent, true);
                        continue;   // since we left the lock, start over. 
                    }

                    Debug.Assert(_numWriteUpgradeWaiters == 0, "There can be at most one thread with the upgrade lock held.");

                    retVal = WaitOnEvent(_waitUpgradeEvent, ref _numWriteUpgradeWaiters, timeout, isWriteWaiter: true);

                    //The lock is not held in case of failure.
                    if (!retVal)
                        return false;
                }
                else
                {
                    // Drat, we need to wait.  Mark that we have waiters and wait.
                    if (_writeEvent == null)     // create the needed event.
                    {
                        LazyCreateEvent(ref _writeEvent, true);
                        continue;   // since we left the lock, start over. 
                    }

                    retVal = WaitOnEvent(_writeEvent, ref _numWriteWaiters, timeout, isWriteWaiter: true);
                    //The lock is not held in case of failure.
                    if (!retVal)
                        return false;
                }
            }

            Debug.Assert((_owners & WRITER_HELD) > 0);

            if (_fIsReentrant)
            {
                if (IsRwHashEntryChanged(lrwc))
                    lrwc = GetThreadRWCount(false);
                lrwc.writercount++;
            }

            ExitMyLock();

            _writeLockOwnerId = id;

            return true;
        }

        public void EnterUpgradeableReadLock()
        {
            TryEnterUpgradeableReadLock(-1);
        }

        public bool TryEnterUpgradeableReadLock(TimeSpan timeout)
        {
            return TryEnterUpgradeableReadLock(new TimeoutTracker(timeout));
        }

        public bool TryEnterUpgradeableReadLock(int millisecondsTimeout)
        {
            return TryEnterUpgradeableReadLock(new TimeoutTracker(millisecondsTimeout));
        }

        private bool TryEnterUpgradeableReadLock(TimeoutTracker timeout)
        {
            return TryEnterUpgradeableReadLockCore(timeout);
        }

        private bool TryEnterUpgradeableReadLockCore(TimeoutTracker timeout)
        {
            if (_fDisposed)
                throw new ObjectDisposedException(null);

            int id = Environment.CurrentManagedThreadId;
            ReaderWriterCount lrwc;

            if (!_fIsReentrant)
            {
                if (id == _upgradeLockOwnerId)
                {
                    //Check for AU->AU
                    throw new LockRecursionException(SR.LockRecursionException_RecursiveUpgradeNotAllowed);
                }
                else if (id == _writeLockOwnerId)
                {
                    //Check for AU->AW
                    throw new LockRecursionException(SR.LockRecursionException_UpgradeAfterWriteNotAllowed);
                }

                EnterMyLock();
                lrwc = GetThreadRWCount(true);
                //Can't acquire upgrade lock with reader lock held. 
                if (lrwc != null && lrwc.readercount > 0)
                {
                    ExitMyLock();
                    throw new LockRecursionException(SR.LockRecursionException_UpgradeAfterReadNotAllowed);
                }
            }
            else
            {
                EnterMyLock();
                lrwc = GetThreadRWCount(false);

                if (id == _upgradeLockOwnerId)
                {
                    lrwc.upgradecount++;
                    ExitMyLock();
                    return true;
                }
                else if (id == _writeLockOwnerId)
                {
                    //Write lock is already held, Just update the global state 
                    //to show presence of upgrader.
                    Debug.Assert((_owners & WRITER_HELD) > 0);
                    _owners++;
                    _upgradeLockOwnerId = id;
                    lrwc.upgradecount++;
                    if (lrwc.readercount > 0)
                        _fUpgradeThreadHoldingRead = true;
                    ExitMyLock();
                    return true;
                }
                else if (lrwc.readercount > 0)
                {
                    //Upgrade locks may not be acquired if only read locks have been
                    //acquired.                
                    ExitMyLock();
                    throw new LockRecursionException(SR.LockRecursionException_UpgradeAfterReadNotAllowed);
                }
            }

            bool retVal = true;

            int spincount = 0;

            for (; ;)
            {
                //Once an upgrade lock is taken, it's like having a reader lock held
                //until upgrade or downgrade operations are performed.              

                if ((_upgradeLockOwnerId == -1) && (_owners < MAX_READER))
                {
                    _owners++;
                    _upgradeLockOwnerId = id;
                    break;
                }

                if (spincount < MaxSpinCount)
                {
                    ExitMyLock();
                    if (timeout.IsExpired)
                        return false;
                    spincount++;
                    SpinWait(spincount);
                    EnterMyLock();
                    continue;
                }

                // Drat, we need to wait.  Mark that we have waiters and wait. 
                if (_upgradeEvent == null)   // Create the needed event
                {
                    LazyCreateEvent(ref _upgradeEvent, true);
                    continue;   // since we left the lock, start over. 
                }

                //Only one thread with the upgrade lock held can proceed.
                retVal = WaitOnEvent(_upgradeEvent, ref _numUpgradeWaiters, timeout, isWriteWaiter: false);
                if (!retVal)
                    return false;
            }

            if (_fIsReentrant)
            {
                //The lock may have been dropped getting here, so make a quick check to see whether some other
                //thread did not grab the entry.
                if (IsRwHashEntryChanged(lrwc))
                    lrwc = GetThreadRWCount(false);
                lrwc.upgradecount++;
            }

            ExitMyLock();

            return true;
        }

        public void ExitReadLock()
        {
            ReaderWriterCount lrwc = null;

            EnterMyLock();

            lrwc = GetThreadRWCount(true);

            if (lrwc == null || lrwc.readercount < 1)
            {
                //You have to be holding the read lock to make this call.
                ExitMyLock();
                throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedRead);
            }

            if (_fIsReentrant)
            {
                if (lrwc.readercount > 1)
                {
                    lrwc.readercount--;
                    ExitMyLock();
                    return;
                }

                if (Environment.CurrentManagedThreadId == _upgradeLockOwnerId)
                {
                    _fUpgradeThreadHoldingRead = false;
                }
            }

            Debug.Assert(_owners > 0, "ReleasingReaderLock: releasing lock and no read lock taken");

            --_owners;

            Debug.Assert(lrwc.readercount == 1);
            lrwc.readercount--;

            ExitAndWakeUpAppropriateWaiters();
        }

        public void ExitWriteLock()
        {
            ReaderWriterCount lrwc;
            if (!_fIsReentrant)
            {
                if (Environment.CurrentManagedThreadId != _writeLockOwnerId)
                {
                    //You have to be holding the write lock to make this call.
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedWrite);
                }
                EnterMyLock();
            }
            else
            {
                EnterMyLock();
                lrwc = GetThreadRWCount(false);

                if (lrwc == null)
                {
                    ExitMyLock();
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedWrite);
                }

                if (lrwc.writercount < 1)
                {
                    ExitMyLock();
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedWrite);
                }

                lrwc.writercount--;

                if (lrwc.writercount > 0)
                {
                    ExitMyLock();
                    return;
                }
            }

            Debug.Assert((_owners & WRITER_HELD) > 0, "Calling ReleaseWriterLock when no write lock is held");

            ClearWriterAcquired();

            _writeLockOwnerId = -1;

            ExitAndWakeUpAppropriateWaiters();
        }

        public void ExitUpgradeableReadLock()
        {
            ReaderWriterCount lrwc;
            if (!_fIsReentrant)
            {
                if (Environment.CurrentManagedThreadId != _upgradeLockOwnerId)
                {
                    //You have to be holding the upgrade lock to make this call.
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedUpgrade);
                }
                EnterMyLock();
            }
            else
            {
                EnterMyLock();
                lrwc = GetThreadRWCount(true);

                if (lrwc == null)
                {
                    ExitMyLock();
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedUpgrade);
                }

                if (lrwc.upgradecount < 1)
                {
                    ExitMyLock();
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedUpgrade);
                }

                lrwc.upgradecount--;

                if (lrwc.upgradecount > 0)
                {
                    ExitMyLock();
                    return;
                }

                _fUpgradeThreadHoldingRead = false;
            }

            _owners--;
            _upgradeLockOwnerId = -1;

            ExitAndWakeUpAppropriateWaiters();
        }

        /// <summary>
        /// A routine for lazily creating a event outside the lock (so if errors
        /// happen they are outside the lock and that we don't do much work
        /// while holding a spin lock).  If all goes well, reenter the lock and
        /// set 'waitEvent' 
        /// </summary>
        private void LazyCreateEvent(ref EventWaitHandle waitEvent, bool makeAutoResetEvent)
        {
#if DEBUG
            Debug.Assert(MyLockHeld);
            Debug.Assert(waitEvent == null);
#endif
            ExitMyLock();
            EventWaitHandle newEvent;
            if (makeAutoResetEvent)
                newEvent = new AutoResetEvent(false);
            else
                newEvent = new ManualResetEvent(false);
            EnterMyLock();
            if (waitEvent == null)          // maybe someone snuck in. 
                waitEvent = newEvent;
            else
                newEvent.Dispose();
        }

        /// <summary>
        /// Waits on 'waitEvent' with a timeout  
        /// Before the wait 'numWaiters' is incremented and is restored before leaving this routine.
        /// </summary>
        private bool WaitOnEvent(
            EventWaitHandle waitEvent,
            ref uint numWaiters,
            TimeoutTracker timeout,
            bool isWriteWaiter)
        {
#if DEBUG
            Debug.Assert(MyLockHeld);
#endif
            waitEvent.Reset();
            numWaiters++;
            _fNoWaiters = false;

            //Setting these bits will prevent new readers from getting in.
            if (_numWriteWaiters == 1)
                SetWritersWaiting();
            if (_numWriteUpgradeWaiters == 1)
                SetUpgraderWaiting();

            bool waitSuccessful = false;
            ExitMyLock();      // Do the wait outside of any lock

            try
            {
                waitSuccessful = waitEvent.WaitOne(timeout.RemainingMilliseconds);
            }
            finally
            {
                EnterMyLock();
                --numWaiters;

                if (_numWriteWaiters == 0 && _numWriteUpgradeWaiters == 0 && _numUpgradeWaiters == 0 && _numReadWaiters == 0)
                    _fNoWaiters = true;

                if (_numWriteWaiters == 0)
                    ClearWritersWaiting();
                if (_numWriteUpgradeWaiters == 0)
                    ClearUpgraderWaiting();

                if (!waitSuccessful)        // We may also be about to throw for some reason.  Exit myLock.
                {
                    if (isWriteWaiter)
                    {
                        // Write waiters block read waiters from acquiring the lock. Since this was the last write waiter, try
                        // to wake up the appropriate read waiters.
                        ExitAndWakeUpAppropriateReadWaiters();
                    }
                    else
                        ExitMyLock();
                }
            }
            return waitSuccessful;
        }

        /// <summary>
        /// Determines the appropriate events to set, leaves the locks, and sets the events. 
        /// </summary>
        private void ExitAndWakeUpAppropriateWaiters()
        {
#if DEBUG
            Debug.Assert(MyLockHeld);
#endif
            if (_fNoWaiters)
            {
                ExitMyLock();
                return;
            }

            ExitAndWakeUpAppropriateWaitersPreferringWriters();
        }

        private void ExitAndWakeUpAppropriateWaitersPreferringWriters()
        {
            uint readercount = GetNumReaders();

            //We need this case for EU->ER->EW case, as the read count will be 2 in
            //that scenario.
            if (_fIsReentrant)
            {
                if (_numWriteUpgradeWaiters > 0 && _fUpgradeThreadHoldingRead && readercount == 2)
                {
                    ExitMyLock();          // Exit before signaling to improve efficiency (wakee will need the lock)
                    _waitUpgradeEvent.Set();     // release all upgraders (however there can be at most one). 
                    return;
                }
            }

            if (readercount == 1 && _numWriteUpgradeWaiters > 0)
            {
                //We have to be careful now, as we are droppping the lock. 
                //No new writes should be allowed to sneak in if an upgrade
                //was pending. 

                ExitMyLock();          // Exit before signaling to improve efficiency (wakee will need the lock)
                _waitUpgradeEvent.Set();     // release all upgraders (however there can be at most one).            
            }
            else if (readercount == 0 && _numWriteWaiters > 0)
            {
                ExitMyLock();      // Exit before signaling to improve efficiency (wakee will need the lock)
                _writeEvent.Set();   // release one writer. 
            }
            else
            {
                ExitAndWakeUpAppropriateReadWaiters();
            }
        }

        private void ExitAndWakeUpAppropriateReadWaiters()
        {
#if DEBUG
            Debug.Assert(MyLockHeld);
#endif

            if (_numWriteWaiters != 0 || _numWriteUpgradeWaiters != 0 || _fNoWaiters)
            {
                ExitMyLock();
                return;
            }

            Debug.Assert(_numReadWaiters != 0 || _numUpgradeWaiters != 0);

            bool setReadEvent = _numReadWaiters != 0;
            bool setUpgradeEvent = _numUpgradeWaiters != 0 && _upgradeLockOwnerId == -1;

            ExitMyLock();    // Exit before signaling to improve efficiency (wakee will need the lock)

            if (setReadEvent)
                _readEvent.Set();  // release all readers. 

            if (setUpgradeEvent)
                _upgradeEvent.Set(); //release one upgrader.
        }

        private bool IsWriterAcquired()
        {
            return (_owners & ~WAITING_WRITERS) == 0;
        }

        private void SetWriterAcquired()
        {
            _owners |= WRITER_HELD;    // indicate we have a writer.
        }

        private void ClearWriterAcquired()
        {
            _owners &= ~WRITER_HELD;
        }

        private void SetWritersWaiting()
        {
            _owners |= WAITING_WRITERS;
        }

        private void ClearWritersWaiting()
        {
            _owners &= ~WAITING_WRITERS;
        }

        private void SetUpgraderWaiting()
        {
            _owners |= WAITING_UPGRADER;
        }

        private void ClearUpgraderWaiting()
        {
            _owners &= ~WAITING_UPGRADER;
        }

        private uint GetNumReaders()
        {
            return _owners & READER_MASK;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnterMyLock()
        {
            if (Interlocked.CompareExchange(ref _myLock, 1, 0) != 0)
                EnterMyLockSpin();
        }

        private void EnterMyLockSpin()
        {
            int pc = Environment.ProcessorCount;
            for (int i = 0; ; i++)
            {
                if (i < LockSpinCount && pc > 1)
                {
                    Helpers.Spin(LockSpinCycles * (i + 1)); // Wait a few dozen instructions to let another processor release lock.
                }
                else if (i < (LockSpinCount + LockSleep0Count))
                {
                    Helpers.Sleep(0);   // Give up my quantum.  
                }
                else
                {
                    Helpers.Sleep(1);   // Give up my quantum.  
                }

                if (_myLock == 0 && Interlocked.CompareExchange(ref _myLock, 1, 0) == 0)
                    return;
            }
        }

        private void ExitMyLock()
        {
            Debug.Assert(_myLock != 0, "Exiting spin lock that is not held");
            Volatile.Write(ref _myLock, 0);
        }

#if DEBUG
        private bool MyLockHeld { get { return _myLock != 0; } }
#endif

        private static void SpinWait(int SpinCount)
        {
            //Exponential backoff
            if ((SpinCount < 5) && (Environment.ProcessorCount > 1))
            {
                Helpers.Spin(LockSpinCycles * SpinCount);
            }
            else if (SpinCount < MaxSpinCount - 3)
            {
                Helpers.Sleep(0);
            }
            else
            {
                Helpers.Sleep(1);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && !_fDisposed)
            {
                if (WaitingReadCount > 0 || WaitingUpgradeCount > 0 || WaitingWriteCount > 0)
                    throw new SynchronizationLockException(SR.SynchronizationLockException_IncorrectDispose);

                if (IsReadLockHeld || IsUpgradeableReadLockHeld || IsWriteLockHeld)
                    throw new SynchronizationLockException(SR.SynchronizationLockException_IncorrectDispose);

                if (_writeEvent != null)
                {
                    _writeEvent.Dispose();
                    _writeEvent = null;
                }

                if (_readEvent != null)
                {
                    _readEvent.Dispose();
                    _readEvent = null;
                }

                if (_upgradeEvent != null)
                {
                    _upgradeEvent.Dispose();
                    _upgradeEvent = null;
                }

                if (_waitUpgradeEvent != null)
                {
                    _waitUpgradeEvent.Dispose();
                    _waitUpgradeEvent = null;
                }

                _fDisposed = true;
            }
        }

        public bool IsReadLockHeld
        {
            get
            {
                if (RecursiveReadCount > 0)
                    return true;
                else
                    return false;
            }
        }

        public bool IsUpgradeableReadLockHeld
        {
            get
            {
                if (RecursiveUpgradeCount > 0)
                    return true;
                else
                    return false;
            }
        }

        public bool IsWriteLockHeld
        {
            get
            {
                if (RecursiveWriteCount > 0)
                    return true;
                else
                    return false;
            }
        }

        public LockRecursionPolicy RecursionPolicy
        {
            get
            {
                if (_fIsReentrant)
                {
                    return LockRecursionPolicy.SupportsRecursion;
                }
                else
                {
                    return LockRecursionPolicy.NoRecursion;
                }
            }
        }

        public int CurrentReadCount
        {
            get
            {
                int numreaders = (int)GetNumReaders();

                if (_upgradeLockOwnerId != -1)
                    return numreaders - 1;
                else
                    return numreaders;
            }
        }


        public int RecursiveReadCount
        {
            get
            {
                int count = 0;
                ReaderWriterCount lrwc = GetThreadRWCount(true);
                if (lrwc != null)
                    count = lrwc.readercount;

                return count;
            }
        }

        public int RecursiveUpgradeCount
        {
            get
            {
                if (_fIsReentrant)
                {
                    int count = 0;

                    ReaderWriterCount lrwc = GetThreadRWCount(true);
                    if (lrwc != null)
                        count = lrwc.upgradecount;

                    return count;
                }
                else
                {
                    if (Environment.CurrentManagedThreadId == _upgradeLockOwnerId)
                        return 1;
                    else
                        return 0;
                }
            }
        }

        public int RecursiveWriteCount
        {
            get
            {
                if (_fIsReentrant)
                {
                    int count = 0;

                    ReaderWriterCount lrwc = GetThreadRWCount(true);
                    if (lrwc != null)
                        count = lrwc.writercount;

                    return count;
                }
                else
                {
                    if (Environment.CurrentManagedThreadId == _writeLockOwnerId)
                        return 1;
                    else
                        return 0;
                }
            }
        }

        public int WaitingReadCount
        {
            get
            {
                return (int)_numReadWaiters;
            }
        }

        public int WaitingUpgradeCount
        {
            get
            {
                return (int)_numUpgradeWaiters;
            }
        }

        public int WaitingWriteCount
        {
            get
            {
                return (int)_numWriteWaiters;
            }
        }
    }
}
