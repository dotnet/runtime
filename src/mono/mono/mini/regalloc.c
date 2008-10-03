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
	g_free (rs->vassign);
	g_free (rs);
}

void
mono_regstate_reset (MonoRegState *rs) {
	rs->next_vreg = MAX (MONO_MAX_IREGS, MONO_MAX_FREGS);
#ifdef MONO_ARCH_NEED_SIMD_BANK
	rs->next_vreg = MAX (rs->next_vreg, MONO_MAX_XREGS);
#endif
}

inline int
mono_regstate_next_long (MonoRegState *rs)
{
	int rval = rs->next_vreg;

	rs->next_vreg += 2;

	return rval;
}

