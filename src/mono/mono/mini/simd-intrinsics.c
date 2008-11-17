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

#define NEW_IR
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
	SIMD_EMIT_GETTER,
	SIMD_EMIT_CTOR,
	SIMD_EMIT_CAST,
	SIMD_EMIT_SHUFFLE,
	SIMD_EMIT_SHIFT,
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
	guint8 simd_emit_mode : 4;
	guint8 simd_version : 4;
	guint8 flags;
} SimdIntrinsc;

/*
Missing:
setters
 */
static const SimdIntrinsc vector4f_intrinsics[] = {
	{ SN_ctor, 0, SIMD_EMIT_CTOR },
	{ SN_AddSub, OP_ADDSUBPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE3 },
	{ SN_AndNot, OP_ANDNPS, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_COMPPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_EQ },
	{ SN_CompareLessEqual, OP_COMPPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_LE },
	{ SN_CompareLessThan, OP_COMPPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_LT },
	{ SN_CompareNotEqual, OP_COMPPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_NEQ },
	{ SN_CompareNotLessEqual, OP_COMPPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_NLE },
	{ SN_CompareNotLessThan, OP_COMPPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_NLT },
	{ SN_CompareOrdered, OP_COMPPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_ORD },
	{ SN_CompareUnordered, OP_COMPPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_UNORD },
	{ SN_DuplicateHigh, OP_DUPPS_HIGH, SIMD_EMIT_UNARY, SIMD_VERSION_SSE3 },
	{ SN_DuplicateLow, OP_DUPPS_LOW, SIMD_EMIT_UNARY, SIMD_VERSION_SSE3 },
	{ SN_HorizontalAdd, OP_HADDPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE3 },
	{ SN_HorizontalSub, OP_HSUBPS, SIMD_EMIT_BINARY, SIMD_VERSION_SSE3 },	
	{ SN_InterleaveHigh, OP_UNPACK_HIGHPS, SIMD_EMIT_BINARY },
	{ SN_InterleaveLow, OP_UNPACK_LOWPS, SIMD_EMIT_BINARY },
	{ SN_InvSqrt, OP_RSQRTPS, SIMD_EMIT_UNARY },
	{ SN_LoadAligned, 0, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_MAXPS, SIMD_EMIT_BINARY },
	{ SN_Min, OP_MINPS, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_NTA },
	{ SN_Reciprocal, OP_RCPPS, SIMD_EMIT_UNARY },
	{ SN_Shuffle, OP_SHUFLEPS, SIMD_EMIT_SHUFFLE },
	{ SN_Sqrt, OP_SQRTPS, SIMD_EMIT_UNARY },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_StoreNonTemporal, OP_STOREX_NTA_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_get_W, 3, SIMD_EMIT_GETTER },
	{ SN_get_X, 0, SIMD_EMIT_GETTER },
	{ SN_get_Y, 1, SIMD_EMIT_GETTER },
	{ SN_get_Z, 2, SIMD_EMIT_GETTER },
	{ SN_op_Addition, OP_ADDPS, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_ANDPS, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_ORPS, SIMD_EMIT_BINARY },
	{ SN_op_Division, OP_DIVPS, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_XORPS, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_EMIT_CAST }, 
	{ SN_op_Multiply, OP_MULPS, SIMD_EMIT_BINARY },
	{ SN_op_Subtraction, OP_SUBPS, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector2d_intrinsics[] = {
	{ SN_AddSub, OP_ADDSUBPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE3 },
	{ SN_AndNot, OP_ANDNPD, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_COMPPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_EQ },
	{ SN_CompareLessEqual, OP_COMPPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_LE },
	{ SN_CompareLessThan, OP_COMPPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_LT },
	{ SN_CompareNotEqual, OP_COMPPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_NEQ },
	{ SN_CompareNotLessEqual, OP_COMPPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_NLE },
	{ SN_CompareNotLessThan, OP_COMPPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_NLT },
	{ SN_CompareOrdered, OP_COMPPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_ORD },
	{ SN_CompareUnordered, OP_COMPPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE1, SIMD_COMP_UNORD },
	{ SN_Duplicate, OP_DUPPD, SIMD_EMIT_UNARY, SIMD_VERSION_SSE3 },
	{ SN_HorizontalAdd, OP_HADDPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE3 },
	{ SN_HorizontalSub, OP_HSUBPD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE3 },	
	{ SN_InterleaveHigh, OP_UNPACK_HIGHPD, SIMD_EMIT_BINARY },
	{ SN_InterleaveLow, OP_UNPACK_LOWPD, SIMD_EMIT_BINARY },
	{ SN_LoadAligned, 0, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_MAXPD, SIMD_EMIT_BINARY },
	{ SN_Min, OP_MINPD, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_NTA },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_op_Addition, OP_ADDPD, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_ANDPD, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_ORPD, SIMD_EMIT_BINARY },
	{ SN_op_Division, OP_DIVPD, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_XORPD, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_EMIT_CAST }, 
	{ SN_op_Multiply, OP_MULPD, SIMD_EMIT_BINARY },
	{ SN_op_Subtraction, OP_SUBPD, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector2ul_intrinsics[] = {
	{ SN_CompareEqual, OP_PCMPEQQ, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_ExtractByteMask, 0, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_NTA },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHQ, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWQ, SIMD_EMIT_BINARY },
	{ SN_op_Addition, OP_PADDQ, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_EMIT_CAST },
	{ SN_op_LeftShift, OP_PSHLQ, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULQ, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSHRQ, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBQ, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector2l_intrinsics[] = {
	{ SN_CompareEqual, OP_PCMPEQQ, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_CompareGreaterThan, OP_PCMPGTQ, SIMD_EMIT_BINARY, SIMD_VERSION_SSE42 },
	{ SN_ExtractByteMask, 0, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_NTA },
	{ SN_ShiftRightLogic, OP_PSHRQ, SIMD_EMIT_SHIFT },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHQ, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWQ, SIMD_EMIT_BINARY },
	{ SN_op_Addition, OP_PADDQ, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_EMIT_CAST },
	{ SN_op_LeftShift, OP_PSHLQ, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULQ, SIMD_EMIT_BINARY },
	{ SN_op_Subtraction, OP_PSUBQ, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector4ui_intrinsics[] = {
	{ SN_CompareEqual, OP_PCMPEQD, SIMD_EMIT_BINARY },
	{ SN_ExtractByteMask, 0, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXD_UN, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_Min, OP_PMIND_UN, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_NTA },
	{ SN_ShiftRightArithmetic, OP_PSARD, SIMD_EMIT_SHIFT },
	{ SN_Shuffle, OP_PSHUFLED, SIMD_EMIT_SHUFFLE },
	{ SN_SignedPackWithSignedSaturation, OP_PACKD, SIMD_EMIT_BINARY },
	{ SN_SignedPackWithUnsignedSaturation, OP_PACKD_UN, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHD, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWD, SIMD_EMIT_BINARY },
	{ SN_op_Addition, OP_PADDD, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_EMIT_CAST },
	{ SN_op_LeftShift, OP_PSHLD, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_op_RightShift, OP_PSHRD, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBD, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector4i_intrinsics[] = {
	{ SN_CompareEqual, OP_PCMPEQD, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTD, SIMD_EMIT_BINARY },
	{ SN_ExtractByteMask, 0, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_Min, OP_PMIND, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_PackWithSignedSaturation, OP_PACKD, SIMD_EMIT_BINARY },
	{ SN_PackWithUnsignedSaturation, OP_PACKD_UN, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_NTA },
	{ SN_ShiftRightLogic, OP_PSHRD, SIMD_EMIT_SHIFT },
	{ SN_Shuffle, OP_PSHUFLED, SIMD_EMIT_SHUFFLE },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_UnpackHigh, OP_UNPACK_HIGHD, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWD, SIMD_EMIT_BINARY },
	{ SN_op_Addition, OP_PADDD, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_EMIT_CAST },
	{ SN_op_LeftShift, OP_PSHLD, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULD, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_op_RightShift, OP_PSARD, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBD, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector8us_intrinsics[] = {
	{ SN_AddWithSaturation, OP_PADDW_SAT_UN, SIMD_EMIT_BINARY },
	{ SN_Average, OP_PAVGW_UN, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQW, SIMD_EMIT_BINARY },
	{ SN_ExtractByteMask, 0, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXW_UN, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_Min, OP_PMINW_UN, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_MultiplyStoreHigh, OP_PMULW_HIGH_UN, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_NTA },
	{ SN_ShiftRightArithmetic, OP_PSARW, SIMD_EMIT_SHIFT },
	{ SN_ShuffleHigh, OP_PSHUFLEW_HIGH, SIMD_EMIT_SHUFFLE },
	{ SN_ShuffleLow, OP_PSHUFLEW_LOW, SIMD_EMIT_SHUFFLE },
	{ SN_SignedPackWithSignedSaturation, OP_PACKW, SIMD_EMIT_BINARY },
	{ SN_SignedPackWithUnsignedSaturation, OP_PACKW_UN, SIMD_EMIT_BINARY },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_SubWithSaturation, OP_PSUBW_SAT_UN, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHW, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWW, SIMD_EMIT_BINARY },
	{ SN_op_Addition, OP_PADDW, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_EMIT_CAST },
	{ SN_op_LeftShift, OP_PSHLW, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULW, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSHRW, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBW, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector8s_intrinsics[] = {
	{ SN_AddWithSaturation, OP_PADDW_SAT, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQW, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTW, SIMD_EMIT_BINARY },
	{ SN_ExtractByteMask, 0, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXW, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMINW, SIMD_EMIT_BINARY },
	{ SN_MultiplyStoreHigh, OP_PMULW_HIGH, SIMD_EMIT_BINARY },
	{ SN_PackWithSignedSaturation, OP_PACKW, SIMD_EMIT_BINARY },
	{ SN_PackWithUnsignedSaturation, OP_PACKW_UN, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_NTA },
	{ SN_ShiftRightLogic, OP_PSHRW, SIMD_EMIT_SHIFT },
	{ SN_ShuffleHigh, OP_PSHUFLEW_HIGH, SIMD_EMIT_SHUFFLE },
	{ SN_ShuffleLow, OP_PSHUFLEW_LOW, SIMD_EMIT_SHUFFLE },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_SubWithSaturation, OP_PSUBW_SAT_UN, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHW, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWW, SIMD_EMIT_BINARY },
	{ SN_op_Addition, OP_PADDW, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_EMIT_CAST },
	{ SN_op_LeftShift, OP_PSHLW, SIMD_EMIT_SHIFT },
	{ SN_op_Multiply, OP_PMULW, SIMD_EMIT_BINARY },
	{ SN_op_RightShift, OP_PSARW, SIMD_EMIT_SHIFT },
	{ SN_op_Subtraction, OP_PSUBW, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector16b_intrinsics[] = {
	{ SN_AddWithSaturation, OP_PADDB_SAT_UN, SIMD_EMIT_BINARY },
	{ SN_Average, OP_PAVGB_UN, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQB, SIMD_EMIT_BINARY },
	{ SN_ExtractByteMask, 0, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXB_UN, SIMD_EMIT_BINARY },
	{ SN_Min, OP_PMINB_UN, SIMD_EMIT_BINARY },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_NTA },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_SubWithSaturation, OP_PSUBB_SAT_UN, SIMD_EMIT_BINARY },
	{ SN_SumOfAbsoluteDifferences, OP_PSUM_ABS_DIFF, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHB, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWB, SIMD_EMIT_BINARY },
	{ SN_op_Addition, OP_PADDB, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_EMIT_CAST },
	{ SN_op_Subtraction, OP_PSUBB, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector16sb_intrinsics[] = {
	{ SN_AddWithSaturation, OP_PADDB_SAT, SIMD_EMIT_BINARY },
	{ SN_CompareEqual, OP_PCMPEQB, SIMD_EMIT_BINARY },
	{ SN_CompareGreaterThan, OP_PCMPGTB, SIMD_EMIT_BINARY },
	{ SN_ExtractByteMask, 0, SIMD_EMIT_EXTRACT_MASK },
	{ SN_LoadAligned, 0, SIMD_EMIT_LOAD_ALIGNED },
	{ SN_Max, OP_PMAXB, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_Min, OP_PMINB, SIMD_EMIT_BINARY, SIMD_VERSION_SSE41 },
	{ SN_PrefetchTemporalAllCacheLevels, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_0 },
	{ SN_PrefetchTemporal1stLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_1 },
	{ SN_PrefetchTemporal2ndLevelCache, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_2 },
	{ SN_PrefetchNonTemporal, 0, SIMD_EMIT_PREFETCH, SIMD_VERSION_SSE1, SIMD_PREFETCH_MODE_NTA },
	{ SN_StoreAligned, OP_STOREX_ALIGNED_MEMBASE_REG, SIMD_EMIT_STORE },
	{ SN_SubWithSaturation, OP_PSUBB_SAT, SIMD_EMIT_BINARY },
	{ SN_UnpackHigh, OP_UNPACK_HIGHB, SIMD_EMIT_BINARY },
	{ SN_UnpackLow, OP_UNPACK_LOWB, SIMD_EMIT_BINARY },
	{ SN_op_Addition, OP_PADDB, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseAnd, OP_PAND, SIMD_EMIT_BINARY },
	{ SN_op_BitwiseOr, OP_POR, SIMD_EMIT_BINARY },
	{ SN_op_ExclusiveOr, OP_PXOR, SIMD_EMIT_BINARY },
	{ SN_op_Explicit, 0, SIMD_EMIT_CAST },
	{ SN_op_Subtraction, OP_PSUBB, SIMD_EMIT_BINARY },
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
		if (apply_vreg_first_block_interference (cfg, ins, ins->sreg1, max_vreg, vreg_flags))
			continue;
		if (apply_vreg_first_block_interference (cfg, ins, ins->sreg2, max_vreg, vreg_flags))
			continue;
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
			
			if (ins->opcode == OP_LDADDR && apply_vreg_following_block_interference (cfg, ins, ((MonoInst*)ins->inst_p0)->dreg, bb, max_vreg, vreg_flags, target_bb))
				continue;
			if (apply_vreg_following_block_interference (cfg, ins, ins->dreg, bb, max_vreg, vreg_flags, target_bb))
				continue;
			if (apply_vreg_following_block_interference (cfg, ins, ins->sreg1, bb, max_vreg, vreg_flags, target_bb))
				continue;
			if (apply_vreg_following_block_interference (cfg, ins, ins->sreg2, bb, max_vreg, vreg_flags, target_bb))
				continue;
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
			/*We can, pretty much kill it.*/
			if (ins->dreg == var->dreg) {
				break;
			} else if (ins->sreg1 == var->dreg || ins->sreg2 == var->dreg) {
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
load_simd_vreg (MonoCompile *cfg, MonoMethod *cmethod, MonoInst *src)
{
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

static MonoInst*
simd_intrinsic_emit_binary (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst* ins;
	int left_vreg, right_vreg;

	left_vreg = get_simd_vreg (cfg, cmethod, args [0]);
	right_vreg = get_simd_vreg (cfg, cmethod, args [1]);
	

	MONO_INST_NEW (cfg, ins, intrinsic->opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = left_vreg;
	ins->sreg2 = right_vreg;
	ins->type = STACK_VTYPE;
	ins->klass = cmethod->klass;
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

static MonoInst*
simd_intrinsic_emit_getter (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *tmp, *ins;
	int vreg;
	
	vreg = load_simd_vreg (cfg, cmethod, args [0]);

	if (intrinsic->opcode) {
		MONO_INST_NEW (cfg, ins, OP_SHUFLEPS);
		ins->klass = cmethod->klass;
		ins->sreg1 = vreg;
		ins->inst_c0 = intrinsic->opcode;
		ins->type = STACK_VTYPE;
		ins->dreg = vreg = alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, ins);
	}

	MONO_INST_NEW (cfg, tmp, OP_EXTRACT_I4);
	tmp->klass = cmethod->klass;
	tmp->sreg1 = vreg;
	tmp->type = STACK_I4;
	tmp->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, tmp);

	MONO_INST_NEW (cfg, ins, OP_ICONV_TO_R8_RAW);
	ins->klass = mono_defaults.single_class;
	ins->sreg1 = tmp->dreg;
	ins->type = STACK_R8;
	ins->dreg = alloc_freg (cfg);
	ins->backend.spill_var = get_int_to_float_spill_area (cfg);
	MONO_ADD_INS (cfg->cbb, ins);	
	return ins;
}

static MonoInst*
simd_intrinsic_emit_ctor (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int i;

	for (i = 1; i < 5; ++i) {
		MONO_INST_NEW (cfg, ins, OP_PUSH_R4);
		ins->sreg1 = args [5 - i]->dreg;
		ins->klass = args [5 - i]->klass;
		MONO_ADD_INS (cfg->cbb, ins);
	}

	if (args [0]->opcode == OP_LDADDR) { /*Eliminate LDADDR if it's initing a local var*/
		int vreg = ((MonoInst*)args [0]->inst_p0)->dreg;
		NULLIFY_INS (args [0]);
		
		MONO_INST_NEW (cfg, ins, OP_LOADX_STACK);
		ins->klass = cmethod->klass;
		ins->type = STACK_VTYPE;
		ins->dreg = vreg;
		MONO_ADD_INS (cfg->cbb, ins);
	} else {
		int vreg = alloc_ireg (cfg);

		MONO_INST_NEW (cfg, ins, OP_LOADX_STACK);
		ins->klass = cmethod->klass;
		ins->type = STACK_VTYPE;
		ins->dreg = vreg;
		MONO_ADD_INS (cfg->cbb, ins);
		
		MONO_INST_NEW (cfg, ins, OP_STOREX_MEMBASE_REG);
		ins->klass = cmethod->klass;
		ins->dreg = args [0]->dreg;
		ins->sreg1 = vreg;
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


static MonoInst*
simd_intrinsic_emit_shuffle (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg;

	/*TODO Exposing shuffle is not a good thing as it's non obvious. We should come up with better abstractions*/

	if (args [1]->opcode != OP_ICONST) {
		g_warning ("Shuffle with non literals is not yet supported");
		g_assert_not_reached ();
	}
	vreg = get_simd_vreg (cfg, cmethod, args [0]);
	NULLIFY_INS (args [1]);

	MONO_INST_NEW (cfg, ins, intrinsic->opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = vreg;
	ins->inst_c0 = args [1]->inst_c0;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
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
	if (result->simd_version && !((1 << result->simd_version) & simd_supported_versions)) {
		if (IS_DEBUG_ON (cfg))
			printf ("function %s::%s/%d requires unsuported SIMD instruction set %s \n", cmethod->klass->name, cmethod->name, fsig->param_count, simd_version_name (result->simd_version));
		return NULL;
	}

	switch (result->simd_emit_mode) {
	case SIMD_EMIT_BINARY:
		return simd_intrinsic_emit_binary (result, cfg, cmethod, args);
	case SIMD_EMIT_UNARY:
		return simd_intrinsic_emit_unary (result, cfg, cmethod, args);
	case SIMD_EMIT_GETTER:
		return simd_intrinsic_emit_getter (result, cfg, cmethod, args);
	case SIMD_EMIT_CTOR:
		return simd_intrinsic_emit_ctor (result, cfg, cmethod, args);
	case SIMD_EMIT_CAST:
		return simd_intrinsic_emit_cast (result, cfg, cmethod, args);
	case SIMD_EMIT_SHUFFLE:
		return simd_intrinsic_emit_shuffle (result, cfg, cmethod, args); 
	case SIMD_EMIT_SHIFT:
		return simd_intrinsic_emit_shift (result, cfg, cmethod, args);
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
	if (!strcmp ("Mono.Simd", cmethod->klass->name_space) && !strcmp ("SimdRuntime", cmethod->klass->name))
		return emit_simd_runtime_intrinsics (cfg, cmethod, fsig, args);
	if (!cmethod->klass->simd_type)
		return NULL;
	cfg->uses_simd_intrinsics = 1;
	if (!strcmp ("Vector2d", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector2d_intrinsics, sizeof (vector2d_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector4f", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector4f_intrinsics, sizeof (vector4f_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector2ul", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector2ul_intrinsics, sizeof (vector2ul_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector2l", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector2l_intrinsics, sizeof (vector2l_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector4ui", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector4ui_intrinsics, sizeof (vector4ui_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector4i", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector4i_intrinsics, sizeof (vector4i_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector8us", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector8us_intrinsics, sizeof (vector8us_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector8s", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector8s_intrinsics, sizeof (vector8s_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector16b", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector16b_intrinsics, sizeof (vector16b_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector16sb", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector16sb_intrinsics, sizeof (vector16sb_intrinsics) / sizeof (SimdIntrinsc));

	return NULL;
}

#endif
