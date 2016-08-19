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
//    Node enum
//                   , "Node name"
//                                  ,commutative
//                                    ,operKind

GTNODE(NONE       , "<none>"     ,0,GTK_SPECIAL)

//-----------------------------------------------------------------------------
//  Leaf nodes (i.e. these nodes have no sub-operands):
//-----------------------------------------------------------------------------

GTNODE(LCL_VAR       , "lclVar"     ,0,GTK_LEAF|GTK_LOCAL)             // local variable
GTNODE(LCL_FLD       , "lclFld"     ,0,GTK_LEAF|GTK_LOCAL)             // field in a non-primitive variable
GTNODE(LCL_VAR_ADDR  , "&lclVar"    ,0,GTK_LEAF)                       // address of local variable
GTNODE(LCL_FLD_ADDR  , "&lclFld"    ,0,GTK_LEAF)                       // address of field in a non-primitive variable
GTNODE(STORE_LCL_VAR , "st.lclVar"  ,0,GTK_UNOP|GTK_LOCAL|GTK_NOVALUE) // store to local variable
GTNODE(STORE_LCL_FLD , "st.lclFld"  ,0,GTK_UNOP|GTK_LOCAL|GTK_NOVALUE) // store to field in a non-primitive variable
GTNODE(CATCH_ARG     , "catchArg"   ,0,GTK_LEAF)                       // Exception object in a catch block
GTNODE(LABEL         , "codeLabel"  ,0,GTK_LEAF)                       // Jump-target
GTNODE(FTN_ADDR      , "ftnAddr"    ,0,GTK_LEAF)                       // Address of a function
GTNODE(RET_EXPR      , "retExpr"    ,0,GTK_LEAF)                       // Place holder for the return expression from an inline candidate

//-----------------------------------------------------------------------------
//  Constant nodes:
//-----------------------------------------------------------------------------

GTNODE(CNS_INT    , "const"       ,0,GTK_LEAF|GTK_CONST)
GTNODE(CNS_LNG    , "lconst"      ,0,GTK_LEAF|GTK_CONST)
GTNODE(CNS_DBL    , "dconst"      ,0,GTK_LEAF|GTK_CONST)
GTNODE(CNS_STR    , "sconst"      ,0,GTK_LEAF|GTK_CONST)

//-----------------------------------------------------------------------------
//  Unary  operators (1 operand):
//-----------------------------------------------------------------------------

GTNODE(NOT        , "~"             ,0,GTK_UNOP)
GTNODE(NOP        , "nop"           ,0,GTK_UNOP)
GTNODE(NEG        , "unary -"       ,0,GTK_UNOP)
GTNODE(COPY       , "copy"          ,0,GTK_UNOP)             // Copies a variable from its current location to a register that satisfies
                                                                // code generation constraints.  The child is the actual lclVar node.
GTNODE(RELOAD     , "reload"        ,0,GTK_UNOP)
GTNODE(CHS        , "flipsign"      ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)  // GT_CHS is actually unary -- op2 is ignored.
                                                                            // Changing to unary presently causes problems, though -- take a little work to fix.

GTNODE(ARR_LENGTH , "arrLen"        ,0,GTK_UNOP|GTK_EXOP)    // array-length

GTNODE(INTRINSIC  , "intrinsic"     ,0,GTK_BINOP|GTK_EXOP)   // intrinsics

GTNODE(LOCKADD          , "lockAdd"       ,0,GTK_BINOP|GTK_NOVALUE)
GTNODE(XADD             , "XAdd"          ,0,GTK_BINOP)
GTNODE(XCHG             , "Xchg"          ,0,GTK_BINOP)
GTNODE(CMPXCHG          , "cmpxchg"       ,0,GTK_SPECIAL)
GTNODE(MEMORYBARRIER    , "memoryBarrier" ,0,GTK_LEAF|GTK_NOVALUE)

GTNODE(CAST             , "cast"          ,0,GTK_UNOP|GTK_EXOP)    // conversion to another type
GTNODE(CKFINITE         , "ckfinite"      ,0,GTK_UNOP)             // Check for NaN
GTNODE(LCLHEAP          , "lclHeap"       ,0,GTK_UNOP)             // alloca()
GTNODE(JMP              , "jump"          ,0,GTK_LEAF|GTK_NOVALUE) // Jump to another function


GTNODE(ADDR             , "addr"          ,0,GTK_UNOP)              // address of
GTNODE(IND              , "indir"         ,0,GTK_UNOP)              // load indirection
GTNODE(STOREIND         , "storeIndir"    ,0,GTK_BINOP|GTK_NOVALUE) // store indirection

                                                                      // TODO-Cleanup: GT_ARR_BOUNDS_CHECK should be made a GTK_BINOP now that it has only two child nodes
GTNODE(ARR_BOUNDS_CHECK , "arrBndsChk"    ,0,GTK_SPECIAL|GTK_NOVALUE) // array bounds check
GTNODE(OBJ              , "obj"           ,0,GTK_UNOP|GTK_EXOP)
GTNODE(BOX              , "box"           ,0,GTK_UNOP|GTK_EXOP|GTK_NOTLIR)

#ifdef FEATURE_SIMD
GTNODE(SIMD_CHK         , "simdChk"       ,0,GTK_SPECIAL|GTK_NOVALUE) // Compare whether an index is less than the given SIMD vector length, and call CORINFO_HELP_RNGCHKFAIL if not.
                                                                      // TODO-CQ: In future may want to add a field that specifies different exceptions but we'll
                                                                      // need VM assistance for that.
                                                                      // TODO-CQ: It would actually be very nice to make this an unconditional throw, and expose the control flow that
                                                                      // does the compare, so that it can be more easily optimized.  But that involves generating qmarks at import time...
#endif // FEATURE_SIMD

GTNODE(ALLOCOBJ         , "allocObj"      ,0,GTK_UNOP|GTK_EXOP) // object allocator

//-----------------------------------------------------------------------------
//  Binary operators (2 operands):
//-----------------------------------------------------------------------------

GTNODE(ADD        , "+"          ,1,GTK_BINOP)
GTNODE(SUB        , "-"          ,0,GTK_BINOP)
GTNODE(MUL        , "*"          ,1,GTK_BINOP)
GTNODE(DIV        , "/"          ,0,GTK_BINOP)
GTNODE(MOD        , "%"          ,0,GTK_BINOP)

GTNODE(UDIV       , "un-/"       ,0,GTK_BINOP)
GTNODE(UMOD       , "un-%"       ,0,GTK_BINOP)

GTNODE(OR         , "|"          ,1,GTK_BINOP|GTK_LOGOP)
GTNODE(XOR        , "^"          ,1,GTK_BINOP|GTK_LOGOP)
GTNODE(AND        , "&"          ,1,GTK_BINOP|GTK_LOGOP)

GTNODE(LSH        , "<<"         ,0,GTK_BINOP)
GTNODE(RSH        , ">>"         ,0,GTK_BINOP)
GTNODE(RSZ        , ">>>"        ,0,GTK_BINOP)
GTNODE(ROL        , "rol"        ,0,GTK_BINOP)
GTNODE(ROR        , "ror"        ,0,GTK_BINOP)
GTNODE(MULHI      , "mulhi"      ,1,GTK_BINOP) // returns high bits (top N bits of the 2N bit result of an NxN multiply)

GTNODE(ASG        , "="          ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_ADD    , "+="         ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_SUB    , "-="         ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_MUL    , "*="         ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_DIV    , "/="         ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_MOD    , "%="         ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)

GTNODE(ASG_UDIV   , "/="         ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_UMOD   , "%="         ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)

GTNODE(ASG_OR     , "|="         ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_XOR    , "^="         ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_AND    , "&="         ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_LSH    , "<<="        ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_RSH    , ">>="        ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)
GTNODE(ASG_RSZ    , ">>>="       ,0,GTK_BINOP|GTK_ASGOP|GTK_NOTLIR)

GTNODE(EQ         , "=="         ,0,GTK_BINOP|GTK_RELOP)
GTNODE(NE         , "!="         ,0,GTK_BINOP|GTK_RELOP)
GTNODE(LT         , "<"          ,0,GTK_BINOP|GTK_RELOP)
GTNODE(LE         , "<="         ,0,GTK_BINOP|GTK_RELOP)
GTNODE(GE         , ">="         ,0,GTK_BINOP|GTK_RELOP)
GTNODE(GT         , ">"          ,0,GTK_BINOP|GTK_RELOP)

GTNODE(COMMA      , "comma"      ,0,GTK_BINOP|GTK_NOTLIR)

GTNODE(QMARK      , "qmark"      ,0,GTK_BINOP|GTK_EXOP|GTK_NOTLIR)
GTNODE(COLON      , "colon"      ,0,GTK_BINOP|GTK_NOTLIR)

GTNODE(INDEX      , "[]"         ,0,GTK_BINOP|GTK_EXOP|GTK_NOTLIR)   // SZ-array-element

GTNODE(MKREFANY   , "mkrefany"   ,0,GTK_BINOP)

GTNODE(LEA        , "lea"        ,0,GTK_BINOP|GTK_EXOP)

#if !defined(LEGACY_BACKEND) && !defined(_TARGET_64BIT_)
// A GT_LONG node simply represents the long value produced by the concatenation
// of its two (lower and upper half) operands.  Some GT_LONG nodes are transient,
// during the decomposing of longs; others are handled by codegen as operands of
// nodes such as calls, returns and stores of long lclVars.
GTNODE(LONG       , "gt_long"    ,0,GTK_BINOP)

// The following are nodes representing the upper half of a 64-bit operation
// that requires a carry/borrow.  However, they are all named GT_XXX_HI for
// consistency.
GTNODE(ADD_LO     , "+Lo"          ,1,GTK_BINOP)
GTNODE(ADD_HI     , "+Hi"          ,1,GTK_BINOP)
GTNODE(SUB_LO     , "-Lo"          ,0,GTK_BINOP)
GTNODE(SUB_HI     , "-Hi"          ,0,GTK_BINOP)
GTNODE(MUL_HI     , "*Hi"          ,1,GTK_BINOP)
GTNODE(DIV_HI     , "/Hi"          ,0,GTK_BINOP)
GTNODE(MOD_HI     , "%Hi"          ,0,GTK_BINOP)
#endif // !defined(LEGACY_BACKEND) && !defined(_TARGET_64BIT_)

#ifdef FEATURE_SIMD
GTNODE(SIMD       , "simd"       ,0,GTK_BINOP|GTK_EXOP)   // SIMD functions/operators/intrinsics
#endif // FEATURE_SIMD

//-----------------------------------------------------------------------------
//  Other nodes that look like unary/binary operators:
//-----------------------------------------------------------------------------

GTNODE(JTRUE      , "jmpTrue"    ,0,GTK_UNOP|GTK_NOVALUE)

GTNODE(LIST       , "<list>"     ,0,GTK_BINOP|GTK_NOVALUE)

//-----------------------------------------------------------------------------
//  Other nodes that have special structure:
//-----------------------------------------------------------------------------

GTNODE(FIELD      , "field"      ,0,GTK_SPECIAL)        // Member-field
GTNODE(ARR_ELEM   , "arrMD&"     ,0,GTK_SPECIAL)        // Multi-dimensional array-element address
GTNODE(ARR_INDEX  , "arrMDIdx"   ,0,GTK_BINOP|GTK_EXOP) // Effective, bounds-checked index for one dimension of a multi-dimensional array element
GTNODE(ARR_OFFSET , "arrMDOffs"  ,0,GTK_SPECIAL)        // Flattened offset of multi-dimensional array element
GTNODE(CALL       , "call()"     ,0,GTK_SPECIAL)

//-----------------------------------------------------------------------------
//  Statement operator nodes:
//-----------------------------------------------------------------------------

GTNODE(BEG_STMTS  , "begStmts"   ,0,GTK_SPECIAL|GTK_NOVALUE) // used only temporarily in importer by impBegin/EndTreeList()
GTNODE(STMT       , "stmtExpr"   ,0,GTK_SPECIAL|GTK_NOVALUE) // top-level list nodes in bbTreeList

GTNODE(RETURN     , "return"     ,0,GTK_UNOP|GTK_NOVALUE)    // return from current function
GTNODE(SWITCH     , "switch"     ,0,GTK_UNOP|GTK_NOVALUE)    // switch

GTNODE(NO_OP      , "no_op"      ,0,GTK_LEAF|GTK_NOVALUE)    // nop!

GTNODE(START_NONGC, "start_nongc",0,GTK_LEAF|GTK_NOVALUE)    // starts a new instruction group that will be non-gc interruptible

GTNODE(PROF_HOOK  , "prof_hook"  ,0,GTK_LEAF|GTK_NOVALUE)    // profiler Enter/Leave/TailCall hook

GTNODE(RETFILT    , "retfilt",    0,GTK_UNOP|GTK_NOVALUE)    // end filter with TYP_I_IMPL return value
#if !FEATURE_EH_FUNCLETS
GTNODE(END_LFIN   , "endLFin"    ,0,GTK_LEAF|GTK_NOVALUE)    // end locally-invoked finally
#endif // !FEATURE_EH_FUNCLETS

GTNODE(INITBLK    , "initBlk"    ,0,GTK_BINOP|GTK_NOVALUE)
GTNODE(COPYBLK    , "copyBlk"    ,0,GTK_BINOP|GTK_NOVALUE)
GTNODE(COPYOBJ    , "copyObj"    ,0,GTK_BINOP|GTK_NOVALUE)

//-----------------------------------------------------------------------------
//  Nodes used for optimizations.
//-----------------------------------------------------------------------------

GTNODE(PHI        , "phi"        ,0,GTK_UNOP)            // phi node for ssa.
GTNODE(PHI_ARG    , "phiArg"     ,0,GTK_LEAF|GTK_LOCAL)  // phi(phiarg, phiarg, phiarg)

//-----------------------------------------------------------------------------
//  Nodes used by Lower to generate a closer CPU representation of other nodes
//-----------------------------------------------------------------------------

GTNODE(JMPTABLE    , "jumpTable"  , 0, GTK_LEAF)               // Generates the jump table for switches
GTNODE(SWITCH_TABLE, "tableSwitch", 0, GTK_BINOP|GTK_NOVALUE)  // Jump Table based switch construct

//-----------------------------------------------------------------------------
//  Nodes used only within the code generator:
//-----------------------------------------------------------------------------

GTNODE(REG_VAR      , "regVar"        ,0,GTK_LEAF|GTK_LOCAL)    // register variable
GTNODE(CLS_VAR      , "clsVar"        ,0,GTK_LEAF)              // static data member
GTNODE(CLS_VAR_ADDR , "&clsVar"       ,0,GTK_LEAF)              // static data member address
GTNODE(STORE_CLS_VAR, "st.clsVar"     ,0,GTK_LEAF|GTK_NOVALUE)  // store to static data member
GTNODE(ARGPLACE     , "argPlace"      ,0,GTK_LEAF)              // placeholder for a register arg
GTNODE(NULLCHECK    , "nullcheck"     ,0,GTK_UNOP|GTK_NOVALUE)  // null checks the source
GTNODE(PHYSREG      , "physregSrc"    ,0,GTK_LEAF)              // read from a physical register
GTNODE(PHYSREGDST   , "physregDst"    ,0,GTK_UNOP|GTK_NOVALUE)  // write to a physical register
GTNODE(EMITNOP      , "emitnop"       ,0,GTK_LEAF|GTK_NOVALUE)  // emitter-placed nop
GTNODE(PINVOKE_PROLOG,"pinvoke_prolog",0,GTK_LEAF|GTK_NOVALUE)  // pinvoke prolog seq
GTNODE(PINVOKE_EPILOG,"pinvoke_epilog",0,GTK_LEAF|GTK_NOVALUE)  // pinvoke epilog seq
GTNODE(PUTARG_REG   , "putarg_reg"    ,0,GTK_UNOP)              // operator that places outgoing arg in register
GTNODE(PUTARG_STK   , "putarg_stk"    ,0,GTK_UNOP)              // operator that places outgoing arg in stack
GTNODE(RETURNTRAP   , "returnTrap"    ,0,GTK_UNOP|GTK_NOVALUE)  // a conditional call to wait on gc
GTNODE(SWAP         , "swap"          ,0,GTK_BINOP|GTK_NOVALUE) // op1 and op2 swap (registers)
GTNODE(IL_OFFSET    , "il_offset"     ,0,GTK_LEAF|GTK_NOVALUE)  // marks an IL offset for debugging purposes

/*****************************************************************************/
#undef  GTNODE
/*****************************************************************************/
// clang-format on
