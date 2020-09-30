#include "mini.h"
#include "interp/interp.h"

void mono_sdb_single_step_trampoline (void);

static void
mono_wasm_specific_trampoline (void)
{
	g_error (__func__);
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoMemoryManager *mem_manager, guint32 *code_len)
{
	return (gpointer)mono_wasm_specific_trampoline;
}

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	g_error (__func__);
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	g_error (__func__);
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr)
{
	g_error (__func__);
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
	g_error ("mono_arch_patch_callsite");
}

gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	g_error (__func__);
	return NULL;
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	g_error (__func__);
	return NULL;
}

static void
interp_to_native_trampoline (void *target_func, InterpMethodArguments *margs)
{
	// Unused on wasm
	g_assert_not_reached ();
}

gpointer
mono_arch_get_interp_to_native_trampoline (MonoTrampInfo **info)
{
	if (info)
		*info = mono_tramp_info_create ("interp_to_native_trampoline", (guint8*)interp_to_native_trampoline, 1, NULL, NULL);
	return (gpointer)interp_to_native_trampoline;
}

guint8*
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	g_assert (!aot);
	const char *name;
	guint8* code;
	if (single_step) {
		name = "sdb_single_step_trampoline";
		code = (guint8*)mono_wasm_single_step_hit;
	} else {
		name = "sdb_breakpoint_trampoline";
		code = (guint8*)mono_wasm_breakpoint_hit;
	}

	if (info)
		*info = mono_tramp_info_create (name, code, 1, NULL, NULL);
	return code;
}

guint8*
mono_arch_get_call_target (guint8 *code)
{
	g_error ("mono_arch_get_call_target");
}

/*
 * mono_arch_get_plt_info_offset:
 *
 *   Return the PLT info offset belonging to the plt entry PLT_ENTRY.
 */
guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, host_mgreg_t *regs, guint8 *code)
{
	g_error (__func__);
	return *(guint32*)(plt_entry + 6);
}

gpointer
mono_arch_get_gsharedvt_arg_trampoline (MonoDomain *domain, gpointer arg, gpointer addr)
{
	g_error (__func__);
	return NULL;
}

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	g_error (__func__);
	return NULL;
}
