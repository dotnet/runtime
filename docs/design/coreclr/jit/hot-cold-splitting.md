# Hot/Cold Code Splitting

This document describes the current state of hot/cold splitting in the JIT.

Hot/Cold splitting is an optimization that splits code into frequently-executed ("hot") and rarely-executed ("cold")
parts, and places them in separate memory regions. Increased hot code density better leverages spatial locality,
improving application performance via fewer instruction cache misses, less OS paging, and fewer TLB misses.

## Background

The JIT previously supported hot/cold splitting for AOT-compiled NGEN images in .NET Framework. With Crossgen2 support
[in progress](https://github.com/dotnet/runtimelab/tree/feature/hot-cold-splitting) (and no existing support for
splitting dynamically-generated code), JIT support has not been tested since retiring .NET Framework -- thus, there
are likely regressions. Furthermore, the JIT never supported splitting functions with certain features, like exception
handling or switch tables. Finally, with ARM64 code generation being a newer addition to the JIT, hot/cold splitting
was never implemented for the architecture. These limitations significantly inhibit the applicability of hot/cold splitting.

The below sections describe various improvements made to the JIT's hot/cold splitting support to remove such limitations.

## Testing the JIT Without Runtime Support

Without runtime support for hot/cold splitting in .NET as of summer 2022, testing the JIT's existing hot/cold splitting
support is not as simple as turning the feature on. A new "fake" splitting mode, enabled by the
`COMPlus_JitFakeProcedureSplitting` environment variable, removes this dependency on runtime support. This mode allows
the JIT to execute its hot/cold splitting workflow without changing the runtime's behavior. This workflow proceeds as
follows:

* The JIT identifies where to split the function in `Compiler::fgDetermineFirstColdBlock`, as usual.
* In `Compiler::eeAllocMem`, the JIT requests one memory buffer from the host (either Crossgen2 or the VM) for the
entire function; this is unlike normal splitting, where separate buffers are allocating for the hot/cold sections.
  * After the host has allocating the buffer, the JIT manually sets the cold code pointers to right after the hot section.
  * Note there is no space between the hot/cold sections, unlike with normal splitting. The instructions are still contiguous.
* During code generation, the JIT emits instructions as if the hot/cold sections are arbitrarily far away:
  * Jumps between hot/cold sections are long.
  * The JIT reports jump target relocations to the host as necessary.
  * On some platforms like ARM64 (see below), the JIT emits certain pseudo-instructions to handle the instruction section's
lack of contiguousness.
* For the sake of simplicity, the JIT generates unwind info as if it is not splitting. Because the hot/cold sections
are adjacent, the JIT generates unwind info once for the entire function.

While enabling fake-splitting also enables `opts.compProcedureSplitting`, there is no guarantee the JIT will fake-split
a function unless `Compiler::fgDetermineFirstColdBlock` finds a splitting point; without PGO data, the JIT's heuristics
may be too conservative for extensive testing. To aid regression testing, the JIT also has a stress-splitting mode now,
under `COMPlus_JitStressProcedureSplitting`. When `opts.compProcedureSplitting` and stress-splitting are both enabled,
the JIT splits every function after its first basic block; in other words, `fgFirstColdBlock` is always
`fgFirstBB->bbNext`. The rest of the hot/cold splitting workflow is the same: The JIT emits instructions to handle the
split code sections and, if fake-splitting, utilizes only one memory buffer.

When used in tandem, fake-splitting and stress-splitting have strong potential to reveal regressions in the JIT's
hot/cold splitting functionality without runtime support. As such, a new rolling test job in the
[runtime-jit-experimental](https://dev.azure.com/dnceng/public/_build?definitionId=793), `jit_stress_splitting`, runs
all `dotnet/runtime` tests with fake-splitting and stress-splitting enabled.

### PRs

* [runtime/69763](https://github.com/dotnet/runtime/pull/69763): Implement fake-splitting and stress-splitting modes
* [runtime/69922](https://github.com/dotnet/runtime/pull/69922): Add `jit_stress_splitting` to `runtime_jit_experimental`

## ARM64 Support

After devising strategies for testing the JIT independently of runtime support for splitting, achieving functional
parity for ARM64 became a priority. While initial splitting prototypes in Crossgen2 target x64, the JIT can
achieve some correctness with hot/cold splitting on ARM64 by leveraging fake-splitting alone.

Most of the JIT's hot/cold splitting workflow is architecture-independent; only code generation is ARM64-specific.
The majority of implementation work here is thus related to emitting various long pseudo-instructions:

* On both ARM32 and ARM64, conditional jumps have less range than unconditional jumps due to the architectures' fixed
instruction width. Normally, the conditional jump's range is large enough to cover any reasonably-sized function.
With splitting enabled, hot/cold sections can be arbitrarily far apart for dynamically-generated code, and up to
2<sup>32</sup> bits apart in AOT-compiled code (this is the maximum code size allowed in PE files). To avoid
arbitrarily limiting code sizes, conditional jumps must have the same range as unconditional jumps. "Jump stubs" solve
this by replacing each conditional jump with a negated conditional jump, followed by an unconditional jump to the
original target -- this pseudo-instruction's format is `IF_LARGEJMP`. For example, `branch condition, target` becomes
the following:

```
branch !condition, pc+1
branch target
```

* Without splitting, the read-only data section is adjacent to the function's instruction section on ARM64. When
splitting, the data section is adjacent to the hot section; from the hot section, we can load constants with a single
`ldr` instruction. However, this is not possible from the cold section: Because it is arbitrarily far away, the target
address cannot be determined relative to the PC. Instead, the JIT emits a `IF_LARGELDC` pseudoinstruction with a
few different possibilities:
  * First, compute the target page address with an `adrp` instruction.
  * Case 1: Load the constant into a general register with a `ldr` instruction. (Final sequence: `adrp + ldr`)
    * If the destination register is a vector register, move the value from the general register with a `fmov`
instruction. (Final sequence: `adrp + ldr + fmov`)
  * Case 2: If the constant is 16 bytes in size, load it directly into a vector register.
    * General registers are 8 bytes in width on ARM64. Thus, they cannot temporarily hold the constant.
    * Compute the exact address with an `add` instruction, and load the constant with an `ld1` instruction.
(Final sequence: `adrp + add + ld1`)

Aside from these pseudo-instructions, hot/cold splitting required a few other tweaks to ARM64 code generation:
* When emitting long jumps between hot/cold sections, the JIT reports the target's relocation to the host with the
relocation type `IMAGE_REL_ARM64_BRANCH26`.
* While enabling fake-splitting did not require changes here, it is worth noting an importance difference in unwind info
generation on x64 versus ARM64. On x64, the JIT emits the full unwind info for a hot function fragment, and emits
"chained" unwind info for the cold function fragment. This chained unwind info does not contain any unwind codes, but
instead points to the hot fragment's unwind info. When unwinding, the VM will use this chained info to find the relevant
unwind info.

There is no concept of chained unwind info on ARM64; instead, the JIT generates unwind info for each function fragment,
regardless of its hot/cold status. While this should not have any immediate implications for JIT work around hot/cold
splitting, this does affect the feature's implementation in Crossgen2 and the VM. On x64, the Crossgen2 splitting
prototype uses chained unwind info to differentiate between cold main body fragments and cold EH funclets (see below for
details on EH splitting). This comparison is not possible on ARM64 -- the JIT may have to pass more information to the
host when generating unwind info on ARM64 to indicate if a cold fragment is a funclet.

### PRs

* [runtime/70708](https://github.com/dotnet/runtime/pull/70708): Enable fake-splitting on ARM64

## Splitting Functions with Exception Handling (EH)

An EH funclet is a "mini-function" for handling or filtering exceptions; for example, for a conventional "try/catch"
expression, the catch block becomes a funclet (this is true for finally/fault/filter/etc. blocks as well). The
JIT places EH funclets contiguously in memory, adjacent to the main function body. Because of the prevalence of
exception handling in .NET programs, enabling splitting of EH funclets massively expands this optimization's
applicability.

Because EH funclets immediately succeed the main function body, the JIT can easily split such functions without
breaking existing invariants:

* If the JIT finds a split point in the main body, it splits there as usual. The latter part of the main body,
along with all of the function's EH funclets, is cold.
* If the JIT does not find a split point in the main body, and none of the funclets execute frequently, it splits
at the beginning of the funclet section. The main body is hot, and all EH funclets become cold.
* Else, no splitting occurs.

This approach may not be the most performant implementation: Splitting funclets individually could yield better
spatial locality. However, this would require re-arranging the order of funclets (currently, there is no specific
order imposed), and significantly altering unwind info generation, thus breaking many invariants in the host. This
approach enables splitting in many more scenarios without breaking existing invariants or introducing
architecture-specific workarounds. However, if the JIT supports splitting functions multiple times in the future, we
should revisit this.

In the absence of PGO data, the JIT assumes exceptions occur rarely; this justifies moving handlers to the cold section.
Because `finally` blocks execute regardless of an exception occurring, it may be detrimental to make these handlers
cold. Thus, [Compiler::fgCloneFinally](https://github.com/dotnet/runtime/blob/41419131095d36fb5b811600ad0dab3b0d804269/src/coreclr/jit/fgehopt.cpp#L617)
copies the `finally` block to the hot section, provided it is not too large. Once runtime support for splitting matures,
we should revisit this optimization to ensure the JIT is not too sparse or overzealous in its usage.

### PRs

* [runtime/71236](https://github.com/dotnet/runtime/pull/71236): Enable hot/cold splitting of EH funclets
* [runtime/71273](https://github.com/dotnet/runtime/pull/71273): Disable `HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION`
* [runtimelab/1923](https://github.com/dotnet/runtimelab/pull/1923): Fix unwind info for cold EH funclets on x64
* [runtimelab/1930](https://github.com/dotnet/runtimelab/pull/1930): Fix unwind info for cold EH funclets on ARM64

## Future Work

As of writing, support for hot/cold splitting in Crossgen2 on x64 is in progress. While some future tasks are JIT-specific
and will not require runtime support to begin work, many will require close collaboration. See the `dotnet/runtimelab`
hot/cold splitting [prototype](https://github.com/dotnet/runtimelab/tree/feature/hot-cold-splitting) for
runtime-specific tasks.

* Profile runtime effects of hot/cold splitting. Since we are largely interested in how this affects spatial locality,
key metrics could include number of hot/cold page touches, number of jumps to the cold section taken, number of
instruction cache misses, etc. It is important that such profiling utilizes PGO data, as the JIT's splitting heuristics
are quite sparse, and may not be useful for measuring performance.
* Enable hot/cold splitting of functions with switch tables.
* Work with Crossgen2 prototype to support hot/cold splitting on ARM64.
  * This task will specifically require work in the JIT for differentiating cold funclets from regular cold code.
On x64, Crossgen2 and the VM use chained unwind info (or lack thereof) to differentiate the two. Since there is no
concept of chained unwind info on ARM64, the JIT may need to report more information to the host.
* Support hot/cold splitting of dynamically-compiled code.
  * Since the JIT has historically never supported splitting jitted code, it may be interesting to measure the overhead
of performing hot/cold splitting during runtime.
* Support hot/cold splitting of NativeAOT code.
  * Most of the work here will likely involve generating unwind info correctly.
