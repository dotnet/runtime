// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off
/*****************************************************************************/
#ifndef GTNODE
#error  Define GTNODE before including this file.
#endif
/*****************************************************************************/
//
//     Node enum
//                       , GenTree struct flavor
//                                           ,commutative
//                                             ,operKind

GTNODE(NONE             , char               ,0,GTK_SPECIAL)

//-----------------------------------------------------------------------------
//  Leaf nodes (i.e. these nodes have no sub-operands):
//-----------------------------------------------------------------------------

GTNODE(LCL_VAR          , GenTreeLclVar      ,0,(GTK_LEAF|GTK_LOCAL))     // local variable
GTNODE(LCL_FLD          , GenTreeLclFld      ,0,(GTK_LEAF|GTK_LOCAL))     // field in a non-primitive variable
GTNODE(LCL_VAR_ADDR     , GenTreeLclVar      ,0,GTK_LEAF)               // address of local variable
GTNODE(LCL_FLD_ADDR     , GenTreeLclFld      ,0,GTK_LEAF)               // address of field in a non-primitive variable
GTNODE(STORE_LCL_VAR    , GenTreeLclVar      ,0,(GTK_UNOP|GTK_LOCAL|GTK_NOVALUE)) // store to local variable
GTNODE(STORE_LCL_FLD    , GenTreeLclFld      ,0,(GTK_UNOP|GTK_LOCAL|GTK_NOVALUE)) // store to field in a non-primitive variable
GTNODE(CATCH_ARG        , GenTree            ,0,GTK_LEAF)               // Exception object in a catch block
GTNODE(LABEL            , GenTree            ,0,GTK_LEAF)               // Jump-target
GTNODE(FTN_ADDR         , GenTreeFptrVal     ,0,GTK_LEAF)               // Address of a function
GTNODE(RET_EXPR         , GenTreeRetExpr     ,0,GTK_LEAF|GTK_NOTLIR)    // Place holder for the return expression from an inline candidate

//-----------------------------------------------------------------------------
//  Constant nodes:
//-----------------------------------------------------------------------------

GTNODE(CNS_INT          , GenTreeIntCon      ,0,(GTK_LEAF|GTK_CONST))
GTNODE(CNS_LNG          , GenTreeLngCon      ,0,(GTK_LEAF|GTK_CONST))
GTNODE(CNS_DBL          , GenTreeDblCon      ,0,(GTK_LEAF|GTK_CONST))
GTNODE(CNS_STR          , GenTreeStrCon      ,0,(GTK_LEAF|GTK_CONST))

//-----------------------------------------------------------------------------
//  Unary  operators (1 operand):
//-----------------------------------------------------------------------------

GTNODE(NOT              , GenTreeOp          ,0,GTK_UNOP)
GTNODE(NOP              , GenTree            ,0,(GTK_UNOP|GTK_NOCONTAIN))
GTNODE(NEG              , GenTreeOp          ,0,GTK_UNOP)
GTNODE(COPY             , GenTreeCopyOrReload,0,GTK_UNOP)               // Copies a variable from its current location to a register that satisfies
                                                                        // code generation constraints.  The child is the actual lclVar node.
GTNODE(RELOAD           , GenTreeCopyOrReload,0,GTK_UNOP)
GTNODE(ARR_LENGTH       , GenTreeArrLen      ,0,(GTK_UNOP|GTK_EXOP))      // array-length
GTNODE(INTRINSIC        , GenTreeIntrinsic   ,0,(GTK_BINOP|GTK_EXOP))     // intrinsics

GTNODE(LOCKADD          , GenTreeOp          ,0,(GTK_BINOP|GTK_NOVALUE))
GTNODE(XAND             , GenTreeOp          ,0,GTK_BINOP)
GTNODE(XORR             , GenTreeOp          ,0,GTK_BINOP)
GTNODE(XADD             , GenTreeOp          ,0,GTK_BINOP)
GTNODE(XCHG             , GenTreeOp          ,0,GTK_BINOP)
GTNODE(CMPXCHG          , GenTreeCmpXchg     ,0,GTK_SPECIAL)
GTNODE(MEMORYBARRIER    , GenTree            ,0,(GTK_LEAF|GTK_NOVALUE))

GTNODE(KEEPALIVE        , GenTree            ,0,(GTK_UNOP|GTK_NOVALUE)) // keep operand alive, generate no code, produce no result

GTNODE(CAST             , GenTreeCast        ,0,(GTK_UNOP|GTK_EXOP))      // conversion to another type
#if defined(TARGET_ARM)
GTNODE(BITCAST          , GenTreeMultiRegOp  ,0,GTK_UNOP)               // reinterpretation of bits as another type
#else
GTNODE(BITCAST          , GenTreeOp          ,0,GTK_UNOP)               // reinterpretation of bits as another type
#endif
GTNODE(CKFINITE         , GenTreeOp          ,0,(GTK_UNOP|GTK_NOCONTAIN)) // Check for NaN
GTNODE(LCLHEAP          , GenTreeOp          ,0,(GTK_UNOP|GTK_NOCONTAIN)) // alloca()
GTNODE(JMP              , GenTreeVal         ,0,(GTK_LEAF|GTK_NOVALUE))   // Jump to another function

GTNODE(ADDR             , GenTreeOp          ,0,GTK_UNOP)               // address of

GTNODE(IND              , GenTreeIndir       ,0,GTK_UNOP)                // load indirection
GTNODE(STOREIND         , GenTreeStoreInd    ,0,(GTK_BINOP|GTK_NOVALUE)) // store indirection

GTNODE(BOUNDS_CHECK     , GenTreeBoundsChk   ,0,(GTK_BINOP|GTK_EXOP|GTK_NOVALUE)) // a bounds check - for arrays/spans/SIMDs/HWINTRINSICs

GTNODE(OBJ              , GenTreeObj         ,0,(GTK_UNOP|GTK_EXOP))              // Object that MAY have gc pointers, and thus includes the relevant gc layout info.
GTNODE(STORE_OBJ        , GenTreeObj         ,0,(GTK_BINOP|GTK_EXOP|GTK_NOVALUE)) // Object that MAY have gc pointers, and thus includes the relevant gc layout info.
GTNODE(BLK              , GenTreeBlk         ,0,(GTK_UNOP|GTK_EXOP))              // Block/object with no gc pointers, and with a known size (e.g. a struct with no gc fields)
GTNODE(STORE_BLK        , GenTreeBlk         ,0,(GTK_BINOP|GTK_EXOP|GTK_NOVALUE)) // Block/object with no gc pointers, and with a known size (e.g. a struct with no gc fields)
GTNODE(DYN_BLK          , GenTreeDynBlk      ,0,GTK_SPECIAL)               // Dynamically sized block object
GTNODE(STORE_DYN_BLK    , GenTreeDynBlk      ,0,(GTK_SPECIAL|GTK_NOVALUE)) // Dynamically sized block object

GTNODE(BOX              , GenTreeBox         ,0,(GTK_UNOP|GTK_EXOP|GTK_NOTLIR))
GTNODE(FIELD            , GenTreeField       ,0,(GTK_UNOP|GTK_EXOP)) // Member-field
GTNODE(ALLOCOBJ         , GenTreeAllocObj    ,0,(GTK_UNOP|GTK_EXOP)) // object allocator

GTNODE(INIT_VAL         , GenTreeOp          ,0,GTK_UNOP)               // Initialization value for an initBlk

GTNODE(RUNTIMELOOKUP    , GenTreeRuntimeLookup, 0,(GTK_UNOP|GTK_EXOP))    // Runtime handle lookup

GTNODE(BSWAP            , GenTreeOp          ,0,GTK_UNOP)               // Byte swap (32-bit or 64-bit)
GTNODE(BSWAP16          , GenTreeOp          ,0,GTK_UNOP)               // Byte swap (16-bit)

//-----------------------------------------------------------------------------
//  Binary operators (2 operands):
//-----------------------------------------------------------------------------

GTNODE(ADD              , GenTreeOp          ,1,GTK_BINOP)
GTNODE(SUB              , GenTreeOp          ,0,GTK_BINOP)
GTNODE(MUL              , GenTreeOp          ,1,GTK_BINOP)
GTNODE(DIV              , GenTreeOp          ,0,GTK_BINOP)
GTNODE(MOD              , GenTreeOp          ,0,GTK_BINOP)

GTNODE(UDIV             , GenTreeOp          ,0,GTK_BINOP)
GTNODE(UMOD             , GenTreeOp          ,0,GTK_BINOP)

GTNODE(OR               , GenTreeOp          ,1,(GTK_BINOP|GTK_LOGOP))
GTNODE(XOR              , GenTreeOp          ,1,(GTK_BINOP|GTK_LOGOP))
GTNODE(AND              , GenTreeOp          ,1,(GTK_BINOP|GTK_LOGOP))

GTNODE(LSH              , GenTreeOp          ,0,GTK_BINOP)
GTNODE(RSH              , GenTreeOp          ,0,GTK_BINOP)
GTNODE(RSZ              , GenTreeOp          ,0,GTK_BINOP)
GTNODE(ROL              , GenTreeOp          ,0,GTK_BINOP)
GTNODE(ROR              , GenTreeOp          ,0,GTK_BINOP)

GTNODE(ASG              , GenTreeOp          ,0,(GTK_BINOP|GTK_NOTLIR))
GTNODE(EQ               , GenTreeOp          ,0,(GTK_BINOP|GTK_RELOP))
GTNODE(NE               , GenTreeOp          ,0,(GTK_BINOP|GTK_RELOP))
GTNODE(LT               , GenTreeOp          ,0,(GTK_BINOP|GTK_RELOP))
GTNODE(LE               , GenTreeOp          ,0,(GTK_BINOP|GTK_RELOP))
GTNODE(GE               , GenTreeOp          ,0,(GTK_BINOP|GTK_RELOP))
GTNODE(GT               , GenTreeOp          ,0,(GTK_BINOP|GTK_RELOP))

// These are similar to GT_EQ/GT_NE but they generate "test" instead of "cmp" instructions.
// Currently these are generated during lowering for code like ((x & y) eq|ne 0) only on
// XArch but ARM could too use these for the same purpose as there is a "tst" instruction.
// Note that the general case of comparing a register against 0 is handled directly by
// codegen which emits a "test reg, reg" instruction, that would be more difficult to do
// during lowering because the source operand is used twice so it has to be a lclvar.
// Because of this there is no need to also add GT_TEST_LT/LE/GE/GT opers.
GTNODE(TEST_EQ          , GenTreeOp          ,0,(GTK_BINOP|GTK_RELOP))
GTNODE(TEST_NE          , GenTreeOp          ,0,(GTK_BINOP|GTK_RELOP))

GTNODE(COMMA            , GenTreeOp          ,0,(GTK_BINOP|GTK_NOTLIR))

GTNODE(QMARK            , GenTreeQmark       ,0,(GTK_BINOP|GTK_EXOP|GTK_NOTLIR))
GTNODE(COLON            , GenTreeColon       ,0,(GTK_BINOP|GTK_NOTLIR))

GTNODE(INDEX            , GenTreeIndex       ,0,(GTK_BINOP|GTK_EXOP|GTK_NOTLIR))   // SZ-array-element
GTNODE(INDEX_ADDR       , GenTreeIndexAddr   ,0,(GTK_BINOP|GTK_EXOP)) // addr of SZ-array-element;
                                                                      // used when aiming to minimize compile times.

GTNODE(MKREFANY         , GenTreeOp          ,0,GTK_BINOP|GTK_NOTLIR)

GTNODE(LEA              , GenTreeAddrMode    ,0,(GTK_BINOP|GTK_EXOP))

#if !defined(TARGET_64BIT)
// A GT_LONG node simply represents the long value produced by the concatenation
// of its two (lower and upper half) operands.  Some GT_LONG nodes are transient,
// during the decomposing of longs; others are handled by codegen as operands of
// nodes such as calls, returns and stores of long lclVars.
GTNODE(LONG             , GenTreeOp          ,0,GTK_BINOP)

// The following are nodes representing x86/arm32 specific long operators, including
// high operators of a 64-bit operations that requires a carry/borrow, which are
// named GT_XXX_HI for consistency, low operators of 64-bit operations that need
// to not be modified in phases post-decompose, and operators that return 64-bit
// results in one instruction.
GTNODE(ADD_LO           , GenTreeOp          ,1,GTK_BINOP)
GTNODE(ADD_HI           , GenTreeOp          ,1,GTK_BINOP)
GTNODE(SUB_LO           , GenTreeOp          ,0,GTK_BINOP)
GTNODE(SUB_HI           , GenTreeOp          ,0,GTK_BINOP)

// The following are nodes that specify shifts that take a GT_LONG op1. The GT_LONG
// contains the hi and lo parts of three operand shift form where one op will be
// shifted into the other op as part of the operation (LSH_HI will shift
// the high bits of the lo operand into the high operand as it shifts left. RSH_LO
// will shift the lo bits of the high operand into the lo operand). LSH_HI
// represents the high operation of a 64-bit left shift by a constant int, and
// RSH_LO represents the lo operation of a 64-bit right shift by a constant int.
GTNODE(LSH_HI           , GenTreeOp          ,0,GTK_BINOP)
GTNODE(RSH_LO           , GenTreeOp          ,0,GTK_BINOP)
#endif // !defined(TARGET_64BIT)

#ifdef FEATURE_SIMD
GTNODE(SIMD             , GenTreeSIMD        ,0,GTK_SPECIAL)     // SIMD functions/operators/intrinsics
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
GTNODE(HWINTRINSIC      , GenTreeHWIntrinsic ,0,GTK_SPECIAL)               // hardware intrinsics
#endif // FEATURE_HW_INTRINSICS

//-----------------------------------------------------------------------------
//  Backend-specific arithmetic nodes:
//-----------------------------------------------------------------------------

GTNODE(INC_SATURATE     , GenTreeOp          ,0,GTK_UNOP)  // saturating increment, used in division by a constant (LowerUnsignedDivOrMod)

// Returns high bits (top N bits of the 2N bit result of an NxN multiply)
// GT_MULHI is used in division by a constant (LowerUnsignedDivOrMod). We turn
// the div into a MULHI + some adjustments. In codegen, we only use the
// results of the high register, and we drop the low results.
GTNODE(MULHI            , GenTreeOp          ,1,GTK_BINOP)

// A mul that returns the 2N bit result of an NxN multiply. This op is used for
// multiplies that take two ints and return a long result. For 32 bit targets,
// all other multiplies with long results are morphed into helper calls.
// It is similar to GT_MULHI, the difference being that GT_MULHI drops the lo
// part of the result, whereas GT_MUL_LONG keeps both parts of the result.
// MUL_LONG is also used on ARM64, where 64 bit multiplication is more expensive.
#if !defined(TARGET_64BIT)
GTNODE(MUL_LONG         , GenTreeMultiRegOp  ,1,GTK_BINOP)
#elif defined(TARGET_ARM64)
GTNODE(MUL_LONG         , GenTreeOp          ,1,GTK_BINOP)
#endif
// AndNot - emitted on ARM/ARM64 as the BIC instruction. Also used for creating AndNot HWINTRINSIC vector nodes in a cross-ISA manner.
GTNODE(AND_NOT          , GenTreeOp          ,0,GTK_BINOP)
//-----------------------------------------------------------------------------
//  LIR specific compare and conditional branch/set nodes:
//-----------------------------------------------------------------------------

GTNODE(CMP              , GenTreeOp          ,0,(GTK_BINOP|GTK_NOVALUE))  // Sets the condition flags according to the compare result.
                                                                        // N.B. Not a relop, it does not produce a value and it cannot be reversed.
GTNODE(JCMP             , GenTreeOp          ,0,(GTK_BINOP|GTK_NOVALUE))  // Makes a comparison and jump if the condition specified.  Does not set flags
GTNODE(JCC              , GenTreeCC          ,0,(GTK_LEAF|GTK_NOVALUE))   // Checks the condition flags and branch if the condition specified
                                                                        // by GenTreeCC::gtCondition is true.
GTNODE(SETCC            , GenTreeCC          ,0,GTK_LEAF)               // Checks the condition flags and produces 1 if the condition specified
                                                                        // by GenTreeCC::gtCondition is true and 0 otherwise.
#ifdef TARGET_XARCH
GTNODE(BT               , GenTreeOp          ,0,(GTK_BINOP|GTK_NOVALUE))  // The XARCH BT instruction. Like CMP, this sets the condition flags (CF
                                                                        // to be precise) and does not produce a value.
#endif
//-----------------------------------------------------------------------------
//  Other nodes that look like unary/binary operators:
//-----------------------------------------------------------------------------

GTNODE(JTRUE            , GenTreeOp          ,0,(GTK_UNOP|GTK_NOVALUE))

//-----------------------------------------------------------------------------
//  Other nodes that have special structure:
//-----------------------------------------------------------------------------

GTNODE(ARR_ELEM         , GenTreeArrElem     ,0,GTK_SPECIAL)            // Multi-dimensional array-element address
GTNODE(ARR_INDEX        , GenTreeArrIndex    ,0,(GTK_BINOP|GTK_EXOP))     // Effective, bounds-checked index for one dimension of a multi-dimensional array element
GTNODE(ARR_OFFSET       , GenTreeArrOffs     ,0,GTK_SPECIAL)            // Flattened offset of multi-dimensional array element
GTNODE(CALL             , GenTreeCall        ,0,(GTK_SPECIAL|GTK_NOCONTAIN))
GTNODE(FIELD_LIST       , GenTreeFieldList   ,0,GTK_SPECIAL)            // List of fields of a struct, when passed as an argument

GTNODE(RETURN           , GenTreeOp          ,0,(GTK_UNOP|GTK_NOVALUE))   // return from current function
GTNODE(SWITCH           , GenTreeOp          ,0,(GTK_UNOP|GTK_NOVALUE))   // switch

GTNODE(NO_OP            , GenTree            ,0,(GTK_LEAF|GTK_NOVALUE))   // nop!

GTNODE(START_NONGC      , GenTree            ,0,(GTK_LEAF|GTK_NOVALUE))   // starts a new instruction group that will be non-gc interruptible

GTNODE(START_PREEMPTGC  , GenTree            ,0,(GTK_LEAF|GTK_NOVALUE))   // starts a new instruction group where preemptive GC is enabled

GTNODE(PROF_HOOK        , GenTree            ,0,(GTK_LEAF|GTK_NOVALUE))   // profiler Enter/Leave/TailCall hook

GTNODE(RETFILT          , GenTreeOp          ,0,(GTK_UNOP|GTK_NOVALUE))   // end filter with TYP_I_IMPL return value
#if !defined(FEATURE_EH_FUNCLETS)
GTNODE(END_LFIN         , GenTreeVal         ,0,(GTK_LEAF|GTK_NOVALUE))   // end locally-invoked finally
#endif // !FEATURE_EH_FUNCLETS

//-----------------------------------------------------------------------------
//  Nodes used for optimizations.
//-----------------------------------------------------------------------------

GTNODE(PHI              , GenTreePhi         ,0,GTK_SPECIAL)              // phi node for ssa.
GTNODE(PHI_ARG          , GenTreePhiArg      ,0,(GTK_LEAF|GTK_LOCAL))     // phi(phiarg, phiarg, phiarg)

//-----------------------------------------------------------------------------
//  Nodes used by Lower to generate a closer CPU representation of other nodes
//-----------------------------------------------------------------------------

#ifdef TARGET_ARM64
GTNODE(MADD             , GenTreeOp          ,0, GTK_BINOP)                // Generates the Multiply-Add instruction (madd/msub)
                                                                           // In future, we might consider enabling it for both armarch and xarch
                                                                           // for floating-point MADD "unsafe" math
#endif
GTNODE(JMPTABLE         , GenTree            ,0, (GTK_LEAF|GTK_NOCONTAIN)) // Generates the jump table for switches
GTNODE(SWITCH_TABLE     , GenTreeOp          ,0, (GTK_BINOP|GTK_NOVALUE))  // Jump Table based switch construct
#ifdef TARGET_ARM64
GTNODE(ADDEX,             GenTreeOp          ,0, GTK_BINOP)                // Add with sign/zero extension
GTNODE(BFIZ             , GenTreeOp          ,0, GTK_BINOP)                // Bitfield Insert in Zero
#endif

//-----------------------------------------------------------------------------
//  Nodes used only within the code generator:
//-----------------------------------------------------------------------------

GTNODE(CLS_VAR          , GenTreeClsVar      ,0,GTK_LEAF)                        // static data member
GTNODE(CLS_VAR_ADDR     , GenTreeClsVar      ,0,GTK_LEAF)                        // static data member address
GTNODE(ARGPLACE         , GenTreeArgPlace    ,0,GTK_LEAF|GTK_NOVALUE|GTK_NOTLIR) // placeholder for a register arg
GTNODE(NULLCHECK        , GenTreeIndir       ,0,GTK_UNOP|GTK_NOVALUE)            // null checks the source
GTNODE(PHYSREG          , GenTreePhysReg     ,0,GTK_LEAF)                        // read from a physical register
GTNODE(EMITNOP          , GenTree            ,0,GTK_LEAF|GTK_NOVALUE)            // emitter-placed nop
GTNODE(PINVOKE_PROLOG   , GenTree            ,0,GTK_LEAF|GTK_NOVALUE)            // pinvoke prolog seq
GTNODE(PINVOKE_EPILOG   , GenTree            ,0,GTK_LEAF|GTK_NOVALUE)            // pinvoke epilog seq
#if defined(TARGET_ARM)
GTNODE(PUTARG_REG       , GenTreeMultiRegOp  ,0,GTK_UNOP)                        // operator that places outgoing arg in register
#else
GTNODE(PUTARG_REG       , GenTreeOp          ,0,GTK_UNOP)                        // operator that places outgoing arg in register
#endif
GTNODE(PUTARG_TYPE      , GenTreeOp          ,0,GTK_UNOP|GTK_NOTLIR)             // operator that places saves argument type between importation and morph
GTNODE(PUTARG_STK       , GenTreePutArgStk   ,0,GTK_UNOP|GTK_NOVALUE)            // operator that places outgoing arg in stack
#if FEATURE_ARG_SPLIT
GTNODE(PUTARG_SPLIT     , GenTreePutArgSplit ,0,GTK_UNOP)                        // operator that places outgoing arg in registers with stack (split struct in ARM32)
#endif // FEATURE_ARG_SPLIT
GTNODE(RETURNTRAP       , GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)            // a conditional call to wait on gc
GTNODE(SWAP             , GenTreeOp          ,0,GTK_BINOP|GTK_NOVALUE)           // op1 and op2 swap (registers)
GTNODE(IL_OFFSET        , Statement        ,0,GTK_LEAF|GTK_NOVALUE)            // marks an IL offset for debugging purposes

/*****************************************************************************/
#undef  GTNODE
/*****************************************************************************/
// clang-format on
