// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Collections.Generic {
    public partial class Dictionary<TKey, TValue> {
        public readonly struct AlternateLookup<TAlternateKey>
            where TAlternateKey : notnull, allows ref struct {

            public readonly Dictionary<TKey, TValue> Dictionary;
            public readonly IAlternateEqualityComparer<TAlternateKey, TKey> Comparer;

            internal AlternateLookup (Dictionary<TKey, TValue> dictionary, IAlternateEqualityComparer<TAlternateKey, TKey> comparer) {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));
                if (comparer == null)
                    throw new ArgumentNullException(nameof(comparer));
                Dictionary = dictionary;
                Comparer = comparer;
            }

            public TValue this [TAlternateKey key] {
                get {
                    ref var pair = ref FindKey(key);
                    if (Unsafe.IsNullRef(ref pair))
                        throw new KeyNotFoundException();
                    return pair.Value;
                }
            }

            public bool ContainsKey (TAlternateKey key) {
                ref var pair = ref FindKey(key);
                return !Unsafe.IsNullRef(ref pair);
            }

            public bool TryGetValue (TAlternateKey key, out TValue value) {
                ref var pair = ref FindKey(key);
                if (Unsafe.IsNullRef(ref pair)) {
                    value = default!;
                    return false;
                } else {
                    value = pair.Value;
                    return true;
                }
            }

            public bool TryAdd (TAlternateKey key, TValue value) {
                // FIXME: Duplicate the TryInsert logic from SimdDictionary to avoid the Create call when there is a key collision
                return Dictionary.TryAdd(Comparer.Create(key), value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ref Pair FindKey (TAlternateKey key) {
                // This is duplicated from SimdDictionary.FindKey, look there for comments.
                var dictionary = Dictionary;

                var comparer = Comparer;
                var hashCode = FinalizeHashCode(unchecked((uint)comparer.GetHashCode(key)));
                var suffix = GetHashSuffix(hashCode);
                var searchVector = Vector128.Create(suffix);
                ref var bucket = ref dictionary.NewEnumerator(hashCode, out var enumerator);
                do {
                    int bucketCount = bucket.Count,
                        startIndex = FindSuffixInBucket(ref bucket, searchVector, bucketCount);
                    ref var pair = ref FindKeyInBucket(ref bucket, startIndex, bucketCount, comparer, key, out _);
                    if (Unsafe.IsNullRef(ref pair)) {
                        if (bucket.CascadeCount == 0)
                            return ref Unsafe.NullRef<Pair>();
                    } else
                        return ref pair;

                    bucket = ref enumerator.Advance();
                } while (!Unsafe.IsNullRef(ref bucket));

                return ref Unsafe.NullRef<Pair>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ref Pair FindKeyInBucket (
                // We have to use UnscopedRef to allow lazy initialization
                [UnscopedRef] ref Bucket bucket,
                int indexInBucket, int bucketCount, IAlternateEqualityComparer<TAlternateKey, TKey> comparer,
                TAlternateKey needle, out int matchIndexInBucket
            ) {
                Debug.Assert(indexInBucket >= 0);
                Debug.Assert(comparer != null);

                // It's impossible to properly initialize this reference until indexInBucket has been range-checked.
                ref var pair = ref Unsafe.NullRef<Pair>();
                for (; indexInBucket < bucketCount; indexInBucket++, pair = ref Unsafe.Add(ref pair, 1)) {
                    if (Unsafe.IsNullRef(ref pair))
                        pair = ref Unsafe.Add(ref bucket.Pairs.Pair0, indexInBucket);
                    if (comparer!.Equals(needle, pair.Key)) {
                        matchIndexInBucket = indexInBucket;
                        return ref pair;
                    }
                }

                matchIndexInBucket = -1;
                return ref Unsafe.NullRef<Pair>();
            }
        }
    }
}
