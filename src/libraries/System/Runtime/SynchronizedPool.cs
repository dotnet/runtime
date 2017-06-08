//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.Runtime
{
    using System.Collections.Generic;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;

    // A simple synchronized pool would simply lock a stack and push/pop on return/take.
    //
    // This implementation tries to reduce locking by exploiting the case where an item
    // is taken and returned by the same thread, which turns out to be common in our 
    // scenarios.  
    //
    // Initially, all the quota is allocated to a global (non-thread-specific) pool, 
    // which takes locks.  As different threads take and return values, we record their IDs, 
    // and if we detect that a thread is taking and returning "enough" on the same thread, 
    // then we decide to "promote" the thread.  When a thread is promoted, we decrease the 
    // quota of the global pool by one, and allocate a thread-specific entry for the thread 
    // to store it's value.  Once this entry is allocated, the thread can take and return 
    // it's value from that entry without taking any locks.  Not only does this avoid 
    // locks, but it affinitizes pooled items to a particular thread.
    //
    // There are a couple of additional things worth noting:
    // 
    // It is possible for a thread that we have reserved an entry for to exit.  This means
    // we will still have a entry allocated for it, but the pooled item stored there 
    // will never be used.  After a while, we could end up with a number of these, and 
    // as a result we would begin to exhaust the quota of the overall pool.  To mitigate this
    // case, we throw away the entire per-thread pool, and return all the quota back to 
    // the global pool if we are unable to promote a thread (due to lack of space).  Then 
    // the set of active threads will be re-promoted as they take and return items.
    // 
    // You may notice that the code does not immediately promote a thread, and does not
    // immediately throw away the entire per-thread pool when it is unable to promote a 
    // thread.  Instead, it uses counters (based on the number of calls to the pool) 
    // and a threshold to figure out when to do these operations.  In the case where the
    // pool to misconfigured to have too few items for the workload, this avoids constant 
    // promoting and rebuilding of the per thread entries.
    //
    // You may also notice that we do not use interlocked methods when adjusting statistics.
    // Since the statistics are a heuristic as to how often something is happening, they 
    // do not need to be perfect.
    // 
    //[Fx.Tag.SynchronizationObject(Blocking = false)]
    class SynchronizedPool<T> where T : class
    {
        const int maxPendingEntries = 128;
        const int maxPromotionFailures = 64;
        const int maxReturnsBeforePromotion = 64;
        const int maxThreadItemsPerProcessor = 16;
        Entry[] entries;
        GlobalPool globalPool;
        int maxCount;
        PendingEntry[] pending;
        int promotionFailures;

        public SynchronizedPool(int maxCount)
        {
            int threadCount = maxCount;
            int maxThreadCount = maxThreadItemsPerProcessor + SynchronizedPoolHelper.ProcessorCount;
            if (threadCount > maxThreadCount)
            {
                threadCount = maxThreadCount;
            }
            this.maxCount = maxCount;
            this.entries = new Entry[threadCount];
            this.pending = new PendingEntry[4];
            this.globalPool = new GlobalPool(maxCount);
        }

        object ThisLock
        {
            get
            {
                return this;
            }
        }

        public void Clear()
        {
            Entry[] entries = this.entries;

            for (int i = 0; i < entries.Length; i++)
            {
                entries[i].value = null;
            }

            globalPool.Clear();
        }

        void HandlePromotionFailure(int thisThreadID)
        {
            int newPromotionFailures = this.promotionFailures + 1;

            if (newPromotionFailures >= maxPromotionFailures)
            {
                lock (ThisLock)
                {
                    this.entries = new Entry[this.entries.Length];

                    globalPool.MaxCount = maxCount;
                }

                PromoteThread(thisThreadID);
            }
            else
            {
                this.promotionFailures = newPromotionFailures;
            }
        }

        bool PromoteThread(int thisThreadID)
        {
            lock (ThisLock)
            {
                for (int i = 0; i < this.entries.Length; i++)
                {
                    int threadID = this.entries[i].threadID;

                    if (threadID == thisThreadID)
                    {
                        return true;
                    }
                    else if (threadID == 0)
                    {
                        globalPool.DecrementMaxCount();
                        this.entries[i].threadID = thisThreadID;
                        return true;
                    }
                }
            }

            return false;
        }

        void RecordReturnToGlobalPool(int thisThreadID)
        {
            PendingEntry[] localPending = this.pending;

            for (int i = 0; i < localPending.Length; i++)
            {
                int threadID = localPending[i].threadID;

                if (threadID == thisThreadID)
                {
                    int newReturnCount = localPending[i].returnCount + 1;

                    if (newReturnCount >= maxReturnsBeforePromotion)
                    {
                        localPending[i].returnCount = 0;

                        if (!PromoteThread(thisThreadID))
                        {
                            HandlePromotionFailure(thisThreadID);
                        }
                    }
                    else
                    {
                        localPending[i].returnCount = newReturnCount;
                    }
                    break;
                }
                else if (threadID == 0)
                {
                    break;
                }
            }
        }

        void RecordTakeFromGlobalPool(int thisThreadID)
        {
            PendingEntry[] localPending = this.pending;

            for (int i = 0; i < localPending.Length; i++)
            {
                int threadID = localPending[i].threadID;

                if (threadID == thisThreadID)
                {
                    return;
                }
                else if (threadID == 0)
                {
                    lock (localPending)
                    {
                        if (localPending[i].threadID == 0)
                        {
                            localPending[i].threadID = thisThreadID;
                            return;
                        }
                    }
                }
            }

            if (localPending.Length >= maxPendingEntries)
            {
                this.pending = new PendingEntry[localPending.Length];
            }
            else
            {
                PendingEntry[] newPending = new PendingEntry[localPending.Length * 2];
                Array.Copy(localPending, newPending, localPending.Length);
                this.pending = newPending;
            }
        }

        public bool Return(T value)
        {
            int thisThreadID = Thread.CurrentThread.ManagedThreadId;

            if (thisThreadID == 0)
            {
                return false;
            }

            if (ReturnToPerThreadPool(thisThreadID, value))
            {
                return true;
            }

            return ReturnToGlobalPool(thisThreadID, value);
        }

        bool ReturnToPerThreadPool(int thisThreadID, T value)
        {
            Entry[] entries = this.entries;

            for (int i = 0; i < entries.Length; i++)
            {
                int threadID = entries[i].threadID;

                if (threadID == thisThreadID)
                {
                    if (entries[i].value == null)
                    {
                        entries[i].value = value;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (threadID == 0)
                {
                    break;
                }
            }

            return false;
        }

        bool ReturnToGlobalPool(int thisThreadID, T value)
        {
            RecordReturnToGlobalPool(thisThreadID);

            return globalPool.Return(value);
        }

        public T Take()
        {
            int thisThreadID = Thread.CurrentThread.ManagedThreadId;

            if (thisThreadID == 0)
            {
                return null;
            }

            T value = TakeFromPerThreadPool(thisThreadID);

            if (value != null)
            {
                return value;
            }

            return TakeFromGlobalPool(thisThreadID);
        }

        T TakeFromPerThreadPool(int thisThreadID)
        {
            Entry[] entries = this.entries;

            for (int i = 0; i < entries.Length; i++)
            {
                int threadID = entries[i].threadID;

                if (threadID == thisThreadID)
                {
                    T value = entries[i].value;

                    if (value != null)
                    {
                        entries[i].value = null;
                        return value;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (threadID == 0)
                {
                    break;
                }
            }

            return null;
        }

        T TakeFromGlobalPool(int thisThreadID)
        {
            RecordTakeFromGlobalPool(thisThreadID);

            return globalPool.Take();
        }

        struct Entry
        {
            public int threadID;
            public T value;
        }

        struct PendingEntry
        {
            public int returnCount;
            public int threadID;
        }

        static class SynchronizedPoolHelper
        {
            public static readonly int ProcessorCount = GetProcessorCount();

            //[Fx.Tag.SecurityNote(Critical = "Asserts in order to get the processor count from the environment", Safe = "This data isn't actually protected so it's ok to leak")]
            //[SecuritySafeCritical]
            //[EnvironmentPermission(SecurityAction.Assert, Read = "NUMBER_OF_PROCESSORS")]
            static int GetProcessorCount()
            {
                return Environment.ProcessorCount;
            }
        }

        //[Fx.Tag.SynchronizationObject(Blocking = false)]
        class GlobalPool
        {
            Stack<T> items;

            int maxCount;

            public GlobalPool(int maxCount)
            {
                this.items = new Stack<T>();
                this.maxCount = maxCount;
            }

            public int MaxCount
            {
                get
                {
                    return maxCount;
                }
                set
                {
                    lock (ThisLock)
                    {
                        while (items.Count > value)
                        {
                            items.Pop();
                        }
                        maxCount = value;
                    }
                }
            }

            object ThisLock
            {
                get
                {
                    return this;
                }
            }

            public void DecrementMaxCount()
            {
                lock (ThisLock)
                {
                    if (items.Count == maxCount)
                    {
                        items.Pop();
                    }
                    maxCount--;
                }
            }

            public T Take()
            {
                if (this.items.Count > 0)
                {
                    lock (ThisLock)
                    {
                        if (this.items.Count > 0)
                        {
                            return this.items.Pop();
                        }
                    }
                }
                return null;
            }

            public bool Return(T value)
            {
                if (this.items.Count < this.MaxCount)
                {
                    lock (ThisLock)
                    {
                        if (this.items.Count < this.MaxCount)
                        {
                            this.items.Push(value);
                            return true;
                        }
                    }
                }
                return false;
            }

            public void Clear()
            {
                lock (ThisLock)
                {
                    this.items.Clear();
                }
            }
        }
    }
}
