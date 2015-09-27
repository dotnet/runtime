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

#ifndef MONO_ARCH_HAVE_OP_TAIL_CALL
gboolean
mono_arch_tail_call_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig)
{
	return mono_metadata_signature_equal (caller_sig, callee_sig) && !MONO_TYPE_ISSTRUCT (callee_sig->ret);
}
#endif

#ifndef MONO_ARCH_HAVE_INIT_COMPILE
void
mono_arch_init_compile (MonoCompile *cfg)
{
#ifdef MONO_ARCH_NEED_GOT_VAR
	if (cfg->compile_aot)
		cfg->need_got_var = 1;
#endif
#ifdef MONO_ARCH_HAVE_CARD_TABLE_WBARRIER
	cfg->have_card_table_wb = 1;
#endif
#ifdef MONO_ARCH_HAVE_OP_GENERIC_CLASS_INIT
	cfg->have_op_generic_class_init = 1;
#endif
#ifdef MONO_ARCH_EMULATE_MUL_DIV
	cfg->emulate_mul_div = 1;
#endif
#ifdef MONO_ARCH_EMULATE_DIV
	cfg->emulate_div = 1;
#endif
#if !defined(MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS)
	cfg->emulate_long_shift_opts = 1;
#endif
#ifdef MONO_ARCH_HAVE_OBJC_GET_SELECTOR
	cfg->have_objc_get_selector = 1;
#endif
#ifdef MONO_ARCH_HAVE_GENERALIZED_IMT_THUNK
	cfg->have_generalized_imt_thunk = 1;
#endif
#ifdef MONO_ARCH_GSHARED_SUPPORTED
	cfg->gshared_supported = 1;
#endif
	if (MONO_ARCH_HAVE_TLS_GET)
		cfg->have_tls_get = 1;
	if (MONO_ARCH_USE_FPSTACK)
		cfg->use_fpstack = 1;
#ifdef MONO_ARCH_HAVE_LIVERANGE_OPS
	cfg->have_liverange_ops = 1;
#endif
}
#endif
