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
mono_arch_unwind_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, 
							 MonoJitInfo *ji, MonoContext *ctx, 
							 MonoContext *new_ctx, MonoLMF **lmf,
							 host_mgreg_t **save_locations,
							 StackFrameInfo *frame)
{
	if (ji)
		g_error ("Can't unwind compiled code");

	if (*lmf) {
		if ((*lmf)->top_entry)
			return FALSE;
		g_error ("Can't handle non-top-entry LMFs\n");
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
