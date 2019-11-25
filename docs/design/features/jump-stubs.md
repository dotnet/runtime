# Jump Stubs

## Overview

On 64-bit platforms (AMD64 (x64) and ARM64), we have a 64-bit address
space. When the CLR formulates code and data addresses, it generally
uses short (<64 bit) relative addresses, and attempts to pack all code
and data relatively close together at runtime, to reduce code size. For
example, on x64, the JIT generates 32-bit relative call instruction
sequences, which can refer to a target address +/- 2GB from the source
address, and which are 5 bytes in size: 1 byte for opcode and 4 bytes
for a 32-bit IP-relative offset (called a rel32 offset). A call sequence
with a full 64-bit target address requires 12 bytes, and in addition
requires a register. Jumps have the same characteristics as calls: there
are rel32 jumps as well.

In case the short relative address is insufficient to address the target
from the source address, we have two options: (1) for data, we must
generate full 64-bit sized addresses, (2) for code, we insert a "jump
stub", so the short relative call or jump targets a "jump stub" which
then jumps directly to the target using a full 64-bit address (and
trashes a register to load that address). Since calls are so common, and
the need for full 64-bit call sequences so rare, using this design
drastically improves code size. The need for jump stubs only arises when
jumps of greater than 2GB range (on x64; 128MB on arm64) are required.
This only happens when the amount of code in a process is very large,
such that all the related code can't be packed tightly together, or the
address space is otherwise tightly packed in the range where code is
normally allocated, once again preventing from packing code together.

An important issue arises, though: these jump stubs themselves must be
allocated within short relative range of the small call or jump
instruction. If that doesn't occur, we encounter a fatal error
condition, if we have no way for the already generated instruction to
reach its intended target.

ARM64 has a similar issue: it has a 28-bit relative branch that is the
preferred branch instruction. The JIT always generates this instruction,
and requires the VM to generate jump stubs if required. However, the VM
does not use this form in any of its stubs; it always uses large form
branches. The remainder of this document will only describe the AMD64
case.

This document will describe the design and implementation of jump stubs,
their various users, the design of their allocation, and how we can
address the problem of failure to allocate required jump stubs (which in
this document I call "mitigation"), for each case.

## Jump stub creation and management

A jump stub looks like this:
```
mov rax, <8-byte address>
jmp rax
```

It is 12 bytes in size. Note that it trashes the RAX register. Since it
is normally used to interpose on a call instruction, and RAX is a
callee-trashed (volatile) register for amd64 (for both Windows and Linux
/ System V ABI), this is not a problem. For calls with custom calling
conventions, like profiler hooks, the VM is careful not to use jump
stubs that might interfere with those conventions.

Jump stub creation goes through the function `rel32UsingJumpStub()`. It
takes the rel32 data address, the target address, and computes the
offset from the source to the target address, and returns this offset.
Note that the source, or "base", address is the address of the rel32
data plus 4 bytes, which it assumes due to the rules of the x86/x64
instruction set which state that the "base" address for computing a
branch offset is the instruction pointer value, or address, of the
following instruction, which is the rel32 address plus 4.

If the offset doesn't fit, it computes the allowed address range (e.g.,
[low ... high]) where a jump stub must be located to create a legal
rel32 offset, and calls `ExecutionManager::jumpStub()` to create or find
an appropriate jump stub.

Jump stubs are allocated in the loader heap associated with a particular
use: either the `LoaderCodeHeap` for normal code, or the `HostCodeHeap`
for DynamicMethod / LCG functions. Dynamic methods cannot share jump
stubs, to support unloading individual methods and reclaiming their
memory. For normal code, jump stubs are reused. In fact, we maintain a
hash table mapping from jump stub target to the jump stub itself, and
look up in this table to find a jump stub to reuse.

In case there is no space left for a jump stub in any existing code heap
in the correct range, a new code heap is attempted to be created in the
range required by the new jump stub, using the function
`ClrVirtualAllocWithinRange()`. This function walks the acceptable address
space range, using OS virtual memory query/allocation APIs, to find and
allocate a new block of memory in the acceptable range. If this function
can't find and allocate space in the required range, we have, on AMD64,
one more fallback: if an emergency jump stub reserve was created using
the `COMPlus_NGenReserveForjumpStubs` configuration (see below), we
attempt to find an appropriate, in range, allocation from that emergency
pool. If all attempts fail to create an allocation in the appropriate
range, we encounter a fatal error (and tear down the process), with a
distinguished "out of memory within range" message (using the
`ThrowOutOfMemoryWithinRange()` function).

## Jump stub allocation failure mitigation

Several strategies have already been created to attempt to lessen the
occurrence of jump stub allocation failure. The following CLR
configuration variables are relevant (these can be set in the registry
as well as the environment, as usual):

* `COMPlus_CodeHeapReserveForJumpStubs`. This value specifies a percentage
of every code heap to reserve for jump stubs. When a non-jump stub
allocation in the code heap would eat into the reserved percentage, a
new code heap is allocated instead, leaving some buffer in the existing
code heap. The default value is 2.
* `COMPlus_NGenReserveForjumpStubs`. This value, when non-zero, creates an
"emergency jump stub reserve". For each NGEN image loaded, an emergency
jump stub reserve space is calculated by multiplying this number, as a
percentage, against the loaded native image size. This amount of space
is allocated, within rel32 range of the NGEN image. An allocation
granularity for these emergency code heaps exceeds the specific
requirement, but multiple NGEN images can share the same jump stub
emergency space heap if it is in range. If an emergency jump stub space
can't be allocated, the failure is ignored (hopefully in this case any
required jump stub will be able to be allocated somewhere else). When
looking to allocate jump stubs, the normal mechanisms for finding jump
stub space are followed, and only if they fail to find appropriate space
are the emergency jump stub reserve heaps tried. The default value is
zero.
* `COMPlus_BreakOnOutOfMemoryWithinRange`. When set to 1, this breaks into
the debugger when the specific jump stub allocation failure condition
occurs.

The `COMPlus_NGenReserveForjumpStubs` mitigation is described publicly
here:
https://support.microsoft.com/en-us/help/3152158/out-of-memory-exception-in-a-managed-application-that-s-running-on-the-64-bit-.net-framework.
(It also mentions, in passing, `COMPlus_CodeHeapReserveForJumpStubs`, but
only to say not to use it.)

## Jump stubs and the JIT

As the JIT generates code on AMD64, it starts by generating all data and
code addresses as rel32 IP-relative offsets. At the end of code
generation, the JIT determines how much code will be generated, and
requests buffers from the VM to hold the generated artifacts: a buffer
for the "hot" code, a buffer for the "cold" code (only used in the case
of hot/cold splitting during NGEN), and a buffer for the read-only data
(see `ICorJitInfo::allocMem()`). The VM finds allocation space in either
existing code heaps, or in newly created code heaps, to satisfy this
request. It is only at this point that the actual addresses where the
generated code will live is known. Note that the JIT has finalized the
exact generated code sequences in the function before calling
`allocMem()`. Then, the JIT issues (or "emits") the generated instruction
bytes into the provided buffers, as well as telling the VM about
exception handling ranges, GC information, and debug information.
When the JIT emits an instruction that includes a rel32 offset (as well
as for other cases of global pointer references), it calls the VM
function `ICorJitInfo::recordRelocation()` to tell the VM the address of
the rel32 data and the intended target address of the rel32 offset. How
this is handled in the VM depends on whether we are JIT-compiling, or
compiling for NGEN.

For JIT compilation, the function `CEEJitInfo::recordRelocation()`
determines the actual rel32 value to use, and fills in the rel32 data in
the generated code buffer. However, what if the offset doesn't fit in a
32-bit rel32 space?

Up to this point, the VM has allowed the JIT to always generate rel32
addresses. It is allowed by the JIT calling
`ICorJitInfo::getRelocTypeHint()`. If this function returns
`IMAGE_REL_BASED_REL32`, then the JIT generates a rel32 address. The first
time in the lifetime of the process when recordRelocation() fails to
compute an offset that fits in a rel32 space, the VM aborts the
compilation, and restarts it in a mode where
`ICorJitInfo::getRelocTypeHint()` never returns `IMAGE_REL_BASED_REL32`.
That is, the VM never allows the JIT to generate rel32 addresses. This
is "rel32 overflow" mode. However, this restriction only applies to data
addresses. The JIT will then load up full 64-bit data addresses in the
code (which are also subject to relocation), and use those. These 64-bit
data addresses are guaranteed to reach the entire address space.

The JIT continues to generate rel32 addresses for call instructions.
After the process is in rel32 overflow mode, if the VM gets a
`ICorJitInfo::recordRelocation()` that overflows rel32 space, it assumes
the rel32 address is for a call instruction, and it attempts to build a
jump stub, and patch the rel32 with the offset to the generated jump
stub.

Note that in rel32 overflow mode, most call instructions are likely to
still reach their intended target with a rel32 offset, so jump stubs are
not expected to be required in most cases.

If this attempt to create a jump stub fails, then the generated code
cannot be used, and the VM restarts the compilation with reserving
extra space in the code heap for jump stubs. The reserved extra space
ensures that the retry succeeds with high probability.

There are several problems with this system:
1. Because the VM doesn't know whether a `IMAGE_REL_BASED_REL32`
relocation is for data or for code, in the normal case (before "rel32
overflow" mode), it assumes the worst, that it is for data. It's
possible that if all rel32 data accesses fit, and only code offsets
don't fit, and the VM could distinguish between code and data
references, that we could generate jump stubs for the too-large code
offsets, and never go into "rel32 overflow" mode that leads to
generating 64-bit data addresses.
2. We can't stress jump stub creation functionality for JIT-generated
code because the JIT generates `IMAGE_REL_BASED_REL32` relocations for
intra-function jumps and calls that it expects and, in fact, requires,
not be replaced with jump stubs, because it doesn't expect the register
used by jump stubs (RAX) to be trashed.

In the NGEN case, rel32 calls are guaranteed to always reach, as PE
image files are limited to 2GB in size, meaning a rel32 offset is
sufficient to reach from any location in the image to any other
location. In addition, all control transfers to locations outside the
image go through indirection stubs. These stubs themselves might require
jump stubs, as described later.

### Failure mitigation

There are several possible alternative mitigations for JIT failure to
allocate jump stubs.
1. When we get into "rel32 overflow" mode, the JIT could always generate
large calls, and never generate rel32 offsets. This is obviously
somewhat expensive, as every external call, such as every call to a JIT
helper, would increase from 5 to 12 bytes. Since it would only occur
once you are in "rel32 overflow" mode, you already know that the process
is quite large, so this is perhaps justifiable, though also perhaps
could be optimized somewhat. This is very simple to implement.
2. Note that you get into "rel32 overflow" mode even for data addresses.
It would be useful to verify that the need for large data addresses
doesn't happen much more frequently than large code addresses.
3. An alternative is to have two separate overflow modes: "data rel32
overflow" and "code rel32 overflow", as follows:
   1. "data rel32 overflow" is entered by not being able to generate a
      rel32 offset for a data address. Restart the compile, and all subsequent
      data addresses will be large.
   2. "code rel32 overflow" is entered by not being able to generate a
      rel32 offset or jump stub for a code address. Restart the compile, and
      all subsequent external call/jump sequences will be large.
      These could be independent, which would require distinguishing code and
      data rel32 to the VM (which might be useful for other reasons, such as
      enabling better stress modes). Or, we could layer them: "data rel32
      overflow" would be the current "rel32 overflow" we have today, which we
      must enter before attempting to generate a jump stub. If a jump stub
      fails to be created, we fail and retry the compilation again, enter
      "code rel32 overflow" mode, and all subsequent code (and data) addresses
      would be large. We would need to add the ability to communicate this new
      mode from the VM to the JIT, implement large call/jump generation in the
      JIT, and implement another type of retry in the VM.
4. Another alternative: The JIT could determine the total number of
unique external call/jump targets from a function, and report that to
the VM. Jump stub space for exactly this number would be allocated,
perhaps along with the function itself (such as at the end), and only if
we are in a "rel32 overflow" mode. Any jump stub required would come
from this space (and identical targets would share the same jump stub;
note that sharing is optional). Since jump stubs would not be shared
between functions, this requires more space than the current jump stub
system but would be guaranteed to work and would only kick in when we
are already experiencing large system behavior.

## Other jump stub creation paths

The VM has several other locations that dynamically generate code or
patch previously generated code, not related to the JIT generating code.
These also must use the jump stub mechanism to possibly create jump
stubs for large distance jumps. The following sections describe these
cases.

## ReJIT

ReJIT is a CLR profiler feature, currently only implemented for x86 and
amd64, that allows a profiler to request a function be re-compiled with
different IL, given by the profiler, and have that newly compiled code
be used instead of the originally compiled IL. This happens within a
live process. A single function can be ReJIT compiled more than once,
and in fact, any number of times. The VM currently implements the
transfer of control to the ReJIT compiled function by replacing the
first five bytes of the generated code of the original function with a
"jmp rel32" to the newly generated code. Call this the "jump patch"
space. One fundamental requirement for this to work is that every
function (a) be at least 5 bytes long, and (b) the first 5 bytes of a
function (except the first, which is the address of the function itself)
can't be the target of any branch. (As an implementation detail, the JIT
currently pads the function prolog out to 5 bytes with NOP instructions,
if required, even if there is enough code following the prolog to
satisfy the 5-byte requirement if those non-prolog bytes are also not
branch targets.)

If the newly ReJIT generated code is at an address that doesn't fit in a
rel32 in the "jmp rel32" patch, then a jump stub is created.

The JIT only creates the required jump patch space if the
`CORJIT_FLG_PROF_REJIT_NOPS` flag is passed to the JIT. For dynamic
compilation, this flag is only passed if a profiler is attached and has
also requested ReJIT services. Note that currently, to enable ReJIT, the
profiler must be present from process launch, and must opt-in to enable
ReJIT at process launch, meaning that all JIT generated functions will
have the jump patch space under these conditions. There will never be a
mix of functions with and without jump patch space in the process if a
profiler has enabled ReJIT. A desirable future state from the profiler
perspective would be to support profiler attach-to-process and ReJIT
(with function swapping) at any time thereafter. This goal may or may
not be achieved via the jump stamp space design.

All NGEN and Ready2Run images are currently built with the
`CORJIT_FLG_PROF_REJIT_NOPS` flag set, to always enable ReJIT using native
images.

A single function can be ReJIT compiled many times. Only the last ReJIT
generated function can be active; the previous compilations consume
address space in the process, but are not collected until the AppDomain
unloads. Each ReJIT event must update the "jmp rel32" patch to point to
the new function, and thus each ReJIT event might require a new jump
stub.

If a situation arises where a single function is ReJIT compiled many
times, and each time requires a new jump stub, it's possible that all
jump stub space near the original function can be consumed simply by the
"leaked" jump stubs created by all the ReJIT compilations for a single
function. The "leaked" ReJIT compiled functions (since they aren't
collected until AppDomain unload) also make it more likely that "close"
code heap address space gets filled up.

### Failure mitigation

A simple mitigation would be to increase the size of the required
function jump patch space from 5 to 12 bytes. This is a two line change
in the `CodeGen::genPrologPadForReJit()` function in the JIT. However,
this would increase the size of all NGEN and Ready2Run images. Note that
many managed code functions are very small, with very small prologs, so
this could significantly impact code size (the change could easily be
measured). For JIT-generated code, where the additional size would only
be added once a profiler has enabled ReJIT, it seems like the additional
code size would be easily justified.

Note that a function has at most one active ReJIT companion function.
When that ReJIT function is no longer used (and thus never again used),
the associated jump stub is also "leaked", and never used again. We
could reserve space for a single jump stub for each function, to be used
by ReJIT, and then, if a jump stub is required for ReJIT, always use
that space. The JIT could pad the function end by 12 bytes when the
`CORJIT_FLG_PROF_REJIT_NOPS` flag is passed, and the ReJIT patching code
could use this reserved space any time it required a jump stub. This
would require 12 bytes extra bytes to be allocated for every function
generated when the `CORJIT_FLG_PROF_REJIT_NOPS` flag is passed. These 12
bytes could also be allocated at the end of the code heap, in the
address space, but not in the normal working set.

For NGEN and Ready2Run, this would require 12 bytes for every function
in the image. This is quite a bit more space than the suggested
mitigation of increasing prolog padding to 12 bytes but only if
necessary (meaning, only if they aren't already 12 bytes in size).
Alternatively, NGEN could allocate this space itself in the native
image, putting it in some distant jump stub data area or section that
would be guaranteed to be within range (due to the 2GB PE file size
limitation) but wouldn't consume physical memory unless needed. This
option would require more complex logic to allocate and find the
associated jump stub during ReJIT. This would be similar to the JIT
case, above, of reserving the jump stub in a distant portion of the code
heap.

## NGEN

NGEN images are built with several tables of code addresses that must be
patched when the NGEN image is loaded.

### CLR Helpers

During NGEN, the JIT generates either direct or indirect calls to CLR
helpers. Most are direct calls. When NGEN constructs the PE file, it
causes these all to branch to (or through, in the case of indirect
calls) the helper table. When a native image is loaded, it replaces the
helper number in the table with a 5-byte "jmp rel32" sequence. If the
rel32 doesn't fit, a jump stub is created. Note that each helper table
entry is allocated with 8 bytes (only 5 are needed for "jmp rel32", but
presumably 8 bytes are reserved to improve alignment.)

The code for filling out the helper table is `Module::LoadHelperTable()`.

#### Failure mitigation

A simple fix is to change NGEN to reserve 12 bytes for each direct call
table entry, to accommodate the 12-byte jump stub sequence. A 5-byte
"jmp rel32" sequence could still be used, if it fits, but the full 12
bytes would be used if necessary.

There are fewer than 200 helpers, so a maximum additional overhead would
be about `200 * (12 - 8) = 800` bytes. That is by far a worst-case
scenario. Mscorlib.ni.dll itself has 72 entries in the helper table.
System.XML.ni.dll has 51 entries, which would lead to 288 and 204 bytes
of additional space, out of 34MB and 12MB total NI file size,
respectively.

An alternative is to change all helper calls in NGEN to be indirect:
```
call [rel32]
```
where the [rel32] offset points to an 8-byte address stored in the
helper table. This method is already used by exactly one helper on
AMD64: `CORINFO_HELP_STOP_FOR_GC`, in particular because this helper
doesn't allow us to trash RAX, as required by jump stubs.
Similarly, Ready2Run images use:
```
call [rel32]
```
for "hot" helpers and:
```
call [rel32]
```
to a shared:
```
jmp [rel32]
```
for cold helpers. We could change NGEN to use the Ready2Run scheme.

Alternatively, we might handle all NGEN jump stub issues by reserving a
section in the image for jump stubs that reserves virtual address space
but does not increase the size of the image (in C++ this is the ".bss"
section). The size of this section could be calculated precisely from
all the required possible jump stub contributions to the image. Then,
the jump stub code would allocate jump stubs from this space when
required for a NGEN image.

### Cross-module inherited methods

Per the comments on `VirtualMethodFixupWorker()`, in an NGEN image,
virtual slots inherited from cross-module dependencies point to jump
thunks. The jump thunk invokes code to ensure the method is loaded and
has a stable entry point, at which point the jump thunk is replaced by a
"jmp rel32" to that stable entrypoint. This is represented by
`CORCOMPILE_VIRTUAL_IMPORT_THUNK`. This can require a jump stub.

Similarly, `CORCOMPILE_EXTERNAL_METHOD_THUNK` represents another kind of
jump thunk in the NGEN image that also can require a jump stub.

#### Failure mitigation

Both external method thunks could be changed to reserve 12 bytes instead
of just 5 for the jump thunk, to provide for space required for any
potential jump stub.

## Precode

Precodes are used as temporary entrypoints for functions that will be
JIT compiled. They are also used for temporary entrypoints in NGEN
images for methods that need to be restored (i.e., the method code has
external references that need to be loaded before the code runs). There
exists `StubPrecode`, `FixupPrecode`, `RemotingPrecode`, and
`ThisPtrRetBufPrecode`. Each of these generates a rel32 jump and/or call
that might require a jump stub.

StubPrecode is the "normal" general case. FixupPrecode is the most
common, and has been heavily size optimized. Each FixupPrecode is 8
bytes. Generated code calls the FixupPrecode address. Initially, the
precode invokes code to generate or fix up the method being called, and
then "fix up" the FixupPrecode itself to jump directly to the native
code. This final code will be a "jmp rel32", possibly via a jump stub.
DynamicMethod / LCG uses FixupPrecode. This code path has been found to
fail in large customer installations.

### Failure mitigation

An implementation has been made which changes the allocation of
FixupPrecode to pre-allocate space for jump stubs, but only in the case
of DynamicMethod. (See https://github.com/dotnet/coreclr/pull/9883).
Currently, FixupPrecode are allocated in "chunks", that share a
MethodDesc pointer. For LCG, each chunk will have an additional set of
bytes allocated, to reserve space for one jump stub per FixupPrecode in
the chunk. When the FixupPrecode is patched, for LCG methods it will use
the pre-allocated space if a jump stub is required.

For non-LCG, we are reserving, but not allocating, a space at the end
of the code heap. This is similar and in addition to the reservation done by
COMPlus_CodeHeapReserveForJumpStubs. (See https://github.com/dotnet/coreclr/pull/15296).

## Ready2Run

There are several DynamicHelpers class methods, used by Ready2Run, which
may create jump stubs (not all do, but many do). The helpers are
allocated dynamically when the helper in question is needed.

### Failure mitigation

These helpers could easily be changed to allocate additional, reserved,
unshared space for a potential jump stub, and that space could be used
when creating the rel32 offset.

## Compact entrypoints

The compact entrypoints implementation might create jump stubs. However,
compact entrypoints are not enabled for AMD64 currently.

## Stress modes

Setting `COMPlus_ForceRelocs=1` forces jump stubs to be created in all
scenarios except for JIT generated code. As described previously, the
VM doesn't know when the JIT is reporting a rel32 data address or code
address, and in addition the JIT reports relocations for intra-function
jumps and calls for which it doesn't expect the register used by the
jump stub to be trashed, thus we don't force jump stubs to be created
for all JIT-reported jumps or calls.

We should improve the communication between the JIT and VM such that we
can reliably force jump stub creation for every rel32 call or jump. In
addition, we should make sure to enable code to stress the creation of
jump stubs for every mitigation that is implemented whether that be
using the existing `COMPlus_ForceRelocs` configuration, or the creation of
a new configuration option.
