# Randomized Allocation Sampling

Christophe Nasarre (@chrisnas), Noah Falk (@noahfalk) - 2024

## Introduction

.NET developers want to understand the GC allocation behavior of their programs both for general observability and specifically to better understand performance costs. Although the runtime has a very high performance GC, reducing the number of bytes allocated in a scenario can have notable impact on the total execution time and frequency of GC pauses. Some ways developers understand these costs are measuring allocated bytes in:
1. Microbenchmarks such as Benchmark.DotNet
2. .NET APIs such as [GC.GetAllocatedBytesForCurrentThread()](https://learn.microsoft.com/dotnet/api/system.gc.getallocatedbytesforcurrentthread)
3. Memory profiling tools such as VS profiler, PerfView, and dotTrace
4. Metrics or other production telemetry

Analysis of allocation behavior often starts simple using the total bytes allocated while executing a block of code or during some time duration. However for any non-trivial scenario gaining a deeper understanding requires attributing allocations to specific lines of source code, callstacks, types, and object sizes. .NET's current state of the art technique for doing this is using a profiling tool to sample using the [AllocationTick](https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-garbage-collection-events#gcallocationtick_v3-event) event. When enabled this event triggers approximately every time 100KB has been allocated. However this sampling is not a random sample. It has a fixed starting point and stride which can lead to significant sampling error for allocation patterns that are periodic. This has been observed in practice so it isn't merely a theoretical concern. The new randomized allocation sampling feature is intended to address the shortcomings of AllocationTick and offer more rigorous estimations of allocation behavior and probabilistic error bounds. We do this by creating a new `AllocationSampled` event that profilers can opt into via any of our standard event tracing technologies (ETW, EventPipe, Lttng, EventListener). The new event is completely independent of AllocationTick and we expect profilers will prefer to use the AllocationSampled event on runtime versions where it is available.

The initial part of this document describe the conceptual sampling model and how we suggest the data be interpretted by profilers. The latter portion describes how the sampling model is implemented in runtime code efficiently.

## The sampling model

When the new AllocationSampled event is enabled, each managed thread starts sampling independent of one another. For a given thread there will be a sequence of allocated objects Object_1, Object_2, etc that may continue indefinitely. Each object has a corresponding .NET type and size. The size of an object includes the object header, method table, object fields, and trailing padding that aligns the size to be a multiple of the pointer size. It does not include any additional memory the GC may optionally allocate for more than pointer-sized alignment, filling gaps that are impossible/inefficient to use for objects, or other GC bookkeeping data structures. Also note that .NET does have a non-GC heap where some objects that stay alive for process lifetime are allocated. Those non-GC heap objects are ignored by this feature.

When each new object is allocated, conceptually the runtime starts doing independent [Bernoulli Trials](https://en.wikipedia.org/wiki/Bernoulli_trial) (weighted coin flips) for every byte in the object. Each trial has probability p = 1/102,400 of being considered a success. As soon as one successful trial is observed no more trials are performed for that object and an AllocationSampled event is emitted. This event contains the object type, its size, and the 0-based offset of the byte where the successful trial occured. This means for a given object if an event was generated `offset` failed trials occured followed by a successful trial, and if no event is generated `size` failed trials occured. This process continues indefinitely for each new allocated object.

This sampling process is closely related to the [Bernoulli process](https://en.wikipedia.org/wiki/Bernoulli_process) and is a well studied area of statistics. Skipping ahead to the end of an object once a successful sample has been produced does require some accomodations in the analysis, but many key results are still applicable.

## Using the feature

### Enabling sample events

The allocation sampled events are enabled on the `Microsoft-Windows-DotNetRuntime` provider using keyword `0x80000000000` at informational or higher level. For more details on how to do this using different event tracing technologies see [here](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventsource-collect-and-view-traces).

### Interpretting sample events

Although diagnostic tools are free to interpret the data in whatever way, we have some recommendations for analysis that we expect is useful and statistically sound.

#### Definitions

For all of this section assume that we enabled the AllocationSampling events and have observed `s` such sample events were generated from a specific thread - `event_1`, `event_2`, ... `event_s`. Each `event_i` contains corresponding fields `type_i`, `size_i`, and `offset_i`. Let `u_i = size_i - offset_i`. `u_i` represents the successful trial byte + the number bytes which remained after it in the same object. Let `u` = the sum of all the `u_i`, `i` = 1 to `s`. `p` is the constant 1/102400, the probability that each trial is successful. `q` is the complement 1 - 1/102400.

#### Estimation strategies

We have explored two different mathematical approaches for [estimating](https://en.wikipedia.org/wiki/Estimator) the number of bytes that were allocated given a set of observed sample events. Both approaches are unbiased which means if we repeated the same sampling procedure many times we expect the average of the estimates to match the number of bytes allocated. Where the approaches differ is on the particular distribution of the estimates.

#### Estimation Approach 1: Weighted samples

We believe this approach gives estimates with lower [Mean Squared Error](https://en.wikipedia.org/wiki/Mean_squared_error) but the exact shape of the distribution is hard to calculate so we don't know a good way to produce [confidence intervals](https://en.wikipedia.org/wiki/Confidence_interval) based on small numbers of samples. The distribution does approach a [normal distribution](https://en.wikipedia.org/wiki/Normal_distribution) as the number of samples increase ([Central Limit Theorem](https://en.wikipedia.org/wiki/Central_limit_theorem)) but we haven't done any analysis attempting to define how rapidly that convergence occurs.

To estimate the number of bytes using this technique let `estimate_i = size_i/(1 - q^size_i)` for each sample `i`. Then sum `estimate_i` over all samples to get a total estimate of the allocated bytes. With sufficiently many samples the estimate distribution should converge to a normal distribution with variance at most `N*q/p` for `N` total bytes of allocated objects.

##### Statistics stuff

Understanding this part isn't necessary to use the estimation formula above but may be helpful.

Proving the weighted sample estimator is unbiased:
Consider the sequence of all objects allocated on a thread. Let `X_j` be a random indicator variable that has value `size_j/(1 - q^size_j)` if the `j`th object is sampled, otherwise zero. Our estimation formula above is the sum of all `X_j` because only the sampled objects will contribute a non-zero term. Based on our sampling procedure the probability for an object to be sampled is `1-q^size_j` which means `E(X_j) = size_j/(1 - q^size_j) * Pr(object j is sampled) = size_j/(1 - q^size_j) * (1 - q^size_j) = size_j`. By linearity of expectation, the expected value of the sum is the sum of the expected values = sum of `size_j` for all `j` = total size of allocated objects.

The variance for this estimation is the sum of variances for each `X_j` term, `(size_j^2)*(q^size_j)/(1-q^size_j)`. If we assume there are `N` total bytes of objects divided up into `N/n` objects of size `n` then total variance for that set of objects would be `(N/n)*n^2*q^n/(1-q^n) = N*n*q^n/(1-q^n)`. That expression is maximized when `n=1` so the maximum variance for any collection objects with total size `N` is `N*1*q^1/(1-q^1) = N*q/(1-q) = N*q/p`.

#### Estimation Approach 2: Estimating failed trials

This is an alternate estimate that has a more predictable distribution, but potentially higher [Mean Squared Error](https://en.wikipedia.org/wiki/Mean_squared_error). You could use this approach to produce both estimates and confidence intervals, or use the weighted sample formula to produce estimates and use this one solely as a conservative confidence interval for the estimate.

The estimation formula is `sq/p + u`.

This estimate is based on the [Negative Bernoulli distribution](https://en.wikipedia.org/wiki/Negative_binomial_distribution) with `s` successes and `p` chance of success. The `sq/p` term is the mean of this distribution and represents the expected number of failed trials necessary to observe `s` successful trials. The `u` term then adds in the number of successful trials (1 per sample) and the number of bytes for which no trials were performed (`u_i-1` per sample).

Here is an approach to calculate a [confidence interval](https://en.wikipedia.org/wiki/Confidence_interval) estimate based on this distribution:

1. Decide on some probability `C` that the actual number of allocated bytes `N` should fall within the interval. You can pick a probability arbitrarily close to 1 however the higher the probability is the wider the estimated interval will be. For the remaining `1-C` probability that `N` is not within the interval we will pick the upper and lower bounds so that there is a `(1-C)/2` chance that `N` is below the interval and `(1-C)/2` chance that `N` is above the interval. We think `C=0.95` would be a reasonable choice for many tools which means there would be a 2.5% chance the actual value is below the lower bound, a 95% chance it is between the lower and upper bound, and a 2.5% chance it is above the upper bound.

2. Implement some method to calculate the Negative Binomial [CDF](https://en.wikipedia.org/wiki/Cumulative_distribution_function). Unfortunately there is no trivial formula for this but there are a couple potential approaches:
    a. The Negative Binomial Distribution has a CDF defined based on the [regularized incomplete beta function](https://en.wikipedia.org/wiki/Beta_function#Incomplete_beta_function). There are various numerical libraries such as scipy in Python that will calculate this for you. Alternately you could directly implement numerical approximation techniques to evaluate the function, either approximating the integral form or approximating the continued fraction expansion.
    b. The Camp-Paulson approximation described in [Barko(66)](https://www.stat.cmu.edu/technometrics/59-69/VOL-08-02/v0802345.pdf). We validated that for p=0.00001 this approximation was within ~0.01 of the true CDF for any number of failures at s=1, within ~0.001 of the true CDF at s=5, and continues to get more accurate as the sample count increases.

3. Do binary search on the CDF to locate the input number of failures for which `CDF(failures, s, p)` is closest to `(1-C)/2` and `C + (1-C)/2`. Assuming that `CDF(low_failures, s, p) = (1-C)/2` and `CDF(high_failures, s, p) = C + (1-C)/2` then the confidence interval for `N` is `[low_failures+u, high_failures+u]`.

For example if we select `C=0.95`, observed 8 samples and `u=10,908` then we'd use binary search to find `CDF(353666, 8, 1/102400) ~= 0.025` and `CDF(1476870, 8, 1/102400) ~= 0.975`. Our interval estimate for the number of bytes allocated would be `[353666 + 10908, 1476870 + 10908]`.

To get a rough idea of the error in proportion to the number of samples, here is table of calculated Negative Binomial failed trials for the 0.025 and 0.975 thresholds of the CDF:

| # of samples (successes)   | CDF() = 0.025           | CDF() = 0.975 |
| ---------------------------| ------------------------| --------------------------- |
| 1                          | 2591                    | 377738 |
| 2                          | 24800                   | 570531 |
| 3                          | 63349                   | 739802 |
| 4                          | 111599                  | 897761 |
| 5                          | 166241                  | 1048730 |
| 6                          | 225469                  | 1194827 |
| 7                          | 288185                  | 1337279 |
| 8                          | 353666                  | 1476870 |
| 9                          | 421407                  | 1614137 |
| 10                         | 491039                  | 1749469 |
| 20                         | 1250954                 | 3038270 |
| 30                         | 2072639                 | 4264804 |
| 40                         | 2926207                 | 5459335 |
| 50                         | 3800118                 | 6633475 |
| 100                        | 8331581                 | 12342053 |
| 200                        | 17739679                | 23413825 |
| 300                        | 27341465                | 34291862 |
| 400                        | 37043463                | 45069676 |
| 500                        | 46809487                | 55783459 |
| 1000                       | 96149867                | 108842093 |
| 2000                       | 195919830               | 213870137 |
| 3000                       | 296301551               | 318286418 |
| 4000                       | 396999923               | 422386047 |
| 5000                       | 497900649               | 526283322 |
| 10000                      | 1004017229              | 1044156743 |

Notice that if we compare the expected total number of trials (102400 * # of samples) to the estimated ranges, at 10 samples the error bars extend more than 50% in each direction showing the predictions on so few samples are very imprecise. However at 1,000 samples the error is ~6% in each direction and at 10,000 samples ~2% in each direction.

The variance for the Negative Binomial Distribution is `sq/p^2`. In the limit where all allocated objects have size 1 byte, `E(s)=Np` which gives an expected variance of `Nq/p`, the same as with the weighted sampled approach. However as object sizes increase the variance on approach 1 decreases more rapidly than in this approach.

#### Compensating for bytes allocated on a thread in between events

It is likely you want to estimate allocations starting and ending at arbitrary points in time that do not correspond exactly with the moment a sampling event was emitted. This means the initial sampling event covered more time than the allocation period we are interested in and the allocations at the end aren't included in any sampling event. You can conservatively adjust the error bounds to account for the uncertainty in the starting and ending allocations. If the starting point is not aligned with a sampling event calculate the lower bound of allocated bytes as if there was one fewer sample received. If the ending point is not aligned with a sampling event calculate the upper bound as if there was one more sample received.

#### Estimating the total number of bytes allocated on all threads

The per-thread estimations can be repeated for all threads and summed up.

#### Estimating the number of bytes allocated for objects of a specific type, size or other characteristic

Select from the sampling events only those events which occured in objects matching your criteria. For example if you want to estimate the number of bytes allocated for Foo typed objects, select the samples in Foo-typed objects. Using this reduced set of samples do the same estimation technique as above. The error on this estimation will also be based on the number of samples in your filtered subset. If there were 1000 initial samples but only 3 of those samples were in Foo-typed objects that might generate an estimate of 310K bytes of Foo objects but beware that the potential sampling error for a small number of samples is very large.

## Implementation design

Overall the implementation needs to do a few steps:
1. Determine if sampling events are enabled. If no, there is nothing else to do, but if yes we need to do steps (2) and (3).
2. Use a random number generator to simulate random trials for each allocated byte and determine which objects contain the successful trials
3. When a successful trial occurs, emit the AllocationSampled event

Steps (1) and (3) are straightforward but step (2) is non-trivial to do correctly and performantly. For step (1) we use the existing macro ETW_TRACING_CATEGORY_ENABLED() which despite its name works for all our event tracing technologies. For step (3) we defined a method FireAllocationSampled() in gchelpers.cpp and the code to emit the event is in there. Like all runtime events the definition for the event itself is in ClrEtwAll.man. All the remaining discussion is how we accomplish step (2).

Our conceptual sampling model involves doing Bernoulli trials for every byte of an object. In theory we could implement that very literally. Each object allocation would run a for loop n iterations for an n byte object and generate random coin flips with a pseudo random number generator (PRNG). However doing this would be incredibly slow. A good way to understand the actual implementation is to imagine we started with this simple slow approach and then did several iterative transformations to make it run faster while maintaining the same output. Imagine that we have some function `bool GetNextTrialResult(CLRRandom* pRandom)` that takes a PRNG and should randomly return true with probability 1 in 102,400. It might be implemented:

```
bool GetNextTrialResult(CLRRandom* pRandom)
{
    return pRandom->NextDouble() < 1/102400;
}
```

We don't have to generate random numbers at the instant we need them however, we are allowed to cache a batch of them at a time and dispense them later. For simplicity treat all the apparent global variables in these examples as being thread-local. In pseudo-code that looks like:

```
List<bool> _cachedTrials = PopulateTrials(pRandom);
List<bool> PopulateTrials(CLRRandom* pRandom)
{
    List<bool> trials = new List<bool>();
    for(int i = 0; i < 100; i++)
    {
        trials.Push(pRandom->NextDouble() < 1/102400);
    }
    return trials;
}
bool GetNextTrialResult(CLRRandom* pRandom)
{
    bool ret = _cachedTrials.Pop();
    // if we are out of trials, cache some more for next time
    if(_cachedTrials.Count == 0)
    {
        _cachedTrials = PopulateTrials(pRandom);
    }
    return ret;
}
```

Notice that almost the every entry in the cached list will be false so this is an inefficient way to store it. Rather than storing a large number of false bits we could store a single number that represents a run of zero or more contiguous false bools followed by a single true bool. There is also no requirement that our cached batches of trials are the same size so we could cache exactly one run of false results. In pseudo-code that looks like:

```
BigInteger _cachedFailedTrials = PopulateTrials(pRandom);
BigInteger PopulateTrials(CLRRandom* pRandom)
{
    BigInteger failedTrials = 0;
    while(pRandom->NextDouble() >= 1/102400)
    {
        failedTrials++;
    }
    return failedTrials;
}
bool GetNextTrialResult(CLRRandom* pRandom)
{
    bool ret = (_cachedFailedTrials == 0);
    _cachedFailedTrials--;
    // if we are out of trials, cache some more for next time
    if(cachedTrials < 0)
    {
        _cachedFailedTrials = PopulateTrials(pRandom);
    }
    return ret;
}
```

Rather than generating `_cachedFailedTrials` by doing many independent queries to a random number generator we can use some math to speed this up. The probability `_cachedFailedTrials` has some particular value `X` is given by the [Geometric distribution](https://en.wikipedia.org/wiki/Geometric_distribution). We can use [Inverse Transform Sampling](https://en.wikipedia.org/wiki/Inverse_transform_sampling) to generate random values for this distribution directly. The CDF for the Geometric distribution is `1-(1-p)^(floor(x)+1)` which means the inverse is `floor(ln(1-y)/ln(1-p))`.

We've been using BigInteger so far because mathmatically there is a non-zero probability of getting an arbitrarily large number of failed trials in a row. In practice however our PRNG has its outputs constrained to return a floating point number with value k/MAX_INT for an integer value of k between 0 and MAX_INT-1. The largest value PopulateTrials() can return under these constraints is ~2.148M which means a 32bit integer can easily accomodate the value. The perfect mathematical model of the Geometric distribution has a 0.00000005% chance of getting a larger run of failed trials but our PRNG rounds that incredibly unlikely case to zero probability.

Both of these changes combined give the pseudo-code

```
int _cachedFailedTrials = CalculateGeometricRandom(pRandom);
// Previously this method was called PopulateTrials()
// Use Inverse Transform Sampling to calculate a random value from the Geometric distribution
int CalculateGeometricRandom(CLRRandom* pRandom)
{
    return floor(log(1-pRandom->NextDouble())/log(1-1/102400));
}
bool GetNextTrialResult(CLRRandom* pRandom)
{
    bool ret = (_cachedFailedTrials == 0);
    _cachedFailedTrials--;
    // if we are out of trials, cache some more for next time
    if(_cachedFailedTrials < 0)
    {
        _cachedFailedTrials = CalculateGeometricRandom(pRandom);
    }
    return ret;
}
```

When allocating an object we need to do many trials at once, one for each byte. A naive implementation of that would look like:

```
bool DoesAnyTrialSucceed(CLRRandom* pRandom, int countOfTrials)
{
    for(int i = 0; i < countOfTrials; i++)
    {
        if(GetNextTrialResult(pRandom)) return true;
    }
    return false;
}
```

However the `_cachedFailedTrials` representation lets us speed this up by checking if the number of failed trials in the cache covers the number of trials we need to perform without iterating through them one at a time:

```
bool DoesAnyTrialSucceed(CLRRandom* pRandom, int countOfTrials)
{
    bool ret = _cachedFailedTrials < countOfTrials;
    _cachedFailedTrials -= countOfTrials;
    // if we are out of trials, cache some more for next time
    if(ret)
    {
        _cachedFailedTrials = CalculateGeometricRandom(pRandom);
    }
    return ret;
}
```


We are getting closer to mapping our pseudo-code implementation to the real CLR code. The current CLR implementation for memory allocation has the GC sub-allocate blocks of memory 8KB in size which the runtime is allowed to sub-allocate from. The GC gives out an `alloc_context` to each thread which has a `alloc_ptr` and 'alloc_limit' fields. These fields define the memory range [alloc_ptr, alloc_limit) which can be used to sub-allocate objects. The runtime has optimized assembly code helper functions to increment `alloc_ptr` directly for objects that are small enough to fit in the current range and don't require any special handling. For all other objects the runtime invokes a slower allocation path that ultimately calls the GC's Alloc() function. If the alloc_context is exhausted, calling GC Alloc() also allocates a new 8KB block for future fast object allocations to use. In order to allocate objects we could naively do this:

```
void* FastAssemblyAllocate(int objectSize)
{
    Thread* pThread = GetThread();
    CLRRandom* pRandom = pThread->GetRandom();
    alloc_context* pAllocContext = pThread->GetAllocContext();
    void* alloc_end = pAllocContext->alloc_ptr + objectSize;
    if(IsSamplingEnabled() && DoesAnyTrialSucceed(pRandom, objectSize))
        PublishSamplingEvent();
    if(alloc_limit < alloc_end)
        return SlowAlloc(objectSize);
    else
        void* objectAddr = pAllocContext->alloc_ptr;
        pAllocContext->alloc_ptr = alloc_end;
        *objectAddr = methodTable
        return objectAddr;
}
```

Although orders of magnitude faster than where we started, this is still too slow. We don't want to put extra conditional checks for IsSamplingEnabled() and DoesAnyTrialSucceed() in the fast path of every allocation. Instead we want to combine the two if conditions down to a single compare and jump, then handle publishing a sample event as part of the slow allocation path. Note that the value of the expression `alloc_ptr + _cachedFailedTrials` doesn't change by repeated calls to the FastAssemblyAllocate() as long as we don't go down the SlowAlloc path or the PublishSamplingEvent() path. Each invocation increments `alloc_ptr` by `objectSize` and decrements `_cachedFailedTrials` by the same amount leaving the sum unchanged. Lets define that sum `alloc_ptr + _cachedFailedTrials = sampling_limit`. You can imagine that if we started allocating objects contiguously from `alloc_ptr`, `sampling_limit` represents the point in the memory range where whatever object overlaps it contains the successful trial and emits the sampling event. A little more rigorously `DoesAnyTrialSucceed()` returns true when `_cachedFailedTrials < objectSize`. Adding `alloc_ptr` to each side shows this is the same as the condition `sampling_limit < alloc_end`:

```
_cachedFailedTrials < objectSize
_cachedFailedTrials + alloc_ptr < objectSize + alloc_ptr
sampling_limit < alloc_end
```

Last to combine the two if conditionals we can define a new field `combined_limit = min(sampling_limit, alloc_limit)`. If sampling events aren't enabled then we define `combined_limit = alloc_limit`. This means that a single check `if(alloc_end < combined_limit)` detects when either the object exceeds `alloc_limit` or it exceeds `sampling_limit`. The runtime actually has a bunch of different fast paths depending on the type of the object being allocated and the CPU architecture, but converted to pseudo-code they all look approximately like this:

```
void* FastAssemblyAllocate(int objectSize)
{
    Thread* pThread = GetThread();
    alloc_context* pAllocContext = pThread->GetAllocContext();
    void* alloc_end = pAllocContext->alloc_ptr + objectSize;
    if(combined_limit < alloc_end)
        return SlowAlloc(objectSize);
    else
        void* objectAddr = pAllocContext->alloc_ptr;
        pAllocContext->alloc_ptr = alloc_end;
        *objectAddr = methodTable
        return objectAddr;
}
```

The only change we've made in the assembly helpers is doing a comparison against combined_limit instead of alloc_limit which should have no performance impact. Look at [JIT_TrialAllocSFastMP_InlineGetThread](https://github.com/dotnet/runtime/blob/5c8bb402e6a8274e8135dd00eda2248b4f57102f/src/coreclr/vm/amd64/JitHelpers_InlineGetThread.asm#L38) for an example of what one of these helpers looks like in assembly code.

The pseudo-code and concepts we've been describing here are now close to matching the runtime code but there are still some important differences to call out to map it more exactly:

1. In the real runtime code the assembly helpers call a variety of different C++ helpers depending on object type and all of those helpers in turn call into [Alloc()](https://github.com/dotnet/runtime/blob/5c8bb402e6a8274e8135dd00eda2248b4f57102f/src/coreclr/vm/gchelpers.cpp#L201). Here we've omitted the different per-type intermediate functions and represented all of them as the SlowAlloc() function in the pseudo-code.

2. The combined_limit field is a member of ee_alloc_context rather than alloc_context. This was done to avoid creating a breaking change in the EE<->GC interface. The ee_alloc_context contains an alloc_context within it as well as any additional fields we want to add that are only visible to the EE.

3. In order to reduce the number of per-thread fields being managed the real implementation doesn't have an explicit `sampling_limit`. Instead this only exists as the transient calculation of `alloc_ptr + CalculateGeometricRandom()` that is used when computing an updated value for `combined_limit`. Whenever `combined_limit < alloc_limit` then it is implied that `sampling_limit = combined_limit` and `_cachedFailedTrials = combined_limit - alloc_ptr`. However if `combined_limit == alloc_limit` that represents one of two possible states:
- Sampling is disabled
- Sampling is enabled and we have a batch of cached failed trials with size `alloc_limit - alloc_ptr`. In the examples above our batches were N failures followed by a success but this is just N failures without any success at the end. This means no objects allocated in the current AC are going to be sampled and whenever we allocate the N+1st byte we'll need to generate a new batch of trial results to determine whether that byte was sampled.
If it turns out to be easier to track `sampling_limit` with an explicit field when sampling is enabled we could do that, it just requires an extra pointer per-thread. As memory overhead its not much, but it will probably land in the L1 cache and wind up evicting some other field on the Thread object that now no longer fits in the cache line. The current implementation tries to minimize this cache impact. We never did any perf testing on alternative implementations that do track sampling_limit explicitly so its possible the difference isn't that meaningful.

4. When we generate batches of trial results in the examples above we always used all the results before generating a new batch, however the real implementation sometimes discards part of a batch. Implicitly this happens when we calculate a value for `sampling_limit=alloc_ptr+CalculateGeometricRandom()`, determine that `alloc_limit` is smaller than `sampling_limit`, and then set `combined_limit=alloc_limit`. Discarding also happens any time we recompute the `sampling_limit` based on a new random value without having fully allocated bytes up to `combined_limit`. It may seem suspicious that we can do this and still generate the correct distribution of samples but it is OK if done properly. Bernoulli trials are independent from one another so it is legal to discard trials from our cache as long as the decision to discard a given trial result is independent of what that trial result is. For example in the very first pseudo-code sample with the List<bool>, it would be legal to generate 100 boolean trials and then arbitrarily truncate the list to size 50. The first 50 values in the list are still valid bernoulli trials with the original p=1/102,400 of being true, as will be all the future ones from the batches that are populated later. However if we scanned the list and conditionally discarded any trials that we observed had a success result that would be problematic. This type of selective removal changes the probability distribution for the items that remain.

5. The GC Alloc() function isn't the only time that the GC updates alloc_ptr and alloc_limit. They also get updated during a GC in the callback inside of GCToEEInterface::GcEnumAllocContexts(). This is another place where combined_limit needs to be updated to ensure it stays synchronized with alloc_ptr and alloc_limit.


## Thanks

Thanks to Christophe Nasarre (@chrisnas) at DataDog for implementing this feature and Mikelle Rogers for doing the investigation of the Camp-Paulson approximation.