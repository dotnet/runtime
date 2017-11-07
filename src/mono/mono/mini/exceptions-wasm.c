#include "mini.h"


gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
	g_error ("mono_arch_get_call_filter");
	return NULL;
}

gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
	g_error ("mono_arch_get_restore_context");
	return NULL;
}

gboolean
mono_arch_unwind_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, 
							 MonoJitInfo *ji, MonoContext *ctx, 
							 MonoContext *new_ctx, MonoLMF **lmf,
							 mgreg_t **save_locations,
							 StackFrameInfo *frame)
{
	g_error ("mono_arch_unwind_frame");
	return FALSE;
}

gpointer 
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	g_error ("mono_arch_get_throw_corlib_exception");
	return NULL;
}

gpointer
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	g_error ("mono_arch_get_rethrow_exception");
	return NULL;
}

gpointer
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	g_error ("mono_arch_get_rethrow_exception");
	return NULL;
}
