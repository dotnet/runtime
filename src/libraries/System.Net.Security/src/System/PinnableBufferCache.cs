// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    internal sealed class PinnableBufferCache
    {
        /// <summary>
        /// Create a new cache for pinned byte[] buffers.
        /// </summary>
        /// <param name="cacheName">A name used in diagnostic messages</param>
        /// <param name="numberOfElements">The size of byte[] buffers in the cache (they are all the same size)</param>
        public PinnableBufferCache(string cacheName, int numberOfElements) : this(cacheName, () => new byte[numberOfElements]) { }

        /// <summary>
        /// Get a buffer from the buffer manager.  If no buffers exist, allocate a new one.
        /// </summary>
        public byte[] AllocateBuffer() { return (byte[])Allocate(); }

        /// <summary>
        /// Return a buffer back to the buffer manager.
        /// </summary>
        public void FreeBuffer(byte[] buffer) { Free(buffer); }

        /// <summary>
        /// Create a PinnableBufferCache that works on any object (it is intended for OverlappedData)
        /// </summary>
        internal PinnableBufferCache(string cacheName, Func<object> factory)
        {
            _notGen2 = new List<object>(DefaultNumberOfBuffers);
            _factory = factory;

            PinnableBufferCacheEventSource.Log.Create(cacheName);
            _cacheName = cacheName;
        }

        /// <summary>
        /// Get a object from the buffer manager.  If no buffers exist, allocate a new one.
        /// </summary>
        [System.Security.SecuritySafeCritical]
        internal object Allocate()
        {
            // Fast path, get it from our Gen2 aged _freeList.  
            object returnBuffer;
            if (!_freeList.TryPop(out returnBuffer))
            {
                Restock(out returnBuffer);
            }

            // Computing free count is expensive enough that we don't want to compute it unless logging is on.
            if (PinnableBufferCacheEventSource.Log.IsEnabled())
            {
                int numAllocCalls = Interlocked.Increment(ref _numAllocCalls);
                if (numAllocCalls >= 1024)
                {
                    lock (this)
                    {
                        int previousNumAllocCalls = Interlocked.Exchange(ref _numAllocCalls, 0);
                        if (previousNumAllocCalls >= 1024)
                        {
                            int nonGen2Count = 0;
                            foreach (object o in _freeList)
                            {
                                if (GC.GetGeneration(o) < GC.MaxGeneration)
                                {
                                    nonGen2Count++;
                                }
                            }

                            PinnableBufferCacheEventSource.Log.WalkFreeListResult(_cacheName, _freeList.Count, nonGen2Count);
                        }
                    }
                }

                PinnableBufferCacheEventSource.Log.AllocateBuffer(_cacheName, PinnableBufferCacheEventSource.AddressOf(returnBuffer), returnBuffer.GetHashCode(), GC.GetGeneration(returnBuffer), _freeList.Count);
            }
            return returnBuffer;
        }

        /// <summary>
        /// Return a buffer back to the buffer manager.
        /// </summary>
        [System.Security.SecuritySafeCritical]
        internal void Free(object buffer)
        {
            if (PinnableBufferCacheEventSource.Log.IsEnabled())
            {
                PinnableBufferCacheEventSource.Log.FreeBuffer(_cacheName, PinnableBufferCacheEventSource.AddressOf(buffer), buffer.GetHashCode(), _freeList.Count);
            }


            // After we've done 3 gen1 GCs, assume that all buffers have aged into gen2 on the free path.
            if ((_gen1CountAtLastRestock + 3) > GC.CollectionCount(GC.MaxGeneration - 1))
            {
                lock (this)
                {
                    if (GC.GetGeneration(buffer) < GC.MaxGeneration)
                    {
                        // The buffer is not aged, so put it in the non-aged free list.
                        _moreThanFreeListNeeded = true;
                        PinnableBufferCacheEventSource.Log.FreeBufferStillTooYoung(_cacheName, _notGen2.Count);
                        _notGen2.Add(buffer);
                        _gen1CountAtLastRestock = GC.CollectionCount(GC.MaxGeneration - 1);
                        return;
                    }
                }
            }

            // If we discovered that it is indeed Gen2, great, put it in the Gen2 list.  
            _freeList.Push(buffer);
        }

        #region Private

        /// <summary>
        /// Called when we don't have any buffers in our free list to give out.    
        /// </summary>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]
        private void Restock(out object returnBuffer)
        {
            lock (this)
            {
                // Try again after getting the lock as another thread could have just filled the free list.  If we don't check
                // then we unnecessarily grab a new set of buffers because we think we are out.     
                if (_freeList.TryPop(out returnBuffer))
                {
                    return;
                }

                // Lazy init, Ask that TrimFreeListIfNeeded be called on every Gen 2 GC.  
                if (_restockSize == 0)
                {
                    Gen2GcCallback.Register(Gen2GcCallbackFunc, this);
                }

                // Indicate to the trimming policy that the free list is insufficient.   
                _moreThanFreeListNeeded = true;
                PinnableBufferCacheEventSource.Log.AllocateBufferFreeListEmpty(_cacheName, _notGen2.Count);

                // Get more buffers if needed.
                if (_notGen2.Count == 0)
                {
                    CreateNewBuffers();
                }

                // We have no buffers in the aged freelist, so get one from the newer list.   Try to pick the best one.
                // Debug.Assert(_notGen2.Count != 0);
                int idx = _notGen2.Count - 1;
                if (GC.GetGeneration(_notGen2[idx]) < GC.MaxGeneration && GC.GetGeneration(_notGen2[0]) == GC.MaxGeneration)
                {
                    idx = 0;
                }

                returnBuffer = _notGen2[idx];
                _notGen2.RemoveAt(idx);

                // Remember any sub-optimal buffer so we don't put it on the free list when it gets freed.   
                if (PinnableBufferCacheEventSource.Log.IsEnabled() && GC.GetGeneration(returnBuffer) < GC.MaxGeneration)
                {
                    PinnableBufferCacheEventSource.Log.AllocateBufferFromNotGen2(_cacheName, _notGen2.Count);
                }

                // If we have a Gen1 collection, then everything on _notGen2 should have aged.  Move them to the _freeList.  
                if (!AgePendingBuffers())
                {
                    // Before we could age at set of buffers, we have handed out half of them.
                    // This implies we should be proactive about allocating more (since we will trim them if we over-allocate).  
                    if (_notGen2.Count == _restockSize / 2)
                    {
                        PinnableBufferCacheEventSource.Log.DebugMessage("Proactively adding more buffers to aging pool");
                        CreateNewBuffers();
                    }
                }
            }
        }

        /// <summary>
        /// See if we can promote the buffers to the free list.  Returns true if successful. 
        /// </summary>
        [System.Security.SecuritySafeCritical]
        private bool AgePendingBuffers()
        {
            if (_gen1CountAtLastRestock < GC.CollectionCount(GC.MaxGeneration - 1))
            {
                // Allocate a temp list of buffers that are not actually in gen2, and swap it in once
                // we're done scanning all buffers.
                int promotedCount = 0;
                List<object> notInGen2 = new List<object>();
                PinnableBufferCacheEventSource.Log.AllocateBufferAged(_cacheName, _notGen2.Count);
                for (int i = 0; i < _notGen2.Count; i++)
                {
                    // We actually check every object to ensure that we aren't putting non-aged buffers into the free list.
                    object currentBuffer = _notGen2[i];
                    if (GC.GetGeneration(currentBuffer) >= GC.MaxGeneration)
                    {
                        _freeList.Push(currentBuffer);
                        promotedCount++;
                    }
                    else
                    {
                        notInGen2.Add(currentBuffer);
                    }
                }
                PinnableBufferCacheEventSource.Log.AgePendingBuffersResults(_cacheName, promotedCount, notInGen2.Count);
                _notGen2 = notInGen2;

                return true;
            }
            return false;
        }

        /// <summary>
        /// Generates some buffers to age into Gen2.
        /// </summary>
        private void CreateNewBuffers()
        {
            // We choose a very modest number of buffers initially because for the client case.  This is often enough.
            if (_restockSize == 0)
            {
                _restockSize = 4;
            }
            else if (_restockSize < DefaultNumberOfBuffers)
            {
                _restockSize = DefaultNumberOfBuffers;
            }
            else if (_restockSize < 256)
            {
                _restockSize = _restockSize * 2;                // Grow quickly at small sizes
            }
            else if (_restockSize < 4096)
            {
                _restockSize = _restockSize * 3 / 2;            // Less aggressively at large ones
            }
            else
            {
                _restockSize = 4096;                             // Cap how aggressive we are
            }

            // Ensure we hit our minimums
            if (_minBufferCount > _buffersUnderManagement)
                _restockSize = Math.Max(_restockSize, _minBufferCount - _buffersUnderManagement);

            PinnableBufferCacheEventSource.Log.AllocateBufferCreatingNewBuffers(_cacheName, _buffersUnderManagement, _restockSize);
            for (int i = 0; i < _restockSize; i++)
            {
                // Make a new buffer.
                object newBuffer = _factory();

                // Create space between the objects.  We do this because otherwise it forms a single plug (group of objects)
                // and the GC pins the entire plug making them NOT move to Gen1 and Gen2.   by putting space between them
                // we ensure that object get a chance to move independently (even if some are pinned).  
                var dummyObject = new object();
                _notGen2.Add(newBuffer);
            }
            _buffersUnderManagement += _restockSize;
            _gen1CountAtLastRestock = GC.CollectionCount(GC.MaxGeneration - 1);
        }

        /// <summary>
        /// This is the static function that is called from the gen2 GC callback.
        /// The input object is the cache itself.
        /// NOTE: The reason that we make this function static and take the cache as a parameter is that
        /// otherwise, we root the cache to the Gen2GcCallback object, and leak the cache even when
        /// the application no longer needs it.
        /// </summary>
        [System.Security.SecuritySafeCritical]
        private static bool Gen2GcCallbackFunc(object targetObj)
        {
            return ((PinnableBufferCache)(targetObj)).TrimFreeListIfNeeded();
        }

        /// <summary>
        /// This is called on every gen2 GC to see if we need to trim the free list.
        /// NOTE: DO NOT CALL THIS DIRECTLY FROM THE GEN2GCCALLBACK.  INSTEAD CALL IT VIA A STATIC FUNCTION (SEE ABOVE).
        /// If you register a non-static function as a callback, then this object will be leaked.
        /// </summary>
        [System.Security.SecuritySafeCritical]
        private bool TrimFreeListIfNeeded()
        {
            int curMSec = Environment.TickCount;
            int deltaMSec = curMSec - _msecNoUseBeyondFreeListSinceThisTime;
            PinnableBufferCacheEventSource.Log.TrimCheck(_cacheName, _buffersUnderManagement, _moreThanFreeListNeeded, deltaMSec);

            // If we needed more than just the set of aged buffers since the last time we were called,
            // we obviously should not be trimming any memory, so do nothing except reset the flag 
            if (_moreThanFreeListNeeded)
            {
                _moreThanFreeListNeeded = false;
                _trimmingExperimentInProgress = false;
                _msecNoUseBeyondFreeListSinceThisTime = curMSec;
                return true;
            }

            // We require a minimum amount of clock time to pass  (10 seconds) before we trim.  Ideally this time
            // is larger than the typical buffer hold time.  
            if (0 <= deltaMSec && deltaMSec < 10000)
            {
                return true;
            }

            // If we got here we have spend the last few second without needing to lengthen the free list.   Thus
            // we have 'enough' buffers, but maybe we have too many. 
            // See if we can trim
            lock (this)
            {
                // Hit a race, try again later.  
                if (_moreThanFreeListNeeded)
                {
                    _moreThanFreeListNeeded = false;
                    _trimmingExperimentInProgress = false;
                    _msecNoUseBeyondFreeListSinceThisTime = curMSec;
                    return true;
                }

                var freeCount = _freeList.Count;   // This is expensive to fetch, do it once.

                // If there is something in _notGen2 it was not used for the last few seconds, it is trim-able.  
                if (_notGen2.Count > 0)
                {
                    // If we are not performing an experiment and we have stuff that is waiting to go into the
                    // free list but has not made it there, it could be because the 'slow path' of restocking
                    // has not happened, so force this (which should flush the list) and start over.  
                    if (!_trimmingExperimentInProgress)
                    {
                        PinnableBufferCacheEventSource.Log.TrimFlush(_cacheName, _buffersUnderManagement, freeCount, _notGen2.Count);
                        AgePendingBuffers();
                        _trimmingExperimentInProgress = true;
                        return true;
                    }

                    PinnableBufferCacheEventSource.Log.TrimFree(_cacheName, _buffersUnderManagement, freeCount, _notGen2.Count);
                    _buffersUnderManagement -= _notGen2.Count;

                    // Possibly revise the restocking down.  We don't want to grow aggressively if we are trimming.  
                    var newRestockSize = _buffersUnderManagement / 4;
                    if (newRestockSize < _restockSize)
                    {
                        _restockSize = Math.Max(newRestockSize, DefaultNumberOfBuffers);
                    }

                    _notGen2.Clear();
                    _trimmingExperimentInProgress = false;
                    return true;
                }

                // Set up an experiment where we use 25% less buffers in our free list.   We put them in 
                // _notGen2, and if they are needed they will be put back in the free list again.  
                var trimSize = freeCount / 4 + 1;

                // We are OK with a 15% overhead, do nothing in that case.  
                if (freeCount * 15 <= _buffersUnderManagement || _buffersUnderManagement - trimSize <= _minBufferCount)
                {
                    PinnableBufferCacheEventSource.Log.TrimFreeSizeOK(_cacheName, _buffersUnderManagement, freeCount);
                    return true;
                }

                // Move buffers from the free list back to the non-aged list.  If we don't use them by next time, then we'll consider trimming them.
                PinnableBufferCacheEventSource.Log.TrimExperiment(_cacheName, _buffersUnderManagement, freeCount, trimSize);
                object buffer;
                for (int i = 0; i < trimSize; i++)
                {
                    if (_freeList.TryPop(out buffer))
                    {
                        _notGen2.Add(buffer);
                    }
                }
                _msecNoUseBeyondFreeListSinceThisTime = curMSec;
                _trimmingExperimentInProgress = true;
            }

            // Indicate that we want to be called back on the next Gen 2 GC.  
            return true;
        }

        private const int DefaultNumberOfBuffers = 16;
        private string _cacheName;
        private Func<object> _factory;

        /// <summary>
        /// Contains 'good' buffers to reuse.  They are guaranteed to be Gen 2 ENFORCED!
        /// </summary>
        private ConcurrentStack<object> _freeList = new ConcurrentStack<object>();
        /// <summary>
        /// Contains buffers that are not gen 2 and thus we do not wish to give out unless we have to.
        /// To implement trimming we sometimes put aged buffers in here as a place to 'park' them
        /// before true deletion.  
        /// </summary>
        private List<object> _notGen2;
        /// <summary>
        /// What whas the gen 1 count the last time re restocked?  If it is now greater, then
        /// we know that all objects are in Gen 2 so we don't have to check.  Should be updated
        /// every time something gets added to the _notGen2 list.
        /// </summary>
        private int _gen1CountAtLastRestock;

        /// <summary>
        /// Used to ensure we have a minimum time between trimmings.  
        /// </summary>
        private int _msecNoUseBeyondFreeListSinceThisTime;
        /// <summary>
        /// To trim, we remove things from the free list (which is Gen 2) and see if we 'hit bottom'
        /// This flag indicates that we hit bottom (we really needed a bigger free list).
        /// </summary>
        private bool _moreThanFreeListNeeded;
        /// <summary>
        /// The total number of buffers that this cache has ever allocated.
        /// Used in trimming heuristics. 
        /// </summary>
        private int _buffersUnderManagement;
        /// <summary>
        /// The number of buffers we added the last time we restocked.
        /// </summary>
        private int _restockSize;
        /// <summary>
        /// Did we put some buffers into _notGen2 to see if we can trim?
        /// </summary>
        private bool _trimmingExperimentInProgress;
        /// <summary>
        /// A forced minimum number of buffers.
        /// </summary>
        private int _minBufferCount = 0;
        /// <summary>
        /// The number of calls to Allocate.
        /// </summary>
        private int _numAllocCalls;
        #endregion
    }

    /// <summary>
    /// Schedules a callback roughly every gen 2 GC (you may see a Gen 0 an Gen 1 but only once)
    /// (We can fix this by capturing the Gen 2 count at startup and testing, but I mostly don't care)
    /// </summary>
    internal sealed class Gen2GcCallback //: CriticalFinalizerObject
    {
        [System.Security.SecuritySafeCritical]
        public Gen2GcCallback()
            : base()
        {
        }

        /// <summary>
        /// Schedule 'callback' to be called in the next GC.  If the callback returns true it is 
        /// rescheduled for the next Gen 2 GC.  Otherwise the callbacks stop. 
        /// 
        /// NOTE: This callback will be kept alive until either the callback function returns false,
        /// or the target object dies.
        /// </summary>
        public static void Register(Func<object, bool> callback, object targetObj)
        {
            // Create a unreachable object that remembers the callback function and target object.
            Gen2GcCallback gcCallback = new Gen2GcCallback();
            gcCallback.Setup(callback, targetObj);
        }

        #region Private

        private Func<object, bool> _callback;
        private GCHandle _weakTargetObj;

        [System.Security.SecuritySafeCritical]
        private void Setup(Func<object, bool> callback, object targetObj)
        {
            _callback = callback;
            _weakTargetObj = GCHandle.Alloc(targetObj, GCHandleType.Weak);
        }

        [System.Security.SecuritySafeCritical]
        ~Gen2GcCallback()
        {
            // Check to see if the target object is still alive.
            object targetObj = _weakTargetObj.Target;
            if (targetObj == null)
            {
                // The target object is dead, so this callback object is no longer needed.
                _weakTargetObj.Free();
                return;
            }

            // Execute the callback method.
            try
            {
                if (!_callback(targetObj))
                {
                    // If the callback returns false, this callback object is no longer needed.
                    return;
                }
            }
            catch
            {
                // Ensure that we still get a chance to resurrect this object, even if the callback throws an exception.
            }

            // Resurrect ourselves by re-registering for finalization.
            if (!Environment.HasShutdownStarted)
            {
                GC.ReRegisterForFinalize(this);
            }
        }
        #endregion
    }

    /// <summary>
    /// PinnableBufferCacheEventSource is a private eventSource that we are using to
    /// debug and monitor the effectiveness of PinnableBufferCache
    /// </summary>

    // The following EventSource Name must be unique per DLL:
    [EventSource(Name = "Microsoft-DotNETRuntime-PinnableBufferCache-Networking")]
    internal sealed class PinnableBufferCacheEventSource : EventSource
    {
        public static readonly PinnableBufferCacheEventSource Log = new PinnableBufferCacheEventSource();

        [Event(1, Level = EventLevel.Verbose)]
        public void DebugMessage(string message) { if (IsEnabled()) WriteEvent(1, message); }
        [Event(2, Level = EventLevel.Verbose)]
        public void DebugMessage1(string message, long value) { if (IsEnabled()) WriteEvent(2, message, value); }
        [Event(3, Level = EventLevel.Verbose)]
        public void DebugMessage2(string message, long value1, long value2) { if (IsEnabled()) WriteEvent(3, message, value1, value2); }
        [Event(18, Level = EventLevel.Verbose)]
        public void DebugMessage3(string message, long value1, long value2, long value3) { if (IsEnabled()) WriteEvent(18, message, value1, value2, value3); }

        [Event(4)]
        public void Create(string cacheName) { if (IsEnabled()) WriteEvent(4, cacheName); }

        [Event(5, Level = EventLevel.Verbose)]
        public void AllocateBuffer(string cacheName, ulong objectId, int objectHash, int objectGen, int freeCountAfter) { if (IsEnabled()) WriteEvent(5, cacheName, objectId, objectHash, objectGen, freeCountAfter); }
        [Event(6)]
        public void AllocateBufferFromNotGen2(string cacheName, int notGen2CountAfter) { if (IsEnabled()) WriteEvent(6, cacheName, notGen2CountAfter); }
        [Event(7)]
        public void AllocateBufferCreatingNewBuffers(string cacheName, int totalBuffsBefore, int objectCount) { if (IsEnabled()) WriteEvent(7, cacheName, totalBuffsBefore, objectCount); }
        [Event(8)]
        public void AllocateBufferAged(string cacheName, int agedCount) { if (IsEnabled()) WriteEvent(8, cacheName, agedCount); }
        [Event(9)]
        public void AllocateBufferFreeListEmpty(string cacheName, int notGen2CountBefore) { if (IsEnabled()) WriteEvent(9, cacheName, notGen2CountBefore); }

        [Event(10, Level = EventLevel.Verbose)]
        public void FreeBuffer(string cacheName, ulong objectId, int objectHash, int freeCountBefore) { if (IsEnabled()) WriteEvent(10, cacheName, objectId, objectHash, freeCountBefore); }
        [Event(11)]
        public void FreeBufferStillTooYoung(string cacheName, int notGen2CountBefore) { if (IsEnabled()) WriteEvent(11, cacheName, notGen2CountBefore); }

        [Event(13)]
        public void TrimCheck(string cacheName, int totalBuffs, bool neededMoreThanFreeList, int deltaMSec) { if (IsEnabled()) WriteEvent(13, cacheName, totalBuffs, neededMoreThanFreeList, deltaMSec); }
        [Event(14)]
        public void TrimFree(string cacheName, int totalBuffs, int freeListCount, int toBeFreed) { if (IsEnabled()) WriteEvent(14, cacheName, totalBuffs, freeListCount, toBeFreed); }
        [Event(15)]
        public void TrimExperiment(string cacheName, int totalBuffs, int freeListCount, int numTrimTrial) { if (IsEnabled()) WriteEvent(15, cacheName, totalBuffs, freeListCount, numTrimTrial); }
        [Event(16)]
        public void TrimFreeSizeOK(string cacheName, int totalBuffs, int freeListCount) { if (IsEnabled()) WriteEvent(16, cacheName, totalBuffs, freeListCount); }
        [Event(17)]
        public void TrimFlush(string cacheName, int totalBuffs, int freeListCount, int notGen2CountBefore) { if (IsEnabled()) WriteEvent(17, cacheName, totalBuffs, freeListCount, notGen2CountBefore); }
        [Event(20)]
        public void AgePendingBuffersResults(string cacheName, int promotedToFreeListCount, int heldBackCount) { if (IsEnabled()) WriteEvent(20, cacheName, promotedToFreeListCount, heldBackCount); }
        [Event(21)]
        public void WalkFreeListResult(string cacheName, int freeListCount, int gen0BuffersInFreeList) { if (IsEnabled()) WriteEvent(21, cacheName, freeListCount, gen0BuffersInFreeList); }


        static internal ulong AddressOf(object obj)
        {
            var asByteArray = obj as byte[];
            if (asByteArray != null)
            {
                return (ulong)AddressOfByteArray(asByteArray);
            }

            return 0;
        }

        [System.Security.SecuritySafeCritical]
        static internal unsafe long AddressOfByteArray(byte[] array)
        {
            if (array == null)
            {
                return 0;
            }

            fixed (byte* ptr = array)
            {
                return (long)(ptr - 2 * sizeof(void*));
            }
        }
    }
}
