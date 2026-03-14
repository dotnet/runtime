# CoreCLR Design Documentation Index

This index maps topics to their design documents and primary source locations,
to help navigate the CoreCLR codebase. All source paths are relative to the
repository root.

## How to Use This Index

- **"I want to understand X"** → find the topic below, read the linked doc, then explore the source files listed.
- **"I'm changing file Y"** → use the [Source Directory Map](#source-directory-map) to find related design docs.
- **Recommended reading order for newcomers**: start with [Introduction to the CLR](botr/intro-to-clr.md), then [Type System](botr/type-system.md), then the topic relevant to your work.

---

## Topic → Document → Source Files

### Runtime Core (Book of the Runtime)

| Topic | Document | Primary Source Files |
|-------|----------|---------------------|
| CLR overview | [intro-to-clr.md](botr/intro-to-clr.md) | `src/coreclr/vm/ceemain.cpp` (startup) |
| Type system | [type-system.md](botr/type-system.md) | `src/coreclr/vm/methodtable.h`, `class.h`, `typedesc.h`, `typehandle.h` |
| Type loading | [type-loader.md](botr/type-loader.md) | `src/coreclr/vm/clsload.hpp`, `methodtablebuilder.cpp` |
| Method descriptors | [method-descriptor.md](botr/method-descriptor.md) | `src/coreclr/vm/method.hpp`, `precode.h` |
| Virtual stub dispatch | [virtual-stub-dispatch.md](botr/virtual-stub-dispatch.md) | `src/coreclr/vm/virtualcallstub.h` |
| Garbage collection | [garbage-collection.md](botr/garbage-collection.md) | `src/coreclr/gc/gc.cpp`, `gcinterface.h`; `src/coreclr/vm/gchelpers.cpp` |
| Threading | [threading.md](botr/threading.md) | `src/coreclr/vm/threads.h`, `threadsuspend.h` |
| Exceptions (CLR internal) | [exceptions.md](botr/exceptions.md) | `src/coreclr/inc/ex.h`; `src/coreclr/vm/excep.h`, `exceptionhandling.h` |
| Stack walking | [stackwalking.md](botr/stackwalking.md) | `src/coreclr/vm/stackwalk.h`, `frames.h` |
| System.Private.CoreLib | [corelib.md](botr/corelib.md) | `src/coreclr/System.Private.CoreLib/` |
| DAC (diagnostics) | [dac-notes.md](botr/dac-notes.md) | `src/coreclr/debug/daccess/` |
| Profiling | [profiling.md](botr/profiling.md) | `src/coreclr/vm/proftoeeinterfaceimpl.h` |
| Profilability | [profilability.md](botr/profilability.md) | `src/coreclr/vm/proftoeeinterfaceimpl.cpp` |
| ReadyToRun | [readytorun-overview.md](botr/readytorun-overview.md) | `src/coreclr/vm/readytoruninfo.h` |
| ReadyToRun format | [readytorun-format.md](botr/readytorun-format.md) | `src/coreclr/inc/readytorun.h` |
| Shared generics | [shared-generics.md](botr/shared-generics.md) | `src/coreclr/vm/generics.h` |
| Managed type system | [managed-type-system.md](botr/managed-type-system.md) | `src/coreclr/tools/Common/TypeSystem/` |
| CLR ABI | [clr-abi.md](botr/clr-abi.md) | `src/coreclr/jit/codegencommon.cpp`, `src/coreclr/inc/corinfo.h` |
| Vectors and intrinsics | [vectors-and-intrinsics.md](botr/vectors-and-intrinsics.md) | `src/coreclr/jit/hwintrinsic.h` |
| Async codegen | [runtime-async-codegen.md](botr/runtime-async-codegen.md) | `src/coreclr/jit/` |
| Porting guide | [guide-for-porting.md](botr/guide-for-porting.md) | — |
| Logging | [logging.md](botr/logging.md) | `src/coreclr/inc/log.h` |
| ILC architecture (NativeAOT) | [ilc-architecture.md](botr/ilc-architecture.md) | `src/coreclr/tools/aot/ILCompiler.Compiler/` |

### JIT Compiler

| Topic | Document | Primary Source Files |
|-------|----------|---------------------|
| **JIT overview (start here)** | [ryujit-overview.md](jit/ryujit-overview.md) | `src/coreclr/jit/compiler.h`, `ee_il_dll.cpp` |
| JIT tutorial | [ryujit-tutorial.md](jit/ryujit-tutorial.md) | `src/coreclr/jit/` |
| Register allocation (LSRA) | [lsra-detail.md](jit/lsra-detail.md) | `src/coreclr/jit/lsra.h`, `lsra.cpp` |
| LSRA heuristic tuning | [lsra-heuristic-tuning.md](jit/lsra-heuristic-tuning.md) | `src/coreclr/jit/lsra.cpp` |
| LSRA throughput | [lsra-throughput.md](jit/lsra-throughput.md) | `src/coreclr/jit/lsra.cpp` |
| Struct handling | [first-class-structs.md](jit/first-class-structs.md) | `src/coreclr/jit/promotion.cpp`, `morph.cpp` |
| Struct ABI | [struct-abi.md](jit/struct-abi.md) | `src/coreclr/jit/compiler.h` |
| Inlining | [inlining-plans.md](jit/inlining-plans.md) | `src/coreclr/jit/inlinepolicy.h`, `inline.h` |
| Inline size estimates | [inline-size-estimates.md](jit/inline-size-estimates.md) | `src/coreclr/jit/inlinepolicy.cpp` |
| GC write barriers | [GC-write-barriers.md](jit/GC-write-barriers.md) | `src/coreclr/jit/gcinfo.cpp` |
| GC info (x86) | [jit-gc-info-x86.md](jit/jit-gc-info-x86.md) | `src/coreclr/jit/gcinfo.cpp` |
| Viewing JIT dumps | [viewing-jit-dumps.md](jit/viewing-jit-dumps.md) | `src/coreclr/jit/jitconfigvalues.h` |
| Guarded devirtualization | [GuardedDevirtualization.md](jit/GuardedDevirtualization.md) | `src/coreclr/jit/indirectcalltransformer.cpp` |
| Call morphing | [jit-call-morphing.md](jit/jit-call-morphing.md) | `src/coreclr/jit/morph.cpp` |
| Finally optimizations | [finally-optimizations.md](jit/finally-optimizations.md) | `src/coreclr/jit/fgehopt.cpp` |
| EH write-through | [eh-writethru.md](jit/eh-writethru.md) | `src/coreclr/jit/lsra.cpp` |
| Multi-reg call nodes | [multi-reg-call-nodes.md](jit/multi-reg-call-nodes.md) | `src/coreclr/jit/gentree.h` |
| Hot/cold splitting | [hot-cold-splitting.md](jit/hot-cold-splitting.md) | `src/coreclr/jit/flowgraph.cpp` |
| Object stack allocation | [object-stack-allocation.md](jit/object-stack-allocation.md) | `src/coreclr/jit/objectalloc.cpp` |
| Escape analysis | [DeabstractionAndConditionalEscapeAnalysis.md](jit/DeabstractionAndConditionalEscapeAnalysis.md) | `src/coreclr/jit/objectalloc.cpp` |
| Value numbering | [Optimization of Heap Access in Value Numbering.md](jit/Optimization%20of%20Heap%20Access%20in%20Value%20Numbering.md) | `src/coreclr/jit/valuenum.h` |
| Profile count reconstruction | [profile-count-reconstruction.md](jit/profile-count-reconstruction.md) | `src/coreclr/jit/flowgraph.cpp` |
| Perf score | [Perf-Score.md](jit/Perf-Score.md) | `src/coreclr/jit/emitarm64.cpp`, `emitxarch.cpp` |
| Porting RyuJIT | [porting-ryujit.md](jit/porting-ryujit.md) | `src/coreclr/jit/target.h` |
| ARM64 frame layout | [arm64-jit-frame-layout.md](jit/arm64-jit-frame-layout.md) | `src/coreclr/jit/codegenarm64.cpp` |
| Longs on 32-bit | [longs-on-32bit-arch.md](jit/longs-on-32bit-arch.md) | `src/coreclr/jit/decomposelongs.cpp` |
| Variable tracking | [variabletracking.md](jit/variabletracking.md) | `src/coreclr/jit/codegencommon.cpp` |
| Optimizer planning | [JitOptimizerPlanningGuide.md](jit/JitOptimizerPlanningGuide.md) | `src/coreclr/jit/optimizer.cpp` |
| Optimizer TODO | [JitOptimizerTodoAssessment.md](jit/JitOptimizerTodoAssessment.md) | `src/coreclr/jit/optimizer.cpp` |
| Stack buffer overflow protection | [Stack Buffer Overflow Protection.md](jit/Stack%20Buffer%20Overflow%20Protection.md) | `src/coreclr/jit/gschecks.cpp` |
| Stress testing | [investigate-stress.md](jit/investigate-stress.md) | `src/coreclr/jit/jitconfigvalues.h` |
| WASM JIT | [WebAssembly overview for JIT.md](jit/WebAssembly%20overview%20for%20JIT.md) | `src/coreclr/jit/` |

### Profiling

| Topic | Document | Primary Source Files |
|-------|----------|---------------------|
| Profiling overview | [profiling/README.md](profiling/README.md) | `src/coreclr/vm/proftoeeinterfaceimpl.h` |
| Profiler attach | [Profiler Attach on CoreCLR.md](profiling/Profiler%20Attach%20on%20CoreCLR.md) | `src/coreclr/vm/proftoeeinterfaceimpl.cpp` |
| ReJIT on attach | [ReJIT on Attach.md](profiling/ReJIT%20on%20Attach.md) | `src/coreclr/vm/rejit.h` |
| IL rewriting | [IL Rewriting Basics.md](profiling/IL%20Rewriting%20Basics.md) | `src/coreclr/vm/proftoeeinterfaceimpl.cpp` |
| Breaking changes | [Profiler Breaking Changes.md](profiling/Profiler%20Breaking%20Changes.md) | — |
| Blog archive (historical) | [davbr-blog-archive/](profiling/davbr-blog-archive/README.md) | — |

---

## Source Directory Map

Use this to find design docs relevant to source directories you're working in.

| Source Directory | Description | Related Design Docs |
|-----------------|-------------|---------------------|
| `src/coreclr/vm/` | Runtime VM (core execution engine) | [type-system](botr/type-system.md), [type-loader](botr/type-loader.md), [threading](botr/threading.md), [exceptions](botr/exceptions.md), [gc](botr/garbage-collection.md), [profiling](botr/profiling.md) |
| `src/coreclr/jit/` | JIT compiler (RyuJIT) | [ryujit-overview](jit/ryujit-overview.md), [lsra-detail](jit/lsra-detail.md), [first-class-structs](jit/first-class-structs.md), all jit/ docs |
| `src/coreclr/gc/` | Garbage collector | [garbage-collection](botr/garbage-collection.md) |
| `src/coreclr/inc/` | Shared headers (corjit.h, corinfo.h, etc.) | [ryujit-overview](jit/ryujit-overview.md) (JIT/EE interface), [clr-abi](botr/clr-abi.md) |
| `src/coreclr/debug/` | Debugger and DAC | [dac-notes](botr/dac-notes.md) |
| `src/coreclr/pal/` | Platform Abstraction Layer | [guide-for-porting](botr/guide-for-porting.md) |
| `src/coreclr/nativeaot/` | NativeAOT compiler | [ilc-architecture](botr/ilc-architecture.md) |
| `src/coreclr/tools/` | Crossgen2, ILVerify, R2RDump, etc. | [readytorun-overview](botr/readytorun-overview.md) |
| `src/coreclr/System.Private.CoreLib/` | Managed core library | [corelib](botr/corelib.md) |
| `src/coreclr/interpreter/` | IL interpreter | *(no design doc yet)* |

---

## Key Entry Points

These are the most important functions to start from when tracing a subsystem:

| Subsystem | Entry Point | File |
|-----------|-------------|------|
| JIT compilation | `CILJit::compileMethod()` | `src/coreclr/jit/ee_il_dll.cpp` |
| Runtime startup | `EEStartup()` | `src/coreclr/vm/ceemain.cpp` |
| Type loading | `ClassLoader::LoadTypeHandleThrowing()` | `src/coreclr/vm/clsload.cpp` |
| Method table building | `MethodTableBuilder::BuildMethodTable()` | `src/coreclr/vm/methodtablebuilder.cpp` |
| GC allocation | `GCHeapUtilities::GetGCHeap()` | `src/coreclr/vm/gcheaputilities.h` |
| GC collection | `GCHeap::GarbageCollect()` | `src/coreclr/gc/gc.cpp` |
| Thread creation | `SetupThread()` | `src/coreclr/vm/threads.cpp` |
| Exception throw | `COMPlusThrow()` | `src/coreclr/vm/excep.h` |
| Stack walk | `Thread::StackWalkFramesEx()` | `src/coreclr/vm/stackwalk.cpp` |
| Virtual dispatch | `VirtualCallStubManager::ResolveWorkerStatic()` | `src/coreclr/vm/virtualcallstub.cpp` |
