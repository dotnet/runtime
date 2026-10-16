# cDAC reader version history

The cDAC reader version is an advisory value used to indicate when updating a diagnostic tool's
reader is recommended. It is independent of individual data contract versions and does not imply
that an older reader can no longer inspect the runtime.

The runtime publishes its recommended reader version through the `RecommendedReaderVersion`
global. A reader reports the functionality it understands through
`IRuntimeInfo.GetCurrentReaderVersion()`. A tool can recommend an update when the runtime's
recommended version is greater than the reader's current version.

## Version 1

Initial reader version.

## Version 2

Added support for discovering the interpreter JIT manager and enumerating its code heaps:

- CoreCLR optionally publishes `InterpreterJitManagerAddress` when built with interpreter support.
- `IExecutionManager.GetJitManagerInfo(JitManagerKind)` returns information for either the EE JIT
  manager or the interpreter JIT manager.
- `IExecutionManager.GetCodeHeapInfos(JitManagerKind)` enumerates only the heaps owned by the
  selected manager.

This is an additive data-descriptor change. Version 1 readers ignore the new global and continue
to enumerate EE JIT code heaps. Version 2 readers tolerate the interpreter global being absent and
report no interpreter manager or interpreter code heaps in that case.

## Version 3

Added support for cached ReadyToRun argument maps on `MethodDescCodeData`:

- Prestub and call-counting frames use `GCRefMap` for GC root enumeration when available.
- On x86, these frames also use the cached map's stack-pop count when unwinding.
- Readers retain signature-based scanning when no map is cached or the field is absent.

Updating the reader is recommended because methods reached through ReadyToRun imports no longer
need their signature value types loaded solely for these frame transitions.
Such targets publish `StackWalk` version `c2`, so older readers cannot silently use the
signature-only algorithm when signature layouts have not been loaded. Version 3 readers also
support `c1` targets, where the cached-map field may be absent.
