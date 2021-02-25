/**
 * \file
 * The new Mono code generator.
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc.
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <math.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#include <mono/utils/memcheck.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/attrdefs.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-path.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/mono-hwcap.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/unlocked.h>
#include <mono/utils/mono-time.h>

#include "mini.h"
#include "seq-points.h"
#include <string.h>
#include <ctype.h>
#include "trace.h"
#include "ir-emit.h"

#include "jit-icalls.h"

#include "mini-gc.h"
#include "debugger-agent.h"
#include "llvm-runtime.h"
#include "mini-llvm.h"
#include "lldb.h"
#include "aot-runtime.h"
#include "mini-runtime.h"

MonoCallSpec *mono_jit_trace_calls;
MonoMethodDesc *mono_inject_async_exc_method;
int mono_inject_async_exc_pos;
MonoMethodDesc *mono_break_at_bb_method;
int mono_break_at_bb_bb_num;
gboolean mono_do_x86_stack_align = TRUE;
gboolean mono_using_xdebug;

/* Counters */
static guint32 discarded_code;
static gint64 discarded_jit_time;

#define mono_jit_lock() mono_os_mutex_lock (&jit_mutex)
#define mono_jit_unlock() mono_os_mutex_unlock (&jit_mutex)
static mono_mutex_t jit_mutex;

#ifndef DISABLE_JIT
static guint32 jinfo_try_holes_size;
static MonoBackend *current_backend;

gpointer
mono_realloc_native_code (MonoCompile *cfg)
{
	return g_realloc (cfg->native_code, cfg->code_size);
}

typedef struct {
	MonoExceptionClause *clause;
	MonoBasicBlock *basic_block;
	int start_offset;
} TryBlockHole;

/**
 * mono_emit_unwind_op:
 *
 *   Add an unwind op with the given parameters for the list of unwind ops stored in
 * cfg->unwind_ops.
 */
void
mono_emit_unwind_op (MonoCompile *cfg, int when, int tag, int reg, int val)
{
	MonoUnwindOp *op = (MonoUnwindOp *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoUnwindOp));

	op->op = tag;
	op->reg = reg;
	op->val = val;
	op->when = when;
	
	cfg->unwind_ops = g_slist_append_mempool (cfg->mempool, cfg->unwind_ops, op);
	if (cfg->verbose_level > 1) {
		switch (tag) {
		case DW_CFA_def_cfa:
			printf ("CFA: [%x] def_cfa: %s+0x%x\n", when, mono_arch_regname (reg), val);
			break;
		case DW_CFA_def_cfa_register:
			printf ("CFA: [%x] def_cfa_reg: %s\n", when, mono_arch_regname (reg));
			break;
		case DW_CFA_def_cfa_offset:
			printf ("CFA: [%x] def_cfa_offset: 0x%x\n", when, val);
			break;
		case DW_CFA_offset:
			printf ("CFA: [%x] offset: %s at cfa-0x%x\n", when, mono_arch_regname (reg), -val);
			break;
		}
	}
}

/**
 * mono_unlink_bblock:
 *
 *   Unlink two basic blocks.
 */
void
mono_unlink_bblock (MonoCompile *cfg, MonoBasicBlock *from, MonoBasicBlock* to)
{
	int i, pos;
	gboolean found;

	found = FALSE;
	for (i = 0; i < from->out_count; ++i) {
		if (to == from->out_bb [i]) {
			found = TRUE;
			break;
		}
	}
	if (found) {
		pos = 0;
		for (i = 0; i < from->out_count; ++i) {
			if (from->out_bb [i] != to)
				from->out_bb [pos ++] = from->out_bb [i];
		}
		g_assert (pos == from->out_count - 1);
		from->out_count--;
	}

	found = FALSE;
	for (i = 0; i < to->in_count; ++i) {
		if (from == to->in_bb [i]) {
			found = TRUE;
			break;
		}
	}
	if (found) {
		pos = 0;
		for (i = 0; i < to->in_count; ++i) {
			if (to->in_bb [i] != from)
				to->in_bb [pos ++] = to->in_bb [i];
		}
		g_assert (pos == to->in_count - 1);
		to->in_count--;
	}
}

/*
 * mono_bblocks_linked:
 *
 *   Return whenever BB1 and BB2 are linked in the CFG.
 */
gboolean
mono_bblocks_linked (MonoBasicBlock *bb1, MonoBasicBlock *bb2)
{
	int i;

	for (i = 0; i < bb1->out_count; ++i) {
		if (bb1->out_bb [i] == bb2)
			return TRUE;
	}

	return FALSE;
}

static int
mono_find_block_region_notry (MonoCompile *cfg, int offset)
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
	}

	return -1;
}

/*
 * mono_get_block_region_notry:
 *
 *   Return the region corresponding to REGION, ignoring try clauses nested inside
 * finally clauses.
 */
int
mono_get_block_region_notry (MonoCompile *cfg, int region)
{
	if ((region & (0xf << 4)) == MONO_REGION_TRY) {
		MonoMethodHeader *header = cfg->header;
		
		/*
		 * This can happen if a try clause is nested inside a finally clause.
		 */
		int clause_index = (region >> 8) - 1;
		g_assert (clause_index >= 0 && clause_index < header->num_clauses);
		
		region = mono_find_block_region_notry (cfg, header->clauses [clause_index].try_offset);
	}

	return region;
}

MonoInst *
mono_find_spvar_for_region (MonoCompile *cfg, int region)
{
	region = mono_get_block_region_notry (cfg, region);

	return (MonoInst *)g_hash_table_lookup (cfg->spvars, GINT_TO_POINTER (region));
}

static void
df_visit (MonoBasicBlock *start, int *dfn, MonoBasicBlock **array)
{
	int i;

	array [*dfn] = start;
	/* g_print ("visit %d at %p (BB%ld)\n", *dfn, start->cil_code, start->block_num); */
	for (i = 0; i < start->out_count; ++i) {
		if (start->out_bb [i]->dfn)
			continue;
		(*dfn)++;
		start->out_bb [i]->dfn = *dfn;
		start->out_bb [i]->df_parent = start;
		array [*dfn] = start->out_bb [i];
		df_visit (start->out_bb [i], dfn, array);
	}
}

guint32
mono_reverse_branch_op (guint32 opcode)
{
	static const int reverse_map [] = {
		CEE_BNE_UN, CEE_BLT, CEE_BLE, CEE_BGT, CEE_BGE,
		CEE_BEQ, CEE_BLT_UN, CEE_BLE_UN, CEE_BGT_UN, CEE_BGE_UN
	};
	static const int reverse_fmap [] = {
		OP_FBNE_UN, OP_FBLT, OP_FBLE, OP_FBGT, OP_FBGE,
		OP_FBEQ, OP_FBLT_UN, OP_FBLE_UN, OP_FBGT_UN, OP_FBGE_UN
	};
	static const int reverse_lmap [] = {
		OP_LBNE_UN, OP_LBLT, OP_LBLE, OP_LBGT, OP_LBGE,
		OP_LBEQ, OP_LBLT_UN, OP_LBLE_UN, OP_LBGT_UN, OP_LBGE_UN
	};
	static const int reverse_imap [] = {
		OP_IBNE_UN, OP_IBLT, OP_IBLE, OP_IBGT, OP_IBGE,
		OP_IBEQ, OP_IBLT_UN, OP_IBLE_UN, OP_IBGT_UN, OP_IBGE_UN
	};
				
	if (opcode >= CEE_BEQ && opcode <= CEE_BLT_UN) {
		opcode = reverse_map [opcode - CEE_BEQ];
	} else if (opcode >= OP_FBEQ && opcode <= OP_FBLT_UN) {
		opcode = reverse_fmap [opcode - OP_FBEQ];
	} else if (opcode >= OP_LBEQ && opcode <= OP_LBLT_UN) {
		opcode = reverse_lmap [opcode - OP_LBEQ];
	} else if (opcode >= OP_IBEQ && opcode <= OP_IBLT_UN) {
		opcode = reverse_imap [opcode - OP_IBEQ];
	} else
		g_assert_not_reached ();

	return opcode;
}

guint
mono_type_to_store_membase (MonoCompile *cfg, MonoType *type)
{
	type = mini_get_underlying_type (type);

handle_enum:
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		return OP_STOREI1_MEMBASE_REG;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		return OP_STOREI2_MEMBASE_REG;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return OP_STOREI4_MEMBASE_REG;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return OP_STORE_MEMBASE_REG;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return OP_STORE_MEMBASE_REG;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return OP_STOREI8_MEMBASE_REG;
	case MONO_TYPE_R4:
		return OP_STORER4_MEMBASE_REG;
	case MONO_TYPE_R8:
		return OP_STORER8_MEMBASE_REG;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			type = mono_class_enum_basetype_internal (type->data.klass);
			goto handle_enum;
		}
		if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type_internal (type)))
			return OP_STOREX_MEMBASE;
		return OP_STOREV_MEMBASE;
	case MONO_TYPE_TYPEDBYREF:
		return OP_STOREV_MEMBASE;
	case MONO_TYPE_GENERICINST:
		if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type_internal (type)))
			return OP_STOREX_MEMBASE;
		type = m_class_get_byval_arg (type->data.generic_class->container_class);
		goto handle_enum;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (mini_type_var_is_vt (type));
		return OP_STOREV_MEMBASE;
	default:
		g_error ("unknown type 0x%02x in type_to_store_membase", type->type);
	}
	return -1;
}

guint
mono_type_to_load_membase (MonoCompile *cfg, MonoType *type)
{
	type = mini_get_underlying_type (type);

	switch (type->type) {
	case MONO_TYPE_I1:
		return OP_LOADI1_MEMBASE;
	case MONO_TYPE_U1:
		return OP_LOADU1_MEMBASE;
	case MONO_TYPE_I2:
		return OP_LOADI2_MEMBASE;
	case MONO_TYPE_U2:
		return OP_LOADU2_MEMBASE;
	case MONO_TYPE_I4:
		return OP_LOADI4_MEMBASE;
	case MONO_TYPE_U4:
		return OP_LOADU4_MEMBASE;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return OP_LOAD_MEMBASE;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return OP_LOAD_MEMBASE;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return OP_LOADI8_MEMBASE;
	case MONO_TYPE_R4:
		return OP_LOADR4_MEMBASE;
	case MONO_TYPE_R8:
		return OP_LOADR8_MEMBASE;
	case MONO_TYPE_VALUETYPE:
		if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type_internal (type)))
			return OP_LOADX_MEMBASE;
	case MONO_TYPE_TYPEDBYREF:
		return OP_LOADV_MEMBASE;
	case MONO_TYPE_GENERICINST:
		if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type_internal (type)))
			return OP_LOADX_MEMBASE;
		if (mono_type_generic_inst_is_valuetype (type))
			return OP_LOADV_MEMBASE;
		else
			return OP_LOAD_MEMBASE;
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (cfg->gshared);
		g_assert (mini_type_var_is_vt (type));
		return OP_LOADV_MEMBASE;
	default:
		g_error ("unknown type 0x%02x in type_to_load_membase", type->type);
	}
	return -1;
}

guint
mini_type_to_stind (MonoCompile* cfg, MonoType *type)
{
	type = mini_get_underlying_type (type);
	if (cfg->gshared && !type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR)) {
		g_assert (mini_type_var_is_vt (type));
		return CEE_STOBJ;
	}
	return mono_type_to_stind (type);
}

int
mono_op_imm_to_op (int opcode)
{
	switch (opcode) {
	case OP_ADD_IMM:
#if SIZEOF_REGISTER == 4
		return OP_IADD;
#else
		return OP_LADD;
#endif
	case OP_IADD_IMM:
		return OP_IADD;
	case OP_LADD_IMM:
		return OP_LADD;
	case OP_ISUB_IMM:
		return OP_ISUB;
	case OP_LSUB_IMM:
		return OP_LSUB;
	case OP_IMUL_IMM:
		return OP_IMUL;
	case OP_LMUL_IMM:
		return OP_LMUL;
	case OP_AND_IMM:
#if SIZEOF_REGISTER == 4
		return OP_IAND;
#else
		return OP_LAND;
#endif
	case OP_OR_IMM:
#if SIZEOF_REGISTER == 4
		return OP_IOR;
#else
		return OP_LOR;
#endif
	case OP_XOR_IMM:
#if SIZEOF_REGISTER == 4
		return OP_IXOR;
#else
		return OP_LXOR;
#endif
	case OP_IAND_IMM:
		return OP_IAND;
	case OP_LAND_IMM:
		return OP_LAND;
	case OP_IOR_IMM:
		return OP_IOR;
	case OP_LOR_IMM:
		return OP_LOR;
	case OP_IXOR_IMM:
		return OP_IXOR;
	case OP_LXOR_IMM:
		return OP_LXOR;
	case OP_ISHL_IMM:
		return OP_ISHL;
	case OP_LSHL_IMM:
		return OP_LSHL;
	case OP_ISHR_IMM:
		return OP_ISHR;
	case OP_LSHR_IMM:
		return OP_LSHR;
	case OP_ISHR_UN_IMM:
		return OP_ISHR_UN;
	case OP_LSHR_UN_IMM:
		return OP_LSHR_UN;
	case OP_IDIV_IMM:
		return OP_IDIV;
	case OP_LDIV_IMM:
		return OP_LDIV;
	case OP_IDIV_UN_IMM:
		return OP_IDIV_UN;
	case OP_LDIV_UN_IMM:
		return OP_LDIV_UN;
	case OP_IREM_UN_IMM:
		return OP_IREM_UN;
	case OP_LREM_UN_IMM:
		return OP_LREM_UN;
	case OP_IREM_IMM:
		return OP_IREM;
	case OP_LREM_IMM:
		return OP_LREM;
	case OP_DIV_IMM:
#if SIZEOF_REGISTER == 4
		return OP_IDIV;
#else
		return OP_LDIV;
#endif
	case OP_REM_IMM:
#if SIZEOF_REGISTER == 4
		return OP_IREM;
#else
		return OP_LREM;
#endif
	case OP_ADDCC_IMM:
		return OP_ADDCC;
	case OP_ADC_IMM:
		return OP_ADC;
	case OP_SUBCC_IMM:
		return OP_SUBCC;
	case OP_SBB_IMM:
		return OP_SBB;
	case OP_IADC_IMM:
		return OP_IADC;
	case OP_ISBB_IMM:
		return OP_ISBB;
	case OP_COMPARE_IMM:
		return OP_COMPARE;
	case OP_ICOMPARE_IMM:
		return OP_ICOMPARE;
	case OP_LOCALLOC_IMM:
		return OP_LOCALLOC;
	}

	return -1;
}

/*
 * mono_decompose_op_imm:
 *
 *   Replace the OP_.._IMM INS with its non IMM variant.
 */
void
mono_decompose_op_imm (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins)
{
	int opcode2 = mono_op_imm_to_op (ins->opcode);
	MonoInst *temp;
	guint32 dreg;
	const char *spec = INS_INFO (ins->opcode);

	if (spec [MONO_INST_SRC2] == 'l') {
		dreg = mono_alloc_lreg (cfg);

		/* Load the 64bit constant using decomposed ops */
		MONO_INST_NEW (cfg, temp, OP_ICONST);
		temp->inst_c0 = ins_get_l_low (ins);
		temp->dreg = MONO_LVREG_LS (dreg);
		mono_bblock_insert_before_ins (bb, ins, temp);

		MONO_INST_NEW (cfg, temp, OP_ICONST);
		temp->inst_c0 = ins_get_l_high (ins);
		temp->dreg = MONO_LVREG_MS (dreg);
	} else {
		dreg = mono_alloc_ireg (cfg);

		MONO_INST_NEW (cfg, temp, OP_ICONST);
		temp->inst_c0 = ins->inst_imm;
		temp->dreg = dreg;
	}

	mono_bblock_insert_before_ins (bb, ins, temp);

	if (opcode2 == -1)
                g_error ("mono_op_imm_to_op failed for %s\n", mono_inst_name (ins->opcode));
	ins->opcode = opcode2;

	if (ins->opcode == OP_LOCALLOC)
		ins->sreg1 = dreg;
	else
		ins->sreg2 = dreg;

	bb->max_vreg = MAX (bb->max_vreg, cfg->next_vreg);
}

static void
set_vreg_to_inst (MonoCompile *cfg, int vreg, MonoInst *inst)
{
	if (vreg >= cfg->vreg_to_inst_len) {
		MonoInst **tmp = cfg->vreg_to_inst;
		int size = cfg->vreg_to_inst_len;

		while (vreg >= cfg->vreg_to_inst_len)
			cfg->vreg_to_inst_len = cfg->vreg_to_inst_len ? cfg->vreg_to_inst_len * 2 : 32;
		cfg->vreg_to_inst = (MonoInst **)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst*) * cfg->vreg_to_inst_len);
		if (size)
			memcpy (cfg->vreg_to_inst, tmp, size * sizeof (MonoInst*));
	}
	cfg->vreg_to_inst [vreg] = inst;
}

#define mono_type_is_long(type) (!(type)->byref && ((mono_type_get_underlying_type (type)->type == MONO_TYPE_I8) || (mono_type_get_underlying_type (type)->type == MONO_TYPE_U8)))
#define mono_type_is_float(type) (!(type)->byref && (((type)->type == MONO_TYPE_R8) || ((type)->type == MONO_TYPE_R4)))

MonoInst*
mono_compile_create_var_for_vreg (MonoCompile *cfg, MonoType *type, int opcode, int vreg)
{
	MonoInst *inst;
	int num = cfg->num_varinfo;
	gboolean regpair;

	type = mini_get_underlying_type (type);

	if ((num + 1) >= cfg->varinfo_count) {
		int orig_count = cfg->varinfo_count;
		cfg->varinfo_count = cfg->varinfo_count ? (cfg->varinfo_count * 2) : 32;
		cfg->varinfo = (MonoInst **)g_realloc (cfg->varinfo, sizeof (MonoInst*) * cfg->varinfo_count);
		cfg->vars = (MonoMethodVar *)g_realloc (cfg->vars, sizeof (MonoMethodVar) * cfg->varinfo_count);
		memset (&cfg->vars [orig_count], 0, (cfg->varinfo_count - orig_count) * sizeof (MonoMethodVar));
	}

	cfg->stat_allocate_var++;

	MONO_INST_NEW (cfg, inst, opcode);
	inst->inst_c0 = num;
	inst->inst_vtype = type;
	inst->klass = mono_class_from_mono_type_internal (type);
	mini_type_to_eval_stack_type (cfg, type, inst);
	/* if set to 1 the variable is native */
	inst->backend.is_pinvoke = 0;
	inst->dreg = vreg;

	if (mono_class_has_failure (inst->klass))
		mono_cfg_set_exception (cfg, MONO_EXCEPTION_TYPE_LOAD);

	if (cfg->compute_gc_maps) {
		if (type->byref) {
			mono_mark_vreg_as_mp (cfg, vreg);
		} else {
			if ((MONO_TYPE_ISSTRUCT (type) && m_class_has_references (inst->klass)) || mini_type_is_reference (type)) {
				inst->flags |= MONO_INST_GC_TRACK;
				mono_mark_vreg_as_ref (cfg, vreg);
			}
		}
	}
	
	cfg->varinfo [num] = inst;

	cfg->vars [num].idx = num;
	cfg->vars [num].vreg = vreg;
	cfg->vars [num].range.first_use.pos.bid = 0xffff;
	cfg->vars [num].reg = -1;

	if (vreg != -1)
		set_vreg_to_inst (cfg, vreg, inst);

#if SIZEOF_REGISTER == 4
	if (mono_arch_is_soft_float ()) {
		regpair = mono_type_is_long (type) || mono_type_is_float (type);
	} else {
		regpair = mono_type_is_long (type);
	}
#else
	regpair = FALSE;
#endif

	if (regpair) {
		MonoInst *tree;

		/* 
		 * These two cannot be allocated using create_var_for_vreg since that would
		 * put it into the cfg->varinfo array, confusing many parts of the JIT.
		 */

		/* 
		 * Set flags to VOLATILE so SSA skips it.
		 */

		if (cfg->verbose_level >= 4) {
			printf ("  Create LVAR R%d (R%d, R%d)\n", inst->dreg, MONO_LVREG_LS (inst->dreg), MONO_LVREG_MS (inst->dreg));
		}

		if (mono_arch_is_soft_float () && cfg->opt & MONO_OPT_SSA) {
			if (mono_type_is_float (type))
				inst->flags = MONO_INST_VOLATILE;
		}

		/* Allocate a dummy MonoInst for the first vreg */
		MONO_INST_NEW (cfg, tree, OP_LOCAL);
		tree->dreg = MONO_LVREG_LS (inst->dreg);
		if (cfg->opt & MONO_OPT_SSA)
			tree->flags = MONO_INST_VOLATILE;
		tree->inst_c0 = num;
		tree->type = STACK_I4;
		tree->inst_vtype = mono_get_int32_type ();
		tree->klass = mono_class_from_mono_type_internal (tree->inst_vtype);

		set_vreg_to_inst (cfg, MONO_LVREG_LS (inst->dreg), tree);

		/* Allocate a dummy MonoInst for the second vreg */
		MONO_INST_NEW (cfg, tree, OP_LOCAL);
		tree->dreg = MONO_LVREG_MS (inst->dreg);
		if (cfg->opt & MONO_OPT_SSA)
			tree->flags = MONO_INST_VOLATILE;
		tree->inst_c0 = num;
		tree->type = STACK_I4;
		tree->inst_vtype = mono_get_int32_type ();
		tree->klass = mono_class_from_mono_type_internal (tree->inst_vtype);

		set_vreg_to_inst (cfg, MONO_LVREG_MS (inst->dreg), tree);
	}

	cfg->num_varinfo++;
	if (cfg->verbose_level > 2)
		g_print ("created temp %d (R%d) of type %s\n", num, vreg, mono_type_get_name (type));

	return inst;
}

MonoInst*
mono_compile_create_var (MonoCompile *cfg, MonoType *type, int opcode)
{
	int dreg;

	if (type->type == MONO_TYPE_VALUETYPE && !type->byref) {
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		if (m_class_is_enumtype (klass) && m_class_get_image (klass) == mono_get_corlib () && !strcmp (m_class_get_name (klass), "StackCrawlMark")) {
			if (!(cfg->method->flags & METHOD_ATTRIBUTE_REQSECOBJ))
				g_error ("Method '%s' which contains a StackCrawlMark local variable must be decorated with [System.Security.DynamicSecurityMethod].", mono_method_get_full_name (cfg->method));
		}
	}

	type = mini_get_underlying_type (type);

	if (mono_type_is_long (type))
		dreg = mono_alloc_dreg (cfg, STACK_I8);
	else if (mono_arch_is_soft_float () && mono_type_is_float (type))
		dreg = mono_alloc_dreg (cfg, STACK_R8);
	else
		/* All the others are unified */
		dreg = mono_alloc_preg (cfg);

	return mono_compile_create_var_for_vreg (cfg, type, opcode, dreg);
}

MonoInst*
mini_get_int_to_float_spill_area (MonoCompile *cfg)
{
#ifdef TARGET_X86
	if (!cfg->iconv_raw_var) {
		cfg->iconv_raw_var = mono_compile_create_var (cfg, mono_get_int32_type (), OP_LOCAL);
		cfg->iconv_raw_var->flags |= MONO_INST_VOLATILE; /*FIXME, use the don't regalloc flag*/
	}
	return cfg->iconv_raw_var;
#else
	return NULL;
#endif
}

void
mono_mark_vreg_as_ref (MonoCompile *cfg, int vreg)
{
	if (vreg >= cfg->vreg_is_ref_len) {
		gboolean *tmp = cfg->vreg_is_ref;
		int size = cfg->vreg_is_ref_len;

		while (vreg >= cfg->vreg_is_ref_len)
			cfg->vreg_is_ref_len = cfg->vreg_is_ref_len ? cfg->vreg_is_ref_len * 2 : 32;
		cfg->vreg_is_ref = (gboolean *)mono_mempool_alloc0 (cfg->mempool, sizeof (gboolean) * cfg->vreg_is_ref_len);
		if (size)
			memcpy (cfg->vreg_is_ref, tmp, size * sizeof (gboolean));
	}
	cfg->vreg_is_ref [vreg] = TRUE;
}	

void
mono_mark_vreg_as_mp (MonoCompile *cfg, int vreg)
{
	if (vreg >= cfg->vreg_is_mp_len) {
		gboolean *tmp = cfg->vreg_is_mp;
		int size = cfg->vreg_is_mp_len;

		while (vreg >= cfg->vreg_is_mp_len)
			cfg->vreg_is_mp_len = cfg->vreg_is_mp_len ? cfg->vreg_is_mp_len * 2 : 32;
		cfg->vreg_is_mp = (gboolean *)mono_mempool_alloc0 (cfg->mempool, sizeof (gboolean) * cfg->vreg_is_mp_len);
		if (size)
			memcpy (cfg->vreg_is_mp, tmp, size * sizeof (gboolean));
	}
	cfg->vreg_is_mp [vreg] = TRUE;
}	

static MonoType*
type_from_stack_type (MonoInst *ins)
{
	switch (ins->type) {
	case STACK_I4: return mono_get_int32_type ();
	case STACK_I8: return m_class_get_byval_arg (mono_defaults.int64_class);
	case STACK_PTR: return mono_get_int_type ();
	case STACK_R8: return m_class_get_byval_arg (mono_defaults.double_class);
	case STACK_MP:
		/* 
		 * this if used to be commented without any specific reason, but
		 * it breaks #80235 when commented
		 */
		if (ins->klass)
			return m_class_get_this_arg (ins->klass);
		else
			return m_class_get_this_arg (mono_defaults.object_class);
	case STACK_OBJ:
		/* ins->klass may not be set for ldnull.
		 * Also, if we have a boxed valuetype, we want an object lass,
		 * not the valuetype class
		 */
		if (ins->klass && !m_class_is_valuetype (ins->klass))
			return m_class_get_byval_arg (ins->klass);
		return mono_get_object_type ();
	case STACK_VTYPE: return m_class_get_byval_arg (ins->klass);
	default:
		g_error ("stack type %d to montype not handled\n", ins->type);
	}
	return NULL;
}

MonoType*
mono_type_from_stack_type (MonoInst *ins)
{
	return type_from_stack_type (ins);
}

/*
 * mono_add_ins_to_end:
 *
 *   Same as MONO_ADD_INS, but add INST before any branches at the end of BB.
 */
void
mono_add_ins_to_end (MonoBasicBlock *bb, MonoInst *inst)
{
	int opcode;

	if (!bb->code) {
		MONO_ADD_INS (bb, inst);
		return;
	}

	switch (bb->last_ins->opcode) {
	case OP_BR:
	case OP_BR_REG:
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
	case OP_SWITCH:
		mono_bblock_insert_before_ins (bb, bb->last_ins, inst);
		break;
	default:
		if (MONO_IS_COND_BRANCH_OP (bb->last_ins)) {
			/* Need to insert the ins before the compare */
			if (bb->code == bb->last_ins) {
				mono_bblock_insert_before_ins (bb, bb->last_ins, inst);
				return;
			}

			if (bb->code->next == bb->last_ins) {
				/* Only two instructions */
				opcode = bb->code->opcode;

				if ((opcode == OP_COMPARE) || (opcode == OP_COMPARE_IMM) || (opcode == OP_ICOMPARE) || (opcode == OP_ICOMPARE_IMM) || (opcode == OP_FCOMPARE) || (opcode == OP_LCOMPARE) || (opcode == OP_LCOMPARE_IMM) || (opcode == OP_RCOMPARE)) {
					/* NEW IR */
					mono_bblock_insert_before_ins (bb, bb->code, inst);
				} else {
					mono_bblock_insert_before_ins (bb, bb->last_ins, inst);
				}
			} else {
				opcode = bb->last_ins->prev->opcode;

				if ((opcode == OP_COMPARE) || (opcode == OP_COMPARE_IMM) || (opcode == OP_ICOMPARE) || (opcode == OP_ICOMPARE_IMM) || (opcode == OP_FCOMPARE) || (opcode == OP_LCOMPARE) || (opcode == OP_LCOMPARE_IMM) || (opcode == OP_RCOMPARE)) {
					/* NEW IR */
					mono_bblock_insert_before_ins (bb, bb->last_ins->prev, inst);
				} else {
					mono_bblock_insert_before_ins (bb, bb->last_ins, inst);
				}					
			}
		}
		else
			MONO_ADD_INS (bb, inst);
		break;
	}
}

void
mono_create_jump_table (MonoCompile *cfg, MonoInst *label, MonoBasicBlock **bbs, int num_blocks)
{
	MonoJumpInfo *ji = (MonoJumpInfo *)mono_mempool_alloc (cfg->mempool, sizeof (MonoJumpInfo));
	MonoJumpInfoBBTable *table;

	table = (MonoJumpInfoBBTable *)mono_mempool_alloc (cfg->mempool, sizeof (MonoJumpInfoBBTable));
	table->table = bbs;
	table->table_size = num_blocks;
	
	ji->ip.label = label;
	ji->type = MONO_PATCH_INFO_SWITCH;
	ji->data.table = table;
	ji->next = cfg->patch_info;
	cfg->patch_info = ji;
}

gboolean
mini_assembly_can_skip_verification (MonoDomain *domain, MonoMethod *method)
{
	MonoAssembly *assembly = m_class_get_image (method->klass)->assembly;
	if (method->wrapper_type != MONO_WRAPPER_NONE && method->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD)
		return FALSE;
	if (assembly->image == mono_defaults.corlib)
		return FALSE;
	return mono_assembly_has_skip_verification (assembly);
}

typedef struct {
	MonoClass *vtype;
	GList *active, *inactive;
	GSList *slots;
} StackSlotInfo;

static gint 
compare_by_interval_start_pos_func (gconstpointer a, gconstpointer b)
{
	MonoMethodVar *v1 = (MonoMethodVar*)a;
	MonoMethodVar *v2 = (MonoMethodVar*)b;

	if (v1 == v2)
		return 0;
	else if (v1->interval->range && v2->interval->range)
		return v1->interval->range->from - v2->interval->range->from;
	else if (v1->interval->range)
		return -1;
	else
		return 1;
}

#if 0
#define LSCAN_DEBUG(a) do { a; } while (0)
#else
#define LSCAN_DEBUG(a) do { } while (0) /* non-empty to avoid warning */
#endif

static gint32*
mono_allocate_stack_slots2 (MonoCompile *cfg, gboolean backward, guint32 *stack_size, guint32 *stack_align)
{
	int i, slot, offset, size;
	guint32 align;
	MonoMethodVar *vmv;
	MonoInst *inst;
	gint32 *offsets;
	GList *vars = NULL, *l, *unhandled;
	StackSlotInfo *scalar_stack_slots, *vtype_stack_slots, *slot_info;
	MonoType *t;
	int nvtypes;
	int vtype_stack_slots_size = 256;
	gboolean reuse_slot;

	LSCAN_DEBUG (printf ("Allocate Stack Slots 2 for %s:\n", mono_method_full_name (cfg->method, TRUE)));

	scalar_stack_slots = (StackSlotInfo *)mono_mempool_alloc0 (cfg->mempool, sizeof (StackSlotInfo) * MONO_TYPE_PINNED);
	vtype_stack_slots = NULL;
	nvtypes = 0;

	offsets = (gint32 *)mono_mempool_alloc (cfg->mempool, sizeof (gint32) * cfg->num_varinfo);
	for (i = 0; i < cfg->num_varinfo; ++i)
		offsets [i] = -1;

	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		inst = cfg->varinfo [i];
		vmv = MONO_VARINFO (cfg, i);

		if ((inst->flags & MONO_INST_IS_DEAD) || inst->opcode == OP_REGVAR || inst->opcode == OP_REGOFFSET)
			continue;

		vars = g_list_prepend (vars, vmv);
	}

	vars = g_list_sort (vars, compare_by_interval_start_pos_func);

	/* Sanity check */
	/*
	i = 0;
	for (unhandled = vars; unhandled; unhandled = unhandled->next) {
		MonoMethodVar *current = unhandled->data;

		if (current->interval->range) {
			g_assert (current->interval->range->from >= i);
			i = current->interval->range->from;
		}
	}
	*/

	offset = 0;
	*stack_align = 0;
	for (unhandled = vars; unhandled; unhandled = unhandled->next) {
		MonoMethodVar *current = (MonoMethodVar *)unhandled->data;

		vmv = current;
		inst = cfg->varinfo [vmv->idx];

		t = mono_type_get_underlying_type (inst->inst_vtype);
		if (cfg->gsharedvt && mini_is_gsharedvt_variable_type (t))
			continue;

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structures */
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (t) && t->type != MONO_TYPE_TYPEDBYREF) {
			size = mono_class_native_size (mono_class_from_mono_type_internal (t), &align);
		}
		else {
			int ialign;

			size = mini_type_stack_size (t, &ialign);
			align = ialign;

			if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type_internal (t)))
				align = 16;
		}

		reuse_slot = TRUE;
		if (cfg->disable_reuse_stack_slots)
			reuse_slot = FALSE;

		t = mini_get_underlying_type (t);
		switch (t->type) {
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (t)) {
				slot_info = &scalar_stack_slots [t->type];
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
			if (!vtype_stack_slots)
				vtype_stack_slots = (StackSlotInfo *)mono_mempool_alloc0 (cfg->mempool, sizeof (StackSlotInfo) * vtype_stack_slots_size);
			for (i = 0; i < nvtypes; ++i)
				if (t->data.klass == vtype_stack_slots [i].vtype)
					break;
			if (i < nvtypes)
				slot_info = &vtype_stack_slots [i];
			else {
				if (nvtypes == vtype_stack_slots_size) {
					int new_slots_size = vtype_stack_slots_size * 2;
					StackSlotInfo* new_slots = (StackSlotInfo *)mono_mempool_alloc0 (cfg->mempool, sizeof (StackSlotInfo) * new_slots_size);

					memcpy (new_slots, vtype_stack_slots, sizeof (StackSlotInfo) * vtype_stack_slots_size);

					vtype_stack_slots = new_slots;
					vtype_stack_slots_size = new_slots_size;
				}
				vtype_stack_slots [nvtypes].vtype = t->data.klass;
				slot_info = &vtype_stack_slots [nvtypes];
				nvtypes ++;
			}
			if (cfg->disable_reuse_ref_stack_slots)
				reuse_slot = FALSE;
			break;

		case MONO_TYPE_PTR:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#if TARGET_SIZEOF_VOID_P == 4
		case MONO_TYPE_I4:
#else
		case MONO_TYPE_I8:
#endif
			if (cfg->disable_ref_noref_stack_slot_share) {
				slot_info = &scalar_stack_slots [MONO_TYPE_I];
				break;
			}
			/* Fall through */

		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_STRING:
			/* Share non-float stack slots of the same size */
			slot_info = &scalar_stack_slots [MONO_TYPE_CLASS];
			if (cfg->disable_reuse_ref_stack_slots)
				reuse_slot = FALSE;
			break;

		default:
			slot_info = &scalar_stack_slots [t->type];
		}

		slot = 0xffffff;
		if (cfg->comp_done & MONO_COMP_LIVENESS) {
			int pos;
			gboolean changed;

			//printf ("START  %2d %08x %08x\n",  vmv->idx, vmv->range.first_use.abs_pos, vmv->range.last_use.abs_pos);

			if (!current->interval->range) {
				if (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))
					pos = ~0;
				else {
					/* Dead */
					inst->flags |= MONO_INST_IS_DEAD;
					continue;
				}
			}
			else
				pos = current->interval->range->from;

			LSCAN_DEBUG (printf ("process R%d ", inst->dreg));
			if (current->interval->range)
				LSCAN_DEBUG (mono_linterval_print (current->interval));
			LSCAN_DEBUG (printf ("\n"));

			/* Check for intervals in active which expired or inactive */
			changed = TRUE;
			/* FIXME: Optimize this */
			while (changed) {
				changed = FALSE;
				for (l = slot_info->active; l != NULL; l = l->next) {
					MonoMethodVar *v = (MonoMethodVar*)l->data;

					if (v->interval->last_range->to < pos) {
						slot_info->active = g_list_delete_link (slot_info->active, l);
						slot_info->slots = g_slist_prepend_mempool (cfg->mempool, slot_info->slots, GINT_TO_POINTER (offsets [v->idx]));
						LSCAN_DEBUG (printf ("Interval R%d has expired, adding 0x%x to slots\n", cfg->varinfo [v->idx]->dreg, offsets [v->idx]));
						changed = TRUE;
						break;
					}
					else if (!mono_linterval_covers (v->interval, pos)) {
						slot_info->inactive = g_list_append (slot_info->inactive, v);
						slot_info->active = g_list_delete_link (slot_info->active, l);
						LSCAN_DEBUG (printf ("Interval R%d became inactive\n", cfg->varinfo [v->idx]->dreg));
						changed = TRUE;
						break;
					}
				}
			}

			/* Check for intervals in inactive which expired or active */
			changed = TRUE;
			/* FIXME: Optimize this */
			while (changed) {
				changed = FALSE;
				for (l = slot_info->inactive; l != NULL; l = l->next) {
					MonoMethodVar *v = (MonoMethodVar*)l->data;

					if (v->interval->last_range->to < pos) {
						slot_info->inactive = g_list_delete_link (slot_info->inactive, l);
						// FIXME: Enabling this seems to cause impossible to debug crashes
						//slot_info->slots = g_slist_prepend_mempool (cfg->mempool, slot_info->slots, GINT_TO_POINTER (offsets [v->idx]));
						LSCAN_DEBUG (printf ("Interval R%d has expired, adding 0x%x to slots\n", cfg->varinfo [v->idx]->dreg, offsets [v->idx]));
						changed = TRUE;
						break;
					}
					else if (mono_linterval_covers (v->interval, pos)) {
						slot_info->active = g_list_append (slot_info->active, v);
						slot_info->inactive = g_list_delete_link (slot_info->inactive, l);
						LSCAN_DEBUG (printf ("\tInterval R%d became active\n", cfg->varinfo [v->idx]->dreg));
						changed = TRUE;
						break;
					}
				}
			}

			/* 
			 * This also handles the case when the variable is used in an
			 * exception region, as liveness info is not computed there.
			 */
			/* 
			 * FIXME: All valuetypes are marked as INDIRECT because of LDADDR
			 * opcodes.
			 */
			if (! (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
				if (slot_info->slots) {
					slot = GPOINTER_TO_INT (slot_info->slots->data);

					slot_info->slots = slot_info->slots->next;
				}

				/* FIXME: We might want to consider the inactive intervals as well if slot_info->slots is empty */

				slot_info->active = mono_varlist_insert_sorted (cfg, slot_info->active, vmv, TRUE);
			}
		}

#if 0
		{
			static int count = 0;
			count ++;

			if (count == atoi (g_getenv ("COUNT3")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (count > atoi (g_getenv ("COUNT3")))
				slot = 0xffffff;
			else
				mono_print_ins (inst);
		}
#endif

		LSCAN_DEBUG (printf ("R%d %s -> 0x%x\n", inst->dreg, mono_type_full_name (t), slot));

		if (inst->flags & MONO_INST_LMF) {
			size = MONO_ABI_SIZEOF (MonoLMF);
			align = sizeof (target_mgreg_t);
			reuse_slot = FALSE;
		}

		if (!reuse_slot)
			slot = 0xffffff;

		if (slot == 0xffffff) {
			/*
			 * Allways allocate valuetypes to sizeof (target_mgreg_t) to allow more
			 * efficient copying (and to work around the fact that OP_MEMCPY
			 * and OP_MEMSET ignores alignment).
			 */
			if (MONO_TYPE_ISSTRUCT (t)) {
				align = MAX (align, sizeof (target_mgreg_t));
				align = MAX (align, mono_class_min_align (mono_class_from_mono_type_internal (t)));
			}

			if (backward) {
				offset += size;
				offset += align - 1;
				offset &= ~(align - 1);
				slot = offset;
			}
			else {
				offset += align - 1;
				offset &= ~(align - 1);
				slot = offset;
				offset += size;
			}

			if (*stack_align == 0)
				*stack_align = align;
		}

		offsets [vmv->idx] = slot;
	}
	g_list_free (vars);
	for (i = 0; i < MONO_TYPE_PINNED; ++i) {
		if (scalar_stack_slots [i].active)
			g_list_free (scalar_stack_slots [i].active);
	}
	for (i = 0; i < nvtypes; ++i) {
		if (vtype_stack_slots [i].active)
			g_list_free (vtype_stack_slots [i].active);
	}

	cfg->stat_locals_stack_size += offset;

	*stack_size = offset;
	return offsets;
}

/*
 *  mono_allocate_stack_slots:
 *
 *  Allocate stack slots for all non register allocated variables using a
 * linear scan algorithm.
 * Returns: an array of stack offsets.
 * STACK_SIZE is set to the amount of stack space needed.
 * STACK_ALIGN is set to the alignment needed by the locals area.
 */
gint32*
mono_allocate_stack_slots (MonoCompile *cfg, gboolean backward, guint32 *stack_size, guint32 *stack_align)
{
	int i, slot, offset, size;
	guint32 align;
	MonoMethodVar *vmv;
	MonoInst *inst;
	gint32 *offsets;
	GList *vars = NULL, *l;
	StackSlotInfo *scalar_stack_slots, *vtype_stack_slots, *slot_info;
	MonoType *t;
	int nvtypes;
	int vtype_stack_slots_size = 256;
	gboolean reuse_slot;

	if ((cfg->num_varinfo > 0) && MONO_VARINFO (cfg, 0)->interval)
		return mono_allocate_stack_slots2 (cfg, backward, stack_size, stack_align);

	scalar_stack_slots = (StackSlotInfo *)mono_mempool_alloc0 (cfg->mempool, sizeof (StackSlotInfo) * MONO_TYPE_PINNED);
	vtype_stack_slots = NULL;
	nvtypes = 0;

	offsets = (gint32 *)mono_mempool_alloc (cfg->mempool, sizeof (gint32) * cfg->num_varinfo);
	for (i = 0; i < cfg->num_varinfo; ++i)
		offsets [i] = -1;

	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		inst = cfg->varinfo [i];
		vmv = MONO_VARINFO (cfg, i);

		if ((inst->flags & MONO_INST_IS_DEAD) || inst->opcode == OP_REGVAR || inst->opcode == OP_REGOFFSET)
			continue;

		vars = g_list_prepend (vars, vmv);
	}

	vars = mono_varlist_sort (cfg, vars, 0);
	offset = 0;
	*stack_align = sizeof (target_mgreg_t);
	for (l = vars; l; l = l->next) {
		vmv = (MonoMethodVar *)l->data;
		inst = cfg->varinfo [vmv->idx];

		t = mono_type_get_underlying_type (inst->inst_vtype);
		if (cfg->gsharedvt && mini_is_gsharedvt_variable_type (t))
			continue;

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structures */
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (t) && t->type != MONO_TYPE_TYPEDBYREF) {
			size = mono_class_native_size (mono_class_from_mono_type_internal (t), &align);
		} else {
			int ialign;

			size = mini_type_stack_size (t, &ialign);
			align = ialign;

			if (mono_class_has_failure (mono_class_from_mono_type_internal (t)))
				mono_cfg_set_exception (cfg, MONO_EXCEPTION_TYPE_LOAD);

			if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type_internal (t)))
				align = 16;
		}

		reuse_slot = TRUE;
		if (cfg->disable_reuse_stack_slots)
			reuse_slot = FALSE;

		t = mini_get_underlying_type (t);
		switch (t->type) {
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (t)) {
				slot_info = &scalar_stack_slots [t->type];
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
			if (!vtype_stack_slots)
				vtype_stack_slots = (StackSlotInfo *)mono_mempool_alloc0 (cfg->mempool, sizeof (StackSlotInfo) * vtype_stack_slots_size);
			for (i = 0; i < nvtypes; ++i)
				if (t->data.klass == vtype_stack_slots [i].vtype)
					break;
			if (i < nvtypes)
				slot_info = &vtype_stack_slots [i];
			else {
				if (nvtypes == vtype_stack_slots_size) {
					int new_slots_size = vtype_stack_slots_size * 2;
					StackSlotInfo* new_slots = (StackSlotInfo *)mono_mempool_alloc0 (cfg->mempool, sizeof (StackSlotInfo) * new_slots_size);

					memcpy (new_slots, vtype_stack_slots, sizeof (StackSlotInfo) * vtype_stack_slots_size);

					vtype_stack_slots = new_slots;
					vtype_stack_slots_size = new_slots_size;
				}
				vtype_stack_slots [nvtypes].vtype = t->data.klass;
				slot_info = &vtype_stack_slots [nvtypes];
				nvtypes ++;
			}
			if (cfg->disable_reuse_ref_stack_slots)
				reuse_slot = FALSE;
			break;

		case MONO_TYPE_PTR:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#if TARGET_SIZEOF_VOID_P == 4
		case MONO_TYPE_I4:
#else
		case MONO_TYPE_I8:
#endif
			if (cfg->disable_ref_noref_stack_slot_share) {
				slot_info = &scalar_stack_slots [MONO_TYPE_I];
				break;
			}
			/* Fall through */

		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_STRING:
			/* Share non-float stack slots of the same size */
			slot_info = &scalar_stack_slots [MONO_TYPE_CLASS];
			if (cfg->disable_reuse_ref_stack_slots)
				reuse_slot = FALSE;
			break;
		case MONO_TYPE_VAR:
		case MONO_TYPE_MVAR:
			slot_info = &scalar_stack_slots [t->type];
			break;
		default:
			slot_info = &scalar_stack_slots [t->type];
			break;
		}

		slot = 0xffffff;
		if (cfg->comp_done & MONO_COMP_LIVENESS) {
			//printf ("START  %2d %08x %08x\n",  vmv->idx, vmv->range.first_use.abs_pos, vmv->range.last_use.abs_pos);
			
			/* expire old intervals in active */
			while (slot_info->active) {
				MonoMethodVar *amv = (MonoMethodVar *)slot_info->active->data;

				if (amv->range.last_use.abs_pos > vmv->range.first_use.abs_pos)
					break;

				//printf ("EXPIR  %2d %08x %08x C%d R%d\n", amv->idx, amv->range.first_use.abs_pos, amv->range.last_use.abs_pos, amv->spill_costs, amv->reg);

				slot_info->active = g_list_delete_link (slot_info->active, slot_info->active);
				slot_info->slots = g_slist_prepend_mempool (cfg->mempool, slot_info->slots, GINT_TO_POINTER (offsets [amv->idx]));
			}

			/* 
			 * This also handles the case when the variable is used in an
			 * exception region, as liveness info is not computed there.
			 */
			/* 
			 * FIXME: All valuetypes are marked as INDIRECT because of LDADDR
			 * opcodes.
			 */
			if (! (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
				if (slot_info->slots) {
					slot = GPOINTER_TO_INT (slot_info->slots->data);

					slot_info->slots = slot_info->slots->next;
				}

				slot_info->active = mono_varlist_insert_sorted (cfg, slot_info->active, vmv, TRUE);
			}
		}

#if 0
		{
			static int count = 0;
			count ++;

			if (count == atoi (g_getenv ("COUNT")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (count > atoi (g_getenv ("COUNT")))
				slot = 0xffffff;
			else
				mono_print_ins (inst);
		}
#endif

		if (inst->flags & MONO_INST_LMF) {
			/*
			 * This variable represents a MonoLMF structure, which has no corresponding
			 * CLR type, so hard-code its size/alignment.
			 */
			size = MONO_ABI_SIZEOF (MonoLMF);
			align = sizeof (target_mgreg_t);
			reuse_slot = FALSE;
		}

		if (!reuse_slot)
			slot = 0xffffff;

		if (slot == 0xffffff) {
			/*
			 * Allways allocate valuetypes to sizeof (target_mgreg_t) to allow more
			 * efficient copying (and to work around the fact that OP_MEMCPY
			 * and OP_MEMSET ignores alignment).
			 */
			if (MONO_TYPE_ISSTRUCT (t)) {
				align = MAX (align, sizeof (target_mgreg_t));
				align = MAX (align, mono_class_min_align (mono_class_from_mono_type_internal (t)));
				/* 
				 * Align the size too so the code generated for passing vtypes in
				 * registers doesn't overwrite random locals.
				 */
				size = (size + (align - 1)) & ~(align -1);
			}

			if (backward) {
				offset += size;
				offset += align - 1;
				offset &= ~(align - 1);
				slot = offset;
			}
			else {
				offset += align - 1;
				offset &= ~(align - 1);
				slot = offset;
				offset += size;
			}

			*stack_align = MAX (*stack_align, align);
		}

		offsets [vmv->idx] = slot;
	}
	g_list_free (vars);
	for (i = 0; i < MONO_TYPE_PINNED; ++i) {
		if (scalar_stack_slots [i].active)
			g_list_free (scalar_stack_slots [i].active);
	}
	for (i = 0; i < nvtypes; ++i) {
		if (vtype_stack_slots [i].active)
			g_list_free (vtype_stack_slots [i].active);
	}

	cfg->stat_locals_stack_size += offset;

	*stack_size = offset;
	return offsets;
}

#define EMUL_HIT_SHIFT 3
#define EMUL_HIT_MASK ((1 << EMUL_HIT_SHIFT) - 1)
/* small hit bitmap cache */
static mono_byte emul_opcode_hit_cache [(OP_LAST>>EMUL_HIT_SHIFT) + 1] = {0};
static short emul_opcode_num = 0;
static short emul_opcode_alloced = 0;
static short *emul_opcode_opcodes;
static MonoJitICallInfo **emul_opcode_map;

MonoJitICallInfo *
mono_find_jit_opcode_emulation (int opcode)
{
	g_assert (opcode >= 0 && opcode <= OP_LAST);
	if (emul_opcode_hit_cache [opcode >> (EMUL_HIT_SHIFT + 3)] & (1 << (opcode & EMUL_HIT_MASK))) {
		int i;
		for (i = 0; i < emul_opcode_num; ++i) {
			if (emul_opcode_opcodes [i] == opcode)
				return emul_opcode_map [i];
		}
	}
	return NULL;
}

void
mini_register_opcode_emulation (int opcode, MonoJitICallInfo *info, const char *name, MonoMethodSignature *sig, gpointer func, const char *symbol, gboolean no_wrapper)
{
	g_assert (info);
	g_assert (!sig->hasthis);
	g_assert (sig->param_count < 3);

	mono_register_jit_icall_info (info, func, name, sig, no_wrapper, symbol);

	if (emul_opcode_num >= emul_opcode_alloced) {
		int incr = emul_opcode_alloced? emul_opcode_alloced/2: 16;
		emul_opcode_alloced += incr;
		emul_opcode_map = (MonoJitICallInfo **)g_realloc (emul_opcode_map, sizeof (emul_opcode_map [0]) * emul_opcode_alloced);
		emul_opcode_opcodes = (short *)g_realloc (emul_opcode_opcodes, sizeof (emul_opcode_opcodes [0]) * emul_opcode_alloced);
	}
	emul_opcode_map [emul_opcode_num] = info;
	emul_opcode_opcodes [emul_opcode_num] = opcode;
	emul_opcode_num++;
	emul_opcode_hit_cache [opcode >> (EMUL_HIT_SHIFT + 3)] |= (1 << (opcode & EMUL_HIT_MASK));
}

static void
print_dfn (MonoCompile *cfg)
{
	int i, j;
	char *code;
	MonoBasicBlock *bb;
	MonoInst *c;

	{
		char *method_name = mono_method_full_name (cfg->method, TRUE);
		g_print ("IR code for method %s\n", method_name);
		g_free (method_name);
	}

	for (i = 0; i < cfg->num_bblocks; ++i) {
		bb = cfg->bblocks [i];
		/*if (bb->cil_code) {
			char* code1, *code2;
			code1 = mono_disasm_code_one (NULL, cfg->method, bb->cil_code, NULL);
			if (bb->last_ins->cil_code)
				code2 = mono_disasm_code_one (NULL, cfg->method, bb->last_ins->cil_code, NULL);
			else
				code2 = g_strdup ("");

			code1 [strlen (code1) - 1] = 0;
			code = g_strdup_printf ("%s -> %s", code1, code2);
			g_free (code1);
			g_free (code2);
		} else*/
			code = g_strdup ("\n");
		g_print ("\nBB%d (%d) (len: %d): %s", bb->block_num, i, bb->cil_length, code);
		MONO_BB_FOR_EACH_INS (bb, c) {
			mono_print_ins_index (-1, c);
		}

		g_print ("\tprev:");
		for (j = 0; j < bb->in_count; ++j) {
			g_print (" BB%d", bb->in_bb [j]->block_num);
		}
		g_print ("\t\tsucc:");
		for (j = 0; j < bb->out_count; ++j) {
			g_print (" BB%d", bb->out_bb [j]->block_num);
		}
		g_print ("\n\tidom: BB%d\n", bb->idom? bb->idom->block_num: -1);

		if (bb->idom)
			g_assert (mono_bitset_test_fast (bb->dominators, bb->idom->dfn));

		if (bb->dominators)
			mono_blockset_print (cfg, bb->dominators, "\tdominators", bb->idom? bb->idom->dfn: -1);
		if (bb->dfrontier)
			mono_blockset_print (cfg, bb->dfrontier, "\tdfrontier", -1);
		g_free (code);
	}

	g_print ("\n");
}

void
mono_bblock_add_inst (MonoBasicBlock *bb, MonoInst *inst)
{
	MONO_ADD_INS (bb, inst);
}

void
mono_bblock_insert_after_ins (MonoBasicBlock *bb, MonoInst *ins, MonoInst *ins_to_insert)
{
	if (ins == NULL) {
		ins = bb->code;
		bb->code = ins_to_insert;

		/* Link with next */
		ins_to_insert->next = ins;
		if (ins)
			ins->prev = ins_to_insert;

		if (bb->last_ins == NULL)
			bb->last_ins = ins_to_insert;
	} else {
		/* Link with next */
		ins_to_insert->next = ins->next;
		if (ins->next)
			ins->next->prev = ins_to_insert;

		/* Link with previous */
		ins->next = ins_to_insert;
		ins_to_insert->prev = ins;

		if (bb->last_ins == ins)
			bb->last_ins = ins_to_insert;
	}
}

void
mono_bblock_insert_before_ins (MonoBasicBlock *bb, MonoInst *ins, MonoInst *ins_to_insert)
{
	if (ins == NULL) {
		ins = bb->code;
		if (ins)
			ins->prev = ins_to_insert;
		bb->code = ins_to_insert;
		ins_to_insert->next = ins;
		if (bb->last_ins == NULL)
			bb->last_ins = ins_to_insert;
	} else {
		/* Link with previous */
		if (ins->prev)
			ins->prev->next = ins_to_insert;
		ins_to_insert->prev = ins->prev;

		/* Link with next */
		ins->prev = ins_to_insert;
		ins_to_insert->next = ins;

		if (bb->code == ins)
			bb->code = ins_to_insert;
	}
}

/*
 * mono_verify_bblock:
 *
 *   Verify that the next and prev pointers are consistent inside the instructions in BB.
 */
void
mono_verify_bblock (MonoBasicBlock *bb)
{
	MonoInst *ins, *prev;

	prev = NULL;
	for (ins = bb->code; ins; ins = ins->next) {
		g_assert (ins->prev == prev);
		prev = ins;
	}
	if (bb->last_ins)
		g_assert (!bb->last_ins->next);
}

/*
 * mono_verify_cfg:
 *
 *   Perform consistency checks on the JIT data structures and the IR
 */
void
mono_verify_cfg (MonoCompile *cfg)
{
	MonoBasicBlock *bb;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
		mono_verify_bblock (bb);
}

// This will free many fields in cfg to save
// memory. Note that this must be safe to call
// multiple times. It must be idempotent. 
void
mono_empty_compile (MonoCompile *cfg)
{
	mono_free_loop_info (cfg);

	// These live in the mempool, and so must be freed
	// first
	for (GSList *l = cfg->headers_to_free; l; l = l->next) {
		mono_metadata_free_mh ((MonoMethodHeader *)l->data);
	}
	cfg->headers_to_free = NULL;

	if (cfg->mempool) {
	//mono_mempool_stats (cfg->mempool);
		mono_mempool_destroy (cfg->mempool);
		cfg->mempool = NULL;
	}

	g_free (cfg->varinfo);
	cfg->varinfo = NULL;

	g_free (cfg->vars);
	cfg->vars = NULL;

	if (cfg->rs) {
		mono_regstate_free (cfg->rs);
		cfg->rs = NULL;
	}
}

void
mono_destroy_compile (MonoCompile *cfg)
{
	mono_empty_compile (cfg);

	mono_metadata_free_mh (cfg->header);

	g_hash_table_destroy (cfg->spvars);
	g_hash_table_destroy (cfg->exvars);
	g_list_free (cfg->ldstr_list);
	g_hash_table_destroy (cfg->token_info_hash);
	g_hash_table_destroy (cfg->abs_patches);

	mono_debug_free_method (cfg);

	g_free (cfg->varinfo);
	g_free (cfg->vars);
	g_free (cfg->exception_message);
	g_free (cfg);
}

void
mono_add_patch_info (MonoCompile *cfg, int ip, MonoJumpInfoType type, gconstpointer target)
{
	if (type == MONO_PATCH_INFO_NONE)
		return;

	MonoJumpInfo *ji = (MonoJumpInfo *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoJumpInfo));

	ji->ip.i = ip;
	ji->type = type;
	ji->data.target = target;
	ji->next = cfg->patch_info;

	cfg->patch_info = ji;
}

void
mono_add_patch_info_rel (MonoCompile *cfg, int ip, MonoJumpInfoType type, gconstpointer target, int relocation)
{
	if (type == MONO_PATCH_INFO_NONE)
		return;

	MonoJumpInfo *ji = (MonoJumpInfo *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoJumpInfo));

	ji->ip.i = ip;
	ji->type = type;
	ji->relocation = relocation;
	ji->data.target = target;
	ji->next = cfg->patch_info;

	cfg->patch_info = ji;
}

void
mono_remove_patch_info (MonoCompile *cfg, int ip)
{
	MonoJumpInfo **ji = &cfg->patch_info;

	while (*ji) {
		if ((*ji)->ip.i == ip)
			*ji = (*ji)->next;
		else
			ji = &((*ji)->next);
	}
}

void
mono_add_seq_point (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, int native_offset)
{
	ins->inst_offset = native_offset;
	g_ptr_array_add (cfg->seq_points, ins);
	if (bb) {
		bb->seq_points = g_slist_prepend_mempool (cfg->mempool, bb->seq_points, ins);
		bb->last_seq_point = ins;
	}
}

void
mono_add_var_location (MonoCompile *cfg, MonoInst *var, gboolean is_reg, int reg, int offset, int from, int to)
{
	MonoDwarfLocListEntry *entry = (MonoDwarfLocListEntry *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoDwarfLocListEntry));

	if (is_reg)
		g_assert (offset == 0);

	entry->is_reg = is_reg;
	entry->reg = reg;
	entry->offset = offset;
	entry->from = from;
	entry->to = to;

	if (var == cfg->args [0])
		cfg->this_loclist = g_slist_append_mempool (cfg->mempool, cfg->this_loclist, entry);
	else if (var == cfg->rgctx_var)
		cfg->rgctx_loclist = g_slist_append_mempool (cfg->mempool, cfg->rgctx_loclist, entry);
}

static void
mono_apply_volatile (MonoInst *inst, MonoBitSet *set, gsize index)
{
	inst->flags |= mono_bitset_test_safe (set, index) ? MONO_INST_VOLATILE : 0;
}

static void
mono_compile_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	int i;

	header = cfg->header;

	sig = mono_method_signature_internal (cfg->method);
	
	if (!MONO_TYPE_IS_VOID (sig->ret)) {
		cfg->ret = mono_compile_create_var (cfg, sig->ret, OP_ARG);
		/* Inhibit optimizations */
		cfg->ret->flags |= MONO_INST_VOLATILE;
	}
	if (cfg->verbose_level > 2)
		g_print ("creating vars\n");

	cfg->args = (MonoInst **)mono_mempool_alloc0 (cfg->mempool, (sig->param_count + sig->hasthis) * sizeof (MonoInst*));

	if (sig->hasthis) {
		MonoInst* arg = mono_compile_create_var (cfg, m_class_get_this_arg (cfg->method->klass), OP_ARG);
		mono_apply_volatile (arg, header->volatile_args, 0);
		cfg->args [0] = arg;
		cfg->this_arg = arg;
	}

	for (i = 0; i < sig->param_count; ++i) {
		MonoInst* arg = mono_compile_create_var (cfg, sig->params [i], OP_ARG);
		mono_apply_volatile (arg, header->volatile_args, i + sig->hasthis);
		cfg->args [i + sig->hasthis] = arg;
	}

	if (cfg->verbose_level > 2) {
		if (cfg->ret) {
			printf ("\treturn : ");
			mono_print_ins (cfg->ret);
		}

		if (sig->hasthis) {
			printf ("\tthis: ");
			mono_print_ins (cfg->args [0]);
		}

		for (i = 0; i < sig->param_count; ++i) {
			printf ("\targ [%d]: ", i);
			mono_print_ins (cfg->args [i + sig->hasthis]);
		}
	}

	cfg->locals_start = cfg->num_varinfo;
	cfg->locals = (MonoInst **)mono_mempool_alloc0 (cfg->mempool, header->num_locals * sizeof (MonoInst*));

	if (cfg->verbose_level > 2)
		g_print ("creating locals\n");

	for (i = 0; i < header->num_locals; ++i) {
		if (cfg->verbose_level > 2)
			g_print ("\tlocal [%d]: ", i);
		cfg->locals [i] = mono_compile_create_var (cfg, header->locals [i], OP_LOCAL);
		mono_apply_volatile (cfg->locals [i], header->volatile_locals, i);
	}

	if (cfg->verbose_level > 2)
		g_print ("locals done\n");

#ifdef ENABLE_LLVM
	if (COMPILE_LLVM (cfg))
		mono_llvm_create_vars (cfg);
	else
		mono_arch_create_vars (cfg);
#else
	mono_arch_create_vars (cfg);
#endif

	if (cfg->method->save_lmf && cfg->create_lmf_var) {
		MonoInst *lmf_var = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		lmf_var->flags |= MONO_INST_VOLATILE;
		lmf_var->flags |= MONO_INST_LMF;
		cfg->lmf_var = lmf_var;
	}
}

void
mono_print_code (MonoCompile *cfg, const char* msg)
{
	MonoBasicBlock *bb;
	
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
		mono_print_bb (bb, msg);
}

static void
mono_postprocess_patches (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int i;

	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_ABS: {
			/*
			 * Change patches of type MONO_PATCH_INFO_ABS into patches describing the
			 * absolute address.
			 */
			if (cfg->abs_patches) {
				MonoJumpInfo *abs_ji = (MonoJumpInfo *)g_hash_table_lookup (cfg->abs_patches, patch_info->data.target);
				if (abs_ji) {
					patch_info->type = abs_ji->type;
					patch_info->data.target = abs_ji->data.target;
				}
			}
			break;
		}
		case MONO_PATCH_INFO_SWITCH: {
			gpointer *table;
			if (cfg->method->dynamic) {
				table = (void **)mono_code_manager_reserve (cfg->dynamic_info->code_mp, sizeof (gpointer) * patch_info->data.table->table_size);
			} else {
				table = (void **)mono_mem_manager_code_reserve (cfg->mem_manager, sizeof (gpointer) * patch_info->data.table->table_size);
			}

			for (i = 0; i < patch_info->data.table->table_size; i++) {
				/* Might be NULL if the switch is eliminated */
				if (patch_info->data.table->table [i]) {
					g_assert (patch_info->data.table->table [i]->native_offset);
					table [i] = GINT_TO_POINTER (patch_info->data.table->table [i]->native_offset);
				} else {
					table [i] = NULL;
				}
			}
			patch_info->data.table->table = (MonoBasicBlock**)table;
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}
}

/* Those patches require the JitInfo of the compiled method already be in place when used */
static void
mono_postprocess_patches_after_ji_publish (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;

	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_METHOD_JUMP: {
			unsigned char *ip = cfg->native_code + patch_info->ip.i;

			mini_register_jump_site (cfg->domain, patch_info->data.method, ip);
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}
}

void
mono_codegen (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	int max_epilog_size;
	guint8 *code;
	MonoMemoryManager *code_mem_manager;
	guint unwindlen = 0;

	if (mono_using_xdebug)
		/*
		 * Recent gdb versions have trouble processing symbol files containing
		 * overlapping address ranges, so allocate all code from the code manager
		 * of the root domain. (#666152).
		 */
		code_mem_manager = mono_domain_memory_manager (mono_get_root_domain ());
	else
		code_mem_manager = cfg->mem_manager;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		cfg->spill_count = 0;
		/* we reuse dfn here */
		/* bb->dfn = bb_count++; */

		mono_arch_lowering_pass (cfg, bb);

		if (cfg->opt & MONO_OPT_PEEPHOLE)
			mono_arch_peephole_pass_1 (cfg, bb);

		mono_local_regalloc (cfg, bb);

		if (cfg->opt & MONO_OPT_PEEPHOLE)
			mono_arch_peephole_pass_2 (cfg, bb);

		if (cfg->gen_seq_points && !cfg->gen_sdb_seq_points)
			mono_bb_deduplicate_op_il_seq_points (cfg, bb);
	}

	code = mono_arch_emit_prolog (cfg);

	set_code_cursor (cfg, code);
	cfg->prolog_end = cfg->code_len;
	cfg->cfa_reg = cfg->cur_cfa_reg;
	cfg->cfa_offset = cfg->cur_cfa_offset;

	mono_debug_open_method (cfg);

	/* emit code all basic blocks */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		bb->native_offset = cfg->code_len;
		bb->real_native_offset = cfg->code_len;
		//if ((bb == cfg->bb_entry) || !(bb->region == -1 && !bb->dfn))
			mono_arch_output_basic_block (cfg, bb);
		bb->native_length = cfg->code_len - bb->native_offset;

		if (bb == cfg->bb_exit) {
			cfg->epilog_begin = cfg->code_len;
			mono_arch_emit_epilog (cfg);
			cfg->epilog_end = cfg->code_len;
		}

		if (bb->clause_holes) {
			GList *tmp;
			for (tmp = bb->clause_holes; tmp; tmp = tmp->prev)
				mono_cfg_add_try_hole (cfg, ((MonoLeaveClause *) tmp->data)->clause, cfg->native_code + bb->native_offset, bb);
		}
	}

	mono_arch_emit_exceptions (cfg);

	max_epilog_size = 0;

	/* we always allocate code in cfg->domain->code_mp to increase locality */
	cfg->code_size = cfg->code_len + max_epilog_size;

	/* fixme: align to MONO_ARCH_CODE_ALIGNMENT */

#ifdef MONO_ARCH_HAVE_UNWIND_TABLE
	if (!cfg->compile_aot)
		unwindlen = mono_arch_unwindinfo_init_method_unwind_info (cfg);
#endif

	if (cfg->method->dynamic) {
		/* Allocate the code into a separate memory pool so it can be freed */
		cfg->dynamic_info = g_new0 (MonoJitDynamicMethodInfo, 1);
		cfg->dynamic_info->code_mp = mono_code_manager_new_dynamic ();

		MonoJitMemoryManager *jit_mm = (MonoJitMemoryManager*)cfg->jit_mm;
		jit_mm_lock (jit_mm);
		if (!jit_mm->dynamic_code_hash)
			jit_mm->dynamic_code_hash = g_hash_table_new (NULL, NULL);
		g_hash_table_insert (jit_mm->dynamic_code_hash, cfg->method, cfg->dynamic_info);
		jit_mm_unlock (jit_mm);

		if (mono_using_xdebug)
			/* See the comment for cfg->code_domain */
			code = (guint8 *)mono_mem_manager_code_reserve (code_mem_manager, cfg->code_size + cfg->thunk_area + unwindlen);
		else
			code = (guint8 *)mono_code_manager_reserve (cfg->dynamic_info->code_mp, cfg->code_size + cfg->thunk_area + unwindlen);
	} else {
		code = (guint8 *)mono_mem_manager_code_reserve (code_mem_manager, cfg->code_size + cfg->thunk_area + unwindlen);
	}

	mono_codeman_enable_write ();

	if (cfg->thunk_area) {
		cfg->thunks_offset = cfg->code_size + unwindlen;
		cfg->thunks = code + cfg->thunks_offset;
		memset (cfg->thunks, 0, cfg->thunk_area);
	}

	g_assert (code);
	memcpy (code, cfg->native_code, cfg->code_len);
	g_free (cfg->native_code);
	cfg->native_code = code;
	code = cfg->native_code + cfg->code_len;
  
	/* g_assert (((int)cfg->native_code & (MONO_ARCH_CODE_ALIGNMENT - 1)) == 0); */
	mono_postprocess_patches (cfg);

#ifdef VALGRIND_JIT_REGISTER_MAP
	if (valgrind_register){
		char* nm = mono_method_full_name (cfg->method, TRUE);
		VALGRIND_JIT_REGISTER_MAP (nm, cfg->native_code, cfg->native_code + cfg->code_len);
		g_free (nm);
	}
#endif
 
	if (cfg->verbose_level > 0) {
		char* nm = mono_method_get_full_name (cfg->method);
		g_print ("Method %s emitted at %p to %p (code length %d) [%s]\n",
				 nm, 
				 cfg->native_code, cfg->native_code + cfg->code_len, cfg->code_len, cfg->domain->friendly_name);
		g_free (nm);
	}

	{
		gboolean is_generic = FALSE;

		if (cfg->method->is_inflated || mono_method_get_generic_container (cfg->method) ||
				mono_class_is_gtd (cfg->method->klass) || mono_class_is_ginst (cfg->method->klass)) {
			is_generic = TRUE;
		}

		if (cfg->gshared)
			g_assert (is_generic);
	}

#ifdef MONO_ARCH_HAVE_SAVE_UNWIND_INFO
	mono_arch_save_unwind_info (cfg);
#endif

	{
		MonoJumpInfo *ji;
		gpointer target;

		for (ji = cfg->patch_info; ji; ji = ji->next) {
			if (cfg->compile_aot) {
				switch (ji->type) {
				case MONO_PATCH_INFO_BB:
				case MONO_PATCH_INFO_LABEL:
					break;
				default:
					/* No need to patch these */
					continue;
				}
			}

			if (ji->type == MONO_PATCH_INFO_NONE)
				continue;

			target = mono_resolve_patch_target (cfg->method, cfg->native_code, ji, cfg->run_cctors, cfg->error);
			if (!is_ok (cfg->error)) {
				mono_cfg_set_exception (cfg, MONO_EXCEPTION_MONO_ERROR);
				return;
			}
			mono_arch_patch_code_new (cfg, cfg->native_code, ji, target);
		}
	}

	if (cfg->method->dynamic) {
		if (mono_using_xdebug)
			mono_mem_manager_code_commit (code_mem_manager, cfg->native_code, cfg->code_size, cfg->code_len);
		else
			mono_code_manager_commit (cfg->dynamic_info->code_mp, cfg->native_code, cfg->code_size, cfg->code_len);
	} else {
		mono_mem_manager_code_commit (code_mem_manager, cfg->native_code, cfg->code_size, cfg->code_len);
	}

	mono_codeman_disable_write ();

	MONO_PROFILER_RAISE (jit_code_buffer, (cfg->native_code, cfg->code_len, MONO_PROFILER_CODE_BUFFER_METHOD, cfg->method));
	
	mono_arch_flush_icache (cfg->native_code, cfg->code_len);

	mono_debug_close_method (cfg);

#ifdef MONO_ARCH_HAVE_UNWIND_TABLE
	if (!cfg->compile_aot)
		mono_arch_unwindinfo_install_method_unwind_info (&cfg->arch.unwindinfo, cfg->native_code, cfg->code_len);
#endif
}

static void
compute_reachable (MonoBasicBlock *bb)
{
	int i;

	if (!(bb->flags & BB_VISITED)) {
		bb->flags |= BB_VISITED;
		for (i = 0; i < bb->out_count; ++i)
			compute_reachable (bb->out_bb [i]);
	}
}

static void mono_bb_ordering (MonoCompile *cfg)
{
	int dfn = 0;
	/* Depth-first ordering on basic blocks */
	cfg->bblocks = (MonoBasicBlock **)mono_mempool_alloc (cfg->mempool, sizeof (MonoBasicBlock*) * (cfg->num_bblocks + 1));

	cfg->max_block_num = cfg->num_bblocks;

	df_visit (cfg->bb_entry, &dfn, cfg->bblocks);

#if defined(__GNUC__) && __GNUC__ == 7 && defined(__x86_64__)
	/* workaround for an AMD specific issue that only happens on GCC 7 so far,
	 * for more information see https://github.com/mono/mono/issues/9298 */
	mono_memory_barrier ();
#endif
	g_assertf (cfg->num_bblocks >= dfn, "cfg->num_bblocks=%d, dfn=%d\n", cfg->num_bblocks, dfn);

	if (cfg->num_bblocks != dfn + 1) {
		MonoBasicBlock *bb;

		cfg->num_bblocks = dfn + 1;

		/* remove unreachable code, because the code in them may be 
		 * inconsistent  (access to dead variables for example) */
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
			bb->flags &= ~BB_VISITED;
		compute_reachable (cfg->bb_entry);
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
			if (bb->flags & BB_EXCEPTION_HANDLER)
				compute_reachable (bb);
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			if (!(bb->flags & BB_VISITED)) {
				if (cfg->verbose_level > 1)
					g_print ("found unreachable code in BB%d\n", bb->block_num);
				bb->code = bb->last_ins = NULL;
				while (bb->out_count)
					mono_unlink_bblock (cfg, bb, bb->out_bb [0]);
			}
		}
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
			bb->flags &= ~BB_VISITED;
	}
}

static void
mono_handle_out_of_line_bblock (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (bb->next_bb && bb->next_bb->out_of_line && bb->last_ins && !MONO_IS_BRANCH_OP (bb->last_ins)) {
			MonoInst *ins;
			MONO_INST_NEW (cfg, ins, OP_BR);
			MONO_ADD_INS (bb, ins);
			ins->inst_target_bb = bb->next_bb;
		}
	}
}

static MonoJitInfo*
create_jit_info (MonoCompile *cfg, MonoMethod *method_to_compile)
{
	GSList *tmp;
	MonoMethodHeader *header;
	MonoJitInfo *jinfo;
	MonoJitInfoFlags flags = JIT_INFO_NONE;
	int num_clauses, num_holes = 0;
	guint32 stack_size = 0;

	g_assert (method_to_compile == cfg->method);
	header = cfg->header;

	if (cfg->gshared)
		flags |= JIT_INFO_HAS_GENERIC_JIT_INFO;

	if (cfg->arch_eh_jit_info) {
		MonoJitArgumentInfo *arg_info;
		MonoMethodSignature *sig = mono_method_signature_internal (cfg->method_to_register);

		/*
		 * This cannot be computed during stack walking, as
		 * mono_arch_get_argument_info () is not signal safe.
		 */
		arg_info = g_newa (MonoJitArgumentInfo, sig->param_count + 1);
		stack_size = mono_arch_get_argument_info (sig, sig->param_count, arg_info);

		if (stack_size)
			flags |= JIT_INFO_HAS_ARCH_EH_INFO;
	}

	if (cfg->has_unwind_info_for_epilog && !(flags & JIT_INFO_HAS_ARCH_EH_INFO))
		flags |= JIT_INFO_HAS_ARCH_EH_INFO;

	if (cfg->thunk_area)
		flags |= JIT_INFO_HAS_THUNK_INFO;

	if (cfg->try_block_holes) {
		for (tmp = cfg->try_block_holes; tmp; tmp = tmp->next) {
			TryBlockHole *hole = (TryBlockHole *)tmp->data;
			MonoExceptionClause *ec = hole->clause;
			int hole_end = hole->basic_block->native_offset + hole->basic_block->native_length;
			MonoBasicBlock *clause_last_bb = cfg->cil_offset_to_bb [ec->try_offset + ec->try_len];
			g_assert (clause_last_bb);

			/* Holes at the end of a try region can be represented by simply reducing the size of the block itself.*/
			if (clause_last_bb->native_offset != hole_end)
				++num_holes;
		}
		if (num_holes)
			flags |= JIT_INFO_HAS_TRY_BLOCK_HOLES;
		if (G_UNLIKELY (cfg->verbose_level >= 4))
			printf ("Number of try block holes %d\n", num_holes);
	}

	if (COMPILE_LLVM (cfg))
		num_clauses = cfg->llvm_ex_info_len;
	else
		num_clauses = header->num_clauses;

	if (cfg->method->dynamic)
		jinfo = (MonoJitInfo *)g_malloc0 (mono_jit_info_size (flags, num_clauses, num_holes));
	else
		jinfo = (MonoJitInfo *)mono_mem_manager_alloc0 (cfg->mem_manager, mono_jit_info_size (flags, num_clauses, num_holes));
	jinfo_try_holes_size += num_holes * sizeof (MonoTryBlockHoleJitInfo);

	mono_jit_info_init (jinfo, cfg->method_to_register, cfg->native_code, cfg->code_len, flags, num_clauses, num_holes);

	if (COMPILE_LLVM (cfg))
		jinfo->from_llvm = TRUE;

	if (cfg->gshared) {
		MonoInst *inst;
		MonoGenericJitInfo *gi;
		GSList *loclist = NULL;

		gi = mono_jit_info_get_generic_jit_info (jinfo);
		g_assert (gi);

		if (cfg->method->dynamic)
			gi->generic_sharing_context = g_new0 (MonoGenericSharingContext, 1);
		else
			gi->generic_sharing_context = (MonoGenericSharingContext *)mono_mem_manager_alloc0 (cfg->mem_manager, sizeof (MonoGenericSharingContext));
		mini_init_gsctx (cfg->method->dynamic ? NULL : cfg->domain, NULL, cfg->gsctx_context, gi->generic_sharing_context);

		if ((method_to_compile->flags & METHOD_ATTRIBUTE_STATIC) ||
				mini_method_get_context (method_to_compile)->method_inst ||
				m_class_is_valuetype (method_to_compile->klass)) {
			g_assert (cfg->rgctx_var);
		}

		gi->has_this = 1;

		if ((method_to_compile->flags & METHOD_ATTRIBUTE_STATIC) ||
				mini_method_get_context (method_to_compile)->method_inst ||
				m_class_is_valuetype (method_to_compile->klass)) {
			inst = cfg->rgctx_var;
			if (!COMPILE_LLVM (cfg))
				g_assert (inst->opcode == OP_REGOFFSET);
			loclist = cfg->rgctx_loclist;
		} else {
			inst = cfg->args [0];
			loclist = cfg->this_loclist;
		}

		if (loclist) {
			/* Needed to handle async exceptions */
			GSList *l;
			int i;

			gi->nlocs = g_slist_length (loclist);
			if (cfg->method->dynamic)
				gi->locations = (MonoDwarfLocListEntry *)g_malloc0 (gi->nlocs * sizeof (MonoDwarfLocListEntry));
			else
				gi->locations = (MonoDwarfLocListEntry *)mono_mem_manager_alloc0 (cfg->mem_manager, gi->nlocs * sizeof (MonoDwarfLocListEntry));
			i = 0;
			for (l = loclist; l; l = l->next) {
				memcpy (&(gi->locations [i]), l->data, sizeof (MonoDwarfLocListEntry));
				i ++;
			}
		}

		if (COMPILE_LLVM (cfg)) {
			g_assert (cfg->llvm_this_reg != -1);
			gi->this_in_reg = 0;
			gi->this_reg = cfg->llvm_this_reg;
			gi->this_offset = cfg->llvm_this_offset;
		} else if (inst->opcode == OP_REGVAR) {
			gi->this_in_reg = 1;
			gi->this_reg = inst->dreg;
		} else {
			g_assert (inst->opcode == OP_REGOFFSET);
#ifdef TARGET_X86
			g_assert (inst->inst_basereg == X86_EBP);
#elif defined(TARGET_AMD64)
			g_assert (inst->inst_basereg == X86_EBP || inst->inst_basereg == X86_ESP);
#endif
			g_assert (inst->inst_offset >= G_MININT32 && inst->inst_offset <= G_MAXINT32);

			gi->this_in_reg = 0;
			gi->this_reg = inst->inst_basereg;
			gi->this_offset = inst->inst_offset;
		}
	}

	if (num_holes) {
		MonoTryBlockHoleTableJitInfo *table;
		int i;

		table = mono_jit_info_get_try_block_hole_table_info (jinfo);
		table->num_holes = (guint16)num_holes;
		i = 0;
		for (tmp = cfg->try_block_holes; tmp; tmp = tmp->next) {
			guint32 start_bb_offset;
			MonoTryBlockHoleJitInfo *hole;
			TryBlockHole *hole_data = (TryBlockHole *)tmp->data;
			MonoExceptionClause *ec = hole_data->clause;
			int hole_end = hole_data->basic_block->native_offset + hole_data->basic_block->native_length;
			MonoBasicBlock *clause_last_bb = cfg->cil_offset_to_bb [ec->try_offset + ec->try_len];
			g_assert (clause_last_bb);

			/* Holes at the end of a try region can be represented by simply reducing the size of the block itself.*/
			if (clause_last_bb->native_offset == hole_end)
				continue;

			start_bb_offset = hole_data->start_offset - hole_data->basic_block->native_offset;
			hole = &table->holes [i++];
			hole->clause = hole_data->clause - &header->clauses [0];
			hole->offset = (guint32)hole_data->start_offset;
			hole->length = (guint16)(hole_data->basic_block->native_length - start_bb_offset);

			if (G_UNLIKELY (cfg->verbose_level >= 4))
				printf ("\tTry block hole at eh clause %d offset %x length %x\n", hole->clause, hole->offset, hole->length);
		}
		g_assert (i == num_holes);
	}

	if (jinfo->has_arch_eh_info) {
		MonoArchEHJitInfo *info;

		info = mono_jit_info_get_arch_eh_info (jinfo);

		info->stack_size = stack_size;
	}

	if (cfg->thunk_area) {
		MonoThunkJitInfo *info;

		info = mono_jit_info_get_thunk_info (jinfo);
		info->thunks_offset = cfg->thunks_offset;
		info->thunks_size = cfg->thunk_area;
	}

	if (COMPILE_LLVM (cfg)) {
		if (num_clauses)
			memcpy (&jinfo->clauses [0], &cfg->llvm_ex_info [0], num_clauses * sizeof (MonoJitExceptionInfo));
	} else if (header->num_clauses) {
		int i;

		for (i = 0; i < header->num_clauses; i++) {
			MonoExceptionClause *ec = &header->clauses [i];
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];
			MonoBasicBlock *tblock;
			MonoInst *exvar;

			ei->flags = ec->flags;

			if (G_UNLIKELY (cfg->verbose_level >= 4))
				printf ("IL clause: try 0x%x-0x%x handler 0x%x-0x%x filter 0x%x\n", ec->try_offset, ec->try_offset + ec->try_len, ec->handler_offset, ec->handler_offset + ec->handler_len, ec->flags == MONO_EXCEPTION_CLAUSE_FILTER ? ec->data.filter_offset : 0);

			exvar = mono_find_exvar_for_offset (cfg, ec->handler_offset);
			ei->exvar_offset = exvar ? exvar->inst_offset : 0;

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
				tblock = cfg->cil_offset_to_bb [ec->data.filter_offset];
				g_assert (tblock);
				ei->data.filter = cfg->native_code + tblock->native_offset;
			} else {
				ei->data.catch_class = ec->data.catch_class;
			}

			tblock = cfg->cil_offset_to_bb [ec->try_offset];
			g_assert (tblock);
			g_assert (tblock->native_offset);
			ei->try_start = cfg->native_code + tblock->native_offset;
			if (tblock->extend_try_block) {
				/*
				 * Extend the try block backwards to include parts of the previous call
				 * instruction.
				 */
				ei->try_start = (guint8*)ei->try_start - cfg->backend->monitor_enter_adjustment;
			}
			if (ec->try_offset + ec->try_len < header->code_size)
				tblock = cfg->cil_offset_to_bb [ec->try_offset + ec->try_len];
			else
				tblock = cfg->bb_exit;
			if (G_UNLIKELY (cfg->verbose_level >= 4))
				printf ("looking for end of try [%d, %d] -> %p (code size %d)\n", ec->try_offset, ec->try_len, tblock, header->code_size);
			g_assert (tblock);
			if (!tblock->native_offset) {
				int j, end;
				for (j = ec->try_offset + ec->try_len, end = ec->try_offset; j >= end; --j) {
					MonoBasicBlock *bb = cfg->cil_offset_to_bb [j];
					if (bb && bb->native_offset) {
						tblock = bb;
						break;
					}
				}
			}
			ei->try_end = cfg->native_code + tblock->native_offset;
			g_assert (tblock->native_offset);
			tblock = cfg->cil_offset_to_bb [ec->handler_offset];
			g_assert (tblock);
			ei->handler_start = cfg->native_code + tblock->native_offset;

			for (tmp = cfg->try_block_holes; tmp; tmp = tmp->next) {
				TryBlockHole *hole = (TryBlockHole *)tmp->data;
				gpointer hole_end = cfg->native_code + (hole->basic_block->native_offset + hole->basic_block->native_length);
				if (hole->clause == ec && hole_end == ei->try_end) {
					if (G_UNLIKELY (cfg->verbose_level >= 4))
						printf ("\tShortening try block %d from %x to %x\n", i, (int)((guint8*)ei->try_end - cfg->native_code), hole->start_offset);

					ei->try_end = cfg->native_code + hole->start_offset;
					break;
				}
			}

			if (ec->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
				int end_offset;
				if (ec->handler_offset + ec->handler_len < header->code_size) {
					tblock = cfg->cil_offset_to_bb [ec->handler_offset + ec->handler_len];
					if (tblock->native_offset) {
						end_offset = tblock->native_offset;
					} else {
						int j, end;

						for (j = ec->handler_offset + ec->handler_len, end = ec->handler_offset; j >= end; --j) {
							MonoBasicBlock *bb = cfg->cil_offset_to_bb [j];
							if (bb && bb->native_offset) {
								tblock = bb;
								break;
							}
						}
						end_offset = tblock->native_offset +  tblock->native_length;
					}
				} else {
					end_offset = cfg->epilog_begin;
				}
				ei->data.handler_end = cfg->native_code + end_offset;
			}

			/* Keep try_start/end non-authenticated, they are never branched to */
			//ei->try_start = MINI_ADDR_TO_FTNPTR (ei->try_start);
			//ei->try_end = MINI_ADDR_TO_FTNPTR (ei->try_end);
			ei->handler_start = MINI_ADDR_TO_FTNPTR (ei->handler_start);
			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
				ei->data.filter = MINI_ADDR_TO_FTNPTR (ei->data.filter);
			else if (ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY)
				ei->data.handler_end = MINI_ADDR_TO_FTNPTR (ei->data.handler_end);
		}
	}

	if (G_UNLIKELY (cfg->verbose_level >= 4)) {
		int i;
		for (i = 0; i < jinfo->num_clauses; i++) {
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];
			int start = (guint8*)ei->try_start - cfg->native_code;
			int end = (guint8*)ei->try_end - cfg->native_code;
			int handler = (guint8*)ei->handler_start - cfg->native_code;
			int handler_end = (guint8*)ei->data.handler_end - cfg->native_code;

			printf ("JitInfo EH clause %d flags %x try %x-%x handler %x-%x\n", i, ei->flags, start, end, handler, handler_end);
		}
	}

	if (cfg->encoded_unwind_ops) {
		/* Generated by LLVM */
		jinfo->unwind_info = mono_cache_unwind_info (cfg->encoded_unwind_ops, cfg->encoded_unwind_ops_len);
		g_free (cfg->encoded_unwind_ops);
	} else if (cfg->unwind_ops) {
		guint32 info_len;
		guint8 *unwind_info = mono_unwind_ops_encode (cfg->unwind_ops, &info_len);
		guint32 unwind_desc;

		unwind_desc = mono_cache_unwind_info (unwind_info, info_len);

		if (cfg->has_unwind_info_for_epilog) {
			MonoArchEHJitInfo *info;

			info = mono_jit_info_get_arch_eh_info (jinfo);
			g_assert (info);
			info->epilog_size = cfg->code_len - cfg->epilog_begin;
		}
		jinfo->unwind_info = unwind_desc;
		g_free (unwind_info);
	} else {
		jinfo->unwind_info = cfg->used_int_regs;
	}

	return jinfo;
}

/* Return whenever METHOD is a gsharedvt method */
static gboolean
is_gsharedvt_method (MonoMethod *method)
{
	MonoGenericContext *context;
	MonoGenericInst *inst;
	int i;

	if (!method->is_inflated)
		return FALSE;
	context = mono_method_get_context (method);
	inst = context->class_inst;
	if (inst) {
		for (i = 0; i < inst->type_argc; ++i)
			if (mini_is_gsharedvt_gparam (inst->type_argv [i]))
				return TRUE;
	}
	inst = context->method_inst;
	if (inst) {
		for (i = 0; i < inst->type_argc; ++i)
			if (mini_is_gsharedvt_gparam (inst->type_argv [i]))
				return TRUE;
	}
	return FALSE;
}

static gboolean
is_open_method (MonoMethod *method)
{
	MonoGenericContext *context;

	if (!method->is_inflated)
		return FALSE;
	context = mono_method_get_context (method);
	if (context->class_inst && context->class_inst->is_open)
		return TRUE;
	if (context->method_inst && context->method_inst->is_open)
		return TRUE;
	return FALSE;
}

static void
mono_insert_nop_in_empty_bb (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (bb->code)
			continue;
		MonoInst *nop;
		MONO_INST_NEW (cfg, nop, OP_NOP);
		MONO_ADD_INS (bb, nop);
	}
}
static void
insert_safepoint (MonoCompile *cfg, MonoBasicBlock *bblock)
{
	MonoInst *poll_addr, *ins;

	if (cfg->disable_gc_safe_points)
		return;

	if (cfg->verbose_level > 1)
		printf ("ADDING SAFE POINT TO BB %d\n", bblock->block_num);

	g_assert (mini_safepoints_enabled ());
	NEW_AOTCONST (cfg, poll_addr, MONO_PATCH_INFO_GC_SAFE_POINT_FLAG, (gpointer)&mono_polling_required);

	MONO_INST_NEW (cfg, ins, OP_GC_SAFE_POINT);
	ins->sreg1 = poll_addr->dreg;

	if (bblock->flags & BB_EXCEPTION_HANDLER) {
		MonoInst *eh_op = bblock->code;

		if (eh_op && eh_op->opcode != OP_START_HANDLER && eh_op->opcode != OP_GET_EX_OBJ) {
			eh_op = NULL;
		} else {
			MonoInst *next_eh_op = eh_op ? eh_op->next : NULL;
			// skip all EH relateds ops
			while (next_eh_op && (next_eh_op->opcode == OP_START_HANDLER || next_eh_op->opcode == OP_GET_EX_OBJ)) {
				eh_op = next_eh_op;
				next_eh_op = eh_op->next;
			}
		}

		mono_bblock_insert_after_ins (bblock, eh_op, poll_addr);
		mono_bblock_insert_after_ins (bblock, poll_addr, ins);
	} else if (bblock == cfg->bb_entry) {
		mono_bblock_insert_after_ins (bblock, bblock->last_ins, poll_addr);
		mono_bblock_insert_after_ins (bblock, poll_addr, ins);
	} else {
		mono_bblock_insert_before_ins (bblock, NULL, poll_addr);
		mono_bblock_insert_after_ins (bblock, poll_addr, ins);
	}
}

/*
This code inserts safepoints into managed code at important code paths.
Those are:

-the first basic block
-landing BB for exception handlers
-loop body starts.

*/
static void
insert_safepoints (MonoCompile *cfg)
{
	MonoBasicBlock *bb;

	g_assert (mini_safepoints_enabled ());

	if (COMPILE_LLVM (cfg)) {
		if (!cfg->llvm_only) {
			/* We rely on LLVM's safepoints insertion capabilities. */
			if (cfg->verbose_level > 1)
				printf ("SKIPPING SAFEPOINTS for code compiled with LLVM\n");
			return;
		}
	}

	if (cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (cfg->method);
		/* These wrappers are called from the wrapper for the polling function, leading to potential stack overflow */
		if (info && info->subtype == WRAPPER_SUBTYPE_ICALL_WRAPPER &&
				(info->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_threads_state_poll ||
				 info->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_thread_interruption_checkpoint ||
				 info->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_threads_exit_gc_safe_region_unbalanced)) {
			if (cfg->verbose_level > 1)
				printf ("SKIPPING SAFEPOINTS for the polling function icall\n");
			return;
		}
	}

	if (cfg->method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED) {
		if (cfg->verbose_level > 1)
			printf ("SKIPPING SAFEPOINTS for native-to-managed wrappers.\n");
		return;
	}

	if (cfg->method->wrapper_type == MONO_WRAPPER_OTHER) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (cfg->method);

		if (info && (info->subtype == WRAPPER_SUBTYPE_INTERP_IN || info->subtype == WRAPPER_SUBTYPE_INTERP_LMF)) {
			/* These wrappers shouldn't do any icalls */
			if (cfg->verbose_level > 1)
				printf ("SKIPPING SAFEPOINTS for interp-in wrappers.\n");
			return;
		}
	}

	if (cfg->verbose_level > 1)
		printf ("INSERTING SAFEPOINTS\n");
	if (cfg->verbose_level > 2)
		mono_print_code (cfg, "BEFORE SAFEPOINTS");

	/* if the method doesn't contain
	 *  (1) a call (so it's a leaf method)
	 *  (2) and no loops
	 * we can skip the GC safepoint on method entry. */
	gboolean requires_safepoint = cfg->has_calls;

	for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {
		if (bb->loop_body_start || (bb->flags & BB_EXCEPTION_HANDLER)) {
			requires_safepoint = TRUE;
			insert_safepoint (cfg, bb);
		}
	}

	if (requires_safepoint)
		insert_safepoint (cfg, cfg->bb_entry);

	if (cfg->verbose_level > 2)
		mono_print_code (cfg, "AFTER SAFEPOINTS");

}


static void
mono_insert_branches_between_bblocks (MonoCompile *cfg)
{
	MonoBasicBlock *bb;

	/* Add branches between non-consecutive bblocks */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (bb->last_ins && MONO_IS_COND_BRANCH_OP (bb->last_ins) &&
			bb->last_ins->inst_false_bb && bb->next_bb != bb->last_ins->inst_false_bb) {
			/* we are careful when inverting, since bugs like #59580
			 * could show up when dealing with NaNs.
			 */
			if (MONO_IS_COND_BRANCH_NOFP(bb->last_ins) && bb->next_bb == bb->last_ins->inst_true_bb) {
				MonoBasicBlock *tmp =  bb->last_ins->inst_true_bb;
				bb->last_ins->inst_true_bb = bb->last_ins->inst_false_bb;
				bb->last_ins->inst_false_bb = tmp;

				bb->last_ins->opcode = mono_reverse_branch_op (bb->last_ins->opcode);
			} else {
				MonoInst *inst = (MonoInst *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst));
				inst->opcode = OP_BR;
				inst->inst_target_bb = bb->last_ins->inst_false_bb;
				mono_bblock_add_inst (bb, inst);
			}
		}
	}

	if (cfg->verbose_level >= 4) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			MonoInst *tree = bb->code;
			g_print ("DUMP BLOCK %d:\n", bb->block_num);
			if (!tree)
				continue;
			for (; tree; tree = tree->next) {
				mono_print_ins_index (-1, tree);
			}
		}
	}

	/* FIXME: */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		bb->max_vreg = cfg->next_vreg;
	}
}

static void
init_backend (MonoBackend *backend)
{
#ifdef MONO_ARCH_NEED_GOT_VAR
	backend->need_got_var = 1;
#endif
#ifdef MONO_ARCH_HAVE_CARD_TABLE_WBARRIER
	backend->have_card_table_wb = 1;
#endif
#ifdef MONO_ARCH_HAVE_OP_GENERIC_CLASS_INIT
	backend->have_op_generic_class_init = 1;
#endif
#ifdef MONO_ARCH_EMULATE_MUL_DIV
	backend->emulate_mul_div = 1;
#endif
#ifdef MONO_ARCH_EMULATE_DIV
	backend->emulate_div = 1;
#endif
#if !defined(MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS)
	backend->emulate_long_shift_opts = 1;
#endif
#ifdef MONO_ARCH_HAVE_OBJC_GET_SELECTOR
	backend->have_objc_get_selector = 1;
#endif
#ifdef MONO_ARCH_HAVE_GENERALIZED_IMT_TRAMPOLINE
	backend->have_generalized_imt_trampoline = 1;
#endif
#ifdef MONO_ARCH_GSHARED_SUPPORTED
	backend->gshared_supported = 1;
#endif
	if (MONO_ARCH_USE_FPSTACK)
		backend->use_fpstack = 1;
// Does the ABI have a volatile non-parameter register, so tailcall
// can pass context to generics or interfaces?
	backend->have_volatile_non_param_register = MONO_ARCH_HAVE_VOLATILE_NON_PARAM_REGISTER;
#ifdef MONO_ARCH_HAVE_OP_TAILCALL_MEMBASE
	backend->have_op_tailcall_membase = 1;
#endif
#ifdef MONO_ARCH_HAVE_OP_TAILCALL_REG
	backend->have_op_tailcall_reg = 1;
#endif
#ifndef MONO_ARCH_MONITOR_ENTER_ADJUSTMENT
	backend->monitor_enter_adjustment = 1;
#else
	backend->monitor_enter_adjustment = MONO_ARCH_MONITOR_ENTER_ADJUSTMENT;
#endif
#if defined(MONO_ARCH_ILP32)
	backend->ilp32 = 1;
#endif
#ifdef MONO_ARCH_NEED_DIV_CHECK
	backend->need_div_check = 1;
#endif
#ifdef NO_UNALIGNED_ACCESS
	backend->no_unaligned_access = 1;
#endif
#ifdef MONO_ARCH_DYN_CALL_PARAM_AREA
	backend->dyn_call_param_area = MONO_ARCH_DYN_CALL_PARAM_AREA;
#endif
#ifdef MONO_ARCH_NO_DIV_WITH_MUL
	backend->disable_div_with_mul = 1;
#endif
#ifdef MONO_ARCH_EXPLICIT_NULL_CHECKS
	backend->explicit_null_checks = 1;
#endif
#ifdef MONO_ARCH_HAVE_OPTIMIZED_DIV
	backend->optimized_div = 1;
#endif
#ifdef MONO_ARCH_FORCE_FLOAT32
	backend->force_float32 = 1;
#endif
}

static gboolean
is_simd_supported (MonoCompile *cfg)
{
#ifdef DISABLE_SIMD
    return FALSE;
#endif
	// FIXME: Clean this up
#ifdef TARGET_WASM
	if ((mini_get_cpu_features (cfg) & MONO_CPU_WASM_SIMD) == 0)
		return FALSE;
#else
	if (cfg->llvm_only)
		return FALSE;
#endif
	return TRUE;
}

/*
 * mini_method_compile:
 * @method: the method to compile
 * @opts: the optimization flags to use
 * @domain: the domain where the method will be compiled in
 * @flags: compilation flags
 * @parts: debug flag
 *
 * Returns: a MonoCompile* pointer. Caller must check the exception_type
 * field in the returned struct to see if compilation succeded.
 */
MonoCompile*
mini_method_compile (MonoMethod *method, guint32 opts, MonoDomain *domain, JitFlags flags, int parts, int aot_method_index)
{
	MonoMethodHeader *header;
	MonoMethodSignature *sig;
	MonoCompile *cfg;
	int i;
	gboolean try_generic_shared, try_llvm = FALSE;
	MonoMethod *method_to_compile, *method_to_register;
	gboolean method_is_gshared = FALSE;
	gboolean run_cctors = (flags & JIT_FLAG_RUN_CCTORS) ? 1 : 0;
	gboolean compile_aot = (flags & JIT_FLAG_AOT) ? 1 : 0;
	gboolean full_aot = (flags & JIT_FLAG_FULL_AOT) ? 1 : 0;
	gboolean disable_direct_icalls = (flags & JIT_FLAG_NO_DIRECT_ICALLS) ? 1 : 0;
	gboolean gsharedvt_method = FALSE;
#ifdef ENABLE_LLVM
	gboolean llvm = (flags & JIT_FLAG_LLVM) ? 1 : 0;
#endif
	static gboolean verbose_method_inited;
	static char **verbose_method_names;

	mono_atomic_inc_i32 (&mono_jit_stats.methods_compiled);
	MONO_PROFILER_RAISE (jit_begin, (method));
	if (MONO_METHOD_COMPILE_BEGIN_ENABLED ())
		MONO_PROBE_METHOD_COMPILE_BEGIN (method);

	gsharedvt_method = is_gsharedvt_method (method);

	/*
	 * In AOT mode, method can be the following:
	 * - a gsharedvt method.
	 * - a method inflated with type parameters. This is for ref/partial sharing.
	 * - a method inflated with concrete types.
	 */
	if (compile_aot) {
		if (is_open_method (method)) {
			try_generic_shared = TRUE;
			method_is_gshared = TRUE;
		} else {
			try_generic_shared = FALSE;
		}
		g_assert (opts & MONO_OPT_GSHARED);
	} else {
		try_generic_shared = mono_class_generic_sharing_enabled (method->klass) &&
			(opts & MONO_OPT_GSHARED) && mono_method_is_generic_sharable_full (method, FALSE, FALSE, FALSE);
		if (mini_is_gsharedvt_sharable_method (method)) {
			/*
			if (!mono_debug_count ())
				try_generic_shared = FALSE;
			*/
		}
	}

	/*
	if (try_generic_shared && !mono_debug_count ())
		try_generic_shared = FALSE;
	*/

	if (opts & MONO_OPT_GSHARED) {
		if (try_generic_shared)
			mono_atomic_inc_i32 (&mono_stats.generics_sharable_methods);
		else if (mono_method_is_generic_impl (method))
			mono_atomic_inc_i32 (&mono_stats.generics_unsharable_methods);
	}

#ifdef ENABLE_LLVM
	try_llvm = mono_use_llvm || llvm;
#endif

#ifndef MONO_ARCH_FLOAT32_SUPPORTED
	opts &= ~MONO_OPT_FLOAT32;
#endif
	if (current_backend->force_float32)
		/* Force float32 mode on newer platforms */
		opts |= MONO_OPT_FLOAT32;

 restart_compile:
	if (method_is_gshared) {
		method_to_compile = method;
	} else {
		if (try_generic_shared) {
			ERROR_DECL (error);
			method_to_compile = mini_get_shared_method_full (method, SHARE_MODE_NONE, error);
			mono_error_assert_ok (error);
		} else {
			method_to_compile = method;
		}
	}

	cfg = g_new0 (MonoCompile, 1);
	cfg->method = method_to_compile;
	cfg->mempool = mono_mempool_new ();
	cfg->opt = opts;
	cfg->run_cctors = run_cctors;
	cfg->domain = domain;
	cfg->verbose_level = mini_verbose;
	cfg->compile_aot = compile_aot;
	cfg->full_aot = full_aot;
	cfg->disable_omit_fp = mini_debug_options.disable_omit_fp;
	cfg->skip_visibility = method->skip_visibility;
	cfg->orig_method = method;
	cfg->gen_seq_points = !mini_debug_options.no_seq_points_compact_data || mini_debug_options.gen_sdb_seq_points;
	cfg->gen_sdb_seq_points = mini_debug_options.gen_sdb_seq_points;
	cfg->llvm_only = (flags & JIT_FLAG_LLVM_ONLY) != 0;
	cfg->interp = (flags & JIT_FLAG_INTERP) != 0;
	cfg->use_current_cpu = (flags & JIT_FLAG_USE_CURRENT_CPU) != 0;
	cfg->self_init = (flags & JIT_FLAG_SELF_INIT) != 0;
	cfg->code_exec_only = (flags & JIT_FLAG_CODE_EXEC_ONLY) != 0;
	cfg->backend = current_backend;
	cfg->jit_mm = jit_mm_for_method (cfg->method);
	cfg->mem_manager = m_method_get_mem_manager (cfg->method);

	if (cfg->method->wrapper_type == MONO_WRAPPER_ALLOC) {
		/* We can't have seq points inside gc critical regions */
		cfg->gen_seq_points = FALSE;
		cfg->gen_sdb_seq_points = FALSE;
	}
	/* coop requires loop detection to happen */
	if (mini_safepoints_enabled ())
		cfg->opt |= MONO_OPT_LOOP;
	cfg->disable_llvm_implicit_null_checks = mini_debug_options.llvm_disable_implicit_null_checks;
	if (cfg->backend->explicit_null_checks || mini_debug_options.explicit_null_checks) {
		/* some platforms have null pages, so we can't SIGSEGV */
		cfg->explicit_null_checks = TRUE;
		cfg->disable_llvm_implicit_null_checks = TRUE;
	} else {
		cfg->explicit_null_checks = flags & JIT_FLAG_EXPLICIT_NULL_CHECKS;
	}
	cfg->soft_breakpoints = mini_debug_options.soft_breakpoints;
	cfg->check_pinvoke_callconv = mini_debug_options.check_pinvoke_callconv;
	cfg->disable_direct_icalls = disable_direct_icalls;
	cfg->direct_pinvoke = (flags & JIT_FLAG_DIRECT_PINVOKE) != 0;
	if (try_generic_shared)
		cfg->gshared = TRUE;
	cfg->compile_llvm = try_llvm;
	cfg->token_info_hash = g_hash_table_new (NULL, NULL);
	if (cfg->compile_aot)
		cfg->method_index = aot_method_index;

	if (cfg->compile_llvm)
		cfg->explicit_null_checks = TRUE;

	/*
	if (!mono_debug_count ())
		cfg->opt &= ~MONO_OPT_FLOAT32;
	*/
	if (!is_simd_supported (cfg))
		cfg->opt &= ~MONO_OPT_SIMD;
	cfg->r4fp = (cfg->opt & MONO_OPT_FLOAT32) ? 1 : 0;
	cfg->r4_stack_type = cfg->r4fp ? STACK_R4 : STACK_R8;

	if (cfg->gen_seq_points)
		cfg->seq_points = g_ptr_array_new ();
	cfg->error = (MonoError*)&cfg->error_value;
	error_init (cfg->error);

	if (cfg->compile_aot && !try_generic_shared && (method->is_generic || mono_class_is_gtd (method->klass) || method_is_gshared)) {
		cfg->exception_type = MONO_EXCEPTION_GENERIC_SHARING_FAILED;
		return cfg;
	}

	if (cfg->gshared && (gsharedvt_method || mini_is_gsharedvt_sharable_method (method))) {
		MonoMethodInflated *inflated;
		MonoGenericContext *context;

		if (gsharedvt_method) {
			g_assert (method->is_inflated);
			inflated = (MonoMethodInflated*)method;
			context = &inflated->context;

			/* We are compiling a gsharedvt method directly */
			g_assert (compile_aot);
		} else {
			g_assert (method_to_compile->is_inflated);
			inflated = (MonoMethodInflated*)method_to_compile;
			context = &inflated->context;
		}

		mini_init_gsctx (NULL, cfg->mempool, context, &cfg->gsctx);
		cfg->gsctx_context = context;

		cfg->gsharedvt = TRUE;
		if (!cfg->llvm_only) {
			cfg->disable_llvm = TRUE;
			cfg->exception_message = g_strdup ("gsharedvt");
		}
	}

	if (cfg->gshared) {
		method_to_register = method_to_compile;
	} else {
		g_assert (method == method_to_compile);
		method_to_register = method;
	}
	cfg->method_to_register = method_to_register;

	ERROR_DECL (err);
	sig = mono_method_signature_checked (cfg->method, err);	
	if (!sig) {
		cfg->exception_type = MONO_EXCEPTION_TYPE_LOAD;
		cfg->exception_message = g_strdup (mono_error_get_message (err));
		mono_error_cleanup (err);
		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
		return cfg;
	}

	header = cfg->header = mono_method_get_header_checked (cfg->method, cfg->error);
	if (!header) {
		mono_cfg_set_exception (cfg, MONO_EXCEPTION_MONO_ERROR);
		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
		return cfg;
	}

#ifdef ENABLE_LLVM
	{
		static gboolean inited;

		if (!inited)
			inited = TRUE;

		/* 
		 * Check for methods which cannot be compiled by LLVM early, to avoid
		 * the extra compilation pass.
		 */
		if (COMPILE_LLVM (cfg)) {
			mono_llvm_check_method_supported (cfg);
			if (cfg->disable_llvm) {
				if (cfg->verbose_level >= (cfg->llvm_only ? 0 : 1)) {
					//nm = mono_method_full_name (cfg->method, TRUE);
					printf ("LLVM failed for '%s.%s': %s\n", m_class_get_name (method->klass), method->name, cfg->exception_message);
					//g_free (nm);
				}
				if (cfg->llvm_only) {
					g_free (cfg->exception_message);
					cfg->disable_aot = TRUE;
					return cfg;
				}
				mono_destroy_compile (cfg);
				try_llvm = FALSE;
				goto restart_compile;
			}
		}
	}
#endif

	cfg->prof_flags = mono_profiler_get_call_instrumentation_flags (cfg->method);
	cfg->prof_coverage = mono_profiler_coverage_instrumentation_enabled (cfg->method);

	gboolean trace = mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method);
	if (trace)
		cfg->prof_flags = (MonoProfilerCallInstrumentationFlags)(
			MONO_PROFILER_CALL_INSTRUMENTATION_ENTER | MONO_PROFILER_CALL_INSTRUMENTATION_ENTER_CONTEXT |
			MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE | MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE_CONTEXT);

	/* The debugger has no liveness information, so avoid sharing registers/stack slots */
	if (mini_debug_options.mdb_optimizations || MONO_CFG_PROFILE_CALL_CONTEXT (cfg)) {
		cfg->disable_reuse_registers = TRUE;
		cfg->disable_reuse_stack_slots = TRUE;
		/* 
		 * This decreases the change the debugger will read registers/stack slots which are
		 * not yet initialized.
		 */
		cfg->disable_initlocals_opt = TRUE;

		cfg->extend_live_ranges = TRUE;

		/* The debugger needs all locals to be on the stack or in a global register */
		cfg->disable_vreg_to_lvreg = TRUE;

		/* Don't remove unused variables when running inside the debugger since the user
		 * may still want to view them. */
		cfg->disable_deadce_vars = TRUE;

		cfg->opt &= ~MONO_OPT_DEADCE;
		cfg->opt &= ~MONO_OPT_INLINE;
		cfg->opt &= ~MONO_OPT_COPYPROP;
		cfg->opt &= ~MONO_OPT_CONSPROP;

		/* This is needed for the soft debugger, which doesn't like code after the epilog */
		cfg->disable_out_of_line_bblocks = TRUE;
	}

	if (mono_using_xdebug) {
		/* 
		 * Make each variable use its own register/stack slot and extend 
		 * their liveness to cover the whole method, making them displayable
		 * in gdb even after they are dead.
		 */
		cfg->disable_reuse_registers = TRUE;
		cfg->disable_reuse_stack_slots = TRUE;
		cfg->extend_live_ranges = TRUE;
		cfg->compute_precise_live_ranges = TRUE;
	}

	mini_gc_init_cfg (cfg);

	if (method->wrapper_type == MONO_WRAPPER_OTHER) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (method);

		if ((info && (info->subtype == WRAPPER_SUBTYPE_GSHAREDVT_IN_SIG || info->subtype == WRAPPER_SUBTYPE_GSHAREDVT_OUT_SIG))) {
			cfg->disable_gc_safe_points = TRUE;
			/* This is safe, these wrappers only store to the stack */
			cfg->gen_write_barriers = FALSE;
		}
	}

	if (COMPILE_LLVM (cfg)) {
		cfg->opt |= MONO_OPT_ABCREM;
	}

	if (!verbose_method_inited) {
		char *env = g_getenv ("MONO_VERBOSE_METHOD");
		if (env != NULL)
			verbose_method_names = g_strsplit (env, ";", -1);
		
		verbose_method_inited = TRUE;
	}
	if (verbose_method_names) {
		int i;
		
		for (i = 0; verbose_method_names [i] != NULL; i++){
			const char *name = verbose_method_names [i];

			if ((strchr (name, '.') > name) || strchr (name, ':')) {
				MonoMethodDesc *desc;
				
				desc = mono_method_desc_new (name, TRUE);
				if (desc) {
					if (mono_method_desc_full_match (desc, cfg->method)) {
						cfg->verbose_level = 4;
					}
					mono_method_desc_free (desc);
				}
			} else {
				if (strcmp (cfg->method->name, name) == 0)
					cfg->verbose_level = 4;
			}
		}
	}

	cfg->intvars = (guint16 *)mono_mempool_alloc0 (cfg->mempool, sizeof (guint16) * STACK_MAX * header->max_stack);

	if (cfg->verbose_level > 0) {
		char *method_name;

		method_name = mono_method_get_full_name (method);
		g_print ("converting %s%s%smethod %s\n", COMPILE_LLVM (cfg) ? "llvm " : "", cfg->gsharedvt ? "gsharedvt " : "", (cfg->gshared && !cfg->gsharedvt) ? "gshared " : "", method_name);
		/*
		if (COMPILE_LLVM (cfg))
			g_print ("converting llvm method %s\n", method_name = mono_method_full_name (method, TRUE));
		else if (cfg->gsharedvt)
			g_print ("converting gsharedvt method %s\n", method_name = mono_method_full_name (method_to_compile, TRUE));
		else if (cfg->gshared)
			g_print ("converting shared method %s\n", method_name = mono_method_full_name (method_to_compile, TRUE));
		else
			g_print ("converting method %s\n", method_name = mono_method_full_name (method, TRUE));
		*/
		g_free (method_name);
	}

	if (cfg->opt & MONO_OPT_ABCREM)
		cfg->opt |= MONO_OPT_SSA;

	cfg->rs = mono_regstate_new ();
	cfg->next_vreg = cfg->rs->next_vreg;

	/* FIXME: Fix SSA to handle branches inside bblocks */
	if (cfg->opt & MONO_OPT_SSA)
		cfg->enable_extended_bblocks = FALSE;

	/*
	 * FIXME: This confuses liveness analysis because variables which are assigned after
	 * a branch inside a bblock become part of the kill set, even though the assignment
	 * might not get executed. This causes the optimize_initlocals pass to delete some
	 * assignments which are needed.
	 * Also, the mono_if_conversion pass needs to be modified to recognize the code
	 * created by this.
	 */
	//cfg->enable_extended_bblocks = TRUE;

	/*
	 * create MonoInst* which represents arguments and local variables
	 */
	mono_compile_create_vars (cfg);

	mono_cfg_dump_create_context (cfg);
	mono_cfg_dump_begin_group (cfg);

	MONO_TIME_TRACK (mono_jit_stats.jit_method_to_ir, i = mono_method_to_ir (cfg, method_to_compile, NULL, NULL, NULL, NULL, 0, FALSE));
	mono_cfg_dump_ir (cfg, "method-to-ir");

	if (cfg->gdump_ctx != NULL) {
		/* workaround for graph visualization, as it doesn't handle empty basic blocks properly */
		mono_insert_nop_in_empty_bb (cfg);
		mono_cfg_dump_ir (cfg, "mono_insert_nop_in_empty_bb");
	}

	if (i < 0) {
		if (try_generic_shared && cfg->exception_type == MONO_EXCEPTION_GENERIC_SHARING_FAILED) {
			if (compile_aot) {
				if (MONO_METHOD_COMPILE_END_ENABLED ())
					MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
				return cfg;
			}
			mono_destroy_compile (cfg);
			try_generic_shared = FALSE;
			goto restart_compile;
		}
		g_assert (cfg->exception_type != MONO_EXCEPTION_GENERIC_SHARING_FAILED);

		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
		/* cfg contains the details of the failure, so let the caller cleanup */
		return cfg;
	}

	cfg->stat_basic_blocks += cfg->num_bblocks;

	if (COMPILE_LLVM (cfg)) {
		MonoInst *ins;

		/* The IR has to be in SSA form for LLVM */
		cfg->opt |= MONO_OPT_SSA;

		// FIXME:
		if (cfg->ret) {
			// Allow SSA on the result value
			cfg->ret->flags &= ~MONO_INST_VOLATILE;

			// Add an explicit return instruction referencing the return value
			MONO_INST_NEW (cfg, ins, OP_SETRET);
			ins->sreg1 = cfg->ret->dreg;

			MONO_ADD_INS (cfg->bb_exit, ins);
		}

		cfg->opt &= ~MONO_OPT_LINEARS;

		/* FIXME: */
		cfg->opt &= ~MONO_OPT_BRANCH;
	}

	/* todo: remove code when we have verified that the liveness for try/catch blocks
	 * works perfectly 
	 */
	/* 
	 * Currently, this can't be commented out since exception blocks are not
	 * processed during liveness analysis.
	 * It is also needed, because otherwise the local optimization passes would
	 * delete assignments in cases like this:
	 * r1 <- 1
	 * <something which throws>
	 * r1 <- 2
	 * This also allows SSA to be run on methods containing exception clauses, since
	 * SSA will ignore variables marked VOLATILE.
	 */
	MONO_TIME_TRACK (mono_jit_stats.jit_liveness_handle_exception_clauses, mono_liveness_handle_exception_clauses (cfg));
	mono_cfg_dump_ir (cfg, "liveness_handle_exception_clauses");

	MONO_TIME_TRACK (mono_jit_stats.jit_handle_out_of_line_bblock, mono_handle_out_of_line_bblock (cfg));
	mono_cfg_dump_ir (cfg, "handle_out_of_line_bblock");

	/*g_print ("numblocks = %d\n", cfg->num_bblocks);*/

	if (!COMPILE_LLVM (cfg)) {
		MONO_TIME_TRACK (mono_jit_stats.jit_decompose_long_opts, mono_decompose_long_opts (cfg));
		mono_cfg_dump_ir (cfg, "decompose_long_opts");
	}

	/* Should be done before branch opts */
	if (cfg->opt & (MONO_OPT_CONSPROP | MONO_OPT_COPYPROP)) {
		MONO_TIME_TRACK (mono_jit_stats.jit_local_cprop, mono_local_cprop (cfg));
		mono_cfg_dump_ir (cfg, "local_cprop");
	}

	if (cfg->flags & MONO_CFG_HAS_TYPE_CHECK) {
		MONO_TIME_TRACK (mono_jit_stats.jit_decompose_typechecks, mono_decompose_typechecks (cfg));
		if (cfg->gdump_ctx != NULL) {
			/* workaround for graph visualization, as it doesn't handle empty basic blocks properly */
			mono_insert_nop_in_empty_bb (cfg);
		}
		mono_cfg_dump_ir (cfg, "decompose_typechecks");
	}

	/*
	 * Should be done after cprop which can do strength reduction on
	 * some of these ops, after propagating immediates.
	 */
	if (cfg->has_emulated_ops) {
		MONO_TIME_TRACK (mono_jit_stats.jit_local_emulate_ops, mono_local_emulate_ops (cfg));
		mono_cfg_dump_ir (cfg, "local_emulate_ops");
	}

	if (cfg->opt & MONO_OPT_BRANCH) {
		MONO_TIME_TRACK (mono_jit_stats.jit_optimize_branches, mono_optimize_branches (cfg));
		mono_cfg_dump_ir (cfg, "optimize_branches");
	}

	/* This must be done _before_ global reg alloc and _after_ decompose */
	MONO_TIME_TRACK (mono_jit_stats.jit_handle_global_vregs, mono_handle_global_vregs (cfg));
	mono_cfg_dump_ir (cfg, "handle_global_vregs");
	if (cfg->opt & MONO_OPT_DEADCE) {
		MONO_TIME_TRACK (mono_jit_stats.jit_local_deadce, mono_local_deadce (cfg));
		mono_cfg_dump_ir (cfg, "local_deadce");
	}
	if (cfg->opt & MONO_OPT_ALIAS_ANALYSIS) {
		MONO_TIME_TRACK (mono_jit_stats.jit_local_alias_analysis, mono_local_alias_analysis (cfg));
		mono_cfg_dump_ir (cfg, "local_alias_analysis");
	}
	/* Disable this for LLVM to make the IR easier to handle */
	if (!COMPILE_LLVM (cfg)) {
		MONO_TIME_TRACK (mono_jit_stats.jit_if_conversion, mono_if_conversion (cfg));
		mono_cfg_dump_ir (cfg, "if_conversion");
	}

	mono_threads_safepoint ();

	MONO_TIME_TRACK (mono_jit_stats.jit_bb_ordering, mono_bb_ordering (cfg));
	mono_cfg_dump_ir (cfg, "bb_ordering");

	if (((cfg->num_varinfo > 2000) || (cfg->num_bblocks > 1000)) && !cfg->compile_aot) {
		/* 
		 * we disable some optimizations if there are too many variables
		 * because JIT time may become too expensive. The actual number needs 
		 * to be tweaked and eventually the non-linear algorithms should be fixed.
		 */
		cfg->opt &= ~ (MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP);
		cfg->disable_ssa = TRUE;
	}

	if (cfg->num_varinfo > 10000 && !cfg->llvm_only)
		/* Disable llvm for overly complex methods */
		cfg->disable_ssa = TRUE;

	if (cfg->opt & MONO_OPT_LOOP) {
		MONO_TIME_TRACK (mono_jit_stats.jit_compile_dominator_info, mono_compile_dominator_info (cfg, MONO_COMP_DOM | MONO_COMP_IDOM));
		MONO_TIME_TRACK (mono_jit_stats.jit_compute_natural_loops, mono_compute_natural_loops (cfg));
	}

	if (mono_threads_are_safepoints_enabled ()) {
		MONO_TIME_TRACK (mono_jit_stats.jit_insert_safepoints, insert_safepoints (cfg));
		mono_cfg_dump_ir (cfg, "insert_safepoints");
	}

	/* after method_to_ir */
	if (parts == 1) {
		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, TRUE);
		return cfg;
	}

	/*
	  if (header->num_clauses)
	  cfg->disable_ssa = TRUE;
	*/

//#define DEBUGSSA "logic_run"
//#define DEBUGSSA_CLASS "Tests"
#ifdef DEBUGSSA

	if (!cfg->disable_ssa) {
		mono_local_cprop (cfg);

#ifndef DISABLE_SSA
		mono_ssa_compute (cfg);
#endif
	}
#else 
	if (cfg->opt & MONO_OPT_SSA) {
		if (!(cfg->comp_done & MONO_COMP_SSA) && !cfg->disable_ssa) {
#ifndef DISABLE_SSA
			MONO_TIME_TRACK (mono_jit_stats.jit_ssa_compute, mono_ssa_compute (cfg));
			mono_cfg_dump_ir (cfg, "ssa_compute");
#endif

			if (cfg->verbose_level >= 2) {
				print_dfn (cfg);
			}
		}
	}
#endif

	/* after SSA translation */
	if (parts == 2) {
		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, TRUE);
		return cfg;
	}

	if ((cfg->opt & MONO_OPT_CONSPROP) || (cfg->opt & MONO_OPT_COPYPROP)) {
		if (cfg->comp_done & MONO_COMP_SSA && !COMPILE_LLVM (cfg)) {
#ifndef DISABLE_SSA
			MONO_TIME_TRACK (mono_jit_stats.jit_ssa_cprop, mono_ssa_cprop (cfg));
			mono_cfg_dump_ir (cfg, "ssa_cprop");
#endif
		}
	}

#ifndef DISABLE_SSA
	if (cfg->comp_done & MONO_COMP_SSA && !COMPILE_LLVM (cfg)) {
		//mono_ssa_strength_reduction (cfg);

		if (cfg->opt & MONO_OPT_DEADCE) {
			MONO_TIME_TRACK (mono_jit_stats.jit_ssa_deadce, mono_ssa_deadce (cfg));
			mono_cfg_dump_ir (cfg, "ssa_deadce");
		}

		if ((cfg->flags & (MONO_CFG_HAS_LDELEMA|MONO_CFG_HAS_CHECK_THIS)) && (cfg->opt & MONO_OPT_ABCREM)) {
			MONO_TIME_TRACK (mono_jit_stats.jit_perform_abc_removal, mono_perform_abc_removal (cfg));
			mono_cfg_dump_ir (cfg, "perform_abc_removal");
		}

		MONO_TIME_TRACK (mono_jit_stats.jit_ssa_remove, mono_ssa_remove (cfg));
		mono_cfg_dump_ir (cfg, "ssa_remove");
		MONO_TIME_TRACK (mono_jit_stats.jit_local_cprop2, mono_local_cprop (cfg));
		mono_cfg_dump_ir (cfg, "local_cprop2");
		MONO_TIME_TRACK (mono_jit_stats.jit_handle_global_vregs2, mono_handle_global_vregs (cfg));
		mono_cfg_dump_ir (cfg, "handle_global_vregs2");
		if (cfg->opt & MONO_OPT_DEADCE) {
			MONO_TIME_TRACK (mono_jit_stats.jit_local_deadce2, mono_local_deadce (cfg));
			mono_cfg_dump_ir (cfg, "local_deadce2");
		}

		if (cfg->opt & MONO_OPT_BRANCH) {
			MONO_TIME_TRACK (mono_jit_stats.jit_optimize_branches2, mono_optimize_branches (cfg));
			mono_cfg_dump_ir (cfg, "optimize_branches2");
		}
	}
#endif

	if (cfg->comp_done & MONO_COMP_SSA && COMPILE_LLVM (cfg)) {
		mono_ssa_loop_invariant_code_motion (cfg);
		mono_cfg_dump_ir (cfg, "loop_invariant_code_motion");
		/* This removes MONO_INST_FAULT flags too so perform it unconditionally */
		if (cfg->opt & MONO_OPT_ABCREM) {
			mono_perform_abc_removal (cfg);
			mono_cfg_dump_ir (cfg, "abc_removal");
		}
	}

	/* after SSA removal */
	if (parts == 3) {
		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, TRUE);
		return cfg;
	}

	if (cfg->llvm_only && cfg->gsharedvt)
		mono_ssa_remove_gsharedvt (cfg);

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
	if (COMPILE_SOFT_FLOAT (cfg))
		mono_decompose_soft_float (cfg);
#endif
	MONO_TIME_TRACK (mono_jit_stats.jit_decompose_vtype_opts, mono_decompose_vtype_opts (cfg));
	if (cfg->flags & MONO_CFG_NEEDS_DECOMPOSE) {
		MONO_TIME_TRACK (mono_jit_stats.jit_decompose_array_access_opts, mono_decompose_array_access_opts (cfg));
		mono_cfg_dump_ir (cfg, "decompose_array_access_opts");
	}

	if (cfg->got_var) {
#ifndef MONO_ARCH_GOT_REG
		GList *regs;
#endif
		int got_reg;

		g_assert (cfg->got_var_allocated);

		/* 
		 * Allways allocate the GOT var to a register, because keeping it
		 * in memory will increase the number of live temporaries in some
		 * code created by inssel.brg, leading to the well known spills+
		 * branches problem. Testcase: mcs crash in 
		 * System.MonoCustomAttrs:GetCustomAttributes.
		 */
#ifdef MONO_ARCH_GOT_REG
		got_reg = MONO_ARCH_GOT_REG;
#else
		regs = mono_arch_get_global_int_regs (cfg);
		g_assert (regs);
		got_reg = GPOINTER_TO_INT (regs->data);
		g_list_free (regs);
#endif
		cfg->got_var->opcode = OP_REGVAR;
		cfg->got_var->dreg = got_reg;
		cfg->used_int_regs |= 1LL << cfg->got_var->dreg;
	}

	/*
	 * Have to call this again to process variables added since the first call.
	 */
	MONO_TIME_TRACK(mono_jit_stats.jit_liveness_handle_exception_clauses2, mono_liveness_handle_exception_clauses (cfg));

	if (cfg->opt & MONO_OPT_LINEARS) {
		GList *vars, *regs, *l;
		
		/* fixme: maybe we can avoid to compute livenesss here if already computed ? */
		cfg->comp_done &= ~MONO_COMP_LIVENESS;
		if (!(cfg->comp_done & MONO_COMP_LIVENESS))
			MONO_TIME_TRACK (mono_jit_stats.jit_analyze_liveness, mono_analyze_liveness (cfg));

		if ((vars = mono_arch_get_allocatable_int_vars (cfg))) {
			regs = mono_arch_get_global_int_regs (cfg);
			/* Remove the reg reserved for holding the GOT address */
			if (cfg->got_var) {
				for (l = regs; l; l = l->next) {
					if (GPOINTER_TO_UINT (l->data) == cfg->got_var->dreg) {
						regs = g_list_delete_link (regs, l);
						break;
					}
				}
			}
			MONO_TIME_TRACK (mono_jit_stats.jit_linear_scan, mono_linear_scan (cfg, vars, regs, &cfg->used_int_regs));
			mono_cfg_dump_ir (cfg, "linear_scan");
		}
	}

	//mono_print_code (cfg, "");

    //print_dfn (cfg);
	
	/* variables are allocated after decompose, since decompose could create temps */
	if (!COMPILE_LLVM (cfg)) {
		MONO_TIME_TRACK (mono_jit_stats.jit_arch_allocate_vars, mono_arch_allocate_vars (cfg));
		mono_cfg_dump_ir (cfg, "arch_allocate_vars");
		if (cfg->exception_type)
			return cfg;
	}

	if (cfg->gsharedvt)
		mono_allocate_gsharedvt_vars (cfg);

	if (!COMPILE_LLVM (cfg)) {
		gboolean need_local_opts;
		MONO_TIME_TRACK (mono_jit_stats.jit_spill_global_vars, mono_spill_global_vars (cfg, &need_local_opts));
		mono_cfg_dump_ir (cfg, "spill_global_vars");

		if (need_local_opts || cfg->compile_aot) {
			/* To optimize code created by spill_global_vars */
			MONO_TIME_TRACK (mono_jit_stats.jit_local_cprop3, mono_local_cprop (cfg));
			if (cfg->opt & MONO_OPT_DEADCE)
				MONO_TIME_TRACK (mono_jit_stats.jit_local_deadce3, mono_local_deadce (cfg));
			mono_cfg_dump_ir (cfg, "needs_local_opts");
		}
	}

	mono_insert_branches_between_bblocks (cfg);

	if (COMPILE_LLVM (cfg)) {
#ifdef ENABLE_LLVM
		char *nm;

		/* The IR has to be in SSA form for LLVM */
		if (!(cfg->comp_done & MONO_COMP_SSA)) {
			cfg->exception_message = g_strdup ("SSA disabled.");
			cfg->disable_llvm = TRUE;
		}

		if (cfg->flags & MONO_CFG_NEEDS_DECOMPOSE)
			mono_decompose_array_access_opts (cfg);

		if (!cfg->disable_llvm)
			mono_llvm_emit_method (cfg);
		if (cfg->disable_llvm) {
			if (cfg->verbose_level >= (cfg->llvm_only ? 0 : 1)) {
				//nm = mono_method_full_name (cfg->method, TRUE);
				printf ("LLVM failed for '%s.%s': %s\n", m_class_get_name (method->klass), method->name, cfg->exception_message);
				//g_free (nm);
			}
			if (cfg->llvm_only) {
				cfg->disable_aot = TRUE;
				return cfg;
			}
			mono_destroy_compile (cfg);
			try_llvm = FALSE;
			goto restart_compile;
		}

		if (cfg->verbose_level > 0 && !cfg->compile_aot) {
			nm = mono_method_get_full_name (cfg->method);
			g_print ("LLVM Method %s emitted at %p to %p (code length %d) [%s]\n", 
					 nm, 
					 cfg->native_code, cfg->native_code + cfg->code_len, cfg->code_len, cfg->domain->friendly_name);
			g_free (nm);
		}
#endif
	} else {
		MONO_TIME_TRACK (mono_jit_stats.jit_codegen, mono_codegen (cfg));
		mono_cfg_dump_ir (cfg, "codegen");
		if (cfg->exception_type)
			return cfg;
	}

	if (COMPILE_LLVM (cfg))
		mono_atomic_inc_i32 (&mono_jit_stats.methods_with_llvm);
	else
		mono_atomic_inc_i32 (&mono_jit_stats.methods_without_llvm);

	MONO_TIME_TRACK (mono_jit_stats.jit_create_jit_info, cfg->jit_info = create_jit_info (cfg, method_to_compile));

	if (cfg->extend_live_ranges) {
		/* Extend live ranges to cover the whole method */
		for (i = 0; i < cfg->num_varinfo; ++i)
			MONO_VARINFO (cfg, i)->live_range_end = cfg->code_len;
	}

	MONO_TIME_TRACK (mono_jit_stats.jit_gc_create_gc_map, mini_gc_create_gc_map (cfg));
	MONO_TIME_TRACK (mono_jit_stats.jit_save_seq_point_info, mono_save_seq_point_info (cfg, cfg->jit_info));

	if (!cfg->compile_aot) {
		mono_save_xdebug_info (cfg);
		mono_lldb_save_method_info (cfg);
	}

	if (cfg->verbose_level >= 2) {
		char *id =  mono_method_full_name (cfg->method, TRUE);
		g_print ("\n*** ASM for %s ***\n", id);
		mono_disassemble_code (cfg, cfg->native_code, cfg->code_len, id + 3);
		g_print ("***\n\n");
		g_free (id);
	}

	if (!cfg->compile_aot && !(flags & JIT_FLAG_DISCARD_RESULTS)) {
		mono_domain_lock (cfg->domain);
		mono_jit_info_table_add (cfg->domain, cfg->jit_info);
		mono_domain_unlock (cfg->domain);

		if (cfg->method->dynamic) {
			MonoJitMemoryManager *jit_mm = (MonoJitMemoryManager*)cfg->jit_mm;
			MonoJitDynamicMethodInfo *res;

			jit_mm_lock (jit_mm);
			g_assert (jit_mm->dynamic_code_hash);
			res = (MonoJitDynamicMethodInfo *)g_hash_table_lookup (jit_mm->dynamic_code_hash, method);
			jit_mm_unlock (jit_mm);
			g_assert (res);
			res->ji = cfg->jit_info;
		}

		mono_postprocess_patches_after_ji_publish (cfg);
	}

#if 0
	if (cfg->gsharedvt)
		printf ("GSHAREDVT: %s\n", mono_method_full_name (cfg->method, TRUE));
#endif

	/* collect statistics */
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_inc_i32 (&mono_perfcounters->jit_methods);
	mono_atomic_fetch_add_i32 (&mono_perfcounters->jit_bytes, header->code_size);
#endif
	gint32 code_size_ratio = cfg->code_len;
	mono_atomic_fetch_add_i32 (&mono_jit_stats.allocated_code_size, code_size_ratio);
	mono_atomic_fetch_add_i32 (&mono_jit_stats.native_code_size, code_size_ratio);
	/* FIXME: use an explicit function to read booleans */
	if ((gboolean)mono_atomic_load_i32 ((gint32*)&mono_jit_stats.enabled)) {
		if (code_size_ratio > mono_atomic_load_i32 (&mono_jit_stats.biggest_method_size)) {
			mono_atomic_store_i32 (&mono_jit_stats.biggest_method_size, code_size_ratio);
			char *biggest_method = g_strdup_printf ("%s::%s)", m_class_get_name (method->klass), method->name);
			biggest_method = (char*)mono_atomic_xchg_ptr ((gpointer*)&mono_jit_stats.biggest_method, biggest_method);
			g_free (biggest_method);
		}
		code_size_ratio = (code_size_ratio * 100) / header->code_size;
		if (code_size_ratio > mono_atomic_load_i32 (&mono_jit_stats.max_code_size_ratio)) {
			mono_atomic_store_i32 (&mono_jit_stats.max_code_size_ratio, code_size_ratio);
			char *max_ratio_method = g_strdup_printf ("%s::%s)", m_class_get_name (method->klass), method->name);
			max_ratio_method = (char*)mono_atomic_xchg_ptr ((gpointer*)&mono_jit_stats.max_ratio_method, max_ratio_method);
			g_free (max_ratio_method);
		}
	}

	if (MONO_METHOD_COMPILE_END_ENABLED ())
		MONO_PROBE_METHOD_COMPILE_END (method, TRUE);

	mono_cfg_dump_close_group (cfg);

	return cfg;
}

gboolean
mini_class_has_reference_variant_generic_argument (MonoCompile *cfg, MonoClass *klass, int context_used)
{
	int i;
	MonoGenericContainer *container;
	MonoGenericInst *ginst;

	if (mono_class_is_ginst (klass)) {
		container = mono_class_get_generic_container (mono_class_get_generic_class (klass)->container_class);
		ginst = mono_class_get_generic_class (klass)->context.class_inst;
	} else if (mono_class_is_gtd (klass) && context_used) {
		container = mono_class_get_generic_container (klass);
		ginst = container->context.class_inst;
	} else {
		return FALSE;
	}

	for (i = 0; i < container->type_argc; ++i) {
		MonoType *type;
		if (!(mono_generic_container_get_param_info (container, i)->flags & (MONO_GEN_PARAM_VARIANT|MONO_GEN_PARAM_COVARIANT)))
			continue;
		type = ginst->type_argv [i];
		if (mini_type_is_reference (type))
			return TRUE;
	}
	return FALSE;
}

void
mono_cfg_add_try_hole (MonoCompile *cfg, MonoExceptionClause *clause, guint8 *start, MonoBasicBlock *bb)
{
	TryBlockHole *hole = (TryBlockHole *)mono_mempool_alloc (cfg->mempool, sizeof (TryBlockHole));
	hole->clause = clause;
	hole->start_offset = start - cfg->native_code;
	hole->basic_block = bb;

	cfg->try_block_holes = g_slist_append_mempool (cfg->mempool, cfg->try_block_holes, hole);
}

void
mono_cfg_set_exception (MonoCompile *cfg, MonoExceptionType type)
{
	cfg->exception_type = type;
}

/* Assumes ownership of the MSG argument */
void
mono_cfg_set_exception_invalid_program (MonoCompile *cfg, char *msg)
{
	mono_cfg_set_exception (cfg, MONO_EXCEPTION_MONO_ERROR);
	mono_error_set_generic_error (cfg->error, "System", "InvalidProgramException", "%s", msg);
}

#endif /* DISABLE_JIT */

gint64 mono_time_track_start ()
{
	return mono_100ns_ticks ();
}

/*
 * mono_time_track_end:
 *
 *   Uses UnlockedAddDouble () to update \param time.
 */
void mono_time_track_end (gint64 *time, gint64 start)
{
	UnlockedAdd64 (time, mono_100ns_ticks () - start);
}

/*
 * mono_update_jit_stats:
 *
 *   Only call this function in locked environments to avoid data races.
 */
MONO_NO_SANITIZE_THREAD
void
mono_update_jit_stats (MonoCompile *cfg)
{
	mono_jit_stats.allocate_var += cfg->stat_allocate_var;
	mono_jit_stats.locals_stack_size += cfg->stat_locals_stack_size;
	mono_jit_stats.basic_blocks += cfg->stat_basic_blocks;
	mono_jit_stats.max_basic_blocks = MAX (cfg->stat_basic_blocks, mono_jit_stats.max_basic_blocks);
	mono_jit_stats.cil_code_size += cfg->stat_cil_code_size;
	mono_jit_stats.regvars += cfg->stat_n_regvars;
	mono_jit_stats.inlineable_methods += cfg->stat_inlineable_methods;
	mono_jit_stats.inlined_methods += cfg->stat_inlined_methods;
	mono_jit_stats.code_reallocs += cfg->stat_code_reallocs;
}

/*
 * mono_jit_compile_method_inner:
 *
 *   Main entry point for the JIT.
 */
gpointer
mono_jit_compile_method_inner (MonoMethod *method, MonoDomain *target_domain, int opt, MonoError *error)
{
	MonoCompile *cfg;
	gpointer code = NULL;
	MonoJitInfo *jinfo, *info;
	MonoVTable *vtable;
	MonoException *ex = NULL;
	gint64 start;
	MonoMethod *prof_method, *shared;

	error_init (error);

	start = mono_time_track_start ();
	cfg = mini_method_compile (method, opt, target_domain, JIT_FLAG_RUN_CCTORS, 0, -1);
	gint64 jit_time = 0.0;
	mono_time_track_end (&jit_time, start);
	UnlockedAdd64 (&mono_jit_stats.jit_time, jit_time);

	prof_method = cfg->method;

	switch (cfg->exception_type) {
	case MONO_EXCEPTION_NONE:
		break;
	case MONO_EXCEPTION_TYPE_LOAD:
	case MONO_EXCEPTION_MISSING_FIELD:
	case MONO_EXCEPTION_MISSING_METHOD:
	case MONO_EXCEPTION_FILE_NOT_FOUND:
	case MONO_EXCEPTION_BAD_IMAGE:
	case MONO_EXCEPTION_INVALID_PROGRAM: {
		/* Throw a type load exception if needed */
		if (cfg->exception_ptr) {
			ex = mono_class_get_exception_for_failure ((MonoClass *)cfg->exception_ptr);
		} else {
			if (cfg->exception_type == MONO_EXCEPTION_MISSING_FIELD)
				ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MissingFieldException", cfg->exception_message);
			else if (cfg->exception_type == MONO_EXCEPTION_MISSING_METHOD)
				ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MissingMethodException", cfg->exception_message);
			else if (cfg->exception_type == MONO_EXCEPTION_TYPE_LOAD)
				ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "TypeLoadException", cfg->exception_message);
			else if (cfg->exception_type == MONO_EXCEPTION_FILE_NOT_FOUND)
				ex = mono_exception_from_name_msg (mono_defaults.corlib, "System.IO", "FileNotFoundException", cfg->exception_message);
			else if (cfg->exception_type == MONO_EXCEPTION_BAD_IMAGE)
				ex = mono_get_exception_bad_image_format (cfg->exception_message);
			else if (cfg->exception_type == MONO_EXCEPTION_INVALID_PROGRAM)
				ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "InvalidProgramException", cfg->exception_message);
			else
				g_assert_not_reached ();
		}
		break;
	}
	case MONO_EXCEPTION_MONO_ERROR:
		// FIXME: MonoError has no copy ctor
		g_assert (!is_ok (cfg->error));
		ex = mono_error_convert_to_exception (cfg->error);
		break;
	default:
		g_assert_not_reached ();
	}

	if (ex) {
		MONO_PROFILER_RAISE (jit_failed, (method));

		mono_destroy_compile (cfg);
		mono_error_set_exception_instance (error, ex);

		return NULL;
	}

	if (mono_method_is_generic_sharable (method, FALSE)) {
		shared = mini_get_shared_method_full (method, SHARE_MODE_NONE, error);
		if (!is_ok (error)) {
			MONO_PROFILER_RAISE (jit_failed, (method));
			mono_destroy_compile (cfg);
			return NULL;
		}
	} else {
		shared = NULL;
	}

	mono_domain_lock (target_domain);

	if (mono_stats_method_desc && mono_method_desc_full_match (mono_stats_method_desc, method)) {
		g_printf ("Printing runtime stats at method: %s\n", mono_method_get_full_name (method));
		mono_runtime_print_stats ();
	}

	/* Check if some other thread already did the job. In this case, we can
       discard the code this thread generated. */

	info = mini_lookup_method (target_domain, method, shared);
	if (info) {
		/* We can't use a domain specific method in another domain */
		if (target_domain == mono_domain_get ()) {
			code = info->code_start;
			discarded_code ++;
			discarded_jit_time += jit_time;
		}
	}
	if (code == NULL) {
		/* The lookup + insert is atomic since this is done inside the domain lock */
		mono_domain_jit_code_hash_lock (target_domain);
		mono_internal_hash_table_insert (&target_domain->jit_code_hash, cfg->jit_info->d.method, cfg->jit_info);
		mono_domain_jit_code_hash_unlock (target_domain);

		code = cfg->native_code;

		if (cfg->gshared && mono_method_is_generic_sharable (method, FALSE))
			mono_atomic_inc_i32 (&mono_stats.generics_shared_methods);
		if (cfg->gsharedvt)
			mono_atomic_inc_i32 (&mono_stats.gsharedvt_methods);
	}

	jinfo = cfg->jit_info;

	/*
	 * Update global stats while holding a lock, instead of doing many
	 * mono_atomic_inc_i32 operations during JITting.
	 */
	mono_update_jit_stats (cfg);

	mono_destroy_compile (cfg);

	mini_patch_llvm_jit_callees (target_domain, method, code);
#ifndef DISABLE_JIT
	mono_emit_jit_map (jinfo);
	mono_emit_jit_dump (jinfo, code);
#endif
	mono_domain_unlock (target_domain);

	if (!is_ok (error))
		return NULL;

	vtable = mono_class_vtable_checked (method->klass, error);
	return_val_if_nok (error, NULL);

	if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) {
		if (mono_marshal_method_from_wrapper (method)) {
			/* Native func wrappers have no method */
			/* The profiler doesn't know about wrappers, so pass the original icall method */
			MONO_PROFILER_RAISE (jit_done, (mono_marshal_method_from_wrapper (method), jinfo));
		}
	}
	MONO_PROFILER_RAISE (jit_done, (method, jinfo));
	if (prof_method != method)
		MONO_PROFILER_RAISE (jit_done, (prof_method, jinfo));

	if (!mono_runtime_class_init_full (vtable, error))
		return NULL;
	return MINI_ADDR_TO_FTNPTR (code);
}

/*
 * mini_get_underlying_type:
 *
 *   Return the type the JIT will use during compilation.
 * Handles: byref, enums, native types, bool/char, ref types, generic sharing.
 * For gsharedvt types, it will return the original VAR/MVAR.
 */
MonoType*
mini_get_underlying_type (MonoType *type)
{
	return mini_type_get_underlying_type (type);
}

void
mini_jit_init (void)
{
	mono_os_mutex_init_recursive (&jit_mutex);

#ifndef DISABLE_JIT
	mono_counters_register ("Discarded method code", MONO_COUNTER_JIT | MONO_COUNTER_INT, &discarded_code);
	mono_counters_register ("Time spent JITting discarded code", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &discarded_jit_time);
	mono_counters_register ("Try holes memory size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &jinfo_try_holes_size);

	mono_counters_register ("JIT/method_to_ir", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_method_to_ir);
	mono_counters_register ("JIT/liveness_handle_exception_clauses", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_liveness_handle_exception_clauses);
	mono_counters_register ("JIT/handle_out_of_line_bblock", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_handle_out_of_line_bblock);
	mono_counters_register ("JIT/decompose_long_opts", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_decompose_long_opts);
	mono_counters_register ("JIT/decompose_typechecks", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_decompose_typechecks);
	mono_counters_register ("JIT/local_cprop", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_local_cprop);
	mono_counters_register ("JIT/local_emulate_ops", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_local_emulate_ops);
	mono_counters_register ("JIT/optimize_branches", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_optimize_branches);
	mono_counters_register ("JIT/handle_global_vregs", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_handle_global_vregs);
	mono_counters_register ("JIT/local_deadce", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_local_deadce);
	mono_counters_register ("JIT/local_alias_analysis", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_local_alias_analysis);
	mono_counters_register ("JIT/if_conversion", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_if_conversion);
	mono_counters_register ("JIT/bb_ordering", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_bb_ordering);
	mono_counters_register ("JIT/compile_dominator_info", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_compile_dominator_info);
	mono_counters_register ("JIT/compute_natural_loops", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_compute_natural_loops);
	mono_counters_register ("JIT/insert_safepoints", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_insert_safepoints);
	mono_counters_register ("JIT/ssa_compute", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_ssa_compute);
	mono_counters_register ("JIT/ssa_cprop", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_ssa_cprop);
	mono_counters_register ("JIT/ssa_deadce", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_ssa_deadce);
	mono_counters_register ("JIT/perform_abc_removal", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_perform_abc_removal);
	mono_counters_register ("JIT/ssa_remove", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_ssa_remove);
	mono_counters_register ("JIT/local_cprop2", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_local_cprop2);
	mono_counters_register ("JIT/handle_global_vregs2", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_handle_global_vregs2);
	mono_counters_register ("JIT/local_deadce2", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_local_deadce2);
	mono_counters_register ("JIT/optimize_branches2", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_optimize_branches2);
	mono_counters_register ("JIT/decompose_vtype_opts", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_decompose_vtype_opts);
	mono_counters_register ("JIT/decompose_array_access_opts", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_decompose_array_access_opts);
	mono_counters_register ("JIT/liveness_handle_exception_clauses2", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_liveness_handle_exception_clauses2);
	mono_counters_register ("JIT/analyze_liveness", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_analyze_liveness);
	mono_counters_register ("JIT/linear_scan", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_linear_scan);
	mono_counters_register ("JIT/arch_allocate_vars", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_arch_allocate_vars);
	mono_counters_register ("JIT/spill_global_var", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_spill_global_vars);
	mono_counters_register ("JIT/local_cprop3", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_local_cprop3);
	mono_counters_register ("JIT/local_deadce3", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_local_deadce3);
	mono_counters_register ("JIT/codegen", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_codegen);
	mono_counters_register ("JIT/create_jit_info", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_create_jit_info);
	mono_counters_register ("JIT/gc_create_gc_map", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_gc_create_gc_map);
	mono_counters_register ("JIT/save_seq_point_info", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_save_seq_point_info);
	mono_counters_register ("Total time spent JITting", MONO_COUNTER_JIT | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_jit_stats.jit_time);
	mono_counters_register ("Basic blocks", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.basic_blocks);
	mono_counters_register ("Max basic blocks", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.max_basic_blocks);
	mono_counters_register ("Allocated vars", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.allocate_var);
	mono_counters_register ("Code reallocs", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.code_reallocs);
	mono_counters_register ("Allocated code size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.allocated_code_size);
	mono_counters_register ("Allocated seq points size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.allocated_seq_points_size);
	mono_counters_register ("Inlineable methods", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.inlineable_methods);
	mono_counters_register ("Inlined methods", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.inlined_methods);
	mono_counters_register ("Regvars", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.regvars);
	mono_counters_register ("Locals stack size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.locals_stack_size);
	mono_counters_register ("Method cache lookups", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.methods_lookups);
	mono_counters_register ("Compiled CIL code size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.cil_code_size);
	mono_counters_register ("Native code size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.native_code_size);
	mono_counters_register ("Aliases found", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.alias_found);
	mono_counters_register ("Aliases eliminated", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.alias_removed);
	mono_counters_register ("Aliased loads eliminated", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.loads_eliminated);
	mono_counters_register ("Aliased stores eliminated", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.stores_eliminated);
	mono_counters_register ("Optimized immediate divisions", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.optimized_divisions);
	current_backend = g_new0 (MonoBackend, 1);
	init_backend (current_backend);
#endif
}

void
mini_jit_cleanup (void)
{
#ifndef DISABLE_JIT
	g_free (emul_opcode_map);
	g_free (emul_opcode_opcodes);
#endif
}

#ifndef ENABLE_LLVM
void
mono_llvm_emit_aot_file_info (MonoAotFileInfo *info, gboolean has_jitted_code)
{
	g_assert_not_reached ();
}

gpointer
mono_llvm_emit_aot_data (const char *symbol, guint8 *data, int data_len)
{
	g_assert_not_reached ();
}

gpointer
mono_llvm_emit_aot_data_aligned (const char *symbol, guint8 *data, int data_len, int align)
{
	g_assert_not_reached ();
}

#endif

#if !defined(ENABLE_LLVM_RUNTIME) && !defined(ENABLE_LLVM)

void
mono_llvm_cpp_throw_exception (void)
{
	g_assert_not_reached ();
}

void
mono_llvm_cpp_catch_exception (MonoLLVMInvokeCallback cb, gpointer arg, gboolean *out_thrown)
{
	g_assert_not_reached ();
}

#endif

#ifdef DISABLE_JIT

MonoCompile*
mini_method_compile (MonoMethod *method, guint32 opts, MonoDomain *domain, JitFlags flags, int parts, int aot_method_index)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_destroy_compile (MonoCompile *cfg)
{
	g_assert_not_reached ();
}

void
mono_add_patch_info (MonoCompile *cfg, int ip, MonoJumpInfoType type, gconstpointer target)
{
	g_assert_not_reached ();
}

#else // DISABLE_JIT

guint8*
mini_realloc_code_slow (MonoCompile *cfg, int size)
{
	const int EXTRA_CODE_SPACE = 16;

	if (cfg->code_len + size > (cfg->code_size - EXTRA_CODE_SPACE)) {
		while (cfg->code_len + size > (cfg->code_size - EXTRA_CODE_SPACE))
			cfg->code_size = cfg->code_size * 2 + EXTRA_CODE_SPACE;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		cfg->stat_code_reallocs++;
	}
	return cfg->native_code + cfg->code_len;
}

#endif /* DISABLE_JIT */

gboolean
mini_class_is_system_array (MonoClass *klass)
{
	return m_class_get_parent (klass) == mono_defaults.array_class;
}

/*
 * mono_target_pagesize:
 *
 *   query pagesize used to determine if an implicit NRE can be used
 */
int
mono_target_pagesize (void)
{
	/* We could query the system's pagesize via mono_pagesize (), however there
	 * are pitfalls: sysconf (3) is called on some posix like systems, and per
	 * POSIX.1-2008 this function doesn't have to be async-safe. Since this
	 * function can be called from a signal handler, we simplify things by
	 * using 4k on all targets. Implicit null-checks with an offset larger than
	 * 4k are _very_ uncommon, so we don't mind emitting an explicit null-check
	 * for those cases.
	 */
	return 4 * 1024;
}

MonoCPUFeatures
mini_get_cpu_features (MonoCompile* cfg)
{
	MonoCPUFeatures features = (MonoCPUFeatures)0;
#if !defined(MONO_CROSS_COMPILE)
	if (!cfg->compile_aot || cfg->use_current_cpu) {
		// detect current CPU features if we are in JIT mode or AOT with use_current_cpu flag.
#if defined(ENABLE_LLVM)
		features = mono_llvm_get_cpu_features (); // llvm has a nice built-in API to detect features
#elif defined(TARGET_AMD64) || defined(TARGET_X86)
		features = mono_arch_get_cpu_features ();
#endif
	}
#endif

#if defined(TARGET_ARM64)
	// All Arm64 devices have this set
	features |= MONO_CPU_ARM64_BASE; 
#endif

	// apply parameters passed via -mattr
	return (features | mono_cpu_features_enabled) & ~mono_cpu_features_disabled;
}
