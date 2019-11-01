Linear Scan Register Allocation: Design and Implementation Notes
===================
Table of Contents
-----------------

[Overview](#overview)

[Preconditions](#preconditions)

[Post-Conditions](#post-conditions)

[LSRA Phases](#lsra-phases)

[Key Data Structures](#key-data-structures)

[Dumps and Debugging Support](#dumps-and-debugging-support)

[LSRA Stress Modes](#lsra-stress-modes)

[Assertions & Validation](#assertions-validation)

[Future Extensions and Enhancements](#future-extensions-and-enhancements)

[References](#references)

Overview
--------

This document provides additional detail on the linear scan register
allocator (LSRA) in RyuJIT. It is expected that the reader has already
read the [RyuJIT Overview document](https://github.com/dotnet/coreclr/blob/master/Documentation/botr/ryujit-overview.md).

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
            as necessary by the resolution phase. When not optimizing
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

    -   For each node, `RefPosition`s are built to reflect the uses,
        definitions and kills of any registers involved in the
        evaluation of the node.

-   The allocation phase iterates over the `RefPosition` list:

    -   At the beginning of each new block the register assignment of
        each lclVar `Interval` is updated, either based on incoming
        argument locations, or based on the location at the end of the
        selected predecessor block.

    -   Iteration proceeds according to ascending order of `RefPosition`s.
        This means that the register assignment is performed in linear
        execution order.

    -   The status of an `Interval` is updated as a `RefPosition` is
        encountered for it.

    -   Splitting or spilling an `Interval` doesn't involve creating a new
        one. Instead, the `RefPosition` simply gets a new assignment, and
        is either marked for reload/copy or its location is simply
        updated in the incoming map.

-   The resolution phase has two parts:

    -   The `RefPosition`s are walked again to write back the register
        assignments to the `GenTree` nodes.

        -   It is at this point that the register assignments for the
            lclVars is finalized at `BasicBlock` boundaries.

    -   Then the block boundaries are handled:

        -   For fork edges (the source block has multiple targets, but
            each target has only that one source), any required
            resolution is placed at the target.

        -   For join edges (a single target block has multiple sources,
            but each source has only that one target), any required
            resolution is placed at the source.

        -   Critical edges require more complicated handling, and may
            require splitting of the edge for placement of resolution.

            -   It may be that it would be more efficient to simply
                spill those lclVars that require the split edge.

Preconditions
-------------

### Lowered IR Form (LIR)

The RyuJIT backend components (register allocation and code generation)
are somewhat unusual in that the IR nodes do not map directly to target
instructions. Instead, the `Lowering` phase of the JIT first performs
IL transformations to ensure that the operations match the semantic
level of the target such that all registers are explicitly represented
in the IL:

-   This may not result in a 1:1 correlation between nodes and target
    instructions, as some nodes (e.g. constants) may not require a
    register, and other nodes (e.g. those representing addressing modes)
    may become part of the parent instruction at code generation time.

It is the job of the `Lowering` phase to transform the IR such that:

-   The nodes are in `LIR` form (i.e. all expression trees have been
    linearized, and the execution order of the nodes within a BasicBlock
    is specified by the `gtNext` and `gtPrev` links).

-   Any node that will be evaluated as part of its parent (consumer)
    node have been identified. These are known as "contained" nodes, and
    are identified by setting the `GTF_CONTAINED` flag in the
    `gtFlags` field of the node.

-   Similarly, nodes that represent values that could be referenced from
    memory by their consumer (i.e. via an addressing mode) are marked as
    "reg-optional" (`LIR::Flags::RegOptional`)

    -   Issues [\#7752](https://github.com/dotnet/coreclr/issues/7752) and [\#7753](https://github.com/dotnet/coreclr/issues/7753) track the proposal to support "folding"
        of operations using a tree temp, i.e. supporting the possibility
        of a def being reg-optional, as well as its use, so that it need
        never occupy a register.

    -   Issue [\#6361](https://github.com/dotnet/coreclr/issues/6361) tracks the problem that `Lowering` currently has
        to select a single operand to be reg-optional, even if either
        operand could be. This requires some additional state because
        LSRA can't easily navigate from one use to the other to
        communicate whether the first operand has been assigned a
        register.

-   All unused values (nodes that produce a result that is not consumed)
    are identified (`gtLIRFlags` has the `LIR::Flags::UnusedValue` bit set)

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

The register requirements are determined by the `TreeNodeInfoInit()`
method, which is called for each node just prior to building its
`RefPosition`s.

The register lifetimes must obey the following lifetime model:

-   First, any internal registers are defined.

-   Next, any source registers are used (and are then freed if they are
    last use and are not identified as `delayRegFree`).

-   Next, the internal registers are used (and are then freed).

-   Next, the destination register(s) are defined

-   Finally, any `delayRegFree` source registesr are freed.

There are several things to note about this order:

-   The internal registers will never overlap any use, but they may
    overlap a destination register.

-   Internal registers are never live beyond the node.

-   The `delayRegFree` annotation is used for instructions that are
    only available in a Read-Modify-Write form. That is, the destination
    register is one of the sources. In this case, we must not use the
    same register for the non-RMW operand as for the destination.

Post-Conditions
---------------

After LSRA, the graph has the following properties:

-   The `gtRegNum` of each tree node contains the allocated register,
    if any. Nodes that produce multiple registers are similarly
    assigned, via extended register number fields. If the node does not
    produce a value directly (i.e. it is either of void type, or it is
    evaluated as part of its parent) its gtRegNum is set to REG_NA.

    -   In most cases, this register must satisfy the constraints
        specified by the `NodeInfo`.

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
        and its current location does not satisfy that requirement, LSRA
        must insert a `GT_COPY` node between the node and its parent. 
        The gtRegNum on the `GT_COPY` node must satisfy the register
        requirement of the parent.

-   GenTree::gtRsvdRegs has a set of registers used for internal temps.
    These must satisfy the constraints given by the `NodeInfo`.

-   A tree node is marked `GTF_SPILL` if the tree node must be spilled by
    the code generator after it has been evaluated.

-   A tree node is marked `GTF_SPILLED` if it is a lclVar that must be
    reloaded prior to use.

    -   The register (gtRegNum) on the node indicates the register to
        which it must be reloaded.

    -   For lclVar nodes, since the uses and defs are distinct tree
        nodes, it is always possible to annotate the node with the
        register to which the variable must be reloaded.

    -   For other nodes, since they represent both the def and use, if
        the value must be reloaded to a different register, LSRA must
        insert a `GT_RELOAD` node to specify the register to which it
        should be reloaded.

-   Local variable table (`LclVarDsc`):

    -   `LclVarDsc::lvRegister` is set to true if a local variable has the
        same register assignment for its entire lifetime.

    -   `LclVarDsc::lvRegNum` is initialized to its initial register
        assignment.

        -   For incoming parameters, this is the register to which
            `genFnPrologCalleeRegArgs()` will move it.

    -   Codegen will set `lvRegNum` to its current value as it processes
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

        -   It is unclear whether it would be beneficial, but if we
            could keep track of the variables that are only used within
            a block (presumably true of many introduced temps), we may
            find that we could continue to limit the number of variables
            whose liveness is tracked across blocks, keeping an expanded
            set only for transient liveness. [Issue \#11339](https://github.com/dotnet/coreclr/issues/11339)

-   `Compiler::fgLocalVarLiveness`

    -   This computes the `BasicBlock::bbLiveIn` and
        `BasicBlock::bbLiveOut` sets that are used by LSRA.

    -   It does this for an lclVar marked `lvTracked`, even if it is
        not a register candidate. The dataflow information is also used
        for GC info, but it may be the case that some of these should
        not be marked during the pre-LSRA dataflow analysis.

-   `LinearScan::identifyCandidates`

    -   This mostly duplicates what is done in
        `Compiler::lvaMarkLocalVars(). There are differences in what
        constituted a register candidate vs. what is tracked, but we
        should probably handle them in ` Compiler::lvaMarkLocalVars()`
        when it is called after `Lowering`.

    -   It sets the ` lvLRACandidate` flag on lclVars that are going
        to be register candidates.

### Block Ordering

The determination of block ordering is done during the building of
`RefPosition`s. However, it is a logically distinct component. Its
objective is to identify a sequence of `BasicBlock`s for allocation
that satisfies the following properties:

-   Each block comes after at least one of its predecessors, ideally the
    one on the edge with the greatest weight.

    -   We use block weight, since edge weight is not tracked in the
        JIT.

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

### Register Requirements

Like block ordering, this is done during the building of
`RefPosition`s, but is a logically separate component.

-   Determine register requirements (Lowering::TreeNodeInfoInit)

    -   This method is responsible for initializing the `NodeInfo` for
        each node.

        -   This is currently maintained in the `gtLsraInfo` for each
            node.

        -   The nodes are placed into a map, that maps from a tree node
            to the list of tree nodes that are the actual operands.

            -   This complication is due to contained nodes, where, e.g.
                a `GT_IND` node may be contained, and have a
                `GT_LEA` child whose base and addr nodes are the
                actual operands.

            -   Work is underway to eliminate the `gtLsraInfo`, and to
                place the `NodeInfo` of the actual operands directly
                into the map, eliminating the need for two level (map of
                lists) data structure [Issue \#7225](https://github.com/dotnet/coreclr/issues/7225)

            -   Subsequent work will eliminate the `NodeInfo`
                altogether, and instead build`RefPosition`s directly
                from the `TreeNodeInfoInit` methods [Issue \#7257](https://github.com/dotnet/coreclr/issues/7257)

    -   The `NodeInfo` includes:

        -   The number of register sources and destinations.

        -   Register restrictions (candidates) for the register(s) defined by the node, if any.
            These restrictions are specified separately as those imposed by this, the defining node
            (`dstCandidates`) and those imposed by the consuming or parent node (`srcCandidates`).

            -   At the time the consuming node is being handled by `TreeNodeInfoInit()`, the `Interval`
                for the defining node (if it is a "tree temp") has already been created, as has the
                defining (`RefTypeDef`) `RefPosition`.
                Any conflicts between the `dstCandidates` on the sources, and the `srcCandidates` at
                the consuming node will be handled when the consuming `RefPosition` (`RefTypeUse`) is
                created.

        -   The number (internalCount) of registers required, and their
            register restrictions (candidates). These are neither inputs
            nor outputs of the node, but used in the sequence of code
            generated for the tree.

            -   At one point, we had planned to eliminate internal
                registers, as they do not precisely represent register
                lifetimes. However, it is felt that the complexity and
                code expansion from eliminating them would outweigh any
                benefit.

-   Register allocation (doLinearScan)

    -   Iterate over the linear ordering of nodes within each
        `BasicBlock`:

        -   Build `Interval`s with annotations

            -   Ref positions (called use positions by [[1]](#[1]) at each
                def or use, and for internal registers.

            -   Add cross-interval preferences

                -   This area has room for improvement [Issue #11463](https://github.com/dotnet/coreclr/issues/11463)

            -   Determine whether to prefer callee-save registers. [Issue
                \#7664](https://github.com/dotnet/coreclr/issues/7664) tracks the need to improve the heuristics for
                when to prefer callee-save.

            -   Add pseudo-uses to the `Interval` if live at loop backedge.

            -   If the previous block is not a predecessor of the
                current block, add DummyDefs for variables that are
                live-in to the current block, but not live-out of the
                previous block.

                -   This is generally not required, as the block will
                    normally have a predecessor block that has already
                    been allocated.

        -   Future improvements:

            -   Identify single-def variables (including arguments) --
                these are candidates for store-at-def spilling. [Issue
                \#11344](https://github.com/dotnet/coreclr/issues/11344).

            -   Identify candidates for recomputation -- values which
                are idempotent and can be more easily recomputed than
                spilled & reloaded [Issue \#6131](https://github.com/dotnet/coreclr/issues/6131)

        -   Add appropriate ref positions for fixed registers (argument
            registers, volatile/caller-save, write barriers, dedicated
            registers)

-   Iterate over `RefPosition`s in forward order, performing linear scan
    allocation. The algorithm below is a modified version of that
    specified in [[3]](#[3]), and doesn't include optimizations such as early
    exit of the loops when the remaining `Interval`s will be out of range.
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
        if (refPosition is BasicBlockBoundary) ProcessBlockBoundaryAlloc()
        Interval currentInterval = refPosition.Interval;
        if (currentInterval->hasValidAssignment())
        {
            refPosition->setReg(currentInterval->physReg);
        }
        // find a register for currentInterval
        // AllocateBlockedReg will take into account whether it must have
        // a register in this position, and may otherwise choose not to
        // allocate if existing intervals have higher priority.
        // A split interval will be added to the unhandled list
        // A split interval will be added to the unhandled list
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
        parameters [Issue \#11356](https://github.com/dotnet/coreclr/issues/11356)

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
            which is not currently live)

    -   It always uses the same order for iterating over the registers.
        The jit32 register allocator used a different ordering for tree
        temps than for lclVars. It's unclear if this matters for LSRA,
        but [Issue \#11357](https://github.com/dotnet/coreclr/issues/11357) tracks this question.

    -   It currently fully evaluates the "goodness" of each register.
        [Issue \#7301](https://github.com/dotnet/coreclr/issues/7301) tracks the possibility of short-circuiting this evaluation.

-   `AllocateBusyReg()` iterates over all the registers trying to find the
    best register to spill (it must only be called if `tryAllocateFreeReg()`
    was unable to find one):

    -   It takes into account the following:

        -   The distance to the next use of the `Interval` being
            spilled

        -   The relative weight of the `Interval`s.

        -   Whether the `RefPosition` being allocated, or the one
            potentially being spilled, is reg-optional

    -   It will always spill an `Interval` either at its most recent
        use, or at the entry to the current block.

        -   Issues [\#7609](https://github.com/dotnet/coreclr/issues/7609) and [\#7665](https://github.com/dotnet/coreclr/issues/7665) track improvement of spill
            placement.

    -   It is quite possible that combining `TryAllocateFreeReg()` and
    `AllocateBusyReg()` might be more effective [Issue \#15408](https://github.com/dotnet/coreclr/issues/15408).

-   Resolution

    -   Perform resolution at block boundaries, adding moves as needed
        (the algorithm for resolution can be found in [[2]](#[2]), though note
        that there is a typo: in the last 'if' statement, it should be
        "if b != loc(pred(b))" instead of "if b = loc(pred(b))"):

        -   First, any variables that are in registers on the incoming
            edge, but need to be on the stack, are stored, freeing those
            registers

        -   Next, any register-to-register moves are done, using a
            standard algorithm.

            -   If there are circular dependencies, swap is used when
                available, and a temp register is used otherwise.

        -   Finally, any stack to register moves are done.

    -   Resolution of exception edges

        -   This is currently done by ensuring that any variable that's
            live in to an exception region is maintained on stack.

        -   Issue \#6001 raises the performance issue due to this
            implementation.

            -   An initial approach, for which some initial
                implementation has been done, is to support the notion
                of "write-thru" variables; for these, all definitions
                would write to memory, but uses could use a register
                value, if available.

                -   The value would be reloaded at exception boundaries.

            -   Support for "write-thru" variables could be extended to
                single-def variables; if they are spilled at their
                single definition, they need never be spilled again
                (issue \#7465).

<!-- -->

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

This is the same bbLiveIn set as used elsewhere in the JIT but is
recomputed prior to register allocation because `Lowering` creates new
variables.

### Live

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

A structure that represents a variable, optimizer temp, or local temp,
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

### RefPosition

A `RefPosition` represents a single use, def or update of an
enregisterable variable or temporary. It contains

-   The `Interval` or `RegRecord` to which this reference belongs.

-   The next `RefPosition` (in code order) for the `Interval` or `RegRecord`.
    This is used when determining which register to spill.

-   Register assignment

    -   Prior to allocation, this represents the set of valid register
        for this reference

    -   After allocation, this is a single element register mask
        including only the allocated register (or two registers in the
        ARM implementation for longs).

-   The type of `RefPosition`:

    -   `RefTypeDef` is a pure definition of an `Interval` .

    -   `RefTypeUse` is a pure use of an `Interval` .

    -   `RefTypeKill` is a location at which a physical register is
        killed. These only exist on `RegRecord`s, not on `Interval`s

        -   Note that this type is probably not needed -- see especially
            notes about physical registers in "future" section.

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
        zero

### Tree Nodes

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
(resolveRegisters) and is used to add resolution moves, as well as to
record the variable locations at the beginning of each block during code
generation.

This map incurs non-trivial JIT-time overhead. Issue \#11396 addresses
the question of whether there is an alternative that would have better
performance.

Dumps and Debugging Support
---------------------------

The following dumps and debugging modes are provided:

-   Prior to register allocation

    -   Liveness (use, def, in, out) per block
    -   A tuple-like list of nodes in execution order

-   During `Interval` & `RefPosition` creation - `buildIntervals()`

    -   For each incoming arg: its type and incoming location
    -   For each instruction:
        - The current contents of the `OperandToLocationInfoMap`. This corresponds to all the nodes that have defined values
        that have not yet been consumed.
        - An abbreviated dump of the GenTree node.
        - The `TreeNodeInfo` generated for it.
        -   A dump of each ref position as it is created.

-   After buildIntervals()
    -   A dump of all the `Interval`s and their `RefPosition`s
    -   A tuple-like list of nodes with their `RefPosition`s

-   During allocation
    -   A table of registers and their contents as allocation
        progresses.

-   After allocation
    -   A dump of `RefPosition`s, sequentially, and grouped for Var
        `Interval` s

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

The implementation uses the COMPLUS_JitStressRegs environment variable.
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
    -   This is done under MinOpts, but MinOpts doesn't actually put an
        lclVars in registers.

-   Modify the starting location (register/stack) for each variable at
    block boundaries (the default is to use the weightiest predecessor
    block that has already been allocated):
    -   Use the location from the previous block in layout order (0x100)
    -   Rotate the variable locations (0x200)

Assertions & Validation
-----------------------

There are many assertions in `LinearScan`. The following are the most
effective at identifying issues (i.e. they frequently show up in bugs):

-   The node information isn't showing the number of consumed registers
    that are expected:
    -   `assert(((consume == 0) && (produce == 0)) || (ComputeAvailableSrcCount(tree) == consume));`
        -   This usually means that the `TreeNodeInfoInit` method for this
            node is not correctly setting its `srcCount` (which is what `consume` has been set to).
-   No register is found, even with spilling.
    - `assert(farthestRefPhysRegRecord != nullptr);`
        - This means that the register requirements were over-constrained, either due to an error, or
          possibly because it is a stress mode, and the instruction hasn't correctly specified its
          minimum number of registers.

At the end of write-back (`resolveRegisters()`), `verifyFinalAllocation()` runs. It doesn't do a lot of validation, but it
    prints the final allocation (including final spill placement), so is useful for tracking down correctness issues.

Future Extensions and Enhancements
----------------------------------

The potential enhancements to the JIT, some of which are referenced in this document, can generally be found by [searching for LSRA in open issues](https://github.com/dotnet/coreclr/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+LSRA+in%3Atitle). The ones that are focused on JIT throughput are labeled `JITThroughput`.

The following haven't yet been opened as github issues:

### Leveraging SSA form

Making SSA form available to LSRA would:

  - Allow splitting of `Interval`s where SSA names are not related by a phi node.

  - Ease potential future support for "General SSA" (where sources and dests of phi nodes may overlap), as existing support for resolution at block boundaries could be easily extended to introduce minimal copies for translating out of General SSA form.

### Spanning trees for physical registers

LLVM has replaced their linear scan register allocator with something it
calls "Greedy Register Allocation". This uses a priority queue for the
order of allocation (sorted by decreasing spill cost), and a B+ tree to
represent each physical register. I think that using the B+ trees for
physical registers would be an improvement over the current PhysRegs,
and we may want to experiment with changing the allocation order as
well. It would not be necessary to significantly modify the process of
creating `Interval`s, nor the resolution phase.

References
----------

1.  <a name="[1]"/> Boissinot, B. et
    al "Fast liveness checking for ssa-form programs," CGO 2008, pp.
    35-44.
    http://portal.acm.org/citation.cfm?id=1356058.1356064&coll=ACM&dl=ACM&CFID=105967773&CFTOKEN=80545349

2.  <a name="[2]"/> Boissinot, B. et al, "Revisiting
    Out-of-SSA Translation for Correctness, Code Quality and
    Efficiency," CGO 2009, pp. 114-125.
    <http://portal.acm.org/citation.cfm?id=1545006.1545063&coll=ACM&dl=ACM&CFID=105967773&CFTOKEN=80545349>


3.  <a name="[3]"/>Wimmer, C. and Mössenböck, D. "Optimized
    Interval Splitting in a Linear Scan Register Allocator," ACM VEE
    2005, pp. 132-141.
    <http://portal.acm.org/citation.cfm?id=1064998&dl=ACM&coll=ACM&CFID=105967773&CFTOKEN=80545349>

4.  <a name="[4]"/> Wimmer, C. and Franz, M. "Linear Scan
    Register Allocation on SSA Form," ACM CGO 2010, pp. 170-179.
    <http://portal.acm.org/citation.cfm?id=1772979&dl=ACM&coll=ACM&CFID=105967773&CFTOKEN=80545349>

5.  <a name="[5]"/> Traub, O. et al "Quality and Speed in Linear-scan Register
    Allocation," SIGPLAN '98, pp. 142-151.
    <http://portal.acm.org/citation.cfm?id=277650.277714&coll=ACM&dl=ACM&CFID=105967773&CFTOKEN=80545349>

6.  <a name="[6]"/> Olesen, J. "Greedy Register Allocation in LLVM 3.0," LLVM Project Blog, Sept. 2011.
    <http://blog.llvm.org/2011/09/greedy-register-allocation-in-llvm-30.html>
    (Last retrieved Feb. 2012)
