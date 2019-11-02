Improving LSRA Throughput
=========================

There are a number of ways in which the current implementation of linear scan register allocation (LSRA) is sub-optimal:
* I'm not certain that the extra pass that enumerates the nodes before the current `TreeNodeInfoInit` pass must be separate.
  Further investigation is needed.
* The identification of opportunities for "containment" (i.e. where the computation of a node's result can be folded into the parent,
such as a load or store) is done during `Lowering` and communicated to the register allocator via a `gtLsraInfo` field on the node, that
is otherwise unused, and is basically duplicated when the `RefPosition`s are built for the node.
  * A more efficient representation of "containment" could allow this to remain in `Lowering`, where existing transformations already
    take into account the parent context.
    * This would also have the additional benefit of simplifying the containment check, which is done at least once for each node
      (at the beginning of `CodeGen::genCodeForTreeNode()`), and then additionally when considering whether the operands of the current
      node are contained.
  * Alternatively, the containment analysis could be done during the building of `RefPosition`s, though see below.
* Similarly, the specification of register requirements is done during the final pass of `Lowering`, and fundamentally requires more
  space (it must specify register masks for sources, destination and any internal registers). In addition, the requirement for a new
  register definition (the destination of the node, or any internal registers) is independent of the parent, so this could be done in
  `LinearScan::buildRefPositionsForNode()` without having to do a dual traversal, unlike the identification of contained nodes.
* After building `RefPositions`, they are traversed in order to set the last use bits.
  This is done separately because there are currently inconsistencies between the gtNext/gtPrev links and the actual order of codegen.
  Once this is resolved, the lastUse bits should be set prior to register allocation by the liveness pass (#7256).
* The `RefPosition`s are all created prior to beginning the register allocation pass. However, they are only really needed in advance
  for the lclVars, which, unlike the "tree temps", have multiple definitions and may be live across basic blocks.
  The `RefPositions`s for the tree temps could potentially be allocated on-the-fly, saving memory and probably improving locality (#7257).
* The loop over all the candidate registers in `LinearScan::tryAllocateFreeReg()` and in `LinearScan::allocateBusyReg()` could be
  short-circuited when a register is found that has the best possible score. Additionally, in the case of MinOpts, it could potentially
  short-circuit as soon as a suitable candidate is found, though one would want to weight the throughput benefit against the code
  quality impact.

Representing Containedness
==========================
My original plan for this was to combine all of the functionality of the current `TreeNodeInfoInit` pass with the building of `RefPositions`,
and eliminate `gtLsraInfo`.
The idea was to later consider pulling the containment analysis back into the first phase of `Lowering`.
However, after beginning down that path (extracting the `TreeNodeInfoInit` methods into separate lsra{arch}.cpp files), I realized that
there would be a great deal of throw-away work to put the containment analysis into `LinearScan`, only to potentially pull it out later.

Furthermore, the representation of containedness is not very clean:
* `Lowering` first communicates this as a combination of implicit knowledge of a node's behavior and its `gtLsraInfo.dstCount`.
* Later, during `CodeGen`, it is determined by combining similar implicit node characteristics with the presence or absence of a register.

I propose instead to do the following:
* Add a flag to each node to indicate whether or not it is a tree root.
  * To free up such a flag, I propose to eliminate `GTF_REG_VAL` for the non-`LEGACY_BACKEND`. Doing so will require some additional cleanup,
    but in the process a number of hacks can be eliminated that are currently there to workaround the fact that the emitter was designed
    to work with a code generator that dynamically assigned registers, and set that flag when the code had been generated to put it in a
    register, unlike the RyuJIT backend, which assigns the registers before generating code.
* Define new register values:
  * `REG_UNK` is assigned by `Lowering` when a register is required.
  * `REG_OPT` is assigned by `Lowering` when a register is optional at both definition and use.
  * `REG_OPT_USE` is assigned by `Lowering` when a register is required at the definition, but optional at the use.
  * I don't know if we need `REG_OPT_DEF`, but that could be added as well.
* Having done this, we can greatly simplify `IsContained()`.

It may be more effective to use the extra bit for an actual `GTF_CONTAINED` flag, and that is something we might want to consider
eventually, but initially it is easier to simplify the containedness check using `GTF_TREE_ROOT` without having to change all the
places that currently mark nodes as contained.

Combining Containedness Analysis with Lowering
==============================================
Once we've changed containedness to use the above representation, we can move the code to set it into the first pass of `Lowering`.
There are likely to be some phase ordering challenges, but I don't think they will be prohibitive.

Eliminating gtLsraInfo
======================
Issue #7225.

After the containedness changes above, all that remains to communicate via `gtLsraInfo` is the register requirements.
This step would still use the `TreeNodeInfo` data structure and the `TreeNodeInfoInit()` methods, but they would be called as
each node is handled by `LinearScan::buildRefPositionsForNode()`.



