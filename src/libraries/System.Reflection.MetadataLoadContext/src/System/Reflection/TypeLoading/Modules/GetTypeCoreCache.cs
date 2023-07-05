// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Reflection.TypeLoading
{
    internal sealed class GetTypeCoreCache
    {
        private volatile Container _container;
        private readonly object _lock;

        public GetTypeCoreCache()
        {
            _lock = new object();
            _container = new Container(this);
        }

        public bool TryGet(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name, int hashCode, [NotNullWhen(true)] out RoDefinitionType? type)
        {
            return _container.TryGetValue(ns, name, hashCode, out type);
        }

        public RoDefinitionType GetOrAdd(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name, int hashCode, RoDefinitionType type)
        {
            if ( _container.TryGetValue(ns, name, hashCode, out RoDefinitionType? prior))
                return prior;

            Monitor.Enter(_lock);
            try
            {
                if (_container.TryGetValue(ns, name, hashCode, out RoDefinitionType? winner))
                    return winner;
                if (!_container.HasCapacity)
                    _container.Resize(); // This overwrites the _container field.
                _container.Add(hashCode, type);
                return type;
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }

        public static int ComputeHashCode(ReadOnlySpan<byte> name)
        {
            int hashCode = 0x38723781;
            for (int i = 0; i < name.Length; i++)
            {
                hashCode = (hashCode << 8) ^ name[i];
            }
            return hashCode;
        }

        private sealed class Container
        {
            public Container(GetTypeCoreCache owner)
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

            private Container(GetTypeCoreCache owner, int[] buckets, Entry[] entries, int nextFreeEntry)
            {
                _buckets = buckets;
                _entries = entries;
                _nextFreeEntry = nextFreeEntry;
                _owner = owner;
            }

            public bool TryGetValue(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name, int hashCode, [NotNullWhen(true)] out RoDefinitionType? value)
            {
                // Lock acquistion NOT required.

                int bucket = ComputeBucket(hashCode, _buckets.Length);
                int i = Volatile.Read(ref _buckets[bucket]);
                while (i != -1)
                {
                    if (hashCode == _entries[i]._hashCode)
                    {
                        RoDefinitionType actualValue = _entries[i]._value;
                        if (actualValue.IsTypeNameEqual(ns, name))
                        {
                            value = actualValue;
                            return true;
                        }
                    }
                    i = _entries[i]._next;
                }

                value = default;
                return false;
            }

            public void Add(int hashCode, RoDefinitionType value)
            {
                int bucket = ComputeBucket(hashCode, _buckets.Length);
                int newEntryIdx = _nextFreeEntry;
                _entries[newEntryIdx]._value = value;
                _entries[newEntryIdx]._hashCode = hashCode;
                _entries[newEntryIdx]._next = _buckets[bucket];

                _nextFreeEntry++;

                // The line that atomically adds the new key/value pair. If the thread is killed before this line executes but after
                // we've incremented _nextFreeEntry, this entry is harmlessly leaked until the next resize.
                Volatile.Write(ref _buckets[bucket], newEntryIdx);

                VerifyUnifierConsistency();
            }

            public bool HasCapacity => _nextFreeEntry != _entries.Length;

            public void Resize()
            {
                int newSize = HashHelpers.GetPrime(_buckets.Length * 2);
#if DEBUG
                newSize = _buckets.Length + 3;
#endif
                if (newSize <= _nextFreeEntry)
                    throw new OutOfMemoryException();

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
                        newEntries[newNextFreeEntry]._value = _entries[entry]._value;
                        newEntries[newNextFreeEntry]._hashCode = _entries[entry]._hashCode;
                        int newBucket = ComputeBucket(newEntries[newNextFreeEntry]._hashCode, newSize);
                        newEntries[newNextFreeEntry]._next = newBuckets[newBucket];
                        newBuckets[newBucket] = newNextFreeEntry;
                        newNextFreeEntry++;
                    }
                }

                // The assertion is "<=" rather than "==" because we allow an entry to "leak" until the next resize if
                // a thread died between the time between we allocated the entry and the time we link it into the bucket stack.
                Debug.Assert(newNextFreeEntry <= _nextFreeEntry);

                // The line that atomically installs the resize. If this thread is killed before this point,
                // the table remains full and the next attempt to add will have to redo the resize.
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

            private readonly GetTypeCoreCache _owner;

            private const int _initialCapacity = 5;
        }

        private struct Entry
        {
            public RoDefinitionType _value;
            public int _hashCode;
            public int _next;
        }
    }
}
