# Dynamic PGO

The following document outlines the overall approach to profile guided optimization (aka PGO) that I'd like to see us adopt in the JIT.

It is based on past experience and familiarity with profile guided optimization in several different AOT compilation toolchains, namely
* HP-UX Compilers (circa 1993-1997, known as PBO)
* MSVC's implementation of PGO (and PGO2)
* Midori's implementation of PGO in Bartok/Phoenix
* LLVM's implementation of PGO (known as FDO)

However, the approach described here differs materially from those efforts because the primary focus of this work is a jitted environment. As such, it presents unique challenges and compelling new opportunities. That being said, much of the work proposed below will also benefit AOT scenarios.

We are calling this effort "Dynamic PGO".

A good portion of what follows is background and exposition. If you're mainly interested in the proposed work, you may want to [skip to that section](#ProposedWork).

You may already be familiar with the legacy .NET approach to profile feedback, commonly called IBC (Instrumented Block Counts). If not, there is a brief [writeup on IBC](#IBC) at the end of the document.

## General Idea

Profile based optimization relies heavily on the principle that past behavior is a good predictor of future behavior. Thus observations about past program behavior can steer optimization decisions in profitable directions, so that future program execution is more efficient.

These observations may come from the recent past, perhaps even from the current execution of a program, or from the distant past. Observations can be from the same version of the program or from different versions.

Observations are most often block counts, but can cover many different aspects of behavior; some of these are sketched below.

A number of important optimizations are really only practical when profile feedback is available. Key among these is aggressive inlining, but many other speculative, time-consuming, or size-expanding optimizations fall in this category.

Profile feedback is especially crucial in JIT-based environments, where compile time is at a premium. Indeed, one can argue that the performance of modern Java and Javascript implementations hinges crucially on effective leverage of profile feedback.

Profile guided optimization benefits both JIT and AOT compilation. While this document focuses largely on the benefits to JIT compilation, but much of what follows is also applicable to AOT. The big distinction is ease of use -- in a jitted environment profile based optimization can be done automatically, and so can be offered as a platform feature without requiring any changes to applications.

.NET currently has a somewhat arm's-length approach to profile guided optimization, and does not obtain much benefit from it. Significant opportunity awaits us if we can tap into this technology.

## Overview

Broadly put, there are three main aspects to profile guided optimization:
* How to [gather profile data](#Gather)
* How to [represent and maintain profile data](#RepMain) during compilation
* How to [leverage profile data](#Opt) for optimization

The following sections explore these in detail.

## Gathering Profile Data
<a name="Gather"></a>

Profile data is most commonly thought of as execution counts for basic blocks. But other kinds of data can be collected, for instance histograms for the distribution of runtime types, indirect call targets, argument lengths to key methods, switch value histograms, and so on. Profile data can also be contextual, representing counts or distributions in specific circumstances. Profile data can try and capture traces or paths instead of simple counts.

For histogram style data one can create approximate histograms using relatively small amounts space using techniques inspired by "online sampling" of data bases. Often exact counts are not needed; what is interesting are cases where a small number of items occur with high frequency. For example, when instrumenting an indirect call site, the interesting case is where one or two targets stand out from all the rest.

Profile feedback can be approximate in other ways, for example derived via "random" sampling based on timers or hardware counter interrupts. It is important that the consumption of profile data be robust in the presence of errors and be tolerant of inconsistent information.

Key techniques for profile feedback are:
* Instrumentation
* Sampling
* Synthesis
* User Annotation
* Contextual Profiling
* Hybrid Approaches

### Instrumentation

Instrumentation describes techniques where some version of the code being measured is explicitly modified to measure events of interest.

#### The "Fixed Point"

For a given class of instrumentation, there is typically one place in the JIT -- the so-called fixed point -- where code is instrumented and/or profile data is read from an external source.

If the profile data is being used to drive optimization, the fixed point must happen before the optimizations. Since the major beneficiary of profile feedback is the inliner and the inlining phase runs quite early, the nominal place for the fixed point is just before inlining.

If the profile data is used for other purposes (say offline analysis) then instrumentation can happen most anywhere. Often it is interesting to have a very late instrumentation pass that can measure the execution frequency of the basic blocks.

#### Probes

Instrumentation sequences to measure aspects of behavior are commonly called probes.

#### Minimizing Probe Costs

There are two aspects to probe costs.

The first is a static cost proportional to the number of probes and the size of each probe. Instrumented code is larger and takes longer to produce. Thus it behooves us to use as few probes as possible and to try and produce compact instruction sequences for each probe.

The second is a dynamic cost -- as each probe is executed at runtime it slows down the overall computation.

As we'll see below there is considerable redundancy in the count data across the blocks and edges of a flow graph. So a simplistic instrumentation scheme that instruments each block collects more data than is strictly necessary. We can use a sparse instrumentation scheme.

One common approach to building a sparse scheme is to leverage flow graph structure. We first build a spanning tree on the graph (a tree starting at method entry that connects all nodes, such that adding back any of the omitted edges would turn the tree into a dag or a graph with a cycle). Then we only need put probes on the edges of the graph not in the spanning tree. This will minimize static probe costs.

If we've run profile synthesis or are instrumenting a flowgraph with profile data, we can form a maximum weight spanning tree. This will minimize dynamic probe costs.

Proper count reconstruction from sparse instrumentation requires that at the point of profile ingestion we can build an exactly matching flow graph to the one we had at instrumentation time. Thus it's not well suited for cases where we have profile data from prior versions of the program. But for in-process profiling it should work well.

One can also consider taking advantage of so-called "counted" loops to remove probes from some loop bodies. However this may require program analysis capabilities that are not available in the compiler doing the instrumentation.

We should set goals for profiling overhead, including
* additional time needed to generate Tier0 code
* additional size for Tier0 code
* additional time to execute Tier0 code
* amount of memory needed to hold profile data

#### Methods With No Control Flow

If a method has no control flow we should consider not instrumenting it at all -- more precisely, there is not much point in gathering block counts. We'll lose any semblance of [global importance](#Global) (that is, we won't know how often such methods are called) but perhaps we can get at that information some other way (e.g. the call counting data the runtime keeps to trigger tiered compilation, or some kind of low-overhead background sampling).

.NET, particularly Tier0, has lots of tiny simple methods like property getters that don't need any instrumentation.

#### Dynamic Range

Modern processors are fairly fast and a 32 bit count value can roll over or saturate in a matter of seconds for a compute intensive loop. We should seriously consider using 64 bit counters and/or using doubles for counts.

Using doubles has the nice property that count values naturally saturate and so there is no concern of roll-over. However there tend not to be compact sequences for incrementing double values (or RMW ops) so this may also increase probe cost.

If we do end up using 32 bit counts then the profile ingestion system will have to tolerate the cases where counts have rolled over. This will likely show up as large-scale inconsistencies in the ingested profile, and likely at that point the entire set of data will need to be dropped.

#### Instrumentation and Inlining

When producing instrumented code we may also want to enable other optimizations like inlining (this is especially important in AOT). The ideal situation here is for inlinees to "carry their probes" with them so that there is one set of counts per inlinee.

Currently Crossgen's IBC mode inlines but does not put any probes in inlinees; this leads to an unfortunate data loss for any inlinee with control flow.

One can consider more elaborate schemes where profile data for inlinees is collected per-caller or per-callsite. More on this below (see contextual profiling).

Similar concerns apply to any other code duplicating optimization within the compiler. But since the profile fixed point tends to be early in the phase pipeline, the probe IR sequences are added early, and so these sequences get duplicated naturally as optimizations unfold. An optimization that duplicates a block -- say for example the `for loop` to `do-while loop` transformation -- will also create a duplicate count probe. The original probe and the duplicate probe will both increment to the same counter, and so at runtime, the counter will end up with a count value that properly reflects the count for the code seen when the code was instrumented.

Whenever code is duplicated like this there is an opportunity here to instead create a distinct set of counters; this creates a version of [contextual profiling](#context) which can be useful -- for example, capturing zero trip counts of loops. More on this below too.

#### Where Profile Data is Kept

For dynamic PGO the probes need to write their data into some sort of storage pool that lives as long as the associated method.

The current prototype (TieredPGO) has the runtime allocate a fixed-sized slab for profile data. As each method is instrumented during Tier0, a chunk of this slab is reserved for that method. The JIT fills in key bits of keying and schema information, and bakes the addresses of the counter slots into the individual probes. As the instrumented method runs these slots accumulate the counts. If the slab fills up then subsequently jitted Tier0 methods simply aren't instrumented.

When a Tier1 method is jitted, the JIT asks the runtime to find the associated profile data; this currently uses the keying information to search the slab, and returns the reserved section. The JIT then reads the values from the section.

Note the Tier0 method may still be running when the Tier1 rejit happens, so it's possible for the count data to be changing as the Tier1 rejit is reading it. This may cause issues for count consistency and may also make accurate SPMI replay tricky (SPMI will record possibly a different count snapshot than the JIT sees).

The implementation for TieredPGO was intentionally kept simple (in part to allow easy import and export) and a "production ready" implementation will likely do things differently:
* allow one slab per method, or multiple slabs
* directly associate reserved part of slab with method so we don't have to search for it during Tier1
* (perhaps) reclaim slabs once Tier0 code is retired
* use a more memory efficient representation

#### Externalizing Data

If we want to externalize data for offline analysis or to feed into a subsequent run of the program, we also need to produce some keying data so that the per-method data can be matched up properly later on.

This is surprisingly challenging in .NET as there's not a strong external notion of method identity from one run to the next. We currently rely on token, hash, and il size as keys. Note things like the multi-core JIT must solve similar problems. We will leverage David Wrighton's [dotnet-pgo](https://github.com/dotnet/runtime/blob/main/src/coreclr/tools/dotnet-pgo/dotnet-pgo-experiment.md) work here.

We may also wish to externalize flow graphs to allow offline analysis or to allow some kind of fuzzy matching of old profile data in a new version of the program (this is done by MSVC for example).

### Sampling

Instead of instrumenting we can simply interrupt the running program and keep track of what code it is executing, similar to what a performance profiler would do. The appeal of sampling is that it can run on any version of the code and potentially has less overall overhead than instrumentation.

But there are a number of challenges posed by sampling which we'll touch on briefly.

The first is that sample data is sparse and so count reconstruction becomes more challenging. The second is that samples are often imprecise and implicate the wrong instructions or basic blocks (commonly called "sample skid"). The third is that sampling happens on the emitted code, and the results must be projected back to the desired profile fixed point, which is commonly early in the phase pipeline (this is especially challenging if we're sampling optimized code). The fourth is that samples can produce isolated profile islands (eg in a method with a hot loop, we may only see samples in the loop) and we might have no data on how control flow reached or left the loop.

### Sampling-Instrumentation Hybrids

Another technique for reducing dynamic instrumentation cost is to run the instrumented version of the code only every so often. That is, we prepare two ve versions of each method, one with probes and one without, and run the version with probes every so often, or randomly.

[Arnold and Grove](#AG05) describe a technique like this used in the `Jikes RVM`, where the system leverages patchpoints to allow occasional transitions to instrumented versions of methods; we could consider something similar.

### Synthesis

Profile synthesis is the term we use for fabricating profile data out of thin air. While it might not seem prudent to make up data, program structure and programmer practice create learnable patterns of behavior that in turn can be used to produce plausible profiles (note: this is one area where we might be able to leverage machine learning / big data).

Indeed many program optimizations already leverage program structure to guide their decision making, e.g. boosting the importance of code within loops, or reducing the importance of code that leads to exceptions. Profile synthesis, in effect, simply recasts this idea of leverage in the guise of profile data, so that these sorts of importance estimations are made uniformly and consistently across the compiler.

And once we've done this all JIT optimizations can leverage profile data in a consistent and uniform manner, as profile data is always present, and always incorporates "the best information available."

The classic paper on profile synthesis is [Ball-Larus](#BL93), which presents a set of local or near-local heuristics to predict block successor probabilities. These can be turned into profile counts using an algorithm like [Wu-Larus](#WL94) or by simply solving the associated system of linear equations. Other researchers have extended this work in various dimensions but the basic principle remains: use aspects of the program structure, operations, and data to predict execution flow.

Profile synthesis is interesting even if instrumented or sampled profile data is available.

First, even in a jitted environment, the JIT may be asked to generate code for methods that have not yet executed, or that have for various reasons bypassed the stage of the system that runs instrumented versions. Profile synthesis can step in and provide provisional profile data.

Second, even if profile feedback is available, it may not be as representative as one might like. In particular parts of methods may not have been executed, or may have distorted profiles. Given the linearity of profile data, we can blend together the actual observations with a synthetic profile to produce a hybrid profile, so that our optimizations do not over-react to the actual profile (think of this as a kind of insurance policy: we want to make sure that if program behavior changes and we suddenly veer into un-profiled or thinly profiled areas, we don't fall off some kind of performance cliff). More on this below.

### User Annotations

There have been various proposals to allow users to convey profile data via annotations or more likely via a special set of intrinsic methods:
```
// indicates b is likely true with probability p
[Intrinsic]
bool Likely(bool b, double p = 0.9) => b

// indicates b is likely false with probability p
[Intrinsic]
bool Unlikely(bool b, double p = 0.9) => b
```
Typical usage would be to control block layout or provide hints for the inliner
```
bool b = ...;

while (Likely(b, 0.99))
{
    // expect this loop will iterate ~ 100 times
}

if (Unlikely(b))
{
    // this code is fairly cold
}
```
We can consider others, e.g. for indicating likely types. Note if the probability is not a JIT-time constant the construct is ignored.

GCC / LLVM have similar constructs.

If used in a control flow context, these would feed values into profile synthesis, or additional `{eij}` into blended/hedged profile modes, if we also have actual profile feedback.

### Contextual Profiling
<a name="context"></a>

As noted above we sometimes may want to profile dependent aspects of program behavior. Assuming B and C call A, we could for instance keep separate counts for A called by B and A called by C. Since we expect callee behavior to be (to some extent) caller dependent, this potentially gives us a more precise picture.

More simply, we may want to track context-dependent behavior of a block we know we are likely to duplicate based on flow. The common example here is the loop exit test for a `for loop` or `while loop`; frontend compilers will typically just include one such copy of this test block, but we commonly will duplicate the test block to create guarded `do-while` loops so we know loop bodies will execute at least one iteration (so we can do loop invariant code hoisting, etc). The key here is that we have a "critical block" in the flow graph, one with two incoming and two outgoing edges. For critical blocks a custom instrumentation scheme can track the outgoing edge probability based on which incoming edge was taken.

Another example in this category is profiling to detect inherently unpredictable branches (to enable profitable use of `cmov` and similar instructions). A flat profile can identify potentially unpredictable branches; these are blocks with two successors where the successor probabilities are close to .50/.50. But that's not sufficient; the branch behavior may still be predictable by hardware. So a more sophisticated probing scheme is needed, one that keeps track of the recent history of the branch.

The ingestion and maintenance of contextual profiling data is more challenging than for regular profiling. And contextual profiling can greatly increase the size of the collected profile data. In my experience, this extra complexity and cost makes contextual profiling less appealing and not a priority for the work proposed here.

### Histogram Style Probes

Lastly we consider probes that try and describe the distribution of values. For example the likely values of a code pointer at an indirect call, or of a class type at a class test, or of argument values to a callee or the trip count of a loop.

A scheme I've used successfully in the past is to use randomized sampling to construct an approximate histogram for the distribution of values.

For discrete things like types, the histogram aims to determine the frequency of occurrence of the most likely types. For continuous things like lengths we may be more interested in relative magnitudes rather than absolute values, so we
may map the values to a discrete set first (say via approximate log2 or similar) and histogram that.

These histogram probes must strike a balance between accuracy on the one hand and runtime size and cost on the other. Typically we don't need a lot of accuracy, but even a minimalist histogram probe will need enough space to hold some number of values and associated counts, so each histogram probe will occupy the space of some number of count probes.

Ideally we'd be smart enough when instrumenting to not create redundant histogram probes. If we have say two back to back virtual calls:
```C#
   x->V();
   x->V();
```
one probe would be sufficient to describe the possible types for `x`. But our current plans call for instrumenting at Tier0, and currently Tier0 has no dataflow analysis capabilities. We'll have to experiment to see whether or not we need to adjust our strategy here.

### Other Considerations

#### Global Importance
<a name="Global"></a>

TieredPGO doesn't give us profile data for all methods. And what data it does give us may not be sufficient to really optimize effectively.

Ideally the JIT would focus optimization efforts on the methods that use the most CPU, or are otherwise performance critical -- that is the methods that are globally most important. We get hints of this from the fact that a method gets promoted to Tier1, but it's not a direct or strong a signal.

If we do not have a good measure of global importance then we don't know which methods we should optimize more aggressively, so we have to be a bit more conservative than we'd like.

If we confidently could identify the top N% of methods (say 5%) then one could imagine optimizing those more aggressively than others, with a larger code size and JIT time budget than normal.

#### Dynamic PGO and R2R

R2R methods bypass Tier0 and so don't get instrumentation in the current TieredPGO prototype. We probably don't want to instrument the code in the R2R image. And many of these R2R methods are key framework methods that are important for performance. So we need to find a way to get data for these methods.

There are a few basic ideas:
* Leverage IBC. If there is IBC data in the R2R image then we can make that data available to the JIT. It may not be as relevant as in-process collected data, but it's quite likely better than synthetic data or no data.
* Sampled instrumentation for R2R methods. Produce an instrumented version and run it every so often before the method gets promoted to Tier1. This may be costly, especially if we have to use unoptimized methods for instrumentation, as we'll do quite a bit of extra jitting.
* Make R2R methods go through Tier0 on their way to Tier1. Likely introduces an unacceptable perf hit.

#### Dynamic PGO, QuickJitForLoops, OSR

Some other methods also bypass Tier0 and so won't get profiled. Notably, in our current system, any method with a loop.

We can address this by having loop methods run at Tier0; doing so safely also requires that we enable OSR so we can escape from long-running instance of these methods.

OSR methods are jitted at Tier1 and so will pick up profile from their Tier0 counterparts. However OSR methods currently represent the "full continuation" of a method at a patchpoint, so if we have a method with multiple loops we may transition to the OSR version while we're still in the first loop and so never collect profile data for the other loops.

A potential fix here is to have OSR methods only encompass a loop body, and rejoin the original Tier0 method upon exit from the loop. This approach is used in JavaScript engines like Chakra.

#### Tier0 as a Data Provider

It's possible we may not get good quality data out of the current scheme where we instrument Tier0 code. The number of times a Tier0 method runs before the Tier1 rejit happens is unpredictable, but it may be as few as 30. In the bring-up phase we can increase the call count threshold and force methods to remain in Tier0 longer, but we may need to find other solutions where we can get both good startup and good profile data.

### Gathering Profile Data: Proposal for Initial Work

For DynamicPGO, initially we'll rely on Tier0 for instrumentation, and use that to build up the capabilities of the JIT. We'll do both block instrumentation and some form of type profiling for virtual and interface calls.

As the abilities of the JIT mature we will likely come back to ensuring that all the important code ends up passing through a profile gathering stage, and that the profiling process is efficient.

## Handling of Profile Data in the JIT
<a name="RepMain"></a>

Once profile data has been collected, we need to incorporate, represent, and maintain the data during compilation.

### Background and General Principles

Block and edge count data is typically stored directly on the flow graph nodes and edges respectively. There are a number of design choices here but typically we prefer the "normalized" representation where counts are expressed relative to the method entry count.

Other kinds of profile data are typically associated with the related IR construct -- e.g. an indirect call profile would be attached in some fashion to the indirect call node.

Optimizations that are profile sensitive then will query these values as needed.

As the flow graph and IR are transformed during compilation, we must take care to properly update profile data so that later phases still see reasonable values.

One might object to the idea that we intentionally modify the profile data as we are compiling, but it is a necessary thing if relative counts from disparate parts of the method are to retain any sort of significance.

One way of thinking about profile maintenance is that these modifications typically arise when we discover some fact about the program that could have been accurately described by the right sort of contextual profile. But we usually won't have that profile. So if all we have as "flat" profile we have to project what happens to profile data as we modify the program.

For example, suppose we have A called by B and C. When we inline A into B we only have one set of counts for the blocks in A. So if there's a difference in how A behaves when called by B and C it might only become evident later as we optimize B+A. Initially we won't know if there is such behavior, and we have to produce some kind of profile for A, so we simply use appropriately scaled counts. And because this isn't always going to be correct, later on we may need to make some repairs.

#### Kirchhoff's Laws

In general we expect that the flow graph the JIT creates to be a comprehensive picture of actual control flow. In practice we often ignore certain details; e.g. exactly where in the flow exceptions might occur. But modulo these sorts of details, we can expect that execution flow must follow the paths described by the flow graph, and hence flow is "conserved" and can't randomly start or end at one place or another. Because of this, one can generally expect that the profile counts flowing into a basic block should match the profile counts flowing out of the block.

This picture of profile flow is entirely analogous to the conservation of current in electrical circuits or other flow problems, and the same principles apply. Abstractly one can create a system of linear equations whose structure is induced by the flow graph that relates the counts in the various blocks and edges. Profile data also flows into the graph from entry points (method entries) and out of the graph at exit points (method returns); these establish the boundary conditions.

For example consider a flow graph with 4 nodes and 4 edges arranged in a simple if-then-else-join diamond. Call the nodes `Xi` edges `Eij`, and where this does not lend to confusion, use the same symbols for the execution counts for these nodes and edges. Then the equations are:
```
Node X0 (entry, f):     X0 = E01 + E02 = IN
Node X1 (then):         X1 = E01 = E13
Node X2 (else):         X2 = E02 = E23
Node X3 (join, exit):   X3 = E13 + E23 = OUT (= IN)
```
Note there are really only two independent values here, e.g. `IN` and `X1`; once we know those all other values are determined. Hence only two instrumentation probes are needed to fully determine the counts for every block and edge in this method. We'll explore the implications of this later.

Kirchhoff's Laws take on the following general form (here focusing on flow into of blocks):
```
Xi = INi + Sum{j}(Eji)
```
That is, the counts flowing into block `i` are the sum of the external counts into `i` plus counts flowing into `i` from its predecessors. A similar equation holds for flows out of a block:
```
Xi = OUTi = Sum{j}(Eij)
```

#### Linearity

A key property of the block and edge equations is that they are linear. This means if we have two consistent sets of counts (`IN`, `OUT`, `Bi`, `Eij`) and (`IN'`, `OUT'`, `Bi'`, `Eij'`) then any linear combination of these counts is also consistent. Typically we think of this as a weighed sum where the weights w determine the relative importance of the two sets of data:

```
IN''  = w * IN  + w' * IN'
Bi''  = w * Bi  + w' + IN'
Eij'' = w * Eij + w' * Eij'
```

Linearity comes in handy when we want to consider blending two or more sources of profile data, for example if we want to use synthetic profile data to hedge against over-reliance on sample or instrumented data. See below.

#### Normalization

If we divide all the counts by `IN` we end up with a "normalized" set of counts. Normalized counts have the nice property that the count value determines how often that part of the graph is traversed (on average) when the method is called. That is, a normalized value of 3 on a block with a call means that callee is called (from this point) 3 times as often as the root method. For most JIT optimizations the absolute value of counts is not as interesting as the normalized values.

Normalized values greater than 1 are only possible in methods with loops.

#### Branch Probability View

An alternative representation to having node and edge counts is to have node counts and outgoing edge probabilities (`eij`). This is a kind of fine-scale normalization where edge counts are now implicit: `Eij = Ni * eij`.

The benefit of this representation is that these edge probabilities tend to be somewhat stable as we modify the flow graph in various JIT phases.

Also note there is redundancy here as `SUM(j){eij} == 1`, that is, the sum outgoing probabilities is 1 -- flow must leave this block somehow. Since most blocks have exactly 1 or 2 successors, we generally only need to record a single value for the probability.

We will sometimes encounter degenerate cases where the only possible exit from a loop is via an exception or other runtime event that we don't model explicitly in the flow graph. If we're not careful these can lead to various profile computations diverging off to infinity. The usual remedy is to add an artificial exit edge of some sort with very low probability to limit the "amplification" this kind of loop can provide.

In this view the input flow equations become
```
Xi = INi + Sum{j}(eji * Xj)
```
and the output flow equations
```
Xi = OUT + Sum{j}(eij * Xj)
```

### Incorporation of Data

To incorporate profile data from an external source we need some kind of keying scheme to pair up an artifact from the current compilation with the corresponding information in the profile data source.

For the JIT we use some aspect method of identity (say token + hash + ilSize) as the primary key to locate the information relevant to this method, and then some aspect of code identity (say ilOffset, for block counts) to locate the particular item.

If the external data is from an older/different version of the application then some kind of fuzzy matching may be appropriate.

External data may or may not be self-consistent, so one of the initial steps is to establish consistency.

### Count Reconstruction from Probabilities

It is possible to reconstruct node (and hence edge) counts from just the edge probabilities eij and the external counts. Given that edge probabilities are a somewhat durable "truth" this gives us an avenue to repairing profile data that has become inconsistent during optimization, and/or a methodology for updating profile data when we make flow graph edits, a way to reconstruct a full profile from locally deduced likelihoods in profile synthesis.

For flow graphs without loops the counts can quickly be derived in one pass. For flow graphs with "reducible" loops we can use the classic [Wu-Larus](#WL94) algorithm. For more complex flow we can either try to solve the general linear system (via say Gauss-Seidel iteration) or just approximate. Note that the linear system is likely quite sparse and there is no need to form the full coefficient matrix `{eji}` explicitly, so using a general iterative solver may not be as expensive as one might imagine.

### Initial consistency

After the profile data is ingested we need to check consistency and possibly adjust counts. Once counts are consistent we can then derive the `{eij}` which we then rely on as the "source of truth" for the remainder of compilation.

If we have dense counts then we can simply use the local `{eij}` derived from those counts. This may not be globally consistent, e.g. the system of equations
```
eij * Bi = I
```
where `I` is the input count vector may not hold (note if we've normalized and the entry block is `Bk` then `I` is a vector with `Ik=1` and all other entries zero).

To solve this we simply find an `B'i` such that the above holds -- this is a straightforward linear system solution. Then we adjust the block counts as needed. Note we need to ensure that the solution `B'i` is strictly non-negative; it turns out that for a well-formed `eij` (exit probabilities between 0 and 1, and sum to 1 for each node) this is the case.

If we have a mixture of profiled and synthesized nodes the above works equally well as synthesis establishes the `{eij}` for the unprofiled nodes.

If we have sparse data (say from a spanning tree) then we may initially need to reconstruct some block counts; this again may produce inconsistent data. Once we've done that we can run the above.

### Reconstruction

As we optimize we may find the global counts again become inconsistent. Assuming we have trustworthy `{eij}` we can reconstruct globally consistent counts at any time.

### Edges and Nodes

As noted above there is some redundancy in a system that represents counts on both nodes and edges, and any time there is this kind of redundancy there is  an increased chance of inconsistency and an extra maintenance burden.

If the primary representation for profile data is block counts and successor probabilities then local maintenance is simple and does not require much data.

One challenge we'd face here is that the current JIT flowgraph is predecessor oriented -- that is, from a node, one can easily enumerate the predecessor flow edges, but not so easily the successor edges. Updating it to be successor oriented may not be practical, and representing edge counts as block counts and predecessor probabilities is not semantically as meaningful.

### Min and Max Edge Counts

The current JIT actually keeps two sets of counts for edges, min and max. The motivation for this is unclear but it immediately poses a dilemma for any consumer: which count to use? I do not see this as adding value and suggest we simply rely on a single value (which would be the node count * successor probability).

### Dynamic Range and Fractional Counts

Profile counts can span a wide dynamic range, and we often will want to divide and scale them. Using an integer representation is challenging because of the limited dynamic range and the inability to represent fractions.

The JIT has traditionally used a fixed-point representation where the count is scaled upwards by 100, so it can represent some fractions. However I propose that we simply use floats or doubles. This greatly simplifies a lot of the math.

### Hedging (blending profiles)

As noted above, even if we have profile data available, we may want to also run synthesis, both to fill in any gaps in profiling, and to provide "downside" insurance in case the profile data is not as representative as we'd like.

The idea is to basically always run synthesis first, producing local probabilities `{eij}`. We then incorporate the actual profile data and update the `{eij}` to reflect primarily the profiled values. Where we have both a profiled and a synthesized probability, and they disagree, we blend the values using [Dempster-Shafer Theory](https://en.wikipedia.org/wiki/Dempster%E2%80%93Shafer_theory) or some other approach to combining predictions, strongly favoring the profile data.

We then induce counts from the blended probabilities.

To make this work, we need to ensure that synthetic profiles do not make too many harsh predictions; that is there should be relatively few cases where the synthetic profile predicts an edge is taken with zero probability, so that even in ostensibly cold areas of code we can reason about the relative importance of things. Note because we're using floating math we can have very small probabilities.

### Ongoing Maintenance

Whenever there is a change to the flow graph we must consider how best to update the profile information. Ideally we'd keep it consistent and correct at all times, but that is not possible or practical.

The general principle here, following the approach outlined by [Wu](#W02), is to consider the local probabilities `{eij}` as the fundamental sources of best information. Everything else can be derived from those. So when we transform the flow graph at a minimum we should try and make sure the `{eij}` are locally consistent -- and local consistency is easy to achieve as we just need to make sure the exit probabilities of each block sum to one.

Global consistency is harder and more costly, and we may be willing to allow the data to become globally inconsistent, and only try and re-establish global consistency when we know a phase will rely on it in some critical way.  Past experience shows the key phases that rely on profile data are the inliner, block layout, and allocator; the first happens early in the phase pipeline and the others towards the end, so perhaps just one profile repair is needed, right before those latter phases.

For some sorts of changes the updates are simple and local and preserve global consistency. e.g. inverting a branch condition simply swaps the probabilities of the associated edges; since the targets of those edges also get swapped, everything remains consistent.

But other cases are not quite so simple:
* conversion of computation to flow
* optimizations that remove edges (or bypass creating edges)
* optimizations that duplicate blocks
* inlining
* tail recursion to loop optimization

We touch on each of these in turn.

#### Conversion of computation to flow

One example of this in the JIT is QMARK expansion. Here flow is introduced where there was a statement before. Since the overall computation is unchanged and the QMARK expands into a conditional execution tree, it is straight forward to reconstruct the counts if we know the relative probabilities. So the expansion needs simply set the `eij` for each newly introduced block, and then propagate the counts downwards.

We can sometimes collapse control flow into computation, e.g. introducing conditionally executed code like CMOV. Typically a prerequisite for such transformations is the identification of a suitable "hammock" in the control flow graph, and the optimization shrinks the hammock to a single node whose profile count is the hammock entry or exit count.

#### Removing Edges

When an edge is removed it is generally expensive to regain global count consistency, as counts may need to be adjusted all over the graph.

It might be surprising at first that edges with nonzero profile counts can be removed, or that blocks with nonzero counts can become dead. But recall that over time the profile data in the flow graph can diverge away from "true" behavior because we lack contextual information. And because of this, we may find ourselves removing blocks and edges with profile counts.

#### Duplicating Blocks

Whenever we duplicate flow we have to make some sort of determination about the profile counts in both the original and the duplicate regions.

Sometimes this determination is simple; for example if we clone a finally we can generally presume the inlined version gets all the counts, and the remaining "fault" version has none (or some very minimally scaled down set of counts).

When we duplicate a loop exit condition to convert loops to if-do-while, we need to determine how often the loop will "zero trip". Typically I've just preserved the probabilities here, so say if the loop originally iterated 8 times (so the exit test ran 9 times, and back-edge probability is 8/9) then we assume the loop is entered with that same probability. But this may not be at all accurate. It could be that the loop trip count varies widely at runtime.

Similar considerations apply to other loop optimizations like unrolling.

#### Inlining

When inlining the common practice is to "normalize" the inlinee and then multiply each count by the call site count (or normalized call site count). This presumes that the behavior of the callee is not correlated with the call site.

It turns out that callsite-callee correlations are common and one might consider techniques like contextual profiling where we capture callsite (or caller) specific behavior; however these impose extra costs during instrumentation and (in my opinion) haven't yet proven to be worth the extra trouble. Callsite specific specialization often happens organically during inlining direct substitution and/or subsequent optimization.

#### Tail Recursion to Loop Optimization

In this optimization, a tail-recursive call is replaced by a jump back to the method entry block. From a flow perspective this introduces a loop into a method where there wasn't one before, and one can reasonably expect that this in turn will increase the count of the blocks in the loop. But this increase might also seem a bit odd, especially in a world where we've normalized counts; how do we justify these increases?

The resolution of this quandary is to realize that this optimization also reduces the call count of the method, and so also reduces the normalizing factor, so the extra counts added to the loop blocks are really just a "renormalization" of sorts.

For example, suppose we have a method that is called 100 times and has a tail-recursive site that is hit 80 times. In the "before" picture the call site block has a normalized count of 0.8; if the "after" picture the proper normalized count is now 4 (80 / (100 - 80)).

Two further points are worth mentioning.

First, the normalized count of any recursive call site in any viable method must be less than 1; otherwise the method suffers from runaway recursion. Likewise, the total normalized sum of all the recursive call sites must be less than one. We can try and leverage this during profile synthesis; the more recursive sites a method has, the less likely each site is to be hit at runtime. Note these recursive sites may only become evident after inlining.

This impacts tail recursion to loop optimizations because if we don't properly respect the recursive probability constraint, there's a chance that after converting the tail recursive sites to loop edges, profile re-computation for the method may diverge. So code that recomputes profiles from local block probabilities needs to guard against this case, and potentially must arbitrarily change some of those probabilities to recover convergence.

Second, if we intend to assess or prioritize inlines based on profile feedback, we might want to anticipate this profile boosting effect. If a method A is tail recursive and also calls B along the path from entry to the tail recursive site, the absolute number of calls to B isn't altered when we convert A's tail call into a loop, but the relative number of calls to B per call to A increases, making B look like a more attractive inline in a relative sense. Since this transformation happens well after inlining and may not always be possible, we might need to develop an predictor that anticipates this optimization.

### Handling Profile Data: Proposal for Initial Work

The initial goal will be to ensure that the inliner and indirect call transformations see profile data. From there we'll broaden out to work on simplifying how data is represented in the JIT and to ensuring that downstream phases see reasonable data, perhaps working on count reconstruction.

As we gain experience with Dynamic PGO we'll consider adding synthesis and hedging strategies to ensure that even unprofiled code gets reasonable performance.

## Use of Profile Data in the JIT
<a name="Opt"></a>

Experience has shown that while profile data can improve many optimizations, the main benefit accrues from inlining (and things that enable inlining) and code layout. Those will be our initial areas of focus.

### Guarded Devirtualization

In [dotnet/coreclr#21270](https://github.com/dotnet/coreclr/pull/21270) we introduced the Guarded Devirtualization optimization, where the JIT introduces an explicit class test upstream of a virtual or interface call, and then devirtualizes (and inlines) the call if the test succeeds.

This is not enabled by default; two key ingredients are missing:
* what class to guess for
* when to guess

Dynamic PGO can address both of these.

Using customized probes to collect class histograms (we may also rely on runtime mechanisms here, e.g. the state of the VSD cache, or by some state tracking in the runtime itself), we can look for calls that have a single or preferred target, and guess for those.

Using block profiles we can transform only highly profiled call sites, so that we do not cause too much code expansion. (Note code expansion can be further mitigated by partial compilation, but that most likely won't be a part of our toolbox, initially.)

### Inlining

The JIT's current inline heuristics already take profile data into account, but the modelling is simplistic. We envision creating a new profile-based policy that relies much more heavily on profile data to guide inlining. We may try and leverage machine learning to evolve such a policy.

### Block Layout and Method Hot/Cold Splitting

Block layout can have a significant impact on code execution time, and profile data provides strong clues on how to produce an effective layout.

It also provides a built-in scoring mechanism (figure of merit) for a given layout that allows us to implement hill climbing or other randomized optimization approaches.

In Midori we developed an algorithm based on an approach proposed by [Young et al](#Y93) that models block layout as a variant of a travelling salesperson problem (TSP) and then uses well-known iterative technique (3-OPT) to search for a better layout. It proved quite effective and I propose that we port it over to the JIT.

One complication here is that it does not take code alignment demands into account; adding that in may be a challenge.

Profile data also naturally leads to identification of hot and cold parts of methods. Performance gains can be had if the method is then split into two parts that are physically separated, aka hot/cold splitting. We already have JIT and runtime support for this in places (though mainly for prejitted code), so we propose to extend this to also cover jitted code.

These gains mainly come because splitting allows the runtime to pack the hot parts of the application code into smaller numbers of cache lines and pages, reducing ICache and ITLB miss rates. For applications with large amounts of code these sorts of misses can often severely impact performance.

### Other Optimizations

Many other current and possible future heuristic-driven optimizations in the JIT can leverage profile data. We list some of them here:
* basic block cloning
* loop rotation (for loop -> do while loop)
* loop cloning
* finally cloning
* loop unrolling
* loop unswitching
* common subexpression elimination
* register allocation
* if conversion (use of `cmov` and other conditional execution instructions) and other conversion of branching code to branch-free forms
* partial inlining
* shrink wrapping
* struct promotion
* ordering of locals on the stack frame

The hope is that all current (and any future) optimizations are set up in such a way that "better" profile input yields better generated code. Unfortunately this is not always the case, and it's likely we'll have to spend some time sprucing up the heuristics for some of the above.

### Other Phases

The main work in other phases is to ensure that they maintain profile data appropriately, and that their heuristics make use of profile data (where it makes sense).

Optimizations that involve tradeoffs need profitability heuristics; ideally these incorporate profile data as part of their reasoning. One of the initial tasks is to audit the various heuristics to see how much (or little) work is needed to ensure they make best use of profile information.

### Using Profile Data: Proposal for Initial Work

The initial focus will be on improving inline heuristics, and subsequently, to enabling guarded devirtualization.

## Proposed Work
<a name="ProposedWork"></a>

The aspirational goal for Dynamic PGO is to have most of the key problems addressed during the .NET 6 development cycle, so we can enable it as a new mode of .NET operation.

The initial focus of the work will be on the representation and use of data by the JIT. Once we are relatively happy with this (as demonstrated by performance improvements in key benchmarks and applications) we will work on ensuring that we have profile data for all methods.

The key initial target for unlocking performance gains is the inliner. It would also be nice to enable guarded devirtualization; this will require some form of class type profiling.

### Details of the Plan
---
#### Group 1: Make Profile Data Available

1. Make DynamicPGO data available for inlinees (done)
2. Implement profile count checker to look for inconsistencies (done)
3. Fix significant profile maintenance issues
4. Change profile data in JIT to be floating point
5. Change most of JIT to only ever deal with normalized counts
6. Remove min/max edge count in favor of just one count
7. Read profiled data for R2R methods (runtime dependence)
8. Support user annotations
9. Remove explicit edge count in favor of successor probability
10. Implement profile reconstruction from successor probabilities
11. Implement custom probe for interface/virtual calls, or moral equivalent
12. Move creation of "full" flow graph so it happens just after the importer
13. Implement profile synthesis
14. Implement blended / hedged profiles

We likely need to get through at least item 7 to meet our aspirational goals.

-----
#### Group 2: Leverage Profile Data

1. Implement new inlining policy
2. Craft heuristics for the new policy
3. Enable guarded devirtualization
4. Enable hot/cold splitting (runtime dependence) for jitted code
5. Implement profile-based block layout (based on "TSP" algorithm above)

We likely need to get through at least item 3 to meet our aspirational goals.

-----
#### Group 3: Profile Handling by Runtime and JIT

1. Update runtime to allow profile storage to grow over time
2. Remove IL offsets from profile storage for dynamic PGO (?) This may be tricky because the Tier1 importer does not produce exactly the same flow graph as Tier0.
3. If count tearing is an issue, implement copy scheme (?)
4. Implement downstream count reconstruction (pre RA/Block Layout) if needed
5. Fix heuristics in other optimization phases as needed

We likely need at least item 1 here to meet our aspirational goals.

-----
#### Group 4: Efficient Instrumentation

1. Don't instrument methods with no control flow
2. Have the JIT produce compact instrumentation sequences
3. Design and implement efficient probe for histogramming
4. Implement spanning tree approach for flow counts
5. Investigate feasibility of using float/double as counter type

We likely need to get through at least item 3 to meet our aspirational goals.

-----
#### Group 5: Testing and Infrastructure

1. Enable regular testing of Dynamic PGO (partially done)
2. SPMI collection of profile-based runs
3. Export and check in side-loaded PGO data so we can leverage PMI based diffs
4. Regular runs of Dynamic PGO in the perf lab
5. Regular runs of Dynamic PGO by key 1st party apps
6. Ensure BenchmarkDotNet gives accurate measurements of Dynamic PGO perf

We likely need to get through all these items to meet our aspirational goals.

-----

### Risks to the Plan

We may find that the benefit of Dynamic PGO is hinges upon an effective solution for R2R compiled methods. Ideally we'll have IBC data for key methods in the framework, which should help.

Likewise we may find that we need to enable QuickJitForLoops, and that may entail a fully supported (and perhaps modified) OSR.

We may find that after aggressively inlining that downstream phases require modifications and upgrades, so that we can't exhibit the full performance we hope to see.

We may need some  better measure of [global importance](#Global) effectively manage code size growth and JIT time. Many methods in a long running program will eventually JIT at Tier1; not all of them are important.

We may find that Tier0 instrumentation doesn't give us good quality profile data, because Tier0 methods run for unpredictable amounts of time.

We may find that Tier0 methods don't give us representative sample data, because methods exhibit phased behavior.

### Runtime Asks

* Make IBC counts available to the JIT ([dotnet/runtime#13672](https://github.com/dotnet/runtime/issues/13672))
* Support hot/cold splitting for jitted code and possibly for crossgenned code
* Partner with us in evolving the runtime side of Dynamic PGO (exact work TBD)

### Performance Team Asks

* Ensure BenchmarkDotNet properly measures Dynamic PGO performance
* Enable benchmark runs with Dynamic PGO and comparisons vs default runtime perf

## Appendix: IBC
<a name="IBC"></a>

NET's legacy approach to PGO is called IBC (Instrumented Block Counts). It is available for prejitted code only.

A special instrumentation mode in crossgen/ngen instructs the JIT to add code to collect per-block counts and (for ngen) also instructs the runtime to track which ngen data structures are accessed at runtime, on a per-assembly basis. This information is externalized from the runtime and can be inspected and manipulated by IBCMerge. It is eventually packaged up and added back to the corresponding assembly, where it can be accessed by a subsequent "optimizing" run of crossgen/ngen. This run makes the per-block data available to the JIT and (for ngen) uses the internal runtime counters to rearrange the runtime data structures so they can be accessed more efficiently. Prejitting can also use the data to determine which methods should be prejitted (so called partial ngen).

The JIT (when prejitting) uses the IBC data much as we propose to use PGO data to optimize the generated code. It also splits methods into hot and cold portions. The runtime leverages IBC data to

The main aspiration of IBC is to improve startup, by ensuring that the important parts of prejitted code and data are compact and can be fetched efficiently from disk.

IBC count data is not currently available to the JIT as the IBC payload is not copied to the optimized assembly.

IBC is available in .NET Core, but is only usable by first-party assemblies; the IBCMerge tool is not available externally.

With the advent of crossgen/R2R much of the startup benefit of IBC has been lost, as the data structures that were organized by ngen now simply get recreated. Also crossgen/R2R does not yet support hot/cold splitting of methods.

## References

<a name="AG05"></a>Arnold, M., and Grove, D. Collecting and exploiting high-accuracy call graph profiles in virtual machines. CGO 2005.

<a name="BL93"></a>Ball, T. and Larus, J. Branch Prediction for Free, PLDI 93,  300-313.

<a name="C95"></a>Calder, B., Grunwald, D., Lindsay, D., Martin, J., Mozer, M., & Zorn, B. Corpus-based Static Branch Prediction. PLDI 95, 79-92.

<a name="WL94"></a>Wu, Y., and Larus, J. Static Branch Frequency and Program Profile Analysis. MICRO 27 (1994). San Jose, CA: ACM.

<a name="W02"></a>Wu, Y. Accuracy of Profile Maintenance in Optimizing Compilers. INTERACT 2002.

<a name="Y93"></a>Young, C., Johnson, D., Karger, D., and Smith. M. Near-optimal Intraprocedural Branch Alignment. PLDI 93.
