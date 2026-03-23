# Profiling Design Documentation

This directory contains design documentation for the CLR profiling APIs.

## Documents

- [Profiler Breaking Changes](Profiler%20Breaking%20Changes.md) — record of breaking changes to profiler APIs
- [Profiler Attach on CoreCLR](Profiler%20Attach%20on%20CoreCLR.md) — design for profiler attach support
- [ReJIT on Attach](ReJIT%20on%20Attach.md) — design for enabling ReJIT on profiler attach
- [IL Rewriting Basics](IL%20Rewriting%20Basics.md) — basics of IL rewriting in profilers

## Blog Archive (Historical)

The [davbr-blog-archive](davbr-blog-archive/) directory contains archived blog posts by David Broman,
the original author of many .NET profiler features. These were written for desktop .NET Framework
before .NET Core existed. **The content is largely accurate but may reference outdated APIs or
behaviors.** See the [archive README](davbr-blog-archive/README.md) for details.

## Key Source Files

All paths relative to repository root:

| Component | Header | Implementation |
|-----------|--------|----------------|
| Profiler-to-EE interface | `src/coreclr/vm/proftoeeinterfaceimpl.h` | `src/coreclr/vm/proftoeeinterfaceimpl.cpp` |
| EE-to-profiler callbacks | `src/coreclr/vm/eetoprofinterfaceimpl.h` | `src/coreclr/vm/eetoprofinterfaceimpl.cpp` |
| Profiling enumerators | `src/coreclr/vm/profilingenumerators.h` | `src/coreclr/vm/profilingenumerators.cpp` |
| Profiler detach | `src/coreclr/vm/profdetach.h` | `src/coreclr/vm/profdetach.cpp` |
| ICorProfilerInfo API | `src/coreclr/inc/corprof.idl` | — |
