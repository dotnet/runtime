// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Provides a way for an app to not start an operation unless
** there's a reasonable chance there's enough memory
** available for the operation to succeed.
**
**
===========================================================*/

using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Threading;

/*
   This class allows an application to fail before starting certain
   activities.  The idea is to fail early instead of failing in the middle
   of some long-running operation to increase the survivability of the
   application and ensure you don't have to write tricky code to handle an
   OOM anywhere in your app's code (which implies state corruption, meaning you
   should unload the appdomain, if you have a transacted environment to ensure
   rollback of individual transactions).  This is an incomplete tool to attempt
   hoisting all your OOM failures from anywhere in your worker methods to one
   particular point where it is easier to handle an OOM failure, and you can
   optionally choose to not start a workitem if it will likely fail.  This does
   not help the performance of your code directly (other than helping to avoid
   AD unloads).  The point is to avoid starting work if it is likely to fail.
   The Enterprise Services team has used these memory gates effectively in the
   unmanaged world for a decade.

   In Whidbey, we will simply check to see if there is enough memory available
   in the OS's page file & attempt to ensure there might be enough space free
   within the process's address space (checking for address space fragmentation
   as well).  We will not commit or reserve any memory.  To avoid race conditions with
   other threads using MemoryFailPoints, we'll also keep track of a
   process-wide amount of memory "reserved" via all currently-active
   MemoryFailPoints.  This has two problems:
      1) This can account for memory twice.  If a thread creates a
         MemoryFailPoint for 100 MB then allocates 99 MB, we'll see 99 MB
         less free memory and 100 MB less reserved memory.  Yet, subtracting
         off the 100 MB is necessary because the thread may not have started
         allocating memory yet.  Disposing of this class immediately after
         front-loaded allocations have completed is a great idea.
      2) This is still vulnerable to race conditions with other threads that don't use
         MemoryFailPoints.
   So this class is far from perfect.  But it may be good enough to
   meaningfully reduce the frequency of OutOfMemoryExceptions in managed apps.

   In Orcas or later, we might allocate some memory from the OS and add it
   to a allocation context for this thread.  Obviously, at that point we need
   some way of conveying when we release this block of memory.  So, we
   implemented IDisposable on this type in Whidbey and expect all users to call
   this from within a using block to provide lexical scope for their memory
   usage.  The call to Dispose (implicit with the using block) will give us an
   opportunity to release this memory, perhaps.  We anticipate this will give
   us the possibility of a more effective design in a future version.

   In Orcas, we may also need to differentiate between allocations that would
   go into the normal managed heap vs. the large object heap, or we should
   consider checking for enough free space in both locations (with any
   appropriate adjustments to ensure the memory is contiguous).
*/

namespace System.Runtime
{
    public sealed partial class MemoryFailPoint : CriticalFinalizerObject, IDisposable
    {
        // Find the top section of user mode memory.  Avoid the last 64K.
        // Windows reserves that block for the kernel, apparently, and doesn't
        // let us ask about that memory.  But since we ask for memory in 1 MB
        // chunks, we don't have to special case this.  Also, we need to
        // deal with 32 bit machines in 3 GB mode.
        // Using Win32's GetSystemInfo should handle all this for us.
        private static readonly ulong s_topOfMemory = GetTopOfMemory();

        // Walking the address space is somewhat expensive, taking around half
        // a millisecond.  Doing that per transaction limits us to a max of
        // ~2000 transactions/second.  Instead, let's do this address space
        // walk once every 10 seconds, or when we will likely fail.  This
        // amortization scheme can reduce the cost of a memory gate by about
        // a factor of 100.
        private static long s_hiddenLastKnownFreeAddressSpace;
        private static long s_hiddenLastTimeCheckingAddressSpace;
        private const int CheckThreshold = 10 * 1000;  // 10 seconds

        private static long LastKnownFreeAddressSpace
        {
            get => Volatile.Read(ref s_hiddenLastKnownFreeAddressSpace);
            set => Volatile.Write(ref s_hiddenLastKnownFreeAddressSpace, value);
        }

        private static void AddToLastKnownFreeAddressSpace(long addend) =>
            Interlocked.Add(ref s_hiddenLastKnownFreeAddressSpace, addend);

        private static long LastTimeCheckingAddressSpace
        {
            get => Volatile.Read(ref s_hiddenLastTimeCheckingAddressSpace);
            set => Volatile.Write(ref s_hiddenLastTimeCheckingAddressSpace, value);
        }

        // When allocating memory segment by segment, we've hit some cases
        // where there are only 22 MB of memory available on the machine,
        // we need 1 16 MB segment, and the OS does not succeed in giving us
        // that memory.  Reasons for this could include:
        // 1) The GC does allocate memory when doing a collection.
        // 2) Another process on the machine could grab that memory.
        // 3) Some other part of the runtime might grab this memory.
        // If we build in a little padding, we can help protect
        // ourselves against some of these cases, and we want to err on the
        // conservative side with this class.
        private const int LowMemoryFudgeFactor = 16 << 20;

        // Round requested size to a 16MB multiple to have a better granularity
        // when checking for available memory.
        private const int MemoryCheckGranularity = 16;

        // Note: This may become dynamically tunable in the future.
        // Also note that we can have different segment sizes for the normal vs.
        // large object heap.  We currently use the max of the two.
        private static readonly ulong s_GCSegmentSize = GC.GetSegmentSize();

        // For multi-threaded workers, we want to ensure that if two workers
        // use a MemoryFailPoint at the same time, and they both succeed, that
        // they don't trample over each other's memory.  Keep a process-wide
        // count of "reserved" memory, and decrement this in Dispose and
        // in the critical finalizer.
        private static long s_failPointReservedMemory;

        private readonly ulong _reservedMemory;  // The size of this request (from user)
        private bool _mustSubtractReservation; // Did we add data to SharedStatics?

        // We can remove this link demand in a future version - we will
        // have scenarios for this in partial trust in the future, but
        // we're doing this just to restrict this in case the code below
        // is somehow incorrect.
        public MemoryFailPoint(int sizeInMegabytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeInMegabytes);

            ulong size = ((ulong)sizeInMegabytes) << 20;
            _reservedMemory = size;

            // Check to see that we both have enough memory on the system
            // and that we have enough room within the user section of the
            // process's address space.  Also, we need to use the GC segment
            // size, not the amount of memory the user wants to allocate.
            // Consider correcting this to reflect free memory within the GC
            // heap, and to check both the normal & large object heaps.
            ulong segmentSize = (ulong)(Math.Ceiling((double)size / s_GCSegmentSize) * s_GCSegmentSize);
            if (segmentSize >= s_topOfMemory)
                throw new InsufficientMemoryException(SR.InsufficientMemory_MemFailPoint_TooBig);

            ulong requestedSizeRounded = (ulong)(Math.Ceiling((double)sizeInMegabytes / MemoryCheckGranularity) * MemoryCheckGranularity);
            // re-convert into bytes
            requestedSizeRounded <<= 20;

            // Check for available memory, with 2 attempts at getting more
            // memory.
            // Stage 0: If we don't have enough, trigger a GC.
            // Stage 1: If we don't have enough, try growing the swap file.
            // Stage 2: Update memory state, then fail or leave loop.
            //
            // (In the future, we could consider adding another stage after
            // Stage 0 to run finalizers.  However, before doing that make sure
            // that we could abort this constructor when we call
            // GC.WaitForPendingFinalizers, noting that this method uses a CER
            // so it can't be aborted, and we have a critical finalizer.  It
            // would probably work, but do some thinking first.)
            for (int stage = 0; stage < 3; stage++)
            {
                ulong availPageFile;  // available VM (physical + page file)
                ulong totalAddressSpaceFree;  // non-contiguous free address space

                if (!CheckForAvailableMemory(out availPageFile, out totalAddressSpaceFree))
                {
                    // _mustSubtractReservation == false
                    return;
                }

                // If we have enough room, then skip some stages.
                // Note that multiple threads can still lead to a race condition for our free chunk
                // of address space, which can't be easily solved.
                ulong reserved = MemoryFailPointReservedMemory;
                ulong segPlusReserved = segmentSize + reserved;
                bool overflow = segPlusReserved < segmentSize || segPlusReserved < reserved;
                bool needPageFile = availPageFile < (requestedSizeRounded + reserved + LowMemoryFudgeFactor) || overflow;
                bool needAddressSpace = totalAddressSpaceFree < segPlusReserved || overflow;

                // Ensure our cached amount of free address space is not stale.
                long now = Environment.TickCount;  // Handle wraparound.
                if (now > LastTimeCheckingAddressSpace + CheckThreshold || now < LastTimeCheckingAddressSpace ||
                    LastKnownFreeAddressSpace < (long)segmentSize)
                {
                    CheckForFreeAddressSpace(segmentSize, false);
                }
                bool needContiguousVASpace = (ulong)LastKnownFreeAddressSpace < segmentSize;

#if false
                Console.WriteLine($"MemoryFailPoint:" +
                    $"Checking for {(segmentSize >> 20)} MB, " +
                    $"for allocation size of {sizeInMegabytes} MB, " +
                    $"stage {stage}. " +
                    $"Need page file? {needPageFile} " +
                    $"Need Address Space? {needAddressSpace} " +
                    $"Need Contiguous address space? {needContiguousVASpace} " +
                    $"Avail page file: {(availPageFile >> 20)} MB " +
                    $"Total free VA space: {totalAddressSpaceFree >> 20} MB " +
                    $"Contiguous free address space (found): {LastKnownFreeAddressSpace >> 20} MB " +
                    $"Space reserved via process's MemoryFailPoints: {reserved} MB");
#endif

                if (!needPageFile && !needAddressSpace && !needContiguousVASpace)
                    break;

                switch (stage)
                {
                    case 0:
                        // The GC will release empty segments to the OS.  This will
                        // relieve us from having to guess whether there's
                        // enough memory in either GC heap, and whether
                        // internal fragmentation will prevent those
                        // allocations from succeeding.
                        GC.Collect();
                        continue;

                    case 1:
                        // Do this step if and only if the page file is too small.
                        if (!needPageFile)
                            continue;

                        // This shouldn't overflow due to the if clauses above.
                        UIntPtr numBytes = new UIntPtr(segmentSize);
                        GrowPageFileIfNecessaryAndPossible(numBytes);
                        continue;

                    case 2:
                        // The call to CheckForAvailableMemory above updated our
                        // state.
                        if (needPageFile || needAddressSpace)
                        {
                            InsufficientMemoryException e = new InsufficientMemoryException(SR.InsufficientMemory_MemFailPoint);
#if DEBUG
                            e.Data["MemFailPointState"] = new MemoryFailPointState(sizeInMegabytes, segmentSize,
                                 needPageFile, needAddressSpace, needContiguousVASpace,
                                 availPageFile >> 20, totalAddressSpaceFree >> 20,
                                 LastKnownFreeAddressSpace >> 20, reserved);
#endif
                            throw e;
                        }

                        if (needContiguousVASpace)
                        {
                            InsufficientMemoryException e = new InsufficientMemoryException(SR.InsufficientMemory_MemFailPoint_VAFrag);
#if DEBUG
                            e.Data["MemFailPointState"] = new MemoryFailPointState(sizeInMegabytes, segmentSize,
                                 needPageFile, needAddressSpace, needContiguousVASpace,
                                 availPageFile >> 20, totalAddressSpaceFree >> 20,
                                 LastKnownFreeAddressSpace >> 20, reserved);
#endif
                            throw e;
                        }

                        break;

                    default:
                        Debug.Fail("Fell through switch statement!");
                        break;
                }
            }

            // Success - we have enough room the last time we checked.
            // Now update our shared state in a somewhat atomic fashion
            // and handle a simple race condition with other MemoryFailPoint instances.
            AddToLastKnownFreeAddressSpace(-((long)size));
            if (LastKnownFreeAddressSpace < 0)
                CheckForFreeAddressSpace(segmentSize, true);

            AddMemoryFailPointReservation((long)size);
            _mustSubtractReservation = true;
        }

        ~MemoryFailPoint()
        {
            Disposing();
        }

        // Applications must call Dispose, which conceptually "releases" the
        // memory that was "reserved" by the MemoryFailPoint.  This affects a
        // global count of reserved memory in this version (helping to throttle
        // future MemoryFailPoints) in this version.  We may in the
        // future create an allocation context and release it in the Dispose
        // method.  While the finalizer will eventually free this block of
        // memory, apps will help their performance greatly by calling Dispose.
        public void Dispose()
        {
            Disposing();
            GC.SuppressFinalize(this);
        }

        private void Disposing()
        {
            // This is just bookkeeping to ensure multiple threads can really
            // get enough memory, and this does not actually reserve memory
            // within the GC heap.
            if (_mustSubtractReservation)
            {
                AddMemoryFailPointReservation(-((long)_reservedMemory));
                _mustSubtractReservation = false;
            }

            /*
            // Prototype performance
            // Let's pretend that we returned at least some free memory to
            // the GC heap.  We don't know this is true - the objects could
            // have a longer lifetime, and the memory could be elsewhere in the
            // GC heap.  Additionally, we subtracted off the segment size, not
            // this size.  That's ok - we don't mind if this slowly degrades
            // and requires us to refresh the value a little bit sooner.
            // But releasing the memory here should help us avoid probing for
            // free address space excessively with large workItem sizes.
            Interlocked.Add(ref LastKnownFreeAddressSpace, _reservedMemory);
            */
        }

        internal static long AddMemoryFailPointReservation(long size) =>
            // Size can legitimately be negative - see Dispose.
            Interlocked.Add(ref s_failPointReservedMemory, (long)size);

        internal static ulong MemoryFailPointReservedMemory
        {
            get
            {
                Debug.Assert(Volatile.Read(ref s_failPointReservedMemory) >= 0, "Process-wide MemoryFailPoint reserved memory was negative!");
                return (ulong)Volatile.Read(ref s_failPointReservedMemory);
            }
        }

#if DEBUG
        [Serializable]
        internal sealed class MemoryFailPointState
        {
            private readonly ulong _segmentSize;
            private readonly int _allocationSizeInMB;
            private readonly bool _needPageFile;
            private readonly bool _needAddressSpace;
            private readonly bool _needContiguousVASpace;
            private readonly ulong _availPageFile;
            private readonly ulong _totalFreeAddressSpace;
            private readonly long _lastKnownFreeAddressSpace;
            private readonly ulong _reservedMem;
            private readonly string _stackTrace;  // Where did we fail, for additional debugging.

            internal MemoryFailPointState(int allocationSizeInMB, ulong segmentSize, bool needPageFile, bool needAddressSpace, bool needContiguousVASpace, ulong availPageFile, ulong totalFreeAddressSpace, long lastKnownFreeAddressSpace, ulong reservedMem)
            {
                _allocationSizeInMB = allocationSizeInMB;
                _segmentSize = segmentSize;
                _needPageFile = needPageFile;
                _needAddressSpace = needAddressSpace;
                _needContiguousVASpace = needContiguousVASpace;
                _availPageFile = availPageFile;
                _totalFreeAddressSpace = totalFreeAddressSpace;
                _lastKnownFreeAddressSpace = lastKnownFreeAddressSpace;
                _reservedMem = reservedMem;
                try
                {
                    _stackTrace = Environment.StackTrace;
                }
                catch (Security.SecurityException)
                {
                    _stackTrace = "no permission";
                }
                catch (OutOfMemoryException)
                {
                    _stackTrace = "out of memory";
                }
            }

            public override string ToString()
            {
                return string.Format(Globalization.CultureInfo.InvariantCulture, "MemoryFailPoint detected insufficient memory to guarantee an operation could complete.  Checked for {0} MB, for allocation size of {1} MB.  Need page file? {2}  Need Address Space? {3}  Need Contiguous address space? {4}  Avail page file: {5} MB  Total free VA space: {6} MB  Contiguous free address space (found): {7} MB  Space reserved by process's MemoryFailPoints: {8} MB",
                    _segmentSize >> 20, _allocationSizeInMB, _needPageFile,
                    _needAddressSpace, _needContiguousVASpace,
                    _availPageFile >> 20, _totalFreeAddressSpace >> 20,
                    _lastKnownFreeAddressSpace >> 20, _reservedMem);
            }
        }
#endif
    }
}
