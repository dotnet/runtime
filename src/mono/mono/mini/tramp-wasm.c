#include "mini.h"
#include "interp/interp.h"

void mono_wasm_interp_to_native_trampoline (void *target_func, InterpMethodArguments *margs);
void mono_sdb_single_step_trampoline (void);

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	g_error ("mono_arch_create_specific_trampoline");
}

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	g_error ("mono_arch_create_generic_trampoline");
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	g_error ("mono_arch_create_rgctx_lazy_fetch_trampoline");
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, mgreg_t *regs, guint8 *addr)
{
	g_error ("mono_arch_patch_plt_entry");
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
	g_error ("mono_arch_patch_callsite");
}

gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	g_error ("mono_arch_get_unbox_trampoline");
	return NULL;
}

gpointer
mono_arch_get_static_rgctx_trampoline (gpointer arg, gpointer addr)
{
	g_error ("mono_arch_get_static_rgctx_trampoline");
	return NULL;
}

gpointer
mono_arch_get_interp_to_native_trampoline (MonoTrampInfo **info)
{
	if (info)
		*info = mono_tramp_info_create ("interp_to_native_trampoline", mono_wasm_interp_to_native_trampoline, 1, NULL, NULL);
	return mono_wasm_interp_to_native_trampoline;
}

guint8*
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	g_assert (!aot);
	const char *name;
	gpointer code;
	if (single_step) {
		name = "sdb_single_step_trampoline";
		code = mono_sdb_single_step_trampoline;
	} else {
		name = "sdb_breakpoint_trampoline";
		code = mono_wasm_breakpoint_hit;
	}

	if (info)
		*info = mono_tramp_info_create (name, code, 1, NULL, NULL);
	return code;
}
