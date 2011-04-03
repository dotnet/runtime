/*
 * simd-instrisics.c: simd support for intrinsics
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
TODO revamp the .ctor sequence as it looks very fragile, maybe use a var just like iconv_to_r8_raw. (or just pinst sse ops) 
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

#ifdef MONO_ARCH_SIMD_INTRINSICS

//#define IS_DEBUG_ON(cfg) (0)

#define IS_DEBUG_ON(cfg) ((cfg)->verbose_level >= 3)
#define DEBUG(a) do { if (IS_DEBUG_ON(cfg)) { a; } } while (0)
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

#ifdef HAVE_ARRAY_ELEM_INIT
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

#else
#define SIMD_METHOD(str,name) str,
static const char * const method_names [] = {
#include "simd-methods.h"
	NULL
};
#undef SIMD_METHOD
#define SIMD_METHOD(str,name) name,
enum {
#include "simd-methods.h"
	SN_LAST
};

#define method_name(idx) (method_names [(idx)])

#endif

typedef struct {
	guint16 name;
	guint16 opcode;
	guint8 simd_version_flags;
	guint8 simd_emit_mode : 4;
	guint8 flags : 4;
} SimdIntrinsc;

static const SimdIntrinsc vector4f_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_R4, SIMD_VERSION_SSE1, SIMD_EMIT_CTOR },
	{ SN_AddSub, OP_ADDSUBPS, SIMD_VERSION_SSE3, SIMD_EMIT_BINARY},
	{ SN_AndNot, OP_ANDNPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY},
	{ SN_CompareEqual, OP_COMPPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_EQ },
	{ SN_CompareLessEqual, OP_COMPPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_LE },
	{ SN_CompareLessThan, OP_COMPPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_LT },
	{ SN_CompareNotEqual, OP_COMPPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_NEQ },
	{ SN_CompareNotLessEqual, OP_COMPPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_NLE },
	{ SN_CompareNotLessThan, OP_COMPPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_NLT },
	{ SN_CompareOrdered, OP_COMPPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_ORD },
	{ SN_CompareUnordered, OP_COMPPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_UNORD },
	{ SN_ConvertToDouble, OP_CVTPS2PD, SIMD_VERSION_SSE2, SIMD_EMIT_UNARY },
	{ SN_ConvertToInt, OP_CVTPS2DQ, SIMD_VERSION_SSE2, SIMD_EMIT_UNARY },
	{ SN_ConvertToIntTruncated, OP_CVTTPS2DQ, SIMD_VERSION_SSE2, SIMD_EMIT_UNARY },
	{ SN_DuplicateHigh, OP_DUPPS_HIGH, SIMD_VERSION_SSE3, SIMD_EMIT_UNARY },
	{ SN_DuplicateLow, OP_DUPPS_LOW, SIMD_VERSION_SSE3, SIMD_EMIT_UNARY },
	{ SN_HorizontalAdd, OP_HADDPS, SIMD_VERSION_SSE3, SIMD_EMIT_BINARY },
	{ SN_HorizontalSub, OP_HSUBPS, SIMD_VERSION_SSE3, SIMD_EMIT_BINARY },	
	{ SN_InterleaveHigh, OP_UNPACK_HIGHPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_InterleaveLow, OP_UNPACK_LOWPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_InvSqrt, OP_RSQRTPS, SIMD_VERSION_SSE1, SIMD_EMIT_UNARY },
	{ SN_LoadAligned, 0, SIMD_VERSION_SSE1, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_MAXPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_Min, OP_MINPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Reciprocal, OP_RCPPS, SIMD_VERSION_SSE1, SIMD_EMIT_UNARY },
	{ SN_Shuffle, OP_PSHUFLED, SIMD_VERSION_SSE1, SIMD_EMIT_SHUFFLE },
	{ SN_Sqrt, OP_SQRTPS, SIMD_VERSION_SSE1, SIMD_EMIT_UNARY },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_StoreNonTemporal, OP_STOREX_NTA_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_get_W, 3, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_Z, 2, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_ADDPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_ANDPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_ORPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Division, OP_DIVPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_COMPPS, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_XORPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_VERSION_SSE1, SIMD_EMIT_CAST }, 
	{ SN_op_Inequality, OP_COMPPS, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_Multiply, OP_MULPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Subtraction, OP_SUBPS, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_set_W, 3, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_Z, 2, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER }
};

static const SimdIntrinsc vector2d_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_R8, SIMD_VERSION_SSE1, SIMD_EMIT_CTOR },
	{ SN_AddSub, OP_ADDSUBPD, SIMD_VERSION_SSE3, SIMD_EMIT_BINARY,},
	{ SN_AndNot, OP_ANDNPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_COMPPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_EQ },
	{ SN_CompareLessEqual, OP_COMPPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_LE },
	{ SN_CompareLessThan, OP_COMPPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_LT },
	{ SN_CompareNotEqual, OP_COMPPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_NEQ },
	{ SN_CompareNotLessEqual, OP_COMPPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_NLE },
	{ SN_CompareNotLessThan, OP_COMPPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_NLT },
	{ SN_CompareOrdered, OP_COMPPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_ORD },
	{ SN_CompareUnordered, OP_COMPPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_COMP_UNORD },
	{ SN_ConvertToFloat, OP_CVTPD2PS, SIMD_VERSION_SSE2, SIMD_EMIT_UNARY },
	{ SN_ConvertToInt, OP_CVTPD2DQ, SIMD_VERSION_SSE2, SIMD_EMIT_UNARY },
	{ SN_ConvertToIntTruncated, OP_CVTTPD2DQ, SIMD_VERSION_SSE2, SIMD_EMIT_UNARY },
	{ SN_Duplicate, OP_DUPPD, SIMD_VERSION_SSE3, SIMD_EMIT_UNARY },
	{ SN_HorizontalAdd, OP_HADDPD, SIMD_VERSION_SSE3, SIMD_EMIT_BINARY },
	{ SN_HorizontalSub, OP_HSUBPD, SIMD_VERSION_SSE3, SIMD_EMIT_BINARY },	
	{ SN_InterleaveHigh, OP_UNPACK_HIGHPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_InterleaveLow, OP_UNPACK_LOWPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, SIMD_VERSION_SSE1, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_MAXPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_Min, OP_MINPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Shuffle, OP_SHUFPD, SIMD_VERSION_SSE1, SIMD_EMIT_SHUFFLE },
	{ SN_Sqrt, OP_SQRTPD, SIMD_VERSION_SSE1, SIMD_EMIT_UNARY },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_get_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER_QWORD },
	{ SN_get_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER_QWORD },
	{ SN_op_Addition, OP_ADDPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_ANDPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_ORPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Division, OP_DIVPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_XORPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_VERSION_SSE1, SIMD_EMIT_CAST }, 
	{ SN_op_Multiply, OP_MULPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Subtraction, OP_SUBPD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_set_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
};

static const SimdIntrinsc vector2ul_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I8, SIMD_VERSION_SSE1, SIMD_EMIT_CTOR },
	{ SN_CompareEqual, OP_PCMPEQQ, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, SIMD_VERSION_SSE1, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Shuffle, OP_SHUFPD, SIMD_VERSION_SSE1, SIMD_EMIT_SHUFFLE },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHQ, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWQ, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_get_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER_QWORD },
	{ SN_get_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER_QWORD },
	{ SN_op_Addition, OP_PADDQ, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1 },
	{ SN_op_Explicit, 0, SIMD_VERSION_SSE1, SIMD_EMIT_CAST },
	{ SN_op_LeftShift, OP_PSHLQ, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULQ, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSHRQ, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBQ, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_set_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
};

static const SimdIntrinsc vector2l_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I8, SIMD_VERSION_SSE1, SIMD_EMIT_CTOR },
	{ SN_CompareEqual, OP_PCMPEQQ, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTQ, SIMD_VERSION_SSE42, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, SIMD_VERSION_SSE1, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_LogicalRightShift, OP_PSHRQ, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Shuffle, OP_SHUFPD, SIMD_VERSION_SSE1, SIMD_EMIT_SHUFFLE },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHQ, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWQ, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_get_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER_QWORD },
	{ SN_get_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER_QWORD },
	{ SN_op_Addition, OP_PADDQ, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_VERSION_SSE1, SIMD_EMIT_CAST },
	{ SN_op_LeftShift, OP_PSHLQ, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULQ, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Subtraction, OP_PSUBQ, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_set_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
};

static const SimdIntrinsc vector4ui_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I4, SIMD_VERSION_SSE1, SIMD_EMIT_CTOR },
	{ SN_ArithmeticRightShift, OP_PSARD, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_CompareEqual, OP_PCMPEQD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, SIMD_VERSION_SSE1, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXD_UN, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMIND_UN, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Shuffle, OP_PSHUFLED, SIMD_VERSION_SSE1, SIMD_EMIT_SHUFFLE },
	{ SN_SignedPackWithSignedSaturation, OP_PACKD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_SignedPackWithUnsignedSaturation, OP_PACKD_UN, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_get_W, 3, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_Z, 2, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQD, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_VERSION_SSE1, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQD, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_LeftShift, OP_PSHLD, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULD, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSHRD, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_set_W, 3, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_Z, 2, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
};

static const SimdIntrinsc vector4i_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I4, SIMD_VERSION_SSE1, SIMD_EMIT_CTOR },
	{ SN_CompareEqual, OP_PCMPEQD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_ConvertToDouble, OP_CVTDQ2PD, SIMD_VERSION_SSE2, SIMD_EMIT_UNARY },
	{ SN_ConvertToFloat, OP_CVTDQ2PS, SIMD_VERSION_SSE2, SIMD_EMIT_UNARY },
	{ SN_LoadAligned, 0, SIMD_VERSION_SSE1, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_LogicalRightShift, OP_PSHRD, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_Max, OP_PMAXD, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMIND, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_PackWithSignedSaturation, OP_PACKD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_PackWithUnsignedSaturation, OP_PACKD_UN, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_Shuffle, OP_PSHUFLED, SIMD_VERSION_SSE1, SIMD_EMIT_SHUFFLE },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_get_W, 3, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_Z, 2, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQD, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_VERSION_SSE1, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQD, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_LeftShift, OP_PSHLD, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULD, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSARD, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBD, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_set_W, 3, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_X, 0, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_Y, 1, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_Z, 2, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
};

static const SimdIntrinsc vector8us_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I2, SIMD_VERSION_SSE1, SIMD_EMIT_CTOR },
	{ SN_AddWithSaturation, OP_PADDW_SAT_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_ArithmeticRightShift, OP_PSARW, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_Average, OP_PAVGW_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1 },
	{ SN_LoadAligned, 0, SIMD_VERSION_SSE1, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXW_UN, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMINW_UN, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_MultiplyStoreHigh, OP_PMULW_HIGH_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_ShuffleHigh, OP_PSHUFLEW_HIGH, SIMD_VERSION_SSE1, SIMD_EMIT_SHUFFLE },
	{ SN_ShuffleLow, OP_PSHUFLEW_LOW, SIMD_VERSION_SSE1, SIMD_EMIT_SHUFFLE },
	{ SN_SignedPackWithSignedSaturation, OP_PACKW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_SignedPackWithUnsignedSaturation, OP_PACKW_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_SubtractWithSaturation, OP_PSUBW_SAT_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_get_V0, 0, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V1, 1, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V2, 2, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V3, 3, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V4, 4, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V5, 5, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V6, 6, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V7, 7, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQW, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_VERSION_SSE1, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQW, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_LeftShift, OP_PSHLW, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSHRW, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_set_V0, 0, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V1, 1, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V2, 2, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V3, 3, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V4, 4, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V5, 5, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V6, 6, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V7, 7, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
};

static const SimdIntrinsc vector8s_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I2, SIMD_VERSION_SSE1, SIMD_EMIT_CTOR },
	{ SN_AddWithSaturation, OP_PADDW_SAT, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, SIMD_VERSION_SSE1, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_LogicalRightShift, OP_PSHRW, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_Max, OP_PMAXW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMINW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_MultiplyStoreHigh, OP_PMULW_HIGH, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_PackWithSignedSaturation, OP_PACKW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_PackWithUnsignedSaturation, OP_PACKW_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_ShuffleHigh, OP_PSHUFLEW_HIGH, SIMD_VERSION_SSE1, SIMD_EMIT_SHUFFLE },
	{ SN_ShuffleLow, OP_PSHUFLEW_LOW, SIMD_VERSION_SSE1, SIMD_EMIT_SHUFFLE },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_SubtractWithSaturation, OP_PSUBW_SAT_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_get_V0, 0, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V1, 1, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V2, 2, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V3, 3, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V4, 4, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V5, 5, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V6, 6, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V7, 7, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQW, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_VERSION_SSE1, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQW, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_LeftShift, OP_PSHLW, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSARW, SIMD_VERSION_SSE1, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBW, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_set_V0, 0, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V1, 1, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V2, 2, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V3, 3, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V4, 4, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V5, 5, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V6, 6, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V7, 7, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
};

static const SimdIntrinsc vector16b_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I1, SIMD_VERSION_SSE1, SIMD_EMIT_CTOR },
	{ SN_AddWithSaturation, OP_PADDB_SAT_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_Average, OP_PAVGB_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_ExtractByteMask, 0, SIMD_VERSION_SSE1, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, SIMD_VERSION_SSE1, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXB_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMINB_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_SubtractWithSaturation, OP_PSUBB_SAT_UN, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_SumOfAbsoluteDifferences, OP_PSUM_ABS_DIFF, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_get_V0, 0, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V1, 1, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V10, 10, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V11, 11, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V12, 12, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V13, 13, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V14, 14, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V15, 15, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V2, 2, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V3, 3, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V4, 4, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V5, 5, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V6, 6, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V7, 7, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V8, 8, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V9, 9, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQB, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_VERSION_SSE1, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQB, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_Subtraction, OP_PSUBB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_set_V0, 0, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V1, 1, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V10, 10, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V11, 11, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V12, 12, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V13, 13, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V14, 14, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V15, 15, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V2, 2, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V3, 3, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V4, 4, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V5, 5, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V6, 6, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V7, 7, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V8, 8, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V9, 9, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
};

/*
Missing:
setters
 */
static const SimdIntrinsc vector16sb_intrinsics[] = {
	{ SN_ctor, OP_EXPAND_I1, SIMD_VERSION_SSE1, SIMD_EMIT_CTOR },
	{ SN_AddWithSaturation, OP_PADDB_SAT, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_ExtractByteMask, 0, SIMD_VERSION_SSE1, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, SIMD_VERSION_SSE1, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXB, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMINB, SIMD_VERSION_SSE41, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_VERSION_SSE1, SIMD_EMIT_PREFETCH, SIMD_PREFETCH_MODE_NTA },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_VERSION_SSE1, SIMD_EMIT_STORE },
	{ SN_SubtractWithSaturation, OP_PSUBB_SAT, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_get_V0, 0, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V1, 1, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V10, 10, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V11, 11, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V12, 12, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V13, 13, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V14, 14, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V15, 15, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V2, 2, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V3, 3, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V4, 4, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V5, 5, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V6, 6, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V7, 7, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V8, 8, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_get_V9, 9, SIMD_VERSION_SSE1, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_PADDB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Equality, OP_PCMPEQB, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_EQ },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_VERSION_SSE1, SIMD_EMIT_CAST },
	{ SN_op_Inequality, OP_PCMPEQB, SIMD_VERSION_SSE1, SIMD_EMIT_EQUALITY, SIMD_COMP_NEQ },
	{ SN_op_Subtraction, OP_PSUBB, SIMD_VERSION_SSE1, SIMD_EMIT_BINARY },
	{ SN_set_V0, 0, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V1, 1, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V10, 10, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V11, 11, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V12, 12, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V13, 13, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V14, 14, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V15, 15, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V2, 2, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V3, 3, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V4, 4, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V5, 5, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V6, 6, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V7, 7, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V8, 8, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
	{ SN_set_V9, 9, SIMD_VERSION_SSE1, SIMD_EMIT_SETTER },
};

static guint32 simd_supported_versions;

/*TODO match using number of parameters as well*/
static int
simd_intrinsic_compare_by_name (const void *key, const void *value)
{
	return strcmp (key, method_name (((SimdIntrinsc *)value)->name));
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
	simd_supported_versions = mono_arch_cpu_enumerate_simd_versions ();
	/*TODO log the supported flags*/
}

static inline gboolean
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

static inline gboolean
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
		if (var->klass->simd_type) {
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
				if (var->klass->simd_type) {
					var->flags |= MONO_INST_INDIRECT;
				}
			}
		}
	}

	DEBUG (printf ("[simd-simplify] max vreg is %d\n", max_vreg));
	vreg_flags = g_malloc0 (max_vreg + 1);
	target_bb = g_new0 (MonoBasicBlock*, max_vreg + 1);

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *var = cfg->varinfo [i];
		if (var->klass->simd_type && !(var->flags & (MONO_INST_INDIRECT|MONO_INST_VOLATILE))) {
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
			if (var->klass->simd_type) {
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
		if (!var->klass->simd_type)
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
				if (sregs [i] == var->dreg)
					found = TRUE;
			}
			/*We can avoid inserting the XZERO if the first use doesn't depend on the zero'ed value.*/
			if (ins->dreg == var->dreg && !found) {
				break;
			} else if (found) {
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
		if (ins->opcode == OP_XZERO && (vreg_flags [ins->dreg] & VREG_SINGLE_BB_USE))
			NULLIFY_INS (ins);
	}

	g_free (vreg_flags);
	g_free (target_bb);
}

/*
 * This function expect that src be a value.
 */
static int
get_simd_vreg (MonoCompile *cfg, MonoMethod *cmethod, MonoInst *src)
{
	if (src->opcode == OP_XMOVE) {
		return src->sreg1;
	} else if (src->type == STACK_VTYPE) {
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
load_simd_vreg (MonoCompile *cfg, MonoMethod *cmethod, MonoInst *src, gboolean *indirect)
{
	if (indirect)
		*indirect = FALSE;
	if (src->opcode == OP_XMOVE) {
		return src->sreg1;
	} else if (src->opcode == OP_LDADDR) {
		int res = ((MonoInst*)src->inst_p0)->dreg;
		NULLIFY_INS (src);
		return res;
	} else if (src->type == STACK_VTYPE) {
		return src->dreg;
	} else if (src->type == STACK_PTR || src->type == STACK_MP) {
		MonoInst *ins;
		if (indirect)
			*indirect = TRUE;

		MONO_INST_NEW (cfg, ins, OP_LOADX_MEMBASE);
		ins->klass = cmethod->klass;
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

static MonoInst*
get_int_to_float_spill_area (MonoCompile *cfg)
{
	if (!cfg->iconv_raw_var) {
		cfg->iconv_raw_var = mono_compile_create_var (cfg, &mono_defaults.int32_class->byval_arg, OP_LOCAL);
		cfg->iconv_raw_var->flags |= MONO_INST_VOLATILE; /*FIXME, use the don't regalloc flag*/
	}	
	return cfg->iconv_raw_var;
}

/*We share the var with fconv_to_r8_x to save some stack space.*/
static MonoInst*
get_double_spill_area (MonoCompile *cfg)
{
	if (!cfg->fconv_to_r8_x_var) {
		cfg->fconv_to_r8_x_var = mono_compile_create_var (cfg, &mono_defaults.double_class->byval_arg, OP_LOCAL);
		cfg->fconv_to_r8_x_var->flags |= MONO_INST_VOLATILE; /*FIXME, use the don't regalloc flag*/
	}	
	return cfg->fconv_to_r8_x_var;
}
static MonoInst*
get_simd_ctor_spill_area (MonoCompile *cfg, MonoClass *avector_klass)
{
	if (!cfg->simd_ctor_var) {
		cfg->simd_ctor_var = mono_compile_create_var (cfg, &avector_klass->byval_arg, OP_LOCAL);
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
get_simd_vreg_or_expanded_scalar (MonoCompile *cfg, MonoMethod *cmethod, MonoInst *src, int position)
{
	MonoInst *ins;
	MonoMethodSignature *sig = mono_method_signature (cmethod);
	int expand_op;

	g_assert (sig->param_count == 2);
	g_assert (position == 0 || position == 1);

	if (mono_class_from_mono_type (sig->params [position])->simd_type)
		return get_simd_vreg (cfg, cmethod, src);

	expand_op = mono_type_to_expand_op (sig->params [position]);
	MONO_INST_NEW (cfg, ins, expand_op);
	ins->klass = cmethod->klass;
	ins->sreg1 = src->dreg;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);

	if (expand_op == OP_EXPAND_R4)
		ins->backend.spill_var = get_int_to_float_spill_area (cfg);
	else if (expand_op == OP_EXPAND_R8)
		ins->backend.spill_var = get_double_spill_area (cfg);

	return ins->dreg;
}

static MonoInst*
simd_intrinsic_emit_binary (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst* ins;
	int left_vreg, right_vreg;

	left_vreg = get_simd_vreg_or_expanded_scalar (cfg, cmethod, args [0], 0);
	right_vreg = get_simd_vreg_or_expanded_scalar (cfg, cmethod, args [1], 1);


	MONO_INST_NEW (cfg, ins, intrinsic->opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = left_vreg;
	ins->sreg2 = right_vreg;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	ins->inst_c0 = intrinsic->flags;
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_unary (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
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
mono_type_to_extract_op (MonoType *type)
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
	case MONO_TYPE_R4:
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
simd_intrinsic_emit_setter (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	MonoMethodSignature *sig = mono_method_signature (cmethod);
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
			ins->backend.spill_var = get_int_to_float_spill_area (cfg);
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

static MonoInst*
simd_intrinsic_emit_getter (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	MonoMethodSignature *sig = mono_method_signature (cmethod);
	int vreg, shift_bits = mono_type_elements_shift_bits (sig->ret);

	vreg = load_simd_vreg (cfg, cmethod, args [0], NULL);

	if ((intrinsic->opcode >> shift_bits) && !cfg->compile_llvm) {
		MONO_INST_NEW (cfg, ins, OP_PSHUFLED);
		ins->klass = cmethod->klass;
		ins->sreg1 = vreg;
		ins->inst_c0 = intrinsic->opcode >> shift_bits;
		ins->type = STACK_VTYPE;
		ins->dreg = vreg = alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, ins);
	}

	MONO_INST_NEW (cfg, ins, mono_type_to_extract_op (sig->ret));
	ins->klass = cmethod->klass;
	ins->sreg1 = vreg;
	ins->type = STACK_I4;
	ins->dreg = vreg = alloc_ireg (cfg);
	if (cfg->compile_llvm)
		ins->inst_c0 = intrinsic->opcode;
	else
		ins->inst_c0 = intrinsic->opcode & ((1 << shift_bits) - 1);
	MONO_ADD_INS (cfg->cbb, ins);

	if (sig->ret->type == MONO_TYPE_R4) {
		MONO_INST_NEW (cfg, ins, OP_ICONV_TO_R8_RAW);
		ins->klass = mono_defaults.single_class;
		ins->sreg1 = vreg;
		ins->type = STACK_R8;
		ins->dreg = alloc_freg (cfg);
		ins->backend.spill_var = get_int_to_float_spill_area (cfg);
		MONO_ADD_INS (cfg->cbb, ins);	
	}
	return ins;
}

static MonoInst*
simd_intrinsic_emit_long_getter (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg;
	gboolean is_r8 = mono_method_signature (cmethod)->ret->type == MONO_TYPE_R8;

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
simd_intrinsic_emit_ctor (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins = NULL;
	int i, addr_reg;
	gboolean is_ldaddr = args [0]->opcode == OP_LDADDR;
	MonoMethodSignature *sig = mono_method_signature (cmethod);
	int store_op = mono_type_to_store_membase (cfg, sig->params [0]);
	int arg_size = mono_type_size (sig->params [0], &i);

	if (sig->param_count == 1) {
		int dreg;
		
		if (is_ldaddr) {
			dreg = args [0]->inst_i0->dreg;
			NULLIFY_INS (args [0]);
		} else {
			g_assert (args [0]->type == STACK_MP || args [0]->type == STACK_PTR);
			dreg = alloc_ireg (cfg);
		}

		MONO_INST_NEW (cfg, ins, intrinsic->opcode);
		ins->klass = cmethod->klass;
		ins->sreg1 = args [1]->dreg;
		ins->type = STACK_VTYPE;
		ins->dreg = dreg;

		MONO_ADD_INS (cfg->cbb, ins);
		if (sig->params [0]->type == MONO_TYPE_R4)
			ins->backend.spill_var = get_int_to_float_spill_area (cfg);
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
simd_intrinsic_emit_cast (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg;

	vreg = get_simd_vreg (cfg, cmethod, args [0]);		

	//TODO macroize this
	MONO_INST_NEW (cfg, ins, OP_XMOVE);
	ins->klass = cmethod->klass;
	ins->type = STACK_VTYPE;
	ins->sreg1 = vreg;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_shift (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
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

static inline gboolean
mono_op_is_packed_compare (int op)
{
	return op >= OP_PCMPEQB && op <= OP_PCMPEQQ;
}

static MonoInst*
simd_intrinsic_emit_equality (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst* ins;
	int left_vreg, right_vreg, tmp_vreg;

	left_vreg = get_simd_vreg (cfg, cmethod, args [0]);
	right_vreg = get_simd_vreg (cfg, cmethod, args [1]);
	

	MONO_INST_NEW (cfg, ins, intrinsic->opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = left_vreg;
	ins->sreg2 = right_vreg;
	ins->type = STACK_VTYPE;
	ins->klass = cmethod->klass;
	ins->dreg = tmp_vreg = alloc_ireg (cfg);
	ins->inst_c0 = intrinsic->flags;
	MONO_ADD_INS (cfg->cbb, ins);

	/*FIXME the next ops are SSE specific*/
	MONO_INST_NEW (cfg, ins, OP_EXTRACT_MASK);
	ins->klass = cmethod->klass;
	ins->sreg1 = tmp_vreg;
	ins->type = STACK_I4;
	ins->dreg = tmp_vreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);

	/*FP ops have a not equal instruction, which means that we must test the results with OR semantics.*/
	if (mono_op_is_packed_compare (intrinsic->opcode) || intrinsic->flags == SIMD_COMP_EQ) {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_vreg, 0xFFFF);
		NEW_UNALU (cfg, ins, intrinsic->flags == SIMD_COMP_EQ ? OP_CEQ : OP_CLT_UN, tmp_vreg, -1);
	} else {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_vreg, 0);
		NEW_UNALU (cfg, ins, OP_CGT_UN, tmp_vreg, -1);
	}
	MONO_ADD_INS (cfg->cbb, ins);	
	return ins;
}


static MonoInst*
simd_intrinsic_emit_shuffle (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg, vreg2 = -1;
	int param_count = mono_method_signature (cmethod)->param_count;

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
simd_intrinsic_emit_load_aligned (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
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
simd_intrinsic_emit_store (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
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
simd_intrinsic_emit_extract_mask (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
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
simd_intrinsic_emit_prefetch (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, OP_PREFETCH_MEMBASE);
	ins->klass = cmethod->klass;
	ins->sreg1 = args [0]->dreg;
	ins->backend.arg_info = intrinsic->flags;
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static const char *
simd_version_name (guint32 version)
{
	switch (version) {
	case SIMD_VERSION_SSE1:
		return "sse1";
	case SIMD_VERSION_SSE2:
		return "sse2";
	case SIMD_VERSION_SSE3:
		return "sse3";
	case SIMD_VERSION_SSSE3:
		return "ssse3";
	case SIMD_VERSION_SSE41:
		return "sse41";
	case SIMD_VERSION_SSE42:
		return "sse42";
	case SIMD_VERSION_SSE4a:
		return "sse4a";
	}
	return "n/a";
}

static MonoInst*
emit_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args, const SimdIntrinsc *intrinsics, guint32 size)
{
	const SimdIntrinsc * result = bsearch (cmethod->name, intrinsics, size, sizeof (SimdIntrinsc), &simd_intrinsic_compare_by_name);
	if (!result) {
		DEBUG (printf ("function doesn't have a simd intrinsic %s::%s/%d\n", cmethod->klass->name, cmethod->name, fsig->param_count));
		return NULL;
	}
	if (IS_DEBUG_ON (cfg)) {
		int i, max;
		printf ("found call to intrinsic %s::%s/%d -> %s\n", cmethod->klass->name, cmethod->name, fsig->param_count, method_name (result->name));
		max = fsig->param_count + fsig->hasthis;
		for (i = 0; i < max; ++i) {
			printf ("param %d:  ", i);
			mono_print_ins (args [i]);
		}
	}
	if (result->simd_version_flags && !(result->simd_version_flags & simd_supported_versions)) {
		if (IS_DEBUG_ON (cfg)) {
			int x;
			printf ("function %s::%s/%d requires one of unsuported SIMD instruction set(s): ", cmethod->klass->name, cmethod->name, fsig->param_count);
			for (x = 1; x <= SIMD_VERSION_INDEX_END; x++)
				if (result->simd_version_flags & (1 << x))
					printf ("%s ", simd_version_name (1 << x));

			printf ("\n");
		}
		return NULL;
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

	size = mono_array_element_size (mono_class_from_mono_type (array_type));
	mult_reg = alloc_preg (cfg);
	array_reg = arr->dreg;
	index_reg = index->dreg;

#if SIZEOF_VOID_P == 8
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
	NEW_BIALU_IMM (cfg, ins, OP_PADD_IMM, add_reg, add_reg, G_STRUCT_OFFSET (MonoArray, vector));
	ins->type = STACK_PTR;
	MONO_ADD_INS (cfg->cbb, ins);

	return add_reg;
}

static MonoInst*
emit_array_extension_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (!strcmp ("GetVector", cmethod->name) || !strcmp ("GetVectorAligned", cmethod->name)) {
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
	if (!strcmp ("SetVector", cmethod->name) || !strcmp ("SetVectorAligned", cmethod->name)) {
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
	if (!strcmp ("IsAligned", cmethod->name)) {
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

static MonoInst*
emit_simd_runtime_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (!strcmp ("get_AccelMode", cmethod->name)) {
		MonoInst *ins;
		EMIT_NEW_ICONST (cfg, ins, simd_supported_versions);
		return ins;
	}
	return NULL;
}

MonoInst*
mono_emit_simd_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	const char *class_name;

	if (strcmp ("Mono.Simd", cmethod->klass->name_space))
		return NULL;

	class_name = cmethod->klass->name;
	if (!strcmp ("SimdRuntime", class_name))
		return emit_simd_runtime_intrinsics (cfg, cmethod, fsig, args);

	if (!strcmp ("ArrayExtensions", class_name))
		return emit_array_extension_intrinsics (cfg, cmethod, fsig, args);
	
	if (!strcmp ("VectorOperations", class_name)) {
		if (!(cmethod->flags & METHOD_ATTRIBUTE_STATIC))
			return NULL;
		class_name = mono_class_from_mono_type (mono_method_signature (cmethod)->params [0])->name;
	} else if (!cmethod->klass->simd_type)
		return NULL;

	cfg->uses_simd_intrinsics = 1;
	if (!strcmp ("Vector2d", class_name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector2d_intrinsics, sizeof (vector2d_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector4f", class_name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector4f_intrinsics, sizeof (vector4f_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector2ul", class_name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector2ul_intrinsics, sizeof (vector2ul_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector2l", class_name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector2l_intrinsics, sizeof (vector2l_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector4ui", class_name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector4ui_intrinsics, sizeof (vector4ui_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector4i", class_name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector4i_intrinsics, sizeof (vector4i_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector8us", class_name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector8us_intrinsics, sizeof (vector8us_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector8s", class_name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector8s_intrinsics, sizeof (vector8s_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector16b", class_name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector16b_intrinsics, sizeof (vector16b_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector16sb", class_name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector16sb_intrinsics, sizeof (vector16sb_intrinsics) / sizeof (SimdIntrinsc));

	return NULL;
}

#endif
