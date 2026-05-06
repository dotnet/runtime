Support for multiple destination regs, GT_CALL and GT_RETURN nodes that return a value in multiple registers:
============================================================================================================

The following targets allow a GT_CALL/GT_RETURN node to return a value in more than one register:

x64 Unix:
Structs of size between 9-16 bytes will be returned in RAX/RDX and/or XMM0/XMM1.

Arm64:
HFA structs will be returned in 1-4 successive VFP registers s0-s3 or d0-d3.
Structs of size 16-bytes will be returned in two return registers.

Arm32:
Long type value is returned in two return registers r0 and r1.
HFA structs will be returned in 1-4 successive VFP registers s0-s3 or d0-d3

x86:
Long type value is returned in two return registers EDX and EAX.

Legacy backend used reg-pairs for representing long return value on 32-bit targets, which makes reg allocation and codegen complex.  Also this approach doesn't scale well to types that are returned in more than 2 registers. Arm32 HFA support in Legacy backend requires that HFA return value of a call is always stored to local in memory and with the local marked as not promotable.  Original implementation of multi-reg return of structs on x64 Unix was similar to Arm32 and further LSRA was not ware of multi-reg call nodes because of which Codegen made some assumptions (e.g. multi-reg return value of a call node is never spilled) that are not always guaranteed.

This doc proposes new IR forms and an implementation design to support multi-reg call nodes in RyuJIT that is scalable without the limitations/complexity that Legacy backend implementation had.

Post Importation IR Forms
-------------------------
In importer any call returning a (struct or long type) value in two or more registers is forced to a temp
and temp is used in place of call node subsequently in IR. Note that creation of 'tmp' is avoided if return value of call is getting assigned to a local in IL.

```
// tmp = GT_CALL, where tmp is of TYP_STRUCT or TYP_LONG
GT_ASG(TYP_STRUCT or TYP_LONG, tmp, GT_CALL node)
```

Similarly importer will force IR of GT_RETURN node returning a value in multiple return registers to be
of the following form if operand of GT_RETURN is anything but a lclVar.

```
GT_ASG(TYP_STRUCT or TYP_LONG, tmp, op1)
GT_RETURN(tmp)
```

Post struct promotion IR forms
------------------------------
Before global morph of basic blocks, struct promotion takes place. It will give rise to the following
three cases:

Case 1:
tmp is not struct promoted or Type Dependently Promoted (P-DEP).

Case 2:
tmp is Type Independently Promoted (P-INDEP) but its field layout doesn't match the layout of return registers or tmp is P-FULL promoted struct (e.g. SIMD type structs).

For example, tmp could be a struct of 4 integers.  But on x64 unix, two fields of such a struct
will be returned in one 8-byte return register.

Case 3:
tmp is P-INDEP promoted and its field layout matches the layout of return registers. That is one promoted field will get mapped to a single, un-shared register in the ABI for returning values.
An example is a struct containing two fields of `{TYP_REF, TYP_DOUBLE} `on X64 Unix.

Post Global Morph, IR forms of tmp=GT_CALL where tmp is of TYP_STRUCT
---------------------------------------------------------------------
Case 3 is morphed into

   `GT_STORE_MULTI_VAR(TYP_STRUCT, <FieldLcl1, FieldLcl2, FieldLcl3, FieldLcl4>, op1 = GT_CALL)`

   Where FieldLcl[1..4] are lcl numbers of P-INDEP promoted fields of tmp.  The limit of 2-4 locals
   is statically dependent on target platform/architecture.

   GT_STORE_MULTI_VAR is a new GenTree node to represent the store operation to 2-4 locals
   using multi-reg/mem value of a call/lclvar respectively.  It also will have additional fields
   to store the registers into which FieldLcl[1..4] need to be stored and also a spill mask
   to indicate which of the locals needs to be spilled.

   During codegen, return value of call in multiple return registers need to be stored to the
   corresponding registers of the locals by properly handling any circular dependencies.

Case 1 is morphed as
   `GT_OBJ(&tmp) = GT_CALL`

   Post rationalization this will get converted to GT_STORE_OBJ/BLK(&tmp, GT_CALL) and
   block op codegen will special case for multi-reg call case.  This case simpler because, although it is
   consuming multiple registers from the call, it doesn't have the complication of multiple
   destination registers.

Case 2 can be handled one of the following two ways
   a) Force tmp to memory and morph it as in case 1 above
   b) Create 2-4 temp locals matching the type of respective return registers of GT_CALL and
      use them to create following IR

```
GT_STORE_MULTI_VAR(TYP_STRUCT, <tmpLcl1, tmpLcl2, tmpLcl3, tmpLcl4>, op1 = GT_CALL)
```

      Example:  say on x64 unix, return type is a struct with 3 fields: TYP_INT, TYP_INT and TYP_REF.
      First two fields would be combined into a single local of TYP_LONG and third field would
      would go into a single local of TYP_REF.

      Additional IR nodes can be created to extract/assemble fields of tmp struct from individual tmpLcls.

Platform agnostic ReturnTypeDesc on a GT_CALL node
--------------------------------------------------
Every GT_CALL node will have a light-weight ReturnTypeDesc that provides a platform independent interface to query the following:
 - Respective return types of a multi-reg return value
 - Respective Return register numbers in which the value is returned.

ReturnTypeDesc is initialized during importation while creating GenTreeCall node.

GT_CALL node is augmented with the following additional state:
  gtOtherRegs - an array of MAX_RET_REG_COUNT-1 reg numbers of multi-reg return. gtRegNum field
  will always be the first return reg.
  gtSpillFlags - an array to hold GTF_SPILL/GTF_SPILLED state of each reg.  This allows us to
  spill/reload individual return registers of a multi-reg call node work.

Post Global Morph, IR forms of GT_RETURN(tmp) where tmp is of TYP_STRUCT
------------------------------------------------------------------------
Case 3 is morphed into

```
    GT_RETURN (TYP_STRUCT, op1 = GT_MULTI_VAR(TYP_STRUCT, <Fieldlcl1, FieldLcl2, FieldLcl3, FieldLcl4>))
```

    Where FieldLcl[1..4] are lcl numbers of independently promoted fields of tmp and
    GT_MULTI_VAR is a new node that represents 2-4 independent local vars.

Case 1 remains unchanged

    `GT_RETURN(TYP_STRUCT, op1 = tmp)`

Case 2 is handled as follows:
    a) Force tmp to memory and morph it as in case 1 above
    b) Create 2-4 temp locals matching the type of respective return registers of GT_RETURN and
       use them to extract individual fields from tmp and morph it as in case 3 above.

          tmpLcl1 = GenTree Nodes to extract first 8-bytes from tmp
          tmpLcl2 = GenTree Nodes to extract next 8-bytes from tmp and so on

```
         GT_RETURN(TYP_STRUCT, op1 = GT_STORE_MULTI_VAR(TYP_STRUCT, <tmpLcl1, tmpLcl2, tmpLcl3, tmpLcl4>, tmp))
```


Post Lowering, IR forms of GT_CALL node returning TYP_LONG value
----------------------------------------------------------------
During Lowering, such call nodes are lowered into tmp=GT_CALL if the return value of call node is not already assigned to a local.  Further tmp is decomposed into GT_LONG.

Post IR lowering, GT_CALL will be transformed into

```
     GT_STORE_LCL_VAR(TYP_LONG, lcl Num of tmp, op1 = GT_CALL)
```

     where tmp is decomposed into GT_LONG(GT_LCL_VAR(tmpHi), GT_LCL_VAR(tmpLo))
     and finally GT_STORE_LCL_VAR is transformed into

```
     GT_STORE_MULTI_VAR(TYP_LONG, <tmpHi, tmpLo>, op1=GT_CALL)
```

      where tmpHi and tmpLo are promoted lcl fields of tmp.

Post Lowering, IR forms of GT_RETURN(tmp) where tmp is of TYP_LONG
------------------------------------------------------------------
LclVar tmp will be decomposed into two locals and the resulting IR would be GT_RETURN(GT_LONG)

Lowering Register specification
--------------------------------
DstCount of GT_CALL node returning multi-reg return value will be set to 2 or 3 or 4 depending on the number of return registers  and its dstCandidates is set to the fixed mask of return registers.

SrcCount and DstCount of GT_STORE_MULTI_VAR will be set 2 or 3 or 4 depending on the number of locals to which a value is assigned. Note that those locals that do not require a value to be assigned are represented as BAD_VAR_NUM.

SrcCount of GT_RETURN returning a multi-reg value will be set to 2 or 3 or 4 depending on the number of return registers.

LSRA Considerations
-------------------
LSRA needs to add support for multi-reg destination GT_CALL, GT_MULTI_VAR and GT_STORE_MULTI_VAR nodes.

Codegen Considerations
----------------------
genProduceReg()/genConsumeReg() code paths need to support mullti-reg destination GT_CALL, GT_MULTI_VAR and GT_STORE_MULTI_VAR nodes.

GT_RETURN(tmp) where tmp is of TYP_STRUCT
  - tmp would either be in memory GT_LCL_VAR or GT_MULTI_VAR of TYP_STRUCT

GT_RETURN(op1) where op1 is of TYP_LONG
  - op1 should be always of the form GT_LONG(tmpLclHi, tmpLclLo)


Sub work items
--------------

The following are the sub work items and against each is indicated its current status:

1. (Done) Refactor code to abstract structDesc field of GenTreeCall node
ReturnTypeDesc would abstract away existing structDesc (x64 unix) and implement
an API and replace all uses of structDesc of call node with the API.  This would be
a pure code refactoring with the addition of ReturnTypeDesc on a GenTreeCall node.

2. (Done) Get rid of structDesc and replace it with ReturnTypeDesc.
Note that on x64 Unix, we still query structDesc from VM and use it to initialize ReturnTypeDesc.

3. (Done) Phase 1 Implementation of multi-reg GT_CALL/GT_RETURN node support for x64 Unix
  - Importer changes to force IR to be of the form tmp=call always for multi-reg call nodes
  - Importer changes to force IR to be of the form GT_RETURN(tmp) always for multi-reg return
  - tmp will always be an in memory lcl var.
  - Till First class struct support for GT_OBJ/GT_STORE_OBJ comes on-line IR will be of the form

    `GT_STORE_LCL_VAR(tmpLcl, op1 = GT_CALL)`

    where tmpLcl will always be an in memory lcl var of TYP_STRUCT
  - Lowering/LSRA/Codegen support to allocate multiple regs to GT_CALL nodes.
  - GT_CALL nodes will be governed by a single spill flag i.e. all return registers are spilled together.
  - GT_RETURN(tmp) - lowering will mark op1=tmp as contained and generate code as existing code does.

4. Phase 2 implementation of multi-reg GT_CALL/GT_RETURN node support for x64 unix
  - Add new gentree nodes GT_MULTI_VAR and GT_STORE_MULTI_VAR and plumb through rest of JIT phases.
  - Global morph code changes to support Case 3 (i.e P-DEP promoted structs)
  - Lowering/LSRA/Codegen changes to support GT_MULTI_VAR and GT_STORE_MULTI_VAR

5. When First class structs support comes on-line, leverage GT_OBJ/GT_STORE_OBJ to store multi-reg
return value of a GT_CALL node to memory cleaning up the code in GT_STORE_LCL_VAR.

6. (Done) HFA and multi-reg struct return support for Arm64

7. (Done) x86 long return support

8. (Optimization) Phase 3 implementation of multi-reg GT_CALL/GT_RETURN node support for x64 unix
  - Global morph code changes to support some of the important Case 2 efficiently

9. HFA struct and long return support for Arm32 RyuJIT - we should be able to leverage x86 long return and Arm64 HFA struct return work here.

