/**
 * \file
 * gsharedvt support code for loongarch64.
 *
 * Authors:
 *   Qiao Pengcheng (qiaopengcheng@loongson.cn), Liu An(liuan@loongson.cn)
 *
 * Copyright (c) 2021 Loongson Technology, Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "mini.h"
#include "mini-loongarch64.h"
#include "mini-loongarch64-gsharedvt.h"
#include "aot-runtime.h"

/*
 * GSHAREDVT
 */
#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED

void
mono_loongarch_gsharedvt_init (void)
{
}

gboolean
mono_arch_gsharedvt_sig_supported (MonoMethodSignature *sig)
{
	/*
	if (sig->ret && is_variable_size (sig->ret))
		return FALSE;
	*/
	return TRUE;
}

static void
add_to_map (GPtrArray *map, int src, int dst)
{
	g_ptr_array_add (map, GUINT_TO_POINTER (src));
	g_ptr_array_add (map, GUINT_TO_POINTER (dst));
}

/*
 * Slot mapping:
 * 0..8  - r0..r8
 * 9..16 - d0..d7
 * 17..  - stack slots
 */

static int
map_reg (int reg)
{
	return reg;
}

static int
map_freg (int reg)
{
	return reg + NUM_GSHAREDVT_ARG_GREGS;
}

static int
map_stack_slot (int slot)
{
	return slot + NUM_GSHAREDVT_ARG_GREGS + NUM_GSHAREDVT_ARG_FREGS;
}

static int
get_arg_slots (ArgInfo *ainfo, int **out_slots)
{
	return 0;
}

/*
 * mono_arch_get_gsharedvt_call_info:
 *
 *   See mini-x86.c for documentation.
 */
gpointer
mono_arch_get_gsharedvt_call_info (MonoMemoryManager *mem_manager, gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	return NULL;
}

#else

void
mono_loongarch_gsharedvt_init (void)
{
}

#endif /* MONO_ARCH_GSHAREDVT_SUPPORTED */
