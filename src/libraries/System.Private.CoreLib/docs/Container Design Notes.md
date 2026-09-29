# Container Design Notes
One take-away from the S.C.G.Dictionary vectorization experiment was that we had multiple constraints that were not known to all libraries developers and were only discovered later in the experiment. This document attempts to enumerate them.

## Dictionary

### Consistent Ordering

As long as you restrict yourself to only inserting new items and then - in a final pass - removing items, without ever inserting new items after a removal, the enumeration order of a Dictionary is consistent and matches the original order of insertion. i.e. for the following set of operations:

* Insert `{A,1}`
* Insert `{B,2}`
* Insert `{C,3}`
* Remove key `B`

Enumerating the dictionary will produce the following sequence of pairs: `{A,1}, {C,3}`. This behavior does not depend on the comparer provided when constructing the dictionary. Cloning or resizing a dictionary does not perturb this enumeration order.

### Safe Removal During Enumeration

While inserting items during enumeration will produce an exception, it is safe to remove items from a Dictionary even if you are currently enumerating it. Removing a key will ensure that future MoveNext operations will not produce that key/value pair. This is already documented in the 'Remarks' section of https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2.remove as a .NET Core 3.0 change. Note that this *does not mean remove operations are thread safe*.
Clear operations are also legal during enumeration in addition to Remove. TrimExcess, however, is *not* legal, and will invalidate enumeration.

### Non-randomized String Comparers Transition to Randomized After Collisions

By default, Dictionary will substitute high-performance 'non-randomized string comparers' for the built-in string comparers if provided one of them at construction time. After precisely 100 hash collisions (see `System.Collections.HashHelpers.HashCollisionThreshold`) the container will switch to the originally-provided randomized comparer and rehash, re-generating its hashcodes and buckets. This does not affect the contents of the entries table so it will not violate the consistent ordering guarantee listed above.

This means that the comparer actually being used by the container is not necessarily `Dictionary.Comparer` but is instead a hidden internal comparer, `Dictionary._comparer`. It is important to preserve the rule that `Dictionary.get_Comparer` returns the comparer that was provided when the container was constructed, and ensure that the use of non-randomized comparers is not observable from outside the container.

### HashCode Caching

Dictionary caches the result of `GetHashCode` for a key at insertion time inside of the `Entry` representing the key-value pair, which means that rehashing operations (with the exception of the rehash triggered by a transition to the randomized string comparers mentioned above) will not invoke `GetHashCode`. Dictionary also makes sure to compare the cached hash against the pre-computed hash of the target key during lookups, removals and insertions before calling `Equals` on the provided comparer. These behaviors are observable by end users and covered by tests, but are not guaranteed by documentation.

Passing a Dictionary of identical type to the Dictionary constructor will reuse the existing `_entries` array from the dictionary being copied if possible, so this operation also does not invoke `GetHashCode` when the source and destination comparer are identical.

Documentation states that any key used in a Dictionary must not change in any way that affects its hash after insertion, so it is technically possible to remove or change this caching behavior in the future, but if a user provides a comparer with side-effects, such a change could cause breakage.

### Thread-safety Violations Must Not Cause Memory Safety Errors or Hangs

Dictionary is designed to fail in a memory-safe way when it is manipulated from multiple threads at once without synchronization, and we have multiple tests that verify this. "Improving" Dictionary to be able to perform operations successfully across multiple threads without synchronization will as a result produce test failures and could result in observable behavior changes.

It is okay for a Dictionary to become irreversibly corrupted when manipulated from multiple threads, but it should not lead to buffer overruns or other types of memory safety errors, and operations should never hang.

### Specialization for Structs with Default Comparers

For struct-typed keys where the default comparer is used, key loops in Dictionary are specialized to use `EqualityComparer<K>.Default` directly instead of perform a virtual invoke on a comparer instance. This is represented by having a `null` `Dictionary._comparer`. The JIT performs aggressive optimizations when this specialization is done correctly.

### Prime Bucket Counts

When allocating buckets, we round the number of buckets up to the next largest or equal prime number. This is done to improve collision resistance, so any change to this behavior should be carefully examined to ensure that collision resistance does not suffer.

### Deterministic Serialization

Serialization preserves the order of all key/value pairs in addition to the internal version of the Dictionary and the internal comparer in use.

## HashSet

HashSet's implementation is based on Dictionary's so it inherits the same guarantees and traits as that container.
