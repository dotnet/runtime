// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Performs a murmur3 finalization mix on hashcodes before using them, for collision resistance
// #define PERMUTE_HASH_CODES

using System;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Serialization;

namespace System.Collections.Generic {
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public partial class Dictionary<TKey, TValue> :
        IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>,
        ICollection<KeyValuePair<TKey, TValue>>, ICloneable, ISerializable,
        IDeserializationCallback
        where TKey : notnull
    {
        // constants for serialization
        private const string VersionName = "Version"; // Do not rename (binary serialization)
        private const string HashSizeName = "HashSize"; // Do not rename (binary serialization). Must save buckets.Length
        private const string KeyValuePairsName = "KeyValuePairs"; // Do not rename (binary serialization)
        private const string ComparerName = "Comparer"; // Do not rename (binary serialization)

        private static class Statics {
    #pragma warning disable CA1825
            // HACK: Move this readonly field out of the dictionary type so it doesn't have a cctor.
            // This removes a cctor check from some hot paths (I think?)
            // HACK: All empty SimdDictionary instances share a single-bucket EmptyBuckets array, so that Find and Remove
            //  operations don't need to do a (_Count == 0) check. This also makes some other uses of ref and MemoryMarshal
            //  safe-by-definition instead of fragile, since we always have a valid reference to the "first" bucket, even when
            //  we're empty.
            public static readonly Bucket[] EmptyBuckets = new Bucket[1];
    #pragma warning restore CA1825
        }

        public IEqualityComparer<TKey>? Comparer { get; private set; }

        private KeyCollection? _Keys;
        private ValueCollection? _Values;

        public KeyCollection Keys => _Keys ??= new KeyCollection(this);
        public ValueCollection Values => _Values ??= new ValueCollection(this);
        // These optimize for the scenario where someone uses IDictionary.Keys or IDictionary<K, V>.Keys. Normally this
        //  would have to box the KeyCollection/ValueCollection structs on demand, so we cache the boxed version of them
        //  in these fields to get rid of the per-use allocation. Most application scenarios will never allocate these.
        private ICollection<TKey>? _BoxedKeys;
        private ICollection<TValue>? _BoxedValues;
        // It's important for an empty dictionary to have both count and capacity be 0
        private int _Count, _Capacity;
        private /* volatile */ ulong _fastModMultiplier;

        private Bucket[] _Buckets = Statics.EmptyBuckets;

        public Dictionary ()
            : this (InitialCapacity, null) {
        }

        public Dictionary (int capacity)
            : this (capacity, null) {
        }

        public Dictionary (IEqualityComparer<TKey>? comparer)
            : this (InitialCapacity, comparer) {
        }

        public Dictionary (int capacity, IEqualityComparer<TKey>? comparer) {
            if (typeof(TKey).IsValueType)
                Comparer = comparer;
            // HACK: DefaultEqualityComparer<K> for string is really bad
            else if (typeof(TKey) == typeof(string))
                Comparer = comparer ?? (IEqualityComparer<TKey>)StringComparer.Ordinal;
            else
                Comparer = comparer ?? EqualityComparer<TKey>.Default;
            EnsureCapacity(capacity);
        }

        public Dictionary (Dictionary<TKey, TValue> source) {
            Comparer = source.Comparer;
            _Count = source._Count;
            _Capacity = source._Capacity;
            _fastModMultiplier = source._fastModMultiplier;
            if (source._Buckets != Statics.EmptyBuckets) {
                _Buckets = new Bucket[source._Buckets.Length];
                Array.Copy(source._Buckets, _Buckets, source._Buckets.Length);
            }
        }

        // [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Dictionary(SerializationInfo info, StreamingContext context)
        {
            // We can't do anything with the keys and values until the entire graph has been deserialized
            // and we have a resonable estimate that GetHashCode is not going to fail.  For the time being,
            // we'll just cache this.  The graph is not valid until OnDeserialization has been called.
            // HashHelpers.SerializationInfoTable.Add(this, info);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity (int capacity) {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            else if (capacity == 0)
                return;

            if (Capacity >= capacity)
                return;

            int nextIncrement = (_Buckets == Statics.EmptyBuckets)
                ? capacity
                : Capacity * 2;

            Resize(Math.Max(capacity, nextIncrement));
        }

        private void Resize (int capacity) {
            if (capacity < Count)
                ThrowInvalidOperation();

            // FIXME: Implement explicit size of zero
            if (capacity == 0) {
                _Capacity = 0;
                _fastModMultiplier = 0;
                _Buckets = Statics.EmptyBuckets;
            }

            var oldBuckets = _Buckets;
            int bucketCount, adjustedCapacityRequest,
                usableCapacity, padding;
            ulong fastModMultiplier;

            // FIXME: It should be possible to calculate this with ints, but I can't get it to work reliably.
            checked {
                adjustedCapacityRequest = capacity + (int)((long)(capacity * OversizePercentage) / 100);
                if (adjustedCapacityRequest < 1)
                    adjustedCapacityRequest = 1;

                bucketCount = (int)((long)(adjustedCapacityRequest + BucketSizeI - 1) / BucketSizeI);

                bucketCount = bucketCount > 1 ? HashHelpers.GetPrime(bucketCount) : 1;
                // Nothing to do if the bucket count hasn't changed.
                if ((oldBuckets != Statics.EmptyBuckets) && (bucketCount == oldBuckets.Length))
                    return;
                fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)bucketCount);
            }

            var slotCount = bucketCount * BucketSizeI;
            checked {
                padding = (int)((long)slotCount * OversizePercentage / 100);
                usableCapacity = slotCount - padding;
            }

            // FIXME: I don't know what I did wrong to cause this to happen, but it does. I've tried a few different ways
            //  of calculating the oversize factor and all of them have this problem.
            if (usableCapacity < capacity)
                // throw new Exception($"Oversize percentage broke allocation size of {capacity}: usable={usableCapacity}, adjustedRequest={adjustedCapacityRequest}, bucketCount={bucketCount}, padding={padding}");
                usableCapacity = capacity;

            // Allocate new array before updating fields so that we don't get corrupted when running out of memory
            var newBuckets = new Bucket[bucketCount];
            _Buckets = newBuckets;
            // HACK: Ensure we store a new larger bucket array before storing the larger fastModMultiplier and capacity.
            // This ensures that concurrent modification will not produce a bucket index that is too big.
            Interlocked.MemoryBarrier();
            _fastModMultiplier = fastModMultiplier;
            _Capacity = usableCapacity;

            // FIXME: In-place rehashing
            if ((oldBuckets != Statics.EmptyBuckets) && (_Count > 0)) {
                var c = new RehashCallback(this);
                EnumeratePairs(oldBuckets, ref c);
            }
        }

        public void TrimExcess () {
            Resize(_Count);
        }

        // FIXME: What does this actually do? The docs don't make it clear. Is it just Resize(capacity) and it throws if
        //  you have too many items to fit?
        public void TrimExcess (int capacity) =>
            throw new NotImplementedException();

        private readonly struct RehashCallback : IPairCallback {
            public readonly Dictionary<TKey, TValue> Self;

            public RehashCallback (Dictionary<TKey, TValue> self) {
                Self = self;
            }

            public bool Pair (ref Pair pair) {
                Self.TryInsert(pair.Key, pair.Value, InsertMode.Rehashing, out var result);
                if (result != InsertResult.OkAddedNew)
                    ThrowCorrupted();
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint FinalizeHashCode (uint hashCode) {
            // TODO: Use static interface methods to determine whether we need to finalize the hash for type K.
            // For BCL types like int32/int64, we need to, but for types with strong hashes like string, we don't,
            //  and for custom comparers, we don't need to do it since the caller is kind of responsible for
            //  bringing their own quality hash if they want good performance.
            // Doing this would improve best-case performance for key types like Object or String.
#if PERMUTE_HASH_CODES
            // MurmurHash3 was written by Austin Appleby, and is placed in the public
            // domain. The author hereby disclaims copyright to this source code.
            // Finalization mix - force all bits of a hash block to avalanche
            unchecked {
                hashCode ^= hashCode >> 16;
                hashCode *= 0x85ebca6b;
                hashCode ^= hashCode >> 13;
                hashCode *= 0xc2b2ae35;
                hashCode ^= hashCode >> 16;
            }
#endif
            return hashCode;
        }

        // The hash suffix is selected from 8 bits of the hash, and then modified to ensure
        //  it is never zero (because a zero suffix indicates an empty slot.)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetHashSuffix (uint hashCode) {
            // We could shift by 24 bits to take the other end of the value, but taking the low 8
            //  bits produces better results for the common scenario where you're using sequential
            //  integers as keys (since their default hash is the identity function).
            var result = unchecked((byte)hashCode);
            // Assuming the JIT turns this into a cmov, this should be better than a bitwise or
            //  since it nearly doubles the number of possible suffixes, improving collision
            //  resistance and reducing the odds of having to check multiple keys.
            return result == 0 ? (byte)255 : result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int BucketIndexForHashCode (uint hashCode, Span<Bucket> buckets) =>
            // NOTE: If the caller observes a new _fastModMultiplier before seeing a larger buckets array,
            //  this can overrun the end of the array.
            unchecked((int)HashHelpers.FastMod(
                hashCode, (uint)buckets.Length,
                // Volatile.Read to ensure that the load of _fastModMultiplier can't get moved before load of _Buckets
                // This doesn't appear to generate a memory barrier or anything.
                // FIXME
                // Volatile.Read(ref _fastModMultiplier)
                _fastModMultiplier
            ));

        // Internal for access from CollectionsMarshal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref Pair FindKey (TKey key) {
            var comparer = Comparer;
            if (typeof(TKey).IsValueType && (comparer == null))
                return ref FindKey<DefaultComparerKeySearcher>(key, null);
            else
                return ref FindKey<ComparerKeySearcher>(key, comparer);
        }

        // Internal for access from CollectionsMarshal
#pragma warning disable CS8619
        internal ref TValue? FindValue (TKey key) {
            ref var pair = ref FindKey(key);
            if (Unsafe.IsNullRef(ref pair))
                return ref Unsafe.NullRef<TValue>();
            else
                return ref pair.Value;
        }
#pragma warning restore CS8619

        // Performance is much worse unless this method is inlined, I'm not sure why.
        // If we disable inlining for it, our generated code size is dramatically reduced.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref Pair FindKey<TKeySearcher> (TKey key, IEqualityComparer<TKey>? comparer)
            where TKeySearcher : struct, IKeySearcher
        {
            var hashCode = TKeySearcher.GetHashCode(comparer, key);
            var suffix = GetHashSuffix(hashCode);
            // We eagerly create the search vector here before we need it, because in many cases it would get LICM'd here
            //  anyway. On some architectures create's latency is very low but on others it isn't, so on average it is better
            //  to put it outside of the loop.
            var searchVector = Vector128.Create(suffix);
            ref var bucket = ref NewEnumerator(hashCode, out var enumerator);
            do {
                // Eagerly load the bucket count early for pipelining purposes, so we don't stall when using it later.
                int bucketCount = bucket.Count,
                    startIndex = FindSuffixInBucket(ref bucket, searchVector, bucketCount);
                ref var pair = ref TKeySearcher.FindKeyInBucket(ref bucket, startIndex, bucketCount, comparer, key, out _);
                if (Unsafe.IsNullRef(ref pair)) {
                    if (bucket.CascadeCount == 0)
                        return ref Unsafe.NullRef<Pair>();
                } else
                    return ref pair;
                bucket = ref enumerator.Advance();
            } while (!Unsafe.IsNullRef(ref bucket));

            return ref Unsafe.NullRef<Pair>();
        }

        // Internal for access from CollectionsMarshal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref Pair TryInsert (TKey key, TValue? value, InsertMode mode, out InsertResult result) {
            var comparer = Comparer;
            if (typeof(TKey).IsValueType && (comparer == null))
                return ref TryInsert<DefaultComparerKeySearcher>(key, value, mode, null, out result);
            else
                return ref TryInsert<ComparerKeySearcher>(key, value, mode, comparer, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Pair TryInsertIntoBucket (ref Bucket bucket, byte suffix, int bucketCount, TKey key, TValue? value) {
            if (bucketCount >= BucketSizeI)
                return ref Unsafe.NullRef<Pair>();

            unchecked {
                ref var destination = ref Unsafe.Add(ref bucket.Pairs.Pair0, bucketCount);
                bucket.Count = (byte)(bucketCount + 1);
                bucket.SetSlot((nuint)bucketCount, suffix);
                destination.Key = key;
                destination.Value = value;
                return ref destination;
            }
        }

        // Inlining required for acceptable codegen
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref Pair TryInsert<TKeySearcher> (TKey key, TValue? value, InsertMode mode, IEqualityComparer<TKey>? comparer, out InsertResult result)
            where TKeySearcher : struct, IKeySearcher
        {
            var needToGrow = (_Count >= _Capacity);
            var hashCode = TKeySearcher.GetHashCode(comparer, key);
            var suffix = GetHashSuffix(hashCode);
            var searchVector = Vector128.Create(suffix);
            // Pipelining: Perform the actual branch later, since in the common case we won't need to grow.
            if (needToGrow) {
                result = InsertResult.NeedToGrow;
                return ref Unsafe.NullRef<Pair>();
            }

            ref var bucket = ref NewEnumerator(hashCode, out var enumerator);
            do {
                int bucketCount = bucket.Count;
                if (mode != InsertMode.Rehashing) {
                    int startIndex = FindSuffixInBucket(ref bucket, searchVector, bucketCount);
                    ref var pair = ref TKeySearcher.FindKeyInBucket(ref bucket, startIndex, bucketCount, comparer, key, out _);

                    if (!Unsafe.IsNullRef(ref pair)) {
                        if (mode == InsertMode.EnsureUnique) {
                            result = InsertResult.KeyAlreadyPresent;
                            return ref pair;
                        } else {
                            pair.Value = value;
                            result = InsertResult.OkOverwroteExisting;
                            return ref pair;
                        }
                    } else if (startIndex < BucketSizeI) {
                        ;
                        // FIXME: Suffix collision. Track these for string rehashing anti-DoS mitigation!
                    }
                }

                ref var insertLocation = ref TryInsertIntoBucket(ref bucket, suffix, bucketCount, key, value);
                if (!Unsafe.IsNullRef(ref insertLocation)) {
                    // Increase the cascade counters for the buckets we checked before this one.
                    AdjustCascadeCounts(enumerator, true);

                    result = InsertResult.OkAddedNew;
                    return ref insertLocation;
                }

                bucket = ref enumerator.Advance();
            } while (!Unsafe.IsNullRef(ref bucket));

            result = InsertResult.CorruptedInternalState;
            return ref Unsafe.NullRef<Pair>();
        }

#pragma warning disable CS8601
        // Inlining required for disasmo
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove (TKey key) {
            var comparer = Comparer;
            // It's legal to pass Unsafe.NullRef as the destination address here because TryRemove handles it explicitly.
            if (typeof(TKey).IsValueType && (comparer == null))
                return TryRemove<DefaultComparerKeySearcher>(key, null, out Unsafe.NullRef<TValue>());
            else
                return TryRemove<ComparerKeySearcher>(key, comparer, out Unsafe.NullRef<TValue>());
        }

        public bool Remove (TKey key, out TValue value) {
            var comparer = Comparer;
            if (typeof(TKey).IsValueType && (comparer == null))
                return TryRemove<DefaultComparerKeySearcher>(key, null, out value);
            else
                return TryRemove<ComparerKeySearcher>(key, comparer, out value);
        }
#pragma warning restore CS8601

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RemoveFromBucket (ref Bucket bucket, int indexInBucket, int bucketCount, ref Pair toRemove) {
            Debug.Assert(bucketCount > 0);
            unchecked {
                int replacementIndexInBucket = bucketCount - 1;
                bucket.Count = (byte)replacementIndexInBucket;
                ref var replacement = ref Unsafe.Add(ref bucket.Pairs.Pair0, replacementIndexInBucket);
                // This rotate-back algorithm makes removes more expensive than if we were to just always zero the slot.
                // But then other algorithms like insertion get more expensive, since we have to search for a zero to replace...
                if (!Unsafe.AreSame(ref toRemove, ref replacement)) {
                    // TODO: This is the only place in the find/insert/remove algorithms that actually needs indexInBucket.
                    // Can we refactor it away? The good news is RyuJIT optimizes it out entirely in find/insert.
                    bucket.SetSlot((uint)indexInBucket, bucket.GetSlot(replacementIndexInBucket));
                    bucket.SetSlot((uint)replacementIndexInBucket, 0);
                    toRemove = replacement;
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<Pair>())
                        replacement = default;
                } else {
                    bucket.SetSlot((uint)indexInBucket, 0);
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<Pair>())
                        toRemove = default;
                }
            }
        }

        // Don't force inlining (to reduce code size), since Remove has two overloads that inline this 1-2 times each
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryRemove<TKeySearcher> (TKey key, IEqualityComparer<TKey>? comparer, out TValue? value)
            where TKeySearcher : struct, IKeySearcher
        {
            // HACK: It is legal to pass a NullRef as the out-address for value
            // This reduces code duplication for the two different overloads of Remove.
            Unsafe.SkipInit(out value);

            var hashCode = TKeySearcher.GetHashCode(comparer, key);
            var suffix = GetHashSuffix(hashCode);
            var searchVector = Vector128.Create(suffix);
            ref var bucket = ref NewEnumerator(hashCode, out var enumerator);
            do {
                int bucketCount = bucket.Count,
                    startIndex = FindSuffixInBucket(ref bucket, searchVector, bucketCount);
                ref var pair = ref TKeySearcher.FindKeyInBucket(ref bucket, startIndex, bucketCount, comparer, key, out int indexInBucket);

                if (!Unsafe.IsNullRef(ref pair)) {
                    if (!Unsafe.IsNullRef(ref value))
                        value = pair.Value;
                    _Count--;
                    RemoveFromBucket(ref bucket, indexInBucket, bucketCount, ref pair);
                    // If we had to check multiple buckets before we found the match, go back and decrement cascade counters.
                    AdjustCascadeCounts(enumerator, false);
                    return true;
                }

                // Important: If the cascade counter is 0 and we didn't find the item, we don't want to check any other buckets.
                // Otherwise, we'd scan the whole table fruitlessly looking for a matching key.
                if (bucket.CascadeCount == 0) {
                    if (!Unsafe.IsNullRef(ref value))
                        value = default!;
                    return false;
                }

                bucket = ref enumerator.Advance();
            } while (!Unsafe.IsNullRef(ref bucket));

            if (!Unsafe.IsNullRef(ref value))
                value = default!;
            return false;
        }

        public TValue this[TKey key] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                ref var pair = ref FindKey(key);
                if (Unsafe.IsNullRef(ref pair))
                    throw new KeyNotFoundException($"Key not found: {key}");
                return pair.Value!;
            }
            set {
            retry:
                TryInsert(key, value, InsertMode.OverwriteValue, out var result);
                switch (result) {
                    case InsertResult.OkAddedNew:
                        _Count++;
                        return;
                    case InsertResult.NeedToGrow:
                        EnsureCapacity(_Count + 1);
                        goto retry;
                    case InsertResult.CorruptedInternalState:
                        throw new Exception("Corrupted internal state");
                }
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => (_BoxedKeys ??= Keys);
        ICollection<TValue> IDictionary<TKey, TValue>.Values => (_BoxedValues ??= Values);

        public int Count => _Count;
        public int Capacity => _Capacity;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => false;

        ICollection IDictionary.Keys => (ICollection)(_BoxedKeys ??= Keys);

        ICollection IDictionary.Values => (ICollection)(_BoxedValues ??= Values);

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => (_BoxedKeys ??= Keys);

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => (_BoxedValues ??= Values);

        object? IDictionary.this[object key] {
            get => this[(TKey)key];
#pragma warning disable CS8600
#pragma warning disable CS8601
            set => this[(TKey)key] = (TValue)value;
#pragma warning restore CS8600
#pragma warning restore CS8601
        }

        public void Add (TKey key, TValue? value) {
            var ok = TryAdd(key, value);
            if (!ok)
                throw new ArgumentException($"Key already exists: {key}");
        }

        // Inlining required for disasmo
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd (TKey key, TValue? value) {
        retry:
            TryInsert(key, value, InsertMode.EnsureUnique, out var result);
            switch (result) {
                case InsertResult.OkAddedNew:
                    _Count++;
                    return true;
                case InsertResult.NeedToGrow:
                    EnsureCapacity(_Count + 1);
                    goto retry;
                case InsertResult.CorruptedInternalState:
                    throw new Exception("Corrupted internal state");
                default:
                    return false;
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add (KeyValuePair<TKey, TValue> item) =>
            Add(item.Key, item.Value);

        private readonly struct ClearCallback : IBucketCallback {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Bucket (ref Bucket bucket) {
                int c = bucket.Count;
                if (c == 0) {
                    bucket.CascadeCount = 0;
                    return true;
                }

                bucket.Suffixes = default;
                if (RuntimeHelpers.IsReferenceOrContainsReferences<Pair>()) {
                    // FIXME: Performs a method call for the clear instead of being inlined
                    // var pairs = (Span<Pair>)bucket.Pairs;
                    // pairs.Clear();
                    ref var pair = ref bucket.Pairs.Pair0;
                    // 4-wide unrolled bucket clear
                    while (c >= 4) {
                        pair = default;
                        Unsafe.Add(ref pair, 1) = default;
                        Unsafe.Add(ref pair, 2) = default;
                        Unsafe.Add(ref pair, 3) = default;
                        pair = ref Unsafe.Add(ref pair, 4);
                        c -= 4;
                    }
                    while (c != 0) {
                        pair = default;
                        pair = ref Unsafe.Add(ref pair, 1);
                        c--;
                    }
                }

                return true;
            }
        }

        // NOTE: In benchmarks this looks much slower than SCG clear, but that's because our backing array at 4096 is
        //  much bigger than SCG's, so we're just measuring how much slower Array.Clear is on a bigger array
        public void Clear () {
            if (_Count == 0)
                return;

            _Count = 0;
            // FIXME: Only do this if _Count is below say 0.5x?
            ClearCallback c = default!;
            EnumerateBuckets(_Buckets, ref c);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains (KeyValuePair<TKey, TValue> item) {
            ref var pair = ref FindKey(item.Key);
            return !Unsafe.IsNullRef(ref pair) && (pair.Value?.Equals(item.Value) == true);
        }

        public bool ContainsKey (TKey key) =>
            !Unsafe.IsNullRef(ref FindKey(key));

        private struct ContainsValueCallback : IPairCallback {
            public readonly TValue? Value;
            public bool Result;

            public ContainsValueCallback (TValue? value) {
                Value = value;
                Result = false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Pair (ref Pair pair) {
                if (EqualityComparer<TValue>.Default.Equals(pair.Value, Value)) {
                    Result = true;
                    return false;
                }
                return true;
            }
        }

        public bool ContainsValue (TValue? value) {
            if (_Count == 0)
                return false;

            var callback = new ContainsValueCallback(value);
            EnumeratePairs(_Buckets, ref callback);
            return callback.Result;
        }

        private struct ForEachImpl : IPairCallback {
            private readonly ForEachCallback Callback;
            private int Index;

            public ForEachImpl (ForEachCallback callback) {
                Callback = callback;
                Index = 0;
            }

            public bool Pair (ref Pair pair) {
                return Callback(Index++, in pair.Key, ref pair.Value);
            }
        }

        public void ForEach (ForEachCallback callback) {
            var state = new ForEachImpl(callback);
            EnumeratePairs(_Buckets, ref state);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo (KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            CopyToArray(array, arrayIndex);
        }

        public Enumerator GetEnumerator () =>
            new Enumerator(this);

        public RefEnumerator GetRefEnumerator () =>
            new RefEnumerator(this);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator () =>
            GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator () =>
            GetEnumerator();

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove (KeyValuePair<TKey, TValue> item) =>
            // FIXME: Check value
            Remove(item.Key);

        // Inlining required for disasmo
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue (TKey key, out TValue value) {
            ref var pair = ref FindKey(key);
            if (Unsafe.IsNullRef(ref pair)) {
                value = default!;
                return false;
            } else {
                value = pair.Value!;
                return true;
            }
        }

        public TValue AddOrUpdate (TKey key, TValue? addValue, Func<TKey, TValue, TValue> updateValueFactory) {
retry:
            ref var pair = ref TryInsert(key, addValue, InsertMode.EnsureUnique, out var result);
            switch (result) {
                case InsertResult.NeedToGrow:
                    EnsureCapacity(Count + 1);
                    goto retry;
                case InsertResult.KeyAlreadyPresent:
                    return (pair.Value = updateValueFactory(key, pair.Value!))!;
                case InsertResult.OkAddedNew:
                    return addValue!;
                default:
                    ThrowConcurrentModification();
                    return default!;
            }
        }

        public TValue GetOrAdd (TKey key, Func<TKey, TValue> valueFactory) {
            // We insert a placeholder if the key is not already present, then overwrite the placeholder.
            // This is faster than doing two passes.
retry:
            ref var pair = ref TryInsert(key, default!, InsertMode.EnsureUnique, out var result);
            switch (result) {
                case InsertResult.NeedToGrow:
                    EnsureCapacity(Count + 1);
                    goto retry;
                case InsertResult.KeyAlreadyPresent:
                    return pair.Value!;
                case InsertResult.OkAddedNew:
                    return (pair.Value = valueFactory(key))!;
                default:
                    ThrowConcurrentModification();
                    return default!;
            }
        }

        public object Clone () =>
            new Dictionary<TKey, TValue>(this);

        private struct CopyToKvp : IPairCallback {
            public readonly KeyValuePair<TKey, TValue>[] Array;
            public int Index;

            public CopyToKvp (KeyValuePair<TKey, TValue>[] array, int index) {
                Array = array;
                Index = index;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Pair (ref Pair pair) {
                Array[Index++] = new KeyValuePair<TKey, TValue>(pair.Key, pair.Value!);
                return true;
            }
        }

        private struct CopyToDictionaryEntry : IPairCallback {
            public readonly DictionaryEntry[] Array;
            public int Index;

            public CopyToDictionaryEntry (DictionaryEntry[] array, int index) {
                Array = array;
                Index = index;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Pair (ref Pair pair) {
                Array[Index++] = new DictionaryEntry(pair.Key, pair.Value!);
                return true;
            }
        }

        private struct CopyToObject : IPairCallback {
            public readonly object[] Array;
            public int Index;

            public CopyToObject (object[] array, int index) {
                Array = array;
                Index = index;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Pair (ref Pair pair) {
                Array[Index++] = new KeyValuePair<TKey, TValue>(pair.Key, pair.Value!);
                return true;
            }
        }

        private void CopyToArray<T> (T[] array, int index) {
            ArgumentNullException.ThrowIfNull(array);
            if ((uint)index > (uint)array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (array.Length - index < Count)
                throw new ArgumentException("Destination array too small", nameof(index));

            if (array is KeyValuePair<TKey, TValue>[] kvp) {
                var c = new CopyToKvp(kvp, index);
                EnumeratePairs(_Buckets, ref c);
            } else if (array is DictionaryEntry[] de) {
                var c = new CopyToDictionaryEntry(de, index);
                EnumeratePairs(_Buckets, ref c);
            } else if (array is object[] o) {
                var c = new CopyToObject(o, index);
                EnumeratePairs(_Buckets, ref c);
            } else
                throw new ArgumentException("Unsupported destination array type");
        }

        public void CopyTo (KeyValuePair<TKey, TValue>[] array, int index) {
            CopyToArray(array, index);
        }

        public void CopyTo (object[] array, int index) {
            CopyToArray(array, index);
        }

        private struct AnalyzeCallback : IBucketCallback {
            public int Normal, Overflowed, Degraded;

            public bool Bucket (ref Bucket bucket) {
                if (bucket.CascadeCount >= DegradedCascadeCount)
                    Degraded++;
                else if (bucket.CascadeCount == 0)
                    Normal++;
                else
                    Overflowed++;
                return true;
            }
        }

        public (int normal, int overflowed, int degraded) AnalyzeBuckets () {
            AnalyzeCallback c = default!;
            EnumerateBuckets(_Buckets, ref c);
            return (c.Normal, c.Overflowed, c.Degraded);
        }

        void IDictionary.Add (object key, object? value) =>
#pragma warning disable CS8600
#pragma warning disable CS8604
            Add((TKey)key, (TValue)value);
#pragma warning restore CS8600
#pragma warning restore CS8604

        bool IDictionary.Contains (object key) =>
            ContainsKey((TKey)key);

        IDictionaryEnumerator IDictionary.GetEnumerator () =>
            new Enumerator(this);

        void IDictionary.Remove (object key) =>
            Remove((TKey)key);

        void ICollection.CopyTo (Array array, int index) {
            if (array is KeyValuePair<TKey, TValue>[] kvpa)
                CopyTo(kvpa, 0);
            else if (array is object[] oa)
                CopyTo(oa, 0);
            else
                throw new ArgumentException("Unsupported destination array type", nameof(array));
        }

        public AlternateLookup<TAlternateKey> GetAlternateLookup<TAlternateKey> ()
            where TAlternateKey : notnull, allows ref struct
        {
            if (!TryGetAlternateLookup<TAlternateKey>(out var result))
                ThrowInvalidOperation();

            return result;
        }

        public bool TryGetAlternateLookup<TAlternateKey> (out AlternateLookup<TAlternateKey> result)
            where TAlternateKey : notnull, allows ref struct
        {
            if (Comparer is IAlternateEqualityComparer<TAlternateKey, TKey> aec) {
                result = new AlternateLookup<TAlternateKey>(this, aec);
                return true;
            }

            result = default;
            return false;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void ThrowInvalidOperation () {
            throw new InvalidOperationException();
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void ThrowCorrupted () {
            throw new Exception("Corrupted dictionary internal state detected");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void ThrowConcurrentModification () {
            throw new Exception("Concurrent modification of dictionary detected");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void ThrowKeyNotFound () {
            throw new KeyNotFoundException();
        }

        public virtual void GetObjectData (SerializationInfo info, StreamingContext context) {
            ArgumentNullException.ThrowIfNull(info);

            info.AddValue(VersionName, 0);
            info.AddValue(ComparerName, Comparer, typeof(IEqualityComparer<TKey>));
            info.AddValue(HashSizeName, Count);

            if (Count > 0) {
                var array = new KeyValuePair<TKey, TValue>[Count];
                CopyTo(array, 0);
                info.AddValue(KeyValuePairsName, array, typeof(KeyValuePair<TKey, TValue>[]));
            }
        }

        public virtual void OnDeserialization (object? sender) {
            // FIXME
            // HashHelpers.SerializationInfoTable.TryGetValue(this, out SerializationInfo? siInfo);
            SerializationInfo? siInfo = null;

            if (siInfo == null)
            {
                // We can return immediately if this function is called twice.
                // Note we remove the serialization info from the table at the end of this method.
                return;
            }

            // int realVersion = siInfo.GetInt32(VersionName);
            int hashsize = siInfo.GetInt32(HashSizeName);
            Comparer = (IEqualityComparer<TKey>)siInfo.GetValue(ComparerName, typeof(IEqualityComparer<TKey>))!; // When serialized if comparer is null, we use the default.
            if (_Count > 0)
                throw new InvalidOperationException("Dictionary not empty before deserialization");

            if (hashsize != 0)
            {
                Resize(hashsize);

                KeyValuePair<TKey, TValue>[]? array = (KeyValuePair<TKey, TValue>[]?)
                    siInfo.GetValue(KeyValuePairsName, typeof(KeyValuePair<TKey, TValue>[]));

                if (array == null)
                {
                    throw new SerializationException("Missing keys");
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Key == null)
                    {
                        throw new SerializationException("Null key");
                    }

                    Add(array[i].Key, array[i].Value);
                }
            }
            else
            {
                Resize(0);
            }

            // HashHelpers.SerializationInfoTable.Remove(this);
        }
    }
}
