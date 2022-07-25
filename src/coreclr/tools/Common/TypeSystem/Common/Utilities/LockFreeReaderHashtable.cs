// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// A hash table which is lock free for readers and up to 1 writer at a time.
    /// It must be possible to compute the key's hashcode from a value.
    /// All values must be reference types.
    /// It must be possible to perform an equality check between a key and a value.
    /// It must be possible to perform an equality check between a value and a value.
    /// A LockFreeReaderKeyValueComparer must be provided to perform these operations.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public abstract class LockFreeReaderHashtable<TKey, TValue> where TValue : class
    {
        private const int _initialSize = 16;
        private const int _fillPercentageBeforeResize = 60;

        /// <summary>
        /// Sentinel value used to represent that a slot in the hashtable is reserved
        /// for use by a writer thread. Readers must treat discovery of a sentinel as
        /// not finding an entry, and other writer threads must do so as well, and the
        /// Expand thread (if active) must treat discovery of a sentinel as a reason to
        /// yield execution until the sentinel is either written over with a null (indicating
        /// that the write is aborted), or over with a non-sentinel value (indicating
        /// the new value to be copied to the expanded hash table)
        ///
        /// In addition to serving as the sentinel, it is also the first item added, to
        /// avoid needing to have a unique valid sentinel value
        /// </summary>
        private TValue _entryInProcessOfWritingSentinel;

        /// <summary>
        /// _hashtable is the currently visible underlying array for the hashtable
        /// Any modifications to this array must be additive only, and there must
        /// never be a situation where the visible _hashtable has less data than
        /// it did at an earlier time. This value is initialized to an array of size
        /// 1. (That array is never mutated as any additions will trigger an Expand
        /// operation, but we don't use an empty array as the
        /// initial step, as this approach allows the TryGetValue logic to always
        /// succeed without needing any length or null checks.)
        /// </summary>
        private volatile TValue[] _hashtable = new TValue[_initialSize];

        /// <summary>
        /// Tracks the hashtable being used by expansion. Used as a sentinel
        /// to threads trying to add to the old hashtable that an expansion is
        /// in progress.
        /// </summary>
        private volatile TValue[] _newHashTable;

        /// <summary>
        /// _count represents the current count of elements in the hashtable
        /// _count is used in combination with _resizeCount to control when the
        /// hashtable should expand
        /// </summary>
        private volatile int _count;

        /// <summary>
        /// Represents _count plus the number of potential adds currently happening.
        /// If this reaches _hashTable.Length-1, an expansion is required (because
        /// one slot must always be null for seeks to complete).
        /// </summary>
        private int _reserve;

        /// <summary>
        /// _resizeCount represents the size at which the hashtable should resize.
        /// While this doesn't strictly need to be volatile, having threads read stale values
        /// triggers a lot of unneeded attempts to expand.
        /// </summary>
        private volatile int _resizeCount = _initialSize * _fillPercentageBeforeResize / 100;

        /// <summary>
        /// Get the underlying array for the hashtable at this time.
        /// </summary>
        private TValue[] GetCurrentHashtable()
        {
            return _hashtable;
        }

        /// <summary>
        /// Set the newly visible hashtable underlying array. Used by writers after
        /// the new array is fully constructed. The volatile write is used to ensure
        /// that all writes to the contents of hashtable are completed before _hashtable
        /// is visible to readers.
        /// </summary>
        private void SetCurrentHashtable(TValue[] hashtable)
        {
            _hashtable = hashtable;
        }

        /// <summary>
        /// Used to ensure that the hashtable can function with
        /// fairly poor initial hash codes.
        /// </summary>
        public static int HashInt1(int key)
        {
            unchecked
            {
                int a = (int)0x9e3779b9 + key;
                int b = (int)0x9e3779b9;
                int c = 16777619;
                a -= b; a -= c; a ^= (c >> 13);
                b -= c; b -= a; b ^= (a << 8);
                c -= a; c -= b; c ^= (b >> 13);
                a -= b; a -= c; a ^= (c >> 12);
                b -= c; b -= a; b ^= (a << 16);
                c -= a; c -= b; c ^= (b >> 5);
                a -= b; a -= c; a ^= (c >> 3);
                b -= c; b -= a; b ^= (a << 10);
                c -= a; c -= b; c ^= (b >> 15);
                return c;
            }
        }

        /// <summary>
        /// Generate a somewhat independent hash value from another integer. This is used
        /// as part of a double hashing scheme. By being relatively prime with powers of 2
        /// this hash function can be reliably used as part of a double hashing scheme as it
        /// is guaranteed to eventually probe every slot in the table. (Table sizes are
        /// constrained to be a power of two)
        /// </summary>
        public static int HashInt2(int key)
        {
            unchecked
            {
                int hash = unchecked((int)0xB1635D64) + key;
                hash += (hash << 3);
                hash ^= (hash >> 11);
                hash += (hash << 15);
                hash |= 0x00000001; //  To make sure that this is relatively prime with power of 2
                return hash;
            }
        }

        /// <summary>
        /// Create the LockFreeReaderHashtable. This hash table is designed for GetOrCreateValue
        /// to be a generally lock free api (unless an add is necessary)
        /// </summary>
        public LockFreeReaderHashtable()
        {

#if DEBUG
            // Ensure the initial value is a power of 2
            bool foundAOne = false;
            for (int i = 0; i < 32; i++)
            {
                int lastBit = _initialSize >> i;
                if ((lastBit & 0x1) == 0x1)
                {
                    Debug.Assert(!foundAOne);
                    foundAOne = true;
                }
            }
#endif // DEBUG
            _newHashTable = _hashtable;
        }

        /// <summary>
        /// The current count of elements in the hashtable
        /// </summary>
        public int Count { get { return _count + (_entryInProcessOfWritingSentinel != null ? 1 : 0); } }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with
        /// the specified key, if the key is found; otherwise, the default value for the type
        /// of the value parameter. This parameter is passed uninitialized. This function is threadsafe,
        /// and wait-free</param>
        /// <returns>true if a value was found</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            TValue[] hashTableLocal = GetCurrentHashtable();
            Debug.Assert(hashTableLocal.Length > 0);
            TValue sentinel;
            int mask = hashTableLocal.Length - 1;
            int hashCode = GetKeyHashCode(key);
            int tableIndex = HashInt1(hashCode) & mask;

            TValue examineEntry = hashTableLocal[tableIndex];
            if ((examineEntry == null) || (examineEntry == _entryInProcessOfWritingSentinel))
            {
                sentinel = Volatile.Read(ref _entryInProcessOfWritingSentinel);
                if (sentinel != null && CompareKeyToValue(key, sentinel))
                {
                    value = sentinel;
                    return true;
                }
                value = null;
                return false;
            }

            if (CompareKeyToValue(key, examineEntry))
            {
                value = examineEntry;
                return true;
            }

            int hash2 = HashInt2(hashCode);
            tableIndex = (tableIndex + hash2) & mask;
            examineEntry = hashTableLocal[tableIndex];
            while ((examineEntry != null) && (examineEntry != _entryInProcessOfWritingSentinel))
            {
                if (CompareKeyToValue(key, examineEntry))
                {
                    value = examineEntry;
                    return true;
                }
                tableIndex = (tableIndex + hash2) & mask;
                examineEntry = hashTableLocal[tableIndex];
            }

            sentinel = Volatile.Read(ref _entryInProcessOfWritingSentinel);
            if (sentinel != null && CompareKeyToValue(key, sentinel))
            {
                value = sentinel;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Spin and wait for a sentinel to disappear.
        /// </summary>
        /// <param name="hashtable"></param>
        /// <param name="tableIndex"></param>
        /// <returns>The value that replaced the sentinel, or null</returns>
        private TValue WaitForSentinelInHashtableToDisappear(TValue[] hashtable, int tableIndex)
        {
            TValue sentinel = Volatile.Read(ref _entryInProcessOfWritingSentinel);
            if (sentinel == null)
                return null;

            var sw = new SpinWait();
            while (true)
            {
                TValue value = Volatile.Read(ref hashtable[tableIndex]);
                if (value != sentinel)
                    return value;
                sw.SpinOnce();
            }
        }

        /// <summary>
        /// Make the underlying array of the hashtable bigger. This function
        /// does not change the contents of the hashtable. This entire function locks.
        /// </summary>
        private void Expand(TValue[] oldHashtable)
        {
            lock (this)
            {
                // If somebody else already resized, don't try to do it based on an old table
                if (oldHashtable != _hashtable)
                {
                    return;
                }

                // The checked statement here protects against both the hashTable size and _reserve overflowing. That does mean
                // the maximum size of _hashTable is 0x70000000
                int newSize = checked(oldHashtable.Length * 2);

                // The hashtable only functions well when it has a certain minimum size
                const int minimumUsefulSize = 16;
                if (newSize < minimumUsefulSize)
                    newSize = minimumUsefulSize;

                // Work in a local variable to avoid lots of unnecessary volatile reads of _newHashTable since only this method can
                // change it and we're under a lock
                TValue[] newHashTable = new TValue[newSize];
                // This is a rare "read-after-write" case where even x64/x86 needs fences.
                // We must ensure that the publishing of _newHashTable happens before we read the first table
                // entry from the pov of an external observer
                Interlocked.Exchange(ref _newHashTable, newHashTable);
                // Due to the volatile write above, any adds on other threads after this point will
                // fail and be redone, thus writing to the new hash table.

                int mask = newHashTable.Length - 1;
                TValue sentinel = Volatile.Read(ref _entryInProcessOfWritingSentinel);

                for (int iEntry = 0; iEntry < oldHashtable.Length; iEntry++)
                {
                    TValue value = oldHashtable[iEntry];
                    if (value == null)
                        continue;

                    if ((value == sentinel) && (sentinel != null))
                    {
                        // Entry is in the process of writing a value.
                        value = WaitForSentinelInHashtableToDisappear(oldHashtable, iEntry);

                        if (value == null)
                        {
                            // write was abandoned
                            continue;
                        }
                        // Otherwise, write completed. Insert the entry
                    }

                    // If there's a deadlock at this point, GetValueHashCode is re-entering Add, which it must not do.
                    int hashCode = GetValueHashCode(value);
                    int tableIndex = HashInt1(hashCode) & mask;

                    // Initial probe into hashtable found empty spot
                    if (newHashTable[tableIndex] == null)
                    {
                        // Add to hash
                        newHashTable[tableIndex] = value;
                        continue;
                    }

                    int hash2 = HashInt2(hashCode);
                    tableIndex = (tableIndex + hash2) & mask;

                    while (newHashTable[tableIndex] != null)
                    {
                        tableIndex = (tableIndex + hash2) & mask;
                    }

                    // We've probed to find an empty spot
                    // Add to hash
                    newHashTable[tableIndex] = value;
                }

                _resizeCount = checked((newSize * _fillPercentageBeforeResize) / 100);
                SetCurrentHashtable(newHashTable);
            }
        }

        /// <summary>
        /// Adds a value to the hashtable if it is not already present.
        /// Note that the key is not specified as it is implicit in the value. This function is thread-safe,
        /// but must only take locks around internal operations and GetValueHashCode.
        /// </summary>
        /// <param name="value">Value to attempt to add to the hashtable, must not be null</param>
        /// <returns>True if the value was added. False if it was already present.</returns>
        public bool TryAdd(TValue value)
        {
            bool addedValue;
            AddOrGetExistingInner(value, out addedValue);
            return addedValue;
        }

        /// <summary>
        /// Add a value to the hashtable, or find a value which is already present in the hashtable.
        /// Note that the key is not specified as it is implicit in the value. This function is thread-safe,
        /// but must only take locks around internal operations and GetValueHashCode.
        /// </summary>
        /// <param name="value">Value to attempt to add to the hashtable, must not be null</param>
        /// <returns>Newly added value, or a value which was already present in the hashtable which is equal to it.</returns>
        public TValue AddOrGetExisting(TValue value)
        {
            return AddOrGetExistingInner(value, out _);
        }

        private TValue AddOrGetExistingInner(TValue value, out bool addedValue)
        {
            if (value == null)
                throw new ArgumentNullException();

            if (_entryInProcessOfWritingSentinel == null)
            {
                if (Interlocked.CompareExchange(ref _entryInProcessOfWritingSentinel, value, null) == null)
                {
                    // First value was added as the sentinel
                    addedValue = true;
                    return value;
                }
            }

            // Optimistically check to see if adding this value may require an expansion. If so, expand
            // the table now. This isn't required to ensure space for the write, but helps keep
            // the ratio in a good range.
            if (_count >= _resizeCount)
            {
                Expand(_hashtable);
            }

            TValue result;
            do
            {
                result = TryAddOrGetExisting(value, out addedValue);
            } while (result == null);
            return result;
        }

        private TValue VolatileReadNonSentinelFromHashtable(TValue[] hashTable, int tableIndex)
        {
            TValue examineEntry = Volatile.Read(ref hashTable[tableIndex]);

            if (examineEntry == _entryInProcessOfWritingSentinel)
                examineEntry = WaitForSentinelInHashtableToDisappear(hashTable, tableIndex);

            return examineEntry;
        }

        /// <summary>
        /// Attempts to add a value to the hashtable, or find a value which is already present in the hashtable.
        /// In some cases, this will fail due to contention with other additions and must be retried.
        /// Note that the key is not specified as it is implicit in the value. This function is thread-safe,
        /// but must only take locks around internal operations and GetValueHashCode.
        /// </summary>
        /// <param name="value">Value to attempt to add to the hashtable, must not be null</param>
        /// <param name="addedValue">Set to true if <paramref name="value"/> was added to the table. False if the value
        /// was already present. Not defined if adding was attempted but failed.</param>
        /// <returns>Newly added value if adding succeds, a value which was already present in the hashtable which is equal to it,
        /// or null if adding fails and must be retried.</returns>
        private TValue TryAddOrGetExisting(TValue value, out bool addedValue)
        {
            // First check if the sentinel/first item matches the value.
            if (CompareValueToValue(value, _entryInProcessOfWritingSentinel))
            {
                // Value was already in the _entryInProcessOfWritingSentinel field
                // do not add
                addedValue = false;
                return _entryInProcessOfWritingSentinel;
            }

            // The table must be captured into a local to ensure reads/writes
            // don't get torn by expansions
            TValue[] hashTableLocal = _hashtable;

            addedValue = true;
            int mask = hashTableLocal.Length - 1;
            int hashCode = GetValueHashCode(value);
            int tableIndex = HashInt1(hashCode) & mask;

            // Find an empty spot, starting with the initial tableIndex
            TValue examineEntry = VolatileReadNonSentinelFromHashtable(hashTableLocal, tableIndex);
            if (examineEntry != null)
            {
                if (CompareValueToValue(value, examineEntry))
                {
                    // Value is already present in hash, do not add
                    addedValue = false;
                    return examineEntry;
                }

                int hash2 = HashInt2(hashCode);
                tableIndex = (tableIndex + hash2) & mask;
                examineEntry = VolatileReadNonSentinelFromHashtable(hashTableLocal, tableIndex);
                while (examineEntry != null)
                {
                    if (CompareValueToValue(value, examineEntry))
                    {
                        // Value is already present in hash, do not add
                        addedValue = false;
                        return examineEntry;
                    }
                    tableIndex = (tableIndex + hash2) & mask;
                    examineEntry = VolatileReadNonSentinelFromHashtable(hashTableLocal, tableIndex);
                }
            }

            // Ensure there's enough space for at least one null slot after this write
            if (Interlocked.Increment(ref _reserve) >= hashTableLocal.Length - 1)
            {
                Interlocked.Decrement(ref _reserve);
                Expand(hashTableLocal);

                // Since we expanded, our index won't work, restart
                return null;
            }

            // We've probed to find an empty spot, add to hash
            if (!TryWriteSentinelToLocation(hashTableLocal, tableIndex))
            {
                // After finding the empty spot, it was taken by some other thread
                Interlocked.Decrement(ref _reserve);
                return null;
            }

            // Now that we've written to the local array, find out if that array has been
            // replaced by expansion. If it has, we need to restart and write to the new array.
            if (_newHashTable != hashTableLocal)
            {
                WriteAbortNullToLocation(hashTableLocal, tableIndex);

                // Pulse the lock so we don't spin during an expansion
                lock (this) { }
                Interlocked.Decrement(ref _reserve);
                return null;
            }

            WriteValueToLocation(value, hashTableLocal, tableIndex);

            // If the write succeeded, increment _count
            Interlocked.Increment(ref _count);
            return value;
        }

        /// <summary>
        /// Attempts to write a the sentinel into the table. May fail if another value has been added.
        /// </summary>
        /// <returns>True if the sentinel was successfully written</returns>
        private bool TryWriteSentinelToLocation(TValue[] hashTableLocal, int tableIndex)
        {
            // Add to hash, use a volatile write to ensure that
            // the contents of the value are fully published to all
            // threads before adding to the hashtable
            if (Interlocked.CompareExchange(ref hashTableLocal[tableIndex], _entryInProcessOfWritingSentinel, null) == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to write a value into the table. Should never fail as the sentinel should be the only
        /// entry that can be in the table at this point
        /// </summary>
        private static void WriteValueToLocation(TValue value, TValue[] hashTableLocal, int tableIndex)
        {
            // Add to hash, use a volatile write to ensure that
            // the contents of the value are fully published to all
            // threads before adding to the hashtable
            Volatile.Write(ref hashTableLocal[tableIndex], value);
        }

        /// <summary>
        /// Attempts to abort write a value into the table. Should never fail as the sentinel should be the only
        /// entry that can be in the table at this point
        /// </summary>
        private static void WriteAbortNullToLocation(TValue[] hashTableLocal, int tableIndex)
        {
            // Add to hash, use a volatile write to ensure that
            // the contents of the value are fully published to all
            // threads before adding to the hashtable
            Volatile.Write(ref hashTableLocal[tableIndex], default(TValue)!);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private TValue CreateValueAndEnsureValueIsInTable(TKey key)
        {
#if WACKYDEBUG
            TValue newValue = CreateValueFromKey(key);
            Debug.Assert(GetValueHashCode(newValue) == GetKeyHashCode(key));
            Debug.Assert(CompareValueToValue(newValue, newValue));
            Debug.Assert(CompareKeyToValue(key, newValue));

            TValue foundValue = AddOrGetExisting(newValue);
            Debug.Assert(TryGetValue(key, out TValue testValue) && (Object.ReferenceEquals(testValue, foundValue)));
            return foundValue;
#else
            return AddOrGetExisting(CreateValueFromKey(key));
#endif
        }

        /// <summary>
        /// Get the value associated with a key. If value is not present in dictionary, use the creator delegate passed in
        /// at object construction time to create the value, and attempt to add it to the table. (Create the value while not
        /// under the lock, but add it to the table while under the lock. This may result in a throw away object being constructed)
        /// This function is thread-safe, but will take a lock to perform its operations.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetOrCreateValue(TKey key)
        {
            TValue existingValue;
            if (TryGetValue(key, out existingValue))
                return existingValue;

            return CreateValueAndEnsureValueIsInTable(key);
        }

        /// <summary>
        /// Determine if this collection contains a value associated with a key. This function is thread-safe, and wait-free.
        /// </summary>
        public bool Contains(TKey key)
        {
            return TryGetValue(key, out _);
        }

        /// <summary>
        /// Determine if this collection contains a given value, and returns the value in the hashtable if found. This function is thread-safe, and wait-free.
        /// </summary>
        /// <param name="value">Value to search for in the hashtable, must not be null</param>
        /// <returns>Value from the hashtable if found, otherwise null.</returns>
        public TValue GetValueIfExists(TValue value)
        {
            if (value == null)
                throw new ArgumentNullException();

            TValue[] hashTableLocal = GetCurrentHashtable();
            Debug.Assert(hashTableLocal.Length > 0);
            TValue sentinel;
            int mask = hashTableLocal.Length - 1;
            int hashCode = GetValueHashCode(value);
            int tableIndex = HashInt1(hashCode) & mask;

            TValue examineEntry = hashTableLocal[tableIndex];
            if ((examineEntry == null) || (examineEntry == _entryInProcessOfWritingSentinel))
            {
                sentinel = Volatile.Read(ref _entryInProcessOfWritingSentinel);
                if (sentinel != null && CompareValueToValue(value, sentinel))
                {
                    return sentinel;
                }
                return null;
            }

            if (CompareValueToValue(value, examineEntry))
                return examineEntry;

            int hash2 = HashInt2(hashCode);
            tableIndex = (tableIndex + hash2) & mask;
            examineEntry = hashTableLocal[tableIndex];

            while ((examineEntry == null) || (examineEntry == _entryInProcessOfWritingSentinel))
            {
                if (CompareValueToValue(value, examineEntry))
                    return examineEntry;

                tableIndex = (tableIndex + hash2) & mask;
                examineEntry = hashTableLocal[tableIndex];
            }

            sentinel = Volatile.Read(ref _entryInProcessOfWritingSentinel);
            if (sentinel != null && CompareValueToValue(value, sentinel))
            {
                return sentinel;
            }

            return null;
        }

        /// <summary>
        /// Enumerator type for the LockFreeReaderHashtable
        /// This is threadsafe, but is not garaunteed to avoid torn state.
        /// In particular, the enumerator may report some newly added values
        /// but not others. All values in the hashtable as of enumerator
        /// creation will always be enumerated.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private TValue[] _hashtableContentsToEnumerate;
            private TValue _sentinel;
            private int _index;
            private TValue _current;

            /// <summary>
            /// Use this to get an enumerable collection from a LockFreeReaderHashtable.
            /// Used instead of a GetEnumerator method on the LockFreeReaderHashtable to
            /// reduce excess type creation. (By moving the method here, the generic dictionary for
            /// LockFreeReaderHashtable does not need to contain a reference to the
            /// enumerator type.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Enumerator Get(LockFreeReaderHashtable<TKey, TValue> hashtable)
            {
                return new Enumerator(hashtable);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator()
            {
                return this;
            }

            internal Enumerator(LockFreeReaderHashtable<TKey, TValue> hashtable)
            {
                _sentinel = hashtable._entryInProcessOfWritingSentinel;
                _hashtableContentsToEnumerate = hashtable._hashtable;
                _index = 0;
                _current = default(TValue);
            }

            public bool MoveNext()
            {
                if (_sentinel != null)
                {
                    if ((_hashtableContentsToEnumerate != null) && (_index < _hashtableContentsToEnumerate.Length))
                    {
                        for (; _index < _hashtableContentsToEnumerate.Length; _index++)
                        {
                            if ((_hashtableContentsToEnumerate[_index] != null) && (_hashtableContentsToEnumerate[_index] != _sentinel))
                            {
                                _current = _hashtableContentsToEnumerate[_index];
                                _index++;
                                return true;
                            }
                        }
                    }
                }

                if ((_index == _hashtableContentsToEnumerate.Length) && _sentinel != null)
                {
                    _current = _sentinel;
                    _index++;
                    return true;
                }

                _current = default(TValue);
                return false;
            }

            public void Dispose()
            {
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public TValue Current
            {
                get
                {
                    return _current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Given a key, compute a hash code. This function must be thread safe.
        /// </summary>
        protected abstract int GetKeyHashCode(TKey key);

        /// <summary>
        /// Given a value, compute a hash code which would be identical to the hash code
        /// for a key which should look up this value. This function must be thread safe.
        /// This function must also not cause additional hashtable adds.
        /// </summary>
        protected abstract int GetValueHashCode(TValue value);

        /// <summary>
        /// Compare a key and value. If the key refers to this value, return true.
        /// This function must be thread safe.
        /// </summary>
        protected abstract bool CompareKeyToValue(TKey key, TValue value);

        /// <summary>
        /// Compare a value with another value. Return true if values are equal.
        /// This function must be thread safe.
        /// </summary>
        protected abstract bool CompareValueToValue(TValue value1, TValue value2);

        /// <summary>
        /// Create a new value from a key. Must be threadsafe. Value may or may not be added
        /// to collection. Return value must not be null.
        /// </summary>
        protected abstract TValue CreateValueFromKey(TKey key);
    }
}
