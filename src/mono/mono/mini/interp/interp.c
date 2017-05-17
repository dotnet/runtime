/**
 * \file
 * PLEASE NOTE: This is a research prototype.
 *
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
#include <setjmp.h>
#include <signal.h>
#include <math.h>
#include <locale.h>

#include <mono/utils/gc_wrapper.h>

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
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-debug.h>
#include <mono/utils/atomic.h>

#include "interp.h"
#include "interp-internals.h"
#include "mintops.h"
#include "hacks.h"

#include <mono/mini/mini.h>
#include <mono/mini/jit-icalls.h>


/* Mingw 2.1 doesnt need this any more, but leave it in for now for older versions */
#ifdef _WIN32
#define isnan _isnan
#define finite _finite
#endif
#ifndef HAVE_FINITE
#ifdef HAVE_ISFINITE
#define finite isfinite
#endif
#endif

static inline void
init_frame (MonoInvocation *frame, MonoInvocation *parent_frame, RuntimeMethod *rmethod, stackval *method_args, stackval *method_retval)
{
	frame->parent = parent_frame;
	frame->stack_args = method_args;
	frame->retval = method_retval;
	frame->runtime_method = rmethod;
	frame->ex = NULL;
	frame->ip = NULL;
	frame->invoke_trap = 0;
}

#define INIT_FRAME(frame,parent_frame,method_args,method_retval,domain,mono_method,error) do { \
	RuntimeMethod *_rmethod = mono_interp_get_runtime_method ((domain), (mono_method), (error));	\
	init_frame ((frame), (parent_frame), _rmethod, (method_args), (method_retval)); \
	} while (0)

/*
 * List of classes whose methods will be executed by transitioning to JITted code.
 * Used for testing.
 */
GSList *jit_classes;

void ves_exec_method (MonoInvocation *frame);

static char* dump_frame (MonoInvocation *inv);
static MonoArray *get_trace_ips (MonoDomain *domain, MonoInvocation *top);
static void ves_exec_method_with_context (MonoInvocation *frame, ThreadContext *context, unsigned short *start_with_ip, MonoException *filter_exception, int exit_at_finally);

typedef void (*ICallMethod) (MonoInvocation *frame);

static guint32 die_on_exception = 0;
static MonoNativeTlsKey thread_context_id;

static char* dump_args (MonoInvocation *inv);

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
	MonoMethodDesc *desc = data;

	if (mono_method_desc_full_match (desc, m))
		break_on_method = 1;
}

static void
debug_enter (MonoInvocation *frame, int *tracing)
{
	if (db_methods) {
		g_list_foreach (db_methods, db_match_method, (gpointer)frame->runtime_method->method);
		if (break_on_method)
			*tracing = nested_trace ? (global_tracing = 2, 3) : 2;
		break_on_method = 0;
	}
	if (*tracing) {
		MonoMethod *method = frame->runtime_method->method;
		char *mn, *args = dump_args (frame);
		debug_indent_level++;
		output_indent ();
		mn = mono_method_full_name (method, FALSE);
		g_print ("(%p) Entering %s (", mono_thread_internal_current (), mn);
		g_free (mn);
		g_print  ("%s)\n", args);
		g_free (args);
	}
	if (mono_profiler_events & MONO_PROFILE_ENTER_LEAVE)
		mono_profiler_method_enter (frame->runtime_method->method);
}


#define DEBUG_LEAVE()	\
	if (tracing) {	\
		char *mn, *args;	\
		args = dump_retval (frame);	\
		output_indent ();	\
		mn = mono_method_full_name (frame->runtime_method->method, FALSE); \
		g_print  ("(%p) Leaving %s", mono_thread_internal_current (),  mn);	\
		g_free (mn); \
		g_print  (" => %s\n", args);	\
		g_free (args);	\
		debug_indent_level--;	\
		if (tracing == 3) global_tracing = 0; \
	}	\
	if (mono_profiler_events & MONO_PROFILE_ENTER_LEAVE)	\
		mono_profiler_method_leave (frame->runtime_method->method);

#else

int mono_interp_traceopt = 0;
static void debug_enter (MonoInvocation *frame, int *tracing)
{
}
#define DEBUG_LEAVE()

#endif

/* Set the current execution state to the resume state in context */
#define SET_RESUME_STATE(context) do { \
		ip = (context)->handler_ip;						\
		sp->data.p = frame->ex;											\
		++sp;															\
		frame->ex = NULL;												\
		(context)->has_resume_state = 0;								\
		(context)->handler_frame = NULL;								\
		goto main_loop;													\
	} while (0)

static void
ves_real_abort (int line, MonoMethod *mh,
		const unsigned short *ip, stackval *stack, stackval *sp)
{
	MonoError error;
	fprintf (stderr, "Execution aborted in method: %s::%s\n", mh->klass->name, mh->name);
	fprintf (stderr, "Line=%d IP=0x%04lx, Aborted execution\n", line,
		 ip-(const unsigned short *)mono_method_get_header_checked (mh, &error)->code);
		mono_error_cleanup (&error); /* FIXME: don't swallow the error */
	g_print ("0x%04x %02x\n",
		 ip-(const unsigned short *)mono_method_get_header_checked (mh, &error)->code, *ip);
	mono_error_cleanup (&error); /* FIXME: don't swallow the error */
	if (sp > stack)
		printf ("\t[%ld] 0x%08x %0.5f\n", sp-stack, sp[-1].data.i, sp[-1].data.f);
}

#define ves_abort() \
	do {\
		ves_real_abort(__LINE__, frame->runtime_method->method, ip, frame->stack, sp); \
		THROW_EX (mono_get_exception_execution_engine (NULL), ip); \
	} while (0);

RuntimeMethod*
mono_interp_get_runtime_method (MonoDomain *domain, MonoMethod *method, MonoError *error)
{
	RuntimeMethod *rtm;
	MonoJitDomainInfo *info;
	MonoMethodSignature *sig;
	int i;

	error_init (error);

	info = domain_jit_info (domain);
	mono_domain_jit_code_hash_lock (domain);
	rtm = mono_internal_hash_table_lookup (&info->interp_code_hash, method);
	mono_domain_jit_code_hash_unlock (domain);
	if (rtm)
		return rtm;

	sig = mono_method_signature (method);

	rtm = mono_domain_alloc0 (domain, sizeof (RuntimeMethod));
	rtm->method = method;
	rtm->param_count = sig->param_count;
	rtm->hasthis = sig->hasthis;
	rtm->rtype = mini_get_underlying_type (sig->ret);
	rtm->param_types = mono_domain_alloc0 (domain, sizeof (MonoType*) * sig->param_count);
	for (i = 0; i < sig->param_count; ++i)
		rtm->param_types [i] = mini_get_underlying_type (sig->params [i]);

	mono_domain_jit_code_hash_lock (domain);
	if (!mono_internal_hash_table_lookup (&info->interp_code_hash, method))
		mono_internal_hash_table_insert (&info->interp_code_hash, method, rtm);
	mono_domain_jit_code_hash_unlock (domain);

	return rtm;
}

gpointer
mono_interp_create_trampoline (MonoDomain *domain, MonoMethod *method, MonoError *error)
{
	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		method = mono_marshal_get_synchronized_wrapper (method);
	return mono_interp_get_runtime_method (domain, method, error);
}

static inline RuntimeMethod*
get_virtual_method (MonoDomain *domain, RuntimeMethod *runtime_method, MonoObject *obj)
{
	MonoMethod *m = runtime_method->method;
	MonoError error;

	if ((m->flags & METHOD_ATTRIBUTE_FINAL) || !(m->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
		RuntimeMethod *ret = NULL;
		if (mono_object_is_transparent_proxy (obj)) {
			ret = mono_interp_get_runtime_method (domain, mono_marshal_get_remoting_invoke (m), &error);
			mono_error_cleanup (&error); /* FIXME: don't swallow the error */
		} else if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) {
			ret = mono_interp_get_runtime_method (domain, mono_marshal_get_synchronized_wrapper (m), &error);
			mono_error_cleanup (&error); /* FIXME: don't swallow the error */
		} else {
			ret = runtime_method;
		}
		return ret;
	}

	mono_class_setup_vtable (obj->vtable->klass);

	int slot = mono_method_get_vtable_slot (m);
	if (mono_class_is_interface (m->klass)) {
		g_assert (obj->vtable->klass != m->klass);
		/* TODO: interface offset lookup is slow, go through IMT instead */
		gboolean non_exact_match;
		slot += mono_class_interface_offset_with_variance (obj->vtable->klass, m->klass, &non_exact_match);
	}

	MonoMethod *virtual_method = obj->vtable->klass->vtable [slot];
	if (m->is_inflated && mono_method_get_context (m)->method_inst) {
		MonoGenericContext context = { NULL, NULL };

		if (mono_class_is_ginst (virtual_method->klass))
			context.class_inst = mono_class_get_generic_class (virtual_method->klass)->context.class_inst;
		else if (mono_class_is_gtd (virtual_method->klass))
			context.class_inst = mono_class_get_generic_container (virtual_method->klass)->context.class_inst;
		context.method_inst = mono_method_get_context (m)->method_inst;

		virtual_method = mono_class_inflate_generic_method_checked (virtual_method, &context, &error);
		mono_error_cleanup (&error); /* FIXME: don't swallow the error */
	}

	if (virtual_method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) {
		virtual_method = mono_marshal_get_synchronized_wrapper (virtual_method);
	}

	RuntimeMethod *virtual_runtime_method = mono_interp_get_runtime_method (domain, virtual_method, &error);
	mono_error_cleanup (&error); /* FIXME: don't swallow the error */
	return virtual_runtime_method;
}

static void inline
stackval_from_data (MonoType *type, stackval *result, char *data, gboolean pinvoke)
{
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
		result->data.f = *(float*)data;
		return;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		result->data.l = *(gint64*)data;
		return;
	case MONO_TYPE_R8:
		result->data.f = *(double*)data;
		return;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
		result->data.p = *(gpointer*)data;
		return;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			stackval_from_data (mono_class_enum_basetype (type->data.klass), result, data, pinvoke);
			return;
		} else
			mono_value_copy (result->data.vt, data, type->data.klass);
		return;
	case MONO_TYPE_GENERICINST:
		stackval_from_data (&type->data.generic_class->container_class->byval_arg, result, data, pinvoke);
		return;
	default:
		g_warning ("got type 0x%02x", type->type);
		g_assert_not_reached ();
	}
}

static void inline
stackval_to_data (MonoType *type, stackval *val, char *data, gboolean pinvoke)
{
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
		gint64 *p = (gint64*)data;
		*p = val->data.l;
		return;
	}
	case MONO_TYPE_R4: {
		float *p = (float*)data;
		*p = val->data.f;
		return;
	}
	case MONO_TYPE_R8: {
		double *p = (double*)data;
		*p = val->data.f;
		return;
	}
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY: {
		gpointer *p = (gpointer *) data;
		mono_gc_wbarrier_generic_store (p, val->data.p);
		return;
	}
	case MONO_TYPE_PTR: {
		gpointer *p = (gpointer *) data;
		*p = val->data.p;
		return;
	}
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			stackval_to_data (mono_class_enum_basetype (type->data.klass), val, data, pinvoke);
			return;
		} else
			mono_value_copy (data, val->data.vt, type->data.klass);
		return;
	case MONO_TYPE_GENERICINST: {
		MonoClass *container_class = type->data.generic_class->container_class;

		if (container_class->valuetype && !container_class->enumtype) {
			mono_value_copy (data, val->data.vt, mono_class_from_mono_type (type));
			return;
		}
		stackval_to_data (&type->data.generic_class->container_class->byval_arg, val, data, pinvoke);
		return;
	}
	default:
		g_warning ("got type %x", type->type);
		g_assert_not_reached ();
	}
}

static void
fill_in_trace (MonoException *exception, MonoInvocation *frame)
{
	MonoError error;
	char *stack_trace = dump_frame (frame);
	MonoDomain *domain = mono_domain_get();
	(exception)->stack_trace = mono_string_new_checked (domain, stack_trace, &error);
	mono_error_cleanup (&error); /* FIXME: don't swallow the error */
	(exception)->trace_ips = get_trace_ips (domain, frame);
	g_free (stack_trace);
}

#define FILL_IN_TRACE(exception, frame) fill_in_trace(exception, frame)

#define THROW_EX(exception,ex_ip)	\
	do {\
		frame->ip = (ex_ip);		\
		frame->ex = (MonoException*)(exception);	\
		FILL_IN_TRACE(frame->ex, frame); \
		goto handle_exception;	\
	} while (0)

static MonoObject*
ves_array_create (MonoInvocation *frame, MonoDomain *domain, MonoClass *klass, MonoMethodSignature *sig, stackval *values)
{
	uintptr_t *lengths;
	intptr_t *lower_bounds;
	MonoObject *obj;
	MonoError error;
	int i;

	lengths = alloca (sizeof (uintptr_t) * klass->rank * 2);
	for (i = 0; i < sig->param_count; ++i) {
		lengths [i] = values->data.i;
		values ++;
	}
	if (klass->rank == sig->param_count) {
		/* Only lengths provided. */
		lower_bounds = NULL;
	} else {
		/* lower bounds are first. */
		lower_bounds = (intptr_t *) lengths;
		lengths += klass->rank;
	}
	obj = (MonoObject*) mono_array_new_full_checked (domain, klass, lengths, lower_bounds, &error);
	if (!mono_error_ok (&error)) {
		frame->ex = mono_error_convert_to_exception (&error);
		FILL_IN_TRACE (frame->ex, frame);
	}
	mono_error_cleanup (&error); /* FIXME: don't swallow the error */
	return obj;
}

static gint32
ves_array_calculate_index (MonoArray *ao, stackval *sp, MonoInvocation *frame)
{
	g_assert (!frame->ex);
	MonoClass *ac = ((MonoObject *) ao)->vtable->klass;

	guint32 pos = 0;
	if (ao->bounds) {
		for (gint32 i = 0; i < ac->rank; i++) {
			guint32 idx = sp [i].data.i;
			guint32 lower = ao->bounds [i].lower_bound;
			guint32 len = ao->bounds [i].length;
			if (idx < lower || (idx - lower) >= len) {
				frame->ex = mono_get_exception_index_out_of_range ();
				FILL_IN_TRACE (frame->ex, frame);
				return -1;
			}
			pos = (pos * len) + idx - lower;
		}
	} else {
		pos = sp [0].data.i;
		if (pos >= ao->max_length) {
			frame->ex = mono_get_exception_index_out_of_range ();
			FILL_IN_TRACE (frame->ex, frame);
			return -1;
		}
	}
	return pos;
}

static void
ves_array_set (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args + 1;

	MonoObject *o = frame->stack_args->data.p;
	MonoArray *ao = (MonoArray *) o;
	MonoClass *ac = o->vtable->klass;

	g_assert (ac->rank >= 1);

	gint32 pos = ves_array_calculate_index (ao, sp, frame);
	if (frame->ex)
		return;

	if (sp [ac->rank].data.p && !mono_object_class (o)->element_class->valuetype) {
		MonoError error;
		MonoObject *isinst = mono_object_isinst_checked (sp [ac->rank].data.p, mono_object_class (o)->element_class, &error);
		mono_error_cleanup (&error);
		if (!isinst) {
			frame->ex = mono_get_exception_array_type_mismatch ();
			FILL_IN_TRACE (frame->ex, frame);
			return;
		}
	}

	gint32 esize = mono_array_element_size (ac);
	gpointer ea = mono_array_addr_with_size (ao, esize, pos);

	MonoType *mt = mono_method_signature (frame->runtime_method->method)->params [ac->rank];
	stackval_to_data (mt, &sp [ac->rank], ea, FALSE);
}

static void
ves_array_get (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args + 1;

	MonoObject *o = frame->stack_args->data.p;
	MonoArray *ao = (MonoArray *) o;
	MonoClass *ac = o->vtable->klass;

	g_assert (ac->rank >= 1);

	gint32 pos = ves_array_calculate_index (ao, sp, frame);
	if (frame->ex)
		return;

	gint32 esize = mono_array_element_size (ac);
	gpointer ea = mono_array_addr_with_size (ao, esize, pos);

	MonoType *mt = mono_method_signature (frame->runtime_method->method)->ret;
	stackval_from_data (mt, frame->retval, ea, FALSE);
}

static gpointer
ves_array_element_address (MonoInvocation *frame, MonoClass *required_type, MonoArray *ao, stackval *sp, gboolean needs_typecheck)
{
	MonoClass *ac = ((MonoObject *) ao)->vtable->klass;

	g_assert (ac->rank >= 1);

	gint32 pos = ves_array_calculate_index (ao, sp, frame);
	if (frame->ex)
		return NULL;

	if (needs_typecheck && !mono_class_is_assignable_from (mono_object_class ((MonoObject *) ao)->element_class, required_type->element_class)) {
		frame->ex = mono_get_exception_array_type_mismatch ();
		FILL_IN_TRACE (frame->ex, frame);
		return NULL;
	}
	gint32 esize = mono_array_element_size (ac);
	return mono_array_addr_with_size (ao, esize, pos);
}

void
interp_walk_stack_with_ctx (MonoInternalStackWalk func, MonoContext *ctx, MonoUnwindOptions options, void *user_data)
{
	MonoError error;
	ThreadContext *context = mono_native_tls_get_value (thread_context_id);

	if (!context)
		return;

	MonoInvocation *frame = context->current_frame;

	while (frame) {
		MonoStackFrameInfo fi;
		memset (&fi, 0, sizeof (MonoStackFrameInfo));

		/* TODO: hack to make some asserts happy. */
		fi.ji = (MonoJitInfo *) frame->runtime_method;

		if (frame->runtime_method)
			fi.method = fi.actual_method = frame->runtime_method->method;

		if (!fi.method || (fi.method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) || (fi.method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME))) {
			fi.il_offset = -1;
			fi.type = FRAME_TYPE_MANAGED_TO_NATIVE;
		} else {
			MonoMethodHeader *hd = mono_method_get_header_checked (fi.method, &error);
			mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			fi.type = FRAME_TYPE_MANAGED;
			fi.il_offset = frame->ip - (const unsigned short *) hd->code;
			if (!fi.method->wrapper_type)
				fi.managed = TRUE;
		}

		if (func (&fi, ctx, user_data))
			return;
		frame = frame->parent;
	}
}

static MonoPIFunc mono_interp_enter_icall_trampoline = NULL;

// TODO: this function is also arch dependent (register width).
static InterpMethodArguments* build_args_from_sig (MonoMethodSignature *sig, MonoInvocation *frame)
{
	InterpMethodArguments *margs = g_malloc0 (sizeof (InterpMethodArguments));

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
		case MONO_TYPE_I8:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_GENERICINST:
			margs->ilen++;
			break;
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
		margs->iargs [0] = frame->stack_args->data.p;
		int_i++;
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
		case MONO_TYPE_I8:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_GENERICINST:
			margs->iargs [int_i] = frame->stack_args [i].data.p;
#if DEBUG_INTERP
			g_print ("build_args_from_sig: margs->iargs [%d]: %p (frame @ %d)\n", int_i, margs->iargs [int_i], i);
#endif
			int_i++;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			if (ptype == MONO_TYPE_R4)
				* (float *) &(margs->fargs [int_f]) = (float) frame->stack_args [i].data.f;
			else
				margs->fargs [int_f] = frame->stack_args [i].data.f;
#if DEBUG_INTERP
			g_print ("build_args_from_sig: margs->fargs [%d]: %p (%f) (frame @ %d)\n", int_f, margs->fargs [int_f], margs->fargs [int_f], i);
#endif
			int_f++;
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
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_GENERICINST:
			margs->retval = &(frame->retval->data.p);
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			margs->retval = &(frame->retval->data.p);
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

static void 
ves_pinvoke_method (MonoInvocation *frame, MonoMethodSignature *sig, MonoFuncV addr, gboolean string_ctor, ThreadContext *context)
{
	jmp_buf env;
	MonoInvocation *old_frame = context->current_frame;
	MonoInvocation *old_env_frame = context->env_frame;
	jmp_buf *old_env = context->current_env;
	MonoLMFExt ext;

	if (setjmp (env)) {
		context->current_frame = old_frame;
		context->env_frame = old_env_frame;
		context->current_env = old_env;
		context->managed_code = 1;
		return;
	}

	frame->ex = NULL;
	context->env_frame = frame;
	context->current_env = &env;

	g_assert (!frame->runtime_method);
	if (!mono_interp_enter_icall_trampoline) {
		MonoTrampInfo *info;
		mono_interp_enter_icall_trampoline = mono_arch_get_enter_icall_trampoline (&info);
		// TODO:
		// mono_tramp_info_register (info, NULL);
	}

	InterpMethodArguments *margs = build_args_from_sig (sig, frame);
#if DEBUG_INTERP
	g_print ("ICALL: mono_interp_enter_icall_trampoline = %p, addr = %p\n", mono_interp_enter_icall_trampoline, addr);
	g_print ("margs(out): ilen=%d, flen=%d\n", margs->ilen, margs->flen);
#endif

	context->current_frame = frame;
	context->managed_code = 0;

	/*
	 * Push an LMF frame on the LMF stack
	 * to mark the transition to native code.
	 */
	memset (&ext, 0, sizeof (ext));
	ext.interp_exit = TRUE;
	ext.interp_exit_data = frame;

	mono_push_lmf (&ext);

	mono_interp_enter_icall_trampoline (addr, margs);

	mono_pop_lmf (&ext.lmf);

	context->managed_code = 1;
	/* domain can only be changed by native code */
	context->domain = mono_domain_get ();

	if (*mono_thread_interruption_request_flag ()) {
		MonoException *exc = mono_thread_interruption_checkpoint ();
		if (exc) {
			frame->ex = exc;
			context->search_for_handler = 1;
		}
	}
	
	if (!frame->ex && !MONO_TYPE_ISSTRUCT (sig->ret))
		stackval_from_data (sig->ret, frame->retval, (char*)&frame->retval->data.p, sig->pinvoke);

	context->current_frame = old_frame;
	context->env_frame = old_env_frame;
	context->current_env = old_env;

	g_free (margs->iargs);
	g_free (margs->fargs);
	g_free (margs);
}

void
mono_interp_init_delegate (MonoDelegate *del)
{
	if (del->method)
		return;
	/* shouldn't need a write barrier because we don't write a MonoObject into the field */
	del->method = ((RuntimeMethod *) del->method_ptr)->method;
}

/*
 * From the spec:
 * runtime specifies that the implementation of the method is automatically
 * provided by the runtime and is primarily used for the methods of delegates.
 */
static void
ves_runtime_method (MonoInvocation *frame, ThreadContext *context)
{
	MonoMethod *method = frame->runtime_method->method;
	const char *name = method->name;
	MonoObject *obj = (MonoObject*) frame->stack_args->data.p;
	MonoObject *isinst_obj;
	MonoError error;

	mono_class_init (method->klass);

	if (method->klass == mono_defaults.array_class) {
		if (!strcmp (method->name, "UnsafeMov")) {
			/* TODO: layout checks */
			MonoType *mt = mono_method_signature (method)->ret;
			stackval_from_data (mt, frame->retval, (char *) frame->stack_args, FALSE);
			return;
		}
	}

	isinst_obj = mono_object_isinst_checked (obj, mono_defaults.array_class, &error);
	mono_error_cleanup (&error); /* FIXME: don't swallow the error */
	if (obj && isinst_obj) {
		if (*name == 'S' && (strcmp (name, "Set") == 0)) {
			ves_array_set (frame);
			return;
		}
		if (*name == 'G' && (strcmp (name, "Get") == 0)) {
			ves_array_get (frame);
			return;
		}
	}
	
	g_error ("Don't know how to exec runtime method %s.%s::%s", 
			method->klass->name_space, method->klass->name,
			method->name);
}

#if DEBUG_INTERP
static char*
dump_stack (stackval *stack, stackval *sp)
{
	stackval *s = stack;
	GString *str = g_string_new ("");
	
	if (sp == stack)
		return g_string_free (str, FALSE);
	
	while (s < sp) {
		g_string_append_printf (str, "[%p (%lld)] ", s->data.l, s->data.l);
		++s;
	}
	return g_string_free (str, FALSE);
}
#endif

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
		if (type->data.klass->enumtype)
			g_string_append_printf (str, "[%d] ", s->data.i);
		else
			g_string_append_printf (str, "[vt:%p] ", s->data.p);
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		g_string_append_printf (str, "[%g] ", s->data.f);
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	default: {
		GString *res = g_string_new ("");
		mono_type_get_desc (res, type, TRUE);
		g_string_append_printf (str, "[{%s} %lld/0x%0llx] ", res->str, s->data.l, s->data.l);
		g_string_free (res, TRUE);
		break;
	}
	}
}

#if DEBUG_INTERP
static char*
dump_retval (MonoInvocation *inv)
{
	GString *str = g_string_new ("");
	MonoType *ret = mono_method_signature (inv->runtime_method->method)->ret;

	if (ret->type != MONO_TYPE_VOID)
		dump_stackval (str, inv->retval, ret);

	return g_string_free (str, FALSE);
}
#endif

static char*
dump_args (MonoInvocation *inv)
{
	GString *str = g_string_new ("");
	int i;
	MonoMethodSignature *signature = mono_method_signature (inv->runtime_method->method);
	
	if (signature->param_count == 0 && !signature->hasthis)
		return g_string_free (str, FALSE);

	if (signature->hasthis) {
		MonoMethod *method = inv->runtime_method->method;
		dump_stackval (str, inv->stack_args, &method->klass->byval_arg);
	}

	for (i = 0; i < signature->param_count; ++i)
		dump_stackval (str, inv->stack_args + (!!signature->hasthis) + i, signature->params [i]);

	return g_string_free (str, FALSE);
}
 
static char*
dump_frame (MonoInvocation *inv)
{
	GString *str = g_string_new ("");
	int i;
	char *args;
	MonoError error;

	for (i = 0; inv; inv = inv->parent) {
		if (inv->runtime_method != NULL) {
			MonoMethod *method = inv->runtime_method->method;
			MonoClass *k;

			int codep = 0;
			const char * opname = "";
			char *name;
			gchar *source = NULL;

			k = method->klass;

			if ((method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) == 0 &&
				(method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) == 0) {
				MonoMethodHeader *hd = mono_method_get_header_checked (method, &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */

				if (hd != NULL) {
					if (inv->ip) {
						opname = mono_interp_opname [*inv->ip];
						codep = inv->ip - inv->runtime_method->code;
						source = g_strdup_printf ("%s:%d // (TODO: proper stacktrace)", method->name, codep);
					} else 
						opname = "";

#if 0
					MonoDebugSourceLocation *minfo = mono_debug_lookup_method (method);
					source = mono_debug_method_lookup_location (minfo, codep);
#endif
				}
			}
			args = dump_args (inv);
			name = mono_method_full_name (method, TRUE);
			if (source)
				g_string_append_printf (str, "#%d: 0x%05x %-10s in %s (%s) at %s\n", i, codep, opname, name, args, source);
			else
				g_string_append_printf (str, "#%d: 0x%05x %-10s in %s (%s)\n", i, codep, opname, name, args);
			g_free (name);
			g_free (args);
			g_free (source);
			++i;
		}
	}
	return g_string_free (str, FALSE);
}

static MonoArray *
get_trace_ips (MonoDomain *domain, MonoInvocation *top)
{
	int i;
	MonoArray *res;
	MonoInvocation *inv;
	MonoError error;

	for (i = 0, inv = top; inv; inv = inv->parent)
		if (inv->runtime_method != NULL)
			++i;

	res = mono_array_new_checked (domain, mono_defaults.int_class, 2 * i, &error);
	mono_error_cleanup (&error); /* FIXME: don't swallow the error */

	for (i = 0, inv = top; inv; inv = inv->parent)
		if (inv->runtime_method != NULL) {
			mono_array_set (res, gpointer, i, inv->runtime_method);
			++i;
			mono_array_set (res, gpointer, i, (gpointer)inv->ip);
			++i;
		}

	return res;
}


#define MYGUINT64_MAX 18446744073709551615ULL
#define MYGINT64_MAX 9223372036854775807LL
#define MYGINT64_MIN (-MYGINT64_MAX -1LL)

#define MYGUINT32_MAX 4294967295U
#define MYGINT32_MAX 2147483647
#define MYGINT32_MIN (-MYGINT32_MAX -1)
	
#define CHECK_ADD_OVERFLOW(a,b) \
	(gint32)(b) >= 0 ? (gint32)(MYGINT32_MAX) - (gint32)(b) < (gint32)(a) ? -1 : 0	\
	: (gint32)(MYGINT32_MIN) - (gint32)(b) > (gint32)(a) ? +1 : 0

#define CHECK_SUB_OVERFLOW(a,b) \
	(gint32)(b) < 0 ? (gint32)(MYGINT32_MAX) + (gint32)(b) < (gint32)(a) ? -1 : 0	\
	: (gint32)(MYGINT32_MIN) + (gint32)(b) > (gint32)(a) ? +1 : 0

#define CHECK_ADD_OVERFLOW_UN(a,b) \
	(guint32)(MYGUINT32_MAX) - (guint32)(b) < (guint32)(a) ? -1 : 0

#define CHECK_SUB_OVERFLOW_UN(a,b) \
	(guint32)(a) < (guint32)(b) ? -1 : 0

#define CHECK_ADD_OVERFLOW64(a,b) \
	(gint64)(b) >= 0 ? (gint64)(MYGINT64_MAX) - (gint64)(b) < (gint64)(a) ? -1 : 0	\
	: (gint64)(MYGINT64_MIN) - (gint64)(b) > (gint64)(a) ? +1 : 0

#define CHECK_SUB_OVERFLOW64(a,b) \
	(gint64)(b) < 0 ? (gint64)(MYGINT64_MAX) + (gint64)(b) < (gint64)(a) ? -1 : 0	\
	: (gint64)(MYGINT64_MIN) + (gint64)(b) > (gint64)(a) ? +1 : 0

#define CHECK_ADD_OVERFLOW64_UN(a,b) \
	(guint64)(MYGUINT64_MAX) - (guint64)(b) < (guint64)(a) ? -1 : 0

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
	(((gint32)(a) < 0) && ((gint32)(b) == -1)) ? (a == - MYGINT32_MAX) : \
	(((gint32)(a) > 0) && ((gint32)(b) > 0)) ? (gint32)(a) > ((MYGINT32_MAX) / (gint32)(b)) : \
	(((gint32)(a) > 0) && ((gint32)(b) < 0)) ? (gint32)(a) > ((MYGINT32_MIN) / (gint32)(b)) : \
	(((gint32)(a) < 0) && ((gint32)(b) > 0)) ? (gint32)(a) < ((MYGINT32_MIN) / (gint32)(b)) : \
	(gint32)(a) < ((MYGINT32_MAX) / (gint32)(b))

#define CHECK_MUL_OVERFLOW_UN(a,b) \
	((guint32)(a) == 0) || ((guint32)(b) == 0) ? 0 : \
	(guint32)(b) > ((MYGUINT32_MAX) / (guint32)(a))

#define CHECK_MUL_OVERFLOW64(a,b) \
	((gint64)(a) == 0) || ((gint64)(b) == 0) ? 0 : \
	(((gint64)(a) > 0) && ((gint64)(b) == -1)) ? FALSE : \
	(((gint64)(a) < 0) && ((gint64)(b) == -1)) ? (a == - MYGINT64_MAX) : \
	(((gint64)(a) > 0) && ((gint64)(b) > 0)) ? (gint64)(a) > ((MYGINT64_MAX) / (gint64)(b)) : \
	(((gint64)(a) > 0) && ((gint64)(b) < 0)) ? (gint64)(a) > ((MYGINT64_MIN) / (gint64)(b)) : \
	(((gint64)(a) < 0) && ((gint64)(b) > 0)) ? (gint64)(a) < ((MYGINT64_MIN) / (gint64)(b)) : \
	(gint64)(a) < ((MYGINT64_MAX) / (gint64)(b))

#define CHECK_MUL_OVERFLOW64_UN(a,b) \
	((guint64)(a) == 0) || ((guint64)(b) == 0) ? 0 : \
	(guint64)(b) > ((MYGUINT64_MAX) / (guint64)(a))

#if SIZEOF_VOID_P == 4
#define CHECK_MUL_OVERFLOW_NAT(a,b) CHECK_MUL_OVERFLOW(a,b)
#define CHECK_MUL_OVERFLOW_NAT_UN(a,b) CHECK_MUL_OVERFLOW_UN(a,b)
#else
#define CHECK_MUL_OVERFLOW_NAT(a,b) CHECK_MUL_OVERFLOW64(a,b)
#define CHECK_MUL_OVERFLOW_NAT_UN(a,b) CHECK_MUL_OVERFLOW64_UN(a,b)
#endif

MonoObject*
mono_interp_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error)
{
	MonoInvocation frame;
	ThreadContext * volatile context = mono_native_tls_get_value (thread_context_id);
	MonoObject *retval = NULL;
	MonoMethodSignature *sig = mono_method_signature (method);
	MonoClass *klass = mono_class_from_mono_type (sig->ret);
	int i, type, isobject = 0;
	void *ret = NULL;
	stackval result;
	stackval *args = alloca (sizeof (stackval) * (sig->param_count + !!sig->hasthis));
	ThreadContext context_struct;
	MonoInvocation *old_frame = NULL;
	jmp_buf env;

	error_init (error);
	if (exc)
		*exc = NULL;

	frame.ex = NULL;

	if (setjmp(env)) {
		if (context != &context_struct) {
			context->domain = mono_domain_get ();
			context->current_frame = old_frame;
			context->managed_code = 0;
		} else 
			mono_native_tls_set_value (thread_context_id, NULL);
		if (exc != NULL)
			*exc = (MonoObject *)frame.ex;
		return retval;
	}

	if (context == NULL) {
		context = &context_struct;
		memset (context, 0, sizeof (ThreadContext));
		context_struct.base_frame = &frame;
		context_struct.env_frame = &frame;
		context_struct.current_env = &env;
		mono_native_tls_set_value (thread_context_id, context);
	}
	else
		old_frame = context->current_frame;

	context->domain = mono_domain_get ();

	switch (sig->ret->type) {
	case MONO_TYPE_VOID:
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		isobject = 1;
		break;
	case MONO_TYPE_VALUETYPE:
		retval = mono_object_new_checked (context->domain, klass, error);
		ret = mono_object_unbox (retval);
		if (!sig->ret->data.klass->enumtype)
			result.data.vt = ret;
		else
			result.data.vt = alloca (mono_class_instance_size (klass));
		break;
	case MONO_TYPE_GENERICINST:
		if (!MONO_TYPE_IS_REFERENCE (sig->ret)) {
			retval = mono_object_new_checked (context->domain, klass, error);
			ret = mono_object_unbox (retval);
			if (!sig->ret->data.klass->enumtype)
				result.data.vt = ret;
			else
				result.data.vt = alloca (mono_class_instance_size (klass));
		} else {
			isobject = 1;
		}
		break;

	case MONO_TYPE_PTR:
		retval = mono_object_new_checked (context->domain, mono_defaults.int_class, error);
		ret = mono_object_unbox (retval);
		break;
	default:
		retval = mono_object_new_checked (context->domain, klass, error);
		ret = mono_object_unbox (retval);
		break;
	}

	if (sig->hasthis)
		args [0].data.p = obj;

	for (i = 0; i < sig->param_count; ++i) {
		int a_index = i + !!sig->hasthis;
		if (sig->params [i]->byref) {
			args [a_index].data.p = params [i];
			continue;
		}
		type = sig->params [i]->type;
handle_enum:
		switch (type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
			args [a_index].data.i = *(MonoBoolean*)params [i];
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
			args [a_index].data.i = *(gint16*)params [i];
			break;
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_U: /* use VAL_POINTER? */
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
			args [a_index].data.i = *(gint32*)params [i];
			break;
#if SIZEOF_VOID_P == 8
		case MONO_TYPE_U:
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			args [a_index].data.l = *(gint64*)params [i];
			break;
		case MONO_TYPE_R4:
			args [a_index].data.f = *(gfloat *) params [i];
			break;
		case MONO_TYPE_R8:
			args [a_index].data.f = *(gdouble *) params [i];
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				type = mono_class_enum_basetype (sig->params [i]->data.klass)->type;
				goto handle_enum;
			} else {
				args [a_index].data.p = params [i];
			}
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_PTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_GENERICINST:
			args [a_index].data.p = params [i];
			break;
		default:
			g_error ("type 0x%x not handled in  runtime invoke", sig->params [i]->type);
		}
	}

	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
		method = mono_marshal_get_native_wrapper (method, FALSE, FALSE);
	INIT_FRAME (&frame,context->current_frame,args,&result,mono_get_root_domain (),method,error);
	if (exc)
		frame.invoke_trap = 1;
	context->managed_code = 1;
	ves_exec_method_with_context (&frame, context, NULL, NULL, -1);
	context->managed_code = 0;
	if (context == &context_struct)
		mono_native_tls_set_value (thread_context_id, NULL);
	else
		context->current_frame = old_frame;
	if (frame.ex != NULL) {
		if (exc != NULL) {
			*exc = (MonoObject*) frame.ex;
			return NULL;
		}
		if (context->current_env != NULL) {
			context->env_frame->ex = frame.ex;
			longjmp(*context->current_env, 1);
		}
		else
			printf("dropped exception...\n");
	}
	if (sig->ret->type == MONO_TYPE_VOID && !method->string_ctor)
		return NULL;
	if (isobject || method->string_ctor)
		return result.data.p;
	stackval_to_data (sig->ret, &result, ret, sig->pinvoke);
	return retval;
}

typedef struct {
	RuntimeMethod *rmethod;
	gpointer this_arg;
	gpointer res;
	gpointer args [16];
	gpointer *many_args;
} InterpEntryData;

/* Main function for entering the interpreter from compiled code */
static void
interp_entry (InterpEntryData *data)
{
	MonoInvocation frame;
	RuntimeMethod *rmethod = data->rmethod;
	ThreadContext *context = mono_native_tls_get_value (thread_context_id);
	ThreadContext context_struct;
	MonoInvocation *old_frame;
	stackval result;
	stackval *args;
	MonoMethod *method;
	MonoMethodSignature *sig;
	MonoType *type;
	int i;

	method = rmethod->method;
	sig = mono_method_signature (method);

	// FIXME: Optimize this

	//printf ("%s\n", mono_method_full_name (method, 1));

	frame.ex = NULL;
	if (context == NULL) {
		context = &context_struct;
		memset (context, 0, sizeof (ThreadContext));
		context_struct.base_frame = &frame;
		context_struct.env_frame = &frame;
		mono_native_tls_set_value (thread_context_id, context);
	}
	else
		old_frame = context->current_frame;
	context->domain = mono_domain_get ();

	args = alloca (sizeof (stackval) * (sig->param_count + (sig->hasthis ? 1 : 0)));
	if (sig->hasthis)
		args [0].data.p = data->this_arg;

	gpointer *params;
	if (data->many_args)
		params = data->many_args;
	else
		params = data->args;
	for (i = 0; i < sig->param_count; ++i) {
		int a_index = i + (sig->hasthis ? 1 : 0);
		if (sig->params [i]->byref) {
			args [a_index].data.p = params [i];
			continue;
		}
		type = rmethod->param_types [i];
		switch (type->type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
			args [a_index].data.i = *(MonoBoolean*)params [i];
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
			args [a_index].data.i = *(gint16*)params [i];
			break;
		case MONO_TYPE_U:
#if SIZEOF_VOID_P == 4
			args [a_index].data.p = GINT_TO_POINTER (*(guint32*)params [i]);
#else
			args [a_index].data.p = GINT_TO_POINTER (*(guint64*)params [i]);
#endif
			break;
		case MONO_TYPE_I:
#if SIZEOF_VOID_P == 4
			args [a_index].data.p = GINT_TO_POINTER (*(gint32*)params [i]);
#else
			args [a_index].data.p = GINT_TO_POINTER (*(gint64*)params [i]);
#endif
			break;
		case MONO_TYPE_U4:
			args [a_index].data.i = *(guint32*)params [i];
			break;
		case MONO_TYPE_I4:
			args [a_index].data.i = *(gint32*)params [i];
			break;
		case MONO_TYPE_U8:
			args [a_index].data.l = *(guint64*)params [i];
			break;
		case MONO_TYPE_I8:
			args [a_index].data.l = *(gint64*)params [i];
			break;
		case MONO_TYPE_PTR:
		case MONO_TYPE_OBJECT:
			args [a_index].data.p = *(MonoObject**)params [i];
			break;
		case MONO_TYPE_VALUETYPE:
			args [a_index].data.p = params [i];
			break;
		case MONO_TYPE_GENERICINST:
			if (MONO_TYPE_IS_REFERENCE (type))
				args [a_index].data.p = params [i];
			else
				args [a_index].data.vt = params [i];
			break;
		default:
			printf ("%s\n", mono_type_full_name (sig->params [i]));
			NOT_IMPLEMENTED;
			break;
		}
	}

	init_frame (&frame, NULL, data->rmethod, args, &result);
	context->managed_code = 1;

	type = rmethod->rtype;
	switch (type->type) {
	case MONO_TYPE_GENERICINST:
		if (!MONO_TYPE_IS_REFERENCE (type))
			frame.retval->data.vt = data->res;
		break;
	case MONO_TYPE_VALUETYPE:
		frame.retval->data.vt = data->res;
		break;
	default:
		break;
	}

	ves_exec_method_with_context (&frame, context, NULL, NULL, -1);
	context->managed_code = 0;
	if (context == &context_struct)
		mono_native_tls_set_value (thread_context_id, NULL);
	else
		context->current_frame = old_frame;

	// FIXME:
	g_assert (frame.ex == NULL);

	type = rmethod->rtype;
	switch (type->type) {
	case MONO_TYPE_VOID:
		break;
	case MONO_TYPE_I1:
		*(gint8*)data->res = frame.retval->data.i;
		break;
	case MONO_TYPE_U1:
		*(guint8*)data->res = frame.retval->data.i;
		break;
	case MONO_TYPE_I2:
		*(gint16*)data->res = frame.retval->data.i;
		break;
	case MONO_TYPE_U2:
		*(guint16*)data->res = frame.retval->data.i;
		break;
	case MONO_TYPE_I4:
		*(gint32*)data->res = frame.retval->data.i;
		break;
	case MONO_TYPE_U4:
		*(guint64*)data->res = frame.retval->data.i;
		break;
	case MONO_TYPE_I8:
		*(gint64*)data->res = frame.retval->data.i;
		break;
	case MONO_TYPE_U8:
		*(guint64*)data->res = frame.retval->data.i;
		break;
	case MONO_TYPE_I:
#if SIZEOF_VOID_P == 8
		*(gint64*)data->res = (gint64)frame.retval->data.p;
#else
		*(gint32*)data->res = (gint32)frame.retval->data.p;
#endif
		break;
	case MONO_TYPE_U:
#if SIZEOF_VOID_P == 8
		*(guint64*)data->res = (guint64)frame.retval->data.p;
#else
		*(guint32*)data->res = (guint32)frame.retval->data.p;
#endif
		break;
	case MONO_TYPE_OBJECT:
		/* No need for a write barrier */
		*(MonoObject**)data->res = (MonoObject*)frame.retval->data.p;
		break;
	case MONO_TYPE_GENERICINST:
		if (MONO_TYPE_IS_REFERENCE (type)) {
			*(MonoObject**)data->res = *(MonoObject**)frame.retval->data.p;
		} else {
			/* Already set before the call */
		}
		break;
	case MONO_TYPE_VALUETYPE:
		/* Already set before the call */
		break;
	default:
		printf ("%s\n", mono_type_full_name (sig->ret));
		NOT_IMPLEMENTED;
		break;
	}
}

static stackval * 
do_icall (ThreadContext *context, int op, stackval *sp, gpointer ptr)
{
	MonoInvocation *old_frame = context->current_frame;
	MonoInvocation *old_env_frame = context->env_frame;
	jmp_buf *old_env = context->current_env;
	jmp_buf env;

	if (setjmp (env)) {
		context->current_frame = old_frame;
		context->env_frame = old_env_frame;
		context->current_env = old_env;
		context->managed_code = 1;
		return sp;
	}

	context->env_frame = context->current_frame;
	context->current_env = &env;
	context->managed_code = 0;

	switch (op) {
	case MINT_ICALL_V_V: {
		void (*func)(void) = ptr;
        	func ();
		break;
	}
	case MINT_ICALL_V_P: {
		gpointer (*func)(void) = ptr;
		sp++;
		sp [-1].data.p = func ();
		break;
	}
	case MINT_ICALL_P_V: {
		void (*func)(gpointer) = ptr;
        	func (sp [-1].data.p);
		sp --;
		break;
	}
	case MINT_ICALL_P_P: {
		gpointer (*func)(gpointer) = ptr;
		sp [-1].data.p = func (sp [-1].data.p);
		break;
	}
	case MINT_ICALL_PP_V: {
		void (*func)(gpointer,gpointer) = ptr;
		sp -= 2;
		func (sp [0].data.p, sp [1].data.p);
		break;
	}
	case MINT_ICALL_PI_V: {
		void (*func)(gpointer,int) = ptr;
		sp -= 2;
		func (sp [0].data.p, sp [1].data.i);
		break;
	}
	case MINT_ICALL_PP_P: {
		gpointer (*func)(gpointer,gpointer) = ptr;
		--sp;
		sp [-1].data.p = func (sp [-1].data.p, sp [0].data.p);
		break;
	}
	case MINT_ICALL_PI_P: {
		gpointer (*func)(gpointer,int) = ptr;
		--sp;
		sp [-1].data.p = func (sp [-1].data.p, sp [0].data.i);
		break;
	}
	case MINT_ICALL_PPP_V: {
		void (*func)(gpointer,gpointer,gpointer) = ptr;
		sp -= 3;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p);
		break;
	}
	case MINT_ICALL_PPI_V: {
		void (*func)(gpointer,gpointer,int) = ptr;
		sp -= 3;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.i);
		break;
	}
	default:
		g_assert_not_reached ();
	}

	context->env_frame = old_env_frame;
	context->current_env = old_env;

	return sp;
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

#define MAX_INTERP_ENTRY_ARGS 8

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

#define ARGLIST0 RuntimeMethod *rmethod
#define ARGLIST1 gpointer arg1, RuntimeMethod *rmethod
#define ARGLIST2 gpointer arg1, gpointer arg2, RuntimeMethod *rmethod
#define ARGLIST3 gpointer arg1, gpointer arg2, gpointer arg3, RuntimeMethod *rmethod
#define ARGLIST4 gpointer arg1, gpointer arg2, gpointer arg3, gpointer arg4, RuntimeMethod *rmethod
#define ARGLIST5 gpointer arg1, gpointer arg2, gpointer arg3, gpointer arg4, gpointer arg5, RuntimeMethod *rmethod
#define ARGLIST6 gpointer arg1, gpointer arg2, gpointer arg3, gpointer arg4, gpointer arg5, gpointer arg6, RuntimeMethod *rmethod
#define ARGLIST7 gpointer arg1, gpointer arg2, gpointer arg3, gpointer arg4, gpointer arg5, gpointer arg6, gpointer arg7, RuntimeMethod *rmethod
#define ARGLIST8 gpointer arg1, gpointer arg2, gpointer arg3, gpointer arg4, gpointer arg5, gpointer arg6, gpointer arg7, gpointer arg8, RuntimeMethod *rmethod

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
static void interp_entry_instance_ret_7 (gpointer this_arg, gpointer res, ARGLIST7) INTERP_ENTRY6 (this_arg, res, rmethod)
static void interp_entry_instance_ret_8 (gpointer this_arg, gpointer res, ARGLIST8) INTERP_ENTRY6 (this_arg, res, rmethod)

#define INTERP_ENTRY_FUNCLIST(type) interp_entry_ ## type ## _0, interp_entry_ ## type ## _1, interp_entry_ ## type ## _2, interp_entry_ ## type ## _3, interp_entry_ ## type ## _4, interp_entry_ ## type ## _5, interp_entry_ ## type ## _6, interp_entry_ ## type ## _7, interp_entry_ ## type ## _8

gpointer entry_funcs_static [MAX_INTERP_ENTRY_ARGS + 1] = { INTERP_ENTRY_FUNCLIST (static) };
gpointer entry_funcs_static_ret [MAX_INTERP_ENTRY_ARGS + 1] = { INTERP_ENTRY_FUNCLIST (static_ret) };
gpointer entry_funcs_instance [MAX_INTERP_ENTRY_ARGS + 1] = { INTERP_ENTRY_FUNCLIST (instance) };
gpointer entry_funcs_instance_ret [MAX_INTERP_ENTRY_ARGS + 1] = { INTERP_ENTRY_FUNCLIST (instance_ret) };

/* General version for methods with more than MAX_INTERP_ENTRY_ARGS arguments */
static void
interp_entry_general (gpointer this_arg, gpointer res, gpointer *args, gpointer rmethod)
{
	INTERP_ENTRY_BASE (rmethod, this_arg, res);
	data.many_args = args;
	interp_entry (&data);
}

/*
 * mono_interp_create_method_pointer:
 *
 * Return a function pointer which can be used to call METHOD using the
 * interpreter. Return NULL for methods which are not supported.
 */
gpointer
mono_interp_create_method_pointer (MonoMethod *method, MonoError *error)
{
	gpointer addr;
	MonoMethodSignature *sig = mono_method_signature (method);
	MonoMethod *wrapper;
	RuntimeMethod *rmethod;

	/* HACK: method_ptr of delegate should point to a runtime method*/
	if (method->wrapper_type && method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
		return mono_interp_get_runtime_method (mono_domain_get (), method, error);

	rmethod = mono_interp_get_runtime_method (mono_domain_get (), method, error);
	if (rmethod->jit_entry)
		return rmethod->jit_entry;
	wrapper = mini_get_interp_in_wrapper (sig);

	gpointer jit_wrapper = mono_jit_compile_method_jit_only (wrapper, error);
	mono_error_assert_ok (error);

	//printf ("%s %s\n", mono_method_full_name (method, 1), mono_method_full_name (wrapper, 1));
	gpointer entry_func;
	if (sig->param_count > MAX_INTERP_ENTRY_ARGS) {
		entry_func = interp_entry_general;
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

	/* This is the argument passed to the interp_in wrapper by the static rgctx trampoline */
	MonoFtnDesc *ftndesc = g_new0 (MonoFtnDesc, 1);
	ftndesc->addr = entry_func;
	ftndesc->arg = rmethod;
	mono_error_assert_ok (error);

	/*
	 * The wrapper is called by compiled code, which doesn't pass the extra argument, so we pass it in the
	 * rgctx register using a trampoline.
	 */

	// FIXME: AOT
	g_assert (!mono_aot_only);
	addr = mono_arch_get_static_rgctx_trampoline (ftndesc, jit_wrapper);

	mono_memory_barrier ();
	rmethod->jit_entry = addr;

	return addr;
}

#if COUNT_OPS
static int opcode_counts[512];

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
		char *mn = mono_method_full_name (frame->runtime_method->method, FALSE); \
		g_print ("(%p) %s -> ", mono_thread_internal_current (), mn); \
		g_free (mn); \
		mono_interp_dis_mintop(rtm->code, ip); \
		g_print ("\t%d:%s\n", vt_sp - vtalloc, ins); \
		g_free (ins); \
	}
#else
#define DUMP_INSTR()
#endif

#ifdef __GNUC__
#define USE_COMPUTED_GOTO 1
#endif
#if USE_COMPUTED_GOTO
#define MINT_IN_SWITCH(op) COUNT_OP(op); goto *in_labels[op];
#define MINT_IN_CASE(x) LAB_ ## x:
#if DEBUG_INTERP
#define MINT_IN_BREAK if (tracing > 1) goto main_loop; else { COUNT_OP(*ip); goto *in_labels[*ip]; }
#else
#define MINT_IN_BREAK { COUNT_OP(*ip); goto *in_labels[*ip]; }
#endif
#define MINT_IN_DEFAULT mint_default: if (0) goto mint_default; /* make gcc shut up */
#else
#define MINT_IN_SWITCH(op) switch (op)
#define MINT_IN_CASE(x) case x:
#define MINT_IN_BREAK break
#define MINT_IN_DEFAULT default:
#endif

/*
 * If EXIT_AT_FINALLY is not -1, exit after exiting the finally clause with that index.
 */
static void 
ves_exec_method_with_context (MonoInvocation *frame, ThreadContext *context, unsigned short *start_with_ip, MonoException *filter_exception, int exit_at_finally)
{
	MonoInvocation child_frame;
	GSList *finally_ips = NULL;
	const unsigned short *endfinally_ip = NULL;
	const unsigned short *ip = NULL;
	register stackval *sp;
	RuntimeMethod *rtm;
#if DEBUG_INTERP
	gint tracing = global_tracing;
	unsigned char *vtalloc;
#else
	gint tracing = 0;
#endif
	int i32;
	unsigned char *vt_sp;
	unsigned char *locals;
	MonoError error;
	MonoObject *o = NULL;
	MonoClass *c;
#if USE_COMPUTED_GOTO
	static void *in_labels[] = {
#define OPDEF(a,b,c,d) \
	&&LAB_ ## a,
#include "mintops.def"
	0 };
#endif

	frame->ex = NULL;
	frame->ex_handler = NULL;
	frame->ip = NULL;
	context->current_frame = frame;

	debug_enter (frame, &tracing);

	if (!frame->runtime_method->transformed) {
		context->managed_code = 0;
#if DEBUG_INTERP
		char *mn = mono_method_full_name (frame->runtime_method->method, TRUE);
		g_print ("(%p) Transforming %s\n", mono_thread_internal_current (), mn);
		g_free (mn);
#endif
		frame->ex = mono_interp_transform_method (frame->runtime_method, context);
		context->managed_code = 1;
		if (frame->ex) {
			rtm = NULL;
			ip = NULL;
			goto exit_frame;
		}
	}

	rtm = frame->runtime_method;
	if (!start_with_ip ) {
		frame->args = alloca (rtm->alloca_size);
		memset (frame->args, 0, rtm->alloca_size);

		ip = rtm->code;
	} else {
		ip = start_with_ip;
	}
	sp = frame->stack = (stackval *) ((char *) frame->args + rtm->args_size);
	vt_sp = (unsigned char *) sp + rtm->stack_size;
#if DEBUG_INTERP
	vtalloc = vt_sp;
#endif
	locals = (unsigned char *) vt_sp + rtm->vt_stack_size;
	child_frame.parent = frame;

	if (filter_exception) {
		sp->data.p = filter_exception;
		sp++;
	}

	/*
	 * using while (ip < end) may result in a 15% performance drop, 
	 * but it may be useful for debug
	 */
	while (1) {
	main_loop:
		/* g_assert (sp >= frame->stack); */
		/* g_assert(vt_sp - vtalloc <= rtm->vt_stack_size); */
		DUMP_INSTR();
		MINT_IN_SWITCH (*ip) {
		MINT_IN_CASE(MINT_INITLOCALS)
			memset (locals, 0, rtm->locals_size);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NOP)
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BREAK)
			++ip;
			G_BREAKPOINT (); /* this is not portable... */
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDNULL) 
			sp->data.p = NULL;
			++ip;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_VTRESULT) {
			int ret_size = * (guint16 *)(ip + 1);
			unsigned char *ret_vt_sp = vt_sp;
			vt_sp -= READ32(ip + 2);
			if (ret_size > 0) {
				memmove (vt_sp, ret_vt_sp, ret_size);
				sp [-1].data.p = vt_sp;
				vt_sp += (ret_size + 7) & ~7;
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
			sp->data.i = *(const short *)(ip + 1);
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
		MINT_IN_CASE(MINT_LDC_R4) {
			guint32 val;
			++ip;
			val = READ32(ip);
			sp->data.f = * (float *)&val;
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
		MINT_IN_CASE(MINT_DUP_VT)
			i32 = READ32 (ip + 1);
			sp->data.p = vt_sp;
			memcpy(sp->data.p, sp [-1].data.p, i32);
			vt_sp += (i32 + 7) & ~7;
			++sp;
			ip += 3;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_POP) {
			guint16 u16 = (* (guint16 *)(ip + 1)) + 1;
			if (u16 > 1)
				memmove (sp - u16, sp - 1, (u16 - 1) * sizeof (stackval));
			sp--;
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_JMP) {
			RuntimeMethod *new_method = rtm->data_items [* (guint16 *)(ip + 1)];
			if (!new_method->transformed) {
				frame->ip = ip;
				frame->ex = mono_interp_transform_method (new_method, context);
				if (frame->ex)
					goto exit_frame;
			}
			ip += 2;
			if (new_method->alloca_size > rtm->alloca_size)
				g_error ("MINT_JMP to method which needs more stack space (%d > %d)", new_method->alloca_size, rtm->alloca_size); 
			rtm = frame->runtime_method = new_method;
			vt_sp = (unsigned char *) sp + rtm->stack_size;
#if DEBUG_INTERP
			vtalloc = vt_sp;
#endif
			locals = vt_sp + rtm->vt_stack_size;
			ip = rtm->new_body_start; /* bypass storing input args from callers frame */
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALLI) {
			MonoMethodSignature *csignature;
			stackval *endsp = sp;

			frame->ip = ip;
			
			csignature = rtm->data_items [* (guint16 *)(ip + 1)];
			ip += 2;
			--sp;
			--endsp;
			child_frame.runtime_method = sp->data.p;

			sp->data.p = vt_sp;
			child_frame.retval = sp;
			/* decrement by the actual number of args */
			sp -= csignature->param_count;
			if (csignature->hasthis)
				--sp;
			child_frame.stack_args = sp;

			/* `this' can be NULL for string:.ctor */
			if (csignature->hasthis && sp->data.p && mono_object_is_transparent_proxy (sp->data.p)) {
				child_frame.runtime_method = mono_interp_get_runtime_method (context->domain, mono_marshal_get_remoting_invoke (child_frame.runtime_method->method), &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			} else if (child_frame.runtime_method->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
				child_frame.runtime_method = mono_interp_get_runtime_method (context->domain, mono_marshal_get_native_wrapper (child_frame.runtime_method->method, FALSE, FALSE), &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			}

			if (csignature->hasthis) {
				MonoObject *this_arg = sp->data.p;

				if (this_arg->vtable->klass->valuetype) {
					gpointer *unboxed = mono_object_unbox (this_arg);
					sp [0].data.p = unboxed;
				}
			}

			ves_exec_method_with_context (&child_frame, context, NULL, NULL, -1);

			context->current_frame = frame;

			if (context->has_resume_state) {
				if (frame == context->handler_frame)
					SET_RESUME_STATE (context);
				else
					goto exit_frame;
			}

			if (child_frame.ex) {
				/*
				 * An exception occurred, need to run finally, fault and catch handlers..
				 */
				frame->ex = child_frame.ex;
				goto handle_finally;
			}

			/* need to handle typedbyref ... */
			if (csignature->ret->type != MONO_TYPE_VOID) {
				*sp = *endsp;
				sp++;
			}
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALLI_NAT) {
			MonoMethodSignature *csignature;
			stackval *endsp = sp;
			unsigned char *code = NULL;

			frame->ip = ip;
			
			csignature = rtm->data_items [* (guint16 *)(ip + 1)];
			ip += 2;
			--sp;
			--endsp;
			code = sp->data.p;
			child_frame.runtime_method = NULL;

			sp->data.p = vt_sp;
			child_frame.retval = sp;
			/* decrement by the actual number of args */
			sp -= csignature->param_count;
			if (csignature->hasthis)
				--sp;
			child_frame.stack_args = sp;
			ves_pinvoke_method (&child_frame, csignature, (MonoFuncV) code, FALSE, context);

			context->current_frame = frame;

			if (context->has_resume_state) {
				if (frame == context->handler_frame)
					SET_RESUME_STATE (context);
				else
					goto exit_frame;
			}

			if (child_frame.ex) {
				/*
				 * An exception occurred, need to run finally, fault and catch handlers..
				 */
				frame->ex = child_frame.ex;
				if (context->search_for_handler) {
					context->search_for_handler = 0;
					goto handle_exception;
				}
				goto handle_finally;
			}

			/* need to handle typedbyref ... */
			if (csignature->ret->type != MONO_TYPE_VOID) {
				*sp = *endsp;
				sp++;
			}
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALL) {
			stackval *endsp = sp;

			frame->ip = ip;
			
			child_frame.runtime_method = rtm->data_items [* (guint16 *)(ip + 1)];
			ip += 2;
			sp->data.p = vt_sp;
			child_frame.retval = sp;
			/* decrement by the actual number of args */
			sp -= child_frame.runtime_method->param_count;
			if (child_frame.runtime_method->hasthis)
				--sp;
			child_frame.stack_args = sp;

			/* `this' can be NULL for string:.ctor */
			if (child_frame.runtime_method->hasthis && !child_frame.runtime_method->method->klass->valuetype && sp->data.p && mono_object_is_transparent_proxy (sp->data.p)) {
				child_frame.runtime_method = mono_interp_get_runtime_method (context->domain, mono_marshal_get_remoting_invoke (child_frame.runtime_method->method), &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			}

			ves_exec_method_with_context (&child_frame, context, NULL, NULL, -1);

			context->current_frame = frame;

			if (context->has_resume_state) {
				if (frame == context->handler_frame)
					SET_RESUME_STATE (context);
				else
					goto exit_frame;
			}

			if (child_frame.ex) {
				/*
				 * An exception occurred, need to run finally, fault and catch handlers..
				 */
				frame->ex = child_frame.ex;
				goto handle_exception;;
			}

			/* need to handle typedbyref ... */
			*sp = *endsp;
			sp++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_VCALL) {
			frame->ip = ip;
			
			child_frame.runtime_method = rtm->data_items [* (guint16 *)(ip + 1)];
			ip += 2;

			sp->data.p = vt_sp;
			child_frame.retval = sp;
			/* decrement by the actual number of args */
			sp -= child_frame.runtime_method->param_count;
			if (child_frame.runtime_method->hasthis) {
				--sp;
				MonoObject *this_arg = sp->data.p;
				if (!this_arg)
					THROW_EX (mono_get_exception_null_reference(), ip - 2);
			}
			child_frame.stack_args = sp;

			if (child_frame.runtime_method->hasthis && !child_frame.runtime_method->method->klass->valuetype && mono_object_is_transparent_proxy (sp->data.p)) {
				child_frame.runtime_method = mono_interp_get_runtime_method (context->domain, mono_marshal_get_remoting_invoke (child_frame.runtime_method->method), &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			}

			ves_exec_method_with_context (&child_frame, context, NULL, NULL, -1);

			context->current_frame = frame;

			if (context->has_resume_state) {
				if (frame == context->handler_frame)
					SET_RESUME_STATE (context);
				else
					goto exit_frame;
			}

			if (child_frame.ex) {
				/*
				 * An exception occurred, need to run finally, fault and catch handlers..
				 */
				frame->ex = child_frame.ex;
				goto handle_finally;
			}
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_JIT_CALL) {
			MonoMethodSignature *sig;
			RuntimeMethod *rmethod = rtm->data_items [* (guint16 *)(ip + 1)];
			MonoFtnDesc ftndesc;
			guint8 res_buf [256];
			MonoType *type;
			MonoLMFExt ext;

			//printf ("%s\n", mono_method_full_name (rmethod->method, 1));

			/*
			 * Call JITted code through a gsharedvt_out wrapper. These wrappers receive every argument
			 * by ref and return a return value using an explicit return value argument.
			 */
			if (!rmethod->jit_wrapper) {
				MonoMethod *method = rmethod->method;
				MonoError error;

				sig = mono_method_signature (method);
				g_assert (sig);

				MonoMethod *wrapper = mini_get_gsharedvt_out_sig_wrapper (sig);
				//printf ("J: %s %s\n", mono_method_full_name (method, 1), mono_method_full_name (wrapper, 1));

				gpointer jit_wrapper = mono_jit_compile_method_jit_only (wrapper, &error);
				mono_error_assert_ok (&error);

				gpointer addr = mono_jit_compile_method_jit_only (method, &error);
				g_assert (addr);
				mono_error_assert_ok (&error);

				rmethod->jit_addr = addr;
				rmethod->jit_sig = sig;
				mono_memory_barrier ();
				rmethod->jit_wrapper = jit_wrapper;

			} else {
				sig = rmethod->jit_sig;
			}

			frame->ip = ip;
			ip += 2;
			sp -= sig->param_count;
			if (sig->hasthis)
				--sp;

			ftndesc.addr = rmethod->jit_addr;
			ftndesc.arg = NULL;

			// FIXME: Optimize this

			gpointer args [32];
			int pindex = 0;
			int stack_index = 0;
			if (rmethod->hasthis) {
				args [pindex ++] = sp [0].data.p;
				stack_index ++;
			}
			type = rmethod->rtype;
			if (type->type != MONO_TYPE_VOID) {
				if (MONO_TYPE_ISSTRUCT (type))
					args [pindex ++] = vt_sp;
				else
					args [pindex ++] = res_buf;
			}
			for (int i = 0; i < rmethod->param_count; ++i) {
				MonoType *t = rmethod->param_types [i];
				stackval *sval = &sp [stack_index + i];
				if (sig->params [i]->byref) {
					args [pindex ++] = sval->data.p;
				} else if (MONO_TYPE_ISSTRUCT (t)) {
					args [pindex ++] = sval->data.p;
				} else if (MONO_TYPE_IS_REFERENCE (t)) {
					args [pindex ++] = &sval->data.p;
				} else {
					switch (t->type) {
					case MONO_TYPE_I1:
					case MONO_TYPE_U1:
					case MONO_TYPE_I2:
					case MONO_TYPE_U2:
					case MONO_TYPE_I4:
					case MONO_TYPE_U4:
					case MONO_TYPE_VALUETYPE:
						args [pindex ++] = &sval->data.i;
						break;
					case MONO_TYPE_PTR:
					case MONO_TYPE_FNPTR:
					case MONO_TYPE_I:
					case MONO_TYPE_U:
					case MONO_TYPE_OBJECT:
						args [pindex ++] = &sval->data.p;
						break;
					case MONO_TYPE_I8:
					case MONO_TYPE_U8:
						args [pindex ++] = &sval->data.l;
						break;
					default:
						printf ("%s\n", mono_type_full_name (t));
						g_assert_not_reached ();
					}
				}
			}

			/*
			 * Push an LMF frame on the LMF stack
			 * to mark the transition to compiled code.
			 */
			memset (&ext, 0, sizeof (ext));
			ext.interp_exit = TRUE;
			ext.interp_exit_data = frame;

			mono_push_lmf (&ext);

			switch (pindex) {
			case 0: {
				void (*func)(gpointer) = rmethod->jit_wrapper;

				func (&ftndesc);
				break;
			}
			case 1: {
				void (*func)(gpointer, gpointer) = rmethod->jit_wrapper;

				func (args [0], &ftndesc);
				break;
			}
			case 2: {
				void (*func)(gpointer, gpointer, gpointer) = rmethod->jit_wrapper;

				func (args [0], args [1], &ftndesc);
				break;
			}
			case 3: {
				void (*func)(gpointer, gpointer, gpointer, gpointer) = rmethod->jit_wrapper;

				func (args [0], args [1], args [2], &ftndesc);
				break;
			}
			case 4: {
				void (*func)(gpointer, gpointer, gpointer, gpointer, gpointer) = rmethod->jit_wrapper;

				func (args [0], args [1], args [2], args [3], &ftndesc);
				break;
			}
			case 5: {
				void (*func)(gpointer, gpointer, gpointer, gpointer, gpointer, gpointer) = rmethod->jit_wrapper;

				func (args [0], args [1], args [2], args [3], args [4], &ftndesc);
				break;
			}
			case 6: {
				void (*func)(gpointer, gpointer, gpointer, gpointer, gpointer, gpointer, gpointer) = rmethod->jit_wrapper;

				func (args [0], args [1], args [2], args [3], args [4], args [5], &ftndesc);
				break;
			}
			case 7: {
				void (*func)(gpointer, gpointer, gpointer, gpointer, gpointer, gpointer, gpointer, gpointer) = rmethod->jit_wrapper;

				func (args [0], args [1], args [2], args [3], args [4], args [5], args [6], &ftndesc);
				break;
			}
			default:
				g_assert_not_reached ();
				break;
			}

			mono_pop_lmf (&ext.lmf);

			if (context->has_resume_state) {
				/*
				 * If this bit is set, it means the call has thrown the exception, and we
				 * reached this point because the EH code in mono_handle_exception ()
				 * unwound all the JITted frames below us. mono_interp_set_resume_state ()
				 * has set the fields in context to indicate where we have to resume execution.
				 */
				if (frame == context->handler_frame)
					SET_RESUME_STATE (context);
				else
					goto exit_frame;
			}

			MonoType *rtype = rmethod->rtype;
			switch (rtype->type) {
			case MONO_TYPE_VOID:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_STRING:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
				sp->data.p = *(gpointer*)res_buf;
				break;
			case MONO_TYPE_I1:
				sp->data.i = *(gint8*)res_buf;
				break;
			case MONO_TYPE_U1:
				sp->data.i = *(guint8*)res_buf;
				break;
			case MONO_TYPE_I2:
				sp->data.i = *(gint16*)res_buf;
				break;
			case MONO_TYPE_U2:
				sp->data.i = *(guint16*)res_buf;
				break;
			case MONO_TYPE_I4:
				sp->data.i = *(gint32*)res_buf;
				break;
			case MONO_TYPE_U4:
				sp->data.i = *(guint32*)res_buf;
				break;
			case MONO_TYPE_VALUETYPE:
				/* The result was written to vt_sp */
				sp->data.p = vt_sp;
				break;
			case MONO_TYPE_GENERICINST:
				if (MONO_TYPE_IS_REFERENCE (rtype)) {
					sp->data.p = *(gpointer*)res_buf;
				} else {
					/* The result was written to vt_sp */
					sp->data.p = vt_sp;
				}
				break;
			default:
				printf ("%s\n", mono_type_full_name (rtype));
				g_assert_not_reached ();
				break;
			}
			if (rtype->type != MONO_TYPE_VOID)
				sp++;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_CALLVIRT) {
			stackval *endsp = sp;
			MonoObject *this_arg;
			guint32 token;

			frame->ip = ip;
			
			token = * (unsigned short *)(ip + 1);
			ip += 2;
			child_frame.runtime_method = rtm->data_items [token];
			sp->data.p = vt_sp;
			child_frame.retval = sp;

			/* decrement by the actual number of args */
			sp -= child_frame.runtime_method->param_count + 1;
			child_frame.stack_args = sp;
			this_arg = sp->data.p;
			if (!this_arg)
				THROW_EX (mono_get_exception_null_reference(), ip - 2);
			child_frame.runtime_method = get_virtual_method (context->domain, child_frame.runtime_method, this_arg);

			MonoClass *this_class = this_arg->vtable->klass;
			if (this_class->valuetype && child_frame.runtime_method->method->klass->valuetype) {
				/* unbox */
				gpointer *unboxed = mono_object_unbox (this_arg);
				sp [0].data.p = unboxed;
			}

			ves_exec_method_with_context (&child_frame, context, NULL, NULL, -1);

			context->current_frame = frame;

			if (context->has_resume_state) {
				if (frame == context->handler_frame)
					SET_RESUME_STATE (context);
				else
					goto exit_frame;
			}

			if (child_frame.ex) {
				/*
				 * An exception occurred, need to run finally, fault and catch handlers..
				 */
				frame->ex = child_frame.ex;
				if (context->search_for_handler) {
					context->search_for_handler = 0;
					goto handle_exception;
				}
				goto handle_finally;
			}

			/* need to handle typedbyref ... */
			*sp = *endsp;
			sp++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_VCALLVIRT) {
			MonoObject *this_arg;
			guint32 token;

			frame->ip = ip;
			
			token = * (unsigned short *)(ip + 1);
			ip += 2;
			child_frame.runtime_method = rtm->data_items [token];
			sp->data.p = vt_sp;
			child_frame.retval = sp;

			/* decrement by the actual number of args */
			sp -= child_frame.runtime_method->param_count + 1;
			child_frame.stack_args = sp;
			this_arg = sp->data.p;
			if (!this_arg)
				THROW_EX (mono_get_exception_null_reference(), ip - 2);
			child_frame.runtime_method = get_virtual_method (context->domain, child_frame.runtime_method, this_arg);

			MonoClass *this_class = this_arg->vtable->klass;
			if (this_class->valuetype && child_frame.runtime_method->method->klass->valuetype) {
				gpointer *unboxed = mono_object_unbox (this_arg);
				sp [0].data.p = unboxed;
			}

			ves_exec_method_with_context (&child_frame, context, NULL, NULL, -1);

			context->current_frame = frame;

			if (context->has_resume_state) {
				if (frame == context->handler_frame)
					SET_RESUME_STATE (context);
				else
					goto exit_frame;
			}

			if (child_frame.ex) {
				/*
				 * An exception occurred, need to run finally, fault and catch handlers..
				 */
				frame->ex = child_frame.ex;
				if (context->search_for_handler) {
					context->search_for_handler = 0;
					goto handle_exception;
				}
				goto handle_finally;
			}
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CALLRUN)
			ves_runtime_method (frame, context);
			if (frame->ex) {
				rtm = NULL;
				goto handle_exception;
			}
			goto exit_frame;
		MINT_IN_CASE(MINT_RET)
			--sp;
			*frame->retval = *sp;
			if (sp > frame->stack)
				g_warning ("ret: more values on stack: %d", sp-frame->stack);
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_VOID)
			if (sp > frame->stack)
				g_warning ("ret.void: more values on stack: %d", sp-frame->stack);
			goto exit_frame;
		MINT_IN_CASE(MINT_RET_VT)
			i32 = READ32(ip + 1);
			--sp;
			memcpy(frame->retval->data.p, sp->data.p, i32);
			if (sp > frame->stack)
				g_warning ("ret.vt: more values on stack: %d", sp-frame->stack);
			goto exit_frame;
		MINT_IN_CASE(MINT_BR_S)
			ip += (short) *(ip + 1);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BR)
			ip += (gint32) READ32(ip + 1);
			MINT_IN_BREAK;
#define ZEROP_S(datamem, op) \
	--sp; \
	if (sp->data.datamem op 0) \
		ip += * (gint16 *)(ip + 1); \
	else \
		ip += 2;

#define ZEROP(datamem, op) \
	--sp; \
	if (sp->data.datamem op 0) \
		ip += READ32(ip + 1); \
	else \
		ip += 3;

		MINT_IN_CASE(MINT_BRFALSE_I4_S)
			ZEROP_S(i, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRFALSE_I8_S)
			ZEROP_S(l, ==);
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
		MINT_IN_CASE(MINT_BRFALSE_R8)
			ZEROP_S(f, ==);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_I4_S)
			ZEROP_S(i, !=);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BRTRUE_I8_S)
			ZEROP_S(l, !=);
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
		MINT_IN_CASE(MINT_BRTRUE_R8)
			ZEROP(f, !=);
			MINT_IN_BREAK;
#define CONDBR_S(cond) \
	sp -= 2; \
	if (cond) \
		ip += * (gint16 *)(ip + 1); \
	else \
		ip += 2;
#define BRELOP_S(datamem, op) \
	CONDBR_S(sp[0].data.datamem op sp[1].data.datamem)

#define CONDBR(cond) \
	sp -= 2; \
	if (cond) \
		ip += READ32(ip + 1); \
	else \
		ip += 3;

#define BRELOP(datamem, op) \
	CONDBR(sp[0].data.datamem op sp[1].data.datamem)

		MINT_IN_CASE(MINT_BEQ_I4_S)
			BRELOP_S(i, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_I8_S)
			BRELOP_S(l, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_R8_S)
			CONDBR_S(!isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f == sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_I4)
			BRELOP(i, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_I8)
			BRELOP(l, ==)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BEQ_R8)
			CONDBR(!isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f == sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I4_S)
			BRELOP_S(i, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I8_S)
			BRELOP_S(l, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_R8_S)
			CONDBR_S(!isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f >= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I4)
			BRELOP(i, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_I8)
			BRELOP(l, >=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_R8)
			CONDBR(!isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f >= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I4_S)
			BRELOP_S(i, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I8_S)
			BRELOP_S(l, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_R8_S)
			CONDBR_S(!isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f > sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I4)
			BRELOP(i, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_I8)
			BRELOP(l, >)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_R8)
			CONDBR(!isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f > sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I4_S)
			BRELOP_S(i, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I8_S)
			BRELOP_S(l, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_R8_S)
			CONDBR_S(!isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f < sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I4)
			BRELOP(i, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_I8)
			BRELOP(l, <)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_R8)
			CONDBR(!isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f < sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I4_S)
			BRELOP_S(i, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I8_S)
			BRELOP_S(l, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_R8_S)
			CONDBR_S(!isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f <= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I4)
			BRELOP(i, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_I8)
			BRELOP(l, <=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_R8)
			CONDBR(!isunordered (sp [0].data.f, sp [1].data.f) && sp[0].data.f <= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I4_S)
			BRELOP_S(i, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I8_S)
			BRELOP_S(l, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_R8_S)
			CONDBR_S(isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f != sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I4)
			BRELOP(i, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_I8)
			BRELOP(l, !=)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BNE_UN_R8)
			CONDBR(isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f != sp[1].data.f)
			MINT_IN_BREAK;

#define BRELOP_S_CAST(datamem, op, type) \
	sp -= 2; \
	if ((type) sp[0].data.datamem op (type) sp[1].data.datamem) \
		ip += * (gint16 *)(ip + 1); \
	else \
		ip += 2;

#define BRELOP_CAST(datamem, op, type) \
	sp -= 2; \
	if ((type) sp[0].data.datamem op (type) sp[1].data.datamem) \
		ip += READ32(ip + 1); \
	else \
		ip += 3;

		MINT_IN_CASE(MINT_BGE_UN_I4_S)
			BRELOP_S_CAST(i, >=, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I8_S)
			BRELOP_S_CAST(l, >=, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_R8_S)
			CONDBR_S(isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f >= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I4)
			BRELOP_CAST(i, >=, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_I8)
			BRELOP_CAST(l, >=, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGE_UN_R8)
			CONDBR(isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f >= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I4_S)
			BRELOP_S_CAST(i, >, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I8_S)
			BRELOP_S_CAST(l, >, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_R8_S)
			CONDBR_S(isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f > sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I4)
			BRELOP_CAST(i, >, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_I8)
			BRELOP_CAST(l, >, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BGT_UN_R8)
			CONDBR(isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f > sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I4_S)
			BRELOP_S_CAST(i, <=, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I8_S)
			BRELOP_S_CAST(l, <=, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_R8_S)
			CONDBR_S(isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f <= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I4)
			BRELOP_CAST(i, <=, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_I8)
			BRELOP_CAST(l, <=, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLE_UN_R8)
			CONDBR(isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f <= sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I4_S)
			BRELOP_S_CAST(i, <, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I8_S)
			BRELOP_S_CAST(l, <, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_R8_S)
			CONDBR_S(isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f < sp[1].data.f)
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I4)
			BRELOP_CAST(i, <, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_I8)
			BRELOP_CAST(l, <, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BLT_UN_R8)
			CONDBR(isunordered (sp [0].data.f, sp [1].data.f) || sp[0].data.f < sp[1].data.f)
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
				ip = st + offset;
			} else {
				ip = st;
			}
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDIND_I1)
			++ip;
			sp[-1].data.i = *(gint8*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_U1)
			++ip;
			sp[-1].data.i = *(guint8*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_I2)
			++ip;
			sp[-1].data.i = *(gint16*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_U2)
			++ip;
			sp[-1].data.i = *(guint16*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_I4) /* Fall through */
		MINT_IN_CASE(MINT_LDIND_U4)
			++ip;
			sp[-1].data.i = *(gint32*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_I8)
			++ip;
			sp[-1].data.l = *(gint64*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_I) {
			guint16 offset = * (guint16 *)(ip + 1);
			sp[-1 - offset].data.p = *(gpointer*)sp[-1 - offset].data.p;
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDIND_R4)
			++ip;
			sp[-1].data.f = *(gfloat*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_R8)
			++ip;
			sp[-1].data.f = *(gdouble*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDIND_REF)
			++ip;
			sp[-1].data.p = *(gpointer*)sp[-1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_REF) 
			++ip;
			sp -= 2;
			mono_gc_wbarrier_generic_store (sp->data.p, sp [1].data.p);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I1)
			++ip;
			sp -= 2;
			* (gint8 *) sp->data.p = (gint8)sp[1].data.i;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I2)
			++ip;
			sp -= 2;
			* (gint16 *) sp->data.p = (gint16)sp[1].data.i;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I4)
			++ip;
			sp -= 2;
			* (gint32 *) sp->data.p = sp[1].data.i;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I)
			++ip;
			sp -= 2;
			* (mono_i *) sp->data.p = (mono_i)sp[1].data.p;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_I8)
			++ip;
			sp -= 2;
			* (gint64 *) sp->data.p = sp[1].data.l;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_R4)
			++ip;
			sp -= 2;
			* (float *) sp->data.p = (gfloat)sp[1].data.f;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STIND_R8)
			++ip;
			sp -= 2;
			* (double *) sp->data.p = sp[1].data.f;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_ATOMIC_STORE_I4)
			++ip;
			sp -= 2;
			InterlockedWrite ((gint32 *) sp->data.p, sp [1].data.i);
			MINT_IN_BREAK;
#define BINOP(datamem, op) \
	--sp; \
	sp [-1].data.datamem = sp [-1].data.datamem op sp [0].data.datamem; \
	++ip;
		MINT_IN_CASE(MINT_ADD_I4)
			BINOP(i, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_I8)
			BINOP(l, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD_R8)
			BINOP(f, +);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ADD1_I4)
			++sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_I4)
			BINOP(i, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_I8)
			BINOP(l, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB_R8)
			BINOP(f, -);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_SUB1_I4)
			--sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_I4)
			BINOP(i, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_I8)
			BINOP(l, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MUL_R8)
			BINOP(f, *);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DIV_I4)
			if (sp [-1].data.i == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (sp [-1].data.i == (-1))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(i, /);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DIV_I8)
			if (sp [-1].data.l == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (sp [-1].data.l == (-1))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(l, /);
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
		MINT_IN_CASE(MINT_REM_I4)
			if (sp [-1].data.i == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (sp [-1].data.i == (-1))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(i, %);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_REM_I8)
			if (sp [-1].data.l == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
			if (sp [-1].data.l == (-1))
				THROW_EX (mono_get_exception_overflow (), ip);
			BINOP(l, %);
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
	sp [-1].data.datamem = sp [-1].data.datamem op sp [0].data.i; \
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
		MINT_IN_CASE(MINT_CONV_I1_R8)
			sp [-1].data.i = (gint8)sp [-1].data.f;
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
		MINT_IN_CASE(MINT_CONV_U1_R8)
			sp [-1].data.i = (guint8)sp [-1].data.f;
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
		MINT_IN_CASE(MINT_CONV_I2_R8)
			sp [-1].data.i = (gint16)sp [-1].data.f;
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
		MINT_IN_CASE(MINT_CONV_U2_R8)
			sp [-1].data.i = (guint16)sp [-1].data.f;
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
		MINT_IN_CASE(MINT_CONV_U4_R8)
			/* needed on arm64 */
			if (isinf (sp [-1].data.f))
				sp [-1].data.i = 0;
			else
				sp [-1].data.i = (guint32)sp [-1].data.f;
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
		MINT_IN_CASE(MINT_CONV_I8_R8)
			sp [-1].data.l = (gint64)sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R4_I4)
			sp [-1].data.f = (float)sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R4_I8)
			sp [-1].data.f = (float)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R4_R8)
			sp [-1].data.f = (float)sp [-1].data.f;
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
		MINT_IN_CASE(MINT_CONV_U8_I4)
			sp [-1].data.l = sp [-1].data.i & 0xffffffff;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_U8_R8)
			sp [-1].data.l = (guint64)sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CPOBJ) {
			c = rtm->data_items[* (guint16 *)(ip + 1)];
			g_assert (c->byval_arg.type == MONO_TYPE_VALUETYPE);
			/* if this assertion fails, we need to add a write barrier */
			g_assert (!MONO_TYPE_IS_REFERENCE (&c->byval_arg));
			stackval_from_data (&c->byval_arg, &sp [-2], sp [-1].data.p, FALSE);
			ip += 2;
			sp -= 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDOBJ) {
			void *p;
			c = rtm->data_items[* (guint16 *)(ip + 1)];
			ip += 2;
			p = sp [-1].data.p;
			if (c->byval_arg.type == MONO_TYPE_VALUETYPE && !c->enumtype) {
				int size = mono_class_value_size (c, NULL);
				sp [-1].data.p = vt_sp;
				vt_sp += (size + 7) & ~7;
			}
			stackval_from_data (&c->byval_arg, &sp [-1], p, FALSE);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSTR)
			sp->data.p = rtm->data_items [* (guint16 *)(ip + 1)];
			++sp;
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NEWOBJ) {
			MonoClass *newobj_class;
			MonoMethodSignature *csig;
			stackval valuetype_this;
			guint32 token;
			stackval retval;

			frame->ip = ip;

			token = * (guint16 *)(ip + 1);
			ip += 2;

			child_frame.ip = NULL;
			child_frame.ex = NULL;

			child_frame.runtime_method = rtm->data_items [token];
			csig = mono_method_signature (child_frame.runtime_method->method);
			newobj_class = child_frame.runtime_method->method->klass;
			/*if (profiling_classes) {
				guint count = GPOINTER_TO_UINT (g_hash_table_lookup (profiling_classes, newobj_class));
				count++;
				g_hash_table_insert (profiling_classes, newobj_class, GUINT_TO_POINTER (count));
			}*/

			if (newobj_class->parent == mono_defaults.array_class) {
				sp -= csig->param_count;
				child_frame.stack_args = sp;
				o = ves_array_create (&child_frame, context->domain, newobj_class, csig, sp);
				if (child_frame.ex)
					THROW_EX (child_frame.ex, ip);
				goto array_constructed;
			}

			g_assert (csig->hasthis);
			if (csig->param_count) {
				sp -= csig->param_count;
				memmove (sp + 1, sp, csig->param_count * sizeof (stackval));
			}
			child_frame.stack_args = sp;

			/*
			 * First arg is the object.
			 */
			if (newobj_class->valuetype) {
				MonoType *t = &newobj_class->byval_arg;
				memset (&valuetype_this, 0, sizeof (stackval));
				if (!newobj_class->enumtype && (t->type == MONO_TYPE_VALUETYPE || (t->type == MONO_TYPE_GENERICINST && mono_type_generic_inst_is_valuetype (t)))) {
					sp->data.p = vt_sp;
					valuetype_this.data.p = vt_sp;
				} else {
					sp->data.p = &valuetype_this;
				}
			} else {
				if (newobj_class != mono_defaults.string_class) {
					context->managed_code = 0;
					o = mono_object_new_checked (context->domain, newobj_class, &error);
					mono_error_cleanup (&error); /* FIXME: don't swallow the error */
					context->managed_code = 1;
					if (*mono_thread_interruption_request_flag ())
						mono_thread_interruption_checkpoint ();
					sp->data.p = o;
				} else {
					sp->data.p = NULL;
					child_frame.retval = &retval;
				}
			}

			g_assert (csig->call_convention == MONO_CALL_DEFAULT);

			ves_exec_method_with_context (&child_frame, context, NULL, NULL, -1);

			context->current_frame = frame;

			if (context->has_resume_state) {
				if (frame == context->handler_frame)
					SET_RESUME_STATE (context);
				else
					goto exit_frame;
			}

			if (child_frame.ex) {
				/*
				 * An exception occurred, need to run finally, fault and catch handlers..
				 */
				frame->ex = child_frame.ex;
				goto handle_finally;
			}
			/*
			 * a constructor returns void, but we need to return the object we created
			 */
array_constructed:
			if (newobj_class->valuetype && !newobj_class->enumtype) {
				*sp = valuetype_this;
			} else if (newobj_class == mono_defaults.string_class) {
				*sp = retval;
			} else {
				sp->data.p = o;
			}
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CASTCLASS)
			c = rtm->data_items [*(guint16 *)(ip + 1)];
			if ((o = sp [-1].data.p)) {
				MonoObject *isinst_obj = mono_object_isinst_checked (o, c, &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
				if (!isinst_obj)
					THROW_EX (mono_get_exception_invalid_cast (), ip);
			}
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ISINST)
			c = rtm->data_items [*(guint16 *)(ip + 1)];
			if ((o = sp [-1].data.p)) {
				MonoObject *isinst_obj = mono_object_isinst_checked (o, c, &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
				if (!isinst_obj)
					sp [-1].data.p = NULL;
			}
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R_UN_I4)
			sp [-1].data.f = (double)(guint32)sp [-1].data.i;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_R_UN_I8)
			sp [-1].data.f = (double)(guint64)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_UNBOX)
			c = rtm->data_items[*(guint16 *)(ip + 1)];
			
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);

			MonoObject *isinst_obj = mono_object_isinst_checked (o, c, &error);
			mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			if (!(isinst_obj || ((o->vtable->klass->rank == 0) && (o->vtable->klass->element_class == c->element_class))))
				THROW_EX (mono_get_exception_invalid_cast (), ip);

			sp [-1].data.p = mono_object_unbox (o);
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_THROW)
			--sp;
			frame->ex_handler = NULL;
			if (!sp->data.p)
				sp->data.p = mono_get_exception_null_reference ();
			THROW_EX ((MonoException *)sp->data.p, ip);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLDA_UNSAFE)
			o = sp [-1].data.p;
			sp[-1].data.p = (char *)o + * (guint16 *)(ip + 1);
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLDA)
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			sp[-1].data.p = (char *)o + * (guint16 *)(ip + 1);
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CKNULL)
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			++ip;
			MINT_IN_BREAK;

#define LDFLD(datamem, fieldtype) \
	o = sp [-1].data.p; \
	if (!o) \
		THROW_EX (mono_get_exception_null_reference (), ip); \
	sp[-1].data.datamem = * (fieldtype *)((char *)o + * (guint16 *)(ip + 1)) ; \
	ip += 2;

		MINT_IN_CASE(MINT_LDFLD_I1) LDFLD(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_U1) LDFLD(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I2) LDFLD(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_U2) LDFLD(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I4) LDFLD(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_I8) LDFLD(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_R4) LDFLD(f, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_R8) LDFLD(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_O) LDFLD(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFLD_P) LDFLD(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDFLD_VT)
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			i32 = READ32(ip + 2);
			sp [-1].data.p = vt_sp;
			memcpy(sp [-1].data.p, (char *)o + * (guint16 *)(ip + 1), i32);
			vt_sp += (i32 + 7) & ~7;
			ip += 4;
			MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDRMFLD) {
			gpointer tmp;
			MonoClassField *field;
			char *addr;

			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			field = rtm->data_items[* (guint16 *)(ip + 1)];
			ip += 2;
			if (mono_object_is_transparent_proxy (o)) {
				MonoClass *klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;

				addr = mono_load_remote_field_checked (o, klass, field, &tmp, &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			} else {
				addr = (char*)o + field->offset;
			}				

			stackval_from_data (field->type, &sp [-1], addr, FALSE);
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LDRMFLD_VT) {
			MonoClassField *field;
			char *addr;
			gpointer tmp;

			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			field = rtm->data_items[* (guint16 *)(ip + 1)];
			i32 = READ32(ip + 2);
			ip += 4;
			if (mono_object_is_transparent_proxy (o)) {
				MonoClass *klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;
				addr = mono_load_remote_field_checked (o, klass, field, &tmp, &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			} else {
				addr = (char*)o + field->offset;
			}				

			sp [-1].data.p = vt_sp;
			memcpy(sp [-1].data.p, (char *)o + * (guint16 *)(ip + 1), i32);
			vt_sp += (i32 + 7) & ~7;
			memcpy(sp [-1].data.p, addr, i32);
			MINT_IN_BREAK;
		}

#define STFLD(datamem, fieldtype) \
	o = sp [-2].data.p; \
	if (!o) \
		THROW_EX (mono_get_exception_null_reference (), ip); \
	sp -= 2; \
	* (fieldtype *)((char *)o + * (guint16 *)(ip + 1)) = sp[1].data.datamem; \
	ip += 2;

		MINT_IN_CASE(MINT_STFLD_I1) STFLD(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_U1) STFLD(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_I2) STFLD(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_U2) STFLD(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_I4) STFLD(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_I8) STFLD(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_R4) STFLD(f, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_R8) STFLD(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_P) STFLD(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_O)
			o = sp [-2].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			sp -= 2;
			mono_gc_wbarrier_set_field (o, (char *) o + * (guint16 *)(ip + 1), sp [1].data.p);
			ip += 2;
			MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STFLD_VT)
			o = sp [-2].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			i32 = READ32(ip + 2);
			sp -= 2;
			memcpy((char *)o + * (guint16 *)(ip + 1), sp [1].data.p, i32);
			vt_sp -= (i32 + 7) & ~7;
			ip += 4;
			MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STRMFLD) {
			MonoClassField *field;

			o = sp [-2].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			
			field = rtm->data_items[* (guint16 *)(ip + 1)];
			ip += 2;

			if (mono_object_is_transparent_proxy (o)) {
				MonoClass *klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;
				mono_store_remote_field_checked (o, klass, field, &sp [-1].data, &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			} else
				stackval_to_data (field->type, &sp [-1], (char*)o + field->offset, FALSE);

			sp -= 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STRMFLD_VT) {
			MonoClassField *field;

			o = sp [-2].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			field = rtm->data_items[* (guint16 *)(ip + 1)];
			i32 = READ32(ip + 2);
			ip += 4;

			if (mono_object_is_transparent_proxy (o)) {
				MonoClass *klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;
				mono_store_remote_field_checked (o, klass, field, &sp [-1].data, &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			} else
				memcpy((char*)o + field->offset, sp [-1].data.p, i32);

			sp -= 2;
			vt_sp -= (i32 + 7) & ~7;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSFLDA) {
			MonoClassField *field = rtm->data_items[*(guint16 *)(ip + 1)];
			sp->data.p = mono_class_static_field_address (context->domain, field);
			ip += 2;
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSFLD) {
			MonoClassField *field = rtm->data_items [* (guint16 *)(ip + 1)];
			gpointer addr = mono_class_static_field_address (context->domain, field);
			stackval_from_data (field->type, sp, addr, FALSE);
			ip += 2;
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSFLD_VT) {
			MonoClassField *field = rtm->data_items [* (guint16 *)(ip + 1)];
			gpointer addr = mono_class_static_field_address (context->domain, field);
			int size = READ32 (ip + 2);
			ip += 4;

			sp->data.p = vt_sp;
			vt_sp += (size + 7) & ~7;
			stackval_from_data (field->type, sp, addr, FALSE);
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STSFLD) {
			MonoClassField *field = rtm->data_items [* (guint16 *)(ip + 1)];
			gpointer addr = mono_class_static_field_address (context->domain, field);
			ip += 2;
			--sp;
			stackval_to_data (field->type, sp, addr, FALSE);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STSFLD_VT) {
			MonoClassField *field = rtm->data_items [* (guint16 *)(ip + 1)];
			gpointer addr = mono_class_static_field_address (context->domain, field);
			int size = READ32 (ip + 2);
			ip += 4;

			--sp;
			stackval_to_data (field->type, sp, addr, FALSE);
			vt_sp -= (size + 7) & ~7;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STOBJ_VT) {
			int size;
			c = rtm->data_items[* (guint16 *)(ip + 1)];
			ip += 2;
			size = mono_class_value_size (c, NULL);
			memcpy(sp [-2].data.p, sp [-1].data.p, size);
			vt_sp -= (size + 7) & ~7;
			sp -= 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STOBJ) {
			c = rtm->data_items[* (guint16 *)(ip + 1)];
			ip += 2;

			g_assert (!c->byval_arg.byref);
			if (MONO_TYPE_IS_REFERENCE (&c->byval_arg))
				mono_gc_wbarrier_generic_store (sp [-2].data.p, sp [-1].data.p);
			else
				stackval_from_data (&c->byval_arg, sp [-2].data.p, (char *) &sp [-1].data.p, FALSE);
			sp -= 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_UN_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > MYGUINT32_MAX)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint32)sp [-1].data.f;
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
			if ((guint64) sp [-1].data.l > MYGINT64_MAX)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U8_R8)
		MINT_IN_CASE(MINT_CONV_OVF_I8_UN_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > MYGINT64_MAX)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.l = (guint64)sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I8_R8)
			if (sp [-1].data.f < MYGINT64_MIN || sp [-1].data.f > MYGINT64_MAX)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.l = (gint64)sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I4_UN_I8)
			if ((mono_u)sp [-1].data.l > MYGUINT32_MAX)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (mono_u)sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_BOX) {
			c = rtm->data_items [* (guint16 *)(ip + 1)];
			guint16 offset = * (guint16 *)(ip + 2);

			if (c->byval_arg.type == MONO_TYPE_VALUETYPE && !c->enumtype) {
				int size = mono_class_value_size (c, NULL);
				sp [-1 - offset].data.p = mono_value_box_checked (context->domain, c, sp [-1 - offset].data.p, &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
				size = (size + 7) & ~7;
				vt_sp -= size;
			} else {
				stackval_to_data (&c->byval_arg, &sp [-1 - offset], (char *) &sp [-1 - offset], FALSE);
				sp [-1 - offset].data.p = mono_value_box_checked (context->domain, c, &sp [-1 - offset], &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			}
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_NEWARR)
			sp [-1].data.p = (MonoObject*) mono_array_new_checked (context->domain, rtm->data_items[*(guint16 *)(ip + 1)], sp [-1].data.i, &error);
			if (!mono_error_ok (&error)) {
				THROW_EX (mono_error_convert_to_exception (&error), ip);
			}
			mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			ip += 2;
			/*if (profiling_classes) {
				guint count = GPOINTER_TO_UINT (g_hash_table_lookup (profiling_classes, o->vtable->klass));
				count++;
				g_hash_table_insert (profiling_classes, o->vtable->klass, GUINT_TO_POINTER (count));
			}*/

			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLEN)
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			sp [-1].data.nati = mono_array_length ((MonoArray *)o);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_GETCHR) {
			MonoString *s;
			s = sp [-2].data.p;
			if (!s)
				THROW_EX (mono_get_exception_null_reference (), ip);
			i32 = sp [-1].data.i;
			if (i32 < 0 || i32 >= mono_string_length (s))
				THROW_EX (mono_get_exception_index_out_of_range (), ip);
			--sp;
			sp [-1].data.i = mono_string_chars(s)[i32];
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STRLEN)
			++ip;
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			sp [-1].data.i = mono_string_length ((MonoString*) o);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ARRAY_RANK)
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			sp [-1].data.i = mono_object_class (sp [-1].data.p)->rank;
			ip++;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEMA)
		MINT_IN_CASE(MINT_LDELEMA_TC) {
			gboolean needs_typecheck = *ip == MINT_LDELEMA_TC;
			
			MonoClass *klass = rtm->data_items [*(guint16 *) (ip + 1)];
			guint16 numargs = *(guint16 *) (ip + 2);
			ip += 3;
			sp -= numargs;

			o = sp [0].data.p;
			sp->data.p = ves_array_element_address (frame, klass, (MonoArray *) o, &sp [1], needs_typecheck);
			if (frame->ex)
				THROW_EX (frame->ex, ip);
			++sp;

			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDELEM_I1) /* fall through */
		MINT_IN_CASE(MINT_LDELEM_U1) /* fall through */
		MINT_IN_CASE(MINT_LDELEM_I2) /* fall through */
		MINT_IN_CASE(MINT_LDELEM_U2) /* fall through */
		MINT_IN_CASE(MINT_LDELEM_I4) /* fall through */
		MINT_IN_CASE(MINT_LDELEM_U4) /* fall through */
		MINT_IN_CASE(MINT_LDELEM_I8)  /* fall through */
		MINT_IN_CASE(MINT_LDELEM_I)  /* fall through */
		MINT_IN_CASE(MINT_LDELEM_R4) /* fall through */
		MINT_IN_CASE(MINT_LDELEM_R8) /* fall through */
		MINT_IN_CASE(MINT_LDELEM_REF) /* fall through */
		MINT_IN_CASE(MINT_LDELEM_VT) {
			MonoArray *o;
			mono_u aindex;

			sp -= 2;

			o = sp [0].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);

			aindex = sp [1].data.i;
			if (aindex >= mono_array_length (o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip);

			/*
			 * FIXME: throw mono_get_exception_array_type_mismatch () if needed 
			 */
			switch (*ip) {
			case MINT_LDELEM_I1:
				sp [0].data.i = mono_array_get (o, gint8, aindex);
				break;
			case MINT_LDELEM_U1:
				sp [0].data.i = mono_array_get (o, guint8, aindex);
				break;
			case MINT_LDELEM_I2:
				sp [0].data.i = mono_array_get (o, gint16, aindex);
				break;
			case MINT_LDELEM_U2:
				sp [0].data.i = mono_array_get (o, guint16, aindex);
				break;
			case MINT_LDELEM_I:
				sp [0].data.nati = mono_array_get (o, mono_i, aindex);
				break;
			case MINT_LDELEM_I4:
				sp [0].data.i = mono_array_get (o, gint32, aindex);
				break;
			case MINT_LDELEM_U4:
				sp [0].data.i = mono_array_get (o, guint32, aindex);
				break;
			case MINT_LDELEM_I8:
				sp [0].data.l = mono_array_get (o, guint64, aindex);
				break;
			case MINT_LDELEM_R4:
				sp [0].data.f = mono_array_get (o, float, aindex);
				break;
			case MINT_LDELEM_R8:
				sp [0].data.f = mono_array_get (o, double, aindex);
				break;
			case MINT_LDELEM_REF:
				sp [0].data.p = mono_array_get (o, gpointer, aindex);
				break;
			case MINT_LDELEM_VT: {
				MonoClass *klass_vt = rtm->data_items [*(guint16 *) (ip + 1)];
				i32 = READ32 (ip + 2);
				char *src_addr = mono_array_addr_with_size ((MonoArray *) o, i32, aindex);
				sp [0].data.vt = vt_sp;
				stackval_from_data (&klass_vt->byval_arg, sp, src_addr, FALSE);
				vt_sp += (i32 + 7) & ~7;
				ip += 3;
				break;
			}
			default:
				ves_abort();
			}

			++ip;
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STELEM_I)  /* fall through */
		MINT_IN_CASE(MINT_STELEM_I1) /* fall through */ 
		MINT_IN_CASE(MINT_STELEM_U1) /* fall through */
		MINT_IN_CASE(MINT_STELEM_I2) /* fall through */
		MINT_IN_CASE(MINT_STELEM_U2) /* fall through */
		MINT_IN_CASE(MINT_STELEM_I4) /* fall through */
		MINT_IN_CASE(MINT_STELEM_I8) /* fall through */
		MINT_IN_CASE(MINT_STELEM_R4) /* fall through */
		MINT_IN_CASE(MINT_STELEM_R8) /* fall through */
		MINT_IN_CASE(MINT_STELEM_REF) /* fall through */
		MINT_IN_CASE(MINT_STELEM_VT) {
			mono_u aindex;

			sp -= 3;

			o = sp [0].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);

			aindex = sp [1].data.i;
			if (aindex >= mono_array_length ((MonoArray *)o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip);

			switch (*ip) {
			case MINT_STELEM_I:
				mono_array_set ((MonoArray *)o, mono_i, aindex, sp [2].data.nati);
				break;
			case MINT_STELEM_I1:
				mono_array_set ((MonoArray *)o, gint8, aindex, sp [2].data.i);
				break;
			case MINT_STELEM_U1:
				mono_array_set ((MonoArray *) o, guint8, aindex, sp [2].data.i);
				break;
			case MINT_STELEM_I2:
				mono_array_set ((MonoArray *)o, gint16, aindex, sp [2].data.i);
				break;
			case MINT_STELEM_U2:
				mono_array_set ((MonoArray *)o, guint16, aindex, sp [2].data.i);
				break;
			case MINT_STELEM_I4:
				mono_array_set ((MonoArray *)o, gint32, aindex, sp [2].data.i);
				break;
			case MINT_STELEM_I8:
				mono_array_set ((MonoArray *)o, gint64, aindex, sp [2].data.l);
				break;
			case MINT_STELEM_R4:
				mono_array_set ((MonoArray *)o, float, aindex, sp [2].data.f);
				break;
			case MINT_STELEM_R8:
				mono_array_set ((MonoArray *)o, double, aindex, sp [2].data.f);
				break;
			case MINT_STELEM_REF: {
				MonoObject *isinst_obj = mono_object_isinst_checked (sp [2].data.p, mono_object_class (o)->element_class, &error);
				mono_error_cleanup (&error); /* FIXME: don't swallow the error */
				if (sp [2].data.p && !isinst_obj)
					THROW_EX (mono_get_exception_array_type_mismatch (), ip);
				mono_array_setref ((MonoArray *) o, aindex, sp [2].data.p);
				break;
			}
			case MINT_STELEM_VT: {
				MonoClass *klass_vt = rtm->data_items [*(guint16 *) (ip + 1)];
				i32 = READ32 (ip + 2);
				char *dst_addr = mono_array_addr_with_size ((MonoArray *) o, i32, aindex);

				stackval_to_data (&klass_vt->byval_arg, &sp [2], dst_addr, FALSE);
				vt_sp -= (i32 + 7) & ~7;
				ip += 3;
				break;
			}
			default:
				ves_abort();
			}

			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_CONV_OVF_I4_U4)
			if (sp [-1].data.i < 0)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I4_I8)
			if (sp [-1].data.l < MYGINT32_MIN || sp [-1].data.l > MYGINT32_MAX)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint32) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I4_U8)
			if (sp [-1].data.l < 0 || sp [-1].data.l > MYGINT32_MAX)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint32) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I4_R8)
			if (sp [-1].data.f < MYGINT32_MIN || sp [-1].data.f > MYGINT32_MAX)
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
			if (sp [-1].data.l < 0 || sp [-1].data.l > MYGUINT32_MAX)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint32) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U4_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > MYGUINT32_MAX)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint32) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_I4)
			if (sp [-1].data.i < -32768 || sp [-1].data.i > 32767)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_I8)
			if (sp [-1].data.l < -32768 || sp [-1].data.l > 32767)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint16) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I2_R8)
			if (sp [-1].data.f < -32768 || sp [-1].data.f > 32767)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint16) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U2_I4)
			if (sp [-1].data.i < 0 || sp [-1].data.i > 65535)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U2_I8)
			if (sp [-1].data.l < 0 || sp [-1].data.l > 65535)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint16) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U2_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > 65535)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint16) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_I4)
			if (sp [-1].data.i < -128 || sp [-1].data.i > 127)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_I8)
			if (sp [-1].data.l < -128 || sp [-1].data.l > 127)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint8) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_I1_R8)
			if (sp [-1].data.f < -128 || sp [-1].data.f > 127)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (gint8) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U1_I4)
			if (sp [-1].data.i < 0 || sp [-1].data.i > 255)
				THROW_EX (mono_get_exception_overflow (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U1_I8)
			if (sp [-1].data.l < 0 || sp [-1].data.l > 255)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint8) sp [-1].data.l;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CONV_OVF_U1_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > 255)
				THROW_EX (mono_get_exception_overflow (), ip);
			sp [-1].data.i = (guint8) sp [-1].data.f;
			++ip;
			MINT_IN_BREAK;
#if 0
		MINT_IN_CASE(MINT_LDELEM) 
		MINT_IN_CASE(MINT_STELEM) 
		MINT_IN_CASE(MINT_UNBOX_ANY) 
#endif
		MINT_IN_CASE(MINT_CKFINITE)
			if (!isfinite(sp [-1].data.f))
				THROW_EX (mono_get_exception_arithmetic (), ip);
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MKREFANY) {
			c = rtm->data_items [*(guint16 *)(ip + 1)];

			/* The value address is on the stack */
			gpointer addr = sp [-1].data.p;
			/* Push the typedref value on the stack */
			sp [-1].data.p = vt_sp;
			vt_sp += sizeof (MonoTypedRef);

			MonoTypedRef *tref = sp [-1].data.p;
			tref->klass = c;
			tref->type = &c->byval_arg;
			tref->value = addr;

			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REFANYTYPE) {
			MonoTypedRef *tref = sp [-1].data.p;
			MonoType *type = tref->type;

			vt_sp -= sizeof (MonoTypedRef);
			sp [-1].data.p = vt_sp;
			vt_sp += 8;
			*(gpointer*)sp [-1].data.p = type;
			ip ++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_REFANYVAL) {
			MonoTypedRef *tref = sp [-1].data.p;
			gpointer addr = tref->value;

			vt_sp -= sizeof (MonoTypedRef);

			sp [-1].data.p = addr;
			ip ++;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDTOKEN)
			sp->data.p = vt_sp;
			vt_sp += 8;
			* (gpointer *)sp->data.p = rtm->data_items[*(guint16 *)(ip + 1)];
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
		MINT_IN_CASE(MINT_ENDFINALLY)
			ip ++;
			int clause_index = *ip;
			if (clause_index == exit_at_finally)
				goto exit_frame;
			while (sp > frame->stack) {
				--sp;
			}
			if (finally_ips) {
				ip = finally_ips->data;
				finally_ips = g_slist_remove (finally_ips, ip);
				goto main_loop;
			}
			if (frame->ex)
				goto handle_fault;
			ves_abort();
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LEAVE) /* Fall through */
		MINT_IN_CASE(MINT_LEAVE_S)
			while (sp > frame->stack) {
				--sp;
			}
			frame->ip = ip;
			if (*ip == MINT_LEAVE_S) {
				ip += (short) *(ip + 1);
			} else {
				ip += (gint32) READ32 (ip + 1);
			}
			endfinally_ip = ip;
			if (frame->ex_handler != NULL && MONO_OFFSET_IN_HANDLER(frame->ex_handler, frame->ip - rtm->code)) {
				frame->ex_handler = NULL;
				frame->ex = NULL;
			}
			goto handle_finally;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ICALL_V_V) 
		MINT_IN_CASE(MINT_ICALL_V_P)
		MINT_IN_CASE(MINT_ICALL_P_V) 
		MINT_IN_CASE(MINT_ICALL_P_P)
		MINT_IN_CASE(MINT_ICALL_PP_V)
		MINT_IN_CASE(MINT_ICALL_PI_V)
		MINT_IN_CASE(MINT_ICALL_PP_P)
		MINT_IN_CASE(MINT_ICALL_PI_P)
		MINT_IN_CASE(MINT_ICALL_PPP_V)
		MINT_IN_CASE(MINT_ICALL_PPI_V)
			sp = do_icall (context, *ip, sp, rtm->data_items [*(guint16 *)(ip + 1)]);
			if (*mono_thread_interruption_request_flag ()) {
				MonoException *exc = mono_thread_interruption_checkpoint ();
				if (exc) {
					frame->ex = exc;
					context->search_for_handler = 1;
				}
			}
			if (frame->ex != NULL)
				goto handle_exception;
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_LDPTR) 
			sp->data.p = rtm->data_items [*(guint16 *)(ip + 1)];
			ip += 2;
			++sp;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_NEWOBJ)
			sp->data.p = mono_object_new_checked (context->domain, rtm->data_items [*(guint16 *)(ip + 1)], &error);
			mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			ip += 2;
			sp++;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_FREE)
			++ip;
			--sp;
			g_error ("that doesn't seem right");
			g_free (sp->data.p);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_RETOBJ)
			++ip;
			sp--;
			stackval_from_data (mono_method_signature (frame->runtime_method->method)->ret, frame->retval, sp->data.p,
			     mono_method_signature (frame->runtime_method->method)->pinvoke);
			if (sp > frame->stack)
				g_warning ("retobj: more values on stack: %d", sp-frame->stack);
			goto exit_frame;
		MINT_IN_CASE(MINT_MONO_TLS) {
			MonoTlsKey key = *(gint32 *)(ip + 1);
			sp->data.p = ((gpointer (*)()) mono_tls_get_tls_getter (key, FALSE)) ();
			sp++;
			ip += 3;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_MONO_JIT_ATTACH) {
			++ip;

			context->original_domain = NULL;
			MonoDomain *tls_domain = (MonoDomain *) ((gpointer (*)()) mono_tls_get_tls_getter (TLS_KEY_DOMAIN, FALSE)) ();
			gpointer tls_jit = ((gpointer (*)()) mono_tls_get_tls_getter (TLS_KEY_DOMAIN, FALSE)) ();

			if (tls_domain != context->domain || !tls_jit)
				context->original_domain = mono_jit_thread_attach (context->domain);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_MONO_JIT_DETACH)
			++ip;
			mono_jit_set_domain (context->original_domain);
			MINT_IN_BREAK;

#define RELOP(datamem, op) \
	--sp; \
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
		MINT_IN_CASE(MINT_CEQ_R8)
			--sp; 
			if (isunordered (sp [-1].data.f, sp [0].data.f))
				sp [-1].data.i = 0;
			else
				sp [-1].data.i = sp [-1].data.f == sp [0].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_I4)
			RELOP(i, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_I8)
			RELOP(l, >);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_R8)
			--sp; 
			if (isunordered (sp [-1].data.f, sp [0].data.f))
				sp [-1].data.i = 0;
			else
				sp [-1].data.i = sp [-1].data.f > sp [0].data.f;
			++ip;
			MINT_IN_BREAK;

#define RELOP_CAST(datamem, op, type) \
	--sp; \
	sp [-1].data.i = (type)sp [-1].data.datamem op (type)sp [0].data.datamem; \
	++ip;

		MINT_IN_CASE(MINT_CGT_UN_I4)
			RELOP_CAST(i, >, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_UN_I8)
			RELOP_CAST(l, >, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CGT_UN_R8)
			--sp; 
			if (isunordered (sp [-1].data.f, sp [0].data.f))
				sp [-1].data.i = 1;
			else
				sp [-1].data.i = sp [-1].data.f > sp [0].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_I4)
			RELOP(i, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_I8)
			RELOP(l, <);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_R8)
			--sp; 
			if (isunordered (sp [-1].data.f, sp [0].data.f))
				sp [-1].data.i = 0;
			else
				sp [-1].data.i = sp [-1].data.f < sp [0].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_I4)
			RELOP_CAST(i, <, guint32);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_I8)
			RELOP_CAST(l, <, guint64);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_CLT_UN_R8)
			--sp; 
			if (isunordered (sp [-1].data.f, sp [0].data.f))
				sp [-1].data.i = 1;
			else
				sp [-1].data.i = sp [-1].data.f < sp [0].data.f;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDFTN) {
			sp->data.p = rtm->data_items [* (guint16 *)(ip + 1)];
			++sp;
			ip += 2;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDVIRTFTN) {
			RuntimeMethod *m = rtm->data_items [* (guint16 *)(ip + 1)];
			ip += 2;
			--sp;
			if (!sp->data.p)
				THROW_EX (mono_get_exception_null_reference (), ip - 2);
				
			sp->data.p = get_virtual_method (context->domain, m, sp->data.p);
			++sp;
			MINT_IN_BREAK;
		}

#define LDARG(datamem, argtype) \
	sp->data.datamem = * (argtype *)(frame->args + * (guint16 *)(ip + 1)); \
	ip += 2; \
	++sp; 
	
		MINT_IN_CASE(MINT_LDARG_I1) LDARG(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDARG_U1) LDARG(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDARG_I2) LDARG(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDARG_U2) LDARG(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDARG_I4) LDARG(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDARG_I8) LDARG(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDARG_R4) LDARG(f, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDARG_R8) LDARG(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDARG_O) LDARG(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDARG_P) LDARG(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDARG_VT)
			sp->data.p = vt_sp;
			i32 = READ32(ip + 2);
			memcpy(sp->data.p, frame->args + * (guint16 *)(ip + 1), i32);
			vt_sp += (i32 + 7) & ~7;
			ip += 4;
			++sp;
			MINT_IN_BREAK;

#define STARG(datamem, argtype) \
	--sp; \
	* (argtype *)(frame->args + * (guint16 *)(ip + 1)) = sp->data.datamem; \
	ip += 2; \
	
		MINT_IN_CASE(MINT_STARG_I1) STARG(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STARG_U1) STARG(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STARG_I2) STARG(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STARG_U2) STARG(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STARG_I4) STARG(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STARG_I8) STARG(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STARG_R4) STARG(f, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STARG_R8) STARG(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STARG_O) STARG(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STARG_P) STARG(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STARG_VT) 
			i32 = READ32(ip + 2);
			--sp;
			memcpy(frame->args + * (guint16 *)(ip + 1), sp->data.p, i32);
			vt_sp -= (i32 + 7) & ~7;
			ip += 4;
			MINT_IN_BREAK;

#define STINARG(datamem, argtype) \
	do { \
		int n = * (guint16 *)(ip + 1); \
		* (argtype *)(frame->args + rtm->arg_offsets [n]) = frame->stack_args [n].data.datamem; \
		ip += 2; \
	} while (0)
	
		MINT_IN_CASE(MINT_STINARG_I1) STINARG(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STINARG_U1) STINARG(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STINARG_I2) STINARG(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STINARG_U2) STINARG(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STINARG_I4) STINARG(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STINARG_I8) STINARG(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STINARG_R4) STINARG(f, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STINARG_R8) STINARG(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STINARG_O) STINARG(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STINARG_P) STINARG(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STINARG_VT) {
			int n = * (guint16 *)(ip + 1);
			i32 = READ32(ip + 2);
			memcpy (frame->args + rtm->arg_offsets [n], frame->stack_args [n].data.p, i32);
			ip += 4;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LDARGA)
			sp->data.p = frame->args + * (guint16 *)(ip + 1);
			ip += 2;
			++sp;
			MINT_IN_BREAK;

#define LDLOC(datamem, argtype) \
	sp->data.datamem = * (argtype *)(locals + * (guint16 *)(ip + 1)); \
	ip += 2; \
	++sp; 
	
		MINT_IN_CASE(MINT_LDLOC_I1) LDLOC(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_U1) LDLOC(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_I2) LDLOC(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_U2) LDLOC(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_I4) LDLOC(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_I8) LDLOC(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_R4) LDLOC(f, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_R8) LDLOC(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_O) LDLOC(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDLOC_P) LDLOC(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDLOC_VT)
			sp->data.p = vt_sp;
			i32 = READ32(ip + 2);
			memcpy(sp->data.p, locals + * (guint16 *)(ip + 1), i32);
			vt_sp += (i32 + 7) & ~7;
			ip += 4;
			++sp;
			MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LDLOCA_S)
			sp->data.p = locals + * (guint16 *)(ip + 1);
			ip += 2;
			++sp;
			MINT_IN_BREAK;

#define STLOC(datamem, argtype) \
	--sp; \
	* (argtype *)(locals + * (guint16 *)(ip + 1)) = sp->data.datamem; \
	ip += 2;
	
		MINT_IN_CASE(MINT_STLOC_I1) STLOC(i, gint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_U1) STLOC(i, guint8); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_I2) STLOC(i, gint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_U2) STLOC(i, guint16); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_I4) STLOC(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_I8) STLOC(l, gint64); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_R4) STLOC(f, float); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_R8) STLOC(f, double); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_O) STLOC(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_P) STLOC(p, gpointer); MINT_IN_BREAK;

#define STLOC_NP(datamem, argtype) \
	* (argtype *)(locals + * (guint16 *)(ip + 1)) = sp [-1].data.datamem; \
	ip += 2;

		MINT_IN_CASE(MINT_STLOC_NP_I4) STLOC_NP(i, gint32); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STLOC_NP_O) STLOC_NP(p, gpointer); MINT_IN_BREAK;

		MINT_IN_CASE(MINT_STLOC_VT)
			i32 = READ32(ip + 2);
			--sp;
			memcpy(locals + * (guint16 *)(ip + 1), sp->data.p, i32);
			vt_sp -= (i32 + 7) & ~7;
			ip += 4;
			MINT_IN_BREAK;

		MINT_IN_CASE(MINT_LOCALLOC) {
			if (sp != frame->stack + 1) /*FIX?*/
				THROW_EX (mono_get_exception_execution_engine (NULL), ip);

			int len = sp [-1].data.i;
			sp [-1].data.p = alloca (len);
			MonoMethodHeader *header = mono_method_get_header_checked (frame->runtime_method->method, &error);
			mono_error_cleanup (&error); /* FIXME: don't swallow the error */
			if (header && header->init_locals)
				memset (sp [-1].data.p, 0, len);
			++ip;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_ENDFILTER)
			/* top of stack is result of filter */
			frame->retval = &sp [-1];
			goto exit_frame;
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
			if (!sp [0].data.p)
				THROW_EX (mono_get_exception_null_reference(), ip - 1);
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
		MINT_IN_CASE(MINT_RETHROW)
			/* 
			 * need to clarify what this should actually do:
			 * start the search from the last found handler in
			 * this method or continue in the caller or what.
			 * Also, do we need to run finally/fault handlers after a retrow?
			 * Well, this implementation will follow the usual search
			 * for an handler, considering the current ip as throw spot.
			 * We need to NULL frame->ex_handler for the later code to
			 * actually run the new found handler.
			 */
			frame->ex_handler = NULL;
			THROW_EX (frame->ex, ip - 1);
			MINT_IN_BREAK;
		MINT_IN_DEFAULT
			g_print ("Unimplemented opcode: %04x %s at 0x%x\n", *ip, mono_interp_opname[*ip], ip-rtm->code);
			THROW_EX (mono_get_exception_execution_engine ("Unimplemented opcode"), ip);
		}
	}

	g_assert_not_reached ();
	/*
	 * Exception handling code.
	 * The exception object is stored in frame->ex.
	 */

	handle_exception:
	{
		int i;
		guint32 ip_offset;
		MonoInvocation *inv;
		MonoExceptionClause *clause;
		/*char *message;*/
		MonoObject *ex_obj;

#if DEBUG_INTERP
		if (tracing)
			g_print ("* Handling exception '%s' at IL_%04x\n", 
				frame->ex == NULL ? "** Unknown **" : mono_object_class (frame->ex)->name, 
				rtm == NULL ? 0 : frame->ip - rtm->code);
#endif
		if (die_on_exception)
			goto die_on_ex;

		for (inv = frame; inv; inv = inv->parent) {
			MonoMethod *method;
			if (inv->runtime_method == NULL)
				continue;
			method = inv->runtime_method->method;
			if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
				continue;
			if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME))
				continue;
			if (inv->ip == NULL)
				continue;
			ip_offset = inv->ip - inv->runtime_method->code;
			inv->ex_handler = NULL; /* clear this in case we are trhowing an exception while handling one  - this one wins */
			for (i = 0; i < inv->runtime_method->num_clauses; ++i) {
				clause = &inv->runtime_method->clauses [i];
#if DEBUG_INTERP
				g_print ("* clause [%d]: %p\n", i, clause);
#endif
				if (!MONO_OFFSET_IN_CLAUSE (clause, ip_offset)) {
					continue;
				}
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
#if DEBUG_INTERP
					if (tracing)
						g_print ("* Filter found at '%s'\n", method->name);
#endif
					MonoInvocation dup_frame;
					stackval retval;
					memcpy (&dup_frame, inv, sizeof (MonoInvocation));
					dup_frame.retval = &retval;
					ves_exec_method_with_context (&dup_frame, context, inv->runtime_method->code + clause->data.filter_offset, frame->ex, -1);
					if (dup_frame.retval->data.i) {
#if DEBUG_INTERP
						if (tracing)
							g_print ("* Matched Filter at '%s'\n", method->name);
#endif
						inv->ex_handler = clause;
						goto handle_finally;
					}
				} else if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE) {
					MonoObject *isinst_obj = mono_object_isinst_checked ((MonoObject*)frame->ex, clause->data.catch_class, &error);
					mono_error_cleanup (&error); /* FIXME: don't swallow the error */
					if (isinst_obj) {
						/* 
						 * OK, we found an handler, now we need to execute the finally
						 * and fault blocks before branching to the handler code.
						 */
#if DEBUG_INTERP
						if (tracing)
							g_print ("* Found handler at '%s'\n", method->name);
#endif
						inv->ex_handler = clause;
						goto handle_finally;
					}
				}
			}
		}
		/*
		 * If we get here, no handler was found: print a stack trace.
		 */
		for (inv = frame; inv; inv = inv->parent) {
			if (inv->invoke_trap)
				goto handle_finally;
		}
die_on_ex:
		ex_obj = (MonoObject *) frame->ex;
		mono_unhandled_exception (ex_obj);
		MonoJitTlsData *jit_tls = (MonoJitTlsData *) mono_tls_get_jit_tls ();
		jit_tls->abort_func (ex_obj);
		g_assert_not_reached ();
	}
	handle_finally:
	{
		int i;
		guint32 ip_offset;
		MonoExceptionClause *clause;
		GSList *old_list = finally_ips;
		MonoMethod *method = frame->runtime_method->method;
		MonoMethodHeader *header = mono_method_get_header_checked (method, &error);
		mono_error_cleanup (&error); /* FIXME: don't swallow the error */
		
#if DEBUG_INTERP
		if (tracing)
			g_print ("* Handle finally IL_%04x\n", endfinally_ip == NULL ? 0 : endfinally_ip - rtm->code);
#endif
		if (rtm == NULL || (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) 
				|| (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME))) {
			goto exit_frame;
		}
		ip_offset = frame->ip - rtm->code;

		if (endfinally_ip != NULL)
			finally_ips = g_slist_prepend(finally_ips, (void *)endfinally_ip);
		for (i = 0; i < header->num_clauses; ++i)
			if (frame->ex_handler == &rtm->clauses [i])
				break;
		while (i > 0) {
			--i;
			clause = &rtm->clauses [i];
			if (MONO_OFFSET_IN_CLAUSE (clause, ip_offset) && (endfinally_ip == NULL || !(MONO_OFFSET_IN_CLAUSE (clause, endfinally_ip - rtm->code)))) {
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
					ip = rtm->code + clause->handler_offset;
					finally_ips = g_slist_prepend (finally_ips, (gpointer) ip);
#if DEBUG_INTERP
					if (tracing)
						g_print ("* Found finally at IL_%04x with exception: %s\n", clause->handler_offset, frame->ex? "yes": "no");
#endif
				}
			}
		}

		endfinally_ip = NULL;

		if (old_list != finally_ips && finally_ips) {
			ip = finally_ips->data;
			finally_ips = g_slist_remove (finally_ips, ip);
			sp = frame->stack; /* spec says stack should be empty at endfinally so it should be at the start too */
			goto main_loop;
		}

		/*
		 * If an exception is set, we need to execute the fault handler, too,
		 * otherwise, we continue normally.
		 */
		if (frame->ex)
			goto handle_fault;
		ves_abort();
	}
	handle_fault:
	{
		int i;
		guint32 ip_offset;
		MonoExceptionClause *clause;
		MonoMethod *method = frame->runtime_method->method;
		MonoMethodHeader *header = mono_method_get_header_checked (method, &error);
		mono_error_cleanup (&error); /* FIXME: don't swallow the error */
		
#if DEBUG_INTERP
		if (tracing)
			g_print ("* Handle fault\n");
#endif
		ip_offset = frame->ip - rtm->code;
		for (i = 0; i < header->num_clauses; ++i) {
			clause = &rtm->clauses [i];
			if (clause->flags == MONO_EXCEPTION_CLAUSE_FAULT && MONO_OFFSET_IN_CLAUSE (clause, ip_offset)) {
				ip = rtm->code + clause->handler_offset;
#if DEBUG_INTERP
				if (tracing)
					g_print ("* Executing handler at IL_%04x\n", clause->handler_offset);
#endif
				goto main_loop;
			}
		}
		/*
		 * If the handler for the exception was found in this method, we jump
		 * to it right away, otherwise we return and let the caller run
		 * the finally, fault and catch blocks.
		 * This same code should be present in the endfault opcode, but it
		 * is corrently not assigned in the ECMA specs: LAMESPEC.
		 */
		if (frame->ex_handler) {
#if DEBUG_INTERP
			if (tracing)
				g_print ("* Executing handler at IL_%04x\n", frame->ex_handler->handler_offset);
#endif
			ip = rtm->code + frame->ex_handler->handler_offset;
			sp = frame->stack;
			vt_sp = (unsigned char *) sp + rtm->stack_size;
			sp->data.p = frame->ex;
			++sp;
			goto main_loop;
		}
		goto exit_frame;
	}
exit_frame:
	DEBUG_LEAVE ();
}

void
ves_exec_method (MonoInvocation *frame)
{
	ThreadContext *context = mono_native_tls_get_value (thread_context_id);
	ThreadContext context_struct;
	MonoError error;
	jmp_buf env;

	frame->ex = NULL;

	if (setjmp(env)) {
		mono_unhandled_exception ((MonoObject*)frame->ex);
		return;
	}
	if (context == NULL) {
		context = &context_struct;
		context_struct.domain = mono_domain_get ();
		context_struct.base_frame = frame;
		context_struct.current_frame = NULL;
		context_struct.env_frame = frame;
		context_struct.current_env = &env;
		context_struct.search_for_handler = 0;
		context_struct.managed_code = 0;
		mono_native_tls_set_value (thread_context_id, context);
	}
	frame->ip = NULL;
	frame->parent = context->current_frame;
	frame->runtime_method = mono_interp_get_runtime_method (context->domain, frame->method, &error);
	mono_error_cleanup (&error); /* FIXME: don't swallow the error */
	context->managed_code = 1;
	ves_exec_method_with_context (frame, context, NULL, NULL, -1);
	context->managed_code = 0;
	if (frame->ex) {
		if (context != &context_struct && context->current_env) {
			context->env_frame->ex = frame->ex;
			longjmp (*context->current_env, 1);
		}
		else
			mono_unhandled_exception ((MonoObject*)frame->ex);
	}
	if (context->base_frame == frame)
		mono_native_tls_set_value (thread_context_id, NULL);
	else
		context->current_frame = frame->parent;
}

void
mono_interp_parse_options (const char *options)
{
	char **args, **ptr;

	args = g_strsplit (options, ",", -1);
	for (ptr = args; ptr && *ptr; ptr ++) {
		char *arg = *ptr;

		if (strncmp (arg, "jit=", 4) == 0)
			jit_classes = g_slist_prepend (jit_classes, arg + 4);
	}
}

void
mono_interp_init ()
{
	mono_native_tls_alloc (&thread_context_id, NULL);
	mono_native_tls_set_value (thread_context_id, NULL);

	mono_interp_transform_init ();
}

typedef int (*TestMethod) (void);

static void
interp_regression_step (MonoImage *image, int verbose, int *total_run, int *total, GTimer *timer, MonoDomain *domain)
{
	int result, expected, failed, cfailed, run;
	double elapsed, transform_time;
	int i;
	MonoObject *result_obj;
	static gboolean filter_method_init = FALSE;
	static const char *filter_method = NULL;

	g_print ("Test run: image=%s\n", mono_image_get_filename (image));
	cfailed = failed = run = 0;
	transform_time = elapsed = 0.0;

	g_timer_start (timer);
	for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
		MonoObject *exc = NULL;
		MonoError error;
		MonoMethod *method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL, NULL, &error);
		if (!method) {
			mono_error_cleanup (&error); /* FIXME don't swallow the error */
			continue;
		}

		if (!filter_method_init) {
			filter_method = g_getenv ("INTERP_FILTER_METHOD");
			filter_method_init = TRUE;
		}
		gboolean filter = FALSE;
		if (filter_method) {
			const char *name = filter_method;

			if ((strchr (name, '.') > name) || strchr (name, ':')) {
				MonoMethodDesc *desc = mono_method_desc_new (name, TRUE);
				filter = mono_method_desc_full_match (desc, method);
				mono_method_desc_free (desc);
			} else {
				filter = strcmp (method->name, name) == 0;
			}
		} else { /* no filter, check for `Category' attribute on method */
			filter = TRUE;
			MonoCustomAttrInfo* ainfo = mono_custom_attrs_from_method_checked (method, &error);
			mono_error_cleanup (&error);

			if (ainfo) {
				int j;
				for (j = 0; j < ainfo->num_attrs && filter; ++j) {
					MonoCustomAttrEntry *centry = &ainfo->attrs [j];
					if (centry->ctor == NULL)
						continue;

					MonoClass *klass = centry->ctor->klass;
					if (strcmp (klass->name, "CategoryAttribute"))
						continue;

					MonoObject *obj = mono_custom_attrs_get_attr_checked (ainfo, klass, &error);
					/* FIXME: there is an ordering problem if there're multiple attributes, do this instead:
					 * MonoObject *obj = create_custom_attr (ainfo->image, centry->ctor, centry->data, centry->data_size, &error); */
					mono_error_cleanup (&error);
					MonoMethod *getter = mono_class_get_method_from_name (klass, "get_Category", -1);
					MonoObject *str = mono_interp_runtime_invoke (getter, obj, NULL, &exc, &error);
					mono_error_cleanup (&error);
					char *utf8_str = mono_string_to_utf8_checked ((MonoString *) str, &error);
					mono_error_cleanup (&error);
					if (!strcmp (utf8_str, "!INTERPRETER")) {
						g_print ("skip %s...\n", method->name);
						filter = FALSE;
					}
				}
			}
		}
		if (strncmp (method->name, "test_", 5) == 0 && filter) {
			MonoError interp_error;
			MonoObject *exc = NULL;

			result_obj = mono_interp_runtime_invoke (method, NULL, NULL, &exc, &interp_error);
			if (!mono_error_ok (&interp_error)) {
				cfailed++;
				g_print ("Test '%s' execution failed.\n", method->name);
			} else if (exc != NULL) {
				g_print ("Exception in Test '%s' occured:\n", method->name);
				mono_object_describe (exc);
				run++;
				failed++;
			} else {
				result = *(gint32 *) mono_object_unbox (result_obj);
				expected = atoi (method->name + 5);  // FIXME: oh no.
				run++;

				if (result != expected) {
					failed++;
					g_print ("Test '%s' failed result (got %d, expected %d).\n", method->name, result, expected);
				}
			}
		}
	}
	g_timer_stop (timer);
	elapsed = g_timer_elapsed (timer, NULL);
	if (failed > 0 || cfailed > 0){
		g_print ("Results: total tests: %d, failed: %d, cfailed: %d (pass: %.2f%%)\n",
				run, failed, cfailed, 100.0*(run-failed-cfailed)/run);
	} else {
		g_print ("Results: total tests: %d, all pass \n",  run);
	}

	g_print ("Elapsed time: %f secs (%f, %f)\n\n", elapsed,
			elapsed - transform_time, transform_time);
	*total += failed + cfailed;
	*total_run += run;
}

static int
interp_regression (MonoImage *image, int verbose, int *total_run)
{
	MonoMethod *method;
	GTimer *timer = g_timer_new ();
	MonoDomain *domain = mono_domain_get ();
	guint32 i;
	int total;

	/* load the metadata */
	for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
		MonoError error;
		method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL, NULL, &error);
		if (!method) {
			mono_error_cleanup (&error);
			continue;
		}
		mono_class_init (method->klass);
	}

	total = 0;
	*total_run = 0;
	interp_regression_step (image, verbose, total_run, &total, timer, domain);

	g_timer_destroy (timer);
	return total;
}

int
mono_interp_regression_list (int verbose, int count, char *images [])
{
	int i, total, total_run, run;
	
	total_run = total = 0;
	for (i = 0; i < count; ++i) {
		MonoAssembly *ass = mono_assembly_open_predicate (images [i], FALSE, FALSE, NULL, NULL, NULL);
		if (!ass) {
			g_warning ("failed to load assembly: %s", images [i]);
			continue;
		}
		total += interp_regression (mono_assembly_get_image (ass), verbose, &run);
		total_run += run;
	}
	if (total > 0) {
		g_print ("Overall results: tests: %d, failed: %d (pass: %.2f%%)\n", total_run, total, 100.0*(total_run-total)/total_run);
	} else {
		g_print ("Overall results: tests: %d, 100%% pass\n", total_run);
	}
	
	return total;
}

/*
 * mono_interp_set_resume_state:
 *
 *   Set the state the interpeter will continue to execute from after execution returns to the interpreter.
 */
void
mono_interp_set_resume_state (MonoException *ex, StackFrameInfo *frame, gpointer handler_ip)
{
	ThreadContext *context = mono_native_tls_get_value (thread_context_id);

	context->has_resume_state = TRUE;
	context->handler_frame = frame->interp_frame;
	/* This is on the stack, so it doesn't need a wbarrier */
	context->handler_frame->ex = ex;
	context->handler_ip = handler_ip;
}

/*
 * mono_interp_run_finally:
 *
 *   Run the finally clause identified by CLAUSE_INDEX in the intepreter frame given by
 * frame->interp_frame.
 */
void
mono_interp_run_finally (StackFrameInfo *frame, int clause_index, gpointer handler_ip)
{
       MonoInvocation *iframe = frame->interp_frame;
       ThreadContext *context = mono_native_tls_get_value (thread_context_id);

       ves_exec_method_with_context (iframe, context, handler_ip, NULL, clause_index);
}

typedef struct {
	MonoInvocation *current;
} StackIter;

/*
 * mono_interp_frame_iter_init:
 *
 *   Initialize an iterator for iterating through interpreted frames.
 */
void
mono_interp_frame_iter_init (MonoInterpStackIter *iter, gpointer interp_exit_data)
{
	StackIter *stack_iter = (StackIter*)iter;

	stack_iter->current = (MonoInvocation*)interp_exit_data;
}

gboolean
mono_interp_frame_iter_next (MonoInterpStackIter *iter, StackFrameInfo *frame)
{
	StackIter *stack_iter = (StackIter*)iter;
	MonoInvocation *iframe = stack_iter->current;

	memset (frame, 0, sizeof (StackFrameInfo));
	/* pinvoke frames doesn't have runtime_method set */
	while (iframe && !iframe->runtime_method)
		iframe = iframe->parent;
	if (!iframe)
		return FALSE;

	frame->type = FRAME_TYPE_INTERP;
	frame->interp_frame = iframe;
	frame->method = iframe->runtime_method->method;
	frame->actual_method = frame->method;
	/* This is the offset in the interpreter IR */
	frame->native_offset = iframe->ip - iframe->runtime_method->code;
	frame->ji = iframe->runtime_method->jinfo;

	stack_iter->current = iframe->parent;

	return TRUE;
}
