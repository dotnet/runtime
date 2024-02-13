// Please keep this in sync with jiterpreter.ts:generate_wasm_body

#define VALUE_ABORT -1
// The heuristic will either evaluate this as VALUE_ABORT or VALUE_LOW
//  depending on whether we are currently inside a branch block.
// Use this for opcodes that will cause a bailout if hit.
#define VALUE_ABORT_OUTSIDE_BRANCH_BLOCK -2
// The same as the above except the opcode has no value
#define VALUE_ABORT_OUTSIDE_BRANCH_BLOCK_NONE -3
#define VALUE_NONE 0
#define VALUE_LOW 1
#define VALUE_BRANCH 1
// This is a normal value opcode that also starts a branch block
#define VALUE_BEGIN_BRANCH_BLOCK 2
#define VALUE_NORMAL 2
#define VALUE_HIGH 4
#define VALUE_MASSIVE 6
#define VALUE_SIMD 8

#define OP(OP, VAL) opcode_value_table[OP] = VALUE_ ## VAL;
#define OPRANGE(OP_MIN, OP_MAX, VAL) \
	for (int i = OP_MIN; i <= OP_MAX; i++) \
		opcode_value_table[i] = VALUE_ ## VAL;

//
// Put ranges first so we can override individual opcodes after
//
OPRANGE(MINT_BR, MINT_BR_S, BEGIN_BRANCH_BLOCK)
OPRANGE(MINT_CALL_HANDLER, MINT_CALL_HANDLER_S, BEGIN_BRANCH_BLOCK)
OPRANGE(MINT_BRFALSE_I4, MINT_BLT_UN_I8_IMM_SP, BEGIN_BRANCH_BLOCK)

OPRANGE(MINT_CALL, MINT_CALLI_NAT_FAST, ABORT_OUTSIDE_BRANCH_BLOCK_NONE)
OPRANGE(MINT_RET, MINT_RET_U2, ABORT_OUTSIDE_BRANCH_BLOCK_NONE)
OPRANGE(MINT_RET_I4_IMM, MINT_RET_I8_IMM, ABORT_OUTSIDE_BRANCH_BLOCK_NONE)

// High value because interp has to do a memory load for the immediate
//  but we can inline it into the trace
OPRANGE(MINT_LDC_I4_M1, MINT_LDC_R8, HIGH)

OPRANGE(MINT_MOV_I4_I1, MINT_MOV_4, NORMAL)
// High value for large/complex moves
OPRANGE(MINT_MOV_8, MINT_MOV_8_4, HIGH)

// Binops. Assume most of them are not any faster in jiterp
OPRANGE(MINT_ADD_I4, MINT_CLT_UN_R8, NORMAL)
// Unops and some superinsns. Most will not be faster in jiterp.
OPRANGE(MINT_ADD1_I4, MINT_SHR_I8_IMM, NORMAL)
// Math intrinsics. We implement most of these by calling libc or using wasm opcodes
OPRANGE(MINT_ASIN, MINT_MAXF, NORMAL)
// Field operations. Null check optimization makes these more efficient than interp
OPRANGE(MINT_LDFLD_I1, MINT_LDTSFLDA, HIGH)
// Indirect operations. Some of these are complex or more efficient than interp
OPRANGE(MINT_LDLOCA_S, MINT_STIND_OFFSET_IMM_I8, HIGH)
// Array operations. These can be more efficient due to null check optimization
OPRANGE(MINT_LDELEM_I1, MINT_GETITEM_LOCALSPAN, HIGH)
// Simd operations have an artificially massive value because generating native wasm
//  simd opcodes will be significantly faster than using interp intrinsics, since
//  interp intrinsics perform an indirect call
OPRANGE(MINT_SIMD_V128_LDC, MINT_SIMD_INTRINS_P_PPP, SIMD)

//
// Individual opcodes. Some of these may override ranges
//
OP(MINT_TIER_ENTER_METHOD, NONE)
OP(MINT_TIER_PATCHPOINT, NONE)
OP(MINT_TIER_PREPARE_JITERPRETER, NONE)
OP(MINT_TIER_NOP_JITERPRETER, NONE)
OP(MINT_TIER_MONITOR_JITERPRETER, NONE)
OP(MINT_TIER_ENTER_JITERPRETER, NONE)
OP(MINT_NOP, NONE)
OP(MINT_DEF, NONE)
OP(MINT_DUMMY_USE, NONE)
OP(MINT_IL_SEQ_POINT, NONE)
OP(MINT_TIER_PATCHPOINT_DATA, NONE)
OP(MINT_MONO_MEMORY_BARRIER, NONE)
// We need to make sure traces abort for breakpoints.
// OP(MINT_SDB_BREAKPOINT, NONE)
OP(MINT_SDB_INTR_LOC, NONE)
OP(MINT_SDB_SEQ_POINT, NONE)

// These are only generated inside catch clauses, so it's safe to assume that
//  during normal execution they won't run, and compile them as a bailout.
OP(MINT_LEAVE_CHECK, NONE)
OP(MINT_LEAVE_S_CHECK, NONE)

OP(MINT_CKNULL, NORMAL)
OP(MINT_LDLOCA_S, NORMAL)
OP(MINT_LDSTR, NORMAL)
OP(MINT_LDFTN, NORMAL)
OP(MINT_LDFTN_ADDR, NORMAL)
OP(MINT_LDPTR, NORMAL)
OP(MINT_STRLEN, NORMAL)
OP(MINT_BOX, NORMAL)
OP(MINT_BOX_VT, NORMAL)
OP(MINT_UNBOX, NORMAL)
OP(MINT_NEWSTR, NORMAL)
OP(MINT_LD_DELEGATE_METHOD_PTR, NORMAL)
OP(MINT_LDTSFLDA, NORMAL)
OP(MINT_ADD_MUL_I4_IMM, NORMAL)
OP(MINT_ADD_MUL_I8_IMM, NORMAL)
OP(MINT_ARRAY_RANK, NORMAL)
OP(MINT_ARRAY_ELEMENT_SIZE, NORMAL)

// We inline the safepoint flag check into the trace which is an improvement
//  over dispatching an interp opcode to do nothing (theoretically)
OP(MINT_SAFEPOINT, HIGH)

// These opcodes are classified as high value because the jiterpreter is able to
//  perform the operation more efficiently than the interpreter can, for example
//  by inlining data_items/immediates as constants, exploiting the memory model,
//  optimizing out null checks, or unrolling memset/memcpy loops
// Some of the _VT and _VT_NOREF opcodes are unrolled raw memcpys in addition
//  to having their types or sizes embedded into the trace as constants
OP(MINT_CPOBJ_VT, HIGH)
OP(MINT_LDOBJ_VT, MASSIVE)
OP(MINT_STOBJ_VT, HIGH)
OP(MINT_STOBJ_VT_NOREF, MASSIVE)
OP(MINT_CPOBJ_VT_NOREF, MASSIVE)
OP(MINT_MOV_VT, MASSIVE)
OP(MINT_LDFLD_VT, MASSIVE)
OP(MINT_LDSFLD_VT, MASSIVE)
OP(MINT_STFLD_VT_NOREF, MASSIVE)
OP(MINT_LDELEM_VT, MASSIVE)
OP(MINT_GETCHR, HIGH)
OP(MINT_GETITEM_SPAN, HIGH)
OP(MINT_GETITEM_LOCALSPAN, HIGH)
OP(MINT_INTRINS_SPAN_CTOR, HIGH)
OP(MINT_INTRINS_GET_TYPE, HIGH)
OP(MINT_INTRINS_MEMORYMARSHAL_GETARRAYDATAREF, HIGH)
OP(MINT_INITLOCAL, MASSIVE)
OP(MINT_INITLOCALS, MASSIVE)
OP(MINT_LOCALLOC, NORMAL)
OP(MINT_INITOBJ, MASSIVE)
OP(MINT_INTRINS_RUNTIMEHELPERS_OBJECT_HAS_COMPONENT_SIZE, HIGH)
OP(MINT_INTRINS_ENUM_HASFLAG, HIGH)
OP(MINT_INTRINS_ORDINAL_IGNORE_CASE_ASCII, HIGH)
OP(MINT_NEWOBJ_INLINED, HIGH)
OP(MINT_CPBLK, HIGH)
OP(MINT_INITBLK, HIGH)
OP(MINT_ROL_I4_IMM, HIGH)
OP(MINT_ROL_I8_IMM, HIGH)
OP(MINT_ROR_I4_IMM, HIGH)
OP(MINT_ROR_I8_IMM, HIGH)

// These opcodes are classified as high value because they are so complex that
//  any trace containing them will be more likely to be long enough to
//  outweigh trace transition overhead, even though they are not more
//  efficient when generated as wasm. Some of them do call C helpers
OP(MINT_CASTCLASS, HIGH)
OP(MINT_CASTCLASS_COMMON, HIGH)
OP(MINT_CASTCLASS_INTERFACE, HIGH)
OP(MINT_ISINST, HIGH)
OP(MINT_ISINST_COMMON, HIGH)
OP(MINT_ISINST_INTERFACE, HIGH)
OP(MINT_INTRINS_GET_HASHCODE, HIGH)
OP(MINT_INTRINS_TRY_GET_HASHCODE, HIGH)
OP(MINT_MONO_CMPXCHG_I4, HIGH)
OP(MINT_MONO_CMPXCHG_I8, HIGH)
OP(MINT_CLZ_I4, HIGH)
OP(MINT_CTZ_I4, HIGH)
OP(MINT_POPCNT_I4, HIGH)
OP(MINT_LOG2_I4, HIGH)
OP(MINT_CLZ_I8, HIGH)
OP(MINT_CTZ_I8, HIGH)
OP(MINT_POPCNT_I8, HIGH)
OP(MINT_LOG2_I8, HIGH)
OP(MINT_SHL_AND_I4, HIGH)
OP(MINT_SHL_AND_I8, HIGH)

// Produces either a backwards branch or a bailout depending on JIT-time
//  information, so treat it as a low value branch
OP(MINT_ENDFINALLY, BRANCH)

OP(MINT_MONO_RETHROW, ABORT_OUTSIDE_BRANCH_BLOCK_NONE)
OP(MINT_THROW, ABORT_OUTSIDE_BRANCH_BLOCK_NONE)

// These opcodes will turn into supported MOVs later
OP(MINT_MOV_SRC_OFF, NORMAL)
OP(MINT_MOV_DST_OFF, NORMAL)

// FIXME: Not implemented individual opcodes
OP(MINT_CONV_U4_R8, ABORT)
