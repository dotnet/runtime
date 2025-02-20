# Unordered Vectorized Dictionary

## Background and Motivation

Dictionaries are a very frequently used container, accounting for a significant % of CPU and memory usage in many common workloads. SCG.Dictionary provides a good general-purpose implementation which is well-tuned for memory and CPU usage. However, its API is dated and its implementation does not fully utilize the capabilities of modern processors, and modernizing it without negative impact on existing applications is extremely challenging due to the ordered enumeration invariant.

Facebook's [F14](https://engineering.fb.com/2019/04/25/developer-tools/f14/) hash table and the [Mono dn_simdhash](https://github.com/dotnet/runtime/pull/100386) container show that it is possible to reduce memory usage and improve average CPU usage for many dictionary scenarios by using 128-bit-wide vectors to accelerate lookups and grouping entries into "buckets" of multiple items for better locality.

Based on results from a [working prototype](https://github.com/kg/SimdDictionary/), we can see significant performance wins from introducing a *new* container that takes advantage of modern instruction sets and offers a modernized API.

By making this new container a part of the BCL it can take advantage of BCL-internal APIs like HashHelpers and NonRandomizedStringComparer, and integrate cleanly with CollectionsMarshal. Making it a part of the BCL also provides easy access to performance wins for any customer who sees Dictionary-related bottlenecks. The universality of being part of the BCL also makes it a safe choice for new development when you want performance but don't want to bring in a third-party dependency that might end up abandoned. Adopting a new container like this in reusable libraries is also a much easier sell when it is part of the BCL instead of a new third-party package that would be added to every customer's dependency graph.

## Design Overview

The full representation of the container is a single "buckets" array combined with a few words of internal state (count, capacity, and fastmod multiplier).

Each "bucket" contains 0-13 key/value pairs along with a 16-entry "suffix" vector, where the first 13 bytes (corresponding to each kv pair slot) contain "hash suffixes" and the last three bytes contain the bucket count and internal state for collision resolution.

Looking up an item given its key occurs in multiple steps:

1. Locate the ideal bucket for the key. This is done by taking the modulus of the hash and the number of buckets.
2. Scan the bucket for potentially matching keys. This is done by performing a vectorized (16-wide) scan of the "suffixes" to find the first suffix matching the suffix for the target key. For a quality hash function, the chance for a false positive is ~1% and for a suffix collision is ~8%. If no match is found and multiple collisions occurred in this bucket, we need to scan more buckets.
3. Once we find a potential match within a bucket, we scan all the keys in the bucket starting from that potential match, using the EqualityComparer's Equals method. In the average case, we will check exactly one key and it will match.

In the average case, successful and failed lookups both complete after checking a single bucket and 0 or 1 key(s).

Collision resolution works by "cascading" out of the ideal bucket into a neighboring bucket, continuing until we find a bucket that has space for the item. As long as the number of collisions in the container is not too high, these collisions have a near-zero performance penalty since they can typically be resolved by sharing the bucket and the colliding item may have a different suffix from its neighbors.

## API Proposal

```csharp
public class VectorizedDictionary<K, V> :
    IDictionary<K, V>, IDictionary, IReadOnlyDictionary<K, V>,
    ICollection<KeyValuePair<K, V>>, ICloneable
    where K : notnull
{
    public delegate bool ForEachCallback (int index, in K key, ref V value);

    public readonly IEqualityComparer<K>? Comparer;
    public int Count { get; }
    public int Capacity { get; }
    public KeyCollection Keys => new KeyCollection(this);
    public ValueCollection Values => new ValueCollection(this);

    public VectorizedDictionary ();
    public VectorizedDictionary (int capacity);
    public VectorizedDictionary (IEqualityComparer<K>? comparer);
    public VectorizedDictionary (int capacity, IEqualityComparer<K>? comparer);
    public VectorizedDictionary (VectorizedDictionary<K, V> source);
    public void EnsureCapacity (int capacity);
    public void Clear ();
    public V this[K key] { get; set; }
    public bool ContainsKey (K key);
    public bool ContainsValue (V value);
    public bool TryGetValue (K key, out V value);
    public V AddOrUpdate (K key, V addValue, Func<K, V, V> updateValueFactory);
    public V GetOrAdd (K key, Func<K, V> valueFactory);
    public void Add (K key, V value);
    public bool TryAdd (K key, V value);
    public bool Remove (K key);
    public void ForEach (ForEachCallback callback);
    public void CopyTo (KeyValuePair<K, V>[] array, int index);
    public Enumerator GetEnumerator ();
    public RefEnumerator GetRefEnumerator ();
    public bool TryGetAlternateLookup<TAlternateKey> (out AlternateLookup<TAlternateKey> result);

    public readonly struct AlternateLookup<TAlternateKey>
        where TAlternateKey : notnull, allows ref struct
    {
        public readonly VectorizedDictionary<K, V> Dictionary;
        public readonly IAlternateEqualityComparer<TAlternateKey, K> Comparer;

        public V this [TAlternateKey key] { get; }
        public bool ContainsKey (TAlternateKey key);
        public bool TryGetValue (TAlternateKey key, out V value);
        public bool TryAdd (TAlternateKey key, V value);
    }

    public struct KeyCollection : ICollection<K>, ICollection {
        public readonly VectorizedDictionary<K, V> Dictionary;

        public struct Enumerator : IEnumerator<K> {
            private VectorizedDictionary<K, V>.Enumerator Inner;

            public K Current => Inner.CurrentKey;
            object? IEnumerator.Current => Inner.CurrentKey;

            internal Enumerator (VectorizedDictionary<K, V> dictionary);

            public void Dispose ();
            public bool MoveNext ();
            public void Reset ();
        }

        internal KeyCollection (VectorizedDictionary<K, V> dictionary);

        public int Count => Dictionary.Count;

        public Enumerator GetEnumerator ();
    }

    public struct ValueCollection : ICollection<V>, ICollection {
        public readonly VectorizedDictionary<K, V> Dictionary;

        public struct Enumerator : IEnumerator<V> {
            private VectorizedDictionary<K, V>.Enumerator Inner;

            public V Current => Inner.CurrentValue;
            object? IEnumerator.Current => Inner.CurrentValue;

            internal Enumerator (VectorizedDictionary<K, V> dictionary);

            public void Dispose ();
            public bool MoveNext ();
            public void Reset ();
        }

        internal ValueCollection (VectorizedDictionary<K, V> dictionary);

        public int Count => Dictionary.Count;

        public Enumerator GetEnumerator ();
    }

    public struct Enumerator : IEnumerator<KeyValuePair<K, V>>, IDictionaryEnumerator {
        public K CurrentKey { get; }
        public V CurrentValue { get; }
        public KeyValuePair<K, V> Current { get; }
        public bool MoveNext ();
        public void Reset ();
        public void Dispose ();
    }

    public ref struct RefEnumerator {
        public ref readonly K CurrentKey { get; }
        public ref readonly V CurrentValue { get; }
        public KeyValuePair<K, V> Current { get; }
        public bool MoveNext ();
    }
}

public static partial class CollectionsMarshal {
    public static ref readonly V GetValueRefOrNullRef<K, V> (this VectorizedDictionary<K, V> self, K key)
        where K : notnull;

    public static ref readonly V GetValueRefOrAddDefault<K, V> (this VectorizedDictionary<K, V> self, K key, V defaultValue = default!)
        where K : notnull;
}
```

## API Usage

Identical to SCG.Dictionary, with the caveat that a few operations like TrimExcess are omitted and there are some new methods available. I can write up example code to address any questions you have.

## Benefits

### Superior Performance vs SCG.Dictionary

| Scenario | % of BCL (Ryzen) | % of BCL (Intel) |
|---------|-----------------:|-----------------:|
| Clear Full | 101.50% | 61.59% |
| Clear Partial | 115.31% | 98.49% |
| Fill With Hash=0 | 79.59% | 71.47% |
| Find Existing Hash=0 | 73.58% | 82.58% |
| Find Missing Hash=0 | 75.91% | 79.71% |
| Clear, Then Refill | 81.91% | 74.20% |
| Insert Existing Item | 78.77% | 49.05% |
| Enumerate KVPs | 93.76% | 92.58% |
| ContainsValue | 66.37% | 70.30% |
| Find Missing Item | 106.77% | 71.83% |
| Remove Items, Then Refill | 90.86% | 62.31% |
| Remove Missing Item | 115.06% | 62.37% |
| Find `<string, long>` | 63.28% | 65.41% |
| Find `AlternateLookup<ReadOnlySpan<char>, long>` | 52.95% | 48.40% |
| Find `<long, long>` | 72.30% | 51.17% |
| Find `<int, long>` | 93.29% | 134.83%`*` |

`*` This benchmark is severely bimodal on Intel, possibly due to unpredictable alignment of allocations.

### Superior Performance for Some Degenerate Hashes

For some types of bad hashes, including "all items have a hash of `0`", this container has ~25% better performance on insertion and lookups, which scales up to large numbers of collisions. This does not hold for all types of bad hashes, which is explained in the Risks section below.

### Reduced Average Memory Usage

Tests across a range of sizes (0-8419 items) show that memory usage is significantly reduced (~25%). This has various benefits, including...

### Better GC Performance

Instances are simpler for the GC to scan because the backing array for a given set of items is typically smaller than the equivalent backing arrays for SCG.Dictionary. This is supported by significantly reduced CPU time and memory usage in BDN measurements.

### Potential for Better Security

A new dictionary is an opportunity to do more security engineering with regard to hash collisions and poor hashes. For example, we can detect degraded buckets and respond by defensively wrapping the comparer with one that avalanches the hash bits or by doubling the container size to improve collision resistance. These mitigations would work for all key types, not just String.

### Modernized API

We can offer a modernized API with easier to use, higher performance primitives like AddOrUpdate, GetOrAdd, ForEach and GetValueRefOrAddDefault.

### Better Performance for Large Keys and Values

We can make use of `in` and `ref` liberally in the API and implementation to improve performance for large keys and/or values by reducing copies, i.e. making Add accept `in V`. (The current prototype doesn't do this in its public API.)

As a proof-of-concept, I did an experiment using `int` keys and 1024-byte values, comparing the efficiency of GetValueRefOrNullRef with that of regular TryGetValue. The improvement there was **~77%**.

### Modernized Implementation

By taking advantage of modern language and runtime features like static interface methods, the implementation can be smaller and less complex than that of SCG.Dictionary while delivering high performance. SCG.Dictionary's FindValue method is nearly 100 lines due to the micro-optimizations and concurrent operation defenses that have to be baked into it, and its TryInsert is around 130 lines. The equivalent methods in the prototype are much simpler. For example:

```csharp
private ref Pair FindKey<TKeySearcher> (K key, IEqualityComparer<K>? comparer)
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
```

## Risks and Tradeoffs

### Non-vectorized Targets

Some target architectures don't support vectorization. At present, the scalar search fallback implementation for this container does not outperform SCG.Dictionary; it's possible it could if we gain support for cmov-inside-loops in NET10. See https://github.com/dotnet/runtime/issues/109940

Currently NativeAOT generates meaningfully worse code for this container because (I think?) it doesn't know which x86-64 instructions the target machine will have available. I'm not sure if this is fixable. It does vectorize, it's just less efficient.

### Higher Minimum Size

For very small dictionaries, the use of multi-item buckets increases this container's memory usage compared to that of a single-item-bucket container. For an application with many such dictionaries, migrating to this container would regress memory usage and might regress lookup performance as well. In practice this means that while SCG.Dictionary uses more memory per item, its theoretical "minimum capacity" in items is 1 while VectorizedDictionary's minimum capacity is however many items fit into one bucket.

Tests across a range of small sizes (0-28 items) show that the average increase is only 5%, however. As long as the user does basic testing of their application post-migration, this is unlikely to be a problem.

This can also be mitigated by reducing the amount of load factor "padding" the container allocates, but this comes with a performance penalty in exchange for the memory savings.

### Expensive Comparers

Due to the different search algorithm, in some cases this container will invoke the EqualityComparer's Equals method more often than SCG.Dictionary. I believe this will not be a problem for most use cases, and there are straightforward workarounds (add a cached hashcode + quick check to the comparer; use SCG.Dictionary) for this problem.

Rehashing operations will also need to call GetHashCode once-per-item during the rehash operation. This could be problematic if a comparer's hashing operation is expensive and uncached. In my testing this is not a significant issue, and rehashing occurs less often.

SCG.Dictionary solves these scenarios by storing the full hashcode next to each item and reusing it to skip Equals calls + accelerate rehashing. However, the RAM/bandwidth costs of doing so are not insignificant, especially for small key/value types and for fast hash functions.

If we decide that this tradeoff is unacceptable, we could update the bucket type to cache the HashCode as SCG.Dictionary does; nothing else would need to change and it would just throw away a lot of the memory usage improvements.

### Use of ref/Unsafe

The prototype uses Unsafe.Add and refs in various places to achieve optimal performance, in part because current JIT codegen for various Span operations is suboptimal. A production implementation might need to pay the cost of various additional field loads and bounds checks to meet our security standards (though I built various safeguards against buffer over/underrun into the prototype, that's no guarantee of anything.)

Upcoming JIT improvements in NET10 could help mitigate this. It's possible that making changes to the structure of the container itself could allow reducing the number of bounds checks or hoisting more of them out of loops.

### Slower Worst-case Clear Performance

SCG.Dictionary is able to outperform this container for clear operations in many cases because of how it stores entries (sequentially in an array) - it doesn't have to clear its whole buffer. VectorizedDictionary by design needs to clear all of its buckets, though it is possible to skip over buckets that are known-empty, which helps a little bit. I'm not certain whether clearing dictionaries is on the hot path for any real application scenarios to the point that this could become a bottleneck.

(For reference, "slower" in this case is around 2x on average, not 10x. The overhead scales off the capacity of the container.)

### Slower Performance for Some Degenerate Hashes

While this container outperforms SCG.Dictionary with optimal hash functions, identity hash functions (`int GetHashCode () => this;`) and null hash functions (`int GetHashCode () => 0;`), a maliciously-designed or degenerate hash will perform worse, with a performance penalty in the range of 1.5-3x. It's possible this could be mitigated with design changes, and I have some ideas for how to do it.

At present the default hash function for `int` in the BCL happens to function as a degenerate hash for this container if the keys are sequential. I think the solution for this is to manually avalanche the bits for integer keys, but I haven't tried it.

### Cache Line Alignment

This type of container performs optimally if buckets are cache line aligned. It is not currently possible to do this in .NET unless you know your key and value types at compile time. In some degenerate cases, the lack of cache line alignment appears to cause nondeterministic performance - on my Intel hardware for one scenario, BDN measurements are bimodal, bouncing between 'much faster than SCG' and 'much slower than SCG', while on Ryzen the effect does not occur.

Improvements to the type system/JIT/GC to allow requesting cache line alignment would resolve this.

Similarly, restricting bucket sizes to powers of 2 would also improve lookup performance by turning `imul`s into bit shifts. (Any `idiv`s would also become shifts, but this container is designed to never use `idiv`.)

### Code Size Increase

At present, generated code (according to BDN) is bigger than SCG.Dictionary in some (but not all) cases, in the range of 10-55%. There *are* cases where it is 25% smaller or more, however. I don't fully trust these numbers and they may be caused by inlining and/or other optimizations.

### Lack of Ordered Enumeration

SCG.Dictionary allows enumerating items in insertion order in many common scenarios; this container cannot do so (it only stays ordered if there is exactly one bucket.)

I think this is an acceptable tradeoff. I prototyped a version of this container with ordered enumeration and the performance was significantly worse. Users who need ordered enumeration can keep using SCG.Dictionary.

### Large Object Heap

SCG.Dictionary splits its data across two smaller arrays, while all data for this container lives in a single array. This means that the backing store for the dictionary could cross the large object threshold sooner than it would under SCG.Dictionary.

## Miscellaneous Notes

* I've tested 256-bit-wide vectors and they're not faster, so the compatibility benefits of 128-bit vectors (supported by WASM, for example) seem to suggest they are the right size.
* The actual layout and implementation of this container are fairly different from dn_simdhash, that's just how it ended up. I don't know how it compares to F14's underlying representation and algorithms because I haven't read their code 🙂
* It is theoretically possible to do in-place rehashes of this container, with the downside of some buckets becoming erroneously degraded. I haven't bothered to prototype this, though.
* Various null checks and length-0 checks are optimized out by giving empty dictionaries a static 1-element EmptyBuckets array. We could probably apply this optimization to SCG.Dictionary too, but I'm not sure it would matter.
* Concurrent modifications are less hazardous than in SCG.Dictionary since we have one array instead of two - a single `Span<Pair>` is all you need to safely scan the container, unless you're doing FastMod trickery.
* Because there is no entries array, we don't have to manage a freelist. This is responsible for some of the code size and memory usage wins.
* Most operations have optimal performance without any sort of load factor mechanism to over-allocate buckets; the exception appears to be searches for missing items.
* TrimExcess is omitted because that operation isn't straightforward with this data model; if it's really needed by customers we could implement it by rehashing 'down' to a smaller size.
* I haven't tested this on ARM or WASM yet, but dn_simdhash has been tested and proven there.
* The JIT's allocation of x64 vector registers is somewhat suboptimal at present. We could see some measurable wins for this container if it gets smarter in the future, which would significantly reduce the amount of stack it uses. See https://github.com/dotnet/runtime/issues/108092
* The prototype has been optimized at an instruction and register level, so that many core operations can complete without ever touching stack. I just think it's neat that it's possible to do this in C#. 🙂
