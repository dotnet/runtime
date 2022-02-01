/**
 * \file
 */

#include "mini.h"

/* Dummy versions of some arch specific functions to avoid ifdefs at call sites */

#ifndef MONO_ARCH_GSHAREDVT_SUPPORTED

gboolean
mono_arch_gsharedvt_sig_supported (MonoMethodSignature *sig)
{
	return FALSE;
}

gpointer
mono_arch_get_gsharedvt_call_info (MonoMemoryManager *mem_manager, gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_gsharedvt_arg_trampoline (gpointer arg, gpointer addr)
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
	return TRUE;
}
#endif

#ifndef MONO_ARCH_HAVE_DECOMPOSE_LONG_OPTS
void
mono_arch_decompose_long_opts (MonoCompile *cfg, MonoInst *ins)
{
}
#endif

#ifndef MONO_ARCH_INTERPRETER_SUPPORTED

gpointer
mono_arch_get_interp_to_native_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_native_to_interp_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_arch_undo_ip_adjustment (MonoContext *context)
{
	g_assert_not_reached ();
}

void
mono_arch_do_ip_adjustment (MonoContext *context)
{
	g_assert_not_reached ();
}

#endif

#ifndef MONO_ARCH_HAVE_EXCEPTIONS_INIT

void
mono_arch_exceptions_init (void)
{
}

#endif

#if defined (DISABLE_JIT) && !defined (HOST_WASM)
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
mono_arch_get_rethrow_preserve_exception (MonoTrampInfo **info, gboolean aot)
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

#endif /* DISABLE_JIT */
