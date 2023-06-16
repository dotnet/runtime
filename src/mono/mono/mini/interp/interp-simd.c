
#include "interp-internals.h"
#include "interp-simd.h"

#if HOST_BROWSER
#include <wasm_simd128.h>
#endif

#ifdef INTERP_ENABLE_SIMD

typedef gint64 v128_i8 __attribute__ ((vector_size (SIZEOF_V128)));
typedef guint64 v128_u8 __attribute__ ((vector_size (SIZEOF_V128)));
typedef gint32 v128_i4 __attribute__ ((vector_size (SIZEOF_V128)));
typedef guint32 v128_u4 __attribute__ ((vector_size (SIZEOF_V128)));
typedef gint16 v128_i2 __attribute__ ((vector_size (SIZEOF_V128)));
typedef guint16 v128_u2 __attribute__ ((vector_size (SIZEOF_V128)));
typedef gint8 v128_i1 __attribute__ ((vector_size (SIZEOF_V128)));
typedef guint8 v128_u1 __attribute__ ((vector_size (SIZEOF_V128)));
typedef float v128_r4 __attribute__ ((vector_size (SIZEOF_V128)));

// get_AllBitsSet
static void
interp_v128_i4_all_bits_set (gpointer res)
{
	memset (res, 0xff, SIZEOF_V128);
}

// op_Addition
static void
interp_v128_i1_op_addition (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i1*)res = *(v128_i1*)v1 + *(v128_i1*)v2;
}

static void
interp_v128_i2_op_addition (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i2*)res = *(v128_i2*)v1 + *(v128_i2*)v2;
}

static void
interp_v128_i4_op_addition (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i4*)res = *(v128_i4*)v1 + *(v128_i4*)v2;
}

static void
interp_v128_r4_op_addition (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_r4*)res = *(v128_r4*)v1 + *(v128_r4*)v2;
}

// op_Subtraction
static void
interp_v128_i1_op_subtraction (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i1*)res = *(v128_i1*)v1 - *(v128_i1*)v2;
}

static void
interp_v128_i2_op_subtraction (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i2*)res = *(v128_i2*)v1 - *(v128_i2*)v2;
}

static void
interp_v128_i4_op_subtraction (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i4*)res = *(v128_i4*)v1 - *(v128_i4*)v2;
}

static void
interp_v128_r4_op_subtraction (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_r4*)res = *(v128_r4*)v1 - *(v128_r4*)v2;
}

// op_BitwiseAnd
static void
interp_v128_op_bitwise_and (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i4*)res = *(v128_i4*)v1 & *(v128_i4*)v2;
}

// op_BitwiseOr
static void
interp_v128_op_bitwise_or (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i4*)res = *(v128_i4*)v1 | *(v128_i4*)v2;
}

// op_Equality
static void
interp_v128_op_bitwise_equality (gpointer res, gpointer v1, gpointer v2)
{
	gint64 *v1_cast = (gint64*)v1;
	gint64 *v2_cast = (gint64*)v2;

	if (*v1_cast == *v2_cast && *(v1_cast + 1) == *(v2_cast + 1))
		*(gint32*)res = 1;
	else
		*(gint32*)res = 0;
}

// op_ExclusiveOr
static void
interp_v128_op_exclusive_or (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i4*)res = *(v128_i4*)v1 ^ *(v128_i4*)v2;
}

// op_Inequality
static void
interp_v128_op_bitwise_inequality (gpointer res, gpointer v1, gpointer v2)
{
	gint64 *v1_cast = (gint64*)v1;
	gint64 *v2_cast = (gint64*)v2;

	if (*v1_cast == *v2_cast && *(v1_cast + 1) == *(v2_cast + 1))
		*(gint32*)res = 0;
	else
		*(gint32*)res = 1;
}

// op_Addition
static void
interp_v128_i1_op_multiply (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i1*)res = *(v128_i1*)v1 * *(v128_i1*)v2;
}

static void
interp_v128_i2_op_multiply (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i2*)res = *(v128_i2*)v1 * *(v128_i2*)v2;
}

static void
interp_v128_i4_op_multiply (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i4*)res = *(v128_i4*)v1 * *(v128_i4*)v2;
}

static void
interp_v128_r4_op_multiply (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_r4*)res = *(v128_r4*)v1 * *(v128_r4*)v2;
}

static void
interp_v128_r4_op_division (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_r4*)res = *(v128_r4*)v1 / *(v128_r4*)v2;
}

// op_UnaryNegation
static void
interp_v128_i1_op_negation (gpointer res, gpointer v1)
{
	*(v128_i1*)res = - (*(v128_i1*)v1);
}

static void
interp_v128_i2_op_negation (gpointer res, gpointer v1)
{
	*(v128_i2*)res = - (*(v128_i2*)v1);
}

static void
interp_v128_i4_op_negation (gpointer res, gpointer v1)
{
	*(v128_i4*)res = - (*(v128_i4*)v1);
}

// op_LeftShift
static void
interp_v128_i1_op_left_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i1*)res = *(v128_i1*)v1 << (*(gint32*)s1 & 7);
}

static void
interp_v128_i2_op_left_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i2*)res = *(v128_i2*)v1 << (*(gint32*)s1 & 15);
}

static void
interp_v128_i4_op_left_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i4*)res = *(v128_i4*)v1 << *(gint32*)s1;
}

static void
interp_v128_i8_op_left_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i8*)res = *(v128_i8*)v1 << *(gint32*)s1;
}

// op_RightShift
static void
interp_v128_i1_op_right_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i1*)res = *(v128_i1*)v1 >> *(gint32*)s1;
}

static void
interp_v128_i2_op_right_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i2*)res = *(v128_i2*)v1 >> *(gint32*)s1;
}

static void
interp_v128_i4_op_right_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i4*)res = *(v128_i4*)v1 >> *(gint32*)s1;
}

// op_UnsignedRightShift
static void
interp_v128_i1_op_uright_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_u1*)res = *(v128_u1*)v1 >> *(gint32*)s1;
}

static void
interp_v128_i2_op_uright_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_u2*)res = *(v128_u2*)v1 >> *(gint32*)s1;
}

static void
interp_v128_i4_op_uright_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_u4*)res = *(v128_u4*)v1 >> *(gint32*)s1;
}

static void
interp_v128_i8_op_uright_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_u8*)res = *(v128_u8*)v1 >> *(gint32*)s1;
}

// op_OnesComplement
static void
interp_v128_op_ones_complement (gpointer res, gpointer v1)
{
	*(v128_i4*)res = ~(*(v128_i4*)v1);
}

// WidenLower
static void
interp_v128_u2_widen_lower (gpointer res, gpointer v1)
{
	guint16 *res_typed = (guint16*)res;
	guint64 lower_copy = *(guint64*)v1;
	guint8 *v1_typed = (guint8*)&lower_copy;

        res_typed [0] = v1_typed [0];
        res_typed [1] = v1_typed [1];
        res_typed [2] = v1_typed [2];
        res_typed [3] = v1_typed [3];
        res_typed [4] = v1_typed [4];
        res_typed [5] = v1_typed [5];
        res_typed [6] = v1_typed [6];
        res_typed [7] = v1_typed [7];
}

// WidenUpper
static void
interp_v128_u2_widen_upper (gpointer res, gpointer v1)
{
	guint16 *res_typed = (guint16*)res;
	guint64 upper_copy = *((guint64*)v1 + 1);
	guint8 *v1_typed = (guint8*)&upper_copy;

        res_typed [0] = v1_typed [0];
        res_typed [1] = v1_typed [1];
        res_typed [2] = v1_typed [2];
        res_typed [3] = v1_typed [3];
        res_typed [4] = v1_typed [4];
        res_typed [5] = v1_typed [5];
        res_typed [6] = v1_typed [6];
        res_typed [7] = v1_typed [7];
}

// Narrow
static void
interp_v128_u1_narrow (gpointer res, gpointer v1, gpointer v2)
{
	guint8 *res_typed = (guint8*)res;
	guint16 *v1_typed = (guint16*)v1;
	guint16 *v2_typed = (guint16*)v2;

	if (res != v2) {
		res_typed [0] = v1_typed [0];
		res_typed [1] = v1_typed [1];
		res_typed [2] = v1_typed [2];
		res_typed [3] = v1_typed [3];
		res_typed [4] = v1_typed [4];
		res_typed [5] = v1_typed [5];
		res_typed [6] = v1_typed [6];
		res_typed [7] = v1_typed [7];

		res_typed [8] = v2_typed [0];
		res_typed [9] = v2_typed [1];
		res_typed [10] = v2_typed [2];
		res_typed [11] = v2_typed [3];
		res_typed [12] = v2_typed [4];
		res_typed [13] = v2_typed [5];
		res_typed [14] = v2_typed [6];
		res_typed [15] = v2_typed [7];
	} else {
		res_typed [15] = v2_typed [7];
		res_typed [14] = v2_typed [6];
		res_typed [13] = v2_typed [5];
		res_typed [12] = v2_typed [4];
		res_typed [11] = v2_typed [3];
		res_typed [10] = v2_typed [2];
		res_typed [9] = v2_typed [1];
		res_typed [8] = v2_typed [0];

		res_typed [0] = v1_typed [0];
		res_typed [1] = v1_typed [1];
		res_typed [2] = v1_typed [2];
		res_typed [3] = v1_typed [3];
		res_typed [4] = v1_typed [4];
		res_typed [5] = v1_typed [5];
		res_typed [6] = v1_typed [6];
		res_typed [7] = v1_typed [7];
	}
}

// GreaterThan
static void
interp_v128_u1_greater_than (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_u1*)res = *(v128_u1*)v1 > *(v128_u1*)v2;
}

// LessThan
static void
interp_v128_i1_less_than (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i1*)res = *(v128_i1*)v1 < *(v128_i1*)v2;
}

static void
interp_v128_u1_less_than (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_u1*)res = *(v128_u1*)v1 < *(v128_u1*)v2;
}

static void
interp_v128_i2_less_than (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i2*)res = *(v128_i2*)v1 < *(v128_i2*)v2;
}

// Equals
static void
interp_v128_i1_equals (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i1*)res = *(v128_i1*)v1 == *(v128_i1*)v2;
}

static void
interp_v128_i2_equals (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i2*)res = *(v128_i2*)v1 == *(v128_i2*)v2;
}

static void
interp_v128_i4_equals (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i4*)res = *(v128_i4*)v1 == *(v128_i4*)v2;
}

static void
interp_v128_r4_equals (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_r4*)res = *(v128_r4*)v1 == *(v128_r4*)v2;
}

static void
interp_v128_i8_equals (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i8*)res = *(v128_i8*)v1 == *(v128_i8*)v2;
}

// CreateScalar
static void
interp_v128_i1_create_scalar (gpointer res, gpointer v1)
{
	gint8 val = *(gint8*)v1;
	memset (res, 0, SIZEOF_V128);
	*(gint8*)res = val;
}

static void
interp_v128_i2_create_scalar (gpointer res, gpointer v1)
{
	gint16 val = *(gint16*)v1;
	memset (res, 0, SIZEOF_V128);
	*(gint16*)res = val;
}
static void
interp_v128_i4_create_scalar (gpointer res, gpointer v1)
{
	gint32 val = *(gint32*)v1;
	memset (res, 0, SIZEOF_V128);
	*(gint32*)res = val;
}
static void
interp_v128_i8_create_scalar (gpointer res, gpointer v1)
{
	gint64 val = *(gint64*)v1;
	memset (res, 0, SIZEOF_V128);
	*(gint64*)res = val;
}

// ExtractMostSignificantBits
static void
interp_v128_i1_extract_msb (gpointer res, gpointer v1)
{
	guint32 val = 0;
	gint8 *v1_typed = (gint8*)v1;
	for (int i = 0; i < SIZEOF_V128 / sizeof (gint8); i++) {
		if (*v1_typed & (1 << 7))
			val |= 1 << i;
		v1_typed++;
	}
	*(guint32*)res = val;
}

static void
interp_v128_i2_extract_msb (gpointer res, gpointer v1)
{
	guint32 val = 0;
	gint16 *v1_typed = (gint16*)v1;
	for (int i = 0; i < SIZEOF_V128 / sizeof (gint16); i++) {
		if (*v1_typed & (1 << 15))
			val |= 1 << i;
		v1_typed++;
	}
	*(guint32*)res = val;
}

static void
interp_v128_i4_extract_msb (gpointer res, gpointer v1)
{
	guint32 val = 0;
	gint32 *v1_typed = (gint32*)v1;
	for (int i = 0; i < SIZEOF_V128 / sizeof (gint32); i++) {
		if (*v1_typed & (1 << 31))
			val |= 1 << i;
		v1_typed++;
	}
	*(guint32*)res = val;
}

static void
interp_v128_i8_extract_msb (gpointer res, gpointer v1)
{
	guint32 val = 0;
	gint64 *v1_typed = (gint64*)v1;
	for (int i = 0; i < SIZEOF_V128 / sizeof (gint64); i++) {
		if (*v1_typed & ((gint64)1 << 63))
			val |= 1 << i;
		v1_typed++;
	}
	*(guint32*)res = val;
}

// ConditionalSelect
static void
interp_v128_conditional_select (gpointer res, gpointer v1, gpointer v2, gpointer v3)
{
	v128_i8 cond = *(v128_i8*)v1;
	*(v128_i8*)res = (*(v128_i8*)v2 & cond) | (*(v128_i8*)v3 & ~cond);
}

// Create
static void
interp_v128_i1_create (gpointer res, gpointer v1)
{
	gint8 val = *(gint8*)v1;
	v128_i1 v = { val, val, val, val,
			val, val, val, val,
			val, val, val, val,
			val, val, val, val };
	*(v128_i1*)res = v;
}

static void
interp_v128_i2_create (gpointer res, gpointer v1)
{
	gint16 val = *(gint16*)v1;
	v128_i2 v = { val, val, val, val,
			val, val, val, val };
	*(v128_i2*)res = v;
}

static void
interp_v128_i4_create (gpointer res, gpointer v1)
{
	gint32 val = *(gint32*)v1;
	v128_i4 v = { val, val, val, val };
	*(v128_i4*)res = v;
}

static void
interp_v128_i8_create (gpointer res, gpointer v1)
{
	gint64 val = *(gint64*)v1;
	v128_i8 v = { val, val };
	*(v128_i8*)res = v;
}

// AndNot
static void
interp_v128_and_not (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_i4*)res = *(v128_i4*)v1 & ~(*(v128_i4*)v2);
}

// LessThanOrEqual
static void
interp_v128_u2_less_than_equal (gpointer res, gpointer v1, gpointer v2)
{
	*(v128_u2*)res = *(v128_u2*)v1 <= *(v128_u2*)v2;
}

// Shuffle

#define V128_SHUFFLE(eltype, itype) do {					\
		eltype result[16];						\
		eltype *v1_typed = (eltype*)v1;					\
		itype *v2_typed = (itype*)v2;					\
		for (int i = 0; i < SIZEOF_V128 / sizeof (eltype); i++) {	\
			itype index = v2_typed [i];				\
			if (index < (SIZEOF_V128 / sizeof (eltype)))		\
				result [i] = v1_typed [index];			\
			else							\
				result [i] = 0;					\
		}								\
		memcpy (res, result, SIZEOF_V128);				\
	} while (0)
static void
interp_v128_i1_shuffle (gpointer res, gpointer v1, gpointer v2)
{
	V128_SHUFFLE (gint8, guint8);
}

static void
interp_v128_i2_shuffle (gpointer res, gpointer v1, gpointer v2)
{
	V128_SHUFFLE (gint16, guint16);
}

static void
interp_v128_i4_shuffle (gpointer res, gpointer v1, gpointer v2)
{
	V128_SHUFFLE (gint32, guint32);
}

static void
interp_v128_i8_shuffle (gpointer res, gpointer v1, gpointer v2)
{
	V128_SHUFFLE (gint64, guint64);
}

#define INTERP_SIMD_INTRINSIC_P_P(a,b,c)
#define INTERP_SIMD_INTRINSIC_P_PP(a,b,c)
#define INTERP_SIMD_INTRINSIC_P_PPP(a,b,c)

// For the wasm packed simd intrinsics we want to automatically generate the C implementations from
//  their corresponding clang intrinsics. See also:
// https://github.com/llvm/llvm-project/blob/main/clang/lib/Headers/wasm_simd128.h
// In this context V means Vector128 and P means void* pointer.
#ifdef HOST_BROWSER

static v128_t
_interp_wasm_simd_assert_not_reached (v128_t lhs, v128_t rhs) {
	g_assert_not_reached ();
}

#define INTERP_WASM_SIMD_INTRINSIC_V_P(id, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## id (gpointer res, gpointer v1) { \
	*((v128_t *)res) = c_intrinsic (v1); \
}

#define INTERP_WASM_SIMD_INTRINSIC_V_V(id, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## id (gpointer res, gpointer v1) { \
	*((v128_t *)res) = c_intrinsic (*((v128_t *)v1)); \
}

#define INTERP_WASM_SIMD_INTRINSIC_I_V(id, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## id (gpointer res, gpointer v1) { \
	*((int32_t *)res) = c_intrinsic (*((v128_t *)v1)); \
}

#define INTERP_WASM_SIMD_INTRINSIC_V_VV(id, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## id (gpointer res, gpointer v1, gpointer v2) { \
	*((v128_t *)res) = c_intrinsic (*((v128_t *)v1), *((v128_t *)v2)); \
}

#define INTERP_WASM_SIMD_INTRINSIC_V_VI(id, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## id (gpointer res, gpointer v1, gpointer v2) { \
	*((v128_t *)res) = c_intrinsic (*((v128_t *)v1), *((int *)v2)); \
}

#define INTERP_WASM_SIMD_INTRINSIC_V_VVV(id, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## id (gpointer res, gpointer v1, gpointer v2, gpointer v3) { \
	*((v128_t *)res) = c_intrinsic (*((v128_t *)v1), *((v128_t *)v2), *((v128_t *)v3)); \
}

#include "interp-simd-intrins.def"

#undef INTERP_WASM_SIMD_INTRINSIC_V_P
#undef INTERP_WASM_SIMD_INTRINSIC_V_V
#undef INTERP_WASM_SIMD_INTRINSIC_I_V
#undef INTERP_WASM_SIMD_INTRINSIC_V_VV
#undef INTERP_WASM_SIMD_INTRINSIC_V_VI
#undef INTERP_WASM_SIMD_INTRINSIC_V_VVV

// Now generate the wasm opcode tables for the intrinsics

#undef INTERP_SIMD_INTRINSIC_P_P
#define INTERP_SIMD_INTRINSIC_P_P(a,b,c) c,

int interp_simd_p_p_wasm_opcode_table [] = {
#include "interp-simd-intrins.def"
};

#undef INTERP_SIMD_INTRINSIC_P_P
#define INTERP_SIMD_INTRINSIC_P_P(a,b,c)

#undef INTERP_SIMD_INTRINSIC_P_PP
#define INTERP_SIMD_INTRINSIC_P_PP(a,b,c) c,

int interp_simd_p_pp_wasm_opcode_table [] = {
#include "interp-simd-intrins.def"
};

#undef INTERP_SIMD_INTRINSIC_P_PP
#define INTERP_SIMD_INTRINSIC_P_PP(a,b,c)

#undef INTERP_SIMD_INTRINSIC_P_PPP
#define INTERP_SIMD_INTRINSIC_P_PPP(a,b,c) c,

int interp_simd_p_ppp_wasm_opcode_table [] = {
#include "interp-simd-intrins.def"
};

#undef INTERP_SIMD_INTRINSIC_P_PPP
#define INTERP_SIMD_INTRINSIC_P_PPP(a,b,c)

#endif // HOST_BROWSER

#undef INTERP_SIMD_INTRINSIC_P_P
#define INTERP_SIMD_INTRINSIC_P_P(a,b,c) b,
PP_SIMD_Method interp_simd_p_p_table [] = {
#include "interp-simd-intrins.def"
};
#undef INTERP_SIMD_INTRINSIC_P_P
#define INTERP_SIMD_INTRINSIC_P_P(a,b,c)

#undef INTERP_SIMD_INTRINSIC_P_PP
#define INTERP_SIMD_INTRINSIC_P_PP(a,b,c) b,
PPP_SIMD_Method interp_simd_p_pp_table [] = {
#include "interp-simd-intrins.def"
};
#undef INTERP_SIMD_INTRINSIC_P_PP
#define INTERP_SIMD_INTRINSIC_P_PP(a,b,c)

#undef INTERP_SIMD_INTRINSIC_P_PPP
#define INTERP_SIMD_INTRINSIC_P_PPP(a,b,c) b,
PPPP_SIMD_Method interp_simd_p_ppp_table [] = {
#include "interp-simd-intrins.def"
};
#undef INTERP_SIMD_INTRINSIC_P_PPP
#define INTERP_SIMD_INTRINSIC_P_PPP(a,b,c)

#endif // INTERP_ENABLE_SIMD
