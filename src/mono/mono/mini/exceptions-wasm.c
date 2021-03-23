#include "mini-runtime.h"
#include "mini.h"

static void
wasm_restore_context (void)
{
	g_error ("wasm_restore_context");
}

static void
wasm_call_filter (void)
{
	g_error ("wasm_call_filter");
}

static void
wasm_throw_exception (void)
{
	g_error ("wasm_throw_exception");
}

static void
wasm_rethrow_exception (void)
{
	g_error ("wasm_rethrow_exception");
}

static void
wasm_rethrow_preserve_exception (void)
{
	g_error ("wasm_rethrow_preserve_exception");
}

static void
wasm_throw_corlib_exception (void)
{
	g_error ("wasm_throw_corlib_exception");
}

gboolean
mono_arch_unwind_frame (MonoJitTlsData *jit_tls, 
						MonoJitInfo *ji, MonoContext *ctx,
						MonoContext *new_ctx, MonoLMF **lmf,
						host_mgreg_t **save_locations,
						StackFrameInfo *frame)
{
	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;

	*new_ctx = *ctx;

	g_assert (!ji);

	/*
	 * Can't unwind native frames on WASM, so we only process the ones
	 * which push an LMF frame. See the needs_stack_walk code in
	 * method-to-ir.c.
	 */
	if (*lmf) {
		ERROR_DECL (error);

		if (*lmf == jit_tls->first_lmf)
			return FALSE;

		/* This will compute the original method address */
		g_assert ((*lmf)->method);
		gpointer addr = mono_compile_method_checked ((*lmf)->method, error);
		mono_error_assert_ok (error);

		ji = mini_jit_info_table_find (addr);
		g_assert (ji);

		frame->type = FRAME_TYPE_MANAGED;
		frame->ji = ji;
		frame->actual_method = (*lmf)->method;

		*lmf = (MonoLMF *)(((guint64)(*lmf)->previous_lmf) & ~3);
		return TRUE;
	}

	return FALSE;
}

gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
	if (info)
		*info = mono_tramp_info_create ("call_filter", (guint8*)wasm_call_filter, 1, NULL, NULL);
	return (gpointer)wasm_call_filter;
}

gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
	if (info)
		*info = mono_tramp_info_create ("restore_context", (guint8*)wasm_restore_context, 1, NULL, NULL);
	return (gpointer)wasm_restore_context;
}
gpointer 
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	if (info)
		*info = mono_tramp_info_create ("throw_corlib_exception", (guint8*)wasm_throw_corlib_exception, 1, NULL, NULL);
	return (gpointer)wasm_throw_corlib_exception;
}

gpointer
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	if (info)
		*info = mono_tramp_info_create ("rethrow_exception", (guint8*)wasm_rethrow_exception, 1, NULL, NULL);
	return (gpointer)wasm_rethrow_exception;
}

gpointer
mono_arch_get_rethrow_preserve_exception (MonoTrampInfo **info, gboolean aot)
{
	if (info)
		*info = mono_tramp_info_create ("rethrow_preserve_exception", (guint8*)wasm_rethrow_preserve_exception, 1, NULL, NULL);
	return (gpointer)wasm_rethrow_exception;
}

gpointer
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	if (info)
		*info = mono_tramp_info_create ("throw_exception", (guint8*)wasm_throw_exception, 1, NULL, NULL);
	return (gpointer)wasm_throw_exception;
}

void
mono_arch_undo_ip_adjustment (MonoContext *ctx)
{
}

gboolean
mono_arch_handle_exception (void *sigctx, gpointer obj)
{
	g_error ("mono_arch_handle_exception");
}
