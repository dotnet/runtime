# Investigating JIT and GC stress

There are two stressing related features for the JIT and JIT generated GC info &ndash; JIT Stress and GC Stress. These features provide a way for the development to discover edge cases and more "real world" scenarios without having to develop complex applications.

## JIT Stress (Debug builds only)

Enabling JIT Stress can be done in several ways. Setting `COMPlus_JitStress` to a non-zero integer value that will generate varying levels of JIT optimizations based on a hash of the methods name or set to a value of two (for example, `COMPlus_JitStress=2`) that will apply all optimizations. Another way to enable enable JIT Stress is by setting `COMPlus_JitStressModeNamesOnly=1` and then requesting the stress modes, space delimited, in the `COMPlus_JitStressModeNames` variable (for example, `COMPlus_JitStressModeNames=STRESS_USE_CMOV STRESS_64RSLT_MUL STRESS_LCL_FLDS`).

It is often useful to use [JIT Dump](./viewing-jit-dumps.md) in tandem with JIT Stress.

## GC Stress

Enabling GC Stress causes GCs to always occur in specific locations. GC Stress can be enabled using the `DOTNET_GCStress` environment variable. It takes a non-zero integer value in hexadecimal format. Note these values can be or'd together (for example, `0x3 = 0x1 | 0x2`).

- **0x1** &ndash; GC on all allocs and 'easy' places.
- **0x2** &ndash; GC on transitions to Preemptive GC.
- **0x4** &ndash; GC on every allowable JITed instr.
- **0x8** &ndash; GC on every allowable NGEN instr.
- **0xF** &ndash; GC only on a unique stack trace.
