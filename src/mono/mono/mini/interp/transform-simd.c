/*
 * SIMD Intrinsics support for interpreter
 */

#include "config.h"
#include <glib.h>
#include <mono/utils/bsearch.h>
#include <mono/metadata/class-internals.h>

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

static guint16 sn_vector_t_methods [] = {
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
	SN_Abs,
	SN_Add,
	SN_AddPairwiseWidening,
	SN_AddSaturate,
	SN_AllTrue,
	SN_And,
	SN_AndNot,
	SN_AnyTrue,
	SN_AverageRounded,
	SN_Bitmask,
	SN_BitwiseSelect,
	SN_Ceiling,
	SN_CompareEqual,
	SN_CompareGreaterThan,
	SN_CompareGreaterThanOrEqual,
	SN_CompareLessThan,
	SN_CompareLessThanOrEqual,
	SN_CompareNotEqual,
	SN_ConvertNarrowingSignedSaturate,
	SN_ConvertNarrowingUnsignedSaturate,
	SN_ConvertToDoubleLower,
	SN_ConvertToInt32Saturate,
	SN_ConvertToSingle,
	SN_ConvertToUnsignedInt32Saturate,
	SN_Divide,
	SN_Dot,
	SN_Floor,
	SN_Max,
	SN_Min,
	SN_Multiply,
	SN_MultiplyWideningLower,
	SN_MultiplyWideningUpper,
	SN_Negate,
	SN_Not,
	SN_Or,
	SN_PopCount,
	SN_PseudoMax,
	SN_PseudoMin,
	SN_RoundToNearest,
	SN_ShiftLeft,
	SN_ShiftRightArithmetic,
	SN_ShiftRightLogical,
	SN_SignExtendWideningLower,
	SN_SignExtendWideningUpper,
	SN_Splat,
	SN_Sqrt,
	SN_Subtract,
	SN_SubtractSaturate,
	SN_Swizzle,
	SN_Truncate,
	SN_Xor,
	SN_ZeroExtendWideningLower,
	SN_ZeroExtendWideningUpper,
	SN_get_IsHardwareAccelerated,
	SN_get_IsSupported,
};

// Returns if opcode was added
static gboolean
emit_common_simd_operations (TransformData *td, int id, int atype, int vector_size, int arg_size, int scalar_arg, gint16 *simd_opcode, gint16 *simd_intrins)
{
	switch (id) {
		case SN_get_AllBitsSet: {
			interp_add_ins (td, MINT_SIMD_V128_LDC);
			guint16 *data = &td->last_ins->data [0];
			for (int i = 0; i < vector_size / sizeof (guint16); i++)
				data [i] = 0xffff;
			return TRUE;
		}
		case SN_get_Count:
			interp_add_ins (td, MINT_LDC_I4_S);
			td->last_ins->data [0] = vector_size / arg_size;
			return TRUE;
		case SN_get_One:
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) {
				interp_add_ins (td, MINT_SIMD_V128_LDC);
				gint8 *data = (gint8*)&td->last_ins->data [0];
				for (int i = 0; i < vector_size / arg_size; i++)
					data [i] = 1;
				return TRUE;
			} else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) {
				interp_add_ins (td, MINT_SIMD_V128_LDC);
				gint16 *data = (gint16*)&td->last_ins->data [0];
				for (int i = 0; i < vector_size / arg_size; i++)
					data [i] = 1;
				return TRUE;
			} else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) {
				interp_add_ins (td, MINT_SIMD_V128_LDC);
				gint32 *data = (gint32*)&td->last_ins->data [0];
				for (int i = 0; i < vector_size / arg_size; i++)
					data [i] = 1;
				return TRUE;
			} else if (atype == MONO_TYPE_I8 || atype == MONO_TYPE_U8) {
				interp_add_ins (td, MINT_SIMD_V128_LDC);
				gint64 *data = (gint64*)&td->last_ins->data [0];
				for (int i = 0; i < vector_size / arg_size; i++)
					data [i] = 1;
				return TRUE;
			}
			break;
		case SN_get_Zero:
			interp_add_ins (td, MINT_INITLOCAL);
			td->last_ins->data [0] = SIZEOF_V128;
			return TRUE;
		case SN_op_Addition:
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_ADD;
			else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_ADD;
			else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_ADD;
			else if (atype == MONO_TYPE_R4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_R4_ADD;
			break;
		case SN_op_BitwiseAnd:
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			*simd_intrins = INTERP_SIMD_INTRINSIC_V128_BITWISE_AND;
			break;
		case SN_op_BitwiseOr:
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			*simd_intrins = INTERP_SIMD_INTRINSIC_V128_BITWISE_OR;
			break;
		case SN_op_Equality:
			if (atype != MONO_TYPE_R4 && atype != MONO_TYPE_R8) {
				*simd_opcode = MINT_SIMD_INTRINS_P_PP;
				*simd_intrins = INTERP_SIMD_INTRINSIC_V128_BITWISE_EQUALITY;
			}
			break;
		case SN_op_ExclusiveOr:
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			*simd_intrins = INTERP_SIMD_INTRINSIC_V128_EXCLUSIVE_OR;
			break;
		case SN_op_Inequality:
			if (atype != MONO_TYPE_R4 && atype != MONO_TYPE_R8) {
				*simd_opcode = MINT_SIMD_INTRINS_P_PP;
				*simd_intrins = INTERP_SIMD_INTRINSIC_V128_BITWISE_INEQUALITY;
			}
			break;
		case SN_op_LeftShift:
			if (scalar_arg != 1)
				break;
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (arg_size == 1) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_LEFT_SHIFT;
			else if (arg_size == 2) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_LEFT_SHIFT;
			else if (arg_size == 4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_LEFT_SHIFT;
			else if (arg_size == 8) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_LEFT_SHIFT;
			break;
		case SN_op_Division:
			if (scalar_arg != -1)
				break;
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_R4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_R4_DIVISION;
			break;
		case SN_op_Multiply:
			if (scalar_arg != -1)
				break;
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_MULTIPLY;
			else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_MULTIPLY;
			else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_MULTIPLY;
			else if (atype == MONO_TYPE_R4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_R4_MULTIPLY;
			break;
		case SN_op_OnesComplement:
			*simd_opcode = MINT_SIMD_INTRINS_P_P;
			*simd_intrins = INTERP_SIMD_INTRINSIC_V128_ONES_COMPLEMENT;
			break;
		case SN_op_RightShift:
			if (scalar_arg != 1)
				break;
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_RIGHT_SHIFT;
			else if (atype == MONO_TYPE_I2) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_RIGHT_SHIFT;
			else if (atype == MONO_TYPE_I4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_RIGHT_SHIFT;
			else if (atype == MONO_TYPE_U1) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_URIGHT_SHIFT;
			else if (atype == MONO_TYPE_U2) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_URIGHT_SHIFT;
			else if (atype == MONO_TYPE_U4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_URIGHT_SHIFT;
			break;
		case SN_op_Subtraction:
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_SUB;
			else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_SUB;
			else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_SUB;
			else if (atype == MONO_TYPE_R4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_R4_SUB;
			break;
		case SN_op_UnaryNegation:
			*simd_opcode = MINT_SIMD_INTRINS_P_P;
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_NEGATION;
			else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_NEGATION;
			else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_NEGATION;
			break;
		case SN_op_UnsignedRightShift:
			if (scalar_arg != 1)
				break;
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (arg_size == 1) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_URIGHT_SHIFT;
			else if (arg_size == 2) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_URIGHT_SHIFT;
			else if (arg_size == 4) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_URIGHT_SHIFT;
			else if (arg_size == 8) *simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_URIGHT_SHIFT;
			break;
	}

	return FALSE;
}

static gboolean
get_common_simd_info (MonoClass *vector_klass, MonoMethodSignature *csignature, MonoTypeEnum *atype, int *vector_size, int *arg_size, int *scalar_arg)
{
	if (!m_class_is_simd_type (vector_klass) && csignature->param_count)
		vector_klass = mono_class_from_mono_type_internal (csignature->params [0]);
	if (!m_class_is_simd_type (vector_klass))
		return FALSE;

	MonoType *arg_type = mono_class_get_context (vector_klass)->class_inst->type_argv [0];
	if (!mono_type_is_primitive (arg_type))
		return FALSE;
	*atype = arg_type->type;
	if (*atype == MONO_TYPE_BOOLEAN)
		return FALSE;
	*vector_size = mono_class_value_size (vector_klass, NULL);
	g_assert (*vector_size == SIZEOF_V128);
	if (arg_size)
		*arg_size = mono_class_value_size (mono_class_from_mono_type_internal (arg_type), NULL);

	*scalar_arg = -1;
	for (int i = 0; i < csignature->param_count; i++) {
		if (csignature->params [i]->type != MONO_TYPE_GENERICINST)
			*scalar_arg = i;
	}

	return TRUE;
}

static void
emit_common_simd_epilogue (TransformData *td, MonoClass *vector_klass, MonoMethodSignature *csignature, int vector_size)
{
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
}

static gboolean
emit_sri_vector128 (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature)
{
	int id = lookup_intrins (sri_vector128_methods, sizeof (sri_vector128_methods), cmethod);
	if (id == -1)
		return FALSE;

	MonoClass *vector_klass = NULL;
	int vector_size = 0;

	if (id == SN_get_IsHardwareAccelerated) {
		interp_add_ins (td, MINT_LDC_I4_1);
		goto opcode_added;
	}

	gint16 simd_opcode = -1;
	gint16 simd_intrins = -1;

	vector_klass = mono_class_from_mono_type_internal (csignature->ret);

	MonoTypeEnum atype;
	int arg_size, scalar_arg;
	if (!get_common_simd_info (vector_klass, csignature, &atype, &vector_size, &arg_size, &scalar_arg))
		return FALSE;

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
	emit_common_simd_epilogue (td, vector_klass, csignature, vector_size);
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

	MonoTypeEnum atype;
	int vector_size, arg_size, scalar_arg;
	if (!get_common_simd_info (vector_klass, csignature, &atype, &vector_size, &arg_size, &scalar_arg))
		return FALSE;

	if (emit_common_simd_operations (td, id, atype, vector_size, arg_size, scalar_arg, &simd_opcode, &simd_intrins))
		goto opcode_added;

	if (simd_opcode == -1 || simd_intrins == -1)
		return FALSE;

	interp_add_ins (td, simd_opcode);
	td->last_ins->data [0] = simd_intrins;

opcode_added:
	emit_common_simd_epilogue (td, vector_klass, csignature, vector_size);
	return TRUE;
}

static gboolean
emit_sn_vector_t (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature)
{
	int id = lookup_intrins (sn_vector_t_methods, sizeof (sn_vector_t_methods), cmethod);
	if (id == -1)
		return FALSE;

	gint16 simd_opcode = -1;
	gint16 simd_intrins = -1;

	// First argument is always vector
	MonoClass *vector_klass = cmethod->klass;

	MonoTypeEnum atype;
	int vector_size, arg_size, scalar_arg;
	if (!get_common_simd_info (vector_klass, csignature, &atype, &vector_size, &arg_size, &scalar_arg))
		return FALSE;

	if (emit_common_simd_operations (td, id, atype, vector_size, arg_size, scalar_arg, &simd_opcode, &simd_intrins))
		goto opcode_added;

	if (simd_opcode == -1 || simd_intrins == -1)
		return FALSE;

	interp_add_ins (td, simd_opcode);
	td->last_ins->data [0] = simd_intrins;

opcode_added:
	emit_common_simd_epilogue (td, vector_klass, csignature, vector_size);
	return TRUE;
}

#if HOST_BROWSER

#define PSIMD_ARGTYPE_I1 MONO_TYPE_I1
#define PSIMD_ARGTYPE_I2 MONO_TYPE_I2
#define PSIMD_ARGTYPE_I4 MONO_TYPE_I4
#define PSIMD_ARGTYPE_I8 MONO_TYPE_I8
#define PSIMD_ARGTYPE_U1 MONO_TYPE_U1
#define PSIMD_ARGTYPE_U2 MONO_TYPE_U2
#define PSIMD_ARGTYPE_U4 MONO_TYPE_U4
#define PSIMD_ARGTYPE_U8 MONO_TYPE_U8
#define PSIMD_ARGTYPE_R4 MONO_TYPE_R4
#define PSIMD_ARGTYPE_R8 MONO_TYPE_R8
#define PSIMD_ARGTYPE_D1 0xF01
#define PSIMD_ARGTYPE_D2 0xF02
#define PSIMD_ARGTYPE_D4 0xF04
#define PSIMD_ARGTYPE_D8 0xF08
#define PSIMD_ARGTYPE_X1 0xF11
#define PSIMD_ARGTYPE_X2 0xF12
#define PSIMD_ARGTYPE_X4 0xF14
#define PSIMD_ARGTYPE_X8 0xF18
#define PSIMD_ARGTYPE_ANY 0xFFF

static gboolean
packedsimd_type_matches (MonoTypeEnum type, int expected_type)
{
	if (expected_type == PSIMD_ARGTYPE_ANY)
		return TRUE;
	else if (type == expected_type)
		return TRUE;

	switch (expected_type) {
		case PSIMD_ARGTYPE_D1:
		case PSIMD_ARGTYPE_X1:
			return (type == MONO_TYPE_I1) || (type == MONO_TYPE_U1);
		case PSIMD_ARGTYPE_D2:
		case PSIMD_ARGTYPE_X2:
			return (type == MONO_TYPE_I2) || (type == MONO_TYPE_U2);
		case PSIMD_ARGTYPE_D4:
			return (type == MONO_TYPE_I4) || (type == MONO_TYPE_U4);
		case PSIMD_ARGTYPE_D8:
			return (type == MONO_TYPE_I8) || (type == MONO_TYPE_U8);
		case PSIMD_ARGTYPE_X4:
			return (type == MONO_TYPE_I4) || (type == MONO_TYPE_U4) || (type == MONO_TYPE_R4);
		case PSIMD_ARGTYPE_X8:
			return (type == MONO_TYPE_I8) || (type == MONO_TYPE_U8) || (type == MONO_TYPE_R8);
		default:
			return FALSE;
	}
}

typedef struct {
	const char *name, *intrinsic_name;
	int arg_type, interp_opcode, simd_intrins;
} PackedSimdIntrinsicInfo;

#define INTRINS_COMMON(_name, arg1, c_intrinsic, _interp_opcode, _id) \
	{ #_name, #c_intrinsic, PSIMD_ARGTYPE_ ## arg1, _interp_opcode, _id },

#undef INTERP_WASM_SIMD_INTRINSIC_V_P
#define INTERP_WASM_SIMD_INTRINSIC_V_P(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_P, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_V_V
#define INTERP_WASM_SIMD_INTRINSIC_V_V(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_P, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_I_V
#define INTERP_WASM_SIMD_INTRINSIC_I_V(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_P, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_V_VV
#define INTERP_WASM_SIMD_INTRINSIC_V_VV(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_PP, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_V_VI
#define INTERP_WASM_SIMD_INTRINSIC_V_VI(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_PP, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_V_VVV
#define INTERP_WASM_SIMD_INTRINSIC_V_VVV(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_PPP, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

static PackedSimdIntrinsicInfo packedsimd_intrinsic_infos[] = {
#include "interp-simd-intrins.def"
};
#undef INTERP_WASM_SIMD_INTRINSIC_V_P
#undef INTERP_WASM_SIMD_INTRINSIC_V_V
#undef INTERP_WASM_SIMD_INTRINSIC_I_V
#undef INTERP_WASM_SIMD_INTRINSIC_V_VV
#undef INTERP_WASM_SIMD_INTRINSIC_V_VI
#undef INTERP_WASM_SIMD_INTRINSIC_V_VVV

static PackedSimdIntrinsicInfo *
lookup_packedsimd_intrinsic (const char *name, MonoType *arg1)
{
	MonoClass *vector_klass = mono_class_from_mono_type_internal (arg1);
	if (!m_class_is_simd_type (vector_klass))
		return FALSE;

	MonoType *arg_type = mono_class_get_context (vector_klass)->class_inst->type_argv [0];
	if (!mono_type_is_primitive (arg_type))
		return FALSE;

	int arg_type_enum = arg_type->type;
	if (arg_type_enum == MONO_TYPE_BOOLEAN)
		return FALSE;

	// FIXME: Sort the info table by (name, argtype) and then do a binary search
	for (int i = 0; i < sizeof (packedsimd_intrinsic_infos); i++) {
		PackedSimdIntrinsicInfo *info = &packedsimd_intrinsic_infos[i];
		if (strcmp (name, info->name))
			continue;
		if (!packedsimd_type_matches (arg_type_enum, info->arg_type)) {
			// g_print ("%s arg mismatch: %d != %d\n", name, arg_type_enum, info->arg_type);
			continue;
		}
		return info;
	}

	return NULL;
}

#endif

static gboolean
emit_sri_packedsimd (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature)
{
	int id = lookup_intrins (sri_packedsimd_methods, sizeof (sri_packedsimd_methods), cmethod);
	// We don't early-out for an unrecognized method, we will generate an NIY later

	MonoClass *vector_klass = mono_class_from_mono_type_internal (csignature->ret);
	MonoTypeEnum atype;
	int vector_size = -1, arg_size, scalar_arg;

	// NOTE: Linker substitutions (used in AOT) will prevent this from running.
	if ((id == SN_get_IsSupported) || (id == SN_get_IsHardwareAccelerated)) {
#if HOST_BROWSER
		interp_add_ins (td, MINT_LDC_I4_1);
#else
		interp_add_ins (td, MINT_LDC_I4_0);
#endif
		goto opcode_added;
	}

	if (!get_common_simd_info (vector_klass, csignature, &atype, &vector_size, &arg_size, &scalar_arg))
		return FALSE;

#if HOST_BROWSER
	gint16 simd_opcode = -1;
	gint16 simd_intrins = -1;

	PackedSimdIntrinsicInfo *info = lookup_packedsimd_intrinsic (cmethod->name, csignature->params[0]);

	if (info && info->interp_opcode && info->simd_intrins) {
		simd_opcode = info->interp_opcode;
		simd_intrins = info->simd_intrins;
		// g_print ("%s %d -> %s %d %s\n", info->name, info->arg_type, mono_interp_opname (simd_opcode), simd_intrins, info->intrinsic_name);
	} else {
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

	interp_add_ins (td, simd_opcode);
	td->last_ins->data [0] = simd_intrins;
#else // HOST_BROWSER
	return FALSE;
#endif // HOST_BROWSER

opcode_added:
	emit_common_simd_epilogue (td, vector_klass, csignature, vector_size);
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
	} else if (!strcmp (class_ns, "System.Numerics")) {
		if (!strcmp (class_name, "Vector`1"))
			return emit_sn_vector_t (td, cmethod, csignature);
	} else if (!strcmp (class_ns, "System.Runtime.Intrinsics.Wasm")) {
		if (!strcmp (class_name, "PackedSimd"))
			return emit_sri_packedsimd (td, cmethod, csignature);
	}
	return FALSE;
}
