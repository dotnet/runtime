/*
 * SIMD Intrinsics support for interpreter
 */

#include "config.h"
#include "interp-simd.h"
#include <glib.h>
#include <mono/utils/bsearch.h>
#include <mono/metadata/class-internals.h>

// We use the same approach as jit/aot for identifying simd methods.
// FIXME Consider sharing the code

#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define SIMD_METHOD(name) char MSGSTRFIELD(__LINE__) [sizeof (#name)];
#define SIMD_METHOD2(str,name) char MSGSTRFIELD(__LINE__) [sizeof (str)];
#include "simd-methods.def"
#undef SIMD_METHOD
#undef SIMD_METHOD2
} method_names = {
#define SIMD_METHOD(name) #name,
#define SIMD_METHOD2(str,name) str,
#include "simd-methods.def"
#undef SIMD_METHOD
#undef SIMD_METHOD2
};

enum {
#define SIMD_METHOD(name) SN_ ## name = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#define SIMD_METHOD2(str,name) SN_ ## name = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
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
	SN_EqualsAny,
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
	SN_EqualsFloatingPoint,
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
	SN_ctor,
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
			} else if (atype == MONO_TYPE_R4) {
				interp_add_ins (td, MINT_SIMD_V128_LDC);
				float *data = (float*)&td->last_ins->data [0];
				for (int i = 0; i < vector_size / arg_size; i++)
					data [i] = 1.0f;
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
		case SN_EqualsFloatingPoint:
			*simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_R4)
				*simd_intrins = INTERP_SIMD_INTRINSIC_V128_R4_FLOAT_EQUALITY;
			else if (atype == MONO_TYPE_R8)
				*simd_intrins = INTERP_SIMD_INTRINSIC_V128_R8_FLOAT_EQUALITY;
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
emit_common_simd_epilogue (TransformData *td, MonoClass *vector_klass, MonoMethodSignature *csignature, int vector_size, gboolean allow_void)
{
	td->sp -= csignature->param_count;
	for (int i = 0; i < csignature->param_count; i++)
		td->last_ins->sregs [i] = td->sp [i].var;

	int ret_mt = mono_mint_type (csignature->ret);
	if (csignature->ret->type == MONO_TYPE_VOID) {
		g_assert (allow_void);
		interp_ins_set_dummy_dreg (td->last_ins, td);
	} else if (ret_mt == MINT_TYPE_VT) {
		// For these intrinsics, if we return a VT then it is a V128
		push_type_vt (td, vector_klass, vector_size);
		interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
	} else {
		push_simple_type (td, stack_type [ret_mt]);
		interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
	}
	td->ip += 5;
}

static void
emit_vector_create (TransformData *td, MonoMethodSignature *csignature, MonoClass *vector_klass, int vector_size)
{
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
		call_args [i] = td->sp [i].var;
	call_args [num_args] = -1;
	init_last_ins_call (td);
	td->last_ins->info.call_info->call_args = call_args;
	if (!td->optimized)
		td->last_ins->info.call_info->call_offset = get_tos_offset (td);
	push_type_vt (td, vector_klass, vector_size);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
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
				emit_vector_create (td, csignature, vector_klass, vector_size);
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
		case SN_EqualsAny:
			simd_opcode = MINT_SIMD_INTRINS_P_PP;
			if (atype == MONO_TYPE_I1 || atype == MONO_TYPE_U1) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I1_EQUALS_ANY;
			else if (atype == MONO_TYPE_I2 || atype == MONO_TYPE_U2) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I2_EQUALS_ANY;
			else if (atype == MONO_TYPE_I4 || atype == MONO_TYPE_U4) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I4_EQUALS_ANY;
			else if (atype == MONO_TYPE_I8 || atype == MONO_TYPE_U8) simd_intrins = INTERP_SIMD_INTRINSIC_V128_I8_EQUALS_ANY;
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
	emit_common_simd_epilogue (td, vector_klass, csignature, vector_size, FALSE);
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
	emit_common_simd_epilogue (td, vector_klass, csignature, vector_size, FALSE);
	return TRUE;
}

static gboolean
emit_sn_vector_t (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature, gboolean newobj)
{
	int id = lookup_intrins (sn_vector_t_methods, sizeof (sn_vector_t_methods), cmethod);
	if (id == -1)
		return FALSE;

	gint16 simd_opcode = -1;
	gint16 simd_intrins = -1;

	// First argument is always vector
	MonoClass *vector_klass = cmethod->klass;
	if (!m_class_is_simd_type (vector_klass))
		return FALSE;

	MonoTypeEnum atype;
	int vector_size, arg_size, scalar_arg;
	if (!get_common_simd_info (vector_klass, csignature, &atype, &vector_size, &arg_size, &scalar_arg))
		return FALSE;

	if (emit_common_simd_operations (td, id, atype, vector_size, arg_size, scalar_arg, &simd_opcode, &simd_intrins)) {
		goto opcode_added;
	} else if (id == SN_ctor) {
		if (csignature->param_count == vector_size / arg_size && atype == csignature->params [0]->type) {
			emit_vector_create (td, csignature, vector_klass, vector_size);
			if (!newobj) {
				// If the ctor is called explicitly, then we need to store to the passed `this`
				interp_emit_stobj (td, vector_klass, FALSE);
				td->ip += 5;
			}
			return TRUE;
		}
	}

	if (simd_opcode == -1 || simd_intrins == -1)
		return FALSE;

	interp_add_ins (td, simd_opcode);
	td->last_ins->data [0] = simd_intrins;

opcode_added:
	emit_common_simd_epilogue (td, vector_klass, csignature, vector_size, FALSE);
	return TRUE;
}

static gboolean
emit_sn_vector4 (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature, gboolean newobj)
{
	int id = lookup_intrins (sn_vector_t_methods, sizeof (sn_vector_t_methods), cmethod);
	if (id == -1)
		return FALSE;

	gint16 simd_opcode = -1;
	gint16 simd_intrins = -1;

	// First argument is always vector
	MonoClass *vector_klass = cmethod->klass;

	MonoTypeEnum atype = MONO_TYPE_R4;
	int vector_size = SIZEOF_V128;
	int arg_size = sizeof (float);
	int scalar_arg = -1;
	for (int i = 0; i < csignature->param_count; i++) {
		if (csignature->params [i]->type != MONO_TYPE_GENERICINST)
			scalar_arg = i;
	}

	if (emit_common_simd_operations (td, id, atype, vector_size, arg_size, scalar_arg, &simd_opcode, &simd_intrins)) {
		goto opcode_added;
	} else if (id == SN_ctor) {
		if (csignature->param_count == vector_size / arg_size && atype == csignature->params [0]->type) {
			emit_vector_create (td, csignature, vector_klass, vector_size);
			if (!newobj) {
				// If the ctor is called explicitly, then we need to store to the passed `this`
				interp_emit_stobj (td, vector_klass, FALSE);
				td->ip += 5;
			}
			return TRUE;
		}
	}

	if (simd_opcode == -1 || simd_intrins == -1)
		return FALSE;

	interp_add_ins (td, simd_opcode);
	td->last_ins->data [0] = simd_intrins;

opcode_added:
	emit_common_simd_epilogue (td, vector_klass, csignature, vector_size, FALSE);
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

#undef INTERP_WASM_SIMD_INTRINSIC_V_C1
#define INTERP_WASM_SIMD_INTRINSIC_V_C1(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_P, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_I_V
#define INTERP_WASM_SIMD_INTRINSIC_I_V(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_P, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_V_VV
#define INTERP_WASM_SIMD_INTRINSIC_V_VV(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_PP, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_V_C2
#define INTERP_WASM_SIMD_INTRINSIC_V_C2(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_PP, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_V_VI
#define INTERP_WASM_SIMD_INTRINSIC_V_VI(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_PP, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_V_VVV
#define INTERP_WASM_SIMD_INTRINSIC_V_VVV(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_PPP, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

#undef INTERP_WASM_SIMD_INTRINSIC_V_C3
#define INTERP_WASM_SIMD_INTRINSIC_V_C3(name, arg1, c_intrinsic, wasm_opcode) \
	INTRINS_COMMON(name, arg1, c_intrinsic, MINT_SIMD_INTRINS_P_PPP, INTERP_SIMD_INTRINSIC_ ## name ## arg1)

static PackedSimdIntrinsicInfo unsorted_packedsimd_intrinsic_infos[] = {
#include "interp-simd-intrins.def"
};
#undef INTERP_WASM_SIMD_INTRINSIC_V_P
#undef INTERP_WASM_SIMD_INTRINSIC_V_V
#undef INTERP_WASM_SIMD_INTRINSIC_V_C1
#undef INTERP_WASM_SIMD_INTRINSIC_I_V
#undef INTERP_WASM_SIMD_INTRINSIC_V_VV
#undef INTERP_WASM_SIMD_INTRINSIC_V_VI
#undef INTERP_WASM_SIMD_INTRINSIC_V_C2
#undef INTERP_WASM_SIMD_INTRINSIC_V_VVV
#undef INTERP_WASM_SIMD_INTRINSIC_V_C3

static PackedSimdIntrinsicInfo *sorted_packedsimd_intrinsic_infos;

static int
compare_packedsimd_intrinsic_info (const void *_lhs, const void *_rhs)
{
	g_assert (_lhs);
	g_assert (_rhs);
	const PackedSimdIntrinsicInfo *lhs = _lhs, *rhs = _rhs;
	return strcmp (lhs->name, rhs->name);
}

static PackedSimdIntrinsicInfo *
lookup_packedsimd_intrinsic (const char *name, MonoType *arg1)
{
	MonoClass *vector_klass = mono_class_from_mono_type_internal (arg1);
	MonoType *arg_type = NULL;

	if (m_class_is_simd_type (vector_klass)) {
		arg_type = mono_class_get_context (vector_klass)->class_inst->type_argv [0];
	} else if (arg1->type == MONO_TYPE_PTR) {
		arg_type = arg1->data.type;
	} else {
		// g_printf ("%s arg1 type was not pointer or simd type: %s\n", name, m_class_get_name (vector_klass));
		return FALSE;
	}

	if (!mono_type_is_primitive (arg_type)) {
		// g_printf ("%s arg1 inner type was not primitive\n", name);
		return FALSE;
	}

	int arg_type_enum = arg_type->type,
		search_begin_index,
		num_intrinsics = sizeof(unsorted_packedsimd_intrinsic_infos) / sizeof(PackedSimdIntrinsicInfo);
	if (arg_type_enum == MONO_TYPE_BOOLEAN)
		return FALSE;

	PackedSimdIntrinsicInfo *result = NULL, *search_begin;
	PackedSimdIntrinsicInfo search_key = { name, name };

	// Ensure we have a sorted version of the intrinsics table
	if (!sorted_packedsimd_intrinsic_infos) {
		int buf_size = sizeof(unsorted_packedsimd_intrinsic_infos);
		PackedSimdIntrinsicInfo *temp_sorted = g_malloc0 (buf_size);
		memcpy (temp_sorted, unsorted_packedsimd_intrinsic_infos, buf_size);
		mono_qsort (temp_sorted, num_intrinsics, sizeof(PackedSimdIntrinsicInfo), compare_packedsimd_intrinsic_info);
		mono_atomic_cas_ptr ((gpointer*)&sorted_packedsimd_intrinsic_infos, (gpointer)temp_sorted, NULL);
		if (sorted_packedsimd_intrinsic_infos != temp_sorted)
			g_free (temp_sorted);
	}

	// Binary search by name to find a suitable starting location for our search
	search_begin = (PackedSimdIntrinsicInfo*)mono_binary_search (
		&search_key, sorted_packedsimd_intrinsic_infos,
		num_intrinsics, sizeof(PackedSimdIntrinsicInfo),
		compare_packedsimd_intrinsic_info
	);
	if (!search_begin) {
		// g_printf ("No matching PackedSimd intrinsics for name %s\n", name);
		return FALSE;
	}

	search_begin_index = search_begin - sorted_packedsimd_intrinsic_infos;

	// Search upwards and downwards through the table simultaneously from our starting location,
	//  looking for an intrinsic with a matching name that also has a compatible argument type
	// NOTE: If there are two suitable matches because you got the table wrong, this is nondeterministic
	for (int low = search_begin_index, high = search_begin_index;
		(low >= 0) || (high < num_intrinsics);
		--low, ++high) {
		PackedSimdIntrinsicInfo *low_info = (low >= 0) ? &sorted_packedsimd_intrinsic_infos[low] : NULL,
			*high_info = (high < num_intrinsics) ? &sorted_packedsimd_intrinsic_infos[high] : NULL;
		// As long as either the low or high offset are within range and have a name match, we keep going
		gboolean low_name_matches = low_info && !strcmp (name, low_info->name),
			high_name_matches = high_info && !strcmp (name, high_info->name);
		if (!low_name_matches && !high_name_matches)
			break;

		// Now see whether we have a matching type and name at either offset
		if (low_name_matches && packedsimd_type_matches (arg_type_enum, low_info->arg_type)) {
			result = low_info;
			break;
		}
		if (high_name_matches && packedsimd_type_matches (arg_type_enum, high_info->arg_type)) {
			result = high_info;
			break;
		}
	}

	/*
	if (!result)
		g_printf ("No matching PackedSimd intrinsic for %s[%s]\n", name, m_class_get_name (mono_class_from_mono_type_internal (arg_type)));
	*/
	return result;
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

	get_common_simd_info (vector_klass, csignature, &atype, &vector_size, &arg_size, &scalar_arg);

#if HOST_BROWSER
	gint16 simd_opcode = -1;
	gint16 simd_intrins = -1;

	PackedSimdIntrinsicInfo *info = lookup_packedsimd_intrinsic (cmethod->name, csignature->params[0]);

	if (info && info->interp_opcode && info->simd_intrins) {
		simd_opcode = info->interp_opcode;
		simd_intrins = info->simd_intrins;
		// g_print ("%s %d -> %s %d %s\n", info->name, info->arg_type, mono_interp_opname (simd_opcode), simd_intrins, info->intrinsic_name);
	} else {
		g_warning ("MONO interpreter: Unimplemented method: System.Runtime.Intrinsics.Wasm.PackedSimd.%s\n", cmethod->name);

		// If we're missing a packedsimd method but the packedsimd method was AOT'd, we can
		//  just let the interpreter generate a native call to the AOT method instead of
		//  generating an NIY that will halt execution
		// FIXME: Should we remove this now that the interpreter supports all of the methods?
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
	emit_common_simd_epilogue (td, vector_klass, csignature, vector_size, TRUE);
	return TRUE;
}

static gboolean
interp_emit_simd_intrinsics (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature, gboolean newobj)
{
	const char *class_name;
	const char *class_ns;
	MonoImage *image = m_class_get_image (cmethod->klass);

	if (image != mono_get_corlib ())
		return FALSE;

	if (!interp_simd_enabled)
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
			return emit_sn_vector_t (td, cmethod, csignature, newobj);
		else if (!strcmp (class_name, "Vector4"))
			return emit_sn_vector4 (td, cmethod, csignature, newobj);
	} else if (!strcmp (class_ns, "System.Runtime.Intrinsics.Wasm")) {
		if (!strcmp (class_name, "PackedSimd"))
			return emit_sri_packedsimd (td, cmethod, csignature);
	}
	return FALSE;
}
