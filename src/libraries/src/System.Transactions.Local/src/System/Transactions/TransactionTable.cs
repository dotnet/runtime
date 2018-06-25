// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace System.Transactions
{
    internal class CheapUnfairReaderWriterLock
    {
        private object _writerFinishedEvent;

        private int _readersIn;
        private int _readersOut;
        private bool _writerPresent;

        private object _syncRoot;

        // Spin lock params
        private const int MAX_SPIN_COUNT = 100;
        private const int SLEEP_TIME = 500;

        public CheapUnfairReaderWriterLock()
        {
        }

        private object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }

        private bool ReadersPresent => _readersIn != _readersOut;

        private ManualResetEvent WriterFinishedEvent
        {
            get
            {
                if (_writerFinishedEvent == null)
                {
                    Interlocked.CompareExchange(ref _writerFinishedEvent, new ManualResetEvent(true), null);
                }
                return (ManualResetEvent)_writerFinishedEvent;
            }
        }

        public int EnterReadLock()
        {
            int readerIndex = 0;
            do
            {
                if (_writerPresent)
                {
                    WriterFinishedEvent.WaitOne();
                }

                readerIndex = Interlocked.Increment(ref _readersIn);

                if (!_writerPresent)
                {
                    break;
                }

                Interlocked.Decrement(ref _readersIn);
            }
            while (true);

            return readerIndex;
        }

        public void EnterWriteLock()
        {
            Monitor.Enter(SyncRoot);

            _writerPresent = true;
            WriterFinishedEvent.Reset();

            do
            {
                int i = 0;
                while (ReadersPresent && i < MAX_SPIN_COUNT)
                {
                    Thread.Sleep(0);
                    i++;
                }

                if (ReadersPresent)
                {
                    Thread.Sleep(SLEEP_TIME);
                }
            }
            while (ReadersPresent);
        }

        public void ExitReadLock()
        {
            Interlocked.Increment(ref _readersOut);
        }

        public void ExitWriteLock()
        {
            try
            {
                _writerPresent = false;
                WriterFinishedEvent.Set();
            }
            finally
            {
                Monitor.Exit(SyncRoot);
            }
        }
    }


    // This transaction table implementation uses an array of lists to avoid contention.  The list for a
    // transaction is decided by its hashcode.
    internal class TransactionTable
    {
        // Use a timer to initiate looking for transactions that have timed out.
        private Timer _timer;

        // Private storage noting if the timer is enabled.
        private bool _timerEnabled;

        // Store the timer interval
        private const int timerInternalExponent = 9;
        private int _timerInterval;

        // Store the number of ticks.  A tick is a mark of 1 timer interval.  By counting ticks
        // we can avoid expensive calls to get the current time for every transaction creation.
        private const long TicksPerMillisecond = 10000;
        private long _ticks;
        private long _lastTimerTime;

        // Sets of arrays of transactions.
        private BucketSet _headBucketSet;

        // Synchronize adding transactions with shutting off the timer and started events.
        private CheapUnfairReaderWriterLock _rwLock;

        internal TransactionTable()
        {
            // Create a timer that is initially disabled by specifing an Infinite time to the first interval
            _timer = new Timer(new TimerCallback(ThreadTimer), null, Timeout.Infinite, _timerInterval);

            // Note that the timer is disabled
            _timerEnabled = false;

            // Store the timer interval
            _timerInterval = 1 << TransactionTable.timerInternalExponent;

            // Ticks start off at zero.
            _ticks = 0;

            // The head of the list is long.MaxValue.  It contains all of the transactions that for
            // some reason or other don't have a timeout.
            _headBucketSet = new BucketSet(this, long.MaxValue);

            // Allocate the lock
            _rwLock = new CheapUnfairReaderWriterLock();
        }


        // Calculate the maximum number of ticks for which this transaction should live
        internal long TimeoutTicks(TimeSpan timeout)
        {
            if (timeout != TimeSpan.Zero)
            {
                // Note: At the current setting of approximately 2 ticks per second this timer will
                //       wrap in approximately 2^64/2/60/60/24/365=292,471,208,677.5360162195585996
                //       (nearly 300 billion) years.
                long timeoutTicks = ((timeout.Ticks / TimeSpan.TicksPerMillisecond) >>
                        TransactionTable.timerInternalExponent) + _ticks;
                // The increment of 2 is necessary to account for the half-second that is
                // lost due to the right-shift truncation and also for the half-second
                // that might be lost because the transaction's AbsoluteTimeout is
                // calculated just before this._ticks is incremented.
                // This increment by 2 could cause a transaction to last up to 1 second longer than the
                // specified timeout, but there are no guarantees that the transaction
                // will timeout exactly at the time specified. But we shouldn't timeout
                // the transaction earlier than the specified time, which is possible without
                // this adjustment.
                return timeoutTicks + 2;
            }
            else
            {
                return long.MaxValue;
            }
        }


        // Absolute timeout
        internal TimeSpan RecalcTimeout(InternalTransaction tx)
        {
            return TimeSpan.FromMilliseconds((tx.AbsoluteTimeout - _ticks) * _timerInterval);
        }


        // Creation time
        private long CurrentTime
        {
            get
            {
                if (_timerEnabled)
                {
                    return _lastTimerTime;
                }
                else
                {
                    return DateTime.UtcNow.Ticks;
                }
            }
        }


        // Add a transaction to the table.  Transactions are added to the end of the list in sorted order based on their 
        // absolute timeout.
        internal int Add(InternalTransaction txNew)
        {
            // Tell the runtime that we are modifying global state.
            int readerIndex = 0;

            readerIndex = _rwLock.EnterReadLock();
            try
            {
                // Start the timer if needed before checking the current time since the current
                // time can be more efficient with a running timer.
                if (txNew.AbsoluteTimeout != long.MaxValue)
                {
                    if (!_timerEnabled)
                    {
                        if (!_timer.Change(_timerInterval, _timerInterval))
                        {
                            throw TransactionException.CreateInvalidOperationException(
                                TraceSourceType.TraceSourceLtm,
                                SR.UnexpectedTimerFailure,
                                null
                                );
                        }
                        _lastTimerTime = DateTime.UtcNow.Ticks;
                        _timerEnabled = true;
                    }
                }
                txNew.CreationTime = CurrentTime;

                AddIter(txNew);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            return readerIndex;
        }


        private void AddIter(InternalTransaction txNew)
        {
            //
            // Theory of operation.
            //
            // Note that the head bucket contains any transaction with essentially infinite
            // timeout (long.MaxValue).  The list is sorted in decending order.  To add
            // a node the code must walk down the list looking for a set of bucket that matches
            // the absolute timeout value for the transaction.  When it is found it passes
            // the insert down to that set.
            //
            // An importent thing to note about the list is that forward links are all weak
            // references and reverse links are all strong references.  This allows the GC
            // to clean up old links in the list so that they don't need to be removed manually.
            // However if there is still a rooted strong reference to an old link in the
            // chain that link won't fall off the list because there is a strong reference held
            // forward.
            //

            BucketSet currentBucketSet = _headBucketSet;

            while (currentBucketSet.AbsoluteTimeout != txNew.AbsoluteTimeout)
            {
                BucketSet lastBucketSet = null;
                do
                {
                    WeakReference nextSetWeak = (WeakReference)currentBucketSet.nextSetWeak;
                    BucketSet nextBucketSet = null;
                    if (nextSetWeak != null)
                    {
                        nextBucketSet = (BucketSet)nextSetWeak.Target;
                    }

                    if (nextBucketSet == null)
                    {
                        //
                        // We've reached the end of the list either because nextSetWeak was null or
                        // because its reference was collected.  This code doesn't care.  Make a new
                        // set, attempt to attach it and move on.
                        //
                        BucketSet newBucketSet = new BucketSet(this, txNew.AbsoluteTimeout);
                        WeakReference newSetWeak = new WeakReference(newBucketSet);

                        WeakReference oldNextSetWeak = (WeakReference)Interlocked.CompareExchange(
                            ref currentBucketSet.nextSetWeak, newSetWeak, nextSetWeak);
                        if (oldNextSetWeak == nextSetWeak)
                        {
                            // Ladies and Gentlemen we have a winner.
                            newBucketSet.prevSet = currentBucketSet;
                        }

                        // Note that at this point we don't update currentBucketSet.  On the next loop
                        // iteration we should be able to pick up where we left off.
                    }
                    else
                    {
                        lastBucketSet = currentBucketSet;
                        currentBucketSet = nextBucketSet;
                    }
                }
                while (currentBucketSet.AbsoluteTimeout > txNew.AbsoluteTimeout);

                if (currentBucketSet.AbsoluteTimeout != txNew.AbsoluteTimeout)
                {
                    //
                    // Getting to here means that we've found a slot in the list where this bucket set should go.
                    //
                    BucketSet newBucketSet = new BucketSet(this, txNew.AbsoluteTimeout);
                    WeakReference newSetWeak = new WeakReference(newBucketSet);

                    newBucketSet.nextSetWeak = lastBucketSet.nextSetWeak;
                    WeakReference oldNextSetWeak = (WeakReference)Interlocked.CompareExchange(
                        ref lastBucketSet.nextSetWeak, newSetWeak, newBucketSet.nextSetWeak);
                    if (oldNextSetWeak == newBucketSet.nextSetWeak)
                    {
                        // Ladies and Gentlemen we have a winner.
                        if (oldNextSetWeak != null)
                        {
                            BucketSet oldSet = (BucketSet)oldNextSetWeak.Target;
                            if (oldSet != null)
                            {
                                // prev references are just there to root things for the GC.  If this object is 
                                // gone we don't really care.
                                oldSet.prevSet = newBucketSet;
                            }
                        }
                        newBucketSet.prevSet = lastBucketSet;
                    }

                    // Special note - We are going to loop back to the BucketSet that preceeds the one we just tried
                    // to insert because we may have lost the race to insert our new BucketSet into the list to another
                    // "Add" thread. By looping back, we check again to see if the BucketSet we just created actually
                    // got added. If it did, we will exit out of the outer loop and add the transaction. But if we
                    // lost the race, we will again try to add a new BucketSet. In the latter case, the BucketSet
                    // we created during the first iteration will simply be Garbage Collected because there are no
                    // strong references to it since we never added the transaction to a bucket and the act of
                    // creating the second BucketSet with remove the backward reference that was created in the
                    // first trip thru the loop.
                    currentBucketSet = lastBucketSet;
                    lastBucketSet = null;

                    // The outer loop will iterate and pick up where we left off.
                }
            }

            //
            // Great we found a spot.
            //
            currentBucketSet.Add(txNew);
        }


        // Remove a transaction from the table.
        internal void Remove(InternalTransaction tx)
        {
            tx._tableBucket.Remove(tx);
            tx._tableBucket = null;
        }


        // Process a timer event
        private void ThreadTimer(object state)
        {
            //
            // Theory of operation.
            //
            // To timeout transactions we must walk down the list starting from the head
            // until we find a link with an absolute timeout that is greater than our own.
            // At that point everything further down in the list is elegable to be timed
            // out.  So simply remove that link in the list and walk down from that point
            // timing out any transaction that is found.
            //

            // There could be a race between this callback being queued and the timer
            // being disabled.  If we get here when the timer is disabled, just return.
            if (!_timerEnabled)
            {
                return;
            }

            // Increment the number of ticks
            _ticks++;
            _lastTimerTime = DateTime.UtcNow.Ticks;

            //
            // First find the starting point of transactions that should time out.  Every transaction after
            // that point will timeout so once we've found it then it is just a matter of traversing the
            // structure.
            //
            BucketSet lastBucketSet = null;
            BucketSet currentBucketSet = _headBucketSet; // The list always has a head.

            // Acquire a writer lock before checking to see if we should disable the timer.
            // Adding of transactions acquires a reader lock and might insert a new BucketSet.
            // If that races with our check for a BucketSet existing, we may not timeout that
            // transaction that is being added.
            WeakReference nextWeakSet = null;
            BucketSet nextBucketSet = null;

            nextWeakSet = (WeakReference)currentBucketSet.nextSetWeak;
            if (nextWeakSet != null)
            {
                nextBucketSet = (BucketSet)nextWeakSet.Target;
            }

            if (nextBucketSet == null)
            {
                _rwLock.EnterWriteLock();
                try
                {
                    // Access the nextBucketSet again in writer lock to account for any race before disabling the timeout. 
                    nextWeakSet = (WeakReference)currentBucketSet.nextSetWeak;
                    if (nextWeakSet != null)
                    {
                        nextBucketSet = (BucketSet)nextWeakSet.Target;
                    }

                    if (nextBucketSet == null)
                    {
                        //
                        // Special case to allow for disabling the timer.
                        //
                        // If there are no transactions on the timeout list we can disable the
                        // timer.
                        if (!_timer.Change(Timeout.Infinite, Timeout.Infinite))
                        {
                            throw TransactionException.CreateInvalidOperationException(
                                TraceSourceType.TraceSourceLtm,
                                SR.UnexpectedTimerFailure,
                                null
                                );
                        }
                        _timerEnabled = false;

                        return;
                    }
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }

            // Note it is slightly subtle that we always skip the head node.  This is done
            // on purpose because the head node contains transactions with essentially 
            // an infinite timeout.
            do
            {
                do
                {
                    nextWeakSet = (WeakReference)currentBucketSet.nextSetWeak;
                    if (nextWeakSet == null)
                    {
                        // Nothing more to do.
                        return;
                    }

                    nextBucketSet = (BucketSet)nextWeakSet.Target;
                    if (nextBucketSet == null)
                    {
                        // Again nothing more to do.
                        return;
                    }
                    lastBucketSet = currentBucketSet;
                    currentBucketSet = nextBucketSet;
                }
                while (currentBucketSet.AbsoluteTimeout > _ticks);

                //
                // Pinch off the list at this point making sure it is still the correct set.
                //
                // Note: We may lose a race with an "Add" thread that is inserting a BucketSet in this location in
                // the list. If that happens, this CompareExchange will not be performed and the returned abortingSetsWeak
                // value will NOT equal nextWeakSet. But we check for that and if this condition occurs, this iteration of
                // the timer thread will simply return, not timing out any transactions. When the next timer interval
                // expires, the thread will walk the list again, find the appropriate BucketSet to pinch off, and
                // then time out the transactions. This means that it is possible for a transaction to live a bit longer,
                // but not much.
                WeakReference abortingSetsWeak =
                    (WeakReference)Interlocked.CompareExchange(ref lastBucketSet.nextSetWeak, null, nextWeakSet);

                if (abortingSetsWeak == nextWeakSet)
                {
                    // Yea - now proceed to abort the transactions.
                    BucketSet abortingBucketSets = null;

                    do
                    {
                        if (abortingSetsWeak != null)
                        {
                            abortingBucketSets = (BucketSet)abortingSetsWeak.Target;
                        }
                        else
                        {
                            abortingBucketSets = null;
                        }
                        if (abortingBucketSets != null)
                        {
                            abortingBucketSets.TimeoutTransactions();
                            abortingSetsWeak = (WeakReference)abortingBucketSets.nextSetWeak;
                        }
                    }
                    while (abortingBucketSets != null);

                    // That's all we needed to do.
                    break;
                }

                // We missed pulling the right transactions off.  Loop back up and try again.
                currentBucketSet = lastBucketSet;
            }
            while (true);
        }
    }


    internal class BucketSet
    {
        // Buckets are kept in sets.  Each element of a set will have the same absoluteTimeout.
        internal object nextSetWeak;
        internal BucketSet prevSet;

        private TransactionTable _table;

        private long _absoluteTimeout;

        internal Bucket headBucket;

        internal BucketSet(TransactionTable table, long absoluteTimeout)
        {
            headBucket = new Bucket(this);
            _table = table;
            _absoluteTimeout = absoluteTimeout;
        }


        internal long AbsoluteTimeout
        {
            get
            {
                return _absoluteTimeout;
            }
        }


        internal void Add(InternalTransaction newTx)
        {
            while (!headBucket.Add(newTx)) ;
        }


        internal void TimeoutTransactions()
        {
            Bucket currentBucket = headBucket;
            // It will always have a head.
            do
            {
                currentBucket.TimeoutTransactions();

                WeakReference nextWeakBucket = (WeakReference)currentBucket.nextBucketWeak;
                if (nextWeakBucket != null)
                {
                    currentBucket = (Bucket)nextWeakBucket.Target;
                }
                else
                {
                    currentBucket = null;
                }
            }
            while (currentBucket != null);
        }
    }


    internal class Bucket
    {
        private bool _timedOut;
        private int _index;
        private int _size;
        private InternalTransaction[] _transactions;
        internal WeakReference nextBucketWeak;
        private Bucket _previous;

        private BucketSet _owningSet;

        internal Bucket(BucketSet owningSet)
        {
            _timedOut = false;
            _index = -1;
            _size = 1024; // A possible design change here is to have this scale dynamically based on load.
            _transactions = new InternalTransaction[_size];
            _owningSet = owningSet;
        }


        internal bool Add(InternalTransaction tx)
        {
            int currentIndex = Interlocked.Increment(ref _index);
            if (currentIndex < _size)
            {
                tx._tableBucket = this;
                tx._bucketIndex = currentIndex;
                Interlocked.MemoryBarrier(); // This data must be written before the transaction 
                                             // could be timed out.
                _transactions[currentIndex] = tx;

                if (_timedOut)
                {
                    lock (tx)
                    {
                        tx.State.Timeout(tx);
                    }
                }
            }
            else
            {
                Bucket newBucket = new Bucket(_owningSet);
                newBucket.nextBucketWeak = new WeakReference(this);

                Bucket oldBucket = Interlocked.CompareExchange(ref _owningSet.headBucket, newBucket, this);
                if (oldBucket == this)
                {
                    // ladies and gentlemen we have a winner.
                    _previous = newBucket;
                }

                return false;
            }
            return true;
        }


        internal void Remove(InternalTransaction tx)
        {
            _transactions[tx._bucketIndex] = null;
        }


        internal void TimeoutTransactions()
        {
            int i;
            int transactionCount = _index;

            _timedOut = true;
            Interlocked.MemoryBarrier();

            for (i = 0; i <= transactionCount && i < _size; i++)
            {
                Debug.Assert(transactionCount == _index, "Index changed timing out transactions");
                InternalTransaction tx = _transactions[i];
                if (tx != null)
                {
                    lock (tx)
                    {
                        tx.State.Timeout(tx);
                    }
                }
            }
        }
    }
}
