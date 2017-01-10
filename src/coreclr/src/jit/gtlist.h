// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// clang-format off
/*****************************************************************************/
#ifndef GTNODE
#error  Define GTNODE before including this file.
#endif
/*****************************************************************************/
//
//     Node enum
//                      ,"Node name"
//                                       ,GenTree struct flavor
//                                                           ,commutative
//                                                             ,operKind

GTNODE(NONE             , "<none>"       ,char               ,0,GTK_SPECIAL)

//-----------------------------------------------------------------------------
//  Leaf nodes (i.e. these nodes have no sub-operands):
//-----------------------------------------------------------------------------

GTNODE(LCL_VAR          , "lclVar"       ,GenTreeLclVar      ,0,GTK_LEAF|GTK_LOCAL)     // local variable
GTNODE(LCL_FLD          , "lclFld"       ,GenTreeLclFld      ,0,GTK_LEAF|GTK_LOCAL)     // field in a non-primitive variable
GTNODE(LCL_VAR_ADDR     , "&lclVar"      ,GenTreeLclVar      ,0,GTK_LEAF)               // address of local variable
GTNODE(LCL_FLD_ADDR     , "&lclFld"      ,GenTreeLclFld      ,0,GTK_LEAF)               // address of field in a non-primitive variable
GTNODE(STORE_LCL_VAR    , "st.lclVar"    ,GenTreeLclVar      ,0,GTK_UNOP|GTK_LOCAL|GTK_NOVALUE) // store to local variable
GTNODE(STORE_LCL_FLD    , "st.lclFld"    ,GenTreeLclFld      ,0,GTK_UNOP|GTK_LOCAL|GTK_NOVALUE) // store to field in a non-primitive variable
GTNODE(CATCH_ARG        , "catchArg"     ,GenTree            ,0,GTK_LEAF)               // Exception object in a catch block
GTNODE(LABEL            , "codeLabel"    ,GenTreeLabel       ,0,GTK_LEAF)               // Jump-target
GTNODE(FTN_ADDR         , "ftnAddr"      ,GenTreeFptrVal     ,0,GTK_LEAF)               // Address of a function
GTNODE(RET_EXPR         , "retExpr"      ,GenTreeRetExpr     ,0,GTK_LEAF)               // Place holder for the return expression from an inline candidate

//-----------------------------------------------------------------------------
//  Constant nodes:
//-----------------------------------------------------------------------------

GTNODE(CNS_INT          , "const"        ,GenTreeIntCon      ,0,GTK_LEAF|GTK_CONST)
GTNODE(CNS_LNG          , "lconst"       ,GenTreeLngCon      ,0,GTK_LEAF|GTK_CONST)
GTNODE(CNS_DBL          , "dconst"       ,GenTreeDblCon      ,0,GTK_LEAF|GTK_CONST)
GTNODE(CNS_STR          , "sconst"       ,GenTreeStrCon      ,0,GTK_LEAF|GTK_CONST)

//-----------------------------------------------------------------------------
//  Unary  operators (1 operand):
//-----------------------------------------------------------------------------

GTNODE(NOT              , "~"            ,GenTreeOp          ,0,GTK_UNOP)
GTNODE(NOP              , "nop"          ,GenTree            ,0,GTK_UNOP)
GTNODE(NEG              , "unary -"      ,GenTreeOp          ,0,GTK_UNOP)
GTNODE(COPY             , "copy"         ,GenTreeCopyOrReload,0,GTK_UNOP)               // Copies a variable from its current location to a register that satisfies
                                                                                        // code generation constraints.  The child is the actual lclVar node.
GTNODE(RELOAD           , "reload"       ,GenTreeCopyOrReload,0,GTK_UNOP)
GTNODE(CHS              , "flipsign"     ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR) // GT_CHS is actually unary -- op2 is ignored.
                                                                                        // Changing to unary presently causes problems, though -- take a little work to fix.

GTNODE(ARR_LENGTH       , "arrLen"       ,GenTreeArrLen      ,0,GTK_UNOP|GTK_EXOP)      // array-length

GTNODE(INTRINSIC        , "intrinsic"    ,GenTreeIntrinsic   ,0,GTK_BINOP|GTK_EXOP)     // intrinsics

GTNODE(LOCKADD          , "lockAdd"      ,GenTreeOp          ,0,GTK_BINOP|GTK_NOVALUE)
GTNODE(XADD             , "XAdd"         ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(XCHG             , "Xchg"         ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(CMPXCHG          , "cmpxchg"      ,GenTreeCmpXchg     ,0,GTK_SPECIAL)
GTNODE(MEMORYBARRIER    , "memoryBarrier",GenTree            ,0,GTK_LEAF|GTK_NOVALUE)

GTNODE(CAST             , "cast"         ,GenTreeCast        ,0,GTK_UNOP|GTK_EXOP)      // conversion to another type
GTNODE(CKFINITE         , "ckfinite"     ,GenTreeOp          ,0,GTK_UNOP)               // Check for NaN
GTNODE(LCLHEAP          , "lclHeap"      ,GenTreeOp          ,0,GTK_UNOP)               // alloca()
GTNODE(JMP              , "jump"         ,GenTreeVal         ,0,GTK_LEAF|GTK_NOVALUE)   // Jump to another function

GTNODE(ADDR             , "addr"         ,GenTreeOp          ,0,GTK_UNOP)               // address of
GTNODE(IND              , "indir"        ,GenTreeOp          ,0,GTK_UNOP)               // load indirection
GTNODE(STOREIND         , "storeIndir"   ,GenTreeStoreInd    ,0,GTK_BINOP|GTK_NOVALUE)  // store indirection

                                                                                        // TODO-Cleanup: GT_ARR_BOUNDS_CHECK should be made a GTK_BINOP now that it has only two child nodes
GTNODE(ARR_BOUNDS_CHECK , "arrBndsChk"   ,GenTreeBoundsChk   ,0,GTK_SPECIAL|GTK_NOVALUE)// array bounds check
GTNODE(OBJ              , "obj"          ,GenTreeObj         ,0,GTK_UNOP|GTK_EXOP)      // Object that MAY have gc pointers, and thus includes the relevant gc layout info.
GTNODE(STORE_OBJ        , "storeObj"     ,GenTreeBlk         ,0,GTK_BINOP|GTK_EXOP|GTK_NOVALUE) // Object that MAY have gc pointers, and thus includes the relevant gc layout info.
GTNODE(BLK              , "blk"          ,GenTreeBlk         ,0,GTK_UNOP)               // Block/object with no gc pointers, and with a known size (e.g. a struct with no gc fields)
GTNODE(STORE_BLK        , "storeBlk"     ,GenTreeBlk         ,0,GTK_BINOP|GTK_NOVALUE)  // Block/object with no gc pointers, and with a known size (e.g. a struct with no gc fields)
GTNODE(DYN_BLK          , "DynBlk"       ,GenTreeBlk         ,0,GTK_SPECIAL)            // Dynamically sized block object
GTNODE(STORE_DYN_BLK    , "storeDynBlk"  ,GenTreeBlk         ,0,GTK_SPECIAL|GTK_NOVALUE)// Dynamically sized block object
GTNODE(BOX              , "box"          ,GenTreeBox         ,0,GTK_UNOP|GTK_EXOP|GTK_NOTLIR)

#ifdef FEATURE_SIMD
GTNODE(SIMD_CHK         , "simdChk"      ,GenTreeBoundsChk   ,0,GTK_SPECIAL|GTK_NOVALUE)// Compare whether an index is less than the given SIMD vector length, and call CORINFO_HELP_RNGCHKFAIL if not.
                                                                                        // TODO-CQ: In future may want to add a field that specifies different exceptions but we'll
                                                                                        // need VM assistance for that.
                                                                                        // TODO-CQ: It would actually be very nice to make this an unconditional throw, and expose the control flow that
                                                                                        // does the compare, so that it can be more easily optimized.  But that involves generating qmarks at import time...
#endif // FEATURE_SIMD

GTNODE(ALLOCOBJ         , "allocObj"     ,GenTreeAllocObj    ,0,GTK_UNOP|GTK_EXOP)      // object allocator

GTNODE(INIT_VAL         , "initVal"      ,GenTreeOp          ,0,GTK_UNOP)               // Initialization value for an initBlk

//-----------------------------------------------------------------------------
//  Binary operators (2 operands):
//-----------------------------------------------------------------------------

GTNODE(ADD              , "+"            ,GenTreeOp          ,1,GTK_BINOP)
GTNODE(SUB              , "-"            ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(MUL              , "*"            ,GenTreeOp          ,1,GTK_BINOP)
GTNODE(DIV              , "/"            ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(MOD              , "%"            ,GenTreeOp          ,0,GTK_BINOP)

GTNODE(UDIV             , "un-/"         ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(UMOD             , "un-%"         ,GenTreeOp          ,0,GTK_BINOP)

GTNODE(OR               , "|"            ,GenTreeOp          ,1,GTK_BINOP|GTK_LOGOP)
GTNODE(XOR              , "^"            ,GenTreeOp          ,1,GTK_BINOP|GTK_LOGOP)
GTNODE(AND              , "&"            ,GenTreeOp          ,1,GTK_BINOP|GTK_LOGOP)

GTNODE(LSH              , "<<"           ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(RSH              , ">>"           ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(RSZ              , ">>>"          ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(ROL              , "rol"          ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(ROR              , "ror"          ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(MULHI            , "mulhi"        ,GenTreeOp          ,1,GTK_BINOP) // returns high bits (top N bits of the 2N bit result of an NxN multiply)
                                                     // GT_MULHI is used in division by a constant (fgMorphDivByConst). We turn
                                                     // the div into a MULHI + some adjustments. In codegen, we only use the
                                                     // results of the high register, and we drop the low results.

GTNODE(ASG              , "="            ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_ADD          , "+="           ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_SUB          , "-="           ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_MUL          , "*="           ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_DIV          , "/="           ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_MOD          , "%="           ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)

GTNODE(ASG_UDIV         , "/="           ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_UMOD         , "%="           ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)

GTNODE(ASG_OR           , "|="           ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_XOR          , "^="           ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_AND          , "&="           ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_LSH          , "<<="          ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_RSH          , ">>="          ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_RSZ          , ">>>="         ,GenTreeOp          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)

GTNODE(EQ               , "=="           ,GenTreeOp          ,0,GTK_BINOP|GTK_RELOP)
GTNODE(NE               , "!="           ,GenTreeOp          ,0,GTK_BINOP|GTK_RELOP)
GTNODE(LT               , "<"            ,GenTreeOp          ,0,GTK_BINOP|GTK_RELOP)
GTNODE(LE               , "<="           ,GenTreeOp          ,0,GTK_BINOP|GTK_RELOP)
GTNODE(GE               , ">="           ,GenTreeOp          ,0,GTK_BINOP|GTK_RELOP)
GTNODE(GT               , ">"            ,GenTreeOp          ,0,GTK_BINOP|GTK_RELOP)

GTNODE(COMMA            , "comma"        ,GenTreeOp          ,0,GTK_BINOP|GTK_NOTLIR)

GTNODE(QMARK            , "qmark"        ,GenTreeQmark       ,0,GTK_BINOP|GTK_EXOP|GTK_NOTLIR)
GTNODE(COLON            , "colon"        ,GenTreeColon       ,0,GTK_BINOP|GTK_NOTLIR)

GTNODE(INDEX            , "[]"           ,GenTreeIndex       ,0,GTK_BINOP|GTK_EXOP|GTK_NOTLIR)   // SZ-array-element

GTNODE(MKREFANY         , "mkrefany"     ,GenTreeOp          ,0,GTK_BINOP)

GTNODE(LEA              , "lea"          ,GenTreeAddrMode    ,0,GTK_BINOP|GTK_EXOP)

#if !defined(LEGACY_BACKEND) && !defined(_TARGET_64BIT_)
// A GT_LONG node simply represents the long value produced by the concatenation
// of its two (lower and upper half) operands.  Some GT_LONG nodes are transient,
// during the decomposing of longs; others are handled by codegen as operands of
// nodes such as calls, returns and stores of long lclVars.
GTNODE(LONG             , "gt_long"      ,GenTreeOp          ,0,GTK_BINOP)

// The following are nodes representing x86 specific long operators, including
// high operators of a 64-bit operations that requires a carry/borrow, which are
// named GT_XXX_HI for consistency, low operators of 64-bit operations that need
// to not be modified in phases post-decompose, and operators that return 64-bit
// results in one instruction.
GTNODE(ADD_LO           , "+Lo"          ,GenTreeOp          ,1,GTK_BINOP)
GTNODE(ADD_HI           , "+Hi"          ,GenTreeOp          ,1,GTK_BINOP)
GTNODE(SUB_LO           , "-Lo"          ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(SUB_HI           , "-Hi"          ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(DIV_HI           , "/Hi"          ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(MOD_HI           , "%Hi"          ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(MUL_LONG         , "*long"        ,GenTreeOp          ,1,GTK_BINOP) // A mul that returns the 2N bit result of an NxN multiply. This op
                                                                           // is used for x86 multiplies that take two ints and return a long
                                                                           // result. All other multiplies with long results are morphed into
                                                                           // helper calls. It is similar to GT_MULHI, the difference being that
                                                                           // GT_MULHI drops the lo part of the result, whereas GT_MUL_LONG keeps
                                                                           // both parts of the result.

// The following are nodes that specify shifts that take a GT_LONG op1. The GT_LONG
// contains the hi and lo parts of three operand shift form where one op will be
// shifted into the other op as part of the operation (LSH_HI will shift
// the high bits of the lo operand into the high operand as it shifts left. RSH_LO
// will shift the lo bits of the high operand into the lo operand). LSH_HI
// represents the high operation of a 64-bit left shift by a constant int, and
// RSH_LO represents the lo operation of a 64-bit right shift by a constant int.
GTNODE(LSH_HI           , "<<Hi"         ,GenTreeOp          ,0,GTK_BINOP)
GTNODE(RSH_LO           , ">>Lo"         ,GenTreeOp          ,0,GTK_BINOP)
#endif // !defined(LEGACY_BACKEND) && !defined(_TARGET_64BIT_)

#ifdef FEATURE_SIMD
GTNODE(SIMD             , "simd"         ,GenTreeSIMD        ,0,GTK_BINOP|GTK_EXOP)     // SIMD functions/operators/intrinsics
#endif // FEATURE_SIMD

//-----------------------------------------------------------------------------
//  Other nodes that look like unary/binary operators:
//-----------------------------------------------------------------------------

GTNODE(JTRUE            , "jmpTrue"      ,GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)
GTNODE(JCC              , "jcc"          ,GenTreeJumpCC      ,0,GTK_LEAF|GTK_NOVALUE)

GTNODE(LIST             , "<list>"       ,GenTreeArgList     ,0,GTK_BINOP|GTK_NOVALUE)
GTNODE(FIELD_LIST       , "<fldList>"    ,GenTreeFieldList   ,0,GTK_BINOP) // List of fields of a struct, when passed as an argument

//-----------------------------------------------------------------------------
//  Other nodes that have special structure:
//-----------------------------------------------------------------------------

GTNODE(FIELD            , "field"        ,GenTreeField       ,0,GTK_SPECIAL)            // Member-field
GTNODE(ARR_ELEM         , "arrMD&"       ,GenTreeArrElem     ,0,GTK_SPECIAL)            // Multi-dimensional array-element address
GTNODE(ARR_INDEX        , "arrMDIdx"     ,GenTreeArrIndex    ,0,GTK_BINOP|GTK_EXOP)     // Effective, bounds-checked index for one dimension of a multi-dimensional array element
GTNODE(ARR_OFFSET       , "arrMDOffs"    ,GenTreeArrOffs     ,0,GTK_SPECIAL)            // Flattened offset of multi-dimensional array element
GTNODE(CALL             , "call()"       ,GenTreeCall        ,0,GTK_SPECIAL)

//-----------------------------------------------------------------------------
//  Statement operator nodes:
//-----------------------------------------------------------------------------

GTNODE(BEG_STMTS        , "begStmts"     ,GenTree            ,0,GTK_SPECIAL|GTK_NOVALUE)// used only temporarily in importer by impBegin/EndTreeList()
GTNODE(STMT             , "stmtExpr"     ,GenTreeStmt        ,0,GTK_SPECIAL|GTK_NOVALUE)// top-level list nodes in bbTreeList

GTNODE(RETURN           , "return"       ,GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)   // return from current function
GTNODE(SWITCH           , "switch"       ,GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)   // switch

GTNODE(NO_OP            , "no_op"        ,GenTree            ,0,GTK_LEAF|GTK_NOVALUE)   // nop!

GTNODE(START_NONGC      , "start_nongc"  ,GenTree            ,0,GTK_LEAF|GTK_NOVALUE)   // starts a new instruction group that will be non-gc interruptible

GTNODE(PROF_HOOK        , "prof_hook"    ,GenTree            ,0,GTK_LEAF|GTK_NOVALUE)   // profiler Enter/Leave/TailCall hook

GTNODE(RETFILT          , "retfilt"      ,GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)   // end filter with TYP_I_IMPL return value
#if !FEATURE_EH_FUNCLETS
GTNODE(END_LFIN         , "endLFin"      ,GenTreeVal         ,0,GTK_LEAF|GTK_NOVALUE)   // end locally-invoked finally
#endif // !FEATURE_EH_FUNCLETS

//-----------------------------------------------------------------------------
//  Nodes used for optimizations.
//-----------------------------------------------------------------------------

GTNODE(PHI              , "phi"          ,GenTreeOp          ,0,GTK_UNOP)               // phi node for ssa.
GTNODE(PHI_ARG          , "phiArg"       ,GenTreePhiArg      ,0,GTK_LEAF|GTK_LOCAL)     // phi(phiarg, phiarg, phiarg)

//-----------------------------------------------------------------------------
//  Nodes used by Lower to generate a closer CPU representation of other nodes
//-----------------------------------------------------------------------------

#ifndef LEGACY_BACKEND
GTNODE(JMPTABLE         , "jumpTable"    ,GenTreeJumpTable   ,0, GTK_LEAF)              // Generates the jump table for switches
#endif
GTNODE(SWITCH_TABLE     , "tableSwitch"  ,GenTreeOp          ,0, GTK_BINOP|GTK_NOVALUE) // Jump Table based switch construct

//-----------------------------------------------------------------------------
//  Nodes used only within the code generator:
//-----------------------------------------------------------------------------

GTNODE(REG_VAR          , "regVar"       ,GenTreeLclVar      ,0,GTK_LEAF|GTK_LOCAL)     // register variable
GTNODE(CLS_VAR          , "clsVar"       ,GenTreeClsVar      ,0,GTK_LEAF)               // static data member
GTNODE(CLS_VAR_ADDR     , "&clsVar"      ,GenTreeClsVar      ,0,GTK_LEAF)               // static data member address
GTNODE(ARGPLACE         , "argPlace"     ,GenTreeArgPlace    ,0,GTK_LEAF)               // placeholder for a register arg
GTNODE(NULLCHECK        , "nullcheck"    ,GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)   // null checks the source
GTNODE(PHYSREG          , "physregSrc"   ,GenTreePhysReg     ,0,GTK_LEAF)               // read from a physical register
GTNODE(PHYSREGDST       , "physregDst"   ,GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)   // write to a physical register
GTNODE(EMITNOP          , "emitnop"      ,GenTree            ,0,GTK_LEAF|GTK_NOVALUE)   // emitter-placed nop
GTNODE(PINVOKE_PROLOG   ,"pinvoke_prolog",GenTree            ,0,GTK_LEAF|GTK_NOVALUE)   // pinvoke prolog seq
GTNODE(PINVOKE_EPILOG   ,"pinvoke_epilog",GenTree            ,0,GTK_LEAF|GTK_NOVALUE)   // pinvoke epilog seq
GTNODE(PUTARG_REG       , "putarg_reg"   ,GenTreeOp          ,0,GTK_UNOP)               // operator that places outgoing arg in register
GTNODE(PUTARG_STK       , "putarg_stk"   ,GenTreePutArgStk   ,0,GTK_UNOP|GTK_NOVALUE)   // operator that places outgoing arg in stack
GTNODE(RETURNTRAP       , "returnTrap"   ,GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)   // a conditional call to wait on gc
GTNODE(SWAP             , "swap"         ,GenTreeOp          ,0,GTK_BINOP|GTK_NOVALUE)  // op1 and op2 swap (registers)
GTNODE(IL_OFFSET        , "il_offset"    ,GenTreeStmt        ,0,GTK_LEAF|GTK_NOVALUE)   // marks an IL offset for debugging purposes

/*****************************************************************************/
#undef  GTNODE
/*****************************************************************************/
// clang-format on
