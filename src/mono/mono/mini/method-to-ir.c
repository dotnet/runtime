/*
 * method-to-ir.c: Convert CIL to the JIT internal representation
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2003-2010 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */

#include <config.h>

#ifndef DISABLE_JIT

#include <signal.h>

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#include <math.h>
#include <string.h>
#include <ctype.h>

#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#include <mono/utils/memcheck.h>
#include "mini.h"
#include <mono/metadata/abi-details.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/attrdefs.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/metadata/mono-basic-block.h>

#include "trace.h"

#include "ir-emit.h"

#include "jit-icalls.h"
#include "jit.h"
#include "debugger-agent.h"

#define BRANCH_COST 10
#define INLINE_LENGTH_LIMIT 20

/* These have 'cfg' as an implicit argument */
#define INLINE_FAILURE(msg) do {									\
	if ((cfg->method != cfg->current_method) && (cfg->current_method->wrapper_type == MONO_WRAPPER_NONE)) { \
		inline_failure (cfg, msg);										\
		goto exception_exit;											\
	} \
	} while (0)
#define CHECK_CFG_EXCEPTION do {\
		if (cfg->exception_type != MONO_EXCEPTION_NONE)	\
			goto exception_exit;						\
	} while (0)
#define METHOD_ACCESS_FAILURE(method, cmethod) do {			\
		method_access_failure ((cfg), (method), (cmethod));			\
		goto exception_exit;										\
	} while (0)
#define FIELD_ACCESS_FAILURE(method, field) do {					\
		field_access_failure ((cfg), (method), (field));			\
		goto exception_exit;	\
	} while (0)
#define GENERIC_SHARING_FAILURE(opcode) do {		\
		if (cfg->gshared) {									\
			gshared_failure (cfg, opcode, __FILE__, __LINE__);	\
			goto exception_exit;	\
		}			\
	} while (0)
#define GSHAREDVT_FAILURE(opcode) do {		\
	if (cfg->gsharedvt) {												\
		gsharedvt_failure (cfg, opcode, __FILE__, __LINE__);			\
		goto exception_exit;											\
	}																	\
	} while (0)
#define OUT_OF_MEMORY_FAILURE do {	\
		mono_cfg_set_exception (cfg, MONO_EXCEPTION_OUT_OF_MEMORY);		\
		goto exception_exit;	\
	} while (0)
#define DISABLE_AOT(cfg) do { \
		if ((cfg)->verbose_level >= 2)						  \
			printf ("AOT disabled: %s:%d\n", __FILE__, __LINE__);	\
		(cfg)->disable_aot = TRUE;							  \
	} while (0)
#define LOAD_ERROR do { \
		break_on_unverified ();								\
		mono_cfg_set_exception (cfg, MONO_EXCEPTION_TYPE_LOAD); \
		goto exception_exit;									\
	} while (0)

#define TYPE_LOAD_ERROR(klass) do { \
		cfg->exception_ptr = klass; \
		LOAD_ERROR;					\
	} while (0)

#define CHECK_CFG_ERROR do {\
		if (!mono_error_ok (&cfg->error)) { \
			mono_cfg_set_exception (cfg, MONO_EXCEPTION_MONO_ERROR);	\
			goto mono_error_exit; \
		} \
	} while (0)

/* Determine whenever 'ins' represents a load of the 'this' argument */
#define MONO_CHECK_THIS(ins) (mono_method_signature (cfg->method)->hasthis && ((ins)->opcode == OP_MOVE) && ((ins)->sreg1 == cfg->args [0]->dreg))

static int ldind_to_load_membase (int opcode);
static int stind_to_store_membase (int opcode);

int mono_op_to_op_imm (int opcode);
int mono_op_to_op_imm_noemul (int opcode);

MONO_API MonoInst* mono_emit_native_call (MonoCompile *cfg, gconstpointer func, MonoMethodSignature *sig, MonoInst **args);

static int inline_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **sp,
						  guchar *ip, guint real_offset, gboolean inline_always, MonoBasicBlock **out_cbb);

/* helper methods signatures */
static MonoMethodSignature *helper_sig_class_init_trampoline;
static MonoMethodSignature *helper_sig_domain_get;
static MonoMethodSignature *helper_sig_generic_class_init_trampoline;
static MonoMethodSignature *helper_sig_generic_class_init_trampoline_llvm;
static MonoMethodSignature *helper_sig_rgctx_lazy_fetch_trampoline;
static MonoMethodSignature *helper_sig_monitor_enter_exit_trampoline;
static MonoMethodSignature *helper_sig_monitor_enter_exit_trampoline_llvm;

/*
 * Instruction metadata
 */
#ifdef MINI_OP
#undef MINI_OP
#endif
#ifdef MINI_OP3
#undef MINI_OP3
#endif
#define MINI_OP(a,b,dest,src1,src2) dest, src1, src2, ' ',
#define MINI_OP3(a,b,dest,src1,src2,src3) dest, src1, src2, src3,
#define NONE ' '
#define IREG 'i'
#define FREG 'f'
#define VREG 'v'
#define XREG 'x'
#if SIZEOF_REGISTER == 8 && SIZEOF_REGISTER == SIZEOF_VOID_P
#define LREG IREG
#else
#define LREG 'l'
#endif
/* keep in sync with the enum in mini.h */
const char
ins_info[] = {
#include "mini-ops.h"
};
#undef MINI_OP
#undef MINI_OP3

#define MINI_OP(a,b,dest,src1,src2) ((src2) != NONE ? 2 : ((src1) != NONE ? 1 : 0)),
#define MINI_OP3(a,b,dest,src1,src2,src3) ((src3) != NONE ? 3 : ((src2) != NONE ? 2 : ((src1) != NONE ? 1 : 0))),
/* 
 * This should contain the index of the last sreg + 1. This is not the same
 * as the number of sregs for opcodes like IA64_CMP_EQ_IMM.
 */
const gint8 ins_sreg_counts[] = {
#include "mini-ops.h"
};
#undef MINI_OP
#undef MINI_OP3

#define MONO_INIT_VARINFO(vi,id) do { \
	(vi)->range.first_use.pos.bid = 0xffff; \
	(vi)->reg = -1; \
	(vi)->idx = (id); \
} while (0)

guint32
mono_alloc_ireg (MonoCompile *cfg)
{
	return alloc_ireg (cfg);
}

guint32
mono_alloc_lreg (MonoCompile *cfg)
{
	return alloc_lreg (cfg);
}

guint32
mono_alloc_freg (MonoCompile *cfg)
{
	return alloc_freg (cfg);
}

guint32
mono_alloc_preg (MonoCompile *cfg)
{
	return alloc_preg (cfg);
}

guint32
mono_alloc_dreg (MonoCompile *cfg, MonoStackType stack_type)
{
	return alloc_dreg (cfg, stack_type);
}

/*
 * mono_alloc_ireg_ref:
 *
 *   Allocate an IREG, and mark it as holding a GC ref.
 */
guint32
mono_alloc_ireg_ref (MonoCompile *cfg)
{
	return alloc_ireg_ref (cfg);
}

/*
 * mono_alloc_ireg_mp:
 *
 *   Allocate an IREG, and mark it as holding a managed pointer.
 */
guint32
mono_alloc_ireg_mp (MonoCompile *cfg)
{
	return alloc_ireg_mp (cfg);
}

/*
 * mono_alloc_ireg_copy:
 *
 *   Allocate an IREG with the same GC type as VREG.
 */
guint32
mono_alloc_ireg_copy (MonoCompile *cfg, guint32 vreg)
{
	if (vreg_is_ref (cfg, vreg))
		return alloc_ireg_ref (cfg);
	else if (vreg_is_mp (cfg, vreg))
		return alloc_ireg_mp (cfg);
	else
		return alloc_ireg (cfg);
}

guint
mono_type_to_regmove (MonoCompile *cfg, MonoType *type)
{
	if (type->byref)
		return OP_MOVE;

	type = mini_replace_type (type);
handle_enum:
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return OP_MOVE;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return OP_MOVE;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return OP_MOVE;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return OP_MOVE;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return OP_MOVE;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
#if SIZEOF_REGISTER == 8
		return OP_MOVE;
#else
		return OP_LMOVE;
#endif
	case MONO_TYPE_R4:
		return OP_FMOVE;
	case MONO_TYPE_R8:
		return OP_FMOVE;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = mono_class_enum_basetype (type->data.klass);
			goto handle_enum;
		}
		if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type (type)))
			return OP_XMOVE;
		return OP_VMOVE;
	case MONO_TYPE_TYPEDBYREF:
		return OP_VMOVE;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		goto handle_enum;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (cfg->generic_sharing_context);
		if (mini_type_var_is_vt (cfg, type))
			return OP_VMOVE;
		else
			return OP_MOVE;
	default:
		g_error ("unknown type 0x%02x in type_to_regstore", type->type);
	}
	return -1;
}

void
mono_print_bb (MonoBasicBlock *bb, const char *msg)
{
	int i;
	MonoInst *tree;

	printf ("\n%s %d: [IN: ", msg, bb->block_num);
	for (i = 0; i < bb->in_count; ++i)
		printf (" BB%d(%d)", bb->in_bb [i]->block_num, bb->in_bb [i]->dfn);
	printf (", OUT: ");
	for (i = 0; i < bb->out_count; ++i)
		printf (" BB%d(%d)", bb->out_bb [i]->block_num, bb->out_bb [i]->dfn);
	printf (" ]\n");
	for (tree = bb->code; tree; tree = tree->next)
		mono_print_ins_index (-1, tree);
}

void
mono_create_helper_signatures (void)
{
	helper_sig_domain_get = mono_create_icall_signature ("ptr");
	helper_sig_class_init_trampoline = mono_create_icall_signature ("void");
	helper_sig_generic_class_init_trampoline = mono_create_icall_signature ("void");
	helper_sig_generic_class_init_trampoline_llvm = mono_create_icall_signature ("void ptr");
	helper_sig_rgctx_lazy_fetch_trampoline = mono_create_icall_signature ("ptr ptr");
	helper_sig_monitor_enter_exit_trampoline = mono_create_icall_signature ("void");
	helper_sig_monitor_enter_exit_trampoline_llvm = mono_create_icall_signature ("void object");
}

static MONO_NEVER_INLINE void
break_on_unverified (void)
{
	if (mini_get_debug_options ()->break_on_unverified)
		G_BREAKPOINT ();
}

static MONO_NEVER_INLINE void
method_access_failure (MonoCompile *cfg, MonoMethod *method, MonoMethod *cil_method)
{
	char *method_fname = mono_method_full_name (method, TRUE);
	char *cil_method_fname = mono_method_full_name (cil_method, TRUE);
	mono_cfg_set_exception (cfg, MONO_EXCEPTION_METHOD_ACCESS);
	cfg->exception_message = g_strdup_printf ("Method `%s' is inaccessible from method `%s'\n", cil_method_fname, method_fname);
	g_free (method_fname);
	g_free (cil_method_fname);
}

static MONO_NEVER_INLINE void
field_access_failure (MonoCompile *cfg, MonoMethod *method, MonoClassField *field)
{
	char *method_fname = mono_method_full_name (method, TRUE);
	char *field_fname = mono_field_full_name (field);
	mono_cfg_set_exception (cfg, MONO_EXCEPTION_FIELD_ACCESS);
	cfg->exception_message = g_strdup_printf ("Field `%s' is inaccessible from method `%s'\n", field_fname, method_fname);
	g_free (method_fname);
	g_free (field_fname);
}

static MONO_NEVER_INLINE void
inline_failure (MonoCompile *cfg, const char *msg)
{
	if (cfg->verbose_level >= 2)
		printf ("inline failed: %s\n", msg);
	mono_cfg_set_exception (cfg, MONO_EXCEPTION_INLINE_FAILED);
}

static MONO_NEVER_INLINE void
gshared_failure (MonoCompile *cfg, int opcode, const char *file, int line)
{
	if (cfg->verbose_level > 2)											\
		printf ("sharing failed for method %s.%s.%s/%d opcode %s line %d\n", cfg->current_method->klass->name_space, cfg->current_method->klass->name, cfg->current_method->name, cfg->current_method->signature->param_count, mono_opcode_name ((opcode)), __LINE__);
	mono_cfg_set_exception (cfg, MONO_EXCEPTION_GENERIC_SHARING_FAILED);
}

static MONO_NEVER_INLINE void
gsharedvt_failure (MonoCompile *cfg, int opcode, const char *file, int line)
{
	cfg->exception_message = g_strdup_printf ("gsharedvt failed for method %s.%s.%s/%d opcode %s %s:%d", cfg->current_method->klass->name_space, cfg->current_method->klass->name, cfg->current_method->name, cfg->current_method->signature->param_count, mono_opcode_name ((opcode)), file, line);
	if (cfg->verbose_level >= 2)
		printf ("%s\n", cfg->exception_message);
	mono_cfg_set_exception (cfg, MONO_EXCEPTION_GENERIC_SHARING_FAILED);
}

/*
 * When using gsharedvt, some instatiations might be verifiable, and some might be not. i.e. 
 * foo<T> (int i) { ldarg.0; box T; }
 */
#define UNVERIFIED do { \
	if (cfg->gsharedvt) { \
		if (cfg->verbose_level > 2)									\
			printf ("gsharedvt method failed to verify, falling back to instantiation.\n"); \
		mono_cfg_set_exception (cfg, MONO_EXCEPTION_GENERIC_SHARING_FAILED); \
		goto exception_exit;											\
	}																	\
	break_on_unverified ();												\
	goto unverified;													\
} while (0)

#define GET_BBLOCK(cfg,tblock,ip) do {	\
		(tblock) = cfg->cil_offset_to_bb [(ip) - cfg->cil_start]; \
		if (!(tblock)) {	\
			if ((ip) >= end || (ip) < header->code) UNVERIFIED; \
            NEW_BBLOCK (cfg, (tblock)); \
			(tblock)->cil_code = (ip);	\
			ADD_BBLOCK (cfg, (tblock));	\
		} \
	} while (0)

#if defined(TARGET_X86) || defined(TARGET_AMD64)
#define EMIT_NEW_X86_LEA(cfg,dest,sr1,sr2,shift,imm) do { \
		MONO_INST_NEW (cfg, dest, OP_X86_LEA); \
		(dest)->dreg = alloc_ireg_mp ((cfg)); \
		(dest)->sreg1 = (sr1); \
		(dest)->sreg2 = (sr2); \
		(dest)->inst_imm = (imm); \
		(dest)->backend.shift_amount = (shift); \
		MONO_ADD_INS ((cfg)->cbb, (dest)); \
	} while (0)
#endif

#if SIZEOF_REGISTER == 8
#define ADD_WIDEN_OP(ins, arg1, arg2) do { \
		/* FIXME: Need to add many more cases */ \
		if ((arg1)->type == STACK_PTR && (arg2)->type == STACK_I4) {	\
			MonoInst *widen; \
			int dr = alloc_preg (cfg); \
			EMIT_NEW_UNALU (cfg, widen, OP_SEXT_I4, dr, (arg2)->dreg); \
			(ins)->sreg2 = widen->dreg; \
		} \
	} while (0)
#else
#define ADD_WIDEN_OP(ins, arg1, arg2)
#endif

#define ADD_BINOP(op) do {	\
		MONO_INST_NEW (cfg, ins, (op));	\
		sp -= 2;	\
		ins->sreg1 = sp [0]->dreg;	\
		ins->sreg2 = sp [1]->dreg;	\
		type_from_op (ins, sp [0], sp [1]);	\
		CHECK_TYPE (ins);	\
		/* Have to insert a widening op */		 \
        ADD_WIDEN_OP (ins, sp [0], sp [1]); \
        ins->dreg = alloc_dreg ((cfg), (ins)->type); \
        MONO_ADD_INS ((cfg)->cbb, (ins)); \
        *sp++ = mono_decompose_opcode ((cfg), (ins)); \
	} while (0)

#define ADD_UNOP(op) do {	\
		MONO_INST_NEW (cfg, ins, (op));	\
		sp--;	\
		ins->sreg1 = sp [0]->dreg;	\
		type_from_op (ins, sp [0], NULL);	\
		CHECK_TYPE (ins);	\
        (ins)->dreg = alloc_dreg ((cfg), (ins)->type); \
        MONO_ADD_INS ((cfg)->cbb, (ins)); \
		*sp++ = mono_decompose_opcode (cfg, ins); \
	} while (0)

#define ADD_BINCOND(next_block) do {	\
		MonoInst *cmp;	\
		sp -= 2; \
		MONO_INST_NEW(cfg, cmp, OP_COMPARE);	\
		cmp->sreg1 = sp [0]->dreg;	\
		cmp->sreg2 = sp [1]->dreg;	\
		type_from_op (cmp, sp [0], sp [1]);	\
		CHECK_TYPE (cmp);	\
		type_from_op (ins, sp [0], sp [1]);	\
		ins->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof(gpointer)*2);	\
		GET_BBLOCK (cfg, tblock, target);		\
		link_bblock (cfg, bblock, tblock);	\
		ins->inst_true_bb = tblock;	\
		if ((next_block)) {	\
			link_bblock (cfg, bblock, (next_block));	\
			ins->inst_false_bb = (next_block);	\
			start_new_bblock = 1;	\
		} else {	\
			GET_BBLOCK (cfg, tblock, ip);		\
			link_bblock (cfg, bblock, tblock);	\
			ins->inst_false_bb = tblock;	\
			start_new_bblock = 2;	\
		}	\
		if (sp != stack_start) {									\
		    handle_stack_args (cfg, stack_start, sp - stack_start); \
			CHECK_UNVERIFIABLE (cfg); \
		} \
        MONO_ADD_INS (bblock, cmp); \
		MONO_ADD_INS (bblock, ins);	\
	} while (0)

/* *
 * link_bblock: Links two basic blocks
 *
 * links two basic blocks in the control flow graph, the 'from'
 * argument is the starting block and the 'to' argument is the block
 * the control flow ends to after 'from'.
 */
static void
link_bblock (MonoCompile *cfg, MonoBasicBlock *from, MonoBasicBlock* to)
{
	MonoBasicBlock **newa;
	int i, found;

#if 0
	if (from->cil_code) {
		if (to->cil_code)
			printf ("edge from IL%04x to IL_%04x\n", from->cil_code - cfg->cil_code, to->cil_code - cfg->cil_code);
		else
			printf ("edge from IL%04x to exit\n", from->cil_code - cfg->cil_code);
	} else {
		if (to->cil_code)
			printf ("edge from entry to IL_%04x\n", to->cil_code - cfg->cil_code);
		else
			printf ("edge from entry to exit\n");
	}
#endif

	found = FALSE;
	for (i = 0; i < from->out_count; ++i) {
		if (to == from->out_bb [i]) {
			found = TRUE;
			break;
		}
	}
	if (!found) {
		newa = mono_mempool_alloc (cfg->mempool, sizeof (gpointer) * (from->out_count + 1));
		for (i = 0; i < from->out_count; ++i) {
			newa [i] = from->out_bb [i];
		}
		newa [i] = to;
		from->out_count++;
		from->out_bb = newa;
	}

	found = FALSE;
	for (i = 0; i < to->in_count; ++i) {
		if (from == to->in_bb [i]) {
			found = TRUE;
			break;
		}
	}
	if (!found) {
		newa = mono_mempool_alloc (cfg->mempool, sizeof (gpointer) * (to->in_count + 1));
		for (i = 0; i < to->in_count; ++i) {
			newa [i] = to->in_bb [i];
		}
		newa [i] = from;
		to->in_count++;
		to->in_bb = newa;
	}
}

void
mono_link_bblock (MonoCompile *cfg, MonoBasicBlock *from, MonoBasicBlock* to)
{
	link_bblock (cfg, from, to);
}

/**
 * mono_find_block_region:
 *
 *   We mark each basic block with a region ID. We use that to avoid BB
 *   optimizations when blocks are in different regions.
 *
 * Returns:
 *   A region token that encodes where this region is, and information
 *   about the clause owner for this block.
 *
 *   The region encodes the try/catch/filter clause that owns this block
 *   as well as the type.  -1 is a special value that represents a block
 *   that is in none of try/catch/filter.
 */
static int
mono_find_block_region (MonoCompile *cfg, int offset)
{
	MonoMethodHeader *header = cfg->header;
	MonoExceptionClause *clause;
	int i;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if ((clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) && (offset >= clause->data.filter_offset) &&
		    (offset < (clause->handler_offset)))
			return ((i + 1) << 8) | MONO_REGION_FILTER | clause->flags;
			   
		if (MONO_OFFSET_IN_HANDLER (clause, offset)) {
			if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY)
				return ((i + 1) << 8) | MONO_REGION_FINALLY | clause->flags;
			else if (clause->flags == MONO_EXCEPTION_CLAUSE_FAULT)
				return ((i + 1) << 8) | MONO_REGION_FAULT | clause->flags;
			else
				return ((i + 1) << 8) | MONO_REGION_CATCH | clause->flags;
		}

		if (MONO_OFFSET_IN_CLAUSE (clause, offset))
			return ((i + 1) << 8) | clause->flags;
	}

	return -1;
}

static GList*
mono_find_final_block (MonoCompile *cfg, unsigned char *ip, unsigned char *target, int type)
{
	MonoMethodHeader *header = cfg->header;
	MonoExceptionClause *clause;
	int i;
	GList *res = NULL;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, (ip - header->code)) && 
		    (!MONO_OFFSET_IN_CLAUSE (clause, (target - header->code)))) {
			if (clause->flags == type)
				res = g_list_append (res, clause);
		}
	}
	return res;
}

static void
mono_create_spvar_for_region (MonoCompile *cfg, int region)
{
	MonoInst *var;

	var = g_hash_table_lookup (cfg->spvars, GINT_TO_POINTER (region));
	if (var)
		return;

	var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
	/* prevent it from being register allocated */
	var->flags |= MONO_INST_VOLATILE;

	g_hash_table_insert (cfg->spvars, GINT_TO_POINTER (region), var);
}

MonoInst *
mono_find_exvar_for_offset (MonoCompile *cfg, int offset)
{
	return g_hash_table_lookup (cfg->exvars, GINT_TO_POINTER (offset));
}

static MonoInst*
mono_create_exvar_for_offset (MonoCompile *cfg, int offset)
{
	MonoInst *var;

	var = g_hash_table_lookup (cfg->exvars, GINT_TO_POINTER (offset));
	if (var)
		return var;

	var = mono_compile_create_var (cfg, &mono_defaults.object_class->byval_arg, OP_LOCAL);
	/* prevent it from being register allocated */
	var->flags |= MONO_INST_VOLATILE;

	g_hash_table_insert (cfg->exvars, GINT_TO_POINTER (offset), var);

	return var;
}

/*
 * Returns the type used in the eval stack when @type is loaded.
 * FIXME: return a MonoType/MonoClass for the byref and VALUETYPE cases.
 */
void
type_to_eval_stack_type (MonoCompile *cfg, MonoType *type, MonoInst *inst)
{
	MonoClass *klass;

	type = mini_replace_type (type);
	inst->klass = klass = mono_class_from_mono_type (type);
	if (type->byref) {
		inst->type = STACK_MP;
		return;
	}

handle_enum:
	switch (type->type) {
	case MONO_TYPE_VOID:
		inst->type = STACK_INV;
		return;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		inst->type = STACK_I4;
		return;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		inst->type = STACK_PTR;
		return;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		inst->type = STACK_OBJ;
		return;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		inst->type = STACK_I8;
		return;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		inst->type = STACK_R8;
		return;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = mono_class_enum_basetype (type->data.klass);
			goto handle_enum;
		} else {
			inst->klass = klass;
			inst->type = STACK_VTYPE;
			return;
		}
	case MONO_TYPE_TYPEDBYREF:
		inst->klass = mono_defaults.typed_reference_class;
		inst->type = STACK_VTYPE;
		return;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		goto handle_enum;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (cfg->generic_sharing_context);
		if (mini_is_gsharedvt_type (cfg, type)) {
			g_assert (cfg->gsharedvt);
			inst->type = STACK_VTYPE;
		} else {
			inst->type = STACK_OBJ;
		}
		return;
	default:
		g_error ("unknown type 0x%02x in eval stack type", type->type);
	}
}

/*
 * The following tables are used to quickly validate the IL code in type_from_op ().
 */
static const char
bin_num_table [STACK_MAX] [STACK_MAX] = {
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_I4,  STACK_INV, STACK_PTR, STACK_INV, STACK_MP,  STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_I8,  STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_PTR, STACK_INV, STACK_PTR, STACK_INV, STACK_MP,  STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_R8,  STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_MP,  STACK_INV, STACK_MP,  STACK_INV, STACK_PTR, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV}
};

static const char 
neg_table [] = {
	STACK_INV, STACK_I4, STACK_I8, STACK_PTR, STACK_R8, STACK_INV, STACK_INV, STACK_INV
};

/* reduce the size of this table */
static const char
bin_int_table [STACK_MAX] [STACK_MAX] = {
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_I4,  STACK_INV, STACK_PTR, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_I8,  STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_PTR, STACK_INV, STACK_PTR, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV}
};

static const char
bin_comp_table [STACK_MAX] [STACK_MAX] = {
/*	Inv i  L  p  F  &  O  vt */
	{0},
	{0, 1, 0, 1, 0, 0, 0, 0}, /* i, int32 */
	{0, 0, 1, 0, 0, 0, 0, 0}, /* L, int64 */
	{0, 1, 0, 1, 0, 2, 4, 0}, /* p, ptr */
	{0, 0, 0, 0, 1, 0, 0, 0}, /* F, R8 */
	{0, 0, 0, 2, 0, 1, 0, 0}, /* &, managed pointer */
	{0, 0, 0, 4, 0, 0, 3, 0}, /* O, reference */
	{0, 0, 0, 0, 0, 0, 0, 0}, /* vt value type */
};

/* reduce the size of this table */
static const char
shift_table [STACK_MAX] [STACK_MAX] = {
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_I4,  STACK_INV, STACK_I4,  STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_I8,  STACK_INV, STACK_I8,  STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_PTR, STACK_INV, STACK_PTR, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV}
};

/*
 * Tables to map from the non-specific opcode to the matching
 * type-specific opcode.
 */
/* handles from CEE_ADD to CEE_SHR_UN (CEE_REM_UN for floats) */
static const guint16
binops_op_map [STACK_MAX] = {
	0, OP_IADD-CEE_ADD, OP_LADD-CEE_ADD, OP_PADD-CEE_ADD, OP_FADD-CEE_ADD, OP_PADD-CEE_ADD
};

/* handles from CEE_NEG to CEE_CONV_U8 */
static const guint16
unops_op_map [STACK_MAX] = {
	0, OP_INEG-CEE_NEG, OP_LNEG-CEE_NEG, OP_PNEG-CEE_NEG, OP_FNEG-CEE_NEG, OP_PNEG-CEE_NEG
};

/* handles from CEE_CONV_U2 to CEE_SUB_OVF_UN */
static const guint16
ovfops_op_map [STACK_MAX] = {
	0, OP_ICONV_TO_U2-CEE_CONV_U2, OP_LCONV_TO_U2-CEE_CONV_U2, OP_PCONV_TO_U2-CEE_CONV_U2, OP_FCONV_TO_U2-CEE_CONV_U2, OP_PCONV_TO_U2-CEE_CONV_U2, OP_PCONV_TO_U2-CEE_CONV_U2
};

/* handles from CEE_CONV_OVF_I1_UN to CEE_CONV_OVF_U_UN */
static const guint16
ovf2ops_op_map [STACK_MAX] = {
	0, OP_ICONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN, OP_LCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN, OP_PCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN, OP_FCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN, OP_PCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN
};

/* handles from CEE_CONV_OVF_I1 to CEE_CONV_OVF_U8 */
static const guint16
ovf3ops_op_map [STACK_MAX] = {
	0, OP_ICONV_TO_OVF_I1-CEE_CONV_OVF_I1, OP_LCONV_TO_OVF_I1-CEE_CONV_OVF_I1, OP_PCONV_TO_OVF_I1-CEE_CONV_OVF_I1, OP_FCONV_TO_OVF_I1-CEE_CONV_OVF_I1, OP_PCONV_TO_OVF_I1-CEE_CONV_OVF_I1
};

/* handles from CEE_BEQ to CEE_BLT_UN */
static const guint16
beqops_op_map [STACK_MAX] = {
	0, OP_IBEQ-CEE_BEQ, OP_LBEQ-CEE_BEQ, OP_PBEQ-CEE_BEQ, OP_FBEQ-CEE_BEQ, OP_PBEQ-CEE_BEQ, OP_PBEQ-CEE_BEQ
};

/* handles from CEE_CEQ to CEE_CLT_UN */
static const guint16
ceqops_op_map [STACK_MAX] = {
	0, OP_ICEQ-OP_CEQ, OP_LCEQ-OP_CEQ, OP_PCEQ-OP_CEQ, OP_FCEQ-OP_CEQ, OP_PCEQ-OP_CEQ, OP_PCEQ-OP_CEQ
};

/*
 * Sets ins->type (the type on the eval stack) according to the
 * type of the opcode and the arguments to it.
 * Invalid IL code is marked by setting ins->type to the invalid value STACK_INV.
 *
 * FIXME: this function sets ins->type unconditionally in some cases, but
 * it should set it to invalid for some types (a conv.x on an object)
 */
static void
type_from_op (MonoInst *ins, MonoInst *src1, MonoInst *src2) {

	switch (ins->opcode) {
	/* binops */
	case CEE_ADD:
	case CEE_SUB:
	case CEE_MUL:
	case CEE_DIV:
	case CEE_REM:
		/* FIXME: check unverifiable args for STACK_MP */
		ins->type = bin_num_table [src1->type] [src2->type];
		ins->opcode += binops_op_map [ins->type];
		break;
	case CEE_DIV_UN:
	case CEE_REM_UN:
	case CEE_AND:
	case CEE_OR:
	case CEE_XOR:
		ins->type = bin_int_table [src1->type] [src2->type];
		ins->opcode += binops_op_map [ins->type];
		break;
	case CEE_SHL:
	case CEE_SHR:
	case CEE_SHR_UN:
		ins->type = shift_table [src1->type] [src2->type];
		ins->opcode += binops_op_map [ins->type];
		break;
	case OP_COMPARE:
	case OP_LCOMPARE:
	case OP_ICOMPARE:
		ins->type = bin_comp_table [src1->type] [src2->type] ? STACK_I4: STACK_INV;
		if ((src1->type == STACK_I8) || ((SIZEOF_VOID_P == 8) && ((src1->type == STACK_PTR) || (src1->type == STACK_OBJ) || (src1->type == STACK_MP))))
			ins->opcode = OP_LCOMPARE;
		else if (src1->type == STACK_R8)
			ins->opcode = OP_FCOMPARE;
		else
			ins->opcode = OP_ICOMPARE;
		break;
	case OP_ICOMPARE_IMM:
		ins->type = bin_comp_table [src1->type] [src1->type] ? STACK_I4 : STACK_INV;
		if ((src1->type == STACK_I8) || ((SIZEOF_VOID_P == 8) && ((src1->type == STACK_PTR) || (src1->type == STACK_OBJ) || (src1->type == STACK_MP))))
			ins->opcode = OP_LCOMPARE_IMM;		
		break;
	case CEE_BEQ:
	case CEE_BGE:
	case CEE_BGT:
	case CEE_BLE:
	case CEE_BLT:
	case CEE_BNE_UN:
	case CEE_BGE_UN:
	case CEE_BGT_UN:
	case CEE_BLE_UN:
	case CEE_BLT_UN:
		ins->opcode += beqops_op_map [src1->type];
		break;
	case OP_CEQ:
		ins->type = bin_comp_table [src1->type] [src2->type] ? STACK_I4: STACK_INV;
		ins->opcode += ceqops_op_map [src1->type];
		break;
	case OP_CGT:
	case OP_CGT_UN:
	case OP_CLT:
	case OP_CLT_UN:
		ins->type = (bin_comp_table [src1->type] [src2->type] & 1) ? STACK_I4: STACK_INV;
		ins->opcode += ceqops_op_map [src1->type];
		break;
	/* unops */
	case CEE_NEG:
		ins->type = neg_table [src1->type];
		ins->opcode += unops_op_map [ins->type];
		break;
	case CEE_NOT:
		if (src1->type >= STACK_I4 && src1->type <= STACK_PTR)
			ins->type = src1->type;
		else
			ins->type = STACK_INV;
		ins->opcode += unops_op_map [ins->type];
		break;
	case CEE_CONV_I1:
	case CEE_CONV_I2:
	case CEE_CONV_I4:
	case CEE_CONV_U4:
		ins->type = STACK_I4;
		ins->opcode += unops_op_map [src1->type];
		break;
	case CEE_CONV_R_UN:
		ins->type = STACK_R8;
		switch (src1->type) {
		case STACK_I4:
		case STACK_PTR:
			ins->opcode = OP_ICONV_TO_R_UN;
			break;
		case STACK_I8:
			ins->opcode = OP_LCONV_TO_R_UN; 
			break;
		}
		break;
	case CEE_CONV_OVF_I1:
	case CEE_CONV_OVF_U1:
	case CEE_CONV_OVF_I2:
	case CEE_CONV_OVF_U2:
	case CEE_CONV_OVF_I4:
	case CEE_CONV_OVF_U4:
		ins->type = STACK_I4;
		ins->opcode += ovf3ops_op_map [src1->type];
		break;
	case CEE_CONV_OVF_I_UN:
	case CEE_CONV_OVF_U_UN:
		ins->type = STACK_PTR;
		ins->opcode += ovf2ops_op_map [src1->type];
		break;
	case CEE_CONV_OVF_I1_UN:
	case CEE_CONV_OVF_I2_UN:
	case CEE_CONV_OVF_I4_UN:
	case CEE_CONV_OVF_U1_UN:
	case CEE_CONV_OVF_U2_UN:
	case CEE_CONV_OVF_U4_UN:
		ins->type = STACK_I4;
		ins->opcode += ovf2ops_op_map [src1->type];
		break;
	case CEE_CONV_U:
		ins->type = STACK_PTR;
		switch (src1->type) {
		case STACK_I4:
			ins->opcode = OP_ICONV_TO_U;
			break;
		case STACK_PTR:
		case STACK_MP:
#if SIZEOF_VOID_P == 8
			ins->opcode = OP_LCONV_TO_U;
#else
			ins->opcode = OP_MOVE;
#endif
			break;
		case STACK_I8:
			ins->opcode = OP_LCONV_TO_U;
			break;
		case STACK_R8:
			ins->opcode = OP_FCONV_TO_U;
			break;
		}
		break;
	case CEE_CONV_I8:
	case CEE_CONV_U8:
		ins->type = STACK_I8;
		ins->opcode += unops_op_map [src1->type];
		break;
	case CEE_CONV_OVF_I8:
	case CEE_CONV_OVF_U8:
		ins->type = STACK_I8;
		ins->opcode += ovf3ops_op_map [src1->type];
		break;
	case CEE_CONV_OVF_U8_UN:
	case CEE_CONV_OVF_I8_UN:
		ins->type = STACK_I8;
		ins->opcode += ovf2ops_op_map [src1->type];
		break;
	case CEE_CONV_R4:
	case CEE_CONV_R8:
		ins->type = STACK_R8;
		ins->opcode += unops_op_map [src1->type];
		break;
	case OP_CKFINITE:
		ins->type = STACK_R8;		
		break;
	case CEE_CONV_U2:
	case CEE_CONV_U1:
		ins->type = STACK_I4;
		ins->opcode += ovfops_op_map [src1->type];
		break;
	case CEE_CONV_I:
	case CEE_CONV_OVF_I:
	case CEE_CONV_OVF_U:
		ins->type = STACK_PTR;
		ins->opcode += ovfops_op_map [src1->type];
		break;
	case CEE_ADD_OVF:
	case CEE_ADD_OVF_UN:
	case CEE_MUL_OVF:
	case CEE_MUL_OVF_UN:
	case CEE_SUB_OVF:
	case CEE_SUB_OVF_UN:
		ins->type = bin_num_table [src1->type] [src2->type];
		ins->opcode += ovfops_op_map [src1->type];
		if (ins->type == STACK_R8)
			ins->type = STACK_INV;
		break;
	case OP_LOAD_MEMBASE:
		ins->type = STACK_PTR;
		break;
	case OP_LOADI1_MEMBASE:
	case OP_LOADU1_MEMBASE:
	case OP_LOADI2_MEMBASE:
	case OP_LOADU2_MEMBASE:
	case OP_LOADI4_MEMBASE:
	case OP_LOADU4_MEMBASE:
		ins->type = STACK_PTR;
		break;
	case OP_LOADI8_MEMBASE:
		ins->type = STACK_I8;
		break;
	case OP_LOADR4_MEMBASE:
	case OP_LOADR8_MEMBASE:
		ins->type = STACK_R8;
		break;
	default:
		g_error ("opcode 0x%04x not handled in type from op", ins->opcode);
		break;
	}

	if (ins->type == STACK_MP)
		ins->klass = mono_defaults.object_class;
}

static const char 
ldind_type [] = {
	STACK_I4, STACK_I4, STACK_I4, STACK_I4, STACK_I4, STACK_I4, STACK_I8, STACK_PTR, STACK_R8, STACK_R8, STACK_OBJ
};

#if 0

static const char
param_table [STACK_MAX] [STACK_MAX] = {
	{0},
};

static int
check_values_to_signature (MonoInst *args, MonoType *this, MonoMethodSignature *sig) {
	int i;

	if (sig->hasthis) {
		switch (args->type) {
		case STACK_I4:
		case STACK_I8:
		case STACK_R8:
		case STACK_VTYPE:
		case STACK_INV:
			return 0;
		}
		args++;
	}
	for (i = 0; i < sig->param_count; ++i) {
		switch (args [i].type) {
		case STACK_INV:
			return 0;
		case STACK_MP:
			if (!sig->params [i]->byref)
				return 0;
			continue;
		case STACK_OBJ:
			if (sig->params [i]->byref)
				return 0;
			switch (sig->params [i]->type) {
			case MONO_TYPE_CLASS:
			case MONO_TYPE_STRING:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_ARRAY:
				break;
			default:
				return 0;
			}
			continue;
		case STACK_R8:
			if (sig->params [i]->byref)
				return 0;
			if (sig->params [i]->type != MONO_TYPE_R4 && sig->params [i]->type != MONO_TYPE_R8)
				return 0;
			continue;
		case STACK_PTR:
		case STACK_I4:
		case STACK_I8:
		case STACK_VTYPE:
			break;
		}
		/*if (!param_table [args [i].type] [sig->params [i]->type])
			return 0;*/
	}
	return 1;
}
#endif

/*
 * When we need a pointer to the current domain many times in a method, we
 * call mono_domain_get() once and we store the result in a local variable.
 * This function returns the variable that represents the MonoDomain*.
 */
inline static MonoInst *
mono_get_domainvar (MonoCompile *cfg)
{
	if (!cfg->domainvar)
		cfg->domainvar = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
	return cfg->domainvar;
}

/*
 * The got_var contains the address of the Global Offset Table when AOT 
 * compiling.
 */
MonoInst *
mono_get_got_var (MonoCompile *cfg)
{
#ifdef MONO_ARCH_NEED_GOT_VAR
	if (!cfg->compile_aot)
		return NULL;
	if (!cfg->got_var) {
		cfg->got_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
	}
	return cfg->got_var;
#else
	return NULL;
#endif
}

static MonoInst *
mono_get_vtable_var (MonoCompile *cfg)
{
	g_assert (cfg->generic_sharing_context);

	if (!cfg->rgctx_var) {
		cfg->rgctx_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
		/* force the var to be stack allocated */
		cfg->rgctx_var->flags |= MONO_INST_VOLATILE;
	}

	return cfg->rgctx_var;
}

static MonoType*
type_from_stack_type (MonoInst *ins) {
	switch (ins->type) {
	case STACK_I4: return &mono_defaults.int32_class->byval_arg;
	case STACK_I8: return &mono_defaults.int64_class->byval_arg;
	case STACK_PTR: return &mono_defaults.int_class->byval_arg;
	case STACK_R8: return &mono_defaults.double_class->byval_arg;
	case STACK_MP:
		return &ins->klass->this_arg;
	case STACK_OBJ: return &mono_defaults.object_class->byval_arg;
	case STACK_VTYPE: return &ins->klass->byval_arg;
	default:
		g_error ("stack type %d to monotype not handled\n", ins->type);
	}
	return NULL;
}

static G_GNUC_UNUSED int
type_to_stack_type (MonoType *t)
{
	t = mono_type_get_underlying_type (t);
	switch (t->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return STACK_I4;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return STACK_PTR;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return STACK_OBJ;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return STACK_I8;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		return STACK_R8;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_TYPEDBYREF:
		return STACK_VTYPE;
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (t))
			return STACK_VTYPE;
		else
			return STACK_OBJ;
		break;
	default:
		g_assert_not_reached ();
	}

	return -1;
}

static MonoClass*
array_access_to_klass (int opcode)
{
	switch (opcode) {
	case CEE_LDELEM_U1:
		return mono_defaults.byte_class;
	case CEE_LDELEM_U2:
		return mono_defaults.uint16_class;
	case CEE_LDELEM_I:
	case CEE_STELEM_I:
		return mono_defaults.int_class;
	case CEE_LDELEM_I1:
	case CEE_STELEM_I1:
		return mono_defaults.sbyte_class;
	case CEE_LDELEM_I2:
	case CEE_STELEM_I2:
		return mono_defaults.int16_class;
	case CEE_LDELEM_I4:
	case CEE_STELEM_I4:
		return mono_defaults.int32_class;
	case CEE_LDELEM_U4:
		return mono_defaults.uint32_class;
	case CEE_LDELEM_I8:
	case CEE_STELEM_I8:
		return mono_defaults.int64_class;
	case CEE_LDELEM_R4:
	case CEE_STELEM_R4:
		return mono_defaults.single_class;
	case CEE_LDELEM_R8:
	case CEE_STELEM_R8:
		return mono_defaults.double_class;
	case CEE_LDELEM_REF:
	case CEE_STELEM_REF:
		return mono_defaults.object_class;
	default:
		g_assert_not_reached ();
	}
	return NULL;
}

/*
 * We try to share variables when possible
 */
static MonoInst *
mono_compile_get_interface_var (MonoCompile *cfg, int slot, MonoInst *ins)
{
	MonoInst *res;
	int pos, vnum;

	/* inlining can result in deeper stacks */ 
	if (slot >= cfg->header->max_stack)
		return mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);

	pos = ins->type - 1 + slot * STACK_MAX;

	switch (ins->type) {
	case STACK_I4:
	case STACK_I8:
	case STACK_R8:
	case STACK_PTR:
	case STACK_MP:
	case STACK_OBJ:
		if ((vnum = cfg->intvars [pos]))
			return cfg->varinfo [vnum];
		res = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
		cfg->intvars [pos] = res->inst_c0;
		break;
	default:
		res = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
	}
	return res;
}

static void
mono_save_token_info (MonoCompile *cfg, MonoImage *image, guint32 token, gpointer key)
{
	/* 
	 * Don't use this if a generic_context is set, since that means AOT can't
	 * look up the method using just the image+token.
	 * table == 0 means this is a reference made from a wrapper.
	 */
	if (cfg->compile_aot && !cfg->generic_context && (mono_metadata_token_table (token) > 0)) {
		MonoJumpInfoToken *jump_info_token = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoJumpInfoToken));
		jump_info_token->image = image;
		jump_info_token->token = token;
		g_hash_table_insert (cfg->token_info_hash, key, jump_info_token);
	}
}

/*
 * This function is called to handle items that are left on the evaluation stack
 * at basic block boundaries. What happens is that we save the values to local variables
 * and we reload them later when first entering the target basic block (with the
 * handle_loaded_temps () function).
 * A single joint point will use the same variables (stored in the array bb->out_stack or
 * bb->in_stack, if the basic block is before or after the joint point).
 *
 * This function needs to be called _before_ emitting the last instruction of
 * the bb (i.e. before emitting a branch).
 * If the stack merge fails at a join point, cfg->unverifiable is set.
 */
static void
handle_stack_args (MonoCompile *cfg, MonoInst **sp, int count)
{
	int i, bindex;
	MonoBasicBlock *bb = cfg->cbb;
	MonoBasicBlock *outb;
	MonoInst *inst, **locals;
	gboolean found;

	if (!count)
		return;
	if (cfg->verbose_level > 3)
		printf ("%d item(s) on exit from B%d\n", count, bb->block_num);
	if (!bb->out_scount) {
		bb->out_scount = count;
		//printf ("bblock %d has out:", bb->block_num);
		found = FALSE;
		for (i = 0; i < bb->out_count; ++i) {
			outb = bb->out_bb [i];
			/* exception handlers are linked, but they should not be considered for stack args */
			if (outb->flags & BB_EXCEPTION_HANDLER)
				continue;
			//printf (" %d", outb->block_num);
			if (outb->in_stack) {
				found = TRUE;
				bb->out_stack = outb->in_stack;
				break;
			}
		}
		//printf ("\n");
		if (!found) {
			bb->out_stack = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * count);
			for (i = 0; i < count; ++i) {
				/* 
				 * try to reuse temps already allocated for this purpouse, if they occupy the same
				 * stack slot and if they are of the same type.
				 * This won't cause conflicts since if 'local' is used to 
				 * store one of the values in the in_stack of a bblock, then
				 * the same variable will be used for the same outgoing stack 
				 * slot as well. 
				 * This doesn't work when inlining methods, since the bblocks
				 * in the inlined methods do not inherit their in_stack from
				 * the bblock they are inlined to. See bug #58863 for an
				 * example.
				 */
				if (cfg->inlined_method)
					bb->out_stack [i] = mono_compile_create_var (cfg, type_from_stack_type (sp [i]), OP_LOCAL);
				else
					bb->out_stack [i] = mono_compile_get_interface_var (cfg, i, sp [i]);
			}
		}
	}

	for (i = 0; i < bb->out_count; ++i) {
		outb = bb->out_bb [i];
		/* exception handlers are linked, but they should not be considered for stack args */
		if (outb->flags & BB_EXCEPTION_HANDLER)
			continue;
		if (outb->in_scount) {
			if (outb->in_scount != bb->out_scount) {
				cfg->unverifiable = TRUE;
				return;
			}
			continue; /* check they are the same locals */
		}
		outb->in_scount = count;
		outb->in_stack = bb->out_stack;
	}

	locals = bb->out_stack;
	cfg->cbb = bb;
	for (i = 0; i < count; ++i) {
		EMIT_NEW_TEMPSTORE (cfg, inst, locals [i]->inst_c0, sp [i]);
		inst->cil_code = sp [i]->cil_code;
		sp [i] = locals [i];
		if (cfg->verbose_level > 3)
			printf ("storing %d to temp %d\n", i, (int)locals [i]->inst_c0);
	}

	/*
	 * It is possible that the out bblocks already have in_stack assigned, and
	 * the in_stacks differ. In this case, we will store to all the different 
	 * in_stacks.
	 */

	found = TRUE;
	bindex = 0;
	while (found) {
		/* Find a bblock which has a different in_stack */
		found = FALSE;
		while (bindex < bb->out_count) {
			outb = bb->out_bb [bindex];
			/* exception handlers are linked, but they should not be considered for stack args */
			if (outb->flags & BB_EXCEPTION_HANDLER) {
				bindex++;
				continue;
			}
			if (outb->in_stack != locals) {
				for (i = 0; i < count; ++i) {
					EMIT_NEW_TEMPSTORE (cfg, inst, outb->in_stack [i]->inst_c0, sp [i]);
					inst->cil_code = sp [i]->cil_code;
					sp [i] = locals [i];
					if (cfg->verbose_level > 3)
						printf ("storing %d to temp %d\n", i, (int)outb->in_stack [i]->inst_c0);
				}
				locals = outb->in_stack;
				found = TRUE;
				break;
			}
			bindex ++;
		}
	}
}

/* Emit code which loads interface_offsets [klass->interface_id]
 * The array is stored in memory before vtable.
*/
static void
mini_emit_load_intf_reg_vtable (MonoCompile *cfg, int intf_reg, int vtable_reg, MonoClass *klass)
{
	if (cfg->compile_aot) {
		int ioffset_reg = alloc_preg (cfg);
		int iid_reg = alloc_preg (cfg);

		MONO_EMIT_NEW_AOTCONST (cfg, iid_reg, klass, MONO_PATCH_INFO_ADJUSTED_IID);
		MONO_EMIT_NEW_BIALU (cfg, OP_PADD, ioffset_reg, iid_reg, vtable_reg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, intf_reg, ioffset_reg, 0);
	}
	else {
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, intf_reg, vtable_reg, -((klass->interface_id + 1) * SIZEOF_VOID_P));
	}
}

static void
mini_emit_interface_bitmap_check (MonoCompile *cfg, int intf_bit_reg, int base_reg, int offset, MonoClass *klass)
{
	int ibitmap_reg = alloc_preg (cfg);
#ifdef COMPRESSED_INTERFACE_BITMAP
	MonoInst *args [2];
	MonoInst *res, *ins;
	NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, ibitmap_reg, base_reg, offset);
	MONO_ADD_INS (cfg->cbb, ins);
	args [0] = ins;
	if (cfg->compile_aot)
		EMIT_NEW_AOTCONST (cfg, args [1], MONO_PATCH_INFO_IID, klass);
	else
		EMIT_NEW_ICONST (cfg, args [1], klass->interface_id);
	res = mono_emit_jit_icall (cfg, mono_class_interface_match, args);
	MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, intf_bit_reg, res->dreg);
#else
	int ibitmap_byte_reg = alloc_preg (cfg);

	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, ibitmap_reg, base_reg, offset);

	if (cfg->compile_aot) {
		int iid_reg = alloc_preg (cfg);
		int shifted_iid_reg = alloc_preg (cfg);
		int ibitmap_byte_address_reg = alloc_preg (cfg);
		int masked_iid_reg = alloc_preg (cfg);
		int iid_one_bit_reg = alloc_preg (cfg);
		int iid_bit_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_AOTCONST (cfg, iid_reg, klass, MONO_PATCH_INFO_IID);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHR_IMM, shifted_iid_reg, iid_reg, 3);
		MONO_EMIT_NEW_BIALU (cfg, OP_PADD, ibitmap_byte_address_reg, ibitmap_reg, shifted_iid_reg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, ibitmap_byte_reg, ibitmap_byte_address_reg, 0);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IAND_IMM, masked_iid_reg, iid_reg, 7);
		MONO_EMIT_NEW_ICONST (cfg, iid_one_bit_reg, 1);
		MONO_EMIT_NEW_BIALU (cfg, OP_ISHL, iid_bit_reg, iid_one_bit_reg, masked_iid_reg);
		MONO_EMIT_NEW_BIALU (cfg, OP_IAND, intf_bit_reg, ibitmap_byte_reg, iid_bit_reg);
	} else {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI1_MEMBASE, ibitmap_byte_reg, ibitmap_reg, klass->interface_id >> 3);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, intf_bit_reg, ibitmap_byte_reg, 1 << (klass->interface_id & 7));
	}
#endif
}

/* 
 * Emit code which loads into "intf_bit_reg" a nonzero value if the MonoClass
 * stored in "klass_reg" implements the interface "klass".
 */
static void
mini_emit_load_intf_bit_reg_class (MonoCompile *cfg, int intf_bit_reg, int klass_reg, MonoClass *klass)
{
	mini_emit_interface_bitmap_check (cfg, intf_bit_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, interface_bitmap), klass);
}

/* 
 * Emit code which loads into "intf_bit_reg" a nonzero value if the MonoVTable
 * stored in "vtable_reg" implements the interface "klass".
 */
static void
mini_emit_load_intf_bit_reg_vtable (MonoCompile *cfg, int intf_bit_reg, int vtable_reg, MonoClass *klass)
{
	mini_emit_interface_bitmap_check (cfg, intf_bit_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, interface_bitmap), klass);
}

/* 
 * Emit code which checks whenever the interface id of @klass is smaller than
 * than the value given by max_iid_reg.
*/
static void
mini_emit_max_iid_check (MonoCompile *cfg, int max_iid_reg, MonoClass *klass,
						 MonoBasicBlock *false_target)
{
	if (cfg->compile_aot) {
		int iid_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_AOTCONST (cfg, iid_reg, klass, MONO_PATCH_INFO_IID);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, max_iid_reg, iid_reg);
	}
	else
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, max_iid_reg, klass->interface_id);
	if (false_target)
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBLT_UN, false_target);
	else
		MONO_EMIT_NEW_COND_EXC (cfg, LT_UN, "InvalidCastException");
}

/* Same as above, but obtains max_iid from a vtable */
static void
mini_emit_max_iid_check_vtable (MonoCompile *cfg, int vtable_reg, MonoClass *klass,
								 MonoBasicBlock *false_target)
{
	int max_iid_reg = alloc_preg (cfg);
		
	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, max_iid_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, max_interface_id));
	mini_emit_max_iid_check (cfg, max_iid_reg, klass, false_target);
}

/* Same as above, but obtains max_iid from a klass */
static void
mini_emit_max_iid_check_class (MonoCompile *cfg, int klass_reg, MonoClass *klass,
								 MonoBasicBlock *false_target)
{
	int max_iid_reg = alloc_preg (cfg);

	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, max_iid_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, max_interface_id));
	mini_emit_max_iid_check (cfg, max_iid_reg, klass, false_target);
}

static void
mini_emit_isninst_cast_inst (MonoCompile *cfg, int klass_reg, MonoClass *klass, MonoInst *klass_ins, MonoBasicBlock *false_target, MonoBasicBlock *true_target)
{
	int idepth_reg = alloc_preg (cfg);
	int stypes_reg = alloc_preg (cfg);
	int stype = alloc_preg (cfg);

	mono_class_setup_supertypes (klass);

	if (klass->idepth > MONO_DEFAULT_SUPERTABLE_SIZE) {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, idepth_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, idepth));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, idepth_reg, klass->idepth);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBLT_UN, false_target);
	}
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stypes_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, supertypes));
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stype, stypes_reg, ((klass->idepth - 1) * SIZEOF_VOID_P));
	if (klass_ins) {
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, stype, klass_ins->dreg);
	} else if (cfg->compile_aot) {
		int const_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_CLASSCONST (cfg, const_reg, klass);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, stype, const_reg);
	} else {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, stype, klass);
	}
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, true_target);
}

static void
mini_emit_isninst_cast (MonoCompile *cfg, int klass_reg, MonoClass *klass, MonoBasicBlock *false_target, MonoBasicBlock *true_target)
{
	mini_emit_isninst_cast_inst (cfg, klass_reg, klass, NULL, false_target, true_target);
}

static void
mini_emit_iface_cast (MonoCompile *cfg, int vtable_reg, MonoClass *klass, MonoBasicBlock *false_target, MonoBasicBlock *true_target)
{
	int intf_reg = alloc_preg (cfg);

	mini_emit_max_iid_check_vtable (cfg, vtable_reg, klass, false_target);
	mini_emit_load_intf_bit_reg_vtable (cfg, intf_reg, vtable_reg, klass);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, intf_reg, 0);
	if (true_target)
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, true_target);
	else
		MONO_EMIT_NEW_COND_EXC (cfg, EQ, "InvalidCastException");		
}

/*
 * Variant of the above that takes a register to the class, not the vtable.
 */
static void
mini_emit_iface_class_cast (MonoCompile *cfg, int klass_reg, MonoClass *klass, MonoBasicBlock *false_target, MonoBasicBlock *true_target)
{
	int intf_bit_reg = alloc_preg (cfg);

	mini_emit_max_iid_check_class (cfg, klass_reg, klass, false_target);
	mini_emit_load_intf_bit_reg_class (cfg, intf_bit_reg, klass_reg, klass);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, intf_bit_reg, 0);
	if (true_target)
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, true_target);
	else
		MONO_EMIT_NEW_COND_EXC (cfg, EQ, "InvalidCastException");
}

static inline void
mini_emit_class_check_inst (MonoCompile *cfg, int klass_reg, MonoClass *klass, MonoInst *klass_inst)
{
	if (klass_inst) {
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, klass_reg, klass_inst->dreg);
	} else if (cfg->compile_aot) {
		int const_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_CLASSCONST (cfg, const_reg, klass);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, klass_reg, const_reg);
	} else {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, klass_reg, klass);
	}
	MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");
}

static inline void
mini_emit_class_check (MonoCompile *cfg, int klass_reg, MonoClass *klass)
{
	mini_emit_class_check_inst (cfg, klass_reg, klass, NULL);
}

static inline void
mini_emit_class_check_branch (MonoCompile *cfg, int klass_reg, MonoClass *klass, int branch_op, MonoBasicBlock *target)
{
	if (cfg->compile_aot) {
		int const_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_CLASSCONST (cfg, const_reg, klass);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, klass_reg, const_reg);
	} else {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, klass_reg, klass);
	}
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, branch_op, target);
}

static void
mini_emit_castclass (MonoCompile *cfg, int obj_reg, int klass_reg, MonoClass *klass, MonoBasicBlock *object_is_null);
	
static void
mini_emit_castclass_inst (MonoCompile *cfg, int obj_reg, int klass_reg, MonoClass *klass, MonoInst *klass_inst, MonoBasicBlock *object_is_null)
{
	if (klass->rank) {
		int rank_reg = alloc_preg (cfg);
		int eclass_reg = alloc_preg (cfg);

		g_assert (!klass_inst);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, rank_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, rank));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, rank_reg, klass->rank);
		MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");
		//		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, eclass_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, cast_class));
		if (klass->cast_class == mono_defaults.object_class) {
			int parent_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, parent_reg, eclass_reg, MONO_STRUCT_OFFSET (MonoClass, parent));
			mini_emit_class_check_branch (cfg, parent_reg, mono_defaults.enum_class->parent, OP_PBNE_UN, object_is_null);
			mini_emit_class_check (cfg, eclass_reg, mono_defaults.enum_class);
		} else if (klass->cast_class == mono_defaults.enum_class->parent) {
			mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class->parent, OP_PBEQ, object_is_null);
			mini_emit_class_check (cfg, eclass_reg, mono_defaults.enum_class);
		} else if (klass->cast_class == mono_defaults.enum_class) {
			mini_emit_class_check (cfg, eclass_reg, mono_defaults.enum_class);
		} else if (klass->cast_class->flags & TYPE_ATTRIBUTE_INTERFACE) {
			mini_emit_iface_class_cast (cfg, eclass_reg, klass->cast_class, NULL, NULL);
		} else {
			// Pass -1 as obj_reg to skip the check below for arrays of arrays
			mini_emit_castclass (cfg, -1, eclass_reg, klass->cast_class, object_is_null);
		}

		if ((klass->rank == 1) && (klass->byval_arg.type == MONO_TYPE_SZARRAY) && (obj_reg != -1)) {
			/* Check that the object is a vector too */
			int bounds_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, bounds_reg, obj_reg, MONO_STRUCT_OFFSET (MonoArray, bounds));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, bounds_reg, 0);
			MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");
		}
	} else {
		int idepth_reg = alloc_preg (cfg);
		int stypes_reg = alloc_preg (cfg);
		int stype = alloc_preg (cfg);

		mono_class_setup_supertypes (klass);

		if (klass->idepth > MONO_DEFAULT_SUPERTABLE_SIZE) {
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, idepth_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, idepth));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, idepth_reg, klass->idepth);
			MONO_EMIT_NEW_COND_EXC (cfg, LT_UN, "InvalidCastException");
		}
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stypes_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, supertypes));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stype, stypes_reg, ((klass->idepth - 1) * SIZEOF_VOID_P));
		mini_emit_class_check_inst (cfg, stype, klass, klass_inst);
	}
}

static void
mini_emit_castclass (MonoCompile *cfg, int obj_reg, int klass_reg, MonoClass *klass, MonoBasicBlock *object_is_null)
{
	mini_emit_castclass_inst (cfg, obj_reg, klass_reg, klass, NULL, object_is_null);
}

static void 
mini_emit_memset (MonoCompile *cfg, int destreg, int offset, int size, int val, int align)
{
	int val_reg;

	g_assert (val == 0);

	if (align == 0)
		align = 4;

	if ((size <= SIZEOF_REGISTER) && (size <= align)) {
		switch (size) {
		case 1:
			MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREI1_MEMBASE_IMM, destreg, offset, val);
			return;
		case 2:
			MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREI2_MEMBASE_IMM, destreg, offset, val);
			return;
		case 4:
			MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREI4_MEMBASE_IMM, destreg, offset, val);
			return;
#if SIZEOF_REGISTER == 8
		case 8:
			MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREI8_MEMBASE_IMM, destreg, offset, val);
			return;
#endif
		}
	}

	val_reg = alloc_preg (cfg);

	if (SIZEOF_REGISTER == 8)
		MONO_EMIT_NEW_I8CONST (cfg, val_reg, val);
	else
		MONO_EMIT_NEW_ICONST (cfg, val_reg, val);

	if (align < 4) {
		/* This could be optimized further if neccesary */
		while (size >= 1) {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, destreg, offset, val_reg);
			offset += 1;
			size -= 1;
		}
		return;
	}	

#if !NO_UNALIGNED_ACCESS
	if (SIZEOF_REGISTER == 8) {
		if (offset % 8) {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, destreg, offset, val_reg);
			offset += 4;
			size -= 4;
		}
		while (size >= 8) {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, destreg, offset, val_reg);
			offset += 8;
			size -= 8;
		}
	}	
#endif

	while (size >= 4) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, destreg, offset, val_reg);
		offset += 4;
		size -= 4;
	}
	while (size >= 2) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, destreg, offset, val_reg);
		offset += 2;
		size -= 2;
	}
	while (size >= 1) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, destreg, offset, val_reg);
		offset += 1;
		size -= 1;
	}
}

void 
mini_emit_memcpy (MonoCompile *cfg, int destreg, int doffset, int srcreg, int soffset, int size, int align)
{
	int cur_reg;

	if (align == 0)
		align = 4;

	/*FIXME arbitrary hack to avoid unbound code expansion.*/
	g_assert (size < 10000);

	if (align < 4) {
		/* This could be optimized further if neccesary */
		while (size >= 1) {
			cur_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI1_MEMBASE, cur_reg, srcreg, soffset);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, destreg, doffset, cur_reg);
			doffset += 1;
			soffset += 1;
			size -= 1;
		}
	}

#if !NO_UNALIGNED_ACCESS
	if (SIZEOF_REGISTER == 8) {
		while (size >= 8) {
			cur_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI8_MEMBASE, cur_reg, srcreg, soffset);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, destreg, doffset, cur_reg);
			doffset += 8;
			soffset += 8;
			size -= 8;
		}
	}	
#endif

	while (size >= 4) {
		cur_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, cur_reg, srcreg, soffset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, destreg, doffset, cur_reg);
		doffset += 4;
		soffset += 4;
		size -= 4;
	}
	while (size >= 2) {
		cur_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI2_MEMBASE, cur_reg, srcreg, soffset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, destreg, doffset, cur_reg);
		doffset += 2;
		soffset += 2;
		size -= 2;
	}
	while (size >= 1) {
		cur_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI1_MEMBASE, cur_reg, srcreg, soffset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, destreg, doffset, cur_reg);
		doffset += 1;
		soffset += 1;
		size -= 1;
	}
}

static void
emit_tls_set (MonoCompile *cfg, int sreg1, int tls_key)
{
	MonoInst *ins, *c;

	if (cfg->compile_aot) {
		EMIT_NEW_TLS_OFFSETCONST (cfg, c, tls_key);
		MONO_INST_NEW (cfg, ins, OP_TLS_SET_REG);
		ins->sreg1 = sreg1;
		ins->sreg2 = c->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
	} else {
		MONO_INST_NEW (cfg, ins, OP_TLS_SET);
		ins->sreg1 = sreg1;
		ins->inst_offset = mini_get_tls_offset (tls_key);
		MONO_ADD_INS (cfg->cbb, ins);
	}
}

/*
 * emit_push_lmf:
 *
 *   Emit IR to push the current LMF onto the LMF stack.
 */
static void
emit_push_lmf (MonoCompile *cfg)
{
	/*
	 * Emit IR to push the LMF:
	 * lmf_addr = <lmf_addr from tls>
	 * lmf->lmf_addr = lmf_addr
	 * lmf->prev_lmf = *lmf_addr
	 * *lmf_addr = lmf
	 */
	int lmf_reg, prev_lmf_reg;
	MonoInst *ins, *lmf_ins;

	if (!cfg->lmf_ir)
		return;

	if (cfg->lmf_ir_mono_lmf && mini_tls_get_supported (cfg, TLS_KEY_LMF)) {
		/* Load current lmf */
		lmf_ins = mono_get_lmf_intrinsic (cfg);
		g_assert (lmf_ins);
		MONO_ADD_INS (cfg->cbb, lmf_ins);
		EMIT_NEW_VARLOADA (cfg, ins, cfg->lmf_var, NULL);
		lmf_reg = ins->dreg;
		/* Save previous_lmf */
		EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STORE_MEMBASE_REG, lmf_reg, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf), lmf_ins->dreg);
		/* Set new LMF */
		emit_tls_set (cfg, lmf_reg, TLS_KEY_LMF);
	} else {
		/*
		 * Store lmf_addr in a variable, so it can be allocated to a global register.
		 */
		if (!cfg->lmf_addr_var)
			cfg->lmf_addr_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);

#ifdef HOST_WIN32
		ins = mono_get_jit_tls_intrinsic (cfg);
		if (ins) {
			int jit_tls_dreg = ins->dreg;

			MONO_ADD_INS (cfg->cbb, ins);
			lmf_reg = alloc_preg (cfg);
			EMIT_NEW_BIALU_IMM (cfg, lmf_ins, OP_PADD_IMM, lmf_reg, jit_tls_dreg, MONO_STRUCT_OFFSET (MonoJitTlsData, lmf));
		} else {
			lmf_ins = mono_emit_jit_icall (cfg, mono_get_lmf_addr, NULL);
		}
#else
		lmf_ins = mono_get_lmf_addr_intrinsic (cfg);
		if (lmf_ins) {
			MONO_ADD_INS (cfg->cbb, lmf_ins);
		} else {
#ifdef TARGET_IOS
			MonoInst *args [16], *jit_tls_ins, *ins;

			/* Inline mono_get_lmf_addr () */
			/* jit_tls = pthread_getspecific (mono_jit_tls_id); lmf_addr = &jit_tls->lmf; */

			/* Load mono_jit_tls_id */
			EMIT_NEW_AOTCONST (cfg, args [0], MONO_PATCH_INFO_JIT_TLS_ID, NULL);
			/* call pthread_getspecific () */
			jit_tls_ins = mono_emit_jit_icall (cfg, pthread_getspecific, args);
			/* lmf_addr = &jit_tls->lmf */
			EMIT_NEW_BIALU_IMM (cfg, ins, OP_PADD_IMM, cfg->lmf_addr_var->dreg, jit_tls_ins->dreg, MONO_STRUCT_OFFSET (MonoJitTlsData, lmf));
			lmf_ins = ins;
#else
			lmf_ins = mono_emit_jit_icall (cfg, mono_get_lmf_addr, NULL);
#endif
		}
#endif
		lmf_ins->dreg = cfg->lmf_addr_var->dreg;

		EMIT_NEW_VARLOADA (cfg, ins, cfg->lmf_var, NULL);
		lmf_reg = ins->dreg;

		prev_lmf_reg = alloc_preg (cfg);
		/* Save previous_lmf */
		EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, prev_lmf_reg, cfg->lmf_addr_var->dreg, 0);
		EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STORE_MEMBASE_REG, lmf_reg, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf), prev_lmf_reg);
		/* Set new lmf */
		EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STORE_MEMBASE_REG, cfg->lmf_addr_var->dreg, 0, lmf_reg);
	}
}

/*
 * emit_pop_lmf:
 *
 *   Emit IR to pop the current LMF from the LMF stack.
 */
static void
emit_pop_lmf (MonoCompile *cfg)
{
	int lmf_reg, lmf_addr_reg, prev_lmf_reg;
	MonoInst *ins;

	if (!cfg->lmf_ir)
		return;

 	EMIT_NEW_VARLOADA (cfg, ins, cfg->lmf_var, NULL);
 	lmf_reg = ins->dreg;

	if (cfg->lmf_ir_mono_lmf && mini_tls_get_supported (cfg, TLS_KEY_LMF)) {
		/* Load previous_lmf */
		prev_lmf_reg = alloc_preg (cfg);
		EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, prev_lmf_reg, lmf_reg, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
		/* Set new LMF */
		emit_tls_set (cfg, prev_lmf_reg, TLS_KEY_LMF);
	} else {
		/*
		 * Emit IR to pop the LMF:
		 * *(lmf->lmf_addr) = lmf->prev_lmf
		 */
		/* This could be called before emit_push_lmf () */
		if (!cfg->lmf_addr_var)
			cfg->lmf_addr_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
		lmf_addr_reg = cfg->lmf_addr_var->dreg;

		prev_lmf_reg = alloc_preg (cfg);
		EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, prev_lmf_reg, lmf_reg, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
		EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STORE_MEMBASE_REG, lmf_addr_reg, 0, prev_lmf_reg);
	}
}

static void
emit_instrumentation_call (MonoCompile *cfg, void *func)
{
	MonoInst *iargs [1];

	/*
	 * Avoid instrumenting inlined methods since it can
	 * distort profiling results.
	 */
	if (cfg->method != cfg->current_method)
		return;

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE) {
		EMIT_NEW_METHODCONST (cfg, iargs [0], cfg->method);
		mono_emit_jit_icall (cfg, func, iargs);
	}
}

static int
ret_type_to_call_opcode (MonoType *type, int calli, int virt, MonoGenericSharingContext *gsctx)
{
	if (type->byref)
		return calli? OP_CALL_REG: virt? OP_CALL_MEMBASE: OP_CALL;

handle_enum:
	type = mini_get_basic_type_from_generic (gsctx, type);
	type = mini_replace_type (type);
	switch (type->type) {
	case MONO_TYPE_VOID:
		return calli? OP_VOIDCALL_REG: virt? OP_VOIDCALL_MEMBASE: OP_VOIDCALL;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return calli? OP_CALL_REG: virt? OP_CALL_MEMBASE: OP_CALL;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return calli? OP_CALL_REG: virt? OP_CALL_MEMBASE: OP_CALL;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return calli? OP_CALL_REG: virt? OP_CALL_MEMBASE: OP_CALL;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return calli? OP_LCALL_REG: virt? OP_LCALL_MEMBASE: OP_LCALL;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		return calli? OP_FCALL_REG: virt? OP_FCALL_MEMBASE: OP_FCALL;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = mono_class_enum_basetype (type->data.klass);
			goto handle_enum;
		} else
			return calli? OP_VCALL_REG: virt? OP_VCALL_MEMBASE: OP_VCALL;
	case MONO_TYPE_TYPEDBYREF:
		return calli? OP_VCALL_REG: virt? OP_VCALL_MEMBASE: OP_VCALL;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		goto handle_enum;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		/* gsharedvt */
		return calli? OP_VCALL_REG: virt? OP_VCALL_MEMBASE: OP_VCALL;
	default:
		g_error ("unknown type 0x%02x in ret_type_to_call_opcode", type->type);
	}
	return -1;
}

/*
 * target_type_is_incompatible:
 * @cfg: MonoCompile context
 *
 * Check that the item @arg on the evaluation stack can be stored
 * in the target type (can be a local, or field, etc).
 * The cfg arg can be used to check if we need verification or just
 * validity checks.
 *
 * Returns: non-0 value if arg can't be stored on a target.
 */
static int
target_type_is_incompatible (MonoCompile *cfg, MonoType *target, MonoInst *arg)
{
	MonoType *simple_type;
	MonoClass *klass;

	target = mini_replace_type (target);
	if (target->byref) {
		/* FIXME: check that the pointed to types match */
		if (arg->type == STACK_MP)
			return arg->klass != mono_class_from_mono_type (target);
		if (arg->type == STACK_PTR)
			return 0;
		return 1;
	}

	simple_type = mono_type_get_underlying_type (target);
	switch (simple_type->type) {
	case MONO_TYPE_VOID:
		return 1;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		if (arg->type != STACK_I4 && arg->type != STACK_PTR)
			return 1;
		return 0;
	case MONO_TYPE_PTR:
		/* STACK_MP is needed when setting pinned locals */
		if (arg->type != STACK_I4 && arg->type != STACK_PTR && arg->type != STACK_MP)
			return 1;
		return 0;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_FNPTR:
		/* 
		 * Some opcodes like ldloca returns 'transient pointers' which can be stored in
		 * in native int. (#688008).
		 */
		if (arg->type != STACK_I4 && arg->type != STACK_PTR && arg->type != STACK_MP)
			return 1;
		return 0;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		if (arg->type != STACK_OBJ)
			return 1;
		/* FIXME: check type compatibility */
		return 0;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		if (arg->type != STACK_I8)
			return 1;
		return 0;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		if (arg->type != STACK_R8)
			return 1;
		return 0;
	case MONO_TYPE_VALUETYPE:
		if (arg->type != STACK_VTYPE)
			return 1;
		klass = mono_class_from_mono_type (simple_type);
		if (klass != arg->klass)
			return 1;
		return 0;
	case MONO_TYPE_TYPEDBYREF:
		if (arg->type != STACK_VTYPE)
			return 1;
		klass = mono_class_from_mono_type (simple_type);
		if (klass != arg->klass)
			return 1;
		return 0;
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (simple_type)) {
			if (arg->type != STACK_VTYPE)
				return 1;
			klass = mono_class_from_mono_type (simple_type);
			if (klass != arg->klass)
				return 1;
			return 0;
		} else {
			if (arg->type != STACK_OBJ)
				return 1;
			/* FIXME: check type compatibility */
			return 0;
		}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (cfg->generic_sharing_context);
		if (mini_type_var_is_vt (cfg, simple_type)) {
			if (arg->type != STACK_VTYPE)
				return 1;
		} else {
			if (arg->type != STACK_OBJ)
				return 1;
		}
		return 0;
	default:
		g_error ("unknown type 0x%02x in target_type_is_incompatible", simple_type->type);
	}
	return 1;
}

/*
 * Prepare arguments for passing to a function call.
 * Return a non-zero value if the arguments can't be passed to the given
 * signature.
 * The type checks are not yet complete and some conversions may need
 * casts on 32 or 64 bit architectures.
 *
 * FIXME: implement this using target_type_is_incompatible ()
 */
static int
check_call_signature (MonoCompile *cfg, MonoMethodSignature *sig, MonoInst **args)
{
	MonoType *simple_type;
	int i;

	if (sig->hasthis) {
		if (args [0]->type != STACK_OBJ && args [0]->type != STACK_MP && args [0]->type != STACK_PTR)
			return 1;
		args++;
	}
	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			if (args [i]->type != STACK_MP && args [i]->type != STACK_PTR)
				return 1;
			continue;
		}
		simple_type = sig->params [i];
		simple_type = mini_get_basic_type_from_generic (cfg->generic_sharing_context, simple_type);
handle_enum:
		switch (simple_type->type) {
		case MONO_TYPE_VOID:
			return 1;
			continue;
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			if (args [i]->type != STACK_I4 && args [i]->type != STACK_PTR)
				return 1;
			continue;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
			if (args [i]->type != STACK_I4 && args [i]->type != STACK_PTR && args [i]->type != STACK_MP && args [i]->type != STACK_OBJ)
				return 1;
			continue;
		case MONO_TYPE_CLASS:
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:    
			if (args [i]->type != STACK_OBJ)
				return 1;
			continue;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			if (args [i]->type != STACK_I8)
				return 1;
			continue;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			if (args [i]->type != STACK_R8)
				return 1;
			continue;
		case MONO_TYPE_VALUETYPE:
			if (simple_type->data.klass->enumtype) {
				simple_type = mono_class_enum_basetype (simple_type->data.klass);
				goto handle_enum;
			}
			if (args [i]->type != STACK_VTYPE)
				return 1;
			continue;
		case MONO_TYPE_TYPEDBYREF:
			if (args [i]->type != STACK_VTYPE)
				return 1;
			continue;
		case MONO_TYPE_GENERICINST:
			simple_type = &simple_type->data.generic_class->container_class->byval_arg;
			goto handle_enum;
		case MONO_TYPE_VAR:
		case MONO_TYPE_MVAR:
			/* gsharedvt */
			if (args [i]->type != STACK_VTYPE)
				return 1;
			continue;
		default:
			g_error ("unknown type 0x%02x in check_call_signature",
				 simple_type->type);
		}
	}
	return 0;
}

static int
callvirt_to_call (int opcode)
{
	switch (opcode) {
	case OP_CALL_MEMBASE:
		return OP_CALL;
	case OP_VOIDCALL_MEMBASE:
		return OP_VOIDCALL;
	case OP_FCALL_MEMBASE:
		return OP_FCALL;
	case OP_VCALL_MEMBASE:
		return OP_VCALL;
	case OP_LCALL_MEMBASE:
		return OP_LCALL;
	default:
		g_assert_not_reached ();
	}

	return -1;
}

/* Either METHOD or IMT_ARG needs to be set */
static void
emit_imt_argument (MonoCompile *cfg, MonoCallInst *call, MonoMethod *method, MonoInst *imt_arg)
{
	int method_reg;

	if (COMPILE_LLVM (cfg)) {
		method_reg = alloc_preg (cfg);

		if (imt_arg) {
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, method_reg, imt_arg->dreg);
		} else if (cfg->compile_aot) {
			MONO_EMIT_NEW_AOTCONST (cfg, method_reg, method, MONO_PATCH_INFO_METHODCONST);
		} else {
			MonoInst *ins;
			MONO_INST_NEW (cfg, ins, OP_PCONST);
			ins->inst_p0 = method;
			ins->dreg = method_reg;
			MONO_ADD_INS (cfg->cbb, ins);
		}

#ifdef ENABLE_LLVM
		call->imt_arg_reg = method_reg;
#endif
#ifdef MONO_ARCH_IMT_REG
	mono_call_inst_add_outarg_reg (cfg, call, method_reg, MONO_ARCH_IMT_REG, FALSE);
#else
	/* Need this to keep the IMT arg alive */
	mono_call_inst_add_outarg_reg (cfg, call, method_reg, 0, FALSE);
#endif
		return;
	}

#ifdef MONO_ARCH_IMT_REG
	method_reg = alloc_preg (cfg);

	if (imt_arg) {
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, method_reg, imt_arg->dreg);
	} else if (cfg->compile_aot) {
		MONO_EMIT_NEW_AOTCONST (cfg, method_reg, method, MONO_PATCH_INFO_METHODCONST);
	} else {
		MonoInst *ins;
		MONO_INST_NEW (cfg, ins, OP_PCONST);
		ins->inst_p0 = method;
		ins->dreg = method_reg;
		MONO_ADD_INS (cfg->cbb, ins);
	}

	mono_call_inst_add_outarg_reg (cfg, call, method_reg, MONO_ARCH_IMT_REG, FALSE);
#else
	mono_arch_emit_imt_argument (cfg, call, imt_arg);
#endif
}

static MonoJumpInfo *
mono_patch_info_new (MonoMemPool *mp, int ip, MonoJumpInfoType type, gconstpointer target)
{
	MonoJumpInfo *ji = mono_mempool_alloc (mp, sizeof (MonoJumpInfo));

	ji->ip.i = ip;
	ji->type = type;
	ji->data.target = target;

	return ji;
}

static int
mini_class_check_context_used (MonoCompile *cfg, MonoClass *klass)
{
	if (cfg->generic_sharing_context)
		return mono_class_check_context_used (klass);
	else
		return 0;
}

static int
mini_method_check_context_used (MonoCompile *cfg, MonoMethod *method)
{
	if (cfg->generic_sharing_context)
		return mono_method_check_context_used (method);
	else
		return 0;
}

/*
 * check_method_sharing:
 *
 *   Check whenever the vtable or an mrgctx needs to be passed when calling CMETHOD.
 */
static void
check_method_sharing (MonoCompile *cfg, MonoMethod *cmethod, gboolean *out_pass_vtable, gboolean *out_pass_mrgctx)
{
	gboolean pass_vtable = FALSE;
	gboolean pass_mrgctx = FALSE;

	if (((cmethod->flags & METHOD_ATTRIBUTE_STATIC) || cmethod->klass->valuetype) &&
		(cmethod->klass->generic_class || cmethod->klass->generic_container)) {
		gboolean sharable = FALSE;

		if (mono_method_is_generic_sharable (cmethod, TRUE)) {
			sharable = TRUE;
		} else {
			gboolean sharing_enabled = mono_class_generic_sharing_enabled (cmethod->klass);
			MonoGenericContext *context = mini_class_get_context (cmethod->klass);
			gboolean context_sharable = mono_generic_context_is_sharable (context, TRUE);

			sharable = sharing_enabled && context_sharable;
		}

		/*
		 * Pass vtable iff target method might
		 * be shared, which means that sharing
		 * is enabled for its class and its
		 * context is sharable (and it's not a
		 * generic method).
		 */
		if (sharable && !(mini_method_get_context (cmethod) && mini_method_get_context (cmethod)->method_inst))
			pass_vtable = TRUE;
	}

	if (mini_method_get_context (cmethod) &&
		mini_method_get_context (cmethod)->method_inst) {
		g_assert (!pass_vtable);

		if (mono_method_is_generic_sharable (cmethod, TRUE)) {
			pass_mrgctx = TRUE;
		} else {
			gboolean sharing_enabled = mono_class_generic_sharing_enabled (cmethod->klass);
			MonoGenericContext *context = mini_method_get_context (cmethod);
			gboolean context_sharable = mono_generic_context_is_sharable (context, TRUE);

			if (sharing_enabled && context_sharable)
				pass_mrgctx = TRUE;
			if (cfg->gsharedvt && mini_is_gsharedvt_signature (cfg, mono_method_signature (cmethod)))
				pass_mrgctx = TRUE;
		}
	}

	if (out_pass_vtable)
		*out_pass_vtable = pass_vtable;
	if (out_pass_mrgctx)
		*out_pass_mrgctx = pass_mrgctx;
}

inline static MonoCallInst *
mono_emit_call_args (MonoCompile *cfg, MonoMethodSignature *sig, 
					 MonoInst **args, int calli, int virtual, int tail, int rgctx, int unbox_trampoline)
{
	MonoType *sig_ret;
	MonoCallInst *call;
#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
	int i;
#endif

	if (tail) {
		emit_instrumentation_call (cfg, mono_profiler_method_leave);

		MONO_INST_NEW_CALL (cfg, call, OP_TAILCALL);
	} else
		MONO_INST_NEW_CALL (cfg, call, ret_type_to_call_opcode (sig->ret, calli, virtual, cfg->generic_sharing_context));

	call->args = args;
	call->signature = sig;
	call->rgctx_reg = rgctx;
	sig_ret = mini_replace_type (sig->ret);

	type_to_eval_stack_type ((cfg), sig_ret, &call->inst);

	if (tail) {
		if (mini_type_is_vtype (cfg, sig_ret)) {
			call->vret_var = cfg->vret_addr;
			//g_assert_not_reached ();
		}
	} else if (mini_type_is_vtype (cfg, sig_ret)) {
		MonoInst *temp = mono_compile_create_var (cfg, sig_ret, OP_LOCAL);
		MonoInst *loada;

		temp->backend.is_pinvoke = sig->pinvoke;

		/*
		 * We use a new opcode OP_OUTARG_VTRETADDR instead of LDADDR for emitting the
		 * address of return value to increase optimization opportunities.
		 * Before vtype decomposition, the dreg of the call ins itself represents the
		 * fact the call modifies the return value. After decomposition, the call will
		 * be transformed into one of the OP_VOIDCALL opcodes, and the VTRETADDR opcode
		 * will be transformed into an LDADDR.
		 */
		MONO_INST_NEW (cfg, loada, OP_OUTARG_VTRETADDR);
		loada->dreg = alloc_preg (cfg);
		loada->inst_p0 = temp;
		/* We reference the call too since call->dreg could change during optimization */
		loada->inst_p1 = call;
		MONO_ADD_INS (cfg->cbb, loada);

		call->inst.dreg = temp->dreg;

		call->vret_var = loada;
	} else if (!MONO_TYPE_IS_VOID (sig_ret))
		call->inst.dreg = alloc_dreg (cfg, call->inst.type);

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
	if (COMPILE_SOFT_FLOAT (cfg)) {
		/* 
		 * If the call has a float argument, we would need to do an r8->r4 conversion using 
		 * an icall, but that cannot be done during the call sequence since it would clobber
		 * the call registers + the stack. So we do it before emitting the call.
		 */
		for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
			MonoType *t;
			MonoInst *in = call->args [i];

			if (i >= sig->hasthis)
				t = sig->params [i - sig->hasthis];
			else
				t = &mono_defaults.int_class->byval_arg;
			t = mono_type_get_underlying_type (t);

			if (!t->byref && t->type == MONO_TYPE_R4) {
				MonoInst *iargs [1];
				MonoInst *conv;

				iargs [0] = in;
				conv = mono_emit_jit_icall (cfg, mono_fload_r4_arg, iargs);

				/* The result will be in an int vreg */
				call->args [i] = conv;
			}
		}
	}
#endif

	call->need_unbox_trampoline = unbox_trampoline;

#ifdef ENABLE_LLVM
	if (COMPILE_LLVM (cfg))
		mono_llvm_emit_call (cfg, call);
	else
		mono_arch_emit_call (cfg, call);
#else
	mono_arch_emit_call (cfg, call);
#endif

	cfg->param_area = MAX (cfg->param_area, call->stack_usage);
	cfg->flags |= MONO_CFG_HAS_CALLS;
	
	return call;
}

static void
set_rgctx_arg (MonoCompile *cfg, MonoCallInst *call, int rgctx_reg, MonoInst *rgctx_arg)
{
#ifdef MONO_ARCH_RGCTX_REG
	mono_call_inst_add_outarg_reg (cfg, call, rgctx_reg, MONO_ARCH_RGCTX_REG, FALSE);
	cfg->uses_rgctx_reg = TRUE;
	call->rgctx_reg = TRUE;
#ifdef ENABLE_LLVM
	call->rgctx_arg_reg = rgctx_reg;
#endif
#else
	NOT_IMPLEMENTED;
#endif
}	

inline static MonoInst*
mono_emit_calli (MonoCompile *cfg, MonoMethodSignature *sig, MonoInst **args, MonoInst *addr, MonoInst *imt_arg, MonoInst *rgctx_arg)
{
	MonoCallInst *call;
	MonoInst *ins;
	int rgctx_reg = -1;
	gboolean check_sp = FALSE;

	if (cfg->check_pinvoke_callconv && cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (cfg->method);

		if (info && info->subtype == WRAPPER_SUBTYPE_PINVOKE)
			check_sp = TRUE;
	}

	if (rgctx_arg) {
		rgctx_reg = mono_alloc_preg (cfg);
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, rgctx_reg, rgctx_arg->dreg);
	}

	if (check_sp) {
		if (!cfg->stack_inbalance_var)
			cfg->stack_inbalance_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);

		MONO_INST_NEW (cfg, ins, OP_GET_SP);
		ins->dreg = cfg->stack_inbalance_var->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
	}

	call = mono_emit_call_args (cfg, sig, args, TRUE, FALSE, FALSE, rgctx_arg ? TRUE : FALSE, FALSE);

	call->inst.sreg1 = addr->dreg;

	if (imt_arg)
		emit_imt_argument (cfg, call, NULL, imt_arg);

	MONO_ADD_INS (cfg->cbb, (MonoInst*)call);

	if (check_sp) {
		int sp_reg;

		sp_reg = mono_alloc_preg (cfg);

		MONO_INST_NEW (cfg, ins, OP_GET_SP);
		ins->dreg = sp_reg;
		MONO_ADD_INS (cfg->cbb, ins);

		/* Restore the stack so we don't crash when throwing the exception */
		MONO_INST_NEW (cfg, ins, OP_SET_SP);
		ins->sreg1 = cfg->stack_inbalance_var->dreg;
		MONO_ADD_INS (cfg->cbb, ins);

		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, cfg->stack_inbalance_var->dreg, sp_reg);
		MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "ExecutionEngineException");
	}

	if (rgctx_arg)
		set_rgctx_arg (cfg, call, rgctx_reg, rgctx_arg);

	return (MonoInst*)call;
}

static MonoInst*
emit_get_gsharedvt_info_klass (MonoCompile *cfg, MonoClass *klass, MonoRgctxInfoType rgctx_type);

static MonoInst*
emit_get_rgctx_method (MonoCompile *cfg, int context_used, MonoMethod *cmethod, MonoRgctxInfoType rgctx_type);
static MonoInst*
emit_get_rgctx_klass (MonoCompile *cfg, int context_used, MonoClass *klass, MonoRgctxInfoType rgctx_type);

static MonoInst*
mono_emit_method_call_full (MonoCompile *cfg, MonoMethod *method, MonoMethodSignature *sig, gboolean tail,
							MonoInst **args, MonoInst *this, MonoInst *imt_arg, MonoInst *rgctx_arg)
{
#ifndef DISABLE_REMOTING
	gboolean might_be_remote = FALSE;
#endif
	gboolean virtual = this != NULL;
	gboolean enable_for_aot = TRUE;
	int context_used;
	MonoCallInst *call;
	int rgctx_reg = 0;
	gboolean need_unbox_trampoline;

	if (!sig)
		sig = mono_method_signature (method);

	if (rgctx_arg) {
		rgctx_reg = mono_alloc_preg (cfg);
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, rgctx_reg, rgctx_arg->dreg);
	}

	if (method->string_ctor) {
		/* Create the real signature */
		/* FIXME: Cache these */
		MonoMethodSignature *ctor_sig = mono_metadata_signature_dup_mempool (cfg->mempool, sig);
		ctor_sig->ret = &mono_defaults.string_class->byval_arg;

		sig = ctor_sig;
	}

	context_used = mini_method_check_context_used (cfg, method);

#ifndef DISABLE_REMOTING
	might_be_remote = this && sig->hasthis &&
		(mono_class_is_marshalbyref (method->klass) || method->klass == mono_defaults.object_class) &&
		!(method->flags & METHOD_ATTRIBUTE_VIRTUAL) && (!MONO_CHECK_THIS (this) || context_used);

	if (might_be_remote && context_used) {
		MonoInst *addr;

		g_assert (cfg->generic_sharing_context);

		addr = emit_get_rgctx_method (cfg, context_used, method, MONO_RGCTX_INFO_REMOTING_INVOKE_WITH_CHECK);

		return mono_emit_calli (cfg, sig, args, addr, NULL, NULL);
	}
#endif

	need_unbox_trampoline = method->klass == mono_defaults.object_class || (method->klass->flags & TYPE_ATTRIBUTE_INTERFACE);

	call = mono_emit_call_args (cfg, sig, args, FALSE, virtual, tail, rgctx_arg ? TRUE : FALSE, need_unbox_trampoline);

#ifndef DISABLE_REMOTING
	if (might_be_remote)
		call->method = mono_marshal_get_remoting_invoke_with_check (method);
	else
#endif
		call->method = method;
	call->inst.flags |= MONO_INST_HAS_METHOD;
	call->inst.inst_left = this;
	call->tail_call = tail;

	if (virtual) {
		int vtable_reg, slot_reg, this_reg;
		int offset;

		this_reg = this->dreg;

		if (ARCH_HAVE_DELEGATE_TRAMPOLINES && (method->klass->parent == mono_defaults.multicastdelegate_class) && !strcmp (method->name, "Invoke")) {
			MonoInst *dummy_use;

			MONO_EMIT_NULL_CHECK (cfg, this_reg);

			/* Make a call to delegate->invoke_impl */
			call->inst.inst_basereg = this_reg;
			call->inst.inst_offset = MONO_STRUCT_OFFSET (MonoDelegate, invoke_impl);
			MONO_ADD_INS (cfg->cbb, (MonoInst*)call);

			/* We must emit a dummy use here because the delegate trampoline will
			replace the 'this' argument with the delegate target making this activation
			no longer a root for the delegate.
			This is an issue for delegates that target collectible code such as dynamic
			methods of GC'able assemblies.

			For a test case look into #667921.

			FIXME: a dummy use is not the best way to do it as the local register allocator
			will put it on a caller save register and spil it around the call. 
			Ideally, we would either put it on a callee save register or only do the store part.  
			 */
			EMIT_NEW_DUMMY_USE (cfg, dummy_use, args [0]);

			return (MonoInst*)call;
		}

		if ((!cfg->compile_aot || enable_for_aot) && 
			(!(method->flags & METHOD_ATTRIBUTE_VIRTUAL) || 
			 (MONO_METHOD_IS_FINAL (method) &&
			  method->wrapper_type != MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK)) &&
			!(mono_class_is_marshalbyref (method->klass) && context_used)) {
			/* 
			 * the method is not virtual, we just need to ensure this is not null
			 * and then we can call the method directly.
			 */
#ifndef DISABLE_REMOTING
			if (mono_class_is_marshalbyref (method->klass) || method->klass == mono_defaults.object_class) {
				/* 
				 * The check above ensures method is not gshared, this is needed since
				 * gshared methods can't have wrappers.
				 */
				method = call->method = mono_marshal_get_remoting_invoke_with_check (method);
			}
#endif

			if (!method->string_ctor)
				MONO_EMIT_NEW_CHECK_THIS (cfg, this_reg);

			call->inst.opcode = callvirt_to_call (call->inst.opcode);
		} else if ((method->flags & METHOD_ATTRIBUTE_VIRTUAL) && MONO_METHOD_IS_FINAL (method)) {
			/*
			 * the method is virtual, but we can statically dispatch since either
			 * it's class or the method itself are sealed.
			 * But first we need to ensure it's not a null reference.
			 */
			MONO_EMIT_NEW_CHECK_THIS (cfg, this_reg);

			call->inst.opcode = callvirt_to_call (call->inst.opcode);
		} else {
			vtable_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_FAULT (cfg, vtable_reg, this_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
			if (method->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
				slot_reg = -1;
				if (mono_use_imt) {
					guint32 imt_slot = mono_method_get_imt_slot (method);
					emit_imt_argument (cfg, call, call->method, imt_arg);
					slot_reg = vtable_reg;
					offset = ((gint32)imt_slot - MONO_IMT_SIZE) * SIZEOF_VOID_P;
				}
				if (slot_reg == -1) {
					slot_reg = alloc_preg (cfg);
					mini_emit_load_intf_reg_vtable (cfg, slot_reg, vtable_reg, method->klass);
					offset = mono_method_get_vtable_index (method) * SIZEOF_VOID_P;
				}
			} else {
				slot_reg = vtable_reg;
				offset = MONO_STRUCT_OFFSET (MonoVTable, vtable) +
					((mono_method_get_vtable_index (method)) * (SIZEOF_VOID_P));
				if (imt_arg) {
					g_assert (mono_method_signature (method)->generic_param_count);
					emit_imt_argument (cfg, call, call->method, imt_arg);
				}
			}

			call->inst.sreg1 = slot_reg;
			call->inst.inst_offset = offset;
			call->virtual = TRUE;
		}
	}

	MONO_ADD_INS (cfg->cbb, (MonoInst*)call);

	if (rgctx_arg)
		set_rgctx_arg (cfg, call, rgctx_reg, rgctx_arg);

	return (MonoInst*)call;
}

MonoInst*
mono_emit_method_call (MonoCompile *cfg, MonoMethod *method, MonoInst **args, MonoInst *this)
{
	return mono_emit_method_call_full (cfg, method, mono_method_signature (method), FALSE, args, this, NULL, NULL);
}

MonoInst*
mono_emit_native_call (MonoCompile *cfg, gconstpointer func, MonoMethodSignature *sig,
					   MonoInst **args)
{
	MonoCallInst *call;

	g_assert (sig);

	call = mono_emit_call_args (cfg, sig, args, FALSE, FALSE, FALSE, FALSE, FALSE);
	call->fptr = func;

	MONO_ADD_INS (cfg->cbb, (MonoInst*)call);

	return (MonoInst*)call;
}

MonoInst*
mono_emit_jit_icall (MonoCompile *cfg, gconstpointer func, MonoInst **args)
{
	MonoJitICallInfo *info = mono_find_jit_icall_by_addr (func);

	g_assert (info);

	return mono_emit_native_call (cfg, mono_icall_get_wrapper (info), info->sig, args);
}

/*
 * mono_emit_abs_call:
 *
 *   Emit a call to the runtime function described by PATCH_TYPE and DATA.
 */
inline static MonoInst*
mono_emit_abs_call (MonoCompile *cfg, MonoJumpInfoType patch_type, gconstpointer data, 
					MonoMethodSignature *sig, MonoInst **args)
{
	MonoJumpInfo *ji = mono_patch_info_new (cfg->mempool, 0, patch_type, data);
	MonoInst *ins;

	/* 
	 * We pass ji as the call address, the PATCH_INFO_ABS resolving code will
	 * handle it.
	 */
	if (cfg->abs_patches == NULL)
		cfg->abs_patches = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (cfg->abs_patches, ji, ji);
	ins = mono_emit_native_call (cfg, ji, sig, args);
	((MonoCallInst*)ins)->fptr_is_patch = TRUE;
	return ins;
}
 
static MonoInst*
mono_emit_widen_call_res (MonoCompile *cfg, MonoInst *ins, MonoMethodSignature *fsig)
{
	if (!MONO_TYPE_IS_VOID (fsig->ret)) {
		if ((fsig->pinvoke || LLVM_ENABLED) && !fsig->ret->byref) {
			int widen_op = -1;

			/* 
			 * Native code might return non register sized integers 
			 * without initializing the upper bits.
			 */
			switch (mono_type_to_load_membase (cfg, fsig->ret)) {
			case OP_LOADI1_MEMBASE:
				widen_op = OP_ICONV_TO_I1;
				break;
			case OP_LOADU1_MEMBASE:
				widen_op = OP_ICONV_TO_U1;
				break;
			case OP_LOADI2_MEMBASE:
				widen_op = OP_ICONV_TO_I2;
				break;
			case OP_LOADU2_MEMBASE:
				widen_op = OP_ICONV_TO_U2;
				break;
			default:
				break;
			}

			if (widen_op != -1) {
				int dreg = alloc_preg (cfg);
				MonoInst *widen;

				EMIT_NEW_UNALU (cfg, widen, widen_op, dreg, ins->dreg);
				widen->type = ins->type;
				ins = widen;
			}
		}
	}

	return ins;
}

static MonoMethod*
get_memcpy_method (void)
{
	static MonoMethod *memcpy_method = NULL;
	if (!memcpy_method) {
		memcpy_method = mono_class_get_method_from_name (mono_defaults.string_class, "memcpy", 3);
		if (!memcpy_method)
			g_error ("Old corlib found. Install a new one");
	}
	return memcpy_method;
}

static void
create_write_barrier_bitmap (MonoCompile *cfg, MonoClass *klass, unsigned *wb_bitmap, int offset)
{
	MonoClassField *field;
	gpointer iter = NULL;

	while ((field = mono_class_get_fields (klass, &iter))) {
		int foffset;

		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		foffset = klass->valuetype ? field->offset - sizeof (MonoObject): field->offset;
		if (mini_type_is_reference (cfg, mono_field_get_type (field))) {
			g_assert ((foffset % SIZEOF_VOID_P) == 0);
			*wb_bitmap |= 1 << ((offset + foffset) / SIZEOF_VOID_P);
		} else {
			MonoClass *field_class = mono_class_from_mono_type (field->type);
			if (field_class->has_references)
				create_write_barrier_bitmap (cfg, field_class, wb_bitmap, offset + foffset);
		}
	}
}

static void
emit_write_barrier (MonoCompile *cfg, MonoInst *ptr, MonoInst *value)
{
	int card_table_shift_bits;
	gpointer card_table_mask;
	guint8 *card_table;
	MonoInst *dummy_use;
	int nursery_shift_bits;
	size_t nursery_size;
	gboolean has_card_table_wb = FALSE;

	if (!cfg->gen_write_barriers)
		return;

	card_table = mono_gc_get_card_table (&card_table_shift_bits, &card_table_mask);

	mono_gc_get_nursery (&nursery_shift_bits, &nursery_size);

#ifdef MONO_ARCH_HAVE_CARD_TABLE_WBARRIER
	has_card_table_wb = TRUE;
#endif

	if (has_card_table_wb && !cfg->compile_aot && card_table && nursery_shift_bits > 0 && !COMPILE_LLVM (cfg)) {
		MonoInst *wbarrier;

		MONO_INST_NEW (cfg, wbarrier, OP_CARD_TABLE_WBARRIER);
		wbarrier->sreg1 = ptr->dreg;
		wbarrier->sreg2 = value->dreg;
		MONO_ADD_INS (cfg->cbb, wbarrier);
	} else if (card_table) {
		int offset_reg = alloc_preg (cfg);
		int card_reg  = alloc_preg (cfg);
		MonoInst *ins;

		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHR_UN_IMM, offset_reg, ptr->dreg, card_table_shift_bits);
		if (card_table_mask)
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_PAND_IMM, offset_reg, offset_reg, card_table_mask);

		/*We can't use PADD_IMM since the cardtable might end up in high addresses and amd64 doesn't support
		 * IMM's larger than 32bits.
		 */
		if (cfg->compile_aot) {
			MONO_EMIT_NEW_AOTCONST (cfg, card_reg, NULL, MONO_PATCH_INFO_GC_CARD_TABLE_ADDR);
		} else {
			MONO_INST_NEW (cfg, ins, OP_PCONST);
			ins->inst_p0 = card_table;
			ins->dreg = card_reg;
			MONO_ADD_INS (cfg->cbb, ins);
		}

		MONO_EMIT_NEW_BIALU (cfg, OP_PADD, offset_reg, offset_reg, card_reg);
		MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREI1_MEMBASE_IMM, offset_reg, 0, 1);
	} else {
		MonoMethod *write_barrier = mono_gc_get_write_barrier ();
		mono_emit_method_call (cfg, write_barrier, &ptr, NULL);
	}

	EMIT_NEW_DUMMY_USE (cfg, dummy_use, value);
}

static gboolean
mono_emit_wb_aware_memcpy (MonoCompile *cfg, MonoClass *klass, MonoInst *iargs[4], int size, int align)
{
	int dest_ptr_reg, tmp_reg, destreg, srcreg, offset;
	unsigned need_wb = 0;

	if (align == 0)
		align = 4;

	/*types with references can't have alignment smaller than sizeof(void*) */
	if (align < SIZEOF_VOID_P)
		return FALSE;

	/*This value cannot be bigger than 32 due to the way we calculate the required wb bitmap.*/
	if (size > 32 * SIZEOF_VOID_P)
		return FALSE;

	create_write_barrier_bitmap (cfg, klass, &need_wb, 0);

	/* We don't unroll more than 5 stores to avoid code bloat. */
	if (size > 5 * SIZEOF_VOID_P) {
		/*This is harmless and simplify mono_gc_wbarrier_value_copy_bitmap */
		size += (SIZEOF_VOID_P - 1);
		size &= ~(SIZEOF_VOID_P - 1);

		EMIT_NEW_ICONST (cfg, iargs [2], size);
		EMIT_NEW_ICONST (cfg, iargs [3], need_wb);
		mono_emit_jit_icall (cfg, mono_gc_wbarrier_value_copy_bitmap, iargs);
		return TRUE;
	}

	destreg = iargs [0]->dreg;
	srcreg = iargs [1]->dreg;
	offset = 0;

	dest_ptr_reg = alloc_preg (cfg);
	tmp_reg = alloc_preg (cfg);

	/*tmp = dreg*/
	EMIT_NEW_UNALU (cfg, iargs [0], OP_MOVE, dest_ptr_reg, destreg);

	while (size >= SIZEOF_VOID_P) {
		MonoInst *load_inst;
		MONO_INST_NEW (cfg, load_inst, OP_LOAD_MEMBASE);
		load_inst->dreg = tmp_reg;
		load_inst->inst_basereg = srcreg;
		load_inst->inst_offset = offset;
		MONO_ADD_INS (cfg->cbb, load_inst);

		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREP_MEMBASE_REG, dest_ptr_reg, 0, tmp_reg);

		if (need_wb & 0x1)
			emit_write_barrier (cfg, iargs [0], load_inst);

		offset += SIZEOF_VOID_P;
		size -= SIZEOF_VOID_P;
		need_wb >>= 1;

		/*tmp += sizeof (void*)*/
		if (size >= SIZEOF_VOID_P) {
			NEW_BIALU_IMM (cfg, iargs [0], OP_PADD_IMM, dest_ptr_reg, dest_ptr_reg, SIZEOF_VOID_P);
			MONO_ADD_INS (cfg->cbb, iargs [0]);
		}
	}

	/* Those cannot be references since size < sizeof (void*) */
	while (size >= 4) {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, tmp_reg, srcreg, offset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, destreg, offset, tmp_reg);
		offset += 4;
		size -= 4;
	}

	while (size >= 2) {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI2_MEMBASE, tmp_reg, srcreg, offset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, destreg, offset, tmp_reg);
		offset += 2;
		size -= 2;
	}

	while (size >= 1) {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI1_MEMBASE, tmp_reg, srcreg, offset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, destreg, offset, tmp_reg);
		offset += 1;
		size -= 1;
	}

	return TRUE;
}

/*
 * Emit code to copy a valuetype of type @klass whose address is stored in
 * @src->dreg to memory whose address is stored at @dest->dreg.
 */
void
mini_emit_stobj (MonoCompile *cfg, MonoInst *dest, MonoInst *src, MonoClass *klass, gboolean native)
{
	MonoInst *iargs [4];
	int context_used, n;
	guint32 align = 0;
	MonoMethod *memcpy_method;
	MonoInst *size_ins = NULL;
	MonoInst *memcpy_ins = NULL;

	g_assert (klass);
	/*
	 * This check breaks with spilled vars... need to handle it during verification anyway.
	 * g_assert (klass && klass == src->klass && klass == dest->klass);
	 */

	if (mini_is_gsharedvt_klass (cfg, klass)) {
		g_assert (!native);
		context_used = mini_class_check_context_used (cfg, klass);
		size_ins = emit_get_gsharedvt_info_klass (cfg, klass, MONO_RGCTX_INFO_VALUE_SIZE);
		memcpy_ins = emit_get_gsharedvt_info_klass (cfg, klass, MONO_RGCTX_INFO_MEMCPY);
	}

	if (native)
		n = mono_class_native_size (klass, &align);
	else
		n = mono_class_value_size (klass, &align);

	/* if native is true there should be no references in the struct */
	if (cfg->gen_write_barriers && (klass->has_references || size_ins) && !native) {
		/* Avoid barriers when storing to the stack */
		if (!((dest->opcode == OP_ADD_IMM && dest->sreg1 == cfg->frame_reg) ||
			  (dest->opcode == OP_LDADDR))) {
			int context_used;

			iargs [0] = dest;
			iargs [1] = src;

			context_used = mini_class_check_context_used (cfg, klass);

			/* It's ok to intrinsify under gsharing since shared code types are layout stable. */
			if (!size_ins && (cfg->opt & MONO_OPT_INTRINS) && mono_emit_wb_aware_memcpy (cfg, klass, iargs, n, align)) {
				return;
			} else if (context_used) {
				iargs [2] = emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_KLASS);
			}  else {
				if (cfg->compile_aot) {
					EMIT_NEW_CLASSCONST (cfg, iargs [2], klass);
				} else {
					EMIT_NEW_PCONST (cfg, iargs [2], klass);
					mono_class_compute_gc_descriptor (klass);
				}
			}

			if (size_ins)
				mono_emit_jit_icall (cfg, mono_gsharedvt_value_copy, iargs);
			else
				mono_emit_jit_icall (cfg, mono_value_copy, iargs);
			return;
		}
	}

	if (!size_ins && (cfg->opt & MONO_OPT_INTRINS) && n <= sizeof (gpointer) * 5) {
		/* FIXME: Optimize the case when src/dest is OP_LDADDR */
		mini_emit_memcpy (cfg, dest->dreg, 0, src->dreg, 0, n, align);
	} else {
		iargs [0] = dest;
		iargs [1] = src;
		if (size_ins)
			iargs [2] = size_ins;
		else
			EMIT_NEW_ICONST (cfg, iargs [2], n);
		
		memcpy_method = get_memcpy_method ();
		if (memcpy_ins)
			mono_emit_calli (cfg, mono_method_signature (memcpy_method), iargs, memcpy_ins, NULL, NULL);
		else
			mono_emit_method_call (cfg, memcpy_method, iargs, NULL);
	}
}

static MonoMethod*
get_memset_method (void)
{
	static MonoMethod *memset_method = NULL;
	if (!memset_method) {
		memset_method = mono_class_get_method_from_name (mono_defaults.string_class, "memset", 3);
		if (!memset_method)
			g_error ("Old corlib found. Install a new one");
	}
	return memset_method;
}

void
mini_emit_initobj (MonoCompile *cfg, MonoInst *dest, const guchar *ip, MonoClass *klass)
{
	MonoInst *iargs [3];
	int n, context_used;
	guint32 align;
	MonoMethod *memset_method;
	MonoInst *size_ins = NULL;
	MonoInst *bzero_ins = NULL;
	static MonoMethod *bzero_method;

	/* FIXME: Optimize this for the case when dest is an LDADDR */

	mono_class_init (klass);
	if (mini_is_gsharedvt_klass (cfg, klass)) {
		context_used = mini_class_check_context_used (cfg, klass);
		size_ins = emit_get_gsharedvt_info_klass (cfg, klass, MONO_RGCTX_INFO_VALUE_SIZE);
		bzero_ins = emit_get_gsharedvt_info_klass (cfg, klass, MONO_RGCTX_INFO_BZERO);
		if (!bzero_method)
			bzero_method = mono_class_get_method_from_name (mono_defaults.string_class, "bzero_aligned_1", 2);
		g_assert (bzero_method);
		iargs [0] = dest;
		iargs [1] = size_ins;
		mono_emit_calli (cfg, mono_method_signature (bzero_method), iargs, bzero_ins, NULL, NULL);
		return;
	}

	n = mono_class_value_size (klass, &align);

	if (n <= sizeof (gpointer) * 5) {
		mini_emit_memset (cfg, dest->dreg, 0, n, 0, align);
	}
	else {
		memset_method = get_memset_method ();
		iargs [0] = dest;
		EMIT_NEW_ICONST (cfg, iargs [1], 0);
		EMIT_NEW_ICONST (cfg, iargs [2], n);
		mono_emit_method_call (cfg, memset_method, iargs, NULL);
	}
}

static MonoInst*
emit_get_rgctx (MonoCompile *cfg, MonoMethod *method, int context_used)
{
	MonoInst *this = NULL;

	g_assert (cfg->generic_sharing_context);

	if (!(method->flags & METHOD_ATTRIBUTE_STATIC) &&
			!(context_used & MONO_GENERIC_CONTEXT_USED_METHOD) &&
			!method->klass->valuetype)
		EMIT_NEW_ARGLOAD (cfg, this, 0);

	if (context_used & MONO_GENERIC_CONTEXT_USED_METHOD) {
		MonoInst *mrgctx_loc, *mrgctx_var;

		g_assert (!this);
		g_assert (method->is_inflated && mono_method_get_context (method)->method_inst);

		mrgctx_loc = mono_get_vtable_var (cfg);
		EMIT_NEW_TEMPLOAD (cfg, mrgctx_var, mrgctx_loc->inst_c0);

		return mrgctx_var;
	} else if (method->flags & METHOD_ATTRIBUTE_STATIC || method->klass->valuetype) {
		MonoInst *vtable_loc, *vtable_var;

		g_assert (!this);

		vtable_loc = mono_get_vtable_var (cfg);
		EMIT_NEW_TEMPLOAD (cfg, vtable_var, vtable_loc->inst_c0);

		if (method->is_inflated && mono_method_get_context (method)->method_inst) {
			MonoInst *mrgctx_var = vtable_var;
			int vtable_reg;

			vtable_reg = alloc_preg (cfg);
			EMIT_NEW_LOAD_MEMBASE (cfg, vtable_var, OP_LOAD_MEMBASE, vtable_reg, mrgctx_var->dreg, MONO_STRUCT_OFFSET (MonoMethodRuntimeGenericContext, class_vtable));
			vtable_var->type = STACK_PTR;
		}

		return vtable_var;
	} else {
		MonoInst *ins;
		int vtable_reg;
	
		vtable_reg = alloc_preg (cfg);
		EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, vtable_reg, this->dreg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		return ins;
	}
}

static MonoJumpInfoRgctxEntry *
mono_patch_info_rgctx_entry_new (MonoMemPool *mp, MonoMethod *method, gboolean in_mrgctx, MonoJumpInfoType patch_type, gconstpointer patch_data, MonoRgctxInfoType info_type)
{
	MonoJumpInfoRgctxEntry *res = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoRgctxEntry));
	res->method = method;
	res->in_mrgctx = in_mrgctx;
	res->data = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfo));
	res->data->type = patch_type;
	res->data->data.target = patch_data;
	res->info_type = info_type;

	return res;
}

static inline MonoInst*
emit_rgctx_fetch (MonoCompile *cfg, MonoInst *rgctx, MonoJumpInfoRgctxEntry *entry)
{
	return mono_emit_abs_call (cfg, MONO_PATCH_INFO_RGCTX_FETCH, entry, helper_sig_rgctx_lazy_fetch_trampoline, &rgctx);
}

static MonoInst*
emit_get_rgctx_klass (MonoCompile *cfg, int context_used,
					  MonoClass *klass, MonoRgctxInfoType rgctx_type)
{
	MonoJumpInfoRgctxEntry *entry = mono_patch_info_rgctx_entry_new (cfg->mempool, cfg->current_method, context_used & MONO_GENERIC_CONTEXT_USED_METHOD, MONO_PATCH_INFO_CLASS, klass, rgctx_type);
	MonoInst *rgctx = emit_get_rgctx (cfg, cfg->current_method, context_used);

	return emit_rgctx_fetch (cfg, rgctx, entry);
}

static MonoInst*
emit_get_rgctx_sig (MonoCompile *cfg, int context_used,
					MonoMethodSignature *sig, MonoRgctxInfoType rgctx_type)
{
	MonoJumpInfoRgctxEntry *entry = mono_patch_info_rgctx_entry_new (cfg->mempool, cfg->current_method, context_used & MONO_GENERIC_CONTEXT_USED_METHOD, MONO_PATCH_INFO_SIGNATURE, sig, rgctx_type);
	MonoInst *rgctx = emit_get_rgctx (cfg, cfg->current_method, context_used);

	return emit_rgctx_fetch (cfg, rgctx, entry);
}

static MonoInst*
emit_get_rgctx_gsharedvt_call (MonoCompile *cfg, int context_used,
							   MonoMethodSignature *sig, MonoMethod *cmethod, MonoRgctxInfoType rgctx_type)
{
	MonoJumpInfoGSharedVtCall *call_info;
	MonoJumpInfoRgctxEntry *entry;
	MonoInst *rgctx;

	call_info = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoJumpInfoGSharedVtCall));
	call_info->sig = sig;
	call_info->method = cmethod;

	entry = mono_patch_info_rgctx_entry_new (cfg->mempool, cfg->current_method, context_used & MONO_GENERIC_CONTEXT_USED_METHOD, MONO_PATCH_INFO_GSHAREDVT_CALL, call_info, rgctx_type);
	rgctx = emit_get_rgctx (cfg, cfg->current_method, context_used);

	return emit_rgctx_fetch (cfg, rgctx, entry);
}


static MonoInst*
emit_get_rgctx_gsharedvt_method (MonoCompile *cfg, int context_used,
								 MonoMethod *cmethod, MonoGSharedVtMethodInfo *info)
{
	MonoJumpInfoRgctxEntry *entry;
	MonoInst *rgctx;

	entry = mono_patch_info_rgctx_entry_new (cfg->mempool, cfg->current_method, context_used & MONO_GENERIC_CONTEXT_USED_METHOD, MONO_PATCH_INFO_GSHAREDVT_METHOD, info, MONO_RGCTX_INFO_METHOD_GSHAREDVT_INFO);
	rgctx = emit_get_rgctx (cfg, cfg->current_method, context_used);

	return emit_rgctx_fetch (cfg, rgctx, entry);
}

/*
 * emit_get_rgctx_method:
 *
 *   Emit IR to load the property RGCTX_TYPE of CMETHOD. If context_used is 0, emit
 * normal constants, else emit a load from the rgctx.
 */
static MonoInst*
emit_get_rgctx_method (MonoCompile *cfg, int context_used,
					   MonoMethod *cmethod, MonoRgctxInfoType rgctx_type)
{
	if (!context_used) {
		MonoInst *ins;

		switch (rgctx_type) {
		case MONO_RGCTX_INFO_METHOD:
			EMIT_NEW_METHODCONST (cfg, ins, cmethod);
			return ins;
		case MONO_RGCTX_INFO_METHOD_RGCTX:
			EMIT_NEW_METHOD_RGCTX_CONST (cfg, ins, cmethod);
			return ins;
		default:
			g_assert_not_reached ();
		}
	} else {
		MonoJumpInfoRgctxEntry *entry = mono_patch_info_rgctx_entry_new (cfg->mempool, cfg->current_method, context_used & MONO_GENERIC_CONTEXT_USED_METHOD, MONO_PATCH_INFO_METHODCONST, cmethod, rgctx_type);
		MonoInst *rgctx = emit_get_rgctx (cfg, cfg->current_method, context_used);

		return emit_rgctx_fetch (cfg, rgctx, entry);
	}
}

static MonoInst*
emit_get_rgctx_field (MonoCompile *cfg, int context_used,
					  MonoClassField *field, MonoRgctxInfoType rgctx_type)
{
	MonoJumpInfoRgctxEntry *entry = mono_patch_info_rgctx_entry_new (cfg->mempool, cfg->current_method, context_used & MONO_GENERIC_CONTEXT_USED_METHOD, MONO_PATCH_INFO_FIELD, field, rgctx_type);
	MonoInst *rgctx = emit_get_rgctx (cfg, cfg->current_method, context_used);

	return emit_rgctx_fetch (cfg, rgctx, entry);
}

static int
get_gsharedvt_info_slot (MonoCompile *cfg, gpointer data, MonoRgctxInfoType rgctx_type)
{
	MonoGSharedVtMethodInfo *info = cfg->gsharedvt_info;
	MonoRuntimeGenericContextInfoTemplate *template;
	int i, idx;

	g_assert (info);

	for (i = 0; i < info->num_entries; ++i) {
		MonoRuntimeGenericContextInfoTemplate *otemplate = &info->entries [i];

		if (otemplate->info_type == rgctx_type && otemplate->data == data && rgctx_type != MONO_RGCTX_INFO_LOCAL_OFFSET)
			return i;
	}

	if (info->num_entries == info->count_entries) {
		MonoRuntimeGenericContextInfoTemplate *new_entries;
		int new_count_entries = info->count_entries ? info->count_entries * 2 : 16;

		new_entries = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoRuntimeGenericContextInfoTemplate) * new_count_entries);

		memcpy (new_entries, info->entries, sizeof (MonoRuntimeGenericContextInfoTemplate) * info->count_entries);
		info->entries = new_entries;
		info->count_entries = new_count_entries;
	}

	idx = info->num_entries;
	template = &info->entries [idx];
	template->info_type = rgctx_type;
	template->data = data;

	info->num_entries ++;

	return idx;
}

/*
 * emit_get_gsharedvt_info:
 *
 *   This is similar to emit_get_rgctx_.., but loads the data from the gsharedvt info var instead of calling an rgctx fetch trampoline.
 */
static MonoInst*
emit_get_gsharedvt_info (MonoCompile *cfg, gpointer data, MonoRgctxInfoType rgctx_type)
{
	MonoInst *ins;
	int idx, dreg;

	idx = get_gsharedvt_info_slot (cfg, data, rgctx_type);
	/* Load info->entries [idx] */
	dreg = alloc_preg (cfg);
	EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, dreg, cfg->gsharedvt_info_var->dreg, MONO_STRUCT_OFFSET (MonoGSharedVtMethodRuntimeInfo, entries) + (idx * sizeof (gpointer)));

	return ins;
}

static MonoInst*
emit_get_gsharedvt_info_klass (MonoCompile *cfg, MonoClass *klass, MonoRgctxInfoType rgctx_type)
{
	return emit_get_gsharedvt_info (cfg, &klass->byval_arg, rgctx_type);
}

/*
 * On return the caller must check @klass for load errors.
 */
static void
emit_generic_class_init (MonoCompile *cfg, MonoClass *klass)
{
	MonoInst *vtable_arg;
	MonoCallInst *call;
	int context_used;

	context_used = mini_class_check_context_used (cfg, klass);

	if (context_used) {
		vtable_arg = emit_get_rgctx_klass (cfg, context_used,
										   klass, MONO_RGCTX_INFO_VTABLE);
	} else {
		MonoVTable *vtable = mono_class_vtable (cfg->domain, klass);

		if (!vtable)
			return;
		EMIT_NEW_VTABLECONST (cfg, vtable_arg, vtable);
	}

	if (COMPILE_LLVM (cfg))
		call = (MonoCallInst*)mono_emit_abs_call (cfg, MONO_PATCH_INFO_GENERIC_CLASS_INIT, NULL, helper_sig_generic_class_init_trampoline_llvm, &vtable_arg);
	else
		call = (MonoCallInst*)mono_emit_abs_call (cfg, MONO_PATCH_INFO_GENERIC_CLASS_INIT, NULL, helper_sig_generic_class_init_trampoline, &vtable_arg);
#ifdef MONO_ARCH_VTABLE_REG
	mono_call_inst_add_outarg_reg (cfg, call, vtable_arg->dreg, MONO_ARCH_VTABLE_REG, FALSE);
	cfg->uses_vtable_reg = TRUE;
#else
	NOT_IMPLEMENTED;
#endif
}

static void
emit_seq_point (MonoCompile *cfg, MonoMethod *method, guint8* ip, gboolean intr_loc, gboolean nonempty_stack)
{
	MonoInst *ins;

	if (cfg->gen_seq_points && cfg->method == method) {
		NEW_SEQ_POINT (cfg, ins, ip - cfg->header->code, intr_loc);
		if (nonempty_stack)
			ins->flags |= MONO_INST_NONEMPTY_STACK;
		MONO_ADD_INS (cfg->cbb, ins);
	}
}

static void
save_cast_details (MonoCompile *cfg, MonoClass *klass, int obj_reg, gboolean null_check, MonoBasicBlock **out_bblock)
{
	if (mini_get_debug_options ()->better_cast_details) {
		int vtable_reg = alloc_preg (cfg);
		int klass_reg = alloc_preg (cfg);
		MonoBasicBlock *is_null_bb = NULL;
		MonoInst *tls_get;
		int to_klass_reg, context_used;

		if (null_check) {
			NEW_BBLOCK (cfg, is_null_bb);

			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, obj_reg, 0);
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, is_null_bb);
		}

		tls_get = mono_get_jit_tls_intrinsic (cfg);
		if (!tls_get) {
			fprintf (stderr, "error: --debug=casts not supported on this platform.\n.");
			exit (1);
		}

		MONO_ADD_INS (cfg->cbb, tls_get);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, vtable_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));

		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, tls_get->dreg, MONO_STRUCT_OFFSET (MonoJitTlsData, class_cast_from), klass_reg);

		context_used = mini_class_check_context_used (cfg, klass);
		if (context_used) {
			MonoInst *class_ins;

			class_ins = emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_KLASS);
			to_klass_reg = class_ins->dreg;
		} else {
			to_klass_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_CLASSCONST (cfg, to_klass_reg, klass);
		}
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, tls_get->dreg, MONO_STRUCT_OFFSET (MonoJitTlsData, class_cast_to), to_klass_reg);

		if (null_check) {
			MONO_START_BB (cfg, is_null_bb);
			if (out_bblock)
				*out_bblock = cfg->cbb;
		}
	}
}

static void
reset_cast_details (MonoCompile *cfg)
{
	/* Reset the variables holding the cast details */
	if (mini_get_debug_options ()->better_cast_details) {
		MonoInst *tls_get = mono_get_jit_tls_intrinsic (cfg);

		MONO_ADD_INS (cfg->cbb, tls_get);
		/* It is enough to reset the from field */
		MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STORE_MEMBASE_IMM, tls_get->dreg, MONO_STRUCT_OFFSET (MonoJitTlsData, class_cast_from), 0);
	}
}

/*
 * On return the caller must check @array_class for load errors
 */
static void
mini_emit_check_array_type (MonoCompile *cfg, MonoInst *obj, MonoClass *array_class)
{
	int vtable_reg = alloc_preg (cfg);
	int context_used;

	context_used = mini_class_check_context_used (cfg, array_class);

	save_cast_details (cfg, array_class, obj->dreg, FALSE, NULL);

	MONO_EMIT_NEW_LOAD_MEMBASE_FAULT (cfg, vtable_reg, obj->dreg, MONO_STRUCT_OFFSET (MonoObject, vtable));

	if (cfg->opt & MONO_OPT_SHARED) {
		int class_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, class_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
		if (cfg->compile_aot) {
			int klass_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_CLASSCONST (cfg, klass_reg, array_class);
			MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, class_reg, klass_reg);
		} else {
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, class_reg, array_class);
		}
	} else if (context_used) {
		MonoInst *vtable_ins;

		vtable_ins = emit_get_rgctx_klass (cfg, context_used, array_class, MONO_RGCTX_INFO_VTABLE);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, vtable_reg, vtable_ins->dreg);
	} else {
		if (cfg->compile_aot) {
			int vt_reg;
			MonoVTable *vtable;

			if (!(vtable = mono_class_vtable (cfg->domain, array_class)))
				return;
			vt_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_VTABLECONST (cfg, vt_reg, vtable);
			MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, vtable_reg, vt_reg);
		} else {
			MonoVTable *vtable;
			if (!(vtable = mono_class_vtable (cfg->domain, array_class)))
				return;
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, vtable_reg, vtable);
		}
	}
	
	MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "ArrayTypeMismatchException");

	reset_cast_details (cfg);
}

/**
 * Handles unbox of a Nullable<T>. If context_used is non zero, then shared 
 * generic code is generated.
 */
static MonoInst*
handle_unbox_nullable (MonoCompile* cfg, MonoInst* val, MonoClass* klass, int context_used)
{
	MonoMethod* method = mono_class_get_method_from_name (klass, "Unbox", 1);

	if (context_used) {
		MonoInst *rgctx, *addr;

		/* FIXME: What if the class is shared?  We might not
		   have to get the address of the method from the
		   RGCTX. */
		addr = emit_get_rgctx_method (cfg, context_used, method,
									  MONO_RGCTX_INFO_GENERIC_METHOD_CODE);

		rgctx = emit_get_rgctx (cfg, cfg->current_method, context_used);

		return mono_emit_calli (cfg, mono_method_signature (method), &val, addr, NULL, rgctx);
	} else {
		gboolean pass_vtable, pass_mrgctx;
		MonoInst *rgctx_arg = NULL;

		check_method_sharing (cfg, method, &pass_vtable, &pass_mrgctx);
		g_assert (!pass_mrgctx);

		if (pass_vtable) {
			MonoVTable *vtable = mono_class_vtable (cfg->domain, method->klass);

			g_assert (vtable);
			EMIT_NEW_VTABLECONST (cfg, rgctx_arg, vtable);
		}

		return mono_emit_method_call_full (cfg, method, NULL, FALSE, &val, NULL, NULL, rgctx_arg);
	}
}

static MonoInst*
handle_unbox (MonoCompile *cfg, MonoClass *klass, MonoInst **sp, int context_used)
{
	MonoInst *add;
	int obj_reg;
	int vtable_reg = alloc_dreg (cfg ,STACK_PTR);
	int klass_reg = alloc_dreg (cfg ,STACK_PTR);
	int eclass_reg = alloc_dreg (cfg ,STACK_PTR);
	int rank_reg = alloc_dreg (cfg ,STACK_I4);

	obj_reg = sp [0]->dreg;
	MONO_EMIT_NEW_LOAD_MEMBASE_FAULT (cfg, vtable_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, rank_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, rank));

	/* FIXME: generics */
	g_assert (klass->rank == 0);
			
	// Check rank == 0
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, rank_reg, 0);
	MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");

	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, eclass_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, element_class));

	if (context_used) {
		MonoInst *element_class;

		/* This assertion is from the unboxcast insn */
		g_assert (klass->rank == 0);

		element_class = emit_get_rgctx_klass (cfg, context_used,
				klass->element_class, MONO_RGCTX_INFO_KLASS);

		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, eclass_reg, element_class->dreg);
		MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");
	} else {
		save_cast_details (cfg, klass->element_class, obj_reg, FALSE, NULL);
		mini_emit_class_check (cfg, eclass_reg, klass->element_class);
		reset_cast_details (cfg);
	}

	NEW_BIALU_IMM (cfg, add, OP_ADD_IMM, alloc_dreg (cfg, STACK_MP), obj_reg, sizeof (MonoObject));
	MONO_ADD_INS (cfg->cbb, add);
	add->type = STACK_MP;
	add->klass = klass;

	return add;
}

static MonoInst*
handle_unbox_gsharedvt (MonoCompile *cfg, MonoClass *klass, MonoInst *obj, MonoBasicBlock **out_cbb)
{
	MonoInst *addr, *klass_inst, *is_ref, *args[16];
	MonoBasicBlock *is_ref_bb, *is_nullable_bb, *end_bb;
	MonoInst *ins;
	int dreg, addr_reg;

	klass_inst = emit_get_gsharedvt_info_klass (cfg, klass, MONO_RGCTX_INFO_KLASS);

	/* obj */
	args [0] = obj;

	/* klass */
	args [1] = klass_inst;

	/* CASTCLASS */
	obj = mono_emit_jit_icall (cfg, mono_object_castclass_unbox, args);

	NEW_BBLOCK (cfg, is_ref_bb);
	NEW_BBLOCK (cfg, is_nullable_bb);
	NEW_BBLOCK (cfg, end_bb);
	is_ref = emit_get_gsharedvt_info_klass (cfg, klass, MONO_RGCTX_INFO_CLASS_BOX_TYPE);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, is_ref->dreg, 1);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, is_ref_bb);

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, is_ref->dreg, 2);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, is_nullable_bb);

	/* This will contain either the address of the unboxed vtype, or an address of the temporary where the ref is stored */
	addr_reg = alloc_dreg (cfg, STACK_MP);

	/* Non-ref case */
	/* UNBOX */
	NEW_BIALU_IMM (cfg, addr, OP_ADD_IMM, addr_reg, obj->dreg, sizeof (MonoObject));
	MONO_ADD_INS (cfg->cbb, addr);

	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

	/* Ref case */
	MONO_START_BB (cfg, is_ref_bb);

	/* Save the ref to a temporary */
	dreg = alloc_ireg (cfg);
	EMIT_NEW_VARLOADA_VREG (cfg, addr, dreg, &klass->byval_arg);
	addr->dreg = addr_reg;
	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, addr->dreg, 0, obj->dreg);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

	/* Nullable case */
	MONO_START_BB (cfg, is_nullable_bb);

	{
		MonoInst *addr = emit_get_gsharedvt_info_klass (cfg, klass, MONO_RGCTX_INFO_NULLABLE_CLASS_UNBOX);
		MonoInst *unbox_call;
		MonoMethodSignature *unbox_sig;
		MonoInst *var;

		var = mono_compile_create_var (cfg, &klass->byval_arg, OP_LOCAL);

		unbox_sig = mono_mempool_alloc0 (cfg->mempool, MONO_SIZEOF_METHOD_SIGNATURE + (1 * sizeof (MonoType *)));
		unbox_sig->ret = &klass->byval_arg;
		unbox_sig->param_count = 1;
		unbox_sig->params [0] = &mono_defaults.object_class->byval_arg;
		unbox_call = mono_emit_calli (cfg, unbox_sig, &obj, addr, NULL, NULL);

		EMIT_NEW_VARLOADA_VREG (cfg, addr, unbox_call->dreg, &klass->byval_arg);
		addr->dreg = addr_reg;
	}

	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

	/* End */
	MONO_START_BB (cfg, end_bb);

	/* LDOBJ */
	EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, addr_reg, 0);

	*out_cbb = cfg->cbb;

	return ins;
}

/*
 * Returns NULL and set the cfg exception on error.
 */
static MonoInst*
handle_alloc (MonoCompile *cfg, MonoClass *klass, gboolean for_box, int context_used)
{
	MonoInst *iargs [2];
	void *alloc_ftn;

	if (context_used) {
		MonoInst *data;
		int rgctx_info;
		MonoInst *iargs [2];

		MonoMethod *managed_alloc = mono_gc_get_managed_allocator (klass, for_box);

		if (cfg->opt & MONO_OPT_SHARED)
			rgctx_info = MONO_RGCTX_INFO_KLASS;
		else
			rgctx_info = MONO_RGCTX_INFO_VTABLE;
		data = emit_get_rgctx_klass (cfg, context_used, klass, rgctx_info);

		if (cfg->opt & MONO_OPT_SHARED) {
			EMIT_NEW_DOMAINCONST (cfg, iargs [0]);
			iargs [1] = data;
			alloc_ftn = mono_object_new;
		} else {
			iargs [0] = data;
			alloc_ftn = mono_object_new_specific;
		}

		if (managed_alloc && !(cfg->opt & MONO_OPT_SHARED))
			return mono_emit_method_call (cfg, managed_alloc, iargs, NULL);

		return mono_emit_jit_icall (cfg, alloc_ftn, iargs);
	}

	if (cfg->opt & MONO_OPT_SHARED) {
		EMIT_NEW_DOMAINCONST (cfg, iargs [0]);
		EMIT_NEW_CLASSCONST (cfg, iargs [1], klass);

		alloc_ftn = mono_object_new;
	} else if (cfg->compile_aot && cfg->cbb->out_of_line && klass->type_token && klass->image == mono_defaults.corlib && !klass->generic_class) {
		/* This happens often in argument checking code, eg. throw new FooException... */
		/* Avoid relocations and save some space by calling a helper function specialized to mscorlib */
		EMIT_NEW_ICONST (cfg, iargs [0], mono_metadata_token_index (klass->type_token));
		return mono_emit_jit_icall (cfg, mono_helper_newobj_mscorlib, iargs);
	} else {
		MonoVTable *vtable = mono_class_vtable (cfg->domain, klass);
		MonoMethod *managed_alloc = NULL;
		gboolean pass_lw;

		if (!vtable) {
			mono_cfg_set_exception (cfg, MONO_EXCEPTION_TYPE_LOAD);
			cfg->exception_ptr = klass;
			return NULL;
		}

#ifndef MONO_CROSS_COMPILE
		managed_alloc = mono_gc_get_managed_allocator (klass, for_box);
#endif

		if (managed_alloc) {
			EMIT_NEW_VTABLECONST (cfg, iargs [0], vtable);
			return mono_emit_method_call (cfg, managed_alloc, iargs, NULL);
		}
		alloc_ftn = mono_class_get_allocation_ftn (vtable, for_box, &pass_lw);
		if (pass_lw) {
			guint32 lw = vtable->klass->instance_size;
			lw = ((lw + (sizeof (gpointer) - 1)) & ~(sizeof (gpointer) - 1)) / sizeof (gpointer);
			EMIT_NEW_ICONST (cfg, iargs [0], lw);
			EMIT_NEW_VTABLECONST (cfg, iargs [1], vtable);
		}
		else {
			EMIT_NEW_VTABLECONST (cfg, iargs [0], vtable);
		}
	}

	return mono_emit_jit_icall (cfg, alloc_ftn, iargs);
}
	
/*
 * Returns NULL and set the cfg exception on error.
 */	
static MonoInst*
handle_box (MonoCompile *cfg, MonoInst *val, MonoClass *klass, int context_used, MonoBasicBlock **out_cbb)
{
	MonoInst *alloc, *ins;

	*out_cbb = cfg->cbb;

	if (mono_class_is_nullable (klass)) {
		MonoMethod* method = mono_class_get_method_from_name (klass, "Box", 1);

		if (context_used) {
			/* FIXME: What if the class is shared?  We might not
			   have to get the method address from the RGCTX. */
			MonoInst *addr = emit_get_rgctx_method (cfg, context_used, method,
													MONO_RGCTX_INFO_GENERIC_METHOD_CODE);
			MonoInst *rgctx = emit_get_rgctx (cfg, cfg->current_method, context_used);

			return mono_emit_calli (cfg, mono_method_signature (method), &val, addr, NULL, rgctx);
		} else {
			gboolean pass_vtable, pass_mrgctx;
			MonoInst *rgctx_arg = NULL;

			check_method_sharing (cfg, method, &pass_vtable, &pass_mrgctx);
			g_assert (!pass_mrgctx);

			if (pass_vtable) {
				MonoVTable *vtable = mono_class_vtable (cfg->domain, method->klass);

				g_assert (vtable);
				EMIT_NEW_VTABLECONST (cfg, rgctx_arg, vtable);
			}

			return mono_emit_method_call_full (cfg, method, NULL, FALSE, &val, NULL, NULL, rgctx_arg);
		}
	}

	if (mini_is_gsharedvt_klass (cfg, klass)) {
		MonoBasicBlock *is_ref_bb, *is_nullable_bb, *end_bb;
		MonoInst *res, *is_ref, *src_var, *addr;
		int addr_reg, dreg;

		dreg = alloc_ireg (cfg);

		NEW_BBLOCK (cfg, is_ref_bb);
		NEW_BBLOCK (cfg, is_nullable_bb);
		NEW_BBLOCK (cfg, end_bb);
		is_ref = emit_get_gsharedvt_info_klass (cfg, klass, MONO_RGCTX_INFO_CLASS_BOX_TYPE);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, is_ref->dreg, 1);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, is_ref_bb);

		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, is_ref->dreg, 2);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, is_nullable_bb);

		/* Non-ref case */
		alloc = handle_alloc (cfg, klass, TRUE, context_used);
		if (!alloc)
			return NULL;
		EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, alloc->dreg, sizeof (MonoObject), val->dreg);
		ins->opcode = OP_STOREV_MEMBASE;

		EMIT_NEW_UNALU (cfg, res, OP_MOVE, dreg, alloc->dreg);
		res->type = STACK_OBJ;
		res->klass = klass;
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);
		
		/* Ref case */
		MONO_START_BB (cfg, is_ref_bb);
		addr_reg = alloc_ireg (cfg);

		/* val is a vtype, so has to load the value manually */
		src_var = get_vreg_to_inst (cfg, val->dreg);
		if (!src_var)
			src_var = mono_compile_create_var_for_vreg (cfg, &klass->byval_arg, OP_LOCAL, val->dreg);
		EMIT_NEW_VARLOADA (cfg, addr, src_var, src_var->inst_vtype);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, addr->dreg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

		/* Nullable case */
		MONO_START_BB (cfg, is_nullable_bb);

		{
			MonoInst *addr = emit_get_gsharedvt_info_klass (cfg, klass,
													MONO_RGCTX_INFO_NULLABLE_CLASS_BOX);
			MonoInst *box_call;
			MonoMethodSignature *box_sig;

			/*
			 * klass is Nullable<T>, need to call Nullable<T>.Box () using a gsharedvt signature, but we cannot
			 * construct that method at JIT time, so have to do things by hand.
			 */
			box_sig = mono_mempool_alloc0 (cfg->mempool, MONO_SIZEOF_METHOD_SIGNATURE + (1 * sizeof (MonoType *)));
			box_sig->ret = &mono_defaults.object_class->byval_arg;
			box_sig->param_count = 1;
			box_sig->params [0] = &klass->byval_arg;
			box_call = mono_emit_calli (cfg, box_sig, &val, addr, NULL, NULL);
			EMIT_NEW_UNALU (cfg, res, OP_MOVE, dreg, box_call->dreg);
			res->type = STACK_OBJ;
			res->klass = klass;
		}

		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

		MONO_START_BB (cfg, end_bb);

		*out_cbb = cfg->cbb;

		return res;
	} else {
		alloc = handle_alloc (cfg, klass, TRUE, context_used);
		if (!alloc)
			return NULL;

		EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, alloc->dreg, sizeof (MonoObject), val->dreg);
		return alloc;
	}
}


static gboolean
mini_class_has_reference_variant_generic_argument (MonoCompile *cfg, MonoClass *klass, int context_used)
{
	int i;
	MonoGenericContainer *container;
	MonoGenericInst *ginst;

	if (klass->generic_class) {
		container = klass->generic_class->container_class->generic_container;
		ginst = klass->generic_class->context.class_inst;
	} else if (klass->generic_container && context_used) {
		container = klass->generic_container;
		ginst = container->context.class_inst;
	} else {
		return FALSE;
	}

	for (i = 0; i < container->type_argc; ++i) {
		MonoType *type;
		if (!(mono_generic_container_get_param_info (container, i)->flags & (MONO_GEN_PARAM_VARIANT|MONO_GEN_PARAM_COVARIANT)))
			continue;
		type = ginst->type_argv [i];
		if (mini_type_is_reference (cfg, type))
			return TRUE;
	}
	return FALSE;
}

#define is_complex_isinst(klass) ((klass->flags & TYPE_ATTRIBUTE_INTERFACE) || klass->rank || mono_class_is_nullable (klass) || mono_class_is_marshalbyref (klass) || (klass->flags & TYPE_ATTRIBUTE_SEALED) || klass->byval_arg.type == MONO_TYPE_VAR || klass->byval_arg.type == MONO_TYPE_MVAR)

static MonoInst*
emit_castclass_with_cache (MonoCompile *cfg, MonoClass *klass, MonoInst **args, MonoBasicBlock **out_bblock)
{
	MonoMethod *mono_castclass;
	MonoInst *res;

	mono_castclass = mono_marshal_get_castclass_with_cache ();

	save_cast_details (cfg, klass, args [0]->dreg, TRUE, out_bblock);
	res = mono_emit_method_call (cfg, mono_castclass, args, NULL);
	reset_cast_details (cfg);
	*out_bblock = cfg->cbb;

	return res;
}

static MonoInst*
emit_castclass_with_cache_nonshared (MonoCompile *cfg, MonoInst *obj, MonoClass *klass, MonoBasicBlock **out_bblock)
{
	MonoInst *args [3];
	int idx;

	/* obj */
	args [0] = obj;

	/* klass */
	EMIT_NEW_CLASSCONST (cfg, args [1], klass);

	/* inline cache*/
	if (cfg->compile_aot) {
		/* Each CASTCLASS_CACHE patch needs a unique index which identifies the call site */
		cfg->castclass_cache_index ++;
		idx = (cfg->method_index << 16) | cfg->castclass_cache_index;
		EMIT_NEW_AOTCONST (cfg, args [2], MONO_PATCH_INFO_CASTCLASS_CACHE, GINT_TO_POINTER (idx));
	} else {
		EMIT_NEW_PCONST (cfg, args [2], mono_domain_alloc0 (cfg->domain, sizeof (gpointer)));
	}

	/*The wrapper doesn't inline well so the bloat of inlining doesn't pay off.*/

	return emit_castclass_with_cache (cfg, klass, args, out_bblock);
}

/*
 * Returns NULL and set the cfg exception on error.
 */
static MonoInst*
handle_castclass (MonoCompile *cfg, MonoClass *klass, MonoInst *src, guint8 *ip, MonoBasicBlock **out_bb, int *inline_costs)
{
	MonoBasicBlock *is_null_bb;
	int obj_reg = src->dreg;
	int vtable_reg = alloc_preg (cfg);
	int context_used;
	MonoInst *klass_inst = NULL, *res;
	MonoBasicBlock *bblock;

	*out_bb = cfg->cbb;

	context_used = mini_class_check_context_used (cfg, klass);

	if (!context_used && mini_class_has_reference_variant_generic_argument (cfg, klass, context_used)) {
		res = emit_castclass_with_cache_nonshared (cfg, src, klass, &bblock);
		(*inline_costs) += 2;
		*out_bb = cfg->cbb;
		return res;
	} else if (!context_used && (mono_class_is_marshalbyref (klass) || klass->flags & TYPE_ATTRIBUTE_INTERFACE)) {
		MonoMethod *mono_castclass;
		MonoInst *iargs [1];
		int costs;

		mono_castclass = mono_marshal_get_castclass (klass); 
		iargs [0] = src;
				
		save_cast_details (cfg, klass, src->dreg, TRUE, &bblock);
		costs = inline_method (cfg, mono_castclass, mono_method_signature (mono_castclass), 
							   iargs, ip, cfg->real_offset, TRUE, &bblock);
		reset_cast_details (cfg);
		CHECK_CFG_EXCEPTION;
		g_assert (costs > 0);
				
		cfg->real_offset += 5;

		(*inline_costs) += costs;

		*out_bb = cfg->cbb;
		return src;
	}

	if (context_used) {
		MonoInst *args [3];

		if(mini_class_has_reference_variant_generic_argument (cfg, klass, context_used) || is_complex_isinst (klass)) {
			MonoInst *cache_ins;

			cache_ins = emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_CAST_CACHE);

			/* obj */
			args [0] = src;

			/* klass - it's the second element of the cache entry*/
			EMIT_NEW_LOAD_MEMBASE (cfg, args [1], OP_LOAD_MEMBASE, alloc_preg (cfg), cache_ins->dreg, sizeof (gpointer));

			/* cache */
			args [2] = cache_ins;

			return emit_castclass_with_cache (cfg, klass, args, out_bb);
		}

		klass_inst = emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_KLASS);
	}

	NEW_BBLOCK (cfg, is_null_bb);

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, obj_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, is_null_bb);

	save_cast_details (cfg, klass, obj_reg, FALSE, NULL);

	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, vtable_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mini_emit_iface_cast (cfg, vtable_reg, klass, NULL, NULL);
	} else {
		int klass_reg = alloc_preg (cfg);

		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, vtable_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));

		if (!klass->rank && !cfg->compile_aot && !(cfg->opt & MONO_OPT_SHARED) && (klass->flags & TYPE_ATTRIBUTE_SEALED)) {
			/* the remoting code is broken, access the class for now */
			if (0) { /*FIXME what exactly is broken? This change refers to r39380 from 2005 and mention some remoting fixes were due.*/
				MonoVTable *vt = mono_class_vtable (cfg->domain, klass);
				if (!vt) {
					mono_cfg_set_exception (cfg, MONO_EXCEPTION_TYPE_LOAD);
					cfg->exception_ptr = klass;
					return NULL;
				}
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, vtable_reg, vt);
			} else {
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, klass_reg, klass);
			}
			MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");
		} else {
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
			mini_emit_castclass_inst (cfg, obj_reg, klass_reg, klass, klass_inst, is_null_bb);
		}
	}

	MONO_START_BB (cfg, is_null_bb);

	reset_cast_details (cfg);

	*out_bb = cfg->cbb;

	return src;

exception_exit:
	return NULL;
}

/*
 * Returns NULL and set the cfg exception on error.
 */
static MonoInst*
handle_isinst (MonoCompile *cfg, MonoClass *klass, MonoInst *src, int context_used)
{
	MonoInst *ins;
	MonoBasicBlock *is_null_bb, *false_bb, *end_bb;
	int obj_reg = src->dreg;
	int vtable_reg = alloc_preg (cfg);
	int res_reg = alloc_ireg_ref (cfg);
	MonoInst *klass_inst = NULL;

	if (context_used) {
		MonoInst *args [3];

		if(mini_class_has_reference_variant_generic_argument (cfg, klass, context_used) || is_complex_isinst (klass)) {
			MonoMethod *mono_isinst = mono_marshal_get_isinst_with_cache ();
			MonoInst *cache_ins;

			cache_ins = emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_CAST_CACHE);

			/* obj */
			args [0] = src;

			/* klass - it's the second element of the cache entry*/
			EMIT_NEW_LOAD_MEMBASE (cfg, args [1], OP_LOAD_MEMBASE, alloc_preg (cfg), cache_ins->dreg, sizeof (gpointer));

			/* cache */
			args [2] = cache_ins;

			return mono_emit_method_call (cfg, mono_isinst, args, NULL);
		}

		klass_inst = emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_KLASS);
	}

	NEW_BBLOCK (cfg, is_null_bb);
	NEW_BBLOCK (cfg, false_bb);
	NEW_BBLOCK (cfg, end_bb);

	/* Do the assignment at the beginning, so the other assignment can be if converted */
	EMIT_NEW_UNALU (cfg, ins, OP_MOVE, res_reg, obj_reg);
	ins->type = STACK_OBJ;
	ins->klass = klass;

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, obj_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, is_null_bb);

	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, vtable_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));

	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		g_assert (!context_used);
		/* the is_null_bb target simply copies the input register to the output */
		mini_emit_iface_cast (cfg, vtable_reg, klass, false_bb, is_null_bb);
	} else {
		int klass_reg = alloc_preg (cfg);

		if (klass->rank) {
			int rank_reg = alloc_preg (cfg);
			int eclass_reg = alloc_preg (cfg);

			g_assert (!context_used);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, rank_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, rank));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, rank_reg, klass->rank);
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, false_bb);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, eclass_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, cast_class));
			if (klass->cast_class == mono_defaults.object_class) {
				int parent_reg = alloc_preg (cfg);
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, parent_reg, eclass_reg, MONO_STRUCT_OFFSET (MonoClass, parent));
				mini_emit_class_check_branch (cfg, parent_reg, mono_defaults.enum_class->parent, OP_PBNE_UN, is_null_bb);
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class, OP_PBEQ, is_null_bb);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);
			} else if (klass->cast_class == mono_defaults.enum_class->parent) {
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class->parent, OP_PBEQ, is_null_bb);
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class, OP_PBEQ, is_null_bb);				
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);
			} else if (klass->cast_class == mono_defaults.enum_class) {
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class, OP_PBEQ, is_null_bb);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);
			} else if (klass->cast_class->flags & TYPE_ATTRIBUTE_INTERFACE) {
				mini_emit_iface_class_cast (cfg, eclass_reg, klass->cast_class, false_bb, is_null_bb);
			} else {
				if ((klass->rank == 1) && (klass->byval_arg.type == MONO_TYPE_SZARRAY)) {
					/* Check that the object is a vector too */
					int bounds_reg = alloc_preg (cfg);
					MONO_EMIT_NEW_LOAD_MEMBASE (cfg, bounds_reg, obj_reg, MONO_STRUCT_OFFSET (MonoArray, bounds));
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, bounds_reg, 0);
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, false_bb);
				}

				/* the is_null_bb target simply copies the input register to the output */
				mini_emit_isninst_cast (cfg, eclass_reg, klass->cast_class, false_bb, is_null_bb);
			}
		} else if (mono_class_is_nullable (klass)) {
			g_assert (!context_used);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
			/* the is_null_bb target simply copies the input register to the output */
			mini_emit_isninst_cast (cfg, klass_reg, klass->cast_class, false_bb, is_null_bb);
		} else {
			if (!cfg->compile_aot && !(cfg->opt & MONO_OPT_SHARED) && (klass->flags & TYPE_ATTRIBUTE_SEALED)) {
				g_assert (!context_used);
				/* the remoting code is broken, access the class for now */
				if (0) {/*FIXME what exactly is broken? This change refers to r39380 from 2005 and mention some remoting fixes were due.*/
					MonoVTable *vt = mono_class_vtable (cfg->domain, klass);
					if (!vt) {
						mono_cfg_set_exception (cfg, MONO_EXCEPTION_TYPE_LOAD);
						cfg->exception_ptr = klass;
						return NULL;
					}
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, vtable_reg, vt);
				} else {
					MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, klass_reg, klass);
				}
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, false_bb);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, is_null_bb);
			} else {
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
				/* the is_null_bb target simply copies the input register to the output */
				mini_emit_isninst_cast_inst (cfg, klass_reg, klass, klass_inst, false_bb, is_null_bb);
			}
		}
	}

	MONO_START_BB (cfg, false_bb);

	MONO_EMIT_NEW_PCONST (cfg, res_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

	MONO_START_BB (cfg, is_null_bb);

	MONO_START_BB (cfg, end_bb);

	return ins;
}

static MonoInst*
handle_cisinst (MonoCompile *cfg, MonoClass *klass, MonoInst *src)
{
	/* This opcode takes as input an object reference and a class, and returns:
	0) if the object is an instance of the class,
	1) if the object is not instance of the class,
	2) if the object is a proxy whose type cannot be determined */

	MonoInst *ins;
#ifndef DISABLE_REMOTING
	MonoBasicBlock *true_bb, *false_bb, *false2_bb, *end_bb, *no_proxy_bb, *interface_fail_bb;
#else
	MonoBasicBlock *true_bb, *false_bb, *end_bb;
#endif
	int obj_reg = src->dreg;
	int dreg = alloc_ireg (cfg);
	int tmp_reg;
#ifndef DISABLE_REMOTING
	int klass_reg = alloc_preg (cfg);
#endif

	NEW_BBLOCK (cfg, true_bb);
	NEW_BBLOCK (cfg, false_bb);
	NEW_BBLOCK (cfg, end_bb);
#ifndef DISABLE_REMOTING
	NEW_BBLOCK (cfg, false2_bb);
	NEW_BBLOCK (cfg, no_proxy_bb);
#endif

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, obj_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, false_bb);

	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
#ifndef DISABLE_REMOTING
		NEW_BBLOCK (cfg, interface_fail_bb);
#endif

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
#ifndef DISABLE_REMOTING
		mini_emit_iface_cast (cfg, tmp_reg, klass, interface_fail_bb, true_bb);
		MONO_START_BB (cfg, interface_fail_bb);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
		
		mini_emit_class_check_branch (cfg, klass_reg, mono_defaults.transparent_proxy_class, OP_PBNE_UN, false_bb);

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, custom_type_info));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_reg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, false2_bb);		
#else
		mini_emit_iface_cast (cfg, tmp_reg, klass, false_bb, true_bb);
#endif
	} else {
#ifndef DISABLE_REMOTING
		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));

		mini_emit_class_check_branch (cfg, klass_reg, mono_defaults.transparent_proxy_class, OP_PBNE_UN, no_proxy_bb);		
		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, remote_class));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoRemoteClass, proxy_class));

		tmp_reg = alloc_preg (cfg);		
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, custom_type_info));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_reg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, no_proxy_bb);
		
		mini_emit_isninst_cast (cfg, klass_reg, klass, false2_bb, true_bb);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false2_bb);

		MONO_START_BB (cfg, no_proxy_bb);

		mini_emit_isninst_cast (cfg, klass_reg, klass, false_bb, true_bb);
#else
		g_error ("transparent proxy support is disabled while trying to JIT code that uses it");
#endif
	}

	MONO_START_BB (cfg, false_bb);

	MONO_EMIT_NEW_ICONST (cfg, dreg, 1);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

#ifndef DISABLE_REMOTING
	MONO_START_BB (cfg, false2_bb);

	MONO_EMIT_NEW_ICONST (cfg, dreg, 2);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);
#endif

	MONO_START_BB (cfg, true_bb);

	MONO_EMIT_NEW_ICONST (cfg, dreg, 0);

	MONO_START_BB (cfg, end_bb);

	/* FIXME: */
	MONO_INST_NEW (cfg, ins, OP_ICONST);
	ins->dreg = dreg;
	ins->type = STACK_I4;

	return ins;
}

static MonoInst*
handle_ccastclass (MonoCompile *cfg, MonoClass *klass, MonoInst *src)
{
	/* This opcode takes as input an object reference and a class, and returns:
	0) if the object is an instance of the class,
	1) if the object is a proxy whose type cannot be determined
	an InvalidCastException exception is thrown otherwhise*/
	
	MonoInst *ins;
#ifndef DISABLE_REMOTING
	MonoBasicBlock *end_bb, *ok_result_bb, *no_proxy_bb, *interface_fail_bb, *fail_1_bb;
#else
	MonoBasicBlock *ok_result_bb;
#endif
	int obj_reg = src->dreg;
	int dreg = alloc_ireg (cfg);
	int tmp_reg = alloc_preg (cfg);

#ifndef DISABLE_REMOTING
	int klass_reg = alloc_preg (cfg);
	NEW_BBLOCK (cfg, end_bb);
#endif

	NEW_BBLOCK (cfg, ok_result_bb);

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, obj_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, ok_result_bb);

	save_cast_details (cfg, klass, obj_reg, FALSE, NULL);

	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
#ifndef DISABLE_REMOTING
		NEW_BBLOCK (cfg, interface_fail_bb);
	
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mini_emit_iface_cast (cfg, tmp_reg, klass, interface_fail_bb, ok_result_bb);
		MONO_START_BB (cfg, interface_fail_bb);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));

		mini_emit_class_check (cfg, klass_reg, mono_defaults.transparent_proxy_class);

		tmp_reg = alloc_preg (cfg);		
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, custom_type_info));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_reg, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, EQ, "InvalidCastException");
		
		MONO_EMIT_NEW_ICONST (cfg, dreg, 1);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);
#else
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mini_emit_iface_cast (cfg, tmp_reg, klass, NULL, NULL);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, ok_result_bb);
#endif
	} else {
#ifndef DISABLE_REMOTING
		NEW_BBLOCK (cfg, no_proxy_bb);

		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
		mini_emit_class_check_branch (cfg, klass_reg, mono_defaults.transparent_proxy_class, OP_PBNE_UN, no_proxy_bb);		

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, remote_class));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoRemoteClass, proxy_class));

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, custom_type_info));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_reg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, no_proxy_bb);

		NEW_BBLOCK (cfg, fail_1_bb);
		
		mini_emit_isninst_cast (cfg, klass_reg, klass, fail_1_bb, ok_result_bb);

		MONO_START_BB (cfg, fail_1_bb);

		MONO_EMIT_NEW_ICONST (cfg, dreg, 1);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

		MONO_START_BB (cfg, no_proxy_bb);

		mini_emit_castclass (cfg, obj_reg, klass_reg, klass, ok_result_bb);
#else
		g_error ("Transparent proxy support is disabled while trying to JIT code that uses it");
#endif
	}

	MONO_START_BB (cfg, ok_result_bb);

	MONO_EMIT_NEW_ICONST (cfg, dreg, 0);

#ifndef DISABLE_REMOTING
	MONO_START_BB (cfg, end_bb);
#endif

	/* FIXME: */
	MONO_INST_NEW (cfg, ins, OP_ICONST);
	ins->dreg = dreg;
	ins->type = STACK_I4;

	return ins;
}

/*
 * Returns NULL and set the cfg exception on error.
 */
static G_GNUC_UNUSED MonoInst*
handle_delegate_ctor (MonoCompile *cfg, MonoClass *klass, MonoInst *target, MonoMethod *method, int context_used, gboolean virtual)
{
	MonoInst *ptr;
	int dreg;
	gpointer trampoline;
	MonoInst *obj, *method_ins, *tramp_ins;
	MonoDomain *domain;
	guint8 **code_slot;
	
	// FIXME reenable optimisation for virtual case
	if (virtual)
		return NULL;

	if (virtual) {
		MonoMethod *invoke = mono_get_delegate_invoke (klass);
		g_assert (invoke);

		if (!mono_get_delegate_virtual_invoke_impl (mono_method_signature (invoke), context_used ? NULL : method))
			return NULL;
	}

	obj = handle_alloc (cfg, klass, FALSE, 0);
	if (!obj)
		return NULL;

	/* Inline the contents of mono_delegate_ctor */

	/* Set target field */
	/* Optimize away setting of NULL target */
	if (!(target->opcode == OP_PCONST && target->inst_p0 == 0)) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, obj->dreg, MONO_STRUCT_OFFSET (MonoDelegate, target), target->dreg);
		if (cfg->gen_write_barriers) {
			dreg = alloc_preg (cfg);
			EMIT_NEW_BIALU_IMM (cfg, ptr, OP_PADD_IMM, dreg, obj->dreg, MONO_STRUCT_OFFSET (MonoDelegate, target));
			emit_write_barrier (cfg, ptr, target);
		}
	}

	/* Set method field */
	method_ins = emit_get_rgctx_method (cfg, context_used, method, MONO_RGCTX_INFO_METHOD);
	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, obj->dreg, MONO_STRUCT_OFFSET (MonoDelegate, method), method_ins->dreg);

	/* 
	 * To avoid looking up the compiled code belonging to the target method
	 * in mono_delegate_trampoline (), we allocate a per-domain memory slot to
	 * store it, and we fill it after the method has been compiled.
	 */
	if (!method->dynamic && !(cfg->opt & MONO_OPT_SHARED)) {
		MonoInst *code_slot_ins;

		if (context_used) {
			code_slot_ins = emit_get_rgctx_method (cfg, context_used, method, MONO_RGCTX_INFO_METHOD_DELEGATE_CODE);
		} else {
			domain = mono_domain_get ();
			mono_domain_lock (domain);
			if (!domain_jit_info (domain)->method_code_hash)
				domain_jit_info (domain)->method_code_hash = g_hash_table_new (NULL, NULL);
			code_slot = g_hash_table_lookup (domain_jit_info (domain)->method_code_hash, method);
			if (!code_slot) {
				code_slot = mono_domain_alloc0 (domain, sizeof (gpointer));
				g_hash_table_insert (domain_jit_info (domain)->method_code_hash, method, code_slot);
			}
			mono_domain_unlock (domain);

			if (cfg->compile_aot)
				EMIT_NEW_AOTCONST (cfg, code_slot_ins, MONO_PATCH_INFO_METHOD_CODE_SLOT, method);
			else
				EMIT_NEW_PCONST (cfg, code_slot_ins, code_slot);
		}
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, obj->dreg, MONO_STRUCT_OFFSET (MonoDelegate, method_code), code_slot_ins->dreg);		
	}

	if (cfg->compile_aot) {
		MonoDelegateClassMethodPair *del_tramp;

		del_tramp = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoDelegateClassMethodPair));
		del_tramp->klass = klass;
		del_tramp->method = context_used ? NULL : method;
		del_tramp->virtual = virtual;
		EMIT_NEW_AOTCONST (cfg, tramp_ins, MONO_PATCH_INFO_DELEGATE_TRAMPOLINE, del_tramp);
	} else {
		if (virtual)
			trampoline = mono_create_delegate_virtual_trampoline (cfg->domain, klass, context_used ? NULL : method);
		else
			trampoline = mono_create_delegate_trampoline_info (cfg->domain, klass, context_used ? NULL : method);
		EMIT_NEW_PCONST (cfg, tramp_ins, trampoline);
	}

	/* Set invoke_impl field */
	if (virtual) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, obj->dreg, MONO_STRUCT_OFFSET (MonoDelegate, invoke_impl), tramp_ins->dreg);
	} else {
		dreg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, tramp_ins->dreg, MONO_STRUCT_OFFSET (MonoDelegateTrampInfo, invoke_impl));
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, obj->dreg, MONO_STRUCT_OFFSET (MonoDelegate, invoke_impl), dreg);

		dreg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, tramp_ins->dreg, MONO_STRUCT_OFFSET (MonoDelegateTrampInfo, method_ptr));
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, obj->dreg, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr), dreg);
	}

	/* All the checks which are in mono_delegate_ctor () are done by the delegate trampoline */

	return obj;
}

static MonoInst*
handle_array_new (MonoCompile *cfg, int rank, MonoInst **sp, unsigned char *ip)
{
	MonoJitICallInfo *info;

	/* Need to register the icall so it gets an icall wrapper */
	info = mono_get_array_new_va_icall (rank);

	cfg->flags |= MONO_CFG_HAS_VARARGS;

	/* mono_array_new_va () needs a vararg calling convention */
	cfg->disable_llvm = TRUE;

	/* FIXME: This uses info->sig, but it should use the signature of the wrapper */
	return mono_emit_native_call (cfg, mono_icall_get_wrapper (info), info->sig, sp);
}

static void
mono_emit_load_got_addr (MonoCompile *cfg)
{
	MonoInst *getaddr, *dummy_use;

	if (!cfg->got_var || cfg->got_var_allocated)
		return;

	MONO_INST_NEW (cfg, getaddr, OP_LOAD_GOTADDR);
	getaddr->cil_code = cfg->header->code;
	getaddr->dreg = cfg->got_var->dreg;

	/* Add it to the start of the first bblock */
	if (cfg->bb_entry->code) {
		getaddr->next = cfg->bb_entry->code;
		cfg->bb_entry->code = getaddr;
	}
	else
		MONO_ADD_INS (cfg->bb_entry, getaddr);

	cfg->got_var_allocated = TRUE;

	/* 
	 * Add a dummy use to keep the got_var alive, since real uses might
	 * only be generated by the back ends.
	 * Add it to end_bblock, so the variable's lifetime covers the whole
	 * method.
	 * It would be better to make the usage of the got var explicit in all
	 * cases when the backend needs it (i.e. calls, throw etc.), so this
	 * wouldn't be needed.
	 */
	NEW_DUMMY_USE (cfg, dummy_use, cfg->got_var);
	MONO_ADD_INS (cfg->bb_exit, dummy_use);
}

static int inline_limit;
static gboolean inline_limit_inited;

static gboolean
mono_method_check_inlining (MonoCompile *cfg, MonoMethod *method)
{
	MonoMethodHeaderSummary header;
	MonoVTable *vtable;
#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
	MonoMethodSignature *sig = mono_method_signature (method);
	int i;
#endif

	if (cfg->disable_inline)
		return FALSE;
	if (cfg->generic_sharing_context)
		return FALSE;

	if (cfg->inline_depth > 10)
		return FALSE;

#ifdef MONO_ARCH_HAVE_LMF_OPS
	if (((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		 (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) &&
	    !MONO_TYPE_ISSTRUCT (signature->ret) && !mini_class_is_system_array (method->klass))
		return TRUE;
#endif


	if (!mono_method_get_header_summary (method, &header))
		return FALSE;

	/*runtime, icall and pinvoke are checked by summary call*/
	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_NOINLINING) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) ||
	    (mono_class_is_marshalbyref (method->klass)) ||
	    header.has_clauses)
		return FALSE;

	/* also consider num_locals? */
	/* Do the size check early to avoid creating vtables */
	if (!inline_limit_inited) {
		if (g_getenv ("MONO_INLINELIMIT"))
			inline_limit = atoi (g_getenv ("MONO_INLINELIMIT"));
		else
			inline_limit = INLINE_LENGTH_LIMIT;
		inline_limit_inited = TRUE;
	}
	if (header.code_size >= inline_limit && !(method->iflags & METHOD_IMPL_ATTRIBUTE_AGGRESSIVE_INLINING))
		return FALSE;

	/*
	 * if we can initialize the class of the method right away, we do,
	 * otherwise we don't allow inlining if the class needs initialization,
	 * since it would mean inserting a call to mono_runtime_class_init()
	 * inside the inlined code
	 */
	if (!(cfg->opt & MONO_OPT_SHARED)) {
		/* The AggressiveInlining hint is a good excuse to force that cctor to run. */
		if (method->iflags & METHOD_IMPL_ATTRIBUTE_AGGRESSIVE_INLINING) {
			vtable = mono_class_vtable (cfg->domain, method->klass);
			if (!vtable)
				return FALSE;
			if (!cfg->compile_aot)
				mono_runtime_class_init (vtable);
		} else if (method->klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT) {
			if (cfg->run_cctors && method->klass->has_cctor) {
				/*FIXME it would easier and lazier to just use mono_class_try_get_vtable */
				if (!method->klass->runtime_info)
					/* No vtable created yet */
					return FALSE;
				vtable = mono_class_vtable (cfg->domain, method->klass);
				if (!vtable)
					return FALSE;
				/* This makes so that inline cannot trigger */
				/* .cctors: too many apps depend on them */
				/* running with a specific order... */
				if (! vtable->initialized)
					return FALSE;
				mono_runtime_class_init (vtable);
			}
		} else if (mono_class_needs_cctor_run (method->klass, NULL)) {
			if (!method->klass->runtime_info)
				/* No vtable created yet */
				return FALSE;
			vtable = mono_class_vtable (cfg->domain, method->klass);
			if (!vtable)
				return FALSE;
			if (!vtable->initialized)
				return FALSE;
		}
	} else {
		/* 
		 * If we're compiling for shared code
		 * the cctor will need to be run at aot method load time, for example,
		 * or at the end of the compilation of the inlining method.
		 */
		if (mono_class_needs_cctor_run (method->klass, NULL) && !((method->klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT)))
			return FALSE;
	}

	/*
	 * CAS - do not inline methods with declarative security
	 * Note: this has to be before any possible return TRUE;
	 */
	if (mono_security_method_has_declsec (method))
		return FALSE;

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
	if (mono_arch_is_soft_float ()) {
		/* FIXME: */
		if (sig->ret && sig->ret->type == MONO_TYPE_R4)
			return FALSE;
		for (i = 0; i < sig->param_count; ++i)
			if (!sig->params [i]->byref && sig->params [i]->type == MONO_TYPE_R4)
				return FALSE;
	}
#endif

	if (g_list_find (cfg->dont_inline, method))
		return FALSE;

	return TRUE;
}

static gboolean
mini_field_access_needs_cctor_run (MonoCompile *cfg, MonoMethod *method, MonoClass *klass, MonoVTable *vtable)
{
	if (!cfg->compile_aot) {
		g_assert (vtable);
		if (vtable->initialized)
			return FALSE;
	}

	if (klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT) {
		if (cfg->method == method)
			return FALSE;
	}

	if (!mono_class_needs_cctor_run (klass, method))
		return FALSE;

	if (! (method->flags & METHOD_ATTRIBUTE_STATIC) && (klass == method->klass))
		/* The initialization is already done before the method is called */
		return FALSE;

	return TRUE;
}

static MonoInst*
mini_emit_ldelema_1_ins (MonoCompile *cfg, MonoClass *klass, MonoInst *arr, MonoInst *index, gboolean bcheck)
{
	MonoInst *ins;
	guint32 size;
	int mult_reg, add_reg, array_reg, index_reg, index2_reg;
	int context_used;

	if (mini_is_gsharedvt_variable_klass (cfg, klass)) {
		size = -1;
	} else {
		mono_class_init (klass);
		size = mono_class_array_element_size (klass);
	}

	mult_reg = alloc_preg (cfg);
	array_reg = arr->dreg;
	index_reg = index->dreg;

#if SIZEOF_REGISTER == 8
	/* The array reg is 64 bits but the index reg is only 32 */
	if (COMPILE_LLVM (cfg)) {
		/* Not needed */
		index2_reg = index_reg;
	} else {
		index2_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_UNALU (cfg, OP_SEXT_I4, index2_reg, index_reg);
	}
#else
	if (index->type == STACK_I8) {
		index2_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_UNALU (cfg, OP_LCONV_TO_I4, index2_reg, index_reg);
	} else {
		index2_reg = index_reg;
	}
#endif

	if (bcheck)
		MONO_EMIT_BOUNDS_CHECK (cfg, array_reg, MonoArray, max_length, index2_reg);

#if defined(TARGET_X86) || defined(TARGET_AMD64)
	if (size == 1 || size == 2 || size == 4 || size == 8) {
		static const int fast_log2 [] = { 1, 0, 1, -1, 2, -1, -1, -1, 3 };

		EMIT_NEW_X86_LEA (cfg, ins, array_reg, index2_reg, fast_log2 [size], MONO_STRUCT_OFFSET (MonoArray, vector));
		ins->klass = mono_class_get_element_class (klass);
		ins->type = STACK_MP;

		return ins;
	}
#endif		

	add_reg = alloc_ireg_mp (cfg);

	if (size == -1) {
		MonoInst *rgctx_ins;

		/* gsharedvt */
		g_assert (cfg->generic_sharing_context);
		context_used = mini_class_check_context_used (cfg, klass);
		g_assert (context_used);
		rgctx_ins = emit_get_gsharedvt_info (cfg, &klass->byval_arg, MONO_RGCTX_INFO_ARRAY_ELEMENT_SIZE);
		MONO_EMIT_NEW_BIALU (cfg, OP_IMUL, mult_reg, index2_reg, rgctx_ins->dreg);
	} else {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_MUL_IMM, mult_reg, index2_reg, size);
	}
	MONO_EMIT_NEW_BIALU (cfg, OP_PADD, add_reg, array_reg, mult_reg);
	NEW_BIALU_IMM (cfg, ins, OP_PADD_IMM, add_reg, add_reg, MONO_STRUCT_OFFSET (MonoArray, vector));
	ins->klass = mono_class_get_element_class (klass);
	ins->type = STACK_MP;
	MONO_ADD_INS (cfg->cbb, ins);

	return ins;
}

#ifndef MONO_ARCH_EMULATE_MUL_DIV
static MonoInst*
mini_emit_ldelema_2_ins (MonoCompile *cfg, MonoClass *klass, MonoInst *arr, MonoInst *index_ins1, MonoInst *index_ins2)
{
	int bounds_reg = alloc_preg (cfg);
	int add_reg = alloc_ireg_mp (cfg);
	int mult_reg = alloc_preg (cfg);
	int mult2_reg = alloc_preg (cfg);
	int low1_reg = alloc_preg (cfg);
	int low2_reg = alloc_preg (cfg);
	int high1_reg = alloc_preg (cfg);
	int high2_reg = alloc_preg (cfg);
	int realidx1_reg = alloc_preg (cfg);
	int realidx2_reg = alloc_preg (cfg);
	int sum_reg = alloc_preg (cfg);
	int index1, index2, tmpreg;
	MonoInst *ins;
	guint32 size;

	mono_class_init (klass);
	size = mono_class_array_element_size (klass);

	index1 = index_ins1->dreg;
	index2 = index_ins2->dreg;

#if SIZEOF_REGISTER == 8
	/* The array reg is 64 bits but the index reg is only 32 */
	if (COMPILE_LLVM (cfg)) {
		/* Not needed */
	} else {
		tmpreg = alloc_preg (cfg);
		MONO_EMIT_NEW_UNALU (cfg, OP_SEXT_I4, tmpreg, index1);
		index1 = tmpreg;
		tmpreg = alloc_preg (cfg);
		MONO_EMIT_NEW_UNALU (cfg, OP_SEXT_I4, tmpreg, index2);
		index2 = tmpreg;
	}
#else
	// FIXME: Do we need to do something here for i8 indexes, like in ldelema_1_ins ?
	tmpreg = -1;
#endif

	/* range checking */
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, bounds_reg, 
				       arr->dreg, MONO_STRUCT_OFFSET (MonoArray, bounds));

	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, low1_reg, 
				       bounds_reg, MONO_STRUCT_OFFSET (MonoArrayBounds, lower_bound));
	MONO_EMIT_NEW_BIALU (cfg, OP_PSUB, realidx1_reg, index1, low1_reg);
	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, high1_reg, 
				       bounds_reg, MONO_STRUCT_OFFSET (MonoArrayBounds, length));
	MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, high1_reg, realidx1_reg);
	MONO_EMIT_NEW_COND_EXC (cfg, LE_UN, "IndexOutOfRangeException");

	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, low2_reg, 
				       bounds_reg, sizeof (MonoArrayBounds) + MONO_STRUCT_OFFSET (MonoArrayBounds, lower_bound));
	MONO_EMIT_NEW_BIALU (cfg, OP_PSUB, realidx2_reg, index2, low2_reg);
	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, high2_reg, 
				       bounds_reg, sizeof (MonoArrayBounds) + MONO_STRUCT_OFFSET (MonoArrayBounds, length));
	MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, high2_reg, realidx2_reg);
	MONO_EMIT_NEW_COND_EXC (cfg, LE_UN, "IndexOutOfRangeException");

	MONO_EMIT_NEW_BIALU (cfg, OP_PMUL, mult_reg, high2_reg, realidx1_reg);
	MONO_EMIT_NEW_BIALU (cfg, OP_PADD, sum_reg, mult_reg, realidx2_reg);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_PMUL_IMM, mult2_reg, sum_reg, size);
	MONO_EMIT_NEW_BIALU (cfg, OP_PADD, add_reg, mult2_reg, arr->dreg);
	NEW_BIALU_IMM (cfg, ins, OP_PADD_IMM, add_reg, add_reg, MONO_STRUCT_OFFSET (MonoArray, vector));

	ins->type = STACK_MP;
	ins->klass = klass;
	MONO_ADD_INS (cfg->cbb, ins);

	return ins;
}
#endif

static MonoInst*
mini_emit_ldelema_ins (MonoCompile *cfg, MonoMethod *cmethod, MonoInst **sp, unsigned char *ip, gboolean is_set)
{
	int rank;
	MonoInst *addr;
	MonoMethod *addr_method;
	int element_size;

	rank = mono_method_signature (cmethod)->param_count - (is_set? 1: 0);

	if (rank == 1)
		return mini_emit_ldelema_1_ins (cfg, cmethod->klass->element_class, sp [0], sp [1], TRUE);

#ifndef MONO_ARCH_EMULATE_MUL_DIV
	/* emit_ldelema_2 depends on OP_LMUL */
	if (rank == 2 && (cfg->opt & MONO_OPT_INTRINS)) {
		return mini_emit_ldelema_2_ins (cfg, cmethod->klass->element_class, sp [0], sp [1], sp [2]);
	}
#endif

	element_size = mono_class_array_element_size (cmethod->klass->element_class);
	addr_method = mono_marshal_get_array_address (rank, element_size);
	addr = mono_emit_method_call (cfg, addr_method, sp, NULL);

	return addr;
}

static MonoBreakPolicy
always_insert_breakpoint (MonoMethod *method)
{
	return MONO_BREAK_POLICY_ALWAYS;
}

static MonoBreakPolicyFunc break_policy_func = always_insert_breakpoint;

/**
 * mono_set_break_policy:
 * policy_callback: the new callback function
 *
 * Allow embedders to decide wherther to actually obey breakpoint instructions
 * (both break IL instructions and Debugger.Break () method calls), for example
 * to not allow an app to be aborted by a perfectly valid IL opcode when executing
 * untrusted or semi-trusted code.
 *
 * @policy_callback will be called every time a break point instruction needs to
 * be inserted with the method argument being the method that calls Debugger.Break()
 * or has the IL break instruction. The callback should return #MONO_BREAK_POLICY_NEVER
 * if it wants the breakpoint to not be effective in the given method.
 * #MONO_BREAK_POLICY_ALWAYS is the default.
 */
void
mono_set_break_policy (MonoBreakPolicyFunc policy_callback)
{
	if (policy_callback)
		break_policy_func = policy_callback;
	else
		break_policy_func = always_insert_breakpoint;
}

static gboolean
should_insert_brekpoint (MonoMethod *method) {
	switch (break_policy_func (method)) {
	case MONO_BREAK_POLICY_ALWAYS:
		return TRUE;
	case MONO_BREAK_POLICY_NEVER:
		return FALSE;
	case MONO_BREAK_POLICY_ON_DBG:
		g_warning ("mdb no longer supported");
		return FALSE;
	default:
		g_warning ("Incorrect value returned from break policy callback");
		return FALSE;
	}
}

/* optimize the simple GetGenericValueImpl/SetGenericValueImpl generic icalls */
static MonoInst*
emit_array_generic_access (MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args, int is_set)
{
	MonoInst *addr, *store, *load;
	MonoClass *eklass = mono_class_from_mono_type (fsig->params [2]);

	/* the bounds check is already done by the callers */
	addr = mini_emit_ldelema_1_ins (cfg, eklass, args [0], args [1], FALSE);
	if (is_set) {
		EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, load, &eklass->byval_arg, args [2]->dreg, 0);
		EMIT_NEW_STORE_MEMBASE_TYPE (cfg, store, &eklass->byval_arg, addr->dreg, 0, load->dreg);
		if (mini_type_is_reference (cfg, fsig->params [2]))
			emit_write_barrier (cfg, addr, load);
	} else {
		EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, load, &eklass->byval_arg, addr->dreg, 0);
		EMIT_NEW_STORE_MEMBASE_TYPE (cfg, store, &eklass->byval_arg, args [2]->dreg, 0, load->dreg);
	}
	return store;
}


static gboolean
generic_class_is_reference_type (MonoCompile *cfg, MonoClass *klass)
{
	return mini_type_is_reference (cfg, &klass->byval_arg);
}

static MonoInst*
emit_array_store (MonoCompile *cfg, MonoClass *klass, MonoInst **sp, gboolean safety_checks)
{
	if (safety_checks && generic_class_is_reference_type (cfg, klass) &&
		!(sp [2]->opcode == OP_PCONST && sp [2]->inst_p0 == NULL)) {
		MonoClass *obj_array = mono_array_class_get_cached (mono_defaults.object_class, 1);
		MonoMethod *helper = mono_marshal_get_virtual_stelemref (obj_array);
		MonoInst *iargs [3];

		if (!helper->slot)
			mono_class_setup_vtable (obj_array);
		g_assert (helper->slot);

		if (sp [0]->type != STACK_OBJ)
			return NULL;
		if (sp [2]->type != STACK_OBJ)
			return NULL;

		iargs [2] = sp [2];
		iargs [1] = sp [1];
		iargs [0] = sp [0];

		return mono_emit_method_call (cfg, helper, iargs, sp [0]);
	} else {
		MonoInst *ins;

		if (mini_is_gsharedvt_variable_klass (cfg, klass)) {
			MonoInst *addr;

			// FIXME-VT: OP_ICONST optimization
			addr = mini_emit_ldelema_1_ins (cfg, klass, sp [0], sp [1], TRUE);
			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, addr->dreg, 0, sp [2]->dreg);
			ins->opcode = OP_STOREV_MEMBASE;
		} else if (sp [1]->opcode == OP_ICONST) {
			int array_reg = sp [0]->dreg;
			int index_reg = sp [1]->dreg;
			int offset = (mono_class_array_element_size (klass) * sp [1]->inst_c0) + MONO_STRUCT_OFFSET (MonoArray, vector);

			if (safety_checks)
				MONO_EMIT_BOUNDS_CHECK (cfg, array_reg, MonoArray, max_length, index_reg);
			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, array_reg, offset, sp [2]->dreg);
		} else {
			MonoInst *addr = mini_emit_ldelema_1_ins (cfg, klass, sp [0], sp [1], safety_checks);
			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, addr->dreg, 0, sp [2]->dreg);
			if (generic_class_is_reference_type (cfg, klass))
				emit_write_barrier (cfg, addr, sp [2]);
		}
		return ins;
	}
}

static MonoInst*
emit_array_unsafe_access (MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args, int is_set)
{
	MonoClass *eklass;
	
	if (is_set)
		eklass = mono_class_from_mono_type (fsig->params [2]);
	else
		eklass = mono_class_from_mono_type (fsig->ret);

	if (is_set) {
		return emit_array_store (cfg, eklass, args, FALSE);
	} else {
		MonoInst *ins, *addr = mini_emit_ldelema_1_ins (cfg, eklass, args [0], args [1], FALSE);
		EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &eklass->byval_arg, addr->dreg, 0);
		return ins;
	}
}

static gboolean
is_unsafe_mov_compatible (MonoClass *param_klass, MonoClass *return_klass)
{
	uint32_t align;

	//Only allow for valuetypes
	if (!param_klass->valuetype || !return_klass->valuetype)
		return FALSE;

	//That are blitable
	if (param_klass->has_references || return_klass->has_references)
		return FALSE;

	/* Avoid mixing structs and primitive types/enums, they need to be handled differently in the JIT */
	if ((MONO_TYPE_ISSTRUCT (&param_klass->byval_arg) && !MONO_TYPE_ISSTRUCT (&return_klass->byval_arg)) ||
		(!MONO_TYPE_ISSTRUCT (&param_klass->byval_arg) && MONO_TYPE_ISSTRUCT (&return_klass->byval_arg)))
		return FALSE;

	if (param_klass->byval_arg.type == MONO_TYPE_R4 || param_klass->byval_arg.type == MONO_TYPE_R8 ||
		return_klass->byval_arg.type == MONO_TYPE_R4 || return_klass->byval_arg.type == MONO_TYPE_R8)
		return FALSE;

	//And have the same size
	if (mono_class_value_size (param_klass, &align) != mono_class_value_size (return_klass, &align))
		return FALSE;
	return TRUE;
}

static MonoInst*
emit_array_unsafe_mov (MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoClass *param_klass = mono_class_from_mono_type (fsig->params [0]);
	MonoClass *return_klass = mono_class_from_mono_type (fsig->ret);

	//Valuetypes that are semantically equivalent
	if (is_unsafe_mov_compatible (param_klass, return_klass))
		return args [0];

	//Arrays of valuetypes that are semantically equivalent
	if (param_klass->rank == 1 && return_klass->rank == 1 && is_unsafe_mov_compatible (param_klass->element_class, return_klass->element_class))
		return args [0];

	return NULL;
}

static MonoInst*
mini_emit_inst_for_ctor (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
#ifdef MONO_ARCH_SIMD_INTRINSICS
	MonoInst *ins = NULL;

	if (cfg->opt & MONO_OPT_SIMD) {
		ins = mono_emit_simd_intrinsics (cfg, cmethod, fsig, args);
		if (ins)
			return ins;
	}
#endif

	return mono_emit_native_types_intrinsics (cfg, cmethod, fsig, args);
}

static MonoInst*
emit_memory_barrier (MonoCompile *cfg, int kind)
{
	MonoInst *ins = NULL;
	MONO_INST_NEW (cfg, ins, OP_MEMORY_BARRIER);
	MONO_ADD_INS (cfg->cbb, ins);
	ins->backend.memory_barrier_kind = kind;

	return ins;
}

static MonoInst*
llvm_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;
	int opcode = 0;

	/* The LLVM backend supports these intrinsics */
	if (cmethod->klass == mono_defaults.math_class) {
		if (strcmp (cmethod->name, "Sin") == 0) {
			opcode = OP_SIN;
		} else if (strcmp (cmethod->name, "Cos") == 0) {
			opcode = OP_COS;
		} else if (strcmp (cmethod->name, "Sqrt") == 0) {
			opcode = OP_SQRT;
		} else if (strcmp (cmethod->name, "Abs") == 0 && fsig->params [0]->type == MONO_TYPE_R8) {
			opcode = OP_ABS;
		}

		if (opcode) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = STACK_R8;
			ins->dreg = mono_alloc_freg (cfg);
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

		if (opcode) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = fsig->params [0]->type == MONO_TYPE_I4 ? STACK_I4 : STACK_I8;
			ins->dreg = mono_alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->sreg2 = args [1]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}
	}

	return ins;
}

static MonoInst*
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

static MonoInst*
mini_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;
	
	static MonoClass *runtime_helpers_class = NULL;
	if (! runtime_helpers_class)
		runtime_helpers_class = mono_class_from_name (mono_defaults.corlib,
			"System.Runtime.CompilerServices", "RuntimeHelpers");

	if (cmethod->klass == mono_defaults.string_class) {
		if (strcmp (cmethod->name, "get_Chars") == 0) {
			int dreg = alloc_ireg (cfg);
			int index_reg = alloc_preg (cfg);
			int mult_reg = alloc_preg (cfg);
			int add_reg = alloc_preg (cfg);

#if SIZEOF_REGISTER == 8
			/* The array reg is 64 bits but the index reg is only 32 */
			MONO_EMIT_NEW_UNALU (cfg, OP_SEXT_I4, index_reg, args [1]->dreg);
#else
			index_reg = args [1]->dreg;
#endif	
			MONO_EMIT_BOUNDS_CHECK (cfg, args [0]->dreg, MonoString, length, index_reg);

#if defined(TARGET_X86) || defined(TARGET_AMD64)
			EMIT_NEW_X86_LEA (cfg, ins, args [0]->dreg, index_reg, 1, MONO_STRUCT_OFFSET (MonoString, chars));
			add_reg = ins->dreg;
			/* Avoid a warning */
			mult_reg = 0;
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADU2_MEMBASE, dreg, 
								   add_reg, 0);
#else
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, mult_reg, index_reg, 1);
			MONO_EMIT_NEW_BIALU (cfg, OP_PADD, add_reg, mult_reg, args [0]->dreg);
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADU2_MEMBASE, dreg, 
								   add_reg, MONO_STRUCT_OFFSET (MonoString, chars));
#endif
			type_from_op (ins, NULL, NULL);
			return ins;
		} else if (strcmp (cmethod->name, "get_Length") == 0) {
			int dreg = alloc_ireg (cfg);
			/* Decompose later to allow more optimizations */
			EMIT_NEW_UNALU (cfg, ins, OP_STRLEN, dreg, args [0]->dreg);
			ins->type = STACK_I4;
			ins->flags |= MONO_INST_FAULT;
			cfg->cbb->has_array_access = TRUE;
			cfg->flags |= MONO_CFG_HAS_ARRAY_ACCESS;

			return ins;
		} else if (strcmp (cmethod->name, "InternalSetChar") == 0) {
			int mult_reg = alloc_preg (cfg);
			int add_reg = alloc_preg (cfg);

			/* The corlib functions check for oob already. */
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, mult_reg, args [1]->dreg, 1);
			MONO_EMIT_NEW_BIALU (cfg, OP_PADD, add_reg, mult_reg, args [0]->dreg);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, add_reg, MONO_STRUCT_OFFSET (MonoString, chars), args [2]->dreg);
			return cfg->cbb->last_ins;
		} else 
			return NULL;
	} else if (cmethod->klass == mono_defaults.object_class) {

		if (strcmp (cmethod->name, "GetType") == 0) {
			int dreg = alloc_ireg_ref (cfg);
			int vt_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_FAULT (cfg, vt_reg, args [0]->dreg, MONO_STRUCT_OFFSET (MonoObject, vtable));
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, dreg, vt_reg, MONO_STRUCT_OFFSET (MonoVTable, type));
			type_from_op (ins, NULL, NULL);

			return ins;
#if !defined(MONO_ARCH_EMULATE_MUL_DIV)
		} else if (strcmp (cmethod->name, "InternalGetHashCode") == 0 && !mono_gc_is_moving ()) {
			int dreg = alloc_ireg (cfg);
			int t1 = alloc_ireg (cfg);
	
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, t1, args [0]->dreg, 3);
			EMIT_NEW_BIALU_IMM (cfg, ins, OP_MUL_IMM, dreg, t1, 2654435761u);
			ins->type = STACK_I4;

			return ins;
#endif
		} else if (strcmp (cmethod->name, ".ctor") == 0) {
 			MONO_INST_NEW (cfg, ins, OP_NOP);
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		} else
			return NULL;
	} else if (cmethod->klass == mono_defaults.array_class) {
		if (!cfg->gsharedvt && strcmp (cmethod->name + 1, "etGenericValueImpl") == 0)
			return emit_array_generic_access (cfg, fsig, args, *cmethod->name == 'S');

#ifndef MONO_BIG_ARRAYS
		/*
		 * This is an inline version of GetLength/GetLowerBound(0) used frequently in
		 * Array methods.
		 */
		if ((strcmp (cmethod->name, "GetLength") == 0 || strcmp (cmethod->name, "GetLowerBound") == 0) && args [1]->opcode == OP_ICONST && args [1]->inst_c0 == 0) {
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

		if (strcmp (cmethod->name, "get_Rank") == 0) {
			int dreg = alloc_ireg (cfg);
			int vtable_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP_FAULT (cfg, OP_LOAD_MEMBASE, vtable_reg, 
												 args [0]->dreg, MONO_STRUCT_OFFSET (MonoObject, vtable));
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADU1_MEMBASE, dreg,
								   vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, rank));
			type_from_op (ins, NULL, NULL);

			return ins;
		} else if (strcmp (cmethod->name, "get_Length") == 0) {
			int dreg = alloc_ireg (cfg);

			EMIT_NEW_LOAD_MEMBASE_FAULT (cfg, ins, OP_LOADI4_MEMBASE, dreg, 
										 args [0]->dreg, MONO_STRUCT_OFFSET (MonoArray, max_length));
			type_from_op (ins, NULL, NULL);

			return ins;
		} else
			return NULL;
	} else if (cmethod->klass == runtime_helpers_class) {

		if (strcmp (cmethod->name, "get_OffsetToStringData") == 0) {
			EMIT_NEW_ICONST (cfg, ins, MONO_STRUCT_OFFSET (MonoString, chars));
			return ins;
		} else
			return NULL;
	} else if (cmethod->klass == mono_defaults.thread_class) {
		if (strcmp (cmethod->name, "SpinWait_nop") == 0) {
			MONO_INST_NEW (cfg, ins, OP_RELAXED_NOP);
			MONO_ADD_INS (cfg->cbb, ins);
			return ins;
		} else if (strcmp (cmethod->name, "MemoryBarrier") == 0) {
			return emit_memory_barrier (cfg, FullBarrier);
		}
	} else if (cmethod->klass == mono_defaults.monitor_class) {

		/* FIXME this should be integrated to the check below once we support the trampoline version */
#if defined(MONO_ARCH_ENABLE_MONITOR_IL_FASTPATH)
		if (strcmp (cmethod->name, "Enter") == 0 && fsig->param_count == 2) {
			MonoMethod *fast_method = NULL;

			/* Avoid infinite recursion */
			if (cfg->method->wrapper_type == MONO_WRAPPER_UNKNOWN && !strcmp (cfg->method->name, "FastMonitorEnterV4"))
				return NULL;
				
			fast_method = mono_monitor_get_fast_path (cmethod);
			if (!fast_method)
				return NULL;

			return (MonoInst*)mono_emit_method_call (cfg, fast_method, args, NULL);
		}
#endif

#if defined(MONO_ARCH_MONITOR_OBJECT_REG)
		if (strcmp (cmethod->name, "Enter") == 0 && fsig->param_count == 1) {
			MonoCallInst *call;

			if (COMPILE_LLVM (cfg)) {
				/* 
				 * Pass the argument normally, the LLVM backend will handle the
				 * calling convention problems.
				 */
				call = (MonoCallInst*)mono_emit_abs_call (cfg, MONO_PATCH_INFO_MONITOR_ENTER, NULL, helper_sig_monitor_enter_exit_trampoline_llvm, args);
			} else {
				call = (MonoCallInst*)mono_emit_abs_call (cfg, MONO_PATCH_INFO_MONITOR_ENTER,
					    NULL, helper_sig_monitor_enter_exit_trampoline, NULL);
				mono_call_inst_add_outarg_reg (cfg, call, args [0]->dreg,
											   MONO_ARCH_MONITOR_OBJECT_REG, FALSE);
			}

			return (MonoInst*)call;
		} else if (strcmp (cmethod->name, "Exit") == 0) {
			MonoCallInst *call;

			if (COMPILE_LLVM (cfg)) {
				call = (MonoCallInst*)mono_emit_abs_call (cfg, MONO_PATCH_INFO_MONITOR_EXIT, NULL, helper_sig_monitor_enter_exit_trampoline_llvm, args);
			} else {
				call = (MonoCallInst*)mono_emit_abs_call (cfg, MONO_PATCH_INFO_MONITOR_EXIT,
					    NULL, helper_sig_monitor_enter_exit_trampoline, NULL);
				mono_call_inst_add_outarg_reg (cfg, call, args [0]->dreg,
											   MONO_ARCH_MONITOR_OBJECT_REG, FALSE);
			}

			return (MonoInst*)call;
		}
#elif defined(MONO_ARCH_ENABLE_MONITOR_IL_FASTPATH)
		{
		MonoMethod *fast_method = NULL;

		/* Avoid infinite recursion */
		if (cfg->method->wrapper_type == MONO_WRAPPER_UNKNOWN &&
				(strcmp (cfg->method->name, "FastMonitorEnter") == 0 ||
				 strcmp (cfg->method->name, "FastMonitorExit") == 0))
			return NULL;

		if ((strcmp (cmethod->name, "Enter") == 0 && fsig->param_count == 2) ||
				strcmp (cmethod->name, "Exit") == 0)
			fast_method = mono_monitor_get_fast_path (cmethod);
		if (!fast_method)
			return NULL;

		return (MonoInst*)mono_emit_method_call (cfg, fast_method, args, NULL);
		}
#endif
	} else if (cmethod->klass->image == mono_defaults.corlib &&
			   (strcmp (cmethod->klass->name_space, "System.Threading") == 0) &&
			   (strcmp (cmethod->klass->name, "Interlocked") == 0)) {
		ins = NULL;

#if SIZEOF_REGISTER == 8
		if (strcmp (cmethod->name, "Read") == 0 && (fsig->params [0]->type == MONO_TYPE_I8)) {
			MonoInst *load_ins;

			emit_memory_barrier (cfg, FullBarrier);

			/* 64 bit reads are already atomic */
			MONO_INST_NEW (cfg, load_ins, OP_LOADI8_MEMBASE);
			load_ins->dreg = mono_alloc_preg (cfg);
			load_ins->inst_basereg = args [0]->dreg;
			load_ins->inst_offset = 0;
			MONO_ADD_INS (cfg->cbb, load_ins);

			emit_memory_barrier (cfg, FullBarrier);

			ins = load_ins;
		}
#endif

		if (strcmp (cmethod->name, "Increment") == 0) {
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
		} else if (strcmp (cmethod->name, "Decrement") == 0) {
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
		} else if (strcmp (cmethod->name, "Add") == 0) {
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

		if (strcmp (cmethod->name, "Exchange") == 0) {
			guint32 opcode;
			gboolean is_ref = fsig->params [0]->type == MONO_TYPE_OBJECT;

			if (fsig->params [0]->type == MONO_TYPE_I4) {
				opcode = OP_ATOMIC_EXCHANGE_I4;
				cfg->has_atomic_exchange_i4 = TRUE;
			}
#if SIZEOF_REGISTER == 8
			else if (is_ref || (fsig->params [0]->type == MONO_TYPE_I8) ||
					(fsig->params [0]->type == MONO_TYPE_I))
				opcode = OP_ATOMIC_EXCHANGE_I8;
#else
			else if (is_ref || (fsig->params [0]->type == MONO_TYPE_I)) {
				opcode = OP_ATOMIC_EXCHANGE_I4;
				cfg->has_atomic_exchange_i4 = TRUE;
			}
#endif
			else
				return NULL;

			if (!mono_arch_opcode_supported (opcode))
				return NULL;

			MONO_INST_NEW (cfg, ins, opcode);
			ins->dreg = is_ref ? mono_alloc_ireg_ref (cfg) : mono_alloc_ireg (cfg);
			ins->inst_basereg = args [0]->dreg;
			ins->inst_offset = 0;
			ins->sreg2 = args [1]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);

			switch (fsig->params [0]->type) {
			case MONO_TYPE_I4:
				ins->type = STACK_I4;
				break;
			case MONO_TYPE_I8:
			case MONO_TYPE_I:
				ins->type = STACK_I8;
				break;
			case MONO_TYPE_OBJECT:
				ins->type = STACK_OBJ;
				break;
			default:
				g_assert_not_reached ();
			}

			if (cfg->gen_write_barriers && is_ref)
				emit_write_barrier (cfg, args [0], args [1]);
		}

		if ((strcmp (cmethod->name, "CompareExchange") == 0)) {
			int size = 0;
			gboolean is_ref = mini_type_is_reference (cfg, fsig->params [1]);
			if (fsig->params [1]->type == MONO_TYPE_I4)
				size = 4;
			else if (is_ref || fsig->params [1]->type == MONO_TYPE_I)
				size = sizeof (gpointer);
			else if (sizeof (gpointer) == 8 && fsig->params [1]->type == MONO_TYPE_I8)
				size = 8;
			if (size == 4) {
				if (!mono_arch_opcode_supported (OP_ATOMIC_CAS_I4))
					return NULL;
				MONO_INST_NEW (cfg, ins, OP_ATOMIC_CAS_I4);
				ins->dreg = is_ref ? alloc_ireg_ref (cfg) : alloc_ireg (cfg);
				ins->sreg1 = args [0]->dreg;
				ins->sreg2 = args [1]->dreg;
				ins->sreg3 = args [2]->dreg;
				ins->type = STACK_I4;
				MONO_ADD_INS (cfg->cbb, ins);
				cfg->has_atomic_cas_i4 = TRUE;
			} else if (size == 8) {
				if (!mono_arch_opcode_supported (OP_ATOMIC_CAS_I8))
					return NULL;
				MONO_INST_NEW (cfg, ins, OP_ATOMIC_CAS_I8);
				ins->dreg = is_ref ? alloc_ireg_ref (cfg) : alloc_ireg (cfg);
				ins->sreg1 = args [0]->dreg;
				ins->sreg2 = args [1]->dreg;
				ins->sreg3 = args [2]->dreg;
				ins->type = STACK_I8;
				MONO_ADD_INS (cfg->cbb, ins);
			} else {
				/* g_assert_not_reached (); */
			}
			if (cfg->gen_write_barriers && is_ref)
				emit_write_barrier (cfg, args [0], args [1]);
		}

		if (strcmp (cmethod->name, "MemoryBarrier") == 0)
			ins = emit_memory_barrier (cfg, FullBarrier);

		if (ins)
			return ins;
	} else if (cmethod->klass->image == mono_defaults.corlib) {
		if (cmethod->name [0] == 'B' && strcmp (cmethod->name, "Break") == 0
				&& strcmp (cmethod->klass->name, "Debugger") == 0) {
			if (should_insert_brekpoint (cfg->method)) {
				ins = mono_emit_jit_icall (cfg, mono_debugger_agent_user_break, NULL);
			} else {
				MONO_INST_NEW (cfg, ins, OP_NOP);
				MONO_ADD_INS (cfg->cbb, ins);
			}
			return ins;
		}
		if (cmethod->name [0] == 'g' && strcmp (cmethod->name, "get_IsRunningOnWindows") == 0
				&& strcmp (cmethod->klass->name, "Environment") == 0) {
#ifdef TARGET_WIN32
	                EMIT_NEW_ICONST (cfg, ins, 1);
#else
	                EMIT_NEW_ICONST (cfg, ins, 0);
#endif
			return ins;
		}
	} else if (cmethod->klass == mono_defaults.math_class) {
		/* 
		 * There is general branches code for Min/Max, but it does not work for 
		 * all inputs:
		 * http://everything2.com/?node_id=1051618
		 */
	} else if ((!strcmp (cmethod->klass->image->assembly->aname.name, "MonoMac") || !strcmp (cmethod->klass->image->assembly->aname.name, "monotouch")) && !strcmp (cmethod->klass->name, "Selector") && !strcmp (cmethod->name, "GetHandle") && cfg->compile_aot && (args [0]->opcode == OP_GOT_ENTRY || args[0]->opcode == OP_AOTCONST)) {
#ifdef MONO_ARCH_HAVE_OBJC_GET_SELECTOR
		MonoInst *pi;
		MonoJumpInfoToken *ji;
		MonoString *s;

		cfg->disable_llvm = TRUE;

		if (args [0]->opcode == OP_GOT_ENTRY) {
			pi = args [0]->inst_p1;
			g_assert (pi->opcode == OP_PATCH_INFO);
			g_assert (GPOINTER_TO_INT (pi->inst_p1) == MONO_PATCH_INFO_LDSTR);
			ji = pi->inst_p0;
		} else {
			g_assert (GPOINTER_TO_INT (args [0]->inst_p1) == MONO_PATCH_INFO_LDSTR);
			ji = args [0]->inst_p0;
		}

		NULLIFY_INS (args [0]);

		// FIXME: Ugly
		s = mono_ldstr (cfg->domain, ji->image, mono_metadata_token_index (ji->token));
		MONO_INST_NEW (cfg, ins, OP_OBJC_GET_SELECTOR);
		ins->dreg = mono_alloc_ireg (cfg);
		// FIXME: Leaks
		ins->inst_p0 = mono_string_to_utf8 (s);
		MONO_ADD_INS (cfg->cbb, ins);
		return ins;
#endif
	}

#ifdef MONO_ARCH_SIMD_INTRINSICS
	if (cfg->opt & MONO_OPT_SIMD) {
		ins = mono_emit_simd_intrinsics (cfg, cmethod, fsig, args);
		if (ins)
			return ins;
	}
#endif

	ins = mono_emit_native_types_intrinsics (cfg, cmethod, fsig, args);
	if (ins)
		return ins;

	if (COMPILE_LLVM (cfg)) {
		ins = llvm_emit_inst_for_method (cfg, cmethod, fsig, args);
		if (ins)
			return ins;
	}

	return mono_arch_emit_inst_for_method (cfg, cmethod, fsig, args);
}

/*
 * This entry point could be used later for arbitrary method
 * redirection.
 */
inline static MonoInst*
mini_redirect_call (MonoCompile *cfg, MonoMethod *method,  
					MonoMethodSignature *signature, MonoInst **args, MonoInst *this)
{
	if (method->klass == mono_defaults.string_class) {
		/* managed string allocation support */
		if (strcmp (method->name, "InternalAllocateStr") == 0 && !(mono_profiler_events & MONO_PROFILE_ALLOCATIONS) && !(cfg->opt & MONO_OPT_SHARED)) {
			MonoInst *iargs [2];
			MonoVTable *vtable = mono_class_vtable (cfg->domain, method->klass);
			MonoMethod *managed_alloc = NULL;

			g_assert (vtable); /*Should not fail since it System.String*/
#ifndef MONO_CROSS_COMPILE
			managed_alloc = mono_gc_get_managed_allocator (method->klass, FALSE);
#endif
			if (!managed_alloc)
				return NULL;
			EMIT_NEW_VTABLECONST (cfg, iargs [0], vtable);
			iargs [1] = args [0];
			return mono_emit_method_call (cfg, managed_alloc, iargs, this);
		}
	}
	return NULL;
}

static void
mono_save_args (MonoCompile *cfg, MonoMethodSignature *sig, MonoInst **sp)
{
	MonoInst *store, *temp;
	int i;

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		MonoType *argtype = (sig->hasthis && (i == 0)) ? type_from_stack_type (*sp) : sig->params [i - sig->hasthis];

		/*
		 * FIXME: We should use *args++ = sp [0], but that would mean the arg
		 * would be different than the MonoInst's used to represent arguments, and
		 * the ldelema implementation can't deal with that.
		 * Solution: When ldelema is used on an inline argument, create a var for 
		 * it, emit ldelema on that var, and emit the saving code below in
		 * inline_method () if needed.
		 */
		temp = mono_compile_create_var (cfg, argtype, OP_LOCAL);
		cfg->args [i] = temp;
		/* This uses cfg->args [i] which is set by the preceeding line */
		EMIT_NEW_ARGSTORE (cfg, store, i, *sp);
		store->cil_code = sp [0]->cil_code;
		sp++;
	}
}

#define MONO_INLINE_CALLED_LIMITED_METHODS 1
#define MONO_INLINE_CALLER_LIMITED_METHODS 1

#if (MONO_INLINE_CALLED_LIMITED_METHODS)
static gboolean
check_inline_called_method_name_limit (MonoMethod *called_method)
{
	int strncmp_result;
	static const char *limit = NULL;
	
	if (limit == NULL) {
		const char *limit_string = g_getenv ("MONO_INLINE_CALLED_METHOD_NAME_LIMIT");

		if (limit_string != NULL)
			limit = limit_string;
		else
			limit = "";
	}

	if (limit [0] != '\0') {
		char *called_method_name = mono_method_full_name (called_method, TRUE);

		strncmp_result = strncmp (called_method_name, limit, strlen (limit));
		g_free (called_method_name);
	
		//return (strncmp_result <= 0);
		return (strncmp_result == 0);
	} else {
		return TRUE;
	}
}
#endif

#if (MONO_INLINE_CALLER_LIMITED_METHODS)
static gboolean
check_inline_caller_method_name_limit (MonoMethod *caller_method)
{
	int strncmp_result;
	static const char *limit = NULL;
	
	if (limit == NULL) {
		const char *limit_string = g_getenv ("MONO_INLINE_CALLER_METHOD_NAME_LIMIT");
		if (limit_string != NULL) {
			limit = limit_string;
		} else {
			limit = "";
		}
	}

	if (limit [0] != '\0') {
		char *caller_method_name = mono_method_full_name (caller_method, TRUE);

		strncmp_result = strncmp (caller_method_name, limit, strlen (limit));
		g_free (caller_method_name);
	
		//return (strncmp_result <= 0);
		return (strncmp_result == 0);
	} else {
		return TRUE;
	}
}
#endif

static void
emit_init_rvar (MonoCompile *cfg, int dreg, MonoType *rtype)
{
	static double r8_0 = 0.0;
	MonoInst *ins;
	int t;

	rtype = mini_replace_type (rtype);
	t = rtype->type;

	if (rtype->byref) {
		MONO_EMIT_NEW_PCONST (cfg, dreg, NULL);
	} else if (t >= MONO_TYPE_BOOLEAN && t <= MONO_TYPE_U4) {
		MONO_EMIT_NEW_ICONST (cfg, dreg, 0);
	} else if (t == MONO_TYPE_I8 || t == MONO_TYPE_U8) {
		MONO_EMIT_NEW_I8CONST (cfg, dreg, 0);
	} else if (t == MONO_TYPE_R4 || t == MONO_TYPE_R8) {
		MONO_INST_NEW (cfg, ins, OP_R8CONST);
		ins->type = STACK_R8;
		ins->inst_p0 = (void*)&r8_0;
		ins->dreg = dreg;
		MONO_ADD_INS (cfg->cbb, ins);
	} else if ((t == MONO_TYPE_VALUETYPE) || (t == MONO_TYPE_TYPEDBYREF) ||
		   ((t == MONO_TYPE_GENERICINST) && mono_type_generic_inst_is_valuetype (rtype))) {
		MONO_EMIT_NEW_VZERO (cfg, dreg, mono_class_from_mono_type (rtype));
	} else if (((t == MONO_TYPE_VAR) || (t == MONO_TYPE_MVAR)) && mini_type_var_is_vt (cfg, rtype)) {
		MONO_EMIT_NEW_VZERO (cfg, dreg, mono_class_from_mono_type (rtype));
	} else {
		MONO_EMIT_NEW_PCONST (cfg, dreg, NULL);
	}
}

static void
emit_dummy_init_rvar (MonoCompile *cfg, int dreg, MonoType *rtype)
{
	int t;

	rtype = mini_replace_type (rtype);
	t = rtype->type;

	if (rtype->byref) {
		MONO_EMIT_NEW_DUMMY_INIT (cfg, dreg, OP_DUMMY_PCONST);
	} else if (t >= MONO_TYPE_BOOLEAN && t <= MONO_TYPE_U4) {
		MONO_EMIT_NEW_DUMMY_INIT (cfg, dreg, OP_DUMMY_ICONST);
	} else if (t == MONO_TYPE_I8 || t == MONO_TYPE_U8) {
		MONO_EMIT_NEW_DUMMY_INIT (cfg, dreg, OP_DUMMY_I8CONST);
	} else if (t == MONO_TYPE_R4 || t == MONO_TYPE_R8) {
		MONO_EMIT_NEW_DUMMY_INIT (cfg, dreg, OP_DUMMY_R8CONST);
	} else if ((t == MONO_TYPE_VALUETYPE) || (t == MONO_TYPE_TYPEDBYREF) ||
		   ((t == MONO_TYPE_GENERICINST) && mono_type_generic_inst_is_valuetype (rtype))) {
		MONO_EMIT_NEW_DUMMY_INIT (cfg, dreg, OP_DUMMY_VZERO);
	} else if (((t == MONO_TYPE_VAR) || (t == MONO_TYPE_MVAR)) && mini_type_var_is_vt (cfg, rtype)) {
		MONO_EMIT_NEW_DUMMY_INIT (cfg, dreg, OP_DUMMY_VZERO);
	} else {
		emit_init_rvar (cfg, dreg, rtype);
	}
}

/* If INIT is FALSE, emit dummy initialization statements to keep the IR valid */
static void
emit_init_local (MonoCompile *cfg, int local, MonoType *type, gboolean init)
{
	MonoInst *var = cfg->locals [local];
	if (COMPILE_SOFT_FLOAT (cfg)) {
		MonoInst *store;
		int reg = alloc_dreg (cfg, var->type);
		emit_init_rvar (cfg, reg, type);
		EMIT_NEW_LOCSTORE (cfg, store, local, cfg->cbb->last_ins);
	} else {
		if (init)
			emit_init_rvar (cfg, var->dreg, type);
		else
			emit_dummy_init_rvar (cfg, var->dreg, type);
	}
}

/*
 * inline_method:
 *
 *   Return the cost of inlining CMETHOD.
 */
static int
inline_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **sp,
			   guchar *ip, guint real_offset, gboolean inline_always, MonoBasicBlock **out_cbb)
{
	MonoInst *ins, *rvar = NULL;
	MonoMethodHeader *cheader;
	MonoBasicBlock *ebblock, *sbblock;
	int i, costs;
	MonoMethod *prev_inlined_method;
	MonoInst **prev_locals, **prev_args;
	MonoType **prev_arg_types;
	guint prev_real_offset;
	GHashTable *prev_cbb_hash;
	MonoBasicBlock **prev_cil_offset_to_bb;
	MonoBasicBlock *prev_cbb;
	unsigned char* prev_cil_start;
	guint32 prev_cil_offset_to_bb_len;
	MonoMethod *prev_current_method;
	MonoGenericContext *prev_generic_context;
	gboolean ret_var_set, prev_ret_var_set, prev_disable_inline, virtual = FALSE;

	g_assert (cfg->exception_type == MONO_EXCEPTION_NONE);

#if (MONO_INLINE_CALLED_LIMITED_METHODS)
	if ((! inline_always) && ! check_inline_called_method_name_limit (cmethod))
		return 0;
#endif
#if (MONO_INLINE_CALLER_LIMITED_METHODS)
	if ((! inline_always) && ! check_inline_caller_method_name_limit (cfg->method))
		return 0;
#endif

	if (cfg->verbose_level > 2)
		printf ("INLINE START %p %s -> %s\n", cmethod,  mono_method_full_name (cfg->method, TRUE), mono_method_full_name (cmethod, TRUE));

	if (!cmethod->inline_info) {
		cfg->stat_inlineable_methods++;
		cmethod->inline_info = 1;
	}

	/* allocate local variables */
	cheader = mono_method_get_header (cmethod);

	if (cheader == NULL || mono_loader_get_last_error ()) {
		MonoLoaderError *error = mono_loader_get_last_error ();

		if (cheader)
			mono_metadata_free_mh (cheader);
		if (inline_always && error)
			mono_cfg_set_exception (cfg, error->exception_type);

		mono_loader_clear_error ();
		return 0;
	}

	/*Must verify before creating locals as it can cause the JIT to assert.*/
	if (mono_compile_is_broken (cfg, cmethod, FALSE)) {
		mono_metadata_free_mh (cheader);
		return 0;
	}

	/* allocate space to store the return value */
	if (!MONO_TYPE_IS_VOID (fsig->ret)) {
		rvar = mono_compile_create_var (cfg, fsig->ret, OP_LOCAL);
	}

	prev_locals = cfg->locals;
	cfg->locals = mono_mempool_alloc0 (cfg->mempool, cheader->num_locals * sizeof (MonoInst*));	
	for (i = 0; i < cheader->num_locals; ++i)
		cfg->locals [i] = mono_compile_create_var (cfg, cheader->locals [i], OP_LOCAL);

	/* allocate start and end blocks */
	/* This is needed so if the inline is aborted, we can clean up */
	NEW_BBLOCK (cfg, sbblock);
	sbblock->real_offset = real_offset;

	NEW_BBLOCK (cfg, ebblock);
	ebblock->block_num = cfg->num_bblocks++;
	ebblock->real_offset = real_offset;

	prev_args = cfg->args;
	prev_arg_types = cfg->arg_types;
	prev_inlined_method = cfg->inlined_method;
	cfg->inlined_method = cmethod;
	cfg->ret_var_set = FALSE;
	cfg->inline_depth ++;
	prev_real_offset = cfg->real_offset;
	prev_cbb_hash = cfg->cbb_hash;
	prev_cil_offset_to_bb = cfg->cil_offset_to_bb;
	prev_cil_offset_to_bb_len = cfg->cil_offset_to_bb_len;
	prev_cil_start = cfg->cil_start;
	prev_cbb = cfg->cbb;
	prev_current_method = cfg->current_method;
	prev_generic_context = cfg->generic_context;
	prev_ret_var_set = cfg->ret_var_set;
	prev_disable_inline = cfg->disable_inline;

	if (*ip == CEE_CALLVIRT && !(cmethod->flags & METHOD_ATTRIBUTE_STATIC))
		virtual = TRUE;

	costs = mono_method_to_ir (cfg, cmethod, sbblock, ebblock, rvar, sp, real_offset, virtual);

	ret_var_set = cfg->ret_var_set;

	cfg->inlined_method = prev_inlined_method;
	cfg->real_offset = prev_real_offset;
	cfg->cbb_hash = prev_cbb_hash;
	cfg->cil_offset_to_bb = prev_cil_offset_to_bb;
	cfg->cil_offset_to_bb_len = prev_cil_offset_to_bb_len;
	cfg->cil_start = prev_cil_start;
	cfg->locals = prev_locals;
	cfg->args = prev_args;
	cfg->arg_types = prev_arg_types;
	cfg->current_method = prev_current_method;
	cfg->generic_context = prev_generic_context;
	cfg->ret_var_set = prev_ret_var_set;
	cfg->disable_inline = prev_disable_inline;
	cfg->inline_depth --;

	if ((costs >= 0 && costs < 60) || inline_always) {
		if (cfg->verbose_level > 2)
			printf ("INLINE END %s -> %s\n", mono_method_full_name (cfg->method, TRUE), mono_method_full_name (cmethod, TRUE));
		
		cfg->stat_inlined_methods++;

		/* always add some code to avoid block split failures */
		MONO_INST_NEW (cfg, ins, OP_NOP);
		MONO_ADD_INS (prev_cbb, ins);

		prev_cbb->next_bb = sbblock;
		link_bblock (cfg, prev_cbb, sbblock);

		/* 
		 * Get rid of the begin and end bblocks if possible to aid local
		 * optimizations.
		 */
		mono_merge_basic_blocks (cfg, prev_cbb, sbblock);

		if ((prev_cbb->out_count == 1) && (prev_cbb->out_bb [0]->in_count == 1) && (prev_cbb->out_bb [0] != ebblock))
			mono_merge_basic_blocks (cfg, prev_cbb, prev_cbb->out_bb [0]);

		if ((ebblock->in_count == 1) && ebblock->in_bb [0]->out_count == 1) {
			MonoBasicBlock *prev = ebblock->in_bb [0];
			mono_merge_basic_blocks (cfg, prev, ebblock);
			cfg->cbb = prev;
			if ((prev_cbb->out_count == 1) && (prev_cbb->out_bb [0]->in_count == 1) && (prev_cbb->out_bb [0] == prev)) {
				mono_merge_basic_blocks (cfg, prev_cbb, prev);
				cfg->cbb = prev_cbb;
			}
		} else {
			/* 
			 * Its possible that the rvar is set in some prev bblock, but not in others.
			 * (#1835).
			 */
			if (rvar) {
				MonoBasicBlock *bb;

				for (i = 0; i < ebblock->in_count; ++i) {
					bb = ebblock->in_bb [i];

					if (bb->last_ins && bb->last_ins->opcode == OP_NOT_REACHED) {
						cfg->cbb = bb;

						emit_init_rvar (cfg, rvar->dreg, fsig->ret);
					}
				}
			}

			cfg->cbb = ebblock;
		}

		*out_cbb = cfg->cbb;

		if (rvar) {
			/*
			 * If the inlined method contains only a throw, then the ret var is not 
			 * set, so set it to a dummy value.
			 */
			if (!ret_var_set)
				emit_init_rvar (cfg, rvar->dreg, fsig->ret);

			EMIT_NEW_TEMPLOAD (cfg, ins, rvar->inst_c0);
			*sp++ = ins;
		}
		cfg->headers_to_free = g_slist_prepend_mempool (cfg->mempool, cfg->headers_to_free, cheader);
		return costs + 1;
	} else {
		if (cfg->verbose_level > 2)
			printf ("INLINE ABORTED %s (cost %d)\n", mono_method_full_name (cmethod, TRUE), costs);
		cfg->exception_type = MONO_EXCEPTION_NONE;
		mono_loader_clear_error ();

		/* This gets rid of the newly added bblocks */
		cfg->cbb = prev_cbb;
	}
	cfg->headers_to_free = g_slist_prepend_mempool (cfg->mempool, cfg->headers_to_free, cheader);
	return 0;
}

/*
 * Some of these comments may well be out-of-date.
 * Design decisions: we do a single pass over the IL code (and we do bblock 
 * splitting/merging in the few cases when it's required: a back jump to an IL
 * address that was not already seen as bblock starting point).
 * Code is validated as we go (full verification is still better left to metadata/verify.c).
 * Complex operations are decomposed in simpler ones right away. We need to let the 
 * arch-specific code peek and poke inside this process somehow (except when the 
 * optimizations can take advantage of the full semantic info of coarse opcodes).
 * All the opcodes of the form opcode.s are 'normalized' to opcode.
 * MonoInst->opcode initially is the IL opcode or some simplification of that 
 * (OP_LOAD, OP_STORE). The arch-specific code may rearrange it to an arch-specific 
 * opcode with value bigger than OP_LAST.
 * At this point the IR can be handed over to an interpreter, a dumb code generator
 * or to the optimizing code generator that will translate it to SSA form.
 *
 * Profiling directed optimizations.
 * We may compile by default with few or no optimizations and instrument the code
 * or the user may indicate what methods to optimize the most either in a config file
 * or through repeated runs where the compiler applies offline the optimizations to 
 * each method and then decides if it was worth it.
 */

#define CHECK_TYPE(ins) if (!(ins)->type) UNVERIFIED
#define CHECK_STACK(num) if ((sp - stack_start) < (num)) UNVERIFIED
#define CHECK_STACK_OVF(num) if (((sp - stack_start) + (num)) > header->max_stack) UNVERIFIED
#define CHECK_ARG(num) if ((unsigned)(num) >= (unsigned)num_args) UNVERIFIED
#define CHECK_LOCAL(num) if ((unsigned)(num) >= (unsigned)header->num_locals) UNVERIFIED
#define CHECK_OPSIZE(size) if (ip + size > end) UNVERIFIED
#define CHECK_UNVERIFIABLE(cfg) if (cfg->unverifiable) UNVERIFIED
#define CHECK_TYPELOAD(klass) if (!(klass) || (klass)->exception_type) TYPE_LOAD_ERROR ((klass))

/* offset from br.s -> br like opcodes */
#define BIG_BRANCH_OFFSET 13

static gboolean
ip_in_bb (MonoCompile *cfg, MonoBasicBlock *bb, const guint8* ip)
{
	MonoBasicBlock *b = cfg->cil_offset_to_bb [ip - cfg->cil_start];

	return b == NULL || b == bb;
}

static int
get_basic_blocks (MonoCompile *cfg, MonoMethodHeader* header, guint real_offset, unsigned char *start, unsigned char *end, unsigned char **pos)
{
	unsigned char *ip = start;
	unsigned char *target;
	int i;
	guint cli_addr;
	MonoBasicBlock *bblock;
	const MonoOpcode *opcode;

	while (ip < end) {
		cli_addr = ip - start;
		i = mono_opcode_value ((const guint8 **)&ip, end);
		if (i < 0)
			UNVERIFIED;
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
			GET_BBLOCK (cfg, bblock, target);
			ip += 2;
			if (ip < end)
				GET_BBLOCK (cfg, bblock, ip);
			break;
		case MonoInlineBrTarget:
			target = start + cli_addr + 5 + (gint32)read32 (ip + 1);
			GET_BBLOCK (cfg, bblock, target);
			ip += 5;
			if (ip < end)
				GET_BBLOCK (cfg, bblock, ip);
			break;
		case MonoInlineSwitch: {
			guint32 n = read32 (ip + 1);
			guint32 j;
			ip += 5;
			cli_addr += 5 + 4 * n;
			target = start + cli_addr;
			GET_BBLOCK (cfg, bblock, target);
			
			for (j = 0; j < n; ++j) {
				target = start + cli_addr + (gint32)read32 (ip);
				GET_BBLOCK (cfg, bblock, target);
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

		if (i == CEE_THROW) {
			unsigned char *bb_start = ip - 1;
			
			/* Find the start of the bblock containing the throw */
			bblock = NULL;
			while ((bb_start >= start) && !bblock) {
				bblock = cfg->cil_offset_to_bb [(bb_start) - start];
				bb_start --;
			}
			if (bblock)
				bblock->out_of_line = 1;
		}
	}
	return 0;
unverified:
exception_exit:
	*pos = ip;
	return 1;
}

static inline MonoMethod *
mini_get_method_allow_open (MonoMethod *m, guint32 token, MonoClass *klass, MonoGenericContext *context)
{
	MonoMethod *method;

	if (m->wrapper_type != MONO_WRAPPER_NONE) {
		method = mono_method_get_wrapper_data (m, token);
		if (context)
			method = mono_class_inflate_generic_method (method, context);
	} else {
		method = mono_get_method_full (m->klass->image, token, klass, context);
	}

	return method;
}

static inline MonoMethod *
mini_get_method (MonoCompile *cfg, MonoMethod *m, guint32 token, MonoClass *klass, MonoGenericContext *context)
{
	MonoMethod *method = mini_get_method_allow_open (m, token, klass, context);

	if (method && cfg && !cfg->generic_sharing_context && mono_class_is_open_constructed_type (&method->klass->byval_arg))
		return NULL;

	return method;
}

static inline MonoClass*
mini_get_class (MonoMethod *method, guint32 token, MonoGenericContext *context)
{
	MonoError error;
	MonoClass *klass;

	if (method->wrapper_type != MONO_WRAPPER_NONE) {
		klass = mono_method_get_wrapper_data (method, token);
		if (context)
			klass = mono_class_inflate_generic_class (klass, context);
	} else {
		klass = mono_class_get_and_inflate_typespec_checked (method->klass->image, token, context, &error);
		mono_error_cleanup (&error); /* FIXME don't swallow the error */
	}
	if (klass)
		mono_class_init (klass);
	return klass;
}

static inline MonoMethodSignature*
mini_get_signature (MonoMethod *method, guint32 token, MonoGenericContext *context)
{
	MonoMethodSignature *fsig;

	if (method->wrapper_type != MONO_WRAPPER_NONE) {
		MonoError error;

		fsig = (MonoMethodSignature *)mono_method_get_wrapper_data (method, token);
		if (context) {
			fsig = mono_inflate_generic_signature (fsig, context, &error);
			// FIXME:
			g_assert (mono_error_ok (&error));
		}
	} else {
		fsig = mono_metadata_parse_signature (method->klass->image, token);
	}
	return fsig;
}

/*
 * Returns TRUE if the JIT should abort inlining because "callee"
 * is influenced by security attributes.
 */
static
gboolean check_linkdemand (MonoCompile *cfg, MonoMethod *caller, MonoMethod *callee)
{
	guint32 result;
	
	if ((cfg->method != caller) && mono_security_method_has_declsec (callee)) {
		return TRUE;
	}
	
	result = mono_declsec_linkdemand (cfg->domain, caller, callee);
	if (result == MONO_JIT_SECURITY_OK)
		return FALSE;

	if (result == MONO_JIT_LINKDEMAND_ECMA) {
		/* Generate code to throw a SecurityException before the actual call/link */
		MonoSecurityManager *secman = mono_security_manager_get_methods ();
		MonoInst *args [2];

		NEW_ICONST (cfg, args [0], 4);
		NEW_METHODCONST (cfg, args [1], caller);
		mono_emit_method_call (cfg, secman->linkdemandsecurityexception, args, NULL);
	} else if (cfg->exception_type == MONO_EXCEPTION_NONE) {
		 /* don't hide previous results */
		mono_cfg_set_exception (cfg, MONO_EXCEPTION_SECURITY_LINKDEMAND);
		cfg->exception_data = result;
		return TRUE;
	}
	
	return FALSE;
}

static MonoMethod*
throw_exception (void)
{
	static MonoMethod *method = NULL;

	if (!method) {
		MonoSecurityManager *secman = mono_security_manager_get_methods ();
		method = mono_class_get_method_from_name (secman->securitymanager, "ThrowException", 1);
	}
	g_assert (method);
	return method;
}

static void
emit_throw_exception (MonoCompile *cfg, MonoException *ex)
{
	MonoMethod *thrower = throw_exception ();
	MonoInst *args [1];

	EMIT_NEW_PCONST (cfg, args [0], ex);
	mono_emit_method_call (cfg, thrower, args, NULL);
}

/*
 * Return the original method is a wrapper is specified. We can only access 
 * the custom attributes from the original method.
 */
static MonoMethod*
get_original_method (MonoMethod *method)
{
	if (method->wrapper_type == MONO_WRAPPER_NONE)
		return method;

	/* native code (which is like Critical) can call any managed method XXX FIXME XXX to validate all usages */
	if (method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED)
		return NULL;

	/* in other cases we need to find the original method */
	return mono_marshal_method_from_wrapper (method);
}

static void
ensure_method_is_allowed_to_access_field (MonoCompile *cfg, MonoMethod *caller, MonoClassField *field,
					  MonoBasicBlock *bblock, unsigned char *ip)
{
	/* we can't get the coreclr security level on wrappers since they don't have the attributes */
	MonoException *ex = mono_security_core_clr_is_field_access_allowed (get_original_method (caller), field);
	if (ex)
		emit_throw_exception (cfg, ex);
}

static void
ensure_method_is_allowed_to_call_method (MonoCompile *cfg, MonoMethod *caller, MonoMethod *callee,
					 MonoBasicBlock *bblock, unsigned char *ip)
{
	/* we can't get the coreclr security level on wrappers since they don't have the attributes */
	MonoException *ex = mono_security_core_clr_is_call_allowed (get_original_method (caller), callee);
	if (ex)
		emit_throw_exception (cfg, ex);
}

/*
 * Check that the IL instructions at ip are the array initialization
 * sequence and return the pointer to the data and the size.
 */
static const char*
initialize_array_data (MonoMethod *method, gboolean aot, unsigned char *ip, MonoClass *klass, guint32 len, int *out_size, guint32 *out_field_token)
{
	/*
	 * newarr[System.Int32]
	 * dup
	 * ldtoken field valuetype ...
	 * call void class [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
	 */
	if (ip [0] == CEE_DUP && ip [1] == CEE_LDTOKEN && ip [5] == 0x4 && ip [6] == CEE_CALL) {
		MonoError error;
		guint32 token = read32 (ip + 7);
		guint32 field_token = read32 (ip + 2);
		guint32 field_index = field_token & 0xffffff;
		guint32 rva;
		const char *data_ptr;
		int size = 0;
		MonoMethod *cmethod;
		MonoClass *dummy_class;
		MonoClassField *field = mono_field_from_token_checked (method->klass->image, field_token, &dummy_class, NULL, &error);
		int dummy_align;

		if (!field) {
			mono_error_cleanup (&error); /* FIXME don't swallow the error */
			return NULL;
		}

		*out_field_token = field_token;

		cmethod = mini_get_method (NULL, method, token, NULL, NULL);
		if (!cmethod)
			return NULL;
		if (strcmp (cmethod->name, "InitializeArray") || strcmp (cmethod->klass->name, "RuntimeHelpers") || cmethod->klass->image != mono_defaults.corlib)
			return NULL;
		switch (mono_type_get_underlying_type (&klass->byval_arg)->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			size = 1; break;
		/* we need to swap on big endian, so punt. Should we handle R4 and R8 as well? */
#if TARGET_BYTE_ORDER == G_LITTLE_ENDIAN
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			size = 2; break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_R4:
			size = 4; break;
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			size = 8; break;
#endif
		default:
			return NULL;
		}
		size *= len;
		if (size > mono_type_size (field->type, &dummy_align))
		    return NULL;
		*out_size = size;
		/*g_print ("optimized in %s: size: %d, numelems: %d\n", method->name, size, newarr->inst_newa_len->inst_c0);*/
		if (!image_is_dynamic (method->klass->image)) {
			field_index = read32 (ip + 2) & 0xffffff;
			mono_metadata_field_info (method->klass->image, field_index - 1, NULL, &rva, NULL);
			data_ptr = mono_image_rva_map (method->klass->image, rva);
			/*g_print ("field: 0x%08x, rva: %d, rva_ptr: %p\n", read32 (ip + 2), rva, data_ptr);*/
			/* for aot code we do the lookup on load */
			if (aot && data_ptr)
				return GUINT_TO_POINTER (rva);
		} else {
			/*FIXME is it possible to AOT a SRE assembly not meant to be saved? */ 
			g_assert (!aot);
			data_ptr = mono_field_get_data (field);
		}
		return data_ptr;
	}
	return NULL;
}

static void
set_exception_type_from_invalid_il (MonoCompile *cfg, MonoMethod *method, unsigned char *ip)
{
	char *method_fname = mono_method_full_name (method, TRUE);
	char *method_code;
	MonoMethodHeader *header = mono_method_get_header (method);

	if (header->code_size == 0)
		method_code = g_strdup ("method body is empty.");
	else
		method_code = mono_disasm_code_one (NULL, method, ip, NULL);
 	mono_cfg_set_exception (cfg, MONO_EXCEPTION_INVALID_PROGRAM);
 	cfg->exception_message = g_strdup_printf ("Invalid IL code in %s: %s\n", method_fname, method_code);
 	g_free (method_fname);
 	g_free (method_code);
	cfg->headers_to_free = g_slist_prepend_mempool (cfg->mempool, cfg->headers_to_free, header);
}

static void
set_exception_object (MonoCompile *cfg, MonoException *exception)
{
	mono_cfg_set_exception (cfg, MONO_EXCEPTION_OBJECT_SUPPLIED);
	MONO_GC_REGISTER_ROOT_SINGLE (cfg->exception_ptr);
	cfg->exception_ptr = exception;
}

static void
emit_stloc_ir (MonoCompile *cfg, MonoInst **sp, MonoMethodHeader *header, int n)
{
	MonoInst *ins;
	guint32 opcode = mono_type_to_regmove (cfg, header->locals [n]);
	if ((opcode == OP_MOVE) && cfg->cbb->last_ins == sp [0]  &&
			((sp [0]->opcode == OP_ICONST) || (sp [0]->opcode == OP_I8CONST))) {
		/* Optimize reg-reg moves away */
		/* 
		 * Can't optimize other opcodes, since sp[0] might point to
		 * the last ins of a decomposed opcode.
		 */
		sp [0]->dreg = (cfg)->locals [n]->dreg;
	} else {
		EMIT_NEW_LOCSTORE (cfg, ins, n, *sp);
	}
}

/*
 * ldloca inhibits many optimizations so try to get rid of it in common
 * cases.
 */
static inline unsigned char *
emit_optimized_ldloca_ir (MonoCompile *cfg, unsigned char *ip, unsigned char *end, int size)
{
	int local, token;
	MonoClass *klass;
	MonoType *type;

	if (size == 1) {
		local = ip [1];
		ip += 2;
	} else {
		local = read16 (ip + 2);
		ip += 4;
	}
	
	if (ip + 6 < end && (ip [0] == CEE_PREFIX1) && (ip [1] == CEE_INITOBJ) && ip_in_bb (cfg, cfg->cbb, ip + 1)) {
		/* From the INITOBJ case */
		token = read32 (ip + 2);
		klass = mini_get_class (cfg->current_method, token, cfg->generic_context);
		CHECK_TYPELOAD (klass);
		type = mini_replace_type (&klass->byval_arg);
		emit_init_local (cfg, local, type, TRUE);
		return ip + 6;
	}
 exception_exit:
	return NULL;
}

static gboolean
is_exception_class (MonoClass *class)
{
	while (class) {
		if (class == mono_defaults.exception_class)
			return TRUE;
		class = class->parent;
	}
	return FALSE;
}

/*
 * is_jit_optimizer_disabled:
 *
 *   Determine whenever M's assembly has a DebuggableAttribute with the
 * IsJITOptimizerDisabled flag set.
 */
static gboolean
is_jit_optimizer_disabled (MonoMethod *m)
{
	MonoAssembly *ass = m->klass->image->assembly;
	MonoCustomAttrInfo* attrs;
	static MonoClass *klass;
	int i;
	gboolean val = FALSE;

	g_assert (ass);
	if (ass->jit_optimizer_disabled_inited)
		return ass->jit_optimizer_disabled;

	if (!klass)
		klass = mono_class_from_name (mono_defaults.corlib, "System.Diagnostics", "DebuggableAttribute");
	if (!klass) {
		/* Linked away */
		ass->jit_optimizer_disabled = FALSE;
		mono_memory_barrier ();
		ass->jit_optimizer_disabled_inited = TRUE;
		return FALSE;
	}

	attrs = mono_custom_attrs_from_assembly (ass);
	if (attrs) {
		for (i = 0; i < attrs->num_attrs; ++i) {
			MonoCustomAttrEntry *attr = &attrs->attrs [i];
			const gchar *p;
			int len;
			MonoMethodSignature *sig;

			if (!attr->ctor || attr->ctor->klass != klass)
				continue;
			/* Decode the attribute. See reflection.c */
			len = attr->data_size;
			p = (const char*)attr->data;
			g_assert (read16 (p) == 0x0001);
			p += 2;

			// FIXME: Support named parameters
			sig = mono_method_signature (attr->ctor);
			if (sig->param_count != 2 || sig->params [0]->type != MONO_TYPE_BOOLEAN || sig->params [1]->type != MONO_TYPE_BOOLEAN)
				continue;
			/* Two boolean arguments */
			p ++;
			val = *p;
		}
		mono_custom_attrs_free (attrs);
	}

	ass->jit_optimizer_disabled = val;
	mono_memory_barrier ();
	ass->jit_optimizer_disabled_inited = TRUE;

	return val;
}

static gboolean
is_supported_tail_call (MonoCompile *cfg, MonoMethod *method, MonoMethod *cmethod, MonoMethodSignature *fsig, int call_opcode)
{
	gboolean supported_tail_call;
	int i;

#ifdef MONO_ARCH_HAVE_OP_TAIL_CALL
	supported_tail_call = mono_arch_tail_call_supported (cfg, mono_method_signature (method), mono_method_signature (cmethod));
#else
	supported_tail_call = mono_metadata_signature_equal (mono_method_signature (method), mono_method_signature (cmethod)) && !MONO_TYPE_ISSTRUCT (mono_method_signature (cmethod)->ret);
#endif

	for (i = 0; i < fsig->param_count; ++i) {
		if (fsig->params [i]->byref || fsig->params [i]->type == MONO_TYPE_PTR || fsig->params [i]->type == MONO_TYPE_FNPTR)
			/* These can point to the current method's stack */
			supported_tail_call = FALSE;
	}
	if (fsig->hasthis && cmethod->klass->valuetype)
		/* this might point to the current method's stack */
		supported_tail_call = FALSE;
	if (cmethod->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
		supported_tail_call = FALSE;
	if (cfg->method->save_lmf)
		supported_tail_call = FALSE;
	if (cmethod->wrapper_type && cmethod->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD)
		supported_tail_call = FALSE;
	if (call_opcode != CEE_CALL)
		supported_tail_call = FALSE;

	/* Debugging support */
#if 0
	if (supported_tail_call) {
		if (!mono_debug_count ())
			supported_tail_call = FALSE;
	}
#endif

	return supported_tail_call;
}

/* the JIT intercepts ldflda instructions to the tlsdata field in ThreadLocal<T> and redirects
 * it to the thread local value based on the tls_offset field. Every other kind of access to
 * the field causes an assert.
 */
static gboolean
is_magic_tls_access (MonoClassField *field)
{
	if (strcmp (field->name, "tlsdata"))
		return FALSE;
	if (strcmp (field->parent->name, "ThreadLocal`1"))
		return FALSE;
	return field->parent->image == mono_defaults.corlib;
}

/* emits the code needed to access a managed tls var (like ThreadStatic)
 * with the value of the tls offset in offset_reg. thread_ins represents the MonoInternalThread
 * pointer for the current thread.
 * Returns the MonoInst* representing the address of the tls var.
 */
static MonoInst*
emit_managed_static_data_access (MonoCompile *cfg, MonoInst *thread_ins, int offset_reg)
{
	MonoInst *addr;
	int static_data_reg, array_reg, dreg;
	int offset2_reg, idx_reg;
	// inlined access to the tls data
	// idx = (offset >> 24) - 1;
	// return ((char*) thread->static_data [idx]) + (offset & 0xffffff);
	static_data_reg = alloc_ireg (cfg);
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, static_data_reg, thread_ins->dreg, MONO_STRUCT_OFFSET (MonoInternalThread, static_data));
	idx_reg = alloc_ireg (cfg);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, idx_reg, offset_reg, 24);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISUB_IMM, idx_reg, idx_reg, 1);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHL_IMM, idx_reg, idx_reg, sizeof (gpointer) == 8 ? 3 : 2);
	MONO_EMIT_NEW_BIALU (cfg, OP_PADD, static_data_reg, static_data_reg, idx_reg);
	array_reg = alloc_ireg (cfg);
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, array_reg, static_data_reg, 0);
	offset2_reg = alloc_ireg (cfg);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IAND_IMM, offset2_reg, offset_reg, 0xffffff);
	dreg = alloc_ireg (cfg);
	EMIT_NEW_BIALU (cfg, addr, OP_PADD, dreg, array_reg, offset2_reg);
	return addr;
}

/*
 * redirect access to the tlsdata field to the tls var given by the tls_offset field.
 * this address is cached per-method in cached_tls_addr.
 */
static MonoInst*
create_magic_tls_access (MonoCompile *cfg, MonoClassField *tls_field, MonoInst **cached_tls_addr, MonoInst *thread_local)
{
	MonoInst *load, *addr, *temp, *store, *thread_ins;
	MonoClassField *offset_field;

	if (*cached_tls_addr) {
		EMIT_NEW_TEMPLOAD (cfg, addr, (*cached_tls_addr)->inst_c0);
		return addr;
	}
	thread_ins = mono_get_thread_intrinsic (cfg);
	offset_field = mono_class_get_field_from_name (tls_field->parent, "tls_offset");

	EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, load, offset_field->type, thread_local->dreg, offset_field->offset);
	if (thread_ins) {
		MONO_ADD_INS (cfg->cbb, thread_ins);
	} else {
		MonoMethod *thread_method;
		thread_method = mono_class_get_method_from_name (mono_get_thread_class(), "CurrentInternalThread_internal", 0);
		thread_ins = mono_emit_method_call (cfg, thread_method, NULL, NULL);
	}
	addr = emit_managed_static_data_access (cfg, thread_ins, load->dreg);
	addr->klass = mono_class_from_mono_type (tls_field->type);
	addr->type = STACK_MP;
	*cached_tls_addr = temp = mono_compile_create_var (cfg, type_from_stack_type (addr), OP_LOCAL);
	EMIT_NEW_TEMPSTORE (cfg, store, temp->inst_c0, addr);

	EMIT_NEW_TEMPLOAD (cfg, addr, temp->inst_c0);
	return addr;
}

/*
 * handle_ctor_call:
 *
 *   Handle calls made to ctors from NEWOBJ opcodes.
 *
 *   REF_BBLOCK will point to the current bblock after the call.
 */
static void
handle_ctor_call (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, int context_used,
				  MonoInst **sp, guint8 *ip, MonoBasicBlock **ref_bblock, int *inline_costs)
{
	MonoInst *vtable_arg = NULL, *callvirt_this_arg = NULL, *ins;
	MonoBasicBlock *bblock = *ref_bblock;

	if (cmethod->klass->valuetype && mono_class_generic_sharing_enabled (cmethod->klass) &&
					mono_method_is_generic_sharable (cmethod, TRUE)) {
		if (cmethod->is_inflated && mono_method_get_context (cmethod)->method_inst) {
			mono_class_vtable (cfg->domain, cmethod->klass);
			CHECK_TYPELOAD (cmethod->klass);

			vtable_arg = emit_get_rgctx_method (cfg, context_used,
												cmethod, MONO_RGCTX_INFO_METHOD_RGCTX);
		} else {
			if (context_used) {
				vtable_arg = emit_get_rgctx_klass (cfg, context_used,
												   cmethod->klass, MONO_RGCTX_INFO_VTABLE);
			} else {
				MonoVTable *vtable = mono_class_vtable (cfg->domain, cmethod->klass);

				CHECK_TYPELOAD (cmethod->klass);
				EMIT_NEW_VTABLECONST (cfg, vtable_arg, vtable);
			}
		}
	}

	/* Avoid virtual calls to ctors if possible */
	if (mono_class_is_marshalbyref (cmethod->klass))
		callvirt_this_arg = sp [0];

	if (cmethod && (cfg->opt & MONO_OPT_INTRINS) && (ins = mini_emit_inst_for_ctor (cfg, cmethod, fsig, sp))) {
		g_assert (MONO_TYPE_IS_VOID (fsig->ret));
		CHECK_CFG_EXCEPTION;
	} else if ((cfg->opt & MONO_OPT_INLINE) && cmethod && !context_used && !vtable_arg &&
			   mono_method_check_inlining (cfg, cmethod) &&
			   !mono_class_is_subclass_of (cmethod->klass, mono_defaults.exception_class, FALSE)) {
		int costs;

		if ((costs = inline_method (cfg, cmethod, fsig, sp, ip, cfg->real_offset, FALSE, &bblock))) {
			cfg->real_offset += 5;

			*inline_costs += costs - 5;
			*ref_bblock = bblock;
		} else {
			INLINE_FAILURE ("inline failure");
			// FIXME-VT: Clean this up
			if (cfg->gsharedvt && mini_is_gsharedvt_signature (cfg, fsig))
				GSHAREDVT_FAILURE(*ip);
			mono_emit_method_call_full (cfg, cmethod, fsig, FALSE, sp, callvirt_this_arg, NULL, NULL);
		}
	} else if (cfg->gsharedvt && mini_is_gsharedvt_signature (cfg, fsig)) {
		MonoInst *addr;

		addr = emit_get_rgctx_gsharedvt_call (cfg, context_used, fsig, cmethod, MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE);
		mono_emit_calli (cfg, fsig, sp, addr, NULL, vtable_arg);
	} else if (context_used &&
			   ((!mono_method_is_generic_sharable_full (cmethod, TRUE, FALSE, FALSE) ||
				 !mono_class_generic_sharing_enabled (cmethod->klass)) || cfg->gsharedvt)) {
		MonoInst *cmethod_addr;

		/* Generic calls made out of gsharedvt methods cannot be patched, so use an indirect call */

		cmethod_addr = emit_get_rgctx_method (cfg, context_used,
											  cmethod, MONO_RGCTX_INFO_GENERIC_METHOD_CODE);

		mono_emit_calli (cfg, fsig, sp, cmethod_addr, NULL, vtable_arg);
	} else {
		INLINE_FAILURE ("ctor call");
		ins = mono_emit_method_call_full (cfg, cmethod, fsig, FALSE, sp,
										  callvirt_this_arg, NULL, vtable_arg);
	}
 exception_exit:
	return;
}

/*
 * mono_method_to_ir:
 *
 *   Translate the .net IL into linear IR.
 */
int
mono_method_to_ir (MonoCompile *cfg, MonoMethod *method, MonoBasicBlock *start_bblock, MonoBasicBlock *end_bblock, 
		   MonoInst *return_var, MonoInst **inline_args, 
		   guint inline_offset, gboolean is_virtual_call)
{
	MonoError error;
	MonoInst *ins, **sp, **stack_start;
	MonoBasicBlock *bblock, *tblock = NULL, *init_localsbb = NULL;
	MonoSimpleBasicBlock *bb = NULL, *original_bb = NULL;
	MonoMethod *cmethod, *method_definition;
	MonoInst **arg_array;
	MonoMethodHeader *header;
	MonoImage *image;
	guint32 token, ins_flag;
	MonoClass *klass;
	MonoClass *constrained_call = NULL;
	unsigned char *ip, *end, *target, *err_pos;
	MonoMethodSignature *sig;
	MonoGenericContext *generic_context = NULL;
	MonoGenericContainer *generic_container = NULL;
	MonoType **param_types;
	int i, n, start_new_bblock, dreg;
	int num_calls = 0, inline_costs = 0;
	int breakpoint_id = 0;
	guint num_args;
	MonoBoolean security, pinvoke;
	MonoSecurityManager* secman = NULL;
	MonoDeclSecurityActions actions;
	GSList *class_inits = NULL;
	gboolean dont_verify, dont_verify_stloc, readonly = FALSE;
	int context_used;
	gboolean init_locals, seq_points, skip_dead_blocks;
	gboolean sym_seq_points = FALSE;
	MonoInst *cached_tls_addr = NULL;
	MonoDebugMethodInfo *minfo;
	MonoBitSet *seq_point_locs = NULL;
	MonoBitSet *seq_point_set_locs = NULL;

	cfg->disable_inline = is_jit_optimizer_disabled (method);

	/* serialization and xdomain stuff may need access to private fields and methods */
	dont_verify = method->klass->image->assembly->corlib_internal? TRUE: FALSE;
	dont_verify |= method->wrapper_type == MONO_WRAPPER_XDOMAIN_INVOKE;
	dont_verify |= method->wrapper_type == MONO_WRAPPER_XDOMAIN_DISPATCH;
 	dont_verify |= method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE; /* bug #77896 */
	dont_verify |= method->wrapper_type == MONO_WRAPPER_COMINTEROP;
	dont_verify |= method->wrapper_type == MONO_WRAPPER_COMINTEROP_INVOKE;

	dont_verify |= mono_security_smcs_hack_enabled ();

	/* still some type unsafety issues in marshal wrappers... (unknown is PtrToStructure) */
	dont_verify_stloc = method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE;
	dont_verify_stloc |= method->wrapper_type == MONO_WRAPPER_UNKNOWN;
	dont_verify_stloc |= method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED;
	dont_verify_stloc |= method->wrapper_type == MONO_WRAPPER_STELEMREF;

	image = method->klass->image;
	header = mono_method_get_header (method);
	if (!header) {
		MonoLoaderError *error;

		if ((error = mono_loader_get_last_error ())) {
			mono_cfg_set_exception (cfg, error->exception_type);
		} else {
			mono_cfg_set_exception (cfg, MONO_EXCEPTION_INVALID_PROGRAM);
			cfg->exception_message = g_strdup_printf ("Missing or incorrect header for method %s", cfg->method->name);
		}
		goto exception_exit;
	}
	generic_container = mono_method_get_generic_container (method);
	sig = mono_method_signature (method);
	num_args = sig->hasthis + sig->param_count;
	ip = (unsigned char*)header->code;
	cfg->cil_start = ip;
	end = ip + header->code_size;
	cfg->stat_cil_code_size += header->code_size;

	seq_points = cfg->gen_seq_points && cfg->method == method;
#ifdef PLATFORM_ANDROID
	seq_points &= cfg->method->wrapper_type == MONO_WRAPPER_NONE;
#endif

	if (method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED) {
		/* We could hit a seq point before attaching to the JIT (#8338) */
		seq_points = FALSE;
	}

	if (cfg->gen_seq_points && cfg->method == method) {
		minfo = mono_debug_lookup_method (method);
		if (minfo) {
			int i, n_il_offsets;
			int *il_offsets;
			int *line_numbers;

			mono_debug_symfile_get_line_numbers_full (minfo, NULL, NULL, &n_il_offsets, &il_offsets, &line_numbers, NULL, NULL, NULL, NULL);
			seq_point_locs = mono_bitset_mem_new (mono_mempool_alloc0 (cfg->mempool, mono_bitset_alloc_size (header->code_size, 0)), header->code_size, 0);
			seq_point_set_locs = mono_bitset_mem_new (mono_mempool_alloc0 (cfg->mempool, mono_bitset_alloc_size (header->code_size, 0)), header->code_size, 0);
			sym_seq_points = TRUE;
			for (i = 0; i < n_il_offsets; ++i) {
				if (il_offsets [i] < header->code_size)
					mono_bitset_set_fast (seq_point_locs, il_offsets [i]);
			}
			g_free (il_offsets);
			g_free (line_numbers);
		}
	}

	/* 
	 * Methods without init_locals set could cause asserts in various passes
	 * (#497220). To work around this, we emit dummy initialization opcodes
	 * (OP_DUMMY_ICONST etc.) which generate no code. These are only supported
	 * on some platforms.
	 */
	if ((cfg->opt & MONO_OPT_UNSAFE) && ARCH_HAVE_DUMMY_INIT)
		init_locals = header->init_locals;
	else
		init_locals = TRUE;

	method_definition = method;
	while (method_definition->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) method_definition;
		method_definition = imethod->declaring;
	}

	/* SkipVerification is not allowed if core-clr is enabled */
	if (!dont_verify && mini_assembly_can_skip_verification (cfg->domain, method)) {
		dont_verify = TRUE;
		dont_verify_stloc = TRUE;
	}

	if (sig->is_inflated)
		generic_context = mono_method_get_context (method);
	else if (generic_container)
		generic_context = &generic_container->context;
	cfg->generic_context = generic_context;

	if (!cfg->generic_sharing_context)
		g_assert (!sig->has_type_parameters);

	if (sig->generic_param_count && method->wrapper_type == MONO_WRAPPER_NONE) {
		g_assert (method->is_inflated);
		g_assert (mono_method_get_context (method)->method_inst);
	}
	if (method->is_inflated && mono_method_get_context (method)->method_inst)
		g_assert (sig->generic_param_count);

	if (cfg->method == method) {
		cfg->real_offset = 0;
	} else {
		cfg->real_offset = inline_offset;
	}

	cfg->cil_offset_to_bb = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoBasicBlock*) * header->code_size);
	cfg->cil_offset_to_bb_len = header->code_size;

	cfg->current_method = method;

	if (cfg->verbose_level > 2)
		printf ("method to IR %s\n", mono_method_full_name (method, TRUE));

	param_types = mono_mempool_alloc (cfg->mempool, sizeof (MonoType*) * num_args);
	if (sig->hasthis)
		param_types [0] = method->klass->valuetype?&method->klass->this_arg:&method->klass->byval_arg;
	for (n = 0; n < sig->param_count; ++n)
		param_types [n + sig->hasthis] = sig->params [n];
	cfg->arg_types = param_types;

	cfg->dont_inline = g_list_prepend (cfg->dont_inline, method);
	if (cfg->method == method) {

		if (cfg->prof_options & MONO_PROFILE_INS_COVERAGE)
			cfg->coverage_info = mono_profiler_coverage_alloc (cfg->method, header->code_size);

		/* ENTRY BLOCK */
		NEW_BBLOCK (cfg, start_bblock);
		cfg->bb_entry = start_bblock;
		start_bblock->cil_code = NULL;
		start_bblock->cil_length = 0;
#if defined(__native_client_codegen__)
		MONO_INST_NEW (cfg, ins, OP_NACL_GC_SAFE_POINT);
		ins->dreg = alloc_dreg (cfg, STACK_I4);
		MONO_ADD_INS (start_bblock, ins);
#endif

		/* EXIT BLOCK */
		NEW_BBLOCK (cfg, end_bblock);
		cfg->bb_exit = end_bblock;
		end_bblock->cil_code = NULL;
		end_bblock->cil_length = 0;
		end_bblock->flags |= BB_INDIRECT_JUMP_TARGET;
		g_assert (cfg->num_bblocks == 2);

		arg_array = cfg->args;

		if (header->num_clauses) {
			cfg->spvars = g_hash_table_new (NULL, NULL);
			cfg->exvars = g_hash_table_new (NULL, NULL);
		}
		/* handle exception clauses */
		for (i = 0; i < header->num_clauses; ++i) {
			MonoBasicBlock *try_bb;
			MonoExceptionClause *clause = &header->clauses [i];
			GET_BBLOCK (cfg, try_bb, ip + clause->try_offset);
			try_bb->real_offset = clause->try_offset;
			try_bb->try_start = TRUE;
			try_bb->region = ((i + 1) << 8) | clause->flags;
			GET_BBLOCK (cfg, tblock, ip + clause->handler_offset);
			tblock->real_offset = clause->handler_offset;
			tblock->flags |= BB_EXCEPTION_HANDLER;

			/*
			 * Linking the try block with the EH block hinders inlining as we won't be able to 
			 * merge the bblocks from inlining and produce an artificial hole for no good reason.
			 */
			if (COMPILE_LLVM (cfg))
				link_bblock (cfg, try_bb, tblock);

			if (*(ip + clause->handler_offset) == CEE_POP)
				tblock->flags |= BB_EXCEPTION_DEAD_OBJ;

			if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY ||
			    clause->flags == MONO_EXCEPTION_CLAUSE_FILTER ||
			    clause->flags == MONO_EXCEPTION_CLAUSE_FAULT) {
				MONO_INST_NEW (cfg, ins, OP_START_HANDLER);
				MONO_ADD_INS (tblock, ins);

				if (seq_points && clause->flags != MONO_EXCEPTION_CLAUSE_FINALLY) {
					/* finally clauses already have a seq point */
					NEW_SEQ_POINT (cfg, ins, clause->handler_offset, TRUE);
					MONO_ADD_INS (tblock, ins);
				}

				/* todo: is a fault block unsafe to optimize? */
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FAULT)
					tblock->flags |= BB_EXCEPTION_UNSAFE;
			}


			/*printf ("clause try IL_%04x to IL_%04x handler %d at IL_%04x to IL_%04x\n", clause->try_offset, clause->try_offset + clause->try_len, clause->flags, clause->handler_offset, clause->handler_offset + clause->handler_len);
			  while (p < end) {
			  printf ("%s", mono_disasm_code_one (NULL, method, p, &p));
			  }*/
			/* catch and filter blocks get the exception object on the stack */
			if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE ||
			    clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
				MonoInst *dummy_use;

				/* mostly like handle_stack_args (), but just sets the input args */
				/* printf ("handling clause at IL_%04x\n", clause->handler_offset); */
				tblock->in_scount = 1;
				tblock->in_stack = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*));
				tblock->in_stack [0] = mono_create_exvar_for_offset (cfg, clause->handler_offset);

				/* 
				 * Add a dummy use for the exvar so its liveness info will be
				 * correct.
				 */
				cfg->cbb = tblock;
				EMIT_NEW_DUMMY_USE (cfg, dummy_use, tblock->in_stack [0]);
				
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
					GET_BBLOCK (cfg, tblock, ip + clause->data.filter_offset);
					tblock->flags |= BB_EXCEPTION_HANDLER;
					tblock->real_offset = clause->data.filter_offset;
					tblock->in_scount = 1;
					tblock->in_stack = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*));
					/* The filter block shares the exvar with the handler block */
					tblock->in_stack [0] = mono_create_exvar_for_offset (cfg, clause->handler_offset);
					MONO_INST_NEW (cfg, ins, OP_START_HANDLER);
					MONO_ADD_INS (tblock, ins);
				}
			}

			if (clause->flags != MONO_EXCEPTION_CLAUSE_FILTER &&
					clause->data.catch_class &&
					cfg->generic_sharing_context &&
					mono_class_check_context_used (clause->data.catch_class)) {
				/*
				 * In shared generic code with catch
				 * clauses containing type variables
				 * the exception handling code has to
				 * be able to get to the rgctx.
				 * Therefore we have to make sure that
				 * the vtable/mrgctx argument (for
				 * static or generic methods) or the
				 * "this" argument (for non-static
				 * methods) are live.
				 */
				if ((method->flags & METHOD_ATTRIBUTE_STATIC) ||
						mini_method_get_context (method)->method_inst ||
						method->klass->valuetype) {
					mono_get_vtable_var (cfg);
				} else {
					MonoInst *dummy_use;

					EMIT_NEW_DUMMY_USE (cfg, dummy_use, arg_array [0]);
				}
			}
		}
	} else {
		arg_array = (MonoInst **) alloca (sizeof (MonoInst *) * num_args);
		cfg->cbb = start_bblock;
		cfg->args = arg_array;
		mono_save_args (cfg, sig, inline_args);
	}

	/* FIRST CODE BLOCK */
	NEW_BBLOCK (cfg, bblock);
	bblock->cil_code = ip;
	cfg->cbb = bblock;
	cfg->ip = ip;

	ADD_BBLOCK (cfg, bblock);

	if (cfg->method == method) {
		breakpoint_id = mono_debugger_method_has_breakpoint (method);
		if (breakpoint_id) {
			MONO_INST_NEW (cfg, ins, OP_BREAK);
			MONO_ADD_INS (bblock, ins);
		}
	}

	if (mono_security_cas_enabled ())
		secman = mono_security_manager_get_methods ();

	security = (secman && mono_security_method_has_declsec (method));
	/* at this point having security doesn't mean we have any code to generate */
	if (security && (cfg->method == method)) {
		/* Only Demand, NonCasDemand and DemandChoice requires code generation.
		 * And we do not want to enter the next section (with allocation) if we
		 * have nothing to generate */
		security = mono_declsec_get_demands (method, &actions);
	}

	/* we must Demand SecurityPermission.Unmanaged before P/Invoking */
	pinvoke = (secman && (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE));
	if (pinvoke) {
		MonoMethod *wrapped = mono_marshal_method_from_wrapper (method);
		if (wrapped && (wrapped->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
			MonoCustomAttrInfo* custom = mono_custom_attrs_from_method (wrapped);

			/* unless the method or it's class has the [SuppressUnmanagedCodeSecurity] attribute */
			if (custom && mono_custom_attrs_has_attr (custom, secman->suppressunmanagedcodesecurity)) {
				pinvoke = FALSE;
			}
			if (custom)
				mono_custom_attrs_free (custom);

			if (pinvoke) {
				custom = mono_custom_attrs_from_class (wrapped->klass);
				if (custom && mono_custom_attrs_has_attr (custom, secman->suppressunmanagedcodesecurity)) {
					pinvoke = FALSE;
				}
				if (custom)
					mono_custom_attrs_free (custom);
			}
		} else {
			/* not a P/Invoke after all */
			pinvoke = FALSE;
		}
	}
	
	/* we use a separate basic block for the initialization code */
	NEW_BBLOCK (cfg, init_localsbb);
	cfg->bb_init = init_localsbb;
	init_localsbb->real_offset = cfg->real_offset;
	start_bblock->next_bb = init_localsbb;
	init_localsbb->next_bb = bblock;
	link_bblock (cfg, start_bblock, init_localsbb);
	link_bblock (cfg, init_localsbb, bblock);
		
	cfg->cbb = init_localsbb;

	if (cfg->gsharedvt && cfg->method == method) {
		MonoGSharedVtMethodInfo *info;
		MonoInst *var, *locals_var;
		int dreg;

		info = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoGSharedVtMethodInfo));
		info->method = cfg->method;
		info->count_entries = 16;
		info->entries = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoRuntimeGenericContextInfoTemplate) * info->count_entries);
		cfg->gsharedvt_info = info;

		var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
		/* prevent it from being register allocated */
		//var->flags |= MONO_INST_VOLATILE;
		cfg->gsharedvt_info_var = var;

		ins = emit_get_rgctx_gsharedvt_method (cfg, mini_method_check_context_used (cfg, method), method, info);
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, var->dreg, ins->dreg);

		/* Allocate locals */
		locals_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
		/* prevent it from being register allocated */
		//locals_var->flags |= MONO_INST_VOLATILE;
		cfg->gsharedvt_locals_var = locals_var;

		dreg = alloc_ireg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, dreg, var->dreg, MONO_STRUCT_OFFSET (MonoGSharedVtMethodRuntimeInfo, locals_size));

		MONO_INST_NEW (cfg, ins, OP_LOCALLOC);
		ins->dreg = locals_var->dreg;
		ins->sreg1 = dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		cfg->gsharedvt_locals_var_ins = ins;
		
		cfg->flags |= MONO_CFG_HAS_ALLOCA;
		/*
		if (init_locals)
			ins->flags |= MONO_INST_INIT;
		*/
	}

	/* at this point we know, if security is TRUE, that some code needs to be generated */
	if (security && (cfg->method == method)) {
		MonoInst *args [2];

		cfg->stat_cas_demand_generation++;

		if (actions.demand.blob) {
			/* Add code for SecurityAction.Demand */
			EMIT_NEW_DECLSECCONST (cfg, args[0], image, actions.demand);
			EMIT_NEW_ICONST (cfg, args [1], actions.demand.size);
			/* Calls static void SecurityManager.InternalDemand (byte* permissions, int size); */
			mono_emit_method_call (cfg, secman->demand, args, NULL);
		}
		if (actions.noncasdemand.blob) {
			/* CLR 1.x uses a .noncasdemand (but 2.x doesn't) */
			/* For Mono we re-route non-CAS Demand to Demand (as the managed code must deal with it anyway) */
			EMIT_NEW_DECLSECCONST (cfg, args[0], image, actions.noncasdemand);
			EMIT_NEW_ICONST (cfg, args [1], actions.noncasdemand.size);
			/* Calls static void SecurityManager.InternalDemand (byte* permissions, int size); */
			mono_emit_method_call (cfg, secman->demand, args, NULL);
		}
		if (actions.demandchoice.blob) {
			/* New in 2.0, Demand must succeed for one of the permissions (i.e. not all) */
			EMIT_NEW_DECLSECCONST (cfg, args[0], image, actions.demandchoice);
			EMIT_NEW_ICONST (cfg, args [1], actions.demandchoice.size);
			/* Calls static void SecurityManager.InternalDemandChoice (byte* permissions, int size); */
			mono_emit_method_call (cfg, secman->demandchoice, args, NULL);
		}
	}

	/* we must Demand SecurityPermission.Unmanaged before p/invoking */
	if (pinvoke) {
		mono_emit_method_call (cfg, secman->demandunmanaged, NULL, NULL);
	}

	if (mono_security_core_clr_enabled ()) {
		/* check if this is native code, e.g. an icall or a p/invoke */
		if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) {
			MonoMethod *wrapped = mono_marshal_method_from_wrapper (method);
			if (wrapped) {
				gboolean pinvk = (wrapped->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL);
				gboolean icall = (wrapped->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL);

				/* if this ia a native call then it can only be JITted from platform code */
				if ((icall || pinvk) && method->klass && method->klass->image) {
					if (!mono_security_core_clr_is_platform_image (method->klass->image)) {
						MonoException *ex = icall ? mono_get_exception_security () : 
							mono_get_exception_method_access ();
						emit_throw_exception (cfg, ex);
					}
				}
			}
		}
	}

	CHECK_CFG_EXCEPTION;

	if (header->code_size == 0)
		UNVERIFIED;

	if (get_basic_blocks (cfg, header, cfg->real_offset, ip, end, &err_pos)) {
		ip = err_pos;
		UNVERIFIED;
	}

	if (cfg->method == method)
		mono_debug_init_method (cfg, bblock, breakpoint_id);

	for (n = 0; n < header->num_locals; ++n) {
		if (header->locals [n]->type == MONO_TYPE_VOID && !header->locals [n]->byref)
			UNVERIFIED;
	}
	class_inits = NULL;

	/* We force the vtable variable here for all shared methods
	   for the possibility that they might show up in a stack
	   trace where their exact instantiation is needed. */
	if (cfg->generic_sharing_context && method == cfg->method) {
		if ((method->flags & METHOD_ATTRIBUTE_STATIC) ||
				mini_method_get_context (method)->method_inst ||
				method->klass->valuetype) {
			mono_get_vtable_var (cfg);
		} else {
			/* FIXME: Is there a better way to do this?
			   We need the variable live for the duration
			   of the whole method. */
			cfg->args [0]->flags |= MONO_INST_VOLATILE;
		}
	}

	/* add a check for this != NULL to inlined methods */
	if (is_virtual_call) {
		MonoInst *arg_ins;

		NEW_ARGLOAD (cfg, arg_ins, 0);
		MONO_ADD_INS (cfg->cbb, arg_ins);
		MONO_EMIT_NEW_CHECK_THIS (cfg, arg_ins->dreg);
	}

	skip_dead_blocks = !dont_verify;
	if (skip_dead_blocks) {
		original_bb = bb = mono_basic_block_split (method, &cfg->error);
		CHECK_CFG_ERROR;
		g_assert (bb);
	}

	/* we use a spare stack slot in SWITCH and NEWOBJ and others */
	stack_start = sp = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst*) * (header->max_stack + 1));

	ins_flag = 0;
	start_new_bblock = 0;
	cfg->cbb = bblock;
	while (ip < end) {
		if (cfg->method == method)
			cfg->real_offset = ip - header->code;
		else
			cfg->real_offset = inline_offset;
		cfg->ip = ip;

		context_used = 0;
		
		if (start_new_bblock) {
			bblock->cil_length = ip - bblock->cil_code;
			if (start_new_bblock == 2) {
				g_assert (ip == tblock->cil_code);
			} else {
				GET_BBLOCK (cfg, tblock, ip);
			}
			bblock->next_bb = tblock;
			bblock = tblock;
			cfg->cbb = bblock;
			start_new_bblock = 0;
			for (i = 0; i < bblock->in_scount; ++i) {
				if (cfg->verbose_level > 3)
					printf ("loading %d from temp %d\n", i, (int)bblock->in_stack [i]->inst_c0);						
				EMIT_NEW_TEMPLOAD (cfg, ins, bblock->in_stack [i]->inst_c0);
				*sp++ = ins;
			}
			if (class_inits)
				g_slist_free (class_inits);
			class_inits = NULL;
		} else {
			if ((tblock = cfg->cil_offset_to_bb [ip - cfg->cil_start]) && (tblock != bblock)) {
				link_bblock (cfg, bblock, tblock);
				if (sp != stack_start) {
					handle_stack_args (cfg, stack_start, sp - stack_start);
					sp = stack_start;
					CHECK_UNVERIFIABLE (cfg);
				}
				bblock->next_bb = tblock;
				bblock = tblock;
				cfg->cbb = bblock;
				for (i = 0; i < bblock->in_scount; ++i) {
					if (cfg->verbose_level > 3)
						printf ("loading %d from temp %d\n", i, (int)bblock->in_stack [i]->inst_c0);						
					EMIT_NEW_TEMPLOAD (cfg, ins, bblock->in_stack [i]->inst_c0);
					*sp++ = ins;
				}
				g_slist_free (class_inits);
				class_inits = NULL;
			}
		}

		if (skip_dead_blocks) {
			int ip_offset = ip - header->code;

			if (ip_offset == bb->end)
				bb = bb->next;

			if (bb->dead) {
				int op_size = mono_opcode_size (ip, end);
				g_assert (op_size > 0); /*The BB formation pass must catch all bad ops*/

				if (cfg->verbose_level > 3) printf ("SKIPPING DEAD OP at %x\n", ip_offset);

				if (ip_offset + op_size == bb->end) {
					MONO_INST_NEW (cfg, ins, OP_NOP);
					MONO_ADD_INS (bblock, ins);
					start_new_bblock = 1;
				}

				ip += op_size;
				continue;
			}
		}
		/*
		 * Sequence points are points where the debugger can place a breakpoint.
		 * Currently, we generate these automatically at points where the IL
		 * stack is empty.
		 */
		if (seq_points && ((!sym_seq_points && (sp == stack_start)) || (sym_seq_points && mono_bitset_test_fast (seq_point_locs, ip - header->code)))) {
			/*
			 * Make methods interruptable at the beginning, and at the targets of
			 * backward branches.
			 * Also, do this at the start of every bblock in methods with clauses too,
			 * to be able to handle instructions with inprecise control flow like
			 * throw/endfinally.
			 * Backward branches are handled at the end of method-to-ir ().
			 */
			gboolean intr_loc = ip == header->code || (!cfg->cbb->last_ins && cfg->header->num_clauses);

			/* Avoid sequence points on empty IL like .volatile */
			// FIXME: Enable this
			//if (!(cfg->cbb->last_ins && cfg->cbb->last_ins->opcode == OP_SEQ_POINT)) {
			NEW_SEQ_POINT (cfg, ins, ip - header->code, intr_loc);
			if (sp != stack_start)
				ins->flags |= MONO_INST_NONEMPTY_STACK;
			MONO_ADD_INS (cfg->cbb, ins);

			if (sym_seq_points)
				mono_bitset_set_fast (seq_point_set_locs, ip - header->code);
		}

		bblock->real_offset = cfg->real_offset;

		if ((cfg->method == method) && cfg->coverage_info) {
			guint32 cil_offset = ip - header->code;
			cfg->coverage_info->data [cil_offset].cil_code = ip;

			/* TODO: Use an increment here */
#if defined(TARGET_X86)
			MONO_INST_NEW (cfg, ins, OP_STORE_MEM_IMM);
			ins->inst_p0 = &(cfg->coverage_info->data [cil_offset].count);
			ins->inst_imm = 1;
			MONO_ADD_INS (cfg->cbb, ins);
#else
			EMIT_NEW_PCONST (cfg, ins, &(cfg->coverage_info->data [cil_offset].count));
			MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STORE_MEMBASE_IMM, ins->dreg, 0, 1);
#endif
		}

		if (cfg->verbose_level > 3)
			printf ("converting (in B%d: stack: %d) %s", bblock->block_num, (int)(sp - stack_start), mono_disasm_code_one (NULL, method, ip, NULL));

		switch (*ip) {
		case CEE_NOP:
			if (seq_points && !sym_seq_points && sp != stack_start) {
				/*
				 * The C# compiler uses these nops to notify the JIT that it should
				 * insert seq points.
				 */
				NEW_SEQ_POINT (cfg, ins, ip - header->code, FALSE);
				MONO_ADD_INS (cfg->cbb, ins);
			}
			if (cfg->keep_cil_nops)
				MONO_INST_NEW (cfg, ins, OP_HARD_NOP);
			else
				MONO_INST_NEW (cfg, ins, OP_NOP);
			ip++;
			MONO_ADD_INS (bblock, ins);
			break;
		case CEE_BREAK:
			if (should_insert_brekpoint (cfg->method)) {
				ins = mono_emit_jit_icall (cfg, mono_debugger_agent_user_break, NULL);
			} else {
				MONO_INST_NEW (cfg, ins, OP_NOP);
			}
			ip++;
			MONO_ADD_INS (bblock, ins);
			break;
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3:
			CHECK_STACK_OVF (1);
			n = (*ip)-CEE_LDARG_0;
			CHECK_ARG (n);
			EMIT_NEW_ARGLOAD (cfg, ins, n);
			ip++;
			*sp++ = ins;
			break;
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3:
			CHECK_STACK_OVF (1);
			n = (*ip)-CEE_LDLOC_0;
			CHECK_LOCAL (n);
			EMIT_NEW_LOCLOAD (cfg, ins, n);
			ip++;
			*sp++ = ins;
			break;
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3: {
			CHECK_STACK (1);
			n = (*ip)-CEE_STLOC_0;
			CHECK_LOCAL (n);
			--sp;
			if (!dont_verify_stloc && target_type_is_incompatible (cfg, header->locals [n], *sp))
				UNVERIFIED;
			emit_stloc_ir (cfg, sp, header, n);
			++ip;
			inline_costs += 1;
			break;
			}
		case CEE_LDARG_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			n = ip [1];
			CHECK_ARG (n);
			EMIT_NEW_ARGLOAD (cfg, ins, n);
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_LDARGA_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			n = ip [1];
			CHECK_ARG (n);
			NEW_ARGLOADA (cfg, ins, n);
			MONO_ADD_INS (cfg->cbb, ins);
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_STARG_S:
			CHECK_OPSIZE (2);
			CHECK_STACK (1);
			--sp;
			n = ip [1];
			CHECK_ARG (n);
			if (!dont_verify_stloc && target_type_is_incompatible (cfg, param_types [ip [1]], *sp))
				UNVERIFIED;
			EMIT_NEW_ARGSTORE (cfg, ins, n, *sp);
			ip += 2;
			break;
		case CEE_LDLOC_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			n = ip [1];
			CHECK_LOCAL (n);
			EMIT_NEW_LOCLOAD (cfg, ins, n);
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_LDLOCA_S: {
			unsigned char *tmp_ip;
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			CHECK_LOCAL (ip [1]);

			if ((tmp_ip = emit_optimized_ldloca_ir (cfg, ip, end, 1))) {
				ip = tmp_ip;
				inline_costs += 1;
				break;
			}

			EMIT_NEW_LOCLOADA (cfg, ins, ip [1]);
			*sp++ = ins;
			ip += 2;
			break;
		}
		case CEE_STLOC_S:
			CHECK_OPSIZE (2);
			CHECK_STACK (1);
			--sp;
			CHECK_LOCAL (ip [1]);
			if (!dont_verify_stloc && target_type_is_incompatible (cfg, header->locals [ip [1]], *sp))
				UNVERIFIED;
			emit_stloc_ir (cfg, sp, header, ip [1]);
			ip += 2;
			inline_costs += 1;
			break;
		case CEE_LDNULL:
			CHECK_STACK_OVF (1);
			EMIT_NEW_PCONST (cfg, ins, NULL);
			ins->type = STACK_OBJ;
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4_M1:
			CHECK_STACK_OVF (1);
			EMIT_NEW_ICONST (cfg, ins, -1);
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4_0:
		case CEE_LDC_I4_1:
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8:
			CHECK_STACK_OVF (1);
			EMIT_NEW_ICONST (cfg, ins, (*ip) - CEE_LDC_I4_0);
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			++ip;
			EMIT_NEW_ICONST (cfg, ins, *((signed char*)ip));
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4:
			CHECK_OPSIZE (5);
			CHECK_STACK_OVF (1);
			EMIT_NEW_ICONST (cfg, ins, (gint32)read32 (ip + 1));
			ip += 5;
			*sp++ = ins;
			break;
		case CEE_LDC_I8:
			CHECK_OPSIZE (9);
			CHECK_STACK_OVF (1);
			MONO_INST_NEW (cfg, ins, OP_I8CONST);
			ins->type = STACK_I8;
			ins->dreg = alloc_dreg (cfg, STACK_I8);
			++ip;
			ins->inst_l = (gint64)read64 (ip);
			MONO_ADD_INS (bblock, ins);
			ip += 8;
			*sp++ = ins;
			break;
		case CEE_LDC_R4: {
			float *f;
			gboolean use_aotconst = FALSE;

#ifdef TARGET_POWERPC
			/* FIXME: Clean this up */
			if (cfg->compile_aot)
				use_aotconst = TRUE;
#endif

			/* FIXME: we should really allocate this only late in the compilation process */
			f = mono_domain_alloc (cfg->domain, sizeof (float));
			CHECK_OPSIZE (5);
			CHECK_STACK_OVF (1);

			if (use_aotconst) {
				MonoInst *cons;
				int dreg;

				EMIT_NEW_AOTCONST (cfg, cons, MONO_PATCH_INFO_R4, f);

				dreg = alloc_freg (cfg);
				EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADR4_MEMBASE, dreg, cons->dreg, 0);
				ins->type = STACK_R8;
			} else {
				MONO_INST_NEW (cfg, ins, OP_R4CONST);
				ins->type = STACK_R8;
				ins->dreg = alloc_dreg (cfg, STACK_R8);
				ins->inst_p0 = f;
				MONO_ADD_INS (bblock, ins);
			}
			++ip;
			readr4 (ip, f);
			ip += 4;
			*sp++ = ins;			
			break;
		}
		case CEE_LDC_R8: {
			double *d;
			gboolean use_aotconst = FALSE;

#ifdef TARGET_POWERPC
			/* FIXME: Clean this up */
			if (cfg->compile_aot)
				use_aotconst = TRUE;
#endif

			/* FIXME: we should really allocate this only late in the compilation process */
			d = mono_domain_alloc (cfg->domain, sizeof (double));
			CHECK_OPSIZE (9);
			CHECK_STACK_OVF (1);

			if (use_aotconst) {
				MonoInst *cons;
				int dreg;

				EMIT_NEW_AOTCONST (cfg, cons, MONO_PATCH_INFO_R8, d);

				dreg = alloc_freg (cfg);
				EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOADR8_MEMBASE, dreg, cons->dreg, 0);
				ins->type = STACK_R8;
			} else {
				MONO_INST_NEW (cfg, ins, OP_R8CONST);
				ins->type = STACK_R8;
				ins->dreg = alloc_dreg (cfg, STACK_R8);
				ins->inst_p0 = d;
				MONO_ADD_INS (bblock, ins);
			}
			++ip;
			readr8 (ip, d);
			ip += 8;
			*sp++ = ins;
			break;
		}
		case CEE_DUP: {
			MonoInst *temp, *store;
			CHECK_STACK (1);
			CHECK_STACK_OVF (1);
			sp--;
			ins = *sp;

			temp = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
			EMIT_NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);

			EMIT_NEW_TEMPLOAD (cfg, ins, temp->inst_c0);
			*sp++ = ins;

			EMIT_NEW_TEMPLOAD (cfg, ins, temp->inst_c0);
			*sp++ = ins;

			++ip;
			inline_costs += 2;
			break;
		}
		case CEE_POP:
			CHECK_STACK (1);
			ip++;
			--sp;

#ifdef TARGET_X86
			if (sp [0]->type == STACK_R8)
				/* we need to pop the value from the x86 FP stack */
				MONO_EMIT_NEW_UNALU (cfg, OP_X86_FPOP, -1, sp [0]->dreg);
#endif
			break;
		case CEE_JMP: {
			MonoCallInst *call;

			INLINE_FAILURE ("jmp");
			GSHAREDVT_FAILURE (*ip);

			CHECK_OPSIZE (5);
			if (stack_start != sp)
				UNVERIFIED;
			token = read32 (ip + 1);
			/* FIXME: check the signature matches */
			cmethod = mini_get_method (cfg, method, token, NULL, generic_context);

			if (!cmethod || mono_loader_get_last_error ())
				LOAD_ERROR;
 
			if (cfg->generic_sharing_context && mono_method_check_context_used (cmethod))
				GENERIC_SHARING_FAILURE (CEE_JMP);

			if (mono_security_cas_enabled ())
				CHECK_CFG_EXCEPTION;

			emit_instrumentation_call (cfg, mono_profiler_method_leave);

			if (ARCH_HAVE_OP_TAIL_CALL) {
				MonoMethodSignature *fsig = mono_method_signature (cmethod);
				int i, n;

				/* Handle tail calls similarly to calls */
				n = fsig->param_count + fsig->hasthis;

				DISABLE_AOT (cfg);

				MONO_INST_NEW_CALL (cfg, call, OP_TAILCALL);
				call->method = cmethod;
				call->tail_call = TRUE;
				call->signature = mono_method_signature (cmethod);
				call->args = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * n);
				call->inst.inst_p0 = cmethod;
				for (i = 0; i < n; ++i)
					EMIT_NEW_ARGLOAD (cfg, call->args [i], i);

				mono_arch_emit_call (cfg, call);
				cfg->param_area = MAX(cfg->param_area, call->stack_usage);
				MONO_ADD_INS (bblock, (MonoInst*)call);
			} else {
				for (i = 0; i < num_args; ++i)
					/* Prevent arguments from being optimized away */
					arg_array [i]->flags |= MONO_INST_VOLATILE;

				MONO_INST_NEW_CALL (cfg, call, OP_JMP);
				ins = (MonoInst*)call;
				ins->inst_p0 = cmethod;
				MONO_ADD_INS (bblock, ins);
			}

			ip += 5;
			start_new_bblock = 1;
			break;
		}
		case CEE_CALLI:
		case CEE_CALL:
		case CEE_CALLVIRT: {
			MonoInst *addr = NULL;
			MonoMethodSignature *fsig = NULL;
			int array_rank = 0;
			int virtual = *ip == CEE_CALLVIRT;
			int calli = *ip == CEE_CALLI;
			gboolean pass_imt_from_rgctx = FALSE;
			MonoInst *imt_arg = NULL;
			MonoInst *keep_this_alive = NULL;
			gboolean pass_vtable = FALSE;
			gboolean pass_mrgctx = FALSE;
			MonoInst *vtable_arg = NULL;
			gboolean check_this = FALSE;
			gboolean supported_tail_call = FALSE;
			gboolean tail_call = FALSE;
			gboolean need_seq_point = FALSE;
			guint32 call_opcode = *ip;
			gboolean emit_widen = TRUE;
			gboolean push_res = TRUE;
			gboolean skip_ret = FALSE;
			gboolean delegate_invoke = FALSE;

			CHECK_OPSIZE (5);
			token = read32 (ip + 1);

			ins = NULL;

			if (calli) {
				//GSHAREDVT_FAILURE (*ip);
				cmethod = NULL;
				CHECK_STACK (1);
				--sp;
				addr = *sp;
				fsig = mini_get_signature (method, token, generic_context);
				n = fsig->param_count + fsig->hasthis;

				if (method->dynamic && fsig->pinvoke) {
					MonoInst *args [3];

					/*
					 * This is a call through a function pointer using a pinvoke
					 * signature. Have to create a wrapper and call that instead.
					 * FIXME: This is very slow, need to create a wrapper at JIT time
					 * instead based on the signature.
					 */
					EMIT_NEW_IMAGECONST (cfg, args [0], method->klass->image);
					EMIT_NEW_PCONST (cfg, args [1], fsig);
					args [2] = addr;
					addr = mono_emit_jit_icall (cfg, mono_get_native_calli_wrapper, args);
				}
			} else {
				MonoMethod *cil_method;

				cmethod = mini_get_method (cfg, method, token, NULL, generic_context);
				cil_method = cmethod;
				
				if (constrained_call) {
					if (method->wrapper_type != MONO_WRAPPER_NONE) {
						if (cfg->verbose_level > 2)
							printf ("DM Constrained call to %s\n", mono_type_get_full_name (constrained_call));
						if (!((constrained_call->byval_arg.type == MONO_TYPE_VAR ||
							   constrained_call->byval_arg.type == MONO_TYPE_MVAR) &&
							  cfg->generic_sharing_context)) {
							cmethod = mono_get_method_constrained_with_method (image, cil_method, constrained_call, generic_context);
						}
					} else {
						if (cfg->verbose_level > 2)
							printf ("Constrained call to %s\n", mono_type_get_full_name (constrained_call));

						if ((constrained_call->byval_arg.type == MONO_TYPE_VAR || constrained_call->byval_arg.type == MONO_TYPE_MVAR) && cfg->generic_sharing_context) {
							/* 
							 * This is needed since get_method_constrained can't find 
							 * the method in klass representing a type var.
							 * The type var is guaranteed to be a reference type in this
							 * case.
							 */
							if (!mini_is_gsharedvt_klass (cfg, constrained_call))
								g_assert (!cmethod->klass->valuetype);
						} else {
							cmethod = mono_get_method_constrained (image, token, constrained_call, generic_context, &cil_method);
						}
					}
				}
					
				if (!cmethod || mono_loader_get_last_error ())
					LOAD_ERROR;
				if (!dont_verify && !cfg->skip_visibility) {
					MonoMethod *target_method = cil_method;
					if (method->is_inflated) {
						target_method = mini_get_method_allow_open (method, token, NULL, &(mono_method_get_generic_container (method_definition)->context));
					}
					if (!mono_method_can_access_method (method_definition, target_method) &&
						!mono_method_can_access_method (method, cil_method))
						METHOD_ACCESS_FAILURE (method, cil_method);
				}

				if (mono_security_core_clr_enabled ())
					ensure_method_is_allowed_to_call_method (cfg, method, cil_method, bblock, ip);

				if (!virtual && (cmethod->flags & METHOD_ATTRIBUTE_ABSTRACT))
					/* MS.NET seems to silently convert this to a callvirt */
					virtual = 1;

				{
					/*
					 * MS.NET accepts non virtual calls to virtual final methods of transparent proxy classes and
					 * converts to a callvirt.
					 *
					 * tests/bug-515884.il is an example of this behavior
					 */
					const int test_flags = METHOD_ATTRIBUTE_VIRTUAL | METHOD_ATTRIBUTE_FINAL | METHOD_ATTRIBUTE_STATIC;
					const int expected_flags = METHOD_ATTRIBUTE_VIRTUAL | METHOD_ATTRIBUTE_FINAL;
					if (!virtual && mono_class_is_marshalbyref (cmethod->klass) && (cmethod->flags & test_flags) == expected_flags && cfg->method->wrapper_type == MONO_WRAPPER_NONE)
						virtual = 1;
				}

				if (!cmethod->klass->inited)
					if (!mono_class_init (cmethod->klass))
						TYPE_LOAD_ERROR (cmethod->klass);

				if (cmethod->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL &&
				    mini_class_is_system_array (cmethod->klass)) {
					array_rank = cmethod->klass->rank;
					fsig = mono_method_signature (cmethod);
				} else {
					fsig = mono_method_signature (cmethod);

					if (!fsig)
						LOAD_ERROR;

					if (fsig->pinvoke) {
						MonoMethod *wrapper = mono_marshal_get_native_wrapper (cmethod,
							check_for_pending_exc, cfg->compile_aot);
						fsig = mono_method_signature (wrapper);
					} else if (constrained_call) {
						fsig = mono_method_signature (cmethod);
					} else {
						fsig = mono_method_get_signature_checked (cmethod, image, token, generic_context, &cfg->error);
						CHECK_CFG_ERROR;
					}
				}

				mono_save_token_info (cfg, image, token, cil_method);

				if (!MONO_TYPE_IS_VOID (fsig->ret)) {
					/*
					 * Need to emit an implicit seq point after every non-void call so single stepping through nested calls like
					 * foo (bar (), baz ())
					 * works correctly. MS does this also:
					 * http://stackoverflow.com/questions/6937198/making-your-net-language-step-correctly-in-the-debugger
					 * The problem with this approach is that the debugger will stop after all calls returning a value,
					 * even for simple cases, like:
					 * int i = foo ();
					 */
					/* Special case a few common successor opcodes */
					if (!(ip + 5 < end && (ip [5] == CEE_POP || ip [5] == CEE_NOP)) && !(seq_point_locs && mono_bitset_test_fast (seq_point_locs, ip + 5 - header->code)))
						need_seq_point = TRUE;
				}

				n = fsig->param_count + fsig->hasthis;

				/* Don't support calls made using type arguments for now */
				/*
				if (cfg->gsharedvt) {
					if (mini_is_gsharedvt_signature (cfg, fsig))
						GSHAREDVT_FAILURE (*ip);
				}
				*/

				if (mono_security_cas_enabled ()) {
					if (check_linkdemand (cfg, method, cmethod))
						INLINE_FAILURE ("linkdemand");
					CHECK_CFG_EXCEPTION;
				}

				if (cmethod->string_ctor && method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE)
					g_assert_not_reached ();
			}

			if (!cfg->generic_sharing_context && cmethod && cmethod->klass->generic_container)
				UNVERIFIED;

			if (!cfg->generic_sharing_context && cmethod)
				g_assert (!mono_method_check_context_used (cmethod));

			CHECK_STACK (n);

			//g_assert (!virtual || fsig->hasthis);

			sp -= n;

			if (constrained_call) {
				if (mini_is_gsharedvt_klass (cfg, constrained_call)) {
					/*
					 * Constrained calls need to behave differently at runtime dependending on whenever the receiver is instantiated as ref type or as a vtype.
					 */
					if ((cmethod->klass != mono_defaults.object_class) && constrained_call->valuetype && cmethod->klass->valuetype) {
						/* The 'Own method' case below */
					} else if (cmethod->klass->image != mono_defaults.corlib && !(cmethod->klass->flags & TYPE_ATTRIBUTE_INTERFACE) && !cmethod->klass->valuetype) {
						/* 'The type parameter is instantiated as a reference type' case below. */
					} else if (((cmethod->klass == mono_defaults.object_class) || (cmethod->klass->flags & TYPE_ATTRIBUTE_INTERFACE) || (!cmethod->klass->valuetype && cmethod->klass->image != mono_defaults.corlib)) &&
							   (MONO_TYPE_IS_VOID (fsig->ret) || MONO_TYPE_IS_PRIMITIVE (fsig->ret) || MONO_TYPE_IS_REFERENCE (fsig->ret) || MONO_TYPE_ISSTRUCT (fsig->ret) || mini_is_gsharedvt_type (cfg, fsig->ret)) &&
							   (fsig->param_count == 0 || (!fsig->hasthis && fsig->param_count == 1) || (fsig->param_count == 1 && (MONO_TYPE_IS_REFERENCE (fsig->params [0]) || mini_is_gsharedvt_type (cfg, fsig->params [0]))))) {
						MonoInst *args [16];

						/*
						 * This case handles calls to
						 * - object:ToString()/Equals()/GetHashCode(),
						 * - System.IComparable<T>:CompareTo()
						 * - System.IEquatable<T>:Equals ()
						 * plus some simple interface calls enough to support AsyncTaskMethodBuilder.
						 */

						args [0] = sp [0];
						if (mono_method_check_context_used (cmethod))
							args [1] = emit_get_rgctx_method (cfg, mono_method_check_context_used (cmethod), cmethod, MONO_RGCTX_INFO_METHOD);
						else
							EMIT_NEW_METHODCONST (cfg, args [1], cmethod);
						args [2] = emit_get_rgctx_klass (cfg, mono_class_check_context_used (constrained_call), constrained_call, MONO_RGCTX_INFO_KLASS);

						/* !fsig->hasthis is for the wrapper for the Object.GetType () icall */
						if (fsig->hasthis && fsig->param_count) {
							/* Pass the arguments using a localloc-ed array using the format expected by runtime_invoke () */
							MONO_INST_NEW (cfg, ins, OP_LOCALLOC_IMM);
							ins->dreg = alloc_preg (cfg);
							ins->inst_imm = fsig->param_count * sizeof (mgreg_t);
							MONO_ADD_INS (cfg->cbb, ins);
							args [4] = ins;

							if (mini_is_gsharedvt_type (cfg, fsig->params [0])) {
								int addr_reg;

								args [3] = emit_get_gsharedvt_info_klass (cfg, mono_class_from_mono_type (fsig->params [0]), MONO_RGCTX_INFO_CLASS_BOX_TYPE);

								EMIT_NEW_VARLOADA_VREG (cfg, ins, sp [1]->dreg, fsig->params [0]);
								addr_reg = ins->dreg;
								EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STORE_MEMBASE_REG, args [4]->dreg, 0, addr_reg);
							} else {
								EMIT_NEW_ICONST (cfg, args [3], 0);
								EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STORE_MEMBASE_REG, args [4]->dreg, 0, sp [1]->dreg);
							}
						} else {
							EMIT_NEW_ICONST (cfg, args [3], 0);
							EMIT_NEW_ICONST (cfg, args [4], 0);
						}
						ins = mono_emit_jit_icall (cfg, mono_gsharedvt_constrained_call, args);
						emit_widen = FALSE;

						if (mini_is_gsharedvt_type (cfg, fsig->ret)) {
							ins = handle_unbox_gsharedvt (cfg, mono_class_from_mono_type (fsig->ret), ins, &bblock);
						} else if (MONO_TYPE_IS_PRIMITIVE (fsig->ret) || MONO_TYPE_ISSTRUCT (fsig->ret)) {
							MonoInst *add;

							/* Unbox */
							NEW_BIALU_IMM (cfg, add, OP_ADD_IMM, alloc_dreg (cfg, STACK_MP), ins->dreg, sizeof (MonoObject));
							MONO_ADD_INS (cfg->cbb, add);
							/* Load value */
							NEW_LOAD_MEMBASE_TYPE (cfg, ins, fsig->ret, add->dreg, 0);
							MONO_ADD_INS (cfg->cbb, ins);
							/* ins represents the call result */
						}

						goto call_end;
					} else {
						GSHAREDVT_FAILURE (*ip);
					}
				}
				/*
				 * We have the `constrained.' prefix opcode.
				 */
				if (constrained_call->valuetype && (cmethod->klass == mono_defaults.object_class || cmethod->klass == mono_defaults.enum_class->parent || cmethod->klass == mono_defaults.enum_class)) {
					/*
					 * The type parameter is instantiated as a valuetype,
					 * but that type doesn't override the method we're
					 * calling, so we need to box `this'.
					 */
					EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &constrained_call->byval_arg, sp [0]->dreg, 0);
					ins->klass = constrained_call;
					sp [0] = handle_box (cfg, ins, constrained_call, mono_class_check_context_used (constrained_call), &bblock);
					CHECK_CFG_EXCEPTION;
				} else if (!constrained_call->valuetype) {
					int dreg = alloc_ireg_ref (cfg);

					/*
					 * The type parameter is instantiated as a reference
					 * type.  We have a managed pointer on the stack, so
					 * we need to dereference it here.
					 */
					EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, dreg, sp [0]->dreg, 0);
					ins->type = STACK_OBJ;
					sp [0] = ins;
				} else {
					if (cmethod->klass->valuetype) {
						/* Own method */
					} else {
						/* Interface method */
						int ioffset, slot;

						mono_class_setup_vtable (constrained_call);
						CHECK_TYPELOAD (constrained_call);
						ioffset = mono_class_interface_offset (constrained_call, cmethod->klass);
						if (ioffset == -1)
							TYPE_LOAD_ERROR (constrained_call);
						slot = mono_method_get_vtable_slot (cmethod);
						if (slot == -1)
							TYPE_LOAD_ERROR (cmethod->klass);
						cmethod = constrained_call->vtable [ioffset + slot];

						if (cmethod->klass == mono_defaults.enum_class) {
							/* Enum implements some interfaces, so treat this as the first case */
							EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &constrained_call->byval_arg, sp [0]->dreg, 0);
							ins->klass = constrained_call;
							sp [0] = handle_box (cfg, ins, constrained_call, mono_class_check_context_used (constrained_call), &bblock);
							CHECK_CFG_EXCEPTION;
						}
					}
					virtual = 0;
				}
				constrained_call = NULL;
			}

			if (!calli && check_call_signature (cfg, fsig, sp))
				UNVERIFIED;

#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
			if (cmethod && (cmethod->klass->parent == mono_defaults.multicastdelegate_class) && !strcmp (cmethod->name, "Invoke"))
				delegate_invoke = TRUE;
#endif

			if (cmethod && (cfg->opt & MONO_OPT_INTRINS) && (ins = mini_emit_inst_for_sharable_method (cfg, cmethod, fsig, sp))) {
				bblock = cfg->cbb;
				if (!MONO_TYPE_IS_VOID (fsig->ret)) {
					type_to_eval_stack_type ((cfg), fsig->ret, ins);
					emit_widen = FALSE;
				}

				goto call_end;
			}

			/* 
			 * If the callee is a shared method, then its static cctor
			 * might not get called after the call was patched.
			 */
			if (cfg->generic_sharing_context && cmethod && cmethod->klass != method->klass && cmethod->klass->generic_class && mono_method_is_generic_sharable (cmethod, TRUE) && mono_class_needs_cctor_run (cmethod->klass, method)) {
				emit_generic_class_init (cfg, cmethod->klass);
				CHECK_TYPELOAD (cmethod->klass);
			}

			if (cmethod)
				check_method_sharing (cfg, cmethod, &pass_vtable, &pass_mrgctx);

			if (cfg->generic_sharing_context && cmethod) {
				MonoGenericContext *cmethod_context = mono_method_get_context (cmethod);

				context_used = mini_method_check_context_used (cfg, cmethod);

				if (context_used && (cmethod->klass->flags & TYPE_ATTRIBUTE_INTERFACE)) {
					/* Generic method interface
					   calls are resolved via a
					   helper function and don't
					   need an imt. */
					if (!cmethod_context || !cmethod_context->method_inst)
						pass_imt_from_rgctx = TRUE;
				}

				/*
				 * If a shared method calls another
				 * shared method then the caller must
				 * have a generic sharing context
				 * because the magic trampoline
				 * requires it.  FIXME: We shouldn't
				 * have to force the vtable/mrgctx
				 * variable here.  Instead there
				 * should be a flag in the cfg to
				 * request a generic sharing context.
				 */
				if (context_used &&
						((method->flags & METHOD_ATTRIBUTE_STATIC) || method->klass->valuetype))
					mono_get_vtable_var (cfg);
			}

			if (pass_vtable) {
				if (context_used) {
					vtable_arg = emit_get_rgctx_klass (cfg, context_used, cmethod->klass, MONO_RGCTX_INFO_VTABLE);
				} else {
					MonoVTable *vtable = mono_class_vtable (cfg->domain, cmethod->klass);

					CHECK_TYPELOAD (cmethod->klass);
					EMIT_NEW_VTABLECONST (cfg, vtable_arg, vtable);
				}
			}

			if (pass_mrgctx) {
				g_assert (!vtable_arg);

				if (!cfg->compile_aot) {
					/* 
					 * emit_get_rgctx_method () calls mono_class_vtable () so check 
					 * for type load errors before.
					 */
					mono_class_setup_vtable (cmethod->klass);
					CHECK_TYPELOAD (cmethod->klass);
				}

				vtable_arg = emit_get_rgctx_method (cfg, context_used, cmethod, MONO_RGCTX_INFO_METHOD_RGCTX);

				/* !marshalbyref is needed to properly handle generic methods + remoting */
				if ((!(cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
					 MONO_METHOD_IS_FINAL (cmethod)) &&
					!mono_class_is_marshalbyref (cmethod->klass)) {
					if (virtual)
						check_this = TRUE;
					virtual = 0;
				}
			}

			if (pass_imt_from_rgctx) {
				g_assert (!pass_vtable);
				g_assert (cmethod);

				imt_arg = emit_get_rgctx_method (cfg, context_used,
					cmethod, MONO_RGCTX_INFO_METHOD);
			}

			if (check_this)
				MONO_EMIT_NEW_CHECK_THIS (cfg, sp [0]->dreg);

			/* Calling virtual generic methods */
			if (cmethod && virtual && 
			    (cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL) && 
		 	    !(MONO_METHOD_IS_FINAL (cmethod) && 
			      cmethod->wrapper_type != MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK) &&
			    fsig->generic_param_count && 
				!(cfg->gsharedvt && mini_is_gsharedvt_signature (cfg, fsig))) {
				MonoInst *this_temp, *this_arg_temp, *store;
				MonoInst *iargs [4];
				gboolean use_imt = FALSE;

				g_assert (fsig->is_inflated);

				/* Prevent inlining of methods that contain indirect calls */
				INLINE_FAILURE ("virtual generic call");

				if (cfg->gsharedvt && mini_is_gsharedvt_signature (cfg, fsig))
					GSHAREDVT_FAILURE (*ip);

#if MONO_ARCH_HAVE_GENERALIZED_IMT_THUNK && defined(MONO_ARCH_GSHARED_SUPPORTED)
				if (cmethod->wrapper_type == MONO_WRAPPER_NONE && mono_use_imt)
					use_imt = TRUE;
#endif

				if (use_imt) {
					g_assert (!imt_arg);
					if (!context_used)
						g_assert (cmethod->is_inflated);
					imt_arg = emit_get_rgctx_method (cfg, context_used,
													 cmethod, MONO_RGCTX_INFO_METHOD);
					ins = mono_emit_method_call_full (cfg, cmethod, fsig, FALSE, sp, sp [0], imt_arg, NULL);
				} else {
					this_temp = mono_compile_create_var (cfg, type_from_stack_type (sp [0]), OP_LOCAL);
					NEW_TEMPSTORE (cfg, store, this_temp->inst_c0, sp [0]);
					MONO_ADD_INS (bblock, store);

					/* FIXME: This should be a managed pointer */
					this_arg_temp = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);

					EMIT_NEW_TEMPLOAD (cfg, iargs [0], this_temp->inst_c0);
					iargs [1] = emit_get_rgctx_method (cfg, context_used,
													   cmethod, MONO_RGCTX_INFO_METHOD);
					EMIT_NEW_TEMPLOADA (cfg, iargs [2], this_arg_temp->inst_c0);
					addr = mono_emit_jit_icall (cfg,
												mono_helper_compile_generic_method, iargs);

					EMIT_NEW_TEMPLOAD (cfg, sp [0], this_arg_temp->inst_c0);

					ins = (MonoInst*)mono_emit_calli (cfg, fsig, sp, addr, NULL, NULL);
				}

				goto call_end;
			}

			/*
			 * Implement a workaround for the inherent races involved in locking:
			 * Monitor.Enter ()
			 * try {
			 * } finally {
			 *    Monitor.Exit ()
			 * }
			 * If a thread abort happens between the call to Monitor.Enter () and the start of the
			 * try block, the Exit () won't be executed, see:
			 * http://www.bluebytesoftware.com/blog/2007/01/30/MonitorEnterThreadAbortsAndOrphanedLocks.aspx
			 * To work around this, we extend such try blocks to include the last x bytes
			 * of the Monitor.Enter () call.
			 */
			if (cmethod && cmethod->klass == mono_defaults.monitor_class && !strcmp (cmethod->name, "Enter") && mono_method_signature (cmethod)->param_count == 1) {
				MonoBasicBlock *tbb;

				GET_BBLOCK (cfg, tbb, ip + 5);
				/* 
				 * Only extend try blocks with a finally, to avoid catching exceptions thrown
				 * from Monitor.Enter like ArgumentNullException.
				 */
				if (tbb->try_start && MONO_REGION_FLAGS(tbb->region) == MONO_EXCEPTION_CLAUSE_FINALLY) {
					/* Mark this bblock as needing to be extended */
					tbb->extend_try_block = TRUE;
				}
			}

			/* Conversion to a JIT intrinsic */
			if (cmethod && (cfg->opt & MONO_OPT_INTRINS) && (ins = mini_emit_inst_for_method (cfg, cmethod, fsig, sp))) {
				bblock = cfg->cbb;
				if (!MONO_TYPE_IS_VOID (fsig->ret)) {
					type_to_eval_stack_type ((cfg), fsig->ret, ins);
					emit_widen = FALSE;
				}
				goto call_end;
			}

			/* Inlining */
			if (cmethod && (cfg->opt & MONO_OPT_INLINE) &&
				(!virtual || !(cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL) || MONO_METHOD_IS_FINAL (cmethod)) &&
			    mono_method_check_inlining (cfg, cmethod)) {
				int costs;
				gboolean always = FALSE;

				if ((cmethod->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
					(cmethod->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
					/* Prevent inlining of methods that call wrappers */
					INLINE_FAILURE ("wrapper call");
					cmethod = mono_marshal_get_native_wrapper (cmethod, check_for_pending_exc, FALSE);
					always = TRUE;
				}

 				costs = inline_method (cfg, cmethod, fsig, sp, ip, cfg->real_offset, always, &bblock);
				if (costs) {
					cfg->real_offset += 5;

 					if (!MONO_TYPE_IS_VOID (fsig->ret)) {
						/* *sp is already set by inline_method */
 						sp++;
						push_res = FALSE;
					}

					inline_costs += costs;

					goto call_end;
				}
			}

			/* Tail recursion elimination */
			if ((cfg->opt & MONO_OPT_TAILC) && call_opcode == CEE_CALL && cmethod == method && ip [5] == CEE_RET && !vtable_arg) {
				gboolean has_vtargs = FALSE;
				int i;

				/* Prevent inlining of methods with tail calls (the call stack would be altered) */
				INLINE_FAILURE ("tail call");

				/* keep it simple */
				for (i =  fsig->param_count - 1; i >= 0; i--) {
					if (MONO_TYPE_ISSTRUCT (mono_method_signature (cmethod)->params [i])) 
						has_vtargs = TRUE;
				}

				if (!has_vtargs) {
					for (i = 0; i < n; ++i)
						EMIT_NEW_ARGSTORE (cfg, ins, i, sp [i]);
					MONO_INST_NEW (cfg, ins, OP_BR);
					MONO_ADD_INS (bblock, ins);
					tblock = start_bblock->out_bb [0];
					link_bblock (cfg, bblock, tblock);
					ins->inst_target_bb = tblock;
					start_new_bblock = 1;

					/* skip the CEE_RET, too */
					if (ip_in_bb (cfg, bblock, ip + 5))
						skip_ret = TRUE;
					push_res = FALSE;
					goto call_end;
				}
			}

			inline_costs += 10 * num_calls++;

			/*
			 * Making generic calls out of gsharedvt methods.
			 * This needs to be used for all generic calls, not just ones with a gsharedvt signature, to avoid
			 * patching gshared method addresses into a gsharedvt method.
			 */
			if (cmethod && cfg->gsharedvt && (mini_is_gsharedvt_signature (cfg, fsig) || cmethod->is_inflated || cmethod->klass->generic_class)) {
				MonoRgctxInfoType info_type;

				if (virtual) {
					//if (cmethod->klass->flags & TYPE_ATTRIBUTE_INTERFACE)
						//GSHAREDVT_FAILURE (*ip);
					// disable for possible remoting calls
					if (fsig->hasthis && (mono_class_is_marshalbyref (method->klass) || method->klass == mono_defaults.object_class))
						GSHAREDVT_FAILURE (*ip);
					if (fsig->generic_param_count) {
						/* virtual generic call */
						g_assert (mono_use_imt);
						g_assert (!imt_arg);
						/* Same as the virtual generic case above */
						imt_arg = emit_get_rgctx_method (cfg, context_used,
														 cmethod, MONO_RGCTX_INFO_METHOD);
						/* This is not needed, as the trampoline code will pass one, and it might be passed in the same reg as the imt arg */
						vtable_arg = NULL;
					} else if ((cmethod->klass->flags & TYPE_ATTRIBUTE_INTERFACE) && !imt_arg) {
						/* This can happen when we call a fully instantiated iface method */
						imt_arg = emit_get_rgctx_method (cfg, context_used,
														 cmethod, MONO_RGCTX_INFO_METHOD);
						vtable_arg = NULL;
					}
				}

				if (cmethod->klass->rank && cmethod->klass->byval_arg.type != MONO_TYPE_SZARRAY)
					/* test_0_multi_dim_arrays () in gshared.cs */
					GSHAREDVT_FAILURE (*ip);

				if ((cmethod->klass->parent == mono_defaults.multicastdelegate_class) && (!strcmp (cmethod->name, "Invoke")))
					keep_this_alive = sp [0];

				if (virtual && (cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL))
					info_type = MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE_VIRT;
				else
					info_type = MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE;
				addr = emit_get_rgctx_gsharedvt_call (cfg, context_used, fsig, cmethod, info_type);

				ins = (MonoInst*)mono_emit_calli (cfg, fsig, sp, addr, imt_arg, vtable_arg);
				goto call_end;
			} else if (calli && cfg->gsharedvt && mini_is_gsharedvt_signature (cfg, fsig)) {
				/*
				 * We pass the address to the gsharedvt trampoline in the rgctx reg
				 */
				MonoInst *callee = addr;

				if (method->wrapper_type != MONO_WRAPPER_DELEGATE_INVOKE)
					/* Not tested */
					GSHAREDVT_FAILURE (*ip);

				addr = emit_get_rgctx_sig (cfg, context_used,
											  fsig, MONO_RGCTX_INFO_SIG_GSHAREDVT_OUT_TRAMPOLINE_CALLI);
				ins = (MonoInst*)mono_emit_calli (cfg, fsig, sp, addr, NULL, callee);
				goto call_end;
			}

			/* Generic sharing */

			/*
			 * Use this if the callee is gsharedvt sharable too, since
			 * at runtime we might find an instantiation so the call cannot
			 * be patched (the 'no_patch' code path in mini-trampolines.c).
			 */
			if (context_used && !imt_arg && !array_rank && !delegate_invoke &&
				(!mono_method_is_generic_sharable_full (cmethod, TRUE, FALSE, FALSE) ||
				 !mono_class_generic_sharing_enabled (cmethod->klass)) &&
				(!virtual || MONO_METHOD_IS_FINAL (cmethod) ||
				 !(cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL))) {
				INLINE_FAILURE ("gshared");

				g_assert (cfg->generic_sharing_context && cmethod);
				g_assert (!addr);

				/*
				 * We are compiling a call to a
				 * generic method from shared code,
				 * which means that we have to look up
				 * the method in the rgctx and do an
				 * indirect call.
				 */
				if (fsig->hasthis)
					MONO_EMIT_NEW_CHECK_THIS (cfg, sp [0]->dreg);

				addr = emit_get_rgctx_method (cfg, context_used, cmethod, MONO_RGCTX_INFO_GENERIC_METHOD_CODE);
				ins = (MonoInst*)mono_emit_calli (cfg, fsig, sp, addr, imt_arg, vtable_arg);
				goto call_end;
			}

			/* Indirect calls */
			if (addr) {
				if (call_opcode == CEE_CALL)
					g_assert (context_used);
				else if (call_opcode == CEE_CALLI)
					g_assert (!vtable_arg);
				else
					/* FIXME: what the hell is this??? */
					g_assert (cmethod->flags & METHOD_ATTRIBUTE_FINAL ||
							!(cmethod->flags & METHOD_ATTRIBUTE_FINAL));

				/* Prevent inlining of methods with indirect calls */
				INLINE_FAILURE ("indirect call");

				if (addr->opcode == OP_PCONST || addr->opcode == OP_AOTCONST || addr->opcode == OP_GOT_ENTRY) {
					int info_type;
					gpointer info_data;

					/* 
					 * Instead of emitting an indirect call, emit a direct call
					 * with the contents of the aotconst as the patch info.
					 */
					if (addr->opcode == OP_PCONST || addr->opcode == OP_AOTCONST) {
						info_type = addr->inst_c1;
						info_data = addr->inst_p0;
					} else {
						info_type = addr->inst_right->inst_c1;
						info_data = addr->inst_right->inst_left;
					}
					
					if (info_type == MONO_PATCH_INFO_ICALL_ADDR || info_type == MONO_PATCH_INFO_JIT_ICALL_ADDR) {
						ins = (MonoInst*)mono_emit_abs_call (cfg, info_type, info_data, fsig, sp);
						NULLIFY_INS (addr);
						goto call_end;
					}
				}
				ins = (MonoInst*)mono_emit_calli (cfg, fsig, sp, addr, imt_arg, vtable_arg);
				goto call_end;
			}
	      				
			/* Array methods */
			if (array_rank) {
				MonoInst *addr;

				if (strcmp (cmethod->name, "Set") == 0) { /* array Set */ 
					MonoInst *val = sp [fsig->param_count];

					if (val->type == STACK_OBJ) {
						MonoInst *iargs [2];

						iargs [0] = sp [0];
						iargs [1] = val;
						
						mono_emit_jit_icall (cfg, mono_helper_stelem_ref_check, iargs);
					}
					
					addr = mini_emit_ldelema_ins (cfg, cmethod, sp, ip, TRUE);
					EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, fsig->params [fsig->param_count - 1], addr->dreg, 0, val->dreg);
					if (cfg->gen_write_barriers && val->type == STACK_OBJ && !(val->opcode == OP_PCONST && val->inst_c0 == 0))
						emit_write_barrier (cfg, addr, val);
				} else if (strcmp (cmethod->name, "Get") == 0) { /* array Get */
					addr = mini_emit_ldelema_ins (cfg, cmethod, sp, ip, FALSE);

					EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, fsig->ret, addr->dreg, 0);
				} else if (strcmp (cmethod->name, "Address") == 0) { /* array Address */
					if (!cmethod->klass->element_class->valuetype && !readonly)
						mini_emit_check_array_type (cfg, sp [0], cmethod->klass);
					CHECK_TYPELOAD (cmethod->klass);
					
					readonly = FALSE;
					addr = mini_emit_ldelema_ins (cfg, cmethod, sp, ip, FALSE);
					ins = addr;
				} else {
					g_assert_not_reached ();
				}

				emit_widen = FALSE;
				goto call_end;
			}

			ins = mini_redirect_call (cfg, cmethod, fsig, sp, virtual ? sp [0] : NULL);
			if (ins)
				goto call_end;

			/* Tail prefix / tail call optimization */

			/* FIXME: Enabling TAILC breaks some inlining/stack trace/etc tests */
			/* FIXME: runtime generic context pointer for jumps? */
			/* FIXME: handle this for generic sharing eventually */
			if (cmethod && (ins_flag & MONO_INST_TAILCALL) &&
				!vtable_arg && !cfg->generic_sharing_context && is_supported_tail_call (cfg, method, cmethod, fsig, call_opcode))
				supported_tail_call = TRUE;

			if (supported_tail_call) {
				MonoCallInst *call;

				/* Prevent inlining of methods with tail calls (the call stack would be altered) */
				INLINE_FAILURE ("tail call");

				//printf ("HIT: %s -> %s\n", mono_method_full_name (cfg->method, TRUE), mono_method_full_name (cmethod, TRUE));

				if (ARCH_HAVE_OP_TAIL_CALL) {
					/* Handle tail calls similarly to normal calls */
					tail_call = TRUE;
				} else {
					emit_instrumentation_call (cfg, mono_profiler_method_leave);

					MONO_INST_NEW_CALL (cfg, call, OP_JMP);
					call->tail_call = TRUE;
					call->method = cmethod;
					call->signature = mono_method_signature (cmethod);

					/*
					 * We implement tail calls by storing the actual arguments into the 
					 * argument variables, then emitting a CEE_JMP.
					 */
					for (i = 0; i < n; ++i) {
						/* Prevent argument from being register allocated */
						arg_array [i]->flags |= MONO_INST_VOLATILE;
						EMIT_NEW_ARGSTORE (cfg, ins, i, sp [i]);
					}
					ins = (MonoInst*)call;
					ins->inst_p0 = cmethod;
					ins->inst_p1 = arg_array [0];
					MONO_ADD_INS (bblock, ins);
					link_bblock (cfg, bblock, end_bblock);			
					start_new_bblock = 1;

					// FIXME: Eliminate unreachable epilogs

					/*
					 * OP_TAILCALL has no return value, so skip the CEE_RET if it is
					 * only reachable from this call.
					 */
					GET_BBLOCK (cfg, tblock, ip + 5);
					if (tblock == bblock || tblock->in_count == 0)
						skip_ret = TRUE;
					push_res = FALSE;

					goto call_end;
				}
			}

			/* 
			 * Synchronized wrappers.
			 * Its hard to determine where to replace a method with its synchronized
			 * wrapper without causing an infinite recursion. The current solution is
			 * to add the synchronized wrapper in the trampolines, and to
			 * change the called method to a dummy wrapper, and resolve that wrapper
			 * to the real method in mono_jit_compile_method ().
			 */
			if (cfg->method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED) {
				MonoMethod *orig = mono_marshal_method_from_wrapper (cfg->method);
				if (cmethod == orig || (cmethod->is_inflated && mono_method_get_declaring_generic_method (cmethod) == orig))
					cmethod = mono_marshal_get_synchronized_inner_wrapper (cmethod);
			}

			/* Common call */
			INLINE_FAILURE ("call");
			ins = mono_emit_method_call_full (cfg, cmethod, fsig, tail_call, sp, virtual ? sp [0] : NULL,
											  imt_arg, vtable_arg);

			if (tail_call) {
				link_bblock (cfg, bblock, end_bblock);			
				start_new_bblock = 1;

				// FIXME: Eliminate unreachable epilogs

				/*
				 * OP_TAILCALL has no return value, so skip the CEE_RET if it is
				 * only reachable from this call.
				 */
				GET_BBLOCK (cfg, tblock, ip + 5);
				if (tblock == bblock || tblock->in_count == 0)
					skip_ret = TRUE;
				push_res = FALSE;
			}

			call_end:

			/* End of call, INS should contain the result of the call, if any */

			if (push_res && !MONO_TYPE_IS_VOID (fsig->ret)) {
				g_assert (ins);
				if (emit_widen)
					*sp++ = mono_emit_widen_call_res (cfg, ins, fsig);
				else
					*sp++ = ins;
			}

			if (keep_this_alive) {
				MonoInst *dummy_use;

				/* See mono_emit_method_call_full () */
				EMIT_NEW_DUMMY_USE (cfg, dummy_use, keep_this_alive);
			}

			CHECK_CFG_EXCEPTION;

			ip += 5;
			if (skip_ret) {
				g_assert (*ip == CEE_RET);
				ip += 1;
			}
			ins_flag = 0;
			constrained_call = NULL;
			if (need_seq_point)
				emit_seq_point (cfg, method, ip, FALSE, TRUE);
			break;
		}
		case CEE_RET:
			if (cfg->method != method) {
				/* return from inlined method */
				/* 
				 * If in_count == 0, that means the ret is unreachable due to
				 * being preceeded by a throw. In that case, inline_method () will
				 * handle setting the return value 
				 * (test case: test_0_inline_throw ()).
				 */
				if (return_var && cfg->cbb->in_count) {
					MonoType *ret_type = mono_method_signature (method)->ret;

					MonoInst *store;
					CHECK_STACK (1);
					--sp;

					if ((method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD || method->wrapper_type == MONO_WRAPPER_NONE) && target_type_is_incompatible (cfg, ret_type, *sp))
						UNVERIFIED;

					//g_assert (returnvar != -1);
					EMIT_NEW_TEMPSTORE (cfg, store, return_var->inst_c0, *sp);
					cfg->ret_var_set = TRUE;
				} 
			} else {
				emit_instrumentation_call (cfg, mono_profiler_method_leave);

				if (cfg->lmf_var && cfg->cbb->in_count)
					emit_pop_lmf (cfg);

				if (cfg->ret) {
					MonoType *ret_type = mini_replace_type (mono_method_signature (method)->ret);

					if (seq_points && !sym_seq_points) {
						/* 
						 * Place a seq point here too even through the IL stack is not
						 * empty, so a step over on
						 * call <FOO>
						 * ret
						 * will work correctly.
						 */
						NEW_SEQ_POINT (cfg, ins, ip - header->code, TRUE);
						MONO_ADD_INS (cfg->cbb, ins);
					}

					g_assert (!return_var);
					CHECK_STACK (1);
					--sp;

					if ((method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD || method->wrapper_type == MONO_WRAPPER_NONE) && target_type_is_incompatible (cfg, ret_type, *sp))
						UNVERIFIED;

					if (mini_type_to_stind (cfg, ret_type) == CEE_STOBJ) {
						MonoInst *ret_addr;

						if (!cfg->vret_addr) {
							MonoInst *ins;

							EMIT_NEW_VARSTORE (cfg, ins, cfg->ret, ret_type, (*sp));
						} else {
							EMIT_NEW_RETLOADA (cfg, ret_addr);

							EMIT_NEW_STORE_MEMBASE (cfg, ins, OP_STOREV_MEMBASE, ret_addr->dreg, 0, (*sp)->dreg);
							ins->klass = mono_class_from_mono_type (ret_type);
						}
					} else {
#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
						if (COMPILE_SOFT_FLOAT (cfg) && !ret_type->byref && ret_type->type == MONO_TYPE_R4) {
							MonoInst *iargs [1];
							MonoInst *conv;

							iargs [0] = *sp;
							conv = mono_emit_jit_icall (cfg, mono_fload_r4_arg, iargs);
							mono_arch_emit_setret (cfg, method, conv);
						} else {
							mono_arch_emit_setret (cfg, method, *sp);
						}
#else
						mono_arch_emit_setret (cfg, method, *sp);
#endif
					}
				}
			}
			if (sp != stack_start)
				UNVERIFIED;
			MONO_INST_NEW (cfg, ins, OP_BR);
			ip++;
			ins->inst_target_bb = end_bblock;
			MONO_ADD_INS (bblock, ins);
			link_bblock (cfg, bblock, end_bblock);
			start_new_bblock = 1;
			break;
		case CEE_BR_S:
			CHECK_OPSIZE (2);
			MONO_INST_NEW (cfg, ins, OP_BR);
			ip++;
			target = ip + 1 + (signed char)(*ip);
			++ip;
			GET_BBLOCK (cfg, tblock, target);
			link_bblock (cfg, bblock, tblock);
			ins->inst_target_bb = tblock;
			if (sp != stack_start) {
				handle_stack_args (cfg, stack_start, sp - stack_start);
				sp = stack_start;
				CHECK_UNVERIFIABLE (cfg);
			}
			MONO_ADD_INS (bblock, ins);
			start_new_bblock = 1;
			inline_costs += BRANCH_COST;
			break;
		case CEE_BEQ_S:
		case CEE_BGE_S:
		case CEE_BGT_S:
		case CEE_BLE_S:
		case CEE_BLT_S:
		case CEE_BNE_UN_S:
		case CEE_BGE_UN_S:
		case CEE_BGT_UN_S:
		case CEE_BLE_UN_S:
		case CEE_BLT_UN_S:
			CHECK_OPSIZE (2);
			CHECK_STACK (2);
			MONO_INST_NEW (cfg, ins, *ip + BIG_BRANCH_OFFSET);
			ip++;
			target = ip + 1 + *(signed char*)ip;
			ip++;

			ADD_BINCOND (NULL);

			sp = stack_start;
			inline_costs += BRANCH_COST;
			break;
		case CEE_BR:
			CHECK_OPSIZE (5);
			MONO_INST_NEW (cfg, ins, OP_BR);
			ip++;

			target = ip + 4 + (gint32)read32(ip);
			ip += 4;
			GET_BBLOCK (cfg, tblock, target);
			link_bblock (cfg, bblock, tblock);
			ins->inst_target_bb = tblock;
			if (sp != stack_start) {
				handle_stack_args (cfg, stack_start, sp - stack_start);
				sp = stack_start;
				CHECK_UNVERIFIABLE (cfg);
			}

			MONO_ADD_INS (bblock, ins);

			start_new_bblock = 1;
			inline_costs += BRANCH_COST;
			break;
		case CEE_BRFALSE_S:
		case CEE_BRTRUE_S:
		case CEE_BRFALSE:
		case CEE_BRTRUE: {
			MonoInst *cmp;
			gboolean is_short = ((*ip) == CEE_BRFALSE_S) || ((*ip) == CEE_BRTRUE_S);
			gboolean is_true = ((*ip) == CEE_BRTRUE_S) || ((*ip) == CEE_BRTRUE);
			guint32 opsize = is_short ? 1 : 4;

			CHECK_OPSIZE (opsize);
			CHECK_STACK (1);
			if (sp [-1]->type == STACK_VTYPE || sp [-1]->type == STACK_R8)
				UNVERIFIED;
			ip ++;
			target = ip + opsize + (is_short ? *(signed char*)ip : (gint32)read32(ip));
			ip += opsize;

			sp--;

			GET_BBLOCK (cfg, tblock, target);
			link_bblock (cfg, bblock, tblock);
			GET_BBLOCK (cfg, tblock, ip);
			link_bblock (cfg, bblock, tblock);

			if (sp != stack_start) {
				handle_stack_args (cfg, stack_start, sp - stack_start);
				CHECK_UNVERIFIABLE (cfg);
			}

			MONO_INST_NEW(cfg, cmp, OP_ICOMPARE_IMM);
			cmp->sreg1 = sp [0]->dreg;
			type_from_op (cmp, sp [0], NULL);
			CHECK_TYPE (cmp);

#if SIZEOF_REGISTER == 4
			if (cmp->opcode == OP_LCOMPARE_IMM) {
				/* Convert it to OP_LCOMPARE */
				MONO_INST_NEW (cfg, ins, OP_I8CONST);
				ins->type = STACK_I8;
				ins->dreg = alloc_dreg (cfg, STACK_I8);
				ins->inst_l = 0;
				MONO_ADD_INS (bblock, ins);
				cmp->opcode = OP_LCOMPARE;
				cmp->sreg2 = ins->dreg;
			}
#endif
			MONO_ADD_INS (bblock, cmp);

			MONO_INST_NEW (cfg, ins, is_true ? CEE_BNE_UN : CEE_BEQ);
			type_from_op (ins, sp [0], NULL);
			MONO_ADD_INS (bblock, ins);
			ins->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof(gpointer)*2);
			GET_BBLOCK (cfg, tblock, target);
			ins->inst_true_bb = tblock;
			GET_BBLOCK (cfg, tblock, ip);
			ins->inst_false_bb = tblock;
			start_new_bblock = 2;

			sp = stack_start;
			inline_costs += BRANCH_COST;
			break;
		}
		case CEE_BEQ:
		case CEE_BGE:
		case CEE_BGT:
		case CEE_BLE:
		case CEE_BLT:
		case CEE_BNE_UN:
		case CEE_BGE_UN:
		case CEE_BGT_UN:
		case CEE_BLE_UN:
		case CEE_BLT_UN:
			CHECK_OPSIZE (5);
			CHECK_STACK (2);
			MONO_INST_NEW (cfg, ins, *ip);
			ip++;
			target = ip + 4 + (gint32)read32(ip);
			ip += 4;

			ADD_BINCOND (NULL);

			sp = stack_start;
			inline_costs += BRANCH_COST;
			break;
		case CEE_SWITCH: {
			MonoInst *src1;
			MonoBasicBlock **targets;
			MonoBasicBlock *default_bblock;
			MonoJumpInfoBBTable *table;
			int offset_reg = alloc_preg (cfg);
			int target_reg = alloc_preg (cfg);
			int table_reg = alloc_preg (cfg);
			int sum_reg = alloc_preg (cfg);
			gboolean use_op_switch;

			CHECK_OPSIZE (5);
			CHECK_STACK (1);
			n = read32 (ip + 1);
			--sp;
			src1 = sp [0];
			if ((src1->type != STACK_I4) && (src1->type != STACK_PTR)) 
				UNVERIFIED;

			ip += 5;
			CHECK_OPSIZE (n * sizeof (guint32));
			target = ip + n * sizeof (guint32);

			GET_BBLOCK (cfg, default_bblock, target);
			default_bblock->flags |= BB_INDIRECT_JUMP_TARGET;

			targets = mono_mempool_alloc (cfg->mempool, sizeof (MonoBasicBlock*) * n);
			for (i = 0; i < n; ++i) {
				GET_BBLOCK (cfg, tblock, target + (gint32)read32(ip));
				targets [i] = tblock;
				targets [i]->flags |= BB_INDIRECT_JUMP_TARGET;
				ip += 4;
			}

			if (sp != stack_start) {
				/* 
				 * Link the current bb with the targets as well, so handle_stack_args
				 * will set their in_stack correctly.
				 */
				link_bblock (cfg, bblock, default_bblock);
				for (i = 0; i < n; ++i)
					link_bblock (cfg, bblock, targets [i]);

				handle_stack_args (cfg, stack_start, sp - stack_start);
				sp = stack_start;
				CHECK_UNVERIFIABLE (cfg);
			}

			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ICOMPARE_IMM, -1, src1->dreg, n);
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBGE_UN, default_bblock);
			bblock = cfg->cbb;

			for (i = 0; i < n; ++i)
				link_bblock (cfg, bblock, targets [i]);

			table = mono_mempool_alloc (cfg->mempool, sizeof (MonoJumpInfoBBTable));
			table->table = targets;
			table->table_size = n;

			use_op_switch = FALSE;
#ifdef TARGET_ARM
			/* ARM implements SWITCH statements differently */
			/* FIXME: Make it use the generic implementation */
			if (!cfg->compile_aot)
				use_op_switch = TRUE;
#endif

			if (COMPILE_LLVM (cfg))
				use_op_switch = TRUE;

			cfg->cbb->has_jump_table = 1;

			if (use_op_switch) {
				MONO_INST_NEW (cfg, ins, OP_SWITCH);
				ins->sreg1 = src1->dreg;
				ins->inst_p0 = table;
				ins->inst_many_bb = targets;
				ins->klass = GUINT_TO_POINTER (n);
				MONO_ADD_INS (cfg->cbb, ins);
			} else {
				if (sizeof (gpointer) == 8)
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, offset_reg, src1->dreg, 3);
				else
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, offset_reg, src1->dreg, 2);

#if SIZEOF_REGISTER == 8
				/* The upper word might not be zero, and we add it to a 64 bit address later */
				MONO_EMIT_NEW_UNALU (cfg, OP_ZEXT_I4, offset_reg, offset_reg);
#endif

				if (cfg->compile_aot) {
					MONO_EMIT_NEW_AOTCONST (cfg, table_reg, table, MONO_PATCH_INFO_SWITCH);
				} else {
					MONO_INST_NEW (cfg, ins, OP_JUMP_TABLE);
					ins->inst_c1 = MONO_PATCH_INFO_SWITCH;
					ins->inst_p0 = table;
					ins->dreg = table_reg;
					MONO_ADD_INS (cfg->cbb, ins);
				}

				/* FIXME: Use load_memindex */
				MONO_EMIT_NEW_BIALU (cfg, OP_PADD, sum_reg, table_reg, offset_reg);
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, target_reg, sum_reg, 0);
				MONO_EMIT_NEW_UNALU (cfg, OP_BR_REG, -1, target_reg);
			}
			start_new_bblock = 1;
			inline_costs += (BRANCH_COST * 2);
			break;
		}
		case CEE_LDIND_I1:
		case CEE_LDIND_U1:
		case CEE_LDIND_I2:
		case CEE_LDIND_U2:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
		case CEE_LDIND_I8:
		case CEE_LDIND_I:
		case CEE_LDIND_R4:
		case CEE_LDIND_R8:
		case CEE_LDIND_REF:
			CHECK_STACK (1);
			--sp;

			switch (*ip) {
			case CEE_LDIND_R4:
			case CEE_LDIND_R8:
				dreg = alloc_freg (cfg);
				break;
			case CEE_LDIND_I8:
				dreg = alloc_lreg (cfg);
				break;
			case CEE_LDIND_REF:
				dreg = alloc_ireg_ref (cfg);
				break;
			default:
				dreg = alloc_preg (cfg);
			}

			NEW_LOAD_MEMBASE (cfg, ins, ldind_to_load_membase (*ip), dreg, sp [0]->dreg, 0);
			ins->type = ldind_type [*ip - CEE_LDIND_I1];
			ins->flags |= ins_flag;
			MONO_ADD_INS (bblock, ins);
			*sp++ = ins;
			if (ins_flag & MONO_INST_VOLATILE) {
				/* Volatile loads have acquire semantics, see 12.6.7 in Ecma 335 */
				/* FIXME it's questionable if acquire semantics require full barrier or just LoadLoad*/
				emit_memory_barrier (cfg, FullBarrier);
			}
			ins_flag = 0;
			++ip;
			break;
		case CEE_STIND_REF:
		case CEE_STIND_I1:
		case CEE_STIND_I2:
		case CEE_STIND_I4:
		case CEE_STIND_I8:
		case CEE_STIND_R4:
		case CEE_STIND_R8:
		case CEE_STIND_I:
			CHECK_STACK (2);
			sp -= 2;

			if (ins_flag & MONO_INST_VOLATILE) {
				/* Volatile stores have release semantics, see 12.6.7 in Ecma 335 */
				/* FIXME it's questionable if acquire semantics require full barrier or just LoadLoad*/
				emit_memory_barrier (cfg, FullBarrier);
			}

			NEW_STORE_MEMBASE (cfg, ins, stind_to_store_membase (*ip), sp [0]->dreg, 0, sp [1]->dreg);
			ins->flags |= ins_flag;
			ins_flag = 0;

			MONO_ADD_INS (bblock, ins);

			if (cfg->gen_write_barriers && *ip == CEE_STIND_REF && method->wrapper_type != MONO_WRAPPER_WRITE_BARRIER && !((sp [1]->opcode == OP_PCONST) && (sp [1]->inst_p0 == 0)))
				emit_write_barrier (cfg, sp [0], sp [1]);

			inline_costs += 1;
			++ip;
			break;

		case CEE_MUL:
			CHECK_STACK (2);

			MONO_INST_NEW (cfg, ins, (*ip));
			sp -= 2;
			ins->sreg1 = sp [0]->dreg;
			ins->sreg2 = sp [1]->dreg;
			type_from_op (ins, sp [0], sp [1]);
			CHECK_TYPE (ins);
			ins->dreg = alloc_dreg ((cfg), (ins)->type);

			/* Use the immediate opcodes if possible */
			if ((sp [1]->opcode == OP_ICONST) && mono_arch_is_inst_imm (sp [1]->inst_c0)) {
				int imm_opcode = mono_op_to_op_imm_noemul (ins->opcode);
				if (imm_opcode != -1) {
					ins->opcode = imm_opcode;
					ins->inst_p1 = (gpointer)(gssize)(sp [1]->inst_c0);
					ins->sreg2 = -1;

					NULLIFY_INS (sp [1]);
				}
			}

			MONO_ADD_INS ((cfg)->cbb, (ins));

			*sp++ = mono_decompose_opcode (cfg, ins);
			ip++;
			break;
		case CEE_ADD:
		case CEE_SUB:
		case CEE_DIV:
		case CEE_DIV_UN:
		case CEE_REM:
		case CEE_REM_UN:
		case CEE_AND:
		case CEE_OR:
		case CEE_XOR:
		case CEE_SHL:
		case CEE_SHR:
		case CEE_SHR_UN:
			CHECK_STACK (2);

			MONO_INST_NEW (cfg, ins, (*ip));
			sp -= 2;
			ins->sreg1 = sp [0]->dreg;
			ins->sreg2 = sp [1]->dreg;
			type_from_op (ins, sp [0], sp [1]);
			CHECK_TYPE (ins);
			ADD_WIDEN_OP (ins, sp [0], sp [1]);
			ins->dreg = alloc_dreg ((cfg), (ins)->type);

			/* FIXME: Pass opcode to is_inst_imm */

			/* Use the immediate opcodes if possible */
			if (((sp [1]->opcode == OP_ICONST) || (sp [1]->opcode == OP_I8CONST)) && mono_arch_is_inst_imm (sp [1]->opcode == OP_ICONST ? sp [1]->inst_c0 : sp [1]->inst_l)) {
				int imm_opcode;

				imm_opcode = mono_op_to_op_imm_noemul (ins->opcode);
#if defined(MONO_ARCH_EMULATE_MUL_DIV) || defined(MONO_ARCH_EMULATE_DIV)
				/* Keep emulated opcodes which are optimized away later */
				if ((ins->opcode == OP_IREM_UN || ins->opcode == OP_IDIV_UN_IMM) && (cfg->opt & (MONO_OPT_CONSPROP | MONO_OPT_COPYPROP)) && sp [1]->opcode == OP_ICONST && mono_is_power_of_two (sp [1]->inst_c0) >= 0) {
					imm_opcode = mono_op_to_op_imm (ins->opcode);
				}
#endif
				if (imm_opcode != -1) {
					ins->opcode = imm_opcode;
					if (sp [1]->opcode == OP_I8CONST) {
#if SIZEOF_REGISTER == 8
						ins->inst_imm = sp [1]->inst_l;
#else
						ins->inst_ls_word = sp [1]->inst_ls_word;
						ins->inst_ms_word = sp [1]->inst_ms_word;
#endif
					}
					else
						ins->inst_imm = (gssize)(sp [1]->inst_c0);
					ins->sreg2 = -1;

					/* Might be followed by an instruction added by ADD_WIDEN_OP */
					if (sp [1]->next == NULL)
						NULLIFY_INS (sp [1]);
				}
			}
			MONO_ADD_INS ((cfg)->cbb, (ins));

			*sp++ = mono_decompose_opcode (cfg, ins);
			ip++;
			break;
		case CEE_NEG:
		case CEE_NOT:
		case CEE_CONV_I1:
		case CEE_CONV_I2:
		case CEE_CONV_I4:
		case CEE_CONV_R4:
		case CEE_CONV_R8:
		case CEE_CONV_U4:
		case CEE_CONV_I8:
		case CEE_CONV_U8:
		case CEE_CONV_OVF_I8:
		case CEE_CONV_OVF_U8:
		case CEE_CONV_R_UN:
			CHECK_STACK (1);

			/* Special case this earlier so we have long constants in the IR */
			if ((((*ip) == CEE_CONV_I8) || ((*ip) == CEE_CONV_U8)) && (sp [-1]->opcode == OP_ICONST)) {
				int data = sp [-1]->inst_c0;
				sp [-1]->opcode = OP_I8CONST;
				sp [-1]->type = STACK_I8;
#if SIZEOF_REGISTER == 8
				if ((*ip) == CEE_CONV_U8)
					sp [-1]->inst_c0 = (guint32)data;
				else
					sp [-1]->inst_c0 = data;
#else
				sp [-1]->inst_ls_word = data;
				if ((*ip) == CEE_CONV_U8)
					sp [-1]->inst_ms_word = 0;
				else
					sp [-1]->inst_ms_word = (data < 0) ? -1 : 0;
#endif
				sp [-1]->dreg = alloc_dreg (cfg, STACK_I8);
			}
			else {
				ADD_UNOP (*ip);
			}
			ip++;
			break;
		case CEE_CONV_OVF_I4:
		case CEE_CONV_OVF_I1:
		case CEE_CONV_OVF_I2:
		case CEE_CONV_OVF_I:
		case CEE_CONV_OVF_U:
			CHECK_STACK (1);

			if (sp [-1]->type == STACK_R8) {
				ADD_UNOP (CEE_CONV_OVF_I8);
				ADD_UNOP (*ip);
			} else {
				ADD_UNOP (*ip);
			}
			ip++;
			break;
		case CEE_CONV_OVF_U1:
		case CEE_CONV_OVF_U2:
		case CEE_CONV_OVF_U4:
			CHECK_STACK (1);

			if (sp [-1]->type == STACK_R8) {
				ADD_UNOP (CEE_CONV_OVF_U8);
				ADD_UNOP (*ip);
			} else {
				ADD_UNOP (*ip);
			}
			ip++;
			break;
		case CEE_CONV_OVF_I1_UN:
		case CEE_CONV_OVF_I2_UN:
		case CEE_CONV_OVF_I4_UN:
		case CEE_CONV_OVF_I8_UN:
		case CEE_CONV_OVF_U1_UN:
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_U4_UN:
		case CEE_CONV_OVF_U8_UN:
		case CEE_CONV_OVF_I_UN:
		case CEE_CONV_OVF_U_UN:
		case CEE_CONV_U2:
		case CEE_CONV_U1:
		case CEE_CONV_I:
		case CEE_CONV_U:
			CHECK_STACK (1);
			ADD_UNOP (*ip);
			CHECK_CFG_EXCEPTION;
			ip++;
			break;
		case CEE_ADD_OVF:
		case CEE_ADD_OVF_UN:
		case CEE_MUL_OVF:
		case CEE_MUL_OVF_UN:
		case CEE_SUB_OVF:
		case CEE_SUB_OVF_UN:
			CHECK_STACK (2);
			ADD_BINOP (*ip);
			ip++;
			break;
		case CEE_CPOBJ:
			GSHAREDVT_FAILURE (*ip);
			CHECK_OPSIZE (5);
			CHECK_STACK (2);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			sp -= 2;
			if (generic_class_is_reference_type (cfg, klass)) {
				MonoInst *store, *load;
				int dreg = alloc_ireg_ref (cfg);

				NEW_LOAD_MEMBASE (cfg, load, OP_LOAD_MEMBASE, dreg, sp [1]->dreg, 0);
				load->flags |= ins_flag;
				MONO_ADD_INS (cfg->cbb, load);

				NEW_STORE_MEMBASE (cfg, store, OP_STORE_MEMBASE_REG, sp [0]->dreg, 0, dreg);
				store->flags |= ins_flag;
				MONO_ADD_INS (cfg->cbb, store);

				if (cfg->gen_write_barriers && cfg->method->wrapper_type != MONO_WRAPPER_WRITE_BARRIER)
					emit_write_barrier (cfg, sp [0], sp [1]);
			} else {
				mini_emit_stobj (cfg, sp [0], sp [1], klass, FALSE);
			}
			ins_flag = 0;
			ip += 5;
			break;
		case CEE_LDOBJ: {
			int loc_index = -1;
			int stloc_len = 0;

			CHECK_OPSIZE (5);
			CHECK_STACK (1);
			--sp;
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			/* Optimize the common ldobj+stloc combination */
			switch (ip [5]) {
			case CEE_STLOC_S:
				loc_index = ip [6];
				stloc_len = 2;
				break;
			case CEE_STLOC_0:
			case CEE_STLOC_1:
			case CEE_STLOC_2:
			case CEE_STLOC_3:
				loc_index = ip [5] - CEE_STLOC_0;
				stloc_len = 1;
				break;
			default:
				break;
			}

			if ((loc_index != -1) && ip_in_bb (cfg, bblock, ip + 5)) {
				CHECK_LOCAL (loc_index);

				EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, sp [0]->dreg, 0);
				ins->dreg = cfg->locals [loc_index]->dreg;
				ins->flags |= ins_flag;
				ip += 5;
				ip += stloc_len;
				if (ins_flag & MONO_INST_VOLATILE) {
					/* Volatile loads have acquire semantics, see 12.6.7 in Ecma 335 */
					/* FIXME it's questionable if acquire semantics require full barrier or just LoadLoad*/
					emit_memory_barrier (cfg, FullBarrier);
				}
				ins_flag = 0;
				break;
			}

			/* Optimize the ldobj+stobj combination */
			/* The reference case ends up being a load+store anyway */
			/* Skip this if the operation is volatile. */
			if (((ip [5] == CEE_STOBJ) && ip_in_bb (cfg, bblock, ip + 5) && read32 (ip + 6) == token) && !generic_class_is_reference_type (cfg, klass) && !(ins_flag & MONO_INST_VOLATILE)) {
				CHECK_STACK (1);

				sp --;

				mini_emit_stobj (cfg, sp [0], sp [1], klass, FALSE);

				ip += 5 + 5;
				ins_flag = 0;
				break;
			}

			EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, sp [0]->dreg, 0);
			ins->flags |= ins_flag;
			*sp++ = ins;

			if (ins_flag & MONO_INST_VOLATILE) {
				/* Volatile loads have acquire semantics, see 12.6.7 in Ecma 335 */
				/* FIXME it's questionable if acquire semantics require full barrier or just LoadLoad*/
				emit_memory_barrier (cfg, FullBarrier);
			}

			ip += 5;
			ins_flag = 0;
			inline_costs += 1;
			break;
		}
		case CEE_LDSTR:
			CHECK_STACK_OVF (1);
			CHECK_OPSIZE (5);
			n = read32 (ip + 1);

			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD) {
				EMIT_NEW_PCONST (cfg, ins, mono_method_get_wrapper_data (method, n));
				ins->type = STACK_OBJ;
				*sp = ins;
			}
			else if (method->wrapper_type != MONO_WRAPPER_NONE) {
				MonoInst *iargs [1];

				EMIT_NEW_PCONST (cfg, iargs [0], mono_method_get_wrapper_data (method, n));				
				*sp = mono_emit_jit_icall (cfg, mono_string_new_wrapper, iargs);
			} else {
				if (cfg->opt & MONO_OPT_SHARED) {
					MonoInst *iargs [3];

					if (cfg->compile_aot) {
						cfg->ldstr_list = g_list_prepend (cfg->ldstr_list, GINT_TO_POINTER (n));
					}
					EMIT_NEW_DOMAINCONST (cfg, iargs [0]);
					EMIT_NEW_IMAGECONST (cfg, iargs [1], image);
					EMIT_NEW_ICONST (cfg, iargs [2], mono_metadata_token_index (n));
					*sp = mono_emit_jit_icall (cfg, mono_ldstr, iargs);
					mono_ldstr (cfg->domain, image, mono_metadata_token_index (n));
				} else {
					if (bblock->out_of_line) {
						MonoInst *iargs [2];

						if (image == mono_defaults.corlib) {
							/* 
							 * Avoid relocations in AOT and save some space by using a 
							 * version of helper_ldstr specialized to mscorlib.
							 */
							EMIT_NEW_ICONST (cfg, iargs [0], mono_metadata_token_index (n));
							*sp = mono_emit_jit_icall (cfg, mono_helper_ldstr_mscorlib, iargs);
						} else {
							/* Avoid creating the string object */
							EMIT_NEW_IMAGECONST (cfg, iargs [0], image);
							EMIT_NEW_ICONST (cfg, iargs [1], mono_metadata_token_index (n));
							*sp = mono_emit_jit_icall (cfg, mono_helper_ldstr, iargs);
						}
					} 
					else
					if (cfg->compile_aot) {
						NEW_LDSTRCONST (cfg, ins, image, n);
						*sp = ins;
						MONO_ADD_INS (bblock, ins);
					} 
					else {
						NEW_PCONST (cfg, ins, NULL);
						ins->type = STACK_OBJ;
						ins->inst_p0 = mono_ldstr (cfg->domain, image, mono_metadata_token_index (n));
						if (!ins->inst_p0)
							OUT_OF_MEMORY_FAILURE;

						*sp = ins;
						MONO_ADD_INS (bblock, ins);
					}
				}
			}

			sp++;
			ip += 5;
			break;
		case CEE_NEWOBJ: {
			MonoInst *iargs [2];
			MonoMethodSignature *fsig;
			MonoInst this_ins;
			MonoInst *alloc;
			MonoInst *vtable_arg = NULL;

			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			cmethod = mini_get_method (cfg, method, token, NULL, generic_context);
			if (!cmethod || mono_loader_get_last_error ())
				LOAD_ERROR;
			fsig = mono_method_get_signature_checked (cmethod, image, token, NULL, &cfg->error);
			CHECK_CFG_ERROR;

			mono_save_token_info (cfg, image, token, cmethod);

			if (!mono_class_init (cmethod->klass))
				TYPE_LOAD_ERROR (cmethod->klass);

			context_used = mini_method_check_context_used (cfg, cmethod);

			if (mono_security_cas_enabled ()) {
				if (check_linkdemand (cfg, method, cmethod))
					INLINE_FAILURE ("linkdemand");
				CHECK_CFG_EXCEPTION;
			} else if (mono_security_core_clr_enabled ()) {
				ensure_method_is_allowed_to_call_method (cfg, method, cmethod, bblock, ip);
 			}

			if (cfg->generic_sharing_context && cmethod && cmethod->klass != method->klass && cmethod->klass->generic_class && mono_method_is_generic_sharable (cmethod, TRUE) && mono_class_needs_cctor_run (cmethod->klass, method)) {
				emit_generic_class_init (cfg, cmethod->klass);
				CHECK_TYPELOAD (cmethod->klass);
			}

			/*
			if (cfg->gsharedvt) {
				if (mini_is_gsharedvt_variable_signature (sig))
					GSHAREDVT_FAILURE (*ip);
			}
			*/

			n = fsig->param_count;
			CHECK_STACK (n);

			/* 
			 * Generate smaller code for the common newobj <exception> instruction in
			 * argument checking code.
			 */
			if (bblock->out_of_line && cmethod->klass->image == mono_defaults.corlib &&
				is_exception_class (cmethod->klass) && n <= 2 &&
				((n < 1) || (!fsig->params [0]->byref && fsig->params [0]->type == MONO_TYPE_STRING)) && 
				((n < 2) || (!fsig->params [1]->byref && fsig->params [1]->type == MONO_TYPE_STRING))) {
				MonoInst *iargs [3];

				sp -= n;

				EMIT_NEW_ICONST (cfg, iargs [0], cmethod->klass->type_token);
				switch (n) {
				case 0:
					*sp ++ = mono_emit_jit_icall (cfg, mono_create_corlib_exception_0, iargs);
					break;
				case 1:
					iargs [1] = sp [0];
					*sp ++ = mono_emit_jit_icall (cfg, mono_create_corlib_exception_1, iargs);
					break;
				case 2:
					iargs [1] = sp [0];
					iargs [2] = sp [1];
					*sp ++ = mono_emit_jit_icall (cfg, mono_create_corlib_exception_2, iargs);
					break;
				default:
					g_assert_not_reached ();
				}

				ip += 5;
				inline_costs += 5;
				break;
			}

			/* move the args to allow room for 'this' in the first position */
			while (n--) {
				--sp;
				sp [1] = sp [0];
			}

			/* check_call_signature () requires sp[0] to be set */
			this_ins.type = STACK_OBJ;
			sp [0] = &this_ins;
			if (check_call_signature (cfg, fsig, sp))
				UNVERIFIED;

			iargs [0] = NULL;

			if (mini_class_is_system_array (cmethod->klass)) {
				*sp = emit_get_rgctx_method (cfg, context_used,
											 cmethod, MONO_RGCTX_INFO_METHOD);

				/* Avoid varargs in the common case */
				if (fsig->param_count == 1)
					alloc = mono_emit_jit_icall (cfg, mono_array_new_1, sp);
				else if (fsig->param_count == 2)
					alloc = mono_emit_jit_icall (cfg, mono_array_new_2, sp);
				else if (fsig->param_count == 3)
					alloc = mono_emit_jit_icall (cfg, mono_array_new_3, sp);
				else if (fsig->param_count == 4)
					alloc = mono_emit_jit_icall (cfg, mono_array_new_4, sp);
				else
					alloc = handle_array_new (cfg, fsig->param_count, sp, ip);
			} else if (cmethod->string_ctor) {
				g_assert (!context_used);
				g_assert (!vtable_arg);
				/* we simply pass a null pointer */
				EMIT_NEW_PCONST (cfg, *sp, NULL); 
				/* now call the string ctor */
				alloc = mono_emit_method_call_full (cfg, cmethod, fsig, FALSE, sp, NULL, NULL, NULL);
			} else {
				if (cmethod->klass->valuetype) {
					iargs [0] = mono_compile_create_var (cfg, &cmethod->klass->byval_arg, OP_LOCAL);
					emit_init_rvar (cfg, iargs [0]->dreg, &cmethod->klass->byval_arg);
					EMIT_NEW_TEMPLOADA (cfg, *sp, iargs [0]->inst_c0);

					alloc = NULL;

					/* 
					 * The code generated by mini_emit_virtual_call () expects
					 * iargs [0] to be a boxed instance, but luckily the vcall
					 * will be transformed into a normal call there.
					 */
				} else if (context_used) {
					alloc = handle_alloc (cfg, cmethod->klass, FALSE, context_used);
					*sp = alloc;
				} else {
					MonoVTable *vtable = NULL;

					if (!cfg->compile_aot)
						vtable = mono_class_vtable (cfg->domain, cmethod->klass);
					CHECK_TYPELOAD (cmethod->klass);

					/*
					 * TypeInitializationExceptions thrown from the mono_runtime_class_init
					 * call in mono_jit_runtime_invoke () can abort the finalizer thread.
					 * As a workaround, we call class cctors before allocating objects.
					 */
					if (mini_field_access_needs_cctor_run (cfg, method, cmethod->klass, vtable) && !(g_slist_find (class_inits, cmethod->klass))) {
						mono_emit_abs_call (cfg, MONO_PATCH_INFO_CLASS_INIT, cmethod->klass, helper_sig_class_init_trampoline, NULL);
						if (cfg->verbose_level > 2)
							printf ("class %s.%s needs init call for ctor\n", cmethod->klass->name_space, cmethod->klass->name);
						class_inits = g_slist_prepend (class_inits, cmethod->klass);
					}

					alloc = handle_alloc (cfg, cmethod->klass, FALSE, 0);
					*sp = alloc;
				}
				CHECK_CFG_EXCEPTION; /*for handle_alloc*/

				if (alloc)
					MONO_EMIT_NEW_UNALU (cfg, OP_NOT_NULL, -1, alloc->dreg);

				/* Now call the actual ctor */
				handle_ctor_call (cfg, cmethod, fsig, context_used, sp, ip, &bblock, &inline_costs);
				CHECK_CFG_EXCEPTION;
			}

			if (alloc == NULL) {
				/* Valuetype */
				EMIT_NEW_TEMPLOAD (cfg, ins, iargs [0]->inst_c0);
				type_to_eval_stack_type (cfg, &ins->klass->byval_arg, ins);
				*sp++= ins;
			} else {
				*sp++ = alloc;
			}
			
			ip += 5;
			inline_costs += 5;
			break;
		}
		case CEE_CASTCLASS:
			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;

			ins = handle_castclass (cfg, klass, *sp, ip, &bblock, &inline_costs);
			CHECK_CFG_EXCEPTION;

			*sp ++ = ins;
			ip += 5;
			break;
		case CEE_ISINST: {
			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;
 
			context_used = mini_class_check_context_used (cfg, klass);

			if (!context_used && mini_class_has_reference_variant_generic_argument (cfg, klass, context_used)) {
				MonoMethod *mono_isinst = mono_marshal_get_isinst_with_cache ();
				MonoInst *args [3];

				/* obj */
				args [0] = *sp;

				/* klass */
				EMIT_NEW_CLASSCONST (cfg, args [1], klass);

				/* inline cache*/
				if (cfg->compile_aot)
					EMIT_NEW_AOTCONST (cfg, args [2], MONO_PATCH_INFO_CASTCLASS_CACHE, NULL);
				else
					EMIT_NEW_PCONST (cfg, args [2], mono_domain_alloc0 (cfg->domain, sizeof (gpointer)));

				*sp++ = mono_emit_method_call (cfg, mono_isinst, args, NULL);
				ip += 5;
				inline_costs += 2;
			} else if (!context_used && (mono_class_is_marshalbyref (klass) || klass->flags & TYPE_ATTRIBUTE_INTERFACE)) {
				MonoMethod *mono_isinst;
				MonoInst *iargs [1];
				int costs;

				mono_isinst = mono_marshal_get_isinst (klass); 
				iargs [0] = sp [0];

				costs = inline_method (cfg, mono_isinst, mono_method_signature (mono_isinst), 
									   iargs, ip, cfg->real_offset, TRUE, &bblock);
				CHECK_CFG_EXCEPTION;
				g_assert (costs > 0);
				
				ip += 5;
				cfg->real_offset += 5;

				*sp++= iargs [0];

				inline_costs += costs;
			}
			else {
				ins = handle_isinst (cfg, klass, *sp, context_used);
				CHECK_CFG_EXCEPTION;
				bblock = cfg->cbb;
				*sp ++ = ins;
				ip += 5;
			}
			break;
		}
		case CEE_UNBOX_ANY: {
			MonoInst *res, *addr;

			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
 
			mono_save_token_info (cfg, image, token, klass);

			context_used = mini_class_check_context_used (cfg, klass);

			if (mini_is_gsharedvt_klass (cfg, klass)) {
				res = handle_unbox_gsharedvt (cfg, klass, *sp, &bblock);
				inline_costs += 2;
			} else if (generic_class_is_reference_type (cfg, klass)) {
				res = handle_castclass (cfg, klass, *sp, ip, &bblock, &inline_costs);
				CHECK_CFG_EXCEPTION;
			} else if (mono_class_is_nullable (klass)) {
				res = handle_unbox_nullable (cfg, *sp, klass, context_used);
			} else {
				addr = handle_unbox (cfg, klass, sp, context_used);
				/* LDOBJ */
				EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, addr->dreg, 0);
				res = ins;
				inline_costs += 2;
			}

			*sp ++ = res;
			ip += 5;
			break;
		}
		case CEE_BOX: {
			MonoInst *val;

			CHECK_STACK (1);
			--sp;
			val = *sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			mono_save_token_info (cfg, image, token, klass);

			context_used = mini_class_check_context_used (cfg, klass);

			if (generic_class_is_reference_type (cfg, klass)) {
				*sp++ = val;
				ip += 5;
				break;
			}

			if (klass == mono_defaults.void_class)
				UNVERIFIED;
			if (target_type_is_incompatible (cfg, &klass->byval_arg, *sp))
				UNVERIFIED;
			/* frequent check in generic code: box (struct), brtrue */

			// FIXME: LLVM can't handle the inconsistent bb linking
			if (!mono_class_is_nullable (klass) &&
				ip + 5 < end && ip_in_bb (cfg, bblock, ip + 5) &&
				(ip [5] == CEE_BRTRUE || 
				 ip [5] == CEE_BRTRUE_S ||
				 ip [5] == CEE_BRFALSE ||
				 ip [5] == CEE_BRFALSE_S)) {
				gboolean is_true = ip [5] == CEE_BRTRUE || ip [5] == CEE_BRTRUE_S;
				int dreg;
				MonoBasicBlock *true_bb, *false_bb;

				ip += 5;

				if (cfg->verbose_level > 3) {
					printf ("converting (in B%d: stack: %d) %s", bblock->block_num, (int)(sp - stack_start), mono_disasm_code_one (NULL, method, ip, NULL));
					printf ("<box+brtrue opt>\n");
				}

				switch (*ip) {
				case CEE_BRTRUE_S:
				case CEE_BRFALSE_S:
					CHECK_OPSIZE (2);
					ip++;
					target = ip + 1 + (signed char)(*ip);
					ip++;
					break;
				case CEE_BRTRUE:
				case CEE_BRFALSE:
					CHECK_OPSIZE (5);
					ip++;
					target = ip + 4 + (gint)(read32 (ip));
					ip += 4;
					break;
				default:
					g_assert_not_reached ();
				}

				/* 
				 * We need to link both bblocks, since it is needed for handling stack
				 * arguments correctly (See test_0_box_brtrue_opt_regress_81102).
				 * Branching to only one of them would lead to inconsistencies, so
				 * generate an ICONST+BRTRUE, the branch opts will get rid of them.
				 */
				GET_BBLOCK (cfg, true_bb, target);
				GET_BBLOCK (cfg, false_bb, ip);

				mono_link_bblock (cfg, cfg->cbb, true_bb);
				mono_link_bblock (cfg, cfg->cbb, false_bb);

				if (sp != stack_start) {
					handle_stack_args (cfg, stack_start, sp - stack_start);
					sp = stack_start;
					CHECK_UNVERIFIABLE (cfg);
				}

				if (COMPILE_LLVM (cfg)) {
					dreg = alloc_ireg (cfg);
					MONO_EMIT_NEW_ICONST (cfg, dreg, 0);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, dreg, is_true ? 0 : 1);

					MONO_EMIT_NEW_BRANCH_BLOCK2 (cfg, OP_IBEQ, true_bb, false_bb);
				} else {
					/* The JIT can't eliminate the iconst+compare */
					MONO_INST_NEW (cfg, ins, OP_BR);
					ins->inst_target_bb = is_true ? true_bb : false_bb;
					MONO_ADD_INS (cfg->cbb, ins);
				}

				start_new_bblock = 1;
				break;
			}

			*sp++ = handle_box (cfg, val, klass, context_used, &bblock);

			CHECK_CFG_EXCEPTION;
			ip += 5;
			inline_costs += 1;
			break;
		}
		case CEE_UNBOX: {
			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			mono_save_token_info (cfg, image, token, klass);

			context_used = mini_class_check_context_used (cfg, klass);

			if (mono_class_is_nullable (klass)) {
				MonoInst *val;

				val = handle_unbox_nullable (cfg, *sp, klass, context_used);
				EMIT_NEW_VARLOADA (cfg, ins, get_vreg_to_inst (cfg, val->dreg), &val->klass->byval_arg);

				*sp++= ins;
			} else {
				ins = handle_unbox (cfg, klass, sp, context_used);
				*sp++ = ins;
			}
			ip += 5;
			inline_costs += 2;
			break;
		}
		case CEE_LDFLD:
		case CEE_LDFLDA:
		case CEE_STFLD:
		case CEE_LDSFLD:
		case CEE_LDSFLDA:
		case CEE_STSFLD: {
			MonoClassField *field;
#ifndef DISABLE_REMOTING
			int costs;
#endif
			guint foffset;
			gboolean is_instance;
			int op;
			gpointer addr = NULL;
			gboolean is_special_static;
			MonoType *ftype;
			MonoInst *store_val = NULL;
			MonoInst *thread_ins;

			op = *ip;
			is_instance = (op == CEE_LDFLD || op == CEE_LDFLDA || op == CEE_STFLD);
			if (is_instance) {
				if (op == CEE_STFLD) {
					CHECK_STACK (2);
					sp -= 2;
					store_val = sp [1];
				} else {
					CHECK_STACK (1);
					--sp;
				}
				if (sp [0]->type == STACK_I4 || sp [0]->type == STACK_I8 || sp [0]->type == STACK_R8)
					UNVERIFIED;
				if (*ip != CEE_LDFLD && sp [0]->type == STACK_VTYPE)
					UNVERIFIED;
			} else {
				if (op == CEE_STSFLD) {
					CHECK_STACK (1);
					sp--;
					store_val = sp [0];
				}
			}

			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE) {
				field = mono_method_get_wrapper_data (method, token);
				klass = field->parent;
			}
			else {
				field = mono_field_from_token_checked (image, token, &klass, generic_context, &cfg->error);
				CHECK_CFG_ERROR;
			}
			if (!dont_verify && !cfg->skip_visibility && !mono_method_can_access_field (method, field))
				FIELD_ACCESS_FAILURE (method, field);
			mono_class_init (klass);

			if (is_instance && *ip != CEE_LDFLDA && is_magic_tls_access (field))
				UNVERIFIED;

			/* if the class is Critical then transparent code cannot access it's fields */
			if (!is_instance && mono_security_core_clr_enabled ())
				ensure_method_is_allowed_to_access_field (cfg, method, field, bblock, ip);

			/* XXX this is technically required but, so far (SL2), no [SecurityCritical] types (not many exists) have
			   any visible *instance* field  (in fact there's a single case for a static field in Marshal) XXX
			if (mono_security_core_clr_enabled ())
				ensure_method_is_allowed_to_access_field (cfg, method, field, bblock, ip);
			*/

			/*
			 * LDFLD etc. is usable on static fields as well, so convert those cases to
			 * the static case.
			 */
			if (is_instance && field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
				switch (op) {
				case CEE_LDFLD:
					op = CEE_LDSFLD;
					break;
				case CEE_STFLD:
					op = CEE_STSFLD;
					break;
				case CEE_LDFLDA:
					op = CEE_LDSFLDA;
					break;
				default:
					g_assert_not_reached ();
				}
				is_instance = FALSE;
			}

			context_used = mini_class_check_context_used (cfg, klass);

			/* INSTANCE CASE */

			foffset = klass->valuetype? field->offset - sizeof (MonoObject): field->offset;
			if (op == CEE_STFLD) {
				if (target_type_is_incompatible (cfg, field->type, sp [1]))
					UNVERIFIED;
#ifndef DISABLE_REMOTING
				if ((mono_class_is_marshalbyref (klass) && !MONO_CHECK_THIS (sp [0])) || mono_class_is_contextbound (klass) || klass == mono_defaults.marshalbyrefobject_class) {
					MonoMethod *stfld_wrapper = mono_marshal_get_stfld_wrapper (field->type); 
					MonoInst *iargs [5];

					GSHAREDVT_FAILURE (op);

					iargs [0] = sp [0];
					EMIT_NEW_CLASSCONST (cfg, iargs [1], klass);
					EMIT_NEW_FIELDCONST (cfg, iargs [2], field);
					EMIT_NEW_ICONST (cfg, iargs [3], klass->valuetype ? field->offset - sizeof (MonoObject) : 
						    field->offset);
					iargs [4] = sp [1];

					if (cfg->opt & MONO_OPT_INLINE || cfg->compile_aot) {
						costs = inline_method (cfg, stfld_wrapper, mono_method_signature (stfld_wrapper), 
											   iargs, ip, cfg->real_offset, TRUE, &bblock);
						CHECK_CFG_EXCEPTION;
						g_assert (costs > 0);
						      
						cfg->real_offset += 5;

						inline_costs += costs;
					} else {
						mono_emit_method_call (cfg, stfld_wrapper, iargs, NULL);
					}
				} else
#endif
				{
					MonoInst *store;

					MONO_EMIT_NULL_CHECK (cfg, sp [0]->dreg);

					if (mini_is_gsharedvt_klass (cfg, klass)) {
						MonoInst *offset_ins;

						context_used = mini_class_check_context_used (cfg, klass);

						offset_ins = emit_get_gsharedvt_info (cfg, field, MONO_RGCTX_INFO_FIELD_OFFSET);
						dreg = alloc_ireg_mp (cfg);
						EMIT_NEW_BIALU (cfg, ins, OP_PADD, dreg, sp [0]->dreg, offset_ins->dreg);
						/* The decomposition will call mini_emit_stobj () which will emit a wbarrier if needed */
						EMIT_NEW_STORE_MEMBASE_TYPE (cfg, store, field->type, dreg, 0, sp [1]->dreg);
					} else {
						EMIT_NEW_STORE_MEMBASE_TYPE (cfg, store, field->type, sp [0]->dreg, foffset, sp [1]->dreg);
					}
					if (sp [0]->opcode != OP_LDADDR)
						store->flags |= MONO_INST_FAULT;

				if (cfg->gen_write_barriers && mini_type_to_stind (cfg, field->type) == CEE_STIND_REF && !(sp [1]->opcode == OP_PCONST && sp [1]->inst_c0 == 0)) {
					/* insert call to write barrier */
					MonoInst *ptr;
					int dreg;

					dreg = alloc_ireg_mp (cfg);
					EMIT_NEW_BIALU_IMM (cfg, ptr, OP_PADD_IMM, dreg, sp [0]->dreg, foffset);
					emit_write_barrier (cfg, ptr, sp [1]);
				}

					store->flags |= ins_flag;
				}
				ins_flag = 0;
				ip += 5;
				break;
			}

#ifndef DISABLE_REMOTING
			if (is_instance && ((mono_class_is_marshalbyref (klass) && !MONO_CHECK_THIS (sp [0])) || mono_class_is_contextbound (klass) || klass == mono_defaults.marshalbyrefobject_class)) {
				MonoMethod *wrapper = (op == CEE_LDFLDA) ? mono_marshal_get_ldflda_wrapper (field->type) : mono_marshal_get_ldfld_wrapper (field->type); 
				MonoInst *iargs [4];

				GSHAREDVT_FAILURE (op);

				iargs [0] = sp [0];
				EMIT_NEW_CLASSCONST (cfg, iargs [1], klass);
				EMIT_NEW_FIELDCONST (cfg, iargs [2], field);
				EMIT_NEW_ICONST (cfg, iargs [3], klass->valuetype ? field->offset - sizeof (MonoObject) : field->offset);
				if (cfg->opt & MONO_OPT_INLINE || cfg->compile_aot) {
					costs = inline_method (cfg, wrapper, mono_method_signature (wrapper), 
										   iargs, ip, cfg->real_offset, TRUE, &bblock);
					CHECK_CFG_EXCEPTION;
					g_assert (costs > 0);
						      
					cfg->real_offset += 5;

					*sp++ = iargs [0];

					inline_costs += costs;
				} else {
					ins = mono_emit_method_call (cfg, wrapper, iargs, NULL);
					*sp++ = ins;
				}
			} else 
#endif
			if (is_instance) {
				if (sp [0]->type == STACK_VTYPE) {
					MonoInst *var;

					/* Have to compute the address of the variable */

					var = get_vreg_to_inst (cfg, sp [0]->dreg);
					if (!var)
						var = mono_compile_create_var_for_vreg (cfg, &klass->byval_arg, OP_LOCAL, sp [0]->dreg);
					else
						g_assert (var->klass == klass);
					
					EMIT_NEW_VARLOADA (cfg, ins, var, &var->klass->byval_arg);
					sp [0] = ins;
				}

				if (op == CEE_LDFLDA) {
					if (is_magic_tls_access (field)) {
						GSHAREDVT_FAILURE (*ip);
						ins = sp [0];
						*sp++ = create_magic_tls_access (cfg, field, &cached_tls_addr, ins);
					} else {
						if (sp [0]->type == STACK_OBJ) {
							MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, sp [0]->dreg, 0);
							MONO_EMIT_NEW_COND_EXC (cfg, EQ, "NullReferenceException");
						}

						dreg = alloc_ireg_mp (cfg);

						if (mini_is_gsharedvt_klass (cfg, klass)) {
							MonoInst *offset_ins;

							offset_ins = emit_get_gsharedvt_info (cfg, field, MONO_RGCTX_INFO_FIELD_OFFSET);
							EMIT_NEW_BIALU (cfg, ins, OP_PADD, dreg, sp [0]->dreg, offset_ins->dreg);
						} else {
							EMIT_NEW_BIALU_IMM (cfg, ins, OP_PADD_IMM, dreg, sp [0]->dreg, foffset);
						}
						ins->klass = mono_class_from_mono_type (field->type);
						ins->type = STACK_MP;
						*sp++ = ins;
					}
				} else {
					MonoInst *load;

					MONO_EMIT_NULL_CHECK (cfg, sp [0]->dreg);

					if (mini_is_gsharedvt_klass (cfg, klass)) {
						MonoInst *offset_ins;

						offset_ins = emit_get_gsharedvt_info (cfg, field, MONO_RGCTX_INFO_FIELD_OFFSET);
						dreg = alloc_ireg_mp (cfg);
						EMIT_NEW_BIALU (cfg, ins, OP_PADD, dreg, sp [0]->dreg, offset_ins->dreg);
						EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, load, field->type, dreg, 0);
					} else {
						EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, load, field->type, sp [0]->dreg, foffset);
					}
					load->flags |= ins_flag;
					if (sp [0]->opcode != OP_LDADDR)
						load->flags |= MONO_INST_FAULT;
					*sp++ = load;
				}
			}

			if (is_instance) {
				ins_flag = 0;
				ip += 5;
				break;
			}

			/* STATIC CASE */

			/*
			 * We can only support shared generic static
			 * field access on architectures where the
			 * trampoline code has been extended to handle
			 * the generic class init.
			 */
#ifndef MONO_ARCH_VTABLE_REG
			GENERIC_SHARING_FAILURE (op);
#endif

			context_used = mini_class_check_context_used (cfg, klass);

			ftype = mono_field_get_type (field);

			if (ftype->attrs & FIELD_ATTRIBUTE_LITERAL)
				UNVERIFIED;

			/* The special_static_fields field is init'd in mono_class_vtable, so it needs
			 * to be called here.
			 */
			if (!context_used && !(cfg->opt & MONO_OPT_SHARED)) {
				mono_class_vtable (cfg->domain, klass);
				CHECK_TYPELOAD (klass);
			}
			mono_domain_lock (cfg->domain);
			if (cfg->domain->special_static_fields)
				addr = g_hash_table_lookup (cfg->domain->special_static_fields, field);
			mono_domain_unlock (cfg->domain);

			is_special_static = mono_class_field_is_special_static (field);

			if (is_special_static && ((gsize)addr & 0x80000000) == 0)
				thread_ins = mono_get_thread_intrinsic (cfg);
			else
				thread_ins = NULL;

			/* Generate IR to compute the field address */
			if (is_special_static && ((gsize)addr & 0x80000000) == 0 && thread_ins && !(cfg->opt & MONO_OPT_SHARED) && !context_used) {
				/*
				 * Fast access to TLS data
				 * Inline version of get_thread_static_data () in
				 * threads.c.
				 */
				guint32 offset;
				int idx, static_data_reg, array_reg, dreg;

				GSHAREDVT_FAILURE (op);

				// offset &= 0x7fffffff;
				// idx = (offset >> 24) - 1;
				//	return ((char*) thread->static_data [idx]) + (offset & 0xffffff);
				MONO_ADD_INS (cfg->cbb, thread_ins);
				static_data_reg = alloc_ireg (cfg);
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, static_data_reg, thread_ins->dreg, MONO_STRUCT_OFFSET (MonoInternalThread, static_data));

				if (cfg->compile_aot) {
					int offset_reg, offset2_reg, idx_reg;

					/* For TLS variables, this will return the TLS offset */
					EMIT_NEW_SFLDACONST (cfg, ins, field);
					offset_reg = ins->dreg;
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IAND_IMM, offset_reg, offset_reg, 0x7fffffff);
					idx_reg = alloc_ireg (cfg);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, idx_reg, offset_reg, 24);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISUB_IMM, idx_reg, idx_reg, 1);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHL_IMM, idx_reg, idx_reg, sizeof (gpointer) == 8 ? 3 : 2);
					MONO_EMIT_NEW_BIALU (cfg, OP_PADD, static_data_reg, static_data_reg, idx_reg);
					array_reg = alloc_ireg (cfg);
					MONO_EMIT_NEW_LOAD_MEMBASE (cfg, array_reg, static_data_reg, 0);
					offset2_reg = alloc_ireg (cfg);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IAND_IMM, offset2_reg, offset_reg, 0xffffff);
					dreg = alloc_ireg (cfg);
					EMIT_NEW_BIALU (cfg, ins, OP_PADD, dreg, array_reg, offset2_reg);
				} else {
					offset = (gsize)addr & 0x7fffffff;
					idx = (offset >> 24) - 1;

					array_reg = alloc_ireg (cfg);
					MONO_EMIT_NEW_LOAD_MEMBASE (cfg, array_reg, static_data_reg, idx * sizeof (gpointer));
					dreg = alloc_ireg (cfg);
					EMIT_NEW_BIALU_IMM (cfg, ins, OP_ADD_IMM, dreg, array_reg, (offset & 0xffffff));
				}
			} else if ((cfg->opt & MONO_OPT_SHARED) ||
					(cfg->compile_aot && is_special_static) ||
					(context_used && is_special_static)) {
				MonoInst *iargs [2];

				g_assert (field->parent);
				EMIT_NEW_DOMAINCONST (cfg, iargs [0]);
				if (context_used) {
					iargs [1] = emit_get_rgctx_field (cfg, context_used,
						field, MONO_RGCTX_INFO_CLASS_FIELD);
				} else {
					EMIT_NEW_FIELDCONST (cfg, iargs [1], field);
				}
				ins = mono_emit_jit_icall (cfg, mono_class_static_field_address, iargs);
			} else if (context_used) {
				MonoInst *static_data;

				/*
				g_print ("sharing static field access in %s.%s.%s - depth %d offset %d\n",
					method->klass->name_space, method->klass->name, method->name,
					depth, field->offset);
				*/

				if (mono_class_needs_cctor_run (klass, method))
					emit_generic_class_init (cfg, klass);

				/*
				 * The pointer we're computing here is
				 *
				 *   super_info.static_data + field->offset
				 */
				static_data = emit_get_rgctx_klass (cfg, context_used,
					klass, MONO_RGCTX_INFO_STATIC_DATA);

				if (mini_is_gsharedvt_klass (cfg, klass)) {
					MonoInst *offset_ins;

					offset_ins = emit_get_rgctx_field (cfg, context_used, field, MONO_RGCTX_INFO_FIELD_OFFSET);
					dreg = alloc_ireg_mp (cfg);
					EMIT_NEW_BIALU (cfg, ins, OP_PADD, dreg, static_data->dreg, offset_ins->dreg);
				} else if (field->offset == 0) {
					ins = static_data;
				} else {
					int addr_reg = mono_alloc_preg (cfg);
					EMIT_NEW_BIALU_IMM (cfg, ins, OP_PADD_IMM, addr_reg, static_data->dreg, field->offset);
				}
				} else if ((cfg->opt & MONO_OPT_SHARED) || (cfg->compile_aot && addr)) {
				MonoInst *iargs [2];

				g_assert (field->parent);
				EMIT_NEW_DOMAINCONST (cfg, iargs [0]);
				EMIT_NEW_FIELDCONST (cfg, iargs [1], field);
				ins = mono_emit_jit_icall (cfg, mono_class_static_field_address, iargs);
			} else {
				MonoVTable *vtable = NULL;

				if (!cfg->compile_aot)
					vtable = mono_class_vtable (cfg->domain, klass);
				CHECK_TYPELOAD (klass);

				if (!addr) {
					if (mini_field_access_needs_cctor_run (cfg, method, klass, vtable)) {
						if (!(g_slist_find (class_inits, klass))) {
							mono_emit_abs_call (cfg, MONO_PATCH_INFO_CLASS_INIT, klass, helper_sig_class_init_trampoline, NULL);
							if (cfg->verbose_level > 2)
								printf ("class %s.%s needs init call for %s\n", klass->name_space, klass->name, mono_field_get_name (field));
							class_inits = g_slist_prepend (class_inits, klass);
						}
					} else {
						if (cfg->run_cctors) {
							MonoException *ex;
							/* This makes so that inline cannot trigger */
							/* .cctors: too many apps depend on them */
							/* running with a specific order... */
							g_assert (vtable);
							if (! vtable->initialized)
								INLINE_FAILURE ("class init");
							ex = mono_runtime_class_init_full (vtable, FALSE);
							if (ex) {
								set_exception_object (cfg, ex);
								goto exception_exit;
							}
						}
					}
					if (cfg->compile_aot)
						EMIT_NEW_SFLDACONST (cfg, ins, field);
					else {
						g_assert (vtable);
						addr = (char*)mono_vtable_get_static_field_data (vtable) + field->offset;
						g_assert (addr);
						EMIT_NEW_PCONST (cfg, ins, addr);
					}
				} else {
					MonoInst *iargs [1];
					EMIT_NEW_ICONST (cfg, iargs [0], GPOINTER_TO_UINT (addr));
					ins = mono_emit_jit_icall (cfg, mono_get_special_static_data, iargs);
				}
			}

			/* Generate IR to do the actual load/store operation */

			if ((op == CEE_STFLD || op == CEE_STSFLD) && (ins_flag & MONO_INST_VOLATILE)) {
				/* Volatile stores have release semantics, see 12.6.7 in Ecma 335 */
				/* FIXME it's questionable if acquire semantics require full barrier or just LoadLoad*/
				emit_memory_barrier (cfg, FullBarrier);
			}

			if (op == CEE_LDSFLDA) {
				ins->klass = mono_class_from_mono_type (ftype);
				ins->type = STACK_PTR;
				*sp++ = ins;
			} else if (op == CEE_STSFLD) {
				MonoInst *store;

				EMIT_NEW_STORE_MEMBASE_TYPE (cfg, store, ftype, ins->dreg, 0, store_val->dreg);
				store->flags |= ins_flag;
			} else {
				gboolean is_const = FALSE;
				MonoVTable *vtable = NULL;
				gpointer addr = NULL;

				if (!context_used) {
					vtable = mono_class_vtable (cfg->domain, klass);
					CHECK_TYPELOAD (klass);
				}
				if ((ftype->attrs & FIELD_ATTRIBUTE_INIT_ONLY) && (((addr = mono_aot_readonly_field_override (field)) != NULL) ||
						(!context_used && !((cfg->opt & MONO_OPT_SHARED) || cfg->compile_aot) && vtable->initialized))) {
					int ro_type = ftype->type;
					if (!addr)
						addr = (char*)mono_vtable_get_static_field_data (vtable) + field->offset;
					if (ro_type == MONO_TYPE_VALUETYPE && ftype->data.klass->enumtype) {
						ro_type = mono_class_enum_basetype (ftype->data.klass)->type;
					}

					GSHAREDVT_FAILURE (op);

					/* printf ("RO-FIELD %s.%s:%s\n", klass->name_space, klass->name, mono_field_get_name (field));*/
					is_const = TRUE;
					switch (ro_type) {
					case MONO_TYPE_BOOLEAN:
					case MONO_TYPE_U1:
						EMIT_NEW_ICONST (cfg, *sp, *((guint8 *)addr));
						sp++;
						break;
					case MONO_TYPE_I1:
						EMIT_NEW_ICONST (cfg, *sp, *((gint8 *)addr));
						sp++;
						break;						
					case MONO_TYPE_CHAR:
					case MONO_TYPE_U2:
						EMIT_NEW_ICONST (cfg, *sp, *((guint16 *)addr));
						sp++;
						break;
					case MONO_TYPE_I2:
						EMIT_NEW_ICONST (cfg, *sp, *((gint16 *)addr));
						sp++;
						break;
						break;
					case MONO_TYPE_I4:
						EMIT_NEW_ICONST (cfg, *sp, *((gint32 *)addr));
						sp++;
						break;						
					case MONO_TYPE_U4:
						EMIT_NEW_ICONST (cfg, *sp, *((guint32 *)addr));
						sp++;
						break;
					case MONO_TYPE_I:
					case MONO_TYPE_U:
					case MONO_TYPE_PTR:
					case MONO_TYPE_FNPTR:
						EMIT_NEW_PCONST (cfg, *sp, *((gpointer *)addr));
						type_to_eval_stack_type ((cfg), field->type, *sp);
						sp++;
						break;
					case MONO_TYPE_STRING:
					case MONO_TYPE_OBJECT:
					case MONO_TYPE_CLASS:
					case MONO_TYPE_SZARRAY:
					case MONO_TYPE_ARRAY:
						if (!mono_gc_is_moving ()) {
							EMIT_NEW_PCONST (cfg, *sp, *((gpointer *)addr));
							type_to_eval_stack_type ((cfg), field->type, *sp);
							sp++;
						} else {
							is_const = FALSE;
						}
						break;
					case MONO_TYPE_I8:
					case MONO_TYPE_U8:
						EMIT_NEW_I8CONST (cfg, *sp, *((gint64 *)addr));
						sp++;
						break;
					case MONO_TYPE_R4:
					case MONO_TYPE_R8:
					case MONO_TYPE_VALUETYPE:
					default:
						is_const = FALSE;
						break;
					}
				}

				if (!is_const) {
					MonoInst *load;

					CHECK_STACK_OVF (1);

					EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, load, field->type, ins->dreg, 0);
					load->flags |= ins_flag;
					ins_flag = 0;
					*sp++ = load;
				}
			}

			if ((op == CEE_LDFLD || op == CEE_LDSFLD) && (ins_flag & MONO_INST_VOLATILE)) {
				/* Volatile loads have acquire semantics, see 12.6.7 in Ecma 335 */
				/* FIXME it's questionable if acquire semantics require full barrier or just LoadLoad*/
				emit_memory_barrier (cfg, FullBarrier);
			}

			ins_flag = 0;
			ip += 5;
			break;
		}
		case CEE_STOBJ:
			CHECK_STACK (2);
			sp -= 2;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			if (ins_flag & MONO_INST_VOLATILE) {
				/* Volatile stores have release semantics, see 12.6.7 in Ecma 335 */
				/* FIXME it's questionable if acquire semantics require full barrier or just LoadLoad*/
				emit_memory_barrier (cfg, FullBarrier);
			}
			/* FIXME: should check item at sp [1] is compatible with the type of the store. */
			EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, sp [0]->dreg, 0, sp [1]->dreg);
			ins->flags |= ins_flag;
			if (cfg->gen_write_barriers && cfg->method->wrapper_type != MONO_WRAPPER_WRITE_BARRIER &&
					generic_class_is_reference_type (cfg, klass)) {
				/* insert call to write barrier */
				emit_write_barrier (cfg, sp [0], sp [1]);
			}
			ins_flag = 0;
			ip += 5;
			inline_costs += 1;
			break;

			/*
			 * Array opcodes
			 */
		case CEE_NEWARR: {
			MonoInst *len_ins;
			const char *data_ptr;
			int data_size = 0;
			guint32 field_token;

			CHECK_STACK (1);
			--sp;

			CHECK_OPSIZE (5);
			token = read32 (ip + 1);

			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			context_used = mini_class_check_context_used (cfg, klass);

			if (sp [0]->type == STACK_I8 || (SIZEOF_VOID_P == 8 && sp [0]->type == STACK_PTR)) {
				MONO_INST_NEW (cfg, ins, OP_LCONV_TO_OVF_U4);
				ins->sreg1 = sp [0]->dreg;
				ins->type = STACK_I4;
				ins->dreg = alloc_ireg (cfg);
				MONO_ADD_INS (cfg->cbb, ins);
				*sp = mono_decompose_opcode (cfg, ins);
			}

			if (context_used) {
				MonoInst *args [3];
				MonoClass *array_class = mono_array_class_get (klass, 1);
				MonoMethod *managed_alloc = mono_gc_get_managed_array_allocator (array_class);

				/* FIXME: Use OP_NEWARR and decompose later to help abcrem */

				/* vtable */
				args [0] = emit_get_rgctx_klass (cfg, context_used,
					array_class, MONO_RGCTX_INFO_VTABLE);
				/* array len */
				args [1] = sp [0];

				if (managed_alloc)
					ins = mono_emit_method_call (cfg, managed_alloc, args, NULL);
				else
					ins = mono_emit_jit_icall (cfg, mono_array_new_specific, args);
			} else {
				if (cfg->opt & MONO_OPT_SHARED) {
					/* Decompose now to avoid problems with references to the domainvar */
					MonoInst *iargs [3];

					EMIT_NEW_DOMAINCONST (cfg, iargs [0]);
					EMIT_NEW_CLASSCONST (cfg, iargs [1], klass);
					iargs [2] = sp [0];

					ins = mono_emit_jit_icall (cfg, mono_array_new, iargs);
				} else {
					/* Decompose later since it is needed by abcrem */
					MonoClass *array_type = mono_array_class_get (klass, 1);
					mono_class_vtable (cfg->domain, array_type);
					CHECK_TYPELOAD (array_type);

					MONO_INST_NEW (cfg, ins, OP_NEWARR);
					ins->dreg = alloc_ireg_ref (cfg);
					ins->sreg1 = sp [0]->dreg;
					ins->inst_newa_class = klass;
					ins->type = STACK_OBJ;
					ins->klass = array_type;
					MONO_ADD_INS (cfg->cbb, ins);
					cfg->flags |= MONO_CFG_HAS_ARRAY_ACCESS;
					cfg->cbb->has_array_access = TRUE;

					/* Needed so mono_emit_load_get_addr () gets called */
					mono_get_got_var (cfg);
				}
			}

			len_ins = sp [0];
			ip += 5;
			*sp++ = ins;
			inline_costs += 1;

			/* 
			 * we inline/optimize the initialization sequence if possible.
			 * we should also allocate the array as not cleared, since we spend as much time clearing to 0 as initializing
			 * for small sizes open code the memcpy
			 * ensure the rva field is big enough
			 */
			if ((cfg->opt & MONO_OPT_INTRINS) && ip + 6 < end && ip_in_bb (cfg, bblock, ip + 6) && (len_ins->opcode == OP_ICONST) && (data_ptr = initialize_array_data (method, cfg->compile_aot, ip, klass, len_ins->inst_c0, &data_size, &field_token))) {
				MonoMethod *memcpy_method = get_memcpy_method ();
				MonoInst *iargs [3];
				int add_reg = alloc_ireg_mp (cfg);

				EMIT_NEW_BIALU_IMM (cfg, iargs [0], OP_PADD_IMM, add_reg, ins->dreg, MONO_STRUCT_OFFSET (MonoArray, vector));
				if (cfg->compile_aot) {
					EMIT_NEW_AOTCONST_TOKEN (cfg, iargs [1], MONO_PATCH_INFO_RVA, method->klass->image, GPOINTER_TO_UINT(field_token), STACK_PTR, NULL);
				} else {
					EMIT_NEW_PCONST (cfg, iargs [1], (char*)data_ptr);
				}
				EMIT_NEW_ICONST (cfg, iargs [2], data_size);
				mono_emit_method_call (cfg, memcpy_method, iargs, NULL);
				ip += 11;
			}

			break;
		}
		case CEE_LDLEN:
			CHECK_STACK (1);
			--sp;
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;

			MONO_INST_NEW (cfg, ins, OP_LDLEN);
			ins->dreg = alloc_preg (cfg);
			ins->sreg1 = sp [0]->dreg;
			ins->type = STACK_I4;
			/* This flag will be inherited by the decomposition */
			ins->flags |= MONO_INST_FAULT;
			MONO_ADD_INS (cfg->cbb, ins);
			cfg->flags |= MONO_CFG_HAS_ARRAY_ACCESS;
			cfg->cbb->has_array_access = TRUE;
			ip ++;
			*sp++ = ins;
			break;
		case CEE_LDELEMA:
			CHECK_STACK (2);
			sp -= 2;
			CHECK_OPSIZE (5);
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;

			cfg->flags |= MONO_CFG_HAS_LDELEMA;

			klass = mini_get_class (method, read32 (ip + 1), generic_context);
			CHECK_TYPELOAD (klass);
			/* we need to make sure that this array is exactly the type it needs
			 * to be for correctness. the wrappers are lax with their usage
			 * so we need to ignore them here
			 */
			if (!klass->valuetype && method->wrapper_type == MONO_WRAPPER_NONE && !readonly) {
				MonoClass *array_class = mono_array_class_get (klass, 1);
				mini_emit_check_array_type (cfg, sp [0], array_class);
				CHECK_TYPELOAD (array_class);
			}

			readonly = FALSE;
			ins = mini_emit_ldelema_1_ins (cfg, klass, sp [0], sp [1], TRUE);
			*sp++ = ins;
			ip += 5;
			break;
		case CEE_LDELEM:
		case CEE_LDELEM_I1:
		case CEE_LDELEM_U1:
		case CEE_LDELEM_I2:
		case CEE_LDELEM_U2:
		case CEE_LDELEM_I4:
		case CEE_LDELEM_U4:
		case CEE_LDELEM_I8:
		case CEE_LDELEM_I:
		case CEE_LDELEM_R4:
		case CEE_LDELEM_R8:
		case CEE_LDELEM_REF: {
			MonoInst *addr;

			CHECK_STACK (2);
			sp -= 2;

			if (*ip == CEE_LDELEM) {
				CHECK_OPSIZE (5);
				token = read32 (ip + 1);
				klass = mini_get_class (method, token, generic_context);
				CHECK_TYPELOAD (klass);
				mono_class_init (klass);
			}
			else
				klass = array_access_to_klass (*ip);

			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;

			cfg->flags |= MONO_CFG_HAS_LDELEMA;

			if (mini_is_gsharedvt_variable_klass (cfg, klass)) {
				// FIXME-VT: OP_ICONST optimization
				addr = mini_emit_ldelema_1_ins (cfg, klass, sp [0], sp [1], TRUE);
				EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, addr->dreg, 0);
				ins->opcode = OP_LOADV_MEMBASE;
			} else if (sp [1]->opcode == OP_ICONST) {
				int array_reg = sp [0]->dreg;
				int index_reg = sp [1]->dreg;
				int offset = (mono_class_array_element_size (klass) * sp [1]->inst_c0) + MONO_STRUCT_OFFSET (MonoArray, vector);

				MONO_EMIT_BOUNDS_CHECK (cfg, array_reg, MonoArray, max_length, index_reg);
				EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, array_reg, offset);
			} else {
				addr = mini_emit_ldelema_1_ins (cfg, klass, sp [0], sp [1], TRUE);
				EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &klass->byval_arg, addr->dreg, 0);
			}
			*sp++ = ins;
			if (*ip == CEE_LDELEM)
				ip += 5;
			else
				++ip;
			break;
		}
		case CEE_STELEM_I:
		case CEE_STELEM_I1:
		case CEE_STELEM_I2:
		case CEE_STELEM_I4:
		case CEE_STELEM_I8:
		case CEE_STELEM_R4:
		case CEE_STELEM_R8:
		case CEE_STELEM_REF:
		case CEE_STELEM: {
			CHECK_STACK (3);
			sp -= 3;

			cfg->flags |= MONO_CFG_HAS_LDELEMA;

			if (*ip == CEE_STELEM) {
				CHECK_OPSIZE (5);
				token = read32 (ip + 1);
				klass = mini_get_class (method, token, generic_context);
				CHECK_TYPELOAD (klass);
				mono_class_init (klass);
			}
			else
				klass = array_access_to_klass (*ip);

			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;

			emit_array_store (cfg, klass, sp, TRUE);

			if (*ip == CEE_STELEM)
				ip += 5;
			else
				++ip;
			inline_costs += 1;
			break;
		}
		case CEE_CKFINITE: {
			CHECK_STACK (1);
			--sp;

			MONO_INST_NEW (cfg, ins, OP_CKFINITE);
			ins->sreg1 = sp [0]->dreg;
			ins->dreg = alloc_freg (cfg);
			ins->type = STACK_R8;
			MONO_ADD_INS (bblock, ins);

			*sp++ = mono_decompose_opcode (cfg, ins);

			++ip;
			break;
		}
		case CEE_REFANYVAL: {
			MonoInst *src_var, *src;

			int klass_reg = alloc_preg (cfg);
			int dreg = alloc_preg (cfg);

			GSHAREDVT_FAILURE (*ip);

			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			CHECK_OPSIZE (5);
			klass = mini_get_class (method, read32 (ip + 1), generic_context);
			CHECK_TYPELOAD (klass);

			context_used = mini_class_check_context_used (cfg, klass);

			// FIXME:
			src_var = get_vreg_to_inst (cfg, sp [0]->dreg);
			if (!src_var)
				src_var = mono_compile_create_var_for_vreg (cfg, &mono_defaults.typed_reference_class->byval_arg, OP_LOCAL, sp [0]->dreg);
			EMIT_NEW_VARLOADA (cfg, src, src_var, src_var->inst_vtype);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, src->dreg, MONO_STRUCT_OFFSET (MonoTypedRef, klass));

			if (context_used) {
				MonoInst *klass_ins;

				klass_ins = emit_get_rgctx_klass (cfg, context_used,
						klass, MONO_RGCTX_INFO_KLASS);

				// FIXME:
				MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, klass_reg, klass_ins->dreg);
				MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");
			} else {
				mini_emit_class_check (cfg, klass_reg, klass);
			}
			EMIT_NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, dreg, src->dreg, MONO_STRUCT_OFFSET (MonoTypedRef, value));
			ins->type = STACK_MP;
			*sp++ = ins;
			ip += 5;
			break;
		}
		case CEE_MKREFANY: {
			MonoInst *loc, *addr;

			GSHAREDVT_FAILURE (*ip);

			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			CHECK_OPSIZE (5);
			klass = mini_get_class (method, read32 (ip + 1), generic_context);
			CHECK_TYPELOAD (klass);

			context_used = mini_class_check_context_used (cfg, klass);

			loc = mono_compile_create_var (cfg, &mono_defaults.typed_reference_class->byval_arg, OP_LOCAL);
			EMIT_NEW_TEMPLOADA (cfg, addr, loc->inst_c0);

			if (context_used) {
				MonoInst *const_ins;
				int type_reg = alloc_preg (cfg);

				const_ins = emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_KLASS);
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREP_MEMBASE_REG, addr->dreg, MONO_STRUCT_OFFSET (MonoTypedRef, klass), const_ins->dreg);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ADD_IMM, type_reg, const_ins->dreg, MONO_STRUCT_OFFSET (MonoClass, byval_arg));
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREP_MEMBASE_REG, addr->dreg, MONO_STRUCT_OFFSET (MonoTypedRef, type), type_reg);
			} else if (cfg->compile_aot) {
				int const_reg = alloc_preg (cfg);
				int type_reg = alloc_preg (cfg);

				MONO_EMIT_NEW_CLASSCONST (cfg, const_reg, klass);
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREP_MEMBASE_REG, addr->dreg, MONO_STRUCT_OFFSET (MonoTypedRef, klass), const_reg);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ADD_IMM, type_reg, const_reg, MONO_STRUCT_OFFSET (MonoClass, byval_arg));
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREP_MEMBASE_REG, addr->dreg, MONO_STRUCT_OFFSET (MonoTypedRef, type), type_reg);
			} else {
				MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREP_MEMBASE_IMM, addr->dreg, MONO_STRUCT_OFFSET (MonoTypedRef, type), &klass->byval_arg);
				MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREP_MEMBASE_IMM, addr->dreg, MONO_STRUCT_OFFSET (MonoTypedRef, klass), klass);
			}
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREP_MEMBASE_REG, addr->dreg, MONO_STRUCT_OFFSET (MonoTypedRef, value), sp [0]->dreg);

			EMIT_NEW_TEMPLOAD (cfg, ins, loc->inst_c0);
			ins->type = STACK_VTYPE;
			ins->klass = mono_defaults.typed_reference_class;
			*sp++ = ins;
			ip += 5;
			break;
		}
		case CEE_LDTOKEN: {
			gpointer handle;
			MonoClass *handle_class;

			CHECK_STACK_OVF (1);

			CHECK_OPSIZE (5);
			n = read32 (ip + 1);

			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD ||
					method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED) {
				handle = mono_method_get_wrapper_data (method, n);
				handle_class = mono_method_get_wrapper_data (method, n + 1);
				if (handle_class == mono_defaults.typehandle_class)
					handle = &((MonoClass*)handle)->byval_arg;
			}
			else {
				handle = mono_ldtoken (image, n, &handle_class, generic_context);
			}
			if (!handle)
				LOAD_ERROR;
			mono_class_init (handle_class);
			if (cfg->generic_sharing_context) {
				if (mono_metadata_token_table (n) == MONO_TABLE_TYPEDEF ||
						mono_metadata_token_table (n) == MONO_TABLE_TYPEREF) {
					/* This case handles ldtoken
					   of an open type, like for
					   typeof(Gen<>). */
					context_used = 0;
				} else if (handle_class == mono_defaults.typehandle_class) {
					context_used = mini_class_check_context_used (cfg, mono_class_from_mono_type (handle));
				} else if (handle_class == mono_defaults.fieldhandle_class)
					context_used = mini_class_check_context_used (cfg, ((MonoClassField*)handle)->parent);
				else if (handle_class == mono_defaults.methodhandle_class)
					context_used = mini_method_check_context_used (cfg, handle);
				else
					g_assert_not_reached ();
			}

			if ((cfg->opt & MONO_OPT_SHARED) &&
					method->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD &&
					method->wrapper_type != MONO_WRAPPER_SYNCHRONIZED) {
				MonoInst *addr, *vtvar, *iargs [3];
				int method_context_used;

				method_context_used = mini_method_check_context_used (cfg, method);

				vtvar = mono_compile_create_var (cfg, &handle_class->byval_arg, OP_LOCAL); 

				EMIT_NEW_IMAGECONST (cfg, iargs [0], image);
				EMIT_NEW_ICONST (cfg, iargs [1], n);
				if (method_context_used) {
					iargs [2] = emit_get_rgctx_method (cfg, method_context_used,
						method, MONO_RGCTX_INFO_METHOD);
					ins = mono_emit_jit_icall (cfg, mono_ldtoken_wrapper_generic_shared, iargs);
				} else {
					EMIT_NEW_PCONST (cfg, iargs [2], generic_context);
					ins = mono_emit_jit_icall (cfg, mono_ldtoken_wrapper, iargs);
				}
				EMIT_NEW_TEMPLOADA (cfg, addr, vtvar->inst_c0);

				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, addr->dreg, 0, ins->dreg);

				EMIT_NEW_TEMPLOAD (cfg, ins, vtvar->inst_c0);
			} else {
				if ((ip + 5 < end) && ip_in_bb (cfg, bblock, ip + 5) && 
					((ip [5] == CEE_CALL) || (ip [5] == CEE_CALLVIRT)) && 
					(cmethod = mini_get_method (cfg, method, read32 (ip + 6), NULL, generic_context)) &&
					(cmethod->klass == mono_defaults.systemtype_class) &&
					(strcmp (cmethod->name, "GetTypeFromHandle") == 0)) {
					MonoClass *tclass = mono_class_from_mono_type (handle);

					mono_class_init (tclass);
					if (context_used) {
						ins = emit_get_rgctx_klass (cfg, context_used,
							tclass, MONO_RGCTX_INFO_REFLECTION_TYPE);
					} else if (cfg->compile_aot) {
						if (method->wrapper_type) {
							mono_error_init (&error); //got to do it since there are multiple conditionals below
							if (mono_class_get_checked (tclass->image, tclass->type_token, &error) == tclass && !generic_context) {
								/* Special case for static synchronized wrappers */
								EMIT_NEW_TYPE_FROM_HANDLE_CONST (cfg, ins, tclass->image, tclass->type_token, generic_context);
							} else {
								mono_error_cleanup (&error); /* FIXME don't swallow the error */
								/* FIXME: n is not a normal token */
								DISABLE_AOT (cfg);
								EMIT_NEW_PCONST (cfg, ins, NULL);
							}
						} else {
							EMIT_NEW_TYPE_FROM_HANDLE_CONST (cfg, ins, image, n, generic_context);
						}
					} else {
						EMIT_NEW_PCONST (cfg, ins, mono_type_get_object (cfg->domain, handle));
					}
					ins->type = STACK_OBJ;
					ins->klass = cmethod->klass;
					ip += 5;
				} else {
					MonoInst *addr, *vtvar;

					vtvar = mono_compile_create_var (cfg, &handle_class->byval_arg, OP_LOCAL);

					if (context_used) {
						if (handle_class == mono_defaults.typehandle_class) {
							ins = emit_get_rgctx_klass (cfg, context_used,
									mono_class_from_mono_type (handle),
									MONO_RGCTX_INFO_TYPE);
						} else if (handle_class == mono_defaults.methodhandle_class) {
							ins = emit_get_rgctx_method (cfg, context_used,
									handle, MONO_RGCTX_INFO_METHOD);
						} else if (handle_class == mono_defaults.fieldhandle_class) {
							ins = emit_get_rgctx_field (cfg, context_used,
									handle, MONO_RGCTX_INFO_CLASS_FIELD);
						} else {
							g_assert_not_reached ();
						}
					} else if (cfg->compile_aot) {
						EMIT_NEW_LDTOKENCONST (cfg, ins, image, n, generic_context);
					} else {
						EMIT_NEW_PCONST (cfg, ins, handle);
					}
					EMIT_NEW_TEMPLOADA (cfg, addr, vtvar->inst_c0);
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, addr->dreg, 0, ins->dreg);
					EMIT_NEW_TEMPLOAD (cfg, ins, vtvar->inst_c0);
				}
			}

			*sp++ = ins;
			ip += 5;
			break;
		}
		case CEE_THROW:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, OP_THROW);
			--sp;
			ins->sreg1 = sp [0]->dreg;
			ip++;
			bblock->out_of_line = TRUE;
			MONO_ADD_INS (bblock, ins);
			MONO_INST_NEW (cfg, ins, OP_NOT_REACHED);
			MONO_ADD_INS (bblock, ins);
			sp = stack_start;
			
			link_bblock (cfg, bblock, end_bblock);
			start_new_bblock = 1;
			break;
		case CEE_ENDFINALLY:
			/* mono_save_seq_point_info () depends on this */
			if (sp != stack_start)
				emit_seq_point (cfg, method, ip, FALSE, FALSE);
			MONO_INST_NEW (cfg, ins, OP_ENDFINALLY);
			MONO_ADD_INS (bblock, ins);
			ip++;
			start_new_bblock = 1;

			/*
			 * Control will leave the method so empty the stack, otherwise
			 * the next basic block will start with a nonempty stack.
			 */
			while (sp != stack_start) {
				sp--;
			}
			break;
		case CEE_LEAVE:
		case CEE_LEAVE_S: {
			GList *handlers;

			if (*ip == CEE_LEAVE) {
				CHECK_OPSIZE (5);
				target = ip + 5 + (gint32)read32(ip + 1);
			} else {
				CHECK_OPSIZE (2);
				target = ip + 2 + (signed char)(ip [1]);
			}

			/* empty the stack */
			while (sp != stack_start) {
				sp--;
			}

			/* 
			 * If this leave statement is in a catch block, check for a
			 * pending exception, and rethrow it if necessary.
			 * We avoid doing this in runtime invoke wrappers, since those are called
			 * by native code which excepts the wrapper to catch all exceptions.
			 */
			for (i = 0; i < header->num_clauses; ++i) {
				MonoExceptionClause *clause = &header->clauses [i];

				/* 
				 * Use <= in the final comparison to handle clauses with multiple
				 * leave statements, like in bug #78024.
				 * The ordering of the exception clauses guarantees that we find the
				 * innermost clause.
				 */
				if (MONO_OFFSET_IN_HANDLER (clause, ip - header->code) && (clause->flags == MONO_EXCEPTION_CLAUSE_NONE) && (ip - header->code + ((*ip == CEE_LEAVE) ? 5 : 2)) <= (clause->handler_offset + clause->handler_len) && method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE) {
					MonoInst *exc_ins;
					MonoBasicBlock *dont_throw;

					/*
					  MonoInst *load;

					  NEW_TEMPLOAD (cfg, load, mono_find_exvar_for_offset (cfg, clause->handler_offset)->inst_c0);
					*/

					exc_ins = mono_emit_jit_icall (cfg, mono_thread_get_undeniable_exception, NULL);

					NEW_BBLOCK (cfg, dont_throw);

					/*
					 * Currently, we always rethrow the abort exception, despite the 
					 * fact that this is not correct. See thread6.cs for an example. 
					 * But propagating the abort exception is more important than 
					 * getting the sematics right.
					 */
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, exc_ins->dreg, 0);
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, dont_throw);
					MONO_EMIT_NEW_UNALU (cfg, OP_THROW, -1, exc_ins->dreg);

					MONO_START_BB (cfg, dont_throw);
					bblock = cfg->cbb;
				}
			}

			if ((handlers = mono_find_final_block (cfg, ip, target, MONO_EXCEPTION_CLAUSE_FINALLY))) {
				GList *tmp;
				MonoExceptionClause *clause;

				for (tmp = handlers; tmp; tmp = tmp->next) {
					clause = tmp->data;
					tblock = cfg->cil_offset_to_bb [clause->handler_offset];
					g_assert (tblock);
					link_bblock (cfg, bblock, tblock);
					MONO_INST_NEW (cfg, ins, OP_CALL_HANDLER);
					ins->inst_target_bb = tblock;
					ins->inst_eh_block = clause;
					MONO_ADD_INS (bblock, ins);
					bblock->has_call_handler = 1;
					if (COMPILE_LLVM (cfg)) {
						MonoBasicBlock *target_bb;

						/* 
						 * Link the finally bblock with the target, since it will
						 * conceptually branch there.
						 * FIXME: Have to link the bblock containing the endfinally.
						 */
						GET_BBLOCK (cfg, target_bb, target);
						link_bblock (cfg, tblock, target_bb);
					}
				}
				g_list_free (handlers);
			} 

			MONO_INST_NEW (cfg, ins, OP_BR);
			MONO_ADD_INS (bblock, ins);
			GET_BBLOCK (cfg, tblock, target);
			link_bblock (cfg, bblock, tblock);
			ins->inst_target_bb = tblock;
			start_new_bblock = 1;

			if (*ip == CEE_LEAVE)
				ip += 5;
			else
				ip += 2;

			break;
		}

			/*
			 * Mono specific opcodes
			 */
		case MONO_CUSTOM_PREFIX: {

			g_assert (method->wrapper_type != MONO_WRAPPER_NONE);

			CHECK_OPSIZE (2);
			switch (ip [1]) {
			case CEE_MONO_ICALL: {
				gpointer func;
				MonoJitICallInfo *info;

				token = read32 (ip + 2);
				func = mono_method_get_wrapper_data (method, token);
				info = mono_find_jit_icall_by_addr (func);
				if (!info)
					g_error ("Could not find icall address in wrapper %s", mono_method_full_name (method, 1));
				g_assert (info);

				CHECK_STACK (info->sig->param_count);
				sp -= info->sig->param_count;

				ins = mono_emit_jit_icall (cfg, info->func, sp);
				if (!MONO_TYPE_IS_VOID (info->sig->ret))
					*sp++ = ins;

				ip += 6;
				inline_costs += 10 * num_calls++;

				break;
			}
			case CEE_MONO_LDPTR: {
				gpointer ptr;

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);

				ptr = mono_method_get_wrapper_data (method, token);
				/* FIXME: Generalize this */
				if (cfg->compile_aot && ptr == mono_thread_interruption_request_flag ()) {
					EMIT_NEW_AOTCONST (cfg, ins, MONO_PATCH_INFO_INTERRUPTION_REQUEST_FLAG, NULL);
					*sp++ = ins;
					ip += 6;
					break;
				}
				EMIT_NEW_PCONST (cfg, ins, ptr);
				*sp++ = ins;
				ip += 6;
				inline_costs += 10 * num_calls++;
				/* Can't embed random pointers into AOT code */
				DISABLE_AOT (cfg);
				break;
			}
			case CEE_MONO_JIT_ICALL_ADDR: {
				MonoJitICallInfo *callinfo;
				gpointer ptr;

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);

				ptr = mono_method_get_wrapper_data (method, token);
				callinfo = mono_find_jit_icall_by_addr (ptr);
				g_assert (callinfo);
				EMIT_NEW_JIT_ICALL_ADDRCONST (cfg, ins, (char*)callinfo->name);
				*sp++ = ins;
				ip += 6;
				inline_costs += 10 * num_calls++;
				break;
			}
			case CEE_MONO_ICALL_ADDR: {
				MonoMethod *cmethod;
				gpointer ptr;

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);

				cmethod = mono_method_get_wrapper_data (method, token);

				if (cfg->compile_aot) {
					EMIT_NEW_AOTCONST (cfg, ins, MONO_PATCH_INFO_ICALL_ADDR, cmethod);
				} else {
					ptr = mono_lookup_internal_call (cmethod);
					g_assert (ptr);
					EMIT_NEW_PCONST (cfg, ins, ptr);
				}
				*sp++ = ins;
				ip += 6;
				break;
			}
			case CEE_MONO_VTADDR: {
				MonoInst *src_var, *src;

				CHECK_STACK (1);
				--sp;

				// FIXME:
				src_var = get_vreg_to_inst (cfg, sp [0]->dreg);
				EMIT_NEW_VARLOADA ((cfg), (src), src_var, src_var->inst_vtype);
				*sp++ = src;
				ip += 2;
				break;
			}
			case CEE_MONO_NEWOBJ: {
				MonoInst *iargs [2];

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
				mono_class_init (klass);
				NEW_DOMAINCONST (cfg, iargs [0]);
				MONO_ADD_INS (cfg->cbb, iargs [0]);
				NEW_CLASSCONST (cfg, iargs [1], klass);
				MONO_ADD_INS (cfg->cbb, iargs [1]);
				*sp++ = mono_emit_jit_icall (cfg, mono_object_new, iargs);
				ip += 6;
				inline_costs += 10 * num_calls++;
				break;
			}
			case CEE_MONO_OBJADDR:
				CHECK_STACK (1);
				--sp;
				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = alloc_ireg_mp (cfg);
				ins->sreg1 = sp [0]->dreg;
				ins->type = STACK_MP;
				MONO_ADD_INS (cfg->cbb, ins);
				*sp++ = ins;
				ip += 2;
				break;
			case CEE_MONO_LDNATIVEOBJ:
				/*
				 * Similar to LDOBJ, but instead load the unmanaged 
				 * representation of the vtype to the stack.
				 */
				CHECK_STACK (1);
				CHECK_OPSIZE (6);
				--sp;
				token = read32 (ip + 2);
				klass = mono_method_get_wrapper_data (method, token);
				g_assert (klass->valuetype);
				mono_class_init (klass);

				{
					MonoInst *src, *dest, *temp;

					src = sp [0];
					temp = mono_compile_create_var (cfg, &klass->byval_arg, OP_LOCAL);
					temp->backend.is_pinvoke = 1;
					EMIT_NEW_TEMPLOADA (cfg, dest, temp->inst_c0);
					mini_emit_stobj (cfg, dest, src, klass, TRUE);

					EMIT_NEW_TEMPLOAD (cfg, dest, temp->inst_c0);
					dest->type = STACK_VTYPE;
					dest->klass = klass;

					*sp ++ = dest;
					ip += 6;
				}
				break;
			case CEE_MONO_RETOBJ: {
				/*
				 * Same as RET, but return the native representation of a vtype
				 * to the caller.
				 */
				g_assert (cfg->ret);
				g_assert (mono_method_signature (method)->pinvoke); 
				CHECK_STACK (1);
				--sp;
				
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);    
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);

				if (!cfg->vret_addr) {
					g_assert (cfg->ret_var_is_local);

					EMIT_NEW_VARLOADA (cfg, ins, cfg->ret, cfg->ret->inst_vtype);
				} else {
					EMIT_NEW_RETLOADA (cfg, ins);
				}
				mini_emit_stobj (cfg, ins, sp [0], klass, TRUE);
				
				if (sp != stack_start)
					UNVERIFIED;
				
				MONO_INST_NEW (cfg, ins, OP_BR);
				ins->inst_target_bb = end_bblock;
				MONO_ADD_INS (bblock, ins);
				link_bblock (cfg, bblock, end_bblock);
				start_new_bblock = 1;
				ip += 6;
				break;
			}
			case CEE_MONO_CISINST:
			case CEE_MONO_CCASTCLASS: {
				int token;
				CHECK_STACK (1);
				--sp;
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
				if (ip [1] == CEE_MONO_CISINST)
					ins = handle_cisinst (cfg, klass, sp [0]);
				else
					ins = handle_ccastclass (cfg, klass, sp [0]);
				bblock = cfg->cbb;
				*sp++ = ins;
				ip += 6;
				break;
			}
			case CEE_MONO_SAVE_LMF:
			case CEE_MONO_RESTORE_LMF:
#ifdef MONO_ARCH_HAVE_LMF_OPS
				MONO_INST_NEW (cfg, ins, (ip [1] == CEE_MONO_SAVE_LMF) ? OP_SAVE_LMF : OP_RESTORE_LMF);
				MONO_ADD_INS (bblock, ins);
				cfg->need_lmf_area = TRUE;
#endif
				ip += 2;
				break;
			case CEE_MONO_CLASSCONST:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				EMIT_NEW_CLASSCONST (cfg, ins, mono_method_get_wrapper_data (method, token));
				*sp++ = ins;
				ip += 6;
				inline_costs += 10 * num_calls++;
				break;
			case CEE_MONO_NOT_TAKEN:
				bblock->out_of_line = TRUE;
				ip += 2;
				break;
			case CEE_MONO_TLS: {
				int key;

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				key = (gint32)read32 (ip + 2);
				g_assert (key < TLS_KEY_NUM);

				ins = mono_create_tls_get (cfg, key);
				if (!ins) {
					if (cfg->compile_aot) {
						DISABLE_AOT (cfg);
						MONO_INST_NEW (cfg, ins, OP_TLS_GET);
						ins->dreg = alloc_preg (cfg);
						ins->type = STACK_PTR;
					} else {
						g_assert_not_reached ();
					}
				}
				ins->type = STACK_PTR;
				MONO_ADD_INS (bblock, ins);
				*sp++ = ins;
				ip += 6;
				break;
			}
			case CEE_MONO_DYN_CALL: {
				MonoCallInst *call;

				/* It would be easier to call a trampoline, but that would put an
				 * extra frame on the stack, confusing exception handling. So
				 * implement it inline using an opcode for now.
				 */

				if (!cfg->dyn_call_var) {
					cfg->dyn_call_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
					/* prevent it from being register allocated */
					cfg->dyn_call_var->flags |= MONO_INST_VOLATILE;
				}

				/* Has to use a call inst since it local regalloc expects it */
				MONO_INST_NEW_CALL (cfg, call, OP_DYN_CALL);
				ins = (MonoInst*)call;
				sp -= 2;
				ins->sreg1 = sp [0]->dreg;
				ins->sreg2 = sp [1]->dreg;
				MONO_ADD_INS (bblock, ins);

				cfg->param_area = MAX (cfg->param_area, MONO_ARCH_DYN_CALL_PARAM_AREA);

				ip += 2;
				inline_costs += 10 * num_calls++;

				break;
			}
			case CEE_MONO_MEMORY_BARRIER: {
				CHECK_OPSIZE (5);
				emit_memory_barrier (cfg, (int)read32 (ip + 1));
				ip += 5;
				break;
			}
			case CEE_MONO_JIT_ATTACH: {
				MonoInst *args [16], *domain_ins;
				MonoInst *ad_ins, *jit_tls_ins;
				MonoBasicBlock *next_bb = NULL, *call_bb = NULL;

				cfg->orig_domain_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);

				EMIT_NEW_PCONST (cfg, ins, NULL);
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->orig_domain_var->dreg, ins->dreg);

				ad_ins = mono_get_domain_intrinsic (cfg);
				jit_tls_ins = mono_get_jit_tls_intrinsic (cfg);

				if (MONO_ARCH_HAVE_TLS_GET && ad_ins && jit_tls_ins) {
					NEW_BBLOCK (cfg, next_bb);
					NEW_BBLOCK (cfg, call_bb);

					if (cfg->compile_aot) {
						/* AOT code is only used in the root domain */
						EMIT_NEW_PCONST (cfg, domain_ins, NULL);
					} else {
						EMIT_NEW_PCONST (cfg, domain_ins, cfg->domain);
					}
					MONO_ADD_INS (cfg->cbb, ad_ins);
					MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, ad_ins->dreg, domain_ins->dreg);
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, call_bb);

					MONO_ADD_INS (cfg->cbb, jit_tls_ins);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, jit_tls_ins->dreg, 0);
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, call_bb);

					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, next_bb);
					MONO_START_BB (cfg, call_bb);
				}

				if (cfg->compile_aot) {
					/* AOT code is only used in the root domain */
					EMIT_NEW_PCONST (cfg, args [0], NULL);
				} else {
					EMIT_NEW_PCONST (cfg, args [0], cfg->domain);
				}
				ins = mono_emit_jit_icall (cfg, mono_jit_thread_attach, args);
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->orig_domain_var->dreg, ins->dreg);

				if (next_bb) {
					MONO_START_BB (cfg, next_bb);
					bblock = cfg->cbb;
				}
				ip += 2;
				break;
			}
			case CEE_MONO_JIT_DETACH: {
				MonoInst *args [16];

				/* Restore the original domain */
				dreg = alloc_ireg (cfg);
				EMIT_NEW_UNALU (cfg, args [0], OP_MOVE, dreg, cfg->orig_domain_var->dreg);
				mono_emit_jit_icall (cfg, mono_jit_set_domain, args);
				ip += 2;
				break;
			}
			default:
				g_error ("opcode 0x%02x 0x%02x not handled", MONO_CUSTOM_PREFIX, ip [1]);
				break;
			}
			break;
		}

		case CEE_PREFIX1: {
			CHECK_OPSIZE (2);
			switch (ip [1]) {
			case CEE_ARGLIST: {
				/* somewhat similar to LDTOKEN */
				MonoInst *addr, *vtvar;
				CHECK_STACK_OVF (1);
				vtvar = mono_compile_create_var (cfg, &mono_defaults.argumenthandle_class->byval_arg, OP_LOCAL); 

				EMIT_NEW_TEMPLOADA (cfg, addr, vtvar->inst_c0);
				EMIT_NEW_UNALU (cfg, ins, OP_ARGLIST, -1, addr->dreg);

				EMIT_NEW_TEMPLOAD (cfg, ins, vtvar->inst_c0);
				ins->type = STACK_VTYPE;
				ins->klass = mono_defaults.argumenthandle_class;
				*sp++ = ins;
				ip += 2;
				break;
			}
			case CEE_CEQ:
			case CEE_CGT:
			case CEE_CGT_UN:
			case CEE_CLT:
			case CEE_CLT_UN: {
				MonoInst *cmp;
				CHECK_STACK (2);
				/*
				 * The following transforms:
				 *    CEE_CEQ    into OP_CEQ
				 *    CEE_CGT    into OP_CGT
				 *    CEE_CGT_UN into OP_CGT_UN
				 *    CEE_CLT    into OP_CLT
				 *    CEE_CLT_UN into OP_CLT_UN
				 */
				MONO_INST_NEW (cfg, cmp, (OP_CEQ - CEE_CEQ) + ip [1]);
				
				MONO_INST_NEW (cfg, ins, cmp->opcode);
				sp -= 2;
				cmp->sreg1 = sp [0]->dreg;
				cmp->sreg2 = sp [1]->dreg;
				type_from_op (cmp, sp [0], sp [1]);
				CHECK_TYPE (cmp);
				if ((sp [0]->type == STACK_I8) || ((SIZEOF_VOID_P == 8) && ((sp [0]->type == STACK_PTR) || (sp [0]->type == STACK_OBJ) || (sp [0]->type == STACK_MP))))
					cmp->opcode = OP_LCOMPARE;
				else if (sp [0]->type == STACK_R8)
					cmp->opcode = OP_FCOMPARE;
				else
					cmp->opcode = OP_ICOMPARE;
				MONO_ADD_INS (bblock, cmp);
				ins->type = STACK_I4;
				ins->dreg = alloc_dreg (cfg, ins->type);
				type_from_op (ins, sp [0], sp [1]);

				if (cmp->opcode == OP_FCOMPARE) {
					/*
					 * The backends expect the fceq opcodes to do the
					 * comparison too.
					 */
					ins->sreg1 = cmp->sreg1;
					ins->sreg2 = cmp->sreg2;
					NULLIFY_INS (cmp);
				}
				MONO_ADD_INS (bblock, ins);
				*sp++ = ins;
				ip += 2;
				break;
			}
			case CEE_LDFTN: {
				MonoInst *argconst;
				MonoMethod *cil_method;

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				n = read32 (ip + 2);
				cmethod = mini_get_method (cfg, method, n, NULL, generic_context);
				if (!cmethod || mono_loader_get_last_error ())
					LOAD_ERROR;
				mono_class_init (cmethod->klass);

				mono_save_token_info (cfg, image, n, cmethod);

				context_used = mini_method_check_context_used (cfg, cmethod);

				cil_method = cmethod;
				if (!dont_verify && !cfg->skip_visibility && !mono_method_can_access_method (method, cmethod))
					METHOD_ACCESS_FAILURE (method, cil_method);

				if (mono_security_cas_enabled ()) {
					if (check_linkdemand (cfg, method, cmethod))
						INLINE_FAILURE ("linkdemand");
					CHECK_CFG_EXCEPTION;
				} else if (mono_security_core_clr_enabled ()) {
					ensure_method_is_allowed_to_call_method (cfg, method, cmethod, bblock, ip);
				}

				/* 
				 * Optimize the common case of ldftn+delegate creation
				 */
				if ((sp > stack_start) && (ip + 6 + 5 < end) && ip_in_bb (cfg, bblock, ip + 6) && (ip [6] == CEE_NEWOBJ)) {
					MonoMethod *ctor_method = mini_get_method (cfg, method, read32 (ip + 7), NULL, generic_context);
					if (ctor_method && (ctor_method->klass->parent == mono_defaults.multicastdelegate_class)) {
						MonoInst *target_ins, *handle_ins;
						MonoMethod *invoke;
						int invoke_context_used;

						invoke = mono_get_delegate_invoke (ctor_method->klass);
						if (!invoke || !mono_method_signature (invoke))
							LOAD_ERROR;

						invoke_context_used = mini_method_check_context_used (cfg, invoke);

						target_ins = sp [-1];

						if (mono_security_core_clr_enabled ())
							ensure_method_is_allowed_to_call_method (cfg, method, ctor_method, bblock, ip);

						if (!(cmethod->flags & METHOD_ATTRIBUTE_STATIC)) {
							/*LAME IMPL: We must not add a null check for virtual invoke delegates.*/
							if (mono_method_signature (invoke)->param_count == mono_method_signature (cmethod)->param_count) {
								MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, target_ins->dreg, 0);
								MONO_EMIT_NEW_COND_EXC (cfg, EQ, "ArgumentException");
							}
						}

#if defined(MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE)
						/* FIXME: SGEN support */
						if (invoke_context_used == 0) {
							ip += 6;
							if (cfg->verbose_level > 3)
								g_print ("converting (in B%d: stack: %d) %s", bblock->block_num, (int)(sp - stack_start), mono_disasm_code_one (NULL, method, ip, NULL));
							if ((handle_ins = handle_delegate_ctor (cfg, ctor_method->klass, target_ins, cmethod, context_used, FALSE))) {
								sp --;
								*sp = handle_ins;
								CHECK_CFG_EXCEPTION;
								ip += 5;
								sp ++;
								break;
							}
							ip -= 6;
						}
#endif
					}
				}

				argconst = emit_get_rgctx_method (cfg, context_used, cmethod, MONO_RGCTX_INFO_METHOD);
				ins = mono_emit_jit_icall (cfg, mono_ldftn, &argconst);
				*sp++ = ins;
				
				ip += 6;
				inline_costs += 10 * num_calls++;
				break;
			}
			case CEE_LDVIRTFTN: {
				MonoInst *args [2];

				CHECK_STACK (1);
				CHECK_OPSIZE (6);
				n = read32 (ip + 2);
				cmethod = mini_get_method (cfg, method, n, NULL, generic_context);
				if (!cmethod || mono_loader_get_last_error ())
					LOAD_ERROR;
				mono_class_init (cmethod->klass);
 
				context_used = mini_method_check_context_used (cfg, cmethod);

				if (mono_security_cas_enabled ()) {
					if (check_linkdemand (cfg, method, cmethod))
						INLINE_FAILURE ("linkdemand");
					CHECK_CFG_EXCEPTION;
				} else if (mono_security_core_clr_enabled ()) {
					ensure_method_is_allowed_to_call_method (cfg, method, cmethod, bblock, ip);
				}

				/*
				 * Optimize the common case of ldvirtftn+delegate creation
				 */
				if ((sp > stack_start) && (ip + 6 + 5 < end) && ip_in_bb (cfg, bblock, ip + 6) && (ip [6] == CEE_NEWOBJ) && (ip > header->code && ip [-1] == CEE_DUP)) {
					MonoMethod *ctor_method = mini_get_method (cfg, method, read32 (ip + 7), NULL, generic_context);
					if (ctor_method && (ctor_method->klass->parent == mono_defaults.multicastdelegate_class)) {
						MonoInst *target_ins, *handle_ins;
						MonoMethod *invoke;
						int invoke_context_used;

						invoke = mono_get_delegate_invoke (ctor_method->klass);
						if (!invoke || !mono_method_signature (invoke))
							LOAD_ERROR;

						invoke_context_used = mini_method_check_context_used (cfg, invoke);

						target_ins = sp [-1];

						if (mono_security_core_clr_enabled ())
							ensure_method_is_allowed_to_call_method (cfg, method, ctor_method, bblock, ip);

#if defined(MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE)
						/* FIXME: SGEN support */
						if (invoke_context_used == 0) {
							ip += 6;
							if (cfg->verbose_level > 3)
								g_print ("converting (in B%d: stack: %d) %s", bblock->block_num, (int)(sp - stack_start), mono_disasm_code_one (NULL, method, ip, NULL));
							if ((handle_ins = handle_delegate_ctor (cfg, ctor_method->klass, target_ins, cmethod, context_used, TRUE))) {
								sp -= 2;
								*sp = handle_ins;
								CHECK_CFG_EXCEPTION;
								ip += 5;
								sp ++;
								break;
							}
							ip -= 6;
						}
#endif
					}
				}

				--sp;
				args [0] = *sp;

				args [1] = emit_get_rgctx_method (cfg, context_used,
												  cmethod, MONO_RGCTX_INFO_METHOD);

				if (context_used)
					*sp++ = mono_emit_jit_icall (cfg, mono_ldvirtfn_gshared, args);
				else
					*sp++ = mono_emit_jit_icall (cfg, mono_ldvirtfn, args);

				ip += 6;
				inline_costs += 10 * num_calls++;
				break;
			}
			case CEE_LDARG:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_ARG (n);
				EMIT_NEW_ARGLOAD (cfg, ins, n);
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_LDARGA:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_ARG (n);
				NEW_ARGLOADA (cfg, ins, n);
				MONO_ADD_INS (cfg->cbb, ins);
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_STARG:
				CHECK_STACK (1);
				--sp;
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_ARG (n);
				if (!dont_verify_stloc && target_type_is_incompatible (cfg, param_types [n], *sp))
					UNVERIFIED;
				EMIT_NEW_ARGSTORE (cfg, ins, n, *sp);
				ip += 4;
				break;
			case CEE_LDLOC:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_LOCAL (n);
				EMIT_NEW_LOCLOAD (cfg, ins, n);
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_LDLOCA: {
				unsigned char *tmp_ip;
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_LOCAL (n);

				if ((tmp_ip = emit_optimized_ldloca_ir (cfg, ip, end, 2))) {
					ip = tmp_ip;
					inline_costs += 1;
					break;
				}			
				
				EMIT_NEW_LOCLOADA (cfg, ins, n);
				*sp++ = ins;
				ip += 4;
				break;
			}
			case CEE_STLOC:
				CHECK_STACK (1);
				--sp;
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_LOCAL (n);
				if (!dont_verify_stloc && target_type_is_incompatible (cfg, header->locals [n], *sp))
					UNVERIFIED;
				emit_stloc_ir (cfg, sp, header, n);
				ip += 4;
				inline_costs += 1;
				break;
			case CEE_LOCALLOC:
				CHECK_STACK (1);
				--sp;
				if (sp != stack_start) 
					UNVERIFIED;
				if (cfg->method != method) 
					/* 
					 * Inlining this into a loop in a parent could lead to 
					 * stack overflows which is different behavior than the
					 * non-inlined case, thus disable inlining in this case.
					 */
					INLINE_FAILURE("localloc");

				MONO_INST_NEW (cfg, ins, OP_LOCALLOC);
				ins->dreg = alloc_preg (cfg);
				ins->sreg1 = sp [0]->dreg;
				ins->type = STACK_PTR;
				MONO_ADD_INS (cfg->cbb, ins);

				cfg->flags |= MONO_CFG_HAS_ALLOCA;
				if (init_locals)
					ins->flags |= MONO_INST_INIT;

				*sp++ = ins;
				ip += 2;
				break;
			case CEE_ENDFILTER: {
				MonoExceptionClause *clause, *nearest;
				int cc, nearest_num;

				CHECK_STACK (1);
				--sp;
				if ((sp != stack_start) || (sp [0]->type != STACK_I4)) 
					UNVERIFIED;
				MONO_INST_NEW (cfg, ins, OP_ENDFILTER);
				ins->sreg1 = (*sp)->dreg;
				MONO_ADD_INS (bblock, ins);
				start_new_bblock = 1;
				ip += 2;

				nearest = NULL;
				nearest_num = 0;
				for (cc = 0; cc < header->num_clauses; ++cc) {
					clause = &header->clauses [cc];
					if ((clause->flags & MONO_EXCEPTION_CLAUSE_FILTER) &&
						((ip - header->code) > clause->data.filter_offset && (ip - header->code) <= clause->handler_offset) &&
					    (!nearest || (clause->data.filter_offset < nearest->data.filter_offset))) {
						nearest = clause;
						nearest_num = cc;
					}
				}
				g_assert (nearest);
				if ((ip - header->code) != nearest->handler_offset)
					UNVERIFIED;

				break;
			}
			case CEE_UNALIGNED_:
				ins_flag |= MONO_INST_UNALIGNED;
				/* FIXME: record alignment? we can assume 1 for now */
				CHECK_OPSIZE (3);
				ip += 3;
				break;
			case CEE_VOLATILE_:
				ins_flag |= MONO_INST_VOLATILE;
				ip += 2;
				break;
			case CEE_TAIL_:
				ins_flag   |= MONO_INST_TAILCALL;
				cfg->flags |= MONO_CFG_HAS_TAIL;
				/* Can't inline tail calls at this time */
				inline_costs += 100000;
				ip += 2;
				break;
			case CEE_INITOBJ:
				CHECK_STACK (1);
				--sp;
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				klass = mini_get_class (method, token, generic_context);
				CHECK_TYPELOAD (klass);
				if (generic_class_is_reference_type (cfg, klass))
					MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STORE_MEMBASE_IMM, sp [0]->dreg, 0, 0);
				else
					mini_emit_initobj (cfg, *sp, NULL, klass);
				ip += 6;
				inline_costs += 1;
				break;
			case CEE_CONSTRAINED_:
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				constrained_call = mini_get_class (method, token, generic_context);
				CHECK_TYPELOAD (constrained_call);
				ip += 6;
				break;
			case CEE_CPBLK:
			case CEE_INITBLK: {
				MonoInst *iargs [3];
				CHECK_STACK (3);
				sp -= 3;

				/* Skip optimized paths for volatile operations. */
				if ((ip [1] == CEE_CPBLK) && !(ins_flag & MONO_INST_VOLATILE) && (cfg->opt & MONO_OPT_INTRINS) && (sp [2]->opcode == OP_ICONST) && ((n = sp [2]->inst_c0) <= sizeof (gpointer) * 5)) {
					mini_emit_memcpy (cfg, sp [0]->dreg, 0, sp [1]->dreg, 0, sp [2]->inst_c0, 0);
				} else if ((ip [1] == CEE_INITBLK) && !(ins_flag & MONO_INST_VOLATILE) && (cfg->opt & MONO_OPT_INTRINS) && (sp [2]->opcode == OP_ICONST) && ((n = sp [2]->inst_c0) <= sizeof (gpointer) * 5) && (sp [1]->opcode == OP_ICONST) && (sp [1]->inst_c0 == 0)) {
					/* emit_memset only works when val == 0 */
					mini_emit_memset (cfg, sp [0]->dreg, 0, sp [2]->inst_c0, sp [1]->inst_c0, 0);
				} else {
					MonoInst *call;
					iargs [0] = sp [0];
					iargs [1] = sp [1];
					iargs [2] = sp [2];
					if (ip [1] == CEE_CPBLK) {
						/*
						 * FIXME: It's unclear whether we should be emitting both the acquire
						 * and release barriers for cpblk. It is technically both a load and
						 * store operation, so it seems like that's the sensible thing to do.
						 */
						MonoMethod *memcpy_method = get_memcpy_method ();
						if (ins_flag & MONO_INST_VOLATILE) {
							/* Volatile stores have release semantics, see 12.6.7 in Ecma 335 */
							/* FIXME it's questionable if acquire semantics require full barrier or just LoadLoad*/
							emit_memory_barrier (cfg, FullBarrier);
						}
						call = mono_emit_method_call (cfg, memcpy_method, iargs, NULL);
						call->flags |= ins_flag;
						if (ins_flag & MONO_INST_VOLATILE) {
							/* Volatile loads have acquire semantics, see 12.6.7 in Ecma 335 */
							/* FIXME it's questionable if acquire semantics require full barrier or just LoadLoad*/
							emit_memory_barrier (cfg, FullBarrier);
						}
					} else {
						MonoMethod *memset_method = get_memset_method ();
						if (ins_flag & MONO_INST_VOLATILE) {
							/* Volatile stores have release semantics, see 12.6.7 in Ecma 335 */
							/* FIXME it's questionable if acquire semantics require full barrier or just LoadLoad*/
							emit_memory_barrier (cfg, FullBarrier);
						}
						call = mono_emit_method_call (cfg, memset_method, iargs, NULL);
						call->flags |= ins_flag;
					}
				}
				ip += 2;
				ins_flag = 0;
				inline_costs += 1;
				break;
			}
			case CEE_NO_:
				CHECK_OPSIZE (3);
				if (ip [2] & 0x1)
					ins_flag |= MONO_INST_NOTYPECHECK;
				if (ip [2] & 0x2)
					ins_flag |= MONO_INST_NORANGECHECK;
				/* we ignore the no-nullcheck for now since we
				 * really do it explicitly only when doing callvirt->call
				 */
				ip += 3;
				break;
			case CEE_RETHROW: {
				MonoInst *load;
				int handler_offset = -1;

				for (i = 0; i < header->num_clauses; ++i) {
					MonoExceptionClause *clause = &header->clauses [i];
					if (MONO_OFFSET_IN_HANDLER (clause, ip - header->code) && !(clause->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
						handler_offset = clause->handler_offset;
						break;
					}
				}

				bblock->flags |= BB_EXCEPTION_UNSAFE;

				if (handler_offset == -1)
					UNVERIFIED;

				EMIT_NEW_TEMPLOAD (cfg, load, mono_find_exvar_for_offset (cfg, handler_offset)->inst_c0);
				MONO_INST_NEW (cfg, ins, OP_RETHROW);
				ins->sreg1 = load->dreg;
				MONO_ADD_INS (bblock, ins);

				MONO_INST_NEW (cfg, ins, OP_NOT_REACHED);
				MONO_ADD_INS (bblock, ins);

				sp = stack_start;
				link_bblock (cfg, bblock, end_bblock);
				start_new_bblock = 1;
				ip += 2;
				break;
			}
			case CEE_SIZEOF: {
				guint32 val;
				int ialign;

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC && !image_is_dynamic (method->klass->image) && !generic_context) {
					MonoType *type = mono_type_create_from_typespec_checked (image, token, &cfg->error);
					CHECK_CFG_ERROR;

					val = mono_type_size (type, &ialign);
				} else {
					MonoClass *klass = mini_get_class (method, token, generic_context);
					CHECK_TYPELOAD (klass);

					val = mono_type_size (&klass->byval_arg, &ialign);

					if (mini_is_gsharedvt_klass (cfg, klass))
						GSHAREDVT_FAILURE (*ip);
				}
				EMIT_NEW_ICONST (cfg, ins, val);
				*sp++= ins;
				ip += 6;
				break;
			}
			case CEE_REFANYTYPE: {
				MonoInst *src_var, *src;

				GSHAREDVT_FAILURE (*ip);

				CHECK_STACK (1);
				--sp;

				// FIXME:
				src_var = get_vreg_to_inst (cfg, sp [0]->dreg);
				if (!src_var)
					src_var = mono_compile_create_var_for_vreg (cfg, &mono_defaults.typed_reference_class->byval_arg, OP_LOCAL, sp [0]->dreg);
				EMIT_NEW_VARLOADA (cfg, src, src_var, src_var->inst_vtype);
				EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, &mono_defaults.typehandle_class->byval_arg, src->dreg, MONO_STRUCT_OFFSET (MonoTypedRef, type));
				*sp++ = ins;
				ip += 2;
				break;
			}
			case CEE_READONLY_:
				readonly = TRUE;
				ip += 2;
				break;

			case CEE_UNUSED56:
			case CEE_UNUSED57:
			case CEE_UNUSED70:
			case CEE_UNUSED:
			case CEE_UNUSED99:
				UNVERIFIED;
				
			default:
				g_warning ("opcode 0xfe 0x%02x not handled", ip [1]);
				UNVERIFIED;
			}
			break;
		}
		case CEE_UNUSED58:
		case CEE_UNUSED1:
			UNVERIFIED;

		default:
			g_warning ("opcode 0x%02x not handled", *ip);
			UNVERIFIED;
		}
	}
	if (start_new_bblock != 1)
		UNVERIFIED;

	bblock->cil_length = ip - bblock->cil_code;
	if (bblock->next_bb) {
		/* This could already be set because of inlining, #693905 */
		MonoBasicBlock *bb = bblock;

		while (bb->next_bb)
			bb = bb->next_bb;
		bb->next_bb = end_bblock;
	} else {
		bblock->next_bb = end_bblock;
	}

	if (cfg->method == method && cfg->domainvar) {
		MonoInst *store;
		MonoInst *get_domain;

		cfg->cbb = init_localsbb;

		if ((get_domain = mono_get_domain_intrinsic (cfg))) {
			MONO_ADD_INS (cfg->cbb, get_domain);
		} else {
			get_domain = mono_emit_jit_icall (cfg, mono_domain_get, NULL);
		}
		NEW_TEMPSTORE (cfg, store, cfg->domainvar->inst_c0, get_domain);
		MONO_ADD_INS (cfg->cbb, store);
	}

#if defined(TARGET_POWERPC) || defined(TARGET_X86)
	if (cfg->compile_aot)
		/* FIXME: The plt slots require a GOT var even if the method doesn't use it */
		mono_get_got_var (cfg);
#endif

	if (cfg->method == method && cfg->got_var)
		mono_emit_load_got_addr (cfg);

	if (init_localsbb) {
		cfg->cbb = init_localsbb;
		cfg->ip = NULL;
		for (i = 0; i < header->num_locals; ++i) {
			emit_init_local (cfg, i, header->locals [i], init_locals);
		}
	}

	if (cfg->init_ref_vars && cfg->method == method) {
		/* Emit initialization for ref vars */
		// FIXME: Avoid duplication initialization for IL locals.
		for (i = 0; i < cfg->num_varinfo; ++i) {
			MonoInst *ins = cfg->varinfo [i];

			if (ins->opcode == OP_LOCAL && ins->type == STACK_OBJ)
				MONO_EMIT_NEW_PCONST (cfg, ins->dreg, NULL);
		}
	}

	if (cfg->lmf_var && cfg->method == method) {
		cfg->cbb = init_localsbb;
		emit_push_lmf (cfg);
	}

	cfg->cbb = init_localsbb;
	emit_instrumentation_call (cfg, mono_profiler_method_enter);

	if (seq_points) {
		MonoBasicBlock *bb;

		/*
		 * Make seq points at backward branch targets interruptable.
		 */
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
			if (bb->code && bb->in_count > 1 && bb->code->opcode == OP_SEQ_POINT)
				bb->code->flags |= MONO_INST_SINGLE_STEP_LOC;
	}

	/* Add a sequence point for method entry/exit events */
	if (seq_points) {
		NEW_SEQ_POINT (cfg, ins, METHOD_ENTRY_IL_OFFSET, FALSE);
		MONO_ADD_INS (init_localsbb, ins);
		NEW_SEQ_POINT (cfg, ins, METHOD_EXIT_IL_OFFSET, FALSE);
		MONO_ADD_INS (cfg->bb_exit, ins);
	}

	/*
	 * Add seq points for IL offsets which have line number info, but wasn't generated a seq point during JITting because
	 * the code they refer to was dead (#11880).
	 */
	if (sym_seq_points) {
		for (i = 0; i < header->code_size; ++i) {
			if (mono_bitset_test_fast (seq_point_locs, i) && !mono_bitset_test_fast (seq_point_set_locs, i)) {
				MonoInst *ins;

				NEW_SEQ_POINT (cfg, ins, i, FALSE);
				mono_add_seq_point (cfg, NULL, ins, SEQ_POINT_NATIVE_OFFSET_DEAD_CODE);
			}
		}
	}

	cfg->ip = NULL;

	if (cfg->method == method) {
		MonoBasicBlock *bb;
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			bb->region = mono_find_block_region (cfg, bb->real_offset);
			if (cfg->spvars)
				mono_create_spvar_for_region (cfg, bb->region);
			if (cfg->verbose_level > 2)
				printf ("REGION BB%d IL_%04x ID_%08X\n", bb->block_num, bb->real_offset, bb->region);
		}
	}

	if (inline_costs < 0) {
		char *mname;

		/* Method is too large */
		mname = mono_method_full_name (method, TRUE);
		mono_cfg_set_exception (cfg, MONO_EXCEPTION_INVALID_PROGRAM);
		cfg->exception_message = g_strdup_printf ("Method %s is too complex.", mname);
		g_free (mname);
	}

	if ((cfg->verbose_level > 2) && (cfg->method == method)) 
		mono_print_code (cfg, "AFTER METHOD-TO-IR");

	goto cleanup;

mono_error_exit:
	g_assert (!mono_error_ok (&cfg->error));
	goto cleanup;
 
 exception_exit:
	g_assert (cfg->exception_type != MONO_EXCEPTION_NONE);
	goto cleanup;

 unverified:
	set_exception_type_from_invalid_il (cfg, method, ip);
	goto cleanup;

 cleanup:
	g_slist_free (class_inits);
	mono_basic_block_free (original_bb);
	cfg->dont_inline = g_list_remove (cfg->dont_inline, method);
	cfg->headers_to_free = g_slist_prepend_mempool (cfg->mempool, cfg->headers_to_free, header);
	if (cfg->exception_type)
		return -1;
	else
		return inline_costs;
}

static int
store_membase_reg_to_store_membase_imm (int opcode)
{
	switch (opcode) {
	case OP_STORE_MEMBASE_REG:
		return OP_STORE_MEMBASE_IMM;
	case OP_STOREI1_MEMBASE_REG:
		return OP_STOREI1_MEMBASE_IMM;
	case OP_STOREI2_MEMBASE_REG:
		return OP_STOREI2_MEMBASE_IMM;
	case OP_STOREI4_MEMBASE_REG:
		return OP_STOREI4_MEMBASE_IMM;
	case OP_STOREI8_MEMBASE_REG:
		return OP_STOREI8_MEMBASE_IMM;
	default:
		g_assert_not_reached ();
	}

	return -1;
}		

int
mono_op_to_op_imm (int opcode)
{
	switch (opcode) {
	case OP_IADD:
		return OP_IADD_IMM;
	case OP_ISUB:
		return OP_ISUB_IMM;
	case OP_IDIV:
		return OP_IDIV_IMM;
	case OP_IDIV_UN:
		return OP_IDIV_UN_IMM;
	case OP_IREM:
		return OP_IREM_IMM;
	case OP_IREM_UN:
		return OP_IREM_UN_IMM;
	case OP_IMUL:
		return OP_IMUL_IMM;
	case OP_IAND:
		return OP_IAND_IMM;
	case OP_IOR:
		return OP_IOR_IMM;
	case OP_IXOR:
		return OP_IXOR_IMM;
	case OP_ISHL:
		return OP_ISHL_IMM;
	case OP_ISHR:
		return OP_ISHR_IMM;
	case OP_ISHR_UN:
		return OP_ISHR_UN_IMM;

	case OP_LADD:
		return OP_LADD_IMM;
	case OP_LSUB:
		return OP_LSUB_IMM;
	case OP_LAND:
		return OP_LAND_IMM;
	case OP_LOR:
		return OP_LOR_IMM;
	case OP_LXOR:
		return OP_LXOR_IMM;
	case OP_LSHL:
		return OP_LSHL_IMM;
	case OP_LSHR:
		return OP_LSHR_IMM;
	case OP_LSHR_UN:
		return OP_LSHR_UN_IMM;
#if SIZEOF_REGISTER == 8
	case OP_LREM:
		return OP_LREM_IMM;
#endif

	case OP_COMPARE:
		return OP_COMPARE_IMM;
	case OP_ICOMPARE:
		return OP_ICOMPARE_IMM;
	case OP_LCOMPARE:
		return OP_LCOMPARE_IMM;

	case OP_STORE_MEMBASE_REG:
		return OP_STORE_MEMBASE_IMM;
	case OP_STOREI1_MEMBASE_REG:
		return OP_STOREI1_MEMBASE_IMM;
	case OP_STOREI2_MEMBASE_REG:
		return OP_STOREI2_MEMBASE_IMM;
	case OP_STOREI4_MEMBASE_REG:
		return OP_STOREI4_MEMBASE_IMM;

#if defined(TARGET_X86) || defined (TARGET_AMD64)
	case OP_X86_PUSH:
		return OP_X86_PUSH_IMM;
	case OP_X86_COMPARE_MEMBASE_REG:
		return OP_X86_COMPARE_MEMBASE_IMM;
#endif
#if defined(TARGET_AMD64)
	case OP_AMD64_ICOMPARE_MEMBASE_REG:
		return OP_AMD64_ICOMPARE_MEMBASE_IMM;
#endif
	case OP_VOIDCALL_REG:
		return OP_VOIDCALL;
	case OP_CALL_REG:
		return OP_CALL;
	case OP_LCALL_REG:
		return OP_LCALL;
	case OP_FCALL_REG:
		return OP_FCALL;
	case OP_LOCALLOC:
		return OP_LOCALLOC_IMM;
	}

	return -1;
}

static int
ldind_to_load_membase (int opcode)
{
	switch (opcode) {
	case CEE_LDIND_I1:
		return OP_LOADI1_MEMBASE;
	case CEE_LDIND_U1:
		return OP_LOADU1_MEMBASE;
	case CEE_LDIND_I2:
		return OP_LOADI2_MEMBASE;
	case CEE_LDIND_U2:
		return OP_LOADU2_MEMBASE;
	case CEE_LDIND_I4:
		return OP_LOADI4_MEMBASE;
	case CEE_LDIND_U4:
		return OP_LOADU4_MEMBASE;
	case CEE_LDIND_I:
		return OP_LOAD_MEMBASE;
	case CEE_LDIND_REF:
		return OP_LOAD_MEMBASE;
	case CEE_LDIND_I8:
		return OP_LOADI8_MEMBASE;
	case CEE_LDIND_R4:
		return OP_LOADR4_MEMBASE;
	case CEE_LDIND_R8:
		return OP_LOADR8_MEMBASE;
	default:
		g_assert_not_reached ();
	}

	return -1;
}

static int
stind_to_store_membase (int opcode)
{
	switch (opcode) {
	case CEE_STIND_I1:
		return OP_STOREI1_MEMBASE_REG;
	case CEE_STIND_I2:
		return OP_STOREI2_MEMBASE_REG;
	case CEE_STIND_I4:
		return OP_STOREI4_MEMBASE_REG;
	case CEE_STIND_I:
	case CEE_STIND_REF:
		return OP_STORE_MEMBASE_REG;
	case CEE_STIND_I8:
		return OP_STOREI8_MEMBASE_REG;
	case CEE_STIND_R4:
		return OP_STORER4_MEMBASE_REG;
	case CEE_STIND_R8:
		return OP_STORER8_MEMBASE_REG;
	default:
		g_assert_not_reached ();
	}

	return -1;
}

int
mono_load_membase_to_load_mem (int opcode)
{
	// FIXME: Add a MONO_ARCH_HAVE_LOAD_MEM macro
#if defined(TARGET_X86) || defined(TARGET_AMD64)
	switch (opcode) {
	case OP_LOAD_MEMBASE:
		return OP_LOAD_MEM;
	case OP_LOADU1_MEMBASE:
		return OP_LOADU1_MEM;
	case OP_LOADU2_MEMBASE:
		return OP_LOADU2_MEM;
	case OP_LOADI4_MEMBASE:
		return OP_LOADI4_MEM;
	case OP_LOADU4_MEMBASE:
		return OP_LOADU4_MEM;
#if SIZEOF_REGISTER == 8
	case OP_LOADI8_MEMBASE:
		return OP_LOADI8_MEM;
#endif
	}
#endif

	return -1;
}

static inline int
op_to_op_dest_membase (int store_opcode, int opcode)
{
#if defined(TARGET_X86)
	if (!((store_opcode == OP_STORE_MEMBASE_REG) || (store_opcode == OP_STOREI4_MEMBASE_REG)))
		return -1;

	switch (opcode) {
	case OP_IADD:
		return OP_X86_ADD_MEMBASE_REG;
	case OP_ISUB:
		return OP_X86_SUB_MEMBASE_REG;
	case OP_IAND:
		return OP_X86_AND_MEMBASE_REG;
	case OP_IOR:
		return OP_X86_OR_MEMBASE_REG;
	case OP_IXOR:
		return OP_X86_XOR_MEMBASE_REG;
	case OP_ADD_IMM:
	case OP_IADD_IMM:
		return OP_X86_ADD_MEMBASE_IMM;
	case OP_SUB_IMM:
	case OP_ISUB_IMM:
		return OP_X86_SUB_MEMBASE_IMM;
	case OP_AND_IMM:
	case OP_IAND_IMM:
		return OP_X86_AND_MEMBASE_IMM;
	case OP_OR_IMM:
	case OP_IOR_IMM:
		return OP_X86_OR_MEMBASE_IMM;
	case OP_XOR_IMM:
	case OP_IXOR_IMM:
		return OP_X86_XOR_MEMBASE_IMM;
	case OP_MOVE:
		return OP_NOP;
	}
#endif

#if defined(TARGET_AMD64)
	if (!((store_opcode == OP_STORE_MEMBASE_REG) || (store_opcode == OP_STOREI4_MEMBASE_REG) || (store_opcode == OP_STOREI8_MEMBASE_REG)))
		return -1;

	switch (opcode) {
	case OP_IADD:
		return OP_X86_ADD_MEMBASE_REG;
	case OP_ISUB:
		return OP_X86_SUB_MEMBASE_REG;
	case OP_IAND:
		return OP_X86_AND_MEMBASE_REG;
	case OP_IOR:
		return OP_X86_OR_MEMBASE_REG;
	case OP_IXOR:
		return OP_X86_XOR_MEMBASE_REG;
	case OP_IADD_IMM:
		return OP_X86_ADD_MEMBASE_IMM;
	case OP_ISUB_IMM:
		return OP_X86_SUB_MEMBASE_IMM;
	case OP_IAND_IMM:
		return OP_X86_AND_MEMBASE_IMM;
	case OP_IOR_IMM:
		return OP_X86_OR_MEMBASE_IMM;
	case OP_IXOR_IMM:
		return OP_X86_XOR_MEMBASE_IMM;
	case OP_LADD:
		return OP_AMD64_ADD_MEMBASE_REG;
	case OP_LSUB:
		return OP_AMD64_SUB_MEMBASE_REG;
	case OP_LAND:
		return OP_AMD64_AND_MEMBASE_REG;
	case OP_LOR:
		return OP_AMD64_OR_MEMBASE_REG;
	case OP_LXOR:
		return OP_AMD64_XOR_MEMBASE_REG;
	case OP_ADD_IMM:
	case OP_LADD_IMM:
		return OP_AMD64_ADD_MEMBASE_IMM;
	case OP_SUB_IMM:
	case OP_LSUB_IMM:
		return OP_AMD64_SUB_MEMBASE_IMM;
	case OP_AND_IMM:
	case OP_LAND_IMM:
		return OP_AMD64_AND_MEMBASE_IMM;
	case OP_OR_IMM:
	case OP_LOR_IMM:
		return OP_AMD64_OR_MEMBASE_IMM;
	case OP_XOR_IMM:
	case OP_LXOR_IMM:
		return OP_AMD64_XOR_MEMBASE_IMM;
	case OP_MOVE:
		return OP_NOP;
	}
#endif

	return -1;
}

static inline int
op_to_op_store_membase (int store_opcode, int opcode)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
	switch (opcode) {
	case OP_ICEQ:
		if (store_opcode == OP_STOREI1_MEMBASE_REG)
			return OP_X86_SETEQ_MEMBASE;
	case OP_CNE:
		if (store_opcode == OP_STOREI1_MEMBASE_REG)
			return OP_X86_SETNE_MEMBASE;
	}
#endif

	return -1;
}

static inline int
op_to_op_src1_membase (int load_opcode, int opcode)
{
#ifdef TARGET_X86
	/* FIXME: This has sign extension issues */
	/*
	if ((opcode == OP_ICOMPARE_IMM) && (load_opcode == OP_LOADU1_MEMBASE))
		return OP_X86_COMPARE_MEMBASE8_IMM;
	*/

	if (!((load_opcode == OP_LOAD_MEMBASE) || (load_opcode == OP_LOADI4_MEMBASE) || (load_opcode == OP_LOADU4_MEMBASE)))
		return -1;

	switch (opcode) {
	case OP_X86_PUSH:
		return OP_X86_PUSH_MEMBASE;
	case OP_COMPARE_IMM:
	case OP_ICOMPARE_IMM:
		return OP_X86_COMPARE_MEMBASE_IMM;
	case OP_COMPARE:
	case OP_ICOMPARE:
		return OP_X86_COMPARE_MEMBASE_REG;
	}
#endif

#ifdef TARGET_AMD64
	/* FIXME: This has sign extension issues */
	/*
	if ((opcode == OP_ICOMPARE_IMM) && (load_opcode == OP_LOADU1_MEMBASE))
		return OP_X86_COMPARE_MEMBASE8_IMM;
	*/

	switch (opcode) {
	case OP_X86_PUSH:
#ifdef __mono_ilp32__
		if (load_opcode == OP_LOADI8_MEMBASE)
#else
		if ((load_opcode == OP_LOAD_MEMBASE) || (load_opcode == OP_LOADI8_MEMBASE))
#endif
			return OP_X86_PUSH_MEMBASE;
		break;
		/* FIXME: This only works for 32 bit immediates
	case OP_COMPARE_IMM:
	case OP_LCOMPARE_IMM:
		if ((load_opcode == OP_LOAD_MEMBASE) || (load_opcode == OP_LOADI8_MEMBASE))
			return OP_AMD64_COMPARE_MEMBASE_IMM;
		*/
	case OP_ICOMPARE_IMM:
		if ((load_opcode == OP_LOADI4_MEMBASE) || (load_opcode == OP_LOADU4_MEMBASE))
			return OP_AMD64_ICOMPARE_MEMBASE_IMM;
		break;
	case OP_COMPARE:
	case OP_LCOMPARE:
#ifdef __mono_ilp32__
		if (load_opcode == OP_LOAD_MEMBASE)
			return OP_AMD64_ICOMPARE_MEMBASE_REG;
		if (load_opcode == OP_LOADI8_MEMBASE)
#else
		if ((load_opcode == OP_LOAD_MEMBASE) || (load_opcode == OP_LOADI8_MEMBASE))
#endif
			return OP_AMD64_COMPARE_MEMBASE_REG;
		break;
	case OP_ICOMPARE:
		if ((load_opcode == OP_LOADI4_MEMBASE) || (load_opcode == OP_LOADU4_MEMBASE))
			return OP_AMD64_ICOMPARE_MEMBASE_REG;
		break;
	}
#endif

	return -1;
}

static inline int
op_to_op_src2_membase (int load_opcode, int opcode)
{
#ifdef TARGET_X86
	if (!((load_opcode == OP_LOAD_MEMBASE) || (load_opcode == OP_LOADI4_MEMBASE) || (load_opcode == OP_LOADU4_MEMBASE)))
		return -1;
	
	switch (opcode) {
	case OP_COMPARE:
	case OP_ICOMPARE:
		return OP_X86_COMPARE_REG_MEMBASE;
	case OP_IADD:
		return OP_X86_ADD_REG_MEMBASE;
	case OP_ISUB:
		return OP_X86_SUB_REG_MEMBASE;
	case OP_IAND:
		return OP_X86_AND_REG_MEMBASE;
	case OP_IOR:
		return OP_X86_OR_REG_MEMBASE;
	case OP_IXOR:
		return OP_X86_XOR_REG_MEMBASE;
	}
#endif

#ifdef TARGET_AMD64
#ifdef __mono_ilp32__
	if ((load_opcode == OP_LOADI4_MEMBASE) || (load_opcode == OP_LOADU4_MEMBASE) || (load_opcode == OP_LOAD_MEMBASE) ) {
#else
	if ((load_opcode == OP_LOADI4_MEMBASE) || (load_opcode == OP_LOADU4_MEMBASE)) {
#endif
		switch (opcode) {
		case OP_ICOMPARE:
			return OP_AMD64_ICOMPARE_REG_MEMBASE;
		case OP_IADD:
			return OP_X86_ADD_REG_MEMBASE;
		case OP_ISUB:
			return OP_X86_SUB_REG_MEMBASE;
		case OP_IAND:
			return OP_X86_AND_REG_MEMBASE;
		case OP_IOR:
			return OP_X86_OR_REG_MEMBASE;
		case OP_IXOR:
			return OP_X86_XOR_REG_MEMBASE;
		}
#ifdef __mono_ilp32__
	} else if (load_opcode == OP_LOADI8_MEMBASE) {
#else
	} else if ((load_opcode == OP_LOADI8_MEMBASE) || (load_opcode == OP_LOAD_MEMBASE)) {
#endif
		switch (opcode) {
		case OP_COMPARE:
		case OP_LCOMPARE:
			return OP_AMD64_COMPARE_REG_MEMBASE;
		case OP_LADD:
			return OP_AMD64_ADD_REG_MEMBASE;
		case OP_LSUB:
			return OP_AMD64_SUB_REG_MEMBASE;
		case OP_LAND:
			return OP_AMD64_AND_REG_MEMBASE;
		case OP_LOR:
			return OP_AMD64_OR_REG_MEMBASE;
		case OP_LXOR:
			return OP_AMD64_XOR_REG_MEMBASE;
		}
	}
#endif

	return -1;
}

int
mono_op_to_op_imm_noemul (int opcode)
{
	switch (opcode) {
#if SIZEOF_REGISTER == 4 && !defined(MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS)
	case OP_LSHR:
	case OP_LSHL:
	case OP_LSHR_UN:
		return -1;
#endif
#if defined(MONO_ARCH_EMULATE_MUL_DIV) || defined(MONO_ARCH_EMULATE_DIV)
	case OP_IDIV:
	case OP_IDIV_UN:
	case OP_IREM:
	case OP_IREM_UN:
		return -1;
#endif
#if defined(MONO_ARCH_EMULATE_MUL_DIV)
	case OP_IMUL:
		return -1;
#endif
	default:
		return mono_op_to_op_imm (opcode);
	}
}

/**
 * mono_handle_global_vregs:
 *
 *   Make vregs used in more than one bblock 'global', i.e. allocate a variable
 * for them.
 */
void
mono_handle_global_vregs (MonoCompile *cfg)
{
	gint32 *vreg_to_bb;
	MonoBasicBlock *bb;
	int i, pos;

	vreg_to_bb = mono_mempool_alloc0 (cfg->mempool, sizeof (gint32*) * cfg->next_vreg + 1);

#ifdef MONO_ARCH_SIMD_INTRINSICS
	if (cfg->uses_simd_intrinsics)
		mono_simd_simplify_indirection (cfg);
#endif

	/* Find local vregs used in more than one bb */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins = bb->code;	
		int block_num = bb->block_num;

		if (cfg->verbose_level > 2)
			printf ("\nHANDLE-GLOBAL-VREGS BLOCK %d:\n", bb->block_num);

		cfg->cbb = bb;
		for (; ins; ins = ins->next) {
			const char *spec = INS_INFO (ins->opcode);
			int regtype = 0, regindex;
			gint32 prev_bb;

			if (G_UNLIKELY (cfg->verbose_level > 2))
				mono_print_ins (ins);

			g_assert (ins->opcode >= MONO_CEE_LAST);

			for (regindex = 0; regindex < 4; regindex ++) {
				int vreg = 0;

				if (regindex == 0) {
					regtype = spec [MONO_INST_DEST];
					if (regtype == ' ')
						continue;
					vreg = ins->dreg;
				} else if (regindex == 1) {
					regtype = spec [MONO_INST_SRC1];
					if (regtype == ' ')
						continue;
					vreg = ins->sreg1;
				} else if (regindex == 2) {
					regtype = spec [MONO_INST_SRC2];
					if (regtype == ' ')
						continue;
					vreg = ins->sreg2;
				} else if (regindex == 3) {
					regtype = spec [MONO_INST_SRC3];
					if (regtype == ' ')
						continue;
					vreg = ins->sreg3;
				}

#if SIZEOF_REGISTER == 4
				/* In the LLVM case, the long opcodes are not decomposed */
				if (regtype == 'l' && !COMPILE_LLVM (cfg)) {
					/*
					 * Since some instructions reference the original long vreg,
					 * and some reference the two component vregs, it is quite hard
					 * to determine when it needs to be global. So be conservative.
					 */
					if (!get_vreg_to_inst (cfg, vreg)) {
						mono_compile_create_var_for_vreg (cfg, &mono_defaults.int64_class->byval_arg, OP_LOCAL, vreg);

						if (cfg->verbose_level > 2)
							printf ("LONG VREG R%d made global.\n", vreg);
					}

					/*
					 * Make the component vregs volatile since the optimizations can
					 * get confused otherwise.
					 */
					get_vreg_to_inst (cfg, vreg + 1)->flags |= MONO_INST_VOLATILE;
					get_vreg_to_inst (cfg, vreg + 2)->flags |= MONO_INST_VOLATILE;
				}
#endif

				g_assert (vreg != -1);

				prev_bb = vreg_to_bb [vreg];
				if (prev_bb == 0) {
					/* 0 is a valid block num */
					vreg_to_bb [vreg] = block_num + 1;
				} else if ((prev_bb != block_num + 1) && (prev_bb != -1)) {
					if (((regtype == 'i' && (vreg < MONO_MAX_IREGS))) || (regtype == 'f' && (vreg < MONO_MAX_FREGS)))
						continue;

					if (!get_vreg_to_inst (cfg, vreg)) {
						if (G_UNLIKELY (cfg->verbose_level > 2))
							printf ("VREG R%d used in BB%d and BB%d made global.\n", vreg, vreg_to_bb [vreg], block_num);

						switch (regtype) {
						case 'i':
							if (vreg_is_ref (cfg, vreg))
								mono_compile_create_var_for_vreg (cfg, &mono_defaults.object_class->byval_arg, OP_LOCAL, vreg);
							else
								mono_compile_create_var_for_vreg (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL, vreg);
							break;
						case 'l':
							mono_compile_create_var_for_vreg (cfg, &mono_defaults.int64_class->byval_arg, OP_LOCAL, vreg);
							break;
						case 'f':
							mono_compile_create_var_for_vreg (cfg, &mono_defaults.double_class->byval_arg, OP_LOCAL, vreg);
							break;
						case 'v':
							mono_compile_create_var_for_vreg (cfg, &ins->klass->byval_arg, OP_LOCAL, vreg);
							break;
						default:
							g_assert_not_reached ();
						}
					}

					/* Flag as having been used in more than one bb */
					vreg_to_bb [vreg] = -1;
				}
			}
		}
	}

	/* If a variable is used in only one bblock, convert it into a local vreg */
	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *var = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		switch (var->type) {
		case STACK_I4:
		case STACK_OBJ:
		case STACK_PTR:
		case STACK_MP:
		case STACK_VTYPE:
#if SIZEOF_REGISTER == 8
		case STACK_I8:
#endif
#if !defined(TARGET_X86)
		/* Enabling this screws up the fp stack on x86 */
		case STACK_R8:
#endif
			if (mono_arch_is_soft_float ())
				break;

			/* Arguments are implicitly global */
			/* Putting R4 vars into registers doesn't work currently */
			/* The gsharedvt vars are implicitly referenced by ldaddr opcodes, but those opcodes are only generated later */
			if ((var->opcode != OP_ARG) && (var != cfg->ret) && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)) && (vreg_to_bb [var->dreg] != -1) && (var->klass->byval_arg.type != MONO_TYPE_R4) && !cfg->disable_vreg_to_lvreg && var != cfg->gsharedvt_info_var && var != cfg->gsharedvt_locals_var && var != cfg->lmf_addr_var) {
				/* 
				 * Make that the variable's liveness interval doesn't contain a call, since
				 * that would cause the lvreg to be spilled, making the whole optimization
				 * useless.
				 */
				/* This is too slow for JIT compilation */
#if 0
				if (cfg->compile_aot && vreg_to_bb [var->dreg]) {
					MonoInst *ins;
					int def_index, call_index, ins_index;
					gboolean spilled = FALSE;

					def_index = -1;
					call_index = -1;
					ins_index = 0;
					for (ins = vreg_to_bb [var->dreg]->code; ins; ins = ins->next) {
						const char *spec = INS_INFO (ins->opcode);

						if ((spec [MONO_INST_DEST] != ' ') && (ins->dreg == var->dreg))
							def_index = ins_index;

						if (((spec [MONO_INST_SRC1] != ' ') && (ins->sreg1 == var->dreg)) ||
							((spec [MONO_INST_SRC1] != ' ') && (ins->sreg1 == var->dreg))) {
							if (call_index > def_index) {
								spilled = TRUE;
								break;
							}
						}

						if (MONO_IS_CALL (ins))
							call_index = ins_index;

						ins_index ++;
					}

					if (spilled)
						break;
				}
#endif

				if (G_UNLIKELY (cfg->verbose_level > 2))
					printf ("CONVERTED R%d(%d) TO VREG.\n", var->dreg, vmv->idx);
				var->flags |= MONO_INST_IS_DEAD;
				cfg->vreg_to_inst [var->dreg] = NULL;
			}
			break;
		}
	}

	/* 
	 * Compress the varinfo and vars tables so the liveness computation is faster and
	 * takes up less space.
	 */
	pos = 0;
	for (i = 0; i < cfg->num_varinfo; ++i) {
		MonoInst *var = cfg->varinfo [i];
		if (pos < i && cfg->locals_start == i)
			cfg->locals_start = pos;
		if (!(var->flags & MONO_INST_IS_DEAD)) {
			if (pos < i) {
				cfg->varinfo [pos] = cfg->varinfo [i];
				cfg->varinfo [pos]->inst_c0 = pos;
				memcpy (&cfg->vars [pos], &cfg->vars [i], sizeof (MonoMethodVar));
				cfg->vars [pos].idx = pos;
#if SIZEOF_REGISTER == 4
				if (cfg->varinfo [pos]->type == STACK_I8) {
					/* Modify the two component vars too */
					MonoInst *var1;

					var1 = get_vreg_to_inst (cfg, cfg->varinfo [pos]->dreg + 1);
					var1->inst_c0 = pos;
					var1 = get_vreg_to_inst (cfg, cfg->varinfo [pos]->dreg + 2);
					var1->inst_c0 = pos;
				}
#endif
			}
			pos ++;
		}
	}
	cfg->num_varinfo = pos;
	if (cfg->locals_start > cfg->num_varinfo)
		cfg->locals_start = cfg->num_varinfo;
}

/**
 * mono_spill_global_vars:
 *
 *   Generate spill code for variables which are not allocated to registers, 
 * and replace vregs with their allocated hregs. *need_local_opts is set to TRUE if
 * code is generated which could be optimized by the local optimization passes.
 */
void
mono_spill_global_vars (MonoCompile *cfg, gboolean *need_local_opts)
{
	MonoBasicBlock *bb;
	char spec2 [16];
	int orig_next_vreg;
	guint32 *vreg_to_lvreg;
	guint32 *lvregs;
	guint32 i, lvregs_len;
	gboolean dest_has_lvreg = FALSE;
	guint32 stacktypes [128];
	MonoInst **live_range_start, **live_range_end;
	MonoBasicBlock **live_range_start_bb, **live_range_end_bb;
	int *gsharedvt_vreg_to_idx = NULL;

	*need_local_opts = FALSE;

	memset (spec2, 0, sizeof (spec2));

	/* FIXME: Move this function to mini.c */
	stacktypes ['i'] = STACK_PTR;
	stacktypes ['l'] = STACK_I8;
	stacktypes ['f'] = STACK_R8;
#ifdef MONO_ARCH_SIMD_INTRINSICS
	stacktypes ['x'] = STACK_VTYPE;
#endif

#if SIZEOF_REGISTER == 4
	/* Create MonoInsts for longs */
	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];

		if ((ins->opcode != OP_REGVAR) && !(ins->flags & MONO_INST_IS_DEAD)) {
			switch (ins->type) {
			case STACK_R8:
			case STACK_I8: {
				MonoInst *tree;

				if (ins->type == STACK_R8 && !COMPILE_SOFT_FLOAT (cfg))
					break;

				g_assert (ins->opcode == OP_REGOFFSET);

				tree = get_vreg_to_inst (cfg, ins->dreg + 1);
				g_assert (tree);
				tree->opcode = OP_REGOFFSET;
				tree->inst_basereg = ins->inst_basereg;
				tree->inst_offset = ins->inst_offset + MINI_LS_WORD_OFFSET;

				tree = get_vreg_to_inst (cfg, ins->dreg + 2);
				g_assert (tree);
				tree->opcode = OP_REGOFFSET;
				tree->inst_basereg = ins->inst_basereg;
				tree->inst_offset = ins->inst_offset + MINI_MS_WORD_OFFSET;
				break;
			}
			default:
				break;
			}
		}
	}
#endif

	if (cfg->compute_gc_maps) {
		/* registers need liveness info even for !non refs */
		for (i = 0; i < cfg->num_varinfo; i++) {
			MonoInst *ins = cfg->varinfo [i];

			if (ins->opcode == OP_REGVAR)
				ins->flags |= MONO_INST_GC_TRACK;
		}
	}

	if (cfg->gsharedvt) {
		gsharedvt_vreg_to_idx = mono_mempool_alloc0 (cfg->mempool, sizeof (int) * cfg->next_vreg);

		for (i = 0; i < cfg->num_varinfo; ++i) {
			MonoInst *ins = cfg->varinfo [i];
			int idx;

			if (mini_is_gsharedvt_variable_type (cfg, ins->inst_vtype)) {
				if (i >= cfg->locals_start) {
					/* Local */
					idx = get_gsharedvt_info_slot (cfg, ins->inst_vtype, MONO_RGCTX_INFO_LOCAL_OFFSET);
					gsharedvt_vreg_to_idx [ins->dreg] = idx + 1;
					ins->opcode = OP_GSHAREDVT_LOCAL;
					ins->inst_imm = idx;
				} else {
					/* Arg */
					gsharedvt_vreg_to_idx [ins->dreg] = -1;
					ins->opcode = OP_GSHAREDVT_ARG_REGOFFSET;
				}
			}
		}
	}
		
	/* FIXME: widening and truncation */

	/*
	 * As an optimization, when a variable allocated to the stack is first loaded into 
	 * an lvreg, we will remember the lvreg and use it the next time instead of loading
	 * the variable again.
	 */
	orig_next_vreg = cfg->next_vreg;
	vreg_to_lvreg = mono_mempool_alloc0 (cfg->mempool, sizeof (guint32) * cfg->next_vreg);
	lvregs = mono_mempool_alloc (cfg->mempool, sizeof (guint32) * 1024);
	lvregs_len = 0;

	/* 
	 * These arrays contain the first and last instructions accessing a given
	 * variable.
	 * Since we emit bblocks in the same order we process them here, and we
	 * don't split live ranges, these will precisely describe the live range of
	 * the variable, i.e. the instruction range where a valid value can be found
	 * in the variables location.
	 * The live range is computed using the liveness info computed by the liveness pass.
	 * We can't use vmv->range, since that is an abstract live range, and we need
	 * one which is instruction precise.
	 * FIXME: Variables used in out-of-line bblocks have a hole in their live range.
	 */
	/* FIXME: Only do this if debugging info is requested */
	live_range_start = g_new0 (MonoInst*, cfg->next_vreg);
	live_range_end = g_new0 (MonoInst*, cfg->next_vreg);
	live_range_start_bb = g_new (MonoBasicBlock*, cfg->next_vreg);
	live_range_end_bb = g_new (MonoBasicBlock*, cfg->next_vreg);
	
	/* Add spill loads/stores */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;

		if (cfg->verbose_level > 2)
			printf ("\nSPILL BLOCK %d:\n", bb->block_num);

		/* Clear vreg_to_lvreg array */
		for (i = 0; i < lvregs_len; i++)
			vreg_to_lvreg [lvregs [i]] = 0;
		lvregs_len = 0;

		cfg->cbb = bb;
		MONO_BB_FOR_EACH_INS (bb, ins) {
			const char *spec = INS_INFO (ins->opcode);
			int regtype, srcindex, sreg, tmp_reg, prev_dreg, num_sregs;
			gboolean store, no_lvreg;
			int sregs [MONO_MAX_SRC_REGS];

			if (G_UNLIKELY (cfg->verbose_level > 2))
				mono_print_ins (ins);

			if (ins->opcode == OP_NOP)
				continue;

			/* 
			 * We handle LDADDR here as well, since it can only be decomposed
			 * when variable addresses are known.
			 */
			if (ins->opcode == OP_LDADDR) {
				MonoInst *var = ins->inst_p0;

				if (var->opcode == OP_VTARG_ADDR) {
					/* Happens on SPARC/S390 where vtypes are passed by reference */
					MonoInst *vtaddr = var->inst_left;
					if (vtaddr->opcode == OP_REGVAR) {
						ins->opcode = OP_MOVE;
						ins->sreg1 = vtaddr->dreg;
					}
					else if (var->inst_left->opcode == OP_REGOFFSET) {
						ins->opcode = OP_LOAD_MEMBASE;
						ins->inst_basereg = vtaddr->inst_basereg;
						ins->inst_offset = vtaddr->inst_offset;
					} else
						NOT_IMPLEMENTED;
				} else if (cfg->gsharedvt && gsharedvt_vreg_to_idx [var->dreg] < 0) {
					/* gsharedvt arg passed by ref */
					g_assert (var->opcode == OP_GSHAREDVT_ARG_REGOFFSET);

					ins->opcode = OP_LOAD_MEMBASE;
					ins->inst_basereg = var->inst_basereg;
					ins->inst_offset = var->inst_offset;
				} else if (cfg->gsharedvt && gsharedvt_vreg_to_idx [var->dreg]) {
					MonoInst *load, *load2, *load3;
					int idx = gsharedvt_vreg_to_idx [var->dreg] - 1;
					int reg1, reg2, reg3;
					MonoInst *info_var = cfg->gsharedvt_info_var;
					MonoInst *locals_var = cfg->gsharedvt_locals_var;

					/*
					 * gsharedvt local.
					 * Compute the address of the local as gsharedvt_locals_var + gsharedvt_info_var->locals_offsets [idx].
					 */

					g_assert (var->opcode == OP_GSHAREDVT_LOCAL);

					g_assert (info_var);
					g_assert (locals_var);

					/* Mark the instruction used to compute the locals var as used */
					cfg->gsharedvt_locals_var_ins = NULL;

					/* Load the offset */
					if (info_var->opcode == OP_REGOFFSET) {
						reg1 = alloc_ireg (cfg);
						NEW_LOAD_MEMBASE (cfg, load, OP_LOAD_MEMBASE, reg1, info_var->inst_basereg, info_var->inst_offset);
					} else if (info_var->opcode == OP_REGVAR) {
						load = NULL;
						reg1 = info_var->dreg;
					} else {
						g_assert_not_reached ();
					}
					reg2 = alloc_ireg (cfg);
					NEW_LOAD_MEMBASE (cfg, load2, OP_LOADI4_MEMBASE, reg2, reg1, MONO_STRUCT_OFFSET (MonoGSharedVtMethodRuntimeInfo, entries) + (idx * sizeof (gpointer)));
					/* Load the locals area address */
					reg3 = alloc_ireg (cfg);
					if (locals_var->opcode == OP_REGOFFSET) {
						NEW_LOAD_MEMBASE (cfg, load3, OP_LOAD_MEMBASE, reg3, locals_var->inst_basereg, locals_var->inst_offset);
					} else if (locals_var->opcode == OP_REGVAR) {
						NEW_UNALU (cfg, load3, OP_MOVE, reg3, locals_var->dreg);
					} else {
						g_assert_not_reached ();
					}
					/* Compute the address */
					ins->opcode = OP_PADD;
					ins->sreg1 = reg3;
					ins->sreg2 = reg2;

					mono_bblock_insert_before_ins (bb, ins, load3);
					mono_bblock_insert_before_ins (bb, load3, load2);
					if (load)
						mono_bblock_insert_before_ins (bb, load2, load);
				} else {
					g_assert (var->opcode == OP_REGOFFSET);

					ins->opcode = OP_ADD_IMM;
					ins->sreg1 = var->inst_basereg;
					ins->inst_imm = var->inst_offset;
				}

				*need_local_opts = TRUE;
				spec = INS_INFO (ins->opcode);
			}

			if (ins->opcode < MONO_CEE_LAST) {
				mono_print_ins (ins);
				g_assert_not_reached ();
			}

			/*
			 * Store opcodes have destbasereg in the dreg, but in reality, it is an
			 * src register.
			 * FIXME:
			 */
			if (MONO_IS_STORE_MEMBASE (ins)) {
				tmp_reg = ins->dreg;
				ins->dreg = ins->sreg2;
				ins->sreg2 = tmp_reg;
				store = TRUE;

				spec2 [MONO_INST_DEST] = ' ';
				spec2 [MONO_INST_SRC1] = spec [MONO_INST_SRC1];
				spec2 [MONO_INST_SRC2] = spec [MONO_INST_DEST];
				spec2 [MONO_INST_SRC3] = ' ';
				spec = spec2;
			} else if (MONO_IS_STORE_MEMINDEX (ins))
				g_assert_not_reached ();
			else
				store = FALSE;
			no_lvreg = FALSE;

			if (G_UNLIKELY (cfg->verbose_level > 2)) {
				printf ("\t %.3s %d", spec, ins->dreg);
				num_sregs = mono_inst_get_src_registers (ins, sregs);
				for (srcindex = 0; srcindex < num_sregs; ++srcindex)
					printf (" %d", sregs [srcindex]);
				printf ("\n");
			}

			/***************/
			/*    DREG     */
			/***************/
			regtype = spec [MONO_INST_DEST];
			g_assert (((ins->dreg == -1) && (regtype == ' ')) || ((ins->dreg != -1) && (regtype != ' ')));
			prev_dreg = -1;

			if ((ins->dreg != -1) && get_vreg_to_inst (cfg, ins->dreg)) {
				MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);
				MonoInst *store_ins;
				int store_opcode;
				MonoInst *def_ins = ins;
				int dreg = ins->dreg; /* The original vreg */

				store_opcode = mono_type_to_store_membase (cfg, var->inst_vtype);

				if (var->opcode == OP_REGVAR) {
					ins->dreg = var->dreg;
				} else if ((ins->dreg == ins->sreg1) && (spec [MONO_INST_DEST] == 'i') && (spec [MONO_INST_SRC1] == 'i') && !vreg_to_lvreg [ins->dreg] && (op_to_op_dest_membase (store_opcode, ins->opcode) != -1)) {
					/* 
					 * Instead of emitting a load+store, use a _membase opcode.
					 */
					g_assert (var->opcode == OP_REGOFFSET);
					if (ins->opcode == OP_MOVE) {
						NULLIFY_INS (ins);
						def_ins = NULL;
					} else {
						ins->opcode = op_to_op_dest_membase (store_opcode, ins->opcode);
						ins->inst_basereg = var->inst_basereg;
						ins->inst_offset = var->inst_offset;
						ins->dreg = -1;
					}
					spec = INS_INFO (ins->opcode);
				} else {
					guint32 lvreg;

					g_assert (var->opcode == OP_REGOFFSET);

					prev_dreg = ins->dreg;

					/* Invalidate any previous lvreg for this vreg */
					vreg_to_lvreg [ins->dreg] = 0;

					lvreg = 0;

					if (COMPILE_SOFT_FLOAT (cfg) && store_opcode == OP_STORER8_MEMBASE_REG) {
						regtype = 'l';
						store_opcode = OP_STOREI8_MEMBASE_REG;
					}

					ins->dreg = alloc_dreg (cfg, stacktypes [regtype]);

#if SIZEOF_REGISTER != 8
					if (regtype == 'l') {
						NEW_STORE_MEMBASE (cfg, store_ins, OP_STOREI4_MEMBASE_REG, var->inst_basereg, var->inst_offset + MINI_LS_WORD_OFFSET, ins->dreg + 1);
						mono_bblock_insert_after_ins (bb, ins, store_ins);
						NEW_STORE_MEMBASE (cfg, store_ins, OP_STOREI4_MEMBASE_REG, var->inst_basereg, var->inst_offset + MINI_MS_WORD_OFFSET, ins->dreg + 2);
						mono_bblock_insert_after_ins (bb, ins, store_ins);
						def_ins = store_ins;
					}
					else
#endif
					{
						g_assert (store_opcode != OP_STOREV_MEMBASE);

						/* Try to fuse the store into the instruction itself */
						/* FIXME: Add more instructions */
						if (!lvreg && ((ins->opcode == OP_ICONST) || ((ins->opcode == OP_I8CONST) && (ins->inst_c0 == 0)))) {
							ins->opcode = store_membase_reg_to_store_membase_imm (store_opcode);
							ins->inst_imm = ins->inst_c0;
							ins->inst_destbasereg = var->inst_basereg;
							ins->inst_offset = var->inst_offset;
							spec = INS_INFO (ins->opcode);
						} else if (!lvreg && ((ins->opcode == OP_MOVE) || (ins->opcode == OP_FMOVE) || (ins->opcode == OP_LMOVE))) {
							ins->opcode = store_opcode;
							ins->inst_destbasereg = var->inst_basereg;
							ins->inst_offset = var->inst_offset;

							no_lvreg = TRUE;

							tmp_reg = ins->dreg;
							ins->dreg = ins->sreg2;
							ins->sreg2 = tmp_reg;
							store = TRUE;

							spec2 [MONO_INST_DEST] = ' ';
							spec2 [MONO_INST_SRC1] = spec [MONO_INST_SRC1];
							spec2 [MONO_INST_SRC2] = spec [MONO_INST_DEST];
							spec2 [MONO_INST_SRC3] = ' ';
							spec = spec2;
						} else if (!lvreg && (op_to_op_store_membase (store_opcode, ins->opcode) != -1)) {
							// FIXME: The backends expect the base reg to be in inst_basereg
							ins->opcode = op_to_op_store_membase (store_opcode, ins->opcode);
							ins->dreg = -1;
							ins->inst_basereg = var->inst_basereg;
							ins->inst_offset = var->inst_offset;
							spec = INS_INFO (ins->opcode);
						} else {
							/* printf ("INS: "); mono_print_ins (ins); */
							/* Create a store instruction */
							NEW_STORE_MEMBASE (cfg, store_ins, store_opcode, var->inst_basereg, var->inst_offset, ins->dreg);

							/* Insert it after the instruction */
							mono_bblock_insert_after_ins (bb, ins, store_ins);

							def_ins = store_ins;

							/* 
							 * We can't assign ins->dreg to var->dreg here, since the
							 * sregs could use it. So set a flag, and do it after
							 * the sregs.
							 */
							if ((!MONO_ARCH_USE_FPSTACK || ((store_opcode != OP_STORER8_MEMBASE_REG) && (store_opcode != OP_STORER4_MEMBASE_REG))) && !((var)->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)))
								dest_has_lvreg = TRUE;
						}
					}
				}

				if (def_ins && !live_range_start [dreg]) {
					live_range_start [dreg] = def_ins;
					live_range_start_bb [dreg] = bb;
				}

				if (cfg->compute_gc_maps && def_ins && (var->flags & MONO_INST_GC_TRACK)) {
					MonoInst *tmp;

					MONO_INST_NEW (cfg, tmp, OP_GC_LIVENESS_DEF);
					tmp->inst_c1 = dreg;
					mono_bblock_insert_after_ins (bb, def_ins, tmp);
				}
			}

			/************/
			/*  SREGS   */
			/************/
			num_sregs = mono_inst_get_src_registers (ins, sregs);
			for (srcindex = 0; srcindex < 3; ++srcindex) {
				regtype = spec [MONO_INST_SRC1 + srcindex];
				sreg = sregs [srcindex];

				g_assert (((sreg == -1) && (regtype == ' ')) || ((sreg != -1) && (regtype != ' ')));
				if ((sreg != -1) && get_vreg_to_inst (cfg, sreg)) {
					MonoInst *var = get_vreg_to_inst (cfg, sreg);
					MonoInst *use_ins = ins;
					MonoInst *load_ins;
					guint32 load_opcode;

					if (var->opcode == OP_REGVAR) {
						sregs [srcindex] = var->dreg;
						//mono_inst_set_src_registers (ins, sregs);
						live_range_end [sreg] = use_ins;
						live_range_end_bb [sreg] = bb;

						if (cfg->compute_gc_maps && var->dreg < orig_next_vreg && (var->flags & MONO_INST_GC_TRACK)) {
							MonoInst *tmp;

							MONO_INST_NEW (cfg, tmp, OP_GC_LIVENESS_USE);
							/* var->dreg is a hreg */
							tmp->inst_c1 = sreg;
							mono_bblock_insert_after_ins (bb, ins, tmp);
						}

						continue;
					}

					g_assert (var->opcode == OP_REGOFFSET);
						
					load_opcode = mono_type_to_load_membase (cfg, var->inst_vtype);

					g_assert (load_opcode != OP_LOADV_MEMBASE);

					if (vreg_to_lvreg [sreg]) {
						g_assert (vreg_to_lvreg [sreg] != -1);

						/* The variable is already loaded to an lvreg */
						if (G_UNLIKELY (cfg->verbose_level > 2))
							printf ("\t\tUse lvreg R%d for R%d.\n", vreg_to_lvreg [sreg], sreg);
						sregs [srcindex] = vreg_to_lvreg [sreg];
						//mono_inst_set_src_registers (ins, sregs);
						continue;
					}

					/* Try to fuse the load into the instruction */
					if ((srcindex == 0) && (op_to_op_src1_membase (load_opcode, ins->opcode) != -1)) {
						ins->opcode = op_to_op_src1_membase (load_opcode, ins->opcode);
						sregs [0] = var->inst_basereg;
						//mono_inst_set_src_registers (ins, sregs);
						ins->inst_offset = var->inst_offset;
					} else if ((srcindex == 1) && (op_to_op_src2_membase (load_opcode, ins->opcode) != -1)) {
						ins->opcode = op_to_op_src2_membase (load_opcode, ins->opcode);
						sregs [1] = var->inst_basereg;
						//mono_inst_set_src_registers (ins, sregs);
						ins->inst_offset = var->inst_offset;
					} else {
						if (MONO_IS_REAL_MOVE (ins)) {
							ins->opcode = OP_NOP;
							sreg = ins->dreg;
						} else {
							//printf ("%d ", srcindex); mono_print_ins (ins);

							sreg = alloc_dreg (cfg, stacktypes [regtype]);

							if ((!MONO_ARCH_USE_FPSTACK || ((load_opcode != OP_LOADR8_MEMBASE) && (load_opcode != OP_LOADR4_MEMBASE))) && !((var)->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)) && !no_lvreg) {
								if (var->dreg == prev_dreg) {
									/*
									 * sreg refers to the value loaded by the load
									 * emitted below, but we need to use ins->dreg
									 * since it refers to the store emitted earlier.
									 */
									sreg = ins->dreg;
								}
								g_assert (sreg != -1);
								vreg_to_lvreg [var->dreg] = sreg;
								g_assert (lvregs_len < 1024);
								lvregs [lvregs_len ++] = var->dreg;
							}
						}

						sregs [srcindex] = sreg;
						//mono_inst_set_src_registers (ins, sregs);

#if SIZEOF_REGISTER != 8
						if (regtype == 'l') {
							NEW_LOAD_MEMBASE (cfg, load_ins, OP_LOADI4_MEMBASE, sreg + 2, var->inst_basereg, var->inst_offset + MINI_MS_WORD_OFFSET);
							mono_bblock_insert_before_ins (bb, ins, load_ins);
							NEW_LOAD_MEMBASE (cfg, load_ins, OP_LOADI4_MEMBASE, sreg + 1, var->inst_basereg, var->inst_offset + MINI_LS_WORD_OFFSET);
							mono_bblock_insert_before_ins (bb, ins, load_ins);
							use_ins = load_ins;
						}
						else
#endif
						{
#if SIZEOF_REGISTER == 4
							g_assert (load_opcode != OP_LOADI8_MEMBASE);
#endif
							NEW_LOAD_MEMBASE (cfg, load_ins, load_opcode, sreg, var->inst_basereg, var->inst_offset);
							mono_bblock_insert_before_ins (bb, ins, load_ins);
							use_ins = load_ins;
						}
					}

					if (var->dreg < orig_next_vreg) {
						live_range_end [var->dreg] = use_ins;
						live_range_end_bb [var->dreg] = bb;
					}

					if (cfg->compute_gc_maps && var->dreg < orig_next_vreg && (var->flags & MONO_INST_GC_TRACK)) {
						MonoInst *tmp;

						MONO_INST_NEW (cfg, tmp, OP_GC_LIVENESS_USE);
						tmp->inst_c1 = var->dreg;
						mono_bblock_insert_after_ins (bb, ins, tmp);
					}
				}
			}
			mono_inst_set_src_registers (ins, sregs);

			if (dest_has_lvreg) {
				g_assert (ins->dreg != -1);
				vreg_to_lvreg [prev_dreg] = ins->dreg;
				g_assert (lvregs_len < 1024);
				lvregs [lvregs_len ++] = prev_dreg;
				dest_has_lvreg = FALSE;
			}

			if (store) {
				tmp_reg = ins->dreg;
				ins->dreg = ins->sreg2;
				ins->sreg2 = tmp_reg;
			}

			if (MONO_IS_CALL (ins)) {
				/* Clear vreg_to_lvreg array */
				for (i = 0; i < lvregs_len; i++)
					vreg_to_lvreg [lvregs [i]] = 0;
				lvregs_len = 0;
			} else if (ins->opcode == OP_NOP) {
				ins->dreg = -1;
				MONO_INST_NULLIFY_SREGS (ins);
			}

			if (cfg->verbose_level > 2)
				mono_print_ins_index (1, ins);
		}

		/* Extend the live range based on the liveness info */
		if (cfg->compute_precise_live_ranges && bb->live_out_set && bb->code) {
			for (i = 0; i < cfg->num_varinfo; i ++) {
				MonoMethodVar *vi = MONO_VARINFO (cfg, i);

				if (vreg_is_volatile (cfg, vi->vreg))
					/* The liveness info is incomplete */
					continue;

				if (mono_bitset_test_fast (bb->live_in_set, i) && !live_range_start [vi->vreg]) {
					/* Live from at least the first ins of this bb */
					live_range_start [vi->vreg] = bb->code;
					live_range_start_bb [vi->vreg] = bb;
				}

				if (mono_bitset_test_fast (bb->live_out_set, i)) {
					/* Live at least until the last ins of this bb */
					live_range_end [vi->vreg] = bb->last_ins;
					live_range_end_bb [vi->vreg] = bb;
				}
			}
		}
	}
	
#ifdef MONO_ARCH_HAVE_LIVERANGE_OPS
	/*
	 * Emit LIVERANGE_START/LIVERANGE_END opcodes, the backend will implement them
	 * by storing the current native offset into MonoMethodVar->live_range_start/end.
	 */
	if (cfg->compute_precise_live_ranges && cfg->comp_done & MONO_COMP_LIVENESS) {
		for (i = 0; i < cfg->num_varinfo; ++i) {
			int vreg = MONO_VARINFO (cfg, i)->vreg;
			MonoInst *ins;

			if (live_range_start [vreg]) {
				MONO_INST_NEW (cfg, ins, OP_LIVERANGE_START);
				ins->inst_c0 = i;
				ins->inst_c1 = vreg;
				mono_bblock_insert_after_ins (live_range_start_bb [vreg], live_range_start [vreg], ins);
			}
			if (live_range_end [vreg]) {
				MONO_INST_NEW (cfg, ins, OP_LIVERANGE_END);
				ins->inst_c0 = i;
				ins->inst_c1 = vreg;
				if (live_range_end [vreg] == live_range_end_bb [vreg]->last_ins)
					mono_add_ins_to_end (live_range_end_bb [vreg], ins);
				else
					mono_bblock_insert_after_ins (live_range_end_bb [vreg], live_range_end [vreg], ins);
			}
		}
	}
#endif

	if (cfg->gsharedvt_locals_var_ins) {
		/* Nullify if unused */
		cfg->gsharedvt_locals_var_ins->opcode = OP_PCONST;
		cfg->gsharedvt_locals_var_ins->inst_imm = 0;
	}

	g_free (live_range_start);
	g_free (live_range_end);
	g_free (live_range_start_bb);
	g_free (live_range_end_bb);
}

/**
 * FIXME:
 * - use 'iadd' instead of 'int_add'
 * - handling ovf opcodes: decompose in method_to_ir.
 * - unify iregs/fregs
 *   -> partly done, the missing parts are:
 *   - a more complete unification would involve unifying the hregs as well, so
 *     code wouldn't need if (fp) all over the place. but that would mean the hregs
 *     would no longer map to the machine hregs, so the code generators would need to
 *     be modified. Also, on ia64 for example, niregs + nfregs > 256 -> bitmasks
 *     wouldn't work any more. Duplicating the code in mono_local_regalloc () into
 *     fp/non-fp branches speeds it up by about 15%.
 * - use sext/zext opcodes instead of shifts
 * - add OP_ICALL
 * - get rid of TEMPLOADs if possible and use vregs instead
 * - clean up usage of OP_P/OP_ opcodes
 * - cleanup usage of DUMMY_USE
 * - cleanup the setting of ins->type for MonoInst's which are pushed on the 
 *   stack
 * - set the stack type and allocate a dreg in the EMIT_NEW macros
 * - get rid of all the <foo>2 stuff when the new JIT is ready.
 * - make sure handle_stack_args () is called before the branch is emitted
 * - when the new IR is done, get rid of all unused stuff
 * - COMPARE/BEQ as separate instructions or unify them ?
 *   - keeping them separate allows specialized compare instructions like
 *     compare_imm, compare_membase
 *   - most back ends unify fp compare+branch, fp compare+ceq
 * - integrate mono_save_args into inline_method
 * - get rid of the empty bblocks created by MONO_EMIT_NEW_BRACH_BLOCK2
 * - handle long shift opts on 32 bit platforms somehow: they require 
 *   3 sregs (2 for arg1 and 1 for arg2)
 * - make byref a 'normal' type.
 * - use vregs for bb->out_stacks if possible, handle_global_vreg will make them a
 *   variable if needed.
 * - do not start a new IL level bblock when cfg->cbb is changed by a function call
 *   like inline_method.
 * - remove inlining restrictions
 * - fix LNEG and enable cfold of INEG
 * - generalize x86 optimizations like ldelema as a peephole optimization
 * - add store_mem_imm for amd64
 * - optimize the loading of the interruption flag in the managed->native wrappers
 * - avoid special handling of OP_NOP in passes
 * - move code inserting instructions into one function/macro.
 * - try a coalescing phase after liveness analysis
 * - add float -> vreg conversion + local optimizations on !x86
 * - figure out how to handle decomposed branches during optimizations, ie.
 *   compare+branch, op_jump_table+op_br etc.
 * - promote RuntimeXHandles to vregs
 * - vtype cleanups:
 *   - add a NEW_VARLOADA_VREG macro
 * - the vtype optimizations are blocked by the LDADDR opcodes generated for 
 *   accessing vtype fields.
 * - get rid of I8CONST on 64 bit platforms
 * - dealing with the increase in code size due to branches created during opcode
 *   decomposition:
 *   - use extended basic blocks
 *     - all parts of the JIT
 *     - handle_global_vregs () && local regalloc
 *   - avoid introducing global vregs during decomposition, like 'vtable' in isinst
 * - sources of increase in code size:
 *   - vtypes
 *   - long compares
 *   - isinst and castclass
 *   - lvregs not allocated to global registers even if used multiple times
 * - call cctors outside the JIT, to make -v output more readable and JIT timings more
 *   meaningful.
 * - check for fp stack leakage in other opcodes too. (-> 'exceptions' optimization)
 * - add all micro optimizations from the old JIT
 * - put tree optimizations into the deadce pass
 * - decompose op_start_handler/op_endfilter/op_endfinally earlier using an arch
 *   specific function.
 * - unify the float comparison opcodes with the other comparison opcodes, i.e.
 *   fcompare + branchCC.
 * - create a helper function for allocating a stack slot, taking into account 
 *   MONO_CFG_HAS_SPILLUP.
 * - merge r68207.
 * - merge the ia64 switch changes.
 * - optimize mono_regstate2_alloc_int/float.
 * - fix the pessimistic handling of variables accessed in exception handler blocks.
 * - need to write a tree optimization pass, but the creation of trees is difficult, i.e.
 *   parts of the tree could be separated by other instructions, killing the tree
 *   arguments, or stores killing loads etc. Also, should we fold loads into other
 *   instructions if the result of the load is used multiple times ?
 * - make the REM_IMM optimization in mini-x86.c arch-independent.
 * - LAST MERGE: 108395.
 * - when returning vtypes in registers, generate IR and append it to the end of the
 *   last bb instead of doing it in the epilog.
 * - change the store opcodes so they use sreg1 instead of dreg to store the base register.
 */

/*

NOTES
-----

- When to decompose opcodes:
  - earlier: this makes some optimizations hard to implement, since the low level IR
  no longer contains the neccessary information. But it is easier to do.
  - later: harder to implement, enables more optimizations.
- Branches inside bblocks:
  - created when decomposing complex opcodes. 
    - branches to another bblock: harmless, but not tracked by the branch 
      optimizations, so need to branch to a label at the start of the bblock.
    - branches to inside the same bblock: very problematic, trips up the local
      reg allocator. Can be fixed by spitting the current bblock, but that is a
      complex operation, since some local vregs can become global vregs etc.
- Local/global vregs:
  - local vregs: temporary vregs used inside one bblock. Assigned to hregs by the
    local register allocator.
  - global vregs: used in more than one bblock. Have an associated MonoMethodVar
    structure, created by mono_create_var (). Assigned to hregs or the stack by
    the global register allocator.
- When to do optimizations like alu->alu_imm:
  - earlier -> saves work later on since the IR will be smaller/simpler
  - later -> can work on more instructions
- Handling of valuetypes:
  - When a vtype is pushed on the stack, a new temporary is created, an 
    instruction computing its address (LDADDR) is emitted and pushed on
    the stack. Need to optimize cases when the vtype is used immediately as in
    argument passing, stloc etc.
- Instead of the to_end stuff in the old JIT, simply call the function handling
  the values on the stack before emitting the last instruction of the bb.
*/

#endif /* DISABLE_JIT */
