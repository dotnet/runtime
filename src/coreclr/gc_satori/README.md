# Satori Garbage Collector #

A simple garbage collector that incorporates various ideas that I had over time. 

### Short term goals: ###
- auto-tuning, auto-scaling, mostly “knobless” design.
- avoid long pauses when user threads are not making progress.  

### Supported Features: ###

- [x] All features expected in a fully functional Garbage Collector: 
  - Mark-and-sweep,
  - Internal pointers,
  - Premortem Finalization, 
  - Weak References, 
  - Dependent Handles, 
  - Unloadable types, 
  -  ... etc...
- [x] Generational GC.
   - younger generations can be collected without touching older ones
- [x] Compacting GC.
   - can optionally relocate objects to reduce fragmentation.
- [x] Concurrent GC. 
   - all major phases, except optional relocation, can be done concurrently with user code.
- [x] Parallel GC
  - GC can employ multiple threads as needed.
- [x] Thread-local GC.
  - generation 0 GC is an inline thread-local GC and does not stop other threads.
- [x] Pacing GC.
  - allocating threads help with concurrent GC to ensure allocations are not getting ahead.
- [x] Precise and Conservative modes
  - supports precise and conservative stack root reporting.
- [x] Low-latency mode.
  - "nearly pauseless" mode when blocking phases sensitive to the heap size are turned off.
- [x] Trimming of committed set.
  - lazy return of unused memory to the OS.

### Supported Platforms: ### 
|         | x64                 | arm64               |
| --------| ------------------- | ------------------- |
| Windows | <ul><li>- [x] </li> | <ul><li>- [x] </li> |
| Linux   | <ul><li>- [x] </li> | <ul><li>- [x] </li> |
| macOS   | <ul><li>- [x] </li> | <ul><li>- [x] </li> |

### Roadmap: ###
- [ ]  explicit memory limits
- [x]  immortal allocations
- [x]  preallocated objects
- [ ]  perf tuning (possibly a lot of opportunities)
- [ ]  more and better diagnostics (support for debuggers and profilers)
- [ ]  NUMA awareness

