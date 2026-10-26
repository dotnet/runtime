# System.Runtime
Contains fundamental classes and base classes that define commonly-used value and reference data types, events and event handlers, interfaces, attributes, and processing exceptions.

Documentation can be found here: https://learn.microsoft.com/dotnet/api/system?view=net-7.0.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-System.Runtime+label%3A%22help+wanted%22+) issues.

## Source
* [../System.Private.CoreLib/src/System](../System.Private.CoreLib/src/System)
* Tests for this library live in [./tests](./tests) and [../System.Runtime.Extensions/tests](../System.Runtime.Extensions/tests)

## Deployment
[System.Runtime](https://www.nuget.org/packages/System.Runtime) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.

## GC pause callback prototype

This checkout prototypes `GC.RegisterPauseDataCallback`, which returns a disposable
subscription. Its callback receives one `GCPauseData` value and a separate
`missedRecords` count. The public names and contract are experimental, not approved
API.

The first implementation supports CoreCLR and NativeAOT. Other runtimes and older
standalone garbage collectors reject registration with
`PlatformNotSupportedException`. Each pause describes an existing contribution to
GC pause accounting, not the entire lifetime of a background collection. A
background collection can produce several records with the same collection index;
foreground collections during it are classified as blocking. LOH and POH work is
included in generation 2, not reported again as separate generations.

Callbacks run on the finalizer thread, outside GC-critical execution. They must
return promptly and catch their own exceptions: an escaping exception terminates
the process. Slow callbacks delay other subscribers and ordinary finalizers.
Disposal stops new deliveries without waiting for a callback already selected,
and a callback may dispose its own subscription.

Capture uses a shared 4,096-record buffer. A full buffer skips new records rather
than blocking the collector or growing memory indefinitely. Sequence numbers
include skipped records. Each subscriber receives newly observed overflow counts
with its next eligible callback; there is no separate notification for loss alone.
New subscriptions do not receive older buffered pauses.

DiagnosticSource uses this public API to implement `dotnet.gc.pause.duration` in
the opt-in `System.Runtime.Detailed.GC` meter on .NET 11 and later. The histogram
reports seconds with `gc.heap.generation` (`gen0`, `gen1`, `gen2`) and
`gc.pause.type` (`blocking`, `background`) tags. It subscribes only while enabled.
Histogram listeners run synchronously on the finalizer thread too; there is no
worker thread or implicit export step. The histogram can undercount when capture
overflows; consumers needing loss information can subscribe to the callback API.
The existing `System.Runtime` meter and cumulative `dotnet.gc.pause.time` metric
are unchanged.
