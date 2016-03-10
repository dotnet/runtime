JIT Compiler Structure
===

# Introduction

RyuJIT is the code name for the next generation Just in Time Compiler (aka “JIT”) for the AMD64 .NET runtime. Its first implementation is for the AMD64 architecture. It is derived from a code base that is still in use for the other targets of .NET.

The primary design considerations for RyuJIT are to:

* Maintain a high compatibility bar with previous JITs, especially those for x86 (jit32) and x64 (jit64).
* Support and enable good runtime performance through code optimizations, register allocation, and code generation.
* Ensure good throughput via largely linear-order optimizations and transformations, along with limitations on tracked variables for analyses (such as dataflow) that are inherently super-linear.
* Ensure that the JIT architecture is designed to support a range of targets and scenarios.

The first objective was the primary motivation for evolving the existing code base, rather than starting from scratch or departing more drastically from the existing IR and architecture.

# Execution Environment and External Interface

RyuJIT provides the just in time compilation service for the .NET runtime. The runtime itself is variously called the EE (execution engine), the VM (virtual machine) or simply the CLR (common language runtime). Depending upon the configuration, the EE and JIT may reside in the same or different executable files. RyuJIT implements the JIT side of the JIT/EE interfaces:

* `ICorJitCompiler` – this is the interface that the JIT compiler implements. This interface is defined in [src/inc/corjit.h](https://github.com/dotnet/coreclr/blob/master/src/inc/corjit.h) and its implementation is in [src/jit/ee_il_dll.cpp](https://github.com/dotnet/coreclr/blob/master/src/jit/ee_il_dll.cpp). The following are the key methods on this interface:
  * `compileMethod` is the main entry point for the JIT. The EE passes it a `ICorJitInfo` object, and the “info” containing the IL, the method header, and various other useful tidbits. It returns a pointer to the code, its size, and additional GC, EH and (optionally) debug info.
  * `getVersionIdentifier` is the mechanism by which the JIT/EE interface is versioned. There is a single GUID (manually generated) which the JIT and EE must agree on.
  * `getMaxIntrinsicSIMDVectorLength` communicates to the EE the largest SIMD vector length that the JIT can support.
* `ICorJitInfo` – this is the interface that the EE implements. It has many methods defined on it that allow the JIT to look up metadata tokens, traverse type signatures, compute field and vtable offsets, find method entry points, construct string literals, etc. This bulk of this interface is inherited from `ICorJitDynamicInfo` which is defined in [src/inc/corinfo.h](https://github.com/dotnet/coreclr/blob/master/src/inc/corinfo.h). The implementation is defined in [src/vm/jitinterface.cpp](https://github.com/dotnet/coreclr/blob/master/src/vm/jitinterface.cpp).

# Internal Representation (IR)

## Overview of the IR

The RyuJIT IR can be described at a high level as follows:

* The Compiler object is the primary data structure of the JIT. Each method is represented as a doubly-linked list of `BasicBlock` objects. The Compiler object points to the head of this list with the `fgFirstBB` link, as well as having additional pointers to the end of the list, and other distinguished locations.
  * `ICorJitCompiler::CompileMethod()` is invoked for each method, and creates a new Compiler object.  Thus, the JIT need not worry about thread synchronization while accessing Compiler state. The EE has the necessary synchronization to ensure there is a single JIT’d copy of a method when two or more threads try to trigger JIT compilation of the same method.
* `BasicBlock` nodes contain a list of doubly-linked statements with no internal control flow (there is an exception for the case of the qmark/colon operator)
  * The `BasicBlock` also contains the dataflow information, when available.
* `GenTree` nodes represent the operations and statement of the method being compiled.
  * It includes the type of the node, as well as value number, assertions, and register assignments when available.
* `LclVarDsc` represents a local variable, argument or JIT-created temp. It has a `gtLclNum` which is the identifier usually associated with the variable in the JIT and its dumps. The `LclVarDsc` contains the type, use count, weighted use count, frame or register assignment etc. These are often referred to simply as “lclVars”. They can be tracked (`lvTracked`), in which case they participate in dataflow analysis, and have a different index (`lvVarIndex`) to allow for the use of dense bit vectors.

![RyuJIT IR Overview](../images/ryujit-ir-overview.png)

The IR has two modes:

* In tree-order mode, non-statement nodes (often described as expression nodes, though they are not always strictly expressions) are linked only via parent-child links (unidirectional). That is, the consuming node has pointers to the nodes that produce its input operands.
* In linear-order mode, non-statement nodes have both parent-child links as well as execution order links (`gtPrev` and `gtNext`).
  * In the interest of maintaining functionality that depends upon the validity of the tree ordering, the linear mode of the `GenTree` IR has an unusual constraint that the execution order must represent a valid traversal of the parent-child links.

A separate representation, `insGroup` and `instrDesc`, is used during the actual instruction encoding.

### Statement Order

During the “front end” of the JIT compiler (prior to Rationalization), the execution order of the `GenTree` nodes on a statement is fully described by the “tree” order – that is, the links from the top node of a statement (the `gtStmtExpr`) to its children. The order is determined by a depth-first, left-to-right traversal of the tree, with the exception of nodes marked `GTF_REVERSE_OPS` on binary nodes, whose second operand is traversed before its first.

After rationalization, the execution order can no longer be deduced from the tree order alone. At this point, the dominant ordering becomes “linear order”. This is because at this point any `GT_COMMA` nodes have been replaced by embedded statements, whose position in the execution order can only be determined by the `gtNext` and `gtPrev` links on the tree nodes.

This modality is captured in the `fgOrder` flag on the Compiler object – it is either `FGOrderTree` or `FGOrderLinear`.

## GenTree Nodes

Each operation is represented as a GenTree node, with an opcode (GT_xxx), zero or more child `GenTree` nodes, and additional fields as needed to represent the semantics of that node.

The `GenTree` nodes are doubly-linked in execution order, but the links are not necessarily valid during all phases of the JIT.

The statement nodes utilize the same `GenTree` base type as the operation nodes, though they are not truly related.

* The statement nodes are doubly-linked. The first statement node in a block points to the last node in the block via its `gtPrev` link. Note that the last statement node does *not* point to the first; that is, the list is not fully circular.
* Each statement node contains two `GenTree` links – `gtStmtExpr` points to the top-level node in the statement (i.e. the root of the tree that represents the statement), while `gtStmtList` points to the first node in execution order (again, this link is not always valid).

### Example of Post-Import IR

For this snippet of code (extracted from [tests/src/JIT/CodeGenBringUpTests/DblRoots.cs](https://github.com/dotnet/coreclr/blob/master/tests/src/JIT/CodeGenBringUpTests/DblRoots.cs)):

       r1 = (-b + Math.Sqrt(b*b - 4*a*c))/(2*a);

A stripped-down dump of the `GenTree` nodes just after they are imported looks like this:

    ▌ stmtExpr  void  (top level) (IL 0x000...0x026)
    │        ┌──▌ lclVar    double V00 arg0
    │     ┌──▌ *         double
    │     │  └──▌ dconst    double 2.00
    │  ┌──▌ /         double
    │  │  │  ┌──▌ mathFN    double sqrt
    │  │  │  │  │     ┌──▌ lclVar    double V02 arg2
    │  │  │  │  │  ┌──▌ *         double
    │  │  │  │  │  │  │  ┌──▌ lclVar    double V00 arg0
    │  │  │  │  │  │  └──▌ *         double
    │  │  │  │  │  │     └──▌ dconst    double 4.00
    │  │  │  │  └──▌ -         double
    │  │  │  │     │       lclVar    double V01 arg1
    │  │  │  │     └──▌ *         double
    │  │  │  │        └──▌ lclVar    double V01 arg1
    │  │  └──▌ +         double
    │  │     └──▌ unary -   double
    │  │        └──▌ lclVar    double V01 arg1
    └──▌  =         double
       └──▌ indir     double
          └──▌ lclVar    byref  V03 arg3

## Types

The JIT is primarily concerned with “primitive” types, i.e. integers, reference types, pointers, and floating point types.  It must also be concerned with the format of user-defined value types (i.e. struct types derived from `System.ValueType`) – specifically, their size and the offset of any GC references they contain, so that they can be correctly initialized and copied.  The primitive types are represented in the JIT by the `var_types` enum, and any additional information required for struct types is obtained from the JIT/EE interface by the use of an opaque `CORINFO_CLASS_HANDLE`.

## Dataflow Information

In order to limit throughput impact, the JIT limits the number of lvlVars for which liveness information is computed. These are the tracked lvlVars (`lvTracked` is true), and they are the only candidates for register allocation.

The liveness analysis determines the set of defs, as well as the uses that are upward exposed, for each block. It then propagates the liveness information. The result of the analysis is captured in the following:

* The live-in and live-out sets are captured in the `bbLiveIn` and `bbLiveOut` fields of the `BasicBlock`.
* The `GTF_VAR_DEF` flag is set on a lvlVar `GenTree` node that is a definition.
* The `GTF_VAR_USEASG` flag is set (in addition to the `GTF_VAR_DEF` flag) for the target of an update (e.g. +=).
* The `GTF_VAR_USEDEF` is set on the target of an assignment of a binary operator with the same lvlVar as an operand.

## SSA

Static single assignment (SSA) form is constructed in a traditional manner [[1]](#[1]). The SSA names are recorded on the lvlVar references. While SSA form usually retains a pointer or link to the defining reference, RyuJIT currently retains only the `BasicBlock` in which the definition of each SSA name resides.

## Value Numbering

Value numbering utilizes SSA for lvlVar values, but also performs value numbering of expression trees. It takes advantage of type safety by not invalidating the value number for field references with a heap write, unless the write is to the same field. The IR nodes are annotated with the value numbers, which are indexes into a type-specific value number store. Value numbering traverses the trees, performing symbolic evaluation of many operations.

# Phases of RyuJIT

The top-level function of interest is `Compiler::compCompile`. It invokes the following phases in order.

| **Phase** | **IR Transformations** |
| --- | --- |
|[Pre-import](#pre-import)|`Compiler->lvaTable` created and filled in for each user argument and variable. BasicBlock list initialized.|
|[Importation](#importation)|`GenTree` nodes created and linked in to Statements, and Statements into BasicBlocks. Inlining candidates identified.|
|[Inlining](#inlining)|The IR for inlined methods is incorporated into the flowgraph.|
|[Struct Promotion](#struct-promotion)|New lvlVars are created for each field of a promoted struct.|
|[Mark Address-Exposed Locals](#mark-addr-exposed)|lvlVars with references occurring in an address-taken context are marked.  This must be kept up-to-date.|
|[Morph Blocks](#morph-blocks)|Performs localized transformations, including mandatory normalization as well as simple optimizations.|
|[Eliminate Qmarks](#eliminate-qmarks)|All `GT_QMARK` nodes are eliminated, other than simple ones that do not require control flow.|
|[Flowgraph Analysis](#flowgraph-analysis)|`BasicBlock` predecessors are computed, and must be kept valid. Loops are identified, and normalized, cloned and/or unrolled.|
|[Normalize IR for Optimization](#normalize-ir)|lvlVar references counts are set, and must be kept valid. Evaluation order of `GenTree` nodes (`gtNext`/`gtPrev`) is determined, and must be kept valid.|
|[SSA and Value Numbering Optimizations](#ssa-vn)|Computes liveness (`bbLiveIn` and `bbLiveOut` on `BasicBlocks`), and dominators. Builds SSA for tracked lvlVars. Computes value numbers.|
|[Loop Invariant Code Hoisting](#licm)|Hoists expressions out of loops.|
|[Copy Propagation](#copy-propagation)|Copy propagation based on value numbers.|
|[Common Subexpression Elimination (CSE)](#cse)|Elimination of redundant subexressions based on value numbers.|
|[Assertion Propagation](#assertion-propagation)|Utilizes value numbers to propagate and transform based on properties such as non-nullness.|
|[Range analysis](#range-analysis)|Eliminate array index range checks based on value numbers and assertions|
|[Rationalization](#rationalization)|Flowgraph order changes from `FGOrderTree` to `FGOrderLinear`. All `GT_COMMA`, `GT_ASG` and `GT_ADDR` nodes are transformed.|
|[Lowering](#lowering)|Register requirements are fully specified (`gtLsraInfo`). All control flow is explicit.|
|[Register allocation](#reg-alloc)|Registers are assigned (`gtRegNum` and/or `gtRsvdRegs`),and the number of spill temps calculated.|
|[Code Generation](#code-generation)|Determines frame layout. Generates code for each `BasicBlock`. Generates prolog & epilog code for the method. Emit EH, GC and Debug info.|

## <a name="pre-import"/>Pre-import

Prior to reading in the IL for the method, the JIT initializes the local variable table, and scans the IL to find branch targets and form BasicBlocks.

## <a name="importation">Importation

Importation is the phase that creates the IR for the method, reading in one IL instruction at a time, and building up the statements. During this process, it may need to generate IR with multiple, nested expressions. This is the purpose of the non-expression-like IR nodes:

* It may need to evaluate part of the expression into a temp, in which case it will use a comma (`GT_COMMA`) node to ensure that the temp is evaluated in the proper execution order – i.e. `GT_COMMA(GT_ASG(temp, exp), temp)` is inserted into the tree where “exp” would go.
* It may need to create conditional expressions, but adding control flow at this point would be quite messy. In this case it generates question mark/colon (?: or `GT_QMARK`/`GT_COLON`) trees that may be nested within an expression.

During importation, tail call candidates (either explicitly marked or opportunistically identified) are identified and flagged. They are further validated, and possibly unmarked, during morphing.

## Morphing

The `fgMorph` phase includes a number of transformations:

### <a name="inlining"/>Inlining

The `fgInline` phase determines whether each call site is a candidate for inlining. The initial determination is made via a state machine that runs over the candidate method’s IL. It estimates the native code size corresponding to the inline method, and uses a set of heuristics, including the estimated size of the current method) to determine if inlining would be profitable. If so, a separate Compiler object is created, and the importation phase is called to create the tree for the candidate inline method. Inlining may be aborted prior to completion, if any conditions are encountered that indicate that it may be unprofitable (or otherwise incorrect). If inlining is successful, the inlinee compiler’s trees are incorporated into the inliner compiler (the “parent”), with args and returns appropriately transformed.

### <a name="struct-promotion"/>Struct Promotion

Struct promotion (`fgPromoteStructs()`) analyzes the local variables and temps, and determines if their fields are candidates for tracking (and possibly enregistering) separately. It first determines whether it is possible to promote, which takes into account whether the layout may have holes or overlapping fields, whether its fields (flattening any contained structs) will fit in registers, etc.

Next, it determines whether it is likely to be profitable, based on the number of fields, and whether the fields are individually referenced.

When a lvlVar is promoted, there are now N+1 lvlVars for the struct, where N is the number of fields. The original struct lvlVar is not considered to be tracked, but its fields may be.

### <a name="mark-addr-exposed"/>Mark Address-Exposed Locals

This phase traverses the expression trees, propagating the context (e.g. taking the address, indirecting) to determine which lvlVars have their address taken, and which therefore will not be register candidates. If a struct lvlVar has been promoted, and is then found to be address-taken, it will be considered “dependently promoted”, which is an odd way of saying that the fields will still be separately tracked, but they will not be register candidates.

### <a name="morph-blocks"/>Morph Blocks

What is often thought of as “morph” involves localized transformations to the trees. In addition to performing simple optimizing transformations, it performs some normalization that is required, such as converting field and array accesses into pointer arithmetic. It can (and must) be called by subsequent phases on newly added or modified trees. During the main Morph phase, the boolean `fgGlobalMorph` is set on the Compiler argument, which governs which transformations are permissible.

### <a name="eliminate-qmarks"/>Eliminate Qmarks

This expands most `GT_QMARK`/`GT_COLON` trees into blocks, except for the case that is instantiating a condition.

## <a name="flowgraph-analysis"/>Flowgraph Analysis

At this point, a number of analyses and transformations are done on the flowgraph:

* Computing the predecessors of each block
* Computing edge weights, if profile information is available
* Computing reachability and dominators
* Identifying and normalizing loops (transforming while loops to “do while”)
* Cloning and unrolling of loops

## <a name="normalize-ir"/>Normalize IR for Optimization

At this point, a number of properties are computed on the IR, and must remain valid for the remaining phases. We will call this “normalization”

* `lvaMarkLocalVars` – set the reference counts (raw and weighted) for lvlVars, sort them, and determine which will be tracked (currently up to 128). Note that after this point any transformation that adds or removes lvlVar references must update the reference counts.
* `optOptimizeBools` – this optimizes Boolean expressions, and may change the flowgraph (why is it not done prior to reachability and dominators?)
* Link the trees in evaluation order (setting `gtNext` and `gtPrev` fields): and `fgFindOperOrder()` and `fgSetBlockOrder()`.

## <a name="ssa-vn"/>SSA and Value Numbering Optimizations

The next set of optimizations are built on top of SSA and value numbering. First, the SSA representation is built (during which dataflow analysis, aka liveness, is computed on the lclVars), then value numbering is done using SSA.

### <a name="licm"/>Loop Invariant Code Hoisting

This phase traverses all the loop nests, in outer-to-inner order (thus hoisting expressions outside the largest loop in which they are invariant). It traverses all of the statements in the blocks in the loop that are always executed. If the statement is:

* A valid CSE candidate
* Has no side-effects
* Does not raise an exception OR occurs in the loop prior to any side-effects
* Has a valid value number, and it is a lvlVar defined outside the loop, or its children (the value numbers from which it was computed) are invariant.

### <a name="copy-propagation"/>Copy Propagation

This phase walks each block in the graph (in dominator-first order, maintaining context between dominator and child) keeping track of every live definition. When it encounters a variable that shares the VN with a live definition, it is replaced with the variable in the live definition.

The JIT currently requires that the IR be maintained in conventional SSA form, as there is no “out of SSA” translation (see the comments on `optVnCopyProp()` for more information).

### <a name="cse"/>Common Subexpression Elimination (CSE)

Utilizes value numbers to identify redundant computations, which are then evaluated to a new temp lvlVar, and then reused.

### <a name="assertion-propagation"/>Assertion Propagation

Utilizes value numbers to propagate and transform based on properties such as non-nullness.

### <a name="range-analysis"/>Range analysis

Optimize array index range checks based on value numbers and assertions.

## <a name=rationalization"/>Rationalization

As the JIT has evolved, changes have been made to improve the ability to reason over the tree in both “tree order” and “linear order”. These changes have been termed the “rationalization” of the IR. In the spirit of reuse and evolution, some of the changes have been made only in the later (“backend”) components of the JIT. The corresponding transformations are made to the IR by a “Rationalizer” component. It is expected that over time some of these changes will migrate to an earlier place in the JIT phase order:

* Elimination of assignment nodes (`GT_ASG`). The assignment node was problematic because the semantics of its destination (left hand side of the assignment) could not be determined without context. For example, a `GT_LCL_VAR` on the left-hand side of an assignment is a definition of the local variable, but on the right-hand side it is a use. Furthermore, since the execution order requires that the children be executed before the parent, it is unnatural that the left-hand side of the assignment appears in execution order before the assignment operator.
  * During rationalization, all assignments are replaced by stores, which either represent their destination on the store node itself (e.g. `GT_LCL_VAR`), or by the use of a child address node (e.g. `GT_STORE_IND`).
* Elimination of address nodes (`GT_ADDR`). These are problematic because of the need for parent context to analyze the child.
* Elimination of “comma” nodes (`GT_COMMA`). These nodes are introduced for convenience during importation, during which a single tree is constructed at a time, and not incorporated into the statement list until it is completed. When it is necessary, for example, to store a partially-constructed tree into a temporary variable, a `GT_COMMA` node is used to link it into the tree. However, in later phases, these comma nodes are an impediment to analysis, and thus are split into separate statements.
  * In some cases, it is not possible to fully extract the tree into a separate statement, due to execution order dependencies. In these cases, an “embedded” statement is created. While these are conceptually very similar to the `GT_COMMA` nodes, they do not masquerade as expressions.
* Elimination of “QMark” (`GT_QMARK`/`GT_COLON`) nodes is actually done at the end of morphing, long before the current rationalization phase. The presence of these nodes made analyses (especially dataflow) overly complex.

For our earlier example (Example of Post-Import IR), here is what the simplified dump looks like just prior to Rationalization (the $ annotations are value numbers).  Note that some common subexpressions have been computed into new temporary lvlVars, and that computation has been inserted as a `GT_COMMA` (comma) node in the IR:

    ▌  stmtExpr  void  (top level) (IL 0x000...0x026)
    │        ┌──▌  lclVar    double V07 cse1          $185
    │     ┌──▌  comma     double                      $185
    │     │  │     ┌──▌  dconst    double 2.00        $143
    │     │  │  ┌──▌  \*         double                $185
    │     │  │  │  └──▌  lclVar   double V00 arg0 u:2 $80
    │     │  └──▌  =         double                   $VN.Void
    │     │     └──▌  lclVar    double V07 cse1       $185
    │  ┌──▌  /         double                         $186
    │  │  │  ┌──▌  unary -   double                   $84
    │  │  │  │  └──▌  lclVar    double V01 arg1   u:2 $81
    │  │  └──▌  +         double                      $184
    │  │     │  ┌──▌  lclVar    double V06 cse0       $83
    │  │     └──▌  comma     double                   $83
    │  │        │  ┌──▌  mathFN    double sqrt        $83
    │  │        │  │  │     ┌──▌  lclVar double V02 arg2 u:2 $82
    │  │        │  │  │  ┌──▌  \*         double              $182
    │  │        │  │  │  │  │  ┌──▌  dconst    double 4.00   $141
    │  │        │  │  │  │  └──▌  \*         double           $181
    │  │        │  │  │  │     └──▌  lclVar double V00 arg0 u:2 $80
    │  │        │  │  └──▌  -         double                    $183
    │  │        │  │     │  ┌──▌  lclVar    double V01 arg1 u:2 $81
    │  │        │  │     └──▌  \*         double                 $180
    │  │        │  │        └──▌  lclVar    double V01 arg1 u:2 $81
    │  │        └──▌  =         double                          $VN.Void
    │  │           └──▌  lclVar    double V06 cse0              $83
    └──▌  =         double                                      $VN.Void
       └──▌  indir     double $186
          └──▌  lclVar    byref  V03 arg3        u:2 (last use) $c0

After rationalization, the nodes are presented in execution order, and the `GT_COMMA` (comma) and `GT_ASG` (=) nodes have been eliminated:

    ▌  stmtExpr  void  (top level) (IL 0x000...  ???)
    │           ┌──▌  lclVar    double V01 arg1
    │           ├──▌  lclVar    double V01 arg1
    │        ┌──▌  \*         double
    │        │     ┌──▌  lclVar    double V00 arg0
    │        │     ├──▌  dconst    double 4.00
    │        │  ┌──▌  \*         double
    │        │  ├──▌  lclVar    double V02 arg2
    │        ├──▌  \*         double
    │     ┌──▌  -         double
    │  ┌──▌  mathFN    double sqrt
    └──▌  st.lclVar double V06

    ▌  stmtExpr  void  (top level) (IL 0x000...0x026)
    │        ┌──▌  lclVar    double V06
    │        │  ┌──▌  lclVar    double V01 arg1
    │        ├──▌  unary -   double
    │     ┌──▌  +         double
    │     │  {  ▌  stmtExpr  void  (embedded) (IL 0x000...  ???)
    │     │  {  │     ┌──▌  lclVar    double V00 arg0
    │     │  {  │     ├──▌  dconst    double 2.00
    │     │  {  │  ┌──▌  \*         double
    │     │  {  └──▌  st.lclVar double V07
    │     ├──▌  lclVar    double V07
    │  ┌──▌  /         double
    │  ├──▌  lclVar    byref  V03 arg3
    └──▌  storeIndir double


Note that the first operand of the first comma has been extracted into a separate statement, but the second comma causes an embedded statement to be created, in order to preserve execution order.

## <a name="lowering"/>Lowering

Lowering is responsible for transforming the IR in such a way that the control flow, and any register requirements, are fully exposed.

It accomplishes this in two passes.

The first pass is a post-order traversal that performs context-dependent transformations such as expanding switch statements (using a switch table or a series of conditional branches), constructing addressing modes, etc.  For example, this:

             ┌──▌  lclVar    ref    V00 arg0
             │     ┌──▌  lclVar    int    V03 loc1
             │  ┌──▌  cast      long <- int
             │  ├──▌  const     long   2
             ├──▌  <<        long
          ┌──▌  +         byref
          ├──▌  const     long   16
       ┌──▌  +         byref
    ┌──▌  indir     int

Is transformed into this, in which the addressing mode is explicit:

          ┌──▌  lclVar    ref    V00 arg0
          │  ┌──▌  lclVar    int    V03 loc1
          ├──▌  cast      long <- int
       ┌──▌  lea(b+(i*4)+16) byref
    ┌──▌  indir     int

The next pass annotates the nodes with register requirements, and this is done in an execution order traversal (effectively post-order) in order to ensure that the children are visited prior to the parent. It may also do some transformations that do not require the parent context, such as determining the code generation strategy for block assignments (e.g. `GT_COPYBLK`) which may become helper calls, unrolled loops, or an instruction like rep stos.

The register requirements are expressed in the `TreeNodeInfo` (`gtLsraInfo`) for each node.  For example, for the `copyBlk` node in this snippet:

    Source      │  ┌──▌  const(h)  long   0xCA4000 static
    Destination │  ├──▌  &lclVar   byref  V04 loc4
                │  ├──▌  const     int    34
                └──▌  copyBlk   void

The `TreeNodeInfo` would be as follows:

    +<TreeNodeInfo @ 15 0=1 1i 1f
          src=[allInt]
          int=[rax rcx rdx rbx rbp rsi rdi r8-r15 mm0-mm5]
          dst=[allInt] I>

The “@ 15” is the location number of the node.  The “0=1” indicates that there are zero destination registers (because this defines only memory), and 1 source register (the address of lclVar V04).  The “1i” indicates that it requires 1 internal integer register (for copying the remainder after copying 16-byte sized chunks), the “1f” indicates that it requires 1 internal floating point register (for copying the two 16-byte chunks).  The src, int and dst fields are encoded masks that indicate the register constraints for the source, internal and destination registers, respectively.

## <a name="reg-alloc"/>Register allocation

The RyuJIT register allocator uses a Linear Scan algorithm, with an approach similar to [[2]](#[2]). In brief, it operates on two main data structures:

* `Intervals` (representing live ranges of variables or tree expressions) and `RegRecords` (representing physical registers), both of which derive from `Referent`.
* `RefPositions`, which represent uses or defs (or variants thereof, such as ExposedUses) of either `Intervals` or physical registers.

Pre-conditions:

* The `NodeInfo` is initialized for each tree node to indicate:
  * Number of registers consumed and produced by the node.
  * Number and type (int versus float) of internal registers required.

Allocation proceeds in 4 phases:

* Determine the order in which the `BasicBlocks` will be allocated, and which predecessor of each block will be used to determine the starting location for variables live-in to the `BasicBlock`.
* Construct Intervals for each tracked lvlVar, then walk the `BasicBlocks` in the determined order building `RefPositions` for each register use, def, or kill.
* Allocate the registers by traversing the `RefPositions`.
* Write back the register assignments, and perform any necessary moves at block boundaries where the allocations don’t match.

Post-conditions:

* The `gtRegNum` property of all `GenTree` nodes that require a register has been set to a valid register number.
* The `gtRsvdRegs` field (a set/mask of registers) has the requested number of registers specified for internal use.
* All spilled values (lvlVar or expression) are marked with `GTF_SPILL` at their definition. For lvlVars, they are also marked with `GTF_SPILLED` at any use at which the value must be reloaded.
* For all lvlVars that are register candidates:
  * `lvRegNum` = initial register location (or `REG_STK`)
  * `lvRegister` flag set if it always lives in the same register
  * `lvSpilled` flag is set if it is ever spilled
* The maximum number of simultaneously-live spill locations of each type (used for spilling expression trees) has been communicated via calls to `compiler->tmpPreAllocateTemps(type)`.

## <a name="code-generation"/>Code Generation

The process of code generation is relatively straightforward, as Lowering has done some of the work already. Code generation proceeds roughly as follows:

* Determine the frame layout – allocating space on the frame for any lvlVars that are not fully enregistered, as well as any spill temps required for spilling non-lvlVar expressions.
* For each `BasicBlock`, in layout order, and each `GenTree` node in the block, in execution order:
  * If the node is “contained” (i.e. its operation is subsumed by a parent node), do nothing.
  * Otherwise, “consume” all the register operands of the node.
    * This updates the liveness information (i.e. marking a lvlVar as dead if this is the last use), and performs any needed copies.
    * This must be done in correct execution order, obeying any reverse flags (GTF_REVERSE_OPS) on the operands, so that register conflicts are handled properly.
  * Track the live variables in registers, as well as the live stack variables that contain GC refs.
  * Produce the `instrDesc(s)` for the operation, with the current live GC references.
  * Update the scope information (debug info) at block boundaries.
* Generate the prolog and epilog code.
* Write the final instruction bytes. It does this by invoking the emitter, which holds all the `instrDescs`.

# Phase-dependent Properties and Invariants of the IR

There are several properties of the IR that are valid only during (or after) specific phases of the JIT. This section describes the phase transitions, and how the IR properties are affected.

## Phase Transitions

* Flowgraph analysis
  * Sets the predecessors of each block, which must be kept valid after this phase.
  * Computes reachability and dominators. These may be invalidated by changes to the flowgraph.
  * Computes edge weights, if profile information is available.
  * Identifies and normalizes loops. These may be invalidated, but must be marked as such.
* Normalization
  * The lvlVar reference counts are set by `lvaMarkLocalVars()`.
  * Statement ordering is determined by `fgSetBlockOrder()`. Execution order is a depth-first preorder traversal of the nodes, with the operands usually executed in order. The exceptions are:
    * Commutative operators, which can have the `GTF_REVERSE_OPS` flag set to indicate that op2 should be evaluated before op1.
    * Assignments, which can also have the `GTF_REVERSE_OPS` flag set to indicate that the rhs (op2) should be evaluated before the target address (if any) on the lhs (op1) is evaluated. This can only be done if there are no side-effects in the expression for the lhs.
* Rationalization
  * All `GT_COMMA` nodes are split into separate statements, which may be embedded in other statements in execution order.
  * All `GT_ASG` trees are transformed into `GT_STORE` variants (e.g. `GT_STORE_LCL_VAR`).
  * All `GT_ADDR` nodes are eliminated (e.g. with `GT_LCL_VAR_ADDR`).
* Lowering
  * `GenTree` nodes are split or transformed as needed to expose all of their register requirements and any necessary `flowgraph` changes (e.g., for switch statements).

## GenTree phase-dependent properties

Ordering:

* For `GenTreeStmt` nodes, the `gtNext` and `gtPrev` fields must always be consistent. The last statement in the `BasicBlock` must have `gtNext` equal to null. By convention, the `gtPrev` of the first statement in the `BasicBlock` must be the last statement of the `BasicBlock`.
  * In all phases, `gtStmtExpr` points to the top-level node of the expression.
* For non-statement nodes, the `gtNext` and `gtPrev` fields are either null, prior to ordering, or they are consistent (i.e. `A->gtPrev->gtNext = A`, and `A->gtNext->gtPrev == A`, if they are non-null).
* After normalization the `gtStmtList` of the containing statement points to the first node to be executed.
* Prior to normalization, the `gtNext` and `gtPrev` pointers on the expression (non-statement) `GenTree` nodes are invalid. The expression nodes are only traversed via the links from parent to child (e.g. `node->gtGetOp1()`, or `node->gtOp.gtOp1`). The `gtNext/gtPrev` links are set by `fgSetBlockOrder()`.
  * After normalization, and prior to rationalization, the parent/child links remain the primary traversal mechanism. The evaluation order of any nested expression-statements (usually assignments) is enforced by the `GT_COMMA` in which they are contained.
* After rationalization, all `GT_COMMA` nodes are eliminated, and the primary traversal mechanism becomes the `gtNext/gtPrev` links. Statements may be embedded within other statements, but the nodes of each statement preserve the valid traversal order.
* In tree ordering:
  * The `gtPrev` of the first node (`gtStmtList`) is always null.
  * The `gtNext` of the last node (`gtStmtExpr`) is always null.
* In linear ordering:
  * The nodes of each statement are ordered such that `gtStmtList` is encountered first, and `gtStmtExpr` is encountered last.
  * The nodes of an embedded statement S2 (starting with `S2->gtStmtList`) appear in the ordering after a node from the “containing” statement S1, and no other node from S1 will appear in the list prior to the `gtStmtExpr` of S2. However, there may be multiple levels of nesting of embedded statements.

TreeNodeInfo:

* The `TreeNodeInfo` (`gtLsraInfo`) is set during the Lowering phase, and communicates the register requirements of the node, including the number and types of registers used as sources, destinations and internal registers. Currently only a single destination per node is supported.

## LclVar phase-dependent properties

Prior to normalization, the reference counts (`lvRefCnt` and `lvRefCntWtd`) are not valid. After normalization they must be updated when lvlVar references are added or removed.

# Supporting technologies and components

## Instruction encoding

Instruction encoding is performed by the emitter ([emit.h](https://github.com/dotnet/coreclr/blob/master/src/jit/emit.h)), using the `insGroup`/`instrDesc` representation. The code generator calls methods on the emitter to construct `instrDescs`. The encodings information is captured in the following:

* The “instruction” enumeration itemizes the different instructions available on each target, and is used as an index into the various encoding tables (e.g. `instInfo[]`, `emitInsModeFmtTab[]`) generated from the `instrs{tgt}.h` (e.g., [instrsxarch.h](https://github.com/dotnet/coreclr/blob/master/src/jit/instrsxarch.h)).
* The skeleton encodings are contained in the tables, and then there are methods on the emitter that handle the special encoding constraints for the various instructions, addressing modes, register types, etc.

## GC Info

Reporting of live GC references is done in two ways:

* For stack locations that are not tracked (these could be spill locations or lvlVars – local variables or temps – that are not register candidates), they are initialized to null in the prolog, and reported as live for the entire method.
* For lvlVars with tracked lifetimes, or for expression involving GC references, we report the range over which the reference is live. This is done by the emitter, which adds this information to the instruction group, and which terminates instruction groups when the GC info changes.

The tracking of GC reference lifetimes is done via the `GCInfo` class in the JIT. It is declared in [src/jit/jitgcinfo.h](https://github.com/dotnet/coreclr/blob/master/src/jit/jitgcinfo.h) (to differentiate it from [src/inc/gcinfo.h](https://github.com/dotnet/coreclr/blob/master/src/inc/gcinfo.h)), and implemented in [src/jit/gcinfo.cpp](https://github.com/dotnet/coreclr/blob/master/src/jit/gcinfo.cpp).

In a JitDump, the generated GC info can be seen following the “In gcInfoBlockHdrSave()” line.

## Debugger info

Debug info consists primarily of two types of information in the JIT:

* Mapping of IL offsets to native code offsets. This is accomplished via:
  * the `gtStmtILoffsx` on the statement nodes (`GenTreeStmt`)
  * the `gtLclILoffs` on lvlVar references (`GenTreeLclVar`)
  * The IL offsets are captured during CodeGen by calling `CodeGen::genIPmappingAdd()`, and then written to debug tables by `CodeGen::genIPmappingGen()`.
* Mapping of user locals to location (register or stack). This is accomplished via:
  * Struct `siVarLoc` (in [compiler.h](https://github.com/dotnet/coreclr/blob/master/src/jit/compiler.h)) captures the location
  * `VarScopeDsc` ([compiler.h](https://github.com/dotnet/coreclr/blob/master/src/jit/compiler.h)) captures the live range of a local variable in a given location.

## Exception handling

Exception handling information is captured in an `EHblkDsc` for each exception handling region. Each region includes the first and last blocks of the try and handler regions, exception type, enclosing region, among other things. Look at [jiteh.h](https://github.com/dotnet/coreclr/blob/master/src/jit/jiteh.h) and [jiteh.cpp](https://github.com/dotnet/coreclr/blob/master/src/jit/jiteh.cpp), especially, for details. Look at `Compiler::fgVerifyHandlerTab()` to see how the exception table constraints are verified.

# Dumps and Other Tools

The behavior of the JIT can be controlled via a number of configuration variables. These are declared in [inc/clrconfigvalues.h](https://github.com/dotnet/coreclr/blob/master/src/inc/clrconfigvalues.h). When used as an environment variable, the string name generally has “COMPlus_” prepended. When used as a registry value name, the configuration name is used directly.

## Setting configuration variables

These can be set in one of three ways:

* Setting the environment variable `COMPlus_<flagname>`. For example, the following will set the `JitDump` flag so that the compilation of all methods named ‘Main’ will be dumped:

    set COMPlus_JitDump=Main

* Setting the registry key `HKCU\Software\Microsoft\.NETFramework`, Value `<flagName>`, type `REG_SZ` or `REG_DWORD` (depending on the flag).
* Setting the registry key `HKLM\Software\Microsoft\.NETFramework`, Value `<flagName>`, type `REG_SZ` or `REG_DWORD` (depending on the flag).

## Specifying method names

The complete syntax for specifying a single method name (for a flag that takes a method name, such as `COMPlus_JitDump`) is:

		[[<Namespace>.]<ClassName>::]<MethodName>[([<types>)]

For example

		System.Object::ToString(System.Object)

The namespace, class name, and argument types are optional, and if they are not present, default to a wildcard. Thus stating:

		Main

will match all methods named Main from any class and any number of arguments.

<types> is a comma separated list of type names. Note that presently only the number of arguments and not the types themselves are used to distinguish methods. Thus, Main(Foo, Bar), and Main(int, int) will both match any main method with two arguments.

The wildcard character ‘*’ can be used for <ClassName> and <MethodName>. In particular * by itself indicates every method.

## Useful COMPLUS variables

Below are some of the most useful `COMPLUS` variables. Where {method-list} is specified in the list below, you can supply a space-separated list of either fully-qualified or simple method names (the former is useful when running something that has many methods of the same name), or you can specific ‘*’ to mean all methods.

* `COMPlus_JitDump`={method-list} – dump lots of useful information about what the JIT is doing (see below).
* `COMPlus_JitDisasm`={method-list} – dump a disassembly listing of each method.
* `COMPlus_JitDiffableDasm` – set to 1 to tell the JIT to avoid printing things like pointer values that can change from one invocation to the next, so that the disassembly can be more easily compared.
* `COMPlus_JitGCDump`={method-list} – dump the GC information.
* `COMPlus_JitUnwindDump`={method-list} – dump the unwind tables.
* `COMPlus_JitEHDump`={method-list} – dump the exception handling tables.
* `COMPlus_JitTimeLogFile`={file name} – this specifies a log file to which timing information is written.
* `COMPlus_JitTimeLogCsv`={file name} – this specifies a log file to which summary timing information can be written, in CSV form.

See also: [CLR Configuration Knobs](../project-docs/clr-configuration-knobs.md)

# Reading a JitDump

One of the best ways of learning about the JIT compiler is examining a compilation dump in detail. The dump shows you all the really important details of the basic data structures without all the implementation detail of the code. Debugging a JIT bug almost always begins with a JitDump. Only after the problem is isolated by the dump does it make sense to start debugging the JIT code itself.

Dumps are also useful because they give you good places to place breakpoints. If you want to see what is happening at some point in the dump, simply search for the dump text in the source code. This gives you a great place to put a conditional breakpoint.

There is not a strong convention about what or how the information is dumped, but generally you can find phase-specific information by searching for the phase name. Some useful points follow.

## Reading expression trees

It takes some time to learn to “read” the expression trees, which are printed with the children indented from the parent, and, for binary operators, with the first operand below the parent and the second operand above.

Here is an example dump

    [000027] ------------             ▌  stmtExpr  void  (top level) (IL 0x010...  ???)
    [000026] --C-G-------             └──▌  return    double
    [000024] --C-G-------                └──▌  call      double BringUpTest.DblSqrt
    [000021] ------------                   │     ┌──▌  lclVar    double V02 arg2
    [000022] ------------                   │  ┌──▌  -         double
    [000020] ------------                   │  │  └──▌  lclVar    double V03 loc0
    [000023] ------------ arg0              └──▌  *         double
    [000017] ------------                      │     ┌──▌  lclVar    double V01 arg1
    [000018] ------------                      │  ┌──▌  -         double
    [000016] ------------                      │  │  └──▌  lclVar    double V03 loc0
    [000019] ------------                      └──▌  *         double
    [000013] ------------                         │     ┌──▌  lclVar    double V00 arg0
    [000014] ------------                         │  ┌──▌  -         double
    [000012] ------------                         │  │  └──▌  lclVar    double V03 loc0
    [000015] ------------                         └──▌  *         double
    [000011] ------------                            └──▌  lclVar    double V03 loc0

The tree nodes are indented to represent the parent-child relationship. Binary operators print first the right hand side, then the operator node itself, then the left hand side. This scheme makes sense if you look at the dump “sideways” (lean your head to the left). Oriented this way, the left hand side operator is actually on the left side, and the right hand operator is on the right side so you can almost visualize the tree if you look at it sideways. The indentation level is also there as a backup.

Tree nodes are identified by their `gtTreeID`. This field only exists in DEBUG builds, but is quite useful for debugging, since all tree nodes are created from the routine `gtNewNode` (in [src/jit/gentree.cpp](https://github.com/dotnet/coreclr/blob/master/src/jit/gentree.cpp)). If you find a bad tree and wish to understand how it got corrupted, you can place a conditional breakpoint at the end of `gtNewNode` to see when it is created, and then a data breakpoint on the field that you believe is corrupted.

The trees are connected by line characters (either in ASCII, by default, or in slightly more readable Unicode when `COMPlus_JitDumpAscii=0` is specified), to make it a bit easier to read.

    N037 (  0,  0) [000391] ----------L- arg0 SETUP  │  ┌──▌  argPlace  ref    REG NA $1c1
    N041 (  2,  8) [000389] ------------             │  │     ┌──▌  const(h) long 0xB410A098 REG rcx $240
    N043 (  4, 10) [000390] ----G-------             │  │  ┌──▌  indir     ref    REG rcx $1c1
    N045 (  4, 10) [000488] ----G------- arg0 in rcx │  ├──▌  putarg_reg ref    REG rcx
    N049 ( 18, 16) [000269] --C-G-------             └──▌  call void System.Diagnostics.TraceInternal.Fail $VN.Void

## Variable naming

The dump uses the index into the local variable table as its name. The arguments to the function come first, then the local variables, then any compiler generated temps. Thus in a function with 2 parameters (remember “this” is also a parameter), and one local variable, the first argument would be variable 0, the second argument variable 1, and the local variable would be variable 2. As described earlier, tracked variables are given a tracked variable index which identifies the bit for that variable in the dataflow bit vectors. This can lead to confusion as to whether the variable number is its index into the local variable table, or its tracked index. In the dumps when we refer to a variable by its local variable table index we use the ‘V’ prefix, and when we print the tracked index we prefix it by a ‘T’.

## References

<a name="[1]"/>
[1] P. Briggs, K. D. Cooper, T. J. Harvey, and L. T. Simpson, "Practical improvements to the construction and destruction of static single assignment form," Software --- Practice and Experience, vol. 28, no. 8, pp. 859---881, Jul. 1998.

<a name="[2]"/>
[2] Wimmer, C. and Mössenböck, D. "Optimized Interval Splitting in a Linear Scan Register Allocator," ACM VEE 2005, pp. 132-141. [http://portal.acm.org/citation.cfm?id=1064998&dl=ACM&coll=ACM&CFID=105967773&CFTOKEN=80545349](http://portal.acm.org/citation.cfm?id=1064998&dl=ACM&coll=ACM&CFID=105967773&CFTOKEN=80545349)
