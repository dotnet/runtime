// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

using System;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

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
    public sealed class MemoryFailPoint : CriticalFinalizerObject, IDisposable
    {
        // Find the top section of user mode memory.  Avoid the last 64K.
        // Windows reserves that block for the kernel, apparently, and doesn't
        // let us ask about that memory.  But since we ask for memory in 1 MB
        // chunks, we don't have to special case this.  Also, we need to
        // deal with 32 bit machines in 3 GB mode.
        // Using Win32's GetSystemInfo should handle all this for us.
        private static readonly ulong TopOfMemory;

        // Walking the address space is somewhat expensive, taking around half
        // a millisecond.  Doing that per transaction limits us to a max of 
        // ~2000 transactions/second.  Instead, let's do this address space 
        // walk once every 10 seconds, or when we will likely fail.  This
        // amortization scheme can reduce the cost of a memory gate by about
        // a factor of 100.
        private static long hiddenLastKnownFreeAddressSpace = 0;
        private static long hiddenLastTimeCheckingAddressSpace = 0;
        private const int CheckThreshold = 10 * 1000;  // 10 seconds

        private static long LastKnownFreeAddressSpace
        {
            get { return Volatile.Read(ref hiddenLastKnownFreeAddressSpace); }
            set { Volatile.Write(ref hiddenLastKnownFreeAddressSpace, value); }
        }

        private static long AddToLastKnownFreeAddressSpace(long addend)
        {
            return Interlocked.Add(ref hiddenLastKnownFreeAddressSpace, addend);
        }

        private static long LastTimeCheckingAddressSpace
        {
            get { return Volatile.Read(ref hiddenLastTimeCheckingAddressSpace); }
            set { Volatile.Write(ref hiddenLastTimeCheckingAddressSpace, value); }
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
        private static readonly ulong GCSegmentSize;

        // For multi-threaded workers, we want to ensure that if two workers
        // use a MemoryFailPoint at the same time, and they both succeed, that
        // they don't trample over each other's memory.  Keep a process-wide
        // count of "reserved" memory, and decrement this in Dispose and
        // in the critical finalizer.  See 
        // SharedStatics.MemoryFailPointReservedMemory
        
        private ulong _reservedMemory;  // The size of this request (from user)
        private bool _mustSubtractReservation; // Did we add data to SharedStatics?

        [System.Security.SecuritySafeCritical]  // auto-generated
        static MemoryFailPoint()
        {
            GetMemorySettings(out GCSegmentSize, out TopOfMemory);
        }

        // We can remove this link demand in a future version - we will
        // have scenarios for this in partial trust in the future, but
        // we're doing this just to restrict this in case the code below
        // is somehow incorrect.
        [System.Security.SecurityCritical]  // auto-generated_required
        public MemoryFailPoint(int sizeInMegabytes)
        {
            if (sizeInMegabytes <= 0)
                throw new ArgumentOutOfRangeException("sizeInMegabytes", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            ulong size = ((ulong)sizeInMegabytes) << 20;
            _reservedMemory = size;

            // Check to see that we both have enough memory on the system
            // and that we have enough room within the user section of the 
            // process's address space.  Also, we need to use the GC segment
            // size, not the amount of memory the user wants to allocate.
            // Consider correcting this to reflect free memory within the GC
            // heap, and to check both the normal & large object heaps.
            ulong segmentSize = (ulong) (Math.Ceiling((double)size / GCSegmentSize) * GCSegmentSize);
            if (segmentSize >= TopOfMemory)
                throw new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint_TooBig"));

            ulong requestedSizeRounded = (ulong)(Math.Ceiling((double)sizeInMegabytes / MemoryCheckGranularity) * MemoryCheckGranularity);
            //re-convert into bytes
            requestedSizeRounded <<= 20;

            ulong availPageFile = 0;  // available VM (physical + page file)
            ulong totalAddressSpaceFree = 0;  // non-contiguous free address space

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
            for(int stage = 0; stage < 3; stage++) {
                CheckForAvailableMemory(out availPageFile, out totalAddressSpaceFree);

                // If we have enough room, then skip some stages.
                // Note that multiple threads can still lead to a race condition for our free chunk
                // of address space, which can't be easily solved.
                ulong reserved = SharedStatics.MemoryFailPointReservedMemory;
                ulong segPlusReserved = segmentSize + reserved;
                bool overflow = segPlusReserved < segmentSize || segPlusReserved < reserved;
                bool needPageFile = availPageFile < (requestedSizeRounded + reserved + LowMemoryFudgeFactor) || overflow;
                bool needAddressSpace = totalAddressSpaceFree < segPlusReserved || overflow;

                // Ensure our cached amount of free address space is not stale.
                long now = Environment.TickCount;  // Handle wraparound.
                if ((now > LastTimeCheckingAddressSpace + CheckThreshold || now < LastTimeCheckingAddressSpace) ||
                    LastKnownFreeAddressSpace < (long) segmentSize) {
                    CheckForFreeAddressSpace(segmentSize, false);
                }
                bool needContiguousVASpace = (ulong) LastKnownFreeAddressSpace < segmentSize;

                BCLDebug.Trace("MEMORYFAILPOINT", "MemoryFailPoint: Checking for {0} MB, for allocation size of {1} MB, stage {9}.  Need page file? {2}  Need Address Space? {3}  Need Contiguous address space? {4}  Avail page file: {5} MB  Total free VA space: {6} MB  Contiguous free address space (found): {7} MB  Space reserved via process's MemoryFailPoints: {8} MB",
                               segmentSize >> 20, sizeInMegabytes, needPageFile, 
                               needAddressSpace, needContiguousVASpace, 
                               availPageFile >> 20, totalAddressSpaceFree >> 20, 
                               LastKnownFreeAddressSpace >> 20, reserved, stage);

                if (!needPageFile && !needAddressSpace && !needContiguousVASpace)
                    break;

                switch(stage) {
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

                    // Attempt to grow the OS's page file.  Note that we ignore
                    // any allocation routines from the host intentionally.
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try {
                    }
                    finally {
                        // This shouldn't overflow due to the if clauses above.
                        UIntPtr numBytes = new UIntPtr(segmentSize);
                        unsafe {
                            void * pMemory = Win32Native.VirtualAlloc(null, numBytes, Win32Native.MEM_COMMIT, Win32Native.PAGE_READWRITE);
                            if (pMemory != null) {
                                bool r = Win32Native.VirtualFree(pMemory, UIntPtr.Zero, Win32Native.MEM_RELEASE);
                                if (!r)
                                    __Error.WinIOError();
                            }
                        }
                    }
                    continue;

                case 2:
                    // The call to CheckForAvailableMemory above updated our 
                    // state.
                    if (needPageFile || needAddressSpace) {
                        InsufficientMemoryException e = new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint"));
#if _DEBUG
                        e.Data["MemFailPointState"] = new MemoryFailPointState(sizeInMegabytes, segmentSize,
                             needPageFile, needAddressSpace, needContiguousVASpace, 
                             availPageFile >> 20, totalAddressSpaceFree >> 20,
                             LastKnownFreeAddressSpace >> 20, reserved);
#endif
                        throw e;
                    }

                    if (needContiguousVASpace) {
                        InsufficientMemoryException e = new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint_VAFrag"));
#if _DEBUG
                        e.Data["MemFailPointState"] = new MemoryFailPointState(sizeInMegabytes, segmentSize,
                             needPageFile, needAddressSpace, needContiguousVASpace, 
                             availPageFile >> 20, totalAddressSpaceFree >> 20,
                             LastKnownFreeAddressSpace >> 20, reserved);
#endif
                        throw e;
                    }

                    break;

                default:
                    Contract.Assert(false, "Fell through switch statement!");
                    break;
                }
            }

            // Success - we have enough room the last time we checked.
            // Now update our shared state in a somewhat atomic fashion
            // and handle a simple race condition with other MemoryFailPoint instances.
            AddToLastKnownFreeAddressSpace(-((long) size));
            if (LastKnownFreeAddressSpace < 0)
                CheckForFreeAddressSpace(segmentSize, true);
            
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                SharedStatics.AddMemoryFailPointReservation((long) size);
                _mustSubtractReservation = true;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static void CheckForAvailableMemory(out ulong availPageFile, out ulong totalAddressSpaceFree)
        {
            bool r;
            Win32Native.MEMORYSTATUSEX memory = new Win32Native.MEMORYSTATUSEX();
            r = Win32Native.GlobalMemoryStatusEx(ref memory);
            if (!r)
                __Error.WinIOError();
            availPageFile = memory.availPageFile;
            totalAddressSpaceFree = memory.availVirtual;
            //Console.WriteLine("Memory gate:  Mem load: {0}%  Available memory (physical + page file): {1} MB  Total free address space: {2} MB  GC Heap: {3} MB", memory.memoryLoad, memory.availPageFile >> 20, memory.availVirtual >> 20, GC.GetTotalMemory(true) >> 20);
        }

        // Based on the shouldThrow parameter, this will throw an exception, or 
        // returns whether there is enough space.  In all cases, we update
        // our last known free address space, hopefully avoiding needing to 
        // probe again.
        [System.Security.SecurityCritical]  // auto-generated
        private static unsafe bool CheckForFreeAddressSpace(ulong size, bool shouldThrow)
        {
            // Start walking the address space at 0.  VirtualAlloc may wrap
            // around the address space.  We don't need to find the exact
            // pages that VirtualAlloc would return - we just need to
            // know whether VirtualAlloc could succeed.
            ulong freeSpaceAfterGCHeap = MemFreeAfterAddress(null, size);

            BCLDebug.Trace("MEMORYFAILPOINT", "MemoryFailPoint: Checked for free VA space.  Found enough? {0}  Asked for: {1}  Found: {2}", (freeSpaceAfterGCHeap >= size), size, freeSpaceAfterGCHeap);

            // We may set these without taking a lock - I don't believe
            // this will hurt, as long as we never increment this number in 
            // the Dispose method.  If we do an extra bit of checking every
            // once in a while, but we avoid taking a lock, we may win.
            LastKnownFreeAddressSpace = (long) freeSpaceAfterGCHeap;
            LastTimeCheckingAddressSpace = Environment.TickCount;

            if (freeSpaceAfterGCHeap < size && shouldThrow)
                throw new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint_VAFrag"));
            return freeSpaceAfterGCHeap >= size;
        }

        // Returns the amount of consecutive free memory available in a block
        // of pages.  If we didn't have enough address space, we still return 
        // a positive value < size, to help potentially avoid the overhead of 
        // this check if we use a MemoryFailPoint with a smaller size next.
        [System.Security.SecurityCritical]  // auto-generated
        private static unsafe ulong MemFreeAfterAddress(void * address, ulong size)
        {
            if (size >= TopOfMemory)
                return 0;

            ulong largestFreeRegion = 0;
            Win32Native.MEMORY_BASIC_INFORMATION memInfo = new Win32Native.MEMORY_BASIC_INFORMATION();
            UIntPtr sizeOfMemInfo = (UIntPtr) Marshal.SizeOf(memInfo);
            
            while (((ulong)address) + size < TopOfMemory) {
                UIntPtr r = Win32Native.VirtualQuery(address, ref memInfo, sizeOfMemInfo);
                if (r == UIntPtr.Zero)
                    __Error.WinIOError();

                ulong regionSize = memInfo.RegionSize.ToUInt64();
                if (memInfo.State == Win32Native.MEM_FREE) {
                    if (regionSize >= size)
                        return regionSize;
                    else
                        largestFreeRegion = Math.Max(largestFreeRegion, regionSize);
                }
                address = (void *) ((ulong) address + regionSize);
            }
            return largestFreeRegion;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void GetMemorySettings(out ulong maxGCSegmentSize, out ulong topOfMemory);

        [System.Security.SecuritySafeCritical] // destructors should be safe to call
        ~MemoryFailPoint()
        {
            Dispose(false);
        }

        // Applications must call Dispose, which conceptually "releases" the
        // memory that was "reserved" by the MemoryFailPoint.  This affects a
        // global count of reserved memory in this version (helping to throttle
        // future MemoryFailPoints) in this version.  We may in the 
        // future create an allocation context and release it in the Dispose
        // method.  While the finalizer will eventually free this block of 
        // memory, apps will help their performance greatly by calling Dispose.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private void Dispose(bool disposing)
        {
            // This is just bookkeeping to ensure multiple threads can really
            // get enough memory, and this does not actually reserve memory
            // within the GC heap.
            if (_mustSubtractReservation) {
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                }
                finally {
                    SharedStatics.AddMemoryFailPointReservation(-((long)_reservedMemory));
                    _mustSubtractReservation = false;
                }
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

#if _DEBUG
        [Serializable]
        internal sealed class MemoryFailPointState
        {
            private ulong _segmentSize;
            private int _allocationSizeInMB;
            private bool _needPageFile;
            private bool _needAddressSpace;
            private bool _needContiguousVASpace;
            private ulong _availPageFile;
            private ulong _totalFreeAddressSpace;
            private long _lastKnownFreeAddressSpace;
            private ulong _reservedMem;
            private String _stackTrace;  // Where did we fail, for additional debugging.

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
                catch (System.Security.SecurityException)
                {
                    _stackTrace = "no permission";
                }
                catch (OutOfMemoryException)
                {
                    _stackTrace = "out of memory";
                }
            }

            public override String ToString()
            {
                return String.Format(System.Globalization.CultureInfo.InvariantCulture, "MemoryFailPoint detected insufficient memory to guarantee an operation could complete.  Checked for {0} MB, for allocation size of {1} MB.  Need page file? {2}  Need Address Space? {3}  Need Contiguous address space? {4}  Avail page file: {5} MB  Total free VA space: {6} MB  Contiguous free address space (found): {7} MB  Space reserved by process's MemoryFailPoints: {8} MB",
                    _segmentSize >> 20, _allocationSizeInMB, _needPageFile, 
                    _needAddressSpace, _needContiguousVASpace, 
                    _availPageFile >> 20, _totalFreeAddressSpace >> 20, 
                    _lastKnownFreeAddressSpace >> 20, _reservedMem);
            }

            public String StackTrace {
                get { return _stackTrace; }
            }
        }
#endif
    }
}
