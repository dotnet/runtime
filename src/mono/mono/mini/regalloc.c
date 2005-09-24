/*
 * regalloc.c: register state class
 *
 * Authors:
 *    Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"

MonoRegState*
mono_regstate_new (void)
{
	MonoRegState* rs = g_new0 (MonoRegState, 1);

	mono_regstate_reset (rs);

	return rs;
}

void
mono_regstate_free (MonoRegState *rs) {
	g_free (rs->iassign);
	g_free (rs->fassign);
	g_free (rs);
}

void
mono_regstate_reset (MonoRegState *rs) {
	rs->next_vireg = MONO_MAX_IREGS;
	rs->next_vfreg = MONO_MAX_FREGS;
}

void
mono_regstate_assign (MonoRegState *rs) {
	int i;
	rs->max_ireg = -1;
	g_free (rs->iassign);
	rs->iassign = g_malloc (MAX (MONO_MAX_IREGS, rs->next_vireg) * sizeof (int));
	for (i = 0; i < MONO_MAX_IREGS; ++i) {
		rs->iassign [i] = i;
		rs->isymbolic [i] = 0;
	}
	for (; i < rs->next_vireg; ++i)
		rs->iassign [i] = -1;

	g_free (rs->fassign);
	rs->fassign = g_malloc (MAX (MONO_MAX_FREGS, rs->next_vfreg) * sizeof (int));
	for (i = 0; i < MONO_MAX_FREGS; ++i) {
		rs->fassign [i] = i;
		rs->fsymbolic [i] = 0;
	}
	for (; i < rs->next_vfreg; ++i)
		rs->fassign [i] = -1;
}

int
mono_regstate_alloc_int (MonoRegState *rs, regmask_t allow)
{
	int i;
	regmask_t mask = allow & rs->ifree_mask;
	for (i = 0; i < MONO_MAX_IREGS; ++i) {
		if (mask & (1 << i)) {
			rs->ifree_mask &= ~ (1 << i);
			rs->max_ireg = MAX (rs->max_ireg, i);
			return i;
		}
	}
	return -1;
}

void
mono_regstate_free_int (MonoRegState *rs, int reg)
{
	if (reg >= 0) {
		rs->ifree_mask |= 1 << reg;
		rs->isymbolic [reg] = 0;
	}
}

int
mono_regstate_alloc_float (MonoRegState *rs, regmask_t allow)
{
	int i;
	regmask_t mask = allow & rs->ffree_mask;
	for (i = 0; i < MONO_MAX_FREGS; ++i) {
		if (mask & (1 << i)) {
			rs->ffree_mask &= ~ (1 << i);
			return i;
		}
	}
	return -1;
}

void
mono_regstate_free_float (MonoRegState *rs, int reg)
{
	if (reg >= 0) {
		rs->ffree_mask |= 1 << reg;
		rs->fsymbolic [reg] = 0;
	}
}

inline int
mono_regstate_next_long (MonoRegState *rs)
{
	int rval = rs->next_vireg;

	rs->next_vireg += 2;

	return rval;
}

