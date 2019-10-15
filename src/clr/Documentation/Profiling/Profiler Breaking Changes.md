# Profiler Breaking Changes #

Over time we will need to modify the Profiler APIs, this document will serve as a record of any breaking changes.

1. Code Versioning introduced changes documented [here](../design-docs/code-versioning-profiler-breaking-changes.md)
2. The work to allow adding new types and methods after module load means ICorProfilerInfo7::ApplyMetadata will now potentially trigger a GC, and will not be callable in situations where a GC can not happen (for example  ICorProfilerCallback::RootReferences).
3. As part of the work to allow ReJIT on attach ReJITted methods will no longer be inlined (ever). Since the inlining is blocked there won't be a `ICorProfilerCallback::JITInlining` callback.