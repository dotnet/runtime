#include "regset.h"
#include <mono/arch/x86/x86-codegen.h>

MonoRegSet *
mono_regset_new (int max_regs)
{
	MonoRegSet *rs;

	g_return_val_if_fail (max_regs > 0 && max_regs <= 32, NULL);

	rs = g_new0 (MonoRegSet, 1);

	rs->max_regs = max_regs;
	rs->used_mask = 0;
	rs->free_mask = ~rs->used_mask;
	rs->reserved_mask = 0;

	return rs;
}

void
mono_regset_free (MonoRegSet *rs)
{
	g_free (rs);
}

void
mono_regset_reserve_reg (MonoRegSet *rs, int regnum)
{
	guint32 ind;

	g_return_if_fail (rs != NULL);
	g_return_if_fail (rs->max_regs > regnum);

	ind = 1 << regnum;

	rs->reserved_mask |= ind;
}

int
mono_regset_alloc_reg (MonoRegSet *rs, int regnum, gboolean exclude_edx)
{
	guint32 i, ind;

	g_return_val_if_fail (rs != NULL, -1);
	g_return_val_if_fail (rs->max_regs > regnum, -1);

	if (regnum < 0) {
		for (i = 0, ind = 1; i < rs->max_regs; i++, ind = ind << 1) {
			if (exclude_edx && i == X86_EDX)
				continue;
			if ((rs->free_mask & ind) && !(rs->reserved_mask & ind)) {
				rs->free_mask &= ~ind;
				rs->used_mask |= ind;
				return i;
			}
		}
		return -1;
	} else {
		if (exclude_edx && regnum == X86_EDX)
			return -1;

		ind = 1 << regnum;
		if ((rs->free_mask & ind) && !(rs->reserved_mask & ind)) {
			rs->free_mask &= ~ind;
			rs->used_mask |= ind;
			return regnum;
		}
		return -1;
	}
}

void
mono_regset_free_reg (MonoRegSet *rs, int regnum)
{
	guint32 ind;

	g_return_if_fail (rs != NULL);
	g_return_if_fail (rs->max_regs > regnum);

	if (regnum < 0)
		return;

	ind = 1 << regnum;

	g_return_if_fail (rs->free_mask && ind);

	rs->free_mask |= ind;
}

gboolean
mono_regset_reg_used (MonoRegSet *rs, int regnum)
{
	guint32 ind;

	g_return_val_if_fail (rs != NULL, FALSE);
	g_return_val_if_fail (rs->max_regs > regnum, FALSE);
	g_return_val_if_fail (regnum >= 0, FALSE);

	ind = 1 << regnum;

	return rs->used_mask & ind;
}

