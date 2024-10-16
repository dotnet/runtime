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
#include <mono/metadata/abi-details.h>

#include "mini.h"
#include "mini-loongarch64.h"
#include "mini-loongarch64-gsharedvt.h"
#include "mini-runtime.h"

/*
 * GSHAREDVT
 */
#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED

/*
 * mono_arch_get_gsharedvt_arg_trampoline:
 *
 *   See tramp-x86.c for documentation.
 */
gpointer
mono_arch_get_gsharedvt_arg_trampoline (gpointer arg, gpointer addr)
{
	return NULL;
}

gpointer
mono_loongarch_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg)
{
	return NULL;
}

#ifndef DISABLE_JIT

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	return NULL;
}

#else

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif

#endif /* MONO_ARCH_GSHAREDVT_SUPPORTED */
