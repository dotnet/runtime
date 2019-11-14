First Class Structs
===================

Objectives
----------
Primary Objectives
- Avoid forcing structs to the stack if they are only assigned to/from, or passed to/returned
  from a call or intrinsic
  - Including SIMD types as well as other pointer-sized-or-less struct types
- Enable enregistration of structs that have no field accesses
- Optimize struct types as effectively as primitive types
  - Value numbering, especially for types that are used in intrinsics (e.g. SIMD)
  - Register allocation

Secondary Objectives
* No “swizzling” or lying about struct types – they are always struct types
 - No confusing use of GT_LCL_FLD to refer to the entire struct as a different type

Normalizing Struct Types
------------------------
We would like to facilitate full enregistration of structs with the following properties:
1. Its fields are infrequently accessed, and
1. The entire struct fits into a register, and
2. Its value is used or defined in a register
(i.e. as an argument to or return value from calls or intrinsics).

In RyuJIT, the concept of a type is very simplistic (which helps support the high throughput
of the JIT). Rather than a symbol table to hold the properties of a type, RyuJIT primarily
deals with types as simple values of an enumeration. When more detailed information is
required about the structure of a type, we query the type system, across the JIT/EE interface.
This is generally done only during the importer (translation from MSIL to the RyuJIT IR),
during struct promotion analysis, and when determining how to pass or return struct values.
As a result, struct types are generally treated as an opaque type
(TYP_STRUCT) of unknown size and structure.

In order to treat fully-enregisterable struct types as "first class" types in RyuJIT, we
 created new types to represent vectors, in order for the JIT to support operations on them:
* `TYP_SIMD8`, `TYP_SIMD12`, `TYP_SIMD16` and (where supported by the target) `TYP_SIMD32`.
 - These types already exist, and represent some already-completed steps toward First Class Structs.

 We had previously proposed to create additional types to be used where struct types of the given size are
 passed and/or returned in registers:
 * `TYP_STRUCT1`, `TYP_STRUCT2`, `TYP_STRUCT4`, `TYP_STRUCT8` (on 64-bit systems)

However, further discussions have suggested that this may not be necessary. Rather, storage decisions
should largely be deferred to the backend (`Lowering` and register allocation).

The following transformations need to be supported effectively for all struct types:
- Optimizations such as CSE and assertion propagation
  - Depends on being able to discern when instances of these types are equivalent.
- Passing and returning these values to and from methods
  - Avoiding unnecessarily copying these values between registers or on the stack.
  - Allowing promoted structs to be passed or returned without forcing them to become
    ineligible for register allocation.

Correct and effective code generation for structs requires that the JIT have ready access
to information about the shape and size of the struct. This information is obtained from the VM
over the JIT/EE interface. This includes:
- Struct size
- Number and type of fields, especially if there are GC references

With the changes from @mikedn in
[#21705 Pull struct type info out of GenTreeObj](https://github.com/dotnet/coreclr/pull/21705)
this information is captured in a `ClassLayout` object. This will make it possible to retain
this shape information on all struct-typed nodes, without impacting node size.

Current Representation of Struct Values
---------------------------------------
### Importer-Only Struct Values

These struct-typed nodes are created by the importer, but transformed in morph, and so are not
encountered by most phases of the JIT:
* `GT_INDEX`: This is transformed to a `GT_IND`
  * Currently, the IND is marked with `GTF_IND_ARR_INDEX` and the node pointer of the `GT_IND` acts as a key
    into the array info map.
  * Proposed: This should be transformed into a `GT_OBJ` when it represents a struct type, and then the
    class handle would no longer need to be obtained from the array info map.
* `GT_FIELD`: This is transformed to a `GT_LCL_FLD` or a `GT_IND`
  * Proposed: A struct typed field should be transformed into a `GT_OBJ`.
* `GT_MKREFANY`: This produces a "known" struct type, which is currently obtained by
  calling `impGetRefAnyClass()` which is a call over the JIT/EE interface. This node is always
  eliminated, and its source address used to create a copy. If it is on the rhs
  of an assignment, it will be eliminated during the importer. If it is a call argument it will
  be eliminated during morph.
  * The presence of any of these in a method disables struct promotion. See `case CEE_MKREFANY` in the
    `Importer`, where it is asserted that these are rare, and therefore not worth the trouble to handle.

### Struct “objects” as lvalues

* The lhs of a struct assignment is a block node or local
  * `GT_OBJ` nodes represent the “shape” info via a struct handle, along with the GC info
    (location and type of GC references within the struct).
    * These are currently used only to represent struct values that contain GC references (although see below).
  * `GT_BLK` nodes represent struct types with no GC references, or opaque blocks of fixed size.
    * These have no struct handle, resulting in some pessimization or even incorrect
      code when the appropriate struct handle can't be determined.
    * These never represent lvalues of structs that contain GC references.
    * Proposed: When a class handle is available, these would remain as `GT_OBJ` since after
      [#21705](https://github.com/dotnet/coreclr/pull/21705) they are no longer large nodes.
  * `GT_STORE_OBJ` and `GT_STORE_BLK` have the same structure as `GT_OBJ` and `GT_BLK`, respectively
    * `Data()` is op2
  * `GT_DYN_BLK` and `GT_STORE_DYN_BLK` (GenTreeDynBlk extends GenTreeBlk)
    * Additional child `gtDynamicSize`
    * Note that these aren't really struct types; they represent dynamically sized blocks
      of arbitrary data.
  * For `GT_LCL_FLD` nodes, we don't retain shape information, except indirectly via the `FieldSeqNode`.
  * For `GT_LCL_VAR` nodes, the`ClassLayout` is obtained from the `LclVarDsc`.

### Struct “objects” as rvalues

Structs only appear as rvalues in the following contexts:

* On the RHS of an assignment
  * The lhs provides the “shape” for an assignment. Note, however, that the LHS isn't always available
    to optimizations, which has led to pessimization, e.g.
    [#23739 Block the hoisting of TYP_STRUCT rvalues in loop hoisting](https://github.com/dotnet/coreclr/pull/23739)

* As a call argument
  * In this context, it must be one of: `GT_OBJ`, `GT_LCL_VAR`, `GT_LCL_FLD` or `GT_FIELD_LIST`

* As an operand to a hardware or SIMD intrinsic (for `TYP_SIMD*` only)
  * In this case the struct handle is generally assumed to be unneeded, as it is captured (directly or
    indirectly) in the `GT_SIMD` or `GT_HWINTRINSIC` node.

After morph, a struct-typed value on the RHS of assignment is one of:
* `GT_IND`: in this case the LHS is expected to provide the struct handle
  * Proposed: `GT_IND` would no longer be used for struct types
* `GT_CALL`
* `GT_LCL_VAR`
* `GT_LCL_FLD`
  * Proposed: `GT_LCL_FLD` would never be used to represent a reference to the full struct (e.g. as a different type).
* `GT_SIMD`
* `GT_OBJ` nodes can also be used as rvalues when they are call arguments
  * Proposed: `GT_OBJ` nodes can be used in any context where a struct rvalue or lvalue might occur,
    except after morph when the struct is independently promoted.

Struct IR Phase Transitions
---------------------------

There are three phases in the JIT that make changes to the representation of struct nodes and lclVars:

* Importer
  * Vector types are normalized to the appropriate `TYP_SIMD*` type. Other struct nodes have `TYP_STRUCT`.
  * Struct-valued nodes that are created with a class handle will retain either a `ClassLayout`
    pointer or an index into the `ClassLayout` cache.

* Struct promotion
  * Fields of promoted structs become separate lclVars (scalar promoted) with primitive types.
    * This is currently an all-or-nothing choice (either all fields are promoted, or none), and is
      constrained in the number of fields that can be promoted.
    * Proposed: Support additional cases. See [Improve Struct Promotion](#Improve-Struct-Promotion)

* Global morph
  * Some promoted structs are forced to stack, and become “dependently promoted”.
    * Proposed: promoted structs are forced to stack ONLY if address taken.
      * This includes removing unnecessary pessimizations of block copies. See [Improve and Simplify Block Assignment Morphing](#Block-Assignments).

  * Call args
    * If the struct has been promoted it is morphed to `GT_FIELD_LIST`
      * Currently this is done only if it is passed on the stack, or if it is passed
        in registers that exactly match the types of its fields.
      * Proposed: This transformation would be made even if it is passed in non-matching
        registers. The necessary transformations for correct code generation would be
        made in `Lowering`.

    * If it is passed in a single register, it is morphed into a `GT_LCL_FLD` node of the appropriate
      type.
      * This may involve making a copy, if the size cannot be safely loaded.
      * Proposed: This would remain a `GT_OBJ` and would be appropriately transformed in `Lowering`,
        e.g. using `GT_BITCAST`.

    * If is passed in multiple registers
      * A `GT_FIELD_LIST` is constructed that represents the load of each register using `GT_LCL_FLD`.
      * Proposed: This would also remain `GT_OBJ` (or `GT_FIELD_LIST` if promoted) and would be transformed
        to a `GT_FIELD_LIST` with the appropriate load, assemble or extraction code as needed.

    * Otherwise, if it is passed by reference or on the stack, it is kept as `GT_OBJ` or `GT_LCL_VAR`
      * Currently, if passed by reference, the value is either forced to the stack or copied.
      * Proposed: This transformation would also be deferred until `Lowering`, at which time the
        liveness information can provide `lastUse` information to allow a dead struct to be passed
        directly by reference instead of being copied.

It is proposed to add the following transformations in `Lowering`:
* Transform struct values that are passed to or returned from calls by creating one or more of the following:
  * `GT_FIELD_LIST` of `GT_LCL_FLD` when the struct is non-enregisterable and is passed in multiple
    registers.
  * `GT_BITCAST` when a promoted floating point field of a single-field struct is passed in an integer register.
  * A sequence of nodes to assemble or extract promoted fields
* Introduce copies as needed for non-last-use struct values that are passed by reference.

Work Items
----------
This is a preliminary breakdown of the work into somewhat separable tasks.
These work items are organized in priority order. Each work item should be able to
proceed independently, though the aggregate effect of multiple work items may be greater
than the individual work items alone.

### <a name="defer-abi-specific-transformations-to-lowering"/>Defer ABI-specific transformations to Lowering

This includes all copies and IR transformations that are only required to pass or return the arguments
as required by the ABI.

Other transformations would remain:
  * Copies required to satisfy ordering constraints
  * Transformations (e.g. `GT_FIELD_LIST` creation) required to expose references
    to promoted struct fields.

This would be done in multiple phases:
  * First, move transformations other than those listed above to `Lowering`, but retain any "pessimizations"
    (e.g. marking nodes as `GTF_DONT_CSE` or marking lclVars as `lvDoNotEnregister`)
  * Add support for passing vector types in the SSE registers for x64/ux
    * This will also involve modifying code in the VM. See [#23675 Arm64 Vector ABI](https://github.com/dotnet/coreclr/pull/23675)
      for a general idea of the kinds of VM changes that may be required.
  * Defer retyping of struct return types (`Compiler::impFixupStructReturnType()` and
    `Compiler::impFixupCallStructReturn()`)
    * This is probably the "right" way to fix [#26491](https://github.com/dotnet/coreclr/issues/26491).
  * Next, eliminate the "pessimizations".
    * For cases where `GT_LCL_FLD` is currently used to "retype" the struct, change it to use *either*
      `GT_LCL_FLD`, if it is already address-taken, or to use a `GT_BITCAST` otherwise.
      * This work item should address issue #1161 (test is `JIT\Regressions\JitBlue\GitHub_1161`) and #8828.
    * Add support in prolog to extract fields, and remove the restriction of not promoting incoming reg
      structs that have more than one field. Note that SIMD types are already reassembled in the prolog.
    * Add support in `Lowering` and `CodeGen` to handle call arguments where the fields of a promoted struct
      must be extracted or reassembled in order to pass the struct in non-matching registers. This includes
      producing the appropriate IR.
    * Add support for extracting the fields for the returned struct value of a call (in registers or on
      stack), producing the appropriate IR.
    * Add support for assembling non-matching fields into registers for call args and returns.
    * For arm64, add support for loading non-promoted or non-local structs with ldp
    * The removal of each of these pessimizations should result in improved code generation
      in cases where previously disabled optimizations are now enabled.
  * Other ABI-related issues:
    * [#8289](https://github.com/dotnet/coreclr/issues/8289) - code generation for x86 promoted struct args.

Related issues: #1133 (maybe), #4766, #23675, #23129

### Fully Enable Struct Optimizations

Most of the existing places in the code where structs are handled conservatively are marked
with `TODO-1stClassStructs`. This work item involves investigating these and making the
necessary improvements (or determining that they are infeasible and removing the `TODO`).
Some of these, such as the handling of `TYP_SIMD8` in LSRA, may be addressed by other work items.

Related: #2003, #18542 (maybe), #19733 (maybe)

### Support Full Enregistration of Struct Types

This would be enabled first by [Defer ABI-specific transformations to Lowering](#defer-abi-specific-transformations-to-lowering). Then the register allocator would consider them as candidates for enregistration.
  * First, fully enregister pointer-sized-or-less structs only if there are no field accesses and they are not
    marked `lvDoNotEnregister`.
  * Next, fully enregister structs that are passed or returned in multiple registers and have no field accesses.
  * Next, when there are field accesses, but the struct is more frequently accessed as a
    full struct (e.g. assignment or passing as the full struct), `Lowering` would expand the field accesses
    as needed to extract the field from the register(s).
    * An initial investigation should be undertaken to determine if this is worthwhile.

  * Related: #11407, #17257

###  <a name="Improve-Struct-Promotion"/>Improve Struct Promotion
 * Support recursive (nested) struct promotion, especially when struct field itself has a single field
   (#10019, #9594, #7313)
 * Support partial struct promotion when some fields are more frequently accessed.
 * Aggressively promote lclVar struct incoming or outgoing args or returns whose fields match the ABI requirements.
   * This should address [\#26710](https://github.com/dotnet/coreclr/issues/26710).
 * Aggressively promote pointer-sized fields of structs used as args or returns
 * Allow struct promotion of locals that are passed or returned in a way that doesn't match
   the field types.
 * Investigate whether it would be useful to re-type single-field structs, rather than creating new lclVars.
   This would complicate type analysis when copied, passed or returned, but would avoid unnecessarily expanding
   the lclVar data structures.
 * Allow promotion of 32-byte SIMD on 16-byte alignment [\#24368](https://github.com/dotnet/coreclr/issues/24368)
 * Related: #6839, #9477, #16887
 * Also, #11888, which suggests adding a struct promotion stress mode.

### <a name="Block-Assignments"/>Improve and Simplify Block and Block Assignment Morphing

* `fgMorphOneAsgBlockOp` should probably be eliminated, and its functionality either moved to
  `Lowering` or simply subsumed by the combination of the addition of fixed-size struct types and
  the full enablement of struct optimizations. Doing so would also involve improving code generation
  for block copies. See [\#21711 Improve init/copy block codegen](https://github.com/dotnet/coreclr/pull/21711).

* This also includes cleanup of the block morphing methods such that block nodes needn't be visited multiple
  times, such as `fgMorphBlkToInd` (may be simply unneeded), `fgMorphBlkNode` and `fgMorphBlkOperand`.
  These methods were introduced to preserve old behavior, but should be simplified.

* Somewhat related is the handling of struct-typed array elements. Currently, after the `GT_INDEX` is transformed
  into a `GT_IND`, that node must be retained as the key into the `ArrayInfoMap`. For structs, this is then
  wrapped in `OBJ(ADDR(...))`. We should be able to change the IND to OBJ and avoid wrapping, and should also be
  able to remove the class handle from the array info map and instead used the one provided by the `GT_OBJ`.

Struct-Related Issues in RyuJIT
-------------------------------
The following issues illustrate some of the motivation for improving the handling of value types
(structs) in RyuJIT:

* [\#11407 [RyuJIT] Fully enregister structs that fit into a single register when profitable](https://github.com/dotnet/coreclr/issues/11407), also VSO Bug 98404: .NET JIT x86 - poor code generated for value type initialization
  * This is a simple test case that should generate simply `xor eax; ret` on x86 and x64, but
    instead generates many unnecessary copies. It is addressed by full enregistration of
    structs that fit into a register. See [Support Full Enregistration of Struct Types](#support-full-enregistration-of-struct-types):

```C#
struct foo { public byte b1, b2, b3, b4; }
static foo getfoo() { return new foo(); }
```

* [\#1133 JIT: Excessive copies when inlining](https://github.com/dotnet/coreclr/issues/1133)
  * The scenario given in this issue involves a struct that is larger than 8 bytes, so
    it is not impacted by the fixed-size types. However, by enabling value numbering and assertion propagation
    for struct types (which, in turn is made easier by using normal assignments), the
    excess copies can be eliminated.
    * Note that these copies are not generated when passing and returning scalar types,
      and it may be worth considering (in future) whether we can avoiding adding them
      in the first place.
  * This case may now be handled; needs verification

* [\#1161  RyuJIT properly optimizes structs with a single field if the field type is int but not if it is double](https://github.com/dotnet/coreclr/issues/1161)
  * This issue arises because we never promote a struct with a single double field, due to
    the fact that such a struct may be passed or returned in a general purpose register.
    This issue could be addressed independently, but should "fall out" of improved heuristics
    for when to promote and enregister structs.
  * Related: [\#8828](https://github.com/dotnet/coreclr/issues/8828)

* [\#1636 Add optimization to avoid copying a struct if passed by reference and there are no
  writes to and no reads after passed to a callee](https://github.com/dotnet/coreclr/issues/1636).
  * This issue is related to #1133, except that in this case the desire is to
    eliminate unneeded copies locally (i.e. not just due to inlining), in the case where
    the struct may or may not be passed or returned directly.
  * Unfortunately, there is not currently a scenario or test case for this issue.

* [\#19425 Unix: Unnecessary struct copy while passsing struct of size <=16](https://github.com/dotnet/coreclr/issues/19425)
* [\#16619 [RyuJIT] Eliminate unecessary copies when passing structs](https://github.com/dotnet/coreclr/issues/16619)
  * These require changing both the callsite and the callee to avoid copying the parameter onto the stack.

* [\#3144 Avoid marking tmp as DoNotEnregister in tmp=GT_CALL() where call returns a
  enregisterable struct in two return registers](https://github.com/dotnet/coreclr/issues/3144)
  * This issue could be addressed without First Class Structs. However, it
    should be done along with the streamlining of the handling of ABI-specific struct passing
    and return values.

* [\#4766 Pi-Digits: Extra Struct copies of BigInteger](https://github.com/dotnet/coreclr/issues/4766)
  * In addition to suffering from the same issue as #1133, this has a struct that is promoted even though it is
    passed (by reference) to its non-inlined constructor. This means that any copy to/from this struct will be field-by-field.

* [\#11816 Extra zeroing with structs and inlining](https://github.com/dotnet/coreclr/issues/11816)
  * This issue illustrates the failure of the JIT to eliminate zero-initialization of structs that are subsequently fully
    defined. It is a related but somewhat different manifestation of the issue in #1133, i.e. that structs are not
    fully supported in value numbering and optimization.

* [\#12865 JIT: inefficient codegen for calls returning 16-byte structs on Linux x64](https://github.com/dotnet/coreclr/issues/12865)
  * This is related to #3144, and requires supporting the assignment of a multi-reg call return into a promoted local variable,
    and enabling subsequent elimination of any redundant copies.

* [\#22445](https://github.com/dotnet/coreclr/issues/22445) and [\#22319](https://github.com/dotnet/coreclr/issues/22319)
  * These are both cases where we introduce a `GT_LCL_FLD` to retype a value that needs
    to be passed in a register.

## Other Struct-related Issues

* [\#17207](https://github.com/dotnet/coreclr/issues/17207)
  * This suffers from pessimization due to poor handling of conversion (`Unsafe.As`) from `Quaternion` to `Vector4`.
    It's not immediately clear what's the best way to improve this.

* [#7740](https://github.com/dotnet/coreclr/issues/7740)
  * Addressing mode expression optimization for struct fields

Sample IR
---------

*** Note: These IR samples have not been updated to correspond to the current state of the IR ***

### Bug 11407
#### Before

The `getfoo` method initializes a struct of 4 bytes.
The dump of the (single) local variable is included to show the change from `struct (8)` to
`struct4`, as the "exact size" of the struct is 4 bytes.
Here is the IR after Import:

```
;  V00 loc0           struct ( 8)

   ▌  stmtExpr  void  (top level) (IL 0x000...  ???)
   │  ┌──▌  const     int    4
   └──▌  initBlk   void
      │  ┌──▌  const     int    0
      └──▌  <list>    void
         └──▌  addr      byref
            └──▌  lclVar    struct V00 loc0

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   └──▌  return    int
      └──▌  lclFld    int    V00 loc0         [+0]
```
This is how it currently looks just before code generation:
```
   ▌  stmtExpr  void  (top level) (IL 0x000...0x003)
   │  ┌──▌  const     int    0 REG rax $81
   │  ├──▌  &lclVar   byref  V00 loc0         d:3 REG NA
   └──▌  storeIndir int    REG NA

   ▌  stmtExpr  void  (top level) (IL 0x008...0x009)
   │  ┌──▌  lclFld    int    V00 loc0         u:3[+0] (last use) REG rax $180
   └──▌  return    int    REG NA $181
```
And here is the resulting code:
```
  push     rax
  xor      rax, rax
  mov      qword ptr [V00 rsp], rax
  xor      eax, eax
  mov      dword ptr [V00 rsp], eax
  mov      eax, dword ptr [V00 rsp]
  add      rsp, 8
  ret
```
#### After
Here is the IR after Import with the prototype First Class Struct changes.
Note that the fixed-size struct variable is assigned and returned just as for a scalar type.

```
;  V00 loc0          struct4

   ▌  stmtExpr  void  (top level) (IL 0x000...  ???)
   │  ┌──▌  const     int    0
   └──▌  =         struct4 (init)
      └──▌  lclVar    struct4 V00 loc0


   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   └──▌  return    struct4
      └──▌  lclVar    struct4    V00 loc0
```
And Here is the resulting code just prior to code generation:
```
   ▌  stmtExpr  void  (top level) (IL 0x008...0x009)
   │  ┌──▌  const     struct4    0 REG rax $81
   └──▌  return    struct4    REG NA $140
```
Finally, here is the resulting code that we were hoping to achieve:
```
  xor      eax, eax
```

### Issue 1133:
#### Before

Here is the IR after Inlining for the `TestValueTypesInInlinedMethods` method that invokes a
sequence of methods that are inlined, creating a sequence of copies.
Because this struct type does not fit into a single register, the types do not change (and
therefore the local variable table is not shown).

```
   ▌  stmtExpr  void  (top level) (IL 0x000...0x003)
   │  ┌──▌  const     int    16
   └──▌  initBlk   void
      │  ┌──▌  const     int    0
      └──▌  <list>    void
         └──▌  addr      byref
            └──▌  lclVar    struct V00 loc0

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  const     int    16
   └──▌  copyBlk   void
      │  ┌──▌  addr      byref
      │  │  └──▌  lclVar    struct V00 loc0
      └──▌  <list>    void
         └──▌  addr      byref
            └──▌  lclVar    struct V01 tmp0

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  const     int    16
   └──▌  copyBlk   void
      │  ┌──▌  addr      byref
      │  │  └──▌  lclVar    struct V01 tmp0
      └──▌  <list>    void
         └──▌  addr      byref
            └──▌  lclVar    struct V02 tmp1

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  const     int    16
   └──▌  copyBlk   void
      │  ┌──▌  addr      byref
      │  │  └──▌  lclVar    struct V02 tmp1
      └──▌  <list>    void
         └──▌  addr      byref
            └──▌  lclVar    struct V03 tmp2

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   └──▌  call help long   HELPER.CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
      ├──▌  const     long   0x7ff918494e10
      └──▌  const     int    1

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  const     int    16
   └──▌  copyBlk   void
      │  ┌──▌  addr      byref
      │  │  └──▌  lclVar    struct V03 tmp2
      └──▌  <list>    void
         │  ┌──▌  const     long   8 Fseq[#FirstElem]
         └──▌  +         byref
            └──▌  field     ref    s_dt

   ▌  stmtExpr  void  (top level) (IL 0x00E...  ???)
   └──▌  return    void
```
And here is the resulting code:
```
sub      rsp, 104
xor      rax, rax
mov      qword ptr [V00 rsp+58H], rax
mov      qword ptr [V00+0x8 rsp+60H], rax
xor      rcx, rcx
lea      rdx, bword ptr [V00 rsp+58H]
vxorpd   ymm0, ymm0
vmovdqu  qword ptr [rdx], ymm0
vmovdqu  ymm0, qword ptr [V00 rsp+58H]
vmovdqu  qword ptr [V01 rsp+48H]ymm0, qword ptr
vmovdqu  ymm0, qword ptr [V01 rsp+48H]
vmovdqu  qword ptr [V02 rsp+38H]ymm0, qword ptr
vmovdqu  ymm0, qword ptr [V02 rsp+38H]
vmovdqu  qword ptr [V03 rsp+28H]ymm0, qword ptr
mov      rcx, 0x7FF918494E10
mov      edx, 1
call     CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
mov      rax, 0x1FAC6EB29C8
mov      rax, gword ptr [rax]
add      rax, 8
vmovdqu  ymm0, qword ptr [V03 rsp+28H]
vmovdqu  qword ptr [rax], ymm0
add      rsp, 104
ret
```

#### After
After fginline:
(note that the obj node will become a blk node downstream).
```
   ▌  stmtExpr  void  (top level) (IL 0x000...0x003)
   │  ┌──▌  const     int    0
   └──▌  =         struct (init)
      └──▌  lclVar    struct V00 loc0

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  lclVar    struct V00 loc0
   └──▌  =         struct (copy)
      └──▌  lclVar    struct V01 tmp0

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  lclVar    struct V01 tmp0
   └──▌  =         struct (copy)
      └──▌  lclVar    struct V02 tmp1

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  lclVar    struct V02 tmp1
   └──▌  =         struct (copy)
      └──▌  lclVar    struct V03 tmp2

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   └──▌  call help long   HELPER.CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
      ├──▌  const     long   0x7ff9184b4e10
      └──▌  const     int    1

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  lclVar    struct V03 tmp2
   └──▌  =         struct (copy)
      └──▌  obj(16)   struct
         │  ┌──▌  const     long   8 Fseq[#FirstElem]
         └──▌  +         byref
            └──▌  field     ref    s_dt

   ▌  stmtExpr  void  (top level) (IL 0x00E...  ???)
   └──▌  return    void
```
Here is the IR after fgMorph:
Note that copy propagation has propagated the zero initialization through to the final store.
```
   ▌  stmtExpr  void  (top level) (IL 0x000...0x003)
   │  ┌──▌  const     int    0
   └──▌  =         struct (init)
      └──▌  lclVar    struct V00 loc0

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  const     struct 0
   └──▌  =         struct (init)
      └──▌  lclVar    struct V01 tmp0

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  const     struct 0
   └──▌  =         struct (init)
      └──▌  lclVar    struct V02 tmp1

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  const     struct 0
   └──▌  =         struct (init)
      └──▌  lclVar    struct V03 tmp2

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   └──▌  call help long   HELPER.CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
      ├──▌  const     long   0x7ffc8bbb4e10
      └──▌  const     int    1

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  const     struct 0
   └──▌  =         struct (init)
      └──▌  obj(16)   struct
         │  ┌──▌  const     long   8 Fseq[#FirstElem]
         └──▌  +         byref
            └──▌  indir     ref
               └──▌  const(h)  long   0x2425b6229c8 static Fseq[s_dt]

   ▌  stmtExpr  void  (top level) (IL 0x00E...  ???)
   └──▌  return    void

```
After liveness analysis the dead stores have been eliminated:
```
   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   └──▌  call help long   HELPER.CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
      ├──▌  const     long   0x7ffc8bbb4e10
      └──▌  const     int    1

   ▌  stmtExpr  void  (top level) (IL 0x008...  ???)
   │  ┌──▌  const     struct 0
   └──▌  =         struct (init)
      └──▌  obj(16)   struct
         │  ┌──▌  const     long   8 Fseq[#FirstElem]
         └──▌  +         byref
            └──▌  indir     ref
               └──▌  const(h)  long   0x2425b6229c8 static Fseq[s_dt]

   ▌  stmtExpr  void  (top level) (IL 0x00E...  ???)
   └──▌  return    void
```
And here is the resulting code, going from a code size of 129 bytes down to 58.
```
sub      rsp, 40
mov      rcx, 0x7FFC8BBB4E10
mov      edx, 1
call     CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
xor      rax, rax
mov      rdx, 0x2425B6229C8
mov      rdx, gword ptr [rdx]
add      rdx, 8
vxorpd   ymm0, ymm0
vmovdqu  qword ptr [rdx], ymm0
add      rsp, 40
ret
```
