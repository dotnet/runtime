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
//                                             ,oper kind | DEBUG oper kind

GTNODE(NONE             , char               ,0,GTK_SPECIAL)

//-----------------------------------------------------------------------------
//  Nodes related to locals:
//-----------------------------------------------------------------------------

GTNODE(PHI              , GenTreePhi         ,0,GTK_SPECIAL)          // phi node for ssa.
GTNODE(PHI_ARG          , GenTreePhiArg      ,0,GTK_LEAF)             // phi(phiarg, phiarg, phiarg)
GTNODE(LCL_VAR          , GenTreeLclVar      ,0,GTK_LEAF)             // local variable
GTNODE(LCL_FLD          , GenTreeLclFld      ,0,GTK_LEAF)             // field in a non-primitive variable
GTNODE(STORE_LCL_VAR    , GenTreeLclVar      ,0,GTK_UNOP|GTK_NOVALUE) // store to local variable
GTNODE(STORE_LCL_FLD    , GenTreeLclFld      ,0,GTK_UNOP|GTK_NOVALUE) // store to a part of the variable
GTNODE(LCL_ADDR         , GenTreeLclFld      ,0,GTK_LEAF)             // local address

//-----------------------------------------------------------------------------
//  Leaf nodes (i.e. these nodes have no sub-operands):
//-----------------------------------------------------------------------------

GTNODE(CATCH_ARG        , GenTree            ,0,GTK_LEAF)             // Exception object in a catch block
GTNODE(LABEL            , GenTree            ,0,GTK_LEAF)             // Jump-target
GTNODE(JMP              , GenTreeVal         ,0,GTK_LEAF|GTK_NOVALUE) // Jump to another function
GTNODE(FTN_ADDR         , GenTreeFptrVal     ,0,GTK_LEAF)             // Address of a function
GTNODE(RET_EXPR         , GenTreeRetExpr     ,0,GTK_LEAF|DBK_NOTLIR)  // Place holder for the return expression from an inline candidate

//-----------------------------------------------------------------------------
//  Constant nodes:
//-----------------------------------------------------------------------------

GTNODE(CNS_INT          , GenTreeIntCon      ,0,GTK_LEAF)
GTNODE(CNS_LNG          , GenTreeLngCon      ,0,GTK_LEAF)
GTNODE(CNS_DBL          , GenTreeDblCon      ,0,GTK_LEAF)
GTNODE(CNS_STR          , GenTreeStrCon      ,0,GTK_LEAF)
GTNODE(CNS_VEC          , GenTreeVecCon      ,0,GTK_LEAF)

//-----------------------------------------------------------------------------
//  Unary  operators (1 operand):
//-----------------------------------------------------------------------------

GTNODE(NOT              , GenTreeOp          ,0,GTK_UNOP)
GTNODE(NOP              , GenTree            ,0,GTK_UNOP|DBK_NOCONTAIN)
GTNODE(NEG              , GenTreeOp          ,0,GTK_UNOP)

GTNODE(INTRINSIC        , GenTreeIntrinsic   ,0,GTK_BINOP|GTK_EXOP)

GTNODE(LOCKADD          , GenTreeOp          ,0,GTK_BINOP|GTK_NOVALUE|DBK_NOTHIR)
GTNODE(XAND             , GenTreeOp          ,0,GTK_BINOP)
GTNODE(XORR             , GenTreeOp          ,0,GTK_BINOP)
GTNODE(XADD             , GenTreeOp          ,0,GTK_BINOP)
GTNODE(XCHG             , GenTreeOp          ,0,GTK_BINOP)
GTNODE(CMPXCHG          , GenTreeCmpXchg     ,0,GTK_SPECIAL)
GTNODE(MEMORYBARRIER    , GenTree            ,0,GTK_LEAF|GTK_NOVALUE)

GTNODE(KEEPALIVE        , GenTree            ,0,GTK_UNOP|GTK_NOVALUE)   // keep operand alive, generate no code, produce no result

GTNODE(CAST             , GenTreeCast        ,0,GTK_UNOP|GTK_EXOP)      // conversion to another type
#if defined(TARGET_ARM)
GTNODE(BITCAST          , GenTreeMultiRegOp  ,0,GTK_UNOP)               // reinterpretation of bits as another type
#else
GTNODE(BITCAST          , GenTreeOp          ,0,GTK_UNOP)               // reinterpretation of bits as another type
#endif
GTNODE(CKFINITE         , GenTreeOp          ,0,GTK_UNOP|DBK_NOCONTAIN) // Check for NaN
GTNODE(LCLHEAP          , GenTreeOp          ,0,GTK_UNOP|DBK_NOCONTAIN) // alloca()

GTNODE(BOUNDS_CHECK     , GenTreeBoundsChk   ,0,GTK_BINOP|GTK_EXOP|GTK_NOVALUE) // a bounds check - for arrays/spans/SIMDs/HWINTRINSICs

GTNODE(IND              , GenTreeIndir       ,0,GTK_UNOP)                       // Load indirection
GTNODE(STOREIND         , GenTreeStoreInd    ,0,GTK_BINOP|GTK_NOVALUE)          // Store indirection
GTNODE(BLK              , GenTreeBlk         ,0,GTK_UNOP|GTK_EXOP)              // Struct load
GTNODE(STORE_BLK        , GenTreeBlk         ,0,GTK_BINOP|GTK_EXOP|GTK_NOVALUE) // Struct store
GTNODE(STORE_DYN_BLK    , GenTreeStoreDynBlk ,0,GTK_SPECIAL|GTK_NOVALUE)        // Dynamically sized block store, with native uint size
GTNODE(NULLCHECK        , GenTreeIndir       ,0,GTK_UNOP|GTK_NOVALUE)           // Null checks the source

GTNODE(ARR_LENGTH       , GenTreeArrLen      ,0,GTK_UNOP|GTK_EXOP)            // single-dimension (SZ) array length
GTNODE(MDARR_LENGTH     , GenTreeMDArr       ,0,GTK_UNOP|GTK_EXOP)            // multi-dimension (MD) array length of a specific dimension
GTNODE(MDARR_LOWER_BOUND, GenTreeMDArr       ,0,GTK_UNOP|GTK_EXOP)            // multi-dimension (MD) array lower bound of a specific dimension
GTNODE(FIELD            , GenTreeField       ,0,GTK_UNOP|GTK_EXOP|DBK_NOTLIR) // Field load
GTNODE(FIELD_ADDR       , GenTreeField       ,0,GTK_UNOP|GTK_EXOP|DBK_NOTLIR) // Field address
GTNODE(ALLOCOBJ         , GenTreeAllocObj    ,0,GTK_UNOP|GTK_EXOP|DBK_NOTLIR) // object allocator

GTNODE(INIT_VAL         , GenTreeOp          ,0,GTK_UNOP) // Initialization value for an initBlk

GTNODE(BOX              , GenTreeBox         ,0,GTK_UNOP|GTK_EXOP|DBK_NOTLIR)   // Marks its first operands (a local) as being a box
GTNODE(RUNTIMELOOKUP    , GenTreeRuntimeLookup, 0,GTK_UNOP|GTK_EXOP|DBK_NOTLIR) // Runtime handle lookup
GTNODE(ARR_ADDR         , GenTreeArrAddr     ,0,GTK_UNOP|GTK_EXOP|DBK_NOTLIR)   // Wraps an array address expression

GTNODE(BSWAP            , GenTreeOp          ,0,GTK_UNOP)               // Byte swap (32-bit or 64-bit)
GTNODE(BSWAP16          , GenTreeOp          ,0,GTK_UNOP)               // Byte swap lower 16-bits and zero upper 16 bits

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

GTNODE(OR               , GenTreeOp          ,1,GTK_BINOP)
GTNODE(XOR              , GenTreeOp          ,1,GTK_BINOP)
GTNODE(AND              , GenTreeOp          ,1,GTK_BINOP)

GTNODE(LSH              , GenTreeOp          ,0,GTK_BINOP)
GTNODE(RSH              , GenTreeOp          ,0,GTK_BINOP)
GTNODE(RSZ              , GenTreeOp          ,0,GTK_BINOP)
GTNODE(ROL              , GenTreeOp          ,0,GTK_BINOP)
GTNODE(ROR              , GenTreeOp          ,0,GTK_BINOP)

GTNODE(ASG              , GenTreeOp          ,0,GTK_BINOP|DBK_NOTLIR)
GTNODE(EQ               , GenTreeOp          ,0,GTK_BINOP)
GTNODE(NE               , GenTreeOp          ,0,GTK_BINOP)
GTNODE(LT               , GenTreeOp          ,0,GTK_BINOP)
GTNODE(LE               , GenTreeOp          ,0,GTK_BINOP)
GTNODE(GE               , GenTreeOp          ,0,GTK_BINOP)
GTNODE(GT               , GenTreeOp          ,0,GTK_BINOP)

// These implement EQ/NE(AND(x, y), 0). They always produce a value (GT_TEST is the version that does not).
GTNODE(TEST_EQ          , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR)
GTNODE(TEST_NE          , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR)

#ifdef TARGET_XARCH
// BITTEST_EQ/NE(a, n) == EQ/NE(AND(a, LSH(1, n)), 0), but only used in xarch that has the BT instruction
GTNODE(BITTEST_EQ       , GenTreeOp          ,0,(GTK_BINOP|DBK_NOTHIR))
GTNODE(BITTEST_NE       , GenTreeOp          ,0,(GTK_BINOP|DBK_NOTHIR))
#endif

// Conditional select with 3 operands: condition, true value, false value
GTNODE(SELECT           , GenTreeConditional ,0,GTK_SPECIAL)

GTNODE(COMMA            , GenTreeOp          ,0,GTK_BINOP|DBK_NOTLIR)
GTNODE(QMARK            , GenTreeQmark       ,0,GTK_BINOP|GTK_EXOP|DBK_NOTLIR)
GTNODE(COLON            , GenTreeColon       ,0,GTK_BINOP|DBK_NOTLIR)

GTNODE(INDEX_ADDR       , GenTreeIndexAddr   ,0,GTK_BINOP|GTK_EXOP)   // Address of SZ-array-element.
GTNODE(MKREFANY         , GenTreeOp          ,0,GTK_BINOP|DBK_NOTLIR)
GTNODE(LEA              , GenTreeAddrMode    ,0,GTK_BINOP|GTK_EXOP|DBK_NOTHIR)

#if !defined(TARGET_64BIT)
// A GT_LONG node simply represents the long value produced by the concatenation
// of its two (lower and upper half) operands.  Some GT_LONG nodes are transient,
// during the decomposing of longs; others are handled by codegen as operands of
// nodes such as calls, returns and stores of long lclVars.
GTNODE(LONG             , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR)

// The following are nodes representing x86/arm32 specific long operators, including
// high operators of a 64-bit operations that requires a carry/borrow, which are
// named GT_XXX_HI for consistency, low operators of 64-bit operations that need
// to not be modified in phases post-decompose, and operators that return 64-bit
// results in one instruction.
GTNODE(ADD_LO           , GenTreeOp          ,1,GTK_BINOP|DBK_NOTHIR)
GTNODE(ADD_HI           , GenTreeOp          ,1,GTK_BINOP|DBK_NOTHIR)
GTNODE(SUB_LO           , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR)
GTNODE(SUB_HI           , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR)

// The following are nodes that specify shifts that take a GT_LONG op1. The GT_LONG
// contains the hi and lo parts of three operand shift form where one op will be
// shifted into the other op as part of the operation (LSH_HI will shift
// the high bits of the lo operand into the high operand as it shifts left. RSH_LO
// will shift the lo bits of the high operand into the lo operand). LSH_HI
// represents the high operation of a 64-bit left shift by a constant int, and
// RSH_LO represents the lo operation of a 64-bit right shift by a constant int.
GTNODE(LSH_HI           , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR)
GTNODE(RSH_LO           , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR)

#endif // !defined(TARGET_64BIT)

#ifdef FEATURE_HW_INTRINSICS
GTNODE(HWINTRINSIC      , GenTreeHWIntrinsic ,0,GTK_SPECIAL)               // hardware intrinsics
#endif // FEATURE_HW_INTRINSICS

//-----------------------------------------------------------------------------
//  Backend-specific arithmetic nodes:
//-----------------------------------------------------------------------------

// Saturating increment, used in division by a constant (LowerUnsignedDivOrMod).
GTNODE(INC_SATURATE     , GenTreeOp          ,0,GTK_UNOP|DBK_NOTHIR)

// Returns high bits (top N bits of the 2N bit result of an NxN multiply)
// GT_MULHI is used in division by a constant (LowerUnsignedDivOrMod). We turn
// the div into a MULHI + some adjustments. In codegen, we only use the
// results of the high register, and we drop the low results.
GTNODE(MULHI            , GenTreeOp          ,1,GTK_BINOP|DBK_NOTHIR)

// A mul that returns the 2N bit result of an NxN multiply. This op is used for
// multiplies that take two ints and return a long result. For 32 bit targets,
// all other multiplies with long results are morphed into helper calls.
// It is similar to GT_MULHI, the difference being that GT_MULHI drops the lo
// part of the result, whereas GT_MUL_LONG keeps both parts of the result.
// MUL_LONG is also used on ARM64, where 64 bit multiplication is more expensive.
#if !defined(TARGET_64BIT)
GTNODE(MUL_LONG         , GenTreeMultiRegOp  ,1,GTK_BINOP|DBK_NOTHIR)
#elif defined(TARGET_ARM64)
GTNODE(MUL_LONG         , GenTreeOp          ,1,GTK_BINOP|DBK_NOTHIR)
#endif
// AndNot - emitted on ARM/ARM64 as the BIC instruction. Also used for creating AndNot HWINTRINSIC vector nodes in a cross-ISA manner.
GTNODE(AND_NOT          , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR)

#ifdef TARGET_ARM64
GTNODE(BFIZ             , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR) // Bitfield Insert in Zero.
GTNODE(CSNEG_MI         , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR) // Conditional select, negate, minus result
GTNODE(CNEG_LT          , GenTreeOp          ,0,GTK_UNOP|DBK_NOTHIR)  // Conditional, negate, signed less than result
#endif

//-----------------------------------------------------------------------------
//  LIR specific compare and conditional branch/set nodes:
//-----------------------------------------------------------------------------

// Sets the condition flags according to the compare result. N.B. Not a relop, it does not produce a value and it cannot be reversed.
GTNODE(CMP              , GenTreeOp          ,0,GTK_BINOP|GTK_NOVALUE|DBK_NOTHIR)
// Generate a test instruction; sets the CPU flags according to (a & b) and does not produce a value.
GTNODE(TEST             , GenTreeOp          ,0,GTK_BINOP|GTK_NOVALUE|DBK_NOTHIR)
#ifdef TARGET_XARCH
// The XARCH BT instruction. Like CMP, this sets the condition flags (CF to be precise) and does not produce a value.
GTNODE(BT               , GenTreeOp          ,0,(GTK_BINOP|GTK_NOVALUE|DBK_NOTHIR))
#endif
// Makes a comparison and jumps if the condition specified by gtCondition is true. Does not set flags.
GTNODE(JCMP             , GenTreeOpCC        ,0,GTK_BINOP|GTK_NOVALUE|DBK_NOTHIR)
// Do a bit test and jump if set/not set.
GTNODE(JTEST            , GenTreeOpCC        ,0,GTK_BINOP|GTK_NOVALUE|DBK_NOTHIR)
// Checks the condition flags and branch if the condition specified by GenTreeCC::gtCondition is true.
GTNODE(JCC              , GenTreeCC          ,0,GTK_LEAF|GTK_NOVALUE|DBK_NOTHIR)
// Checks the condition flags and produces 1 if the condition specified by GenTreeCC::gtCondition is true and 0 otherwise.
GTNODE(SETCC            , GenTreeCC          ,0,GTK_LEAF|DBK_NOTHIR)
// Variant of SELECT that reuses flags computed by a previous node with the specified condition.
GTNODE(SELECTCC         , GenTreeOpCC        ,0,GTK_BINOP|DBK_NOTHIR)
#ifdef TARGET_ARM64
// The arm64 ccmp instruction. If the specified condition is true, compares two
// operands and sets the condition flags according to the result. Otherwise
// sets the condition flags to the specified immediate value.
GTNODE(CCMP             , GenTreeCCMP        ,0,GTK_BINOP|GTK_NOVALUE|DBK_NOTHIR)
// Maps to arm64 cinc instruction. It returns the operand incremented by one when the condition is true.
// Otherwise returns the unchanged operand. Optimises for patterns such as, result = condition ? op1 + 1 : op1
GTNODE(CINC             , GenTreeOp          ,0,GTK_BINOP|DBK_NOTHIR)
// Variant of CINC that reuses flags computed by a previous node with the specified condition.
GTNODE(CINCCC           , GenTreeOpCC        ,0,GTK_UNOP|DBK_NOTHIR)
#endif

//-----------------------------------------------------------------------------
//  Other nodes that look like unary/binary operators:
//-----------------------------------------------------------------------------

GTNODE(JTRUE            , GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)

//-----------------------------------------------------------------------------
//  Other nodes that have special structure:
//-----------------------------------------------------------------------------

GTNODE(ARR_ELEM         , GenTreeArrElem     ,0,GTK_SPECIAL)            // Multi-dimensional array-element address
GTNODE(ARR_INDEX        , GenTreeArrIndex    ,0,GTK_BINOP|GTK_EXOP)     // Effective, bounds-checked index for one dimension of a multi-dimensional array element
GTNODE(ARR_OFFSET       , GenTreeArrOffs     ,0,GTK_SPECIAL)            // Flattened offset of multi-dimensional array element
GTNODE(CALL             , GenTreeCall        ,0,GTK_SPECIAL|DBK_NOCONTAIN)
GTNODE(FIELD_LIST       , GenTreeFieldList   ,0,GTK_SPECIAL)            // List of fields of a struct, when passed as an argument

GTNODE(RETURN           , GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)
GTNODE(SWITCH           , GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE)
GTNODE(NO_OP            , GenTree            ,0,GTK_LEAF|GTK_NOVALUE) // A NOP that cannot be deleted.

GTNODE(START_NONGC      , GenTree            ,0,GTK_LEAF|GTK_NOVALUE|DBK_NOTHIR) // Starts a new instruction group that will be non-gc interruptible.
GTNODE(START_PREEMPTGC  , GenTree            ,0,GTK_LEAF|GTK_NOVALUE|DBK_NOTHIR) // Starts a new instruction group where preemptive GC is enabled.
GTNODE(PROF_HOOK        , GenTree            ,0,GTK_LEAF|GTK_NOVALUE|DBK_NOTHIR) // Profiler Enter/Leave/TailCall hook.

GTNODE(RETFILT          , GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE) // End filter with TYP_I_IMPL return value.
#if !defined(FEATURE_EH_FUNCLETS)
GTNODE(END_LFIN         , GenTreeVal         ,0,GTK_LEAF|GTK_NOVALUE) // End locally-invoked finally.
#endif // !FEATURE_EH_FUNCLETS

//-----------------------------------------------------------------------------
//  Nodes used by Lower to generate a closer CPU representation of other nodes
//-----------------------------------------------------------------------------

GTNODE(JMPTABLE         , GenTree            ,0,GTK_LEAF|DBK_NOCONTAIN|DBK_NOTHIR) // Generates the jump table for switches
GTNODE(SWITCH_TABLE     , GenTreeOp          ,0,GTK_BINOP|GTK_NOVALUE|DBK_NOTHIR)  // Jump Table based switch construct

//-----------------------------------------------------------------------------
//  Nodes used only within the code generator:
//-----------------------------------------------------------------------------

GTNODE(CLS_VAR_ADDR     , GenTreeClsVar      ,0,GTK_LEAF|DBK_NOTHIR)              // static data member address
GTNODE(PHYSREG          , GenTreePhysReg     ,0,GTK_LEAF|DBK_NOTHIR)              // read from a physical register
GTNODE(EMITNOP          , GenTree            ,0,GTK_LEAF|GTK_NOVALUE|DBK_NOTHIR)  // emitter-placed nop
GTNODE(PINVOKE_PROLOG   , GenTree            ,0,GTK_LEAF|GTK_NOVALUE|DBK_NOTHIR)  // pinvoke prolog seq
GTNODE(PINVOKE_EPILOG   , GenTree            ,0,GTK_LEAF|GTK_NOVALUE|DBK_NOTHIR)  // pinvoke epilog seq
GTNODE(RETURNTRAP       , GenTreeOp          ,0,GTK_UNOP|GTK_NOVALUE|DBK_NOTHIR)  // a conditional call to wait on gc
#if defined(TARGET_ARM)
GTNODE(PUTARG_REG       , GenTreeMultiRegOp  ,0,GTK_UNOP|DBK_NOTHIR)              // operator that places outgoing arg in register
#else
GTNODE(PUTARG_REG       , GenTreeOp          ,0,GTK_UNOP|DBK_NOTHIR)              // operator that places outgoing arg in register
#endif
GTNODE(PUTARG_STK       , GenTreePutArgStk   ,0,GTK_UNOP|GTK_NOVALUE|DBK_NOTHIR)  // operator that places outgoing arg in stack
#if FEATURE_ARG_SPLIT
GTNODE(PUTARG_SPLIT     , GenTreePutArgSplit ,0,GTK_UNOP|DBK_NOTHIR)              // operator that places outgoing arg in registers with stack (split struct in ARM32)
#endif // FEATURE_ARG_SPLIT
GTNODE(SWAP             , GenTreeOp          ,0,GTK_BINOP|GTK_NOVALUE|DBK_NOTHIR) // op1 and op2 swap (registers)
GTNODE(COPY             , GenTreeCopyOrReload,0,GTK_UNOP|DBK_NOTHIR)              // Copies a variable from its current location to a register that satisfies
GTNODE(RELOAD           , GenTreeCopyOrReload,0,GTK_UNOP|DBK_NOTHIR)              // code generation constraints. The operand is the actual lclVar node.
GTNODE(IL_OFFSET        , GenTreeILOffset    ,0,GTK_LEAF|GTK_NOVALUE|DBK_NOTHIR)  // marks an IL offset for debugging purposes

/*****************************************************************************/
#undef  GTNODE
/*****************************************************************************/
// clang-format on
