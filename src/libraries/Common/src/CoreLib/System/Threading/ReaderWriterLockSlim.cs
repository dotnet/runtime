// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics; // for TraceInformation
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
        public ReaderWriterCount? next;
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
        private static readonly int ProcessorCount = Environment.ProcessorCount;

        //Specifying if the lock can be reacquired recursively.
        private readonly bool _fIsReentrant;

        // Lock specification for _spinLock:  This lock protects exactly the local fields associated with this
        // instance of ReaderWriterLockSlim.  It does NOT protect the memory associated with 
        // the events that hang off this lock (eg writeEvent, readEvent upgradeEvent).
        SpinLock _spinLock;

        // These variables allow use to avoid Setting events (which is expensive) if we don't have to. 
        private uint _numWriteWaiters;        // maximum number of threads that can be doing a WaitOne on the writeEvent 
        private uint _numReadWaiters;         // maximum number of threads that can be doing a WaitOne on the readEvent
        private uint _numWriteUpgradeWaiters;      // maximum number of threads that can be doing a WaitOne on the upgradeEvent (at most 1). 
        private uint _numUpgradeWaiters;

        private WaiterStates _waiterStates;

        private int _upgradeLockOwnerId;
        private int _writeLockOwnerId;

        // conditions we wait on. 
        private EventWaitHandle? _writeEvent;    // threads waiting to acquire a write lock go here.
        private EventWaitHandle? _readEvent;     // threads waiting to acquire a read lock go here (will be released in bulk)
        private EventWaitHandle? _upgradeEvent;  // thread waiting to acquire the upgrade lock
        private EventWaitHandle? _waitUpgradeEvent;  // thread waiting to upgrade from the upgrade lock to a write lock go here (at most one)

        // Every lock instance has a unique ID, which is used by ReaderWriterCount to associate itself with the lock
        // without holding a reference to it.
        private static long s_nextLockID;
        private long _lockID;

        // See comments on ReaderWriterCount.
        [ThreadStatic]
        private static ReaderWriterCount? t_rwc;

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
            _waiterStates = WaiterStates.NoWaiters;
            _lockID = Interlocked.Increment(ref s_nextLockID);
        }

        private bool HasNoWaiters
        {
            get
            {
#if DEBUG
                Debug.Assert(_spinLock.IsHeld);
#endif

                return (_waiterStates & WaiterStates.NoWaiters) != WaiterStates.None;
            }
            set
            {
#if DEBUG
                Debug.Assert(_spinLock.IsHeld);
#endif

                if (value)
                {
                    _waiterStates |= WaiterStates.NoWaiters;
                }
                else
                {
                    _waiterStates &= ~WaiterStates.NoWaiters;
                }
            }
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
        private ReaderWriterCount? GetThreadRWCount(bool dontAllocate) // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
        {
            ReaderWriterCount? rwc = t_rwc;
            ReaderWriterCount? empty = null;
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
                if (ltm < -1 || ltm > (long)int.MaxValue)
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

            ReaderWriterCount lrwc;
            int id = Environment.CurrentManagedThreadId;

            if (!_fIsReentrant)
            {
                if (id == _writeLockOwnerId)
                {
                    //Check for AW->AR
                    throw new LockRecursionException(SR.LockRecursionException_ReadAfterWriteNotAllowed);
                }

                _spinLock.Enter(EnterSpinLockReason.EnterAnyRead);

                lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761

                //Check if the reader lock is already acquired. Note, we could
                //check the presence of a reader by not allocating rwc (But that 
                //would lead to two lookups in the common case. It's better to keep
                //a count in the structure).
                if (lrwc.readercount > 0)
                {
                    _spinLock.Exit();
                    throw new LockRecursionException(SR.LockRecursionException_RecursiveReadNotAllowed);
                }
                else if (id == _upgradeLockOwnerId)
                {
                    //The upgrade lock is already held.
                    //Update the global read counts and exit.

                    lrwc.readercount++;
                    _owners++;
                    _spinLock.Exit();
                    return true;
                }
            }
            else
            {
                _spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
                lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                if (lrwc.readercount > 0)
                {
                    lrwc.readercount++;
                    _spinLock.Exit();
                    return true;
                }
                else if (id == _upgradeLockOwnerId)
                {
                    //The upgrade lock is already held.
                    //Update the global read counts and exit.
                    lrwc.readercount++;
                    _owners++;
                    _spinLock.Exit();
                    _fUpgradeThreadHoldingRead = true;
                    return true;
                }
                else if (id == _writeLockOwnerId)
                {
                    //The write lock is already held.
                    //Update global read counts here,
                    lrwc.readercount++;
                    _owners++;
                    _spinLock.Exit();
                    return true;
                }
            }

            bool retVal = true;
            int spinCount = 0;

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

                if (timeout.IsExpired)
                {
                    _spinLock.Exit();
                    return false;
                }

                if (spinCount < MaxSpinCount && ShouldSpinForEnterAnyRead())
                {
                    _spinLock.Exit();
                    spinCount++;
                    SpinWait(spinCount);
                    _spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
                    //The per-thread structure may have been recycled as the lock is acquired (due to message pumping), load again.
                    if (IsRwHashEntryChanged(lrwc))
                        lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                    continue;
                }

                // Drat, we need to wait.  Mark that we have waiters and wait.  
                if (_readEvent == null)      // Create the needed event 
                {
                    LazyCreateEvent(ref _readEvent, EnterLockType.Read);
                    if (IsRwHashEntryChanged(lrwc))
                        lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                    continue;   // since we left the lock, start over. 
                }

                retVal = WaitOnEvent(_readEvent, ref _numReadWaiters, timeout, EnterLockType.Read);
                if (!retVal)
                {
                    return false;
                }
                if (IsRwHashEntryChanged(lrwc))
                    lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
            }

            _spinLock.Exit();
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
            ReaderWriterCount? lrwc;
            bool upgradingToWrite = false;

            if (!_fIsReentrant)
            {
                EnterSpinLockReason enterMyLockReason;
                if (id == _writeLockOwnerId)
                {
                    //Check for AW->AW
                    throw new LockRecursionException(SR.LockRecursionException_RecursiveWriteNotAllowed);
                }
                else if (id == _upgradeLockOwnerId)
                {
                    //AU->AW case is allowed once.
                    upgradingToWrite = true;
                    enterMyLockReason = EnterSpinLockReason.UpgradeToWrite;
                }
                else
                {
                    enterMyLockReason = EnterSpinLockReason.EnterWrite;
                }
                _spinLock.Enter(enterMyLockReason);

                lrwc = GetThreadRWCount(dontAllocate: true);

                //Can't acquire write lock with reader lock held. 
                if (lrwc != null && lrwc.readercount > 0)
                {
                    _spinLock.Exit();
                    throw new LockRecursionException(SR.LockRecursionException_WriteAfterReadNotAllowed);
                }
            }
            else
            {
                EnterSpinLockReason enterMyLockReason;
                if (id == _writeLockOwnerId)
                {
                    enterMyLockReason = EnterSpinLockReason.EnterRecursiveWrite;
                }
                else if (id == _upgradeLockOwnerId)
                {
                    enterMyLockReason = EnterSpinLockReason.UpgradeToWrite;
                }
                else
                {
                    enterMyLockReason = EnterSpinLockReason.EnterWrite;
                }
                _spinLock.Enter(enterMyLockReason);

                lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761

                if (id == _writeLockOwnerId)
                {
                    lrwc.writercount++;
                    _spinLock.Exit();
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
                    _spinLock.Exit();
                    throw new LockRecursionException(SR.LockRecursionException_WriteAfterReadNotAllowed);
                }
            }

            bool retVal = true;
            int spinCount = 0;

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
                                lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761

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

                if (timeout.IsExpired)
                {
                    _spinLock.Exit();
                    return false;
                }

                if (spinCount < MaxSpinCount && ShouldSpinForEnterAnyWrite(upgradingToWrite))
                {
                    _spinLock.Exit();
                    spinCount++;
                    SpinWait(spinCount);
                    _spinLock.Enter(upgradingToWrite ? EnterSpinLockReason.UpgradeToWrite : EnterSpinLockReason.EnterWrite);
                    continue;
                }

                if (upgradingToWrite)
                {
                    if (_waitUpgradeEvent == null)   // Create the needed event
                    {
                        LazyCreateEvent(ref _waitUpgradeEvent, EnterLockType.UpgradeToWrite);
                        continue;   // since we left the lock, start over. 
                    }

                    Debug.Assert(_numWriteUpgradeWaiters == 0, "There can be at most one thread with the upgrade lock held.");

                    retVal = WaitOnEvent(_waitUpgradeEvent, ref _numWriteUpgradeWaiters, timeout, EnterLockType.UpgradeToWrite);

                    //The lock is not held in case of failure.
                    if (!retVal)
                        return false;
                }
                else
                {
                    // Drat, we need to wait.  Mark that we have waiters and wait.
                    if (_writeEvent == null)     // create the needed event.
                    {
                        LazyCreateEvent(ref _writeEvent, EnterLockType.Write);
                        continue;   // since we left the lock, start over. 
                    }

                    retVal = WaitOnEvent(_writeEvent, ref _numWriteWaiters, timeout, EnterLockType.Write);
                    //The lock is not held in case of failure.
                    if (!retVal)
                        return false;
                }
            }

            Debug.Assert((_owners & WRITER_HELD) > 0);

            if (_fIsReentrant)
            {
                Debug.Assert(lrwc != null, "Initialized based on _fIsReentrant earlier in the method");
                if (IsRwHashEntryChanged(lrwc))
                    lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                lrwc.writercount++;
            }

            _spinLock.Exit();

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
            ReaderWriterCount? lrwc;

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

                _spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
                lrwc = GetThreadRWCount(dontAllocate: true);
                //Can't acquire upgrade lock with reader lock held. 
                if (lrwc != null && lrwc.readercount > 0)
                {
                    _spinLock.Exit();
                    throw new LockRecursionException(SR.LockRecursionException_UpgradeAfterReadNotAllowed);
                }
            }
            else
            {
                _spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
                lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761

                if (id == _upgradeLockOwnerId)
                {
                    lrwc.upgradecount++;
                    _spinLock.Exit();
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
                    _spinLock.Exit();
                    return true;
                }
                else if (lrwc.readercount > 0)
                {
                    //Upgrade locks may not be acquired if only read locks have been
                    //acquired.                
                    _spinLock.Exit();
                    throw new LockRecursionException(SR.LockRecursionException_UpgradeAfterReadNotAllowed);
                }
            }

            bool retVal = true;
            int spinCount = 0;

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

                if (timeout.IsExpired)
                {
                    _spinLock.Exit();
                    return false;
                }

                if (spinCount < MaxSpinCount && ShouldSpinForEnterAnyRead())
                {
                    _spinLock.Exit();
                    spinCount++;
                    SpinWait(spinCount);
                    _spinLock.Enter(EnterSpinLockReason.EnterAnyRead);
                    continue;
                }

                // Drat, we need to wait.  Mark that we have waiters and wait. 
                if (_upgradeEvent == null)   // Create the needed event
                {
                    LazyCreateEvent(ref _upgradeEvent, EnterLockType.UpgradeableRead);
                    continue;   // since we left the lock, start over. 
                }

                //Only one thread with the upgrade lock held can proceed.
                retVal = WaitOnEvent(_upgradeEvent, ref _numUpgradeWaiters, timeout, EnterLockType.UpgradeableRead);
                if (!retVal)
                    return false;
            }

            if (_fIsReentrant)
            {
                //The lock may have been dropped getting here, so make a quick check to see whether some other
                //thread did not grab the entry.
                Debug.Assert(lrwc != null, "Initialized based on _fIsReentrant earlier in the method");
                if (IsRwHashEntryChanged(lrwc))
                    lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                lrwc.upgradecount++;
            }

            _spinLock.Exit();

            return true;
        }

        public void ExitReadLock()
        {
            _spinLock.Enter(EnterSpinLockReason.ExitAnyRead);

            ReaderWriterCount? lrwc = GetThreadRWCount(dontAllocate: true);

            if (lrwc == null || lrwc.readercount < 1)
            {
                //You have to be holding the read lock to make this call.
                _spinLock.Exit();
                throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedRead);
            }

            if (_fIsReentrant)
            {
                if (lrwc.readercount > 1)
                {
                    lrwc.readercount--;
                    _spinLock.Exit();
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
                _spinLock.Enter(EnterSpinLockReason.ExitAnyWrite);
            }
            else
            {
                _spinLock.Enter(EnterSpinLockReason.ExitAnyWrite);
                lrwc = GetThreadRWCount(dontAllocate: false)!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761

                if (lrwc == null)
                {
                    _spinLock.Exit();
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedWrite);
                }

                if (lrwc.writercount < 1)
                {
                    _spinLock.Exit();
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedWrite);
                }

                lrwc.writercount--;

                if (lrwc.writercount > 0)
                {
                    _spinLock.Exit();
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
            ReaderWriterCount? lrwc;
            if (!_fIsReentrant)
            {
                if (Environment.CurrentManagedThreadId != _upgradeLockOwnerId)
                {
                    //You have to be holding the upgrade lock to make this call.
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedUpgrade);
                }
                _spinLock.Enter(EnterSpinLockReason.ExitAnyRead);
            }
            else
            {
                _spinLock.Enter(EnterSpinLockReason.ExitAnyRead);
                lrwc = GetThreadRWCount(dontAllocate: true);

                if (lrwc == null)
                {
                    _spinLock.Exit();
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedUpgrade);
                }

                if (lrwc.upgradecount < 1)
                {
                    _spinLock.Exit();
                    throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedUpgrade);
                }

                lrwc.upgradecount--;

                if (lrwc.upgradecount > 0)
                {
                    _spinLock.Exit();
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
        private void LazyCreateEvent(ref EventWaitHandle? waitEvent, EnterLockType enterLockType) // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
        {
#if DEBUG
            Debug.Assert(_spinLock.IsHeld);
            Debug.Assert(waitEvent == null);
#endif

            _spinLock.Exit();

            var newEvent =
                new EventWaitHandle(
                    false,
                    enterLockType == EnterLockType.Read ? EventResetMode.ManualReset : EventResetMode.AutoReset);

            EnterSpinLockReason enterMyLockReason;
            switch (enterLockType)
            {
                case EnterLockType.Read:
                case EnterLockType.UpgradeableRead:
                    enterMyLockReason = EnterSpinLockReason.EnterAnyRead | EnterSpinLockReason.Wait;
                    break;

                case EnterLockType.Write:
                    enterMyLockReason = EnterSpinLockReason.EnterWrite | EnterSpinLockReason.Wait;
                    break;

                default:
                    Debug.Assert(enterLockType == EnterLockType.UpgradeToWrite);
                    enterMyLockReason = EnterSpinLockReason.UpgradeToWrite | EnterSpinLockReason.Wait;
                    break;
            }
            _spinLock.Enter(enterMyLockReason);

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
            EnterLockType enterLockType)
        {
#if DEBUG
            Debug.Assert(_spinLock.IsHeld);
#endif

            WaiterStates waiterSignaledState = WaiterStates.None;
            EnterSpinLockReason enterMyLockReason;
            switch (enterLockType)
            {
                case EnterLockType.UpgradeableRead:
                    waiterSignaledState = WaiterStates.UpgradeableReadWaiterSignaled;
                    goto case EnterLockType.Read;

                case EnterLockType.Read:
                    enterMyLockReason = EnterSpinLockReason.EnterAnyRead;
                    break;

                case EnterLockType.Write:
                    waiterSignaledState = WaiterStates.WriteWaiterSignaled;
                    enterMyLockReason = EnterSpinLockReason.EnterWrite;
                    break;

                default:
                    Debug.Assert(enterLockType == EnterLockType.UpgradeToWrite);
                    enterMyLockReason = EnterSpinLockReason.UpgradeToWrite;
                    break;
            }

            // It was not possible to acquire the RW lock because some other thread was holding some type of lock. The other
            // thread, when it releases its lock, will wake appropriate waiters. Along with resetting the wait event, clear the
            // waiter signaled bit for this type of waiter if applicable, to indicate that a waiter of this type is no longer
            // signaled.
            //
            // If the waiter signaled bit is not updated upon event reset, the following scenario would lead to deadlock:
            //   - Thread T0 signals the write waiter event or the upgradeable read waiter event to wake a waiter
            //   - There are no threads waiting on the event, but T1 is in WaitOnEvent() after exiting the spin lock and before
            //     actually waiting on the event (that is, it's recorded that there is one waiter for the event). It remains in
            //     this region for a while, in the repro case it typically gets context-switched out.
            //   - T2 acquires the RW lock in some fashion that blocks T0 or T3 from acquiring the RW lock
            //   - T0 or T3 fails to acquire the RW lock enough times for it to enter WaitOnEvent for the same event as T1
            //   - T0 or T3 resets the event
            //   - T2 releases the RW lock and does not wake a waiter because the reset at the previous step lost a signal but
            //     _waiterStates was not updated to reflect that
            //   - T1 and other threads begin waiting on the event, but there's no longer any thread that would wake them
            if (waiterSignaledState != WaiterStates.None && (_waiterStates & waiterSignaledState) != WaiterStates.None)
            {
                _waiterStates &= ~waiterSignaledState;
            }
            waitEvent.Reset();

            numWaiters++;
            HasNoWaiters = false;

            //Setting these bits will prevent new readers from getting in.
            if (_numWriteWaiters == 1)
                SetWritersWaiting();
            if (_numWriteUpgradeWaiters == 1)
                SetUpgraderWaiting();

            bool waitSuccessful = false;
            _spinLock.Exit();      // Do the wait outside of any lock

            try
            {
                waitSuccessful = waitEvent.WaitOne(timeout.RemainingMilliseconds);
            }
            finally
            {
                _spinLock.Enter(enterMyLockReason);

                --numWaiters;

                if (waitSuccessful &&
                    waiterSignaledState != WaiterStates.None &&
                    (_waiterStates & waiterSignaledState) != WaiterStates.None)
                {
                    // Indicate that a signaled waiter of this type has woken. Since non-read waiters are signaled to wake one
                    // at a time, we avoid waking up more than one waiter of that type upon successive enter/exit loops until
                    // the signaled thread actually wakes up. For example, if there are multiple write waiters and one thread is
                    // repeatedly entering and exiting a write lock, every exit would otherwise signal a different write waiter
                    // to wake up unnecessarily when only one woken waiter may actually succeed in entering the write lock.
                    _waiterStates &= ~waiterSignaledState;
                }

                if (_numWriteWaiters == 0 && _numWriteUpgradeWaiters == 0 && _numUpgradeWaiters == 0 && _numReadWaiters == 0)
                    HasNoWaiters = true;

                if (_numWriteWaiters == 0)
                    ClearWritersWaiting();
                if (_numWriteUpgradeWaiters == 0)
                    ClearUpgraderWaiting();

                if (!waitSuccessful)        // We may also be about to throw for some reason.  Exit myLock.
                {
                    if (enterLockType >= EnterLockType.Write)
                    {
                        // Write waiters block read waiters from acquiring the lock. Since this was the last write waiter, try
                        // to wake up the appropriate read waiters.
                        ExitAndWakeUpAppropriateReadWaiters();
                    }
                    else
                    {
                        _spinLock.Exit();
                    }
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
            Debug.Assert(_spinLock.IsHeld);
#endif
            if (HasNoWaiters)
            {
                _spinLock.Exit();
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
                    _spinLock.Exit();      // Exit before signaling to improve efficiency (wakee will need the lock)
                    _waitUpgradeEvent!.Set();     // release all upgraders (however there can be at most one).  Known non-null because _numWriteUpgradeWaiters > 0.
                    return;
                }
            }

            if (readercount == 1 && _numWriteUpgradeWaiters > 0)
            {
                //We have to be careful now, as we are dropping the lock. 
                //No new writes should be allowed to sneak in if an upgrade
                //was pending.

                _spinLock.Exit();      // Exit before signaling to improve efficiency (wakee will need the lock)
                _waitUpgradeEvent!.Set();     // release all upgraders (however there can be at most one). Known non-null because _numWriteUpgradeWaiters > 0.
            }
            else if (readercount == 0 && _numWriteWaiters > 0)
            {
                // Check if a waiter of the same type has already been signaled but hasn't woken yet. If so, avoid signaling
                // and waking another waiter unnecessarily.
                WaiterStates signaled = _waiterStates & WaiterStates.WriteWaiterSignaled;
                if (signaled == WaiterStates.None)
                {
                    _waiterStates |= WaiterStates.WriteWaiterSignaled;
                }

                _spinLock.Exit();      // Exit before signaling to improve efficiency (wakee will need the lock)

                if (signaled == WaiterStates.None)
                {
                    _writeEvent!.Set();   // release one writer.  Known non-null because _numWriteWaiters > 0.
                }
            }
            else
            {
                ExitAndWakeUpAppropriateReadWaiters();
            }
        }

        private void ExitAndWakeUpAppropriateReadWaiters()
        {
#if DEBUG
            Debug.Assert(_spinLock.IsHeld);
#endif

            if (_numWriteWaiters != 0 || _numWriteUpgradeWaiters != 0 || HasNoWaiters)
            {
                _spinLock.Exit();
                return;
            }

            Debug.Assert(_numReadWaiters != 0 || _numUpgradeWaiters != 0);

            bool setReadEvent = _numReadWaiters != 0;
            bool setUpgradeEvent = _numUpgradeWaiters != 0 && _upgradeLockOwnerId == -1;
            if (setUpgradeEvent)
            {
                // Check if a waiter of the same type has already been signaled but hasn't woken yet. If so, avoid signaling
                // and waking another waiter unnecessarily.
                if ((_waiterStates & WaiterStates.UpgradeableReadWaiterSignaled) == WaiterStates.None)
                {
                    _waiterStates |= WaiterStates.UpgradeableReadWaiterSignaled;
                }
                else
                {
                    setUpgradeEvent = false;
                }
            }

            _spinLock.Exit();    // Exit before signaling to improve efficiency (wakee will need the lock)

            if (setReadEvent)
                _readEvent!.Set();  // release all readers. Known non-null because _numUpgradeWaiters != 0.

            if (setUpgradeEvent)
                _upgradeEvent!.Set(); //release one upgrader.
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

        private bool ShouldSpinForEnterAnyRead()
        {
            // If there is a write waiter or write upgrade waiter, the waiter would block a reader from acquiring the RW lock
            // because the waiter takes precedence. In that case, the reader is not likely to make progress by spinning.
            // Although another thread holding a write lock would prevent this thread from acquiring a read lock, it is by
            // itself not a good enough reason to skip spinning.
            return HasNoWaiters || (_numWriteWaiters == 0 && _numWriteUpgradeWaiters == 0);
        }

        private bool ShouldSpinForEnterAnyWrite(bool isUpgradeToWrite)
        {
            // If there is a write upgrade waiter, the waiter would block a writer from acquiring the RW lock because the waiter
            // holds a read lock. In that case, the writer is not likely to make progress by spinning. Regarding upgrading to a
            // write lock, there is no type of waiter that would block the upgrade from happening. Although another thread
            // holding a read or write lock would prevent this thread from acquiring the write lock, it is by itself not a good
            // enough reason to skip spinning.
            return isUpgradeToWrite || _numWriteUpgradeWaiters == 0;
        }

        private static void SpinWait(int spinCount)
        {
            const int LockSpinCycles = 20;

            //Exponential back-off
            if ((spinCount < 5) && (ProcessorCount > 1))
            {
                Thread.SpinWait(LockSpinCycles * spinCount);
            }
            else
            {
                Thread.Sleep(0);
            }

            // Don't want to Sleep(1) in this spin wait:
            //   - Don't want to spin for that long, since a proper wait will follow when the spin wait fails. The artificial
            //     delay introduced by Sleep(1) will in some cases be much longer than desired.
            //   - Sleep(1) would put the thread into a wait state, and a proper wait will follow when the spin wait fails
            //     anyway, so it's preferable to put the thread into the proper wait state
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
                ReaderWriterCount? lrwc = GetThreadRWCount(dontAllocate: true);
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

                    ReaderWriterCount? lrwc = GetThreadRWCount(dontAllocate: true);
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

                    ReaderWriterCount? lrwc = GetThreadRWCount(dontAllocate: true);
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

        private struct SpinLock
        {
            private int _isLocked;

            /// <summary>
            /// Used to deprioritize threads attempting to enter the lock when they would not make progress after doing so.
            /// <see cref="EnterSpin(EnterSpinLockReason)"/> avoids acquiring the lock as long as the operation for which it
            /// was called is deprioritized.
            /// 
            /// Layout:
            /// - Low 16 bits: Number of threads that have deprioritized an enter-any-write operation
            /// - High 16 bits: Number of threads that have deprioritized an enter-any-read operation
            /// </summary>
            private int _enterDeprioritizationState;

            // Layout-specific constants for _enterDeprioritizationState
            private const int DeprioritizeEnterAnyReadIncrement = 1 << 16;
            private const int DeprioritizeEnterAnyWriteIncrement = 1;

            // The variables controlling spinning behavior of this spin lock
            private const int LockSpinCycles = 20;
            private const int LockSpinCount = 10;
            private const int LockSleep0Count = 5;
            private const int DeprioritizedLockSleep1Count = 5;

            private static int GetEnterDeprioritizationStateChange(EnterSpinLockReason reason)
            {
                EnterSpinLockReason operation = reason & EnterSpinLockReason.OperationMask;
                switch (operation)
                {
                    case EnterSpinLockReason.EnterAnyRead:
                        return 0;

                    case EnterSpinLockReason.ExitAnyRead:
                        // A read lock is held until this thread is able to exit it, so deprioritize enter-write threads as they
                        // will not be able to make progress
                        return DeprioritizeEnterAnyWriteIncrement;

                    case EnterSpinLockReason.EnterWrite:
                        // Writers are typically much less frequent and much less in number than readers. Waiting writers take
                        // precedence over new read attempts in order to let current readers release their lock and allow a
                        // writer to obtain the lock. Before a writer can register as a waiter though, the presence of just
                        // relatively few enter-read spins can easily starve the enter-write from even entering this lock,
                        // delaying its spin loop for an unreasonable duration.
                        //
                        // Deprioritize enter-read to preference enter-write. This makes it easier for enter-write threads to
                        // starve enter-read threads. However, writers can already by design starve readers. A waiting writer
                        // blocks enter-read threads and a new enter-write that needs to wait will be given precedence over
                        // previously waiting enter-read threads. So this is not a new problem, and the RW lock is designed for
                        // scenarios where writers are rare compared to readers.
                        return DeprioritizeEnterAnyReadIncrement;

                    default:
                        Debug.Assert(
                            operation == EnterSpinLockReason.UpgradeToWrite ||
                            operation == EnterSpinLockReason.EnterRecursiveWrite ||
                            operation == EnterSpinLockReason.ExitAnyWrite);

                        // UpgradeToWrite:
                        // - A read lock is held and an exit-read is not nearby, so deprioritize enter-write threads as they
                        //   will not be able to make progress. This thread also intends to enter a write lock, so deprioritize
                        //   enter -read threads as well, see case EnterSpinLockReason.EnterWrite for the rationale.
                        // EnterRecursiveWrite, ExitAnyWrite:
                        // - In both cases, a write lock is held until this thread is able to exit it, so deprioritize
                        //   enter -read and enter-write threads as they will not be able to make progress
                        return DeprioritizeEnterAnyReadIncrement + DeprioritizeEnterAnyWriteIncrement;
                }
            }

            private ushort EnterForEnterAnyReadDeprioritizedCount
            {
                get
                {
                    Debug.Assert(DeprioritizeEnterAnyReadIncrement == (1 << 16));
                    return (ushort)((uint)_enterDeprioritizationState >> 16);
                }
            }

            private ushort EnterForEnterAnyWriteDeprioritizedCount
            {
                get
                {
                    Debug.Assert(DeprioritizeEnterAnyWriteIncrement == 1);
                    return (ushort)_enterDeprioritizationState;
                }
            }

            private bool IsEnterDeprioritized(EnterSpinLockReason reason)
            {
                Debug.Assert((reason & EnterSpinLockReason.Wait) != 0 || reason == (reason & EnterSpinLockReason.OperationMask));
                Debug.Assert(
                    (reason & EnterSpinLockReason.Wait) == 0 ||
                    (reason & EnterSpinLockReason.OperationMask) == EnterSpinLockReason.EnterAnyRead ||
                    (reason & EnterSpinLockReason.OperationMask) == EnterSpinLockReason.EnterWrite ||
                    (reason & EnterSpinLockReason.OperationMask) == EnterSpinLockReason.UpgradeToWrite);

                switch (reason)
                {
                    default:
                        Debug.Assert(
                            (reason & EnterSpinLockReason.Wait) != 0 ||
                            reason == EnterSpinLockReason.ExitAnyRead ||
                            reason == EnterSpinLockReason.EnterRecursiveWrite ||
                            reason == EnterSpinLockReason.ExitAnyWrite);
                        return false;

                    case EnterSpinLockReason.EnterAnyRead:
                        return EnterForEnterAnyReadDeprioritizedCount != 0;

                    case EnterSpinLockReason.EnterWrite:
                        Debug.Assert((GetEnterDeprioritizationStateChange(reason) & DeprioritizeEnterAnyWriteIncrement) == 0);
                        return EnterForEnterAnyWriteDeprioritizedCount != 0;

                    case EnterSpinLockReason.UpgradeToWrite:
                        Debug.Assert((GetEnterDeprioritizationStateChange(reason) & DeprioritizeEnterAnyWriteIncrement) != 0);
                        return EnterForEnterAnyWriteDeprioritizedCount > 1;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool TryEnter()
            {
                return Interlocked.CompareExchange(ref _isLocked, 1, 0) == 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Enter(EnterSpinLockReason reason)
            {
                if (!TryEnter())
                {
                    EnterSpin(reason);
                }
            }

            private void EnterSpin(EnterSpinLockReason reason)
            {
                int deprioritizationStateChange = GetEnterDeprioritizationStateChange(reason);
                if (deprioritizationStateChange != 0)
                {
                    Interlocked.Add(ref _enterDeprioritizationState, deprioritizationStateChange);
                }

                int processorCount = ProcessorCount;
                for (int spinIndex = 0; ; spinIndex++)
                {
                    if (spinIndex < LockSpinCount && processorCount > 1)
                    {
                        Thread.SpinWait(LockSpinCycles * (spinIndex + 1)); // Wait a few dozen instructions to let another processor release lock.
                    }
                    else if (spinIndex < (LockSpinCount + LockSleep0Count))
                    {
                        Thread.Sleep(0);   // Give up my quantum.
                    }
                    else
                    {
                        Thread.Sleep(1);   // Give up my quantum.
                    }

                    if (!IsEnterDeprioritized(reason))
                    {
                        if (_isLocked == 0 && TryEnter())
                        {
                            if (deprioritizationStateChange != 0)
                            {
                                Interlocked.Add(ref _enterDeprioritizationState, -deprioritizationStateChange);
                            }
                            return;
                        }
                        continue;
                    }

                    // It's possible for an Enter thread to be deprioritized for an extended duration. It's undesirable for a
                    // deprioritized thread to keep waking up to spin despite a Sleep(1) when a large number of such threads are
                    // involved. After a threshold of Sleep(1)s, ignore the deprioritization and enter this lock to allow this
                    // thread to stop spinning and hopefully enter a proper wait state.
                    Debug.Assert(
                        reason == EnterSpinLockReason.EnterAnyRead ||
                        reason == EnterSpinLockReason.EnterWrite ||
                        reason == EnterSpinLockReason.UpgradeToWrite);
                    if (spinIndex >= (LockSpinCount + LockSleep0Count + DeprioritizedLockSleep1Count))
                    {
                        reason |= EnterSpinLockReason.Wait;
                        spinIndex = -1;
                    }
                }
            }

            public void Exit()
            {
                Debug.Assert(_isLocked != 0, "Exiting spin lock that is not held");
                Volatile.Write(ref _isLocked, 0);
            }

#if DEBUG
            public bool IsHeld => _isLocked != 0;
#endif
        }

        [Flags]
        private enum WaiterStates : byte
        {
            None = 0x0,

            // Used for quick check when there are no waiters
            NoWaiters = 0x1,

            // Used to avoid signaling more than one waiter to wake up when only one can make progress, see WaitOnEvent
            WriteWaiterSignaled = 0x2,
            UpgradeableReadWaiterSignaled = 0x4
            // Write upgrade waiters are excluded because there can only be one at any given time
        }

        private enum EnterSpinLockReason
        {
            EnterAnyRead = 0,
            ExitAnyRead = 1,
            EnterWrite = 2,
            UpgradeToWrite = 3,
            EnterRecursiveWrite = 4,
            ExitAnyWrite = 5,

            OperationMask = 0x7,

            Wait = 0x8
        }

        private enum EnterLockType
        {
            Read,
            UpgradeableRead,
            Write,
            UpgradeToWrite
        }
    }
}
