/**
 * SIMD Intrinsics support for netcore.
 * Only LLVM is supported as a backend.
 */

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

#if defined (MONO_ARCH_SIMD_INTRINSICS)

#if defined(DISABLE_JIT)

void
mono_simd_intrinsics_init (void)
{
}

#else

#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define METHOD(name) char MSGSTRFIELD(__LINE__) [sizeof (#name)];
#define METHOD2(str,name) char MSGSTRFIELD(__LINE__) [sizeof (str)];
#include "simd-methods.h"
#undef METHOD
#undef METHOD2
} method_names = {
#define METHOD(name) #name,
#define METHOD2(str,name) str,
#include "simd-methods.h"
#undef METHOD
#undef METHOD2
};

enum {
#define METHOD(name) SN_ ## name = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#define METHOD2(str,name) SN_ ## name = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "simd-methods.h"
};
#define method_name(idx) ((const char*)&method_names + (idx))

static int register_size;

#define None 0

typedef struct {
	uint16_t id; // One of the SN_ constants
	uint16_t default_op; // ins->opcode
	uint16_t default_instc0; // ins->inst_c0
	uint16_t unsigned_op;
	uint16_t unsigned_instc0;
	uint16_t floating_op;
	uint16_t floating_instc0;
} SimdIntrinsic;

static const SimdIntrinsic unsupported [] = { {SN_get_IsSupported} };

void
mono_simd_intrinsics_init (void)
{
	register_size = 16;
#if 0
	if ((mini_get_cpu_features () & MONO_CPU_X86_AVX) != 0)
		register_size = 32;
#endif
}

MonoInst*
mono_emit_simd_field_load (MonoCompile *cfg, MonoClassField *field, MonoInst *addr)
{
	return NULL;
}

static gboolean
is_zero_const (const MonoInst* ins)
{
	switch (ins->opcode) {
	case OP_ICONST:
		return (0 == GTMREG_TO_INT (ins->inst_c0));
	case OP_I8CONST:
		return (0 == ins->inst_l);
	case OP_R4CONST:
		return (0 == *(const uint32_t*)(ins->inst_p0)); // Must be binary zero. -0.0f has a sign of 1.
	case OP_R8CONST:
		return (0 == *(const uint64_t*)(ins->inst_p0));
	}
	return FALSE;
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

static gboolean
has_intrinsic_cattr (MonoMethod *method)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *cattr;
	gboolean res = FALSE;

	cattr = mono_custom_attrs_from_method_checked (method, error);
	mono_error_assert_ok (error);

	if (cattr) {
		for (int i = 0; i < cattr->num_attrs; ++i) {
			MonoCustomAttrEntry *attr = &cattr->attrs [i];

			g_assert (attr->ctor);

			if (!strcmp (m_class_get_name_space (attr->ctor->klass), "System.Runtime.CompilerServices") &&
				!strcmp (m_class_get_name (attr->ctor->klass), "IntrinsicAttribute")) {
				res = TRUE;
				break;
			}
		}
		mono_custom_attrs_free (cattr);
	}

	return res;
}

static gboolean
is_SIMD_feature_supported(MonoCompile *cfg, MonoCPUFeatures feature) 
{
	return mini_get_cpu_features (cfg) & feature;
}

static G_GNUC_UNUSED void
check_no_intrinsic_cattr (MonoMethod *method)
{
	if (has_intrinsic_cattr (method)) {
		printf ("%s\n", mono_method_get_full_name (method));
		g_assert_not_reached ();
	}
}

static gboolean
type_is_simd_vector (MonoType *type)
{
	return m_class_is_simd_type (mono_class_from_mono_type_internal (type));
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

static gboolean type_enum_is_unsigned (MonoTypeEnum type);
static gboolean type_enum_is_float (MonoTypeEnum type);
static int type_to_expand_op (MonoTypeEnum type);

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

static MonoInst*
emit_simd_ins_for_binary_op (MonoCompile *cfg, MonoClass *klass, MonoMethodSignature *fsig, MonoInst **args, MonoTypeEnum arg_type, int id)
{
	int instc0 = -1;
	int op = OP_XBINOP;

	if (id == SN_BitwiseAnd || id == SN_BitwiseOr || id == SN_Xor ||
		id == SN_op_BitwiseAnd || id == SN_op_BitwiseOr || id == SN_op_ExclusiveOr) {
		op = OP_XBINOP_FORCEINT;
	
		switch (id) {
		case SN_BitwiseAnd:
		case SN_op_BitwiseAnd:
			instc0 = XBINOP_FORCEINT_AND;
			break;
		case SN_BitwiseOr:
		case SN_op_BitwiseOr:
			instc0 = XBINOP_FORCEINT_OR;
			break;
		case SN_op_ExclusiveOr:
		case SN_Xor:
			instc0 = XBINOP_FORCEINT_XOR;
			break;
		}
	} else {
		if (type_enum_is_float (arg_type)) {
			switch (id) {
			case SN_Add:
			case SN_op_Addition:
				instc0 = OP_FADD;
				break;
			case SN_Divide:
			case SN_op_Division: {
				const char *class_name = m_class_get_name (klass);
				if (strcmp ("Quaternion", class_name) && strcmp ("Plane", class_name)) {
					if (!type_is_simd_vector (fsig->params [1]))
						return handle_mul_div_by_scalar (cfg, klass, arg_type, args [1]->dreg, args [0]->dreg, OP_FDIV);
					else if (type_is_simd_vector (fsig->params [0]) && type_is_simd_vector (fsig->params [1])) {
						instc0 = OP_FDIV;
						break;
					} else {
						return NULL;
					}
				}
				instc0 = OP_FDIV;
				break;
			}
			case SN_Max:
				instc0 = OP_FMAX;
				break;
			case SN_Min:
				instc0 = OP_FMIN;
				break;
			case SN_Multiply:
			case SN_op_Multiply: {
				const char *class_name = m_class_get_name (klass);
				if (strcmp ("Quaternion", class_name) && strcmp ("Plane", class_name)) {
					if (!type_is_simd_vector (fsig->params [1]))
						return handle_mul_div_by_scalar (cfg, klass, arg_type, args [1]->dreg, args [0]->dreg, OP_FMUL);
					else if (!type_is_simd_vector (fsig->params [0]))
						return handle_mul_div_by_scalar (cfg, klass, arg_type, args [0]->dreg, args [1]->dreg, OP_FMUL);
					else if (type_is_simd_vector (fsig->params [0]) && type_is_simd_vector (fsig->params [1])) {
						instc0 = OP_FMUL;
						break;
					} else {
						return NULL;
					}
				}
				instc0 = OP_FMUL;
				break;
			}
			case SN_Subtract:
			case SN_op_Subtraction:
				instc0 = OP_FSUB;
				break;
			default:
				g_assert_not_reached ();
			}
		} else {
			switch (id) {
			case SN_Add:
			case SN_op_Addition:
				instc0 = OP_IADD;
				break;
			case SN_Divide:
			case SN_op_Division:
				return NULL;
			case SN_Max:
				instc0 = type_enum_is_unsigned (arg_type) ? OP_IMAX_UN : OP_IMAX;
#ifdef TARGET_AMD64
				if (!COMPILE_LLVM (cfg) && instc0 == OP_IMAX_UN)
					return NULL;
#endif
				break;
			case SN_Min:
				instc0 = type_enum_is_unsigned (arg_type) ? OP_IMIN_UN : OP_IMIN;
#ifdef TARGET_AMD64
				if (!COMPILE_LLVM (cfg) && instc0 == OP_IMIN_UN)
					return NULL;
#endif
				break;
			case SN_Multiply:
			case SN_op_Multiply: {
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
			case SN_Subtract:
			case SN_op_Subtraction:
				instc0 = OP_ISUB;
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}
	return emit_simd_ins_for_sig (cfg, klass, op, instc0, arg_type, fsig, args);
}

static MonoInst*
emit_simd_ins_for_unary_op (MonoCompile *cfg, MonoClass *klass, MonoMethodSignature *fsig, MonoInst **args, MonoTypeEnum arg_type, int id)
{
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
	int op = -1;
	switch (id){
	case SN_Negate:
	case SN_op_UnaryNegation:
		op = OP_NEGATION;
		break;
	case SN_OnesComplement:
	case SN_op_OnesComplement:
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
	case SN_Negate:
		op = OP_NEGATION;
		break;
	case SN_OnesComplement:
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

static gboolean
is_hw_intrinsics_class (MonoClass *klass, const char *name, gboolean *is_64bit)
{
	const char *class_name = m_class_get_name (klass);
	if ((!strcmp (class_name, "X64") || !strcmp (class_name, "Arm64")) && m_class_get_nested_in (klass)) {
		*is_64bit = TRUE;
		return !strcmp (m_class_get_name (m_class_get_nested_in (klass)), name);
	} else if (!strcmp (class_name, "VL")) {
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
	int opcode = type_enum_is_float (etype) ? OP_XCOMPARE_FP : OP_XCOMPARE;

	ins = emit_simd_ins (cfg, klass, opcode, arg1->dreg, arg2->dreg);
	ins->inst_c0 = CMP_EQ;
	ins->inst_c1 = etype;
	return ins;
}

static MonoInst*
emit_xcompare_for_intrinsic (MonoCompile *cfg, MonoClass *klass, int intrinsic_id, MonoTypeEnum etype, MonoInst *arg1, MonoInst *arg2)
{
	MonoInst *ins = emit_xcompare (cfg, klass, etype, arg1, arg2);
	gboolean is_unsigned = type_enum_is_unsigned (etype);

	switch (intrinsic_id) {
	case SN_GreaterThan:
	case SN_GreaterThanAll:
	case SN_GreaterThanAny:
		ins->inst_c0 = is_unsigned ? CMP_GT_UN : CMP_GT;
		break;
	case SN_GreaterThanOrEqual:
	case SN_GreaterThanOrEqualAll:
	case SN_GreaterThanOrEqualAny:
		ins->inst_c0 = is_unsigned ? CMP_GE_UN : CMP_GE;
		break;
	case SN_LessThan:
	case SN_LessThanAll:
	case SN_LessThanAny:
		ins->inst_c0 = is_unsigned ? CMP_LT_UN : CMP_LT;
		break;
	case SN_LessThanOrEqual:
	case SN_LessThanOrEqualAll:
	case SN_LessThanOrEqualAny:
		ins->inst_c0 = is_unsigned ? CMP_LE_UN : CMP_LE;
		break;
	default:
		g_assert_not_reached ();
	}

	return ins;
}

static MonoInst*
emit_xequal (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum element_type, MonoInst *arg1, MonoInst *arg2)
{
#ifdef TARGET_ARM64
	gint32 simd_size = mono_class_value_size (klass, NULL);
	if (!COMPILE_LLVM (cfg)) {
		MonoInst* cmp = emit_xcompare (cfg, klass, element_type, arg1, arg2);
		MonoInst* ret = emit_simd_ins (cfg, mono_defaults.boolean_class, OP_XEXTRACT, cmp->dreg, -1);
		ret->inst_c0 = SIMD_EXTR_ARE_ALL_SET;
		ret->inst_c1 = mono_class_value_size (klass, NULL);
		return ret;
	} else if (simd_size == 12 || simd_size == 16) {
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

static MonoInst*
emit_not_xequal (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum element_type, MonoInst *arg1, MonoInst *arg2)
{
	MonoInst *ins = emit_xequal (cfg, klass, element_type, arg1, arg2);
	int sreg = ins->dreg;
	int dreg = alloc_ireg (cfg);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, sreg, 0);
	EMIT_NEW_UNALU (cfg, ins, OP_CEQ, dreg, -1);
	return ins;
}

static MonoInst*
emit_xzero (MonoCompile *cfg, MonoClass *klass)
{
	return emit_simd_ins (cfg, klass, OP_XZERO, -1, -1);
}

static MonoInst*
emit_xones (MonoCompile *cfg, MonoClass *klass)
{
	return emit_simd_ins (cfg, klass, OP_XONES, -1, -1);
}

static MonoInst*
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
static int type_to_extract_op (MonoTypeEnum type);
static MonoType* get_vector_t_elem_type (MonoType *vector_type);

static MonoInst*
emit_sum_vector (MonoCompile *cfg, MonoType *vector_type, MonoTypeEnum element_type, MonoInst *arg)
{
	MonoClass *vector_class = mono_class_from_mono_type_internal (vector_type);
	int vector_size = mono_class_value_size (vector_class, NULL);
	int element_size;
	
	guint32 nelems;
 	mini_get_simd_type_info (vector_class, &nelems);

	// Override nelems for Vector3, with actual number of elements, instead of treating it as a 4-element vector (three elements + zero).
	const char *klass_name = m_class_get_name (vector_class); 
	if (!strcmp (klass_name, "Vector3"))
		nelems = 3;

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
#endif
#ifdef TARGET_WASM
static MonoInst* emit_sum_vector (MonoCompile *cfg, MonoType *vector_type, MonoTypeEnum element_type, MonoInst *arg);
#endif

#if defined(TARGET_AMD64) || defined(TARGET_WASM)
static int type_to_extract_op (MonoTypeEnum type);
static MonoInst*
extract_first_element (MonoCompile *cfg, MonoClass *klass, MonoTypeEnum element_type, int sreg)
{
	int extract_op = type_to_extract_op (element_type);
	MonoInst *ins = emit_simd_ins (cfg, klass, extract_op, sreg, -1);
	ins->inst_c0 = 0;
	ins->inst_c1 = element_type;

	return ins;
}
#endif

#ifdef TARGET_AMD64
static const int fast_log2 [] = { -1, -1, 1, -1, 2, -1, -1, -1, 3 };

static MonoInst*
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
#endif

static gboolean
is_intrinsics_vector_type (MonoType *vector_type)
{
	if (vector_type->type != MONO_TYPE_GENERICINST) return FALSE;
	MonoClass *klass = mono_class_from_mono_type_internal (vector_type);
	const char *name = m_class_get_name (klass);
	return !strcmp (name, "Vector64`1") || !strcmp (name, "Vector128`1") || !strcmp (name, "Vector256`1") || !strcmp (name, "Vector512`1");
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
		!strcmp (m_class_get_name (klass), "Vector64`1") ||
		!strcmp (m_class_get_name (klass), "Vector128`1") ||
		!strcmp (m_class_get_name (klass), "Vector256`1") ||
		!strcmp (m_class_get_name (klass), "Vector512`1"));
	etype = mono_class_get_context (klass)->class_inst->type_argv [0];
	return etype;
}

static gboolean
type_is_unsigned (MonoType *type)
{
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	MonoType *etype = mono_class_get_context (klass)->class_inst->type_argv [0];
	return type_enum_is_unsigned (etype->type);
}

static gboolean
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

static gboolean
type_is_float (MonoType *type)
{
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	MonoType *etype = mono_class_get_context (klass)->class_inst->type_argv [0];
	return type_enum_is_float (etype->type);
}

static gboolean
type_enum_is_float (MonoTypeEnum type)
{
	return type == MONO_TYPE_R4 || type == MONO_TYPE_R8;
}

static int
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

static int
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

static int
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

typedef struct {
	const char *name;
	MonoCPUFeatures feature;
	const SimdIntrinsic *intrinsics;
	int intrinsics_size;
	gboolean jit_supported;
} IntrinGroup;

typedef MonoInst * (* EmitIntrinsicFn) (
	MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args,
	MonoClass *klass, const IntrinGroup *intrin_group,
	const SimdIntrinsic *info, int id, MonoTypeEnum arg0_type,
	gboolean is_64bit);

static const IntrinGroup unsupported_intrin_group [] = {
	{ "", 0, unsupported, sizeof (unsupported) },
};

static MonoInst *
emit_hardware_intrinsics (
	MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig,
	MonoInst **args, const IntrinGroup *groups, int groups_size_bytes,
	EmitIntrinsicFn custom_emit)
{
	MonoClass *klass = cmethod->klass;
	const IntrinGroup *intrin_group = unsupported_intrin_group;
	gboolean is_64bit = FALSE;
	int groups_size = groups_size_bytes / sizeof (groups [0]);
	for (int i = 0; i < groups_size; ++i) {
		const IntrinGroup *group = &groups [i];
		if (is_hw_intrinsics_class (klass, group->name, &is_64bit)) {
			intrin_group = group;
			break;
		}
	}

	gboolean supported = FALSE;
	MonoTypeEnum arg0_type = fsig->param_count > 0 ? get_underlying_type (fsig->params [0]) : MONO_TYPE_VOID;
	int id = -1;
	uint16_t op = 0;
	uint16_t c0 = 0;
	const SimdIntrinsic *intrinsics = intrin_group->intrinsics;
	int intrinsics_size = intrin_group->intrinsics_size;
	MonoCPUFeatures feature = intrin_group->feature;
	const SimdIntrinsic *info = lookup_intrins_info ((SimdIntrinsic *) intrinsics, intrinsics_size, cmethod);
	{
		if (!info)
			goto support_probe_complete;
		id = info->id;

#ifdef TARGET_ARM64
		if (!(cfg->compile_aot && cfg->full_aot && !cfg->interp) && !intrin_group->jit_supported) {
			goto support_probe_complete;
		}
#endif

		// Hardware intrinsics are LLVM-only.
		if (!COMPILE_LLVM (cfg) && !intrin_group->jit_supported)
			goto support_probe_complete;

		if (intrin_group->intrinsics == unsupported)
			supported = FALSE;
		else if (feature)
			supported = (mini_get_cpu_features (cfg) & feature) != 0;
		else
			supported = TRUE;


		op = info->default_op;
		c0 = info->default_instc0;
		gboolean is_unsigned = FALSE;
		gboolean is_float = FALSE;
		switch (arg0_type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_U2:
		case MONO_TYPE_U4:
		case MONO_TYPE_U8:
		case MONO_TYPE_U:
			is_unsigned = TRUE;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			is_float = TRUE;
			break;
		}
		if (is_unsigned && info->unsigned_op != 0) {
			op = info->unsigned_op;
			c0 = info->unsigned_instc0;
		} else if (is_float && info->floating_op != 0) {
			op = info->floating_op;
			c0 = info->floating_instc0;
		}
	}
support_probe_complete:
	if (id == SN_get_IsSupported) {
		MonoInst *ins = NULL;
		EMIT_NEW_ICONST (cfg, ins, supported ? 1 : 0);
		if (cfg->verbose_level > 1)
			g_printf ("\t-> %s\n", supported ? "true" : " false");
		return ins;
	}
	if (!supported) {
		// Can't emit non-supported llvm intrinsics
		if (cfg->method != cmethod) {
			// Keep the original call so we end up in the intrinsic method
			return NULL;
		} else {
			// Emit an exception from the intrinsic method
			mono_emit_jit_icall (cfg, mono_throw_platform_not_supported, NULL);
			return NULL;
		}
	}
	if (op != 0)
		return emit_simd_ins_for_sig (cfg, klass, op, c0, arg0_type, fsig, args);
	return custom_emit (cfg, fsig, args, klass, intrin_group, info, id, arg0_type, is_64bit);
}

static MonoInst*
emit_vector_insert_element (
	MonoCompile* cfg, MonoClass* vklass, MonoInst* ins, MonoTypeEnum type, MonoInst* element, 
	int index, gboolean is_zero_inited)
{
	int op = type_to_insert_op (type);

	if (is_zero_inited && is_zero_const (element)) {
			// element already set to zero
#ifdef TARGET_ARM64
	} else if (!COMPILE_LLVM (cfg) && element->opcode == type_to_extract_op (type) && 
		(type == MONO_TYPE_R4 || type == MONO_TYPE_R8)) {
		// OP_INSERT_Ix inserts from GP reg, not SIMD. Cannot optimize for int types.
		ins = emit_simd_ins (cfg, vklass, op, ins->dreg, element->sreg1);
		ins->inst_c0 = index | ((element->inst_c0) << 8);
		ins->inst_c1 = type;
#endif
	} else {
		ins = emit_simd_ins (cfg, vklass, op, ins->dreg, element->dreg);
		ins->inst_c0 = index;
		ins->inst_c1 = type;
	}

	return ins;
}

static MonoInst *
emit_vector_create_elementwise (
	MonoCompile *cfg, MonoMethodSignature *fsig, MonoType *vtype,
	MonoTypeEnum type, MonoInst **args)
{
	MonoClass *vklass = mono_class_from_mono_type_internal (vtype);
	MonoInst *ins = emit_xzero (cfg, vklass);
	for (int i = 0; i < fsig->param_count; ++i)
		ins = emit_vector_insert_element (cfg, vklass, ins, type, args[i], i, TRUE);

	return ins;
}

#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_WASM)

static int
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

static int
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

static int
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

static MonoClass *
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

static guint16 sri_vector_methods [] = {
	SN_Abs,
	SN_Add,
	SN_AndNot,
	SN_As,
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
	SN_BitwiseAnd,
	SN_BitwiseOr,
	SN_Ceiling,
	SN_ConditionalSelect,
	SN_ConvertToDouble,
	SN_ConvertToInt32,
	SN_ConvertToInt64,
	SN_ConvertToSingle,
	SN_ConvertToUInt32,
	SN_ConvertToUInt64,
	SN_Create,
	SN_CreateScalar,
	SN_CreateScalarUnsafe,
	SN_Divide,
	SN_Dot,
	SN_Equals,
	SN_EqualsAll,
	SN_EqualsAny,
	SN_ExtractMostSignificantBits,
	SN_Floor,
	SN_GetElement,
	SN_GetLower,
	SN_GetUpper,
	SN_GreaterThan,
	SN_GreaterThanAll,
	SN_GreaterThanAny,
	SN_GreaterThanOrEqual,
	SN_GreaterThanOrEqualAll,
	SN_GreaterThanOrEqualAny,
	SN_LessThan,
	SN_LessThanAll,
	SN_LessThanAny,
	SN_LessThanOrEqual,
	SN_LessThanOrEqualAll,
	SN_LessThanOrEqualAny,
	SN_Max,
	SN_Min,
	SN_Multiply,
	SN_Narrow,
	SN_Negate,
	SN_OnesComplement,
	SN_Shuffle,
	SN_Sqrt,
	SN_Subtract,
	SN_Sum,
	SN_ToScalar,
	SN_ToVector128,
	SN_ToVector128Unsafe,
	SN_WidenLower,
	SN_WidenUpper,
	SN_WithElement,
	SN_WithLower,
	SN_WithUpper,
	SN_Xor,
	SN_get_IsHardwareAccelerated,
};

static gboolean
is_elementwise_ctor (MonoMethodSignature *fsig, MonoType *etype)
{
	if (fsig->param_count < 1)
		return FALSE;
	for (int i = 0; i < fsig->param_count; ++i)
		if (!mono_metadata_type_equal (etype, fsig->params [i]))
			return FALSE;
	return TRUE;
}

static gboolean
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

static gboolean
is_create_from_half_vectors_overload (MonoMethodSignature *fsig)
{
	if (fsig->param_count != 2) return FALSE;
	if (!is_intrinsics_vector_type (fsig->params [0])) return FALSE;
	return mono_metadata_type_equal (fsig->params [0], fsig->params [1]);
}

static gboolean
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

#ifdef TARGET_ARM64
static MonoInst*
emit_msb_vector_mask (MonoCompile *cfg, MonoClass *arg_class, MonoTypeEnum arg_type)
{
	guint64 msb_mask_value[2];
	// TODO: with mini, one can emit movi to achieve broadcasting immediate i8/i16/i32

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

static MonoInst*
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

/*
 * Emit intrinsics in System.Numerics.Vector and System.Runtime.Intrinsics.Vector64/128/256/512.
 * If the intrinsic is not supported for some reasons, return NULL, and fall back to the c#
 * implementation.
 */
static MonoInst*
emit_sri_vector (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{	
	int id = lookup_intrins (sri_vector_methods, sizeof (sri_vector_methods), cmethod);
	if (id == -1) {
		//check_no_intrinsic_cattr (cmethod);
		return NULL;
	}

	int vector_size;
	if (!strcmp (m_class_get_name (cmethod->klass), "Vector64"))
		vector_size = 64;
	else if (!strcmp (m_class_get_name (cmethod->klass), "Vector128"))
		vector_size = 128;
	else if (!strcmp (m_class_get_name (cmethod->klass), "Vector256"))
		vector_size = 256;
	else if (!strcmp (m_class_get_name (cmethod->klass), "Vector512"))
		vector_size = 512;
	else if (!strcmp (m_class_get_name (cmethod->klass), "Vector"))
		vector_size = register_size * 8;
	else
		return NULL;

	if (vector_size == 256 || vector_size == 512)
		return NULL;

// FIXME: This limitation could be removed once everything here are supported by mini JIT on arm64
#ifdef TARGET_ARM64
	if (!COMPILE_LLVM (cfg)) {
		if (vector_size != 128)
			return NULL;
		}
#endif

#ifdef TARGET_WASM
	g_assert (COMPILE_LLVM (cfg));
	if (vector_size != 128)
		return NULL;
#endif

#ifdef TARGET_AMD64
	if (!COMPILE_LLVM (cfg)) {
		if (vector_size != 128)
			return NULL;
		if (!is_SIMD_feature_supported (cfg, MONO_CPU_X86_SSE41))
			/* Some opcodes like pextrd require sse41 */
			return NULL;
	}
	if (vector_size != 128)
		return NULL;
#endif

	MonoClass* klass = fsig->param_count > 0 ? args[0]->klass : cmethod->klass;
	MonoTypeEnum arg0_type = fsig->param_count > 0 ? get_underlying_type (fsig->params [0]) : MONO_TYPE_VOID;

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	switch (id) {
	case SN_get_IsHardwareAccelerated: {
		MonoInst* ins;
		EMIT_NEW_ICONST (cfg, ins, 1);
		return ins;
	}
	case SN_Abs: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		if (type_enum_is_unsigned (arg0_type))
			return NULL;
#ifdef TARGET_ARM64
		int iid = type_enum_is_float (arg0_type) ? INTRINS_AARCH64_ADV_SIMD_FABS : INTRINS_AARCH64_ADV_SIMD_ABS;
		return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X, iid, arg0_type, fsig, args);
#elif defined(TARGET_AMD64)
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		if (type_enum_is_float(arg0_type)) {
			// args [0] & ~vector(-0.0)
			MonoInst *zero = emit_xzero(cfg, arg_class);	// 0.0
			zero = emit_simd_ins (cfg, klass, OP_NEGATION, zero->dreg, -1); // -0.0
			zero->inst_c1 = arg0_type;
			MonoInst *ins = emit_simd_ins (cfg, klass, OP_VECTOR_ANDN, zero->dreg, args [0]->dreg);
			ins->inst_c1 = arg0_type;
			return ins;
		} else {
			if (COMPILE_LLVM (cfg))
				return emit_simd_ins_for_sig (cfg, klass, OP_VECTOR_IABS, -1, arg0_type, fsig, args);
			
			// SSSE3 does not support i64
			if (is_SIMD_feature_supported (cfg, MONO_CPU_X86_SSSE3) && 
				!(arg0_type == MONO_TYPE_I8 || (TARGET_SIZEOF_VOID_P == 8 && arg0_type == MONO_TYPE_I)))
				return emit_simd_ins_for_sig (cfg, klass, OP_VECTOR_IABS, -1, arg0_type, fsig, args);

			MonoInst *zero = emit_xzero (cfg, klass);
			MonoInst *neg = emit_simd_ins (cfg, klass, OP_XBINOP, zero->dreg, args [0]->dreg);
			neg->inst_c0 = OP_ISUB;
			neg->inst_c1 = arg0_type;
			
			MonoInst *ins = emit_simd_ins (cfg, klass, OP_XBINOP, args [0]->dreg, neg->dreg);
			ins->inst_c0 = OP_IMAX;
			ins->inst_c1 = arg0_type;
			return ins;
		}
#elif defined(TARGET_WASM)
		if (type_enum_is_float(arg0_type)) {
			return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X, arg0_type == MONO_TYPE_R8 ? INTRINS_WASM_FABS_V2 : INTRINS_WASM_FABS_V4, -1, fsig, args);
		} else {
			return emit_simd_ins_for_sig (cfg, klass, OP_VECTOR_IABS, -1, arg0_type, fsig, args);
		}
#else
		return NULL;
#endif
	}
	case SN_Add:
	case SN_BitwiseAnd:
	case SN_BitwiseOr:
	case SN_Divide:
	case SN_Max:
	case SN_Min:
	case SN_Multiply:
	case SN_Subtract:
	case SN_Xor:
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		return emit_simd_ins_for_binary_op (cfg, klass, fsig, args, arg0_type, id);
	case SN_AndNot: {
		if (!is_element_type_primitive (fsig->params [0])) 
			return NULL;
#ifdef TARGET_ARM64
		return emit_simd_ins_for_sig (cfg, klass, OP_ARM64_BIC, -1, arg0_type, fsig, args);
#elif defined(TARGET_AMD64) || defined(TARGET_WASM)
		/* Swap lhs and rhs because Vector128 needs lhs & !rhs
		   whereas SSE2 does !lhs & rhs */
		MonoInst *tmp = args[0];
		args[0] = args[1];
		args[1] = tmp;

		return emit_simd_ins_for_sig (cfg, klass, OP_VECTOR_ANDN, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case SN_As:
	case SN_AsByte:
	case SN_AsDouble:
	case SN_AsInt16:
	case SN_AsInt32:
	case SN_AsInt64:
	case SN_AsSByte:
	case SN_AsSingle:
	case SN_AsUInt16:
	case SN_AsUInt32:
	case SN_AsUInt64: {
		if (!is_element_type_primitive (fsig->ret) || !is_element_type_primitive (fsig->params [0]))
			return NULL;
		return emit_simd_ins (cfg, klass, OP_XCAST, args [0]->dreg, -1);
	}
	case SN_Ceiling:
	case SN_Floor: {
		if (!type_enum_is_float (arg0_type))
			return NULL;
#ifdef TARGET_ARM64
		int ceil_or_floor = id == SN_Ceiling ? INTRINS_SIMD_CEIL : INTRINS_SIMD_FLOOR;
		return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X, ceil_or_floor, arg0_type, fsig, args);
#elif defined(TARGET_AMD64)
		if (!is_SIMD_feature_supported (cfg, MONO_CPU_X86_SSE41))
			return NULL;

		int ceil_or_floor = id == SN_Ceiling ? 10 : 9;
		return emit_simd_ins_for_sig (cfg, klass, OP_SSE41_ROUNDP, ceil_or_floor, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case SN_ConditionalSelect: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

#if defined(TARGET_ARM64) || defined(TARGET_AMD64) || defined(TARGET_WASM)

#if defined(TARGET_AMD64)
		if (!COMPILE_LLVM (cfg)) {
			MonoInst *val1 = emit_simd_ins (cfg, klass, OP_XBINOP_FORCEINT, args [0]->dreg, args [1]->dreg);
			val1->inst_c0 = XBINOP_FORCEINT_AND;
			MonoInst *val2 = emit_simd_ins (cfg, klass, OP_VECTOR_ANDN, args [0]->dreg, args [2]->dreg);
			MonoInst *ins = emit_simd_ins (cfg, klass, OP_XBINOP_FORCEINT, val1->dreg, val2->dreg);
			ins->inst_c0 = XBINOP_FORCEINT_OR;
			return ins;
		}
#endif

		return emit_simd_ins_for_sig (cfg, klass, OP_BSL, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case SN_ConvertToDouble: {
		if ((arg0_type != MONO_TYPE_I8) && (arg0_type != MONO_TYPE_U8))
			return NULL;
#if defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg)) {
			return emit_simd_ins_for_sig (cfg, klass, OP_XUNOP, 
				arg0_type == MONO_TYPE_I8 ? OP_CVT_SI_FP : OP_CVT_UI_FP, 
				MONO_TYPE_R8, fsig, args);
		}
#endif
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		int size = mono_class_value_size (arg_class, NULL);
		int op = -1;
		if (size == 8)
			op = arg0_type == MONO_TYPE_I8 ? OP_CVT_SI_FP_SCALAR : OP_CVT_UI_FP_SCALAR;
		else
			op = arg0_type == MONO_TYPE_I8 ? OP_CVT_SI_FP : OP_CVT_UI_FP;

#ifdef TARGET_AMD64
		// Fall back to the c# code
		if (!COMPILE_LLVM (cfg))
			return NULL;
#endif

		return emit_simd_ins_for_sig (cfg, klass, op, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case SN_ConvertToInt32: 
	case SN_ConvertToUInt32: {
		if (arg0_type != MONO_TYPE_R4)
			return NULL;
#if defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg)) {
			return emit_simd_ins_for_sig (cfg, klass, OP_XUNOP, 
				id == SN_ConvertToInt32 ? OP_CVT_FP_SI : OP_CVT_FP_UI, 
				id == SN_ConvertToInt32 ? MONO_TYPE_I4 : MONO_TYPE_U4, 
				fsig, args);
		}
#endif
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)

#if defined(TARGET_AMD64)
		if (!COMPILE_LLVM (cfg) && id == SN_ConvertToUInt32)
			// FIXME:
			return NULL;
#endif

		int op = id == SN_ConvertToInt32 ? OP_CVT_FP_SI : OP_CVT_FP_UI;
		return emit_simd_ins_for_sig (cfg, klass, op, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case SN_ConvertToInt64:
	case SN_ConvertToUInt64: {
		if (arg0_type != MONO_TYPE_R8)
			return NULL;
#if defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg)) {
			return emit_simd_ins_for_sig (cfg, klass, OP_XUNOP, 
				id == SN_ConvertToInt64 ? OP_CVT_FP_SI : OP_CVT_FP_UI, 
				id == SN_ConvertToInt64 ? MONO_TYPE_I8 : MONO_TYPE_U8, 
				fsig, args);
		}
#endif
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		int size = mono_class_value_size (arg_class, NULL);
		int op = -1;
		if (id == SN_ConvertToInt64)
			op = size == 8 ? OP_CVT_FP_SI_SCALAR : OP_CVT_FP_SI;
		else
			op = size == 8 ? OP_CVT_FP_UI_SCALAR : OP_CVT_FP_UI;

#if defined(TARGET_AMD64)
		if (!COMPILE_LLVM (cfg))
			// FIXME:
			return NULL;
#endif

		return emit_simd_ins_for_sig (cfg, klass, op, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case SN_ConvertToSingle: {
		if ((arg0_type != MONO_TYPE_I4) && (arg0_type != MONO_TYPE_U4))
			return NULL;
#if defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg)) {
			return emit_simd_ins_for_sig (cfg, klass, OP_XUNOP, 
				arg0_type == MONO_TYPE_I4 ? OP_CVT_SI_FP : OP_CVT_UI_FP, 
				MONO_TYPE_R4, fsig, args);
		}
#endif
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
		int op = arg0_type == MONO_TYPE_I4 ? OP_CVT_SI_FP : OP_CVT_UI_FP;

#if defined(TARGET_AMD64)
		if (!COMPILE_LLVM (cfg) && op == OP_CVT_UI_FP)
			// FIXME:
			return NULL;
#endif

		return emit_simd_ins_for_sig (cfg, klass, op, -1, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case SN_Create: {
		MonoType *etype = get_vector_t_elem_type (fsig->ret);
		if (!MONO_TYPE_IS_VECTOR_PRIMITIVE (etype))
			return NULL;
		if (fsig->param_count == 1 && mono_metadata_type_equal (fsig->params [0], etype)) {
			MonoInst* ins = emit_simd_ins (cfg, klass, type_to_expand_op (etype->type), args [0]->dreg, -1);
			ins->inst_c1 = arg0_type;
			return ins;
		} else if (is_create_from_half_vectors_overload (fsig)) {
#if defined(TARGET_AMD64)
			// Require Vector64 SIMD support
			if (!COMPILE_LLVM (cfg))
				return NULL;
#endif
			if (COMPILE_LLVM (cfg)) {
				/*
				 * The sregs are half size, and the dreg is full size
				 * which can cause problems if mono_handle_global_vregs () is trying to
				 * spill them since it uses ins->klass to create the variable.
				 * So create variables for them here.
				 * This is not a problem for the JIT since that only has 1 simd type right now.
				 */
				mono_compile_create_var_for_vreg (cfg, fsig->params [0], OP_LOCAL, args [0]->dreg);
				mono_compile_create_var_for_vreg (cfg, fsig->params [1], OP_LOCAL, args [1]->dreg);
			}

			return emit_simd_ins (cfg, klass, OP_XCONCAT, args [0]->dreg, args [1]->dreg);
		}
		else if (is_elementwise_create_overload (fsig, etype))
			return emit_vector_create_elementwise (cfg, fsig, fsig->ret, arg0_type, args);
		break;
	}
	case SN_CreateScalar:
	case SN_CreateScalarUnsafe: {
		MonoType *etype = get_vector_t_elem_type (fsig->ret);
		if (!MONO_TYPE_IS_VECTOR_PRIMITIVE (etype))
			return NULL;
		gboolean is_unsafe = id == SN_CreateScalarUnsafe;
		if (COMPILE_LLVM (cfg)) {
			return emit_simd_ins_for_sig (cfg, klass, is_unsafe ? OP_CREATE_SCALAR_UNSAFE : OP_CREATE_SCALAR, -1, arg0_type, fsig, args);
		} else {
#ifdef TARGET_AMD64
			MonoInst *ins;

			ins = emit_xzero (cfg, klass);
			if (!is_zero_const (args [0])) {
				ins = emit_simd_ins (cfg, klass, type_to_insert_op (arg0_type), ins->dreg, args [0]->dreg);
				ins->inst_c0 = 0;
				ins->inst_c1 = arg0_type;
			}
			return ins;
#else
			if (type_enum_is_float (arg0_type)) {
				return emit_simd_ins_for_sig (cfg, klass, is_unsafe ? OP_CREATE_SCALAR_UNSAFE_FLOAT : OP_CREATE_SCALAR_FLOAT, -1, arg0_type, fsig, args);
			} else {
				return emit_simd_ins_for_sig (cfg, klass, is_unsafe ? OP_CREATE_SCALAR_UNSAFE_INT : OP_CREATE_SCALAR_INT, -1, arg0_type, fsig, args);
			}
#endif
		}
	}
	case SN_Dot: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
#if defined(TARGET_WASM)
		if (!COMPILE_LLVM (cfg) && (arg0_type == MONO_TYPE_I8 || arg0_type == MONO_TYPE_U8))
			return NULL;
#elif defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg) && (arg0_type == MONO_TYPE_I8 || arg0_type == MONO_TYPE_U8 || arg0_type == MONO_TYPE_I || arg0_type == MONO_TYPE_U))
			return NULL;
#endif

#if defined(TARGET_ARM64) || defined(TARGET_WASM)
		int instc0 = type_enum_is_float (arg0_type) ? OP_FMUL : OP_IMUL;
		MonoInst *pairwise_multiply = emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, instc0, arg0_type, fsig, args);
		return emit_sum_vector (cfg, fsig->params [0], arg0_type, pairwise_multiply);
#elif defined(TARGET_AMD64)
		int instc =-1;
		if (type_enum_is_float (arg0_type)) {
			if (is_SIMD_feature_supported (cfg, MONO_CPU_X86_SSE41)) {
				int mask_val = -1;
				switch (arg0_type) {
				case MONO_TYPE_R4:
					instc = COMPILE_LLVM (cfg) ? OP_SSE41_DPPS : OP_SSE41_DPPS_IMM;
					mask_val = 0xf1; // 0xf1 ... 0b11110001
					break;
				case MONO_TYPE_R8:
					instc = COMPILE_LLVM (cfg) ? OP_SSE41_DPPD : OP_SSE41_DPPD_IMM;
					mask_val = 0x31; // 0x31 ... 0b00110001
					break;
				default:
					return NULL;
				}	

				MonoInst *dot;
				if (COMPILE_LLVM (cfg)) {
					int mask_reg = alloc_ireg (cfg);
					MONO_EMIT_NEW_ICONST (cfg, mask_reg, mask_val);

					dot = emit_simd_ins (cfg, klass, instc, args [0]->dreg, args [1]->dreg);
					dot->sreg3 = mask_reg;
				} else {
					dot = emit_simd_ins (cfg, klass, instc, args [0]->dreg, args [1]->dreg);
					dot->inst_c0 = mask_val;
				}

				return extract_first_element (cfg, klass, arg0_type, dot->dreg);
			} else {
				instc = OP_FMUL;
			}	
		} else {
			if (arg0_type == MONO_TYPE_I1 || arg0_type == MONO_TYPE_U1)
				return NULL; 	// We don't support sum vector for byte, sbyte types yet

			// FIXME:
			if (!COMPILE_LLVM (cfg))
				return NULL;

			instc = OP_IMUL;
		}
		MonoInst *pairwise_multiply = emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, instc, arg0_type, fsig, args);

		return emit_sum_vector (cfg, fsig->params [0], arg0_type, pairwise_multiply);
#else
		return NULL;
#endif
	}
	case SN_Equals:
	case SN_EqualsAll:
	case SN_EqualsAny: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		if (id == SN_Equals)
			return emit_xcompare (cfg, klass, arg0_type, args [0], args [1]);

		if (COMPILE_LLVM (cfg)) {
			switch (id) {
				case SN_EqualsAll:
					return emit_xequal (cfg, arg_class, arg0_type, args [0], args [1]);
				case SN_EqualsAny: {
					MonoInst *cmp_eq = emit_xcompare (cfg, arg_class, arg0_type, args [0], args [1]);
					MonoInst *zero = emit_xzero (cfg, arg_class);
					return emit_not_xequal (cfg, arg_class, arg0_type, cmp_eq, zero);
				}
			}
		} else {
			MonoInst* cmp = emit_xcompare (cfg, arg_class, arg0_type, args [0], args [1]);
			MonoInst* ret = emit_simd_ins (cfg, mono_defaults.boolean_class, OP_XEXTRACT, cmp->dreg, -1);
			ret->inst_c0 = (id == SN_EqualsAll) ? SIMD_EXTR_ARE_ALL_SET : SIMD_EXTR_IS_ANY_SET;
			ret->inst_c1 = mono_class_value_size (klass, NULL);
			return ret;
		}
		g_assert_not_reached ();
	}
	case SN_ExtractMostSignificantBits: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
#ifdef TARGET_WASM
		if (type_enum_is_float (arg0_type))
			return NULL;

		return emit_simd_ins_for_sig (cfg, klass, OP_WASM_SIMD_BITMASK, -1, -1, fsig, args);
#elif defined(TARGET_ARM64)
		MonoClass* arg_class;
		if (type_enum_is_float (arg0_type)) {
			MonoClass* cast_class;
			if (arg0_type == MONO_TYPE_R4) {
				arg0_type = MONO_TYPE_I4;
				cast_class = mono_defaults.int32_class;
			} else {
				arg0_type = MONO_TYPE_I8;
				cast_class = mono_defaults.int64_class;
			}
			arg_class = create_class_instance ("System.Runtime.Intrinsics", m_class_get_name (klass), m_class_get_byval_arg (cast_class));
		} else {
			arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		}
		
		// FIXME: Add support for Vector64 on arm64 https://github.com/dotnet/runtime/issues/90402
		int size = mono_class_value_size (arg_class, NULL);
		if (size != 16)
			return NULL;

		MonoInst* msb_mask_vec = emit_msb_vector_mask (cfg, arg_class, arg0_type);
		MonoInst* and_res_vec = emit_simd_ins_for_binary_op (cfg, arg_class, fsig, args, arg0_type, SN_BitwiseAnd);
		and_res_vec->sreg2 = msb_mask_vec->dreg;

		MonoInst* msb_shift_vec = emit_msb_shift_vector_constant (cfg, arg_class, arg0_type);

		MonoInst* shift_res_vec = emit_simd_ins (cfg, arg_class, OP_XOP_OVR_X_X_X, and_res_vec->dreg, msb_shift_vec->dreg);
		shift_res_vec->inst_c0 = INTRINS_AARCH64_ADV_SIMD_USHL;
		shift_res_vec->inst_c1 = arg0_type;

		MonoInst* result_ins = NULL;
		if (arg0_type == MONO_TYPE_I1 || arg0_type == MONO_TYPE_U1) {
			// Always perform unsigned operations as vector sum and extract operations could sign-extend the result into the GP register
			// making the final result invalid. This is not needed for wider type as the maximum sum of extracted MSB cannot be larger than 8bits
			arg0_type = MONO_TYPE_U1;

			MonoInst* ext_low_vec = emit_simd_ins_for_sig (cfg, arg_class, OP_XLOWER, 8, arg0_type, fsig, &shift_res_vec);
			MonoInst* sum_low_vec = emit_sum_vector (cfg, fsig->params [0], arg0_type, ext_low_vec);
			
			MonoInst* ext_high_vec = emit_simd_ins_for_sig (cfg, arg_class, OP_XUPPER, 8, arg0_type, fsig, &shift_res_vec);
			MonoInst* sum_high_vec = emit_sum_vector (cfg, fsig->params [0], arg0_type, ext_high_vec);			
			
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, sum_high_vec->dreg, sum_high_vec->dreg, 8);
			EMIT_NEW_BIALU (cfg, result_ins, OP_IOR, sum_high_vec->dreg, sum_high_vec->dreg, sum_low_vec->dreg);
		} else {
			result_ins = emit_sum_vector (cfg, fsig->params [0], arg0_type, shift_res_vec);
		}
		return result_ins;
#elif defined(TARGET_AMD64)
		int type = MONO_TYPE_I1;

		switch (arg0_type) {
			case MONO_TYPE_U2:
			case MONO_TYPE_I2: {
				if (!is_SIMD_feature_supported (cfg, MONO_CPU_X86_SSSE3)) 
					return NULL;
					
				type = type_enum_is_unsigned (arg0_type) ? MONO_TYPE_U1 : MONO_TYPE_I1;
				MonoClass* arg_class = mono_class_from_mono_type_internal (fsig->params [0]);

				guint64 shuffle_mask[2];
				shuffle_mask[0] = 0x0F0D0B0907050301; // Place odd bytes in the lower half of vector
				shuffle_mask[1] = 0x8080808080808080; // Zero the upper half

				MonoInst* shuffle_vec = emit_xconst_v128 (cfg, arg_class, (guint8*)shuffle_mask);
				shuffle_vec->klass = arg_class;

				args [0] = emit_simd_ins (cfg, klass, OP_SSSE3_SHUFFLE, args [0]->dreg, shuffle_vec->dreg);
				args [0]->inst_c1 = type;
				break;
			}
#if TARGET_SIZEOF_VOID_P == 4
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_U4:
			case MONO_TYPE_I4:
			case MONO_TYPE_R4: {
				type = MONO_TYPE_R4;
				break;
			}
#if TARGET_SIZEOF_VOID_P == 8
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_U8:
			case MONO_TYPE_I8:
			case MONO_TYPE_R8: {
				type = MONO_TYPE_R8;
				break;
			}
		}

		return emit_simd_ins_for_sig (cfg, klass, OP_SSE_MOVMSK, -1, type, fsig, args);
#endif
	}
	case SN_GetElement: {
		int elems;

		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);

		if (fsig->params [0]->type == MONO_TYPE_GENERICINST) {
			MonoType *etype = mono_class_get_context (arg_class)->class_inst->type_argv [0];
			int size = mono_class_value_size (arg_class, NULL);
			int esize = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
			elems = size / esize;
		} else {
			// This exists to handle the static extension methods for Vector2/3/4, Quaternion, and Plane
			// which live on System.Numerics.Vector

			arg0_type = MONO_TYPE_R4;
			elems = 4;
		}

		if (args [1]->opcode == OP_ICONST) {
			// If the index is provably a constant, we can generate vastly better code.
			int index = GTMREG_TO_INT (args[1]->inst_c0);

			if (index < 0 || index >= elems) {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, elems);
				MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");
			} 

			// Bounds check is elided if we know the index is safe.
			int extract_op = type_to_extract_op (arg0_type);
			MonoInst* ret = emit_simd_ins (cfg, args [0]->klass, extract_op, args [0]->dreg, -1);
			ret->inst_c0 = index;
			ret->inst_c1 = fsig->ret->type;
			return ret;
		}

		// Bounds check needed in non-const case.
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, elems);
		MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");

		gboolean use_xextract;
#ifdef TARGET_AMD64
		use_xextract = FALSE;
#else
		use_xextract = type_to_width_log2 (arg0_type) == 3;
#endif

		if (COMPILE_LLVM (cfg) || use_xextract) {
			// Use optimized paths for 64-bit extractions or whatever LLVM yields if enabled.
			int extract_op = type_to_xextract_op (arg0_type);
			return emit_simd_ins_for_sig (cfg, klass, extract_op, -1, arg0_type, fsig, args);
		} else {
			// Spill the vector reg.
			// Load back from spilled + index << elem_size_log2
			// TODO: on x86, use a LEA
			MonoInst* spilled;
			NEW_VARLOADA_VREG (cfg, spilled, args [0]->dreg, fsig->params [0]);
			MONO_ADD_INS (cfg->cbb, spilled);
			int offset_reg = alloc_lreg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, offset_reg, args [1]->dreg, type_to_width_log2 (arg0_type));
			int addr_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_BIALU(cfg, OP_PADD, addr_reg, spilled->dreg, offset_reg);
			MonoInst* ret;
			int dreg = arg0_type == MONO_TYPE_R4 ? alloc_freg (cfg) : alloc_ireg (cfg);
			NEW_LOAD_MEMBASE (cfg, ret, mono_type_to_load_membase (cfg, fsig->ret), dreg, addr_reg, 0);
			MONO_ADD_INS (cfg->cbb, ret);
			return ret;
		}
		break;
	}
	case SN_GetLower:
	case SN_GetUpper: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		int op = id == SN_GetLower ? OP_XLOWER : OP_XUPPER;

#ifdef TARGET_AMD64
		if (!COMPILE_LLVM (cfg))
		  /* These return a Vector64 */
			return NULL;
#endif
		return emit_simd_ins_for_sig (cfg, klass, op, 0, arg0_type, fsig, args);
	}
	case SN_GreaterThan:
	case SN_GreaterThanOrEqual:
	case SN_LessThan:
	case SN_LessThanOrEqual: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

		return emit_xcompare_for_intrinsic (cfg, klass, id, arg0_type, args [0], args [1]);
	}
	case SN_GreaterThanAll:
	case SN_GreaterThanAny:
	case SN_GreaterThanOrEqualAll:
	case SN_GreaterThanOrEqualAny:
	case SN_LessThanAll:
	case SN_LessThanAny:
	case SN_LessThanOrEqualAll:
	case SN_LessThanOrEqualAny: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

		g_assert (fsig->param_count == 2 &&
			fsig->ret->type == MONO_TYPE_BOOLEAN &&
			mono_metadata_type_equal (fsig->params [0], fsig->params [1]));

		gboolean is_all = FALSE;
		switch (id) {
		case SN_GreaterThanAll:
		case SN_GreaterThanOrEqualAll:
		case SN_LessThanAll:
		case SN_LessThanOrEqualAll: 
			is_all = TRUE;
			break;
		}

		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		if (COMPILE_LLVM (cfg)) {
			MonoInst *cmp = emit_xcompare_for_intrinsic (cfg, klass, id, arg0_type, args [0], args [1]);
			if (is_all) {
				// for floating point numbers all ones is NaN and so
				// they must be treated differently than integer types
				if (type_enum_is_float (arg0_type)) {
					MonoInst *zero = emit_xzero (cfg, arg_class);
					MonoInst *inverted_cmp = emit_xcompare (cfg, klass, arg0_type, cmp, zero);
					return emit_xequal (cfg, arg_class, arg0_type, inverted_cmp, zero);
				}

				MonoInst *ones = emit_xones (cfg, arg_class);
				return emit_xequal (cfg, arg_class, arg0_type, cmp, ones);
			} else {
				MonoInst *zero = emit_xzero (cfg, arg_class);
				return emit_not_xequal (cfg, arg_class, arg0_type, cmp, zero);
			}
		} else {
			MonoInst* cmp = emit_xcompare_for_intrinsic (cfg, arg_class, id, arg0_type, args [0], args [1]);
			MonoInst* ret = emit_simd_ins (cfg, mono_defaults.boolean_class, OP_XEXTRACT, cmp->dreg, -1);
			ret->inst_c0 = is_all ? SIMD_EXTR_ARE_ALL_SET : SIMD_EXTR_IS_ANY_SET;
			ret->inst_c1 = mono_class_value_size (klass, NULL);
			return ret;
		}
	}
	case SN_Narrow: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

#ifdef TARGET_ARM64
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		int size = mono_class_value_size (arg_class, NULL);

		if (size == 16) {
			switch (arg0_type) {
			case MONO_TYPE_R8: {
				MonoInst* ins = emit_simd_ins (cfg, arg_class, OP_ARM64_FCVTN, args [0]->dreg, -1);
				ins->inst_c1 = arg0_type;
				MonoInst* ret = emit_simd_ins (cfg, arg_class, OP_ARM64_FCVTN2, ins->dreg, args [1]->dreg);
				ret->inst_c1 = arg0_type;
				return ret;
			}
			case MONO_TYPE_I2:
			case MONO_TYPE_I4:
			case MONO_TYPE_I8:
			case MONO_TYPE_U2:
			case MONO_TYPE_U4:
			case MONO_TYPE_U8: {
				MonoInst* ins = emit_simd_ins (cfg, arg_class, OP_ARM64_XTN, args [0]->dreg, -1);
				ins->inst_c1 = arg0_type;
				MonoInst* ret = emit_simd_ins (cfg, arg_class, OP_ARM64_XTN2, ins->dreg, args [1]->dreg);
				ret->inst_c1 = arg0_type;
				return ret;
			}
			default:
				return NULL;
			}
		} else {
			if (!COMPILE_LLVM (cfg))
				return NULL;

			switch (arg0_type) {
			case MONO_TYPE_R8: {
				//Widen arg0
				MonoInst *ins1 = emit_simd_ins (cfg, arg_class, OP_XWIDEN_UNSAFE, args [0]->dreg, -1);

				//Insert arg1 to arg0
				int tmp = alloc_ireg (cfg);
				MONO_EMIT_NEW_ICONST (cfg, tmp, 1);
				MonoInst *ins2 = emit_simd_ins (cfg, arg_class, OP_EXTRACT_R8, args [1]->dreg, -1);
				ins2->inst_c0 = 0;
				ins2->inst_c1 = arg0_type;

				MonoType* param_type = get_vector_t_elem_type (fsig->params[0]);
				MonoClass *ivector128_inst = create_class_instance ("System.Runtime.Intrinsics", "Vector128`1", param_type);

				ins1 = emit_simd_ins (cfg, ivector128_inst, OP_XINSERT_R8, ins1->dreg, ins2->dreg);
				ins1->sreg3 = tmp;
				ins1->inst_c1 = arg0_type;

				//ConvertToSingleLower
				return emit_simd_ins (cfg, arg_class, OP_ARM64_FCVTN, ins1->dreg, -1);
			}
			case MONO_TYPE_I2:
			case MONO_TYPE_I4:
			case MONO_TYPE_I8:
			case MONO_TYPE_U2:
			case MONO_TYPE_U4:
			case MONO_TYPE_U8: {
				//Widen arg0
				MonoInst *arg0 = emit_simd_ins (cfg, arg_class, OP_XWIDEN_UNSAFE, args [0]->dreg, -1);

				//Cast arg0 and arg1 to u/int64
				MonoType *type_new;
				MonoTypeEnum type_enum_new;
				if (type_enum_is_unsigned (arg0_type)) {
					type_new = m_class_get_byval_arg (mono_defaults.uint64_class);
					type_enum_new = MONO_TYPE_U8;
				} else {
					type_new = m_class_get_byval_arg (mono_defaults.int64_class);
					type_enum_new = MONO_TYPE_I8;
				}
				MonoClass *ivector128_64_inst = create_class_instance ("System.Runtime.Intrinsics", "Vector128`1", type_new);
				arg0 = emit_simd_ins (cfg, ivector128_64_inst, OP_XCAST, arg0->dreg, -1);
				MonoClass *ivector64_64_inst = create_class_instance ("System.Runtime.Intrinsics", "Vector64`1", type_new);
				MonoInst *arg1 = emit_simd_ins (cfg, ivector64_64_inst, OP_XCAST, args [1]->dreg, -1);

				//Insert arg1 to arg0
				int tmp = alloc_ireg (cfg);
				MONO_EMIT_NEW_ICONST (cfg, tmp, 1);
				arg1 = emit_simd_ins (cfg, ivector64_64_inst, OP_EXTRACT_I8, arg1->dreg, -1);
				arg1->inst_c0 = 0;
				arg1->inst_c1 = type_enum_new;
				MonoType *param_type = get_vector_t_elem_type (fsig->params[0]);
				MonoClass *ivector128_inst = create_class_instance ("System.Runtime.Intrinsics", "Vector128`1", param_type);
				MonoInst *ins = emit_simd_ins (cfg, ivector128_64_inst, OP_XINSERT_I8, arg0->dreg, arg1->dreg);
				ins->sreg3 = tmp;
				ins->inst_c1 = type_enum_new;

				//Cast arg0 back to its original element type (arg0_type)
				ins = emit_simd_ins (cfg, ivector128_inst, OP_XCAST, ins->dreg, -1);

				//ExtractNarrowingLower
				return emit_simd_ins (cfg, ivector128_inst, OP_ARM64_XTN, ins->dreg, -1);
			}
			default:
				return NULL;
			}
		}
#elif defined(TARGET_WASM)
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		int size = mono_class_value_size (arg_class, NULL);

		if (size != 16)
			return NULL;

		switch (arg0_type) {
		case MONO_TYPE_I2:
		case MONO_TYPE_I4:
		case MONO_TYPE_I8:
		case MONO_TYPE_U2:
		case MONO_TYPE_U4:
		case MONO_TYPE_U8:
			return emit_simd_ins_for_sig (cfg, klass, OP_WASM_EXTRACT_NARROW, -1, -1, fsig, args);
		}

		return NULL;
#else
		return NULL;
#endif
	}
	case SN_Negate:
	case SN_OnesComplement: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		return emit_simd_ins_for_unary_op (cfg, klass, fsig, args, arg0_type, id);
	} 
	case SN_Shuffle: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
#ifdef TARGET_WASM
		return emit_simd_ins_for_sig (cfg, klass, OP_WASM_SIMD_SWIZZLE, -1, -1, fsig, args);
#elif defined(TARGET_ARM64)
		if (vector_size == 128 && (arg0_type == MONO_TYPE_I1 || arg0_type == MONO_TYPE_U1))
			return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_TBL1, 0, fsig, args);
		return NULL;
#elif defined(TARGET_AMD64)
		if (COMPILE_LLVM (cfg)) {
			if (is_SIMD_feature_supported (cfg, MONO_CPU_X86_SSSE3) && vector_size == 128 && (arg0_type == MONO_TYPE_I1 || arg0_type == MONO_TYPE_U1))
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PSHUFB, 0, fsig, args);
		}
		// There is no variable shuffle until avx512
		return NULL;
#else
		return NULL;
#endif
	}
	case SN_Sum: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
#if defined(TARGET_ARM64) || defined(TARGET_AMD64) || defined(TARGET_WASM)
		return emit_sum_vector (cfg, fsig->params [0], arg0_type, args [0]);
#else
		return NULL;
#endif
	}
	case SN_Sqrt: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		if (!type_enum_is_float (arg0_type))
			return NULL;
#ifdef TARGET_ARM64
		return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FSQRT, arg0_type, fsig, args);
#elif defined(TARGET_AMD64) || defined(TARGET_WASM)
		int instc0 = arg0_type == MONO_TYPE_R4 ? INTRINS_SIMD_SQRT_R4 : INTRINS_SIMD_SQRT_R8;

		return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X, instc0, arg0_type, fsig, args);
#else
		return NULL;
#endif
	}
	case SN_ToScalar: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		int extract_op = type_to_extract_op (arg0_type);
		return emit_simd_ins_for_sig (cfg, klass, extract_op, 0, arg0_type, fsig, args);
	}
	case SN_ToVector128:
	case SN_ToVector128Unsafe: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		int op = id == SN_ToVector128 ? OP_XWIDEN : OP_XWIDEN_UNSAFE;
		return emit_simd_ins_for_sig (cfg, klass, op, 0, arg0_type, fsig, args);
	}
	case SN_WithElement: {
		int elems;
		
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;

		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);

		if (fsig->params [0]->type == MONO_TYPE_GENERICINST) {
			MonoType *etype = mono_class_get_context (arg_class)->class_inst->type_argv [0];
			int size = mono_class_value_size (arg_class, NULL);
			int esize = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
			elems = size / esize;
		} else {
			// This exists to handle the static extension methods for Vector2/3/4, Quaternion, and Plane
			// which live on System.Numerics.Vector

			arg0_type = MONO_TYPE_R4;
			elems = 4;
		}

		if (args [1]->opcode == OP_ICONST) {
			// If the index is provably a constant, we can generate vastly better code.
			int index = GTMREG_TO_INT (args[1]->inst_c0);
			if (index < 0 || index >= elems) {
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, elems);
					MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");
			}

			return emit_vector_insert_element (cfg, klass, args [0], arg0_type, args [2], index, FALSE);
		} 

		if (!COMPILE_LLVM (cfg) && fsig->params [0]->type != MONO_TYPE_GENERICINST)
			return NULL;

		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, elems);
		MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");

		gboolean use_xextract;
#ifdef TARGET_AMD64
		use_xextract = FALSE;
#else
		use_xextract = type_to_width_log2 (arg0_type) == 3;
#endif

		if (COMPILE_LLVM (cfg) || use_xextract) {
			int insert_op = type_to_xinsert_op (arg0_type);
			MonoInst *ins = emit_simd_ins (cfg, klass, insert_op, args [0]->dreg, args [2]->dreg);
			ins->sreg3 = args [1]->dreg;
			ins->inst_c1 = arg0_type;
			return ins;
		} else {
			// Create a blank reg and spill it.
			// Overwrite memory with original value.
			// Overwrite [spilled + index << elem_size_log2] with replacement value
			// Read back.
			// TODO: on x86, use a LEA
			MonoInst* scratch = emit_xzero (cfg, args [0]->klass);
			MonoInst* scratcha, *ins;
			NEW_VARLOADA_VREG (cfg, scratcha, scratch->dreg, fsig->params [0]);
			MONO_ADD_INS (cfg->cbb, scratcha);
			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, fsig->params [0], scratcha->dreg, 0, args [0]->dreg);

			int offset_reg = alloc_lreg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, offset_reg, args [1]->dreg, type_to_width_log2 (arg0_type));
			int addr_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_BIALU(cfg, OP_PADD, addr_reg, scratcha->dreg, offset_reg);

			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, fsig->params [2], addr_reg, 0, args [2]->dreg);

			MonoInst* ret;
			NEW_LOAD_MEMBASE (cfg, ret, mono_type_to_load_membase (cfg, fsig->ret), scratch->dreg, scratcha->dreg, 0);
			MONO_ADD_INS (cfg->cbb, ret);

			return ret;
		}
		break;
	}
	case SN_WidenLower:
	case SN_WidenUpper: {
		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
#if defined(TARGET_ARM64)
		if (!COMPILE_LLVM (cfg)) {
			int subop = 0;
			gboolean is_upper = (id == SN_WidenUpper);
			if (type_enum_is_float (arg0_type))
				subop = is_upper ? OP_SIMD_FCVTL2 : OP_SIMD_FCVTL;
			else if (type_enum_is_unsigned (arg0_type))
				subop = is_upper ? OP_ARM64_UXTL2 : OP_ARM64_UXTL;
			else
				subop = is_upper ? OP_ARM64_SXTL2 : OP_ARM64_SXTL;
			
			MonoInst* ins = emit_simd_ins (cfg, klass, OP_XUNOP, args [0]->dreg, -1);
			ins->inst_c0 = subop;
			ins->inst_c1 = arg0_type;
			return ins;
		}
#endif
#if defined(TARGET_ARM64) || defined(TARGET_WASM)
		int op = id == SN_WidenLower ? OP_XLOWER : OP_XUPPER;
		MonoInst *lower_or_upper_half = emit_simd_ins_for_sig (cfg, klass, op, 0, arg0_type, fsig, args);
		if (type_enum_is_float (arg0_type)) {
			return emit_simd_ins (cfg, klass, OP_SIMD_FCVTL, lower_or_upper_half->dreg, -1);
		} else {
			int zero = alloc_ireg (cfg);
			MONO_EMIT_NEW_ICONST (cfg, zero, 0);
			op = type_enum_is_unsigned (arg0_type) ? OP_SIMD_USHLL : OP_SIMD_SSHLL;
			return emit_simd_ins (cfg, klass, op, lower_or_upper_half->dreg, zero);
		}
#elif defined(TARGET_AMD64)
		// FIXME:
		return NULL;
#else
		return NULL;
#endif
	}
	case SN_WithLower:
	case SN_WithUpper: {
#ifdef TARGET_AMD64
		if (!COMPILE_LLVM (cfg))
		  /* These return a Vector64 */
			return NULL;
#endif

		if (!is_element_type_primitive (fsig->params [0]))
			return NULL;
		int op = id == SN_WithLower ? OP_XINSERT_LOWER : OP_XINSERT_UPPER;
		return emit_simd_ins_for_sig (cfg, klass, op, 0, arg0_type, fsig, args);
	}
	default:
		break;
	}

	return NULL;
}

static guint16 sri_vector_t_methods [] = {
	SN_get_AllBitsSet,
	SN_get_Count,
	SN_get_IsSupported,
	SN_get_One,
	SN_get_Zero,
	SN_op_Addition,
	SN_op_BitwiseAnd,
	SN_op_BitwiseOr,
	SN_op_Division,
	SN_op_Equality,
	SN_op_ExclusiveOr,
	SN_op_Inequality,
	SN_op_Multiply,
	SN_op_OnesComplement,
	SN_op_Subtraction,
	SN_op_UnaryNegation,
	SN_op_UnaryPlus,
};

/* Emit intrinsics in System.Runtime.Intrinsics.Vector64<T>/128<T>/256<T>/512<T> */
static MonoInst*
emit_sri_vector_t (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	int id = lookup_intrins (sri_vector_t_methods, sizeof (sri_vector_t_methods), cmethod);
	if (id == -1) {
		//check_no_intrinsic_cattr (cmethod);
		return NULL;
	}

	MonoClass *klass = cmethod->klass;
	MonoType *etype = mono_class_get_context (klass)->class_inst->type_argv [0];

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	// Apart from filtering out non-primitive types this also filters out shared generic instance types like: T_BYTE which cannot be intrinsified
	if (!MONO_TYPE_IS_VECTOR_PRIMITIVE (etype)) {
		// Happens often in gshared code
		if (mini_type_get_underlying_type (etype)->type == MONO_TYPE_OBJECT) {
			if (id == SN_get_IsSupported) {
				MonoInst *ins = NULL;
				EMIT_NEW_ICONST (cfg, ins, 0);
				if (cfg->verbose_level > 1)
					printf ("  -> %d\n", (int)ins->inst_c0);
				return ins;
			}
		}

		return NULL;
	}

	int size = mono_class_value_size (klass, NULL);
	int esize = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
	g_assert (size > 0);
	g_assert (esize > 0);
	int len = size / esize;
	MonoTypeEnum arg0_type = fsig->param_count > 0 ? get_underlying_type (fsig->params [0]) : MONO_TYPE_VOID;

	// Support this even for unsupported instances/platforms to help dead code elimination
	if (id == SN_get_Count) {
		MonoInst *ins;
		EMIT_NEW_ICONST (cfg, ins, len);
		if (cfg->verbose_level > 1)
			printf ("  -> %d\n", len);
		return ins;
	}

	// Special case SN_get_IsSupported intrinsic handling in this function which verifies whether a type parameter T is supported for a generic vector.
	// As we got passed the MONO_TYPE_IS_VECTOR_PRIMITIVE check above, this should always return true for primitive types.

	if (id == SN_get_IsSupported) {
		MonoInst *ins = NULL;
		EMIT_NEW_ICONST (cfg, ins, 1);
		if (cfg->verbose_level > 1)
			printf ("  -> %d\n", (int)ins->inst_c0);
		return ins;
	}

	/* Vector256/Vector512 */
	if (size == 32 || size == 64)
		return NULL;

#if defined(TARGET_WASM)
	if (!COMPILE_LLVM (cfg))
		return NULL;
	if (size != 16)
		return NULL;
#endif

// FIXME: Support Vector64 for mini JIT on arm64
#ifdef TARGET_ARM64
	if (!COMPILE_LLVM (cfg) && (size != 16))
		return NULL;
#endif

#ifdef TARGET_AMD64
	if (!COMPILE_LLVM (cfg) && (size != 16))
		return NULL;
	if (size != 16)
		return NULL;
#endif

	switch (id) {
	case SN_get_Zero: {
		return emit_xzero (cfg, klass);
	}
	case SN_get_AllBitsSet: {
		return emit_xones (cfg, klass);
	}
	case SN_get_One: {
		guint64 buf [8];

		/* For Vector64, the upper elements are 0 */
		g_assert (sizeof (buf) >= size);
		memset (buf, 0, sizeof (buf));

		switch (etype->type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_U1: {
			guint8 *value = (guint8*)buf;

			for (int i = 0; i < len; ++i) {
				value [i] = 1;
			}

			return emit_xconst_v128 (cfg, klass, value);
		}
		case MONO_TYPE_I2:
		case MONO_TYPE_U2: {
			guint16 *value = (guint16*)buf;

			for (int i = 0; i < len; ++i) {
				value [i] = 1;
			}

			return emit_xconst_v128 (cfg, klass, (guint8*)value);
		}
#if TARGET_SIZEOF_VOID_P == 4
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#endif
		case MONO_TYPE_I4:
		case MONO_TYPE_U4: {
			guint32 *value = (guint32*)buf;

			for (int i = 0; i < len; ++i) {
				value [i] = 1;
			}

			return emit_xconst_v128 (cfg, klass, (guint8*)value);
		}
#if TARGET_SIZEOF_VOID_P == 8
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#endif
		case MONO_TYPE_I8:
		case MONO_TYPE_U8: {
			guint64 *value = (guint64*)buf;

			for (int i = 0; i < len; ++i) {
				value [i] = 1;
			}

			return emit_xconst_v128 (cfg, klass, (guint8*)value);
		}
		case MONO_TYPE_R4: {
			float *value = (float*)buf;

			for (int i = 0; i < len; ++i) {
				value [i] = 1.0f;
			}

			return emit_xconst_v128 (cfg, klass, (guint8*)value);
		}
		case MONO_TYPE_R8: {
			double *value = (double*)buf;

			for (int i = 0; i < len; ++i) {
				value [i] = 1.0;
			}

			return emit_xconst_v128 (cfg, klass, (guint8*)value);
		}
		default:
			g_assert_not_reached ();
		}
	}
	case SN_op_Addition:
	case SN_op_BitwiseAnd:
	case SN_op_BitwiseOr:
	case SN_op_Division:
	case SN_op_ExclusiveOr:
	case SN_op_Multiply:
	case SN_op_Subtraction: {
		if (fsig->param_count != 2 )
			return NULL;
		arg0_type = fsig->param_count > 0 ? get_underlying_type (fsig->params [0]) : MONO_TYPE_VOID;
		return emit_simd_ins_for_binary_op (cfg, klass, fsig, args, arg0_type, id);
		
	}
	case SN_op_Equality:
	case SN_op_Inequality: {
		if (fsig->param_count != 2 )
			return NULL;
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		switch (id) {
			case SN_op_Equality: return emit_xequal (cfg, arg_class, arg0_type, args [0], args [1]);
			case SN_op_Inequality: return emit_not_xequal (cfg, arg_class, arg0_type, args [0], args [1]);
			default: g_assert_not_reached ();
		}
	}
	case SN_op_OnesComplement:
	case SN_op_UnaryNegation:
		if (fsig->param_count != 1 )
			return NULL;
		return emit_simd_ins_for_unary_op (cfg, klass, fsig, args, arg0_type, id);
	case SN_op_UnaryPlus:
		if (fsig->param_count != 1)
			return NULL;
		return args [0];
	default:
		break;
	}

	return NULL;
}

// System.Numerics.Vector2/Vector3/Vector4, Quaternion, and Plane
static guint16 vector_2_3_4_methods[] = {
	SN_ctor,
	SN_Abs,
	SN_Add,
	SN_Clamp,
	SN_Conjugate,
	SN_CopyTo,
	SN_Distance,
	SN_DistanceSquared,
	SN_Divide,
	SN_Dot,
	SN_Length,
	SN_LengthSquared,
	SN_Lerp,
	SN_Max,
	SN_Min,
	SN_Multiply,
	SN_Negate,
	SN_Normalize,
	SN_SquareRoot,
	SN_Subtract,
	SN_get_Identity,
	SN_get_Item,
	SN_get_One,
	SN_get_UnitW,
	SN_get_UnitX,
	SN_get_UnitY,
	SN_get_UnitZ,
	SN_get_Zero,
	SN_op_Addition,
	SN_op_Division,
	SN_op_Equality,
	SN_op_Inequality,
	SN_op_Multiply,
	SN_op_Subtraction,
	SN_op_UnaryNegation,
	SN_set_Item,
};

static G_GNUC_UNUSED MonoInst*
emit_vector_2_3_4 (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins;
	int id, len;
	MonoClass *klass;
	MonoType *type, *etype;

	id = lookup_intrins (vector_2_3_4_methods, sizeof (vector_2_3_4_methods), cmethod);
	if (id == -1) {
		// https://github.com/dotnet/runtime/issues/81961
		// check_no_intrinsic_cattr (cmethod);
		return NULL;
	}

	klass = cmethod->klass;
	type = m_class_get_byval_arg (klass);
	etype = m_class_get_byval_arg (mono_defaults.single_class);
	len = mono_class_value_size (klass, NULL) / 4;

#ifndef TARGET_ARM64
	if (!COMPILE_LLVM (cfg))
		return NULL;
#endif

#ifdef TARGET_AMD64
	if (len != 4)
		return NULL;
#endif

#ifdef TARGET_WASM
	if (len != 4)
		return NULL;
#endif

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	// Similar to the cases in emit_sys_numerics_vector_t ()
	switch (id) {
	case SN_ctor:
		if (is_elementwise_ctor (fsig, etype)) {
			gboolean indirect = FALSE;
			int dreg = load_simd_vreg (cfg, cmethod, args [0], &indirect);

			int opcode = type_to_expand_op (etype->type);
			ins = emit_simd_ins (cfg, klass, opcode, args [1]->dreg, -1);
			ins->dreg = dreg;
			ins->inst_c1 = MONO_TYPE_R4;

			for (int i = 1; i < fsig->param_count; ++i)
				ins = emit_vector_insert_element (cfg, klass, ins, MONO_TYPE_R4, args [i + 1], i, FALSE);

			if (len == 3) {
				static float r4_0 = 0;
				MonoInst *zero;
				int zero_dreg = alloc_freg (cfg);
				MONO_INST_NEW (cfg, zero, OP_R4CONST);
				zero->inst_p0 = (void*)&r4_0;
				zero->dreg = zero_dreg;
				MONO_ADD_INS (cfg->cbb, zero);
				ins = emit_vector_insert_element (cfg, klass, ins, MONO_TYPE_R4, zero, 3, FALSE);
			}

			ins->dreg = dreg;

			if (indirect) {
				/* Have to store back */
				EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STOREX_MEMBASE, args [0]->dreg, 0, dreg);
				ins->klass = klass;
			}
			return ins;
		} else if (len == 3 && fsig->param_count == 2 && fsig->params [0]->type == MONO_TYPE_VALUETYPE && fsig->params [1]->type == etype->type) {
			/* Vector3 (Vector2, float) */
			if (!mini_class_is_simd (cfg, mono_class_from_mono_type_internal (fsig->params [0])))
				// FIXME: Support Vector2 and Vector3 for WASM and AMD64
				return NULL;
			int dreg = load_simd_vreg (cfg, cmethod, args [0], NULL);
			MonoInst* vec_ins = args [1];
			if (COMPILE_LLVM (cfg)) {
				vec_ins = emit_simd_ins (cfg, klass, OP_XWIDEN, args [1]->dreg, -1);
			}

			ins = emit_vector_insert_element (cfg, klass, vec_ins, MONO_TYPE_R4, args [2], 2, FALSE);
			ins->dreg = dreg;
			return ins;
		} else if (len == 4 && fsig->param_count == 2 && fsig->params [0]->type == MONO_TYPE_VALUETYPE && fsig->params [1]->type == etype->type) {
			/* Vector4 (Vector3, float) */
			if (!mini_class_is_simd (cfg, mono_class_from_mono_type_internal (fsig->params [0])))
				// FIXME: Support Vector2 and Vector3 for WASM and AMD64
				return NULL;
			int dreg = load_simd_vreg (cfg, cmethod, args [0], NULL);
			ins = emit_vector_insert_element (cfg, klass, args [1], MONO_TYPE_R4, args [2], 3, FALSE);
			ins->dreg = dreg;
			return ins;
		} else if (len == 4 && fsig->param_count == 3 && fsig->params [0]->type == MONO_TYPE_VALUETYPE && fsig->params [1]->type == etype->type && fsig->params [2]->type == etype->type) {
			/* Vector4 (Vector2, float, float) */
			if (!mini_class_is_simd (cfg, mono_class_from_mono_type_internal (fsig->params [0])))
				// FIXME: Support Vector2 and Vector3 for WASM and AMD64
				return NULL;
			int dreg = load_simd_vreg (cfg, cmethod, args [0], NULL);
			MonoInst* vec_ins = args [1];
			if (COMPILE_LLVM (cfg)) {
				vec_ins = emit_simd_ins (cfg, klass, OP_XWIDEN, args [1]->dreg, -1);
			}

			ins = emit_vector_insert_element (cfg, klass, vec_ins, MONO_TYPE_R4, args [2], 2, FALSE);
			ins = emit_vector_insert_element (cfg, klass, ins, MONO_TYPE_R4, args [3], 3, FALSE);
			ins->dreg = dreg;
			return ins;
		}
		break;
	case SN_get_Item: {
		// GetElement is marked as Intrinsic, but handling this in get_Item leads to better code
		int src1 = load_simd_vreg (cfg, cmethod, args [0], NULL);
		MonoTypeEnum ty = etype->type;

		if (args [1]->opcode == OP_ICONST) {
			// If the index is provably a constant, we can generate vastly better code.
			int index = GTMREG_TO_INT (args[1]->inst_c0);

			if (index < 0 || index >= len) {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, len);
				MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");
			}

			int opcode = type_to_extract_op (ty);
			ins = emit_simd_ins (cfg, klass, opcode, src1, -1);
			ins->inst_c0 = index;
			ins->inst_c1 = ty;
			return ins;
		}

		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, len);
		MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");

		if (COMPILE_LLVM (cfg)) {
			int opcode = type_to_xextract_op (ty);
			ins = emit_simd_ins (cfg, klass, opcode, src1, args [1]->dreg);
			ins->inst_c1 = ty;
		} else {
			// Load back from vector_dreg + index << elem_size_log2
			// TODO: on x86, use a LEA
			int offset_reg = alloc_freg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, offset_reg, args [1]->dreg, type_to_width_log2 (ty));
			int addr_reg = alloc_freg (cfg);
			MONO_EMIT_NEW_BIALU(cfg, OP_PADD, addr_reg, args [0]->dreg, offset_reg);
			int dreg = alloc_freg (cfg);
			NEW_LOAD_MEMBASE (cfg, ins, mono_type_to_load_membase (cfg, fsig->ret), dreg, addr_reg, 0);
			MONO_ADD_INS (cfg->cbb, ins);
		}
		return ins;
	}
	case SN_get_Zero:
		return emit_xzero (cfg, klass);
	case SN_get_UnitX: {
		float value[4];
		value [0] = 1.0f;
		value [1] = 0.0f;
		value [2] = 0.0f;
		value [3] = 0.0f;
		return emit_xconst_v128 (cfg, klass, (guint8*)value);
	}
	case SN_get_UnitY: {
		float value[4];
		value [0] = 0.0f;
		value [1] = 1.0f;
		value [2] = 0.0f;
		value [3] = 0.0f;
		return emit_xconst_v128 (cfg, klass, (guint8*)value);
	}
	case SN_get_UnitZ: {
		float value[4];
		value [0] = 0.0f;
		value [1] = 0.0f;
		value [2] = 1.0f;
		value [3] = 0.0f;
		return emit_xconst_v128 (cfg, klass, (guint8*)value);
	}
	case SN_get_Identity:
	case SN_get_UnitW: {
		float value[4];
		value [0] = 0.0f;
		value [1] = 0.0f;
		value [2] = 0.0f;
		value [3] = 1.0f;
		return emit_xconst_v128 (cfg, klass, (guint8*)value);
	}
	case SN_get_One: {
		float value[4];
		value [0] = 1.0f;
		value [1] = 1.0f;
		value [2] = 1.0f;
		value [3] = 1.0f;
		return emit_xconst_v128 (cfg, klass, (guint8*)value);
	}
	case SN_set_Item: {
		// WithElement is marked as Intrinsic, but handling this in set_Item leads to better code
		g_assert (fsig->hasthis && fsig->param_count == 2 && fsig->params [0]->type == MONO_TYPE_I4 && fsig->params [1]->type == MONO_TYPE_R4);

		gboolean indirect = FALSE;
		int index = GTMREG_TO_INT (args [1]->inst_c0);
		int dreg = load_simd_vreg (cfg, cmethod, args [0], &indirect);

		if (args [1]->opcode == OP_ICONST) {
			// If the index is provably a constant, we can generate vastly better code.
			// Bounds check only if the index is out of range
			if (index < 0 || index >= len) {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, len);
				MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");
			}

			if (args [0]->dreg == dreg) {
				ins = emit_vector_insert_element (cfg, klass, args [0], MONO_TYPE_R4, args [2], index, FALSE);
			} else {
				ins = emit_simd_ins (cfg, klass, OP_INSERT_R4, dreg, args [2]->dreg);
				ins->inst_c0 = index;
				ins->inst_c1 = MONO_TYPE_R4;
				ins->dreg = dreg;
			}

			if (indirect) {
				EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STOREX_MEMBASE, args [0]->dreg, 0, dreg);
				ins->klass = klass;
			}

			return ins;
		}

		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, len);
		MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");

		if (COMPILE_LLVM (cfg)) {
			ins = emit_simd_ins (cfg, klass, OP_XINSERT_R4, dreg, args [2]->dreg);
			ins->sreg3 = args [1]->dreg;
			ins->inst_c1 = MONO_TYPE_R4;
			ins->dreg = dreg;

			if (indirect) {
				EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STOREX_MEMBASE, args [0]->dreg, 0, dreg);
				ins->klass = klass;
			}
		} else {
			// Overwrite [vector_dreg + index << elem_size_log2] with replacement value
			// TODO: on x86, use a LEA
			int offset_reg = alloc_freg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, offset_reg, args [1]->dreg, type_to_width_log2 (MONO_TYPE_R4));
			int addr_reg = alloc_freg (cfg);
			MONO_EMIT_NEW_BIALU(cfg, OP_PADD, addr_reg, args [0]->dreg, offset_reg);

			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, fsig->params [1], addr_reg, 0, args [2]->dreg);
		}

		return ins;
	}
	case SN_Add:
	case SN_Divide:
	case SN_Multiply:
	case SN_Subtract:
	case SN_op_Addition:
	case SN_op_Division:
	case SN_op_Multiply:
	case SN_op_Subtraction:
	case SN_Max:
	case SN_Min: {
		const char *klass_name = m_class_get_name (klass);
		// FIXME https://github.com/dotnet/runtime/issues/82408
		if ((id == SN_op_Multiply || id == SN_Multiply || id == SN_op_Division || id == SN_Divide) && !strcmp (klass_name, "Quaternion"))
			return NULL;
		return emit_simd_ins_for_binary_op (cfg, klass, fsig, args, MONO_TYPE_R4, id);
	}
	case SN_Dot: {
#if defined(TARGET_ARM64) || defined(TARGET_WASM)
		MonoInst *pairwise_multiply = emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, OP_FMUL, MONO_TYPE_R4, fsig, args);
		return emit_sum_vector (cfg, fsig->params [0], MONO_TYPE_R4, pairwise_multiply);
#elif defined(TARGET_AMD64)
		if (!(mini_get_cpu_features (cfg) & MONO_CPU_X86_SSE41))
			return NULL;

		int mask_reg = alloc_ireg (cfg);
		MONO_EMIT_NEW_ICONST (cfg, mask_reg, 0xf1);
		MonoInst *dot = emit_simd_ins (cfg, klass, OP_SSE41_DPPS, args [0]->dreg, args [1]->dreg);
		dot->sreg3 = mask_reg;

		MONO_INST_NEW (cfg, ins, OP_EXTRACT_R4);
		ins->dreg = alloc_freg (cfg);
		ins->sreg1 = dot->dreg;
		ins->inst_c0 = 0;
		ins->inst_c1 = MONO_TYPE_R4;
		MONO_ADD_INS (cfg->cbb, ins);
		return ins;
#else
		return NULL;
#endif
	}
	case SN_Negate:
	case SN_op_UnaryNegation: {
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
		ins = emit_simd_ins (cfg, klass, OP_NEGATION, args [0]->dreg, -1);
		ins->inst_c1 = MONO_TYPE_R4;
		return ins;
#else
		return NULL;
#endif
	}
	case SN_Abs: {
		if (!COMPILE_LLVM (cfg)) {
#ifdef TARGET_ARM64
			return emit_simd_ins_for_sig (cfg, cmethod->klass, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FABS, MONO_TYPE_R4, fsig, args);
#endif
		}
		// MAX(x,0-x)
		MonoInst *zero = emit_xzero (cfg, klass);
		MonoInst *neg = emit_simd_ins (cfg, klass, OP_XBINOP, zero->dreg, args [0]->dreg);
		neg->inst_c0 = OP_FSUB;
		neg->inst_c1 = MONO_TYPE_R4;
		ins = emit_simd_ins (cfg, klass, OP_XBINOP, args [0]->dreg, neg->dreg);
		ins->inst_c0 = OP_FMAX;
		ins->inst_c1 = MONO_TYPE_R4;
		return ins;
	}
	case SN_op_Equality: {
		if (!(fsig->param_count == 2 && mono_metadata_type_equal (fsig->params [0], type) && mono_metadata_type_equal (fsig->params [1], type)))
			return NULL;
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		return emit_xequal (cfg, arg_class, MONO_TYPE_R4, args [0], args [1]);
	}
	case SN_op_Inequality: {
		if (!(fsig->param_count == 2 && mono_metadata_type_equal (fsig->params [0], type) && mono_metadata_type_equal (fsig->params [1], type)))
			return NULL;
		MonoClass *arg_class = mono_class_from_mono_type_internal (fsig->params [0]);
		return emit_not_xequal (cfg, arg_class, MONO_TYPE_R4, args [0], args [1]);
	}
	case SN_SquareRoot: {
#ifdef TARGET_ARM64
		return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FSQRT, MONO_TYPE_R4, fsig, args);
#elif defined(TARGET_AMD64) || defined(TARGET_WASM)
		ins = emit_simd_ins (cfg, klass, OP_XOP_X_X, args [0]->dreg, -1);
		ins->inst_c0 = (IntrinsicId)INTRINS_SIMD_SQRT_R4;
		return ins;
#else
		return NULL;
#endif
	}
	case SN_CopyTo:
		// FIXME: https://github.com/dotnet/runtime/issues/91394
		return NULL;
	case SN_Clamp: {
		if (!(!fsig->hasthis && fsig->param_count == 3 && mono_metadata_type_equal (fsig->ret, type) && mono_metadata_type_equal (fsig->params [0], type) && mono_metadata_type_equal (fsig->params [1], type) && mono_metadata_type_equal (fsig->params [2], type)))
			return NULL;

		MonoInst *max = emit_simd_ins (cfg, klass, OP_XBINOP, args[0]->dreg, args[1]->dreg);
		max->inst_c0 = OP_FMAX;
		max->inst_c1 = MONO_TYPE_R4;

		MonoInst *min = emit_simd_ins (cfg, klass, OP_XBINOP, max->dreg, args[2]->dreg);
		min->inst_c0 = OP_FMIN;
		min->inst_c1 = MONO_TYPE_R4;

		return min;
	}
	case SN_Conjugate:
	case SN_Distance:
	case SN_DistanceSquared:
	case SN_Length:
	case SN_LengthSquared:
	case SN_Lerp:
	case SN_Normalize: {
		// FIXME: https://github.com/dotnet/runtime/issues/91394
		return NULL;
	}
	default:
		g_assert_not_reached ();
	}

	return NULL;
}

#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_WASM)

#ifdef TARGET_AMD64

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
	int id;
	MonoType *etype;

	id = lookup_intrins (vector_methods, sizeof (vector_methods), cmethod);
	if (id == -1) {
		//check_no_intrinsic_cattr (cmethod);
		return NULL;
	}

	//printf ("%s\n", mono_method_full_name (cmethod, 1));

	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	switch (id) {
	case SN_get_IsHardwareAccelerated:
		EMIT_NEW_ICONST (cfg, ins, 1);
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
	SN_GreaterThan,
	SN_GreaterThanOrEqual,
	SN_LessThan,
	SN_LessThanOrEqual,
	SN_Max,
	SN_Min,
	SN_get_AllBitsSet,
	SN_get_Count,
	SN_get_IsSupported,
	SN_get_Item,
	SN_get_One,
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
	if (id == -1) {
		//check_no_intrinsic_cattr (cmethod);
		return NULL;
	}

	klass = cmethod->klass;
	type = m_class_get_byval_arg (klass);
	etype = mono_class_get_context (klass)->class_inst->type_argv [0];

	if (!MONO_TYPE_IS_VECTOR_PRIMITIVE (etype))
		return NULL;

	size = mono_class_value_size (mono_class_from_mono_type_internal (etype), NULL);
	g_assert (size);
	len = register_size / size;


	if (cfg->verbose_level > 1) {
		char *name = mono_method_full_name (cmethod, TRUE);
		printf ("  SIMD intrinsic %s\n", name);
		g_free (name);
	}

	switch (id) {
	case SN_get_IsSupported: {
		EMIT_NEW_ICONST (cfg, ins, 1);
		return ins;
	}
	case SN_get_Count:
		if (!(fsig->param_count == 0 && fsig->ret->type == MONO_TYPE_I4))
			break;
		EMIT_NEW_ICONST (cfg, ins, len);
		return ins;
	case SN_get_Zero:
		g_assert (fsig->param_count == 0 && mono_metadata_type_equal (fsig->ret, type));
		return emit_xzero (cfg, klass);
	case SN_get_One: {
		g_assert (fsig->param_count == 0 && mono_metadata_type_equal (fsig->ret, type));
		g_assert (register_size == 16);

		switch (etype->type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_U1: {
			guint8 value[16];

			for (int i = 0; i < len; ++i) {
				value [i] = 1;
			}

			return emit_xconst_v128 (cfg, klass, value);
		}
		case MONO_TYPE_I2:
		case MONO_TYPE_U2: {
			guint16 value[8];

			for (int i = 0; i < len; ++i) {
				value [i] = 1;
			}

			return emit_xconst_v128 (cfg, klass, (guint8*)value);
		}
#if TARGET_SIZEOF_VOID_P == 4
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#endif
		case MONO_TYPE_I4:
		case MONO_TYPE_U4: {
			guint32 value[4];

			for (int i = 0; i < len; ++i) {
				value [i] = 1;
			}

			return emit_xconst_v128 (cfg, klass, (guint8*)value);
		}
#if TARGET_SIZEOF_VOID_P == 8
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#endif
		case MONO_TYPE_I8:
		case MONO_TYPE_U8: {
			guint64 value[2];

			for (int i = 0; i < len; ++i) {
				value [i] = 1;
			}

			return emit_xconst_v128 (cfg, klass, (guint8*)value);
		}
		case MONO_TYPE_R4: {
			float value[4];

			for (int i = 0; i < len; ++i) {
				value [i] = 1.0f;
			}

			return emit_xconst_v128 (cfg, klass, (guint8*)value);
		}
		case MONO_TYPE_R8: {
			double value[2];

			for (int i = 0; i < len; ++i) {
				value [i] = 1.0;
			}

			return emit_xconst_v128 (cfg, klass, (guint8*)value);
		}
		default:
			g_assert_not_reached ();
		}
	}
	case SN_get_AllBitsSet: {
		return emit_xones (cfg, klass);
	}
	case SN_get_Item: {
		if (!COMPILE_LLVM (cfg))
			return NULL;
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, args [1]->dreg, len);
		MONO_EMIT_NEW_COND_EXC (cfg, GE_UN, "ArgumentOutOfRangeException");
		MonoTypeEnum ty = etype->type;
		int opcode = type_to_xextract_op (ty);
		int src1 = load_simd_vreg (cfg, cmethod, args [0], NULL);
		ins = emit_simd_ins (cfg, klass, opcode, src1, args [1]->dreg);
		ins->inst_c1 = ty;
		return ins;
	}
	case SN_ctor:
		if (fsig->param_count == 1 && mono_metadata_type_equal (fsig->params [0], etype)) {
			int dreg = load_simd_vreg (cfg, cmethod, args [0], NULL);

			int opcode = type_to_expand_op (etype->type);
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

			/* Emit bounds check for the index (index >= 0) */
			mini_emit_bounds_check_offset (cfg, array_ins->dreg, MONO_STRUCT_OFFSET (MonoArray, max_length), index_ins->dreg, "ArgumentOutOfRangeException", FALSE);

			/* Emit bounds check for the end (index + len - 1 < array length) */
			end_index_reg = alloc_ireg (cfg);
			EMIT_NEW_BIALU_IMM (cfg, ins, OP_IADD_IMM, end_index_reg, index_ins->dreg, len - 1);
			mini_emit_bounds_check_offset (cfg, array_ins->dreg, MONO_STRUCT_OFFSET (MonoArray, max_length), end_index_reg, "ArgumentOutOfRangeException", FALSE);

			/* Load the array slice into the simd reg */
			ldelema_ins = mini_emit_ldelema_1_ins (cfg, mono_class_from_mono_type_internal (etype), array_ins, index_ins, FALSE, FALSE);
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
			mini_emit_bounds_check_offset (cfg, array_ins->dreg, MONO_STRUCT_OFFSET (MonoArray, max_length), index_ins->dreg, "ArgumentOutOfRangeException", FALSE);
			end_index_reg = alloc_ireg (cfg);
			int len_reg = alloc_ireg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP_FLAGS (cfg, OP_LOADI4_MEMBASE, len_reg, array_ins->dreg, MONO_STRUCT_OFFSET (MonoArray, max_length), MONO_INST_INVARIANT_LOAD);
			EMIT_NEW_BIALU (cfg, ins, OP_ISUB, end_index_reg, len_reg, index_ins->dreg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, end_index_reg, len);
			MONO_EMIT_NEW_COND_EXC (cfg, LT, "ArgumentException");

			/* Load the array slice into the simd reg */
			ldelema_ins = mini_emit_ldelema_1_ins (cfg, mono_class_from_mono_type_internal (etype), array_ins, index_ins, FALSE, FALSE);
			EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STOREX_MEMBASE, ldelema_ins->dreg, 0, val_vreg);
			ins->klass = cmethod->klass;
			return ins;
		}
		break;
	case SN_op_Equality:
	case SN_op_Inequality:
		g_assert (fsig->param_count == 2 && fsig->ret->type == MONO_TYPE_BOOLEAN &&
				  mono_metadata_type_equal (fsig->params [0], type) &&
				  mono_metadata_type_equal (fsig->params [1], type));
		switch (id) {
			case SN_op_Equality: return emit_xequal (cfg, klass, etype->type, args [0], args [1]);
			case SN_op_Inequality: return emit_not_xequal (cfg, klass, etype->type, args [0], args [1]);
			default: g_assert_not_reached ();
		}
	case SN_GreaterThan:
	case SN_GreaterThanOrEqual:
	case SN_LessThan:
	case SN_LessThanOrEqual:
		g_assert (fsig->param_count == 2 && mono_metadata_type_equal (fsig->ret, type) && mono_metadata_type_equal (fsig->params [0], type) && mono_metadata_type_equal (fsig->params [1], type));
		is_unsigned = etype->type == MONO_TYPE_U1 || etype->type == MONO_TYPE_U2 || etype->type == MONO_TYPE_U4 || etype->type == MONO_TYPE_U8 || etype->type == MONO_TYPE_U;
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

		if (type_enum_is_float (etype->type)) {
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
#endif // TARGET_AMD64

#ifdef TARGET_ARM64

static SimdIntrinsic armbase_methods [] = {
	{SN_LeadingSignCount},
	{SN_LeadingZeroCount},
	{SN_MultiplyHigh},
	{SN_ReverseElementBits},
	{SN_Yield},
	{SN_get_IsSupported},
};

static SimdIntrinsic crc32_methods [] = {
	{SN_ComputeCrc32},
	{SN_ComputeCrc32C},
	{SN_get_IsSupported}
};

static SimdIntrinsic crypto_aes_methods [] = {
	{SN_Decrypt, OP_XOP_X_X_X, INTRINS_AARCH64_AESD},
	{SN_Encrypt, OP_XOP_X_X_X, INTRINS_AARCH64_AESE},
	{SN_InverseMixColumns, OP_XOP_X_X, INTRINS_AARCH64_AESIMC},
	{SN_MixColumns, OP_XOP_X_X, INTRINS_AARCH64_AESMC},
	{SN_PolynomialMultiplyWideningLower},
	{SN_PolynomialMultiplyWideningUpper},
	{SN_get_IsSupported},
};

static SimdIntrinsic sha1_methods [] = {
	{SN_FixedRotate, OP_XOP_X_X, INTRINS_AARCH64_SHA1H},
	{SN_HashUpdateChoose, OP_XOP_X_X_X_X, INTRINS_AARCH64_SHA1C},
	{SN_HashUpdateMajority, OP_XOP_X_X_X_X, INTRINS_AARCH64_SHA1M},
	{SN_HashUpdateParity, OP_XOP_X_X_X_X, INTRINS_AARCH64_SHA1P},
	{SN_ScheduleUpdate0, OP_XOP_X_X_X_X, INTRINS_AARCH64_SHA1SU0},
	{SN_ScheduleUpdate1, OP_XOP_X_X_X, INTRINS_AARCH64_SHA1SU1},
	{SN_get_IsSupported}
};

static SimdIntrinsic sha256_methods [] = {
	{SN_HashUpdate1, OP_XOP_X_X_X_X, INTRINS_AARCH64_SHA256H},
	{SN_HashUpdate2, OP_XOP_X_X_X_X, INTRINS_AARCH64_SHA256H2},
	{SN_ScheduleUpdate0, OP_XOP_X_X_X, INTRINS_AARCH64_SHA256SU0},
	{SN_ScheduleUpdate1, OP_XOP_X_X_X_X, INTRINS_AARCH64_SHA256SU1},
	{SN_get_IsSupported}
};

// This table must be kept in sorted order. ASCII } is sorted after alphanumeric
// characters, so blind use of your editor's "sort lines" facility will
// mis-order the lines.
//
// In Vim you can use `sort /.*{[0-9A-z]*/ r` to sort this table.

static SimdIntrinsic advsimd_methods [] = {
	{SN_Abs, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_ABS, None, None, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FABS},
	{SN_AbsSaturate, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_SQABS},
	{SN_AbsSaturateScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_SQABS},
	{SN_AbsScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_ABS, None, None, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FABS},
	{SN_AbsoluteCompareGreaterThan},
	{SN_AbsoluteCompareGreaterThanOrEqual},
	{SN_AbsoluteCompareGreaterThanOrEqualScalar},
	{SN_AbsoluteCompareGreaterThanScalar},
	{SN_AbsoluteCompareLessThan},
	{SN_AbsoluteCompareLessThanOrEqual},
	{SN_AbsoluteCompareLessThanOrEqualScalar},
	{SN_AbsoluteCompareLessThanScalar},
	{SN_AbsoluteDifference, OP_ARM64_SABD, None, OP_ARM64_UABD, None, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FABD},
	{SN_AbsoluteDifferenceAdd, OP_ARM64_SABA, None, OP_ARM64_UABA},
	{SN_AbsoluteDifferenceScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FABD_SCALAR},
	{SN_AbsoluteDifferenceWideningLower, OP_ARM64_SABDL, None, OP_ARM64_UABDL},
	{SN_AbsoluteDifferenceWideningLowerAndAdd, OP_ARM64_SABAL, None, OP_ARM64_UABAL},
	{SN_AbsoluteDifferenceWideningUpper, OP_ARM64_SABDL2, None, OP_ARM64_UABDL2},
	{SN_AbsoluteDifferenceWideningUpperAndAdd, OP_ARM64_SABAL2, None, OP_ARM64_UABAL2},
	{SN_Add, OP_XBINOP, OP_IADD, None, None, OP_XBINOP, OP_FADD},
	{SN_AddAcross, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_SADDV, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_UADDV},
	{SN_AddAcrossWidening, OP_ARM64_SADDLV, None, OP_ARM64_UADDLV},
	{SN_AddHighNarrowingLower, OP_ARM64_ADDHN},
	{SN_AddHighNarrowingUpper, OP_ARM64_ADDHN2},
	{SN_AddPairwise, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_ADDP, None, None, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FADDP},
	{SN_AddPairwiseScalar, OP_ARM64_ADDP_SCALAR, None, None, None, OP_ARM64_FADDP_SCALAR},
	{SN_AddPairwiseWidening, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_SADDLP, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_UADDLP},
	{SN_AddPairwiseWideningAndAdd, OP_ARM64_SADALP, None, OP_ARM64_UADALP},
	{SN_AddPairwiseWideningAndAddScalar, OP_ARM64_SADALP, None, OP_ARM64_UADALP},
	{SN_AddPairwiseWideningScalar, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_SADDLP, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_UADDLP},
	{SN_AddRoundedHighNarrowingLower, OP_ARM64_RADDHN},
	{SN_AddRoundedHighNarrowingUpper, OP_ARM64_RADDHN2},
	{SN_AddSaturate},
	{SN_AddSaturateScalar},
	{SN_AddScalar, OP_XBINOP_SCALAR, OP_IADD, None, None, OP_XBINOP_SCALAR, OP_FADD},
	{SN_AddWideningLower, OP_ARM64_SADD, None, OP_ARM64_UADD},
	{SN_AddWideningUpper, OP_ARM64_SADD2, None, OP_ARM64_UADD2},
	{SN_And, OP_XBINOP_FORCEINT, XBINOP_FORCEINT_AND},
	{SN_BitwiseClear, OP_ARM64_BIC},
	{SN_BitwiseSelect, OP_BSL},
	{SN_Ceiling, OP_XOP_OVR_X_X, INTRINS_SIMD_CEIL},
	{SN_CeilingScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_SIMD_CEIL},
	{SN_CompareEqual, OP_XCOMPARE, CMP_EQ, OP_XCOMPARE, CMP_EQ, OP_XCOMPARE_FP, CMP_EQ},
	{SN_CompareEqualScalar, OP_XCOMPARE_SCALAR, CMP_EQ, OP_XCOMPARE_SCALAR, CMP_EQ, OP_XCOMPARE_FP_SCALAR, CMP_EQ},
	{SN_CompareGreaterThan, OP_XCOMPARE, CMP_GT, OP_XCOMPARE, CMP_GT_UN, OP_XCOMPARE_FP, CMP_GT},
	{SN_CompareGreaterThanOrEqual, OP_XCOMPARE, CMP_GE, OP_XCOMPARE, CMP_GE_UN, OP_XCOMPARE_FP, CMP_GE},
	{SN_CompareGreaterThanOrEqualScalar, OP_XCOMPARE_SCALAR, CMP_GE, OP_XCOMPARE_SCALAR, CMP_GE_UN, OP_XCOMPARE_FP_SCALAR, CMP_GE},
	{SN_CompareGreaterThanScalar, OP_XCOMPARE_SCALAR, CMP_GT, OP_XCOMPARE_SCALAR, CMP_GT_UN, OP_XCOMPARE_FP_SCALAR, CMP_GT},
	{SN_CompareLessThan, OP_XCOMPARE, CMP_LT, OP_XCOMPARE, CMP_LT_UN, OP_XCOMPARE_FP, CMP_LT},
	{SN_CompareLessThanOrEqual, OP_XCOMPARE, CMP_LE, OP_XCOMPARE, CMP_LE_UN, OP_XCOMPARE_FP, CMP_LE},
	{SN_CompareLessThanOrEqualScalar, OP_XCOMPARE_SCALAR, CMP_LE, OP_XCOMPARE_SCALAR, CMP_LE_UN, OP_XCOMPARE_FP_SCALAR, CMP_LE},
	{SN_CompareLessThanScalar, OP_XCOMPARE_SCALAR, CMP_LT, OP_XCOMPARE_SCALAR, CMP_LT_UN, OP_XCOMPARE_FP_SCALAR, CMP_LT},
	{SN_CompareTest, OP_ARM64_CMTST},
	{SN_CompareTestScalar, OP_ARM64_CMTST},
	{SN_ConvertToDouble, OP_CVT_SI_FP, None, OP_CVT_UI_FP, None, OP_SIMD_FCVTL},
	{SN_ConvertToDoubleScalar, OP_CVT_SI_FP_SCALAR, None, OP_CVT_UI_FP_SCALAR},
	{SN_ConvertToDoubleUpper, OP_SIMD_FCVTL2},
	{SN_ConvertToInt32RoundAwayFromZero, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTAS},
	{SN_ConvertToInt32RoundAwayFromZeroScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTAS},
	{SN_ConvertToInt32RoundToEven, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTNS},
	{SN_ConvertToInt32RoundToEvenScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTNS},
	{SN_ConvertToInt32RoundToNegativeInfinity, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTMS},
	{SN_ConvertToInt32RoundToNegativeInfinityScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTMS},
	{SN_ConvertToInt32RoundToPositiveInfinity, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTPS},
	{SN_ConvertToInt32RoundToPositiveInfinityScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTPS},
	{SN_ConvertToInt32RoundToZero, OP_CVT_FP_SI},
	{SN_ConvertToInt32RoundToZeroScalar, OP_CVT_FP_SI_SCALAR},
	{SN_ConvertToInt64RoundAwayFromZero, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTAS},
	{SN_ConvertToInt64RoundAwayFromZeroScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTAS},
	{SN_ConvertToInt64RoundToEven, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTNS},
	{SN_ConvertToInt64RoundToEvenScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTNS},
	{SN_ConvertToInt64RoundToNegativeInfinity, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTMS},
	{SN_ConvertToInt64RoundToNegativeInfinityScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTMS},
	{SN_ConvertToInt64RoundToPositiveInfinity, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTPS},
	{SN_ConvertToInt64RoundToPositiveInfinityScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTPS},
	{SN_ConvertToInt64RoundToZero, OP_CVT_FP_SI},
	{SN_ConvertToInt64RoundToZeroScalar, OP_CVT_FP_SI_SCALAR},
	{SN_ConvertToSingle, OP_CVT_SI_FP, None, OP_CVT_UI_FP},
	{SN_ConvertToSingleLower, OP_ARM64_FCVTN},
	{SN_ConvertToSingleRoundToOddLower, OP_ARM64_FCVTXN},
	{SN_ConvertToSingleRoundToOddUpper, OP_ARM64_FCVTXN2},
	{SN_ConvertToSingleScalar, OP_CVT_SI_FP_SCALAR, None, OP_CVT_UI_FP_SCALAR},
	{SN_ConvertToSingleUpper, OP_ARM64_FCVTN2},
	{SN_ConvertToUInt32RoundAwayFromZero, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTAU},
	{SN_ConvertToUInt32RoundAwayFromZeroScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTAU},
	{SN_ConvertToUInt32RoundToEven, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTNU},
	{SN_ConvertToUInt32RoundToEvenScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTNU},
	{SN_ConvertToUInt32RoundToNegativeInfinity, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTMU},
	{SN_ConvertToUInt32RoundToNegativeInfinityScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTMU},
	{SN_ConvertToUInt32RoundToPositiveInfinity, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTPU},
	{SN_ConvertToUInt32RoundToPositiveInfinityScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTPU},
	{SN_ConvertToUInt32RoundToZero, OP_CVT_FP_UI},
	{SN_ConvertToUInt32RoundToZeroScalar, OP_CVT_FP_UI_SCALAR},
	{SN_ConvertToUInt64RoundAwayFromZero, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTAU},
	{SN_ConvertToUInt64RoundAwayFromZeroScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTAU},
	{SN_ConvertToUInt64RoundToEven, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTNU},
	{SN_ConvertToUInt64RoundToEvenScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTNU},
	{SN_ConvertToUInt64RoundToNegativeInfinity, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTMU},
	{SN_ConvertToUInt64RoundToNegativeInfinityScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTMU},
	{SN_ConvertToUInt64RoundToPositiveInfinity, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTPU},
	{SN_ConvertToUInt64RoundToPositiveInfinityScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FCVTPU},
	{SN_ConvertToUInt64RoundToZero, OP_CVT_FP_UI},
	{SN_ConvertToUInt64RoundToZeroScalar, OP_CVT_FP_UI_SCALAR},
	{SN_Divide, OP_XBINOP, OP_FDIV},
	{SN_DivideScalar, OP_XBINOP_SCALAR, OP_FDIV},
	{SN_DuplicateSelectedScalarToVector128},
	{SN_DuplicateSelectedScalarToVector64},
	{SN_DuplicateToVector128},
	{SN_DuplicateToVector64},
	{SN_Extract},
	{SN_ExtractNarrowingLower, OP_ARM64_XTN},
	{SN_ExtractNarrowingSaturateLower, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_SQXTN, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_UQXTN},
	{SN_ExtractNarrowingSaturateScalar, OP_ARM64_XNARROW_SCALAR, INTRINS_AARCH64_ADV_SIMD_SQXTN, OP_ARM64_XNARROW_SCALAR, INTRINS_AARCH64_ADV_SIMD_UQXTN},
	{SN_ExtractNarrowingSaturateUnsignedLower, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_SQXTUN},
	{SN_ExtractNarrowingSaturateUnsignedScalar, OP_ARM64_XNARROW_SCALAR, INTRINS_AARCH64_ADV_SIMD_SQXTUN},
	{SN_ExtractNarrowingSaturateUnsignedUpper, OP_ARM64_SQXTUN2},
	{SN_ExtractNarrowingSaturateUpper, OP_ARM64_SQXTN2, None, OP_ARM64_UQXTN2},
	{SN_ExtractNarrowingUpper, OP_ARM64_XTN2},
	{SN_ExtractVector128, OP_ARM64_EXT},
	{SN_ExtractVector64, OP_ARM64_EXT},
	{SN_Floor, OP_XOP_OVR_X_X, INTRINS_SIMD_FLOOR},
	{SN_FloorScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_SIMD_FLOOR},
	{SN_FusedAddHalving, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SHADD, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UHADD},
	{SN_FusedAddRoundedHalving, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SRHADD, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_URHADD},
	{SN_FusedMultiplyAdd, OP_ARM64_FMADD},
	{SN_FusedMultiplyAddByScalar, OP_ARM64_FMADD_BYSCALAR},
	{SN_FusedMultiplyAddBySelectedScalar},
	{SN_FusedMultiplyAddNegatedScalar, OP_ARM64_FNMADD_SCALAR},
	{SN_FusedMultiplyAddScalar, OP_ARM64_FMADD_SCALAR},
	{SN_FusedMultiplyAddScalarBySelectedScalar},
	{SN_FusedMultiplySubtract, OP_ARM64_FMSUB},
	{SN_FusedMultiplySubtractByScalar, OP_ARM64_FMSUB_BYSCALAR},
	{SN_FusedMultiplySubtractBySelectedScalar},
	{SN_FusedMultiplySubtractNegatedScalar, OP_ARM64_FNMSUB_SCALAR},
	{SN_FusedMultiplySubtractScalar, OP_ARM64_FMSUB_SCALAR},
	{SN_FusedMultiplySubtractScalarBySelectedScalar},
	{SN_FusedSubtractHalving, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SHSUB, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UHSUB},
	{SN_Insert},
	{SN_InsertScalar},
	{SN_InsertSelectedScalar},
	{SN_LeadingSignCount, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_CLS},
	{SN_LeadingZeroCount, OP_ARM64_CLZ},
	{SN_LoadAndInsertScalar},
	{SN_LoadAndReplicateToVector128, OP_ARM64_LD1R},
	{SN_LoadAndReplicateToVector128x2, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD2R_V128},
	{SN_LoadAndReplicateToVector128x3, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD3R_V128},
	{SN_LoadAndReplicateToVector128x4, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD4R_V128},
	{SN_LoadAndReplicateToVector64, OP_ARM64_LD1R},
	{SN_LoadAndReplicateToVector64x2, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD2R_V64},
	{SN_LoadAndReplicateToVector64x3, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD3R_V64},
	{SN_LoadAndReplicateToVector64x4, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD4R_V64},
	{SN_LoadPairScalarVector64, OP_ARM64_LDP_SCALAR},
	{SN_LoadPairScalarVector64NonTemporal, OP_ARM64_LDNP_SCALAR},
	{SN_LoadPairVector128, OP_ARM64_LDP},
	{SN_LoadPairVector128NonTemporal, OP_ARM64_LDNP},
	{SN_LoadPairVector64, OP_ARM64_LDP},
	{SN_LoadPairVector64NonTemporal, OP_ARM64_LDNP},
	{SN_LoadVector128, OP_ARM64_LD1},
	{SN_LoadVector128x2, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD1X2_V128},
	{SN_LoadVector128x2AndUnzip, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD2_V128},
	{SN_LoadVector128x3, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD1X3_V128},
	{SN_LoadVector128x3AndUnzip, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD3_V128},
	{SN_LoadVector128x4, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD1X4_V128},
	{SN_LoadVector128x4AndUnzip, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD4_V128},
	{SN_LoadVector64, OP_ARM64_LD1},
	{SN_LoadVector64x2, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD1X2_V64},
	{SN_LoadVector64x2AndUnzip, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD2_V64},
	{SN_LoadVector64x3, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD1X3_V64},
	{SN_LoadVector64x3AndUnzip, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD3_V64},
	{SN_LoadVector64x4, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD1X4_V64},
	{SN_LoadVector64x4AndUnzip, OP_ARM64_LDM, INTRINS_AARCH64_ADV_SIMD_LD4_V64},
	{SN_Max, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SMAX, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UMAX, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMAX},
	{SN_MaxAcross, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_SMAXV, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_UMAXV, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_FMAXV},
	{SN_MaxNumber, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMAXNM},
	{SN_MaxNumberAcross, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_FMAXNMV},
	{SN_MaxNumberPairwise, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMAXNMP},
	{SN_MaxNumberPairwiseScalar, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_FMAXNMV},
	{SN_MaxNumberScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMAXNM},
	{SN_MaxPairwise, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SMAXP, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UMAXP, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMAXP},
	{SN_MaxPairwiseScalar, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_FMAXV},
	{SN_MaxScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMAX},
	{SN_Min, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SMIN, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UMIN, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMIN},
	{SN_MinAcross, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_SMINV, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_UMINV, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_FMINV},
	{SN_MinNumber, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMINNM},
	{SN_MinNumberAcross, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_FMINNMV},
	{SN_MinNumberPairwise, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMINNMP},
	{SN_MinNumberPairwiseScalar, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_FMINNMV},
	{SN_MinNumberScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMINNM},
	{SN_MinPairwise, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SMINP, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UMINP, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMINP},
	{SN_MinPairwiseScalar, OP_ARM64_XHORIZ, INTRINS_AARCH64_ADV_SIMD_FMINV},
	{SN_MinScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMIN},
	{SN_Multiply, OP_XBINOP, OP_IMUL, None, None, OP_XBINOP, OP_FMUL},
	{SN_MultiplyAdd, OP_ARM64_MLA},
	{SN_MultiplyAddByScalar, OP_ARM64_MLA_SCALAR},
	{SN_MultiplyAddBySelectedScalar},
	{SN_MultiplyByScalar, OP_XBINOP_BYSCALAR, OP_IMUL, None, None, OP_XBINOP_BYSCALAR, OP_FMUL},
	{SN_MultiplyBySelectedScalar},
	{SN_MultiplyBySelectedScalarWideningLower},
	{SN_MultiplyBySelectedScalarWideningLowerAndAdd},
	{SN_MultiplyBySelectedScalarWideningLowerAndSubtract},
	{SN_MultiplyBySelectedScalarWideningUpper},
	{SN_MultiplyBySelectedScalarWideningUpperAndAdd},
	{SN_MultiplyBySelectedScalarWideningUpperAndSubtract},
	{SN_MultiplyDoublingByScalarSaturateHigh, OP_XOP_OVR_BYSCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQDMULH},
	{SN_MultiplyDoublingBySelectedScalarSaturateHigh},
	{SN_MultiplyDoublingSaturateHigh, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQDMULH},
	{SN_MultiplyDoublingSaturateHighScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQDMULH},
	{SN_MultiplyDoublingScalarBySelectedScalarSaturateHigh},
	{SN_MultiplyDoublingWideningAndAddSaturateScalar, OP_ARM64_SQDMLAL_SCALAR},
	{SN_MultiplyDoublingWideningAndSubtractSaturateScalar, OP_ARM64_SQDMLSL_SCALAR},
	{SN_MultiplyDoublingWideningLowerAndAddSaturate, OP_ARM64_SQDMLAL},
	{SN_MultiplyDoublingWideningLowerAndSubtractSaturate, OP_ARM64_SQDMLSL},
	{SN_MultiplyDoublingWideningLowerByScalarAndAddSaturate, OP_ARM64_SQDMLAL_BYSCALAR},
	{SN_MultiplyDoublingWideningLowerByScalarAndSubtractSaturate, OP_ARM64_SQDMLSL_BYSCALAR},
	{SN_MultiplyDoublingWideningLowerBySelectedScalarAndAddSaturate},
	{SN_MultiplyDoublingWideningLowerBySelectedScalarAndSubtractSaturate},
	{SN_MultiplyDoublingWideningSaturateLower, OP_ARM64_SQDMULL},
	{SN_MultiplyDoublingWideningSaturateLowerByScalar, OP_ARM64_SQDMULL_BYSCALAR},
	{SN_MultiplyDoublingWideningSaturateLowerBySelectedScalar},
	{SN_MultiplyDoublingWideningSaturateScalar, OP_ARM64_SQDMULL_SCALAR},
	{SN_MultiplyDoublingWideningSaturateScalarBySelectedScalar},
	{SN_MultiplyDoublingWideningSaturateUpper, OP_ARM64_SQDMULL2},
	{SN_MultiplyDoublingWideningSaturateUpperByScalar, OP_ARM64_SQDMULL2_BYSCALAR},
	{SN_MultiplyDoublingWideningSaturateUpperBySelectedScalar},
	{SN_MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate},
	{SN_MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate},
	{SN_MultiplyDoublingWideningUpperAndAddSaturate, OP_ARM64_SQDMLAL2},
	{SN_MultiplyDoublingWideningUpperAndSubtractSaturate, OP_ARM64_SQDMLSL2},
	{SN_MultiplyDoublingWideningUpperByScalarAndAddSaturate, OP_ARM64_SQDMLAL2_BYSCALAR},
	{SN_MultiplyDoublingWideningUpperByScalarAndSubtractSaturate, OP_ARM64_SQDMLSL2_BYSCALAR},
	{SN_MultiplyDoublingWideningUpperBySelectedScalarAndAddSaturate},
	{SN_MultiplyDoublingWideningUpperBySelectedScalarAndSubtractSaturate},
	{SN_MultiplyExtended, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMULX},
	{SN_MultiplyExtendedByScalar, OP_XOP_OVR_BYSCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMULX},
	{SN_MultiplyExtendedBySelectedScalar},
	{SN_MultiplyExtendedScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FMULX},
	{SN_MultiplyExtendedScalarBySelectedScalar},
	{SN_MultiplyRoundedDoublingByScalarSaturateHigh, OP_XOP_OVR_BYSCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQRDMULH},
	{SN_MultiplyRoundedDoublingBySelectedScalarSaturateHigh},
	{SN_MultiplyRoundedDoublingSaturateHigh, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQRDMULH},
	{SN_MultiplyRoundedDoublingSaturateHighScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQRDMULH},
	{SN_MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh},
	{SN_MultiplyScalar, OP_XBINOP_SCALAR, OP_FMUL},
	{SN_MultiplyScalarBySelectedScalar, OP_ARM64_FMUL_SEL},
	{SN_MultiplySubtract, OP_ARM64_MLS},
	{SN_MultiplySubtractByScalar, OP_ARM64_MLS_SCALAR},
	{SN_MultiplySubtractBySelectedScalar},
	{SN_MultiplyWideningLower, OP_ARM64_SMULL, None, OP_ARM64_UMULL},
	{SN_MultiplyWideningLowerAndAdd, OP_ARM64_SMLAL, None, OP_ARM64_UMLAL},
	{SN_MultiplyWideningLowerAndSubtract, OP_ARM64_SMLSL, None, OP_ARM64_UMLSL},
	{SN_MultiplyWideningUpper, OP_ARM64_SMULL2, None, OP_ARM64_UMULL2},
	{SN_MultiplyWideningUpperAndAdd, OP_ARM64_SMLAL2, None, OP_ARM64_UMLAL2},
	{SN_MultiplyWideningUpperAndSubtract, OP_ARM64_SMLSL2, None, OP_ARM64_UMLSL2},
	{SN_Negate, OP_NEGATION},
	{SN_NegateSaturate, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_SQNEG},
	{SN_NegateSaturateScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_SQNEG},
	{SN_NegateScalar, OP_NEGATION_SCALAR},
	{SN_Not, OP_ONES_COMPLEMENT},
	{SN_Or, OP_XBINOP_FORCEINT, XBINOP_FORCEINT_OR},
	{SN_OrNot, OP_XBINOP_FORCEINT, XBINOP_FORCEINT_ORNOT},
	{SN_PolynomialMultiply, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_PMUL},
	{SN_PolynomialMultiplyWideningLower, OP_ARM64_PMULL},
	{SN_PolynomialMultiplyWideningUpper, OP_ARM64_PMULL2},
	{SN_PopCount, OP_XOP_OVR_X_X, INTRINS_SIMD_POPCNT},
	{SN_ReciprocalEstimate, None, None, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_URECPE, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FRECPE},
	{SN_ReciprocalEstimateScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FRECPE},
	{SN_ReciprocalExponentScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FRECPX},
	{SN_ReciprocalSquareRootEstimate, None, None, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_URSQRTE, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FRSQRTE},
	{SN_ReciprocalSquareRootEstimateScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FRSQRTE},
	{SN_ReciprocalSquareRootStep, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FRSQRTS},
	{SN_ReciprocalSquareRootStepScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FRSQRTS},
	{SN_ReciprocalStep, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FRECPS},
	{SN_ReciprocalStepScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_FRECPS},
	{SN_ReverseElement16, OP_ARM64_REVN, 16},
	{SN_ReverseElement32, OP_ARM64_REVN, 32},
	{SN_ReverseElement8, OP_ARM64_REVN, 8},
	{SN_ReverseElementBits, OP_XOP_OVR_X_X, INTRINS_BITREVERSE},
	{SN_RoundAwayFromZero, OP_XOP_OVR_X_X, INTRINS_SIMD_ROUND},
	{SN_RoundAwayFromZeroScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_SIMD_ROUND},
	{SN_RoundToNearest, OP_XOP_OVR_X_X, INTRINS_ROUNDEVEN},
	{SN_RoundToNearestScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_ROUNDEVEN},
	{SN_RoundToNegativeInfinity, OP_XOP_OVR_X_X, INTRINS_SIMD_FLOOR},
	{SN_RoundToNegativeInfinityScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_SIMD_FLOOR},
	{SN_RoundToPositiveInfinity, OP_XOP_OVR_X_X, INTRINS_SIMD_CEIL},
	{SN_RoundToPositiveInfinityScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_SIMD_CEIL},
	{SN_RoundToZero, OP_XOP_OVR_X_X, INTRINS_SIMD_TRUNC},
	{SN_RoundToZeroScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_SIMD_TRUNC},
	{SN_ShiftArithmetic, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SSHL},
	{SN_ShiftArithmeticRounded, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SRSHL},
	{SN_ShiftArithmeticRoundedSaturate, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQRSHL},
	{SN_ShiftArithmeticRoundedSaturateScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQRSHL},
	{SN_ShiftArithmeticRoundedScalar, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SRSHL},
	{SN_ShiftArithmeticSaturate, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQSHL},
	{SN_ShiftArithmeticSaturateScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQSHL},
	{SN_ShiftArithmeticScalar, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SSHL},
	{SN_ShiftLeftAndInsert, OP_ARM64_SLI},
	{SN_ShiftLeftAndInsertScalar, OP_ARM64_SLI},
	{SN_ShiftLeftLogical, OP_SIMD_SHL},
	{SN_ShiftLeftLogicalSaturate},
	{SN_ShiftLeftLogicalSaturateScalar},
	{SN_ShiftLeftLogicalSaturateUnsigned, OP_ARM64_SQSHLU},
	{SN_ShiftLeftLogicalSaturateUnsignedScalar, OP_ARM64_SQSHLU_SCALAR},
	{SN_ShiftLeftLogicalScalar, OP_SIMD_SHL},
	{SN_ShiftLeftLogicalWideningLower, OP_SIMD_SSHLL, None, OP_SIMD_USHLL},
	{SN_ShiftLeftLogicalWideningUpper, OP_SIMD_SSHLL2, None, OP_SIMD_USHLL2},
	{SN_ShiftLogical, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_USHL},
	{SN_ShiftLogicalRounded, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_URSHL},
	{SN_ShiftLogicalRoundedSaturate, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UQRSHL},
	{SN_ShiftLogicalRoundedSaturateScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UQRSHL},
	{SN_ShiftLogicalRoundedScalar, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_URSHL},
	{SN_ShiftLogicalSaturate, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UQSHL},
	{SN_ShiftLogicalSaturateScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UQSHL},
	{SN_ShiftLogicalScalar, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_USHL},
	{SN_ShiftRightAndInsert, OP_ARM64_SRI},
	{SN_ShiftRightAndInsertScalar, OP_ARM64_SRI},
	{SN_ShiftRightArithmetic, OP_SIMD_SSHR},
	{SN_ShiftRightArithmeticAdd, OP_SIMD_SSRA},
	{SN_ShiftRightArithmeticAddScalar, OP_SIMD_SSRA},
	{SN_ShiftRightArithmeticNarrowingSaturateLower, OP_ARM64_XNSHIFT, INTRINS_AARCH64_ADV_SIMD_SQSHRN},
	{SN_ShiftRightArithmeticNarrowingSaturateScalar, OP_ARM64_XNSHIFT_SCALAR, INTRINS_AARCH64_ADV_SIMD_SQSHRN},
	{SN_ShiftRightArithmeticNarrowingSaturateUnsignedLower, OP_ARM64_XNSHIFT, INTRINS_AARCH64_ADV_SIMD_SQSHRUN},
	{SN_ShiftRightArithmeticNarrowingSaturateUnsignedScalar, OP_ARM64_XNSHIFT_SCALAR, INTRINS_AARCH64_ADV_SIMD_SQSHRUN},
	{SN_ShiftRightArithmeticNarrowingSaturateUnsignedUpper, OP_ARM64_XNSHIFT2, INTRINS_AARCH64_ADV_SIMD_SQSHRUN},
	{SN_ShiftRightArithmeticNarrowingSaturateUpper, OP_ARM64_XNSHIFT2, INTRINS_AARCH64_ADV_SIMD_SQSHRN},
	{SN_ShiftRightArithmeticRounded, OP_ARM64_SRSHR},
	{SN_ShiftRightArithmeticRoundedAdd, OP_ARM64_SRSRA},
	{SN_ShiftRightArithmeticRoundedAddScalar, OP_ARM64_SRSRA},
	{SN_ShiftRightArithmeticRoundedNarrowingSaturateLower, OP_ARM64_XNSHIFT, INTRINS_AARCH64_ADV_SIMD_SQRSHRN},
	{SN_ShiftRightArithmeticRoundedNarrowingSaturateScalar, OP_ARM64_XNSHIFT_SCALAR, INTRINS_AARCH64_ADV_SIMD_SQRSHRN},
	{SN_ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower, OP_ARM64_XNSHIFT, INTRINS_AARCH64_ADV_SIMD_SQRSHRUN},
	{SN_ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar, OP_ARM64_XNSHIFT_SCALAR, INTRINS_AARCH64_ADV_SIMD_SQRSHRUN},
	{SN_ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper, OP_ARM64_XNSHIFT2, INTRINS_AARCH64_ADV_SIMD_SQRSHRUN},
	{SN_ShiftRightArithmeticRoundedNarrowingSaturateUpper, OP_ARM64_XNSHIFT2, INTRINS_AARCH64_ADV_SIMD_SQRSHRN},
	{SN_ShiftRightArithmeticRoundedScalar, OP_ARM64_SRSHR},
	{SN_ShiftRightArithmeticScalar, OP_SIMD_SSHR},
	{SN_ShiftRightLogical, OP_SIMD_USHR},
	{SN_ShiftRightLogicalAdd, OP_SIMD_USRA},
	{SN_ShiftRightLogicalAddScalar, OP_SIMD_USRA},
	{SN_ShiftRightLogicalNarrowingLower, OP_ARM64_SHRN},
	{SN_ShiftRightLogicalNarrowingSaturateLower, OP_ARM64_XNSHIFT, INTRINS_AARCH64_ADV_SIMD_UQSHRN},
	{SN_ShiftRightLogicalNarrowingSaturateScalar, OP_ARM64_XNSHIFT_SCALAR, INTRINS_AARCH64_ADV_SIMD_UQSHRN},
	{SN_ShiftRightLogicalNarrowingSaturateUpper, OP_ARM64_XNSHIFT2, INTRINS_AARCH64_ADV_SIMD_UQSHRN},
	{SN_ShiftRightLogicalNarrowingUpper, OP_ARM64_SHRN2},
	{SN_ShiftRightLogicalRounded, OP_ARM64_URSHR},
	{SN_ShiftRightLogicalRoundedAdd, OP_ARM64_URSRA},
	{SN_ShiftRightLogicalRoundedAddScalar, OP_ARM64_URSRA},
	{SN_ShiftRightLogicalRoundedNarrowingLower, OP_ARM64_XNSHIFT, INTRINS_AARCH64_ADV_SIMD_RSHRN},
	{SN_ShiftRightLogicalRoundedNarrowingSaturateLower, OP_ARM64_XNSHIFT, INTRINS_AARCH64_ADV_SIMD_UQRSHRN},
	{SN_ShiftRightLogicalRoundedNarrowingSaturateScalar, OP_ARM64_XNSHIFT_SCALAR, INTRINS_AARCH64_ADV_SIMD_UQRSHRN},
	{SN_ShiftRightLogicalRoundedNarrowingSaturateUpper, OP_ARM64_XNSHIFT2, INTRINS_AARCH64_ADV_SIMD_UQRSHRN},
	{SN_ShiftRightLogicalRoundedNarrowingUpper, OP_ARM64_XNSHIFT2, INTRINS_AARCH64_ADV_SIMD_RSHRN},
	{SN_ShiftRightLogicalRoundedScalar, OP_ARM64_URSHR},
	{SN_ShiftRightLogicalScalar, OP_SIMD_USHR},
	{SN_SignExtendWideningLower, OP_ARM64_SXTL},
	{SN_SignExtendWideningUpper, OP_ARM64_SXTL2},
	{SN_Sqrt, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FSQRT},
	{SN_SqrtScalar, OP_XOP_OVR_SCALAR_X_X, INTRINS_AARCH64_ADV_SIMD_FSQRT},
	{SN_Store, OP_ARM64_ST1},
	{SN_StorePair, OP_ARM64_STP},
	{SN_StorePairNonTemporal, OP_ARM64_STNP},
	{SN_StorePairScalar, OP_ARM64_STP_SCALAR},
	{SN_StorePairScalarNonTemporal, OP_ARM64_STNP_SCALAR},
	{SN_StoreSelectedScalar},
	{SN_StoreVector128x2},
	{SN_StoreVector128x2AndZip},
	{SN_StoreVector128x3},
	{SN_StoreVector128x3AndZip},
	{SN_StoreVector128x4},
	{SN_StoreVector128x4AndZip},
	{SN_StoreVector64x2},
	{SN_StoreVector64x2AndZip},
	{SN_StoreVector64x3},
	{SN_StoreVector64x3AndZip},
	{SN_StoreVector64x4},
	{SN_StoreVector64x4AndZip},
	{SN_Subtract, OP_XBINOP, OP_ISUB, None, None, OP_XBINOP, OP_FSUB},
	{SN_SubtractHighNarrowingLower, OP_ARM64_SUBHN},
	{SN_SubtractHighNarrowingUpper, OP_ARM64_SUBHN2},
	{SN_SubtractRoundedHighNarrowingLower, OP_ARM64_RSUBHN},
	{SN_SubtractRoundedHighNarrowingUpper, OP_ARM64_RSUBHN2},
	{SN_SubtractSaturate, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQSUB, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UQSUB},
	{SN_SubtractSaturateScalar, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_SQSUB, OP_XOP_OVR_SCALAR_X_X_X, INTRINS_AARCH64_ADV_SIMD_UQSUB},
	{SN_SubtractScalar, OP_XBINOP_SCALAR, OP_ISUB, None, None, OP_XBINOP_SCALAR, OP_FSUB},
	{SN_SubtractWideningLower, OP_ARM64_SSUB, None, OP_ARM64_USUB},
	{SN_SubtractWideningUpper, OP_ARM64_SSUB2, None, OP_ARM64_USUB2},
	{SN_TransposeEven, OP_ARM64_TRN1},
	{SN_TransposeOdd, OP_ARM64_TRN2},
	{SN_UnzipEven, OP_ARM64_UZP1},
	{SN_UnzipOdd, OP_ARM64_UZP2},
	{SN_VectorTableLookup},
	{SN_VectorTableLookupExtension},
	{SN_Xor, OP_XBINOP_FORCEINT, XBINOP_FORCEINT_XOR},
	{SN_ZeroExtendWideningLower, OP_ARM64_UXTL},
	{SN_ZeroExtendWideningUpper, OP_ARM64_UXTL2},
	{SN_ZipHigh, OP_ARM64_ZIP2},
	{SN_ZipLow, OP_ARM64_ZIP1},
	{SN_get_IsSupported},
};

static const SimdIntrinsic rdm_methods [] = {
	{SN_MultiplyRoundedDoublingAndAddSaturateHigh, OP_ARM64_SQRDMLAH},
	{SN_MultiplyRoundedDoublingAndAddSaturateHighScalar, OP_ARM64_SQRDMLAH_SCALAR},
	{SN_MultiplyRoundedDoublingAndSubtractSaturateHigh, OP_ARM64_SQRDMLSH},
	{SN_MultiplyRoundedDoublingAndSubtractSaturateHighScalar, OP_ARM64_SQRDMLSH_SCALAR},
	{SN_MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh},
	{SN_MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh},
	{SN_MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh},
	{SN_MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh},
	{SN_get_IsSupported},
};

static const SimdIntrinsic dp_methods [] = {
	{SN_DotProduct, OP_XOP_OVR_X_X_X_X, INTRINS_AARCH64_ADV_SIMD_SDOT, OP_XOP_OVR_X_X_X_X, INTRINS_AARCH64_ADV_SIMD_UDOT},
	{SN_DotProductBySelectedQuadruplet},
	{SN_get_IsSupported},
};

static const IntrinGroup supported_arm_intrinsics [] = {
	{ "AdvSimd", MONO_CPU_ARM64_NEON, advsimd_methods, sizeof (advsimd_methods) },
	{ "Aes", MONO_CPU_ARM64_CRYPTO, crypto_aes_methods, sizeof (crypto_aes_methods) },
	{ "ArmBase", MONO_CPU_ARM64_BASE, armbase_methods, sizeof (armbase_methods), TRUE },
	{ "Crc32", MONO_CPU_ARM64_CRC, crc32_methods, sizeof (crc32_methods), TRUE },
	{ "Dp", MONO_CPU_ARM64_DP, dp_methods, sizeof (dp_methods), TRUE },
	{ "Rdm", MONO_CPU_ARM64_RDM, rdm_methods, sizeof (rdm_methods) },
	{ "Sha1", MONO_CPU_ARM64_CRYPTO, sha1_methods, sizeof (sha1_methods) },
	{ "Sha256", MONO_CPU_ARM64_CRYPTO, sha256_methods, sizeof (sha256_methods) },
	{ "Sve", MONO_CPU_ARM64_SVE, unsupported, sizeof (unsupported) },
};

static MonoInst*
emit_arm64_intrinsics (
	MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args,
	MonoClass *klass, const IntrinGroup *intrin_group,
	const SimdIntrinsic *info, int id, MonoTypeEnum arg0_type,
	gboolean is_64bit)
{
	MonoCPUFeatures feature = intrin_group->feature;

	gboolean arg0_i32 = (arg0_type == MONO_TYPE_I4) || (arg0_type == MONO_TYPE_U4);
#if TARGET_SIZEOF_VOID_P == 4
	arg0_i32 = arg0_i32 || (arg0_type == MONO_TYPE_I) || (arg0_type == MONO_TYPE_U);
#endif

	if (feature == MONO_CPU_ARM64_BASE) {
		switch (id) {
		case SN_LeadingZeroCount:
			return emit_simd_ins_for_sig (cfg, klass, arg0_i32 ? OP_LZCNT32 : OP_LZCNT64, 0, arg0_type, fsig, args);
		case SN_LeadingSignCount:
			return emit_simd_ins_for_sig (cfg, klass, arg0_i32 ? OP_LSCNT32 : OP_LSCNT64, 0, arg0_type, fsig, args);
		case SN_MultiplyHigh:
			return emit_simd_ins_for_sig (cfg, klass,
				(arg0_type == MONO_TYPE_I8 ? OP_ARM64_SMULH : OP_ARM64_UMULH), 0, arg0_type, fsig, args);
		case SN_ReverseElementBits:
			return emit_simd_ins_for_sig (cfg, klass,
				(is_64bit ? OP_XOP_I8_I8 : OP_XOP_I4_I4),
				(is_64bit ? INTRINS_BITREVERSE_I64 : INTRINS_BITREVERSE_I32),
				arg0_type, fsig, args);
		case SN_Yield: {
			MonoInst* ins;
			MONO_INST_NEW (cfg, ins, OP_ARM64_HINT);
			ins->inst_c0 = ARMHINT_YIELD;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		}
			
		default:
			g_assert_not_reached (); // if a new API is added we need to either implement it or change IsSupported to false
		}
	}

	if (feature == MONO_CPU_ARM64_CRC) {
		switch (id) {
		case SN_ComputeCrc32:
		case SN_ComputeCrc32C: {
			IntrinsicId op = (IntrinsicId)0;
			gboolean is_c = info->id == SN_ComputeCrc32C;
			switch (get_underlying_type (fsig->params [1])) {
			case MONO_TYPE_U1: op = is_c ? INTRINS_AARCH64_CRC32CB : INTRINS_AARCH64_CRC32B; break;
			case MONO_TYPE_U2: op = is_c ? INTRINS_AARCH64_CRC32CH : INTRINS_AARCH64_CRC32H; break;
			case MONO_TYPE_U4: op = is_c ? INTRINS_AARCH64_CRC32CW : INTRINS_AARCH64_CRC32W; break;
			case MONO_TYPE_U8: op = is_c ? INTRINS_AARCH64_CRC32CX : INTRINS_AARCH64_CRC32X; break;
			default: g_assert_not_reached (); break;
			}
			return emit_simd_ins_for_sig (cfg, klass, is_64bit ? OP_XOP_I4_I4_I8 : OP_XOP_I4_I4_I4, op, arg0_type, fsig, args);
		}
		default:
			g_assert_not_reached (); // if a new API is added we need to either implement it or change IsSupported to false
		}
	}

	if (feature == MONO_CPU_ARM64_NEON) {
		switch (id) {
		case SN_AbsoluteCompareGreaterThan:
		case SN_AbsoluteCompareGreaterThanOrEqual:
		case SN_AbsoluteCompareLessThan:
		case SN_AbsoluteCompareLessThanOrEqual:
		case SN_AbsoluteCompareGreaterThanScalar:
		case SN_AbsoluteCompareGreaterThanOrEqualScalar:
		case SN_AbsoluteCompareLessThanScalar:
		case SN_AbsoluteCompareLessThanOrEqualScalar: {
			gboolean reverse_args = FALSE;
			gboolean use_geq = FALSE;
			gboolean scalar = FALSE;
			MonoInst *cmp_args [] = { args [0], args [1] };
			switch (id) {
			case SN_AbsoluteCompareGreaterThanScalar: scalar = TRUE;
			case SN_AbsoluteCompareGreaterThan: break;

			case SN_AbsoluteCompareGreaterThanOrEqualScalar: scalar = TRUE;
			case SN_AbsoluteCompareGreaterThanOrEqual: use_geq = TRUE; break;

			case SN_AbsoluteCompareLessThanScalar: scalar = TRUE;
			case SN_AbsoluteCompareLessThan: reverse_args = TRUE; break;

			case SN_AbsoluteCompareLessThanOrEqualScalar: scalar = TRUE;
			case SN_AbsoluteCompareLessThanOrEqual: reverse_args = TRUE; use_geq = TRUE; break;
			}
			if (reverse_args) {
				cmp_args [0] = args [1];
				cmp_args [1] = args [0];
			}
			int iid = use_geq ? INTRINS_AARCH64_ADV_SIMD_FACGE : INTRINS_AARCH64_ADV_SIMD_FACGT;
			return emit_simd_ins_for_sig (cfg, klass, OP_ARM64_ABSCOMPARE, iid, scalar, fsig, cmp_args);
		}
		case SN_AddSaturate:
		case SN_AddSaturateScalar: {
			gboolean arg0_unsigned = type_is_unsigned (fsig->params [0]);
			gboolean arg1_unsigned = type_is_unsigned (fsig->params [1]);
			int iid = 0;
			if (arg0_unsigned && arg1_unsigned)
				iid = INTRINS_AARCH64_ADV_SIMD_UQADD;
			else if (arg0_unsigned && !arg1_unsigned)
				iid = INTRINS_AARCH64_ADV_SIMD_USQADD;
			else if (!arg0_unsigned && arg1_unsigned)
				iid = INTRINS_AARCH64_ADV_SIMD_SUQADD;
			else
				iid = INTRINS_AARCH64_ADV_SIMD_SQADD;
			int op = id == SN_AddSaturateScalar ? OP_XOP_OVR_SCALAR_X_X_X : OP_XOP_OVR_X_X_X;
			return emit_simd_ins_for_sig (cfg, klass, op, iid, arg0_type, fsig, args);
		}
		case SN_DuplicateSelectedScalarToVector128:
		case SN_DuplicateSelectedScalarToVector64:
		case SN_DuplicateToVector64:
		case SN_DuplicateToVector128: {
			MonoClass *ret_klass = mono_class_from_mono_type_internal (fsig->ret);
			MonoType *rtype = get_vector_t_elem_type (fsig->ret);
			int scalar_src_reg = args [0]->dreg;
			switch (id) {
			case SN_DuplicateSelectedScalarToVector128:
			case SN_DuplicateSelectedScalarToVector64: {
				MonoInst *ins = emit_simd_ins (cfg, ret_klass, type_to_xextract_op (rtype->type), args [0]->dreg, args [1]->dreg);
				ins->inst_c1 = arg0_type;
				scalar_src_reg = ins->dreg;
				break;
			}
			}
			return emit_simd_ins (cfg, ret_klass, type_to_expand_op (rtype->type), scalar_src_reg, -1);
		}
		case SN_Extract: {
			int extract_op = type_to_xextract_op (arg0_type);
			MonoInst *ins = emit_simd_ins (cfg, klass, extract_op, args [0]->dreg, args [1]->dreg);
			ins->inst_c1 = arg0_type;
			return ins;
		}
		case SN_LoadAndInsertScalar: {
			int load_op;
			if (is_intrinsics_vector_type (fsig->params [0]))
				load_op = OP_ARM64_LD1_INSERT;
			else
				load_op = OP_ARM64_LDM_INSERT;
			return emit_simd_ins_for_sig (cfg, klass, load_op, 0, arg0_type, fsig, args);
		}
		case SN_InsertSelectedScalar:
		case SN_InsertScalar:
		case SN_Insert: {
			MonoClass *ret_klass = mono_class_from_mono_type_internal (fsig->ret);
			int insert_op = type_to_xinsert_op (arg0_type);
			int extract_op = type_to_extract_op (arg0_type);
			int val_src_reg = args [2]->dreg;
			switch (id) {
			case SN_InsertSelectedScalar: {
				MonoInst *scalar = emit_simd_ins (cfg, klass, OP_ARM64_SELECT_SCALAR, args [2]->dreg, args [3]->dreg);
				val_src_reg = scalar->dreg;
				// fallthrough
			}
			case SN_InsertScalar: {
				MonoInst *ins = emit_simd_ins (cfg, klass, extract_op, val_src_reg, -1);
				ins->inst_c0 = 0;
				ins->inst_c1 = arg0_type;
				val_src_reg = ins->dreg;
				break;
			}
			}
			MonoInst *ins = emit_simd_ins (cfg, ret_klass, insert_op, args [0]->dreg, val_src_reg);
			ins->sreg3 = args [1]->dreg;
			ins->inst_c1 = arg0_type;
			return ins;
		}
		case SN_ShiftLeftLogicalSaturate:
		case SN_ShiftLeftLogicalSaturateScalar: {
			MonoClass *ret_klass = mono_class_from_mono_type_internal (fsig->ret);
			MonoType *etype = get_vector_t_elem_type (fsig->ret);
			gboolean is_unsigned = type_is_unsigned (fsig->ret);
			gboolean scalar = id == SN_ShiftLeftLogicalSaturateScalar;
			int s2v = scalar ? OP_CREATE_SCALAR_UNSAFE : type_to_expand_op (etype->type);
			int xop = scalar ? OP_XOP_OVR_SCALAR_X_X_X : OP_XOP_OVR_X_X_X;
			int iid = is_unsigned ? INTRINS_AARCH64_ADV_SIMD_UQSHL : INTRINS_AARCH64_ADV_SIMD_SQSHL;
			MonoInst *shift_vector = emit_simd_ins (cfg, ret_klass, s2v, args [1]->dreg, -1);
			shift_vector->inst_c1 = etype->type;
			MonoInst *ret = emit_simd_ins (cfg, ret_klass, xop, args [0]->dreg, shift_vector->dreg);
			ret->inst_c0 = iid;
			ret->inst_c1 = etype->type;
			return ret;
		}
		case SN_StoreSelectedScalar: {
			int store_op;
			if (is_intrinsics_vector_type (fsig->params [1]))
				store_op = OP_ARM64_ST1_SCALAR;
			else
				store_op = OP_ARM64_STM_SCALAR;
			MonoClass* klass_tuple_var = mono_class_from_mono_type_internal (fsig->params [1]);
			return emit_simd_ins_for_sig (cfg, klass_tuple_var, store_op, 0, arg0_type, fsig, args);
		}
		case SN_MultiplyRoundedDoublingBySelectedScalarSaturateHigh:
		case SN_MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh:
		case SN_MultiplyDoublingScalarBySelectedScalarSaturateHigh:
		case SN_MultiplyDoublingWideningSaturateScalarBySelectedScalar:
		case SN_MultiplyExtendedBySelectedScalar:
		case SN_MultiplyExtendedScalarBySelectedScalar:
		case SN_MultiplyBySelectedScalar:
		case SN_MultiplyBySelectedScalarWideningLower:
		case SN_MultiplyBySelectedScalarWideningUpper:
		case SN_MultiplyDoublingBySelectedScalarSaturateHigh:
		case SN_MultiplyDoublingWideningSaturateLowerBySelectedScalar:
		case SN_MultiplyDoublingWideningSaturateUpperBySelectedScalar: {
			MonoClass *ret_klass = mono_class_from_mono_type_internal (fsig->ret);
			gboolean is_unsigned = type_is_unsigned (fsig->ret);
			gboolean is_float = type_is_float (fsig->ret);
			int opcode = 0;
			int c0 = 0;
			switch (id) {
			case SN_MultiplyRoundedDoublingBySelectedScalarSaturateHigh: opcode = OP_XOP_OVR_BYSCALAR_X_X_X; c0 = INTRINS_AARCH64_ADV_SIMD_SQRDMULH; break;
			case SN_MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh: opcode = OP_XOP_OVR_SCALAR_X_X_X; c0 = INTRINS_AARCH64_ADV_SIMD_SQRDMULH; break;
			case SN_MultiplyDoublingScalarBySelectedScalarSaturateHigh: opcode = OP_XOP_OVR_SCALAR_X_X_X; c0 = INTRINS_AARCH64_ADV_SIMD_SQDMULH; break;
			case SN_MultiplyDoublingWideningSaturateScalarBySelectedScalar: opcode = OP_ARM64_SQDMULL_SCALAR; break;
			case SN_MultiplyExtendedBySelectedScalar: opcode = OP_XOP_OVR_BYSCALAR_X_X_X; c0 = INTRINS_AARCH64_ADV_SIMD_FMULX; break;
			case SN_MultiplyExtendedScalarBySelectedScalar: opcode = OP_XOP_OVR_SCALAR_X_X_X; c0 = INTRINS_AARCH64_ADV_SIMD_FMULX; break;
			case SN_MultiplyBySelectedScalar: opcode = OP_XBINOP_BYSCALAR; c0 = OP_IMUL; break;
			case SN_MultiplyBySelectedScalarWideningLower: opcode = OP_ARM64_SMULL_SCALAR; break;
			case SN_MultiplyBySelectedScalarWideningUpper: opcode = OP_ARM64_SMULL2_SCALAR; break;
			case SN_MultiplyDoublingBySelectedScalarSaturateHigh: opcode = OP_XOP_OVR_BYSCALAR_X_X_X; c0 = INTRINS_AARCH64_ADV_SIMD_SQDMULH; break;
			case SN_MultiplyDoublingWideningSaturateLowerBySelectedScalar: opcode = OP_ARM64_SQDMULL_BYSCALAR; break;
			case SN_MultiplyDoublingWideningSaturateUpperBySelectedScalar: opcode = OP_ARM64_SQDMULL2_BYSCALAR; break;
			default: g_assert_not_reached();
			}
			if (is_unsigned)
				switch (opcode) {
				case OP_ARM64_SMULL_SCALAR: opcode = OP_ARM64_UMULL_SCALAR; break;
				case OP_ARM64_SMULL2_SCALAR: opcode = OP_ARM64_UMULL2_SCALAR; break;
				}
			if (is_float)
				switch (opcode) {
				case OP_XBINOP_BYSCALAR: c0 = OP_FMUL;
				}
			MonoInst *scalar = emit_simd_ins (cfg, ret_klass, OP_ARM64_SELECT_SCALAR, args [1]->dreg, args [2]->dreg);
			MonoInst *ret = emit_simd_ins (cfg, ret_klass, opcode, args [0]->dreg, scalar->dreg);
			ret->inst_c0 = c0;
			ret->inst_c1 = arg0_type;
			return ret;
		}
		case SN_FusedMultiplyAddBySelectedScalar:
		case SN_FusedMultiplyAddScalarBySelectedScalar:
		case SN_FusedMultiplySubtractBySelectedScalar:
		case SN_FusedMultiplySubtractScalarBySelectedScalar:
		case SN_MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate:
		case SN_MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate:
		case SN_MultiplyAddBySelectedScalar:
		case SN_MultiplySubtractBySelectedScalar:
		case SN_MultiplyBySelectedScalarWideningLowerAndAdd:
		case SN_MultiplyBySelectedScalarWideningLowerAndSubtract:
		case SN_MultiplyBySelectedScalarWideningUpperAndAdd:
		case SN_MultiplyBySelectedScalarWideningUpperAndSubtract:
		case SN_MultiplyDoublingWideningLowerBySelectedScalarAndAddSaturate:
		case SN_MultiplyDoublingWideningLowerBySelectedScalarAndSubtractSaturate:
		case SN_MultiplyDoublingWideningUpperBySelectedScalarAndAddSaturate:
		case SN_MultiplyDoublingWideningUpperBySelectedScalarAndSubtractSaturate: {
			MonoClass *ret_klass = mono_class_from_mono_type_internal (fsig->ret);
			gboolean is_unsigned = type_is_unsigned (fsig->ret);
			int opcode = 0;
			switch (id) {
			case SN_FusedMultiplyAddBySelectedScalar: opcode = OP_ARM64_FMADD_BYSCALAR; break;
			case SN_FusedMultiplyAddScalarBySelectedScalar: opcode = OP_ARM64_FMADD_SCALAR; break;
			case SN_FusedMultiplySubtractBySelectedScalar: opcode = OP_ARM64_FMSUB_BYSCALAR; break;
			case SN_FusedMultiplySubtractScalarBySelectedScalar: opcode = OP_ARM64_FMSUB_SCALAR; break;
			case SN_MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate: opcode = OP_ARM64_SQDMLAL_SCALAR; break;
			case SN_MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate: opcode = OP_ARM64_SQDMLSL_SCALAR; break;
			case SN_MultiplyAddBySelectedScalar: opcode = OP_ARM64_MLA_SCALAR; break;
			case SN_MultiplySubtractBySelectedScalar: opcode = OP_ARM64_MLS_SCALAR; break;
			case SN_MultiplyBySelectedScalarWideningLowerAndAdd: opcode = OP_ARM64_SMLAL_SCALAR; break;
			case SN_MultiplyBySelectedScalarWideningLowerAndSubtract: opcode = OP_ARM64_SMLSL_SCALAR; break;
			case SN_MultiplyBySelectedScalarWideningUpperAndAdd: opcode = OP_ARM64_SMLAL2_SCALAR; break;
			case SN_MultiplyBySelectedScalarWideningUpperAndSubtract: opcode = OP_ARM64_SMLSL2_SCALAR; break;
			case SN_MultiplyDoublingWideningLowerBySelectedScalarAndAddSaturate: opcode = OP_ARM64_SQDMLAL_BYSCALAR; break;
			case SN_MultiplyDoublingWideningLowerBySelectedScalarAndSubtractSaturate: opcode = OP_ARM64_SQDMLSL_BYSCALAR; break;
			case SN_MultiplyDoublingWideningUpperBySelectedScalarAndAddSaturate: opcode = OP_ARM64_SQDMLAL2_BYSCALAR; break;
			case SN_MultiplyDoublingWideningUpperBySelectedScalarAndSubtractSaturate: opcode = OP_ARM64_SQDMLSL2_BYSCALAR; break;
			default: g_assert_not_reached();
			}
			if (is_unsigned)
				switch (opcode) {
				case OP_ARM64_SMLAL_SCALAR: opcode = OP_ARM64_UMLAL_SCALAR; break;
				case OP_ARM64_SMLSL_SCALAR: opcode = OP_ARM64_UMLSL_SCALAR; break;
				case OP_ARM64_SMLAL2_SCALAR: opcode = OP_ARM64_UMLAL2_SCALAR; break;
				case OP_ARM64_SMLSL2_SCALAR: opcode = OP_ARM64_UMLSL2_SCALAR; break;
				}
			MonoInst *scalar = emit_simd_ins (cfg, ret_klass, OP_ARM64_SELECT_SCALAR, args [2]->dreg, args [3]->dreg);
			MonoInst *ret = emit_simd_ins (cfg, ret_klass, opcode, args [0]->dreg, args [1]->dreg);
			ret->sreg3 = scalar->dreg;
			return ret;
		}
        case SN_VectorTableLookup:
        case SN_VectorTableLookupExtension: {
			if (type_is_simd_vector (fsig->params [0]) && type_is_simd_vector (fsig->params [1])) {
				if (id == SN_VectorTableLookup)
					return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X_X, INTRINS_AARCH64_ADV_SIMD_TBL1, 0, fsig, args);
				else
					return emit_simd_ins_for_sig (cfg, klass, OP_XOP_OVR_X_X_X_X, INTRINS_AARCH64_ADV_SIMD_TBX1, 0, fsig, args);
			}

			MonoInst *ins, *addr;
			int tuple_argindex;

			if (id == SN_VectorTableLookup)
				/* VectorTableLookup((Vector128<sbyte>, Vector128<sbyte>) table, Vector128<sbyte> byteIndexes) */
				tuple_argindex = 0;
			else
				/* VectorTableLookupExtension(Vector128<byte> defaultValues, (Vector128<byte>, Vector128<byte>) table, Vector128<byte> byteIndexes */
				tuple_argindex = 1;

			/*
			 * These intrinsics have up to 5 inputs, and our IR can't model that, so save the inputs to the stack and have
			 * the LLVM implementation read them back.
			 */
			MonoType *tuple_type = fsig->params [tuple_argindex];
			g_assert (tuple_type->type == MONO_TYPE_GENERICINST);
			MonoClass *tclass = mono_class_from_mono_type_internal (tuple_type);
			mono_class_init_internal (tclass);

			MonoClassField *fields = m_class_get_fields (tclass);
			int nfields = mono_class_get_field_count (tclass);
			guint32 *offsets = mono_mempool_alloc0 (cfg->mempool, nfields * sizeof (guint32));
			for (uint32_t i = 0; i < mono_class_get_field_count (tclass); ++i)
				offsets [i] = mono_field_get_offset (&fields [i]) - MONO_ABI_SIZEOF (MonoObject);

			int vreg = alloc_xreg (cfg);
			NEW_VARLOADA_VREG (cfg, addr, vreg, tuple_type);
			MONO_ADD_INS (cfg->cbb, addr);

			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, tuple_type, addr->dreg, 0, args [tuple_argindex]->dreg);

			MONO_INST_NEW (cfg, ins, id == SN_VectorTableLookup ? OP_ARM64_TBL_INDIRECT : OP_ARM64_TBX_INDIRECT);
			ins->dreg = alloc_xreg (cfg);
			ins->sreg1 = addr->dreg;
			if (id == SN_VectorTableLookup) {
				/* byteIndexes */
				ins->sreg2 = args [1]->dreg;
			} else {
				/* defaultValues */
				ins->sreg2 = args [0]->dreg;
				/* byteIndexes */
				ins->sreg3 = args [2]->dreg;
			}
			ins->inst_c0 = nfields;
			ins->inst_p1 = offsets;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		}
		case SN_StoreVector128x2:
		case SN_StoreVector128x3:
		case SN_StoreVector128x4:
		case SN_StoreVector64x2:
		case SN_StoreVector64x3:
		case SN_StoreVector64x4:
		case SN_StoreVector128x2AndZip:
		case SN_StoreVector128x3AndZip:
		case SN_StoreVector128x4AndZip:
		case SN_StoreVector64x2AndZip:
		case SN_StoreVector64x3AndZip:
		case SN_StoreVector64x4AndZip: {
			int iid = 0;
			switch (id) {
			case SN_StoreVector128x2: iid = INTRINS_AARCH64_ADV_SIMD_ST1X2_V128; break;
			case SN_StoreVector128x3: iid = INTRINS_AARCH64_ADV_SIMD_ST1X3_V128; break;
			case SN_StoreVector128x4: iid = INTRINS_AARCH64_ADV_SIMD_ST1X4_V128; break;
			case SN_StoreVector64x2: iid = INTRINS_AARCH64_ADV_SIMD_ST1X2_V64; break;
			case SN_StoreVector64x3: iid = INTRINS_AARCH64_ADV_SIMD_ST1X3_V64; break;
			case SN_StoreVector64x4: iid = INTRINS_AARCH64_ADV_SIMD_ST1X4_V64; break;
			case SN_StoreVector128x2AndZip: iid = INTRINS_AARCH64_ADV_SIMD_ST2_V128; break;
			case SN_StoreVector128x3AndZip: iid = INTRINS_AARCH64_ADV_SIMD_ST3_V128; break;
			case SN_StoreVector128x4AndZip: iid = INTRINS_AARCH64_ADV_SIMD_ST4_V128; break;
			case SN_StoreVector64x2AndZip: iid = INTRINS_AARCH64_ADV_SIMD_ST2_V64; break;
			case SN_StoreVector64x3AndZip: iid = INTRINS_AARCH64_ADV_SIMD_ST3_V64; break;
			case SN_StoreVector64x4AndZip: iid = INTRINS_AARCH64_ADV_SIMD_ST4_V64; break;
			default: g_assert_not_reached ();
			}

			MonoClass* klass_tuple_var = mono_class_from_mono_type_internal (fsig->params [1]);
			return emit_simd_ins_for_sig (cfg, klass_tuple_var, OP_ARM64_STM, iid, arg0_type, fsig, args);
		}
		default:
			g_assert_not_reached ();
		}
	}

	if (feature == MONO_CPU_ARM64_CRYPTO) {
		switch (id) {
		case SN_PolynomialMultiplyWideningLower:
			return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_AARCH64_PMULL64, 0, fsig, args);
		case SN_PolynomialMultiplyWideningUpper:
			return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_AARCH64_PMULL64, 1, fsig, args);
		default:
			g_assert_not_reached ();
		}
	}

	if (feature == MONO_CPU_ARM64_RDM) {
		switch (id) {
		case SN_MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh:
		case SN_MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh:
		case SN_MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh:
		case SN_MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh: {
			MonoClass *ret_klass = mono_class_from_mono_type_internal (fsig->ret);
			int opcode = 0;
			switch (id) {
			case SN_MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh: opcode = OP_ARM64_SQRDMLAH_BYSCALAR; break;
			case SN_MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh: opcode = OP_ARM64_SQRDMLSH_BYSCALAR; break;
			case SN_MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh: opcode = OP_ARM64_SQRDMLAH_SCALAR; break;
			case SN_MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh: opcode = OP_ARM64_SQRDMLSH_SCALAR; break;
			}
			MonoInst *scalar = emit_simd_ins (cfg, ret_klass, OP_ARM64_SELECT_SCALAR, args [2]->dreg, args [3]->dreg);
			MonoInst *ret = emit_simd_ins (cfg, ret_klass, opcode, args [0]->dreg, args [1]->dreg);
			ret->inst_c1 = arg0_type;
			ret->sreg3 = scalar->dreg;
			return ret;
		}
		default:
			g_assert_not_reached ();
		}
	}

	if (feature == MONO_CPU_ARM64_DP) {
		switch (id) {
		case SN_DotProductBySelectedQuadruplet: {
			MonoClass *ret_klass = mono_class_from_mono_type_internal (fsig->ret);
			MonoClass *arg_klass = mono_class_from_mono_type_internal (fsig->params [1]);
			MonoClass *quad_klass = mono_class_from_mono_type_internal (fsig->params [2]);
			gboolean is_unsigned = type_is_unsigned (fsig->ret);
			int iid = is_unsigned ? INTRINS_AARCH64_ADV_SIMD_UDOT : INTRINS_AARCH64_ADV_SIMD_SDOT;

			MonoInst *quad;
			if (!COMPILE_LLVM (cfg)) {
				if (mono_class_value_size (arg_klass, NULL) != 16 || mono_class_value_size (quad_klass, NULL) != 16)
					return NULL;
				// FIXME: The c# api has ConstantExpected(Max = (byte)(15)), but the hw only supports
				// selecting one of the 4 32 bit words
				if (args [3]->opcode != OP_ICONST || args [3]->inst_c0 < 0 || args [3]->inst_c0 > 3) {
					// FIXME: Throw the right exception ?
					mono_emit_jit_icall (cfg, mono_throw_platform_not_supported, NULL);
					return NULL;
				}
				quad = emit_simd_ins (cfg, klass, OP_ARM64_BROADCAST_ELEM, args [2]->dreg, -1);
				quad->inst_c0 = args [3]->inst_c0;
			} else {
				quad = emit_simd_ins (cfg, arg_klass, OP_ARM64_SELECT_QUAD, args [2]->dreg, args [3]->dreg);
				quad->data.op [1].klass = quad_klass;
			}
			MonoInst *ret = emit_simd_ins (cfg, ret_klass, OP_XOP_OVR_X_X_X_X, args [0]->dreg, args [1]->dreg);
			ret->sreg3 = quad->dreg;
			ret->inst_c0 = iid;
			return ret;
		}
		default:
			g_assert_not_reached ();
		}
	}

	return NULL;
}
#endif // TARGET_ARM64

#ifdef TARGET_AMD64

static SimdIntrinsic sse_methods [] = {
	{SN_Add, OP_XBINOP, OP_FADD},
	{SN_AddScalar, OP_SSE_ADDSS},
	{SN_And, OP_SSE_AND},
	{SN_AndNot, OP_VECTOR_ANDN},
	{SN_CompareEqual, OP_XCOMPARE_FP, CMP_EQ},
	{SN_CompareGreaterThan, OP_XCOMPARE_FP,CMP_GT},
	{SN_CompareGreaterThanOrEqual, OP_XCOMPARE_FP, CMP_GE},
	{SN_CompareLessThan, OP_XCOMPARE_FP, CMP_LT},
	{SN_CompareLessThanOrEqual, OP_XCOMPARE_FP, CMP_LE},
	{SN_CompareNotEqual, OP_XCOMPARE_FP, CMP_NE},
	{SN_CompareNotGreaterThan, OP_XCOMPARE_FP, CMP_LE_UN},
	{SN_CompareNotGreaterThanOrEqual, OP_XCOMPARE_FP, CMP_LT_UN},
	{SN_CompareNotLessThan, OP_XCOMPARE_FP, CMP_GE_UN},
	{SN_CompareNotLessThanOrEqual, OP_XCOMPARE_FP, CMP_GT_UN},
	{SN_CompareOrdered, OP_XCOMPARE_FP, CMP_ORD},
	{SN_CompareScalarEqual, OP_SSE_CMPSS, CMP_EQ},
	{SN_CompareScalarGreaterThan, OP_SSE_CMPSS, CMP_GT},
	{SN_CompareScalarGreaterThanOrEqual, OP_SSE_CMPSS, CMP_GE},
	{SN_CompareScalarLessThan, OP_SSE_CMPSS, CMP_LT},
	{SN_CompareScalarLessThanOrEqual, OP_SSE_CMPSS, CMP_LE},
	{SN_CompareScalarNotEqual, OP_SSE_CMPSS, CMP_NE},
	{SN_CompareScalarNotGreaterThan, OP_SSE_CMPSS, CMP_LE_UN},
	{SN_CompareScalarNotGreaterThanOrEqual, OP_SSE_CMPSS, CMP_LT_UN},
	{SN_CompareScalarNotLessThan, OP_SSE_CMPSS, CMP_GE_UN},
	{SN_CompareScalarNotLessThanOrEqual, OP_SSE_CMPSS, CMP_GT_UN},
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
	{SN_ConvertToInt32, OP_XOP_I4_X, INTRINS_SSE_CVTSS2SI},
	{SN_ConvertToInt32WithTruncation, OP_XOP_I4_X, INTRINS_SSE_CVTTSS2SI},
	{SN_ConvertToInt64, OP_XOP_I8_X, INTRINS_SSE_CVTSS2SI64},
	{SN_ConvertToInt64WithTruncation, OP_XOP_I8_X, INTRINS_SSE_CVTTSS2SI64},
	{SN_Divide, OP_XBINOP, OP_FDIV},
	{SN_DivideScalar, OP_SSE_DIVSS},
	{SN_LoadAlignedVector128, OP_SSE_LOADU, 16 /* alignment */},
	{SN_LoadHigh, OP_SSE_MOVHPS_LOAD},
	{SN_LoadLow, OP_SSE_MOVLPS_LOAD},
	{SN_LoadScalarVector128, OP_SSE_MOVSS},
	{SN_LoadVector128, OP_SSE_LOADU, 1 /* alignment */},
	{SN_Max, OP_XOP_X_X_X, INTRINS_SSE_MAXPS},
	{SN_MaxScalar, OP_XOP_X_X_X, INTRINS_SSE_MAXSS},
	{SN_Min, OP_XOP_X_X_X, INTRINS_SSE_MINPS},
	{SN_MinScalar, OP_XOP_X_X_X, INTRINS_SSE_MINSS},
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
	{SN_Reciprocal, OP_XOP_X_X, INTRINS_SSE_RCP_PS},
	{SN_ReciprocalScalar},
	{SN_ReciprocalSqrt, OP_XOP_X_X, INTRINS_SSE_RSQRT_PS},
	{SN_ReciprocalSqrtScalar},
	{SN_Shuffle},
	{SN_Sqrt, OP_XOP_X_X, INTRINS_SIMD_SQRT_R4},
	{SN_SqrtScalar},
	{SN_Store, OP_SIMD_STORE, 1 /* alignment */},
	{SN_StoreAligned, OP_SIMD_STORE, 16 /* alignment */},
	{SN_StoreAlignedNonTemporal, OP_SSE_MOVNTPS, 16 /* alignment */},
	{SN_StoreFence, OP_XOP, INTRINS_SSE_SFENCE},
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

static SimdIntrinsic sse2_methods [] = {
	{SN_Add},
	{SN_AddSaturate, OP_SSE2_ADDS},
	{SN_AddScalar, OP_SSE2_ADDSD},
	{SN_And, OP_SSE_AND},
	{SN_AndNot, OP_VECTOR_ANDN},
	{SN_Average},
	{SN_CompareEqual},
	{SN_CompareGreaterThan},
	{SN_CompareGreaterThanOrEqual, OP_XCOMPARE_FP, CMP_GE},
	{SN_CompareLessThan},
	{SN_CompareLessThanOrEqual, OP_XCOMPARE_FP, CMP_LE},
	{SN_CompareNotEqual, OP_XCOMPARE_FP, CMP_NE},
	{SN_CompareNotGreaterThan, OP_XCOMPARE_FP, CMP_LE_UN},
	{SN_CompareNotGreaterThanOrEqual, OP_XCOMPARE_FP, CMP_LT_UN},
	{SN_CompareNotLessThan, OP_XCOMPARE_FP, CMP_GE_UN},
	{SN_CompareNotLessThanOrEqual, OP_XCOMPARE_FP, CMP_GT_UN},
	{SN_CompareOrdered, OP_XCOMPARE_FP, CMP_ORD},
	{SN_CompareScalarEqual, OP_SSE2_CMPSD, CMP_EQ},
	{SN_CompareScalarGreaterThan, OP_SSE2_CMPSD, CMP_GT},
	{SN_CompareScalarGreaterThanOrEqual, OP_SSE2_CMPSD, CMP_GE},
	{SN_CompareScalarLessThan, OP_SSE2_CMPSD, CMP_LT},
	{SN_CompareScalarLessThanOrEqual, OP_SSE2_CMPSD, CMP_LE},
	{SN_CompareScalarNotEqual, OP_SSE2_CMPSD, CMP_NE},
	{SN_CompareScalarNotGreaterThan, OP_SSE2_CMPSD, CMP_LE_UN},
	{SN_CompareScalarNotGreaterThanOrEqual, OP_SSE2_CMPSD, CMP_LT_UN},
	{SN_CompareScalarNotLessThan, OP_SSE2_CMPSD, CMP_GE_UN},
	{SN_CompareScalarNotLessThanOrEqual, OP_SSE2_CMPSD, CMP_GT_UN},
	{SN_CompareScalarOrdered, OP_SSE2_CMPSD, CMP_ORD},
	{SN_CompareScalarOrderedEqual, OP_SSE2_COMISD, CMP_EQ},
	{SN_CompareScalarOrderedGreaterThan, OP_SSE2_COMISD, CMP_GT},
	{SN_CompareScalarOrderedGreaterThanOrEqual, OP_SSE2_COMISD, CMP_GE},
	{SN_CompareScalarOrderedLessThan, OP_SSE2_COMISD, CMP_LT},
	{SN_CompareScalarOrderedLessThanOrEqual, OP_SSE2_COMISD, CMP_LE},
	{SN_CompareScalarOrderedNotEqual, OP_SSE2_COMISD, CMP_NE},
	{SN_CompareScalarUnordered, OP_SSE2_CMPSD, CMP_UNORD},
	{SN_CompareScalarUnorderedEqual, OP_SSE2_UCOMISD, CMP_EQ},
	{SN_CompareScalarUnorderedGreaterThan, OP_SSE2_UCOMISD, CMP_GT},
	{SN_CompareScalarUnorderedGreaterThanOrEqual, OP_SSE2_UCOMISD, CMP_GE},
	{SN_CompareScalarUnorderedLessThan, OP_SSE2_UCOMISD, CMP_LT},
	{SN_CompareScalarUnorderedLessThanOrEqual, OP_SSE2_UCOMISD, CMP_LE},
	{SN_CompareScalarUnorderedNotEqual, OP_SSE2_UCOMISD, CMP_NE},
	{SN_CompareUnordered, OP_XCOMPARE_FP, CMP_UNORD},
	{SN_ConvertScalarToVector128Double},
	{SN_ConvertScalarToVector128Int32},
	{SN_ConvertScalarToVector128Int64},
	{SN_ConvertScalarToVector128Single, OP_XOP_X_X_X, INTRINS_SSE_CVTSD2SS},
	{SN_ConvertScalarToVector128UInt32},
	{SN_ConvertScalarToVector128UInt64},
	{SN_ConvertToInt32},
	{SN_ConvertToInt32WithTruncation, OP_XOP_I4_X, INTRINS_SSE_CVTTSD2SI},
	{SN_ConvertToInt64},
	{SN_ConvertToInt64WithTruncation, OP_XOP_I8_X, INTRINS_SSE_CVTTSD2SI64},
	{SN_ConvertToUInt32},
	{SN_ConvertToUInt64},
	{SN_ConvertToVector128Double},
	{SN_ConvertToVector128Int32},
	{SN_ConvertToVector128Int32WithTruncation},
	{SN_ConvertToVector128Single},
	{SN_Divide, OP_XBINOP, OP_FDIV},
	{SN_DivideScalar, OP_SSE2_DIVSD},
	{SN_Extract},
	{SN_Insert},
	{SN_LoadAlignedVector128},
	{SN_LoadFence, OP_XOP, INTRINS_SSE_LFENCE},
	{SN_LoadHigh, OP_SSE2_MOVHPD_LOAD},
	{SN_LoadLow, OP_SSE2_MOVLPD_LOAD},
	{SN_LoadScalarVector128},
	{SN_LoadVector128},
	{SN_MaskMove, OP_SSE2_MASKMOVDQU},
	{SN_Max},
	{SN_MaxScalar, OP_XOP_X_X_X, INTRINS_SSE_MAXSD},
	{SN_MemoryFence, OP_XOP, INTRINS_SSE_MFENCE},
	{SN_Min}, // FIXME:
	{SN_MinScalar, OP_XOP_X_X_X, INTRINS_SSE_MINSD},
	{SN_MoveMask, OP_SSE_MOVMSK},
	{SN_MoveScalar},
	{SN_Multiply},
	{SN_MultiplyAddAdjacent, OP_XOP_X_X_X, INTRINS_SSE_PMADDWD},
	{SN_MultiplyHigh},
	{SN_MultiplyLow, OP_PMULW},
	{SN_MultiplyScalar, OP_SSE2_MULSD},
	{SN_Or, OP_SSE_OR},
	{SN_PackSignedSaturate},
	{SN_PackUnsignedSaturate},
	{SN_ShiftLeftLogical},
	{SN_ShiftLeftLogical128BitLane},
	{SN_ShiftRightArithmetic},
	{SN_ShiftRightLogical},
	{SN_ShiftRightLogical128BitLane},
	{SN_Shuffle},
	{SN_ShuffleHigh},
	{SN_ShuffleLow},
	{SN_Sqrt, OP_XOP_X_X, INTRINS_SIMD_SQRT_R8},
	{SN_SqrtScalar},
	{SN_Store, OP_SIMD_STORE, 1 /* alignment */},
	{SN_StoreAligned, OP_SIMD_STORE, 16 /* alignment */},
	{SN_StoreAlignedNonTemporal, OP_SSE_MOVNTPS, 16 /* alignment */},
	{SN_StoreHigh, OP_SSE2_MOVHPD_STORE},
	{SN_StoreLow, OP_SSE2_MOVLPD_STORE},
	{SN_StoreNonTemporal, OP_SSE_MOVNTPS, 1 /* alignment */},
	{SN_StoreScalar, OP_SSE_STORES},
	{SN_Subtract},
	{SN_SubtractSaturate, OP_SSE2_SUBS},
	{SN_SubtractScalar, OP_SSE2_SUBSD},
	{SN_SumAbsoluteDifferences, OP_XOP_X_X_X, INTRINS_SSE_PSADBW},
	{SN_UnpackHigh, OP_SSE_UNPACKHI},
	{SN_UnpackLow, OP_SSE_UNPACKLO},
	{SN_Xor, OP_SSE_XOR},
	{SN_get_IsSupported}
};

static SimdIntrinsic sse3_methods [] = {
	{SN_AddSubtract},
	{SN_HorizontalAdd},
	{SN_HorizontalSubtract},
	{SN_LoadAndDuplicateToVector128, OP_SSE3_MOVDDUP_MEM},
	{SN_LoadDquVector128, OP_XOP_X_I, INTRINS_SSE_LDU_DQ},
	{SN_MoveAndDuplicate, OP_SSE3_MOVDDUP},
	{SN_MoveHighAndDuplicate, OP_SSE3_MOVSHDUP},
	{SN_MoveLowAndDuplicate, OP_SSE3_MOVSLDUP},
	{SN_get_IsSupported}
};

static SimdIntrinsic ssse3_methods [] = {
	{SN_Abs, OP_VECTOR_IABS},
	{SN_AlignRight},
	{SN_HorizontalAdd},
	{SN_HorizontalAddSaturate, OP_XOP_X_X_X, INTRINS_SSE_PHADDSW},
	{SN_HorizontalSubtract},
	{SN_HorizontalSubtractSaturate, OP_XOP_X_X_X, INTRINS_SSE_PHSUBSW},
	{SN_MultiplyAddAdjacent, OP_XOP_X_X_X, INTRINS_SSE_PMADDUBSW},
	{SN_MultiplyHighRoundScale, OP_XOP_X_X_X, INTRINS_SSE_PMULHRSW},
	{SN_Shuffle, OP_SSSE3_SHUFFLE},
	{SN_Sign},
	{SN_get_IsSupported}
};

static SimdIntrinsic sse41_methods [] = {
	{SN_Blend},
	{SN_BlendVariable},
	{SN_Ceiling, OP_SSE41_ROUNDP, 10 /*round mode*/},
	{SN_CeilingScalar, 0, 10 /*round mode*/},
	{SN_CompareEqual, OP_XCOMPARE, CMP_EQ},
	{SN_ConvertToVector128Int16, OP_SSE_CVTII, MONO_TYPE_I2},
	{SN_ConvertToVector128Int32, OP_SSE_CVTII, MONO_TYPE_I4},
	{SN_ConvertToVector128Int64, OP_SSE_CVTII, MONO_TYPE_I8},
	{SN_DotProduct},
	{SN_Extract},
	{SN_Floor, OP_SSE41_ROUNDP, 9 /*round mode*/},
	{SN_FloorScalar, 0, 9 /*round mode*/},
	{SN_Insert},
	{SN_LoadAlignedVector128NonTemporal, OP_SSE41_LOADANT},
	{SN_Max, OP_XBINOP, OP_IMAX},
	{SN_Min, OP_XBINOP, OP_IMIN},
	{SN_MinHorizontal, OP_XOP_X_X, INTRINS_SSE_PHMINPOSUW},
	{SN_MultipleSumAbsoluteDifferences},
	{SN_Multiply, OP_SSE41_MUL},
	{SN_MultiplyLow, OP_SSE41_MULLO},
	{SN_PackUnsignedSaturate, OP_XOP_X_X_X, INTRINS_SSE_PACKUSDW},
	{SN_RoundCurrentDirection, OP_SSE41_ROUNDP, 4 /*round mode*/},
	{SN_RoundCurrentDirectionScalar, 0, 4 /*round mode*/},
	{SN_RoundToNearestInteger, OP_SSE41_ROUNDP, 8 /*round mode*/},
	{SN_RoundToNearestIntegerScalar, 0, 8 /*round mode*/},
	{SN_RoundToNegativeInfinity, OP_SSE41_ROUNDP, 9 /*round mode*/},
	{SN_RoundToNegativeInfinityScalar, 0, 9 /*round mode*/},
	{SN_RoundToPositiveInfinity, OP_SSE41_ROUNDP, 10 /*round mode*/},
	{SN_RoundToPositiveInfinityScalar, 0, 10 /*round mode*/},
	{SN_RoundToZero, OP_SSE41_ROUNDP, 11 /*round mode*/},
	{SN_RoundToZeroScalar, 0, 11 /*round mode*/},
	{SN_TestC, OP_XOP_I4_X_X, INTRINS_SSE_TESTC},
	{SN_TestNotZAndNotC, OP_XOP_I4_X_X, INTRINS_SSE_TESTNZ},
	{SN_TestZ, OP_XOP_I4_X_X, INTRINS_SSE_TESTZ},
	{SN_get_IsSupported}
};

static SimdIntrinsic sse42_methods [] = {
	{SN_CompareGreaterThan, OP_XCOMPARE, CMP_GT},
	{SN_Crc32},
	{SN_get_IsSupported}
};

static SimdIntrinsic pclmulqdq_methods [] = {
	{SN_CarrylessMultiply},
	{SN_get_IsSupported}
};

static SimdIntrinsic aes_methods [] = {
	{SN_Decrypt, OP_XOP_X_X_X, INTRINS_AESNI_AESDEC},
	{SN_DecryptLast, OP_XOP_X_X_X, INTRINS_AESNI_AESDECLAST},
	{SN_Encrypt, OP_XOP_X_X_X, INTRINS_AESNI_AESENC},
	{SN_EncryptLast, OP_XOP_X_X_X, INTRINS_AESNI_AESENCLAST},
	{SN_InverseMixColumns, OP_XOP_X_X, INTRINS_AESNI_AESIMC},
	{SN_KeygenAssist},
	{SN_get_IsSupported}
};

static SimdIntrinsic popcnt_methods [] = {
	{SN_PopCount},
	{SN_get_IsSupported}
};

static SimdIntrinsic lzcnt_methods [] = {
	{SN_LeadingZeroCount},
	{SN_get_IsSupported}
};

static SimdIntrinsic bmi1_methods [] = {
	{SN_AndNot},
	{SN_BitFieldExtract},
	{SN_ExtractLowestSetBit},
	{SN_GetMaskUpToLowestSetBit},
	{SN_ResetLowestSetBit},
	{SN_TrailingZeroCount},
	{SN_get_IsSupported}
};

static SimdIntrinsic bmi2_methods [] = {
	{SN_MultiplyNoFlags},
	{SN_ParallelBitDeposit},
	{SN_ParallelBitExtract},
	{SN_ZeroHighBits},
	{SN_get_IsSupported}
};

static SimdIntrinsic x86base_methods [] = {
	{SN_BitScanForward},
	{SN_BitScanReverse},
	{SN_DivRem},
	{SN_Pause, OP_XOP, INTRINS_SSE_PAUSE},
	{SN_get_IsSupported}
};

static const IntrinGroup supported_x86_intrinsics [] = {
	{ "Aes", MONO_CPU_X86_AES, aes_methods, sizeof (aes_methods) },
	{ "Avx", MONO_CPU_X86_AVX, unsupported, sizeof (unsupported) },
	{ "Avx2", MONO_CPU_X86_AVX2, unsupported, sizeof (unsupported) },
	{ "Avx512BW", MONO_CPU_X86_AVX2, unsupported, sizeof (unsupported) },
	{ "Avx512CD", MONO_CPU_X86_AVX2, unsupported, sizeof (unsupported) },
	{ "Avx512DQ", MONO_CPU_X86_AVX2, unsupported, sizeof (unsupported) },
	{ "Avx512F", MONO_CPU_X86_AVX2, unsupported, sizeof (unsupported) },
	{ "Avx512Vbmi", MONO_CPU_X86_AVX2, unsupported, sizeof (unsupported) },
	{ "AvxVnni", 0, unsupported, sizeof (unsupported) },
	{ "Bmi1", MONO_CPU_X86_BMI1, bmi1_methods, sizeof (bmi1_methods) },
	{ "Bmi2", MONO_CPU_X86_BMI2, bmi2_methods, sizeof (bmi2_methods) },
	{ "Fma", MONO_CPU_X86_FMA, unsupported, sizeof (unsupported) },
	{ "Lzcnt", MONO_CPU_X86_LZCNT, lzcnt_methods, sizeof (lzcnt_methods), TRUE },
	{ "Pclmulqdq", MONO_CPU_X86_PCLMUL, pclmulqdq_methods, sizeof (pclmulqdq_methods) },
	{ "Popcnt", MONO_CPU_X86_POPCNT, popcnt_methods, sizeof (popcnt_methods), TRUE },
	{ "Sse", MONO_CPU_X86_SSE, sse_methods, sizeof (sse_methods) },
	{ "Sse2", MONO_CPU_X86_SSE2, sse2_methods, sizeof (sse2_methods) },
	{ "Sse3", MONO_CPU_X86_SSE3, sse3_methods, sizeof (sse3_methods) },
	{ "Sse41", MONO_CPU_X86_SSE41, sse41_methods, sizeof (sse41_methods) },
	{ "Sse42", MONO_CPU_X86_SSE42, sse42_methods, sizeof (sse42_methods) },
	{ "Ssse3", MONO_CPU_X86_SSSE3, ssse3_methods, sizeof (ssse3_methods) },
	{ "X86Base", MONO_CPU_INITED, x86base_methods, sizeof (x86base_methods), TRUE },
	{ "X86Serialize", 0, unsupported, sizeof (unsupported) },
};

static MonoInst*
emit_x86_intrinsics (
	MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args,
	MonoClass *klass, const IntrinGroup *intrin_group,
	const SimdIntrinsic *info, int id, MonoTypeEnum arg0_type,
	gboolean is_64bit)
{
	MonoCPUFeatures feature = intrin_group->feature;
	const SimdIntrinsic *intrinsics = intrin_group->intrinsics;

	if (feature == MONO_CPU_X86_SSE) {
		switch (id) {
		case SN_Shuffle:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_SHUFPS, 0, arg0_type, fsig, args);
		case SN_ConvertScalarToVector128Single: {
			int op = 0;
			switch (fsig->params [1]->type) {
			case MONO_TYPE_I4: op = OP_SSE_CVTSI2SS; break;
			case MONO_TYPE_I8: op = OP_SSE_CVTSI2SS64; break;
			default: g_assert_not_reached (); break;
			}
			return emit_simd_ins_for_sig (cfg, klass, op, 0, 0, fsig, args);
		}
		case SN_ReciprocalScalar:
		case SN_ReciprocalSqrtScalar:
		case SN_SqrtScalar: {
			int op = 0;
			switch (id) {
			case SN_ReciprocalScalar: op = OP_SSE_RCPSS; break;
			case SN_ReciprocalSqrtScalar: op = OP_SSE_RSQRTSS; break;
			case SN_SqrtScalar: op = OP_SSE_SQRTSS; break;
			};
			if (fsig->param_count == 1)
				return emit_simd_ins (cfg, klass, op, args [0]->dreg, args[0]->dreg);
			else if (fsig->param_count == 2)
				return emit_simd_ins (cfg, klass, op, args [0]->dreg, args[1]->dreg);
			else
				g_assert_not_reached ();
			break;
		}
		case SN_LoadScalarVector128:
			return NULL;
		default:
			return NULL;
		}
	}

	if (feature == MONO_CPU_X86_SSE2) {
		switch (id) {
		case SN_Subtract:
			return emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, arg0_type == MONO_TYPE_R8 ? OP_FSUB : OP_ISUB, arg0_type, fsig, args);
		case SN_Add:
			return emit_simd_ins_for_sig (cfg, klass, OP_XBINOP, arg0_type == MONO_TYPE_R8 ? OP_FADD : OP_IADD, arg0_type, fsig, args);
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
		case SN_ConvertToInt32:
			if (arg0_type == MONO_TYPE_R8)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_I4_X, INTRINS_SSE_CVTSD2SI, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_I4)
				return emit_simd_ins_for_sig (cfg, klass, OP_EXTRACT_I4, 0, arg0_type, fsig, args);
			else
				return NULL;
		case SN_ConvertToInt64:
			if (arg0_type == MONO_TYPE_R8)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_I8_X, INTRINS_SSE_CVTSD2SI64, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_I8)
				return emit_simd_ins_for_sig (cfg, klass, OP_EXTRACT_I8, 0 /*element index*/, arg0_type, fsig, args);
			else
				g_assert_not_reached ();
			break;
		case SN_ConvertScalarToVector128Double: {
			int op = OP_SSE2_CVTSS2SD;
			switch (fsig->params [1]->type) {
			case MONO_TYPE_I4: op = OP_SSE2_CVTSI2SD; break;
			case MONO_TYPE_I8: op = OP_SSE2_CVTSI2SD64; break;
			}
			return emit_simd_ins_for_sig (cfg, klass, op, 0, 0, fsig, args);
		}
		case SN_ConvertScalarToVector128Int32:
		case SN_ConvertScalarToVector128Int64:
		case SN_ConvertScalarToVector128UInt32:
		case SN_ConvertScalarToVector128UInt64:
			return emit_simd_ins_for_sig (cfg, klass, OP_CREATE_SCALAR, -1, arg0_type, fsig, args);
		case SN_ConvertToUInt32:
			return emit_simd_ins_for_sig (cfg, klass, OP_EXTRACT_I4, 0 /*element index*/, arg0_type, fsig, args);
		case SN_ConvertToUInt64:
			return emit_simd_ins_for_sig (cfg, klass, OP_EXTRACT_I8, 0 /*element index*/, arg0_type, fsig, args);
		case SN_ConvertToVector128Double:
			if (arg0_type == MONO_TYPE_R4)
				return emit_simd_ins_for_sig (cfg, klass, OP_CVTPS2PD, 0, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_I4)
				return emit_simd_ins_for_sig (cfg, klass, OP_CVTDQ2PD, 0, arg0_type, fsig, args);
			else
				return NULL;
		case SN_ConvertToVector128Int32:
			if (arg0_type == MONO_TYPE_R4)
				return emit_simd_ins_for_sig (cfg, klass, OP_CVTPS2DQ, 0, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_R8)
				return emit_simd_ins_for_sig (cfg, klass, OP_CVTPD2DQ, 0, arg0_type, fsig, args);
			else
				return NULL;
		case SN_ConvertToVector128Int32WithTruncation:
			if (arg0_type == MONO_TYPE_R4)
				return emit_simd_ins_for_sig (cfg, klass, OP_CVTTPS2DQ, 0, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_R8)
				return emit_simd_ins_for_sig (cfg, klass, OP_CVTTPD2DQ, 0, arg0_type, fsig, args);
			else
				return NULL;
		case SN_ConvertToVector128Single:
			if (arg0_type == MONO_TYPE_I4)
				return emit_simd_ins_for_sig (cfg, klass, OP_CVTDQ2PS, 0, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_R8)
				return emit_simd_ins_for_sig (cfg, klass, OP_CVTPD2PS, 0, arg0_type, fsig, args);
			else
				return NULL;
		case SN_LoadAlignedVector128:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_LOADU, 16 /*alignment*/, arg0_type, fsig, args);
		case SN_LoadVector128:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE_LOADU, 1 /*alignment*/, arg0_type, fsig, args);
		case SN_MoveScalar:
			return emit_simd_ins_for_sig (cfg, klass, fsig->param_count == 2 ? OP_SSE_MOVS2 : OP_SSE_MOVS, -1, arg0_type, fsig, args);
		case SN_Max:
			switch (arg0_type) {
			case MONO_TYPE_U1:
				return emit_simd_ins_for_sig (cfg, klass, OP_PMAXB_UN, 0, arg0_type, fsig, args);
			case MONO_TYPE_I2:
				return emit_simd_ins_for_sig (cfg, klass, OP_PMAXW, 0, arg0_type, fsig, args);
			case MONO_TYPE_R8: return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_MAXPD, arg0_type, fsig, args);
			default:
				g_assert_not_reached ();
				break;
			}
			break;
		case SN_Min:
			switch (arg0_type) {
			case MONO_TYPE_U1:
				return emit_simd_ins_for_sig (cfg, klass, OP_PMINB_UN, 0, arg0_type, fsig, args);
			case MONO_TYPE_I2:
				return emit_simd_ins_for_sig (cfg, klass, OP_PMINW, 0, arg0_type, fsig, args);
			case MONO_TYPE_R8: return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_MINPD, arg0_type, fsig, args);
			default:
				g_assert_not_reached ();
				break;
			}
			break;
		case SN_Multiply:
			if (arg0_type == MONO_TYPE_U4)
				return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_PMULUDQ, 0, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_R8)
				return emit_simd_ins_for_sig (cfg, klass, OP_MULPD, 0, arg0_type, fsig, args);
			else
				g_assert_not_reached ();
		case SN_MultiplyHigh:
			if (arg0_type == MONO_TYPE_I2)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PMULHW, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_U2)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PMULHUW, arg0_type, fsig, args);
			else
				g_assert_not_reached ();
		case SN_PackSignedSaturate:
			if (arg0_type == MONO_TYPE_I2)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PACKSSWB, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_I4)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PACKSSDW, arg0_type, fsig, args);
			else
				g_assert_not_reached ();
		case SN_PackUnsignedSaturate:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_PACKUS, -1, arg0_type, fsig, args);
		case SN_Extract:
			g_assert (arg0_type == MONO_TYPE_U2);
			return emit_simd_ins_for_sig (cfg, klass, OP_XEXTRACT_I4, 0, arg0_type, fsig, args);
		case SN_Insert:
			g_assert (arg0_type == MONO_TYPE_I2 || arg0_type == MONO_TYPE_U2);
			return emit_simd_ins_for_sig (cfg, klass, OP_XINSERT_I2, 0, arg0_type, fsig, args);
		case SN_ShiftRightLogical: {
			gboolean is_imm = fsig->params [1]->type == MONO_TYPE_U1;
			IntrinsicId op = (IntrinsicId)0;
			switch (arg0_type) {
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
				op = is_imm ? INTRINS_SSE_PSRLI_W : INTRINS_SSE_PSRL_W;
				break;
#if TARGET_SIZEOF_VOID_P == 4
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
				op = is_imm ? INTRINS_SSE_PSRLI_D : INTRINS_SSE_PSRL_D;
				break;
#if TARGET_SIZEOF_VOID_P == 8
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
				op = is_imm ? INTRINS_SSE_PSRLI_Q : INTRINS_SSE_PSRL_Q;
				break;
			default: g_assert_not_reached (); break;
			}
			return emit_simd_ins_for_sig (cfg, klass, is_imm ? OP_XOP_X_X_I4 : OP_XOP_X_X_X, op, arg0_type, fsig, args);
		}
		case SN_ShiftRightArithmetic: {
			gboolean is_imm = fsig->params [1]->type == MONO_TYPE_U1;
			IntrinsicId op = (IntrinsicId)0;
			switch (arg0_type) {
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
				op = is_imm ? INTRINS_SSE_PSRAI_W : INTRINS_SSE_PSRA_W;
				break;
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
				op = is_imm ? INTRINS_SSE_PSRAI_D : INTRINS_SSE_PSRA_D;
				break;
			default: g_assert_not_reached (); break;
			}
			return emit_simd_ins_for_sig (cfg, klass, is_imm ? OP_XOP_X_X_I4 : OP_XOP_X_X_X, op, arg0_type, fsig, args);
		}
		case SN_ShiftLeftLogical: {
			gboolean is_imm = fsig->params [1]->type == MONO_TYPE_U1;
			IntrinsicId op = (IntrinsicId)0;
			switch (arg0_type) {
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
				op = is_imm ? INTRINS_SSE_PSLLI_W : INTRINS_SSE_PSLL_W;
				break;
#if TARGET_SIZEOF_VOID_P == 4
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
				op = is_imm ? INTRINS_SSE_PSLLI_D : INTRINS_SSE_PSLL_D;
				break;
#if TARGET_SIZEOF_VOID_P == 8
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
				op = is_imm ? INTRINS_SSE_PSLLI_Q : INTRINS_SSE_PSLL_Q;
				break;
			default: g_assert_not_reached (); break;
			}
			return emit_simd_ins_for_sig (cfg, klass, is_imm ? OP_XOP_X_X_I4 : OP_XOP_X_X_X, op, arg0_type, fsig, args);
		}
		case SN_ShiftLeftLogical128BitLane:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_PSLLDQ, 0, arg0_type, fsig, args);
		case SN_ShiftRightLogical128BitLane:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_PSRLDQ, 0, arg0_type, fsig, args);
		case SN_Shuffle: {
			if (fsig->param_count == 2) {
				g_assert (arg0_type == MONO_TYPE_I4 || arg0_type == MONO_TYPE_U4);
				return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_PSHUFD, 0, arg0_type, fsig, args);
			} else if (fsig->param_count == 3) {
				g_assert (arg0_type == MONO_TYPE_R8);
				return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_SHUFPD, 0, arg0_type, fsig, args);
			} else {
				g_assert_not_reached ();
				break;
			}
		}
		case SN_ShuffleHigh:
			g_assert (fsig->param_count == 2);
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_PSHUFHW, 0, arg0_type, fsig, args);
		case SN_ShuffleLow:
			g_assert (fsig->param_count == 2);
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE2_PSHUFLW, 0, arg0_type, fsig, args);
		case SN_SqrtScalar: {
			if (fsig->param_count == 1)
				return emit_simd_ins (cfg, klass, OP_SSE2_SQRTSD, args [0]->dreg, args[0]->dreg);
			else if (fsig->param_count == 2)
				return emit_simd_ins (cfg, klass, OP_SSE2_SQRTSD, args [0]->dreg, args[1]->dreg);
			else {
				g_assert_not_reached ();
				break;
			}
		}
		case SN_LoadScalarVector128: {
			int op = 0;
			switch (arg0_type) {
#if TARGET_SIZEOF_VOID_P == 4
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_I4:
			case MONO_TYPE_U4: op = OP_SIMD_LOAD_SCALAR_I4; break;
#if TARGET_SIZEOF_VOID_P == 8
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_I8:
			case MONO_TYPE_U8: op = OP_SIMD_LOAD_SCALAR_I8; break;
			case MONO_TYPE_R8: op = OP_SIMD_LOAD_SCALAR_R8; break;
			default: g_assert_not_reached(); break;
			}
			return emit_simd_ins_for_sig (cfg, klass, op, 0, 0, fsig, args);
		}
		default:
			return NULL;
		}
	}

	if (feature == MONO_CPU_X86_SSE3) {
		switch (id) {
		case SN_AddSubtract:
			if (arg0_type == MONO_TYPE_R4)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_ADDSUBPS, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_R8)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_ADDSUBPD, arg0_type, fsig, args);
			else
				g_assert_not_reached ();
			break;
		case SN_HorizontalAdd:
			if (arg0_type == MONO_TYPE_R4)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_HADDPS, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_R8)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_HADDPD, arg0_type, fsig, args);
			else
				g_assert_not_reached ();
			break;
		case SN_HorizontalSubtract:
			if (arg0_type == MONO_TYPE_R4)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_HSUBPS, arg0_type, fsig, args);
			else if (arg0_type == MONO_TYPE_R8)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_HSUBPD, arg0_type, fsig, args);
			else
				g_assert_not_reached ();
			break;
		default:
			g_assert_not_reached ();
			break;
		}
	}

	if (feature == MONO_CPU_X86_SSSE3) {
		switch (id) {
		case SN_AlignRight:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSSE3_ALIGNR, 0, arg0_type, fsig, args);
		case SN_HorizontalAdd:
			if (arg0_type == MONO_TYPE_I2)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PHADDW, arg0_type, fsig, args);
			return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PHADDD, arg0_type, fsig, args);
		case SN_HorizontalSubtract:
			if (arg0_type == MONO_TYPE_I2)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PHSUBW, arg0_type, fsig, args);
			return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PHSUBD, arg0_type, fsig, args);
		case SN_Sign:
			if (arg0_type == MONO_TYPE_I1)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PSIGNB, arg0_type, fsig, args);
			if (arg0_type == MONO_TYPE_I2)
				return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PSIGNW, arg0_type, fsig, args);
			return emit_simd_ins_for_sig (cfg, klass, OP_XOP_X_X_X, INTRINS_SSE_PSIGND, arg0_type, fsig, args);
		default:
			g_assert_not_reached ();
			break;
		}
	}

	if (feature == MONO_CPU_X86_SSE41) {
		switch (id) {
		case SN_DotProduct: {
			int op = 0;
			switch (arg0_type) {
			case MONO_TYPE_R4: op = OP_SSE41_DPPS; break;
			case MONO_TYPE_R8: op = OP_SSE41_DPPD; break;
			default: g_assert_not_reached (); break;
			}
			return emit_simd_ins_for_sig (cfg, klass, op, 0, arg0_type, fsig, args);
		}
		case SN_MultipleSumAbsoluteDifferences:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE41_MPSADBW, 0, arg0_type, fsig, args);
		case SN_Blend:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE41_BLEND, 0, arg0_type, fsig, args);
		case SN_BlendVariable:
			return emit_simd_ins_for_sig (cfg, klass, OP_SSE41_BLENDV, -1, arg0_type, fsig, args);
		case SN_Extract: {
			int op = 0;
			switch (arg0_type) {
			case MONO_TYPE_U1: op = OP_XEXTRACT_I1; break;
#if TARGET_SIZEOF_VOID_P == 4
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_U4:
			case MONO_TYPE_I4: op = OP_XEXTRACT_I4; break;
#if TARGET_SIZEOF_VOID_P == 8
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#endif
			case MONO_TYPE_U8:
			case MONO_TYPE_I8: op = OP_XEXTRACT_I8; break;
			case MONO_TYPE_R4: op = OP_XEXTRACT_R4; break;
			default: g_assert_not_reached(); break;
			}
			return emit_simd_ins_for_sig (cfg, klass, op, 0, arg0_type, fsig, args);
		}
		case SN_Insert: {
			int op = arg0_type == MONO_TYPE_R4 ? OP_SSE41_INSERTPS : type_to_xinsert_op (arg0_type);
			return emit_simd_ins_for_sig (cfg, klass, op, -1, arg0_type, fsig, args);
		}
		case SN_CeilingScalar:
		case SN_FloorScalar:
		case SN_RoundCurrentDirectionScalar:
		case SN_RoundToNearestIntegerScalar:
		case SN_RoundToNegativeInfinityScalar:
		case SN_RoundToPositiveInfinityScalar:
		case SN_RoundToZeroScalar:
			if (fsig->param_count == 2) {
				return emit_simd_ins_for_sig (cfg, klass, OP_SSE41_ROUNDS, info->default_instc0, arg0_type, fsig, args);
			} else {
				MonoInst* ins = emit_simd_ins (cfg, klass, OP_SSE41_ROUNDS, args [0]->dreg, args [0]->dreg);
				ins->inst_c0 = info->default_instc0;
				ins->inst_c1 = arg0_type;
				return ins;
			}
			break;
		default:
			g_assert_not_reached ();
			break;
		}
	}

	if (feature == MONO_CPU_X86_SSE42) {
		switch (id) {
		case SN_Crc32: {
			MonoTypeEnum arg1_type = get_underlying_type (fsig->params [1]);
			return emit_simd_ins_for_sig (cfg, klass,
				arg1_type == MONO_TYPE_U8 ? OP_SSE42_CRC64 : OP_SSE42_CRC32,
				arg1_type, arg0_type, fsig, args);
		}
		default:
			g_assert_not_reached ();
			break;
		}
	}

	if (feature == MONO_CPU_X86_PCLMUL) {
		switch (id) {
		case SN_CarrylessMultiply: {
			return emit_simd_ins_for_sig (cfg, klass, OP_PCLMULQDQ, 0, arg0_type, fsig, args);
		}
		default:
			g_assert_not_reached ();
			break;
		}
	}

	if (feature == MONO_CPU_X86_AES) {
		switch (id) {
		case SN_KeygenAssist: {
			return emit_simd_ins_for_sig (cfg, klass, OP_AES_KEYGENASSIST, 0, arg0_type, fsig, args);
		}
		default:
			g_assert_not_reached ();
			break;
		}
	}

	MonoInst *ins = NULL;
	if (feature == MONO_CPU_X86_POPCNT) {
		switch (id) {
		case SN_PopCount:
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
	if (feature == MONO_CPU_X86_LZCNT) {
		switch (id) {
		case SN_LeadingZeroCount:
			return emit_simd_ins_for_sig (cfg, klass, is_64bit ? OP_LZCNT64 : OP_LZCNT32, 0, arg0_type, fsig, args);
		default:
			return NULL;
		}
	}
	if (feature == MONO_CPU_X86_BMI1) {
		switch (id) {
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
			int ctlreg = args [1]->dreg;
			if (fsig->param_count == 2) {
			} else if (fsig->param_count == 3) {
				/* This intrinsic is also implemented in managed code.
				 * TODO: remove this if cross-AOT-assembly inlining works
				 */
				int startreg = args [1]->dreg;
				int lenreg = args [2]->dreg;
				int dreg1 = alloc_ireg (cfg);
				EMIT_NEW_BIALU_IMM (cfg, ins, OP_SHL_IMM, dreg1, lenreg, 8);
				int dreg2 = alloc_ireg (cfg);
				EMIT_NEW_BIALU (cfg, ins, OP_IOR, dreg2, startreg, dreg1);
				ctlreg = dreg2;
			} else {
				g_assert_not_reached ();
			}
			return emit_simd_ins (cfg, klass, is_64bit ? OP_BMI1_BEXTR64 : OP_BMI1_BEXTR32, args [0]->dreg, ctlreg);
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
	if (feature == MONO_CPU_X86_BMI2) {
		switch (id) {
		case SN_MultiplyNoFlags: {
			int op = 0;
			if (fsig->param_count == 2) {
				op = is_64bit ? OP_MULX_H64 : OP_MULX_H32;
			} else if (fsig->param_count == 3) {
				op = is_64bit ? OP_MULX_HL64 : OP_MULX_HL32;
			} else {
				g_assert_not_reached ();
			}
			return emit_simd_ins_for_sig (cfg, klass, op, 0, 0, fsig, args);
		}
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

	if (intrinsics == x86base_methods) {
		switch (id) {
		case SN_BitScanForward:
			MONO_INST_NEW (cfg, ins, is_64bit ? OP_X86_BSF64 : OP_X86_BSF32);
			ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->type = is_64bit ? STACK_I8 : STACK_I4;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		case SN_BitScanReverse:
			MONO_INST_NEW (cfg, ins, is_64bit ? OP_X86_BSR64 : OP_X86_BSR32);
			ins->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->type = is_64bit ? STACK_I8 : STACK_I4;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		case SN_DivRem: {
			g_assert (!(TARGET_SIZEOF_VOID_P == 4 && is_64bit)); // x86(no -64) cannot do divisions with 64-bit regs 
			const MonoStackType divtype = is_64bit ? STACK_I8 : STACK_I4;
			const int storetype = is_64bit ? OP_STOREI8_MEMBASE_REG : OP_STOREI4_MEMBASE_REG;
			const int obj_size = MONO_ABI_SIZEOF (MonoObject);

			// We must decide by the second argument, the first is always unsigned here	
			MonoTypeEnum arg1_type = fsig->param_count > 1 ? get_underlying_type (fsig->params [1]) : MONO_TYPE_VOID;
			MonoInst* div;
			MonoInst* div2; 

			if (type_enum_is_unsigned (arg1_type)) {
				MONO_INST_NEW (cfg, div, is_64bit ? OP_X86_LDIVREMU : OP_X86_IDIVREMU);
			} else {
				MONO_INST_NEW (cfg, div, is_64bit ? OP_X86_LDIVREM : OP_X86_IDIVREM);
			}
			div->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			div->sreg1 = args [0]->dreg; // we can use this directly, reg alloc knows that the contents will be destroyed
			div->sreg2 = args [1]->dreg; // same here as ^
			div->sreg3 = args [2]->dreg;
			div->type = divtype;
			MONO_ADD_INS (cfg->cbb, div);

			// Protect the contents of edx/rdx by assigning it a vreg. The instruction must
			// immediately follow DIV/IDIV so that edx content is not modified.
			// In LLVM the remainder is already calculated, just need to capture it in a vreg.
			MONO_INST_NEW (cfg, div2, is_64bit ? OP_X86_LDIVREM2 : OP_X86_IDIVREM2);
			div2->dreg = is_64bit ? alloc_lreg (cfg) : alloc_ireg (cfg);
			div2->type = divtype;
			MONO_ADD_INS (cfg->cbb, div2);
			
			// TODO: Can the creation of tuple be elided? (e.g. if deconstruction is used)
			MonoInst* tuple = mono_compile_create_var (cfg, fsig->ret, OP_LOCAL);
			MonoInst* tuple_addr;
			EMIT_NEW_TEMPLOADA (cfg, tuple_addr, tuple->inst_c0);

			MonoClassField* field1 = mono_class_get_field_from_name_full (tuple->klass, "Item1", NULL);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, storetype, tuple_addr->dreg, field1->offset - obj_size, div->dreg);
			MonoClassField* field2 = mono_class_get_field_from_name_full (tuple->klass, "Item2", NULL);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, storetype, tuple_addr->dreg, field2->offset - obj_size, div2->dreg);
			EMIT_NEW_TEMPLOAD (cfg, ins, tuple->inst_c0);
			return ins;
			}
		default:
			g_assert_not_reached ();
		}
	}

	return NULL;
}
#endif // !TARGET_ARM64

#ifdef TARGET_WASM

static MonoInst*
emit_sum_vector (MonoCompile *cfg, MonoType *vector_type, MonoTypeEnum element_type, MonoInst *arg)
{
	MonoClass *vector_class = mono_class_from_mono_type_internal (vector_type);
	MonoInst* vsum = emit_simd_ins (cfg, vector_class, OP_WASM_SIMD_SUM, arg->dreg, -1);

	return extract_first_element (cfg, vector_class, element_type, vsum->dreg);
}

static guint16 bitoperations_methods [] = {
	SN_LeadingZeroCount,
	SN_TrailingZeroCount,
};

static SimdIntrinsic wasmbase_methods [] = {
	{SN_LeadingZeroCount},
	{SN_TrailingZeroCount},
	{SN_get_IsSupported},
};

static SimdIntrinsic packedsimd_methods [] = {
	{SN_Abs},
	{SN_Add},
	{SN_AddPairwiseWidening},
	{SN_AddSaturate},
	{SN_AllTrue},
	{SN_And, OP_XBINOP_FORCEINT, XBINOP_FORCEINT_AND},
	{SN_AndNot},
	{SN_AnyTrue},
	{SN_AverageRounded},
	{SN_Bitmask, OP_WASM_SIMD_BITMASK},
	{SN_BitwiseSelect, OP_BSL},
	{SN_Ceiling, OP_XOP_OVR_X_X, INTRINS_SIMD_CEIL},
	{SN_CompareEqual, OP_XCOMPARE, CMP_EQ, OP_XCOMPARE, CMP_EQ, OP_XCOMPARE_FP, CMP_EQ},
	{SN_CompareGreaterThan, OP_XCOMPARE, CMP_GT, OP_XCOMPARE, CMP_GT_UN, OP_XCOMPARE_FP, CMP_GT},
	{SN_CompareGreaterThanOrEqual, OP_XCOMPARE, CMP_GE, OP_XCOMPARE, CMP_GE_UN, OP_XCOMPARE_FP, CMP_GE},
	{SN_CompareLessThan, OP_XCOMPARE, CMP_LT, OP_XCOMPARE, CMP_LT_UN, OP_XCOMPARE_FP, CMP_LT},
	{SN_CompareLessThanOrEqual, OP_XCOMPARE, CMP_LE, OP_XCOMPARE, CMP_LE_UN, OP_XCOMPARE_FP, CMP_LE},
	{SN_CompareNotEqual, OP_XCOMPARE, CMP_NE, OP_XCOMPARE, CMP_NE, OP_XCOMPARE_FP, CMP_NE},
	{SN_ConvertNarrowingSaturateSigned},
	{SN_ConvertNarrowingSaturateUnsigned},
	{SN_ConvertToDoubleLower, OP_CVTDQ2PD, 0, OP_WASM_SIMD_CONV_U4_TO_R8_LOW, 0, OP_CVTPS2PD},
	{SN_ConvertToInt32Saturate},
	{SN_ConvertToSingle, OP_CVT_SI_FP, 0, OP_CVT_UI_FP, 0, OP_WASM_SIMD_CONV_R8_TO_R4},
	{SN_ConvertToUInt32Saturate},
	{SN_Divide},
	{SN_Dot, OP_XOP_X_X_X, INTRINS_WASM_DOT},
	{SN_ExtractScalar},
	{SN_Floor, OP_XOP_OVR_X_X, INTRINS_SIMD_FLOOR},
	{SN_LoadScalarAndInsert, OP_WASM_SIMD_LOAD_SCALAR_INSERT},
	{SN_LoadScalarAndSplatVector128, OP_WASM_SIMD_LOAD_SCALAR_SPLAT},
	{SN_LoadScalarVector128},
	{SN_LoadVector128, OP_LOADX_MEMBASE},
	{SN_LoadWideningVector128, OP_WASM_SIMD_LOAD_WIDENING},
	{SN_Max, OP_XBINOP, OP_IMIN, OP_XBINOP, OP_IMIN_UN, OP_XBINOP, OP_FMIN},
	{SN_Min, OP_XBINOP, OP_IMAX, OP_XBINOP, OP_IMAX_UN, OP_XBINOP, OP_FMAX},
	{SN_Multiply},
	{SN_MultiplyRoundedSaturateQ15, OP_XOP_X_X_X, INTRINS_WASM_Q15MULR_SAT_SIGNED},
	{SN_MultiplyWideningLower, OP_WASM_EXTMUL_LOWER, 0, OP_WASM_EXTMUL_LOWER_U},
	{SN_MultiplyWideningUpper, OP_WASM_EXTMUL_UPPER, 0, OP_WASM_EXTMUL_UPPER_U},
	{SN_Negate},
	{SN_Not, OP_WASM_ONESCOMPLEMENT},
	{SN_Or, OP_XBINOP_FORCEINT, XBINOP_FORCEINT_OR},
	{SN_PopCount, OP_XOP_OVR_X_X, INTRINS_SIMD_POPCNT},
	{SN_PseudoMax, OP_XOP_OVR_X_X_X, INTRINS_WASM_PMAX},
	{SN_PseudoMin, OP_XOP_OVR_X_X_X, INTRINS_WASM_PMIN},
	{SN_ReplaceScalar},
	{SN_RoundToNearest, OP_XOP_OVR_X_X, INTRINS_SIMD_NEAREST},
	{SN_ShiftLeft, OP_SIMD_SHL},
	{SN_ShiftRightArithmetic, OP_SIMD_SSHR},
	{SN_ShiftRightLogical, OP_SIMD_USHR},
	{SN_Shuffle},
	{SN_SignExtendWideningLower, OP_WASM_SIMD_SEXT_LOWER},
	{SN_SignExtendWideningUpper, OP_WASM_SIMD_SEXT_UPPER},
	{SN_Splat},
	{SN_Sqrt},
	{SN_Store, OP_SIMD_STORE, 1},
	{SN_StoreSelectedScalar, OP_WASM_SIMD_STORE_LANE},
	{SN_Subtract},
	{SN_SubtractSaturate},
	{SN_Swizzle, OP_WASM_SIMD_SWIZZLE},
	{SN_Truncate, OP_XOP_OVR_X_X, INTRINS_SIMD_TRUNC},
	{SN_Xor, OP_XBINOP_FORCEINT, XBINOP_FORCEINT_XOR},
	{SN_ZeroExtendWideningLower, OP_WASM_SIMD_ZEXT_LOWER},
	{SN_ZeroExtendWideningUpper, OP_WASM_SIMD_ZEXT_UPPER},
	{SN_get_IsSupported},
};

static const IntrinGroup supported_wasm_intrinsics [] = {
	{ "PackedSimd", MONO_CPU_WASM_SIMD, packedsimd_methods, sizeof (packedsimd_methods) },
	{ "WasmBase", MONO_CPU_WASM_BASE, wasmbase_methods, sizeof (wasmbase_methods) },
};

static const IntrinGroup supported_wasm_common_intrinsics [] = {
	{ "WasmBase", MONO_CPU_WASM_BASE, wasmbase_methods, sizeof (wasmbase_methods) },
};

static MonoInst*
emit_wasm_zero_count (MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args,
	MonoClass *klass, int id, MonoTypeEnum arg0_type)
{
	gboolean arg0_i32 = (arg0_type == MONO_TYPE_I4) || (arg0_type == MONO_TYPE_U4);
#if TARGET_SIZEOF_VOID_P == 4
	arg0_i32 = arg0_i32 || (arg0_type == MONO_TYPE_I) || (arg0_type == MONO_TYPE_U);
#endif
	int opcode = id == SN_LeadingZeroCount ? (arg0_i32 ? OP_LZCNT32 : OP_LZCNT64) : (arg0_i32 ? OP_CTTZ32 : OP_CTTZ64);

	return emit_simd_ins_for_sig (cfg, klass, opcode, 0, arg0_type, fsig, args);
}

static MonoInst*
emit_wasm_bitoperations_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	int id = lookup_intrins (bitoperations_methods, sizeof (bitoperations_methods), cmethod);
	if (id == -1) {
		return NULL;
	}

	MonoTypeEnum arg0_type = fsig->param_count > 0 ? get_underlying_type (fsig->params [0]) : MONO_TYPE_VOID;

	switch (id) {
		case SN_LeadingZeroCount:
		case SN_TrailingZeroCount: {
			if (!MONO_TYPE_IS_INT_32_64 (fsig->params [0]))
				return NULL;
			return emit_wasm_zero_count(cfg, fsig, args, cmethod->klass, id, arg0_type);
		}
	}

	return NULL;
}

static MonoInst*
emit_wasm_supported_intrinsics (
	MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args,
	MonoClass *klass, const IntrinGroup *intrin_group,
	const SimdIntrinsic *info, int id, MonoTypeEnum arg0_type,
	gboolean is_64bit)
{
	MonoCPUFeatures feature = intrin_group->feature;

	if (feature == MONO_CPU_WASM_BASE) {
		switch (id) {
			case SN_LeadingZeroCount:
			case SN_TrailingZeroCount: {
				if (!MONO_TYPE_IS_INT_32_64 (fsig->params [0]))
					return NULL;
				return emit_wasm_zero_count(cfg, fsig, args, klass, id, arg0_type);
			}
		}
	}

	if (feature == MONO_CPU_WASM_SIMD) {
		if ((id != SN_Splat && id != SN_Store && id != SN_LoadScalarVector128 && !is_element_type_primitive (fsig->params [0])) ||
		    (id == SN_Splat && !MONO_TYPE_IS_VECTOR_PRIMITIVE(fsig->params [0])) ||
		    (id == SN_Store && !is_element_type_primitive (fsig->params [1])))
			return NULL;

		uint16_t op = info->default_op;
		uint16_t c0 = info->default_instc0;

		switch (id) {
			case SN_Abs: {
				if (type_enum_is_float(arg0_type)) {
					op = OP_XOP_X_X;
					c0 = arg0_type == MONO_TYPE_R8 ? INTRINS_WASM_FABS_V2 : INTRINS_WASM_FABS_V4;
				} else {
					op = OP_VECTOR_IABS;
				}
				// continue with default emit
				break;
			}
			case SN_Add:
			case SN_Divide:
			case SN_Subtract:
			case SN_Multiply:
				return emit_simd_ins_for_binary_op (cfg, klass, fsig, args, arg0_type, id);
			case SN_Negate:
				return emit_simd_ins_for_unary_op (cfg, klass, fsig, args, arg0_type, id);
			case SN_AndNot: {
				/* Swap lhs and rhs because Vector128 needs lhs & !rhs
				whereas SSE2 does !lhs & rhs */
				MonoInst *tmp = args[0];
				args[0] = args[1];
				args[1] = tmp;
				op = OP_VECTOR_ANDN;
				// continue with default emit
				break;
			}
			case SN_AnyTrue: {
				op = OP_XOP_X_X;

				switch (arg0_type) {
				case MONO_TYPE_U1:
				case MONO_TYPE_I1:
						c0 = INTRINS_WASM_ANYTRUE_V16;
						break;
				case MONO_TYPE_U2:
				case MONO_TYPE_I2:
						c0 = INTRINS_WASM_ANYTRUE_V8;
						break;
				case MONO_TYPE_U4:
				case MONO_TYPE_I4:
						c0 = INTRINS_WASM_ANYTRUE_V4;
						break;
				case MONO_TYPE_U8:
				case MONO_TYPE_I8:
						c0 = INTRINS_WASM_ANYTRUE_V2;
						break;
				}

				// continue with default emit
				if (c0 != 0)
						break;

				return NULL;
			}
			case SN_AllTrue: {
				op = OP_XOP_X_X;

				switch (arg0_type) {
				case MONO_TYPE_U1:
				case MONO_TYPE_I1:
						c0 = INTRINS_WASM_ALLTRUE_V16;
						break;
				case MONO_TYPE_U2:
				case MONO_TYPE_I2:
						c0 = INTRINS_WASM_ALLTRUE_V8;
						break;
				case MONO_TYPE_U4:
				case MONO_TYPE_I4:
						c0 = INTRINS_WASM_ALLTRUE_V4;
						break;
				case MONO_TYPE_U8:
				case MONO_TYPE_I8:
						c0 = INTRINS_WASM_ALLTRUE_V2;
						break;
				}

				// continue with default emit
				if (c0 != 0)
						break;

				return NULL;
			}
			case SN_AddPairwiseWidening: {
				op = OP_XOP_X_X;

				switch (arg0_type) {
				case MONO_TYPE_I1:
						c0 = INTRINS_WASM_EXTADD_PAIRWISE_SIGNED_V16;
						break;
				case MONO_TYPE_I2:
						c0 = INTRINS_WASM_EXTADD_PAIRWISE_SIGNED_V8;
						break;
				case MONO_TYPE_U1:
						c0 = INTRINS_WASM_EXTADD_PAIRWISE_UNSIGNED_V16;
						break;
				case MONO_TYPE_U2:
						c0 = INTRINS_WASM_EXTADD_PAIRWISE_UNSIGNED_V8;
						break;
				}

				// continue with default emit
				if (c0 != 0)
						break;

				return NULL;
			}
			case SN_AddSaturate: {
				op = OP_XOP_X_X_X;

				switch (arg0_type) {
				case MONO_TYPE_I1:
						c0 = INTRINS_SSE_SADD_SATI8;
						break;
				case MONO_TYPE_I2:
						c0 = INTRINS_SSE_SADD_SATI16;
						break;
				case MONO_TYPE_U1:
						c0 = INTRINS_SSE_UADD_SATI8;
						break;
				case MONO_TYPE_U2:
						c0 = INTRINS_SSE_UADD_SATI16;
						break;
				}

				// continue with default emit
				if (c0 != 0)
						break;

				return NULL;
			}
			case SN_AverageRounded: {
				op = OP_XOP_X_X_X;

				switch (arg0_type) {
				case MONO_TYPE_U1:
						c0 = INTRINS_WASM_AVERAGE_ROUNDED_V16;
						break;
				case MONO_TYPE_U2:
						c0 = INTRINS_WASM_AVERAGE_ROUNDED_V8;
						break;
				}

				// continue with default emit
				if (c0 != 0)
						break;

				return NULL;
			}
			case SN_ConvertNarrowingSaturateSigned: {
				op = OP_XOP_X_X_X;

				switch (arg0_type) {
				case MONO_TYPE_I2:
						c0 = INTRINS_WASM_NARROW_SIGNED_V16;
						break;
				case MONO_TYPE_I4:
						c0 = INTRINS_WASM_NARROW_SIGNED_V8;
						break;
				}

				// continue with default emit
				if (c0 != 0)
						break;

				return NULL;
			}
			case SN_ConvertNarrowingSaturateUnsigned: {
				op = OP_XOP_X_X_X;

				switch (arg0_type) {
				case MONO_TYPE_I2:
						c0 = INTRINS_WASM_NARROW_UNSIGNED_V16;
						break;
				case MONO_TYPE_I4:
						c0 = INTRINS_WASM_NARROW_UNSIGNED_V8;
						break;
				}

				// continue with default emit
				if (c0 != 0)
						break;

				return NULL;
			}
			case SN_ConvertToInt32Saturate: {
				switch (arg0_type) {
					case MONO_TYPE_R4:
						op = OP_CVT_FP_SI;
						break;
					case MONO_TYPE_R8:
						op = OP_WASM_SIMD_CONV_R8_TO_I4_ZERO;
						c0 = INTRINS_WASM_CONV_R8_TO_I4;
						break;
					default:
						return NULL;
				}

				// continue with default emit
				break;
			}
			case SN_ConvertToUInt32Saturate: {
				switch (arg0_type) {
					case MONO_TYPE_R4:
						op = OP_CVT_FP_UI;
						break;
					case MONO_TYPE_R8:
						op = OP_WASM_SIMD_CONV_R8_TO_I4_ZERO;
						c0 = INTRINS_WASM_CONV_R8_TO_U4;
						break;
					default:
						return NULL;
				}

				// continue with default emit
				break;
			}
			case SN_ExtractScalar: {
				op = GINT_TO_UINT16 (type_to_xextract_op (arg0_type));
				break;
			}
			case SN_LoadScalarVector128: {
				switch (arg0_type) {
				case MONO_TYPE_I:
				case MONO_TYPE_U:
				case MONO_TYPE_I4:
				case MONO_TYPE_U4:
				case MONO_TYPE_R4: // use OP_SIMD_LOAD_SCALAR_I4 to make llvm emit the v128.load32_zero
					op = OP_SIMD_LOAD_SCALAR_I4;
					break;
				case MONO_TYPE_I8:
				case MONO_TYPE_U8:
				case MONO_TYPE_R8: // use OP_SIMD_LOAD_SCALAR_I8 to make llvm emit the v128.load64_zero
					op = OP_SIMD_LOAD_SCALAR_I8;
					break;
				default:
					g_assert_not_reached();
					return NULL;
				}

				// continue with default emit
				break;
			}
			case SN_ReplaceScalar: {
				int insert_op = type_to_xinsert_op (arg0_type);
				MonoInst *ins = emit_simd_ins (cfg, klass, insert_op, args [0]->dreg, args [2]->dreg);
				ins->sreg3 = args [1]->dreg;
				ins->inst_c1 = arg0_type;
				return ins;
			}
			case SN_Splat: {
				MonoType *etype = get_vector_t_elem_type (fsig->ret);
				g_assert (fsig->param_count == 1 && mono_metadata_type_equal (fsig->params [0], etype));
				return emit_simd_ins (cfg, klass, type_to_expand_op (etype->type), args [0]->dreg, -1);
			}
			case SN_Sqrt: {
				op = OP_XOP_X_X;
				c0 = arg0_type == MONO_TYPE_R4 ? INTRINS_SIMD_SQRT_R4 : INTRINS_SIMD_SQRT_R8;
				// continue with default emit
				break;
			}
			case SN_SubtractSaturate: {
				op = OP_XOP_X_X_X;

				switch (arg0_type) {
				case MONO_TYPE_I1:
						c0 = INTRINS_WASM_SUB_SAT_SIGNED_V16;
						break;
				case MONO_TYPE_I2:
						c0 = INTRINS_WASM_SUB_SAT_SIGNED_V8;
						break;
				case MONO_TYPE_U1:
						c0 = INTRINS_WASM_SUB_SAT_UNSIGNED_V16;
						break;
				case MONO_TYPE_U2:
						c0 = INTRINS_WASM_SUB_SAT_UNSIGNED_V8;
						break;
				}

				// continue with default emit
				if (c0 != 0)
						break;

				return NULL;
			}
		case SN_Shuffle: {
			/*
			 * FIXME: llvm.wasm_shuffle causes llvm to crash if the shuffle argument is not a constant,
			 * and we can't determine that here since the JIT has no vector constants.
			 */
			return NULL;
		}
		}

		// default emit path for cases with op set
		if (op != 0)
			return emit_simd_ins_for_sig (cfg, klass, op, c0, arg0_type, fsig, args);
	}

	g_assert_not_reached ();

	return NULL;
}

#endif // TARGET_WASM

#ifdef TARGET_ARM64
static
MonoInst*
arch_emit_simd_intrinsics (const char *class_ns, const char *class_name, MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (!strcmp (class_ns, "System.Runtime.Intrinsics.Arm")) {
		return emit_hardware_intrinsics(cfg, cmethod, fsig, args,
			supported_arm_intrinsics, sizeof (supported_arm_intrinsics),
			emit_arm64_intrinsics);
	}

	if (!strcmp (class_ns, "System.Numerics")) {
		if (!strcmp (class_name, "Vector"))
			return emit_sri_vector (cfg, cmethod, fsig, args);
		if (!strcmp (class_name, "Vector`1"))
			return emit_sri_vector_t (cfg, cmethod, fsig, args);
	}

	return NULL;
}
#elif defined(TARGET_AMD64)
// TODO: test and enable for x86 too
static
MonoInst*
arch_emit_simd_intrinsics (const char *class_ns, const char *class_name, MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (!strcmp (class_ns, "System.Runtime.Intrinsics.X86")) {
		return emit_hardware_intrinsics (cfg, cmethod, fsig, args,
			supported_x86_intrinsics, sizeof (supported_x86_intrinsics),
			emit_x86_intrinsics);
	}

	if (!strcmp (class_ns, "System.Numerics")) {
		// FIXME: Shouldn't this call emit_sri_vector () ?
		if (!strcmp (class_name, "Vector"))
			return emit_sys_numerics_vector (cfg, cmethod, fsig, args);
		// FIXME: Shouldn't this call emit_sri_vector_t () ?
		if (!strcmp (class_name, "Vector`1"))
			return emit_sys_numerics_vector_t (cfg, cmethod, fsig, args);
	}

	return NULL;
}
#elif defined(TARGET_WASM)
static
MonoInst*
arch_emit_simd_intrinsics (const char *class_ns, const char *class_name, MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (!strcmp (class_ns, "System.Runtime.Intrinsics.Wasm")) {
		return emit_hardware_intrinsics (cfg, cmethod, fsig, args,
			supported_wasm_intrinsics, sizeof (supported_wasm_intrinsics),
			emit_wasm_supported_intrinsics);
	}

	if (!strcmp (class_ns, "System.Numerics")) {
		if (!strcmp (class_name, "Vector"))
			return emit_sri_vector (cfg, cmethod, fsig, args);
		if (!strcmp (class_name, "Vector`1"))
			return emit_sri_vector_t (cfg, cmethod, fsig, args);
	}

	return NULL;
}
#else
static
MonoInst*
arch_emit_simd_intrinsics (const char *class_ns, const char *class_name, MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}
#endif

#if defined(TARGET_WASM)
static
MonoInst*
arch_emit_common_intrinsics (const char *class_ns, const char *class_name, MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (!strcmp (class_ns, "System.Runtime.Intrinsics.Wasm")) {
		return emit_hardware_intrinsics (cfg, cmethod, fsig, args,
			supported_wasm_common_intrinsics, sizeof (supported_wasm_common_intrinsics),
			emit_wasm_supported_intrinsics);
	}

	if (!strcmp (class_ns, "System.Numerics") && !strcmp (class_name, "BitOperations")) {
		return emit_wasm_bitoperations_intrinsics (cfg, cmethod, fsig, args);
	}

	return NULL;
}
#else
static
MonoInst*
arch_emit_common_intrinsics (const char *class_ns, const char *class_name, MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}
#endif

static MonoInst*
emit_simd_intrinsics (const char *class_ns, const char *class_name, MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins;

	ins = arch_emit_simd_intrinsics (class_ns, class_name, cfg, cmethod, fsig, args);
	if (ins)
		return ins;

	if (!strcmp (class_ns, "System.Runtime.Intrinsics")) {
		if (!strcmp (class_name, "Vector64") || !strcmp (class_name, "Vector128") || !strcmp (class_name, "Vector256") || !strcmp (class_name, "Vector512"))
			return emit_sri_vector (cfg, cmethod, fsig, args);
		if (!strcmp (class_name, "Vector64`1") || !strcmp (class_name, "Vector128`1") || !strcmp (class_name, "Vector256`1") || !strcmp (class_name, "Vector512`1"))
			return emit_sri_vector_t (cfg, cmethod, fsig, args);
	}

	if (!strcmp (class_ns, "System.Numerics")) {
		if (!strcmp (class_name, "Vector2") || !strcmp (class_name, "Vector3") || !strcmp (class_name, "Vector4") ||
			!strcmp (class_name, "Quaternion") || !strcmp (class_name, "Plane"))
			return emit_vector_2_3_4 (cfg, cmethod, fsig, args);
	}

	return NULL;
}

typedef MonoInst* (*EmitCallback)(const char *class_ns, const char *class_name, MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args);

static MonoInst*
emit_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args, EmitCallback ecb)
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

	MonoInst *simd_inst = ecb (class_ns, class_name, cfg, cmethod, fsig, args);
	if (simd_inst)
		cfg->uses_simd_intrinsics = TRUE;
	return simd_inst;
}

MonoInst*
mono_emit_simd_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return emit_intrinsics (cfg, cmethod, fsig, args, emit_simd_intrinsics);
}

MonoInst*
mono_emit_common_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return emit_intrinsics (cfg, cmethod, fsig, args, arch_emit_common_intrinsics);
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
	if (cfg->uses_simd_intrinsics)
		return TRUE;

	switch (ins->opcode) {
	case OP_XMOVE:
	case OP_XZERO:
	case OP_XPHI:
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
	if (src_var && src_var->opcode == OP_ARG && src_var->klass && mini_class_is_simd (cfg, src_var->klass)) {
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

static void
decompose_vtype_opt_store_arg (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, gint32 *dreg_int32)
{
	guint32 *dreg = (guint32*)dreg_int32;
	MonoInst *dest_var = get_vreg_to_inst (cfg, *dreg);
	if (dest_var && dest_var->opcode == OP_ARG && dest_var->klass && mini_class_is_simd (cfg, dest_var->klass)) {
		MonoInst *varload_ins, *store_ins;
		*dreg = alloc_xreg (cfg);
		NEW_VARLOADA (cfg, varload_ins, dest_var, dest_var->inst_vtype);
		mono_bblock_insert_after_ins (bb, ins, varload_ins);
		MONO_INST_NEW (cfg, store_ins, OP_STOREX_MEMBASE);
		store_ins->klass = dest_var->klass;
		store_ins->type = STACK_VTYPE;
		store_ins->sreg1 = *dreg;
		store_ins->dreg = varload_ins->dreg;
		mono_bblock_insert_after_ins (bb, varload_ins, store_ins);
	}
}

void
mono_simd_decompose_intrinsic (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins)
{
	if ((cfg->opt & MONO_OPT_SIMD) && decompose_vtype_opt_uses_simd_intrinsics(cfg, ins)) {
		const char *spec = INS_INFO (ins->opcode);
		if (spec [MONO_INST_SRC1] == 'x')
			decompose_vtype_opt_load_arg (cfg, bb, ins, &(ins->sreg1));
		if (spec [MONO_INST_SRC2] == 'x')
			decompose_vtype_opt_load_arg (cfg, bb, ins, &(ins->sreg2));
		if (spec [MONO_INST_SRC3] == 'x')
			decompose_vtype_opt_load_arg (cfg, bb, ins, &(ins->sreg3));
		if (spec [MONO_INST_DEST] == 'x')
			decompose_vtype_opt_store_arg (cfg, bb, ins, &(ins->dreg));
	}
}

gboolean
mono_simd_unsupported_aggressive_inline_intrinsic_type (MonoMethod *cmethod)
{
	/*
	* If a method has been marked with aggressive inlining, check if we support
	* aggressive inlining of the intrinsics type, if not, ignore aggressive inlining
	* since it could end up inlining a large amount of code that most likely will end
	* up as dead code.
	*/
	if (!strcmp (m_class_get_name_space (cmethod->klass), "System.Runtime.Intrinsics")) {
		if (!strncmp(m_class_get_name (cmethod->klass), "Vector", 6)) {
			const char *vector_type = m_class_get_name (cmethod->klass) + 6;
			if (!strcmp(vector_type, "256`1") || !strcmp(vector_type, "512`1"))
				return TRUE;
		}
	}
	return FALSE;
}
#else
void
mono_simd_decompose_intrinsic (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins)
{
}

gboolean
mono_simd_unsupported_aggressive_inline_intrinsic_type (MonoMethod* cmethod)
{
	return FALSE;
}

#endif /*defined(TARGET_WIN32) && defined(TARGET_AMD64)*/

#endif /* DISABLE_JIT */

#else /* MONO_ARCH_SIMD_INTRINSICS */

void
mono_simd_intrinsics_init (void)
{
}

MonoInst*
mono_emit_simd_field_load (MonoCompile *cfg, MonoClassField *field, MonoInst *addr)
{
	return NULL;
}

MonoInst*
mono_emit_simd_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}

MonoInst*
mono_emit_common_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}

void
mono_simd_decompose_intrinsic (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins)
{
}

gboolean
mono_simd_unsupported_aggressive_inline_intrinsic_type (MonoMethod* cmethod)
{
	return FALSE;
}

#endif /* MONO_ARCH_SIMD_INTRINSICS */

#if defined(TARGET_AMD64)
void
ves_icall_System_Runtime_Intrinsics_X86_X86Base___cpuidex (int abcd[4], int function_id, int subfunction_id)
{
#ifndef MONO_CROSS_COMPILE
	mono_hwcap_x86_call_cpuidex (function_id, subfunction_id,
		&abcd [0], &abcd [1], &abcd [2], &abcd [3]);
#endif
}
#endif

MONO_EMPTY_SOURCE_FILE (simd_intrinsics_netcore);
