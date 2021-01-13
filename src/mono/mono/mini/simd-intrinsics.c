/**
 * \file
 * simd support for intrinsics
 *
 * Author:
 *   Rodrigo Kumpera (rkumpera@novell.com)
 *
 * (C) 2008 Novell, Inc.
 */

#include <config.h>
#include <stdio.h>

#include "mini.h"
#include "ir-emit.h"
#include <mono/metadata/abi-details.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/bsearch.h>

/*
General notes on SIMD intrinsics

TODO handle operands with non SIMD args, such as op_Addition (Vector4f, float)
TODO optimize r4const in .ctor so it doesn't go into the FP stack first
TODO extend op_to_op_dest_membase to handle simd ops
TODO add support for indexed versions of simd ops
TODO to an amd64 port and figure out how to properly handle extractors/.ctor
TODO make sure locals, arguments and spills are properly aligned.
TODO add support for fusing a XMOVE into a simd op in mono_spill_global_vars.
TODO add stuff to man pages
TODO document this under /docs
TODO make passing a xmm as argument not cause it to be LDADDR'ed (introduce an OP_XPUSH)
TODO revamp the .ctor sequence as it looks very fragile, maybe use a var just like move_i4_to_f. (or just pinst sse ops) 
TODO figure out what's wrong with OP_STOREX_MEMBASE_REG and OP_STOREX_MEMBASE (the 2nd is for imm operands)
TODO maybe add SSE3 emulation on top of SSE2, or just implement the corresponding functions using SSE2 intrinsics.
TODO pass simd arguments in registers or, at least, add SSE support for pushing large (>=16) valuetypes 
TODO pass simd args byval to a non-intrinsic method cause some useless local var load/store to happen.
TODO check if we need to init the SSE control word with better precision.
TODO add support for 3 reg sources in mini without slowing the common path. Or find a way to make MASKMOVDQU work.
TODO make SimdRuntime.get_AccelMode work under AOT
TODO patterns such as "a ^= b" generate slower code as the LDADDR op will be copied to a tmp first. Look at adding a indirection reduction pass after the dce pass.
TODO extend bounds checking code to support for range checking.  

General notes for SIMD intrinsics.

-Bad extractor and constructor performance
Extracting a float from a XMM is a complete disaster if you are passing it as an argument.
It will be loaded in the FP stack just to be pushed on the call stack.

A similar thing happens with Vector4f constructor that require float vars to be 

The fix for this issue is similar to the one required for r4const as method args. Avoiding the
trip to the FP stack is desirable.

-Extractor and constructor code doesn't make sense under amd64. Both currently assume separate banks
for simd and fp.


-Promote OP_EXTRACT_I4 to a STORE op
The advantage of this change is that it could have a _membase version and promote further optimizations.

-Create a MONO_INST_DONT_REGALLOC and use it in all places that MONO_INST_INDIRECT is used
without a OP_LDADDR.
*/

#if defined (MONO_ARCH_SIMD_INTRINSICS) && !defined(ENABLE_NETCORE)

#if defined (DISABLE_JIT)

void
mono_simd_intrinsics_init (void)
{
}

#else

//#define IS_DEBUG_ON(cfg) (0)

#define IS_DEBUG_ON(cfg) ((cfg)->verbose_level >= 3)
#define DEBUG(a) do { if (IS_DEBUG_ON(cfg)) { a; } } while (0)

/*
This enum MUST be kept in sync with its managed mirror Mono.Simd.AccelMode.
 */
enum {
	SIMD_VERSION_SSE1	= 1 << 0,
	SIMD_VERSION_SSE2	= 1 << 1,
	SIMD_VERSION_SSE3	= 1 << 2,
	SIMD_VERSION_SSSE3	= 1 << 3,
	SIMD_VERSION_SSE41	= 1 << 4,
	SIMD_VERSION_SSE42	= 1 << 5,
	SIMD_VERSION_SSE4a	= 1 << 6,
};

enum {
	SIMD_EMIT_BINARY,
	SIMD_EMIT_UNARY,
	SIMD_EMIT_SETTER,
	SIMD_EMIT_GETTER,
	SIMD_EMIT_GETTER_QWORD,
	SIMD_EMIT_CTOR,
	SIMD_EMIT_CAST,
	SIMD_EMIT_SHUFFLE,
	SIMD_EMIT_SHIFT,
	SIMD_EMIT_EQUALITY,
	SIMD_EMIT_LOAD_ALIGNED,
	SIMD_EMIT_STORE,
	SIMD_EMIT_EXTRACT_MASK,
	SIMD_EMIT_PREFETCH
};

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define SIMD_METHOD(str,name) char MSGSTRFIELD(__LINE__) [sizeof (str)];
#include "simd-methods.h"
#undef SIMD_METHOD
} method_names = {
#define SIMD_METHOD(str,name) str,
#include "simd-methods.h"
#undef SIMD_METHOD
};

enum {
#define SIMD_METHOD(str,name) name = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "simd-methods.h"
};
#define method_name(idx) ((const char*)&method_names + (idx))

typedef struct {
	guint16 name;
	guint16 opcode;
	guint32 simd_version;
	guint8 simd_emit_mode : 4;
	guint8 flags : 4;
} SimdIntrinsic;

static const SimdIntrinsic vector4f_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_R4, MONO_CPU_X86_SSE, SIMD_EMIT_CTOR },
	{ SN_AddSub, OP_ADDSUBPS, MONO_CPU_X86_SSE3, SIMD_EMIT_BINARY},
	{ SN_AndNot, OP_ANDNPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY},
	{ SN_CompareEqual, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_EQ },
	{ SN_CompareLessEqual, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_LE },
	{ SN_CompareLessThan, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_LT },
	{ SN_CompareNotEqual, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_NEQ },
	{ SN_CompareNotLessEqual, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_NLE },
	{ SN_CompareNotLessThan, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_NLT },
	{ SN_CompareOrdered, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_ORD },
	{ SN_CompareUnordered, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_UNORD },
	{ SN_ConvertToDouble, OP_CVTPS2PD, MONO_CPU_X86_SSE2, SIMD_EMIT_UNARY },
	{ SN_ConvertToInt, OP_CVTPS2DQ, MONO_CPU_X86_SSE2, SIMD_EMIT_UNARY },
	{ SN_ConvertToIntTruncated, OP_CVTTPS2DQ, MONO_CPU_X86_SSE2, SIMD_EMIT_UNARY },
	{ SN_DuplicateHigh, OP_DUPPS_HIGH, MONO_CPU_X86_SSE3, SIMD_EMIT_UNARY },
	{ SN_DuplicateLow, OP_DUPPS_LOW, MONO_CPU_X86_SSE3, SIMD_EMIT_UNARY },
	{ SN_HorizontalAdd, OP_HADDPS, MONO_CPU_X86_SSE3, SIMD_EMIT_BINARY },
	{ SN_HorizontalSub, OP_HSUBPS, MONO_CPU_X86_SSE3, SIMD_EMIT_BINARY },	
	{ SN_InterleaveHigh, OP_UNPACK_HIGHPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_InterleaveLow, OP_UNPACK_LOWPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_InvSqrt, OP_RSQRTPS, MONO_CPU_X86_SSE, SIMD_EMIT_UNARY },
	{ SN_LoadAligned, 0, MONO_CPU_X86_SSE, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_MAXPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_Min, OP_MINPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Reciprocal, OP_RCPPS, MONO_CPU_X86_SSE, SIMD_EMIT_UNARY },
	{ SN_Shuffle, OP_PSHUFLED, MONO_CPU_X86_SSE, SIMD_EMIT_SHUFFLE },
	{ SN_Sqrt, OP_SQRTPS, MONO_CPU_X86_SSE, SIMD_EMIT_UNARY },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_StoreNonTemporal, OP_STOREX_NTA_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_get_W, 3, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_Z, 2, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_ADDPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_ANDPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_ORPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Division, OP_DIVPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_XORPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, MONO_CPU_X86_SSE, SIMD_EMIT_CAST }, 
	{ SN_op_Inequality, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_Multiply, OP_MULPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Subtraction, OP_SUBPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_set_W, 3, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_Z, 2, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER }
};

static const SimdIntrinsic vector2d_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_R8, MONO_CPU_X86_SSE, SIMD_EMIT_CTOR },
	{ SN_AddSub, OP_ADDSUBPD, MONO_CPU_X86_SSE3, SIMD_EMIT_BINARY,},
	{ SN_AndNot, OP_ANDNPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_COMPPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_EQ },
	{ SN_CompareLessEqual, OP_COMPPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_LE },
	{ SN_CompareLessThan, OP_COMPPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_LT },
	{ SN_CompareNotEqual, OP_COMPPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_NEQ },
	{ SN_CompareNotLessEqual, OP_COMPPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_NLE },
	{ SN_CompareNotLessThan, OP_COMPPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_NLT },
	{ SN_CompareOrdered, OP_COMPPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_ORD },
	{ SN_CompareUnordered, OP_COMPPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, SIMD_COMP_UNORD },
	{ SN_ConvertToFloat, OP_CVTPD2PS, MONO_CPU_X86_SSE2, SIMD_EMIT_UNARY },
	{ SN_ConvertToInt, OP_CVTPD2DQ, MONO_CPU_X86_SSE2, SIMD_EMIT_UNARY },
	{ SN_ConvertToIntTruncated, OP_CVTTPD2DQ, MONO_CPU_X86_SSE2, SIMD_EMIT_UNARY },
	{ SN_Duplicate, OP_DUPPD, MONO_CPU_X86_SSE3, SIMD_EMIT_UNARY },
	{ SN_HorizontalAdd, OP_HADDPD, MONO_CPU_X86_SSE3, SIMD_EMIT_BINARY },
	{ SN_HorizontalSub, OP_HSUBPD, MONO_CPU_X86_SSE3, SIMD_EMIT_BINARY },	
	{ SN_InterleaveHigh, OP_UNPACK_HIGHPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_InterleaveLow, OP_UNPACK_LOWPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, MONO_CPU_X86_SSE, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_MAXPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_Min, OP_MINPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Shuffle, OP_SHUFPD, MONO_CPU_X86_SSE, SIMD_EMIT_SHUFFLE },
	{ SN_Sqrt, OP_SQRTPD, MONO_CPU_X86_SSE, SIMD_EMIT_UNARY },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_get_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER_QWORD },
	{ SN_get_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER_QWORD },
	{ SN_op_Addition, OP_ADDPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_ANDPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_ORPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Division, OP_DIVPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_XORPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, MONO_CPU_X86_SSE, SIMD_EMIT_CAST }, 
	{ SN_op_Multiply, OP_MULPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Subtraction, OP_SUBPD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_set_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
};

static const SimdIntrinsic vector2ul_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I8, MONO_CPU_X86_SSE, SIMD_EMIT_CTOR },
	{ SN_CompareEqual, OP_PCMPEQQ, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, MONO_CPU_X86_SSE, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_PrefetchTemporalAllCacheLevels, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Shuffle, OP_SHUFPD, MONO_CPU_X86_SSE, SIMD_EMIT_SHUFFLE },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHQ, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWQ, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_get_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER_QWORD },
	{ SN_get_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER_QWORD },
	{ SN_op_Addition, OP_PADDQ, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_EMIT_BINARY, MONO_CPU_X86_SSE },
	{ SN_op_Explicit, 0, MONO_CPU_X86_SSE, SIMD_EMIT_CAST },
	{ SN_op_LeftShift, OP_PSHLQ, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULQ, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSHRQ, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBQ, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_set_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
};

static const SimdIntrinsic vector2l_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I8, MONO_CPU_X86_SSE, SIMD_EMIT_CTOR },
	{ SN_CompareEqual, OP_PCMPEQQ, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTQ, MONO_CPU_X86_SSE42, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, MONO_CPU_X86_SSE, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_LogicalRightShift, OP_PSHRQ, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_PrefetchTemporalAllCacheLevels, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Shuffle, OP_SHUFPD, MONO_CPU_X86_SSE, SIMD_EMIT_SHUFFLE },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHQ, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWQ, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_get_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER_QWORD },
	{ SN_get_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER_QWORD },
	{ SN_op_Addition, OP_PADDQ, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, MONO_CPU_X86_SSE, SIMD_EMIT_CAST },
	{ SN_op_LeftShift, OP_PSHLQ, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULQ, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Subtraction, OP_PSUBQ, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_set_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
};

static const SimdIntrinsic vector4ui_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I4, MONO_CPU_X86_SSE, SIMD_EMIT_CTOR },
	{ SN_ArithmeticRightShift, OP_PSARD, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_CompareEqual, OP_PCMPEQD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, MONO_CPU_X86_SSE, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXD_UN, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMIND_UN, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Shuffle, OP_PSHUFLED, MONO_CPU_X86_SSE, SIMD_EMIT_SHUFFLE },
	{ SN_SignedPackWithSignedSaturation, OP_PACKD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_SignedPackWithUnsignedSaturation, OP_PACKD_UN, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_get_W, 3, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_Z, 2, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQD, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, MONO_CPU_X86_SSE, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQD, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_LeftShift, OP_PSHLD, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULD, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSHRD, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_set_W, 3, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_Z, 2, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
};

static const SimdIntrinsic vector4i_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I4, MONO_CPU_X86_SSE, SIMD_EMIT_CTOR },
	{ SN_CompareEqual, OP_PCMPEQD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_ConvertToDouble, OP_CVTDQ2PD, MONO_CPU_X86_SSE2, SIMD_EMIT_UNARY },
	{ SN_ConvertToFloat, OP_CVTDQ2PS, MONO_CPU_X86_SSE2, SIMD_EMIT_UNARY },
	{ SN_LoadAligned, 0, MONO_CPU_X86_SSE, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_LogicalRightShift, OP_PSHRD, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_Max, OP_PMAXD, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMIND, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_PackWithSignedSaturation, OP_PACKD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_PackWithUnsignedSaturation, OP_PACKD_UN, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Shuffle, OP_PSHUFLED, MONO_CPU_X86_SSE, SIMD_EMIT_SHUFFLE },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_get_W, 3, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_Z, 2, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQD, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, MONO_CPU_X86_SSE, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQD, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_LeftShift, OP_PSHLD, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULD, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSARD, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBD, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_set_W, 3, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_X, 0, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_Z, 2, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
};

static const SimdIntrinsic vector8us_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I2, MONO_CPU_X86_SSE, SIMD_EMIT_CTOR },
	{ SN_AddWithSaturation, OP_PADDW_SAT_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_ArithmeticRightShift, OP_PSARW, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_Average, OP_PAVGW_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY, MONO_CPU_X86_SSE },
	{ SN_LoadAligned, 0, MONO_CPU_X86_SSE, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXW_UN, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMINW_UN, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_MultiplyStoreHigh, OP_PMULW_HIGH_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_ShuffleHigh, OP_PSHUFLEW_HIGH, MONO_CPU_X86_SSE, SIMD_EMIT_SHUFFLE },
	{ SN_ShuffleLow, OP_PSHUFLEW_LOW, MONO_CPU_X86_SSE, SIMD_EMIT_SHUFFLE },
	{ SN_SignedPackWithSignedSaturation, OP_PACKW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_SignedPackWithUnsignedSaturation, OP_PACKW_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_SubtractWithSaturation, OP_PSUBW_SAT_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_get_V0, 0, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V1, 1, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V2, 2, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V3, 3, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V4, 4, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V5, 5, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V6, 6, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V7, 7, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQW, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, MONO_CPU_X86_SSE, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQW, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_LeftShift, OP_PSHLW, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSHRW, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_set_V0, 0, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V1, 1, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V2, 2, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V3, 3, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V4, 4, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V5, 5, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V6, 6, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V7, 7, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
};

static const SimdIntrinsic vector8s_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I2, MONO_CPU_X86_SSE, SIMD_EMIT_CTOR },
	{ SN_AddWithSaturation, OP_PADDW_SAT, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, MONO_CPU_X86_SSE, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_LogicalRightShift, OP_PSHRW, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_Max, OP_PMAXW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMINW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_MultiplyStoreHigh, OP_PMULW_HIGH, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_PackWithSignedSaturation, OP_PACKW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_PackWithUnsignedSaturation, OP_PACKW_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_ShuffleHigh, OP_PSHUFLEW_HIGH, MONO_CPU_X86_SSE, SIMD_EMIT_SHUFFLE },
	{ SN_ShuffleLow, OP_PSHUFLEW_LOW, MONO_CPU_X86_SSE, SIMD_EMIT_SHUFFLE },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_SubtractWithSaturation, OP_PSUBW_SAT_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_get_V0, 0, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V1, 1, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V2, 2, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V3, 3, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V4, 4, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V5, 5, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V6, 6, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V7, 7, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQW, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, MONO_CPU_X86_SSE, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQW, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_LeftShift, OP_PSHLW, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSARW, MONO_CPU_X86_SSE, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBW, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_set_V0, 0, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V1, 1, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V2, 2, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V3, 3, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V4, 4, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V5, 5, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V6, 6, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V7, 7, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
};

static const SimdIntrinsic vector16b_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I1, MONO_CPU_X86_SSE, SIMD_EMIT_CTOR },
	{ SN_AddWithSaturation, OP_PADDB_SAT_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_Average, OP_PAVGB_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_ExtractByteMask, 0, MONO_CPU_X86_SSE, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, MONO_CPU_X86_SSE, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXB_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMINB_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_SubtractWithSaturation, OP_PSUBB_SAT_UN, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_SumOfAbsoluteDifferences, OP_PSUM_ABS_DIFF, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_get_V0, 0, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V1, 1, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V10, 10, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V11, 11, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V12, 12, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V13, 13, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V14, 14, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V15, 15, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V2, 2, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V3, 3, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V4, 4, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V5, 5, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V6, 6, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V7, 7, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V8, 8, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V9, 9, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQB, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, MONO_CPU_X86_SSE, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQB, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_Subtraction, OP_PSUBB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_set_V0, 0, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V1, 1, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V10, 10, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V11, 11, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V12, 12, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V13, 13, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V14, 14, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V15, 15, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V2, 2, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V3, 3, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V4, 4, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V5, 5, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V6, 6, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V7, 7, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V8, 8, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V9, 9, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
};

/*
Missing:
setters
 */
static const SimdIntrinsic vector16sb_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I1, MONO_CPU_X86_SSE, SIMD_EMIT_CTOR },
	{ SN_AddWithSaturation, OP_PADDB_SAT, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_ExtractByteMask, 0, MONO_CPU_X86_SSE, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, MONO_CPU_X86_SSE, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXB, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMINB, MONO_CPU_X86_SSE41, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, MONO_CPU_X86_SSE, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, MONO_CPU_X86_SSE, SIMD_EMIT_STORE },
	{ SN_SubtractWithSaturation, OP_PSUBB_SAT, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_get_V0, 0, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V1, 1, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V10, 10, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V11, 11, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V12, 12, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V13, 13, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V14, 14, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V15, 15, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V2, 2, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V3, 3, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V4, 4, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V5, 5, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V6, 6, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V7, 7, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V8, 8, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_get_V9, 9, MONO_CPU_X86_SSE, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQB, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, MONO_CPU_X86_SSE, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQB, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_Subtraction, OP_PSUBB, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_set_V0, 0, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V1, 1, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V10, 10, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V11, 11, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V12, 12, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V13, 13, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V14, 14, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V15, 15, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V2, 2, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V3, 3, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V4, 4, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V5, 5, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V6, 6, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V7, 7, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V8, 8, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
	{ SN_set_V9, 9, MONO_CPU_X86_SSE, SIMD_EMIT_SETTER },
};

static MonoInst* emit_sys_numerics_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args);
static MonoInst* emit_sys_numerics_vectors_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args);

/*TODO match using number of parameters as well*/
static int
simd_intrinsic_compare_by_name (const void *key, const void *value)
{
	return strcmp ((const char*)key, method_name (((SimdIntrinsic *)value)->name));
}

typedef enum {
	VREG_USED  				= 0x01,
	VREG_HAS_XZERO_BB0		= 0x02,
	VREG_HAS_OTHER_OP_BB0	= 0x04,
	VREG_SINGLE_BB_USE		= 0x08,
	VREG_MANY_BB_USE		= 0x10,
} KillFlags;

void
mono_simd_intrinsics_init (void)
{
}

static gboolean
apply_vreg_first_block_interference (MonoCompile *cfg, MonoInst *ins, int reg, int max_vreg, char *vreg_flags)
{
	if (reg != -1 && reg <= max_vreg && vreg_flags [reg]) {
		vreg_flags [reg] &= ~VREG_HAS_XZERO_BB0;
		vreg_flags [reg] |= VREG_HAS_OTHER_OP_BB0;
		DEBUG (printf ("[simd-simplify] R%d used: ", reg); mono_print_ins(ins));
		return TRUE;
	}
	return FALSE;
}

static gboolean
apply_vreg_following_block_interference (MonoCompile *cfg, MonoInst *ins, int reg, MonoBasicBlock *bb, int max_vreg, char *vreg_flags, MonoBasicBlock **target_bb)
{
	if (reg == -1 || reg > max_vreg || !(vreg_flags [reg] & VREG_HAS_XZERO_BB0) || target_bb [reg] == bb)
		return FALSE;

	if (vreg_flags [reg] & VREG_SINGLE_BB_USE) {
		vreg_flags [reg] &= ~VREG_SINGLE_BB_USE;
		vreg_flags [reg] |= VREG_MANY_BB_USE;
		DEBUG (printf ("[simd-simplify] R%d used by many bb: ", reg); mono_print_ins(ins));
		return TRUE;
	} else if (!(vreg_flags [reg] & VREG_MANY_BB_USE)) {
		vreg_flags [reg] |= VREG_SINGLE_BB_USE;
		target_bb [reg] = bb;
		DEBUG (printf ("[simd-simplify] R%d first used by: ", reg); mono_print_ins(ins));
		return TRUE;
	}
	return FALSE;
}

/*
This pass recalculate which vars need MONO_INST_INDIRECT.

We cannot do this for non SIMD vars since code like mono_get_vtable_var
uses MONO_INST_INDIRECT to signal that the variable must be stack allocated.
*/
void
mono_simd_simplify_indirection (MonoCompile *cfg)
{
	int i, max_vreg = 0;
	MonoBasicBlock *bb, *first_bb = NULL, **target_bb;
	MonoInst *ins;
	char *vreg_flags;

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *var = cfg->varinfo [i];
		if (m_class_is_simd_type (var->klass)) {
			var->flags &= ~MONO_INST_INDIRECT;
			max_vreg = MAX (var->dreg, max_vreg);
		}
	}

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (!first_bb && bb->code)
			first_bb = bb;
		for (ins = bb->code; ins; ins = ins->next) {
			if (ins->opcode == OP_LDADDR) {
				MonoInst *var = (MonoInst*)ins->inst_p0;
				if (m_class_is_simd_type (var->klass)) {
					var->flags |= MONO_INST_INDIRECT;
				}
			}
		}
	}

	DEBUG (printf ("[simd-simplify] max vreg is %d\n", max_vreg));
	vreg_flags = (char *)g_malloc0 (max_vreg + 1);
	target_bb = g_new0 (MonoBasicBlock*, max_vreg + 1);

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *var = cfg->varinfo [i];
		if (m_class_is_simd_type (var->klass) && !(var->flags & (MONO_INST_INDIRECT|MONO_INST_VOLATILE))) {
			vreg_flags [var->dreg] = VREG_USED;
			DEBUG (printf ("[simd-simplify] processing var %d with vreg %d\n", i, var->dreg));
		}
	}

	/*Scan the first basic block looking xzeros not used*/
	for (ins = first_bb->code; ins; ins = ins->next) {
		int num_sregs;
		int sregs [MONO_MAX_SRC_REGS];

		if (ins->opcode == OP_XZERO) {
			if (!(vreg_flags [ins->dreg] & VREG_HAS_OTHER_OP_BB0)) {
				DEBUG (printf ("[simd-simplify] R%d has vzero: ", ins->dreg); mono_print_ins(ins));
				vreg_flags [ins->dreg] |= VREG_HAS_XZERO_BB0;
			}
			continue;
		}
		if (ins->opcode == OP_LDADDR && apply_vreg_first_block_interference (cfg, ins, ((MonoInst*)ins->inst_p0)->dreg, max_vreg, vreg_flags))
			continue;
		if (apply_vreg_first_block_interference (cfg, ins, ins->dreg, max_vreg, vreg_flags))
			continue;
		num_sregs = mono_inst_get_src_registers (ins, sregs);
		for (i = 0; i < num_sregs; ++i) {
			if (apply_vreg_first_block_interference (cfg, ins, sregs [i], max_vreg, vreg_flags))
				break;
		}
	}

	if (IS_DEBUG_ON (cfg)) {
		for (i = 0; i < cfg->num_varinfo; i++) {
			MonoInst *var = cfg->varinfo [i];
			if (m_class_is_simd_type (var->klass)) {
				if ((vreg_flags [var->dreg] & VREG_HAS_XZERO_BB0))
					DEBUG (printf ("[simd-simplify] R%d has xzero only\n", var->dreg));
				if ((vreg_flags [var->dreg] & VREG_HAS_OTHER_OP_BB0))
					DEBUG (printf ("[simd-simplify] R%d has other ops on bb0\n", var->dreg));
			}
		}
	}

	/*TODO stop here if no var is xzero only*/

	/*
	Scan all other bb and check if it has only one other use
	Ideally this would be done after an extended bb formation pass

	FIXME This pass could use dominator information to properly
	place the XZERO on the bb that dominates all uses of the var,
	but this will have zero effect with the current local reg alloc 
	
	TODO simply the use of flags.
	*/

	for (bb = first_bb->next_bb; bb; bb = bb->next_bb) {
		for (ins = bb->code; ins; ins = ins->next) {
			int num_sregs;
			int sregs [MONO_MAX_SRC_REGS];

			if (ins->opcode == OP_LDADDR && apply_vreg_following_block_interference (cfg, ins, ((MonoInst*)ins->inst_p0)->dreg, bb, max_vreg, vreg_flags, target_bb))
				continue;
			if (apply_vreg_following_block_interference (cfg, ins, ins->dreg, bb, max_vreg, vreg_flags, target_bb))
				continue;
			num_sregs = mono_inst_get_src_registers (ins, sregs);
			for (i = 0; i < num_sregs; ++i) {
				if (apply_vreg_following_block_interference (cfg, ins, sregs [i], bb,
						max_vreg, vreg_flags, target_bb))
					continue;
			}
		}
	}

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *var = cfg->varinfo [i];
		if (!m_class_is_simd_type (var->klass))
			continue;
		if ((vreg_flags [var->dreg] & VREG_SINGLE_BB_USE))
			DEBUG (printf ("[simd-simplify] R%d has single bb use\n", var->dreg));
		if ((vreg_flags [var->dreg] & VREG_MANY_BB_USE))
			DEBUG (printf ("[simd-simplify] R%d has many bb in use\n", var->dreg));

		if (!(vreg_flags [var->dreg] & VREG_SINGLE_BB_USE))
			continue;
		for (ins = target_bb [var->dreg]->code; ins; ins = ins->next) {
			int num_sregs, j;
			int sregs [MONO_MAX_SRC_REGS];
			gboolean found = FALSE;

			num_sregs = mono_inst_get_src_registers (ins, sregs);
			for (j = 0; j < num_sregs; ++j) {
				if (sregs [j] == var->dreg)
					found = TRUE;
			}
			/*We can avoid inserting the XZERO if the first use doesn't depend on the zero'ed value.*/
			if (ins->dreg == var->dreg && !found) {
				DEBUG (printf ("[simd-simplify] INGORING R%d on BB %d because first op is a def", i, target_bb [var->dreg]->block_num););
				break;
			} else if (found) {
				DEBUG (printf ("[simd-simplify] Adding XZERO for R%d on BB %d: ", i, target_bb [var->dreg]->block_num); );
				MonoInst *tmp;
				MONO_INST_NEW (cfg, tmp, OP_XZERO);
				tmp->dreg = var->dreg;
				tmp->type = STACK_VTYPE;
				tmp->klass = var->klass;
				mono_bblock_insert_before_ins (target_bb [var->dreg], ins, tmp);
				break;
			}
		}
	}

	for (ins = first_bb->code; ins; ins = ins->next) {
		if (ins->opcode == OP_XZERO && (vreg_flags [ins->dreg] & VREG_SINGLE_BB_USE)) {
			DEBUG (printf ("[simd-simplify] Nullify %d on first BB: ", ins->dreg); mono_print_ins(ins));
			NULLIFY_INS (ins);
		}
	}

	g_free (vreg_flags);
	g_free (target_bb);
}

/*
* Windows x64 value type ABI uses reg/stack references (ArgValuetypeAddrInIReg/ArgValuetypeAddrOnStack)
* for function arguments. When using SIMD intrinsics arguments optimized into OP_ARG needs to be decomposed
* into correspondig SIMD LOADX/STOREX instructions.
*/
#if defined(TARGET_WIN32) && defined(TARGET_AMD64)
static gboolean
decompose_vtype_opt_uses_simd_intrinsics (MonoCompile *cfg, MonoInst *ins)
{
	if (cfg->uses_simd_intrinsics & MONO_CFG_USES_SIMD_INTRINSICS_DECOMPOSE_VTYPE)
		return TRUE;

	switch (ins->opcode) {
	case OP_XMOVE:
	case OP_XZERO:
	case OP_LOADX_MEMBASE:
	case OP_LOADX_ALIGNED_MEMBASE:
	case OP_STOREX_MEMBASE:
	case OP_STOREX_ALIGNED_MEMBASE_REG:
		return TRUE;
	default:
		return FALSE;
	}
}

static void
decompose_vtype_opt_load_arg (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, gint32 *sreg_int32)
{
	guint32 *sreg = (guint32*)sreg_int32;
	MonoInst *src_var = get_vreg_to_inst (cfg, *sreg);
	if (src_var && src_var->opcode == OP_ARG && src_var->klass && MONO_CLASS_IS_SIMD (cfg, src_var->klass)) {
		MonoInst *varload_ins, *load_ins;
		NEW_VARLOADA (cfg, varload_ins, src_var, src_var->inst_vtype);
		mono_bblock_insert_before_ins (bb, ins, varload_ins);
		MONO_INST_NEW (cfg, load_ins, OP_LOADX_MEMBASE);
		load_ins->klass = src_var->klass;
		load_ins->type = STACK_VTYPE;
		load_ins->sreg1 = varload_ins->dreg;
		load_ins->dreg = alloc_xreg (cfg);
		mono_bblock_insert_after_ins (bb, varload_ins, load_ins);
		*sreg = load_ins->dreg;
	}
}

void
mono_simd_decompose_intrinsic (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins)
{
	if (cfg->opt & MONO_OPT_SIMD && decompose_vtype_opt_uses_simd_intrinsics (cfg, ins)) {
		decompose_vtype_opt_load_arg (cfg, bb, ins, &(ins->sreg1));
		decompose_vtype_opt_load_arg (cfg, bb, ins, &(ins->sreg2));
		decompose_vtype_opt_load_arg (cfg, bb, ins, &(ins->sreg3));
		MonoInst *dest_var = get_vreg_to_inst (cfg, ins->dreg);
		if (dest_var && dest_var->opcode == OP_ARG && dest_var->klass && MONO_CLASS_IS_SIMD (cfg, dest_var->klass)) {
			MonoInst *varload_ins, *store_ins;
			ins->dreg = alloc_xreg (cfg);
			NEW_VARLOADA (cfg, varload_ins, dest_var, dest_var->inst_vtype);
			mono_bblock_insert_after_ins (bb, ins, varload_ins);
			MONO_INST_NEW (cfg, store_ins, OP_STOREX_MEMBASE);
			store_ins->klass = dest_var->klass;
			store_ins->type = STACK_VTYPE;
			store_ins->sreg1 = ins->dreg;
			store_ins->dreg = varload_ins->dreg;
			mono_bblock_insert_after_ins (bb, varload_ins, store_ins);
		}
	}
}
#else
void
mono_simd_decompose_intrinsic (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins)
{
}
#endif /*defined(TARGET_WIN32) && defined(TARGET_AMD64)*/

/*
 * This function expect that src be a value.
 */
static int
get_simd_vreg (MonoCompile *cfg, MonoMethod *cmethod, MonoInst *src)
{
	const char *spec = INS_INFO (src->opcode);

	if (src->opcode == OP_XMOVE) {
		return src->sreg1;
	} else if (spec [MONO_INST_DEST] == 'x') {
		return src->dreg;
	} else if (src->opcode == OP_VCALL || src->opcode == OP_VCALL_MEMBASE) {
		return src->dreg;
	}

	g_warning ("get_simd_vreg:: could not infer source simd vreg for op");
	mono_print_ins (src);
	g_assert_not_reached ();
}

/*
 * This function will load the value if needed. 
 */
static int
load_simd_vreg_class (MonoCompile *cfg, MonoClass *klass, MonoInst *src, gboolean *indirect)
{
	const char *spec = INS_INFO (src->opcode);

	if (indirect)
		*indirect = FALSE;
	if (src->opcode == OP_XMOVE) {
		return src->sreg1;
	} else if (src->opcode == OP_LDADDR) {
		int res = ((MonoInst*)src->inst_p0)->dreg;
		return res;
	} else if (spec [MONO_INST_DEST] == 'x') {
		return src->dreg;
	} else if (src->type == STACK_PTR || src->type == STACK_MP) {
		MonoInst *ins;
		if (indirect)
			*indirect = TRUE;

		MONO_INST_NEW (cfg, ins, OP_LOADX_MEMBASE);
		ins->klass = klass;
		ins->sreg1 = src->dreg;
		ins->type = STACK_VTYPE;
		ins->dreg = alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, ins);
		return ins->dreg;
	}
	g_warning ("load_simd_vreg:: could not infer source simd (%d) vreg for op", src->type);
	mono_print_ins (src);
	g_assert_not_reached ();
}

static int
load_simd_vreg (MonoCompile *cfg, MonoMethod *cmethod, MonoInst *src, gboolean *indirect)
{
	return load_simd_vreg_class (cfg, cmethod->klass, src, indirect);
}

/*We share the var with fconv_to_r8_x to save some stack space.*/
static MonoInst*
get_double_spill_area (MonoCompile *cfg)
{
	if (!cfg->fconv_to_r8_x_var) {
		cfg->fconv_to_r8_x_var = mono_compile_create_var (cfg, m_class_get_byval_arg (mono_defaults.double_class), OP_LOCAL);
		cfg->fconv_to_r8_x_var->flags |= MONO_INST_VOLATILE; /*FIXME, use the don't regalloc flag*/
	}	
	return cfg->fconv_to_r8_x_var;
}
static MonoInst*
get_simd_ctor_spill_area (MonoCompile *cfg, MonoClass *avector_klass)
{
	if (!cfg->simd_ctor_var) {
		cfg->simd_ctor_var = mono_compile_create_var (cfg, m_class_get_byval_arg (avector_klass), OP_LOCAL);
		cfg->simd_ctor_var->flags |= MONO_INST_VOLATILE; /*FIXME, use the don't regalloc flag*/
	}	
	return cfg->simd_ctor_var;
}

static int
mono_type_to_expand_op (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		return OP_EXPAND_I1;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		return OP_EXPAND_I2;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return OP_EXPAND_I4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return OP_EXPAND_I8;
	case MONO_TYPE_R4:
		return OP_EXPAND_R4;
	case MONO_TYPE_R8:
		return OP_EXPAND_R8;
	default:
		g_assert_not_reached ();
	}
}

static int
type_to_comp_op (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		return OP_PCMPEQB;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		return OP_PCMPEQW;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return OP_PCMPEQD;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return OP_PCMPEQQ;
	case MONO_TYPE_R4:
		return OP_COMPPS;
	case MONO_TYPE_R8:
		return OP_COMPPD;
	default:
		g_assert_not_reached ();
		return -1;
	}
}

static int
type_to_gt_op (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_I1:
		return OP_PCMPGTB;
	case MONO_TYPE_I2:
		return OP_PCMPGTW;
	case MONO_TYPE_I4:
		return OP_PCMPGTD;
	case MONO_TYPE_I8:
		return OP_PCMPGTQ;
	default:
		return -1;
	}
}

static int
type_to_padd_op (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
		return OP_PADDB;
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
		return OP_PADDW;
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
		return OP_PADDD;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		return OP_PADDQ;
	case MONO_TYPE_R4:
		return OP_ADDPS;
	case MONO_TYPE_R8:
		return OP_ADDPD;
	default:
		break;
	}
	return -1;
}

static int
type_to_psub_op (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
		return OP_PSUBB;
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
		return OP_PSUBW;
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
		return OP_PSUBD;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		return OP_PSUBQ;
	case MONO_TYPE_R4:
		return OP_SUBPS;
	case MONO_TYPE_R8:
		return OP_SUBPD;
	default:
		break;
	}
	return -1;
}

static int
type_to_pmul_op (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
		return OP_PMULW;
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
		return OP_PMULD;
	case MONO_TYPE_R4:
		return OP_MULPS;
	case MONO_TYPE_R8:
		return OP_MULPD;
	case MONO_TYPE_U8:
		/* PMULQ multiplies two 32 bit numbers into a 64 bit one */
		return -1;
	case MONO_TYPE_I8:
		return -1;
	default:
		break;
	}
	return -1;
}

static int
type_to_pdiv_op (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_R4:
		return OP_DIVPS;
	case MONO_TYPE_R8:
		return OP_DIVPD;
	default:
		break;
	}
	return -1;
}

static int
type_to_pxor_op (MonoType *t)
{
	/*
	 * These opcodes have the same semantics, but using the
	 * correctly typed version is better for performance.
	 */
	switch (t->type) {
	case MONO_TYPE_R4:
		return OP_XORPS;
	case MONO_TYPE_R8:
		return OP_XORPD;
	default:
		return OP_PXOR;
	}
}

static int
type_to_pand_op (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_R4:
		return OP_ANDPS;
	case MONO_TYPE_R8:
		return OP_ANDPD;
	default:
		return OP_PAND;
	}
}

static int
type_to_por_op (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_R4:
		return OP_ORPS;
	case MONO_TYPE_R8:
		return OP_ORPD;
	default:
		return OP_POR;
	}
}

static int
type_to_pmin_op (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_R4:
		return OP_MINPS;
	case MONO_TYPE_R8:
		return OP_MINPD;
	case MONO_TYPE_I1:
		return OP_PMINB;
	case MONO_TYPE_U1:
		return OP_PMINB_UN;
	case MONO_TYPE_I2:
		return OP_PMINW;
	case MONO_TYPE_U2:
		return OP_PMINW_UN;
	case MONO_TYPE_I4:
		return OP_PMIND;
	case MONO_TYPE_U4:
		return OP_PMIND_UN;
	default:
		return -1;
	}
}

static int
type_to_pmax_op (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_R4:
		return OP_MAXPS;
	case MONO_TYPE_R8:
		return OP_MAXPD;
	case MONO_TYPE_I1:
		return OP_PMAXB;
	case MONO_TYPE_U1:
		return OP_PMAXB_UN;
	case MONO_TYPE_I2:
		return OP_PMAXW;
	case MONO_TYPE_U2:
		return OP_PMAXW_UN;
	case MONO_TYPE_I4:
		return OP_PMAXD;
	case MONO_TYPE_U4:
		return OP_PMAXD_UN;
	default:
		return -1;
	}
}

static int
get_simd_vreg_or_expanded_scalar (MonoCompile *cfg, MonoClass *klass, MonoType *param_type, MonoInst *src)
{
	MonoInst *ins;
	int expand_op;

	if (m_class_is_simd_type (mono_class_from_mono_type_internal (param_type)))
		return get_simd_vreg (cfg, NULL, src);

	expand_op = mono_type_to_expand_op (param_type);
	MONO_INST_NEW (cfg, ins, expand_op);
	ins->klass = klass;
	ins->sreg1 = src->dreg;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);

	if (expand_op == OP_EXPAND_R4)
		ins->backend.spill_var = mini_get_int_to_float_spill_area (cfg);
	else if (expand_op == OP_EXPAND_R8)
		ins->backend.spill_var = get_double_spill_area (cfg);

	return ins->dreg;
}

/*
 * simd_intrinsic_emit_binary_op:
 *
 *   Emit a binary SIMD opcode.
 * @LHS/@RHS are the two arguments, they can be either a SIMD type or a scalar one. Scalar arguments are
 * expanded to the SIMD type.
 */
static MonoInst*
simd_intrinsic_emit_binary_op (MonoCompile *cfg, int opcode, int flags, MonoClass *klass, MonoType *lhs_type, MonoType *rhs_type, MonoInst *lhs, MonoInst *rhs)
{
	MonoInst* ins;
	int left_vreg, right_vreg;

	left_vreg = get_simd_vreg_or_expanded_scalar (cfg, klass, lhs_type, lhs);
	right_vreg = get_simd_vreg_or_expanded_scalar (cfg, klass, rhs_type, rhs);

	MONO_INST_NEW (cfg, ins, opcode);
	ins->klass = klass;
	ins->sreg1 = left_vreg;
	ins->sreg2 = right_vreg;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	ins->inst_c0 = flags;
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_binary (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoMethodSignature *sig = mono_method_signature_internal (cmethod);

	g_assert (sig->param_count == 2);

	return simd_intrinsic_emit_binary_op (cfg, intrinsic->opcode, intrinsic->flags, cmethod->klass, sig->params [0], sig->params [1], args [0], args [1]);
}

static MonoInst*
simd_intrinsic_emit_unary (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst* ins;
	int vreg;
	
	vreg = get_simd_vreg (cfg, cmethod, args [0]);

	MONO_INST_NEW (cfg, ins, intrinsic->opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = vreg;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static int
mono_type_to_extract_op (MonoCompile *cfg, MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_I1:
		return OP_EXTRACT_I1;
	case MONO_TYPE_U1:
		return OP_EXTRACT_U1;
	case MONO_TYPE_I2:
		return OP_EXTRACT_I2;
	case MONO_TYPE_U2:
		return OP_EXTRACT_U2;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return OP_EXTRACT_I4;
	case MONO_TYPE_R4:
		if (COMPILE_LLVM (cfg))
			return OP_EXTRACT_R4;
		else
			return OP_EXTRACT_I4;
	default:
		g_assert_not_reached ();
	}
}

/*Returns the amount to shift the element index to get the dword it belongs to*/
static int
mono_type_elements_shift_bits (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		return 2;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		return 1;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
		return 0;
	default:
		g_assert_not_reached ();
	}
}

static G_GNUC_UNUSED int
mono_type_to_insert_op (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		return OP_INSERT_I1;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		return OP_INSERT_I2;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return OP_INSERT_I4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return OP_INSERT_I8;
	case MONO_TYPE_R4:
		return OP_INSERT_R4;
	case MONO_TYPE_R8:
		return OP_INSERT_R8;
	default:
		g_assert_not_reached ();
	}
}

static int
mono_type_to_slow_insert_op (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		return OP_INSERTX_U1_SLOW;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		return OP_INSERT_I2;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return OP_INSERTX_I4_SLOW;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return OP_INSERTX_I8_SLOW;
	case MONO_TYPE_R4:
		return OP_INSERTX_R4_SLOW;
	case MONO_TYPE_R8:
		return OP_INSERTX_R8_SLOW;
	default:
		g_assert_not_reached ();
	}
}

static MonoInst*
simd_intrinsic_emit_setter (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	MonoMethodSignature *sig = mono_method_signature_internal (cmethod);
	int size, align;
	gboolean indirect;
	int dreg;

	size = mono_type_size (sig->params [0], &align); 

	if (COMPILE_LLVM (cfg)) {
		MONO_INST_NEW (cfg, ins, mono_type_to_insert_op (sig->params [0]));
		ins->klass = cmethod->klass;
		ins->dreg = ins->sreg1 = dreg = load_simd_vreg (cfg, cmethod, args [0], &indirect);
		ins->sreg2 = args [1]->dreg;
		ins->inst_c0 = intrinsic->opcode;
		MONO_ADD_INS (cfg->cbb, ins);
	} else if (size == 2 || size == 4 || size == 8) {
		MONO_INST_NEW (cfg, ins, mono_type_to_slow_insert_op (sig->params [0]));
		ins->klass = cmethod->klass;
		/*This is a partial load so we encode the dependency on the previous value by setting dreg and sreg1 to the same value.*/
		ins->dreg = ins->sreg1 = dreg = load_simd_vreg (cfg, cmethod, args [0], &indirect);
		ins->sreg2 = args [1]->dreg;
		ins->inst_c0 = intrinsic->opcode;
		if (sig->params [0]->type == MONO_TYPE_R4)
			ins->backend.spill_var = mini_get_int_to_float_spill_area (cfg);
		else if (sig->params [0]->type == MONO_TYPE_R8)
			ins->backend.spill_var = get_double_spill_area (cfg);
		MONO_ADD_INS (cfg->cbb, ins);
	} else {
		int vreg, sreg;

		MONO_INST_NEW (cfg, ins, OP_EXTRACTX_U2);
		ins->klass = cmethod->klass;
		ins->sreg1 = sreg = dreg = load_simd_vreg (cfg, cmethod, args [0], &indirect);
		ins->type = STACK_I4;
		ins->dreg = vreg = alloc_ireg (cfg);
		ins->inst_c0 = intrinsic->opcode / 2;
		MONO_ADD_INS (cfg->cbb, ins);

		MONO_INST_NEW (cfg, ins, OP_INSERTX_U1_SLOW);
		ins->klass = cmethod->klass;
		ins->sreg1 = vreg;
		ins->sreg2 = args [1]->dreg;
		ins->dreg = sreg;
		ins->inst_c0 = intrinsic->opcode;
		MONO_ADD_INS (cfg->cbb, ins);
	}

	if (indirect) {
		MONO_INST_NEW (cfg, ins, OP_STOREX_MEMBASE);
		ins->klass = cmethod->klass;
		ins->dreg = args [0]->dreg;
		ins->sreg1 = dreg;
		MONO_ADD_INS (cfg->cbb, ins);
	}
	return ins;
}

/*
 * simd_intrinsic_emit_getter_op:
 *
 *   Emit IR for loading an element of a SIMD value.
 *
 * @klass is the simd type, @type is the element type.
 */
static MonoInst*
simd_intrinsic_emit_getter_op (MonoCompile *cfg, int index, MonoClass *klass, MonoType *type, MonoInst *arg)
{
	MonoInst *ins;
	int vreg, shift_bits;

	vreg = load_simd_vreg_class (cfg, klass, arg, NULL);

	if (type->type == MONO_TYPE_I8 || type->type == MONO_TYPE_U8 || type->type == MONO_TYPE_R8) {
		MonoInst *ins;
		gboolean is_r8 = type->type == MONO_TYPE_R8;

		MONO_INST_NEW (cfg, ins, is_r8 ? OP_EXTRACT_R8 : OP_EXTRACT_I8);
		ins->klass = klass;
		ins->sreg1 = vreg;
		ins->inst_c0 = index;
		if (is_r8) {
			ins->type = STACK_R8;
			ins->dreg = alloc_freg (cfg);
			ins->backend.spill_var = get_double_spill_area (cfg);
		} else {
			ins->type = STACK_I8;
			ins->dreg = alloc_lreg (cfg);
		}
		MONO_ADD_INS (cfg->cbb, ins);
		return ins;
	}

	shift_bits = mono_type_elements_shift_bits (type);

	if ((index >> shift_bits) && !cfg->compile_llvm) {
		MONO_INST_NEW (cfg, ins, OP_PSHUFLED);
		ins->klass = klass;
		ins->sreg1 = vreg;
		ins->inst_c0 = index >> shift_bits;
		ins->type = STACK_VTYPE;
		ins->dreg = vreg = alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, ins);
	}

	MONO_INST_NEW (cfg, ins, mono_type_to_extract_op (cfg, type));
	ins->klass = klass;
	ins->sreg1 = vreg;
	if (ins->opcode == OP_EXTRACT_R4) {
		ins->type = STACK_R4;
		ins->dreg = vreg = alloc_freg (cfg);
	} else {
		ins->type = STACK_I4;
		ins->dreg = vreg = alloc_ireg (cfg);
	}
	if (cfg->compile_llvm)
		ins->inst_c0 = index;
	else
		ins->inst_c0 = index & ((1 << shift_bits) - 1);
	MONO_ADD_INS (cfg->cbb, ins);

	if (type->type == MONO_TYPE_R4 && ins->opcode != OP_EXTRACT_R4) {
		MONO_INST_NEW (cfg, ins, cfg->r4fp ? OP_ICONV_TO_R4_RAW : OP_MOVE_I4_TO_F);
		ins->klass = mono_defaults.single_class;
		ins->sreg1 = vreg;
		ins->type = cfg->r4_stack_type;
		ins->dreg = alloc_freg (cfg);
		ins->backend.spill_var = mini_get_int_to_float_spill_area (cfg);
		MONO_ADD_INS (cfg->cbb, ins);
	}
	return ins;
}

static MonoInst*
simd_intrinsic_emit_getter (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoMethodSignature *sig = mono_method_signature_internal (cmethod);

	return simd_intrinsic_emit_getter_op (cfg, intrinsic->opcode, cmethod->klass, sig->ret, args [0]);
}

static MonoInst*
simd_intrinsic_emit_long_getter (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg;
	gboolean is_r8 = mono_method_signature_internal (cmethod)->ret->type == MONO_TYPE_R8;

	vreg = load_simd_vreg (cfg, cmethod, args [0], NULL);

	MONO_INST_NEW (cfg, ins, is_r8 ? OP_EXTRACT_R8 : OP_EXTRACT_I8);
	ins->klass = cmethod->klass;
	ins->sreg1 = vreg;
	ins->inst_c0 = intrinsic->opcode;
	if (is_r8) {
		ins->type = STACK_R8;
		ins->dreg = alloc_freg (cfg);
		ins->backend.spill_var = get_double_spill_area (cfg);
	} else {
		ins->type = STACK_I8;
		ins->dreg = alloc_lreg (cfg);
	}
	MONO_ADD_INS (cfg->cbb, ins);

	return ins;
}

static MonoInst*
simd_intrinsic_emit_ctor (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins = NULL;
	int i, addr_reg;
	gboolean is_ldaddr = (args [0]->opcode == OP_LDADDR && args [0]->inst_left->opcode != OP_ARG);
	MonoMethodSignature *sig = mono_method_signature_internal (cmethod);
	int store_op = mono_type_to_store_membase (cfg, sig->params [0]);
	int arg_size = mono_type_size (sig->params [0], &i);
	int opcode;

	if (sig->param_count == 1) {
		int dreg;
		
		if (is_ldaddr) {
			dreg = args [0]->inst_i0->dreg;
			NULLIFY_INS (args [0]);
		} else {
			g_assert (args [0]->type == STACK_MP || args [0]->type == STACK_PTR);
			dreg = alloc_ireg (cfg);
		}

		if (intrinsic)
			opcode = intrinsic->opcode;
		else
			opcode = mono_type_to_expand_op (sig->params [0]);
		MONO_INST_NEW (cfg, ins, opcode);
		ins->klass = cmethod->klass;
		ins->sreg1 = args [1]->dreg;
		ins->type = STACK_VTYPE;
		ins->dreg = dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		if (sig->params [0]->type == MONO_TYPE_R4)
			ins->backend.spill_var = mini_get_int_to_float_spill_area (cfg);
		else if (sig->params [0]->type == MONO_TYPE_R8)
			ins->backend.spill_var = get_double_spill_area (cfg);

		if (!is_ldaddr) {
			MONO_INST_NEW (cfg, ins, OP_STOREX_MEMBASE);
			ins->dreg = args [0]->dreg;
			ins->sreg1 = dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}
		return ins;
	}

	if (is_ldaddr) {
		NEW_VARLOADA (cfg, ins, get_simd_ctor_spill_area (cfg, cmethod->klass), &cmethod->klass->byref_arg);
		MONO_ADD_INS (cfg->cbb, ins);
		addr_reg = ins->dreg;
	} else {
		g_assert (args [0]->type == STACK_MP || args [0]->type == STACK_PTR);
		addr_reg = args [0]->dreg;
	}

	for (i = sig->param_count - 1; i >= 0; --i) {
		EMIT_NEW_STORE_MEMBASE (cfg, ins, store_op, addr_reg, i * arg_size, args [i + 1]->dreg);
	}

	if (sig->param_count * arg_size < 16) {
		/* If there are not enough arguments, fill the rest with 0s */
		for (i = sig->param_count; i < 16 / arg_size; ++i) {
			switch (arg_size) {
			case 4:
				MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREI4_MEMBASE_IMM, addr_reg, i * arg_size, 0);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
		}
	}

	if (is_ldaddr) { /*Eliminate LDADDR if it's initing a local var*/
		int vreg = ((MonoInst*)args [0]->inst_p0)->dreg;
		NULLIFY_INS (args [0]);
		
		MONO_INST_NEW (cfg, ins, OP_LOADX_MEMBASE);
		ins->klass = cmethod->klass;
		ins->sreg1 = addr_reg;
		ins->type = STACK_VTYPE;
		ins->dreg = vreg;
		MONO_ADD_INS (cfg->cbb, ins);
	}
	return ins;
}

static MonoInst*
simd_intrinsic_emit_cast (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	MonoClass *klass;
	int vreg;

	vreg = get_simd_vreg (cfg, cmethod, args [0]);		

	if (cmethod->is_inflated)
		/* Vector<T> */
		klass = mono_class_from_mono_type_internal (mono_method_signature_internal (cmethod)->ret);
	else
		klass = cmethod->klass;

	MONO_INST_NEW (cfg, ins, OP_XMOVE);
	ins->klass = klass;
	ins->type = STACK_VTYPE;
	ins->sreg1 = vreg;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_shift (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg, vreg2 = -1, opcode = intrinsic->opcode;

	vreg = get_simd_vreg (cfg, cmethod, args [0]);

	if (args [1]->opcode != OP_ICONST) {
		MONO_INST_NEW (cfg, ins, OP_ICONV_TO_X);
		ins->klass = mono_defaults.int32_class;
		ins->sreg1 = args [1]->dreg;
		ins->type = STACK_I4;
		ins->dreg = vreg2 = alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, ins);

		++opcode; /*The shift_reg version op is always +1 from the regular one.*/
	}

	MONO_INST_NEW (cfg, ins, opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = vreg;
	ins->sreg2 = vreg2;

	if (args [1]->opcode == OP_ICONST) {
		ins->inst_imm = args [1]->inst_c0;
		NULLIFY_INS (args [1]);
	}

	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static gboolean
mono_op_is_packed_compare (int op)
{
	return op >= OP_PCMPEQB && op <= OP_PCMPEQQ;
}

static MonoInst*
simd_intrinsic_emit_equality_op (MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args, int opcode, int flags)
{
	MonoInst* ins;
	int left_vreg, right_vreg, tmp_vreg;

	left_vreg = load_simd_vreg (cfg, cmethod, args [0], NULL);
	right_vreg = get_simd_vreg (cfg, cmethod, args [1]);
	
	MONO_INST_NEW (cfg, ins, opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = left_vreg;
	ins->sreg2 = right_vreg;
	ins->type = STACK_VTYPE;
	ins->klass = cmethod->klass;
	ins->dreg = tmp_vreg = alloc_ireg (cfg);
	ins->inst_c0 = flags;
	MONO_ADD_INS (cfg->cbb, ins);

	/*FIXME the next ops are SSE specific*/
	MONO_INST_NEW (cfg, ins, OP_EXTRACT_MASK);
	ins->klass = cmethod->klass;
	ins->sreg1 = tmp_vreg;
	ins->type = STACK_I4;
	ins->dreg = tmp_vreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);

	/*FP ops have a not equal instruction, which means that we must test the results with OR semantics.*/
	if (mono_op_is_packed_compare (opcode) || flags == SIMD_COMP_EQ) {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_vreg, 0xFFFF);
		NEW_UNALU (cfg, ins, flags == SIMD_COMP_EQ ? OP_CEQ : OP_CLT_UN, tmp_vreg, -1);
	} else {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_vreg, 0);
		NEW_UNALU (cfg, ins, OP_CGT_UN, tmp_vreg, -1);
	}
	MONO_ADD_INS (cfg->cbb, ins);	
	return ins;
}

static MonoInst*
simd_intrinsic_emit_equality (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	return simd_intrinsic_emit_equality_op (cfg, cmethod, args, intrinsic->opcode, intrinsic->flags);
}

static MonoInst*
simd_intrinsic_emit_shuffle (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg, vreg2 = -1;
	int param_count = mono_method_signature_internal (cmethod)->param_count;

	if (args [param_count - 1]->opcode != OP_ICONST) {
		/*TODO Shuffle with non literals is not yet supported */
		return NULL;
	}

	vreg = get_simd_vreg (cfg, cmethod, args [0]);
	if (param_count == 3)
		vreg2 = get_simd_vreg (cfg, cmethod, args [1]);

	NULLIFY_INS (args [param_count - 1]);


	MONO_INST_NEW (cfg, ins, intrinsic->opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = vreg;
	ins->sreg2 = vreg2;
	ins->inst_c0 = args [param_count - 1]->inst_c0;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);

	if (param_count == 3 && ins->opcode == OP_PSHUFLED)
		ins->opcode = OP_SHUFPS;
	return ins;
}

static MonoInst*
simd_intrinsic_emit_load_aligned (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, OP_LOADX_ALIGNED_MEMBASE);
	ins->klass = cmethod->klass;
	ins->sreg1 = args [0]->dreg;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_store (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg;

	vreg = get_simd_vreg (cfg, cmethod, args [1]);

	MONO_INST_NEW (cfg, ins, intrinsic->opcode);
	ins->klass = cmethod->klass;
	ins->dreg = args [0]->dreg;
	ins->sreg1 = vreg;
	ins->type = STACK_VTYPE;
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_extract_mask (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg;
	
	vreg = get_simd_vreg (cfg, cmethod, args [0]);

	MONO_INST_NEW (cfg, ins, OP_EXTRACT_MASK);
	ins->klass = cmethod->klass;
	ins->sreg1 = vreg;
	ins->type = STACK_I4;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);

	return ins;
}

static MonoInst*
simd_intrinsic_emit_prefetch (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, OP_PREFETCH_MEMBASE);
	ins->klass = cmethod->klass;
	ins->sreg1 = args [0]->dreg;
	ins->backend.arg_info = intrinsic->flags;
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_const (const SimdIntrinsic *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, intrinsic->opcode);
	ins->klass = cmethod->klass;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_xreg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
emit_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args, const SimdIntrinsic *intrinsics, guint32 size)
{
	const SimdIntrinsic *result = (const SimdIntrinsic *)mono_binary_search (cmethod->name, intrinsics, size, sizeof (SimdIntrinsic), &simd_intrinsic_compare_by_name);
	if (!result) {
		DEBUG (printf ("function doesn't have a simd intrinsic %s::%s/%d\n", m_class_get_name (cmethod->klass), cmethod->name, fsig->param_count));
		return NULL;
	}
	if (IS_DEBUG_ON (cfg)) {
		int i, max;
		printf ("found call to intrinsic %s::%s/%d -> %s\n", m_class_get_name (cmethod->klass), cmethod->name, fsig->param_count, method_name (result->name));
		max = fsig->param_count + fsig->hasthis;
		for (i = 0; i < max; ++i) {
			printf ("param %d:  ", i);
			mono_print_ins (args [i]);
		}
	}
	if (result->simd_version) {
		MonoCPUFeatures features = mini_get_cpu_features (cfg);
		if ((result->simd_version & features) == 0) {
			printf ("function %s::%s/%d requires one of unsuported SIMD instruction set(s). \n", m_class_get_name (cmethod->klass), cmethod->name, fsig->param_count);
			if (IS_DEBUG_ON (cfg))
				printf ("function %s::%s/%d requires one of unsuported SIMD instruction set(s). \n", m_class_get_name (cmethod->klass), cmethod->name, fsig->param_count);
			return NULL;
		}
	}

	switch (result->simd_emit_mode) {
	case SIMD_EMIT_BINARY:
		return simd_intrinsic_emit_binary (result, cfg, cmethod, args);
	case SIMD_EMIT_UNARY:
		return simd_intrinsic_emit_unary (result, cfg, cmethod, args);
	case SIMD_EMIT_SETTER:
		return simd_intrinsic_emit_setter (result, cfg, cmethod, args);
	case SIMD_EMIT_GETTER:
		return simd_intrinsic_emit_getter (result, cfg, cmethod, args);
	case SIMD_EMIT_GETTER_QWORD:
		return simd_intrinsic_emit_long_getter (result, cfg, cmethod, args);
	case SIMD_EMIT_CTOR:
		return simd_intrinsic_emit_ctor (result, cfg, cmethod, args);
	case SIMD_EMIT_CAST:
		return simd_intrinsic_emit_cast (result, cfg, cmethod, args);
	case SIMD_EMIT_SHUFFLE:
		return simd_intrinsic_emit_shuffle (result, cfg, cmethod, args); 
	case SIMD_EMIT_SHIFT:
		return simd_intrinsic_emit_shift (result, cfg, cmethod, args);
	case SIMD_EMIT_EQUALITY:
		return simd_intrinsic_emit_equality (result, cfg, cmethod, args);
	case SIMD_EMIT_LOAD_ALIGNED:
		return simd_intrinsic_emit_load_aligned (result, cfg, cmethod, args);
	case SIMD_EMIT_STORE:
		return simd_intrinsic_emit_store (result, cfg, cmethod, args);
	case SIMD_EMIT_EXTRACT_MASK:
		return simd_intrinsic_emit_extract_mask (result, cfg, cmethod, args);
	case SIMD_EMIT_PREFETCH:
		return simd_intrinsic_emit_prefetch (result, cfg, cmethod, args);
	}
	g_assert_not_reached ();
}

static int
mono_emit_vector_ldelema (MonoCompile *cfg, MonoType *array_type, MonoInst *arr, MonoInst *index, gboolean check_bounds)
{
	MonoInst *ins;
	guint32 size;
	int mult_reg, add_reg, array_reg, index_reg, index2_reg, index3_reg;

	size = mono_array_element_size (mono_class_from_mono_type_internal (array_type));
	mult_reg = alloc_preg (cfg);
	array_reg = arr->dreg;
	index_reg = index->dreg;

#if TARGET_SIZEOF_VOID_P == 8
	/* The array reg is 64 bits but the index reg is only 32 */
	index2_reg = alloc_preg (cfg);
	MONO_EMIT_NEW_UNALU (cfg, OP_SEXT_I4, index2_reg, index_reg);
#else
	index2_reg = index_reg;
#endif
	index3_reg = alloc_preg (cfg);

	if (check_bounds) {
		MONO_EMIT_BOUNDS_CHECK (cfg, array_reg, MonoArray, max_length, index2_reg);
		MONO_EMIT_NEW_BIALU_IMM (cfg,  OP_PADD_IMM, index3_reg, index2_reg, 16 / size - 1);
		MONO_EMIT_BOUNDS_CHECK (cfg, array_reg, MonoArray, max_length, index3_reg);
	}

	add_reg = alloc_preg (cfg);

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_MUL_IMM, mult_reg, index2_reg, size);
	MONO_EMIT_NEW_BIALU (cfg, OP_PADD, add_reg, array_reg, mult_reg);
	NEW_BIALU_IMM (cfg, ins, OP_PADD_IMM, add_reg, add_reg, MONO_STRUCT_OFFSET (MonoArray, vector));
	ins->type = STACK_PTR;
	MONO_ADD_INS (cfg->cbb, ins);

	return add_reg;
}

static MonoInst*
emit_array_extension_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if ((!strcmp ("GetVector", cmethod->name) || !strcmp ("GetVectorAligned", cmethod->name)) && fsig->param_count == 2) {
		MonoInst *load;
		int addr = mono_emit_vector_ldelema (cfg, fsig->params [0], args [0], args [1], TRUE);

		MONO_INST_NEW (cfg, load, !strcmp ("GetVectorAligned", cmethod->name) ? OP_LOADX_ALIGNED_MEMBASE : OP_LOADX_MEMBASE );
		load->klass = cmethod->klass;
		load->sreg1 = addr;
		load->type = STACK_VTYPE;
		load->dreg = alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, load);

		return load;
	}
	if ((!strcmp ("SetVector", cmethod->name) || !strcmp ("SetVectorAligned", cmethod->name)) && fsig->param_count == 3) {
		MonoInst *store;
		int vreg = get_simd_vreg (cfg, cmethod, args [1]);
		int addr = mono_emit_vector_ldelema (cfg, fsig->params [0], args [0], args [2], TRUE);

		MONO_INST_NEW (cfg, store, !strcmp ("SetVectorAligned", cmethod->name) ? OP_STOREX_ALIGNED_MEMBASE_REG :  OP_STOREX_MEMBASE);
		store->klass = cmethod->klass;
		store->dreg = addr;
		store->sreg1 = vreg;
		MONO_ADD_INS (cfg->cbb, store);

		return store;
	}
	if (!strcmp ("IsAligned", cmethod->name) && fsig->param_count == 2) {
		MonoInst *ins;
		int addr = mono_emit_vector_ldelema (cfg, fsig->params [0], args [0], args [1], FALSE);

		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, addr, addr, 15);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, addr, 0);
		NEW_UNALU (cfg, ins, OP_CEQ, addr, -1);
		MONO_ADD_INS (cfg->cbb, ins);

		return ins;
	}
	return NULL;
}

static guint32
get_simd_supported_versions (MonoCompile *cfg)
{
	MonoCPUFeatures features = mini_get_cpu_features (cfg);
	guint32 versions = 0;

	if (features & MONO_CPU_X86_SSE)
		versions |= SIMD_VERSION_SSE1;
	if (features & MONO_CPU_X86_SSE2)
		versions |= SIMD_VERSION_SSE2;
	if (features & MONO_CPU_X86_SSE3)
		versions |= SIMD_VERSION_SSE3;
	if (features & MONO_CPU_X86_SSSE3)
		versions |= SIMD_VERSION_SSSE3;
	if (features & MONO_CPU_X86_SSE41)
		versions |= SIMD_VERSION_SSE41;
	if (features & MONO_CPU_X86_SSE42)
		versions |= SIMD_VERSION_SSE42;
	return versions;
}

static MonoInst*
emit_simd_runtime_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (!strcmp ("get_AccelMode", cmethod->name) && fsig->param_count == 0) {
		MonoInst *ins;
		guint32 versions = get_simd_supported_versions (cfg);
		EMIT_NEW_ICONST (cfg, ins, versions);
		return ins;
	}
	return NULL;
}

static gboolean
is_sys_numerics_assembly (MonoAssembly *assembly)
{
	return !strcmp ("System.Numerics", assembly->aname.name);
}

static gboolean
is_sys_numerics_vectors_assembly (MonoAssembly *assembly)
{
	return !strcmp ("System.Numerics.Vectors", assembly->aname.name);
}

MonoInst*
mono_emit_simd_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	const char *class_name;
	MonoInst *simd_inst = NULL;

	if (is_sys_numerics_assembly (m_class_get_image (cmethod->klass)->assembly)) {
		simd_inst = emit_sys_numerics_intrinsics (cfg, cmethod, fsig, args);
		goto on_exit;
	}

	if (is_sys_numerics_vectors_assembly (m_class_get_image (cmethod->klass)->assembly)) {
		simd_inst = emit_sys_numerics_vectors_intrinsics (cfg, cmethod, fsig, args);
		goto on_exit;
	}

	if (strcmp ("Mono.Simd", m_class_get_image (cmethod->klass)->assembly->aname.name) ||
	    strcmp ("Mono.Simd", m_class_get_name_space (cmethod->klass))) {
		goto on_exit;
	}

	class_name = m_class_get_name (cmethod->klass);
	if (!strcmp ("SimdRuntime", class_name)) {
		simd_inst = emit_simd_runtime_intrinsics (cfg, cmethod, fsig, args);
		goto on_exit;
	}

	if (!strcmp ("ArrayExtensions", class_name)) {
		simd_inst = emit_array_extension_intrinsics (cfg, cmethod, fsig, args);
		goto on_exit;
	}
	
	if (!strcmp ("VectorOperations", class_name)) {
		if (!(cmethod->flags & METHOD_ATTRIBUTE_STATIC))
			goto on_exit;
		class_name = m_class_get_name (mono_class_from_mono_type_internal (mono_method_signature_internal (cmethod)->params [0]));
	} else if (!m_class_is_simd_type (cmethod->klass))
		goto on_exit;

	cfg->uses_simd_intrinsics |= MONO_CFG_USES_SIMD_INTRINSICS_SIMPLIFY_INDIRECTION;
	if (!strcmp ("Vector2d", class_name)) {
		simd_inst = emit_intrinsics (cfg, cmethod, fsig, args, vector2d_intrinsics, sizeof (vector2d_intrinsics) / sizeof (SimdIntrinsic));
		goto on_exit;
	}
	if (!strcmp ("Vector4f", class_name)) {
		simd_inst = emit_intrinsics (cfg, cmethod, fsig, args, vector4f_intrinsics, sizeof (vector4f_intrinsics) / sizeof (SimdIntrinsic));
		goto on_exit;
	}
	if (!strcmp ("Vector2ul", class_name)) {
		simd_inst = emit_intrinsics (cfg, cmethod, fsig, args, vector2ul_intrinsics, sizeof (vector2ul_intrinsics) / sizeof (SimdIntrinsic));
		goto on_exit;
	}
	if (!strcmp ("Vector2l", class_name)) {
		simd_inst = emit_intrinsics (cfg, cmethod, fsig, args, vector2l_intrinsics, sizeof (vector2l_intrinsics) / sizeof (SimdIntrinsic));
		goto on_exit;
	}
	if (!strcmp ("Vector4ui", class_name)) {
		simd_inst = emit_intrinsics (cfg, cmethod, fsig, args, vector4ui_intrinsics, sizeof (vector4ui_intrinsics) / sizeof (SimdIntrinsic));
		goto on_exit;
	}
	if (!strcmp ("Vector4i", class_name)) {
		simd_inst = emit_intrinsics (cfg, cmethod, fsig, args, vector4i_intrinsics, sizeof (vector4i_intrinsics) / sizeof (SimdIntrinsic));
		goto on_exit;
	}
	if (!strcmp ("Vector8us", class_name)) {
		simd_inst = emit_intrinsics (cfg, cmethod, fsig, args, vector8us_intrinsics, sizeof (vector8us_intrinsics) / sizeof (SimdIntrinsic));
		goto on_exit;
	}
	if (!strcmp ("Vector8s", class_name)) {
		simd_inst = emit_intrinsics (cfg, cmethod, fsig, args, vector8s_intrinsics, sizeof (vector8s_intrinsics) / sizeof (SimdIntrinsic));
		goto on_exit;
	}
	if (!strcmp ("Vector16b", class_name)) {
		simd_inst = emit_intrinsics (cfg, cmethod, fsig, args, vector16b_intrinsics, sizeof (vector16b_intrinsics) / sizeof (SimdIntrinsic));
		goto on_exit;
	}
	if (!strcmp ("Vector16sb", class_name)) {
		simd_inst = emit_intrinsics (cfg, cmethod, fsig, args, vector16sb_intrinsics, sizeof (vector16sb_intrinsics) / sizeof (SimdIntrinsic));
		goto on_exit;
	}

on_exit:
	if (simd_inst != NULL) {
		cfg->uses_simd_intrinsics |= MONO_CFG_USES_SIMD_INTRINSICS;
		cfg->uses_simd_intrinsics |= MONO_CFG_USES_SIMD_INTRINSICS_DECOMPOSE_VTYPE;
	}

	return simd_inst;
}

static void
assert_handled (MonoCompile *cfg, MonoMethod *method)
{
	MonoCustomAttrInfo *cattr;
	ERROR_DECL (error);

	if (cfg->verbose_level > 1) {
		cattr = mono_custom_attrs_from_method_checked (method, error);

		if (cattr) {
			gboolean has_attr = FALSE;
			for (int i = 0; i < cattr->num_attrs; ++i)
				if (cattr->attrs [i].ctor && (!strcmp (m_class_get_name (cattr->attrs [i].ctor->klass), "JitIntrinsicAttribute")))
					has_attr = TRUE;
			if (has_attr) {
				printf ("SIMD intrinsic unhandled: %s\n", mono_method_get_name_full (method, TRUE, TRUE, MONO_TYPE_NAME_FORMAT_IL));
				fflush (stdout);
				//g_assert_not_reached ();
			}
			mono_custom_attrs_free (cattr);
		}
	}
}

// The entries should be ordered by name
// System.Numerics.Vector2/Vector3/Vector4
static const SimdIntrinsic vector2_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_R4 },
	{ SN_Abs },
	{ SN_Dot, OP_DPPS },
	{ SN_Equals, OP_COMPPS, MONO_CPU_X86_SSE, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_Max, OP_MAXPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_Min, OP_MINPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_SquareRoot, OP_SQRTPS, MONO_CPU_X86_SSE, SIMD_EMIT_UNARY },
	{ SN_op_Addition, OP_ADDPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Division, OP_DIVPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Multiply, OP_MULPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
	{ SN_op_Subtraction, OP_SUBPS, MONO_CPU_X86_SSE, SIMD_EMIT_BINARY },
};

static MonoInst*
emit_vector_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	const SimdIntrinsic *intrins;
	MonoMethodSignature *sig = mono_method_signature_internal (cmethod);
	MonoType *type = m_class_get_byval_arg (cmethod->klass);

	if (!m_class_is_simd_type (cmethod->klass))
		return NULL;

	/*
	 * Vector2/3/4 are handled the same way, since the underlying SIMD type is the same (4 * r4).
	 */
	intrins = (const SimdIntrinsic*)mono_binary_search (cmethod->name, vector2_intrinsics, sizeof (vector2_intrinsics) / sizeof (SimdIntrinsic), sizeof (SimdIntrinsic), &simd_intrinsic_compare_by_name);
	if (!intrins) {
		assert_handled (cfg, cmethod);
		return NULL;
	}

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	switch (intrins->name) {
	case SN_ctor: {
		gboolean match = TRUE;
		for (int i = 0; i < fsig->param_count; ++i)
			if (fsig->params [i]->type != MONO_TYPE_R4)
				match = FALSE;
		if (!match)
			break;
		return simd_intrinsic_emit_ctor (intrins, cfg, cmethod, args);
	}
	case SN_Equals:
		if (!(fsig->param_count == 1 && fsig->ret->type == MONO_TYPE_BOOLEAN && fsig->params [0] == type))
			break;
		return simd_intrinsic_emit_equality (intrins, cfg, cmethod, args);
	case SN_SquareRoot:
		if (!(fsig->param_count == 1 && fsig->ret == type && fsig->params [0] == type))
			break;
		return simd_intrinsic_emit_unary (intrins, cfg, cmethod, args);
	case SN_Dot:
		if (!(fsig->param_count == 2 && fsig->ret->type == MONO_TYPE_R4 && fsig->params [0] == type && fsig->params [1] == type))
			break;
		if (COMPILE_LLVM (cfg)) {
			MonoInst *ins;

			if (!(mini_get_cpu_features (cfg) & MONO_CPU_X86_SSE41))
				break;

			ins = simd_intrinsic_emit_binary (intrins, cfg, cmethod, args);
			/* The end result is in the lowest element */
			return simd_intrinsic_emit_getter_op (cfg, 0, cmethod->klass, mono_method_signature_internal (cmethod)->ret, ins);
		}
		break;
	case SN_Abs: {
		// abs(x) = max(x, sub(0,x))
		MonoInst *sub;
		MonoInst *zero;

		if (!(fsig->param_count == 1 && fsig->ret == type && fsig->params [0] == type))
			break;

		MONO_INST_NEW (cfg, zero, OP_XZERO);
		zero->dreg = alloc_xreg (cfg);
		zero->klass = cmethod->klass;
		MONO_ADD_INS (cfg->cbb, zero);

		sub = simd_intrinsic_emit_binary_op (cfg, OP_SUBPS, 0, cmethod->klass, sig->params [0], sig->params [0], zero, args [0]);
		return simd_intrinsic_emit_binary_op (cfg, OP_MAXPS, 0, cmethod->klass, sig->params [0], sig->params [0], args [0], sub);
	}
	case SN_Max:
	case SN_Min:
	case SN_op_Addition:
	case SN_op_Division:
	case SN_op_Multiply:
	case SN_op_Subtraction:
		if (!(fsig->param_count == 2 && fsig->ret == type && (fsig->params [0] == type || fsig->params [0]->type == MONO_TYPE_R4) && (fsig->params [1] == type || fsig->params [1]->type == MONO_TYPE_R4)))
			break;
		return simd_intrinsic_emit_binary (intrins, cfg, cmethod, args);
	default:
		break;
	}

	assert_handled (cfg, cmethod);

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD method %s not handled.\n", name);
		g_free (name);
	}
	return NULL;
}

static MonoInst*
emit_vector_is_hardware_accelerated_intrinsic (MonoCompile *cfg)
{
	MonoInst *ins;

	if (get_simd_supported_versions (cfg))
		EMIT_NEW_ICONST (cfg, ins, 1);
	else
		EMIT_NEW_ICONST (cfg, ins, 0);
	ins->type = STACK_I4;
	return ins;
}

/* These should be ordered by name */
static const SimdIntrinsic vector_t_intrinsics[] = {
	{ SN_ctor },
	{ SN_Abs },
	{ SN_CopyTo },
	{ SN_Equals },
	{ SN_GreaterThan },
	{ SN_GreaterThanOrEqual },
	{ SN_LessThan },
	{ SN_LessThanOrEqual },
	{ SN_Max },
	{ SN_Min },
	{ SN_get_AllOnes, OP_XONES },
	{ SN_get_Count },
	{ SN_get_Item },
	{ SN_get_Zero, OP_XZERO },
	{ SN_op_Addition },
	{ SN_op_BitwiseAnd },
	{ SN_op_BitwiseOr },
	{ SN_op_Division },
	{ SN_op_ExclusiveOr },
	{ SN_op_Explicit },
	{ SN_op_Multiply },
	{ SN_op_Subtraction }
};

static MonoInst*
emit_vector_t_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	const SimdIntrinsic *intrins;
	MonoType *type, *etype;
	MonoInst *ins;
	int size, len, index;

	intrins = (const SimdIntrinsic*)mono_binary_search (cmethod->name, vector_t_intrinsics, sizeof (vector_t_intrinsics) / sizeof (SimdIntrinsic), sizeof (SimdIntrinsic), &simd_intrinsic_compare_by_name);
	if (!intrins) {
		assert_handled (cfg, cmethod);
		return NULL;
	}

	type = m_class_get_byval_arg (cmethod->klass);
	etype = mono_class_get_context (cmethod->klass)->class_inst->type_argv [0];
	size = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
	g_assert (size);
	len = 16 / size;

	if (!MONO_TYPE_IS_PRIMITIVE (etype))
		return NULL;

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	switch (intrins->name) {
	case SN_get_Count:
		if (!(fsig->param_count == 0 && fsig->ret->type == MONO_TYPE_I4))
			break;
		EMIT_NEW_ICONST (cfg, ins, len);
		return ins;
	case SN_get_AllOnes:
	case SN_get_Zero:
		if (!(fsig->param_count == 0 && mono_metadata_type_equal (fsig->ret, type)))
			break;
		return simd_intrinsic_emit_const (intrins, cfg, cmethod, args);
	case SN_get_Item:
		g_assert (fsig->param_count == 1);
		if (args [1]->opcode != OP_ICONST)
			return NULL;
		index = args [1]->inst_c0;
		if (index < 0 || index >= len)
			return NULL;
		return simd_intrinsic_emit_getter_op (cfg, index, cmethod->klass, etype, args [0]);
	case SN_ctor:
		if (fsig->param_count == 1 && mono_metadata_type_equal (fsig->params [0], etype))
			return simd_intrinsic_emit_ctor (NULL, cfg, cmethod, args);
		if ((fsig->param_count == 1 || fsig->param_count == 2) && (fsig->params [0]->type == MONO_TYPE_SZARRAY)) {
			MonoInst *array_ins = args [1];
			MonoInst *index_ins;
			MonoInst *ldelema_ins;
			MonoInst *var;
			int end_index_reg;

			if (args [0]->opcode != OP_LDADDR)
				return NULL;

			/* .ctor (T[]) or .ctor (T[], index) */

			if (fsig->param_count == 2) {
				index_ins = args [2];
			} else {
				EMIT_NEW_ICONST (cfg, index_ins, 0);
			}

			/* Emit index check for the end (index + len - 1 < array length) */
			end_index_reg = alloc_ireg (cfg);
			EMIT_NEW_BIALU_IMM (cfg, ins, OP_IADD_IMM, end_index_reg, index_ins->dreg, len - 1);
			MONO_EMIT_BOUNDS_CHECK (cfg, array_ins->dreg, MonoArray, max_length, end_index_reg);

			/* Load the array slice into the simd reg */
			ldelema_ins = mini_emit_ldelema_1_ins (cfg, mono_class_from_mono_type_internal (etype), array_ins, index_ins, TRUE, FALSE);
			g_assert (args [0]->opcode == OP_LDADDR);
			var = (MonoInst*)args [0]->inst_p0;
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADX_MEMBASE, var->dreg, ldelema_ins->dreg, 0);
			ins->klass = cmethod->klass;
			return args [0];
		}
		break;
	case SN_op_Explicit:
		return simd_intrinsic_emit_cast (intrins, cfg, cmethod, args);
	case SN_Equals:
		if (fsig->param_count == 1 && fsig->ret->type == MONO_TYPE_BOOLEAN && mono_metadata_type_equal (fsig->params [0], type))
			return simd_intrinsic_emit_equality_op (cfg, cmethod, args, type_to_comp_op (etype), SIMD_COMP_EQ);
		if (fsig->param_count == 2 && mono_metadata_type_equal (fsig->ret, type) && mono_metadata_type_equal (fsig->params [0], type) && mono_metadata_type_equal (fsig->params [1], type))
			return simd_intrinsic_emit_binary_op (cfg, type_to_comp_op (etype), 0, cmethod->klass, fsig->params [0], fsig->params [1], args [0], args [1]);
		break;

	case SN_GreaterThan:
	case SN_GreaterThanOrEqual:
	case SN_LessThan:
	case SN_LessThanOrEqual: {
		MonoInst *cmp1, *cmp2;
		int eq_op, gt_op;

		switch (etype->type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_I2:
		case MONO_TYPE_I4:
		case MONO_TYPE_I8:
			break;
		default:
			return NULL;
		}

		eq_op = type_to_comp_op (etype);
		gt_op = type_to_gt_op (etype);

		switch (intrins->name) {
		case SN_GreaterThan:
			return simd_intrinsic_emit_binary_op (cfg, gt_op, 0, cmethod->klass, fsig->params [0], fsig->params [1], args [0], args [1]);
		case SN_LessThan:
			return simd_intrinsic_emit_binary_op (cfg, gt_op, 0, cmethod->klass, fsig->params [0], fsig->params [1], args [1], args [0]);
		case SN_LessThanOrEqual:
			cmp1 = simd_intrinsic_emit_binary_op (cfg, eq_op, 0, cmethod->klass, fsig->params [0], fsig->params [1], args [1], args [0]);
			cmp2 = simd_intrinsic_emit_binary_op (cfg, gt_op, 0, cmethod->klass, fsig->params [0], fsig->params [1], args [1], args [0]);
			return simd_intrinsic_emit_binary_op (cfg, OP_POR, 0, cmethod->klass, fsig->params [0], fsig->params [1], cmp1, cmp2);
		case SN_GreaterThanOrEqual:
			cmp1 = simd_intrinsic_emit_binary_op (cfg, eq_op, 0, cmethod->klass, fsig->params [0], fsig->params [1], args [0], args [1]);
			cmp2 = simd_intrinsic_emit_binary_op (cfg, gt_op, 0, cmethod->klass, fsig->params [0], fsig->params [1], args [0], args [1]);
			return simd_intrinsic_emit_binary_op (cfg, OP_POR, 0, cmethod->klass, fsig->params [0], fsig->params [1], cmp1, cmp2);
		default:
			g_assert_not_reached ();
			break;
		}
	}
	case SN_Abs:
		/* Vector<T>.Abs */
		switch (etype->type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_U2:
		case MONO_TYPE_U4:
		case MONO_TYPE_U8: {
			MonoInst *ins;

			/* No-op */
			MONO_INST_NEW (cfg, ins, OP_XMOVE);
			ins->klass = cmethod->klass;
			ins->type = STACK_VTYPE;
			ins->sreg1 = args [0]->dreg;
			ins->dreg = alloc_xreg (cfg);
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		}
		default:
			break;
		}
		break;
	case SN_op_Addition:
	case SN_op_Subtraction:
	case SN_op_Multiply:
	case SN_op_Division:
	case SN_op_ExclusiveOr:
	case SN_op_BitwiseAnd:
	case SN_op_BitwiseOr:
	case SN_Max:
	case SN_Min: {
		if (!(fsig->param_count == 2 && mono_metadata_type_equal (fsig->ret, fsig->params [0]) && mono_metadata_type_equal (fsig->params [0], fsig->params [1])))
			break;
		int op = 0;
		switch (intrins->name) {
		case SN_op_Addition:
			op = type_to_padd_op (etype);
			break;
		case SN_op_Subtraction:
			op = type_to_psub_op (etype);
			break;
		case SN_op_Multiply:
			op = type_to_pmul_op (etype);
			break;
		case SN_op_Division:
			op = type_to_pdiv_op (etype);
			break;
		case SN_op_ExclusiveOr:
			op = type_to_pxor_op (etype);
			break;
		case SN_op_BitwiseAnd:
			op = type_to_pand_op (etype);
			break;
		case SN_op_BitwiseOr:
			op = type_to_por_op (etype);
			break;
		case SN_Min:
			op = type_to_pmin_op (etype);
			break;
		case SN_Max:
			op = type_to_pmax_op (etype);
			break;
		default:
			g_assert_not_reached ();
		}
		if (op != -1)
			return simd_intrinsic_emit_binary_op (cfg, op, 0, cmethod->klass, fsig->params [0], fsig->params [0], args [0], args [1]);
		break;
	}
	case SN_CopyTo: {
		MonoInst *array_ins = args [1];
		MonoInst *index_ins = args [2];
		MonoInst *ldelema_ins;
		MonoInst *var;
		int end_index_reg;

		if (args [0]->opcode != OP_LDADDR)
			return NULL;

		/* Emit index check for the end (index + len - 1 < array length) */
		end_index_reg = alloc_ireg (cfg);
		EMIT_NEW_BIALU_IMM (cfg, ins, OP_IADD_IMM, end_index_reg, index_ins->dreg, len - 1);

		int length_reg = alloc_ireg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP_FAULT (cfg, OP_LOADI4_MEMBASE, length_reg, array_ins->dreg, MONO_STRUCT_OFFSET (MonoArray, max_length));
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, length_reg, end_index_reg);
		MONO_EMIT_NEW_COND_EXC (cfg, LE_UN, "ArgumentException");

		/* Load the simd reg into the array slice */
		ldelema_ins = mini_emit_ldelema_1_ins (cfg, mono_class_from_mono_type_internal (etype), array_ins, index_ins, TRUE, FALSE);
		g_assert (args [0]->opcode == OP_LDADDR);
		var = (MonoInst*)args [0]->inst_p0;
		EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STOREX_MEMBASE, ldelema_ins->dreg, 0, var->dreg);
		ins->klass = cmethod->klass;
		return args [0];
		break;
	}
	default:
		break;
	}

	assert_handled (cfg, cmethod);

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD method %s not handled.\n", name);
		g_free (name);
	}

	return NULL;
}

/*
 * emit_sys_numerics_intrinsics:
 *
 *   Emit intrinsics for the System.Numerics assembly.
 */
static MonoInst*
emit_sys_numerics_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	const char *nspace = m_class_get_name_space (cmethod->klass);
	const char *class_name = m_class_get_name (cmethod->klass);

	if (!strcmp ("Vector2", class_name) || !strcmp ("Vector4", class_name) || !strcmp ("Vector3", class_name))
		return emit_vector_intrinsics (cfg, cmethod, fsig, args);

	if (!strcmp ("System.Numerics", nspace) && !strcmp ("Vector", class_name)) {
		if (!strcmp (cmethod->name, "get_IsHardwareAccelerated"))
			return emit_vector_is_hardware_accelerated_intrinsic (cfg);
	}

	return NULL;
}

static MonoInst*
emit_sys_numerics_vectors_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	const char *nspace = m_class_get_name_space (cmethod->klass);
	const char *class_name = m_class_get_name (cmethod->klass);

	if (!strcmp (class_name, "Vector`1"))
		return emit_vector_t_intrinsics (cfg, cmethod, fsig, args);

	if (!strcmp ("System.Numerics", nspace) && !strcmp ("Vector", class_name)) {
		if (!strcmp (cmethod->name, "get_IsHardwareAccelerated"))
			return emit_vector_is_hardware_accelerated_intrinsic (cfg);
	}

	return NULL;
}

MonoInst*
mono_emit_simd_field_load (MonoCompile *cfg, MonoClassField *field, MonoInst *addr)
{
	MonoInst * simd_inst = NULL;

	if (is_sys_numerics_assembly (m_class_get_image (field->parent)->assembly)) {
		int index = -1;

		const char *parent_name = m_class_get_name (field->parent);
		if (!strcmp (parent_name, "Vector2") ||
			!strcmp (parent_name, "Vector3") ||
			!strcmp (parent_name, "Vector4")) {
			if (!strcmp (field->name, "X"))
				index = 0;
			else if (!strcmp (field->name, "Y"))
				index = 1;
			else if (!strcmp (field->name, "Z"))
				index = 2;
			else if (!strcmp (field->name, "W"))
				index = 3;
		}

		if (index != -1) {
			if (cfg->verbose_level > 1)
				printf ("  SIMD intrinsic field access: %s\n", field->name);

			simd_inst = simd_intrinsic_emit_getter_op (cfg, index, field->parent, mono_field_get_type_internal (field), addr);
			goto on_exit;
		}
	}

on_exit:

	if (simd_inst != NULL) {
		cfg->uses_simd_intrinsics |= MONO_CFG_USES_SIMD_INTRINSICS;
		cfg->uses_simd_intrinsics |= MONO_CFG_USES_SIMD_INTRINSICS_DECOMPOSE_VTYPE;
	}

	return simd_inst;
}

#endif /* DISABLE_JIT */
MONO_EMPTY_SOURCE_FILE (simd_intrinsics);
#endif /* MONO_ARCH_SIMD_INTRINSICS */
