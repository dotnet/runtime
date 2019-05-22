/**
 * Intrinsics support
 */

#include <config.h>
#include <mono/utils/mono-compiler.h>
#include <math.h>

#ifndef DISABLE_JIT

#include "mini.h"
#include "mini-runtime.h"
#include "ir-emit.h"
#include "jit-icalls.h"
#include "debugger-agent.h"

#include <mono/metadata/abi-details.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/monitor.h>
#include <mono/utils/mono-memory-model.h>

static GENERATE_GET_CLASS_WITH_CACHE (runtime_helpers, "System.Runtime.CompilerServices", "RuntimeHelpers")
static GENERATE_TRY_GET_CLASS_WITH_CACHE (math, "System", "Math")

/* optimize the simple GetGenericValueImpl/SetGenericValueImpl generic icalls */
static MonoInst*
emit_array_generic_access (MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args, int is_set)
{
	MonoInst *addr, *store, *load;
	MonoClass *eklass = mono_class_from_mono_type_internal (fsig->params [2]);

	/* the bounds check is already done by the callers */
	addr = mini_emit_ldelema_1_ins (cfg, eklass, args [0], args [1], FALSE);
	MonoType *etype = m_class_get_byval_arg (eklass);
	if (is_set) {
		EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, load, etype, args [2]->dreg, 0);
		EMIT_NEW_STORE_MEMBASE_TYPE (cfg, store, etype, addr->dreg, 0, load->dreg);
		if (mini_type_is_reference (etype))
			mini_emit_write_barrier (cfg, addr, load);
	} else {
		EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, load, etype, addr->dreg, 0);
		EMIT_NEW_STORE_MEMBASE_TYPE (cfg, store, etype, args [2]->dreg, 0, load->dreg);
	}
	return store;
}

static gboolean
mono_type_is_native_blittable (MonoType *t)
{
	if (MONO_TYPE_IS_REFERENCE (t))
		return FALSE;

	if (MONO_TYPE_IS_PRIMITIVE_SCALAR (t))
		return TRUE;

	MonoClass *klass = mono_class_from_mono_type_internal (t);

	//MonoClass::blitable depends on mono_class_setup_fields being done.
	mono_class_setup_fields (klass);
	if (!m_class_is_blittable (klass))
		return FALSE;

	// If the native marshal size is different we can't convert PtrToStructure to a type load
	if (mono_class_native_size (klass, NULL) != mono_class_value_size (klass, NULL))
		return FALSE;

	return TRUE;
}

MonoInst*
mini_emit_inst_for_ctor (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	const char* cmethod_klass_name_space = m_class_get_name_space (cmethod->klass);
	const char* cmethod_klass_name = m_class_get_name (cmethod->klass);
	MonoImage *cmethod_klass_image = m_class_get_image (cmethod->klass);
	gboolean in_corlib = cmethod_klass_image == mono_defaults.corlib;
	MonoInst *ins = NULL;

	/* Required intrinsics are always used even with -O=-intrins */
	if (in_corlib &&
		!strcmp (cmethod_klass_name_space, "System") &&
		!strcmp (cmethod_klass_name, "ByReference`1")) {
		/* public ByReference(ref T value) */
		g_assert (fsig->hasthis && fsig->param_count == 1);
		EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STORE_MEMBASE_REG, args [0]->dreg, 0, args [1]->dreg);
		return ins;
	}

	ins = mono_emit_native_types_intrinsics (cfg, cmethod, fsig, args);
	if (ins)
		return ins;

	if (!(cfg->opt & MONO_OPT_INTRINS))
		return NULL;

#ifdef MONO_ARCH_SIMD_INTRINSICS
	if (cfg->opt & MONO_OPT_SIMD) {
		ins = mono_emit_simd_intrinsics (cfg, cmethod, fsig, args);
		if (ins)
			return ins;
	}
#endif

	return NULL;
}

static MonoInst*
llvm_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args, gboolean in_corlib)
{
	MonoInst *ins = NULL;
	int opcode = 0;

	if (in_corlib && !strcmp (m_class_get_name (cmethod->klass), "MathF") && fsig->param_count && fsig->params [0]->type == MONO_TYPE_R4 && cfg->r4fp) {
		if (!strcmp (cmethod->name, "Sin"))
			opcode = OP_SINF;
		else if (!strcmp (cmethod->name, "Cos"))
			opcode = OP_COSF;
		else if (!strcmp (cmethod->name, "Abs"))
			opcode = OP_ABSF;
		else if (!strcmp (cmethod->name, "Sqrt"))
			opcode = OP_SQRTF;
		else if (!strcmp (cmethod->name, "Max"))
			opcode = OP_RMAX;
		else if (!strcmp (cmethod->name, "Pow"))
			opcode = OP_RPOW;
		if (opcode) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = STACK_R8;
			ins->dreg = mono_alloc_dreg (cfg, (MonoStackType)ins->type);
			ins->sreg1 = args [0]->dreg;
			if (fsig->param_count == 2)
				ins->sreg2 = args [1]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}
	}
	/* The LLVM backend supports these intrinsics */
	if (cmethod->klass == mono_class_try_get_math_class ()) {
		if (strcmp (cmethod->name, "Sin") == 0) {
			opcode = OP_SIN;
		} else if (strcmp (cmethod->name, "Cos") == 0) {
			opcode = OP_COS;
		} else if (strcmp (cmethod->name, "Sqrt") == 0) {
			opcode = OP_SQRT;
		} else if (strcmp (cmethod->name, "Abs") == 0 && fsig->params [0]->type == MONO_TYPE_R8) {
			opcode = OP_ABS;
		}

		if (opcode && fsig->param_count == 1) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = STACK_R8;
			ins->dreg = mono_alloc_dreg (cfg, (MonoStackType)ins->type);
			ins->sreg1 = args [0]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}

		opcode = 0;
		if (cfg->opt & MONO_OPT_CMOV) {
			if (strcmp (cmethod->name, "Min") == 0) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_IMIN;
				if (fsig->params [0]->type == MONO_TYPE_U4)
					opcode = OP_IMIN_UN;
				else if (fsig->params [0]->type == MONO_TYPE_I8)
					opcode = OP_LMIN;
				else if (fsig->params [0]->type == MONO_TYPE_U8)
					opcode = OP_LMIN_UN;
			} else if (strcmp (cmethod->name, "Max") == 0) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_IMAX;
				if (fsig->params [0]->type == MONO_TYPE_U4)
					opcode = OP_IMAX_UN;
				else if (fsig->params [0]->type == MONO_TYPE_I8)
					opcode = OP_LMAX;
				else if (fsig->params [0]->type == MONO_TYPE_U8)
					opcode = OP_LMAX_UN;
			}
		}

		if (opcode && fsig->param_count == 2) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = fsig->params [0]->type == MONO_TYPE_I4 ? STACK_I4 : STACK_I8;
			ins->dreg = mono_alloc_dreg (cfg, (MonoStackType)ins->type);
			ins->sreg1 = args [0]->dreg;
			ins->sreg2 = args [1]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}
	}

	return ins;
}

static MonoInst*
emit_span_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins;

	MonoClassField *ptr_field = mono_class_get_field_from_name_full (cmethod->klass, "_pointer", NULL);
	if (!ptr_field)
		/* Portable Span<T> */
		return NULL;

	if (!strcmp (cmethod->name, "get_Item") && fsig->params [0]->type != MONO_TYPE_VALUETYPE) {
		MonoClassField *length_field = mono_class_get_field_from_name_full (cmethod->klass, "_length", NULL);

		g_assert (length_field);

		MonoGenericClass *gclass = mono_class_get_generic_class (cmethod->klass);
		MonoClass *param_class = mono_class_from_mono_type_internal (gclass->context.class_inst->type_argv [0]);

		if (mini_is_gsharedvt_variable_klass (param_class))
			return NULL;

		int span_reg = args [0]->dreg;
		/* Load _pointer.Value */
		int base_reg = alloc_preg (cfg);
		EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, base_reg, span_reg, ptr_field->offset - MONO_ABI_SIZEOF (MonoObject));
		/* Similar to mini_emit_ldelema_1_ins () */
		int size = mono_class_array_element_size (param_class);

		int index_reg = mini_emit_sext_index_reg (cfg, args [1]);

		MONO_EMIT_BOUNDS_CHECK_OFFSET(cfg, span_reg, length_field->offset - MONO_ABI_SIZEOF (MonoObject), index_reg);

		// FIXME: Sign extend index ?

		int mult_reg = alloc_preg (cfg);
		int add_reg = alloc_preg (cfg);

		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_MUL_IMM, mult_reg, index_reg, size);
		EMIT_NEW_BIALU (cfg, ins, OP_PADD, add_reg, base_reg, mult_reg);
		ins->klass = param_class;
		ins->type = STACK_MP;

		return ins;
	} else if (!strcmp (cmethod->name, "get_Length")) {
		MonoClassField *length_field = mono_class_get_field_from_name_full (cmethod->klass, "_length", NULL);
		g_assert (length_field);

		/*
		 * FIXME: This doesn't work with abcrem, since the src is a unique LDADDR not
		 * the same array object.
		 */
		MONO_INST_NEW (cfg, ins, OP_LDLEN);
		ins->dreg = alloc_preg (cfg);
		ins->sreg1 = args [0]->dreg;
		ins->inst_imm = length_field->offset - MONO_ABI_SIZEOF (MonoObject);
		ins->type = STACK_I4;
		MONO_ADD_INS (cfg->cbb, ins);

		cfg->flags |= MONO_CFG_NEEDS_DECOMPOSE;
		cfg->cbb->needs_decompose = TRUE;

		return ins;
	}

	return NULL;
}

static MonoInst*
emit_unsafe_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins;
	int dreg, align;
	MonoGenericContext *ctx = mono_method_get_context (cmethod);
	MonoType *t;

	if (!strcmp (cmethod->name, "As")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);

		if (ctx->method_inst->type_argc == 2) {
			dreg = alloc_preg (cfg);
			EMIT_NEW_UNALU (cfg, ins, OP_MOVE, dreg, args [0]->dreg);
			ins->type = STACK_OBJ;
			ins->klass = mono_get_object_class ();
			return ins;
		} else if (ctx->method_inst->type_argc == 1) {
			// Casts the given object to the specified type, performs no dynamic type checking.
			g_assert (fsig->param_count == 1);
			g_assert (fsig->params [0]->type == MONO_TYPE_OBJECT);
			dreg = alloc_preg (cfg);
			EMIT_NEW_UNALU (cfg, ins, OP_MOVE, dreg, args [0]->dreg);
			ins->type = STACK_OBJ;
			ins->klass = mono_class_from_mono_type_internal (ctx->method_inst->type_argv [0]);
			return ins;
		}
	} else if (!strcmp (cmethod->name, "AsPointer")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 1);

		dreg = alloc_preg (cfg);
		EMIT_NEW_UNALU (cfg, ins, OP_MOVE, dreg, args [0]->dreg);
		ins->type = STACK_PTR;
		return ins;
	} else if (!strcmp (cmethod->name, "AsRef")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 1);

		dreg = alloc_preg (cfg);
		EMIT_NEW_UNALU (cfg, ins, OP_MOVE, dreg, args [0]->dreg);
		ins->type = STACK_OBJ;
		ins->klass = mono_get_object_class ();
		return ins;
	} else if (!strcmp (cmethod->name, "AreSame")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 2);

		dreg = alloc_ireg (cfg);
		EMIT_NEW_BIALU (cfg, ins, OP_COMPARE, -1, args [0]->dreg, args [1]->dreg);
		EMIT_NEW_UNALU (cfg, ins, OP_PCEQ, dreg, -1);
		return ins;
	} else if (!strcmp (cmethod->name, "IsAddressLessThan")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 2);

		dreg = alloc_ireg (cfg);
		EMIT_NEW_BIALU (cfg, ins, OP_COMPARE, -1, args [0]->dreg, args [1]->dreg);
		EMIT_NEW_UNALU (cfg, ins, OP_PCLT, dreg, -1);
		return ins;
	} else if (!strcmp (cmethod->name, "IsAddressGreaterThan")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 2);

		dreg = alloc_ireg (cfg);
		EMIT_NEW_BIALU (cfg, ins, OP_COMPARE, -1, args [0]->dreg, args [1]->dreg);
		EMIT_NEW_UNALU (cfg, ins, OP_PCGT, dreg, -1);
		return ins;
	} else if (!strcmp (cmethod->name, "Add")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 2);

		int mul_reg = alloc_preg (cfg);

		t = ctx->method_inst->type_argv [0];
		MonoInst *esize_ins;
		if (mini_is_gsharedvt_variable_type (t)) {
			esize_ins = mini_emit_get_gsharedvt_info_klass (cfg, mono_class_from_mono_type_internal (t), MONO_RGCTX_INFO_CLASS_SIZEOF);
			if (SIZEOF_REGISTER == 8)
				MONO_EMIT_NEW_UNALU (cfg, OP_SEXT_I4, esize_ins->dreg, esize_ins->dreg);
		} else {
			t = mini_type_get_underlying_type (t);
			int esize = mono_class_array_element_size (mono_class_from_mono_type_internal (t));
			EMIT_NEW_ICONST (cfg, esize_ins, esize);
		}
		esize_ins->type = STACK_I4;

		EMIT_NEW_BIALU (cfg, ins, OP_PMUL, mul_reg, args [1]->dreg, esize_ins->dreg);
		ins->type = STACK_PTR;

		dreg = alloc_preg (cfg);
		EMIT_NEW_BIALU (cfg, ins, OP_PADD, dreg, args [0]->dreg, mul_reg);
		ins->type = STACK_PTR;
		return ins;
	} else if (!strcmp (cmethod->name, "AddByteOffset")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 2);

		if (fsig->params [1]->type == MONO_TYPE_I) {
			int dreg = alloc_preg (cfg);
			EMIT_NEW_BIALU (cfg, ins, OP_PADD, dreg, args [0]->dreg, args [1]->dreg);
			ins->type = STACK_PTR;
			return ins;
		} else if (fsig->params [1]->type == MONO_TYPE_U8) {
			int sreg = args [1]->dreg;
			if (SIZEOF_REGISTER == 4) {
				sreg = alloc_ireg (cfg);
				EMIT_NEW_UNALU (cfg, ins, OP_LCONV_TO_U4, sreg, args [1]->dreg);
			}
			int dreg = alloc_preg (cfg);
			EMIT_NEW_BIALU (cfg, ins, OP_PADD, dreg, args [0]->dreg, sreg);
			ins->type = STACK_PTR;
			return ins;
		}
	} else if (!strcmp (cmethod->name, "SizeOf")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 0);

		t = ctx->method_inst->type_argv [0];
		if (mini_is_gsharedvt_variable_type (t)) {
			ins = mini_emit_get_gsharedvt_info_klass (cfg, mono_class_from_mono_type_internal (t), MONO_RGCTX_INFO_CLASS_SIZEOF);
		} else {
			int esize = mono_type_size (t, &align);
			EMIT_NEW_ICONST (cfg, ins, esize);
		}
		ins->type = STACK_I4;
		return ins;
	} else if (!strcmp (cmethod->name, "ReadUnaligned")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 1);

		t = ctx->method_inst->type_argv [0];
		t = mini_get_underlying_type (t);
		if (MONO_TYPE_IS_PRIMITIVE (t) && t->type != MONO_TYPE_R4 && t->type != MONO_TYPE_R8) {
			dreg = alloc_ireg (cfg);
			EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, t, args [0]->dreg, 0);
			ins->type = STACK_I4;
			ins->flags |= MONO_INST_UNALIGNED;
			return ins;
		}
	} else if (!strcmp (cmethod->name, "WriteUnaligned")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 2);

		t = ctx->method_inst->type_argv [0];
		t = mini_get_underlying_type (t);
		if (MONO_TYPE_IS_PRIMITIVE (t) && t->type != MONO_TYPE_R4 && t->type != MONO_TYPE_R8) {
			dreg = alloc_ireg (cfg);
			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, t, args [0]->dreg, 0, args [1]->dreg);
			ins->flags |= MONO_INST_UNALIGNED;
			return ins;
		}
	} else if (!strcmp (cmethod->name, "ByteOffset")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 2);

		int dreg = alloc_preg (cfg);
		EMIT_NEW_BIALU (cfg, ins, OP_PSUB, dreg, args [1]->dreg, args [0]->dreg);
		ins->type = STACK_PTR;
		return ins;
	}

	return NULL;
}

static MonoInst*
emit_jit_helpers_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins;
	int dreg;
	MonoGenericContext *ctx = mono_method_get_context (cmethod);
	MonoType *t;

	if (!strcmp (cmethod->name, "EnumEquals") || !strcmp (cmethod->name, "EnumCompareTo")) {
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (fsig->param_count == 2);

		t = ctx->method_inst->type_argv [0];
		t = mini_get_underlying_type (t);
		if (mini_is_gsharedvt_variable_type (t))
			return NULL;

		gboolean is_i8 = (t->type == MONO_TYPE_I8 || t->type == MONO_TYPE_U8);
		gboolean is_unsigned = (t->type == MONO_TYPE_U1 || t->type == MONO_TYPE_U2 || t->type == MONO_TYPE_U4 || t->type == MONO_TYPE_U8 || t->type == MONO_TYPE_U);
		int cmp_op, ceq_op, cgt_op, clt_op;

		if (is_i8) {
			cmp_op = OP_LCOMPARE;
			ceq_op = OP_LCEQ;
			cgt_op = is_unsigned ? OP_LCGT_UN : OP_LCGT;
			clt_op = is_unsigned ? OP_LCLT_UN : OP_LCLT;
		} else {
			cmp_op = OP_ICOMPARE;
			ceq_op = OP_ICEQ;
			cgt_op = is_unsigned ? OP_ICGT_UN : OP_ICGT;
			clt_op = is_unsigned ? OP_ICLT_UN : OP_ICLT;
		}

		if (!strcmp (cmethod->name, "EnumEquals")) {
			dreg = alloc_ireg (cfg);
			EMIT_NEW_BIALU (cfg, ins, cmp_op, -1, args [0]->dreg, args [1]->dreg);
			EMIT_NEW_UNALU (cfg, ins, ceq_op, dreg, -1);
		} else {
			// Use the branchless code (a > b) - (a < b)
			int reg1, reg2;

			reg1 = alloc_ireg (cfg);
			reg2 = alloc_ireg (cfg);
			dreg = alloc_ireg (cfg);

			EMIT_NEW_BIALU (cfg, ins, cmp_op, -1, args [0]->dreg, args [1]->dreg);
			EMIT_NEW_UNALU (cfg, ins, cgt_op, reg1, -1);
			EMIT_NEW_BIALU (cfg, ins, cmp_op, -1, args [0]->dreg, args [1]->dreg);
			EMIT_NEW_UNALU (cfg, ins, clt_op, reg2, -1);
			EMIT_NEW_BIALU (cfg, ins, OP_ISUB, dreg, reg1, reg2);
		}
		return ins;
	}

	return NULL;
}

MonoInst*
mini_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;
	MonoClass *runtime_helpers_class = mono_class_get_runtime_helpers_class ();

	const char* cmethod_klass_name_space = m_class_get_name_space (cmethod->klass);
	const char* cmethod_klass_name = m_class_get_name (cmethod->klass);
	MonoImage *cmethod_klass_image = m_class_get_image (cmethod->klass);
	gboolean in_corlib = cmethod_klass_image == mono_defaults.corlib;

	/* Required intrinsics are always used even with -O=-intrins */
	if (in_corlib &&
		!strcmp (cmethod_klass_name_space, "System") &&
		!strcmp (cmethod_klass_name, "ByReference`1") &&
		!strcmp (cmethod->name, "get_Value")) {
		g_assert (fsig->hasthis && fsig->param_count == 0);
		int dreg = alloc_preg (cfg);
		EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, dreg, args [0]->dreg, 0);
		return ins;
	} else if (in_corlib && cmethod->klass == mono_defaults.object_class) {
		if (!strcmp (cmethod->name, "GetRawData")) {
			int dreg = alloc_preg (cfg);
			EMIT_NEW_BIALU_IMM (cfg, ins, OP_PADD_IMM, dreg, args [0]->dreg, MONO_ABI_SIZEOF (MonoObject));
			return ins;
		}
	}

	if (!(cfg->opt & MONO_OPT_INTRINS))
		return NULL;

	if (cmethod->klass == mono_defaults.string_class) {
		if (strcmp (cmethod->name, "get_Chars") == 0 && fsig->param_count + fsig->hasthis == 2) {
			int dreg = alloc_ireg (cfg);
			int index_reg = alloc_preg (cfg);
			int add_reg = alloc_preg (cfg);

#if SIZEOF_REGISTER == 8
			if (COMPILE_LLVM (cfg)) {
				MONO_EMIT_NEW_UNALU (cfg, OP_ZEXT_I4, index_reg, args [1]->dreg);
			} else {
				/* The array reg is 64 bits but the index reg is only 32 */
				MONO_EMIT_NEW_UNALU (cfg, OP_SEXT_I4, index_reg, args [1]->dreg);
			}
#else
			index_reg = args [1]->dreg;
#endif	
			MONO_EMIT_BOUNDS_CHECK (cfg, args [0]->dreg, MonoString, length, index_reg);

#if defined(TARGET_X86) || defined(TARGET_AMD64)
			EMIT_NEW_X86_LEA (cfg, ins, args [0]->dreg, index_reg, 1, MONO_STRUCT_OFFSET (MonoString, chars));
			add_reg = ins->dreg;
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADU2_MEMBASE, dreg, 
								   add_reg, 0);
#else
			int mult_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, mult_reg, index_reg, 1);
			MONO_EMIT_NEW_BIALU (cfg, OP_PADD, add_reg, mult_reg, args [0]->dreg);
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADU2_MEMBASE, dreg, 
								   add_reg, MONO_STRUCT_OFFSET (MonoString, chars));
#endif
			mini_type_from_op (cfg, ins, NULL, NULL);
			return ins;
		} else if (strcmp (cmethod->name, "get_Length") == 0 && fsig->param_count + fsig->hasthis == 1) {
			int dreg = alloc_ireg (cfg);
			/* Decompose later to allow more optimizations */
			EMIT_NEW_UNALU (cfg, ins, OP_STRLEN, dreg, args [0]->dreg);
			ins->type = STACK_I4;
			ins->flags |= MONO_INST_FAULT;
			cfg->cbb->needs_decompose = TRUE;
			cfg->flags |= MONO_CFG_NEEDS_DECOMPOSE;

			return ins;
		} else 
			return NULL;
	} else if (cmethod->klass == mono_defaults.object_class) {
		if (strcmp (cmethod->name, "GetType") == 0 && fsig->param_count + fsig->hasthis == 1) {
			int dreg = alloc_ireg_ref (cfg);
			int vt_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_FAULT (cfg, vt_reg, args [0]->dreg, MONO_STRUCT_OFFSET (MonoObject, vtable));
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, dreg, vt_reg, MONO_STRUCT_OFFSET (MonoVTable, type));
			mini_type_from_op (cfg, ins, NULL, NULL);

			return ins;
		} else if (!cfg->backend->emulate_mul_div && strcmp (cmethod->name, "InternalGetHashCode") == 0 && fsig->param_count == 1 && !mono_gc_is_moving ()) {
			int dreg = alloc_ireg (cfg);
			int t1 = alloc_ireg (cfg);
	
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, t1, args [0]->dreg, 3);
			EMIT_NEW_BIALU_IMM (cfg, ins, OP_MUL_IMM, dreg, t1, 2654435761u);
			ins->type = STACK_I4;

			return ins;
		} else if (strcmp (cmethod->name, ".ctor") == 0 && fsig->param_count == 0) {
 			MONO_INST_NEW (cfg, ins, OP_NOP);
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		} else
			return NULL;
	} else if (cmethod->klass == mono_defaults.array_class) {
		if (strcmp (cmethod->name, "GetGenericValueImpl") == 0 && fsig->param_count + fsig->hasthis == 3 && !cfg->gsharedvt)
			return emit_array_generic_access (cfg, fsig, args, FALSE);
		else if (strcmp (cmethod->name, "SetGenericValueImpl") == 0 && fsig->param_count + fsig->hasthis == 3 && !cfg->gsharedvt)
			return emit_array_generic_access (cfg, fsig, args, TRUE);
		else if (!strcmp (cmethod->name, "GetRawSzArrayData")) {
			int dreg = alloc_preg (cfg);
			EMIT_NEW_BIALU_IMM (cfg, ins, OP_PADD_IMM, dreg, args [0]->dreg, MONO_STRUCT_OFFSET (MonoArray, vector));
			return ins;
		}

#ifndef MONO_BIG_ARRAYS
		/*
		 * This is an inline version of GetLength/GetLowerBound(0) used frequently in
		 * Array methods.
		 */
		else if (((strcmp (cmethod->name, "GetLength") == 0 && fsig->param_count + fsig->hasthis == 2) ||
		         (strcmp (cmethod->name, "GetLowerBound") == 0 && fsig->param_count + fsig->hasthis == 2)) &&
		         args [1]->opcode == OP_ICONST && args [1]->inst_c0 == 0) {
			int dreg = alloc_ireg (cfg);
			int bounds_reg = alloc_ireg_mp (cfg);
			MonoBasicBlock *end_bb, *szarray_bb;
			gboolean get_length = strcmp (cmethod->name, "GetLength") == 0;

			NEW_BBLOCK (cfg, end_bb);
			NEW_BBLOCK (cfg, szarray_bb);

			EMIT_NEW_LOAD_MEMBASE_FAULT (cfg, ins, OP_LOAD_MEMBASE, bounds_reg,
										 args [0]->dreg, MONO_STRUCT_OFFSET (MonoArray, bounds));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, bounds_reg, 0);
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, szarray_bb);
			/* Non-szarray case */
			if (get_length)
				EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADI4_MEMBASE, dreg,
									   bounds_reg, MONO_STRUCT_OFFSET (MonoArrayBounds, length));
			else
				EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADI4_MEMBASE, dreg,
									   bounds_reg, MONO_STRUCT_OFFSET (MonoArrayBounds, lower_bound));
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);
			MONO_START_BB (cfg, szarray_bb);
			/* Szarray case */
			if (get_length)
				EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADI4_MEMBASE, dreg,
									   args [0]->dreg, MONO_STRUCT_OFFSET (MonoArray, max_length));
			else
				MONO_EMIT_NEW_ICONST (cfg, dreg, 0);
			MONO_START_BB (cfg, end_bb);

			EMIT_NEW_UNALU (cfg, ins, OP_MOVE, dreg, dreg);
			ins->type = STACK_I4;
			
			return ins;
		}
#endif

 		if (cmethod->name [0] != 'g')
 			return NULL;

		if (strcmp (cmethod->name, "get_Rank") == 0 && fsig->param_count + fsig->hasthis == 1) {
			int dreg = alloc_ireg (cfg);
			int vtable_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP_FAULT (cfg, OP_LOAD_MEMBASE, vtable_reg, 
												 args [0]->dreg, MONO_STRUCT_OFFSET (MonoObject, vtable));
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADU1_MEMBASE, dreg,
								   vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, rank));
			mini_type_from_op (cfg, ins, NULL, NULL);

			return ins;
		} else if (strcmp (cmethod->name, "get_Length") == 0 && fsig->param_count + fsig->hasthis == 1) {
			int dreg = alloc_ireg (cfg);

			EMIT_NEW_LOAD_MEMBASE_FAULT (cfg, ins, OP_LOADI4_MEMBASE, dreg, 
										 args [0]->dreg, MONO_STRUCT_OFFSET (MonoArray, max_length));
			mini_type_from_op (cfg, ins, NULL, NULL);

			return ins;
		} else
			return NULL;
	} else if (cmethod->klass == runtime_helpers_class) {
		if (strcmp (cmethod->name, "get_OffsetToStringData") == 0 && fsig->param_count == 0) {
			EMIT_NEW_ICONST (cfg, ins, MONO_STRUCT_OFFSET (MonoString, chars));
			return ins;
		} else if (strcmp (cmethod->name, "IsReferenceOrContainsReferences") == 0 && fsig->param_count == 0) {
			MonoGenericContext *ctx = mono_method_get_context (cmethod);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);
			MonoType *arg_type = ctx->method_inst->type_argv [0];
			MonoType *t;
			MonoClass *klass;

			ins = NULL;

			/* Resolve the argument class as possible so we can handle common cases fast */
			t = mini_get_underlying_type (arg_type);
			klass = mono_class_from_mono_type_internal (t);
			mono_class_init_internal (klass);
			if (MONO_TYPE_IS_REFERENCE (t))
				EMIT_NEW_ICONST (cfg, ins, 1);
			else if (MONO_TYPE_IS_PRIMITIVE (t))
				EMIT_NEW_ICONST (cfg, ins, 0);
			else if (cfg->gshared && (t->type == MONO_TYPE_VAR || t->type == MONO_TYPE_MVAR) && !mini_type_var_is_vt (t))
				EMIT_NEW_ICONST (cfg, ins, 1);
			else if (!cfg->gshared || !mini_class_check_context_used (cfg, klass))
				EMIT_NEW_ICONST (cfg, ins, m_class_has_references (klass) ? 1 : 0);
			else {
				g_assert (cfg->gshared);

				/* Have to use the original argument class here */
				MonoClass *arg_class = mono_class_from_mono_type_internal (arg_type);
				int context_used = mini_class_check_context_used (cfg, arg_class);

				/* This returns 1 or 2 */
				MonoInst *info = mini_emit_get_rgctx_klass (cfg, context_used, arg_class, MONO_RGCTX_INFO_CLASS_IS_REF_OR_CONTAINS_REFS);
				int dreg = alloc_ireg (cfg);
				EMIT_NEW_BIALU_IMM (cfg, ins, OP_ISUB_IMM, dreg, info->dreg, 1);
			}

			return ins;
		} else if (strcmp (cmethod->name, "IsBitwiseEquatable") == 0 && fsig->param_count == 0) {
			MonoGenericContext *ctx = mono_method_get_context (cmethod);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);
			MonoType *arg_type = ctx->method_inst->type_argv [0];
			MonoType *t;
			ins = NULL;

			/* Resolve the argument class as possible so we can handle common cases fast */
			t = mini_get_underlying_type (arg_type);

			if (MONO_TYPE_IS_PRIMITIVE (t) && t->type != MONO_TYPE_R4 && t->type != MONO_TYPE_R8)
				EMIT_NEW_ICONST (cfg, ins, 1);
			else
				EMIT_NEW_ICONST (cfg, ins, 0);
			return ins;
		} else if (!strcmp (cmethod->name, "ObjectHasComponentSize")) {
			g_assert (fsig->param_count == 1);
			g_assert (fsig->params [0]->type == MONO_TYPE_OBJECT);
			// Return true for arrays and string
			int dreg;

			dreg = alloc_ireg (cfg);

			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, dreg, args [0]->dreg, MONO_STRUCT_OFFSET (MonoObject, vtable));
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, dreg, dreg, MONO_STRUCT_OFFSET (MonoVTable, flags));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IAND_IMM, dreg, dreg, 	MONO_VT_FLAG_ARRAY_OR_STRING);
			EMIT_NEW_BIALU_IMM (cfg, ins, OP_COMPARE_IMM, -1, dreg, 0);
			EMIT_NEW_UNALU (cfg, ins, OP_ICGT, dreg, -1);
			ins->type = STACK_I4;
			return ins;
		} else
			return NULL;
	} else if (cmethod->klass == mono_defaults.monitor_class) {
		gboolean is_enter = FALSE;
		gboolean is_v4 = FALSE;

		if (!strcmp (cmethod->name, "Enter") && fsig->param_count == 2 && fsig->params [1]->byref) {
			is_enter = TRUE;
			is_v4 = TRUE;
		}
		if (!strcmp (cmethod->name, "Enter") && fsig->param_count == 1)
			is_enter = TRUE;

		if (is_enter) {
			/*
			 * To make async stack traces work, icalls which can block should have a wrapper.
			 * For Monitor.Enter, emit two calls: a fastpath which doesn't have a wrapper, and a slowpath, which does.
			 */
			MonoBasicBlock *end_bb;

			NEW_BBLOCK (cfg, end_bb);

			if (is_v4)
				ins = mono_emit_jit_icall (cfg, mono_monitor_enter_v4_fast, args);
			else
				ins = mono_emit_jit_icall (cfg, mono_monitor_enter_fast, args);

			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ICOMPARE_IMM, -1, ins->dreg, 0);
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBNE_UN, end_bb);

			if (is_v4)
				ins = mono_emit_jit_icall (cfg, mono_monitor_enter_v4_internal, args);
			else
				ins = mono_emit_jit_icall (cfg, mono_monitor_enter_internal, args);

			MONO_START_BB (cfg, end_bb);
			return ins;
		}
	} else if (cmethod->klass == mono_defaults.thread_class) {
		if (strcmp (cmethod->name, "SpinWait_nop") == 0 && fsig->param_count == 0) {
			MONO_INST_NEW (cfg, ins, OP_RELAXED_NOP);
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		} else if (strcmp (cmethod->name, "MemoryBarrier") == 0 && fsig->param_count == 0) {
			return mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_SEQ);
		} else if (!strcmp (cmethod->name, "VolatileRead") && fsig->param_count == 1) {
			guint32 opcode = 0;
			gboolean is_ref = mini_type_is_reference (fsig->params [0]);

			if (fsig->params [0]->type == MONO_TYPE_I1)
				opcode = OP_LOADI1_MEMBASE;
			else if (fsig->params [0]->type == MONO_TYPE_U1)
				opcode = OP_LOADU1_MEMBASE;
			else if (fsig->params [0]->type == MONO_TYPE_I2)
				opcode = OP_LOADI2_MEMBASE;
			else if (fsig->params [0]->type == MONO_TYPE_U2)
				opcode = OP_LOADU2_MEMBASE;
			else if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_LOADI4_MEMBASE;
			else if (fsig->params [0]->type == MONO_TYPE_U4)
				opcode = OP_LOADU4_MEMBASE;
			else if (fsig->params [0]->type == MONO_TYPE_I8 || fsig->params [0]->type == MONO_TYPE_U8)
				opcode = OP_LOADI8_MEMBASE;
			else if (fsig->params [0]->type == MONO_TYPE_R4)
				opcode = OP_LOADR4_MEMBASE;
			else if (fsig->params [0]->type == MONO_TYPE_R8)
				opcode = OP_LOADR8_MEMBASE;
			else if (is_ref || fsig->params [0]->type == MONO_TYPE_I || fsig->params [0]->type == MONO_TYPE_U)
				opcode = OP_LOAD_MEMBASE;

			if (opcode) {
				MONO_INST_NEW (cfg, ins, opcode);
				ins->inst_basereg = args [0]->dreg;
				ins->inst_offset = 0;
				MONO_ADD_INS (cfg->cbb, ins);

				switch (fsig->params [0]->type) {
				case MONO_TYPE_I1:
				case MONO_TYPE_U1:
				case MONO_TYPE_I2:
				case MONO_TYPE_U2:
				case MONO_TYPE_I4:
				case MONO_TYPE_U4:
					ins->dreg = mono_alloc_ireg (cfg);
					ins->type = STACK_I4;
					break;
				case MONO_TYPE_I8:
				case MONO_TYPE_U8:
					ins->dreg = mono_alloc_lreg (cfg);
					ins->type = STACK_I8;
					break;
				case MONO_TYPE_I:
				case MONO_TYPE_U:
					ins->dreg = mono_alloc_ireg (cfg);
#if SIZEOF_REGISTER == 8
					ins->type = STACK_I8;
#else
					ins->type = STACK_I4;
#endif
					break;
				case MONO_TYPE_R4:
				case MONO_TYPE_R8:
					ins->dreg = mono_alloc_freg (cfg);
					ins->type = STACK_R8;
					break;
				default:
					g_assert (mini_type_is_reference (fsig->params [0]));
					ins->dreg = mono_alloc_ireg_ref (cfg);
					ins->type = STACK_OBJ;
					break;
				}

				if (opcode == OP_LOADI8_MEMBASE)
					ins = mono_decompose_opcode (cfg, ins);

				mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_SEQ);

				return ins;
			}
		} else if (!strcmp (cmethod->name, "VolatileWrite") && fsig->param_count == 2) {
			guint32 opcode = 0;
			gboolean is_ref = mini_type_is_reference (fsig->params [0]);

			if (fsig->params [0]->type == MONO_TYPE_I1 || fsig->params [0]->type == MONO_TYPE_U1)
				opcode = OP_STOREI1_MEMBASE_REG;
			else if (fsig->params [0]->type == MONO_TYPE_I2 || fsig->params [0]->type == MONO_TYPE_U2)
				opcode = OP_STOREI2_MEMBASE_REG;
			else if (fsig->params [0]->type == MONO_TYPE_I4 || fsig->params [0]->type == MONO_TYPE_U4)
				opcode = OP_STOREI4_MEMBASE_REG;
			else if (fsig->params [0]->type == MONO_TYPE_I8 || fsig->params [0]->type == MONO_TYPE_U8)
				opcode = OP_STOREI8_MEMBASE_REG;
			else if (fsig->params [0]->type == MONO_TYPE_R4)
				opcode = OP_STORER4_MEMBASE_REG;
			else if (fsig->params [0]->type == MONO_TYPE_R8)
				opcode = OP_STORER8_MEMBASE_REG;
			else if (is_ref || fsig->params [0]->type == MONO_TYPE_I || fsig->params [0]->type == MONO_TYPE_U)
				opcode = OP_STORE_MEMBASE_REG;

			if (opcode) {
				mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_SEQ);

				MONO_INST_NEW (cfg, ins, opcode);
				ins->sreg1 = args [1]->dreg;
				ins->inst_destbasereg = args [0]->dreg;
				ins->inst_offset = 0;
				MONO_ADD_INS (cfg->cbb, ins);

				if (opcode == OP_STOREI8_MEMBASE_REG)
					ins = mono_decompose_opcode (cfg, ins);

				return ins;
			}
		}
	} else if (in_corlib &&
			(strcmp (cmethod_klass_name_space, "System.Threading") == 0) &&
			(strcmp (cmethod_klass_name, "Interlocked") == 0)) {
		ins = NULL;

#if SIZEOF_REGISTER == 8
		if (!cfg->llvm_only && strcmp (cmethod->name, "Read") == 0 && fsig->param_count == 1 && (fsig->params [0]->type == MONO_TYPE_I8)) {
			if (!cfg->llvm_only && mono_arch_opcode_supported (OP_ATOMIC_LOAD_I8)) {
				MONO_INST_NEW (cfg, ins, OP_ATOMIC_LOAD_I8);
				ins->dreg = mono_alloc_preg (cfg);
				ins->sreg1 = args [0]->dreg;
				ins->type = STACK_I8;
				ins->backend.memory_barrier_kind = MONO_MEMORY_BARRIER_SEQ;
				MONO_ADD_INS (cfg->cbb, ins);
			} else {
				MonoInst *load_ins;

				mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_SEQ);

				/* 64 bit reads are already atomic */
				MONO_INST_NEW (cfg, load_ins, OP_LOADI8_MEMBASE);
				load_ins->dreg = mono_alloc_preg (cfg);
				load_ins->inst_basereg = args [0]->dreg;
				load_ins->inst_offset = 0;
				load_ins->type = STACK_I8;
				MONO_ADD_INS (cfg->cbb, load_ins);

				mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_SEQ);

				ins = load_ins;
			}
		}
#endif

		if (strcmp (cmethod->name, "Increment") == 0 && fsig->param_count == 1) {
			MonoInst *ins_iconst;
			guint32 opcode = 0;

			if (fsig->params [0]->type == MONO_TYPE_I4) {
				opcode = OP_ATOMIC_ADD_I4;
				cfg->has_atomic_add_i4 = TRUE;
			}
#if SIZEOF_REGISTER == 8
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_I8;
#endif
			if (opcode) {
				if (!mono_arch_opcode_supported (opcode))
					return NULL;
				MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
				ins_iconst->inst_c0 = 1;
				ins_iconst->dreg = mono_alloc_ireg (cfg);
				MONO_ADD_INS (cfg->cbb, ins_iconst);

				MONO_INST_NEW (cfg, ins, opcode);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->inst_basereg = args [0]->dreg;
				ins->inst_offset = 0;
				ins->sreg2 = ins_iconst->dreg;
				ins->type = (opcode == OP_ATOMIC_ADD_I4) ? STACK_I4 : STACK_I8;
				MONO_ADD_INS (cfg->cbb, ins);
			}
		} else if (strcmp (cmethod->name, "Decrement") == 0 && fsig->param_count == 1) {
			MonoInst *ins_iconst;
			guint32 opcode = 0;

			if (fsig->params [0]->type == MONO_TYPE_I4) {
				opcode = OP_ATOMIC_ADD_I4;
				cfg->has_atomic_add_i4 = TRUE;
			}
#if SIZEOF_REGISTER == 8
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_I8;
#endif
			if (opcode) {
				if (!mono_arch_opcode_supported (opcode))
					return NULL;
				MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
				ins_iconst->inst_c0 = -1;
				ins_iconst->dreg = mono_alloc_ireg (cfg);
				MONO_ADD_INS (cfg->cbb, ins_iconst);

				MONO_INST_NEW (cfg, ins, opcode);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->inst_basereg = args [0]->dreg;
				ins->inst_offset = 0;
				ins->sreg2 = ins_iconst->dreg;
				ins->type = (opcode == OP_ATOMIC_ADD_I4) ? STACK_I4 : STACK_I8;
				MONO_ADD_INS (cfg->cbb, ins);
			}
		} else if (strcmp (cmethod->name, "Add") == 0 && fsig->param_count == 2) {
			guint32 opcode = 0;

			if (fsig->params [0]->type == MONO_TYPE_I4) {
				opcode = OP_ATOMIC_ADD_I4;
				cfg->has_atomic_add_i4 = TRUE;
			}
#if SIZEOF_REGISTER == 8
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_I8;
#endif
			if (opcode) {
				if (!mono_arch_opcode_supported (opcode))
					return NULL;
				MONO_INST_NEW (cfg, ins, opcode);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->inst_basereg = args [0]->dreg;
				ins->inst_offset = 0;
				ins->sreg2 = args [1]->dreg;
				ins->type = (opcode == OP_ATOMIC_ADD_I4) ? STACK_I4 : STACK_I8;
				MONO_ADD_INS (cfg->cbb, ins);
			}
		}
		else if (strcmp (cmethod->name, "Exchange") == 0 && fsig->param_count == 2) {
			MonoInst *f2i = NULL, *i2f;
			guint32 opcode, f2i_opcode, i2f_opcode;
			gboolean is_ref = mini_type_is_reference (fsig->params [0]);
			gboolean is_float = fsig->params [0]->type == MONO_TYPE_R4 || fsig->params [0]->type == MONO_TYPE_R8;

			if (fsig->params [0]->type == MONO_TYPE_I4 ||
			    fsig->params [0]->type == MONO_TYPE_R4) {
				opcode = OP_ATOMIC_EXCHANGE_I4;
				f2i_opcode = OP_MOVE_F_TO_I4;
				i2f_opcode = OP_MOVE_I4_TO_F;
				cfg->has_atomic_exchange_i4 = TRUE;
			}
#if SIZEOF_REGISTER == 8
			else if (is_ref ||
			         fsig->params [0]->type == MONO_TYPE_I8 ||
			         fsig->params [0]->type == MONO_TYPE_R8 ||
			         fsig->params [0]->type == MONO_TYPE_I) {
				opcode = OP_ATOMIC_EXCHANGE_I8;
				f2i_opcode = OP_MOVE_F_TO_I8;
				i2f_opcode = OP_MOVE_I8_TO_F;
			}
#else
			else if (is_ref || fsig->params [0]->type == MONO_TYPE_I) {
				opcode = OP_ATOMIC_EXCHANGE_I4;
				cfg->has_atomic_exchange_i4 = TRUE;
			}
#endif
			else
				return NULL;

			if (!mono_arch_opcode_supported (opcode))
				return NULL;

			if (is_float) {
				/* TODO: Decompose these opcodes instead of bailing here. */
				if (COMPILE_SOFT_FLOAT (cfg))
					return NULL;

				MONO_INST_NEW (cfg, f2i, f2i_opcode);
				f2i->dreg = mono_alloc_ireg (cfg);
				f2i->sreg1 = args [1]->dreg;
				if (f2i_opcode == OP_MOVE_F_TO_I4)
					f2i->backend.spill_var = mini_get_int_to_float_spill_area (cfg);
				MONO_ADD_INS (cfg->cbb, f2i);
			}

			MONO_INST_NEW (cfg, ins, opcode);
			ins->dreg = is_ref ? mono_alloc_ireg_ref (cfg) : mono_alloc_ireg (cfg);
			ins->inst_basereg = args [0]->dreg;
			ins->inst_offset = 0;
			ins->sreg2 = is_float ? f2i->dreg : args [1]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);

			switch (fsig->params [0]->type) {
			case MONO_TYPE_I4:
				ins->type = STACK_I4;
				break;
			case MONO_TYPE_I8:
				ins->type = STACK_I8;
				break;
			case MONO_TYPE_I:
#if SIZEOF_REGISTER == 8
				ins->type = STACK_I8;
#else
				ins->type = STACK_I4;
#endif
				break;
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
				ins->type = STACK_R8;
				break;
			default:
				g_assert (mini_type_is_reference (fsig->params [0]));
				ins->type = STACK_OBJ;
				break;
			}

			if (is_float) {
				MONO_INST_NEW (cfg, i2f, i2f_opcode);
				i2f->dreg = mono_alloc_freg (cfg);
				i2f->sreg1 = ins->dreg;
				i2f->type = STACK_R8;
				if (i2f_opcode == OP_MOVE_I4_TO_F)
					i2f->backend.spill_var = mini_get_int_to_float_spill_area (cfg);
				MONO_ADD_INS (cfg->cbb, i2f);

				ins = i2f;
			}

			if (cfg->gen_write_barriers && is_ref)
				mini_emit_write_barrier (cfg, args [0], args [1]);
		}
		else if ((strcmp (cmethod->name, "CompareExchange") == 0) && fsig->param_count == 3) {
			MonoInst *f2i_new = NULL, *f2i_cmp = NULL, *i2f;
			guint32 opcode, f2i_opcode, i2f_opcode;
			gboolean is_ref = mini_type_is_reference (fsig->params [1]);
			gboolean is_float = fsig->params [1]->type == MONO_TYPE_R4 || fsig->params [1]->type == MONO_TYPE_R8;

			if (fsig->params [1]->type == MONO_TYPE_I4 ||
			    fsig->params [1]->type == MONO_TYPE_R4) {
				opcode = OP_ATOMIC_CAS_I4;
				f2i_opcode = OP_MOVE_F_TO_I4;
				i2f_opcode = OP_MOVE_I4_TO_F;
				cfg->has_atomic_cas_i4 = TRUE;
			}
#if SIZEOF_REGISTER == 8
			else if (is_ref ||
			         fsig->params [1]->type == MONO_TYPE_I8 ||
			         fsig->params [1]->type == MONO_TYPE_R8 ||
			         fsig->params [1]->type == MONO_TYPE_I) {
				opcode = OP_ATOMIC_CAS_I8;
				f2i_opcode = OP_MOVE_F_TO_I8;
				i2f_opcode = OP_MOVE_I8_TO_F;
			}
#else
			else if (is_ref || fsig->params [1]->type == MONO_TYPE_I) {
				opcode = OP_ATOMIC_CAS_I4;
				cfg->has_atomic_cas_i4 = TRUE;
			}
#endif
			else
				return NULL;

			if (!mono_arch_opcode_supported (opcode))
				return NULL;

			if (is_float) {
				/* TODO: Decompose these opcodes instead of bailing here. */
				if (COMPILE_SOFT_FLOAT (cfg))
					return NULL;

				MONO_INST_NEW (cfg, f2i_new, f2i_opcode);
				f2i_new->dreg = mono_alloc_ireg (cfg);
				f2i_new->sreg1 = args [1]->dreg;
				if (f2i_opcode == OP_MOVE_F_TO_I4)
					f2i_new->backend.spill_var = mini_get_int_to_float_spill_area (cfg);
				MONO_ADD_INS (cfg->cbb, f2i_new);

				MONO_INST_NEW (cfg, f2i_cmp, f2i_opcode);
				f2i_cmp->dreg = mono_alloc_ireg (cfg);
				f2i_cmp->sreg1 = args [2]->dreg;
				if (f2i_opcode == OP_MOVE_F_TO_I4)
					f2i_cmp->backend.spill_var = mini_get_int_to_float_spill_area (cfg);
				MONO_ADD_INS (cfg->cbb, f2i_cmp);
			}

			MONO_INST_NEW (cfg, ins, opcode);
			ins->dreg = is_ref ? alloc_ireg_ref (cfg) : alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->sreg2 = is_float ? f2i_new->dreg : args [1]->dreg;
			ins->sreg3 = is_float ? f2i_cmp->dreg : args [2]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);

			switch (fsig->params [1]->type) {
			case MONO_TYPE_I4:
				ins->type = STACK_I4;
				break;
			case MONO_TYPE_I8:
				ins->type = STACK_I8;
				break;
			case MONO_TYPE_I:
#if SIZEOF_REGISTER == 8
				ins->type = STACK_I8;
#else
				ins->type = STACK_I4;
#endif
				break;
			case MONO_TYPE_R4:
				ins->type = cfg->r4_stack_type;
				break;
			case MONO_TYPE_R8:
				ins->type = STACK_R8;
				break;
			default:
				g_assert (mini_type_is_reference (fsig->params [1]));
				ins->type = STACK_OBJ;
				break;
			}

			if (is_float) {
				MONO_INST_NEW (cfg, i2f, i2f_opcode);
				i2f->dreg = mono_alloc_freg (cfg);
				i2f->sreg1 = ins->dreg;
				i2f->type = STACK_R8;
				if (i2f_opcode == OP_MOVE_I4_TO_F)
					i2f->backend.spill_var = mini_get_int_to_float_spill_area (cfg);
				MONO_ADD_INS (cfg->cbb, i2f);

				ins = i2f;
			}

			if (cfg->gen_write_barriers && is_ref)
				mini_emit_write_barrier (cfg, args [0], args [1]);
		}
		else if ((strcmp (cmethod->name, "CompareExchange") == 0) && fsig->param_count == 4 &&
		         fsig->params [1]->type == MONO_TYPE_I4) {
			MonoInst *cmp, *ceq;

			if (!mono_arch_opcode_supported (OP_ATOMIC_CAS_I4))
				return NULL;

			/* int32 r = CAS (location, value, comparand); */
			MONO_INST_NEW (cfg, ins, OP_ATOMIC_CAS_I4);
			ins->dreg = alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->sreg2 = args [1]->dreg;
			ins->sreg3 = args [2]->dreg;
			ins->type = STACK_I4;
			MONO_ADD_INS (cfg->cbb, ins);

			/* bool result = r == comparand; */
			MONO_INST_NEW (cfg, cmp, OP_ICOMPARE);
			cmp->sreg1 = ins->dreg;
			cmp->sreg2 = args [2]->dreg;
			cmp->type = STACK_I4;
			MONO_ADD_INS (cfg->cbb, cmp);

			MONO_INST_NEW (cfg, ceq, OP_ICEQ);
			ceq->dreg = alloc_ireg (cfg);
			ceq->type = STACK_I4;
			MONO_ADD_INS (cfg->cbb, ceq);

			/* *success = result; */
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, args [3]->dreg, 0, ceq->dreg);

			cfg->has_atomic_cas_i4 = TRUE;
		}
		else if (strcmp (cmethod->name, "MemoryBarrier") == 0 && fsig->param_count == 0)
			ins = mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_SEQ);

		if (ins)
			return ins;
	} else if (in_corlib &&
			(strcmp (cmethod_klass_name_space, "System.Threading") == 0) &&
			(strcmp (cmethod_klass_name, "Volatile") == 0)) {
		ins = NULL;

		if (!cfg->llvm_only && !strcmp (cmethod->name, "Read") && fsig->param_count == 1) {
			guint32 opcode = 0;
			MonoType *t = fsig->params [0];
			gboolean is_ref;
			gboolean is_float = t->type == MONO_TYPE_R4 || t->type == MONO_TYPE_R8;

			g_assert (t->byref);
			/* t is a byref type, so the reference check is more complicated */
			is_ref = mini_type_is_reference (m_class_get_byval_arg (mono_class_from_mono_type_internal (t)));
			if (t->type == MONO_TYPE_I1)
				opcode = OP_ATOMIC_LOAD_I1;
			else if (t->type == MONO_TYPE_U1 || t->type == MONO_TYPE_BOOLEAN)
				opcode = OP_ATOMIC_LOAD_U1;
			else if (t->type == MONO_TYPE_I2)
				opcode = OP_ATOMIC_LOAD_I2;
			else if (t->type == MONO_TYPE_U2)
				opcode = OP_ATOMIC_LOAD_U2;
			else if (t->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_LOAD_I4;
			else if (t->type == MONO_TYPE_U4)
				opcode = OP_ATOMIC_LOAD_U4;
			else if (t->type == MONO_TYPE_R4)
				opcode = OP_ATOMIC_LOAD_R4;
			else if (t->type == MONO_TYPE_R8)
				opcode = OP_ATOMIC_LOAD_R8;
#if SIZEOF_REGISTER == 8
			else if (t->type == MONO_TYPE_I8 || t->type == MONO_TYPE_I)
				opcode = OP_ATOMIC_LOAD_I8;
			else if (is_ref || t->type == MONO_TYPE_U8 || t->type == MONO_TYPE_U)
				opcode = OP_ATOMIC_LOAD_U8;
#else
			else if (t->type == MONO_TYPE_I)
				opcode = OP_ATOMIC_LOAD_I4;
			else if (is_ref || t->type == MONO_TYPE_U)
				opcode = OP_ATOMIC_LOAD_U4;
#endif

			if (opcode) {
				if (!mono_arch_opcode_supported (opcode))
					return NULL;

				MONO_INST_NEW (cfg, ins, opcode);
				ins->dreg = is_ref ? mono_alloc_ireg_ref (cfg) : (is_float ? mono_alloc_freg (cfg) : mono_alloc_ireg (cfg));
				ins->sreg1 = args [0]->dreg;
				ins->backend.memory_barrier_kind = MONO_MEMORY_BARRIER_ACQ;
				MONO_ADD_INS (cfg->cbb, ins);

				switch (t->type) {
				case MONO_TYPE_BOOLEAN:
				case MONO_TYPE_I1:
				case MONO_TYPE_U1:
				case MONO_TYPE_I2:
				case MONO_TYPE_U2:
				case MONO_TYPE_I4:
				case MONO_TYPE_U4:
					ins->type = STACK_I4;
					break;
				case MONO_TYPE_I8:
				case MONO_TYPE_U8:
					ins->type = STACK_I8;
					break;
				case MONO_TYPE_I:
				case MONO_TYPE_U:
#if SIZEOF_REGISTER == 8
					ins->type = STACK_I8;
#else
					ins->type = STACK_I4;
#endif
					break;
				case MONO_TYPE_R4:
					ins->type = cfg->r4_stack_type;
					break;
				case MONO_TYPE_R8:
					ins->type = STACK_R8;
					break;
				default:
					g_assert (is_ref);
					ins->type = STACK_OBJ;
					break;
				}
			}
		}

		if (!cfg->llvm_only && !strcmp (cmethod->name, "Write") && fsig->param_count == 2) {
			guint32 opcode = 0;
			MonoType *t = fsig->params [0];
			gboolean is_ref;

			g_assert (t->byref);
			is_ref = mini_type_is_reference (m_class_get_byval_arg (mono_class_from_mono_type_internal (t)));
			if (t->type == MONO_TYPE_I1)
				opcode = OP_ATOMIC_STORE_I1;
			else if (t->type == MONO_TYPE_U1 || t->type == MONO_TYPE_BOOLEAN)
				opcode = OP_ATOMIC_STORE_U1;
			else if (t->type == MONO_TYPE_I2)
				opcode = OP_ATOMIC_STORE_I2;
			else if (t->type == MONO_TYPE_U2)
				opcode = OP_ATOMIC_STORE_U2;
			else if (t->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_STORE_I4;
			else if (t->type == MONO_TYPE_U4)
				opcode = OP_ATOMIC_STORE_U4;
			else if (t->type == MONO_TYPE_R4)
				opcode = OP_ATOMIC_STORE_R4;
			else if (t->type == MONO_TYPE_R8)
				opcode = OP_ATOMIC_STORE_R8;
#if SIZEOF_REGISTER == 8
			else if (t->type == MONO_TYPE_I8 || t->type == MONO_TYPE_I)
				opcode = OP_ATOMIC_STORE_I8;
			else if (is_ref || t->type == MONO_TYPE_U8 || t->type == MONO_TYPE_U)
				opcode = OP_ATOMIC_STORE_U8;
#else
			else if (t->type == MONO_TYPE_I)
				opcode = OP_ATOMIC_STORE_I4;
			else if (is_ref || t->type == MONO_TYPE_U)
				opcode = OP_ATOMIC_STORE_U4;
#endif

			if (opcode) {
				if (!mono_arch_opcode_supported (opcode))
					return NULL;

				MONO_INST_NEW (cfg, ins, opcode);
				ins->dreg = args [0]->dreg;
				ins->sreg1 = args [1]->dreg;
				ins->backend.memory_barrier_kind = MONO_MEMORY_BARRIER_REL;
				MONO_ADD_INS (cfg->cbb, ins);

				if (cfg->gen_write_barriers && is_ref)
					mini_emit_write_barrier (cfg, args [0], args [1]);
			}
		}

		if (ins)
			return ins;
	} else if (in_corlib &&
			(strcmp (cmethod_klass_name_space, "System.Diagnostics") == 0) &&
			(strcmp (cmethod_klass_name, "Debugger") == 0)) {
		if (!strcmp (cmethod->name, "Break") && fsig->param_count == 0) {
			if (mini_should_insert_breakpoint (cfg->method)) {
				ins = mono_emit_jit_icall (cfg, mono_debugger_agent_user_break, NULL);
			} else {
				MONO_INST_NEW (cfg, ins, OP_NOP);
				MONO_ADD_INS (cfg->cbb, ins);
			}
			return ins;
		}
	} else if (in_corlib &&
	        	(strcmp (cmethod_klass_name_space, "System") == 0) &&
	        	(strcmp (cmethod_klass_name, "Environment") == 0)) {
		if (!strcmp (cmethod->name, "get_IsRunningOnWindows") && fsig->param_count == 0) {
#ifdef TARGET_WIN32
			EMIT_NEW_ICONST (cfg, ins, 1);
#else
			EMIT_NEW_ICONST (cfg, ins, 0);
#endif
		}
	} else if (in_corlib &&
			(strcmp (cmethod_klass_name_space, "System.Reflection") == 0) &&
			(strcmp (cmethod_klass_name, "Assembly") == 0)) {
		if (cfg->llvm_only && !strcmp (cmethod->name, "GetExecutingAssembly")) {
			/* No stack walks are currently available, so implement this as an intrinsic */
			MonoInst *assembly_ins;

			EMIT_NEW_AOTCONST (cfg, assembly_ins, MONO_PATCH_INFO_IMAGE, m_class_get_image (cfg->method->klass));
			ins = mono_emit_jit_icall (cfg, mono_get_assembly_object, &assembly_ins);
			return ins;
		}

		// While it is not required per
		//  https://msdn.microsoft.com/en-us/library/system.reflection.assembly.getcallingassembly(v=vs.110).aspx.
		// have GetCallingAssembly be consistent independently of varying optimization.
		// This fixes mono/tests/test-inline-call-stack.cs under FullAOT+LLVM.
		cfg->no_inline |= COMPILE_LLVM (cfg) && strcmp (cmethod->name, "GetCallingAssembly") == 0;

	} else if (in_corlib &&
			   (strcmp (cmethod_klass_name_space, "System.Reflection") == 0) &&
			   (strcmp (cmethod_klass_name, "MethodBase") == 0)) {
		if (cfg->llvm_only && !strcmp (cmethod->name, "GetCurrentMethod")) {
			/* No stack walks are currently available, so implement this as an intrinsic */
			MonoInst *method_ins;
			MonoMethod *declaring = cfg->method;

			/* This returns the declaring generic method */
			if (declaring->is_inflated)
				declaring = ((MonoMethodInflated*)cfg->method)->declaring;
			EMIT_NEW_AOTCONST (cfg, method_ins, MONO_PATCH_INFO_METHODCONST, declaring);
			ins = mono_emit_jit_icall (cfg, mono_get_method_object, &method_ins);
			cfg->no_inline = TRUE;
			if (cfg->method != cfg->current_method)
				mini_set_inline_failure (cfg, "MethodBase:GetCurrentMethod ()");
			return ins;
		}
	} else if (cmethod->klass == mono_class_try_get_math_class ()) {
		/* 
		 * There is general branchless code for Min/Max, but it does not work for 
		 * all inputs:
		 * http://everything2.com/?node_id=1051618
		 */

		/*
		 * Constant folding for various Math methods.
		 * we avoid folding constants that when computed would raise an error, in
		 * case the user code was expecting to get that error raised
		 */
		if (fsig->param_count == 1 && args [0]->opcode == OP_R8CONST){
			double source = *(double *)args [0]->inst_p0;
			int opcode = 0;
			const char *mname = cmethod->name;
			char c = mname [0];
			
			if (c == 'A'){
				if (strcmp (mname, "Abs") == 0 && fsig->params [0]->type == MONO_TYPE_R8) {
					opcode = OP_ABS;
				} else if (strcmp (mname, "Asin") == 0){
					if (fabs (source) <= 1)
						opcode = OP_ASIN;
				} else if (strcmp (mname, "Asinh") == 0){
					opcode = OP_ASINH;
				} else if (strcmp (mname, "Acos") == 0){
					if (fabs (source) <= 1)
						opcode = OP_ACOS;
				} else if (strcmp (mname, "Acosh") == 0){
					if (source >= 1)
						opcode = OP_ACOSH;
				} else if (strcmp (mname, "Atan") == 0){
					opcode = OP_ATAN;
				} else if (strcmp (mname, "Atanh") == 0){
					if (fabs (source) < 1)
						opcode = OP_ATANH;
				} 
			} else if (c == 'C'){
				if (strcmp (mname, "Cos") == 0) {
					if (!isinf (source))
						opcode = OP_COS;
				} else if (strcmp (mname, "Cbrt") == 0){
					opcode = OP_CBRT;
				} else if (strcmp (mname, "Cosh") == 0){
					opcode = OP_COSH;
				}
			} else if (c == 'R'){
				if (strcmp (mname, "Round") == 0)
					opcode = OP_ROUND;
			} else if (c == 'S'){
				if (strcmp (mname, "Sin") == 0) {
					if (!isinf (source))
						opcode = OP_SIN;
				} else if (strcmp (mname, "Sqrt") == 0) {
					if (source >= 0)
						opcode = OP_SQRT;
				} else if (strcmp (mname, "Sinh") == 0){
					opcode = OP_SINH;
				}
			} else if (c == 'T'){
				if (strcmp (mname, "Tan") == 0){
					if (!isinf (source))
						opcode = OP_TAN;
				} else if (strcmp (mname, "Tanh") == 0){
					opcode = OP_TANH;
				}
			}

			if (opcode) {
				double *dest = (double *) mono_domain_alloc (cfg->domain, sizeof (double));
				double result;
				MONO_INST_NEW (cfg, ins, OP_R8CONST);
				ins->type = STACK_R8;
				ins->dreg = mono_alloc_dreg (cfg, (MonoStackType) ins->type);
				ins->inst_p0 = dest;
				
				switch (opcode){
				case OP_ABS:
					result = fabs (source);
					break;
				case OP_ACOS:
					result = acos (source);
					break;
				case OP_ACOSH:
					result = acosh (source);
					break;
				case OP_ASIN:
					result = asin (source);
					break;
				case OP_ASINH:
					result= asinh (source);
					break;
				case OP_ATAN:
					result = atan (source);
					break;
				case OP_ATANH:
					result = atanh (source);
					break;
				case OP_CBRT:
					result = cbrt (source);
					break;
				case OP_COS:
					result = cos (source);
					break;
				case OP_COSH:
					result = cosh (source);
					break;
				case OP_ROUND:
					result = round (source);
					break;
				case OP_SIN:
					result = sin (source);
					break;
				case OP_SINH:
					result = sinh (source);
					break;
				case OP_SQRT:
					result = sqrt (source);
					break;
				case OP_TAN:
					result = tan (source);
					break;
				case OP_TANH:
					result = tanh (source);
					break;
				}
				*dest = result;
				MONO_ADD_INS (cfg->cbb, ins);
				NULLIFY_INS (args [0]);
				return ins;
			}
		}
	} else if (cmethod->klass == mono_defaults.systemtype_class && !strcmp (cmethod->name, "op_Equality")) {
		EMIT_NEW_BIALU (cfg, ins, OP_COMPARE, -1, args [0]->dreg, args [1]->dreg);
		MONO_INST_NEW (cfg, ins, OP_PCEQ);
		ins->dreg = alloc_preg (cfg);
		ins->type = STACK_I4;
		MONO_ADD_INS (cfg->cbb, ins);
		return ins;
	} else if (((!strcmp (cmethod_klass_image->assembly->aname.name, "MonoMac") ||
	            !strcmp (cmethod_klass_image->assembly->aname.name, "monotouch")) &&
				!strcmp (cmethod_klass_name_space, "XamCore.ObjCRuntime") &&
				!strcmp (cmethod_klass_name, "Selector")) ||
			   ((!strcmp (cmethod_klass_image->assembly->aname.name, "Xamarin.iOS") ||
				 !strcmp (cmethod_klass_image->assembly->aname.name, "Xamarin.Mac")) &&
				!strcmp (cmethod_klass_name_space, "ObjCRuntime") &&
				!strcmp (cmethod_klass_name, "Selector"))
			   ) {
		if ((cfg->backend->have_objc_get_selector || cfg->compile_llvm) &&
			!strcmp (cmethod->name, "GetHandle") && fsig->param_count == 1 &&
		    (args [0]->opcode == OP_GOT_ENTRY || args [0]->opcode == OP_AOTCONST) &&
		    cfg->compile_aot) {
			MonoInst *pi;
			MonoJumpInfoToken *ji;
			char *s;

			if (args [0]->opcode == OP_GOT_ENTRY) {
				pi = (MonoInst *)args [0]->inst_p1;
				g_assert (pi->opcode == OP_PATCH_INFO);
				g_assert (GPOINTER_TO_INT (pi->inst_p1) == MONO_PATCH_INFO_LDSTR);
				ji = (MonoJumpInfoToken *)pi->inst_p0;
			} else {
				g_assert (GPOINTER_TO_INT (args [0]->inst_p1) == MONO_PATCH_INFO_LDSTR);
				ji = (MonoJumpInfoToken *)args [0]->inst_p0;
			}

			NULLIFY_INS (args [0]);

			s = mono_ldstr_utf8 (ji->image, mono_metadata_token_index (ji->token), &cfg->error);
			return_val_if_nok (&cfg->error, NULL);

			MONO_INST_NEW (cfg, ins, OP_OBJC_GET_SELECTOR);
			ins->dreg = mono_alloc_ireg (cfg);
			// FIXME: Leaks
			ins->inst_p0 = s;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		}
	} else if (in_corlib &&
			(strcmp (cmethod_klass_name_space, "System.Runtime.InteropServices") == 0) &&
			(strcmp (cmethod_klass_name, "Marshal") == 0)) {
		//Convert Marshal.PtrToStructure<T> of blittable T to direct loads
		if (strcmp (cmethod->name, "PtrToStructure") == 0 &&
				cmethod->is_inflated &&
				fsig->param_count == 1 &&
				!mini_method_check_context_used (cfg, cmethod)) {

			MonoGenericContext *method_context = mono_method_get_context (cmethod);
			MonoType *arg0 = method_context->method_inst->type_argv [0];
			if (mono_type_is_native_blittable (arg0))
				return mini_emit_memory_load (cfg, arg0, args [0], 0, 0);
		}
	} else if (cmethod->klass == mono_defaults.enum_class && !strcmp (cmethod->name, "HasFlag") &&
			   args [0]->opcode == OP_BOX && args [1]->opcode == OP_BOX_ICONST && args [0]->klass == args [1]->klass) {
		args [1]->opcode = OP_ICONST;
		ins = mini_handle_enum_has_flag (cfg, args [0]->klass, NULL, args [0]->sreg1, args [1]);
		NULLIFY_INS (args [0]);
		return ins;
	} else if (in_corlib &&
			   !strcmp (cmethod_klass_name_space, "System") &&
			   (!strcmp (cmethod_klass_name, "Span`1") || !strcmp (cmethod_klass_name, "ReadOnlySpan`1"))) {
		return emit_span_intrinsics (cfg, cmethod, fsig, args);
	} else if (in_corlib &&
			   !strcmp (cmethod_klass_name_space, "Internal.Runtime.CompilerServices") &&
			   !strcmp (cmethod_klass_name, "Unsafe")) {
		return emit_unsafe_intrinsics (cfg, cmethod, fsig, args);
	} else if (in_corlib &&
			   !strcmp (cmethod_klass_name_space, "System.Runtime.CompilerServices") &&
			   !strcmp (cmethod_klass_name, "Unsafe")) {
		return emit_unsafe_intrinsics (cfg, cmethod, fsig, args);
	} else if (in_corlib &&
			   !strcmp (cmethod_klass_name_space, "System.Runtime.CompilerServices") &&
			   !strcmp (cmethod_klass_name, "JitHelpers")) {
		return emit_jit_helpers_intrinsics (cfg, cmethod, fsig, args);
	}

#ifdef MONO_ARCH_SIMD_INTRINSICS
	if (cfg->opt & MONO_OPT_SIMD) {
		ins = mono_emit_simd_intrinsics (cfg, cmethod, fsig, args);
		if (ins)
			return ins;
	}
#endif

	/* Fallback if SIMD is disabled */
	if (in_corlib && !strcmp ("System.Numerics", cmethod_klass_name_space) && !strcmp ("Vector", cmethod_klass_name)) {
		if (!strcmp (cmethod->name, "get_IsHardwareAccelerated")) {
			EMIT_NEW_ICONST (cfg, ins, 0);
			ins->type = STACK_I4;
			return ins;
		}
	}

	ins = mono_emit_native_types_intrinsics (cfg, cmethod, fsig, args);
	if (ins)
		return ins;

	if (COMPILE_LLVM (cfg)) {
		ins = llvm_emit_inst_for_method (cfg, cmethod, fsig, args, in_corlib);
		if (ins)
			return ins;
	}

	return mono_arch_emit_inst_for_method (cfg, cmethod, fsig, args);
}


static MonoInst*
emit_array_unsafe_access (MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args, int is_set)
{
	MonoClass *eklass;
	
	if (is_set)
		eklass = mono_class_from_mono_type_internal (fsig->params [2]);
	else
		eklass = mono_class_from_mono_type_internal (fsig->ret);

	if (is_set) {
		return mini_emit_array_store (cfg, eklass, args, FALSE);
	} else {
		MonoInst *ins, *addr = mini_emit_ldelema_1_ins (cfg, eklass, args [0], args [1], FALSE);
		EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, m_class_get_byval_arg (eklass), addr->dreg, 0);
		return ins;
	}
}

static gboolean
is_unsafe_mov_compatible (MonoCompile *cfg, MonoClass *param_klass, MonoClass *return_klass)
{
	uint32_t align;
	int param_size, return_size;

	param_klass = mono_class_from_mono_type_internal (mini_get_underlying_type (m_class_get_byval_arg (param_klass)));
	return_klass = mono_class_from_mono_type_internal (mini_get_underlying_type (m_class_get_byval_arg (return_klass)));

	if (cfg->verbose_level > 3)
		printf ("[UNSAFE-MOV-INTRISIC] %s <- %s\n", m_class_get_name (return_klass), m_class_get_name (param_klass));

	//Don't allow mixing reference types with value types
	if (m_class_is_valuetype (param_klass) != m_class_is_valuetype (return_klass)) {
		if (cfg->verbose_level > 3)
			printf ("[UNSAFE-MOV-INTRISIC]\tone of the args is a valuetype and the other is not\n");
		return FALSE;
	}

	if (!m_class_is_valuetype (param_klass)) {
		if (cfg->verbose_level > 3)
			printf ("[UNSAFE-MOV-INTRISIC]\targs are reference types\n");
		return TRUE;
	}

	//That are blitable
	if (m_class_has_references (param_klass) || m_class_has_references (return_klass))
		return FALSE;

	MonoType *param_type = m_class_get_byval_arg (param_klass);
	MonoType *return_type = m_class_get_byval_arg (return_klass);

	/* Avoid mixing structs and primitive types/enums, they need to be handled differently in the JIT */
	if ((MONO_TYPE_ISSTRUCT (param_type) && !MONO_TYPE_ISSTRUCT (return_type)) ||
		(!MONO_TYPE_ISSTRUCT (param_type) && MONO_TYPE_ISSTRUCT (return_type))) {
			if (cfg->verbose_level > 3)
				printf ("[UNSAFE-MOV-INTRISIC]\tmixing structs and scalars\n");
		return FALSE;
	}

	if (param_type->type == MONO_TYPE_R4 || param_type->type == MONO_TYPE_R8 ||
		return_type->type == MONO_TYPE_R4 || return_type->type == MONO_TYPE_R8) {
		if (cfg->verbose_level > 3)
			printf ("[UNSAFE-MOV-INTRISIC]\tfloat or double are not supported\n");
		return FALSE;
	}

	param_size = mono_class_value_size (param_klass, &align);
	return_size = mono_class_value_size (return_klass, &align);

	//We can do it if sizes match
	if (param_size == return_size) {
		if (cfg->verbose_level > 3)
			printf ("[UNSAFE-MOV-INTRISIC]\tsame size\n");
		return TRUE;
	}

	//No simple way to handle struct if sizes don't match
	if (MONO_TYPE_ISSTRUCT (param_type)) {
		if (cfg->verbose_level > 3)
			printf ("[UNSAFE-MOV-INTRISIC]\tsize mismatch and type is a struct\n");
		return FALSE;
	}

	/*
	 * Same reg size category.
	 * A quick note on why we don't require widening here.
	 * The intrinsic is "R Array.UnsafeMov<S,R> (S s)".
	 *
	 * Since the source value comes from a function argument, the JIT will already have
	 * the value in a VREG and performed any widening needed before (say, when loading from a field).
	 */
	if (param_size <= 4 && return_size <= 4) {
		if (cfg->verbose_level > 3)
			printf ("[UNSAFE-MOV-INTRISIC]\tsize mismatch but both are of the same reg class\n");
		return TRUE;
	}

	return FALSE;
}

static MonoInst*
emit_array_unsafe_mov (MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoClass *param_klass = mono_class_from_mono_type_internal (fsig->params [0]);
	MonoClass *return_klass = mono_class_from_mono_type_internal (fsig->ret);

	if (mini_is_gsharedvt_variable_type (fsig->ret))
		return NULL;

	//Valuetypes that are semantically equivalent or numbers than can be widened to
	if (is_unsafe_mov_compatible (cfg, param_klass, return_klass))
		return args [0];

	//Arrays of valuetypes that are semantically equivalent
	if (m_class_get_rank (param_klass) == 1 && m_class_get_rank (return_klass) == 1 && is_unsafe_mov_compatible (cfg, m_class_get_element_class (param_klass), m_class_get_element_class (return_klass)))
		return args [0];

	return NULL;
}

MonoInst*
mini_emit_inst_for_sharable_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (cmethod->klass == mono_defaults.array_class) {
		if (strcmp (cmethod->name, "UnsafeStore") == 0)
			return emit_array_unsafe_access (cfg, fsig, args, TRUE);
		else if (strcmp (cmethod->name, "UnsafeLoad") == 0)
			return emit_array_unsafe_access (cfg, fsig, args, FALSE);
		else if (strcmp (cmethod->name, "UnsafeMov") == 0)
			return emit_array_unsafe_mov (cfg, fsig, args);
	}

	return NULL;
}

MonoInst*
mini_emit_inst_for_field_load (MonoCompile *cfg, MonoClassField *field)
{
	MonoClass *klass = field->parent;
	const char *klass_name_space = m_class_get_name_space (klass);
	const char *klass_name = m_class_get_name (klass);
	MonoImage *klass_image = m_class_get_image (klass);
	gboolean in_corlib = klass_image == mono_defaults.corlib;
	gboolean is_le;
	MonoInst *ins;

	if (in_corlib && !strcmp (klass_name_space, "System") && !strcmp (klass_name, "BitConverter") && !strcmp (field->name, "IsLittleEndian")) {
		is_le = (TARGET_BYTE_ORDER == G_LITTLE_ENDIAN);
		EMIT_NEW_ICONST (cfg, ins, is_le);
		return ins;
	}
	return NULL;
}
#else
MONO_EMPTY_SOURCE_FILE (intrinsics);
#endif
