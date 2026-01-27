# Tracking Issue: Tail-call stepping correctness in ReadyToRun assemblies

## Description

The `IsTailCall` function in `src/coreclr/debug/ee/controller.cpp` assumes that given a call IP, we can immediately determine the destination `MethodDesc` that will be invoked. However, this assumption breaks for cross ReadyToRun (R2R) image tail calls.

Specifically, if ReadyToRun image A has method `Foo1()` which tail-calls to `Foo2()` in image B, then `Foo1()` may have a call to one of the tail-call helpers at the end that is indirected through an `ExternalMethodFixup`. The `StubManager` for `ExternalMethodFixup` does not immediately figure out the destination `MethodDesc` - it uses callbacks that pass content to `TraceManager()` to determine where execution will go next.

This means the debugger may not correctly handle stepping through tail calls that cross ReadyToRun image boundaries.

## Background

This issue was discovered as part of PR #123640, which fixed an access violation (AV) in `IsTailCall` when `trace.GetAddress()` returns NULL. The AV was fixed by adding a NULL check before calling `GetNativeCodeMethodDesc()`, but the underlying stepping correctness issue remains.

Related changes:
- PR #108942 introduced `TRACE_MULTICAST_DELEGATE_HELPER` trace type
- PR #108414 introduced `TRACE_EXTERNAL_METHOD_FIXUP` trace type

These trace types set `address = NULL` by design because:
1. `TRACE_UNJITTED_METHOD` - The `MethodDesc` field describes a method that will be jitted and executed in the future, code address not yet known.
2. `TRACE_MULTICAST_DELEGATE_HELPER` and `TRACE_EXTERNAL_METHOD_FIXUP` - A callback can be turned on and the content of that callback can be passed to `TraceManager()` to determine where execution will go next.
3. Other trace types - The address is some IP that will be executed in the future where a breakpoint can be placed and then further actions taken from there.

## Current Behavior

When stepping through a tail call that crosses R2R image boundaries (via `ExternalMethodFixup`), the debugger may not correctly step into the target method because `IsTailCall` cannot determine the destination `MethodDesc` at the time of the call.

## Expected Behavior

The debugger should correctly step through tail calls regardless of whether they cross ReadyToRun image boundaries.

## Affected Code

- `src/coreclr/debug/ee/controller.cpp` - `IsTailCall` function (lines ~5820-5860)
- `src/coreclr/vm/stubmgr.cpp` - `ExternalMethodFixupStubManager`

## Potential Solutions

This will require redesigning how `IsTailCall` determines the target method for calls that go through `ExternalMethodFixup` stubs. Possible approaches include:
1. Enhancing the `StubManager` to provide target method information for `ExternalMethodFixup`
2. Using the callback mechanism (`TraceManager()`) within `IsTailCall` to resolve the target
3. Special-casing R2R cross-image tail calls in the stepping logic

## References

- PR #123640 - Fix AV in IsTailCall by checking for NULL trace address
- PR #108942 - Introduced `TRACE_MULTICAST_DELEGATE_HELPER` trace type
- PR #108414 - Introduced `TRACE_EXTERNAL_METHOD_FIXUP` trace type
- [Comment discussion](https://github.com/dotnet/runtime/pull/123640#issuecomment-3803767807)

## Labels

- `area-Diagnostics-coreclr`
- `bug`

## Assignees

cc @noahfalk @lateralusX @rcj1
