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
#include "debug.h"

//#define MNAME "nest_test"

#ifdef MNAME
#define DEGUG_LIVENESS
#define DEBUG_LSCAN
#endif

inline static MonoBitSet
mono_bitset_alloc (MonoFlowGraph *cfg, int n, gboolean use_mempool)
{
	if (use_mempool)
		return (MonoBitSet)mono_mempool_alloc0 (cfg->mp, ((n + 31) >> 5) << 2);
	else
		return (MonoBitSet)g_malloc0 (((n + 31) >> 5) << 2);	
}

static void
mono_bitset_set (MonoBitSet set, int n)
{
	int ind = n >> 5;
	int pos = n & 0x1f;
	set [ind] |= 1<<pos;
}

static gboolean
mono_bitset_inset (MonoBitSet set, int n)
{
	int ind = n >> 5;
	int pos = n & 0x1f;

	if (set [ind] & (1<<pos))
		return TRUE;

	return FALSE;
}

static void
mono_bitset_unset (MonoBitSet set, int n)
{
	int ind = n >> 5;
	int pos = n & 0x1f;

	set [ind] &= ~(1<<pos);
}

static void
mono_bitset_add (MonoBitSet set1, MonoBitSet set2, int len)
{
	int i, ind = (len + 31) >> 5;
	
	for (i = 0; i < ind; i++)
		set1 [i] |= set2 [i];
}

static void
mono_bitset_sub (MonoBitSet set1, MonoBitSet set2, int len)
{
	int i, ind = (len + 31) >> 5;
	
	for (i = 0; i < ind; i++)
		set1 [i] &= ~(set2 [i]);
}

static void
mono_bitset_copy (MonoBitSet set1, MonoBitSet set2, int len)
{
	memcpy (set1, set2, (len + 7) >> 3);
}

static int
mono_bitset_cmp (MonoBitSet set1, MonoBitSet set2, int len)
{
	return memcmp (set1, set2, (len + 7) >> 3);
}

static void
mono_bitset_clear (MonoBitSet set, int len)
{
	memset (set, 0, (len + 7) >> 3);
}

static void
mono_bitset_print (MonoBitSet set, int len)
{
	int i;

	printf ("{");
	for (i = 0; i < len; i++) {
		int ind = i >> 5;
		int pos = i & 0x1f;

		if (set [ind] & (1<<pos))
			printf ("%d, ", i);

	}
	printf ("}");
}

static void
update_live_range (MonoFlowGraph *cfg, int varnum, int block_num, int tree_pos)
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

		for (j = cfg->varinfo->len; j > 0; j--) {
			
			if (mono_bitset_inset (bb->live_in_set, j))
				update_live_range (cfg, j, bb->num, 0);

			if (mono_bitset_inset (bb->live_out_set, j))
				update_live_range (cfg, j, bb->num, bb->forest->len);
		} 
	} 
}

static void
update_tree_live_info (MonoFlowGraph *cfg, MonoBBlock *bb, MBTree *tree)
{
	if (tree->left)
		update_tree_live_info (cfg, bb, tree->left);
	if (tree->right)
		update_tree_live_info (cfg, bb, tree->right);

	if (tree->op == MB_TERM_ADDR_L)
		update_live_range (cfg, tree->data.i, bb->num, bb->forest->len); 
} 

static void
update_gen_set (MBTree *tree, MonoBitSet set)
{
	if (tree->left) {
		switch (tree->op) {
		case MB_TERM_REMOTE_STIND_I1:
		case MB_TERM_REMOTE_STIND_I2:
		case MB_TERM_REMOTE_STIND_I4:
		case MB_TERM_REMOTE_STIND_I8:
		case MB_TERM_REMOTE_STIND_R4:
		case MB_TERM_REMOTE_STIND_R8:
		case MB_TERM_REMOTE_STIND_REF:
		case MB_TERM_REMOTE_STIND_OBJ:
		case MB_TERM_STIND_I1:
		case MB_TERM_STIND_I2:
		case MB_TERM_STIND_I4:
		case MB_TERM_STIND_I8:
		case MB_TERM_STIND_R4:
		case MB_TERM_STIND_R8:
		case MB_TERM_STIND_REF:
		case MB_TERM_STIND_OBJ:
			if (tree->left->op != MB_TERM_ADDR_L)
				update_gen_set (tree->left, set);
				break;
		default:
			update_gen_set (tree->left, set);
			break;
		}
	}

	if (tree->right)
		update_gen_set (tree->right, set);

	if (tree->op == MB_TERM_ADDR_L) 
		mono_bitset_set (set, tree->data.i);
} 

static void
mono_analyze_liveness (MonoFlowGraph *cfg)
{
	MonoBitSet old_live_in_set, old_live_out_set;
	gboolean changes;
	GList *l;
	int i, j , max_vars = cfg->varinfo->len;

#ifdef DEGUG_LIVENESS
	int debug = !strcmp (cfg->method->name, MNAME);
	if (debug)
		printf ("LIVENLESS %s.%s:%s\n", cfg->method->klass->name_space,
			cfg->method->klass->name, cfg->method->name);
#endif

	old_live_in_set = mono_bitset_alloc (cfg, max_vars, FALSE);
	old_live_out_set = mono_bitset_alloc (cfg, max_vars, FALSE);

	for (i = 0; i < cfg->block_count; i++) {
		MonoBBlock *bb = &cfg->bblocks [i];

		bb->gen_set = mono_bitset_alloc (cfg, max_vars, TRUE);
		bb->kill_set = mono_bitset_alloc (cfg, max_vars, TRUE);
		bb->live_in_set = mono_bitset_alloc (cfg, max_vars, TRUE);
		bb->live_out_set = mono_bitset_alloc (cfg, max_vars, TRUE);

		for (j = 0; j < bb->forest->len; j++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (bb->forest, j);

			update_tree_live_info (cfg, bb, t1);

			mono_bitset_clear (old_live_in_set, max_vars);
			update_gen_set (t1, old_live_in_set);
			mono_bitset_sub (old_live_in_set, bb->kill_set, max_vars);
			mono_bitset_add (bb->gen_set, old_live_in_set, max_vars);

			switch (t1->op) {
			case MB_TERM_REMOTE_STIND_I1:
			case MB_TERM_REMOTE_STIND_I2:
			case MB_TERM_REMOTE_STIND_I4:
			case MB_TERM_REMOTE_STIND_I8:
			case MB_TERM_REMOTE_STIND_R4:
			case MB_TERM_REMOTE_STIND_R8:
			case MB_TERM_REMOTE_STIND_REF:
			case MB_TERM_REMOTE_STIND_OBJ:
			case MB_TERM_STIND_I1:
			case MB_TERM_STIND_I2:
			case MB_TERM_STIND_I4:
			case MB_TERM_STIND_I8:
			case MB_TERM_STIND_R4:
			case MB_TERM_STIND_R8:
			case MB_TERM_STIND_REF:
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
			printf ("GEN  %d: ", i); mono_bitset_print (bb->gen_set, max_vars); printf ("\n");
			printf ("KILL %d: ", i); mono_bitset_print (bb->kill_set, max_vars); printf ("\n");
		}
#endif
	}
	
	do {
		changes = FALSE;
		for (i = 0; i < cfg->block_count; i++) {
			MonoBBlock *bb = &cfg->bblocks [i];

			mono_bitset_copy (old_live_in_set, bb->live_in_set, max_vars);
			mono_bitset_copy (old_live_out_set, bb->live_out_set, max_vars);

			mono_bitset_copy (bb->live_in_set, bb->live_out_set, max_vars);
			mono_bitset_sub (bb->live_in_set, bb->kill_set, max_vars);
			mono_bitset_add (bb->live_in_set, bb->gen_set, max_vars);

			mono_bitset_clear (bb->live_out_set, max_vars);
			
			for (l = bb->succ; l; l = l->next) {
				MonoBBlock *t = (MonoBBlock *)l->data;
				mono_bitset_add (bb->live_out_set, t->live_in_set, max_vars);
			}

			if (mono_bitset_cmp (old_live_in_set, bb->live_in_set, max_vars) ||
			    mono_bitset_cmp (old_live_out_set, bb->live_out_set, max_vars))
				changes = TRUE;
			    
		}

	} while (changes);

	g_free (old_live_in_set);
	g_free (old_live_out_set);


#ifdef DEGUG_LIVENESS
	if (debug) {
		for (i = 0; i < cfg->block_count; i++) {
			MonoBBlock *bb = &cfg->bblocks [i];
		
			printf ("LIVE IN  %d: ", i); 
			mono_bitset_print (bb->live_in_set, max_vars); 
			printf ("\n");
			printf ("LIVE OUT %d: ", i); 
			mono_bitset_print (bb->live_out_set, max_vars); 
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

