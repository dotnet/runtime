# String Deduplication

## **Problem Space**

Duplicated strings have been frequently observed in managed heaps. In some user scenarios they can take up most of the heap. Folks have been doing their own string deduplication. One of our first party customers did their own deduplication and were able to reduce the heap size by >70% but they could not do this for all workloads they have; only when they can find a feasible place to do so.

A general opt in solution in the runtime would help reduce the heap size significantly for scenarios with a large percentage taken up by duplicated strings.

## **Glossary**

Dedup - string deduplication is often shortened to dedup in this document.

## **Design goals**

This is an opt-in feature and should have no performance penalty when it’s off. And by default it’s off.

When it’s on, we aim to – 

- Only deduplicate strings in old generations of the GC heap.
- Not increase the STW pauses for ephemeral GCs.
- Not regress string allocation speed.
- Provide static analysis and runtime checks to detect patterns incompatible with string deduping. This is required to enable customers to opt-in into this feature with confidence. 

## Details

#### **History** 

The string deduplication feature has been brought up before. See [runtime issue #9022](https://github.com/dotnet/runtime/issues/9022) for discussion. 

And a proof of concept was gracefully [attempted](https://github.com/dotnet/coreclr/pull/15135) by [@Rattenkrieg](https://github.com/Rattenkrieg) before. But it was incomplete and the design didn’t have the kind of perf characteristics desired – it had most of the logic in GC vs outside GC.

An example of a user implemented string deduplication is Roslyn’s [StringTable utility](https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/InternalUtilities/StringTable.cs).

#### **Customer impact estimation and validation**

As a general rule we want to have this for all features we add to the runtime. 

Issue #[9022](https://github.com/dotnet/runtime/issues/9022) It mentioned some general data: 

“The expectation is that typical apps have 20% of their GC heap be strings. Some measurements we have seen is that for at least some applications, 10-30% of strings all may be duplicated, so this might save 2-3% of the GC heap. Not huge, but the feature is not that difficult either.”

But keep in mind that this is an *average* - specific scenarios could have much higher percentage of duplicated strings. I have heard from customers who had much more dramatic heap reduction when they eliminated duplicated strings. Those are scenarios that would benefit from this feature the most.

There are 2 sources of data we could get –

- Our dump database. We can ask the TI team to help us look at the general percent of duplicated strings on the GC heap. It would be useful to also provide tooling to help users to check how much space duplicated strings take. Some internal folks have written tooling for this but it requires work to ship.
- We already know of teams that can benefit from deduping. We should start a conversation with them to collect some data to have a rough idea about the baseline and make sure we have a way to verify private builds with them.

#### **Design outline**

This is an opt in feature. When the runtime detects it’s turned on, it creates a dedup thread to do the work. 

Detection of duplicated strings is done by looking into a hash table. The key into this hash table is the hash code of the content of a string. Detailed description of this detection is later in this doc. 

As the dedup thread goes through the old generations linearly, it looks for references to a string object (denoted by the method table) and either calculates or looks up the hash code of that string to see if it already exists in the hash table. If so it will attempt to change the reference to point to that string with a CAS operation. If this fails, which means some other thread changed the reference at the mean time, we simply ignore this and move on. We expect the CAS failure rate to be very low.

Since the new string reference we will write to the heap has the exact same type info, it means BGC can read either the old or the new reference and will get the same size info so deduping would not conflict with BGC concurrent mark. It does not conflict with BGC sweep because BGC sweep does not read references inside an object.

The dedup hash table acts as weak references to the strings. Depending on the scenario we might choose to null out these weak references or not (if it’s more performant to rebuild the hash table). If we we do the former these weak references would be treated as short weak handles so the following will happen before we scan for finalization -

- During BGC final mark phase we will need to null out the strings that are not marked in the hash table. This can be made concurrent. 

- During a full blocking GC we will need to null out the strings that are not marked in the hash table, and relocate the ones that got promoted if we are doing a compacting GC.

**Alternate design points**

- Should we create multiple threads to do the work? 

Deduping can be done leisurely, so it doesn’t merit having multiple threads. 

- Can we use an existing thread to do the work on? 

The finalizer thread is something that’s idling most of the time. However there are already plenty of types of work scheduled to potentially run on the finalizer thread so adding yet another thing, especially an opt in feature, can get messy.

#### **Which strings to dedup**

Only strings allocated on the managed heap will be considered for deduplication. And only the ones that live in the old generations, meaning gen2 an LOH, will be considered. Deduping in young generations would be unproductive as they will likely die very soon. It would be extremely unproductive if the copy we are deduping to actually lived in young gens.

#### **Calculating hash codes and large string consideration**

Currently calling GetHashCode of a string calculates a 32-bit hash code. This is not stored anywhere, unlike the default hash code that’s stored either in the syncblk or a syncblk entry, depending whether the syncblk is also used by something else like locking. As the deduping thread goes through the heap it will calculate the 32-bit hash code and actually install it.

However, a 32-bit hash code means we always need to check for collision by actually comparing the string content if the hash code is the same. And for large strings having to compare the string content could be very costly. For LOH compaction we already allocate a padding object for each large object (which currently takes up at most 0.4% of LOH space on 64-bit). We could make this padding object 1-ptr size larger and store the address of the string it’s deduped too. Likewise we can also use this to store the fact “this is the copy the hash table keeps track of so no need to dedup”. This way we can avoid having to do the detection multiple times for the same string. Below illustrates a scenario where large strings are deduplicated. 

`pad | s0 | pad | s1 | pad | s0_1` 

`obj0 (-> s0) | obj1 (-> s0_1) | obj2 (->s1) | obj3 (->s0_1) | obj4 (->s1) `

Each string obj, ie, s*, is a string on LOH and has a padding object in front of it which is not shown.

s0_1 has the same content as s0. s1 has the same hash code but not the same content.

"obj->s" means obj points to a string object s, or has a ref to s. So obj0 has a ref to s0, obj1 has a ref to s0_1, obj2 has a ref to s1 and so on. 

1. As we go through the heap, we see obj0 which points to s0.
2. s0’s hash is calculated which we use to look into the hash table.
3. We see that no entries exist for that hash so we create an entry for it and in s0’s padding indicates that it’s stored in the hash table, ie, it’s the copy we keep.
4. Then we see obj1 which points to s0_1 whose hash doesn’t exist yet. We calculate the hash for s0_1, and see that there’s an entry for this hash already in the hash table, now we compare the content and see that it’s the same, now we store s0 in the padding object before s0_1 and change obj1’s ref to point to s0. 
5. Then we see obj2 and calculate s1’s hash. We notice an entry already exists for that hash so we compare the content and the content is not the same as s0’s. So we enter s1 into the hash table and indicate that it’s stored in the hash table.
6. Then we see obj3, and s0_1 indicates that it should be deduped to s0 so we change obj3’s ref to point to s0_1 right away.
7. Then we see obj4 which points to s1 and s1 indicates it’s stored in the hash table so we don’t need to dedup.

Since we know the size of a string object trivially, we know which strings are on LOH.

**Situations when we choose to not dedup**

- If `InterlockCompareExchangePointer` fails because the ref was modified while we were finding a copy to dedup to (or insert into the hash table), we skip this ref.
- If too many collisions exist for a hash code, we skip deduping for strings with that hash code. This avoids the DoS attack by creating too many strings for the same hash.
- If the string is too large. At some point going through a very large string to calculate its hash code will become simply not worth the effort. We'll need to do some perf investigation to figure out a good limit. 

**Alternate design points**

- Should we calculate the hash codes for SOH strings as gen1 GCs promote them into gen2? 

This would increase gen1 pause.

- Should we dedup while creating LOH strings?

This would reduce LOH string allocation speed and potentially by a lot.

#### **How often to dedup**

If there’s no new objects that get into gen2/LOH, there’s no need to run dedup as the duplicates would have already been eliminated (mostly) during the last dedup cycle. So we should use the new amount of objects these generations get to determine how often to dedup, ie, taking the following factors into consideration -

- We know when new LOH strings are allocated (all LOH allocations are done via C++ helpers in gchelpers.cpp), we can take the accumulative size of new LOH strings till it reaches a certain percentage of LOH before starting the next dedup cycle.

- We know how much is promoted into gen2 and we can also wait till a certain percentage of gen2 consists of newly promoted memory before starting the next dedup cycle.

#### **Compatibility**

The following scenarios become problematic or more problematic when deduping is on so we need ways to help users find them.

- Mutating the string content

Strings are supposed to be immutable. However in unsafe code you can change the string content after it’s created. Changing string content already asking for trouble without deduping – you could be changing the interned copy which means you are modifying someone else’s string which could cause completely unpredictable results for them. 

The most common way is to use the fixed keyword:

```c#
fixed (char* p = str)
{
  p[0] = 'C';
}
```

There are other ways such as 

`((char*)(gcHandlePointingToString.AddrOfPinnedObject())) = 'c';`

Or

`Unsafe.AsRef (in str.GetPinnableReference()) = 'c';`

- Locking on a string

Locking on a string object is already discouraged due to a string can be interned. Having string dedup on can make this problematic more often if the string you called lock on is now deduped to a different string object. 

- Reference equality

This example illustrates an example of `ReferenceEquals` that will not work if `_field` was deduped to a different string reference by the time the assignment to a2 happens.

```c#
string _field;

void Test()
{
    _field = new string('a', 1);

    string a1 = _field;
    Thread.Sleep(1000);
    string a2 = _field;

    // This assert can fail with string deduping turned on
    Debug.Assert(ReferenceEquals(a1,a2));
}
```

**Runtime assistance to help with these scenarios**

- Static analysis

For .NET 5.0, there’s a static analysis [plan](https://github.com/dotnet/runtime/issues/30740). Verifying if string content is mutated can be part of that analysis suite. It would also be beneficial to have VS do this verification and give you a warning when you are mutating string content.

To start with we will provide analysis for the following –

1. Using the fixed keyword to modify string content.

I’m seeing that there are almost 600 places in libraries that do `fixed (char*` but hopefully most of them do not actually modify the string content. We should definitely be encouraging folks to switch to using `string.Create` like what [PR#31700](https://github.com/dotnet/runtime/pull/31700) did.

2. Using lock on a string object. 

- Reference equality checks on strings

Since `ReferenceEquals` is performance critical API, we cannot do checks in its implementation. Checks will be done by how it's jitted - when the JIT sees that there are two object references being compared for equality, when a special diagnostics flag is set, it can jit a call to a helper method to do that and that helper method can produce diagnostics when the code is trying to compare identity of two strings.

We do have some libraries that rely on `ReferenceEquals`. We need to figure out what to do about them. See discussion [here](https://github.com/dotnet/runtime/pull/31971#pullrequestreview-355531406).

- Additional checks in heap verification 

Heap verification will now include checks to verify that no one changes the string content after it’s hash is computed. This can be turned on when a certain level of COMPlus_HeapVerify is specified.

- Stress mode

Instead of waiting till the productive moment to start the next deduping cycle, we can have a stress mode where we dedup randomly to catch problems sooner, same idea as GC stress to detect GC holes sooner. 

We could even artificially create duplicates in this stress mode to find places that depend on object identity.

- Backup nonconcurrent dedup mode

If you are running into these problematic scenarios and cannot make changes, and you can tolerate longer STW (Stop-The-World) pauses, we will provide a mode where deduping is completely done in either a full blocking GC or during the STW pause of a BGC. Obviously this would make BGC’s STW pauses much, much longer. This would basically just call the function that the dedup thread calls, but in these STW pauses. If you use a CPU profiler you should be able to see preciously how long the dedup part takes during the STW pause.

We will either provide this as an API (GC.DeduplicateStrings) or as a config that only does dedup during full blocking GCs. The former is flexible and can give you detailed info such as how much space deduplicating strings saved. You could call this leisurely at a time when you know longer latency can be tolerated (eg, the server instance is taken out of rotation so you can tolerate a long pause).

#### **Potential future work**

**Experimenting with RTM**

We might see some performance gains using RTM (Restricted Transactional Memory) on machines with large L1 caches but this is a P2 item. After we have the software solution implemented it would be easy to experiment with this. However, I have doubts it would give perf gain in most scenarios since looking into the hash table is quite a bit of work for RTM and likely will give high abort rate.

**Deduping other types of objects**

We might consider to not limit deduping to just strings. There was a discussion in [runtime issue #12628](https://github.com/dotnet/runtime/issues/12628). 

**Deduping long lived references on stack** 

There might be merit to look into deduping long lived refs on the stack. The amount of work it requires and the return makes it low priority but it may help with some corner cases.