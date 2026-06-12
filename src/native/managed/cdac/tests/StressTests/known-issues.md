# cDAC Stack Reference Walking — Known Issues

This document tracks known gaps between the cDAC's stack reference
enumeration and the runtime's own GC root scanning, exposed by the
`cdacstress` framework (`src/coreclr/vm/cdacstress.cpp`).

## Verification verdicts

When running `RunStressTests.ps1` (Checked, `DOTNET_CdacStress=0x001` =
`ALLOC`), each verification is bucketed into one of:

| Verdict | Meaning |
|---------|---------|
| `[PASS]` | cDAC matches the runtime's GC root enumeration. |
| `[KNOWN_ISSUE]` | cDAC and the runtime differ, but every diff is on a Frame the cDAC explicitly marked as deferred (see Bucket 1 below). Not a regression. |
| `[FAIL]` | A real cDAC vs runtime discrepancy, or `GetStackReferences` failed at the API boundary. Investigate. |

The native harness detects the deferred-frame sentinels emitted by the
cDAC managed code and relabels per-frame diffs as `[KNOWN_NIE]`
in the structured log.

## Open buckets

### Bucket 1: `PromoteCallerStack` requires `ICallingConvention`

`GcScanner.PromoteCallerStack` (in
`src/native/managed/cdac/.../Contracts/StackWalk/GC/GcScanner.cs`)
is deliberately stubbed: instead of enumerating the caller's argument
refs it records the frame as deferred and returns. Producing correct
caller-argument layouts requires porting `ArgIterator` behind the
`ICallingConvention` contract, which is a separate deferred work item.

To prevent these deferred frames from masquerading as real cDAC bugs,
the managed code records each deferred frame on the `GcScanContext`
via `RecordDeferredFrame`, which emits a sentinel `StackRefData` entry
with `GcScanFlags.CDAC_DEFERRED_FRAME` (0x40000000) set. The native
stress harness strips these sentinels and re-classifies any RT-only
diff at a deferred Source address as `[KNOWN_NIE]`, and the
whole verification as `[KNOWN_ISSUE]` rather than `[FAIL]`.

Expected pattern in the log:

```
[KNOWN_ISSUE] Thread=0x... IP=0x... cDAC=6 RT=7 frames=5 (match=4 mismatch=0 known_nie=1)
  Frame #4 <frame PrestubMethodFrame 0x...> [KNOWN_NIE] cDAC=0 RT=1 SP_cDAC=0x0 SP_RT=0x0
      [NIE(RT)] Addr=0x... Obj=0x... Flags=0x0 Reg=-1 Off=0
  [STACK_TRACE] (cDAC=6 RT=7 frames=5)
    #0 System.AppContext.Setup(...) (cDAC=2 RT=2)
    ...
    #4 <frame PrestubMethodFrame 0x...> (cDAC=0 RT=1) <-- KNOWN_NIE (PromoteCallerStack deferred)
```

Every JIT frame's count matches exactly; the only discrepancy is on
the explicit transition Frame that `PromoteCallerStack` would scan.

To re-enable: implement `ICallingConvention.PortableArgumentIterator`,
then replace the `RecordDeferredFrame` stub in `PromoteCallerStack` with
a call into the new contract. Once that lands, the previously-tracked
`ELEMENT_TYPE_INTERNAL` (0x21) case in signature decoding will also
need to be handled — that case currently isn't reachable because
`PromoteCallerStack` short-circuits without iterating the signature.

## Future work

- Investigate the GcInfo safe-point bitmap decoding difference for
  QCall frames.
- Replace `fprintf`-based stress logging in `cdacstress.cpp` with a
  more structured mechanism (e.g., ETW events or StressLog) for better
  tooling integration and reduced I/O overhead during stress runs.

## Log Format

Each verification emits a single header line followed by, on `[FAIL]` or
`[KNOWN_ISSUE]`, a per-broken-frame block and a stack trace.

```
[PASS] Thread=0x... IP=0x... cDAC=N RT=N frames=N

[KNOWN_ISSUE] Thread=0x... IP=0x... cDAC=N RT=M frames=N (match=N mismatch=N known_nie=N)
  Frame #i <frame TypeName 0x...> [KNOWN_NIE] cDAC=X RT=Y SP_cDAC=0x... SP_RT=0x...
      [NIE(RT)] Addr=0x... Obj=0x... Flags=0x... Reg=N Off=N
  [STACK_TRACE] (cDAC=N RT=M frames=N)
    #i MethodName (cDAC=X RT=Y)
    #i <frame TypeName 0x...> (cDAC=X RT=Y) <-- KNOWN_NIE (PromoteCallerStack deferred)

[FAIL] Thread=0x... IP=0x... cDAC=N RT=M frames=N (match=N mismatch=N known_nie=N)
  Frame #i MethodName [MISMATCH] cDAC=X RT=Y SP_cDAC=0x... SP_RT=0x...
      [ONLY(cDAC)] Addr=0x... Obj=0x... Flags=0x... Reg=N Off=N
      [ONLY(RT)]   Addr=0x... Obj=0x... Flags=0x... Reg=N Off=N
  [STACK_TRACE] (cDAC=N RT=M frames=N)
    #i MethodName (cDAC=X RT=Y) [<-- MISMATCH]
```

Frames whose counts match are omitted from the per-frame block in
concise mode; verbose mode (`DOTNET_CdacStress=0x201`) also emits the
matched refs.

