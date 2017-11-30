/**
 * \file
 * transform CIL into different opcodes for more
 * efficient interpretation
 *
 * Written by Bernie Solomon (bernard@ugsolutions.com)
 * Copyright (c) 2004.
 */

#include <string.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/seq-points-data.h>

#include <mono/mini/mini.h>
#include <mono/mini/mini-runtime.h>

#include "mintops.h"
#include "interp-internals.h"
#include "interp.h"

// TODO: export from marshal.c
MonoDelegate* mono_ftnptr_to_delegate (MonoClass *klass, gpointer ftn);

#define DEBUG 0

typedef struct
{
	MonoClass *klass;
	unsigned char type;
	unsigned char flags;
} StackInfo;

typedef struct {
	guint8 *ip;
	GSList *preds;
	GSList *seq_points;
	SeqPoint *last_seq_point;

	// This will hold a list of last sequence points of incoming basic blocks
	SeqPoint **pred_seq_points;
	guint num_pred_seq_points;
} InterpBasicBlock;

typedef enum {
	RELOC_SHORT_BRANCH,
	RELOC_LONG_BRANCH,
	RELOC_SWITCH
} RelocType;

typedef struct {
	RelocType type;
	/* In the interpreter IR */
	int offset;
	/* In the IL code */
	int target;
} Reloc;

typedef struct
{
	MonoMethod *method;
	MonoMethodHeader *header;
	InterpMethod *rtm;
	const unsigned char *il_code;
	const unsigned char *ip;
	const unsigned char *last_ip;
	const unsigned char *in_start;
	int code_size;
	int *in_offsets;
	StackInfo **stack_state;
	int *stack_height;
	int *vt_stack_size;
	unsigned char *is_bb_start;
	unsigned short *new_code;
	unsigned short *new_code_end;
	unsigned short *new_ip;
	unsigned short *last_new_ip;
	unsigned int max_code_size;
	StackInfo *stack;
	StackInfo *sp;
	unsigned int max_stack_height;
	unsigned int vt_sp;
	unsigned int max_vt_sp;
	int n_data_items;
	int max_data_items;
	void **data_items;
	GHashTable *data_hash;
	int *clause_indexes;
	gboolean gen_sdb_seq_points;
	GPtrArray *seq_points;
	InterpBasicBlock **offset_to_bb;
	InterpBasicBlock *entry_bb;
	MonoMemPool     *mempool;
	GList *basic_blocks;
	GPtrArray *relocs;
	gboolean verbose_level;
} TransformData;

#define STACK_TYPE_I4 0
#define STACK_TYPE_I8 1
#define STACK_TYPE_R8 2
#define STACK_TYPE_O  3
#define STACK_TYPE_VT 4
#define STACK_TYPE_MP 5
#define STACK_TYPE_F  6

static const char *stack_type_string [] = { "I4", "I8", "R8", "O ", "VT", "MP", "F " };

#if SIZEOF_VOID_P == 8
#define STACK_TYPE_I STACK_TYPE_I8
#else
#define STACK_TYPE_I STACK_TYPE_I4
#endif

static int stack_type [] = {
	STACK_TYPE_I4, /*I1*/
	STACK_TYPE_I4, /*U1*/
	STACK_TYPE_I4, /*I2*/
	STACK_TYPE_I4, /*U2*/
	STACK_TYPE_I4, /*I4*/
	STACK_TYPE_I8, /*I8*/
	STACK_TYPE_R8, /*R4*/
	STACK_TYPE_R8, /*R8*/
	STACK_TYPE_O,  /*O*/
	STACK_TYPE_MP, /*P*/
	STACK_TYPE_VT
};

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

#else

#define MINT_NEG_P MINT_NEG_I4
#define MINT_NOT_P MINT_NOT_I4

#define MINT_NEG_FP MINT_NEG_R8

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

static void
grow_code (TransformData *td)
{
	unsigned int old_ip_offset = td->new_ip - td->new_code;
	unsigned int old_last_ip_offset = td->last_new_ip - td->new_code;
	g_assert (old_ip_offset <= td->max_code_size);
	td->new_code = g_realloc (td->new_code, (td->max_code_size *= 2) * sizeof (td->new_code [0]));
	td->new_code_end = td->new_code + td->max_code_size;
	td->new_ip = td->new_code + old_ip_offset;
	td->last_new_ip = td->new_code + old_last_ip_offset;
}

#define ENSURE_CODE(td, n) \
	do { \
		if ((td)->new_ip + (n) > (td)->new_code_end) \
			grow_code (td); \
	} while (0)

#define ADD_CODE(td, n) \
	do { \
		if ((td)->new_ip == (td)->new_code_end) \
			grow_code (td); \
		*(td)->new_ip++ = (n); \
	} while (0)

#define CHECK_STACK(td, n) \
	do { \
		int stack_size = (td)->sp - (td)->stack; \
		if (stack_size < (n)) \
			g_warning ("%s.%s: not enough values (%d < %d) on stack at %04x", \
				(td)->method->klass->name, (td)->method->name, \
				stack_size, n, (td)->ip - (td)->il_code); \
	} while (0)

#define ENSURE_I4(td, sp_off) \
	do { \
		if ((td)->sp [-sp_off].type == STACK_TYPE_I8) \
			ADD_CODE(td, sp_off == 1 ? MINT_CONV_I4_I8 : MINT_CONV_I4_I8_SP); \
	} while (0)

static void 
handle_branch (TransformData *td, int short_op, int long_op, int offset)
{
	int shorten_branch = 0;
	int target = td->ip + offset - td->il_code;
	if (target < 0 || target >= td->code_size)
		g_assert_not_reached ();
	if (offset > 0 && td->stack_height [target] < 0) {
		td->stack_height [target] = td->sp - td->stack;
		if (td->stack_height [target] > 0)
			td->stack_state [target] = g_memdup (td->stack, td->stack_height [target] * sizeof (td->stack [0]));
		td->vt_stack_size [target] = td->vt_sp;
	}
	if (offset < 0) {
		offset = td->in_offsets [target] - (td->new_ip - td->new_code);
		if (offset >= -32768) {
			shorten_branch = 1;
		}
	} else {
		if (td->header->code_size <= 25000) /* FIX to be precise somehow? */
			shorten_branch = 1;

		Reloc *reloc = mono_mempool_alloc0 (td->mempool, sizeof (Reloc));
		if (shorten_branch) {
			offset = 0xffff;
			reloc->type = RELOC_SHORT_BRANCH;
		} else {
			offset = 0xdeadbeef;
			reloc->type = RELOC_LONG_BRANCH;
		}
		reloc->offset = td->new_ip - td->new_code;
		reloc->target = target;
		g_ptr_array_add (td->relocs, reloc);
	}
	if (shorten_branch) {
		ADD_CODE(td, short_op);
		ADD_CODE(td, offset);
	} else {
		ADD_CODE(td, long_op);
		ADD_CODE(td, * (unsigned short *)(&offset));
		ADD_CODE(td, * ((unsigned short *)&offset + 1));
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
		ADD_CODE(td, MINT_CONV_I8_I4);
		td->in_offsets [td->ip - td->il_code]++;
	} else if (type1 == STACK_TYPE_I8 && type2 == STACK_TYPE_I4) {
		ADD_CODE(td, MINT_CONV_I8_I4_SP);
		td->in_offsets [td->ip - td->il_code]++;
	} else if (type1 != type2) {
		g_warning("%s.%s: branch type mismatch %d %d", 
			td->method->klass->name, td->method->name, 
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
	ADD_CODE(td, op);
}

static void
binary_arith_op(TransformData *td, int mint_op)
{
	int type1 = td->sp [-2].type;
	int type2 = td->sp [-1].type;
	int op;
#if SIZEOF_VOID_P == 8
	if ((type1 == STACK_TYPE_MP || type1 == STACK_TYPE_I8) && type2 == STACK_TYPE_I4) {
		ADD_CODE(td, MINT_CONV_I8_I4);
		type2 = STACK_TYPE_I8;
	}
	if (type1 == STACK_TYPE_I4 && (type2 == STACK_TYPE_MP || type2 == STACK_TYPE_I8)) {
		ADD_CODE(td, MINT_CONV_I8_I4_SP);
		type1 = STACK_TYPE_I8;
		td->sp [-2].type = STACK_TYPE_I8;
	}
#endif
	if (type1 == STACK_TYPE_MP)
		type1 = STACK_TYPE_I;
	if (type2 == STACK_TYPE_MP)
		type2 = STACK_TYPE_I;
	if (type1 != type2) {
		g_warning("%s.%s: %04x arith type mismatch %s %d %d", 
			td->method->klass->name, td->method->name, 
			td->ip - td->il_code, mono_interp_opname[mint_op], type1, type2);
	}
	op = mint_op + type1 - STACK_TYPE_I4;
	CHECK_STACK(td, 2);
	ADD_CODE(td, op);
	--td->sp;
}

static void
shift_op(TransformData *td, int mint_op)
{
	int op = mint_op + td->sp [-2].type - STACK_TYPE_I4;
	CHECK_STACK(td, 2);
	if (td->sp [-1].type != STACK_TYPE_I4) {
		g_warning("%s.%s: shift type mismatch %d", 
			td->method->klass->name, td->method->name,
			td->sp [-2].type);
	}
	ADD_CODE(td, op);
	--td->sp;
}

static int 
can_store (int stack_type, int var_type)
{
	if (stack_type == STACK_TYPE_O || stack_type == STACK_TYPE_MP)
		stack_type = STACK_TYPE_I;
	if (var_type == STACK_TYPE_O || var_type == STACK_TYPE_MP)
		var_type = STACK_TYPE_I;
	return stack_type == var_type;
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

#define PUSH_SIMPLE_TYPE(td, ty) \
	do { \
		int sp_height; \
		(td)->sp++; \
		sp_height = (td)->sp - (td)->stack; \
		if (sp_height > (td)->max_stack_height) \
			(td)->max_stack_height = sp_height; \
		SET_SIMPLE_TYPE((td)->sp - 1, ty); \
	} while (0)

#define PUSH_TYPE(td, ty, k) \
	do { \
		int sp_height; \
		(td)->sp++; \
		sp_height = (td)->sp - (td)->stack; \
		if (sp_height > (td)->max_stack_height) \
			(td)->max_stack_height = sp_height; \
		SET_TYPE((td)->sp - 1, ty, k); \
	} while (0)

#define PUSH_VT(td, size) \
	do { \
		(td)->vt_sp += ((size) + 7) & ~7; \
		if ((td)->vt_sp > (td)->max_vt_sp) \
			(td)->max_vt_sp = (td)->vt_sp; \
	} while (0)

#define POP_VT(td, size) \
	do { \
		(td)->vt_sp -= ((size) + 7) & ~7; \
	} while (0)

#if NO_UNALIGNED_ACCESS
#define WRITE32(td, v) \
	do { \
		ENSURE_CODE(td, 2); \
		* (guint16 *)((td)->new_ip) = * (guint16 *)(v); \
		* ((guint16 *)((td)->new_ip) + 1) = * ((guint16 *)(v) + 1); \
		(td)->new_ip += 2; \
	} while (0)

#define WRITE64(td, v) \
	do { \
		ENSURE_CODE(td, 4); \
		* (guint16 *)((td)->new_ip) = * (guint16 *)(v); \
		* ((guint16 *)((td)->new_ip) + 1) = * ((guint16 *)(v) + 1); \
		* ((guint16 *)((td)->new_ip) + 2) = * ((guint16 *)(v) + 2); \
		* ((guint16 *)((td)->new_ip) + 3) = * ((guint16 *)(v) + 3); \
		(td)->new_ip += 4; \
	} while (0)
#else
#define WRITE32(td, v) \
	do { \
		ENSURE_CODE(td, 2); \
		* (guint32 *)((td)->new_ip) = * (guint32 *)(v); \
		(td)->new_ip += 2; \
	} while (0)

#define WRITE64(td, v) \
	do { \
		ENSURE_CODE(td, 4); \
		* (guint64 *)((td)->new_ip) = * (guint64 *)(v); \
		(td)->new_ip += 4; \
	} while (0)

#endif

static void 
load_arg(TransformData *td, int n)
{
	int mt;
	MonoClass *klass = NULL;
	MonoType *type;

	gboolean hasthis = mono_method_signature (td->method)->hasthis;
	if (hasthis && n == 0)
		type = &td->method->klass->byval_arg;
	else
		type = mono_method_signature (td->method)->params [hasthis ? n - 1 : n];

	mt = mint_type (type);
	if (mt == MINT_TYPE_VT) {
		gint32 size;
		klass = mono_class_from_mono_type (type);
		if (mono_method_signature (td->method)->pinvoke)
			size = mono_class_native_size (klass, NULL);
		else
			size = mono_class_value_size (klass, NULL);

		if (hasthis && n == 0) {
			mt = MINT_TYPE_P;
			ADD_CODE (td, MINT_LDARG_P);
			ADD_CODE (td, td->rtm->arg_offsets [n]); /* FIX for large offset */
			klass = NULL;
		} else {
			PUSH_VT (td, size);
			ADD_CODE (td, MINT_LDARG_VT);
			ADD_CODE (td, td->rtm->arg_offsets [n]); /* FIX for large offset */
			WRITE32 (td, &size);
		}
	} else {
		if (hasthis && n == 0) {
			mt = MINT_TYPE_P;
			ADD_CODE (td, MINT_LDARG_P);
			ADD_CODE (td, td->rtm->arg_offsets [n]); /* FIX for large offset */
			klass = NULL;
		} else {
			ADD_CODE(td, MINT_LDARG_I1 + (mt - MINT_TYPE_I1));
			ADD_CODE(td, td->rtm->arg_offsets [n]); /* FIX for large offset */
			if (mt == MINT_TYPE_O)
				klass = mono_class_from_mono_type (type);
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

	gboolean hasthis = mono_method_signature (td->method)->hasthis;
	if (hasthis && n == 0)
		type = &td->method->klass->byval_arg;
	else
		type = mono_method_signature (td->method)->params [n - !!hasthis];

	mt = mint_type (type);
	if (mt == MINT_TYPE_VT) {
		gint32 size;
		MonoClass *klass = mono_class_from_mono_type (type);
		if (mono_method_signature (td->method)->pinvoke)
			size = mono_class_native_size (klass, NULL);
		else
			size = mono_class_value_size (klass, NULL);
		ADD_CODE(td, MINT_STARG_VT);
		ADD_CODE(td, td->rtm->arg_offsets [n]);
		WRITE32(td, &size);
		if (td->sp [-1].type == STACK_TYPE_VT)
			POP_VT(td, size);
	} else {
		ADD_CODE(td, MINT_STARG_I1 + (mt - MINT_TYPE_I1));
		ADD_CODE(td, td->rtm->arg_offsets [n]);
	}
	--td->sp;
}

static void 
store_inarg(TransformData *td, int n)
{
	MonoType *type;
	gboolean hasthis = mono_method_signature (td->method)->hasthis;
	if (hasthis && n == 0)
		type = &td->method->klass->byval_arg;
	else
		type = mono_method_signature (td->method)->params [n - !!hasthis];

	int mt = mint_type (type);
	if (hasthis && n == 0) {
		ADD_CODE (td, MINT_STINARG_P);
		ADD_CODE (td, n);
		return;
	}
	if (mt == MINT_TYPE_VT) {
		MonoClass *klass = mono_class_from_mono_type (type);
		gint32 size;
		if (mono_method_signature (td->method)->pinvoke)
			size = mono_class_native_size (klass, NULL);
		else
			size = mono_class_value_size (klass, NULL);
		ADD_CODE(td, MINT_STINARG_VT);
		ADD_CODE(td, n);
		WRITE32(td, &size);
	} else {
		ADD_CODE(td, MINT_STINARG_I1 + (mt - MINT_TYPE_I1));
		ADD_CODE(td, n);
	}
}

static void 
load_local(TransformData *td, int n)
{
	MonoType *type = td->header->locals [n];
	int mt = mint_type (type);
	int offset = td->rtm->local_offsets [n];
	MonoClass *klass = NULL;
	if (mt == MINT_TYPE_VT) {
		klass = mono_class_from_mono_type (type);
		gint32 size = mono_class_value_size (klass, NULL);
		PUSH_VT(td, size);
		ADD_CODE(td, MINT_LDLOC_VT);
		ADD_CODE(td, offset); /*FIX for large offset */
		WRITE32(td, &size);
	} else {
		g_assert (mt < MINT_TYPE_VT);
		if (!td->gen_sdb_seq_points &&
			mt == MINT_TYPE_I4 && !td->is_bb_start [td->in_start - td->il_code] && td->last_new_ip != NULL &&
			td->last_new_ip [0] == MINT_STLOC_I4 && td->last_new_ip [1] == offset) {
			td->last_new_ip [0] = MINT_STLOC_NP_I4;
		} else if (!td->gen_sdb_seq_points &&
				   mt == MINT_TYPE_O && !td->is_bb_start [td->in_start - td->il_code] && td->last_new_ip != NULL &&
				   td->last_new_ip [0] == MINT_STLOC_O && td->last_new_ip [1] == offset) {
			td->last_new_ip [0] = MINT_STLOC_NP_O;
		} else {
			ADD_CODE(td, MINT_LDLOC_I1 + (mt - MINT_TYPE_I1));
			ADD_CODE(td, offset); /*FIX for large offset */
		}
		if (mt == MINT_TYPE_O)
			klass = mono_class_from_mono_type (type);
	}
	PUSH_TYPE(td, stack_type[mt], klass);
}

static void 
store_local(TransformData *td, int n)
{
	MonoType *type = td->header->locals [n];
	int mt = mint_type (type);
	int offset = td->rtm->local_offsets [n];
	CHECK_STACK (td, 1);
#if SIZEOF_VOID_P == 8
	if (td->sp [-1].type == STACK_TYPE_I4 && stack_type [mt] == STACK_TYPE_I8) {
		ADD_CODE(td, MINT_CONV_I8_I4);
		td->sp [-1].type = STACK_TYPE_I8;
	}
#endif
	if (!can_store(td->sp [-1].type, stack_type [mt])) {
		g_warning("%s.%s: Store local stack type mismatch %d %d", 
			td->method->klass->name, td->method->name,
			stack_type [mt], td->sp [-1].type);
	}
	if (mt == MINT_TYPE_VT) {
		MonoClass *klass = mono_class_from_mono_type (type);
		gint32 size = mono_class_value_size (klass, NULL);
		ADD_CODE(td, MINT_STLOC_VT);
		ADD_CODE(td, offset); /*FIX for large offset */
		WRITE32(td, &size);
		if (td->sp [-1].type == STACK_TYPE_VT)
			POP_VT(td, size);
	} else {
		g_assert (mt < MINT_TYPE_VT);
		ADD_CODE(td, MINT_STLOC_I1 + (mt - MINT_TYPE_I1));
		ADD_CODE(td, offset); /*FIX for large offset */
	}
	--td->sp;
}

#define SIMPLE_OP(td, op) \
	do { \
		ADD_CODE(td, op); \
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
		td->data_items = g_realloc (td->data_items, td->max_data_items * sizeof(td->data_items [0]));
	}
	index = td->n_data_items;
	td->data_items [index] = ptr;
	++td->n_data_items;
	g_hash_table_insert (td->data_hash, ptr, GUINT_TO_POINTER (index + 1));
	return index;
}

static gboolean
jit_call_supported (MonoMethod *method, MonoMethodSignature *sig)
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

	for (l = jit_classes; l; l = l->next) {
		char *class_name = l->data;
		// FIXME: Namespaces
		if (!strcmp (method->klass->name, class_name))
			return TRUE;
	}

	//return TRUE;
	return FALSE;
}

static inline gboolean
type_size (MonoType *type)
{
	if (type->type == MONO_TYPE_I4 || type->type == MONO_TYPE_U4)
		return 4;
	else if (type->type == MONO_TYPE_I8 || type->type == MONO_TYPE_U8)
		return 8;
	else if (type->type == MONO_TYPE_R4 && !type->byref)
		return 4;
	else if (type->type == MONO_TYPE_R8 && !type->byref)
		return 8;
	return SIZEOF_VOID_P;
}

static int mono_class_get_magic_index (MonoClass *k)
{
	if (mono_class_is_magic_int (k))
		return !strcmp ("nint", k->name) ? 0 : 1;

	if (mono_class_is_magic_float (k))
		return 2;

	return -1;
}


static void
interp_transform_call (TransformData *td, MonoMethod *method, MonoMethod *target_method, MonoDomain *domain, MonoGenericContext *generic_context, unsigned char *is_bb_start, int body_start_offset, MonoClass *constrained_class, gboolean readonly, MonoError *error)
{
	MonoImage *image = method->klass->image;
	MonoMethodSignature *csignature;
	int virtual = *td->ip == CEE_CALLVIRT;
	int calli = *td->ip == CEE_CALLI || *td->ip == CEE_MONO_CALLI_EXTRA_ARG;
	int i;
	guint32 vt_stack_used = 0;
	guint32 vt_res_size = 0;
	int op = -1;
	int native = 0;
	int is_void = 0;

	guint32 token = read32 (td->ip + 1);

	if (target_method == NULL) {
		if (calli) {
			CHECK_STACK(td, 1);
			native = (method->wrapper_type != MONO_WRAPPER_DELEGATE_INVOKE && td->sp [-1].type == STACK_TYPE_I);
			--td->sp;
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				csignature = (MonoMethodSignature *)mono_method_get_wrapper_data (method, token);
			else
				csignature = mono_metadata_parse_signature (image, token);

			if (generic_context) {
				csignature = mono_inflate_generic_signature (csignature, generic_context, error);
				return_if_nok (error);
			}

			target_method = NULL;
		} else {
			if (method->wrapper_type == MONO_WRAPPER_NONE)
				target_method = mono_get_method_full (image, token, NULL, generic_context);
			else
				target_method = (MonoMethod *)mono_method_get_wrapper_data (method, token);
			csignature = mono_method_signature (target_method);

			if (generic_context) {
				csignature = mono_inflate_generic_signature (csignature, generic_context, error);
				return_if_nok (error);
				target_method = mono_class_inflate_generic_method_checked (target_method, generic_context, error);
				return_if_nok (error);
			}
		}
	} else {
		csignature = mono_method_signature (target_method);
	}

	if (target_method && target_method->string_ctor) {
		/* Create the real signature */
		MonoMethodSignature *ctor_sig = mono_metadata_signature_dup_mempool (td->mempool, csignature);
		ctor_sig->ret = &mono_defaults.string_class->byval_arg;

		csignature = ctor_sig;
	}

	/* Intrinsics */
	if (target_method) {
		const char *tm = target_method->name;
		int type_index = mono_class_get_magic_index (target_method->klass);

		if (target_method->klass == mono_defaults.string_class) {
			if (tm [0] == 'g') {
				if (strcmp (tm, "get_Chars") == 0)
					op = MINT_GETCHR;
				else if (strcmp (tm, "get_Length") == 0)
					op = MINT_STRLEN;
			}
		} else if (type_index >= 0) {
			MonoClass *magic_class = target_method->klass;

			const int mt = mint_type (&magic_class->byval_arg);
			if (!strcmp (".ctor", tm)) {
				MonoType *arg = csignature->params [0];
				/* depending on SIZEOF_VOID_P and the type of the value passed to the .ctor we either have to CONV it, or do nothing */
				int arg_size = type_size (arg);

				if (arg_size > SIZEOF_VOID_P) { // 8 -> 4
					switch (type_index) {
					case 0: case 1:
						ADD_CODE (td, MINT_CONV_I8_I4);
						break;
					case 2:
						// ADD_CODE (td, MINT_CONV_R8_R4);
						break;
					}
				}

				if (arg_size < SIZEOF_VOID_P) { // 4 -> 8
					switch (type_index) {
					case 0: case 1:
						ADD_CODE (td, MINT_CONV_I4_I8);
						break;
					case 2:
						ADD_CODE (td, MINT_CONV_R4_R8);
						break;
					}
				}

				switch (type_index) {
				case 0: case 1:
#if SIZEOF_VOID_P == 4
					ADD_CODE (td, MINT_STIND_I4);
#else
					ADD_CODE (td, MINT_STIND_I8);
#endif
					break;
				case 2:
#if SIZEOF_VOID_P == 4
					ADD_CODE (td, MINT_STIND_R4);
#else
					ADD_CODE (td, MINT_STIND_R8);
#endif
					break;
				}

				td->sp -= 2;
				td->ip += 5;
				return;
			} else if (!strcmp ("op_Implicit", tm ) || !strcmp ("op_Explicit", tm)) {
				int arg_size = type_size (csignature->params [0]);
				if (arg_size > SIZEOF_VOID_P) { // 8 -> 4
					switch (type_index) {
					case 0: case 1:
						ADD_CODE (td, MINT_CONV_I8_I4);
						break;
					case 2:
						// ADD_CODE (td, MINT_CONV_R4_R8);
						break;
					}
				}

				if (arg_size < SIZEOF_VOID_P) { // 4 -> 8
					switch (type_index) {
					case 0: case 1:
						ADD_CODE (td, MINT_CONV_I4_I8);
						break;
					case 2:
						ADD_CODE (td, MINT_CONV_R4_R8);
						break;
					}
				}

				SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
				td->ip += 5;
				return;
			} else if (!strcmp (".cctor", tm)) {
				/* white list */
				goto no_intrinsic;
			} else if (!strcmp ("Parse", tm)) {
				/* white list */
				goto no_intrinsic;
			}

			for (i = 0; i < sizeof (int_unnop) / sizeof  (MagicIntrinsic); ++i) {
				if (!strcmp (int_unnop [i].op_name, tm)) {
					ADD_CODE (td, int_unnop [i].insn [type_index]);
					SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
					td->ip += 5;
					return;
				}
			}

			for (i = 0; i < sizeof (int_binop) / sizeof  (MagicIntrinsic); ++i) {
				if (!strcmp (int_binop [i].op_name, tm)) {
					ADD_CODE (td, int_binop [i].insn [type_index]);
					td->sp -= 1;
					SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
					td->ip += 5;
					return;
				}
			}

			for (i = 0; i < sizeof (int_cmpop) / sizeof  (MagicIntrinsic); ++i) {
				if (!strcmp (int_cmpop [i].op_name, tm)) {
					MonoClass *k = mono_defaults.boolean_class;
					ADD_CODE (td, int_cmpop [i].insn [type_index]);
					td->sp -= 1;
					SET_TYPE (td->sp - 1, stack_type [mint_type (&k->byval_arg)], k);
					td->ip += 5;
					return;
				}
			}

			g_error ("TODO: interp_transform_call %s:%s", target_method->klass->name, tm);
		} else if (mono_class_is_subclass_of (target_method->klass, mono_defaults.array_class, FALSE)) {
			if (!strcmp (tm, "get_Rank")) {
				op = MINT_ARRAY_RANK;
			} else if (!strcmp (tm, "get_Length")) {
				op = MINT_LDLEN;
			} else if (!strcmp (tm, "Address")) {
				op = readonly ? MINT_LDELEMA : MINT_LDELEMA_TC;
			}
		} else if (target_method->klass->image == mono_defaults.corlib &&
				   (strcmp (target_method->klass->name_space, "System.Diagnostics") == 0) &&
				   (strcmp (target_method->klass->name, "Debugger") == 0)) {
			if (!strcmp (tm, "Break") && csignature->param_count == 0) {
				if (mini_should_insert_breakpoint (method))
					op = MINT_BREAK;
			}
		}
	}
no_intrinsic:

	if (constrained_class) {
		if (constrained_class->enumtype && !strcmp (target_method->name, "GetHashCode")) {
			/* Use the corresponding method from the base type to avoid boxing */
			MonoType *base_type = mono_class_enum_basetype (constrained_class);
			g_assert (base_type);
			constrained_class = mono_class_from_mono_type (base_type);
			target_method = mono_class_get_method_from_name (constrained_class, target_method->name, 0);
			g_assert (target_method);
		}
	}

	if (constrained_class) {
		mono_class_setup_vtable (constrained_class);
#if DEBUG_INTERP
		g_print ("CONSTRAINED.CALLVIRT: %s::%s.  %s (%p) ->\n", target_method->klass->name, target_method->name, mono_signature_full_name (target_method->signature), target_method);
#endif
		target_method = mono_get_method_constrained_with_method (image, target_method, constrained_class, generic_context, error);
#if DEBUG_INTERP
		g_print ("                    : %s::%s.  %s (%p)\n", target_method->klass->name, target_method->name, mono_signature_full_name (target_method->signature), target_method);
#endif
		return_if_nok (error);
		mono_class_setup_vtable (target_method->klass);

		if (constrained_class->valuetype && (target_method->klass == mono_defaults.object_class || target_method->klass == mono_defaults.enum_class->parent || target_method->klass == mono_defaults.enum_class)) {
			if (target_method->klass == mono_defaults.enum_class && (td->sp - csignature->param_count - 1)->type == STACK_TYPE_MP) {
				/* managed pointer on the stack, we need to deref that puppy */
				ADD_CODE (td, MINT_LDIND_I);
				ADD_CODE (td, csignature->param_count);
			}
			ADD_CODE (td, MINT_BOX);
			ADD_CODE (td, get_data_item_index (td, constrained_class));
			ADD_CODE (td, csignature->param_count | ((td->sp - 1)->type != STACK_TYPE_MP ? 0 : BOX_NOT_CLEAR_VT_SP));
		} else if (!constrained_class->valuetype) {
			/* managed pointer on the stack, we need to deref that puppy */
			ADD_CODE (td, MINT_LDIND_I);
			ADD_CODE (td, csignature->param_count);
		} else {
			if (target_method->klass->valuetype) {
				/* Own method */
			} else {
				/* Interface method */
				int ioffset, slot;

				mono_class_setup_vtable (constrained_class);
				ioffset = mono_class_interface_offset (constrained_class, target_method->klass);
				if (ioffset == -1)
					g_error ("type load error: constrained_class");
				slot = mono_method_get_vtable_slot (target_method);
				if (slot == -1)
					g_error ("type load error: target_method->klass");
				target_method = constrained_class->vtable [ioffset + slot];

				if (target_method->klass == mono_defaults.enum_class) {
					if ((td->sp - csignature->param_count - 1)->type == STACK_TYPE_MP) {
						/* managed pointer on the stack, we need to deref that puppy */
						ADD_CODE (td, MINT_LDIND_I);
						ADD_CODE (td, csignature->param_count);
					}
					ADD_CODE (td, MINT_BOX);
					ADD_CODE (td, get_data_item_index (td, constrained_class));
					ADD_CODE (td, csignature->param_count | ((td->sp - 1)->type != STACK_TYPE_MP ? 0 : BOX_NOT_CLEAR_VT_SP));
				}
			}
			virtual = FALSE;
		}
	}

	if (target_method)
		mono_class_init (target_method->klass);

	CHECK_STACK (td, csignature->param_count + csignature->hasthis);
	if (!calli && op == -1 && (!virtual || (target_method->flags & METHOD_ATTRIBUTE_VIRTUAL) == 0) &&
		(target_method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) == 0 && 
		(target_method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) == 0 &&
		!(target_method->iflags & METHOD_IMPL_ATTRIBUTE_NOINLINING)) {
		int called_inited = mono_class_vtable (domain, target_method->klass)->initialized;

		if (/*mono_metadata_signature_equal (method->signature, target_method->signature) */ method == target_method && *(td->ip + 5) == CEE_RET) {
			int offset;
			if (td->verbose_level)
				g_print ("Optimize tail call of %s.%s\n", target_method->klass->name, target_method->name);

			for (i = csignature->param_count - 1 + !!csignature->hasthis; i >= 0; --i)
				store_arg (td, i);

			ADD_CODE(td, MINT_BR_S);
			offset = body_start_offset - ((td->new_ip - 1) - td->new_code);
			ADD_CODE(td, offset);
			if (!is_bb_start [td->ip + 5 - td->il_code])
				++td->ip; /* gobble the CEE_RET if it isn't branched to */				
			td->ip += 5;
			return;
		} else {
			MonoMethodHeader *mheader = mono_method_get_header (target_method);
			/* mheader might not exist if this is a delegate invoc, etc */
			gboolean has_vt_arg = FALSE;
			for (i = 0; i < csignature->param_count; i++)
				has_vt_arg |= !mini_type_is_reference (csignature->params [i]);

			gboolean empty_callee = mheader && *mheader->code == CEE_RET;
			if (mheader)
				mono_metadata_free_mh (mheader);

			if (empty_callee && called_inited && !has_vt_arg) {
				if (td->verbose_level)
					g_print ("Inline (empty) call of %s.%s\n", target_method->klass->name, target_method->name);
				for (i = 0; i < csignature->param_count; i++) {
					ADD_CODE (td, MINT_POP); /*FIX: vt */
					ADD_CODE (td, 0);
				}
				if (csignature->hasthis) {
					if (virtual)
						ADD_CODE(td, MINT_CKNULL);
					ADD_CODE (td, MINT_POP);
					ADD_CODE (td, 0);
				}
				td->sp -= csignature->param_count + csignature->hasthis;
				td->ip += 5;
				return;
			}
		}
	}
	if (method->wrapper_type == MONO_WRAPPER_NONE && target_method != NULL) {
		if (target_method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
			target_method = mono_marshal_get_native_wrapper (target_method, FALSE, FALSE);
		if (!virtual && target_method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
			target_method = mono_marshal_get_synchronized_wrapper (target_method);
	}

	if (csignature->call_convention == MONO_CALL_VARARG) {
		char *fullname = mono_method_full_name (method, TRUE);
		mono_error_set_execution_engine (error, "__arglist not supported yet: %s\n", fullname);
		g_free (fullname);
		return;
	}

	g_assert (csignature->call_convention == MONO_CALL_DEFAULT || csignature->call_convention == MONO_CALL_C || csignature->call_convention == MONO_CALL_STDCALL);
	td->sp -= csignature->param_count + !!csignature->hasthis;
	for (i = 0; i < csignature->param_count; ++i) {
		if (td->sp [i + !!csignature->hasthis].type == STACK_TYPE_VT) {
			gint32 size;
			MonoClass *klass = mono_class_from_mono_type (csignature->params [i]);
			if (csignature->pinvoke && method->wrapper_type != MONO_WRAPPER_NONE)
				size = mono_class_native_size (klass, NULL);
			else
				size = mono_class_value_size (klass, NULL);
			size = (size + 7) & ~7;
			vt_stack_used += size;
		}
	}

	/* need to handle typedbyref ... */
	if (csignature->ret->type != MONO_TYPE_VOID) {
		int mt = mint_type(csignature->ret);
		MonoClass *klass = mono_class_from_mono_type (csignature->ret);
		if (mt == MINT_TYPE_VT) {
			if (csignature->pinvoke && method->wrapper_type != MONO_WRAPPER_NONE)
				vt_res_size = mono_class_native_size (klass, NULL);
			else
				vt_res_size = mono_class_value_size (klass, NULL);
			PUSH_VT(td, vt_res_size);
		}
		PUSH_TYPE(td, stack_type[mt], klass);
	} else
		is_void = TRUE;

	if (op >= 0) {
		ADD_CODE (td, op);
#if SIZEOF_VOID_P == 8
		if (op == MINT_LDLEN)
			ADD_CODE (td, MINT_CONV_I4_I8);
#endif
		if (op == MINT_LDELEMA || op == MINT_LDELEMA_TC) {
			ADD_CODE (td, get_data_item_index (td, target_method->klass));
			ADD_CODE (td, 1 + target_method->klass->rank);
		}
	} else if (!calli && !virtual && jit_call_supported (target_method, csignature)) {
		ADD_CODE(td, MINT_JIT_CALL);
		ADD_CODE(td, get_data_item_index (td, (void *)mono_interp_get_imethod (domain, target_method, error)));
		mono_error_assert_ok (error);
	} else {
		if (calli)
			ADD_CODE(td, native ? MINT_CALLI_NAT : MINT_CALLI);
		else if (virtual)
			ADD_CODE(td, is_void ? MINT_VCALLVIRT : MINT_CALLVIRT);
		else
			ADD_CODE(td, is_void ? MINT_VCALL : MINT_CALL);

		if (calli) {
			ADD_CODE(td, get_data_item_index (td, (void *)csignature));
		} else {
			ADD_CODE(td, get_data_item_index (td, (void *)mono_interp_get_imethod (domain, target_method, error)));
			return_if_nok (error);
		}
	}
	td->ip += 5;
	if (vt_stack_used != 0 || vt_res_size != 0) {
		ADD_CODE(td, MINT_VTRESULT);
		ADD_CODE(td, vt_res_size);
		WRITE32(td, &vt_stack_used);
		td->vt_sp -= vt_stack_used;
	}
}

static MonoClassField *
interp_field_from_token (MonoMethod *method, guint32 token, MonoClass **klass, MonoGenericContext *generic_context, MonoError *error)
{
	MonoClassField *field = NULL;
	if (method->wrapper_type != MONO_WRAPPER_NONE) {
		field = (MonoClassField *) mono_method_get_wrapper_data (method, token);
		*klass = field->parent;
	} else {
		field = mono_field_from_token_checked (method->klass->image, token, klass, generic_context, error);
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
		bb = mono_mempool_alloc0 (td->mempool, sizeof (InterpBasicBlock));
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

	td->offset_to_bb = mono_mempool_alloc0 (td->mempool, sizeof (InterpBasicBlock*) * (end - start + 1));
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
	dinfo->code_size = td->new_ip - td->new_code;
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
	mono_debug_add_method (rtm->method, dinfo, mono_domain_get ());

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
	const gpointer MONO_SEQ_SEEN_LOOP = GINT_TO_POINTER(-1);
	GSList *l;

	GArray *predecessors = g_array_new (FALSE, TRUE, sizeof (gpointer));
	GHashTable *seen = g_hash_table_new_full (g_direct_hash, NULL, NULL, NULL);

	// Insert/remove sentinel into the memoize table to detect loops containing bb
	bb->pred_seq_points = MONO_SEQ_SEEN_LOOP;

	for (l = bb->preds; l; l = l->next) {
		InterpBasicBlock *in_bb = l->data;

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
		bb->pred_seq_points = mono_mempool_alloc0 (td->mempool, sizeof (SeqPoint *) * predecessors->len);
		bb->num_pred_seq_points = predecessors->len;

		for (int newer = 0; newer < bb->num_pred_seq_points; newer++) {
			bb->pred_seq_points [newer] = g_array_index (predecessors, gpointer, newer);
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
save_seq_points (TransformData *td)
{
	InterpMethod *rtm = td->rtm;
	GByteArray *array;
	int i, seq_info_size;
	MonoSeqPointInfo *info;
	MonoDomain *domain = mono_domain_get ();
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
		SeqPoint *sp = g_ptr_array_index (td->seq_points, i);

		/* Store the seq point index here temporarily */
		sp->next_offset = i;
	}
	next = mono_mempool_alloc0 (td->mempool, sizeof (GList*) * td->seq_points->len);
	for (bblist = td->basic_blocks; bblist; bblist = bblist->next) {
		InterpBasicBlock *bb = bblist->data;

		GSList *bb_seq_points = g_slist_reverse (bb->seq_points);
		SeqPoint *last = NULL;
		for (GSList *l = bb_seq_points; l; l = l->next) {
			SeqPoint *sp = l->data;

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

	mono_domain_lock (domain);
	g_hash_table_insert (domain_jit_info (domain)->seq_points, rtm->method, info);
	mono_domain_unlock (domain);
}

static void
emit_seq_point (TransformData *td, int il_offset, InterpBasicBlock *cbb, gboolean nonempty_stack)
{
	SeqPoint *seqp;

	seqp = mono_mempool_alloc0 (td->mempool, sizeof (SeqPoint));
	seqp->il_offset = il_offset;
	seqp->native_offset = (guint8*)td->new_ip - (guint8*)td->new_code;
	if (nonempty_stack)
		seqp->flags |= MONO_SEQ_POINT_FLAG_NONEMPTY_STACK;

	ADD_CODE (td, MINT_SDB_SEQ_POINT);
	g_ptr_array_add (td->seq_points, seqp);

	cbb->seq_points = g_slist_prepend_mempool (td->mempool, cbb->seq_points, seqp);
	cbb->last_seq_point = seqp;
}

#define BARRIER_IF_VOLATILE(td) \
	do { \
		if (volatile_) { \
			ADD_CODE (td, MINT_MONO_MEMORY_BARRIER); \
			volatile_ = FALSE; \
		} \
	} while (0)

static void
generate (MonoMethod *method, MonoMethodHeader *header, InterpMethod *rtm, unsigned char *is_bb_start, MonoGenericContext *generic_context, MonoError *error)
{
	MonoMethodSignature *signature = mono_method_signature (method);
	MonoImage *image = method->klass->image;
	MonoDomain *domain = rtm->domain;
	MonoClass *constrained_class = NULL;
	int offset, mt, i, i32;
	gboolean readonly = FALSE;
	gboolean volatile_ = FALSE;
	MonoClass *klass;
	MonoClassField *field;
	const unsigned char *end;
	int new_in_start_offset;
	int body_start_offset;
	int target;
	guint32 token;
	TransformData transform_data;
	TransformData *td;
	int generating_code = 1;
	GArray *line_numbers;
	MonoDebugMethodInfo *minfo;
	MonoBitSet *seq_point_locs = NULL;
	MonoBitSet *seq_point_set_locs = NULL;
	gboolean sym_seq_points = FALSE;
	InterpBasicBlock *bb_exit = NULL;
	static gboolean verbose_method_inited;
	static char* verbose_method_name;

	if (!verbose_method_inited) {
		verbose_method_name = getenv ("MONO_VERBOSE_METHOD");
		verbose_method_inited = TRUE;
	}

	memset (&transform_data, 0, sizeof(transform_data));
	td = &transform_data;

	td->method = method;
	td->rtm = rtm;
	td->is_bb_start = is_bb_start;
	td->il_code = header->code;
	td->code_size = header->code_size;
	td->header = header;
	td->max_code_size = td->code_size;
	td->new_code = (unsigned short *)g_malloc(td->max_code_size * sizeof(gushort));
	td->new_code_end = td->new_code + td->max_code_size;
	td->mempool = mono_mempool_new ();
	td->in_offsets = g_malloc0((header->code_size + 1) * sizeof(int));
	td->stack_state = g_malloc0(header->code_size * sizeof(StackInfo *));
	td->stack_height = g_malloc(header->code_size * sizeof(int));
	td->vt_stack_size = g_malloc(header->code_size * sizeof(int));
	td->n_data_items = 0;
	td->max_data_items = 0;
	td->data_items = NULL;
	td->data_hash = g_hash_table_new (NULL, NULL);
	td->clause_indexes = g_malloc (header->code_size * sizeof (int));
	td->gen_sdb_seq_points = debug_options.gen_sdb_seq_points;
	td->seq_points = g_ptr_array_new ();
	td->relocs = g_ptr_array_new ();
	td->verbose_level = mono_interp_traceopt;
	rtm->data_items = td->data_items;
	for (i = 0; i < header->code_size; i++) {
		td->stack_height [i] = -1;
		td->clause_indexes [i] = -1;
	}

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

	if (td->gen_sdb_seq_points) {
		get_basic_blocks (td);

		minfo = mono_debug_lookup_method (method);

		if (minfo) {
			MonoSymSeqPoint *sps;
			int i, n_il_offsets;

			mono_debug_get_seq_points (minfo, NULL, NULL, NULL, &sps, &n_il_offsets);
			// FIXME: Free
			seq_point_locs = mono_bitset_mem_new (mono_mempool_alloc0 (td->mempool, mono_bitset_alloc_size (header->code_size, 0)), header->code_size, 0);
			seq_point_set_locs = mono_bitset_mem_new (mono_mempool_alloc0 (td->mempool, mono_bitset_alloc_size (header->code_size, 0)), header->code_size, 0);
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
		} else if (!method->wrapper_type && !method->dynamic && mono_debug_image_has_debug_info (method->klass->image)) {
			/* Methods without line number info like auto-generated property accessors */
			seq_point_locs = mono_bitset_new (header->code_size, 0);
			seq_point_set_locs = mono_bitset_new (header->code_size, 0);
			sym_seq_points = TRUE;
		}
	}

	td->new_ip = td->new_code;
	td->last_new_ip = NULL;

	td->stack = g_malloc0 ((header->max_stack + 1) * sizeof (td->stack [0]));
	td->sp = td->stack;
	td->max_stack_height = 0;

	line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));

	for (i = 0; i < header->num_clauses; i++) {
		MonoExceptionClause *c = header->clauses + i;
		td->stack_height [c->handler_offset] = 0;
		td->vt_stack_size [c->handler_offset] = 0;
		td->is_bb_start [c->handler_offset] = 1;

		td->stack_height [c->handler_offset] = 1;
		td->stack_state [c->handler_offset] = g_malloc0(sizeof(StackInfo));
		td->stack_state [c->handler_offset][0].type = STACK_TYPE_O;
		td->stack_state [c->handler_offset][0].klass = NULL; /*FIX*/

		if (c->flags & MONO_EXCEPTION_CLAUSE_FILTER) {
			td->stack_height [c->data.filter_offset] = 0;
			td->vt_stack_size [c->data.filter_offset] = 0;
			td->is_bb_start [c->data.filter_offset] = 1;

			td->stack_height [c->data.filter_offset] = 1;
			td->stack_state [c->data.filter_offset] = g_malloc0(sizeof(StackInfo));
			td->stack_state [c->data.filter_offset][0].type = STACK_TYPE_O;
			td->stack_state [c->data.filter_offset][0].klass = NULL; /*FIX*/
		}

		for (int j = c->handler_offset; j < c->handler_offset + c->handler_len; ++j) {
			if (td->clause_indexes [j] == -1)
				td->clause_indexes [j] = i;
		}
	}

	td->ip = header->code;
	end = td->ip + header->code_size;

	if (td->verbose_level) {
		char *tmp = mono_disasm_code (NULL, method, td->ip, end);
		char *name = mono_method_full_name (method, TRUE);
		g_print ("Method %s, original code:\n", name);
		g_print ("%s\n", tmp);
		g_free (tmp);
		g_free (name);
	}

	if (signature->hasthis)
		store_inarg (td, 0);
	for (i = 0; i < signature->param_count; i++)
		store_inarg (td, i + !!signature->hasthis);

	body_start_offset = td->new_ip - td->new_code;

	for (i = 0; i < header->num_locals; i++) {
		int mt = mint_type(header->locals [i]);
		if (mt == MINT_TYPE_VT || mt == MINT_TYPE_O || mt == MINT_TYPE_P) {
			ADD_CODE(td, MINT_INITLOCALS);
			break;
		}
	}

	if (rtm->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_ENTER)
		ADD_CODE (td, MINT_PROF_ENTER);

	if (sym_seq_points) {
		InterpBasicBlock *cbb = td->offset_to_bb [0];
		g_assert (cbb);
		emit_seq_point (td, METHOD_ENTRY_IL_OFFSET, cbb, FALSE);
	}

	int in_offset;
	while (td->ip < end) {
		g_assert (td->sp >= td->stack);
		g_assert (td->vt_sp < 0x10000000);
		in_offset = td->ip - header->code;
		td->in_offsets [in_offset] = td->new_ip - td->new_code;
		new_in_start_offset = td->new_ip - td->new_code;
		td->in_start = td->ip;

		MonoDebugLineNumberEntry lne;
		lne.native_offset = (guint8*)td->new_ip - (guint8*)td->new_code;
		lne.il_offset = in_offset;
		g_array_append_val (line_numbers, lne);

		if (td->stack_height [in_offset] >= 0) {
			g_assert (is_bb_start [in_offset]);
			if (td->stack_height [in_offset] > 0)
				memcpy (td->stack, td->stack_state [in_offset], td->stack_height [in_offset] * sizeof(td->stack [0]));
			td->sp = td->stack + td->stack_height [in_offset];
			td->vt_sp = td->vt_stack_size [in_offset];
		}
		if (is_bb_start [in_offset]) {
			generating_code = 1;
		}
		if (!generating_code) {
			while (td->ip < end && !is_bb_start [td->ip - td->il_code])
				++td->ip;
			continue;
		}
		if (td->verbose_level > 1) {
			g_print ("IL_%04lx %s %-10s -> IL_%04lx, sp %ld, %s %-12s vt_sp %u (max %u)\n", 
				td->ip - td->il_code,
				td->is_bb_start [td->ip - td->il_code] == 3 ? "<>" :
				td->is_bb_start [td->ip - td->il_code] == 2 ? "< " :
				td->is_bb_start [td->ip - td->il_code] == 1 ? " >" : "  ",
				mono_opcode_name (*td->ip), td->new_ip - td->new_code, td->sp - td->stack, 
				td->sp > td->stack ? stack_type_string [td->sp [-1].type] : "  ",
				(td->sp > td->stack && (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_VT)) ? (td->sp [-1].klass == NULL ? "?" : td->sp [-1].klass->name) : "",
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
				ADD_CODE (td, MINT_SDB_INTR_LOC);

			emit_seq_point (td, in_offset, cbb, FALSE);

			mono_bitset_set_fast (seq_point_set_locs, td->ip - header->code);
		}

		if (sym_seq_points)
			bb_exit = td->offset_to_bb [td->ip - header->code];

		switch (*td->ip) {
		case CEE_NOP: 
			/* lose it */
			++td->ip;
			break;
		case CEE_BREAK:
			SIMPLE_OP(td, MINT_BREAK);
			break;
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3:
			load_arg (td, *td->ip - CEE_LDARG_0);
			++td->ip;
			break;
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3:
			load_local (td, *td->ip - CEE_LDLOC_0);
			++td->ip;
			break;
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3:
			store_local (td, *td->ip - CEE_STLOC_0);
			++td->ip;
			break;
		case CEE_LDARG_S:
			load_arg (td, ((guint8 *)td->ip)[1]);
			td->ip += 2;
			break;
		case CEE_LDARGA_S: {
			/* NOTE: n includes this */
			int n = ((guint8 *) td->ip) [1];
			ADD_CODE (td, MINT_LDARGA);
			ADD_CODE (td, td->rtm->arg_offsets [n]);
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_MP);
			td->ip += 2;
			break;
		}
		case CEE_STARG_S:
			store_arg (td, ((guint8 *)td->ip)[1]);
			td->ip += 2;
			break;
		case CEE_LDLOC_S:
			load_local (td, ((guint8 *)td->ip)[1]);
			td->ip += 2;
			break;
		case CEE_LDLOCA_S:
			ADD_CODE(td, MINT_LDLOCA_S);
			ADD_CODE(td, td->rtm->local_offsets [((guint8 *)td->ip)[1]]);
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_MP);
			td->ip += 2;
			break;
		case CEE_STLOC_S:
			store_local (td, ((guint8 *)td->ip)[1]);
			td->ip += 2;
			break;
		case CEE_LDNULL: 
			SIMPLE_OP(td, MINT_LDNULL);
			PUSH_TYPE(td, STACK_TYPE_O, NULL);
			break;
		case CEE_LDC_I4_M1:
			SIMPLE_OP(td, MINT_LDC_I4_M1);
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
			break;
		case CEE_LDC_I4_0:
			if (!td->is_bb_start[td->ip + 1 - td->il_code] && td->ip [1] == 0xfe && td->ip [2] == CEE_CEQ && 
				td->sp > td->stack && td->sp [-1].type == STACK_TYPE_I4) {
				SIMPLE_OP(td, MINT_CEQ0_I4);
				td->ip += 2;
			} else {
				SIMPLE_OP(td, MINT_LDC_I4_0);
				PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
			}
			break;
		case CEE_LDC_I4_1:
			if (!td->is_bb_start[td->ip + 1 - td->il_code] && 
				(td->ip [1] == CEE_ADD || td->ip [1] == CEE_SUB) && td->sp [-1].type == STACK_TYPE_I4) {
				ADD_CODE(td, td->ip [1] == CEE_ADD ? MINT_ADD1_I4 : MINT_SUB1_I4);
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
			ADD_CODE(td, MINT_LDC_I4_S);
			ADD_CODE(td, ((gint8 *) td->ip) [1]);
			td->ip += 2;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
			break;
		case CEE_LDC_I4:
			i32 = read32 (td->ip + 1);
			ADD_CODE(td, MINT_LDC_I4);
			WRITE32(td, &i32);
			td->ip += 5;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
			break;
		case CEE_LDC_I8: {
			gint64 val = read64 (td->ip + 1);
			ADD_CODE(td, MINT_LDC_I8);
			WRITE64(td, &val);
			td->ip += 9;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_I8);
			break;
		}
		case CEE_LDC_R4: {
			float val;
			readr4 (td->ip + 1, &val);
			ADD_CODE(td, MINT_LDC_R4);
			WRITE32(td, &val);
			td->ip += 5;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_R8);
			break;
		}
		case CEE_LDC_R8: {
			double val;
			readr8 (td->ip + 1, &val);
			ADD_CODE(td, MINT_LDC_R8);
			WRITE64(td, &val);
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
				ADD_CODE(td, MINT_DUP_VT);
				WRITE32(td, &size);
				td->ip ++;
			} else 
				SIMPLE_OP(td, MINT_DUP);
			PUSH_TYPE(td, type, klass);
			break;
		}
		case CEE_POP:
			CHECK_STACK(td, 1);
			SIMPLE_OP(td, MINT_POP);
			ADD_CODE (td, 0);
			if (td->sp [-1].type == STACK_TYPE_VT) {
				int size = mono_class_value_size (td->sp [-1].klass, NULL);
				size = (size + 7) & ~7;
				ADD_CODE(td, MINT_VTRESULT);
				ADD_CODE(td, 0);
				WRITE32(td, &size);
				td->vt_sp -= size;
			}
			--td->sp;
			break;
		case CEE_JMP: {
			MonoMethod *m;
			if (td->sp > td->stack)
				g_warning ("CEE_JMP: stack must be empty");
			token = read32 (td->ip + 1);
			m = mono_get_method_full (image, token, NULL, generic_context);
			ADD_CODE (td, MINT_JMP);
			ADD_CODE (td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
			return_if_nok (error);
			td->ip += 5;
			break;
		}
		case CEE_CALLVIRT: /* Fall through */
		case CEE_CALLI:    /* Fall through */
		case CEE_CALL: {
			gboolean need_seq_point = FALSE;

			if (sym_seq_points && !mono_bitset_test_fast (seq_point_locs, td->ip + 5 - header->code))
				need_seq_point = TRUE;

			interp_transform_call (td, method, NULL, domain, generic_context, is_bb_start, body_start_offset, constrained_class, readonly, error);
			return_if_nok (error);

			if (need_seq_point) {
				InterpBasicBlock *cbb = td->offset_to_bb [td->ip - header->code];
				g_assert (cbb);

				emit_seq_point (td, td->ip - header->code, cbb, TRUE);
			}

			constrained_class = NULL;
			readonly = FALSE;
			break;
		}
		case CEE_RET: {
			int vt_size = 0;
			if (signature->ret->type != MONO_TYPE_VOID) {
				--td->sp;
				MonoClass *klass = mono_class_from_mono_type (signature->ret);
				if (mint_type (&klass->byval_arg) == MINT_TYPE_VT) {
					vt_size = mono_class_value_size (klass, NULL);
					vt_size = (vt_size + 7) & ~7;
				}
			}
			if (td->sp > td->stack)
				g_warning ("%s.%s: CEE_RET: more values on stack: %d", td->method->klass->name, td->method->name, td->sp - td->stack);
			if (td->vt_sp != vt_size)
				g_error ("%s.%s: CEE_RET: value type stack: %d vs. %d", td->method->klass->name, td->method->name, td->vt_sp, vt_size);

			if (sym_seq_points) {
				InterpBasicBlock *cbb = td->offset_to_bb [td->ip - header->code];
				g_assert (cbb);
				emit_seq_point (td, METHOD_EXIT_IL_OFFSET, bb_exit, FALSE);
			}

			if (vt_size == 0)
				SIMPLE_OP(td, signature->ret->type == MONO_TYPE_VOID ? MINT_RET_VOID : MINT_RET);
			else {
				ADD_CODE(td, MINT_RET_VT);
				WRITE32(td, &vt_size);
				++td->ip;
			}
			generating_code = 0;
			break;
		}
		case CEE_BR:
			handle_branch (td, MINT_BR_S, MINT_BR, 5 + read32 (td->ip + 1));
			td->ip += 5;
			generating_code = 0;
			break;
		case CEE_BR_S:
			handle_branch (td, MINT_BR_S, MINT_BR, 2 + (gint8)td->ip [1]);
			td->ip += 2;
			generating_code = 0;
			break;
		case CEE_BRFALSE:
			one_arg_branch (td, MINT_BRFALSE_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BRFALSE_S:
			one_arg_branch (td, MINT_BRFALSE_I4, 2 + (gint8)td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BRTRUE:
			one_arg_branch (td, MINT_BRTRUE_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BRTRUE_S:
			one_arg_branch (td, MINT_BRTRUE_I4, 2 + (gint8)td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BEQ:
			two_arg_branch (td, MINT_BEQ_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BEQ_S:
			two_arg_branch (td, MINT_BEQ_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BGE:
			two_arg_branch (td, MINT_BGE_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BGE_S:
			two_arg_branch (td, MINT_BGE_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BGT:
			two_arg_branch (td, MINT_BGT_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BGT_S:
			two_arg_branch (td, MINT_BGT_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BLT:
			two_arg_branch (td, MINT_BLT_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BLT_S:
			two_arg_branch (td, MINT_BLT_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BLE:
			two_arg_branch (td, MINT_BLE_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BLE_S:
			two_arg_branch (td, MINT_BLE_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BNE_UN:
			two_arg_branch (td, MINT_BNE_UN_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BNE_UN_S:
			two_arg_branch (td, MINT_BNE_UN_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BGE_UN:
			two_arg_branch (td, MINT_BGE_UN_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BGE_UN_S:
			two_arg_branch (td, MINT_BGE_UN_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BGT_UN:
			two_arg_branch (td, MINT_BGT_UN_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BGT_UN_S:
			two_arg_branch (td, MINT_BGT_UN_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BLE_UN:
			two_arg_branch (td, MINT_BLE_UN_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BLE_UN_S:
			two_arg_branch (td, MINT_BLE_UN_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_BLT_UN:
			two_arg_branch (td, MINT_BLT_UN_I4, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BLT_UN_S:
			two_arg_branch (td, MINT_BLT_UN_I4, 2 + (gint8) td->ip [1]);
			td->ip += 2;
			break;
		case CEE_SWITCH: {
			guint32 n;
			const unsigned char *next_ip;
			++td->ip;
			n = read32 (td->ip);
			ADD_CODE (td, MINT_SWITCH);
			WRITE32 (td, &n);
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
					target = td->in_offsets [target] - (td->new_ip - td->new_code);
				} else {
					td->stack_height [target] = stack_height;
					td->vt_stack_size [target] = td->vt_sp;
					if (stack_height > 0)
						td->stack_state [target] = g_memdup (td->stack, stack_height * sizeof (td->stack [0]));

					Reloc *reloc = mono_mempool_alloc0 (td->mempool, sizeof (Reloc));
					reloc->type = RELOC_SWITCH;
					reloc->offset = td->new_ip - td->new_code;
					reloc->target = target;
					g_ptr_array_add (td->relocs, reloc);
					target = 0xffff;
				}
				WRITE32 (td, &target);
				td->ip += 4;
			}
			break;
		}
		case CEE_LDIND_I1:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I1);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_U1:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_U1);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_I2:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I2);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_U2:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_U2);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_I4:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I4);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_U4:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_U4);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_I8:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I8);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_I:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I);
			ADD_CODE (td, 0);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_R4:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_R4);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_R8:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_R8);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_REF:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_REF);
			BARRIER_IF_VOLATILE (td);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_O);
			break;
		case CEE_STIND_REF:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td);
			SIMPLE_OP (td, MINT_STIND_REF);
			td->sp -= 2;
			break;
		case CEE_STIND_I1:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td);
			SIMPLE_OP (td, MINT_STIND_I1);
			td->sp -= 2;
			break;
		case CEE_STIND_I2:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td);
			SIMPLE_OP (td, MINT_STIND_I2);
			td->sp -= 2;
			break;
		case CEE_STIND_I4:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td);
			SIMPLE_OP (td, MINT_STIND_I4);
			td->sp -= 2;
			break;
		case CEE_STIND_I:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td);
			SIMPLE_OP (td, MINT_STIND_I);
			td->sp -= 2;
			break;
		case CEE_STIND_I8:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td);
			SIMPLE_OP (td, MINT_STIND_I8);
			td->sp -= 2;
			break;
		case CEE_STIND_R4:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td);
			SIMPLE_OP (td, MINT_STIND_R4);
			td->sp -= 2;
			break;
		case CEE_STIND_R8:
			CHECK_STACK (td, 2);
			BARRIER_IF_VOLATILE (td);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_U1_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_U1_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_U1_I8);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_I1_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_I1_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_I1_I8);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_U2_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_U2_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_U2_I8);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_I2_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_I2_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_I2_I8);
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
				ADD_CODE(td, MINT_CONV_U4_R8);
#else
				ADD_CODE(td, MINT_CONV_U8_R8);
#endif
				break;
			case STACK_TYPE_I4:
#if SIZEOF_VOID_P == 8
				ADD_CODE(td, MINT_CONV_U8_I4);
#endif
				break;
			case STACK_TYPE_I8:
#if SIZEOF_VOID_P == 4
				ADD_CODE(td, MINT_CONV_U4_I8);
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
				ADD_CODE(td, MINT_CONV_I8_R8);
#else
				ADD_CODE(td, MINT_CONV_I4_R8);
#endif
				break;
			case STACK_TYPE_I4:
#if SIZEOF_VOID_P == 8
				ADD_CODE(td, MINT_CONV_I8_I4);
#endif
				break;
			case STACK_TYPE_O:
				break;
			case STACK_TYPE_MP:
				break;
			case STACK_TYPE_I8:
#if SIZEOF_VOID_P == 4
				ADD_CODE(td, MINT_CONV_I4_I8);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_U4_R8);
				break;
			case STACK_TYPE_I4:
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_U4_I8);
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 8
				ADD_CODE(td, MINT_CONV_U4_I8);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_I4_R8);
				break;
			case STACK_TYPE_I4:
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_I4_I8);
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 8
				ADD_CODE(td, MINT_CONV_I4_I8);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_I8_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_I8_I4);
				break;
			case STACK_TYPE_I8:
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 4
				ADD_CODE(td, MINT_CONV_I8_I4);
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
				ADD_CODE(td, MINT_CONV_R4_R8);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_R4_I8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_R4_I4);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
			break;
		case CEE_CONV_R8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_R8_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_R8_I8);
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
				ADD_CODE(td, MINT_CONV_U8_I4);
				break;
			case STACK_TYPE_I8:
				break;
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_U8_R8);
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 4
				ADD_CODE(td, MINT_CONV_U8_I4);
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
			klass = mono_class_get_full (image, token, generic_context);

			if (klass->valuetype) {
				ADD_CODE (td, MINT_CPOBJ);
				ADD_CODE (td, get_data_item_index(td, klass));
			} else {
				ADD_CODE (td, MINT_LDIND_REF);
				ADD_CODE (td, MINT_STIND_REF);
			}
			td->ip += 5;
			td->sp -= 2;
			break;
		}
		case CEE_LDOBJ: {
			int size;
			CHECK_STACK (td, 1);

			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get_full (image, token, generic_context);

			MonoClass *tos_klass = td->sp [-1].klass;
			if (tos_klass && td->sp [-1].type == STACK_TYPE_VT) {
				int tos_size = mono_class_value_size (tos_klass, NULL);
				POP_VT (td, tos_size);
			}

			ADD_CODE(td, MINT_LDOBJ);
			ADD_CODE(td, get_data_item_index(td, klass));
			int mt = mint_type (&klass->byval_arg);
			if (mt == MINT_TYPE_VT) {
				size = mono_class_value_size (klass, NULL);
				PUSH_VT (td, size);
			}
			td->ip += 5;
			SET_TYPE (td->sp - 1, stack_type [mt], klass);
			BARRIER_IF_VOLATILE (td);
			break;
		}
		case CEE_LDSTR: {
			MonoString *s;
			token = mono_metadata_token_index (read32 (td->ip + 1));
			td->ip += 5;
			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD) {
				s = mono_method_get_wrapper_data (method, token);
			} else if (method->wrapper_type != MONO_WRAPPER_NONE) {
				s = mono_string_new_wrapper (mono_method_get_wrapper_data (method, token));
			} else {
				s = mono_ldstr (domain, image, token);
			}
			ADD_CODE(td, MINT_LDSTR);
			ADD_CODE(td, get_data_item_index (td, s));
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

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				m = (MonoMethod *)mono_method_get_wrapper_data (method, token);
			else 
				m = mono_get_method_full (image, token, NULL, generic_context);

			csignature = mono_method_signature (m);
			klass = m->klass;

			td->sp -= csignature->param_count;
			if (mono_class_is_magic_int (klass) || mono_class_is_magic_float (klass)) {
				ADD_CODE (td, MINT_NEWOBJ_MAGIC);
				ADD_CODE (td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
				return_if_nok (error);

				PUSH_TYPE (td, stack_type [mint_type (&klass->byval_arg)], klass);
			} else {
				ADD_CODE(td, MINT_NEWOBJ);
				ADD_CODE(td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
				return_if_nok (error);

				if (mint_type (&klass->byval_arg) == MINT_TYPE_VT) {
					vt_res_size = mono_class_value_size (klass, NULL);
					PUSH_VT (td, vt_res_size);
				}
				for (i = 0; i < csignature->param_count; ++i) {
					int mt = mint_type(csignature->params [i]);
					if (mt == MINT_TYPE_VT) {
						MonoClass *k = mono_class_from_mono_type (csignature->params [i]);
						gint32 size = mono_class_value_size (k, NULL);
						size = (size + 7) & ~7;
						vt_stack_used += size;
					}
				}
				if (vt_stack_used != 0 || vt_res_size != 0) {
					ADD_CODE(td, MINT_VTRESULT);
					ADD_CODE(td, vt_res_size);
					WRITE32(td, &vt_stack_used);
					td->vt_sp -= vt_stack_used;
				}
				PUSH_TYPE (td, stack_type [mint_type (&klass->byval_arg)], klass);
			}
			break;
		}
		case CEE_CASTCLASS:
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			ADD_CODE(td, MINT_CASTCLASS);
			ADD_CODE(td, get_data_item_index (td, klass));
			td->sp [-1].klass = klass;
			td->ip += 5;
			break;
		case CEE_ISINST:
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			ADD_CODE(td, MINT_ISINST);
			ADD_CODE(td, get_data_item_index (td, klass));
			td->ip += 5;
			break;
		case CEE_CONV_R_UN:
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_R_UN_I8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_R_UN_I4);
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
			else 
				klass = mono_class_get_full (image, token, generic_context);

			if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method = mono_class_get_method_from_name (klass, "Unbox", 1);
				/* td->ip is incremented by interp_transform_call */
				interp_transform_call (td, method, target_method, domain, generic_context, is_bb_start, body_start_offset, NULL, FALSE, error);

				return_if_nok (error);
			} else {
				ADD_CODE (td, MINT_UNBOX);
				ADD_CODE (td, get_data_item_index (td, klass));
				SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_MP);
				td->ip += 5;
			}
			break;
		case CEE_UNBOX_ANY:
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);

			klass = mini_get_class (method, token, generic_context);

			if (mini_type_is_reference (&klass->byval_arg)) {
				int mt = mint_type (&klass->byval_arg);
				ADD_CODE (td, MINT_CASTCLASS);
				ADD_CODE (td, get_data_item_index (td, klass));
				SET_TYPE (td->sp - 1, stack_type [mt], klass);
				td->ip += 5;
			} else if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method = mono_class_get_method_from_name (klass, "Unbox", 1);
				/* td->ip is incremented by interp_transform_call */
				interp_transform_call (td, method, target_method, domain, generic_context, is_bb_start, body_start_offset, NULL, FALSE, error);

				return_if_nok (error);
			} else {
				int mt = mint_type (&klass->byval_arg);
				ADD_CODE (td, MINT_UNBOX);
				ADD_CODE (td, get_data_item_index (td, klass));

				ADD_CODE (td, MINT_LDOBJ);
				ADD_CODE (td, get_data_item_index(td, klass));
				SET_TYPE (td->sp - 1, stack_type [mt], klass);

				if (mt == MINT_TYPE_VT) {
					int size = mono_class_value_size (klass, NULL);
					PUSH_VT (td, size);
				}
				td->ip += 5;
			}

			break;
		case CEE_THROW:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_THROW);
			--td->sp;
			generating_code = 0;
			break;
		case CEE_LDFLDA: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			return_if_nok (error);
			MonoType *ftype = mono_field_get_type (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			mono_class_init (klass);
			if (is_static) {
				ADD_CODE (td, MINT_POP);
				ADD_CODE (td, 0);
				ADD_CODE (td, MINT_LDSFLDA);
				ADD_CODE (td, get_data_item_index (td, field));
			} else {
				if ((td->sp - 1)->type == STACK_TYPE_O) {
					ADD_CODE (td, MINT_LDFLDA);
				} else {
					g_assert ((td->sp -1)->type == STACK_TYPE_MP);
					ADD_CODE (td, MINT_LDFLDA_UNSAFE);
				}
				ADD_CODE (td, klass->valuetype ? field->offset - sizeof (MonoObject) : field->offset);
			}
			td->ip += 5;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);
			break;
		}
		case CEE_LDFLD: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			return_if_nok (error);
			MonoType *ftype = mono_field_get_type (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			mono_class_init (klass);

			MonoClass *field_klass = mono_class_from_mono_type (ftype);
			mt = mint_type (&field_klass->byval_arg);
#ifndef DISABLE_REMOTING
			if (klass->marshalbyref) {
				g_assert (!is_static);
				ADD_CODE(td, mt == MINT_TYPE_VT ? MINT_LDRMFLD_VT :  MINT_LDRMFLD);
				ADD_CODE(td, get_data_item_index (td, field));
			} else
#endif
			{
				if (is_static) {
					ADD_CODE (td, MINT_POP);
					ADD_CODE (td, 0);
					ADD_CODE (td, mt == MINT_TYPE_VT ? MINT_LDSFLD_VT : MINT_LDSFLD);
					ADD_CODE (td, get_data_item_index (td, field));
				} else {
					ADD_CODE (td, MINT_LDFLD_I1 + mt - MINT_TYPE_I1);
					ADD_CODE (td, klass->valuetype ? field->offset - sizeof(MonoObject) : field->offset);
					if (mt == MINT_TYPE_VT)
						ADD_CODE (td, get_data_item_index (td, field));
				}
			}
			if (mt == MINT_TYPE_VT) {
				int size = mono_class_value_size (field_klass, NULL);
				PUSH_VT (td, size);
			}
			if (td->sp [-1].type == STACK_TYPE_VT) {
				int size = mono_class_value_size (klass, NULL);
				size = (size + 7) & ~7;
				td->vt_sp -= size;
				ADD_CODE (td, MINT_VTRESULT);
				ADD_CODE (td, 0);
				WRITE32 (td, &size);
			}
			td->ip += 5;
			SET_TYPE (td->sp - 1, stack_type [mt], field_klass);
			BARRIER_IF_VOLATILE (td);
			break;
		}
		case CEE_STFLD: {
			CHECK_STACK (td, 2);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			return_if_nok (error);
			MonoType *ftype = mono_field_get_type (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			mono_class_init (klass);
			mt = mint_type (ftype);

			BARRIER_IF_VOLATILE (td);

#ifndef DISABLE_REMOTING
			if (klass->marshalbyref) {
				g_assert (!is_static);
				ADD_CODE(td, mt == MINT_TYPE_VT ? MINT_STRMFLD_VT : MINT_STRMFLD);
				ADD_CODE(td, get_data_item_index (td, field));
			} else
#endif
			{
				if (is_static) {
					ADD_CODE (td, MINT_POP);
					ADD_CODE (td, 1);
					ADD_CODE (td, mt == MINT_TYPE_VT ? MINT_STSFLD_VT : MINT_STSFLD);
					ADD_CODE (td, get_data_item_index (td, field));
				} else {
					ADD_CODE (td, MINT_STFLD_I1 + mt - MINT_TYPE_I1);
					ADD_CODE (td, klass->valuetype ? field->offset - sizeof(MonoObject) : field->offset);
					if (mt == MINT_TYPE_VT)
						ADD_CODE (td, get_data_item_index (td, field));
				}
			}
			if (mt == MINT_TYPE_VT) {
				MonoClass *klass = mono_class_from_mono_type (ftype);
				int size = mono_class_value_size (klass, NULL);
				POP_VT (td, size);
			}
			td->ip += 5;
			td->sp -= 2;
			break;
		}
		case CEE_LDSFLDA: {
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			return_if_nok (error);
			mono_field_get_type (field);
			ADD_CODE(td, MINT_LDSFLDA);
			ADD_CODE(td, get_data_item_index (td, field));
			td->ip += 5;
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_MP);
			break;
		}
		case CEE_LDSFLD: {
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			return_if_nok (error);
			MonoType *ftype = mono_field_get_type (field);
			mt = mint_type (ftype);
			ADD_CODE(td, mt == MINT_TYPE_VT ? MINT_LDSFLD_VT : MINT_LDSFLD);
			ADD_CODE(td, get_data_item_index (td, field));
			klass = NULL;
			if (mt == MINT_TYPE_VT) {
				MonoClass *klass = mono_class_from_mono_type (ftype);
				int size = mono_class_value_size (klass, NULL);
				PUSH_VT(td, size);
				WRITE32(td, &size);
				klass = ftype->data.klass;
			} else {
				if (mt == MINT_TYPE_O) 
					klass = mono_class_from_mono_type (ftype);
			}
			td->ip += 5;
			PUSH_TYPE(td, stack_type [mt], klass);
			break;
		}
		case CEE_STSFLD:
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			return_if_nok (error);
			MonoType *ftype = mono_field_get_type (field);
			mt = mint_type (ftype);
			ADD_CODE(td, mt == MINT_TYPE_VT ? MINT_STSFLD_VT : MINT_STSFLD);
			ADD_CODE(td, get_data_item_index (td, field));
			if (mt == MINT_TYPE_VT) {
				MonoClass *klass = mono_class_from_mono_type (ftype);
				int size = mono_class_value_size (klass, NULL);
				POP_VT (td, size);
			}
			td->ip += 5;
			--td->sp;
			break;
		case CEE_STOBJ: {
			int size;
			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);

			BARRIER_IF_VOLATILE (td);
			ADD_CODE(td, td->sp [-1].type == STACK_TYPE_VT ? MINT_STOBJ_VT : MINT_STOBJ);
			ADD_CODE(td, get_data_item_index (td, klass));
			if (td->sp [-1].type == STACK_TYPE_VT) {
				size = mono_class_value_size (klass, NULL);
				POP_VT (td, size);
			}
			td->ip += 5;
			td->sp -= 2;
			break;
		}
		case CEE_CONV_OVF_I_UN:
		case CEE_CONV_OVF_U_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
#if SIZEOF_VOID_P == 8
				ADD_CODE(td, MINT_CONV_OVF_I8_UN_R8);
#else
				ADD_CODE(td, MINT_CONV_OVF_I4_UN_R8);
#endif
				break;
			case STACK_TYPE_I8:
#if SIZEOF_VOID_P == 4
				ADD_CODE (td, MINT_CONV_OVF_I4_UN_I8);
#endif
				break;
			case STACK_TYPE_I4:
#if SIZEOF_VOID_P == 8
				ADD_CODE(td, MINT_CONV_I8_U4);
#elif SIZEOF_VOID_P == 4
				if (*td->ip == CEE_CONV_OVF_I_UN)
					ADD_CODE(td, MINT_CONV_OVF_I4_U4);
#endif
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
			++td->ip;
			break;
		case CEE_CONV_OVF_I8_UN:
		case CEE_CONV_OVF_U8_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_OVF_I8_UN_R8);
				break;
			case STACK_TYPE_I8:
				if (*td->ip == CEE_CONV_OVF_I8_UN)
					ADD_CODE (td, MINT_CONV_OVF_I8_U8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_I8_U4);
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

			if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method = mono_class_get_method_from_name (klass, "Box", 1);
				/* td->ip is incremented by interp_transform_call */
				interp_transform_call (td, method, target_method, domain, generic_context, is_bb_start, body_start_offset, NULL, FALSE, error);
				return_if_nok (error);
			} else if (!klass->valuetype) {
				/* already boxed, do nothing. */
				td->ip += 5;
			} else {
				if (mint_type (&klass->byval_arg) == MINT_TYPE_VT && !klass->enumtype) {
					size = mono_class_value_size (klass, NULL);
					size = (size + 7) & ~7;
					td->vt_sp -= size;
				}
				ADD_CODE(td, MINT_BOX);
				ADD_CODE(td, get_data_item_index (td, klass));
				ADD_CODE (td, 0);
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

			unsigned char lentype = (td->sp - 1)->type;
			if (lentype == STACK_TYPE_I8) {
				/* mimic mini behaviour */
				ADD_CODE (td, MINT_CONV_OVF_U4_I8);
			} else {
				g_assert (lentype == STACK_TYPE_I4);
				ADD_CODE (td, MINT_CONV_OVF_U4_I4);
			}
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
			ADD_CODE (td, MINT_NEWARR);
			ADD_CODE (td, get_data_item_index (td, klass));
			SET_TYPE (td->sp - 1, STACK_TYPE_O, klass);
			td->ip += 5;
			break;
		}
		case CEE_LDLEN:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDLEN);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I);
			break;
		case CEE_LDELEMA:
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *) mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);

			if (!klass->valuetype && method->wrapper_type == MONO_WRAPPER_NONE && !readonly) {
				ADD_CODE (td, MINT_LDELEMA_TC);
			} else {
				ADD_CODE (td, MINT_LDELEMA);
			}
			ADD_CODE (td, get_data_item_index (td, klass));
			/* according to spec, ldelema bytecode is only used for 1-dim arrays */
			ADD_CODE (td, 2);
			readonly = FALSE;

			td->ip += 5;
			--td->sp;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);
			break;
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
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
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
			switch (mint_type (&klass->byval_arg)) {
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
					SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
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
					ADD_CODE (td, get_data_item_index (td, klass));
					WRITE32 (td, &size);
					--td->sp;
					SET_TYPE (td->sp - 1, STACK_TYPE_VT, klass);
					PUSH_VT (td, size);
					break;
				}
				default: {
					GString *res = g_string_new ("");
					mono_type_get_desc (res, &klass->byval_arg, TRUE);
					g_print ("LDELEM: %s -> %d (%s)\n", klass->name, mint_type (&klass->byval_arg), res->str);
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
			switch (mint_type (&klass->byval_arg)) {
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
				case MINT_TYPE_R8:
					SIMPLE_OP (td, MINT_STELEM_R8);
					break;
				case MINT_TYPE_O:
					SIMPLE_OP (td, MINT_STELEM_REF);
					break;
				case MINT_TYPE_VT: {
					int size = mono_class_value_size (klass, NULL);
					SIMPLE_OP (td, MINT_STELEM_VT);
					ADD_CODE (td, get_data_item_index (td, klass));
					WRITE32 (td, &size);
					POP_VT (td, size);
					break;
				}
				default: {
					GString *res = g_string_new ("");
					mono_type_get_desc (res, &klass->byval_arg, TRUE);
					g_print ("STELEM: %s -> %d (%s)\n", klass->name, mint_type (&klass->byval_arg), res->str);
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

			ADD_CODE (td, MINT_MKREFANY);
			ADD_CODE (td, get_data_item_index (td, klass));

			td->ip += 5;
			PUSH_VT (td, sizeof (MonoTypedRef));
			SET_TYPE(td->sp - 1, STACK_TYPE_VT, mono_defaults.typed_reference_class);
			break;
		case CEE_REFANYVAL: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);

			ADD_CODE (td, MINT_REFANYVAL);

			POP_VT (td, sizeof (MonoTypedRef));
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);

			td->ip += 5;
			break;
		}
		case CEE_CONV_OVF_I1:
		case CEE_CONV_OVF_I1_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_OVF_I1_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_OVF_I1_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_OVF_I1_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_CONV_OVF_U1:
		case CEE_CONV_OVF_U1_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_OVF_U1_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_OVF_U1_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_OVF_U1_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_CONV_OVF_I2:
		case CEE_CONV_OVF_I2_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_OVF_I2_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_OVF_I2_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_OVF_I2_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			break;
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_U2:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_OVF_U2_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_OVF_U2_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_OVF_U2_I8);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_OVF_I4_R8);
				break;
			case STACK_TYPE_I4:
				if (*td->ip == CEE_CONV_OVF_I4_UN)
					ADD_CODE(td, MINT_CONV_OVF_I4_U4);
				break;
			case STACK_TYPE_I8:
				if (*td->ip == CEE_CONV_OVF_I4_UN)
					ADD_CODE (td, MINT_CONV_OVF_I4_U8);
				else
					ADD_CODE (td, MINT_CONV_OVF_I4_I8);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_OVF_U4_R8);
				break;
			case STACK_TYPE_I4:
				if (*td->ip != CEE_CONV_OVF_U4_UN)
					ADD_CODE(td, MINT_CONV_OVF_U4_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_OVF_U4_I8);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_OVF_I8_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_I8_I4);
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
			case STACK_TYPE_R8:
				ADD_CODE(td, MINT_CONV_OVF_U8_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, MINT_CONV_OVF_U8_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE (td, MINT_CONV_OVF_U8_I8);
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
					handle = &((MonoClass *) handle)->byval_arg;

				if (generic_context) {
					handle = mono_class_inflate_generic_type_checked (handle, generic_context, error);
					return_if_nok (error);
				}
			} else {
				handle = mono_ldtoken_checked (image, token, &klass, generic_context, error);
				return_if_nok (error);
			}
			mono_class_init (klass);
			mt = mint_type (&klass->byval_arg);
			g_assert (mt == MINT_TYPE_VT);
			size = mono_class_value_size (klass, NULL);
			g_assert (size == sizeof(gpointer));
			PUSH_VT (td, sizeof(gpointer));
			ADD_CODE (td, MINT_LDTOKEN);
			ADD_CODE (td, get_data_item_index (td, handle));

			SET_TYPE (td->sp, stack_type [mt], klass);
			td->sp++;
			td->ip += 5;
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
		case CEE_ENDFINALLY:
			g_assert (td->clause_indexes [in_offset] != -1);
			td->sp = td->stack;
			SIMPLE_OP (td, MINT_ENDFINALLY);
			ADD_CODE (td, td->clause_indexes [in_offset]);
			generating_code = 0;
			break;
		case CEE_LEAVE:
			td->sp = td->stack;
			if (td->clause_indexes [in_offset] != -1) {
				/* LEAVE instructions in catch clauses need to check for abort exceptions */
				handle_branch (td, MINT_LEAVE_S_CHECK, MINT_LEAVE_CHECK, 5 + read32 (td->ip + 1));
			} else {
				handle_branch (td, MINT_LEAVE_S, MINT_LEAVE, 5 + read32 (td->ip + 1));
			}
			td->ip += 5;
			generating_code = 0;
			break;
		case CEE_LEAVE_S:
			td->sp = td->stack;
			if (td->clause_indexes [in_offset] != -1) {
				/* LEAVE instructions in catch clauses need to check for abort exceptions */
				handle_branch (td, MINT_LEAVE_S_CHECK, MINT_LEAVE_CHECK, 2 + (gint8)td->ip [1]);
			} else {
				handle_branch (td, MINT_LEAVE_S, MINT_LEAVE, 2 + (gint8)td->ip [1]);
			}
			td->ip += 2;
			generating_code = 0;
			break;
		case CEE_UNUSED41:
			++td->ip;
		        switch (*td->ip) {
				case CEE_MONO_CALLI_EXTRA_ARG:
					/* Same as CEE_CALLI, except that we drop the extra arg required for llvm specific behaviour */
					ADD_CODE (td, MINT_POP);
					ADD_CODE (td, 1);
					--td->sp;
					interp_transform_call (td, method, NULL, domain, generic_context, is_bb_start, body_start_offset, NULL, FALSE, error);
					return_if_nok (error);
					break;
				case CEE_MONO_JIT_ICALL_ADDR: {
					guint32 token;
					gpointer func;
					MonoJitICallInfo *info;

					token = read32 (td->ip + 1);
					td->ip += 5;
					func = mono_method_get_wrapper_data (method, token);
					info = mono_find_jit_icall_by_addr (func);

					ADD_CODE (td, MINT_LDFTN);
					ADD_CODE (td, get_data_item_index (td, func));
					PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
					break;
				}
				case CEE_MONO_ICALL: {
					guint32 token;
					gpointer func;
					MonoJitICallInfo *info;

					token = read32 (td->ip + 1);
					td->ip += 5;
					func = mono_method_get_wrapper_data (method, token);
					info = mono_find_jit_icall_by_addr (func);
					g_assert (info);

					CHECK_STACK (td, info->sig->param_count);
					switch (info->sig->param_count) {
					case 0:
						if (MONO_TYPE_IS_VOID (info->sig->ret))
							ADD_CODE (td,MINT_ICALL_V_V);
						else
							ADD_CODE (td, MINT_ICALL_V_P);
						break;
					case 1:
						if (MONO_TYPE_IS_VOID (info->sig->ret))
							ADD_CODE (td,MINT_ICALL_P_V);
						else
							ADD_CODE (td,MINT_ICALL_P_P);
						break;
					case 2:
						if (MONO_TYPE_IS_VOID (info->sig->ret)) {
							if (info->sig->params [1]->type == MONO_TYPE_I4)
								ADD_CODE (td,MINT_ICALL_PI_V);
							else
								ADD_CODE (td,MINT_ICALL_PP_V);
						} else {
							if (info->sig->params [1]->type == MONO_TYPE_I4)
								ADD_CODE (td,MINT_ICALL_PI_P);
							else
								ADD_CODE (td,MINT_ICALL_PP_P);
						}
						break;
					case 3:
						g_assert (MONO_TYPE_IS_VOID (info->sig->ret));
						if (info->sig->params [2]->type == MONO_TYPE_I4)
							ADD_CODE (td,MINT_ICALL_PPI_V);
						else
							ADD_CODE (td,MINT_ICALL_PPP_V);
						break;
					default:
						g_assert_not_reached ();
					}

					if (func == mono_ftnptr_to_delegate) {
						g_error ("TODO: CEE_MONO_ICALL mono_ftnptr_to_delegate?");
					}
					ADD_CODE(td, get_data_item_index (td, func));
					td->sp -= info->sig->param_count;

					if (!MONO_TYPE_IS_VOID (info->sig->ret)) {
						int mt = mint_type (info->sig->ret);
						td->sp ++;
						SET_SIMPLE_TYPE(td->sp - 1, stack_type [mt]);
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
				size = (size + 7) & ~7;
				ADD_CODE(td, MINT_VTRESULT);
				ADD_CODE(td, 0);
				WRITE32(td, &size);
				td->vt_sp -= size;
				++td->ip;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);
				break;
			}
			case CEE_MONO_LDPTR:
			case CEE_MONO_CLASSCONST:
				token = read32 (td->ip + 1);
				td->ip += 5;
				ADD_CODE(td, MINT_MONO_LDPTR);
				ADD_CODE(td, get_data_item_index (td, mono_method_get_wrapper_data (method, token)));
				td->sp [0].type = STACK_TYPE_I;
				++td->sp;
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
				ADD_CODE(td, MINT_MONO_NEWOBJ);
				ADD_CODE(td, get_data_item_index (td, mono_method_get_wrapper_data (method, token)));
				td->sp [0].type = STACK_TYPE_O;
				++td->sp;
				break;
			case CEE_MONO_RETOBJ:
				CHECK_STACK (td, 1);
				token = read32 (td->ip + 1);
				td->ip += 5;
				ADD_CODE(td, MINT_MONO_RETOBJ);
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
				g_assert(klass->valuetype);
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);
				break;
			case CEE_MONO_TLS: {
				gint32 key = read32 (td->ip + 1);
				td->ip += 5;
				g_assert (key < TLS_KEY_NUM);
				ADD_CODE (td, MINT_MONO_TLS);
				WRITE32 (td, &key);
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
				ADD_CODE (td, MINT_MONO_LDPTR);
				ADD_CODE (td, get_data_item_index (td, mono_thread_interruption_request_flag ()));
				PUSH_TYPE (td, STACK_TYPE_MP, NULL);
				++td->ip;
				break;
			case CEE_MONO_MEMORY_BARRIER:
				ADD_CODE (td, MINT_MONO_MEMORY_BARRIER);
				++td->ip;
				break;
			case CEE_MONO_JIT_ATTACH:
				ADD_CODE (td, MINT_MONO_JIT_ATTACH);
				++td->ip;
				break;
			case CEE_MONO_JIT_DETACH:
				ADD_CODE (td, MINT_MONO_JIT_DETACH);
				++td->ip;
				break;
			case CEE_MONO_LDDOMAIN:
				ADD_CODE (td, MINT_MONO_LDDOMAIN);
				td->sp [0].type = STACK_TYPE_I;
				++td->sp;
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
#if 0
			case CEE_ARGLIST: ves_abort(); break;
#endif
			case CEE_CEQ:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					ADD_CODE(td, MINT_CEQ_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					ADD_CODE(td, MINT_CEQ_I4 + td->sp [-1].type - STACK_TYPE_I4);
				--td->sp;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
				++td->ip;
				break;
			case CEE_CGT:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					ADD_CODE(td, MINT_CGT_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					ADD_CODE(td, MINT_CGT_I4 + td->sp [-1].type - STACK_TYPE_I4);
				--td->sp;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
				++td->ip;
				break;
			case CEE_CGT_UN:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					ADD_CODE(td, MINT_CGT_UN_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					ADD_CODE(td, MINT_CGT_UN_I4 + td->sp [-1].type - STACK_TYPE_I4);
				--td->sp;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
				++td->ip;
				break;
			case CEE_CLT:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					ADD_CODE(td, MINT_CLT_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					ADD_CODE(td, MINT_CLT_I4 + td->sp [-1].type - STACK_TYPE_I4);
				--td->sp;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
				++td->ip;
				break;
			case CEE_CLT_UN:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					ADD_CODE(td, MINT_CLT_UN_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					ADD_CODE(td, MINT_CLT_UN_I4 + td->sp [-1].type - STACK_TYPE_I4);
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
				if (method->wrapper_type != MONO_WRAPPER_NONE)
					m = (MonoMethod *)mono_method_get_wrapper_data (method, token);
				else 
					m = mono_get_method_full (image, token, NULL, generic_context);

				if (method->wrapper_type == MONO_WRAPPER_NONE && m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
					m = mono_marshal_get_synchronized_wrapper (m);

				ADD_CODE(td, *td->ip == CEE_LDFTN ? MINT_LDFTN : MINT_LDVIRTFTN);
				ADD_CODE(td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
				return_if_nok (error);
				td->ip += 5;
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_F);
				break;
			}
			case CEE_LDARG:
				load_arg (td, read16 (td->ip + 1));
				td->ip += 3;
				break;
			case CEE_LDARGA: {
				int n = read16 (td->ip + 1);
				ADD_CODE (td, MINT_LDARGA);
				ADD_CODE (td, td->rtm->arg_offsets [n]); /* FIX for large offsets */
				PUSH_SIMPLE_TYPE(td, STACK_TYPE_MP);
				td->ip += 3;
				break;
			}
			case CEE_STARG:
				store_arg (td, read16 (td->ip + 1));
				td->ip += 3;
				break;
			case CEE_LDLOC:
				load_local (td, read16 (td->ip + 1));
				td->ip += 3;
				break;
			case CEE_LDLOCA:
				ADD_CODE(td, MINT_LDLOCA_S);
				ADD_CODE(td, td->rtm->local_offsets [read16 (td->ip + 1)]);
				PUSH_SIMPLE_TYPE(td, STACK_TYPE_MP);
				td->ip += 3;
				break;
			case CEE_STLOC:
				store_local (td, read16 (td->ip + 1));
				td->ip += 3;
				break;
			case CEE_LOCALLOC:
				CHECK_STACK (td, 1);
#if SIZEOF_VOID_P == 8
				if (td->sp [-1].type == STACK_TYPE_I8)
					ADD_CODE(td, MINT_CONV_I4_I8);
#endif				
				ADD_CODE(td, MINT_LOCALLOC);
				if (td->sp != td->stack + 1)
					g_warning("CEE_LOCALLOC: stack not empty");
				++td->ip;
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_MP);
				break;
#if 0
			case CEE_UNUSED57: ves_abort(); break;
#endif
			case CEE_ENDFILTER:
				ADD_CODE (td, MINT_ENDFILTER);
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
				if (klass->valuetype) {
					ADD_CODE (td, MINT_INITOBJ);
					i32 = mono_class_value_size (klass, NULL);
					WRITE32 (td, &i32);
				} else {
					ADD_CODE (td, MINT_LDNULL);
					ADD_CODE (td, MINT_STIND_REF);
				}
				td->ip += 5;
				--td->sp;
				break;
			case CEE_CPBLK:
				CHECK_STACK(td, 3);
				/* FIX? convert length to I8? */
				if (volatile_)
					ADD_CODE (td, MINT_MONO_MEMORY_BARRIER);
				ADD_CODE(td, MINT_CPBLK);
				BARRIER_IF_VOLATILE (td);
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
				mono_class_init (constrained_class);
				td->ip += 5;
				break;
			case CEE_INITBLK:
				CHECK_STACK(td, 3);
				BARRIER_IF_VOLATILE (td);
				ADD_CODE(td, MINT_INITBLK);
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
				ADD_CODE (td, rtm->exvar_offsets [clause_index]);
				generating_code = 0;
				break;
			}
			case CEE_SIZEOF: {
				gint32 size;
				token = read32 (td->ip + 1);
				td->ip += 5;
				if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC && !image_is_dynamic (method->klass->image) && !generic_context) {
					int align;
					MonoType *type = mono_type_create_from_typespec (image, token);
					size = mono_type_size (type, &align);
				} else {
					int align;
					MonoClass *szclass = mini_get_class (method, token, generic_context);
					mono_class_init (szclass);
#if 0
					if (!szclass->valuetype)
						THROW_EX (mono_exception_from_name (mono_defaults.corlib, "System", "InvalidProgramException"), ip - 5);
#endif
					size = mono_type_size (&szclass->byval_arg, &align);
				} 
				ADD_CODE(td, MINT_LDC_I4);
				WRITE32(td, &size);
				PUSH_SIMPLE_TYPE(td, STACK_TYPE_I4);
				break;
			}
			case CEE_REFANYTYPE:
				ADD_CODE (td, MINT_REFANYTYPE);
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

		if (td->new_ip - td->new_code != new_in_start_offset) 
			td->last_new_ip = td->new_code + new_in_start_offset;
		else if (td->is_bb_start [td->in_start - td->il_code])
			td->is_bb_start [td->ip - td->il_code] = 1;
			
		td->last_ip = td->in_start;
	}
	in_offset = td->ip - header->code;
	g_assert (td->ip == end);
	td->in_offsets [in_offset] = td->new_ip - td->new_code;

	/* Handle relocations */
	for (int i = 0; i < td->relocs->len; ++i) {
		Reloc *reloc = g_ptr_array_index (td->relocs, i);

		int offset = td->in_offsets [reloc->target] - reloc->offset;

		switch (reloc->type) {
		case RELOC_SHORT_BRANCH:
			g_assert (td->new_code [reloc->offset + 1] == 0xffff);
			td->new_code [reloc->offset + 1] = offset;
			break;
		case RELOC_LONG_BRANCH: {
			guint16 *v = (guint16 *) &offset;
			g_assert (td->new_code [reloc->offset + 1] == 0xbeef);
			g_assert (td->new_code [reloc->offset + 2] == 0xdead);
			td->new_code [reloc->offset + 1] = *(guint16 *) v;
			td->new_code [reloc->offset + 2] = *(guint16 *) (v + 1);
			break;
		}
		case RELOC_SWITCH: {
			guint16 *v = (guint16*)&offset;
			td->new_code [reloc->offset] = *(guint16*)v;
			td->new_code [reloc->offset + 1] = *(guint16*)(v + 1);
			break;
		}
		default:
			g_assert_not_reached ();
			break;
		}
	}

	if (td->verbose_level) {
		const guint16 *p = td->new_code;
		g_print ("Runtime method: %s %p, VT stack size: %d\n", mono_method_full_name (method, TRUE), rtm, td->max_vt_sp);
		g_print ("Calculated stack size: %d, stated size: %d\n", td->max_stack_height, header->max_stack);
		while (p < td->new_ip) {
			char *ins = mono_interp_dis_mintop (td->new_code, p);
			g_print ("%s\n", ins);
			g_free (ins);
			p = mono_interp_dis_mintop_len (p);
		}
	}
	g_assert (td->max_stack_height <= (header->max_stack + 1));

	int code_len = td->new_ip - td->new_code;

	rtm->clauses = mono_domain_alloc0 (domain, header->num_clauses * sizeof (MonoExceptionClause));
	memcpy (rtm->clauses, header->clauses, header->num_clauses * sizeof(MonoExceptionClause));
	rtm->code = mono_domain_alloc0 (domain, (td->new_ip - td->new_code) * sizeof (gushort));
	memcpy (rtm->code, td->new_code, (td->new_ip - td->new_code) * sizeof(gushort));
	g_free (td->new_code);
	rtm->new_body_start = rtm->code + body_start_offset;
	rtm->init_locals = header->init_locals;
	rtm->num_clauses = header->num_clauses;
	for (i = 0; i < header->num_clauses; i++) {
		MonoExceptionClause *c = rtm->clauses + i;
		int end_off = c->try_offset + c->try_len;
		c->try_offset = td->in_offsets [c->try_offset];
		c->try_len = td->in_offsets [end_off] - c->try_offset;
		end_off = c->handler_offset + c->handler_len;
		c->handler_offset = td->in_offsets [c->handler_offset];
		c->handler_len = td->in_offsets [end_off] - c->handler_offset;
		if (c->flags & MONO_EXCEPTION_CLAUSE_FILTER)
			c->data.filter_offset = td->in_offsets [c->data.filter_offset];
	}
	rtm->vt_stack_size = td->max_vt_sp;
	rtm->alloca_size = rtm->locals_size + rtm->args_size + rtm->vt_stack_size + rtm->stack_size;
	rtm->data_items = mono_domain_alloc0 (domain, td->n_data_items * sizeof (td->data_items [0]));
	memcpy (rtm->data_items, td->data_items, td->n_data_items * sizeof (td->data_items [0]));

	/* Save debug info */
	interp_save_debug_info (rtm, header, td, line_numbers);

	/* Create a MonoJitInfo for the interpreted method by creating the interpreter IR as the native code. */
	int jinfo_len = mono_jit_info_size (0, header->num_clauses, 0);
	MonoJitInfo *jinfo = (MonoJitInfo *)mono_domain_alloc0 (domain, jinfo_len);
	jinfo->is_interp = 1;
	rtm->jinfo = jinfo;
	mono_jit_info_init (jinfo, method, (guint8*)rtm->code, code_len, 0, header->num_clauses, 0);
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
		} else {
			ei->data.catch_class = c->data.catch_class;
		}
	}

	save_seq_points (td);

	g_free (td->in_offsets);
	for (i = 0; i < header->code_size; ++i)
		g_free (td->stack_state [i]);
	g_free (td->stack_state);
	g_free (td->stack_height);
	g_free (td->vt_stack_size);
	g_free (td->data_items);
	g_free (td->stack);
	g_hash_table_destroy (td->data_hash);
	g_free (td->clause_indexes);
	g_ptr_array_free (td->seq_points, TRUE);
	g_array_free (line_numbers, TRUE);
	g_ptr_array_free (td->relocs, TRUE);
	mono_mempool_destroy (td->mempool);
}

static mono_mutex_t calc_section;

void 
mono_interp_transform_init (void)
{
	mono_os_mutex_init_recursive(&calc_section);
}

MonoException *
mono_interp_transform_method (InterpMethod *imethod, ThreadContext *context, InterpFrame *frame)
{
	MonoError error;
	int i, align, size, offset;
	MonoMethod *method = imethod->method;
	MonoImage *image = method->klass->image;
	MonoMethodHeader *header = NULL;
	MonoMethodSignature *signature = mono_method_signature (method);
	register const unsigned char *ip, *end;
	const MonoOpcode *opcode;
	MonoMethod *m;
	MonoClass *klass;
	unsigned char *is_bb_start;
	int in;
	MonoVTable *method_class_vt;
	int backwards;
	MonoGenericContext *generic_context = NULL;
	MonoDomain *domain = imethod->domain;
	InterpMethod tmp_imethod;
	InterpMethod *real_imethod;

	error_init (&error);

	if (mono_class_is_open_constructed_type (&method->klass->byval_arg)) {
		mono_error_set_invalid_operation (&error, "Could not execute the method because the containing type is not fully instantiated.");
		return mono_error_convert_to_exception (&error);
	}

	// g_printerr ("TRANSFORM(0x%016lx): begin %s::%s\n", mono_thread_current (), method->klass->name, method->name);
	method_class_vt = mono_class_vtable_full (domain, imethod->method->klass, &error);
	if (!is_ok (&error))
		return mono_error_convert_to_exception (&error);

	if (!method_class_vt->initialized) {
		mono_runtime_class_init_full (method_class_vt, &error);
		if (!mono_error_ok (&error)) {
			return mono_error_convert_to_exception (&error);
		}
	}

	MONO_PROFILER_RAISE (jit_begin, (method));

	if (mono_method_signature (method)->is_inflated)
		generic_context = mono_method_get_context (method);
	else {
		MonoGenericContainer *generic_container = mono_method_get_generic_container (method);
		if (generic_container)
			generic_context = &generic_container->context;
	}

	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		MonoMethod *nm = NULL;
		mono_os_mutex_lock(&calc_section);
		if (imethod->transformed) {
			mono_os_mutex_unlock(&calc_section);
			MONO_PROFILER_RAISE (jit_done, (method, imethod->jinfo));
			return NULL;
		}

		/* assumes all internal calls with an array this are built in... */
		if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL && (! mono_method_signature (method)->hasthis || method->klass->rank == 0)) {
			nm = mono_marshal_get_native_wrapper (method, TRUE, FALSE);
			signature = mono_method_signature (nm);
		} else {
			const char *name = method->name;
			if (method->klass->parent == mono_defaults.multicastdelegate_class) {
				if (*name == '.' && (strcmp (name, ".ctor") == 0)) {
					MonoJitICallInfo *mi = mono_find_jit_icall_by_name ("ves_icall_mono_delegate_ctor");
					g_assert (mi);
					char *wrapper_name = g_strdup_printf ("__icall_wrapper_%s", mi->name);
					nm = mono_marshal_get_icall_wrapper (mi->sig, wrapper_name, mi->func, TRUE);
					g_free (wrapper_name);
				} else if (*name == 'I' && (strcmp (name, "Invoke") == 0)) {
					MonoDelegate *del = frame->stack_args [0].data.p;
					nm = mono_marshal_get_delegate_invoke (method, del);
				} else if (*name == 'B' && (strcmp (name, "BeginInvoke") == 0)) {
					nm = mono_marshal_get_delegate_begin_invoke (method);
				} else if (*name == 'E' && (strcmp (name, "EndInvoke") == 0)) {
					nm = mono_marshal_get_delegate_end_invoke (method);
				}
			}
			if (nm == NULL) {
				imethod->code = g_malloc(sizeof(short));
				imethod->code[0] = MINT_CALLRUN;
			}
		}
		if (nm == NULL) {
			imethod->stack_size = sizeof (stackval); /* for tracing */
			imethod->alloca_size = imethod->stack_size;
			imethod->transformed = TRUE;
			mono_os_mutex_unlock(&calc_section);
			MONO_PROFILER_RAISE (jit_done, (method, NULL));
			return NULL;
		}
		method = nm;
		header = mono_method_get_header (nm);
		mono_os_mutex_unlock(&calc_section);
	} else if (method->klass == mono_defaults.array_class) {
		if (!strcmp (method->name, "UnsafeMov") || !strcmp (method->name, "UnsafeLoad")) {
			mono_os_mutex_lock (&calc_section);
			if (!imethod->transformed) {
				imethod->code = g_malloc (sizeof (short));
				imethod->code[0] = MINT_CALLRUN;
				imethod->stack_size = sizeof (stackval); /* for tracing */
				imethod->alloca_size = imethod->stack_size;
				imethod->transformed = TRUE;
			}
			mono_os_mutex_unlock(&calc_section);
			MONO_PROFILER_RAISE (jit_done, (method, NULL));
			return NULL;
		} else if (!strcmp (method->name, "UnsafeStore")) {
			g_error ("TODO ArrayClass::UnsafeStore");
		}
	}

	if (!header) {
		header = mono_method_get_header_checked (method, &error);
		if (!is_ok (&error))
			return mono_error_convert_to_exception (&error);
	}

	g_assert ((signature->param_count + signature->hasthis) < 1000);
	g_assert (header->max_stack < 10000);
	/* intern the strings in the method. */
	ip = header->code;
	end = ip + header->code_size;

	is_bb_start = g_malloc0(header->code_size);
	is_bb_start [0] = 1;
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
			if (method->wrapper_type == MONO_WRAPPER_NONE)
				mono_ldstr (domain, image, mono_metadata_token_index (read32 (ip + 1)));
			ip += 5;
			break;
		case MonoInlineType:
			if (method->wrapper_type == MONO_WRAPPER_NONE) {
				klass = mini_get_class (method, read32 (ip + 1), generic_context);
				mono_class_init (klass);
				/* quick fix to not do this for the fake ptr classes - probably should not be getting the vtable at all here */
#if 0
				g_error ("FIXME: interface method lookup: %s (in method %s)", klass->name, method->name);
				if (!(klass->flags & TYPE_ATTRIBUTE_INTERFACE) && klass->interface_offsets != NULL)
					mono_class_vtable (domain, klass);
#endif
			}
			ip += 5;
			break;
		case MonoInlineMethod:
			if (method->wrapper_type == MONO_WRAPPER_NONE && *ip != CEE_CALLI) {
				m = mono_get_method_checked (image, read32 (ip + 1), NULL, generic_context, &error);
				if (!is_ok (&error)) {
					g_free (is_bb_start);
					return mono_error_convert_to_exception (&error);
				}
				mono_class_init (m->klass);
				if (!mono_class_is_interface (m->klass))
					mono_class_vtable (domain, m->klass);
			}
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
			backwards = offset < 0;
			offset += ip - header->code;
			g_assert (offset >= 0 && offset < header->code_size);
			is_bb_start [offset] |= backwards ? 2 : 1;
			break;
		case MonoShortInlineBrTarget:
			offset = ((gint8 *)ip) [1];
			ip += 2;
			backwards = offset < 0;
			offset += ip - header->code;
			g_assert (offset >= 0 && offset < header->code_size);
			is_bb_start [offset] |= backwards ? 2 : 1;
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
				is_bb_start [offset] |= backwards ? 2 : 1;
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
	// g_printerr ("TRANSFORM(0x%016lx): end %s::%s\n", mono_thread_current (), method->klass->name, method->name);

	/* Make modifications to a copy of imethod, copy them back inside the lock */
	real_imethod = imethod;
	memcpy (&tmp_imethod, imethod, sizeof (InterpMethod));
	imethod = &tmp_imethod;

	imethod->local_offsets = g_malloc (header->num_locals * sizeof(guint32));
	imethod->stack_size = (sizeof (stackval)) * (header->max_stack + 2); /* + 1 for returns of called functions  + 1 for 0-ing in trace*/
	imethod->stack_size = (imethod->stack_size + 7) & ~7;
	offset = 0;
	for (i = 0; i < header->num_locals; ++i) {
		size = mono_type_size (header->locals [i], &align);
		offset += align - 1;
		offset &= ~(align - 1);
		imethod->local_offsets [i] = offset;
		offset += size;
	}
	offset = (offset + 7) & ~7;

	imethod->exvar_offsets = g_malloc (header->num_clauses * sizeof (guint32));
	for (i = 0; i < header->num_clauses; i++) {
		imethod->exvar_offsets [i] = offset;
		offset += sizeof (MonoObject*);
	}
	offset = (offset + 7) & ~7;

	imethod->locals_size = offset;
	g_assert (imethod->locals_size < 65536);
	offset = 0;
	imethod->arg_offsets = g_malloc ((!!signature->hasthis + signature->param_count) * sizeof(guint32));

	if (signature->hasthis) {
		g_assert (!signature->pinvoke);
		size = align = SIZEOF_VOID_P;
		offset += align - 1;
		offset &= ~(align - 1);
		imethod->arg_offsets [0] = offset;
		offset += size;
	}

	for (i = 0; i < signature->param_count; ++i) {
		if (signature->pinvoke) {
			guint32 dummy;
			size = mono_type_native_stack_size (signature->params [i], &dummy);
			align = 8;
		}
		else
			size = mono_type_stack_size (signature->params [i], &align);
		offset += align - 1;
		offset &= ~(align - 1);
		imethod->arg_offsets [i + !!signature->hasthis] = offset;
		offset += size;
	}
	offset = (offset + 7) & ~7;
	imethod->args_size = offset;
	g_assert (imethod->args_size < 10000);

	error_init (&error);
	generate (method, header, imethod, is_bb_start, generic_context, &error);

	mono_metadata_free_mh (header);
	g_free (is_bb_start);

	if (!mono_error_ok (&error))
		return mono_error_convert_to_exception (&error);

	// FIXME: Add a different callback ?
	MONO_PROFILER_RAISE (jit_done, (method, imethod->jinfo));

	/* Copy changes back */
	imethod = real_imethod;
	mono_os_mutex_lock (&calc_section);
	if (!imethod->transformed) {
		InterpMethod *hash = imethod->next_jit_code_hash;
		memcpy (imethod, &tmp_imethod, sizeof (InterpMethod));
		imethod->next_jit_code_hash = hash;
		mono_memory_barrier ();
		imethod->transformed = TRUE;
	}
	mono_os_mutex_unlock (&calc_section);

	return NULL;
}

