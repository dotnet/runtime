# JIT compiler design documentation

This is the index for design and architecture documentation for [RyuJIT](ryujit-overview.md),
the .NET just-in-time compiler. The implementation lives in
[`src/coreclr/jit`](../../../../src/coreclr/jit); design notes, proposals, and internals write-ups
live in this directory.

See also the [Book of the Runtime](../botr/README.md),
the [CLR JIT coding conventions](../../../coding-guidelines/clr-jit-coding-conventions.md),
and [Updating the JIT/EE interface](../../../project/updating-jitinterface.md).

## Start here

- [JIT Compiler Structure (RyuJIT overview)](ryujit-overview.md) — the canonical overview:
  compilation flow, phases, core data structures, HIR/LIR and other IR shapes, phase invariants,
  and how to read a JIT dump.
- [RyuJIT tutorial](ryujit-tutorial.md) — illustrated walkthrough with diagrams and a `PopCount`
  example. Some details are outdated; prefer the overview for current behavior.
- [Porting RyuJIT to different platforms](porting-ryujit.md) — maps source files and phases to
  the things a new target needs (ISA, ABI, GC encoding, debug info).

## IR and compiler data structures

- [RyuJIT IR shapes](ir-shapes.md) — short guide to HIR vs LIR, phase-dependent node restrictions,
  and where the exhaustive `GT_*` table lives.
- [First-class structs](first-class-structs.md) — design for representing and optimizing structs
  (including SIMD types) without forcing them to memory.
- [Morphing of call nodes in RyuJIT](jit-call-morphing.md) — call argument evaluation order,
  temporaries, and `GenTreeCall` shapes.
- [Support for multiple destination registers in call/return nodes](multi-reg-call-nodes.md) —
  ABI-driven multi-register return values.
- [Removing embedded statements from the RyuJIT backend IR](removing-embedded-statements.md) —
  2016 proposal behind rationalization and linearized LIR; historical background, not a
  description of current code.
- [Modeling longs on 32-bit architectures](longs-on-32bit-arch.md) — liveness and register
  allocation model for split 64-bit values.

## Optimizations and analyses

- [De-abstraction and conditional escape analysis](<./DeabstractionAndConditionalEscapeAnalysis.md>)
  — optimizing patterns where objects of several types collaborate (for example, enumerators).
- [Guarded devirtualization](GuardedDevirtualization.md) — proposed optimization and design sketch;
  not necessarily shipped as described.
- [Hot/cold code splitting](hot-cold-splitting.md) — current state of splitting frequently and
  rarely executed code.
- [Inline size estimates](inline-size-estimates.md) — work in progress on estimating the size
  impact of an inline.
- [Proposed plans for inlining](inlining-plans.md) — inlining machinery, legality, and future plans.
- [General runtime async inlining](runtime-async-inlining.md) — preserving runtime async semantics
  when inlining suspending calls.
- [Optimization of heap access in value numbering](<./Optimization%20of%20Heap%20Access%20in%20Value%20Numbering.md>)
  — 2013 design note on heap modeling in value numbering; historical.
- [Object stack allocation](object-stack-allocation.md) — moving heap allocations to the stack
  when the object's lifetime is bounded by the method.
- [Exception handling write-through](eh-writethru.md) — keeping variables live across EH regions
  enregisterable via compiler temporaries backed by stack locals.
- [Finally optimizations](<./finally-optimizations.md>) — how `try/finally` is lowered into local
  functions and funclets.
- [Profile count reconstruction](profile-count-reconstruction.md) — reconstructing block weights
  from edge likelihoods in the flowgraph.

## Backend and target support

- [Linear scan register allocation: design and implementation notes](lsra-detail.md) — LSRA
  phases, data structures, and pre/postconditions; assumes the overview has been read.
- [Improving LSRA throughput](lsra-throughput.md) — notes on inefficiencies and possible
  restructuring of register allocation.
- [LSRA heuristic tuning](lsra-heuristic-tuning.md) — experiments with tuning register-selection
  heuristics using a genetic algorithm.
- [ARM64 JIT frame layout](<./arm64-jit-frame-layout.md>) — frame layout constraints and options;
  written before the implementation and not verified against current code.
- [Passing and returning structs](struct-abi.md) — ABI support for structs and known problems
  with the current implementation.
- [GC write barriers](GC-write-barriers.md) — the JIT's write barrier helpers and where they are
  emitted.
- [GC information encoding for x86](jit-gc-info-x86.md) — old documentation; converted to
  Markdown but not otherwise updated.
- [Stack buffer overflow protection](<./Stack%20Buffer%20Overflow%20Protection.md>) — Guard Stack
  checks emitted by the code generator.
- [Reporting variable location](variabletracking.md) — debug info structures (`siScope`, live
  ranges) and how variable locations are described.
- [WebAssembly overview for JIT](<./WebAssembly%20overview%20for%20JIT.md>) — background on
  WebAssembly concepts relevant to JIT code generation.

## Diagnostics and developer workflows

- [Viewing JIT disassembly and dumps](viewing-jit-dumps.md) — using `DOTNET_JitDump`, disassembly,
  and GC info dumps.
- [Investigating JIT and GC hole stress](investigate-stress.md) — `DOTNET_JitStress` and GC hole
  stress modes.
- [Perf Score](Perf-Score.md) — the JIT's estimate of a method's dynamic execution cost.

## Planning and historical

- [JIT optimizer planning guide](JitOptimizerPlanningGuide.md) — how optimizer investments are
  prioritized and validated.
- [Optimizer codebase status/investments](JitOptimizerTodoAssessment.md) — known areas for
  improvement and their state.
- [Jit Architecture Plan (2009)](<./Jit%20Architecture%20Plan%202009.md>) — excerpt from a
  historical plan (SSA, value numbering, constraint propagation); no guarantee any of it was
  implemented.
