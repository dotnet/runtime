/**
 * \file
 * Static single assign form support for the JIT compiler.
 *
 * Author:
 *    Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <string.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_JIT

#include "mini.h"
#include "mini-runtime.h"
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#define CREATE_PRUNED_SSA

//#define DEBUG_SSA 1

#define NEW_PHI(cfg,dest,val) do {	\
	MONO_INST_NEW ((cfg), (dest), OP_PHI); \
	(dest)->inst_c0 = (val);							   \
	} while (0)

typedef struct {
	MonoBasicBlock *bb;
	MonoInst *inst;
} MonoVarUsageInfo;

static void
unlink_target (MonoBasicBlock *bb, MonoBasicBlock *target)
{
	int i;

	for (i = 0; i < bb->out_count; i++) {
		if (bb->out_bb [i] == target) {
			bb->out_bb [i] = bb->out_bb [--bb->out_count];
			break;
		}
	}
	for (i = 0; i < target->in_count; i++) {
		if (target->in_bb [i] == bb) {
			target->in_bb [i] = target->in_bb [--target->in_count];
			break;

		}
	}
}

static void
unlink_unused_bblocks (MonoCompile *cfg)
{
	int i, j;
	MonoBasicBlock *bb;

	g_assert (cfg->comp_done & MONO_COMP_REACHABILITY);

	if (G_UNLIKELY (cfg->verbose_level > 1))
		printf ("\nUNLINK UNUSED BBLOCKS:\n");

	for (bb = cfg->bb_entry; bb && bb->next_bb;) {
		if (!(bb->next_bb->flags & BB_REACHABLE)) {
			bb->next_bb = bb->next_bb->next_bb;
		} else
			bb = bb->next_bb;
	}

	for (i = 1; i < cfg->num_bblocks; i++) {
		bb = cfg->bblocks [i];

		if (!(bb->flags & BB_REACHABLE)) {
			for (j = 0; j < bb->in_count; j++) {
				unlink_target (bb->in_bb [j], bb);
			}
			for (j = 0; j < bb->out_count; j++) {
				unlink_target (bb, bb->out_bb [j]);
			}
			if (G_UNLIKELY (cfg->verbose_level > 1))
				printf ("\tUnlinked BB%d\n", bb->block_num);
		}

	}
}

/**
 * remove_bb_from_phis:
 *
 *   Remove BB from the PHI statements in TARGET.
 */
static void
remove_bb_from_phis (MonoCompile *cfg, MonoBasicBlock *bb, MonoBasicBlock *target)
{
	MonoInst *ins;
	int i, j;

	for (i = 0; i < target->in_count; i++) {
		if (target->in_bb [i] == bb) {
			break;
		}
	}
	g_assert (i < target->in_count);

	for (ins = target->code; ins; ins = ins->next) {
		if (MONO_IS_PHI (ins)) {
			for (j = i; j < ins->inst_phi_args [0] - 1; ++j)
				ins->inst_phi_args [j + 1] = ins->inst_phi_args [j + 2];
			ins->inst_phi_args [0] --;
		}
		else
			break;
	}
}

static int
op_phi_to_move (int opcode)
{
	switch (opcode) {
	case OP_PHI:
		return OP_MOVE;
	case OP_FPHI:
		return OP_FMOVE;
	case OP_VPHI:
		return OP_VMOVE;
	case OP_XPHI:
		return OP_XMOVE;
	default:
		g_assert_not_reached ();
	}

	return -1;
}

static void
record_use (MonoCompile *cfg, MonoInst *var, MonoBasicBlock *bb, MonoInst *ins)
{
	MonoMethodVar *info;
	MonoVarUsageInfo *ui = (MonoVarUsageInfo *)mono_mempool_alloc (cfg->mempool, sizeof (MonoVarUsageInfo));

	info = MONO_VARINFO (cfg, var->inst_c0);

	ui->bb = bb;
	ui->inst = ins;
	info->uses = g_list_prepend_mempool (cfg->mempool, info->uses, ui);
}

typedef struct {
	MonoInst *var;
	int idx;
} RenameInfo;

static void
rename_phi_arguments_in_out_bbs(MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **stack)
{
	MonoInst *ins, *new_var;
	int i, j;

	for (i = 0; i < bb->out_count; i++) {
		MonoBasicBlock *n = bb->out_bb [i];

		for (j = 0; j < n->in_count; j++)
			if (n->in_bb [j] == bb)
				break;

		for (ins = n->code; ins; ins = ins->next) {
			if (MONO_IS_PHI (ins)) {
				target_mgreg_t idx = ins->inst_c0;
				if (stack [idx])
					new_var = stack [idx];
				else
					new_var = cfg->varinfo [idx];
#ifdef DEBUG_SSA
				printf ("FOUND PHI %d (%d, %d)\n", GTMREG_TO_INT (idx), j, new_var->inst_c0);
#endif
				ins->inst_phi_args [j + 1] = new_var->dreg;
				record_use (cfg,  new_var, n, ins);
				if (G_UNLIKELY (cfg->verbose_level >= 4))
					printf ("\tAdd PHI R%d <- R%d to BB%d\n", ins->dreg, new_var->dreg, n->block_num);
			}
			else
				/* The phi nodes are at the beginning of the bblock */
				break;
		}
	}
}

static int
create_new_vars (MonoCompile *cfg, int max_vars, MonoBasicBlock *bb, gboolean *originals_used, MonoInst **stack, guint32 *lvreg_stack, gboolean *lvreg_defined, RenameInfo **stack_history, int *stack_history_size)
{
	MonoInst *ins, *new_var;
	int i, stack_history_len = 0;

	for (ins = bb->code; ins; ins = ins->next) {
		const char *spec = INS_INFO (ins->opcode);
		int num_sregs;
		int sregs [MONO_MAX_SRC_REGS];

#ifdef DEBUG_SSA
		printf ("\tProcessing "); mono_print_ins (ins);
#endif
		if (ins->opcode == OP_NOP)
			continue;

		/* SREGs */
		num_sregs = mono_inst_get_src_registers (ins, sregs);
		for (i = 0; i < num_sregs; ++i) {
			if (spec [MONO_INST_SRC1 + i] != ' ') {
				MonoInst *var = get_vreg_to_inst (cfg, sregs [i]);
				if (var && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
					target_mgreg_t idx = var->inst_c0;
					if (stack [idx]) {
						if (var->opcode != OP_ARG)
							g_assert (stack [idx]);
						sregs [i] = stack [idx]->dreg;
						record_use (cfg, stack [idx], bb, ins);
					}
					else
						record_use (cfg, var, bb, ins);
				}
				else if (G_UNLIKELY (!var && lvreg_stack [sregs [i]]))
					sregs [i] = lvreg_stack [sregs [i]];
			}
		}
		mono_inst_set_src_registers (ins, sregs);

		if (MONO_IS_STORE_MEMBASE (ins)) {
			MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);
			if (var && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
				target_mgreg_t idx = var->inst_c0;
				if (stack [idx]) {
					if (var->opcode != OP_ARG)
						g_assert (stack [idx]);
					ins->dreg = stack [idx]->dreg;
					record_use (cfg, stack [idx], bb, ins);
				}
				else
					record_use (cfg, var, bb, ins);
			}
			else if (G_UNLIKELY (!var && lvreg_stack [ins->dreg]))
				ins->dreg = lvreg_stack [ins->dreg];
		}

		/* DREG */
		if ((spec [MONO_INST_DEST] != ' ') && !MONO_IS_STORE_MEMBASE (ins)) {
			MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);
			MonoMethodVar *info;

			if (var && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
				int idx = GTMREG_TO_INT (var->inst_c0);
				g_assert (idx < max_vars);

				if (var->opcode == OP_ARG)
					originals_used [idx] = TRUE;

				if (stack_history_len + 128 > *stack_history_size) {
					*stack_history_size += 1024;
					RenameInfo *new_history = mono_mempool_alloc (cfg->mempool, sizeof (RenameInfo) * *stack_history_size);
					memcpy (new_history, *stack_history, stack_history_len * sizeof (RenameInfo));
					*stack_history = new_history;
				}

				(*stack_history) [stack_history_len].var = stack [idx];
				(*stack_history) [stack_history_len].idx = idx;
				stack_history_len ++;

				if (originals_used [idx]) {
					new_var = mono_compile_create_var (cfg, var->inst_vtype, OP_LOCAL);
					new_var->flags = var->flags;
					MONO_VARINFO (cfg, new_var->inst_c0)->reg = idx;

					if (cfg->verbose_level >= 4)
						printf ("  R%d -> R%d\n", var->dreg, new_var->dreg);

					stack [idx] = new_var;

					ins->dreg = new_var->dreg;
					var = new_var;
				}
				else {
					stack [idx] = var;
					originals_used [idx] = TRUE;
				}

				info = MONO_VARINFO (cfg, var->inst_c0);
				info->def = ins;
				info->def_bb = bb;
			}
			else if (G_UNLIKELY (!var && lvreg_defined [ins->dreg] && (ins->dreg >= MONO_MAX_IREGS))) {
				/* Perform renaming for local vregs */
				lvreg_stack [ins->dreg] = vreg_is_ref (cfg, ins->dreg) ? mono_alloc_ireg_ref (cfg) : mono_alloc_preg (cfg);
				ins->dreg = lvreg_stack [ins->dreg];
			}
			else
				lvreg_defined [ins->dreg] = TRUE;
		}

#ifdef DEBUG_SSA
		printf ("\tAfter processing "); mono_print_ins (ins);
#endif
	}

	return stack_history_len;
}

static void
restore_stack (MonoInst **stack, RenameInfo *stack_history, int stack_history_len)
{
	int i = stack_history_len;
	while (i-- > 0) {
		stack [stack_history [i].idx] = stack_history [i].var;
	}
}

typedef struct {
	GSList *blocks;
	RenameInfo *history;
	int size;
	int len;
} RenameStackInfo;

/**
 * mono_ssa_rename_vars:
 * Implement renaming of SSA variables. Also compute def-use information in parallel.
 * \p stack_history points to an area of memory which can be used for storing changes
 * made to the stack, so they can be reverted later.
 */
static void
mono_ssa_rename_vars (MonoCompile *cfg, int max_vars, MonoBasicBlock *bb)
{
	GSList *blocks = NULL;
	RenameStackInfo* rename_stack;
	int rename_stack_size, rename_stack_idx = 0;
	RenameInfo *stack_history;
	int stack_history_size;
	gboolean *originals;
	guint32 *lvreg_stack;
	gboolean *lvreg_defined;
	MonoInst **stack;

	stack = g_newa (MonoInst*, cfg->num_varinfo);
	memset (stack, 0, sizeof (MonoInst *) * cfg->num_varinfo);
	lvreg_stack = g_new0 (guint32, cfg->next_vreg);
	lvreg_defined = g_new0 (gboolean, cfg->next_vreg);
	stack_history_size = 10240;
	stack_history = g_new (RenameInfo, stack_history_size);
	originals = g_new0 (gboolean, cfg->num_varinfo);
	rename_stack_size = 16;
	rename_stack = g_new (RenameStackInfo, rename_stack_size);

	do {
		if (G_UNLIKELY (cfg->verbose_level >= 4))
			printf ("\nRENAME VARS BLOCK %d:\n", bb->block_num);

		int stack_history_len = create_new_vars (cfg, max_vars, bb, originals, stack, lvreg_stack, lvreg_defined, &stack_history, &stack_history_size);
		rename_phi_arguments_in_out_bbs (cfg, bb, stack);

		if (bb->dominated) {
			if (rename_stack_idx >= rename_stack_size - 1) {
				rename_stack_size += MIN(rename_stack_size, 1024);
				rename_stack = g_realloc(rename_stack, sizeof(RenameStackInfo)*rename_stack_size);
			}

			RenameStackInfo* info = rename_stack + rename_stack_idx;
			rename_stack_idx++;
			info->blocks = blocks;
			info->history = stack_history;
			info->size = stack_history_size;
			info->len = stack_history_len;
			stack_history += stack_history_len;
			stack_history_size -= stack_history_len;
			blocks = bb->dominated;
		} else {
			restore_stack (stack, stack_history, stack_history_len);
			blocks = blocks->next;

			while (!blocks && rename_stack_idx > 0) {
				rename_stack_idx--;
				RenameStackInfo* info = rename_stack + rename_stack_idx;
				blocks = info->blocks ? info->blocks->next : NULL;
				stack_history = info->history;
				stack_history_size = info->size;
				stack_history_len = info->len;
				restore_stack (stack, stack_history, stack_history_len);
			}
		}

		if (blocks)
			bb = (MonoBasicBlock*) blocks->data;
	} while (blocks);

	g_free (stack_history);
	g_free (originals);
	g_free (lvreg_stack);
	g_free (lvreg_defined);
	g_free (rename_stack);
	cfg->comp_done |= MONO_COMP_SSA_DEF_USE;
}

void
mono_ssa_compute (MonoCompile *cfg)
{
	int i, j, idx, bitsize;
	MonoBitSet *set;
	MonoMethodVar *vinfo = g_new0 (MonoMethodVar, cfg->num_varinfo);
	MonoInst *ins;
	guint8 *buf, *buf_start;

	g_assert (!(cfg->comp_done & MONO_COMP_SSA));

	g_assert (!cfg->disable_ssa);

	if (cfg->verbose_level >= 4)
		printf ("\nCOMPUTE SSA %d (R%d-)\n\n", cfg->num_varinfo, cfg->next_vreg);

#ifdef CREATE_PRUNED_SSA
	/* we need liveness for pruned SSA */
	if (!(cfg->comp_done & MONO_COMP_LIVENESS))
		mono_analyze_liveness (cfg);
#endif

	mono_compile_dominator_info (cfg, MONO_COMP_DOM | MONO_COMP_IDOM | MONO_COMP_DFRONTIER);

	bitsize = mono_bitset_alloc_size (cfg->num_bblocks, 0);
	buf = buf_start = (guint8 *)g_malloc0 (mono_bitset_alloc_size (cfg->num_bblocks, 0) * cfg->num_varinfo);

	for (i = 0; i < cfg->num_varinfo; ++i) {
		vinfo [i].def_in = mono_bitset_mem_new (buf, cfg->num_bblocks, 0);
		buf += bitsize;
		vinfo [i].idx = i;
		/* implicit reference at start */
		if (cfg->varinfo [i]->opcode == OP_ARG)
			mono_bitset_set_fast (vinfo [i].def_in, 0);
	}

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MONO_BB_FOR_EACH_INS (cfg->bblocks [i], ins) {
			if (ins->opcode == OP_NOP)
				continue;

			if (!MONO_IS_STORE_MEMBASE (ins) && get_vreg_to_inst (cfg, ins->dreg)) {
				mono_bitset_set_fast (vinfo [get_vreg_to_inst (cfg, ins->dreg)->inst_c0].def_in, i);
			}
		}
	}

	/* insert phi functions */
	for (i = 0; i < cfg->num_varinfo; ++i) {
		MonoInst *var = cfg->varinfo [i];

#if SIZEOF_REGISTER == 4
		if (var->type == STACK_I8 && !COMPILE_LLVM (cfg))
			continue;
#endif
		if (var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))
			continue;

		/* Most variables have only one definition */
		if (mono_bitset_count (vinfo [i].def_in) <= 1)
			continue;

		set = mono_compile_iterated_dfrontier (cfg, vinfo [i].def_in);

		if (cfg->verbose_level >= 4) {
			if (mono_bitset_count (set) > 0) {
				printf ("\tR%d needs PHI functions in ", var->dreg);
				mono_blockset_print (cfg, set, "", -1);
			}
		}

		mono_bitset_foreach_bit (set, idx, cfg->num_bblocks) {
			MonoBasicBlock *bb = cfg->bblocks [idx];

			/* fixme: create pruned SSA? we would need liveness information for that */

			if (bb == cfg->bb_exit && !COMPILE_LLVM (cfg))
				continue;

			if ((cfg->comp_done & MONO_COMP_LIVENESS) && !mono_bitset_test_fast (bb->live_in_set, i)) {
				//printf ("%d is not live in BB%d %s\n", i, bb->block_num, mono_method_full_name (cfg->method, TRUE));
				continue;
			}

			NEW_PHI (cfg, ins, i);

			switch (var->type) {
			case STACK_I4:
			case STACK_I8:
			case STACK_PTR:
			case STACK_MP:
			case STACK_OBJ:
				ins->opcode = OP_PHI;
				break;
			case STACK_R8:
				ins->opcode = OP_FPHI;
				break;
			case STACK_VTYPE:
				ins->opcode = MONO_CLASS_IS_SIMD (cfg, var->klass) ? OP_XPHI : OP_VPHI;
				break;
			}

 			if (m_type_is_byref (var->inst_vtype))
 				ins->klass = mono_defaults.int_class;
 			else
 				ins->klass = var->klass;

			ins->inst_phi_args = (int *)mono_mempool_alloc0 (cfg->mempool, sizeof (int) * (cfg->bblocks [idx]->in_count + 1));
			ins->inst_phi_args [0] = cfg->bblocks [idx]->in_count;

			/* For debugging */
			for (j = 0; j < cfg->bblocks [idx]->in_count; ++j)
				ins->inst_phi_args [j + 1] = -1;

			ins->dreg = cfg->varinfo [i]->dreg;

			mono_bblock_insert_before_ins (bb, bb->code, ins);

#ifdef DEBUG_SSA
			printf ("ADD PHI BB%d %s\n", cfg->bblocks [idx]->block_num, mono_method_full_name (cfg->method, TRUE));
#endif
		}
	}

	/* free the stuff */
	g_free (vinfo);
	g_free (buf_start);

	/* Renaming phase */
	mono_ssa_rename_vars (cfg, cfg->num_varinfo, cfg->bb_entry);

	if (cfg->verbose_level >= 4)
		printf ("\nEND COMPUTE SSA.\n\n");

	cfg->comp_done |= MONO_COMP_SSA;
}

/*
 * mono_ssa_remove_gsharedvt:
 *
 *   Same as mono_ssa_remove, but only remove phi nodes for gsharedvt variables.
 */
void
mono_ssa_remove_gsharedvt (MonoCompile *cfg)
{
	MonoInst *ins, *var, *move;
	int i, j, first;

	/*
	 * When compiling gsharedvt code, we need to get rid of the VPHI instructions,
	 * since they cannot be handled later in the llvm backend.
	 */
	g_assert (cfg->comp_done & MONO_COMP_SSA);

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		if (cfg->verbose_level >= 4)
			printf ("\nREMOVE SSA %d:\n", bb->block_num);

		for (ins = bb->code; ins; ins = ins->next) {
			if (!(MONO_IS_PHI (ins) && ins->opcode == OP_VPHI && mini_is_gsharedvt_variable_type (m_class_get_byval_arg (ins->klass))))
				continue;

			g_assert (ins->inst_phi_args [0] == bb->in_count);
			var = get_vreg_to_inst (cfg, ins->dreg);

			/* Check for PHI nodes where all the inputs are the same */
			first = ins->inst_phi_args [1];

			for (j = 1; j < bb->in_count; ++j)
				if (first != ins->inst_phi_args [j + 1])
					break;

			if ((bb->in_count > 1) && (j == bb->in_count)) {
				ins->opcode = GINT_TO_OPCODE (op_phi_to_move (ins->opcode));
				if (ins->opcode == OP_VMOVE)
					g_assert (ins->klass);
				ins->sreg1 = first;
			} else {
				for (j = 0; j < bb->in_count; j++) {
					MonoBasicBlock *pred = bb->in_bb [j];
					int sreg = ins->inst_phi_args [j + 1];

					if (cfg->verbose_level >= 4)
						printf ("\tADD R%d <- R%d in BB%d\n", var->dreg, sreg, pred->block_num);
					if (var->dreg != sreg) {
						MONO_INST_NEW (cfg, move, op_phi_to_move (ins->opcode));
						if (move->opcode == OP_VMOVE) {
							g_assert (ins->klass);
							move->klass = ins->klass;
						}
						move->dreg = var->dreg;
						move->sreg1 = sreg;
						mono_add_ins_to_end (pred, move);
					}
				}

				NULLIFY_INS (ins);
			}
		}
	}
}

void
mono_ssa_remove (MonoCompile *cfg)
{
	MonoInst *ins, *move;
	int bbindex, i, j, first;

	g_assert (cfg->comp_done & MONO_COMP_SSA);

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		if (cfg->verbose_level >= 4)
			printf ("\nREMOVE SSA %d:\n", bb->block_num);

		for (ins = bb->code; ins; ins = ins->next) {
			if (MONO_IS_PHI (ins)) {
				g_assert (ins->inst_phi_args [0] == bb->in_count);
				MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);

				/* Check for PHI nodes where all the inputs are the same */
				first = ins->inst_phi_args [1];

				for (j = 1; j < bb->in_count; ++j)
					if (first != ins->inst_phi_args [j + 1])
						break;

				if ((bb->in_count > 1) && (j == bb->in_count)) {
					ins->opcode = GINT_TO_OPCODE (op_phi_to_move (ins->opcode));
					if (ins->opcode == OP_VMOVE)
						g_assert (ins->klass);
					ins->sreg1 = first;
				} else {
					for (j = 0; j < bb->in_count; j++) {
						MonoBasicBlock *pred = bb->in_bb [j];
						int sreg = ins->inst_phi_args [j + 1];

						if (cfg->verbose_level >= 4)
							printf ("\tADD R%d <- R%d in BB%d\n", var->dreg, sreg, pred->block_num);
						if (var->dreg != sreg) {
							MONO_INST_NEW (cfg, move, op_phi_to_move (ins->opcode));
							if (move->opcode == OP_VMOVE) {
								g_assert (ins->klass);
								move->klass = ins->klass;
							}
							move->dreg = var->dreg;
							move->sreg1 = sreg;
							mono_add_ins_to_end (pred, move);
						}
					}

					NULLIFY_INS (ins);
				}
			}
		}
	}

	if (cfg->verbose_level >= 4) {
		for (i = 0; i < cfg->num_bblocks; ++i) {
			MonoBasicBlock *bb = cfg->bblocks [i];

			mono_print_bb (bb, "AFTER REMOVE SSA:");
		}
	}

	/*
	 * Removal of SSA form introduces many copies. To avoid this, we tyry to coalesce
	 * the variables if possible. Since the newly introduced SSA variables don't
	 * have overlapping live ranges (because we don't do agressive optimization), we
	 * can coalesce them into the original variable.
	 */

	for (bbindex = 0; bbindex < cfg->num_bblocks; ++bbindex) {
		MonoBasicBlock *bb = cfg->bblocks [bbindex];

		for (ins = bb->code; ins; ins = ins->next) {
			const char *spec = INS_INFO (ins->opcode);
			int num_sregs;
			int sregs [MONO_MAX_SRC_REGS];

			if (ins->opcode == OP_NOP)
				continue;

			if (spec [MONO_INST_DEST] != ' ') {
				MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);

				if (var) {
					MonoMethodVar *vmv = MONO_VARINFO (cfg, var->inst_c0);

					/*
					 * The third condition avoids coalescing with variables eliminated
					 * during deadce.
					 */
					if ((vmv->reg != -1) && (vmv->idx != vmv->reg) && (MONO_VARINFO (cfg, vmv->reg)->reg != -1)) {
						printf ("COALESCE: R%d -> R%d\n", ins->dreg, cfg->varinfo [vmv->reg]->dreg);
						ins->dreg = cfg->varinfo [vmv->reg]->dreg;
					}
				}
			}

			num_sregs = mono_inst_get_src_registers (ins, sregs);
			for (i = 0; i < num_sregs; ++i) {
				MonoInst *var = get_vreg_to_inst (cfg, sregs [i]);

				if (var) {
					MonoMethodVar *vmv = MONO_VARINFO (cfg, var->inst_c0);

					if ((vmv->reg != -1) && (vmv->idx != vmv->reg) && (MONO_VARINFO (cfg, vmv->reg)->reg != -1)) {
						printf ("COALESCE: R%d -> R%d\n", sregs [i], cfg->varinfo [vmv->reg]->dreg);
						sregs [i] = cfg->varinfo [vmv->reg]->dreg;
					}
				}
			}
			mono_inst_set_src_registers (ins, sregs);
		}
	}

	for (i = 0; i < cfg->num_varinfo; ++i) {
		MONO_VARINFO (cfg, i)->reg = -1;
	}

	if (cfg->comp_done & MONO_COMP_REACHABILITY)
		unlink_unused_bblocks (cfg);

	cfg->comp_done &= ~MONO_COMP_LIVENESS;

	cfg->comp_done &= ~MONO_COMP_SSA;
}

static void
mono_ssa_create_def_use (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	MonoInst *ins;
	int i;

	g_assert (!(cfg->comp_done & MONO_COMP_SSA_DEF_USE));

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		for (ins = bb->code; ins; ins = ins->next) {
			const char *spec = INS_INFO (ins->opcode);
			MonoMethodVar *info;
			int num_sregs;
			int sregs [MONO_MAX_SRC_REGS];

			if (ins->opcode == OP_NOP)
				continue;

			/* SREGs */
			num_sregs = mono_inst_get_src_registers (ins, sregs);
			for (i = 0; i < num_sregs; ++i) {
				MonoInst *var = get_vreg_to_inst (cfg, sregs [i]);
				if (var && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)))
					record_use (cfg, var, bb, ins);
			}

			if (MONO_IS_STORE_MEMBASE (ins)) {
				MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);
				if (var && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)))
					record_use (cfg, var, bb, ins);
			}

			if (MONO_IS_PHI (ins)) {
				for (i = ins->inst_phi_args [0]; i > 0; i--) {
					g_assert (ins->inst_phi_args [i] != -1);
					record_use (cfg,  get_vreg_to_inst (cfg, ins->inst_phi_args [i]), bb, ins);
				}
			}

			/* DREG */
			if ((spec [MONO_INST_DEST] != ' ') && !MONO_IS_STORE_MEMBASE (ins)) {
				MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);

				if (var && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
					info = MONO_VARINFO (cfg, var->inst_c0);
					info->def = ins;
					info->def_bb = bb;
				}
			}
		}
	}

	cfg->comp_done |= MONO_COMP_SSA_DEF_USE;
}

static void
mono_ssa_copyprop (MonoCompile *cfg)
{
	int i, index;
	GList *l;

	g_assert ((cfg->comp_done & MONO_COMP_SSA_DEF_USE));

	for (index = 0; index < cfg->num_varinfo; ++index) {
		MonoInst *var = cfg->varinfo [index];
		MonoMethodVar *info = MONO_VARINFO (cfg, index);

		if (info->def && (MONO_IS_MOVE (info->def))) {
			MonoInst *var2 = get_vreg_to_inst (cfg, info->def->sreg1);

			if (var2 && !(var2->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)) && MONO_VARINFO (cfg, var2->inst_c0)->def && (!MONO_IS_PHI (MONO_VARINFO (cfg, var2->inst_c0)->def))) {
				/* Rewrite all uses of var to be uses of var2 */
				int dreg = var->dreg;
				int sreg1 = var2->dreg;

				l = info->uses;
				while (l) {
					MonoVarUsageInfo *u = (MonoVarUsageInfo*)l->data;
					MonoInst *ins = u->inst;
					GList *next = l->next;
					int num_sregs;
					int sregs [MONO_MAX_SRC_REGS];

					num_sregs = mono_inst_get_src_registers (ins, sregs);
					for (i = 0; i < num_sregs; ++i) {
						if (sregs [i] == dreg)
							break;
					}
					if (i < num_sregs) {
						g_assert (sregs [i] == dreg);
						sregs [i] = sreg1;
						mono_inst_set_src_registers (ins, sregs);
					} else if (MONO_IS_STORE_MEMBASE (ins) && ins->dreg == dreg) {
						ins->dreg = sreg1;
					} else if (MONO_IS_PHI (ins)) {
						for (i = ins->inst_phi_args [0]; i > 0; i--) {
							int sreg = ins->inst_phi_args [i];
							if (sreg == var->dreg)
								break;
						}
						g_assert (i > 0);
						ins->inst_phi_args [i] = sreg1;
					}
					else
						g_assert_not_reached ();

					record_use (cfg, var2, u->bb, ins);

					l = next;
				}

				info->uses = NULL;
			}
		}
	}

	if (cfg->verbose_level >= 4) {
		MonoBasicBlock *bb;

		for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
			mono_print_bb (bb, "AFTER SSA COPYPROP");
	}
}

static int
evaluate_ins (MonoCompile *cfg, MonoInst *ins, MonoInst **res, MonoInst **carray)
{
	MonoInst *args [MONO_MAX_SRC_REGS];
	int rs [MONO_MAX_SRC_REGS];
	MonoInst *c0;
	gboolean const_args = TRUE;
	const char *spec = INS_INFO (ins->opcode);
	int num_sregs, i;
	int sregs [MONO_MAX_SRC_REGS];

	/* Short-circuit this */
	if (ins->opcode == OP_ICONST) {
		*res = ins;
		return 1;
	}

	if (ins->opcode == OP_NOP)
		return 2;

	num_sregs = mono_inst_get_src_registers (ins, sregs);

	if (num_sregs > 2)
		return 2;

	for (i = 0; i < MONO_MAX_SRC_REGS; ++i)
		args [i] = NULL;
	for (i = 0; i < num_sregs; ++i) {
		MonoInst *var = get_vreg_to_inst (cfg, sregs [i]);

		rs [i] = 2;
		args [i] = carray [sregs [i]];
		if (args [i])
			rs [i] = 1;
		else if (var && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)))
			rs [i] = MONO_VARINFO (cfg, var->inst_c0)->cpstate;
		if (rs [i] != 1)
			const_args = FALSE;
	}

	c0 = NULL;

	if (num_sregs > 0 && const_args) {
		g_assert (num_sregs <= 2);
		if ((spec [MONO_INST_DEST] != ' ') && carray [ins->dreg]) {
			// Cached value
			*res = carray [ins->dreg];
			return 1;
		}
		c0 = mono_constant_fold_ins (cfg, ins, args [0], args [1], FALSE);
		if (c0) {
			if (G_UNLIKELY (cfg->verbose_level > 1)) {
				printf ("\t cfold -> ");
				mono_print_ins (c0);
			}
			*res = c0;
			return 1;
		}
		else
			/* Can't cfold this ins */
			return 2;
	}

	if (num_sregs == 0)
		return 2;
	for (i = 0; i < num_sregs; ++i) {
		if (rs [i] == 2)
			return 2;
	}
	return 0;
}

static void
change_varstate (MonoCompile *cfg, GList **cvars, MonoMethodVar *info, char state, MonoInst *c0, MonoInst **carray)
{
	if (info->cpstate >= state)
		return;

	info->cpstate = state;

	if (G_UNLIKELY (cfg->verbose_level > 1))
		printf ("\tState of R%d set to %d\n", cfg->varinfo [info->idx]->dreg, info->cpstate);

	if (state == 1)
		g_assert (c0);

	carray [cfg->varinfo [info->idx]->dreg] = c0;

	if (!g_list_find (*cvars, info)) {
		*cvars = g_list_prepend (*cvars, info);
	}
}

static void
add_cprop_bb (MonoCompile *cfg, MonoBasicBlock *bb, GList **bblist)
{
	if (G_UNLIKELY (cfg->verbose_level > 1))
		printf ("\tAdd BB%d to worklist\n", bb->block_num);

    if (!(bb->flags &  BB_REACHABLE)) {
	    bb->flags |= BB_REACHABLE;
		*bblist = g_list_prepend (*bblist, bb);
	}
}

static void
visit_inst (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, GList **cvars, GList **bblist, MonoInst **carray)
{
	const char *spec = INS_INFO (ins->opcode);

	if (ins->opcode == OP_NOP)
		return;

	if (cfg->verbose_level > 1)
		mono_print_ins (ins);

	/* FIXME: Support longs/floats */
	/* FIXME: Work on vregs as well */

	if (MONO_IS_PHI (ins)) {
		MonoMethodVar *info = MONO_VARINFO (cfg, get_vreg_to_inst (cfg, ins->dreg)->inst_c0);
		MonoInst *c0 = NULL;
		int j;

		for (j = 1; j <= ins->inst_phi_args [0]; j++) {
			MonoInst *var = get_vreg_to_inst (cfg, ins->inst_phi_args [j]);
			MonoMethodVar *mv = MONO_VARINFO (cfg, var->inst_c0);
			MonoInst *src = mv->def;

			if (mv->def_bb && !(mv->def_bb->flags & BB_REACHABLE))
				continue;

			if (!mv->def || !src || mv->cpstate == 2) {
				change_varstate (cfg, cvars, info, 2, NULL, carray);
				break;
			}

			if (mv->cpstate == 0)
				continue;

			g_assert (carray [var->dreg]);

			if (!c0)
				c0 = carray [var->dreg];

			/* FIXME: */
			if (c0->opcode != OP_ICONST) {
				change_varstate (cfg, cvars, info, 2, NULL, carray);
				break;
			}

			if (carray [var->dreg]->inst_c0 != c0->inst_c0) {
				change_varstate (cfg, cvars, info, 2, NULL, carray);
				break;
			}
		}

		if (c0 && info->cpstate < 1) {
			change_varstate (cfg, cvars, info, 1, c0, carray);

			g_assert (c0->opcode == OP_ICONST);
		}
	}
	else if (!MONO_IS_STORE_MEMBASE (ins) && ((spec [MONO_INST_SRC1] != ' ') || (spec [MONO_INST_SRC2] != ' ') || (spec [MONO_INST_DEST] != ' '))) {
		MonoInst *var, *c0;
		int state;

		if (spec [MONO_INST_DEST] !=  ' ')
			var = get_vreg_to_inst (cfg, ins->dreg);
		else
			var = NULL;

		c0 = NULL;
		state = evaluate_ins (cfg, ins, &c0, carray);

		if (var && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
			MonoMethodVar *info = MONO_VARINFO (cfg, var->inst_c0);

			if (info->cpstate < 2) {
				if (state == 1)
					change_varstate (cfg, cvars, info, 1, c0, carray);
				else if (state == 2)
					change_varstate (cfg, cvars, info, 2, NULL, carray);
			}
		}
		else if (!var && (ins->dreg != -1)) {
			/*
			 * We don't record def-use information for local vregs since it would be
			 * expensive. Instead, we depend on the fact that all uses of the vreg are in
			 * the same bblock, so they will be examined after the definition.
			 * FIXME: This isn't true if the ins is visited through an SSA edge.
			 */
			if (c0) {
				carray [ins->dreg] = c0;
			} else {
				if (carray [ins->dreg]) {
					/*
					 * The state of the vreg changed from constant to non-constant
					 * -> need to rescan the whole bblock.
					 */
					carray [ins->dreg] = NULL;
					/* FIXME: Speed this up */

					if (!g_list_find (*bblist, bb))
						*bblist = g_list_prepend (*bblist, bb);
				}
			}
		}

		if (MONO_IS_JUMP_TABLE (ins)) {
			int i;
			MonoJumpInfoBBTable *table = (MonoJumpInfoBBTable *)MONO_JUMP_TABLE_FROM_INS (ins);

			if (!ins->next || ins->next->opcode != OP_PADD) {
				/* The PADD was optimized away */
				/* FIXME: handle this as well */
				for (i = 0; i < table->table_size; i++)
					if (table->table [i])
						add_cprop_bb (cfg, table->table [i], bblist);
				return;
			}

			g_assert (ins->next->opcode == OP_PADD);
			g_assert (ins->next->sreg1 == ins->dreg);

			if (carray [ins->next->sreg2]) {
#if SIZEOF_REGISTER == 8
				int idx = GTMREG_TO_INT (carray [ins->next->sreg2]->inst_c0 >> 3);
#else
				int idx = GTMREG_TO_INT (carray [ins->next->sreg2]->inst_c0 >> 2);
#endif
				if ((idx < 0) || (idx >= table->table_size))
					/* Out-of-range, no branch is executed */
					return;
				else
					if (table->table [idx])
						add_cprop_bb (cfg, table->table [idx], bblist);
			}
			else {
				for (i = 0; i < table->table_size; i++)
					if (table->table [i])
						add_cprop_bb (cfg, table->table [i], bblist);
			}
		}

		if (ins->opcode == OP_SWITCH) {
			int i;
			MonoJumpInfoBBTable *table = (MonoJumpInfoBBTable *)ins->inst_p0;

			for (i = 0; i < table->table_size; i++)
				if (table->table [i])
					add_cprop_bb (cfg, table->table [i], bblist);
		}

		/* Handle COMPARE+BRCOND pairs */
		if (ins->next && MONO_IS_COND_BRANCH_OP (ins->next)) {
			if (c0) {
				g_assert (c0->opcode == OP_ICONST);

				if (c0->inst_c0)
					ins->next->flags |= MONO_INST_CFOLD_TAKEN;
				else
					ins->next->flags |= MONO_INST_CFOLD_NOT_TAKEN;
			}
			else {
				ins->next->flags &= ~(MONO_INST_CFOLD_TAKEN | MONO_INST_CFOLD_NOT_TAKEN);
			}

			visit_inst (cfg, bb, ins->next, cvars, bblist, carray);
		}
	} else if (ins->opcode == OP_BR) {
		add_cprop_bb (cfg, ins->inst_target_bb, bblist);
	} else if (MONO_IS_COND_BRANCH_OP (ins)) {
		if (ins->flags & MONO_INST_CFOLD_TAKEN) {
			add_cprop_bb (cfg, ins->inst_true_bb, bblist);
		} else if (ins->flags & MONO_INST_CFOLD_NOT_TAKEN) {
			if (ins->inst_false_bb)
				add_cprop_bb (cfg, ins->inst_false_bb, bblist);
        } else {
			add_cprop_bb (cfg, ins->inst_true_bb, bblist);
			if (ins->inst_false_bb)
				add_cprop_bb (cfg, ins->inst_false_bb, bblist);
		}
	}
}

/**
 * fold_ins:
 *
 *   Replace INS with its constant value, if it exists
 */
static void
fold_ins (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, MonoInst **carray)
{
	const char *spec = INS_INFO (ins->opcode);
	int opcode2;
	int num_sregs = mono_inst_get_num_src_registers (ins);

	if ((ins->opcode != OP_NOP) && (ins->dreg != -1) && !MONO_IS_STORE_MEMBASE (ins)) {
		if (carray [ins->dreg] && (spec [MONO_INST_DEST] == 'i') && (ins->dreg >= MONO_MAX_IREGS)) {
			/* Perform constant folding */
			/* FIXME: */
			g_assert (carray [ins->dreg]->opcode == OP_ICONST);
			ins->opcode = OP_ICONST;
			ins->inst_c0 = carray [ins->dreg]->inst_c0;
			MONO_INST_NULLIFY_SREGS (ins);
		} else if (num_sregs == 2 && carray [ins->sreg2]) {
			/* Perform op->op_imm conversion */
			opcode2 = mono_op_to_op_imm (ins->opcode);
			if (opcode2 != -1) {
				ins->opcode = GINT_TO_OPCODE (opcode2);
				ins->inst_imm = carray [ins->sreg2]->inst_c0;
				ins->sreg2 = -1;

				if ((opcode2 == OP_VOIDCALL) || (opcode2 == OP_CALL) || (opcode2 == OP_LCALL) || (opcode2 == OP_FCALL))
					((MonoCallInst*)ins)->fptr = (gpointer)(uintptr_t)ins->inst_imm;
			}
		} else {
			/* FIXME: Handle 3 op insns */
		}

		if (MONO_IS_JUMP_TABLE (ins)) {
			int i;
			MonoJumpInfoBBTable *table = (MonoJumpInfoBBTable *)MONO_JUMP_TABLE_FROM_INS (ins);

			if (!ins->next || ins->next->opcode != OP_PADD) {
				/* The PADD was optimized away */
				/* FIXME: handle this as well */
				return;
			}

			g_assert (ins->next->opcode == OP_PADD);
			g_assert (ins->next->sreg1 == ins->dreg);
			g_assert (ins->next->next->opcode == OP_LOAD_MEMBASE);

			if (carray [ins->next->sreg2]) {
				/* Convert to a simple branch */
#if SIZEOF_REGISTER == 8
				int idx = GTMREG_TO_INT (carray [ins->next->sreg2]->inst_c0 >> 3);
#else
				int idx = GTMREG_TO_INT (carray [ins->next->sreg2]->inst_c0 >> 2);
#endif

				if (!((idx >= 0) && (idx < table->table_size))) {
					/* Out of range, eliminate the whole switch */
					for (i = 0; i < table->table_size; ++i) {
						remove_bb_from_phis (cfg, bb, table->table [i]);
						mono_unlink_bblock (cfg, bb, table->table [i]);
					}

					NULLIFY_INS (ins);
					NULLIFY_INS (ins->next);
					NULLIFY_INS (ins->next->next);
					if (ins->next->next->next)
						NULLIFY_INS (ins->next->next->next);

					return;
				}

				if (!ins->next->next->next || ins->next->next->next->opcode != OP_BR_REG) {
					/* A one-way switch which got optimized away */
					if (G_UNLIKELY (cfg->verbose_level > 1)) {
						printf ("\tNo cfold on ");
						mono_print_ins (ins);
					}
					return;
				}

				if (G_UNLIKELY (cfg->verbose_level > 1)) {
					printf ("\tcfold on ");
					mono_print_ins (ins);
				}

				/* Unlink target bblocks */
				for (i = 0; i < table->table_size; ++i) {
					if (table->table [i] != table->table [idx]) {
						remove_bb_from_phis (cfg, bb, table->table [i]);
						mono_unlink_bblock (cfg, bb, table->table [i]);
					}
				}

				/* Change the OP_BR_REG to a simple branch */
				ins->next->next->next->opcode = OP_BR;
				ins->next->next->next->inst_target_bb = table->table [idx];
				ins->next->next->next->sreg1 = -1;

				/* Nullify the other instructions */
				NULLIFY_INS (ins);
				NULLIFY_INS (ins->next);
				NULLIFY_INS (ins->next->next);
			}
		}
	}
	else if (MONO_IS_COND_BRANCH_OP (ins)) {
		if (ins->flags & MONO_INST_CFOLD_TAKEN) {
			remove_bb_from_phis (cfg, bb, ins->inst_false_bb);
			mono_unlink_bblock (cfg, bb, ins->inst_false_bb);
			ins->opcode = OP_BR;
			ins->inst_target_bb = ins->inst_true_bb;
		} else if (ins->flags & MONO_INST_CFOLD_NOT_TAKEN) {
			remove_bb_from_phis (cfg, bb, ins->inst_true_bb);
			mono_unlink_bblock (cfg, bb, ins->inst_true_bb);
			ins->opcode = OP_BR;
			ins->inst_target_bb = ins->inst_false_bb;
		}
	}
}

void
mono_ssa_cprop (MonoCompile *cfg)
{
	MonoInst **carray;
	MonoBasicBlock *bb;
	GList *bblock_list, *cvars;
	GList *tmp;
	int i;
	//printf ("SIMPLE OPTS BB%d %s\n", bb->block_num, mono_method_full_name (cfg->method, TRUE));

	carray = g_new0 (MonoInst*, cfg->next_vreg);

	if (!(cfg->comp_done & MONO_COMP_SSA_DEF_USE))
		mono_ssa_create_def_use (cfg);

	bblock_list = g_list_prepend (NULL, cfg->bb_entry);
	cfg->bb_entry->flags |= BB_REACHABLE;

	memset (carray, 0, sizeof (MonoInst *) * cfg->num_varinfo);

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoMethodVar *info = MONO_VARINFO (cfg, i);
		if (!info->def)
			info->cpstate = 2;
	}

	for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {
		/*
		 * FIXME: This should be bb->flags & BB_FLAG_EXCEPTION_HANDLER, but
		 * that would still allow unreachable try's to be removed.
		 */
		if (bb->region)
			add_cprop_bb (cfg, bb, &bblock_list);
	}

	cvars = NULL;

	while (bblock_list) {
		MonoInst *inst;

		bb = (MonoBasicBlock *)bblock_list->data;

		bblock_list = g_list_delete_link (bblock_list, bblock_list);

		g_assert (bb->flags &  BB_REACHABLE);

		/*
		 * Some bblocks are linked to 2 others even through they fall through to the
		 * next bblock.
		 */
		if (!(bb->last_ins && MONO_IS_BRANCH_OP (bb->last_ins))) {
			for (i = 0; i < bb->out_count; ++i)
				add_cprop_bb (cfg, bb->out_bb [i], &bblock_list);
		}

		if (cfg->verbose_level > 1)
			printf ("\nSSA CONSPROP BB%d:\n", bb->block_num);

		for (inst = bb->code; inst; inst = inst->next) {
			visit_inst (cfg, bb, inst, &cvars, &bblock_list, carray);
		}

		while (cvars) {
			MonoMethodVar *info = (MonoMethodVar *)cvars->data;
			cvars = g_list_delete_link (cvars, cvars);

			for (tmp = info->uses; tmp; tmp = tmp->next) {
				MonoVarUsageInfo *ui = (MonoVarUsageInfo *)tmp->data;
				if (!(ui->bb->flags & BB_REACHABLE))
					continue;
				visit_inst (cfg, ui->bb, ui->inst, &cvars, &bblock_list, carray);
			}
		}
	}

	for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {
		MonoInst *inst;
		for (inst = bb->code; inst; inst = inst->next) {
			fold_ins (cfg, bb, inst, carray);
		}
	}

	g_free (carray);

	cfg->comp_done |= MONO_COMP_REACHABILITY;

	/* fixme: we should update usage infos during cprop, instead of computing it again */
	cfg->comp_done &=  ~MONO_COMP_SSA_DEF_USE;
	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoMethodVar *info = MONO_VARINFO (cfg, i);
		info->def = NULL;
		info->uses = NULL;
	}
}

static void
add_to_dce_worklist (MonoCompile *cfg, MonoMethodVar *var, MonoMethodVar *use, GList **wl)
{
	GList *tmp;

	*wl = g_list_prepend_mempool (cfg->mempool, *wl, use);

	for (tmp = use->uses; tmp; tmp = tmp->next) {
		MonoVarUsageInfo *ui = (MonoVarUsageInfo *)tmp->data;
		if (ui->inst == var->def) {
			/* from the mempool */
			use->uses = g_list_remove_link (use->uses, tmp);
			break;
		}
	}
}

void
mono_ssa_deadce (MonoCompile *cfg)
{
	int i;
	GList *work_list;

	g_assert (cfg->comp_done & MONO_COMP_SSA);

	//printf ("DEADCE %s\n", mono_method_full_name (cfg->method, TRUE));

	if (!(cfg->comp_done & MONO_COMP_SSA_DEF_USE))
		mono_ssa_create_def_use (cfg);

	mono_ssa_copyprop (cfg);

	work_list = NULL;
	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoMethodVar *info = MONO_VARINFO (cfg, i);
		work_list = g_list_prepend_mempool (cfg->mempool, work_list, info);
	}

	while (work_list) {
		MonoMethodVar *info = (MonoMethodVar *)work_list->data;
		work_list = g_list_remove_link (work_list, work_list);

		/*
		 * The second part of the condition happens often when PHI nodes have their dreg
		 * as one of their arguments due to the fact that we use the original vars.
		 */
		if (info->def && (!info->uses || ((info->uses->next == NULL) && (((MonoVarUsageInfo*)info->uses->data)->inst == info->def)))) {
			MonoInst *def = info->def;

			/* Eliminating FMOVE could screw up the fp stack */
			if (MONO_IS_MOVE (def) && (!MONO_ARCH_USE_FPSTACK || (def->opcode != OP_FMOVE))) {
				MonoInst *src_var = get_vreg_to_inst (cfg, def->sreg1);
				if (src_var && !(src_var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)))
					add_to_dce_worklist (cfg, info, MONO_VARINFO (cfg, src_var->inst_c0), &work_list);
				NULLIFY_INS (def);
				info->reg = -1;
			} else if ((def->opcode == OP_ICONST) || (def->opcode == OP_I8CONST) || MONO_IS_ZERO (def)) {
				NULLIFY_INS (def);
				info->reg = -1;
			} else if (MONO_IS_PHI (def)) {
				int j;
				for (j = def->inst_phi_args [0]; j > 0; j--) {
					MonoMethodVar *u = MONO_VARINFO (cfg, get_vreg_to_inst (cfg, def->inst_phi_args [j])->inst_c0);
					add_to_dce_worklist (cfg, info, u, &work_list);
				}
				NULLIFY_INS (def);
				info->reg = -1;
			}
			else if (def->opcode == OP_NOP) {
			}
			//else
			//mono_print_ins (def);
		}

	}
}

#if 0
void
mono_ssa_strength_reduction (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	int i;

	g_assert (cfg->comp_done & MONO_COMP_SSA);
	g_assert (cfg->comp_done & MONO_COMP_LOOPS);
	g_assert (cfg->comp_done & MONO_COMP_SSA_DEF_USE);

	for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {
		GList *lp = bb->loop_blocks;

		if (lp) {
			MonoBasicBlock *h = (MonoBasicBlock *)lp->data;

			/* we only consider loops with 2 in bblocks */
			if (!h->in_count == 2)
				continue;

			for (i = 0; i < cfg->num_varinfo; i++) {
				MonoMethodVar *info = MONO_VARINFO (cfg, i);

				if (info->def && info->def->ssa_op == MONO_SSA_STORE &&
				    info->def->inst_i0->opcode == OP_LOCAL && g_list_find (lp, info->def_bb)) {
					MonoInst *v = info->def->inst_i1;


					printf ("FOUND %d in %s\n", info->idx, mono_method_full_name (cfg->method, TRUE));
				}
			}
		}
	}
}
#endif

void
mono_ssa_loop_invariant_code_motion (MonoCompile *cfg)
{
	MonoBasicBlock *bb, *h, *idom;
	MonoInst *ins, *n;

	g_assert (cfg->comp_done & MONO_COMP_SSA);
	if (!(cfg->comp_done & MONO_COMP_LOOPS) || !(cfg->comp_done & MONO_COMP_SSA_DEF_USE))
		return;

	for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {
		GList *lp = bb->loop_blocks;

		if (!lp)
			continue;
		h = (MonoBasicBlock *)lp->data;
		if (bb != h)
			continue;
		MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
			/*
			 * Try to move instructions out of loop headers into the preceeding bblock.
			 */
			if (ins->opcode == OP_LDLEN || ins->opcode == OP_STRLEN || ins->opcode == OP_CHECK_THIS || ins->opcode == OP_AOTCONST || ins->opcode == OP_GENERIC_CLASS_INIT) {
				MonoInst *tins;
				gboolean skip;
				int sreg;

				idom = h->idom;
				/*
				 * h->nesting is needed to work around:
				 * http://llvm.org/bugs/show_bug.cgi?id=17868
				 */
				if (!(idom && idom->last_ins && idom->last_ins->opcode == OP_BR && idom->last_ins->inst_target_bb == h && h->nesting == 1)) {
					continue;
				}

				/*
				 * Make sure there are no instructions with side effects before ins.
				 */
				skip = FALSE;
				MONO_BB_FOR_EACH_INS (bb, tins) {
					if (tins == ins)
						break;
					if (!MONO_INS_HAS_NO_SIDE_EFFECT (tins)) {
						skip = TRUE;
						break;
					}
				}
				if (skip) {
					/*
					  printf ("%s\n", mono_method_full_name (cfg->method, TRUE));
					  mono_print_ins (tins);
					*/
					continue;
				}

				/* Make sure we don't move the instruction before the def of its sreg */
				if (ins->opcode == OP_LDLEN || ins->opcode == OP_STRLEN || ins->opcode == OP_CHECK_THIS)
					sreg = ins->sreg1;
				else
					sreg = -1;
				if (sreg != -1) {
					MonoInst *var;

					skip = FALSE;
					for (tins = ins->prev; tins; tins = tins->prev) {
						const char *spec = INS_INFO (tins->opcode);

						if (tins->opcode == OP_MOVE && tins->dreg == sreg) {
							sreg = tins->sreg1;
						} if (spec [MONO_INST_DEST] != ' ' && tins->dreg == sreg) {
							skip = TRUE;
							break;
						}
					}
					if (skip)
						continue;
					var = get_vreg_to_inst (cfg, sreg);
					if (var && (var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)))
						continue;
					ins->sreg1 = sreg;
				}

				/* if any successor block of the immediate post dominator is an
				 * exception handler, it's not safe to do the code motion */
				skip = FALSE;
				for (int j = 0; j < idom->out_count && !skip; j++)
					skip |= !!(idom->out_bb [j]->flags & BB_EXCEPTION_HANDLER);
				if (skip)
					continue;

				if (cfg->verbose_level > 1) {
					printf ("licm in BB%d on ", bb->block_num);
					mono_print_ins (ins);
				}
				//{ static int count = 0; count ++; printf ("%d\n", count); }
				MONO_REMOVE_INS (bb, ins);
				mono_bblock_insert_before_ins (idom, idom->last_ins, ins);
				if (ins->opcode == OP_LDLEN || ins->opcode == OP_STRLEN)
					idom->needs_decompose = TRUE;
			}
		}
	}

	cfg->comp_done &=  ~MONO_COMP_SSA_DEF_USE;
	for (guint i = 0; i < cfg->num_varinfo; i++) {
		MonoMethodVar *info = MONO_VARINFO (cfg, i);
		info->def = NULL;
		info->uses = NULL;
	}
}

#else /* !DISABLE_JIT */

MONO_EMPTY_SOURCE_FILE (ssa);

#endif /* !DISABLE_JIT */
