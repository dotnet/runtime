# Investigating JIT and GC Hole stress

There are two stressing related features for the JIT and JIT generated GC info &ndash; JIT Stress and GC Hole Stress. These features provide a way during development to discover edge cases and more "real world" scenarios without having to develop complex applications.

## JIT Stress (`DEBUG` builds only &ndash; Debug and Checked)

Enabling JIT Stress can be done in several ways. Setting `DOTNET_JitStress` to a non-zero integer value that will generate varying levels of JIT optimizations based on a hash of the method's name or set to a value of two (for example, `DOTNET_JitStress=2`) that will apply all optimizations. Another way to enable JIT Stress is by setting `DOTNET_JitStressModeNamesOnly=1` and then requesting the stress modes, space delimited, in the `DOTNET_JitStressModeNames` variable (for example, `DOTNET_JitStressModeNames=STRESS_USE_CMOV STRESS_64RSLT_MUL STRESS_LCL_FLDS`).

A comprehensive list of stress modes can be found in [`compiler.h`](/src/coreclr/jit/compiler.h) &ndash; search for the `STRESS_MODES` define.

It is often useful to use [JIT Dump](./viewing-jit-dumps.md) in tandem with JIT Stress. Using a JIT Dump file, one can discover which stress modes were applied. An example of how to find an applied stress mode is looking for a statement similar to:

```
*** JitStress: STRESS_NULL_OBJECT_CHECK ***
```

## GC Hole Stress

Enabling GC Hole Stress causes GCs to always occur in specific locations and that helps to track down GC holes. GC Hole Stress can be enabled using the `DOTNET_GCStress` environment variable. It takes a non-zero integer value in hexadecimal format. Note these values can be or'd together (for example, `0x3 = 0x1 | 0x2`).

- **0x1** &ndash; GC on all allocs and 'easy' places.
- **0x2** &ndash; GC on transitions to Preemptive GC.
- **0x4** &ndash; GC on every allowable JITed instr.
- **0x8** &ndash; GC on every allowable R2R instr.
- **0xF** &ndash; GC only on a unique stack trace.

### Common combinations

**0x1 | 0x2** &ndash; 0x3 are "in the VM". Failures in 0x1 or 0x2 can be due to VM-related reasons, like lack of GC reporting/pinning in interop frames.

**0x4 | 0x8** &ndash; 0xC runs GC stress for each JIT generated instruction (either dynamically or AOT, in R2R). Failures in 0x4 or 0x8 typically mean a failure in GC info. Only happens once for any instruction, so can miss failures that only occur on non-first GCs. This mode replaces the target instuction with a with breakpoint instruction and that affects disassembly.
