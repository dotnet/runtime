/**
 * \file
 * intrinsics for variable sized int/floats
 *
 * Author:
 *   Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2013 Xamarin
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <stdio.h>

#include "mini.h"
#include "ir-emit.h"
#include "glib.h"


typedef struct {
	const char *op_name;
	short op_table[4];
} IntIntrisic;

typedef struct {
	short op_index;
	short big_stack_type;
	short small_stack_type;
	short stack_type;
	short conv_4_to_8;
	short conv_8_to_4;
	short move;
	short inc_op;
	short dec_op;
	short store_op;
	short compare_op;
} MagicTypeInfo;


#if SIZEOF_VOID_P == 8
#define OP_PT_ADD OP_LADD
#define OP_PT_SUB OP_LSUB
#define OP_PT_MUL OP_LMUL
#define OP_PT_DIV OP_LDIV
#define OP_PT_REM OP_LREM
#define OP_PT_NEG OP_LNEG
#define OP_PT_AND OP_LAND
#define OP_PT_OR OP_LOR
#define OP_PT_XOR OP_LXOR
#define OP_PT_NOT OP_LNOT
#define OP_PT_SHL OP_LSHL
#define OP_PT_SHR OP_LSHR

#define OP_PT_DIV_UN OP_LDIV_UN
#define OP_PT_REM_UN OP_LREM_UN
#define OP_PT_SHR_UN OP_LSHR_UN

#define OP_PT_ADD_IMM OP_LADD_IMM
#define OP_PT_SUB_IMM OP_LSUB_IMM

#define OP_PT_STORE_FP_MEMBASE_REG OP_STORER8_MEMBASE_REG

#define OP_PCOMPARE OP_LCOMPARE

#else
#define OP_PT_ADD OP_IADD
#define OP_PT_SUB OP_ISUB
#define OP_PT_MUL OP_IMUL
#define OP_PT_DIV OP_IDIV
#define OP_PT_REM OP_IREM
#define OP_PT_NEG OP_INEG
#define OP_PT_AND OP_IAND
#define OP_PT_OR OP_IOR
#define OP_PT_XOR OP_IXOR
#define OP_PT_NOT OP_INOT
#define OP_PT_SHL OP_ISHL
#define OP_PT_SHR OP_ISHR

#define OP_PT_DIV_UN OP_IDIV_UN
#define OP_PT_REM_UN OP_IREM_UN
#define OP_PT_SHR_UN OP_ISHR_UN

#define OP_PT_ADD_IMM OP_IADD_IMM
#define OP_PT_SUB_IMM OP_ISUB_IMM

#define OP_PT_STORE_FP_MEMBASE_REG OP_STORER4_MEMBASE_REG

#define OP_PCOMPARE OP_ICOMPARE

#endif

static const IntIntrisic int_binop[] = {
	{ "op_Addition", { OP_PT_ADD, OP_PT_ADD, OP_FADD, OP_RADD } },
	{ "op_Subtraction", { OP_PT_SUB, OP_PT_SUB, OP_FSUB, OP_RSUB } },
	{ "op_Multiply", { OP_PT_MUL, OP_PT_MUL, OP_FMUL, OP_RMUL } },
	{ "op_Division", { OP_PT_DIV, OP_PT_DIV_UN, OP_FDIV, OP_RDIV } },
	{ "op_Modulus", { OP_PT_REM, OP_PT_REM_UN, OP_FREM, OP_RREM } },
	{ "op_BitwiseAnd", { OP_PT_AND, OP_PT_AND } },
	{ "op_BitwiseOr", { OP_PT_OR, OP_PT_OR } },
	{ "op_ExclusiveOr", { OP_PT_XOR, OP_PT_XOR } },
	{ "op_LeftShift", { OP_PT_SHL, OP_PT_SHL } },
	{ "op_RightShift", { OP_PT_SHR, OP_PT_SHR_UN } },
};

static const IntIntrisic int_unnop[] = {
	{ "op_UnaryPlus", { OP_MOVE, OP_MOVE, OP_FMOVE, OP_RMOVE } },
	{ "op_UnaryNegation", { OP_PT_NEG, OP_PT_NEG, OP_FNEG, OP_RNEG } },
	{ "op_OnesComplement", { OP_PT_NOT, OP_PT_NOT, OP_FNOT, OP_RNOT } },
};

static const IntIntrisic int_cmpop[] = {
	{ "op_Inequality", { OP_ICNEQ, OP_ICNEQ, OP_FCNEQ, OP_RCNEQ } },
	{ "op_Equality", { OP_ICEQ, OP_ICEQ, OP_FCEQ, OP_RCEQ } },
	{ "op_GreaterThan", { OP_ICGT, OP_ICGT_UN, OP_FCGT, OP_RCGT } },
	{ "op_GreaterThanOrEqual", { OP_ICGE, OP_ICGE_UN, OP_FCGE, OP_RCGE } },
	{ "op_LessThan", { OP_ICLT, OP_ICLT_UN, OP_FCLT, OP_RCLT } },
	{ "op_LessThanOrEqual", { OP_ICLE, OP_ICLE_UN, OP_FCLE, OP_RCLE } },
};

static const MagicTypeInfo type_info[] = {
	//nint
	{ 0, STACK_I8, STACK_I4, STACK_PTR, OP_ICONV_TO_I8, OP_LCONV_TO_I4, OP_MOVE, OP_PT_ADD_IMM, OP_PT_SUB_IMM, OP_STORE_MEMBASE_REG, OP_PCOMPARE },
	//nuint
	{ 1, STACK_I8, STACK_I4, STACK_PTR, OP_ICONV_TO_U8, OP_LCONV_TO_U4, OP_MOVE, OP_PT_ADD_IMM, OP_PT_SUB_IMM, OP_STORE_MEMBASE_REG, OP_PCOMPARE },
	//nfloat
	{ 2, STACK_R8, STACK_R8, STACK_R8, OP_FCONV_TO_R8, OP_FCONV_TO_R4, OP_FMOVE, 0, 0, OP_PT_STORE_FP_MEMBASE_REG, 0 },
};


static inline gboolean
type_size (MonoCompile *cfg, MonoType *type)
{
	if (type->type == MONO_TYPE_I4 || type->type == MONO_TYPE_U4)
		return 4;
	else if (type->type == MONO_TYPE_I8 || type->type == MONO_TYPE_U8)
		return 8;
	else if (type->type == MONO_TYPE_R4 && !type->byref && cfg->r4fp)
		return 4;
	else if (type->type == MONO_TYPE_R8 && !type->byref)
		return 8;
	return SIZEOF_VOID_P;
}

#ifndef DISABLE_JIT

static gboolean is_int_type (MonoType *t);
static gboolean is_float_type (MonoType *t);

static MonoInst*
emit_narrow (MonoCompile *cfg, const MagicTypeInfo *info, int sreg)
{
	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, info->conv_8_to_4);
	ins->sreg1 = sreg;
	if (info->conv_8_to_4 == OP_FCONV_TO_R4)
		ins->type = cfg->r4_stack_type;
	else
		ins->type = info->small_stack_type;
	ins->dreg = alloc_dreg (cfg, ins->type);
	MONO_ADD_INS (cfg->cbb, ins);
	return mono_decompose_opcode (cfg, ins);
}

static MonoInst*
emit_widen (MonoCompile *cfg, const MagicTypeInfo *info, int sreg)
{
	MonoInst *ins;

	if (cfg->r4fp && info->conv_4_to_8 == OP_FCONV_TO_R8)
		MONO_INST_NEW (cfg, ins, OP_RCONV_TO_R8);
	else
		MONO_INST_NEW (cfg, ins, info->conv_4_to_8);
	ins->sreg1 = sreg;
	ins->type = info->big_stack_type;
	ins->dreg = alloc_dreg (cfg, info->big_stack_type); 
	MONO_ADD_INS (cfg->cbb, ins);
	return mono_decompose_opcode (cfg, ins);
}

static MonoInst*
emit_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args, const MagicTypeInfo *info)
{
	int i = 0;
	const char *name = cmethod->name;
	MonoInst *ins;
	int type_index, stack_type;

	if (info->op_index == 2 && cfg->r4fp && SIZEOF_VOID_P == 4) {
		type_index = 3;
		stack_type = STACK_R4;
	} else {
		type_index = info->op_index;
		stack_type = info->stack_type;
	}

	if (!strcmp ("op_Implicit", name) || !strcmp ("op_Explicit", name)) {
		int source_size = type_size (cfg, fsig->params [0]);
		int dest_size = type_size (cfg, fsig->ret);

		switch (info->big_stack_type) {
		case STACK_I8:
			if (!is_int_type (fsig->params [0]) || !is_int_type (fsig->ret))
				return NULL;
			break;
		case STACK_R8:
			if (!is_float_type (fsig->params [0]) || !is_float_type (fsig->ret))
				return NULL;
			break;
		default:
			g_assert_not_reached ();
		}

		//4 -> 4 or 8 -> 8
		if (source_size == dest_size)
			return args [0];

		//4 -> 8
		if (source_size < dest_size)
			return emit_widen (cfg, info, args [0]->dreg);

		//8 -> 4
		return emit_narrow (cfg, info, args [0]->dreg);
	}

	if (!strcmp (".ctor", name)) {
		gboolean is_ldaddr = args [0]->opcode == OP_LDADDR;
		int arg0 = args [1]->dreg;
		int arg_size = type_size (cfg, fsig->params [0]);

		if (arg_size > SIZEOF_VOID_P) //8 -> 4
			arg0 = emit_narrow (cfg, info, arg0)->dreg;
		else if (arg_size < SIZEOF_VOID_P) //4 -> 8
			arg0 = emit_widen (cfg, info, arg0)->dreg;

		if (is_ldaddr) { /*Eliminate LDADDR if it's initing a local var*/
			int dreg = ((MonoInst*)args [0]->inst_p0)->dreg;
			NULLIFY_INS (args [0]);
			EMIT_NEW_UNALU (cfg, ins, info->move, dreg, arg0);
			cfg->has_indirection = TRUE;
		} else {
			EMIT_NEW_STORE_MEMBASE (cfg, ins, info->store_op, args [0]->dreg, 0, arg0);
		}
		return ins;
	}

	if (!strcmp ("op_Increment", name) || !strcmp ("op_Decrement", name)) {
		gboolean inc = !strcmp ("op_Increment", name);
		/* FIXME float inc is too complex to bother with*/
		//this is broken with ints too
		// if (!info->inc_op)
			return NULL;

		/* We have IR for inc/dec */
		MONO_INST_NEW (cfg, ins, inc ? info->inc_op : info->dec_op);
		ins->dreg = alloc_dreg (cfg, info->stack_type);
		ins->sreg1 = args [0]->dreg;
		ins->inst_imm = 1;
		ins->type = info->stack_type;
		MONO_ADD_INS (cfg->cbb, ins);
		return ins;
	}

	for (i = 0; i < sizeof (int_binop) / sizeof  (IntIntrisic); ++i) {
		if (!strcmp (int_binop [i].op_name, name)) {
			if (!int_binop [i].op_table [info->op_index])
				return NULL;
			g_assert (int_binop [i].op_table [type_index]);

			MONO_INST_NEW (cfg, ins, int_binop [i].op_table [type_index]);
			ins->dreg = alloc_dreg (cfg, stack_type);
			ins->sreg1 = args [0]->dreg;
	        ins->sreg2 = args [1]->dreg;
			ins->type = stack_type;
			MONO_ADD_INS (cfg->cbb, ins);
			return mono_decompose_opcode (cfg, ins);
		}
	}

	for (i = 0; i < sizeof (int_unnop) / sizeof  (IntIntrisic); ++i) {
		if (!strcmp (int_unnop [i].op_name, name)) {
			g_assert (int_unnop [i].op_table [type_index]);

			MONO_INST_NEW (cfg, ins, int_unnop [i].op_table [type_index]);
			ins->dreg = alloc_dreg (cfg, stack_type);
			ins->sreg1 = args [0]->dreg;
			ins->type = stack_type;
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		}
	}

	for (i = 0; i < sizeof (int_cmpop) / sizeof  (IntIntrisic); ++i) {
		if (!strcmp (int_cmpop [i].op_name, name)) {
			g_assert (int_cmpop [i].op_table [type_index]);

			if (info->compare_op) {
				MONO_INST_NEW (cfg, ins, info->compare_op);
		        ins->dreg = -1;
				ins->sreg1 = args [0]->dreg;
		        ins->sreg2 = args [1]->dreg;
				MONO_ADD_INS (cfg->cbb, ins);

				MONO_INST_NEW (cfg, ins, int_cmpop [i].op_table [type_index]);
		        ins->dreg = alloc_preg (cfg);
				ins->type = STACK_I4;
				MONO_ADD_INS (cfg->cbb, ins);
			} else {
				MONO_INST_NEW (cfg, ins, int_cmpop [i].op_table [type_index]);
				ins->dreg = alloc_ireg (cfg);
				ins->sreg1 = args [0]->dreg;
		        ins->sreg2 = args [1]->dreg;
				MONO_ADD_INS (cfg->cbb, ins);
			}

			return ins;
		}
	}

	return NULL;
}


MonoInst*
mono_emit_native_types_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (mono_class_is_magic_int (cmethod->klass)) {
		const char *class_name = cmethod->klass->name;
		if (!strcmp ("nint", class_name))
			return emit_intrinsics (cfg, cmethod, fsig, args, &type_info [0]);
		else
			return emit_intrinsics (cfg, cmethod, fsig, args, &type_info [1]);
	} else if (mono_class_is_magic_float (cmethod->klass))
		return emit_intrinsics (cfg, cmethod, fsig, args, &type_info [2]);

	return NULL;
}

#endif /* !DISABLE_JIT */

static inline gboolean
mono_class_is_magic_assembly (MonoClass *klass)
{
	if (!klass->image->assembly_name)
		return FALSE;
	if (!strcmp ("Xamarin.iOS", klass->image->assembly_name))
		return TRUE;
	if (!strcmp ("Xamarin.Mac", klass->image->assembly_name))
		return TRUE;
	if (!strcmp ("Xamarin.WatchOS", klass->image->assembly_name))
		return TRUE;
	/* regression test suite */
	if (!strcmp ("builtin-types", klass->image->assembly_name))
		return TRUE;
	if (!strcmp ("mini_tests", klass->image->assembly_name))
		return TRUE;
	return FALSE;
}

gboolean
mono_class_is_magic_int (MonoClass *klass)
{
	static MonoClass *magic_nint_class;
	static MonoClass *magic_nuint_class;

	if (klass == magic_nint_class)
		return TRUE;

	if (klass == magic_nuint_class)
		return TRUE;

	if (magic_nint_class && magic_nuint_class)
		return FALSE;

	if (!mono_class_is_magic_assembly (klass))
		return FALSE;

	if (strcmp ("System", klass->name_space) != 0)
		return FALSE;

	if (strcmp ("nint", klass->name) == 0) {
		magic_nint_class = klass;
		return TRUE;
	}

	if (strcmp ("nuint", klass->name) == 0){
		magic_nuint_class = klass;
		return TRUE;
	}
	return FALSE;
}

gboolean
mono_class_is_magic_float (MonoClass *klass)
{
	static MonoClass *magic_nfloat_class;

	if (klass == magic_nfloat_class)
		return TRUE;

	if (magic_nfloat_class)
		return FALSE;

	if (!mono_class_is_magic_assembly (klass))
		return FALSE;

	if (strcmp ("System", klass->name_space) != 0)
		return FALSE;

	if (strcmp ("nfloat", klass->name) == 0) {
		magic_nfloat_class = klass;

		/* Assert that we are using the matching assembly */
		MonoClassField *value_field = mono_class_get_field_from_name (klass, "v");
		g_assert (value_field);
		MonoType *t = mono_field_get_type (value_field);
		MonoType *native = mini_native_type_replace_type (&klass->byval_arg);
		if (t->type != native->type)
			g_error ("Assembly used for native types '%s' doesn't match this runtime, %s is mapped to %s, expecting %s.\n", klass->image->name, klass->name, mono_type_full_name (t), mono_type_full_name (native));
		return TRUE;
	}
	return FALSE;
}

static gboolean
is_int_type (MonoType *t)
{
	if (t->type != MONO_TYPE_I4 && t->type != MONO_TYPE_I8 && t->type != MONO_TYPE_U4 && t->type != MONO_TYPE_U8 && !mono_class_is_magic_int (mono_class_from_mono_type (t)))
		return FALSE;
	return TRUE;
}

static gboolean
is_float_type (MonoType *t)
{
	if (t->type != MONO_TYPE_R4 && t->type != MONO_TYPE_R8 && !mono_class_is_magic_float (mono_class_from_mono_type (t)))
		return FALSE;
	return TRUE;
}

MonoType*
mini_native_type_replace_type (MonoType *type)
{
	MonoClass *klass;

	if (type->type != MONO_TYPE_VALUETYPE)
		return type;
	klass = type->data.klass;

	if (mono_class_is_magic_int (klass))
		return type->byref ? &mono_defaults.int_class->this_arg : &mono_defaults.int_class->byval_arg;
	if (mono_class_is_magic_float (klass))
#if SIZEOF_VOID_P == 8
		return type->byref ? &mono_defaults.double_class->this_arg : &mono_defaults.double_class->byval_arg;
#else
		return type->byref ? &mono_defaults.single_class->this_arg : &mono_defaults.single_class->byval_arg;
#endif
	return type;
}
