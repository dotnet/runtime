/**
 * \file
 * liveness analysis
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include "mini.h"
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_JIT

static void mono_linear_scan2 (MonoCompile *cfg, GList *vars, GList *regs, regmask_t *used_mask);

GList *
mono_varlist_insert_sorted (MonoCompile *cfg, GList *list, MonoMethodVar *mv, int sort_type)
{
	GList *l;

	if (!list)
		return g_list_prepend (NULL, mv);

	for (l = list; l; l = l->next) {
		MonoMethodVar *v1 = (MonoMethodVar *)l->data;

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

// #define DEBUG_LSCAN

void
mono_linear_scan (MonoCompile *cfg, GList *vars, GList *regs, regmask_t *used_mask)
{
	GList *l, *a, *active = NULL;
	MonoMethodVar *vmv, *amv;
	int max_regs, n_regvars;
	int gains [sizeof (regmask_t) * 8];
	regmask_t used_regs = 0;
	gboolean cost_driven;

	if (!cfg->disable_reuse_registers && vars && (((MonoMethodVar*)vars->data)->interval != NULL)) {
		mono_linear_scan2 (cfg, vars, regs, used_mask);
		g_list_free (regs);
		g_list_free (vars);
		return;
	}

	cost_driven = TRUE;

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
		int regnum = GPOINTER_TO_INT (l->data);
		g_assert (regnum < G_N_ELEMENTS (gains));
		gains [regnum] = 0;
	}

	/* linear scan */
	for (l = vars; l; l = l->next) {
		vmv = (MonoMethodVar *)l->data;

#ifdef DEBUG_LSCAN
		printf ("START  %2d %08x %08x\n",  vmv->idx, vmv->range.first_use.abs_pos,
			vmv->range.last_use.abs_pos);
#endif
		/* expire old intervals in active */
		if (!cfg->disable_reuse_registers) {
			while (active) {
				amv = (MonoMethodVar *)active->data;

				if (amv->range.last_use.abs_pos > vmv->range.first_use.abs_pos)
					break;

#ifdef DEBUG_LSCAN
				printf ("EXPIR  %2d %08x %08x C%d R%d\n", amv->idx, amv->range.first_use.abs_pos,
						amv->range.last_use.abs_pos, amv->spill_costs, amv->reg);
#endif
				active = g_list_delete_link (active, active);
				regs = g_list_prepend (regs, GINT_TO_POINTER (amv->reg));
				gains [amv->reg] += amv->spill_costs;
			}
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

			vmv->reg = GPOINTER_TO_INT (regs->data);

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

	n_regvars = 0;
	for (l = vars; l; l = l->next) {
		vmv = (MonoMethodVar *)l->data;

		if (vmv->reg >= 0)  {
			if ((GINT_TO_UINT32(gains [vmv->reg]) > mono_arch_regalloc_cost (cfg, vmv)) && (cfg->varinfo [vmv->idx]->opcode != OP_REGVAR)) {
				if (cfg->verbose_level > 2) {
					printf ("ALLOCATED R%d(%d) TO HREG %d COST %d\n", cfg->varinfo [vmv->idx]->dreg, vmv->idx, vmv->reg, vmv->spill_costs);
				}
				cfg->varinfo [vmv->idx]->opcode = OP_REGVAR;
				cfg->varinfo [vmv->idx]->dreg = vmv->reg;
				n_regvars ++;
			} else {
				if (cfg->verbose_level > 2)
					printf ("COSTLY: R%d C%d C%d %s\n", vmv->idx, vmv->spill_costs, mono_arch_regalloc_cost (cfg, vmv), mono_arch_regname (vmv->reg));
				vmv->reg = -1;
			}
		}

		if (vmv->reg == -1) {
			if (cfg->verbose_level > 2)
				printf ("NOT REGVAR: %d\n", vmv->idx);
		}
	}

	cfg->stat_n_regvars = n_regvars;

	/* Compute used regs */
	used_regs = 0;
	for (l = vars; l; l = l->next) {
		vmv = (MonoMethodVar *)l->data;

		if (vmv->reg >= 0)
			used_regs |= 1LL << vmv->reg;
	}

	*used_mask |= used_regs;

#ifdef DEBUG_LSCAN
	if (cfg->verbose_level > 2)
		printf ("EXIT: final used mask: %08x\n", *used_mask);
#endif

	g_list_free (regs);
	g_list_free (active);
	g_list_free (vars);
}

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

/* FIXME: This is x86 only */
static guint32
regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	MonoInst *ins = cfg->varinfo [vmv->idx];

	/* Load if it is an argument */
	return (ins->opcode == OP_ARG) ? 1 : 0;
}

void
mono_linear_scan2 (MonoCompile *cfg, GList *vars, GList *regs, regmask_t *used_mask)
{
	GList *unhandled, *active, *inactive, *l;
	MonoMethodVar *vmv;
	gint32 free_pos [sizeof (regmask_t) * 8];
	gint32 gains [sizeof (regmask_t) * 8];
	regmask_t used_regs = 0;
	int n_regs, n_regvars, i;

	for (l = vars; l; l = l->next) {
		vmv = (MonoMethodVar *)l->data;
		LSCAN_DEBUG (printf ("VAR R%d %08x %08x C%d\n", cfg->varinfo [vmv->idx]->dreg, vmv->range.first_use.abs_pos,
							 vmv->range.last_use.abs_pos, vmv->spill_costs));
	}

	LSCAN_DEBUG (printf ("Linear Scan 2 for %s:\n", mono_method_full_name (cfg->method, TRUE)));

	n_regs = g_list_length (regs);
	memset (gains, 0, n_regs * sizeof (gint32));
	unhandled = g_list_sort (g_list_copy (vars), compare_by_interval_start_pos_func);
	active = NULL;
	inactive = NULL;

	while (unhandled) {
		MonoMethodVar *current = (MonoMethodVar *)unhandled->data;
		int pos, reg, max_free_pos;
		gboolean changed;

		unhandled = g_list_delete_link (unhandled, unhandled);

		LSCAN_DEBUG (printf ("Processing R%d: ", cfg->varinfo [current->idx]->dreg));
		LSCAN_DEBUG (mono_linterval_print (current->interval));
		LSCAN_DEBUG (printf ("\n"));

		if (!current->interval->range)
			continue;

		pos = current->interval->range->from;

		/* Check for intervals in active which expired or inactive */
		changed = TRUE;
		/* FIXME: Optimize this */
		while (changed) {
			changed = FALSE;
			for (l = active; l != NULL; l = l->next) {
				MonoMethodVar *v = (MonoMethodVar*)l->data;

				if (v->interval->last_range->to < pos) {
					active = g_list_delete_link (active, l);
					LSCAN_DEBUG (printf ("Interval R%d has expired\n", cfg->varinfo [v->idx]->dreg));
					changed = TRUE;
					break;
				}
				else if (!mono_linterval_covers (v->interval, pos)) {
					inactive = g_list_append (inactive, v);
					active = g_list_delete_link (active, l);
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
			for (l = inactive; l != NULL; l = l->next) {
				MonoMethodVar *v = (MonoMethodVar*)l->data;

				if (v->interval->last_range->to < pos) {
					inactive = g_list_delete_link (inactive, l);
					LSCAN_DEBUG (printf ("\tInterval R%d has expired\n", cfg->varinfo [v->idx]->dreg));
					changed = TRUE;
					break;
				}
				else if (mono_linterval_covers (v->interval, pos)) {
					active = g_list_append (active, v);
					inactive = g_list_delete_link (inactive, l);
					LSCAN_DEBUG (printf ("\tInterval R%d became active\n", cfg->varinfo [v->idx]->dreg));
					changed = TRUE;
					break;
				}
			}
		}

		/* Find a register for the current interval */
		for (i = 0; i < n_regs; ++i)
			free_pos [i] = ((gint32)0x7fffffff);

		for (l = active; l != NULL; l = l->next) {
			MonoMethodVar *v = (MonoMethodVar*)l->data;

			if (v->reg >= 0) {
				free_pos [v->reg] = 0;
				LSCAN_DEBUG (printf ("\threg %d is busy (cost %d)\n", v->reg, v->spill_costs));
			}
		}

		for (l = inactive; l != NULL; l = l->next) {
			MonoMethodVar *v = (MonoMethodVar*)l->data;
			gint32 intersect_pos;

			if (v->reg >= 0) {
				intersect_pos = mono_linterval_get_intersect_pos (current->interval, v->interval);
				if (intersect_pos != -1) {
					free_pos [v->reg] = intersect_pos;
					LSCAN_DEBUG (printf ("\threg %d becomes free at %d\n", v->reg, intersect_pos));
				}
			}
		}

		max_free_pos = -1;
		reg = -1;
		for (i = 0; i < n_regs; ++i)
			if (free_pos [i] > max_free_pos) {
				reg = i;
				max_free_pos = free_pos [i];
			}

		g_assert (reg != -1);

		if (free_pos [reg] >= current->interval->last_range->to) {
			/* Register available for whole interval */
			current->reg = reg;
			LSCAN_DEBUG (printf ("\tAssigned hreg %d to R%d\n", reg, cfg->varinfo [current->idx]->dreg));

			active = g_list_append (active, current);
			gains [current->reg] += current->spill_costs;
		}
		else {
			/*
			 * free_pos [reg] > 0 means there is a register available for parts
			 * of the interval, so splitting it is possible. This is not yet
			 * supported, so we spill in this case too.
			 */

			/* Spill an interval */

			/* FIXME: Optimize the selection of the interval */

			if (active) {
				GList *min_spill_pos;
#if 0
				/*
				 * This favors registers with big spill costs, thus larger liveness ranges,
				 * thus actually leading to worse code size.
				 */
				guint32 min_spill_value = G_MAXINT32;

				for (l = active; l != NULL; l = l->next) {
					vmv = (MonoMethodVar*)l->data;

					if (vmv->spill_costs < min_spill_value) {
						min_spill_pos = l;
						min_spill_value = vmv->spill_costs;
					}
				}
#else
				/* Spill either the first active or the current interval */
				min_spill_pos = active;
#endif
				vmv = (MonoMethodVar*)min_spill_pos->data;
				if (vmv->spill_costs < current->spill_costs) {
				//				if (vmv->interval->last_range->to < current->interval->last_range->to) {
					gains [vmv->reg] -= vmv->spill_costs;
					vmv->reg = -1;
					LSCAN_DEBUG (printf ("\tSpilled R%d\n", cfg->varinfo [vmv->idx]->dreg));
					active = g_list_delete_link (active, min_spill_pos);
				}
				else
					LSCAN_DEBUG (printf ("\tSpilled current (cost %d)\n", current->spill_costs));
			}
			else
				LSCAN_DEBUG (printf ("\tSpilled current\n"));
		}
	}

	/* Decrease the gains by the cost of saving+restoring the register */
	for (i = 0; i < n_regs; ++i) {
		if (gains [i]) {
			/* FIXME: This is x86 only */
			gains [i] -= cfg->method->save_lmf ? 1 : 2;
			if (gains [i] < 0)
				gains [i] = 0;
		}
	}

	/* Do the actual register assignment */
	n_regvars = 0;
	for (l = vars; l; l = l->next) {
		vmv = (MonoMethodVar *)l->data;

		if (vmv->reg >= 0) {
			int reg_index = vmv->reg;

			/* During allocation, vmv->reg is an index into the regs list */
			vmv->reg = GPOINTER_TO_INT (g_list_nth_data (regs, vmv->reg));

			if ((GINT_TO_UINT32(gains [reg_index]) > regalloc_cost (cfg, vmv)) && (cfg->varinfo [vmv->idx]->opcode != OP_REGVAR)) {
				if (cfg->verbose_level > 2)
					printf ("REGVAR R%d G%d C%d %s\n", cfg->varinfo [vmv->idx]->dreg, gains [reg_index], regalloc_cost (cfg, vmv), mono_arch_regname (vmv->reg));
				cfg->varinfo [vmv->idx]->opcode = OP_REGVAR;
				cfg->varinfo [vmv->idx]->dreg = vmv->reg;
				n_regvars ++;
			}
			else {
				if (cfg->verbose_level > 2)
					printf ("COSTLY: %s R%d G%d C%d %s\n", mono_method_full_name (cfg->method, TRUE), cfg->varinfo [vmv->idx]->dreg, gains [reg_index], regalloc_cost (cfg, vmv), mono_arch_regname (vmv->reg));
				vmv->reg = -1;
			}
		}
	}

	cfg->stat_n_regvars = n_regvars;

	/* Compute used regs */
	used_regs = 0;
	for (l = vars; l; l = l->next) {
		vmv = (MonoMethodVar *)l->data;

		if (vmv->reg >= 0)
			used_regs |= 1LL << vmv->reg;
	}

	*used_mask |= used_regs;

	g_list_free (active);
	g_list_free (inactive);
}

#else /* !DISABLE_JIT */

MONO_EMPTY_SOURCE_FILE (linear_scan);

#endif /* !DISABLE_JIT */
