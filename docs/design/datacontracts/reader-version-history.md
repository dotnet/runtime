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
