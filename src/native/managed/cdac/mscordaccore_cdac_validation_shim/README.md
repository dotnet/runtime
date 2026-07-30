# mscordaccore_cdac_validation_shim

A **test-only** NativeAOT binary that sits between a diagnostics consumer (SOS, `dotnet-dump`,
ClrMD, ...) and the production cDAC, and validates the cDAC against the legacy DAC on every
call. It is never shipped and never packaged: it exists so the dotnet/diagnostics SOS test
suite can run the whole cDAC surface and report where it diverges from the DAC.

Before the production decoupling, this comparison lived inside the cDAC itself: the in-box
DAC created the cDAC reader, handed it its own `ISOSDacInterface` and the cDAC compared
results in `#if DEBUG` blocks. That coupling is gone from the product. The comparison blocks
were moved here verbatim, so the validation behavior is preserved without the production cDAC
knowing that a legacy DAC exists.

## How it works

The shim exports the same entry points as the production cDAC:

```
cdac_reader_init            cdac_reader_free
cdac_reader_create_sos_interface        cdac_reader_create_dacdbi_interface
CLRDataCreateInstance       DacDbiInterfaceInstance
DbgShimCreateInstanceFromContractDescriptor
```

so a consumer can load it wherever it would load `mscordaccore_universal`.

On first use it loads and pins two modules for the lifetime of the process:

* the **production cDAC** (`mscordaccore_universal`) from the directory the shim itself was
  loaded from — so deployment is simply "put both binaries in the same folder";
* the **legacy DAC** from `DOTNET_CDAC_LEGACY_DAC_PATH`.

Neither module is ever unloaded: the COM objects handed to the consumer are implemented by
code inside them.

Every call is then:

1. forwarded to the production cDAC — **its result is what the caller gets**;
2. forwarded to the legacy DAC with a private copy of the output buffers;
3. compared, with any mismatch reported to stderr as `[cDAC] Validation mismatch: ...`.

The production cDAC result is always the one returned to the caller. The shim's `Debug` class
(`Hosting/Debug.cs`) shadows `System.Diagnostics.Debug` inside the proxies: the recovered
`Debug.Assert` / `Debug.Fail` / `Debug.ValidateHResult` calls first record and log the mismatch
(so the SOS test run always sees a `[cDAC] Validation mismatch` line) and then forward to the
real `System.Diagnostics.Debug.Fail`, so an assertion behaves exactly as it did in the
pre-refactor cDAC. Because the cDAC answer is already captured, the caller still receives it if
assertion execution continues.

## Environment variables

| Variable | Meaning |
|----------|---------|
| `DOTNET_CDAC_LEGACY_DAC_PATH` | Full path to the legacy DAC (`mscordaccore`). When unset the shim runs as a pure pass-through to the production cDAC. |
| `DOTNET_CDAC_VALIDATION_MODE` | `fallback` (default) or `strict`. |
| `DOTNET_CDAC_PRODUCTION_PATH` | Optional override for the production cDAC. Defaults to the cDAC adjacent to the shim. |

`fallback` mirrors the pre-refactor default: an API the cDAC answers with `E_NOTIMPL`
delegates to the legacy DAC. `strict` mirrors the old `CDAC_NO_FALLBACK=1`: only the
allowlisted APIs may delegate, everything else surfaces the cDAC's `E_NOTIMPL`. Blocked and
allowed delegations are logged exactly as before (`[cDAC] Blocked fallback: ...`).

The allowlist is preserved verbatim in `Hosting/LegacyFallbackHelper.cs`, including its two
quirks: it matches by *simple method name* (so any `EnumMemoryRegions`, on any interface, is
allowed) and by *exact file name* (so all of `DacDbiImpl.cs` is allowed but
`DacDbiImpl.NativeCodeInfo.cs` is not).

## Object, handle and callback pairing

The two implementations hand out different objects for the same entity, so the shim pairs
them:

* **Child objects.** Every COM object returned through a `DacComNullableByRef<T>` output is
  paired and wrapped in a proxy (`ShimProxy.Pair*`). Pairing is cached per session, so the
  caller sees stable interface identity. When the caller passes one of those objects back in,
  it is unwrapped to the cDAC object for the cDAC call and to the legacy object for the DAC
  call. An object that is *not* one of the shim's proxies is asserted on and passed through
  unchanged — it can only have come from the caller.
* **Enumeration handles.** `StartEnum*` produces a handle on each side; the shim registers the
  pair and hands the caller its own token, translating on every `Enum*` and releasing on
  `EndEnum*`. The paired state survives across the whole enumeration.
* **Query interface.** A proxy exposes exactly the interfaces the underlying production object
  exposes, so a consumer cannot observe a capability the cDAC does not have.
* **Caller callbacks.** `IXCLRDataProcess::TranslateExceptionRecordToNotification` takes a sink
  the caller implements. The cDAC drives the real sink through a recording proxy; the legacy
  DAC then drives a replaying proxy that compares each notification and returns the recorded
  HRESULT, so the caller's sink is invoked exactly once.

## Target mutations

Reads pass straight through to the caller's data target. Mutations are handled asymmetrically,
because running both implementations' writes against a live target would corrupt it and
letting both allocate would give the DAC a different scratch buffer than the cDAC used:

* the data target given to the **cDAC** executes the mutation and records it
  (`RecordingDataTarget`);
* the data target given to the **legacy DAC** compares the requested mutation against the
  recorded one and replays the recorded outcome, including the allocated address
  (`ReplayDataTarget`).

Record/replay state is scoped to one proxied call and nests, so a caller callback that
re-enters the shim cannot disturb the call already in flight.

## Limitations

* **`cdac_reader_*` runs without validation.** Those entry points receive a narrow callback
  ABI (memory read/write, thread context read/write, virtual alloc) rather than an
  `ICLRDataTarget`. `SynthesizedReaderDataTarget` derives what it can — the pointer size from
  the contract descriptor's flags word, and the contract descriptor address itself — and
  returns `E_NOTIMPL` for machine type, image bases, TLS slots, the current thread id and
  `Request`. The legacy DAC needs `GetMachineType`, so it normally refuses to initialize over
  that target; the shim reports this once and proxies the production cDAC alone. Consumers
  that want validation should use `CLRDataCreateInstance`, which carries a full data target.
* **`cdac_reader_create_dacdbi_interface` has no comparison side**, for the same reason: the
  legacy `DacDbiInterfaceInstance` needs the runtime module base and an `ICorDebugDataTarget`,
  neither of which the reader ABI carries. The standalone `DacDbiInterfaceInstance` export
  does compare.
* **Comparison coverage matches the pre-refactor cDAC exactly.** Where the old implementation
  had no `#if DEBUG` block, the shim performs no comparison. A few recovered blocks referenced
  cDAC-internal locals that do not exist on this side of the boundary and are reproduced with an
  equivalent adaptation rather than an HRESULT-only fallback: the `IXCLRDataFrame`/
  `IXCLRDataStackWalk` context comparison, which used the cDAC `Target`-bound
  `IPlatformAgnosticContext`, compares the meaningful context bytes instead (the layouts are
  fixed and packed); and the `GetThreadData` state comparison, which enumerated the internal
  `ThreadState` contract enum, uses a duplicated bit mask that must stay in sync with
  `Abstractions/Contracts/IThread.cs`. See `Proxies/*.cs` — every reproduced block is present.
* **Metadata enumerator handles are not paired.** `IMetaDataImport` enumerators returned to
  the caller are the production cDAC's. The recovered comparison blocks create and own their
  own legacy enumerators, which is how the pre-refactor code behaved.
* **Code-notification APIs are not compared.** `SetCodeNotification(s)` and
  `GetCodeNotification(s)` had no comparison block before the refactor, because both
  implementations allocate `g_pNotificationTable` on demand. The shim's replay data target now
  makes that safe (the DAC's allocation replays the cDAC's address and its writes are compared
  rather than executed), but the comparison coverage is left exactly as it was.

## Building

```bash
./build.sh -s tools.cdacvalidationshim -c Debug
```

The subset is opt-in and is not part of `tools.cdac`, so ordinary builds never produce it.
Output lands in `artifacts/bin/mscordaccore_cdac_validation_shim/<config>/<rid>/publish/`; it
is deliberately *not* installed into `artifacts/bin/coreclr/...`, so it cannot leak into a
runtime pack.

The shim is always compiled with `DEBUG` defined and optimizations off, regardless of the
repo configuration, because the recovered comparison blocks are guarded by `#if DEBUG`.

## Maintaining the proxies

The proxy classes under `Proxies/` are ordinary checked-in source. Each method follows the
same shape:

```csharp
int ISOSDacInterface.GetSomething(SomeData* data)
{
    using ShimCall shimCall = ShimCall.Enter();
    int hr = _cdacImpl is not null ? _cdacImpl.GetSomething(data) : HResults.E_NOTIMPL;
#if DEBUG
    if (_legacyImpl is not null)
    {
        // ... recovered verbatim from the pre-refactor cDAC ...
    }
#endif
    return hr;
}
```

`hr` is the production cDAC result and `_legacy*` are the legacy DAC's interfaces, which is
what the recovered blocks expect. When a cDAC API gains an implementation, nothing here needs
to change; when a *new* API is added to a proxied interface, add the corresponding proxy
method following the same shape.
