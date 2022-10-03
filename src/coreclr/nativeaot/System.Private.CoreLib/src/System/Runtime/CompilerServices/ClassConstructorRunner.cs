// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.Runtime;
using Internal.Runtime.CompilerHelpers;

namespace System.Runtime.CompilerServices
{
    internal static partial class ClassConstructorRunner
    {
        //==============================================================================================================
        // Ensures the class constructor for the given type has run.
        //
        // Called by the runtime when it finds a class whose static class constructor has probably not run
        // (probably because it checks in the initialized flag without thread synchronization).
        //
        // The context structure passed by reference lives in the image of one of the application's modules.
        // The contents are thus fixed (do not require pinning) and the address can be used as a unique
        // identifier for the context.
        //
        // This guarantee is violated in one specific case: where a class constructor cycle would cause a deadlock. If
        // so, per ECMA specs, this method returns without guaranteeing that the .cctor has run.
        //
        // No attempt is made to detect or break deadlocks due to other synchronization mechanisms.
        //==============================================================================================================

        private static unsafe object CheckStaticClassConstructionReturnGCStaticBase(StaticClassConstructionContext* context, object gcStaticBase)
        {
            EnsureClassConstructorRun(context);
            return gcStaticBase;
        }

        private static unsafe IntPtr CheckStaticClassConstructionReturnNonGCStaticBase(StaticClassConstructionContext* context, IntPtr nonGcStaticBase)
        {
            EnsureClassConstructorRun(context);
            return nonGcStaticBase;
        }

        private static unsafe object CheckStaticClassConstructionReturnThreadStaticBase(TypeManagerSlot* pModuleData, int typeTlsIndex, StaticClassConstructionContext* context)
        {
            object threadStaticBase = ThreadStatics.GetThreadStaticBaseForType(pModuleData, typeTlsIndex);
            EnsureClassConstructorRun(context);
            return threadStaticBase;
        }

        public static unsafe void EnsureClassConstructorRun(StaticClassConstructionContext* pContext)
        {
            IntPtr pfnCctor = pContext->cctorMethodAddress;
            NoisyLog("EnsureClassConstructorRun, cctor={0}, thread={1}", pfnCctor, CurrentManagedThreadId);

            // If we were called from MRT, this check is redundant but harmless. This is in case someone within classlib
            // (cough, Reflection) needs to call this explicitly.
            if (pContext->initialized == 1)
            {
                NoisyLog("Cctor already run, cctor={0}, thread={1}", pfnCctor, CurrentManagedThreadId);
                return;
            }

            CctorHandle cctor = Cctor.GetCctor(pContext);
            Cctor[] cctors = cctor.Array;
            int cctorIndex = cctor.Index;
            try
            {
                Lock cctorLock = cctors[cctorIndex].Lock;
                if (DeadlockAwareAcquire(cctor, pfnCctor))
                {
                    int currentManagedThreadId = CurrentManagedThreadId;
                    try
                    {
                        NoisyLog("Acquired cctor lock, cctor={0}, thread={1}", pfnCctor, currentManagedThreadId);

                        cctors[cctorIndex].HoldingThread = currentManagedThreadId;
                        if (pContext->initialized == 0)  // Check again in case some thread raced us while we were acquiring the lock.
                        {
                            TypeInitializationException priorException = cctors[cctorIndex].Exception;
                            if (priorException != null)
                                throw priorException;
                            try
                            {
                                NoisyLog("Calling cctor, cctor={0}, thread={1}", pfnCctor, currentManagedThreadId);

                                ((delegate*<void>)pfnCctor)();

                                // Insert a memory barrier here to order any writes executed as part of static class
                                // construction above with respect to the initialized flag update we're about to make
                                // below. This is important since the fast path for checking the cctor uses a normal read
                                // and doesn't come here so without the barrier it could observe initialized == 1 but
                                // still see uninitialized static fields on the class.
                                Interlocked.MemoryBarrier();

                                NoisyLog("Set type inited, cctor={0}, thread={1}", pfnCctor, currentManagedThreadId);

                                pContext->initialized = 1;
                            }
                            catch (Exception e)
                            {
                                TypeInitializationException wrappedException = new TypeInitializationException(null, SR.TypeInitialization_Type_NoTypeAvailable, e);
                                cctors[cctorIndex].Exception = wrappedException;
                                throw wrappedException;
                            }
                        }
                    }
                    finally
                    {
                        cctors[cctorIndex].HoldingThread = ManagedThreadIdNone;
                        NoisyLog("Releasing cctor lock, cctor={0}, thread={1}", pfnCctor, currentManagedThreadId);

                        cctorLock.Release();
                    }
                }
                else
                {
                    // Cctor cycle resulted in a deadlock. We will break the guarantee and return without running the
                    // .cctor.
                }
            }
            finally
            {
                Cctor.Release(cctor);
            }
            NoisyLog("EnsureClassConstructorRun complete, cctor={0}, thread={1}", pfnCctor, CurrentManagedThreadId);
        }

        //=========================================================================================================
        // Return value:
        //   true   - lock acquired.
        //   false  - deadlock detected. Lock not acquired.
        //=========================================================================================================
        private static bool DeadlockAwareAcquire(CctorHandle cctor, IntPtr pfnCctor)
        {
            const int WaitIntervalSeedInMS = 1;      // seed with 1ms and double every time through the loop
            const int WaitIntervalLimitInMS = WaitIntervalSeedInMS << 7; // limit of 128ms

            int waitIntervalInMS = WaitIntervalSeedInMS;

            int cctorIndex = cctor.Index;
            Cctor[] cctors = cctor.Array;
            Lock lck = cctors[cctorIndex].Lock;
            if (lck.IsAcquired)
                return false;     // Thread recursively triggered the same cctor.

            if (lck.TryAcquire(waitIntervalInMS))
                return true;

            // We couldn't acquire the lock. See if this .cctor is involved in a cross-thread deadlock.  If so, break
            // the deadlock by breaking the guarantee - we'll skip running the .cctor and let the caller take his chances.
            int currentManagedThreadId = CurrentManagedThreadId;
            int unmarkCookie = -1;
            try
            {
                // We'll spin in a forever-loop of checking for a deadlock state, then waiting a short time, then
                // checking for a deadlock state again, and so on. This is because the BlockedRecord info has a built-in
                // lag time - threads don't report themselves as blocking until they've been blocked for a non-trivial
                // amount of time.
                //
                // If the threads are deadlocked for any reason other a class constructor cycling, this loop will never
                // terminate - this is by design. If the user code inside the class constructors were to
                // deadlock themselves, then that's a bug in user code.
                for (;;)
                {
                    using (LockHolder.Hold(s_cctorGlobalLock))
                    {
                        // Ask the guy who holds the cctor lock we're trying to acquire who he's waiting for. Keep
                        // walking down that chain until we either discover a cycle or reach a non-blocking state. Note
                        // that reaching a non-blocking state is not proof that we've avoided a deadlock due to the
                        // BlockingRecord reporting lag.
                        CctorHandle cctorWalk = cctor;
                        int chainStepCount = 0;
                        for (; chainStepCount < Cctor.Count; chainStepCount++)
                        {
                            int cctorWalkIndex = cctorWalk.Index;
                            Cctor[] cctorWalkArray = cctorWalk.Array;

                            int holdingThread = cctorWalkArray[cctorWalkIndex].HoldingThread;
                            if (holdingThread == currentManagedThreadId)
                            {
                                // Deadlock detected.  We will break the guarantee and return without running the .cctor.
                                DebugLog("A class constructor was skipped due to class constructor cycle. cctor={0}, thread={1}",
                                    pfnCctor, currentManagedThreadId);

                                // We are maintaining an invariant that the BlockingRecords never show a cycle because,
                                // before we add a record, we first check for a cycle.  As a result, once we've said
                                // we're waiting, we are committed to waiting and will not need to skip running this
                                // .cctor.
                                Debug.Assert(unmarkCookie == -1);
                                return false;
                            }

                            if (holdingThread == ManagedThreadIdNone)
                            {
                                // No one appears to be holding this cctor lock. Give the current thread some more time
                                // to acquire the lock.
                                break;
                            }

                            cctorWalk = BlockingRecord.GetCctorThatThreadIsBlockedOn(holdingThread);
                            if (cctorWalk.Array == null)
                            {
                                // The final thread in the chain appears to be blocked on nothing. Give the current
                                // thread some more time to acquire the lock.
                                break;
                            }
                        }

                        // We don't allow cycles in the BlockingRecords, so we must always enumerate at most each entry,
                        // but never more.
                        Debug.Assert(chainStepCount < Cctor.Count);

                        // We have not discovered a deadlock, so let's register the fact that we're waiting on another
                        // thread and continue to wait.  It is important that we only signal that we are blocked after
                        // we check for a deadlock because, otherwise, we give all threads involved in the deadlock the
                        // opportunity to break it themselves and that leads to "ping-ponging" between the cctors
                        // involved in the cycle, allowing intermediate cctor results to be observed.
                        //
                        // The invariant here is that we never 'publish' a BlockingRecord that forms a cycle.  So it is
                        // important that the look-for-cycle-and-then-publish-wait-status operation be atomic with
                        // respect to other updates to the BlockingRecords.
                        if (unmarkCookie == -1)
                        {
                            NoisyLog("Mark thread blocked, cctor={0}, thread={1}", pfnCctor, currentManagedThreadId);

                            unmarkCookie = BlockingRecord.MarkThreadAsBlocked(currentManagedThreadId, cctor);
                        }
                    } // _cctorGlobalLock scope

                    if (waitIntervalInMS < WaitIntervalLimitInMS)
                        waitIntervalInMS *= 2;

                    // We didn't find a cycle yet, try to take the lock again.
                    if (lck.TryAcquire(waitIntervalInMS))
                        return true;
                } // infinite loop
            }
            finally
            {
                if (unmarkCookie != -1)
                {
                    NoisyLog("Unmark thread blocked, cctor={0}, thread={1}", pfnCctor, currentManagedThreadId);
                    BlockingRecord.UnmarkThreadAsBlocked(unmarkCookie);
                }
            }
        }

        //==============================================================================================================
        // These structs are allocated on demand whenever the runtime tries to run a class constructor. Once the
        // the class constructor has been successfully initialized, we reclaim this structure. The structure is long-
        // lived only if the class constructor threw an exception.
        //==============================================================================================================
        private unsafe struct Cctor
        {
            public Lock Lock;
            public TypeInitializationException Exception;
            public int HoldingThread;
            private int _refCount;
            private StaticClassConstructionContext* _pContext;

            //==========================================================================================================
            // Gets the Cctor entry associated with a specific class constructor context (creating it if necessary.)
            //==========================================================================================================
            public static CctorHandle GetCctor(StaticClassConstructionContext* pContext)
            {
#if DEBUG
                const int Grow = 2;
#else
                const int Grow = 10;
#endif

                // WASMTODO: Remove this when the Initialize method gets called by the runtime startup
#if TARGET_WASM
                if (s_cctorGlobalLock == null)
                {
                    Interlocked.CompareExchange(ref s_cctorGlobalLock, new Lock(), null);
                }
                if (s_cctorArrays == null)
                {
                    Interlocked.CompareExchange(ref s_cctorArrays, new Cctor[10][], null);
                }
#endif // TARGET_WASM

                using (LockHolder.Hold(s_cctorGlobalLock))
                {
                    Cctor[]? resultArray = null;
                    int resultIndex = -1;

                    if (s_count != 0)
                    {
                        // Search for the cctor context in our existing arrays
                        for (int cctorIndex = 0; cctorIndex < s_cctorArraysCount; ++cctorIndex)
                        {
                            Cctor[] segment = s_cctorArrays[cctorIndex];
                            for (int i = 0; i < segment.Length; i++)
                            {
                                if (segment[i]._pContext == pContext)
                                {
                                    resultArray = segment;
                                    resultIndex = i;
                                    break;
                                }
                            }
                            if (resultArray != null)
                                break;
                        }
                    }
                    if (resultArray == null)
                    {
                        // look for an empty entry in an existing array
                        for (int cctorIndex = 0; cctorIndex < s_cctorArraysCount; ++cctorIndex)
                        {
                            Cctor[] segment = s_cctorArrays[cctorIndex];
                            for (int i = 0; i < segment.Length; i++)
                            {
                                if (segment[i]._pContext == default(StaticClassConstructionContext*))
                                {
                                    resultArray = segment;
                                    resultIndex = i;
                                    break;
                                }
                            }
                            if (resultArray != null)
                                break;
                        }
                        if (resultArray == null)
                        {
                            // allocate a new array
                            resultArray = new Cctor[Grow];
                            if (s_cctorArraysCount == s_cctorArrays.Length)
                            {
                                // grow the container
                                Array.Resize(ref s_cctorArrays, (s_cctorArrays.Length * 2) + 1);
                            }
                            // store the array in the container, this cctor gets index 0
                            s_cctorArrays[s_cctorArraysCount] = resultArray;
                            s_cctorArraysCount++;
                            resultIndex = 0;
                        }

                        Debug.Assert(resultArray[resultIndex]._pContext == default(StaticClassConstructionContext*));
                        resultArray[resultIndex]._pContext = pContext;
                        resultArray[resultIndex].Lock = new Lock();
                        s_count++;
                    }

                    Interlocked.Increment(ref resultArray[resultIndex]._refCount);
                    return new CctorHandle(resultArray, resultIndex);
                }
            }

            public static int Count
            {
                get
                {
                    Debug.Assert(s_cctorGlobalLock.IsAcquired);
                    return s_count;
                }
            }

            public static void Release(CctorHandle cctor)
            {
                using (LockHolder.Hold(s_cctorGlobalLock))
                {
                    Cctor[] cctors = cctor.Array;
                    int cctorIndex = cctor.Index;
                    if (0 == Interlocked.Decrement(ref cctors[cctorIndex]._refCount))
                    {
                        if (cctors[cctorIndex].Exception == null)
                        {
                            cctors[cctorIndex] = default;
                            s_count--;
                        }
                    }
                }
            }
        }

        private struct CctorHandle
        {
            public CctorHandle(Cctor[] array, int index)
            {
                _array = array;
                _index = index;
            }

            public Cctor[] Array { get { return _array; } }
            public int Index { get { return _index; } }

            private Cctor[] _array;
            private int _index;
        }

        //==============================================================================================================
        // Keeps track of threads that are blocked on a cctor lock (alas, we don't have ThreadLocals here in
        // System.Private.CoreLib so we have to use a side table.)
        //
        // This is used for cross-thread deadlock detection.
        //
        // - Data is only entered here if a thread has been blocked past a certain timeout (otherwise, it's certainly
        //   not participating of a deadlock.)
        // - Reads and writes to _blockingRecord are guarded by _cctorGlobalLock.
        // - BlockingRecords for individual threads are created on demand. Since this is a rare event, we won't attempt
        //   to recycle them directly (however,
        //   ManagedThreadId's are themselves recycled pretty quickly - and threads that inherit the managed id also
        //   inherit the BlockingRecord.)
        //==============================================================================================================
        private struct BlockingRecord
        {
            public int ManagedThreadId;     // ManagedThreadId of the blocked thread
            public CctorHandle BlockedOn;

            public static int MarkThreadAsBlocked(int managedThreadId, CctorHandle blockedOn)
            {
#if DEBUG
                const int Grow = 2;
#else
                const int Grow = 10;
#endif
                using (LockHolder.Hold(s_cctorGlobalLock))
                {
                    s_blockingRecords ??= new BlockingRecord[Grow];
                    int found;
                    for (found = 0; found < s_nextBlockingRecordIndex; found++)
                    {
                        if (s_blockingRecords[found].ManagedThreadId == managedThreadId)
                            break;
                    }
                    if (found == s_nextBlockingRecordIndex)
                    {
                        if (s_nextBlockingRecordIndex == s_blockingRecords.Length)
                        {
                            BlockingRecord[] newBlockingRecords = new BlockingRecord[s_blockingRecords.Length + Grow];
                            for (int i = 0; i < s_blockingRecords.Length; i++)
                            {
                                newBlockingRecords[i] = s_blockingRecords[i];
                            }
                            s_blockingRecords = newBlockingRecords;
                        }
                        s_blockingRecords[s_nextBlockingRecordIndex].ManagedThreadId = managedThreadId;
                        s_nextBlockingRecordIndex++;
                    }
                    s_blockingRecords[found].BlockedOn = blockedOn;
                    return found;
                }
            }

            public static void UnmarkThreadAsBlocked(int blockRecordIndex)
            {
                // This method must never throw
                s_cctorGlobalLock.Acquire();
                s_blockingRecords[blockRecordIndex].BlockedOn = new CctorHandle(null, 0);
                s_cctorGlobalLock.Release();
            }

            public static CctorHandle GetCctorThatThreadIsBlockedOn(int managedThreadId)
            {
                Debug.Assert(s_cctorGlobalLock.IsAcquired);
                for (int i = 0; i < s_nextBlockingRecordIndex; i++)
                {
                    if (s_blockingRecords[i].ManagedThreadId == managedThreadId)
                        return s_blockingRecords[i].BlockedOn;
                }
                return new CctorHandle(null, 0);
            }

            private static BlockingRecord[] s_blockingRecords;
            private static int s_nextBlockingRecordIndex;
        }

        private static int CurrentManagedThreadId => ManagedThreadId.Current;
        private const int ManagedThreadIdNone = ManagedThreadId.IdNone;

        private static Lock s_cctorGlobalLock;

        // These three  statics are used by ClassConstructorRunner.Cctor but moved out to avoid an unnecessary
        // extra class constructor call.
        //
        // Because Cctor's are mutable structs, we have to give our callers raw references to the underlying arrays
        // for this collection to be usable.  This also means once we place a Cctor in an array, we can't grow or
        // reallocate the array.
        private static Cctor[][] s_cctorArrays;
        private static int s_cctorArraysCount;
        private static int s_count;

        // Eager construction called from LibraryInitialize Cctor.GetCctor uses _cctorGlobalLock.
        internal static void Initialize()
        {
            s_cctorArrays = new Cctor[10][];
            s_cctorGlobalLock = new Lock();
        }

        [Conditional("ENABLE_NOISY_CCTOR_LOG")]
        private static void NoisyLog(string format, IntPtr cctorMethod, int threadId)
        {
            // We cannot utilize any of the typical number formatting code because it triggers globalization code to run
            // and this cctor code is layered below globalization.
#if DEBUG
            Debug.WriteLine(format, ToHexString(cctorMethod), ToHexString(threadId));
#endif // DEBUG
        }

        [Conditional("DEBUG")]
        private static void DebugLog(string format, IntPtr cctorMethod, int threadId)
        {
            // We cannot utilize any of the typical number formatting code because it triggers globalization code to run
            // and this cctor code is layered below globalization.
#if DEBUG
            Debug.WriteLine(format, ToHexString(cctorMethod), ToHexString(threadId));
#endif
        }

        // We cannot utilize any of the typical number formatting code because it triggers globalization code to run
        // and this cctor code is layered below globalization.
#if DEBUG
        private static string ToHexString(int num)
        {
            return ToHexStringUnsignedLong((ulong)num, false, 8);
        }
        private static string ToHexString(IntPtr num)
        {
            return ToHexStringUnsignedLong((ulong)num, false, 16);
        }
        private static char GetHexChar(uint u)
        {
            if (u < 10)
                return unchecked((char)('0' + u));

            return unchecked((char)('a' + (u - 10)));
        }
        public static unsafe string ToHexStringUnsignedLong(ulong u, bool zeroPrepad, int numChars)
        {
            char[] chars = new char[numChars];

            int i = numChars - 1;

            for (; i >= 0; i--)
            {
                chars[i] = GetHexChar((uint)(u % 16));
                u /= 16;

                if ((i == 0) || (!zeroPrepad && (u == 0)))
                    break;
            }

            string str;
            fixed (char* p = &chars[i])
            {
                str = new string(p, 0, numChars - i);
            }
            return str;
        }
#endif
    }
}
