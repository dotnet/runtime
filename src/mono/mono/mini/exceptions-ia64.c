/*
 * exceptions-ia64.c: exception support for IA64
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>
#include <sys/ucontext.h>

#include <mono/arch/ia64/ia64-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-ia64.h"

#define ALIGN_TO(val,align) (((val) + ((align) - 1)) & ~((align) - 1))

#define NOT_IMPLEMENTED g_assert_not_reached ()

/*
 * mono_arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
gpointer
mono_arch_get_restore_context (void)
{
	static guint8 *start = NULL;
	static gboolean inited = FALSE;
	Ia64CodegenState code;

	if (inited)
		return start;

	/* restore_contect (MonoContext *ctx) */

	start = mono_global_codeman_reserve (256);

	/* FIXME: */
	ia64_codegen_init (code, start);
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - start) <= 256);

	mono_arch_flush_icache (start, code.buf - start);

	return start;
}

/*
 * mono_arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as 
 * @exc object in this case).
 */
gpointer
mono_arch_get_call_filter (void)
{
	static guint8 *start;
	static gboolean inited = FALSE;
	int i;
	guint32 pos;
	Ia64CodegenState code;

	if (inited)
		return start;

	if (inited)
		return start;

	start = mono_global_codeman_reserve (256);

	/* call_filter (MonoContext *ctx, unsigned long eip) */

	/* FIXME: */
	ia64_codegen_init (code, start);
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - start) <= 256);

	mono_arch_flush_icache (start, code.buf - start);

	return start;
}

static void
throw_exception (MonoObject *exc, guint64 rip, guint64 rsp,
				 guint64 rbx, guint64 rbp, guint64 r12, guint64 r13, 
				 guint64 r14, guint64 r15, guint64 rethrow)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;

	if (!restore_context)
		restore_context = mono_arch_get_restore_context ();

	NOT_IMPLEMENTED;
}

static gpointer
get_throw_trampoline (gboolean rethrow)
{
	guint8* start;
	Ia64CodegenState code;

	start = mono_global_codeman_reserve (64);

	/* FIXME: */
	ia64_codegen_init (code, start);
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - start) <= 256);

	mono_arch_flush_icache (start, code.buf - start);

	return start;
}

/**
 * mono_arch_get_throw_exception:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 *
 */
gpointer 
mono_arch_get_throw_exception (void)
{
	static guint8* start;
	static gboolean inited = FALSE;

	if (inited)
		return start;

	start = get_throw_trampoline (FALSE);

	inited = TRUE;

	return start;
}

gpointer 
mono_arch_get_rethrow_exception (void)
{
	static guint8* start;
	static gboolean inited = FALSE;

	if (inited)
		return start;

	start = get_throw_trampoline (TRUE);

	inited = TRUE;

	return start;
}

gpointer 
mono_arch_get_throw_exception_by_name (void)
{	
	static gboolean inited = FALSE;	
	guint8* start;
	Ia64CodegenState code;

	start = mono_global_codeman_reserve (64);

	/* Not used on ia64 */
	ia64_codegen_init (code, start);
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - start) <= 256);

	mono_arch_flush_icache (start, code.buf - start);

	return start;
}

/**
 * mono_arch_get_throw_corlib_exception:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (guint32 ex_token, guint32 offset); 
 * Here, offset is the offset which needs to be substracted from the caller IP 
 * to get the IP of the throw. Passing the offset has the advantage that it 
 * needs no relocations in the caller.
 */
gpointer 
mono_arch_get_throw_corlib_exception (void)
{
	static guint8* start;
	static gboolean inited = FALSE;
	guint64 throw_ex;
	Ia64CodegenState code;

	if (inited)
		return start;

	start = mono_global_codeman_reserve (64);

	/* FIXME: */
	ia64_codegen_init (code, start);
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - start) <= 256);

	mono_arch_flush_icache (start, code.buf - start);

	return start;
}

/* mono_arch_find_jit_info:
 *
 * This function is used to gather information from @ctx. It return the 
 * MonoJitInfo of the corresponding function, unwinds one stack frame and
 * stores the resulting context into @new_ctx. It also stores a string 
 * describing the stack location into @trace (if not NULL), and modifies
 * the @lmf if necessary. @native_offset return the IP offset from the 
 * start of the function or -1 if that info is not available.
 */
MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx, 
			 MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset,
			 gboolean *managed)
{
	MonoJitInfo *ji;
	int i;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	NOT_IMPLEMENTED;
	return NULL;
}

/**
 * mono_arch_handle_exception:
 *
 * @ctx: saved processor state
 * @obj: the exception object
 */
gboolean
mono_arch_handle_exception (void *sigctx, gpointer obj, gboolean test_only)
{
	ucontext_t *ctx = (ucontext_t*)sigctx;
	MonoContext mctx;

	NOT_IMPLEMENTED;
	return FALSE;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	NOT_IMPLEMENTED;
	return NULL;
}
