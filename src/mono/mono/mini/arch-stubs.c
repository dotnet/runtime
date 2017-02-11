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

#ifndef MONO_ARCH_HAVE_OP_TAIL_CALL
gboolean
mono_arch_tail_call_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig)
{
	return mono_metadata_signature_equal (caller_sig, callee_sig) && !MONO_TYPE_ISSTRUCT (callee_sig->ret);
}
#endif
