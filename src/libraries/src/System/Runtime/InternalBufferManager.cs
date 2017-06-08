//----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------------------
namespace System.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Threading;


#if DEBUG
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Globalization;
    using System.Security;
    using System.Security.Permissions;
#endif //DEBUG

    abstract class InternalBufferManager
    {
        protected InternalBufferManager()
        {
        }

        public abstract byte[] TakeBuffer(int bufferSize);
        public abstract void ReturnBuffer(byte[] buffer);
        public abstract void Clear();

        public static InternalBufferManager Create(long maxBufferPoolSize, int maxBufferSize)
        {
            if (maxBufferPoolSize == 0)
            {
                return GCBufferManager.Value;
            }
            else
            {
                //Fx.Assert(maxBufferPoolSize > 0 && maxBufferSize >= 0, "bad params, caller should verify");
                return new PooledBufferManager(maxBufferPoolSize, maxBufferSize);
            }
        }

        class PooledBufferManager : InternalBufferManager
        {
            const int minBufferSize = 128;
            const int maxMissesBeforeTuning = 8;
            const int initialBufferCount = 1;
            readonly object tuningLock;

            int[] bufferSizes;
            BufferPool[] bufferPools;
            long memoryLimit;
            long remainingMemory;
            bool areQuotasBeingTuned;
            int totalMisses;
#if DEBUG
            ConcurrentDictionary<int, string> buffersPooled = new ConcurrentDictionary<int, string>();
#endif //DEBUG

            public PooledBufferManager(long maxMemoryToPool, int maxBufferSize)
            {
                this.tuningLock = new object();
                this.memoryLimit = maxMemoryToPool;
                this.remainingMemory = maxMemoryToPool;
                List<BufferPool> bufferPoolList = new List<BufferPool>();

                for (int bufferSize = minBufferSize; ;)
                {
                    long bufferCountLong = this.remainingMemory / bufferSize;

                    int bufferCount = bufferCountLong > int.MaxValue ? int.MaxValue : (int)bufferCountLong;

                    if (bufferCount > initialBufferCount)
                    {
                        bufferCount = initialBufferCount;
                    }

                    bufferPoolList.Add(BufferPool.CreatePool(bufferSize, bufferCount));

                    this.remainingMemory -= (long)bufferCount * bufferSize;

                    if (bufferSize >= maxBufferSize)
                    {
                        break;
                    }

                    long newBufferSizeLong = (long)bufferSize * 2;

                    if (newBufferSizeLong > (long)maxBufferSize)
                    {
                        bufferSize = maxBufferSize;
                    }
                    else
                    {
                        bufferSize = (int)newBufferSizeLong;
                    }
                }

                this.bufferPools = bufferPoolList.ToArray();
                this.bufferSizes = new int[bufferPools.Length];
                for (int i = 0; i < bufferPools.Length; i++)
                {
                    this.bufferSizes[i] = bufferPools[i].BufferSize;
                }
            }

            public override void Clear()
            {
#if DEBUG
                this.buffersPooled.Clear();
#endif //DEBUG

                for (int i = 0; i < this.bufferPools.Length; i++)
                {
                    BufferPool bufferPool = this.bufferPools[i];
                    bufferPool.Clear();
                }
            }

            void ChangeQuota(ref BufferPool bufferPool, int delta)
            {

                //if (TraceCore.BufferPoolChangeQuotaIsEnabled(Fx.Trace))
                //{
                //    TraceCore.BufferPoolChangeQuota(Fx.Trace, bufferPool.BufferSize, delta);
                //}

                BufferPool oldBufferPool = bufferPool;
                int newLimit = oldBufferPool.Limit + delta;
                BufferPool newBufferPool = BufferPool.CreatePool(oldBufferPool.BufferSize, newLimit);
                for (int i = 0; i < newLimit; i++)
                {
                    byte[] buffer = oldBufferPool.Take();
                    if (buffer == null)
                    {
                        break;
                    }
                    newBufferPool.Return(buffer);
                    newBufferPool.IncrementCount();
                }
                this.remainingMemory -= oldBufferPool.BufferSize * delta;
                bufferPool = newBufferPool;
            }

            void DecreaseQuota(ref BufferPool bufferPool)
            {
                ChangeQuota(ref bufferPool, -1);
            }

            int FindMostExcessivePool()
            {
                long maxBytesInExcess = 0;
                int index = -1;

                for (int i = 0; i < this.bufferPools.Length; i++)
                {
                    BufferPool bufferPool = this.bufferPools[i];

                    if (bufferPool.Peak < bufferPool.Limit)
                    {
                        long bytesInExcess = (bufferPool.Limit - bufferPool.Peak) * (long)bufferPool.BufferSize;

                        if (bytesInExcess > maxBytesInExcess)
                        {
                            index = i;
                            maxBytesInExcess = bytesInExcess;
                        }
                    }
                }

                return index;
            }

            int FindMostStarvedPool()
            {
                long maxBytesMissed = 0;
                int index = -1;

                for (int i = 0; i < this.bufferPools.Length; i++)
                {
                    BufferPool bufferPool = this.bufferPools[i];

                    if (bufferPool.Peak == bufferPool.Limit)
                    {
                        long bytesMissed = bufferPool.Misses * (long)bufferPool.BufferSize;

                        if (bytesMissed > maxBytesMissed)
                        {
                            index = i;
                            maxBytesMissed = bytesMissed;
                        }
                    }
                }

                return index;
            }

            BufferPool FindPool(int desiredBufferSize)
            {
                for (int i = 0; i < this.bufferSizes.Length; i++)
                {
                    if (desiredBufferSize <= this.bufferSizes[i])
                    {
                        return this.bufferPools[i];
                    }
                }

                return null;
            }

            void IncreaseQuota(ref BufferPool bufferPool)
            {
                ChangeQuota(ref bufferPool, 1);
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                //Fx.Assert(buffer != null, "caller must verify");

#if DEBUG
                //int hash = buffer.GetHashCode();
                //if (!this.buffersPooled.TryAdd(hash, CaptureStackTrace()))
                //{
                //    string originalStack;
                //    if (!this.buffersPooled.TryGetValue(hash, out originalStack))
                //    {
                //        originalStack = "NULL";
                //    }

                //    //Fx.Assert(
                //        //string.Format(
                //        //    CultureInfo.InvariantCulture,
                //        //    "Buffer '{0}' has already been returned to the bufferManager before. Previous CallStack: {1} Current CallStack: {2}",
                //        //    hash,
                //        //    originalStack,
                //        //    CaptureStackTrace()));

                //}
#endif //DEBUG

                BufferPool bufferPool = FindPool(buffer.Length);
                if (bufferPool != null)
                {
                    if (buffer.Length != bufferPool.BufferSize)
                    {
                        throw new ArgumentException(); //Fx.Exception.Argument("buffer", InternalSR.BufferIsNotRightSizeForBufferManager);
                    }

                    if (bufferPool.Return(buffer))
                    {
                        bufferPool.IncrementCount();
                    }
                }
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                //Fx.Assert(bufferSize >= 0, "caller must ensure a non-negative argument");

                BufferPool bufferPool = FindPool(bufferSize);
                byte[] returnValue;
                if (bufferPool != null)
                {
                    byte[] buffer = bufferPool.Take();
                    if (buffer != null)
                    {
                        bufferPool.DecrementCount();
                        returnValue = buffer;
                    }
                    else
                    {
                        if (bufferPool.Peak == bufferPool.Limit)
                        {
                            bufferPool.Misses++;
                            if (++totalMisses >= maxMissesBeforeTuning)
                            {
                                TuneQuotas();
                            }
                        }

                        //if (TraceCore.BufferPoolAllocationIsEnabled(Fx.Trace))
                        //{
                        //    TraceCore.BufferPoolAllocation(Fx.Trace, bufferPool.BufferSize);
                        //}

                        returnValue = new Byte[bufferSize]; //Fx.AllocateByteArray(bufferPool.BufferSize);
                    }
                }
                else
                {
                    //if (TraceCore.BufferPoolAllocationIsEnabled(Fx.Trace))
                    //{
                    //    TraceCore.BufferPoolAllocation(Fx.Trace, bufferSize);
                    //}

                    returnValue = new Byte[bufferSize]; //Fx.AllocateByteArray(bufferSize);
                }

#if DEBUG
                string dummy;
                this.buffersPooled.TryRemove(returnValue.GetHashCode(), out dummy);
#endif //DEBUG

                return returnValue;
            }

#if DEBUG
            //[SecuritySafeCritical]
            //[PermissionSet(SecurityAction.Assert, Unrestricted = true)]
            //private static string CaptureStackTrace()
            //{
            //    return new StackTrace(true).ToString();
            //}
#endif //DEBUG

            void TuneQuotas()
            {
                if (this.areQuotasBeingTuned)
                {
                    return;
                }

                bool lockHeld = false;
                try
                {
                    Monitor.TryEnter(this.tuningLock, ref lockHeld);

                    // Don't bother if another thread already has the lock
                    if (!lockHeld || this.areQuotasBeingTuned)
                    {
                        return;
                    }

                    this.areQuotasBeingTuned = true;
                }
                finally
                {
                    if (lockHeld)
                    {
                        Monitor.Exit(this.tuningLock);
                    }
                }

                // find the "poorest" pool
                int starvedIndex = FindMostStarvedPool();
                if (starvedIndex >= 0)
                {
                    BufferPool starvedBufferPool = this.bufferPools[starvedIndex];

                    if (this.remainingMemory < starvedBufferPool.BufferSize)
                    {
                        // find the "richest" pool
                        int excessiveIndex = FindMostExcessivePool();
                        if (excessiveIndex >= 0)
                        {
                            // steal from the richest
                            DecreaseQuota(ref this.bufferPools[excessiveIndex]);
                        }
                    }

                    if (this.remainingMemory >= starvedBufferPool.BufferSize)
                    {
                        // give to the poorest
                        IncreaseQuota(ref this.bufferPools[starvedIndex]);
                    }
                }

                // reset statistics
                for (int i = 0; i < this.bufferPools.Length; i++)
                {
                    BufferPool bufferPool = this.bufferPools[i];
                    bufferPool.Misses = 0;
                }

                this.totalMisses = 0;
                this.areQuotasBeingTuned = false;
            }

            abstract class BufferPool
            {
                int bufferSize;
                int count;
                int limit;
                int misses;
                int peak;

                public BufferPool(int bufferSize, int limit)
                {
                    this.bufferSize = bufferSize;
                    this.limit = limit;
                }

                public int BufferSize
                {
                    get { return this.bufferSize; }
                }

                public int Limit
                {
                    get { return this.limit; }
                }

                public int Misses
                {
                    get { return this.misses; }
                    set { this.misses = value; }
                }

                public int Peak
                {
                    get { return this.peak; }
                }

                public void Clear()
                {
                    this.OnClear();
                    this.count = 0;
                }

                public void DecrementCount()
                {
                    int newValue = this.count - 1;
                    if (newValue >= 0)
                    {
                        this.count = newValue;
                    }
                }

                public void IncrementCount()
                {
                    int newValue = this.count + 1;
                    if (newValue <= this.limit)
                    {
                        this.count = newValue;
                        if (newValue > this.peak)
                        {
                            this.peak = newValue;
                        }
                    }
                }

                internal abstract byte[] Take();
                internal abstract bool Return(byte[] buffer);
                internal abstract void OnClear();

                internal static BufferPool CreatePool(int bufferSize, int limit)
                {
                    // To avoid many buffer drops during training of large objects which
                    // get allocated on the LOH, we use the LargeBufferPool and for 
                    // bufferSize < 85000, the SynchronizedPool. However if bufferSize < 85000
                    // and (bufferSize + array-overhead) > 85000, this would still use 
                    // the SynchronizedPool even though it is allocated on the LOH.
                    if (bufferSize < 85000)
                    {
                        return new SynchronizedBufferPool(bufferSize, limit);
                    }
                    else
                    {
                        return new LargeBufferPool(bufferSize, limit);
                    }
                }

                class SynchronizedBufferPool : BufferPool
                {
                    SynchronizedPool<byte[]> innerPool;

                    internal SynchronizedBufferPool(int bufferSize, int limit)
                        : base(bufferSize, limit)
                    {
                        this.innerPool = new SynchronizedPool<byte[]>(limit);
                    }

                    internal override void OnClear()
                    {
                        this.innerPool.Clear();
                    }

                    internal override byte[] Take()
                    {
                        return this.innerPool.Take();
                    }

                    internal override bool Return(byte[] buffer)
                    {
                        return this.innerPool.Return(buffer);
                    }
                }

                class LargeBufferPool : BufferPool
                {
                    Stack<byte[]> items;

                    internal LargeBufferPool(int bufferSize, int limit)
                        : base(bufferSize, limit)
                    {
                        this.items = new Stack<byte[]>(limit);
                    }

                    object ThisLock
                    {
                        get
                        {
                            return this.items;
                        }
                    }

                    internal override void OnClear()
                    {
                        lock (ThisLock)
                        {
                            this.items.Clear();
                        }
                    }

                    internal override byte[] Take()
                    {
                        lock (ThisLock)
                        {
                            if (this.items.Count > 0)
                            {
                                return this.items.Pop();
                            }
                        }

                        return null;
                    }

                    internal override bool Return(byte[] buffer)
                    {
                        lock (ThisLock)
                        {
                            if (this.items.Count < this.Limit)
                            {
                                this.items.Push(buffer);
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }
        }

        class GCBufferManager : InternalBufferManager
        {
            static GCBufferManager value = new GCBufferManager();

            GCBufferManager()
            {
            }

            public static GCBufferManager Value
            {
                get { return value; }
            }

            public override void Clear()
            {
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                return new Byte[bufferSize];// Fx.AllocateByteArray(bufferSize);
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                // do nothing, GC will reclaim this buffer
            }
        }
    }
}
