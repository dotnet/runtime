/*
 * linear-scan.c: linbear scan register allocation
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include "jit.h"
#include "codegen.h"

//#define MNAME "nest_test"

#ifdef MNAME
#define DEGUG_LIVENESS
#define DEBUG_LSCAN
#endif

static MonoBitSet* 
mono_bitset_mp_new (MonoMemPool *mp,  guint32 max_size)
{
	int size = mono_bitset_alloc_size (max_size, 0);
	gpointer mem;

	mem = mono_mempool_alloc0 (mp, size);
	return mono_bitset_mem_new (mem, max_size, MONO_BITSET_DONT_FREE);
}

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

static void
mono_update_live_range (MonoFlowGraph *cfg, int varnum, int block_num, int tree_pos)
{
	MonoVarInfo *vi = &g_array_index (cfg->varinfo, MonoVarInfo, varnum);
	guint32 abs_pos = (block_num << 16) | tree_pos;

	if (vi->range.first_use.abs_pos > abs_pos)
		vi->range.first_use.abs_pos = abs_pos;

	if (vi->range.last_use.abs_pos < abs_pos)
		vi->range.last_use.abs_pos = abs_pos;
}

static void
mono_update_live_info (MonoFlowGraph *cfg)
{
	int i, j;

	for (i = 0; i < cfg->block_count; i++) {
		MonoBBlock *bb = &cfg->bblocks [i];

		for (j = cfg->varinfo->len - 1; j > 0; j--) {
			
			if (mono_bitset_test (bb->live_in_set, j))
				mono_update_live_range (cfg, j, bb->num, 0);

			if (mono_bitset_test (bb->live_out_set, j))
				mono_update_live_range (cfg, j, bb->num, bb->forest->len);
		} 
	} 
}

static void
mono_update_gen_set (MonoFlowGraph *cfg, MonoBBlock *bb, MBTree *tree, 
		     int tnum, MonoBitSet *set)
{
	if (tree->left) {
		switch (tree->op) {
		case MB_TERM_REMOTE_STIND_I1:
		case MB_TERM_REMOTE_STIND_I2:
		case MB_TERM_REMOTE_STIND_I4:
		case MB_TERM_REMOTE_STIND_I8:
		case MB_TERM_REMOTE_STIND_R4:
		case MB_TERM_REMOTE_STIND_R8:
		case MB_TERM_REMOTE_STIND_OBJ:
		case MB_TERM_STIND_I1:
		case MB_TERM_STIND_I2:
		case MB_TERM_STIND_I4:
		case MB_TERM_STIND_I8:
		case MB_TERM_STIND_R4:
		case MB_TERM_STIND_R8:
		case MB_TERM_STIND_OBJ:
			if (tree->left->op != MB_TERM_ADDR_L)
				mono_update_gen_set (cfg, bb, tree->left, tnum, set);
			else
				mono_update_live_range (cfg, tree->left->data.i, 
							bb->num, tnum); 
			break;
		default:
			mono_update_gen_set (cfg, bb, tree->left, tnum, set);
			break;
		}
	}

	if (tree->right)
		mono_update_gen_set (cfg, bb, tree->right, tnum, set);

	if (tree->op == MB_TERM_ADDR_L) {
		mono_update_live_range (cfg, tree->data.i, bb->num, tnum); 
		mono_bitset_set (set, tree->data.i);
	}
} 

static void
mono_analyze_liveness (MonoFlowGraph *cfg)
{
	MonoBitSet *old_live_in_set, *old_live_out_set;
	gboolean changes;
	GList *l;
	int i, j , max_vars = cfg->varinfo->len;

#ifdef DEGUG_LIVENESS
	int debug = !strcmp (cfg->method->name, MNAME);
	if (debug)
		printf ("LIVENLESS %s.%s:%s\n", cfg->method->klass->name_space,
			cfg->method->klass->name, cfg->method->name);
#endif

	old_live_in_set = mono_bitset_new (max_vars, 0);
	old_live_out_set = mono_bitset_new (max_vars, 0);

	for (i = 0; i < cfg->block_count; i++) {
		MonoBBlock *bb = &cfg->bblocks [i];

		bb->gen_set = mono_bitset_mp_new (cfg->mp, max_vars);
		bb->kill_set = mono_bitset_mp_new (cfg->mp, max_vars);
		bb->live_in_set = mono_bitset_mp_new (cfg->mp, max_vars);
		bb->live_out_set = mono_bitset_mp_new (cfg->mp, max_vars);

		for (j = 0; j < bb->forest->len; j++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (bb->forest, j);

			mono_bitset_clear_all (old_live_in_set);
			mono_update_gen_set (cfg, bb, t1, j, old_live_in_set);
			mono_bitset_sub (old_live_in_set, bb->kill_set);
			mono_bitset_union (bb->gen_set, old_live_in_set);

			switch (t1->op) {
			case MB_TERM_REMOTE_STIND_I1:
			case MB_TERM_REMOTE_STIND_I2:
			case MB_TERM_REMOTE_STIND_I4:
			case MB_TERM_REMOTE_STIND_I8:
			case MB_TERM_REMOTE_STIND_R4:
			case MB_TERM_REMOTE_STIND_R8:
			case MB_TERM_REMOTE_STIND_OBJ:
			case MB_TERM_STIND_I1:
			case MB_TERM_STIND_I2:
			case MB_TERM_STIND_I4:
			case MB_TERM_STIND_I8:
			case MB_TERM_STIND_R4:
			case MB_TERM_STIND_R8:
			case MB_TERM_STIND_OBJ:
				if (t1->left->op == MB_TERM_ADDR_L)
					mono_bitset_set (bb->kill_set, t1->left->data.i);
				break;
			}
		}

#ifdef DEGUG_LIVENESS
		if (debug) {
			printf ("BLOCK %d (", bb->num);
			for (l = bb->succ; l; l = l->next) {
				MonoBBlock *t = (MonoBBlock *)l->data;
				printf ("%d, ", t->num);
			}
			printf (")\n");
			printf ("GEN  %d: ", i); mono_bitset_print (bb->gen_set); printf ("\n");
			printf ("KILL %d: ", i); mono_bitset_print (bb->kill_set); printf ("\n");
		}
#endif
	}
	
	do {
		changes = FALSE;

		for (i =  cfg->block_count - 1; i >= 0; i--) {
			MonoBBlock *bb = &cfg->bblocks [i];

			mono_bitset_copyto (bb->live_in_set, old_live_in_set);
			mono_bitset_copyto (bb->live_out_set, old_live_out_set);

			mono_bitset_copyto (bb->live_out_set, bb->live_in_set);
			mono_bitset_sub (bb->live_in_set, bb->kill_set);
			mono_bitset_union (bb->live_in_set, bb->gen_set);

			mono_bitset_clear_all (bb->live_out_set);
			
			for (l = bb->succ; l; l = l->next) {
				MonoBBlock *t = (MonoBBlock *)l->data;
				mono_bitset_union (bb->live_out_set, t->live_in_set);
			}

			if (!(mono_bitset_equal (old_live_in_set, bb->live_in_set) &&
			      mono_bitset_equal (old_live_out_set, bb->live_out_set)))
				changes = TRUE;
		}

	} while (changes);

	mono_bitset_free (old_live_in_set);
	mono_bitset_free (old_live_out_set);


#ifdef DEGUG_LIVENESS
	if (debug) {
		for (i = 0; i < cfg->block_count; i++) {
			MonoBBlock *bb = &cfg->bblocks [i];
		
			printf ("LIVE IN  %d: ", i); 
			mono_bitset_print (bb->live_in_set); 
			printf ("\n");
			printf ("LIVE OUT %d: ", i); 
			mono_bitset_print (bb->live_out_set); 
			printf ("\n");
		}
	}
#endif
}

static GList *
mono_varlist_insert_sorted (GList *list, MonoVarInfo *vi, gboolean sort_end)
{
	GList *l;

	if (!list)
		return g_list_prepend (NULL, vi);

	for (l = list; l; l = l->next) {
		MonoVarInfo *v = (MonoVarInfo *)l->data;
		
		if (sort_end) {
			if (vi->range.last_use.abs_pos <= v->range.last_use.abs_pos) {
				list = g_list_insert_before (list, l, vi);
				break;
			}
		} else {
			if (vi->range.first_use.abs_pos <= v->range.first_use.abs_pos) {
				list = g_list_insert_before (list, l, vi);
				break;
			}
		}
	}
	if (!l)
		list = g_list_append (list, vi);

	return list;
}

void
mono_linear_scan (MonoFlowGraph *cfg, guint32 *used_mask)
{
	GList *l, *ranges = NULL;
	GList *active = NULL;
	GList *regs = NULL;
	int i, max_regs;

#ifdef DEBUG_LSCAN
	MonoMethod *m = cfg->method;
	int debug = !strcmp (cfg->method->name, MNAME);

	if (debug)
		printf ("VARINFO for %s.%s:%s\n", m->klass->name_space, m->klass->name, m->name);
#endif

	mono_analyze_liveness (cfg);
	mono_update_live_info (cfg);

	for (i = 1; i < cfg->varinfo->len; i++) {
		MonoVarInfo *vi = &g_array_index (cfg->varinfo, MonoVarInfo, i);

		/* unused vars */
		if (vi->range.first_use.abs_pos > vi->range.last_use.abs_pos)
			continue;

		/* we can only allocate 32 bit values */
		if (vi->isvolatile || (vi->type != VAL_I32 && vi->type != VAL_POINTER))
			continue;

		ranges = mono_varlist_insert_sorted (ranges, vi, FALSE);

	}

#ifdef DEBUG_LSCAN
	if (debug) {
		for (l = ranges; l; l = l->next) {
			MonoVarInfo *vi = (MonoVarInfo *)l->data;

			printf ("VAR %d %08x %08x\n", vi->varnum, vi->range.first_use.abs_pos,  
				vi->range.last_use.abs_pos);
		}
	}
#endif
	
	/* we can use 2 registers for global allocation */
	regs = g_list_prepend (regs, (gpointer)X86_EBX);
	regs = g_list_prepend (regs, (gpointer)X86_ESI);

	max_regs = g_list_length (regs);

	/* linear scan */

	for (l = ranges; l; l = l->next) {
		MonoVarInfo *vi = (MonoVarInfo *)l->data;

#ifdef DEBUG_LSCAN
		if (debug)
			printf ("START  %2d %08x %08x\n",  vi->varnum, vi->range.first_use.abs_pos, 
				vi->range.last_use.abs_pos);
#endif
		/* expire old intervals in active */
		while (active) {
			MonoVarInfo *v = (MonoVarInfo *)active->data;

			if (v->range.last_use.abs_pos >= vi->range.first_use.abs_pos)
				break;

#ifdef DEBUG_LSCAN
			if (debug)
				printf ("EXPIR  %2d %08x %08x\n",  v->varnum, 
					v->range.first_use.abs_pos, v->range.last_use.abs_pos);
#endif
			active = g_list_remove_link (active, active);
			regs = g_list_prepend (regs, (gpointer)v->reg);
		}
		
		if (active && g_list_length (active) == max_regs) {
			/* Spill */
			
			GList *a = g_list_nth (active, max_regs - 1);
			MonoVarInfo *v = (MonoVarInfo *)a->data;

			if (v->range.last_use.abs_pos > vi->range.last_use.abs_pos) {
				vi->reg = v->reg;
				v->reg = -1;
				active = g_list_remove_link (active, a);
				active = mono_varlist_insert_sorted (active, vi, TRUE);		
#ifdef DEBUG_LSCAN
				if (debug)
					printf ("SPILL0 %2d %08x %08x\n",  v->varnum, 
						v->range.first_use.abs_pos, v->range.last_use.abs_pos);
#endif
			} else {
#ifdef DEBUG_LSCAN
				if (debug)
					printf ("SPILL1 %2d %08x %08x\n",  vi->varnum, 
						vi->range.first_use.abs_pos, vi->range.last_use.abs_pos);
#endif
				vi->reg = -1;
			}
		} else {
			/* assign register */

			g_assert (regs);

			vi->reg = (int)regs->data;

			*used_mask |= 1 << vi->reg;

			regs = g_list_remove_link (regs, regs);

#ifdef DEBUG_LSCAN
			if (debug)
				printf ("ADD    %2d %08x %08x\n",  vi->varnum, 
					vi->range.first_use.abs_pos, vi->range.last_use.abs_pos);
#endif
			active = mono_varlist_insert_sorted (active, vi, TRUE);		
		}


#ifdef DEBUG_LSCAN
		if (debug) {
			GList *a;
			for (a = active; a; a = a->next) {
				MonoVarInfo *v = (MonoVarInfo *)a->data;
			     
				printf ("ACT    %2d %08x %08x %d\n", v->varnum, 
					v->range.first_use.abs_pos,  v->range.last_use.abs_pos, v->reg);
			}
			printf ("NEXT\n");
		}
#endif
	}	

	g_list_free (regs);
	g_list_free (active);
	g_list_free (ranges);

}

