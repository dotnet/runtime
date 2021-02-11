## Removing Embedded Statements From the RyuJIT Backend IR

Pat Gavlin ([*pagavlin@microsoft.com*](mailto:pagavlin@microsoft.com))

July 2016

### Abstract

RyuJIT’s IR comes in two forms: that produced and manipulated in the front end and that expected and manipulated by the
back end. The boundary between these two forms of IR is comprised of rationalization and lowering, both of which
transform the front-end IR into back end IR. For the purposes of this paper, the relevant differences between the two
IRs revolve around ordering constructs within basic blocks: top-level statements, trees, comma nodes, and embedded
statements. The latter two constructs are used to represent arbitrary (often side-effecting) code that executes at a
specific point in a tree but does not otherwise participate in the tree’s dataflow. Unfortunately, representational
challenges with embedded statements make them difficult to understand and error-prone to manipulate. This paper proposes
that we remove all statements--embedded and otherwise--from the backend IR by chaining the last linearly-threaded node
within a statement to the first linearly-threaded node within its successor and vice versa as well as removing certain
constraints on the shape of the nodes within a block.

### Review: IR ordering semantics

As previously mentioned, RyuJIT uses two forms of IR: the front-end IR (referred to hereafter as HIR) and the back-end
IR (referred to hereafter as LIR). Aside from using different representations for operations such as stores, HIR and LIR
differ in their ordering constructs within basic blocks.

Within a basic block, the HIR is ordered first by statements. Each statement consists of a single tree that performs the
computation associated with that statement. The nodes of that tree are executed in the order produced by a left-to-right
post-order visit of the tree’s nodes (with the exception of binary operator nodes that have the GTF\_REVERSE\_OPS flag
set, which reverses the order of their operand trees). As such, the edges in a tree represent both ordering and edges in
a dataflow graph where every definition has a single use, with some exceptions:

-   Edges to nodes that represent defs of storage locations (e.g. edges to nodes on the LHS of assignment operators)
    often represent neither ordering or a use-to-def relationship: the def of the location happens as part of the
    execution of its parent, and the edge does not represent a use of an SDSU temp.

-   Edges to unused values do not represent a use-to-def relationship: these edges exist only for ordering.

The primary source of the latter are comma nodes, which are used in the HIR specifically to insert arbitrary code into a
tree’s execution that does not otherwise participate in its SDSU dataflow.

Similar to the HIR, the LIR is ordered first by statements. Each statement consists of a single tree and a linear
threading of nodes that represent the SDSU dataflow and computation, respectively, that are associated with that
statement. The linear threading of nodes must contain each node that is present in the tree, and all nodes that make up
the tree must be present in the linear threading in the same order in which they would have been executed by the HIR ’s
tree ordering. Additional nodes may be present in the linear threading in the form of embedded statements, which are
sub-statements whose nodes form a contiguous subsequence of their containing statement’s execution order, but do not
participate in the containing statement’s SDSU dataflow (i.e. the embedded statement’s nodes are not present in the
containing statement’s tree). Embedded statements otherwise have the same ordering semantics as top-level statements,
and are used for the same purpose as comma nodes in the HIR: they allow the compiler to insert arbitrary code into a
statement’s execution that does not otherwise participate in its SDSU dataflow.

As mentioned, both comma nodes and embedded statements are used for the same purpose in the HIR and LIR, respectively.
Each construct represents a contiguous sequence of code that executes within the context of a tree but that does not
participate in its SDSU dataflow. Of the two, however, embedded statements are generally more difficult to work with:
since they are not present in their containing statement’s tree, the developer must take particular care when processing
LIR in order to avoid omitting the nodes that comprise embedded statements from analyses such as those required for code
motion (e.g. the analysis required to make addressing mode “contained”) and to avoid violating the tree order constraint
placed upon the LIR ’s linear threading.

The problems with manipulating embedded statements have two relatively clear solutions: either replace embedded
statements with a construct that is represented in its parent statement’s tree, or remove the tree order constraint and
move the LIR to a linear ordering that is constrained only by the requirements of dataflow and side-effect consistency.
We believe that the latter is preferable to the former, as it is consistent with the overall direction that the LIR has
taken thus far and reduces the number of concepts bound up in tree edges, which would thereafter represent only the SDSU
dataflow for a node. This would clarify the meaning of the IR as well as the analysis required to perform the
transformations required by the backend.

### Approach

The approach that we propose is outlined below.

#### 1. Add utilities for working with LIR.

Efficiently working with the new IR shape will require the creation of utilities for manipulating, analyzing,
validating, and displaying linearly-ordered LIR. Of these, validation and display of the LIR are particularly
interesting. Validation is likely to check at least the following properties:

1.  The linear threading is loop-free
2.  All SDSU temps (i.e. temps represented by edges) are in fact singly-used
3.  All defs of SDSU temps occur before the corresponding use
4.  All SDSU defs that are used are present in the linear IR

In the short term, we propose that the LIR be displayed using the linear dump that was recently added to the JIT. These
dumps would be configured such that all of the information typically present in today’s tree-style dumps (e.g. node
flags, value numbers, etc.) is also present in the linear dump.

#### 2. Stop Generating embedded statements in the rationalizer.

This can be approached in a few different ways:

1.  Remove commas from the IR before rationalizing. There is already infrastructure in-flight in order to perform this
    transformation, so this is attractive from a dev-hours point of view. Unfortunately, this approach carries both
    throughput and a code quality risks due to the addition of another pass and the creation of additional local vars.

2.  Change the rationalizer such that it simply does not produce embedded statements. This requires that the
    rationalizer is either able to operate on both HIR and LIR or simply linearize commas as it goes.

3.  Remove embedded statements from the IR with a linearization pass between the rationalizer and lowering.

We will move forward with option 2, as it is the most attractive from a throughput and code quality standpoint:
it does not add additional passes (as does option 3), nor does it introduce additional local vars (as does option
1).

#### 3. Refactor decomposition and lowering to work with linear LIR.

The bulk of the work in this step involves moving these passes from statement- and tree-ordered walks to linear walks
and refactoring any code that uses the parent stack to instead use a helper that can calculate the def-to-use edge for a
node. It will also be necessary to replace embedded statement insertion with simple linear IR insertion, which is
straightforward.

##### 3.i. Decomposition

Transitioning decomposition should be rather simple. This pass walks the nodes of each statement in execution order,
decomposing 64-bit operations into equivalent sequences of 32-bit operations as it goes. Critically, the rewrites
performed by decomposition are universally expansions of a single operator into a contiguous sequence of operators whose
results are combined to form the ultimate result. This sort of transformation is easily performed on linear IR by
inserting the new nodes before the node being replaced. Furthermore, because nodes will appear in the linear walk in the
same relative order that they appear in the execution-order tree walk, rewrites performed in linear order will occur in
the same order in which they were originally performed.

##### 3.ii. Lowering

The picture for lowering is much the same as the picture for decomposition. Like decomposition, all rewrites are
performed in execution order, and most rewrites are simple expansions of a single node into multiple nodes. There is one
notable exception, however: the lowering of add nodes for the x86 architecture examines the parent stack provided by
tree walks in order to defer lowering until it may be possible to form an address mode. In this case, the use of the
parent stack can be replaced with a helper that finds the node that uses the def produced by the add node.

##### 3.iii. General issues

Both decomposition and lowering make use of a utility to fix up information present in the per-call-node side table that
tracks extra information about the call’s arguments when necessary. This helper can be replaced by instead fixing up the
side table entries when the call is visited by each pass.

#### 4. Remove uses of statements and the tree-order invariant from LSRA.

LSRA currently depends on the tree order invariant so that it can build a stack to track data associated with the defs
consumed by an operator. Removing the tree order invariant requires that LSRA is able to accommodate situations where
the contents of this stack as produced by a linear walk would no longer contain correct information due to the insertion
of new nodes that produce values that are live across the operator and some subset of its operands. Analysis indicates
that a simple map from defs to the necessary information should be sufficient.

#### 5. Remove uses of statements from the rest of the backend.

The rest of the backend--i.e. codegen--has no known dependencies on tree order, so there are no concerns with respect to
the change in ordering semantics. However, statement nodes are used to derive IL offsets for debug information. There
are a number of alternative methods of tracking IL offsets to consider:

1.  Insert “IL offset” nodes into the linearly-ordered LIR. These nodes would then be noted by code generation and used
    to emit the necessary IP-mapping entries in the same way that statements are today. This has the disadvantage of
    requiring additional work when performing code motion on LIR if the IL offset for a particular node needs to be
    kept up-to-date. However, today’s backend does not perform this sort of code motion and even if it did, optimized
    debugging is not currently a scenario, so a loss of debugging fidelity may be acceptable.

2.  Use a side table to map from each node to its IL offset. Although this increases the total working set of the
    backend, this has the advantage of making it easy to keep IL offsets correct in the face of code motion.

3.  Add IL offset information directly to each node. This has the disadvantage of increasing the working set of the
    entire compiler.

Unless we expect to require correct debug information in the face of code motion in the future, our recommendation is
for the first option, which comes at a minimum size and implementation cost.

#### 6. Remove contained nodes from the LIR’s execution order.

Once statements are removed from the LIR, there is one major issue left to address: the presence of contained nodes in
the LIR’s execution order. The execution of these nodes logically happens as part of the node in which they are
contained, but these nodes remain physically present in the IR at their original locations. As a result, the backend
must often take care to skip contained nodes when manipulating the LIR in ways that must take execution order into
account. Instead of leaving such nodes in the LIR, these node should be removed from execution order and instead be
represented as (probably unordered) trees referenced only by the containing node.

### Conclusion and Future Directions

The changes suggested in this paper move RyuJIT’s LIR from a linear view of a tree-ordered nodes with certain nodes
represented only in execution order to a linearly ordered sequence of nodes. Furthermore, with this change, tree edges
in LIR would represent either uses of SDSU temps that have a place in execution order (where the edge points from the
use of the temp to the def) or uses of unordered expression trees that execute as part of the parent node. This form
should be easier to work with due to the removal of the tree order constraint and embedded statements and its similarity
to other linear IR designs.
