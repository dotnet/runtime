/*
 * dominators.c: Dominator computation on the control flow graph
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */
#include <string.h>
#include <mono/metadata/debug-helpers.h>

#include "mini.h"

//#define DEBUG_DOMINATORS

/* the simpler, dumber algorithm */
static void
compute_dominators (MonoCompile *m) {
	int change = TRUE;
	int i, j, bitsize;
	MonoBasicBlock *bb;
	MonoBitSet *T;
	char* mem;

	g_assert (!(m->comp_done & MONO_COMP_DOM));

	bitsize = mono_bitset_alloc_size (m->num_bblocks, 0);
	/* the first is always the entry */
	bb = m->bblocks [0];
	mem = mono_mempool_alloc0 (m->mempool, bitsize * (m->num_bblocks + 1));
	bb->dominators = mono_bitset_mem_new (mem, m->num_bblocks, 0);

	mem += bitsize;
	mono_bitset_set (bb->dominators, 0);

	T = mono_bitset_mem_new (mem, m->num_bblocks, 0);
	mem += bitsize;


	for (i = 1; i < m->num_bblocks; ++i) {
		bb = m->bblocks [i];
		bb->dominators = mono_bitset_mem_new (mem, m->num_bblocks, 0);
		mem += bitsize;
		mono_bitset_invert (bb->dominators);

#ifdef DEBUG_DOMINATORS
		printf ("BB%d IN: ", bb->block_num);
		for (j = 0; j < bb->in_count; ++j) 
			printf ("%d ", bb->in_bb [j]->block_num);
		printf ("\n");
#endif
	}

	do {
		change = FALSE;
		for (i = 1; i < m->num_bblocks; ++i) {
			bb = m->bblocks [i];
			mono_bitset_set_all (T);
			for (j = 0; j < bb->in_count; ++j) {
				if (bb->in_bb [j]->dominators)
					mono_bitset_intersection (T, bb->in_bb [j]->dominators);
			}
			mono_bitset_set (T, i);
			if (!mono_bitset_equal (T, bb->dominators)) {
				change = TRUE;
				mono_bitset_copyto (T, bb->dominators);
			}
		}
	} while (change);

	m->comp_done |= MONO_COMP_DOM;

#ifdef DEBUG_DOMINATORS
	printf ("DTREE %s %d\n", mono_method_full_name (m->method, TRUE), 
		((MonoMethodNormal *)m->method)->header->num_clauses);
	for (i = 0; i < m->num_bblocks; ++i) {
		bb = m->bblocks [i];
		printf ("BB%d: ", bb->block_num);
		mono_blockset_print (m, bb->dominators, NULL, -1);
	}
#endif
}

static void
compute_idominators (MonoCompile* m) {
	char *mem;
	int bitsize, i, s, t;
	MonoBitSet **T, *temp;
	MonoBasicBlock *bb;

	g_assert (!(m->comp_done & MONO_COMP_IDOM));

	bitsize = mono_bitset_alloc_size (m->num_bblocks, 0);
	mem = mono_mempool_alloc (m->mempool, bitsize * (m->num_bblocks + 1));
	T = mono_mempool_alloc (m->mempool, sizeof (MonoBitSet*) * m->num_bblocks);

	for (i = 0; i < m->num_bblocks; ++i) {
		bb = m->bblocks [i];
		T [i] = mono_bitset_mem_new (mem, m->num_bblocks, 0);
		mono_bitset_copyto (bb->dominators, T [i]);
		mono_bitset_clear (T [i], i);
		if (mono_bitset_count (bb->dominators) - 1 != mono_bitset_count (T [i])) {
			mono_blockset_print (m, bb->dominators, "dominators", -1);
			mono_blockset_print (m, T [i], "T [i]", -1);
			g_error ("problem at %d (%d)\n", i, bb->dfn);
		}
		mem += bitsize;
	}
	temp = mono_bitset_mem_new (mem, m->num_bblocks, 0);

	for (i = 1; i < m->num_bblocks; ++i) {

		temp = T [i];
			
		mono_bitset_foreach_bit_rev (temp, s, m->num_bblocks) {

			mono_bitset_foreach_bit_rev (temp, t, m->num_bblocks) {
						
				if (t == s)
					continue;

				//if (mono_bitset_test_fast (T [s], t))
				if (mono_bitset_test_fast (m->bblocks [s]->dominators, t))
					mono_bitset_clear (temp, t);
			}
		}

#ifdef DEBUG_DOMINATORS
		printf ("IDOMSET BB%d %d: ", m->bblocks [i]->block_num, m->num_bblocks);
		mono_blockset_print (m, T [i], NULL, -1);
#endif
	}

	for (i = 1; i < m->num_bblocks; ++i) {
		bb = m->bblocks [i];
		s = mono_bitset_find_start (T [i]);
		g_assert (s != -1);
		/*fixme:mono_bitset_count does not really work */
		//g_assert (mono_bitset_count (T [i]) == 1);
		t = mono_bitset_find_first (T [i], s);
		g_assert (t == -1 || t >=  m->num_bblocks);
		bb->idom = m->bblocks [s];
		bb->idom->dominated = g_list_prepend (bb->idom->dominated, bb);
	}

	m->comp_done |= MONO_COMP_IDOM;
}

static void
postorder_visit (MonoBasicBlock *start, int *idx, MonoBasicBlock **array)
{
	int i;

	/* we assume the flag was already cleared by the caller. */
	start->flags |= BB_VISITED;
	/*g_print ("visit %d at %p\n", *dfn, start->cil_code);*/
	for (i = 0; i < start->out_count; ++i) {
		if (start->out_bb [i]->flags & BB_VISITED)
			continue;
		postorder_visit (start->out_bb [i], idx, array);
	}
	array [*idx] = start;
	(*idx)++;
}

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

#if 0
/* there is a bug in this code */
static void
compute_dominance_frontier_old (MonoCompile *m) {
	int i, j, bitsize;
	MonoBasicBlock **postorder;
	MonoBasicBlock *bb, *z;
	char *mem;

	g_assert (!(m->comp_done & MONO_COMP_DFRONTIER));

	postorder = mono_mempool_alloc (m->mempool, sizeof (MonoBasicBlock*) * m->num_bblocks);
	i = 0;
	postorder_visit (m->bb_entry, &i, postorder);
	/*g_print ("postorder traversal:");
	for (i = 0; i < m->num_bblocks; ++i)
		g_print (" B%d", postorder [i]->dfn);
	g_print ("\n");*/
	
	/* we could reuse the bitsets allocated in compute_idominators() */
	bitsize = mono_bitset_alloc_size (m->num_bblocks, 0);
	mem = mono_mempool_alloc0 (m->mempool, bitsize * m->num_bblocks);

	for (i = 0; i < m->num_bblocks; ++i) {
		bb = postorder [i];
		bb->dfrontier = mono_bitset_mem_new (mem, m->num_bblocks, 0);
		mem += bitsize;
	}
	for (i = 0; i < m->num_bblocks; ++i) {
		bb = postorder [i];
		/* the local component */
		for (j = 0; j < bb->out_count; ++j) {
			//if (bb->out_bb [j] != bb->idom)
			if (bb->out_bb [j]->idom != bb)
				mono_bitset_set (bb->dfrontier, bb->out_bb [j]->dfn);
		}
	}
	for (i = 0; i < m->num_bblocks; ++i) {
		bb = postorder [i];
		/* the up component */
		if (bb->idom) {
			z = bb->idom;
			mono_bitset_foreach_bit (z->dfrontier, j, m->num_bblocks) {
				//if (m->bblocks [j] != bb->idom)
				if (m->bblocks [j]->idom != bb)
					mono_bitset_set (bb->dfrontier, m->bblocks [j]->dfn);
			}
		}
	}

	/* this is a check for the dominator frontier */
	for (i = 0; i < m->num_bblocks; ++i) {
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

	m->comp_done |= MONO_COMP_DFRONTIER;
}
#endif

/* this is an implementation of the dominance frontier algorithm described in
 * "modern compiler implementation in C" written by Andrew W. Appel
 */
static void
compute_dominance_frontier_appel (MonoCompile *m, int n) 
{
	int i, j;
	MonoBasicBlock *bb;

	bb = m->bblocks [n];
	g_assert (!(bb->flags & BB_VISITED));
	bb->flags |= BB_VISITED;

	for (i = 0; i < bb->out_count; ++i) {
		MonoBasicBlock *y = bb->out_bb [i];
		if (y->idom != bb) {
			g_assert (!(mono_bitset_test_fast (y->dominators, bb->dfn) && bb->dfn != y->dfn));
			mono_bitset_set (bb->dfrontier, y->dfn);
		}
	}
	
	
	for (i = 0; i < m->num_bblocks; ++i) {
		MonoBasicBlock *c = m->bblocks [i];
		if (c->idom == bb) {
			if (!(c->flags & BB_VISITED))
				compute_dominance_frontier_appel (m, c->dfn);
			mono_bitset_foreach_bit (c->dfrontier, j, m->num_bblocks) {
				MonoBasicBlock *w = m->bblocks [j];
				if (!(mono_bitset_test_fast (w->dominators, bb->dfn) && bb->dfn != w->dfn))
					mono_bitset_set (bb->dfrontier, w->dfn);
			}
		}
	}
}

static void
compute_dominance_frontier (MonoCompile *m) 
{
	MonoBasicBlock *bb;
	char *mem;
	int i, j, bitsize;

	g_assert (!(m->comp_done & MONO_COMP_DFRONTIER));

	for (i = 0; i < m->num_bblocks; ++i)
		m->bblocks [i]->flags &= ~BB_VISITED;

	/* we could reuse the bitsets allocated in compute_idominators() */
	bitsize = mono_bitset_alloc_size (m->num_bblocks, 0);
	mem = mono_mempool_alloc0 (m->mempool, bitsize * m->num_bblocks);
 
	for (i = 0; i < m->num_bblocks; ++i) {
		bb = m->bblocks [i];
		bb->dfrontier = mono_bitset_mem_new (mem, m->num_bblocks, 0);
		mem += bitsize;
	}

	compute_dominance_frontier_appel (m, 0);

#if 0
	for (i = 0; i < m->num_bblocks; ++i) {
		MonoBasicBlock *x = m->bblocks [i];
		
		printf ("DFRONT %s BB%d: ", mono_method_full_name (m->method, TRUE), x->block_num);
		mono_blockset_print (m, x->dfrontier, NULL, -1);
	}
#endif

#if 1
	/* this is a check for the dominator frontier */
	for (i = 0; i < m->num_bblocks; ++i) {
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

	m->comp_done |= MONO_COMP_DFRONTIER;
}

void    
mono_compile_dominator_info (MonoCompile *cfg, int dom_flags)
{
	if ((dom_flags & MONO_COMP_DOM) && !(cfg->comp_done & MONO_COMP_DOM))
		compute_dominators (cfg);
	if ((dom_flags & MONO_COMP_IDOM) && !(cfg->comp_done & MONO_COMP_IDOM))
		compute_idominators (cfg);
	if ((dom_flags & MONO_COMP_DFRONTIER) && !(cfg->comp_done & MONO_COMP_DFRONTIER))
		compute_dominance_frontier (cfg);
}

static void
df_set (MonoCompile *m, MonoBitSet* dest, MonoBitSet *set) 
{
	int i;

	mono_bitset_clear_all (dest);
	mono_bitset_foreach_bit (set, i, m->num_bblocks) {
		mono_bitset_union (dest, m->bblocks [i]->dfrontier);
	}
}

/* TODO: alloc tmp and D on the stack */
MonoBitSet*
mono_compile_iterated_dfrontier (MonoCompile *m, MonoBitSet *set) 
{
	MonoBitSet *result, *D;
	int bitsize, change = TRUE;

	bitsize = mono_bitset_alloc_size (m->num_bblocks, 0);
	result = mono_bitset_mem_new (mono_mempool_alloc (m->mempool, bitsize), m->num_bblocks, 0);
	D = mono_bitset_mem_new (mono_mempool_alloc (m->mempool, bitsize), m->num_bblocks, 0);

	df_set (m, result, set);
	do {
		change = FALSE;
		df_set (m, D, result);
		mono_bitset_union (D, result);

		if (!mono_bitset_equal (D, result)) {
			mono_bitset_copyto (D, result);
			change = TRUE;
		}
	} while (change);
	
	return result;
}

//#define DEBUG_NATURAL_LOOPS

/*
 * code to detect loops and loop nesting level
 */
void 
mono_compute_natural_loops (MonoCompile *cfg)
{
	int i, j, k;

	g_assert (!(cfg->comp_done & MONO_COMP_LOOPS));

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *n = cfg->bblocks [i];

		for (j = 0; j < n->out_count; j++) {
			MonoBasicBlock *h = n->out_bb [j];
			/* check for back-edge from n to h */
			if (n != h && mono_bitset_test (n->dominators, h->dfn)) {
				GList *todo;

				n->loop_body_start = 1;

				/* already in loop_blocks? */
				if (h->loop_blocks && g_list_find (h->loop_blocks, n))
					continue;
				
				todo = g_list_prepend (NULL, n);

				while (todo) {
					MonoBasicBlock *cb = (MonoBasicBlock *)todo->data;
					todo = g_list_delete_link (todo, todo);

					if (g_list_find (h->loop_blocks, cb))
						continue;

					h->loop_blocks = g_list_prepend (h->loop_blocks, cb);
					cb->nesting++;

					for (k = 0; k < cb->in_count; k++) {
						MonoBasicBlock *prev = cb->in_bb [k];
						/* add all previous blocks */
						if (prev != h && !g_list_find (h->loop_blocks, prev)) {
							todo = g_list_prepend (todo, prev);
						}
					}
				}

				/* add the header if not already there */
				if (!g_list_find (h->loop_blocks, h)) {
					h->loop_blocks = g_list_prepend (h->loop_blocks, h);
					h->nesting++;
				}
			}
		}
	}

	cfg->comp_done |= MONO_COMP_LOOPS;
	
#ifdef DEBUG_NATURAL_LOOPS
	for (i = 0; i < cfg->num_bblocks; ++i) {
		if (cfg->bblocks [i]->loop_blocks) {
			MonoBasicBlock *h = (MonoBasicBlock *)cfg->bblocks [i]->loop_blocks->data;
			GList *l;
			printf ("LOOP START %d\n", h->block_num);
			for (l = h->loop_blocks; l; l = l->next) {
				MonoBasicBlock *cb = (MonoBasicBlock *)l->data;
				printf (" BB%d %d %p\n", cb->block_num, cb->nesting, cb->loop_blocks);
			}
		}
	}
#endif

}

static void
clear_idominators (MonoCompile *cfg)
{
	guint i;
    
	for (i = 0; i < cfg->num_bblocks; ++i) {
		if (cfg->bblocks[i]->dominated) {
			g_list_free (cfg->bblocks[i]->dominated);        
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
		if (cfg->bblocks[i]->loop_blocks) {
			g_list_free (cfg->bblocks[i]->loop_blocks);        
			cfg->bblocks[i]->loop_blocks = NULL;
		}
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
    
