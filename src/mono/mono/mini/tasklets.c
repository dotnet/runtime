/**
 * \file
 */

#include "config.h"
#include "tasklets.h"
#include "mono/metadata/exception.h"
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/icall-internals.h"
#include "mini.h"
#include "mini-runtime.h"
#include "mono/metadata/loader-internals.h"
#include "mono/utils/mono-tls-inline.h"

#if !defined(ENABLE_NETCORE)
#if defined(MONO_SUPPORT_TASKLETS)

#include "mono/metadata/loader-internals.h"

static mono_mutex_t tasklets_mutex;
#define tasklets_lock() mono_os_mutex_lock(&tasklets_mutex)
#define tasklets_unlock() mono_os_mutex_unlock(&tasklets_mutex)

/* LOCKING: tasklets_mutex is assumed to e taken */
static void
internal_init (void)
{
	if (!mono_gc_is_moving ())
		/* Boehm requires the keepalive stacks to be kept in a hash since mono_gc_alloc_fixed () returns GC memory */
		g_assert_not_reached ();
}

static void*
continuation_alloc (void)
{
	MonoContinuation *cont = g_new0 (MonoContinuation, 1);
	return cont;
}

static void
continuation_free (MonoContinuation *cont)
{
	if (cont->saved_stack)
		mono_gc_free_fixed (cont->saved_stack);
	g_free (cont);
}

static MonoException*
continuation_mark_frame (MonoContinuation *cont)
{
	MonoJitTlsData *jit_tls;
	MonoLMF *lmf;
	MonoContext ctx, new_ctx;
	MonoJitInfo *ji, rji;
	int endloop = FALSE;

	if (cont->domain)
		return mono_get_exception_argument ("cont", "Already marked");

	jit_tls = (MonoJitTlsData *)mono_tls_get_jit_tls ();
	lmf = mono_get_lmf();
	cont->domain = mono_domain_get ();
	cont->thread_id = mono_native_thread_id_get ();

	/* get to the frame that called Mark () */
	memset (&rji, 0, sizeof (rji));
	memset (&ctx, 0, sizeof (ctx));
	do {
		ji = mono_find_jit_info (cont->domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, NULL, NULL);
		if (!ji || ji == (gpointer)-1) {
			return mono_get_exception_not_supported ("Invalid stack frame");
		}
		ctx = new_ctx;
		if (endloop)
			break;
		if (!ji->is_trampoline && strcmp (jinfo_get_method (ji)->name, "Mark") == 0)
			endloop = TRUE;
	} while (1);

	cont->top_sp = MONO_CONTEXT_GET_SP (&ctx);
	/*g_print ("method: %s, sp: %p\n", jinfo_get_method (ji)->name, cont->top_sp);*/

	return NULL;
}

static int
continuation_store (MonoContinuation *cont, int state, MonoException **e)
{
	MonoLMF *lmf = mono_get_lmf ();
	gsize num_bytes;

	if (!cont->domain) {
		*e =  mono_get_exception_argument ("cont", "Continuation not initialized");
		return 0;
	}
	if (cont->domain != mono_domain_get () || !mono_native_thread_id_equals (cont->thread_id, mono_native_thread_id_get ())) {
		*e = mono_get_exception_argument ("cont", "Continuation from another thread or domain");
		return 0;
	}

	cont->lmf = lmf;
	cont->return_ip = __builtin_extract_return_addr (__builtin_return_address (0));
	cont->return_sp = __builtin_frame_address (0);

	num_bytes = (char*)cont->top_sp - (char*)cont->return_sp;

	/*g_print ("store: %d bytes, sp: %p, ip: %p, lmf: %p\n", num_bytes, cont->return_sp, cont->return_ip, lmf);*/

	if (cont->saved_stack && num_bytes <= cont->stack_alloc_size) {
		/* clear to avoid GC retention */
		if (num_bytes < cont->stack_used_size) {
			memset ((char*)cont->saved_stack + num_bytes, 0, cont->stack_used_size - num_bytes);
		}
		cont->stack_used_size = num_bytes;
	} else {
		tasklets_lock ();
		internal_init ();
		if (cont->saved_stack)
			mono_gc_free_fixed (cont->saved_stack);
		cont->stack_used_size = num_bytes;
		cont->stack_alloc_size = num_bytes * 1.1;
		cont->saved_stack = mono_gc_alloc_fixed_no_descriptor (cont->stack_alloc_size, MONO_ROOT_SOURCE_THREADING, NULL, "Tasklet Saved Stack");
		tasklets_unlock ();
	}
	memcpy (cont->saved_stack, cont->return_sp, num_bytes);

	return state;
}

static MonoException*
continuation_restore (MonoContinuation *cont, int state)
{
	MonoLMF **lmf_addr = mono_get_lmf_addr ();
	MonoContinuationRestore restore_state = mono_tasklets_arch_restore ();

	if (!cont->domain || !cont->return_sp)
		return mono_get_exception_argument ("cont", "Continuation not initialized");
	if (cont->domain != mono_domain_get () || !mono_native_thread_id_equals (cont->thread_id, mono_native_thread_id_get ()))
		return mono_get_exception_argument ("cont", "Continuation from another thread or domain");

	/*g_print ("restore: %p, state: %d\n", cont, state);*/
	*lmf_addr = cont->lmf;
	restore_state (cont, state, lmf_addr);
	g_assert_not_reached ();
}

void
mono_tasklets_init (void)
{
	mono_os_mutex_init_recursive (&tasklets_mutex);

	mono_add_internal_call_internal ("Mono.Tasklets.Continuation::alloc", continuation_alloc);
	mono_add_internal_call_internal ("Mono.Tasklets.Continuation::free", continuation_free);
	mono_add_internal_call_internal ("Mono.Tasklets.Continuation::mark", continuation_mark_frame);
	mono_add_internal_call_internal ("Mono.Tasklets.Continuation::store", continuation_store);
	mono_add_internal_call_internal ("Mono.Tasklets.Continuation::restore", continuation_restore);
}

void
mono_tasklets_cleanup (void)
{
}
#else

static
void continuations_not_supported (void)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "Tasklets are not implemented on this platform.");
	mono_error_set_pending_exception (error);
}

static void*
continuation_alloc (void)
{
	continuations_not_supported ();
	return NULL;
}

static void
continuation_free (MonoContinuation *cont)
{
	continuations_not_supported ();
}

static MonoException*
continuation_mark_frame (MonoContinuation *cont)
{
	continuations_not_supported ();
	return NULL;
}

static int
continuation_store (MonoContinuation *cont, int state, MonoException **e)
{
	continuations_not_supported ();
	return 0;
}

static MonoException*
continuation_restore (MonoContinuation *cont, int state)
{
	continuations_not_supported ();
	return NULL;
}

void
mono_tasklets_init(void)
{
	mono_add_internal_call_internal ("Mono.Tasklets.Continuation::alloc", continuation_alloc);
	mono_add_internal_call_internal ("Mono.Tasklets.Continuation::free", continuation_free);
	mono_add_internal_call_internal ("Mono.Tasklets.Continuation::mark", continuation_mark_frame);
	mono_add_internal_call_internal ("Mono.Tasklets.Continuation::store", continuation_store);
	mono_add_internal_call_internal ("Mono.Tasklets.Continuation::restore", continuation_restore);

}
#endif /* MONO_SUPPORT_TASKLETS */

#endif /* ENABLE_NETCORE */

