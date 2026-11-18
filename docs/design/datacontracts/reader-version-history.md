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

Added support for cached and virtual ReadyToRun argument maps:

- Prestub and call-counting frames use `GCRefMap` for GC root enumeration when available.
- On x86, these frames also use the cached map's stack-pop count when unwinding.
- Readers retain signature-based scanning when no map is cached or the field is absent.

- `StackWalk` version `c2` decodes `THIS_REF` and `THIS_BYREF`, which encode the
  call-site receiver kind without a separate bit. `VASIG_COOKIE` moves from 5 to 7;
  version `c1` targets retain the old value.
- Supported ReadyToRun images have major version 33 or newer, so all instance import maps
  use contextual receiver tokens. Declaration lookup checks method-instantiation and explicit-context
  eligibility from metadata, without decoding maps or maintaining per-map format flags.
- Prestub, call-counting, and stub-dispatch frames can reuse same-slot declaration maps,
  including overriding declarations, class MethodImpl aliases, and interface implementations
  on the actual receiver.
- Readers also consume maps memoized by native lookup into an existing `MethodDescCodeData`;
  diagnostic lookup remains read-only.
- Dispatch-map matching uses method-definition and canonical-owner identity, rather than
  equating unrelated inherited nonvirtual and derived virtual slots with the same ordinal.
- GC root enumeration and x86 stack-pop unwinding use the same selected map.
- `MethodTable.InterfaceMap` and the size of `MethodTableAuxiliaryData` describe the loaded
  dispatch metadata needed for lookup. Version `c2` uses the new dispatch-map header and
  optional class alias prefix; version `c1` retains its previous algorithm.

Updating the reader is recommended because ReadyToRun transitions may no longer have signature
value-type layouts loaded. Such targets publish `StackWalk` version `c2`, so older readers
cannot silently use the signature-only algorithm when signature layouts have not been loaded.
Version 3 readers continue to support `c1` targets, where the cached-map field may be absent.
