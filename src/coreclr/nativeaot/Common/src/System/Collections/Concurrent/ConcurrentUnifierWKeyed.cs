// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.Collections.Concurrent
{
    // Abstract base for a thread-safe dictionary mapping a set of keys (K) to values (V).
    //
    // This flavor of ConcurrentUnifier holds values using weak references. It does not store the keys directly. Instead, values are
    // required to contain the key and expose it via IKeyedItem<K>. This flavor should be used in situations where the keys themselves
    // could store direct or indirect references to the value (thus, preventing the value's from being GC'd if the table were to
    // store the keys directly.)
    //
    // Value immortality is guaranteed. Once entered into the dictionary, the value never expires
    // in an observable way as long as values don't have finalizers.
    //
    // To create an actual dictionary, subclass this type and override the protected Factory method
    // to instantiate values (V) for the "Add" case.
    //
    // The key must be of a type that implements IEquatable<K>. The unifier calls IEquality<K>.Equals()
    // and Object.GetHashCode() on the keys.
    //
    // The value must be a reference type that implements IKeyedItem<K>. The unifier invokes the
    // IKeyedItem<K>.PrepareKey() method (outside the lock) on any value returned by the factory. This gives the value
    // a chance to do any lazy evaluation of the keys while it's safe to do so.
    //
    // Deadlock risks:
    //    - Keys may be tested for equality and asked to compute their hashcode while the unifier
    //      holds its lock. Thus these operations must be written carefully to avoid deadlocks and
    //      reentrancy in to the table.
    //
    //    - Values may get their IKeyedItem<K>.Key property called while the unifier holds its lock.
    //      Values that need to do lazy evaluation to compute their keys should do that in the PrepareKey()
    //      method which the unifier promises to call outside the lock prior to entering the value into the table.
    //
    //    - The Factory method will never be called inside the unifier lock. If two threads race to
    //      enter a value for the same key, the Factory() may get invoked twice for the same key - one
    //      of them will "win" the race and its result entered into the dictionary - other gets thrown away.
    //
    // Notes:
    //    - This class is used to look up types when GetType() or typeof() is invoked.
    //      That means that this class itself cannot do or call anything that does these
    //      things.
    //
    //    - For this reason, it chooses not to mimic the official ConcurrentDictionary class
    //      (I don't even want to risk using delegates.) Even the LowLevel versions of these
    //      general utility classes may not be low-level enough for this class's purpose.
    //
    // Thread safety guarantees:
    //
    //    ConcurrentUnifier is fully thread-safe and requires no
    //    additional locking to be done by callers.
    //
    // Performance characteristics:
    //
    //    ConcurrentUnifier will not block a reader, even while
    //    the table is being written.  Only one writer is allowed at a time;
    //    ConcurrentUnifier handles the synchronization that ensures this.
    //
    //    Safety for concurrent readers is ensured as follows:
    //
    //    Each hash bucket is maintained as a stack.  Inserts are done under
    //    a lock in one of two ways:
    //
    //    -  The entry is filled out completely, then "published" by a
    //       single write to the top of the bucket.  This ensures that a reader
    //       will see a valid snapshot of the bucket, once it has read the head.
    //
    //    -  An expired WeakReference inside an existing entry is replaced atomically
    //       by a new WeakReference. A reader will either see the old expired WeakReference
    //       (if so, he'll wait for the current lock to be released then do the locked retry)
    //       or the new WeakReference (which is fine for him to see.))
    //
    //    On resize, we allocate an entirely new table, rather than resizing
    //    in place.  We fill in the new table completely, under the lock,
    //    then "publish" it with a single write.  Any reader that races with
    //    this will either see the old table or the new one; each will contain
    //    the same data.
    //
    internal abstract class ConcurrentUnifierWKeyed<K, V>
        where K : IEquatable<K>
        where V : class, IKeyedItem<K>
    {
        protected ConcurrentUnifierWKeyed()
        {
            _lock = new Lock();
            _container = new Container(this);
        }

        //
        // Retrieve the *unique* value for a given key. If the key was previously not entered into the dictionary,
        // this method invokes the overridable Factory() method to create the new value. The Factory() method is
        // invoked outside of any locks. If two threads race to enter a value for the same key, the Factory()
        // may get invoked twice for the same key - one of them will "win" the race and its result entered into the
        // dictionary - other gets thrown away.
        //
        public V GetOrAdd(K key)
        {
            Debug.Assert(key != null);
            Debug.Assert(!_lock.IsAcquired, "GetOrAdd called while lock already acquired. A possible cause of this is an Equals or GetHashCode method that causes reentrancy in the table.");

            int hashCode = key.GetHashCode();
            V value;
            bool found = _container.TryGetValue(key, hashCode, out value);
#if DEBUG
            {
                V checkedValue;
                bool checkedFound;
                // In debug builds, always exercise a locked TryGet (this is a good way to detect deadlock/reentrancy through Equals/GetHashCode()).
                using (LockHolder.Hold(_lock))
                {
                    _container.VerifyUnifierConsistency();
                    int h = key.GetHashCode();
                    checkedFound = _container.TryGetValue(key, h, out checkedValue);
                }

                if (found)
                {
                    // Since this DEBUG code is holding a strong reference to "value", state of a key must never go from found to not found,
                    // and only one value may exist per key.
                    Debug.Assert(checkedFound);
                    Debug.Assert(object.ReferenceEquals(checkedValue, value));
                    GC.KeepAlive(value);
                }
            }
#endif //DEBUG
            if (found)
                return value;

            value = this.Factory(key);

            // This doesn't catch every object that has a finalizer, but the old saying about half a loaf...
            Debug.Assert(!(value is IDisposable),
                "Values placed in this table should not have finalizers. ConcurrentUnifiers guarantee observational immortality only " +
                "in the absence of finalizers. Or to speak more plainly, we can use WeakReferences to guarantee observational immortality " +
                "without paying the cost of storage immortality.");

            if (value == null)
            {
                // There's no point in caching null's in the dictionary as a WeakReference of null will always show up as expired
                // and force a re-add every time. Just return the null value without storing it. This does mean that repeated look ups
                // for this case will be very slow - this generally corresponds to scenarios like looking for a type member that doesn't
                // exist so hopefully, it's better to have awful throughput for such cases rather than polluting the dictionary with
                // "null entries" that have to be special-cased for everyone.
                return null;
            }

            // While still outside the lock, invoke the value's PrepareKey method to give the chance to do any lazy evaluation
            // it needs to produce the key quickly and in a deadlock-free manner once we're inside the lock.
            value.PrepareKey();

            using (LockHolder.Hold(_lock))
            {
                V heyIWasHereFirst;
                if (_container.TryGetValue(key, hashCode, out heyIWasHereFirst))
                    return heyIWasHereFirst;
                if (!_container.HasCapacity)
                    _container.Resize(); // This overwrites the _container field.
                _container.Add(key, hashCode, value);
                return value;
            }
        }

        protected abstract V Factory(K key);

        private volatile Container _container;
        private readonly Lock _lock;

        private sealed class Container
        {
            public Container(ConcurrentUnifierWKeyed<K, V> owner)
            {
                // Note: This could be done by calling Resize()'s logic but we cannot safely do that as this code path is reached
                // during class construction time and Resize() pulls in enough stuff that we get cyclic cctor warnings from the build.
                _buckets = new int[_initialCapacity];
                for (int i = 0; i < _initialCapacity; i++)
                    _buckets[i] = -1;
                _entries = new Entry[_initialCapacity];
                _nextFreeEntry = 0;
                _owner = owner;
            }

            private Container(ConcurrentUnifierWKeyed<K, V> owner, int[] buckets, Entry[] entries, int nextFreeEntry)
            {
                _buckets = buckets;
                _entries = entries;
                _nextFreeEntry = nextFreeEntry;
                _owner = owner;
            }

            public bool TryGetValue(K key, int hashCode, out V value)
            {
                // Lock acquistion NOT required.

                int bucket = ComputeBucket(hashCode, _buckets.Length);
                int i = Volatile.Read(ref _buckets[bucket]);
                while (i != -1)
                {
                    V? actualValue;
                    if (hashCode == _entries[i]._hashCode && _entries[i]._weakValue.TryGetTarget(out actualValue))
                    {
                        K actualKey = actualValue.Key;
                        if (key.Equals(actualKey))
                        {
                            value = actualValue;
                            return true;
                        }
                    }
                    i = _entries[i]._next;
                }

                value = default(V);
                return false;
            }

            public void Add(K key, int hashCode, V value)
            {
                Debug.Assert(_owner._lock.IsAcquired);

                int bucket = ComputeBucket(hashCode, _buckets.Length);
                int newEntryIdx = _nextFreeEntry;
                _entries[newEntryIdx]._weakValue = new WeakReference<V>(value, trackResurrection: false);
                _entries[newEntryIdx]._hashCode = hashCode;
                _entries[newEntryIdx]._next = _buckets[bucket];

                _nextFreeEntry++;

                // The line that atomically adds the new key/value pair. If the thread is killed before this line executes but after
                // we've incremented _nextFreeEntry, this entry is harmlessly leaked until the next resize.
                Volatile.Write(ref _buckets[bucket], newEntryIdx);

                VerifyUnifierConsistency();
            }

            public bool HasCapacity
            {
                get
                {
                    Debug.Assert(_owner._lock.IsAcquired);
                    return _nextFreeEntry != _entries.Length;
                }
            }

            public void Resize()
            {
                Debug.Assert(_owner._lock.IsAcquired);

                // Before we actually grow the size of the table, figure out how much we can recover just by dropping entries with
                // expired weak references.
                int estimatedNumLiveEntries = 0;
                for (int bucket = 0; bucket < _buckets.Length; bucket++)
                {
                    for (int entry = _buckets[bucket]; entry != -1; entry = _entries[entry]._next)
                    {
                        // Check if the weakreference has expired.
                        V? value;
                        if (_entries[entry]._weakValue.TryGetTarget(out value))
                            estimatedNumLiveEntries++;
                    }
                }
                double estimatedLivePercentage = ((double)estimatedNumLiveEntries) / ((double)(_entries.Length));
                int newSize;
                if (estimatedLivePercentage < _growThreshold && (_entries.Length - estimatedNumLiveEntries) > _initialCapacity)
                {
                    newSize = _buckets.Length;
                }
                else
                {
                    newSize = HashHelpers.GetPrime(_buckets.Length * 2);
#if DEBUG
                    newSize = _buckets.Length + 3;
#endif
                    if (newSize <= _nextFreeEntry)
                        throw new OutOfMemoryException();
                }
                Entry[] newEntries = new Entry[newSize];
                int[] newBuckets = new int[newSize];
                for (int i = 0; i < newSize; i++)
                    newBuckets[i] = -1;

                // Note that we walk the bucket chains rather than iterating over _entries. This is because we allow for the possibility
                // of abandoned entries (with undefined contents) if a thread is killed between allocating an entry and linking it onto the
                // bucket chain.
                int newNextFreeEntry = 0;
                for (int bucket = 0; bucket < _buckets.Length; bucket++)
                {
                    for (int entry = _buckets[bucket]; entry != -1; entry = _entries[entry]._next)
                    {
                        // Check if the weakreference has expired. If so, this is where we drop the entry altogether.
                        V? value;
                        if (_entries[entry]._weakValue.TryGetTarget(out value))
                        {
                            newEntries[newNextFreeEntry]._weakValue = _entries[entry]._weakValue;
                            newEntries[newNextFreeEntry]._hashCode = _entries[entry]._hashCode;
                            int newBucket = ComputeBucket(newEntries[newNextFreeEntry]._hashCode, newSize);
                            newEntries[newNextFreeEntry]._next = newBuckets[newBucket];
                            newBuckets[newBucket] = newNextFreeEntry;
                            newNextFreeEntry++;
                        }
                    }
                }

                // The assertion is "<=" rather than "==" because we allow an entry to "leak" until the next resize if
                // a thread died between the time between we allocated the entry and the time we link it into the bucket stack.
                // In addition, we don't bother copying entries where the weak reference has expired.
                Debug.Assert(newNextFreeEntry <= _nextFreeEntry);

                // The line that atomically installs the resize. If this thread is killed before this point,
                // the table remains full and the next guy attempting an add will have to redo the resize.
                _owner._container = new Container(_owner, newBuckets, newEntries, newNextFreeEntry);

                _owner._container.VerifyUnifierConsistency();
            }

            private static int ComputeBucket(int hashCode, int numBuckets)
            {
                int bucket = (hashCode & 0x7fffffff) % numBuckets;
                return bucket;
            }

            [Conditional("DEBUG")]
            public void VerifyUnifierConsistency()
            {
                // There's a point at which this check becomes gluttonous, even by checked build standards...
                if (_nextFreeEntry >= 5000 && (0 != (_nextFreeEntry % 100)))
                    return;

                Debug.Assert(_owner._lock.IsAcquired);
                Debug.Assert(_nextFreeEntry >= 0 && _nextFreeEntry <= _entries.Length);
                int numEntriesEncountered = 0;
                for (int bucket = 0; bucket < _buckets.Length; bucket++)
                {
                    int walk1 = _buckets[bucket];
                    int walk2 = _buckets[bucket];  // walk2 advances two elements at a time - if walk1 ever meets walk2, we've detected a cycle.
                    while (walk1 != -1)
                    {
                        numEntriesEncountered++;
                        Debug.Assert(walk1 >= 0 && walk1 < _nextFreeEntry);
                        Debug.Assert(walk2 >= -1 && walk2 < _nextFreeEntry);
                        Debug.Assert(_entries[walk1]._weakValue != null);
                        V? value;
                        if (_entries[walk1]._weakValue.TryGetTarget(out value))
                        {
                            K key = value.Key;
                            Debug.Assert(key != null);
                            int hashCode = key.GetHashCode();
                            Debug.Assert(hashCode == _entries[walk1]._hashCode);
                        }

                        int storedBucket = ComputeBucket(_entries[walk1]._hashCode, _buckets.Length);
                        Debug.Assert(storedBucket == bucket);
                        walk1 = _entries[walk1]._next;
                        if (walk2 != -1)
                            walk2 = _entries[walk2]._next;
                        if (walk2 != -1)
                            walk2 = _entries[walk2]._next;
                        if (walk1 == walk2 && walk2 != -1)
                            Debug.Fail("Bucket " + bucket + " has a cycle in its linked list.");
                    }
                }
                // The assertion is "<=" rather than "==" because we allow an entry to "leak" until the next resize if
                // a thread died between the time between we allocated the entry and the time we link it into the bucket stack.
                Debug.Assert(numEntriesEncountered <= _nextFreeEntry);
            }

            private readonly int[] _buckets;
            private readonly Entry[] _entries;
            private int _nextFreeEntry;

            private readonly ConcurrentUnifierWKeyed<K, V> _owner;

            private const int _initialCapacity = 5;
            private const double _growThreshold = 0.75;
        }

        private struct Entry
        {
            public WeakReference<V> _weakValue;
            public int _hashCode;
            public int _next;
        }
    }
}
