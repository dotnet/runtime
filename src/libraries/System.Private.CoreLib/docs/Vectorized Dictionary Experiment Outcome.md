# (Not) Vectorizing the .NET Dictionary class

I recently concluded research into using SIMD to improve performance and reduce memory usage for [the default Dictionary type in the .NET Base Class Library](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-9.0). The result of this research was negative - at present, it doesn't appear to be possible to deliver speed and memory improvements without breaking existing applications. But why did I qualify that statement? The answer is complex, and comes down to a combination of platform limitations, promises we made to developers and end users in the past, and the realities of shipping and maintaining software long-term. Let's start at the beginning!

## Origins

In early 2024, I was doing in-depth performance measurements to understand where our time went during start-up for Blazor applications. The time was going lots of places, but one thing that showed up frequently in call stacks was the hashtable type used by the Mono runtime, [GHashTable](https://github.com/mono/eglib/blob/master/src/ghashtable.c). It was last modified approximately 14 years ago, and has generally bad performance characteristics in multiple areas, so it seemed like a good target for improvement.

As it happens, I had recently read a blog post about the [F14 hash tables](https://engineering.fb.com/2019/04/25/developer-tools/f14/?r=1) and it occurred to me that a similar data structure could be the answer for us. The description from that blog post was enough for me to understand the principles behind the data structure and build my own vectorized hashtable - a really excellent piece of technical literature.

### Origins pt. 2: dn_simdhash

The result of this effort was a data structure called [dn_simdhash](https://github.com/dotnet/runtime/tree/main/src/native/containers), a portable vectorized hash table written in C with type specialization via macros. I was able to substitute it for the classic GHashTable data structure in various parts of the Mono runtime, delivering improvements to both CPU usage during startup and overall memory usage. Job done, right? But did I just start by writing C from scratch? As it turns out, no...

### Origins pt. 1.5: SimdDictionary 1.0

[My original prototyping](https://github.com/kg/SimdDictionary/) was done in C#, using the vector intrinsics available in .NET 8.0 (the latest official version at the time). I liked the rapid iteration and debugging experience available in C# and Visual Studio, and I liked [the de-facto benchmarking tool for .NET, BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet). I've still never found a good benchmarking library or tool for C, so I ended up needing to roll my own for dn_simdhash... but I digress.

My initial prototyping phase in C# lasted around 4 days, and I experimented with various layouts for the data structure while also building a small suite of benchmarks. My goal was to construct a simple vectorized dictionary type as a proof of concept, and compare it with one of the best-in-class scalar dictionary types available to me, [.NET's System.Collections.Generic.Dictionary](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-9.0). It may be surprising to hear that I consider this implementation to be best-in-class (or at least close to it), but it really does perform well, and if you [read the source code](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs), you can tell why. It's been carefully constructed and hand-optimized to deliver very good performance given the constraints it operates under. What constraints? I'll get to that later. In any case...

At the end of these 4 days, I had a working vectorized generic dictionary in C# with good performance and a fairly simple implementation. I was confident I could write a similar data structure in C based on this experience and the benchmark data, so I began work on dn_simdhash in late March, landing the initial PR in mid-April. Over the next few months I improved the C implementation and migrated various parts of the Mono runtime to using it. Eventually, all the hotspots I'd seen in my startup measurements were more or less dealt with, so I moved on to other tasks.

## SimdDictionary emerges from the dark

But it kind of ate at me a bit. The C# prototype performed pretty good, didn't it? Maybe I should do something with it? And occasionally people would say - hey, that simdhash thing turned out pretty good. Should we do that to [the native hash table in the .NET runtime](https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/shash.h), too? (maybe! I still need to look into it...) Should we do that to the C# Dictionary in the .NET BCL? Eventually I decided to give it a serious look in late September of 2024, as we began an experiment/research phase on our team and time rolled forward into the holidays.

This meant it was time to dust off the abandoned prototype repository and approach the task more seriously. I began by examining the existing Dictionary type in depth and [requesting guidance from people who knew it well](https://github.com/dotnet/runtime/issues/107830) - how good is our test coverage, what are the subtle behaviors it needs to have, what scenarios does it need to handle, what targets does it need to run on, etc. The replies I got helped me understand the problem much better, and examining the BCL source code helped inspire some improvements in my prototype.

 Once I felt I had enough information to begin, I resurrected my old C# prototype repository.

## A research experiment resumes with myriad twists and turns

I began by rewriting the prototype from scratch, but kept the tests and benchmark suite - the original had been cobbled together quite messily and wasn't a good foundation for ongoing work. This only took around a day or so, and after that I began refactoring the new prototype to improve code quality and make it easier to work with. Because I now had a new goal - determine whether it was possible to improve on the existing Dictionary in the BCL via vectorization - this influenced some of the decisions I made, like imitating optimization techniques from the BCL. Some of these optimization techniques lowered code quality or increased compiled code size in exchange for better performance, which irritated me.

One central design element that changed multiple times was the arrangement of buckets, keys, and values. The [F14 blog post](https://engineering.fb.com/2019/04/25/developer-tools/f14/?r=1) explains a lot of these concepts better than I can, but to summarize it briefly, a traditional Dictionary in the vein of the BCL's has "buckets" and "entries", where all possible hash codes are mapped to specific buckets, and then buckets are pointers to entries. Entries contain the actual Data part of the data structure - the keys and values, along with information that can be used to resolve hash collisions - in the case of the .NET Dictionary, Entries form a linked list when multiple items need to occupy a bucket, so the Entry structure contains link information to achieve this, and it also stores free-list information for managing empty slots.

A F14 style hash table takes a different approach to buckets. Instead of having an array of buckets where each bucket points to an entry, buckets are now vectors, where each vector contains a number of slots (up to 14, hence the name F14... I think?) for "suffixes", along with additional metadata. This vector of suffixes enables fast search within the container because you can use SIMD instructions to scan all the suffixes at once instead of having to sequentially check them one at a time. This layout also enables some other smaller optimizations.

## Many possible layouts

As I mentioned, the arrangement of buckets, keys and values changed multiple times. I experimented with a few arrangements:

1. A container with separate Bucket[], Key[] and Value[] arrays. This has some nice properties - it's easy to write, the indices all (mostly) line up, there are no weird data types to stare at, and you can write really mundane code. It's where I started, and is how dn_simdhash ended up because it was good enough for our needs at the time. But the performance is far from optimal because of extra work done to index into 3 separate arrays.
2. A container with separate Bucket[] and Entry[] arrays, where Entry contains a Key/Value pair. This improves on #1 by reducing the amount of time we spend indexing arrays (that is, computing addresses of array elements). When you're writing a critical piece of infrastructure like a hash table, this kind of overhead matters. The complexity increase from this change is actually not too bad, and in some cases it made the code smaller and simpler to begin working in terms of Entries (or Pairs, as I called them at the time.). This is the layout that most closely resembles the existing Dictionary in the BCL.
3. A container with separate Bucket[] and Value[] arrays, where each Bucket also contains all the keys associated with the bucket. The idea with this approach is that the most common, expensive operation - scanning a bucket's keys to locate an exact match - would occur on data that is already in cache since the bucket suffixes and keys live right next to each other in memory. More importantly, doing this means that once we have the address of the bucket to check, we know where the keys are - we don't need to index into an Entry or Key array to find the keys. This was a nice performance win, at the cost of significantly increased complexity, and the loss of perfect cache line alignment for buckets. (It's possible to make buckets cache line aligned with this design in specific scenarios, but doing it generally is a tall order, at least in C#.)
4. A container with a single Bucket[] array, where each Bucket contains all the key/value pairs associated with the bucket. Optimizing out the expensive operation of indexing into the Keys array was helpful, after all. Why not optimize out the Values array too? This is a big performance win, at the cost of more code complexity. And some other problems start to bare their fangs at you in the process... however, this is ultimately the layout I went with for most of my research, because I liked the upsides and didn't mind the downsides. I'll explain in detail [why I chose this layout in an appendix below](#why-layout-4).

## An iterative series of optimizations and deoptimizations

Once I arrived at the layout that I felt had the most potential, I started thinking more seriously about how to keep code size down and improve performance. The existing Dictionary implementation contained a lot of code duplication for performance reasons, and so did my prototype. I wanted to find a way to eliminate that duplication without adding painful overhead - every bit of duplication was making it harder to change the container, and I kept accidentally introducing bugs by changing code in three places when I needed to change it in five places.

This led to a process of aggressively refactoring small pieces of the design or implementation, often causing things to get slightly slower before eventually bringing them back to their prior speed, or occasionally making them even faster. During this whole process I relied on a comprehensive set of 'self-test' scenarios alongside a suite of around 100 custom microbenchmarks. For many changes I knew they would only affect a subset of the benchmarks, so I took advantage of BDN's selector to only run the suites I cared about so I could iterate faster.

Out of all the refactorings and tricks I put into use, there are two things that I think had the biggest impact, so I'll call them out now!

### Static Interface Methods

[C# 11 added support for defining abstract static methods on interfaces](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/static-abstracts-in-interfaces) and I made aggressive use of this feature here to remove duplication while maintaining great performance. My realization was simple: Most of the duplicated code was trying to respond to information about the dictionary keys and their comparer. I could encapsulate all this information into a structure with static methods on it, and then write all my algorithms once as generic methods accepting an appropriate structure, so long as I made sure to use structures and not classes [to avoid canonization](https://alexandrnikitin.github.io/blog/dotnet-generics-under-the-hood/).

Before exploiting SIMs, I had multiple methods with duplicated loop bodies, like this:

```csharp
if (typeof(K).IsValueType && (comparer == null)) {
    foreach (...) {
        // ...
        var b = EqualityComparer<K>.Default.Equals(needle, haystack[i]);
        // ...
    }
} else {
    foreach (...) {
        // ...
        var b = comparer.Equals(needle, haystack[i]);
        // ...
    }
}
```

As you might expect, these loop bodies can get quite large when you're writing a data structure, so the amount of duplicated code reached hundreds of lines. Any time I made data structure or algorithm changes, I had to update 6 or more different locations by hand to make sure they were all correct, or things would break.

With SIMs in play, I write the loop body once in the form of a helper method, and then call it multiple times, like this:

```csharp
if (typeof(K).IsValueType && (comparer == null))
    return HelperMethod<DefaultValueTypeComparerSearcher>(null, ...);
else
    return HelperMethod<ComparerSearcher>(comparer, null);
```

Since `K` is known at JIT time in practice only one of these calls to HelperMethod will ever occur and the unused one won't get compiled, similar to the optimizations that apply to the hand-duplicated loop body above.

### Struct Promotion

Over the past few years [.NET gained support for replacing aggregates - structs - with scalars](https://github.com/dotnet/runtime/issues/76928), effectively turning a struct local into a cluster of individual local variables. This enables a bunch of cool optimizations, like complete removal of unused struct fields, and it also means that inlining becomes way more powerful. This enabled one of my refactorings to produce huge returns - at the time I wasn't even aware .NET had this feature, but it does!

Before exploiting this, all my methods had similar high level structures with duplication, like in this pseudocode:
```csharp
var comparer = this._Comparer;
Span<Bucket> buckets = this._Buckets;
var hashCode = comparer.GetHashCode(needle);
var bucketIndex = unchecked(hashCode % buckets.Length);
ref var firstBucket = ref buckets[0];
ref var lastBucket = ref buckets[buckets.Length - 1];
ref var initialBucket = ref buckets[bucketIndex];
ref var bucket = ref initialBucket;
do {
    // ... do something with the current bucket ...

    if (Unsafe.AreSame(ref bucket, ref lastBucket))
        // We just scanned the last bucket, wrap around to the first one.
        bucket = ref firstBucket;
    else
        // Advance to the next bucket.
        bucket = ref Unsafe.Add(ref bucket, 1);

    // If we reached our initial bucket that means we've scanned every bucket and are done.
} while (!Unsafe.AreSame(ref bucket, ref initialBucket));
```

It's awkward just looking at it. Now imagine there are 12 different versions of this - or more - and you need to carefully update each one when making changes. I kept screwing it up! So I introduced a 'looping bucket enumerator' type that encapsulated most of this, resulting in code like this instead:

```csharp
var hashCode = comparer.GetHashCode(needle);
ref var bucket = ref NewEnumerator(hashCode, out LoopingBucketEnumerator enumerator);
do {
    // ... do something with the current bucket ...

    bucket = ref enumerator.Advance();
} while (!Unsafe.IsNullRef(ref bucket));
```

I expected this to make my code slower, but it didn't, because the .NET JIT and NativeAOT compilers are both smart enough to turn the bucket enumerator struct into individual locals and optimize out the unused ones. With enough careful engineering, I was able to make some of my most important methods fit entirely into x64 registers - no stack needed!

## So everything's good, right?

I had a prototype with code quality I really liked, and the performance numbers - according to my microbenchmarks - were fantastic. Consistently a bit faster on my Ryzen system, and consistently a *lot* faster on my Intel system. Let's open a PR and get it merged, right? Not so fast!

As I got closer to having a prototype I considered "complete", I had more discussions with a wider set of people and learned some surprising things about the existing .NET Dictionary:

1. It actually preserves insertion order, as long as you don't remove any items!
2. It's legal to remove items from it while enumerating it!

The first item was a big surprise and quite troubling since as mentioned before, it's hard to make this data structure ordered. Not impossible, of course, but difficult. And the second item was also quite surprising, but is relatively easy to handle with this data structure. So in practice, only one road block had appeared, but it was a big one.

## The ordered detour

I now had to devise an ordered version of this container and try to maintain acceptable performance. I ended up carefully examining the BCL dictionary and replicating its Entry type, preserving the linked list for the purposes of maintaining a freelist to allow tracking which entry slots were empty and reusing them when adding new items after removal. This touched almost every part of the container, but thanks to the encapsulation the commit to do it still only changes around 400 lines.

All good, right? Unfortunately, no. The performance was no longer that much better than the BCL, and the memory usage was no longer better than the BCL's. I spent a while trying to optimize it, but eventually hit a wall.

But I had at least proven it was possible, so I marked a git tag and moved on, going back to the design I believed in - unordered, using layout 4. My conclusion was that at this point, it only made sense to either relax guarantee #1, or introduce a new vectorized dictionary that offered superior performance for people who wanted it.

I continued working on the unordered dictionary, improving it to match the full feature set of the BCL's current type, improving the performance, reducing code size, etc.

## Real integration, pt. 1

In the background, discussions continued about how to deal with the ordered guarantee. Could we relax the guarantee? Could we remove it entirely? Make it an application-wide configuration flag you could turn on for better performance? Should we introduce a new container instead?

In the interim, I decided to take the necessary step of getting real, comparable data - make a custom build of .NET that used my Dictionary implementation instead of the current one. I prepared all the source code from SimdDictionary for insertion into the BCL and then copied all of the files into the appropriate place inside of a fork. After this, I incrementally performed all the renames and edits necessary to satisfy the strict code quality analyzers applied to the BCL, and started running tests.

Initially, I had around 400 test failures. The majority of these broke down into a few categories:

* We were testing very nuanced, specific behaviors of the Dictionary - essentially, "is Dictionary behaving like Dictionary in this corner case" tests. Things like "if you request a size of 10, do you get a size of 13 (because of implementation details)".
* We were testing for specific behaviors I had overlooked. One important one was DDoS mitigations for string hash collisions - I had made the decision not to implement them in the prototype until I knew they were necessary, so it was expected to see these tests fail, and any further integration would require those tests to pass.
* We were testing for specific exceptions when a Dictionary is accessed concurrently by multiple threads. These scenarios no longer threw exceptions because the new implementation was too robust, and because the way the tests tried to corrupt its internal state didn't actually corrupt its internal state anymore, which resulted in a test failure.
* The exact shape of the type needed to be unmodified for various binaries relied on by tests to work - for example, argument names couldn't change, nullability of specific arguments needed to be the same, etc. Because I had transplanted a new implementation in, even though it was source-compatible it was not binary-compatible and this initially broke things like XUnit until I made enough changes, and it continued to cause problems.
* The vectorized dictionary was too liberal about what operations it allowed you to perform without errors, for example it permitted you to add items during enumeration since it was not hazardous to do so. Naturally, the BCL has tests to ensure that we prevent this, since it's against the rules.

My takeaway from this exercise was that it was still possible to move forward, but the right path was not to integrate the prototype. The right path was to rewrite the current Dictionary class in-place, based on the prototype, preserving as much code as possible to hopefully keep all the tests passing.

## Real integration, pt. 2

So that's what I did. I took the existing Dictionary class and applied this high-level process:

1. Change the underlying data structures, like buckets and entries, to look the way I know they need to look
2. If code starts producing build errors, delete that code and replace it with a FIXME.
3. Go through and replace each FIXME with newly written code based on consulting both the prototype and the original BCL implementation
4. Where possible, fix tests.

This resulted in [a changeset of around 750 lines, including changes to tests](https://github.com/dotnet/runtime/pull/110701). The final run of our full test suite showed a total of 61 test failures out of 3,757,348 total tests. Not bad!

Unfortunately, this meant that the resulting data structure was ordered, so it would probably have the mediocre performance I had seen in the original ordered prototype. I had proven that this was possible, at least - the remaining test failures were all fixable. The next step was to gather real data.

## Gathering data

The .NET development team has compiled [a big set of microbenchmarks and functional tests](https://github.com/dotnet/performance) that are run regularly and compared automatically to spot regressions. For scenarios like mine, [there is a documented process to compare one version of the runtime against another](https://github.com/dotnet/performance/blob/main/docs/benchmarking-workflow-dotnet-runtime.md#preventing-regressions)! After overcoming a few process/environment related roadblocks like BenchmarkDotNet not being able to find my custom build of the runtime, I was able to run all the Dictionary-related benchmarks in about an hour, and I collected separate control and vectorized measurements so I could see what the actual performance situation was using the diffing tool. Once this was done, I generated the diff and began to analyze it.

## The results

The results were bad! This was demoralizing, but it's something you have to get used to when doing performance work - even if you spent weeks on an optimization or a design overhaul, in the end it might end up being worse than what you had before. That was the case here.

The diffs showed that in some cases the new vectorized, ordered dictionary was only 5% slower - the sort of thing you can probably fix by examining generated code and tweaking things until it's faster - but in other cases it was 50% slower, or even worse. Those kinds of regressions are much harder to fix, and it's a bad starting point for something that aims to be an improvement.

And I hadn't run memory usage measurements yet.

## The outcome

By this point our discussions internally had led me to the conclusion that the ordering guarantee was unchangeable - it simply wasn't possible to ship a version of Dictionary that wasn't ordered. And the idea of shipping this unordered vectorized Dictionary as a *new* type in the BCL was a tough sell - we already have so many Dictionaries, after all, do we really need another one? How many users would benefit from another type since they'd have to change all their code, and change all the libraries they use as well in order to benefit from this new faster container?

So my final judgment call was to shelve this effort. The research was complete and the result was "nope, not today." Perhaps in the future a breakthrough will enable us to revisit this idea, who knows? Regardless, I learned a lot about various things in the process, so this was a valuable research project.

If you've read this far, you're probably getting tired. Thanks for sticking with this post until the end! I have some more detailed technical notes here for you if you're curious, though, so here they are:

# Appendices

## Staring at every x64 opcode in your method bodies and asking "why are you here?"

When your goal is to beat one of the best data structures out there at its own game, you may end up in a situation where the number of instructions inside a method matters, and what those instructions are matters. At that point it's no longer enough to just mess with code and run benchmarks - you're kind of searching around in a dark room, unable to see anything. You might eventually find the right lever to pull, but it's better to just turn on a flashlight.

The flashlight in this case was [a tool called Disasmo](https://github.com/EgorBo/Disasmo/), which integrates into Visual Studio and allows you to summon a full dump of the native code generated for a given C# method. It took some fussing and I encountered some bugs in the process, but I eventually arrived at a process where I could make changes to the container and then press Reload in the disasmo window to figure out whether I had improved the generated code or not. This helped tremendously because once I spent enough time staring at the generated code, I could understand which lines of C# were generating which instructions and I started to form a deeper understanding of the data structure's performance.

This is what led me to realize that .NET has [Struct Promotion](#struct-promotion) support - I was looking for field accesses in my generated code and they simply weren't there, because they had been optimized out! A pleasant surprise.

Many of the smaller optimizations I made were one-line changes to reduce a method body's size by 4 bytes. In isolation, that's probably only a 1% speed improvement, or it may not make things faster at all, but collectively 10 optimizations like this can add up to a bigger improvement by making something start fitting into cache that didn't fit before.

A critical use of this was stack pressure optimization - by examining the generated code and seeing where things spilled out of registers into memory (the stack), I was able to form an understanding of how much "room" I had for temporary state and eventually optimize things so that key operations would occur entirely inside of registers. Doing this without seeing the actual generated code would have been nearly impossible, because the way registers actually get allocated rarely matches your expectations. In some cases code that "looked faster" was actually slower, and the root cause of that slowness was because it caused registers to spill to the stack.

## When code is fast and slow at the same time

If you spend enough time writing and running BenchmarkDotNet benchmarks, you may see something in your results about modality, or it may tell you that one of your benchmarks is "bimodal". In layman's terms, what this means is that instead of having a single speed, something is causing your benchmark to rotate between multiple different speeds. These can be caused by various things - global state, like a bool that gets flipped or a buffer that fills up, perhaps. Or in this case - memory addresses!

Each time you allocate an array in .NET its location in memory is basically up to chance. If you have an algorithm that is very sensitive to the behavior of the CPU's caches - like a data structure - this location in memory can become important because it influences the behavior of the cache and the speed of individual memory operations. Up until this point I had yet to experience this for myself, but some of the SimdDictionary benchmarks turned out to be bimodal, and were in fact *only bimodal on certain PCs*!

Addressing problems like this requires an even deeper understanding of generated native code and how it runs on your target hardware. Given the outcome of this research, I didn't get to the point of fixing this particular issue.

## Getting rid of those pesky null and zero checks

A key piece of overhead I noticed in the existing Dictionary implementation that also applied to my prototype was that we frequently had to check whether a given array was null, or whether its length was 0. After all, our container might be empty. The operations `array[index % array.Length]` and `array[array.Length - 1]` would both break for a length of 0, after all.

I realized that there's an easy solution for this in the common case: Always have an array with at least one element in it - in the prototype this is `Statics.EmptyBuckets`. Instead of checking for a null array (when it matters - which is only the Insert and Resize operations) you check to see whether you have the EmptyBuckets array. Searches and removals can proceed without any checks, because they will Find Nothing very quickly (it's empty, after all!)

This reduced code size quite a bit and had a measurable impact on performance, so I was pretty happy to arrive at this solution.

## Bucket counts and collision resistance

The F14 blog post talks about making the number of buckets a power of two, because this allows turning a hashcode into a bucket index by doing a bitwise AND instead of an integer remainder. This does have a measurable impact on performance - in my benchmarks, the bitwise AND was a lot faster. But in the end, I ended up copying what the BCL did, and made bucket counts a prime number. Why?

In practice, for certain types of bad/degenerate hashes, prime bucket counts have *much, much* better collision resistance. The difference was night and day, so I simply couldn't ignore that worst case in order to make my best case 5-10% faster. This was a good lesson for me - I had originally not written many benchmarks for hash collisions, and only once I wrote some on a whim for various scenarios I'd cooked up did I discover that the F14-inspired approach to bucket counts had such a glaring weakness.

If you know that all your hash functions are optimal, you can probably get away with using power-of-two bucket counts and enjoy the resulting performance boost, but we definitely can't do that. It could be interesting as a future research project to try and adaptively detect a bad hash and only switch to using prime bucket counts once you spot one, though! There are also things that can be done to 'improve' a bad hash function once you know you have one, like [avalanching the bits](https://en.wikipedia.org/wiki/Avalanche_effect) via a [finalization function](http://zimbry.blogspot.com/2011/09/better-bit-mixing-improving-on.html).

The prime bucket counts also appeared to reduce average memory usage a bit, but that might have been a measurement artifact, because the F14 blog post seems to argue the opposite.

## Less memory equals less CPU time

One takeaway was that in most cases, if I made the data structure use less memory, it would automatically become faster. It was fine to add a couple extra instructions or even make things a bit more complex if the trade-off was that a big dictionary used 10% less or even 25% less memory, and this held up in the benchmarks. In the past I've overlooked this part of performance optimization, likely at my own peril - CPU caches are only so big, and you only have *so much* memory bandwidth with which to fill those caches.

This is also part of why optimizing for code size is so important - the smaller your code is, the more likely it will stay in the CPU's instruction caches, allowing it to run more quickly.

## Why layout 4?

I mentioned that I liked the upsides of the layout I chose - layout #4, a single Bucket array where Buckets contain Key/Value pairs after the suffix table - and didn't mind the downsides. Let's enumerate some of them:

### Upsides of Layout 4

* We end up with a single Bucket type that contains a 16-byte suffix header, then 1-14 Key/Value pairs sequentially. We know the size of the bucket at compile time, and the location of the first pair at compile time, and the size of a pair at compile time. This means that once we've located a bucket, we can do everything we need to do without extra multiplies, divides or remainders, other than one small imul/shift to skip some pairs. This makes a big difference!
* For small key/value types, everything fits into a small handful of cache lines, which improves performance during scans. The first few key/value pairs will be in the same cache line as the suffix header, so if your dictionary's buckets aren't particularly full you get fantastic performance.
* The entire state of the container lives inside the single Buckets array - it is possible to capture that array and manipulate it without the aid of any other information. This produces a data structure with extremely good resistance to corruption from multithreaded access, and reduces the amount of memory/register pressure in code manipulating it. "Corruption from multithreaded access" is something that the existing BCL Dictionary implementation has to put a lot of effort into detecting, and Layout 4 simply doesn't have to worry about it.
* Enumeration can occur by walking through one bucket at a time and then walking through all the pairs in the bucket. Almost every operation can be implemented this way, and if you want to guard against concurrent access, you can do so by simply snapshotting the current bucket as you move through the buckets array. That's all you have to do.
* Various management tasks become simple and easy - there's exactly one array to resize when growing, to clear you just clear the whole array, etc.

### Downsides of Layout 4

* It's not possible to trivially skip clearing buckets that are already empty, since a dictionary with 4 items in it might have those items spread across 20 different buckets. You have to check each bucket to see whether anything's in it, at a minimum, and this does have a cost. In practice clearing huge dictionaries is not a high frequency operation, so I spent some time optimizing this and moved on. The BCL stores entries sequentially in a separate array, so clearing is much cheaper there because it knows which parts of that array are already empty.
* Enumeration also has overhead when mostly empty, since we have to scan through buckets that might not contain anything. In practice my benchmarks showed that this wasn't a problem, so I didn't spend time worrying about it. 😊 I'm sure that for some users, it's very important to be able to efficiently enumerate a huge dictionary that is mostly empty!
* It's extremely difficult to make this container preserve insertion order - while it can be done, it imposes a lot of overhead and complexity. As far as I knew, this wasn't a problem, so I didn't worry about it. 😓 Most hash tables aren't ordered, after all, and if you want an ordered dictionary, well, [we already have one for you](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ordereddictionary-2?view=net-9.0)...
* The minimum size of this data structure is higher than that of the other layouts because each Bucket has a fixed capacity - you can't realistically allocate half a bucket, so if your bucket size is 14 items, every Dictionary now has to allocate space for at least 14 items. This is a real concern, and I addressed it by lowering the bucket size and making sure I kept overhead down in other areas. It's possible that for specific use cases - lots and lots of 3-item dictionaries, for example - it would have been a real regression for users. The other layouts can be under-allocated since the keys and/or values live in separate arrays, so that is one reason to consider them.
