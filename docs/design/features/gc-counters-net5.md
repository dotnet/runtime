# GC Counters for .NET 5

This document describes the new GC EventCounters that are being added in .NET 5.

## Problem

The garbage collector is one of the key runtime components that contribute to the performance characteristics of a .NET application. We added a number of GC counters to CoreCLR in `System.Runtime` provider in .NET Core 3.0. Number of first-party and external customers adopted these counters and have actively been using them in production.
Currently the exposed GC counters are:

* GC Heap Size
* % time in GC
* Gen 0/1/2 Collections Rate
* Gen 0/1/2 Size

For "deeper" diagnostics on GC performance, one needs to collect a trace via ETW/LTTng/EventPipe and process the trace using TraceEvent programmatically or view it on tools like PerfView/VS. This is not the most convenient way for developers to "easily" see some of the relevant GC metrics, and moreover, the performance overhead of tracing may not be something they can incur in performance-sensitive environments.

In essence, the GC metrics can be thought of as a "basic" performance indicator, as they provide a high-level point-of-view of the GC performance, and can be used to detect regressions in performance tests, or used for monitoring  production environments.

We received feedback from first-party customers (i.e. Exchange, Bing) to increase the number of counters in .NET 5, specifically for the GC, to help them monitor some of their key performance metrics in production. The counters that were requested in the original issues were:
* GC Heap Fragmentation %
* Gen 0/1/2 Pause Time
* Gen 0/1/2 Time Between Each GC

## Solution

The proposal is to add the following GC counters:

* GC Heap Fragmentation %
* Gen 0/1/2 Pause Time 

After giving some thoughts, the Gen 0/1/2 Time between each GC counter didn't make much sense to add because it can be inferred from the existing Gen 0/1/2 Collections Rates (The time between each GC is inversely proportional to these rates).

## Computing the Metrics

Both Fragmentation % and the Gen 0/1/2 Pause time can be computed using the GCMemoryInfo API. 

All EventCounter metrics are emitted at a specific interval set by the listener. For System.Runtime counters, all the counters are `Polling` counters, which means that these counters polls for values at the interval specified by the listener. This means that similar to the existing GC counters, these counters show “snapshotted” values of the metric captured at the last sampling period, so if there are more than 1 GCs happening within the sampling period, the counter would miss out on a lot of the picture in between the samples. This introduces a potential inaccuracy factor to the counter values by the nature of sampling - but over a sufficient interval the values being produced should be able to point to a performance problem if one exists.

To implement these counters, we can query the GCMemoryInfo API precisely once per interval, and update these four metrics using the retrieved GCMemoryInfo.

Specifically, `RuntimeEventSourceHelper` can be modified to keep the necessary states (i.e. last-updated timestamp - basically the last time GC.GetMemoryInfo() was called), helper methods can be added to `RuntimeEventSourceHelper` to retrieve the info from latest-grabbed GCMemoryInfo, and call `GC.GetMemoryInfo()` again if the difference between current time and the latest GC.GetMemoryInfo() API update is more than the update interval on `System.Runtime`.

The rationale behind doing this instead of simply calling GC.GetMemoryInfo() for each of the updates of each GC counters is because it is possible for another GC to happen between such update. This means that in the same "batch" of counters, it may show info from two separate GCs which may be misleading and confusing.
