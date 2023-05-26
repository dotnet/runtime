/*
 * SIMD Intrinsics support for interpreter
 */

#include "config.h"
#include <glib.h>
#include <mono/utils/bsearch.h>

// We use the same approach as jit/aot for identifying simd methods.
// FIXME Consider sharing the code

#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define SIMD_METHOD(name) char MSGSTRFIELD(__LINE__) [sizeof (#name)];
#include "simd-methods.def"
#undef SIMD_METHOD
} method_names = {
#define SIMD_METHOD(name) #name,
#include "simd-methods.def"
#undef SIMD_METHOD
};

enum {
#define SIMD_METHOD(name) SN_ ## name = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "simd-methods.def"
};

#define method_name(idx) ((const char*)&method_names + (idx))

static int
simd_intrinsic_compare_by_name (const void *key, const void *value)
{
	return strcmp ((const char*)key, method_name (*(guint16*)value));
}

static int
lookup_intrins (guint16 *intrinsics, int size, MonoMethod *cmethod)
{
        guint16 *result = mono_binary_search (cmethod->name, intrinsics, size / sizeof (guint16), sizeof (guint16), &simd_intrinsic_compare_by_name);

        if (result == NULL)
                return -1;
        else
                return (int)*result;
}

// These items need to be in ASCII order, which means alphabetical order where lowercase is after uppercase
// i.e. all 'get_' and 'op_' need to come after regular title-case names
static guint16 sri_vector128_methods [] = {
	SN_AndNot,
	SN_ConditionalSelect,
	SN_Create,
	SN_CreateScalar,
	SN_CreateScalarUnsafe,
	SN_Equals,
	SN_ExtractMostSignificantBits,
	SN_GreaterThan,
	SN_LessThan,
	SN_LessThanOrEqual,
	SN_Narrow,
	SN_ShiftLeft,
	SN_ShiftRightArithmetic,
	SN_ShiftRightLogical,
	SN_Shuffle,
	SN_WidenLower,
	SN_WidenUpper,
	SN_get_IsHardwareAccelerated,
};

static guint16 sri_vector128_t_methods [] = {
	SN_get_AllBitsSet,
	SN_get_Count,
	SN_get_One,
	SN_get_Zero,
	SN_op_Addition,
	SN_op_BitwiseAnd,
	SN_op_BitwiseOr,
	SN_op_Division,
	SN_op_Equality,
	SN_op_ExclusiveOr,
	SN_op_Inequality,
	SN_op_LeftShift,
	SN_op_Multiply,
	SN_op_OnesComplement,
	SN_op_RightShift,
	SN_op_Subtraction,
	SN_op_UnaryNegation,
	SN_op_UnsignedRightShift,
};

static guint16 sri_packedsimd_methods [] = {
	SN_Add,
	SN_And,
	SN_Bitmask,
	SN_CompareEqual,
	SN_CompareNotEqual,
	SN_ConvertNarrowingSignedSaturate,
	SN_ConvertNarrowingUnsignedSaturate,
	SN_Dot,
	SN_Multiply,
	SN_Negate,
	SN_ShiftLeft,
	SN_ShiftRightArithmetic,
	SN_ShiftRightLogical,
	SN_Splat,
	SN_Subtract,
	SN_Swizzle,
	SN_get_IsHardwareAccelerated,
	SN_get_IsSupported,
};

#if HOST_BROWSER

/*
 * maps from INTERP_SIMD_INTRINSIC_WASM_I8X16_xxx to the correct one for the return type,
 * assuming that they are laid out sequentially like this:
 * INTERP_WASM_SIMD_INTRINSIC_V_VV (INTERP_SIMD_INTRINSIC_WASM_I8X16_COMPAREEQUAL, wasm_i8x16_eq, 0x0)
 * INTERP_WASM_SIMD_INTRINSIC_V_VV (INTERP_SIMD_INTRINSIC_WASM_I16X8_COMPAREEQUAL, wasm_i16x8_eq, 0x0)
 * INTERP_WASM_SIMD_INTRINSIC_V_VV (INTERP_SIMD_INTRINSIC_WASM_I32X4_COMPAREEQUAL, wasm_i32x4_eq, 0x0)
 * INTERP_WASM_SIMD_INTRINSIC_V_VV (INTERP_SIMD_INTRINSIC_WASM_I64X2_COMPAREEQUAL, wasm_i64x2_eq, 0x0)
 * INTERP_WASM_SIMD_INTRINSIC_V_VV (INTERP_SIMD_INTRINSIC_WASM_F32X4_COMPAREEQUAL, wasm_f32x4_eq, 0x0)
 * INTERP_WASM_SIMD_INTRINSIC_V_VV (INTERP_SIMD_INTRINSIC_WASM_F64X2_COMPAREEQUAL, wasm_f64x2_eq, 0x0)
 * It is your responsibility to ensure that it's actually laid out this way!
 */

static int sri_packedsimd_offset_from_atype [] = {
	-1, // MONO_TYPE_END        = 0x00,
	-1, // MONO_TYPE_VOID       = 0x01,
	-1, // MONO_TYPE_BOOLEAN    = 0x02,
	-1, // MONO_TYPE_CHAR       = 0x03,
	0, // MONO_TYPE_I1         = 0x04,
	0, // MONO_TYPE_U1         = 0x05,
	1, // MONO_TYPE_I2         = 0x06,
	1, // MONO_TYPE_U2         = 0x07,
	2, // MONO_TYPE_I4         = 0x08,
	2, // MONO_TYPE_U4         = 0x09,
	3, // MONO_TYPE_I8         = 0x0a,
	3, // MONO_TYPE_U8         = 0x0b,
	4, // MONO_TYPE_R4         = 0x0c,
	5, // MONO_TYPE_R8         = 0x0d,
	-1, // MONO_TYPE_STRING     = 0x0e,
	-1, // MONO_TYPE_PTR        = 0x0f,
	-1, // MONO_TYPE_BYREF      = 0x10,
	-1, // MONO_TYPE_VALUETYPE  = 0x11,
	-1, // MONO_TYPE_CLASS      = 0x12,
	-1, // MONO_TYPE_VAR	     = 0x13,
	-1, // MONO_TYPE_ARRAY      = 0x14,
	-1, // MONO_TYPE_GENERICINST= 0x15,
	-1, // MONO_TYPE_TYPEDBYREF = 0x16,
	2, // MONO_TYPE_I          = 0x18,
	2, // MONO_TYPE_U          = 0x19,
};

static const int sri_packedsimd_offset_from_atype_length = sizeof(sri_packedsimd_offset_from_atype) / sizeof(sri_packedsimd_offset_from_atype[0]);
#endif // HOST_BROWSER

static gboolean
emit_sri_vector128 (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature)
{
	int id = lookup_intrins (sri_vector128_methods, sizeof (sri_vector128_methods), cmethod);
	if (id == -1)
		return FALSE;

	MonoClass *vector_klass = mono_class_from_mono_type_internal (csignature->ret);
	if (id == SN_get_IsHardwareAccelerated) {
		interp_add_ins (td, MINT_LDC_I4_1);
		goto opcode_added;
	}

	gint16 simd_opcode = -1;
	gint16 simd_intrins = -1;
	if (!m_class_is_simd_type (vector_klass))
		vector_klass = mono_class_from_mono_type_internal (csignature->params [0]);
	if (!m_class_is_simd_type (vector_klass))
		return FALSE;

	MonoType *arg_type = mono_class_get_context (vector_klass)->class_inst->type_argv [0];
	if (!mono_type_is_primitive (arg_type))
		return FALSE;
	MonoTypeEnum atype = arg_type->type;
	if (atype == MONO_TYPE_BOOLEAN)
		return FALSE;
	int vector_size = mono_class_value_size (vector_klass, NULL);
	int arg_size = mono_class_value_size (mono_class_from_mono_type_internal (arg_type), NULL);
	g_assert (vector_size == SIZEOF_V128);

	int scalar_arg = -1;
	for (int i = 0; i < csignature->param_count; i++) {
		if (csignature->params [i]->type != MONO_TYPE_GENERICINST)
			scalar_arg = i;
	}

	switch (id) {
		case SN_AndNot:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = INTERP_SIMD_INTRINSIC_V128_AND_NOT;
			break;
		case SN_ConditionalSelect:
			simd_opcode = MINT_SIMD_INTRINS_P_PPP;
			simd_intrins = INTERP_SIMD_INTRINSIC_V128_CONDITIONAL_SELECT;
			break;
		case SN_Create:
			if (csignature->param_count == 1 && atype == csignature->params [0]->type) {
				simd_opcode = MINT_SIMD_INTRINS_P_P;
				if (arg_size == 1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_CREATE;
				else if (arg_size == 2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_CREATE;
				else if (arg_size == 4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_CREATE;
				else if (arg_size == 8) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_CREATE;
			} else if (csignature->param_count == vector_size / arg_size && atype == csignature->params [0]->type) {
				int num_args = csignature->param_count;
				if (num_args == 16) interp_add_ins (td, MINT_SIMD_V128_I1_CREATE);
				else if (num_args == 8) interp_add_ins (td, MINT_SIMD_V128_I2_CREATE);
				else if (num_args == 4) interp_add_ins (td, MINT_SIMD_V128_I4_CREATE);
				else if (num_args == 2) interp_add_ins (td, MINT_SIMD_V128_I8_CREATE);
				else g_assert_not_reached ();

				// We use call args machinery since we have too many args
				interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
				int *call_args = (int*)mono_mempool_alloc (td->mempool, (num_args + 1) * sizeof (int));
				td->sp -= csignature->param_count;
				for (int i = 0; i < num_args; i++)
					call_args [i] = td->sp [i].local;
				call_args [num_args] = -1;
				init_last_ins_call (td);
				td->last_ins->info.call_info->call_args = call_args;
				if (!td->optimized)
					td->last_ins->info.call_info->call_offset = get_tos_offset (td);
				push_type_vt (td, vector_klass, vector_size);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].local);
				td->ip += 5;
				return TRUE;
			}
			break;
		case SN_CreateScalar:
		case SN_CreateScalarUnsafe:
			simd_opcode = MINT_SIMD_INTRINS_P_P;
			if (arg_size == 1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_CREATE_SCALAR;
			else if (arg_size == 2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_CREATE_SCALAR;
			else if (arg_size == 4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_CREATE_SCALAR;
			else if (arg_size == 8) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_CREATE_SCALAR;
			break;
		case SN_Equals:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_EQUALS;
			else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_EQUALS;
			else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_EQUALS;
			else if (atype == MONO_TYPE_I8 || atype == MONO_TYPE_U8) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_EQUALS;
			else if (atype == MONO_TYPE_R4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_R4_EQUALS;
			break;
		case SN_ExtractMostSignificantBits:
			simd_opcode = MINT_SIMD_INTRINS_P_P;
			if (arg_size == 1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_EXTRACT_MSB;
			else if (arg_size == 2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_EXTRACT_MSB;
			else if (arg_size == 4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_EXTRACT_MSB;
			else if (arg_size == 8) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_EXTRACT_MSB;
			break;
		case SN_GreaterThan:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_U1_GREATER_THAN;
			break;
		case SN_LessThan:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_LESS_THAN;
			else if (atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_U1_LESS_THAN;
			else if (atype == MONO_TYPE_I2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_LESS_THAN;
			break;
		case SN_LessThanOrEqual:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_U2_LESS_THAN_EQUAL;
			break;
		case SN_Narrow:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_U1_NARROW;
			break;
		case SN_ShiftLeft:
			g_assert (scalar_arg == 1);
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (arg_size == 1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_LEFT_SHIFT;
			else if (arg_size == 2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_LEFT_SHIFT;
			else if (arg_size == 4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_LEFT_SHIFT;
			else if (arg_size == 8) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_LEFT_SHIFT;
			break;
		case SN_ShiftRightLogical:
			g_assert (scalar_arg == 1);
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (arg_size == 1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_URIGHT_SHIFT;
			else if (arg_size == 2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_URIGHT_SHIFT;
			else if (arg_size == 4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_URIGHT_SHIFT;
			else if (arg_size == 8) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_URIGHT_SHIFT;
			break;
		case SN_ShiftRightArithmetic:
			g_assert (scalar_arg == 1);
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_RIGHT_SHIFT;
			else if (atype == MONO_TYPE_I2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_RIGHT_SHIFT;
			else if (atype == MONO_TYPE_I4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_RIGHT_SHIFT;
			else if (atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_URIGHT_SHIFT;
			else if (atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_URIGHT_SHIFT;
			else if (atype == MONO_TYPE_U4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_URIGHT_SHIFT;
			break;
		case SN_Shuffle:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (arg_size == 1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_SHUFFLE;
			else if (arg_size == 2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_SHUFFLE;
			else if (arg_size == 4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_SHUFFLE;
			else if (arg_size == 8) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_SHUFFLE;
			break;
		case SN_WidenLower:
			simd_opcode = MINT_SIMD_INTRINS_P_P;
			if (atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_U2_WIDEN_LOWER;
			break;
		case SN_WidenUpper:
			simd_opcode = MINT_SIMD_INTRINS_P_P;
			if (atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_U2_WIDEN_UPPER;
			break;
		default:
			return FALSE;
	}

	if (simd_opcode == -1 || simd_intrins == -1) {
		return FALSE;
	}

	interp_add_ins (td, simd_opcode);
	td->last_ins->data [0] = simd_intrins;

opcode_added:
	td->sp -= csignature->param_count;
	for (int i = 0; i < csignature->param_count; i++)
		td->last_ins->sregs [i] = td->sp [i].local;

	g_assert (csignature->ret->type != MONO_TYPE_VOID);
	int ret_mt = mono_mint_type (csignature->ret);
	if (ret_mt == MINT_TYPE_VT) {
		// For these intrinsics, if we return a VT then it is a V128
		push_type_vt (td, vector_klass, vector_size);
	} else {
		push_simple_type (td, stack_type [ret_mt]);
	}
	interp_ins_set_dreg (td->last_ins, td->sp [-1].local);
	td->ip += 5;
	return TRUE;
}

static gboolean
emit_sri_vector128_t (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature)
{
	int id = lookup_intrins (sri_vector128_t_methods, sizeof (sri_vector128_t_methods), cmethod);
	if (id == -1)
		return FALSE;

	gint16 simd_opcode = -1;
	gint16 simd_intrins = -1;

	// First argument is always vector
	MonoClass *vector_klass = cmethod->klass;
	MonoType *arg_type = mono_class_get_context (vector_klass)->class_inst->type_argv [0];
	if (!mono_type_is_primitive (arg_type))
		return FALSE;
	MonoTypeEnum atype = arg_type->type;
	if (atype == MONO_TYPE_BOOLEAN)
		return FALSE;
	int vector_size = mono_class_value_size (vector_klass, NULL);
	int arg_size = mono_class_value_size (mono_class_from_mono_type_internal (arg_type), NULL);
	g_assert (vector_size == SIZEOF_V128);

	int scalar_arg = -1;
	for (int i = 0; i < csignature->param_count; i++) {
		if (csignature->params [i]->type != MONO_TYPE_GENERICINST)
			scalar_arg = i;
	}

	switch (id) {
		case SN_get_AllBitsSet: {
			interp_add_ins (td, MINT_SIMD_V128_LDC);
			guint16 *data = &td->last_ins->data [0];
			for (int i = 0; i < vector_size / sizeof (guint16); i++)
				data [i] = 0xffff;
			goto opcode_added;
		}
		case SN_get_Count:
			interp_add_ins (td, MINT_LDC_I4_S);
			td->last_ins->data [0] = vector_size / arg_size;
			goto opcode_added;
		case SN_get_One:
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) {
				interp_add_ins (td, MINT_SIMD_V128_LDC);
				gint8 *data = (gint8*)&td->last_ins->data [0];
				for (int i = 0; i < vector_size / arg_size; i++)
					data [i] = 1;
				goto opcode_added;
			} else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) {
				interp_add_ins (td, MINT_SIMD_V128_LDC);
				gint16 *data = (gint16*)&td->last_ins->data [0];
				for (int i = 0; i < vector_size / arg_size; i++)
					data [i] = 1;
				goto opcode_added;
			} else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) {
				interp_add_ins (td, MINT_SIMD_V128_LDC);
				gint32 *data = (gint32*)&td->last_ins->data [0];
				for (int i = 0; i < vector_size / arg_size; i++)
					data [i] = 1;
				goto opcode_added;
			} else if (atype == MONO_TYPE_I8 || atype == MONO_TYPE_U8) {
				interp_add_ins (td, MINT_SIMD_V128_LDC);
				gint64 *data = (gint64*)&td->last_ins->data [0];
				for (int i = 0; i < vector_size / arg_size; i++)
					data [i] = 1;
				goto opcode_added;
			}
			break;
		case SN_get_Zero:
			interp_add_ins (td, MINT_INITLOCAL);
			td->last_ins->data [0] = SIZEOF_V128;
			goto opcode_added;
		case SN_op_Addition:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_ADD;
			else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_ADD;
			else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_ADD;
			break;
		case SN_op_BitwiseAnd:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = INTERP_SIMD_INTRINSIC_V128_BITWISE_AND;
			break;
		case SN_op_BitwiseOr:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = INTERP_SIMD_INTRINSIC_V128_BITWISE_OR;
			break;
		case SN_op_Equality:
			if (atype != MONO_TYPE_R4 && atype != MONO_TYPE_R8) {
				simd_opcode = MINT_SIMD_INTRINS_P_PP;
				simd_intrins = INTERP_SIMD_INTRINSIC_V128_BITWISE_EQUALITY;
			}
			break;
		case SN_op_ExclusiveOr:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = INTERP_SIMD_INTRINSIC_V128_EXCLUSIVE_OR;
			break;
		case SN_op_Inequality:
			if (atype != MONO_TYPE_R4 && atype != MONO_TYPE_R8) {
				simd_opcode = MINT_SIMD_INTRINS_P_PP;
				simd_intrins = INTERP_SIMD_INTRINSIC_V128_BITWISE_INEQUALITY;
			}
			break;
		case SN_op_LeftShift:
			if (scalar_arg != 1)
				return FALSE;
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (arg_size == 1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_LEFT_SHIFT;
			else if (arg_size == 2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_LEFT_SHIFT;
			else if (arg_size == 4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_LEFT_SHIFT;
			else if (arg_size == 8) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_LEFT_SHIFT;
			break;
		case SN_op_Division:
			if (scalar_arg != -1)
				return FALSE;
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_R4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_R4_DIVISION;
			break;
		case SN_op_Multiply:
			if (scalar_arg != -1)
				return FALSE;
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_MULTIPLY;
			else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_MULTIPLY;
			else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_MULTIPLY;
			else if (atype == MONO_TYPE_R4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_R4_MULTIPLY;
			break;
		case SN_op_OnesComplement:
			simd_opcode = MINT_SIMD_INTRINS_P_P;
			simd_intrins = INTERP_SIMD_INTRINSIC_V128_ONES_COMPLEMENT;
			break;
		case SN_op_RightShift:
			if (scalar_arg != 1)
				return FALSE;
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_RIGHT_SHIFT;
			else if (atype == MONO_TYPE_I2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_RIGHT_SHIFT;
			else if (atype == MONO_TYPE_I4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_RIGHT_SHIFT;
			else if (atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_URIGHT_SHIFT;
			else if (atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_URIGHT_SHIFT;
			else if (atype == MONO_TYPE_U4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_URIGHT_SHIFT;
			break;
		case SN_op_Subtraction:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_SUB;
			else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_SUB;
			else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_SUB;
			break;
		case SN_op_UnaryNegation:
			simd_opcode = MINT_SIMD_INTRINS_P_P;
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_NEGATION;
			else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_NEGATION;
			else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_NEGATION;
			break;
		case SN_op_UnsignedRightShift:
			if (scalar_arg != 1)
				return FALSE;
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (arg_size == 1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_URIGHT_SHIFT;
			else if (arg_size == 2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_URIGHT_SHIFT;
			else if (arg_size == 4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_URIGHT_SHIFT;
			else if (arg_size == 8) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_URIGHT_SHIFT;
			break;
	}

	if (simd_opcode == -1 || simd_intrins == -1) {
		return FALSE;
	}

	interp_add_ins (td, simd_opcode);
	td->last_ins->data [0] = simd_intrins;

opcode_added:
	td->sp -= csignature->param_count;
	for (int i = 0; i < csignature->param_count; i++)
		td->last_ins->sregs [i] = td->sp [i].local;

	g_assert (csignature->ret->type != MONO_TYPE_VOID);
	int ret_mt = mono_mint_type (csignature->ret);
	if (ret_mt == MINT_TYPE_VT) {
		// For these intrinsics, if we return a VT then it is a V128
		push_type_vt (td, vector_klass, vector_size);
	} else {
		push_simple_type (td, stack_type [ret_mt]);
	}
	interp_ins_set_dreg (td->last_ins, td->sp [-1].local);
	td->ip += 5;
	return TRUE;
}

#if HOST_BROWSER
static int
map_packedsimd_intrins_based_on_atype (MonoTypeEnum atype, int base_intrins, gboolean allow_float)
{
	int max_offset = allow_float ? 5 : 3;
	if ((atype < 0) || (atype >= sri_packedsimd_offset_from_atype_length))
		return -1;
	int offset = sri_packedsimd_offset_from_atype [atype];
	if ((offset < 0) || (offset > max_offset))
		return -1;
	return base_intrins + offset;
}
#endif

static gboolean
emit_sri_packedsimd (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature)
{
	int id = lookup_intrins (sri_packedsimd_methods, sizeof (sri_packedsimd_methods), cmethod);
	// We don't early-out for an unrecognized method, we will generate an NIY later

	MonoClass *vector_klass = mono_class_from_mono_type_internal (csignature->ret);
	int vector_size = -1;

	// NOTE: Linker substitutions (used in AOT) will prevent this from running.
	if ((id == SN_get_IsSupported) || (id == SN_get_IsHardwareAccelerated)) {
#if HOST_BROWSER
		interp_add_ins (td, MINT_LDC_I4_1);
#else
		interp_add_ins (td, MINT_LDC_I4_0);
#endif
		goto opcode_added;
	}

#if HOST_BROWSER
	if (id < 0) {
		g_print ("MONO interpreter: Unimplemented method: System.Runtime.Intrinsics.Wasm.PackedSimd.%s\n", cmethod->name);

		// If we're missing a packedsimd method but the packedsimd method was AOT'd, we can
		//  just let the interpreter generate a native call to the AOT method instead of
		//  generating an NIY that will halt execution
		ERROR_DECL (error);
		gpointer addr = mono_aot_get_method (cmethod, error);
		if (addr)
			return FALSE;

		// The packedsimd method implementations recurse infinitely and cause a stack overflow,
		//  so replace them with a NIY opcode instead that will assert
		interp_add_ins (td, MINT_NIY);
		goto opcode_added;
	}

	gint16 simd_opcode = -1;
	gint16 simd_intrins = -1;
	if (!m_class_is_simd_type (vector_klass))
		vector_klass = mono_class_from_mono_type_internal (csignature->params [0]);
	if (!m_class_is_simd_type (vector_klass))
		return FALSE;

	vector_size = mono_class_value_size (vector_klass, NULL);
	g_assert (vector_size == SIZEOF_V128);

	MonoType *arg_type = mono_class_get_context (vector_klass)->class_inst->type_argv [0];
	if (!mono_type_is_primitive (arg_type))
		return FALSE;
	MonoTypeEnum atype = arg_type->type;
	if (atype == MONO_TYPE_BOOLEAN)
		return FALSE;

	int scalar_arg = -1;
	for (int i = 0; i < csignature->param_count; i++) {
		if (csignature->params [i]->type != MONO_TYPE_GENERICINST)
			scalar_arg = i;
	}

	switch (id) {
		case SN_Splat: {
			simd_opcode = MINT_SIMD_INTRINS_P_P;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_SPLAT, FALSE);
			break;
		}
		case SN_Swizzle: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = INTERP_SIMD_INTRINSIC_WASM_I8X16_SWIZZLE;
			break;
		}
		case SN_Add: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_ADD, FALSE);
			break;
		}
		case SN_Subtract: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_SUBTRACT, FALSE);
			break;
		}
		case SN_Multiply: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_MULTIPLY, FALSE);
			break;
		}
		case SN_Dot: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = INTERP_SIMD_INTRINSIC_WASM_I32X4_DOT_I16X8;
			break;
		}
		case SN_Negate: {
			simd_opcode = MINT_SIMD_INTRINS_P_P;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_NEGATE, FALSE);
			break;
		}
		case SN_ShiftLeft: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_SHIFTLEFT, FALSE);
			break;
		}
		case SN_ShiftRightArithmetic: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_SHIFTRIGHTARITHMETIC, FALSE);
			break;
		}
		case SN_ShiftRightLogical: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_SHIFTRIGHTLOGICAL, FALSE);
			break;
		}
		case SN_And: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = INTERP_SIMD_INTRINSIC_WASM_AND;
			break;
		}
		case SN_Bitmask: {
			simd_opcode = MINT_SIMD_INTRINS_P_P;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_BITMASK, FALSE);
			break;
		}
		case SN_CompareEqual: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_COMPAREEQUAL, TRUE);
			break;
		}
		case SN_CompareNotEqual: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			simd_intrins = map_packedsimd_intrins_based_on_atype (atype, INTERP_SIMD_INTRINSIC_WASM_I8X16_COMPARENOTEQUAL, TRUE);
			break;
		}
		case SN_ConvertNarrowingSignedSaturate: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1)
				simd_intrins = INTERP_SIMD_INTRINSIC_WASM_I8X16_NARROW_I16X8_S;
			else if (atype == MONO_TYPE_I2)
				simd_intrins = INTERP_SIMD_INTRINSIC_WASM_I16X8_NARROW_I32X4_S;
			break;
		}
		case SN_ConvertNarrowingUnsignedSaturate: {
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_U1)
				simd_intrins = INTERP_SIMD_INTRINSIC_WASM_I8X16_NARROW_I16X8_U;
			else if (atype == MONO_TYPE_U2)
				simd_intrins = INTERP_SIMD_INTRINSIC_WASM_I16X8_NARROW_I32X4_U;
			break;
		}
		default:
			return FALSE;
	}

	if (simd_opcode == -1 || simd_intrins == -1) {
		return FALSE;
	}

	interp_add_ins (td, simd_opcode);
	td->last_ins->data [0] = simd_intrins;
#else // HOST_BROWSER
	return FALSE;
#endif // HOST_BROWSER

opcode_added:
	td->sp -= csignature->param_count;
	for (int i = 0; i < csignature->param_count; i++)
		td->last_ins->sregs [i] = td->sp [i].local;

	g_assert (csignature->ret->type != MONO_TYPE_VOID);
	int ret_mt = mono_mint_type (csignature->ret);
	if (ret_mt == MINT_TYPE_VT) {
		// For these intrinsics, if we return a VT then it is a V128
		push_type_vt (td, vector_klass, vector_size);
	} else {
		push_simple_type (td, stack_type [ret_mt]);
	}
	interp_ins_set_dreg (td->last_ins, td->sp [-1].local);
	td->ip += 5;
	return TRUE;
}

static gboolean
interp_emit_simd_intrinsics (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature)
{
	const char *class_name;
	const char *class_ns;
	MonoImage *image = m_class_get_image (cmethod->klass);

	if (image != mono_get_corlib ())
		return FALSE;

	class_ns = m_class_get_name_space (cmethod->klass);
	class_name = m_class_get_name (cmethod->klass);

	if (!strcmp (class_ns, "System.Runtime.Intrinsics")) {
		if (!strcmp (class_name, "Vector128"))
			return emit_sri_vector128 (td, cmethod, csignature);
		else if (!strcmp (class_name, "Vector128`1"))
			return emit_sri_vector128_t (td, cmethod, csignature);
	} else if (!strcmp (class_ns, "System.Runtime.Intrinsics.Wasm")) {
		if (!strcmp (class_name, "PackedSimd"))
			return emit_sri_packedsimd (td, cmethod, csignature);
	}
	return FALSE;
}
