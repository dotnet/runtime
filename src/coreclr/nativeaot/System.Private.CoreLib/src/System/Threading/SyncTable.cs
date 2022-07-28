// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    /// <summary>
    /// Stores the hash code and the Monitor synchronization object for all managed objects used
    /// with the Monitor.Enter/TryEnter/Exit methods.
    /// </summary>
    /// <remarks>
    /// This implementation is faster than ConditionalWeakTable since we store the synchronization
    /// entry index in the object header, which avoids a hash table lookup. It closely matches
    /// implementation of SyncBlocks.
    ///
    /// SyncTable assigns a unique table entry to each object it is asked for.  The assigned entry
    /// index is stored in the object header and preserved during table expansion (we never shrink
    /// the table).  Each table entry contains a long weak GC handle representing the owner object
    /// of that entry and may be in one of the three states:
    /// 1) Free (IsAllocated == false).  These entries have been either never used or used and
    ///    freed/recycled after their owners died.  We keep a linked list of recycled entries and
    ///    use it to dispense entries to new objects.
    /// 2) Live (Target != null).  These entries store the hash code and the Monitor synchronization
    ///    object assigned to Target.
    /// 3) Dead (Target == null).  These entries lost their owners and are ready to be freed/recycled.
    ///
    /// Here is the state diagram for an entry:
    ///    Free --{AssignEntry}--> Live --{GC}--> Dead --{(Recycle|Free)DeadEntries} --> Free
    ///
    /// The public methods operates on live entries only and acquire the following locks:
    /// * GetLockObject : Lock-free.  We always allocate a Monitor synchronization object before
    ///                   the entry goes live.  The returned object may be used as normal; no
    ///                   additional synchronization required.
    /// * GetHashCode   : Lock-free.  A stale zero value may be returned.
    /// * SetHashCode   : Acquires s_lock.
    /// * AssignEntry   : Acquires s_lock.
    ///
    /// The important part here is that all read operations are lock-free and fast, and write
    /// operations are expected to be much less frequent than read ones.
    ///
    /// </remarks>
    [EagerStaticClassConstruction]
    internal static class SyncTable
    {
        /// <summary>
        /// The initial size of the table.  Must be positive and not greater than
        /// ObjectHeader.MASK_HASHCODE_INDEX + 1.
        /// </summary>
#if DEBUG
        // Exercise table expansion more frequently in debug builds
        private const int InitialSize = 1;
#else
        private const int InitialSize = 1 << 7;
#endif

        /// <summary>
        /// The table size threshold for doubling in size.  Must be positive.
        /// </summary>
        private const int DoublingSizeThreshold = 1 << 20;

        /// <summary>
        /// Protects all mutable operations on s_entrie, s_freeEntryList, s_unusedEntryIndex. Also protects growing the table.
        /// </summary>
        internal static Lock s_lock = new Lock();

        /// <summary>
        /// The dynamically growing array of sync entries.
        /// </summary>
        private static Entry[] s_entries = new Entry[InitialSize];

        /// <summary>
        /// The head of the list of freed entries linked using the Next property.
        /// </summary>
        private static int s_freeEntryList;

        /// <summary>
        /// The index of the lowest never used entry.  We skip the 0th entry and start with 1.
        /// If all entries have been used, s_unusedEntryIndex == s_entries.Length.  This counter
        /// never decreases.
        /// </summary>
        private static int s_unusedEntryIndex = 1;

        /// <summary>
        /// Assigns a sync table entry to the object in a thread-safe way.
        /// </summary>
        public static unsafe int AssignEntry(object obj, int* pHeader)
        {
            // Allocate the synchronization object outside the lock
            Lock lck = new Lock();
            DeadEntryCollector collector = new DeadEntryCollector();
            DependentHandle handle = new DependentHandle(obj, collector);

            try
            {
                using (LockHolder.Hold(s_lock))
                {
                    // After acquiring the lock check whether another thread already assigned the sync entry
                    if (ObjectHeader.GetSyncEntryIndex(*pHeader, out int hashOrIndex))
                    {
                        return hashOrIndex;
                    }

                    int syncIndex;
                    if (s_freeEntryList != 0)
                    {
                        // Grab a free entry from the list
                        syncIndex = s_freeEntryList;

                        ref Entry freeEntry = ref s_entries[syncIndex];
                        s_freeEntryList = freeEntry.Next;
                        freeEntry.Next = 0;
                    }
                    else
                    {
                        if (s_unusedEntryIndex >= s_entries.Length)
                        {
                            // No free entries, use the slow path.  This call may OOM.
                            Grow();
                        }

                        // Grab the next unused entry
                        Debug.Assert(s_unusedEntryIndex < s_entries.Length);
                        syncIndex = s_unusedEntryIndex++;
                    }

                    ref Entry entry = ref s_entries[syncIndex];

                    // Found a free entry to assign
                    Debug.Assert(!entry.Owner.IsAllocated);
                    Debug.Assert(entry.Lock == null);
                    Debug.Assert(entry.HashCode == 0);

                    // Set up the new entry.  We should not fail after this point.
                    entry.Lock = lck;

                    // The hash code will be set by the SetSyncEntryIndex call below
                    entry.Owner = handle;
                    handle = default;

                    collector.Activate(syncIndex);
                    collector = default!;

                    // Finally, store the entry index in the object header
                    ObjectHeader.SetSyncEntryIndex(pHeader, syncIndex);
                    return syncIndex;
                }
            }
            finally
            {
                if (collector != null)
                    GC.SuppressFinalize(collector);
                handle.Dispose();
            }
        }

        /// <summary>
        /// Grows the sync table.  If memory is not available, it throws an OOM exception keeping
        /// the state valid.
        /// </summary>
        private static void Grow()
        {
            Debug.Assert(s_lock.IsAcquired);

            int oldSize = s_entries.Length;
            int newSize = CalculateNewSize(oldSize);
            Entry[] newEntries = new Entry[newSize];

            // Copy the shallow content of the table
            Array.Copy(s_entries, newEntries, oldSize);

            // Publish the new table.  Lock-free reader threads must not see the new value of
            // s_entries until all the content is copied to the new table.
            Volatile.Write(ref s_entries, newEntries);
        }

        /// <summary>
        /// Calculates the new size of the sync table if it needs to grow.  Throws an OOM exception
        /// in case of size overflow.
        /// </summary>
        private static int CalculateNewSize(int oldSize)
        {
            Debug.Assert(oldSize > 0);
            Debug.Assert(ObjectHeader.MASK_HASHCODE_INDEX < int.MaxValue);
            int newSize;

            if (oldSize <= DoublingSizeThreshold)
            {
                // Double in size; overflow is checked below
                newSize = unchecked(oldSize * 2);
            }
            else
            {
                // For bigger tables use a smaller factor 1.5
                Debug.Assert(oldSize > 1);
                newSize = unchecked(oldSize + (oldSize >> 1));
            }

            // All indices must fit in the mask, limit the size accordingly
            newSize = Math.Min(newSize, ObjectHeader.MASK_HASHCODE_INDEX + 1);

            // Make sure the new size has not overflowed and is actually bigger
            if (newSize <= oldSize)
            {
                throw new OutOfMemoryException();
            }

            return newSize;
        }

        /// <summary>
        /// Returns the stored hash code.  The zero value indicates the hash code has not yet been
        /// assigned or visible to this thread.
        /// </summary>
        public static int GetHashCode(int syncIndex)
        {
            // This thread may be looking at an old version of s_entries.  If the old version had
            // no hash code stored, GetHashCode returns zero and the subsequent SetHashCode call
            // will resolve the potential race.
            return s_entries[syncIndex].HashCode;
        }

        /// <summary>
        /// Sets the hash code in a thread-safe way.
        /// </summary>
        public static int SetHashCode(int syncIndex, int hashCode)
        {
            Debug.Assert((0 < syncIndex) && (syncIndex < s_unusedEntryIndex));

            // Acquire the lock to ensure we are updating the latest version of s_entries.  This
            // lock may be avoided if we store the hash code and Monitor synchronization data in
            // the same object accessed by a reference.
            using (LockHolder.Hold(s_lock))
            {
                int currentHash = s_entries[syncIndex].HashCode;
                if (currentHash != 0)
                {
                    return currentHash;
                }
                s_entries[syncIndex].HashCode = hashCode;
                return hashCode;
            }
        }

        /// <summary>
        /// Sets the hash code assuming the caller holds s_lock.  Use for not yet
        /// published entries only.
        /// </summary>
        public static void MoveHashCodeToNewEntry(int syncIndex, int hashCode)
        {
            Debug.Assert(s_lock.IsAcquired);
            Debug.Assert((0 < syncIndex) && (syncIndex < s_unusedEntryIndex));
            s_entries[syncIndex].HashCode = hashCode;
        }

        /// <summary>
        /// Returns the Monitor synchronization object.  The return value is never null.
        /// </summary>
        public static Lock GetLockObject(int syncIndex)
        {
            // Note that we do not take a lock here.  When we replace s_entries, we preserve all
            // indices and Lock references.
            return s_entries[syncIndex].Lock;
        }

        private sealed class DeadEntryCollector
        {
            private int _index;

            public DeadEntryCollector()
            {
            }

            public void Activate(int index) => _index = index;

            ~DeadEntryCollector()
            {
                if (_index == 0)
                    return;

                Lock? lockToDispose = default;
                DependentHandle dependentHadleToDispose = default;

                using (LockHolder.Hold(s_lock))
                {
                    ref Entry entry = ref s_entries[_index];

                    if (entry.Owner.Target != null)
                    {
                        // Retry later if the owner is not collected yet.
                        GC.ReRegisterForFinalize(this);
                        return;
                    }

                    dependentHadleToDispose = entry.Owner;
                    entry.Owner = default;

                    lockToDispose = entry.Lock;
                    entry.Lock = default;

                    entry.Next = s_freeEntryList;
                    s_freeEntryList = _index;
                }

                // Dispose outside the lock
                dependentHadleToDispose.Dispose();
                lockToDispose?.Dispose();
            }
        }

        /// <summary>
        /// Stores the Monitor synchronization object and the hash code for an arbitrary object.
        /// </summary>
        private struct Entry
        {
            /// <summary>
            /// The Monitor synchronization object.
            /// </summary>
            public Lock Lock;

            /// <summary>
            /// Contains either the hash code or the index of the next freed entry.
            /// </summary>
            private int _hashOrNext;

            /// <summary>
            /// The dependent GC handle representing the owner object of this sync entry and the collector responsible
            /// for freeing the entry.
            /// </summary>
            public DependentHandle Owner;

            /// <summary>
            /// For entries in use, this property gets or sets the hash code of the owner object.
            /// The zero value indicates the hash code has not yet been assigned.
            /// </summary>
            public int HashCode
            {
                get { return _hashOrNext; }
                set { _hashOrNext = value; }
            }

            /// <summary>
            /// For freed entries, this property gets or sets the index of the next freed entry.
            /// The zero value indicates the end of the list.
            /// </summary>
            public int Next
            {
                get { return _hashOrNext; }
                set { _hashOrNext = value; }
            }
        }
    }
}
