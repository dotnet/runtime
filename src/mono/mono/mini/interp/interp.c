/**
 * \file
 *
 * interp.c: Interpreter for CIL byte codes
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Miguel de Icaza (miguel@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001, 2002 Ximian, Inc.
 */
#ifndef __USE_ISOC99
#define __USE_ISOC99
#endif
#include "config.h"

#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <glib.h>
#include <math.h>
#include <locale.h>

#include <mono/utils/gc_wrapper.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-tls-inline.h>
#include <mono/utils/mono-membar.h>

#ifdef HAVE_ALLOCA_H
#   include <alloca.h>
#else
#   ifdef __CYGWIN__
#      define alloca __builtin_alloca
#   endif
#endif

/* trim excessive headers */
#include <mono/metadata/image.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threadpool.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/gc-internals.h>
#include <mono/utils/atomic.h>

#include "interp.h"
#include "interp-internals.h"
#include "mintops.h"
#include "interp-intrins.h"

#include <mono/mini/mini.h>
#include <mono/mini/mini-runtime.h>
#include <mono/mini/aot-runtime.h>
#include <mono/mini/llvm-runtime.h>
#include <mono/mini/llvmonly-runtime.h>
#include <mono/mini/jit-icalls.h>
#include <mono/mini/debugger-agent.h>
#include <mono/mini/ee.h>
#include <mono/mini/trace.h>

#ifdef TARGET_ARM
#include <mono/mini/mini-arm.h>
#endif
#include <mono/metadata/icall-decl.h>

/* Arguments that are passed when invoking only a finally/filter clause from the frame */
struct FrameClauseArgs {
	/* Where we start the frame execution from */
	const guint16 *start_with_ip;
	/*
	 * End ip of the exit_clause. We need it so we know whether the resume
	 * state is for this frame (which is called from EH) or for the original
	 * frame further down the stack.
	 */
	const guint16 *end_at_ip;
	/* When exiting this clause we also exit the frame */
	int exit_clause;
	/* Exception that we are filtering */
	MonoException *filter_exception;
	/* Frame that is executing this clause */
	InterpFrame *exec_frame;
};

/*
 * This code synchronizes with interp_mark_stack () using compiler memory barriers.
 */

static FrameDataFragment*
frame_data_frag_new (int size)
{
	FrameDataFragment *frag = (FrameDataFragment*)g_malloc (size);

	frag->pos = (guint8*)&frag->data;
	frag->end = (guint8*)frag + size;
	frag->next = NULL;
	return frag;
}

static void
frame_data_frag_free (FrameDataFragment *frag)
{
	while (frag) {
		FrameDataFragment *next = frag->next;
		g_free (frag);
		frag = next;
	}
}

static void
frame_data_allocator_init (FrameDataAllocator *stack, int size)
{
	FrameDataFragment *frag;

	frag = frame_data_frag_new (size);
	stack->first = stack->current = frag;
	stack->infos_capacity = 4;
	stack->infos = g_malloc (stack->infos_capacity * sizeof (FrameDataInfo));
}

static void
frame_data_allocator_free (FrameDataAllocator *stack)
{
	/* Assert to catch leaks */
	g_assert_checked (stack->current == stack->first && stack->current->pos == (guint8*)&stack->current->data);
	frame_data_frag_free (stack->first);
}

static FrameDataFragment*
frame_data_allocator_add_frag (FrameDataAllocator *stack, int size)
{
	FrameDataFragment *new_frag;

	// FIXME:
	int frag_size = 4096;
	if (size + sizeof (FrameDataFragment) > frag_size)
		frag_size = size + sizeof (FrameDataFragment);
	new_frag = frame_data_frag_new (frag_size);
	mono_compiler_barrier ();
	stack->current->next = new_frag;
	stack->current = new_frag;
	return new_frag;
}

static gpointer
frame_data_allocator_alloc (FrameDataAllocator *stack, InterpFrame *frame, int size)
{
	FrameDataFragment *current = stack->current;
	gpointer res;

	int infos_len = stack->infos_len;

	if (!infos_len || (infos_len > 0 && stack->infos [infos_len - 1].frame != frame)) {
		/* First allocation by this frame. Save the markers for restore */
		if (infos_len == stack->infos_capacity) {
			stack->infos_capacity = infos_len * 2;
			stack->infos = g_realloc (stack->infos, stack->infos_capacity * sizeof (FrameDataInfo));
		}
		stack->infos [infos_len].frame = frame;
		stack->infos [infos_len].frag = current;
		stack->infos [infos_len].pos = current->pos;
		stack->infos_len++;
	}

	if (G_LIKELY (current->pos + size <= current->end)) {
		res = current->pos;
		current->pos += size;
	} else {
		if (current->next && current->next->pos + size <= current->next->end) {
			current = stack->current = current->next;
			current->pos = (guint8*)&current->data;
		} else {
			FrameDataFragment *tmp = current->next;
			/* avoid linking to be freed fragments, so the GC can't trip over it */
			current->next = NULL;
			mono_compiler_barrier ();
			frame_data_frag_free (tmp);

			current = frame_data_allocator_add_frag (stack, size);
		}
		g_assert (current->pos + size <= current->end);
		res = (gpointer)current->pos;
		current->pos += size;
	}
	mono_compiler_barrier ();
	return res;
}

static void
frame_data_allocator_pop (FrameDataAllocator *stack, InterpFrame *frame)
{
	int infos_len = stack->infos_len;

	if (infos_len > 0 && stack->infos [infos_len - 1].frame == frame) {
		infos_len--;
		stack->current = stack->infos [infos_len].frag;
		stack->current->pos = stack->infos [infos_len].pos;
		stack->infos_len = infos_len;
	}
}

/*
 * reinit_frame:
 *
 *   Reinitialize a frame.
 */
static void
reinit_frame (InterpFrame *frame, InterpFrame *parent, InterpMethod *imethod, stackval *sp)
{
	frame->parent = parent;
	frame->imethod = imethod;
	frame->stack = sp;
	frame->state.ip = NULL;
}

/*
 * List of classes whose methods will be executed by transitioning to JITted code.
 * Used for testing.
 */
GSList *mono_interp_jit_classes;
/* Optimizations enabled with interpreter */
int mono_interp_opt = INTERP_OPT_DEFAULT;
/* If TRUE, interpreted code will be interrupted at function entry/backward branches */
static gboolean ss_enabled;

static gboolean interp_init_done = FALSE;

static void
interp_exec_method (InterpFrame *frame, ThreadContext *context, FrameClauseArgs *clause_args);

static MonoException* do_transform_method (InterpFrame *frame, ThreadContext *context);

static InterpMethod* lookup_method_pointer (gpointer addr);

typedef void (*ICallMethod) (InterpFrame *frame);

static MonoNativeTlsKey thread_context_id;

#define DEBUG_INTERP 0
#define COUNT_OPS 0

#if DEBUG_INTERP
int mono_interp_traceopt = 2;
/* If true, then we output the opcodes as we interpret them */
static int global_tracing = 2;

static int debug_indent_level = 0;

static int break_on_method = 0;
static int nested_trace = 0;
static GList *db_methods = NULL;
static char* dump_args (InterpFrame *inv);

static void
output_indent (void)
{
	int h;

	for (h = 0; h < debug_indent_level; h++)
		g_print ("  ");
}

static void
db_match_method (gpointer data, gpointer user_data)
{
	MonoMethod *m = (MonoMethod*)user_data;
	MonoMethodDesc *desc = (MonoMethodDesc*)data;

	if (mono_method_desc_full_match (desc, m))
		break_on_method = 1;
}

static void
debug_enter (InterpFrame *frame, int *tracing)
{
	if (db_methods) {
		g_list_foreach (db_methods, db_match_method, (gpointer)frame->imethod->method);
		if (break_on_method)
			*tracing = nested_trace ? (global_tracing = 2, 3) : 2;
		break_on_method = 0;
	}
	if (*tracing) {
		MonoMethod *method = frame->imethod->method;
		char *mn, *args = dump_args (frame);
		debug_indent_level++;
		output_indent ();
		mn = mono_method_full_name (method, FALSE);
		g_print ("(%p) Entering %s (", mono_thread_internal_current (), mn);
		g_free (mn);
		g_print  ("%s)\n", args);
		g_free (args);
	}
}

#define DEBUG_LEAVE()	\
	if (tracing) {	\
		char *mn, *args;	\
		args = dump_retval (frame);	\
		output_indent ();	\
		mn = mono_method_full_name (frame->imethod->method, FALSE); \
		g_print  ("(%p) Leaving %s", mono_thread_internal_current (),  mn);	\
		g_free (mn); \
		g_print  (" => %s\n", args);	\
		g_free (args);	\
		debug_indent_level--;	\
		if (tracing == 3) global_tracing = 0; \
	}

#else

int mono_interp_traceopt = 0;
#define DEBUG_LEAVE()

#endif

#if defined(__GNUC__) && !defined(TARGET_WASM) && !COUNT_OPS && !DEBUG_INTERP && !ENABLE_CHECKED_BUILD && !PROFILE_INTERP
#define USE_COMPUTED_GOTO 1
#endif

#if USE_COMPUTED_GOTO

#define MINT_IN_DISPATCH(op) goto *in_labels [opcode = (MintOpcode)(op)]
#define MINT_IN_SWITCH(op)   MINT_IN_DISPATCH (op);
#define MINT_IN_BREAK        MINT_IN_DISPATCH (*ip)
#define MINT_IN_CASE(x)      LAB_ ## x:

#else

#define MINT_IN_SWITCH(op) COUNT_OP(op); switch (opcode = (MintOpcode)(op))
#define MINT_IN_CASE(x) case x:
#define MINT_IN_BREAK break

#endif

static GSList*
clear_resume_state (ThreadContext *context, GSList *finally_ips)
{
	/* We have thrown an exception from a finally block. Some of the leave targets were unwound already */
	while (finally_ips &&
		   finally_ips->data >= context->handler_ei->try_start &&
		   finally_ips->data < context->handler_ei->try_end)
		finally_ips = g_slist_remove (finally_ips, finally_ips->data);
	context->has_resume_state = 0;
	context->handler_frame = NULL;
	context->handler_ei = NULL;
	g_assert (context->exc_gchandle);
	mono_gchandle_free_internal (context->exc_gchandle);
	context->exc_gchandle = 0;
	return finally_ips;
}

/*
 * If this bit is set, it means the call has thrown the exception, and we
 * reached this point because the EH code in mono_handle_exception ()
 * unwound all the JITted frames below us. mono_interp_set_resume_state ()
 * has set the fields in context to indicate where we have to resume execution.
 */
#define CHECK_RESUME_STATE(context) do { \
		if ((context)->has_resume_state)	\
			goto resume;			\
	} while (0)

static void
set_context (ThreadContext *context)
{
	mono_native_tls_set_value (thread_context_id, context);

	if (!context)
		return;

	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	g_assertf (jit_tls, "ThreadContext needs initialized JIT TLS");

	/* jit_tls assumes ownership of 'context' */
	jit_tls->interp_context = context;
}

static ThreadContext *
get_context (void)
{
	ThreadContext *context = (ThreadContext *) mono_native_tls_get_value (thread_context_id);
	if (context == NULL) {
		context = g_new0 (ThreadContext, 1);
		context->stack_start = (guchar*)mono_valloc (0, INTERP_STACK_SIZE, MONO_MMAP_READ | MONO_MMAP_WRITE, MONO_MEM_ACCOUNT_INTERP_STACK);
		context->stack_pointer = context->stack_start;

		frame_data_allocator_init (&context->data_stack, 8192);
		/* Make sure all data is initialized before publishing the context */
		mono_compiler_barrier ();
		set_context (context);
	}
	return context;
}

static void
interp_free_context (gpointer ctx)
{
	ThreadContext *context = (ThreadContext*)ctx;

	mono_vfree (context->stack_start, INTERP_STACK_SIZE, MONO_MEM_ACCOUNT_INTERP_STACK);
	/* Prevent interp_mark_stack from trying to scan the data_stack, before freeing it */
	context->stack_start = NULL;
	mono_compiler_barrier ();
	frame_data_allocator_free (&context->data_stack);
	g_free (context);
}

static void
mono_interp_error_cleanup (MonoError* error)
{
	mono_error_cleanup (error); /* FIXME: don't swallow the error */
	error_init_reuse (error); // one instruction, so this function is good inline candidate
}

static MONO_NEVER_INLINE void
ves_real_abort (int line, MonoMethod *mh,
		const unsigned short *ip, stackval *stack, stackval *sp)
{
	ERROR_DECL (error);
	MonoMethodHeader *header = mono_method_get_header_checked (mh, error);
	mono_error_cleanup (error); /* FIXME: don't swallow the error */
	g_printerr ("Execution aborted in method: %s::%s\n", m_class_get_name (mh->klass), mh->name);
	g_printerr ("Line=%d IP=0x%04lx, Aborted execution\n", line, ip-(const unsigned short *) header->code);
	g_printerr ("0x%04x %02x\n", ip-(const unsigned short *) header->code, *ip);
	mono_metadata_free_mh (header);
	g_assert_not_reached ();
}

#define ves_abort() \
	do {\
		ves_real_abort(__LINE__, frame->imethod->method, ip, frame->stack, sp); \
		THROW_EX (mono_get_exception_execution_engine (NULL), ip); \
	} while (0);

static InterpMethod*
lookup_imethod (MonoDomain *domain, MonoMethod *method)
{
	InterpMethod *imethod;
	MonoJitDomainInfo *info;

	info = domain_jit_info (domain);
	mono_domain_jit_code_hash_lock (domain);
	imethod = (InterpMethod*)mono_internal_hash_table_lookup (&info->interp_code_hash, method);
	mono_domain_jit_code_hash_unlock (domain);
	return imethod;
}

static gpointer
interp_get_remoting_invoke (MonoMethod *method, gpointer addr, MonoError *error)
{
#ifndef DISABLE_REMOTING
	InterpMethod *imethod;

	if (addr) {
		imethod = lookup_method_pointer (addr);
	} else {
		g_assert (method);
		imethod = mono_interp_get_imethod (mono_domain_get (), method, error);
		return_val_if_nok (error, NULL);
	}
	g_assert (imethod);
	g_assert (mono_use_interpreter);

	MonoMethod *remoting_invoke_method = mono_marshal_get_remoting_invoke (imethod->method, error);
	return_val_if_nok (error, NULL);
	return mono_interp_get_imethod (mono_domain_get (), remoting_invoke_method, error);
#else
	g_assert_not_reached ();
	return NULL;
#endif
}

InterpMethod*
mono_interp_get_imethod (MonoDomain *domain, MonoMethod *method, MonoError *error)
{
	InterpMethod *imethod;
	MonoJitDomainInfo *info;
	MonoMethodSignature *sig;
	int i;

	error_init (error);

	info = domain_jit_info (domain);
	mono_domain_jit_code_hash_lock (domain);
	imethod = (InterpMethod*)mono_internal_hash_table_lookup (&info->interp_code_hash, method);
	mono_domain_jit_code_hash_unlock (domain);
	if (imethod)
		return imethod;

	sig = mono_method_signature_internal (method);

	imethod = (InterpMethod*)mono_domain_alloc0 (domain, sizeof (InterpMethod));
	imethod->method = method;
	imethod->domain = domain;
	imethod->param_count = sig->param_count;
	imethod->hasthis = sig->hasthis;
	imethod->vararg = sig->call_convention == MONO_CALL_VARARG;
	imethod->code_type = IMETHOD_CODE_UNKNOWN;
	if (imethod->method->string_ctor)
		imethod->rtype = m_class_get_byval_arg (mono_defaults.string_class);
	else
		imethod->rtype = mini_get_underlying_type (sig->ret);
	imethod->param_types = (MonoType**)mono_domain_alloc0 (domain, sizeof (MonoType*) * sig->param_count);
	for (i = 0; i < sig->param_count; ++i)
		imethod->param_types [i] = mini_get_underlying_type (sig->params [i]);

	mono_domain_jit_code_hash_lock (domain);
	if (!mono_internal_hash_table_lookup (&info->interp_code_hash, method))
		mono_internal_hash_table_insert (&info->interp_code_hash, method, imethod);
	mono_domain_jit_code_hash_unlock (domain);

	imethod->prof_flags = mono_profiler_get_call_instrumentation_flags (imethod->method);

	return imethod;
}

#if defined (MONO_CROSS_COMPILE) || defined (HOST_WASM)
#define INTERP_PUSH_LMF_WITH_CTX_BODY(ext, exit_label) \
	(ext).kind = MONO_LMFEXT_INTERP_EXIT;

#elif defined(MONO_ARCH_HAS_NO_PROPER_MONOCTX)
/* some platforms, e.g. appleTV, don't provide us a precise MonoContext
 * (registers are not accurate), thus resuming to the label does not work. */
#define INTERP_PUSH_LMF_WITH_CTX_BODY(ext, exit_label) \
	(ext).kind = MONO_LMFEXT_INTERP_EXIT;
#elif defined (_MSC_VER)
#define INTERP_PUSH_LMF_WITH_CTX_BODY(ext, exit_label) \
	(ext).kind = MONO_LMFEXT_INTERP_EXIT_WITH_CTX; \
	(ext).interp_exit_label_set = FALSE; \
	MONO_CONTEXT_GET_CURRENT ((ext).ctx); \
	if ((ext).interp_exit_label_set == FALSE) \
		mono_arch_do_ip_adjustment (&(ext).ctx); \
	if ((ext).interp_exit_label_set == TRUE) \
		goto exit_label; \
	(ext).interp_exit_label_set = TRUE;
#elif defined(MONO_ARCH_HAS_MONO_CONTEXT)
#define INTERP_PUSH_LMF_WITH_CTX_BODY(ext, exit_label) \
	(ext).kind = MONO_LMFEXT_INTERP_EXIT_WITH_CTX; \
	MONO_CONTEXT_GET_CURRENT ((ext).ctx); \
	MONO_CONTEXT_SET_IP (&(ext).ctx, (&&exit_label)); \
	mono_arch_do_ip_adjustment (&(ext).ctx);
#else
#define INTERP_PUSH_LMF_WITH_CTX_BODY(ext, exit_label) g_error ("requires working mono-context");
#endif

/* INTERP_PUSH_LMF_WITH_CTX:
 *
 * same as interp_push_lmf, but retrieving and attaching MonoContext to it.
 * This is needed to resume into the interp when the exception is thrown from
 * native code (see ./mono/tests/install_eh_callback.exe).
 *
 * This must be a macro in order to retrieve the right register values for
 * MonoContext.
 */
#define INTERP_PUSH_LMF_WITH_CTX(frame, ext, exit_label) \
	memset (&(ext), 0, sizeof (MonoLMFExt)); \
	(ext).interp_exit_data = (frame); \
	INTERP_PUSH_LMF_WITH_CTX_BODY ((ext), exit_label); \
	mono_push_lmf (&(ext));

/*
 * interp_push_lmf:
 *
 * Push an LMF frame on the LMF stack
 * to mark the transition to native code.
 * This is needed for the native code to
 * be able to do stack walks.
 */
static void
interp_push_lmf (MonoLMFExt *ext, InterpFrame *frame)
{
	memset (ext, 0, sizeof (MonoLMFExt));
	ext->kind = MONO_LMFEXT_INTERP_EXIT;
	ext->interp_exit_data = frame;

	mono_push_lmf (ext);
}

static void
interp_pop_lmf (MonoLMFExt *ext)
{
	mono_pop_lmf (&ext->lmf);
}

static InterpMethod*
get_virtual_method (InterpMethod *imethod, MonoVTable *vtable)
{
	MonoMethod *m = imethod->method;
	MonoDomain *domain = imethod->domain;
	InterpMethod *ret = NULL;

#ifndef DISABLE_REMOTING
	if (mono_class_is_transparent_proxy (vtable->klass)) {
		ERROR_DECL (error);
		MonoMethod *remoting_invoke_method = mono_marshal_get_remoting_invoke_with_check (m, error);
		mono_error_assert_ok (error);
		ret = mono_interp_get_imethod (domain, remoting_invoke_method, error);
		mono_error_assert_ok (error);
		return ret;
	}
#endif

	if ((m->flags & METHOD_ATTRIBUTE_FINAL) || !(m->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
		if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) {
			ERROR_DECL (error);
			ret = mono_interp_get_imethod (domain, mono_marshal_get_synchronized_wrapper (m), error);
			mono_error_cleanup (error); /* FIXME: don't swallow the error */
		} else {
			ret = imethod;
		}
		return ret;
	}

	mono_class_setup_vtable (vtable->klass);

	int slot = mono_method_get_vtable_slot (m);
	if (mono_class_is_interface (m->klass)) {
		g_assert (vtable->klass != m->klass);
		/* TODO: interface offset lookup is slow, go through IMT instead */
		gboolean non_exact_match;
		slot += mono_class_interface_offset_with_variance (vtable->klass, m->klass, &non_exact_match);
	}

	MonoMethod *virtual_method = m_class_get_vtable (vtable->klass) [slot];
	if (m->is_inflated && mono_method_get_context (m)->method_inst) {
		MonoGenericContext context = { NULL, NULL };

		if (mono_class_is_ginst (virtual_method->klass))
			context.class_inst = mono_class_get_generic_class (virtual_method->klass)->context.class_inst;
		else if (mono_class_is_gtd (virtual_method->klass))
			context.class_inst = mono_class_get_generic_container (virtual_method->klass)->context.class_inst;
		context.method_inst = mono_method_get_context (m)->method_inst;

		ERROR_DECL (error);
		virtual_method = mono_class_inflate_generic_method_checked (virtual_method, &context, error);
		mono_error_cleanup (error); /* FIXME: don't swallow the error */
	}

	if (virtual_method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		virtual_method = mono_marshal_get_native_wrapper (virtual_method, FALSE, FALSE);
	}

	if (virtual_method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) {
		virtual_method = mono_marshal_get_synchronized_wrapper (virtual_method);
	}

	ERROR_DECL (error);
	InterpMethod *virtual_imethod = mono_interp_get_imethod (domain, virtual_method, error);
	mono_error_cleanup (error); /* FIXME: don't swallow the error */
	return virtual_imethod;
}

typedef struct {
	InterpMethod *imethod;
	InterpMethod *target_imethod;
} InterpVTableEntry;

/* domain lock must be held */
static GSList*
append_imethod (MonoDomain *domain, GSList *list, InterpMethod *imethod, InterpMethod *target_imethod)
{
	GSList *ret;
	InterpVTableEntry *entry;

	entry = (InterpVTableEntry*) mono_mempool_alloc (domain->mp, sizeof (InterpVTableEntry));
	entry->imethod = imethod;
	entry->target_imethod = target_imethod;
	ret = g_slist_append_mempool (domain->mp, list, entry);

	return ret;
}

static InterpMethod*
get_target_imethod (GSList *list, InterpMethod *imethod)
{
	while (list != NULL) {
		InterpVTableEntry *entry = (InterpVTableEntry*) list->data;
		if (entry->imethod == imethod)
			return entry->target_imethod;
		list = list->next;
	}
	return NULL;
}

static gpointer*
get_method_table (MonoVTable *vtable, int offset)
{
	if (offset >= 0)
		return vtable->interp_vtable;
	else
		return (gpointer*)vtable;
}

static gpointer*
alloc_method_table (MonoVTable *vtable, int offset)
{
	gpointer *table;

	if (offset >= 0) {
		table = mono_domain_alloc0 (vtable->domain, m_class_get_vtable_size (vtable->klass) * sizeof (gpointer));
		vtable->interp_vtable = table;
	} else {
		table = (gpointer*)vtable;
	}

	return table;
}

static InterpMethod* // Inlining causes additional stack use in caller.
get_virtual_method_fast (InterpMethod *imethod, MonoVTable *vtable, int offset)
{
	gpointer *table;

#ifndef DISABLE_REMOTING
	/* FIXME Remoting */
	if (mono_class_is_transparent_proxy (vtable->klass))
		return get_virtual_method (imethod, vtable);
#endif

	table = get_method_table (vtable, offset);

	if (!table) {
		/* Lazily allocate method table */
		mono_domain_lock (vtable->domain);
		table = get_method_table (vtable, offset);
		if (!table)
			table = alloc_method_table (vtable, offset);
		mono_domain_unlock (vtable->domain);
	}

	if (!table [offset]) {
		InterpMethod *target_imethod = get_virtual_method (imethod, vtable);
		/* Lazily initialize the method table slot */
		mono_domain_lock (vtable->domain);
		if (!table [offset]) {
			if (imethod->method->is_inflated || offset < 0)
				table [offset] = append_imethod (vtable->domain, NULL, imethod, target_imethod);
			else
				table [offset] = (gpointer) ((gsize)target_imethod | 0x1);
		}
		mono_domain_unlock (vtable->domain);
	}

	if ((gsize)table [offset] & 0x1) {
		/* Non generic virtual call. Only one method in slot */
		return (InterpMethod*) ((gsize)table [offset] & ~0x1);
	} else {
		/* Virtual generic or interface call. Multiple methods in slot */
		InterpMethod *target_imethod = get_target_imethod ((GSList*)table [offset], imethod);

		if (!target_imethod) {
			target_imethod = get_virtual_method (imethod, vtable);
			mono_domain_lock (vtable->domain);
			if (!get_target_imethod ((GSList*)table [offset], imethod))
				table [offset] = append_imethod (vtable->domain, (GSList*)table [offset], imethod, target_imethod);
			mono_domain_unlock (vtable->domain);
		}
		return target_imethod;
	}
}

static void inline
stackval_from_data (MonoType *type, stackval *result, const void *data, gboolean pinvoke)
{
	type = mini_native_type_replace_type (type);
	if (type->byref) {
		switch (type->type) {
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_STRING:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			break;
		default:
			break;
		}
		result->data.p = *(gpointer*)data;
		return;
	}
	switch (type->type) {
	case MONO_TYPE_VOID:
		return;
	case MONO_TYPE_I1:
		result->data.i = *(gint8*)data;
		return;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		result->data.i = *(guint8*)data;
		return;
	case MONO_TYPE_I2:
		result->data.i = *(gint16*)data;
		return;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		result->data.i = *(guint16*)data;
		return;
	case MONO_TYPE_I4:
		result->data.i = *(gint32*)data;
		return;
	case MONO_TYPE_U:
	case MONO_TYPE_I:
		result->data.nati = *(mono_i*)data;
		return;
	case MONO_TYPE_PTR:
		result->data.p = *(gpointer*)data;
		return;
	case MONO_TYPE_U4:
		result->data.i = *(guint32*)data;
		return;
	case MONO_TYPE_R4:
		/* memmove handles unaligned case */
		memmove (&result->data.f_r4, data, sizeof (float));
		return;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		memmove (&result->data.l, data, sizeof (gint64));
		return;
	case MONO_TYPE_R8:
		memmove (&result->data.f, data, sizeof (double));
		return;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
		result->data.p = *(gpointer*)data;
		return;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			stackval_from_data (mono_class_enum_basetype_internal (type->data.klass), result, data, pinvoke);
			return;
		} else if (pinvoke) {
			memcpy (result->data.vt, data, mono_class_native_size (type->data.klass, NULL));
		} else {
			mono_value_copy_internal (result->data.vt, data, type->data.klass);
		}
		return;
	case MONO_TYPE_GENERICINST: {
		if (mono_type_generic_inst_is_valuetype (type)) {
			MonoClass *klass = mono_class_from_mono_type_internal (type);
			if (pinvoke)
				memcpy (result->data.vt, data, mono_class_native_size (klass, NULL));
			else
				mono_value_copy_internal (result->data.vt, data, klass);
			return;
		}
		stackval_from_data (m_class_get_byval_arg (type->data.generic_class->container_class), result, data, pinvoke);
		return;
	}
	default:
		g_error ("got type 0x%02x", type->type);
	}
}

static void inline
stackval_to_data (MonoType *type, stackval *val, void *data, gboolean pinvoke)
{
	type = mini_native_type_replace_type (type);
	if (type->byref) {
		gpointer *p = (gpointer*)data;
		*p = val->data.p;
		return;
	}
	/* printf ("TODAT0 %p\n", data); */
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1: {
		guint8 *p = (guint8*)data;
		*p = val->data.i;
		return;
	}
	case MONO_TYPE_BOOLEAN: {
		guint8 *p = (guint8*)data;
		*p = (val->data.i != 0);
		return;
	}
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR: {
		guint16 *p = (guint16*)data;
		*p = val->data.i;
		return;
	}
	case MONO_TYPE_I: {
		mono_i *p = (mono_i*)data;
		/* In theory the value used by stloc should match the local var type
	 	   but in practice it sometimes doesn't (a int32 gets dup'd and stloc'd into
		   a native int - both by csc and mcs). Not sure what to do about sign extension
		   as it is outside the spec... doing the obvious */
		*p = (mono_i)val->data.nati;
		return;
	}
	case MONO_TYPE_U: {
		mono_u *p = (mono_u*)data;
		/* see above. */
		*p = (mono_u)val->data.nati;
		return;
	}
	case MONO_TYPE_I4:
	case MONO_TYPE_U4: {
		gint32 *p = (gint32*)data;
		*p = val->data.i;
		return;
	}
	case MONO_TYPE_I8:
	case MONO_TYPE_U8: {
		memmove (data, &val->data.l, sizeof (gint64));
		return;
	}
	case MONO_TYPE_R4: {
		/* memmove handles unaligned case */
		memmove (data, &val->data.f_r4, sizeof (float));
		return;
	}
	case MONO_TYPE_R8: {
		memmove (data, &val->data.f, sizeof (double));
		return;
	}
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY: {
		gpointer *p = (gpointer *) data;
		mono_gc_wbarrier_generic_store_internal (p, val->data.o);
		return;
	}
	case MONO_TYPE_PTR: {
		gpointer *p = (gpointer *) data;
		*p = val->data.p;
		return;
	}
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			stackval_to_data (mono_class_enum_basetype_internal (type->data.klass), val, data, pinvoke);
			return;
		} else if (pinvoke) {
			memcpy (data, val->data.vt, mono_class_native_size (type->data.klass, NULL));
		} else {
			mono_value_copy_internal (data, val->data.vt, type->data.klass);
		}
		return;
	case MONO_TYPE_GENERICINST: {
		MonoClass *container_class = type->data.generic_class->container_class;

		if (m_class_is_valuetype (container_class) && !m_class_is_enumtype (container_class)) {
			MonoClass *klass = mono_class_from_mono_type_internal (type);
			if (pinvoke)
				memcpy (data, val->data.vt, mono_class_native_size (klass, NULL));
			else
				mono_value_copy_internal (data, val->data.vt, klass);
			return;
		}
		stackval_to_data (m_class_get_byval_arg (type->data.generic_class->container_class), val, data, pinvoke);
		return;
	}
	default:
		g_error ("got type %x", type->type);
	}
}

/*
 * Same as stackval_to_data but return address of storage instead
 * of copying the value.
 */
static gpointer
stackval_to_data_addr (MonoType *type, stackval *val)
{
	type = mini_native_type_replace_type (type);
	if (type->byref)
		return &val->data.p;

	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return &val->data.i;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		return &val->data.nati;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return &val->data.l;
	case MONO_TYPE_R4:
		return &val->data.f_r4;
	case MONO_TYPE_R8:
		return &val->data.f;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_PTR:
		return &val->data.p;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass))
			return stackval_to_data_addr (mono_class_enum_basetype_internal (type->data.klass), val);
		else
			return val->data.vt;
	case MONO_TYPE_TYPEDBYREF:
		return val->data.vt;
	case MONO_TYPE_GENERICINST: {
		MonoClass *container_class = type->data.generic_class->container_class;

		if (m_class_is_valuetype (container_class) && !m_class_is_enumtype (container_class))
			return val->data.vt;
		return stackval_to_data_addr (m_class_get_byval_arg (type->data.generic_class->container_class), val);
	}
	default:
		g_error ("got type %x", type->type);
	}
}

/*
 * interp_throw:
 *   Throw an exception from the interpreter.
 */
static MONO_NEVER_INLINE void
interp_throw (ThreadContext *context, MonoException *ex, InterpFrame *frame, const guint16* ip, gboolean rethrow)
{
	ERROR_DECL (error);
	MonoLMFExt ext;

	interp_push_lmf (&ext, frame);
	/*
	 * When explicitly throwing exception we pass the ip of the instruction that throws the exception.
	 * Offset the subtraction from interp_frame_get_ip, so we don't end up in prev instruction.
	 */
	frame->state.ip = ip + 1;

	if (mono_object_isinst_checked ((MonoObject *) ex, mono_defaults.exception_class, error)) {
		MonoException *mono_ex = ex;
		if (!rethrow) {
			mono_ex->stack_trace = NULL;
			mono_ex->trace_ips = NULL;
		}
	}
	mono_error_assert_ok (error);

	MonoContext ctx;
	memset (&ctx, 0, sizeof (MonoContext));
	MONO_CONTEXT_SET_SP (&ctx, frame);

	/*
	 * Call the JIT EH code. The EH code will call back to us using:
	 * - mono_interp_set_resume_state ()/run_finally ()/run_filter ().
	 * Since ctx.ip is 0, this will start unwinding from the LMF frame
	 * pushed above, which points to our frames.
	 */
	mono_handle_exception (&ctx, (MonoObject*)ex);
	if (MONO_CONTEXT_GET_IP (&ctx) != 0) {
		/* We need to unwind into non-interpreter code */
		mono_restore_context (&ctx);
		g_assert_not_reached ();
	}

	interp_pop_lmf (&ext);

	g_assert (context->has_resume_state);
}

#define THROW_EX_GENERAL(exception,ex_ip, rethrow)		\
	do {							\
		interp_throw (context, (exception), (frame), (ex_ip), (rethrow)); \
		goto resume;							  \
	} while (0)

#define THROW_EX(exception,ex_ip) THROW_EX_GENERAL ((exception), (ex_ip), FALSE)

#define NULL_CHECK(o) do { \
	if (G_UNLIKELY (!(o))) \
		THROW_EX (mono_get_exception_null_reference (), ip); \
	} while (0)

#define EXCEPTION_CHECKPOINT	\
	do {										\
		if (mono_thread_interruption_request_flag && !mono_threads_is_critical_method (frame->imethod->method)) { \
			MonoException *exc = mono_thread_interruption_checkpoint ();	\
			if (exc)							\
				THROW_EX (exc, ip);					\
		}									\
	} while (0)

/* Don't throw exception if thread is in GC Safe mode. Should only happen in managed-to-native wrapper. */
#define EXCEPTION_CHECKPOINT_GC_UNSAFE	\
	do {										\
		if (mono_thread_interruption_request_flag && !mono_threads_is_critical_method (frame->imethod->method) && mono_thread_is_gc_unsafe_mode ()) { \
			MonoException *exc = mono_thread_interruption_checkpoint ();	\
			if (exc)							\
				THROW_EX (exc, ip);					\
		}									\
	} while (0)

static MonoObject*
ves_array_create (MonoDomain *domain, MonoClass *klass, int param_count, stackval *values, MonoError *error)
{
	int rank = m_class_get_rank (klass);
	uintptr_t *lengths = g_newa (uintptr_t, rank * 2);
	intptr_t *lower_bounds = NULL;
	if (2 * rank == param_count) {
		for (int l = 0; l < 2; ++l) {
			int src = l;
			int dst = l * rank;
			for (int r = 0; r < rank; ++r, src += 2, ++dst) {
				lengths [dst] = values [src].data.i;
			}
		}
		/* lower bounds are first. */
		lower_bounds = (intptr_t *) lengths;
		lengths += rank;
	} else {
		/* Only lengths provided. */
		for (int i = 0; i < param_count; ++i) {
			lengths [i] = values [i].data.i;
		}
	}
	return (MonoObject*) mono_array_new_full_checked (domain, klass, lengths, lower_bounds, error);
}

static gint32
ves_array_calculate_index (MonoArray *ao, stackval *sp, gboolean safe)
{
	MonoClass *ac = ((MonoObject *) ao)->vtable->klass;

	guint32 pos = 0;
	if (ao->bounds) {
		for (gint32 i = 0; i < m_class_get_rank (ac); i++) {
			guint32 idx = sp [i].data.i;
			guint32 lower = ao->bounds [i].lower_bound;
			guint32 len = ao->bounds [i].length;
			if (safe && (idx < lower || (idx - lower) >= len))
				return -1;
			pos = (pos * len) + idx - lower;
		}
	} else {
		pos = sp [0].data.i;
		if (safe && pos >= ao->max_length)
			return -1;
	}
	return pos;
}

static MonoException*
ves_array_get (InterpFrame *frame, stackval *sp, stackval *retval, MonoMethodSignature *sig, gboolean safe)
{
	MonoObject *o = sp->data.o;
	MonoArray *ao = (MonoArray *) o;
	MonoClass *ac = o->vtable->klass;

	g_assert (m_class_get_rank (ac) >= 1);

	gint32 pos = ves_array_calculate_index (ao, sp + 1, safe);
	if (pos == -1)
		return mono_get_exception_index_out_of_range ();

	gint32 esize = mono_array_element_size (ac);
	gconstpointer ea = mono_array_addr_with_size_fast (ao, esize, pos);

	MonoType *mt = sig->ret;
	stackval_from_data (mt, retval, ea, FALSE);
	return NULL;
}

static MonoException*
ves_array_element_address (InterpFrame *frame, MonoClass *required_type, MonoArray *ao, stackval *sp, gboolean needs_typecheck)
{
	MonoClass *ac = ((MonoObject *) ao)->vtable->klass;

	g_assert (m_class_get_rank (ac) >= 1);

	gint32 pos = ves_array_calculate_index (ao, sp, TRUE);
	if (pos == -1)
		return mono_get_exception_index_out_of_range ();

	if (needs_typecheck && !mono_class_is_assignable_from_internal (m_class_get_element_class (mono_object_class ((MonoObject *) ao)), required_type))
		return mono_get_exception_array_type_mismatch ();
	gint32 esize = mono_array_element_size (ac);
	sp [-1].data.p = mono_array_addr_with_size_fast (ao, esize, pos);
	return NULL;
}

#ifdef MONO_ARCH_HAVE_INTERP_ENTRY_TRAMPOLINE
static MonoFuncV mono_native_to_interp_trampoline = NULL;
#endif

#ifndef MONO_ARCH_HAVE_INTERP_PINVOKE_TRAMP
static InterpMethodArguments* build_args_from_sig (MonoMethodSignature *sig, InterpFrame *frame)
{
	InterpMethodArguments *margs = g_malloc0 (sizeof (InterpMethodArguments));

#ifdef TARGET_ARM
	g_assert (mono_arm_eabi_supported ());
	int i8_align = mono_arm_i8_align ();
#endif

#ifdef TARGET_WASM
	margs->sig = sig;
#endif

	if (sig->hasthis)
		margs->ilen++;

	for (int i = 0; i < sig->param_count; i++) {
		guint32 ptype = sig->params [i]->byref ? MONO_TYPE_PTR : sig->params [i]->type;
		switch (ptype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_GENERICINST:
#if SIZEOF_VOID_P == 8
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
#endif
			margs->ilen++;
			break;
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
#ifdef TARGET_ARM
			/* pairs begin at even registers */
			if (i8_align == 8 && margs->ilen & 1)
				margs->ilen++;
#endif
			margs->ilen += 2;
			break;
#endif
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			margs->flen++;
			break;
		default:
			g_error ("build_args_from_sig: not implemented yet (1): 0x%x\n", ptype);
		}
	}

	if (margs->ilen > 0)
		margs->iargs = g_malloc0 (sizeof (gpointer) * margs->ilen);

	if (margs->flen > 0)
		margs->fargs = g_malloc0 (sizeof (double) * margs->flen);

	if (margs->ilen > INTERP_ICALL_TRAMP_IARGS)
		g_error ("build_args_from_sig: TODO, allocate gregs: %d\n", margs->ilen);

	if (margs->flen > INTERP_ICALL_TRAMP_FARGS)
		g_error ("build_args_from_sig: TODO, allocate fregs: %d\n", margs->flen);


	size_t int_i = 0;
	size_t int_f = 0;

	if (sig->hasthis) {
		margs->iargs [0] = frame->stack [0].data.p;
		int_i++;
		g_error ("FIXME if hasthis, we incorrectly access the args below");
	}

	for (int i = 0; i < sig->param_count; i++) {
		guint32 ptype = sig->params [i]->byref ? MONO_TYPE_PTR : sig->params [i]->type;
		switch (ptype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_GENERICINST:
#if SIZEOF_VOID_P == 8
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
#endif
			margs->iargs [int_i] = frame->stack [i].data.p;
#if DEBUG_INTERP
			g_print ("build_args_from_sig: margs->iargs [%d]: %p (frame @ %d)\n", int_i, margs->iargs [int_i], i);
#endif
			int_i++;
			break;
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_I8:
		case MONO_TYPE_U8: {
			stackval *sarg = &frame->stack [i];
#ifdef TARGET_ARM
			/* pairs begin at even registers */
			if (i8_align == 8 && int_i & 1)
				int_i++;
#endif
			margs->iargs [int_i] = (gpointer) sarg->data.pair.lo;
			int_i++;
			margs->iargs [int_i] = (gpointer) sarg->data.pair.hi;
#if DEBUG_INTERP
			g_print ("build_args_from_sig: margs->iargs [%d/%d]: 0x%016" PRIx64 ", hi=0x%08x lo=0x%08x (frame @ %d)\n", int_i - 1, int_i, *((guint64 *) &margs->iargs [int_i - 1]), sarg->data.pair.hi, sarg->data.pair.lo, i);
#endif
			int_i++;
			break;
		}
#endif
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			if (ptype == MONO_TYPE_R4)
				* (float *) &(margs->fargs [int_f]) = frame->stack [i].data.f_r4;
			else
				margs->fargs [int_f] = frame->stack [i].data.f;
#if DEBUG_INTERP
			g_print ("build_args_from_sig: margs->fargs [%d]: %p (%f) (frame @ %d)\n", int_f, margs->fargs [int_f], margs->fargs [int_f], i);
#endif
			int_f ++;
			break;
		default:
			g_error ("build_args_from_sig: not implemented yet (2): 0x%x\n", ptype);
		}
	}

	switch (sig->ret->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_GENERICINST:
			margs->retval = &frame->retval->data.p;
			margs->is_float_ret = 0;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			margs->retval = &frame->retval->data.p;
			margs->is_float_ret = 1;
			break;
		case MONO_TYPE_VOID:
			margs->retval = NULL;
			break;
		default:
			g_error ("build_args_from_sig: ret type not implemented yet: 0x%x\n", sig->ret->type);
	}

	return margs;
}
#endif

static void
interp_frame_arg_to_data (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gpointer data)
{
	InterpFrame *iframe = (InterpFrame*)frame;

	if (index == -1)
		stackval_to_data (sig->ret, iframe->retval, data, sig->pinvoke);
	else
		stackval_to_data (sig->params [index], &iframe->stack [index], data, sig->pinvoke);
}

static void
interp_data_to_frame_arg (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gconstpointer data)
{
	InterpFrame *iframe = (InterpFrame*)frame;

	if (index == -1)
		stackval_from_data (sig->ret, iframe->retval, data, sig->pinvoke);
	else if (sig->hasthis && index == 0)
		iframe->stack [index].data.p = *(gpointer*)data;
	else
		stackval_from_data (sig->params [index - sig->hasthis], &iframe->stack [index], data, sig->pinvoke);
}

static gpointer
interp_frame_arg_to_storage (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index)
{
	InterpFrame *iframe = (InterpFrame*)frame;

	if (index == -1)
		return stackval_to_data_addr (sig->ret, iframe->retval);
	else
		return stackval_to_data_addr (sig->params [index], &iframe->stack [index]);
}

static void
interp_frame_arg_set_storage (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gpointer storage)
{
	InterpFrame *iframe = (InterpFrame*)frame;
	stackval *val = (index == -1) ? iframe->retval : &iframe->stack [index];
	MonoType *type = (index == -1) ? sig->ret : sig->params [index];

	switch (type->type) {
	case MONO_TYPE_GENERICINST:
		if (!MONO_TYPE_IS_REFERENCE (type))
			val->data.vt = storage;
		break;
	case MONO_TYPE_VALUETYPE:
		val->data.vt = storage;
		break;
	default:
		g_assert_not_reached ();
	}
}

static MonoPIFunc
get_interp_to_native_trampoline (void)
{
	static MonoPIFunc trampoline = NULL;

	if (!trampoline) {
		if (mono_ee_features.use_aot_trampolines) {
			trampoline = (MonoPIFunc) mono_aot_get_trampoline ("interp_to_native_trampoline");
		} else {
			MonoTrampInfo *info;
			trampoline = (MonoPIFunc) mono_arch_get_interp_to_native_trampoline (&info);
			mono_tramp_info_register (info, NULL);
		}
		mono_memory_barrier ();
	}
	return trampoline;
}

static void
interp_to_native_trampoline (gpointer addr, gpointer ccontext)
{
	get_interp_to_native_trampoline () (addr, ccontext);
}

/* MONO_NO_OPTIMIZATION is needed due to usage of INTERP_PUSH_LMF_WITH_CTX. */
#ifdef _MSC_VER
#pragma optimize ("", off)
#endif
static MONO_NO_OPTIMIZATION MONO_NEVER_INLINE void
ves_pinvoke_method (
	MonoMethodSignature *sig,
	MonoFuncV addr,
	ThreadContext *context,
	InterpFrame *parent_frame,
	stackval *retval,
	gboolean save_last_error,
	gpointer *cache,
	stackval *sp)
{
	InterpFrame frame = {0};
	frame.parent = parent_frame;
	frame.stack = sp;
	frame.retval = retval;

	MonoLMFExt ext;
	gpointer args;

	g_assert (!frame.imethod);

	/*
	 * When there's a calli in a pinvoke wrapper, we're in GC Safe mode.
	 * When we're called for some other calli, we may be in GC Unsafe mode.
	 *
	 * On any code path where we call anything other than the entry_func,
	 * we need to switch back to GC Unsafe before calling the runtime.
	 */
	MONO_REQ_GC_NEUTRAL_MODE;

#ifdef HOST_WASM
	/*
	 * Use a per-signature entry function.
	 * Cache it in imethod->data_items.
	 * This is GC safe.
	 */
	MonoPIFunc entry_func = *cache;
	if (!entry_func) {
		entry_func = (MonoPIFunc)mono_wasm_get_interp_to_native_trampoline (sig);
		mono_memory_barrier ();
		*cache = entry_func;
	}
#else
	static MonoPIFunc entry_func = NULL;
	if (!entry_func) {
		MONO_ENTER_GC_UNSAFE;
#ifdef MONO_ARCH_HAS_NO_PROPER_MONOCTX
		ERROR_DECL (error);
		entry_func = (MonoPIFunc) mono_jit_compile_method_jit_only (mini_get_interp_lmf_wrapper ("mono_interp_to_native_trampoline", (gpointer) mono_interp_to_native_trampoline), error);
		mono_error_assert_ok (error);
#else
		entry_func = get_interp_to_native_trampoline ();
#endif
		mono_memory_barrier ();
		MONO_EXIT_GC_UNSAFE;
	}
#endif

#ifdef ENABLE_NETCORE
	if (save_last_error) {
		mono_marshal_clear_last_error ();
	}
#endif

#ifdef MONO_ARCH_HAVE_INTERP_PINVOKE_TRAMP
	CallContext ccontext;
	MONO_ENTER_GC_UNSAFE;
	mono_arch_set_native_call_context_args (&ccontext, &frame, sig);
	MONO_EXIT_GC_UNSAFE;
	args = &ccontext;
#else
	InterpMethodArguments *margs = build_args_from_sig (sig, &frame);
	args = margs;
#endif

	INTERP_PUSH_LMF_WITH_CTX (&frame, ext, exit_pinvoke);
	entry_func ((gpointer) addr, args);
	if (save_last_error)
		mono_marshal_set_last_error ();
	interp_pop_lmf (&ext);

#ifdef MONO_ARCH_HAVE_INTERP_PINVOKE_TRAMP
	if (!context->has_resume_state) {
		MONO_ENTER_GC_UNSAFE;
		mono_arch_get_native_call_context_ret (&ccontext, &frame, sig);
		MONO_EXIT_GC_UNSAFE;
	}

	g_free (ccontext.stack);
#else
	if (!context->has_resume_state && !MONO_TYPE_ISSTRUCT (sig->ret))
		stackval_from_data (sig->ret, frame.retval, (char*)&frame.retval->data.p, sig->pinvoke);

	g_free (margs->iargs);
	g_free (margs->fargs);
	g_free (margs);
#endif
	goto exit_pinvoke; // prevent unused label warning in some configurations
exit_pinvoke:
	return;
}
#ifdef _MSC_VER
#pragma optimize ("", on)
#endif

/*
 * interp_init_delegate:
 *
 *   Initialize del->interp_method.
 */
static void
interp_init_delegate (MonoDelegate *del, MonoError *error)
{
	MonoMethod *method;

	if (del->interp_method) {
		/* Delegate created by a call to ves_icall_mono_delegate_ctor_interp () */
		del->method = ((InterpMethod *)del->interp_method)->method;
	} if (del->method_ptr && !del->method) {
		/* Delegate created from methodInfo.MethodHandle.GetFunctionPointer() */
		del->interp_method = (InterpMethod *)del->method_ptr;
	} else if (del->method) {
		/* Delegate created dynamically */
		del->interp_method = mono_interp_get_imethod (del->object.vtable->domain, del->method, error);
	} else {
		/* Created from JITted code */
		g_assert_not_reached ();
	}

	method = ((InterpMethod*)del->interp_method)->method;
	if (del->target &&
			method &&
			method->flags & METHOD_ATTRIBUTE_VIRTUAL &&
			method->flags & METHOD_ATTRIBUTE_ABSTRACT &&
			mono_class_is_abstract (method->klass))
		del->interp_method = get_virtual_method ((InterpMethod*)del->interp_method, del->target->vtable);

	method = ((InterpMethod*)del->interp_method)->method;
	if (method && m_class_get_parent (method->klass) == mono_defaults.multicastdelegate_class) {
		const char *name = method->name;
		if (*name == 'I' && (strcmp (name, "Invoke") == 0)) {
			/*
			 * When invoking the delegate interp_method is executed directly. If it's an
			 * invoke make sure we replace it with the appropriate delegate invoke wrapper.
			 *
			 * FIXME We should do this later, when we also know the delegate on which the
			 * target method is called.
			 */
			del->interp_method = mono_interp_get_imethod (del->object.vtable->domain, mono_marshal_get_delegate_invoke (method, NULL), error);
			mono_error_assert_ok (error);
		}
	}

	if (!((InterpMethod *) del->interp_method)->transformed && method_is_dynamic (method)) {
		/* Return any errors from method compilation */
		mono_interp_transform_method ((InterpMethod *) del->interp_method, get_context (), error);
		return_if_nok (error);
	}
}

static void
interp_delegate_ctor (MonoObjectHandle this_obj, MonoObjectHandle target, gpointer addr, MonoError *error)
{
	/*
	 * addr is the result of an LDFTN opcode, i.e. an InterpMethod
	 */
	InterpMethod *imethod = (InterpMethod*)addr;

	if (!(imethod->method->flags & METHOD_ATTRIBUTE_STATIC)) {
		MonoMethod *invoke = mono_get_delegate_invoke_internal (mono_handle_class (this_obj));
		/* virtual invoke delegates must not have null check */
		if (mono_method_signature_internal (imethod->method)->param_count == mono_method_signature_internal (invoke)->param_count
				&& MONO_HANDLE_IS_NULL (target)) {
			mono_error_set_argument (error, "this", "Delegate to an instance method cannot have null 'this'");
			return;
		}
	}

	g_assert (imethod->method);
	gpointer entry = mini_get_interp_callbacks ()->create_method_pointer (imethod->method, FALSE, error);
	return_if_nok (error);

	MONO_HANDLE_SETVAL (MONO_HANDLE_CAST (MonoDelegate, this_obj), interp_method, gpointer, imethod);

	mono_delegate_ctor (this_obj, target, entry, imethod->method, error);
}

/*
 * From the spec:
 * runtime specifies that the implementation of the method is automatically
 * provided by the runtime and is primarily used for the methods of delegates.
 */
#ifndef ENABLE_NETCORE
static MONO_NEVER_INLINE MonoException*
ves_imethod (InterpFrame *frame, MonoMethod *method, MonoMethodSignature *sig, stackval *sp, stackval *retval)
{
	const char *name = method->name;
	mono_class_init_internal (method->klass);

	if (method->klass == mono_defaults.array_class) {
		if (!strcmp (name, "UnsafeMov")) {
			/* TODO: layout checks */
			stackval_from_data (sig->ret, retval, (char*) sp, FALSE);
			return NULL;
		}
		if (!strcmp (name, "UnsafeLoad"))
			return ves_array_get (frame, sp, retval, sig, FALSE);
	}
	
	g_error ("Don't know how to exec runtime method %s.%s::%s", 
			m_class_get_name_space (method->klass), m_class_get_name (method->klass),
			method->name);
}
#endif

#if DEBUG_INTERP
static char*
dump_stack (stackval *stack, stackval *sp)
{
	stackval *s = stack;
	GString *str = g_string_new ("");
	
	if (sp == stack)
		return g_string_free (str, FALSE);
	
	while (s < sp) {
		g_string_append_printf (str, "[%p (%" PRId64 ")] ", s->data.l, (gint64)s->data.l);
		++s;
	}
	return g_string_free (str, FALSE);
}

static void
dump_stackval (GString *str, stackval *s, MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_BOOLEAN:
		g_string_append_printf (str, "[%d] ", s->data.i);
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		g_string_append_printf (str, "[%p] ", s->data.p);
		break;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass))
			g_string_append_printf (str, "[%d] ", s->data.i);
		else
			g_string_append_printf (str, "[vt:%p] ", s->data.p);
		break;
	case MONO_TYPE_R4:
		g_string_append_printf (str, "[%g] ", s->data.f_r4);
		break;
	case MONO_TYPE_R8:
		g_string_append_printf (str, "[%g] ", s->data.f);
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	default: {
		GString *res = g_string_new ("");
		mono_type_get_desc (res, type, TRUE);
		g_string_append_printf (str, "[{%s} %" PRId64 "/0x%0" PRIx64 "] ", res->str, (gint64)s->data.l, (guint64)s->data.l);
		g_string_free (res, TRUE);
		break;
	}
	}
}

static char*
dump_retval (InterpFrame *inv)
{
	GString *str = g_string_new ("");
	MonoType *ret = mono_method_signature_internal (inv->imethod->method)->ret;

	if (ret->type != MONO_TYPE_VOID)
		dump_stackval (str, inv->retval, ret);

	return g_string_free (str, FALSE);
}

static char*
dump_args (InterpFrame *inv)
{
	GString *str = g_string_new ("");
	int i;
	MonoMethodSignature *signature = mono_method_signature_internal (inv->imethod->method);
	
	if (signature->param_count == 0 && !signature->hasthis)
		return g_string_free (str, FALSE);

	if (signature->hasthis) {
		MonoMethod *method = inv->imethod->method;
		dump_stackval (str, inv->stack, m_class_get_byval_arg (method->klass));
	}

	for (i = 0; i < signature->param_count; ++i)
		dump_stackval (str, inv->stack + (!!signature->hasthis) + i, signature->params [i]);

	return g_string_free (str, FALSE);
}
#endif

#define CHECK_ADD_OVERFLOW(a,b) \
	(gint32)(b) >= 0 ? (gint32)(G_MAXINT32) - (gint32)(b) < (gint32)(a) ? -1 : 0	\
	: (gint32)(G_MININT32) - (gint32)(b) > (gint32)(a) ? +1 : 0

#define CHECK_SUB_OVERFLOW(a,b) \
	(gint32)(b) < 0 ? (gint32)(G_MAXINT32) + (gint32)(b) < (gint32)(a) ? -1 : 0	\
	: (gint32)(G_MININT32) + (gint32)(b) > (gint32)(a) ? +1 : 0

#define CHECK_ADD_OVERFLOW_UN(a,b) \
	(guint32)(G_MAXUINT32) - (guint32)(b) < (guint32)(a) ? -1 : 0

#define CHECK_SUB_OVERFLOW_UN(a,b) \
	(guint32)(a) < (guint32)(b) ? -1 : 0

#define CHECK_ADD_OVERFLOW64(a,b) \
	(gint64)(b) >= 0 ? (gint64)(G_MAXINT64) - (gint64)(b) < (gint64)(a) ? -1 : 0	\
	: (gint64)(G_MININT64) - (gint64)(b) > (gint64)(a) ? +1 : 0

#define CHECK_SUB_OVERFLOW64(a,b) \
	(gint64)(b) < 0 ? (gint64)(G_MAXINT64) + (gint64)(b) < (gint64)(a) ? -1 : 0	\
	: (gint64)(G_MININT64) + (gint64)(b) > (gint64)(a) ? +1 : 0

#define CHECK_ADD_OVERFLOW64_UN(a,b) \
	(guint64)(G_MAXUINT64) - (guint64)(b) < (guint64)(a) ? -1 : 0

#define CHECK_SUB_OVERFLOW64_UN(a,b) \
	(guint64)(a) < (guint64)(b) ? -1 : 0

#if SIZEOF_VOID_P == 4
#define CHECK_ADD_OVERFLOW_NAT(a,b) CHECK_ADD_OVERFLOW(a,b)
#define CHECK_ADD_OVERFLOW_NAT_UN(a,b) CHECK_ADD_OVERFLOW_UN(a,b)
#else
#define CHECK_ADD_OVERFLOW_NAT(a,b) CHECK_ADD_OVERFLOW64(a,b)
#define CHECK_ADD_OVERFLOW_NAT_UN(a,b) CHECK_ADD_OVERFLOW64_UN(a,b)
#endif

/* Resolves to TRUE if the operands would overflow */
#define CHECK_MUL_OVERFLOW(a,b) \
	((gint32)(a) == 0) || ((gint32)(b) == 0) ? 0 : \
	(((gint32)(a) > 0) && ((gint32)(b) == -1)) ? FALSE : \
	(((gint32)(a) < 0) && ((gint32)(b) == -1)) ? (a == G_MININT32) : \
	(((gint32)(a) > 0) && ((gint32)(b) > 0)) ? (gint32)(a) > ((G_MAXINT32) / (gint32)(b)) : \
	(((gint32)(a) > 0) && ((gint32)(b) < 0)) ? (gint32)(a) > ((G_MININT32) / (gint32)(b)) : \
	(((gint32)(a) < 0) && ((gint32)(b) > 0)) ? (gint32)(a) < ((G_MININT32) / (gint32)(b)) : \
	(gint32)(a) < ((G_MAXINT32) / (gint32)(b))

#define CHECK_MUL_OVERFLOW_UN(a,b) \
	((guint32)(a) == 0) || ((guint32)(b) == 0) ? 0 : \
	(guint32)(b) > ((G_MAXUINT32) / (guint32)(a))

#define CHECK_MUL_OVERFLOW64(a,b) \
	((gint64)(a) == 0) || ((gint64)(b) == 0) ? 0 : \
	(((gint64)(a) > 0) && ((gint64)(b) == -1)) ? FALSE : \
	(((gint64)(a) < 0) && ((gint64)(b) == -1)) ? (a == G_MININT64) : \
	(((gint64)(a) > 0) && ((gint64)(b) > 0)) ? (gint64)(a) > ((G_MAXINT64) / (gint64)(b)) : \
	(((gint64)(a) > 0) && ((gint64)(b) < 0)) ? (gint64)(a) > ((G_MININT64) / (gint64)(b)) : \
	(((gint64)(a) < 0) && ((gint64)(b) > 0)) ? (gint64)(a) < ((G_MININT64) / (gint64)(b)) : \
	(gint64)(a) < ((G_MAXINT64) / (gint64)(b))

#define CHECK_MUL_OVERFLOW64_UN(a,b) \
	((guint64)(a) == 0) || ((guint64)(b) == 0) ? 0 : \
	(guint64)(b) > ((G_MAXUINT64) / (guint64)(a))

#if SIZEOF_VOID_P == 4
#define CHECK_MUL_OVERFLOW_NAT(a,b) CHECK_MUL_OVERFLOW(a,b)
#define CHECK_MUL_OVERFLOW_NAT_UN(a,b) CHECK_MUL_OVERFLOW_UN(a,b)
#else
#define CHECK_MUL_OVERFLOW_NAT(a,b) CHECK_MUL_OVERFLOW64(a,b)
#define CHECK_MUL_OVERFLOW_NAT_UN(a,b) CHECK_MUL_OVERFLOW64_UN(a,b)
#endif

// Do not inline in case order of frame addresses matters.
static MONO_NEVER_INLINE MonoObject*
interp_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error)
{
	ThreadContext *context = get_context ();
	MonoMethodSignature *sig = mono_method_signature_internal (method);
	MonoClass *klass = mono_class_from_mono_type_internal (sig->ret);
	stackval result;
	stackval *sp = (stackval*)context->stack_pointer;
	MonoMethod *target_method = method;

	error_init (error);
	if (exc)
		*exc = NULL;

	MonoDomain *domain = mono_domain_get ();

	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
		target_method = mono_marshal_get_native_wrapper (target_method, FALSE, FALSE);
	MonoMethod *invoke_wrapper = mono_marshal_get_runtime_invoke_full (target_method, FALSE, TRUE);

	//* <code>MonoObject *runtime_invoke (MonoObject *this_obj, void **params, MonoObject **exc, void* method)</code>

	result.data.vt = alloca (mono_class_instance_size (klass));
	if (sig->hasthis)
		sp [0].data.p = obj;
	else
		sp [0].data.p = NULL;
	sp [1].data.p = params;
	sp [2].data.p = exc;
	sp [3].data.p = target_method;

	InterpMethod *imethod = mono_interp_get_imethod (domain, invoke_wrapper, error);
	mono_error_assert_ok (error);

	InterpFrame frame = {0};
	frame.imethod = imethod;
	frame.stack = sp;
	frame.retval = &result;

	// The method to execute might not be transformed yet, so we don't know how much stack
	// it uses. We bump the stack_pointer here so any code triggered by method compilation
	// will not attempt to use the space that we used to push the args for this method.
	// The real top of stack for this method will be set in interp_exec_method once the
	// method is transformed.
	context->stack_pointer = (guchar*)(sp + 4);

	interp_exec_method (&frame, context, NULL);

	context->stack_pointer = (guchar*)sp;

	if (context->has_resume_state) {
		/*
		 * This can happen on wasm where native frames cannot be skipped during EH.
		 * EH processing will continue when control returns to the interpreter.
		 */
		return NULL;
	}
	return (MonoObject*)result.data.p;
}

typedef struct {
	InterpMethod *rmethod;
	gpointer this_arg;
	gpointer res;
	gpointer args [16];
	gpointer *many_args;
} InterpEntryData;

/* Main function for entering the interpreter from compiled code */
// Do not inline in case order of frame addresses matters.
static MONO_NEVER_INLINE void
interp_entry (InterpEntryData *data)
{
	InterpMethod *rmethod;
	ThreadContext *context;
	stackval *sp;
	stackval result;
	MonoMethod *method;
	MonoMethodSignature *sig;
	MonoType *type;
	gpointer orig_domain = NULL, attach_cookie;
	int i;

	if ((gsize)data->rmethod & 1) {
		/* Unbox */
		data->this_arg = mono_object_unbox_internal ((MonoObject*)data->this_arg);
		data->rmethod = (InterpMethod*)(gpointer)((gsize)data->rmethod & ~1);
	}
	rmethod = data->rmethod;

	if (rmethod->needs_thread_attach)
		orig_domain = mono_threads_attach_coop (mono_domain_get (), &attach_cookie);

	context = get_context ();
	sp = (stackval*)context->stack_pointer;

	method = rmethod->method;
	sig = mono_method_signature_internal (method);

	// FIXME: Optimize this

	if (sig->hasthis)
		sp [0].data.p = data->this_arg;

	gpointer *params;
	if (data->many_args)
		params = data->many_args;
	else
		params = data->args;
	for (i = 0; i < sig->param_count; ++i) {
		int a_index = i + (sig->hasthis ? 1 : 0);
		if (sig->params [i]->byref) {
			sp [a_index].data.p = params [i];
			continue;
		}
		type = rmethod->param_types [i];
		switch (type->type) {
		case MONO_TYPE_VALUETYPE:
			sp [a_index].data.p = params [i];
			break;
		case MONO_TYPE_GENERICINST:
			if (MONO_TYPE_IS_REFERENCE (type))
				sp [a_index].data.p = *(gpointer*)params [i];
			else
				sp [a_index].data.vt = params [i];
			break;
		default:
			stackval_from_data (type, &sp [a_index], params [i], FALSE);
			break;
		}
	}

	memset (&result, 0, sizeof (result));

	InterpFrame frame = {0};
	frame.imethod = data->rmethod;
	frame.stack = sp;
	frame.retval = &result;

	type = rmethod->rtype;
	switch (type->type) {
	case MONO_TYPE_GENERICINST:
		if (!MONO_TYPE_IS_REFERENCE (type))
			result.data.vt = data->res;
		break;
	case MONO_TYPE_VALUETYPE:
		result.data.vt = data->res;
		break;
	default:
		break;
	}

	context->stack_pointer = (guchar*)(sp + sig->hasthis + sig->param_count);

	interp_exec_method (&frame, context, NULL);

	context->stack_pointer = (guchar*)sp;

	g_assert (!context->has_resume_state);

	if (rmethod->needs_thread_attach)
		mono_threads_detach_coop (orig_domain, &attach_cookie);

	if (mono_llvm_only) {
		if (context->has_resume_state)
			mono_llvm_reraise_exception ((MonoException*)mono_gchandle_get_target_internal (context->exc_gchandle));
	} else {
		g_assert (!context->has_resume_state);
	}

	type = rmethod->rtype;
	switch (type->type) {
	case MONO_TYPE_VOID:
		break;
	case MONO_TYPE_OBJECT:
		/* No need for a write barrier */
		*(MonoObject**)data->res = (MonoObject*)result.data.p;
		break;
	case MONO_TYPE_GENERICINST:
		if (MONO_TYPE_IS_REFERENCE (type)) {
			*(MonoObject**)data->res = (MonoObject*)result.data.p;
		} else {
			/* Already set before the call */
		}
		break;
	case MONO_TYPE_VALUETYPE:
		/* Already set before the call */
		break;
	default:
		stackval_to_data (type, &result, data->res, FALSE);
		break;
	}
}

static stackval *
do_icall (MonoMethodSignature *sig, int op, stackval *sp, gpointer ptr, gboolean save_last_error)
{
#ifdef ENABLE_NETCORE
	if (save_last_error)
		mono_marshal_clear_last_error ();
#endif

	switch (op) {
	case MINT_ICALL_V_V: {
		typedef void (*T)(void);
		T func = (T)ptr;
        	func ();
		break;
	}
	case MINT_ICALL_V_P: {
		typedef gpointer (*T)(void);
		T func = (T)ptr;
		sp++;
		sp [-1].data.p = func ();
		break;
	}
	case MINT_ICALL_P_V: {
		typedef void (*T)(gpointer);
		T func = (T)ptr;
        	func (sp [-1].data.p);
		sp --;
		break;
	}
	case MINT_ICALL_P_P: {
		typedef gpointer (*T)(gpointer);
		T func = (T)ptr;
		sp [-1].data.p = func (sp [-1].data.p);
		break;
	}
	case MINT_ICALL_PP_V: {
		typedef void (*T)(gpointer,gpointer);
		T func = (T)ptr;
		sp -= 2;
		func (sp [0].data.p, sp [1].data.p);
		break;
	}
	case MINT_ICALL_PP_P: {
		typedef gpointer (*T)(gpointer,gpointer);
		T func = (T)ptr;
		--sp;
		sp [-1].data.p = func (sp [-1].data.p, sp [0].data.p);
		break;
	}
	case MINT_ICALL_PPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer);
		T func = (T)ptr;
		sp -= 3;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p);
		break;
	}
	case MINT_ICALL_PPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer);
		T func = (T)ptr;
		sp -= 2;
		sp [-1].data.p = func (sp [-1].data.p, sp [0].data.p, sp [1].data.p);
		break;
	}
	case MINT_ICALL_PPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		sp -= 4;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p);
		break;
	}
	case MINT_ICALL_PPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		sp -= 3;
		sp [-1].data.p = func (sp [-1].data.p, sp [0].data.p, sp [1].data.p, sp [2].data.p);
		break;
	}
	case MINT_ICALL_PPPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		sp -= 5;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p);
		break;
	}
	case MINT_ICALL_PPPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		sp -= 4;
		sp [-1].data.p = func (sp [-1].data.p, sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p);
		break;
	}
	case MINT_ICALL_PPPPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		sp -= 6;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p, sp [5].data.p);
		break;
	}
	case MINT_ICALL_PPPPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		sp -= 5;
		sp [-1].data.p = func (sp [-1].data.p, sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p);
		break;
	}
	default:
		g_assert_not_reached ();
	}

	if (save_last_error)
		mono_marshal_set_last_error ();

	/* convert the native representation to the stackval representation */
	if (sig)
		stackval_from_data (sig->ret, &sp [-1], (char*) &sp [-1].data.p, sig->pinvoke);

	return sp;
}

/* MONO_NO_OPTIMIZATION is needed due to usage of INTERP_PUSH_LMF_WITH_CTX. */
#ifdef _MSC_VER
#pragma optimize ("", off)
#endif
// Do not inline in case order of frame addresses matters, and maybe other reasons.
static MONO_NO_OPTIMIZATION MONO_NEVER_INLINE stackval *
do_icall_wrapper (InterpFrame *frame, MonoMethodSignature *sig, int op, stackval *sp, gpointer ptr, gboolean save_last_error)
{
	MonoLMFExt ext;
	INTERP_PUSH_LMF_WITH_CTX (frame, ext, exit_icall);

	sp = do_icall (sig, op, sp, ptr, save_last_error);

	interp_pop_lmf (&ext);

	goto exit_icall; // prevent unused label warning in some configurations
exit_icall:
	return sp;
}
#ifdef _MSC_VER
#pragma optimize ("", on)
#endif

typedef struct {
	int pindex;
	gpointer jit_wrapper;
	gpointer *args;
	MonoFtnDesc ftndesc;
} JitCallCbData;

/* Callback called by mono_llvm_cpp_catch_exception () */
static void
jit_call_cb (gpointer arg)
{
	JitCallCbData *cb_data = (JitCallCbData*)arg;
	gpointer jit_wrapper = cb_data->jit_wrapper;
	int pindex = cb_data->pindex;
	gpointer *args = cb_data->args;
	MonoFtnDesc *ftndesc = &cb_data->ftndesc;

	switch (pindex) {
	case 0: {
		typedef void (*T)(gpointer);
		T func = (T)jit_wrapper;

		func (ftndesc);
		break;
	}
	case 1: {
		typedef void (*T)(gpointer, gpointer);
		T func = (T)jit_wrapper;

		func (args [0], ftndesc);
		break;
	}
	case 2: {
		typedef void (*T)(gpointer, gpointer, gpointer);
		T func = (T)jit_wrapper;

		func (args [0], args [1], ftndesc);
		break;
	}
	case 3: {
		typedef void (*T)(gpointer, gpointer, gpointer, gpointer);
		T func = (T)jit_wrapper;

		func (args [0], args [1], args [2], ftndesc);
		break;
	}
	case 4: {
		typedef void (*T)(gpointer, gpointer, gpointer, gpointer, gpointer);
		T func = (T)jit_wrapper;

		func (args [0], args [1], args [2], args [3], ftndesc);
		break;
	}
	case 5: {
		typedef void (*T)(gpointer, gpointer, gpointer, gpointer, gpointer, gpointer);
		T func = (T)jit_wrapper;

		func (args [0], args [1], args [2], args [3], args [4], ftndesc);
		break;
	}
	case 6: {
		typedef void (*T)(gpointer, gpointer, gpointer, gpointer, gpointer, gpointer, gpointer);
		T func = (T)jit_wrapper;

		func (args [0], args [1], args [2], args [3], args [4], args [5], ftndesc);
		break;
	}
	case 7: {
		typedef void (*T)(gpointer, gpointer, gpointer, gpointer, gpointer, gpointer, gpointer, gpointer);
		T func = (T)jit_wrapper;

		func (args [0], args [1], args [2], args [3], args [4], args [5], args [6], ftndesc);
		break;
	}
	case 8: {
		typedef void (*T)(gpointer, gpointer, gpointer, gpointer, gpointer, gpointer, gpointer, gpointer, gpointer);
		T func = (T)jit_wrapper;

		func (args [0], args [1], args [2], args [3], args [4], args [5], args [6], args [7], ftndesc);
		break;
	}
	default:
		g_assert_not_reached ();
		break;
	}
}

enum {
	/* Pass stackval->data.p */
	JIT_ARG_BYVAL,
	/* Pass &stackval->data.p */
	JIT_ARG_BYREF
};

enum {
       JIT_RET_VOID,
       JIT_RET_SCALAR,
       JIT_RET_VTYPE
};

typedef struct _JitCallInfo JitCallInfo;
struct _JitCallInfo {
	gpointer addr;
	gpointer extra_arg;
	gpointer wrapper;
	MonoMethodSignature *sig;
	guint8 *arginfo;
	gint32 vt_res_size;
	int ret_mt;
};

static MONO_NEVER_INLINE void
init_jit_call_info (InterpMethod *rmethod, MonoError *error)
{
	MonoMethodSignature *sig;
	JitCallInfo *cinfo;

	//printf ("jit_call: %s\n", mono_method_full_name (rmethod->method, 1));

	MonoMethod *method = rmethod->method;

	// FIXME: Memory management
	cinfo = g_new0 (JitCallInfo, 1);

	sig = mono_method_signature_internal (method);
	g_assert (sig);

	MonoMethod *wrapper = mini_get_gsharedvt_out_sig_wrapper (sig);
	//printf ("J: %s %s\n", mono_method_full_name (method, 1), mono_method_full_name (wrapper, 1));

	gpointer jit_wrapper = mono_jit_compile_method_jit_only (wrapper, error);
	mono_error_assert_ok (error);

	gpointer addr = mono_jit_compile_method_jit_only (method, error);
	return_if_nok (error);
	g_assert (addr);

	if (mono_llvm_only)
		cinfo->addr = mini_llvmonly_add_method_wrappers (method, addr, FALSE, FALSE, &cinfo->extra_arg);
	else
		cinfo->addr = addr;
	cinfo->sig = sig;
	cinfo->wrapper = jit_wrapper;

	if (sig->ret->type != MONO_TYPE_VOID) {
		int mt = mint_type (sig->ret);
		if (mt == MINT_TYPE_VT) {
			MonoClass *klass = mono_class_from_mono_type_internal (sig->ret);
			/*
			 * We cache this size here, instead of the instruction stream of the
			 * calling instruction, to save space for common callvirt instructions
			 * that could end up doing a jit call.
			 */
			gint32 size = mono_class_value_size (klass, NULL);
			cinfo->vt_res_size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
		}
		cinfo->ret_mt = mt;
	} else {
		cinfo->ret_mt = -1;
	}

	if (sig->param_count) {
		cinfo->arginfo = g_new0 (guint8, sig->param_count);

		for (int i = 0; i < rmethod->param_count; ++i) {
			MonoType *t = rmethod->param_types [i];
			int mt = mint_type (t);
			if (sig->params [i]->byref) {
				cinfo->arginfo [i] = JIT_ARG_BYVAL;
			} else if (mt == MINT_TYPE_VT) {
				cinfo->arginfo [i] = JIT_ARG_BYVAL;
			} else if (mt == MINT_TYPE_O) {
				cinfo->arginfo [i] = JIT_ARG_BYREF;
			} else {
				/* stackval->data is an union */
				cinfo->arginfo [i] = JIT_ARG_BYREF;
			}
		}
	}

	mono_memory_barrier ();
	rmethod->jit_call_info = cinfo;
}

static MONO_NEVER_INLINE void
do_jit_call (stackval *sp, unsigned char *vt_sp, InterpFrame *frame, InterpMethod *rmethod, MonoError *error)
{
	guint8 res_buf [256];
	MonoLMFExt ext;
	JitCallInfo *cinfo;

	//printf ("jit_call: %s\n", mono_method_full_name (rmethod->method, 1));

	/*
	 * Call JITted code through a gsharedvt_out wrapper. These wrappers receive every argument
	 * by ref and return a return value using an explicit return value argument.
	 */
	if (G_UNLIKELY (!rmethod->jit_call_info)) {
		init_jit_call_info (rmethod, error);
		mono_error_assert_ok (error);
	}
	cinfo = (JitCallInfo*)rmethod->jit_call_info;

	/*
	 * Convert the arguments on the interpeter stack to the format expected by the gsharedvt_out wrapper.
	 */
	gpointer args [32];
	int pindex = 0;
	int stack_index = 0;
	if (rmethod->hasthis) {
		args [pindex ++] = sp [0].data.p;
		stack_index ++;
	}
	switch (cinfo->ret_mt) {
	case -1:
		break;
	case MINT_TYPE_VT:
		args [pindex ++] = vt_sp;
		break;
	default:
		args [pindex ++] = res_buf;
		break;
	}
	for (int i = 0; i < rmethod->param_count; ++i) {
		stackval *sval = &sp [stack_index + i];
		if (cinfo->arginfo [i] == JIT_ARG_BYVAL)
			args [pindex ++] = sval->data.p;
		else
			/* data is an union, so can use 'p' for all types */
			args [pindex ++] = &sval->data.p;
	}

	JitCallCbData cb_data;
	memset (&cb_data, 0, sizeof (cb_data));
	cb_data.jit_wrapper = cinfo->wrapper;
	cb_data.pindex = pindex;
	cb_data.args = args;
	cb_data.ftndesc.addr = cinfo->addr;
	cb_data.ftndesc.arg = cinfo->extra_arg;

	interp_push_lmf (&ext, frame);
	gboolean thrown = FALSE;
	if (mono_aot_mode == MONO_AOT_MODE_LLVMONLY_INTERP) {
		/* Catch the exception thrown by the native code using a try-catch */
		mono_llvm_cpp_catch_exception (jit_call_cb, &cb_data, &thrown);
	} else {
		jit_call_cb (&cb_data);
	}
	interp_pop_lmf (&ext);
	if (thrown) {
		MonoObject *obj = mono_llvm_load_exception ();
		g_assert (obj);
		mono_error_set_exception_instance (error, (MonoException*)obj);
		return;
	}

	if (cinfo->ret_mt != -1) {
		switch (cinfo->ret_mt) {
		case MINT_TYPE_O:
			sp->data.p = *(gpointer*)res_buf;
			break;
		case MINT_TYPE_I1:
			sp->data.i = *(gint8*)res_buf;
			break;
		case MINT_TYPE_U1:
			sp->data.i = *(guint8*)res_buf;
			break;
		case MINT_TYPE_I2:
			sp->data.i = *(gint16*)res_buf;
			break;
		case MINT_TYPE_U2:
			sp->data.i = *(guint16*)res_buf;
			break;
		case MINT_TYPE_I4:
			sp->data.i = *(gint32*)res_buf;
			break;
		case MINT_TYPE_I8:
			sp->data.l = *(gint64*)res_buf;
			break;
		case MINT_TYPE_R4:
			sp->data.f_r4 = *(float*)res_buf;
			break;
		case MINT_TYPE_R8:
			sp->data.f = *(double*)res_buf;
			break;
		case MINT_TYPE_VT:
			/* The result was written to vt_sp */
			sp->data.p = vt_sp;
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

static MONO_NEVER_INLINE void
do_debugger_tramp (void (*tramp) (void), InterpFrame *frame)
{
	MonoLMFExt ext;
	interp_push_lmf (&ext, frame);
	tramp ();
	interp_pop_lmf (&ext);
}

static MONO_NEVER_INLINE MonoException*
do_transform_method (InterpFrame *frame, ThreadContext *context)
{
	MonoLMFExt ext;
	/* Don't push lmf if we have no interp data */
	gboolean push_lmf = frame->parent != NULL;
	ERROR_DECL (error);

#if DEBUG_INTERP
	char *mn = mono_method_full_name (frame->imethod->method, TRUE);
	g_print ("(%p) Transforming %s\n", mono_thread_internal_current (), mn);
	g_free (mn);
#endif

	/* Use the parent frame as the current frame is not complete yet */
	if (push_lmf)
		interp_push_lmf (&ext, frame->parent);

	mono_interp_transform_method (frame->imethod, context, error);

	if (push_lmf)
		interp_pop_lmf (&ext);

	return mono_error_convert_to_exception (error);
}

static void
copy_varargs_vtstack (MonoMethodSignature *csig, stackval *sp, guchar *vt_sp_start)
{
	stackval *first_arg = sp - csig->param_count;
	guchar *vt_sp = vt_sp_start;

	/*
	 * We need to have the varargs linearly on the stack so the ArgIterator
	 * can iterate over them. We pass the signature first and then copy them
	 * one by one on the vtstack. The callee (MINT_ARGLIST) will be able to
	 * find this space by adding the current vt_sp pointer in the parent frame
	 * with the amount of vtstack space used by the parameters.
	 */
	*(gpointer*)vt_sp = csig;
	vt_sp += sizeof (gpointer);

	for (int i = csig->sentinelpos; i < csig->param_count; i++) {
		int align, arg_size;
		arg_size = mono_type_stack_size (csig->params [i], &align);
		vt_sp = (guchar*)ALIGN_PTR_TO (vt_sp, align);

		stackval_to_data (csig->params [i], &first_arg [i], vt_sp, FALSE);
		vt_sp += arg_size;
	}
}

/*
 * These functions are the entry points into the interpreter from compiled code.
 * They are called by the interp_in wrappers. They have the following signature:
 * void (<optional this_arg>, <optional retval pointer>, <arg1>, ..., <argn>, <method ptr>)
 * They pack up their arguments into an InterpEntryData structure and call interp_entry ().
 * It would be possible for the wrappers to pack up the arguments etc, but that would make them bigger, and there are
 * more wrappers then these functions.
 * this/static * ret/void * 16 arguments -> 64 functions.
 */

#define INTERP_ENTRY_BASE(_method, _this_arg, _res) \
	InterpEntryData data; \
	(data).rmethod = (_method); \
	(data).res = (_res); \
	(data).this_arg = (_this_arg); \
	(data).many_args = NULL;

#define INTERP_ENTRY0(_this_arg, _res, _method) {	\
	INTERP_ENTRY_BASE (_method, _this_arg, _res); \
	interp_entry (&data); \
	}
#define INTERP_ENTRY1(_this_arg, _res, _method) {	  \
	INTERP_ENTRY_BASE (_method, _this_arg, _res); \
	(data).args [0] = arg1; \
	interp_entry (&data); \
	}
#define INTERP_ENTRY2(_this_arg, _res, _method) {  \
	INTERP_ENTRY_BASE (_method, _this_arg, _res); \
	(data).args [0] = arg1; \
	(data).args [1] = arg2; \
	interp_entry (&data); \
	}
#define INTERP_ENTRY3(_this_arg, _res, _method) { \
	INTERP_ENTRY_BASE (_method, _this_arg, _res); \
	(data).args [0] = arg1; \
	(data).args [1] = arg2; \
	(data).args [2] = arg3; \
	interp_entry (&data); \
	}
#define INTERP_ENTRY4(_this_arg, _res, _method) {	\
	INTERP_ENTRY_BASE (_method, _this_arg, _res); \
	(data).args [0] = arg1; \
	(data).args [1] = arg2; \
	(data).args [2] = arg3; \
	(data).args [3] = arg4; \
	interp_entry (&data); \
	}
#define INTERP_ENTRY5(_this_arg, _res, _method) {	\
	INTERP_ENTRY_BASE (_method, _this_arg, _res); \
	(data).args [0] = arg1; \
	(data).args [1] = arg2; \
	(data).args [2] = arg3; \
	(data).args [3] = arg4; \
	(data).args [4] = arg5; \
	interp_entry (&data); \
	}
#define INTERP_ENTRY6(_this_arg, _res, _method) {	\
	INTERP_ENTRY_BASE (_method, _this_arg, _res); \
	(data).args [0] = arg1; \
	(data).args [1] = arg2; \
	(data).args [2] = arg3; \
	(data).args [3] = arg4; \
	(data).args [4] = arg5; \
	(data).args [5] = arg6; \
	interp_entry (&data); \
	}
#define INTERP_ENTRY7(_this_arg, _res, _method) {	\
	INTERP_ENTRY_BASE (_method, _this_arg, _res); \
	(data).args [0] = arg1; \
	(data).args [1] = arg2; \
	(data).args [2] = arg3; \
	(data).args [3] = arg4; \
	(data).args [4] = arg5; \
	(data).args [5] = arg6; \
	(data).args [6] = arg7; \
	interp_entry (&data); \
	}
#define INTERP_ENTRY8(_this_arg, _res, _method) {	\
	INTERP_ENTRY_BASE (_method, _this_arg, _res); \
	(data).args [0] = arg1; \
	(data).args [1] = arg2; \
	(data).args [2] = arg3; \
	(data).args [3] = arg4; \
	(data).args [4] = arg5; \
	(data).args [5] = arg6; \
	(data).args [6] = arg7; \
	(data).args [7] = arg8; \
	interp_entry (&data); \
	}

#define ARGLIST0 InterpMethod *rmethod
#define ARGLIST1 gpointer arg1, InterpMethod *rmethod
#define ARGLIST2 gpointer arg1, gpointer arg2, InterpMethod *rmethod
#define ARGLIST3 gpointer arg1, gpointer arg2, gpointer arg3, InterpMethod *rmethod
#define ARGLIST4 gpointer arg1, gpointer arg2, gpointer arg3, gpointer arg4, InterpMethod *rmethod
#define ARGLIST5 gpointer arg1, gpointer arg2, gpointer arg3, gpointer arg4, gpointer arg5, InterpMethod *rmethod
#define ARGLIST6 gpointer arg1, gpointer arg2, gpointer arg3, gpointer arg4, gpointer arg5, gpointer arg6, InterpMethod *rmethod
#define ARGLIST7 gpointer arg1, gpointer arg2, gpointer arg3, gpointer arg4, gpointer arg5, gpointer arg6, gpointer arg7, InterpMethod *rmethod
#define ARGLIST8 gpointer arg1, gpointer arg2, gpointer arg3, gpointer arg4, gpointer arg5, gpointer arg6, gpointer arg7, gpointer arg8, InterpMethod *rmethod

static void interp_entry_static_0 (ARGLIST0) INTERP_ENTRY0 (NULL, NULL, rmethod)
static void interp_entry_static_1 (ARGLIST1) INTERP_ENTRY1 (NULL, NULL, rmethod)
static void interp_entry_static_2 (ARGLIST2) INTERP_ENTRY2 (NULL, NULL, rmethod)
static void interp_entry_static_3 (ARGLIST3) INTERP_ENTRY3 (NULL, NULL, rmethod)
static void interp_entry_static_4 (ARGLIST4) INTERP_ENTRY4 (NULL, NULL, rmethod)
static void interp_entry_static_5 (ARGLIST5) INTERP_ENTRY5 (NULL, NULL, rmethod)
static void interp_entry_static_6 (ARGLIST6) INTERP_ENTRY6 (NULL, NULL, rmethod)
static void interp_entry_static_7 (ARGLIST7) INTERP_ENTRY7 (NULL, NULL, rmethod)
static void interp_entry_static_8 (ARGLIST8) INTERP_ENTRY8 (NULL, NULL, rmethod)
static void interp_entry_static_ret_0 (gpointer res, ARGLIST0) INTERP_ENTRY0 (NULL, res, rmethod)
static void interp_entry_static_ret_1 (gpointer res, ARGLIST1) INTERP_ENTRY1 (NULL, res, rmethod)
static void interp_entry_static_ret_2 (gpointer res, ARGLIST2) INTERP_ENTRY2 (NULL, res, rmethod)
static void interp_entry_static_ret_3 (gpointer res, ARGLIST3) INTERP_ENTRY3 (NULL, res, rmethod)
static void interp_entry_static_ret_4 (gpointer res, ARGLIST4) INTERP_ENTRY4 (NULL, res, rmethod)
static void interp_entry_static_ret_5 (gpointer res, ARGLIST5) INTERP_ENTRY5 (NULL, res, rmethod)
static void interp_entry_static_ret_6 (gpointer res, ARGLIST6) INTERP_ENTRY6 (NULL, res, rmethod)
static void interp_entry_static_ret_7 (gpointer res, ARGLIST7) INTERP_ENTRY7 (NULL, res, rmethod)
static void interp_entry_static_ret_8 (gpointer res, ARGLIST8) INTERP_ENTRY8 (NULL, res, rmethod)
static void interp_entry_instance_0 (gpointer this_arg, ARGLIST0) INTERP_ENTRY0 (this_arg, NULL, rmethod)
static void interp_entry_instance_1 (gpointer this_arg, ARGLIST1) INTERP_ENTRY1 (this_arg, NULL, rmethod)
static void interp_entry_instance_2 (gpointer this_arg, ARGLIST2) INTERP_ENTRY2 (this_arg, NULL, rmethod)
static void interp_entry_instance_3 (gpointer this_arg, ARGLIST3) INTERP_ENTRY3 (this_arg, NULL, rmethod)
static void interp_entry_instance_4 (gpointer this_arg, ARGLIST4) INTERP_ENTRY4 (this_arg, NULL, rmethod)
static void interp_entry_instance_5 (gpointer this_arg, ARGLIST5) INTERP_ENTRY5 (this_arg, NULL, rmethod)
static void interp_entry_instance_6 (gpointer this_arg, ARGLIST6) INTERP_ENTRY6 (this_arg, NULL, rmethod)
static void interp_entry_instance_7 (gpointer this_arg, ARGLIST7) INTERP_ENTRY7 (this_arg, NULL, rmethod)
static void interp_entry_instance_8 (gpointer this_arg, ARGLIST8) INTERP_ENTRY8 (this_arg, NULL, rmethod)
static void interp_entry_instance_ret_0 (gpointer this_arg, gpointer res, ARGLIST0) INTERP_ENTRY0 (this_arg, res, rmethod)
static void interp_entry_instance_ret_1 (gpointer this_arg, gpointer res, ARGLIST1) INTERP_ENTRY1 (this_arg, res, rmethod)
static void interp_entry_instance_ret_2 (gpointer this_arg, gpointer res, ARGLIST2) INTERP_ENTRY2 (this_arg, res, rmethod)
static void interp_entry_instance_ret_3 (gpointer this_arg, gpointer res, ARGLIST3) INTERP_ENTRY3 (this_arg, res, rmethod)
static void interp_entry_instance_ret_4 (gpointer this_arg, gpointer res, ARGLIST4) INTERP_ENTRY4 (this_arg, res, rmethod)
static void interp_entry_instance_ret_5 (gpointer this_arg, gpointer res, ARGLIST5) INTERP_ENTRY5 (this_arg, res, rmethod)
static void interp_entry_instance_ret_6 (gpointer this_arg, gpointer res, ARGLIST6) INTERP_ENTRY6 (this_arg, res, rmethod)
static void interp_entry_instance_ret_7 (gpointer this_arg, gpointer res, ARGLIST7) INTERP_ENTRY7 (this_arg, res, rmethod)
static void interp_entry_instance_ret_8 (gpointer this_arg, gpointer res, ARGLIST8) INTERP_ENTRY8 (this_arg, res, rmethod)

#define INTERP_ENTRY_FUNCLIST(type) (gpointer)interp_entry_ ## type ## _0, (gpointer)interp_entry_ ## type ## _1, (gpointer)interp_entry_ ## type ## _2, (gpointer)interp_entry_ ## type ## _3, (gpointer)interp_entry_ ## type ## _4, (gpointer)interp_entry_ ## type ## _5, (gpointer)interp_entry_ ## type ## _6, (gpointer)interp_entry_ ## type ## _7, (gpointer)interp_entry_ ## type ## _8

static gpointer entry_funcs_static [MAX_INTERP_ENTRY_ARGS + 1] = { INTERP_ENTRY_FUNCLIST (static) };
static gpointer entry_funcs_static_ret [MAX_INTERP_ENTRY_ARGS + 1] = { INTERP_ENTRY_FUNCLIST (static_ret) };
static gpointer entry_funcs_instance [MAX_INTERP_ENTRY_ARGS + 1] = { INTERP_ENTRY_FUNCLIST (instance) };
static gpointer entry_funcs_instance_ret [MAX_INTERP_ENTRY_ARGS + 1] = { INTERP_ENTRY_FUNCLIST (instance_ret) };

/* General version for methods with more than MAX_INTERP_ENTRY_ARGS arguments */
static void
interp_entry_general (gpointer this_arg, gpointer res, gpointer *args, gpointer rmethod)
{
	INTERP_ENTRY_BASE ((InterpMethod*)rmethod, this_arg, res);
	data.many_args = args;
	interp_entry (&data);
}

#ifdef MONO_ARCH_HAVE_INTERP_ENTRY_TRAMPOLINE

// inline so we can alloc on stack
#define alloc_storage_for_stackval(s, t, p) do {							\
		if ((t)->type == MONO_TYPE_GENERICINST && !MONO_TYPE_IS_REFERENCE (t)) {		\
			(s)->data.vt = alloca (mono_class_value_size (mono_class_from_mono_type_internal (t), NULL));	\
		} else if ((t)->type == MONO_TYPE_VALUETYPE) {						\
			if (p)										\
				(s)->data.vt = alloca (mono_class_native_size ((t)->data.klass, NULL));	\
			else										\
				(s)->data.vt = alloca (mono_class_value_size ((t)->data.klass, NULL));	\
		}											\
	} while (0)

// Do not inline in case order of frame addresses matters.
static MONO_NEVER_INLINE void
interp_entry_from_trampoline (gpointer ccontext_untyped, gpointer rmethod_untyped)
{
	ThreadContext *context;
	stackval *sp;
	stackval result;
	MonoMethod *method;
	MonoMethodSignature *sig;
	CallContext *ccontext = (CallContext*) ccontext_untyped;
	InterpMethod *rmethod = (InterpMethod*) rmethod_untyped;
	gpointer orig_domain = NULL, attach_cookie;
	int i;

	if (rmethod->needs_thread_attach)
		orig_domain = mono_threads_attach_coop (mono_domain_get (), &attach_cookie);

	context = get_context ();
	sp = (stackval*)context->stack_pointer;

	method = rmethod->method;
	sig = mono_method_signature_internal (method);
	if (method->string_ctor) {
		MonoMethodSignature *newsig = g_alloca (MONO_SIZEOF_METHOD_SIGNATURE + ((sig->param_count + 2) * sizeof (MonoType*)));
		memcpy (newsig, sig, mono_metadata_signature_size (sig));
		newsig->ret = m_class_get_byval_arg (mono_defaults.string_class);
		sig = newsig;
	}

	/* Allocate storage for value types */
	for (i = 0; i < sig->param_count; i++) {
		MonoType *type = sig->params [i];
		alloc_storage_for_stackval (&sp [i + sig->hasthis], type, sig->pinvoke);
	}

	if (sig->ret->type != MONO_TYPE_VOID)
		alloc_storage_for_stackval (&result, sig->ret, sig->pinvoke);

	InterpFrame frame = {0};
	frame.imethod = rmethod;
	frame.stack = sp;
	frame.retval = &result;

	/* Copy the args saved in the trampoline to the frame stack */
	mono_arch_get_native_call_context_args (ccontext, &frame, sig);

	context->stack_pointer = (guchar*)(sp + sig->hasthis + sig->param_count);

	interp_exec_method (&frame, context, NULL);

	context->stack_pointer = (guchar*)sp;
	g_assert (!context->has_resume_state);

	if (rmethod->needs_thread_attach)
		mono_threads_detach_coop (orig_domain, &attach_cookie);

	/* Write back the return value */
	/* 'frame' is still valid */
	mono_arch_set_native_call_context_ret (ccontext, &frame, sig);
}

#else

static void
interp_entry_from_trampoline (gpointer ccontext_untyped, gpointer rmethod_untyped)
{
	g_assert_not_reached ();
}

#endif /* MONO_ARCH_HAVE_INTERP_ENTRY_TRAMPOLINE */

static InterpMethod*
lookup_method_pointer (gpointer addr)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitDomainInfo *info = domain_jit_info (domain);
	InterpMethod *res = NULL;

	mono_domain_lock (domain);
	if (info->interp_method_pointer_hash)
		res = (InterpMethod*)g_hash_table_lookup (info->interp_method_pointer_hash, addr);
	mono_domain_unlock (domain);

	return res;
}

#ifndef MONO_ARCH_HAVE_INTERP_NATIVE_TO_MANAGED
static void
interp_no_native_to_managed (void)
{
	g_error ("interpreter: native-to-managed transition not available on this platform");
}
#endif

static void
no_llvmonly_interp_method_pointer (void)
{
	g_assert_not_reached ();
}

/*
 * interp_create_method_pointer_llvmonly:
 *
 *   Return an ftndesc for entering the interpreter and executing METHOD.
 */
static MonoFtnDesc*
interp_create_method_pointer_llvmonly (MonoMethod *method, gboolean unbox, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();
	gpointer addr, entry_func, entry_wrapper;
	MonoMethodSignature *sig;
	MonoMethod *wrapper;
	MonoJitDomainInfo *info;
	InterpMethod *imethod;

	imethod = mono_interp_get_imethod (domain, method, error);
	return_val_if_nok (error, NULL);

	if (unbox) {
		if (imethod->llvmonly_unbox_entry)
			return (MonoFtnDesc*)imethod->llvmonly_unbox_entry;
	} else {
		if (imethod->jit_entry)
			return (MonoFtnDesc*)imethod->jit_entry;
	}

	sig = mono_method_signature_internal (method);

	/*
	 * The entry functions need access to the method to call, so we have
	 * to use a ftndesc. The caller uses a normal signature, while the
	 * entry functions use a gsharedvt_in signature, so wrap the entry function in
	 * a gsharedvt_in_sig wrapper.
	 * We use a gsharedvt_in_sig wrapper instead of an interp_in wrapper, because they
	 * are mostly the same, and they are already generated. The exception is the
	 * wrappers for methods with more than 8 arguments, those are different.
	 */
	if (sig->param_count > MAX_INTERP_ENTRY_ARGS)
		wrapper = mini_get_interp_in_wrapper (sig);
	else
		wrapper = mini_get_gsharedvt_in_sig_wrapper (sig);

	entry_wrapper = mono_jit_compile_method_jit_only (wrapper, error);
	mono_error_assertf_ok (error, "couldn't compile wrapper \"%s\" for \"%s\"",
			mono_method_get_name_full (wrapper, TRUE, TRUE, MONO_TYPE_NAME_FORMAT_IL),
			mono_method_get_name_full (method,  TRUE, TRUE, MONO_TYPE_NAME_FORMAT_IL));

	if (sig->param_count > MAX_INTERP_ENTRY_ARGS) {
		entry_func = (gpointer)interp_entry_general;
	} else if (sig->hasthis) {
		if (sig->ret->type == MONO_TYPE_VOID)
			entry_func = entry_funcs_instance [sig->param_count];
		else
			entry_func = entry_funcs_instance_ret [sig->param_count];
	} else {
		if (sig->ret->type == MONO_TYPE_VOID)
			entry_func = entry_funcs_static [sig->param_count];
		else
			entry_func = entry_funcs_static_ret [sig->param_count];
	}
	g_assert (entry_func);

	/* Encode unbox in the lower bit of imethod */
	gpointer entry_arg = imethod;
	if (unbox)
		entry_arg = (gpointer)(((gsize)entry_arg) | 1);
	MonoFtnDesc *entry_ftndesc = mini_llvmonly_create_ftndesc (mono_domain_get (), entry_func, entry_arg);

	addr = mini_llvmonly_create_ftndesc (mono_domain_get (), entry_wrapper, entry_ftndesc);

	info = domain_jit_info (domain);
	mono_domain_lock (domain);
	if (!info->interp_method_pointer_hash)
		info->interp_method_pointer_hash = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (info->interp_method_pointer_hash, addr, imethod);
	mono_domain_unlock (domain);

	mono_memory_barrier ();
	if (unbox)
		imethod->llvmonly_unbox_entry = addr;
	else
		imethod->jit_entry = addr;

	return (MonoFtnDesc*)addr;
}

/*
 * interp_create_method_pointer:
 *
 * Return a function pointer which can be used to call METHOD using the
 * interpreter. Return NULL for methods which are not supported.
 */
static gpointer
interp_create_method_pointer (MonoMethod *method, gboolean compile, MonoError *error)
{
	gpointer addr, entry_func, entry_wrapper = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoJitDomainInfo *info;
	InterpMethod *imethod = mono_interp_get_imethod (domain, method, error);

	if (imethod->jit_entry)
		return imethod->jit_entry;

	if (compile && !imethod->transformed) {
		/* Return any errors from method compilation */
		mono_interp_transform_method (imethod, get_context (), error);
		return_val_if_nok (error, NULL);
	}

	MonoMethodSignature *sig = mono_method_signature_internal (method);
	if (method->string_ctor) {
		MonoMethodSignature *newsig = g_alloca (MONO_SIZEOF_METHOD_SIGNATURE + ((sig->param_count + 2) * sizeof (MonoType*)));
		memcpy (newsig, sig, mono_metadata_signature_size (sig));
		newsig->ret = m_class_get_byval_arg (mono_defaults.string_class);
		sig = newsig;
	}

	if (sig->param_count > MAX_INTERP_ENTRY_ARGS) {
		entry_func = (gpointer)interp_entry_general;
	} else if (sig->hasthis) {
		if (sig->ret->type == MONO_TYPE_VOID)
			entry_func = entry_funcs_instance [sig->param_count];
		else
			entry_func = entry_funcs_instance_ret [sig->param_count];
	} else {
		if (sig->ret->type == MONO_TYPE_VOID)
			entry_func = entry_funcs_static [sig->param_count];
		else
			entry_func = entry_funcs_static_ret [sig->param_count];
	}

#ifndef MONO_ARCH_HAVE_INTERP_NATIVE_TO_MANAGED
#ifdef HOST_WASM
	if (method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (method);
		MonoMethod *orig_method = info->d.native_to_managed.method;

		/*
		 * These are called from native code. Ask the host app for a trampoline.
		 */
		MonoFtnDesc *ftndesc = g_new0 (MonoFtnDesc, 1);
		ftndesc->addr = entry_func;
		ftndesc->arg = imethod;

		addr = mono_wasm_get_native_to_interp_trampoline (orig_method, ftndesc);
		if (addr) {
			mono_memory_barrier ();
			imethod->jit_entry = addr;
			return addr;
		}

#ifdef ENABLE_NETCORE
		/*
		 * The runtime expects a function pointer unique to method and
		 * the native caller expects a function pointer with the
		 * right signature, so fail right away.
		 */
		mono_error_set_platform_not_supported (error, "No native to managed transitions on this platform.");
		return NULL;
#endif
	}
#endif
	return (gpointer)interp_no_native_to_managed;
#endif

	if (mono_llvm_only) {
		/* The caller should call interp_create_method_pointer_llvmonly */
		//g_assert_not_reached ();
		return (gpointer)no_llvmonly_interp_method_pointer;
	}

	if (method->wrapper_type && method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE)
		return imethod;

#ifndef MONO_ARCH_HAVE_FTNPTR_ARG_TRAMPOLINE
	/*
	 * Interp in wrappers get the argument in the rgctx register. If
	 * MONO_ARCH_HAVE_FTNPTR_ARG_TRAMPOLINE is defined it means that
	 * on that arch the rgctx register is not scratch, so we use a
	 * separate temp register. We should update the wrappers for this
	 * if we really care about those architectures (arm).
	 */
	MonoMethod *wrapper = mini_get_interp_in_wrapper (sig);

	entry_wrapper = mono_jit_compile_method_jit_only (wrapper, error);
#endif
	if (!entry_wrapper) {
#ifndef MONO_ARCH_HAVE_INTERP_ENTRY_TRAMPOLINE
		g_assertion_message ("couldn't compile wrapper \"%s\" for \"%s\"",
				mono_method_get_name_full (wrapper, TRUE, TRUE, MONO_TYPE_NAME_FORMAT_IL),
				mono_method_get_name_full (method,  TRUE, TRUE, MONO_TYPE_NAME_FORMAT_IL));
#else
		mono_error_cleanup (error);
		error_init_reuse (error);
		if (!mono_native_to_interp_trampoline) {
			if (mono_aot_only) {
				mono_native_to_interp_trampoline = (MonoFuncV)mono_aot_get_trampoline ("native_to_interp_trampoline");
			} else {
				MonoTrampInfo *info;
				mono_native_to_interp_trampoline = (MonoFuncV)mono_arch_get_native_to_interp_trampoline (&info);
				mono_tramp_info_register (info, NULL);
			}
		}
		entry_wrapper = (gpointer)mono_native_to_interp_trampoline;
		/* We need the lmf wrapper only when being called from mixed mode */
		if (sig->pinvoke)
			entry_func = (gpointer)interp_entry_from_trampoline;
		else {
			static gpointer cached_func = NULL;
			if (!cached_func) {
				cached_func = mono_jit_compile_method_jit_only (mini_get_interp_lmf_wrapper ("mono_interp_entry_from_trampoline", (gpointer) mono_interp_entry_from_trampoline), error);
				mono_memory_barrier ();
			}
			entry_func = cached_func;
		}
#endif
	}

	g_assert (entry_func);
	/* This is the argument passed to the interp_in wrapper by the static rgctx trampoline */
	MonoFtnDesc *ftndesc = g_new0 (MonoFtnDesc, 1);
	ftndesc->addr = entry_func;
	ftndesc->arg = imethod;
	mono_error_assert_ok (error);

	/*
	 * The wrapper is called by compiled code, which doesn't pass the extra argument, so we pass it in the
	 * rgctx register using a trampoline.
	 */

	addr = mono_create_ftnptr_arg_trampoline (ftndesc, entry_wrapper);

	info = domain_jit_info (domain);
	mono_domain_lock (domain);
	if (!info->interp_method_pointer_hash)
		info->interp_method_pointer_hash = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (info->interp_method_pointer_hash, addr, imethod);
	mono_domain_unlock (domain);

	mono_memory_barrier ();
	imethod->jit_entry = addr;

	return addr;
}

static void
interp_free_method (MonoDomain *domain, MonoMethod *method)
{
	MonoJitDomainInfo *info = domain_jit_info (domain);

	mono_domain_jit_code_hash_lock (domain);
	/* InterpMethod is allocated in the domain mempool. We might haven't
	 * allocated an InterpMethod for this instance yet */
	mono_internal_hash_table_remove (&info->interp_code_hash, method);
	mono_domain_jit_code_hash_unlock (domain);
}

#if COUNT_OPS
static long opcode_counts[MINT_LASTOP];

#define COUNT_OP(op) opcode_counts[op]++
#else
#define COUNT_OP(op) 
#endif

#if DEBUG_INTERP
#define DUMP_INSTR() \
	if (tracing > 1) { \
		char *ins; \
		if (sp > frame->stack) { \
			ins = dump_stack (frame->stack, sp); \
		} else { \
			ins = g_strdup (""); \
		} \
		sp->data.l = 0; \
		output_indent (); \
		char *mn = mono_method_full_name (frame->imethod->method, FALSE); \
		char *disasm = mono_interp_dis_mintop ((gint32)(ip - frame->imethod->code), TRUE, ip + 1, *ip); \
		g_print ("(%p) %s -> %s\t%d:%s\n", mono_thread_internal_current (), mn, disasm, vt_sp - vtalloc, ins); \
		g_free (mn); \
		g_free (ins); \
		g_free (disasm); \
	}
#else
#define DUMP_INSTR()
#endif

#define INIT_VTABLE(vtable) do { \
		if (G_UNLIKELY (!(vtable)->initialized)) { \
			mono_runtime_class_init_full ((vtable), error); \
			if (!is_ok (error)) \
				THROW_EX (mono_error_convert_to_exception (error), ip); \
		} \
	} while (0);

static MonoObject*
mono_interp_new (MonoDomain* domain, MonoClass* klass)
{
	ERROR_DECL (error);
	MonoObject* const object = mono_object_new_checked (domain, klass, error);
	mono_error_cleanup (error); // FIXME: do not swallow the error
	return object;
}

static void
mono_interp_load_remote_field (
	InterpMethod* imethod,
	MonoObject* o,
	const guint16* ip,
	stackval* sp)
{
	g_assert (o); // Caller checks and throws exception properly.

	void* addr;
	MonoClassField* const field = (MonoClassField*)imethod->data_items[ip [1]];

#ifndef DISABLE_REMOTING
	gpointer tmp;
	if (mono_object_is_transparent_proxy (o)) {
		MonoClass * const klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;
		ERROR_DECL (error);
		addr = mono_load_remote_field_checked (o, klass, field, &tmp, error);
		mono_error_cleanup (error); /* FIXME: don't swallow the error */
	} else
#endif
		addr = (char*)o + field->offset;
	stackval_from_data (field->type, &sp [-1], addr, FALSE);
}

static
guchar* // Return new vt_sp instead of take-address.
mono_interp_load_remote_field_vt (
	InterpMethod* imethod,
	MonoObject* o,
	const guint16* ip,
	stackval* sp,
	guchar* vt_sp)
{
	g_assert (o); // Caller checks and throws exception properly.

	void* addr;
	MonoClassField* const field = (MonoClassField*)imethod->data_items[ip [1]];
	MonoClass* klass = mono_class_from_mono_type_internal (field->type);
	int const i32 = mono_class_value_size (klass, NULL);

#ifndef DISABLE_REMOTING
	gpointer tmp;
	if (mono_object_is_transparent_proxy (o)) {
		klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;
		ERROR_DECL (error);
		addr = mono_load_remote_field_checked (o, klass, field, &tmp, error);
		mono_error_cleanup (error); /* FIXME: don't swallow the error */
	} else
#endif
		addr = (char*)o + field->offset;
	sp [-1].data.p = vt_sp;
	memcpy (vt_sp, addr, i32);
	return vt_sp + ALIGN_TO (i32, MINT_VT_ALIGNMENT);
}

static gboolean
mono_interp_isinst (MonoObject* object, MonoClass* klass)
{
	ERROR_DECL (error);
	gboolean isinst;
	MonoClass *obj_class = mono_object_class (object);
	// mono_class_is_assignable_from_checked can't handle remoting casts
	if (mono_class_is_transparent_proxy (obj_class))
		isinst = mono_object_isinst_checked (object, klass, error) != NULL;
	else
		mono_class_is_assignable_from_checked (klass, obj_class, &isinst, error);
	mono_error_cleanup (error); // FIXME: do not swallow the error
	return isinst;
}

static MONO_NEVER_INLINE InterpMethod*
mono_interp_get_native_func_wrapper (InterpMethod* imethod, MonoMethodSignature* csignature, guchar* code)
{
	ERROR_DECL(error);

	/* Pinvoke call is missing the wrapper. See mono_get_native_calli_wrapper */
	MonoMarshalSpec** mspecs = g_newa0 (MonoMarshalSpec*, csignature->param_count + 1);

	MonoMethodPInvoke iinfo;
	memset (&iinfo, 0, sizeof (iinfo));

	MonoMethod* m = mono_marshal_get_native_func_wrapper (m_class_get_image (imethod->method->klass), csignature, &iinfo, mspecs, code);

	for (int i = csignature->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);

	InterpMethod *cmethod = mono_interp_get_imethod (imethod->domain, m, error);
	mono_error_cleanup (error); /* FIXME: don't swallow the error */

	return cmethod;
}

// Do not inline in case order of frame addresses matters.
static MONO_NEVER_INLINE MonoException*
mono_interp_leave (InterpFrame* parent_frame)
{
	InterpFrame frame = {parent_frame};

	stackval tmp_sp;
	/*
	 * We need for mono_thread_get_undeniable_exception to be able to unwind
	 * to check the abort threshold. For this to work we use frame as a
	 * dummy frame that is stored in the lmf and serves as the transition frame
	 */
	do_icall_wrapper (&frame, NULL, MINT_ICALL_V_P, &tmp_sp, (gpointer)mono_thread_get_undeniable_exception, FALSE);

	return (MonoException*)tmp_sp.data.p;
}

static void
mono_interp_enum_hasflag (stackval* sp, MonoClass* klass)
{
	guint64 a_val = 0, b_val = 0;

	stackval_to_data (m_class_get_byval_arg (klass), --sp, &b_val, FALSE);
	stackval_to_data (m_class_get_byval_arg (klass), --sp, &a_val, FALSE);
	sp->data.i = (a_val & b_val) == b_val;
}

static int
mono_interp_box_nullable (InterpFrame* frame, const guint16* ip, stackval* sp, MonoError* error)
{
	InterpMethod* const imethod = frame->imethod;
	MonoClass* const c = (MonoClass*)imethod->data_items [ip [1]];

	int const size = mono_class_value_size (c, NULL);

	guint16 offset = ip [2];
	guint16 pop_vt_sp = !ip [3];

	sp [-1 - offset].data.o = mono_nullable_box (sp [-1 - offset].data.p, c, error);
	mono_interp_error_cleanup (error); /* FIXME: don't swallow the error */

	return pop_vt_sp ? ALIGN_TO (size, MINT_VT_ALIGNMENT) : 0;
}

static int
mono_interp_box_vt (InterpFrame* frame, const guint16* ip, stackval* sp)
{
	InterpMethod* const imethod = frame->imethod;

	MonoObject* o; // See the comment about GC safety.
	MonoVTable * const vtable = (MonoVTable*)imethod->data_items [ip [1]];
	MonoClass* const c = vtable->klass;

	int const size = mono_class_value_size (c, NULL);

	guint16 offset = ip [2];
	guint16 pop_vt_sp = !ip [3];

	OBJREF (o) = mono_gc_alloc_obj (vtable, m_class_get_instance_size (vtable->klass));
	mono_value_copy_internal (mono_object_get_data (o), sp [-1 - offset].data.p, c);

	sp [-1 - offset].data.p = o;
	return pop_vt_sp ? ALIGN_TO (size, MINT_VT_ALIGNMENT) : 0;
}

static void
mono_interp_box (InterpFrame* frame, const guint16* ip, stackval* sp)
{
	MonoObject *o; // See the comment about GC safety.
	MonoVTable * const vtable = (MonoVTable*)frame->imethod->data_items [ip [1]];

	OBJREF (o) = mono_gc_alloc_obj (vtable, m_class_get_instance_size (vtable->klass));

	guint16 const offset = ip [2];

	stackval_to_data (m_class_get_byval_arg (vtable->klass), &sp [-1 - offset], mono_object_get_data (o), FALSE);

	sp [-1 - offset].data.p = o;
}

static int
mono_interp_store_remote_field_vt (InterpFrame* frame, const guint16* ip, stackval* sp, MonoError* error)
{
	InterpMethod* const imethod = frame->imethod;
	MonoClassField *field;

	MonoObject* const o = sp [-2].data.o;

	field = (MonoClassField*)imethod->data_items[ip [1]];
	MonoClass *klass = mono_class_from_mono_type_internal (field->type);
	int const i32 = mono_class_value_size (klass, NULL);

#ifndef DISABLE_REMOTING
	if (mono_object_is_transparent_proxy (o)) {
		MonoClass *klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;
		mono_store_remote_field_checked (o, klass, field, sp [-1].data.p, error);
		mono_interp_error_cleanup (error); /* FIXME: don't swallow the error */
	} else
#endif
		mono_value_copy_internal ((char *) o + field->offset, sp [-1].data.p, klass);

	return ALIGN_TO (i32, MINT_VT_ALIGNMENT);
}

// varargs in wasm consumes extra linear stack per call-site.
// These g_warning/g_error wrappers fix that. It is not the
// small wasm stack, but conserving it is still desirable.
static void
g_warning_d (const char *format, int d)
{
	g_warning (format, d);
}

#if !USE_COMPUTED_GOTO
static void
interp_error_xsx (const char *format, int x1, const char *s, int x2)
{
	g_error (format, x1, s, x2);
}
#endif

static MONO_ALWAYS_INLINE gboolean
method_entry (ThreadContext *context, InterpFrame *frame,
#if DEBUG_INTERP
	int *out_tracing,
#endif
	MonoException **out_ex)
{
	gboolean slow = FALSE;

#if DEBUG_INTERP
	debug_enter (frame, out_tracing);
#endif
#if PROFILE_INTERP
	frame->imethod->calls++;
#endif

	*out_ex = NULL;
	if (!G_UNLIKELY (frame->imethod->transformed)) {
		slow = TRUE;
		MonoException *ex = do_transform_method (frame, context);
		if (ex) {
			*out_ex = ex;
			/*
			 * Initialize the stack base pointer here, in the uncommon branch, so we don't
			 * need to check for it everytime when exitting a frame.
			 */
			frame->stack = (stackval*)context->stack_pointer;
			return slow;
		}
	}

	return slow;
}

/* Save the state of the interpeter main loop into FRAME */
#define SAVE_INTERP_STATE(frame) do { \
	frame->state.ip = ip;  \
	frame->state.sp = sp; \
	frame->state.vt_sp = vt_sp; \
	frame->state.finally_ips = finally_ips; \
	} while (0)

/* Load and clear state from FRAME */
#define LOAD_INTERP_STATE(frame) do { \
	ip = frame->state.ip; \
	sp = frame->state.sp; \
	vt_sp = frame->state.vt_sp; \
	finally_ips = frame->state.finally_ips; \
	locals = (unsigned char *)frame->stack; \
	frame->state.ip = NULL; \
	} while (0)

/* Initialize interpreter state for executing FRAME */
#define INIT_INTERP_STATE(frame, _clause_args) do {	 \
	ip = _clause_args ? ((FrameClauseArgs *)_clause_args)->start_with_ip : (frame)->imethod->code; \
	locals = (unsigned char *)(frame)->stack; \
	vt_sp = (unsigned char *) locals + (frame)->imethod->total_locals_size; \
	sp = (stackval*)(vt_sp + (frame)->imethod->vt_stack_size); \
	finally_ips = NULL; \
	} while (0)

#if PROFILE_INTERP
static long total_executed_opcodes;
#endif

/*
 * If CLAUSE_ARGS is non-null, start executing from it.
 * The ERROR argument is used to avoid declaring an error object for every interp frame, its not used
 * to return error information.
 * FRAME is only valid until the next call to alloc_frame ().
 */
static MONO_NEVER_INLINE void
interp_exec_method (InterpFrame *frame, ThreadContext *context, FrameClauseArgs *clause_args)
{
	InterpMethod *cmethod;
	MonoException *ex;
	ERROR_DECL(error);

	/* Interpreter main loop state (InterpState) */
	const guint16 *ip = NULL;
	stackval *sp;
	unsigned char *vt_sp;
	unsigned char *locals = NULL;
	GSList *finally_ips = NULL;

#if DEBUG_INTERP
	int tracing = global_tracing;
	unsigned char *vtalloc;
#endif
#if USE_COMPUTED_GOTO
	static void * const in_labels[] = {
#define OPDEF(a,b,c,d,e,f) &&LAB_ ## a,
#include "mintops.def"
	};
#endif

	if (method_entry (context, frame,
#if DEBUG_INTERP
		&tracing,
#endif
		&ex)) {
		if (ex)
			THROW_EX (ex, NULL);
		EXCEPTION_CHECKPOINT;
	}

	if (!clause_args) {
		context->stack_pointer = (guchar*)frame->stack + frame->imethod->alloca_size;
		/* Make sure the stack pointer is bumped before we store any references on the stack */
		mono_compiler_barrier ();
	}

	INIT_INTERP_STATE (frame, clause_args);

#if DEBUG_INTERP
	vtalloc = vt_sp;
#endif

	if (clause_args && clause_args->filter_exception) {
		sp->data.p = clause_args->filter_exception;
		sp++;
	}

#ifdef ENABLE_EXPERIMENT_TIERED
	mini_tiered_inc (frame->imethod->domain, frame->imethod->method, &frame->imethod->tiered_counter, 0);
#endif
	//g_print ("(%p) Call %s\n", mono_thread_internal_current (), mono_method_get_full_name (frame->imethod->method));

#if defined(ENABLE_HYBRID_SUSPEND) || defined(ENABLE_COOP_SUSPEND)
	mono_threads_safepoint ();
#endif
main_loop:
	/*
	 * using while (ip < end) may result in a 15% performance drop, 
	 * but it may be useful for debug
	 */
	while (1) {
#if PROFILE_INTERP
		frame->imethod->opcounts++;
		total_executed_opcodes++;
#endif
		MintOpcode opcode;
#ifdef ENABLE_CHECKED_BUILD
		guchar *vt_start = (guchar*)frame->stack + frame->imethod->total_locals_size;
		guchar *sp_start = vt_start + frame->imethod->vt_stack_size;
		guchar *sp_end = sp_start + frame->imethod->stack_size;
		g_assert (locals == (guchar*)frame->stack);
		g_assert (vt_sp >= vt_start);
		g_assert (vt_sp <= sp_start);
		g_assert ((guchar*)sp >= sp_start);
		g_assert ((guchar*)sp <= sp_end);
#endif
		DUMP_INSTR();
		MINT_IN_SWITCH (*ip) {
		MINT_IN_CASE(MINT_INITLOCALS)
			memset (locals + ip [1], 0, ip [2]);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NOP)
		MINT_IN_CASE(MINT_NIY)
			g_assert_not_reached ();
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BREAK)
			++ip;
			do_debugger_tramp (mini_get_dbg_callbacks ()->user_break, frame);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BREAKPOINT)
			++ip;
			mono_break ();
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDNULL) 
			sp->data.p = NULL;
			++ip;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ARGLIST)
			sp->data.p = vt_sp;
			/*
			 * We know we have been called by an MINT_CALL_VARARG and the amount of vtstack
			 * used by the parameters is at ip [-1] (the last argument to MINT_CALL_VARARG that
			 * is embedded in the instruction stream).
			 */
			*(gpointer*)sp->data.p = frame->parent->state.vt_sp + frame->parent->state.ip [-1];
			vt_sp += ALIGN_TO (sizeof (gpointer), MINT_VT_ALIGNMENT);
			++ip;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_VTRESULT) {
			int ret_size = ip [1];
			unsigned char *ret_vt_sp = vt_sp;
			vt_sp -= READ32(ip + 2);
			if (ret_size > 0) {
				memmove (vt_sp, ret_vt_sp, ret_size);
				sp [-1].data.p = vt_sp;
				vt_sp += ALIGN_TO (ret_size, MINT_VT_ALIGNMENT);
			}
			ip += 4;
			MINT_IN_BREAK;
		}
#define LDC(n) do { sp->data.i = (n); ++ip; ++sp; } while (0)
		MINT_IN_CASE(MINT_LDC_I4_M1)
			LDC(-1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4_0)
			LDC(0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4_1)
			LDC(1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4_2)
			LDC(2);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4_3)
			LDC(3);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4_4)
			LDC(4);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4_5)
			LDC(5);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4_6)
			LDC(6);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4_7)
			LDC(7);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4_8)
			LDC(8);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4_S) 
			sp->data.i = (short)ip [1];
			ip += 2;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4)
			++ip;
			sp->data.i = READ32 (ip);
			ip += 2;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I8)
			++ip;
			sp->data.l = READ64 (ip);
			ip += 4;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I8_S)
			sp->data.l = (short)ip [1];
			ip += 2;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_R4) {
			guint32 val;
			++ip;
			val = READ32(ip);
			sp->data.f_r4 = * (float *)&val;
			ip += 2;
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDC_R8) 
			sp->data.l = READ64 (ip + 1); /* note union usage */
			ip += 5;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DUP) 
			sp [0] = sp[-1];
			++sp;
			++ip; 
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DUP_VT) {
			int const i32 = READ32 (ip + 1);
			sp->data.p = vt_sp;
			memcpy(sp->data.p, sp [-1].data.p, i32);
			vt_sp += ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			++sp;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_POP) {
			sp--;
			ip++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_POP_VT) {
			int i32 = READ32 (ip + 1);
			vt_sp -= ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			sp--;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_POP1) {
			sp [-2] = sp [-1];
			sp--;
			ip++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_JMP) {
			g_assert_checked (sp == (stackval*)(locals + frame->imethod->total_locals_size + frame->imethod->vt_stack_size));
			InterpMethod *new_method = (InterpMethod*)frame->imethod->data_items [ip [1]];

			if (frame->imethod->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_TAIL_CALL)
				MONO_PROFILER_RAISE (method_tail_call, (frame->imethod->method, new_method->method));

			if (!new_method->transformed) {
				error_init_reuse (error);

				mono_interp_transform_method (new_method, context, error);
				MonoException *ex = mono_error_convert_to_exception (error);
				if (ex)
					THROW_EX (ex, ip);
				EXCEPTION_CHECKPOINT;
			}
			/*
			 * It's possible for the caller stack frame to be smaller
			 * than the callee stack frame (at the interp level)
			 */
			context->stack_pointer = (guchar*)frame->stack + new_method->alloca_size;
			frame->imethod = new_method;
			vt_sp = locals + frame->imethod->total_locals_size;
#if DEBUG_INTERP
			vtalloc = vt_sp;
#endif
			sp = (stackval*)(vt_sp + frame->imethod->vt_stack_size);
			ip = frame->imethod->code;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALL_DELEGATE) {
			MonoMethodSignature *csignature = (MonoMethodSignature*)frame->imethod->data_items [ip [1]];
			int param_count = csignature->param_count;
			MonoDelegate *del = (MonoDelegate*) sp [-param_count - 1].data.o;
			gboolean is_multicast = del->method == NULL;
			InterpMethod *del_imethod = (InterpMethod*)del->interp_invoke_impl;

			if (!del_imethod) {
				if (is_multicast) {
					error_init_reuse (error);
					MonoMethod *invoke = mono_get_delegate_invoke_internal (del->object.vtable->klass);
					del_imethod = mono_interp_get_imethod (del->object.vtable->domain, mono_marshal_get_delegate_invoke (invoke, del), error);
					del->interp_invoke_impl = del_imethod;
					mono_error_assert_ok (error);
				} else if (!del->interp_method) {
					// Not created from interpreted code
					error_init_reuse (error);
					g_assert (del->method);
					del_imethod = mono_interp_get_imethod (del->object.vtable->domain, del->method, error);
					del->interp_method = del_imethod;
					del->interp_invoke_impl = del_imethod;
					mono_error_assert_ok (error);
				} else {
					del_imethod = (InterpMethod*)del->interp_method;
					if (del_imethod->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
						error_init_reuse (error);
						del_imethod = mono_interp_get_imethod (frame->imethod->domain, mono_marshal_get_native_wrapper (del_imethod->method, FALSE, FALSE), error);
						mono_error_assert_ok (error);
						del->interp_invoke_impl = del_imethod;
					} else if (del_imethod->method->flags & METHOD_ATTRIBUTE_VIRTUAL && !del->target) {
						// 'this' is passed dynamically, we need to recompute the target method
						// with each call
						del_imethod = get_virtual_method (del_imethod, sp [-param_count].data.o->vtable);
					} else {
						del->interp_invoke_impl = del_imethod;
					}
				}
			}
			cmethod = del_imethod;
			vt_sp -= ip [2];
			sp -= param_count + 1;
			if (!is_multicast) {
				if (cmethod->param_count == param_count + 1) {
					// Target method is static but the delegate has a target object. We handle
					// this separately from the case below, because, for these calls, the instance
					// is allowed to be null.
					sp [0].data.o = del->target;
				} else if (del->target) {
					MonoObject *this_arg = del->target;

					// replace the MonoDelegate* on the stack with 'this' pointer
					if (m_class_is_valuetype (this_arg->vtable->klass)) {
						gpointer unboxed = mono_object_unbox_internal (this_arg);
						sp [0].data.p = unboxed;
					} else {
						sp [0].data.o = this_arg;
					}
				} else {
					// skip the delegate pointer for static calls
					// FIXME we could avoid memmove
					memmove (sp, sp + 1, param_count * sizeof (stackval));
				}
			}
			ip += 3;

			goto call;
		}
		MINT_IN_CASE(MINT_CALLI) {
			MonoMethodSignature *csignature;

			csignature = (MonoMethodSignature*)frame->imethod->data_items [ip [1]];
			--sp;

			cmethod = (InterpMethod*)sp->data.p;
			if (cmethod->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
				cmethod = mono_interp_get_imethod (frame->imethod->domain, mono_marshal_get_native_wrapper (cmethod->method, FALSE, FALSE), error);
				mono_interp_error_cleanup (error); /* FIXME: don't swallow the error */
			}

			/* decrement by the actual number of args */
			sp -= csignature->param_count;
			if (csignature->hasthis)
				--sp;
			vt_sp -= ip [2];

			if (csignature->hasthis) {
				MonoObject *this_arg = (MonoObject*)sp->data.p;

				if (m_class_is_valuetype (this_arg->vtable->klass)) {
					gpointer unboxed = mono_object_unbox_internal (this_arg);
					sp [0].data.p = unboxed;
				}
			}
			ip += 3;

			goto call;
		}
		MINT_IN_CASE(MINT_CALLI_NAT_FAST) {
			gpointer target_ip = sp [-1].data.p;
			MonoMethodSignature *csignature = (MonoMethodSignature*)frame->imethod->data_items [ip [1]];
			int opcode = ip [2];
			gboolean save_last_error = ip [3];

			sp--;
			/* for calls, have ip pointing at the start of next instruction */
			frame->state.ip = ip + 4;

			sp = do_icall_wrapper (frame, csignature, opcode, sp, target_ip, save_last_error);
			EXCEPTION_CHECKPOINT_GC_UNSAFE;
			CHECK_RESUME_STATE (context);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALLI_NAT_DYNAMIC) {
			MonoMethodSignature* csignature;

			csignature = (MonoMethodSignature*)frame->imethod->data_items [ip [1]];

			--sp;
			guchar* code = (guchar*)sp->data.p;

			/* decrement by the actual number of args */
			sp -= csignature->param_count;
			if (csignature->hasthis)
				--sp;
			vt_sp -= ip [2];

			cmethod = mono_interp_get_native_func_wrapper (frame->imethod, csignature, code);

			ip += 3;
			goto call;
		}
		MINT_IN_CASE(MINT_CALLI_NAT) {
			MonoMethodSignature* csignature;
			stackval retval;

			csignature = (MonoMethodSignature*)frame->imethod->data_items [ip [1]];

			--sp;
			guchar* const code = (guchar*)sp->data.p;

			/* decrement by the actual number of args */
			sp -= csignature->param_count;
			if (csignature->hasthis)
				--sp;
			vt_sp -= ip [2];
			/* If this is a vt return, the pinvoke will write the result directly to vt_sp */
			retval.data.p = vt_sp;

			gboolean save_last_error = ip [4];
			gpointer *cache = (gpointer*)&frame->imethod->data_items [ip [5]];
			/* for calls, have ip pointing at the start of next instruction */
			frame->state.ip = ip + 6;
			ves_pinvoke_method (csignature, (MonoFuncV)code, context, frame, &retval, save_last_error, cache, sp);

			CHECK_RESUME_STATE (context);

			if (csignature->ret->type != MONO_TYPE_VOID) {
				*sp = retval;
				vt_sp += ip [3];
				sp++;
			}
			ip += 6;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALLVIRT_FAST) {
			MonoObject *this_arg;
			int slot;

			cmethod = (InterpMethod*)frame->imethod->data_items [ip [1]];
			slot = (gint16)ip [2];

			/* decrement by the actual number of args */
			sp -= cmethod->param_count + cmethod->hasthis;
			vt_sp -= ip [3];
			this_arg = (MonoObject*)sp->data.p;
			ip += 4;

			cmethod = get_virtual_method_fast (cmethod, this_arg->vtable, slot);
			if (m_class_is_valuetype (this_arg->vtable->klass) && m_class_is_valuetype (cmethod->method->klass)) {
				/* unbox */
				gpointer unboxed = mono_object_unbox_internal (this_arg);
				sp [0].data.p = unboxed;
			}

			InterpMethodCodeType code_type = cmethod->code_type;

			g_assert (code_type == IMETHOD_CODE_UNKNOWN ||
			          code_type == IMETHOD_CODE_INTERP ||
			          code_type == IMETHOD_CODE_COMPILED);

			if (G_UNLIKELY (code_type == IMETHOD_CODE_UNKNOWN)) {
				MonoMethodSignature *sig = mono_method_signature_internal (cmethod->method);
				if (mono_interp_jit_call_supported (cmethod->method, sig))
					code_type = IMETHOD_CODE_COMPILED;
				else
					code_type = IMETHOD_CODE_INTERP;
				cmethod->code_type = code_type;
			}

			if (code_type == IMETHOD_CODE_INTERP) {

				goto call;

			} else if (code_type == IMETHOD_CODE_COMPILED) {
				frame->state.ip = ip;
				error_init_reuse (error);
				do_jit_call (sp, vt_sp, frame, cmethod, error);
				if (!is_ok (error)) {
					MonoException *ex = mono_error_convert_to_exception (error);
					THROW_EX (ex, ip);
				}

				CHECK_RESUME_STATE (context);

				if (cmethod->rtype->type != MONO_TYPE_VOID) {
					sp++;
					vt_sp += ((JitCallInfo*)cmethod->jit_call_info)->vt_res_size;
				}
			}

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALL_VARARG) {
			MonoMethodSignature *csig;

			cmethod = (InterpMethod*)frame->imethod->data_items [ip [1]];

			/* The real signature for vararg calls */
			csig = (MonoMethodSignature*) frame->imethod->data_items [ip [2]];

			/* Push all vararg arguments from normal sp to vt_sp together with the signature */
			copy_varargs_vtstack (csig, sp, vt_sp);
			vt_sp -= ip [3];

			/* decrement by the actual number of args */
			// FIXME This seems excessive: frame and csig param_count.
			sp -= cmethod->param_count + cmethod->hasthis + csig->param_count - csig->sentinelpos;

			ip += 4;
			goto call;
		}
		MINT_IN_CASE(MINT_CALLVIRT) {
			// FIXME CALLVIRT opcodes are not used on netcore. We should kill them.
			cmethod = (InterpMethod*)frame->imethod->data_items [ip [1]];

			/* decrement by the actual number of args */
			sp -= ip [2];
			vt_sp -= ip [3];

			MonoObject *this_arg = (MonoObject*)sp->data.p;

			cmethod = get_virtual_method (cmethod, this_arg->vtable);
			if (m_class_is_valuetype (this_arg->vtable->klass) && m_class_is_valuetype (cmethod->method->klass)) {
				/* unbox */
				gpointer unboxed = mono_object_unbox_internal (this_arg);
				sp [0].data.p = unboxed;
			}

#ifdef ENABLE_EXPERIMENT_TIERED
			ip += 5;
#else
			ip += 4;
#endif
			goto call;
		}
		MINT_IN_CASE(MINT_CALL) {
			cmethod = (InterpMethod*)frame->imethod->data_items [ip [1]];

			/* decrement by the actual number of args */
			sp -= ip [2];
			vt_sp -= ip [3];

#ifdef ENABLE_EXPERIMENT_TIERED
			ip += 5;
#else
			ip += 4;
#endif
call:
			/*
			 * Make a non-recursive call by loading the new interpreter state based on child frame,
			 * and going back to the main loop.
			 */
			SAVE_INTERP_STATE (frame);

			// Allocate child frame.
			// FIXME: Add stack overflow checks
			{
				InterpFrame *child_frame = frame->next_free;
				if (!child_frame) {
					child_frame = g_newa0 (InterpFrame, 1);
					// Not free currently, but will be when allocation attempted.
					frame->next_free = child_frame;
				}
				reinit_frame (child_frame, frame, cmethod, sp);
				frame = child_frame;
			}
			if (method_entry (context, frame,
#if DEBUG_INTERP
				&tracing,
#endif
				&ex)) {
				if (ex)
					THROW_EX (ex, NULL);
				EXCEPTION_CHECKPOINT;
			}

			context->stack_pointer = (guchar*)sp + cmethod->alloca_size;
			/* Make sure the stack pointer is bumped before we store any references on the stack */
			mono_compiler_barrier ();

			INIT_INTERP_STATE (frame, NULL);

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_JIT_CALL) {
			InterpMethod *rmethod = (InterpMethod*)frame->imethod->data_items [ip [1]];
			error_init_reuse (error);
			sp -= rmethod->param_count + rmethod->hasthis;
			vt_sp -= ip [2];
			/* for calls, have ip pointing at the start of next instruction */
			frame->state.ip = ip + 3;
			do_jit_call (sp, vt_sp, frame, rmethod, error);
			if (!is_ok (error)) {
				MonoException *ex = mono_error_convert_to_exception (error);
				THROW_EX (ex, ip);
			}

			CHECK_RESUME_STATE (context);

			if (rmethod->rtype->type != MONO_TYPE_VOID) {
				sp++;
				vt_sp += ((JitCallInfo*)rmethod->jit_call_info)->vt_res_size;
			}
			ip += 3;

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_JIT_CALL2) {
#ifdef ENABLE_EXPERIMENT_TIERED
			InterpMethod *rmethod = (InterpMethod *) READ64 (ip + 1);

			error_init_reuse (error);

			sp -= rmethod->param_count + rmethod->hasthis;
			frame->state.ip = ip + 5;
			do_jit_call (sp, vt_sp, frame, rmethod, error);
			if (!is_ok (error)) {
				MonoException *ex = mono_error_convert_to_exception (error);
				THROW_EX (ex, ip);
			}

			CHECK_RESUME_STATE (context);

			if (rmethod->rtype->type != MONO_TYPE_VOID)
				sp++;
			ip += 5;
#else
			g_error ("MINT_JIT_ICALL2 shouldn't be used");
#endif
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALLRUN) {
#ifndef ENABLE_NETCORE
			MonoMethod *target_method = (MonoMethod*) frame->imethod->data_items [ip [1]];
			MonoMethodSignature *sig = (MonoMethodSignature*) frame->imethod->data_items [ip [2]];

			sp->data.p = vt_sp;
			stackval *retval = sp;

			sp -= sig->param_count;
			if (sig->hasthis)
				sp--;

			MonoException *ex = ves_imethod (frame, target_method, sig, sp, retval);
			if (ex)
				THROW_EX (ex, ip);

			if (sig->ret->type != MONO_TYPE_VOID) {
				*sp = *retval;
				sp++;
			}
			ip += 3;
#else
			g_assert_not_reached ();
#endif
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_RET)
			--sp;
			if (frame->parent) {
				frame->parent->state.sp [0] = *sp;
				frame->parent->state.sp++;
			} else {
				// FIXME This can only happen in a few wrappers. Add separate opcode for it
				*frame->retval = *sp;
			}
			g_assert_checked (sp == (stackval*)(locals + frame->imethod->total_locals_size + frame->imethod->vt_stack_size));
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_VOID)
			g_assert_checked (sp == (stackval*)(locals + frame->imethod->total_locals_size + frame->imethod->vt_stack_size));
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_VT) {
			int const i32 = READ32 (ip + 1);
			--sp;
			if (frame->parent) {
				gpointer dest_vt = frame->parent->state.vt_sp;
				// Push the valuetype in the parent frame. parent->state.sp [0] can be inside
				// vt to be returned, so we need to copy it before updating sp [0].
				memcpy (dest_vt, sp->data.p, i32);
				frame->parent->state.sp [0].data.p = dest_vt;
				frame->parent->state.sp++;
				frame->parent->state.vt_sp += ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			} else {
				gpointer dest_vt = frame->retval->data.p;
				memcpy (dest_vt, sp->data.p, i32);
			}
			g_assert_checked (sp == (stackval*)(locals + frame->imethod->total_locals_size + frame->imethod->vt_stack_size));
			goto exit_frame;
		}
		MINT_IN_CASE(MINT_RET_LOCALLOC)
			--sp;
			if (frame->parent) {
				frame->parent->state.sp [0] = *sp;
				frame->parent->state.sp++;
			} else {
				// FIXME This can only happen in a few wrappers. Add separate opcode for it
				*frame->retval = *sp;
			}
			frame_data_allocator_pop (&context->data_stack, frame);
			g_assert_checked (sp == (stackval*)(locals + frame->imethod->total_locals_size + frame->imethod->vt_stack_size));
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_VOID_LOCALLOC)
			frame_data_allocator_pop (&context->data_stack, frame);
			g_assert_checked (sp == (stackval*)(locals + frame->imethod->total_locals_size + frame->imethod->vt_stack_size));
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_VT_LOCALLOC) {
			int const i32 = READ32 (ip + 1);
			--sp;
			if (frame->parent) {
				gpointer dest_vt = frame->parent->state.vt_sp;
				/* Push the valuetype in the parent frame */
				memcpy (dest_vt, sp->data.p, i32);
				frame->parent->state.sp [0].data.p = dest_vt;
				frame->parent->state.sp++;
				frame->parent->state.vt_sp += ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			} else {
				memcpy (frame->retval->data.p, sp->data.p, i32);
			}
			frame_data_allocator_pop (&context->data_stack, frame);
			g_assert_checked (sp == (stackval*)(locals + frame->imethod->total_locals_size + frame->imethod->vt_stack_size));
			goto exit_frame;
		}

#ifdef ENABLE_EXPERIMENT_TIERED
#define BACK_BRANCH_PROFILE(offset) do { \
		if (offset < 0) \
			mini_tiered_inc (frame->imethod->domain, frame->imethod->method, &frame->imethod->tiered_counter, 0); \
	} while (0);
#else
#define BACK_BRANCH_PROFILE(offset)
#endif

		MINT_IN_CASE(MINT_BR_S) {
			short br_offset = (short) *(ip + 1);
			BACK_BRANCH_PROFILE (br_offset);
			ip += br_offset;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BR) {
			gint32 br_offset = (gint32) READ32(ip + 1);
			BACK_BRANCH_PROFILE (br_offset);
			ip += br_offset;
			MINT_IN_BREAK;
		}

#define ZEROP_S(datamem, op) \
	--sp; \
	if (sp->data.datamem op 0) { \
		gint16 br_offset = (gint16) ip [1]; \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 2;

#define ZEROP(datamem, op) \
	--sp; \
	if (sp->data.datamem op 0) { \
		gint32 br_offset = (gint32)READ32(ip + 1); \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 3;

		MINT_IN_CASE(MINT_BRFALSE_I4_S)
			ZEROP_S(i, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_I8_S)
			ZEROP_S(l, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_R4_S)
			ZEROP_S(f_r4, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_R8_S)
			ZEROP_S(f, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_I4)
			ZEROP(i, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_I8)
			ZEROP(l, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_R4)
			ZEROP_S(f_r4, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_R8)
			ZEROP_S(f, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_I4_S)
			ZEROP_S(i, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_I8_S)
			ZEROP_S(l, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_R4_S)
			ZEROP_S(f_r4, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_R8_S)
			ZEROP_S(f, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_I4)
			ZEROP(i, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_I8)
			ZEROP(l, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_R4)
			ZEROP(f_r4, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_R8)
			ZEROP(f, !=);
			MINT_IN_BREAK;
#define CONDBR_S(cond) \
	sp -= 2; \
	if (cond) { \
		gint16 br_offset = (gint16) ip [1]; \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 2;
#define BRELOP_S(datamem, op) \
	CONDBR_S(sp[0].data.datamem op sp[1].data.datamem)

#define CONDBR(cond) \
	sp -= 2; \
	if (cond) { \
		gint32 br_offset = (gint32) READ32 (ip + 1); \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 3;

#define BRELOP(datamem, op) \
	CONDBR(sp[0].data.datamem op sp[1].data.datamem)

		MINT_IN_CASE(MINT_BEQ_I4_S)
			BRELOP_S(i, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_I8_S)
			BRELOP_S(l, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_R4_S)
			CONDBR_S(!isunordered (sp [0].data.f_r4, sp [1].data.f_r4) && sp[0].data.f_r4 == sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_R8_S)
			CONDBR_S(!mono_isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f == sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_I4)
			BRELOP(i, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_I8)
			BRELOP(l, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_R4)
			CONDBR(!isunordered (sp [0].data.f_r4, sp [1].data.f_r4) && sp[0].data.f_r4 == sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_R8)
			CONDBR(!mono_isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f == sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I4_S)
			BRELOP_S(i, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I8_S)
			BRELOP_S(l, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_R4_S)
			CONDBR_S(!isunordered (sp [0].data.f_r4, sp [1].data.f_r4) && sp[0].data.f_r4 >= sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_R8_S)
			CONDBR_S(!mono_isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f >= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I4)
			BRELOP(i, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I8)
			BRELOP(l, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_R4)
			CONDBR(!isunordered (sp [0].data.f_r4, sp [1].data.f_r4) && sp[0].data.f_r4 >= sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_R8)
			CONDBR(!mono_isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f >= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I4_S)
			BRELOP_S(i, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I8_S)
			BRELOP_S(l, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_R4_S)
			CONDBR_S(!isunordered (sp [0].data.f_r4, sp [1].data.f_r4) && sp[0].data.f_r4 > sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_R8_S)
			CONDBR_S(!mono_isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f > sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I4)
			BRELOP(i, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I8)
			BRELOP(l, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_R4)
			CONDBR(!isunordered (sp [0].data.f_r4, sp [1].data.f_r4) && sp[0].data.f_r4 > sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_R8)
			CONDBR(!mono_isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f > sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I4_S)
			BRELOP_S(i, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I8_S)
			BRELOP_S(l, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_R4_S)
			CONDBR_S(!isunordered (sp [0].data.f_r4, sp [1].data.f_r4) && sp[0].data.f_r4 < sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_R8_S)
			CONDBR_S(!mono_isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f < sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I4)
			BRELOP(i, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I8)
			BRELOP(l, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_R4)
			CONDBR(!isunordered (sp [0].data.f_r4, sp [1].data.f_r4) && sp[0].data.f_r4 < sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_R8)
			CONDBR(!mono_isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f < sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I4_S)
			BRELOP_S(i, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I8_S)
			BRELOP_S(l, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_R4_S)
			CONDBR_S(!isunordered (sp [0].data.f_r4, sp [1].data.f_r4) && sp[0].data.f_r4 <= sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_R8_S)
			CONDBR_S(!mono_isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f <= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I4)
			BRELOP(i, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I8)
			BRELOP(l, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_R4)
			CONDBR(!isunordered (sp [0].data.f_r4, sp [1].data.f_r4) && sp[0].data.f_r4 <= sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_R8)
			CONDBR(!mono_isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f <= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I4_S)
			BRELOP_S(i, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I8_S)
			BRELOP_S(l, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_R4_S)
			CONDBR_S(isunordered (sp [0].data.f_r4, sp [1].data.f_r4) || sp[0].data.f_r4 != sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_R8_S)
			CONDBR_S(mono_isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f != sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I4)
			BRELOP(i, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I8)
			BRELOP(l, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_R4)
			CONDBR(isunordered (sp [0].data.f_r4, sp [1].data.f_r4) || sp[0].data.f_r4 != sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_R8)
			CONDBR(mono_isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f != sp[1].data.f)
			MINT_IN_BREAK;

#define BRELOP_S_CAST(datamem, op, type) \
	sp -= 2; \
	if ((type) sp[0].data.datamem op (type) sp[1].data.datamem) { \
		gint16 br_offset = (gint16) ip [1]; \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 2;

#define BRELOP_CAST(datamem, op, type) \
	sp -= 2; \
	if ((type) sp[0].data.datamem op (type) sp[1].data.datamem) { \
		gint32 br_offset = (gint32) ip [1]; \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 3;

		MINT_IN_CASE(MINT_BGE_UN_I4_S)
			BRELOP_S_CAST(i, >=, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I8_S)
			BRELOP_S_CAST(l, >=, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_R4_S)
			CONDBR_S(isunordered (sp [0].data.f_r4, sp [1].data.f_r4) || sp[0].data.f_r4 >= sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_R8_S)
			CONDBR_S(mono_isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f >= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I4)
			BRELOP_CAST(i, >=, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I8)
			BRELOP_CAST(l, >=, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_R4)
			CONDBR(isunordered (sp [0].data.f_r4, sp [1].data.f_r4) || sp[0].data.f_r4 >= sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_R8)
			CONDBR(mono_isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f >= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I4_S)
			BRELOP_S_CAST(i, >, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I8_S)
			BRELOP_S_CAST(l, >, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_R4_S)
			CONDBR_S(isunordered (sp [0].data.f_r4, sp [1].data.f_r4) || sp[0].data.f_r4 > sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_R8_S)
			CONDBR_S(mono_isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f > sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I4)
			BRELOP_CAST(i, >, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I8)
			BRELOP_CAST(l, >, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_R4)
			CONDBR(isunordered (sp [0].data.f_r4, sp [1].data.f_r4) || sp[0].data.f_r4 > sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_R8)
			CONDBR(mono_isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f > sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I4_S)
			BRELOP_S_CAST(i, <=, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I8_S)
			BRELOP_S_CAST(l, <=, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_R4_S)
			CONDBR_S(isunordered (sp [0].data.f_r4, sp [1].data.f_r4) || sp[0].data.f_r4 <= sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_R8_S)
			CONDBR_S(mono_isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f <= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I4)
			BRELOP_CAST(i, <=, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I8)
			BRELOP_CAST(l, <=, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_R4)
			CONDBR(isunordered (sp [0].data.f_r4, sp [1].data.f_r4) || sp[0].data.f_r4 <= sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_R8)
			CONDBR(mono_isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f <= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I4_S)
			BRELOP_S_CAST(i, <, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I8_S)
			BRELOP_S_CAST(l, <, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_R4_S)
			CONDBR_S(isunordered (sp [0].data.f_r4, sp [1].data.f_r4) || sp[0].data.f_r4 < sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_R8_S)
			CONDBR_S(mono_isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f < sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I4)
			BRELOP_CAST(i, <, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I8)
			BRELOP_CAST(l, <, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_R4)
			CONDBR(isunordered (sp [0].data.f_r4, sp [1].data.f_r4) || sp[0].data.f_r4 < sp[1].data.f_r4)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_R8)
			CONDBR(mono_isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f < sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SWITCH) {
			guint32 n;
			const unsigned short *st;
			++ip;
			n = READ32 (ip);
			ip += 2;
			st = ip + 2 * n;
			--sp;
			if ((guint32)sp->data.i < n) {
				gint offset;
				ip += 2 * (guint32)sp->data.i;
				offset = READ32 (ip);
				ip = ip + offset;
			} else {
				ip = st;
			}
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDIND_I1_CHECK)
			NULL_CHECK (sp [-1].data.p);
			++ip;
			sp[-1].data.i = *(gint8*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_U1_CHECK)
			NULL_CHECK (sp [-1].data.p);
			++ip;
			sp[-1].data.i = *(guint8*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_I2_CHECK)
			NULL_CHECK (sp [-1].data.p);
			++ip;
			sp[-1].data.i = *(gint16*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_U2_CHECK)
			NULL_CHECK (sp [-1].data.p);
			++ip;
			sp[-1].data.i = *(guint16*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_I4_CHECK) /* Fall through */
		MINT_IN_CASE(MINT_LDIND_U4_CHECK)
			NULL_CHECK (sp [-1].data.p);
			++ip;
			sp[-1].data.i = *(gint32*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_I8_CHECK)
			NULL_CHECK (sp [-1].data.p);
			++ip;
#ifdef NO_UNALIGNED_ACCESS
			if ((gsize)sp [-1].data.p % SIZEOF_VOID_P)
				memcpy (&sp [-1].data.l, sp [-1].data.p, sizeof (gint64));
			else
#endif
			sp[-1].data.l = *(gint64*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_I) {
			guint16 offset = ip [1];
			sp[-1 - offset].data.p = *(gpointer*)sp[-1 - offset].data.p;
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDIND_I8) {
			guint16 offset = ip [1];
#ifdef NO_UNALIGNED_ACCESS
			if ((gsize)sp [-1 - offset].data.p % SIZEOF_VOID_P)
				memcpy (&sp [-1 - offset].data.l, sp [-1 - offset].data.p, sizeof (gint64));
			else
#endif
			sp[-1 - offset].data.l = *(gint64*)sp[-1 - offset].data.p;
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDIND_R4_CHECK)
			NULL_CHECK (sp [-1].data.p);
			++ip;
			sp[-1].data.f_r4 = *(gfloat*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_R8_CHECK)
			NULL_CHECK (sp [-1].data.p);
			++ip;
#ifdef NO_UNALIGNED_ACCESS
			if ((gsize)sp [-1].data.p % SIZEOF_VOID_P)
				memcpy (&sp [-1].data.f, sp [-1].data.p, sizeof (gdouble));
			else
#endif
			sp[-1].data.f = *(gdouble*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_REF)
			++ip;
			sp[-1].data.p = *(gpointer*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_REF_CHECK) {
			NULL_CHECK (sp [-1].data.p);
			++ip;
			sp [-1].data.p = *(gpointer*)sp [-1].data.p;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STIND_REF) 
			NULL_CHECK (sp [-2].data.p);
			++ip;
			sp -= 2;
			mono_gc_wbarrier_generic_store_internal (sp->data.p, sp [1].data.o);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I1)
			NULL_CHECK (sp [-2].data.p);
			++ip;
			sp -= 2;
			* (gint8 *) sp->data.p = (gint8)sp[1].data.i;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I2)
			NULL_CHECK (sp [-2].data.p);
			++ip;
			sp -= 2;
			* (gint16 *) sp->data.p = (gint16)sp[1].data.i;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I4)
			NULL_CHECK (sp [-2].data.p);
			++ip;
			sp -= 2;
			* (gint32 *) sp->data.p = sp[1].data.i;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I)
			NULL_CHECK (sp [-2].data.p);
			++ip;
			sp -= 2;
			* (mono_i *) sp->data.p = (mono_i)sp[1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I8)
			NULL_CHECK (sp [-2].data.p);
			++ip;
			sp -= 2;
#ifdef NO_UNALIGNED_ACCESS
			if ((gsize)sp->data.p % SIZEOF_VOID_P)
				memcpy (sp->data.p, &sp [1].data.l, sizeof (gint64));
			else
#endif
			* (gint64 *) sp->data.p = sp[1].data.l;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_R4)
			NULL_CHECK (sp [-2].data.p);
			++ip;
			sp -= 2;
			* (float *) sp->data.p = sp[1].data.f_r4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_R8)
			NULL_CHECK (sp [-2].data.p);
			++ip;
			sp -= 2;
#ifdef NO_UNALIGNED_ACCESS
			if ((gsize)sp->data.p % SIZEOF_VOID_P)
				memcpy (sp->data.p, &sp [1].data.f, sizeof (double));
			else
#endif
			* (double *) sp->data.p = sp[1].data.f;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_ATOMIC_STORE_I4)
			++ip;
			sp -= 2;
			mono_atomic_store_i32 ((gint32 *) sp->data.p, sp [1].data.i);
			MINT_IN_BREAK;
#define BINOP(datamem, op) \
	--sp; \
	sp [-1].data.datamem op ## = sp [0].data.datamem; \
	++ip;
		MINT_IN_CASE(MINT_ADD_I4)
			BINOP(i, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_I8)
			BINOP(l, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_R4)
			BINOP(f_r4, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_R8)
			BINOP(f, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD1_I4)
			++sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD1_I8)
			++sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LOCADD1_I4)
			*(gint32*)(locals + ip [1]) += 1;
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LOCADD1_I8)
			*(gint64*)(locals + ip [1]) += 1;
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_I4)
			BINOP(i, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_I8)
			BINOP(l, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_R4)
			BINOP(f_r4, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_R8)
			BINOP(f, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB1_I4)
			--sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB1_I8)
			--sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LOCSUB1_I4)
			*(gint32*)(locals + ip [1]) -= 1;
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LOCSUB1_I8)
			*(gint64*)(locals + ip [1]) -= 1;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_I4)
			BINOP(i, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_I8)
			BINOP(l, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_R4)
			BINOP(f_r4, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_R8)
			BINOP(f, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DIV_I4) {
			gint32 l1 = sp [-1].data.i;
			gint32 l2 = sp [-2].data.i;
			if (l1 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (l1 == (-1) && l2 == G_MININT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(i, /);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_DIV_I8) {
			gint64 l1 = sp [-1].data.l;
			gint64 l2 = sp [-2].data.l;
			if (l1 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (l1 == (-1) && l2 == G_MININT64)
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(l, /);
			MINT_IN_BREAK;
			}
		MINT_IN_CASE(MINT_DIV_R4)
			BINOP(f_r4, /);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DIV_R8)
			BINOP(f, /);
			MINT_IN_BREAK;

#define BINOP_CAST(datamem, op, type) \
	--sp; \
	sp [-1].data.datamem = (type)sp [-1].data.datamem op (type)sp [0].data.datamem; \
	++ip;
		MINT_IN_CASE(MINT_DIV_UN_I4)
			if (sp [-1].data.i == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			BINOP_CAST(i, /, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DIV_UN_I8)
			if (sp [-1].data.l == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			BINOP_CAST(l, /, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_REM_I4) {
			int i1 = sp [-1].data.i;
			int i2 = sp [-2].data.i;
			if (i1 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (i1 == (-1) && i2 == G_MININT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(i, %);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REM_I8) {
			gint64 l1 = sp [-1].data.l;
			gint64 l2 = sp [-2].data.l;
			if (l1 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (l1 == (-1) && l2 == G_MININT64)
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(l, %);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REM_R4)
			/* FIXME: what do we actually do here? */
			--sp;
			sp [-1].data.f_r4 = fmodf (sp [-1].data.f_r4, sp [0].data.f_r4);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_REM_R8)
			/* FIXME: what do we actually do here? */
			--sp;
			sp [-1].data.f = fmod (sp [-1].data.f, sp [0].data.f);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_REM_UN_I4)
			if (sp [-1].data.i == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			BINOP_CAST(i, %, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_REM_UN_I8)
			if (sp [-1].data.l == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			BINOP_CAST(l, %, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_AND_I4)
			BINOP(i, &);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_AND_I8)
			BINOP(l, &);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_OR_I4)
			BINOP(i, |);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_OR_I8)
			BINOP(l, |);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_XOR_I4)
			BINOP(i, ^);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_XOR_I8)
			BINOP(l, ^);
			MINT_IN_BREAK;

#define SHIFTOP(datamem, op) \
	--sp; \
	sp [-1].data.datamem op ## = sp [0].data.i; \
	++ip;

		MINT_IN_CASE(MINT_SHL_I4)
			SHIFTOP(i, <<);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHL_I8)
			SHIFTOP(l, <<);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_I4)
			SHIFTOP(i, >>);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_I8)
			SHIFTOP(l, >>);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_UN_I4)
			--sp;
			sp [-1].data.i = (guint32)sp [-1].data.i >> sp [0].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_UN_I8)
			--sp;
			sp [-1].data.l = (guint64)sp [-1].data.l >> sp [0].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NEG_I4)
			sp [-1].data.i = - sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NEG_I8)
			sp [-1].data.l = - sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NEG_R4)
			sp [-1].data.f_r4 = - sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NEG_R8)
			sp [-1].data.f = - sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NOT_I4)
			sp [-1].data.i = ~ sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NOT_I8)
			sp [-1].data.l = ~ sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I1_I4)
			sp [-1].data.i = (gint8)sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I1_I8)
			sp [-1].data.i = (gint8)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I1_R4)
			sp [-1].data.i = (gint8) (gint32) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I1_R8)
			/* without gint32 cast, C compiler is allowed to use undefined
			 * behaviour if data.f is bigger than >255. See conv.fpint section
			 * in C standard:
			 * > The conversion truncates; that is, the fractional  part
			 * > is discarded.  The behavior is undefined if the truncated
			 * > value cannot be represented in the destination type.
			 * */
			sp [-1].data.i = (gint8) (gint32) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U1_I4)
			sp [-1].data.i = (guint8)sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U1_I8)
			sp [-1].data.i = (guint8)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U1_R4)
			sp [-1].data.i = (guint8) (guint32) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U1_R8)
			sp [-1].data.i = (guint8) (guint32) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I2_I4)
			sp [-1].data.i = (gint16)sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I2_I8)
			sp [-1].data.i = (gint16)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I2_R4)
			sp [-1].data.i = (gint16) (gint32) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I2_R8)
			sp [-1].data.i = (gint16) (gint32) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U2_I4)
			sp [-1].data.i = (guint16)sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U2_I8)
			sp [-1].data.i = (guint16)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U2_R4)
			sp [-1].data.i = (guint16) (guint32) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U2_R8)
			sp [-1].data.i = (guint16) (guint32) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I4_R4)
			sp [-1].data.i = (gint32) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I4_R8)
			sp [-1].data.i = (gint32)sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U4_I8)
		MINT_IN_CASE(MINT_CONV_I4_I8)
			sp [-1].data.i = (gint32)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I4_I8_SP)
			sp [-2].data.i = (gint32)sp [-2].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U4_R4)
#ifdef MONO_ARCH_EMULATE_FCONV_TO_U4
			sp [-1].data.i = mono_rconv_u4 (sp [-1].data.f_r4);
#else
			sp [-1].data.i = (guint32) sp [-1].data.f_r4;
#endif
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U4_R8)
#ifdef MONO_ARCH_EMULATE_FCONV_TO_U4
			sp [-1].data.i = mono_fconv_u4_2 (sp [-1].data.f);
#else
			sp [-1].data.i = (guint32) sp [-1].data.f;
#endif
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I8_I4)
			sp [-1].data.l = sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I8_I4_SP)
			sp [-2].data.l = sp [-2].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I8_U4)
			sp [-1].data.l = (guint32)sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I8_R4)
			sp [-1].data.l = (gint64) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I8_R8)
			sp [-1].data.l = (gint64)sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R4_I4)
			sp [-1].data.f_r4 = (float)sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R4_I8)
			sp [-1].data.f_r4 = (float)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R4_R8)
			sp [-1].data.f_r4 = (float)sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R8_I4)
			sp [-1].data.f = (double)sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R8_I8)
			sp [-1].data.f = (double)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R8_R4)
			sp [-1].data.f = (double) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R8_R4_SP)
			sp [-2].data.f = (double) sp [-2].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U8_R4)
#ifdef MONO_ARCH_EMULATE_FCONV_TO_U8
			sp [-1].data.l = mono_rconv_u8 (sp [-1].data.f_r4);
#else
			sp [-1].data.l = (guint64) sp [-1].data.f_r4;
#endif
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U8_R8)
#ifdef MONO_ARCH_EMULATE_FCONV_TO_U8
			sp [-1].data.l = mono_fconv_u8_2 (sp [-1].data.f);
#else
			sp [-1].data.l = (guint64)sp [-1].data.f;
#endif
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CPOBJ) {
			MonoClass* const c = (MonoClass*)frame->imethod->data_items[ip [1]];
			g_assert (m_class_is_valuetype (c));
			/* if this assertion fails, we need to add a write barrier */
			g_assert (!MONO_TYPE_IS_REFERENCE (m_class_get_byval_arg (c)));
			stackval_from_data (m_class_get_byval_arg (c), (stackval*)sp [-2].data.p, sp [-1].data.p, FALSE);
			ip += 2;
			sp -= 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CPOBJ_VT) {
			MonoClass* const c = (MonoClass*)frame->imethod->data_items[ip [1]];
			mono_value_copy_internal (sp [-2].data.vt, sp [-1].data.vt, c);
			ip += 2;
			sp -= 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDOBJ_VT) {
			int size = READ32(ip + 1);
			ip += 3;
			memcpy (vt_sp, sp [-1].data.p, size);
			sp [-1].data.p = vt_sp;
			vt_sp += ALIGN_TO (size, MINT_VT_ALIGNMENT);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSTR)
			sp->data.p = frame->imethod->data_items [ip [1]];
			++sp;
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSTR_TOKEN) {
			MonoString *s = NULL;
			guint32 strtoken = (guint32)(gsize)frame->imethod->data_items [ip [1]];

			MonoMethod *method = frame->imethod->method;
			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD) {
				s = (MonoString*)mono_method_get_wrapper_data (method, strtoken);
			} else if (method->wrapper_type != MONO_WRAPPER_NONE) {
				s = mono_string_new_wrapper_internal ((const char*)mono_method_get_wrapper_data (method, strtoken));
			} else {
				g_assert_not_reached ();
			}
			sp->data.p = s;
			++sp;
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_NEWOBJ_ARRAY) {
			MonoClass *newobj_class;
			guint32 token = ip [1];
			guint16 param_count = ip [2];

			newobj_class = (MonoClass*) frame->imethod->data_items [token];

			sp -= param_count;
			sp->data.o = ves_array_create (frame->imethod->domain, newobj_class, param_count, sp, error);
			if (!is_ok (error))
				THROW_EX (mono_error_convert_to_exception (error), ip);

			++sp;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_NEWOBJ_STRING) {
			cmethod = (InterpMethod*)frame->imethod->data_items [ip [1]];

			const int param_count = ip [2];
			if (param_count) {
				sp -= param_count;
				memmove (sp + 1, sp, param_count * sizeof (stackval));
			}
			// `this` is implicit null. The created string will be returned
			// by the call, even though the call has void return (?!).
			sp->data.p = NULL;
			ip += 3;
			goto call;
		}
		MINT_IN_CASE(MINT_NEWOBJ_FAST) {
			MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [3]];
			INIT_VTABLE (vtable);
			MonoObject *o; // See the comment about GC safety.
			guint16 param_count;
			guint16 imethod_index = ip [1];

			const gboolean is_inlined = imethod_index == INLINED_METHOD_FLAG;

			param_count = ip [2];

			// Make room for two copies of o -- this parameter and return value.
			if (param_count || !is_inlined) {
				sp -= param_count;
				memmove (sp + 2, sp, param_count * sizeof (stackval));
			}

			OBJREF (o) = mono_gc_alloc_obj (vtable, m_class_get_instance_size (vtable->klass));
			if (G_UNLIKELY (!o)) {
				mono_error_set_out_of_memory (error, "Could not allocate %i bytes", m_class_get_instance_size (vtable->klass));
				THROW_EX (mono_error_convert_to_exception (error), ip);
			}

			// Store o next to and before the parameters on the stack so GC will see it,
			// and where it is needed when the call returns.
			sp [0].data.o = o;
			sp [1].data.o = o;
			ip += 4;
			if (is_inlined) {
				sp += param_count + 2;
			} else {
				cmethod = (InterpMethod*)frame->imethod->data_items [imethod_index];
				goto call_newobj;
			}

			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_NEWOBJ_VT_FAST)
		MINT_IN_CASE(MINT_NEWOBJ_VTST_FAST) {
			guint16 imethod_index = ip [1];
			gboolean is_inlined = imethod_index == INLINED_METHOD_FLAG;

			guint16 const param_count = ip [2];

			// Make room for extra parameter and result.
			if (param_count) {
				sp -= param_count;
				memmove (sp + 2, sp, param_count * sizeof (stackval));
			}

			gboolean const vtst = *ip == MINT_NEWOBJ_VTST_FAST;
			if (vtst) {
				memset (vt_sp, 0, ip [3]);
				ip += 4;
				// Put extra parameter and result on stack, before other parameters,
				// and point stack to extra parameter, after result.
				// This pattern occurs for newobj_vt_fast and newobj_fast.
				sp [1].data.p = vt_sp;
				sp [0].data.p = vt_sp;
			} else {
				ip += 3;
				// Like newobj_fast, add valuetype_this parameter
				// and result and point stack to this after result.
				memset (sp, 0, sizeof (*sp));
				sp [1].data.p = &sp [0].data; // valuetype_this == result
			}

			if (is_inlined) {
				if (vtst)
					vt_sp += ALIGN_TO (ip [-1], MINT_VT_ALIGNMENT);
				sp += param_count + 2;
				MINT_IN_BREAK;
			}
			cmethod = (InterpMethod*)frame->imethod->data_items [imethod_index];
			}
			// call_newobj captures the pattern where the return value is placed
			// on the stack before the call, instead of the call forming it.
call_newobj:
			++sp; // Point sp at added extra param, after return value.
			goto call;

		MINT_IN_CASE(MINT_NEWOBJ) {
			guint32 const token = ip [1];

			cmethod = (InterpMethod*)frame->imethod->data_items [token];

			MonoMethodSignature* const csig = mono_method_signature_internal (cmethod->method);

			g_assert (csig->hasthis);

			// Make room for first parameter and return value.
			const int param_count = csig->param_count;
			if (param_count) {
				sp -= param_count;
				memmove (sp + 2, sp, param_count * sizeof (stackval));
			}

			MonoClass * const newobj_class = cmethod->method->klass;

			/*if (profiling_classes) {
				guint count = GPOINTER_TO_UINT (g_hash_table_lookup (profiling_classes, newobj_class));
				count++;
				g_hash_table_insert (profiling_classes, newobj_class, GUINT_TO_POINTER (count));
			}*/

			/*
			 * First arg is the object.
			 * a constructor returns void, but we need to return the object we created
			 */

			g_assert (!m_class_is_valuetype (newobj_class));

			MonoDomain* const domain = frame->imethod->domain;
			MonoVTable *vtable = mono_class_vtable_checked (domain, newobj_class, error);
			if (!is_ok (error) || !mono_runtime_class_init_full (vtable, error)) {
				MonoException *exc = mono_error_convert_to_exception (error);
				g_assert (exc);
				THROW_EX (exc, ip);
			}
			error_init_reuse (error);
			MonoObject* o = NULL; // See the comment about GC safety.
			OBJREF (o) = mono_object_new_checked (domain, newobj_class, error);
			mono_error_cleanup (error); // FIXME: do not swallow the error
			error_init_reuse (error);
			EXCEPTION_CHECKPOINT;
			sp [0].data.o = o; // return value
			sp [1].data.o = o; // first parameter
#ifndef DISABLE_REMOTING
			if (mono_object_is_transparent_proxy (o)) {
				MonoMethod *remoting_invoke_method = mono_marshal_get_remoting_invoke_with_check (cmethod->method, error);
				mono_error_assert_ok (error);
				cmethod = mono_interp_get_imethod (domain, remoting_invoke_method, error);
				mono_error_assert_ok (error);
			}
#endif
			ip += 2;
			goto call_newobj;
		}
		MINT_IN_CASE(MINT_NEWOBJ_MAGIC) {
			ip += 2;

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_BYREFERENCE_CTOR) {
			gpointer arg0 = sp [-1].data.p;
			gpointer *byreference_this = (gpointer*)vt_sp;
			*byreference_this = arg0;

			sp [-1].data.p = vt_sp;
			vt_sp += MINT_VT_ALIGNMENT;
			ip++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_SPAN_CTOR) {
			gpointer ptr = sp [-2].data.p;
			int len = sp [-1].data.i;
			if (len < 0)
				THROW_EX (mono_get_exception_argument_out_of_range ("length"), ip);
			*(gpointer*)vt_sp = ptr;
			*(gint32*)((gpointer*)vt_sp + 1) = len;
			sp [-2].data.p = vt_sp;
#if SIZEOF_VOID_P == 8
			vt_sp += ALIGN_TO (12, MINT_VT_ALIGNMENT);
#else
			vt_sp += ALIGN_TO (8, MINT_VT_ALIGNMENT);
#endif
			sp--;
			ip++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_BYREFERENCE_GET_VALUE) {
			gpointer *byreference_this = (gpointer*)sp [-1].data.p;
			sp [-1].data.p = *byreference_this;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_UNSAFE_ADD_BYTE_OFFSET) {
			sp -= 2;
			sp [0].data.p = (guint8*)sp [0].data.p + sp [1].data.nati;
			sp ++;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_CLEAR_WITH_REFERENCES) {
			sp -= 2;
			gpointer p = sp [0].data.p;
			size_t size = sp [1].data.nati * sizeof (gpointer);
			mono_gc_bzero_aligned (p, size);
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_MARVIN_BLOCK) {
			sp -= 2;
			interp_intrins_marvin_block ((guint32*)sp [0].data.p, (guint32*)sp [1].data.p);
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_ASCII_CHARS_TO_UPPERCASE) {
			sp [-1].data.i = interp_intrins_ascii_chars_to_uppercase ((guint32)sp [-1].data.i);
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_MEMORYMARSHAL_GETARRAYDATAREF) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			sp[-1].data.p = (guint8*)o + MONO_STRUCT_OFFSET (MonoArray, vector);
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_ORDINAL_IGNORE_CASE_ASCII) {
			sp--;
			sp [-1].data.i = interp_intrins_ordinal_ignore_case_ascii ((guint32)sp [-1].data.i, (guint32)sp [0].data.i);
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_64ORDINAL_IGNORE_CASE_ASCII) {
			sp--;
			sp [-1].data.i = interp_intrins_64ordinal_ignore_case_ascii ((guint64)sp [-1].data.l, (guint64)sp [0].data.l);
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_U32_TO_DECSTR) {
			MonoArray **cache_addr = (MonoArray**)frame->imethod->data_items [ip [1]];
			MonoVTable *string_vtable = (MonoVTable*)frame->imethod->data_items [ip [2]];
			sp [-1].data.o = (MonoObject*)interp_intrins_u32_to_decstr ((guint32)sp [-1].data.i, *cache_addr, string_vtable);
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_WIDEN_ASCII_TO_UTF16) {
			sp -= 2;
			sp [-1].data.nati = interp_intrins_widen_ascii_to_utf16 ((guint8*)sp [-1].data.p, (mono_unichar2*)sp [0].data.p, sp [1].data.nati);
			ip++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_UNSAFE_BYTE_OFFSET) {
			sp -= 2;
			sp [0].data.nati = (guint8*)sp [1].data.p - (guint8*)sp [0].data.p;
			sp ++;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_RUNTIMEHELPERS_OBJECT_HAS_COMPONENT_SIZE) {
			MonoObject *obj = sp [-1].data.o;
			sp [-1].data.i = (obj->vtable->flags & MONO_VT_FLAG_ARRAY_OR_STRING) != 0;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CASTCLASS_INTERFACE)
		MINT_IN_CASE(MINT_ISINST_INTERFACE) {
			MonoObject* const o = sp [-1].data.o;
			if (o) {
				MonoClass* const c = (MonoClass*)frame->imethod->data_items [ip [1]];
				gboolean isinst;
				if (MONO_VTABLE_IMPLEMENTS_INTERFACE (o->vtable, m_class_get_interface_id (c))) {
					isinst = TRUE;
				} else if (m_class_is_array_special_interface (c) || mono_object_is_transparent_proxy (o)) {
					/* slow path */
					isinst = mono_interp_isinst (o, c); // FIXME: do not swallow the error
				} else {
					isinst = FALSE;
				}

				if (!isinst) {
					gboolean const isinst_instr = *ip == MINT_ISINST_INTERFACE;
					if (isinst_instr)
						sp [-1].data.p = NULL;
					else
						THROW_EX (mono_get_exception_invalid_cast (), ip);
				}
			}
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CASTCLASS_COMMON)
		MINT_IN_CASE(MINT_ISINST_COMMON) {
			MonoObject* const o = sp [-1].data.o;
			if (o) {
				MonoClass* const c = (MonoClass*)frame->imethod->data_items [ip [1]];
				gboolean isinst = mono_class_has_parent_fast (o->vtable->klass, c);

				if (!isinst) {
					gboolean const isinst_instr = *ip == MINT_ISINST_COMMON;
					if (isinst_instr)
						sp [-1].data.p = NULL;
					else
						THROW_EX (mono_get_exception_invalid_cast (), ip);
				}
			}
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CASTCLASS)
		MINT_IN_CASE(MINT_ISINST) {
			MonoObject* const o = sp [-1].data.o;
			if (o) {
				MonoClass* const c = (MonoClass*)frame->imethod->data_items [ip [1]];
				if (!mono_interp_isinst (o, c)) { // FIXME: do not swallow the error
					gboolean const isinst_instr = *ip == MINT_ISINST;
					if (isinst_instr)
						sp [-1].data.p = NULL;
					else
						THROW_EX (mono_get_exception_invalid_cast (), ip);
				}
			}
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_R_UN_I4)
			sp [-1].data.f = (double)(guint32)sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R_UN_I8)
			sp [-1].data.f = (double)(guint64)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_UNBOX) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			MonoClass* const c = (MonoClass*)frame->imethod->data_items[ip [1]];

			if (!(m_class_get_rank (o->vtable->klass) == 0 && m_class_get_element_class (o->vtable->klass) == m_class_get_element_class (c)))
				THROW_EX (mono_get_exception_invalid_cast (), ip);

			sp [-1].data.p = mono_object_unbox_internal (o);
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_THROW)
			--sp;
			if (!sp->data.p)
				sp->data.p = mono_get_exception_null_reference ();

			THROW_EX ((MonoException *)sp->data.p, ip);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CHECKPOINT)
			/* Do synchronous checking of abort requests */
			EXCEPTION_CHECKPOINT;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SAFEPOINT)
			/* Do synchronous checking of abort requests */
			EXCEPTION_CHECKPOINT;
			/* Poll safepoint */
			mono_threads_safepoint ();
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLDA_UNSAFE) {
			sp[-1].data.p = (char*)sp [-1].data.o + ip [1];
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDFLDA) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			sp[-1].data.p = (char *)o + ip [1];
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CKNULL_N) {
			/* Same as CKNULL, but further down the stack */
			int const n = ip [1];
			MonoObject* const o = sp [-n].data.o;
			NULL_CHECK (o);
			ip += 2;
			MINT_IN_BREAK;
		}

#define LDFLD_VT_UNALIGNED(datamem, fieldtype, unaligned) do { \
	gpointer p = sp [-1].data.p; \
	vt_sp -= ip [2]; \
	if (unaligned) \
		memcpy (&sp[-1].data.datamem, (char *)p + ip [1], sizeof (fieldtype)); \
	else \
		sp [-1].data.datamem = * (fieldtype *)((char *)p + ip [1]); \
	ip += 3; \
} while (0)

#define LDFLD_VT(datamem, fieldtype) LDFLD_VT_UNALIGNED(datamem, fieldtype, FALSE)

		MINT_IN_CASE(MINT_LDFLD_VT_I1) LDFLD_VT(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_VT_U1) LDFLD_VT(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_VT_I2) LDFLD_VT(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_VT_U2) LDFLD_VT(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_VT_I4) LDFLD_VT(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_VT_I8) LDFLD_VT(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_VT_R4) LDFLD_VT(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_VT_R8) LDFLD_VT(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_VT_O) LDFLD_VT(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_VT_I8_UNALIGNED) LDFLD_VT_UNALIGNED(l, gint64, TRUE); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_VT_R8_UNALIGNED) LDFLD_VT_UNALIGNED(f, double, TRUE); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDFLD_VT_VT) {
			gpointer p = sp [-1].data.p;

			vt_sp -= ip [2];
			sp [-1].data.p = vt_sp;
			memmove (vt_sp, (char *)p + ip [1], ip [3]);
			vt_sp += ip [3];
			ip += 4;
			MINT_IN_BREAK;
		}

#define LDFLD_UNALIGNED(datamem, fieldtype, unaligned) do { \
	MonoObject* const o = sp [-1].data.o; \
	NULL_CHECK (o); \
	if (unaligned) \
		memcpy (&sp[-1].data.datamem, (char *)o + ip [1], sizeof (fieldtype)); \
	else \
		sp[-1].data.datamem = * (fieldtype *)((char *)o + ip [1]) ; \
	ip += 2; \
} while (0)

#define LDFLD(datamem, fieldtype) LDFLD_UNALIGNED(datamem, fieldtype, FALSE)

		MINT_IN_CASE(MINT_LDFLD_I1) LDFLD(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_U1) LDFLD(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I2) LDFLD(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_U2) LDFLD(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I4) LDFLD(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I8) LDFLD(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_R4) LDFLD(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_R8) LDFLD(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_O) LDFLD(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I8_UNALIGNED) LDFLD_UNALIGNED(l, gint64, TRUE); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_R8_UNALIGNED) LDFLD_UNALIGNED(f, double, TRUE); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDFLD_VT) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);

			int size = READ32(ip + 2);
			sp [-1].data.p = vt_sp;
			memcpy (sp [-1].data.p, (char *)o + ip [1], size);
			vt_sp += ALIGN_TO (size, MINT_VT_ALIGNMENT);
			ip += 4;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LDRMFLD) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			mono_interp_load_remote_field (frame->imethod, o, ip, sp);
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDRMFLD_VT) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			vt_sp = mono_interp_load_remote_field_vt (frame->imethod, o, ip, sp, vt_sp);
			ip += 2;
			MINT_IN_BREAK;
		}

#define LDLOCFLD(datamem, fieldtype) do { \
	MonoObject *o = *(MonoObject**)(locals + ip [1]); \
	NULL_CHECK (o); \
	sp [0].data.datamem = * (fieldtype *)((char *)o + ip [2]) ; \
	sp++; \
	ip += 3; \
} while (0)
		MINT_IN_CASE(MINT_LDLOCFLD_I1) LDLOCFLD(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOCFLD_U1) LDLOCFLD(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOCFLD_I2) LDLOCFLD(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOCFLD_U2) LDLOCFLD(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOCFLD_I4) LDLOCFLD(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOCFLD_I8) LDLOCFLD(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOCFLD_R4) LDLOCFLD(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOCFLD_R8) LDLOCFLD(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOCFLD_O) LDLOCFLD(p, gpointer); MINT_IN_BREAK;

#define STFLD_UNALIGNED(datamem, fieldtype, unaligned) do { \
	MonoObject* const o = sp [-2].data.o; \
	NULL_CHECK (o); \
	sp -= 2; \
	if (unaligned) \
		memcpy ((char *)o + ip [1], &sp[1].data.datamem, sizeof (fieldtype)); \
	else \
		* (fieldtype *)((char *)o + ip [1]) = sp[1].data.datamem; \
	ip += 2; \
} while (0)

#define STFLD(datamem, fieldtype) STFLD_UNALIGNED(datamem, fieldtype, FALSE)

		MINT_IN_CASE(MINT_STFLD_I1) STFLD(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_U1) STFLD(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_I2) STFLD(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_U2) STFLD(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_I4) STFLD(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_I8) STFLD(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_R4) STFLD(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_R8) STFLD(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_O) {
			MonoObject* const o = sp [-2].data.o;
			NULL_CHECK (o);
			sp -= 2;
			mono_gc_wbarrier_set_field_internal (o, (char *) o + ip [1], sp [1].data.o);
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STFLD_I8_UNALIGNED) STFLD_UNALIGNED(l, gint64, TRUE); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_R8_UNALIGNED) STFLD_UNALIGNED(f, double, TRUE); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STFLD_VT_NOREF) {
			MonoObject* const o = sp [-2].data.o;
			NULL_CHECK (o);
			sp -= 2;

			guint16 offset = ip [1];
			guint16 vtsize = ip [2];

			memcpy ((char *) o + offset, sp [1].data.p, vtsize);

			vt_sp -= ALIGN_TO (vtsize, MINT_VT_ALIGNMENT);
			ip += 3;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_STFLD_VT) {
			MonoObject* const o = sp [-2].data.o;
			NULL_CHECK (o);
			sp -= 2;

			MonoClass *klass = (MonoClass*)frame->imethod->data_items[ip [2]];
			int const i32 = mono_class_value_size (klass, NULL);

			guint16 offset = ip [1];
			mono_value_copy_internal ((char *) o + offset, sp [1].data.p, klass);

			vt_sp -= ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STRMFLD) {
			MonoClassField *field;

			MonoObject* const o = sp [-2].data.o;
			NULL_CHECK (o);
			
			field = (MonoClassField*)frame->imethod->data_items[ip [1]];
			ip += 2;

#ifndef DISABLE_REMOTING
			if (mono_object_is_transparent_proxy (o)) {
				MonoClass *klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;
				mono_store_remote_field_checked (o, klass, field, &sp [-1].data, error);
				mono_interp_error_cleanup (error); /* FIXME: don't swallow the error */
			} else
#endif
				stackval_to_data (field->type, &sp [-1], (char*)o + field->offset, FALSE);

			sp -= 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STRMFLD_VT)

			NULL_CHECK (sp [-2].data.o);
			vt_sp -= mono_interp_store_remote_field_vt (frame, ip, sp, error);
			ip += 2;
			sp -= 2;
			MINT_IN_BREAK;

#define STLOCFLD(datamem, fieldtype) do { \
	MonoObject *o = *(MonoObject**)(locals + ip [1]); \
	NULL_CHECK (o); \
	sp--; \
	* (fieldtype *)((char *)o + ip [2]) = sp [0].data.datamem; \
	ip += 3; \
} while (0)
		MINT_IN_CASE(MINT_STLOCFLD_I1) STLOCFLD(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOCFLD_U1) STLOCFLD(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOCFLD_I2) STLOCFLD(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOCFLD_U2) STLOCFLD(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOCFLD_I4) STLOCFLD(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOCFLD_I8) STLOCFLD(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOCFLD_R4) STLOCFLD(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOCFLD_R8) STLOCFLD(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOCFLD_O) {
			MonoObject *o = *(MonoObject**)(locals + ip [1]);
			NULL_CHECK (o);
			sp--;
			mono_gc_wbarrier_set_field_internal (o, (char *) o + ip [2], sp [0].data.o);
			ip += 3;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LDSFLDA) {
			MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [1]];
			INIT_VTABLE (vtable);
			sp->data.p = frame->imethod->data_items [ip [2]];
			ip += 3;
			++sp;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LDSSFLDA) {
			guint32 offset = READ32(ip + 1);
			sp->data.p = mono_get_special_static_data (offset);
			ip += 3;
			++sp;
			MINT_IN_BREAK;
		}

/* We init class here to preserve cctor order */
#define LDSFLD(datamem, fieldtype) { \
	MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [1]]; \
	INIT_VTABLE (vtable); \
	sp[0].data.datamem = * (fieldtype *)(frame->imethod->data_items [ip [2]]) ; \
	ip += 3; \
	sp++; \
	}

		MINT_IN_CASE(MINT_LDSFLD_I1) LDSFLD(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_U1) LDSFLD(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_I2) LDSFLD(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_U2) LDSFLD(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_I4) LDSFLD(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_I8) LDSFLD(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_R4) LDSFLD(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_R8) LDSFLD(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_O) LDSFLD(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDSFLD_VT) {
			MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [1]];
			INIT_VTABLE (vtable);
			sp->data.p = vt_sp;

			gpointer addr = frame->imethod->data_items [ip [2]];
			int const i32 = READ32 (ip + 3);
			memcpy (vt_sp, addr, i32);
			vt_sp += ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			ip += 5;
			++sp;
			MINT_IN_BREAK;
		}

#define LDTSFLD(datamem, fieldtype) { \
	MonoInternalThread *thread = mono_thread_internal_current (); \
	guint32 offset = READ32 (ip + 1); \
	gpointer addr = ((char*)thread->static_data [offset & 0x3f]) + (offset >> 6); \
	sp[0].data.datamem = *(fieldtype*)addr; \
	ip += 3; \
	++sp; \
	}
		MINT_IN_CASE(MINT_LDTSFLD_I1) LDTSFLD(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDTSFLD_U1) LDTSFLD(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDTSFLD_I2) LDTSFLD(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDTSFLD_U2) LDTSFLD(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDTSFLD_I4) LDTSFLD(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDTSFLD_I8) LDTSFLD(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDTSFLD_R4) LDTSFLD(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDTSFLD_R8) LDTSFLD(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDTSFLD_O) LDTSFLD(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDSSFLD) {
			guint32 offset = READ32(ip + 2);
			gpointer addr = mono_get_special_static_data (offset);
			MonoClassField *field = (MonoClassField*)frame->imethod->data_items [ip [1]];
			stackval_from_data (field->type, sp, addr, FALSE);
			ip += 4;
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSSFLD_VT) {
			guint32 offset = READ32(ip + 1);
			gpointer addr = mono_get_special_static_data (offset);

			int size = READ32 (ip + 3);
			memcpy (vt_sp, addr, size);
			sp->data.p = vt_sp;
			vt_sp += ALIGN_TO (size, MINT_VT_ALIGNMENT);
			ip += 5;
			++sp;
			MINT_IN_BREAK;
		}
#define STSFLD(datamem, fieldtype) { \
	MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [1]]; \
	INIT_VTABLE (vtable); \
	sp --; \
	* (fieldtype *)(frame->imethod->data_items [ip [2]]) = sp[0].data.datamem; \
	ip += 3; \
	}

		MINT_IN_CASE(MINT_STSFLD_I1) STSFLD(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_U1) STSFLD(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_I2) STSFLD(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_U2) STSFLD(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_I4) STSFLD(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_I8) STSFLD(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_R4) STSFLD(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_R8) STSFLD(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_O) STSFLD(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STSFLD_VT) {
			MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [1]];
			INIT_VTABLE (vtable);
			int const i32 = READ32 (ip + 3);
			gpointer addr = frame->imethod->data_items [ip [2]];

			memcpy (addr, sp [-1].data.vt, i32);
			vt_sp -= ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			ip += 5;
			--sp;
			MINT_IN_BREAK;
		}

#define STTSFLD(datamem, fieldtype) { \
	MonoInternalThread *thread = mono_thread_internal_current (); \
	guint32 offset = READ32 (ip + 1); \
	gpointer addr = ((char*)thread->static_data [offset & 0x3f]) + (offset >> 6); \
	sp--; \
	*(fieldtype*)addr = sp[0].data.datamem; \
	ip += 3; \
	}

		MINT_IN_CASE(MINT_STTSFLD_I1) STTSFLD(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STTSFLD_U1) STTSFLD(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STTSFLD_I2) STTSFLD(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STTSFLD_U2) STTSFLD(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STTSFLD_I4) STTSFLD(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STTSFLD_I8) STTSFLD(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STTSFLD_R4) STTSFLD(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STTSFLD_R8) STTSFLD(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STTSFLD_O) STTSFLD(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STSSFLD) {
			guint32 offset = READ32(ip + 2);
			gpointer addr = mono_get_special_static_data (offset);
			MonoClassField *field = (MonoClassField*)frame->imethod->data_items [ip [1]];
			--sp;
			stackval_to_data (field->type, sp, addr, FALSE);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STSSFLD_VT) {
			guint32 offset = READ32(ip + 1);
			gpointer addr = mono_get_special_static_data (offset);
			--sp;
			int size = READ32 (ip + 3);
			memcpy (addr, sp->data.vt, size);
			vt_sp -= ALIGN_TO (size, MINT_VT_ALIGNMENT);
			ip += 5;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_STOBJ_VT) {
			int size;
			MonoClass* const c = (MonoClass*)frame->imethod->data_items[ip [1]];
			ip += 2;
			size = mono_class_value_size (c, NULL);
			mono_value_copy_internal (sp [-2].data.p, sp [-1].data.p, c);
			vt_sp -= ALIGN_TO (size, MINT_VT_ALIGNMENT);
			sp -= 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_UN_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > G_MAXINT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint32)sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U8_I4)
			if (sp [-1].data.i < 0)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.l = sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U8_I8)
			if (sp [-1].data.l < 0)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I8_U8)
			if ((guint64) sp [-1].data.l > G_MAXINT64)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U8_R4) {
			guint64 res = (guint64)sp [-1].data.f_r4;
			if (mono_isnan (sp [-1].data.f_r4) || mono_trunc (sp [-1].data.f_r4) != res)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.l = res;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U8_R8) {
			guint64 res = (guint64)sp [-1].data.f;
			if (mono_isnan (sp [-1].data.f) || mono_trunc (sp [-1].data.f) != res)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.l = res;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I8_UN_R8) {
			gint64 res = (gint64)sp [-1].data.f;
			if (res < 0 || mono_isnan (sp [-1].data.f) || mono_trunc (sp [-1].data.f) != res)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.l = res;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I8_UN_R4) {
			gint64 res = (gint64)sp [-1].data.f_r4;
			if (res < 0 || mono_isnan (sp [-1].data.f_r4) || mono_trunc (sp [-1].data.f_r4) != res)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.l = res;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I8_R4) {
			gint64 res = (gint64)sp [-1].data.f_r4;
			if (mono_isnan (sp [-1].data.f_r4) || mono_trunc (sp [-1].data.f_r4) != res)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.l = res;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I8_R8) {
			gint64 res = (gint64)sp [-1].data.f;
			if (mono_isnan (sp [-1].data.f) || mono_trunc (sp [-1].data.f) != res)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.l = res;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BOX) {
			mono_interp_box (frame, ip, sp);
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BOX_VT) {
			vt_sp -= mono_interp_box_vt (frame, ip, sp);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BOX_NULLABLE) {
			vt_sp -= mono_interp_box_nullable (frame, ip, sp, error);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_NEWARR) {
			MonoVTable *vtable = (MonoVTable*)frame->imethod->data_items[ip [1]];
			sp [-1].data.o = (MonoObject*) mono_array_new_specific_checked (vtable, sp [-1].data.i, error);
			if (!is_ok (error)) {
				THROW_EX (mono_error_convert_to_exception (error), ip);
			}
			ip += 2;
			/*if (profiling_classes) {
				guint count = GPOINTER_TO_UINT (g_hash_table_lookup (profiling_classes, o->vtable->klass));
				count++;
				g_hash_table_insert (profiling_classes, o->vtable->klass, GUINT_TO_POINTER (count));
			}*/

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDLEN) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			sp [-1].data.nati = mono_array_length_internal ((MonoArray *)o);
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDLEN_SPAN) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			gsize offset_length = (gsize)(gint16)ip [1];
			sp [-1].data.nati = *(gint32 *) ((guint8 *) o + offset_length);
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_GETCHR) {
			MonoString *s;
			s = (MonoString*)sp [-2].data.p;
			NULL_CHECK (s);
			int const i32 = sp [-1].data.i;
			if (i32 < 0 || i32 >= mono_string_length_internal (s))
				THROW_EX (mono_get_exception_index_out_of_range (), ip);
			--sp;
			sp [-1].data.i = mono_string_chars_internal (s)[i32];
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_GETITEM_SPAN) {
			guint8 * const span = (guint8 *) sp [-2].data.p;
			const int index = sp [-1].data.i;
			sp--;

			NULL_CHECK (span);

			const gsize offset_length = (gsize)(gint16)ip [2];

			const gint32 length = *(gint32 *) (span + offset_length);
			if (index < 0 || index >= length)
				THROW_EX (mono_get_exception_index_out_of_range (), ip);

			const gsize element_size = (gsize)(gint16)ip [1];
			const gsize offset_pointer = (gsize)(gint16)ip [3];

			const gpointer pointer = *(gpointer *)(span + offset_pointer);
			sp [-1].data.p = (guint8 *) pointer + index * element_size;

			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STRLEN) {
			++ip;
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			sp [-1].data.i = mono_string_length_internal ((MonoString*) o);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ARRAY_RANK) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			sp [-1].data.i = m_class_get_rank (mono_object_class (o));
			ip++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ARRAY_ELEMENT_SIZE) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			sp [-1].data.i = mono_array_element_size (mono_object_class (o));
			ip++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ARRAY_IS_PRIMITIVE) {
			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);
			sp [-1].data.i = m_class_is_primitive (m_class_get_element_class (mono_object_class (o)));
			ip++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDELEMA1) {
			/* No bounds, one direction */
			MonoArray *ao = (MonoArray*)sp [-2].data.o;
			NULL_CHECK (ao);
			gint32 const index = sp [-1].data.i;
			if (index >= ao->max_length)
				THROW_EX (mono_get_exception_index_out_of_range (), ip);
			gint32 const size = READ32 (ip + 1);
			sp [-2].data.p = mono_array_addr_with_size_fast (ao, size, index);
			ip += 3;
			sp --;

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDELEMA) {
			guint16 rank = ip [1];
			gint32 const esize = READ32 (ip + 2);
			ip += 4;
			sp -= rank;

			MonoArray* const ao = (MonoArray*) sp [-1].data.o;
			NULL_CHECK (ao);

			g_assert (ao->bounds);
			guint32 pos = 0;
			for (int i = 0; i < rank; i++) {
				guint32 idx = sp [i].data.i;
				guint32 lower = ao->bounds [i].lower_bound;
				guint32 len = ao->bounds [i].length;
				if (idx < lower || (idx - lower) >= len)
					THROW_EX (mono_get_exception_index_out_of_range (), ip);
				pos = (pos * len) + idx - lower;
			}

			sp [-1].data.p = mono_array_addr_with_size_fast (ao, esize, pos);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDELEMA_TC) {
			guint16 rank = ip [1];
			ip += 3;
			sp -= rank;

			MonoObject* const o = sp [-1].data.o;
			NULL_CHECK (o);

			MonoClass *klass = (MonoClass*)frame->imethod->data_items [ip [-3 + 2]];
			const gboolean needs_typecheck = ip [-3] == MINT_LDELEMA_TC;
			MonoException *ex = ves_array_element_address (frame, klass, (MonoArray *) o, sp, needs_typecheck);
			if (ex)
				THROW_EX (ex, ip);
			MINT_IN_BREAK;
		}

#define LDELEM(datamem,elemtype) do { \
	sp--; \
	MonoArray *o = (MonoArray*)sp [-1].data.p; \
	NULL_CHECK (o); \
	gint32 aindex = sp [0].data.i; \
	if (aindex >= mono_array_length_internal (o)) \
		THROW_EX (mono_get_exception_index_out_of_range (), ip); \
	sp [-1].data.datamem = mono_array_get_fast (o, elemtype, aindex); \
	ip++; \
} while (0)
		MINT_IN_CASE(MINT_LDELEM_I1) LDELEM(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_U1) LDELEM(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_I2) LDELEM(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_U2) LDELEM(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_I4) LDELEM(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_U4) LDELEM(i, guint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_I8) LDELEM(l, guint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_I)  LDELEM(nati, mono_i); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_R4) LDELEM(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_R8) LDELEM(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_REF) LDELEM(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_VT) {
			sp--;
			MonoArray *o = (MonoArray*)sp [-1].data.p;
			NULL_CHECK (o);
			mono_u aindex = sp [0].data.i;
			if (aindex >= mono_array_length_internal (o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip);

			int i32 = READ32 (ip + 1);
			char *src_addr = mono_array_addr_with_size_fast ((MonoArray *) o, i32, aindex);
			sp [-1].data.vt = vt_sp;
			// Copying to vtstack. No wbarrier needed
			memcpy (sp [-1].data.vt, src_addr, i32);
			vt_sp += ALIGN_TO (i32, MINT_VT_ALIGNMENT);

			ip += 3;
			MINT_IN_BREAK;
		}
#define STELEM_PROLOG(o, aindex) do { \
	sp -= 3; \
	o = (MonoArray*)sp [0].data.p; \
	NULL_CHECK (o); \
	aindex = sp [1].data.i; \
	if (aindex >= mono_array_length_internal (o)) \
		THROW_EX (mono_get_exception_index_out_of_range (), ip); \
} while (0)

#define STELEM(datamem,elemtype) do { \
	MonoArray *o; \
	gint32 aindex; \
	STELEM_PROLOG(o, aindex); \
	mono_array_set_fast (o, elemtype, aindex, sp [2].data.datamem); \
	ip++; \
} while (0)
		MINT_IN_CASE(MINT_STELEM_I1) STELEM(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_U1) STELEM(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_I2) STELEM(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_U2) STELEM(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_I4) STELEM(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_I8) STELEM(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_I)  STELEM(nati, mono_i); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_R4) STELEM(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_R8) STELEM(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_REF) {
			MonoArray *o;
			gint32 aindex;
			STELEM_PROLOG(o, aindex);

			if (sp [2].data.o) {
				gboolean isinst = mono_interp_isinst (sp [2].data.o, m_class_get_element_class (mono_object_class (o)));
				if (!isinst)
					THROW_EX (mono_get_exception_array_type_mismatch (), ip);
			}
			mono_array_setref_fast ((MonoArray *) o, aindex, sp [2].data.p);
			ip++;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_STELEM_VT) {
			MonoArray *o;
			gint32 aindex;
			STELEM_PROLOG(o, aindex);

			MonoClass *klass_vt = (MonoClass*)frame->imethod->data_items [ip [1]];
			int const i32 = READ32 (ip + 2);
			char *dst_addr = mono_array_addr_with_size_fast ((MonoArray *) o, i32, aindex);

			mono_value_copy_internal (dst_addr, sp [2].data.vt, klass_vt);
			vt_sp -= ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_U4)
			if (sp [-1].data.i < 0)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I4_I8)
			if (sp [-1].data.l < G_MININT32 || sp [-1].data.l > G_MAXINT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint32) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I4_U8)
			if ((guint64)sp [-1].data.l > G_MAXINT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint32) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I4_R4) {
			gint32 res = (gint32)sp [-1].data.f_r4;
			if (mono_isnan (sp [-1].data.f_r4) || mono_trunc (sp [-1].data.f_r4) != res)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = res;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_R8)
			if (sp [-1].data.f < G_MININT32 || sp [-1].data.f > G_MAXINT32 || isnan (sp [-1].data.f))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint32) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U4_I4)
			if (sp [-1].data.i < 0)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U4_I8)
			if (sp [-1].data.l < 0 || sp [-1].data.l > G_MAXUINT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint32) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U4_R4) {
			guint32 res = (guint32)sp [-1].data.f_r4;
			if (mono_isnan (sp [-1].data.f_r4) || mono_trunc (sp [-1].data.f_r4) != res)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = res;
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U4_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > G_MAXUINT32 || isnan (sp [-1].data.f))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint32) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_I4)
			if (sp [-1].data.i < G_MININT16 || sp [-1].data.i > G_MAXINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_U4)
			if (sp [-1].data.i < 0 || sp [-1].data.i > G_MAXINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_I8)
			if (sp [-1].data.l < G_MININT16 || sp [-1].data.l > G_MAXINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint16) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_U8)
			if (sp [-1].data.l < 0 || sp [-1].data.l > G_MAXINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint16) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_R4)
			if (sp [-1].data.f_r4 < G_MININT16 || sp [-1].data.f_r4 > G_MAXINT16 || isnan (sp [-1].data.f_r4))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint16) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_R8)
			if (sp [-1].data.f < G_MININT16 || sp [-1].data.f > G_MAXINT16 || isnan (sp [-1].data.f))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint16) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_UN_R4)
			if (sp [-1].data.f_r4 < 0 || sp [-1].data.f_r4 > G_MAXINT16 || isnan (sp [-1].data.f_r4))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint16) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_UN_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > G_MAXINT16 || isnan (sp [-1].data.f))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint16) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U2_I4)
			if (sp [-1].data.i < 0 || sp [-1].data.i > G_MAXUINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U2_I8)
			if (sp [-1].data.l < 0 || sp [-1].data.l > G_MAXUINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint16) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U2_R4)
			if (sp [-1].data.f_r4 < 0 || sp [-1].data.f_r4 > G_MAXUINT16 || isnan (sp [-1].data.f_r4))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint16) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U2_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > G_MAXUINT16 || isnan (sp [-1].data.f))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint16) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_I4)
			if (sp [-1].data.i < G_MININT8 || sp [-1].data.i > G_MAXINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_U4)
			if (sp [-1].data.i < 0 || sp [-1].data.i > G_MAXINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_I8)
			if (sp [-1].data.l < G_MININT8 || sp [-1].data.l > G_MAXINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint8) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_U8)
			if (sp [-1].data.l < 0 || sp [-1].data.l > G_MAXINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint8) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_R4)
			if (sp [-1].data.f_r4 < G_MININT8 || sp [-1].data.f_r4 > G_MAXINT8 || isnan (sp [-1].data.f_r4))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint8) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_R8)
			if (sp [-1].data.f < G_MININT8 || sp [-1].data.f > G_MAXINT8 || isnan (sp [-1].data.f))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint8) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_UN_R4)
			if (sp [-1].data.f_r4 < 0 || sp [-1].data.f_r4 > G_MAXINT8 || isnan (sp [-1].data.f_r4))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint8) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_UN_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > G_MAXINT8 || isnan (sp [-1].data.f))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint8) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U1_I4)
			if (sp [-1].data.i < 0 || sp [-1].data.i > G_MAXUINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U1_I8)
			if (sp [-1].data.l < 0 || sp [-1].data.l > G_MAXUINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint8) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U1_R4)
			if (sp [-1].data.f_r4 < 0 || sp [-1].data.f_r4 > G_MAXUINT8 || isnan (sp [-1].data.f_r4))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint8) sp [-1].data.f_r4;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U1_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > G_MAXUINT8 || isnan (sp [-1].data.f))
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint8) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CKFINITE)
			if (!mono_isfinite (sp [-1].data.f))
				THROW_EX (mono_get_exception_arithmetic (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MKREFANY) {
			MonoClass* const c = (MonoClass*)frame->imethod->data_items [ip [1]];

			/* The value address is on the stack */
			gpointer addr = sp [-1].data.p;
			/* Push the typedref value on the stack */
			sp [-1].data.p = vt_sp;
			vt_sp += ALIGN_TO (sizeof (MonoTypedRef), MINT_VT_ALIGNMENT);

			MonoTypedRef *tref = (MonoTypedRef*)sp [-1].data.p;
			tref->klass = c;
			tref->type = m_class_get_byval_arg (c);
			tref->value = addr;

			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REFANYTYPE) {
			MonoTypedRef *tref = (MonoTypedRef*)sp [-1].data.p;
			MonoType *type = tref->type;

			vt_sp -= ALIGN_TO (sizeof (MonoTypedRef), MINT_VT_ALIGNMENT);
			sp [-1].data.p = vt_sp;
			vt_sp += 8;
			*(gpointer*)sp [-1].data.p = type;
			ip ++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REFANYVAL) {
			MonoTypedRef *tref = (MonoTypedRef*)sp [-1].data.p;
			gpointer addr = tref->value;

			MonoClass* const c = (MonoClass*)frame->imethod->data_items [ip [1]];
			if (c != tref->klass)
				THROW_EX (mono_get_exception_invalid_cast (), ip);

			vt_sp -= ALIGN_TO (sizeof (MonoTypedRef), MINT_VT_ALIGNMENT);

			sp [-1].data.p = addr;
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDTOKEN)
			sp->data.p = vt_sp;
			vt_sp += 8;
			* (gpointer *)sp->data.p = frame->imethod->data_items[ip [1]];
			ip += 2;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_OVF_I4)
			if (CHECK_ADD_OVERFLOW (sp [-2].data.i, sp [-1].data.i))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(i, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_OVF_I8)
			if (CHECK_ADD_OVERFLOW64 (sp [-2].data.l, sp [-1].data.l))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(l, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_OVF_UN_I4)
			if (CHECK_ADD_OVERFLOW_UN (sp [-2].data.i, sp [-1].data.i))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP_CAST(i, +, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_OVF_UN_I8)
			if (CHECK_ADD_OVERFLOW64_UN (sp [-2].data.l, sp [-1].data.l))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP_CAST(l, +, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_OVF_I4)
			if (CHECK_MUL_OVERFLOW (sp [-2].data.i, sp [-1].data.i))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(i, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_OVF_I8)
			if (CHECK_MUL_OVERFLOW64 (sp [-2].data.l, sp [-1].data.l))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(l, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_OVF_UN_I4)
			if (CHECK_MUL_OVERFLOW_UN (sp [-2].data.i, sp [-1].data.i))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP_CAST(i, *, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_OVF_UN_I8)
			if (CHECK_MUL_OVERFLOW64_UN (sp [-2].data.l, sp [-1].data.l))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP_CAST(l, *, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_OVF_I4)
			if (CHECK_SUB_OVERFLOW (sp [-2].data.i, sp [-1].data.i))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(i, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_OVF_I8)
			if (CHECK_SUB_OVERFLOW64 (sp [-2].data.l, sp [-1].data.l))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(l, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_OVF_UN_I4)
			if (CHECK_SUB_OVERFLOW_UN (sp [-2].data.i, sp [-1].data.i))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP_CAST(i, -, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_OVF_UN_I8)
			if (CHECK_SUB_OVERFLOW64_UN (sp [-2].data.l, sp [-1].data.l))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP_CAST(l, -, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_START_ABORT_PROT)
			mono_threads_begin_abort_protected_block ();
			ip ++;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ENDFINALLY) {
			gboolean pending_abort = mono_threads_end_abort_protected_block ();
			ip ++;

			// After mono_threads_end_abort_protected_block to conserve stack.
			const int clause_index = *ip;

			// clause_args stores the clause args only for the first frame that
			// we started executing in interp_exec_method. If we are exiting the
			// current frame at this finally clause, we need to make sure that
			// this is the first frame invoked with interp_exec_method.
			if (clause_args && clause_args->exec_frame == frame && clause_index == clause_args->exit_clause)
				goto exit_clause;

			// endfinally empties the stack
			vt_sp = (guchar*)frame->stack + frame->imethod->total_locals_size;
			sp = (stackval*)(vt_sp + frame->imethod->vt_stack_size);

			if (finally_ips) {
				ip = (const guint16*)finally_ips->data;
				finally_ips = g_slist_remove (finally_ips, ip);
				/* Throw abort after the last finally block to avoid confusing EH */
				if (pending_abort && !finally_ips)
					EXCEPTION_CHECKPOINT;
				// goto main_loop instead of MINT_IN_DISPATCH helps the compiler and therefore conserves stack.
				// This is a slow/rare path and conserving stack is preferred over its performance otherwise.
				goto main_loop;
			}
			ves_abort();
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LEAVE)
		MINT_IN_CASE(MINT_LEAVE_S)
		MINT_IN_CASE(MINT_LEAVE_CHECK)
		MINT_IN_CASE(MINT_LEAVE_S_CHECK) {
			guint32 ip_offset = ip - frame->imethod->code;
			// leave empties the stack
			vt_sp = (guchar*)frame->stack + frame->imethod->total_locals_size;
			sp = (stackval*)(vt_sp + frame->imethod->vt_stack_size);

			int opcode = *ip;
			gboolean const check = opcode == MINT_LEAVE_CHECK || opcode == MINT_LEAVE_S_CHECK;

			if (check && frame->imethod->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE) {
				MonoException *abort_exc = mono_interp_leave (frame);
				if (abort_exc)
					THROW_EX (abort_exc, ip);
			}

			opcode = *ip; // Refetch to avoid register/stack pressure.
			gboolean const short_offset = opcode == MINT_LEAVE_S || opcode == MINT_LEAVE_S_CHECK;
			ip += short_offset ? (short)*(ip + 1) : (gint32)READ32 (ip + 1);
			const guint16 *endfinally_ip = ip;
			GSList *old_list = finally_ips;
#if DEBUG_INTERP
			if (tracing)
				g_print ("* Handle finally IL_%04x\n", endfinally_ip - frame->imethod->code);
#endif
			finally_ips = g_slist_prepend (finally_ips, (void *)endfinally_ip);

			for (int i = frame->imethod->num_clauses - 1; i >= 0; i--) {
				MonoExceptionClause* const clause = &frame->imethod->clauses [i];
				if (MONO_OFFSET_IN_CLAUSE (clause, ip_offset) && !(MONO_OFFSET_IN_CLAUSE (clause, endfinally_ip - frame->imethod->code))) {
					if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
						ip = frame->imethod->code + clause->handler_offset;
						finally_ips = g_slist_prepend (finally_ips, (gpointer) ip);
#if DEBUG_INTERP
						if (tracing)
							g_print ("* Found finally at IL_%04x with exception: %s\n", clause->handler_offset, context->has_resume_state ? "yes": "no");
#endif
					}
				}
			}

			if (old_list != finally_ips && finally_ips) {
				ip = (const guint16*)finally_ips->data;
				finally_ips = g_slist_remove (finally_ips, ip);
				// goto main_loop instead of MINT_IN_DISPATCH helps the compiler and therefore conserves stack.
				// This is a slow/rare path and conserving stack is preferred over its performance otherwise.
				goto main_loop;
			}

			ves_abort();
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ICALL_V_V) 
		MINT_IN_CASE(MINT_ICALL_V_P)
		MINT_IN_CASE(MINT_ICALL_P_V) 
		MINT_IN_CASE(MINT_ICALL_P_P)
		MINT_IN_CASE(MINT_ICALL_PP_V)
		MINT_IN_CASE(MINT_ICALL_PP_P)
		MINT_IN_CASE(MINT_ICALL_PPP_V)
		MINT_IN_CASE(MINT_ICALL_PPP_P)
		MINT_IN_CASE(MINT_ICALL_PPPP_V)
		MINT_IN_CASE(MINT_ICALL_PPPP_P)
		MINT_IN_CASE(MINT_ICALL_PPPPP_V)
		MINT_IN_CASE(MINT_ICALL_PPPPP_P)
		MINT_IN_CASE(MINT_ICALL_PPPPPP_V)
		MINT_IN_CASE(MINT_ICALL_PPPPPP_P)
			frame->state.ip = ip + 2;
			sp = do_icall_wrapper (frame, NULL, *ip, sp, frame->imethod->data_items [ip [1]], FALSE);
			EXCEPTION_CHECKPOINT_GC_UNSAFE;
			CHECK_RESUME_STATE (context);
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_LDPTR) 
			sp->data.p = frame->imethod->data_items [ip [1]];
			ip += 2;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_NEWOBJ)
			sp->data.o = mono_interp_new (frame->imethod->domain, (MonoClass*)frame->imethod->data_items [ip [1]]); // FIXME: do not swallow the error
			ip += 2;
			sp++;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_RETOBJ)
			++ip;
			sp--;
			stackval_from_data (mono_method_signature_internal (frame->imethod->method)->ret, frame->retval, sp->data.p,
			     mono_method_signature_internal (frame->imethod->method)->pinvoke);
			if (sp > frame->stack)
				g_warning_d ("retobj: more values on stack: %d", sp - frame->stack);
			frame_data_allocator_pop (&context->data_stack, frame);
			goto exit_frame;
		MINT_IN_CASE(MINT_MONO_SGEN_THREAD_INFO)
			sp->data.p = mono_tls_get_sgen_thread_info ();
			sp++;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_MEMORY_BARRIER) {
			++ip;
			mono_memory_barrier ();
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_MONO_LDDOMAIN)
			sp->data.p = mono_domain_get ();
			++sp;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_GET_SP)
			sp->data.p = frame;
			++sp;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SDB_INTR_LOC)
			if (G_UNLIKELY (ss_enabled)) {
				typedef void (*T) (void);
				static T ss_tramp;

				if (!ss_tramp) {
					void *tramp = mini_get_single_step_trampoline ();
					mono_memory_barrier ();
					ss_tramp = (T)tramp;
				}

				/*
				 * Make this point to the MINT_SDB_SEQ_POINT instruction which follows this since
				 * the address of that instruction is stored as the seq point address. Add also
				 * 1 to offset subtraction from interp_frame_get_ip.
				 */
				frame->state.ip = ip + 2;

				/*
				 * Use the same trampoline as the JIT. This ensures that
				 * the debugger has the context for the last interpreter
				 * native frame.
				 */
				do_debugger_tramp (ss_tramp, frame);

				CHECK_RESUME_STATE (context);
			}
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SDB_SEQ_POINT)
			/* Just a placeholder for a breakpoint */
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SDB_BREAKPOINT) {
			typedef void (*T) (void);
			static T bp_tramp;
			if (!bp_tramp) {
				void *tramp = mini_get_breakpoint_trampoline ();
				mono_memory_barrier ();
				bp_tramp = (T)tramp;
			}

			/* Add 1 to offset subtraction from interp_frame_get_ip */
			frame->state.ip = ip + 1;

			/* Use the same trampoline as the JIT */
			do_debugger_tramp (bp_tramp, frame);

			CHECK_RESUME_STATE (context);

			++ip;
			MINT_IN_BREAK;
		}

#define RELOP(datamem, op) \
	--sp; \
	sp [-1].data.i = sp [-1].data.datamem op sp [0].data.datamem; \
	++ip;

#define RELOP_FP(datamem, op, noorder) \
	--sp; \
	if (mono_isunordered (sp [-1].data.datamem, sp [0].data.datamem)) \
		sp [-1].data.i = noorder; \
	else \
		sp [-1].data.i = sp [-1].data.datamem op sp [0].data.datamem; \
	++ip;

		MINT_IN_CASE(MINT_CEQ_I4)
			RELOP(i, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CEQ0_I4)
			sp [-1].data.i = (sp [-1].data.i == 0);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CEQ_I8)
			RELOP(l, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CEQ_R4)
			RELOP_FP(f_r4, ==, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CEQ_R8)
			RELOP_FP(f, ==, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CNE_I4)
			RELOP(i, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CNE_I8)
			RELOP(l, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CNE_R4)
			RELOP_FP(f_r4, !=, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CNE_R8)
			RELOP_FP(f, !=, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_I4)
			RELOP(i, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_I8)
			RELOP(l, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_R4)
			RELOP_FP(f_r4, >, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_R8)
			RELOP_FP(f, >, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGE_I4)
			RELOP(i, >=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGE_I8)
			RELOP(l, >=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGE_R4)
			RELOP_FP(f_r4, >=, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGE_R8)
			RELOP_FP(f, >=, 0);
			MINT_IN_BREAK;

#define RELOP_CAST(datamem, op, type) \
	--sp; \
	sp [-1].data.i = (type)sp [-1].data.datamem op (type)sp [0].data.datamem; \
	++ip;

		MINT_IN_CASE(MINT_CGE_UN_I4)
			RELOP_CAST(l, >=, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGE_UN_I8)
			RELOP_CAST(l, >=, guint64);
			MINT_IN_BREAK;

		MINT_IN_CASE(MINT_CGT_UN_I4)
			RELOP_CAST(i, >, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_UN_I8)
			RELOP_CAST(l, >, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_UN_R4)
			RELOP_FP(f_r4, >, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_UN_R8)
			RELOP_FP(f, >, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_I4)
			RELOP(i, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_I8)
			RELOP(l, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_R4)
			RELOP_FP(f_r4, <, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_R8)
			RELOP_FP(f, <, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_I4)
			RELOP_CAST(i, <, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_I8)
			RELOP_CAST(l, <, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_R4)
			RELOP_FP(f_r4, <, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_R8)
			RELOP_FP(f, <, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_I4)
			RELOP(i, <=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_I8)
			RELOP(l, <=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_UN_I4)
			RELOP_CAST(l, <=, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_UN_I8)
			RELOP_CAST(l, <=, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_R4)
			RELOP_FP(f_r4, <=, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_R8)
			RELOP_FP(f, <=, 0);
			MINT_IN_BREAK;

#undef RELOP
#undef RELOP_FP
#undef RELOP_CAST

		MINT_IN_CASE(MINT_LDFTN) {
			sp->data.p = frame->imethod->data_items [ip [1]];
			++sp;
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDVIRTFTN) {
			InterpMethod *m = (InterpMethod*)frame->imethod->data_items [ip [1]];
			--sp;
			NULL_CHECK (sp->data.p);
				
			sp->data.p = get_virtual_method (m, sp->data.o->vtable);
			ip += 2;
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDFTN_DYNAMIC) {
			error_init_reuse (error);
			InterpMethod *m = mono_interp_get_imethod (mono_domain_get (), (MonoMethod*) sp [-1].data.p, error);
			mono_error_assert_ok (error);
			sp [-1].data.p = m;
			ip++;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LDARG_VT) {
			sp->data.p = vt_sp;
			int const i32 = READ32 (ip + 2);
			memcpy(sp->data.p, frame->stack [ip [1]].data.p, i32);
			vt_sp += ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			ip += 4;
			++sp;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_STARG_VT) {
			int const i32 = READ32 (ip + 2);
			--sp;
			memcpy(frame->stack [ip [1]].data.p, sp->data.p, i32);
			vt_sp -= ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			ip += 4;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_PROF_ENTER) {
			guint16 flag = ip [1];
			ip += 2;

			if ((flag & TRACING_FLAG) || ((flag & PROFILING_FLAG) && MONO_PROFILER_ENABLED (method_enter) &&
					(frame->imethod->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_ENTER_CONTEXT))) {
				MonoProfilerCallContext *prof_ctx = g_new0 (MonoProfilerCallContext, 1);
				prof_ctx->interp_frame = frame;
				prof_ctx->method = frame->imethod->method;
				if (flag & TRACING_FLAG)
					mono_trace_enter_method (frame->imethod->method, frame->imethod->jinfo, prof_ctx);
				if (flag & PROFILING_FLAG)
					MONO_PROFILER_RAISE (method_enter, (frame->imethod->method, prof_ctx));
				g_free (prof_ctx);
			} else if ((flag & PROFILING_FLAG) && MONO_PROFILER_ENABLED (method_enter)) {
				MONO_PROFILER_RAISE (method_enter, (frame->imethod->method, NULL));
			}
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_PROF_EXIT)
		MINT_IN_CASE(MINT_PROF_EXIT_VOID) {
			guint16 flag = ip [1];
			// Set retval
			int const i32 = READ32 (ip + 2);
			if (i32 == -1) {
			} else if (i32) {
				sp--;
				if (frame->parent) {
					gpointer dest_vt = frame->parent->state.vt_sp;
					/* Push the valuetype in the parent frame */
					memcpy (dest_vt, sp->data.p, i32);
					frame->parent->state.sp [0].data.p = dest_vt;
					frame->parent->state.sp++;
					frame->parent->state.vt_sp += ALIGN_TO (i32, MINT_VT_ALIGNMENT);
				} else {
					memcpy (frame->retval->data.p, sp->data.p, i32);
				}
			} else {
				sp--;
				if (frame->parent) {
					frame->parent->state.sp [0] = *sp;
					frame->parent->state.sp++;
				} else {
					*frame->retval = *sp;
				}
			}

			if ((flag & TRACING_FLAG) || ((flag & PROFILING_FLAG) && MONO_PROFILER_ENABLED (method_leave) &&
					(frame->imethod->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE_CONTEXT))) {
				MonoProfilerCallContext *prof_ctx = g_new0 (MonoProfilerCallContext, 1);
				prof_ctx->interp_frame = frame;
				prof_ctx->method = frame->imethod->method;
				if (i32 != -1) {
					if (i32) {
						if (frame->parent)
							prof_ctx->return_value = frame->parent->state.sp [-1].data.p;
						else
							prof_ctx->return_value = frame->retval->data.p;
					} else {
						if (frame->parent)
							prof_ctx->return_value = frame->parent->state.sp - 1;
						else
							prof_ctx->return_value = frame->retval;
					}
				}
				if (flag & TRACING_FLAG)
					mono_trace_leave_method (frame->imethod->method, frame->imethod->jinfo, prof_ctx);
				if (flag & PROFILING_FLAG)
					MONO_PROFILER_RAISE (method_leave, (frame->imethod->method, prof_ctx));
				g_free (prof_ctx);
			} else if ((flag & PROFILING_FLAG) && MONO_PROFILER_ENABLED (method_enter)) {
				MONO_PROFILER_RAISE (method_leave, (frame->imethod->method, NULL));
			}

			ip += 4;
			frame_data_allocator_pop (&context->data_stack, frame);
			goto exit_frame;
		}
		MINT_IN_CASE(MINT_PROF_COVERAGE_STORE) {
			++ip;
			guint32 *p = (guint32*)GINT_TO_POINTER (READ64 (ip));
			*p = 1;
			ip += 4;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LDARGA_VT)
			sp->data.p = frame->stack [ip [1]].data.p;
			ip += 2;
			++sp;
			MINT_IN_BREAK;

#define LDLOC(datamem, argtype) \
	sp->data.datamem = * (argtype *)(locals + ip [1]); \
	ip += 2; \
	++sp; 
	
		MINT_IN_CASE(MINT_LDLOC_I1) LDLOC(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_U1) LDLOC(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_I2) LDLOC(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_U2) LDLOC(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_I4) LDLOC(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_I8) LDLOC(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_R4) LDLOC(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_R8) LDLOC(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_O) LDLOC(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDLOC_VT) {
			sp->data.p = vt_sp;
			int const i32 = READ32 (ip + 2);
			memcpy(sp->data.p, locals + ip [1], i32);
			vt_sp += ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			ip += 4;
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDLOCA_S)
			sp->data.p = locals + ip [1];
			ip += 2;
			++sp;
			MINT_IN_BREAK;

#define STLOC(datamem, argtype) \
	--sp; \
	* (argtype *)(locals + ip [1]) = sp->data.datamem; \
	ip += 2;
	
		MINT_IN_CASE(MINT_STLOC_I1) STLOC(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_U1) STLOC(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_I2) STLOC(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_U2) STLOC(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_I4) STLOC(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_I8) STLOC(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_R4) STLOC(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_R8) STLOC(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_O) STLOC(p, gpointer); MINT_IN_BREAK;

#define STLOC_NP(datamem, argtype) \
	* (argtype *)(locals + ip [1]) = sp [-1].data.datamem; \
	ip += 2;

		MINT_IN_CASE(MINT_STLOC_NP_I4) STLOC_NP(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_NP_I8) STLOC_NP(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_NP_R4) STLOC_NP(f_r4, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_NP_R8) STLOC_NP(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_NP_O) STLOC_NP(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STLOC_VT) {
			int const i32 = READ32 (ip + 2);
			--sp;
			memcpy(locals + ip [1], sp->data.p, i32);
			vt_sp -= ALIGN_TO (i32, MINT_VT_ALIGNMENT);
			ip += 4;
			MINT_IN_BREAK;
		}

#define MOVLOC(argtype) \
	* (argtype *)(locals + ip [2]) = * (argtype *)(locals + ip [1]); \
	ip += 3;

		MINT_IN_CASE(MINT_MOVLOC_1) MOVLOC(guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MOVLOC_2) MOVLOC(guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MOVLOC_4) MOVLOC(guint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MOVLOC_8) MOVLOC(guint64); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_MOVLOC_VT) {
			int const i32 = READ32(ip + 3);
			memcpy (locals + ip [2], locals + ip [1], i32);
			ip += 5;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LOCALLOC) {
			stackval *sp_start = (stackval*)(locals + frame->imethod->total_locals_size + frame->imethod->vt_stack_size);
			if (sp != sp_start + 1) /*FIX?*/
				THROW_EX (mono_get_exception_execution_engine (NULL), ip);

			int len = sp [-1].data.i;
			// FIXME we need a separate allocator for localloc sections
			sp [-1].data.p = frame_data_allocator_alloc (&context->data_stack, frame, ALIGN_TO (len, MINT_VT_ALIGNMENT));

			if (frame->imethod->init_locals)
				memset (sp [-1].data.p, 0, len);
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ENDFILTER)
			/* top of stack is result of filter */
			frame->retval->data.i = sp [-1].data.i;
			goto exit_clause;
		MINT_IN_CASE(MINT_INITOBJ)
			--sp;
			memset (sp->data.vt, 0, READ32(ip + 1));
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CPBLK)
			sp -= 3;
			if (!sp [0].data.p || !sp [1].data.p)
				THROW_EX (mono_get_exception_null_reference(), ip - 1);
			++ip;
			/* FIXME: value and size may be int64... */
			memcpy (sp [0].data.p, sp [1].data.p, sp [2].data.i);
			MINT_IN_BREAK;
#if 0
		MINT_IN_CASE(MINT_CONSTRAINED_) {
			guint32 token;
			/* FIXME: implement */
			++ip;
			token = READ32 (ip);
			ip += 2;
			MINT_IN_BREAK;
		}
#endif
		MINT_IN_CASE(MINT_INITBLK)
			sp -= 3;
			NULL_CHECK (sp [0].data.p);
			++ip;
			/* FIXME: value and size may be int64... */
			memset (sp [0].data.p, sp [1].data.i, sp [2].data.i);
			MINT_IN_BREAK;
#if 0
		MINT_IN_CASE(MINT_NO_)
			/* FIXME: implement */
			ip += 2;
			MINT_IN_BREAK;
#endif
	   MINT_IN_CASE(MINT_RETHROW) {
			int exvar_offset = ip [1];
			THROW_EX_GENERAL (*(MonoException**)(frame_locals (frame) + exvar_offset), ip, TRUE);
			MINT_IN_BREAK;
	   }
	   MINT_IN_CASE(MINT_MONO_RETHROW) {
			/* 
			 * need to clarify what this should actually do:
			 *
			 * Takes an exception from the stack and rethrows it.
			 * This is useful for wrappers that don't want to have to
			 * use CEE_THROW and lose the exception stacktrace. 
			 */

			--sp;
			if (!sp->data.p)
				sp->data.p = mono_get_exception_null_reference ();

			THROW_EX_GENERAL ((MonoException *)sp->data.p, ip, TRUE);
			MINT_IN_BREAK;
	   }
	   MINT_IN_CASE(MINT_LD_DELEGATE_METHOD_PTR) {
		   MonoDelegate *del;

		   --sp;
		   del = (MonoDelegate*)sp->data.p;
		   if (!del->interp_method) {
			   /* Not created from interpreted code */
			   error_init_reuse (error);
			   g_assert (del->method);
			   del->interp_method = mono_interp_get_imethod (del->object.vtable->domain, del->method, error);
			   mono_error_assert_ok (error);
		   }
		   g_assert (del->interp_method);
		   sp->data.p = del->interp_method;
		   ++sp;
		   ip += 1;
		   MINT_IN_BREAK;
	   }
		MINT_IN_CASE(MINT_LD_DELEGATE_INVOKE_IMPL) {
			MonoDelegate *del;
			int n = ip [1];
			del = (MonoDelegate*)sp [-n].data.p;
			if (!del->interp_invoke_impl) {
				/*
				 * First time we are called. Set up the invoke wrapper. We might be able to do this
				 * in ctor but we would need to handle AllocDelegateLike_internal separately
				 */
				error_init_reuse (error);
				MonoMethod *invoke = mono_get_delegate_invoke_internal (del->object.vtable->klass);
				del->interp_invoke_impl = mono_interp_get_imethod (del->object.vtable->domain, mono_marshal_get_delegate_invoke (invoke, del), error);
				mono_error_assert_ok (error);
			}
			sp ++;
			sp [-1].data.p = del->interp_invoke_impl;
			ip += 2;
			MINT_IN_BREAK;
		}

#define MATH_UNOP(mathfunc) \
	sp [-1].data.f = mathfunc (sp [-1].data.f); \
	++ip;

#define MATH_BINOP(mathfunc) \
	sp--; \
	sp [-1].data.f = mathfunc (sp [-1].data.f, sp [0].data.f); \
	++ip;

		MINT_IN_CASE(MINT_ABS) MATH_UNOP(fabs); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ASIN) MATH_UNOP(asin); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ASINH) MATH_UNOP(asinh); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ACOS) MATH_UNOP(acos); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ACOSH) MATH_UNOP(acosh); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ATAN) MATH_UNOP(atan); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ATANH) MATH_UNOP(atanh); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CEILING) MATH_UNOP(ceil); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_COS) MATH_UNOP(cos); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CBRT) MATH_UNOP(cbrt); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_COSH) MATH_UNOP(cosh); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_EXP) MATH_UNOP(exp); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_FLOOR) MATH_UNOP(floor); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LOG) MATH_UNOP(log); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LOG2) MATH_UNOP(log2); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LOG10) MATH_UNOP(log10); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SIN) MATH_UNOP(sin); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SQRT) MATH_UNOP(sqrt); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SINH) MATH_UNOP(sinh); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_TAN) MATH_UNOP(tan); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_TANH) MATH_UNOP(tanh); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_ATAN2) MATH_BINOP(atan2); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_POW) MATH_BINOP(pow); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_FMA)
			sp -= 2;
			sp [-1].data.f = fma (sp [-1].data.f, sp [0].data.f, sp [1].data.f);
			ip++;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SCALEB)
			sp--;
			sp [-1].data.f = scalbn (sp [-1].data.f, sp [0].data.i);
			ip++;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ILOGB) {
			int result;
			double x = sp [-1].data.f;
			if (FP_ILOGB0 != INT_MIN && x == 0.0)
				result = INT_MIN;
			else if (FP_ILOGBNAN != INT_MAX && isnan(x))
				result = INT_MAX;
			else
				result = ilogb (x);
			sp [-1].data.i = result;
			ip++;
			MINT_IN_BREAK;
		}

#define MATH_UNOPF(mathfunc) \
	sp [-1].data.f_r4 = mathfunc (sp [-1].data.f_r4); \
	++ip;

#define MATH_BINOPF(mathfunc) \
	sp--; \
	sp [-1].data.f_r4 = mathfunc (sp [-1].data.f_r4, sp [0].data.f_r4); \
	++ip;
		MINT_IN_CASE(MINT_ABSF) MATH_UNOPF(fabsf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ASINF) MATH_UNOPF(asinf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ASINHF) MATH_UNOPF(asinhf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ACOSF) MATH_UNOPF(acosf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ACOSHF) MATH_UNOPF(acoshf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ATANF) MATH_UNOPF(atanf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ATANHF) MATH_UNOPF(atanhf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CEILINGF) MATH_UNOPF(ceilf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_COSF) MATH_UNOPF(cosf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CBRTF) MATH_UNOPF(cbrtf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_COSHF) MATH_UNOPF(coshf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_EXPF) MATH_UNOPF(expf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_FLOORF) MATH_UNOPF(floorf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LOGF) MATH_UNOPF(logf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LOG2F) MATH_UNOPF(log2f); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LOG10F) MATH_UNOPF(log10f); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SINF) MATH_UNOPF(sinf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SQRTF) MATH_UNOPF(sqrtf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SINHF) MATH_UNOPF(sinhf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_TANF) MATH_UNOPF(tanf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_TANHF) MATH_UNOPF(tanhf); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_ATAN2F) MATH_BINOPF(atan2f); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_POWF) MATH_BINOPF(powf); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_FMAF)
			sp -= 2;
			sp [-1].data.f_r4 = fmaf (sp [-1].data.f_r4, sp [0].data.f_r4, sp [1].data.f_r4);
			ip++;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SCALEBF)
			sp--;
			sp [-1].data.f_r4 = scalbnf (sp [-1].data.f_r4, sp [0].data.i);
			ip++;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ILOGBF) {
			int result;
			float x = sp [-1].data.f_r4;
			if (FP_ILOGB0 != INT_MIN && x == 0.0)
				result = INT_MIN;
			else if (FP_ILOGBNAN != INT_MAX && isnan(x))
				result = INT_MAX;
			else
				result = ilogbf (x);
			sp [-1].data.i = result;
			ip++;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_INTRINS_ENUM_HASFLAG) {
			MonoClass *klass = (MonoClass*)frame->imethod->data_items[ip [1]];
			mono_interp_enum_hasflag (sp, klass);
			sp--;
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_GET_HASHCODE) {
			sp [-1].data.i = mono_object_hash_internal (sp [-1].data.o);
			ip++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_GET_TYPE) {
			NULL_CHECK (sp [-1].data.p);
			sp [-1].data.o = (MonoObject*) sp [-1].data.o->vtable->type;
			ip++;
			MINT_IN_BREAK;
		}

#if !USE_COMPUTED_GOTO
		default:
			interp_error_xsx ("Unimplemented opcode: %04x %s at 0x%x\n", *ip, mono_interp_opname (*ip), ip - frame->imethod->code);
#endif
		}
	}

	g_assert_not_reached ();

resume:
	g_assert (context->has_resume_state);
	g_assert (frame->imethod);

	if (frame == context->handler_frame) {
		/*
		 * When running finally blocks, we can have the same frame twice on the stack. If we have
		 * clause_args information, we need to check whether resuming should happen inside this
		 * finally block, or in some other part of the method, in which case we need to exit.
		 */
		if (clause_args && frame == clause_args->exec_frame && context->handler_ip >= clause_args->end_at_ip) {
			goto exit_clause;
		} else {
			/* Set the current execution state to the resume state in context */
			ip = context->handler_ip;
			/* spec says stack should be empty at endfinally so it should be at the start too */
			locals = (guchar*)frame->stack;
			vt_sp = locals + frame->imethod->total_locals_size;
			sp = (stackval*)(vt_sp + frame->imethod->vt_stack_size);
			g_assert (context->exc_gchandle);
			sp->data.p = mono_gchandle_get_target_internal (context->exc_gchandle);
			++sp;

			finally_ips = clear_resume_state (context, finally_ips);
			// goto main_loop instead of MINT_IN_DISPATCH helps the compiler and therefore conserves stack.
			// This is a slow/rare path and conserving stack is preferred over its performance otherwise.
			goto main_loop;
		}
	} else if (clause_args && frame == clause_args->exec_frame) {
		/*
		 * This frame doesn't handle the resume state and it is the first frame invoked from EH.
		 * We can't just return to parent. We must first exit the EH mechanism and start resuming
		 * again from the original frame.
		 */
		goto exit_clause;
	}
	// Because we are resuming in another frame, bypassing a normal ret opcode,
	// we need to make sure to reset the localloc stack
	frame_data_allocator_pop (&context->data_stack, frame);
	// fall through
exit_frame:
	g_assert_checked (frame->imethod);

	if (frame->parent && frame->parent->state.ip) {
		/* Return to the main loop after a non-recursive interpreter call */
		//printf ("R: %s -> %s %p\n", mono_method_get_full_name (frame->imethod->method), mono_method_get_full_name (frame->parent->imethod->method), frame->parent->state.ip);
		g_assert_checked (frame->stack);
		frame = frame->parent;
		/*
		 * FIXME We should be able to avoid dereferencing imethod here, if we will have
		 * a param_area and all calls would inherit the same sp, or if we are full coop.
		 */
		context->stack_pointer = (guchar*)frame->stack + frame->imethod->alloca_size;
		LOAD_INTERP_STATE (frame);

		CHECK_RESUME_STATE (context);

		goto main_loop;
	}
exit_clause:
	if (!clause_args)
		context->stack_pointer = (guchar*)frame->stack;

	DEBUG_LEAVE ();
}

static void
interp_parse_options (const char *options)
{
	char **args, **ptr;

	if (!options)
		return;

	args = g_strsplit (options, ",", -1);
	for (ptr = args; ptr && *ptr; ptr ++) {
		char *arg = *ptr;

		if (strncmp (arg, "jit=", 4) == 0)
			mono_interp_jit_classes = g_slist_prepend (mono_interp_jit_classes, arg + 4);
		else if (strncmp (arg, "interp-only=", strlen ("interp-only=")) == 0)
			mono_interp_only_classes = g_slist_prepend (mono_interp_only_classes, arg + strlen ("interp-only="));
		else if (strncmp (arg, "-inline", 7) == 0)
			mono_interp_opt &= ~INTERP_OPT_INLINE;
		else if (strncmp (arg, "-cprop", 6) == 0)
			mono_interp_opt &= ~INTERP_OPT_CPROP;
		else if (strncmp (arg, "-super", 6) == 0)
			mono_interp_opt &= ~INTERP_OPT_SUPER_INSTRUCTIONS;
		else if (strncmp (arg, "-all", 4) == 0)
			mono_interp_opt = INTERP_OPT_NONE;
	}
}

/*
 * interp_set_resume_state:
 *
 *   Set the state the interpeter will continue to execute from after execution returns to the interpreter.
 */
static void
interp_set_resume_state (MonoJitTlsData *jit_tls, MonoObject *ex, MonoJitExceptionInfo *ei, MonoInterpFrameHandle interp_frame, gpointer handler_ip)
{
	ThreadContext *context;

	g_assert (jit_tls);
	context = (ThreadContext*)jit_tls->interp_context;
	g_assert (context);

	context->has_resume_state = TRUE;
	context->handler_frame = (InterpFrame*)interp_frame;
	context->handler_ei = ei;
	if (context->exc_gchandle)
		mono_gchandle_free_internal (context->exc_gchandle);
	context->exc_gchandle = mono_gchandle_new_internal ((MonoObject*)ex, FALSE);
	/* Ditto */
	if (ei)
		*(MonoObject**)(frame_locals (context->handler_frame) + ei->exvar_offset) = ex;
	context->handler_ip = (const guint16*)handler_ip;
}

static void
interp_get_resume_state (const MonoJitTlsData *jit_tls, gboolean *has_resume_state, MonoInterpFrameHandle *interp_frame, gpointer *handler_ip)
{
	g_assert (jit_tls);
	ThreadContext *context = (ThreadContext*)jit_tls->interp_context;

	*has_resume_state = context ? context->has_resume_state : FALSE;
	if (!*has_resume_state)
		return;

	*interp_frame = context->handler_frame;
	*handler_ip = (gpointer)context->handler_ip;
}

/*
 * interp_run_finally:
 *
 *   Run the finally clause identified by CLAUSE_INDEX in the intepreter frame given by
 * frame->interp_frame.
 * Return TRUE if the finally clause threw an exception.
 */
static gboolean
interp_run_finally (StackFrameInfo *frame, int clause_index, gpointer handler_ip, gpointer handler_ip_end)
{
	InterpFrame *iframe = (InterpFrame*)frame->interp_frame;
	ThreadContext *context = get_context ();
	FrameClauseArgs clause_args;
	const guint16 *state_ip;

	memset (&clause_args, 0, sizeof (FrameClauseArgs));
	clause_args.start_with_ip = (const guint16*)handler_ip;
	clause_args.end_at_ip = (const guint16*)handler_ip_end;
	clause_args.exit_clause = clause_index;
	clause_args.exec_frame = iframe;

	state_ip = iframe->state.ip;
	iframe->state.ip = NULL;

	InterpFrame* const next_free = iframe->next_free;
	iframe->next_free = NULL;

	interp_exec_method (iframe, context, &clause_args);

	iframe->next_free = next_free;
	iframe->state.ip = state_ip;
	if (context->has_resume_state) {
		return TRUE;
	} else {
		return FALSE;
	}
}

/*
 * interp_run_filter:
 *
 *   Run the filter clause identified by CLAUSE_INDEX in the intepreter frame given by
 * frame->interp_frame.
 */
// Do not inline in case order of frame addresses matters.
static MONO_NEVER_INLINE gboolean
interp_run_filter (StackFrameInfo *frame, MonoException *ex, int clause_index, gpointer handler_ip, gpointer handler_ip_end)
{
	InterpFrame *iframe = (InterpFrame*)frame->interp_frame;
	ThreadContext *context = get_context ();
	stackval retval;
	FrameClauseArgs clause_args;

	/*
	 * Have to run the clause in a new frame which is a copy of IFRAME, since
	 * during debugging, there are two copies of the frame on the stack.
	 */
	InterpFrame child_frame = {0};
	child_frame.parent = iframe;
	child_frame.imethod = iframe->imethod;
	child_frame.stack = (stackval*)context->stack_pointer;
	child_frame.retval = &retval;

	/* Copy the stack frame of the original method */
	memcpy (child_frame.stack, iframe->stack, iframe->imethod->total_locals_size);
	context->stack_pointer += iframe->imethod->alloca_size;

	memset (&clause_args, 0, sizeof (FrameClauseArgs));
	clause_args.start_with_ip = (const guint16*)handler_ip;
	clause_args.end_at_ip = (const guint16*)handler_ip_end;
	clause_args.filter_exception = ex;
	clause_args.exec_frame = &child_frame;

	interp_exec_method (&child_frame, context, &clause_args);

	/* Copy back the updated frame */
	memcpy (iframe->stack, child_frame.stack, iframe->imethod->total_locals_size);

	context->stack_pointer = (guchar*)child_frame.stack;

	/* ENDFILTER stores the result into child_frame->retval */
	return retval.data.i ? TRUE : FALSE;
}

typedef struct {
	InterpFrame *current;
} StackIter;

static gpointer
interp_frame_get_ip (MonoInterpFrameHandle frame)
{
	InterpFrame *iframe = (InterpFrame*)frame;

	g_assert (iframe->imethod);
	/*
	 * For calls, state.ip points to the instruction following the call, so we need to subtract
	 * in order to get inside the call instruction range. Other instructions that set the IP for
	 * the rest of the runtime to see, like throws and sdb breakpoints, will need to account for
	 * this subtraction that we are doing here.
	 */
	return (gpointer)(iframe->state.ip - 1);
}

/*
 * interp_frame_iter_init:
 *
 *   Initialize an iterator for iterating through interpreted frames.
 */
static void
interp_frame_iter_init (MonoInterpStackIter *iter, gpointer interp_exit_data)
{
	StackIter *stack_iter = (StackIter*)iter;

	stack_iter->current = (InterpFrame*)interp_exit_data;
}

/*
 * interp_frame_iter_next:
 *
 *   Fill out FRAME with date for the next interpreter frame.
 */
static gboolean
interp_frame_iter_next (MonoInterpStackIter *iter, StackFrameInfo *frame)
{
	StackIter *stack_iter = (StackIter*)iter;
	InterpFrame *iframe = stack_iter->current;

	memset (frame, 0, sizeof (StackFrameInfo));
	/* pinvoke frames doesn't have imethod set */
	while (iframe && !(iframe->imethod && iframe->imethod->code && iframe->imethod->jinfo))
		iframe = iframe->parent;
	if (!iframe)
		return FALSE;

	MonoMethod *method = iframe->imethod->method;
	frame->domain = iframe->imethod->domain;
	frame->interp_frame = iframe;
	frame->method = method;
	frame->actual_method = method;
	if (method && ((method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) || (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)))) {
		frame->native_offset = -1;
		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;
	} else {
		frame->type = FRAME_TYPE_INTERP;
		/* This is the offset in the interpreter IR. */
		frame->native_offset = (guint8*)interp_frame_get_ip (iframe) - (guint8*)iframe->imethod->code;
		if (!method->wrapper_type || method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
			frame->managed = TRUE;
	}
	frame->ji = iframe->imethod->jinfo;
	frame->frame_addr = iframe;

	stack_iter->current = iframe->parent;

	return TRUE;
}

static MonoJitInfo*
interp_find_jit_info (MonoDomain *domain, MonoMethod *method)
{
	InterpMethod* imethod;

	imethod = lookup_imethod (domain, method);
	if (imethod)
		return imethod->jinfo;
	else
		return NULL;
}

static void
interp_set_breakpoint (MonoJitInfo *jinfo, gpointer ip)
{
	guint16 *code = (guint16*)ip;
	g_assert (*code == MINT_SDB_SEQ_POINT);
	*code = MINT_SDB_BREAKPOINT;
}

static void
interp_clear_breakpoint (MonoJitInfo *jinfo, gpointer ip)
{
	guint16 *code = (guint16*)ip;
	g_assert (*code == MINT_SDB_BREAKPOINT);
	*code = MINT_SDB_SEQ_POINT;
}

static MonoJitInfo*
interp_frame_get_jit_info (MonoInterpFrameHandle frame)
{
	InterpFrame *iframe = (InterpFrame*)frame;

	g_assert (iframe->imethod);
	return iframe->imethod->jinfo;
}

static gpointer
interp_frame_get_arg (MonoInterpFrameHandle frame, int pos)
{
	InterpFrame *iframe = (InterpFrame*)frame;
	MonoMethodSignature *sig;

	g_assert (iframe->imethod);

	sig = mono_method_signature_internal (iframe->imethod->method);
	return stackval_to_data_addr (sig->params [pos], &iframe->stack [pos + !!iframe->imethod->hasthis]);
}

static gpointer
interp_frame_get_local (MonoInterpFrameHandle frame, int pos)
{
	InterpFrame *iframe = (InterpFrame*)frame;

	g_assert (iframe->imethod);

	return frame_locals (iframe) + iframe->imethod->local_offsets [pos];
}

static gpointer
interp_frame_get_this (MonoInterpFrameHandle frame)
{
	InterpFrame *iframe = (InterpFrame*)frame;

	g_assert (iframe->imethod);
	g_assert (iframe->imethod->hasthis);
	return &iframe->stack [0].data.p;
}

static MonoInterpFrameHandle
interp_frame_get_parent (MonoInterpFrameHandle frame)
{
	InterpFrame *iframe = (InterpFrame*)frame;

	return iframe->parent;
}

static gpointer
interp_frame_get_res (MonoInterpFrameHandle frame)
{
	InterpFrame *iframe = (InterpFrame*)frame;
	MonoMethodSignature *sig;

	g_assert (iframe->imethod);
	sig = mono_method_signature_internal (iframe->imethod->method);
	if (sig->ret->type == MONO_TYPE_VOID)
		return NULL;
	else if (iframe->parent)
		return stackval_to_data_addr (sig->ret, iframe->parent->state.sp - 1);
	else
		return stackval_to_data_addr (sig->ret, iframe->retval);
}

static void
interp_start_single_stepping (void)
{
	ss_enabled = TRUE;
}

static void
interp_stop_single_stepping (void)
{
	ss_enabled = FALSE;
}

/*
 * interp_mark_stack:
 *
 *   Mark the interpreter stack frames for a thread.
 *
 */
static void
interp_mark_stack (gpointer thread_data, GcScanFunc func, gpointer gc_data, gboolean precise)
{
	MonoThreadInfo *info = (MonoThreadInfo*)thread_data;

	if (!mono_use_interpreter)
		return;
	if (precise)
		return;

	/*
	 * We explicitly mark the frames instead of registering the stack fragments as GC roots, so
	 * we have to process less data and avoid false pinning from data which is above 'pos'.
	 *
	 * The stack frame handling code uses compiler write barriers only, but the calling code
	 * in sgen-mono.c already did a mono_memory_barrier_process_wide () so we can
	 * process these data structures normally.
	 */
	MonoJitTlsData *jit_tls = (MonoJitTlsData *)info->tls [TLS_KEY_JIT_TLS];
	if (!jit_tls)
		return;

	ThreadContext *context = (ThreadContext*)jit_tls->interp_context;
	if (!context || !context->stack_start)
		return;

	// FIXME: Scan the whole area with 1 call
	for (gpointer *p = (gpointer*)context->stack_start; p < (gpointer*)context->stack_pointer; p++)
		func (p, gc_data);

	FrameDataFragment *frag;
	for (frag = context->data_stack.first; frag; frag = frag->next) {
		// FIXME: Scan the whole area with 1 call
		for (gpointer *p = (gpointer*)&frag->data; p < (gpointer*)frag->pos; ++p)
			func (p, gc_data);
		if (frag == context->data_stack.current)
			break;
	}
}

#if COUNT_OPS

static int
opcode_count_comparer (const void * pa, const void * pb)
{
	long counta = opcode_counts [*(int*)pa];
	long countb = opcode_counts [*(int*)pb];

	if (counta < countb)
		return 1;
	else if (counta > countb)
		return -1;
	else
		return 0;
}

static void
interp_print_op_count (void)
{
	int ordered_ops [MINT_LASTOP];
	int i;
	long total_ops = 0;

	for (i = 0; i < MINT_LASTOP; i++) {
		ordered_ops [i] = i;
		total_ops += opcode_counts [i];
	}
	qsort (ordered_ops, MINT_LASTOP, sizeof (int), opcode_count_comparer);

	for (i = 0; i < MINT_LASTOP; i++) {
		long count = opcode_counts [ordered_ops [i]];
		g_print ("%s : %ld (%.2lf%%)\n", mono_interp_opname (ordered_ops [i]), count, (double)count / total_ops * 100);
	}
}
#endif

#if PROFILE_INTERP

static InterpMethod **imethods;
static int num_methods;
const int opcount_threshold = 100000;

static void
interp_add_imethod (gpointer method)
{
	InterpMethod *imethod = (InterpMethod*) method;
	if (imethod->opcounts > opcount_threshold)
		imethods [num_methods++] = imethod;
}

static int
imethod_opcount_comparer (gconstpointer m1, gconstpointer m2)
{
	return (*(InterpMethod**)m2)->opcounts - (*(InterpMethod**)m1)->opcounts;
}

static void
interp_print_method_counts (void)
{
	MonoDomain *domain = mono_get_root_domain ();
	MonoJitDomainInfo *info = domain_jit_info (domain);

	mono_domain_jit_code_hash_lock (domain);
	imethods = (InterpMethod**) malloc (info->interp_code_hash.num_entries * sizeof (InterpMethod*));
	mono_internal_hash_table_apply (&info->interp_code_hash, interp_add_imethod);
	mono_domain_jit_code_hash_unlock (domain);

	qsort (imethods, num_methods, sizeof (InterpMethod*), imethod_opcount_comparer);

	printf ("Total executed opcodes %ld\n", total_executed_opcodes);
	long cumulative_executed_opcodes = 0;
	for (int i = 0; i < num_methods; i++) {
		cumulative_executed_opcodes += imethods [i]->opcounts;
		printf ("%d%% Opcounts %ld, calls %ld, Method %s, imethod ptr %p\n", (int)(cumulative_executed_opcodes * 100 / total_executed_opcodes), imethods [i]->opcounts, imethods [i]->calls, mono_method_full_name (imethods [i]->method, TRUE), imethods [i]);
	}
}
#endif

static void
interp_set_optimizations (guint32 opts)
{
	mono_interp_opt = opts;
}

static void
invalidate_transform (gpointer imethod_)
{
	InterpMethod *imethod = (InterpMethod *) imethod_;
	imethod->transformed = FALSE;
}

static void
interp_invalidate_transformed (MonoDomain *domain)
{
	MonoJitDomainInfo *info = domain_jit_info (domain);
	mono_domain_jit_code_hash_lock (domain);
	mono_internal_hash_table_apply (&info->interp_code_hash, invalidate_transform);
	mono_domain_jit_code_hash_unlock (domain);
}

static void
interp_cleanup (void)
{
#if COUNT_OPS
	interp_print_op_count ();
#endif
#if PROFILE_INTERP
	interp_print_method_counts ();
#endif
}

static void
register_interp_stats (void)
{
	mono_counters_init ();
	mono_counters_register ("Total transform time", MONO_COUNTER_INTERP | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_interp_stats.transform_time);
	mono_counters_register ("Methods transformed", MONO_COUNTER_INTERP | MONO_COUNTER_LONG, &mono_interp_stats.methods_transformed);
	mono_counters_register ("Total cprop time", MONO_COUNTER_INTERP | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_interp_stats.cprop_time);
	mono_counters_register ("Total super instructions time", MONO_COUNTER_INTERP | MONO_COUNTER_LONG | MONO_COUNTER_TIME, &mono_interp_stats.super_instructions_time);
	mono_counters_register ("STLOC_NP count", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.stloc_nps);
	mono_counters_register ("MOVLOC count", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.movlocs);
	mono_counters_register ("Copy propagations", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.copy_propagations);
	mono_counters_register ("Added pop count", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.added_pop_count);
	mono_counters_register ("Constant folds", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.constant_folds);
	mono_counters_register ("Ldlocas removed", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.ldlocas_removed);
	mono_counters_register ("Super instructions", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.super_instructions);
	mono_counters_register ("Killed instructions", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.killed_instructions);
	mono_counters_register ("Emitted instructions", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.emitted_instructions);
	mono_counters_register ("Methods inlined", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.inlined_methods);
	mono_counters_register ("Inline failures", MONO_COUNTER_INTERP | MONO_COUNTER_INT, &mono_interp_stats.inline_failures);
}

#undef MONO_EE_CALLBACK
#define MONO_EE_CALLBACK(ret, name, sig) interp_ ## name,

static const MonoEECallbacks mono_interp_callbacks = {
	MONO_EE_CALLBACKS
};

void
mono_ee_interp_init (const char *opts)
{
	g_assert (mono_ee_api_version () == MONO_EE_API_VERSION);
	g_assert (!interp_init_done);
	interp_init_done = TRUE;

	mono_native_tls_alloc (&thread_context_id, NULL);
	set_context (NULL);

	interp_parse_options (opts);
	/* Don't do any optimizations if running under debugger */
	if (mini_get_debug_options ()->mdb_optimizations)
		mono_interp_opt = 0;
	mono_interp_transform_init ();

	mini_install_interp_callbacks (&mono_interp_callbacks);

	register_interp_stats ();
}
