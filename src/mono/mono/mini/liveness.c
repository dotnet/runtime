/*
 * liveness.c: liveness analysis
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include "mini.h"
#include "inssel.h"

//#define DEBUG_LIVENESS


extern guint8 mono_burg_arity [];

/* mono_bitset_mp_new:
 * 
 * allocates a MonoBitSet inside a memory pool
 */
static MonoBitSet* 
mono_bitset_mp_new (MonoMemPool *mp,  guint32 max_size)
{
	int size = mono_bitset_alloc_size (max_size, 0);
	gpointer mem;

	mem = mono_mempool_alloc0 (mp, size);
	return mono_bitset_mem_new (mem, max_size, MONO_BITSET_DONT_FREE);
}

#ifdef DEBUG_LIVENESS
static void
mono_bitset_print (MonoBitSet *set)
{
	int i;

	printf ("{");
	for (i = 0; i < mono_bitset_size (set); i++) {

		if (mono_bitset_test (set, i))
			printf ("%d, ", i);

	}
	printf ("}");
}
#endif

static void
update_live_range (MonoCompile *cfg, int idx, int block_dfn, int tree_pos)
{
	MonoLiveRange *range = &MONO_VARINFO (cfg, idx)->range;
	guint32 abs_pos = (block_dfn << 16) | tree_pos;

	if (range->first_use.abs_pos > abs_pos)
		range->first_use.abs_pos = abs_pos;

	if (range->last_use.abs_pos < abs_pos)
		range->last_use.abs_pos = abs_pos;
}

static void
update_gen_kill_set (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *inst, int inst_num)
{
	int arity = mono_burg_arity [inst->opcode];
	int max_vars = cfg->num_varinfo;

	if (arity)
		update_gen_kill_set (cfg, bb, inst->inst_i0, inst_num);

	if (arity > 1)
		update_gen_kill_set (cfg, bb, inst->inst_i1, inst_num);

	if (inst->ssa_op == MONO_SSA_LOAD) {
		int idx = inst->inst_i0->inst_c0;
		MonoMethodVar *vi = MONO_VARINFO (cfg, idx);
		g_assert (idx < max_vars);
		update_live_range (cfg, idx, bb->dfn, inst_num); 
		if (!mono_bitset_test (bb->kill_set, idx))
			mono_bitset_set (bb->gen_set, idx);
		vi->spill_costs += 1 + (bb->nesting * 2);
	} else if (inst->ssa_op == MONO_SSA_STORE) {
		int idx = inst->inst_i0->inst_c0;
		MonoMethodVar *vi = MONO_VARINFO (cfg, idx);
		g_assert (idx < max_vars);
		g_assert (inst->inst_i1->opcode != OP_PHI);

		update_live_range (cfg, idx, bb->dfn, inst_num); 
		mono_bitset_set (bb->kill_set, idx);
		vi->spill_costs += 1 + (bb->nesting * 2);
	}
} 

/* generic liveness analysis code. CFG specific parts are 
 * in update_gen_kill_set()
 */
void
mono_analyze_liveness (MonoCompile *cfg)
{
	MonoBitSet *old_live_in_set, *old_live_out_set;
	gboolean changes;
	int i, j, max_vars = cfg->num_varinfo;

#ifdef DEBUG_LIVENESS
	printf ("LIVENLESS %s\n", mono_method_full_name (cfg->method, TRUE));
#endif

	g_assert (!(cfg->comp_done & MONO_COMP_LIVENESS));

	cfg->comp_done |= MONO_COMP_LIVENESS;
	
	if (max_vars == 0)
		return;

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		bb->gen_set = mono_bitset_mp_new (cfg->mempool, max_vars);
		bb->kill_set = mono_bitset_mp_new (cfg->mempool, max_vars);
		bb->live_in_set = mono_bitset_mp_new (cfg->mempool, max_vars);
		bb->live_out_set = mono_bitset_mp_new (cfg->mempool, max_vars);
	}

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];
		MonoInst *inst;
		int tree_num;

		for (tree_num = 0, inst = bb->code; inst; inst = inst->next, tree_num++) {
			//mono_print_tree (inst); printf ("\n");
			update_gen_kill_set (cfg, bb, inst, tree_num);
		}

#ifdef DEBUG_LIVENESS
		printf ("BLOCK BB%d (", bb->block_num);
		for (j = 0; j < bb->out_count; j++) 
			printf ("BB%d, ", bb->out_bb [j]->block_num);
		
		printf (")\n");
		printf ("GEN  BB%d: ", bb->block_num); mono_bitset_print (bb->gen_set); printf ("\n");
		printf ("KILL BB%d: ", bb->block_num); mono_bitset_print (bb->kill_set); printf ("\n");
#endif
	}

	old_live_in_set = mono_bitset_new (max_vars, 0);
	old_live_out_set = mono_bitset_new (max_vars, 0);
 
	do {
		changes = FALSE;

		for (i = cfg->num_bblocks - 1; i >= 0; i--) {
			MonoBasicBlock *bb = cfg->bblocks [i];

			mono_bitset_copyto (bb->live_in_set, old_live_in_set);
			mono_bitset_copyto (bb->live_out_set, old_live_out_set);

			mono_bitset_copyto (bb->live_out_set, bb->live_in_set);
			mono_bitset_sub (bb->live_in_set, bb->kill_set);
			mono_bitset_union (bb->live_in_set, bb->gen_set);

			mono_bitset_clear_all (bb->live_out_set);
			
			for (j = 0; j < bb->out_count; j++) 
				mono_bitset_union (bb->live_out_set, bb->out_bb [j]->live_in_set);

			if (!(mono_bitset_equal (old_live_in_set, bb->live_in_set) &&
			      mono_bitset_equal (old_live_out_set, bb->live_out_set)))
				changes = TRUE;
		}

	} while (changes);

	mono_bitset_free (old_live_in_set);
	mono_bitset_free (old_live_out_set);

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		for (j = max_vars - 1; j >= 0; j--) {
			
			if (mono_bitset_test (bb->live_in_set, j))
				update_live_range (cfg, j, bb->dfn, 0);

			if (mono_bitset_test (bb->live_out_set, j))
				update_live_range (cfg, j, bb->dfn, 0xffff);
		} 
	} 

#ifdef DEBUG_LIVENESS
	for (i = cfg->num_bblocks - 1; i >= 0; i--) {
		MonoBasicBlock *bb = cfg->bblocks [i];
		
		printf ("LIVE IN  BB%d: ", bb->block_num); 
		mono_bitset_print (bb->live_in_set); 
		printf ("\n");
		printf ("LIVE OUT BB%d: ", bb->block_num); 
		mono_bitset_print (bb->live_out_set); 
		printf ("\n");
	}
#endif
}

