// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Collections.Generic
{
    /*============================================================
    **
    ** Class:  LowLevelDictionary<TKey, TValue>
    **
    ** Private version of Dictionary<> for internal System.Private.CoreLib use. This
    ** permits sharing more source between BCL and System.Private.CoreLib (as well as the
    ** fact that Dictionary<> is just a useful class in general.)
    **
    ** This does not strive to implement the full api surface area
    ** (but any portion it does implement should match the real Dictionary<>'s
    ** behavior.)
    ** 
    ===========================================================*/
    internal partial class LowLevelDictionary<TKey, TValue>
    {
        private const int DefaultSize = 17;

        public LowLevelDictionary()
            : this(DefaultSize, new DefaultComparer<TKey>())
        {
        }

        public LowLevelDictionary(int capacity)
            : this(capacity, new DefaultComparer<TKey>())
        {
        }

        public LowLevelDictionary(IEqualityComparer<TKey> comparer) : this(DefaultSize, comparer)
        {
        }

        public LowLevelDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer;
            Clear(capacity);
        }

        public int Count
        {
            get
            {
                return _numEntries;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                Entry entry = Find(key);
                if (entry == null)
                    throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
                return entry._value;
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                _version++;
                Entry entry = Find(key);
                if (entry != null)
                    entry._value = value;
                else
                    UncheckedAdd(key, value);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            Entry entry = Find(key);
            if (entry != null)
            {
                value = entry._value;
                return true;
            }
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            Entry entry = Find(key);
            if (entry != null)
                throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate, key));
            _version++;
            UncheckedAdd(key, value);
        }

        public void Clear(int capacity = DefaultSize)
        {
            _version++;
            _buckets = new Entry[capacity];
            _numEntries = 0;
        }

        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            int bucket = GetBucket(key);
            Entry prev = null;
            Entry entry = _buckets[bucket];
            while (entry != null)
            {
                if (_comparer.Equals(key, entry._key))
                {
                    if (prev == null)
                    {
                        _buckets[bucket] = entry._next;
                    }
                    else
                    {
                        prev._next = entry._next;
                    }
                    _version++;
                    _numEntries--;
                    return true;
                }

                prev = entry;
                entry = entry._next;
            }
            return false;
        }

        private Entry Find(TKey key)
        {
            int bucket = GetBucket(key);
            Entry entry = _buckets[bucket];
            while (entry != null)
            {
                if (_comparer.Equals(key, entry._key))
                    return entry;

                entry = entry._next;
            }
            return null;
        }

        private Entry UncheckedAdd(TKey key, TValue value)
        {
            Entry entry = new Entry();
            entry._key = key;
            entry._value = value;

            int bucket = GetBucket(key);
            entry._next = _buckets[bucket];
            _buckets[bucket] = entry;

            _numEntries++;
            if (_numEntries > (_buckets.Length * 2))
                ExpandBuckets();

            return entry;
        }


        private void ExpandBuckets()
        {
            try
            {
                int newNumBuckets = _buckets.Length * 2 + 1;
                Entry[] newBuckets = new Entry[newNumBuckets];
                for (int i = 0; i < _buckets.Length; i++)
                {
                    Entry entry = _buckets[i];
                    while (entry != null)
                    {
                        Entry nextEntry = entry._next;

                        int bucket = GetBucket(entry._key, newNumBuckets);
                        entry._next = newBuckets[bucket];
                        newBuckets[bucket] = entry;

                        entry = nextEntry;
                    }
                }
                _buckets = newBuckets;
            }
            catch (OutOfMemoryException)
            {
            }
        }

        private int GetBucket(TKey key, int numBuckets = 0)
        {
            int h = _comparer.GetHashCode(key);
            h &= 0x7fffffff;
            return (h % (numBuckets == 0 ? _buckets.Length : numBuckets));
        }

        private sealed class Entry
        {
            public TKey _key;
            public TValue _value;
            public Entry _next;
        }

        private Entry[] _buckets;
        private int _numEntries;
        private int _version;
        private IEqualityComparer<TKey> _comparer;

        // This comparator is used if no comparator is supplied. It emulates the behavior of EqualityComparer<T>.Default.
        private sealed class DefaultComparer<T> : IEqualityComparer<T>
        {
            public bool Equals(T x, T y)
            {
                if (x == null)
                    return y == null;
                IEquatable<T> iequatable = x as IEquatable<T>;
                if (iequatable != null)
                    return iequatable.Equals(y);
                return ((object)x).Equals(y);
            }

            public int GetHashCode(T obj)
            {
                return ((object)obj).GetHashCode();
            }
        }
    }
}
