// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace System.Transactions
{
    // This transaction table implementation uses an array of lists to avoid contention.  The list for a
    // transaction is decided by its hashcode.
    internal sealed class TransactionTable
    {
        // Use a timer to initiate looking for transactions that have timed out.
        private readonly Timer _timer;

        // Private storage noting if the timer is enabled.
        private bool _timerEnabled;

        // Store the timer interval
        private const int timerInternalExponent = 9;
        private const int timerInterval = 1 << timerInternalExponent;

        // Store the number of ticks.  A tick is a mark of 1 timer interval.  By counting ticks
        // we can avoid expensive calls to get the current time for every transaction creation.
        private long _ticks;
        private long _lastTimerTime;

        private int _nextTransactionId;

        // Sets of arrays of transactions.
        private readonly BucketSet _headBucketSet;

        // Serializes inserting new bucket sets into the list.
        private readonly Lock _bucketSetLock;

        // Synchronize adding transactions with shutting off the timer and started events.
        private readonly ReaderWriterLockSlim _rwLock;

        internal TransactionTable()
        {
            // Create a timer that is initially disabled by specifing an Infinite time to the first interval
            _timer = new Timer(new TimerCallback(ThreadTimer), null, Timeout.Infinite, timerInterval);

            // Note that the timer is disabled
            _timerEnabled = false;

            // Ticks start off at zero.
            _ticks = 0;

            // The head of the list is long.MaxValue.  It contains all of the transactions that for
            // some reason or other don't have a timeout.
            _headBucketSet = new BucketSet(long.MaxValue);
            _bucketSetLock = new Lock();

            // Allocate the lock
            _rwLock = new ReaderWriterLockSlim();
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
            return TimeSpan.FromMilliseconds((tx.AbsoluteTimeout - _ticks) * timerInterval);
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
            int transactionHash = Interlocked.Increment(ref _nextTransactionId);

            _rwLock.EnterReadLock();

            try
            {
                // Start the timer if needed before checking the current time since the current
                // time can be more efficient with a running timer.
                if (txNew.AbsoluteTimeout != long.MaxValue)
                {
                    if (!_timerEnabled)
                    {
                        if (!_timer.Change(timerInterval, timerInterval))
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

            return transactionHash;
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
            // An important thing to note about the list is that forward links are all weak
            // references and reverse links are all strong references.  This allows the GC
            // to clean up old links in the list so that they don't need to be removed manually.
            // However if there is still a rooted strong reference to an old link in the
            // chain that link won't fall off the list because there is a strong reference held
            // forward.
            //
            // For that to work every set in the list must be held by the set after it.  If a set
            // in the middle of the list is collected, the sets after it can no longer be reached
            // and their transactions never time out.  So new sets are only inserted under
            // _bucketSetLock, which keeps the prevSet links consistent.  Finding an existing set
            // doesn't need the lock.
            //

            BucketSet? bucketSet = FindBucketSet(txNew.AbsoluteTimeout, insert: false);
            if (bucketSet == null)
            {
                lock (_bucketSetLock)
                {
                    bucketSet = FindBucketSet(txNew.AbsoluteTimeout, insert: true);
                }
            }

            Debug.Assert(bucketSet != null);
            bucketSet.Add(txNew);
        }


        // Find the set for the absolute timeout.  If there is none, insert one when insert is true,
        // otherwise return null.  Inserting must be done under _bucketSetLock.
        private BucketSet? FindBucketSet(long absoluteTimeout, bool insert)
        {
            BucketSet currentBucketSet = _headBucketSet;

            while (currentBucketSet.AbsoluteTimeout != absoluteTimeout)
            {
                WeakReference<BucketSet>? nextSetWeak = currentBucketSet.nextSetWeak;
                BucketSet? nextBucketSet = null;
                if (nextSetWeak != null)
                {
                    nextSetWeak.TryGetTarget(out nextBucketSet);
                }

                if (nextBucketSet != null && nextBucketSet.AbsoluteTimeout >= absoluteTimeout)
                {
                    currentBucketSet = nextBucketSet;
                    continue;
                }

                //
                // The set belongs between currentBucketSet and nextBucketSet.  nextBucketSet is null
                // if we've reached the end of the list, either because nextSetWeak was null or because
                // its reference was collected.
                //
                if (!insert)
                {
                    return null;
                }

                Debug.Assert(_bucketSetLock.IsHeldByCurrentThread);

                BucketSet newBucketSet = new BucketSet(absoluteTimeout);
                newBucketSet.nextSetWeak = nextBucketSet != null ? nextSetWeak : null;
                newBucketSet.prevSet = currentBucketSet;

                // Other inserts are excluded by the lock, so this can only fail if the timer has
                // pinched off the list here.  In that case start over from the head.
                if (Interlocked.CompareExchange(
                        ref currentBucketSet.nextSetWeak, new WeakReference<BucketSet>(newBucketSet), nextSetWeak) == nextSetWeak)
                {
                    nextBucketSet?.prevSet = newBucketSet;
                    return newBucketSet;
                }

                currentBucketSet = _headBucketSet;
            }

            return currentBucketSet;
        }


        // Remove a transaction from the table.
        internal static void Remove(InternalTransaction tx)
        {
            Debug.Assert(tx._tableBucket != null);
            tx._tableBucket.Remove(tx);
            tx._tableBucket = null;
        }


        // Process a timer event
        private void ThreadTimer(object? state)
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
            BucketSet? lastBucketSet;
            BucketSet currentBucketSet = _headBucketSet; // The list always has a head.

            // Acquire a writer lock before checking to see if we should disable the timer.
            // Adding of transactions acquires a reader lock and might insert a new BucketSet.
            // If that races with our check for a BucketSet existing, we may not timeout that
            // transaction that is being added.
            WeakReference<BucketSet>? nextWeakSet = currentBucketSet.nextSetWeak;
            BucketSet? nextBucketSet = null;

            if (nextWeakSet != null)
            {
                nextWeakSet.TryGetTarget(out nextBucketSet);
            }

            if (nextBucketSet == null)
            {
                _rwLock.EnterWriteLock();
                try
                {
                    // Access the nextBucketSet again in writer lock to account for any race before disabling the timeout.
                    nextWeakSet = currentBucketSet.nextSetWeak;
                    if (nextWeakSet != null)
                    {
                        nextWeakSet.TryGetTarget(out nextBucketSet);
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
                    nextWeakSet = currentBucketSet.nextSetWeak;
                    if (nextWeakSet == null)
                    {
                        // Nothing more to do.
                        return;
                    }

                    if (!nextWeakSet.TryGetTarget(out nextBucketSet))
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
                WeakReference<BucketSet>? abortingSetsWeak =
                    Interlocked.CompareExchange(ref lastBucketSet.nextSetWeak, null, nextWeakSet);

                if (abortingSetsWeak == nextWeakSet)
                {
                    // Yea - now proceed to abort the transactions.
                    BucketSet? abortingBucketSets;

                    do
                    {
                        if (abortingSetsWeak != null)
                        {
                            abortingSetsWeak.TryGetTarget(out abortingBucketSets);
                        }
                        else
                        {
                            abortingBucketSets = null;
                        }
                        if (abortingBucketSets != null)
                        {
                            abortingBucketSets.TimeoutTransactions();
                            abortingSetsWeak = abortingBucketSets.nextSetWeak;
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


    internal sealed class BucketSet
    {
        // Buckets are kept in sets.  Each element of a set will have the same absoluteTimeout.
        internal WeakReference<BucketSet>? nextSetWeak;
        internal BucketSet? prevSet;

        private readonly long _absoluteTimeout;

        internal Bucket headBucket;

        internal BucketSet(long absoluteTimeout)
        {
            headBucket = new Bucket(this);
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
            Bucket? currentBucket = headBucket;
            // It will always have a head.
            do
            {
                currentBucket.TimeoutTransactions();

                WeakReference<Bucket>? nextWeakBucket = currentBucket.nextBucketWeak;
                if (nextWeakBucket != null)
                {
                    nextWeakBucket.TryGetTarget(out currentBucket);
                }
                else
                {
                    currentBucket = null;
                }
            }
            while (currentBucket != null);
        }
    }


    internal sealed class Bucket
    {
        private const int size = 1024; // A possible design change here is to have this scale dynamically based on load.
        private bool _timedOut;
        private int _index;
        private readonly InternalTransaction?[] _transactions;
        internal WeakReference<Bucket>? nextBucketWeak;
        private Bucket? _previous;

        private readonly BucketSet _owningSet;

        internal Bucket(BucketSet owningSet)
        {
            _timedOut = false;
            _index = -1;
            _transactions = new InternalTransaction[size];
            _owningSet = owningSet;
        }


        internal bool Add(InternalTransaction tx)
        {
            int currentIndex = Interlocked.Increment(ref _index);
            if (currentIndex < size)
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
                        tx.State!.Timeout(tx);
                    }
                }
            }
            else
            {
                Bucket newBucket = new Bucket(_owningSet);
                newBucket.nextBucketWeak = new WeakReference<Bucket>(this);

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

            for (i = 0; i <= transactionCount && i < size; i++)
            {
                Debug.Assert(transactionCount == _index, "Index changed timing out transactions");
                InternalTransaction? tx = _transactions[i];
                if (tx != null)
                {
                    lock (tx)
                    {
                        tx.State!.Timeout(tx);
                    }
                }
            }
        }
    }
}
