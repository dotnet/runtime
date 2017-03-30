// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Debug = System.Diagnostics.Debug;
using Interlocked = System.Threading.Interlocked;

namespace System.Xml.Linq
{
    /// <summary>
    /// This is a thread-safe hash table which maps string keys to values of type TValue.  It is assumed that the string key is embedded in the hashed value
    /// and can be extracted via a call to ExtractKeyDelegate (in order to save space and allow cleanup of key if value is released due to a WeakReference
    /// TValue releasing its target).
    /// </summary>
    /// <remarks>
    /// All methods on this class are thread-safe.
    ///
    /// When the hash table fills up, it is necessary to resize it and rehash all contents.  Because this can be expensive,
    /// a lock is taken, and one thread is responsible for the resize.  Other threads which need to add values must wait
    /// for the resize to be complete.
    ///
    /// Thread-Safety Notes
    /// ===================
    ///
    /// 1. Because performance and scalability are such a concern with the global name table, I have avoided the use of
    ///    BIFALOs (Big Fat Locks).  Instead, I use CompareExchange, Interlocked.Increment, memory barriers, atomic state objects,
    ///    etc. to avoid locks.  Any changes to code which accesses these variables should be carefully reviewed and tested,
    ///    as it can be *very* tricky.  In particular, if you don't understand the CLR memory model or if you don't know
    ///    what a memory barrier is, DON'T attempt to modify this code.  A good discussion of these topics can be found at
    ///    <![CDATA[http://discuss.develop.com/archives/wa.exe?A2=ind0203B&L=DOTNET&P=R375]]>. 
    ///
    /// 2. Because I am not sure if the CLR spec has changed since versions 1.0/1.1, I am assuming the weak memory model that
    ///    is described in the ECMA spec, in which normal writes can be reordered.  This means I must introduce more memory
    ///    barriers than otherwise would be necessary.
    ///
    /// 3. There are several thread-safety concepts and patterns I utilize in this code:
    ///      a. Publishing -- There are a small number of places where state is exposed, or published, to multiple threads.
    ///                       These places are marked with the comment "PUBLISH", and are key locations to consider when
    ///                       reviewing the code for thread-safety.
    ///
    ///      b. Immutable objects -- Immutable objects initialize their fields once in their constructor and then never modify
    ///                              them again.  As long as care is taken to ensure that initial field values are visible to
    ///                              other threads before publishing the immutable object itself, immutable objects are
    ///                              completely thread-safe.
    ///
    ///      c. Atomic state objects -- Locks typically are taken when several pieces of state must be updated atomically.  In
    ///                                 other words, there is a window in which state is inconsistent, and that window must
    ///                                 be protected from view by locking.  However, if a new object is created each time state
    ///                                 changes (or state changes substantially), then during creation the new object is only
    ///                                 visible to a single thread.  Once construction is complete, an assignment (guaranteed
    ///                                 atomic) can replace the old state object with the new state object, thus publishing a
    ///                                 consistent view to all threads.
    ///
    ///      d. Retry -- When several threads contend over shared state which only one is allowed to possess, it is possible
    ///                  to avoid locking by repeatedly attempting to acquire the shared state.  The CompareExchange method
    ///                  is useful for atomically ensuring that only one thread succeeds, and other threads are notified that
    ///                  they must retry.
    ///
    /// 4. All variables which can be written by multiple threads are marked "SHARED STATE".
    /// </remarks>
    internal sealed class XHashtable<TValue>
    {
        private XHashtableState _state;                          // SHARED STATE: Contains all XHashtable state, so it can be atomically swapped when resizes occur

        private const int StartingHash = (5381 << 16) + 5381;   // Starting hash code value for string keys to be hashed

        /// <summary>
        /// Prototype of function which is called to extract a string key value from a hashed value.
        /// Returns null if the hashed value is invalid (e.g. value has been released due to a WeakReference TValue being cleaned up).
        /// </summary>
        public delegate string ExtractKeyDelegate(TValue value);

        /// <summary>
        /// Construct a new XHashtable with the specified starting capacity.
        /// </summary>
        public XHashtable(ExtractKeyDelegate extractKey, int capacity)
        {
            _state = new XHashtableState(extractKey, capacity);
        }

        /// <summary>
        /// Get an existing value from the hash table.  Return false if no such value exists.
        /// </summary>
        public bool TryGetValue(string key, int index, int count, out TValue value)
        {
            return _state.TryGetValue(key, index, count, out value);
        }

        /// <summary>
        /// Add a value to the hash table, hashed based on a string key embedded in it.  Return the added value (may be a different object than "value").
        /// </summary>
        public TValue Add(TValue value)
        {
            TValue newValue;

            // Loop until value is in hash table
            while (true)
            {
                // Add new value
                // XHashtableState.TryAdd returns false if hash table is not big enough
                if (_state.TryAdd(value, out newValue))
                    return newValue;

                // PUBLISH (state)
                // Hash table was not big enough, so resize it.
                // We only want one thread to perform a resize, as it is an expensive operation
                // First thread will perform resize; waiting threads will call Resize(), but should immediately
                // return since there will almost always be space in the hash table resized by the first thread.
                lock (this)
                {
                    XHashtableState newState = _state.Resize();

                    // Use memory barrier to ensure that the resized XHashtableState object is fully constructed before it is assigned
#if !SILVERLIGHT 
                    Thread.MemoryBarrier();
#else // SILVERLIGHT
                    // The MemoryBarrier method usage is probably incorrect and should be removed.

                    // Replacing with Interlocked.CompareExchange for now (with no effect)
                    //   which will do a very similar thing to MemoryBarrier (it's just slower)
                    System.Threading.Interlocked.CompareExchange<XHashtableState>(ref _state, null, null);
#endif // SILVERLIGHT
                    _state = newState;
                }
            }
        }

        /// <summary>
        /// This class contains all the hash table state.  Rather than creating a bucket object, buckets are structs
        /// packed into an array.  Buckets with the same truncated hash code are linked into lists, so that collisions
        /// can be disambiguated.
        /// </summary>
        /// <remarks>
        /// Note that the "buckets" and "entries" arrays are never themselves written by multiple threads.  Instead, the
        /// *contents* of the array are written by multiple threads.  Resizing the hash table does not modify these variables,
        /// or even modify the contents of these variables.  Instead, resizing makes an entirely new XHashtableState object
        /// in which all entries are rehashed.  This strategy allows reader threads to continue finding values in the "old"
        /// XHashtableState, while writer threads (those that need to add a new value to the table) are blocked waiting for
        /// the resize to complete.
        /// </remarks>
        private sealed class XHashtableState
        {
            private int[] _buckets;                  // Buckets contain indexes into entries array (bucket values are SHARED STATE)
            private Entry[] _entries;                // Entries contain linked lists of buckets (next pointers are SHARED STATE)
            private int _numEntries;                 // SHARED STATE: Current number of entries (including orphaned entries)
            private ExtractKeyDelegate _extractKey;  // Delegate called in order to extract string key embedded in hashed TValue

            private const int EndOfList = 0;        // End of linked list marker
            private const int FullList = -1;        // Indicates entries should not be added to end of linked list

            /// <summary>
            /// Construct a new XHashtableState object with the specified capacity.
            /// </summary>
            public XHashtableState(ExtractKeyDelegate extractKey, int capacity)
            {
                Debug.Assert((capacity & (capacity - 1)) == 0, "capacity must be a power of 2");
                Debug.Assert(extractKey != null, "extractKey may not be null");

                // Initialize hash table data structures, with specified maximum capacity
                _buckets = new int[capacity];
                _entries = new Entry[capacity];

                // Save delegate
                _extractKey = extractKey;
            }

            /// <summary>
            /// If this table is not full, then just return "this".  Otherwise, create and return a new table with
            /// additional capacity, and rehash all values in the table.
            /// </summary>
            public XHashtableState Resize()
            {
                // No need to resize if there are open entries
                if (_numEntries < _buckets.Length)
                    return this;

                int newSize = 0;

                // Determine capacity of resized hash table by first counting number of valid, non-orphaned entries
                // As this count proceeds, close all linked lists so that no additional entries can be added to them
                for (int bucketIdx = 0; bucketIdx < _buckets.Length; bucketIdx++)
                {
                    int entryIdx = _buckets[bucketIdx];

                    if (entryIdx == EndOfList)
                    {
                        // Replace EndOfList with FullList, so that any threads still attempting to add will be forced to resize
                        entryIdx = Interlocked.CompareExchange(ref _buckets[bucketIdx], FullList, EndOfList);
                    }

                    // Loop until we've guaranteed that the list has been counted and closed to further adds
                    while (entryIdx > EndOfList)
                    {
                        // Count each valid entry
                        if (_extractKey(_entries[entryIdx].Value) != null)
                            newSize++;

                        if (_entries[entryIdx].Next == EndOfList)
                        {
                            // Replace EndOfList with FullList, so that any threads still attempting to add will be forced to resize
                            entryIdx = Interlocked.CompareExchange(ref _entries[entryIdx].Next, FullList, EndOfList);
                        }
                        else
                        {
                            // Move to next entry in the list
                            entryIdx = _entries[entryIdx].Next;
                        }
                    }
                    Debug.Assert(entryIdx == EndOfList, "Resize() should only be called by one thread");
                }

                // Double number of valid entries; if result is less than current capacity, then use current capacity
                if (newSize < _buckets.Length / 2)
                {
                    newSize = _buckets.Length;
                }
                else
                {
                    newSize = _buckets.Length * 2;

                    if (newSize < 0)
                        throw new OverflowException();
                }

                // Create new hash table with additional capacity
                XHashtableState newHashtable = new XHashtableState(_extractKey, newSize);

                // Rehash names (TryAdd will always succeed, since we won't fill the new table)
                // Do not simply walk over entries and add them to table, as that would add orphaned
                // entries.  Instead, walk the linked lists and add each name.
                for (int bucketIdx = 0; bucketIdx < _buckets.Length; bucketIdx++)
                {
                    int entryIdx = _buckets[bucketIdx];
                    TValue newValue;

                    while (entryIdx > EndOfList)
                    {
                        newHashtable.TryAdd(_entries[entryIdx].Value, out newValue);

                        entryIdx = _entries[entryIdx].Next;
                    }
                    Debug.Assert(entryIdx == FullList, "Linked list should have been closed when it was counted");
                }

                return newHashtable;
            }

            /// <summary>
            /// Attempt to find "key" in the table.  If the key exists, return the associated value in "value" and
            /// return true.  Otherwise return false.
            /// </summary>
            public bool TryGetValue(string key, int index, int count, out TValue value)
            {
                int hashCode = ComputeHashCode(key, index, count);
                int entryIndex = 0;

                // If a matching entry is found, return its value
                if (FindEntry(hashCode, key, index, count, ref entryIndex))
                {
                    value = _entries[entryIndex].Value;
                    return true;
                }

                // No matching entry found, so return false
                value = default(TValue);
                return false;
            }

            /// <summary>
            /// Attempt to add "value" to the table, hashed by an embedded string key.  If a value having the same key already exists,
            /// then return the existing value in "newValue".  Otherwise, return the newly added value in "newValue".
            ///
            /// If the hash table is full, return false.  Otherwise, return true.
            /// </summary>
            public bool TryAdd(TValue value, out TValue newValue)
            {
                int newEntry, entryIndex;
                string key;
                int hashCode;

                // Assume "value" will be added and returned as "newValue"
                newValue = value;

                // Extract the key from the value.  If it's null, then value is invalid and does not need to be added to table.
                key = _extractKey(value);
                if (key == null)
                    return true;

                // Compute hash code over entire length of key
                hashCode = ComputeHashCode(key, 0, key.Length);

                // Assume value is not yet in the hash table, and prepare to add it (if table is full, return false).
                // Use the entry index returned from Increment, which will never be zero, as zero conflicts with EndOfList.
                // Although this means that the first entry will never be used, it avoids the need to initialize all
                // starting buckets to the EndOfList value.
                newEntry = Interlocked.Increment(ref _numEntries);
                if (newEntry < 0 || newEntry >= _buckets.Length)
                    return false;

                _entries[newEntry].Value = value;
                _entries[newEntry].HashCode = hashCode;

                // Ensure that all writes to the entry can't be reordered past this barrier (or other threads might see new entry
                // in list before entry has been initialized!).
#if !SILVERLIGHT
                Thread.MemoryBarrier();
#else // SILVERLIGHT
                // The MemoryBarrier method usage is probably incorrect and should be removed.

                // Replacing with Interlocked.CompareExchange for now (with no effect)
                //   which will do a very similar thing to MemoryBarrier (it's just slower)
                System.Threading.Interlocked.CompareExchange<Entry[]>(ref _entries, null, null);
#endif // SILVERLIGHT

                // Loop until a matching entry is found, a new entry is added, or linked list is found to be full
                entryIndex = 0;
                while (!FindEntry(hashCode, key, 0, key.Length, ref entryIndex))
                {
                    // PUBLISH (buckets slot)
                    // No matching entry found, so add the new entry to the end of the list ("entryIndex" is index of last entry)
                    if (entryIndex == 0)
                        entryIndex = Interlocked.CompareExchange(ref _buckets[hashCode & (_buckets.Length - 1)], newEntry, EndOfList);
                    else
                        entryIndex = Interlocked.CompareExchange(ref _entries[entryIndex].Next, newEntry, EndOfList);

                    // Return true only if the CompareExchange succeeded (happens when replaced value is EndOfList).
                    // Return false if the linked list turned out to be full because another thread is currently resizing
                    // the hash table.  In this case, entries[newEntry] is orphaned (not part of any linked list) and the
                    // Add needs to be performed on the new hash table.  Otherwise, keep looping, looking for new end of list.
                    if (entryIndex <= EndOfList)
                        return entryIndex == EndOfList;
                }

                // Another thread already added the value while this thread was trying to add, so return that instance instead.
                // Note that entries[newEntry] will be orphaned (not part of any linked list) in this case
                newValue = _entries[entryIndex].Value;

                return true;
            }

            /// <summary>
            /// Searches a linked list of entries, beginning at "entryIndex".  If "entryIndex" is 0, then search starts at a hash bucket instead.
            /// Each entry in the list is matched against the (hashCode, key, index, count) key.  If a matching entry is found, then its
            /// entry index is returned in "entryIndex" and true is returned.  If no matching entry is found, then the index of the last entry
            /// in the list (or 0 if list is empty) is returned in "entryIndex" and false is returned.
            /// </summary>
            /// <remarks>
            /// This method has the side effect of removing invalid entries from the list as it is traversed.
            /// </remarks>
            private bool FindEntry(int hashCode, string key, int index, int count, ref int entryIndex)
            {
                int previousIndex = entryIndex;
                int currentIndex;

                // Set initial value of currentIndex to index of the next entry following entryIndex
                if (previousIndex == 0)
                    currentIndex = _buckets[hashCode & (_buckets.Length - 1)];
                else
                    currentIndex = previousIndex;

                // Loop while not at end of list
                while (currentIndex > EndOfList)
                {
                    // Check for matching hash code, then matching key
                    if (_entries[currentIndex].HashCode == hashCode)
                    {
                        string keyCompare = _extractKey(_entries[currentIndex].Value);

                        // If the key is invalid, then attempt to remove the current entry from the linked list.
                        // This is thread-safe in the case where the Next field points to another entry, since once a Next field points
                        // to another entry, it will never be modified to be EndOfList or FullList.
                        if (keyCompare == null)
                        {
                            if (_entries[currentIndex].Next > EndOfList)
                            {
                                // PUBLISH (buckets slot or entries slot)
                                // Entry is invalid, so modify previous entry to point to its next entry
                                _entries[currentIndex].Value = default(TValue);
                                currentIndex = _entries[currentIndex].Next;

                                if (previousIndex == 0)
                                    _buckets[hashCode & (_buckets.Length - 1)] = currentIndex;
                                else
                                    _entries[previousIndex].Next = currentIndex;

                                continue;
                            }
                        }
                        else
                        {
                            // Valid key, so compare keys
                            if (count == keyCompare.Length && string.CompareOrdinal(key, index, keyCompare, 0, count) == 0)
                            {
                                // Found match, so return true and matching entry in list
                                entryIndex = currentIndex;
                                return true;
                            }
                        }
                    }

                    // Move to next entry
                    previousIndex = currentIndex;
                    currentIndex = _entries[currentIndex].Next;
                }

                // Return false and last entry in list
                entryIndex = previousIndex;
                return false;
            }

            /// <summary>
            /// Compute hash code for a string key (index, count substring of "key").  The algorithm used is the same on used in NameTable.cs in System.Xml.
            /// </summary>
            private static int ComputeHashCode(string key, int index, int count)
            {
                int hashCode = StartingHash;
                int end = index + count;
                Debug.Assert(key != null, "key should have been checked previously for null");

                // Hash the key
                for (int i = index; i < end; i++)
                    unchecked
                    {
                        hashCode += (hashCode << 7) ^ key[i];
                    }

                // Mix up hash code a bit more and clear the sign bit.  This code was taken from NameTable.cs in System.Xml.
                hashCode -= hashCode >> 17;
                hashCode -= hashCode >> 11;
                hashCode -= hashCode >> 5;
                return hashCode & 0x7FFFFFFF;
            }

            /// <summary>
            /// Hash table entry.  The "Value" and "HashCode" fields are filled during initialization, and are never changed.  The "Next"
            /// field is updated when a new entry is chained to this one, and therefore care must be taken to ensure that updates to
            /// this field are thread-safe.
            /// </summary>
            private struct Entry
            {
                public TValue Value;    // Hashed value
                public int HashCode;    // Hash code of string key (equal to extractKey(Value).GetHashCode())
                public int Next;        // SHARED STATE: Points to next entry in linked list
            }
        }
    }
}
