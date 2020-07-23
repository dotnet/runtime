/**
 * \file
 * transform CIL into different opcodes for more
 * efficient interpretation
 *
 * Written by Bernie Solomon (bernard@ugsolutions.com)
 * Copyright (c) 2004.
 */

#include "config.h"
#include <string.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/mono-basic-block.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/utils/unlocked.h>
#include <mono/utils/mono-memory-model.h>

#include <mono/mini/mini.h>
#include <mono/mini/mini-runtime.h>

#include "mintops.h"
#include "interp-internals.h"
#include "interp.h"
#include "transform.h"

MonoInterpStats mono_interp_stats;

#define DEBUG 0

#if SIZEOF_VOID_P == 8
#define MINT_NEG_P MINT_NEG_I8
#define MINT_NOT_P MINT_NOT_I8

#define MINT_NEG_FP MINT_NEG_R8

#define MINT_ADD_P MINT_ADD_I8
#define MINT_SUB_P MINT_SUB_I8
#define MINT_MUL_P MINT_MUL_I8
#define MINT_DIV_P MINT_DIV_I8
#define MINT_DIV_UN_P MINT_DIV_UN_I8
#define MINT_REM_P MINT_REM_I8
#define MINT_REM_UN_P MINT_REM_UN_I8
#define MINT_AND_P MINT_AND_I8
#define MINT_OR_P MINT_OR_I8
#define MINT_XOR_P MINT_XOR_I8
#define MINT_SHL_P MINT_SHL_I8
#define MINT_SHR_P MINT_SHR_I8
#define MINT_SHR_UN_P MINT_SHR_UN_I8

#define MINT_CEQ_P MINT_CEQ_I8
#define MINT_CNE_P MINT_CNE_I8
#define MINT_CLT_P MINT_CLT_I8
#define MINT_CLT_UN_P MINT_CLT_UN_I8
#define MINT_CGT_P MINT_CGT_I8
#define MINT_CGT_UN_P MINT_CGT_UN_I8
#define MINT_CLE_P MINT_CLE_I8
#define MINT_CLE_UN_P MINT_CLE_UN_I8
#define MINT_CGE_P MINT_CGE_I8
#define MINT_CGE_UN_P MINT_CGE_UN_I8

#define MINT_ADD_FP MINT_ADD_R8
#define MINT_SUB_FP MINT_SUB_R8
#define MINT_MUL_FP MINT_MUL_R8
#define MINT_DIV_FP MINT_DIV_R8
#define MINT_REM_FP MINT_REM_R8

#define MINT_CNE_FP MINT_CNE_R8
#define MINT_CEQ_FP MINT_CEQ_R8
#define MINT_CGT_FP MINT_CGT_R8
#define MINT_CGE_FP MINT_CGE_R8
#define MINT_CLT_FP MINT_CLT_R8
#define MINT_CLE_FP MINT_CLE_R8

#define MINT_CONV_OVF_U4_P MINT_CONV_OVF_U4_I8
#else

#define MINT_NEG_P MINT_NEG_I4
#define MINT_NOT_P MINT_NOT_I4

#define MINT_NEG_FP MINT_NEG_R4

#define MINT_ADD_P MINT_ADD_I4
#define MINT_SUB_P MINT_SUB_I4
#define MINT_MUL_P MINT_MUL_I4
#define MINT_DIV_P MINT_DIV_I4
#define MINT_DIV_UN_P MINT_DIV_UN_I4
#define MINT_REM_P MINT_REM_I4
#define MINT_REM_UN_P MINT_REM_UN_I4
#define MINT_AND_P MINT_AND_I4
#define MINT_OR_P MINT_OR_I4
#define MINT_XOR_P MINT_XOR_I4
#define MINT_SHL_P MINT_SHL_I4
#define MINT_SHR_P MINT_SHR_I4
#define MINT_SHR_UN_P MINT_SHR_UN_I4

#define MINT_CEQ_P MINT_CEQ_I4
#define MINT_CNE_P MINT_CNE_I4
#define MINT_CLT_P MINT_CLT_I4
#define MINT_CLT_UN_P MINT_CLT_UN_I4
#define MINT_CGT_P MINT_CGT_I4
#define MINT_CGT_UN_P MINT_CGT_UN_I4
#define MINT_CLE_P MINT_CLE_I4
#define MINT_CLE_UN_P MINT_CLE_UN_I4
#define MINT_CGE_P MINT_CGE_I4
#define MINT_CGE_UN_P MINT_CGE_UN_I4

#define MINT_ADD_FP MINT_ADD_R4
#define MINT_SUB_FP MINT_SUB_R4
#define MINT_MUL_FP MINT_MUL_R4
#define MINT_DIV_FP MINT_DIV_R4
#define MINT_REM_FP MINT_REM_R4

#define MINT_CNE_FP MINT_CNE_R4
#define MINT_CEQ_FP MINT_CEQ_R4
#define MINT_CGT_FP MINT_CGT_R4
#define MINT_CGE_FP MINT_CGE_R4
#define MINT_CLT_FP MINT_CLT_R4
#define MINT_CLE_FP MINT_CLE_R4

#define MINT_CONV_OVF_U4_P MINT_CONV_OVF_U4_I4
#endif

typedef struct {
	const gchar *op_name;
	guint16 insn [3];
} MagicIntrinsic;

// static const MagicIntrinsic int_binop[] = {

static const MagicIntrinsic int_unnop[] = {
	{ "op_UnaryPlus", {MINT_NOP, MINT_NOP, MINT_NOP}},
	{ "op_UnaryNegation", {MINT_NEG_P, MINT_NEG_P, MINT_NEG_FP}},
	{ "op_OnesComplement", {MINT_NOT_P, MINT_NOT_P, MINT_NIY}}
};

static const MagicIntrinsic int_binop[] = {
	{ "op_Addition", {MINT_ADD_P, MINT_ADD_P, MINT_ADD_FP}},
	{ "op_Subtraction", {MINT_SUB_P, MINT_SUB_P, MINT_SUB_FP}},
	{ "op_Multiply", {MINT_MUL_P, MINT_MUL_P, MINT_MUL_FP}},
	{ "op_Division", {MINT_DIV_P, MINT_DIV_UN_P, MINT_DIV_FP}},
	{ "op_Modulus", {MINT_REM_P, MINT_REM_UN_P, MINT_REM_FP}},
	{ "op_BitwiseAnd", {MINT_AND_P, MINT_AND_P, MINT_NIY}},
	{ "op_BitwiseOr", {MINT_OR_P, MINT_OR_P, MINT_NIY}},
	{ "op_ExclusiveOr", {MINT_XOR_P, MINT_XOR_P, MINT_NIY}},
	{ "op_LeftShift", {MINT_SHL_P, MINT_SHL_P, MINT_NIY}},
	{ "op_RightShift", {MINT_SHR_P, MINT_SHR_UN_P, MINT_NIY}},
};

static const MagicIntrinsic int_cmpop[] = {
	{ "op_Inequality", {MINT_CNE_P, MINT_CNE_P, MINT_CNE_FP}},
	{ "op_Equality", {MINT_CEQ_P, MINT_CEQ_P, MINT_CEQ_FP}},
	{ "op_GreaterThan", {MINT_CGT_P, MINT_CGT_UN_P, MINT_CGT_FP}},
	{ "op_GreaterThanOrEqual", {MINT_CGE_P, MINT_CGE_UN_P, MINT_CGE_FP}},
	{ "op_LessThan", {MINT_CLT_P, MINT_CLT_UN_P, MINT_CLT_FP}},
	{ "op_LessThanOrEqual", {MINT_CLE_P, MINT_CLE_UN_P, MINT_CLE_FP}}
};

static const char *stack_type_string [] = { "I4", "I8", "R4", "R8", "O ", "VT", "MP", "F " };

static int stack_type [] = {
	STACK_TYPE_I4, /*I1*/
	STACK_TYPE_I4, /*U1*/
	STACK_TYPE_I4, /*I2*/
	STACK_TYPE_I4, /*U2*/
	STACK_TYPE_I4, /*I4*/
	STACK_TYPE_I8, /*I8*/
	STACK_TYPE_R4, /*R4*/
	STACK_TYPE_R8, /*R8*/
	STACK_TYPE_O,  /*O*/
	STACK_TYPE_VT
};

static gboolean generate_code (TransformData *td, MonoMethod *method, MonoMethodHeader *header, MonoGenericContext *generic_context, MonoError *error);

static InterpInst*
interp_new_ins (TransformData *td, guint16 opcode, int len)
{
	InterpInst *new_inst;
	// Size of data region of instruction is length of instruction minus 1 (the opcode slot)
	new_inst = mono_mempool_alloc0 (td->mempool, sizeof (InterpInst) + sizeof (guint16) * ((len > 0) ? (len - 1) : 0));
	new_inst->opcode = opcode;
	new_inst->il_offset = td->current_il_offset;
	return new_inst;
}

// This version need to be used with switch opcode, which doesn't have constant length
static InterpInst*
interp_add_ins_explicit (TransformData *td, guint16 opcode, int len)
{
	InterpInst *new_inst = interp_new_ins (td, opcode, len);
	new_inst->prev = td->last_ins;
	if (td->last_ins)
		td->last_ins->next = new_inst;
	else
		td->first_ins = new_inst;
	td->last_ins = new_inst;
	return new_inst;
}

static InterpInst*
interp_add_ins (TransformData *td, guint16 opcode)
{
	return interp_add_ins_explicit (td, opcode, mono_interp_oplen [opcode]);
}

// This instruction will have the il_offset of the previous instruction
static InterpInst*
interp_insert_ins (TransformData *td, InterpInst *prev_ins, guint16 opcode)
{
	InterpInst *new_inst = interp_new_ins (td, opcode, mono_interp_oplen [opcode]);
	g_assert (prev_ins);
	new_inst->il_offset = prev_ins->il_offset;

	new_inst->prev = prev_ins;
	new_inst->next = prev_ins->next;
	prev_ins->next = new_inst;

	if (new_inst->next == NULL)
		td->last_ins = new_inst;
	else
		new_inst->next->prev = new_inst;

	return new_inst;
}

static void
interp_clear_ins (TransformData *td, InterpInst *ins)
{
	// Clearing instead of removing from the list makes everything easier.
	// We don't change structure of the instruction list, we don't need
	// to worry about updating the il_offset, or whether this instruction
	// was at the start of a basic block etc.
	ins->opcode = MINT_NOP;
}

static InterpInst*
interp_prev_ins (InterpInst *ins)
{
	ins = ins->prev;
	while (ins && ins->opcode == MINT_NOP)
		ins = ins->prev;
	return ins;
}

#define CHECK_STACK(td, n) \
	do { \
		int stack_size = (td)->sp - (td)->stack; \
		if (stack_size < (n)) \
			g_warning ("%s.%s: not enough values (%d < %d) on stack at %04x", \
				m_class_get_name ((td)->method->klass), (td)->method->name, \
				stack_size, n, (td)->ip - (td)->il_code); \
	} while (0)

#define ENSURE_I4(td, sp_off) \
	do { \
		if ((td)->sp [-sp_off].type == STACK_TYPE_I8) \
			interp_add_ins (td, sp_off == 1 ? MINT_CONV_I4_I8 : MINT_CONV_I4_I8_SP); \
	} while (0)

#define CHECK_TYPELOAD(klass) \
	do { \
		if (!(klass) || mono_class_has_failure (klass)) { \
			mono_error_set_for_class_failure (error, klass); \
			goto exit; \
		} \
	} while (0)

#if NO_UNALIGNED_ACCESS
#define WRITE32(ip, v) \
	do { \
		* (ip) = * (guint16 *)(v); \
		* ((ip) + 1) = * ((guint16 *)(v) + 1); \
		(ip) += 2; \
	} while (0)

#define WRITE32_INS(ins, index, v) \
	do { \
		(ins)->data [index] = * (guint16 *)(v); \
		(ins)->data [index + 1] = * ((guint16 *)(v) + 1); \
	} while (0)

#define WRITE64(ins, v) \
	do { \
		*((ins) + 0) = * ((guint16 *)(v) + 0); \
		*((ins) + 1) = * ((guint16 *)(v) + 1); \
		*((ins) + 2) = * ((guint16 *)(v) + 2); \
		*((ins) + 3) = * ((guint16 *)(v) + 3); \
	} while (0)

#define WRITE64_INS(ins, index, v) \
	do { \
		(ins)->data [index] = * (guint16 *)(v); \
		(ins)->data [index + 1] = * ((guint16 *)(v) + 1); \
		(ins)->data [index + 2] = * ((guint16 *)(v) + 2); \
		(ins)->data [index + 3] = * ((guint16 *)(v) + 3); \
	} while (0)
#else
#define WRITE32(ip, v) \
	do { \
		* (guint32*)(ip) = * (guint32 *)(v); \
		(ip) += 2; \
	} while (0)
#define WRITE32_INS(ins, index, v) \
	do { \
		* (guint32 *)(&(ins)->data [index]) = * (guint32 *)(v); \
	} while (0)

#define WRITE64(ip, v) \
	do { \
		* (guint64*)(ip) = * (guint64 *)(v); \
		(ip) += 4; \
	} while (0)
#define WRITE64_INS(ins, index, v) \
	do { \
		* (guint64 *)(&(ins)->data [index]) = * (guint64 *)(v); \
	} while (0)

#endif


static void 
handle_branch (TransformData *td, int short_op, int long_op, int offset)
{
	int shorten_branch = 0;
	int target = td->ip + offset - td->il_code;
	if (target < 0 || target >= td->code_size)
		g_assert_not_reached ();
	/* Add exception checkpoint or safepoint for backward branches */
	if (offset < 0) {
		if (mono_threads_are_safepoints_enabled ())
			interp_add_ins (td, MINT_SAFEPOINT);
		else
			interp_add_ins (td, MINT_CHECKPOINT);
	}
	if (offset > 0 && td->stack_height [target] < 0) {
		td->stack_height [target] = td->sp - td->stack;
		if (td->stack_height [target] > 0)
			td->stack_state [target] = (StackInfo*)g_memdup (td->stack, td->stack_height [target] * sizeof (td->stack [0]));
		td->vt_stack_size [target] = td->vt_sp;
	}

	if (td->header->code_size <= 25000) /* FIX to be precise somehow? */
		shorten_branch = 1;

	if (shorten_branch) {
		interp_add_ins (td, short_op);
		td->last_ins->data [0] = (guint16) target;
	} else {
		interp_add_ins (td, long_op);
		WRITE32_INS (td->last_ins, 0, &target);
	}
}

static void 
one_arg_branch(TransformData *td, int mint_op, int offset) 
{
	int type = td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP ? STACK_TYPE_I : td->sp [-1].type;
	int long_op = mint_op + type - STACK_TYPE_I4;
	int short_op = long_op + MINT_BRFALSE_I4_S - MINT_BRFALSE_I4;
	CHECK_STACK(td, 1);
	--td->sp;
	handle_branch (td, short_op, long_op, offset);
}

static void 
two_arg_branch(TransformData *td, int mint_op, int offset) 
{
	int type1 = td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP ? STACK_TYPE_I : td->sp [-1].type;
	int type2 = td->sp [-2].type == STACK_TYPE_O || td->sp [-2].type == STACK_TYPE_MP ? STACK_TYPE_I : td->sp [-2].type;
	int long_op = mint_op + type1 - STACK_TYPE_I4;
	int short_op = long_op + MINT_BEQ_I4_S - MINT_BEQ_I4;
	CHECK_STACK(td, 2);
	if (type1 == STACK_TYPE_I4 && type2 == STACK_TYPE_I8) {
		// The il instruction starts with the actual branch, and not with the conversion opcodes
		interp_insert_ins (td, td->last_ins, MINT_CONV_I8_I4);
	} else if (type1 == STACK_TYPE_I8 && type2 == STACK_TYPE_I4) {
		interp_insert_ins (td, td->last_ins, MINT_CONV_I8_I4_SP);
	} else if (type1 == STACK_TYPE_R4 && type2 == STACK_TYPE_R8) {
		interp_insert_ins (td, td->last_ins, MINT_CONV_R8_R4);
	} else if (type1 == STACK_TYPE_R8 && type2 == STACK_TYPE_R4) {
		interp_insert_ins (td, td->last_ins, MINT_CONV_R8_R4_SP);
	} else if (type1 != type2) {
		g_warning("%s.%s: branch type mismatch %d %d", 
			m_class_get_name (td->method->klass), td->method->name,
			td->sp [-1].type, td->sp [-2].type);
	}
	td->sp -= 2;
	handle_branch (td, short_op, long_op, offset);
}

static void
unary_arith_op(TransformData *td, int mint_op)
{
	int op = mint_op + td->sp [-1].type - STACK_TYPE_I4;
	CHECK_STACK(td, 1);
	interp_add_ins (td, op);
}

static void
binary_arith_op(TransformData *td, int mint_op)
{
	int type1 = td->sp [-2].type;
	int type2 = td->sp [-1].type;
	int op;
#if SIZEOF_VOID_P == 8
	if ((type1 == STACK_TYPE_MP || type1 == STACK_TYPE_I8) && type2 == STACK_TYPE_I4) {
		interp_add_ins (td, MINT_CONV_I8_I4);
		type2 = STACK_TYPE_I8;
	}
	if (type1 == STACK_TYPE_I4 && (type2 == STACK_TYPE_MP || type2 == STACK_TYPE_I8)) {
		interp_add_ins (td, MINT_CONV_I8_I4_SP);
		type1 = STACK_TYPE_I8;
		td->sp [-2].type = STACK_TYPE_I8;
	}
#endif
	if (type1 == STACK_TYPE_R8 && type2 == STACK_TYPE_R4) {
		interp_add_ins (td, MINT_CONV_R8_R4);
		type2 = STACK_TYPE_R8;
	}
	if (type1 == STACK_TYPE_R4 && type2 == STACK_TYPE_R8) {
		interp_add_ins (td, MINT_CONV_R8_R4_SP);
		type1 = STACK_TYPE_R8;
		td->sp [-2].type = STACK_TYPE_R8;
	}
	if (type1 == STACK_TYPE_MP)
		type1 = STACK_TYPE_I;
	if (type2 == STACK_TYPE_MP)
		type2 = STACK_TYPE_I;
	if (type1 != type2) {
		g_warning("%s.%s: %04x arith type mismatch %s %d %d", 
			m_class_get_name (td->method->klass), td->method->name,
			td->ip - td->il_code, mono_interp_opname (mint_op), type1, type2);
	}
	op = mint_op + type1 - STACK_TYPE_I4;
	CHECK_STACK(td, 2);
	interp_add_ins (td, op);
	--td->sp;
}

static void
shift_op(TransformData *td, int mint_op)
{
	int op = mint_op + td->sp [-2].type - STACK_TYPE_I4;
	CHECK_STACK(td, 2);
	if (td->sp [-1].type != STACK_TYPE_I4) {
		g_warning("%s.%s: shift type mismatch %d", 
			m_class_get_name (td->method->klass), td->method->name,
			td->sp [-2].type);
	}
	interp_add_ins (td, op);
	--td->sp;
}

static int 
can_store (int st_value, int vt_value)
{
	if (st_value == STACK_TYPE_O || st_value == STACK_TYPE_MP)
		st_value = STACK_TYPE_I;
	if (vt_value == STACK_TYPE_O || vt_value == STACK_TYPE_MP)
		vt_value = STACK_TYPE_I;
	return st_value == vt_value;
}

#define SET_SIMPLE_TYPE(s, ty) \
	do { \
		(s)->type = (ty); \
		(s)->flags = 0; \
		(s)->klass = NULL; \
	} while (0)

#define SET_TYPE(s, ty, k) \
	do { \
		(s)->type = (ty); \
		(s)->flags = 0; \
		(s)->klass = k; \
	} while (0)

#define REALLOC_STACK(td, sppos) \
	do { \
		(td)->stack_capacity *= 2; \
		(td)->stack = (StackInfo*)realloc ((td)->stack, (td)->stack_capacity * sizeof (td->stack [0])); \
		(td)->sp = (td)->stack + (sppos); \
	} while (0);

#define PUSH_SIMPLE_TYPE(td, ty) \
	do { \
		int sp_height; \
		(td)->sp++; \
		sp_height = (td)->sp - (td)->stack; \
		if (sp_height > (td)->max_stack_height) \
			(td)->max_stack_height = sp_height; \
		if (sp_height > (td)->stack_capacity) \
			REALLOC_STACK(td, sp_height); \
		SET_SIMPLE_TYPE((td)->sp - 1, ty); \
	} while (0)

#define PUSH_TYPE(td, ty, k) \
	do { \
		int sp_height; \
		(td)->sp++; \
		sp_height = (td)->sp - (td)->stack; \
		if (sp_height > (td)->max_stack_height) \
			(td)->max_stack_height = sp_height; \
		if (sp_height > (td)->stack_capacity) \
			REALLOC_STACK(td, sp_height); \
		SET_TYPE((td)->sp - 1, ty, k); \
	} while (0)

static void
move_stack (TransformData *td, int start, int amount)
{
	int sp_height = td->sp - td->stack;
	int to_move = sp_height - start;

	td->sp += amount;
	sp_height += amount;
	if (amount > 0) {
		if (sp_height > td->max_stack_height)
			td->max_stack_height = sp_height;
		if (sp_height > td->stack_capacity)
			REALLOC_STACK (td, sp_height);
	} else {
		g_assert (td->sp >= td->stack);
	}

	if (to_move > 0)
		memmove (td->stack + start + amount, td->stack + start, to_move * sizeof (StackInfo));
}

static void
simulate_runtime_stack_increase (TransformData *td, int amount)
{
	const int sp_height = td->sp - td->stack + amount;

	if (sp_height > td->max_stack_height)
		td->max_stack_height = sp_height;
}

#define PUSH_VT(td, size) \
	do { \
		(td)->vt_sp += ALIGN_TO ((size), MINT_VT_ALIGNMENT); \
		if ((td)->vt_sp > (td)->max_vt_sp) \
			(td)->max_vt_sp = (td)->vt_sp; \
	} while (0)

#define POP_VT(td, size) \
	do { \
		(td)->vt_sp -= ALIGN_TO ((size), MINT_VT_ALIGNMENT); \
	} while (0)

static MonoType*
get_arg_type_exact (TransformData *td, int n, int *mt)
{
	MonoType *type;
	gboolean hasthis = mono_method_signature_internal (td->method)->hasthis;

	if (hasthis && n == 0)
		type = m_class_get_byval_arg (td->method->klass);
	else
		type = mono_method_signature_internal (td->method)->params [n - !!hasthis];

	if (mt)
		*mt = mint_type (type);

	return type;
}

static void 
load_arg(TransformData *td, int n)
{
	int mt;
	MonoClass *klass = NULL;
	MonoType *type;
	gboolean hasthis = mono_method_signature_internal (td->method)->hasthis;

	type = get_arg_type_exact (td, n, &mt);

	if (mt == MINT_TYPE_VT) {
		gint32 size;
		klass = mono_class_from_mono_type_internal (type);
		if (mono_method_signature_internal (td->method)->pinvoke)
			size = mono_class_native_size (klass, NULL);
		else
			size = mono_class_value_size (klass, NULL);

		if (hasthis && n == 0) {
			mt = MINT_TYPE_I;
			interp_add_ins (td, MINT_LDLOC_O);
			td->last_ins->data [0] = 0;
			klass = NULL;
		} else {
			PUSH_VT (td, size);
			interp_add_ins (td, MINT_LDARG_VT);
			td->last_ins->data [0] = n;
			WRITE32_INS (td->last_ins, 1, &size);
		}
	} else {
		if ((hasthis || mt == MINT_TYPE_I) && n == 0) {
			// Special case loading of the first ptr sized argument
			if (mt != MINT_TYPE_O)
				mt = MINT_TYPE_I;
			interp_add_ins (td, MINT_LDLOC_O);
			td->last_ins->data [0] = 0;
		} else {
			interp_add_ins (td, MINT_LDLOC_I1 + (mt - MINT_TYPE_I1));
			td->last_ins->data [0] = n;
			if (mt == MINT_TYPE_O)
				klass = mono_class_from_mono_type_internal (type);
		}
	}
	PUSH_TYPE(td, stack_type[mt], klass);
}

static void 
store_arg(TransformData *td, int n)
{
	int mt;
	CHECK_STACK (td, 1);
	MonoType *type;

	type = get_arg_type_exact (td, n, &mt);

	if (mt == MINT_TYPE_VT) {
		gint32 size;
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		if (mono_method_signature_internal (td->method)->pinvoke)
			size = mono_class_native_size (klass, NULL);
		else
			size = mono_class_value_size (klass, NULL);
		interp_add_ins (td, MINT_STARG_VT);
		td->last_ins->data [0] = n;
		WRITE32_INS (td->last_ins, 1, &size);
		if (td->sp [-1].type == STACK_TYPE_VT)
			POP_VT(td, size);
	} else {
		interp_add_ins (td, MINT_STLOC_I1 + (mt - MINT_TYPE_I1));
		td->last_ins->data [0] = n;
	}
	--td->sp;
}

static void
load_local (TransformData *td, int local)
{
	MonoType *type = td->locals [local].type;
	int mt = td->locals [local].mt;
	MonoClass *klass = NULL;
	if (mt == MINT_TYPE_VT) {
		klass = mono_class_from_mono_type_internal (type);
		gint32 size = mono_class_value_size (klass, NULL);
		PUSH_VT(td, size);
		interp_add_ins (td, MINT_LDLOC_VT);
		td->last_ins->data [0] = local;
		WRITE32_INS (td->last_ins, 1, &size);
	} else {
		g_assert (mt < MINT_TYPE_VT);
		interp_add_ins (td, MINT_LDLOC_I1 + (mt - MINT_TYPE_I1));
		td->last_ins->data [0] = local;
		if (mt == MINT_TYPE_O)
			klass = mono_class_from_mono_type_internal (type);
	}
	PUSH_TYPE(td, stack_type[mt], klass);
}

static void 
store_local (TransformData *td, int local)
{
	MonoType *type = td->locals [local].type;
	int mt = td->locals [local].mt;
	CHECK_STACK (td, 1);
#if SIZEOF_VOID_P == 8
	if (td->sp [-1].type == STACK_TYPE_I4 && stack_type [mt] == STACK_TYPE_I8) {
		interp_add_ins (td, MINT_CONV_I8_I4);
		td->sp [-1].type = STACK_TYPE_I8;
	}
#endif
	if (!can_store(td->sp [-1].type, stack_type [mt])) {
		g_warning("%s.%s: Store local stack type mismatch %d %d", 
			m_class_get_name (td->method->klass), td->method->name,
			stack_type [mt], td->sp [-1].type);
	}
	if (mt == MINT_TYPE_VT) {
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		gint32 size = mono_class_value_size (klass, NULL);
		interp_add_ins (td, MINT_STLOC_VT);
		td->last_ins->data [0] = local;
		WRITE32_INS (td->last_ins, 1, &size);
		if (td->sp [-1].type == STACK_TYPE_VT)
			POP_VT(td, size);
	} else {
		g_assert (mt < MINT_TYPE_VT);
		interp_add_ins (td, MINT_STLOC_I1 + (mt - MINT_TYPE_I1));
		td->last_ins->data [0] = local;
	}
	--td->sp;
}

#define SIMPLE_OP(td, op) \
	do { \
		interp_add_ins (td, op); \
		++td->ip; \
	} while (0)

static guint16
get_data_item_index (TransformData *td, void *ptr)
{
	gpointer p = g_hash_table_lookup (td->data_hash, ptr);
	guint index;
	if (p != NULL)
		return GPOINTER_TO_UINT (p) - 1;
	if (td->max_data_items == td->n_data_items) {
		td->max_data_items = td->n_data_items == 0 ? 16 : 2 * td->max_data_items;
		td->data_items = (gpointer*)g_realloc (td->data_items, td->max_data_items * sizeof(td->data_items [0]));
	}
	index = td->n_data_items;
	td->data_items [index] = ptr;
	++td->n_data_items;
	g_hash_table_insert (td->data_hash, ptr, GUINT_TO_POINTER (index + 1));
	return index;
}

static guint16
get_data_item_index_nonshared (TransformData *td, void *ptr)
{
	guint index;
	if (td->max_data_items == td->n_data_items) {
		td->max_data_items = td->n_data_items == 0 ? 16 : 2 * td->max_data_items;
		td->data_items = (gpointer*)g_realloc (td->data_items, td->max_data_items * sizeof(td->data_items [0]));
	}
	index = td->n_data_items;
	td->data_items [index] = ptr;
	++td->n_data_items;
	return index;
}

gboolean
mono_interp_jit_call_supported (MonoMethod *method, MonoMethodSignature *sig)
{
	GSList *l;

	if (sig->param_count > 6)
		return FALSE;
	if (sig->pinvoke)
		return FALSE;
	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
		return FALSE;
	if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
		return FALSE;
	if (method->is_inflated)
		return FALSE;
	if (method->string_ctor)
		return FALSE;
	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;

	if (mono_aot_only && m_class_get_image (method->klass)->aot_module && !(method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)) {
		ERROR_DECL (error);
		gpointer addr = mono_jit_compile_method_jit_only (method, error);
		if (addr && is_ok (error))
			return TRUE;
	}

	for (l = mono_interp_jit_classes; l; l = l->next) {
		const char *class_name = (const char*)l->data;
		// FIXME: Namespaces
		if (!strcmp (m_class_get_name (method->klass), class_name))
			return TRUE;
	}

	//return TRUE;
	return FALSE;
}

#ifdef ENABLE_EXPERIMENT_TIERED
static gboolean
jit_call2_supported (MonoMethod *method, MonoMethodSignature *sig)
{
	if (sig->param_count > 6)
		return FALSE;
	if (sig->pinvoke)
		return FALSE;
	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
		return FALSE;
	if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
		return FALSE;
	if (method->is_inflated)
		return FALSE;
	if (method->string_ctor)
		return FALSE;

	return TRUE;
}
#endif

static int mono_class_get_magic_index (MonoClass *k)
{
	if (mono_class_is_magic_int (k))
		return !strcmp ("nint", m_class_get_name (k)) ? 0 : 1;

	if (mono_class_is_magic_float (k))
		return 2;

	return -1;
}

static void
interp_generate_mae_throw (TransformData *td, MonoMethod *method, MonoMethod *target_method)
{
	MonoJitICallInfo *info = &mono_get_jit_icall_info ()->mono_throw_method_access;

	/* Inject code throwing MethodAccessException */
	interp_add_ins (td, MINT_MONO_LDPTR);
	td->last_ins->data [0] = get_data_item_index (td, method);
	PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);

	interp_add_ins (td, MINT_MONO_LDPTR);
	td->last_ins->data [0] = get_data_item_index (td, target_method);
	PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);

	interp_add_ins (td, MINT_ICALL_PP_V);
	td->last_ins->data [0] = get_data_item_index (td, (gpointer)info->func);

	td->sp -= 2;
}

static void
interp_generate_bie_throw (TransformData *td)
{
	MonoJitICallInfo *info = &mono_get_jit_icall_info ()->mono_throw_bad_image;

	interp_add_ins (td, MINT_ICALL_V_V);
	td->last_ins->data [0] = get_data_item_index (td, (gpointer)info->func);
}

static void
interp_generate_not_supported_throw (TransformData *td)
{
	MonoJitICallInfo *info = &mono_get_jit_icall_info ()->mono_throw_not_supported;

	interp_add_ins (td, MINT_ICALL_V_V);
	td->last_ins->data [0] = get_data_item_index (td, (gpointer)info->func);
}

static void
interp_generate_ipe_throw_with_msg (TransformData *td, MonoError *error_msg)
{
	MonoJitICallInfo *info = &mono_get_jit_icall_info ()->mono_throw_invalid_program;

	char *msg = mono_mempool_strdup (td->rtm->domain->mp, mono_error_get_message (error_msg));

	interp_add_ins (td, MINT_MONO_LDPTR);
	td->last_ins->data [0] = get_data_item_index (td, msg);
	PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);

	interp_add_ins (td, MINT_ICALL_P_V);
	td->last_ins->data [0] = get_data_item_index (td, (gpointer)info->func);

	td->sp -= 1;
}

/*
 * These are additional locals that can be allocated as we transform the code.
 * They are allocated past the method locals so they are accessed in the same
 * way, with an offset relative to the frame->locals.
 */
static int
create_interp_local (TransformData *td, MonoType *type)
{
	if (td->locals_size == td->locals_capacity) {
		td->locals_capacity *= 2;
		if (td->locals_capacity == 0)
			td->locals_capacity = 2;
		td->locals = (InterpLocal*) g_realloc (td->locals, td->locals_capacity * sizeof (InterpLocal));
	}
	td->locals [td->locals_size].type = type;
	td->locals [td->locals_size].mt = mint_type (type);
	td->locals [td->locals_size].flags = 0;
	td->locals [td->locals_size].indirects = 0;
	td->locals [td->locals_size].offset = -1;
	td->locals_size++;
	return td->locals_size - 1;
}

static int
get_interp_local_offset (TransformData *td, int local)
{
	int align, size, offset;

	if (td->locals [local].offset != -1)
		return td->locals [local].offset;

	offset = td->total_locals_size;
	size = mono_type_size (td->locals [local].type, &align);
	offset = ALIGN_TO (offset, align);

	td->locals [local].offset = offset;

	td->total_locals_size = offset + size;
	g_assert (td->total_locals_size < G_MAXUINT16);

	return offset;
}

static void
dump_mint_code (const guint16 *start, const guint16* end)
{
	const guint16 *p = start;
	while (p < end) {
		char *ins = mono_interp_dis_mintop ((gint32)(p - start), TRUE, p + 1, *p);
		g_print ("%s\n", ins);
		g_free (ins);
		p = mono_interp_dis_mintop_len (p);
	}
}

static void
dump_interp_inst (InterpInst *ins)
{
	char *descr = mono_interp_dis_mintop (ins->il_offset, FALSE, &ins->data [0], ins->opcode);
	g_print ("%s", descr);
	g_free (descr);
}

static void
dump_interp_inst_newline (InterpInst *ins)
{
	dump_interp_inst (ins);
	g_print ("\n");
}

/* For debug use */
void
mono_interp_print_code (InterpMethod *imethod)
{
	MonoJitInfo *jinfo = imethod->jinfo;
	const guint8 *start;

	if (!jinfo)
		return;

	char *name = mono_method_full_name (imethod->method, 1);
	g_print ("Method : %s\n", name);
	g_free (name);

	start = (guint8*) jinfo->code_start;
	dump_mint_code ((const guint16*)start, (const guint16*)(start + jinfo->code_size));
}

/* For debug use */
void
mono_interp_print_td_code (TransformData *td)
{
	InterpInst *ins = td->first_ins;

	char *name = mono_method_full_name (td->method, TRUE);
	g_print ("IR for \"%s\"\n", name);
	g_free (name);
	while (ins) {
		dump_interp_inst_newline (ins);
		ins = ins->next;
	}
}


static MonoMethodHeader*
interp_method_get_header (MonoMethod* method, MonoError *error)
{
	/* An explanation: mono_method_get_header_internal returns an error if
	 * called on a method with no body (e.g. an abstract method, or an
	 * icall).  We don't want that.
	 */
	if (mono_method_has_no_body (method))
		return NULL;
	else
		return mono_method_get_header_internal (method, error);
}

/* stores top of stack as local and pushes address of it on stack */
static void
emit_store_value_as_local (TransformData *td, MonoType *src)
{
	int size = mini_magic_type_size (NULL, src);
	int local = create_interp_local (td, mini_native_type_replace_type (src));

	store_local (td, local);

	size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
	interp_add_ins (td, MINT_LDLOC_VT);
	td->last_ins->data [0] = local;
	WRITE32_INS (td->last_ins, 1, &size);

	PUSH_VT (td, size);
	PUSH_TYPE (td, STACK_TYPE_VT, NULL);
}

// Returns whether we can optimize away the instructions starting at start.
// If any instructions are part of a new basic block, we can't remove them.
static gboolean
interp_is_bb_start (TransformData *td, InterpInst *start, InterpInst *end)
{
	InterpInst *ins = start;
	while (ins != end) {
		if (ins->il_offset != -1) {
			if (td->is_bb_start [ins->il_offset])
				return TRUE;
		}
		ins = ins->next;
	}

	// Also check if end is bb start
	if (ins != NULL && ins->il_offset != -1 && td->is_bb_start [ins->il_offset])
		return TRUE;
	return FALSE;
}

static gboolean
interp_ins_is_ldc (InterpInst *ins)
{
	return ins->opcode >= MINT_LDC_I4_M1 && ins->opcode <= MINT_LDC_I8;
}

static gint32
interp_get_const_from_ldc_i4 (InterpInst *ins)
{
	switch (ins->opcode) {
	case MINT_LDC_I4_M1: return -1;
	case MINT_LDC_I4_0: return 0;
	case MINT_LDC_I4_1: return 1;
	case MINT_LDC_I4_2: return 2;
	case MINT_LDC_I4_3: return 3;
	case MINT_LDC_I4_4: return 4;
	case MINT_LDC_I4_5: return 5;
	case MINT_LDC_I4_6: return 6;
	case MINT_LDC_I4_7: return 7;
	case MINT_LDC_I4_8: return 8;
	case MINT_LDC_I4_S: return (gint32)(gint8)ins->data [0];
	case MINT_LDC_I4: return READ32 (&ins->data [0]);
	default:
		g_assert_not_reached ();
	}
}

/* If ins is not null, it will replace it with the ldc */
static InterpInst*
interp_get_ldc_i4_from_const (TransformData *td, InterpInst *ins, gint32 ct)
{
	int opcode;
	switch (ct) {
	case -1: opcode = MINT_LDC_I4_M1; break;
	case 0: opcode = MINT_LDC_I4_0; break;
	case 1: opcode = MINT_LDC_I4_1; break;
	case 2: opcode = MINT_LDC_I4_2; break;
	case 3: opcode = MINT_LDC_I4_3; break;
	case 4: opcode = MINT_LDC_I4_4; break;
	case 5: opcode = MINT_LDC_I4_5; break;
	case 6: opcode = MINT_LDC_I4_6; break;
	case 7: opcode = MINT_LDC_I4_7; break;
	case 8: opcode = MINT_LDC_I4_8; break;
	default:
		if (ct >= -128 && ct <= 127)
			opcode = MINT_LDC_I4_S;
		else
			opcode = MINT_LDC_I4;
		break;
	}

	int new_size = mono_interp_oplen [opcode];

	if (ins == NULL)
		ins = interp_add_ins (td, opcode);

	int ins_size = mono_interp_oplen [ins->opcode];
	if (ins_size < new_size) {
		// We can't replace the passed instruction, discard it and emit a new one
		ins = interp_insert_ins (td, ins, opcode);
		interp_clear_ins (td, ins->prev);
	} else {
		ins->opcode = opcode;
	}

	if (new_size == 2)
		ins->data [0] = (gint8)ct;
	else if (new_size == 3)
		WRITE32_INS (ins, 0, &ct);

	return ins;
}

static InterpInst*
interp_inst_replace_with_i8_const (TransformData *td, InterpInst *ins, gint64 ct)
{
	int size = mono_interp_oplen [ins->opcode];

	if (size < 5) {
		ins = interp_insert_ins (td, ins, MINT_LDC_I8);
		interp_clear_ins (td, ins->prev);
	} else {
		ins->opcode = MINT_LDC_I8;
	}
	WRITE64_INS (ins, 0, &ct);

	return ins;
}

static int
interp_get_ldind_for_mt (int mt)
{
	switch (mt) {
		case MINT_TYPE_I1: return MINT_LDIND_I1_CHECK;
		case MINT_TYPE_U1: return MINT_LDIND_U1_CHECK;
		case MINT_TYPE_I2: return MINT_LDIND_I2_CHECK;
		case MINT_TYPE_U2: return MINT_LDIND_U2_CHECK;
		case MINT_TYPE_I4: return MINT_LDIND_I4_CHECK;
		case MINT_TYPE_I8: return MINT_LDIND_I8_CHECK;
		case MINT_TYPE_R4: return MINT_LDIND_R4_CHECK;
		case MINT_TYPE_R8: return MINT_LDIND_R8_CHECK;
		case MINT_TYPE_O: return MINT_LDIND_REF;
		default:
			g_assert_not_reached ();
	}
	return -1;
}

static void
interp_emit_ldobj (TransformData *td, MonoClass *klass)
{
	int mt = mint_type (m_class_get_byval_arg (klass));
	int size;

	if (mt == MINT_TYPE_VT) {
		interp_add_ins (td, MINT_LDOBJ_VT);
		size = mono_class_value_size (klass, NULL);
		WRITE32_INS (td->last_ins, 0, &size);
		PUSH_VT (td, size);
	} else {
		int opcode = interp_get_ldind_for_mt (mt);
		interp_add_ins (td, opcode);
	}

	SET_TYPE (td->sp - 1, stack_type [mt], klass);
}

static void
interp_emit_stobj (TransformData *td, MonoClass *klass)
{
	int mt = mint_type (m_class_get_byval_arg (klass));

	if (mt == MINT_TYPE_VT) {
		int size;
		interp_add_ins (td, MINT_STOBJ_VT);
		td->last_ins->data [0] = get_data_item_index(td, klass);
		size = mono_class_value_size (klass, NULL);
		POP_VT (td, size);
	} else {
		int opcode;
		switch (mt) {
			case MINT_TYPE_I1:
			case MINT_TYPE_U1:
				opcode = MINT_STIND_I1;
				break;
			case MINT_TYPE_I2:
			case MINT_TYPE_U2:
				opcode = MINT_STIND_I2;
				break;
			case MINT_TYPE_I4:
				opcode = MINT_STIND_I4;
				break;
			case MINT_TYPE_I8:
				opcode = MINT_STIND_I8;
				break;
			case MINT_TYPE_R4:
				opcode = MINT_STIND_R4;
				break;
			case MINT_TYPE_R8:
				opcode = MINT_STIND_R8;
				break;
			case MINT_TYPE_O:
				opcode = MINT_STIND_REF;
				break;
			default: g_assert_not_reached (); break;
		}
		interp_add_ins (td, opcode);
	}
	td->sp -= 2;
}

static void
interp_emit_ldelema (TransformData *td, MonoClass *array_class, MonoClass *check_class)
{
	MonoClass *element_class = m_class_get_element_class (array_class);
	int rank = m_class_get_rank (array_class);
	int size = mono_class_array_element_size (element_class);

	// We only need type checks when writing to array of references
	if (!check_class || m_class_is_valuetype (element_class)) {
		if (rank == 1) {
			interp_add_ins (td, MINT_LDELEMA1);
			WRITE32_INS (td->last_ins, 0, &size);
		} else {
			interp_add_ins (td, MINT_LDELEMA);
			td->last_ins->data [0] = rank;
			WRITE32_INS (td->last_ins, 1, &size);
		}
	} else {
		interp_add_ins (td, MINT_LDELEMA_TC);
		td->last_ins->data [0] = rank;
		td->last_ins->data [1] = get_data_item_index (td, check_class);
	}

	td->sp -= rank;
	SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_MP);
}

static gboolean
interp_handle_magic_type_intrinsics (TransformData *td, MonoMethod *target_method, MonoMethodSignature *csignature, int type_index)
{
	MonoClass *magic_class = target_method->klass;
	const char *tm = target_method->name;
	int i;

	const int mt = mint_type (m_class_get_byval_arg (magic_class));
	if (!strcmp (".ctor", tm)) {
		MonoType *arg = csignature->params [0];
		/* depending on SIZEOF_VOID_P and the type of the value passed to the .ctor we either have to CONV it, or do nothing */
		int arg_size = mini_magic_type_size (NULL, arg);

		if (arg_size > SIZEOF_VOID_P) { // 8 -> 4
			switch (type_index) {
			case 0: case 1:
				interp_add_ins (td, MINT_CONV_I4_I8);
				break;
			case 2:
				interp_add_ins (td, MINT_CONV_R4_R8);
				break;
			}
		}

		if (arg_size < SIZEOF_VOID_P) { // 4 -> 8
			switch (type_index) {
			case 0:
				interp_add_ins (td, MINT_CONV_I8_I4);
				break;
			case 1:
				interp_add_ins (td, MINT_CONV_I8_U4);
				break;
			case 2:
				interp_add_ins (td, MINT_CONV_R8_R4);
				break;
			}
		}

		switch (type_index) {
		case 0: case 1:
#if SIZEOF_VOID_P == 4
			interp_add_ins (td, MINT_STIND_I4);
#else
			interp_add_ins (td, MINT_STIND_I8);
#endif
			break;
		case 2:
#if SIZEOF_VOID_P == 4
			interp_add_ins (td, MINT_STIND_R4);
#else
			interp_add_ins (td, MINT_STIND_R8);
#endif
			break;
		}

		td->sp -= 2;
		td->ip += 5;
		return TRUE;
	} else if (!strcmp ("op_Implicit", tm ) || !strcmp ("op_Explicit", tm)) {
		MonoType *src = csignature->params [0];
		MonoType *dst = csignature->ret;
		MonoClass *src_klass = mono_class_from_mono_type_internal (src);
		int src_size = mini_magic_type_size (NULL, src);
		int dst_size = mini_magic_type_size (NULL, dst);

		gboolean store_value_as_local = FALSE;

		switch (type_index) {
		case 0: case 1:
			if (!mini_magic_is_int_type (src) || !mini_magic_is_int_type (dst)) {
				if (mini_magic_is_int_type (src))
					store_value_as_local = TRUE;
				else if (mono_class_is_magic_float (src_klass))
					store_value_as_local = TRUE;
				else
					return FALSE;
			}
			break;
		case 2:
			if (!mini_magic_is_float_type (src) || !mini_magic_is_float_type (dst)) {
				if (mini_magic_is_float_type (src))
					store_value_as_local = TRUE;
				else if (mono_class_is_magic_int (src_klass))
					store_value_as_local = TRUE;
				else
					return FALSE;
			}
			break;
		}

		if (store_value_as_local) {
			emit_store_value_as_local (td, src);

			/* emit call to managed conversion method */
			return FALSE;
		}

		if (src_size > dst_size) { // 8 -> 4
			switch (type_index) {
			case 0: case 1:
				interp_add_ins (td, MINT_CONV_I4_I8);
				break;
			case 2:
				interp_add_ins (td, MINT_CONV_R4_R8);
				break;
			}
		}

		if (src_size < dst_size) { // 4 -> 8
			switch (type_index) {
			case 0:
				interp_add_ins (td, MINT_CONV_I8_I4);
				break;
			case 1:
				interp_add_ins (td, MINT_CONV_I8_U4);
				break;
			case 2:
				interp_add_ins (td, MINT_CONV_R8_R4);
				break;
			}
		}

		SET_TYPE (td->sp - 1, stack_type [mint_type (dst)], mono_class_from_mono_type_internal (dst));
		td->ip += 5;
		return TRUE;
	} else if (!strcmp ("op_Increment", tm)) {
		g_assert (type_index != 2); // no nfloat
#if SIZEOF_VOID_P == 8
		interp_add_ins (td, MINT_ADD1_I8);
#else
		interp_add_ins (td, MINT_ADD1_I4);
#endif
		SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
		td->ip += 5;
		return TRUE;
	} else if (!strcmp ("op_Decrement", tm)) {
		g_assert (type_index != 2); // no nfloat
#if SIZEOF_VOID_P == 8
		interp_add_ins (td, MINT_SUB1_I8);
#else
		interp_add_ins (td, MINT_SUB1_I4);
#endif
		SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
		td->ip += 5;
		return TRUE;
	} else if (!strcmp ("CompareTo", tm) || !strcmp ("Equals", tm)) {
		MonoType *arg = csignature->params [0];

		/* on 'System.n*::{CompareTo,Equals} (System.n*)' variant we need to push managed
		 * pointer instead of value */
		if (arg->type == MONO_TYPE_VALUETYPE)
			emit_store_value_as_local (td, arg);

		/* emit call to managed conversion method */
		return FALSE;
	} else if (!strcmp (".cctor", tm)) {
		return FALSE;
	} else if (!strcmp ("Parse", tm)) {
		return FALSE;
	} else if (!strcmp ("ToString", tm)) {
		return FALSE;
	} else if (!strcmp ("GetHashCode", tm)) {
		return FALSE;
	} else if (!strcmp ("IsNaN", tm) || !strcmp ("IsInfinity", tm) || !strcmp ("IsNegativeInfinity", tm) || !strcmp ("IsPositiveInfinity", tm)) {
		g_assert (type_index == 2); // nfloat only
		return FALSE;
	}

	for (i = 0; i < sizeof (int_unnop) / sizeof  (MagicIntrinsic); ++i) {
		if (!strcmp (int_unnop [i].op_name, tm)) {
			interp_add_ins (td, int_unnop [i].insn [type_index]);
			SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
			td->ip += 5;
			return TRUE;
		}
	}

	for (i = 0; i < sizeof (int_binop) / sizeof  (MagicIntrinsic); ++i) {
		if (!strcmp (int_binop [i].op_name, tm)) {
			interp_add_ins (td, int_binop [i].insn [type_index]);
			td->sp -= 1;
			SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
			td->ip += 5;
			return TRUE;
		}
	}

	for (i = 0; i < sizeof (int_cmpop) / sizeof  (MagicIntrinsic); ++i) {
		if (!strcmp (int_cmpop [i].op_name, tm)) {
			MonoClass *k = mono_defaults.boolean_class;
			interp_add_ins (td, int_cmpop [i].insn [type_index]);
			td->sp -= 1;
			SET_TYPE (td->sp - 1, stack_type [mint_type (m_class_get_byval_arg (k))], k);
			td->ip += 5;
			return TRUE;
		}
	}

	g_error ("TODO: interp_transform_call %s:%s", m_class_get_name (target_method->klass), tm);
}

/* Return TRUE if call transformation is finished */
static gboolean
interp_handle_intrinsics (TransformData *td, MonoMethod *target_method, MonoClass *constrained_class, MonoMethodSignature *csignature, gboolean readonly, int *op)
{
	const char *tm = target_method->name;
	int type_index = mono_class_get_magic_index (target_method->klass);
	gboolean in_corlib = m_class_get_image (target_method->klass) == mono_defaults.corlib;
	const char *klass_name_space;
	if (m_class_get_nested_in (target_method->klass))
		klass_name_space = m_class_get_name_space (m_class_get_nested_in (target_method->klass));
	else
		klass_name_space = m_class_get_name_space (target_method->klass);
	const char *klass_name = m_class_get_name (target_method->klass);

	if (target_method->klass == mono_defaults.string_class) {
		if (tm [0] == 'g') {
			if (strcmp (tm, "get_Chars") == 0)
				*op = MINT_GETCHR;
			else if (strcmp (tm, "get_Length") == 0)
				*op = MINT_STRLEN;
		}
	} else if (type_index >= 0) {
		return interp_handle_magic_type_intrinsics (td, target_method, csignature, type_index);
	} else if (mono_class_is_subclass_of_internal (target_method->klass, mono_defaults.array_class, FALSE)) {
		if (!strcmp (tm, "get_Rank")) {
			*op = MINT_ARRAY_RANK;
		} else if (!strcmp (tm, "get_Length")) {
			*op = MINT_LDLEN;
		} else if (!strcmp (tm, "GetElementSize")) {
			*op = MINT_ARRAY_ELEMENT_SIZE;
		} else if (!strcmp (tm, "IsPrimitive")) {
			*op = MINT_ARRAY_IS_PRIMITIVE;
		} else if (!strcmp (tm, "Address")) {
			MonoClass *check_class = readonly ? NULL : m_class_get_element_class (target_method->klass);
			interp_emit_ldelema (td, target_method->klass, check_class);
			td->ip += 5;
			return TRUE;
#ifndef ENABLE_NETCORE
		} else if (!strcmp (tm, "UnsafeMov") || !strcmp (tm, "UnsafeLoad")) {
			*op = MINT_CALLRUN;
#endif
		} else if (!strcmp (tm, "Get")) {
			interp_emit_ldelema (td, target_method->klass, NULL);
			interp_emit_ldobj (td, m_class_get_element_class (target_method->klass));
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "Set")) {
			MonoClass *element_class = m_class_get_element_class (target_method->klass);
			MonoType *local_type = m_class_get_byval_arg (element_class);
			MonoClass *value_class = td->sp [-1].klass;
			// If value_class is NULL it means the top of stack is a simple type (valuetype)
			// which doesn't require type checks, or that we have no type information because
			// the code is unsafe (like in some wrappers). In that case we assume the type
			// of the array and don't do any checks.

			int local = create_interp_local (td, local_type);

			store_local (td, local);
			interp_emit_ldelema (td, target_method->klass, value_class);
			load_local (td, local);
			interp_emit_stobj (td, element_class);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "UnsafeStore")) {
			g_error ("TODO ArrayClass::UnsafeStore");
		}
	} else if (in_corlib &&
			!strcmp (klass_name_space, "System.Diagnostics") &&
			!strcmp (klass_name, "Debugger")) {
		if (!strcmp (tm, "Break") && csignature->param_count == 0) {
			if (mini_should_insert_breakpoint (td->method))
				*op = MINT_BREAK;
		}
	} else if (in_corlib &&
			!strcmp (klass_name_space, "System") &&
			!strcmp (klass_name, "SpanHelpers") &&
			!strcmp (tm, "ClearWithReferences")) {
		*op = MINT_INTRINS_CLEAR_WITH_REFERENCES;
	} else if (in_corlib && !strcmp (klass_name_space, "System") && !strcmp (klass_name, "ByReference`1")) {
		g_assert (!strcmp (tm, "get_Value"));
		*op = MINT_INTRINS_BYREFERENCE_GET_VALUE;
	} else if (in_corlib && !strcmp (klass_name_space, "System") && !strcmp (klass_name, "Marvin")) {
		if (!strcmp (tm, "Block"))
			*op = MINT_INTRINS_MARVIN_BLOCK;
	} else if (in_corlib && !strcmp (klass_name_space, "System.Text.Unicode") && !strcmp (klass_name, "Utf16Utility")) {
		if (!strcmp (tm, "ConvertAllAsciiCharsInUInt32ToUppercase"))
			*op = MINT_INTRINS_ASCII_CHARS_TO_UPPERCASE;
		else if (!strcmp (tm, "UInt32OrdinalIgnoreCaseAscii"))
			*op = MINT_INTRINS_ORDINAL_IGNORE_CASE_ASCII;
		else if (!strcmp (tm, "UInt64OrdinalIgnoreCaseAscii"))
			*op = MINT_INTRINS_64ORDINAL_IGNORE_CASE_ASCII;
	} else if (in_corlib && !strcmp (klass_name_space, "System.Text") && !strcmp (klass_name, "ASCIIUtility")) {
		if (!strcmp (tm, "WidenAsciiToUtf16"))
			*op = MINT_INTRINS_WIDEN_ASCII_TO_UTF16;
	} else if (in_corlib && !strcmp (klass_name_space, "System") && !strcmp (klass_name, "Number")) {
		if (!strcmp (tm, "UInt32ToDecStr") && csignature->param_count == 1) {
			ERROR_DECL(error);
			MonoVTable *vtable = mono_class_vtable_checked (td->rtm->domain, target_method->klass, error);
			if (!is_ok (error)) {
				mono_error_cleanup (error);
				return FALSE;
			}
			/* Don't use intrinsic if cctor not yet run */
			if (!vtable->initialized)
				return FALSE;
			/* The cache is the first static field. Update this if bcl code changes */
			MonoClassField *field = m_class_get_fields (target_method->klass);
			g_assert (!strcmp (field->name, "s_singleDigitStringCache"));
			interp_add_ins (td, MINT_INTRINS_U32_TO_DECSTR);
			td->last_ins->data [0] = get_data_item_index (td, (char*)mono_vtable_get_static_field_data (vtable) + field->offset);
			td->last_ins->data [1] = get_data_item_index (td, mono_class_vtable_checked (td->rtm->domain, mono_defaults.string_class, error));
			SET_TYPE (td->sp - 1, STACK_TYPE_O, mono_defaults.string_class);
			td->ip += 5;
			return TRUE;
		}
	} else if (in_corlib && !strcmp (klass_name_space, "System") &&
			(!strcmp (klass_name, "Math") || !strcmp (klass_name, "MathF"))) {
		gboolean is_float = strcmp (klass_name, "MathF") == 0;
		int param_type = is_float ? MONO_TYPE_R4 : MONO_TYPE_R8;
		// FIXME add also intrinsic for Round
		if (csignature->param_count == 1 && csignature->params [0]->type == param_type) {
			// unops
			if (tm [0] == 'A') {
				if (strcmp (tm, "Abs") == 0) {
					*op = MINT_ABS;
				} else if (strcmp (tm, "Asin") == 0){
					*op = MINT_ASIN;
				} else if (strcmp (tm, "Asinh") == 0){
					*op = MINT_ASINH;
				} else if (strcmp (tm, "Acos") == 0){
					*op = MINT_ACOS;
				} else if (strcmp (tm, "Acosh") == 0){
					*op = MINT_ACOSH;
				} else if (strcmp (tm, "Atan") == 0){
					*op = MINT_ATAN;
				} else if (strcmp (tm, "Atanh") == 0){
					*op = MINT_ATANH;
				}
			} else if (tm [0] == 'C') {
				if (strcmp (tm, "Ceiling") == 0) {
					*op = MINT_CEILING;
				} else if (strcmp (tm, "Cos") == 0) {
					*op = MINT_COS;
				} else if (strcmp (tm, "Cbrt") == 0){
					*op = MINT_CBRT;
				} else if (strcmp (tm, "Cosh") == 0){
					*op = MINT_COSH;
				}
			} else if (strcmp (tm, "Exp") == 0) {
				*op = MINT_EXP;
			} else if (strcmp (tm, "Floor") == 0) {
				*op = MINT_FLOOR;
			} else if (strcmp (tm, "ILogB") == 0) {
				*op = MINT_ILOGB;
			} else if (tm [0] == 'L') {
				if (strcmp (tm, "Log") == 0) {
					*op = MINT_LOG;
				} else if (strcmp (tm, "Log2") == 0) {
					*op = MINT_LOG2;
				} else if (strcmp (tm, "Log10") == 0) {
					*op = MINT_LOG10;
				}
			} else if (tm [0] == 'S') {
				if (strcmp (tm, "Sin") == 0) {
					*op = MINT_SIN;
				} else if (strcmp (tm, "Sqrt") == 0) {
					*op = MINT_SQRT;
				} else if (strcmp (tm, "Sinh") == 0){
					*op = MINT_SINH;
				}
			} else if (tm [0] == 'T') {
				if (strcmp (tm, "Tan") == 0) {
					*op = MINT_TAN;
				} else if (strcmp (tm, "Tanh") == 0){
					*op = MINT_TANH;
				}
			}
		} else if (csignature->param_count == 2 && csignature->params [0]->type == param_type && csignature->params [1]->type == param_type) {
			if (strcmp (tm, "Atan2") == 0)
				*op = MINT_ATAN2;
			else if (strcmp (tm, "Pow") == 0)
				*op = MINT_POW;
		} else if (csignature->param_count == 3 && csignature->params [0]->type == param_type && csignature->params [1]->type == param_type && csignature->params [2]->type == param_type) {
			if (strcmp (tm, "FusedMultiplyAdd") == 0)
				*op = MINT_FMA;
		} else if (csignature->param_count == 2 && csignature->params [0]->type == param_type && csignature->params [1]->type == MONO_TYPE_I4 && strcmp (tm, "ScaleB") == 0) {
			*op = MINT_SCALEB;
		}

		if (*op != -1 && is_float) {
			*op = *op + (MINT_ABSF - MINT_ABS);
		}
	} else if (in_corlib && !strcmp (klass_name_space, "System") && (!strcmp (klass_name, "Span`1") || !strcmp (klass_name, "ReadOnlySpan`1"))) {
		if (!strcmp (tm, "get_Item")) {
			MonoGenericClass *gclass = mono_class_get_generic_class (target_method->klass);
			MonoClass *param_class = mono_class_from_mono_type_internal (gclass->context.class_inst->type_argv [0]);

			if (!mini_is_gsharedvt_variable_klass (param_class)) {
				MonoClassField *length_field = mono_class_get_field_from_name_full (target_method->klass, "_length", NULL);
				g_assert (length_field);
				int offset_length = length_field->offset - sizeof (MonoObject);

				MonoClassField *ptr_field = mono_class_get_field_from_name_full (target_method->klass, "_pointer", NULL);
				g_assert (ptr_field);
				int offset_pointer = ptr_field->offset - sizeof (MonoObject);

				int size = mono_class_array_element_size (param_class);
				interp_add_ins (td, MINT_GETITEM_SPAN);
				td->last_ins->data [0] = size;
				td->last_ins->data [1] = offset_length;
				td->last_ins->data [2] = offset_pointer;

				SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_MP);
				td->sp -= 1;
				td->ip += 5;
				return TRUE;
			}
		} else if (!strcmp (tm, "get_Length")) {
			MonoClassField *length_field = mono_class_get_field_from_name_full (target_method->klass, "_length", NULL);
			g_assert (length_field);
			int offset_length = length_field->offset - sizeof (MonoObject);
			interp_add_ins (td, MINT_LDLEN_SPAN);
			td->last_ins->data [0] = offset_length;
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
			td->ip += 5;
			return TRUE;
		}
	} else if (((in_corlib && !strcmp (klass_name_space, "Internal.Runtime.CompilerServices"))
				|| !strcmp (klass_name_space, "System.Runtime.CompilerServices"))
			   && !strcmp (klass_name, "Unsafe")) {
#ifdef ENABLE_NETCORE
		if (!strcmp (tm, "AddByteOffset"))
			*op = MINT_INTRINS_UNSAFE_ADD_BYTE_OFFSET;
		else if (!strcmp (tm, "ByteOffset"))
			*op = MINT_INTRINS_UNSAFE_BYTE_OFFSET;
		else if (!strcmp (tm, "As") || !strcmp (tm, "AsRef"))
			*op = MINT_NOP;
		else if (!strcmp (tm, "AsPointer")) {
			/* NOP */
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_MP);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "IsAddressLessThan")) {
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);

			MonoClass *k = mono_defaults.boolean_class;
			interp_add_ins (td, MINT_CLT_UN_P);
			td->sp -= 1;
			SET_TYPE (td->sp - 1, stack_type [mint_type (m_class_get_byval_arg (k))], k);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "SizeOf")) {
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);
			MonoType *t = ctx->method_inst->type_argv [0];
			int align;
			int esize = mono_type_size (t, &align);
			interp_add_ins (td, MINT_LDC_I4);
			WRITE32_INS (td->last_ins, 0, &esize);
			PUSH_SIMPLE_TYPE (td, STACK_TYPE_I4);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "AreSame")) {
			*op = MINT_CEQ_P;
		} else if (!strcmp (tm, "SkipInit")) {
			*op = MINT_POP;
		} else if (!strcmp (tm, "InitBlockUnaligned")) {
			*op = MINT_INITBLK;
		}
#endif
	} else if (in_corlib && !strcmp (klass_name_space, "System.Runtime.CompilerServices") && !strcmp (klass_name, "RuntimeHelpers")) {
#ifdef ENABLE_NETCORE
		if (!strcmp (tm, "get_OffsetToStringData")) {
			g_assert (csignature->param_count == 0);
			int offset = MONO_STRUCT_OFFSET (MonoString, chars);
			interp_add_ins (td, MINT_LDC_I4);
			WRITE32_INS (td->last_ins, 0, &offset);
			PUSH_SIMPLE_TYPE (td, STACK_TYPE_I4);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "IsBitwiseEquatable")) {
			g_assert (csignature->param_count == 0);
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);
			MonoType *t = mini_get_underlying_type (ctx->method_inst->type_argv [0]);

			if (MONO_TYPE_IS_PRIMITIVE (t) && t->type != MONO_TYPE_R4 && t->type != MONO_TYPE_R8)
				*op = MINT_LDC_I4_1;
			else
				*op = MINT_LDC_I4_0;
		} else if (!strcmp (tm, "ObjectHasComponentSize")) {
			*op = MINT_INTRINS_RUNTIMEHELPERS_OBJECT_HAS_COMPONENT_SIZE;
		} else if (!strcmp (tm, "IsReferenceOrContainsReferences")) {
			g_assert (csignature->param_count == 0);
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);
			MonoType *t = mini_get_underlying_type (ctx->method_inst->type_argv [0]);

			gboolean has_refs;

			MonoClass *klass = mono_class_from_mono_type_internal (t);
			mono_class_init_internal (klass);
			if (MONO_TYPE_IS_REFERENCE (t))
				has_refs = TRUE;
			else if (MONO_TYPE_IS_PRIMITIVE (t))
				has_refs = FALSE;
			else
				has_refs = m_class_has_references (klass);

			*op = has_refs ? MINT_LDC_I4_1 : MINT_LDC_I4_0;
		}
#endif
	} else if (in_corlib && !strcmp (klass_name_space, "System") && !strcmp (klass_name, "RuntimeMethodHandle") && !strcmp (tm, "GetFunctionPointer") && csignature->param_count == 1) {
		// We must intrinsify this method on interp so we don't return a pointer to native code entering interpreter
		*op = MINT_LDFTN_DYNAMIC;
	} else if (in_corlib && target_method->klass == mono_defaults.systemtype_class && !strcmp (target_method->name, "op_Equality")) {
		*op = MINT_CEQ_P;
	} else if (in_corlib && target_method->klass == mono_defaults.object_class) {
		if (!strcmp (tm, "InternalGetHashCode"))
			*op = MINT_INTRINS_GET_HASHCODE;
		else if (!strcmp (tm, "GetType")
#ifndef DISABLE_REMOTING
			// Invoking GetType via reflection on proxies has some special semantics
			// See InterfaceProxyGetTypeViaReflectionOkay corlib test
			&& td->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE
#endif
				)
			*op = MINT_INTRINS_GET_TYPE;
#ifdef ENABLE_NETCORE
		else if (!strcmp (tm, "GetRawData")) {
#if SIZEOF_VOID_P == 8
			interp_add_ins (td, MINT_LDC_I8_S);
#else
			interp_add_ins (td, MINT_LDC_I4_S);
#endif
			td->last_ins->data [0] = (gint16) MONO_ABI_SIZEOF (MonoObject);

			interp_add_ins (td, MINT_ADD_P);
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_MP);

			td->ip += 5;
			return TRUE;
		}
#endif
	} else if (in_corlib && target_method->klass == mono_defaults.enum_class && !strcmp (tm, "HasFlag")) {
		gboolean intrinsify = FALSE;
		MonoClass *base_klass = NULL;
		if (td->last_ins && td->last_ins->opcode == MINT_BOX &&
				td->last_ins->prev && interp_ins_is_ldc (td->last_ins->prev) &&
				td->last_ins->prev->prev && td->last_ins->prev->prev->opcode == MINT_BOX &&
				td->sp [-2].klass == td->sp [-1].klass &&
				!interp_is_bb_start (td, td->last_ins->prev->prev, NULL) &&
				!td->is_bb_start [td->in_start - td->il_code]) {
			// csc pattern : box, ldc, box, call HasFlag
			g_assert (m_class_is_enumtype (td->sp [-2].klass));
			MonoType *base_type = mono_type_get_underlying_type (m_class_get_byval_arg (td->sp [-2].klass));
			base_klass = mono_class_from_mono_type_internal (base_type);

			// Remove the boxing of valuetypes
			interp_clear_ins (td, td->last_ins->prev->prev);
			interp_clear_ins (td, td->last_ins);

			intrinsify = TRUE;
		} else if (td->last_ins && td->last_ins->opcode == MINT_BOX &&
				td->last_ins->prev && interp_ins_is_ldc (td->last_ins->prev) &&
				constrained_class && td->sp [-1].klass == constrained_class &&
				!interp_is_bb_start (td, td->last_ins->prev, NULL) &&
				!td->is_bb_start [td->in_start - td->il_code]) {
			// mcs pattern : ldc, box, constrained Enum, call HasFlag
			g_assert (m_class_is_enumtype (constrained_class));
			MonoType *base_type = mono_type_get_underlying_type (m_class_get_byval_arg (constrained_class));
			base_klass = mono_class_from_mono_type_internal (base_type);
			int mt = mint_type (m_class_get_byval_arg (base_klass));

			// Remove boxing and load the value of this
			interp_clear_ins (td, td->last_ins);
			interp_insert_ins (td, td->last_ins->prev->prev, interp_get_ldind_for_mt (mt));

			intrinsify = TRUE;
		}
		if (intrinsify) {
			interp_add_ins (td, MINT_INTRINS_ENUM_HASFLAG);
			td->last_ins->data [0] = get_data_item_index (td, base_klass);
			td->sp -= 2;
			PUSH_SIMPLE_TYPE (td, STACK_TYPE_I4);
			td->ip += 5;
			return TRUE;
		}
	} else if (in_corlib && !strcmp (klass_name_space, "System.Threading") && !strcmp (klass_name, "Interlocked")) {
		if (!strcmp (tm, "MemoryBarrier") && csignature->param_count == 0)
			*op = MINT_MONO_MEMORY_BARRIER;
	} else if (in_corlib && !strcmp (klass_name_space, "System.Threading") && !strcmp (klass_name, "Thread")) {
		if (!strcmp (tm, "MemoryBarrier") && csignature->param_count == 0)
			*op = MINT_MONO_MEMORY_BARRIER;
	} else if (in_corlib &&
			!strcmp (klass_name_space, "System.Runtime.CompilerServices") &&
			!strcmp (klass_name, "JitHelpers") &&
			(!strcmp (tm, "EnumEquals") || !strcmp (tm, "EnumCompareTo"))) {
		MonoGenericContext *ctx = mono_method_get_context (target_method);
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (csignature->param_count == 2);

		MonoType *t = ctx->method_inst->type_argv [0];
		t = mini_get_underlying_type (t);

		gboolean is_i8 = (t->type == MONO_TYPE_I8 || t->type == MONO_TYPE_U8);
		gboolean is_unsigned = (t->type == MONO_TYPE_U1 || t->type == MONO_TYPE_U2 || t->type == MONO_TYPE_U4 || t->type == MONO_TYPE_U8 || t->type == MONO_TYPE_U);

		gboolean is_compareto = strcmp (tm, "EnumCompareTo") == 0;
		if (is_compareto) {
			int locala, localb;
			locala = create_interp_local (td, t);
			localb = create_interp_local (td, t);

			// Save arguments
			store_local (td, localb);
			store_local (td, locala);
			// (a > b)
			load_local (td, locala);
			load_local (td, localb);
			if (is_unsigned)
				interp_add_ins (td, is_i8 ? MINT_CGT_UN_I8 : MINT_CGT_UN_I4);
			else
				interp_add_ins (td, is_i8 ? MINT_CGT_I8 : MINT_CGT_I4);
			td->sp --;
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
			// (a < b)
			load_local (td, locala);
			load_local (td, localb);
			if (is_unsigned)
				interp_add_ins (td, is_i8 ? MINT_CLT_UN_I8 : MINT_CLT_UN_I4);
			else
				interp_add_ins (td, is_i8 ? MINT_CLT_I8 : MINT_CLT_I4);
			td->sp --;
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
			// (a > b) - (a < b)
			interp_add_ins (td, MINT_SUB_I4);
			td->sp --;
			td->ip += 5;
			return TRUE;
		} else {
			if (is_i8) {
				*op = MINT_CEQ_I8;
			} else {
				*op = MINT_CEQ_I4;
			}
		}
	}
#ifdef ENABLE_NETCORE
	else if (in_corlib &&
			   !strcmp ("System.Runtime.CompilerServices", klass_name_space) &&
			   !strcmp ("RuntimeFeature", klass_name)) {
		if (!strcmp (tm, "get_IsDynamicCodeSupported"))
			*op = MINT_LDC_I4_1;
		else if (!strcmp (tm, "get_IsDynamicCodeCompiled"))
			*op = MINT_LDC_I4_0;
	} else if (in_corlib &&
			!strncmp ("System.Runtime.Intrinsics", klass_name_space, 25) &&
			!strcmp (tm, "get_IsSupported")) {
		*op = MINT_LDC_I4_0;
	}
#endif

	return FALSE;
}

static MonoMethod*
interp_transform_internal_calls (MonoMethod *method, MonoMethod *target_method, MonoMethodSignature *csignature, gboolean is_virtual)
{
	if (method->wrapper_type == MONO_WRAPPER_NONE && target_method != NULL) {
		if (target_method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
			target_method = mono_marshal_get_native_wrapper (target_method, FALSE, FALSE);
		if (!is_virtual && target_method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
			target_method = mono_marshal_get_synchronized_wrapper (target_method);

		if (target_method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL && !is_virtual && !mono_class_is_marshalbyref (target_method->klass) && m_class_get_rank (target_method->klass) == 0)
			target_method = mono_marshal_get_native_wrapper (target_method, FALSE, FALSE);
	}
	return target_method;
}

static gboolean
interp_type_as_ptr (MonoType *tp)
{
	if (MONO_TYPE_IS_POINTER (tp))
		return TRUE;
	if (MONO_TYPE_IS_REFERENCE (tp))
		return TRUE;
	if ((tp)->type == MONO_TYPE_I4)
		return TRUE;
#if SIZEOF_VOID_P == 8
	if ((tp)->type == MONO_TYPE_I8)
		return TRUE;
#endif
	if ((tp)->type == MONO_TYPE_BOOLEAN)
		return TRUE;
	if ((tp)->type == MONO_TYPE_CHAR)
		return TRUE;
	if ((tp)->type == MONO_TYPE_VALUETYPE && m_class_is_enumtype (tp->data.klass))
		return TRUE;
	return FALSE;
}

#define INTERP_TYPE_AS_PTR(tp) interp_type_as_ptr (tp)

static int
interp_icall_op_for_sig (MonoMethodSignature *sig)
{
	int op = -1;
	switch (sig->param_count) {
	case 0:
		if (MONO_TYPE_IS_VOID (sig->ret))
			op = MINT_ICALL_V_V;
		else if (INTERP_TYPE_AS_PTR (sig->ret))
			op = MINT_ICALL_V_P;
		break;
	case 1:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]))
				op = MINT_ICALL_P_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]))
				op = MINT_ICALL_P_P;
		}
		break;
	case 2:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]))
				op = MINT_ICALL_PP_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]))
				op = MINT_ICALL_PP_P;
		}
		break;
	case 3:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]))
				op = MINT_ICALL_PPP_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]))
				op = MINT_ICALL_PPP_P;
		}
		break;
	case 4:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]))
				op = MINT_ICALL_PPPP_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]))
				op = MINT_ICALL_PPPP_P;
		}
		break;
	case 5:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]) &&
					INTERP_TYPE_AS_PTR (sig->params [4]))
				op = MINT_ICALL_PPPPP_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]) &&
					INTERP_TYPE_AS_PTR (sig->params [4]))
				op = MINT_ICALL_PPPPP_P;
		}
		break;
	case 6:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]) &&
					INTERP_TYPE_AS_PTR (sig->params [4]) &&
					INTERP_TYPE_AS_PTR (sig->params [5]))
				op = MINT_ICALL_PPPPPP_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]) &&
					INTERP_TYPE_AS_PTR (sig->params [4]) &&
					INTERP_TYPE_AS_PTR (sig->params [5]))
				op = MINT_ICALL_PPPPPP_P;
		}
		break;
	}
	return op;
}

#define INLINE_LENGTH_LIMIT 20

static gboolean
interp_method_check_inlining (TransformData *td, MonoMethod *method, MonoMethodSignature *csignature)
{
	MonoMethodHeaderSummary header;

	if (method->flags & METHOD_ATTRIBUTE_REQSECOBJ)
		/* Used to mark methods containing StackCrawlMark locals */
		return FALSE;

	if (csignature->call_convention == MONO_CALL_VARARG)
		return FALSE;

	if (!mono_method_get_header_summary (method, &header))
		return FALSE;

	/*runtime, icall and pinvoke are checked by summary call*/
	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_NOINLINING) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) ||
	    (mono_class_is_marshalbyref (method->klass)) ||
	    header.has_clauses)
		return FALSE;

	if (header.code_size >= INLINE_LENGTH_LIMIT && !(method->iflags & METHOD_IMPL_ATTRIBUTE_AGGRESSIVE_INLINING))
		return FALSE;

	if (mono_class_needs_cctor_run (method->klass, NULL)) {
		MonoVTable *vtable;
		ERROR_DECL (error);
		if (!m_class_get_runtime_info (method->klass))
			/* No vtable created yet */
			return FALSE;
		vtable = mono_class_vtable_checked (td->rtm->domain, method->klass, error);
		if (!is_ok (error)) {
			mono_error_cleanup (error);
			return FALSE;
		}
		if (!vtable->initialized)
			return FALSE;
	}

	/* We currently access at runtime the wrapper data */
	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;

	/* Our usage of `emit_store_value_as_local ()` for nint, nuint and nfloat
	 * is kinda hacky, and doesn't work with the inliner */
	if (mono_class_get_magic_index (method->klass) >= 0)
		return FALSE;

	if (td->prof_coverage)
		return FALSE;

	if (g_list_find (td->dont_inline, method))
		return FALSE;

	return TRUE;
}

static gboolean
interp_inline_method (TransformData *td, MonoMethod *target_method, MonoMethodHeader *header, MonoError *error)
{
	const unsigned char *prev_ip, *prev_il_code, *prev_in_start;
	int *prev_in_offsets;
	gboolean ret;
	unsigned int prev_max_stack_height, prev_max_vt_sp, prev_locals_size;
	int prev_n_data_items;
	int i, prev_vt_sp;
	int prev_sp_offset;
	MonoGenericContext *generic_context = NULL;
	StackInfo *prev_param_area;
	MonoMethod *prev_inlined_method;
	MonoMethodSignature *csignature = mono_method_signature_internal (target_method);
	int nargs = csignature->param_count + !!csignature->hasthis;
	InterpInst *prev_last_ins;

	if (csignature->is_inflated)
		generic_context = mono_method_get_context (target_method);
	else {
		MonoGenericContainer *generic_container = mono_method_get_generic_container (target_method);
		if (generic_container)
			generic_context = &generic_container->context;
	}

	prev_ip = td->ip;
	prev_il_code = td->il_code;
	prev_in_start = td->in_start;
	prev_sp_offset = td->sp - td->stack;
	prev_vt_sp = td->vt_sp;
	prev_inlined_method = td->inlined_method;
	prev_last_ins = td->last_ins;
	td->inlined_method = target_method;

	prev_max_stack_height = td->max_stack_height;
	prev_max_vt_sp = td->max_vt_sp;
	prev_locals_size = td->locals_size;

	prev_n_data_items = td->n_data_items;
	prev_in_offsets = td->in_offsets;
	td->in_offsets = (int*)g_malloc0((header->code_size + 1) * sizeof(int));

	/* Inlining pops the arguments, restore the stack */
	prev_param_area = (StackInfo*)g_malloc (nargs * sizeof (StackInfo));
	memcpy (prev_param_area, &td->sp [-nargs], nargs * sizeof (StackInfo));

	int const prev_code_size = td->code_size;
	td->code_size = header->code_size;

	if (td->verbose_level)
		g_print ("Inline start method %s.%s\n", m_class_get_name (target_method->klass), target_method->name);
	ret = generate_code (td, target_method, header, generic_context, error);

	if (!ret) {
		if (td->verbose_level)
			g_print ("Inline aborted method %s.%s\n", m_class_get_name (target_method->klass), target_method->name);
		td->max_stack_height = prev_max_stack_height;
		td->max_vt_sp = prev_max_vt_sp;
		td->locals_size = prev_locals_size;


		/* Remove any newly added items */
		for (i = prev_n_data_items; i < td->n_data_items; i++) {
			g_hash_table_remove (td->data_hash, td->data_items [i]);
		}
		td->n_data_items = prev_n_data_items;
		td->sp = td->stack + prev_sp_offset;
		memcpy (&td->sp [-nargs], prev_param_area, nargs * sizeof (StackInfo));
		td->vt_sp = prev_vt_sp;
		td->last_ins = prev_last_ins;
		if (td->last_ins)
			td->last_ins->next = NULL;
		UnlockedIncrement (&mono_interp_stats.inline_failures);
	} else {
		if (td->verbose_level)
			g_print ("Inline end method %s.%s\n", m_class_get_name (target_method->klass), target_method->name);
		UnlockedIncrement (&mono_interp_stats.inlined_methods);
		// Make sure we have an IR instruction associated with the now removed IL CALL
		// FIXME This could be prettier. We might be able to make inlining saner now that
		// that we can easily tweak the instruction list.
		if (!prev_inlined_method) {
			if (prev_last_ins) {
				if (prev_last_ins->next)
					prev_last_ins->next->il_offset = prev_in_start - prev_il_code;
			} else if (td->first_ins) {
				td->first_ins->il_offset = prev_in_start - prev_il_code;
			}
		}
	}

	td->ip = prev_ip;
	td->in_start = prev_in_start;
	td->il_code = prev_il_code;
	td->inlined_method = prev_inlined_method;
	td->code_size = prev_code_size;

	g_free (td->in_offsets);
	td->in_offsets = prev_in_offsets;

	g_free (prev_param_area);
	return ret;
}

static void
interp_constrained_box (TransformData *td, MonoDomain *domain, MonoClass *constrained_class, MonoMethodSignature *csignature, MonoError *error)
{
	int mt = mint_type (m_class_get_byval_arg (constrained_class));
	if (mono_class_is_nullable (constrained_class)) {
		g_assert (mt == MINT_TYPE_VT);
		interp_add_ins (td, MINT_BOX_NULLABLE);
		td->last_ins->data [0] = get_data_item_index (td, constrained_class);
		td->last_ins->data [1] = csignature->param_count;
		td->last_ins->data [2] = 1;
	} else {
		MonoVTable *vtable = mono_class_vtable_checked (domain, constrained_class, error);
		return_if_nok (error);

		if (mt == MINT_TYPE_VT) {
			interp_add_ins (td, MINT_BOX_VT);
			td->last_ins->data [0] = get_data_item_index (td, vtable);
			td->last_ins->data [1] = csignature->param_count;
			td->last_ins->data [2] = 1;
		} else {
			interp_add_ins (td, MINT_BOX);
			td->last_ins->data [0] = get_data_item_index (td, vtable);
			td->last_ins->data [1] = csignature->param_count;
		}
	}
}

static MonoMethod*
interp_get_method (MonoMethod *method, guint32 token, MonoImage *image, MonoGenericContext *generic_context, MonoError *error)
{
	if (method->wrapper_type == MONO_WRAPPER_NONE)
		return mono_get_method_checked (image, token, NULL, generic_context, error);
	else
		return (MonoMethod *)mono_method_get_wrapper_data (method, token);
}

/* Return FALSE if error, including inline failure */
static gboolean
interp_transform_call (TransformData *td, MonoMethod *method, MonoMethod *target_method, MonoDomain *domain, MonoGenericContext *generic_context, unsigned char *is_bb_start, MonoClass *constrained_class, gboolean readonly, MonoError *error, gboolean check_visibility, gboolean save_last_error)
{
	MonoImage *image = m_class_get_image (method->klass);
	MonoMethodSignature *csignature;
	int is_virtual = *td->ip == CEE_CALLVIRT;
	int calli = *td->ip == CEE_CALLI || *td->ip == CEE_MONO_CALLI_EXTRA_ARG;
	int i;
	guint32 vt_stack_used = 0;
	guint32 vt_res_size = 0;
	int op = -1;
	int native = 0;
	int need_null_check = is_virtual;
	gboolean is_delegate_invoke = FALSE;

	guint32 token = read32 (td->ip + 1);

	if (target_method == NULL) {
		if (calli) {
			CHECK_STACK(td, 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				csignature = (MonoMethodSignature *)mono_method_get_wrapper_data (method, token);
			else {
				csignature = mono_metadata_parse_signature_checked (image, token, error);
				return_val_if_nok (error, FALSE);
			}

			if (generic_context) {
				csignature = mono_inflate_generic_signature (csignature, generic_context, error);
				return_val_if_nok (error, FALSE);
			}

			/*
			 * The compiled interp entry wrapper is passed to runtime_invoke instead of
			 * the InterpMethod pointer. FIXME
			 */
			native = csignature->pinvoke || method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE;

			target_method = NULL;
		} else {
			target_method = interp_get_method (method, token, image, generic_context, error);
			return_val_if_nok (error, FALSE);
			csignature = mono_method_signature_internal (target_method);

			if (generic_context) {
				csignature = mono_inflate_generic_signature (csignature, generic_context, error);
				return_val_if_nok (error, FALSE);
				target_method = mono_class_inflate_generic_method_checked (target_method, generic_context, error);
				return_val_if_nok (error, FALSE);
			}
		}
	} else {
		csignature = mono_method_signature_internal (target_method);
	}

	if (check_visibility && target_method && !mono_method_can_access_method (method, target_method))
		interp_generate_mae_throw (td, method, target_method);

	if (target_method && target_method->string_ctor) {
		/* Create the real signature */
		MonoMethodSignature *ctor_sig = mono_metadata_signature_dup_mempool (td->mempool, csignature);
		ctor_sig->ret = m_class_get_byval_arg (mono_defaults.string_class);

		csignature = ctor_sig;
	}

	/* Intrinsics */
	if (target_method && interp_handle_intrinsics (td, target_method, constrained_class, csignature, readonly, &op))
		return TRUE;

	if (constrained_class) {
		if (m_class_is_enumtype (constrained_class) && !strcmp (target_method->name, "GetHashCode")) {
			/* Use the corresponding method from the base type to avoid boxing */
			MonoType *base_type = mono_class_enum_basetype_internal (constrained_class);
			g_assert (base_type);
			constrained_class = mono_class_from_mono_type_internal (base_type);
			target_method = mono_class_get_method_from_name_checked (constrained_class, target_method->name, 0, 0, error);
			mono_error_assert_ok (error);
			g_assert (target_method);
		}
	}

	if (constrained_class) {
		mono_class_setup_vtable (constrained_class);
		if (mono_class_has_failure (constrained_class)) {
			mono_error_set_for_class_failure (error, constrained_class);
			return FALSE;
		}
#if DEBUG_INTERP
		g_print ("CONSTRAINED.CALLVIRT: %s::%s.  %s (%p) ->\n", target_method->klass->name, target_method->name, mono_signature_full_name (target_method->signature), target_method);
#endif
		target_method = mono_get_method_constrained_with_method (image, target_method, constrained_class, generic_context, error);
#if DEBUG_INTERP
		g_print ("                    : %s::%s.  %s (%p)\n", target_method->klass->name, target_method->name, mono_signature_full_name (target_method->signature), target_method);
#endif
		/* Intrinsics: Try again, it could be that `mono_get_method_constrained_with_method` resolves to a method that we can substitute */
		if (target_method && interp_handle_intrinsics (td, target_method, constrained_class, csignature, readonly, &op))
			return TRUE;

		return_val_if_nok (error, FALSE);
		mono_class_setup_vtable (target_method->klass);

		if (!m_class_is_valuetype (constrained_class)) {
			/* managed pointer on the stack, we need to deref that puppy */
			interp_add_ins (td, MINT_LDIND_I);
			td->last_ins->data [0] = csignature->param_count;
		} else if (target_method->klass == mono_defaults.object_class || target_method->klass == m_class_get_parent (mono_defaults.enum_class) || target_method->klass == mono_defaults.enum_class) {
			/*
			 * Constrained expects a managed pointer that normally needs dereferencing.
			 * For value types that have their storage on the vtstack, a managed pointer
			 * to it is identical to the internal pointer that is passed on the stack
			 * when using the value type, not needing any dereferencing.
			 */
			int this_type = (td->sp - csignature->param_count - 1)->type;
			g_assert (this_type == STACK_TYPE_I || this_type == STACK_TYPE_MP);
			if (mint_type (m_class_get_byval_arg (constrained_class)) != MINT_TYPE_VT) {
				/* Always load the entire stackval, to handle also the case where the enum has long storage */
				interp_add_ins (td, MINT_LDIND_I8);
				td->last_ins->data [0] = csignature->param_count;
			}

			interp_constrained_box (td, domain, constrained_class, csignature, error);
			return_val_if_nok (error, FALSE);
		} else {
			if (target_method->klass != constrained_class) {
				/*
				 * The type parameter is instantiated as a valuetype,
				 * but that type doesn't override the method we're
				 * calling, so we need to box `this'.
				 */
				int this_type = (td->sp - csignature->param_count - 1)->type;
				g_assert (this_type == STACK_TYPE_I || this_type == STACK_TYPE_MP);
				if (mint_type (m_class_get_byval_arg (constrained_class)) != MINT_TYPE_VT) {
					/* managed pointer on the stack, we need to deref that puppy */
					/* Always load the entire stackval, to handle also the case where the enum has long storage */
					interp_add_ins (td, MINT_LDIND_I8);
					td->last_ins->data [0] = csignature->param_count;
				}

				interp_constrained_box (td, domain, constrained_class, csignature, error);
				return_val_if_nok (error, FALSE);
			}
			is_virtual = FALSE;
		}
	}

	if (target_method)
		mono_class_init_internal (target_method->klass);

	if (!is_virtual && target_method && (target_method->flags & METHOD_ATTRIBUTE_ABSTRACT)) {
		if (!mono_class_is_interface (method->klass))
			interp_generate_bie_throw (td);
		else
			is_virtual = TRUE;
	}

	if (is_virtual && target_method && (!(target_method->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
		(MONO_METHOD_IS_FINAL (target_method) &&
		 target_method->wrapper_type != MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK)) &&
		!(mono_class_is_marshalbyref (target_method->klass))) {
		/* Not really virtual, just needs a null check */
		is_virtual = FALSE;
		need_null_check = TRUE;
	}

	CHECK_STACK (td, csignature->param_count + csignature->hasthis);
	if (!td->gen_sdb_seq_points && !calli && op == -1 && (!is_virtual || (target_method->flags & METHOD_ATTRIBUTE_VIRTUAL) == 0) &&
		(target_method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) == 0 && 
		(target_method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) == 0 &&
		!(target_method->iflags & METHOD_IMPL_ATTRIBUTE_NOINLINING)) {
		(void)mono_class_vtable_checked (domain, target_method->klass, error);
		return_val_if_nok (error, FALSE);

		if (method == target_method && *(td->ip + 5) == CEE_RET && !(csignature->hasthis && m_class_is_valuetype (target_method->klass))) {
			if (td->inlined_method)
				return FALSE;

			if (td->verbose_level)
				g_print ("Optimize tail call of %s.%s\n", m_class_get_name (target_method->klass), target_method->name);

			for (i = csignature->param_count - 1 + !!csignature->hasthis; i >= 0; --i)
				store_arg (td, i);

			interp_add_ins (td, MINT_BR_S);
			// We are branching to the beginning of the method
			td->last_ins->data [0] = 0;
			if (!is_bb_start [td->ip + 5 - td->il_code])
				++td->ip; /* gobble the CEE_RET if it isn't branched to */				
			td->ip += 5;
			return TRUE;
		}
	}

	target_method = interp_transform_internal_calls (method, target_method, csignature, is_virtual);

	if (csignature->call_convention == MONO_CALL_VARARG) {
		csignature = mono_method_get_signature_checked (target_method, image, token, generic_context, error);
		int vararg_stack = 0;
		/*
		 * For vararg calls, ArgIterator expects the signature and the varargs to be
		 * stored in a linear memory. We allocate the necessary vt_stack space for
		 * this. All varargs will be pushed to the vt_stack at call site.
		 */
		vararg_stack += sizeof (gpointer);
		for (i = csignature->sentinelpos; i < csignature->param_count; ++i) {
			int align, arg_size;
			arg_size = mono_type_stack_size (csignature->params [i], &align);
			vararg_stack = ALIGN_TO (vararg_stack, align);
			vararg_stack += arg_size;
		}
		/*
		 * MINT_CALL_VARARG needs this space on the vt stack. Make sure the
		 * vtstack space is sufficient.
		 */
		PUSH_VT (td, vararg_stack);
		POP_VT (td, vararg_stack);
	}

	if (need_null_check) {
		interp_add_ins (td, MINT_CKNULL_N);
		td->last_ins->data [0] = csignature->param_count + 1;
	}

	g_assert (csignature->call_convention != MONO_CALL_FASTCALL);
	if ((mono_interp_opt & INTERP_OPT_INLINE) && op == -1 && !is_virtual && target_method && interp_method_check_inlining (td, target_method, csignature)) {
		MonoMethodHeader *mheader = interp_method_get_header (target_method, error);
		return_val_if_nok (error, FALSE);

		if (interp_inline_method (td, target_method, mheader, error)) {
			td->ip += 5;
			return TRUE;
		}
	}

	/* Don't inline methods that do calls */
	if (op == -1 && td->inlined_method)
		return FALSE;

	/* We need to convert delegate invoke to a indirect call on the interp_invoke_impl field */
	if (target_method && m_class_get_parent (target_method->klass) == mono_defaults.multicastdelegate_class) {
		const char *name = target_method->name;
		if (*name == 'I' && (strcmp (name, "Invoke") == 0))
			is_delegate_invoke = TRUE;
	}

	/* Pop the function pointer */
	if (calli)
		--td->sp;

	td->sp -= csignature->param_count + !!csignature->hasthis;
	for (i = 0; i < csignature->param_count; ++i) {
		if (td->sp [i + !!csignature->hasthis].type == STACK_TYPE_VT) {
			gint32 size;
			MonoClass *klass = mono_class_from_mono_type_internal (csignature->params [i]);
			if (csignature->pinvoke && method->wrapper_type != MONO_WRAPPER_NONE)
				size = mono_class_native_size (klass, NULL);
			else
				size = mono_class_value_size (klass, NULL);
			size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
			vt_stack_used += size;
		}
	}
	/* Pop the vt stack used by the arguments */
	td->vt_sp -= vt_stack_used;

	/* need to handle typedbyref ... */
	if (csignature->ret->type != MONO_TYPE_VOID) {
		int mt = mint_type(csignature->ret);
		MonoClass *klass = mono_class_from_mono_type_internal (csignature->ret);
		if (mt == MINT_TYPE_VT) {
			if (csignature->pinvoke && method->wrapper_type != MONO_WRAPPER_NONE)
				vt_res_size = mono_class_native_size (klass, NULL);
			else
				vt_res_size = mono_class_value_size (klass, NULL);
			vt_res_size = ALIGN_TO (vt_res_size, MINT_VT_ALIGNMENT);
			if (mono_class_has_failure (klass)) {
				mono_error_set_for_class_failure (error, klass);
				return FALSE;
			}
			PUSH_VT(td, vt_res_size);
		}
		PUSH_TYPE(td, stack_type[mt], klass);
	}

	if (op >= 0) {
		interp_add_ins (td, op);

		if (op == MINT_LDLEN) {
#ifdef MONO_BIG_ARRAYS
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I8);
#else
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
#endif
		}

#ifndef ENABLE_NETCORE
		if (op == MINT_CALLRUN) {
			td->last_ins->data [0] = get_data_item_index (td, target_method);
			td->last_ins->data [1] = get_data_item_index (td, mono_method_signature_internal (target_method));
		}
#endif
	} else if (!calli && !is_delegate_invoke && !is_virtual && mono_interp_jit_call_supported (target_method, csignature)) {
		interp_add_ins (td, MINT_JIT_CALL);
		td->last_ins->data [0] = get_data_item_index (td, (void *)mono_interp_get_imethod (domain, target_method, error));
		mono_error_assert_ok (error);
		td->last_ins->data [1] = vt_stack_used;
	} else {
		if (is_delegate_invoke) {
			interp_add_ins (td, MINT_CALL_DELEGATE);
			td->last_ins->data [0] = get_data_item_index (td, (void *)csignature);
			td->last_ins->data [1] = vt_stack_used;
		} else if (calli) {
#ifndef MONO_ARCH_HAS_NO_PROPER_MONOCTX
			/* Try using fast icall path for simple signatures */
			if (native && !method->dynamic)
				op = interp_icall_op_for_sig (csignature);
#endif
			if (op != -1) {
				interp_add_ins (td, MINT_CALLI_NAT_FAST);
				td->last_ins->data [1] = op;
				td->last_ins->data [2] = save_last_error;
			} else if (native && method->dynamic && csignature->pinvoke) {
				interp_add_ins (td, MINT_CALLI_NAT_DYNAMIC);
				td->last_ins->data [1] = vt_stack_used;
			} else if (native) {
				interp_add_ins (td, MINT_CALLI_NAT);
#ifdef TARGET_X86
				/* Windows not tested/supported yet */
				g_assertf (csignature->call_convention == MONO_CALL_DEFAULT || csignature->call_convention == MONO_CALL_C, "Interpreter supports only cdecl pinvoke on x86");
#endif
				td->last_ins->data [1] = vt_stack_used;
				td->last_ins->data [2] = vt_res_size;
				td->last_ins->data [3] = save_last_error;
				/* Cache slot */
				td->last_ins->data [4] = get_data_item_index_nonshared (td, NULL);
			} else {
				interp_add_ins (td, MINT_CALLI);
				td->last_ins->data [1] = vt_stack_used;
			}
			td->last_ins->data [0] = get_data_item_index (td, (void *)csignature);
		} else {
			InterpMethod *imethod = mono_interp_get_imethod (domain, target_method, error);
			return_val_if_nok (error, FALSE);

			if (csignature->call_convention == MONO_CALL_VARARG) {
				interp_add_ins (td, MINT_CALL_VARARG);
				td->last_ins->data [1] = get_data_item_index (td, (void *)csignature);
			} else if (is_virtual && !mono_class_is_marshalbyref (target_method->klass)) {
				interp_add_ins (td, MINT_CALLVIRT_FAST);
				if (mono_class_is_interface (target_method->klass))
					td->last_ins->data [1] = -2 * MONO_IMT_SIZE + mono_method_get_imt_slot (target_method);
				else
					td->last_ins->data [1] = mono_method_get_vtable_slot (target_method);
			} else if (is_virtual) {
				interp_add_ins (td, MINT_CALLVIRT);
				td->last_ins->data [1] = imethod->param_count + imethod->hasthis;
			} else {
				interp_add_ins (td, MINT_CALL);
				td->last_ins->data [1] = imethod->param_count + imethod->hasthis;
			}
			td->last_ins->data [0] = get_data_item_index (td, (void *)imethod);
			td->last_ins->data [2] = vt_stack_used;

#ifdef ENABLE_EXPERIMENT_TIERED
			if (MINT_IS_PATCHABLE_CALL (td->last_ins->opcode)) {
				g_assert (!calli && !is_virtual);
				td->last_ins->flags |= INTERP_INST_FLAG_RECORD_CALL_PATCH;
				g_hash_table_insert (td->patchsite_hash, td->last_ins, target_method);
			}
#endif
		}
	}
	td->ip += 5;

	return TRUE;
}

static MonoClassField *
interp_field_from_token (MonoMethod *method, guint32 token, MonoClass **klass, MonoGenericContext *generic_context, MonoError *error)
{
	MonoClassField *field = NULL;
	if (method->wrapper_type != MONO_WRAPPER_NONE) {
		field = (MonoClassField *) mono_method_get_wrapper_data (method, token);
		*klass = field->parent;

		mono_class_setup_fields (field->parent);
	} else {
		field = mono_field_from_token_checked (m_class_get_image (method->klass), token, klass, generic_context, error);
		return_val_if_nok (error, NULL);
	}

	if (!method->skip_visibility && !mono_method_can_access_field (method, field)) {
		char *method_fname = mono_method_full_name (method, TRUE);
		char *field_fname = mono_field_full_name (field);
		mono_error_set_generic_error (error, "System", "FieldAccessException", "Field `%s' is inaccessible from method `%s'\n", field_fname, method_fname);
		g_free (method_fname);
		g_free (field_fname);
		return NULL;
	}

	return field;
}

static InterpBasicBlock*
get_bb (TransformData *td, InterpBasicBlock *cbb, unsigned char *ip)
{
	int offset = ip - td->il_code;
	InterpBasicBlock *bb = td->offset_to_bb [offset];

	if (!bb) {
		bb = (InterpBasicBlock*)mono_mempool_alloc0 (td->mempool, sizeof (InterpBasicBlock));
		bb->ip = ip;
		td->offset_to_bb [offset] = bb;

		td->basic_blocks = g_list_append_mempool (td->mempool, td->basic_blocks, bb);
	}

	if (cbb)
		bb->preds = g_slist_prepend_mempool (td->mempool, bb->preds, cbb);
	return bb;
}

/*
 * get_basic_blocks:
 *
 *   Compute the set of IL level basic blocks.
 */
static void
get_basic_blocks (TransformData *td)
{
	guint8 *start = (guint8*)td->il_code;
	guint8 *end = (guint8*)td->il_code + td->code_size;
	guint8 *ip = start;
	unsigned char *target;
	int i;
	guint cli_addr;
	const MonoOpcode *opcode;
	InterpBasicBlock *cbb;

	td->offset_to_bb = (InterpBasicBlock**)mono_mempool_alloc0 (td->mempool, sizeof (InterpBasicBlock*) * (end - start + 1));
	td->entry_bb = cbb = get_bb (td, NULL, start);

	while (ip < end) {
		cli_addr = ip - start;
		td->offset_to_bb [cli_addr] = cbb;
		i = mono_opcode_value ((const guint8 **)&ip, end);
		opcode = &mono_opcodes [i];
		switch (opcode->argument) {
		case MonoInlineNone:
			ip++;
			break;
		case MonoInlineString:
		case MonoInlineType:
		case MonoInlineField:
		case MonoInlineMethod:
		case MonoInlineTok:
		case MonoInlineSig:
		case MonoShortInlineR:
		case MonoInlineI:
			ip += 5;
			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
			ip += 2;
			break;
		case MonoShortInlineBrTarget:
			target = start + cli_addr + 2 + (signed char)ip [1];
			get_bb (td, cbb, target);
			ip += 2;
			cbb = get_bb (td, cbb, ip);
			break;
		case MonoInlineBrTarget:
			target = start + cli_addr + 5 + (gint32)read32 (ip + 1);
			get_bb (td, cbb, target);
			ip += 5;
			cbb = get_bb (td, cbb, ip);
			break;
		case MonoInlineSwitch: {
			guint32 n = read32 (ip + 1);
			guint32 j;
			ip += 5;
			cli_addr += 5 + 4 * n;
			target = start + cli_addr;
			get_bb (td, cbb, target);

			for (j = 0; j < n; ++j) {
				target = start + cli_addr + (gint32)read32 (ip);
				get_bb (td, cbb, target);
				ip += 4;
			}
			cbb = get_bb (td, cbb, ip);
			break;
		}
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		default:
			g_assert_not_reached ();
		}

		if (i == CEE_THROW)
			cbb = get_bb (td, NULL, ip);
	}
}

static void
interp_save_debug_info (InterpMethod *rtm, MonoMethodHeader *header, TransformData *td, GArray *line_numbers)
{
	MonoDebugMethodJitInfo *dinfo;
	int i;

	if (!mono_debug_enabled ())
		return;

	/*
	 * We save the debug info in the same way the JIT does it, treating the interpreter IR as the native code.
	 */

	dinfo = g_new0 (MonoDebugMethodJitInfo, 1);
	dinfo->num_params = rtm->param_count;
	dinfo->params = g_new0 (MonoDebugVarInfo, dinfo->num_params);
	dinfo->num_locals = header->num_locals;
	dinfo->locals = g_new0 (MonoDebugVarInfo, header->num_locals);
	dinfo->code_start = (guint8*)rtm->code;
	dinfo->code_size = td->new_code_end - td->new_code;
	dinfo->epilogue_begin = 0;
	dinfo->has_var_info = TRUE;
	dinfo->num_line_numbers = line_numbers->len;
	dinfo->line_numbers = g_new0 (MonoDebugLineNumberEntry, dinfo->num_line_numbers);

	for (i = 0; i < dinfo->num_params; i++) {
		MonoDebugVarInfo *var = &dinfo->params [i];
		var->type = rtm->param_types [i];
	}
	for (i = 0; i < dinfo->num_locals; i++) {
		MonoDebugVarInfo *var = &dinfo->locals [i];
		var->type = mono_metadata_type_dup (NULL, header->locals [i]);
	}

	for (i = 0; i < dinfo->num_line_numbers; i++)
		dinfo->line_numbers [i] = g_array_index (line_numbers, MonoDebugLineNumberEntry, i);
	mono_debug_add_method (rtm->method, dinfo, rtm->domain);

	mono_debug_free_method_jit_info (dinfo);
}

/* Same as the code in seq-points.c */
static void
insert_pred_seq_point (SeqPoint *last_sp, SeqPoint *sp, GSList **next)
{
	GSList *l;
	int src_index = last_sp->next_offset;
	int dst_index = sp->next_offset;

	/* bb->in_bb might contain duplicates */
	for (l = next [src_index]; l; l = l->next)
		if (GPOINTER_TO_UINT (l->data) == dst_index)
			break;
	if (!l)
		next [src_index] = g_slist_append (next [src_index], GUINT_TO_POINTER (dst_index));
}

static void
recursively_make_pred_seq_points (TransformData *td, InterpBasicBlock *bb)
{
	SeqPoint ** const MONO_SEQ_SEEN_LOOP = (SeqPoint**)GINT_TO_POINTER(-1);
	GSList *l;

	GArray *predecessors = g_array_new (FALSE, TRUE, sizeof (gpointer));
	GHashTable *seen = g_hash_table_new_full (g_direct_hash, NULL, NULL, NULL);

	// Insert/remove sentinel into the memoize table to detect loops containing bb
	bb->pred_seq_points = MONO_SEQ_SEEN_LOOP;

	for (l = bb->preds; l; l = l->next) {
		InterpBasicBlock *in_bb = (InterpBasicBlock*)l->data;

		// This bb has the last seq point, append it and continue
		if (in_bb->last_seq_point != NULL) {
			predecessors = g_array_append_val (predecessors, in_bb->last_seq_point);
			continue;
		}

		// We've looped or handled this before, exit early.
		// No last sequence points to find.
		if (in_bb->pred_seq_points == MONO_SEQ_SEEN_LOOP)
			continue;

		// Take sequence points from incoming basic blocks

		if (in_bb == td->entry_bb)
			continue;

		if (in_bb->pred_seq_points == NULL)
			recursively_make_pred_seq_points (td, in_bb);

		// Union sequence points with incoming bb's
		for (int i=0; i < in_bb->num_pred_seq_points; i++) {
			if (!g_hash_table_lookup (seen, in_bb->pred_seq_points [i])) {
				g_array_append_val (predecessors, in_bb->pred_seq_points [i]);
				g_hash_table_insert (seen, in_bb->pred_seq_points [i], (gpointer)&MONO_SEQ_SEEN_LOOP);
			}
		}
		// predecessors = g_array_append_vals (predecessors, in_bb->pred_seq_points, in_bb->num_pred_seq_points);
	}

	g_hash_table_destroy (seen);

	if (predecessors->len != 0) {
		bb->pred_seq_points = (SeqPoint**)mono_mempool_alloc0 (td->mempool, sizeof (SeqPoint *) * predecessors->len);
		bb->num_pred_seq_points = predecessors->len;

		for (int newer = 0; newer < bb->num_pred_seq_points; newer++) {
			bb->pred_seq_points [newer] = (SeqPoint*)g_array_index (predecessors, gpointer, newer);
		}
	}

	g_array_free (predecessors, TRUE);
}

static void
collect_pred_seq_points (TransformData *td, InterpBasicBlock *bb, SeqPoint *seqp, GSList **next)
{
	// Doesn't have a last sequence point, must find from incoming basic blocks
	if (bb->pred_seq_points == NULL && bb != td->entry_bb)
		recursively_make_pred_seq_points (td, bb);

	for (int i = 0; i < bb->num_pred_seq_points; i++)
		insert_pred_seq_point (bb->pred_seq_points [i], seqp, next);

	return;
}

static void
save_seq_points (TransformData *td, MonoJitInfo *jinfo)
{
	GByteArray *array;
	int i, seq_info_size;
	MonoSeqPointInfo *info;
	GSList **next = NULL;
	GList *bblist;

	if (!td->gen_sdb_seq_points)
		return;

	/*
	 * For each sequence point, compute the list of sequence points immediately
	 * following it, this is needed to implement 'step over' in the debugger agent.
	 * Similar to the code in mono_save_seq_point_info ().
	 */
	for (i = 0; i < td->seq_points->len; ++i) {
		SeqPoint *sp = (SeqPoint*)g_ptr_array_index (td->seq_points, i);

		/* Store the seq point index here temporarily */
		sp->next_offset = i;
	}
	next = (GSList**)mono_mempool_alloc0 (td->mempool, sizeof (GList*) * td->seq_points->len);
	for (bblist = td->basic_blocks; bblist; bblist = bblist->next) {
		InterpBasicBlock *bb = (InterpBasicBlock*)bblist->data;

		GSList *bb_seq_points = g_slist_reverse (bb->seq_points);
		SeqPoint *last = NULL;
		for (GSList *l = bb_seq_points; l; l = l->next) {
			SeqPoint *sp = (SeqPoint*)l->data;

			if (sp->il_offset == METHOD_ENTRY_IL_OFFSET || sp->il_offset == METHOD_EXIT_IL_OFFSET)
				/* Used to implement method entry/exit events */
				continue;

			if (last != NULL) {
				/* Link with the previous seq point in the same bb */
				next [last->next_offset] = g_slist_append_mempool (td->mempool, next [last->next_offset], GINT_TO_POINTER (sp->next_offset));
			} else {
				/* Link with the last bb in the previous bblocks */
				collect_pred_seq_points (td, bb, sp, next);
			}
			last = sp;
		}
	}

	/* Serialize the seq points into a byte array */
	array = g_byte_array_new ();
	SeqPoint zero_seq_point = {0};
	SeqPoint* last_seq_point = &zero_seq_point;
	for (i = 0; i < td->seq_points->len; ++i) {
		SeqPoint *sp = (SeqPoint*)g_ptr_array_index (td->seq_points, i);

		sp->next_offset = 0;
		if (mono_seq_point_info_add_seq_point (array, sp, last_seq_point, next [i], TRUE))
			last_seq_point = sp;
	}

	if (td->verbose_level) {
		g_print ("\nSEQ POINT MAP FOR %s: \n", td->method->name);

		for (i = 0; i < td->seq_points->len; ++i) {
			SeqPoint *sp = (SeqPoint*)g_ptr_array_index (td->seq_points, i);
			GSList *l;

			if (!next [i])
				continue;

			g_print ("\tIL0x%x[0x%0x] ->", sp->il_offset, sp->native_offset);
			for (l = next [i]; l; l = l->next) {
				int next_index = GPOINTER_TO_UINT (l->data);
				g_print (" IL0x%x", ((SeqPoint*)g_ptr_array_index (td->seq_points, next_index))->il_offset);
			}
			g_print ("\n");
		}
	}

	info = mono_seq_point_info_new (array->len, TRUE, array->data, TRUE, &seq_info_size);
	mono_atomic_fetch_add_i32 (&mono_jit_stats.allocated_seq_points_size, seq_info_size);

	g_byte_array_free (array, TRUE);

	jinfo->seq_points = info;
}

static void
interp_emit_memory_barrier (TransformData *td, int kind)
{
#if defined(TARGET_WASM)
	// mono_memory_barrier is dummy on wasm
#elif defined(TARGET_X86) || defined(TARGET_AMD64)
	if (kind == MONO_MEMORY_BARRIER_SEQ)
		interp_add_ins (td, MINT_MONO_MEMORY_BARRIER);
#else
	interp_add_ins (td, MINT_MONO_MEMORY_BARRIER);
#endif
}

#define BARRIER_IF_VOLATILE(td, kind) \
	do { \
		if (volatile_) { \
			interp_emit_memory_barrier (td, kind); \
			volatile_ = FALSE; \
		} \
	} while (0)

#define INLINE_FAILURE \
	do { \
		if (inlining) \
			goto exit; \
	} while (0)

static void
interp_method_compute_offsets (TransformData *td, InterpMethod *imethod, MonoMethodSignature *sig, MonoMethodHeader *header, MonoError *error)
{
	int i, offset, size, align;
	int num_args = sig->hasthis + sig->param_count;
	int num_il_locals = header->num_locals;
	int num_locals = num_args + num_il_locals;

	imethod->local_offsets = (guint32*)g_malloc (num_il_locals * sizeof(guint32));
	td->locals = (InterpLocal*)g_malloc (num_locals * sizeof (InterpLocal));
	td->locals_size = num_locals;
	td->locals_capacity = td->locals_size;
	offset = 0;

	g_assert (sizeof (stackval) == MINT_VT_ALIGNMENT);

	/*
	 * We will load arguments as if they are locals. Unlike normal locals, every argument
	 * is stored in a stackval sized slot and valuetypes have special semantics since we
	 * receive a pointer to the valuetype data rather than the data itself.
	 */
	for (i = 0; i < num_args; i++) {
		MonoType *type;
		if (sig->hasthis && i == 0)
			type = m_class_get_byval_arg (td->method->klass);
		else
			type = mono_method_signature_internal (td->method)->params [i - sig->hasthis];
		td->locals [i].offset = offset;
		td->locals [i].flags = 0;
		td->locals [i].indirects = 0;
		td->locals [i].type = type;
		td->locals [i].mt = mint_type (type);
		offset += sizeof (stackval);
	}

	td->il_locals_offset = offset;
	for (i = 0; i < num_il_locals; ++i) {
		int index = num_args + i;
		size = mono_type_size (header->locals [i], &align);
		if (header->locals [i]->type == MONO_TYPE_VALUETYPE) {
			if (mono_class_has_failure (header->locals [i]->data.klass)) {
				mono_error_set_for_class_failure (error, header->locals [i]->data.klass);
				return;
			}
		}
		offset += align - 1;
		offset &= ~(align - 1);
		imethod->local_offsets [i] = offset;
		td->locals [index].offset = offset;
		td->locals [index].flags = 0;
		td->locals [index].indirects = 0;
		td->locals [index].type = header->locals [i];
		td->locals [index].mt = mint_type (header->locals [i]);
		offset += size;
	}
	offset = ALIGN_TO (offset, MINT_VT_ALIGNMENT);
	td->il_locals_size = offset - td->il_locals_offset;

	imethod->exvar_offsets = (guint32*)g_malloc (header->num_clauses * sizeof (guint32));
	for (i = 0; i < header->num_clauses; i++) {
		imethod->exvar_offsets [i] = offset;
		offset += sizeof (MonoObject*);
	}
	offset = ALIGN_TO (offset, MINT_VT_ALIGNMENT);

	g_assert (offset < G_MAXUINT16);
	td->total_locals_size = offset;
}

void
mono_test_interp_method_compute_offsets (TransformData *td, InterpMethod *imethod, MonoMethodSignature *signature, MonoMethodHeader *header)
{
	ERROR_DECL (error);
	interp_method_compute_offsets (td, imethod, signature, header, error);
}

static gboolean
type_has_references (MonoType *type)
{
	if (MONO_TYPE_IS_REFERENCE (type))
		return TRUE;
	if (MONO_TYPE_ISSTRUCT (type)) {
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		if (!m_class_is_inited (klass))
			mono_class_init_internal (klass);
		return m_class_has_references (klass);
	}
	return FALSE;
}

/* Return false is failure to init basic blocks due to being in inline method */
static gboolean
init_bb_start (TransformData *td, MonoMethodHeader *header, gboolean inlining)
{
	const unsigned char *ip, *end;
	const MonoOpcode *opcode;
	int offset, i, in, backwards;

	/* intern the strings in the method. */
	ip = header->code;
	end = ip + header->code_size;

	/* inlined method continues the basic block of parent method */
	if (!inlining)
		td->is_bb_start [0] = 1;
	while (ip < end) {
		in = *ip;
		if (in == 0xfe) {
			ip++;
			in = *ip + 256;
		}
		else if (in == 0xf0) {
			ip++;
			in = *ip + MONO_CEE_MONO_ICALL;
		}
		opcode = &mono_opcodes [in];
		switch (opcode->argument) {
		case MonoInlineNone:
			++ip;
			break;
		case MonoInlineString:
			ip += 5;
			break;
		case MonoInlineType:
			ip += 5;
			break;
		case MonoInlineMethod:
			ip += 5;
			break;
		case MonoInlineField:
		case MonoInlineSig:
		case MonoInlineI:
		case MonoInlineTok:
		case MonoShortInlineR:
			ip += 5;
			break;
		case MonoInlineBrTarget:
			offset = read32 (ip + 1);
			ip += 5;
			/* this branch is ignored */
			if (offset == 0 && in == MONO_CEE_BR)
				break;
			backwards = offset < 0;
			offset += ip - header->code;
			g_assert (offset >= 0 && offset < header->code_size);
			if (inlining)
				return FALSE;
			td->is_bb_start [offset] |= backwards ? 2 : 1;
			break;
		case MonoShortInlineBrTarget:
			offset = ((gint8 *)ip) [1];
			ip += 2;
			/* this branch is ignored */
			if (offset == 0 && in == MONO_CEE_BR_S)
				break;
			backwards = offset < 0;
			offset += ip - header->code;
			g_assert (offset >= 0 && offset < header->code_size);
			if (inlining)
				return FALSE;
			td->is_bb_start [offset] |= backwards ? 2 : 1;
			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
			ip += 2;
			break;
		case MonoInlineSwitch: {
			guint32 n;
			const unsigned char *next_ip;
			++ip;
			n = read32 (ip);
			ip += 4;
			next_ip = ip + 4 * n;
			for (i = 0; i < n; i++) {
				offset = read32 (ip);
				backwards = offset < 0;
				offset += next_ip - header->code;
				g_assert (offset >= 0 && offset < header->code_size);
				if (inlining)
					return FALSE;
				td->is_bb_start [offset] |= backwards ? 2 : 1;
				ip += 4;
			}
			break;
		}
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		default:
			g_assert_not_reached ();
		}
	}
	return TRUE;
}

#ifdef NO_UNALIGNED_ACCESS
static int
get_unaligned_opcode (int opcode)
{
	switch (opcode) {
		case MINT_LDFLD_I8:
			return MINT_LDFLD_I8_UNALIGNED;
		case MINT_LDFLD_R8:
			return MINT_LDFLD_R8_UNALIGNED;
		case MINT_STFLD_I8:
			return MINT_STFLD_I8_UNALIGNED;
		case MINT_STFLD_R8:
			return MINT_STFLD_R8_UNALIGNED;
		default:
			g_assert_not_reached ();
	}
	return -1;
}
#endif

static void
interp_handle_isinst (TransformData *td, MonoClass *klass, gboolean isinst_instr)
{
	/* Follow the logic from jit's handle_isinst */
	if (!mono_class_has_variant_generic_params (klass)) {
		if (mono_class_is_interface (klass))
			interp_add_ins (td, isinst_instr ? MINT_ISINST_INTERFACE : MINT_CASTCLASS_INTERFACE);
		else if (!mono_class_is_marshalbyref (klass) && m_class_get_rank (klass) == 0 && !mono_class_is_nullable (klass))
			interp_add_ins (td, isinst_instr ? MINT_ISINST_COMMON : MINT_CASTCLASS_COMMON);
		else
			interp_add_ins (td, isinst_instr ? MINT_ISINST : MINT_CASTCLASS);
	} else {
		interp_add_ins (td, isinst_instr ? MINT_ISINST : MINT_CASTCLASS);
	}
	td->last_ins->data [0] = get_data_item_index (td, klass);
	td->ip += 5;
}

static void
interp_emit_ldsflda (TransformData *td, MonoClassField *field, MonoError *error)
{
	MonoDomain *domain = td->rtm->domain;
	// Initialize the offset for the field
	MonoVTable *vtable = mono_class_vtable_checked (domain, field->parent, error);
	return_if_nok (error);

	if (mono_class_field_is_special_static (field)) {
		guint32 offset;

		mono_domain_lock (domain);
		g_assert (domain->special_static_fields);
		offset = GPOINTER_TO_UINT (g_hash_table_lookup (domain->special_static_fields, field));
		mono_domain_unlock (domain);
		g_assert (offset);

		interp_add_ins (td, MINT_LDSSFLDA);
		WRITE32_INS(td->last_ins, 0, &offset);
	} else {
		interp_add_ins (td, MINT_LDSFLDA);
		td->last_ins->data [0] = get_data_item_index (td, vtable);
		td->last_ins->data [1] = get_data_item_index (td, (char*)mono_vtable_get_static_field_data (vtable) + field->offset);
	}
}

static gboolean
interp_emit_load_const (TransformData *td, gpointer field_addr, int mt)
{
	if ((mt >= MINT_TYPE_I1 && mt <= MINT_TYPE_I4)) {
		gint32 val;
		switch (mt) {
		case MINT_TYPE_I1:
			val = *(gint8*)field_addr;
			break;
		case MINT_TYPE_U1:
			val = *(guint8*)field_addr;
			break;
		case MINT_TYPE_I2:
			val = *(gint16*)field_addr;
			break;
		case MINT_TYPE_U2:
			val = *(guint16*)field_addr;
			break;
		default:
			val = *(gint32*)field_addr;
		}
		interp_get_ldc_i4_from_const (td, NULL, val);
	} else if (mt == MINT_TYPE_I8) {
		gint64 val = *(gint64*)field_addr;
		interp_add_ins (td, MINT_LDC_I8);
		WRITE64_INS (td->last_ins, 0, &val);
	} else if (mt == MINT_TYPE_R4) {
		float val = *(float*)field_addr;
		interp_add_ins (td, MINT_LDC_R4);
		WRITE32_INS (td->last_ins, 0, &val);
	} else if (mt == MINT_TYPE_R8) {
		double val = *(double*)field_addr;
		interp_add_ins (td, MINT_LDC_R8);
		WRITE64_INS (td->last_ins, 0, &val);
	} else {
		return FALSE;
	}
	return TRUE;
}

/*
 * emit_convert:
 *
 *   Emit some implicit conversions which are not part of the .net spec, but are allowed by MS.NET.
 */
static void
emit_convert (TransformData *td, int stack_type, MonoType *ftype)
{
	ftype = mini_get_underlying_type (ftype);

	// FIXME: Add more
	switch (ftype->type) {
	case MONO_TYPE_I8: {
		switch (stack_type) {
		case STACK_TYPE_I4:
			interp_add_ins (td, MINT_CONV_I8_I4);
			break;
		default:
			break;
		}
		break;
	}
	default:
		break;
	}
}

static void
interp_emit_sfld_access (TransformData *td, MonoClassField *field, MonoClass *field_class, int mt, gboolean is_load, MonoError *error)
{
	MonoDomain *domain = td->rtm->domain;
	// Initialize the offset for the field
	MonoVTable *vtable = mono_class_vtable_checked (domain, field->parent, error);
	return_if_nok (error);

	if (mono_class_field_is_special_static (field)) {
		guint32 offset;

		mono_domain_lock (domain);
		g_assert (domain->special_static_fields);
		offset = GPOINTER_TO_UINT (g_hash_table_lookup (domain->special_static_fields, field));
		mono_domain_unlock (domain);
		g_assert (offset);

		// Offset is SpecialStaticOffset
		if ((offset & 0x80000000) == 0 && mt != MINT_TYPE_VT) {
			// This field is thread static
			interp_add_ins (td, (is_load ? MINT_LDTSFLD_I1 : MINT_STTSFLD_I1) + mt);
			WRITE32_INS(td->last_ins, 0, &offset);
		} else {
			if (mt == MINT_TYPE_VT) {
				interp_add_ins (td, is_load ? MINT_LDSSFLD_VT : MINT_STSSFLD_VT);
				WRITE32_INS(td->last_ins, 0, &offset);

				int size = mono_class_value_size (field_class, NULL);
				WRITE32_INS(td->last_ins, 2, &size);
			} else {
				interp_add_ins (td, is_load ? MINT_LDSSFLD : MINT_STSSFLD);
				td->last_ins->data [0] = get_data_item_index (td, field);
				WRITE32_INS(td->last_ins, 1, &offset);
			}
		}
	} else {
		gpointer field_addr = (char*)mono_vtable_get_static_field_data (vtable) + field->offset;
		if (is_load) {
			MonoType *ftype = mono_field_get_type_internal (field);
			if (ftype->attrs & FIELD_ATTRIBUTE_INIT_ONLY && vtable->initialized) {
				if (interp_emit_load_const (td, field_addr, mt))
					return;
			}
			interp_add_ins (td, (mt == MINT_TYPE_VT) ? MINT_LDSFLD_VT : (MINT_LDSFLD_I1 + mt - MINT_TYPE_I1));
		} else {
			interp_add_ins (td, (mt == MINT_TYPE_VT) ? MINT_STSFLD_VT : (MINT_STSFLD_I1 + mt - MINT_TYPE_I1));
		}

		td->last_ins->data [0] = get_data_item_index (td, vtable);
		td->last_ins->data [1] = get_data_item_index (td, (char*)field_addr);

		if (mt == MINT_TYPE_VT) {
			int size = mono_class_value_size (field_class, NULL);
			WRITE32_INS(td->last_ins, 2, &size);
		}
	}
}

static gboolean
signature_has_vt_params (MonoMethodSignature *csignature)
{
	int i;
	for (i = 0; i < csignature->param_count; ++i) {
		int mt = mint_type (csignature->params [i]);
		if (mt == MINT_TYPE_VT)
			return TRUE;
	}
	return FALSE;
}

static gboolean
generate_code (TransformData *td, MonoMethod *method, MonoMethodHeader *header, MonoGenericContext *generic_context, MonoError *error)
{
	int target;
	int offset, mt, i, i32;
	guint32 token;
	int in_offset;
	const unsigned char *end;
	MonoSimpleBasicBlock *bb = NULL, *original_bb = NULL;
	gboolean sym_seq_points = FALSE;
	MonoBitSet *seq_point_locs = NULL;
	gboolean readonly = FALSE;
	gboolean volatile_ = FALSE;
	MonoClass *constrained_class = NULL;
	MonoClass *klass;
	MonoClassField *field;
	MonoImage *image = m_class_get_image (method->klass);
	InterpMethod *rtm = td->rtm;
	MonoDomain *domain = rtm->domain;
	MonoMethodSignature *signature = mono_method_signature_internal (method);
	int num_args = signature->hasthis + signature->param_count;
	gboolean ret = TRUE;
	gboolean emitted_funccall_seq_point = FALSE;
	guint32 *arg_locals = NULL;
	guint32 *local_locals = NULL;
	InterpInst *last_seq_point = NULL;
	gboolean save_last_error = FALSE;
	gboolean inlining = td->method != method;

	original_bb = bb = mono_basic_block_split (method, error, header);
	goto_if_nok (error, exit);
	g_assert (bb);

	td->il_code = header->code;
	td->in_start = td->ip = header->code;
	end = td->ip + header->code_size;

	if (!init_bb_start (td, header, inlining))
		goto exit;

	if (!inlining) {
		for (i = 0; i < header->code_size; i++) {
			td->stack_height [i] = -1;
			td->clause_indexes [i] = -1;
		}
	}

	for (i = 0; i < header->num_clauses; i++) {
		MonoExceptionClause *c = header->clauses + i;
		td->stack_height [c->handler_offset] = 0;
		td->vt_stack_size [c->handler_offset] = 0;
		td->is_bb_start [c->handler_offset] = 1;
		td->is_bb_start [c->try_offset] = 1;

		td->stack_height [c->handler_offset] = 1;
		td->stack_state [c->handler_offset] = (StackInfo*)g_malloc0(sizeof(StackInfo));
		td->stack_state [c->handler_offset][0].type = STACK_TYPE_O;
		td->stack_state [c->handler_offset][0].klass = NULL; /*FIX*/

		if (c->flags & MONO_EXCEPTION_CLAUSE_FILTER) {
			td->stack_height [c->data.filter_offset] = 0;
			td->vt_stack_size [c->data.filter_offset] = 0;
			td->is_bb_start [c->data.filter_offset] = 1;

			td->stack_height [c->data.filter_offset] = 1;
			td->stack_state [c->data.filter_offset] = (StackInfo*)g_malloc0(sizeof(StackInfo));
			td->stack_state [c->data.filter_offset][0].type = STACK_TYPE_O;
			td->stack_state [c->data.filter_offset][0].klass = NULL; /*FIX*/
		}

		for (int j = c->handler_offset; j < c->handler_offset + c->handler_len; ++j) {
			if (td->clause_indexes [j] == -1)
				td->clause_indexes [j] = i;
		}
	}

	if (td->gen_sdb_seq_points && !inlining) {
		MonoDebugMethodInfo *minfo;
		get_basic_blocks (td);

		minfo = mono_debug_lookup_method (method);

		if (minfo) {
			MonoSymSeqPoint *sps;
			int i, n_il_offsets;

			mono_debug_get_seq_points (minfo, NULL, NULL, NULL, &sps, &n_il_offsets);
			// FIXME: Free
			seq_point_locs = mono_bitset_mem_new (mono_mempool_alloc0 (td->mempool, mono_bitset_alloc_size (header->code_size, 0)), header->code_size, 0);
			sym_seq_points = TRUE;

			for (i = 0; i < n_il_offsets; ++i) {
				if (sps [i].il_offset < header->code_size)
					mono_bitset_set_fast (seq_point_locs, sps [i].il_offset);
			}
			g_free (sps);

			MonoDebugMethodAsyncInfo* asyncMethod = mono_debug_lookup_method_async_debug_info (method);
			if (asyncMethod) {
				for (i = 0; asyncMethod != NULL && i < asyncMethod->num_awaits; i++) {
					mono_bitset_set_fast (seq_point_locs, asyncMethod->resume_offsets [i]);
					mono_bitset_set_fast (seq_point_locs, asyncMethod->yield_offsets [i]);
				}
				mono_debug_free_method_async_debug_info (asyncMethod);
			}
		} else if (!method->wrapper_type && !method->dynamic && mono_debug_image_has_debug_info (m_class_get_image (method->klass))) {
			/* Methods without line number info like auto-generated property accessors */
			seq_point_locs = mono_bitset_new (header->code_size, 0);
			sym_seq_points = TRUE;
		}
	}

	if (sym_seq_points) {
		last_seq_point = interp_add_ins (td, MINT_SDB_SEQ_POINT);
		last_seq_point->flags |= INTERP_INST_FLAG_SEQ_POINT_METHOD_ENTRY;
	}

	if (mono_debugger_method_has_breakpoint (method))
		interp_add_ins (td, MINT_BREAKPOINT);

	if (!inlining) {
		if (td->verbose_level) {
			char *tmp = mono_disasm_code (NULL, method, td->ip, end);
			char *name = mono_method_full_name (method, TRUE);
			g_print ("Method %s, original code:\n", name);
			g_print ("%s\n", tmp);
			g_free (tmp);
			g_free (name);
		}

		/*
		 * We initialize the locals regardless of the presence of the init_locals
		 * flag. Locals holding references need to be zeroed so we don't risk
		 * crashing the GC if they end up being stored in an object.
		 *
		 * FIXME
		 * Track values of locals over multiple basic blocks. This would enable
		 * us to kill the MINT_INITLOCALS instruction if all locals are initialized
		 * before use. We also don't need this instruction if the init locals flag
		 * is not set and there are no locals holding references.
		 */
		if (header->num_locals) {
			interp_add_ins (td, MINT_INITLOCALS);
			td->last_ins->data [0] = td->il_locals_offset;
			td->last_ins->data [1] = td->il_locals_size;
		}

		guint16 enter_profiling = 0;
		if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
			enter_profiling |= TRACING_FLAG;
		if (rtm->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_ENTER)
			enter_profiling |= PROFILING_FLAG;
		if (enter_profiling) {
			interp_add_ins (td, MINT_PROF_ENTER);
			td->last_ins->data [0] = enter_profiling;
		}

		/*
		 * If safepoints are required by default, always check for polling,
		 * without emitting new instructions. This optimizes method entry in
		 * the common scenario, which is coop.
		 */
#if !defined(ENABLE_HYBRID_SUSPEND) && !defined(ENABLE_COOP_SUSPEND)
		/* safepoint is required on method entry */
		if (mono_threads_are_safepoints_enabled ())
			interp_add_ins (td, MINT_SAFEPOINT);
#endif
	} else {
		int local;
		arg_locals = (guint32*) g_malloc ((!!signature->hasthis + signature->param_count) * sizeof (guint32));
		/* Allocate locals to store inlined method args from stack */
		for (i = signature->param_count - 1; i >= 0; i--) {
			local = create_interp_local (td, signature->params [i]);
			arg_locals [i + !!signature->hasthis] = local;
			store_local (td, local);
		}

		if (signature->hasthis) {
			/*
			 * If this is value type, it is passed by address and not by value.
			 * Valuetype this local gets integer type MINT_TYPE_I.
			 */
			MonoType *type;
			if (m_class_is_valuetype (method->klass))
				type = mono_get_int_type ();
			else
				type = mono_get_object_type ();
			local = create_interp_local (td, type);
			arg_locals [0] = local;
			store_local (td, local);
		}

		local_locals = (guint32*) g_malloc (header->num_locals * sizeof (guint32));
		/* Allocate locals to store inlined method args from stack */
		for (i = 0; i < header->num_locals; i++)
			local_locals [i] = create_interp_local (td, header->locals [i]);
	}

	td->dont_inline = g_list_prepend (td->dont_inline, method);
	while (td->ip < end) {
		g_assert (td->sp >= td->stack);
		g_assert (td->vt_sp < 0x10000000);
		in_offset = td->ip - header->code;
		if (!inlining)
			td->current_il_offset = in_offset;
		td->in_start = td->ip;
		InterpInst *prev_last_ins = td->last_ins;

		// Inlined method doesn't have clauses or branches
		if (!inlining && td->stack_height [in_offset] >= 0) {
			g_assert (td->is_bb_start [in_offset]);
			if (td->stack_height [in_offset] > 0)
				memcpy (td->stack, td->stack_state [in_offset], td->stack_height [in_offset] * sizeof(td->stack [0]));
			td->sp = td->stack + td->stack_height [in_offset];
			td->vt_sp = td->vt_stack_size [in_offset];
		}

		if (in_offset == bb->end)
			bb = bb->next;

		if (bb->dead) {
			int op_size = mono_opcode_size (td->ip, end);
			g_assert (op_size > 0); /* The BB formation pass must catch all bad ops */

			if (td->verbose_level > 1)
				g_print ("SKIPPING DEAD OP at %x\n", in_offset);

			td->ip += op_size;
			continue;
		}

		if (td->verbose_level > 1) {
			g_print ("IL_%04lx %s %-10s, sp %ld, %s %-12s vt_sp %u (max %u)\n",
				td->ip - td->il_code,
				td->is_bb_start [td->ip - td->il_code] == 3 ? "<>" :
				td->is_bb_start [td->ip - td->il_code] == 2 ? "< " :
				td->is_bb_start [td->ip - td->il_code] == 1 ? " >" : "  ",
				mono_opcode_name (*td->ip), td->sp - td->stack,
				td->sp > td->stack ? stack_type_string [td->sp [-1].type] : "  ",
				(td->sp > td->stack && (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_VT)) ? (td->sp [-1].klass == NULL ? "?" : m_class_get_name (td->sp [-1].klass)) : "",
				td->vt_sp, td->max_vt_sp);
		}

		if (sym_seq_points && mono_bitset_test_fast (seq_point_locs, td->ip - header->code)) {
			InterpBasicBlock *cbb = td->offset_to_bb [td->ip - header->code];
			g_assert (cbb);

			/*
			 * Make methods interruptable at the beginning, and at the targets of
			 * backward branches.
			 */
			if (in_offset == 0 || g_slist_length (cbb->preds) > 1)
				interp_add_ins (td, MINT_SDB_INTR_LOC);

			last_seq_point = interp_add_ins (td, MINT_SDB_SEQ_POINT);
		}

		if (td->prof_coverage) {
			guint32 cil_offset = td->ip - header->code;
			gpointer counter = &td->coverage_info->data [cil_offset].count;
			td->coverage_info->data [cil_offset].cil_code = (unsigned char*)td->ip;

			interp_add_ins (td, MINT_PROF_COVERAGE_STORE);
			WRITE64_INS (td->last_ins, 0, &counter);
		}

		if (!inlining && td->is_bb_start [in_offset]) {
			int index = td->clause_indexes [in_offset];
			if (index != -1) {
				MonoExceptionClause *clause = &header->clauses [index];
				if ((clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY ||
					clause->flags == MONO_EXCEPTION_CLAUSE_FAULT) &&
						in_offset == clause->handler_offset)
					interp_add_ins (td, MINT_START_ABORT_PROT);
			}
		}

		switch (*td->ip) {
		case CEE_NOP: 
			/* lose it */
			emitted_funccall_seq_point = FALSE;
			++td->ip;
			break;
		case CEE_BREAK:
			SIMPLE_OP(td, MINT_BREAK);
			break;
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3: {
			int arg_n = *td->ip - CEE_LDARG_0;
			if (!inlining)
				load_arg (td, arg_n);
			else
				load_local (td, arg_locals [arg_n]);
			++td->ip;
			break;
		}
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3: {
			int loc_n = *td->ip - CEE_LDLOC_0;
			if (!inlining)
				load_local (td, num_args + loc_n);
			else
				load_local (td, local_locals [loc_n]);
			++td->ip;
			break;
		}
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3: {
			int loc_n = *td->ip - CEE_STLOC_0;
			if (!inlining)
				store_local (td, num_args + loc_n);
			else
				store_local (td, local_locals [loc_n]);
			++td->ip;
			break;
		}
		case CEE_LDARG_S: {
			int arg_n = ((guint8 *)td->ip)[1];
			if (!inlining)
				load_arg (td, arg_n);
			else
				load_local (td, arg_locals [arg_n]);
			td->ip += 2;
			break;
		}
		case CEE_LDARGA_S: {
			/* NOTE: n includes this */
			int n = ((guint8 *) td->ip) [1];

			if (!inlining) {
				get_arg_type_exact (td, n, &mt);
				if (mt == MINT_TYPE_VT) {
					interp_add_ins (td, MINT_LDARGA_VT);
				} else {
					interp_add_ins (td, MINT_LDLOCA_S);
					td->locals [n].indirects++;
				}
				td->last_ins->data [0] = n;
			} else {
				int loc_n = arg_locals [n];
				interp_add_ins (td, MINT_LDLOCA_S);
				td->last_ins->data [0] = loc_n;
				td->locals [loc_n].indirects++;
			}
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_MP);
			td->ip += 2;
			break;
		}
		case CEE_STARG_S: {
			int arg_n = ((guint8 *)td->ip)[1];
			if (!inlining)
				store_arg (td, arg_n);
			else
				store_local (td, arg_locals [arg_n]);
			td->ip += 2;
			break;
		}
		case CEE_LDLOC_S: {
			int loc_n = ((guint8 *)td->ip)[1];
			if (!inlining)
				load_local (td, num_args + loc_n);
			else
				load_local (td, local_locals [loc_n]);
			td->ip += 2;
			break;
		}
		case CEE_LDLOCA_S: {
			int loc_n = ((guint8 *)td->ip)[1];
			interp_add_ins (td, MINT_LDLOCA_S);
			if (!inlining)
				loc_n += num_args;
			else
				loc_n = local_locals [loc_n];
			td->last_ins->data [0] = loc_n;
			td->locals [loc_n].indirects++;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_MP);
			td->ip += 2;
			break;
		}
		case CEE_STLOC_S: {
			int loc_n = ((guint8 *)td->ip)[1];
			if (!inlining)
				store_local (td, num_args + loc_n);
			else
				store_local (td, local_locals [loc_n]);
			td->ip += 2;
			break;
		}
		case CEE_LDNULL: 
			SIMPLE_OP(td, MINT_LDNULL);
			PUSH_TYPE(td, STACK_TYPE_O, NULL);
			break;
		case CEE_LDC_I4_M1:
			SIMPLE_OP(td, MINT_LDC_I4_M1);
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
			break;
		case CEE_LDC_I4_0:
			// Only single basic block functions are inlined.
			if (td->ip - td->il_code + 2 < td->code_size && (inlining || !td->is_bb_start [td->ip + 1 - td->il_code]) && td->ip [1] == 0xfe && td->ip [2] == CEE_CEQ &&
				td->sp > td->stack && td->sp [-1].type == STACK_TYPE_I4) {
				SIMPLE_OP(td, MINT_CEQ0_I4);
				td->ip += 2;
			} else {
				SIMPLE_OP(td, MINT_LDC_I4_0);
				PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
			}
			break;
		case CEE_LDC_I4_1:
			// Only single basic block functions are inlined.
			if (td->ip - td->il_code + 1 < td->code_size && (inlining || !td->is_bb_start [td->ip + 1 - td->il_code]) &&
				(td->ip [1] == CEE_ADD || td->ip [1] == CEE_SUB) && td->sp [-1].type == STACK_TYPE_I4) {
				interp_add_ins (td, td->ip [1] == CEE_ADD ? MINT_ADD1_I4 : MINT_SUB1_I4);
				td->ip += 2;
			} else {
				SIMPLE_OP(td, MINT_LDC_I4_1);
				PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
			}
			break;
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8:
			SIMPLE_OP(td, (*td->ip - CEE_LDC_I4_0) + MINT_LDC_I4_0);
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
			break;
		case CEE_LDC_I4_S: 
			interp_add_ins (td, MINT_LDC_I4_S);
			td->last_ins->data [0] = ((gint8 *) td->ip) [1];
			td->ip += 2;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
			break;
		case CEE_LDC_I4:
			i32 = read32 (td->ip + 1);
			interp_add_ins (td, MINT_LDC_I4);
			WRITE32_INS (td->last_ins, 0, &i32);
			td->ip += 5;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
			break;
		case CEE_LDC_I8: {
			gint64 val = read64 (td->ip + 1);
			interp_add_ins (td, MINT_LDC_I8);
			WRITE64_INS (td->last_ins, 0, &val);
			td->ip += 9;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_I8);
			break;
		}
		case CEE_LDC_R4: {
			float val;
			readr4 (td->ip + 1, &val);
			interp_add_ins (td, MINT_LDC_R4);
			WRITE32_INS (td->last_ins, 0, &val);
			td->ip += 5;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_R4);
			break;
		}
		case CEE_LDC_R8: {
			double val;
			readr8 (td->ip + 1, &val);
			interp_add_ins (td, MINT_LDC_R8);
			WRITE64_INS (td->last_ins, 0, &val);
			td->ip += 9;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_R8);
			break;
		}
		case CEE_DUP: {
			int type = td->sp [-1].type;
			MonoClass *klass = td->sp [-1].klass;
			if (td->sp [-1].type == STACK_TYPE_VT) {
				gint32 size = mono_class_value_size (klass, NULL);
				PUSH_VT(td, size);
				interp_add_ins (td, MINT_DUP_VT);
				WRITE32_INS (td->last_ins, 0, &size);
				td->ip ++;
			} else 
				SIMPLE_OP(td, MINT_DUP);
			PUSH_TYPE(td, type, klass);
			break;
		}
		case CEE_POP:
			CHECK_STACK(td, 1);
			if (td->sp [-1].type == STACK_TYPE_VT) {
				int size = mono_class_value_size (td->sp [-1].klass, NULL);
				size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
				interp_add_ins (td, MINT_POP_VT);
				WRITE32_INS (td->last_ins, 0, &size);
				td->vt_sp -= size;
				td->ip++;
			} else {
				SIMPLE_OP(td, MINT_POP);
			}
			--td->sp;
			break;
		case CEE_JMP: {
			MonoMethod *m;
			INLINE_FAILURE;
			if (td->sp > td->stack)
				g_warning ("CEE_JMP: stack must be empty");
			token = read32 (td->ip + 1);
			m = mono_get_method_checked (image, token, NULL, generic_context, error);
			goto_if_nok (error, exit);
			interp_add_ins (td, MINT_JMP);
			td->last_ins->data [0] = get_data_item_index (td, mono_interp_get_imethod (domain, m, error));
			goto_if_nok (error, exit);
			td->ip += 5;
			break;
		}
		case CEE_CALLVIRT: /* Fall through */
		case CEE_CALLI:    /* Fall through */
		case CEE_CALL: {
			gboolean need_seq_point = FALSE;

			if (sym_seq_points && !mono_bitset_test_fast (seq_point_locs, td->ip + 5 - header->code))
				need_seq_point = TRUE;

			if (!interp_transform_call (td, method, NULL, domain, generic_context, td->is_bb_start, constrained_class, readonly, error, TRUE, save_last_error))
				goto exit;

			if (need_seq_point) {
				//check is is a nested call and remove the MONO_INST_NONEMPTY_STACK of the last breakpoint, only for non native methods
				if (!(method->flags & METHOD_IMPL_ATTRIBUTE_NATIVE)) {
					if (emitted_funccall_seq_point)	{
						if (last_seq_point)
							last_seq_point->flags |= INTERP_INST_FLAG_SEQ_POINT_NESTED_CALL;
					}
					else
						emitted_funccall_seq_point = TRUE;	
				}
				last_seq_point = interp_add_ins (td, MINT_SDB_SEQ_POINT);
				// This seq point is actually associated with the instruction following the call
				last_seq_point->il_offset = td->ip - header->code;
				last_seq_point->flags = INTERP_INST_FLAG_SEQ_POINT_NONEMPTY_STACK;
			}

			constrained_class = NULL;
			readonly = FALSE;
			save_last_error = FALSE;
			break;
		}
		case CEE_RET: {
			/* Return from inlined method, return value is on top of stack */
			if (td->method != method) {
				td->ip++;
				break;
			}

			int vt_size = 0;
			MonoType *ult = mini_type_get_underlying_type (signature->ret);
			if (ult->type != MONO_TYPE_VOID) {
				CHECK_STACK (td, 1);
				--td->sp;
				if (mint_type (ult) == MINT_TYPE_VT) {
					MonoClass *klass = mono_class_from_mono_type_internal (ult);
					vt_size = mono_class_value_size (klass, NULL);
				}
			}
			if (td->sp > td->stack) {
				mono_error_set_generic_error (error, "System", "InvalidProgramException", "");
				goto exit;
			}
			if (td->vt_sp != ALIGN_TO (vt_size, MINT_VT_ALIGNMENT))
				g_error ("%s: CEE_RET: value type stack: %d vs. %d", mono_method_full_name (td->method, TRUE), td->vt_sp, vt_size);

			if (sym_seq_points) {
				last_seq_point = interp_add_ins (td, MINT_SDB_SEQ_POINT);
				td->last_ins->flags |= INTERP_INST_FLAG_SEQ_POINT_METHOD_EXIT;
			}

			guint16 exit_profiling = 0;
			if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
				exit_profiling |= TRACING_FLAG;
			if (rtm->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE)
				exit_profiling |= PROFILING_FLAG;
			if (exit_profiling) {
				/* This does the return as well */
				if (ult->type == MONO_TYPE_VOID) {
					interp_add_ins (td, MINT_PROF_EXIT_VOID);
					vt_size = -1;
				} else {
					interp_add_ins (td, MINT_PROF_EXIT);
				}
				td->last_ins->data [0] = exit_profiling;
				WRITE32_INS (td->last_ins, 1, &vt_size);
				POP_VT (td, vt_size);
				++td->ip;
			} else {
				if (vt_size == 0)
					SIMPLE_OP(td, ult->type == MONO_TYPE_VOID ? MINT_RET_VOID : MINT_RET);
				else {
					interp_add_ins (td, MINT_RET_VT);
					WRITE32_INS (td->last_ins, 0, &vt_size);
					POP_VT (td, vt_size);
					++td->ip;
				}
			}
			break;
		}
		case CEE_BR: {
			int offset = read32 (td->ip + 1);
			if (offset) {
				INLINE_FAILURE;
				handle_branch (td, MINT_BR_S, MINT_BR, 5 + offset);
			}
			td->ip += 5;
			break;
		}
		case CEE_BR_S: {
			int offset = (gint8)td->ip [1];
			if (offset) {
				INLINE_FAILURE;
				handle_branch (td, MINT_BR_S, MINT_BR, 2 + (gint8)td->ip [1]);
			}
			td->ip += 2;
			break;
		}
		case CEE_BRFALSE:
			INLINE_FAILURE;
			one_arg_branch (td, MINT_BRFALSE_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BRFALSE_S:
			INLINE_FAILURE;
			one_arg_branch (td, MINT_BRFALSE_I4, 2 + (gint8)td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BRTRUE:
			INLINE_FAILURE;
			one_arg_branch (td, MINT_BRTRUE_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BRTRUE_S:
			INLINE_FAILURE;
			one_arg_branch (td, MINT_BRTRUE_I4, 2 + (gint8)td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BEQ:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BEQ_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BEQ_S:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BEQ_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BGE:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BGE_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BGE_S:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BGE_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BGT:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BGT_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BGT_S:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BGT_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BLT:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BLT_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BLT_S:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BLT_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BLE:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BLE_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BLE_S:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BLE_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BNE_UN:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BNE_UN_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BNE_UN_S:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BNE_UN_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BGE_UN:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BGE_UN_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BGE_UN_S:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BGE_UN_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BGT_UN:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BGT_UN_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BGT_UN_S:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BGT_UN_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BLE_UN:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BLE_UN_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BLE_UN_S:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BLE_UN_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BLT_UN:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BLT_UN_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BLT_UN_S:
			INLINE_FAILURE;
			two_arg_branch (td, MINT_BLT_UN_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_SWITCH: {
			INLINE_FAILURE;
			guint32 n;
			const unsigned char *next_ip;
			++td->ip;
			n = read32 (td->ip);
			interp_add_ins_explicit (td, MINT_SWITCH, MINT_SWITCH_LEN (n));
			WRITE32_INS (td->last_ins, 0, &n);
			td->ip += 4;
			next_ip = td->ip + n * 4;
			--td->sp;
			int stack_height = td->sp - td->stack;
			for (i = 0; i < n; i++) {
				offset = read32 (td->ip);
				target = next_ip - td->il_code + offset;
				if (offset < 0) {
#if DEBUG_INTERP
					if (stack_height > 0 && stack_height != td->stack_height [target])
						g_warning ("SWITCH with back branch and non-empty stack");
#endif
				} else {
					td->stack_height [target] = stack_height;
					td->vt_stack_size [target] = td->vt_sp;
					if (stack_height > 0)
						td->stack_state [target] = (StackInfo*)g_memdup (td->stack, stack_height * sizeof (td->stack [0]));
				}
				WRITE32_INS (td->last_ins, 2 + i * 2, &target);
				td->ip += 4;
			}
			break;
		}
		case CEE_LDIND_I1:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I1_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		case CEE_LDIND_U1:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_U1_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		case CEE_LDIND_I2:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I2_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		case CEE_LDIND_U2:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_U2_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		case CEE_LDIND_I4:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I4_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		case CEE_LDIND_U4:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_U4_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		case CEE_LDIND_I8:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I8_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		case CEE_LDIND_I:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_REF_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		case CEE_LDIND_R4:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_R4_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R4);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		case CEE_LDIND_R8:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_R8_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		case CEE_LDIND_REF:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_REF_CHECK);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_O);
			break;
		case CEE_STIND_REF:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);
			SIMPLE_OP (td, MINT_STIND_REF);
			td->sp -= 2;
			break;
		case CEE_STIND_I1:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);
			SIMPLE_OP (td, MINT_STIND_I1);
			td->sp -= 2;
			break;
		case CEE_STIND_I2:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);
			SIMPLE_OP (td, MINT_STIND_I2);
			td->sp -= 2;
			break;
		case CEE_STIND_I4:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);
			SIMPLE_OP (td, MINT_STIND_I4);
			td->sp -= 2;
			break;
		case CEE_STIND_I:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);
			SIMPLE_OP (td, MINT_STIND_I);
			td->sp -= 2;
			break;
		case CEE_STIND_I8:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);
			SIMPLE_OP (td, MINT_STIND_I8);
			td->sp -= 2;
			break;
		case CEE_STIND_R4:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);
			SIMPLE_OP (td, MINT_STIND_R4);
			td->sp -= 2;
			break;
		case CEE_STIND_R8:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);
			SIMPLE_OP (td, MINT_STIND_R8);
			td->sp -= 2;
			break;
		case CEE_ADD:
			binary_arith_op(td, MINT_ADD_I4);
			++td->ip;
			break;
		case CEE_SUB:
			binary_arith_op(td, MINT_SUB_I4);
			++td->ip;
			break;
		case CEE_MUL:
			binary_arith_op(td, MINT_MUL_I4);
			++td->ip;
			break;
		case CEE_DIV:
			binary_arith_op(td, MINT_DIV_I4);
			++td->ip;
			break;
		case CEE_DIV_UN:
			binary_arith_op(td, MINT_DIV_UN_I4);
			++td->ip;
			break;
		case CEE_REM:
			binary_arith_op (td, MINT_REM_I4);
			++td->ip;
			break;
		case CEE_REM_UN:
			binary_arith_op (td, MINT_REM_UN_I4);
			++td->ip;
			break;
		case CEE_AND:
			binary_arith_op (td, MINT_AND_I4);
			++td->ip;
			break;
		case CEE_OR:
			binary_arith_op (td, MINT_OR_I4);
			++td->ip;
			break;
		case CEE_XOR:
			binary_arith_op (td, MINT_XOR_I4);
			++td->ip;
			break;
		case CEE_SHL:
			shift_op (td, MINT_SHL_I4);
			++td->ip;
			break;
		case CEE_SHR:
			shift_op (td, MINT_SHR_I4);
			++td->ip;
			break;
		case CEE_SHR_UN:
			shift_op (td, MINT_SHR_UN_I4);
			++td->ip;
			break;
		case CEE_NEG:
			unary_arith_op (td, MINT_NEG_I4);
			++td->ip;
			break;
		case CEE_NOT:
			unary_arith_op (td, MINT_NOT_I4);
			++td->ip;
			break;
		case CEE_CONV_U1:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_U1_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_U1_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_U1_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_U1_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_CONV_I1:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_I1_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_I1_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_I1_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_I1_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_CONV_U2:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_U2_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_U2_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_U2_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_U2_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_CONV_I2:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_I2_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_I2_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_I2_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_I2_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_CONV_U:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
#if SIZEOF_VOID_P == 4
				interp_add_ins (td, MINT_CONV_U4_R8);
#else
				interp_add_ins (td, MINT_CONV_U8_R8);
#endif
				break;
			case STACK_TYPE_I4:
#if SIZEOF_VOID_P == 8
				interp_add_ins (td, MINT_CONV_I8_U4);
#endif
				break;
			case STACK_TYPE_I8:
#if SIZEOF_VOID_P == 4
				interp_add_ins (td, MINT_CONV_U4_I8);
#endif
				break;
			case STACK_TYPE_MP:
			case STACK_TYPE_O:
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I);
			break;
		case CEE_CONV_I: 
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
#if SIZEOF_VOID_P == 8
				interp_add_ins (td, MINT_CONV_I8_R8);
#else
				interp_add_ins (td, MINT_CONV_I4_R8);
#endif
				break;
			case STACK_TYPE_I4:
#if SIZEOF_VOID_P == 8
				interp_add_ins (td, MINT_CONV_I8_I4);
#endif
				break;
			case STACK_TYPE_O:
				break;
			case STACK_TYPE_MP:
				break;
			case STACK_TYPE_I8:
#if SIZEOF_VOID_P == 4
				interp_add_ins (td, MINT_CONV_I4_I8);
#endif
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I);
			break;
		case CEE_CONV_U4:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_U4_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_U4_R8);
				break;
			case STACK_TYPE_I4:
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_U4_I8);
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 8
				interp_add_ins (td, MINT_CONV_U4_I8);
#endif
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_CONV_I4:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_I4_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_I4_R8);
				break;
			case STACK_TYPE_I4:
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_I4_I8);
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 8
				interp_add_ins (td, MINT_CONV_I4_I8);
#endif
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_CONV_I8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_I8_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_I8_R8);
				break;
			case STACK_TYPE_I4: {
				if (interp_ins_is_ldc (td->last_ins) && (inlining || !td->is_bb_start [in_offset])) {
					gint64 ct = interp_get_const_from_ldc_i4 (td->last_ins);
					interp_clear_ins (td, td->last_ins);

					interp_add_ins (td, MINT_LDC_I8);
					WRITE64_INS (td->last_ins, 0, &ct);
				} else {
					interp_add_ins (td, MINT_CONV_I8_I4);
				}
				break;
			}
			case STACK_TYPE_I8:
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 4
				interp_add_ins (td, MINT_CONV_I8_I4);
#endif
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
			break;
		case CEE_CONV_R4:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_R4_R8);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_R4_I8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_R4_I4);
				break;
			case STACK_TYPE_R4:
				/* no-op */
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R4);
			break;
		case CEE_CONV_R8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_R8_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_R8_I8);
				break;
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_R8_R4);
				break;
			case STACK_TYPE_R8:
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
			break;
		case CEE_CONV_U8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_I4:
				if (interp_ins_is_ldc (td->last_ins) && (inlining || !td->is_bb_start [in_offset])) {
					gint64 ct = (guint32)interp_get_const_from_ldc_i4 (td->last_ins);
					interp_clear_ins (td, td->last_ins);

					interp_add_ins (td, MINT_LDC_I8);
					WRITE64_INS (td->last_ins, 0, &ct);
				} else {
					interp_add_ins (td, MINT_CONV_I8_U4);
				}
				break;
			case STACK_TYPE_I8:
				break;
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_U8_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_U8_R8);
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 4
				interp_add_ins (td, MINT_CONV_I8_U4);
#endif
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
			break;
		case CEE_CPOBJ: {
			CHECK_STACK (td, 2);

			token = read32 (td->ip + 1);
			klass = mono_class_get_and_inflate_typespec_checked (image, token, generic_context, error);
			goto_if_nok (error, exit);

			if (m_class_is_valuetype (klass)) {
				int mt = mint_type (m_class_get_byval_arg (klass));
				interp_add_ins (td, (mt == MINT_TYPE_VT) ? MINT_CPOBJ_VT : MINT_CPOBJ);
				td->last_ins->data [0] = get_data_item_index(td, klass);
			} else {
				interp_add_ins (td, MINT_LDIND_REF);
				interp_add_ins (td, MINT_STIND_REF);
			}
			td->ip += 5;
			td->sp -= 2;
			break;
		}
		case CEE_LDOBJ: {
			CHECK_STACK (td, 1);

			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else {
				klass = mono_class_get_and_inflate_typespec_checked (image, token, generic_context, error);
				goto_if_nok (error, exit);
			}

			interp_emit_ldobj (td, klass);

			td->ip += 5;
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		}
		case CEE_LDSTR: {
			token = mono_metadata_token_index (read32 (td->ip + 1));
			td->ip += 5;
			if (method->wrapper_type == MONO_WRAPPER_NONE) {
				MonoString *s = mono_ldstr_checked (domain, image, token, error);
				goto_if_nok (error, exit);
				/* GC won't scan code stream, but reference is held by metadata
				 * machinery so we are good here */
				interp_add_ins (td, MINT_LDSTR);
				td->last_ins->data [0] = get_data_item_index (td, s);
			} else {
				/* defer allocation to execution-time */
				interp_add_ins (td, MINT_LDSTR_TOKEN);
				td->last_ins->data [0] = get_data_item_index (td, GUINT_TO_POINTER (token));
			}
			PUSH_TYPE(td, STACK_TYPE_O, mono_defaults.string_class);
			break;
		}
		case CEE_NEWOBJ: {
			MonoMethod *m;
			MonoMethodSignature *csignature;
			guint32 vt_stack_used = 0;
			guint32 vt_res_size = 0;

			td->ip++;
			token = read32 (td->ip);
			td->ip += 4;

			m = interp_get_method (method, token, image, generic_context, error);
			goto_if_nok (error, exit);

			csignature = mono_method_signature_internal (m);
			klass = m->klass;

			if (!mono_class_init_internal (klass)) {
				mono_error_set_for_class_failure (error, klass);
				goto_if_nok (error, exit);
			}

			if (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_ABSTRACT) {
				char* full_name = mono_type_get_full_name (klass);
				mono_error_set_member_access (error, "Cannot create an abstract class: %s", full_name);
				g_free (full_name);
				goto_if_nok (error, exit);
			}

			if (mono_class_is_magic_int (klass) || mono_class_is_magic_float (klass)) {
				td->sp -= csignature->param_count;
#if SIZEOF_VOID_P == 8
				if (mono_class_is_magic_int (klass) && td->sp [0].type == STACK_TYPE_I4)
					interp_add_ins (td, MINT_CONV_I8_I4);
				else if (mono_class_is_magic_float (klass) && td->sp [0].type == STACK_TYPE_R4)
					interp_add_ins (td, MINT_CONV_R8_R4);
#endif
				interp_add_ins (td, MINT_NEWOBJ_MAGIC);
				td->last_ins->data [0] = get_data_item_index (td, mono_interp_get_imethod (domain, m, error));
				goto_if_nok (error, exit);

				PUSH_TYPE (td, stack_type [mint_type (m_class_get_byval_arg (klass))], klass);
			} else if (klass == mono_defaults.int_class && csignature->param_count == 1)  {
				td->sp--;
#if SIZEOF_VOID_P == 8
				if (td->sp [0].type == STACK_TYPE_I4)
					interp_add_ins (td, MINT_CONV_I8_I4);
#else
				if (td->sp [0].type == STACK_TYPE_I8)
					interp_add_ins (td, MINT_CONV_OVF_I4_I8);
#endif

				PUSH_TYPE (td, stack_type [mint_type (m_class_get_byval_arg (klass))], klass);
			} else {
				gboolean can_inline = TRUE;
				if (m_class_get_parent (klass) == mono_defaults.array_class) {
					interp_add_ins (td, MINT_NEWOBJ_ARRAY);
					td->last_ins->data [0] = get_data_item_index (td, m->klass);
					td->last_ins->data [1] = csignature->param_count;
				} else if (klass == mono_defaults.string_class) {
					interp_add_ins (td, MINT_NEWOBJ_STRING);
					td->last_ins->data [0] = get_data_item_index (td, mono_interp_get_imethod (domain, m, error));
					td->last_ins->data [1] = csignature->param_count;
				} else if (m_class_get_image (klass) == mono_defaults.corlib &&
						!strcmp (m_class_get_name (m->klass), "ByReference`1") &&
						!strcmp (m->name, ".ctor")) {
					/* public ByReference(ref T value) */
					g_assert (csignature->hasthis && csignature->param_count == 1);
					interp_add_ins (td, MINT_INTRINS_BYREFERENCE_CTOR);
				} else if (m_class_get_image (klass) == mono_defaults.corlib &&
						(!strcmp (m_class_get_name (m->klass), "Span`1") ||
						!strcmp (m_class_get_name (m->klass), "ReadOnlySpan`1")) &&
						csignature->param_count == 2 &&
						csignature->params [0]->type == MONO_TYPE_PTR &&
						!type_has_references (mono_method_get_context (m)->class_inst->type_argv [0])) {
					/* ctor frequently used with ReadOnlySpan over static arrays */
					interp_add_ins (td, MINT_INTRINS_SPAN_CTOR);
				} else if (klass != mono_defaults.string_class &&
						!mono_class_is_marshalbyref (klass) &&
						!mono_class_has_finalizer (klass) &&
						!m_class_has_weak_fields (klass)) {
					gboolean is_vt = m_class_is_valuetype (klass);
					int mt = mint_type (m_class_get_byval_arg (klass));
					gboolean is_vtst = is_vt ? mt == MINT_TYPE_VT : FALSE;
					int vtsize = mono_class_value_size (klass, NULL);
					InterpInst *newobj_fast;

					if (is_vt) {
						newobj_fast = interp_add_ins (td, is_vtst ? MINT_NEWOBJ_VTST_FAST : MINT_NEWOBJ_VT_FAST);
						if (is_vtst)
							newobj_fast->data [2] = vtsize;
					} else {
						MonoVTable *vtable = mono_class_vtable_checked (domain, klass, error);
						goto_if_nok (error, exit);
						newobj_fast = interp_add_ins (td, MINT_NEWOBJ_FAST);
						newobj_fast->data [2] = get_data_item_index (td, vtable);
					}
					newobj_fast->data [1] = csignature->param_count;

					move_stack (td, (td->sp - td->stack) - csignature->param_count, 2);
					StackInfo *tmp_sp = td->sp - csignature->param_count - 2;
					SET_TYPE (tmp_sp, stack_type [mt], klass);
					// for MINT_TYPE_VT, we will push a value type on vtstack
					if (is_vtst)
						PUSH_VT (td, vtsize);
					// In vt case we pass indirect pointer as this
					if (is_vt)
						SET_SIMPLE_TYPE (tmp_sp + 1, STACK_TYPE_I);
					else
						SET_TYPE (tmp_sp + 1, STACK_TYPE_O, klass);

					// We don't support inlining ctors of MINT_TYPE_VT which also receive a MINT_TYPE_VT
					// as an argument. The reason is that we would need to push this on the vtstack before
					// the argument, which is very awkward for uncommon scenario.
					if ((mono_interp_opt & INTERP_OPT_INLINE) && interp_method_check_inlining (td, m, csignature) &&
							(!is_vtst || !signature_has_vt_params (csignature))) {
						MonoMethodHeader *mheader = interp_method_get_header (m, error);
						goto_if_nok (error, exit);

						if (interp_inline_method (td, m, mheader, error)) {
							newobj_fast->data [0] = INLINED_METHOD_FLAG;
							break;
						}
					}
					// If inlining failed, restore the stack.
					// At runtime, interp.c newobj_fast uses an extra stack element
					// after the parameters to store `o` across the non-recursive call
					// where GC will see it.
					// move_stack with the last parameter negative does not reduce max_stack.
					if (is_vtst)
						POP_VT (td, vtsize);
					move_stack (td, (td->sp - td->stack) - csignature->param_count, -2);
					// Set the method to be executed as part of newobj instruction
					newobj_fast->data [0] = get_data_item_index (td, mono_interp_get_imethod (domain, m, error));
					can_inline = FALSE;
				} else {
					// Runtime (interp_exec_method_full in interp.c) inserts
					// extra stack to hold this and return value, before call.
					simulate_runtime_stack_increase (td, 2);
					interp_add_ins (td, MINT_NEWOBJ);
					g_assert (!m_class_is_valuetype (klass));
					td->last_ins->data [0] = get_data_item_index (td, mono_interp_get_imethod (domain, m, error));
				}
				goto_if_nok (error, exit);
				if (!can_inline) {
					/* The constructor was not inlined, abort inlining of current method */
					INLINE_FAILURE;
				}

				td->sp -= csignature->param_count;
				if (mint_type (m_class_get_byval_arg (klass)) == MINT_TYPE_VT) {
					vt_res_size = mono_class_value_size (klass, NULL);
					PUSH_VT (td, vt_res_size);
				}
				for (i = 0; i < csignature->param_count; ++i) {
					int mt = mint_type(csignature->params [i]);
					if (mt == MINT_TYPE_VT) {
						MonoClass *k = mono_class_from_mono_type_internal (csignature->params [i]);
						gint32 size = mono_class_value_size (k, NULL);
						size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
						vt_stack_used += size;
					}
				}
				if ((vt_stack_used != 0 || vt_res_size != 0) &&
						td->last_ins->opcode != MINT_INTRINS_BYREFERENCE_CTOR &&
						td->last_ins->opcode != MINT_INTRINS_SPAN_CTOR) {
					/* FIXME Remove this once vtsp and sp are unified */
					interp_add_ins (td, MINT_VTRESULT);
					td->last_ins->data [0] = vt_res_size;
					WRITE32_INS (td->last_ins, 1, &vt_stack_used);
					td->vt_sp -= vt_stack_used;
				}
				PUSH_TYPE (td, stack_type [mint_type (m_class_get_byval_arg (klass))], klass);
			}
			break;
		}
		case CEE_CASTCLASS:
		case CEE_ISINST: {
			gboolean isinst_instr = *td->ip == CEE_ISINST;
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			interp_handle_isinst (td, klass, isinst_instr);
			if (!isinst_instr)
				td->sp [-1].klass = klass;
			break;
		}
		case CEE_CONV_R_UN:
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_R_UN_I8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_R_UN_I4);
				break;
			default:
				g_assert_not_reached ();
			}
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
			++td->ip;
			break;
		case CEE_UNBOX:
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else {
				klass = mono_class_get_and_inflate_typespec_checked (image, token, generic_context, error);
				goto_if_nok (error, exit);
			}

			if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method;
				if (m_class_is_enumtype (mono_class_get_nullable_param_internal (klass)))
					target_method = mono_class_get_method_from_name_checked (klass, "UnboxExact", 1, 0, error);
				else
					target_method = mono_class_get_method_from_name_checked (klass, "Unbox", 1, 0, error);
				goto_if_nok (error, exit);
				/* td->ip is incremented by interp_transform_call */
				if (!interp_transform_call (td, method, target_method, domain, generic_context, td->is_bb_start, NULL, FALSE, error, FALSE, FALSE))
					goto exit;
				/*
				 * CEE_UNBOX needs to push address of vtype while Nullable.Unbox returns the value type
				 * We create a local variable in the frame so that we can fetch its address.
				 */
				int local = create_interp_local (td, m_class_get_byval_arg (klass));
				store_local (td, local);
				interp_add_ins (td, MINT_LDLOCA_S);
				td->last_ins->data [0] = local;
				td->locals [local].indirects++;
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_MP);
			} else {
				interp_add_ins (td, MINT_UNBOX);
				td->last_ins->data [0] = get_data_item_index (td, klass);
				SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_MP);
				td->ip += 5;
			}
			break;
		case CEE_UNBOX_ANY:
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);

			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			if (mini_type_is_reference (m_class_get_byval_arg (klass))) {
				int mt = mint_type (m_class_get_byval_arg (klass));
				interp_handle_isinst (td, klass, FALSE);
				SET_TYPE (td->sp - 1, stack_type [mt], klass);
			} else if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method;
				if (m_class_is_enumtype (mono_class_get_nullable_param_internal (klass)))
					target_method = mono_class_get_method_from_name_checked (klass, "UnboxExact", 1, 0, error);
				else
					target_method = mono_class_get_method_from_name_checked (klass, "Unbox", 1, 0, error);
				goto_if_nok (error, exit);
				/* td->ip is incremented by interp_transform_call */
				if (!interp_transform_call (td, method, target_method, domain, generic_context, td->is_bb_start, NULL, FALSE, error, FALSE, FALSE))
					goto exit;
			} else {
				interp_add_ins (td, MINT_UNBOX);
				td->last_ins->data [0] = get_data_item_index (td, klass);

				interp_emit_ldobj (td, klass);

				td->ip += 5;
			}

			break;
		case CEE_THROW:
			INLINE_FAILURE;
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_THROW);
			td->sp = td->stack;
			break;
		case CEE_LDFLDA: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			mono_class_init_internal (klass);
#ifndef DISABLE_REMOTING
			if (m_class_get_marshalbyref (klass) || mono_class_is_contextbound (klass) || klass == mono_defaults.marshalbyrefobject_class) {
				g_assert (!is_static);
				int offset = m_class_is_valuetype (klass) ? field->offset - MONO_ABI_SIZEOF (MonoObject) : field->offset;

				interp_add_ins (td, MINT_MONO_LDPTR);
				td->last_ins->data [0] = get_data_item_index (td, klass);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
				interp_add_ins (td, MINT_MONO_LDPTR);
				td->last_ins->data [0] = get_data_item_index (td, field);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
				interp_add_ins (td, MINT_LDC_I4);
				WRITE32_INS (td->last_ins, 0, &offset);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_I4);
#if SIZEOF_VOID_P == 8
				interp_add_ins (td, MINT_CONV_I8_I4);
#endif

				MonoMethod *wrapper = mono_marshal_get_ldflda_wrapper (field->type);
				/* td->ip is incremented by interp_transform_call */
				if (!interp_transform_call (td, method, wrapper, domain, generic_context, td->is_bb_start, NULL, FALSE, error, FALSE, FALSE))
					goto exit;
			} else
#endif
			{
				if (is_static) {
					interp_add_ins (td, MINT_POP);
					interp_emit_ldsflda (td, field, error);
					goto_if_nok (error, exit);
				} else {
					if ((td->sp - 1)->type == STACK_TYPE_O) {
						interp_add_ins (td, MINT_LDFLDA);
					} else {
						int sp_type = (td->sp - 1)->type;
						g_assert (sp_type == STACK_TYPE_MP || sp_type == STACK_TYPE_I);
						interp_add_ins (td, MINT_LDFLDA_UNSAFE);
					}
					td->last_ins->data [0] = m_class_is_valuetype (klass) ? field->offset - MONO_ABI_SIZEOF (MonoObject) : field->offset;
				}
				td->ip += 5;
			}
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);
			break;
		}
		case CEE_LDFLD: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			mono_class_init_internal (klass);

			MonoClass *field_klass = mono_class_from_mono_type_internal (ftype);
			mt = mint_type (m_class_get_byval_arg (field_klass));
			int field_size = mono_class_value_size (field_klass, NULL);
			field_size = ALIGN_TO (field_size, MINT_VT_ALIGNMENT);
			int obj_size = mono_class_value_size (klass, NULL);
			obj_size = ALIGN_TO (obj_size, MINT_VT_ALIGNMENT);

#ifndef DISABLE_REMOTING
			if ((m_class_get_marshalbyref (klass) && !(signature->hasthis && td->last_ins->opcode == MINT_LDLOC_O && td->last_ins->data [0] == 0)) ||
					mono_class_is_contextbound (klass) ||
					klass == mono_defaults.marshalbyrefobject_class) {
				g_assert (!is_static);
				interp_add_ins (td, mt == MINT_TYPE_VT ? MINT_LDRMFLD_VT :  MINT_LDRMFLD);
				td->last_ins->data [0] = get_data_item_index (td, field);
			} else
#endif
			{
				if (is_static) {
					interp_add_ins (td, MINT_POP);
					interp_emit_sfld_access (td, field, field_klass, mt, TRUE, error);
					goto_if_nok (error, exit);
				} else if (td->sp [-1].type == STACK_TYPE_VT) {
					/* First we pop the vt object from the stack. Then we push the field */
					int opcode = MINT_LDFLD_VT_I1 + mt - MINT_TYPE_I1;
#ifdef NO_UNALIGNED_ACCESS
					if (field->offset % SIZEOF_VOID_P != 0) {
						if (mt == MINT_TYPE_I8)
							opcode = MINT_LDFLD_VT_I8_UNALIGNED;
						else if (mt == MINT_TYPE_R8)
							opcode = MINT_LDFLD_VT_R8_UNALIGNED;
					}
#endif
					interp_add_ins (td, opcode);
					g_assert (m_class_is_valuetype (klass));
					td->last_ins->data [0] = field->offset - MONO_ABI_SIZEOF (MonoObject);
					td->last_ins->data [1] = obj_size;
					if (mt == MINT_TYPE_VT)
						td->last_ins->data [2] = field_size;
					POP_VT (td, obj_size);
				} else {
					int opcode = MINT_LDFLD_I1 + mt - MINT_TYPE_I1;
#ifdef NO_UNALIGNED_ACCESS
					if ((mt == MINT_TYPE_I8 || mt == MINT_TYPE_R8) && field->offset % SIZEOF_VOID_P != 0)
						opcode = get_unaligned_opcode (opcode);
#endif
					interp_add_ins (td, opcode);
					td->last_ins->data [0] = m_class_is_valuetype (klass) ? field->offset - MONO_ABI_SIZEOF (MonoObject) : field->offset;
					if (mt == MINT_TYPE_VT) {
						int size = mono_class_value_size (field_klass, NULL);
						WRITE32_INS (td->last_ins, 1, &size);
					}
				}
			}
			if (mt == MINT_TYPE_VT)
				PUSH_VT (td, field_size);
			td->ip += 5;
			SET_TYPE (td->sp - 1, stack_type [mt], field_klass);
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		}
		case CEE_STFLD: {
			CHECK_STACK (td, 2);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			MonoClass *field_klass = mono_class_from_mono_type_internal (ftype);
			mono_class_init_internal (klass);
			mt = mint_type (ftype);

			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);

#ifndef DISABLE_REMOTING
			if (m_class_get_marshalbyref (klass)) {
				g_assert (!is_static);
				interp_add_ins (td, mt == MINT_TYPE_VT ? MINT_STRMFLD_VT : MINT_STRMFLD);
				td->last_ins->data [0] = get_data_item_index (td, field);
			} else
#endif
			{
				if (is_static) {
					interp_emit_sfld_access (td, field, field_klass, mt, FALSE, error);
					goto_if_nok (error, exit);

					/* pop the unused object reference */
					interp_add_ins (td, MINT_POP);

					/* the vtable of the field might not be initialized at this point */
					mono_class_vtable_checked (domain, field_klass, error);
					goto_if_nok (error, exit);
				} else {
					int opcode = MINT_STFLD_I1 + mt - MINT_TYPE_I1;
#ifdef NO_UNALIGNED_ACCESS
					if ((mt == MINT_TYPE_I8 || mt == MINT_TYPE_R8) && field->offset % SIZEOF_VOID_P != 0)
						opcode = get_unaligned_opcode (opcode);
#endif
					interp_add_ins (td, opcode);
					td->last_ins->data [0] = m_class_is_valuetype (klass) ? field->offset - MONO_ABI_SIZEOF (MonoObject) : field->offset;
					if (mt == MINT_TYPE_VT) {
						/* the vtable of the field might not be initialized at this point */
						mono_class_vtable_checked (domain, field_klass, error);
						goto_if_nok (error, exit);
						if (m_class_has_references (field_klass)) {
							td->last_ins->data [1] = get_data_item_index (td, field_klass);
						} else {
							td->last_ins->opcode = MINT_STFLD_VT_NOREF;
							td->last_ins->data [1] = mono_class_value_size (field_klass, NULL);
						}
					}
				}
			}
			if (mt == MINT_TYPE_VT) {
				int size = mono_class_value_size (field_klass, NULL);
				POP_VT (td, size);
			}
			td->ip += 5;
			td->sp -= 2;
			break;
		}
		case CEE_LDSFLDA: {
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			interp_emit_ldsflda (td, field, error);
			goto_if_nok (error, exit);
			td->ip += 5;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_MP);
			break;
		}
		case CEE_LDSFLD: {
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			mt = mint_type (ftype);
			klass = mono_class_from_mono_type_internal (ftype);

			interp_emit_sfld_access (td, field, klass, mt, TRUE, error);
			goto_if_nok (error, exit);

			if (mt == MINT_TYPE_VT) {
				int size = mono_class_value_size (klass, NULL);
				PUSH_VT(td, size);
			}
			td->ip += 5;
			PUSH_TYPE(td, stack_type [mt], klass);
			break;
		}
		case CEE_STSFLD: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			mt = mint_type (ftype);

			emit_convert (td, td->sp [-1].type, ftype);

			/* the vtable of the field might not be initialized at this point */
			MonoClass *fld_klass = mono_class_from_mono_type_internal (ftype);
			mono_class_vtable_checked (domain, fld_klass, error);
			goto_if_nok (error, exit);

			interp_emit_sfld_access (td, field, fld_klass, mt, FALSE, error);
			goto_if_nok (error, exit);

			if (mt == MINT_TYPE_VT) {
				int size = mono_class_value_size (fld_klass, NULL);
				POP_VT(td, size);
			}
			td->ip += 5;
			--td->sp;
			break;
		}
		case CEE_STOBJ: {
			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);

			interp_emit_stobj (td, klass);

			td->ip += 5;
			break;
		}
		case CEE_CONV_OVF_I_UN:
		case CEE_CONV_OVF_U_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
#if SIZEOF_VOID_P == 8
				interp_add_ins (td, MINT_CONV_OVF_I8_UN_R8);
#else
				interp_add_ins (td, MINT_CONV_OVF_I4_UN_R8);
#endif
				break;
			case STACK_TYPE_I8:
#if SIZEOF_VOID_P == 4
				interp_add_ins (td, MINT_CONV_OVF_I4_U8);
#endif
				break;
			case STACK_TYPE_I4:
#if SIZEOF_VOID_P == 8
				interp_add_ins (td, MINT_CONV_I8_U4);
#elif SIZEOF_VOID_P == 4
				if (*td->ip == CEE_CONV_OVF_I_UN)
					interp_add_ins (td, MINT_CONV_OVF_I4_U4);
#endif
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I);
			++td->ip;
			break;
		case CEE_CONV_OVF_I8_UN:
		case CEE_CONV_OVF_U8_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_OVF_I8_UN_R8);
				break;
			case STACK_TYPE_I8:
				if (*td->ip == CEE_CONV_OVF_I8_UN)
					interp_add_ins (td, MINT_CONV_OVF_I8_U8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_I8_U4);
				break;
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_OVF_I8_UN_R4);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
			++td->ip;
			break;
		case CEE_BOX: {
			int size;
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method = mono_class_get_method_from_name_checked (klass, "Box", 1, 0, error);
				goto_if_nok (error, exit);
				/* td->ip is incremented by interp_transform_call */
				if (!interp_transform_call (td, method, target_method, domain, generic_context, td->is_bb_start, NULL, FALSE, error, FALSE, FALSE))
					goto exit;
			} else if (!m_class_is_valuetype (klass)) {
				/* already boxed, do nothing. */
				td->ip += 5;
			} else {
				if (G_UNLIKELY (m_class_is_byreflike (klass))) {
					mono_error_set_bad_image (error, image, "Cannot box IsByRefLike type '%s.%s'", m_class_get_name_space (klass), m_class_get_name (klass));
					goto exit;
				}

				const gboolean vt = mint_type (m_class_get_byval_arg (klass)) == MINT_TYPE_VT;

				if (vt) {
					size = mono_class_value_size (klass, NULL);
					size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
					td->vt_sp -= size;
				} else if (td->sp [-1].type == STACK_TYPE_R8 && m_class_get_byval_arg (klass)->type == MONO_TYPE_R4) {
					interp_add_ins (td, MINT_CONV_R4_R8);
				}
				MonoVTable *vtable = mono_class_vtable_checked (domain, klass, error);
				goto_if_nok (error, exit);

				interp_add_ins (td, vt ? MINT_BOX_VT : MINT_BOX);
				td->last_ins->data [0] = get_data_item_index (td, vtable);
				td->last_ins->data [1] = 0;
				SET_TYPE(td->sp - 1, STACK_TYPE_O, klass);
				td->ip += 5;
			}

			break;
		}
		case CEE_NEWARR: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			MonoClass *array_class = mono_class_create_array (klass, 1);
			MonoVTable *vtable = mono_class_vtable_checked (domain, array_class, error);
			goto_if_nok (error, exit);

			unsigned char lentype = (td->sp - 1)->type;
			if (lentype == STACK_TYPE_I8) {
				/* mimic mini behaviour */
				interp_add_ins (td, MINT_CONV_OVF_U4_I8);
			} else {
				g_assert (lentype == STACK_TYPE_I4);
				interp_add_ins (td, MINT_CONV_OVF_U4_I4);
			}
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
			interp_add_ins (td, MINT_NEWARR);
			td->last_ins->data [0] = get_data_item_index (td, vtable);
			SET_TYPE (td->sp - 1, STACK_TYPE_O, array_class);
			td->ip += 5;
			break;
		}
		case CEE_LDLEN:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDLEN);
#ifdef MONO_BIG_ARRAYS
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I8);
#else
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
#endif
			break;
		case CEE_LDELEMA: {
			gint32 size;
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *) mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);

			CHECK_TYPELOAD (klass);

			if (!m_class_is_valuetype (klass) && method->wrapper_type == MONO_WRAPPER_NONE && !readonly) {
				/*
				 * Check the class for failures before the type check, which can
				 * throw other exceptions.
				 */
				mono_class_setup_vtable (klass);
				CHECK_TYPELOAD (klass);
				interp_add_ins (td, MINT_LDELEMA_TC);
				td->last_ins->data [0] = 1;
				td->last_ins->data [1] = get_data_item_index (td, klass);
			} else {
				interp_add_ins (td, MINT_LDELEMA1);
				mono_class_init_internal (klass);
				size = mono_class_array_element_size (klass);
				WRITE32_INS (td->last_ins, 0, &size);
			}

			readonly = FALSE;

			td->ip += 5;
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);
			break;
		}
		case CEE_LDELEM_I1:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_I1);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_U1:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_U1);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_I2:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_I2);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_U2:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_U2);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_I4:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_I4);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_U4:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_U4);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_I8:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_I8);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
			break;
		case CEE_LDELEM_I:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_I);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I);
			break;
		case CEE_LDELEM_R4:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_R4);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R4);
			break;
		case CEE_LDELEM_R8:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_R8);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
			break;
		case CEE_LDELEM_REF:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			SIMPLE_OP (td, MINT_LDELEM_REF);
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_O);
			break;
		case CEE_LDELEM:
			CHECK_STACK (td, 2);
			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			switch (mint_type (m_class_get_byval_arg (klass))) {
				case MINT_TYPE_I1:
					ENSURE_I4 (td, 1);
					SIMPLE_OP (td, MINT_LDELEM_I1);
					--td->sp;
					SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
					break;
				case MINT_TYPE_U1:
					ENSURE_I4 (td, 1);
					SIMPLE_OP (td, MINT_LDELEM_U1);
					--td->sp;
					SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
					break;
				case MINT_TYPE_U2:
					ENSURE_I4 (td, 1);
					SIMPLE_OP (td, MINT_LDELEM_U2);
					--td->sp;
					SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
					break;
				case MINT_TYPE_I2:
					ENSURE_I4 (td, 1);
					SIMPLE_OP (td, MINT_LDELEM_I2);
					--td->sp;
					SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
					break;
				case MINT_TYPE_I4:
					ENSURE_I4 (td, 1);
					SIMPLE_OP (td, MINT_LDELEM_I4);
					--td->sp;
					SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
					break;
				case MINT_TYPE_I8:
					ENSURE_I4 (td, 1);
					SIMPLE_OP (td, MINT_LDELEM_I8);
					--td->sp;
					SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
					break;
				case MINT_TYPE_R4:
					ENSURE_I4 (td, 1);
					SIMPLE_OP (td, MINT_LDELEM_R4);
					--td->sp;
					SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R4);
					break;
				case MINT_TYPE_R8:
					ENSURE_I4 (td, 1);
					SIMPLE_OP (td, MINT_LDELEM_R8);
					--td->sp;
					SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
					break;
				case MINT_TYPE_O:
					ENSURE_I4 (td, 1);
					SIMPLE_OP (td, MINT_LDELEM_REF);
					--td->sp;
					SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_O);
					break;
				case MINT_TYPE_VT: {
					int size = mono_class_value_size (klass, NULL);
					ENSURE_I4 (td, 1);
					SIMPLE_OP (td, MINT_LDELEM_VT);
					WRITE32_INS (td->last_ins, 0, &size);
					--td->sp;
					SET_TYPE (td->sp - 1, STACK_TYPE_VT, klass);
					PUSH_VT (td, size);
					break;
				}
				default: {
					GString *res = g_string_new ("");
					mono_type_get_desc (res, m_class_get_byval_arg (klass), TRUE);
					g_print ("LDELEM: %s -> %d (%s)\n", m_class_get_name (klass), mint_type (m_class_get_byval_arg (klass)), res->str);
					g_string_free (res, TRUE);
					g_assert (0);
					break;
				}
			}
			td->ip += 4;
			break;
		case CEE_STELEM_I:
			CHECK_STACK (td, 3);
			ENSURE_I4 (td, 2);
			SIMPLE_OP (td, MINT_STELEM_I);
			td->sp -= 3;
			break;
		case CEE_STELEM_I1:
			CHECK_STACK (td, 3);
			ENSURE_I4 (td, 2);
			SIMPLE_OP (td, MINT_STELEM_I1);
			td->sp -= 3;
			break;
		case CEE_STELEM_I2:
			CHECK_STACK (td, 3);
			ENSURE_I4 (td, 2);
			SIMPLE_OP (td, MINT_STELEM_I2);
			td->sp -= 3;
			break;
		case CEE_STELEM_I4:
			CHECK_STACK (td, 3);
			ENSURE_I4 (td, 2);
			SIMPLE_OP (td, MINT_STELEM_I4);
			td->sp -= 3;
			break;
		case CEE_STELEM_I8:
			CHECK_STACK (td, 3);
			ENSURE_I4 (td, 2);
			SIMPLE_OP (td, MINT_STELEM_I8);
			td->sp -= 3;
			break;
		case CEE_STELEM_R4:
			CHECK_STACK (td, 3);
			ENSURE_I4 (td, 2);
			SIMPLE_OP (td, MINT_STELEM_R4);
			td->sp -= 3;
			break;
		case CEE_STELEM_R8:
			CHECK_STACK (td, 3);
			ENSURE_I4 (td, 2);
			SIMPLE_OP (td, MINT_STELEM_R8);
			td->sp -= 3;
			break;
		case CEE_STELEM_REF:
			CHECK_STACK (td, 3);
			ENSURE_I4 (td, 2);
			SIMPLE_OP (td, MINT_STELEM_REF);
			td->sp -= 3;
			break;
		case CEE_STELEM:
			CHECK_STACK (td, 3);
			ENSURE_I4 (td, 2);
			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			switch (mint_type (m_class_get_byval_arg (klass))) {
				case MINT_TYPE_I1:
					SIMPLE_OP (td, MINT_STELEM_I1);
					break;
				case MINT_TYPE_U1:
					SIMPLE_OP (td, MINT_STELEM_U1);
					break;
				case MINT_TYPE_I2:
					SIMPLE_OP (td, MINT_STELEM_I2);
					break;
				case MINT_TYPE_U2:
					SIMPLE_OP (td, MINT_STELEM_U2);
					break;
				case MINT_TYPE_I4:
					SIMPLE_OP (td, MINT_STELEM_I4);
					break;
				case MINT_TYPE_I8:
					SIMPLE_OP (td, MINT_STELEM_I8);
					break;
				case MINT_TYPE_R4:
					SIMPLE_OP (td, MINT_STELEM_R4);
					break;
				case MINT_TYPE_R8:
					SIMPLE_OP (td, MINT_STELEM_R8);
					break;
				case MINT_TYPE_O:
					SIMPLE_OP (td, MINT_STELEM_REF);
					break;
				case MINT_TYPE_VT: {
					int size = mono_class_value_size (klass, NULL);
					SIMPLE_OP (td, MINT_STELEM_VT);
					td->last_ins->data [0] = get_data_item_index (td, klass);
					WRITE32_INS (td->last_ins, 1, &size);
					POP_VT (td, size);
					break;
				}
				default: {
					GString *res = g_string_new ("");
					mono_type_get_desc (res, m_class_get_byval_arg (klass), TRUE);
					g_print ("STELEM: %s -> %d (%s)\n", m_class_get_name (klass), mint_type (m_class_get_byval_arg (klass)), res->str);
					g_string_free (res, TRUE);
					g_assert (0);
					break;
				}
			}
			td->ip += 4;
			td->sp -= 3;
			break;
#if 0
		case CEE_CONV_OVF_U1:

		case CEE_CONV_OVF_I8:

#if SIZEOF_VOID_P == 8
		case CEE_CONV_OVF_U:
#endif
#endif
		case CEE_CKFINITE:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_CKFINITE);
			break;
		case CEE_MKREFANY:
			CHECK_STACK (td, 1);

			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			interp_add_ins (td, MINT_MKREFANY);
			td->last_ins->data [0] = get_data_item_index (td, klass);

			td->ip += 5;
			PUSH_VT (td, sizeof (MonoTypedRef));
			SET_TYPE(td->sp - 1, STACK_TYPE_VT, mono_defaults.typed_reference_class);
			break;
		case CEE_REFANYVAL: {
			CHECK_STACK (td, 1);

			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			interp_add_ins (td, MINT_REFANYVAL);
			td->last_ins->data [0] = get_data_item_index (td, klass);

			POP_VT (td, sizeof (MonoTypedRef));
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);

			td->ip += 5;
			break;
		}
		case CEE_CONV_OVF_I1:
		case CEE_CONV_OVF_I1_UN: {
			gboolean is_un = *td->ip == CEE_CONV_OVF_I1_UN;
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, is_un ? MINT_CONV_OVF_I1_UN_R4 : MINT_CONV_OVF_I1_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, is_un ? MINT_CONV_OVF_I1_UN_R8 : MINT_CONV_OVF_I1_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, is_un ? MINT_CONV_OVF_I1_U4 : MINT_CONV_OVF_I1_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, is_un ? MINT_CONV_OVF_I1_U8 : MINT_CONV_OVF_I1_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		}
		case CEE_CONV_OVF_U1:
		case CEE_CONV_OVF_U1_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_OVF_U1_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_OVF_U1_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_OVF_U1_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_OVF_U1_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_CONV_OVF_I2:
		case CEE_CONV_OVF_I2_UN: {
			gboolean is_un = *td->ip == CEE_CONV_OVF_I2_UN;
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, is_un ? MINT_CONV_OVF_I2_UN_R4 : MINT_CONV_OVF_I2_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, is_un ? MINT_CONV_OVF_I2_UN_R8 : MINT_CONV_OVF_I2_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, is_un ? MINT_CONV_OVF_I2_U4 : MINT_CONV_OVF_I2_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, is_un ? MINT_CONV_OVF_I2_U8 : MINT_CONV_OVF_I2_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		}
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_U2:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_OVF_U2_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_OVF_U2_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_OVF_U2_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_OVF_U2_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
#if SIZEOF_VOID_P == 4
		case CEE_CONV_OVF_I:
#endif
		case CEE_CONV_OVF_I4:
		case CEE_CONV_OVF_I4_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_OVF_I4_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_OVF_I4_R8);
				break;
			case STACK_TYPE_I4:
				if (*td->ip == CEE_CONV_OVF_I4_UN)
					interp_add_ins (td, MINT_CONV_OVF_I4_U4);
				break;
			case STACK_TYPE_I8:
				if (*td->ip == CEE_CONV_OVF_I4_UN)
					interp_add_ins (td, MINT_CONV_OVF_I4_U8);
				else
					interp_add_ins (td, MINT_CONV_OVF_I4_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
#if SIZEOF_VOID_P == 4
		case CEE_CONV_OVF_U:
#endif
		case CEE_CONV_OVF_U4:
		case CEE_CONV_OVF_U4_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_OVF_U4_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_OVF_U4_R8);
				break;
			case STACK_TYPE_I4:
				if (*td->ip != CEE_CONV_OVF_U4_UN)
					interp_add_ins (td, MINT_CONV_OVF_U4_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_OVF_U4_I8);
				break;
			case STACK_TYPE_MP:
				interp_add_ins (td, MINT_CONV_OVF_U4_P);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
#if SIZEOF_VOID_P == 8
		case CEE_CONV_OVF_I:
#endif
		case CEE_CONV_OVF_I8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_OVF_I8_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_OVF_I8_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_I8_I4);
				break;
			case STACK_TYPE_I8:
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
			break;
#if SIZEOF_VOID_P == 8
		case CEE_CONV_OVF_U:
#endif
		case CEE_CONV_OVF_U8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_ins (td, MINT_CONV_OVF_U8_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_ins (td, MINT_CONV_OVF_U8_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_ins (td, MINT_CONV_OVF_U8_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_ins (td, MINT_CONV_OVF_U8_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
			break;
		case CEE_LDTOKEN: {
			int size;
			gpointer handle;
			token = read32 (td->ip + 1);
			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD || method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED) {
				handle = mono_method_get_wrapper_data (method, token);
				klass = (MonoClass *) mono_method_get_wrapper_data (method, token + 1);
				if (klass == mono_defaults.typehandle_class)
					handle = m_class_get_byval_arg ((MonoClass *) handle);

				if (generic_context) {
					handle = mono_class_inflate_generic_type_checked ((MonoType*)handle, generic_context, error);
					goto_if_nok (error, exit);
				}
			} else {
				handle = mono_ldtoken_checked (image, token, &klass, generic_context, error);
				goto_if_nok (error, exit);
			}
			mono_class_init_internal (klass);
			mt = mint_type (m_class_get_byval_arg (klass));
			g_assert (mt == MINT_TYPE_VT);
			size = mono_class_value_size (klass, NULL);
			g_assert (size == sizeof(gpointer));

			const unsigned char *next_ip = td->ip + 5;
			MonoMethod *cmethod;
			if (next_ip < end &&
					(inlining || !td->is_bb_start [next_ip - td->il_code]) &&
					(*next_ip == CEE_CALL || *next_ip == CEE_CALLVIRT) &&
					(cmethod = interp_get_method (method, read32 (next_ip + 1), image, generic_context, error)) &&
					(cmethod->klass == mono_defaults.systemtype_class) &&
					(strcmp (cmethod->name, "GetTypeFromHandle") == 0)) {
				const unsigned char *next_next_ip = next_ip + 5;
				MonoMethod *next_cmethod;
				MonoClass *tclass = mono_class_from_mono_type_internal ((MonoType *)handle);
				// Optimize to true/false if next instruction is `call instance bool Type::get_IsValueType()`
				if (next_next_ip < end &&
						(inlining || !td->is_bb_start [next_next_ip - td->il_code]) &&
						(*next_next_ip == CEE_CALL || *next_next_ip == CEE_CALLVIRT) &&
						(next_cmethod = interp_get_method (method, read32 (next_next_ip + 1), image, generic_context, error)) &&
						(next_cmethod->klass == mono_defaults.systemtype_class) &&
						!strcmp (next_cmethod->name, "get_IsValueType")) {
					g_assert (!mono_class_is_open_constructed_type (m_class_get_byval_arg (tclass)));
					if (m_class_is_valuetype (tclass))
						interp_add_ins (td, MINT_LDC_I4_1);
					else
						interp_add_ins (td, MINT_LDC_I4_0);
					PUSH_SIMPLE_TYPE (td, STACK_TYPE_I4);
					td->ip = next_next_ip + 5;
					break;
				}

				interp_add_ins (td, MINT_MONO_LDPTR);
				gpointer systype = mono_type_get_object_checked (domain, (MonoType*)handle, error);
				goto_if_nok (error, exit);
				td->last_ins->data [0] = get_data_item_index (td, systype);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_MP);
				td->ip = next_ip + 5;
			} else {
				PUSH_VT (td, sizeof(gpointer));
				interp_add_ins (td, MINT_LDTOKEN);
				td->last_ins->data [0] = get_data_item_index (td, handle);
				PUSH_TYPE (td, stack_type [mt], klass);
				td->ip += 5;
			}

			break;
		}
		case CEE_ADD_OVF:
			binary_arith_op(td, MINT_ADD_OVF_I4);
			++td->ip;
			break;
		case CEE_ADD_OVF_UN:
			binary_arith_op(td, MINT_ADD_OVF_UN_I4);
			++td->ip;
			break;
		case CEE_MUL_OVF:
			binary_arith_op(td, MINT_MUL_OVF_I4);
			++td->ip;
			break;
		case CEE_MUL_OVF_UN:
			binary_arith_op(td, MINT_MUL_OVF_UN_I4);
			++td->ip;
			break;
		case CEE_SUB_OVF:
			binary_arith_op(td, MINT_SUB_OVF_I4);
			++td->ip;
			break;
		case CEE_SUB_OVF_UN:
			binary_arith_op(td, MINT_SUB_OVF_UN_I4);
			++td->ip;
			break;
		case CEE_ENDFINALLY: {
			g_assert (td->clause_indexes [in_offset] != -1);
			td->sp = td->stack;
			SIMPLE_OP (td, MINT_ENDFINALLY);
			td->last_ins->data [0] = td->clause_indexes [in_offset];
			// next instructions, if they exist, are always part of new bb
			// endfinally can be the last instruction in a function.
			// functions with clauses/endfinally are never inlined.
			// is_bb_start is not valid while inlining.
			g_assert (!inlining);
			if (td->ip - td->il_code < td->code_size)
				td->is_bb_start [td->ip - header->code] = 1;
			break;
		}
		case CEE_LEAVE:
		case CEE_LEAVE_S: {
			int offset;

			if (*td->ip == CEE_LEAVE)
				offset = 5 + read32 (td->ip + 1);
			else
				offset = 2 + (gint8)td->ip [1];

			td->sp = td->stack;
			if (td->clause_indexes [in_offset] != -1) {
				/* LEAVE instructions in catch clauses need to check for abort exceptions */
				handle_branch (td, MINT_LEAVE_S_CHECK, MINT_LEAVE_CHECK, offset);
			} else {
				handle_branch (td, MINT_LEAVE_S, MINT_LEAVE, offset);
			}

			if (*td->ip == CEE_LEAVE)
				td->ip += 5;
			else
				td->ip += 2;
			break;
		}
		case MONO_CUSTOM_PREFIX:
			++td->ip;
		        switch (*td->ip) {
				case CEE_MONO_RETHROW:
					CHECK_STACK (td, 1);
					SIMPLE_OP (td, MINT_MONO_RETHROW);
					td->sp = td->stack;
					break;

				case CEE_MONO_LD_DELEGATE_METHOD_PTR:
					--td->sp;
					td->ip += 1;
					interp_add_ins (td, MINT_LD_DELEGATE_METHOD_PTR);
					PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
					break;
				case CEE_MONO_CALLI_EXTRA_ARG:
					/* Same as CEE_CALLI, except that we drop the extra arg required for llvm specific behaviour */
					interp_add_ins (td, MINT_POP1);
					--td->sp;
					if (!interp_transform_call (td, method, NULL, domain, generic_context, td->is_bb_start, NULL, FALSE, error, FALSE, FALSE))
						goto exit;
					break;
				case CEE_MONO_JIT_ICALL_ADDR: {
					const guint32 token = read32 (td->ip + 1);
					td->ip += 5;
					const gconstpointer func = mono_find_jit_icall_info ((MonoJitICallId)token)->func;

					interp_add_ins (td, MINT_LDFTN);
					td->last_ins->data [0] = get_data_item_index (td, (gpointer)func);
					PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
					break;
				}
				case CEE_MONO_ICALL: {
					MonoJitICallId const jit_icall_id = (MonoJitICallId)read32 (td->ip + 1);
					MonoJitICallInfo const * const info = mono_find_jit_icall_info (jit_icall_id);
					td->ip += 5;

					CHECK_STACK (td, info->sig->param_count);
					if (jit_icall_id == MONO_JIT_ICALL_mono_threads_attach_coop) {
						rtm->needs_thread_attach = 1;

						/* attach needs two arguments, and has one return value: leave one element on the stack */
						interp_add_ins (td, MINT_POP);
					} else if (jit_icall_id == MONO_JIT_ICALL_mono_threads_detach_coop) {
						g_assert (rtm->needs_thread_attach);

						/* detach consumes two arguments, and no return value: drop both of them */
						interp_add_ins (td, MINT_POP);
						interp_add_ins (td, MINT_POP);
					} else {
						int const icall_op = interp_icall_op_for_sig (info->sig);
						g_assert (icall_op != -1);

						interp_add_ins (td, icall_op);
						// hash here is overkill
						td->last_ins->data [0] = get_data_item_index (td, (gpointer)info->func);
					}
					td->sp -= info->sig->param_count;

					if (!MONO_TYPE_IS_VOID (info->sig->ret)) {
						int mt = mint_type (info->sig->ret);
						PUSH_SIMPLE_TYPE(td, stack_type [mt]);
					}
					break;
				}
			case CEE_MONO_VTADDR: {
				int size;
				CHECK_STACK (td, 1);
				if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE)
					size = mono_class_native_size(td->sp [-1].klass, NULL);
				else
					size = mono_class_value_size(td->sp [-1].klass, NULL);
				size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
				interp_add_ins (td, MINT_VTRESULT);
				td->last_ins->data [0] = 0;
				WRITE32_INS (td->last_ins, 1, &size);
				td->vt_sp -= size;
				++td->ip;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);
				break;
			}
			case CEE_MONO_LDPTR:
			case CEE_MONO_CLASSCONST:
				token = read32 (td->ip + 1);
				td->ip += 5;
				interp_add_ins (td, MINT_MONO_LDPTR);
				td->last_ins->data [0] = get_data_item_index (td, mono_method_get_wrapper_data (method, token));
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
				break;
			case CEE_MONO_OBJADDR:
				CHECK_STACK (td, 1);
				++td->ip;
				td->sp[-1].type = STACK_TYPE_MP;
				/* do nothing? */
				break;
			case CEE_MONO_NEWOBJ:
				token = read32 (td->ip + 1);
				td->ip += 5;
				interp_add_ins (td, MINT_MONO_NEWOBJ);
				td->last_ins->data [0] = get_data_item_index (td, mono_method_get_wrapper_data (method, token));
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_O);
				break;
			case CEE_MONO_RETOBJ:
				CHECK_STACK (td, 1);
				token = read32 (td->ip + 1);
				td->ip += 5;
				interp_add_ins (td, MINT_MONO_RETOBJ);
				td->sp--;

				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
				
				/*stackval_from_data (signature->ret, frame->retval, sp->data.vt, signature->pinvoke);*/

				if (td->sp > td->stack)
					g_warning ("CEE_MONO_RETOBJ: more values on stack: %d", td->sp-td->stack);
				break;
			case CEE_MONO_LDNATIVEOBJ:
				token = read32 (td->ip + 1);
				td->ip += 5;
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
				g_assert(m_class_is_valuetype (klass));
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);
				break;
			case CEE_MONO_TLS: {
				gint32 key = read32 (td->ip + 1);
				td->ip += 5;
				g_assertf (key == TLS_KEY_SGEN_THREAD_INFO, "%d", key);
				interp_add_ins (td, MINT_MONO_SGEN_THREAD_INFO);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_MP);
				break;
			}
			case CEE_MONO_ATOMIC_STORE_I4:
				CHECK_STACK (td, 2);
				SIMPLE_OP (td, MINT_MONO_ATOMIC_STORE_I4);
				td->sp -= 2;
				td->ip++;
				break;
			case CEE_MONO_SAVE_LMF:
			case CEE_MONO_RESTORE_LMF:
			case CEE_MONO_NOT_TAKEN:
				++td->ip;
				break;
			case CEE_MONO_LDPTR_INT_REQ_FLAG:
				interp_add_ins (td, MINT_MONO_LDPTR);
				td->last_ins->data [0] = get_data_item_index (td, &mono_thread_interruption_request_flag);
				PUSH_TYPE (td, STACK_TYPE_MP, NULL);
				++td->ip;
				break;
			case CEE_MONO_MEMORY_BARRIER:
				interp_add_ins (td, MINT_MONO_MEMORY_BARRIER);
				++td->ip;
				break;
			case CEE_MONO_LDDOMAIN:
				interp_add_ins (td, MINT_MONO_LDDOMAIN);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
				++td->ip;
				break;
			case CEE_MONO_SAVE_LAST_ERROR:
				save_last_error = TRUE;
				++td->ip;
				break;
			case CEE_MONO_GET_SP:
				interp_add_ins (td, MINT_MONO_GET_SP);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
				++td->ip;
				break;
			default:
				g_error ("transform.c: Unimplemented opcode: 0xF0 %02x at 0x%x\n", *td->ip, td->ip-header->code);
			}
			break;
#if 0
		case CEE_PREFIX7:
		case CEE_PREFIX6:
		case CEE_PREFIX5:
		case CEE_PREFIX4:
		case CEE_PREFIX3:
		case CEE_PREFIX2:
		case CEE_PREFIXREF: ves_abort(); break;
#endif
		/*
		 * Note: Exceptions thrown when executing a prefixed opcode need
		 * to take into account the number of prefix bytes (usually the
		 * throw point is just (ip - n_prefix_bytes).
		 */
		case CEE_PREFIX1: 
			++td->ip;
			switch (*td->ip) {
			case CEE_ARGLIST:
				interp_add_ins (td, MINT_ARGLIST);
				PUSH_VT (td, SIZEOF_VOID_P);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_VT);
				++td->ip;
				break;
			case CEE_CEQ:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP) {
					interp_add_ins (td, MINT_CEQ_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				} else {
					if (td->sp [-1].type == STACK_TYPE_R4 && td->sp [-2].type == STACK_TYPE_R8)
						interp_add_ins (td, MINT_CONV_R8_R4);
					if (td->sp [-1].type == STACK_TYPE_R8 && td->sp [-2].type == STACK_TYPE_R4)
						interp_add_ins (td, MINT_CONV_R8_R4_SP);
					interp_add_ins (td, MINT_CEQ_I4 + td->sp [-1].type - STACK_TYPE_I4);
				}
				--td->sp;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
				++td->ip;
				break;
			case CEE_CGT:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					interp_add_ins (td, MINT_CGT_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					interp_add_ins (td, MINT_CGT_I4 + td->sp [-1].type - STACK_TYPE_I4);
				--td->sp;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
				++td->ip;
				break;
			case CEE_CGT_UN:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					interp_add_ins (td, MINT_CGT_UN_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					interp_add_ins (td, MINT_CGT_UN_I4 + td->sp [-1].type - STACK_TYPE_I4);
				--td->sp;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
				++td->ip;
				break;
			case CEE_CLT:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					interp_add_ins (td, MINT_CLT_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					interp_add_ins (td, MINT_CLT_I4 + td->sp [-1].type - STACK_TYPE_I4);
				--td->sp;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
				++td->ip;
				break;
			case CEE_CLT_UN:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					interp_add_ins (td, MINT_CLT_UN_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					interp_add_ins (td, MINT_CLT_UN_I4 + td->sp [-1].type - STACK_TYPE_I4);
				--td->sp;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
				++td->ip;
				break;
			case CEE_LDVIRTFTN: /* fallthrough */
			case CEE_LDFTN: {
				MonoMethod *m;
				if (*td->ip == CEE_LDVIRTFTN) {
					CHECK_STACK (td, 1);
					--td->sp;
				}
				token = read32 (td->ip + 1);
				m = interp_get_method (method, token, image, generic_context, error);
				goto_if_nok (error, exit);

				if (!mono_method_can_access_method (method, m))
					interp_generate_mae_throw (td, method, m);

				if (method->wrapper_type == MONO_WRAPPER_NONE && m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
					m = mono_marshal_get_synchronized_wrapper (m);

				if (G_UNLIKELY (*td->ip == CEE_LDFTN &&
						m->wrapper_type == MONO_WRAPPER_NONE &&
						mono_method_has_unmanaged_callers_only_attribute (m))) {

					if (m->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
						interp_generate_not_supported_throw (td);
						interp_add_ins (td, MINT_LDNULL);
						td->ip += 5;
						PUSH_SIMPLE_TYPE (td, STACK_TYPE_MP);
						break;
					}

					MonoMethod *ctor_method;

					const unsigned char *next_ip = td->ip + 5;
					/* check for
					 *    ldftn method_sig
					 *    newobj Delegate::.ctor
					 */
					if (next_ip < end &&
					    *next_ip == CEE_NEWOBJ &&
					    ((ctor_method = interp_get_method (method, read32 (next_ip + 1), image, generic_context, error))) &&
					    is_ok (error) &&
					    m_class_get_parent (ctor_method->klass) == mono_defaults.multicastdelegate_class &&
					    !strcmp (ctor_method->name, ".ctor")) {
						mono_error_set_not_supported (error, "Cannot create delegate from method with UnmanagedCallersOnlyAttribute");
						goto exit;
					}

					MonoClass *delegate_klass = NULL;
					MonoGCHandle target_handle = 0;
					ERROR_DECL (wrapper_error);
					m = mono_marshal_get_managed_wrapper (m, delegate_klass, target_handle, wrapper_error);
					if (!is_ok (wrapper_error)) {
						/* Generate a call that will throw an exception if the
						 * UnmanagedCallersOnly attribute is used incorrectly */
						interp_generate_ipe_throw_with_msg (td, wrapper_error);
						mono_error_cleanup (wrapper_error);
						interp_add_ins (td, MINT_LDNULL);
					} else {
						/* push a pointer to a trampoline that calls m */
						gpointer entry = mini_get_interp_callbacks ()->create_method_pointer (m, TRUE, error);
#if SIZEOF_VOID_P == 8
						interp_add_ins (td, MINT_LDC_I8);
						WRITE64_INS (td->last_ins, 0, &entry);
#else
						interp_add_ins (td, MINT_LDC_I4);
						WRITE32_INS (td->last_ins, 0, &entry);
#endif
					}
					td->ip += 5;
					PUSH_SIMPLE_TYPE (td, STACK_TYPE_MP);
					break;
				}

				interp_add_ins (td, *td->ip == CEE_LDFTN ? MINT_LDFTN : MINT_LDVIRTFTN);
				td->last_ins->data [0] = get_data_item_index (td, mono_interp_get_imethod (domain, m, error));
				goto_if_nok (error, exit);
				td->ip += 5;
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_F);
				break;
			}
			case CEE_LDARG: {
				int arg_n = read16 (td->ip + 1);
				if (!inlining)
					load_arg (td, arg_n);
				else
					load_local (td, arg_locals [arg_n]);
				td->ip += 3;
				break;
			}
			case CEE_LDARGA: {
				int n = read16 (td->ip + 1);

				if (!inlining) {
					get_arg_type_exact (td, n, &mt);
					if (mt == MINT_TYPE_VT) {
						interp_add_ins (td, MINT_LDARGA_VT);
					} else {
						interp_add_ins (td, MINT_LDLOCA_S);
						td->locals [n].indirects++;
					}
					td->last_ins->data [0] = n;
				} else {
					int loc_n = arg_locals [n];
					interp_add_ins (td, MINT_LDLOCA_S);
					td->last_ins->data [0] = loc_n;
					td->locals [loc_n].indirects++;
				}
				PUSH_SIMPLE_TYPE(td, STACK_TYPE_MP);
				td->ip += 3;
				break;
			}
			case CEE_STARG: {
				int arg_n = read16 (td->ip + 1);
				if (!inlining)
					store_arg (td, arg_n);
				else
					store_local (td, arg_locals [arg_n]);
				td->ip += 3;
				break;
			}
			case CEE_LDLOC: {
				int loc_n = read16 (td->ip + 1);
				if (!inlining)
					load_local (td, num_args + loc_n);
				else
					load_local (td, local_locals [loc_n]);
				td->ip += 3;
				break;
			}
			case CEE_LDLOCA: {
				int loc_n = read16 (td->ip + 1);
				interp_add_ins (td, MINT_LDLOCA_S);
				if (!inlining)
					loc_n += num_args;
				else
					loc_n = local_locals [loc_n];
				td->last_ins->data [0] = loc_n;
				td->locals [loc_n].indirects++;
				PUSH_SIMPLE_TYPE(td, STACK_TYPE_MP);
				td->ip += 3;
				break;
			}
			case CEE_STLOC: {
				int loc_n = read16 (td->ip + 1);
				if (!inlining)
					store_local (td, num_args + loc_n);
				else
					store_local (td, local_locals [loc_n]);
				td->ip += 3;
				break;
			}
			case CEE_LOCALLOC:
				INLINE_FAILURE;
				CHECK_STACK (td, 1);
#if SIZEOF_VOID_P == 8
				if (td->sp [-1].type == STACK_TYPE_I8)
					interp_add_ins (td, MINT_CONV_I4_I8);
#endif				
				interp_add_ins (td, MINT_LOCALLOC);
				if (td->sp != td->stack + 1)
					g_warning("CEE_LOCALLOC: stack not empty");
				++td->ip;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);
				td->has_localloc = TRUE;
				break;
#if 0
			case CEE_UNUSED57: ves_abort(); break;
#endif
			case CEE_ENDFILTER:
				interp_add_ins (td, MINT_ENDFILTER);
				++td->ip;
				break;
			case CEE_UNALIGNED_:
				td->ip += 2;
				break;
			case CEE_VOLATILE_:
				++td->ip;
				volatile_ = TRUE;
				break;
			case CEE_TAIL_:
				++td->ip;
				/* FIX: should do something? */;
				// TODO: This should raise a method_tail_call profiler event.
				break;
			case CEE_INITOBJ:
				CHECK_STACK(td, 1);
				token = read32 (td->ip + 1);
				klass = mini_get_class (method, token, generic_context);
				CHECK_TYPELOAD (klass);
				if (m_class_is_valuetype (klass)) {
					interp_add_ins (td, MINT_INITOBJ);
					i32 = mono_class_value_size (klass, NULL);
					WRITE32_INS (td->last_ins, 0, &i32);
					--td->sp;
				} else {
					interp_add_ins (td, MINT_LDNULL);
					PUSH_TYPE(td, STACK_TYPE_O, NULL);
					interp_add_ins (td, MINT_STIND_REF);
					td->sp -= 2;
				}
				td->ip += 5;
				break;
			case CEE_CPBLK:
				CHECK_STACK(td, 3);
				/* FIX? convert length to I8? */
				if (volatile_)
					interp_add_ins (td, MINT_MONO_MEMORY_BARRIER);
				interp_add_ins (td, MINT_CPBLK);
				BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_SEQ);
				td->sp -= 3;
				++td->ip;
				break;
			case CEE_READONLY_:
				readonly = TRUE;
				td->ip += 1;
				break;
			case CEE_CONSTRAINED_:
				token = read32 (td->ip + 1);
				constrained_class = mini_get_class (method, token, generic_context);
				CHECK_TYPELOAD (constrained_class);
				td->ip += 5;
				break;
			case CEE_INITBLK:
				CHECK_STACK(td, 3);
				BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);
				interp_add_ins (td, MINT_INITBLK);
				td->sp -= 3;
				td->ip += 1;
				break;
			case CEE_NO_:
				/* FIXME: implement */
				td->ip += 2;
				break;
			case CEE_RETHROW: {
				int clause_index = td->clause_indexes [in_offset];
				g_assert (clause_index != -1);
				SIMPLE_OP (td, MINT_RETHROW);
				td->last_ins->data [0] = rtm->exvar_offsets [clause_index];
				td->sp = td->stack;
				break;
			}
			case CEE_SIZEOF: {
				gint32 size;
				token = read32 (td->ip + 1);
				td->ip += 5;
				if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC && !image_is_dynamic (m_class_get_image (method->klass)) && !generic_context) {
					int align;
					MonoType *type = mono_type_create_from_typespec_checked (image, token, error);
					goto_if_nok (error, exit);
					size = mono_type_size (type, &align);
				} else {
					int align;
					MonoClass *szclass = mini_get_class (method, token, generic_context);
					CHECK_TYPELOAD (szclass);
#if 0
					if (!szclass->valuetype)
						THROW_EX (mono_exception_from_name (mono_defaults.corlib, "System", "InvalidProgramException"), ip - 5);
#endif
					size = mono_type_size (m_class_get_byval_arg (szclass), &align);
				} 
				interp_add_ins (td, MINT_LDC_I4);
				WRITE32_INS (td->last_ins, 0, &size);
				PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
				break;
			}
			case CEE_REFANYTYPE:
				interp_add_ins (td, MINT_REFANYTYPE);
				td->ip += 1;
				POP_VT (td, sizeof (MonoTypedRef));
				PUSH_VT (td, sizeof (gpointer));
				SET_TYPE(td->sp - 1, STACK_TYPE_VT, NULL);
				break;
			default:
				g_error ("transform.c: Unimplemented opcode: 0xFE %02x (%s) at 0x%x\n", *td->ip, mono_opcode_name (256 + *td->ip), td->ip-header->code);
			}
			break;
		default:
			g_error ("transform.c: Unimplemented opcode: %02x at 0x%x\n", *td->ip, td->ip-header->code);
		}

		// No IR instructions were added as part of a bb_start IL instruction. Add a MINT_NOP
		// so we always have an instruction associated with a bb_start. This is simple and avoids
		// any complications associated with il_offset tracking.
		if (prev_last_ins == td->last_ins && (!inlining && td->is_bb_start [in_offset]) && td->ip < end)
			interp_add_ins (td, MINT_NOP);
	}

	g_assert (td->ip == end);

exit_ret:
	g_free (arg_locals);
	g_free (local_locals);
	mono_basic_block_free (original_bb);
	td->dont_inline = g_list_remove (td->dont_inline, method);

	return ret;
exit:
	ret = FALSE;
	goto exit_ret;
}

// Find the offset of the first interp instruction generated starting il_offset
// This is needed to find the end of clauses.
static int
find_in_offset (TransformData *td, int il_offset)
{
	int i = il_offset;
	while (!td->in_offsets [i])
		i++;
	return td->in_offsets [i] - 1;
}

// We store in the in_offset array the native_offset + 1, so 0 can mean only that the il
// offset is uninitialized. Otherwise 0 is valid value for first interp instruction.
static int
get_in_offset (TransformData *td, int il_offset)
{
	int target_offset = td->in_offsets [il_offset];
	g_assert (target_offset);
	return target_offset - 1;
}

static void
handle_relocations (TransformData *td)
{
	// Handle relocations
	for (int i = 0; i < td->relocs->len; ++i) {
		Reloc *reloc = (Reloc*)g_ptr_array_index (td->relocs, i);
		int offset = get_in_offset (td, reloc->target) - reloc->offset;

		switch (reloc->type) {
		case RELOC_SHORT_BRANCH:
			g_assert (td->new_code [reloc->offset + 1] == 0xdead);
			td->new_code [reloc->offset + 1] = offset;
			break;
		case RELOC_LONG_BRANCH: {
			guint16 *v = (guint16 *) &offset;
			g_assert (td->new_code [reloc->offset + 1] == 0xdead);
			g_assert (td->new_code [reloc->offset + 2] == 0xbeef);
			td->new_code [reloc->offset + 1] = *(guint16 *) v;
			td->new_code [reloc->offset + 2] = *(guint16 *) (v + 1);
			break;
		}
		case RELOC_SWITCH: {
			guint16 *v = (guint16*)&offset;
			g_assert (td->new_code [reloc->offset] == 0xdead);
			g_assert (td->new_code [reloc->offset + 1] == 0xbeef);
			td->new_code [reloc->offset] = *(guint16*)v;
			td->new_code [reloc->offset + 1] = *(guint16*)(v + 1);
			break;
		}
		default:
			g_assert_not_reached ();
			break;
		}
	}
}


static int
get_inst_length (InterpInst *ins)
{
	if (ins->opcode == MINT_SWITCH)
		return MINT_SWITCH_LEN (READ32 (&ins->data [0]));
#ifdef ENABLE_EXPERIMENT_TIERED
	else if (MINT_IS_PATCHABLE_CALL (ins->opcode))
		return MAX (mono_interp_oplen [MINT_JIT_CALL2], mono_interp_oplen [ins->opcode]);
#endif
	else
		return mono_interp_oplen [ins->opcode];
}

static void
get_inst_stack_usage (TransformData *td, InterpInst *ins, int *pop, int *push)
{
	guint16 opcode = ins->opcode;
	if (mono_interp_oppop [opcode] == MINT_VAR_POP ||
			mono_interp_oppush [opcode] == MINT_VAR_PUSH) {
		switch (opcode) {
		case MINT_JIT_CALL:
		case MINT_CALL:
		case MINT_CALLVIRT:
		case MINT_CALLVIRT_FAST: {
			InterpMethod *imethod = (InterpMethod*) td->data_items [ins->data [0]];
			*pop = imethod->param_count + imethod->hasthis;
			*push = imethod->rtype->type != MONO_TYPE_VOID;
			break;
		}
#ifndef ENABLE_NETCORE
		case MINT_CALLRUN: {
			MonoMethodSignature *csignature = (MonoMethodSignature*) td->data_items [ins->data [1]];
			*pop = csignature->param_count + csignature->hasthis;
			*push = csignature->ret->type != MONO_TYPE_VOID;
			break;
		}
#endif
		case MINT_CALL_DELEGATE: {
			MonoMethodSignature *csignature = (MonoMethodSignature*) td->data_items [ins->data [0]];
			*pop = csignature->param_count + 1;
			*push = csignature->ret->type != MONO_TYPE_VOID;
			break;
		}
		case MINT_CALLI:
		case MINT_CALLI_NAT:
		case MINT_CALLI_NAT_DYNAMIC:
		case MINT_CALLI_NAT_FAST: {
			MonoMethodSignature *csignature = (MonoMethodSignature*) td->data_items [ins->data [0]];
			*pop = csignature->param_count + csignature->hasthis + 1;
			*push = csignature->ret->type != MONO_TYPE_VOID;
			break;
		}
		case MINT_CALL_VARARG: {
			InterpMethod *imethod = (InterpMethod*) td->data_items [ins->data [0]];
			MonoMethodSignature *csignature = (MonoMethodSignature*) td->data_items [ins->data [1]];
			*pop = imethod->param_count + imethod->hasthis + csignature->param_count - csignature->sentinelpos;
			*push = imethod->rtype->type != MONO_TYPE_VOID;
			break;
		}
		case MINT_NEWOBJ_VT_FAST:
		case MINT_NEWOBJ_VTST_FAST:
		case MINT_NEWOBJ_FAST: {
			int param_count = ins->data [1];
			gboolean is_inlined = ins->data [0] == INLINED_METHOD_FLAG;
			if (is_inlined) {
				// This needs to be handled explictly during cprop, in order to properly
				// keep track of stack contents
				*pop = 0;
				*push = 2;
			} else {
				*pop = param_count;
				*push = 1;
			}
			break;
		}
		case MINT_NEWOBJ_ARRAY:
		case MINT_NEWOBJ_STRING:
			*pop = ins->data [1];
			*push = 1;
			break;
		case MINT_LDELEMA:
		case MINT_LDELEMA_TC:
			*pop = ins->data [0] + 1;
			*push = 1;
			break;
		case MINT_NEWOBJ: {
			InterpMethod *imethod = (InterpMethod*) td->data_items [ins->data [0]];
			*pop = imethod->param_count;
			*push = 1;
			break;
		}
		default:
			g_assert_not_reached ();
		}
	} else {
		*pop = mono_interp_oppop [opcode];
		*push = mono_interp_oppush [opcode];
	}
}

static guint16*
emit_compacted_instruction (TransformData *td, guint16* start_ip, InterpInst *ins)
{
	guint16 opcode = ins->opcode;
	guint16 *ip = start_ip;

	// We know what IL offset this instruction was created for. We can now map the IL offset
	// to the IR offset. We use this array to resolve the relocations, which reference the IL.
	if (ins->il_offset != -1 && !td->in_offsets [ins->il_offset]) {
		g_assert (ins->il_offset >= 0 && ins->il_offset < td->header->code_size);
		td->in_offsets [ins->il_offset] = start_ip - td->new_code + 1;

		MonoDebugLineNumberEntry lne;
		lne.native_offset = (guint8*)start_ip - (guint8*)td->new_code;
		lne.il_offset = ins->il_offset;
		g_array_append_val (td->line_numbers, lne);
	}

	if (opcode == MINT_NOP)
		return ip;

	*ip++ = opcode;
	if (opcode == MINT_SWITCH) {
		int labels = READ32 (&ins->data [0]);
		// Write number of switch labels
		*ip++ = ins->data [0];
		*ip++ = ins->data [1];
		// Add relocation for each label
		for (int i = 0; i < labels; i++) {
			Reloc *reloc = (Reloc*)mono_mempool_alloc0 (td->mempool, sizeof (Reloc));
			reloc->type = RELOC_SWITCH;
			reloc->offset = ip - td->new_code;
			reloc->target = READ32 (&ins->data [2 + i * 2]);
			g_ptr_array_add (td->relocs, reloc);
			*ip++ = 0xdead;
			*ip++ = 0xbeef;
		}
	} else if ((opcode >= MINT_BRFALSE_I4_S && opcode <= MINT_BRTRUE_R8_S) ||
			(opcode >= MINT_BEQ_I4_S && opcode <= MINT_BLT_UN_R8_S) ||
			opcode == MINT_BR_S || opcode == MINT_LEAVE_S || opcode == MINT_LEAVE_S_CHECK) {
		const int br_offset = start_ip - td->new_code;
		if (ins->data [0] < ins->il_offset) {
			// Backwards branch. We can already patch it.
			*ip++ = get_in_offset (td, ins->data [0]) - br_offset;
		} else {
			// We don't know the in_offset of the target, add a reloc
			Reloc *reloc = (Reloc*)mono_mempool_alloc0 (td->mempool, sizeof (Reloc));
			reloc->type = RELOC_SHORT_BRANCH;
			reloc->offset = br_offset;
			reloc->target = ins->data [0];
			g_ptr_array_add (td->relocs, reloc);
			*ip++ = 0xdead;
		}
	} else if ((opcode >= MINT_BRFALSE_I4 && opcode <= MINT_BRTRUE_R8) ||
			(opcode >= MINT_BEQ_I4 && opcode <= MINT_BLT_UN_R8) ||
			opcode == MINT_BR || opcode == MINT_LEAVE || opcode == MINT_LEAVE_CHECK) {
		const int br_offset = start_ip - td->new_code;
		int target_il = READ32 (&ins->data [0]);
		if (target_il < ins->il_offset) {
			// Backwards branch. We can already patch it
			const int br_offset = start_ip - td->new_code;
			int target_offset = get_in_offset (td, target_il) - br_offset;
			WRITE32 (ip, &target_offset);
		} else {
			Reloc *reloc = (Reloc*)mono_mempool_alloc0 (td->mempool, sizeof (Reloc));
			reloc->type = RELOC_LONG_BRANCH;
			reloc->offset = br_offset;
			reloc->target = target_il;
			g_ptr_array_add (td->relocs, reloc);
			*ip++ = 0xdead;
			*ip++ = 0xbeef;
		}
	} else if (opcode == MINT_SDB_SEQ_POINT) {
		SeqPoint *seqp = (SeqPoint*)mono_mempool_alloc0 (td->mempool, sizeof (SeqPoint));
		InterpBasicBlock *cbb;

		if (ins->flags & INTERP_INST_FLAG_SEQ_POINT_METHOD_ENTRY) {
			seqp->il_offset = METHOD_ENTRY_IL_OFFSET;
			cbb = td->offset_to_bb [0];
		} else {
			if (ins->flags & INTERP_INST_FLAG_SEQ_POINT_METHOD_EXIT)
				seqp->il_offset = METHOD_EXIT_IL_OFFSET;
			else
				seqp->il_offset = ins->il_offset;
			cbb = td->offset_to_bb [ins->il_offset];
		}
		seqp->native_offset = (guint8*)start_ip - (guint8*)td->new_code;
		if (ins->flags & INTERP_INST_FLAG_SEQ_POINT_NONEMPTY_STACK)
			seqp->flags |= MONO_SEQ_POINT_FLAG_NONEMPTY_STACK;
		if (ins->flags & INTERP_INST_FLAG_SEQ_POINT_NESTED_CALL)
			seqp->flags |= MONO_SEQ_POINT_FLAG_NESTED_CALL;
		g_ptr_array_add (td->seq_points, seqp);

		cbb->seq_points = g_slist_prepend_mempool (td->mempool, cbb->seq_points, seqp);
		cbb->last_seq_point = seqp;
#ifdef ENABLE_EXPERIMENT_TIERED
	} else if (ins->flags & INTERP_INST_FLAG_RECORD_CALL_PATCH) {
		g_assert (MINT_IS_PATCHABLE_CALL (opcode));

		/* TODO: could `ins` be removed by any interp optimization? */
		MonoMethod *target_method = (MonoMethod *) g_hash_table_lookup (td->patchsite_hash, ins);
		g_assert (target_method);
		g_hash_table_remove (td->patchsite_hash, ins);

		mini_tiered_record_callsite (start_ip, target_method, TIERED_PATCH_KIND_INTERP);

		int size = mono_interp_oplen [ins->opcode];
		int jit_call2_size = mono_interp_oplen [MINT_JIT_CALL2];

		g_assert (size < jit_call2_size);

		// Emit the rest of the data
		for (int i = 0; i < size - 1; i++)
			*ip++ = ins->data [i];

		/* intentional padding so we can patch a MINT_JIT_CALL2 here */
		for (int i = size - 1; i < (jit_call2_size - 1); i++)
			*ip++ = MINT_NIY;
#endif
	} else {
		if (MINT_IS_LDLOC (opcode) || MINT_IS_STLOC (opcode) || MINT_IS_STLOC_NP (opcode) || opcode == MINT_LDLOCA_S ||
				MINT_IS_LDLOCFLD (opcode) || MINT_IS_LOCUNOP (opcode) || MINT_IS_STLOCFLD (opcode)) {
			ins->data [0] = get_interp_local_offset (td, ins->data [0]);
		} else if (MINT_IS_MOVLOC (opcode)) {
			ins->data [0] = get_interp_local_offset (td, ins->data [0]);
			ins->data [1] = get_interp_local_offset (td, ins->data [1]);
		}

		int size = get_inst_length (ins) - 1;
		// Emit the rest of the data
		for (int i = 0; i < size; i++)
			*ip++ = ins->data [i];
	}
	mono_interp_stats.emitted_instructions++;
	return ip;
}

// Generates the final code, after we are done with all the passes
static void
generate_compacted_code (TransformData *td)
{
	guint16 *ip;
	int size = 0;
	td->relocs = g_ptr_array_new ();

	// Iterate once to compute the exact size of the compacted code
	InterpInst *ins = td->first_ins;
	while (ins) {
		size += get_inst_length (ins);
		ins = ins->next;
	}

	// Generate the compacted stream of instructions
	td->new_code = ip = (guint16*)mono_domain_alloc0 (td->rtm->domain, size * sizeof (guint16));
	ins = td->first_ins;
	while (ins) {
		ip = emit_compacted_instruction (td, ip, ins);
		ins = ins->next;
	}
	td->new_code_end = ip;
	td->in_offsets [td->header->code_size] = td->new_code_end - td->new_code;

	// Patch all branches
	handle_relocations (td);

	g_ptr_array_free (td->relocs, TRUE);
}

static int
get_movloc_for_type (int mt)
{
	switch (mt) {
	case MINT_TYPE_I1:
	case MINT_TYPE_U1:
		return MINT_MOVLOC_1;
	case MINT_TYPE_I2:
	case MINT_TYPE_U2:
		return MINT_MOVLOC_2;
	case MINT_TYPE_I4:
	case MINT_TYPE_R4:
		return MINT_MOVLOC_4;
	case MINT_TYPE_I8:
	case MINT_TYPE_R8:
		return MINT_MOVLOC_8;
	case MINT_TYPE_O:
#if SIZEOF_VOID_P == 8
		return MINT_MOVLOC_8;
#else
		return MINT_MOVLOC_4;
#endif
	case MINT_TYPE_VT:
		return MINT_MOVLOC_VT;
	}
	g_assert_not_reached ();
}

// The value of local has changed. This means the contents of the stack where the
// local was loaded, no longer contain the value of the local. Clear them.
static void
clear_stack_content_info_for_local (StackContentInfo *start, StackContentInfo *end, int local)
{
	StackContentInfo *si;
	for (si = start; si < end; si++) {
		if (si->val.type == STACK_VALUE_LOCAL && si->val.local == local)
			si->val.type = STACK_VALUE_NONE;
	}
}

// The value of local has changed. This means we can no longer assume that any other local
// is a copy of this local.
static void
clear_local_content_info_for_local (StackValue *start, StackValue *end, int local)
{
	StackValue *sval;
	for (sval = start; sval < end; sval++) {
		if (sval->type == STACK_VALUE_LOCAL && sval->local == local)
			sval->type = STACK_VALUE_NONE;
	}
}

static gboolean
interp_local_deadce (TransformData *td, int *local_ref_count)
{
	InterpInst *ins;
	gboolean needs_dce = FALSE;
	gboolean needs_cprop = FALSE;

	for (int i = 0; i < td->locals_size; i++) {
		g_assert (local_ref_count [i] >= 0);
		g_assert (td->locals [i].indirects >= 0);
		if (!local_ref_count [i] &&
				!td->locals [i].indirects &&
				(td->locals [i].flags & INTERP_LOCAL_FLAG_DEAD) == 0) {
			needs_dce = TRUE;
			// If we do another deadce iteration over the code, make sure we don't try
			// to kill instructions accessing locals that have already been handled in
			// a previous iteration.
			td->locals [i].flags |= INTERP_LOCAL_FLAG_DEAD;
			break;
		}
	}

	// Return early if all locals are alive
	if (!needs_dce)
		return FALSE;

	// Kill instructions that don't use stack and are storing into dead locals
	for (ins = td->first_ins; ins != NULL; ins = ins->next) {
		if (MINT_IS_STLOC_NP (ins->opcode)) {
			if (!local_ref_count [ins->data [0]] && !td->locals [ins->data [0]].indirects) {
				interp_clear_ins (td, ins);
				mono_interp_stats.killed_instructions++;
				// We killed an instruction that makes use of the stack. This might uncover new optimizations
				needs_cprop = TRUE;
			}
		} else if (MINT_IS_MOVLOC (ins->opcode)) {
			if (!local_ref_count [ins->data [1]] && !td->locals [ins->data [1]].indirects) {
				interp_clear_ins (td, ins);
				mono_interp_stats.killed_instructions++;
			}
		} else if (MINT_IS_STLOC (ins->opcode) && ins->opcode != MINT_STLOC_VT) {
			if (!local_ref_count [ins->data [0]] && !td->locals [ins->data [0]].indirects) {
				// We store to a dead stloc, we can replace it with a POP to save local space
				ins->opcode = MINT_POP;
				mono_interp_stats.added_pop_count++;
				// We might to be able to kill both the pop and the instruction pushing the value
				needs_cprop = TRUE;
			}
		}
	}
	return needs_cprop;
}

#define INTERP_FOLD_UNOP(opcode,stack_type,field,op) \
	case opcode: \
		g_assert (sp->val.type == stack_type); \
		result.type = stack_type; \
		result.field = op sp->val.field; \
		break;

#define INTERP_FOLD_CONV(opcode,stack_type_dst,field_dst,stack_type_src,field_src,cast_type) \
	case opcode: \
		g_assert (sp->val.type == stack_type_src); \
		result.type = stack_type_dst; \
		result.field_dst = (cast_type)sp->val.field_src; \
		break;

#define INTERP_FOLD_CONV_FULL(opcode,stack_type_dst,field_dst,stack_type_src,field_src,cast_type,cond) \
	case opcode: \
		g_assert (sp->val.type == stack_type_src); \
		if (!(cond)) goto cfold_failed; \
		result.type = stack_type_dst; \
		result.field_dst = (cast_type)sp->val.field_src; \
		break;

static InterpInst*
interp_fold_unop (TransformData *td, StackContentInfo *sp, InterpInst *ins)
{
	StackValue result;

	if (ins->opcode >= MINT_CONV_I4_I8_SP &&
			ins->opcode <= MINT_CONV_R8_R4_SP) {
		// Decrement sp so it's easier to access top of the stack
		sp -= 2;
		if (sp->val.type != STACK_VALUE_I4 && sp->val.type != STACK_VALUE_I8)
			goto cfold_failed;

		switch (ins->opcode) {
			INTERP_FOLD_CONV (MINT_CONV_I4_I8_SP, STACK_VALUE_I4, i, STACK_VALUE_I8, l, gint32);
			INTERP_FOLD_CONV (MINT_CONV_I8_I4_SP, STACK_VALUE_I8, l, STACK_VALUE_I4, i, gint64);
			default:
				goto cfold_failed;
		}
	} else {
		// Decrement sp so it's easier to access top of the stack
		sp--;
		if (sp->val.type != STACK_VALUE_I4 && sp->val.type != STACK_VALUE_I8)
			goto cfold_failed;

		// Top of the stack is a constant
		switch (ins->opcode) {
			INTERP_FOLD_UNOP (MINT_ADD1_I4, STACK_VALUE_I4, i, 1+);
			INTERP_FOLD_UNOP (MINT_ADD1_I8, STACK_VALUE_I8, l, 1+);
			INTERP_FOLD_UNOP (MINT_SUB1_I4, STACK_VALUE_I4, i, -1+);
			INTERP_FOLD_UNOP (MINT_SUB1_I8, STACK_VALUE_I8, l, -1+);
			INTERP_FOLD_UNOP (MINT_NEG_I4, STACK_VALUE_I4, i, -);
			INTERP_FOLD_UNOP (MINT_NEG_I8, STACK_VALUE_I8, l, -);
			INTERP_FOLD_UNOP (MINT_NOT_I4, STACK_VALUE_I4, i, ~);
			INTERP_FOLD_UNOP (MINT_NOT_I8, STACK_VALUE_I8, l, ~);
			INTERP_FOLD_UNOP (MINT_CEQ0_I4, STACK_VALUE_I4, i, 0 ==);

			INTERP_FOLD_CONV (MINT_CONV_I1_I4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, gint8);
			INTERP_FOLD_CONV (MINT_CONV_I1_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, gint8);
			INTERP_FOLD_CONV (MINT_CONV_U1_I4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, guint8);
			INTERP_FOLD_CONV (MINT_CONV_U1_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, guint8);

			INTERP_FOLD_CONV (MINT_CONV_I2_I4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, gint16);
			INTERP_FOLD_CONV (MINT_CONV_I2_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, gint16);
			INTERP_FOLD_CONV (MINT_CONV_U2_I4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, guint16);
			INTERP_FOLD_CONV (MINT_CONV_U2_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, guint16);

			INTERP_FOLD_CONV (MINT_CONV_I4_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, gint32);
			INTERP_FOLD_CONV (MINT_CONV_U4_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, gint32);

			INTERP_FOLD_CONV (MINT_CONV_I8_I4, STACK_VALUE_I8, l, STACK_VALUE_I4, i, gint32);
			INTERP_FOLD_CONV (MINT_CONV_I8_U4, STACK_VALUE_I8, l, STACK_VALUE_I4, i, guint32);

			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I1_I4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, gint8, sp [0].val.i >= G_MININT8 && sp [0].val.i <= G_MAXINT8);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I1_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, gint8, sp [0].val.l >= G_MININT8 && sp [0].val.l <= G_MAXINT8);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I1_U4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, gint8, sp [0].val.i >= 0 && sp [0].val.i <= G_MAXINT8);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I1_U8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, gint8, sp [0].val.l >= 0 && sp [0].val.l <= G_MAXINT8);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U1_I4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, guint8, sp [0].val.i >= 0 && sp [0].val.i <= G_MAXUINT8);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U1_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, guint8, sp [0].val.l >= 0 && sp [0].val.l <= G_MAXUINT8);

			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I2_I4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, gint16, sp [0].val.i >= G_MININT16 && sp [0].val.i <= G_MAXINT16);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I2_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, i, gint16, sp [0].val.l >= G_MININT16 && sp [0].val.l <= G_MAXINT16);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I2_U4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, gint16, sp [0].val.i >= 0 && sp [0].val.i <= G_MAXINT16);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I2_U8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, gint16, sp [0].val.l >= 0 && sp [0].val.l <= G_MAXINT16);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U2_I4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, guint16, sp [0].val.i >= 0 && sp [0].val.i <= G_MAXUINT16);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U2_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, guint16, sp [0].val.l >= 0 && sp [0].val.l <= G_MAXUINT16);

			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I4_U4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, gint32, sp [0].val.i >= 0);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I4_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, gint32, sp [0].val.l >= G_MININT32 && sp [0].val.l <= G_MAXINT32);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I4_U8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, gint32, sp [0].val.l >= 0 && sp [0].val.l <= G_MAXINT32);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U4_I4, STACK_VALUE_I4, i, STACK_VALUE_I4, i, guint32, sp [0].val.i >= 0);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U4_I8, STACK_VALUE_I4, i, STACK_VALUE_I8, l, guint32, sp [0].val.l >= 0 && sp [0].val.l <= G_MAXINT32);

			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I8_U8, STACK_VALUE_I8, l, STACK_VALUE_I8, l, gint64, sp [0].val.l >= 0);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U8_I4, STACK_VALUE_I8, l, STACK_VALUE_I4, i, guint64, sp [0].val.i >= 0);
			INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U8_I8, STACK_VALUE_I8, l, STACK_VALUE_I8, l, guint64, sp [0].val.l >= 0);

			default:
				goto cfold_failed;
		}
	}

	// We were able to compute the result of the ins instruction. We store the
	// current value for the top of the stack and, if possible, try to replace the
	// instructions that are part of this unary operation with a single LDC.
	mono_interp_stats.constant_folds++;
	if (sp->ins != NULL) {
		// The instruction that pushed the top of stack can be replaced with the new constant result
		if (result.type == STACK_VALUE_I4)
			sp->ins = interp_get_ldc_i4_from_const (td, sp->ins, result.i);
		else if (result.type == STACK_VALUE_I8)
			sp->ins = interp_inst_replace_with_i8_const (td, sp->ins, result.l);
		else
			g_assert_not_reached ();
		if (td->verbose_level) {
			g_print ("Fold unop :\n\t");
			dump_interp_inst_newline (sp->ins);
		}
		mono_interp_stats.killed_instructions++;
		interp_clear_ins (td, ins);
	}
	sp->val = result;
	return ins;

cfold_failed:
	sp->ins = NULL;
	sp->val.type = STACK_VALUE_NONE;
	return ins;
}

#define INTERP_FOLD_BINOP(opcode,stack_type,field,op) \
	case opcode: \
		g_assert (sp [0].val.type == stack_type && sp [1].val.type == stack_type); \
		result.type = stack_type; \
		result.field = sp [0].val.field op sp [1].val.field; \
		break;

#define INTERP_FOLD_BINOP_FULL(opcode,stack_type,field,op,cast_type,cond) \
	case opcode: \
		g_assert (sp [0].val.type == stack_type && sp [1].val.type == stack_type); \
		if (!(cond)) goto cfold_failed; \
		result.type = stack_type; \
		result.field = (cast_type)sp [0].val.field op (cast_type)sp [1].val.field; \
		break;

#define INTERP_FOLD_SHIFTOP(opcode,stack_type,field,shift_op,cast_type) \
	case opcode: \
		g_assert (sp [1].val.type == STACK_VALUE_I4); \
		result.type = stack_type; \
		result.field = (cast_type)sp [0].val.field shift_op sp [1].val.i; \
		break;

#define INTERP_FOLD_RELOP(opcode,stack_type,field,relop,cast_type) \
	case opcode: \
		g_assert (sp [0].val.type == stack_type && sp [1].val.type == stack_type); \
		result.type = STACK_VALUE_I4; \
		result.i = (cast_type) sp [0].val.field relop (cast_type) sp [1].val.field; \
		break;


static InterpInst*
interp_fold_binop (TransformData *td, StackContentInfo *sp, InterpInst *ins)
{
	StackValue result;
	// Decrement sp so it's easier to access top of the stack
	sp -= 2;
	if (sp [0].val.type != STACK_VALUE_I4 && sp [0].val.type != STACK_VALUE_I8)
		goto cfold_failed;
	if (sp [1].val.type != STACK_VALUE_I4 && sp [1].val.type != STACK_VALUE_I8)
		goto cfold_failed;

	// Top two values of the stack are constants
	switch (ins->opcode) {
		INTERP_FOLD_BINOP (MINT_ADD_I4, STACK_VALUE_I4, i, +);
		INTERP_FOLD_BINOP (MINT_ADD_I8, STACK_VALUE_I8, l, +);
		INTERP_FOLD_BINOP (MINT_SUB_I4, STACK_VALUE_I4, i, -);
		INTERP_FOLD_BINOP (MINT_SUB_I8, STACK_VALUE_I8, l, -);
		INTERP_FOLD_BINOP (MINT_MUL_I4, STACK_VALUE_I4, i, *);
		INTERP_FOLD_BINOP (MINT_MUL_I8, STACK_VALUE_I8, l, *);

		INTERP_FOLD_BINOP (MINT_AND_I4, STACK_VALUE_I4, i, &);
		INTERP_FOLD_BINOP (MINT_AND_I8, STACK_VALUE_I8, l, &);
		INTERP_FOLD_BINOP (MINT_OR_I4, STACK_VALUE_I4, i, |);
		INTERP_FOLD_BINOP (MINT_OR_I8, STACK_VALUE_I8, l, |);
		INTERP_FOLD_BINOP (MINT_XOR_I4, STACK_VALUE_I4, i, ^);
		INTERP_FOLD_BINOP (MINT_XOR_I8, STACK_VALUE_I8, l, ^);

		INTERP_FOLD_SHIFTOP (MINT_SHL_I4, STACK_VALUE_I4, i, <<, gint32);
		INTERP_FOLD_SHIFTOP (MINT_SHL_I8, STACK_VALUE_I8, l, <<, gint64);
		INTERP_FOLD_SHIFTOP (MINT_SHR_I4, STACK_VALUE_I4, i, >>, gint32);
		INTERP_FOLD_SHIFTOP (MINT_SHR_I8, STACK_VALUE_I8, l, >>, gint64);
		INTERP_FOLD_SHIFTOP (MINT_SHR_UN_I4, STACK_VALUE_I4, i, >>, guint32);
		INTERP_FOLD_SHIFTOP (MINT_SHR_UN_I8, STACK_VALUE_I8, l, >>, guint64);

		INTERP_FOLD_RELOP (MINT_CEQ_I4, STACK_VALUE_I4, i, ==, gint32);
		INTERP_FOLD_RELOP (MINT_CEQ_I8, STACK_VALUE_I8, l, ==, gint64);
		INTERP_FOLD_RELOP (MINT_CNE_I4, STACK_VALUE_I4, i, !=, gint32);
		INTERP_FOLD_RELOP (MINT_CNE_I8, STACK_VALUE_I8, l, !=, gint64);

		INTERP_FOLD_RELOP (MINT_CGT_I4, STACK_VALUE_I4, i, >, gint32);
		INTERP_FOLD_RELOP (MINT_CGT_I8, STACK_VALUE_I8, l, >, gint64);
		INTERP_FOLD_RELOP (MINT_CGT_UN_I4, STACK_VALUE_I4, i, >, guint32);
		INTERP_FOLD_RELOP (MINT_CGT_UN_I8, STACK_VALUE_I8, l, >, guint64);

		INTERP_FOLD_RELOP (MINT_CGE_I4, STACK_VALUE_I4, i, >=, gint32);
		INTERP_FOLD_RELOP (MINT_CGE_I8, STACK_VALUE_I8, l, >=, gint64);
		INTERP_FOLD_RELOP (MINT_CGE_UN_I4, STACK_VALUE_I4, i, >=, guint32);
		INTERP_FOLD_RELOP (MINT_CGE_UN_I8, STACK_VALUE_I8, l, >=, guint64);

		INTERP_FOLD_RELOP (MINT_CLT_I4, STACK_VALUE_I4, i, <, gint32);
		INTERP_FOLD_RELOP (MINT_CLT_I8, STACK_VALUE_I8, l, <, gint64);
		INTERP_FOLD_RELOP (MINT_CLT_UN_I4, STACK_VALUE_I4, i, <, guint32);
		INTERP_FOLD_RELOP (MINT_CLT_UN_I8, STACK_VALUE_I8, l, <, guint64);

		INTERP_FOLD_RELOP (MINT_CLE_I4, STACK_VALUE_I4, i, <=, gint32);
		INTERP_FOLD_RELOP (MINT_CLE_I8, STACK_VALUE_I8, l, <=, gint64);
		INTERP_FOLD_RELOP (MINT_CLE_UN_I4, STACK_VALUE_I4, i, <=, guint32);
		INTERP_FOLD_RELOP (MINT_CLE_UN_I8, STACK_VALUE_I8, l, <=, guint64);

		INTERP_FOLD_BINOP_FULL (MINT_DIV_I4, STACK_VALUE_I4, i, /, gint32, sp [1].val.i != 0 && (sp [0].val.i != G_MININT32 || sp [1].val.i != -1));
		INTERP_FOLD_BINOP_FULL (MINT_DIV_I8, STACK_VALUE_I8, l, /, gint64, sp [1].val.l != 0 && (sp [0].val.l != G_MININT64 || sp [1].val.l != -1));
		INTERP_FOLD_BINOP_FULL (MINT_DIV_UN_I4, STACK_VALUE_I4, i, /, guint32, sp [1].val.i != 0);
		INTERP_FOLD_BINOP_FULL (MINT_DIV_UN_I8, STACK_VALUE_I8, l, /, guint64, sp [1].val.l != 0);

		INTERP_FOLD_BINOP_FULL (MINT_REM_I4, STACK_VALUE_I4, i, %, gint32, sp [1].val.i != 0 && (sp [0].val.i != G_MININT32 || sp [1].val.i != -1));
		INTERP_FOLD_BINOP_FULL (MINT_REM_I8, STACK_VALUE_I8, l, %, gint64, sp [1].val.l != 0 && (sp [0].val.l != G_MININT64 || sp [1].val.l != -1));
		INTERP_FOLD_BINOP_FULL (MINT_REM_UN_I4, STACK_VALUE_I4, i, %, guint32, sp [1].val.i != 0);
		INTERP_FOLD_BINOP_FULL (MINT_REM_UN_I8, STACK_VALUE_I8, l, %, guint64, sp [1].val.l != 0);

		default:
			goto cfold_failed;
	}

	// We were able to compute the result of the ins instruction. We store the
	// current value for the top of the stack and, if possible, try to replace the
	// instructions that are part of this unary operation with a single LDC.
	mono_interp_stats.constant_folds++;
	if (sp [0].ins != NULL && sp [1].ins != NULL) {
		interp_clear_ins (td, sp [0].ins);
		interp_clear_ins (td, sp [1].ins);
		mono_interp_stats.killed_instructions += 2;
		if (result.type == STACK_VALUE_I4)
			ins = interp_get_ldc_i4_from_const (td, ins, result.i);
		else if (result.type == STACK_VALUE_I8)
			ins = interp_inst_replace_with_i8_const (td, ins, result.l);
		else
			g_assert_not_reached ();
		if (td->verbose_level) {
			g_print ("Fold binop :\n\t");
			dump_interp_inst_newline (ins);
		}
		sp [0].ins = ins;
	} else {
		sp [0].ins = NULL;
	}
	sp [0].val = result;
	return ins;

cfold_failed:
	sp->ins = NULL;
	sp->val.type = STACK_VALUE_NONE;
	return ins;
}

static gboolean
interp_local_equal (StackValue *locals, int local1, int local2)
{
	if (local1 == local2)
		return TRUE;
	if (locals [local1].type == STACK_VALUE_LOCAL && locals [local1].local == local2) {
		// local1 is a copy of local2
		return TRUE;
	}
	if (locals [local2].type == STACK_VALUE_LOCAL && locals [local2].local == local1) {
		// local2 is a copy of local1
		return TRUE;
	}
	if (locals [local1].type == STACK_VALUE_I4 && locals [local2].type == STACK_VALUE_I4)
		return locals [local1].i == locals [local2].i;
	if (locals [local1].type == STACK_VALUE_I8 && locals [local2].type == STACK_VALUE_I8)
		return locals [local1].l == locals [local2].l;
	return FALSE;
}

static void
interp_cprop (TransformData *td)
{
	if (!td->max_stack_height)
		return;
	StackContentInfo *stack = (StackContentInfo*) g_malloc (td->max_stack_height * sizeof (StackContentInfo));
	StackContentInfo *stack_end = stack + td->max_stack_height;
	StackContentInfo *sp;
	StackValue *locals = (StackValue*) g_malloc (td->locals_size * sizeof (StackValue));
	int *local_ref_count = (int*) g_malloc (td->locals_size * sizeof (int));
	InterpInst *ins;
	int last_il_offset;

retry:
	sp = stack;
	last_il_offset = -1;
	memset (local_ref_count, 0, td->locals_size * sizeof (int));

	for (ins = td->first_ins; ins != NULL; ins = ins->next) {
		int pop, push;
		int il_offset = ins->il_offset;
		// Optimizations take place only inside a single basic block
		// If two instructions have the same il_offset, then the second one
		// cannot be part the start of a basic block.
		gboolean is_bb_start = il_offset != -1 && td->is_bb_start [il_offset] && il_offset != last_il_offset;
		if (is_bb_start) {
			if (td->stack_height [il_offset] >= 0) {
				sp = stack + td->stack_height [il_offset];
				g_assert (sp <= stack_end);
				memset (stack, 0, (sp - stack) * sizeof (StackContentInfo));
			}
			memset (locals, 0, td->locals_size * sizeof (StackValue));
		}
		// The instruction pops some values then pushes some other
		get_inst_stack_usage (td, ins, &pop, &push);
		if (td->verbose_level && ins->opcode != MINT_NOP) {
			dump_interp_inst (ins);
			g_print (", sp %d, (pop %d, push %d)\n", sp - stack, pop, push);
		}
		if (MINT_IS_LDLOC (ins->opcode)) {
			int replace_op = 0;
			int loaded_local = ins->data [0];
			local_ref_count [loaded_local]++;
			InterpInst *prev_ins = interp_prev_ins (ins);
			if (prev_ins && MINT_IS_STLOC (prev_ins->opcode) && !interp_is_bb_start (td, prev_ins, ins) && interp_local_equal (locals, prev_ins->data [0], loaded_local)) {
				int mt = prev_ins->opcode - MINT_STLOC_I1;
				if (ins->opcode - MINT_LDLOC_I1 == mt) {
					if (mt == MINT_TYPE_I4)
						replace_op = MINT_STLOC_NP_I4;
					else if (mt == MINT_TYPE_I8)
						replace_op = MINT_STLOC_NP_I8;
					else if (mt == MINT_TYPE_R4)
						replace_op = MINT_STLOC_NP_R4;
					else if (mt == MINT_TYPE_R8)
						replace_op = MINT_STLOC_NP_R8;
					else if (mt == MINT_TYPE_O)
						replace_op = MINT_STLOC_NP_O;
					if (replace_op) {
						int stored_local = prev_ins->data [0];
						sp->ins = NULL;
						if (sp->val.type == STACK_VALUE_NONE && !td->locals [stored_local].indirects) {
							// We know what local is on the stack now. Track it
							sp->val.type = STACK_VALUE_LOCAL;
							sp->val.local = stored_local;
						}

						// Clear the previous stloc instruction
						interp_clear_ins (td, prev_ins);
						ins->opcode = replace_op;
						ins->data [0] = stored_local;
						local_ref_count [loaded_local]--;
						if (td->verbose_level) {
							g_print ("Add stloc.np :\n\t");
							dump_interp_inst_newline (ins);
						}
						mono_interp_stats.stloc_nps++;
						mono_interp_stats.killed_instructions++;
					}
				}
			} else if (locals [loaded_local].type == STACK_VALUE_LOCAL) {
				g_assert (!td->locals [loaded_local].indirects);
				// do copy propagation of the original source
				mono_interp_stats.copy_propagations++;
				local_ref_count [loaded_local]--;
				// We can't propagate a local that has its address taken
				g_assert (!td->locals [locals [loaded_local].local].indirects);
				ins->data [0] = locals [loaded_local].local;
				local_ref_count [ins->data [0]]++;
				if (td->verbose_level) {
					g_print ("cprop loc %d -> loc %d :\n\t", loaded_local, locals [loaded_local].local);
					dump_interp_inst_newline (ins);
				}
			} else if (locals [loaded_local].type == STACK_VALUE_I4 || locals [loaded_local].type == STACK_VALUE_I8) {
				gboolean is_i4 = locals [loaded_local].type == STACK_VALUE_I4;
				g_assert (!td->locals [loaded_local].indirects);
				if (is_i4)
					ins = interp_get_ldc_i4_from_const (td, ins, locals [loaded_local].i);
				else
					ins = interp_inst_replace_with_i8_const (td, ins, locals [loaded_local].l);
				sp->ins = ins;
				sp->val = locals [loaded_local];
				local_ref_count [loaded_local]--;
				mono_interp_stats.copy_propagations++;
				if (td->verbose_level) {
					g_print ("cprop loc %d -> ct :\n\t", loaded_local);
					dump_interp_inst_newline (ins);
				}
				// FIXME this replace_op got ugly
				replace_op = ins->opcode;
			}
			if (!replace_op) {
				// Save the ldloc on the stack if it wasn't optimized away
				// For simplicity we don't track locals that have their address taken
				// since it is hard to detect instructions that change the local value.
				if (td->locals [loaded_local].indirects) {
					sp->val.type = STACK_VALUE_NONE;
				} else {
					sp->val.type = STACK_VALUE_LOCAL;
					sp->val.local = ins->data [0];
				}
				sp->ins = ins;
			}
			sp++;
		} else if (MINT_IS_STLOC (ins->opcode)) {
			int dest_local = ins->data [0];
			sp--;
			if (sp->val.type == STACK_VALUE_LOCAL) {
				int src_local = sp->val.local;
				if (td->locals [src_local].mt == td->locals [dest_local].mt) {
					// The locals have the same type. We can propagate the value
					int vtsize = (ins->opcode == MINT_STLOC_VT) ? ins->data [1] : 0;

					if (!td->locals [dest_local].indirects) {
						// Track what exactly is stored into local
						locals [dest_local].type = STACK_VALUE_LOCAL;
						locals [dest_local].local = src_local;
					}

					if (sp->ins) {
						// If the top of stack is not pushed by a ldloc, we are introducing a
						// new dependency on the src_local since we are adding a movloc from it.
						if (!MINT_IS_LDLOC (sp->ins->opcode))
							local_ref_count [src_local]++;
						interp_clear_ins (td, sp->ins);
						interp_clear_ins (td, ins);

						ins = interp_insert_ins (td, ins, get_movloc_for_type (td->locals [src_local].mt));
						ins->data [0] = src_local;
						ins->data [1] = dest_local;
						if (vtsize)
							ins->data [2] = vtsize;
						// Clear ldloc / stloc pair and replace it with movloc superinstruction
						if (td->verbose_level) {
							g_print ("Add movloc (ldloc off %d) :\n\t", sp->ins->il_offset);
							dump_interp_inst_newline (ins);
						}
						mono_interp_stats.movlocs++;
						mono_interp_stats.killed_instructions++;
					}
				} else {
					locals [dest_local].type = STACK_VALUE_NONE;
				}
			} else if (sp->val.type == STACK_VALUE_NONE) {
				locals [dest_local].type = STACK_VALUE_NONE;
			} else {
				g_assert (sp->val.type == STACK_VALUE_I4 || sp->val.type == STACK_VALUE_I8);
				if (!td->locals [dest_local].indirects)
					locals [dest_local] = sp->val;
			}
			clear_stack_content_info_for_local (stack, sp, dest_local);
			clear_local_content_info_for_local (locals, locals + td->locals_size, dest_local);
		} else if (MINT_IS_LDC_I4 (ins->opcode) || ins->opcode == MINT_LDC_I8) {
			StackValue val;
			gboolean is_i8 = ins->opcode == MINT_LDC_I8;
			InterpInst *prev_ins = interp_prev_ins (ins);

			if (is_i8) {
				val.type = STACK_VALUE_I8;
				val.l = READ64 (&ins->data [0]);
			} else {
				val.type = STACK_VALUE_I4;
				val.i = interp_get_const_from_ldc_i4 (ins);
			}

			if (prev_ins && prev_ins->opcode == MINT_POP &&
					((is_i8 && sp->val.type == STACK_VALUE_I8 && sp->val.l == val.l) ||
					(!is_i8 && sp->val.type == STACK_VALUE_I4 && sp->val.i == val.i)) &&
					!interp_is_bb_start (td, prev_ins, ins)) {
				// The previous instruction pops the stack of the value we are pushing
				// right now. We can kill both instructions
				if (td->verbose_level)
					g_print ("Kill redundant pop/ldc pair: pop (off %p), ldc (off %p)\n", prev_ins->il_offset, ins->il_offset);
				interp_clear_ins (td, prev_ins);
				interp_clear_ins (td, ins);
				mono_interp_stats.killed_instructions += 2;
			} else {
				sp->ins = ins;
				sp->val = val;
			}
			sp++;
		} else if (MINT_IS_MOVLOC (ins->opcode)) {
			int src_local = ins->data [0];
			int dest_local = ins->data [1];
			local_ref_count [src_local]++;
			if (!td->locals [dest_local].indirects) {
				if (locals [src_local].type != STACK_VALUE_NONE) {
					locals [dest_local] = locals [src_local];
				} else {
					locals [dest_local].type = STACK_VALUE_LOCAL;
					locals [dest_local].local = src_local;
				}
				clear_stack_content_info_for_local (stack, sp, dest_local);
				clear_local_content_info_for_local (locals, locals + td->locals_size, dest_local);
			}
		} else if (MINT_IS_STLOC_NP (ins->opcode)) {
			int dest_local = ins->data [0];
			// Prevent optimizing away the instruction that pushed the value on the stack
			sp [-1].ins = NULL;
			// The local contains the value of the top of stack
			if (!td->locals [dest_local].indirects) {
				locals [dest_local] = sp [-1].val;
				clear_stack_content_info_for_local (stack, sp, dest_local);
				clear_local_content_info_for_local (locals, locals + td->locals_size, dest_local);
			}
		} else if (ins->opcode == MINT_DUP || ins->opcode == MINT_DUP_VT) {
			sp [0].val = sp [-1].val;
			sp [0].ins = ins;
			// If top of stack is known, we could also replace dup with an explicit
			// propagated instruction, so we remove the top of stack dependency
			sp [-1].ins = NULL;
			sp++;
		} else if (ins->opcode >= MINT_BOX && ins->opcode <= MINT_BOX_NULLABLE) {
			int offset = ins->data [1];
			// Clear the stack slot that is boxed
			memset (&sp [-1 - offset], 0, sizeof (StackContentInfo));
			// Make sure that the instructions that pushed this stack slot can't be
			// optimized away. If we would optimize them away, we would also need to
			// update the offset in the box instruction, which we can't, for now.
			for (int i = 1; i <= offset; i++)
				sp [-i].ins = NULL;
		} else if (ins->opcode == MINT_CKNULL_N) {
			int offset = ins->data [0];
			for (int i = 1; i <= offset; i++)
				sp [-i].ins = NULL;
		} else if (ins->opcode == MINT_LD_DELEGATE_INVOKE_IMPL) {
			int offset = ins->data [0];
			for (int i = 1; i <= offset; i++)
				sp [-i].ins = NULL;
			memset (sp, 0, sizeof (StackContentInfo));
			sp++;
		} else if (ins->opcode == MINT_POP || ins->opcode == MINT_POP_VT) {
			sp--;
			if (sp->ins) {
				// The top of the stack is not used by any instructions. Kill both the
				// instruction that pushed it and the pop.
				interp_clear_ins (td, sp->ins);
				interp_clear_ins (td, ins);
				mono_interp_stats.killed_instructions += 2;
				// The value pop-ed by this instruction can still be accessed. If we also
				// kill the instruction pushing the value, then we need to empty the
				// value of the stack, so it is not considered for further optimizations.
				sp->val.type = STACK_VALUE_NONE;
			}
		} else if ((ins->opcode >= MINT_NEWOBJ_FAST && ins->opcode <= MINT_NEWOBJ_VTST_FAST) && ins->data [0] == INLINED_METHOD_FLAG) {
			int param_count = ins->data [1];
			// memmove the stack values while clearing ins, to prevent instruction removal
			for (int i = 1; i <= param_count; i++) {
				sp [-i + 2] = sp [-i];
				sp [-i + 2].ins = NULL;
			}
			// clear stack information for the slots where the allocated object resides
			memset (&sp [-param_count], 0, 2 * sizeof (StackContentInfo));
			sp += 2;
		} else if (ins->opcode == MINT_CASTCLASS || ins->opcode == MINT_CASTCLASS_COMMON || ins->opcode == MINT_CASTCLASS_INTERFACE) {
			// Keep the value on the stack, but prevent optimizing away
			sp [-1].ins = NULL;
		} else if (MINT_IS_CONDITIONAL_BRANCH (ins->opcode)) {
			sp -= pop;
			g_assert (push == 0);
			// We can't clear any instruction that pushes the stack, because the
			// branched code will expect a certain stack size.
			for (StackContentInfo *sp_iter = stack; sp_iter < sp; sp_iter++)
				sp_iter->ins = NULL;
		} else if (MINT_IS_UNOP (ins->opcode)) {
			ins = interp_fold_unop (td, sp, ins);
		} else if (MINT_IS_BINOP (ins->opcode)) {
			ins = interp_fold_binop (td, sp, ins);
			sp--;
		} else if (ins->opcode == MINT_LDLOCA_S && MINT_IS_LDFLD (ins->next->opcode) &&
				td->locals [ins->data [0]].mt == (ins->next->opcode - MINT_LDFLD_I1) &&
				ins->next->data [0] == 0) {
			int mt = ins->next->opcode - MINT_LDFLD_I1;
			int local = ins->data [0];
			// Replace LDLOCA + LDFLD with LDLOC, when the storing field represents
			// the entire local. This is the case with storing to the only field of
			// an IntPtr. We don't handle value type loads.
			ins->next->opcode = MINT_LDLOC_I1 + mt;
			ins->next->data [0] = local;
			td->locals [local].indirects--;
			interp_clear_ins (td, ins);
			mono_interp_stats.killed_instructions++;
			mono_interp_stats.ldlocas_removed++;
			if (td->verbose_level) {
				g_print ("Replace ldloca/ldfld pair :\n\t");
				dump_interp_inst_newline (ins->next);
			}
		} else if (ins->opcode >= MINT_STFLD_I1 && ins->opcode <= MINT_STFLD_O) {
			StackContentInfo *src = &sp [-2];
			if (src->ins) {
				if (src->ins->opcode == MINT_LDLOCA_S && td->locals [src->ins->data [0]].mt == (ins->opcode - MINT_STFLD_I1) &&
						ins->data [0] == 0) {
					int mt = ins->opcode - MINT_STFLD_I1;
					int local = src->ins->data [0];
					interp_clear_ins (td, src->ins);
					ins->opcode = MINT_STLOC_I1 + mt;
					ins->data [0] = local;
					td->locals [local].indirects--;
					mono_interp_stats.killed_instructions++;
					mono_interp_stats.ldlocas_removed++;
					// FIXME Update stack contents for stloc, we currently rely on cprop running again.
					clear_stack_content_info_for_local (stack, sp, local);
					clear_local_content_info_for_local (locals, locals + td->locals_size, local);

					if (td->verbose_level) {
						g_print ("Replace ldloca/stfld pair (off %p) :\n\t", src->ins->il_offset);
						dump_interp_inst_newline (ins);
					}
				} else if (src->val.type == STACK_VALUE_LOCAL && (mono_interp_opt & INTERP_OPT_SUPER_INSTRUCTIONS)) {
					int loc_index = src->val.local;
					int fld_offset = ins->data [0];
					int mt = ins->opcode - MINT_STFLD_I1;
					ins = interp_insert_ins (td, ins, MINT_STLOCFLD_I1 + mt);
					ins->data [0] = loc_index;
					ins->data [1] = fld_offset;
					local_ref_count [loc_index]++;
					interp_clear_ins (td, ins->prev);
					interp_clear_ins (td, src->ins);
					mono_interp_stats.super_instructions++;
					mono_interp_stats.killed_instructions++;
				}
			}
			sp -= 2;
		} else if (MINT_IS_STLOCFLD (ins->opcode)) {
			local_ref_count [ins->data [0]]++;
			sp--;
		} else {
			if (pop == MINT_POP_ALL)
				pop = sp - stack;
			sp += push - pop;
			g_assert (sp >= stack && sp <= stack_end);
			g_assert ((sp - push) >= stack && (sp - push) <= stack_end);
			memset (sp - push, 0, push * sizeof (StackContentInfo));
			// If this instruction only pushes a single value, make it a candidate for
			// removal, if its value is not used anywhere.
			if (push == 1 && pop == 0 && !MINT_IS_CALL (ins->opcode) && !MINT_IS_NEWOBJ (ins->opcode))
				sp [-1].ins = ins;
		}
		last_il_offset = ins->il_offset;
	}

	if (interp_local_deadce (td, local_ref_count))
		goto retry;

	g_free (stack);
	g_free (locals);
	g_free (local_ref_count);
}

void
mono_test_interp_cprop (TransformData *td)
{
	interp_cprop (td);
}

static void
interp_super_instructions (TransformData *td)
{
	InterpInst *ins;
	InterpInst *prev1_ins = NULL;
	InterpInst *prev2_ins = NULL;
	int last_il_offset = -1;
	for (ins = td->first_ins; ins != NULL; ins = ins->next) {
		int il_offset = ins->il_offset;
		// If two instructions have the same il_offset, then the second one
		// cannot be the start of a basic block.
		gboolean is_bb_start = il_offset != -1 && td->is_bb_start [il_offset] && il_offset != last_il_offset;
		last_il_offset = il_offset;
		if (ins->opcode == MINT_NOP)
			continue;
		if (is_bb_start) {
			// Prevent optimizations spanning multiple basic blocks
			prev2_ins = NULL;
			prev1_ins = NULL;
		}
		if (ins->opcode >= MINT_LDFLD_I1 && ins->opcode <= MINT_LDFLD_O && prev1_ins) {
			if (prev1_ins->opcode == MINT_LDLOC_O) {
				int loc_index = prev1_ins->data [0];
				int fld_offset = ins->data [0];
				int mt = ins->opcode - MINT_LDFLD_I1;
				ins = interp_insert_ins (td, ins, MINT_LDLOCFLD_I1 + mt);
				ins->data [0] = loc_index;
				ins->data [1] = fld_offset;
				interp_clear_ins (td, ins->prev);
				interp_clear_ins (td, prev1_ins);
				prev1_ins = NULL;
				mono_interp_stats.super_instructions++;
				mono_interp_stats.killed_instructions++;
			}
		} else if (MINT_IS_STLOC (ins->opcode) && prev1_ins && prev2_ins) {
			if (prev1_ins->opcode == MINT_ADD1_I4 || prev1_ins->opcode == MINT_ADD1_I8 ||
				prev1_ins->opcode == MINT_SUB1_I4 || prev1_ins->opcode == MINT_SUB1_I8) {
				if (MINT_IS_LDLOC (prev2_ins->opcode) && prev2_ins->data [0] == ins->data [0]) {
					if (prev1_ins->opcode == MINT_ADD1_I4)
						ins->opcode = MINT_LOCADD1_I4;
					else if (prev1_ins->opcode == MINT_ADD1_I8)
						ins->opcode = MINT_LOCADD1_I8;
					else if (prev1_ins->opcode == MINT_SUB1_I4)
						ins->opcode = MINT_LOCSUB1_I4;
					else
						ins->opcode = MINT_LOCSUB1_I8;
					// the local index is already set inside the replaced STLOC instruction
					interp_clear_ins (td, prev1_ins);
					interp_clear_ins (td, prev2_ins);
					prev1_ins = NULL;
					mono_interp_stats.super_instructions++;
					mono_interp_stats.killed_instructions += 2;
				}
			}
		}
		prev2_ins = prev1_ins;
		prev1_ins = ins;
	}
}

static void
interp_optimize_code (TransformData *td)
{
	if (mono_interp_opt & INTERP_OPT_CPROP)
		MONO_TIME_TRACK (mono_interp_stats.cprop_time, interp_cprop (td));

	if (mono_interp_opt & INTERP_OPT_SUPER_INSTRUCTIONS)
		MONO_TIME_TRACK (mono_interp_stats.super_instructions_time, interp_super_instructions (td));
}

/*
 * Very few methods have localloc. Handle it separately to not impact performance
 * of other methods. We replace the normal return opcodes with opcodes that also
 * reset the localloc stack.
 */
static void
interp_fix_localloc_ret (TransformData *td)
{
	g_assert (td->has_localloc);
	InterpInst *ins;
	for (ins = td->first_ins; ins != NULL; ins = ins->next) {
		if (ins->opcode >= MINT_RET && ins->opcode <= MINT_RET_VT)
			ins->opcode += MINT_RET_LOCALLOC - MINT_RET;
	}
}

static void
generate (MonoMethod *method, MonoMethodHeader *header, InterpMethod *rtm, MonoGenericContext *generic_context, MonoError *error)
{
	MonoDomain *domain = rtm->domain;
	int i;
	TransformData transform_data;
	TransformData *td;
	static gboolean verbose_method_inited;
	static char* verbose_method_name;

	if (!verbose_method_inited) {
		verbose_method_name = g_getenv ("MONO_VERBOSE_METHOD");
		verbose_method_inited = TRUE;
	}

	memset (&transform_data, 0, sizeof(transform_data));
	td = &transform_data;

	td->method = method;
	td->rtm = rtm;
	td->code_size = header->code_size;
	td->header = header;
	td->max_code_size = td->code_size;
	td->in_offsets = (int*)g_malloc0((header->code_size + 1) * sizeof(int));
	td->stack_height = (int*)g_malloc(header->code_size * sizeof(int));
	td->stack_state = (StackInfo**)g_malloc0(header->code_size * sizeof(StackInfo *));
	td->vt_stack_size = (int*)g_malloc(header->code_size * sizeof(int));
	td->clause_indexes = (int*)g_malloc (header->code_size * sizeof (int));
	td->is_bb_start = (guint8*)g_malloc0(header->code_size);
	td->mempool = mono_mempool_new ();
	td->n_data_items = 0;
	td->max_data_items = 0;
	td->data_items = NULL;
	td->data_hash = g_hash_table_new (NULL, NULL);
#ifdef ENABLE_EXPERIMENT_TIERED
	td->patchsite_hash = g_hash_table_new (NULL, NULL);
#endif
	td->gen_sdb_seq_points = mini_debug_options.gen_sdb_seq_points;
	td->seq_points = g_ptr_array_new ();
	td->verbose_level = mono_interp_traceopt;
	td->prof_coverage = mono_profiler_coverage_instrumentation_enabled (method);
	rtm->data_items = td->data_items;

	if (td->prof_coverage)
		td->coverage_info = mono_profiler_coverage_alloc (method, header->code_size);

	interp_method_compute_offsets (td, rtm, mono_method_signature_internal (method), header, error);
	goto_if_nok (error, exit);

	if (verbose_method_name) {
		const char *name = verbose_method_name;

		if ((strchr (name, '.') > name) || strchr (name, ':')) {
			MonoMethodDesc *desc;

			desc = mono_method_desc_new (name, TRUE);
			if (mono_method_desc_full_match (desc, method)) {
				td->verbose_level = 4;
			}
			mono_method_desc_free (desc);
		} else {
			if (strcmp (method->name, name) == 0)
				td->verbose_level = 4;
		}
	}

	td->stack = (StackInfo*)g_malloc0 ((header->max_stack + 1) * sizeof (td->stack [0]));
	td->stack_capacity = header->max_stack + 1;
	td->sp = td->stack;
	td->max_stack_height = 0;
	td->line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));
	td->current_il_offset = -1;

	generate_code (td, method, header, generic_context, error);
	goto_if_nok (error, exit);

	if (td->has_localloc)
		interp_fix_localloc_ret (td);

	interp_optimize_code (td);

	generate_compacted_code (td);

	if (td->verbose_level) {
		g_print ("Runtime method: %s %p\n", mono_method_full_name (method, TRUE), rtm);
		g_print ("Locals size %d, VT stack size: %d\n", td->total_locals_size, td->max_vt_sp);
		g_print ("Calculated stack size: %d, stated size: %d\n", td->max_stack_height, header->max_stack);
		dump_mint_code (td->new_code, td->new_code_end);
	}

	/* Check if we use excessive stack space */
	if (td->max_stack_height > header->max_stack * 3 && header->max_stack > 16)
		g_warning ("Excessive stack space usage for method %s, %d/%d", method->name, td->max_stack_height, header->max_stack);

	int code_len_u8, code_len_u16;
	code_len_u8 = (guint8 *) td->new_code_end - (guint8 *) td->new_code;
	code_len_u16 = td->new_code_end - td->new_code;

	rtm->clauses = (MonoExceptionClause*)mono_domain_alloc0 (domain, header->num_clauses * sizeof (MonoExceptionClause));
	memcpy (rtm->clauses, header->clauses, header->num_clauses * sizeof(MonoExceptionClause));
	rtm->code = (gushort*)td->new_code;
	rtm->init_locals = header->init_locals;
	rtm->num_clauses = header->num_clauses;
	for (i = 0; i < header->num_clauses; i++) {
		MonoExceptionClause *c = rtm->clauses + i;
		int end_off = c->try_offset + c->try_len;
		c->try_offset = get_in_offset (td, c->try_offset);
		c->try_len = find_in_offset (td, end_off) - c->try_offset;
		g_assert ((c->try_offset + c->try_len) < code_len_u16);
		end_off = c->handler_offset + c->handler_len;
		c->handler_offset = get_in_offset (td, c->handler_offset);
		c->handler_len = find_in_offset (td, end_off) - c->handler_offset;
		g_assert (c->handler_len >= 0 && (c->handler_offset + c->handler_len) <= code_len_u16);
		if (c->flags & MONO_EXCEPTION_CLAUSE_FILTER)
			c->data.filter_offset = get_in_offset (td, c->data.filter_offset);
	}
	rtm->stack_size = (sizeof (stackval)) * (td->max_stack_height + 2); /* + 1 for returns of called functions  + 1 for 0-ing in trace*/
	rtm->stack_size = ALIGN_TO (rtm->stack_size, MINT_VT_ALIGNMENT);
	rtm->vt_stack_size = td->max_vt_sp;
	rtm->total_locals_size = ALIGN_TO (td->total_locals_size, MINT_VT_ALIGNMENT);
	rtm->alloca_size = ALIGN_TO (rtm->total_locals_size + rtm->vt_stack_size + rtm->stack_size, 8);
	rtm->data_items = (gpointer*)mono_domain_alloc0 (domain, td->n_data_items * sizeof (td->data_items [0]));
	memcpy (rtm->data_items, td->data_items, td->n_data_items * sizeof (td->data_items [0]));

	/* Save debug info */
	interp_save_debug_info (rtm, header, td, td->line_numbers);

	/* Create a MonoJitInfo for the interpreted method by creating the interpreter IR as the native code. */
	int jinfo_len;
	jinfo_len = mono_jit_info_size ((MonoJitInfoFlags)0, header->num_clauses, 0);
	MonoJitInfo *jinfo;
	jinfo = (MonoJitInfo *)mono_domain_alloc0 (domain, jinfo_len);
	jinfo->is_interp = 1;
	rtm->jinfo = jinfo;
	mono_jit_info_init (jinfo, method, (guint8*)rtm->code, code_len_u8, (MonoJitInfoFlags)0, header->num_clauses, 0);
	for (i = 0; i < jinfo->num_clauses; ++i) {
		MonoJitExceptionInfo *ei = &jinfo->clauses [i];
		MonoExceptionClause *c = rtm->clauses + i;

		ei->flags = c->flags;
		ei->try_start = (guint8*)(rtm->code + c->try_offset);
		ei->try_end = (guint8*)(rtm->code + c->try_offset + c->try_len);
		ei->handler_start = (guint8*)(rtm->code + c->handler_offset);
		ei->exvar_offset = rtm->exvar_offsets [i];
		if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			ei->data.filter = (guint8*)(rtm->code + c->data.filter_offset);
		} else if (ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
			ei->data.handler_end = (guint8*)(rtm->code + c->handler_offset + c->handler_len);
		} else {
			ei->data.catch_class = c->data.catch_class;
		}
	}

	save_seq_points (td, jinfo);
#ifdef ENABLE_EXPERIMENT_TIERED
	/* debugging aid, it makes `mono_pmip` work. */
	mono_jit_info_table_add (domain, jinfo);
#endif

exit:
	g_free (td->in_offsets);
	g_free (td->stack_height);
	for (i = 0; i < header->code_size; ++i)
		g_free (td->stack_state [i]);
	g_free (td->stack_state);
	g_free (td->vt_stack_size);
	g_free (td->clause_indexes);
	g_free (td->data_items);
	g_free (td->stack);
	g_free (td->is_bb_start);
	g_free (td->locals);
	g_hash_table_destroy (td->data_hash);
#ifdef ENABLE_EXPERIMENT_TIERED
	g_hash_table_destroy (td->patchsite_hash);
#endif
	g_ptr_array_free (td->seq_points, TRUE);
	g_array_free (td->line_numbers, TRUE);
	mono_mempool_destroy (td->mempool);
}

gboolean
mono_test_interp_generate_code (TransformData *td, MonoMethod *method, MonoMethodHeader *header, MonoGenericContext *generic_context, MonoError *error)
{
	return generate_code (td, method, header, generic_context, error);
}

static mono_mutex_t calc_section;

#ifdef ENABLE_EXPERIMENT_TIERED
static gboolean
tiered_patcher (MiniTieredPatchPointContext *ctx, gpointer patchsite)
{
	ERROR_DECL (error);
	MonoMethod *m = ctx->target_method;

	if (!jit_call2_supported (m, mono_method_signature_internal (m)))
		return FALSE;

	/* TODO: Force compilation here. Currently the JIT will be invoked upon
	 *       first execution of `MINT_JIT_CALL2`. */
	InterpMethod *rmethod = mono_interp_get_imethod (ctx->domain, m, error);
	mono_error_assert_ok (error);

	guint16 *ip = ((guint16 *) patchsite);
	*ip++ = MINT_JIT_CALL2;
	/* FIXME: this only works on 64bit */
	WRITE64 (ip, &rmethod);
	mono_memory_barrier ();

	return TRUE;
}
#endif


void 
mono_interp_transform_init (void)
{
	mono_os_mutex_init_recursive(&calc_section);

#ifdef ENABLE_EXPERIMENT_TIERED
	mini_tiered_register_callsite_patcher (tiered_patcher, TIERED_PATCH_KIND_INTERP);
#endif
}

void
mono_interp_transform_method (InterpMethod *imethod, ThreadContext *context, MonoError *error)
{
	MonoMethod *method = imethod->method;
	MonoMethodHeader *header = NULL;
	MonoMethodSignature *signature = mono_method_signature_internal (method);
	MonoVTable *method_class_vt;
	MonoGenericContext *generic_context = NULL;
	MonoDomain *domain = imethod->domain;
	InterpMethod tmp_imethod;
	InterpMethod *real_imethod;

	error_init (error);

	if (mono_class_is_open_constructed_type (m_class_get_byval_arg (method->klass))) {
		mono_error_set_invalid_operation (error, "%s", "Could not execute the method because the containing type is not fully instantiated.");
		return;
	}

	// g_printerr ("TRANSFORM(0x%016lx): begin %s::%s\n", mono_thread_current (), method->klass->name, method->name);
	method_class_vt = mono_class_vtable_checked (domain, imethod->method->klass, error);
	return_if_nok (error);

	if (!method_class_vt->initialized) {
		mono_runtime_class_init_full (method_class_vt, error);
		return_if_nok (error);
	}

	MONO_PROFILER_RAISE (jit_begin, (method));

	if (mono_method_signature_internal (method)->is_inflated)
		generic_context = mono_method_get_context (method);
	else {
		MonoGenericContainer *generic_container = mono_method_get_generic_container (method);
		if (generic_container)
			generic_context = &generic_container->context;
	}

	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		MonoMethod *nm = NULL;
		if (imethod->transformed) {
			MONO_PROFILER_RAISE (jit_done, (method, imethod->jinfo));
			return;
		}

		/* assumes all internal calls with an array this are built in... */
		if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL && (! mono_method_signature_internal (method)->hasthis || m_class_get_rank (method->klass) == 0)) {
			nm = mono_marshal_get_native_wrapper (method, FALSE, FALSE);
			signature = mono_method_signature_internal (nm);
		} else {
			const char *name = method->name;
			if (m_class_get_parent (method->klass) == mono_defaults.multicastdelegate_class) {
				if (*name == '.' && (strcmp (name, ".ctor") == 0)) {
					MonoJitICallInfo *mi = &mono_get_jit_icall_info ()->ves_icall_mono_delegate_ctor_interp;
					nm = mono_marshal_get_icall_wrapper (mi, TRUE);
				} else if (*name == 'I' && (strcmp (name, "Invoke") == 0)) {
					/*
					 * Usually handled during transformation of the caller, but
					 * when the caller is handled by another execution engine
					 * (for example fullAOT) we need to handle it here. That's
					 * known to be wrong in cases where the reference to
					 * `MonoDelegate` would be needed (FIXME).
					 */
					nm = mono_marshal_get_delegate_invoke (method, NULL);
				} else if (*name == 'B' && (strcmp (name, "BeginInvoke") == 0)) {
					nm = mono_marshal_get_delegate_begin_invoke (method);
				} else if (*name == 'E' && (strcmp (name, "EndInvoke") == 0)) {
					nm = mono_marshal_get_delegate_end_invoke (method);
				}
			}
			if (nm == NULL)
				g_assert_not_reached ();
		}
		if (nm == NULL) {
			mono_os_mutex_lock (&calc_section);
			imethod->stack_size = sizeof (stackval); /* for tracing */
			imethod->alloca_size = imethod->stack_size;
			mono_memory_barrier ();
			imethod->transformed = TRUE;
			mono_interp_stats.methods_transformed++;
			mono_os_mutex_unlock (&calc_section);
			MONO_PROFILER_RAISE (jit_done, (method, NULL));
			return;
		}
		method = nm;
		header = interp_method_get_header (nm, error);
		return_if_nok (error);
	}

	if (!header) {
		header = mono_method_get_header_checked (method, error);
		return_if_nok (error);
	}

	g_assert ((signature->param_count + signature->hasthis) < 1000);
	// g_printerr ("TRANSFORM(0x%016lx): end %s::%s\n", mono_thread_current (), method->klass->name, method->name);

	/* Make modifications to a copy of imethod, copy them back inside the lock */
	real_imethod = imethod;
	memcpy (&tmp_imethod, imethod, sizeof (InterpMethod));
	imethod = &tmp_imethod;

	MONO_TIME_TRACK (mono_interp_stats.transform_time, generate (method, header, imethod, generic_context, error));

	mono_metadata_free_mh (header);

	return_if_nok (error);

	/* Copy changes back */
	imethod = real_imethod;
	mono_os_mutex_lock (&calc_section);
	if (!imethod->transformed) {
		// Ignore the first two fields which are unchanged. next_jit_code_hash shouldn't
		// be modified because it is racy with internal hash table insert.
		const int start_offset = 2 * sizeof (gpointer);
		memcpy ((char*)imethod + start_offset, (char*)&tmp_imethod + start_offset, sizeof (InterpMethod) - start_offset);
		mono_memory_barrier ();
		imethod->transformed = TRUE;
		mono_interp_stats.methods_transformed++;
		mono_atomic_fetch_add_i32 (&mono_jit_stats.methods_with_interp, 1);

	}
	mono_os_mutex_unlock (&calc_section);

	mono_domain_lock (domain);
	if (mono_stats_method_desc && mono_method_desc_full_match (mono_stats_method_desc, imethod->method)) {
		g_printf ("Printing runtime stats at method: %s\n", mono_method_get_full_name (imethod->method));
		mono_runtime_print_stats ();
	}
	if (!g_hash_table_lookup (domain_jit_info (domain)->seq_points, imethod->method))
		g_hash_table_insert (domain_jit_info (domain)->seq_points, imethod->method, imethod->jinfo->seq_points);
	mono_domain_unlock (domain);

	// FIXME: Add a different callback ?
	MONO_PROFILER_RAISE (jit_done, (method, imethod->jinfo));
}

