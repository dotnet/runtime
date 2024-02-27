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

Struct Types in RyuJIT
----------------------

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
 - The are used to implement both the platform-independent (`Vector2`, `Vector3`, `Vector4` and `Vector<T>`)
   types as well as the types used for platform-specific hardware intrinsics ('`Vector64<T>`, `Vector128<T>`
   and `Vector256<T>`).
 - These types are useful not only for enregistration purposes, but also because we can have
   values of these types that are produced by computational `SIMD` and `HWIntrinsic` nodes.

 We had previously proposed to create additional types to be used where struct types of the given size are
 passed and/or returned in registers:
 * `TYP_STRUCT1`, `TYP_STRUCT2`, `TYP_STRUCT4`, `TYP_STRUCT8` (on 64-bit systems)

However, further investigation and implementation has suggested that this may not be necessary.
Rather, storage decisions should largely be deferred to the backend (`Lowering` and register allocation).

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
this information is captured in a `ClassLayout` object which captures the size and GC layout of a struct type.
The associated `ClassLayoutTable` on the `Compiler` object which supports lookup. This enables associating this
this shape information with all struct-typed nodes, without impacting node size.

Current Representation of Struct Values
---------------------------------------
### Importer-Only Struct Values

These struct-typed nodes are created by the importer, but transformed in morph, and so are not
encountered by most phases of the JIT:
* `GT_FIELD`: This is transformed to a `GT_LCL_VAR` by the `Compiler::fgMarkAddressExposedLocals()` phase
  if it's a promoted struct field, or to a `GT_LCL_FLD` or GT_IND` by `fgMorphField()`.
  * Proposed: A non-promoted struct typed field should be transformed into a `GT_OBJ`, so that consistently all struct
    nodes, even r-values, have `ClassLayout`.
* `GT_MKREFANY`: This produces a "known" struct type, which is currently obtained by
  calling `impGetRefAnyClass()` which is a call over the JIT/EE interface. This node is always
  eliminated, and its source address used to create a copy. If it is on the rhs
  of an assignment, it will be eliminated during the importer. If it is a call argument it will
  be eliminated during morph.
  * The presence of any of these in a method disables struct promotion. See `case CEE_MKREFANY` in the
    `Importer`, where it is asserted that these are rare, and therefore not worth the trouble to handle.

### Struct “objects” as lvalues

* The lhs of a struct assignment is a block or local node:
  * `GT_OBJ` nodes represent struct types with a handle, and store a pointer to the `ClassLayout` object.
  * `GT_BLK` nodes represent struct types with no GC references, or opaque blocks of fixed size.
    * These have no struct handle, resulting in some pessimization or even incorrect
      code when the appropriate struct handle can't be determined.
    * These never represent lvalues of structs that contain GC references.
    * Proposed: When a class handle is available, these would remain as `GT_OBJ` since after
      [#21705](https://github.com/dotnet/coreclr/pull/21705) they are no longer large nodes.
  * `GT_STORE_OBJ` and `GT_STORE_BLK` have the same structure as `GT_OBJ` and `GT_BLK`, respectively
    * `Data()` is op2
  * For `GT_LCL_FLD` nodes, we store a pointer to `ClassLayout` in the node.
  * For `GT_LCL_VAR` nodes, the `ClassLayout` is obtained from the `LclVarDsc`.

### Struct “objects” as rvalues

Structs only appear as rvalues in the following contexts:

* On the RHS of an assignment
  * The lhs provides the “shape” for an assignment. Note, however, that the LHS isn't always available
    to optimizations, which has led to pessimization, e.g.
    [#23739 Block the hoisting of TYP_STRUCT rvalues in loop hoisting](https://github.com/dotnet/coreclr/pull/23739)

* As a call argument
  * In this context, it must be one of: `GT_OBJ`, `GT_LCL_VAR`, `GT_LCL_FLD` or `GT_FIELD_LIST`.

* As an operand to a hardware intrinsic (for `TYP_SIMD*` only)
  * In this case the struct handle is generally assumed to be unneeded, as it is captured (directly or
    indirectly) in the `GT_HWINTRINSIC` node.
  * It would simplify both the recognition and optimization of these nodes if they carried a `ClassLayout`.

After morph, a struct-typed value on the RHS of assignment is one of:
* `GT_IND`: in this case the LHS is expected to provide the struct handle
  * Proposed: `GT_IND` would no longer be used for struct types
* `GT_CALL`
* `GT_LCL_VAR`
* `GT_LCL_FLD`
* `GT_OBJ` nodes can also be used as rvalues when they are call arguments
  * Proposed: `GT_OBJ` nodes can be used in any context where a struct rvalue or lvalue might occur,
    except after morph when the struct is independently promoted.

Ideally, we should be able to obtain a valid `CLASS_HANDLE` for any struct-valued node.
Once that is the case, we should be able to transform most or all uses of `gtGetStructHandleIfPresent()` to
`gtGetStructHandle()`.

Struct IR Phase Transitions
---------------------------

There are three main phases in the JIT that make changes to the representation of struct nodes and lclVars:

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

    * If it is passed in a single register, it is morphed into a `GT_LCL_FLD` node of the appropriate primitive type.
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
        Related: [\#4524 Add optimization to avoid copying a struct if passed by reference and there are no
  writes to and no reads after passed to a callee](https://github.com/dotnet/runtime/issues/4524)

It is proposed to add the following transformations in `Lowering`:
* Transform struct values that are passed to or returned from calls by creating one or more of the following:
  * `GT_FIELD_LIST` of `GT_LCL_FLD` when the struct is non-enregisterable and is passed in multiple
    registers.
  * `GT_BITCAST` when a promoted floating point field of a single-field struct is passed in an integer register.
  * A sequence of nodes to assemble or extract promoted fields
* Introduce copies as needed for non-last-use struct values that are passed by reference.

Work Items
----------
This is a rough breakdown of the work into somewhat separable tasks.
These work items are organized in priority order. Each work item should be able to
proceed independently, though the aggregate effect of multiple work items may be greater
than the individual work items alone.

### <a name="defer-abi-specific-transformations-to-lowering"></a>Defer ABI-specific transformations to Lowering

This includes all copies and IR transformations that are only required to pass or return the arguments
as required by the ABI.

Other transformations would remain:
  * Copies required to satisfy ordering constraints.
  * Transformations (e.g. `GT_FIELD_LIST` creation) required to expose references
    to promoted struct fields.

This would be done in multiple phases:
  * First, move transformations other than those listed above to `Lowering`, but retain any "pessimizations"
    (e.g. marking nodes as `GTF_DONT_CSE` or marking lclVars as `lvDoNotEnregister`)
  * Add support for passing vector types in the SSE registers for x64/ux
    * This will also involve modifying code in the VM.
      [#23675 Arm64 Vector ABI](https://github.com/dotnet/coreclr/pull/23675) added similar support
      for Arm64. The https://github.com/CarolEidt/runtime/tree/X64Vector16ABI branch was intended to
      add this support for 16 byte vectors for .NET 5, but didn't make it into that release.
      The https://github.com/CarolEidt/runtime/tree/FixX64VectorABI branch was an earlier attempt
      to support both 16 and 32 byte vectors, but was abandoned in favor of doing just 16 byte vectors
      first.
  * Next, eliminate the "pessimizations".
    * For cases where `GT_LCL_FLD` is currently used to "retype" the struct, change it to use *either*
      `GT_LCL_FLD`, if it is already address-taken, or to use a `GT_BITCAST` otherwise.
      * This work item should address issue [#4323 RyuJIT properly optimizes structs with a single field
        if the field type is int but not if it is double](https://github.com/dotnet/runtime/issues/4323)
        (test is `JIT\Regressions\JitBlue\GitHub_1161`),
        [#7200 Struct getters are generating unnecessary
        instructions on x64 when struct contains floats](https://github.com/dotnet/runtime/issues/7200)
        and [#11413 Inefficient codegen for casts between same size types](https://github.com/dotnet/runtime/issues/11413).
    * Remove the pessimization in `LocalAddressVisitor::PostOrderVisit()` for the `GT_RETURN` case.
    * Add support in prolog to extract fields, and remove the restriction of not promoting incoming reg
      structs whose fields do not match the register count or types.
      Note that SIMD types are already reassembled in the prolog.
    * Add support in `Lowering` and `CodeGen` to handle call arguments where the fields of a promoted struct
      must be extracted or reassembled in order to pass the struct in non-matching registers. This probably
      includes producing the appropriate IR, in order to correctly represent the register requirements.
    * Add support for extracting the fields for the returned struct value of a call (in registers or on
      stack), producing the appropriate IR.
    * Add support for assembling non-matching fields into registers for call args and returns.
    * For arm64, add support for loading non-promoted or non-local structs with ldp
    * The removal of each of these pessimizations should result in improved code generation
      in cases where previously disabled optimizations are now enabled.
  * Other ABI-related issues:
    * [#7048](https://github.com/dotnet/runtime/issues/7048) - code generation for x86 promoted struct args.

Related issues:
  * [#4308 JIT: Excessive copies when inlining](https://github.com/dotnet/runtime/issues/4308) (maybe).
    Test is `JIT\Regressions\JitBlue\GitHub_1133`.
  * [#12219 Inlined struct copies via params, returns and assignment not elided](https://github.com/dotnet/runtime/issues/12219)

### Fully Enable Struct Optimizations

Most of the existing places in the code where structs are handled conservatively are marked
with `TODO-1stClassStructs`. This work item involves investigating these and making the
necessary improvements (or determining that they are infeasible and removing the `TODO`).

Related:

* [#4659 JIT - slow generated code on Release for iterating simple array of struct](https://github.com/dotnet/runtime/issues/4659)
* [#11000 Strange codegen with struct forwarding implementation to another struct](https://github.com/dotnet/runtime/issues/11000) (maybe)

### Support Full Enregistration of Struct Types

This would be enabled first by [Defer ABI-specific transformations to Lowering](#defer-abi-specific-transformations-to-lowering). Then the register allocator would consider them as candidates for enregistration.
  * First, fully enregister pointer-sized-or-less structs only if there are no field accesses and they are not
    marked `lvDoNotEnregister`.
  * Next, fully enregister structs that are passed or returned in multiple registers and have no field accesses.
  * Next, when there are field accesses, but the struct is more frequently accessed as a
    full struct (e.g. assignment or passing as the full struct), `Lowering` would expand the field accesses
    as needed to extract the field from the register(s).
    * An initial investigation should be undertaken to determine if this is worthwhile.

  * Related: [#10045 Accessing a field of a Vector4 causes later codegen to be inefficient if inlined](https://github.com/dotnet/runtime/issues/10045)

###  Improve Struct Promotion

 * Support recursive (nested) struct promotion, especially when struct field itself has a single field:
   * [#7576 Recursive Promotion of structs containing fields of structs with a single pointer-sized field](https://github.com/dotnet/runtime/issues/7576)
   * [#7441 RyuJIT: Allow promotions of structs with fields of struct containing a single primitive field](https://github.com/dotnet/runtime/issues/7441)
   * [#6707 RyuJIT x86: allow long-typed struct fields to be recursively promoted](https://github.com/dotnet/runtime/issues/6707)
 * Support partial struct promotion when some fields are more frequently accessed.
 * Aggressively promote pointer-sized fields of structs used as args or returns
 * Allow struct promotion of locals that are passed or returned in a way that doesn't match
   the field types.
 * Investigate whether it would be useful to re-type single-field structs, rather than creating new lclVars.
   This would complicate type analysis when copied, passed or returned, but would avoid unnecessarily expanding
   the lclVar data structures.
 * Allow promotion of 32-byte SIMD on 16-byte alignment [\#12623](https://github.com/dotnet/runtime/issues/12623)
 * Related:
   * [#6534 Promote (scalar replace) structs with more than 4 fields](https://github.com/dotnet/runtime/issues/6534)
   * [#7395 Heuristic meant to promote structs with no field access should consider the impact of passing to/from a call ](https://github.com/dotnet/runtime/issues/7395)
   * [#9916 RyuJIT generates poor code for a helper method which does return Method(value, value)](https://github.com/dotnet/runtime/issues/9916)
   * [#8227 JIT: add struct promotion stress mode](https://github.com/dotnet/runtime/issues/8227).

### <a name="Block-Assignments"></a>Improve and Simplify Block and Block Assignment Morphing

* `fgMorphOneAsgBlockOp` should probably be eliminated, and its functionality either moved to
  `Lowering` or simply subsumed by the combination of the addition of fixed-size struct types and
  the full enablement of struct optimizations. Doing so would also involve improving code generation
  for block copies. See [\#21711 Improve init/copy block codegen](https://github.com/dotnet/coreclr/pull/21711).

* This also includes cleanup of the block morphing methods such that block nodes needn't be visited multiple
  times, such as `fgMorphBlkNode` and `fgMorphBlkOperand`.
  These methods were introduced to preserve old behavior, but should be simplified.

### Miscellaneous Cleanup

These are all marked with `TODO-1stClassStructs` or `TODO-Cleanup` in the last case:

* The checking at the end of `gtNewTempAssign()` should be simplified.

* When we create a struct assignment, we use `impAssignStruct()`. This code will, in some cases, create
  or re-create address or block nodes when not necessary.

* For Linux X64, the handling of arguments could be simplified. For a single argument (or for the same struct
  class), there may be multiple calls to `eeGetSystemVAmd64PassStructInRegisterDescriptor()`, and in some cases
  (e.g. look for the `TODO-Cleanup` in `fgMakeTmpArgNode()`) there are awkward workarounds to avoid additional
  calls. It might be useful to cache the struct descriptors so we don't have to call across the JIT/EE interface
  for the same struct class more than once. It would also potentially be useful to save the descriptor for the
  current method return type on the `Compiler` object for use when handling `RETURN` nodes.

Struct-Related Issues in RyuJIT
-------------------------------
The following issues illustrate some of the motivation for improving the handling of value types
(structs) in RyuJIT (these issues are also cited above, in the applicable sections):

* [\#4308 JIT: Excessive copies when inlining](https://github.com/dotnet/runtime/issues/4308)
  * The scenario given in this issue involves a struct that is larger than 8 bytes, so
    it is not impacted by the fixed-size types. However, by enabling value numbering and assertion propagation
    for struct types (which, in turn is made easier by using normal assignments), the
    excess copies can be eliminated.
    * Note that these copies are not generated when passing and returning scalar types,
      and it may be worth considering (in future) whether we can avoiding adding them
      in the first place.
  * This case may now be handled; needs verification

* [\#4323  RyuJIT properly optimizes structs with a single field if the field type is int but not if it is double](https://github.com/dotnet/runtime/issues/4323)
  * This issue arises because we never promote a struct with a single double field, due to
    the fact that such a struct may be passed or returned in a general purpose register.
    This issue could be addressed independently, but should "fall out" of improved heuristics
    for when to promote and enregister structs.
  * Related: [\#7200](https://github.com/dotnet/runtime/issues/7200)

* [\#4524 Add optimization to avoid copying a struct if passed by reference and there are no
  writes to and no reads after passed to a callee](https://github.com/dotnet/runtime/issues/4524).
  * This issue is related to #1133, except that in this case the desire is to
    eliminate unneeded copies locally (i.e. not just due to inlining), in the case where
    the struct may or may not be passed or returned directly.
  * Unfortunately, there is not currently a scenario or test case for this issue.

* [\#10879 Unix: Unnecessary struct copy while passing struct of size <=16](https://github.com/dotnet/runtime/issues/10879)
* [\#9839 [RyuJIT] Eliminate unnecessary copies when passing structs](https://github.com/dotnet/runtime/issues/9839)
  * These require changing both the callsite and the callee to avoid copying the parameter onto the stack.
  * It may be that these have been addressed by [PR #43870](https://github.com/dotnet/runtime/pull/43870).

* [\#11992](https://github.com/dotnet/runtime/issues/11992)
  * This is a case where we introduce a `GT_LCL_FLD` to retype a value that needs
    to be passed in a register. It may have been addressed by [PR #37745](https://github.com/dotnet/runtime/pull/37745)

## Other Struct-related Issues

* [\#10029](https://github.com/dotnet/runtime/issues/10029)
  * This suffers from pessimization due to poor handling of conversion (`Unsafe.As`) from `Quaternion` to `Vector4`.
    It's not immediately clear what's the best way to improve this.

* [#6858](https://github.com/dotnet/runtime/issues/6858)
  * Addressing mode expression optimization for struct fields
