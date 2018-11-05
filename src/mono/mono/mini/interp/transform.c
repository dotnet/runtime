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
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/seq-points-data.h>
#include <mono/metadata/mono-basic-block.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/reflection-internals.h>

#include <mono/mini/mini.h>
#include <mono/mini/mini-runtime.h>

#include "mintops.h"
#include "interp-internals.h"
#include "interp.h"

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
	unsigned int stack_capacity;
	unsigned int vt_sp;
	unsigned int max_vt_sp;
	unsigned int total_locals_size;
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
#define STACK_TYPE_R4 2
#define STACK_TYPE_R8 3
#define STACK_TYPE_O  4
#define STACK_TYPE_VT 5
#define STACK_TYPE_MP 6
#define STACK_TYPE_F  7

static const char *stack_type_string [] = { "I4", "I8", "R4", "R8", "O ", "VT", "MP", "F " };

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
	STACK_TYPE_R4, /*R4*/
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
	td->new_code = (guint16*)g_realloc (td->new_code, (td->max_code_size *= 2) * sizeof (td->new_code [0]));
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
				m_class_get_name ((td)->method->klass), (td)->method->name, \
				stack_size, n, (td)->ip - (td)->il_code); \
	} while (0)

#define ENSURE_I4(td, sp_off) \
	do { \
		if ((td)->sp [-sp_off].type == STACK_TYPE_I8) \
			ADD_CODE(td, sp_off == 1 ? MINT_CONV_I4_I8 : MINT_CONV_I4_I8_SP); \
	} while (0)

#define CHECK_TYPELOAD(klass) \
	do { \
		if (!(klass) || mono_class_has_failure (klass)) { \
			mono_error_set_for_class_failure (error, klass); \
			goto exit; \
		} \
	} while (0)


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
			ADD_CODE (td, MINT_SAFEPOINT);
		else
			ADD_CODE (td, MINT_CHECKPOINT);
	}
	if (offset > 0 && td->stack_height [target] < 0) {
		td->stack_height [target] = td->sp - td->stack;
		if (td->stack_height [target] > 0)
			td->stack_state [target] = (StackInfo*)g_memdup (td->stack, td->stack_height [target] * sizeof (td->stack [0]));
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

		Reloc *reloc = (Reloc*)mono_mempool_alloc0 (td->mempool, sizeof (Reloc));
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
	} else if (type1 == STACK_TYPE_R4 && type2 == STACK_TYPE_R8) {
		ADD_CODE (td, MINT_CONV_R8_R4);
		td->in_offsets [td->ip - td->il_code]++;
	} else if (type1 == STACK_TYPE_R8 && type2 == STACK_TYPE_R4) {
		ADD_CODE (td, MINT_CONV_R8_R4_SP);
		td->in_offsets [td->ip - td->il_code]++;
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
	if (type1 == STACK_TYPE_R8 && type2 == STACK_TYPE_R4) {
		ADD_CODE (td, MINT_CONV_R8_R4);
		type2 = STACK_TYPE_R8;
	}
	if (type1 == STACK_TYPE_R4 && type2 == STACK_TYPE_R8) {
		ADD_CODE (td, MINT_CONV_R8_R4_SP);
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
			m_class_get_name (td->method->klass), td->method->name,
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

#define REALLOC_STACK(td, sppos) \
	do { \
		(td)->stack_capacity *= 2; \
		(td)->stack = (StackInfo*)realloc ((td)->stack, (td)->stack_capacity * sizeof (td->stack [0])); \
		(td)->sp = (td)->stack + sppos; \
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

	gboolean hasthis = mono_method_signature_internal (td->method)->hasthis;
	if (hasthis && n == 0)
		type = m_class_get_byval_arg (td->method->klass);
	else
		type = mono_method_signature_internal (td->method)->params [hasthis ? n - 1 : n];

	mt = mint_type (type);
	if (mt == MINT_TYPE_VT) {
		gint32 size;
		klass = mono_class_from_mono_type_internal (type);
		if (mono_method_signature_internal (td->method)->pinvoke)
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

	gboolean hasthis = mono_method_signature_internal (td->method)->hasthis;
	if (hasthis && n == 0)
		type = m_class_get_byval_arg (td->method->klass);
	else
		type = mono_method_signature_internal (td->method)->params [n - !!hasthis];

	mt = mint_type (type);
	if (mt == MINT_TYPE_VT) {
		gint32 size;
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		if (mono_method_signature_internal (td->method)->pinvoke)
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
	gboolean hasthis = mono_method_signature_internal (td->method)->hasthis;
	if (hasthis && n == 0)
		type = m_class_get_byval_arg (td->method->klass);
	else
		type = mono_method_signature_internal (td->method)->params [n - !!hasthis];

	int mt = mint_type (type);
	if (hasthis && n == 0) {
		ADD_CODE (td, MINT_STINARG_P);
		ADD_CODE (td, n);
		return;
	}
	if (mt == MINT_TYPE_VT) {
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		gint32 size;
		if (mono_method_signature_internal (td->method)->pinvoke)
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
		klass = mono_class_from_mono_type_internal (type);
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
			klass = mono_class_from_mono_type_internal (type);
	}
	PUSH_TYPE(td, stack_type[mt], klass);
}

static void 
store_local_general (TransformData *td, int offset, MonoType *type)
{
	int mt = mint_type (type);
	CHECK_STACK (td, 1);
#if SIZEOF_VOID_P == 8
	if (td->sp [-1].type == STACK_TYPE_I4 && stack_type [mt] == STACK_TYPE_I8) {
		ADD_CODE(td, MINT_CONV_I8_I4);
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

static void
store_local (TransformData *td, int n)
{
	MonoType *type = td->header->locals [n];
	int offset = td->rtm->local_offsets [n];
	store_local_general (td, offset, type);
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
		td->data_items = (gpointer*)g_realloc (td->data_items, td->max_data_items * sizeof(td->data_items [0]));
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

	if (mono_aot_only && m_class_get_image (method->klass)->aot_module) {
		ERROR_DECL (error);
		gpointer addr = mono_jit_compile_method_jit_only (method, error);
		if (addr && mono_error_ok (error))
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
	MonoJitICallInfo *info = mono_find_jit_icall_by_name ("mono_throw_method_access");

	/* Inject code throwing MethodAccessException */
	ADD_CODE (td, MINT_MONO_LDPTR);
	ADD_CODE (td, get_data_item_index (td, method));
	PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);

	ADD_CODE (td, MINT_MONO_LDPTR);
	ADD_CODE (td, get_data_item_index (td, target_method));
	PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);

	ADD_CODE (td, MINT_ICALL_PP_V);
	ADD_CODE (td, get_data_item_index (td, (gpointer)info->func));

	td->sp -= 2;
}

/*
 * These are additional locals that can be allocated as we transform the code.
 * They are allocated past the method locals so they are accessed in the same
 * way, with an offset relative to the frame->locals.
 */
static int
create_interp_local (TransformData *td, MonoType *type)
{
	int align, size;
	int offset = td->total_locals_size;

	size = mono_type_size (type, &align);
	offset = ALIGN_TO (offset, align);

	td->total_locals_size = offset + size;

	return offset;
}

static void
dump_mint_code (TransformData *td)
{
	const guint16 *p = td->new_code;
	while (p < td->new_ip) {
		char *ins = mono_interp_dis_mintop (td->new_code, p);
		g_print ("%s\n", ins);
		g_free (ins);
		p = mono_interp_dis_mintop_len (p);
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
	int local_offset = create_interp_local (td, mini_native_type_replace_type (src));

	store_local_general (td, local_offset, src);

	size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
	ADD_CODE (td, MINT_LDLOC_VT);
	ADD_CODE (td, local_offset);
	WRITE32 (td, &size);

	PUSH_VT (td, size);
	PUSH_TYPE (td, STACK_TYPE_VT, NULL);
}

/* Return TRUE if call transformation is finished */
static gboolean
interp_handle_intrinsics (TransformData *td, MonoMethod *target_method, MonoMethodSignature *csignature, gboolean readonly, int *op)
{
	const char *tm = target_method->name;
	int i;
	int type_index = mono_class_get_magic_index (target_method->klass);
	gboolean in_corlib = m_class_get_image (target_method->klass) == mono_defaults.corlib;
	const char *klass_name_space = m_class_get_name_space (target_method->klass);
	const char *klass_name = m_class_get_name (target_method->klass);

	if (target_method->klass == mono_defaults.string_class) {
		if (tm [0] == 'g') {
			if (strcmp (tm, "get_Chars") == 0)
				*op = MINT_GETCHR;
			else if (strcmp (tm, "get_Length") == 0)
				*op = MINT_STRLEN;
		}
	} else if (type_index >= 0) {
		MonoClass *magic_class = target_method->klass;

		const int mt = mint_type (m_class_get_byval_arg (magic_class));
		if (!strcmp (".ctor", tm)) {
			MonoType *arg = csignature->params [0];
			/* depending on SIZEOF_VOID_P and the type of the value passed to the .ctor we either have to CONV it, or do nothing */
			int arg_size = mini_magic_type_size (NULL, arg);

			if (arg_size > SIZEOF_VOID_P) { // 8 -> 4
				switch (type_index) {
				case 0: case 1:
					ADD_CODE (td, MINT_CONV_I4_I8);
					break;
				case 2:
					ADD_CODE (td, MINT_CONV_R4_R8);
					break;
				}
			}

			if (arg_size < SIZEOF_VOID_P) { // 4 -> 8
				switch (type_index) {
				case 0:
					ADD_CODE (td, MINT_CONV_I8_I4);
					break;
				case 1:
					ADD_CODE (td, MINT_CONV_I8_U4);
					break;
				case 2:
					ADD_CODE (td, MINT_CONV_R8_R4);
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
			return TRUE;
		} else if (!strcmp ("op_Implicit", tm ) || !strcmp ("op_Explicit", tm)) {
			MonoType *src = csignature->params [0];
			MonoType *dst = csignature->ret;
			int src_size = mini_magic_type_size (NULL, src);
			int dst_size = mini_magic_type_size (NULL, dst);

			gboolean store_value_as_local = FALSE;

			switch (type_index) {
			case 0: case 1:
				if (!mini_magic_is_int_type (src) || !mini_magic_is_int_type (dst)) {
					if (mini_magic_is_int_type (src))
						store_value_as_local = TRUE;
					else
						return FALSE;
				}
				break;
			case 2:
				if (!mini_magic_is_float_type (src) || !mini_magic_is_float_type (dst)) {
					if (mini_magic_is_float_type (src))
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

#if SIZEOF_VOID_P == 4
			if (src_size > dst_size) { // 8 -> 4
				switch (type_index) {
				case 0: case 1:
					ADD_CODE (td, MINT_CONV_I4_I8);
					break;
				case 2:
					ADD_CODE (td, MINT_CONV_R4_R8);
					break;
				}
			}
#endif

#if SIZEOF_VOID_P == 8
			if (src_size < dst_size) { // 4 -> 8
				switch (type_index) {
				case 0:
					ADD_CODE (td, MINT_CONV_I8_I4);
					break;
				case 1:
					ADD_CODE (td, MINT_CONV_I8_U4);
					break;
				case 2:
					ADD_CODE (td, MINT_CONV_R8_R4);
					break;
				}
			}
#endif

			SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp ("op_Increment", tm)) {
			g_assert (type_index != 2); // no nfloat
#if SIZEOF_VOID_P == 8
			ADD_CODE (td, MINT_ADD1_I8);
#else
			ADD_CODE (td, MINT_ADD1_I4);
#endif
			SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp ("op_Decrement", tm)) {
			g_assert (type_index != 2); // no nfloat
#if SIZEOF_VOID_P == 8
			ADD_CODE (td, MINT_SUB1_I8);
#else
			ADD_CODE (td, MINT_SUB1_I4);
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
			/* white list */
			return FALSE;
		} else if (!strcmp ("Parse", tm)) {
			/* white list */
			return FALSE;
		} else if (!strcmp ("ToString", tm)) {
			/* white list */
			return FALSE;
		} else if (!strcmp ("IsNaN", tm) || !strcmp ("IsInfinity", tm) || !strcmp ("IsNegativeInfinity", tm) || !strcmp ("IsPositiveInfinity", tm)) {
			g_assert (type_index == 2); // nfloat only
			/* white list */
			return FALSE;
		}

		for (i = 0; i < sizeof (int_unnop) / sizeof  (MagicIntrinsic); ++i) {
			if (!strcmp (int_unnop [i].op_name, tm)) {
				ADD_CODE (td, int_unnop [i].insn [type_index]);
				SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
				td->ip += 5;
				return TRUE;
			}
		}

		for (i = 0; i < sizeof (int_binop) / sizeof  (MagicIntrinsic); ++i) {
			if (!strcmp (int_binop [i].op_name, tm)) {
				ADD_CODE (td, int_binop [i].insn [type_index]);
				td->sp -= 1;
				SET_TYPE (td->sp - 1, stack_type [mt], magic_class);
				td->ip += 5;
				return TRUE;
			}
		}

		for (i = 0; i < sizeof (int_cmpop) / sizeof  (MagicIntrinsic); ++i) {
			if (!strcmp (int_cmpop [i].op_name, tm)) {
				MonoClass *k = mono_defaults.boolean_class;
				ADD_CODE (td, int_cmpop [i].insn [type_index]);
				td->sp -= 1;
				SET_TYPE (td->sp - 1, stack_type [mint_type (m_class_get_byval_arg (k))], k);
				td->ip += 5;
				return TRUE;
			}
		}

		g_error ("TODO: interp_transform_call %s:%s", m_class_get_name (target_method->klass), tm);
	} else if (mono_class_is_subclass_of (target_method->klass, mono_defaults.array_class, FALSE)) {
		if (!strcmp (tm, "get_Rank")) {
			*op = MINT_ARRAY_RANK;
		} else if (!strcmp (tm, "get_Length")) {
			*op = MINT_LDLEN;
		} else if (!strcmp (tm, "Address")) {
			*op = readonly ? MINT_LDELEMA : MINT_LDELEMA_TC;
		} else if (!strcmp (tm, "UnsafeMov") || !strcmp (tm, "UnsafeLoad") || !strcmp (tm, "Set") || !strcmp (tm, "Get")) {
			*op = MINT_CALLRUN;
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
	} else if (in_corlib && !strcmp (klass_name_space, "System") && !strcmp (klass_name, "ByReference`1")) {
		*op = MINT_INTRINS_BYREFERENCE_GET_VALUE;
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
				ADD_CODE (td, MINT_GETITEM_SPAN);
				ADD_CODE (td, size);
				ADD_CODE (td, offset_length);
				ADD_CODE (td, offset_pointer);

				SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_MP);
				td->sp -= 1;
				td->ip += 5;
				return TRUE;
			}
		} else if (!strcmp (tm, "get_Length")) {
			MonoClassField *length_field = mono_class_get_field_from_name_full (target_method->klass, "_length", NULL);
			g_assert (length_field);
			int offset_length = length_field->offset - sizeof (MonoObject);
			ADD_CODE (td, MINT_LDLEN_SPAN);
			ADD_CODE (td, offset_length);
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
			td->ip += 5;
			return TRUE;
		}
	}
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
			target_method = mono_marshal_get_native_wrapper (target_method, TRUE, FALSE);
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

static void
interp_transform_call (TransformData *td, MonoMethod *method, MonoMethod *target_method, MonoDomain *domain, MonoGenericContext *generic_context, unsigned char *is_bb_start, int body_start_offset, MonoClass *constrained_class, gboolean readonly, MonoError *error, gboolean check_visibility)
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
	int is_void = 0;
	int need_null_check = is_virtual;

	guint32 token = read32 (td->ip + 1);

	if (target_method == NULL) {
		if (calli) {
			CHECK_STACK(td, 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				csignature = (MonoMethodSignature *)mono_method_get_wrapper_data (method, token);
			else {
				csignature = mono_metadata_parse_signature_checked (image, token, error);
				return_if_nok (error);
			}

			if (generic_context) {
				csignature = mono_inflate_generic_signature (csignature, generic_context, error);
				return_if_nok (error);
			}

			/*
			 * The compiled interp entry wrapper is passed to runtime_invoke instead of
			 * the InterpMethod pointer. FIXME
			 */
			native = csignature->pinvoke || method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE;
			--td->sp;

			target_method = NULL;
		} else {
			if (method->wrapper_type == MONO_WRAPPER_NONE) {
				target_method = mono_get_method_checked (image, token, NULL, generic_context, error);
				return_if_nok (error);
			} else
				target_method = (MonoMethod *)mono_method_get_wrapper_data (method, token);
			csignature = mono_method_signature_internal (target_method);

			if (generic_context) {
				csignature = mono_inflate_generic_signature (csignature, generic_context, error);
				return_if_nok (error);
				target_method = mono_class_inflate_generic_method_checked (target_method, generic_context, error);
				return_if_nok (error);
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
	if (target_method && interp_handle_intrinsics (td, target_method, csignature, readonly, &op))
		return;

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
			return;
		}
#if DEBUG_INTERP
		g_print ("CONSTRAINED.CALLVIRT: %s::%s.  %s (%p) ->\n", target_method->klass->name, target_method->name, mono_signature_full_name (target_method->signature), target_method);
#endif
		target_method = mono_get_method_constrained_with_method (image, target_method, constrained_class, generic_context, error);
#if DEBUG_INTERP
		g_print ("                    : %s::%s.  %s (%p)\n", target_method->klass->name, target_method->name, mono_signature_full_name (target_method->signature), target_method);
#endif
		return_if_nok (error);
		mono_class_setup_vtable (target_method->klass);

		if (m_class_is_valuetype (constrained_class) && (target_method->klass == mono_defaults.object_class || target_method->klass == m_class_get_parent (mono_defaults.enum_class) || target_method->klass == mono_defaults.enum_class)) {
			if (target_method->klass == mono_defaults.enum_class && (td->sp - csignature->param_count - 1)->type == STACK_TYPE_MP) {
				/* managed pointer on the stack, we need to deref that puppy */
				ADD_CODE (td, MINT_LDIND_I);
				ADD_CODE (td, csignature->param_count);
			}
			if (mint_type (m_class_get_byval_arg (constrained_class)) == MINT_TYPE_VT) {
				ADD_CODE (td, MINT_BOX_VT);
				ADD_CODE (td, get_data_item_index (td, constrained_class));
				ADD_CODE (td, csignature->param_count | ((td->sp - 1 - csignature->param_count)->type != STACK_TYPE_MP ? 0 : BOX_NOT_CLEAR_VT_SP));
			} else {
				ADD_CODE (td, MINT_BOX);
				ADD_CODE (td, get_data_item_index (td, constrained_class));
				ADD_CODE (td, csignature->param_count);
			}
		} else if (!m_class_is_valuetype (constrained_class)) {
			/* managed pointer on the stack, we need to deref that puppy */
			ADD_CODE (td, MINT_LDIND_I);
			ADD_CODE (td, csignature->param_count);
		} else {
			if (m_class_is_valuetype (target_method->klass)) {
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
				target_method = m_class_get_vtable (constrained_class) [ioffset + slot];

				if (target_method->klass == mono_defaults.enum_class) {
					if ((td->sp - csignature->param_count - 1)->type == STACK_TYPE_MP) {
						/* managed pointer on the stack, we need to deref that puppy */
						ADD_CODE (td, MINT_LDIND_I);
						ADD_CODE (td, csignature->param_count);
					}
					if (mint_type (m_class_get_byval_arg (constrained_class)) == MINT_TYPE_VT) {
						ADD_CODE (td, MINT_BOX_VT);
						ADD_CODE (td, get_data_item_index (td, constrained_class));
						ADD_CODE (td, csignature->param_count | ((td->sp - 1 - csignature->param_count)->type != STACK_TYPE_MP ? 0 : BOX_NOT_CLEAR_VT_SP));
					} else {
						ADD_CODE (td, MINT_BOX);
						ADD_CODE (td, get_data_item_index (td, constrained_class));
						ADD_CODE (td, csignature->param_count);
					}
				}
			}
			is_virtual = FALSE;
		}
	}

	if (target_method)
		mono_class_init (target_method->klass);

	if (!is_virtual && target_method && (target_method->flags & METHOD_ATTRIBUTE_ABSTRACT))
		/* MS.NET seems to silently convert this to a callvirt */
		is_virtual = TRUE;

	if (is_virtual && target_method && (!(target_method->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
		(MONO_METHOD_IS_FINAL (target_method) &&
		 target_method->wrapper_type != MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK)) &&
		!(mono_class_is_marshalbyref (target_method->klass))) {
		/* Not really virtual, just needs a null check */
		is_virtual = FALSE;
		need_null_check = TRUE;
	}

	CHECK_STACK (td, csignature->param_count + csignature->hasthis);
	if (!calli && op == -1 && (!is_virtual || (target_method->flags & METHOD_ATTRIBUTE_VIRTUAL) == 0) &&
		(target_method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) == 0 && 
		(target_method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) == 0 &&
		!(target_method->iflags & METHOD_IMPL_ATTRIBUTE_NOINLINING)) {
		MonoVTable *vt = mono_class_vtable_checked (domain, target_method->klass, error);
		return_if_nok (error);
		int called_inited = vt->initialized;

		if (method == target_method && *(td->ip + 5) == CEE_RET && !(csignature->hasthis && m_class_is_valuetype (target_method->klass))) {
			int offset;
			if (td->verbose_level)
				g_print ("Optimize tail call of %s.%s\n", m_class_get_name (target_method->klass), target_method->name);

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
			MonoMethodHeader *mheader = interp_method_get_header (target_method, error);
			return_if_nok (error);
			/* mheader might not exist if this is a delegate invoc, etc */
			gboolean has_vt_arg = FALSE;
			for (i = 0; i < csignature->param_count; i++)
				has_vt_arg |= !mini_type_is_reference (csignature->params [i]);

			gboolean empty_callee = mheader && *mheader->code == CEE_RET;
			mono_metadata_free_mh (mheader);

			if (empty_callee && called_inited && !has_vt_arg) {
				if (td->verbose_level)
					g_print ("Inline (empty) call of %s.%s\n", m_class_get_name (target_method->klass), target_method->name);
				for (i = 0; i < csignature->param_count; i++) {
					ADD_CODE (td, MINT_POP); /*FIX: vt */
					ADD_CODE (td, 0);
				}
				if (csignature->hasthis) {
					if (is_virtual || need_null_check)
						ADD_CODE (td, MINT_CKNULL);
					ADD_CODE (td, MINT_POP);
					ADD_CODE (td, 0);
				}
				td->sp -= csignature->param_count + csignature->hasthis;
				td->ip += 5;
				return;
			}
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
			vararg_stack += arg_size;
		}
		vt_stack_used += ALIGN_TO (vararg_stack, MINT_VT_ALIGNMENT);
		PUSH_VT (td, vararg_stack);
	}

	g_assert (csignature->call_convention != MONO_CALL_FASTCALL);
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

	if (need_null_check) {
		ADD_CODE (td, MINT_CKNULL_N);
		ADD_CODE (td, csignature->param_count + 1);
	}

	/* need to handle typedbyref ... */
	if (csignature->ret->type != MONO_TYPE_VOID) {
		int mt = mint_type(csignature->ret);
		MonoClass *klass = mono_class_from_mono_type_internal (csignature->ret);
		if (mt == MINT_TYPE_VT) {
			if (csignature->pinvoke && method->wrapper_type != MONO_WRAPPER_NONE)
				vt_res_size = mono_class_native_size (klass, NULL);
			else
				vt_res_size = mono_class_value_size (klass, NULL);
			if (mono_class_has_failure (klass)) {
				mono_error_set_for_class_failure (error, klass);
				return;
			}
			PUSH_VT(td, vt_res_size);
		}
		PUSH_TYPE(td, stack_type[mt], klass);
	} else
		is_void = TRUE;

	/* We need to convert delegate invoke to a indirect call on the interp_invoke_impl field */
	if (target_method && m_class_get_parent (target_method->klass) == mono_defaults.multicastdelegate_class) {
		const char *name = target_method->name;
		if (*name == 'I' && (strcmp (name, "Invoke") == 0)) {
			calli = TRUE;
			ADD_CODE (td, MINT_LD_DELEGATE_INVOKE_IMPL);
			ADD_CODE (td, csignature->param_count + 1);
		}
	}

	if (op >= 0) {
		ADD_CODE (td, op);

		if (op == MINT_LDLEN) {
#ifdef MONO_BIG_ARRAYS
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I8);
#else
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
#endif
		}
		if (op == MINT_LDELEMA || op == MINT_LDELEMA_TC) {
			ADD_CODE (td, get_data_item_index (td, target_method->klass));
			ADD_CODE (td, 1 + m_class_get_rank (target_method->klass));
		}

		if (op == MINT_CALLRUN) {
			ADD_CODE (td, get_data_item_index (td, target_method));
			ADD_CODE (td, get_data_item_index (td, mono_method_signature_internal (target_method)));
		}
	} else if (!calli && !is_virtual && jit_call_supported (target_method, csignature)) {
		ADD_CODE(td, MINT_JIT_CALL);
		ADD_CODE(td, get_data_item_index (td, (void *)mono_interp_get_imethod (domain, target_method, error)));
		mono_error_assert_ok (error);
	} else {
		/* Try using fast icall path for simple signatures */
		if (native && !method->dynamic)
			op = interp_icall_op_for_sig (csignature);
		if (calli)
			ADD_CODE(td, native ? ((op != -1) ? MINT_CALLI_NAT_FAST : MINT_CALLI_NAT) : MINT_CALLI);
		else if (is_virtual)
			ADD_CODE(td, is_void ? MINT_VCALLVIRT : MINT_CALLVIRT);
		else
			ADD_CODE(td, is_void ? MINT_VCALL : MINT_CALL);

		if (calli) {
			ADD_CODE(td, get_data_item_index (td, (void *)csignature));
			if (op != -1)
				ADD_CODE (td, op);
		} else {
			ADD_CODE(td, get_data_item_index (td, (void *)mono_interp_get_imethod (domain, target_method, error)));
			return_if_nok (error);
			if (csignature->call_convention == MONO_CALL_VARARG)
				ADD_CODE(td, get_data_item_index (td, (void *)csignature));
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

	mono_domain_lock (domain);
	g_hash_table_insert (domain_jit_info (domain)->seq_points, rtm->method, info);
	mono_domain_unlock (domain);

	jinfo->seq_points = info;
}

static void
emit_seq_point (TransformData *td, int il_offset, InterpBasicBlock *cbb, gboolean nonempty_stack)
{
	SeqPoint *seqp;

	seqp = (SeqPoint*)mono_mempool_alloc0 (td->mempool, sizeof (SeqPoint));
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
	MonoMethodSignature *signature = mono_method_signature_internal (method);
	MonoImage *image = m_class_get_image (method->klass);
	MonoDomain *domain = rtm->domain;
	MonoClass *constrained_class = NULL;
	MonoSimpleBasicBlock *bb = NULL, *original_bb = NULL;
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
	GArray *line_numbers;
	MonoDebugMethodInfo *minfo;
	MonoBitSet *seq_point_locs = NULL;
	MonoBitSet *seq_point_set_locs = NULL;
	gboolean sym_seq_points = FALSE;
	InterpBasicBlock *bb_exit = NULL;
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
	td->is_bb_start = is_bb_start;
	td->il_code = header->code;
	td->code_size = header->code_size;
	td->header = header;
	td->max_code_size = td->code_size;
	td->new_code = (unsigned short *)g_malloc(td->max_code_size * sizeof(gushort));
	td->new_code_end = td->new_code + td->max_code_size;
	td->mempool = mono_mempool_new ();
	td->in_offsets = (int*)g_malloc0((header->code_size + 1) * sizeof(int));
	td->stack_state = (StackInfo**)g_malloc0(header->code_size * sizeof(StackInfo *));
	td->stack_height = (int*)g_malloc(header->code_size * sizeof(int));
	td->vt_stack_size = (int*)g_malloc(header->code_size * sizeof(int));
	td->n_data_items = 0;
	td->max_data_items = 0;
	td->data_items = NULL;
	td->data_hash = g_hash_table_new (NULL, NULL);
	td->clause_indexes = (int*)g_malloc (header->code_size * sizeof (int));
	td->gen_sdb_seq_points = mini_debug_options.gen_sdb_seq_points;
	td->seq_points = g_ptr_array_new ();
	td->relocs = g_ptr_array_new ();
	td->verbose_level = mono_interp_traceopt;
	td->total_locals_size = rtm->locals_size;
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
		} else if (!method->wrapper_type && !method->dynamic && mono_debug_image_has_debug_info (m_class_get_image (method->klass))) {
			/* Methods without line number info like auto-generated property accessors */
			seq_point_locs = mono_bitset_new (header->code_size, 0);
			seq_point_set_locs = mono_bitset_new (header->code_size, 0);
			sym_seq_points = TRUE;
		}
	}

	td->new_ip = td->new_code;
	td->last_new_ip = NULL;

	td->stack = (StackInfo*)g_malloc0 ((header->max_stack + 1) * sizeof (td->stack [0]));
	td->stack_capacity = header->max_stack + 1;
	td->sp = td->stack;
	td->max_stack_height = 0;

	line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));

	for (i = 0; i < header->num_clauses; i++) {
		MonoExceptionClause *c = header->clauses + i;
		td->stack_height [c->handler_offset] = 0;
		td->vt_stack_size [c->handler_offset] = 0;
		td->is_bb_start [c->handler_offset] = 1;

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

	/* safepoint is required on method entry */
	if (mono_threads_are_safepoints_enabled ())
		ADD_CODE (td, MINT_SAFEPOINT);

	if (sym_seq_points) {
		InterpBasicBlock *cbb = td->offset_to_bb [0];
		g_assert (cbb);
		emit_seq_point (td, METHOD_ENTRY_IL_OFFSET, cbb, FALSE);
	}

	original_bb = bb = mono_basic_block_split (method, error, header);
	goto_if_nok (error, exit);
	g_assert (bb);

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
			g_print ("IL_%04lx %s %-10s -> IL_%04lx, sp %ld, %s %-12s vt_sp %u (max %u)\n", 
				td->ip - td->il_code,
				td->is_bb_start [td->ip - td->il_code] == 3 ? "<>" :
				td->is_bb_start [td->ip - td->il_code] == 2 ? "< " :
				td->is_bb_start [td->ip - td->il_code] == 1 ? " >" : "  ",
				mono_opcode_name (*td->ip), td->new_ip - td->new_code, td->sp - td->stack, 
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
				ADD_CODE (td, MINT_SDB_INTR_LOC);

			emit_seq_point (td, in_offset, cbb, FALSE);

			mono_bitset_set_fast (seq_point_set_locs, td->ip - header->code);
		}

		if (sym_seq_points)
			bb_exit = td->offset_to_bb [td->ip - header->code];

		if (is_bb_start [in_offset]) {
			int index = td->clause_indexes [in_offset];
			if (index != -1) {
				MonoExceptionClause *clause = &header->clauses [index];
				if ((clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY ||
					clause->flags == MONO_EXCEPTION_CLAUSE_FAULT) &&
						in_offset == clause->handler_offset)
					ADD_CODE (td, MINT_START_ABORT_PROT);
			}
		}

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
			PUSH_SIMPLE_TYPE(td, STACK_TYPE_R4);
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
				size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
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
			m = mono_get_method_checked (image, token, NULL, generic_context, error);
			goto_if_nok (error, exit);
			ADD_CODE (td, MINT_JMP);
			ADD_CODE (td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
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

			interp_transform_call (td, method, NULL, domain, generic_context, is_bb_start, body_start_offset, constrained_class, readonly, error, TRUE);
			goto_if_nok (error, exit);

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
				InterpBasicBlock *cbb = td->offset_to_bb [td->ip - header->code];
				g_assert (cbb);
				emit_seq_point (td, METHOD_EXIT_IL_OFFSET, bb_exit, FALSE);
			}

			if (vt_size == 0)
				SIMPLE_OP(td, ult->type == MONO_TYPE_VOID ? MINT_RET_VOID : MINT_RET);
			else {
				ADD_CODE(td, MINT_RET_VT);
				WRITE32(td, &vt_size);
				++td->ip;
			}
			break;
		}
		case CEE_BR:
			handle_branch (td, MINT_BR_S, MINT_BR, 5 + read32 (td->ip + 1));
			td->ip += 5;
			break;
		case CEE_BR_S:
			handle_branch (td, MINT_BR_S, MINT_BR, 2 + (gint8)td->ip [1]);
			td->ip += 2;
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
						td->stack_state [target] = (StackInfo*)g_memdup (td->stack, stack_height * sizeof (td->stack [0]));

					Reloc *reloc = (Reloc*)mono_mempool_alloc0 (td->mempool, sizeof (Reloc));
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
			SIMPLE_OP (td, MINT_LDIND_I1_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_U1:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_U1_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_I2:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I2_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_U2:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_U2_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_I4:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I4_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_U4:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_U4_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_I8:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_I8_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_I:
			CHECK_STACK (td, 1);
			ADD_CODE (td, MINT_CKNULL);
			SIMPLE_OP (td, MINT_LDIND_I);
			ADD_CODE (td, 0);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_R4:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_R4_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R4);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_R8:
			CHECK_STACK (td, 1);
			SIMPLE_OP (td, MINT_LDIND_R8_CHECK);
			SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_R8);
			BARRIER_IF_VOLATILE (td);
			break;
		case CEE_LDIND_REF:
			CHECK_STACK (td, 1);
			ADD_CODE (td, MINT_CKNULL);
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
			case STACK_TYPE_R4:
				ADD_CODE (td, MINT_CONV_U1_R4);
				break;
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
			case STACK_TYPE_R4:
				ADD_CODE(td, MINT_CONV_I1_R4);
				break;
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
			case STACK_TYPE_R4:
				ADD_CODE(td, MINT_CONV_U2_R4);
				break;
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
			case STACK_TYPE_R4:
				ADD_CODE(td, MINT_CONV_I2_R4);
				break;
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
			case STACK_TYPE_R4:
				ADD_CODE(td, MINT_CONV_U4_R4);
				break;
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
			case STACK_TYPE_R4:
				ADD_CODE (td, MINT_CONV_I4_R4);
				break;
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
			case STACK_TYPE_R4:
				ADD_CODE(td, MINT_CONV_I8_R4);
				break;
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
				ADD_CODE(td, MINT_CONV_R8_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, MINT_CONV_R8_I8);
				break;
			case STACK_TYPE_R4:
				ADD_CODE (td, MINT_CONV_R8_R4);
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
			case STACK_TYPE_R4:
				ADD_CODE (td, MINT_CONV_U8_R4);
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
			klass = mono_class_get_and_inflate_typespec_checked (image, token, generic_context, error);
			goto_if_nok (error, exit);

			if (m_class_is_valuetype (klass)) {
				int mt = mint_type (m_class_get_byval_arg (klass));
				ADD_CODE (td, (mt == MINT_TYPE_VT) ? MINT_CPOBJ_VT : MINT_CPOBJ);
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
			else {
				klass = mono_class_get_and_inflate_typespec_checked (image, token, generic_context, error);
				goto_if_nok (error, exit);
			}

			MonoClass *tos_klass = td->sp [-1].klass;
			if (tos_klass && td->sp [-1].type == STACK_TYPE_VT) {
				int tos_size = mono_class_value_size (tos_klass, NULL);
				POP_VT (td, tos_size);
			}

			int mt = mint_type (m_class_get_byval_arg (klass));

			ADD_CODE(td, (mt == MINT_TYPE_VT) ? MINT_LDOBJ_VT: MINT_LDOBJ);
			ADD_CODE(td, get_data_item_index(td, klass));

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
			token = mono_metadata_token_index (read32 (td->ip + 1));
			td->ip += 5;
			if (method->wrapper_type == MONO_WRAPPER_NONE) {
				MonoString *s = mono_ldstr_checked (domain, image, token, error);
				goto_if_nok (error, exit);
				/* GC won't scan code stream, but reference is held by metadata
				 * machinery so we are good here */
				ADD_CODE (td, MINT_LDSTR);
				ADD_CODE (td, get_data_item_index (td, s));
			} else {
				/* defer allocation to execution-time */
				ADD_CODE (td, MINT_LDSTR_TOKEN);
				ADD_CODE (td, get_data_item_index (td, GUINT_TO_POINTER (token)));
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

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				m = (MonoMethod *)mono_method_get_wrapper_data (method, token);
			else  {
				m = mono_get_method_checked (image, token, NULL, generic_context, error);
				goto_if_nok (error, exit);
			}

			csignature = mono_method_signature_internal (m);
			klass = m->klass;

			if (!mono_class_init (klass)) {
				mono_error_set_for_class_failure (error, klass);
				goto_if_nok (error, exit);
			}

			if (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_ABSTRACT) {
				char* full_name = mono_type_get_full_name (klass);
				mono_error_set_member_access (error, "Cannot create an abstract class: %s", full_name);
				g_free (full_name);
				goto_if_nok (error, exit);
			}

			td->sp -= csignature->param_count;
			if (mono_class_is_magic_int (klass) || mono_class_is_magic_float (klass)) {
#if SIZEOF_VOID_P == 8
				if (mono_class_is_magic_int (klass) && td->sp [0].type == STACK_TYPE_I4)
					ADD_CODE (td, MINT_CONV_I8_I4);
				else if (mono_class_is_magic_float (klass) && td->sp [0].type == STACK_TYPE_R4)
					ADD_CODE (td, MINT_CONV_R8_R4);
#endif
				ADD_CODE (td, MINT_NEWOBJ_MAGIC);
				ADD_CODE (td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
				goto_if_nok (error, exit);

				PUSH_TYPE (td, stack_type [mint_type (m_class_get_byval_arg (klass))], klass);
			} else {
				if (m_class_get_parent (klass) == mono_defaults.array_class) {
					ADD_CODE(td, MINT_NEWOBJ_ARRAY);
					ADD_CODE(td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
					ADD_CODE(td, csignature->param_count);
				} else if (m_class_get_image (klass) == mono_defaults.corlib &&
						!strcmp (m_class_get_name (m->klass), "ByReference`1") &&
						!strcmp (m->name, ".ctor")) {
					/* public ByReference(ref T value) */
					g_assert (csignature->hasthis && csignature->param_count == 1);
					ADD_CODE(td, MINT_INTRINS_BYREFERENCE_CTOR);
					ADD_CODE(td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
				} else if (klass != mono_defaults.string_class &&
						!mono_class_is_marshalbyref (klass) &&
						!mono_class_has_finalizer (klass) &&
						!m_class_has_weak_fields (klass)) {
					if (!m_class_is_valuetype (klass))
						ADD_CODE(td, MINT_NEWOBJ_FAST);
					else if (mint_type (m_class_get_byval_arg (klass)) == MINT_TYPE_VT)
						ADD_CODE(td, MINT_NEWOBJ_VTST_FAST);
					else
						ADD_CODE(td, MINT_NEWOBJ_VT_FAST);

					ADD_CODE(td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
					ADD_CODE(td, csignature->param_count);

					if (!m_class_is_valuetype (klass)) {
						MonoVTable *vtable = mono_class_vtable_checked (domain, klass, error);
						goto_if_nok (error, exit);
						ADD_CODE(td, get_data_item_index (td, vtable));
					} else if (mint_type (m_class_get_byval_arg (klass)) == MINT_TYPE_VT) {
						ADD_CODE(td, mono_class_value_size (klass, NULL));
					}
				} else {
					ADD_CODE(td, MINT_NEWOBJ);
					ADD_CODE(td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
				}
				goto_if_nok (error, exit);

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
				if (vt_stack_used != 0 || vt_res_size != 0) {
					ADD_CODE(td, MINT_VTRESULT);
					ADD_CODE(td, vt_res_size);
					WRITE32(td, &vt_stack_used);
					td->vt_sp -= vt_stack_used;
				}
				PUSH_TYPE (td, stack_type [mint_type (m_class_get_byval_arg (klass))], klass);
			}
			break;
		}
		case CEE_CASTCLASS:
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			ADD_CODE(td, MINT_CASTCLASS);
			ADD_CODE(td, get_data_item_index (td, klass));
			td->sp [-1].klass = klass;
			td->ip += 5;
			break;
		case CEE_ISINST:
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
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
			else {
				klass = mono_class_get_and_inflate_typespec_checked (image, token, generic_context, error);
				goto_if_nok (error, exit);
			}

			if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method;
				if (m_class_is_enumtype (mono_class_get_nullable_param (klass)))
					target_method = mono_class_get_method_from_name_checked (klass, "UnboxExact", 1, 0, error);
				else
					target_method = mono_class_get_method_from_name_checked (klass, "Unbox", 1, 0, error);
				goto_if_nok (error, exit);
				/* td->ip is incremented by interp_transform_call */
				interp_transform_call (td, method, target_method, domain, generic_context, is_bb_start, body_start_offset, NULL, FALSE, error, FALSE);
				goto_if_nok (error, exit);
				/*
				 * CEE_UNBOX needs to push address of vtype while Nullable.Unbox returns the value type
				 * We create a local variable in the frame so that we can fetch its address.
				 */
				int local_offset = create_interp_local (td, m_class_get_byval_arg (klass));
				store_local_general (td, local_offset, m_class_get_byval_arg (klass));
				ADD_CODE (td, MINT_LDLOCA_S);
				ADD_CODE (td, local_offset);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_MP);
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
			CHECK_TYPELOAD (klass);

			if (mini_type_is_reference (m_class_get_byval_arg (klass))) {
				int mt = mint_type (m_class_get_byval_arg (klass));
				ADD_CODE (td, MINT_CASTCLASS);
				ADD_CODE (td, get_data_item_index (td, klass));
				SET_TYPE (td->sp - 1, stack_type [mt], klass);
				td->ip += 5;
			} else if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method;
				if (m_class_is_enumtype (mono_class_get_nullable_param (klass)))
					target_method = mono_class_get_method_from_name_checked (klass, "UnboxExact", 1, 0, error);
				else
					target_method = mono_class_get_method_from_name_checked (klass, "Unbox", 1, 0, error);
				goto_if_nok (error, exit);
				/* td->ip is incremented by interp_transform_call */
				interp_transform_call (td, method, target_method, domain, generic_context, is_bb_start, body_start_offset, NULL, FALSE, error, FALSE);

				goto_if_nok (error, exit);
			} else {
				int mt = mint_type (m_class_get_byval_arg (klass));
				ADD_CODE (td, MINT_UNBOX);
				ADD_CODE (td, get_data_item_index (td, klass));

				ADD_CODE (td, (mt == MINT_TYPE_VT) ? MINT_LDOBJ_VT: MINT_LDOBJ);
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
			td->sp = td->stack;
			break;
		case CEE_LDFLDA: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			mono_class_init (klass);
#ifndef DISABLE_REMOTING
			if (m_class_get_marshalbyref (klass) || mono_class_is_contextbound (klass) || klass == mono_defaults.marshalbyrefobject_class) {
				g_assert (!is_static);
				int offset = m_class_is_valuetype (klass) ? field->offset - MONO_ABI_SIZEOF (MonoObject) : field->offset;

				ADD_CODE (td, MINT_MONO_LDPTR);
				ADD_CODE (td, get_data_item_index (td, klass));
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
				ADD_CODE (td, MINT_MONO_LDPTR);
				ADD_CODE (td, get_data_item_index (td, field));
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
				ADD_CODE (td, MINT_LDC_I4);
				WRITE32 (td, &offset);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_I4);
#if SIZEOF_VOID_P == 8
				ADD_CODE(td, MINT_CONV_I8_I4);
#endif

				MonoMethod *wrapper = mono_marshal_get_ldflda_wrapper (field->type);
				/* td->ip is incremented by interp_transform_call */
				interp_transform_call (td, method, wrapper, domain, generic_context, is_bb_start, body_start_offset, NULL, FALSE, error, FALSE);
				goto_if_nok (error, exit);
			} else
#endif
			{
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
					ADD_CODE (td, m_class_is_valuetype (klass) ? field->offset - MONO_ABI_SIZEOF (MonoObject) : field->offset);
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
			mono_class_init (klass);

			MonoClass *field_klass = mono_class_from_mono_type_internal (ftype);
			mt = mint_type (m_class_get_byval_arg (field_klass));
#ifndef DISABLE_REMOTING
			if (m_class_get_marshalbyref (klass)) {
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
					ADD_CODE (td, m_class_is_valuetype (klass) ? field->offset - MONO_ABI_SIZEOF (MonoObject) : field->offset);
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
				size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
				int field_vt_size = 0;
				if (mt == MINT_TYPE_VT) {
					/*
					 * Pop the loaded field from the vtstack (it will still be present
					 * at the same vtstack address) and we will load it in place of the
					 * containing valuetype with the second MINT_VTRESULT.
					 */
					field_vt_size = mono_class_value_size (field_klass, NULL);
					field_vt_size = ALIGN_TO (field_vt_size, MINT_VT_ALIGNMENT);
					ADD_CODE (td, MINT_VTRESULT);
					ADD_CODE (td, 0);
					WRITE32 (td, &field_vt_size);
				}
				td->vt_sp -= size;
				ADD_CODE (td, MINT_VTRESULT);
				ADD_CODE (td, field_vt_size);
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
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			mono_class_init (klass);
			mt = mint_type (ftype);

			BARRIER_IF_VOLATILE (td);

#ifndef DISABLE_REMOTING
			if (m_class_get_marshalbyref (klass)) {
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

					/* the vtable of the field might not be initialized at this point */
					MonoClass *fld_klass = mono_class_from_mono_type_internal (field->type);
					mono_class_vtable_checked (domain, fld_klass, error);
					goto_if_nok (error, exit);
				} else {
					ADD_CODE (td, MINT_STFLD_I1 + mt - MINT_TYPE_I1);
					ADD_CODE (td, m_class_is_valuetype (klass) ? field->offset - MONO_ABI_SIZEOF (MonoObject) : field->offset);
					if (mt == MINT_TYPE_VT) {
						ADD_CODE (td, get_data_item_index (td, field));

						/* the vtable of the field might not be initialized at this point */
						MonoClass *fld_klass = mono_class_from_mono_type_internal (field->type);
						mono_class_vtable_checked (domain, fld_klass, error);
						goto_if_nok (error, exit);
					}
				}
			}
			if (mt == MINT_TYPE_VT) {
				MonoClass *klass = mono_class_from_mono_type_internal (ftype);
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
			goto_if_nok (error, exit);
			mono_field_get_type_internal (field);
			ADD_CODE(td, MINT_LDSFLDA);
			ADD_CODE(td, get_data_item_index (td, field));
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
			klass = NULL;
			if (mt == MINT_TYPE_VT) {
				ADD_CODE(td, MINT_LDSFLD_VT);
				ADD_CODE(td, get_data_item_index (td, field));
				klass = mono_class_from_mono_type_internal (ftype);
				int size = mono_class_value_size (klass, NULL);
				PUSH_VT(td, size);
				WRITE32(td, &size);
			} else {
				if (mono_class_field_is_special_static (field)) {
					ADD_CODE(td, MINT_LDSFLD);
					ADD_CODE(td, get_data_item_index (td, field));
				} else {
					MonoVTable *vtable = mono_class_vtable_checked (domain, field->parent, error);
					goto_if_nok (error, exit);

					ADD_CODE(td, MINT_LDSFLD_I1 + mt - MINT_TYPE_I1);
					ADD_CODE(td, get_data_item_index (td, vtable));
					ADD_CODE(td, get_data_item_index (td, (char*)mono_vtable_get_static_field_data (vtable) + field->offset));
				}
				if (mt == MINT_TYPE_O) 
					klass = mono_class_from_mono_type_internal (ftype);
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

			/* the vtable of the field might not be initialized at this point */
			MonoClass *fld_klass = mono_class_from_mono_type_internal (field->type);
			mono_class_vtable_checked (domain, fld_klass, error);
			goto_if_nok (error, exit);

			if (mt == MINT_TYPE_VT) {
				MonoClass *klass = mono_class_from_mono_type_internal (ftype);
				int size = mono_class_value_size (klass, NULL);
				ADD_CODE(td, MINT_STSFLD_VT);
				ADD_CODE(td, get_data_item_index (td, field));
				POP_VT (td, size);
			} else {
				if (mono_class_field_is_special_static (field)) {
					ADD_CODE(td, MINT_STSFLD);
					ADD_CODE(td, get_data_item_index (td, field));
				} else {
					MonoVTable *vtable = mono_class_vtable_checked (domain, field->parent, error);
					goto_if_nok (error, exit);

					ADD_CODE(td, MINT_STSFLD_I1 + mt - MINT_TYPE_I1);
					ADD_CODE(td, get_data_item_index (td, vtable));
					ADD_CODE(td, get_data_item_index (td, (char*)mono_vtable_get_static_field_data (vtable) + field->offset));
				}

			}
			td->ip += 5;
			--td->sp;
			break;
		}
		case CEE_STOBJ: {
			int size;
			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

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
			CHECK_TYPELOAD (klass);

			if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method = mono_class_get_method_from_name_checked (klass, "Box", 1, 0, error);
				goto_if_nok (error, exit);
				/* td->ip is incremented by interp_transform_call */
				interp_transform_call (td, method, target_method, domain, generic_context, is_bb_start, body_start_offset, NULL, FALSE, error, FALSE);
				goto_if_nok (error, exit);
			} else if (!m_class_is_valuetype (klass)) {
				/* already boxed, do nothing. */
				td->ip += 5;
			} else {
				if (G_UNLIKELY (m_class_is_byreflike (klass))) {
					mono_error_set_bad_image (error, image, "Cannot box IsByRefLike type '%s.%s'", m_class_get_name_space (klass), m_class_get_name (klass));
					goto exit;
				}
				if (mint_type (m_class_get_byval_arg (klass)) == MINT_TYPE_VT && !m_class_is_enumtype (klass)) {
					size = mono_class_value_size (klass, NULL);
					size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
					td->vt_sp -= size;
				} else if (td->sp [-1].type == STACK_TYPE_R8 && m_class_get_byval_arg (klass)->type == MONO_TYPE_R4) {
					ADD_CODE (td, MINT_CONV_R4_R8);
				}
				if (mint_type (m_class_get_byval_arg (klass)) == MINT_TYPE_VT)
					ADD_CODE (td, MINT_BOX_VT);
				else
					ADD_CODE (td, MINT_BOX);
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
			CHECK_TYPELOAD (klass);

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
#ifdef MONO_BIG_ARRAYS
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I8);
#else
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
#endif
			break;
		case CEE_LDELEMA:
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
					ADD_CODE (td, get_data_item_index (td, klass));
					WRITE32 (td, &size);
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
					ADD_CODE (td, get_data_item_index (td, klass));
					WRITE32 (td, &size);
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

			ADD_CODE (td, MINT_MKREFANY);
			ADD_CODE (td, get_data_item_index (td, klass));

			td->ip += 5;
			PUSH_VT (td, sizeof (MonoTypedRef));
			SET_TYPE(td->sp - 1, STACK_TYPE_VT, mono_defaults.typed_reference_class);
			break;
		case CEE_REFANYVAL: {
			CHECK_STACK (td, 1);

			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			ADD_CODE (td, MINT_REFANYVAL);
			ADD_CODE (td, get_data_item_index (td, klass));

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
			case STACK_TYPE_R8:
				ADD_CODE(td, is_un ? MINT_CONV_OVF_I1_UN_R8 : MINT_CONV_OVF_I1_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, is_un ? MINT_CONV_OVF_I1_U4 : MINT_CONV_OVF_I1_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, is_un ? MINT_CONV_OVF_I1_U8 : MINT_CONV_OVF_I1_I8);
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
		case CEE_CONV_OVF_I2_UN: {
			gboolean is_un = *td->ip == CEE_CONV_OVF_I2_UN;
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				ADD_CODE(td, is_un ? MINT_CONV_OVF_I2_UN_R8 : MINT_CONV_OVF_I2_R8);
				break;
			case STACK_TYPE_I4:
				ADD_CODE(td, is_un ? MINT_CONV_OVF_I2_U4 : MINT_CONV_OVF_I2_I4);
				break;
			case STACK_TYPE_I8:
				ADD_CODE(td, is_un ? MINT_CONV_OVF_I2_U8 : MINT_CONV_OVF_I2_I8);
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
			case STACK_TYPE_R4:
				ADD_CODE(td, MINT_CONV_OVF_I4_R4);
				break;
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
			case STACK_TYPE_R4:
				ADD_CODE(td, MINT_CONV_OVF_U4_R4);
				break;
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
			case STACK_TYPE_R4:
				ADD_CODE(td, MINT_CONV_OVF_I8_R4);
				break;
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
			case STACK_TYPE_R4:
				ADD_CODE(td, MINT_CONV_OVF_U8_R4);
				break;
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
					handle = m_class_get_byval_arg ((MonoClass *) handle);

				if (generic_context) {
					handle = mono_class_inflate_generic_type_checked ((MonoType*)handle, generic_context, error);
					goto_if_nok (error, exit);
				}
			} else {
				handle = mono_ldtoken_checked (image, token, &klass, generic_context, error);
				goto_if_nok (error, exit);
			}
			mono_class_init (klass);
			mt = mint_type (m_class_get_byval_arg (klass));
			g_assert (mt == MINT_TYPE_VT);
			size = mono_class_value_size (klass, NULL);
			g_assert (size == sizeof(gpointer));

			const unsigned char *next_ip = td->ip + 5;
			MonoMethod *cmethod;
			if (next_ip < end &&
					!is_bb_start [next_ip - td->il_code] &&
					(*next_ip == CEE_CALL || *next_ip == CEE_CALLVIRT) &&
					(cmethod = mono_get_method_checked (image, read32 (next_ip + 1), NULL, generic_context, error)) &&
					(cmethod->klass == mono_defaults.systemtype_class) &&
					(strcmp (cmethod->name, "GetTypeFromHandle") == 0)) {
				ADD_CODE (td, MINT_MONO_LDPTR);
				gpointer systype = mono_type_get_object_checked (domain, (MonoType*)handle, error);
				goto_if_nok (error, exit);
				ADD_CODE (td, get_data_item_index (td, systype));
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_MP);
				td->ip = next_ip + 5;
			} else {
				PUSH_VT (td, sizeof(gpointer));
				ADD_CODE (td, MINT_LDTOKEN);
				ADD_CODE (td, get_data_item_index (td, handle));
				SET_TYPE (td->sp, stack_type [mt], klass);
				td->sp++;
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
			ADD_CODE (td, td->clause_indexes [in_offset]);
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
					ADD_CODE (td, MINT_LD_DELEGATE_METHOD_PTR);
					PUSH_SIMPLE_TYPE (td, STACK_TYPE_I);
					break;
				case CEE_MONO_CALLI_EXTRA_ARG:
					/* Same as CEE_CALLI, except that we drop the extra arg required for llvm specific behaviour */
					ADD_CODE (td, MINT_POP);
					ADD_CODE (td, 1);
					--td->sp;
					interp_transform_call (td, method, NULL, domain, generic_context, is_bb_start, body_start_offset, NULL, FALSE, error, FALSE);
					goto_if_nok (error, exit);
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
					int icall_op;

					token = read32 (td->ip + 1);
					td->ip += 5;
					func = mono_method_get_wrapper_data (method, token);
					info = mono_find_jit_icall_by_addr (func);
					g_assert (info);

					CHECK_STACK (td, info->sig->param_count);
					if (!strcmp (info->name, "mono_threads_attach_coop")) {
						rtm->needs_thread_attach = 1;

						/* attach needs two arguments, and has one return value: leave one element on the stack */
						ADD_CODE (td, MINT_POP);
						ADD_CODE (td, 0);
					} else if (!strcmp (info->name, "mono_threads_detach_coop")) {
						g_assert (rtm->needs_thread_attach);

						/* detach consumes two arguments, and no return value: drop both of them */
						ADD_CODE (td, MINT_POP);
						ADD_CODE (td, 0);
						ADD_CODE (td, MINT_POP);
						ADD_CODE (td, 0);
					} else {
						icall_op = interp_icall_op_for_sig (info->sig);
						g_assert (icall_op != -1);

						ADD_CODE(td, icall_op);
						ADD_CODE(td, get_data_item_index (td, func));
					}
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
				size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
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
				g_assert(m_class_is_valuetype (klass));
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
			case CEE_ARGLIST:
				ADD_CODE (td, MINT_ARGLIST);
				PUSH_VT (td, SIZEOF_VOID_P);
				PUSH_SIMPLE_TYPE (td, STACK_TYPE_VT);
				++td->ip;
				break;
			case CEE_CEQ:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP) {
					ADD_CODE(td, MINT_CEQ_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				} else {
					if (td->sp [-1].type == STACK_TYPE_R4 && td->sp [-2].type == STACK_TYPE_R8)
						ADD_CODE (td, MINT_CONV_R8_R4);
					if (td->sp [-1].type == STACK_TYPE_R8 && td->sp [-2].type == STACK_TYPE_R4)
						ADD_CODE (td, MINT_CONV_R8_R4_SP);
					ADD_CODE(td, MINT_CEQ_I4 + td->sp [-1].type - STACK_TYPE_I4);
				}
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
				else {
					m = mono_get_method_checked (image, token, NULL, generic_context, error);
					goto_if_nok (error, exit);
				}

				if (!mono_method_can_access_method (method, m))
					interp_generate_mae_throw (td, method, m);

				if (method->wrapper_type == MONO_WRAPPER_NONE && m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
					m = mono_marshal_get_synchronized_wrapper (m);

				ADD_CODE(td, *td->ip == CEE_LDFTN ? MINT_LDFTN : MINT_LDVIRTFTN);
				ADD_CODE(td, get_data_item_index (td, mono_interp_get_imethod (domain, m, error)));
				goto_if_nok (error, exit);
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
				CHECK_TYPELOAD (klass);
				if (m_class_is_valuetype (klass)) {
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
				CHECK_TYPELOAD (constrained_class);
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
		Reloc *reloc = (Reloc*)g_ptr_array_index (td->relocs, i);

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
		g_print ("Runtime method: %s %p, VT stack size: %d\n", mono_method_full_name (method, TRUE), rtm, td->max_vt_sp);
		g_print ("Calculated stack size: %d, stated size: %d\n", td->max_stack_height, header->max_stack);
		dump_mint_code (td);
	}

	/* Check if we use excessive stack space */
	if (td->max_stack_height > header->max_stack * 3)
		g_warning ("Excessive stack space usage for method %s, %d/%d", method->name, td->max_stack_height, header->max_stack);

	int code_len;
	code_len = td->new_ip - td->new_code;

	rtm->clauses = (MonoExceptionClause*)mono_domain_alloc0 (domain, header->num_clauses * sizeof (MonoExceptionClause));
	memcpy (rtm->clauses, header->clauses, header->num_clauses * sizeof(MonoExceptionClause));
	rtm->code = (gushort*)mono_domain_alloc0 (domain, (td->new_ip - td->new_code) * sizeof (gushort));
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
	rtm->stack_size = (sizeof (stackval)) * (td->max_stack_height + 2); /* + 1 for returns of called functions  + 1 for 0-ing in trace*/
	rtm->stack_size = ALIGN_TO (rtm->stack_size, MINT_VT_ALIGNMENT);
	rtm->vt_stack_size = td->max_vt_sp;
	rtm->total_locals_size = td->total_locals_size;
	rtm->alloca_size = rtm->total_locals_size + rtm->args_size + rtm->vt_stack_size + rtm->stack_size;
	rtm->data_items = (gpointer*)mono_domain_alloc0 (domain, td->n_data_items * sizeof (td->data_items [0]));
	memcpy (rtm->data_items, td->data_items, td->n_data_items * sizeof (td->data_items [0]));

	/* Save debug info */
	interp_save_debug_info (rtm, header, td, line_numbers);

	/* Create a MonoJitInfo for the interpreted method by creating the interpreter IR as the native code. */
	int jinfo_len;
	jinfo_len = mono_jit_info_size ((MonoJitInfoFlags)0, header->num_clauses, 0);
	MonoJitInfo *jinfo;
	jinfo = (MonoJitInfo *)mono_domain_alloc0 (domain, jinfo_len);
	jinfo->is_interp = 1;
	rtm->jinfo = jinfo;
	mono_jit_info_init (jinfo, method, (guint8*)rtm->code, code_len, (MonoJitInfoFlags)0, header->num_clauses, 0);
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

exit:
	mono_basic_block_free (original_bb);
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

void
mono_interp_transform_method (InterpMethod *imethod, ThreadContext *context, MonoError *error)
{
	int i, align, size, offset;
	MonoMethod *method = imethod->method;
	MonoImage *image = m_class_get_image (method->klass);
	MonoMethodHeader *header = NULL;
	MonoMethodSignature *signature = mono_method_signature_internal (method);
	const unsigned char *ip, *end;
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
		mono_os_mutex_lock (&calc_section);
		if (imethod->transformed) {
			mono_os_mutex_unlock (&calc_section);
			MONO_PROFILER_RAISE (jit_done, (method, imethod->jinfo));
			return;
		}

		/* assumes all internal calls with an array this are built in... */
		if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL && (! mono_method_signature_internal (method)->hasthis || m_class_get_rank (method->klass) == 0)) {
			nm = mono_marshal_get_native_wrapper (method, TRUE, FALSE);
			signature = mono_method_signature_internal (nm);
		} else {
			const char *name = method->name;
			if (m_class_get_parent (method->klass) == mono_defaults.multicastdelegate_class) {
				if (*name == '.' && (strcmp (name, ".ctor") == 0)) {
					MonoJitICallInfo *mi = mono_find_jit_icall_by_name ("ves_icall_mono_delegate_ctor_interp");
					g_assert (mi);
					char *wrapper_name = g_strdup_printf ("__icall_wrapper_%s", mi->name);
					nm = mono_marshal_get_icall_wrapper (mi->sig, wrapper_name, mi->func, TRUE);
					g_free (wrapper_name);
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
			imethod->stack_size = sizeof (stackval); /* for tracing */
			imethod->alloca_size = imethod->stack_size;
			imethod->transformed = TRUE;
			mono_os_mutex_unlock(&calc_section);
			MONO_PROFILER_RAISE (jit_done, (method, NULL));
			return;
		}
		method = nm;
		header = interp_method_get_header (nm, error);
		mono_os_mutex_unlock (&calc_section);
		return_if_nok (error);
	}

	if (!header) {
		header = mono_method_get_header_checked (method, error);
		return_if_nok (error);
	}

	g_assert ((signature->param_count + signature->hasthis) < 1000);
	g_assert (header->max_stack < 10000);
	/* intern the strings in the method. */
	ip = header->code;
	end = ip + header->code_size;

	is_bb_start = (guint8*)g_malloc0(header->code_size);
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
			if (method->wrapper_type == MONO_WRAPPER_NONE) {
				mono_ldstr_checked (domain, image, mono_metadata_token_index (read32 (ip + 1)), error);
				if (!is_ok (error)) {
					g_free (is_bb_start);
					mono_metadata_free_mh (header);
					return;
				}
			}
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
				m = mono_get_method_checked (image, read32 (ip + 1), NULL, generic_context, error);
				if (!is_ok (error)) {
					g_free (is_bb_start);
					mono_metadata_free_mh (header);
					return;
				}
				mono_class_init (m->klass);
				if (!mono_class_is_interface (m->klass)) {
					mono_class_vtable_checked (domain, m->klass, error);
					if (!is_ok (error)) {
						g_free (is_bb_start);
						mono_metadata_free_mh (header);
						return;
					}
				}
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

	imethod->local_offsets = (guint32*)g_malloc (header->num_locals * sizeof(guint32));
	offset = 0;
	for (i = 0; i < header->num_locals; ++i) {
		size = mono_type_size (header->locals [i], &align);
		offset += align - 1;
		offset &= ~(align - 1);
		imethod->local_offsets [i] = offset;
		offset += size;
	}
	offset = (offset + 7) & ~7;

	imethod->exvar_offsets = (guint32*)g_malloc (header->num_clauses * sizeof (guint32));
	for (i = 0; i < header->num_clauses; i++) {
		imethod->exvar_offsets [i] = offset;
		offset += sizeof (MonoObject*);
	}
	offset = (offset + 7) & ~7;

	imethod->locals_size = offset;
	g_assert (imethod->locals_size < 65536);
	offset = 0;
	imethod->arg_offsets = (guint32*)g_malloc ((!!signature->hasthis + signature->param_count) * sizeof(guint32));

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

	generate (method, header, imethod, is_bb_start, generic_context, error);

	mono_metadata_free_mh (header);
	g_free (is_bb_start);

	return_if_nok (error);

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
		mono_atomic_fetch_add_i32 (&mono_jit_stats.methods_with_interp, 1);
	}
	mono_os_mutex_unlock (&calc_section);
}

