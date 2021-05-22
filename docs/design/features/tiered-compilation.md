Tiered Compilation
==================

Author: Noah Falk ([@noahfalk](https://github.com/noahfalk)) - 2018

Introduction
============

Tiered Compilation allows the .NET runtime to substitute different assembly code method implementations for the same method during the lifetime of an application to achieve higher performance. It currently does this in two ways:

  **Startup** - Whenever code needs to be jitted, the runtime first generates a low quality code body, then replaces it with a higher code quality version later if the method appears hot. The lower quality initial codegen saves JIT time and this savings typically dominates the additional cost to run the lower quality code for a short time.

  **Steady-State** - If code loaded from ReadyToRun images appears hot, the runtime replaces it with jitted code which is typically higher quality. At runtime the JIT is able to observe the exact dependencies that are loaded as well as CPU instruction support which allows it to generate superior code. In the future it may also utilize profile guided feedback but it does not currently do so.



Dependencies
============

## Tiered Compilation Depends On ##

### The CodeVersionManager ###
The CodeVersionManager assists in creating new method implementations for a given .NET method,  configuring those versions and (indirectly) producing their code, keeping track of which ones exist, and switching which implementation is currently active.

There is a lot of useful background information in the [CodeVersionManager spec](./code-versioning.md) to understand general concepts around versioning of methods.

### The Prestub ###
The Prestub provides an initial version agnostic implementation of any method before code is available. After code is available Tiered Compilation continues to use it to count calls to a method before dispatching to the currently active code.

### The Threadpool ###
The threadpool provides the threads that are used to background compile better versions of a method's code. It also provides timer callbacks used for some aspects of tiered compilation policy.

## Components that depend on Tiered Compilation ##

### Interpreter ###
The IL interpreter (disabled by default with both build defines and runtime switches) now uses tiered compilation to asynchronously promote from interpreted to jitted code.

Design
======

## Historical context ##

Tiered Compilation was prototyped in 2016, introduced into the runtime code in 2017, and offered as an opt-in Preview feature in .NET Core 2.1 RTM in 2018. This design doc was written after the fact. We had been trying to mitigate runtime startup and performance problems for nearly 20 years with various forms of pre-compilation (NGEN, ReadyToRun, MulticoreJit) but this was the first serious foray into using compilation tiers to achieve similar goals. The IL interpreter appears similar but as best I understand it was not primarily targeted at performance, but rather at portability into environments that did not allow jitting. Although the idea of tiered compilation had come up repeatedly in the past it had never gained the degree of consensus/acceptance/momentum necessary to move forward relative to other performance investments.

## Goals ##

1. Improve the steady state and startup performance of typical .NET Core workloads while minimizing regressions.
2. Compliment existing precompilation techniques so that developers can leverage the best of both options.


## Concepts ##

Tiered compilation first divides all code into two buckets. Code that is eligible for tiering and code that is not. Code that is not eligible works as all code did prior to the tiered compilation feature. Code that is eligible can have two different variations called tiers:

 - Tier0 - This is whatever code can be made available most quickly to first run a method. For methods that are precompiled in ReadyToRun images, the precompiled code is the tier0 version. For methods that are not precompiled, the JIT generates code using minimal optimizations.
 - Tier1 - This is whatever code the runtime thinks will run faster than Tier0. Currently it is equivalent to code that would be jitted for a method when tiered compilation is not in use.

When a method is first invoked (or whenever something requires assembly code for a method to exist) the Tier0 version is produced first. Once it appears that the method is hot then a Tier1 version of the same method is produced and made active.

Most of the mechanics to make new code versions, configure them, and switch the active one are handled by the CodeVersionManager. Tiered Compilation owns the policy to decide when to switch versions, the counters and timers that provide inputs to that policy, queues to track needed work, and the background threads that are used to do compilation.


### Tiered Compilation Policy (2.1 RTM) ###

There are two mechanisms that need to be satisfied in order for a Tier0 method to be promoted to Tier1:

1. The method needs to be called at least 30 times, as measured by the call counter, and this gives us a rough notion that the method is 'hot'. The number 30 was derived with a small amount of early empirical testing but there hasn't been a large amount of effort applied in checking if the number is optimal. We assumed that both the policy and the sample benchmarks we were measuring would be in a state of flux for a while to come so there wasn't much reason to spend a lot of time finding the exact maximum of a shifting curve. As best we can tell there is also not a steep response between changes in this value and changes in the performance of many scenarios. An order of magnitude should produce a notable difference but +-5 can vanish into the noise.

2. At startup a timer is initiated with a 100ms timeout. If any Tier0 jitting occurs while the timer is running then it is reset. If the timer completes without any Tier0 jitting then, and only then, is call counting allowed to commence. This means a method could be called 1000 times in the first 100ms, but the timer will still need to expire and have the method called 30 more times before it is eligible for Tier1. The reason for the timer is to measure whether or not Tier0 jitting is still occuring, which is a heuristic to measure whether or not the application is still in its startup phase. Before adding the timer we observed that both the call counter and background threads compiling Tier1 code versions were slowing down the foreground threads trying to complete startup, and this could result in losing all the startup performance wins from Tier0 jitting. By delaying until after 'startup' the Tier0 code is left running longer, but that was nearly always a better performing outcome than trying to replace it with Tier1 code too eagerly.

After these two conditions are satisfied the method is placed in a queue for Tier1 compilation, compiled on a background thread, and then the Tier1 version is made active.

Known Issues (some of which have already generated policy changes post-2.1 RTM):

1. The call counter may not adequately address cases where methods are hot by virtue of containing loops, even if they aren't invoked many times.
2. The fixed time value on the timer doesn't scale depending on hardware or process resource constraints.
3. Once the 100ms elapses with no Tier0, the timer never restarts. Some applications go through multiple iterations of startup-like behavior. For example a server may have an initial burst of activity to listen for requests and then go idle. Then a request arrives more than 100ms later, triggering the first execution of all the request handling code which begins jitting heavily once again.
4. It's not clear if Tier0 jitting is the best indicator for startup-like behavior, or the best indicator that background jitting would contend for resources with the foreground thread.

Despite the anticipated shortcomings, the 2.1 RTM policy is a surprisingly decent heuristic for many scenarios we measured.

Implementation
==============

The majority of the implementation can be located in [tieredcompilation.h](../../../src/coreclr/vm/tieredcompilation.h), and [tieredcompilation.cpp](../../../src/coreclr/vm/tieredcompilation.cpp)

The call counter is implemented in [callcounting.h](../../../src/coreclr/vm/callcounting.h), and [callcounting.cpp](../../../src/coreclr/vm/callcounting.cpp)

The policy that determines which methods are eligible for tiering is implemented in `MethodDesc::IsEligibleForTieredCompilation`, located in [method.hpp](../../../src/coreclr/vm/method.hpp)

Most of the implementation is relatively straightforward given the design and best described by reading the code, but a few notes:

1. The current call counter implementation is utterly naive and using the PreStub has a high per-invocation cost relative to other more sophisticated implementation options. We expected it would need to change sooner, but so far despite having some measurable cost it hasn't been reached the top of the priority list for performance gain vs. work necessary. Part of what makes it not as bad as it looks is that there is a bound on the number of times it can be called for any one method and relative to typical 100,000 cycle costs for jitting a method even an expensive call counter doesn't make a huge impact.

2. Right now background compilation is limited to a single thread taken from the threadpool and used for up to 10ms. If we need more time than that we return the thread and request another. The goal is to be a good citizen in the threadpool's overall workload while still doing enough work in chunks that we get decent cache and thread quantum utilization. It's possible we could do better as the policy here hasn't been profiled much. Thus far we haven't profiled any performance issues that suggested we should be handling this differently.
