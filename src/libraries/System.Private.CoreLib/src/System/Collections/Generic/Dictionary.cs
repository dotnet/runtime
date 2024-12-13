// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    [DebuggerTypeProxy(typeof(IDictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class Dictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>, ISerializable, IDeserializationCallback where TKey : notnull
    {
        // A comparison protocol is an adapter to allow us to have a single implementation of all our
        //  search/insertion/removal algorithms that generalizes efficiently over different comparers at JIT
        //  or AOT time without duplicated code
        private interface IComparisonProtocol<TActualKey>
            where TActualKey : allows ref struct
        {
            int GetHashCode(TActualKey key);
            bool Equals(TActualKey lhs, TKey rhs);
            TKey GetKey(TActualKey input);
        }

        private readonly struct DefaultValueTypeComparerComparisonProtocol : IComparisonProtocol<TKey>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(TKey lhs, TKey rhs) => EqualityComparer<TKey>.Default.Equals(lhs, rhs);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(TKey key) => EqualityComparer<TKey>.Default.GetHashCode(key);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TKey GetKey(TKey input) => input;
        }

        private readonly struct ComparerComparisonProtocol
                        : IComparisonProtocol<TKey>
        {
            public readonly IEqualityComparer<TKey> comparer;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ComparerComparisonProtocol (IEqualityComparer<TKey> comparer)
            {
                Debug.Assert(comparer != null);
                this.comparer = comparer;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(TKey lhs, TKey rhs) => comparer.Equals(lhs, rhs);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(TKey key) => comparer.GetHashCode(key);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TKey GetKey(TKey input) => input;
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly struct AlternateComparerComparisonProtocol<TAlternateKey>(IAlternateEqualityComparer<TAlternateKey, TKey> comparer)
            : IComparisonProtocol<TAlternateKey>
            where TAlternateKey : allows ref struct
        {
            public readonly IAlternateEqualityComparer<TAlternateKey, TKey> comparer = comparer;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(TAlternateKey lhs, TKey rhs) => comparer.Equals(lhs, rhs);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(TAlternateKey key) => comparer.GetHashCode(key);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TKey GetKey(TAlternateKey input)
            {
                TKey result = comparer.Create(input);
                if (result == null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
                return result;
            }
        }

        /*
        static Dictionary () {
            while (!Debugger.IsAttached)
                Debugger.Launch();
        }
        */

        private ref struct LoopingBucketEnumerator
        {
            // The size of this struct is REALLY important! Adding even a single field to this will add stack spills to critical loops.
            // FIXME: This span being a field puts pressure on the JIT to do recursive struct decomposition; I'm not sure it always does
            private readonly Span<Bucket> _buckets;
            private readonly int _initialIndex;
            private int _index;

            [Obsolete("Use LoopingBucketEnumerator.New")]
            public LoopingBucketEnumerator()
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private LoopingBucketEnumerator(Span<Bucket> buckets, uint hashCode, ulong fastModMultiplier)
            {
                _buckets = buckets;
                _initialIndex = GetBucketIndexForHashCode(buckets, hashCode, fastModMultiplier);
                _index = _initialIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref Bucket New(Span<Bucket> buckets, uint hashCode, ulong fastModMultiplier, out LoopingBucketEnumerator enumerator)
            {
                // FIXME: Optimize this out with EmptyBuckets array like SimdDictionary
                if (buckets.IsEmpty)
                {
                    enumerator = default;
                    return ref Unsafe.NullRef<Bucket>();
                }
                else
                {
                    enumerator = new LoopingBucketEnumerator(buckets, hashCode, fastModMultiplier);
                    // FIXME: Optimize out the memory load of _initialIndex somehow.
                    return ref enumerator._buckets[enumerator._initialIndex];
                }
            }

            /// <summary>
            /// Walks forward through buckets, wrapping around at the end of the container.
            /// Never visits a bucket twice.
            /// </summary>
            /// <returns>The next bucket, or NullRef if you have visited every bucket exactly once.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref Bucket Advance()
            {
                // Operating on the index field directly is harmless as long as the enumerator struct got decomposed, which it seems to
                // Caching index into a local and then doing a writeback at the end increases generated code size so it's not worth it
                if (++_index >= _buckets.Length)
                    _index = 0;

                if (_index == _initialIndex)
                    return ref Unsafe.NullRef<Bucket>();
                else
                    return ref _buckets[_index];
            }

            /// <summary>
            /// Walks back through the buckets you have previously visited.
            /// </summary>
            /// <returns>Each bucket you previously visited, exactly once, in reverse order, then NullRef.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref Bucket Retreat()
            {
                if (_index == _initialIndex)
                    return ref Unsafe.NullRef<Bucket>();

                if (--_index < 0)
                    _index = _buckets.Length - 1;
                return ref _buckets[_index];
            }
        }

        [InlineArray(12)]
        [StructLayout(LayoutKind.Sequential)]
        private struct InlineEntryIndexArray
        {
            public int Index0;
        }

        private struct Bucket
        {
            public const int Capacity = 12,
                CountSlot = 13,
                CascadeSlot = 14,
                DegradedCascadeCount = 0xFF;

            public Vector128<byte> Suffixes;
            public InlineEntryIndexArray Indices;

            // This analysis is incorrect
#pragma warning disable IDE0251
            public ref byte Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref Unsafe.AddByteOffset(ref Unsafe.As<Vector128<byte>, byte>(ref Unsafe.AsRef(in Suffixes)), CountSlot);
            }

            public ref ushort CascadeCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref Unsafe.AddByteOffset(ref Unsafe.As<Vector128<byte>, ushort>(ref Unsafe.AsRef(in Suffixes)), CascadeSlot);
            }
#pragma warning restore IDE0251

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly byte GetSlot(int index)
            {
                Debug.Assert(index < Vector128<byte>.Count);
                // the extract-lane opcode this generates is slower than doing a byte load from memory,
                //  even if we already have the bucket in a register. Not sure why, but my guess based on agner's
                //  instruction tables is that it's because lane extract generates more uops than a byte move.
                // the two operations have the same latency on icelake, and the byte move's latency is lower on zen4
                // return self[index];
                // index &= 15;
                return Unsafe.AddByteOffset(ref Unsafe.As<Vector128<byte>, byte>(ref Unsafe.AsRef(in Suffixes)), index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetSlot(nuint index, byte value)
            {
                Debug.Assert(index < (nuint)Vector128<byte>.Count);
                // index &= 15;
                Unsafe.AddByteOffset(ref Unsafe.As<Vector128<byte>, byte>(ref Suffixes), index) = value;
            }

            public readonly int FindSuffix(int bucketCount, byte suffix, Vector128<byte> searchVector)
            {
                if (Sse2.IsSupported)
                {
                    return BitOperations.TrailingZeroCount(Sse2.MoveMask(Sse2.CompareEqual(searchVector, Suffixes)));
                }
                else if (AdvSimd.Arm64.IsSupported)
                {
                    // Completely untested
                    var laneBits = AdvSimd.And(
                        AdvSimd.CompareEqual(searchVector, Suffixes),
                        Vector128.Create(1, 2, 4, 8, 16, 32, 64, 128, 1, 2, 4, 8, 16, 32, 64, 128)
                    );
                    var moveMask = AdvSimd.Arm64.AddAcross(laneBits.GetLower()).ToScalar() |
                        (AdvSimd.Arm64.AddAcross(laneBits.GetUpper()).ToScalar() << 8);
                    return BitOperations.TrailingZeroCount(moveMask);
                }
                else if (PackedSimd.IsSupported)
                {
                    // Completely untested
                    return BitOperations.TrailingZeroCount(PackedSimd.Bitmask(PackedSimd.CompareEqual(searchVector, Suffixes)));
                }
                else
                {
                    return FindSuffixScalar(bucketCount, suffix);
                }
            }

            public readonly unsafe int FindSuffixScalar(int bucketCount, byte suffix)
            {
                // Hand-unrolling the search into four comparisons per loop iteration is a significant performance improvement
                //  for a moderate code size penalty (733b -> 826b; 399usec -> 321usec)
                var haystack = (byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in Suffixes));
                for (int i = 0; i < bucketCount; i += 4, haystack += 4)
                {
                    // FIXME: It's not possible to use cmovs here due to a JIT limitation (can't do cmovs in loops)
                    // A chain of cmovs would be much faster.
                    if (haystack[0] == suffix)
                        return i;
                    if (haystack[1] == suffix)
                        return i + 1;
                    if (haystack[2] == suffix)
                        return i + 2;
                    if (haystack[3] == suffix)
                        return i + 3;
                }

                return 32;
            }
        }

        // constants for serialization
        private const string VersionName = "Version"; // Do not rename (binary serialization)
        private const string HashSizeName = "HashSize"; // Do not rename (binary serialization). Must save buckets.Length
        private const string KeyValuePairsName = "KeyValuePairs"; // Do not rename (binary serialization)
        private const string ComparerName = "Comparer"; // Do not rename (binary serialization)

        private Bucket[]? _buckets;
        private Entry[]? _entries;
        //#if TARGET_64BIT
        private ulong _fastModMultiplier;
        //#endif
        private int _count;
        private int _freeList;
        private int _freeCount;
        private int _version;
        private IEqualityComparer<TKey>? _comparer;
        private KeyCollection? _keys;
        private ValueCollection? _values;
        private const int StartOfFreeList = -3;

        public Dictionary() : this(0, null) { }

        public Dictionary(int capacity) : this(capacity, null) { }

        public Dictionary(IEqualityComparer<TKey>? comparer) : this(0, comparer) { }

        public Dictionary(int capacity, IEqualityComparer<TKey>? comparer)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            if (capacity > 0)
            {
                Initialize(capacity);
            }

            // For reference types, we always want to store a comparer instance, either
            // the one provided, or if one wasn't provided, the default (accessing
            // EqualityComparer<TKey>.Default with shared generics on every dictionary
            // access can add measurable overhead).  For value types, if no comparer is
            // provided, or if the default is provided, we'd prefer to use
            // EqualityComparer<TKey>.Default.Equals on every use, enabling the JIT to
            // devirtualize and possibly inline the operation.
            if (!typeof(TKey).IsValueType)
            {
                _comparer = comparer ?? EqualityComparer<TKey>.Default;

                // Special-case EqualityComparer<string>.Default, StringComparer.Ordinal, and StringComparer.OrdinalIgnoreCase.
                // We use a non-randomized comparer for improved perf, falling back to a randomized comparer if the
                // hash buckets become unbalanced.
                if (typeof(TKey) == typeof(string) &&
                    NonRandomizedStringEqualityComparer.GetStringComparer(_comparer!) is IEqualityComparer<string> stringComparer)
                {
                    _comparer = (IEqualityComparer<TKey>)stringComparer;
                }
            }
            else if (comparer is not null && // first check for null to avoid forcing default comparer instantiation unnecessarily
                     comparer != EqualityComparer<TKey>.Default)
            {
                _comparer = comparer;
            }
        }

        public Dictionary(IDictionary<TKey, TValue> dictionary) : this(dictionary, null) { }

        public Dictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer) :
            this(dictionary?.Count ?? 0, comparer)
        {
            if (dictionary == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
            }

            AddRange(dictionary);
        }

        public Dictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, null) { }

        public Dictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer) :
            this((collection as ICollection<KeyValuePair<TKey, TValue>>)?.Count ?? 0, comparer)
        {
            if (collection == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
            }

            AddRange(collection);
        }

        private void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        {
            // It is likely that the passed-in enumerable is Dictionary<TKey,TValue>. When this is the case,
            // avoid the enumerator allocation and overhead by looping through the entries array directly.
            // We only do this when dictionary is Dictionary<TKey,TValue> and not a subclass, to maintain
            // back-compat with subclasses that may have overridden the enumerator behavior.
            if (enumerable.GetType() == typeof(Dictionary<TKey, TValue>))
            {
                Dictionary<TKey, TValue> source = (Dictionary<TKey, TValue>)enumerable;

                if (source.Count == 0)
                {
                    // Nothing to copy, all done
                    return;
                }

                // This is not currently a true .AddRange as it needs to be an initialized dictionary
                // of the correct size, and also an empty dictionary with no current entities (and no argument checks).
                Debug.Assert(source._entries is not null);
                Debug.Assert(_entries is not null);
                Debug.Assert(_entries.Length >= source.Count);
                Debug.Assert(_count == 0);

                Entry[] oldEntries = source._entries;
                // FIXME
                /*
                if (source._comparer == _comparer)
                {
                    // If comparers are the same, we can copy _entries without rehashing.
                    CopyEntries(oldEntries, source._count);
                    return;
                }
                */

                // Comparers differ need to rehash all the entries via Add
                int allocatedEntryCount = source._count;
                for (int i = 0; i < allocatedEntryCount; i++)
                {
                    // Only copy if an entry
                    if (oldEntries[i].next >= -1)
                    {
                        Add(oldEntries[i].key, oldEntries[i].value);
                    }
                }
                return;
            }

            // We similarly special-case KVP<>[] and List<KVP<>>, as they're commonly used to seed dictionaries, and
            // we want to avoid the enumerator costs (e.g. allocation) for them as well. Extract a span if possible.
            ReadOnlySpan<KeyValuePair<TKey, TValue>> span;
            if (enumerable is KeyValuePair<TKey, TValue>[] array)
            {
                span = array;
            }
            else if (enumerable.GetType() == typeof(List<KeyValuePair<TKey, TValue>>))
            {
                span = CollectionsMarshal.AsSpan((List<KeyValuePair<TKey, TValue>>)enumerable);
            }
            else
            {
                // Fallback path for all other enumerables
                foreach (KeyValuePair<TKey, TValue> pair in enumerable)
                {
                    Add(pair.Key, pair.Value);
                }
                return;
            }

            // We got a span. Add the elements to the dictionary.
            foreach (KeyValuePair<TKey, TValue> pair in span)
            {
                Add(pair.Key, pair.Value);
            }
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Dictionary(SerializationInfo info, StreamingContext context)
        {
            // We can't do anything with the keys and values until the entire graph has been deserialized
            // and we have a resonable estimate that GetHashCode is not going to fail.  For the time being,
            // we'll just cache this.  The graph is not valid until OnDeserialization has been called.
            HashHelpers.SerializationInfoTable.Add(this, info);
        }

        public IEqualityComparer<TKey> Comparer
        {
            get
            {
                if (typeof(TKey) == typeof(string))
                {
                    Debug.Assert(_comparer is not null, "The comparer should never be null for a reference type.");
                    return (IEqualityComparer<TKey>)IInternalStringEqualityComparer.GetUnderlyingEqualityComparer((IEqualityComparer<string?>)_comparer);
                }
                else
                {
                    return _comparer ?? EqualityComparer<TKey>.Default;
                }
            }
        }

        public int Count => _count - _freeCount;

        /// <summary>
        /// Gets the total numbers of elements the internal data structure can hold without resizing.
        /// </summary>
        public int Capacity => _entries?.Length ?? 0;

        public KeyCollection Keys => _keys ??= new KeyCollection(this);

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        public ValueCollection Values => _values ??= new ValueCollection(this);

        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public TValue this[TKey key]
        {
            get
            {
                ref TValue value = ref FindValue(key);
                if (!Unsafe.IsNullRef(ref value))
                {
                    return value;
                }

                ThrowHelper.ThrowKeyNotFoundException(key);
                return default;
            }
            set
            {
                ref Entry result = ref TryInsert(key, value, InsertionBehavior.OverwriteExisting, out _);
                Debug.Assert(!Unsafe.IsNullRef(ref result));
            }
        }

        public void Add(TKey key, TValue value)
        {
            ref Entry result = ref TryInsert(key, value, InsertionBehavior.ThrowOnExisting, out _);
            Debug.Assert(!Unsafe.IsNullRef(ref result));
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair) =>
            Add(keyValuePair.Key, keyValuePair.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            ref TValue value = ref FindValue(keyValuePair.Key);
            if (!Unsafe.IsNullRef(ref value) && EqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value))
            {
                return true;
            }

            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            ref TValue value = ref FindValue(keyValuePair.Key);
            if (!Unsafe.IsNullRef(ref value) && EqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value))
            {
                Remove(keyValuePair.Key);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            int count = _count;
            if (count > 0)
            {
                Debug.Assert(_buckets != null, "_buckets should be non-null");
                Debug.Assert(_entries != null, "_entries should be non-null");

                // TODO: Optimized clear that only touches buckets where count is nonzero
                Array.Clear(_buckets);

                _count = 0;
                _freeList = -1;
                _freeCount = 0;
                Array.Clear(_entries, 0, count);
            }
        }

        public bool ContainsKey(TKey key) =>
            !Unsafe.IsNullRef(ref FindValue(key));

        public bool ContainsValue(TValue value)
        {
            Entry[]? entries = _entries;
            if (value == null)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (entries![i].next >= -1 && entries[i].value == null)
                    {
                        return true;
                    }
                }
            }
            else if (typeof(TValue).IsValueType)
            {
                // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                for (int i = 0; i < _count; i++)
                {
                    if (entries![i].next >= -1 && EqualityComparer<TValue>.Default.Equals(entries[i].value, value))
                    {
                        return true;
                    }
                }
            }
            else
            {
                // Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize
                // https://github.com/dotnet/runtime/issues/10050
                // So cache in a local rather than get EqualityComparer per loop iteration
                EqualityComparer<TValue> defaultComparer = EqualityComparer<TValue>.Default;
                for (int i = 0; i < _count; i++)
                {
                    if (entries![i].next >= -1 && defaultComparer.Equals(entries[i].value, value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if ((uint)index > (uint)array.Length)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }

            if (array.Length - index < Count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            int count = _count;
            Entry[]? entries = _entries;
            for (int i = 0; i < count; i++)
            {
                if (entries![i].next >= -1)
                {
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
            Count == 0 ? GenericEmptyEnumerator<KeyValuePair<TKey, TValue>>.Instance :
            GetEnumerator();

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
            }

            info.AddValue(VersionName, _version);
            info.AddValue(ComparerName, Comparer, typeof(IEqualityComparer<TKey>));
            info.AddValue(HashSizeName, _entries == null ? 0 : _entries.Length); // This is the length of the bucket array

            if (_entries != null)
            {
                var array = new KeyValuePair<TKey, TValue>[Count];
                CopyTo(array, 0);
                info.AddValue(KeyValuePairsName, array, typeof(KeyValuePair<TKey, TValue>[]));
            }
        }

        private ref Entry FindEntry<TProtocol, TActualKey>(TProtocol protocol, TActualKey key)
            where TProtocol : struct, IComparisonProtocol<TActualKey>
            where TActualKey : allows ref struct
        {
            uint hashCode = (uint)protocol.GetHashCode(key);
            var suffix = GetHashSuffix(hashCode);
            var vectorized = Sse2.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported;
            Vector128<byte> searchVector = vectorized ? Vector128.Create(suffix) : default;
            ref var bucket = ref LoopingBucketEnumerator.New(_buckets, hashCode, _fastModMultiplier, out var enumerator);
            Span<Entry> entries = _entries!;
            // FIXME: Change to do { } while () by introducing EmptyBuckets optimization from SimdDictionary
            while (!Unsafe.IsNullRef(ref bucket))
            {
                // Pipelining
                int bucketCount = bucket.Count;
                // Determine start index for key search
                int startIndex = vectorized
                    ? bucket.FindSuffix(bucketCount, suffix, searchVector)
                    : bucket.FindSuffixScalar(bucketCount, suffix);
                ref var entry = ref FindEntryInBucket(protocol, ref bucket, entries, startIndex, bucketCount, key, out _, out _);
                if (Unsafe.IsNullRef(ref entry))
                {
                    if (bucket.CascadeCount == 0)
                        return ref Unsafe.NullRef<Entry>();
                }
                else
                    return ref entry;
                bucket = ref enumerator.Advance();
            }

            return ref Unsafe.NullRef<Entry>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Entry FindEntryInBucket<TProtocol, TActualKey>(
            TProtocol protocol, ref Bucket bucket, Span<Entry> entries,
            int startIndex, int bucketCount, TActualKey key,
            // These out-params are annoying but inlining seems to optimize them away
            out int entryIndex, out int matchIndexInBucket
        )
            where TProtocol : struct, IComparisonProtocol<TActualKey>
            where TActualKey : allows ref struct
        {
            Unsafe.SkipInit(out matchIndexInBucket);
            Unsafe.SkipInit(out entryIndex);
            Debug.Assert(startIndex >= 0);

            int count = bucketCount - startIndex;
            if (count <= 0)
                return ref Unsafe.NullRef<Entry>();

            ref int indexSlot = ref bucket.Indices[startIndex];
            while (true)
            {
                ref var entry = ref entries[indexSlot];
                if (protocol.Equals(key, entry.key))
                {
                    // We could optimize out the bucketCount local to prevent a stack spill in some cases by doing
                    //  Unsafe.ByteOffset(...) / sizeof(Pair), but the potential idiv is extremely painful
                    entryIndex = indexSlot;
                    matchIndexInBucket = bucketCount - count;
                    return ref entry;
                }

                // NOTE: --count <= 0 produces an extra 'test' opcode
                if (--count == 0)
                    return ref Unsafe.NullRef<Entry>();
                else
                    indexSlot = ref Unsafe.Add(ref indexSlot, 1);
            }
        }

        internal ref TValue FindValue(TKey key)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            var comparer = _comparer;
            ref Entry entry = ref (typeof(TKey).IsValueType && (comparer == null))
                ? ref FindEntry(default(DefaultValueTypeComparerComparisonProtocol), key)
                : ref FindEntry(new ComparerComparisonProtocol(comparer!), key);

            if (Unsafe.IsNullRef(ref entry))
                return ref Unsafe.NullRef<TValue>();
            else
                return ref entry.value;
        }

        private static void FillNewBucketsForResizeOrRehash(
            Span<Bucket> newBuckets, ulong fastModMultiplier,
            Span<Entry> entries, int allocatedEntryCount, IEqualityComparer<TKey> comparer
        )
        {
            for (int index = 0; index < allocatedEntryCount; index++)
            {
                // FIXME: Use Unsafe.Add to optimize out the imul per element
                ref var entry = ref entries[index];
                if (entry.next >= -1)
                    InsertExistingEntryIntoNewBucket(newBuckets, fastModMultiplier, comparer, ref entry, index);
            }

            static void InsertExistingEntryIntoNewBucket(
                Span<Bucket> newBuckets, ulong fastModMultiplier,
                IEqualityComparer<TKey> comparer, ref Entry entry, int entryIndex
            )
            {
                Debug.Assert(comparer is not null || typeof(TKey).IsValueType);
                uint hashCode = (uint)((typeof(TKey).IsValueType && comparer == null) ? entry.key.GetHashCode() : comparer!.GetHashCode(entry.key));
                var suffix = GetHashSuffix(hashCode);
                // FIXME: Skip this on non-vectorized targets
                var searchVector = Vector128.Create(suffix);

                ref var bucket = ref LoopingBucketEnumerator.New(newBuckets, hashCode, fastModMultiplier, out var enumerator);
                // FIXME: Change to do { } while () by introducing EmptyBuckets optimization from SimdDictionary
                while (!Unsafe.IsNullRef(ref bucket))
                {
                    // Pipelining
                    int bucketCount = bucket.Count;
                    if (bucketCount < Bucket.Capacity)
                    {
                        InsertIntoBucket(ref bucket, suffix, bucketCount, entryIndex);
                        // We can ignore the return value of this, we're in the middle of rehashing/resizing so we wouldn't ever
                        //  do a comparer swap in this scenario
                        AdjustCascadeCounts(enumerator, true);
                        return;
                    }

                    bucket = ref enumerator.Advance();
                }

                ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
            }
        }

        private int Initialize(int capacity)
        {
            int size = HashHelpers.GetPrime(capacity);
            int bucketCount = GetBucketCountForEntryCount(size);
            Bucket[] buckets = new Bucket[bucketCount];
            Entry[] entries = new Entry[size];

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _freeList = -1;
            _freeCount = 0;
#if TARGET_64BIT
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)bucketCount);
#endif
            _buckets = buckets;
            _entries = entries;
            _count = 0;

            return size;
        }

        // TODO: Figure out if we can outline this (reduces code size) without regressing performance for all inserts/removes
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AdjustCascadeCounts(LoopingBucketEnumerator enumerator, bool increase)
        {
            bool needRehash = false;
            // We may have cascaded out of a previous bucket; if so, scan backwards and update
            //  the cascade count for every bucket we previously scanned.
            ref Bucket bucket = ref enumerator.Retreat();
            while (!Unsafe.IsNullRef(ref bucket))
            {
                // FIXME: Track number of times we cascade out of a bucket for string rehashing anti-DoS mitigation!
                var cascadeCount = bucket.CascadeCount;
                if (increase)
                {
                    // Never overflow (wrap around) the counter
                    if (cascadeCount < Bucket.DegradedCascadeCount)
                    {
                        int newCascadeCount = bucket.CascadeCount = (ushort)(cascadeCount + 1);
                        if (!typeof(TKey).IsValueType && (newCascadeCount >= HashHelpers.HashCollisionThreshold))
                            needRehash = true;
                    }
                }
                else
                {
                    if (cascadeCount == 0)
                        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();

                    // If the cascade counter hit the maximum, it's possible the actual cascade count through here is higher,
                    //  so it's no longer safe to decrement. This is a very rare scenario, but it permanently degrades the table.
                    // TODO: Track this and trigger a rehash once too many buckets are in this state + dict is mostly empty.
                    else if (cascadeCount < Bucket.DegradedCascadeCount)
                        bucket.CascadeCount = (ushort)(cascadeCount - 1);
                }

                bucket = ref enumerator.Retreat();
            }

            return needRehash;
        }

        private ref Entry TryInsert(TKey key, TValue value, InsertionBehavior behavior, out bool exists)
        {
            var comparer = _comparer;
            return ref (typeof(TKey).IsValueType && (comparer == null))
                ? ref TryInsert(default(DefaultValueTypeComparerComparisonProtocol), key, value, behavior, out exists)
                : ref TryInsert(new ComparerComparisonProtocol(comparer!), key, value, behavior, out exists);
        }

        private ref Entry TryInsert<TProtocol, TActualKey>(TProtocol protocol, TActualKey key, TValue value, InsertionBehavior behavior, out bool exists)
            where TProtocol : struct, IComparisonProtocol<TActualKey>
            where TActualKey : allows ref struct
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            uint hashCode = (uint)protocol.GetHashCode(key);
            var suffix = GetHashSuffix(hashCode);
            var vectorized = Sse2.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported;
            Vector128<byte> searchVector = vectorized ? Vector128.Create(suffix) : default;

        // We need to retry when we grow the buckets array since the correct destination bucket will have changed and might not
        //  be the same as the destination bucket before resizing (it probably isn't, in fact)
        retry:
            Span<Bucket> buckets = _buckets;
            if (buckets.IsEmpty)
            {
                Initialize(0);
                buckets = _buckets;
            }
            Debug.Assert(!buckets.IsEmpty);
            Span<Entry> entries = _entries!;
            Debug.Assert(!entries.IsEmpty, "expected entries to be non-null");

            ref var bucket = ref LoopingBucketEnumerator.New(buckets, hashCode, _fastModMultiplier, out var enumerator);
            // FIXME: Change to do { } while () by introducing EmptyBuckets optimization from SimdDictionary
            while (!Unsafe.IsNullRef(ref bucket))
            {
                // Pipelining
                int bucketCount = bucket.Count;
                // Determine start index for key search
                int startIndex = vectorized
                    ? bucket.FindSuffix(bucketCount, suffix, searchVector)
                    : bucket.FindSuffixScalar(bucketCount, suffix);
                ref var entry = ref FindEntryInBucket(protocol, ref bucket, entries, startIndex, bucketCount, key, out _, out _);
                if (!Unsafe.IsNullRef(ref entry))
                {
                    exists = true;
                    switch (behavior)
                    {
                        case InsertionBehavior.InsertNewOnly:
                            return ref entry;
                        case InsertionBehavior.OverwriteExisting:
                            entry.value = value;
                            return ref entry;
                        case InsertionBehavior.ThrowOnExisting:
                            ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(protocol.GetKey(key));
                            return ref entry;
                        default:
                            ThrowHelper.ThrowArgumentOutOfRangeException();
                            return ref entry;
                    }
                }
                else if (startIndex < Bucket.Capacity)
                {
                    // FIXME: Suffix collision. Track these for rehashing anti-DoS mitigation.
                }

                if (bucketCount < Bucket.Capacity)
                {
                    // NOTE: Compute this before creating the entry, otherwise a comparer that throws could corrupt us.
                    var actualKey = protocol.GetKey(key);

                    int newEntryIndex = TryCreateNewEntry(entries);
                    if (newEntryIndex < 0)
                    {
                        // We can't reuse the existing target bucket once we resized, so start over. This is very rare.
                        Resize();
                        goto retry;
                    }

                    ref var newEntry = ref entries[newEntryIndex];
                    PopulateEntry(ref newEntry, actualKey, value);
                    InsertIntoBucket(ref bucket, suffix, bucketCount, newEntryIndex);
                    _version++;
                    exists = false;
                    if (AdjustCascadeCounts(enumerator, true) && (_comparer is NonRandomizedStringEqualityComparer))
                    {
                        // if AdjustCascadeCounts returned true, we need to change comparers (if possible) to one with better collision
                        //  resistance.
                        ChangeToRandomizedStringEqualityComparer();
                        // This will have invalidated our buckets but not our entries, so it's safe to return newEntry.
                    }
                    return ref newEntry;
                }

                bucket = ref enumerator.Advance();
            }

            // We failed to find any bucket with room and hit the end of the loop, so we should be full. This is very rare.
            if (_count >= entries.Length)
            {
                Resize();
                goto retry;
            }

            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
            exists = false;
            return ref Unsafe.NullRef<Entry>();
        }

        private int TryCreateNewEntry(Span<Entry> entries)
        {
            int index;
            if (_freeCount > 0)
            {
                index = _freeList;
                Debug.Assert((StartOfFreeList - entries[_freeList].next) >= -1, "shouldn't overflow because `next` cannot underflow");
                _freeList = StartOfFreeList - entries[_freeList].next;
                _freeCount--;
            }
            else
            {
                index = _count;
                // Resize needed
                if (_count >= entries.Length)
                    return -1;
                _count = index + 1;
            }
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PopulateEntry(ref Entry entry, TKey key, TValue value)
        {
            entry.key = key;
            entry.value = value;
            entry.next = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool InsertIntoBucket(ref Bucket bucket, byte suffix, int bucketCount, int entryIndex)
        {
            Debug.Assert(bucketCount < Bucket.Capacity);

            unchecked
            {
                ref var destination = ref bucket.Indices[bucketCount];
                bucket.Count = (byte)(bucketCount + 1);
                bucket.SetSlot((nuint)bucketCount, suffix);
                destination = entryIndex;
                return true;
            }
        }

        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on the current <see cref="Dictionary{TKey, TValue}"/>
        /// using a <typeparamref name="TAlternateKey"/> as a key instead of a <typeparamref name="TKey"/>.
        /// </summary>
        /// <typeparam name="TAlternateKey">The alternate type of a key for performing lookups.</typeparam>
        /// <returns>The created lookup instance.</returns>
        /// <exception cref="InvalidOperationException">The dictionary's comparer is not compatible with <typeparamref name="TAlternateKey"/>.</exception>
        /// <remarks>
        /// The dictionary must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternateKey, TKey}"/> with
        /// <typeparamref name="TAlternateKey"/> and <typeparamref name="TKey"/>. If it doesn't, an exception will be thrown.
        /// </remarks>
        public AlternateLookup<TAlternateKey> GetAlternateLookup<TAlternateKey>()
            where TAlternateKey : notnull, allows ref struct
        {
            if (!AlternateLookup<TAlternateKey>.IsCompatibleKey(this))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IncompatibleComparer);
            }

            return new AlternateLookup<TAlternateKey>(this);
        }

        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on the current <see cref="Dictionary{TKey, TValue}"/>
        /// using a <typeparamref name="TAlternateKey"/> as a key instead of a <typeparamref name="TKey"/>.
        /// </summary>
        /// <typeparam name="TAlternateKey">The alternate type of a key for performing lookups.</typeparam>
        /// <param name="lookup">The created lookup instance when the method returns true, or a default instance that should not be used if the method returns false.</param>
        /// <returns>true if a lookup could be created; otherwise, false.</returns>
        /// <remarks>
        /// The dictionary must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternateKey, TKey}"/> with
        /// <typeparamref name="TAlternateKey"/> and <typeparamref name="TKey"/>. If it doesn't, the method will return false.
        /// </remarks>
        public bool TryGetAlternateLookup<TAlternateKey>(
            out AlternateLookup<TAlternateKey> lookup)
            where TAlternateKey : notnull, allows ref struct
        {
            if (AlternateLookup<TAlternateKey>.IsCompatibleKey(this))
            {
                lookup = new AlternateLookup<TAlternateKey>(this);
                return true;
            }

            lookup = default;
            return false;
        }

        /// <summary>
        /// Provides a type that may be used to perform operations on a <see cref="Dictionary{TKey, TValue}"/>
        /// using a <typeparamref name="TAlternateKey"/> as a key instead of a <typeparamref name="TKey"/>.
        /// </summary>
        /// <typeparam name="TAlternateKey">The alternate type of a key for performing lookups.</typeparam>
        public readonly struct AlternateLookup<TAlternateKey> where TAlternateKey : notnull, allows ref struct
        {
            /// <summary>Initialize the instance. The dictionary must have already been verified to have a compatible comparer.</summary>
            internal AlternateLookup(Dictionary<TKey, TValue> dictionary)
            {
                Debug.Assert(dictionary is not null);
                Debug.Assert(IsCompatibleKey(dictionary));
                Dictionary = dictionary;
            }

            /// <summary>Gets the <see cref="Dictionary{TKey, TValue}"/> against which this instance performs operations.</summary>
            public Dictionary<TKey, TValue> Dictionary { get; }

            /// <summary>Gets or sets the value associated with the specified alternate key.</summary>
            /// <param name="key">The alternate key of the value to get or set.</param>
            /// <value>
            /// The value associated with the specified alternate key. If the specified alternate key is not found, a get operation throws
            /// a <see cref="KeyNotFoundException"/>, and a set operation creates a new element with the specified key.
            /// </value>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
            /// <exception cref="KeyNotFoundException">The property is retrieved and alternate key does not exist in the collection.</exception>
            public TValue this[TAlternateKey key]
            {
                get
                {
                    ref TValue value = ref FindValue(key, out _);
                    if (Unsafe.IsNullRef(ref value))
                    {
                        ThrowHelper.ThrowKeyNotFoundException(GetAlternateComparer(Dictionary).Create(key));
                    }

                    return value;
                }
                set => GetValueRefOrAddDefault(key, out _) = value;
            }

            /// <summary>Checks whether the dictionary has a comparer compatible with <typeparamref name="TAlternateKey"/>.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool IsCompatibleKey(Dictionary<TKey, TValue> dictionary)
            {
                Debug.Assert(dictionary is not null);
                return dictionary._comparer is IAlternateEqualityComparer<TAlternateKey, TKey>;
            }

            /// <summary>Gets the dictionary's alternate comparer. The dictionary must have already been verified as compatible.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static IAlternateEqualityComparer<TAlternateKey, TKey> GetAlternateComparer(Dictionary<TKey, TValue> dictionary)
            {
                Debug.Assert(IsCompatibleKey(dictionary));
                return Unsafe.As<IAlternateEqualityComparer<TAlternateKey, TKey>>(dictionary._comparer);
            }

            /// <summary>Gets the value associated with the specified alternate key.</summary>
            /// <param name="key">The alternate key of the value to get.</param>
            /// <param name="value">
            /// When this method returns, contains the value associated with the specified key, if the key is found;
            /// otherwise, the default value for the type of the value parameter.
            /// </param>
            /// <returns><see langword="true"/> if an entry was found; otherwise, <see langword="false"/>.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
            public bool TryGetValue(TAlternateKey key, [MaybeNullWhen(false)] out TValue value)
            {
                ref TValue valueRef = ref FindValue(key, out _);
                if (!Unsafe.IsNullRef(ref valueRef))
                {
                    value = valueRef;
                    return true;
                }

                value = default;
                return false;
            }

            /// <summary>Gets the value associated with the specified alternate key.</summary>
            /// <param name="key">The alternate key of the value to get.</param>
            /// <param name="actualKey">
            /// When this method returns, contains the actual key associated with the alternate key, if the key is found;
            /// otherwise, the default value for the type of the key parameter.
            /// </param>
            /// <param name="value">
            /// When this method returns, contains the value associated with the specified key, if the key is found;
            /// otherwise, the default value for the type of the value parameter.
            /// </param>
            /// <returns><see langword="true"/> if an entry was found; otherwise, <see langword="false"/>.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
            public bool TryGetValue(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey, [MaybeNullWhen(false)] out TValue value)
            {
                ref TValue valueRef = ref FindValue(key, out actualKey);
                if (!Unsafe.IsNullRef(ref valueRef))
                {
                    value = valueRef;
                    Debug.Assert(actualKey is not null);
                    return true;
                }

                value = default;
                return false;
            }

            /// <summary>Determines whether the <see cref="Dictionary{TKey, TValue}"/> contains the specified alternate key.</summary>
            /// <param name="key">The alternate key to check.</param>
            /// <returns><see langword="true"/> if the key is in the dictionary; otherwise, <see langword="false"/>.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
            public bool ContainsKey(TAlternateKey key) =>
                !Unsafe.IsNullRef(ref FindValue(key, out _));

            /// <summary>Finds the entry associated with the specified alternate key.</summary>
            /// <param name="key">The alternate key.</param>
            /// <param name="actualKey">The actual key, if found.</param>
            /// <returns>A reference to the value associated with the key, if found; otherwise, a null reference.</returns>
            internal ref TValue FindValue(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey)
            {
                Dictionary<TKey, TValue> dictionary = Dictionary;
                AlternateComparerComparisonProtocol<TAlternateKey> protocol = new(GetAlternateComparer(dictionary));

                ref var entry = ref dictionary.FindEntry(protocol, key);
                if (Unsafe.IsNullRef(ref entry))
                {
                    actualKey = default!;
                    return ref Unsafe.NullRef<TValue>();
                }
                else
                {
                    actualKey = entry.key;
                    return ref entry.value;
                }
            }

            /// <summary>Removes the value with the specified alternate key from the <see cref="Dictionary{TKey, TValue}"/>.</summary>
            /// <param name="key">The alternate key of the element to remove.</param>
            /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
            public bool Remove(TAlternateKey key)
            {
                Dictionary<TKey, TValue> dictionary = Dictionary;
                AlternateComparerComparisonProtocol<TAlternateKey> protocol = new(GetAlternateComparer(dictionary));
                return dictionary.Remove(protocol, key, out Unsafe.NullRef<TKey>()!, out Unsafe.NullRef<TValue>()!);
            }

            /// <summary>
            /// Removes the value with the specified alternate key from the <see cref="Dictionary{TKey, TValue}"/>,
            /// and copies the element to the value parameter.
            /// </summary>
            /// <param name="key">The alternate key of the element to remove.</param>
            /// <param name="actualKey">The removed key.</param>
            /// <param name="value">The removed element.</param>
            /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
            public bool Remove(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey, [MaybeNullWhen(false)] out TValue value)
            {
                Dictionary<TKey, TValue> dictionary = Dictionary;
                AlternateComparerComparisonProtocol<TAlternateKey> protocol = new(GetAlternateComparer(dictionary));
                return dictionary.Remove(protocol, key, out actualKey, out value);
            }

            /// <summary>Attempts to add the specified key and value to the dictionary.</summary>
            /// <param name="key">The alternate key of the element to add.</param>
            /// <param name="value">The value of the element to add.</param>
            /// <returns>true if the key/value pair was added to the dictionary successfully; otherwise, false.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
            public bool TryAdd(TAlternateKey key, TValue value)
            {
                ref TValue? slot = ref GetValueRefOrAddDefault(key, out bool exists);
                if (!exists)
                {
                    slot = value;
                    return true;
                }

                return false;
            }

            /// <inheritdoc cref="CollectionsMarshal.GetValueRefOrAddDefault{TKey, TValue}(Dictionary{TKey, TValue}, TKey, out bool)"/>
#pragma warning disable IDE0060
            internal ref TValue? GetValueRefOrAddDefault(TAlternateKey key, out bool exists)
#pragma warning restore IDE0060
            {
                Dictionary<TKey, TValue> dictionary = Dictionary;
                AlternateComparerComparisonProtocol<TAlternateKey> protocol = new(GetAlternateComparer(dictionary));
                return ref dictionary.TryInsert(protocol, key, default!, InsertionBehavior.InsertNewOnly, out exists).value!;
            }
        }

        /// <summary>
        /// A helper class containing APIs exposed through <see cref="CollectionsMarshal"/> or <see cref="CollectionExtensions"/>.
        /// These methods are relatively niche and only used in specific scenarios, so adding them in a separate type avoids
        /// the additional overhead on each <see cref="Dictionary{TKey, TValue}"/> instantiation, especially in AOT scenarios.
        /// </summary>
        internal static class CollectionsMarshalHelper
        {
            /// <inheritdoc cref="CollectionsMarshal.GetValueRefOrAddDefault{TKey, TValue}(Dictionary{TKey, TValue}, TKey, out bool)"/>
            public static ref TValue? GetValueRefOrAddDefault(Dictionary<TKey, TValue> dictionary, TKey key, out bool exists)
            {
                // NOTE: this method is mirrored by Dictionary<TKey, TValue>.TryInsert above.
                // If you make any changes here, make sure to keep that version in sync as well.

                if (key == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
                }

                ref var entry = ref dictionary.TryInsert(key, default!, InsertionBehavior.InsertNewOnly, out exists);
                if (!Unsafe.IsNullRef(ref entry))
                    return ref entry.value!;
                else
                    return ref Unsafe.NullRef<TValue?>();
            }
        }

        public virtual void OnDeserialization(object? sender)
        {
            HashHelpers.SerializationInfoTable.TryGetValue(this, out SerializationInfo? siInfo);

            if (siInfo == null)
            {
                // We can return immediately if this function is called twice.
                // Note we remove the serialization info from the table at the end of this method.
                return;
            }

            int realVersion = siInfo.GetInt32(VersionName);
            int hashsize = siInfo.GetInt32(HashSizeName);
            _comparer = (IEqualityComparer<TKey>)siInfo.GetValue(ComparerName, typeof(IEqualityComparer<TKey>))!; // When serialized if comparer is null, we use the default.

            if (hashsize != 0)
            {
                Initialize(hashsize);

                KeyValuePair<TKey, TValue>[]? array = (KeyValuePair<TKey, TValue>[]?)
                    siInfo.GetValue(KeyValuePairsName, typeof(KeyValuePair<TKey, TValue>[]));

                if (array == null)
                {
                    ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_MissingKeys);
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Key == null)
                    {
                        ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_NullKey);
                    }

                    Add(array[i].Key, array[i].Value);
                }
            }
            else
            {
                _buckets = null;
            }

            _version = realVersion;
            HashHelpers.SerializationInfoTable.Remove(this);
        }

        private void Resize() => Resize(HashHelpers.ExpandPrime(_count));

        private void Resize(int newSize)
        {
            Debug.Assert(_entries != null, "_entries should be non-null");
            Debug.Assert(newSize >= _entries.Length);

            Entry[] entries = new Entry[newSize];

            int count = _count;
            Array.Copy(_entries, entries, count);

            int newBucketCount = GetBucketCountForEntryCount(newSize);
            ulong fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newBucketCount);

            if (newBucketCount != _buckets?.Length)
            {
                Bucket[] newBuckets = new Bucket[newBucketCount];
                FillNewBucketsForResizeOrRehash(
                    newBuckets, fastModMultiplier, entries, _count,
                    // FIXME
                    _comparer ?? EqualityComparer<TKey>.Default
                );
                _buckets = newBuckets;
                _fastModMultiplier = fastModMultiplier;
            }

            _entries = entries;
            _version++;
        }

        private void ChangeToRandomizedStringEqualityComparer()
        {
            Debug.Assert(_comparer is NonRandomizedStringEqualityComparer);
            _comparer = (IEqualityComparer<TKey>)((NonRandomizedStringEqualityComparer)_comparer).GetRandomizedEqualityComparer();
            Debug.Assert(_buckets != null);
            Array.Clear(_buckets!);
            FillNewBucketsForResizeOrRehash(_buckets, _fastModMultiplier, _entries, _count, _comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RemoveIndexFromBucket(ref Bucket bucket, int indexInBucket, int bucketCount)
        {
            Debug.Assert(bucketCount > 0);
            unchecked
            {
                int replacementIndexInBucket = bucketCount - 1;
                bucket.Count = (byte)replacementIndexInBucket;
                ref var toRemove = ref bucket.Indices[indexInBucket];
                ref var replacement = ref bucket.Indices[replacementIndexInBucket];
                // This rotate-back algorithm makes removes more expensive than if we were to just always zero the slot.
                // But then other algorithms like insertion get more expensive, since we have to search for a zero to replace...
                if (!Unsafe.AreSame(ref toRemove, ref replacement))
                {
                    // TODO: This is the only place in the find/insert/remove algorithms that actually needs indexInBucket.
                    // Can we refactor it away? The good news is RyuJIT optimizes it out entirely in find/insert.
                    bucket.SetSlot((uint)indexInBucket, bucket.GetSlot(replacementIndexInBucket));
                    bucket.SetSlot((uint)replacementIndexInBucket, 0);
                    toRemove = replacement;
                }
                else
                {
                    bucket.SetSlot((uint)indexInBucket, 0);
                    toRemove = default!;
                }
            }
        }

        private void RemoveEntry(ref Entry entry, int entryIndex)
        {
            Debug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
            entry.next = StartOfFreeList - _freeList;

            if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
            {
                entry.key = default!;
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                entry.value = default!;
            }

            _freeList = entryIndex;
            _freeCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            return Remove(key, out Unsafe.NullRef<TValue>()!);
        }

        public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            var comparer = _comparer;
            return (typeof(TKey).IsValueType && (comparer == null))
                ? Remove(default(DefaultValueTypeComparerComparisonProtocol), key, out Unsafe.NullRef<TKey>()!, out value)
                : Remove(new ComparerComparisonProtocol(comparer!), key, out Unsafe.NullRef<TKey>()!, out value);
        }

        private bool Remove<TProtocol, TActualKey>(
            TProtocol protocol, TActualKey key,
            [MaybeNullWhen(false)] out TKey actualKey,
            [MaybeNullWhen(false)] out TValue value
        )
            where TProtocol : struct, IComparisonProtocol<TActualKey>
            where TActualKey : allows ref struct
        {
            // This allows using Remove(key, out value) to implement Remove(key) efficiently,
            //  as long as we check whether value is a null reference before writing to it.
            Unsafe.SkipInit(out actualKey);
            Unsafe.SkipInit(out value);

            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            if (_buckets == null)
            {
                if (!Unsafe.IsNullRef(ref actualKey))
                    actualKey = default!;
                if (!Unsafe.IsNullRef(ref value))
                    value = default!;
                return false;
            }

            uint hashCode = (uint)protocol.GetHashCode(key);
            var suffix = GetHashSuffix(hashCode);
            var vectorized = Sse2.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported;
            Vector128<byte> searchVector = vectorized ? Vector128.Create(suffix) : default;
            Span<Entry> entries = _entries!;
            Debug.Assert(!entries.IsEmpty, "expected entries to be non-null");

            ref var bucket = ref LoopingBucketEnumerator.New(_buckets, hashCode, _fastModMultiplier, out var enumerator);

            // FIXME: Change to do { } while () by introducing EmptyBuckets optimization from SimdDictionary
            while (!Unsafe.IsNullRef(ref bucket))
            {
                // Pipelining
                int bucketCount = bucket.Count;
                // Determine start index for key search
                int startIndex = vectorized
                    ? bucket.FindSuffix(bucketCount, suffix, searchVector)
                    : bucket.FindSuffixScalar(bucketCount, suffix);
                ref var entry = ref FindEntryInBucket(
                    protocol, ref bucket, entries, startIndex, bucketCount, key,
                    out int entryIndex, out int indexInBucket
                );

                if (!Unsafe.IsNullRef(ref entry))
                {
                    if (!Unsafe.IsNullRef(ref actualKey))
                        actualKey = entry.key;
                    if (!Unsafe.IsNullRef(ref value))
                        value = entry.value;
                    // NOTE: We don't increment version because it's documented that removal during enumeration works.
                    RemoveEntry(ref entry, entryIndex);
                    RemoveIndexFromBucket(ref bucket, indexInBucket, bucketCount);
                    AdjustCascadeCounts(enumerator, false);
                    return true;
                }

                if (bucket.CascadeCount == 0)
                {
                    if (!Unsafe.IsNullRef(ref actualKey))
                        actualKey = default!;
                    if (!Unsafe.IsNullRef(ref value))
                        value = default!;
                    return false;
                }

                bucket = ref enumerator.Advance();
            }

            if (!Unsafe.IsNullRef(ref actualKey))
                actualKey = default!;
            if (!Unsafe.IsNullRef(ref value))
                value = default!;
            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
            return false;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            ref TValue valRef = ref FindValue(key);
            if (!Unsafe.IsNullRef(ref valRef))
            {
                value = valRef;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            TryInsert(key, value, InsertionBehavior.InsertNewOnly, out bool exists);
            return !exists;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index) =>
            CopyTo(array, index);

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (array.Rank != 1)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            }

            if (array.GetLowerBound(0) != 0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
            }

            if ((uint)index > (uint)array.Length)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }

            if (array.Length - index < Count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            if (array is KeyValuePair<TKey, TValue>[] pairs)
            {
                CopyTo(pairs, index);
            }
            else if (array is DictionaryEntry[] dictEntryArray)
            {
                Entry[]? entries = _entries;
                for (int i = 0; i < _count; i++)
                {
                    if (entries![i].next >= -1)
                    {
                        dictEntryArray[index++] = new DictionaryEntry(entries[i].key, entries[i].value);
                    }
                }
            }
            else
            {
                object[]? objects = array as object[];
                if (objects == null)
                {
                    ThrowHelper.ThrowArgumentException_Argument_IncompatibleArrayType();
                }

                try
                {
                    int count = _count;
                    Entry[]? entries = _entries;
                    for (int i = 0; i < count; i++)
                    {
                        if (entries![i].next >= -1)
                        {
                            objects[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                        }
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    ThrowHelper.ThrowArgumentException_Argument_IncompatibleArrayType();
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();

        /// <summary>
        /// Ensures that the dictionary can hold up to 'capacity' entries without any further expansion of its backing storage
        /// </summary>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            int currentCapacity = _entries == null ? 0 : _entries.Length;
            if (currentCapacity >= capacity)
            {
                return currentCapacity;
            }

            _version++;

            if (_buckets == null)
            {
                return Initialize(capacity);
            }

            int newSize = HashHelpers.GetPrime(capacity);
            Resize(newSize);
            return newSize;
        }

        /// <summary>
        /// Sets the capacity of this dictionary to what it would be if it had been originally initialized with all its entries
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize the memory overhead
        /// once it is known that no new elements will be added.
        ///
        /// To allocate minimum size storage array, execute the following statements:
        ///
        /// dictionary.Clear();
        /// dictionary.TrimExcess();
        /// </remarks>
        public void TrimExcess() => TrimExcess(Count);

        /// <summary>
        /// Sets the capacity of this dictionary to hold up 'capacity' entries without any further expansion of its backing storage
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize the memory overhead
        /// once it is known that no new elements will be added.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Passed capacity is lower than entries count.</exception>
        public void TrimExcess(int capacity)
        {
            int allocatedEntryCount = _count;
            if (capacity < Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            int newSize = HashHelpers.GetPrime(capacity);
            Span<Entry> oldEntries = _entries;
            int currentCapacity = oldEntries.IsEmpty ? 0 : oldEntries.Length;
            if (newSize >= currentCapacity)
            {
                return;
            }

            _version++;
            Initialize(newSize);

            // FIXME: Write a dedicated special-case implementation of this loop maybe?
            // Not sure how much faster it could actually be.
            if (!oldEntries.IsEmpty)
            {
                for (int i = 0; i < allocatedEntryCount; i++)
                {
                    ref var entry = ref oldEntries[i];
                    // Initialize zeroed our count and created new bucket/entry arrays so we can use the regular insert operation
                    //  to repopulate our new backing stores
                    if (entry.next >= -1)
                        TryInsert(entry.key, entry.value, InsertionBehavior.ThrowOnExisting, out _);
                }
            }
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => false;

        ICollection IDictionary.Keys => Keys;

        ICollection IDictionary.Values => Values;

        object? IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                {
                    ref TValue value = ref FindValue((TKey)key);
                    if (!Unsafe.IsNullRef(ref value))
                    {
                        return value;
                    }
                }

                return null;
            }
            set
            {
                if (key == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
                }
                ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);

                try
                {
                    TKey tempKey = (TKey)key;
                    try
                    {
                        this[tempKey] = (TValue)value!;
                    }
                    catch (InvalidCastException)
                    {
                        ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));
                    }
                }
                catch (InvalidCastException)
                {
                    ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
                }
            }
        }

        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }
            return key is TKey;
        }

        void IDictionary.Add(object key, object? value)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);

            try
            {
                TKey tempKey = (TKey)key;

                try
                {
                    Add(tempKey, (TValue)value!);
                }
                catch (InvalidCastException)
                {
                    ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));
                }
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
            }
        }

        bool IDictionary.Contains(object key)
        {
            if (IsCompatibleKey(key))
            {
                return ContainsKey((TKey)key);
            }

            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, Enumerator.DictEntry);

        void IDictionary.Remove(object key)
        {
            if (IsCompatibleKey(key))
            {
                Remove((TKey)key);
            }
        }

        // The hash suffix is selected from 8 bits of the hash, and then modified to ensure
        //  it is never zero (because a zero suffix indicates an empty slot.)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetHashSuffix(uint hashCode)
        {
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
        private static int GetBucketCountForEntryCount(int count)
        {
            int result = checked((count + Bucket.Capacity - 1) / Bucket.Capacity);
            return (result > 1)
                ? HashHelpers.GetPrime(result)
                : result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBucketIndexForHashCode(Span<Bucket> buckets, uint hashCode, ulong fastModMultiplier)
        {
            unchecked
            {
#if TARGET_64BIT
                return (int)HashHelpers.FastMod(hashCode, (uint)buckets.Length, fastModMultiplier);
#else
                return (int)(hashCode % buckets.Length);
#endif
            }
        }

        private struct Entry
        {
            /// <summary>
            /// encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
            /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
            /// </summary>
            public int next;
            public TKey key;     // Key of entry
            public TValue value; // Value of entry
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly Dictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _index;
            private KeyValuePair<TKey, TValue> _current;
            private readonly int _getEnumeratorRetType;  // What should Enumerator.Current return?

            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            internal Enumerator(Dictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = 0;
                _getEnumeratorRetType = getEnumeratorRetType;
                _current = default;
            }

            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
                while ((uint)_index < (uint)_dictionary._count)
                {
                    ref Entry entry = ref _dictionary._entries![_index++];

                    if (entry.next >= -1)
                    {
                        _current = new KeyValuePair<TKey, TValue>(entry.key, entry.value);
                        return true;
                    }
                }

                _index = _dictionary._count + 1;
                _current = default;
                return false;
            }

            public KeyValuePair<TKey, TValue> Current => _current;

            public void Dispose() { }

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }

                    if (_getEnumeratorRetType == DictEntry)
                    {
                        return new DictionaryEntry(_current.Key, _current.Value);
                    }

                    return new KeyValuePair<TKey, TValue>(_current.Key, _current.Value);
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _dictionary._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                _index = 0;
                _current = default;
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }

                    return new DictionaryEntry(_current.Key, _current.Value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }

                    return _current.Key;
                }
            }

            object? IDictionaryEnumerator.Value
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }

                    return _current.Value;
                }
            }
        }

        [DebuggerTypeProxy(typeof(DictionaryKeyCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            private readonly Dictionary<TKey, TValue> _dictionary;

            public KeyCollection(Dictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
                }

                _dictionary = dictionary;
            }

            public Enumerator GetEnumerator() => new Enumerator(_dictionary);

            public void CopyTo(TKey[] array, int index)
            {
                if (array == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (index < 0 || index > array.Length)
                {
                    ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
                }

                if (array.Length - index < _dictionary.Count)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                }

                int count = _dictionary._count;
                Entry[]? entries = _dictionary._entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries![i].next >= -1) array[index++] = entries[i].key;
                }
            }

            public int Count => _dictionary.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            void ICollection<TKey>.Add(TKey item) =>
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);

            void ICollection<TKey>.Clear() =>
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);

            public bool Contains(TKey item) =>
                _dictionary.ContainsKey(item);

            bool ICollection<TKey>.Remove(TKey item)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
                return false;
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() =>
                Count == 0 ? SZGenericArrayEnumerator<TKey>.Empty :
                GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TKey>)this).GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (array.Rank != 1)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
                }

                if (array.GetLowerBound(0) != 0)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
                }

                if ((uint)index > (uint)array.Length)
                {
                    ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
                }

                if (array.Length - index < _dictionary.Count)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                }

                if (array is TKey[] keys)
                {
                    CopyTo(keys, index);
                }
                else
                {
                    object[]? objects = array as object[];
                    if (objects == null)
                    {
                        ThrowHelper.ThrowArgumentException_Argument_IncompatibleArrayType();
                    }

                    int count = _dictionary._count;
                    Entry[]? entries = _dictionary._entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries![i].next >= -1) objects[index++] = entries[i].key;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        ThrowHelper.ThrowArgumentException_Argument_IncompatibleArrayType();
                    }
                }
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            public struct Enumerator : IEnumerator<TKey>, IEnumerator
            {
                private readonly Dictionary<TKey, TValue> _dictionary;
                private int _index;
                private readonly int _version;
                private TKey? _currentKey;

                internal Enumerator(Dictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _version = dictionary._version;
                    _index = 0;
                    _currentKey = default;
                }

                public void Dispose() { }

                public bool MoveNext()
                {
                    if (_version != _dictionary._version)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                    }

                    while ((uint)_index < (uint)_dictionary._count)
                    {
                        ref Entry entry = ref _dictionary._entries![_index++];

                        if (entry.next >= -1)
                        {
                            _currentKey = entry.key;
                            return true;
                        }
                    }

                    _index = _dictionary._count + 1;
                    _currentKey = default;
                    return false;
                }

                public TKey Current => _currentKey!;

                object? IEnumerator.Current
                {
                    get
                    {
                        if (_index == 0 || (_index == _dictionary._count + 1))
                        {
                            ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                        }

                        return _currentKey;
                    }
                }

                void IEnumerator.Reset()
                {
                    if (_version != _dictionary._version)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                    }

                    _index = 0;
                    _currentKey = default;
                }
            }
        }

        [DebuggerTypeProxy(typeof(DictionaryValueCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly Dictionary<TKey, TValue> _dictionary;

            public ValueCollection(Dictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
                }

                _dictionary = dictionary;
            }

            public Enumerator GetEnumerator() => new Enumerator(_dictionary);

            public void CopyTo(TValue[] array, int index)
            {
                if (array == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if ((uint)index > array.Length)
                {
                    ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
                }

                if (array.Length - index < _dictionary.Count)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                }

                int count = _dictionary._count;
                Entry[]? entries = _dictionary._entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries![i].next >= -1) array[index++] = entries[i].value;
                }
            }

            public int Count => _dictionary.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            void ICollection<TValue>.Add(TValue item) =>
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);

            bool ICollection<TValue>.Remove(TValue item)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
                return false;
            }

            void ICollection<TValue>.Clear() =>
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);

            bool ICollection<TValue>.Contains(TValue item) => _dictionary.ContainsValue(item);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() =>
                Count == 0 ? SZGenericArrayEnumerator<TValue>.Empty :
                GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TValue>)this).GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (array.Rank != 1)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
                }

                if (array.GetLowerBound(0) != 0)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
                }

                if ((uint)index > (uint)array.Length)
                {
                    ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
                }

                if (array.Length - index < _dictionary.Count)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                }

                if (array is TValue[] values)
                {
                    CopyTo(values, index);
                }
                else
                {
                    object[]? objects = array as object[];
                    if (objects == null)
                    {
                        ThrowHelper.ThrowArgumentException_Argument_IncompatibleArrayType();
                    }

                    int count = _dictionary._count;
                    Entry[]? entries = _dictionary._entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries![i].next >= -1) objects[index++] = entries[i].value!;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        ThrowHelper.ThrowArgumentException_Argument_IncompatibleArrayType();
                    }
                }
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            public struct Enumerator : IEnumerator<TValue>, IEnumerator
            {
                private readonly Dictionary<TKey, TValue> _dictionary;
                private int _index;
                private readonly int _version;
                private TValue? _currentValue;

                internal Enumerator(Dictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _version = dictionary._version;
                    _index = 0;
                    _currentValue = default;
                }

                public void Dispose() { }

                public bool MoveNext()
                {
                    if (_version != _dictionary._version)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                    }

                    while ((uint)_index < (uint)_dictionary._count)
                    {
                        ref Entry entry = ref _dictionary._entries![_index++];

                        if (entry.next >= -1)
                        {
                            _currentValue = entry.value;
                            return true;
                        }
                    }
                    _index = _dictionary._count + 1;
                    _currentValue = default;
                    return false;
                }

                public TValue Current => _currentValue!;

                object? IEnumerator.Current
                {
                    get
                    {
                        if (_index == 0 || (_index == _dictionary._count + 1))
                        {
                            ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                        }

                        return _currentValue;
                    }
                }

                void IEnumerator.Reset()
                {
                    if (_version != _dictionary._version)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                    }

                    _index = 0;
                    _currentValue = default;
                }
            }
        }
    }
}
