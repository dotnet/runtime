#include "mini.h"


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

gpointer
mono_arch_get_enter_icall_trampoline (MonoTrampInfo **info)
{
	printf ("mono_arch_get_enter_icall_trampoline");
	g_assert (0);
	return NULL;
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