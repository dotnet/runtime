# Jump Stubs

## Overview

Relative calls and jumps reduce code size, but have a limited reach.
On x64, a direct call or jump with a `rel32` displacement occupies
5 bytes and can reach a target within a signed 32-bit displacement. Other
architectures have similar restrictions.

When a call or branch code target is outside the instruction's range,
CoreCLR can allocate a jump stub within reach of the source instruction.
The source branches to the stub, which transfers control to the full
target address. Data references cannot use jump stubs; they require an
addressing sequence that can reach the data.

If a required jump stub cannot be allocated within range, the code
containing the branch must be discarded and generated again.

## Jump stub creation and management

On x64, a jump stub occupies 12 bytes:

```asm
mov rax, <8-byte address>
jmp rax
```

The stub clobbers RAX, which is volatile under both the Windows and
System V x64 ABIs. It is suitable only for transfers whose calling
convention permits this register to be clobbered. Other architectures
use their own stub encodings and scratch registers.

On x64, `rel32UsingJumpStub()` computes the `rel32` displacement to
the target. If the displacement does not fit, it computes the range
in which a jump stub can be reached and calls `ExecutionManager::jumpStub()`.

`ExecutionManager::jumpStub()` searches a cache for a stub with the
requested target and within the allowed address range. If there is no
matching stub, `ExecutionManager::getNextJumpStub()` finds space in an
existing jump-stub block or allocates a new block.

Normal methods share a jump-stub cache associated with their
`LoaderAllocator`, and their stubs are allocated from a `LoaderCodeHeap`.
Dynamic methods use a per-method cache and `HostCodeHeap` so their
stubs can be reclaimed with the method.

If it is not possible to allocate a jump-stub block within a suitable
range, jump-stub allocation fails. Callers must handle the failure
gracefully by retrying code generation, potentially with a different
strategy.

## Jump stubs and the JIT

The JIT requests code and data buffers through `ICorJitInfo::allocMem()`.
After the addresses are known, it reports relocations through
`ICorJitInfo::recordRelocation()`. The runtime resolves the relocations
and patches the generated code.

On x64, optional relative relocations allow the JIT to use
`rel32` relocations for both data references and direct code targets.
If such a relocation is out of range, the runtime cannot assume it is safe to
insert a jump stub: the reference might be to data. It marks the
compilation for retry and disables optional relative data relocations
for subsequent compilations in the process. The retried compilation uses
full addresses for those data references. A potential improvement is
to distinguish data references, external code targets, and intra-method
transfers in the relocation information passed from the JIT to the VM.
This would allow code references to use jump stubs independently of
data relocation overflow.

Direct calls can still use relative branches. When an out-of-range code
target requires a jump stub and allocation fails, the runtime discards
the generated code and recompiles the method.

Relocation handling accumulates a conservative estimate of the space
needed for jump stubs. The retry passes this estimate as
`reserveForJumpStubs` in the method's code allocation request, leaving
room for subsequent jump stub allocations. This increases the likelihood
that the retry can allocate the required jump stubs within range; it is not
a guarantee of success.

Other architectures where jump stubs are implemented use variants of this
scheme. The relocation types, branch ranges, and addressing sequences are
architecture-specific.

## ReadyToRun dynamic helpers

x64 ReadyToRun dynamic helpers can contain a `rel32` jump to a runtime
target. They use `rel32UsingJumpStub()` to resolve the jump, requesting
a jump stub if necessary.

If the jump cannot be resolved, the runtime allocates another helper
buffer and emits the whole helper again. Failed buffers are retained
in the dynamic-helper heap, ensuring that the next attempt uses a
different address. The new address is likely to allow a jump stub to
be allocated within range. Only a successfully emitted helper is
returned to the caller.

## Stress modes

`DOTNET_ForceRelocs=1` exercises relocation and code-generation retry
paths. Jump-stub allocation fails with a 10% probability when a new
stub is requested, exercising allocation-failure handling for both JIT
code and ReadyToRun dynamic helpers. A matching cached stub can still
be reused without entering this failure-injection path.

On x64, this mode also forces `rel32UsingJumpStub()` to request a jump
stub even when the target is within range. JIT relocations are not all
unconditionally routed through stubs: they include data references and
intra-method transfers that cannot tolerate the stub's register
clobber.
