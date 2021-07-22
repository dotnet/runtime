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
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-tls-inline.h>
#include <mono/utils/mono-threads.h>
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
#include <mono/mini/ee.h>
#include <mono/mini/trace.h>

#include <mono/metadata/components.h>

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
	stack->infos = (FrameDataInfo*)g_malloc (stack->infos_capacity * sizeof (FrameDataInfo));
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
			stack->infos = (FrameDataInfo*)g_realloc (stack->infos, stack->infos_capacity * sizeof (FrameDataInfo));
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
reinit_frame (InterpFrame *frame, InterpFrame *parent, InterpMethod *imethod, gpointer retval, gpointer stack)
{
	frame->parent = parent;
	frame->imethod = imethod;
	frame->stack = (stackval*)stack;
	frame->retval = (stackval*)retval;
	frame->state.ip = NULL;
}

#define STACK_ADD_BYTES(sp,bytes) ((stackval*)((char*)(sp) + ALIGN_TO(bytes, MINT_STACK_SLOT_SIZE)))
#define STACK_SUB_BYTES(sp,bytes) ((stackval*)((char*)(sp) - ALIGN_TO(bytes, MINT_STACK_SLOT_SIZE)))

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

static void
clear_resume_state (ThreadContext *context)
{
	context->has_resume_state = 0;
	context->handler_frame = NULL;
	context->handler_ei = NULL;
	g_assert (context->exc_gchandle);
	mono_gchandle_free_internal (context->exc_gchandle);
	context->exc_gchandle = 0;
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

	ThreadContext *current_context = (ThreadContext *) mono_native_tls_get_value (thread_context_id);
	/* at thread exit, we can be called from the JIT TLS key destructor with current_context == NULL */
	if (current_context != NULL) {
		/* check that the context we're freeing is the current one before overwriting TLS */
		g_assert (context == current_context);
		set_context (NULL);
	}

	context->safepoint_frame = NULL;

	mono_vfree (context->stack_start, INTERP_STACK_SIZE, MONO_MEM_ACCOUNT_INTERP_STACK);
	/* Prevent interp_mark_stack from trying to scan the data_stack, before freeing it */
	context->stack_start = NULL;
	mono_compiler_barrier ();
	frame_data_allocator_free (&context->data_stack);
	g_free (context);
}

static void
context_set_safepoint_frame (ThreadContext *context, InterpFrame *frame)
{
	g_assert (!context->has_resume_state);
	context->safepoint_frame = frame;
}

static void
context_clear_safepoint_frame (ThreadContext *context)
{
	context->safepoint_frame = NULL;
}

void
mono_interp_error_cleanup (MonoError* error)
{
	mono_error_cleanup (error); /* FIXME: don't swallow the error */
	error_init_reuse (error); // one instruction, so this function is good inline candidate
}

static InterpMethod*
lookup_imethod (MonoMethod *method)
{
	InterpMethod *imethod;
	MonoJitMemoryManager *jit_mm = jit_mm_for_method (method);

	jit_mm_lock (jit_mm);
	imethod = (InterpMethod*)mono_internal_hash_table_lookup (&jit_mm->interp_code_hash, method);
	jit_mm_unlock (jit_mm);

	return imethod;
}

InterpMethod*
mono_interp_get_imethod (MonoMethod *method, MonoError *error)
{
	InterpMethod *imethod;
	MonoMethodSignature *sig;
	MonoJitMemoryManager *jit_mm = jit_mm_for_method (method);
	int i;

	error_init (error);

	jit_mm_lock (jit_mm);
	imethod = (InterpMethod*)mono_internal_hash_table_lookup (&jit_mm->interp_code_hash, method);
	jit_mm_unlock (jit_mm);
	if (imethod)
		return imethod;

	sig = mono_method_signature_internal (method);

	imethod = (InterpMethod*)m_method_alloc0 (method, sizeof (InterpMethod));
	imethod->method = method;
	imethod->param_count = sig->param_count;
	imethod->hasthis = sig->hasthis;
	imethod->vararg = sig->call_convention == MONO_CALL_VARARG;
	imethod->code_type = IMETHOD_CODE_UNKNOWN;
	if (imethod->method->string_ctor)
		imethod->rtype = m_class_get_byval_arg (mono_defaults.string_class);
	else
		imethod->rtype = mini_get_underlying_type (sig->ret);
	imethod->param_types = (MonoType**)m_method_alloc0 (method, sizeof (MonoType*) * sig->param_count);
	for (i = 0; i < sig->param_count; ++i)
		imethod->param_types [i] = mini_get_underlying_type (sig->params [i]);

	jit_mm_lock (jit_mm);
	if (!mono_internal_hash_table_lookup (&jit_mm->interp_code_hash, method))
		mono_internal_hash_table_insert (&jit_mm->interp_code_hash, method, imethod);
	jit_mm_unlock (jit_mm);

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
	InterpMethod *ret = NULL;

	if ((m->flags & METHOD_ATTRIBUTE_FINAL) || !(m->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
		if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) {
			ERROR_DECL (error);
			ret = mono_interp_get_imethod (mono_marshal_get_synchronized_wrapper (m), error);
			mono_interp_error_cleanup (error); /* FIXME: don't swallow the error */
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
	InterpMethod *virtual_imethod = mono_interp_get_imethod (virtual_method, error);
	mono_error_cleanup (error); /* FIXME: don't swallow the error */
	return virtual_imethod;
}

typedef struct {
	InterpMethod *imethod;
	InterpMethod *target_imethod;
} InterpVTableEntry;

/* memory manager lock must be held */
static GSList*
append_imethod (MonoMemoryManager *memory_manager, GSList *list, InterpMethod *imethod, InterpMethod *target_imethod)
{
	GSList *ret;
	InterpVTableEntry *entry;

	entry = (InterpVTableEntry*) mono_mem_manager_alloc0 (memory_manager, sizeof (InterpVTableEntry));
	entry->imethod = imethod;
	entry->target_imethod = target_imethod;
	ret = mono_mem_manager_alloc0 (memory_manager, sizeof (GSList));
	ret->data = entry;
	ret = g_slist_concat (list, ret);

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
		table = (gpointer*)m_class_alloc0 (vtable->klass, m_class_get_vtable_size (vtable->klass) * sizeof (gpointer));
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
	MonoMemoryManager *memory_manager = m_class_get_mem_manager (vtable->klass);

	table = get_method_table (vtable, offset);

	if (!table) {
		/* Lazily allocate method table */
		mono_mem_manager_lock (memory_manager);
		table = get_method_table (vtable, offset);
		if (!table)
			table = alloc_method_table (vtable, offset);
		mono_mem_manager_unlock (memory_manager);
	}

	if (!table [offset]) {
		InterpMethod *target_imethod = get_virtual_method (imethod, vtable);
		/* Lazily initialize the method table slot */
		mono_mem_manager_lock (memory_manager);
		if (!table [offset]) {
			if (imethod->method->is_inflated || offset < 0)
				table [offset] = append_imethod (memory_manager, NULL, imethod, target_imethod);
			else
				table [offset] = (gpointer) ((gsize)target_imethod | 0x1);
		}
		mono_mem_manager_unlock (memory_manager);
	}

	if ((gsize)table [offset] & 0x1) {
		/* Non generic virtual call. Only one method in slot */
		return (InterpMethod*) ((gsize)table [offset] & ~0x1);
	} else {
		/* Virtual generic or interface call. Multiple methods in slot */
		InterpMethod *target_imethod = get_target_imethod ((GSList*)table [offset], imethod);

		if (!target_imethod) {
			target_imethod = get_virtual_method (imethod, vtable);
			mono_mem_manager_lock (memory_manager);
			if (!get_target_imethod ((GSList*)table [offset], imethod))
				table [offset] = append_imethod (memory_manager, (GSList*)table [offset], imethod, target_imethod);
			mono_mem_manager_unlock (memory_manager);
		}
		return target_imethod;
	}
}

// Returns the size it uses on the interpreter stack
static int
stackval_from_data (MonoType *type, stackval *result, const void *data, gboolean pinvoke)
{
	type = mini_native_type_replace_type (type);
	if (type->byref) {
		result->data.p = *(gpointer*)data;
		return MINT_STACK_SLOT_SIZE;
	}
	switch (type->type) {
	case MONO_TYPE_VOID:
		return 0;
	case MONO_TYPE_I1:
		result->data.i = *(gint8*)data;
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		result->data.i = *(guint8*)data;
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_I2:
		result->data.i = *(gint16*)data;
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		result->data.i = *(guint16*)data;
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_I4:
		result->data.i = *(gint32*)data;
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_U:
	case MONO_TYPE_I:
		result->data.nati = *(mono_i*)data;
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		result->data.p = *(gpointer*)data;
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_U4:
		result->data.i = *(guint32*)data;
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_R4:
		/* memmove handles unaligned case */
		memmove (&result->data.f_r4, data, sizeof (float));
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		memmove (&result->data.l, data, sizeof (gint64));
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_R8:
		memmove (&result->data.f, data, sizeof (double));
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
		result->data.p = *(gpointer*)data;
		return MINT_STACK_SLOT_SIZE;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			return stackval_from_data (mono_class_enum_basetype_internal (type->data.klass), result, data, pinvoke);
		} else {
			int size;
			if (pinvoke)
				size = mono_class_native_size (type->data.klass, NULL);
			else
				size = mono_class_value_size (type->data.klass, NULL);
			memcpy (result, data, size);
			return ALIGN_TO (size, MINT_STACK_SLOT_SIZE);
		}
	case MONO_TYPE_GENERICINST: {
		if (mono_type_generic_inst_is_valuetype (type)) {
			MonoClass *klass = mono_class_from_mono_type_internal (type);
			int size;
			if (pinvoke)
				size = mono_class_native_size (klass, NULL);
			else
				size = mono_class_value_size (klass, NULL);
			memcpy (result, data, size);
			return ALIGN_TO (size, MINT_STACK_SLOT_SIZE);
		}
		return stackval_from_data (m_class_get_byval_arg (type->data.generic_class->container_class), result, data, pinvoke);
	}
	default:
		g_error ("got type 0x%02x", type->type);
	}
}

static int
stackval_to_data (MonoType *type, stackval *val, void *data, gboolean pinvoke)
{
	type = mini_native_type_replace_type (type);
	if (type->byref) {
		gpointer *p = (gpointer*)data;
		*p = val->data.p;
		return MINT_STACK_SLOT_SIZE;
	}
	/* printf ("TODAT0 %p\n", data); */
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1: {
		guint8 *p = (guint8*)data;
		*p = val->data.i;
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_BOOLEAN: {
		guint8 *p = (guint8*)data;
		*p = (val->data.i != 0);
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR: {
		guint16 *p = (guint16*)data;
		*p = val->data.i;
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_I: {
		mono_i *p = (mono_i*)data;
		/* In theory the value used by stloc should match the local var type
	 	   but in practice it sometimes doesn't (a int32 gets dup'd and stloc'd into
		   a native int - both by csc and mcs). Not sure what to do about sign extension
		   as it is outside the spec... doing the obvious */
		*p = (mono_i)val->data.nati;
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_U: {
		mono_u *p = (mono_u*)data;
		/* see above. */
		*p = (mono_u)val->data.nati;
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_I4:
	case MONO_TYPE_U4: {
		gint32 *p = (gint32*)data;
		*p = val->data.i;
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_I8:
	case MONO_TYPE_U8: {
		memmove (data, &val->data.l, sizeof (gint64));
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_R4: {
		/* memmove handles unaligned case */
		memmove (data, &val->data.f_r4, sizeof (float));
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_R8: {
		memmove (data, &val->data.f, sizeof (double));
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY: {
		gpointer *p = (gpointer *) data;
		mono_gc_wbarrier_generic_store_internal (p, val->data.o);
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR: {
		gpointer *p = (gpointer *) data;
		*p = val->data.p;
		return MINT_STACK_SLOT_SIZE;
	}
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			return stackval_to_data (mono_class_enum_basetype_internal (type->data.klass), val, data, pinvoke);
		} else {
			int size;
			if (pinvoke) {
				size = mono_class_native_size (type->data.klass, NULL);
				memcpy (data, val, size);
			} else {
				size = mono_class_value_size (type->data.klass, NULL);
				mono_value_copy_internal (data, val, type->data.klass);
			}
			return ALIGN_TO (size, MINT_STACK_SLOT_SIZE);
		}
	case MONO_TYPE_GENERICINST: {
		MonoClass *container_class = type->data.generic_class->container_class;

		if (m_class_is_valuetype (container_class) && !m_class_is_enumtype (container_class)) {
			MonoClass *klass = mono_class_from_mono_type_internal (type);
			int size;
			if (pinvoke) {
				size = mono_class_native_size (klass, NULL);
				memcpy (data, val, size);
			} else {
				size = mono_class_value_size (klass, NULL);
				mono_value_copy_internal (data, val, klass);
			}
			return ALIGN_TO (size, MINT_STACK_SLOT_SIZE);
		}
		return stackval_to_data (m_class_get_byval_arg (type->data.generic_class->container_class), val, data, pinvoke);
	}
	default:
		g_error ("got type %x", type->type);
	}
}

typedef struct {
	MonoException *ex;
	MonoContext *ctx;
} HandleExceptionCbData;

static void
handle_exception_cb (gpointer arg)
{
	HandleExceptionCbData *cb_data = (HandleExceptionCbData*)arg;

	mono_handle_exception (cb_data->ctx, (MonoObject*)cb_data->ex);
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
	interp_push_lmf (&ext, frame);

	mono_handle_exception (&ctx, (MonoObject*)ex);

	interp_pop_lmf (&ext);

	if (MONO_CONTEXT_GET_IP (&ctx) != 0) {
		/* We need to unwind into non-interpreter code */
		mono_restore_context (&ctx);
		g_assert_not_reached ();
	}

	g_assert (context->has_resume_state);
}

// We conservatively pin exception object here to avoid tweaking the
// numerous call sites of this macro, even though, in a few cases,
// this is not needed.
#define THROW_EX_GENERAL(exception,ex_ip, rethrow)		\
	do {							\
		MonoException *__ex = (exception);		\
		MONO_HANDLE_ASSIGN_RAW (tmp_handle, (MonoObject*)__ex); \
		interp_throw (context, __ex, (frame), (ex_ip), (rethrow)); \
		MONO_HANDLE_ASSIGN_RAW (tmp_handle, (MonoObject*)NULL); \
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

// Reduce duplicate code in interp_exec_method
static void
do_safepoint (InterpFrame *frame, ThreadContext *context)
{
	context_set_safepoint_frame (context, frame);
	/* Poll safepoint */
	mono_threads_safepoint ();
	context_clear_safepoint_frame (context);
}

#define SAFEPOINT \
	do {						\
		if (G_UNLIKELY (mono_polling_required)) \
			do_safepoint (frame, context);	\
	} while (0)

static MonoObject*
ves_array_create (MonoClass *klass, int param_count, stackval *values, MonoError *error)
{
	int rank = m_class_get_rank (klass);
	uintptr_t *lengths = g_newa (uintptr_t, rank * 2);
	intptr_t *lower_bounds = NULL;

	if (param_count > rank && m_class_get_byval_arg (klass)->type == MONO_TYPE_SZARRAY) {
		// Special constructor for jagged arrays
		for (int i = 0; i < param_count; ++i)
			lengths [i] = values [i].data.i;
		return (MonoObject*) mono_array_new_jagged_checked (klass, param_count, lengths, error);
	} else if (2 * rank == param_count) {
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
	return (MonoObject*) mono_array_new_full_checked (klass, lengths, lower_bounds, error);
}

static gint32
ves_array_calculate_index (MonoArray *ao, stackval *sp, gboolean safe)
{
	MonoClass *ac = ((MonoObject *) ao)->vtable->klass;

	guint32 pos = 0;
	if (ao->bounds) {
		for (gint32 i = 0; i < m_class_get_rank (ac); i++) {
			gint32 idx = sp [i].data.i;
			gint32 lower = ao->bounds [i].lower_bound;
			guint32 len = ao->bounds [i].length;
			if (safe && (idx < lower || (guint32)(idx - lower) >= len))
				return -1;
			pos = (pos * len) + (guint32)(idx - lower);
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
ves_array_element_address (InterpFrame *frame, MonoClass *required_type, MonoArray *ao, gpointer *ret, stackval *sp, gboolean needs_typecheck)
{
	MonoClass *ac = ((MonoObject *) ao)->vtable->klass;

	g_assert (m_class_get_rank (ac) >= 1);

	gint32 pos = ves_array_calculate_index (ao, sp, TRUE);
	if (pos == -1)
		return mono_get_exception_index_out_of_range ();

	if (needs_typecheck && !mono_class_is_assignable_from_internal (m_class_get_element_class (mono_object_class ((MonoObject *) ao)), required_type))
		return mono_get_exception_array_type_mismatch ();
	gint32 esize = mono_array_element_size (ac);
	*ret = mono_array_addr_with_size_fast (ao, esize, pos);
	return NULL;
}

/* Does not handle `this` argument */
static guint32
compute_arg_offset (MonoMethodSignature *sig, int index, int prev_offset)
{
	if (index == 0)
		return 0;

	if (prev_offset == -1) {
		guint32 offset = 0;
		for (int i = 0; i < index; i++) {
			int size, align;
			MonoType *type = sig->params [i];
			size = mono_type_size (type, &align);
			offset += ALIGN_TO (size, MINT_STACK_SLOT_SIZE);
		}
		return offset;
	} else {
		int size, align;
		MonoType *type = sig->params [index - 1];
		size = mono_type_size (type, &align);
		return prev_offset + ALIGN_TO (size, MINT_STACK_SLOT_SIZE);
	}
}

static guint32*
initialize_arg_offsets (InterpMethod *imethod, MonoMethodSignature *csig)
{
	if (imethod->arg_offsets)
		return imethod->arg_offsets;

	// For pinvokes, csig represents the real signature with marshalled args. If an explicit
	// marshalled signature was not provided, we use the managed signature of the method.
	MonoMethodSignature *sig = csig;
	if (!sig)
		sig = mono_method_signature_internal (imethod->method);
	int arg_count = sig->hasthis + sig->param_count;
	g_assert (arg_count);
	guint32 *arg_offsets = (guint32*) g_malloc ((sig->hasthis + sig->param_count) * sizeof (int));
	int index = 0, offset_addend = 0, prev_offset = 0;

	if (sig->hasthis) {
		arg_offsets [index++] = 0;
		offset_addend = MINT_STACK_SLOT_SIZE;
	}

	for (int i = 0; i < sig->param_count; i++) {
		prev_offset = compute_arg_offset (sig, i, prev_offset);
		arg_offsets [index++] = prev_offset + offset_addend;
	}

	mono_memory_write_barrier ();
	if (mono_atomic_cas_ptr ((gpointer*)&imethod->arg_offsets, arg_offsets, NULL) != NULL)
		g_free (arg_offsets);
	return imethod->arg_offsets;
}

static guint32
get_arg_offset_fast (InterpMethod *imethod, MonoMethodSignature *sig, int index)
{
	guint32 *arg_offsets = imethod->arg_offsets;
	if (arg_offsets)
		return arg_offsets [index];

	arg_offsets = initialize_arg_offsets (imethod, sig);
	g_assert (arg_offsets);
	return arg_offsets [index];
}

static guint32
get_arg_offset (InterpMethod *imethod, MonoMethodSignature *sig, int index)
{
	if (imethod) {
		return get_arg_offset_fast (imethod, sig, index);
	} else {
		g_assert (!sig->hasthis);
		return compute_arg_offset (sig, index, -1);
	}
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
		case MONO_TYPE_FNPTR:
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
		guint32 offset = get_arg_offset (frame->imethod, sig, i);
		stackval *sp_arg = STACK_ADD_BYTES (frame->stack, offset);
		MonoType *type = sig->params [i];
		guint32 ptype;
retry:
		ptype = type->byref ? MONO_TYPE_PTR : type->type;
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
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
#if SIZEOF_VOID_P == 8
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
#endif
			margs->iargs [int_i] = sp_arg->data.p;
#if DEBUG_INTERP
			g_print ("build_args_from_sig: margs->iargs [%d]: %p (frame @ %d)\n", int_i, margs->iargs [int_i], i);
#endif
			int_i++;
			break;
		case MONO_TYPE_VALUETYPE:
			if (m_class_is_enumtype (type->data.klass)) {
				type = mono_class_enum_basetype_internal (type->data.klass);
				goto retry;
			}
			margs->iargs [int_i] = sp_arg;
#if DEBUG_INTERP
			g_print ("build_args_from_sig: margs->iargs [%d]: %p (vt) (frame @ %d)\n", int_i, margs->iargs [int_i], i);
#endif
			int_i++;
			break;
		case MONO_TYPE_GENERICINST: {
			MonoClass *container_class = type->data.generic_class->container_class;
			type = m_class_get_byval_arg (container_class);
			goto retry;
		}
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_I8:
		case MONO_TYPE_U8: {
#ifdef TARGET_ARM
			/* pairs begin at even registers */
			if (i8_align == 8 && int_i & 1)
				int_i++;
#endif
			margs->iargs [int_i] = (gpointer) sp_arg->data.pair.lo;
			int_i++;
			margs->iargs [int_i] = (gpointer) sp_arg->data.pair.hi;
#if DEBUG_INTERP
			g_print ("build_args_from_sig: margs->iargs [%d/%d]: 0x%016" PRIx64 ", hi=0x%08x lo=0x%08x (frame @ %d)\n", int_i - 1, int_i, *((guint64 *) &margs->iargs [int_i - 1]), sp_arg->data.pair.hi, sp_arg->data.pair.lo, i);
#endif
			int_i++;
			break;
		}
#endif
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			if (ptype == MONO_TYPE_R4)
				* (float *) &(margs->fargs [int_f]) = sp_arg->data.f_r4;
			else
				margs->fargs [int_f] = sp_arg->data.f;
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
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_GENERICINST:
			margs->retval = (gpointer*)frame->retval;
			margs->is_float_ret = 0;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			margs->retval = (gpointer*)frame->retval;
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
	InterpMethod *imethod = iframe->imethod;

	// If index == -1, we finished executing an InterpFrame and the result is at retval.
	if (index == -1)
		stackval_to_data (sig->ret, iframe->retval, data, TRUE);
	else if (sig->hasthis && index == 0)
		*(gpointer*)data = iframe->stack->data.p;
	else
		stackval_to_data (sig->params [index - sig->hasthis], STACK_ADD_BYTES (iframe->stack, get_arg_offset (imethod, sig, index)), data, sig->pinvoke);
}

static void
interp_data_to_frame_arg (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gconstpointer data)
{
	InterpFrame *iframe = (InterpFrame*)frame;
	InterpMethod *imethod = iframe->imethod;

	// Get result from pinvoke call, put it directly on top of execution stack in the caller frame
	if (index == -1)
		stackval_from_data (sig->ret, iframe->retval, data, TRUE);
	else if (sig->hasthis && index == 0)
		iframe->stack->data.p = *(gpointer*)data;
	else
		stackval_from_data (sig->params [index - sig->hasthis], STACK_ADD_BYTES (iframe->stack, get_arg_offset (imethod, sig, index)), data, sig->pinvoke);
}

static gpointer
interp_frame_arg_to_storage (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index)
{
	InterpFrame *iframe = (InterpFrame*)frame;
	InterpMethod *imethod = iframe->imethod;

	if (index == -1)
		return iframe->retval;
	else
		return STACK_ADD_BYTES (iframe->stack, get_arg_offset (imethod, sig, index));
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
static MONO_NO_OPTIMIZATION MONO_NEVER_INLINE gpointer
ves_pinvoke_method (
	InterpMethod *imethod,
	MonoMethodSignature *sig,
	MonoFuncV addr,
	ThreadContext *context,
	InterpFrame *parent_frame,
	stackval *ret_sp,
	stackval *sp,
	gboolean save_last_error,
	gpointer *cache)
{
	InterpFrame frame = {0};
	frame.parent = parent_frame;
	frame.imethod = imethod;
	frame.stack = sp;
	frame.retval = ret_sp;

	MonoLMFExt ext;
	gpointer args;

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

	if (save_last_error) {
		mono_marshal_clear_last_error ();
	}

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
	// Only the vt address has been returned, we need to copy the entire content on interp stack
	if (!context->has_resume_state && MONO_TYPE_ISSTRUCT (sig->ret))
		stackval_from_data (sig->ret, frame.retval, (char*)frame.retval->data.p, sig->pinvoke);

	g_free (margs->iargs);
	g_free (margs->fargs);
	g_free (margs);
#endif
	goto exit_pinvoke; // prevent unused label warning in some configurations
exit_pinvoke:
	return NULL;
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
		if (mono_llvm_only)
			// FIXME:
			g_assert_not_reached ();
	} else if (del->method) {
		/* Delegate created dynamically */
		del->interp_method = mono_interp_get_imethod (del->method, error);
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
			del->interp_method = mono_interp_get_imethod (mono_marshal_get_delegate_invoke (method, NULL), error);
			mono_error_assert_ok (error);
		}
	}

	if (!((InterpMethod *) del->interp_method)->transformed && method_is_dynamic (method)) {
		/* Return any errors from method compilation */
		mono_interp_transform_method ((InterpMethod *) del->interp_method, get_context (), error);
		return_if_nok (error);
	}
}

/* Convert a function pointer for a managed method to an InterpMethod* */
static InterpMethod*
ftnptr_to_imethod (gpointer addr)
{
	InterpMethod *imethod;

	if (mono_llvm_only) {
		ERROR_DECL (error);
		/* Function pointers are represented by a MonoFtnDesc structure */
		MonoFtnDesc *ftndesc = (MonoFtnDesc*)addr;
		g_assert (ftndesc);
		g_assert (ftndesc->method);

		imethod = ftndesc->interp_method;
		if (!imethod) {
			imethod = mono_interp_get_imethod (ftndesc->method, error);
			mono_error_assert_ok (error);
			mono_memory_barrier ();
			ftndesc->interp_method = imethod;
		}
	} else {
		/* Function pointers are represented by their InterpMethod */
		imethod = (InterpMethod*)addr;
	}
	return imethod;
}

static gpointer
imethod_to_ftnptr (InterpMethod *imethod)
{
	if (mono_llvm_only) {
		ERROR_DECL (error);
		/* Function pointers are represented by a MonoFtnDesc structure */
		MonoFtnDesc *ftndesc = imethod->ftndesc;
		if (!ftndesc) {
			ftndesc = mini_llvmonly_load_method_ftndesc (imethod->method, FALSE, FALSE, error);
			mono_error_assert_ok (error);
			mono_memory_barrier ();
			imethod->ftndesc = ftndesc;
		}
		return ftndesc;
	} else {
		return imethod;
	}
}

static void
interp_delegate_ctor (MonoObjectHandle this_obj, MonoObjectHandle target, gpointer addr, MonoError *error)
{
	/* addr is the result of an LDFTN opcode */
	InterpMethod *imethod = ftnptr_to_imethod (addr);

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

#if DEBUG_INTERP
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
	case MONO_TYPE_FNPTR:
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
		dump_stackval (str, inv->stack, ret);

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
	stackval *sp = (stackval*)context->stack_pointer;
	MonoMethod *target_method = method;

	error_init (error);
	if (exc)
		*exc = NULL;

	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
		target_method = mono_marshal_get_native_wrapper (target_method, FALSE, FALSE);
	MonoMethod *invoke_wrapper = mono_marshal_get_runtime_invoke_full (target_method, FALSE, TRUE);

	//* <code>MonoObject *runtime_invoke (MonoObject *this_obj, void **params, MonoObject **exc, void* method)</code>

	if (sig->hasthis)
		sp [0].data.p = obj;
	else
		sp [0].data.p = NULL;
	sp [1].data.p = params;
	sp [2].data.p = exc;
	sp [3].data.p = target_method;

	InterpMethod *imethod = mono_interp_get_imethod (invoke_wrapper, error);
	mono_error_assert_ok (error);

	InterpFrame frame = {0};
	frame.imethod = imethod;
	frame.stack = sp;
	frame.retval = sp;

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
	// The return value is at the bottom of the stack
	return frame.stack->data.o;
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
	stackval *sp, *sp_args;
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
	sp_args = sp = (stackval*)context->stack_pointer;

	method = rmethod->method;
	sig = mono_method_signature_internal (method);

	// FIXME: Optimize this

	if (sig->hasthis) {
		sp_args->data.p = data->this_arg;
		sp_args++;
	}

	gpointer *params;
	if (data->many_args)
		params = data->many_args;
	else
		params = data->args;
	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			sp_args->data.p = params [i];
			sp_args++;
		} else {
			int size = stackval_from_data (sig->params [i], sp_args, params [i], FALSE);
			sp_args = STACK_ADD_BYTES (sp_args, size);
		}
	}

	InterpFrame frame = {0};
	frame.imethod = data->rmethod;
	frame.stack = sp;
	frame.retval = sp;

	context->stack_pointer = (guchar*)sp_args;

	interp_exec_method (&frame, context, NULL);

	context->stack_pointer = (guchar*)sp;

	g_assert (!context->safepoint_frame);

	if (rmethod->needs_thread_attach)
		mono_threads_detach_coop (orig_domain, &attach_cookie);

	if (mono_llvm_only) {
		if (context->has_resume_state)
			/* The exception will be handled in a frame above us */
			mono_llvm_reraise_exception ((MonoException*)mono_gchandle_get_target_internal (context->exc_gchandle));
	} else {
		g_assert (!context->has_resume_state);
	}

	// The return value is at the bottom of the stack, after the locals space
	type = rmethod->rtype;
	if (type->type != MONO_TYPE_VOID)
		stackval_to_data (type, frame.stack, data->res, FALSE);
}

static void
do_icall (MonoMethodSignature *sig, int op, stackval *ret_sp, stackval *sp, gpointer ptr, gboolean save_last_error)
{
	if (save_last_error)
		mono_marshal_clear_last_error ();

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
		ret_sp->data.p = func ();
		break;
	}
	case MINT_ICALL_P_V: {
		typedef void (*T)(gpointer);
		T func = (T)ptr;
		func (sp [0].data.p);
		break;
	}
	case MINT_ICALL_P_P: {
		typedef gpointer (*T)(gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p);
		break;
	}
	case MINT_ICALL_PP_V: {
		typedef void (*T)(gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p);
		break;
	}
	case MINT_ICALL_PP_P: {
		typedef gpointer (*T)(gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p);
		break;
	}
	case MINT_ICALL_PPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p);
		break;
	}
	case MINT_ICALL_PPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p);
		break;
	}
	case MINT_ICALL_PPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p);
		break;
	}
	case MINT_ICALL_PPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p);
		break;
	}
	case MINT_ICALL_PPPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p);
		break;
	}
	case MINT_ICALL_PPPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p);
		break;
	}
	case MINT_ICALL_PPPPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p, sp [5].data.p);
		break;
	}
	case MINT_ICALL_PPPPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p, sp [5].data.p);
		break;
	}
	default:
		g_assert_not_reached ();
	}

	if (save_last_error)
		mono_marshal_set_last_error ();

	/* convert the native representation to the stackval representation */
	if (sig)
		stackval_from_data (sig->ret, ret_sp, (char*) &ret_sp->data.p, sig->pinvoke);
}

/* MONO_NO_OPTIMIZATION is needed due to usage of INTERP_PUSH_LMF_WITH_CTX. */
#ifdef _MSC_VER
#pragma optimize ("", off)
#endif
// Do not inline in case order of frame addresses matters, and maybe other reasons.
static MONO_NO_OPTIMIZATION MONO_NEVER_INLINE gpointer
do_icall_wrapper (InterpFrame *frame, MonoMethodSignature *sig, int op, stackval *ret_sp, stackval *sp, gpointer ptr, gboolean save_last_error)
{
	MonoLMFExt ext;
	INTERP_PUSH_LMF_WITH_CTX (frame, ext, exit_icall);

	do_icall (sig, op, ret_sp, sp, ptr, save_last_error);

	interp_pop_lmf (&ext);

	goto exit_icall; // prevent unused label warning in some configurations
	/* If an exception is thrown from native code, execution will continue here */
exit_icall:
	return NULL;
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
	gint32 res_size;
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
			cinfo->res_size = ALIGN_TO (size, MINT_VT_ALIGNMENT);
		} else {
			cinfo->res_size = MINT_STACK_SLOT_SIZE;
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
do_jit_call (ThreadContext *context, stackval *ret_sp, stackval *sp, InterpFrame *frame, InterpMethod *rmethod, MonoError *error)
{
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
	/* return address */
	if (cinfo->ret_mt != -1)
		args [pindex ++] = ret_sp;
	for (int i = 0; i < rmethod->param_count; ++i) {
		stackval *sval = STACK_ADD_BYTES (sp, get_arg_offset_fast (rmethod, NULL, stack_index + i));
		if (cinfo->arginfo [i] == JIT_ARG_BYVAL)
			args [pindex ++] = sval->data.p;
		else
			/* data is an union, so can use 'p' for all types */
			args [pindex ++] = sval;
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
		if (context->has_resume_state)
			/*
			 * This happens when interp_entry calls mono_llvm_reraise_exception ().
			 */
			return;
		MonoObject *obj = mono_llvm_load_exception ();
		g_assert (obj);
		mono_llvm_clear_exception ();
		mono_error_set_exception_instance (error, (MonoException*)obj);
		return;
	}
	if (cinfo->ret_mt != -1) {
		//  Sign/zero extend if necessary
		switch (cinfo->ret_mt) {
		case MINT_TYPE_I1:
			ret_sp->data.i = *(gint8*)ret_sp;
			break;
		case MINT_TYPE_U1:
			ret_sp->data.i = *(guint8*)ret_sp;
			break;
		case MINT_TYPE_I2:
			ret_sp->data.i = *(gint16*)ret_sp;
			break;
		case MINT_TYPE_U2:
			ret_sp->data.i = *(guint16*)ret_sp;
			break;
		case MINT_TYPE_I4:
		case MINT_TYPE_I8:
		case MINT_TYPE_R4:
		case MINT_TYPE_R8:
		case MINT_TYPE_VT:
		case MINT_TYPE_O:
			/* The result was written to ret_sp */
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
init_arglist (InterpFrame *frame, MonoMethodSignature *sig, stackval *sp, char *arglist)
{
	*(gpointer*)arglist = sig;
	arglist += sizeof (gpointer);

	for (int i = sig->sentinelpos; i < sig->param_count; i++) {
		int align, arg_size, sv_size;
		arg_size = mono_type_stack_size (sig->params [i], &align);
		arglist = (char*)ALIGN_PTR_TO (arglist, align);

		sv_size = stackval_to_data (sig->params [i], sp, arglist, FALSE);
		arglist += arg_size;
		sp = STACK_ADD_BYTES (sp, sv_size);
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

// Do not inline in case order of frame addresses matters.
static MONO_NEVER_INLINE void
interp_entry_from_trampoline (gpointer ccontext_untyped, gpointer rmethod_untyped)
{
	ThreadContext *context;
	stackval *sp;
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
		MonoMethodSignature *newsig = (MonoMethodSignature*)g_alloca (MONO_SIZEOF_METHOD_SIGNATURE + ((sig->param_count + 2) * sizeof (MonoType*)));
		memcpy (newsig, sig, mono_metadata_signature_size (sig));
		newsig->ret = m_class_get_byval_arg (mono_defaults.string_class);
		sig = newsig;
	}

	InterpFrame frame = {0};
	frame.imethod = rmethod;
	frame.stack = sp;
	frame.retval = sp;

	/* Copy the args saved in the trampoline to the frame stack */
	gpointer retp = mono_arch_get_native_call_context_args (ccontext, &frame, sig);

	/* Allocate storage for value types */
	stackval *newsp = sp;
	/* FIXME we should reuse computation on imethod for this */
	if (sig->hasthis)
		newsp++;
	for (i = 0; i < sig->param_count; i++) {
		MonoType *type = sig->params [i];
		int size;

		if (type->type == MONO_TYPE_GENERICINST && !MONO_TYPE_IS_REFERENCE (type)) {
			size = mono_class_value_size (mono_class_from_mono_type_internal (type), NULL);
		} else if (type->type == MONO_TYPE_VALUETYPE) {
			if (sig->pinvoke)
				size = mono_class_native_size (type->data.klass, NULL);
			else
				size = mono_class_value_size (type->data.klass, NULL);
		} else {
			size = MINT_STACK_SLOT_SIZE;
		}
		newsp = STACK_ADD_BYTES (newsp, size);
	}
	context->stack_pointer = (guchar*)newsp;

	interp_exec_method (&frame, context, NULL);

	context->stack_pointer = (guchar*)sp;
	g_assert (!context->has_resume_state);

	if (rmethod->needs_thread_attach)
		mono_threads_detach_coop (orig_domain, &attach_cookie);

	/* Write back the return value */
	/* 'frame' is still valid */
	mono_arch_set_native_call_context_ret (ccontext, &frame, sig, retp);
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
	InterpMethod *res = NULL;
	MonoJitMemoryManager *jit_mm = get_default_jit_mm ();

	jit_mm_lock (jit_mm);
	if (jit_mm->interp_method_pointer_hash)
		res = (InterpMethod*)g_hash_table_lookup (jit_mm->interp_method_pointer_hash, addr);
	jit_mm_unlock (jit_mm);

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
	gpointer addr, entry_func, entry_wrapper;
	MonoMethodSignature *sig;
	MonoMethod *wrapper;
	InterpMethod *imethod;

	imethod = mono_interp_get_imethod (method, error);
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
	MonoFtnDesc *entry_ftndesc = mini_llvmonly_create_ftndesc (method, entry_func, entry_arg);

	addr = mini_llvmonly_create_ftndesc (method, entry_wrapper, entry_ftndesc);

	// FIXME:
	MonoJitMemoryManager *jit_mm = get_default_jit_mm ();
	jit_mm_lock (jit_mm);
	if (!jit_mm->interp_method_pointer_hash)
		jit_mm->interp_method_pointer_hash = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (jit_mm->interp_method_pointer_hash, addr, imethod);
	jit_mm_unlock (jit_mm);

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
	InterpMethod *imethod = mono_interp_get_imethod (method, error);

	if (imethod->jit_entry)
		return imethod->jit_entry;

	if (compile && !imethod->transformed) {
		/* Return any errors from method compilation */
		mono_interp_transform_method (imethod, get_context (), error);
		return_val_if_nok (error, NULL);
	}

	MonoMethodSignature *sig = mono_method_signature_internal (method);
	if (method->string_ctor) {
		MonoMethodSignature *newsig = (MonoMethodSignature*)g_alloca (MONO_SIZEOF_METHOD_SIGNATURE + ((sig->param_count + 2) * sizeof (MonoType*)));
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

		/*
		 * The runtime expects a function pointer unique to method and
		 * the native caller expects a function pointer with the
		 * right signature, so fail right away.
		 */
		mono_error_set_platform_not_supported (error, "No native to managed transitions on this platform.");
		return NULL;
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
		mono_interp_error_cleanup (error);
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

	MonoJitMemoryManager *jit_mm = get_default_jit_mm ();
	jit_mm_lock (jit_mm);
	if (!jit_mm->interp_method_pointer_hash)
		jit_mm->interp_method_pointer_hash = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (jit_mm->interp_method_pointer_hash, addr, imethod);
	jit_mm_unlock (jit_mm);

	mono_memory_barrier ();
	imethod->jit_entry = addr;

	return addr;
}

static void
interp_free_method (MonoMethod *method)
{
	MonoJitMemoryManager *jit_mm = jit_mm_for_method (method);

	jit_mm_lock (jit_mm);
	/* InterpMethod is allocated in the domain mempool. We might haven't
	 * allocated an InterpMethod for this instance yet */
	mono_internal_hash_table_remove (&jit_mm->interp_code_hash, method);
	jit_mm_unlock (jit_mm);
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
		output_indent (); \
		char *mn = mono_method_full_name (frame->imethod->method, FALSE); \
		char *disasm = mono_interp_dis_mintop ((gint32)(ip - frame->imethod->code), TRUE, ip + 1, *ip); \
		g_print ("(%p) %s -> %s\n", mono_thread_internal_current (), mn, disasm); \
		g_free (mn); \
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
mono_interp_new (MonoClass* klass)
{
	ERROR_DECL (error);
	MonoObject* const object = mono_object_new_checked (klass, error);
	mono_error_cleanup (error); // FIXME: do not swallow the error
	return object;
}

static gboolean
mono_interp_isinst (MonoObject* object, MonoClass* klass)
{
	ERROR_DECL (error);
	gboolean isinst;
	MonoClass *obj_class = mono_object_class (object);
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

	InterpMethod *cmethod = mono_interp_get_imethod (m, error);
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
	do_icall_wrapper (&frame, NULL, MINT_ICALL_V_P, &tmp_sp, &tmp_sp, (gpointer)mono_thread_get_undeniable_exception, FALSE);

	return (MonoException*)tmp_sp.data.p;
}

static gint32
mono_interp_enum_hasflag (stackval *sp1, stackval *sp2, MonoClass* klass)
{
	guint64 a_val = 0, b_val = 0;

	stackval_to_data (m_class_get_byval_arg (klass), sp1, &a_val, FALSE);
	stackval_to_data (m_class_get_byval_arg (klass), sp2, &b_val, FALSE);
	return (a_val & b_val) == b_val;
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
	} while (0)

/* Load and clear state from FRAME */
#define LOAD_INTERP_STATE(frame) do { \
	ip = frame->state.ip; \
	locals = (unsigned char *)frame->stack; \
	frame->state.ip = NULL; \
	} while (0)

/* Initialize interpreter state for executing FRAME */
#define INIT_INTERP_STATE(frame, _clause_args) do {	 \
	ip = _clause_args ? ((FrameClauseArgs *)_clause_args)->start_with_ip : (frame)->imethod->code; \
	locals = (unsigned char *)(frame)->stack; \
	} while (0)

#if PROFILE_INTERP
static long total_executed_opcodes;
#endif

#define LOCAL_VAR(offset,type) (*(type*)(locals + (offset)))

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
	unsigned char *locals = NULL;
	int call_args_offset;
	int return_offset;

#if DEBUG_INTERP
	int tracing = global_tracing;
#endif
#if USE_COMPUTED_GOTO
	static void * const in_labels[] = {
#define OPDEF(a,b,c,d,e,f) &&LAB_ ## a,
#include "mintops.def"
	};
#endif

	HANDLE_FUNCTION_ENTER ();
	/*
	 * GC SAFETY:
	 *
	 *  The interpreter executes in gc unsafe (non-preempt) mode. On wasm, we cannot rely on
	 * scanning the stack or any registers. In order to make the code GC safe, every objref
	 * handled by the code needs to be kept alive and pinned in any of the following ways:
	 * - the object needs to be stored on the interpreter stack. In order to make sure the
	 * object actually gets stored on the interp stack and the store is not optimized out,
	 * the store/variable should be volatile.
	 * - if the execution of an opcode requires an object not coming from interp stack to be
	 * kept alive, the tmp_handle below can be used. This handle will keep only one object
	 * pinned by the GC. Ideally, once this object is no longer needed, the handle should be
	 * cleared. If we will need to have more objects pinned simultaneously, additional handles
	 * can be reserved here.
	 */
	MonoObjectHandle tmp_handle = MONO_HANDLE_NEW (MonoObject, NULL);

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

#ifdef ENABLE_EXPERIMENT_TIERED
	mini_tiered_inc (frame->imethod->method, &frame->imethod->tiered_counter, 0);
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
		DUMP_INSTR();
		MINT_IN_SWITCH (*ip) {
		MINT_IN_CASE(MINT_INITLOCAL)
		MINT_IN_CASE(MINT_INITLOCALS)
			memset (locals + ip [1], 0, ip [2]);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NOP)
		MINT_IN_CASE(MINT_NIY)
		MINT_IN_CASE(MINT_DEF)
		MINT_IN_CASE(MINT_DUMMY_USE)
			g_assert_not_reached ();
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BREAK)
			++ip;
			do_debugger_tramp (mono_component_debugger ()->user_break, frame);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BREAKPOINT)
			++ip;
			mono_break ();
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_INIT_ARGLIST) {
			const guint16 *call_ip = frame->parent->state.ip - 6;
			g_assert_checked (*call_ip == MINT_CALL_VARARG);
			int params_stack_size = call_ip [5];
			MonoMethodSignature *sig = (MonoMethodSignature*)frame->parent->imethod->data_items [call_ip [4]];

			// we are being overly conservative with the size here, for simplicity
			gpointer arglist = frame_data_allocator_alloc (&context->data_stack, frame, params_stack_size + MINT_STACK_SLOT_SIZE);

			init_arglist (frame, sig, STACK_ADD_BYTES (frame->stack, ip [2]), (char*)arglist);

			// save the arglist for future access with MINT_ARGLIST
			LOCAL_VAR (ip [1], gpointer) = arglist;

			ip += 3;
			MINT_IN_BREAK;
		}

#define LDC(n) do { LOCAL_VAR (ip [1], gint32) = (n); ip += 2; } while (0)
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
			LOCAL_VAR (ip [1], gint32) = (short)ip [2];
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I4)
			LOCAL_VAR (ip [1], gint32) = READ32 (ip + 2);
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I8_0)
			LOCAL_VAR (ip [1], gint64) = 0;
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I8)
			LOCAL_VAR (ip [1], gint64) = READ64 (ip + 2);
			ip += 6;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_I8_S)
			LOCAL_VAR (ip [1], gint64) = (short)ip [2];
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDC_R4) {
			LOCAL_VAR (ip [1], gint32) = READ32(ip + 2); /* not union usage */
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDC_R8) 
			LOCAL_VAR (ip [1], gint64) = READ64 (ip + 2); /* note union usage */
			ip += 6;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_JMP) {
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
			ip = frame->imethod->code;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALL_DELEGATE) {
			// FIXME We don't need to encode the whole signature, just param_count
			MonoMethodSignature *csignature = (MonoMethodSignature*)frame->imethod->data_items [ip [4]];
			int param_count = csignature->param_count;
			return_offset = ip [1];
			call_args_offset = ip [2];
			MonoDelegate *del = LOCAL_VAR (call_args_offset, MonoDelegate*);
			gboolean is_multicast = del->method == NULL;
			InterpMethod *del_imethod = (InterpMethod*)del->interp_invoke_impl;

			if (!del_imethod) {
				if (is_multicast) {
					error_init_reuse (error);
					MonoMethod *invoke = mono_get_delegate_invoke_internal (del->object.vtable->klass);
					del_imethod = mono_interp_get_imethod (mono_marshal_get_delegate_invoke (invoke, del), error);
					del->interp_invoke_impl = del_imethod;
					mono_error_assert_ok (error);
				} else if (!del->interp_method) {
					// Not created from interpreted code
					error_init_reuse (error);
					g_assert (del->method);
					del_imethod = mono_interp_get_imethod (del->method, error);
					del->interp_method = del_imethod;
					del->interp_invoke_impl = del_imethod;
					mono_error_assert_ok (error);
				} else {
					del_imethod = (InterpMethod*)del->interp_method;
					if (del_imethod->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
						error_init_reuse (error);
						del_imethod = mono_interp_get_imethod (mono_marshal_get_native_wrapper (del_imethod->method, FALSE, FALSE), error);
						mono_error_assert_ok (error);
						del->interp_invoke_impl = del_imethod;
					} else if (del_imethod->method->flags & METHOD_ATTRIBUTE_VIRTUAL && !del->target && !m_class_is_valuetype (del_imethod->method->klass)) {
						// 'this' is passed dynamically, we need to recompute the target method
						// with each call
						del_imethod = get_virtual_method (del_imethod, LOCAL_VAR (call_args_offset + MINT_STACK_SLOT_SIZE, MonoObject*)->vtable);
					} else {
						del->interp_invoke_impl = del_imethod;
					}
				}
			}
			cmethod = del_imethod;
			if (!is_multicast) {
				if (cmethod->param_count == param_count + 1) {
					// Target method is static but the delegate has a target object. We handle
					// this separately from the case below, because, for these calls, the instance
					// is allowed to be null.
					LOCAL_VAR (call_args_offset, MonoObject*) = del->target;
				} else if (del->target) {
					MonoObject *this_arg = del->target;

					// replace the MonoDelegate* on the stack with 'this' pointer
					if (m_class_is_valuetype (this_arg->vtable->klass)) {
						gpointer unboxed = mono_object_unbox_internal (this_arg);
						LOCAL_VAR (call_args_offset, gpointer) = unboxed;
					} else {
						LOCAL_VAR (call_args_offset, MonoObject*) = this_arg;
					}
				} else {
					// skip the delegate pointer for static calls
					// FIXME we could avoid memmove
					memmove (locals + call_args_offset, locals + call_args_offset + MINT_STACK_SLOT_SIZE, ip [3]);
				}
			}
			ip += 5;

			goto call;
		}
		MINT_IN_CASE(MINT_CALLI) {
			MonoMethodSignature *csignature;

			csignature = (MonoMethodSignature*)frame->imethod->data_items [ip [4]];

			/* In mixed mode, stay in the interpreter for simplicity even if there is an AOT version of the callee */
			cmethod = ftnptr_to_imethod (LOCAL_VAR (ip [2], gpointer));

			if (cmethod->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
				cmethod = mono_interp_get_imethod (mono_marshal_get_native_wrapper (cmethod->method, FALSE, FALSE), error);
				mono_interp_error_cleanup (error); /* FIXME: don't swallow the error */
			}

			return_offset = ip [1];
			call_args_offset = ip [3];

			if (csignature->hasthis) {
				MonoObject *this_arg = LOCAL_VAR (call_args_offset, MonoObject*); 

				if (m_class_is_valuetype (this_arg->vtable->klass)) {
					gpointer unboxed = mono_object_unbox_internal (this_arg);
					LOCAL_VAR (call_args_offset, gpointer) = unboxed;
				}
			}
			ip += 5;

			goto call;
		}
		MINT_IN_CASE(MINT_CALLI_NAT_FAST) {
			MonoMethodSignature *csignature = (MonoMethodSignature*)frame->imethod->data_items [ip [4]];
			int opcode = ip [5];
			gboolean save_last_error = ip [6];

			stackval *ret = (stackval*)(locals + ip [1]);
			gpointer target_ip = LOCAL_VAR (ip [2], gpointer);
			stackval *args = (stackval*)(locals + ip [3]);
			/* for calls, have ip pointing at the start of next instruction */
			frame->state.ip = ip + 7;

			do_icall_wrapper (frame, csignature, opcode, ret, args, target_ip, save_last_error);
			EXCEPTION_CHECKPOINT_GC_UNSAFE;
			CHECK_RESUME_STATE (context);
			ip += 7;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALLI_NAT_DYNAMIC) {
			MonoMethodSignature* csignature = (MonoMethodSignature*)frame->imethod->data_items [ip [4]];

			return_offset = ip [1];
			guchar* code = LOCAL_VAR (ip [2], guchar*);
			call_args_offset = ip [3];

			cmethod = mono_interp_get_native_func_wrapper (frame->imethod, csignature, code);

			ip += 5;
			goto call;
		}
		MINT_IN_CASE(MINT_CALLI_NAT) {
			MonoMethodSignature *csignature = (MonoMethodSignature*)frame->imethod->data_items [ip [4]];
			InterpMethod *imethod = (InterpMethod*)frame->imethod->data_items [ip [5]];

			guchar *code = LOCAL_VAR (ip [2], guchar*);

			gboolean save_last_error = ip [6];
			gpointer *cache = (gpointer*)&frame->imethod->data_items [ip [7]];
			/* for calls, have ip pointing at the start of next instruction */
			frame->state.ip = ip + 8;
			ves_pinvoke_method (imethod, csignature, (MonoFuncV)code, context, frame, (stackval*)(locals + ip [1]), (stackval*)(locals + ip [3]), save_last_error, cache);

			EXCEPTION_CHECKPOINT_GC_UNSAFE;
			CHECK_RESUME_STATE (context);

			ip += 8;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALLVIRT_FAST) {
			MonoObject *this_arg;
			int slot;

			cmethod = (InterpMethod*)frame->imethod->data_items [ip [3]];
			return_offset = ip [1];
			call_args_offset = ip [2];

			this_arg = LOCAL_VAR (call_args_offset, MonoObject*);

			slot = (gint16)ip [4];
			ip += 5;
			cmethod = get_virtual_method_fast (cmethod, this_arg->vtable, slot);
			if (m_class_is_valuetype (this_arg->vtable->klass) && m_class_is_valuetype (cmethod->method->klass)) {
				/* unbox */
				gpointer unboxed = mono_object_unbox_internal (this_arg);
				LOCAL_VAR (call_args_offset, gpointer) = unboxed;
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
				do_jit_call (context, (stackval*)(locals + return_offset), (stackval*)(locals + call_args_offset), frame, cmethod, error);
				if (!is_ok (error)) {
					MonoException *ex = mono_error_convert_to_exception (error);
					THROW_EX (ex, ip);
				}

				CHECK_RESUME_STATE (context);
			}

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALL_VARARG) {
			// Same as MINT_CALL, except at ip [4] we have the index for the csignature,
			// which is required by the called method to set up the arglist.
			cmethod = (InterpMethod*)frame->imethod->data_items [ip [3]];
			return_offset = ip [1];
			call_args_offset = ip [2];
			ip += 6;
			goto call;
		}

		MINT_IN_CASE(MINT_CALLVIRT) {
			// FIXME CALLVIRT opcodes are not used on netcore. We should kill them.
			cmethod = (InterpMethod*)frame->imethod->data_items [ip [3]];
			return_offset = ip [1];
			call_args_offset = ip [2];

			MonoObject *this_arg = LOCAL_VAR (call_args_offset, MonoObject*);

			cmethod = get_virtual_method (cmethod, this_arg->vtable);
			if (m_class_is_valuetype (this_arg->vtable->klass) && m_class_is_valuetype (cmethod->method->klass)) {
				/* unbox */
				gpointer unboxed = mono_object_unbox_internal (this_arg);
				LOCAL_VAR (call_args_offset, gpointer) = unboxed;
			}

#ifdef ENABLE_EXPERIMENT_TIERED
			ip += 5;
#else
			ip += 4;
#endif
			goto call;
		}
		MINT_IN_CASE(MINT_CALL) {
			cmethod = (InterpMethod*)frame->imethod->data_items [ip [3]];
			return_offset = ip [1];
			call_args_offset = ip [2];

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
				reinit_frame (child_frame, frame, cmethod, locals + return_offset, locals + call_args_offset);
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

			context->stack_pointer = (guchar*)frame->stack + cmethod->alloca_size;
			/* Make sure the stack pointer is bumped before we store any references on the stack */
			mono_compiler_barrier ();

			INIT_INTERP_STATE (frame, NULL);

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_JIT_CALL) {
			InterpMethod *rmethod = (InterpMethod*)frame->imethod->data_items [ip [3]];
			error_init_reuse (error);
			/* for calls, have ip pointing at the start of next instruction */
			frame->state.ip = ip + 4;
			do_jit_call (context, (stackval*)(locals + ip [1]), (stackval*)(locals + ip [2]), frame, rmethod, error);
			if (!is_ok (error)) {
				MonoException *ex = mono_error_convert_to_exception (error);
				THROW_EX (ex, ip);
			}

			CHECK_RESUME_STATE (context);
			ip += 4;

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_JIT_CALL2) {
#ifdef ENABLE_EXPERIMENT_TIERED
			InterpMethod *rmethod = (InterpMethod *) READ64 (ip + 2);

			error_init_reuse (error);

			frame->state.ip = ip + 6;
			do_jit_call (context, (stackval*)(locals + ip [1]), frame, rmethod, error);
			if (!is_ok (error)) {
				MonoException *ex = mono_error_convert_to_exception (error);
				THROW_EX (ex, ip);
			}

			CHECK_RESUME_STATE (context);

			ip += 6;
#else
			g_error ("MINT_JIT_ICALL2 shouldn't be used");
#endif
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALLRUN) {
			g_assert_not_reached ();

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_RET)
			frame->retval [0] = LOCAL_VAR (ip [1], stackval);
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_I4_IMM)
			frame->retval [0].data.i = (gint16)ip [1];
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_I8_IMM)
			frame->retval [0].data.l = (gint16)ip [1];
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_VOID)
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_VT) {
			memmove (frame->retval, locals + ip [1], ip [2]);
			goto exit_frame;
		}
		MINT_IN_CASE(MINT_RET_LOCALLOC)
			frame->retval [0] = LOCAL_VAR (ip [1], stackval);
			frame_data_allocator_pop (&context->data_stack, frame);
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_VOID_LOCALLOC)
			frame_data_allocator_pop (&context->data_stack, frame);
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_VT_LOCALLOC) {
			memmove (frame->retval, locals + ip [1], ip [2]);
			frame_data_allocator_pop (&context->data_stack, frame);
			goto exit_frame;
		}

#ifdef ENABLE_EXPERIMENT_TIERED
#define BACK_BRANCH_PROFILE(offset) do { \
		if (offset < 0) \
			mini_tiered_inc (frame->imethod->method, &frame->imethod->tiered_counter, 0); \
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

#define ZEROP_S(datatype, op) \
	if (LOCAL_VAR (ip [1], datatype) op 0) { \
		gint16 br_offset = (gint16) ip [2]; \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 3;

#define ZEROP(datatype, op) \
	if (LOCAL_VAR (ip [1], datatype) op 0) { \
		gint32 br_offset = (gint32)READ32(ip + 2); \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 4;

		MINT_IN_CASE(MINT_BRFALSE_I4_S)
			ZEROP_S(gint32, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_I8_S)
			ZEROP_S(gint64, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_R4_S)
			ZEROP_S(float, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_R8_S)
			ZEROP_S(double, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_I4)
			ZEROP(gint32, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_I8)
			ZEROP(gint64, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_R4)
			ZEROP_S(float, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_R8)
			ZEROP_S(double, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_I4_S)
			ZEROP_S(gint32, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_I8_S)
			ZEROP_S(gint64, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_R4_S)
			ZEROP_S(float, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_R8_S)
			ZEROP_S(double, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_I4)
			ZEROP(gint32, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_I8)
			ZEROP(gint64, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_R4)
			ZEROP(float, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_R8)
			ZEROP(double, !=);
		MINT_IN_BREAK;
#define CONDBR_S(cond) \
	if (cond) { \
		gint16 br_offset = (gint16) ip [3]; \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 4;
#define BRELOP_S(datatype, op) \
	CONDBR_S(LOCAL_VAR (ip [1], datatype) op LOCAL_VAR (ip [2], datatype))

#define CONDBR(cond) \
	if (cond) { \
		gint32 br_offset = (gint32) READ32 (ip + 3); \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 5;

#define BRELOP(datatype, op) \
	CONDBR(LOCAL_VAR (ip [1], datatype) op LOCAL_VAR (ip [2], datatype))

		MINT_IN_CASE(MINT_BEQ_I4_S)
			BRELOP_S(gint32, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_I8_S)
			BRELOP_S(gint64, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_R4_S) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR_S(!isunordered (f1, f2) && f1 == f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BEQ_R8_S) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR_S(!mono_isunordered (d1, d2) && d1 == d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BEQ_I4)
			BRELOP(gint32, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_I8)
			BRELOP(gint64, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_R4) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR(!isunordered (f1, f2) && f1 == f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BEQ_R8) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR(!mono_isunordered (d1, d2) && d1 == d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGE_I4_S)
			BRELOP_S(gint32, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I8_S)
			BRELOP_S(gint64, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_R4_S) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR_S(!isunordered (f1, f2) && f1 >= f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGE_R8_S) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR_S(!mono_isunordered (d1, d2) && d1 >= d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGE_I4)
			BRELOP(gint32, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I8)
			BRELOP(gint64, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_R4) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR(!isunordered (f1, f2) && f1 >= f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGE_R8) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR(!mono_isunordered (d1, d2) && d1 >= d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGT_I4_S)
			BRELOP_S(gint32, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I8_S)
			BRELOP_S(gint64, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_R4_S) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR_S(!isunordered (f1, f2) && f1 > f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGT_R8_S) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR_S(!mono_isunordered (d1, d2) && d1 > d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGT_I4)
			BRELOP(gint32, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I8)
			BRELOP(gint64, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_R4) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR(!isunordered (f1, f2) && f1 > f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGT_R8) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR(!mono_isunordered (d1, d2) && d1 > d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLT_I4_S)
			BRELOP_S(gint32, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I8_S)
			BRELOP_S(gint64, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_R4_S) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR_S(!isunordered (f1, f2) && f1 < f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLT_R8_S) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR_S(!mono_isunordered (d1, d2) && d1 < d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLT_I4)
			BRELOP(gint32, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I8)
			BRELOP(gint64, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_R4) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR(!isunordered (f1, f2) && f1 < f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLT_R8) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR(!mono_isunordered (d1, d2) && d1 < d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLE_I4_S)
			BRELOP_S(gint32, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I8_S)
			BRELOP_S(gint64, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_R4_S) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR_S(!isunordered (f1, f2) && f1 <= f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLE_R8_S) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR_S(!mono_isunordered (d1, d2) && d1 <= d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLE_I4)
			BRELOP(gint32, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I8)
			BRELOP(gint64, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_R4) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR(!isunordered (f1, f2) && f1 <= f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLE_R8) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR(!mono_isunordered (d1, d2) && d1 <= d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BNE_UN_I4_S)
			BRELOP_S(gint32, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I8_S)
			BRELOP_S(gint64, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_R4_S) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR_S(isunordered (f1, f2) || f1 != f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BNE_UN_R8_S) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR_S(mono_isunordered (d1, d2) || d1 != d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BNE_UN_I4)
			BRELOP(gint32, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I8)
			BRELOP(gint64, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_R4) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR(isunordered (f1, f2) || f1 != f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BNE_UN_R8) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR(mono_isunordered (d1, d2) || d1 != d2)
			MINT_IN_BREAK;
		}

#define BRELOP_S_CAST(datatype, op) \
	if (LOCAL_VAR (ip [1], datatype) op LOCAL_VAR (ip [2], datatype)) { \
		gint16 br_offset = (gint16) ip [3]; \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 4;

#define BRELOP_CAST(datatype, op) \
	if (LOCAL_VAR (ip [1], datatype) op LOCAL_VAR (ip [2], datatype)) { \
		gint32 br_offset = (gint32)READ32(ip + 3); \
		BACK_BRANCH_PROFILE (br_offset); \
		ip += br_offset; \
	} else \
		ip += 5;

		MINT_IN_CASE(MINT_BGE_UN_I4_S)
			BRELOP_S_CAST(guint32, >=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I8_S)
			BRELOP_S_CAST(guint64, >=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_R4_S) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR_S(isunordered (f1, f2) || f1 >= f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGE_UN_R8_S) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR_S(mono_isunordered (d1, d2) || d1 >= d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGE_UN_I4)
			BRELOP_CAST(guint32, >=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I8)
			BRELOP_CAST(guint64, >=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_R4) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR(isunordered (f1, f2) || f1 >= f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGE_UN_R8) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR(mono_isunordered (d1, d2) || d1 >= d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGT_UN_I4_S)
			BRELOP_S_CAST(guint32, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I8_S)
			BRELOP_S_CAST(guint64, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_R4_S) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR_S(isunordered (f1, f2) || f1 > f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGT_UN_R8_S) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR_S(mono_isunordered (d1, d2) || d1 > d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGT_UN_I4)
			BRELOP_CAST(guint32, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I8)
			BRELOP_CAST(guint64, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_R4) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR(isunordered (f1, f2) || f1 > f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BGT_UN_R8) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR(mono_isunordered (d1, d2) || d1 > d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLE_UN_I4_S)
			BRELOP_S_CAST(guint32, <=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I8_S)
			BRELOP_S_CAST(guint64, <=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_R4_S) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR_S(isunordered (f1, f2) || f1 <= f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLE_UN_R8_S) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR_S(mono_isunordered (d1, d2) || d1 <= d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLE_UN_I4)
			BRELOP_CAST(guint32, <=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I8)
			BRELOP_CAST(guint64, <=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_R4) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR(isunordered (f1, f2) || f1 <= f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLE_UN_R8) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR(mono_isunordered (d1, d2) || d1 <= d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLT_UN_I4_S)
			BRELOP_S_CAST(guint32, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I8_S)
			BRELOP_S_CAST(guint64, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_R4_S) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR_S(isunordered (f1, f2) || f1 < f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLT_UN_R8_S) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR_S(mono_isunordered (d1, d2) || d1 < d2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLT_UN_I4)
			BRELOP_CAST(guint32, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I8)
			BRELOP_CAST(guint64, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_R4) {
			float f1 = LOCAL_VAR (ip [1], float);
			float f2 = LOCAL_VAR (ip [2], float);
			CONDBR(isunordered (f1, f2) || f1 < f2)
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BLT_UN_R8) {
			double d1 = LOCAL_VAR (ip [1], double);
			double d2 = LOCAL_VAR (ip [2], double);
			CONDBR(mono_isunordered (d1, d2) || d1 < d2)
			MINT_IN_BREAK;
		}

#define ZEROP_SP(datatype, op) \
	if (LOCAL_VAR (ip [1], datatype) op 0) { \
		gint16 br_offset = (gint16) ip [2]; \
		BACK_BRANCH_PROFILE (br_offset); \
		SAFEPOINT; \
		ip += br_offset; \
	} else \
		ip += 3;

MINT_IN_CASE(MINT_BRFALSE_I4_SP) ZEROP_SP(gint32, ==); MINT_IN_BREAK;
MINT_IN_CASE(MINT_BRFALSE_I8_SP) ZEROP_SP(gint64, ==); MINT_IN_BREAK;
MINT_IN_CASE(MINT_BRTRUE_I4_SP) ZEROP_SP(gint32, !=); MINT_IN_BREAK;
MINT_IN_CASE(MINT_BRTRUE_I8_SP) ZEROP_SP(gint64, !=); MINT_IN_BREAK;

#define CONDBR_SP(cond) \
	if (cond) { \
		gint16 br_offset = (gint16) ip [3]; \
		BACK_BRANCH_PROFILE (br_offset); \
		SAFEPOINT; \
		ip += br_offset; \
	} else \
		ip += 4;
#define BRELOP_SP(datatype, op) \
	CONDBR_SP(LOCAL_VAR (ip [1], datatype) op LOCAL_VAR (ip [2], datatype))

		MINT_IN_CASE(MINT_BEQ_I4_SP) BRELOP_SP(gint32, ==); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_I8_SP) BRELOP_SP(gint64, ==); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I4_SP) BRELOP_SP(gint32, >=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I8_SP) BRELOP_SP(gint64, >=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I4_SP) BRELOP_SP(gint32, >); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I8_SP) BRELOP_SP(gint64, >); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I4_SP) BRELOP_SP(gint32, <); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I8_SP) BRELOP_SP(gint64, <); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I4_SP) BRELOP_SP(gint32, <=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I8_SP) BRELOP_SP(gint64, <=); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_BNE_UN_I4_SP) BRELOP_SP(guint32, !=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I8_SP) BRELOP_SP(guint64, !=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I4_SP) BRELOP_SP(guint32, >=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I8_SP) BRELOP_SP(guint64, >=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I4_SP) BRELOP_SP(guint32, >); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I8_SP) BRELOP_SP(guint64, >); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I4_SP) BRELOP_SP(guint32, <=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I8_SP) BRELOP_SP(guint64, <=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I4_SP) BRELOP_SP(guint32, <); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I8_SP) BRELOP_SP(guint64, <); MINT_IN_BREAK;

#define BRELOP_IMM_SP(datatype, op) \
	CONDBR_SP(LOCAL_VAR (ip [1], datatype) op (datatype)(gint16)ip [2])

		MINT_IN_CASE(MINT_BEQ_I4_IMM_SP) BRELOP_IMM_SP(gint32, ==); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_I8_IMM_SP) BRELOP_IMM_SP(gint64, ==); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I4_IMM_SP) BRELOP_IMM_SP(gint32, >=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I8_IMM_SP) BRELOP_IMM_SP(gint64, >=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I4_IMM_SP) BRELOP_IMM_SP(gint32, >); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I8_IMM_SP) BRELOP_IMM_SP(gint64, >); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I4_IMM_SP) BRELOP_IMM_SP(gint32, <); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I8_IMM_SP) BRELOP_IMM_SP(gint64, <); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I4_IMM_SP) BRELOP_IMM_SP(gint32, <=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I8_IMM_SP) BRELOP_IMM_SP(gint64, <=); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_BNE_UN_I4_IMM_SP) BRELOP_IMM_SP(guint32, !=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I8_IMM_SP) BRELOP_IMM_SP(guint64, !=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I4_IMM_SP) BRELOP_IMM_SP(guint32, >=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I8_IMM_SP) BRELOP_IMM_SP(guint64, >=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I4_IMM_SP) BRELOP_IMM_SP(guint32, >); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I8_IMM_SP) BRELOP_IMM_SP(guint64, >); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I4_IMM_SP) BRELOP_IMM_SP(guint32, <=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I8_IMM_SP) BRELOP_IMM_SP(guint64, <=); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I4_IMM_SP) BRELOP_IMM_SP(guint32, <); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I8_IMM_SP) BRELOP_IMM_SP(guint64, <); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_SWITCH) {
			guint32 val = LOCAL_VAR (ip [1], guint32);
			guint32 n = READ32 (ip + 2);
			ip += 4;
			if (val < n) {
				ip += 2 * val;
				int offset = READ32 (ip);
				ip += offset;
			} else {
				ip += 2 * n;
			}
			MINT_IN_BREAK;
		}
#define LDIND(datatype,casttype,unaligned) do { \
	gpointer ptr = LOCAL_VAR (ip [2], gpointer); \
	NULL_CHECK (ptr); \
	if (unaligned && ((gsize)ptr % SIZEOF_VOID_P)) \
		memcpy (locals + ip [1], ptr, sizeof (datatype)); \
	else \
		LOCAL_VAR (ip [1], datatype) = *(casttype*)ptr; \
	ip += 3; \
} while (0)
		MINT_IN_CASE(MINT_LDIND_I1)
			LDIND(int, gint8, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_U1)
			LDIND(int, guint8, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_I2)
			LDIND(int, gint16, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_U2)
			LDIND(int, guint16, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_I4) {
			LDIND(int, gint32, FALSE);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDIND_I8)
#ifdef NO_UNALIGNED_ACCESS
			LDIND(gint64, gint64, TRUE);
#else
			LDIND(gint64, gint64, FALSE);
#endif
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_R4)
			LDIND(float, gfloat, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_R8)
#ifdef NO_UNALIGNED_ACCESS
			LDIND(double, gdouble, TRUE);
#else
			LDIND(double, gdouble, FALSE);
#endif
			MINT_IN_BREAK;

#define LDIND_OFFSET(datatype,casttype,unaligned) do { \
	gpointer ptr = LOCAL_VAR (ip [2], gpointer); \
	NULL_CHECK (ptr); \
	ptr = (char*)ptr + LOCAL_VAR (ip [3], mono_i); \
	if (unaligned && ((gsize)ptr % SIZEOF_VOID_P)) \
		memcpy (locals + ip [1], ptr, sizeof (datatype)); \
	else \
		LOCAL_VAR (ip [1], datatype) = *(casttype*)ptr; \
	ip += 4; \
} while (0)
		MINT_IN_CASE(MINT_LDIND_OFFSET_I1)
			LDIND_OFFSET(int, gint8, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_OFFSET_U1)
			LDIND_OFFSET(int, guint8, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_OFFSET_I2)
			LDIND_OFFSET(int, gint16, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_OFFSET_U2)
			LDIND_OFFSET(int, guint16, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_OFFSET_I4)
			LDIND_OFFSET(int, gint32, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_OFFSET_I8)
#ifdef NO_UNALIGNED_ACCESS
			LDIND_OFFSET(gint64, gint64, TRUE);
#else
			LDIND_OFFSET(gint64, gint64, FALSE);
#endif
			MINT_IN_BREAK;

#define LDIND_OFFSET_IMM(datatype,casttype,unaligned) do { \
	gpointer ptr = LOCAL_VAR (ip [2], gpointer); \
	NULL_CHECK (ptr); \
	ptr = (char*)ptr + (gint16)ip [3]; \
	if (unaligned && ((gsize)ptr % SIZEOF_VOID_P)) \
		memcpy (locals + ip [1], ptr, sizeof (datatype)); \
	else \
		LOCAL_VAR (ip [1], datatype) = *(casttype*)ptr; \
	ip += 4; \
} while (0)
		MINT_IN_CASE(MINT_LDIND_OFFSET_IMM_I1)
			LDIND_OFFSET_IMM(int, gint8, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_OFFSET_IMM_U1)
			LDIND_OFFSET_IMM(int, guint8, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_OFFSET_IMM_I2)
			LDIND_OFFSET_IMM(int, gint16, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_OFFSET_IMM_U2)
			LDIND_OFFSET_IMM(int, guint16, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_OFFSET_IMM_I4)
			LDIND_OFFSET_IMM(int, gint32, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_OFFSET_IMM_I8)
#ifdef NO_UNALIGNED_ACCESS
			LDIND_OFFSET_IMM(gint64, gint64, TRUE);
#else
			LDIND_OFFSET_IMM(gint64, gint64, FALSE);
#endif
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_REF) {
			gpointer ptr = LOCAL_VAR (ip [1], gpointer);
			NULL_CHECK (ptr);
			mono_gc_wbarrier_generic_store_internal (ptr, LOCAL_VAR (ip [2], MonoObject*));
			ip += 3;
			MINT_IN_BREAK;
		}
#define STIND(datatype,unaligned) do { \
	gpointer ptr = LOCAL_VAR (ip [1], gpointer); \
	NULL_CHECK (ptr); \
	if (unaligned && ((gsize)ptr % SIZEOF_VOID_P)) \
		memcpy (ptr, locals + ip [2], sizeof (datatype)); \
	else \
		*(datatype*)ptr = LOCAL_VAR (ip [2], datatype); \
	ip += 3; \
} while (0)
		MINT_IN_CASE(MINT_STIND_I1)
			STIND(gint8, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I2)
			STIND(gint16, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I4)
			STIND(gint32, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I8)
#ifdef NO_UNALIGNED_ACCESS
			STIND(gint64, TRUE);
#else
			STIND(gint64, FALSE);
#endif
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_R4)
			STIND(float, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_R8)
#ifdef NO_UNALIGNED_ACCESS
			STIND(double, TRUE);
#else
			STIND(double, FALSE);
#endif
			MINT_IN_BREAK;

#define STIND_OFFSET(datatype,unaligned) do { \
	gpointer ptr = LOCAL_VAR (ip [1], gpointer); \
	NULL_CHECK (ptr); \
	ptr = (char*)ptr + LOCAL_VAR (ip [2], mono_i); \
	if (unaligned && ((gsize)ptr % SIZEOF_VOID_P)) \
		memcpy (ptr, locals + ip [3], sizeof (datatype)); \
	else \
		*(datatype*)ptr = LOCAL_VAR (ip [3], datatype); \
	ip += 4; \
} while (0)
		MINT_IN_CASE(MINT_STIND_OFFSET_I1)
			STIND_OFFSET(gint8, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_OFFSET_I2)
			STIND_OFFSET(gint16, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_OFFSET_I4)
			STIND_OFFSET(gint32, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_OFFSET_I8)
#ifdef NO_UNALIGNED_ACCESS
			STIND_OFFSET(gint64, TRUE);
#else
			STIND_OFFSET(gint64, FALSE);
#endif
			MINT_IN_BREAK;

#define STIND_OFFSET_IMM(datatype,unaligned) do { \
	gpointer ptr = LOCAL_VAR (ip [1], gpointer); \
	NULL_CHECK (ptr); \
	ptr = (char*)ptr + (gint16)ip [3]; \
	if (unaligned && ((gsize)ptr % SIZEOF_VOID_P)) \
		memcpy (ptr, locals + ip [2], sizeof (datatype)); \
	else \
		*(datatype*)ptr = LOCAL_VAR (ip [2], datatype); \
	ip += 4; \
} while (0)
		MINT_IN_CASE(MINT_STIND_OFFSET_IMM_I1)
			STIND_OFFSET_IMM(gint8, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_OFFSET_IMM_I2)
			STIND_OFFSET_IMM(gint16, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_OFFSET_IMM_I4)
			STIND_OFFSET_IMM(gint32, FALSE);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_OFFSET_IMM_I8)
#ifdef NO_UNALIGNED_ACCESS
			STIND_OFFSET_IMM(gint64, TRUE);
#else
			STIND_OFFSET_IMM(gint64, FALSE);
#endif
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_ATOMIC_STORE_I4)
			mono_atomic_store_i32 (LOCAL_VAR (ip [1], gint32*), LOCAL_VAR (ip [2], gint32));
			ip += 3;
			MINT_IN_BREAK;
#define BINOP(datatype, op) \
	LOCAL_VAR (ip [1], datatype) = LOCAL_VAR (ip [2], datatype) op LOCAL_VAR (ip [3], datatype); \
	ip += 4;
		MINT_IN_CASE(MINT_ADD_I4)
			BINOP(gint32, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_I8)
			BINOP(gint64, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_R4)
			BINOP(float, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_R8)
			BINOP(double, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD1_I4)
			LOCAL_VAR (ip [1], gint32) = LOCAL_VAR (ip [2], gint32) + 1;
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_I4_IMM)
			LOCAL_VAR (ip [1], gint32) = LOCAL_VAR (ip [2], gint32) + (gint16)ip [3];
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD1_I8)
			LOCAL_VAR (ip [1], gint64) = LOCAL_VAR (ip [2], gint64) + 1;
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_I8_IMM)
			LOCAL_VAR (ip [1], gint64) = LOCAL_VAR (ip [2], gint64) + (gint16)ip [3];
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_I4)
			BINOP(gint32, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_I8)
			BINOP(gint64, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_R4)
			BINOP(float, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_R8)
			BINOP(double, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB1_I4)
			LOCAL_VAR (ip [1], gint32) = LOCAL_VAR (ip [2], gint32) - 1;
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB1_I8)
			LOCAL_VAR (ip [1], gint64) = LOCAL_VAR (ip [2], gint64) - 1;
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_I4)
			BINOP(gint32, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_I8)
			BINOP(gint64, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_I4_IMM)
			LOCAL_VAR (ip [1], gint32) = LOCAL_VAR (ip [2], gint32) * (gint16)ip [3];
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_I8_IMM)
			LOCAL_VAR (ip [1], gint64) = LOCAL_VAR (ip [2], gint64) * (gint16)ip [3];
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_R4)
			BINOP(float, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_R8)
			BINOP(double, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DIV_I4) {
			gint32 i1 = LOCAL_VAR (ip [2], gint32);
			gint32 i2 = LOCAL_VAR (ip [3], gint32);
			if (i2 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (i2 == (-1) && i1 == G_MININT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = i1 / i2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_DIV_I8) {
			gint64 l1 = LOCAL_VAR (ip [2], gint64);
			gint64 l2 = LOCAL_VAR (ip [3], gint64);
			if (l2 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (l2 == (-1) && l1 == G_MININT64)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = l1 / l2;
			ip += 4;
			MINT_IN_BREAK;
			}
		MINT_IN_CASE(MINT_DIV_R4)
			BINOP(float, /);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DIV_R8)
			BINOP(double, /);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DIV_UN_I4) {
			guint32 i2 = LOCAL_VAR (ip [3], guint32);
			if (i2 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			LOCAL_VAR (ip [1], guint32) = LOCAL_VAR (ip [2], guint32) / i2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_DIV_UN_I8) {
			guint64 l2 = LOCAL_VAR (ip [3], guint64);
			if (l2 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			LOCAL_VAR (ip [1], guint64) = LOCAL_VAR (ip [2], guint64) / l2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REM_I4) {
			gint32 i1 = LOCAL_VAR (ip [2], gint32);
			gint32 i2 = LOCAL_VAR (ip [3], gint32);
			if (i2 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (i2 == (-1) && i1 == G_MININT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = i1 % i2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REM_I8) {
			gint64 l1 = LOCAL_VAR (ip [2], gint64);
			gint64 l2 = LOCAL_VAR (ip [3], gint64);
			if (l2 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (l2 == (-1) && l1 == G_MININT64)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = l1 % l2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REM_R4)
			LOCAL_VAR (ip [1], float) = fmodf (LOCAL_VAR (ip [2], float), LOCAL_VAR (ip [3], float));
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_REM_R8)
			LOCAL_VAR (ip [1], double) = fmod (LOCAL_VAR (ip [2], double), LOCAL_VAR (ip [3], double));
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_REM_UN_I4) {
			guint32 i2 = LOCAL_VAR (ip [3], guint32);
			if (i2 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			LOCAL_VAR (ip [1], guint32) = LOCAL_VAR (ip [2], guint32) % i2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REM_UN_I8) {
			guint64 l2 = LOCAL_VAR (ip [3], guint64);
			if (l2 == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			LOCAL_VAR (ip [1], guint64) = LOCAL_VAR (ip [2], guint64) % l2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_AND_I4)
			BINOP(gint32, &);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_AND_I8)
			BINOP(gint64, &);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_OR_I4)
			BINOP(gint32, |);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_OR_I8)
			BINOP(gint64, |);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_XOR_I4)
			BINOP(gint32, ^);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_XOR_I8)
			BINOP(gint64, ^);
			MINT_IN_BREAK;

#define SHIFTOP(datatype, op) \
	LOCAL_VAR (ip [1], datatype) = LOCAL_VAR (ip [2], datatype) op LOCAL_VAR (ip [3], gint32); \
	ip += 4;

		MINT_IN_CASE(MINT_SHL_I4)
			SHIFTOP(gint32, <<);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHL_I8)
			SHIFTOP(gint64, <<);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_I4)
			SHIFTOP(gint32, >>);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_I8)
			SHIFTOP(gint64, >>);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_UN_I4)
			SHIFTOP(guint32, >>);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_UN_I8)
			SHIFTOP(guint64, >>);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHL_I4_IMM)
			LOCAL_VAR (ip [1], gint32) = LOCAL_VAR (ip [2], gint32) << ip [3];
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHL_I8_IMM)
			LOCAL_VAR (ip [1], gint64) = LOCAL_VAR (ip [2], gint64) << ip [3];
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_I4_IMM)
			LOCAL_VAR (ip [1], gint32) = LOCAL_VAR (ip [2], gint32) >> ip [3];
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_I8_IMM)
			LOCAL_VAR (ip [1], gint64) = LOCAL_VAR (ip [2], gint64) >> ip [3];
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_UN_I4_IMM)
			LOCAL_VAR (ip [1], guint32) = LOCAL_VAR (ip [2], guint32) >> ip [3];
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SHR_UN_I8_IMM)
			LOCAL_VAR (ip [1], guint64) = LOCAL_VAR (ip [2], guint64) >> ip [3];
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NEG_I4)
			LOCAL_VAR (ip [1], gint32) = - LOCAL_VAR (ip [2], gint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NEG_I8)
			LOCAL_VAR (ip [1], gint64) = - LOCAL_VAR (ip [2], gint64);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NEG_R4)
			LOCAL_VAR (ip [1], float) = - LOCAL_VAR (ip [2], float);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NEG_R8)
			LOCAL_VAR (ip [1], double) = - LOCAL_VAR (ip [2], double);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NOT_I4)
			LOCAL_VAR (ip [1], gint32) = ~ LOCAL_VAR (ip [2], gint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NOT_I8)
			LOCAL_VAR (ip [1], gint64) = ~ LOCAL_VAR (ip [2], gint64);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I1_I4)
			// FIXME read casted var directly and remove redundant conv opcodes
			LOCAL_VAR (ip [1], gint32) = (gint8)LOCAL_VAR (ip [2], gint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I1_I8)
			LOCAL_VAR (ip [1], gint32) = (gint8)LOCAL_VAR (ip [2], gint64);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I1_R4)
			LOCAL_VAR (ip [1], gint32) = (gint8) (gint32) LOCAL_VAR (ip [2], float);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I1_R8)
			/* without gint32 cast, C compiler is allowed to use undefined
			 * behaviour if data.f is bigger than >255. See conv.fpint section
			 * in C standard:
			 * > The conversion truncates; that is, the fractional  part
			 * > is discarded.  The behavior is undefined if the truncated
			 * > value cannot be represented in the destination type.
			 * */
			LOCAL_VAR (ip [1], gint32) = (gint8) (gint32) LOCAL_VAR (ip [2], double);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U1_I4)
			LOCAL_VAR (ip [1], gint32) = (guint8) LOCAL_VAR (ip [2], gint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U1_I8)
			LOCAL_VAR (ip [1], gint32) = (guint8) LOCAL_VAR (ip [2], gint64);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U1_R4)
			LOCAL_VAR (ip [1], gint32) = (guint8) (guint32) LOCAL_VAR (ip [2], float);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U1_R8)
			LOCAL_VAR (ip [1], gint32) = (guint8) (guint32) LOCAL_VAR (ip [2], double);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I2_I4)
			LOCAL_VAR (ip [1], gint32) = (gint16) LOCAL_VAR (ip [2], gint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I2_I8)
			LOCAL_VAR (ip [1], gint32) = (gint16) LOCAL_VAR (ip [2], gint64);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I2_R4)
			LOCAL_VAR (ip [1], gint32) = (gint16) (gint32) LOCAL_VAR (ip [2], float);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I2_R8)
			LOCAL_VAR (ip [1], gint32) = (gint16) (gint32) LOCAL_VAR (ip [2], double);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U2_I4)
			LOCAL_VAR (ip [1], gint32) = (guint16) LOCAL_VAR (ip [2], gint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U2_I8)
			LOCAL_VAR (ip [1], gint32) = (guint16) LOCAL_VAR (ip [2], gint64);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U2_R4)
			LOCAL_VAR (ip [1], gint32) = (guint16) (guint32) LOCAL_VAR (ip [2], float);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U2_R8)
			LOCAL_VAR (ip [1], gint32) = (guint16) (guint32) LOCAL_VAR (ip [2], double);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I4_R4)
			LOCAL_VAR (ip [1], gint32) = (gint32) LOCAL_VAR (ip [2], float);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I4_R8)
			LOCAL_VAR (ip [1], gint32) = (gint32) LOCAL_VAR (ip [2], double);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U4_R4)
#ifdef MONO_ARCH_EMULATE_FCONV_TO_U4
			LOCAL_VAR (ip [1], gint32) = mono_rconv_u4 (LOCAL_VAR (ip [2], float));
#else
			LOCAL_VAR (ip [1], gint32) = (guint32) LOCAL_VAR (ip [2], float);
#endif
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U4_R8)
#ifdef MONO_ARCH_EMULATE_FCONV_TO_U4
			LOCAL_VAR (ip [1], gint32) = mono_fconv_u4_2 (LOCAL_VAR (ip [2], double));
#else
			LOCAL_VAR (ip [1], gint32) = (guint32) LOCAL_VAR (ip [2], double);
#endif
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I8_I4)
			LOCAL_VAR (ip [1], gint64) = LOCAL_VAR (ip [2], gint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I8_U4)
			LOCAL_VAR (ip [1], gint64) = (guint32) LOCAL_VAR (ip [2], gint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I8_R4)
			LOCAL_VAR (ip [1], gint64) = (gint64) LOCAL_VAR (ip [2], float);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_I8_R8)
			LOCAL_VAR (ip [1], gint64) = (gint64) LOCAL_VAR (ip [2], double);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R4_I4)
			LOCAL_VAR (ip [1], float) = (float) LOCAL_VAR (ip [2], gint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R4_I8)
			LOCAL_VAR (ip [1], float) = (float) LOCAL_VAR (ip [2], gint64);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R4_R8)
			LOCAL_VAR (ip [1], float) = (float) LOCAL_VAR (ip [2], double);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R8_I4)
			LOCAL_VAR (ip [1], double) = (double) LOCAL_VAR (ip [2], gint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R8_I8)
			LOCAL_VAR (ip [1], double) = (double) LOCAL_VAR (ip [2], gint64);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R8_R4)
			LOCAL_VAR (ip [1], double) = (double) LOCAL_VAR (ip [2], float);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U8_R4)
#ifdef MONO_ARCH_EMULATE_FCONV_TO_U8
			LOCAL_VAR (ip [1], gint64) = mono_rconv_u8 (LOCAL_VAR (ip [2], float));
#else
			LOCAL_VAR (ip [1], gint64) = (guint64) LOCAL_VAR (ip [2], float);
#endif
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U8_R8)
#ifdef MONO_ARCH_EMULATE_FCONV_TO_U8
			LOCAL_VAR (ip [1], gint64) = mono_fconv_u8_2 (LOCAL_VAR (ip [2], double));
#else
			LOCAL_VAR (ip [1], gint64) = (guint64) LOCAL_VAR (ip [2], double);
#endif
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CPOBJ) {
			MonoClass* const c = (MonoClass*)frame->imethod->data_items[ip [3]];
			g_assert (m_class_is_valuetype (c));
			/* if this assertion fails, we need to add a write barrier */
			g_assert (!MONO_TYPE_IS_REFERENCE (m_class_get_byval_arg (c)));
			stackval_from_data (m_class_get_byval_arg (c), (stackval*)LOCAL_VAR (ip [1], gpointer), LOCAL_VAR (ip [2], gpointer), FALSE);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CPOBJ_VT) {
			MonoClass* const c = (MonoClass*)frame->imethod->data_items[ip [3]];
			mono_value_copy_internal (LOCAL_VAR (ip [1], gpointer), LOCAL_VAR (ip [2], gpointer), c);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDOBJ_VT) {
			guint16 size = ip [3];
			memcpy (locals + ip [1], LOCAL_VAR (ip [2], gpointer), size);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSTR)
			LOCAL_VAR (ip [1], gpointer) = frame->imethod->data_items [ip [2]];
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSTR_TOKEN) {
			MonoString *s = NULL;
			guint32 strtoken = (guint32)(gsize)frame->imethod->data_items [ip [2]];

			MonoMethod *method = frame->imethod->method;
			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD) {
				s = (MonoString*)mono_method_get_wrapper_data (method, strtoken);
			} else if (method->wrapper_type != MONO_WRAPPER_NONE) {
				s = mono_string_new_wrapper_internal ((const char*)mono_method_get_wrapper_data (method, strtoken));
			} else {
				g_assert_not_reached ();
			}
			LOCAL_VAR (ip [1], gpointer) = s;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_NEWOBJ_ARRAY) {
			MonoClass *newobj_class;
			guint32 token = ip [3];
			guint16 param_count = ip [4];

			newobj_class = (MonoClass*) frame->imethod->data_items [token];

			LOCAL_VAR (ip [1], MonoObject*) = ves_array_create (newobj_class, param_count, (stackval*)(locals + ip [2]), error);
			if (!is_ok (error))
				THROW_EX (mono_error_convert_to_exception (error), ip);
			ip += 5;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_NEWOBJ_STRING) {
			cmethod = (InterpMethod*)frame->imethod->data_items [ip [3]];
			return_offset = ip [1];
			call_args_offset = ip [2];

			// `this` is implicit null. The created string will be returned
			// by the call, even though the call has void return (?!).
			LOCAL_VAR (call_args_offset, gpointer) = NULL;
			ip += 4;
			goto call;
		}
		MINT_IN_CASE(MINT_NEWOBJ) {
			MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [4]];
			INIT_VTABLE (vtable);
			guint16 imethod_index = ip [3];
			return_offset = ip [1];
			call_args_offset = ip [2];

			MonoObject *o = mono_gc_alloc_obj (vtable, m_class_get_instance_size (vtable->klass));
			if (G_UNLIKELY (!o)) {
				mono_error_set_out_of_memory (error, "Could not allocate %i bytes", m_class_get_instance_size (vtable->klass));
				THROW_EX (mono_error_convert_to_exception (error), ip);
			}

			// This is return value
			LOCAL_VAR (return_offset, MonoObject*) = o;
			// Set `this` arg for ctor call
			LOCAL_VAR (call_args_offset, MonoObject*) = o;
			ip += 5;

			cmethod = (InterpMethod*)frame->imethod->data_items [imethod_index];
			goto call;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_NEWOBJ_INLINED) {
			MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [2]];
			INIT_VTABLE (vtable);

			MonoObject *o = mono_gc_alloc_obj (vtable, m_class_get_instance_size (vtable->klass));
			if (G_UNLIKELY (!o)) {
				mono_error_set_out_of_memory (error, "Could not allocate %i bytes", m_class_get_instance_size (vtable->klass));
				THROW_EX (mono_error_convert_to_exception (error), ip);
			}

			// This is return value
			LOCAL_VAR (ip [1], MonoObject*) = o;
			ip += 3;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_NEWOBJ_VT) {
			guint16 imethod_index = ip [3];
			guint16 ret_size = ip [4];
			return_offset = ip [1];
			call_args_offset = ip [2];
			gpointer this_vt = locals + return_offset;

			// clear the valuetype
			memset (this_vt, 0, ret_size);
			// pass the address of the valuetype
			LOCAL_VAR (call_args_offset, gpointer) = this_vt;
			ip += 5;

			cmethod = (InterpMethod*)frame->imethod->data_items [imethod_index];
			goto call;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_NEWOBJ_VT_INLINED) {
			guint16 ret_size = ip [3];
			gpointer this_vt = locals + ip [2];

			memset (this_vt, 0, ret_size);
			LOCAL_VAR (ip [1], gpointer) = this_vt;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_NEWOBJ_SLOW) {
			guint32 const token = ip [3];
			return_offset = ip [1];
			call_args_offset = ip [2];

			cmethod = (InterpMethod*)frame->imethod->data_items [token];

			MonoClass * const newobj_class = cmethod->method->klass;

			/*
			 * First arg is the object.
			 * a constructor returns void, but we need to return the object we created
			 */

			g_assert (!m_class_is_valuetype (newobj_class));

			MonoVTable *vtable = mono_class_vtable_checked (newobj_class, error);
			if (!is_ok (error) || !mono_runtime_class_init_full (vtable, error)) {
				MonoException *exc = mono_error_convert_to_exception (error);
				g_assert (exc);
				THROW_EX (exc, ip);
			}
			error_init_reuse (error);
			MonoObject* o = mono_object_new_checked (newobj_class, error);
			LOCAL_VAR (return_offset, MonoObject*) = o; // return value
			LOCAL_VAR (call_args_offset, MonoObject*) = o; // first parameter

			mono_interp_error_cleanup (error); // FIXME: do not swallow the error
			EXCEPTION_CHECKPOINT;
			ip += 4;
			goto call;
		}
		MINT_IN_CASE(MINT_INTRINS_SPAN_CTOR) {
			gpointer ptr = LOCAL_VAR (ip [2], gpointer);
			int len = LOCAL_VAR (ip [3], gint32);
			if (len < 0)
				THROW_EX (mono_get_exception_argument_out_of_range ("length"), ip);
			gpointer span = locals + ip [1];
			*(gpointer*)span = ptr;
			*(gint32*)((gpointer*)span + 1) = len;
			ip += 4;;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_CLEAR_WITH_REFERENCES) {
			gpointer p = LOCAL_VAR (ip [1], gpointer);
			size_t size = LOCAL_VAR (ip [2], mono_u) * sizeof (gpointer);
			mono_gc_bzero_aligned (p, size);
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_MARVIN_BLOCK) {
			interp_intrins_marvin_block ((guint32*)(locals + ip [1]), (guint32*)(locals + ip [2]));
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_ASCII_CHARS_TO_UPPERCASE) {
			LOCAL_VAR (ip [1], gint32) = interp_intrins_ascii_chars_to_uppercase (LOCAL_VAR (ip [2], guint32));
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_MEMORYMARSHAL_GETARRAYDATAREF) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			LOCAL_VAR (ip [1], gpointer) = (guint8*)o + MONO_STRUCT_OFFSET (MonoArray, vector);
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_ORDINAL_IGNORE_CASE_ASCII) {
			LOCAL_VAR (ip [1], gint32) = interp_intrins_ordinal_ignore_case_ascii (LOCAL_VAR (ip [2], guint32), LOCAL_VAR (ip [3], guint32));
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_64ORDINAL_IGNORE_CASE_ASCII) {
			LOCAL_VAR (ip [1], gint32) = interp_intrins_64ordinal_ignore_case_ascii (LOCAL_VAR (ip [2], guint64), LOCAL_VAR (ip [3], guint64));
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_U32_TO_DECSTR) {
			MonoArray **cache_addr = (MonoArray**)frame->imethod->data_items [ip [3]];
			MonoVTable *string_vtable = (MonoVTable*)frame->imethod->data_items [ip [4]];
			LOCAL_VAR (ip [1], MonoObject*) = (MonoObject*)interp_intrins_u32_to_decstr (LOCAL_VAR (ip [2], guint32), *cache_addr, string_vtable);
			ip += 5;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_WIDEN_ASCII_TO_UTF16) {
			LOCAL_VAR (ip [1], mono_u) = interp_intrins_widen_ascii_to_utf16 (LOCAL_VAR (ip [2], guint8*), LOCAL_VAR (ip [3], mono_unichar2*), LOCAL_VAR (ip [4], mono_u));
			ip += 5;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_UNSAFE_BYTE_OFFSET) {
			LOCAL_VAR (ip [1], mono_u) = LOCAL_VAR (ip [3], guint8*) - LOCAL_VAR (ip [2], guint8*);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_RUNTIMEHELPERS_OBJECT_HAS_COMPONENT_SIZE) {
			MonoObject *obj = LOCAL_VAR (ip [2], MonoObject*);
			LOCAL_VAR (ip [1], gint32) = (obj->vtable->flags & MONO_VT_FLAG_ARRAY_OR_STRING) != 0;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CASTCLASS_INTERFACE)
		MINT_IN_CASE(MINT_ISINST_INTERFACE) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			if (o) {
				MonoClass *c = (MonoClass*)frame->imethod->data_items [ip [3]];
				gboolean isinst;
				if (MONO_VTABLE_IMPLEMENTS_INTERFACE (o->vtable, m_class_get_interface_id (c))) {
					isinst = TRUE;
				} else if (m_class_is_array_special_interface (c)) {
					/* slow path */
					isinst = mono_interp_isinst (o, c); // FIXME: do not swallow the error
				} else {
					isinst = FALSE;
				}

				if (!isinst) {
					gboolean const isinst_instr = *ip == MINT_ISINST_INTERFACE;
					if (isinst_instr)
						LOCAL_VAR (ip [1], MonoObject*) = NULL;
					else
						THROW_EX (mono_get_exception_invalid_cast (), ip);
				} else {
					LOCAL_VAR (ip [1], MonoObject*) = o;
				}
			} else {
				LOCAL_VAR (ip [1], MonoObject*) = NULL;
			}
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CASTCLASS_COMMON)
		MINT_IN_CASE(MINT_ISINST_COMMON) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			if (o) {
				MonoClass *c = (MonoClass*)frame->imethod->data_items [ip [3]];
				gboolean isinst = mono_class_has_parent_fast (o->vtable->klass, c);

				if (!isinst) {
					gboolean const isinst_instr = *ip == MINT_ISINST_COMMON;
					if (isinst_instr)
						LOCAL_VAR (ip [1], MonoObject*) = NULL;
					else
						THROW_EX (mono_get_exception_invalid_cast (), ip);
				} else {
					LOCAL_VAR (ip [1], MonoObject*) = o;
				}
			} else {
				LOCAL_VAR (ip [1], MonoObject*) = NULL;
			}
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CASTCLASS)
		MINT_IN_CASE(MINT_ISINST) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			if (o) {
				MonoClass* const c = (MonoClass*)frame->imethod->data_items [ip [3]];
				if (!mono_interp_isinst (o, c)) { // FIXME: do not swallow the error
					gboolean const isinst_instr = *ip == MINT_ISINST;
					if (isinst_instr)
						LOCAL_VAR (ip [1], MonoObject*) = NULL;
					else
						THROW_EX (mono_get_exception_invalid_cast (), ip);
				} else {
					LOCAL_VAR (ip [1], MonoObject*) = o;
				}
			} else {
				LOCAL_VAR (ip [1], MonoObject*) = NULL;
			}
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_R_UN_I4)
			LOCAL_VAR (ip [1], double) = (double)LOCAL_VAR (ip [2], guint32);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R_UN_I8)
			LOCAL_VAR (ip [1], double) = (double)LOCAL_VAR (ip [2], guint64);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_UNBOX) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			MonoClass *c = (MonoClass*)frame->imethod->data_items [ip [3]];

			if (!(m_class_get_rank (o->vtable->klass) == 0 && m_class_get_element_class (o->vtable->klass) == m_class_get_element_class (c)))
				THROW_EX (mono_get_exception_invalid_cast (), ip);

			LOCAL_VAR (ip [1], gpointer) = mono_object_unbox_internal (o);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_THROW) {
			MonoException *ex = LOCAL_VAR (ip [1], MonoException*);
			if (!ex)
				ex = mono_get_exception_null_reference ();

			THROW_EX (ex, ip);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_SAFEPOINT)
			SAFEPOINT;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLDA_UNSAFE) {
			LOCAL_VAR (ip [1], gpointer) = (char*)LOCAL_VAR (ip [2], gpointer) + ip [3];
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDFLDA) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			LOCAL_VAR (ip [1], gpointer) = (char *)o + ip [3];
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CKNULL) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			LOCAL_VAR (ip [1], MonoObject*) = o;
			ip += 3;
			MINT_IN_BREAK;
		}

#define LDFLD_UNALIGNED(datatype, fieldtype, unaligned) do { \
	MonoObject *o = LOCAL_VAR (ip [2], MonoObject*); \
	NULL_CHECK (o); \
	if (unaligned) \
		memcpy (locals + ip [1], (char *)o + ip [3], sizeof (fieldtype)); \
	else \
		LOCAL_VAR (ip [1], datatype) = * (fieldtype *)((char *)o + ip [3]) ; \
	ip += 4; \
} while (0)

#define LDFLD(datamem, fieldtype) LDFLD_UNALIGNED(datamem, fieldtype, FALSE)

		MINT_IN_CASE(MINT_LDFLD_I1) LDFLD(gint32, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_U1) LDFLD(gint32, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I2) LDFLD(gint32, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_U2) LDFLD(gint32, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I4) LDFLD(gint32, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I8) LDFLD(gint64, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_R4) LDFLD(float, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_R8) LDFLD(double, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_O) LDFLD(gpointer, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I8_UNALIGNED) LDFLD_UNALIGNED(gint64, gint64, TRUE); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_R8_UNALIGNED) LDFLD_UNALIGNED(double, double, TRUE); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDFLD_VT) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			memcpy (locals + ip [1], (char *)o + ip [3], ip [4]);
			ip += 5;
			MINT_IN_BREAK;
		}

#define STFLD_UNALIGNED(datatype, fieldtype, unaligned) do { \
	MonoObject *o = LOCAL_VAR (ip [1], MonoObject*); \
	NULL_CHECK (o); \
	if (unaligned) \
		memcpy ((char *)o + ip [3], locals + ip [2], sizeof (fieldtype)); \
	else \
		* (fieldtype *)((char *)o + ip [3]) = LOCAL_VAR (ip [2], datatype); \
	ip += 4; \
} while (0)

#define STFLD(datamem, fieldtype) STFLD_UNALIGNED(datamem, fieldtype, FALSE)

		MINT_IN_CASE(MINT_STFLD_I1) STFLD(gint32, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_U1) STFLD(gint32, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_I2) STFLD(gint32, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_U2) STFLD(gint32, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_I4) STFLD(gint32, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_I8) STFLD(gint64, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_R4) STFLD(float, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_R8) STFLD(double, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_O) {
			MonoObject *o = LOCAL_VAR (ip [1], MonoObject*);
			NULL_CHECK (o);
			mono_gc_wbarrier_set_field_internal (o, (char*)o + ip [3], LOCAL_VAR (ip [2], MonoObject*));
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STFLD_I8_UNALIGNED) STFLD_UNALIGNED(gint64, gint64, TRUE); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_R8_UNALIGNED) STFLD_UNALIGNED(double, double, TRUE); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STFLD_VT_NOREF) {
			MonoObject *o = LOCAL_VAR (ip [1], MonoObject*);
			NULL_CHECK (o);
			memcpy ((char*)o + ip [3], locals + ip [2], ip [4]);
			ip += 5;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_STFLD_VT) {
			MonoClass *klass = (MonoClass*)frame->imethod->data_items [ip [4]];
			MonoObject *o = LOCAL_VAR (ip [1], MonoObject*);
			NULL_CHECK (o);
			mono_value_copy_internal ((char*)o + ip [3], locals + ip [2], klass);
			ip += 5;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LDSFLDA) {
			MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [2]];
			INIT_VTABLE (vtable);
			LOCAL_VAR (ip [1], gpointer) = frame->imethod->data_items [ip [3]];
			ip += 4;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LDTSFLDA) {
			MonoInternalThread *thread = mono_thread_internal_current ();
			guint32 offset = READ32 (ip + 2);
			LOCAL_VAR (ip [1], gpointer) = ((char*)thread->static_data [offset & 0x3f]) + (offset >> 6);
			ip += 4;
			MINT_IN_BREAK;
		}

/* We init class here to preserve cctor order */
#define LDSFLD(datatype, fieldtype) { \
	MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [2]]; \
	INIT_VTABLE (vtable); \
	LOCAL_VAR (ip [1], datatype) = * (fieldtype *)(frame->imethod->data_items [ip [3]]) ; \
	ip += 4; \
	}

		MINT_IN_CASE(MINT_LDSFLD_I1) LDSFLD(gint32, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_U1) LDSFLD(gint32, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_I2) LDSFLD(gint32, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_U2) LDSFLD(gint32, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_I4) LDSFLD(gint32, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_I8) LDSFLD(gint64, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_R4) LDSFLD(float, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_R8) LDSFLD(double, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDSFLD_O) LDSFLD(gpointer, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDSFLD_VT) {
			MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [2]];
			INIT_VTABLE (vtable);

			gpointer addr = frame->imethod->data_items [ip [3]];
			guint16 size = ip [4];

			memcpy (locals + ip [1], addr, size);
			ip += 5;
			MINT_IN_BREAK;
		}

#define STSFLD(datatype, fieldtype) { \
	MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [2]]; \
	INIT_VTABLE (vtable); \
	* (fieldtype *)(frame->imethod->data_items [ip [3]]) = LOCAL_VAR (ip [1], datatype); \
	ip += 4; \
	}

		MINT_IN_CASE(MINT_STSFLD_I1) STSFLD(gint32, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_U1) STSFLD(gint32, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_I2) STSFLD(gint32, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_U2) STSFLD(gint32, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_I4) STSFLD(gint32, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_I8) STSFLD(gint64, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_R4) STSFLD(float, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_R8) STSFLD(double, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STSFLD_O) STSFLD(gpointer, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STSFLD_VT) {
			MonoVTable *vtable = (MonoVTable*) frame->imethod->data_items [ip [2]];
			INIT_VTABLE (vtable);
			gpointer addr = frame->imethod->data_items [ip [3]];
			memcpy (addr, locals + ip [1], ip [4]);
			ip += 5;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_STOBJ_VT) {
			MonoClass *c = (MonoClass*)frame->imethod->data_items [ip [3]];
			mono_value_copy_internal (LOCAL_VAR (ip [1], gpointer), locals + ip [2], c);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_UN_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (val < 0 || val > G_MAXINT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint32)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U8_I4) {
			gint32 val = LOCAL_VAR (ip [2], gint32);
			if (val < 0)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], guint64) = val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U8_I8) {
			gint64 val = LOCAL_VAR (ip [2], gint64);
			if (val < 0)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], guint64) = val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I8_U8) {
			guint64 val = LOCAL_VAR (ip [2], guint64);
			if (val > G_MAXINT64)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U8_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (mono_isnan (val) || mono_trunc (val) != (guint64)val)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], guint64) = (guint64)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U8_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (mono_isnan (val) || mono_trunc (val) != (guint64)val)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], guint64) = (guint64)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I8_UN_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (val < 0 || mono_isnan (val) || mono_trunc (val) != (gint64)val)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = (gint64)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I8_UN_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (val < 0 || mono_isnan (val) || mono_trunc (val) != (gint64)val)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = (gint64)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I8_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (mono_isnan (val) || mono_trunc (val) != (gint64)val)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = (gint64)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I8_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (mono_isnan (val) || mono_trunc (val) != (gint64)val)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = (gint64)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BOX) {
			MonoVTable *vtable = (MonoVTable*)frame->imethod->data_items [ip [3]];

			MonoObject *o = mono_gc_alloc_obj (vtable, m_class_get_instance_size (vtable->klass));
			MONO_HANDLE_ASSIGN_RAW (tmp_handle, o);
			stackval_to_data (m_class_get_byval_arg (vtable->klass), (stackval*)(locals + ip [2]), mono_object_get_data (o), FALSE);
			MONO_HANDLE_ASSIGN_RAW (tmp_handle, NULL);

			LOCAL_VAR (ip [1], MonoObject*) = o;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BOX_VT) {
			MonoVTable *vtable = (MonoVTable*)frame->imethod->data_items [ip [3]];
			MonoClass *c = vtable->klass;

			MonoObject* o = mono_gc_alloc_obj (vtable, m_class_get_instance_size (c));
			MONO_HANDLE_ASSIGN_RAW (tmp_handle, o);
			mono_value_copy_internal (mono_object_get_data (o), locals + ip [2], c);
			MONO_HANDLE_ASSIGN_RAW (tmp_handle, NULL);

			LOCAL_VAR (ip [1], MonoObject*) = o;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BOX_PTR) {
			MonoVTable *vtable = (MonoVTable*)frame->imethod->data_items [ip [3]];
			MonoClass *c = vtable->klass;

			MonoObject* o = mono_gc_alloc_obj (vtable, m_class_get_instance_size (c));
			MONO_HANDLE_ASSIGN_RAW (tmp_handle, o);
			mono_value_copy_internal (mono_object_get_data (o), LOCAL_VAR (ip [2], gpointer), c);
			MONO_HANDLE_ASSIGN_RAW (tmp_handle, NULL);

			LOCAL_VAR (ip [1], MonoObject*) = o;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_BOX_NULLABLE_PTR) {
			MonoClass *c = (MonoClass*)frame->imethod->data_items [ip [3]];

			LOCAL_VAR (ip [1], MonoObject*) = mono_nullable_box (LOCAL_VAR (ip [2], gpointer), c, error);
			mono_interp_error_cleanup (error); /* FIXME: don't swallow the error */
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_NEWARR) {
			MonoVTable *vtable = (MonoVTable*)frame->imethod->data_items [ip [3]];
			LOCAL_VAR (ip [1], MonoObject*) = (MonoObject*) mono_array_new_specific_checked (vtable, LOCAL_VAR (ip [2], gint32), error);
			if (!is_ok (error)) {
				THROW_EX (mono_error_convert_to_exception (error), ip);
			}
			ip += 4;
			/*if (profiling_classes) {
				guint count = GPOINTER_TO_UINT (g_hash_table_lookup (profiling_classes, o->vtable->klass));
				count++;
				g_hash_table_insert (profiling_classes, o->vtable->klass, GUINT_TO_POINTER (count));
			}*/

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDLEN) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			LOCAL_VAR (ip [1], mono_u) = mono_array_length_internal ((MonoArray *)o);
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDLEN_SPAN) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			// FIXME What's the point of this opcode ? It's just a LDFLD
			gsize offset_length = (gsize)(gint16)ip [3];
			LOCAL_VAR (ip [1], mono_u) = *(gint32 *) ((guint8 *) o + offset_length);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_GETCHR) {
			MonoString *s = LOCAL_VAR (ip [2], MonoString*);
			NULL_CHECK (s);
			int i32 = LOCAL_VAR (ip [3], int);
			if (i32 < 0 || i32 >= mono_string_length_internal (s))
				THROW_EX (mono_get_exception_index_out_of_range (), ip);
			LOCAL_VAR (ip [1], gint32) = mono_string_chars_internal (s)[i32];
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_GETITEM_SPAN) {
			guint8 *span = LOCAL_VAR (ip [2], guint8*);
			int index = LOCAL_VAR (ip [3], int);
			NULL_CHECK (span);

			gsize offset_length = (gsize)(gint16)ip [5];

			const gint32 length = *(gint32 *) (span + offset_length);
			if (index < 0 || index >= length)
				THROW_EX (mono_get_exception_index_out_of_range (), ip);

			gsize element_size = (gsize)(gint16)ip [4];
			gsize offset_pointer = (gsize)(gint16)ip [6];

			const gpointer pointer = *(gpointer *)(span + offset_pointer);
			LOCAL_VAR (ip [1], gpointer) = (guint8 *) pointer + index * element_size;

			ip += 7;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STRLEN) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			LOCAL_VAR (ip [1], gint32) = mono_string_length_internal ((MonoString*) o);
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ARRAY_RANK) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			LOCAL_VAR (ip [1], gint32) = m_class_get_rank (mono_object_class (o));
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ARRAY_ELEMENT_SIZE) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			LOCAL_VAR (ip [1], gint32) = mono_array_element_size (mono_object_class (o));
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ARRAY_IS_PRIMITIVE) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			LOCAL_VAR (ip [1], gint32) = m_class_is_primitive (m_class_get_element_class (mono_object_class (o)));
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDELEMA1) {
			/* No bounds, one direction */
			MonoArray *ao = LOCAL_VAR (ip [2], MonoArray*);
			NULL_CHECK (ao);
			gint32 index = LOCAL_VAR (ip [3], gint32);
			if (index >= ao->max_length)
				THROW_EX (mono_get_exception_index_out_of_range (), ip);
			guint16 size = ip [4];
			LOCAL_VAR (ip [1], gpointer) = mono_array_addr_with_size_fast (ao, size, index);
			ip += 5;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDELEMA) {
			guint16 rank = ip [3];
			guint16 esize = ip [4];
			stackval *sp = (stackval*)(locals + ip [2]);

			MonoArray *ao = (MonoArray*) sp [0].data.o;
			NULL_CHECK (ao);

			g_assert (ao->bounds);
			guint32 pos = 0;
			for (int i = 0; i < rank; i++) {
				gint32 idx = sp [i + 1].data.i;
				gint32 lower = ao->bounds [i].lower_bound;
				guint32 len = ao->bounds [i].length;
				if (idx < lower || (guint32)(idx - lower) >= len)
					THROW_EX (mono_get_exception_index_out_of_range (), ip);
				pos = (pos * len) + (guint32)(idx - lower);
			}

			LOCAL_VAR (ip [1], gpointer) = mono_array_addr_with_size_fast (ao, esize, pos);
			ip += 5;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDELEMA_TC) {
			stackval *sp = (stackval*)(locals + ip [2]);

			MonoObject *o = (MonoObject*) sp [0].data.o;
			NULL_CHECK (o);

			MonoClass *klass = (MonoClass*)frame->imethod->data_items [ip [3]];
			MonoException *ex = ves_array_element_address (frame, klass, (MonoArray *) o, (gpointer*)(locals + ip [1]), sp + 1, TRUE);
			if (ex)
				THROW_EX (ex, ip);
			ip += 4;
			MINT_IN_BREAK;
		}

#define LDELEM(datatype,elemtype) do { \
	MonoArray *o = LOCAL_VAR (ip [2], MonoArray*); \
	NULL_CHECK (o); \
	gint32 aindex = LOCAL_VAR (ip [3], gint32); \
	if (aindex >= mono_array_length_internal (o)) \
		THROW_EX (mono_get_exception_index_out_of_range (), ip); \
	LOCAL_VAR (ip [1], datatype) = mono_array_get_fast (o, elemtype, aindex); \
	ip += 4; \
} while (0)
		MINT_IN_CASE(MINT_LDELEM_I1) LDELEM(gint32, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_U1) LDELEM(gint32, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_I2) LDELEM(gint32, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_U2) LDELEM(gint32, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_I4) LDELEM(gint32, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_U4) LDELEM(gint32, guint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_I8) LDELEM(gint64, guint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_I)  LDELEM(mono_u, mono_i); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_R4) LDELEM(float, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_R8) LDELEM(double, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_REF) LDELEM(gpointer, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEM_VT) {
			MonoArray *o = LOCAL_VAR (ip [2], MonoArray*);
			NULL_CHECK (o);
			mono_u aindex = LOCAL_VAR (ip [3], gint32);
			if (aindex >= mono_array_length_internal (o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip);

			guint16 size = ip [4];
			char *src_addr = mono_array_addr_with_size_fast ((MonoArray *) o, size, aindex);
			memcpy (locals + ip [1], src_addr, size);

			ip += 5;
			MINT_IN_BREAK;
		}
#define STELEM_PROLOG(o, aindex) do { \
	o = LOCAL_VAR (ip [1], MonoArray*); \
	NULL_CHECK (o); \
	aindex = LOCAL_VAR (ip [2], gint32); \
	if (aindex >= mono_array_length_internal (o)) \
		THROW_EX (mono_get_exception_index_out_of_range (), ip); \
} while (0)

#define STELEM(datatype, elemtype) do { \
	MonoArray *o; \
	gint32 aindex; \
	STELEM_PROLOG(o, aindex); \
	mono_array_set_fast (o, elemtype, aindex, LOCAL_VAR (ip [3], datatype)); \
	ip += 4; \
} while (0)
		MINT_IN_CASE(MINT_STELEM_I1) STELEM(gint32, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_U1) STELEM(gint32, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_I2) STELEM(gint32, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_U2) STELEM(gint32, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_I4) STELEM(gint32, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_I8) STELEM(gint64, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_I)  STELEM(mono_u, mono_i); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_R4) STELEM(float, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_R8) STELEM(double, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STELEM_REF) {
			MonoArray *o;
			gint32 aindex;
			STELEM_PROLOG(o, aindex);
			MonoObject *ref = LOCAL_VAR (ip [3], MonoObject*);

			if (ref) {
				gboolean isinst = mono_interp_isinst (ref, m_class_get_element_class (mono_object_class (o)));
				if (!isinst)
					THROW_EX (mono_get_exception_array_type_mismatch (), ip);
			}
			mono_array_setref_fast ((MonoArray *) o, aindex, ref);
			ip += 4;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_STELEM_VT) {
			MonoArray *o = LOCAL_VAR (ip [1], MonoArray*);
			NULL_CHECK (o);
			gint32 aindex = LOCAL_VAR (ip [2], gint32);
			if (aindex >= mono_array_length_internal (o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip);

			guint16 size = ip [5];
			char *dst_addr = mono_array_addr_with_size_fast ((MonoArray *) o, size, aindex);
			MonoClass *klass_vt = (MonoClass*)frame->imethod->data_items [ip [4]];
			mono_value_copy_internal (dst_addr, locals + ip [3], klass_vt);
			ip += 6;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_U4) {
			gint32 val = LOCAL_VAR (ip [2], gint32);
			if (val < 0)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_I8) {
			gint64 val = LOCAL_VAR (ip [2], gint64);
			if (val < G_MININT32 || val > G_MAXINT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint32) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_U8) {
			guint64 val = LOCAL_VAR (ip [2], guint64);
			if (val > G_MAXINT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint32) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (mono_isnan (val) || mono_trunc (val) != (gint32)val)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint32) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (val < G_MININT32 || val > G_MAXINT32 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint32)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U4_I4) {
			gint32 val = LOCAL_VAR (ip [2], gint32);
			if (val < 0)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U4_I8) {
			gint64 val = LOCAL_VAR (ip [2], gint64);
			if (val < 0 || val > G_MAXUINT32)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (guint32) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U4_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (mono_isnan (val) || mono_trunc (val) != (guint32)val)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (guint32)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U4_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (val < 0 || val > G_MAXUINT32 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (guint32) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I2_I4) {
			gint32 val = LOCAL_VAR (ip [2], gint32);
			if (val < G_MININT16 || val > G_MAXINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint16)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I2_U4) {
			gint32 val = LOCAL_VAR (ip [2], gint32);
			if (val < 0 || val > G_MAXINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint16)val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I2_I8) {
			gint64 val = LOCAL_VAR (ip [2], gint64);
			if (val < G_MININT16 || val > G_MAXINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint16) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I2_U8) {
			gint64 val = LOCAL_VAR (ip [2], gint64);
			if (val < 0 || val > G_MAXINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint16) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I2_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (val < G_MININT16 || val > G_MAXINT16 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint16) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I2_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (val < G_MININT16 || val > G_MAXINT16 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint16) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I2_UN_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (val < 0 || val > G_MAXINT16 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint16) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I2_UN_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (val < 0 || val > G_MAXINT16 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint16) val;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U2_I4) {
			gint32 val = LOCAL_VAR (ip [2], gint32);
			if (val < 0 || val > G_MAXUINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U2_I8) {
			gint64 val = LOCAL_VAR (ip [2], gint64);
			if (val < 0 || val > G_MAXUINT16)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (guint16) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U2_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (val < 0 || val > G_MAXUINT16 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (guint16) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U2_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (val < 0 || val > G_MAXUINT16 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (guint16) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I1_I4) {
			gint32 val = LOCAL_VAR (ip [2], gint32);
			if (val < G_MININT8 || val > G_MAXINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I1_U4) {
			gint32 val = LOCAL_VAR (ip [2], gint32);
			if (val < 0 || val > G_MAXINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I1_I8) {
			gint64 val = LOCAL_VAR (ip [2], gint64);
			if (val < G_MININT8 || val > G_MAXINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint8) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I1_U8) {
			gint64 val = LOCAL_VAR (ip [2], gint64);
			if (val < 0 || val > G_MAXINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint8) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I1_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (val < G_MININT8 || val > G_MAXINT8 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint8) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I1_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (val < G_MININT8 || val > G_MAXINT8 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint8) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I1_UN_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (val < 0 || val > G_MAXINT8 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint8) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I1_UN_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (val < 0 || val > G_MAXINT8 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (gint8) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U1_I4) {
			gint32 val = LOCAL_VAR (ip [2], gint32);
			if (val < 0 || val > G_MAXUINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U1_I8) {
			gint64 val = LOCAL_VAR (ip [2], gint64);
			if (val < 0 || val > G_MAXUINT8)
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (guint8) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U1_R4) {
			float val = LOCAL_VAR (ip [2], float);
			if (val < 0 || val > G_MAXUINT8 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (guint8) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_U1_R8) {
			double val = LOCAL_VAR (ip [2], double);
			if (val < 0 || val > G_MAXUINT8 || isnan (val))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = (guint8) val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CKFINITE) {
			double val = LOCAL_VAR (ip [2], double);
			if (!mono_isfinite (val))
				THROW_EX (mono_get_exception_arithmetic (), ip);
			LOCAL_VAR (ip [1], double) = val;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_MKREFANY) {
			MonoClass *c = (MonoClass*)frame->imethod->data_items [ip [3]];

			gpointer addr = LOCAL_VAR (ip [2], gpointer);
			/* Write the typedref value */
			MonoTypedRef *tref = (MonoTypedRef*)(locals + ip [1]);
			tref->klass = c;
			tref->type = m_class_get_byval_arg (c);
			tref->value = addr;

			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REFANYTYPE) {
			MonoTypedRef *tref = (MonoTypedRef*)(locals + ip [2]);

			LOCAL_VAR (ip [1], gpointer) = tref->type;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REFANYVAL) {
			MonoTypedRef *tref = (MonoTypedRef*)(locals + ip [2]);

			MonoClass *c = (MonoClass*)frame->imethod->data_items [ip [3]];
			if (c != tref->klass)
				THROW_EX (mono_get_exception_invalid_cast (), ip);

			LOCAL_VAR (ip [1], gpointer) = tref->value;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDTOKEN)
			// FIXME same as MINT_MONO_LDPTR
			LOCAL_VAR (ip [1], gpointer) = frame->imethod->data_items [ip [2]];
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_OVF_I4) {
			gint32 i1 = LOCAL_VAR (ip [2], gint32);
			gint32 i2 = LOCAL_VAR (ip [3], gint32);
			if (CHECK_ADD_OVERFLOW (i1, i2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = i1 + i2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ADD_OVF_I8) {
			gint64 l1 = LOCAL_VAR (ip [2], gint64);
			gint64 l2 = LOCAL_VAR (ip [3], gint64);
			if (CHECK_ADD_OVERFLOW64 (l1, l2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = l1 + l2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ADD_OVF_UN_I4) {
			guint32 i1 = LOCAL_VAR (ip [2], guint32);
			guint32 i2 = LOCAL_VAR (ip [3], guint32);
			if (CHECK_ADD_OVERFLOW_UN (i1, i2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], guint32) = i1 + i2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ADD_OVF_UN_I8) {
			guint64 l1 = LOCAL_VAR (ip [2], guint64);
			guint64 l2 = LOCAL_VAR (ip [3], guint64);
			if (CHECK_ADD_OVERFLOW64_UN (l1, l2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], guint64) = l1 + l2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_MUL_OVF_I4) {
			gint32 i1 = LOCAL_VAR (ip [2], gint32);
			gint32 i2 = LOCAL_VAR (ip [3], gint32);
			if (CHECK_MUL_OVERFLOW (i1, i2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = i1 * i2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_MUL_OVF_I8) {
			gint64 l1 = LOCAL_VAR (ip [2], gint64);
			gint64 l2 = LOCAL_VAR (ip [3], gint64);
			if (CHECK_MUL_OVERFLOW64 (l1, l2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = l1 * l2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_MUL_OVF_UN_I4) {
			guint32 i1 = LOCAL_VAR (ip [2], guint32);
			guint32 i2 = LOCAL_VAR (ip [3], guint32);
			if (CHECK_MUL_OVERFLOW_UN (i1, i2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], guint32) = i1 * i2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_MUL_OVF_UN_I8) {
			guint64 l1 = LOCAL_VAR (ip [2], guint64);
			guint64 l2 = LOCAL_VAR (ip [3], guint64);
			if (CHECK_MUL_OVERFLOW64_UN (l1, l2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], guint64) = l1 * l2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_SUB_OVF_I4) {
			gint32 i1 = LOCAL_VAR (ip [2], gint32);
			gint32 i2 = LOCAL_VAR (ip [3], gint32);
			if (CHECK_SUB_OVERFLOW (i1, i2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint32) = i1 - i2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_SUB_OVF_I8) {
			gint64 l1 = LOCAL_VAR (ip [2], gint64);
			gint64 l2 = LOCAL_VAR (ip [3], gint64);
			if (CHECK_SUB_OVERFLOW64 (l1, l2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = l1 - l2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_SUB_OVF_UN_I4) {
			guint32 i1 = LOCAL_VAR (ip [2], guint32);
			guint32 i2 = LOCAL_VAR (ip [3], guint32);
			if (CHECK_SUB_OVERFLOW_UN (i1, i2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], guint32) = i1 - i2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_SUB_OVF_UN_I8) {
			guint64 l1 = LOCAL_VAR (ip [2], guint64);
			guint64 l2 = LOCAL_VAR (ip [3], guint64);
			if (CHECK_SUB_OVERFLOW64_UN (l1, l2))
				THROW_EX (mono_get_exception_overflow (), ip);
			LOCAL_VAR (ip [1], gint64) = l1 - l2;
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ENDFINALLY) {
			guint16 clause_index = *(ip + 1);

			guint16 *ret_ip = *(guint16**)(locals + frame->imethod->clause_data_offsets [clause_index]);
			if (!ret_ip) {
				// this clause was called from EH, return to eh
				g_assert (clause_args && clause_args->exec_frame == frame);
				goto exit_clause;
			}
			ip = ret_ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALL_HANDLER)
		MINT_IN_CASE(MINT_CALL_HANDLER_S) {
			gboolean short_offset = *ip == MINT_CALL_HANDLER_S;
			const guint16 *ret_ip = short_offset ? (ip + 3) : (ip + 4);
			guint16 clause_index = *(ret_ip - 1);

			*(const guint16**)(locals + frame->imethod->clause_data_offsets [clause_index]) = ret_ip;

			// jump to clause
			ip += short_offset ? (gint16)*(ip + 1) : (gint32)READ32 (ip + 1);
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LEAVE)
		MINT_IN_CASE(MINT_LEAVE_S)
		MINT_IN_CASE(MINT_LEAVE_CHECK)
		MINT_IN_CASE(MINT_LEAVE_S_CHECK) {
			int opcode = *ip;
			gboolean const check = opcode == MINT_LEAVE_CHECK || opcode == MINT_LEAVE_S_CHECK;

			if (check && frame->imethod->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE) {
				MonoException *abort_exc = mono_interp_leave (frame);
				if (abort_exc)
					THROW_EX (abort_exc, ip);
			}

			gboolean const short_offset = opcode == MINT_LEAVE_S || opcode == MINT_LEAVE_S_CHECK;
			ip += short_offset ? (gint16)*(ip + 1) : (gint32)READ32 (ip + 1);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ICALL_V_V) 
		MINT_IN_CASE(MINT_ICALL_P_V) 
		MINT_IN_CASE(MINT_ICALL_PP_V)
		MINT_IN_CASE(MINT_ICALL_PPP_V)
		MINT_IN_CASE(MINT_ICALL_PPPP_V)
		MINT_IN_CASE(MINT_ICALL_PPPPP_V)
		MINT_IN_CASE(MINT_ICALL_PPPPPP_V)
			frame->state.ip = ip + 3;
			do_icall_wrapper (frame, NULL, *ip, NULL, (stackval*)(locals + ip [1]), frame->imethod->data_items [ip [2]], FALSE);
			EXCEPTION_CHECKPOINT_GC_UNSAFE;
			CHECK_RESUME_STATE (context);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ICALL_V_P)
		MINT_IN_CASE(MINT_ICALL_P_P)
		MINT_IN_CASE(MINT_ICALL_PP_P)
		MINT_IN_CASE(MINT_ICALL_PPP_P)
		MINT_IN_CASE(MINT_ICALL_PPPP_P)
		MINT_IN_CASE(MINT_ICALL_PPPPP_P)
		MINT_IN_CASE(MINT_ICALL_PPPPPP_P)
			frame->state.ip = ip + 4;
			do_icall_wrapper (frame, NULL, *ip, (stackval*)(locals + ip [1]), (stackval*)(locals + ip [2]), frame->imethod->data_items [ip [3]], FALSE);
			EXCEPTION_CHECKPOINT_GC_UNSAFE;
			CHECK_RESUME_STATE (context);
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_LDPTR) 
			LOCAL_VAR (ip [1], gpointer) = frame->imethod->data_items [ip [2]];
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_NEWOBJ)
			LOCAL_VAR (ip [1], MonoObject*) = mono_interp_new ((MonoClass*)frame->imethod->data_items [ip [2]]); // FIXME: do not swallow the error
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_RETOBJ)
			stackval_from_data (mono_method_signature_internal (frame->imethod->method)->ret, frame->stack, LOCAL_VAR (ip [1], gpointer),
			     mono_method_signature_internal (frame->imethod->method)->pinvoke);
			frame_data_allocator_pop (&context->data_stack, frame);
			goto exit_frame;
		MINT_IN_CASE(MINT_MONO_SGEN_THREAD_INFO)
			LOCAL_VAR (ip [1], gpointer) = mono_tls_get_sgen_thread_info ();
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_MEMORY_BARRIER) {
			++ip;
			mono_memory_barrier ();
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_MONO_EXCHANGE_I8) {
			gboolean flag = FALSE;
			gint64 *dest = LOCAL_VAR (ip [2], gint64*);
			gint64 exch = LOCAL_VAR (ip [3], gint64);
#if SIZEOF_VOID_P == 4
			if (G_UNLIKELY (((size_t)dest) & 0x7)) {
				gint64 result;
				mono_interlocked_lock ();
				result = *dest;
				*dest = exch;
				mono_interlocked_unlock ();
				LOCAL_VAR (ip [1], gint64) = result;
				flag = TRUE;
			}
#endif
			if (!flag)
				LOCAL_VAR (ip [1], gint64) = mono_atomic_xchg_i64 (dest, exch);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_MONO_LDDOMAIN)
			LOCAL_VAR (ip [1], gpointer) = mono_domain_get ();
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_GET_SP)
			LOCAL_VAR (ip [1], gpointer) = frame;
			ip += 2;
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

#define RELOP(datatype, op) \
	LOCAL_VAR (ip [1], gint32) = LOCAL_VAR (ip [2], datatype) op LOCAL_VAR (ip [3], datatype); \
	ip += 4;

#define RELOP_FP(datatype, op, noorder) do { \
	datatype a1 = LOCAL_VAR (ip [2], datatype); \
	datatype a2 = LOCAL_VAR (ip [3], datatype); \
	if (mono_isunordered (a1, a2)) \
		LOCAL_VAR (ip [1], gint32) = noorder; \
	else \
		LOCAL_VAR (ip [1], gint32) = a1 op a2; \
	ip += 4; \
} while (0)

		MINT_IN_CASE(MINT_CEQ_I4)
			RELOP(gint32, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CEQ0_I4)
			LOCAL_VAR (ip [1], gint32) = (LOCAL_VAR (ip [2], gint32) == 0);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CEQ_I8)
			RELOP(gint64, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CEQ_R4)
			RELOP_FP(float, ==, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CEQ_R8)
			RELOP_FP(double, ==, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CNE_I4)
			RELOP(gint32, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CNE_I8)
			RELOP(gint64, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CNE_R4)
			RELOP_FP(float, !=, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CNE_R8)
			RELOP_FP(double, !=, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_I4)
			RELOP(gint32, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_I8)
			RELOP(gint64, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_R4)
			RELOP_FP(float, >, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_R8)
			RELOP_FP(double, >, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGE_I4)
			RELOP(gint32, >=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGE_I8)
			RELOP(gint64, >=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGE_R4)
			RELOP_FP(float, >=, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGE_R8)
			RELOP_FP(double, >=, 0);
			MINT_IN_BREAK;

#define RELOP_CAST(datatype, op) \
	LOCAL_VAR (ip [1], gint32) = LOCAL_VAR (ip [2], datatype) op LOCAL_VAR (ip [3], datatype); \
	ip += 4;

		MINT_IN_CASE(MINT_CGE_UN_I4)
			RELOP_CAST(guint32, >=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGE_UN_I8)
			RELOP_CAST(guint64, >=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_UN_I4)
			RELOP_CAST(guint32, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_UN_I8)
			RELOP_CAST(guint64, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_UN_R4)
			RELOP_FP(float, >, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_UN_R8)
			RELOP_FP(double, >, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_I4)
			RELOP(gint32, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_I8)
			RELOP(gint64, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_R4)
			RELOP_FP(float, <, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_R8)
			RELOP_FP(double, <, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_I4)
			RELOP_CAST(guint32, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_I8)
			RELOP_CAST(guint64, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_R4)
			RELOP_FP(float, <, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_R8)
			RELOP_FP(double, <, 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_I4)
			RELOP(gint32, <=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_I8)
			RELOP(gint64, <=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_UN_I4)
			RELOP_CAST(guint32, <=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_UN_I8)
			RELOP_CAST(guint64, <=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_R4)
			RELOP_FP(float, <=, 0);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLE_R8)
			RELOP_FP(double, <=, 0);
			MINT_IN_BREAK;

#undef RELOP
#undef RELOP_FP
#undef RELOP_CAST

		MINT_IN_CASE(MINT_LDFTN_ADDR) {
			LOCAL_VAR (ip [1], gpointer) = frame->imethod->data_items [ip [2]];
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDFTN) {
			InterpMethod *m = (InterpMethod*)frame->imethod->data_items [ip [2]];

			LOCAL_VAR (ip [1], gpointer) = imethod_to_ftnptr (m);
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDVIRTFTN) {
			InterpMethod *m = (InterpMethod*)frame->imethod->data_items [ip [3]];
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);

			m = get_virtual_method (m, o->vtable);
			LOCAL_VAR (ip [1], gpointer) = imethod_to_ftnptr (m);
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDFTN_DYNAMIC) {
			error_init_reuse (error);

			MonoMethod *cmethod = LOCAL_VAR (ip [2], MonoMethod*);

			InterpMethod *m = mono_interp_get_imethod (cmethod, error);
			mono_error_assert_ok (error);
			LOCAL_VAR (ip [1], gpointer) = imethod_to_ftnptr (m);
			ip += 3;
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
			gboolean is_void = ip [0] == MINT_PROF_EXIT_VOID;
			guint16 flag = is_void ? ip [1] : ip [2];
			// Set retval
			if (!is_void) {
				int i32 = READ32 (ip + 3);
				if (i32)
					memmove (frame->retval, locals + ip [1], i32);
				else
					frame->retval [0] = LOCAL_VAR (ip [1], stackval);
			}

			if ((flag & TRACING_FLAG) || ((flag & PROFILING_FLAG) && MONO_PROFILER_ENABLED (method_leave) &&
					(frame->imethod->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE_CONTEXT))) {
				MonoProfilerCallContext *prof_ctx = g_new0 (MonoProfilerCallContext, 1);
				prof_ctx->interp_frame = frame;
				prof_ctx->method = frame->imethod->method;
				if (!is_void)
					prof_ctx->return_value = frame->retval;
				if (flag & TRACING_FLAG)
					mono_trace_leave_method (frame->imethod->method, frame->imethod->jinfo, prof_ctx);
				if (flag & PROFILING_FLAG)
					MONO_PROFILER_RAISE (method_leave, (frame->imethod->method, prof_ctx));
				g_free (prof_ctx);
			} else if ((flag & PROFILING_FLAG) && MONO_PROFILER_ENABLED (method_enter)) {
				MONO_PROFILER_RAISE (method_leave, (frame->imethod->method, NULL));
			}

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

		MINT_IN_CASE(MINT_LDLOCA_S)
			LOCAL_VAR (ip [1], gpointer) = locals + ip [2];
			ip += 3;
			MINT_IN_BREAK;

		MINT_IN_CASE(MINT_MOV_OFF)
			// This opcode is resolved to a normal MINT_MOV when emitting compacted instructions
			g_assert_not_reached ();
			MINT_IN_BREAK;

#define MOV(argtype1,argtype2) \
	LOCAL_VAR (ip [1], argtype1) = LOCAL_VAR (ip [2], argtype2); \
	ip += 3;
		// When loading from a local, we might need to sign / zero extend to 4 bytes
		// which is our minimum "register" size in interp. They are only needed when
		// the address of the local is taken and we should try to optimize them out
		// because the local can't be propagated.
		MINT_IN_CASE(MINT_MOV_I1) MOV(guint32, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MOV_U1) MOV(guint32, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MOV_I2) MOV(guint32, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MOV_U2) MOV(guint32, guint16); MINT_IN_BREAK;
		// Normal moves between locals
		MINT_IN_CASE(MINT_MOV_4) MOV(guint32, guint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MOV_8) MOV(guint64, guint64); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_MOV_VT) {
			guint16 size = ip [3];
			memmove (locals + ip [1], locals + ip [2], size);
			ip += 4;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_MOV_8_2)
			LOCAL_VAR (ip [1], guint64) = LOCAL_VAR (ip [2], guint64);
			LOCAL_VAR (ip [3], guint64) = LOCAL_VAR (ip [4], guint64);
			ip += 5;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MOV_8_3)
			LOCAL_VAR (ip [1], guint64) = LOCAL_VAR (ip [2], guint64);
			LOCAL_VAR (ip [3], guint64) = LOCAL_VAR (ip [4], guint64);
			LOCAL_VAR (ip [5], guint64) = LOCAL_VAR (ip [6], guint64);
			ip += 7;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MOV_8_4)
			LOCAL_VAR (ip [1], guint64) = LOCAL_VAR (ip [2], guint64);
			LOCAL_VAR (ip [3], guint64) = LOCAL_VAR (ip [4], guint64);
			LOCAL_VAR (ip [5], guint64) = LOCAL_VAR (ip [6], guint64);
			LOCAL_VAR (ip [7], guint64) = LOCAL_VAR (ip [8], guint64);
			ip += 9;
			MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LOCALLOC) {
			int len = LOCAL_VAR (ip [2], gint32);
			gpointer mem = frame_data_allocator_alloc (&context->data_stack, frame, ALIGN_TO (len, MINT_VT_ALIGNMENT));

			if (frame->imethod->init_locals)
				memset (mem, 0, len);
			LOCAL_VAR (ip [1], gpointer) = mem;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ENDFILTER)
			/* top of stack is result of filter */
			frame->retval->data.i = LOCAL_VAR (ip [1], gint32);
			goto exit_clause;
		MINT_IN_CASE(MINT_INITOBJ)
			memset (LOCAL_VAR (ip [1], gpointer), 0, ip [2]);
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CPBLK) {
			gpointer dest = LOCAL_VAR (ip [1], gpointer);
			gpointer src = LOCAL_VAR (ip [2], gpointer);
			if (!dest || !src)
				THROW_EX (mono_get_exception_null_reference(), ip);
			/* FIXME: value and size may be int64... */
			memcpy (dest, src, LOCAL_VAR (ip [3], gint32));
			ip += 4;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INITBLK) {
			gpointer dest = LOCAL_VAR (ip [1], gpointer);
			NULL_CHECK (dest);
			/* FIXME: value and size may be int64... */
			memset (dest, LOCAL_VAR (ip [2], gint32), LOCAL_VAR (ip [3], gint32));
			ip += 4;
			MINT_IN_BREAK;
		}
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

			MonoException *exc = LOCAL_VAR (ip [1], MonoException*);
			if (!exc)
				exc = mono_get_exception_null_reference ();

			THROW_EX_GENERAL (exc, ip, TRUE);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LD_DELEGATE_METHOD_PTR) {
			MonoDelegate *del = LOCAL_VAR (ip [2], MonoDelegate*);
			if (!del->interp_method) {
				/* Not created from interpreted code */
				error_init_reuse (error);
				g_assert (del->method);
				del->interp_method = mono_interp_get_imethod (del->method, error);
				mono_error_assert_ok (error);
			}
			g_assert (del->interp_method);
			LOCAL_VAR (ip [1], gpointer) = imethod_to_ftnptr (del->interp_method);
			ip += 3;
			MINT_IN_BREAK;
		}

#define MATH_UNOP(mathfunc) \
	LOCAL_VAR (ip [1], double) = mathfunc (LOCAL_VAR (ip [2], double)); \
	ip += 3;

#define MATH_BINOP(mathfunc) \
	LOCAL_VAR (ip [1], double) = mathfunc (LOCAL_VAR (ip [2], double), LOCAL_VAR (ip [3], double)); \
	ip += 4;

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
			LOCAL_VAR (ip [1], double) = fma (LOCAL_VAR (ip [2], double), LOCAL_VAR (ip [3], double), LOCAL_VAR (ip [4], double));
			ip += 5;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SCALEB)
			LOCAL_VAR (ip [1], double) = scalbn (LOCAL_VAR (ip [2], double), LOCAL_VAR (ip [3], gint32));
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ILOGB) {
			int result;
			double x = LOCAL_VAR (ip [2], double);
			if (FP_ILOGB0 != INT_MIN && x == 0.0)
				result = INT_MIN;
			else if (FP_ILOGBNAN != INT_MAX && isnan(x))
				result = INT_MAX;
			else
				result = ilogb (x);
			LOCAL_VAR (ip [1],  gint32) = result;
			ip += 3;
			MINT_IN_BREAK;
		}

#define MATH_UNOPF(mathfunc) \
	LOCAL_VAR (ip [1], float) = mathfunc (LOCAL_VAR (ip [2], float)); \
	ip += 3;

#define MATH_BINOPF(mathfunc) \
	LOCAL_VAR (ip [1], float) = mathfunc (LOCAL_VAR (ip [2], float), LOCAL_VAR (ip [3], float)); \
	ip += 4;
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
			LOCAL_VAR (ip [1], float) = fmaf (LOCAL_VAR (ip [2], float), LOCAL_VAR (ip [3], float), LOCAL_VAR (ip [4], float));
			ip += 5;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SCALEBF)
			LOCAL_VAR (ip [1], float) = scalbnf (LOCAL_VAR (ip [2], float), LOCAL_VAR (ip [3], gint32));
			ip += 4;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ILOGBF) {
			int result;
			float x = LOCAL_VAR (ip [2], float);
			if (FP_ILOGB0 != INT_MIN && x == 0.0)
				result = INT_MIN;
			else if (FP_ILOGBNAN != INT_MAX && isnan(x))
				result = INT_MAX;
			else
				result = ilogbf (x);
			LOCAL_VAR (ip [1], gint32) = result;
			ip += 3;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_INTRINS_ENUM_HASFLAG) {
			MonoClass *klass = (MonoClass*)frame->imethod->data_items [ip [4]];
			LOCAL_VAR (ip [1], gint32) = mono_interp_enum_hasflag ((stackval*)(locals + ip [2]), (stackval*)(locals + ip [3]), klass);
			ip += 5;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_GET_HASHCODE) {
			LOCAL_VAR (ip [1], gint32) = mono_object_hash_internal (LOCAL_VAR (ip [2], MonoObject*));
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_INTRINS_GET_TYPE) {
			MonoObject *o = LOCAL_VAR (ip [2], MonoObject*);
			NULL_CHECK (o);
			LOCAL_VAR (ip [1], MonoObject*) = (MonoObject*) o->vtable->type;
			ip += 3;
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
			g_assert (context->exc_gchandle);

			clear_resume_state (context);
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

	HANDLE_FUNCTION_RETURN ();
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
		else if (strncmp (arg, "-bblocks", 8) == 0)
			mono_interp_opt &= ~INTERP_OPT_BBLOCKS;
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

	// this informs MINT_ENDFINALLY to return to EH
	*(guint16**)(frame_locals (iframe) + iframe->imethod->clause_data_offsets [clause_index]) = NULL;

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
	memcpy (child_frame.stack, iframe->stack, iframe->imethod->locals_size);
	// Write the exception object in its reserved stack slot
	*((MonoException**)((char*)child_frame.stack + iframe->imethod->clause_data_offsets [clause_index])) = ex;
	context->stack_pointer += iframe->imethod->alloca_size;

	memset (&clause_args, 0, sizeof (FrameClauseArgs));
	clause_args.start_with_ip = (const guint16*)handler_ip;
	clause_args.end_at_ip = (const guint16*)handler_ip_end;
	clause_args.exec_frame = &child_frame;

	interp_exec_method (&child_frame, context, &clause_args);

	/* Copy back the updated frame */
	memcpy (iframe->stack, child_frame.stack, iframe->imethod->locals_size);

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
interp_find_jit_info (MonoMethod *method)
{
	InterpMethod* imethod;

	imethod = lookup_imethod (method);
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

	g_assert (iframe->imethod);

	return (char*)iframe->stack + get_arg_offset_fast (iframe->imethod, NULL, pos + iframe->imethod->hasthis);
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
	return iframe->stack;
}

static MonoInterpFrameHandle
interp_frame_get_parent (MonoInterpFrameHandle frame)
{
	InterpFrame *iframe = (InterpFrame*)frame;

	return iframe->parent;
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

	g_print ("total ops %ld\n", total_ops);
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
interp_add_imethod (gpointer method, gpointer user_data)
{
	InterpMethod *imethod = (InterpMethod*) method;
	if (imethod->opcounts > opcount_threshold)
		imethods [num_methods++] = imethod;
}

static int
imethod_opcount_comparer (gconstpointer m1, gconstpointer m2)
{
	long diff = (*(InterpMethod**)m2)->opcounts > (*(InterpMethod**)m1)->opcounts;
	if (diff > 0)
		return 1;
	else if (diff < 0)
		return -1;
	else
		return 0;
}

static void
interp_print_method_counts (void)
{
	MonoJitMemoryManager *jit_mm = get_default_jit_mm ();

	jit_mm_lock (jit_mm);
	imethods = (InterpMethod**) malloc (jit_mm->interp_code_hash.num_entries * sizeof (InterpMethod*));
	mono_internal_hash_table_apply (&jit_mm->interp_code_hash, interp_add_imethod, NULL);
	jit_mm_unlock (jit_mm);

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
invalidate_transform (gpointer imethod_, gpointer user_data)
{
	InterpMethod *imethod = (InterpMethod *) imethod_;
	imethod->transformed = FALSE;
}

static void
copy_imethod_for_frame (InterpFrame *frame)
{
	InterpMethod *copy = (InterpMethod *) m_method_alloc0 (frame->imethod->method, sizeof (InterpMethod));
	memcpy (copy, frame->imethod, sizeof (InterpMethod));
	copy->next_jit_code_hash = NULL; /* we don't want that in our copy */
	frame->imethod = copy;
	/* Note: The copy will be around until the method is unloaded. Ideally we
	 * would reclaim its memory when the corresponding InterpFrame is popped.
	 */
}

static void
metadata_update_backup_frames (MonoThreadInfo *info, InterpFrame *frame)
{
	while (frame) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "threadinfo=%p, copy imethod for method=%s", info, mono_method_full_name (frame->imethod->method, 1));
		copy_imethod_for_frame (frame);
		frame = frame->parent;
	}
}

static void
metadata_update_prepare_to_invalidate (void)
{
	/* (1) make a copy of imethod for every interpframe that is on the stack,
	 * so we do not invalidate currently running methods */

	FOREACH_THREAD_EXCLUDE (info, MONO_THREAD_INFO_FLAGS_NO_GC) {
		if (!info || !info->jit_data)
			continue;

		ThreadContext *context = (ThreadContext*)info->jit_data->interp_context;

		/* If the thread was in the interpreter and hit a safepoint
		 * opcode and suspended, backup the frames since the last lmf.
		 */
		if (context && context->safepoint_frame) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_METADATA_UPDATE, "threadinfo=%p, has safepoint frame %p", info, context->safepoint_frame);
			metadata_update_backup_frames (info, context->safepoint_frame);
		}

		MonoLMF *lmf = info->jit_data->lmf;
		while (lmf) {
			if (((gsize) lmf->previous_lmf) & 2) {
				MonoLMFExt *ext = (MonoLMFExt *) lmf;
				if (ext->kind == MONO_LMFEXT_INTERP_EXIT || ext->kind == MONO_LMFEXT_INTERP_EXIT_WITH_CTX) {
					InterpFrame *frame = ext->interp_exit_data;
					metadata_update_backup_frames (info, frame);
				}
			}
			lmf = (MonoLMF *)(((gsize) lmf->previous_lmf) & ~3);
		}
	} FOREACH_THREAD_END

	/* (2) invalidate all the registered imethods */
}


static void
interp_invalidate_transformed (void)
{
	gboolean need_stw_restart = FALSE;
        if (mono_metadata_has_updates ()) {
                mono_stop_world (MONO_THREAD_INFO_FLAGS_NO_GC);
                metadata_update_prepare_to_invalidate ();
                need_stw_restart = TRUE;
        }

	// FIXME: Enumerate all memory managers
	MonoJitMemoryManager *jit_mm = get_default_jit_mm ();

	jit_mm_lock (jit_mm);
	mono_internal_hash_table_apply (&jit_mm->interp_code_hash, invalidate_transform, NULL);
	jit_mm_unlock (jit_mm);

	if (need_stw_restart)
		mono_restart_world (MONO_THREAD_INFO_FLAGS_NO_GC);
}

typedef struct {
	InterpJitInfoFunc func;
	gpointer user_data;
} InterpJitInfoFuncUserData;

static void
interp_call_jit_info_func (gpointer imethod, gpointer user_data)
{
	InterpJitInfoFuncUserData *data = (InterpJitInfoFuncUserData *)user_data;
	data->func (((InterpMethod *)imethod)->jinfo, data->user_data);
}

static void
interp_jit_info_foreach (InterpJitInfoFunc func, gpointer user_data)
{
	InterpJitInfoFuncUserData data = {func, user_data};

	// FIXME: Enumerate all memory managers
	MonoJitMemoryManager *jit_mm = get_default_jit_mm ();

	jit_mm_lock (jit_mm);
	mono_internal_hash_table_apply (&jit_mm->interp_code_hash, interp_call_jit_info_func, &data);
	jit_mm_unlock (jit_mm);
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
