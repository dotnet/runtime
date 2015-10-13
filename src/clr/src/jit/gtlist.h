//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

GTNODE(GT_NONE       , "<none>"     ,0,GTK_SPECIAL)

//-----------------------------------------------------------------------------
//  Leaf nodes (i.e. these nodes have no sub-operands):
//-----------------------------------------------------------------------------

GTNODE(GT_LCL_VAR       , "lclVar"     ,0,GTK_LEAF|GTK_LOCAL) // local variable
GTNODE(GT_LCL_FLD       , "lclFld"     ,0,GTK_LEAF|GTK_LOCAL) // field in a non-primitive variable
GTNODE(GT_LCL_VAR_ADDR  , "&lclVar"    ,0,GTK_LEAF)           // address of local variable
GTNODE(GT_LCL_FLD_ADDR  , "&lclFld"    ,0,GTK_LEAF)           // address of field in a non-primitive variable
GTNODE(GT_STORE_LCL_VAR , "st.lclVar"  ,0,GTK_UNOP|GTK_LOCAL) // store to local variable
GTNODE(GT_STORE_LCL_FLD , "st.lclFld"  ,0,GTK_UNOP|GTK_LOCAL) // store to field in a non-primitive variable
GTNODE(GT_CATCH_ARG     , "catchArg"   ,0,GTK_LEAF)           // Exception object in a catch block
GTNODE(GT_LABEL         , "codeLabel"  ,0,GTK_LEAF)           // Jump-target
GTNODE(GT_FTN_ADDR      , "ftnAddr"    ,0,GTK_LEAF)           // Address of a function
GTNODE(GT_RET_EXPR      , "retExpr"    ,0,GTK_LEAF)           // Place holder for the return expression from an inline candidate

//-----------------------------------------------------------------------------
//  Constant nodes:
//-----------------------------------------------------------------------------

GTNODE(GT_CNS_INT    , "const"       ,0,GTK_LEAF|GTK_CONST)
GTNODE(GT_CNS_LNG    , "lconst"      ,0,GTK_LEAF|GTK_CONST)
GTNODE(GT_CNS_DBL    , "dconst"      ,0,GTK_LEAF|GTK_CONST)
GTNODE(GT_CNS_STR    , "sconst"      ,0,GTK_LEAF|GTK_CONST)

//-----------------------------------------------------------------------------
//  Unary  operators (1 operand):
//-----------------------------------------------------------------------------

GTNODE(GT_NOT        , "~"             ,0,GTK_UNOP)
GTNODE(GT_NOP        , "nop"           ,0,GTK_UNOP)
GTNODE(GT_NEG        , "unary -"       ,0,GTK_UNOP)
GTNODE(GT_COPY       , "copy"          ,0,GTK_UNOP)             // Copies a variable from its current location to a register that satisfies
                                                                // code generation constraints.  The child is the actual lclVar node.
GTNODE(GT_RELOAD     , "reload"        ,0,GTK_UNOP)
GTNODE(GT_CHS        , "flipsign"      ,0,GTK_BINOP|GTK_ASGOP)  // GT_CHS is actually unary -- op2 is ignored.
                                                                // Changing to unary presently causes problems, though -- take a little work to fix.

GTNODE(GT_ARR_LENGTH , "arrLen"        ,0,GTK_UNOP|GTK_EXOP)    // array-length

#if     INLINE_MATH
GTNODE(GT_MATH       , "mathFN"        ,0,GTK_BINOP|GTK_EXOP)   // Math functions/operators/intrinsics
#endif

                                                                   //Interlocked intrinsics
GTNODE(GT_LOCKADD          , "lockAdd"       ,0,GTK_BINOP)
GTNODE(GT_XADD             , "XAdd"          ,0,GTK_BINOP)
GTNODE(GT_XCHG             , "Xchg"          ,0,GTK_BINOP)
GTNODE(GT_CMPXCHG          , "cmpxchg"       ,0,GTK_SPECIAL)
GTNODE(GT_MEMORYBARRIER    , "memoryBarrier" ,0,GTK_LEAF)

GTNODE(GT_CAST             , "cast"          ,0,GTK_UNOP|GTK_EXOP) // conversion to another type
GTNODE(GT_CKFINITE         , "ckfinite"      ,0,GTK_UNOP)          // Check for NaN
GTNODE(GT_LCLHEAP          , "lclHeap"       ,0,GTK_UNOP)          // alloca()
GTNODE(GT_JMP              , "jump"          ,0,GTK_LEAF)          // Jump to another function


GTNODE(GT_ADDR             , "addr"          ,0,GTK_UNOP)          // address of
GTNODE(GT_IND              , "indir"         ,0,GTK_UNOP)          // load indirection
GTNODE(GT_STOREIND         , "storeIndir"    ,0,GTK_BINOP)         // store indirection
                                                                   // TODO-Cleanup: GT_ARR_BOUNDS_CHECK should be made a GTK_BINOP now that it has only two child nodes
GTNODE(GT_ARR_BOUNDS_CHECK , "arrBndsChk"    ,0,GTK_SPECIAL)       // array bounds check
GTNODE(GT_LDOBJ            , "ldobj"         ,0,GTK_UNOP|GTK_EXOP)
GTNODE(GT_BOX              , "box"           ,0,GTK_UNOP|GTK_EXOP)

#ifdef FEATURE_SIMD
GTNODE(GT_SIMD_CHK         , "simdChk"       ,0,GTK_SPECIAL)       // Compare whether an index is less than the given SIMD vector length, and call CORINFO_HELP_RNGCHKFAIL if not.
                                                                   // TODO-CQ: In future may want to add a field that specifies different exceptions but we'll
                                                                   // need VM assistance for that.
                                                                   // TODO-CQ: It would actually be very nice to make this an unconditional throw, and expose the control flow that
                                                                   // does the compare, so that it can be more easily optimized.  But that involves generating qmarks at import time...
#endif // FEATURE_SIMD

//-----------------------------------------------------------------------------
//  Binary operators (2 operands):
//-----------------------------------------------------------------------------

GTNODE(GT_ADD        , "+"          ,1,GTK_BINOP)
GTNODE(GT_SUB        , "-"          ,0,GTK_BINOP)
GTNODE(GT_MUL        , "*"          ,1,GTK_BINOP)
GTNODE(GT_DIV        , "/"          ,0,GTK_BINOP)
GTNODE(GT_MOD        , "%"          ,0,GTK_BINOP)

GTNODE(GT_UDIV       , "/"          ,0,GTK_BINOP)
GTNODE(GT_UMOD       , "%"          ,0,GTK_BINOP)

GTNODE(GT_OR         , "|"          ,1,GTK_BINOP|GTK_LOGOP)
GTNODE(GT_XOR        , "^"          ,1,GTK_BINOP|GTK_LOGOP)
GTNODE(GT_AND        , "&"          ,1,GTK_BINOP|GTK_LOGOP)

GTNODE(GT_LSH        , "<<"         ,0,GTK_BINOP)
GTNODE(GT_RSH        , ">>"         ,0,GTK_BINOP)
GTNODE(GT_RSZ        , ">>>"        ,0,GTK_BINOP)
GTNODE(GT_ROL        , "rol"        ,0,GTK_BINOP)
GTNODE(GT_ROR        , "ror"        ,0,GTK_BINOP)
GTNODE(GT_MULHI      , "mulhi"      ,1,GTK_BINOP) // returns high bits (top N bits of the 2N bit result of an NxN multiply)

GTNODE(GT_ASG        , "="          ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_ADD    , "+="         ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_SUB    , "-="         ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_MUL    , "*="         ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_DIV    , "/="         ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_MOD    , "%="         ,0,GTK_BINOP|GTK_ASGOP)

GTNODE(GT_ASG_UDIV   , "/="         ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_UMOD   , "%="         ,0,GTK_BINOP|GTK_ASGOP)

GTNODE(GT_ASG_OR     , "|="         ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_XOR    , "^="         ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_AND    , "&="         ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_LSH    , "<<="        ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_RSH    , ">>="        ,0,GTK_BINOP|GTK_ASGOP)
GTNODE(GT_ASG_RSZ    , ">>>="       ,0,GTK_BINOP|GTK_ASGOP)

GTNODE(GT_EQ         , "=="         ,0,GTK_BINOP|GTK_RELOP)
GTNODE(GT_NE         , "!="         ,0,GTK_BINOP|GTK_RELOP)
GTNODE(GT_LT         , "<"          ,0,GTK_BINOP|GTK_RELOP)
GTNODE(GT_LE         , "<="         ,0,GTK_BINOP|GTK_RELOP)
GTNODE(GT_GE         , ">="         ,0,GTK_BINOP|GTK_RELOP)
GTNODE(GT_GT         , ">"          ,0,GTK_BINOP|GTK_RELOP)

GTNODE(GT_COMMA      , "comma"      ,0,GTK_BINOP)

GTNODE(GT_QMARK      , "qmark"      ,0,GTK_BINOP|GTK_EXOP)
GTNODE(GT_COLON      , "colon"      ,0,GTK_BINOP)

GTNODE(GT_INDEX      , "[]"         ,0,GTK_BINOP|GTK_EXOP)   // SZ-array-element

GTNODE(GT_MKREFANY   , "mkrefany"   ,0,GTK_BINOP)

GTNODE(GT_LEA        , "lea"        ,0,GTK_BINOP|GTK_EXOP)

#if !defined(LEGACY_BACKEND) && !defined(_TARGET_64BIT_)
// A GT_LONG node simply represents the long value produced by the concatenation
// of its two (lower and upper half) operands.  Some GT_LONG nodes are transient,
// during the decomposing of longs; others are handled by codegen as operands of
// nodes such as calls, returns and stores of long lclVars.
GTNODE(GT_LONG       , "long"       ,0,GTK_BINOP)
#endif // !defined(LEGACY_BACKEND) && !defined(_TARGET_64BIT_)

#ifdef FEATURE_SIMD
GTNODE(GT_SIMD       , "simd"       ,0,GTK_BINOP|GTK_EXOP)   // SIMD functions/operators/intrinsics
#endif // FEATURE_SIMD

//-----------------------------------------------------------------------------
//  Other nodes that look like unary/binary operators:
//-----------------------------------------------------------------------------

GTNODE(GT_JTRUE      , "jmpTrue"    ,0,GTK_UNOP)

GTNODE(GT_LIST       , "<list>"     ,0,GTK_BINOP)

//-----------------------------------------------------------------------------
//  Other nodes that have special structure:
//-----------------------------------------------------------------------------

GTNODE(GT_FIELD      , "field"      ,0,GTK_SPECIAL)        // Member-field
GTNODE(GT_ARR_ELEM   , "arrMD&"     ,0,GTK_SPECIAL)        // Multi-dimensional array-element address
GTNODE(GT_ARR_INDEX  , "arrMDIdx"   ,0,GTK_BINOP|GTK_EXOP) // Effective, bounds-checked index for one dimension of a multi-dimensional array element
GTNODE(GT_ARR_OFFSET , "arrMDOffs"  ,0,GTK_SPECIAL)        // Flattened offset of multi-dimensional array element
GTNODE(GT_CALL       , "call()"     ,0,GTK_SPECIAL)

//-----------------------------------------------------------------------------
//  Statement operator nodes:
//-----------------------------------------------------------------------------

GTNODE(GT_BEG_STMTS  , "begStmts"   ,0,GTK_SPECIAL) // used only temporarily in importer by impBegin/EndTreeList()
GTNODE(GT_STMT       , "stmtExpr"   ,0,GTK_SPECIAL) // top-level list nodes in bbTreeList

GTNODE(GT_RETURN     , "return"     ,0,GTK_UNOP)    // return from current function
GTNODE(GT_SWITCH     , "switch"     ,0,GTK_UNOP)    // switch

GTNODE(GT_NO_OP      , "no_op"      ,0,GTK_LEAF)    // nop!

GTNODE(GT_START_NONGC, "start_nongc",0,GTK_LEAF)    // starts a new instruction group that will be non-gc interruptible

GTNODE(GT_PROF_HOOK  , "prof_hook"  ,0,GTK_LEAF)    // profiler Enter/Leave/TailCall hook

GTNODE(GT_RETFILT    , "retfilt",    0,GTK_UNOP)    // end filter with TYP_I_IMPL return value
#if !FEATURE_EH_FUNCLETS
GTNODE(GT_END_LFIN   , "endLFin"    ,0,GTK_LEAF)    // end locally-invoked finally
#endif // !FEATURE_EH_FUNCLETS

GTNODE(GT_INITBLK    , "initBlk"    ,0,GTK_BINOP)
GTNODE(GT_COPYBLK    , "copyBlk"    ,0,GTK_BINOP)
GTNODE(GT_COPYOBJ    , "copyObj"    ,0,GTK_BINOP)

//-----------------------------------------------------------------------------
//  Nodes used for optimizations.
//-----------------------------------------------------------------------------

GTNODE(GT_PHI        , "phi"        ,0,GTK_UNOP)              // phi node for ssa.
GTNODE(GT_PHI_ARG    , "phiArg"     ,0,GTK_LEAF|GTK_LOCAL)    // phi(phiarg, phiarg, phiarg)

//-----------------------------------------------------------------------------
//  Nodes used by Lower to generate a closer CPU representation of other nodes
//-----------------------------------------------------------------------------

GTNODE(GT_JMPTABLE    , "jumpTable"  , 0, GTK_LEAF)   // Generates the jump table for switches
GTNODE(GT_SWITCH_TABLE, "tableSwitch", 0, GTK_BINOP)  // Jump Table based switch construct

//-----------------------------------------------------------------------------
//  Nodes used only within the code generator:
//-----------------------------------------------------------------------------

GTNODE(GT_REG_VAR      , "regVar"        ,0,GTK_LEAF|GTK_LOCAL) // register variable
GTNODE(GT_CLS_VAR      , "clsVar"        ,0,GTK_LEAF)           // static data member
GTNODE(GT_CLS_VAR_ADDR , "&clsVar"       ,0,GTK_LEAF)           // static data member address
GTNODE(GT_STORE_CLS_VAR, "st.clsVar"     ,0,GTK_LEAF)           // store to static data member
GTNODE(GT_ARGPLACE     , "argPlace"      ,0,GTK_LEAF)           // placeholder for a register arg
GTNODE(GT_NULLCHECK    , "nullcheck"     ,0,GTK_UNOP)           // null checks the source
GTNODE(GT_PHYSREG      , "physregSrc"    ,0,GTK_LEAF)           // read from a physical register
GTNODE(GT_PHYSREGDST   , "physregDst"    ,0,GTK_UNOP)           // write to a physical register
GTNODE(GT_EMITNOP      , "emitnop"       ,0,GTK_LEAF)           // emitter-placed nop
GTNODE(GT_PINVOKE_PROLOG,"pinvoke_prolog",0,GTK_LEAF)           // pinvoke prolog seq
GTNODE(GT_PINVOKE_EPILOG,"pinvoke_epilog",0,GTK_LEAF)           // pinvoke epilog seq
GTNODE(GT_PUTARG_REG   , "putarg_reg"    ,0,GTK_UNOP)           // operator that places outgoing arg in register
GTNODE(GT_PUTARG_STK   , "putarg_stk"    ,0,GTK_UNOP)           // operator that places outgoing arg in stack
GTNODE(GT_RETURNTRAP   , "returnTrap"    ,0,GTK_UNOP)           // a conditional call to wait on gc
GTNODE(GT_SWAP         , "swap"          ,0,GTK_BINOP)          // op1 and op2 swap (registers)

/*****************************************************************************/
#undef  GTNODE
/*****************************************************************************/
