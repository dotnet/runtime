/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

#include "mini-runtime.h"

#include <mono/metadata/abi-details.h>
#include <mono/utils/mono-sigcontext.h>
#include "mono/utils/mono-tls-inline.h"

#ifndef DISABLE_JIT

static gpointer
nop_stub (void)
{
	guint8 *code, *start;

	start = code = mono_global_codeman_reserve (0x50);

	/* nop */
	riscv_addi (code, RISCV_X0, RISCV_X0, 0);

	mono_arch_flush_icache (start, code - start);

	return start;
}

gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
	*info = NULL;
	return nop_stub ();
}

gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
	*info = NULL;
	return nop_stub ();
}

gpointer
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	*info = NULL;
	return nop_stub ();
}

gpointer
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	*info = NULL;
	return nop_stub ();
}

gpointer
mono_arch_get_rethrow_preserve_exception (MonoTrampInfo **info, gboolean aot)
{
	*info = NULL;
	return nop_stub ();
}

gpointer
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	*info = NULL;
	return nop_stub ();
}

#else

gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
    g_assert_not_reached ();
    return NULL;
}

gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
    g_assert_not_reached ();
    return NULL;
}

gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
    g_assert_not_reached ();
    return NULL;
}

gpointer
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
    g_assert_not_reached ();
    return NULL;
}

gpointer
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
    g_assert_not_reached ();
    return NULL;
}

gpointer
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif

void
mono_arch_exceptions_init (void)
{
	// NOT_IMPLEMENTED;
}

gboolean
mono_arch_unwind_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *ji,
                        MonoContext *ctx, MonoContext *new_ctx, MonoLMF **lmf,
                        host_mgreg_t **save_locations, StackFrameInfo *frame)
{
	NOT_IMPLEMENTED;
	return FALSE;
}

static void
handle_signal_exception (gpointer obj)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoContext ctx = jit_tls->ex_ctx;

	mono_handle_exception (&ctx, obj);
	mono_restore_context (&ctx);
}

gboolean
mono_arch_handle_exception (void *ctx, gpointer obj)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();

	mono_sigctx_to_monoctx (ctx, &jit_tls->ex_ctx);

	// Call handle_signal_exception () on the normal stack.
	UCONTEXT_GREGS (ctx) [RISCV_A0] = (long) obj;
	UCONTEXT_REG_PC (ctx) = (long) handle_signal_exception;

	return TRUE;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	return (gpointer) UCONTEXT_REG_PC (sigctx);
}

void
mono_arch_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data)
{
	// Allocate a stack frame and redirect PC.
	MONO_CONTEXT_SET_SP (ctx, (host_mgreg_t) MONO_CONTEXT_GET_SP (ctx) - 32);

	mono_arch_setup_resume_sighandler_ctx (ctx, async_cb);
}

void
mono_arch_setup_resume_sighandler_ctx (MonoContext *ctx, gpointer func)
{
	MONO_CONTEXT_SET_IP (ctx, func);
}

void
mono_arch_undo_ip_adjustment (MonoContext *context)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_do_ip_adjustment (MonoContext *context)
{
	g_assert_not_reached ();
}
