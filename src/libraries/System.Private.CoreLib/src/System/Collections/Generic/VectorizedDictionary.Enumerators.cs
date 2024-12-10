// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic {
    public partial class Dictionary<TKey, TValue> {
        public sealed class KeyCollection : ICollection<TKey>, ICollection {
            public readonly Dictionary<TKey, TValue> Dictionary;

            public struct Enumerator : IEnumerator<TKey> {
                private Dictionary<TKey, TValue>.Enumerator Inner;

                public TKey Current => Inner.CurrentKey;
                object? IEnumerator.Current => Inner.CurrentKey;

                internal Enumerator (Dictionary<TKey, TValue> dictionary) {
                    Inner = dictionary.GetEnumerator();
                }

                public void Dispose () =>
                    Inner.Dispose();

                public bool MoveNext () =>
                    Inner.MoveNext();

                public void Reset () =>
                    Inner.Reset();
            }

            internal KeyCollection (Dictionary<TKey, TValue> dictionary) {
                Dictionary = dictionary;
            }

            public int Count => Dictionary.Count;
            bool ICollection<TKey>.IsReadOnly => true;

            void ICollection<TKey>.Add (TKey item) =>
                ThrowInvalidOperation();

            void ICollection<TKey>.Clear () =>
                Dictionary.Clear();

            public bool Contains (TKey item) =>
                Dictionary.ContainsKey(item);

            public void CopyTo (TKey[] array, int arrayIndex) {
                // FIXME: Use EnumerateBuckets
                using (var e = GetEnumerator())
                    while (e.MoveNext())
                        array[arrayIndex++] = e.Current;
            }

            public Enumerator GetEnumerator () =>
                new Enumerator(Dictionary);

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator () =>
                GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator () =>
                GetEnumerator();

            bool ICollection<TKey>.Remove (TKey item) =>
                Dictionary.Remove(item);

            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => Dictionary;
            void ICollection.CopyTo(System.Array array, int index) {
                // FIXME: Use EnumerateBuckets
                using (var e = GetEnumerator())
                    while (e.MoveNext())
                        array.SetValue(e.Current, index++);
            }
        }

        public sealed class ValueCollection : ICollection<TValue>, ICollection {
            public readonly Dictionary<TKey, TValue> Dictionary;

            public struct Enumerator : IEnumerator<TValue> {
                private Dictionary<TKey, TValue>.Enumerator Inner;

                public TValue Current => Inner.CurrentValue!;
                object? IEnumerator.Current => Inner.CurrentValue;

                internal Enumerator (Dictionary<TKey, TValue> dictionary) {
                    Inner = dictionary.GetEnumerator();
                }

                public void Dispose () =>
                    Inner.Dispose();

                public bool MoveNext () =>
                    Inner.MoveNext();

                public void Reset () =>
                    Inner.Reset();
            }

            internal ValueCollection (Dictionary<TKey, TValue> dictionary) {
                Dictionary = dictionary;
            }

            public int Count => Dictionary.Count;
            bool ICollection<TValue>.IsReadOnly => true;

            void ICollection<TValue>.Add (TValue? item) =>
                ThrowInvalidOperation();

            void ICollection<TValue>.Clear () =>
                Dictionary.Clear();

            // FIXME
            bool ICollection<TValue>.Contains (TValue? item) =>
                Dictionary.ContainsValue(item);

            public void CopyTo (TValue[] array, int index) {
                // FIXME: Use EnumerateBuckets
                using (var e = GetEnumerator())
                    while (e.MoveNext())
                        array[index++] = e.Current;
            }

            public Enumerator GetEnumerator () =>
                new Enumerator(Dictionary);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator () =>
                GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator () =>
                GetEnumerator();

            bool ICollection<TValue>.Remove (TValue? item) =>
                throw new InvalidOperationException();

            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => Dictionary;
            void ICollection.CopyTo(System.Array array, int index) {
                // FIXME: Use EnumerateBuckets
                using (var e = GetEnumerator())
                    while (e.MoveNext())
                        array.SetValue(e.Current, index++);
            }
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator {
            public readonly Dictionary<TKey, TValue> Dictionary;

            private int _bucketIndex, _valueIndexLocal;
            // NOTE: Copying the bucket as we enter it means that concurrent modification during enumeration is completely safe;
            //  the old contents of the bucket will be observed instead of a mix of modified and unmodified bucket items, and
            //  removing or adding items during iteration of the bucket won't cause issues.
            // You still shouldn't mutate the collection while enumerating it though!
            private Bucket _currentBucket;
            private Bucket[] _buckets;

            public TKey CurrentKey {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    return _currentBucket.Pairs[_valueIndexLocal].Key;
                }
            }

            public TValue? CurrentValue {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    return _currentBucket.Pairs[_valueIndexLocal].Value;
                }
            }

            public KeyValuePair<TKey, TValue> Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    ref var pair = ref _currentBucket.Pairs[_valueIndexLocal];
                    return new KeyValuePair<TKey, TValue>(pair.Key, pair.Value!);
                }
            }
            object IEnumerator.Current => Current;

            DictionaryEntry IDictionaryEnumerator.Entry {
                get {
                    ref var pair = ref _currentBucket.Pairs[_valueIndexLocal];
                    return new DictionaryEntry(pair.Key, pair.Value);
                }
            }

            object IDictionaryEnumerator.Key => CurrentKey;

            object? IDictionaryEnumerator.Value => CurrentValue;

            public Enumerator (Dictionary<TKey, TValue> dictionary) {
                Dictionary = dictionary;
                _bucketIndex = -1;
                _valueIndexLocal = BucketSizeI;
                _buckets = dictionary._Buckets;
            }

            public void Dispose () {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext () {
                _valueIndexLocal++;

                while (_bucketIndex < _buckets.Length) {
                    var count = _currentBucket.Count;
                    if (_valueIndexLocal >= count) {
                        _valueIndexLocal = 0;
                        _bucketIndex++;
                        if (_bucketIndex >= _buckets.Length)
                            return false;
                        _currentBucket = _buckets[_bucketIndex];
                    }

                    while (_valueIndexLocal < count) {
                        var suffix = _currentBucket.GetSlot(_valueIndexLocal);
                        if (suffix != 0)
                            return true;
                        _valueIndexLocal++;
                    }
                }

                return false;
            }

            public void Reset () {
                _bucketIndex = -1;
                _valueIndexLocal = BucketSizeI;
                _buckets = Dictionary._Buckets;
            }
        }

        public ref struct RefEnumerator {
            private int _bucketIndex, _valueIndexLocal;
            // FIXME: Make a copy of the current bucket as we walk, so that modification during enumeration isn't hazardous?
            private ref Bucket _currentBucket;
            private ref Pair _currentPair;
            private Span<Bucket> _buckets;

            public ref readonly TKey CurrentKey {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    return ref _currentPair.Key;
                }
            }

            public ref TValue? CurrentValue {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    return ref _currentPair.Value;
                }
            }

            public KeyValuePair<TKey, TValue> Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    ref var pair = ref _currentPair;
                    return new KeyValuePair<TKey, TValue>(pair.Key, pair.Value!);
                }
            }

            public RefEnumerator (Dictionary<TKey, TValue> dictionary) {
                _bucketIndex = -1;
                _valueIndexLocal = BucketSizeI;
                _buckets = dictionary._Buckets;
                _currentPair = ref Unsafe.NullRef<Pair>();
                _currentBucket = ref Unsafe.NullRef<Bucket>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext () {
                _valueIndexLocal++;

                while (_bucketIndex < _buckets.Length) {
                    var count = Unsafe.IsNullRef(in _currentBucket) ? 0 : _currentBucket.Count;
                    if (_valueIndexLocal >= count) {
                        _valueIndexLocal = 0;
                        _bucketIndex++;
                        if (_bucketIndex >= _buckets.Length)
                            return false;
                        _currentBucket = ref _buckets[_bucketIndex];
                    }

                    while (_valueIndexLocal < count) {
                        var suffix = _currentBucket.GetSlot(_valueIndexLocal);
                        if (suffix != 0) {
                            _currentPair = ref _currentBucket.Pairs[_valueIndexLocal];
                            return true;
                        }
                        _valueIndexLocal++;
                    }
                }

                return false;
            }
        }
    }
}
