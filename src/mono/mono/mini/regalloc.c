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

	rs->next_vreg = MAX (MONO_MAX_IREGS, MONO_MAX_FREGS);
#ifdef MONO_ARCH_NEED_SIMD_BANK
	rs->next_vreg = MAX (rs->next_vreg, MONO_MAX_XREGS);
#endif

	return rs;
}

void
mono_regstate_free (MonoRegState *rs) {
	g_free (rs->vassign);
	g_free (rs);
}
