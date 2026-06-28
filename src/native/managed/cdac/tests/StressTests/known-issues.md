# cDAC Stack Reference Walking — Known Issues

This document tracks known gaps between the cDAC's stack reference
enumeration and the runtime's own GC root scanning, exposed by the
`cdacstress` framework (`src/coreclr/vm/cdacstress.cpp`).

## Verification verdicts

When running `RunStressTests.ps1` (Checked, `DOTNET_CdacStress=0x101` =
`ALLOC + GCREFS`), each verification is bucketed into one of:

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

## Known intermittent failure: x86 stress flake (cDAC EnumGcRefs misses callee-saved register refs during EH-rich startup)

Pattern (~20% of full x86 suite runs trigger this on at least one
debuggee):

```
[FAIL] Thread=0x... IP=0x... cDAC=7 RT=45 frames=12 (match=5 mismatch=7 known_nie=0)
  Frame #4 System.RuntimeType.SplitName(...) [MISMATCH] cDAC=0 RT=1 SP_cDAC=0x0 SP_RT=0x0
      [ONLY(RT)] Addr=0x0 Obj=0x... Flags=0x0 Reg=6 Off=0
  Frame #5 System.RuntimeType.GetNestedType(...) [MISMATCH] cDAC=0 RT=3 ...
      [ONLY(RT)] Addr=0x0 Obj=0x... Flags=0x0 Reg=6 Off=0
      ...
  ... continues up through System.Diagnostics.Tracing.EventSource frames
```

Signature: the RT-only refs are concentrated in callee-saved registers
(`Reg=6` ESI, `Reg=7` EDI) on frames whose stack trace runs through
`NativeRuntimeEventSource..cctor()` -> `EventSource.Initialize` ->
`RuntimeType.GetNestedType` -> `RuntimeType.SplitName`. The frames
otherwise unwind cleanly and surrounding frames match.

x86-only -- x64 and arm64 stress runs are consistently clean. This is
not the previously-tracked x64 GC-stress crashes #129545/#129546
(those are completely different crashes in `MethodTable::Validate`
during managed exception unwind).

Investigation so far:
- `HasFrameBeenUnwoundByAnyActiveException` is NOT the cause: across
  ~77k invocations during a reproduced flake it returned `false` every
  time, matching the runtime's behavior.
- `EnumerateLiveSlots` returns 0 slots for the affected frames at the
  cDAC-computed `relativeOffset` (small values like 0x2d / 0x1f / 0x14).
  The runtime sees ESI/EDI live at those frames -- so either cDAC's IP
  differs from the runtime's view of the frame, or our partially-
  interruptible call-site matching has an off-by-one when the trigger
  fires between (rather than exactly at) call sites.

Most likely root cause is one of:
1. For partially-interruptible methods, our `activeCallSite` match
   requires `transition.Offset == instructionOffset` exactly. If the
   IP cDAC reads for a frame mid-EH-dispatch is not the call-site
   return-address offset (e.g., it's the call-instruction offset or
   somewhere mid-instruction), the match fails and no register refs
   are emitted.
2. The runtime tracks callee-saved register values across unwinds via
   REGDISPLAY's `pCallerContext`; cDAC re-unwinds via
   `Context.Clone().Unwind()` each call. A divergent context state
   would produce a divergent IP and thus a divergent live-mask.
3. Some x86-specific frame-type handling difference between cDAC's
   `StackWalk_1` iteration and the runtime's `StackWalk` during the
   EH-dispatch-of-managed-exceptions path.

Resolving this requires a follow-up investigation that compares the
IPs cDAC and the runtime see for these specific frames during a
reproduced flake (e.g., by adding per-frame instruction-pointer logging
to both sides of the stress harness and diffing them). It is x86-only,
flaky, and not gated on this PR.

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
concise mode; verbose mode (`DOTNET_CdacStress=0x10101`) also emits the
matched refs.

