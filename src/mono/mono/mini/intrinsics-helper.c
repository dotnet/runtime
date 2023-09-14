#include <config.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/icall-decl.h>
#include "mini.h"
#include "mini-runtime.h"
#include "ir-emit.h"
#include "llvm-intrinsics-types.h"
#ifdef ENABLE_LLVM
#include "mini-llvm.h"
#include "mini-llvm-cpp.h"
#endif
#include "mono/utils/bsearch.h"
#include <mono/metadata/abi-details.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/utils/mono-hwcap.h>
#include "intrinsics-helper.h"
#include <../../../coreclr/jit/namedinitrinsiclist.h>

gboolean
is_SIMD_feature_supported(MonoCompile *cfg, MonoCPUFeatures feature) 
{
	return mini_get_cpu_features (cfg) & feature;
}

gboolean
is_elementwise_ctor (MonoMethodSignature *fsig, MonoType *etype)
{
	if (fsig->param_count < 1)
		return FALSE;
	for (int i = 0; i < fsig->param_count; ++i)
		if (!mono_metadata_type_equal (etype, fsig->params [i]))
			return FALSE;
	return TRUE;
}

gboolean
is_elementwise_create_overload (MonoMethodSignature *fsig, MonoType *ret_type)
{
	uint16_t param_count = fsig->param_count;
	if (param_count < 1) return FALSE;
	MonoType *type = fsig->params [0];
	if (!MONO_TYPE_IS_VECTOR_PRIMITIVE (type)) return FALSE;
	if (!mono_metadata_type_equal (ret_type, type)) return FALSE;
	for (uint16_t i = 1; i < param_count; ++i)
		if (!mono_metadata_type_equal (type, fsig->params [i])) return FALSE;
	return TRUE;
}

gboolean
is_create_from_half_vectors_overload (MonoMethodSignature *fsig)
{
	if (fsig->param_count != 2) return FALSE;
	if (!is_intrinsics_vector_type (fsig->params [0])) return FALSE;
	return mono_metadata_type_equal (fsig->params [0], fsig->params [1]);
}

gboolean
is_element_type_primitive (MonoType *vector_type)
{
	if (vector_type->type == MONO_TYPE_GENERICINST) {
		MonoType *element_type = get_vector_t_elem_type (vector_type);
		return MONO_TYPE_IS_VECTOR_PRIMITIVE (element_type);
	} else {
		MonoClass *klass = mono_class_from_mono_type_internal (vector_type);
		g_assert (
			!strcmp (m_class_get_name (klass), "Plane") ||
			!strcmp (m_class_get_name (klass), "Quaternion") ||
			!strcmp (m_class_get_name (klass), "Vector2") ||
			!strcmp (m_class_get_name (klass), "Vector3") ||
			!strcmp (m_class_get_name (klass), "Vector4"));
		return TRUE;
	}
}

gboolean
is_intrinsics_vector_type (MonoType *vector_type)
{
	if (vector_type->type != MONO_TYPE_GENERICINST) return FALSE;
	MonoClass *klass = mono_class_from_mono_type_internal (vector_type);
	const char *name = m_class_get_name (klass);
	return !strcmp (name, "Vector64`1") || !strcmp (name, "Vector128`1") || !strcmp (name, "Vector256`1") || !strcmp (name, "Vector512`1");
}

MonoType*
get_vector_t_elem_type (MonoType *vector_type)
{
	MonoClass *klass;
	MonoType *etype;

	g_assert (vector_type->type == MONO_TYPE_GENERICINST);
	klass = mono_class_from_mono_type_internal (vector_type);
	g_assert (
		!strcmp (m_class_get_name (klass), "Vector`1") ||
		!strcmp (m_class_get_name (klass), "Vector64`1") ||
		!strcmp (m_class_get_name (klass), "Vector128`1") ||
		!strcmp (m_class_get_name (klass), "Vector256`1") ||
		!strcmp (m_class_get_name (klass), "Vector512`1"));
	etype = mono_class_get_context (klass)->class_inst->type_argv [0];
	return etype;
}

gboolean
type_is_unsigned (MonoType *type)
{
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	MonoType *etype = mono_class_get_context (klass)->class_inst->type_argv [0];
	return type_enum_is_unsigned (etype->type);
}

gboolean
type_enum_is_unsigned (MonoTypeEnum type)
{
	switch (type) {
	case MONO_TYPE_U1:
	case MONO_TYPE_U2:
	case MONO_TYPE_U4:
	case MONO_TYPE_U8:
	case MONO_TYPE_U:
		return TRUE;
	}
	return FALSE;
}

gboolean
type_is_float (MonoType *type)
{
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	MonoType *etype = mono_class_get_context (klass)->class_inst->type_argv [0];
	return type_enum_is_float (etype->type);
}

gboolean
type_enum_is_float (MonoTypeEnum type)
{
	return type == MONO_TYPE_R4 || type == MONO_TYPE_R8;
}

int
type_to_expand_op (MonoTypeEnum type)
{
	switch (type) {
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
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#if TARGET_SIZEOF_VOID_P == 8
		return OP_EXPAND_I8;
#else
		return OP_EXPAND_I4;
#endif
	default:
		g_assert_not_reached ();
	}
}

int
type_to_insert_op (MonoTypeEnum type)
{
	switch (type) {
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
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#if TARGET_SIZEOF_VOID_P == 8
		return OP_INSERT_I8;
#else
		return OP_INSERT_I4;
#endif
	default:
		g_assert_not_reached ();
	}
}

int
type_to_xinsert_op (MonoTypeEnum type)
{
	switch (type) {
	case MONO_TYPE_I1: case MONO_TYPE_U1: return OP_XINSERT_I1;
	case MONO_TYPE_I2: case MONO_TYPE_U2: return OP_XINSERT_I2;
	case MONO_TYPE_I4: case MONO_TYPE_U4: return OP_XINSERT_I4;
	case MONO_TYPE_I8: case MONO_TYPE_U8: return OP_XINSERT_I8;
	case MONO_TYPE_R4: return OP_XINSERT_R4;
	case MONO_TYPE_R8: return OP_XINSERT_R8;
	case MONO_TYPE_I: case MONO_TYPE_U:
#if TARGET_SIZEOF_VOID_P == 8
		return OP_XINSERT_I8;
#else
		return OP_XINSERT_I4;
#endif
	default: g_assert_not_reached ();
	}
}

int
type_to_xextract_op (MonoTypeEnum type)
{
	switch (type) {
	case MONO_TYPE_I1: case MONO_TYPE_U1: return OP_XEXTRACT_I1;
	case MONO_TYPE_I2: case MONO_TYPE_U2: return OP_XEXTRACT_I2;
	case MONO_TYPE_I4: case MONO_TYPE_U4: return OP_XEXTRACT_I4;
	case MONO_TYPE_I8: case MONO_TYPE_U8: return OP_XEXTRACT_I8;
	case MONO_TYPE_R4: return OP_XEXTRACT_R4;
	case MONO_TYPE_R8: return OP_XEXTRACT_R8;
	case MONO_TYPE_I: case MONO_TYPE_U:
#if TARGET_SIZEOF_VOID_P == 8
		return OP_XEXTRACT_I8;
#else
		return OP_XEXTRACT_I4;
#endif
	default: g_assert_not_reached ();
	}
}

int
type_to_extract_op (MonoTypeEnum type)
{
	switch (type) {
	case MONO_TYPE_I1: case MONO_TYPE_U1: return OP_EXTRACT_I1;
	case MONO_TYPE_I2: case MONO_TYPE_U2: return OP_EXTRACT_I2;
	case MONO_TYPE_I4: case MONO_TYPE_U4: return OP_EXTRACT_I4;
	case MONO_TYPE_I8: case MONO_TYPE_U8: return OP_EXTRACT_I8;
	case MONO_TYPE_R4: return OP_EXTRACT_R4;
	case MONO_TYPE_R8: return OP_EXTRACT_R8;
	case MONO_TYPE_I: case MONO_TYPE_U:
#if TARGET_SIZEOF_VOID_P == 8
		return OP_EXTRACT_I8;
#else
		return OP_EXTRACT_I4;
#endif
	default: g_assert_not_reached ();
	}
}

int
type_to_width_log2 (MonoTypeEnum type)
{
	switch (type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		return 0;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		return 1;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return 2;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return 3;
	case MONO_TYPE_R4:
		return 2;
	case MONO_TYPE_R8:
		return 3;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#if TARGET_SIZEOF_VOID_P == 8
		return 3;
#else
		return 2;
#endif
	default:
		g_assert_not_reached ();
	}
}

MonoTypeEnum
get_underlying_type (MonoType* type)
{
	MonoClass* klass = mono_class_from_mono_type_internal (type);
	if (type->type == MONO_TYPE_PTR) // e.g. int* => MONO_TYPE_I4
		return m_class_get_byval_arg (m_class_get_element_class (klass))->type;
	else if (type->type == MONO_TYPE_GENERICINST) // e.g. Vector128<int> => MONO_TYPE_I4
		return mono_class_get_context (klass)->class_inst->type_argv [0]->type;
	else
		return type->type;
}

MonoClass *
create_class_instance (const char* name_space, const char *name, MonoType *param_type)
{
	MonoClass *ivector = mono_class_load_from_name (mono_defaults.corlib, name_space, name);

	MonoType *args [ ] = { param_type };
	MonoGenericContext ctx;
	memset (&ctx, 0, sizeof (ctx));
	ctx.class_inst = mono_metadata_get_generic_inst (1, args);
	ERROR_DECL (error);
	MonoClass *ivector_inst = mono_class_inflate_generic_class_checked (ivector, &ctx, error);
	mono_error_assert_ok (error); /* FIXME don't swallow the error */

	return ivector_inst;
}

MonoInst*
extract_first_element (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum element_type, int sreg)
{
	int extract_op = type_to_extract_op (element_type);
	MonoInst *ins = emit_simd_ins (cfg, klass, extract_op, sreg, -1);
	ins->inst_c0 = 0;
	ins->inst_c1 = element_type;

	return ins;
}

static MonoInst*
handle_mul_div_by_scalar (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum arg_type, int scalar_reg, int vector_reg, int sub_op)
{
	MonoInst* ins;

	if (COMPILE_LLVM (cfg)) {
		ins = emit_simd_ins (cfg, klass, OP_CREATE_SCALAR_UNSAFE, scalar_reg, -1);
		ins->inst_c1 = arg_type;
		ins = emit_simd_ins (cfg, klass, OP_XBINOP_BYSCALAR, vector_reg, ins->dreg);
		ins->inst_c0 = sub_op;
	} else {
		ins = emit_simd_ins (cfg, klass, type_to_expand_op (arg_type), scalar_reg, -1);
		ins->inst_c1 = arg_type;
		ins = emit_simd_ins (cfg, klass, OP_XBINOP, vector_reg, ins->dreg);
		ins->inst_c0 = sub_op;
		ins->inst_c1 = arg_type;
	}

	return ins;
}

MonoInst*
emit_simd_ins_for_binary_op (MonoCompile *cfg, MonoClass *klass, MonoMethodSignature *fsig, MonoInst **args, MonoTypeEnum arg_type, int id)
{
	int instc0 = -1;
	int op = OP_XBINOP;

	if (id == NI_Vector128_BitwiseAnd || id == NI_Vector128_BitwiseOr || id == NI_Vector128_Xor ||
		id == NI_Vector128_op_BitwiseAnd || id == NI_Vector128_op_BitwiseOr || id == NI_Vector128_op_ExclusiveOr) {
		op = OP_XBINOP_FORCEINT;
	
		switch (id) {
		case NI_Vector128_BitwiseAnd:
		case NI_Vector128_op_BitwiseAnd:
			instc0 = XBINOP_FORCEINT_AND;
			break;
		case NI_Vector128_BitwiseOr:
		case NI_Vector128_op_BitwiseOr:
			instc0 = XBINOP_FORCEINT_OR;
			break;
		case NI_Vector128_op_ExclusiveOr:
		case NI_Vector128_Xor:
			instc0 = XBINOP_FORCEINT_XOR;
			break;
		}
	} else {
		if (type_enum_is_float (arg_type)) {
			switch (id) {
			case NI_Vector128_Add:
			case NI_Vector128_op_Addition:
				instc0 = OP_FADD;
				break;
			case NI_Vector128_Divide:
			case NI_Vector128_op_Division: {
				const char *class_name = m_class_get_name (klass);
				if (!strcmp("Vector4", class_name) && fsig->params [1]->type == MONO_TYPE_R4) {
					// Handles  Vector4 / scalar
					return handle_mul_div_by_scalar (cfg, klass, MONO_TYPE_R4, args [1]->dreg, args [0]->dreg, OP_FDIV);	
				}
				if (strcmp ("Vector2", class_name) && strcmp ("Vector4", class_name) && strcmp ("Quaternion", class_name) && strcmp ("Plane", class_name)) {
					if ((fsig->params [0]->type == MONO_TYPE_GENERICINST) && (fsig->params [1]->type != MONO_TYPE_GENERICINST))
						return handle_mul_div_by_scalar (cfg, klass, arg_type, args [1]->dreg, args [0]->dreg, OP_FDIV);
					else if ((fsig->params [0]->type == MONO_TYPE_GENERICINST) && (fsig->params [1]->type == MONO_TYPE_GENERICINST)) {
						instc0 = OP_FDIV;
						break;
					} else {
						return NULL;
					}
				}
				instc0 = OP_FDIV;
				break;
			}
			case NI_Vector128_Max:
				instc0 = OP_FMAX;
				break;
			case NI_Vector128_Min:
				instc0 = OP_FMIN;
				break;
			case NI_Vector128_Multiply:
			case NI_Vector128_op_Multiply: {
				const char *class_name = m_class_get_name (klass);
				if (!strcmp("Vector4", class_name)) {
					// Handles scalar * Vector4 and Vector4 * scalar
					if (fsig->params [0]->type == MONO_TYPE_R4) {
						return handle_mul_div_by_scalar (cfg, klass, MONO_TYPE_R4, args [0]->dreg, args [1]->dreg, OP_FMUL);
					} else if (fsig->params [1]->type == MONO_TYPE_R4) {
						return handle_mul_div_by_scalar (cfg, klass, MONO_TYPE_R4, args [1]->dreg, args [0]->dreg, OP_FMUL);
					}
				}
				if (strcmp ("Vector2", class_name) && strcmp ("Vector4", class_name) && strcmp ("Quaternion", class_name) && strcmp ("Plane", class_name)) {
					if (fsig->params [1]->type != MONO_TYPE_GENERICINST)
						return handle_mul_div_by_scalar (cfg, klass, arg_type, args [1]->dreg, args [0]->dreg, OP_FMUL);
					else if (fsig->params [0]->type != MONO_TYPE_GENERICINST)
						return handle_mul_div_by_scalar (cfg, klass, arg_type, args [0]->dreg, args [1]->dreg, OP_FMUL);
					else if ((fsig->params [0]->type == MONO_TYPE_GENERICINST) && (fsig->params [1]->type == MONO_TYPE_GENERICINST)) {
						instc0 = OP_FMUL;
						break;
					} else {
						return NULL;
					}
				}
				instc0 = OP_FMUL;
				break;
			}
			case NI_Vector128_Subtract:
			case NI_Vector128_op_Subtraction:
				instc0 = OP_FSUB;
				break;
			default:
				g_assert_not_reached ();
			}
		} else {
			switch (id) {
			case NI_Vector128_Add:
			case NI_Vector128_op_Addition:
				instc0 = OP_IADD;
				break;
			case NI_Vector128_Divide:
			case NI_Vector128_op_Division:
				return NULL;
			case NI_Vector128_Max:
				instc0 = type_enum_is_unsigned (arg_type) ? OP_IMAX_UN : OP_IMAX;
#ifdef TARGET_AMD64
				if (!COMPILE_LLVM (cfg) && instc0 == OP_IMAX_UN)
					return NULL;
#endif
				break;
			case NI_Vector128_Min:
				instc0 = type_enum_is_unsigned (arg_type) ? OP_IMIN_UN : OP_IMIN;
#ifdef TARGET_AMD64
				if (!COMPILE_LLVM (cfg) && instc0 == OP_IMIN_UN)
					return NULL;
#endif
				break;
			case NI_Vector128_Multiply:
			case NI_Vector128_op_Multiply: {
#ifdef TARGET_ARM64
				if (!COMPILE_LLVM (cfg) && (arg_type == MONO_TYPE_I8 || arg_type == MONO_TYPE_U8 || arg_type == MONO_TYPE_I || arg_type == MONO_TYPE_U))
					return NULL;
#endif
#ifdef TARGET_AMD64
				if (!COMPILE_LLVM (cfg))
					return NULL;
#endif
				if (fsig->params [1]->type != MONO_TYPE_GENERICINST) 
					return handle_mul_div_by_scalar (cfg, klass, arg_type, args [1]->dreg, args [0]->dreg, OP_IMUL);
				else if (fsig->params [0]->type != MONO_TYPE_GENERICINST)
					return handle_mul_div_by_scalar (cfg, klass, arg_type, args [0]->dreg, args [1]->dreg, OP_IMUL);
				instc0 = OP_IMUL;
				break;
			}
			case NI_Vector128_Subtract:
			case NI_Vector128_op_Subtraction:
				instc0 = OP_ISUB;
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}
	return emit_simd_ins_for_sig (cfg, klass, op, instc0, arg_type, fsig, args);
}

MonoInst*
emit_simd_ins_for_unary_op (MonoCompile *cfg, MonoClass *klass, MonoMethodSignature *fsig, MonoInst **args, MonoTypeEnum arg_type, int id)
{
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
	int op = -1;
	switch (id){
	case NI_Vector128_Negate:
	case NI_Vector128_op_UnaryNegation:
		op = OP_NEGATION;
		break;
	case NI_Vector128_OnesComplement:
	case NI_Vector128_op_OnesComplement:
		op = OP_ONES_COMPLEMENT;
		break;
	default:
		g_assert_not_reached ();
	}
	return emit_simd_ins_for_sig (cfg, klass, op, -1, arg_type, fsig, args);
#elif defined(TARGET_WASM)
	int op = -1;
	switch (id)
	{
	case NI_Vector128_Negate:
		op = OP_NEGATION;
		break;
	case NI_Vector128_OnesComplement:
		op = OP_WASM_ONESCOMPLEMENT;
		break;
	default:
		return NULL;
	}
	return emit_simd_ins_for_sig (cfg, klass, op, -1, arg_type, fsig, args);
#else
	return NULL;
#endif
}

MonoInst*
emit_xcompare (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum etype, MonoInst *arg1, MonoInst *arg2)
{
	MonoInst *ins;
	int opcode = type_enum_is_float (etype) ? OP_XCOMPARE_FP : OP_XCOMPARE;

	ins = emit_simd_ins (cfg, klass, opcode, arg1->dreg, arg2->dreg);
	ins->inst_c0 = CMP_EQ;
	ins->inst_c1 = etype;
	return ins;
}

MonoInst*
emit_xcompare_for_intrinsic (MonoCompile *cfg, MonoClass *klass, int intrinsic_id, MonoTypeEnum etype, MonoInst *arg1, MonoInst *arg2)
{
	MonoInst *ins = emit_xcompare (cfg, klass, etype, arg1, arg2);
	gboolean is_unsigned = type_enum_is_unsigned (etype);

	switch (intrinsic_id) {
	case NI_Vector128_GreaterThan:
	case NI_Vector128_GreaterThanAll:
	case NI_Vector128_GreaterThanAny:
		ins->inst_c0 = is_unsigned ? CMP_GT_UN : CMP_GT;
		break;
	case NI_Vector128_GreaterThanOrEqual:
	case NI_Vector128_GreaterThanOrEqualAll:
	case NI_Vector128_GreaterThanOrEqualAny:
		ins->inst_c0 = is_unsigned ? CMP_GE_UN : CMP_GE;
		break;
	case NI_Vector128_LessThan:
	case NI_Vector128_LessThanAll:
	case NI_Vector128_LessThanAny:
		ins->inst_c0 = is_unsigned ? CMP_LT_UN : CMP_LT;
		break;
	case NI_Vector128_LessThanOrEqual:
	case NI_Vector128_LessThanOrEqualAll:
	case NI_Vector128_LessThanOrEqualAny:
		ins->inst_c0 = is_unsigned ? CMP_LE_UN : CMP_LE;
		break;
	default:
		g_assert_not_reached ();
	}

	return ins;
}

MonoInst*
emit_xequal (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum element_type, MonoInst *arg1, MonoInst *arg2)
{
#ifdef TARGET_ARM64
	if (!COMPILE_LLVM (cfg)) {
		MonoInst* cmp = emit_xcompare (cfg, klass, element_type, arg1, arg2);
		MonoInst* ret = emit_simd_ins (cfg, mono_defaults.boolean_class, OP_XEXTRACT, cmp->dreg, -1);
		ret->inst_c0 = SIMD_EXTR_ARE_ALL_SET;
		ret->inst_c1 = mono_class_value_size (klass, NULL);
		return ret;
	} else if (mono_class_value_size (klass, NULL) == 16) {
		return emit_simd_ins (cfg, klass, OP_XEQUAL_ARM64_V128_FAST, arg1->dreg, arg2->dreg);
	} else {
		return emit_simd_ins (cfg, klass, OP_XEQUAL, arg1->dreg, arg2->dreg);
	}
#else	
	MonoInst *ins = emit_simd_ins (cfg, klass, OP_XEQUAL, arg1->dreg, arg2->dreg);
	if (!COMPILE_LLVM (cfg))
		ins->inst_c1 = mono_class_get_context (klass)->class_inst->type_argv [0]->type;
	return ins;
#endif
}

MonoInst*
emit_not_xequal (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum element_type, MonoInst *arg1, MonoInst *arg2)
{
	MonoInst *ins = emit_xequal (cfg, klass, element_type, arg1, arg2);
	int sreg = ins->dreg;
	int dreg = alloc_ireg (cfg);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, sreg, 0);
	EMIT_NEW_UNALU (cfg, ins, OP_CEQ, dreg, -1);
	return ins;
}

MonoInst*
emit_xzero (MonoCompile *cfg, MonoClass *klass)
{
	return emit_simd_ins (cfg, klass, OP_XZERO, -1, -1);
}

MonoInst*
emit_xones (MonoCompile *cfg, MonoClass *klass)
{
	return emit_simd_ins (cfg, klass, OP_XONES, -1, -1);
}

MonoInst*
emit_xconst_v128 (MonoCompile *cfg, MonoClass *klass, guint8 value[16])
{
	const int size = 16;

	gboolean all_zeros = TRUE;

	for (int i = 0; i < size; ++i) {
		if (value[i] != 0x00) {
			all_zeros = FALSE;
			break;
		}
	}

	if (all_zeros) {
		return emit_xzero (cfg, klass);
	}

	gboolean all_ones = TRUE;

	for (int i = 0; i < size; ++i) {
		if (value[i] != 0xFF) {
			all_ones = FALSE;
			break;
		}
	}

	if (all_ones) {
		return emit_xones (cfg, klass);
	}

	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, OP_XCONST);
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_xreg (cfg);
	ins->inst_p0 = mono_mem_manager_alloc (cfg->mem_manager, size);
	MONO_ADD_INS (cfg->cbb, ins);

	memcpy (ins->inst_p0, &value[0], size);
	return ins;
}

#ifdef TARGET_ARM64
MonoInst*
emit_msb_vector_mask (MonoCompile *cfg, MonoClass *arg_class, MonoTypeEnum arg_type)
{
	guint64 msb_mask_value[2];

	switch (arg_type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			msb_mask_value[0] = 0x8080808080808080;
			msb_mask_value[1] = 0x8080808080808080;
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			msb_mask_value[0] = 0x8000800080008000;
			msb_mask_value[1] = 0x8000800080008000;
			break;
#if TARGET_SIZEOF_VOID_P == 4
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#endif
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_R4:
			msb_mask_value[0] = 0x8000000080000000;
			msb_mask_value[1] = 0x8000000080000000;
			break;
#if TARGET_SIZEOF_VOID_P == 8
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#endif
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_R8:
			msb_mask_value[0] = 0x8000000000000000;
			msb_mask_value[1] = 0x8000000000000000;
			break;
		default:
			g_assert_not_reached ();
	}

	MonoInst* msb_mask_vec = emit_xconst_v128 (cfg, arg_class, (guint8*)msb_mask_value);
	msb_mask_vec->klass = arg_class;
	return msb_mask_vec;
}

MonoInst*
emit_msb_shift_vector_constant (MonoCompile *cfg, MonoClass *arg_class, MonoTypeEnum arg_type)
{
	guint64 msb_shift_value[2];

	// NOTE: On ARM64 ushl shifts a vector left or right depending on the sign of the shift constant
	switch (arg_type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			msb_shift_value[0] = 0x00FFFEFDFCFBFAF9;
			msb_shift_value[1] = 0x00FFFEFDFCFBFAF9;
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			msb_shift_value[0] = 0xFFF4FFF3FFF2FFF1;
			msb_shift_value[1] = 0xFFF8FFF7FFF6FFF5;
			break;
#if TARGET_SIZEOF_VOID_P == 4
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#endif
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_R4:
			msb_shift_value[0] = 0xFFFFFFE2FFFFFFE1;
			msb_shift_value[1] = 0xFFFFFFE4FFFFFFE3;
			break;
#if TARGET_SIZEOF_VOID_P == 8
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#endif
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_R8:
			msb_shift_value[0] = 0xFFFFFFFFFFFFFFC1;
			msb_shift_value[1] = 0xFFFFFFFFFFFFFFC2;
			break;
		default:
			g_assert_not_reached ();
	}

	MonoInst* msb_shift_vec = emit_xconst_v128 (cfg, arg_class, (guint8*)msb_shift_value);
	msb_shift_vec->klass = arg_class;
	return msb_shift_vec;
}
#endif

#if defined(TARGET_AMD64)
const int fast_log2 [] = { -1, -1, 1, -1, 2, -1, -1, -1, 3 };

MonoInst*
emit_sum_vector (MonoCompile *cfg, MonoType *vector_type, MonoTypeEnum element_type, MonoInst *arg)
{
	MonoClass *vector_class = mono_class_from_mono_type_internal (vector_type);

	int instc0 = -1;
	switch (element_type) {
	case MONO_TYPE_R4:
		instc0 = INTRINS_SSE_HADDPS;
		break;
	case MONO_TYPE_R8:
		instc0 = INTRINS_SSE_HADDPD;
		break;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		// byte, sbyte not supported yet
		return NULL;
	case MONO_TYPE_I2: 
	case MONO_TYPE_U2:
		instc0 = INTRINS_SSE_PHADDW;
		break;
#if TARGET_SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		instc0 = INTRINS_SSE_PHADDD;
		break;
#if TARGET_SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I8:
	case MONO_TYPE_U8: {
		// Ssse3 doesn't have support for HorizontalAdd on i64
		MonoInst *lower = emit_simd_ins (cfg, vector_class, OP_XLOWER, arg->dreg, -1);
		MonoInst *upper = emit_simd_ins (cfg, vector_class, OP_XUPPER, arg->dreg, -1);

		// Sum lower and upper i64
		MonoInst *ins = emit_simd_ins (cfg, vector_class, OP_XBINOP, lower->dreg, upper->dreg);
		ins->inst_c0 = OP_IADD;
		ins->inst_c1 = element_type;

		return extract_first_element (cfg, vector_class, element_type, ins->dreg);
	}
	default: {
		return NULL;
	}
	}	
	
	// Check if necessary SIMD intrinsics are supported on the current machine
	MonoCPUFeatures feature = type_enum_is_float (element_type) ? MONO_CPU_X86_SSE3 : MONO_CPU_X86_SSSE3;
	if (!is_SIMD_feature_supported (cfg, feature))
		return NULL;	

	int vector_size = mono_class_value_size (vector_class, NULL);
	MonoType *etype = mono_class_get_context (vector_class)->class_inst->type_argv [0];
	int elem_size = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
	int num_elems = vector_size / elem_size;
	int num_rounds = fast_log2[num_elems];

	MonoInst *tmp = emit_xzero (cfg, vector_class);
	MonoInst *ins = arg;
	// HorizontalAdds over vector log2(num_elems) times
	for (int i = 0; i < num_rounds; ++i) {
		ins = emit_simd_ins (cfg, vector_class, OP_XOP_X_X_X, ins->dreg, tmp->dreg);
		ins->inst_c0 = instc0;
		ins->inst_c1 = element_type;
	}

	return extract_first_element (cfg, vector_class, element_type, ins->dreg);
}
#elif defined(TARGET_ARM64)
MonoInst*
emit_sum_vector (MonoCompile *cfg, MonoType *vector_type, MonoTypeEnum element_type, MonoInst *arg)
{
	MonoClass *vector_class = mono_class_from_mono_type_internal (vector_type);
	int vector_size = mono_class_value_size (vector_class, NULL);
	int element_size;

	// FIXME: Support Vector3
	guint32 nelems;
	mini_get_simd_type_info (vector_class, &nelems);
	element_size = vector_size / nelems;
	gboolean has_single_element = vector_size == element_size;

	// If there's just one element we need to extract it instead of summing the whole array
	if (has_single_element) {
		MonoInst *ins = emit_simd_ins (cfg, vector_class, type_to_extract_op (element_type), arg->dreg, -1);
		ins->inst_c0 = 0;
		ins->inst_c1 = element_type;
		return ins;
	}

	MonoInst *sum = emit_simd_ins (cfg, vector_class, OP_ARM64_XADDV, arg->dreg, -1);
	if (type_enum_is_float (element_type)) {
		sum->inst_c0 = INTRINS_AARCH64_ADV_SIMD_FADDV;
		sum->inst_c1 = element_type;
	} else {
		sum->inst_c0 = type_enum_is_unsigned (element_type) ? INTRINS_AARCH64_ADV_SIMD_UADDV : INTRINS_AARCH64_ADV_SIMD_SADDV;
		sum->inst_c1 = element_type;
	}

	if (COMPILE_LLVM (cfg)) {
		return sum;
	} else {
		MonoInst *ins = emit_simd_ins (cfg, vector_class, type_to_extract_op (element_type), sum->dreg, -1);
		ins->inst_c0 = 0;
		ins->inst_c1 = element_type;
		return ins;
	}
}
#elif defined(TARGET_WASM)
MonoInst*
emit_sum_vector (MonoCompile *cfg, MonoType *vector_type, MonoTypeEnum element_type, MonoInst *arg)
{
	MonoClass *vector_class = mono_class_from_mono_type_internal (vector_type);
	MonoInst* vsum = emit_simd_ins (cfg, vector_class, OP_WASM_SIMD_SUM, arg->dreg, -1);

	return extract_first_element (cfg, vector_class, element_type, vsum->dreg);
}
#endif

MonoInst*
emit_vector_create_elementwise (
	MonoCompile *cfg, MonoMethodSignature *fsig, MonoType *vtype,
	MonoTypeEnum type, MonoInst **args)
{
	int op = type_to_insert_op (type);
	MonoClass *vklass = mono_class_from_mono_type_internal (vtype);
	MonoInst *ins = emit_xzero (cfg, vklass);
	for (int i = 0; i < fsig->param_count; ++i) {
		ins = emit_simd_ins (cfg, vklass, op, ins->dreg, args [i]->dreg);
		ins->inst_c0 = i;
		ins->inst_c1 = type;
	}
	return ins;
}

/* Create and emit a SIMD instruction, dreg is auto-allocated */
MonoInst*
emit_simd_ins (MonoCompile *cfg, MonoClass *klass, int opcode, int sreg1, int sreg2)
{
	const char *spec = INS_INFO (opcode);
	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, opcode);
	if (spec [MONO_INST_DEST] == 'x') {
		ins->dreg = alloc_xreg (cfg);
		ins->type = STACK_VTYPE;
	} else if (spec [MONO_INST_DEST] == 'i') {
		ins->dreg = alloc_ireg (cfg);
		ins->type = STACK_I4;
	} else if (spec [MONO_INST_DEST] == 'l') {
		ins->dreg = alloc_lreg (cfg);
		ins->type = STACK_I8;
	} else if (spec [MONO_INST_DEST] == 'f') {
		ins->dreg = alloc_freg (cfg);
		ins->type = STACK_R8;
	} else if (spec [MONO_INST_DEST] == 'v') {
		ins->dreg = alloc_dreg (cfg, STACK_VTYPE);
		ins->type = STACK_VTYPE;
	}
	ins->sreg1 = sreg1;
	ins->sreg2 = sreg2;
	ins->klass = klass;
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

MonoInst*
emit_simd_ins_for_sig (MonoCompile *cfg, MonoClass *klass, int opcode, int instc0, int instc1, MonoMethodSignature *fsig, MonoInst **args)
{
	g_assert (fsig->param_count <= 3);
	MonoInst* ins = emit_simd_ins (cfg, klass, opcode,
		fsig->param_count > 0 ? args [0]->dreg : -1,
		fsig->param_count > 1 ? args [1]->dreg : -1);
	if (instc0 != -1)
		ins->inst_c0 = instc0;
	if (instc1 != -1)
		ins->inst_c1 = instc1;
	if (fsig->param_count == 3)
		ins->sreg3 = args [2]->dreg;
	return ins;
}
