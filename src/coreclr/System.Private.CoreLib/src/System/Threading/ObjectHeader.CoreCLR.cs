// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime;

namespace System.Threading
{
    /// <summary>
    /// Manipulates the object header located 4 bytes before each object's MethodTable pointer
    /// in the managed heap.
    /// </summary>
    /// <remarks>
    /// Do not store managed pointers (ref int) to the object header in locals or parameters
    /// as they may be incorrectly updated during garbage collection.
    /// </remarks>
    internal static partial class ObjectHeader
    {
        // The following three header bits are reserved for the GC engine:
        //   BIT_SBLK_UNUSED        = 0x80000000
        //   BIT_SBLK_FINALIZER_RUN = 0x40000000
        //   BIT_SBLK_GC_RESERVE    = 0x20000000
        //
        // All other bits may be used to store runtime data: hash code, sync entry index, etc.
        // Here we use the same bit layout as in CLR: if bit 26 (BIT_SBLK_IS_HASHCODE) is set,
        // all the lower bits 0..25 store the hash code, otherwise they store either the sync
        // entry index (indicated by BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) or thin lock data.
        private const int IS_HASHCODE_BIT_NUMBER = 26;
        private const int IS_HASH_OR_SYNCBLKINDEX_BIT_NUMBER = 27;
        private const int BIT_SBLK_IS_HASHCODE = 1 << IS_HASHCODE_BIT_NUMBER;
        internal const int MASK_HASHCODE_INDEX = BIT_SBLK_IS_HASHCODE - 1;
        private const int BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX = 1 << IS_HASH_OR_SYNCBLKINDEX_BIT_NUMBER;

        // This lock is only taken when we need to modify the index value in m_SyncBlockValue.
        // It should not be taken if the object already has a real syncblock index.
        // In this managed side, we skip the fast path while this spinlock is in use.
        // We'll sync up in the slow path.
        private const int BIT_SBLK_SPIN_LOCK = 0x10000000;

        // if BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX is clear, the rest of the header dword is laid out as follows:
        // - lower sixteen bits (bits 0 thru 15) is thread id used for the thin locks
        //   value is zero if no thread is holding the lock
        // - following six bits (bits 16 thru 21) is recursion level used for the thin locks
        //   value is zero if lock is not taken or only taken once by the same thread
        private const int SBLK_MASK_LOCK_THREADID = 0x0000FFFF;   // special value of 0 + 65535 thread ids
        private const int SBLK_MASK_LOCK_RECLEVEL = 0x003F0000;   // 64 recursion levels
        private const int SBLK_LOCK_RECLEVEL_INC = 0x00010000;    // each level is this much higher than the previous one

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int* GetHeaderPtr(nint* ppMethodTable)
        {
            // The header is 4 bytes before MT pointer on all architectures
            return (int*)ppMethodTable - 1;
        }

        internal static Lock GetLockObject(object obj)
        {
            IntPtr lockHandle = GetLockHandleIfExists(obj);
            if (lockHandle != 0)
            {
                Lock lockObj = GCHandle<Lock>.FromIntPtr(lockHandle).Target;
                GC.KeepAlive(obj);
                return lockObj;
            }

            return GetLockObjectFallback(obj);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static Lock GetLockObjectFallback(object obj)
            {
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                object lockObj = new Lock();
#pragma warning restore CS9216
                GetOrCreateLockObject(ObjectHandleOnStack.Create(ref obj), ObjectHandleOnStack.Create(ref lockObj));
                return (Lock)lockObj!;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetLockHandleIfExists(object obj);

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjectHeader_GetOrCreateLockObject")]
        private static partial void GetOrCreateLockObject(ObjectHandleOnStack obj, ObjectHandleOnStack lockObj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsAcquired(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            fixed (nint* ppMethodTable = &obj.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);

                // If the spin lock is set, the header may be transitioning to a sync block.
                // In that case, fall back to the slow path to determine ownership.
                int oldBits = *pHeader;

                // If no hash code or syncblock, the lock state is determined by the header.
                if ((oldBits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_SPIN_LOCK)) == 0)
                {
                    // do we own the thin lock?
                    return (oldBits & SBLK_MASK_LOCK_THREADID) == ManagedThreadId.Current;
                }
            }

            // Has a hash code or syncblock - let the slow path determine ownership.
            // Done outside the fixed block to avoid pinning the object across the call.
            return GetLockObject(obj).IsHeldByCurrentThread;
        }
    }
}
