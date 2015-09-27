#include "mini.h"

/* Dummy versions of some arch specific functions to avoid ifdefs at call sites */

#ifndef MONO_ARCH_GSHAREDVT_SUPPORTED

gboolean
mono_arch_gsharedvt_sig_supported (MonoMethodSignature *sig)
{
	return FALSE;
}

gpointer
mono_arch_get_gsharedvt_call_info (gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_gsharedvt_arg_trampoline (MonoDomain *domain, gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif

#if defined(MONO_ARCH_GSHAREDVT_SUPPORTED) && !defined(ENABLE_GSHAREDVT)

gboolean
mono_arch_gsharedvt_sig_supported (MonoMethodSignature *sig)
{
	return FALSE;
}

gpointer
mono_arch_get_gsharedvt_call_info (gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	NOT_IMPLEMENTED;
	return NULL;
}

#endif

#ifndef MONO_ARCH_HAVE_DECOMPOSE_OPTS
void
mono_arch_decompose_opts (MonoCompile *cfg, MonoInst *ins)
{
}
#endif

#ifndef MONO_ARCH_HAVE_OPCODE_NEEDS_EMULATION
gboolean
mono_arch_opcode_needs_emulation (MonoCompile *cfg, int opcode)
{
	return FALSE;
}
#endif

#ifndef MONO_ARCH_HAVE_DECOMPOSE_LONG_OPTS
void
mono_arch_decompose_long_opts (MonoCompile *cfg, MonoInst *ins)
{
}
#endif
