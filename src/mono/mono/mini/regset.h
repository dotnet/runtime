/*
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_JIT_REGSET_H_
#define _MONO_JIT_REGSET_H_

#include <glib.h>

typedef struct {
	int max_regs;
	guint32 free_mask;
	guint32 used_mask;
	guint32 reserved_mask;
} MonoRegSet;

MonoRegSet *
mono_regset_new         (int max_regs);

void
mono_regset_free        (MonoRegSet *rs);

int
mono_regset_alloc_reg   (MonoRegSet *rs, int regnum, guint32 exclude_mask);

void
mono_regset_free_reg    (MonoRegSet *rs, int regnum);

void
mono_regset_reserve_reg (MonoRegSet *rs, int regnum);

gboolean
mono_regset_reg_used    (MonoRegSet *rs, int regnum);

#endif
