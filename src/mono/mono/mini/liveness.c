/**
 * \file
 * liveness analysis
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_JIT

#include "mini.h"

#define SPILL_COST_INCREMENT (1 << (bb->nesting << 1))

#define DEBUG_LIVENESS

#define BITS_PER_CHUNK MONO_BITSET_BITS_PER_CHUNK

#define BB_ID_SHIFT 18

/* 
 * The liveness2 pass can't handle long vars on 32 bit platforms because the component
 * vars have the same 'idx'.
 */
#if SIZEOF_REGISTER == 8
#define ENABLE_LIVENESS2
#endif

#ifdef ENABLE_LIVENESS2
static void mono_analyze_liveness2 (MonoCompile *cfg);
#endif


#define INLINE_SIZE 16

typedef struct {
	int capacity;
	union {
		gpointer data [INLINE_SIZE];
		GHashTable *hashtable;
	};
} MonoPtrSet;

static void
mono_ptrset_init (MonoPtrSet *set)
{
	set->capacity = 0;
}

static void
mono_ptrset_destroy (MonoPtrSet *set)
{
	if (set->capacity > INLINE_SIZE)
		g_hash_table_destroy (set->hashtable);
}

static void
mono_ptrset_add (MonoPtrSet *set, gpointer val)
{
	//switch to hashtable
	if (set->capacity == INLINE_SIZE) {
		GHashTable *tmp = g_hash_table_new (NULL, NULL);
		for (int i = 0; i < INLINE_SIZE; ++i)
			g_hash_table_insert (tmp, set->data [i], set->data [i]);
		set->hashtable = tmp;
		++set->capacity;
	}

	if (set->capacity > INLINE_SIZE) {
		g_hash_table_insert (set->hashtable, val, val);
	} else {
		set->data [set->capacity] = val;
		++set->capacity;
	}
}

static gboolean
mono_ptrset_contains (MonoPtrSet *set, gpointer val)
{
	if (set->capacity <= INLINE_SIZE) {
		for (int i = 0; i < set->capacity; ++i) {
			if (set->data [i] == val)
				return TRUE;
		}
		return FALSE;
	}

	return g_hash_table_lookup (set->hashtable, val) != NULL;
}


static void
optimize_initlocals (MonoCompile *cfg);

/* mono_bitset_mp_new:
 * 
 * allocates a MonoBitSet inside a memory pool
 */
static MonoBitSet*
mono_bitset_mp_new (MonoMemPool *mp, guint32 size, guint32 max_size)
{
	guint8 *mem = (guint8 *)mono_mempool_alloc0 (mp, size);
	return mono_bitset_mem_new (mem, max_size, MONO_BITSET_DONT_FREE);
}

static MonoBitSet*
mono_bitset_mp_new_noinit (MonoMemPool *mp, guint32 size, guint32 max_size)
{
	guint8 *mem = (guint8 *)mono_mempool_alloc (mp, size);
	return mono_bitset_mem_new (mem, max_size, MONO_BITSET_DONT_FREE);
}

G_GNUC_UNUSED static void
mono_bitset_print (MonoBitSet *set)
{
	int i;
	gboolean first = TRUE;

	printf ("{");
	for (i = 0; i < mono_bitset_size (set); i++) {

		if (mono_bitset_test (set, i)) {
			if (!first)
				printf (", ");
			printf ("%d", i);
			first = FALSE;
		}
	}
	printf ("}\n");
}

static void
visit_bb (MonoCompile *cfg, MonoBasicBlock *bb, MonoPtrSet *visited)
{
	int i;
	MonoInst *ins;

	if (mono_ptrset_contains (visited, bb))
		return;

	for (ins = bb->code; ins; ins = ins->next) {
		const char *spec = INS_INFO (ins->opcode);
		int regtype, srcindex, sreg, num_sregs;
		int sregs [MONO_MAX_SRC_REGS];

		if (ins->opcode == OP_NOP)
			continue;

		/* DREG */
		regtype = spec [MONO_INST_DEST];
		g_assert (((ins->dreg == -1) && (regtype == ' ')) || ((ins->dreg != -1) && (regtype != ' ')));
				
		if ((ins->dreg != -1) && get_vreg_to_inst (cfg, ins->dreg)) {
			MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);
			int idx = var->inst_c0;
			MonoMethodVar *vi = MONO_VARINFO (cfg, idx);

			cfg->varinfo [vi->idx]->flags |= MONO_INST_VOLATILE;
			if (SIZEOF_REGISTER == 4 && (var->type == STACK_I8 || (var->type == STACK_R8 && COMPILE_SOFT_FLOAT (cfg)))) {
				/* Make the component vregs volatile as well (#612206) */
				get_vreg_to_inst (cfg, MONO_LVREG_LS (var->dreg))->flags |= MONO_INST_VOLATILE;
				get_vreg_to_inst (cfg, MONO_LVREG_MS (var->dreg))->flags |= MONO_INST_VOLATILE;
			}
		}
			
		/* SREGS */
		num_sregs = mono_inst_get_src_registers (ins, sregs);
		for (srcindex = 0; srcindex < num_sregs; ++srcindex) {
			sreg = sregs [srcindex];

			g_assert (sreg != -1);
			if (get_vreg_to_inst (cfg, sreg)) {
				MonoInst *var = get_vreg_to_inst (cfg, sreg);
				int idx = var->inst_c0;
				MonoMethodVar *vi = MONO_VARINFO (cfg, idx);

				cfg->varinfo [vi->idx]->flags |= MONO_INST_VOLATILE;
				if (SIZEOF_REGISTER == 4 && (var->type == STACK_I8 || (var->type == STACK_R8 && COMPILE_SOFT_FLOAT (cfg)))) {
					/* Make the component vregs volatile as well (#612206) */
					get_vreg_to_inst (cfg, MONO_LVREG_LS (var->dreg))->flags |= MONO_INST_VOLATILE;
					get_vreg_to_inst (cfg, MONO_LVREG_MS (var->dreg))->flags |= MONO_INST_VOLATILE;
				}
			}
		}
	}

	mono_ptrset_add (visited, bb);

	/* 
	 * Need to visit all bblocks reachable from this one since they can be
	 * reached during exception handling.
	 */
	for (i = 0; i < bb->out_count; ++i) {
		visit_bb (cfg, bb->out_bb [i], visited);
	}
}

void
mono_liveness_handle_exception_clauses (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	MonoMethodHeader *header = cfg->header;
	MonoExceptionClause *clause, *clause2;
	int i, j;
	gboolean *outer_try;

	/* 
	 * Determine which clauses are outer try clauses, i.e. they are not contained in any
	 * other non-try clause.
	 */
	outer_try = (gboolean *)mono_mempool_alloc0 (cfg->mempool, sizeof (gboolean) * header->num_clauses);
	for (i = 0; i < header->num_clauses; ++i)
		outer_try [i] = TRUE;
	/* Iterate over the clauses backward, so outer clauses come first */
	/* This avoids doing an O(2) search, since we can determine when inner clauses end */
	for (i = header->num_clauses - 1; i >= 0; --i) {
		clause = &header->clauses [i];

		if (clause->flags != 0) {
			outer_try [i] = TRUE;
			/* Iterate over inner clauses */
			for (j = i - 1; j >= 0; --j) {
				clause2 = &header->clauses [j];

				if (clause2->flags == 0 && MONO_OFFSET_IN_HANDLER (clause, clause2->try_offset)) {
					outer_try [j] = FALSE;
					break;
				}
				if (clause2->try_offset < clause->try_offset)
					/* End of inner clauses */
					break;
			}
		}
	}

	MonoPtrSet visited;
	mono_ptrset_init (&visited);
	/*
	 * Variables in exception handler register cannot be allocated to registers
	 * so make them volatile. See bug #42136. This will not be neccessary when
	 * the back ends could guarantee that the variables will be in the
	 * correct registers when a handler is called.
	 * This includes try blocks too, since a variable in a try block might be
	 * accessed after an exception handler has been run.
	 */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {

		if (bb->region == -1)
			continue;

		if (MONO_BBLOCK_IS_IN_REGION (bb, MONO_REGION_TRY) && outer_try [MONO_REGION_CLAUSE_INDEX (bb->region)])
			continue;

		if (cfg->verbose_level > 2)
			printf ("pessimize variables in bb %d.\n", bb->block_num);

		visit_bb (cfg, bb, &visited);
	}
	mono_ptrset_destroy (&visited);
}

static void
update_live_range (MonoMethodVar *var, int abs_pos)
{
	if (var->range.first_use.abs_pos > abs_pos)
		var->range.first_use.abs_pos = abs_pos;

	if (var->range.last_use.abs_pos < abs_pos)
		var->range.last_use.abs_pos = abs_pos;
}

static void
analyze_liveness_bb (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	int sreg, inst_num;
	MonoMethodVar *vars = cfg->vars;
	guint32 abs_pos = (bb->dfn << BB_ID_SHIFT);
	
	/* Start inst_num from > 0, so last_use.abs_pos is only 0 for dead variables */
	for (inst_num = 2, ins = bb->code; ins; ins = ins->next, inst_num += 2) {
		const char *spec = INS_INFO (ins->opcode);
		int num_sregs, i;
		int sregs [MONO_MAX_SRC_REGS];

#ifdef DEBUG_LIVENESS
		if (cfg->verbose_level > 1) {
			mono_print_ins_index (1, ins);
		}
#endif

		if (ins->opcode == OP_NOP)
			continue;

		if (ins->opcode == OP_LDADDR) {
			MonoInst *var = (MonoInst *)ins->inst_p0;
			int idx = var->inst_c0;
			MonoMethodVar *vi = MONO_VARINFO (cfg, idx);

#ifdef DEBUG_LIVENESS
			if (cfg->verbose_level > 1)
				printf ("\tGEN: R%d(%d)\n", var->dreg, idx);
#endif
			update_live_range (&vars [idx], abs_pos + inst_num); 
			if (!mono_bitset_test_fast (bb->kill_set, idx))
				mono_bitset_set_fast (bb->gen_set, idx);
			vi->spill_costs += SPILL_COST_INCREMENT;
		}				

		/* SREGs must come first, so MOVE r <- r is handled correctly */
		num_sregs = mono_inst_get_src_registers (ins, sregs);
		for (i = 0; i < num_sregs; ++i) {
			sreg = sregs [i];
			if ((spec [MONO_INST_SRC1 + i] != ' ') && get_vreg_to_inst (cfg, sreg)) {
				MonoInst *var = get_vreg_to_inst (cfg, sreg);
				int idx = var->inst_c0;
				MonoMethodVar *vi = MONO_VARINFO (cfg, idx);

#ifdef DEBUG_LIVENESS
				if (cfg->verbose_level > 1)
					printf ("\tGEN: R%d(%d)\n", sreg, idx);
#endif
				update_live_range (&vars [idx], abs_pos + inst_num); 
				if (!mono_bitset_test_fast (bb->kill_set, idx))
					mono_bitset_set_fast (bb->gen_set, idx);
				vi->spill_costs += SPILL_COST_INCREMENT;
			}
		}

		/* DREG */
		if ((spec [MONO_INST_DEST] != ' ') && get_vreg_to_inst (cfg, ins->dreg)) {
			MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);
			int idx = var->inst_c0;
			MonoMethodVar *vi = MONO_VARINFO (cfg, idx);

			if (MONO_IS_STORE_MEMBASE (ins)) {
				update_live_range (&vars [idx], abs_pos + inst_num); 
				if (!mono_bitset_test_fast (bb->kill_set, idx))
					mono_bitset_set_fast (bb->gen_set, idx);
				vi->spill_costs += SPILL_COST_INCREMENT;
			} else {
#ifdef DEBUG_LIVENESS
				if (cfg->verbose_level > 1)
					printf ("\tKILL: R%d(%d)\n", ins->dreg, idx);
#endif
				update_live_range (&vars [idx], abs_pos + inst_num + 1); 
				mono_bitset_set_fast (bb->kill_set, idx);
				vi->spill_costs += SPILL_COST_INCREMENT;
			}
		}
	}
}

/* generic liveness analysis code. CFG specific parts are 
 * in update_gen_kill_set()
 */
void
mono_analyze_liveness (MonoCompile *cfg)
{
	MonoBitSet *old_live_out_set;
	int i, j, max_vars = cfg->num_varinfo;
	int out_iter;
	gboolean *in_worklist;
	MonoBasicBlock **worklist;
	guint32 l_end;
	int bitsize;

#ifdef DEBUG_LIVENESS
	if (cfg->verbose_level > 1)
		printf ("\nLIVENESS:\n");
#endif

	g_assert (!(cfg->comp_done & MONO_COMP_LIVENESS));

	cfg->comp_done |= MONO_COMP_LIVENESS;
	
	if (max_vars == 0)
		return;

	bitsize = mono_bitset_alloc_size (max_vars, 0);

	for (i = 0; i < max_vars; i ++) {
		MONO_VARINFO (cfg, i)->range.first_use.abs_pos = ~ 0;
		MONO_VARINFO (cfg, i)->range.last_use .abs_pos =   0;
		MONO_VARINFO (cfg, i)->spill_costs = 0;
	}

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		bb->gen_set = mono_bitset_mp_new (cfg->mempool, bitsize, max_vars);
		bb->kill_set = mono_bitset_mp_new (cfg->mempool, bitsize, max_vars);

#ifdef DEBUG_LIVENESS
		if (cfg->verbose_level > 1) {
			printf ("BLOCK BB%d (", bb->block_num);
			for (j = 0; j < bb->out_count; j++) 
				printf ("BB%d, ", bb->out_bb [j]->block_num);
		
			printf ("):\n");
		}
#endif

		analyze_liveness_bb (cfg, bb);

#ifdef DEBUG_LIVENESS
		if (cfg->verbose_level > 1) {
			printf ("GEN  BB%d: ", bb->block_num); mono_bitset_print (bb->gen_set);
			printf ("KILL BB%d: ", bb->block_num); mono_bitset_print (bb->kill_set);
		}
#endif
	}

	old_live_out_set = mono_bitset_new (max_vars, 0);
	in_worklist = g_new0 (gboolean, cfg->num_bblocks + 1);

	worklist = g_new (MonoBasicBlock *, cfg->num_bblocks + 1);
	l_end = 0;

	/*
	 * This is a backward dataflow analysis problem, so we process blocks in
	 * decreasing dfn order, this speeds up the iteration.
	 */
	for (i = 0; i < cfg->num_bblocks; i ++) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		worklist [l_end ++] = bb;
		in_worklist [bb->dfn] = TRUE;

		/* Initialized later */
		bb->live_in_set = NULL;
		bb->live_out_set = mono_bitset_mp_new (cfg->mempool, bitsize, max_vars);
	}

	out_iter = 0;

	if (cfg->verbose_level > 1)
		printf ("\nITERATION:\n");

	while (l_end != 0) {
		MonoBasicBlock *bb = worklist [--l_end];
		MonoBasicBlock *out_bb;
		gboolean changed;

		in_worklist [bb->dfn] = FALSE;

#ifdef DEBUG_LIVENESS
		if (cfg->verbose_level > 1) {
			printf ("P: BB%d(%d): IN: ", bb->block_num, bb->dfn);
			for (j = 0; j < bb->in_count; ++j) 
				printf ("BB%d ", bb->in_bb [j]->block_num);
			printf ("OUT:");
			for (j = 0; j < bb->out_count; ++j) 
				printf ("BB%d ", bb->out_bb [j]->block_num);
			printf ("\n");
		}
#endif


		if (bb->out_count == 0)
			continue;

		out_iter ++;

		if (!bb->live_in_set) {
			/* First pass over this bblock */
			changed = TRUE;
		}
		else {
			changed = FALSE;
			mono_bitset_copyto_fast (bb->live_out_set, old_live_out_set);
		}
 
		for (j = 0; j < bb->out_count; j++) {
			out_bb = bb->out_bb [j];

			if (!out_bb->live_in_set) {
				out_bb->live_in_set = mono_bitset_mp_new_noinit (cfg->mempool, bitsize, max_vars);

				mono_bitset_copyto_fast (out_bb->live_out_set, out_bb->live_in_set);
				mono_bitset_sub_fast (out_bb->live_in_set, out_bb->kill_set);
				mono_bitset_union_fast (out_bb->live_in_set, out_bb->gen_set);
			}

			// FIXME: Do this somewhere else
			if (bb->last_ins && bb->last_ins->opcode == OP_NOT_REACHED) {
			} else {
				mono_bitset_union_fast (bb->live_out_set, out_bb->live_in_set);
			}
		}
				
		if (changed || !mono_bitset_equal (old_live_out_set, bb->live_out_set)) {
			if (!bb->live_in_set)
				bb->live_in_set = mono_bitset_mp_new_noinit (cfg->mempool, bitsize, max_vars);
			mono_bitset_copyto_fast (bb->live_out_set, bb->live_in_set);
			mono_bitset_sub_fast (bb->live_in_set, bb->kill_set);
			mono_bitset_union_fast (bb->live_in_set, bb->gen_set);

			for (j = 0; j < bb->in_count; j++) {
				MonoBasicBlock *in_bb = bb->in_bb [j];
				/* 
				 * Some basic blocks do not seem to be in the 
				 * cfg->bblocks array...
				 */
				if (in_bb->gen_set && !in_worklist [in_bb->dfn]) {
#ifdef DEBUG_LIVENESS
					if (cfg->verbose_level > 1)
						printf ("\tADD: %d\n", in_bb->block_num);
#endif
					/*
					 * Put the block at the top of the stack, so it
					 * will be processed right away.
					 */
					worklist [l_end ++] = in_bb;
					in_worklist [in_bb->dfn] = TRUE;
				}
			}
		}

		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("\tLIVE IN  BB%d: ", bb->block_num); 
			mono_bitset_print (bb->live_in_set); 
		}			
	}

#ifdef DEBUG_LIVENESS
	if (cfg->verbose_level > 1)
		printf ("IT: %d %d.\n", cfg->num_bblocks, out_iter);
#endif

	mono_bitset_free (old_live_out_set);

	g_free (worklist);
	g_free (in_worklist);

	/* Compute live_in_set for bblocks skipped earlier */
	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		if (!bb->live_in_set) {
			bb->live_in_set = mono_bitset_mp_new (cfg->mempool, bitsize, max_vars);

			mono_bitset_copyto_fast (bb->live_out_set, bb->live_in_set);
			mono_bitset_sub_fast (bb->live_in_set, bb->kill_set);
			mono_bitset_union_fast (bb->live_in_set, bb->gen_set);
		}
	}

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];
		guint32 max;
		guint32 abs_pos = (bb->dfn << BB_ID_SHIFT);
		MonoMethodVar *vars = cfg->vars;

		if (!bb->live_out_set)
			continue;

		max = ((max_vars + (BITS_PER_CHUNK -1)) / BITS_PER_CHUNK);
		for (j = 0; j < max; ++j) {
			gsize bits_in;
			gsize bits_out;
			int k;

			bits_in = mono_bitset_get_fast (bb->live_in_set, j);
			bits_out = mono_bitset_get_fast (bb->live_out_set, j);

			k = (j * BITS_PER_CHUNK);
			while ((bits_in || bits_out)) {
				if (bits_in & 1)
					update_live_range (&vars [k], abs_pos + 0);
				if (bits_out & 1)
					update_live_range (&vars [k], abs_pos + ((1 << BB_ID_SHIFT) - 1));
				bits_in >>= 1;
				bits_out >>= 1;
				k ++;
			}
		}
	}

	/*
	 * Arguments need to have their live ranges extended to the beginning of
	 * the method to account for the arg reg/memory -> global register copies
	 * in the prolog (bug #74992).
	 */

	for (i = 0; i < max_vars; i ++) {
		MonoMethodVar *vi = MONO_VARINFO (cfg, i);
		if (cfg->varinfo [vi->idx]->opcode == OP_ARG) {
			if (vi->range.last_use.abs_pos == 0 && !(cfg->varinfo [vi->idx]->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
				/* 
				 * Can't eliminate the this variable in gshared code, since
				 * it is needed during exception handling to identify the
				 * method.
				 * It is better to check for this here instead of marking the variable
				 * VOLATILE, since that would prevent it from being allocated to
				 * registers.
				 */
				 if (!cfg->disable_deadce_vars && !(cfg->gshared && mono_method_signature_internal (cfg->method)->hasthis && cfg->varinfo [vi->idx] == cfg->args [0]))
					 cfg->varinfo [vi->idx]->flags |= MONO_INST_IS_DEAD;
			}
			vi->range.first_use.abs_pos = 0;
		}
	}

#ifdef DEBUG_LIVENESS
	if (cfg->verbose_level > 1) {
		for (i = cfg->num_bblocks - 1; i >= 0; i--) {
			MonoBasicBlock *bb = cfg->bblocks [i];
		
			printf ("LIVE IN  BB%d: ", bb->block_num); 
			mono_bitset_print (bb->live_in_set); 
			printf ("LIVE OUT BB%d: ", bb->block_num); 
			mono_bitset_print (bb->live_out_set); 
		}

		for (i = 0; i < max_vars; i ++) {
			MonoMethodVar *vi = MONO_VARINFO (cfg, i);

			printf ("V%d: [0x%x - 0x%x]\n", i, vi->range.first_use.abs_pos, vi->range.last_use.abs_pos);
		}
	}
#endif

	if (!cfg->disable_initlocals_opt)
		optimize_initlocals (cfg);

#ifdef ENABLE_LIVENESS2
	/* This improves code size by about 5% but slows down compilation too much */
	if (cfg->compile_aot)
		mono_analyze_liveness2 (cfg);
#endif
}

/**
 * optimize_initlocals:
 *
 * Try to optimize away some of the redundant initialization code inserted because of
 * 'locals init' using the liveness information.
 */
static void
optimize_initlocals (MonoCompile *cfg)
{
	MonoBitSet *used;
	MonoInst *ins;
	MonoBasicBlock *initlocals_bb;

	used = mono_bitset_new (cfg->next_vreg + 1, 0);

	mono_bitset_clear_all (used);
	initlocals_bb = cfg->bb_entry->next_bb;
	for (ins = initlocals_bb->code; ins; ins = ins->next) {
		int num_sregs, i;
		int sregs [MONO_MAX_SRC_REGS];

		num_sregs = mono_inst_get_src_registers (ins, sregs);
		for (i = 0; i < num_sregs; ++i)
			mono_bitset_set_fast (used, sregs [i]);

		if (MONO_IS_STORE_MEMBASE (ins))
			mono_bitset_set_fast (used, ins->dreg);
	}

	for (ins = initlocals_bb->code; ins; ins = ins->next) {
		const char *spec = INS_INFO (ins->opcode);

		/* Look for statements whose dest is not used in this bblock and not live on exit. */
		if ((spec [MONO_INST_DEST] != ' ') && !MONO_IS_STORE_MEMBASE (ins)) {
			MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);

			if (var && !mono_bitset_test_fast (used, ins->dreg) && !mono_bitset_test_fast (initlocals_bb->live_out_set, var->inst_c0) && (var != cfg->ret) && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
				//printf ("DEAD: "); mono_print_ins (ins);
				if (cfg->disable_initlocals_opt_refs && var->type == STACK_OBJ)
					continue;
				if ((ins->opcode == OP_ICONST) || (ins->opcode == OP_I8CONST) || (ins->opcode == OP_R8CONST) || (ins->opcode == OP_R4CONST)) {
					NULLIFY_INS (ins);
					MONO_VARINFO (cfg, var->inst_c0)->spill_costs -= 1;
					/* 
					 * We should shorten the liveness interval of these vars as well, but
					 * don't have enough info to do that.
					 */
				}
			}
		}
	}

	g_free (used);
}

void
mono_linterval_add_range (MonoCompile *cfg, MonoLiveInterval *interval, int from, int to)
{
	MonoLiveRange2 *prev_range, *next_range, *new_range;

	g_assert (to >= from);

	/* Optimize for extending the first interval backwards */
	if (G_LIKELY (interval->range && (interval->range->from > from) && (interval->range->from == to))) {
		interval->range->from = from;
		return;
	}

	/* Find a place in the list for the new range */
	prev_range = NULL;
	next_range = interval->range;
	while ((next_range != NULL) && (next_range->from <= from)) {
		prev_range = next_range;
		next_range = next_range->next;
	}

	if (prev_range && prev_range->to == from) {
		/* Merge with previous */
		prev_range->to = to;
	} else if (next_range && next_range->from == to) {
		/* Merge with previous */
		next_range->from = from;
	} else {
		/* Insert it */
		new_range = (MonoLiveRange2 *)mono_mempool_alloc (cfg->mempool, sizeof (MonoLiveRange2));
		new_range->from = from;
		new_range->to = to;
		new_range->next = NULL;

		if (prev_range)
			prev_range->next = new_range;
		else
			interval->range = new_range;
		if (next_range)
			new_range->next = next_range;
		else
			interval->last_range = new_range;
	}

	/* FIXME: Merge intersecting ranges */
}

void
mono_linterval_print (MonoLiveInterval *interval)
{
	MonoLiveRange2 *range;

	for (range = interval->range; range != NULL; range = range->next)
		printf ("[%x-%x] ", range->from, range->to);
}

void
mono_linterval_print_nl (MonoLiveInterval *interval)
{
	mono_linterval_print (interval);
	printf ("\n");
}

/**
 * mono_linterval_convers:
 *
 *   Return whenever INTERVAL covers the position POS.
 */
gboolean
mono_linterval_covers (MonoLiveInterval *interval, int pos)
{
	MonoLiveRange2 *range;

	for (range = interval->range; range != NULL; range = range->next) {
		if (pos >= range->from && pos <= range->to)
			return TRUE;
		if (range->from > pos)
			return FALSE;
	}

	return FALSE;
}

/**
 * mono_linterval_get_intersect_pos:
 *
 *   Determine whenever I1 and I2 intersect, and if they do, return the first
 * point of intersection. Otherwise, return -1.
 */
gint32
mono_linterval_get_intersect_pos (MonoLiveInterval *i1, MonoLiveInterval *i2)
{
	MonoLiveRange2 *r1, *r2;

	/* FIXME: Optimize this */
	for (r1 = i1->range; r1 != NULL; r1 = r1->next) {
		for (r2 = i2->range; r2 != NULL; r2 = r2->next) {
			if (r2->to > r1->from && r2->from < r1->to) {
				if (r2->from <= r1->from)
					return r1->from;
				else
					return r2->from;
			}
		}
	}

	return -1;
}
 
/**
 * mono_linterval_split
 *
 *   Split L at POS and store the newly created intervals into L1 and L2. POS becomes
 * part of L2.
 */
void
mono_linterval_split (MonoCompile *cfg, MonoLiveInterval *interval, MonoLiveInterval **i1, MonoLiveInterval **i2, int pos)
{
	MonoLiveRange2 *r;

	g_assert (pos > interval->range->from && pos <= interval->last_range->to);

	*i1 = (MonoLiveInterval *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoLiveInterval));
	*i2 = (MonoLiveInterval *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoLiveInterval));

	for (r = interval->range; r; r = r->next) {
		if (pos > r->to) {
			/* Add it to the first child */
			mono_linterval_add_range (cfg, *i1, r->from, r->to);
		} else if (pos > r->from && pos <= r->to) {
			/* Split at pos */
			mono_linterval_add_range (cfg, *i1, r->from, pos - 1);
			mono_linterval_add_range (cfg, *i2, pos, r->to);
		} else {
			/* Add it to the second child */
			mono_linterval_add_range (cfg, *i2, r->from, r->to);
		}
	}
}

#if 1
#define LIVENESS_DEBUG(a) do { if (cfg->verbose_level > 1) do { a; } while (0); } while (0)
#define ENABLE_LIVENESS_DEBUG 1
#else
#define LIVENESS_DEBUG(a)
#endif

#ifdef ENABLE_LIVENESS2

static void
update_liveness2 (MonoCompile *cfg, MonoInst *ins, gboolean set_volatile, int inst_num, gint32 *last_use)
{
	const char *spec = INS_INFO (ins->opcode);
	int sreg;
	int num_sregs, i;
	int sregs [MONO_MAX_SRC_REGS];

	LIVENESS_DEBUG (printf ("\t%x: ", inst_num); mono_print_ins (ins));

	if (ins->opcode == OP_NOP || ins->opcode == OP_IL_SEQ_POINT)
		return;

	/* DREG */
	if ((spec [MONO_INST_DEST] != ' ') && get_vreg_to_inst (cfg, ins->dreg)) {
		MonoInst *var = get_vreg_to_inst (cfg, ins->dreg);
		int idx = var->inst_c0;
		MonoMethodVar *vi = MONO_VARINFO (cfg, idx);

		if (MONO_IS_STORE_MEMBASE (ins)) {
			if (last_use [idx] == 0) {
				LIVENESS_DEBUG (printf ("\tlast use of R%d set to %x\n", ins->dreg, inst_num));
				last_use [idx] = inst_num;
			}
		} else {
			if (last_use [idx] > 0) {
				LIVENESS_DEBUG (printf ("\tadd range to R%d: [%x, %x)\n", ins->dreg, inst_num, last_use [idx]));
				mono_linterval_add_range (cfg, vi->interval, inst_num, last_use [idx]);
				last_use [idx] = 0;
			}
			else {
				/* Try dead code elimination */
				if (!cfg->disable_deadce_vars && (var != cfg->ret) && !(var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)) && ((ins->opcode == OP_ICONST) || (ins->opcode == OP_I8CONST) || (ins->opcode == OP_R8CONST)) && !(var->flags & MONO_INST_VOLATILE)) {
					LIVENESS_DEBUG (printf ("\tdead def of R%d, eliminated\n", ins->dreg));
					NULLIFY_INS (ins);
					return;
				} else {
					int inst_num_add = 1;
					MonoInst *next = ins->next;
					while (next && next->opcode == OP_IL_SEQ_POINT) {
						inst_num_add++;
						next = next->next;
					}

					LIVENESS_DEBUG (printf ("\tdead def of R%d, add range to R%d: [%x, %x]\n", ins->dreg, ins->dreg, inst_num, inst_num + inst_num_add));
					mono_linterval_add_range (cfg, vi->interval, inst_num, inst_num + inst_num_add);
				}
			}
		}
	}

	/* SREGs */
	num_sregs = mono_inst_get_src_registers (ins, sregs);
	for (i = 0; i < num_sregs; ++i) {
		sreg = sregs [i];
		if ((spec [MONO_INST_SRC1 + i] != ' ') && get_vreg_to_inst (cfg, sreg)) {
			MonoInst *var = get_vreg_to_inst (cfg, sreg);
			int idx = var->inst_c0;

			if (last_use [idx] == 0) {
				LIVENESS_DEBUG (printf ("\tlast use of R%d set to %x\n", sreg, inst_num));
				last_use [idx] = inst_num;
			}
		}
	}
}

static void
mono_analyze_liveness2 (MonoCompile *cfg)
{
	int bnum, idx, i, j, nins, max, max_vars, block_from, block_to, pos;
	gint32 *last_use;
	static guint32 disabled = -1;

	if (disabled == -1)
		disabled = g_hasenv ("DISABLED");

	if (disabled)
		return;

	if (cfg->num_bblocks >= (1 << (32 - BB_ID_SHIFT - 1)))
		/* Ranges would overflow */
		return;

	for (bnum = cfg->num_bblocks - 1; bnum >= 0; --bnum) {
		MonoBasicBlock *bb = cfg->bblocks [bnum];
		MonoInst *ins;

		nins = 0;
		for (nins = 0, ins = bb->code; ins; ins = ins->next, ++nins)
			nins ++;

		if (nins >= ((1 << BB_ID_SHIFT) - 1))
			/* Ranges would overflow */
			return;
	}

	LIVENESS_DEBUG (printf ("LIVENESS 2 %s\n", mono_method_full_name (cfg->method, TRUE)));

	/*
	if (strstr (cfg->method->name, "test_") != cfg->method->name)
		return;
	*/

	max_vars = cfg->num_varinfo;
	last_use = g_new0 (gint32, max_vars);

	for (idx = 0; idx < max_vars; ++idx) {
		MonoMethodVar *vi = MONO_VARINFO (cfg, idx);

		vi->interval = (MonoLiveInterval *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoLiveInterval));
	}

	/*
	 * Process bblocks in reverse order, so the addition of new live ranges
	 * to the intervals is faster.
	 */
	for (bnum = cfg->num_bblocks - 1; bnum >= 0; --bnum) {
		MonoBasicBlock *bb = cfg->bblocks [bnum];
		MonoInst *ins;

		block_from = (bb->dfn << BB_ID_SHIFT) + 1; /* so pos > 0 */
		if (bnum < cfg->num_bblocks - 1)
			/* Beginning of the next bblock */
			block_to = (cfg->bblocks [bnum + 1]->dfn << BB_ID_SHIFT) + 1;
		else
			block_to = (bb->dfn << BB_ID_SHIFT) + ((1 << BB_ID_SHIFT) - 1);

		LIVENESS_DEBUG (printf ("LIVENESS BLOCK BB%d:\n", bb->block_num));

		memset (last_use, 0, max_vars * sizeof (gint32));
		
		/* For variables in bb->live_out, set last_use to block_to */

		max = ((max_vars + (BITS_PER_CHUNK -1)) / BITS_PER_CHUNK);
		for (j = 0; j < max; ++j) {
			gsize bits_out;
			int k;

			bits_out = mono_bitset_get_fast (bb->live_out_set, j);
			k = (j * BITS_PER_CHUNK);	
			while (bits_out) {
				if (bits_out & 1) {
					LIVENESS_DEBUG (printf ("Var R%d live at exit, set last_use to %x\n", cfg->varinfo [k]->dreg, block_to));
					last_use [k] = block_to;
				}
				bits_out >>= 1;
				k ++;
			}
		}

		if (cfg->ret)
			last_use [cfg->ret->inst_c0] = block_to;

		pos = block_from + 1;
		MONO_BB_FOR_EACH_INS (bb, ins) pos++;

		/* Process instructions backwards */
		MONO_BB_FOR_EACH_INS_REVERSE (bb, ins) {
			update_liveness2 (cfg, ins, FALSE, pos, last_use);
			pos--;
		}

		for (idx = 0; idx < max_vars; ++idx) {
			MonoMethodVar *vi = MONO_VARINFO (cfg, idx);

			if (last_use [idx] != 0) {
				/* Live at exit, not written -> live on enter */
				LIVENESS_DEBUG (printf ("Var R%d live at enter, add range to R%d: [%x, %x)\n", cfg->varinfo [idx]->dreg, cfg->varinfo [idx]->dreg, block_from, last_use [idx]));
				mono_linterval_add_range (cfg, vi->interval, block_from, last_use [idx]);
			}
		}
	}

	/*
	 * Arguments need to have their live ranges extended to the beginning of
	 * the method to account for the arg reg/memory -> global register copies
	 * in the prolog (bug #74992).
	 */
	for (i = 0; i < max_vars; i ++) {
		MonoMethodVar *vi = MONO_VARINFO (cfg, i);
		if (cfg->varinfo [vi->idx]->opcode == OP_ARG)
			mono_linterval_add_range (cfg, vi->interval, 0, 1);
	}

#if 0
	for (idx = 0; idx < max_vars; ++idx) {
		MonoMethodVar *vi = MONO_VARINFO (cfg, idx);
		
		LIVENESS_DEBUG (printf ("LIVENESS R%d: ", cfg->varinfo [idx]->dreg));
		LIVENESS_DEBUG (mono_linterval_print (vi->interval));
		LIVENESS_DEBUG (printf ("\n"));
	}
#endif

	g_free (last_use);
}

#endif

static void
update_liveness_gc (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, gint32 *last_use, MonoMethodVar **vreg_to_varinfo, GSList **callsites)
{
	if (ins->opcode == OP_GC_LIVENESS_DEF || ins->opcode == OP_GC_LIVENESS_USE) {
		int vreg = ins->inst_c1;
		MonoMethodVar *vi = vreg_to_varinfo [vreg];
		int idx = vi->idx;
		int pc_offset = ins->backend.pc_offset;

		LIVENESS_DEBUG (printf ("\t%x: ", pc_offset); mono_print_ins (ins));

		if (ins->opcode == OP_GC_LIVENESS_DEF) {
			if (last_use [idx] > 0) {
				LIVENESS_DEBUG (printf ("\tadd range to R%d: [%x, %x)\n", vreg, pc_offset, last_use [idx]));
				last_use [idx] = 0;
			}
		} else {
			if (last_use [idx] == 0) {
				LIVENESS_DEBUG (printf ("\tlast use of R%d set to %x\n", vreg, pc_offset));
				last_use [idx] = pc_offset;
			}
		}
	} else if (ins->opcode == OP_GC_PARAM_SLOT_LIVENESS_DEF) {
		GCCallSite *last;

		/* Add it to the last callsite */
		g_assert (*callsites);
		last = (GCCallSite *)(*callsites)->data;
		last->param_slots = g_slist_prepend_mempool (cfg->mempool, last->param_slots, ins);
	} else if (ins->flags & MONO_INST_GC_CALLSITE) {
		GCCallSite *callsite = (GCCallSite *)mono_mempool_alloc0 (cfg->mempool, sizeof (GCCallSite));
		int i;

		LIVENESS_DEBUG (printf ("\t%x: ", ins->backend.pc_offset); mono_print_ins (ins));
		LIVENESS_DEBUG (printf ("\t\tlive: "));

		callsite->bb = bb;
		callsite->liveness = (guint8 *)mono_mempool_alloc0 (cfg->mempool, ALIGN_TO (cfg->num_varinfo, 8) / 8);
		callsite->pc_offset = ins->backend.pc_offset;
		for (i = 0; i < cfg->num_varinfo; ++i) {
			if (last_use [i] != 0) {
				LIVENESS_DEBUG (printf ("R%d", MONO_VARINFO (cfg, i)->vreg));
				callsite->liveness [i / 8] |= (1 << (i % 8));
			}
		}
		LIVENESS_DEBUG (printf ("\n"));
		*callsites = g_slist_prepend_mempool (cfg->mempool, *callsites, callsite);
	}
}

static int
get_vreg_from_var (MonoCompile *cfg, MonoInst *var)
{
	if (var->opcode == OP_REGVAR)
		/* dreg contains a hreg, but inst_c0 still contains the var index */
		return MONO_VARINFO (cfg, var->inst_c0)->vreg;
	else
		/* dreg still contains the vreg */
		return var->dreg;
}

/*
 * mono_analyze_liveness_gc:
 *
 *   Compute liveness bitmaps for each call site.
 * This function is a modified version of mono_analyze_liveness2 ().
 */
void
mono_analyze_liveness_gc (MonoCompile *cfg)
{
	int idx, i, j, nins, max, max_vars, block_from, block_to, pos, reverse_len;
	gint32 *last_use;
	MonoInst **reverse;
	MonoMethodVar **vreg_to_varinfo = NULL;
	MonoBasicBlock *bb;
	GSList *callsites;

	LIVENESS_DEBUG (printf ("\n------------ GC LIVENESS: ----------\n"));

	max_vars = cfg->num_varinfo;
	last_use = g_new0 (gint32, max_vars);

	/*
	 * var->inst_c0 no longer contains the variable index, so compute a mapping now.
	 */
	vreg_to_varinfo = g_new0 (MonoMethodVar*, cfg->next_vreg);
	for (idx = 0; idx < max_vars; ++idx) {
		MonoMethodVar *vi = MONO_VARINFO (cfg, idx);

		vreg_to_varinfo [vi->vreg] = vi;
	}

	reverse_len = 1024;
	reverse = (MonoInst **)mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * reverse_len);

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;

		block_from = bb->real_native_offset;
		block_to = bb->native_offset + bb->native_length;

		LIVENESS_DEBUG (printf ("GC LIVENESS BB%d:\n", bb->block_num));

		if (!bb->code)
			continue;

		memset (last_use, 0, max_vars * sizeof (gint32));
		
		/* For variables in bb->live_out, set last_use to block_to */

		max = ((max_vars + (BITS_PER_CHUNK -1)) / BITS_PER_CHUNK);
		for (j = 0; j < max; ++j) {
			gsize bits_out;
			int k;

			if (!bb->live_out_set)
				/* The variables used in this bblock are volatile anyway */
				continue;

			bits_out = mono_bitset_get_fast (bb->live_out_set, j);
			k = (j * BITS_PER_CHUNK);	
			while (bits_out) {
				if ((bits_out & 1) && cfg->varinfo [k]->flags & MONO_INST_GC_TRACK) {
					int vreg = get_vreg_from_var (cfg, cfg->varinfo [k]);
					LIVENESS_DEBUG (printf ("Var R%d live at exit, last_use set to %x.\n", vreg, block_to));
					last_use [k] = block_to;
				}
				bits_out >>= 1;
				k ++;
			}
		}

		for (nins = 0, pos = block_from, ins = bb->code; ins; ins = ins->next, ++nins, ++pos) {
			if (nins >= reverse_len) {
				int new_reverse_len = reverse_len * 2;
				MonoInst **new_reverse = (MonoInst **)mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * new_reverse_len);
				memcpy (new_reverse, reverse, sizeof (MonoInst*) * reverse_len);
				reverse = new_reverse;
				reverse_len = new_reverse_len;
			}

			reverse [nins] = ins;
		}

		/* Process instructions backwards */
		callsites = NULL;
		for (i = nins - 1; i >= 0; --i) {
			MonoInst *ins = (MonoInst*)reverse [i];

			update_liveness_gc (cfg, bb, ins, last_use, vreg_to_varinfo, &callsites);
		}
		/* The callsites should already be sorted by pc offset because we added them backwards */
		bb->gc_callsites = callsites;
	}

	g_free (last_use);
	g_free (vreg_to_varinfo);
}

#else /* !DISABLE_JIT */

MONO_EMPTY_SOURCE_FILE (liveness);

#endif /* !DISABLE_JIT */
