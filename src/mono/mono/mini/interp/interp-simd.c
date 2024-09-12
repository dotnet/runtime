
#include "interp-internals.h"
#include "interp-simd.h"

#if HOST_BROWSER
#include <wasm_simd128.h>
#endif

#ifdef INTERP_ENABLE_SIMD

gboolean interp_simd_enabled = TRUE;

typedef gint64 v128_i8 __attribute__ ((vector_size (SIZEOF_V128)));
typedef guint64 v128_u8 __attribute__ ((vector_size (SIZEOF_V128)));
typedef gint32 v128_i4 __attribute__ ((vector_size (SIZEOF_V128)));
typedef guint32 v128_u4 __attribute__ ((vector_size (SIZEOF_V128)));
typedef gint16 v128_i2 __attribute__ ((vector_size (SIZEOF_V128)));
typedef guint16 v128_u2 __attribute__ ((vector_size (SIZEOF_V128)));
typedef gint8 v128_i1 __attribute__ ((vector_size (SIZEOF_V128)));
typedef guint8 v128_u1 __attribute__ ((vector_size (SIZEOF_V128)));
typedef float v128_r4 __attribute__ ((vector_size (SIZEOF_V128)));
typedef double v128_r8 __attribute__ ((vector_size (SIZEOF_V128)));

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

// Vector128<float>EqualsFloatingPoint
static void
interp_v128_r4_float_equality (gpointer res, gpointer v1, gpointer v2)
{
	v128_r4 v1_cast = *(v128_r4*)v1;
	v128_r4 v2_cast = *(v128_r4*)v2;
	v128_r4 result = (v1_cast == v2_cast) | ~((v1_cast == v1_cast) | (v2_cast == v2_cast));
	memset (&v1_cast, 0xff, SIZEOF_V128);

	*(gint32*)res = memcmp (&v1_cast, &result, SIZEOF_V128) == 0;
}

static void
interp_v128_r8_float_equality (gpointer res, gpointer v1, gpointer v2)
{
	v128_r8 v1_cast = *(v128_r8*)v1;
	v128_r8 v2_cast = *(v128_r8*)v2;
	v128_r8 result = (v1_cast == v2_cast) | ~((v1_cast == v1_cast) | (v2_cast == v2_cast));
	memset (&v1_cast, 0xff, SIZEOF_V128);

	*(gint32*)res = memcmp (&v1_cast, &result, SIZEOF_V128) == 0;
}

// op_Multiply
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

// op_Division
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
	*(v128_i4*)res = *(v128_i4*)v1 << (*(gint32*)s1 & 31);
}

static void
interp_v128_i8_op_left_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i8*)res = *(v128_i8*)v1 << (*(gint32*)s1 & 63);
}

// op_RightShift
static void
interp_v128_i1_op_right_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i1*)res = *(v128_i1*)v1 >> (*(gint32*)s1 & 7);
}

static void
interp_v128_i2_op_right_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i2*)res = *(v128_i2*)v1 >> (*(gint32*)s1 & 15);
}

static void
interp_v128_i4_op_right_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_i4*)res = *(v128_i4*)v1 >> (*(gint32*)s1 & 31);
}

// op_UnsignedRightShift
static void
interp_v128_i1_op_uright_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_u1*)res = *(v128_u1*)v1 >> (*(gint32*)s1 & 7);
}

static void
interp_v128_i2_op_uright_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_u2*)res = *(v128_u2*)v1 >> (*(gint32*)s1 & 15);
}

static void
interp_v128_i4_op_uright_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_u4*)res = *(v128_u4*)v1 >> (*(gint32*)s1 & 31);
}

static void
interp_v128_i8_op_uright_shift (gpointer res, gpointer v1, gpointer s1)
{
	*(v128_u8*)res = *(v128_u8*)v1 >> (*(gint32*)s1 & 63);
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
	guint8 res_typed [SIZEOF_V128];
	guint16 *v1_typed = (guint16*)v1;
	guint16 *v2_typed = (guint16*)v2;

	for (int i = 0; i < 8; i++)
		res_typed [i] = v1_typed [i];
	for (int i = 0; i < 8; i++)
		res_typed [i + 8] = v2_typed [i];
	memcpy (res, res_typed, SIZEOF_V128);
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

// EqualsAny
static void
interp_v128_i1_equals_any (gpointer res, gpointer v1, gpointer v2)
{
	v128_i1 resv = *(v128_i1*)v1 == *(v128_i1*)v2;

	gint64 *resv_cast = (gint64*)&resv;
	*(gint32*)res = *resv_cast || *(resv_cast + 1);
}

static void
interp_v128_i2_equals_any (gpointer res, gpointer v1, gpointer v2)
{
	v128_i2 resv = *(v128_i2*)v1 == *(v128_i2*)v2;

	gint64 *resv_cast = (gint64*)&resv;
	*(gint32*)res = *resv_cast || *(resv_cast + 1);
}

static void
interp_v128_i4_equals_any (gpointer res, gpointer v1, gpointer v2)
{
	v128_i4 resv = *(v128_i4*)v1 == *(v128_i4*)v2;

	gint64 *resv_cast = (gint64*)&resv;
	*(gint32*)res = *resv_cast || *(resv_cast + 1);
}

static void
interp_v128_i8_equals_any (gpointer res, gpointer v1, gpointer v2)
{
	v128_i8 resv = *(v128_i8*)v1 == *(v128_i8*)v2;

	gint64 *resv_cast = (gint64*)&resv;
	*(gint32*)res = *resv_cast || *(resv_cast + 1);
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

#define LANE_COUNT(lane_type) (sizeof(v128_t) / sizeof(lane_type))

// ensure the lane is valid by wrapping it (in AOT it would fail to compile)
#define WRAP_LANE(lane_type, lane_ptr)  \
	*((unsigned char *)lane_ptr) & (LANE_COUNT(lane_type) - 1)

#define EXTRACT_LANE(result_type, lane_type) \
	int _lane = WRAP_LANE(lane_type, lane); \
	*((result_type *)res) = ((lane_type *)vec)[_lane];

#define REPLACE_LANE(lane_type) \
	int _lane = WRAP_LANE(lane_type, lane); \
	v128_t temp = *((v128_t *)vec); \
	((lane_type *)&temp)[_lane] = *(lane_type *)value; \
	*((v128_t *)res) = temp;

static void
interp_packedsimd_extractscalar_i1 (gpointer res, gpointer vec, gpointer lane) {
	EXTRACT_LANE(gint32, gint8);
}

static void
interp_packedsimd_extractscalar_u1 (gpointer res, gpointer vec, gpointer lane) {
	EXTRACT_LANE(gint32, guint8);
}

static void
interp_packedsimd_extractscalar_i2 (gpointer res, gpointer vec, gpointer lane) {
	EXTRACT_LANE(gint32, gint16);
}

static void
interp_packedsimd_extractscalar_u2 (gpointer res, gpointer vec, gpointer lane) {
	EXTRACT_LANE(gint32, guint16);
}

static void
interp_packedsimd_extractscalar_i4 (gpointer res, gpointer vec, gpointer lane) {
	EXTRACT_LANE(gint32, gint32);
}

static void
interp_packedsimd_extractscalar_i8 (gpointer res, gpointer vec, gpointer lane) {
	EXTRACT_LANE(gint64, gint64);
}

static void
interp_packedsimd_extractscalar_r4 (gpointer res, gpointer vec, gpointer lane) {
	EXTRACT_LANE(float, float);
}

static void
interp_packedsimd_extractscalar_r8 (gpointer res, gpointer vec, gpointer lane) {
	EXTRACT_LANE(double, double);
}

static void
interp_packedsimd_replacescalar_i1 (gpointer res, gpointer vec, gpointer lane, gpointer value) {
	REPLACE_LANE(gint8);
}

static void
interp_packedsimd_replacescalar_i2 (gpointer res, gpointer vec, gpointer lane, gpointer value) {
	REPLACE_LANE(gint16);
}

static void
interp_packedsimd_replacescalar_i4 (gpointer res, gpointer vec, gpointer lane, gpointer value) {
	REPLACE_LANE(gint32);
}

static void
interp_packedsimd_replacescalar_i8 (gpointer res, gpointer vec, gpointer lane, gpointer value) {
	REPLACE_LANE(gint64);
}

static void
interp_packedsimd_replacescalar_r4 (gpointer res, gpointer vec, gpointer lane, gpointer value) {
	REPLACE_LANE(float);
}

static void
interp_packedsimd_replacescalar_r8 (gpointer res, gpointer vec, gpointer lane, gpointer value) {
	REPLACE_LANE(double);
}

static void
interp_packedsimd_shuffle (gpointer res, gpointer _lower, gpointer _upper, gpointer _indices) {
	v128_i1 indices = *((v128_i1 *)_indices),
		lower = *((v128_i1 *)_lower),
		upper = *((v128_i1 *)_upper),
		result = { 0 };

	for (int i = 0; i < 16; i++) {
		int index = indices[i] & 31;
		if (index > 15)
			result[i] = upper[index - 16];
		else
			result[i] = lower[index];
	}

	*((v128_i1 *)res) = result;
}

#define INDIRECT_LOAD(fn) \
	*(v128_t*)res = fn(*(void **)addr_of_addr);

static void
interp_packedsimd_load128 (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_v128_load);
}

static void
interp_packedsimd_load32_zero (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_v128_load32_zero);
}

static void
interp_packedsimd_load64_zero (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_v128_load64_zero);
}

static void
interp_packedsimd_load8_splat (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_v128_load8_splat);
}

static void
interp_packedsimd_load16_splat (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_v128_load16_splat);
}

static void
interp_packedsimd_load32_splat (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_v128_load32_splat);
}

static void
interp_packedsimd_load64_splat (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_v128_load64_splat);
}

static void
interp_packedsimd_load8x8_s (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_i16x8_load8x8);
}

static void
interp_packedsimd_load8x8_u (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_u16x8_load8x8);
}

static void
interp_packedsimd_load16x4_s (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_i32x4_load16x4);
}

static void
interp_packedsimd_load16x4_u (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_u32x4_load16x4);
}

static void
interp_packedsimd_load32x2_s (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_i64x2_load32x2);
}

static void
interp_packedsimd_load32x2_u (gpointer res, gpointer addr_of_addr) {
	INDIRECT_LOAD(wasm_u64x2_load32x2);
}

static void
interp_packedsimd_store (gpointer res, gpointer addr_of_addr, gpointer vec) {
	// HACK: Result is unused because Store has a void return value
	**(v128_t **)addr_of_addr = *(v128_t *)vec;
}

#define INDIRECT_STORE_LANE(lane_type) \
	int _lane = WRAP_LANE(lane_type, lane); \
	**(lane_type **)addr_of_addr = ((lane_type *)vec)[_lane];

static void
interp_packedsimd_store8_lane (gpointer res, gpointer addr_of_addr, gpointer vec, gpointer lane) {
	INDIRECT_STORE_LANE(guint8);
}

static void
interp_packedsimd_store16_lane (gpointer res, gpointer addr_of_addr, gpointer vec, gpointer lane) {
	INDIRECT_STORE_LANE(guint16);
}

static void
interp_packedsimd_store32_lane (gpointer res, gpointer addr_of_addr, gpointer vec, gpointer lane) {
	INDIRECT_STORE_LANE(guint32);
}

static void
interp_packedsimd_store64_lane (gpointer res, gpointer addr_of_addr, gpointer vec, gpointer lane) {
	INDIRECT_STORE_LANE(guint64);
}

#define INDIRECT_LOAD_LANE(lane_type) \
	int _lane = WRAP_LANE(lane_type, lane); \
	/* we need temporary storage to do this since res may be the same as vec, addr_of_addr, or lane */ \
	lane_type lanes[LANE_COUNT(lane_type)]; \
	memcpy (lanes, vec, 16); \
	lanes[_lane] = **(lane_type **)addr_of_addr; \
	memcpy (res, lanes, 16);

static void
interp_packedsimd_load8_lane (gpointer res, gpointer addr_of_addr, gpointer vec, gpointer lane) {
	INDIRECT_LOAD_LANE(guint8);
}

static void
interp_packedsimd_load16_lane (gpointer res, gpointer addr_of_addr, gpointer vec, gpointer lane) {
	INDIRECT_LOAD_LANE(guint16);
}

static void
interp_packedsimd_load32_lane (gpointer res, gpointer addr_of_addr, gpointer vec, gpointer lane) {
	INDIRECT_LOAD_LANE(guint32);
}

static void
interp_packedsimd_load64_lane (gpointer res, gpointer addr_of_addr, gpointer vec, gpointer lane) {
	INDIRECT_LOAD_LANE(guint64);
}

#define INTERP_WASM_SIMD_INTRINSIC_V_P(name, arg1, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## c_intrinsic (gpointer res, gpointer v1) { \
	*((v128_t *)res) = c_intrinsic (v1); \
}

#define INTERP_WASM_SIMD_INTRINSIC_V_V(name, arg1, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## c_intrinsic (gpointer res, gpointer v1) { \
	*((v128_t *)res) = c_intrinsic (*((v128_t *)v1)); \
}

#define INTERP_WASM_SIMD_INTRINSIC_I_V(name, arg1, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## c_intrinsic (gpointer res, gpointer v1) { \
	*((int32_t *)res) = c_intrinsic (*((v128_t *)v1)); \
}

#define INTERP_WASM_SIMD_INTRINSIC_V_VV(name, arg1, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## c_intrinsic (gpointer res, gpointer v1, gpointer v2) { \
	*((v128_t *)res) = c_intrinsic (*((v128_t *)v1), *((v128_t *)v2)); \
}

#define INTERP_WASM_SIMD_INTRINSIC_V_VI(name, arg1, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## c_intrinsic (gpointer res, gpointer v1, gpointer v2) { \
	*((v128_t *)res) = c_intrinsic (*((v128_t *)v1), *((int *)v2)); \
}

#define INTERP_WASM_SIMD_INTRINSIC_V_VVV(name, arg1, c_intrinsic, wasm_opcode) \
static void \
_mono_interp_simd_ ## c_intrinsic (gpointer res, gpointer v1, gpointer v2, gpointer v3) { \
	*((v128_t *)res) = c_intrinsic (*((v128_t *)v1), *((v128_t *)v2), *((v128_t *)v3)); \
}

#define INTERP_WASM_SIMD_INTRINSIC_V_C1(name, arg1, c_function, wasm_opcode)
#define INTERP_WASM_SIMD_INTRINSIC_V_C2(name, arg1, c_function, wasm_opcode)
#define INTERP_WASM_SIMD_INTRINSIC_V_C3(name, arg1, c_function, wasm_opcode)

#include "interp-simd-intrins.def"

#undef INTERP_WASM_SIMD_INTRINSIC_V_P
#undef INTERP_WASM_SIMD_INTRINSIC_V_V
#undef INTERP_WASM_SIMD_INTRINSIC_I_V
#undef INTERP_WASM_SIMD_INTRINSIC_V_VV
#undef INTERP_WASM_SIMD_INTRINSIC_V_VI
#undef INTERP_WASM_SIMD_INTRINSIC_V_VVV
#undef INTERP_WASM_SIMD_INTRINSIC_V_C1
#undef INTERP_WASM_SIMD_INTRINSIC_V_C2
#undef INTERP_WASM_SIMD_INTRINSIC_V_C3

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
