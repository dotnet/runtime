/*
 * liveness.c: liveness analysis
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include "mini.h"

GList *
mono_varlist_insert_sorted (MonoCompile *cfg, GList *list, MonoMethodVar *mv, int sort_type)
{
	GList *l;

	if (!list)
		return g_list_prepend (NULL, mv);

	for (l = list; l; l = l->next) {
		MonoMethodVar *v1 = l->data;
		
		if (sort_type == 2) {
			if (mv->spill_costs >= v1->spill_costs) {
				list = g_list_insert_before (list, l, mv);
				break;
			}			
		} else if (sort_type == 1) {
			if (mv->range.last_use.abs_pos <= v1->range.last_use.abs_pos) {
				list = g_list_insert_before (list, l, mv);
				break;
			}
		} else {
			if (mv->range.first_use.abs_pos <= v1->range.first_use.abs_pos) {
				list = g_list_insert_before (list, l, mv);
				break;
			}
		}
	}
	if (!l)
		list = g_list_append (list, mv);

	return list;
}

static gint 
compare_by_first_use_func (gconstpointer a, gconstpointer b)
{
	MonoMethodVar *v1 = (MonoMethodVar*)a;
	MonoMethodVar *v2 = (MonoMethodVar*)b;

	return v1->range.first_use.abs_pos - v2->range.first_use.abs_pos;
}

GList *
mono_varlist_sort (MonoCompile *cfg, GList *list, int sort_type)
{
	if (sort_type == 0)
		return g_list_sort (list, compare_by_first_use_func);
	else
		g_assert_not_reached ();

	return NULL;
}

//#define DEBUG_LSCAN

void
mono_linear_scan (MonoCompile *cfg, GList *vars, GList *regs, regmask_t *used_mask)
{
	GList *l, *a, *active = NULL;
	MonoMethodVar *vmv, *amv;
	int max_regs, gains [sizeof (regmask_t) * 8];
	regmask_t used_regs = 0;
	gboolean cost_driven;

	cost_driven = (cfg->comp_done & MONO_COMP_LOOPS);

#ifdef DEBUG_LSCAN
	printf ("Linears scan for %s\n", mono_method_full_name (cfg->method, TRUE));
#endif

#ifdef DEBUG_LSCAN
	for (l = vars; l; l = l->next) {
		vmv = l->data;
		printf ("VAR %d %08x %08x C%d\n", vmv->idx, vmv->range.first_use.abs_pos, 
			vmv->range.last_use.abs_pos, vmv->spill_costs);
	}
#endif
	max_regs = g_list_length (regs);

	for (l = regs; l; l = l->next) {
		int regnum = (int)l->data;
		g_assert (regnum < G_N_ELEMENTS (gains));
		gains [regnum] = 0;
	}

	/* linear scan */
	for (l = vars; l; l = l->next) {
		vmv = l->data;

#ifdef DEBUG_LSCAN
		printf ("START  %2d %08x %08x\n",  vmv->idx, vmv->range.first_use.abs_pos, 
			vmv->range.last_use.abs_pos);
#endif
		/* expire old intervals in active */
		while (active) {
			amv = (MonoMethodVar *)active->data;

			if (amv->range.last_use.abs_pos >= vmv->range.first_use.abs_pos)
				break;

#ifdef DEBUG_LSCAN
			printf ("EXPIR  %2d %08x %08x C%d R%d\n", amv->idx, amv->range.first_use.abs_pos, 
				amv->range.last_use.abs_pos, amv->spill_costs, amv->reg);
#endif
			active = g_list_delete_link (active, active);
			regs = g_list_prepend (regs, (gpointer)amv->reg);
			gains [amv->reg] += amv->spill_costs;
		}
		
		if (active && g_list_length (active) == max_regs) {
			/* Spill */

			a = g_list_nth (active, max_regs - 1);
			amv = (MonoMethodVar *)a->data;

			if ((cost_driven && amv->spill_costs < vmv->spill_costs) || 			     
			    (!cost_driven && amv->range.last_use.abs_pos > vmv->range.last_use.abs_pos)) {
				vmv->reg = amv->reg;
				amv->reg = -1;
				active = g_list_delete_link (active, a);

				if (cost_driven)
					active = mono_varlist_insert_sorted (cfg, active, vmv, 2);	
				else
					active = mono_varlist_insert_sorted (cfg, active, vmv, 1);	

#ifdef DEBUG_LSCAN
				printf ("SPILL0 %2d %08x %08x C%d\n",  amv->idx, 
					amv->range.first_use.abs_pos, amv->range.last_use.abs_pos,
					amv->spill_costs);
#endif
			} else {
#ifdef DEBUG_LSCAN
				printf ("SPILL1 %2d %08x %08x C%d\n",  vmv->idx, 
					vmv->range.first_use.abs_pos, vmv->range.last_use.abs_pos,
					vmv->spill_costs);
#endif
				vmv->reg = -1;
			}
		} else {
			/* assign register */

			g_assert (regs);

			vmv->reg = (int)regs->data;

			used_regs |= 1LL << vmv->reg;

			regs = g_list_delete_link (regs, regs);

#ifdef DEBUG_LSCAN
			printf ("ADD    %2d %08x %08x C%d R%d\n",  vmv->idx, 
				vmv->range.first_use.abs_pos, vmv->range.last_use.abs_pos, 
				vmv->spill_costs, vmv->reg);
#endif
			active = mono_varlist_insert_sorted (cfg, active, vmv, TRUE);		
		}


#ifdef DEBUG_LSCAN
		for (a = active; a; a = a->next) {
			amv = (MonoMethodVar *)a->data;		
			printf ("ACT    %2d %08x %08x C%d R%d\n", amv->idx, amv->range.first_use.abs_pos,  
				amv->range.last_use.abs_pos, amv->spill_costs, amv->reg);
		}
		printf ("NEXT\n");
#endif
	}	

	for (a = active; a; a = a->next) {
		amv = (MonoMethodVar *)a->data;		
		gains [amv->reg] += amv->spill_costs;
	}

	for (l = vars; l; l = l->next) {
		vmv = l->data;
		
		if (vmv->reg >= 0)  {
			if (gains [vmv->reg] > mono_arch_regalloc_cost (cfg, vmv)) {
				cfg->varinfo [vmv->idx]->opcode = OP_REGVAR;
				cfg->varinfo [vmv->idx]->dreg = vmv->reg;
				if (cfg->verbose_level > 2)
					printf ("REGVAR %d C%d R%d\n", vmv->idx, vmv->spill_costs, vmv->reg);
			} else
				vmv->reg = -1;
		}
	}

	/* Compute used regs */
	used_regs = 0;
	for (l = vars; l; l = l->next) {
		vmv = l->data;
		
		if (vmv->reg >= 0)
			used_regs |= 1LL << vmv->reg;
	}

	*used_mask |= used_regs;

	g_list_free (regs);
	g_list_free (active);
	g_list_free (vars);
}

