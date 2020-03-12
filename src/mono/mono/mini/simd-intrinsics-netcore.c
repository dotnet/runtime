/**
 * SIMD Intrinsics support for netcore
 */

#include <config.h>
#include <mono/utils/mono-compiler.h>

#if defined(DISABLE_JIT)

void
mono_simd_intrinsics_init (void)
{
}

#else

/*
 * Only LLVM is supported as a backend.
 */

#include "mini.h"
#include "mini-runtime.h"
#include "ir-emit.h"
#ifdef ENABLE_LLVM
#include "mini-llvm.h"
#endif
#include "mono/utils/bsearch.h"
#include <mono/metadata/abi-details.h>
#include <mono/metadata/reflection-internals.h>

#if defined (MONO_ARCH_SIMD_INTRINSICS) && defined(ENABLE_NETCORE)

#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define METHOD(name) char MSGSTRFIELD(__LINE__) [sizeof (#name)];
#define METHOD2(str,name) char MSGSTRFIELD(__LINE__) [sizeof (str)];
#include "simd-methods-netcore.h"
#undef METHOD
#undef METHOD2
} method_names = {
#define METHOD(name) #name,
#define METHOD2(str,name) str,
#include "simd-methods-netcore.h"
#undef METHOD
#undef METHOD2
};

enum {
#define METHOD(name) SN_ ## name = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#define METHOD2(str,name) SN_ ## name = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "simd-methods-netcore.h"
};
#define method_name(idx) ((const char*)&method_names + (idx))

static int register_size;

typedef struct {
	// One of the SN_ constants
	guint16 id;
	// ins->opcode
	int op;
	// ins->inst_c0
	int instc0;
} SimdIntrinsic;

void
mono_simd_intrinsics_init (void)
{
	register_size = 16;
#if FALSE
	if ((mini_get_cpu_features () & MONO_CPU_X86_AVX) != 0)
		register_size = 32;
#endif
	/* Tell the class init code the size of the System.Numerics.Register type */
	mono_simd_register_size = register_size;
}

MonoInst*
mono_emit_simd_field_load (MonoCompile *cfg, MonoClassField *field, MonoInst *addr)
{
	return NULL;
}

static int
simd_intrinsic_compare_by_name (const void *key, const void *value)
{
	return strcmp ((const char*)key, method_name (*(guint16*)value));
}

static int
simd_intrinsic_info_compare_by_name (const void *key, const void *value)
{
	SimdIntrinsic *info = (SimdIntrinsic*)value;
	return strcmp ((const char*)key, method_name (info->id));
}

static int
lookup_intrins (guint16 *intrinsics, int size, MonoMethod *cmethod)
{
	const guint16 *result = (const guint16 *)mono_binary_search (cmethod->name, intrinsics, size / sizeof (guint16), sizeof (guint16), &simd_intrinsic_compare_by_name);
	
	if (result == NULL)
		return -1;
	else
		return (int)*result;
}

static SimdIntrinsic*
lookup_intrins_info (SimdIntrinsic *intrinsics, int size, MonoMethod *cmethod)
{
#if 0
	for (int i = 0; i < (size / sizeof (SimdIntrinsic)) - 1; ++i) {
		const char *n1 = method_name (intrinsics [i].id);
		const char *n2 = method_name (intrinsics [i + 1].id);
		int len1 = strlen (n1);
		int len2 = strlen (n2);
		for (int j = 0; j < len1 && j < len2; ++j) {
			if (n1 [j] > n2 [j]) {
				printf ("%s %s\n", n1, n2);
				g_assert_not_reached ();
			} else if (n1 [j] < n2 [j]) {
				break;
			}
		}
	}
#endif

	return (SimdIntrinsic *)mono_binary_search (cmethod->name, intrinsics, size / sizeof (SimdIntrinsic), sizeof (SimdIntrinsic), &simd_intrinsic_info_compare_by_name);
}

static int
type_to_expand_op (MonoType *type)
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

/*
 * Return a simd vreg for the simd value represented by SRC.
 * SRC is the 'this' argument to methods.
 * Set INDIRECT to TRUE if the value was loaded from memory.
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

/* Create and emit a SIMD instruction, dreg is auto-allocated */
static MonoInst*
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
	}
	ins->sreg1 = sreg1;
	ins->sreg2 = sreg2;
	ins->klass = klass;
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
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

static gboolean
is_hw_intrinsics_class (MonoClass *klass, const char *name, gboolean *is_64bit)
{
	const char *class_name = m_class_get_name (klass);
	if ((!strcmp (class_name, "X64") || !strcmp (class_name, "Arm64")) && m_class_get_nested_in (klass)) {
		*is_64bit = TRUE;
		return !strcmp (m_class_get_name (m_class_get_nested_in (klass)), name);
	} else {
		*is_64bit = FALSE;
		return !strcmp (class_name, name);
	}
}

static MonoTypeEnum
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

static MonoInst*
emit_xcompare (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum etype, MonoInst *arg1, MonoInst *arg2)
{
	MonoInst *ins;
	gboolean is_fp = etype == MONO_TYPE_R4 || etype == MONO_TYPE_R8;

	ins = emit_simd_ins (cfg, klass, is_fp ? OP_XCOMPARE_FP : OP_XCOMPARE, arg1->dreg, arg2->dreg);
	ins->inst_c0 = CMP_EQ;
	ins->inst_c1 = etype;
	return ins;
}

static MonoType*
get_vector_t_elem_type (MonoType *vector_type)
{
	MonoClass *klass;
	MonoType *etype;

	g_assert (vector_type->type == MONO_TYPE_GENERICINST);
	klass = mono_class_from_mono_type_internal (vector_type);
	g_assert (
		!strcmp (m_class_get_name (klass), "Vector`1") || 
		!strcmp (m_class_get_name (klass), "Vector128`1") || 
		!strcmp (m_class_get_name (klass), "Vector256`1"));
	etype = mono_class_get_context (klass)->class_inst->type_argv [0];
	return etype;
}

static guint16 vector_methods [] = {
	SN_ConvertToDouble,
	SN_ConvertToInt32,
	SN_ConvertToInt64,
	SN_ConvertToSingle,
	SN_ConvertToUInt32,
	SN_ConvertToUInt64,
	SN_Narrow,
	SN_Widen,
	SN_get_IsHardwareAccelerated,
};

static MonoInst*
emit_sys_numerics_vector (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins;
	gboolean supported = FALSE;
	int id;
	MonoType *etype;

	id = lookup_intrins (vector_methods, sizeof (vector_methods), cmethod);
	if (id == -1)
		return NULL;

	//printf ("%s\n", mono_method_full_name (cmethod, 1));

#ifdef MONO_ARCH_SIMD_INTRINSICS
	supported = TRUE;
#endif

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	switch (id) {
	case SN_get_IsHardwareAccelerated:
		EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
		ins->type = STACK_I4;
		return ins;
	case SN_ConvertToInt32:
		etype = get_vector_t_elem_type (fsig->params [0]);
		g_assert (etype->type == MONO_TYPE_R4);
		return emit_simd_ins (cfg, mono_class_from_mono_type_internal (fsig->ret), OP_CVTPS2DQ, args [0]->dreg, -1);
	case SN_ConvertToSingle:
		etype = get_vector_t_elem_type (fsig->params [0]);
		g_assert (etype->type == MONO_TYPE_I4 || etype->type == MONO_TYPE_U4);
		// FIXME:
		if (etype->type == MONO_TYPE_U4)
			return NULL;
		return emit_simd_ins (cfg, mono_class_from_mono_type_internal (fsig->ret), OP_CVTDQ2PS, args [0]->dreg, -1);
	case SN_ConvertToDouble:
	case SN_ConvertToInt64:
	case SN_ConvertToUInt32:
	case SN_ConvertToUInt64:
	case SN_Narrow:
	case SN_Widen:
		// FIXME:
		break;
	default:
		break;
	}

	return NULL;
}

static guint16 vector_t_methods [] = {
	SN_ctor,
	SN_CopyTo,
	SN_Equals,
	SN_GreaterThan,
	SN_GreaterThanOrEqual,
	SN_LessThan,
	SN_LessThanOrEqual,
	SN_Max,
	SN_Min,
	SN_get_AllOnes,
	SN_get_Count,
	SN_get_Item,
	SN_get_Zero,
	SN_op_Addition,
	SN_op_BitwiseAnd,
	SN_op_BitwiseOr,
	SN_op_Division,
	SN_op_Equality,
	SN_op_ExclusiveOr,
	SN_op_Explicit,
	SN_op_Inequality,
	SN_op_Multiply,
	SN_op_Subtraction
};

static MonoInst*
emit_sys_numerics_vector_t (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins;
	MonoType *type, *etype;
	MonoClass *klass;
	int size, len, id;
	gboolean is_unsigned;

	id = lookup_intrins (vector_t_methods, sizeof (vector_t_methods), cmethod);
	if (id == -1)
		return NULL;

	klass = cmethod->klass;
	type = m_class_get_byval_arg (klass);
	etype = mono_class_get_context (klass)->class_inst->type_argv [0];
	size = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
	g_assert (size);
	len = register_size / size;

	if (!MONO_TYPE_IS_PRIMITIVE (etype) || etype->type == MONO_TYPE_CHAR || etype->type == MONO_TYPE_BOOLEAN)
		return NULL;

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	switch (id) {
	case SN_get_Count:
		if (!(fsig->param_count == 0 && fsig->ret->type == MONO_TYPE_I4))
			break;
		EMIT_NEW_ICONST (cfg, ins, len);
		return ins;
	case SN_get_Zero:
		g_assert (fsig->param_count == 0 && mono_metadata_type_equal (fsig->ret, type));
		return emit_simd_ins (cfg, klass, OP_XZERO, -1, -1);
	case SN_get_AllOnes: {
		/* Compare a zero vector with itself */
		ins = emit_simd_ins (cfg, klass, OP_XZERO, -1, -1);
		return emit_xcompare (cfg, klass, etype->type, ins, ins);
	}
	case SN_get_Item: {
		if (!COMPILE_LLVM (cfg))
			return NULL;
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, len);
		MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "IndexOutOfRangeException");
		int opcode = -1;
		int dreg;
		gboolean is64 = FALSE;
		switch (etype->type) {
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			opcode = OP_XEXTRACT_I64;
			is64 = TRUE;
			dreg = alloc_lreg (cfg);
			break;
		case MONO_TYPE_R8:
			opcode = OP_XEXTRACT_R8;
			dreg = alloc_freg (cfg);
			break;
		case MONO_TYPE_R4:
			g_assert (cfg->r4fp);
			opcode = OP_XEXTRACT_R4;
			dreg = alloc_freg (cfg);
			break;
		default:
			opcode = OP_XEXTRACT_I32;
			dreg = alloc_ireg (cfg);
			break;
		}
		MONO_INST_NEW (cfg, ins, opcode);
		ins->dreg = dreg;
		ins->sreg1 = load_simd_vreg (cfg, cmethod, args [0], NULL);
		ins->sreg2 = args [1]->dreg;
		ins->inst_c0 = etype->type;
		mini_type_to_eval_stack_type (cfg, etype, ins);
		MONO_ADD_INS (cfg->cbb, ins);
		return ins;
	}
	case SN_ctor:
		if (fsig->param_count == 1 && mono_metadata_type_equal (fsig->params [0], etype)) {
			int dreg = load_simd_vreg (cfg, cmethod, args [0], NULL);

			int opcode = type_to_expand_op (etype);
			ins = emit_simd_ins (cfg, klass, opcode, args [1]->dreg, -1);
			ins->dreg = dreg;
			return ins;
		}
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
			ldelema_ins = mini_emit_ldelema_1_ins (cfg, mono_class_from_mono_type_internal (etype), array_ins, index_ins, TRUE);
			g_assert (args [0]->opcode == OP_LDADDR);
			var = (MonoInst*)args [0]->inst_p0;
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADX_MEMBASE, var->dreg, ldelema_ins->dreg, 0);
			ins->klass = cmethod->klass;
			return args [0];
		}
		break;
	case SN_CopyTo:
		if ((fsig->param_count == 1 || fsig->param_count == 2) && (fsig->params [0]->type == MONO_TYPE_SZARRAY)) {
			MonoInst *array_ins = args [1];
			MonoInst *index_ins;
			MonoInst *ldelema_ins;
			int val_vreg, end_index_reg;

			val_vreg = load_simd_vreg (cfg, cmethod, args [0], NULL);

			/* CopyTo (T[]) or CopyTo (T[], index) */

			if (fsig->param_count == 2) {
				index_ins = args [2];
			} else {
				EMIT_NEW_ICONST (cfg, index_ins, 0);
			}

			/* CopyTo () does complicated argument checks */
			mini_emit_bounds_check_offset (cfg, array_ins->dreg, MONO_STRUCT_OFFSET (MonoArray, max_length), index_ins->dreg, "ArgumentOutOfRangeException");
			end_index_reg = alloc_ireg (cfg);
			int len_reg = alloc_ireg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP_FLAGS (cfg, OP_LOADI4_MEMBASE, len_reg, array_ins->dreg, MONO_STRUCT_OFFSET (MonoArray, max_length), MONO_INST_INVARIANT_LOAD);
			EMIT_NEW_BIALU (cfg, ins, OP_ISUB, end_index_reg, len_reg, index_ins->dreg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, end_index_reg, len);
			MONO_EMIT_NEW_COND_EXC (cfg, LT, "ArgumentException");

			/* Load the array slice into the simd reg */
			ldelema_ins = mini_emit_ldelema_1_ins (cfg, mono_class_from_mono_type_internal (etype), array_ins, index_ins, FALSE);
			EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STOREX_MEMBASE, ldelema_ins->dreg, 0, val_vreg);
			ins->klass = cmethod->klass;
			return ins;
		}
		break;
	case SN_Equals:
		if (fsig->param_count == 1 && fsig->ret->type == MONO_TYPE_BOOLEAN && mono_metadata_type_equal (fsig->params [0], type)) {
			int sreg1 = load_simd_vreg (cfg, cmethod, args [0], NULL);

			return emit_simd_ins (cfg, klass, OP_XEQUAL, sreg1, args [1]->dreg);
		} else if (fsig->param_count == 2 && mono_metadata_type_equal (fsig->ret, type) && mono_metadata_type_equal (fsig->params [0], type) && mono_metadata_type_equal (fsig->params [1], type)) {
			/* Per element equality */
			return emit_xcompare (cfg, klass, etype->type, args [0], args [1]);
		}
		break;
	case SN_op_Equality:
	case SN_op_Inequality:
		g_assert (fsig->param_count == 2 && fsig->ret->type == MONO_TYPE_BOOLEAN &&
				  mono_metadata_type_equal (fsig->params [0], type) &&
				  mono_metadata_type_equal (fsig->params [1], type));
		ins = emit_simd_ins (cfg, klass, OP_XEQUAL, args [0]->dreg, args [1]->dreg);
		if (id == SN_op_Inequality) {
			int sreg = ins->dreg;
			int dreg = alloc_ireg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, sreg, 0);
			EMIT_NEW_UNALU (cfg, ins, OP_CEQ, dreg, -1);
		}
		return ins;
	case SN_GreaterThan:
	case SN_GreaterThanOrEqual:
	case SN_LessThan:
	case SN_LessThanOrEqual:
		g_assert (fsig->param_count == 2 && mono_metadata_type_equal (fsig->ret, type) && mono_metadata_type_equal (fsig->params [0], type) && mono_metadata_type_equal (fsig->params [1], type));
		is_unsigned = etype->type == MONO_TYPE_U1 || etype->type == MONO_TYPE_U2 || etype->type == MONO_TYPE_U4 || etype->type == MONO_TYPE_U8;
		ins = emit_xcompare (cfg, klass, etype->type, args [0], args [1]);
		switch (id) {
		case SN_GreaterThan:
			ins->inst_c0 = is_unsigned ? CMP_GT_UN : CMP_GT;
			break;
		case SN_GreaterThanOrEqual:
			ins->inst_c0 = is_unsigned ? CMP_GE_UN : CMP_GE;
			break;
		case SN_LessThan:
			ins->inst_c0 = is_unsigned ? CMP_LT_UN : CMP_LT;
			break;
		case SN_LessThanOrEqual:
			ins->inst_c0 = is_unsigned ? CMP_LE_UN : CMP_LE;
			break;
		default:
			g_assert_not_reached ();
		}
		return ins;
	case SN_op_Explicit:
		return emit_simd_ins (cfg, klass, OP_XCAST, args [0]->dreg, -1);
	case SN_op_Addition:
	case SN_op_Subtraction:
	case SN_op_Division:
	case SN_op_Multiply:
	case SN_op_BitwiseAnd:
	case SN_op_BitwiseOr:
	case SN_op_ExclusiveOr:
	case SN_Max:
	case SN_Min:
		if (!(fsig->param_count == 2 && mono_metadata_type_equal (fsig->ret, type) && mono_metadata_type_equal (fsig->params [0], type) && mono_metadata_type_equal (fsig->params [1], type)))
			return NULL;
		ins = emit_simd_ins (cfg, klass, OP_XBINOP, args [0]->dreg, args [1]->dreg);
		ins->inst_c1 = etype->type;

		if (etype->type == MONO_TYPE_R4 || etype->type == MONO_TYPE_R8) {
			switch (id) {
			case SN_op_Addition:
				ins->inst_c0 = OP_FADD;
				break;
			case SN_op_Subtraction:
				ins->inst_c0 = OP_FSUB;
				break;
			case SN_op_Multiply:
				ins->inst_c0 = OP_FMUL;
				break;
			case SN_op_Division:
				ins->inst_c0 = OP_FDIV;
				break;
			case SN_Max:
				ins->inst_c0 = OP_FMAX;
				break;
			case SN_Min:
				ins->inst_c0 = OP_FMIN;
				break;
			default:
				NULLIFY_INS (ins);
				return NULL;
			}
		} else {
			switch (id) {
			case SN_op_Addition:
				ins->inst_c0 = OP_IADD;
				break;
			case SN_op_Subtraction:
				ins->inst_c0 = OP_ISUB;
				break;
				/*
			case SN_op_Division:
				ins->inst_c0 = OP_IDIV;
				break;
			case SN_op_Multiply:
				ins->inst_c0 = OP_IMUL;
				break;
				*/
			case SN_op_BitwiseAnd:
				ins->inst_c0 = OP_IAND;
				break;
			case SN_op_BitwiseOr:
				ins->inst_c0 = OP_IOR;
				break;
			case SN_op_ExclusiveOr:
				ins->inst_c0 = OP_IXOR;
				break;
			case SN_Max:
				ins->inst_c0 = OP_IMAX;
				break;
			case SN_Min:
				ins->inst_c0 = OP_IMIN;
				break;
			default:
				NULLIFY_INS (ins);
				return NULL;
			}
		}
		return ins;
	default:
		break;
	}

	return NULL;
}

#ifdef TARGET_AMD64

static SimdIntrinsic sse_methods [] = {
	{SN_Add, OP_XBINOP, OP_FADD},
	{SN_AddScalar, OP_SSE_ADDSS},
	{SN_And, OP_SSE_AND},
	{SN_AndNot, OP_SSE_ANDN},
	{SN_CompareEqual, OP_XCOMPARE_FP, CMP_EQ},
	{SN_CompareGreaterThan, OP_XCOMPARE_FP,CMP_GT},
	{SN_CompareGreaterThanOrEqual, OP_XCOMPARE_FP, CMP_GE},
	{SN_CompareLessThan, OP_XCOMPARE_FP, CMP_LT},
	{SN_CompareLessThanOrEqual, OP_XCOMPARE_FP, CMP_LE},
	{SN_CompareNotEqual, OP_XCOMPARE_FP, CMP_NE},
	{SN_CompareNotGreaterThan, OP_XCOMPARE_FP, CMP_LE},
	{SN_CompareNotGreaterThanOrEqual, OP_XCOMPARE_FP, CMP_LT},
	{SN_CompareNotLessThan, OP_XCOMPARE_FP, CMP_GE},
	{SN_CompareNotLessThanOrEqual, OP_XCOMPARE_FP, CMP_GT},
	{SN_CompareOrdered, OP_XCOMPARE_FP, CMP_ORD},
	{SN_CompareScalarEqual, OP_SSE_CMPSS, CMP_EQ},
	{SN_CompareScalarGreaterThan, OP_SSE_CMPSS, CMP_GT},
	{SN_CompareScalarGreaterThanOrEqual, OP_SSE_CMPSS, CMP_GE},
	{SN_CompareScalarLessThan, OP_SSE_CMPSS, CMP_LT},
	{SN_CompareScalarLessThanOrEqual, OP_SSE_CMPSS, CMP_LE},
	{SN_CompareScalarNotEqual, OP_SSE_CMPSS, CMP_NE},
	{SN_CompareScalarNotGreaterThan, OP_SSE_CMPSS, CMP_LE},
	{SN_CompareScalarNotGreaterThanOrEqual, OP_SSE_CMPSS, CMP_LT},
	{SN_CompareScalarNotLessThan, OP_SSE_CMPSS, CMP_GE},
	{SN_CompareScalarNotLessThanOrEqual, OP_SSE_CMPSS, CMP_GT},
	{SN_CompareScalarOrdered, OP_SSE_CMPSS, CMP_ORD},
	{SN_CompareScalarOrderedEqual, OP_SSE_COMISS, CMP_EQ},
	{SN_CompareScalarOrderedGreaterThan, OP_SSE_COMISS, CMP_GT},
	{SN_CompareScalarOrderedGreaterThanOrEqual, OP_SSE_COMISS, CMP_GE},
	{SN_CompareScalarOrderedLessThan, OP_SSE_COMISS, CMP_LT},
	{SN_CompareScalarOrderedLessThanOrEqual, OP_SSE_COMISS, CMP_LE},
	{SN_CompareScalarOrderedNotEqual, OP_SSE_COMISS, CMP_NE},
	{SN_CompareScalarUnordered, OP_SSE_CMPSS, CMP_UNORD},
	{SN_CompareScalarUnorderedEqual, OP_SSE_UCOMISS, CMP_EQ},
	{SN_CompareScalarUnorderedGreaterThan, OP_SSE_UCOMISS, CMP_GT},
	{SN_CompareScalarUnorderedGreaterThanOrEqual, OP_SSE_UCOMISS, CMP_GE},
	{SN_CompareScalarUnorderedLessThan, OP_SSE_UCOMISS, CMP_LT},
	{SN_CompareScalarUnorderedLessThanOrEqual, OP_SSE_UCOMISS, CMP_LE},
	{SN_CompareScalarUnorderedNotEqual, OP_SSE_UCOMISS, CMP_NE},
	{SN_CompareUnordered, OP_XCOMPARE_FP, CMP_UNORD},
	{SN_ConvertScalarToVector128Single},
	{SN_ConvertToInt32, OP_XOP_I4_X, SIMD_OP_SSE_CVTSS2SI},
	{SN_ConvertToInt32WithTruncation, OP_XOP_I4_X, SIMD_OP_SSE_CVTTSS2SI},
	{SN_ConvertToInt64, OP_XOP_I8_X, SIMD_OP_SSE_CVTSS2SI64},
	{SN_ConvertToInt64WithTruncation, OP_XOP_I8_X, SIMD_OP_SSE_CVTTSS2SI64},
	{SN_Divide, OP_XBINOP, OP_FDIV},
	{SN_DivideScalar, OP_SSE_DIVSS},
	{SN_LoadAlignedVector128, OP_SSE_LOADU, 16 /* alignment */},
	{SN_LoadHigh, OP_SSE_MOVHPS_LOAD},
	{SN_LoadLow, OP_SSE_MOVLPS_LOAD},
	{SN_LoadScalarVector128, OP_SSE_MOVSS},
	{SN_LoadVector128, OP_SSE_LOADU, 1 /* alignment */},
	{SN_Max, OP_XOP_X_X_X, SIMD_OP_SSE_MAXPS},
	{SN_MaxScalar, OP_XOP_X_X_X, SIMD_OP_SSE_MAXSS},
	{SN_Min, OP_XOP_X_X_X, SIMD_OP_SSE_MINPS},
	{SN_MinScalar, OP_XOP_X_X_X, SIMD_OP_SSE_MINSS},
	{SN_MoveHighToLow, OP_SSE_MOVEHL},
	{SN_MoveLowToHigh, OP_SSE_MOVELH},
	{SN_MoveMask, OP_SSE_MOVMSK},
	{SN_MoveScalar, OP_SSE_MOVS2},
	{SN_Multiply, OP_XBINOP, OP_FMUL},
	{SN_MultiplyScalar, OP_SSE_MULSS},
	{SN_Or, OP_SSE_OR},
	{SN_Prefetch0, OP_SSE_PREFETCHT0},
	{SN_Prefetch1, OP_SSE_PREFETCHT1},
	{SN_Prefetch2, OP_SSE_PREFETCHT2},
	{SN_PrefetchNonTemporal, OP_SSE_PREFETCHNTA},
	{SN_Reciprocal, OP_XOP_X_X, SIMD_OP_SSE_RCPPS},
	{SN_ReciprocalScalar, 0, SIMD_OP_SSE_RCPSS},
	{SN_ReciprocalSqrt, OP_XOP_X_X, SIMD_OP_SSE_RSQRTPS},
	{SN_ReciprocalSqrtScalar, 0, SIMD_OP_SSE_RSQRTSS},
	{SN_Sqrt, OP_XOP_X_X, SIMD_OP_SSE_SQRTPS},
	{SN_SqrtScalar, 0, SIMD_OP_SSE_SQRTSS},
	{SN_Shuffle},
	{SN_Store, OP_SSE_STORE, 1 /* alignment */},
	{SN_StoreAligned, OP_SSE_STORE, 16 /* alignment */},
	{SN_StoreAlignedNonTemporal, OP_SSE_MOVNTPS},
	{SN_StoreFence, OP_XOP, SIMD_OP_SSE_SFENCE},
	{SN_StoreHigh, OP_SSE_MOVHPS_STORE},
	{SN_StoreLow, OP_SSE_MOVLPS_STORE},
	{SN_StoreScalar, OP_SSE_MOVSS_STORE},
	{SN_Subtract, OP_XBINOP, OP_FSUB},
	{SN_SubtractScalar, OP_SSE_SUBSS},
	{SN_UnpackHigh, OP_SSE_UNPACKHI},
	{SN_UnpackLow, OP_SSE_UNPACKLO},
	{SN_Xor, OP_SSE_XOR},
	{SN_get_IsSupported}
};

static guint16 sse2_methods [] = {
	SN_Add,
	SN_AddSaturate,
	SN_AddScalar,
	SN_And,
	SN_AndNot,
	SN_Average,
	SN_CompareEqual,
	SN_CompareGreaterThan,
	SN_CompareLessThan,
	SN_CompareNotEqual,
	SN_CompareScalarEqual,
	SN_ConvertScalarToVector128Double,
	SN_ConvertScalarToVector128Int32,
	SN_ConvertScalarToVector128Int64,
	SN_ConvertScalarToVector128UInt32,
	SN_ConvertScalarToVector128UInt64,
	SN_ConvertToInt64,
	SN_ConvertToInt64WithTruncation,
	SN_ConvertToUInt32,
	SN_ConvertToUInt64,
	SN_LoadAlignedVector128,
	SN_LoadVector128,
	SN_MoveMask,
	SN_MoveScalar,
	SN_Or,
	SN_PackUnsignedSaturate,
	SN_ShiftRightLogical,
	SN_Shuffle,
	SN_Store,
	SN_StoreAligned,
	SN_StoreScalar,
	SN_Subtract,
	SN_UnpackHigh,
	SN_UnpackLow,
	SN_Xor,
	SN_get_IsSupported
};

static guint16 ssse3_methods [] = {
	SN_Shuffle,
	SN_get_IsSupported
};

static guint16 sse3_methods [] = {
	SN_MoveAndDuplicate,
	SN_get_IsSupported
};

static guint16 sse41_methods [] = {
	SN_Insert,
	SN_Max,
	SN_Min,
	SN_TestZ,
	SN_get_IsSupported
};

static guint16 popcnt_methods [] = {
	SN_PopCount,
	SN_get_IsSupported
};

static guint16 lzcnt_methods [] = {
	SN_LeadingZeroCount,
	SN_get_IsSupported
};

static guint16 bmi1_methods [] = {
	SN_AndNot,
	SN_BitFieldExtract,
	SN_ExtractLowestSetBit,
	SN_GetMaskUpToLowestSetBit,
	SN_ResetLowestSetBit,
	SN_TrailingZeroCount,
	SN_get_IsSupported,
};

static guint16 bmi2_methods [] = {
	SN_MultiplyNoFlags,
	SN_ParallelBitDeposit,
	SN_ParallelBitExtract,
	SN_ZeroHighBits,
	SN_get_IsSupported,
};

static MonoInst*
emit_x86_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins;
	int id;
	gboolean supported, is_64bit;
	MonoClass *klass = cmethod->klass;
	MonoTypeEnum arg0_type = fsig->param_count > 0 ? get_underlying_type (fsig->params [0]) : MONO_TYPE_VOID;
	SimdIntrinsic *info;
	gboolean is_corlib = m_class_get_image (cfg->method->klass) == mono_get_corlib ();

	if (is_hw_intrinsics_class (klass, "Sse", &is_64bit)) {
		if (!COMPILE_LLVM (cfg))
			return NULL;
		info = lookup_intrins_info (sse_methods, sizeof (sse_methods), cmethod);
		if (!info)
			return NULL;
		int id = info->id;

		supported = (mini_get_cpu_features (cfg) & MONO_CPU_X86_SSE) != 0;

		/* Common case */
		if (info->op != 0)
			return emit_simd_ins_for_sig (cfg, klass, info->op, info->instc0, arg0_type, fsig, args);

		switch (id) {
		case SN_get_IsSupported:
			EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
			ins->type = STACK_I4;
			return ins;
		case SN_Shuffle: {
			if (args [2]->opcode != OP_ICONST) {
				mono_cfg_set_exception (cfg, MONO_EXCEPTION_MONO_ERROR);
				mono_error_set_generic_error (cfg->error, "System", 
					"InvalidOperationException", "mask in Sse.Shuffle must be constant.");
				return NULL;
			}
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_SHUFFLE, args [2]->inst_c0 /*mask*/, arg0_type, fsig, args);
		}
		case SN_ConvertScalarToVector128Single:
			if (fsig->params [1]->type == MONO_TYPE_I4)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_I4, SIMD_OP_SSE_CVTSI2SS, 0, fsig, args);
			else if (fsig->params [1]->type == MONO_TYPE_I8)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_I8, SIMD_OP_SSE_CVTSI2SS64, 0, fsig, args);
			else
				g_assert_not_reached ();
			break;
		case SN_ReciprocalScalar:
		case SN_ReciprocalSqrtScalar:
		case SN_SqrtScalar:
			if (fsig->param_count == 1)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X, info->instc0, arg0_type, fsig, args);
			else
				return NULL;
		case SN_LoadScalarVector128:
			return NULL;
		default:
			return NULL;
		}
	}

	if (is_hw_intrinsics_class (klass, "Sse2", &is_64bit)) {
		if (!COMPILE_LLVM (cfg))
			return NULL;
		id = lookup_intrins (sse2_methods, sizeof (sse2_methods), cmethod);
		if (id == -1)
			return NULL;

		supported = (mini_get_cpu_features (cfg) & MONO_CPU_X86_SSE3) != 0 && is_corlib;// We only support the subset used by corelib
		
		switch (id) {
		case SN_get_IsSupported: {
			EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
			ins->type = STACK_I4;
			return ins;
		}
		case SN_Subtract:
			return emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, arg0_type == MONO_TYPE_R8 ? OP_FSUB : OP_ISUB, arg0_type, fsig, args);
		case SN_Add:
			return emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, arg0_type == MONO_TYPE_R8 ? OP_FADD : OP_IADD, arg0_type, fsig, args);
		case SN_AddSaturate:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_ADDS, -1, arg0_type, fsig, args);
		case SN_AddScalar:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_ADDSD, -1, arg0_type, fsig, args);
		case SN_And:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_AND, -1, arg0_type, fsig, args);
		case SN_AndNot:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_ANDN, -1, arg0_type, fsig, args);
		case SN_Average:
			if (arg0_type == MONO_TYPE_U1)
				return emit_simd_ins_for_sig (cfg, klass, OP_PAVGB_UN, -1, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_U2)
				return emit_simd_ins_for_sig (cfg, klass, OP_PAVGW_UN, -1, arg0_type, fsig, args);
			else
				return NULL;
		case SN_CompareNotEqual:
			return emit_simd_ins_for_sig (cfg, klass, arg0_type == MONO_TYPE_R8 ? OP_XCOMPARE_FP : OP_XCOMPARE, CMP_NE, arg0_type, fsig, args);
		case SN_CompareEqual:
			return emit_simd_ins_for_sig (cfg, klass, arg0_type == MONO_TYPE_R8 ? OP_XCOMPARE_FP : OP_XCOMPARE, CMP_EQ, arg0_type, fsig, args);
		case SN_CompareGreaterThan:
			return emit_simd_ins_for_sig (cfg, klass, arg0_type == MONO_TYPE_R8 ? OP_XCOMPARE_FP : OP_XCOMPARE, CMP_GT, arg0_type, fsig, args);
		case SN_CompareLessThan:
			return emit_simd_ins_for_sig (cfg, klass, arg0_type == MONO_TYPE_R8 ? OP_XCOMPARE_FP : OP_XCOMPARE, CMP_LT, arg0_type, fsig, args);
		case SN_CompareScalarEqual:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_CMPSD, CMP_EQ, arg0_type, fsig, args);
		case SN_ConvertScalarToVector128Int32:
		case SN_ConvertScalarToVector128Int64:
		case SN_ConvertScalarToVector128UInt32:
		case SN_ConvertScalarToVector128UInt64:
			return emit_simd_ins_for_sig (cfg, klass, OP_CREATE_SCALAR, -1, arg0_type, fsig, args);
		case SN_ConvertToUInt32:
			return emit_simd_ins_for_sig (cfg, klass, OP_EXTRACT_I4, 0 /*element index*/, arg0_type, fsig, args);
		case SN_ConvertToUInt64:
			return emit_simd_ins_for_sig (cfg, klass, OP_EXTRACT_I8, 0 /*element index*/, arg0_type, fsig, args);
		case SN_LoadAlignedVector128:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_LOADU, 16 /*alignment*/, arg0_type, fsig, args);
		case SN_LoadVector128:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_LOADU, 1 /*alignment*/, arg0_type, fsig, args);
		case SN_MoveMask:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_MOVMSK, -1, arg0_type, fsig, args);
		case SN_MoveScalar:
			return emit_simd_ins_for_sig (cfg, klass, fsig->param_count == 2 ? OP_SSE_MOVS2 : OP_SSE_MOVS, -1, arg0_type, fsig, args);
		case SN_PackUnsignedSaturate:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_PACKUS, -1, arg0_type, fsig, args);
		case SN_ShiftRightLogical: {
			if (arg0_type != MONO_TYPE_U2 || fsig->params [1]->type != MONO_TYPE_U1)
				return NULL; // TODO: implement other overloads
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_SRLI, -1, arg0_type, fsig, args);
		}
		case SN_Shuffle: {
			if ((arg0_type == MONO_TYPE_R8 && args [2]->opcode != OP_ICONST) || 
				(arg0_type != MONO_TYPE_R8 && args [1]->opcode != OP_ICONST)) {
				mono_cfg_set_exception (cfg, MONO_EXCEPTION_MONO_ERROR);
				mono_error_set_generic_error (cfg->error, "System", "InvalidOperationException",
					"mask in Sse2.Shuffle must be constant.");
				return NULL;
			}
			ins = emit_simd_ins_for_sig (cfg, klass, OP_SSE2_SHUFFLE, -1, arg0_type, fsig, args);
			ins->sreg3 = -1; // last arg is always a constant mask
			if (arg0_type == MONO_TYPE_R8) { // "double" overload accepts two vectors
				ins->sreg2 = args [1]->dreg;
				ins->inst_c0 = args [2]->inst_c0; // mask
			} else {
				ins->sreg2 = args [0]->dreg;
				ins->inst_c0 = args [1]->inst_c0; // mask
			}
			return ins;
		}
		case SN_Store:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_STORE, 1 /*alignment*/, arg0_type, fsig, args);
		case SN_StoreAligned:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_STORE, 16 /*alignment*/, arg0_type, fsig, args);
		case SN_StoreScalar:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_STORES, -1, arg0_type, fsig, args);
		case SN_UnpackLow:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_UNPACKLO, -1, arg0_type, fsig, args);
		case SN_UnpackHigh:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_UNPACKHI, -1, arg0_type, fsig, args);
		case SN_Or:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_OR, -1, arg0_type, fsig, args);
		case SN_Xor:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_XOR, -1, arg0_type, fsig, args);
		default:
			return NULL;
		}
	}

	if (is_hw_intrinsics_class (klass, "Sse3", &is_64bit)) {
		if (!COMPILE_LLVM (cfg))
			return NULL;
		id = lookup_intrins (sse3_methods, sizeof (sse3_methods), cmethod);
		if (id == -1)
			return NULL;

		supported = (mini_get_cpu_features (cfg) & MONO_CPU_X86_SSE3) != 0 && is_corlib; // We only support the subset used by corelib

		switch (id) {
		case SN_get_IsSupported:
			EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
			ins->type = STACK_I4;
			return ins;
		case SN_MoveAndDuplicate:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE3_MOVDDUP, -1, arg0_type, fsig, args);
		default:
			return NULL;
		}
	}

	if (is_hw_intrinsics_class (klass, "Ssse3", &is_64bit)) {
		if (!COMPILE_LLVM (cfg))
			return NULL;
		id = lookup_intrins (ssse3_methods, sizeof (ssse3_methods), cmethod);
		if (id == -1)
			return NULL;

		supported = (mini_get_cpu_features (cfg) & MONO_CPU_X86_SSSE3) != 0 && is_corlib; // We only support the subset used by corelib

		switch (id) {
		case SN_get_IsSupported:
			EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
			ins->type = STACK_I4;
			return ins;
		case SN_Shuffle:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSSE3_SHUFFLE, -1, arg0_type, fsig, args);
		default:
			return NULL;
		}
	}

	if (is_hw_intrinsics_class (klass, "Sse41", &is_64bit)) {
		if (!COMPILE_LLVM (cfg))
			return NULL;
		id = lookup_intrins (sse41_methods, sizeof (sse41_methods), cmethod);
		if (id == -1)
			return NULL;

		supported = COMPILE_LLVM (cfg) && (mini_get_cpu_features (cfg) & MONO_CPU_X86_SSE41) != 0 && is_corlib; // We only support the subset used by corelib

		switch (id) {
		case SN_get_IsSupported:
			EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
			ins->type = STACK_I4;
			return ins;
		case SN_Insert:
			if (args [2]->opcode != OP_ICONST) {
				mono_cfg_set_exception (cfg, MONO_EXCEPTION_MONO_ERROR);
				mono_error_set_generic_error (cfg->error, "System", 
					"InvalidOperationException", "index in Sse41.Insert must be constant.");
				return NULL;
			}
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE41_INSERT, -1, arg0_type, fsig, args);
		case SN_Max:
			return emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, OP_IMAX, arg0_type, fsig, args);
		case SN_Min:
			return emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, OP_IMIN, arg0_type, fsig, args);
		case SN_TestZ:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE41_PTESTZ, -1, arg0_type, fsig, args);
		default:
			return NULL;
		}
	}

	if (is_hw_intrinsics_class (klass, "Popcnt", &is_64bit)) {
		id = lookup_intrins (popcnt_methods, sizeof (popcnt_methods), cmethod);
		if (id == -1)
			return NULL;

		supported = (mini_get_cpu_features (cfg) & MONO_CPU_X86_POPCNT) != 0;

		switch (id) {
		case SN_get_IsSupported:
			EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
			ins->type = STACK_I4;
			return ins;
		case SN_PopCount:
			if (!supported)
				return NULL;
			MONO_INST_NEW (cfg, ins, is_64bit ? OP_POPCNT64 : OP_POPCNT32);
			ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->type = is_64bit ? STACK_I8 : STACK_I4;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		default:
			return NULL;
		}
	}
	if (is_hw_intrinsics_class (klass, "Lzcnt", &is_64bit)) {
		id = lookup_intrins (lzcnt_methods, sizeof (lzcnt_methods), cmethod);
		if (id == -1)
			return NULL;

		supported = (mini_get_cpu_features (cfg) & MONO_CPU_X86_LZCNT) != 0;

		switch (id) {
		case SN_get_IsSupported:
			EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
			ins->type = STACK_I4;
			return ins;
		case SN_LeadingZeroCount:
			if (!supported)
				return NULL;
			MONO_INST_NEW (cfg, ins, is_64bit ? OP_LZCNT64 : OP_LZCNT32);
			ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->type = is_64bit ? STACK_I8 : STACK_I4;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		default:
			return NULL;
		}
	}
	if (is_hw_intrinsics_class (klass, "Bmi1", &is_64bit)) {
		if (!COMPILE_LLVM (cfg))
			return NULL;
		id = lookup_intrins (bmi1_methods, sizeof (bmi1_methods), cmethod);

		g_assert (id != -1);
		supported = (mini_get_cpu_features (cfg) & MONO_CPU_X86_BMI1) != 0;

		switch (id) {
		case SN_get_IsSupported:
			EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
			ins->type = STACK_I4;
			return ins;
		case SN_AndNot: {
			// (a ^ -1) & b
			// LLVM replaces it with `andn`
			int tmp_reg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			int result_reg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			EMIT_NEW_BIALU_IMM (cfg, ins, is_64bit ? OP_LXOR_IMM : OP_IXOR_IMM, tmp_reg, args [0]->dreg, -1);
			EMIT_NEW_BIALU (cfg, ins, is_64bit ? OP_LAND : OP_IAND, result_reg, tmp_reg, args [1]->dreg);
			return ins;
		}
		case SN_BitFieldExtract: {
			if (fsig->param_count == 2) {
				MONO_INST_NEW (cfg, ins, is_64bit ? OP_BEXTR64 : OP_BEXTR32);
				ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
				ins->sreg1 = args [0]->dreg;
				ins->sreg2 = args [1]->dreg;
				ins->type = is_64bit ? STACK_I8 : STACK_I4;
				MONO_ADD_INS (cfg->cbb, ins);
				return ins;
			}
		}
		case SN_GetMaskUpToLowestSetBit: {
			// x ^ (x - 1)
			// LLVM replaces it with `blsmsk`
			int tmp_reg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			int result_reg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			EMIT_NEW_BIALU_IMM (cfg, ins, is_64bit ? OP_LSUB_IMM : OP_ISUB_IMM, tmp_reg, args [0]->dreg, 1);
			EMIT_NEW_BIALU (cfg, ins, is_64bit ? OP_LXOR : OP_IXOR, result_reg, args [0]->dreg, tmp_reg);
			return ins;
		}
		case SN_ResetLowestSetBit: {
			// x & (x - 1)
			// LLVM replaces it with `blsr`
			int tmp_reg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			int result_reg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			EMIT_NEW_BIALU_IMM (cfg, ins, is_64bit ? OP_LSUB_IMM : OP_ISUB_IMM, tmp_reg, args [0]->dreg, 1);
			EMIT_NEW_BIALU (cfg, ins, is_64bit ? OP_LAND : OP_IAND, result_reg, args [0]->dreg, tmp_reg);
			return ins;
		}
		case SN_ExtractLowestSetBit: {
			// x & (0 - x)
			// LLVM replaces it with `blsi`
			int tmp_reg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			int result_reg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			int zero_reg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			MONO_EMIT_NEW_ICONST (cfg, zero_reg, 0);
			EMIT_NEW_BIALU (cfg, ins, is_64bit ? OP_LSUB : OP_ISUB, tmp_reg, zero_reg, args [0]->dreg);
			EMIT_NEW_BIALU (cfg, ins, is_64bit ? OP_LAND : OP_IAND, result_reg, args [0]->dreg, tmp_reg);
			return ins;
		}
		case SN_TrailingZeroCount:
			MONO_INST_NEW (cfg, ins, is_64bit ? OP_CTTZ64 : OP_CTTZ32);
			ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->type = is_64bit ? STACK_I8 : STACK_I4;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		default:
			g_assert_not_reached ();
		}
	}
	if (is_hw_intrinsics_class (klass, "Bmi2", &is_64bit)) {
		if (!COMPILE_LLVM (cfg))
			return NULL;
		id = lookup_intrins (bmi2_methods, sizeof (bmi2_methods), cmethod);
		g_assert (id != -1);
		supported = (mini_get_cpu_features (cfg) & MONO_CPU_X86_BMI2) != 0;

		switch (id) {
		case SN_get_IsSupported:
			EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
			ins->type = STACK_I4;
			return ins;
		case SN_MultiplyNoFlags:
			if (fsig->param_count == 2) {
				MONO_INST_NEW (cfg, ins, is_64bit ? OP_MULX_H64 : OP_MULX_H32);
				ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
				ins->sreg1 = args [0]->dreg;
				ins->sreg2 = args [1]->dreg;
				ins->type = is_64bit ? STACK_I8 : STACK_I4;
				MONO_ADD_INS (cfg->cbb, ins);
			} else if (fsig->param_count == 3) {
				MONO_INST_NEW (cfg, ins, is_64bit ? OP_MULX_HL64 : OP_MULX_HL32);
				ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
				ins->sreg1 = args [0]->dreg;
				ins->sreg2 = args [1]->dreg;
				ins->sreg3 = args [2]->dreg;
				ins->type = is_64bit ? STACK_I8 : STACK_I4;
				MONO_ADD_INS (cfg->cbb, ins);
			} else {
				g_assert_not_reached ();
			}
			return ins;
		case SN_ZeroHighBits:
			MONO_INST_NEW (cfg, ins, is_64bit ? OP_BZHI64 : OP_BZHI32);
			ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->sreg2 = args [1]->dreg;
			ins->type = is_64bit ? STACK_I8 : STACK_I4;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		case SN_ParallelBitExtract:
			MONO_INST_NEW (cfg, ins, is_64bit ? OP_PEXT64 : OP_PEXT32);
			ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->sreg2 = args [1]->dreg;
			ins->type = is_64bit ? STACK_I8 : STACK_I4;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		case SN_ParallelBitDeposit:
			MONO_INST_NEW (cfg, ins, is_64bit ? OP_PDEP64 : OP_PDEP32);
			ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->sreg2 = args [1]->dreg;
			ins->type = is_64bit ? STACK_I8 : STACK_I4;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		default:
			g_assert_not_reached ();
		}
	}

	return NULL;
}
#endif

static guint16 vector_128_methods [] = {
	SN_AsByte,
	SN_AsDouble,
	SN_AsInt16,
	SN_AsInt32,
	SN_AsInt64,
	SN_AsSByte,
	SN_AsSingle,
	SN_AsUInt16,
	SN_AsUInt32,
	SN_AsUInt64,
	SN_Create,
	SN_CreateScalarUnsafe,
};

static guint16 vector_128_t_methods [] = {
	SN_get_Count,
	SN_get_Zero,
};

static MonoInst*
emit_vector128 (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoClass *klass;
	int id;

	if (!COMPILE_LLVM (cfg))
		return NULL;

	klass = cmethod->klass;
	id = lookup_intrins (vector_128_methods, sizeof (vector_128_methods), cmethod);
	if (id == -1)
		return NULL;

	if (!strcmp (m_class_get_name (cfg->method->klass), "Vector256"))
		return NULL; // TODO: Fix Vector256.WithUpper/WithLower

	MonoTypeEnum arg0_type = fsig->param_count > 0 ? get_underlying_type (fsig->params [0]) : MONO_TYPE_VOID;

	switch (id) {
	case SN_AsByte:
	case SN_AsDouble:
	case SN_AsInt16:
	case SN_AsInt32:
	case SN_AsInt64:
	case SN_AsSByte:
	case SN_AsSingle:
	case SN_AsUInt16:
	case SN_AsUInt32:
	case SN_AsUInt64:
		return emit_simd_ins (cfg, klass, OP_XCAST, args [0]->dreg, -1);
	case SN_Create: {
		MonoType *etype = get_vector_t_elem_type (fsig->ret);
		if (fsig->param_count == 1 && mono_metadata_type_equal (fsig->params [0], etype)) {
			return emit_simd_ins (cfg, klass, type_to_expand_op (etype), args [0]->dreg, -1);
		} else {
			// TODO: Optimize Create(a1, a2, a3 ...) overloads
			break;
		}
	}
	case SN_CreateScalarUnsafe:
		return emit_simd_ins_for_sig (cfg, klass, OP_CREATE_SCALAR_UNSAFE, -1, arg0_type, fsig, args);
	default:
		break;
	}

	return NULL;
}

static MonoInst*
emit_vector128_t (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins;
	MonoType *type, *etype;
	MonoClass *klass;
	int size, len, id;

	id = lookup_intrins (vector_128_t_methods, sizeof (vector_128_t_methods), cmethod);
	if (id == -1)
		return NULL;

	klass = cmethod->klass;
	type = m_class_get_byval_arg (klass);
	etype = mono_class_get_context (klass)->class_inst->type_argv [0];
	size = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
	g_assert (size);
	len = 16 / size;

	if (!MONO_TYPE_IS_PRIMITIVE (etype) || etype->type == MONO_TYPE_CHAR || etype->type == MONO_TYPE_BOOLEAN)
		return NULL;

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	switch (id) {
	case SN_get_Count:
		if (!(fsig->param_count == 0 && fsig->ret->type == MONO_TYPE_I4))
			break;
		EMIT_NEW_ICONST (cfg, ins, len);
		return ins;
	case SN_get_Zero: {
		return emit_simd_ins (cfg, klass, OP_XZERO, -1, -1);
	}
	default:
		break;
	}

	return NULL;
}

static guint16 vector_256_t_methods [] = {
	SN_get_Count,
};

static MonoInst*
emit_vector256_t (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins;
	MonoType *type, *etype;
	MonoClass *klass;
	int size, len, id;

	id = lookup_intrins (vector_256_t_methods, sizeof (vector_256_t_methods), cmethod);
	if (id == -1)
		return NULL;

	klass = cmethod->klass;
	type = m_class_get_byval_arg (klass);
	etype = mono_class_get_context (klass)->class_inst->type_argv [0];
	size = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
	g_assert (size);
	len = 32 / size;

	if (!MONO_TYPE_IS_PRIMITIVE (etype) || etype->type == MONO_TYPE_CHAR || etype->type == MONO_TYPE_BOOLEAN)
		return NULL;

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	switch (id) {
	case SN_get_Count:
		if (!(fsig->param_count == 0 && fsig->ret->type == MONO_TYPE_I4))
			break;
		EMIT_NEW_ICONST (cfg, ins, len);
		return ins;
	default:
		break;
	}

	return NULL;
}

MonoInst*
mono_emit_simd_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	const char *class_name;
	const char *class_ns;
	MonoImage *image = m_class_get_image (cmethod->klass);

	if (image != mono_get_corlib ())
		return NULL;

	class_ns = m_class_get_name_space (cmethod->klass);
	class_name = m_class_get_name (cmethod->klass);

	// If cmethod->klass is nested, the namespace is on the enclosing class.
	if (m_class_get_nested_in (cmethod->klass))
		class_ns = m_class_get_name_space (m_class_get_nested_in (cmethod->klass));

#ifdef TARGET_AMD64 // TODO: test and enable for x86 too
	if (!strcmp (class_ns, "System.Runtime.Intrinsics.X86")) {
		MonoInst *ins = emit_x86_intrinsics (cfg ,cmethod, fsig, args);
		return ins;
	}
#endif

	if (!strcmp (class_ns, "System.Runtime.Intrinsics")) {
		if (!strcmp (class_name, "Vector128`1"))
			return emit_vector128_t (cfg, cmethod, fsig, args);
		if (!strcmp (class_name, "Vector128"))
			return emit_vector128 (cfg, cmethod, fsig, args);
		if (!strcmp (class_name, "Vector256`1"))
			return emit_vector256_t (cfg, cmethod, fsig, args);
	}

	if (!strcmp (class_ns, "System.Numerics")) {
		if (!strcmp (class_name, "Vector"))
			return emit_sys_numerics_vector (cfg, cmethod, fsig, args);
		if (!strcmp (class_name, "Vector`1"))
			return emit_sys_numerics_vector_t (cfg, cmethod, fsig, args);
	}

	return NULL;
}

void
mono_simd_decompose_intrinsic (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins)
{
}

void
mono_simd_simplify_indirection (MonoCompile *cfg)
{
}

#else

MONO_EMPTY_SOURCE_FILE (simd_intrinsics_netcore);

#endif

#endif /* DISABLE_JIT */
