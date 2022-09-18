Linear Scan Register Allocation: Design and Implementation Notes
===================
Table of Contents
-----------------

  * [Overview](#overview)
  * [Preconditions](#preconditions)
    + [Lowered IR Form (LIR)](#lowered-ir-form-lir)
    + [Register Requirements](#register-requirements)
  * [Post-Conditions](#post-conditions)
  * [LSRA Phases](#lsra-phases)
    + [Liveness and Candidate Identification](#liveness-and-candidate-identification)
    + [Block Ordering](#block-ordering)
    + [Building Intervals and RefPositions](#building-intervals-and-refpositions)
    + [Register allocation (doLinearScan)](#register-allocation-dolinearscan)
  * [Key Data Structures](#key-data-structures)
    + [Live In](#live-in)
    + [currentLiveVars](#currentlivevars)
    + [Referenceable](#referenceable)
    + [Interval](#interval)
    + [RegRecord](#regrecord)
    + [RefPosition](#refposition)
    + [GenTree Nodes](#gentree-nodes)
    + [VarToRegMap](#vartoregmap)
  * [Dumps and Debugging Support](#dumps-and-debugging-support)
  * [LSRA Stress Modes](#lsra-stress-modes)
  * [Assertions & Validation](#assertions--validation)
  * [Future Extensions and Enhancements](#future-extensions-and-enhancements)
  * [Feature Enhancements](#feature-enhancements)
    + [Support for Allocating Consecutive Registers](#support-for-allocating-consecutive-registers)
  * [Code Quality Enhancements](#code-quality-enhancements)
    + [Merge Allocation of Free and Busy Registers](#merge-allocation-of-free-and-busy-registers)
    + [Auto-tuning of register selection](#auto-tuning-of-register-selection)
    + [Pre-allocating high frequency lclVars](#pre-allocating-high-frequency-lclvars)
    + [Avoid Splitting Loop Backedges](#avoid-splitting-loop-backedges)
    + [Enable EHWriteThru by default](#enable-ehwritethru-by-default)
    + [Avoid Spill When Stack Copy is Valid](#avoid-spill-when-stack-copy-is-valid)
    + [Rematerialization](#rematerialization)
    + [Improving Reg-Optional Support](#improving-reg-optional-support)
      - [Reg-Optional Defs](#reg-optional-defs)
      - [Don't Pre-determine Reg-Optional Operand](#dont-pre-determine-reg-optional-operand)
      - [Don't Mark DelayFree for Duplicate Operands](#dont-mark-delayfree-for-duplicate-operands)
    + [Improving Preferencing](#improving-preferencing)
    + [Leveraging SSA form](#leveraging-ssa-form)
    + [Spanning trees for physical registers](#spanning-trees-for-physical-registers)
    + [Improve the handling of def/use conflicts](#improve-the-handling-of-defuse-conflicts)
  * [Throughput Enhancements](#throughput-enhancements)
    + [Allocation Window for Min-Opts and Tier 0](#allocation-window-for-min-opts-and-tier-0)
    + [Distinguish Intra-Block versus Inter-Block Variables](#distinguish-intra-block-versus-inter-block-variables)
    + [Improve the VarToRegMap](#improve-the-vartoregmap)
    + [Other Throughput Investigations](#other-throughput-investigations)
  * [Test and Cleanup Issues](#test-and-cleanup-issues)
  * [References](#references)

Overview
--------

This document provides additional detail on the linear scan register
allocator (LSRA) in RyuJIT. It is expected that the reader has already
read the [RyuJIT Overview document](ryujit-overview.md).

Register allocation is performed using a linear scan register allocation
scheme, implemented by the `LinearScan` class.

-   Physical registers are represented by the `RegRecord` class.

-   Values (`lclVar` references and non-lclVar `GenTree` nodes that
    produce a value) are represented by the `Interval` class.

    -   Each `Interval` (rather poorly named, since it really is
        multiple "intervals") consists of one or more live ranges, each
        of which has a list of `RefPosition`s (termed "use positions"
        in much of the literature) that identify nodes at which the
        value is referenced, as well as the register requirements for
        that reference.

-   References to both physical (`RegRecord`) and virtual
    (`Interval`) registers are represented by the
    `RefPosition` class. It contains information about the location,
    type and other requirements of the reference.

There are four main phases to LSRA:

-   Preparation

    -   The order of allocation of the `BasicBlock`s is determined:

        -   This attempts to ensure that at least one predecessor of a
            block is allocated before it.

        -   When not optimizing, the layout order of the blocks is used.

        -   Note that the order doesn't affect correctness, as the
            location of `lclVar`s across block boundaries is fixed up
            as necessary by the resolution phase. When not optimizing,
            `lclVar`s are not enregistered, so there is no benefit to
            using a different order.

    -   An `Interval` is built for each register-candidate `lclVar`.

    -   A `RegRecord` is built for each physical register.

-   Constructing `RefPosition`s

    -   The `RefPosition`s are built in a linear traversal of all the
        `GenTree` nodes

        -   Iterating over the `BasicBlock`s in the order determined
            above, a `RefTypeBB` `RefPosition` is created at each
            new `BasicBlock`. This is the signal to the register
            allocator to re-establish variable locations at the
            boundary.

        -   Iterating over the nodes within the block in execution
            order:

            -   Two locations (`LsraLocation`, which is an `unsigned
                int`) are assigned to each node.

                -   The first is the virtual location at which its
                    operands are used.

                -   The second is the virtual location at which its
                    target register(s) are defined.

                -   This allows an instruction to use the same register
                    as both a source and target (where the source is not
                    marked `delayRegFree`.

                -   An exception is multi-reg local stores of multi-reg sources.
                    For these, the code generator will read each source register,
                    and then move it, if needed, to the destination register.
                    These nodes have 2*N locations where N is the number of registers,
                    so that the liveness can be reflected accordingly.

    -   For each node, `RefPosition`s are built to reflect the uses,
        definitions and kills of any registers involved in the
        evaluation of the node.

-   The allocation phase iterates over the `RefPosition` list:

    -   At the beginning of each new block the register assignment of
        each lclVar `Interval` is updated, either based on incoming
        argument locations (for the first block in the method),
        or based on the location at the end of the selected predecessor block.

    -   Iteration proceeds according to ascending order of `RefPosition`s.
        This means that the register assignment is performed in linear
        execution order within a block.

    -   The status of an `Interval` is updated as a `RefPosition` is
        encountered for it.

    -   Splitting or spilling an `Interval` doesn't involve creating a new
        one. Instead, the `RefPosition` simply gets a new assignment, and
        is either marked for reload/copy or its location is simply
        updated in the incoming map. This differs from other linear-scan
        allocators, where separate intervals are constructed for this case.

-   The resolution phase has two parts:

    -   The `RefPosition`s are walked again to write back the register
        assignments to the `GenTree` nodes.

        -   It is at this point that the register assignments for the
            lclVars are finalized at `BasicBlock` boundaries.

    -   Then, if we are optimizing (i.e. enregistering lclVars) the block
        boundaries are handled:

        -   For fork edges (the source block has multiple targets, but
            each target has only that one source), any required
            resolution is placed at the individual target(s).

        -   For join edges (a single target block has multiple sources,
            but each source has only that one target), any required
            resolution is placed at the individual source(s).

        -   Critical edges require more complicated handling, and may
            require splitting of the edge for placement of resolution.

            -   This is an area in need of improvement, see
                [Avoid Splitting Loop Backedges](#avoid-split).

Preconditions
-------------

### Lowered IR Form (LIR)

The RyuJIT backend components (register allocation and code generation)
are somewhat unusual (compared to other compilers) in that the IR nodes
do not map directly to target
instructions. Instead, the `Lowering` phase of the JIT first performs
IL transformations to ensure that the operations match the semantic
level of the target such that all registers are explicitly represented
in the IL:

-   This may not result in a 1:1 correlation between nodes and target
    instructions, as some nodes (e.g. constants) may not require a
    register, and other nodes (e.g. those representing addressing modes)
    may become part of the parent instruction at code generation time.
    Other nodes may cause multiple instructions to be generated.

It is the job of the `Lowering` phase to transform the IR such that:

-   The nodes are in `LIR` form (i.e. all expression trees have been
    linearized, and the execution order of the nodes within a BasicBlock
    is specified by the `gtNext` and `gtPrev` links).

-   Any node that will be evaluated as part of its parent (consumer)
    node has been identified. These are known as "contained" nodes, and
    are identified by setting the `GTF_CONTAINED` flag in the
    `gtFlags` field of the node.

-   Similarly, nodes that represent values that could be referenced from
    memory by their consumer (i.e. via an addressing mode) are marked as
    "reg-optional" (`LIR::Flags::RegOptional`)

-   All unused values (nodes that produce a result that is not consumed)
    are identified (`LIR::Flags::UnusedValue` flag is set in `gtLIRFlags`).

    -   Since tree temps (the values produced by nodes and consumed by
        their parent) are expected to be single-def, single-use (SDSU),
        normally the live range can be determined to end at the use. If
        there is no use, the register allocator doesn't know where the
        live range ends.

-   Code can be generated without any context from the parent (consumer)
    of each node.

### Register Requirements

There are three types of value for which registers are allocated:

-   Local variables, `lclVar`s, which appear in the IR as
    `GT_LCL_VAR` for uses or `GT_STORE_LCL_VAR` for
    definitions.

-   Values produced by any other IR node. These are known as "tree
    temps", and never have more than a single use.

-   "Internal registers" that are required for the evaluation of a node.

    -   These are an exception to the explicit representation of
        registers.

The register requirements are determined by the `LinearScan::BuildNode()`
method which is called for each node in order to build its
`RefPosition`s. (It in turn may invoke a node-specific `LinearScan::BuildXXX()` method).

The register lifetimes must obey the following lifetime model:

-   First, any internal registers are defined.

-   Next, any source registers are used (and are then freed if they are
    last use and are not identified as `delayRegFree`).

-   Next, the internal registers are used (and are then freed).

-   Next, any registers in the kill set for the instruction are killed.

-   Next, the destination register(s) are defined.

-   Finally, any `delayRegFree` source registers are freed.

There are several things to note about this order:

-   The lifetime of internal registers will always overlap a use (that is, they may never
    get the same register as a source), but they do not overlap the lifetime of a destination register (so they may get the same register as a target unless they are marked `delayRegFree`).

-   Internal registers are never live beyond the node.

-   The `delayRegFree` annotation is used to identify register uses that must not
    be allocated the same register as a register target for the same node. This is
    primarily used for instructions that are
    only available in a Read-Modify-Write form. That is, the destination
    register is one of the sources. In this case, we must not use the
    same register for the non-RMW operand as for the destination. Thus, the non-RMW operand
    is marked with the `delayRegFree` annotation.

Post-Conditions
---------------

After LSRA, the graph has the following properties:

-   The `_gtRegNum` of each tree node (`GetRegNum()`) contains the allocated register,
    if any. Nodes that produce multiple registers are similarly
    assigned, via extended register number fields. If the node does not
    produce a value directly (i.e. it is either of void type, or it is
    evaluated as part of its parent) its `_gtRegNum` is set to `REG_NA`.

    -   In most cases, this register must satisfy the constraints
        specified for each `RefPosition` by the `BuildNode` methods.

    -   In some cases, this is difficult:

        -   If a lclVar node currently lives in some register, it may
            not be desirable to move it (i.e. its current location may
            be desirable for future uses, e.g. if it's a callee save
            register, but needs to be in a specific arg register for a
            call).

        -   In other cases there may be conflicts on the restrictions
            placed by the defining node and the node which consumes it

    -   If such a node is constrained to a single fixed register (e.g.
        an arg register, or a return from a call), then LSRA is free to
        annotate the node with a different register.  The code generator
        must issue the appropriate move.

    -   However, if such a node is constrained to a set of registers,
        as in the case of x86 instructions which require a byte-addressable register,
        and its current location does not satisfy that requirement, LSRA
        must insert a `GT_COPY` node between the node and its parent.
        The `_gtRegNum` on the `GT_COPY` node must satisfy the register
        requirement of the parent.

-   `GenTree::gtRsvdRegs` has a set of registers used for internal temps.
    These must satisfy the constraints specified by the associated `RefPosition`s.

-   A tree node is marked `GTF_SPILL` if the tree node must be spilled by
    the code generator after it has been evaluated. The value will no longer be
    live in the register, except in some cases involving EH-write-thru vars, see below.

-   A tree node is marked `GTF_SPILLED` if it is a lclVar that must be
    reloaded prior to use.

    -   The register (`_gtRegNum`) on the node indicates the register to
        which it must be reloaded.

    -   For lclVar nodes, since the uses and defs are distinct tree
        nodes, it is always possible to annotate the node with the
        register to which the variable must be reloaded.

    -   For other nodes, since they represent both the def and use, if
        the value must be reloaded to a different register than the one specified
        on the tree node (which is the one used when the node is evaluated), LSRA must
        insert a `GT_RELOAD` node to specify the register to which it
        should be reloaded.

-   If a node has both `GTF_SPILL` and `GTF_SPILLED`, the tree node is reloaded prior to using
    it (`GTF_SPILLED`) and spilled after it is evaluated (`GTF_SPILL`).

    -   For normal variables, we can only have both `GTF_SPILL` and `GTF_SPILLED` on uses,
        since a def never needs to reload an old value. However, for EH-write-thru variable
        defs, this combination of flags has a special meaning. A def of an EH-write-thru variable is
        always written to the stack. However, if it is also marked `GTF_SPILLED` it remains live in the
        register, in addition to being written to the stack. This is somewhat counter-intuitive since
        normally the reloading (`GTF_SPILLED`) takes place *prior* to evaluation of the node.


-   Note that `GT_COPY` and `GT_RELOAD` nodes are inserted immediately after the
    instruction that must be copied or reloaded. However, the reload or copy
    isn't actually generated until the code generator is generating code for
    the consuming node.

-   Local variable table (`LclVarDsc`):

    -   `LclVarDsc::lvRegister` is set to true if a local variable has the
        same register assignment for its entire lifetime.

    -   `LclVarDsc::_lvRegNum` is initialized to its initial register
        assignment.

        -   For incoming parameters, this is the register to which
            `genFnPrologCalleeRegArgs()` will move it.

    -   Codegen will set `_lvRegNum` to its current value as it processes
        the trees, since a variable can be assigned different registers
        over its lifetimes.

LSRA Phases
-----------

This section describes the phases of the `LinearScan` allocator (as
well as supporting components) in more depth.

### Liveness and Candidate Identification

-   `Compiler::lvaMarkLocalVars`

    -   This determines which variables are tracked for the purposes of
        dataflow analysis. Only those variables will be candidates for
        register allocation.


-   `Compiler::fgLocalVarLiveness`

    -   This computes the `BasicBlock::bbLiveIn` and
        `BasicBlock::bbLiveOut` sets that are used by LSRA.

    -   It does this for any lclVar marked `lvTracked`, even if it is
        not a register candidate. The dataflow information is also used
        for GC info, but it may be the case that some of these should
        not be marked during the pre-LSRA dataflow analysis.

-   `LinearScan::identifyCandidates`

    -   This mostly duplicates what is done in
        `Compiler::lvaMarkLocalVars()`. There are differences in what
        constitutes a register candidate vs. what is tracked, but we
        should probably handle them in `Compiler::lvaMarkLocalVars()`
        when it is called after `Lowering`.

    -   It sets the `lvLRACandidate` flag on lclVars that are going
        to be register candidates.

### Block Ordering

The determination of the order in which the register allocator handles
blocks is done during the building of
`RefPosition`s. However, it is a logically distinct component. Its
objective is to identify a sequence of `BasicBlock`s for allocation
that satisfies the following properties:

-   Each block comes after at least one of its predecessors, ideally the
    one on the edge with the greatest weight.

    -   We use block weight, since edge weight is not tracked in the
        JIT.

-   Blocks that enter EH regions have no predecessor. All live-in vars are on the stack.

The order of the `BasicBlock`s is captured in the `blockSequence` member of `LinearScan`.

Other implementations of linear scan register allocation aim to ensure
that a block is immediately preceded by a predecessor block. This is not
as big a consideration in our implementation because we reset the
variable locations to those at the end of the most frequent successor.

It begins with the first block, and then adds successor blocks to the
ready list, in sorted order, where the block weight is the sorting
criterion.

After a block is added to the sequence list,
`findPredBlockForLiveIn()` is called to determine which predecessor to
use to set the register location of live-in lclVars, which may not be the
same as the previous block in the `blockSequence`. Its `bbNum` is captured in
the `LsraBlockInfo`.

The block ordering pass also identifies whether and where there are
critical edges. This also captured in the `LsraBlockInfo` and is used by the resolution phase.

### Building Intervals and RefPositions

`Interval`s are built for lclVars up-front. These are maintained in an array,
`localVarIntervals` which is indexed by the `lvVarIndex` (not the `varNum`, since
we never allocate registers for non-tracked lclVars). Other intervals (for tree temps and
internal registers) are constructed as the relevant node is encountered. Intervals for
`lclVar`s that are live into an exception region are marked `isWriteThru`.

The building of `RefPosition`s is done via a traversal of the nodes, using the `blockSequence`
constructed as described above. This traversal invokes `LinearScan::BuildNode()` for each
node, which builds `RefPositions` according to the liveness model described above:

-   First, we create `RefPosition`s to define any internal registers that are required.
    As we create new `Interval`s for these, we add the definition `RefPosition` to an array,
    `internalDefs`. This allows us to create the corresponding uses of these internal
    registers later.

-   Then we create `RefPosition`s for each use in the instruction.

    -   A use of a register candidate lclVar becomes a `RefTypeUse` `RefPosition` on the
        `Interval` associated with the lclVar.

    -   For tree-temp operands (including non-register-candidate lclVars), we may have one
        of 3 situations:

         - A contained immediate requires no registers, so no `RefPosition`s are created.

         - A contained memory operand or addressing mode will cause `RefPosition`s to be
           created for any (non-contained) base or index registers.

         - A single `RefPosition` is created for non-contained nodes.

         In order to build these uses, we need to find the `Interval` associated with the
         tree-temp. This is done via the `defList`, which contains the `RefTypeDef` `RefPosition`
         for all tree temps whose def has been encountered but for which we
         have not yet seen the use. This is a simple list on the assumption that the distance
         between defs and uses of tree temps is rarely very great.

         When we have an instruction that will overwrite one of its sources, such as RMW
         operands common on x86 and x64,
         we need to ensure that the other source(s) isn't/aren't given the same register as the
         target. For this, we annotate those use `RefPosition`s with `delayRegFree`.

-   Next we create the uses of the internal registers, using the `internalDefs` array.
    This is cleared before the next instruction is handled.

-   Next, any registers in the kill set for the instruction are killed. This is performed
    by `buildKillPositionsForNode()`, which takes a kill mask that is node-specific and
    either provided directly by the `buildXXX()` method for the node, or by a `getKillSetForXXX()`
    method. There is a debug-only method, `getKillSetForNode()` which is only used for validation.

-   Finally, we create `RefTypeDef` `RefPositions` for any registers that are defined by
    the node.

    -   For a `STORE_LCL_VAR` of a write-thru `lclVar`, the `RefPosition` is marked `writeThru`.

    -   There is some special handling for `GT_PUTARG_REG` nodes whose source is a non-last-use lclVar.
        At build time, we mark such intervals as `isSpecialPutArg`.
        At allocation time, if the lclVar is already in the argument register, but it has another use
        prior to the call, we don't want to reassign that register to the tree temp interval, since that
        would require us to spill the lclVar and reload it for the next use.
        Instead, we retain the assignment of the register to the lclVar, and mark that `RegRecord` as
        `isBusyUntilNextKill` so that it isn't reused if the lclVar goes dead before the call.
        (Otherwise, if the lclVar is in a different register, or if its next use is after
        the call, we clear the `isSpecialPutArg` flag on the interval.) Here is
        a case from `Microsoft.Win32.OAVariantLib:ChangeType(System.Variant,System.Type,short,System.Globalization.CultureInfo):System.Variant` in System.Private.CoreLib.dll (I've edited the
        dump to make it easier to see the structure):
```
N037  t16 =    ┌──▌  LCL_VAR   ref    V04 arg3
N039 t127 = ┌──▌  PUTARG_REG ref    REG rcx
N041 t128 = │                 ┌──▌  LCL_VAR   ref    V04 arg3    (last use)
N043 t129 = │              ┌──▌  LEA(b+0)  byref
N045 t130 = │           ┌──▌  IND       long
N047 t131 = │        ┌──▌  LEA(b+72) long
N049 t132 = │     ┌──▌  IND       long
N051 t133 = │  ┌──▌  LEA(b+40) long
N053 t134 = ├──▌  IND       long   REG NA
N055  t17 = ▌  CALLV ind int    System.Globalization.CultureInfo.get_LCID $242
```
The `PUTARG_REG` at location 39 uses V04, and wants it in `RCX`. Because it is not a last use,
the tree temp interval `I14` is marked `isSpecialPutArg` when it is built. At allocation time, we
allocate V04 to `RCX` so we leave the `isSpecialPutArg` flag and mark rcx as `isBusyUntilNextKill`
(this shows as "Busy" in the dump after the last use of V04 has been encountered).
This allows the use of V04 by the indirection at location 45
to use the value in `RCX`, and since that register is busy, we don't incorrectly free it even
though it is a last use of V04.
```
────────────────────────────────┼────┼────┼────┼────┼────┼────┼────┼────┼────┤
Loc RP#  Name Type  Action Reg  │rax │rcx │rdx │rbx │rbp │rsi │rdi │r8  │r9  │
────────────────────────────────┼────┼────┼────┼────┼────┼────┼────┼────┼────┤
 39.#15  V4   Use    Keep  rcx  │C12i│V4 a│    │V1 a│V3 a│V0 a│V2 a│    │    │
 40.#16  rcx  Fixd   Keep  rcx  │C12i│V4 a│    │V1 a│V3 a│V0 a│V2 a│    │    │
 40.#17  I14  Def    PtArg rcx  │C12i│V4 a│    │V1 a│V3 a│V0 a│V2 a│    │    │
 45.#18  V4   Use *  Keep  rcx  │C12i│V4 a│    │V1 a│V3 a│V0 a│V2 a│    │    │
 46.#19  I15  Def    Alloc rax  │I15a│Busy│    │V1 a│V3 a│V0 a│V2 a│    │    │
 49.#20  I15  Use *  Keep  rax  │I15a│Busy│    │V1 a│V3 a│V0 a│V2 a│    │    │
 50.#21  I16  Def    Alloc rax  │I16a│Busy│    │V1 a│V3 a│V0 a│V2 a│    │    │
 55.#22  rcx  Fixd   Keep  rcx  │I16a│Busy│    │V1 a│V3 a│V0 a│V2 a│    │    │
 55.#23  I14  Use *  PtArg rcx  │I16a│Busy│    │V1 a│V3 a│V0 a│V2 a│    │    │
 55.#24  I16  Use *  Keep  rax  │I16a│Busy│    │V1 a│V3 a│V0 a│V2 a│    │    │
 56.#25  rax  Kill   Keep  rax  │    │Busy│    │V1 a│V3 a│V0 a│V2 a│    │    │
```
-   A `RefTypeBB` `RefPosition` marks the beginning of a block, at which the incoming live
    variables are set to their locations at the end of the selected predecessor.

    -   If there are live-in variables with no location in the
        selected predecessor, a `RefTypeDummyDef` is created for each of these.

        -   This is generally not required, as the block will
            normally have a predecessor block that has already
            been allocated. This facility is exercised by the 0x100
            (`LSRA_BLOCK_BOUNDARY_LAYOUT`) or 0x200 (`LSRA_BLOCK_BOUNDARY_ROTATE`) settings of `COMPlus_JitStressRegs`.

-   At the end of a block, for any exposed uses that do not have downstream
    `RefPosition`s (e.g. variables that are live across the backedge, so there is no
    last use), a `RefTypeExposedUse` is created.

During this phase, preferences are set:

-   Cross-interval preferences are expressed via the `relatedInterval` field of `Interval`

    -   When a use is encountered, it is preferenced to the target `Interval` for the
        node, if that is deemed to be profitable. During register selection, it tries to
        find a register that will fit the requirements of that preferenced `Interval`.
        Then, when the use `RefPosition` is assigned a register, the
        register preference of the target `Interval` is updated.
        This addresses the scenario when the lclVar has already
        (at a previous definition) been assigned a register, and we want to try to use that
        register again, as well as the case where it has yet to be assigned a register.

        This area has room for improvement, (see [Improving Preferencing](#improving-preferencing)
        for specific issues related to this.).

    - Register preferences are set:

        - When the use or definition of a value must use a fixed register, due to instruction
          or ABI constraints. In this case, the register may be added to the `registerPreferences`,
          depending on whether it conflicts with existing preferences
          (the heuristics for determining this are in `LinearScan::mergeRegisterPreferences()`).

        - When a register is killed at a point where a lclVar is live, that register is removed
          from the `registerPreferences` of the lclVar `Interval`.

        - The register preferences of a related `Interval` may also be updated.

### Register allocation (doLinearScan)

The allocation phase iterates over the blocks in `BlockSequence` order, and then over the
linear ordering of nodes within each `BasicBlock`:

-   Iterate over `RefPosition`s in forward order, performing linear scan
    allocation. The algorithm below is a modified version of that
    specified in [[3]](#[3]).
    It is a high-level abstraction of the algorithm as implemented:
```
LinearScanAllocation(List<RefPosition> refPositions)

{
    List<Interval> active = {};

    Location currentLoc = Start;
    foreach refPosition in RefPositions
    {
        if (refPosition.Location > currentLoc)
        {
            freeRegisters();
            currentLoc = refPosition.Location;
        }
        if (refPosition is BasicBlockBoundary)
        {
            ProcessBlockBoundaryAlloc();
            continue;
        }
        Interval currentInterval = refPosition.Interval;
        if (currentInterval->hasValidAssignment())
        {
            refPosition->setReg(currentInterval->physReg);
        }
        // Find a register for currentInterval.
        // AllocateBusyReg() will take into account whether it must have
        // a register in this position, and may otherwise choose not to
        // allocate if existing intervals have higher priority.
        if (!TryAllocateFreeReg(refPosition))
            AllocateBusyReg(refPosition);
    }
}
```
-   hasValidAssignment() is not currently an actual method. If the
    `Interval` currently has an assigned register, and it meets the
    requirements of `refPosition` it is used. Otherwise, a new register
    may be assigned.

    -   Currently, parameters may not be allocated a register if their
        weighted reference count is less than `BB_UNITY_WEIGHT`, however
        plenty of room remains for improving the allocation of
        parameters [Issue \#7999](https://github.com/dotnet/runtime/issues/7999)

-   `TryAllocateFreeReg()` iterates over the registers, attempting to find
    the best free register (if any) to allocate:

    -   It uses a set of scoring criteria to evaluate the "goodness" of
        the register that includes:

        -   Whether it covers the lifetime of the `Interval` and/or the
            `Interval` to which it is preferenced, if any

        -   Whether it is in the register preference set for the
            `Interval`

        -   Whether it is not only available but currently unassigned
            (i.e. this register is NOT currently assigned to an `Interval`
            which is not currently live, but which previously occupied
            that register).

        -   Currently it doesn't take encoding size into account.
            [Issue \#7996](https://github.com/dotnet/runtime/issues/7996)
            tracks this.

    -   It always uses the same order for iterating over the registers.
        The jit32 register allocator used a different ordering for tree
        temps than for lclVars. It's unclear if this matters for LSRA,
        but [Issue \#8000](https://github.com/dotnet/runtime/issues/8000)
        tracks this question.

-   `AllocateBusyReg()` iterates over all the registers trying to find the
    best register to spill (it must only be called if `tryAllocateFreeReg()`
    was unable to find one):

    -   It takes into account a number of heuristics including:

        -   The distance to the next use of the `Interval` being
            spilled

        -   The relative weight of the `Interval`s.

        -   Whether the `RefPosition` being allocated, or the one
            potentially being spilled, is reg-optional

    -   Both `tryAllocateFreeReg()` and `allocateBusyReg()` currently fully evaluate the "goodness"
        of each  register, except in certain cases.

    -   It will always spill an `Interval` either at its most recent
        use, or at the entry to the current block.

    -   It is quite likely that combining `TryAllocateFreeReg()` and
        `AllocateBusyReg()` would be more effective, see
        [Merge Allocation of Free and Busy Registers](#merge-allocation-of-free-and-busy-registers)

-   Resolution

    Since register allocation is performed over a linearized list of nodes, there
    is no guarantee that register assignments on off-path edges will be consistent.
    The resolution phase is responsible for performing moves, as needed to match up
    the register assignments across edges.

    -    All of the critical edges are handled first.These are the most problematic, as there
         is no single location at which the moves can be added. This is done in a pass over all
         `BasicBlock`s with an outgoing critical edge, handling all critical edges from that block.
         The approach taken is:
           - First, eliminate any variables that always get the same register, or which
             are never live on an edge.
           - Then, eliminate any variables that are in the same register at the end of this
             block, and at all target blocks.
           - Next, for the remaining variables, classify them as either:
             - In different registers at one or more targets. These require that the edge
               be split so that we can insert the move on the edge (this is the `diffResolutionSet`).
             - In the same register at each target (this is the `sameResolutionSet`), but different from the end of this block.
               For these, we can insert a move at the end of this block, as long as they
               don't write to any of the registers read by the `diffResolutionSet` as those
               must remain live into the split block.

    -   The actual resolution, for all edge types, is done by `resolveEdge()`.
        Based on the `ResolveType`, it either inserts the move at the top or bottom
        of the block.
        The algorithm for resolution can be found in [[2]](#[2]), though note
        that there is a typo: in the last 'if' statement, it should be
        "if b != loc(pred(b))" instead of "if b = loc(pred(b))":

        -   First, any variables that are in registers on the incoming
            edge, but need to be on the stack, are stored, freeing those
            registers

        -   Next, any register-to-register moves are done, using a
            standard algorithm.

            -   If there are circular dependencies, swap is used when
                available, and a temp register is used otherwise.

        -   Finally, any stack to register moves are done.

    -   Resolution of exception edges

        -   When `COMPlus_EnableEHWriteThru == 0`, any variable that's
            live in to an exception region is always referenced on the stack.

        -   See [Enable EHWriteThru by default](#enable-ehwritethru-by-default).

-   Code generation (genGenerateCode)

    -   Code generation utilizes the registers specified on the
        instructions, as gtRegNum.

    -   At block boundaries, the code generator calls the register
        allocator to update the locations of variables
        (`recordVarLocationsAtStartOfBB()`)

    -   Spills and reloads are generated as annotated in `gtFlags`
        (`GTF_SPILL`, `GTF_SPILLED`, respectively).

    -   GC tracking is performed during codegen.

Key Data Structures
-------------------

### Live In

This is the same `bbLiveIn` set as used elsewhere in the JIT but is
recomputed prior to register allocation because `Lowering` creates new
variables.

### currentLiveVars

This is a temporary single-instance data structure used during the
construction of `Interval`s.

### Referenceable

This is a common base class for `Interval`, which represent virtual
registers, and `RegRecord`s, which represent the physical registers. It
contains:

-   An ordered list of ref positions -- each point in the linear order
    at which it is defined or used, with a flag indicating whether it
    must be in a register.

### Interval

A structure that represents a variable, optimizer temp, or tree temp,
that is a candidate for enregistering. Its `RefPosition`s reflect
actual uses (`RefTypeUse`) or defs (`RefTypeDef`) in the code
stream, entry "definitions" (`RefTypeParamDef` and
`RefTypeZeroInit`) or convey boundary information
(`RefTypeDummyDef`, `RefTypeExpUse`). It also contains:

-   Register preference information:

    -   Set of preferred registers

    -   An `Interval` to which it is related by assignment or copy.

-   The assigned register and its `RegRecord`

    -   During allocation, this may change, and reflects the current
        assignment, if any, at the most recent `RefPosition`

    -   If the `Interval` is spilled or becomes inactive, it will retain
        the `RegRecord`, but its `assignedReg` field will be set to
        REG_NA.

A special type of `Interval` is used to track the status of the upper half
of a vector register in the case where calls do not preserve the upper half
of the callee-save vector registers. This is true for 256-bit vectors on
`AVX` and for 128-bit registers on Arm64 `NEON`. These `Interval`s are
marked with `isUpperVector`, and their `relatedInterval` points to the associated
large vector `Interval`. This is an imperfect workaround for this special type
of partial register tracking, which is costly to support in full generality.

### RegRecord

`RegRecord`s for physical registers share a common base class with
`Interval`s, but represent actual registers instead of virtual
registers. They have a list of `RefPosition`s for code locations at
which a fixed physical register is required, and during allocation they
maintain a pointer to the current `Interval` to which the physical
register is assigned.

`RegRecord` `RefPosition`s are `RefTypeKill` or `RefTypeFixedReg`. The former is
inserted at a point where a register is killed, e.g. by a call. The
latter are inserted at a point where a specific register must be used or
defined. These are generally due to calls or instructions with implicit
fixed registers.

The representation of `TYP_DOUBLE` registers on 32-bit `Arm` is complicated by the
fact that they overlap two `TYP_FLOAT` registers. The handling of this
case could be improved. See [Support for Allocating Consecutive Registers](#Support-for-Allocating-Consecutive-Registers).

### RefPosition

A `RefPosition` represents a single use, def or update of an
enregisterable variable or temporary or physical register. It contains

-   The `Interval` or `RegRecord` to which this reference belongs.

-   The next `RefPosition` (in code order) for the `Interval` or `RegRecord`.
    This is used when determining which register to spill.

-   Register assignment

    -   Prior to allocation, this represents the set of valid registers
        for this reference.

    -   After allocation, this is a single element register mask
        including only the allocated register (or two registers in the
        ARM implementation for longs).

-   The type of `RefPosition`:

    -   `RefTypeDef` is a pure definition of an `Interval` .

    -   `RefTypeUse` is a pure use of an `Interval` .

    -   `RefTypeKill` is a location at which a physical register is
         killed. These only exist on `RegRecord`s, not on `Interval`s

    -   `RefTypeBB` is really just a marker in the list of `RefPosition`s,
         where the register allocator needs to record the register
         locations at block boundaries. It is not associated with an
        `Interval` or `RegRecord`.

    -   `RefTypeFixedReg` is a `RefPosition` on a `RegRecord` that marks a
        position at which it is required by some `Interval`. This allows
        the register allocator to determine the range in which a
        register is free.

    -   `RefTypeExpUse` is an `Interval` `RefPosition` inserted just prior to
        a loop backedge, that allows the register allocator to identify
        the actual end of the live range

    -   `RefTypeParamDef` is an `Interval` `RefPosition` that represents the
        incoming "definition" of a parameter (register or stack)

    -   `RefTypeDummyDef` is inserted at a block into which a variable is
        live, but was not live out of the previous block (in traversal
        order).

    -   `RefTypeZeroInit` is an `Interval` `RefPosition` that represents the
        position at entry at which a variable will be initialized to
        zero.

    -   `RefTypeUpperVectorSave` is a `RefPosition` for an upper vector `Interval`
        that is inserted prior to a call that will kill the upper vector if
        it is currently occupying a register. The `Interval` is then marked with
        `isPartiallySpilled`.

    -   `RefTypeUpperVectorRestore` is a `RefPosition` for an upper vector `Interval`
        that is inserted prior to a use of the associated vector, or at the end
        of a block. This is only done in the case where the `RefPosition` might
        have become partially spilled.

### GenTree Nodes

The tree nodes are updated during the writeback traversal with the
register that has been assigned to it (if any). If the associated
`RefPosition` is marked `spillAfter`, the `GTF_SPILL` flag is set. If it is
marked "reload" then the `GTF_SPILLED` flag is set.

The `GT_LCL_VAR` nodes are updated with the current "active" register
for the variable. For `Interval`s that represent incoming parameters, this
must be set to the initial register assignment for the parameter (this
may differ from the incoming register for register parameters; the
prolog generation code will move it as needed).

In the event that a tree-node (non-lclVar) is spilled, and the original
assigned register is not available at the time it is reloaded, a
GT_RELOAD node is inserted in the graph, and is annotated with the
register to which it should be reloaded just prior to use.

### VarToRegMap

This mapping records the location of each live variable at the entry and
exit of each block. It is recorded during the writeback phase
(`resolveRegisters()`) and is used to add resolution moves, as well as to
record the variable locations at the beginning of each block during code
generation.

Dumps and Debugging Support
---------------------------

The following dumps and debugging modes are provided:

-   Prior to register allocation

    -   Liveness (use, def, in, out) per block
    -   A tuple-like list of nodes in execution order

-   During `Interval` & `RefPosition` creation - `buildIntervals()`

    -   For each incoming arg: its type and incoming location
    -   For each instruction:
        - The current contents of the `defList`.
          This corresponds to all the nodes that have defined values
          that have not yet been consumed.
        - An abbreviated dump of the GenTree node.
        - A dump of each ref position as it is created.

-   After buildIntervals()
    -   A dump of all the `Interval`s and their `RefPosition`s
    -   A tuple-like list of nodes with their `RefPosition`s

-   During allocation
    -   A table of registers and their contents as allocation
        progresses.

-   After allocation
    -   A dump of `RefPosition`s, sequentially, and grouped for `lclVar`
        `Interval`s

-   During resolution
    -   A list of the candidates for resolution at each split, join or
        critical edge (i.e. the vars that are live) and where they
        reside (stack or reg)
    -   Any actual moves that are required

-   After resolution
    -   A table of registers and their contents, including spills made
        after each `RefPosition` was allocated.
    -   A tuple-like list of nodes with their assigned registers

LSRA Stress Modes
-----------------

The implementation uses the `COMPlus_JitStressRegs` environment variable.
The following are the stress modes associated with this variable. For
the most part they can be combined, though in some cases the values are
exclusive:

-   Limit the number of registers available for allocation (at most one
    of these can be specified):

    -   Limit to callee-regs (0x1)

    -   Limit to caller-save regs (0x2)

    -   Limit to a reduced set of both callee and caller-save regs (0x3)

    -   Note that some instructions require a large number of registers,
        and may require special handling (this is captured in the
        DEBUG-only field `RefPosition::minRegCandidateCount`, which is
        set during `buildRefPositionsForNode()`)

-   Modify register selection heuristics
    -   For free registers
        -   Reverse register "scoring" criteria (0x4)
        -   Prefer caller-save regs when the `Interval` spans a call (0x8)
    -   For spilling
        -   Spill the nearest-use instead of the farthest (subject to
            the correctness condition that registers can't be reused
            within the same node) (0x10)

-   Modify the order in which the basic blocks are traversed:
    -   Layout order (0x20)
    -   Pred-first order (0x40) -- the default
    -   "Random" order (0x60) -- not yet implemented
        -   For repeatability, the order should be pseudo-random, e.g.
            using a given skip and mod-divisor where the mod-divisor is
            larger than the maximum number of basic blocks.

-   Extend lifetimes to the entire method (0x80)
    -   Note that under MinOpts the lifetimes are already extended to the entire method,
        but MinOpts doesn't actually put any
        lclVars in registers, so this option is useful when the JIT
        is generating optimized code.

-   Modify the starting location (register/stack) for each variable at
    block boundaries (the default is to use the weightiest predecessor
    block that has already been allocated):
    -   Use the location from the previous block in layout order (0x100)
    -   Rotate the variable locations (0x200)

-   Always insert a `GTF_RELOAD` above a use of a spilled register (0x400).

-   Always spill (0x800). This mode is not fully functional, as there are
    cases where spill isn't actually supported (it should be possible to simply
    not spill in such cases).

-   Never allocate a register for a `RefPosition` marked `regOptional` (0x1000).

Assertions & Validation
-----------------------

There are many assertions in `LinearScan`. The following are the most
effective at identifying issues (i.e. they frequently show up in bugs):

-   The def and use counts don't match what's expected:
    -   See the asserts at the end of the `LinearScan::BuildNode()` method (these are
        architecture-specific, and can be found in lsraxarch.cpp, lsraarm64.cpp and lsraarm.cpp).
        -   This usually means that the `BuildXXX` method for this
            node is not building `RefPosition`s for all of its uses (which is what `consume` has been set to).
-   The liveness information is incorrect. This assert comes from `LinearScan::checkLastUses()` which
    does a reverse iteration over the `RefPosition`s for a block to verify that `bbLiveIn` is consistent with `bbLiveOut` updated with the defs and last uses:
    -   `assert(!foundDiff);`
-   No register is found, even with spilling.
    - `assert(farthestRefPhysRegRecord != nullptr);`
        - This means that the register requirements were over-constrained, either due to an error, or
          possibly because it is a stress mode, and the instruction hasn't correctly specified its
          minimum number of registers.

At the end of write-back (`resolveRegisters()`), `verifyFinalAllocation()` runs. It doesn't do a
lot of validation, but it prints the final allocation (including final spill placement), so is
useful for tracking down correctness issues.

Future Extensions and Enhancements
----------------------------------

The potential enhancements to the JIT, some of which are referenced in this document, can generally be found by [searching for LSRA in open issues](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+lsra+in%3Atitle). The ones that are focused on JIT throughput are labeled `JITThroughput`.

## Feature Enhancements

### Support for Allocating Consecutive Registers

This is [\#39457](https://github.com/dotnet/runtime/issues/39457). As described there, the challenge is to do this without impacting the common path. This should also include cleaning up the allocating of consecutive registers for `TYP_DOUBLE` for Arm32 [\#8758](https://github.com/dotnet/runtime/issues/8758).

## Code Quality Enhancements

### Merge Allocation of Free and Busy Registers

This is captured as [\#9399](https://github.com/dotnet/runtime/issues/9399)
Consider merging allocating free & busy regs.

Currently the register allocator will always allocate an available register, even if it only meets
the minimum requirements for the current node. This results in some really bad worst-case behavior
where a single scratch register is available, and is immediately spilled because it is killed prior
to the next use. Often this same register will be used and spilled many times before a better register
becomes free.

The alternative approach under consideration is to combine free and busy register allocation
(`tryAllocateFreeReg()` and `allocateBusyReg()`) such that a busy register will be spilled if there
are no suitable free registers, and the current `Interval` has greater weight than the `Interval`
occupying the register. This must be accompanied by some improvements in the efficiency of the
checks, so as not to degrade throughput. This is currently a work-in-progress (https://github.com/CarolEidt/runtime/tree/CombineAlloc), and needs
further work to eliminate diffs and improve throughput.

This would make it possible to spill a register for a higher weight `lclVar` rather than "settling"
for a register that's a poor fit for its requirements. This is probably the best approach to
address Issues [\#6824](https://github.com/dotnet/runtime/issues/6824):
Heuristics for callee saved reg allocation and
[\#8846](https://github.com/dotnet/runtime/issues/8846):
Let variables within a loop use register first.

The following issues are related:

-   Issues [\#6806](https://github.com/dotnet/runtime/issues/6806) and
    [\#6825](https://github.com/dotnet/runtime/issues/6825) track improvement of spill
    placement.

-   Issue [\#6705](https://github.com/dotnet/runtime/issues/6705) tracks the possibility of
    short-circuiting this evaluation.
    Making such an improvement should probably be done in conjunction with this work.

-   Issue [\#13466](https://github.com/dotnet/runtime/issues/13466):
    Inefficient register allocation in simple method which dereferences a span

### Auto-tuning of register selection

This is not yet captured as a GitHub issue.

This would be best done after [Merge Allocation of Free and Busy Registers](#merge-allocation-of-free-and-busy-registers).

The idea would be to add support to change the weight given to the various selection heuristics
according to a configuration specification, allowing them to be auto-tuned.

The scoring could be done using perf scores, or even using actual performance measurements,
though running actual benchmarks on each configuration could be quite costly.

In order to enable this capability without impacting throughput, it is likely that the configurability
would be added as an alternate path in the register allocator, leaving the default path as-is.

### Support for Multi-register Instructions

There are a couple of distinct opportunities here:

-   On Arm64, there are intrinsics that require the register allocator to allocate
    continuous registers. This could be considered a generalization of the support for
    doubles on Arm32, though some refactoring is probably required to ensure that
    it doesn't hurt throughput for single-register allocation. This is Issue [\#39457](https://github.com/dotnet/runtime/issues/39457).

-   Other intrinsics, as well as long multiply on X86 and Arm32, require allocating
    multiple register targets, but do not require that they are contiguous. Now
    that we have support for multi-reg structs, this should be relatively straightforward.

### Pre-allocating high frequency lclVars

This is captured as Issue [\#8019](https://github.com/dotnet/runtime/issues/8019)
Consider pre-allocating high-frequency lclVars.
A fix for this might also address [\#13466](https://github.com/dotnet/runtime/issues/13466).


The idea here is to ensure that high frequency lclVars aren't forced to use less-than-optimal
registers (or worse, spilled), by allocating them ahead of the linear scan.

This requires a mechanism for recording the location of these lclVars. The LinearScan
implementation doesn't keep a reservation table for the physical registers; instead it
maintains the current status of each register (i.e. which `Interval` it is allocated to)
as it proceeds during allocation or write-back. While keeping a reservation table may be
a reasonable approach for Tier 1 or higher
(see [Spanning trees for physical registers](#spanning-trees-for-physical-registers)),
an alternate approach would be to only perform
pre-allocation at block granularity, including block boundaries (for which the variable
locations are recorded in the `varToRegMaps`). Without an interference graph or a reservation
table, it will likely be limited to a small number of the highest frequency lclVars.

One strategy would be to do something along the lines of (appropriate hand-waving applies):

  - Set aside a subset of the registers for pre-allocation. This might include
    all of the callee-save registers, and a small number of caller-save registers.
    Some tuning would likely be required to determine exactly what and how many to reserve.

  - After `buildIntervals()` has completed,for each register candidate local `Interval`,
    in sorted order, assign a register that corresponds to its `registerPreferences`. If such
    a register is not available, stop pre-allocating. This becomes its `assignedInterval`
    (which will be inactive at the start of alloaction).

  - During `allocateRegisters()` as each new block is encountered, instead of simply copying
    the predecessor `varToRegMap`, iterate over the most frequently lclVars in the union of the
    live-in, uses and defs, and displace any `Intervals` that are occupying registers that
    would be more profitably used by the high-frequencly lclVars, weighing spill costs.

### <a name="avoid-split"></a>Avoid Splitting Loop Backedges

This is captured as Issue [\#9909](https://github.com/dotnet/runtime/issues/9909).

When the register allocator performs resolution across block boundaries, it may split critical
edges (edges from a block with multiple successors to a block with multiple predecessors).
This can seriously impact performance, and is especially bad when the edge is a loop backedge.

One option would be to avoid edge splitting altogether. Instead, we would construct `CriticalEdgeSet`s that
would include the set of critical edges that have any edges in common.
For these, once we have allocated the first block that contains either an in-edge or an out-edge in such
a set, the variable locations on that edge would be fixed, and future blocks that have edges in that
set would ensure that the variable locations match. This would eliminate not just backedge splitting,
but all the extra branches currently inserted for resolution. It remains to be seen whether this would
outweigh the impact of cases where more resolution moves would be required.

I have an old experimental branch where I started working on this:
https://github.com/CarolEidt/runtime/tree/NoEdgeSplitting. It was ported from the coreclr to
the runtime repo, but not validated in any significant way.
Initial experience showed that this resulted in more regressions than improvements.

A less aggressive approach would be to make minor modifications to reduce the
need for split edges:

* When selecting a predecessor for the fall-through block for a loop backedge,
  instead of using the actual predecessor, use the non-backedge predecessor of
  the loop head. This will mean that both successors of the loop will use the same
  mapping, reducing the need to split the edge.

* When a `RefTypeExpUse` is encountered, if it is at a loop backedge, the variable
  is in a register, and the loop head has the variable on the stack, spill it.
  Spilling preemptively will enable the spill to be performed at an actual reference,
  rather than at the block boundary.

This approach has been implemented experimentally in https://github.com/CarolEidt/runtime/tree/AvoidSplittingBackedge.
Running crossgen diffs across frameworks and benchmarks for X64 Windows shows
a delta of -2392 (-0.007%) with 317 methods improved and 66 regressed.
Further analysis of the regressions is needed.

Issues [\#8552](https://github.com/dotnet/runtime/issues/8552) and [\#40264](https://github.com/dotnet/runtime/issues/40264) may be related.

### Enable EHWriteThru by default

When `COMPlus_EnableEHWriteThru` is set, some performance regressions are observed. When an EH write-thru variable (i.e. one that is live into an exception region) is defined, its value is
always stored, in addition to potentially remaining live in a register. This increases register
pressure which may result in worse code.

Further investigation is needed, but the following mitigations may be effective (here the
term "EH Var" means a `lclVar` marked `lvLiveInOutOfHndlr`):

-   Adjust the heuristics:

    1. For determining whether an EH var should be a candidate for register allocation,
       e.g. if the defs outweigh the uses.

       - An initial investigation might only consider an EH var as a register candidate if it has a single use. One complication is that we sometimes generate better code for a non-register-candidate local than one that is always spilled (we don't support `RegOptional` defs).
       Thus, it would be better to identify *before* building intervals whether we should consider it a candidate, but the problem with that is that we don't necessarily know at that
       time whether there is a single def. A possible approach:

            - Add an `isSingleDef` flag to `Interval`.
            - When allocating a use of a `writeThru` interval:
                - If it's marked `isSingleDef`, allocate as usual.
                - Otherwise, if it's `RegOptional`, don't allocate.
                - Otherwise, allocate a register but spill it immediately.

    2. For determining when a definition of an EH var should be only stored to the stack,
       rather than also remaining live in the register.

        -   If the weight of the defs exceeds the weight of the blocks with successors in exception
        regions, consider spilling the `lclVar` to the stack only at those boundaries.

The original issue to enable EH WriteThru is [#6212](https://github.com/dotnet/runtime/issues/6212).
It remains open pending the resolution of the performance regressions.

### Avoid Spill When Stack Copy is Valid

The idea here is to avoid spilling at a use if the value on the stack is already the correct value.

Issues that this might address include:
- [\#7994](https://github.com/dotnet/runtime/issues/7994) Spill single-def vars at def,
- [\#6825](https://github.com/dotnet/runtime/issues/6825) Improve spill placement, and
- [\#6761](https://github.com/dotnet/runtime/issues/6761) Avoiding reg spill to memory when reg-value is consistent with memory.

Currently the register allocator doesn't track whether a local variable has the same value on the stack
as in a register. The support for "write-thru" EH variables (variables live across exception
boundaries) has added the capability to liveness analysis and code generation (in addition to the register allocator)
to handle variables that are live in both registers and on the stack. This support could be further leveraged
to avoid spilling single-def variables to memory if they have already been spilled at their
definition.

Extending such support to more generally track whether there is already a valid stack copy involves more
work. Fully general support would require such information at block boundaries, but it might be worth
investigating whether it would be worthwhile and cheaper to simply track this information within a block.

### Rematerialization

This would involve identifying `Interval`s whose values are cheaper to recompute than to spill
and reload. Without SSA form, this would probably be easiest to do when there's a single def.
Issue [\#6264](https://github.com/dotnet/runtime/issues/6264).

### Improving Reg-Optional Support

#### Reg-Optional Defs

Issues [\#6862](https://github.com/dotnet/runtime/issues/6862) and
[\#6863](https://github.com/dotnet/runtime/issues/6863) track the
 proposal to support "folding" of operations using a tree temp when
the defining operation supports read-modify-write (RMW) to memory.
This involves supporting the possibility
of a def being reg-optional, as well as its use, so that it need
never occupy a register.

I have an old experimental branch: https://github.com/CarolEidt/coreclr/tree/RegOptDef
where I started working on this, and it is in the process of being ported to the runtime repo.

#### Don't Pre-determine Reg-Optional Operand

Issue [\#6358](https://github.com/dotnet/runtime/issues/6358)
tracks the problem that `Lowering` currently has
to select a single operand to be reg-optional, even if either
operand could be. This requires some additional state because
LSRA can't easily navigate from one use to the other to
communicate whether the first operand has been assigned a
register.

#### Don't Mark DelayFree for Duplicate Operands

Issue [\#9896](https://github.com/dotnet/runtime/issues/9896).

### Improving Preferencing

-   Issues [#36454](https://github.com/dotnet/runtime/issues/36454),
    [#11260](https://github.com/dotnet/runtime/issues/11260) and
    [#12945](https://github.com/dotnet/runtime/issues/12945)
    involve preferencing for HW intrinsics.

-   Issue [#11959](https://github.com/dotnet/runtime/issues/11959) also has a pointer
    to some methods that could benefit from improved preferencing.

-   Issue [#13090](https://github.com/dotnet/runtime/issues/13090) involves a case where anti-preferencing might be useful.

-   Issue [#10296](https://github.com/dotnet/runtime/issues/10296) may also be related to preferencing, if it is still an issue.

### Leveraging SSA form

This has not yet been opened as a github issue.

Making SSA form available to LSRA would:

  - Allow splitting of `Interval`s where SSA names are not related by a phi node.

  - Ease potential future support for "General SSA" (where sources and dests of phi nodes may overlap)
    as existing support for resolution at block boundaries could be easily extended to introduce minimal copies for translating out of General SSA form.

### Spanning trees for physical registers

This has not yet been opened as a github issue.

LLVM has extended their linear scan register allocator with something it
calls "Greedy Register Allocation"[[6](#6),[7](#7)]. This uses a priority queue for the
order of allocation (sorted by decreasing spill cost), and a B+ tree to
represent each physical register. I think that using the B+ trees for
physical registers would be an improvement over the current PhysRegs,
and we may want to experiment with changing the allocation order as
well. It would not be necessary to significantly modify the process of
creating `Interval`s, nor the resolution phase.

### Improve the handling of def/use conflicts

Def/use conflicts arise when the producing and conusming nodes each have register requirements,
and they conflict. The current mechanism, in which the register assignment of one of the
`RefPosition`s is changed, can lead to problems because there's then
no associated `RefTypeFixedReg` for that reference. This is Issue [\#10196](https://github.com/dotnet/runtime/issues/10196).

A related issue is Issue [\#7966](https://github.com/dotnet/runtime/issues/7966), which captures an issue with propagating a fixed-register use to its definition, if the value is
defined by a node with delay-free operands.

## Throughput Enhancements

### Allocation Window for Min-Opts and Tier 0

Currently, the register allocator builds the RefPositions for the entire method at once.
In Min-Opts and Tier 0, or any time there are no local variables available for enregistration,
there is no value in doing so, as no values remain live beyond the top-level node of an
expression. Since the JIT doesn't do instruction scheduling or other reordering of nodes
after `Lowering`, it is generally the case that the live-ranges for the values produced by
nodes of disjoint top-level trees do not overlap.

Performing the entire allocation (building, allocation and write-back) on a single expression
at a time, and maintaining pools of data structures for reuse, could improve
throughput and reduce memory consumption.

As the register allocator builds the `RefPosition`s, it keeps track of outstanding values in the
form of a `defList` that holds all of the tree temp values that have been defined but not yet used.
Once this is empty, the register allocator could process the current list of `RefPosition`s and then
start over.

[Issue \#6690](https://github.com/dotnet/runtime/issues/6690) proposes to build `RefPositions` incrementally, which is part of this item.

### Distinguish Intra-Block versus Inter-Block Variables

It is unclear whether it would be beneficial, but if we could keep track of the variables that are
only used within a block (presumably true of many introduced temps), we may find that we could
continue to limit the number of variables whose liveness is tracked across blocks, keeping an expanded
set only for transient liveness. Issue [\#7992](https://github.com/dotnet/runtime/issues/7992).

Note that this would only improve JIT throughput for optimized code.

### Improve the VarToRegMap

The `VarToRegMap` incurs non-trivial JIT-time overhead.
Issue [\#8013](https://github.com/dotnet/runtime/issues/8013) addresses
the question of whether there is an alternative that would have better
performance. This would also improve JIT throughput only for optimized code.

### Other Throughput Investigations

Issue [\#7998](https://github.com/dotnet/runtime/issues/7998) suggests evluating the throughput cost of updating the preferences at each
kill site.

## Test and Cleanup Issues

Issue [\#9767](https://github.com/dotnet/runtime/issues/9767) captures the issue that the
"spill always" stress mode, `LSRA_SPILL_ALWAYS`, `COMPlus_JitStressRegs=0x800` doesn't work properly.

Issue [\#6261](https://github.com/dotnet/runtime/issues/6261) has to do with `RegOptional`
`RefPositions` that are marked as `copyReg` or `moveReg`. See the notes on this issue;
I don't think such cases should arise, but there may be some cleanup needed here.

Issue [\#5793](https://github.com/dotnet/runtime/issues/5793) suggests adding a stress mode that
allocates registers for multi-reg nodes in the reverse of the ABI requirements.

Issue [#10691](https://github.com/dotnet/runtime/issues/10691) suggests adding a stress mode that
deliberately trashes registers that are not currently occupied (e.g. at block boundaries).

References
----------

1.  <a id="1"></a> Boissinot, B. et
    al "Fast liveness checking for ssa-form programs," CGO 2008, pp.
    35-44.
    http://portal.acm.org/citation.cfm?id=1356058.1356064&coll=ACM&dl=ACM&CFID=105967773&CFTOKEN=80545349

2.  <a id="2"></a> Boissinot, B. et al, "Revisiting
    Out-of-SSA Translation for Correctness, Code Quality and
    Efficiency," CGO 2009, pp. 114-125.
    <http://portal.acm.org/citation.cfm?id=1545006.1545063&coll=ACM&dl=ACM&CFID=105967773&CFTOKEN=80545349>


3.  <a id="3"></a>Wimmer, C. and Mössenböck, D. "Optimized
    Interval Splitting in a Linear Scan Register Allocator," ACM VEE
    2005, pp. 132-141.
    <http://portal.acm.org/citation.cfm?id=1064998&dl=ACM&coll=ACM&CFID=105967773&CFTOKEN=80545349>

4.  <a id="4"></a> Wimmer, C. and Franz, M. "Linear Scan
    Register Allocation on SSA Form," ACM CGO 2010, pp. 170-179.
    <http://portal.acm.org/citation.cfm?id=1772979&dl=ACM&coll=ACM&CFID=105967773&CFTOKEN=80545349>

5.  <a id="5"></a> Traub, O. et al "Quality and Speed in Linear-scan Register
    Allocation," SIGPLAN '98, pp. 142-151.
    <http://portal.acm.org/citation.cfm?id=277650.277714&coll=ACM&dl=ACM&CFID=105967773&CFTOKEN=80545349>

6.  <a id="6"></a> Olesen, J. "Greedy Register Allocation in LLVM 3.0," LLVM Project Blog, Sept. 2011.
    <http://blog.llvm.org/2011/09/greedy-register-allocation-in-llvm-30.html>
    (Last retrieved July 2020)

7.  <a id="7"/></a> Yatsina, M. "LLVM Greedy Register Allocator," LLVM Dev Meeting, April 2018.
    <https://llvm.org/devmtg/2018-04/slides/Yatsina-LLVM%20Greedy%20Register%20Allocator.pdf>
    (Last retrieved July 2020)
