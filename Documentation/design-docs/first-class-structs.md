First Class Structs
===================

Objectives
----------
Primary Objectives
- Avoid forcing structs to the stack if they are only assigned to/from, or passed to/returned
  from a call or intrinsic
  - Including SIMD types as well as other pointer-sized-or-less struct types
- Enable enregistration of structs that have no field accesses
- Optimize these types as effectively as any other basic type
  - Value numbering, especially for types that are used in intrinsics (e.g. SIMD)
  - Register allocation

Secondary Objectives
* No “swizzling” or lying about struct types – they are always struct types
 - No confusing use of GT_LCL_FLD to refer to the entire struct as a different type

Struct-Related Issues in RyuJIT
-------------------------------
The following issues illustrate some of the motivation for improving the handling of value types
(structs) in RyuJIT:

* [\#11407 [RyuJIT] Fully enregister structs that fit into a single register when profitable](https://github.com/dotnet/coreclr/issues/11407), also VSO Bug 98404: .NET JIT x86 - poor code generated for value type initialization
  * This is a simple test case that should generate simply `xor eax; ret` on x86 and x64, but
    instead generates many unnecessary copies. It is addressed by full enregistration of
    structs that fit into a register (see work item 7):
 
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
 
* [\#1161  RyuJIT properly optimizes structs with a single field if the field type is int but not if it is double](https://github.com/dotnet/coreclr/issues/1161)
  * This issue arises because we never promote a struct with a single double field, due to
    the fact that such a struct may be passed or returned in a general purpose register.
    This issue could be addressed independently, but should "fall out" of improved heuristics
    for when to promote and enregister structs.
  
* [\#1636 Add optimization to avoid copying a struct if passed by reference and there are no
  writes to and no reads after passed to a callee](https://github.com/dotnet/coreclr/issues/1636).
  * This issue is related to #1133, except that in this case the desire is to
    eliminate unneeded copies locally (i.e. not just due to inlining), in the case where
    the struct may or may not be passed or returned directly.
  * Unfortunately, there is not currently a scenario or test case for this issue.

* [\#2908 Unix: Unecessary struct copy while passing by value on stack](https://github.com/dotnet/coreclr/issues/2908)
* [\#6264 Unix: Unnecessary struct copy while passsing struct of size <=16](https://github.com/dotnet/coreclr/issues/6264)
* [\#6266 Unix: Unnecessary copies for promoted struct arguments](https://github.com/dotnet/coreclr/issues/6266)
* [\#16619 [RyuJIT] Eliminate unecessary copies when passing structs](https://github.com/dotnet/coreclr/issues/16619)
  * These require changing both the callsite and the callee to avoid copying the parameter onto the stack.
  * Should be addressed with work items 5 and 10. SIMD parameters are a special case (see #6265), but should be addressed at the
    same time.

* [\#3144 Avoid marking tmp as DoNotEnregister in tmp=GT_CALL() where call returns a
  enregisterable struct in two return registers](https://github.com/dotnet/coreclr/issues/3144)
  * This issue could be addressed without First Class Structs. However,
    it will be easier with struct assignments that are normalized as regular assignments, and
    should be done along with the streamlining of the handling of ABI-specific struct passing
    and return values.
    
* [\#3539 RyuJIT: Poor code quality for tight generic loop with many inlineable calls](https://github.com/dotnet/coreclr/issues/3539)
(factor x8 slower than non-generic few calls loop).
  * This appears to be the same fundamental issue as #1133. Since there is a benchmark associated with this, it should be
    verified when #1133 is addressed.

* [\#4766 Pi-Digits: Extra Struct copies of BigInteger](https://github.com/dotnet/coreclr/issues/4766)
  * In addition to suffering from the same issue as #1133, this has a struct that is promoted even though it is
    passed (by reference) to its non-inlined constructor. This means that any copy to/from this struct will be field-by-field.

* [\#5556 RuyJIT: structs in parameters and enregistering](https://github.com/dotnet/coreclr/issues/5556)
  * The test case in this issue generates effectively the same loop body on most targets except for x86.
    Addressing this requires us to "Add support in prolog to extract fields, and
    remove the restriction of not promoting incoming reg structs that have more than one field" - see [Dependent Work Items](https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/first-class-structs.md#dependent-work-items)
    
* [\#6265 Unix: Unnecessary copies for struct argument with GT_ADDR(GT_SIMD)](https://github.com/dotnet/coreclr/issues/6265)
  * The IR for passing structs doesn't support having a `GT_SIMD` node as an argument. This should be fixed by work item 10 below.

* [\#11816 Extra zeroing with structs and inlining](https://github.com/dotnet/coreclr/issues/11816)
  * This issue illustrates the failure of the JIT to eliminate zero-initialization of structs that are subsequently fully
    defined. It is a related but somewhat different manifestation of the issue in #1133, i.e. that structs are not
    fully supported in value numbering and optimization.

* [\#12865 JIT: inefficient codegen for calls returning 16-byte structs on Linux x64](https://github.com/dotnet/coreclr/issues/12865)
  * This is related to #3144, and requires supporting the assignment of a multi-reg call return into a promoted local variable,
    and enabling subsequent elimination of any redundant copies.
  
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
This is generally done only during the importer (translation from MSIL to the RyuJIT IR), and
during struct promotion analysis. As a result, struct types are treated as an opaque type
(TYP_STRUCT) of unknown size and structure.

In order to treat fully-enregisterable struct types as "first class" types in RyuJIT, we
 create new types with fixed size and structure:
* TYP_SIMD8, TYP_SIMD12, TYP_SIMD16 and (where supported by the target) TYP_SIMD32
 - These types already exist, and represent some already-completed steps toward First Class Structs.
* TYP_STRUCT1, TYP_STRUCT2, TYP_STRUCT4, TYP_STRUCT8 (on 64-bit systems)
 - These types are new, and will be used where struct types of the given size are passed and/or
 returned in registers.

We want to identify and normalize these types early in the compiler, before any decisions are
made regarding whether they are constrained to live on the stack and whether and how they are
promoted (scalar replaced) or copied.

One issue that arises is that it becomes necessary to know the size of any struct type that
we encounter, even if we may not actually need to know the size in order to generate code.
The major cause of additional queries seems to be for field references. It is possible to
defer some of these cases. I don't know what the throughput impact will be to always do the
normalization, but in principle I think it is worth doing because the alternative would be
to transform the types later (e.g. during morph) and use a contextual tree walk to see if we
care about the size of the struct. That would likely be a messier analysis.

Current Struct IR Phase Transitions
-----------------------------------

There are three phases in the JIT that make changes to the representation of struct tree
nodes and lclVars:

* Importer
 * All struct type lclVars have TYP_STRUCT
 * All struct assignments/inits are block ops
 * All struct call args are ldobj
 * Other struct nodes have TYP_STRUCT
* Struct promotion
 * Fields of promoted structs become separate lclVars (scalar promoted) with primitive types
* Global morph
 * All struct nodes are transformed to block ops
   - Besides call args
  * Some promoted structs are forced to stack
   - Become “dependently promoted”
 * Call args 
   - Morphed to GT_LCL_FLD if passed in a register
   - Treated in various ways otherwise (inconsistent)

Proposed Approach
-----------------
The most fundamental change with first class structs is that struct assignments become
just a special case of assignment. The existing block ops (GT_INITBLK, GT_COPYBLK,
 GT_COPYOBJ, GT_LDOBJ) are eliminated. Instead, the block operations in the incoming MSIL
 are translated into assignments to or from a new GT_OBJ node.

New fixed-size struct types are added: (TYP_STRUCT[1|2|4|8]), which are somewhat similar
to the (existing) SIMD types (TYP_SIMD[8|16|32]). As is currently done for the SIMD types,
these types are normalized in the importer.

Conceptually, struct nodes refer to the object, not the address. This is important, as
the existing block operations all take address operands, meaning that any lclVar involved
in an assignment (including initialization) will be in an address-taken context in the JIT,
requiring special analysis to identify the cases where the address is only taken in order
to assign to or from the lclVar. This further allows for consistency in the treatment of
structs and simple types - even potentially enabling optimizations of non-enregisterable
structs.

### Struct promotion

* Struct promotion analysis
 * Aggressively promote pointer-sized fields of structs used as args or returns
 * Consider FULL promotion of pointer-size structs
   * If there are fewer field references than calls or returns

### Assignments
* Struct assignments look like any other assignment
* GenTreeAsg (GT_ASG) extends GenTreeOp with:

```C#
// True if this assignment is a volatile memory operation.
bool IsVolatile() const { return (gtFlags & GTF_BLK_VOLATILE) != 0; }
bool gtAsgGcUnsafe;

// What code sequence we will be using to encode this operation.
enum
{
    AsgKindInvalid,
    AsgKindDirect,
    AsgKindHelper,
    AsgKindRepInstr,
    AsgKindUnroll,
} gtAsgKind;
```

### Struct “objects” as lvalues
* Lhs of a struct assignment is a block node or lclVar
* Block nodes represent the address and “shape” info formerly on the block copy:
 * GT_BLK and GT_STORE_BLK (GenTreeBlk)
   * Has a (non-tree node) size field
   * Addr() is op1
   * Data() is op2
 * GT_OBJ and GT_STORE_OBJ (GenTreeObj extends GenTreeBlk)
   * gtClass, gtGcPtrs, gtGcPtrCount, gtSlots
 * GT_DYN_BLK and GT_STORE_DYN_BLK (GenTreeDynBlk extends GenTreeBlk)
   * Additional child gtDynamicSize

### Struct “objects” as rvalues
After morph, structs on rhs of assignment are either:
* The tree node for the object: e.g. call, retExpr
* GT_IND of an address (e.g. GT_LEA)

The lhs provides the “shape” for the assignment. Note: it has been suggested that these could 
remain as GT_BLK nodes, but I have not given that any deep consideration.

### Preserving Struct Types in Trees

Prior to morphing, all nodes that may represent a struct type will have a class handle.
After morphing, some will become GT_IND.

### Structs As Call Arguments

All struct args imported as GT_OBJ, transformed as follows during morph:
* P_FULL promoted locals:
  * Remain as a GT_LCL_VAR nodes, with the appropriate fixed-size struct type.
  * Note that these may or may not be passed in registers.
* P_INDEP promoted locals:
  * These are the ones where the fields don’t match the reg types
    GT_STRUCT (or something) for aggregating multiple fields into a single register
  * Op1 is a lclVar for the first promoted field
  * Op2 is the lclVar for the next field, OR another GT_STRUCT
  * Bit offset for the second child
* All other cases (non-locals, OR P_DEP or non-promoted locals):
  * GT_LIST of GT_IND for each half

### Struct Return

The return of a struct value from the current method is represented as follows:
* GT_RET(GT_OBJ) initially
* GT_OBJ morphed, and then transformed similarly to call args

Proposed Struct IR Phase Transitions
------------------------------------

* Importer
  * Struct assignments are imported as GT_ASG
  * Struct type is normalized to TYP_STRUCT* or TYP_SIMD*
* Struct promotion
  * Fields of promoted structs become separate lclVars (as is)
  * Enregisterable structs (including Pair Types) may be promoted to P_FULL (i.e. fully enregistered)
  * As a future optimization, we may "restructure" multi-register argument or return values as a
    synthesized struct of appropriately typed fields, and then promoted in the normal manner.
* Global morph
  * All struct type local variables remain as simple GT_LCL_VAR nodes.
  * All other struct nodes are transformed to GT_IND (rhs of assignment) or remain as GT_OBJ
    * In Lowering, GT_OBJ will be changed to GT_BLK if there are no gc ptrs. This could be done
      earlier, but there are some places where the object pointer is desired.
    * It is not actually clear if there is a great deal of value in the GT_BLK, but it was added
      to be more compatible with existing code that expects block copies with gc pointers to be
      distinguished from those that do not.
  * Promoted structs are forced to stack ONLY if address taken
  * Call arguments
    * Fixed-size enregisterable structs: GT_LCL_VAR or GT_OBJ of appropriate type.
    * Multi-register arguments: GT_LIST of register-sized operands:
      * GT_LCL_VAR if there is a promoted field that exactly matches the register size and type
        (note that, if we have performed the optimization mentioned above in struct promotion,
        we may have a GT_LCL_VAR of a synthesized struct field).
      * GT_LCL_FLD if there is a matching field in the struct that has not been promoted.
      * GT_IND otherwise. Note that if this is a struct local that does not have a matching field,
        this will force the local to live on the stack.
* Lowering
  * Pair types (e.g. TYP_LONG on 32-bit targets) are decomposed as needed to expose register requirements.
    Although these are not strictly structs, their handling is similar.
    * Computations are decomposed into their constituent parts when they independently write
      separate registers.
    * TYP_LONG lclVars (and TYP_DOUBLE on ARM) are split (similar to promotion/scalar replacement of
      structs) if and only if they are register candidates.
    * Other TYP_LONG/TYP_DOUBLE lclVars are loaded into independent registers either via:
      * Single GT_LCL_VAR that will translate into a pair load instruction (ldp), with two register 
        targets, or
      * GT_LCL_FLD (current approach) or GT_IND (probaby a better approach)
  * Calls and loads that target multiple registers
    * Existing gtLsraInfo has the capability to specify multiple destination registers
    * Additional work is required in LSRA to handle these correctly
    * If HFAs can be return values (not just call args), then we may need to support up to 4 destination
      registers for LSRA

Sample IR
---------
### Bug 98404
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

Work Items
----------
This is a preliminary breakdown of the work into somewhat separable tasks. Those whose descriptions
are prefaced by '*' have been prototyped in an earlier version of the JIT, and that work is now
being re-integrated and tested, but may require some cleanup and/or phasing with other work items
before a PR is submitted.

### Mostly-Independent work items
1.	*Replace block ops with assignments & new nodes.

2.	*Add new fixed-size types, and normalize them in the importer (might be best to do this with or after #1, but not really dependent)

3.	LSRA
    * Enable support for multiple destination regs, call nodes that return a struct in multiple
      registers (for x64/ux, and for arm)
    * Handle multiple destination regs for ldp on arm64 (could be done before or concurrently with the above).
      Note that this work item is specifically intended for call arguments. It is likely the case that
      utilizing ldp for general-purpose code sequences would be handled separately.

4.	X64/ux: aggressively promote lclVar struct incoming or outgoing args with two 8-byte fields

5.	X64/ux:
    * modify the handling of multireg struct args to use GT_LIST of GT_IND
    * remove the restriction to NOT promote things that are multi-reg args, as long as they match (i.e. two 8-byte fields).
      Pass those using GT_LIST of GT_LCL_VAR.
    * stop adding extra lclVar copies

6.	Arm64:
    * Promote 16-byte struct lclVars that are incoming or outgoing register arguments only if they have 2 8-byte fields (DONE).
      Pass those using GT_LIST of GT_LCL_VAR (as above for x64/ux).
      Note that, if the types do not match, e.g. a TYP_DOUBLE field that will be passed in an integer register,
      it will require special handling in Lowering and LSRA, as is currently done in the TYP_SIMD8 case.
    * For other cases, pass as GT_LIST of GT_IND (DONE)
    * The GT_LIST would be created in fgMorphArgs(). Then in Lower, putarg_reg nodes will be inserted between
      the GT_LIST and the list item (GT_LCL_VAR or GT_IND). (DONE)
    * Add support for HFAs.
    
    ### Dependent work items:
    
7.	*(Depends on 1 & 2): Fully enregister TYP_STRUCT[1|2|3|4|8] with no field accesses.

8.  *(Depends on 1 & 2): Enable value numbering and assertion propagation for struct types.

9.	(Depends on 1 & 2, mostly to avoid conflicts): Add support in prolog to extract fields, and
    remove the restriction of not promoting incoming reg structs that have more than one field.
    Note that SIMD types are already reassembled in the prolog.
    
10.	(Not really dependent, but probably best done after 1, 2, 5, 6): Add support for assembling
    non-matching fields into registers for call args and returns. This includes producing the
    appropriate IR, which may be simply be shifts and or's of the appropriate fields.
    This would either be done during `fgMorphArgs()` and the `GT_RETURN` case of `fgMorphSmpOp()`
    or as described below in
    [Extracting and Assembling Structs](#Extract-Assemble).
    
11. (Not really dependent, but probably best done after 1, 2, 5, 6): Add support for extracting the fields for the
    returned struct value of a call, producing the appropriate IR, which may simply be shifts and
    and's.
    This would either be done during the morphing of the call itself, or as described below in
    [Extracting and Assembling Structs](#Extract-Assemble).

12.	(Depends on 3, may replace the second part of 6): For arm64, add support for loading non-promoted
    or non-local structs with ldp
    * Either using TYP_STRUCT and special case handling, OR adding TYP_STRUCT16

13.	(Depends on 7, 9, 10, 11): Enhance struct promotion to allow full enregistration of structs,
    even if some field are accessed, if there are more call/return references than field references.
    This work item should address issue #1161, by removing the automatic non-promotion
    of structs with a single double field, and adding appropriate heuristics for when it
    should be allowed.

Related Work Item
-----------------
These changes are somewhat orthogonal, though will likely have merge issues if done in parallel with any of
the above:
* Unified API for ABI info
  * Pass/Return info:
    * Num regs used for passing
    * Per-slot location (reg num / REG_STK)
    * Per-slot type (for reg “slots”)
    * Starting stack slot offset (if passed on stack)
    * By reference?
    * etc.
  * We should be able to unify HFA handling into this model
  * For arg passing, the API for creating the argEntry should take an arg state that keeps track of
    what regs have been used, and handles the backfilling case for ARM
    
Open Design Questions
---------------------
### <a name="Extract-Assemble"/>Extracting and Assembling Structs

Should the IR for extracting and assembling struct arguments from or to argument or return registers
be generated directly during the morphing of call arguments and returns, or should this capability
be handled in a more general fashion in `fgMorphCopyBlock()`?
The latter seems desirable for its general applicability.

One way to handle this might be:

1. Whenever you have a case of mismatched structs (call args, call node, or return node),
   create a promoted temp of the "fake struct type", e.g. for arm you would introduce three
   new temps for the struct, and for each of its TYP_LONG promoted fields.
2. Add an assignment to or from the temp (e.g. as a setup arg node), BUT the structs on
   both sides of that assignment can now be promoted.
3. Add code to fgMorphCopyBlock to handle the extraction and assembling of structs.
4. The promoted fields of the temp would be preferenced to the appropriate argument or return registers.
