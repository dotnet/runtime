/**
 * \file
 * Dominator computation on the control flow graph
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
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

#include "mini.h"

#ifndef DISABLE_JIT

/*
 * bb->dfn == 0 means either the bblock is ignored by the dfn calculation, or
 * it is the entry bblock.
 */
#define HAS_DFN(bb, entry) ((bb)->dfn || ((bb) == entry))

/*
 * Compute dominators and immediate dominators using the algorithm in the
 * paper "A Simple, Fast Dominance Algorithm" by Keith D. Cooper,
 * Timothy J. Harvey, and Ken Kennedy:
 * http://citeseer.ist.psu.edu/cooper01simple.html
 */
static void
compute_dominators (MonoCompile *cfg)
{
	int bitsize;
	MonoBasicBlock *entry;
	MonoBasicBlock **doms;
	gboolean changed;

	g_assert (!(cfg->comp_done & MONO_COMP_DOM));

	bitsize = mono_bitset_alloc_size (cfg->num_bblocks, 0);

	entry = cfg->bblocks [0];

	doms = g_new0 (MonoBasicBlock*, cfg->num_bblocks);
	doms [entry->dfn] = entry;

	if (cfg->verbose_level > 1) {
		for (guint i = 0; i < cfg->num_bblocks; ++i) {
			int j;
			MonoBasicBlock *bb = cfg->bblocks [i];

			printf ("BB%d IN: ", bb->block_num);
			for (j = 0; j < bb->in_count; ++j)
				printf ("%d ", bb->in_bb [j]->block_num);
			printf ("\n");
		}
	}

	changed = TRUE;
	while (changed) {
		changed = FALSE;

		for (guint bindex = 0; bindex < cfg->num_bblocks; ++bindex) {
			MonoBasicBlock *bb = cfg->bblocks [bindex];
			MonoBasicBlock *idom;

			idom = NULL;
			gint16 i;
			for (i = 0; i < bb->in_count; ++i) {
				MonoBasicBlock *in_bb = bb->in_bb [i];
				if ((in_bb != bb) && doms [in_bb->dfn]) {
					idom = in_bb;
					break;
				}
			}
			if (bb != cfg->bblocks [0])
				g_assert (idom);

			while (i < bb->in_count) {
				MonoBasicBlock *in_bb = bb->in_bb [i];

				if (HAS_DFN (in_bb, entry) && doms [in_bb->dfn]) {
					/* Intersect */
					MonoBasicBlock *f1 = idom;
					MonoBasicBlock *f2 = in_bb;

					while (f1 != f2) {
						if (f1->dfn < f2->dfn)
							f2 = doms [f2->dfn];
						else
							f1 = doms [f1->dfn];
					}

					idom = f1;
				}
				i ++;
			}

			if (idom != doms [bb->dfn]) {
				if (bb == cfg->bblocks [0])
					doms [bb->dfn] = bb;
				else {
					doms [bb->dfn] = idom;
					changed = TRUE;
				}

				//printf ("A: bb=%d dfn=%d dom:%d\n", bb->block_num, bb->dfn, doms [bb->dfn]->block_num);
			}
		}
	}

	/* Compute bb->dominators for each bblock */
	for (guint i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];
		MonoBasicBlock *cbb;
		MonoBitSet *dominators;
		char *mem;

		mem = (char *)mono_mempool_alloc0 (cfg->mempool, bitsize);

		bb->dominators = dominators = mono_bitset_mem_new (mem, cfg->num_bblocks, 0);
		mem += bitsize;

		mono_bitset_set_fast (dominators, bb->dfn);

		if (bb->dfn) {
			for (cbb = doms [bb->dfn]; cbb->dfn; cbb = doms [cbb->dfn])
				mono_bitset_set_fast (dominators, cbb->dfn);

			bb->idom = doms [bb->dfn];
			if (bb->idom)
				bb->idom->dominated = g_slist_prepend_mempool (cfg->mempool, bb->idom->dominated, bb);
		}

		/* The entry bb */
		mono_bitset_set_fast (dominators, 0);
	}

	g_free (doms);

	cfg->comp_done |= MONO_COMP_DOM | MONO_COMP_IDOM;

	if (cfg->verbose_level > 1) {
		printf ("DTREE %s %d\n", mono_method_full_name (cfg->method, TRUE),
			cfg->header->num_clauses);
		for (guint i = 0; i < cfg->num_bblocks; ++i) {
			MonoBasicBlock *bb = cfg->bblocks [i];
			printf ("BB%d(dfn=%d) (IDOM=BB%d): ", bb->block_num, bb->dfn, bb->idom ? bb->idom->block_num : -1);
			mono_blockset_print (cfg, bb->dominators, NULL, -1);
		}
	}
}

#if 0

static void
check_dominance_frontier (MonoBasicBlock *x, MonoBasicBlock *t)
{
	int i, j;

	t->flags |= BB_VISITED;

	if (mono_bitset_test_fast (t->dominators, x->dfn)) {
		for (i = 0; i < t->out_count; ++i) {
			if (!(t->flags & BB_VISITED)) {
				int found = FALSE;
				check_dominance_frontier (x, t->out_bb [i]);

				for (j = 0; j < t->out_bb [i]->in_count; j++) {
					if (t->out_bb [i]->in_bb [j] == t)
						found = TRUE;
				}
				g_assert (found);
			}
		}
	} else {
		if (!mono_bitset_test_fast (x->dfrontier, t->dfn)) {
			printf ("BB%d not in frontier of BB%d\n", t->block_num, x->block_num);
			g_assert_not_reached ();
		}
	}
}

#endif

/**
 * Compute dominance frontiers using the algorithm from the same paper.
 */
static void
compute_dominance_frontier (MonoCompile *cfg)
{
	char *mem;
	int bitsize;
	gint16 j;

	g_assert (!(cfg->comp_done & MONO_COMP_DFRONTIER));

	for (guint i = 0; i < cfg->num_bblocks; ++i)
		cfg->bblocks [i]->flags &= ~BB_VISITED;

	bitsize = mono_bitset_alloc_size (cfg->num_bblocks, 0);
	mem = (char *)mono_mempool_alloc0 (cfg->mempool, bitsize * cfg->num_bblocks);

	for (guint i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];
		bb->dfrontier = mono_bitset_mem_new (mem, cfg->num_bblocks, 0);
		mem += bitsize;
	}

	for (guint i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		if (bb->in_count > 1) {
			for (j = 0; j < bb->in_count; ++j) {
				MonoBasicBlock *p = bb->in_bb [j];

				if (p->dfn || (p == cfg->bblocks [0])) {
					while (p != bb->idom) {
						mono_bitset_set_fast (p->dfrontier, bb->dfn);
						p = p->idom;
					}
				}
			}
		}
	}

#if 0
	for (guint i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		printf ("DFRONT %s BB%d: ", mono_method_full_name (cfg->method, TRUE), bb->block_num);
		mono_blockset_print (cfg, bb->dfrontier, NULL, -1);
	}
#endif

#if 0
	/* this is a check for the dominator frontier */
	for (guint i = 0; i < m->num_bblocks; ++i) {
		MonoBasicBlock *x = m->bblocks [i];

		mono_bitset_foreach_bit ((x->dfrontier), j, (m->num_bblocks)) {
			MonoBasicBlock *w = m->bblocks [j];
			int k;
			/* x must not strictly dominates w */
			if (mono_bitset_test_fast (w->dominators, x->dfn) && w != x)
				g_assert_not_reached ();

			for (k = 0; k < m->num_bblocks; ++k)
				m->bblocks [k]->flags &= ~BB_VISITED;

			check_dominance_frontier (x, x);
		}
	}
#endif

	cfg->comp_done |= MONO_COMP_DFRONTIER;
}

static void
df_set (MonoCompile *m, MonoBitSet* dest, MonoBitSet *set)
{
	guint i;
	mono_bitset_foreach_bit (set, i, m->num_bblocks) {
		mono_bitset_union_fast (dest, m->bblocks [i]->dfrontier);
	}
}

MonoBitSet*
mono_compile_iterated_dfrontier (MonoCompile *m, MonoBitSet *set)
{
	MonoBitSet *result;
	int bitsize, count1, count2;

	bitsize = mono_bitset_alloc_size (m->num_bblocks, 0);
	result = mono_bitset_mem_new (mono_mempool_alloc0 (m->mempool, bitsize), m->num_bblocks, 0);

	df_set (m, result, set);
	count2 = mono_bitset_count (result);
	do {
		count1 = count2;
		df_set (m, result, result);
		count2 = mono_bitset_count (result);
	} while (count2 > count1);

	return result;
}

void
mono_compile_dominator_info (MonoCompile *cfg, int dom_flags)
{
	if ((dom_flags & MONO_COMP_DOM) && !(cfg->comp_done & MONO_COMP_DOM))
		compute_dominators (cfg);
	if ((dom_flags & MONO_COMP_DFRONTIER) && !(cfg->comp_done & MONO_COMP_DFRONTIER))
		compute_dominance_frontier (cfg);
}

/*
 * code to detect loops and loop nesting level
 */
void
mono_compute_natural_loops (MonoCompile *cfg)
{
	MonoBitSet *in_loop_blocks;
	int *bb_indexes;

	g_assert (!(cfg->comp_done & MONO_COMP_LOOPS));

	in_loop_blocks = mono_bitset_new (cfg->num_bblocks + 1, 0);
	for (guint i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *n = cfg->bblocks [i];

		for (gint16 j = 0; j < n->out_count; j++) {
			MonoBasicBlock *h = n->out_bb [j];
			/* check for single block loops */
			if (n == h) {
				h->loop_blocks = g_list_prepend_mempool (cfg->mempool, h->loop_blocks, h);
				h->nesting++;
			}
			/* check for back-edge from n to h */
			else if (n != h && mono_bitset_test_fast (n->dominators, h->dfn)) {
				GSList *todo;

				/* already in loop_blocks? */
				if (h->loop_blocks && g_list_find (h->loop_blocks, n)) {
					continue;
				}

				mono_bitset_clear_all (in_loop_blocks);
				if (h->loop_blocks) {
					GList *l;

					for (l = h->loop_blocks; l; l = l->next) {
						MonoBasicBlock *b = (MonoBasicBlock *)l->data;
						if (b->dfn)
							mono_bitset_set_fast (in_loop_blocks, b->dfn);
					}
				}
				todo = g_slist_prepend (NULL, n);

				while (todo) {
					MonoBasicBlock *cb = (MonoBasicBlock *)todo->data;
					todo = g_slist_delete_link (todo, todo);

					if ((cb->dfn && mono_bitset_test_fast (in_loop_blocks, cb->dfn)) || (!cb->dfn && g_list_find (h->loop_blocks, cb)))
						continue;

					h->loop_blocks = g_list_prepend_mempool (cfg->mempool, h->loop_blocks, cb);
					cb->nesting++;
					if (cb->dfn)
						mono_bitset_set_fast (in_loop_blocks, cb->dfn);

					for (gint16 k = 0; k < cb->in_count; k++) {
						MonoBasicBlock *prev = cb->in_bb [k];
						/* add all previous blocks */
						if (prev != h && !((prev->dfn && mono_bitset_test_fast (in_loop_blocks, prev->dfn)) || (!prev->dfn && g_list_find (h->loop_blocks, prev)))) {
							todo = g_slist_prepend (todo, prev);
						}
					}
				}

				/* add the header if not already there */
				if (!((h->dfn && mono_bitset_test_fast (in_loop_blocks, h->dfn)) || (!h->dfn && g_list_find (h->loop_blocks, h)))) {
					h->loop_blocks = g_list_prepend_mempool (cfg->mempool, h->loop_blocks, h);
					h->nesting++;
				}
			}
		}
	}
	mono_bitset_free (in_loop_blocks);

	cfg->comp_done |= MONO_COMP_LOOPS;

	/* Compute loop_body_start for each loop */
	bb_indexes = g_new0 (int, cfg->num_bblocks);
	{
		MonoBasicBlock *bb = cfg->bb_entry;
		for (int i = 0; bb; i ++, bb = bb->next_bb) {
			if (bb->dfn)
				bb_indexes [bb->dfn] = i;
		}
	}
	for (guint i = 0; i < cfg->num_bblocks; ++i) {
		if (cfg->bblocks [i]->loop_blocks) {
			/* The loop body start is the first bblock in the order they will be emitted */
			MonoBasicBlock *h = cfg->bblocks [i];
			MonoBasicBlock *body_start = h;
			GList *l;

			for (l = h->loop_blocks; l; l = l->next) {
				MonoBasicBlock *cb = (MonoBasicBlock *)l->data;

				if (cb->dfn && bb_indexes [cb->dfn] < bb_indexes [body_start->dfn]) {
					body_start = cb;
				}
			}

			body_start->loop_body_start = 1;
		}
	}
	g_free (bb_indexes);

	if (cfg->verbose_level > 1) {
		for (guint i = 0; i < cfg->num_bblocks; ++i) {
			if (cfg->bblocks [i]->loop_blocks) {
				MonoBasicBlock *h = (MonoBasicBlock *)cfg->bblocks [i]->loop_blocks->data;
				GList *l;
				printf ("LOOP START %d\n", h->block_num);
				for (l = h->loop_blocks; l; l = l->next) {
					MonoBasicBlock *cb = (MonoBasicBlock *)l->data;
					printf ("\tBB%d %d %p\n", cb->block_num, cb->nesting, cb->loop_blocks);
				}
			}
		}
	}
}

static void
clear_idominators (MonoCompile *cfg)
{
	guint i;

	for (i = 0; i < cfg->num_bblocks; ++i) {
		if (cfg->bblocks[i]->dominated) {
			cfg->bblocks[i]->dominated = NULL;
		}
	}

	cfg->comp_done &= ~MONO_COMP_IDOM;
}

static void
clear_loops (MonoCompile *cfg)
{
	guint i;

	for (i = 0; i < cfg->num_bblocks; ++i) {
		cfg->bblocks[i]->nesting = 0;
		cfg->bblocks[i]->loop_blocks = NULL;
	}

	cfg->comp_done &= ~MONO_COMP_LOOPS;
}

void
mono_free_loop_info (MonoCompile *cfg)
{
    if (cfg->comp_done & MONO_COMP_IDOM)
        clear_idominators (cfg);
    if (cfg->comp_done & MONO_COMP_LOOPS)
        clear_loops (cfg);
}

#else /* DISABLE_JIT */

void
mono_free_loop_info (MonoCompile *cfg)
{
}

#endif /* DISABLE_JIT */
