/*
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
#include <mono/metadata/assembly.h>
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
#include <mono/os/util.h>

#include "interp.h"
#include "mintops.h"
#include "embed.h"
#include "hacks.h"

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	CEE_LASTOP
};
#undef OPDEF

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

static gint *abort_requested;

/* If true, then we output the opcodes as we interpret them */
static int global_tracing = 0;
static int global_no_pointers = 0;

int mono_interp_traceopt = 0;

static int debug_indent_level = 0;

#define INIT_FRAME(frame,parent_frame,obj_this,method_args,method_retval,mono_method)	\
	do {	\
		(frame)->parent = (parent_frame);	\
		(frame)->obj = (obj_this);	\
		(frame)->stack_args = (method_args);	\
		(frame)->retval = (method_retval);	\
		(frame)->runtime_method = mono_interp_get_runtime_method (mono_method);	\
		(frame)->ex = NULL;	\
		(frame)->ip = NULL;	\
		(frame)->invoke_trap = 0;	\
	} while (0)

void ves_exec_method (MonoInvocation *frame);

static char* dump_stack (stackval *stack, stackval *sp);
static char* dump_frame (MonoInvocation *inv);
static MonoArray *get_trace_ips (MonoDomain *domain, MonoInvocation *top);
static void ves_exec_method_with_context (MonoInvocation *frame, ThreadContext *context);

typedef void (*ICallMethod) (MonoInvocation *frame);

static guint32 die_on_exception = 0;
static guint32 thread_context_id = 0;

#define DEBUG_INTERP 1
#define COUNT_OPS 0
#if DEBUG_INTERP

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

#define DEBUG_ENTER()	\
	if (db_methods) { \
		g_list_foreach (db_methods, db_match_method, (gpointer)frame->runtime_method->method);	\
		if (break_on_method) tracing=nested_trace ? (global_tracing = 2, 3) : 2;	\
		break_on_method = 0;	\
	} \
	if (tracing) {	\
		MonoMethod *method = frame->runtime_method->method ;\
		char *mn, *args = dump_args (frame);	\
		debug_indent_level++;	\
		output_indent ();	\
		mn = mono_method_full_name (method, FALSE); \
		g_print ("(%u) Entering %s (", GetCurrentThreadId(), mn);	\
		g_free (mn); \
		g_print ("%s)\n", args);	\
		g_free (args);	\
	}	\
	if (mono_profiler_events & MONO_PROFILE_ENTER_LEAVE)	\
		mono_profiler_method_enter (frame->runtime_method->method);

#define DEBUG_LEAVE()	\
	if (tracing) {	\
		char *mn, *args;	\
		args = dump_retval (frame);	\
		output_indent ();	\
		mn = mono_method_full_name (frame->runtime_method->method, FALSE); \
		g_print ("(%u) Leaving %s", GetCurrentThreadId(),  mn);	\
		g_free (mn); \
		g_print (" => %s\n", args);	\
		g_free (args);	\
		debug_indent_level--;	\
		if (tracing == 3) global_tracing = 0; \
	}	\
	if (mono_profiler_events & MONO_PROFILE_ENTER_LEAVE)	\
		mono_profiler_method_leave (frame->runtime_method->method);

#else

#define DEBUG_ENTER()
#define DEBUG_LEAVE()

#endif

static void
interp_ex_handler (MonoException *ex) {
	ThreadContext *context = TlsGetValue (thread_context_id);
	char *stack_trace;
	if (context == NULL)
		return;
	stack_trace = dump_frame (context->current_frame);
	ex->stack_trace = mono_string_new (mono_domain_get(), stack_trace);
	g_free (stack_trace);
	if (context->current_env == NULL || strcmp(ex->object.vtable->klass->name, "ExecutionEngineException") == 0) {
		char *strace = mono_string_to_utf8 (ex->stack_trace);
		fprintf(stderr, "Nothing can catch this exception: ");
		fprintf(stderr, "%s", ex->object.vtable->klass->name);
		if (ex->message != NULL) {
			char *m = mono_string_to_utf8 (ex->message);
			fprintf(stderr, ": %s", m);
			g_free(m);
		}
		fprintf(stderr, "\n");
		fprintf(stderr, "%s\n", strace);
		g_free (strace);
		if (ex->inner_ex != NULL) {
			ex = (MonoException *)ex->inner_ex;
			fprintf(stderr, "Inner exception: %s", ex->object.vtable->klass->name);
			if (ex->message != NULL) {
				char *m = mono_string_to_utf8 (ex->message);
				fprintf(stderr, ": %s", m);
				g_free(m);
			}
			strace = mono_string_to_utf8 (ex->stack_trace);
			fprintf(stderr, "\n");
			fprintf(stderr, "%s\n", strace);
			g_free (strace);
		}
		/* wait for other threads to also collapse */
		Sleep(1000);
		exit(1);
	}
	context->env_frame->ex = ex;
	context->search_for_handler = 1;
	longjmp (*context->current_env, 1);
}

static void
ves_real_abort (int line, MonoMethod *mh,
		const unsigned short *ip, stackval *stack, stackval *sp)
{
	fprintf (stderr, "Execution aborted in method: %s::%s\n", mh->klass->name, mh->name);
	fprintf (stderr, "Line=%d IP=0x%04x, Aborted execution\n", line,
		 ip-(const unsigned short *)mono_method_get_header (mh)->code);
	g_print ("0x%04x %02x\n",
		 ip-(const unsigned short *)mono_method_get_header (mh)->code, *ip);
	if (sp > stack)
		printf ("\t[%d] 0x%08x %0.5f\n", sp-stack, sp[-1].data.i, sp[-1].data.f);
}

#define ves_abort() \
	do {\
		ves_real_abort(__LINE__, frame->runtime_method->method, ip, frame->stack, sp); \
		THROW_EX (mono_get_exception_execution_engine (NULL), ip); \
	} while (0);

static gpointer
interp_create_remoting_trampoline (MonoMethod *method, MonoRemotingTarget target)
{
	return mono_interp_get_runtime_method (mono_marshal_get_remoting_invoke_for_target (method, target));
}

static CRITICAL_SECTION runtime_method_lookup_section;

RuntimeMethod*
mono_interp_get_runtime_method (MonoMethod *method)
{
	MonoDomain *domain = mono_domain_get ();
	RuntimeMethod *rtm;

	EnterCriticalSection (&runtime_method_lookup_section);
	if ((rtm = mono_internal_hash_table_lookup (&domain->jit_code_hash, method))) {
		LeaveCriticalSection (&runtime_method_lookup_section);
		return rtm;
	}
	rtm = mono_mempool_alloc (domain->mp, sizeof (RuntimeMethod));
	memset (rtm, 0, sizeof (*rtm));
	rtm->method = method;
	rtm->param_count = mono_method_signature (method)->param_count;
	rtm->hasthis = mono_method_signature (method)->hasthis;
	rtm->valuetype = method->klass->valuetype;
	mono_internal_hash_table_insert (&domain->jit_code_hash, method, rtm);
	LeaveCriticalSection (&runtime_method_lookup_section);

	return rtm;
}

static gpointer
interp_create_trampoline (MonoMethod *method)
{
	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		method = mono_marshal_get_synchronized_wrapper (method);
	return mono_interp_get_runtime_method (method);
}

static inline RuntimeMethod*
get_virtual_method (RuntimeMethod *runtime_method, MonoObject *obj)
{
	MonoMethod *m = runtime_method->method;

	if ((m->flags & METHOD_ATTRIBUTE_FINAL) || !(m->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
		if (obj->vtable->klass == mono_defaults.transparent_proxy_class) 
			return mono_interp_get_runtime_method (mono_marshal_get_remoting_invoke (m));
		else if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
			return mono_interp_get_runtime_method (mono_marshal_get_synchronized_wrapper (m));
		else
			return runtime_method;
	}

	if (m->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		return ((RuntimeMethod **)obj->vtable->interface_offsets [m->klass->interface_id]) [m->slot];
	} else {
		return ((RuntimeMethod **)obj->vtable->vtable) [m->slot];
	}
}

void inline
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
			stackval_from_data (type->data.klass->enum_basetype, result, data, pinvoke);
			return;
		} else {
			int size;
			
			if (pinvoke)
				size = mono_class_native_size (type->data.klass, NULL);
			else
				size = mono_class_value_size (type->data.klass, NULL);
			memcpy (result->data.vt, data, size);
		}
		return;
	default:
		g_warning ("got type 0x%02x", type->type);
		g_assert_not_reached ();
	}
}

void inline
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
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_PTR: {
		gpointer *p = (gpointer*)data;
		*p = val->data.p;
		return;
	}
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			stackval_to_data (type->data.klass->enum_basetype, val, data, pinvoke);
			return;
		} else {
			int size;

			if (pinvoke)
				size = mono_class_native_size (type->data.klass, NULL);
			else
				size = mono_class_value_size (type->data.klass, NULL);

			memcpy (data, val->data.p, size);
		}
		return;
	default:
		g_warning ("got type %x", type->type);
		g_assert_not_reached ();
	}
}

static void
fill_in_trace (MonoException *exception, MonoInvocation *frame)
{
	char *stack_trace = dump_frame (frame);
	MonoDomain *domain = mono_domain_get();
	(exception)->stack_trace = mono_string_new (domain, stack_trace);
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
ves_array_create (MonoDomain *domain, MonoClass *klass, MonoMethodSignature *sig, stackval *values)
{
	guint32 *lengths;
	guint32 *lower_bounds;
	int i;

	lengths = alloca (sizeof (guint32) * klass->rank * 2);
	for (i = 0; i < sig->param_count; ++i) {
		lengths [i] = values->data.i;
		values ++;
	}
	if (klass->rank == sig->param_count) {
		/* Only lengths provided. */
		lower_bounds = NULL;
	} else {
		/* lower bounds are first. */
		lower_bounds = lengths;
		lengths += klass->rank;
	}
	return (MonoObject*)mono_array_new_full (domain, klass, lengths, lower_bounds);
}

static void 
ves_array_set (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoObject *o;
	MonoArray *ao;
	MonoClass *ac;
	gint32 i, t, pos, esize;
	gpointer ea;
	MonoType *mt;

	o = frame->obj;
	ao = (MonoArray *)o;
	ac = o->vtable->klass;

	g_assert (ac->rank >= 1);

	pos = sp [0].data.i;
	if (ao->bounds != NULL) {
		pos -= ao->bounds [0].lower_bound;
		for (i = 1; i < ac->rank; i++) {
			if ((t = sp [i].data.i - ao->bounds [i].lower_bound) >= 
			    ao->bounds [i].length) {
				frame->ex = mono_get_exception_index_out_of_range ();
				FILL_IN_TRACE(frame->ex, frame);
				return;
			}
			pos = pos*ao->bounds [i].length + sp [i].data.i - 
				ao->bounds [i].lower_bound;
		}
	} else if (pos >= ao->max_length) {
		frame->ex = mono_get_exception_index_out_of_range ();
		FILL_IN_TRACE(frame->ex, frame);
		return;
	}

#if 0 /* FIX */
	if (sp [ac->rank].data.p && !mono_object_isinst (sp [ac->rank].data.p, mono_object_class (o)->element_class)) {
		frame->ex = mono_get_exception_array_type_mismatch ();
		FILL_IN_TRACE (frame->ex, frame);
		return;
	}
#endif

	esize = mono_array_element_size (ac);
	ea = mono_array_addr_with_size (ao, esize, pos);

	mt = mono_method_signature (frame->runtime_method->method)->params [ac->rank];
	stackval_to_data (mt, &sp [ac->rank], ea, FALSE);
}

static void 
ves_array_get (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoObject *o;
	MonoArray *ao;
	MonoClass *ac;
	gint32 i, t, pos, esize;
	gpointer ea;
	MonoType *mt;

	o = frame->obj;
	ao = (MonoArray *)o;
	ac = o->vtable->klass;

	g_assert (ac->rank >= 1);

	pos = sp [0].data.i;
	if (ao->bounds != NULL) {
		pos -= ao->bounds [0].lower_bound;
		for (i = 1; i < ac->rank; i++) {
			if ((t = sp [i].data.i - ao->bounds [i].lower_bound) >= 
			    ao->bounds [i].length) {
				frame->ex = mono_get_exception_index_out_of_range ();
				FILL_IN_TRACE(frame->ex, frame);
				return;
			}

			pos = pos*ao->bounds [i].length + sp [i].data.i - 
				ao->bounds [i].lower_bound;
		}
	} else if (pos >= ao->max_length) {
		frame->ex = mono_get_exception_index_out_of_range ();
		FILL_IN_TRACE(frame->ex, frame);
		return;
	}

	esize = mono_array_element_size (ac);
	ea = mono_array_addr_with_size (ao, esize, pos);

	mt = mono_method_signature (frame->runtime_method->method)->ret;
	stackval_from_data (mt, frame->retval, ea, FALSE);
}

static void
ves_array_element_address (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoObject *o;
	MonoArray *ao;
	MonoClass *ac;
	gint32 i, t, pos, esize;
	gpointer ea;

	o = frame->obj;
	ao = (MonoArray *)o;
	ac = o->vtable->klass;

	g_assert (ac->rank >= 1);

	pos = sp [0].data.i;
	if (ao->bounds != NULL) {
		pos -= ao->bounds [0].lower_bound;
		for (i = 1; i < ac->rank; i++) {
			if ((t = sp [i].data.i - ao->bounds [i].lower_bound) >= 
			    ao->bounds [i].length) {
				frame->ex = mono_get_exception_index_out_of_range ();
				FILL_IN_TRACE(frame->ex, frame);
				return;
			}
			pos = pos*ao->bounds [i].length + sp [i].data.i - 
				ao->bounds [i].lower_bound;
		}
	} else if (pos >= ao->max_length) {
		frame->ex = mono_get_exception_index_out_of_range ();
		FILL_IN_TRACE(frame->ex, frame);
		return;
	}

	esize = mono_array_element_size (ac);
	ea = mono_array_addr_with_size (ao, esize, pos);

	frame->retval->data.p = ea;
}

static void
interp_walk_stack (MonoStackWalk func, gboolean do_il_offset, gpointer user_data)
{
	ThreadContext *context = TlsGetValue (thread_context_id);
	MonoInvocation *frame;
	int il_offset;
	MonoMethodHeader *hd;

	if (!context) return;
		
	frame = context->current_frame;

	while (frame) {
		gboolean managed = FALSE;
		MonoMethod *method = frame->runtime_method->method;
		if (!method || (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) || 
				(method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)))
			il_offset = -1;
		else {
			hd = mono_method_get_header (method);
			il_offset = frame->ip - (const unsigned short *)hd->code;
			if (!method->wrapper_type)
				managed = TRUE;
		}
		if (func (method, -1, il_offset, managed, user_data))
			return;
		frame = frame->parent;
	}
}

static void 
ves_pinvoke_method (MonoInvocation *frame, MonoMethodSignature *sig, MonoFunc addr, gboolean string_ctor, ThreadContext *context)
{
	jmp_buf env;
	MonoPIFunc func;
	MonoInvocation *old_frame = context->current_frame;
	MonoInvocation *old_env_frame = context->env_frame;
	jmp_buf *old_env = context->current_env;

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

	if (frame->runtime_method) {
		func = frame->runtime_method->func;
	} else {
		func = mono_arch_create_trampoline (sig, string_ctor);
	}

	context->current_frame = frame;
	context->managed_code = 0;

	func (addr, &frame->retval->data.p, frame->obj, frame->stack_args);

	context->managed_code = 1;
	/* domain can only be changed by native code */
	context->domain = mono_domain_get ();

	if (*abort_requested)
		mono_thread_interruption_checkpoint ();
	
	if (string_ctor) {
		stackval_from_data (&mono_defaults.string_class->byval_arg, 
				    frame->retval, (char*)&frame->retval->data.p, sig->pinvoke);
 	} else if (!MONO_TYPE_ISSTRUCT (sig->ret))
		stackval_from_data (sig->ret, frame->retval, (char*)&frame->retval->data.p, sig->pinvoke);

	context->current_frame = old_frame;
	context->env_frame = old_env_frame;
	context->current_env = old_env;
}

static void
interp_delegate_ctor (MonoDomain *domain, MonoObject *this, MonoObject *target, RuntimeMethod *runtime_method)
{
	MonoDelegate *delegate = (MonoDelegate *)this;

	delegate->method_info = mono_method_get_object (domain, runtime_method->method, NULL);
	delegate->target = target;

	if (target && target->vtable->klass == mono_defaults.transparent_proxy_class) {
		MonoMethod *method = mono_marshal_get_remoting_invoke (runtime_method->method);
		delegate->method_ptr = mono_interp_get_runtime_method (method);
	} else {
		delegate->method_ptr = runtime_method;
	}
}

MonoDelegate*
mono_interp_ftnptr_to_delegate (MonoClass *klass, gpointer ftn)
{
	MonoDelegate *d;
	MonoJitInfo *ji;
	MonoDomain *domain = mono_domain_get ();

	d = (MonoDelegate*)mono_object_new (domain, klass);

	ji = mono_jit_info_table_find (domain, ftn);
	if (ji == NULL)
		mono_raise_exception (mono_get_exception_argument ("", "Function pointer was not created by a Delegate."));

	/* FIXME: discard the wrapper and call the original method */
	interp_delegate_ctor (domain, (MonoObject*)d, NULL, mono_interp_get_runtime_method (ji->method));

	return d;
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
	MonoObject *obj = (MonoObject*)frame->obj;

	mono_class_init (method->klass);
	
	if (obj && mono_object_isinst (obj, mono_defaults.multicastdelegate_class)) {
		if (*name == '.' && (strcmp (name, ".ctor") == 0)) {
			interp_delegate_ctor (context->domain, obj, frame->stack_args[0].data.p, frame->stack_args[1].data.p);
			return;
		}
	}

	if (obj && mono_object_isinst (obj, mono_defaults.array_class)) {
		if (*name == 'S' && (strcmp (name, "Set") == 0)) {
			ves_array_set (frame);
			return;
		}
		if (*name == 'G' && (strcmp (name, "Get") == 0)) {
			ves_array_get (frame);
			return;
		}
		if (*name == 'A' && (strcmp (name, "Address") == 0)) {
			ves_array_element_address (frame);
			return;
		}
	}
	
	g_error ("Don't know how to exec runtime method %s.%s::%s", 
			method->klass->name_space, method->klass->name,
			method->name);
}

static char*
dump_stack (stackval *stack, stackval *sp)
{
	stackval *s = stack;
	GString *str = g_string_new ("");
	
	if (sp == stack)
		return g_string_free (str, FALSE);
	
	while (s < sp) {
		g_string_append_printf (str, "[%lld/0x%0llx] ", s->data.l, s->data.l);
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
	default:
		g_string_append_printf (str, "[%lld/0x%0llx] ", s->data.l, s->data.l);
		break;
	}
}

static char*
dump_args (MonoInvocation *inv)
{
	GString *str = g_string_new ("");
	int i;
	MonoMethodSignature *signature = mono_method_signature (inv->runtime_method->method);
	
	if (signature->param_count == 0)
		return g_string_free (str, FALSE);

	if (signature->hasthis)
		g_string_append_printf (str, "%p ", inv->obj);

	for (i = 0; i < signature->param_count; ++i)
		dump_stackval (str, inv->stack_args + i, signature->params [i]);

	return g_string_free (str, FALSE);
}

static char*
dump_retval (MonoInvocation *inv)
{
	GString *str = g_string_new ("");
	MonoType *ret = mono_method_signature (inv->runtime_method->method)->ret;

	if (ret->type != MONO_TYPE_VOID)
		dump_stackval (str, inv->retval, ret);

	return g_string_free (str, FALSE);
}
 
static char*
dump_frame (MonoInvocation *inv)
{
	GString *str = g_string_new ("");
	int i;
	char *args;
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
				MonoMethodHeader *hd = mono_method_get_header (method);

				if (hd != NULL) {
					if (inv->ip) {
						opname = mono_interp_opname [*inv->ip];
						codep = inv->ip - inv->runtime_method->code;
					} else 
						opname = "";
	
					source = mono_debug_source_location_from_il_offset (method, codep, NULL);
				}
			}
			args = dump_args (inv);
			name = mono_method_full_name (method, TRUE);
			if (source)
				g_string_append_printf (str, "#%d: 0x%05x %-10s in %s (%s) at %s\n", i, codep, opname,
						   name, args, source);
			else
				g_string_append_printf (str, "#%d: 0x%05x %-10s in %s (%s)\n", i, codep, opname,
						   name, args);
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

	for (i = 0, inv = top; inv; inv = inv->parent)
		if (inv->runtime_method != NULL)
			++i;

	res = mono_array_new (domain, mono_defaults.int_class, 2 * i);

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

static MonoObject*
interp_mono_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc)
{
	MonoInvocation frame;
	ThreadContext * volatile context = TlsGetValue (thread_context_id);
	MonoObject *retval = NULL;
	MonoMethodSignature *sig = mono_method_signature (method);
	MonoClass *klass = mono_class_from_mono_type (sig->ret);
	int i, type, isobject = 0;
	void *ret = NULL;
	stackval result;
	stackval *args = alloca (sizeof (stackval) * sig->param_count);
	ThreadContext context_struct;
	MonoInvocation *old_frame = NULL;
	jmp_buf env;

	frame.ex = NULL;

	if (setjmp(env)) {
		if (context != &context_struct) {
			context->domain = mono_domain_get ();
			context->current_frame = old_frame;
			context->managed_code = 0;
		} else 
			TlsSetValue (thread_context_id, NULL);
		if (exc != NULL)
			*exc = (MonoObject *)frame.ex;
		return retval;
	}

	if (context == NULL) {
		context = &context_struct;
		context_struct.base_frame = &frame;
		context_struct.current_frame = NULL;
		context_struct.env_frame = &frame;
		context_struct.current_env = &env;
		context_struct.search_for_handler = 0;
		context_struct.managed_code = 0;
		TlsSetValue (thread_context_id, context);
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
		retval = mono_object_new (context->domain, klass);
		ret = ((char*)retval) + sizeof (MonoObject);
		if (!sig->ret->data.klass->enumtype)
			result.data.vt = ret;
		break;
	default:
		retval = mono_object_new (context->domain, klass);
		ret = ((char*)retval) + sizeof (MonoObject);
		break;
	}

	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			args [i].data.p = params [i];
			continue;
		}
		type = sig->params [i]->type;
handle_enum:
		switch (type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
			args [i].data.i = *(MonoBoolean*)params [i];
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
			args [i].data.i = *(gint16*)params [i];
			break;
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_U: /* use VAL_POINTER? */
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
			args [i].data.i = *(gint32*)params [i];
			break;
#if SIZEOF_VOID_P == 8
		case MONO_TYPE_U:
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			args [i].data.l = *(gint64*)params [i];
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				type = sig->params [i]->data.klass->enum_basetype->type;
				goto handle_enum;
			} else {
				args [i].data.p = params [i];
			}
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
			args [i].data.p = params [i];
			break;
		default:
			g_error ("type 0x%x not handled in  runtime invoke", sig->params [i]->type);
		}
	}

	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) 
		method = mono_marshal_get_native_wrapper (method);
	INIT_FRAME(&frame,context->current_frame,obj,args,&result,method);
	if (exc)
		frame.invoke_trap = 1;
	context->managed_code = 1;
	ves_exec_method_with_context (&frame, context);
	context->managed_code = 0;
	if (context == &context_struct)
		TlsSetValue (thread_context_id, NULL);
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
		void (*func)() = ptr;
        	func ();
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

static CRITICAL_SECTION create_method_pointer_mutex;

static MonoGHashTable *method_pointer_hash = NULL;

static void *
mono_create_method_pointer (MonoMethod *method)
{
	gpointer addr;
	MonoJitInfo *ji;

	EnterCriticalSection (&create_method_pointer_mutex);
	if (!method_pointer_hash) {
		MONO_GC_REGISTER_ROOT (method_pointer_hash);
		method_pointer_hash = mono_g_hash_table_new (NULL, NULL);
	}
	addr = mono_g_hash_table_lookup (method_pointer_hash, method);
	if (addr) {
		LeaveCriticalSection (&create_method_pointer_mutex);
		return addr;
	}

	/*
	 * If it is a static P/Invoke method, we can just return the pointer
	 * to the method implementation.
	 */
	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL && ((MonoMethodPInvoke*) method)->addr) {
		ji = g_new0 (MonoJitInfo, 1);
		ji->method = method;
		ji->code_size = 1;
		ji->code_start = addr = ((MonoMethodPInvoke*) method)->addr;

		mono_jit_info_table_add (mono_get_root_domain (), ji);
	}		
	else
		addr = mono_arch_create_method_pointer (method);

	mono_g_hash_table_insert (method_pointer_hash, method, addr);
	LeaveCriticalSection (&create_method_pointer_mutex);

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
		g_print ("(%u) ", GetCurrentThreadId()); \
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
 * Defining this causes register allocation errors in some versions of gcc:
 * error: unable to find a register to spill in class `SIREG'
 */
/* #define MINT_USE_DEDICATED_IP_REG */

static void 
ves_exec_method_with_context (MonoInvocation *frame, ThreadContext *context)
{
	MonoInvocation child_frame;
	GSList *finally_ips = NULL;
	const unsigned short *endfinally_ip = NULL;
#if defined(__GNUC__) && defined (i386) && defined (MINT_USE_DEDICATED_IP_REG)
	register const unsigned short *ip asm ("%esi");
#else
	register const unsigned short *ip;
#endif
	register stackval *sp;
	RuntimeMethod *rtm;
#if DEBUG_INTERP
	gint tracing = global_tracing;
	unsigned char *vtalloc;
#endif
	int i32;
	unsigned char *vt_sp;
	char *locals;
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

	DEBUG_ENTER ();

	if (!frame->runtime_method->transformed) {
		context->managed_code = 0;
		frame->ex = mono_interp_transform_method (frame->runtime_method, context);
		context->managed_code = 1;
		if (frame->ex) {
			rtm = NULL;
			ip = NULL;
			goto exit_frame;
		}
	}

	rtm = frame->runtime_method;
	frame->args = alloca (rtm->alloca_size);
	sp = frame->stack = (stackval *)((char *)frame->args + rtm->args_size);
#if DEBUG_INTERP
	if (tracing > 1)
		memset(sp, 0, rtm->stack_size);
#endif
	vt_sp = (char *)sp + rtm->stack_size;
#if DEBUG_INTERP
	vtalloc = vt_sp;
#endif
	locals = vt_sp + rtm->vt_stack_size;

	child_frame.parent = frame;

	/* ready to go */
	ip = rtm->code;

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
			char *ret_vt_sp = vt_sp;
			vt_sp -= READ32(ip + 2);
			if (ret_size > 0) {
				memmove (vt_sp, ret_vt_sp, ret_size);
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
		MINT_IN_CASE(MINT_POP)
			++ip;
			--sp;
			MINT_IN_BREAK;
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
			vt_sp = (char *)sp + rtm->stack_size;
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
			child_frame.stack_args = sp;
			if (csignature->hasthis) {
				--sp;
				child_frame.obj = sp->data.p;
			} else {
				child_frame.obj = NULL;
			}
			if (csignature->hasthis &&
					((MonoObject *)child_frame.obj)->vtable->klass == mono_defaults.transparent_proxy_class) {
				child_frame.runtime_method = mono_interp_get_runtime_method (
								mono_marshal_get_remoting_invoke (child_frame.runtime_method->method));
			} else if (child_frame.runtime_method->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) 
				child_frame.runtime_method = mono_interp_get_runtime_method (
								mono_marshal_get_native_wrapper (child_frame.runtime_method->method));

			ves_exec_method_with_context (&child_frame, context);

			context->current_frame = frame;

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
			child_frame.stack_args = sp;
			if (csignature->hasthis) {
				--sp;
				child_frame.obj = sp->data.p;
			} else {
				child_frame.obj = NULL;
			}
			ves_pinvoke_method (&child_frame, csignature, (MonoFunc) code, FALSE, context);

			context->current_frame = frame;

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
			child_frame.stack_args = sp;
			if (child_frame.runtime_method->hasthis) {
				--sp;
				child_frame.obj = sp->data.p;
			} else {
				child_frame.obj = NULL;
			}
			if (child_frame.runtime_method->hasthis && !child_frame.runtime_method->valuetype &&
					((MonoObject *)child_frame.obj)->vtable->klass == mono_defaults.transparent_proxy_class) {
				child_frame.runtime_method = mono_interp_get_runtime_method (
								mono_marshal_get_remoting_invoke (child_frame.runtime_method->method));
			}
			ves_exec_method_with_context (&child_frame, context);

			context->current_frame = frame;

			if (child_frame.ex) {
				/*
				 * An exception occurred, need to run finally, fault and catch handlers..
				 */
				frame->ex = child_frame.ex;
				goto handle_finally;
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
			child_frame.stack_args = sp;
			if (child_frame.runtime_method->hasthis) {
				--sp;
				child_frame.obj = sp->data.p;
			} else {
				child_frame.obj = NULL;
			}

			if (child_frame.runtime_method->hasthis && !child_frame.runtime_method->valuetype &&
					((MonoObject *)child_frame.obj)->vtable->klass == mono_defaults.transparent_proxy_class) {
				child_frame.runtime_method = mono_interp_get_runtime_method (
								mono_marshal_get_remoting_invoke (child_frame.runtime_method->method));
			}

			ves_exec_method_with_context (&child_frame, context);

			context->current_frame = frame;

			if (child_frame.ex) {
				/*
				 * An exception occurred, need to run finally, fault and catch handlers..
				 */
				frame->ex = child_frame.ex;
				goto handle_finally;
			}
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
			sp -= child_frame.runtime_method->param_count;
			child_frame.stack_args = sp;
			--sp;
			child_frame.obj = this_arg = sp->data.p;
			if (!this_arg)
				THROW_EX (mono_get_exception_null_reference(), ip - 2);
			child_frame.runtime_method = get_virtual_method (child_frame.runtime_method, this_arg);

			if (this_arg->vtable->klass->valuetype && child_frame.runtime_method->valuetype) {
				child_frame.obj = (char *)this_arg + sizeof(MonoObject);
			}

			ves_exec_method_with_context (&child_frame, context);

			context->current_frame = frame;

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
			sp -= child_frame.runtime_method->param_count;
			child_frame.stack_args = sp;
			--sp;
			child_frame.obj = this_arg = sp->data.p;
			if (!this_arg)
				THROW_EX (mono_get_exception_null_reference(), ip - 2);
			child_frame.runtime_method = get_virtual_method (child_frame.runtime_method, this_arg);

			if (this_arg->vtable->klass->valuetype && child_frame.runtime_method->valuetype) {
				child_frame.obj = (char *)this_arg + sizeof(MonoObject);
			}

			ves_exec_method_with_context (&child_frame, context);

			context->current_frame = frame;

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
		MINT_IN_CASE(MINT_CALLINT)
			ves_pinvoke_method (frame, mono_method_signature (frame->runtime_method->method), ((MonoMethodPInvoke*) frame->runtime_method->method)->addr, 
				    frame->runtime_method->method->string_ctor, context);
			if (frame->ex) {
				rtm = NULL;
				goto handle_exception;
			}
			goto exit_frame;
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
		MINT_IN_CASE(MINT_LDIND_I)
			++ip;
			sp[-1].data.p = *(gpointer*)sp[-1].data.p;
			MINT_IN_BREAK;
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
			* (gpointer *) sp->data.p = sp[1].data.p;
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
			BINOP(i, /);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_DIV_I8)
			if (sp [-1].data.l == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
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
			BINOP(i, %);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_REM_I8)
			if (sp [-1].data.l == 0)
				THROW_EX (mono_get_exception_divide_by_zero (), ip);
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
#if 0
		MINT_IN_CASE(MINT_CPOBJ) {
			MonoClass *vtklass;
			++ip;
			vtklass = rtm->data_items[READ32 (ip)];
			ip += 2;
			sp -= 2;
			memcpy (sp [0].data.p, sp [1].data.p, mono_class_value_size (vtklass, NULL));
			MINT_IN_BREAK;
		}
#endif
		MINT_IN_CASE(MINT_LDOBJ) {
			int size;
			void *p;
			c = rtm->data_items[* (guint16 *)(ip + 1)];
			ip += 2;
			if (c->byval_arg.type != MONO_TYPE_VALUETYPE || c->byval_arg.data.klass->enumtype) {
				p = sp [-1].data.p;
				stackval_from_data (&c->byval_arg, &sp [-1], p, FALSE);
			} else {
				size = mono_class_value_size (c, NULL);
				p = sp [-1].data.p;
				sp [-1].data.p = vt_sp;
				memcpy(vt_sp, p, size);
				vt_sp += (size + 7) & ~7;
			}
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
				o = ves_array_create (context->domain, newobj_class, csig, sp);
				goto array_constructed;
			}

			/*
			 * First arg is the object.
			 */
			if (newobj_class->valuetype) {
				if (!newobj_class->enumtype && (newobj_class->byval_arg.type == MONO_TYPE_VALUETYPE)) {
					child_frame.obj = vt_sp;
					valuetype_this.data.p = vt_sp;
				} else {
					memset (&valuetype_this, 0, sizeof (stackval));
					child_frame.obj = &valuetype_this;
				}
			} else {
				if (newobj_class != mono_defaults.string_class) {
					context->managed_code = 0;
					o = mono_object_new (context->domain, newobj_class);
					context->managed_code = 1;
					if (*abort_requested)
						mono_thread_interruption_checkpoint ();
					child_frame.obj = o;
				} else {
					child_frame.retval = &retval;
				}
			}

			if (csig->param_count) {
				sp -= csig->param_count;
				child_frame.stack_args = sp;
			} else {
				child_frame.stack_args = NULL;
			}

			g_assert (csig->call_convention == MONO_CALL_DEFAULT);

			child_frame.ip = NULL;
			child_frame.ex = NULL;

			ves_exec_method_with_context (&child_frame, context);

			context->current_frame = frame;

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
				if (c->marshalbyref) {
					if (!mono_object_isinst_mbyref (o, c))
						THROW_EX (mono_get_exception_invalid_cast (), ip);
				} else {
					MonoVTable *vt = o->vtable;
					MonoClass *oklass = vt->klass;
					if (c->flags & TYPE_ATTRIBUTE_INTERFACE) {
						if (c->interface_id > vt->max_interface_id ||
						    vt->interface_offsets [c->interface_id] == 0) {
							THROW_EX (mono_get_exception_invalid_cast (), ip);
						}
					} else if (c->rank) {
						if (!mono_object_isinst (o, c))
							THROW_EX (mono_get_exception_invalid_cast (), ip);
					} else if (!mono_class_has_parent (oklass, c)) {
						THROW_EX (mono_get_exception_invalid_cast (), ip);
					}
				}
			}
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ISINST)
			c = rtm->data_items [*(guint16 *)(ip + 1)];
			if ((o = sp [-1].data.p)) {
				if (c->marshalbyref) {
					if (!mono_object_isinst_mbyref (o, c))
						sp [-1].data.p = NULL;
				} else {
					MonoVTable *vt = o->vtable;
					MonoClass *oklass = vt->klass;
					if (c->flags & TYPE_ATTRIBUTE_INTERFACE) {
						if (c->interface_id > vt->max_interface_id ||
						    vt->interface_offsets [c->interface_id] == 0) {
							sp [-1].data.p = NULL;
						}
					} else if (c->rank) {
						if (!mono_object_isinst (o, c))
							sp [-1].data.p = NULL;
					} else if (!mono_class_has_parent (oklass, c)) {
						sp [-1].data.p = NULL;
					}
				}
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
				THROW_EX (mono_get_exception_null_reference(), ip);

			if (!(mono_object_isinst (o, c) || 
				  ((o->vtable->klass->rank == 0) && 
				   (o->vtable->klass->element_class == c->element_class))))
				THROW_EX (mono_get_exception_invalid_cast (), ip);

			sp [-1].data.p = (char *)o + sizeof (MonoObject);
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_THROW)
			--sp;
			frame->ex_handler = NULL;
			if (!sp->data.p)
				sp->data.p = mono_get_exception_null_reference ();
			THROW_EX ((MonoException *)sp->data.p, ip);
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
			if (o->vtable->klass == mono_defaults.transparent_proxy_class) {
				MonoClass *klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;

				addr = mono_load_remote_field (o, klass, field, &tmp);
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
			if (o->vtable->klass == mono_defaults.transparent_proxy_class) {
				MonoClass *klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;
				addr = mono_load_remote_field (o, klass, field, &tmp);
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
		MINT_IN_CASE(MINT_STFLD_O) STFLD(p, gpointer); MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STFLD_P) STFLD(p, gpointer); MINT_IN_BREAK;

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

			if (o->vtable->klass == mono_defaults.transparent_proxy_class) {
				MonoClass *klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;
				mono_store_remote_field (o, klass, field, &sp [-1].data);
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

			if (o->vtable->klass == mono_defaults.transparent_proxy_class) {
				MonoClass *klass = ((MonoTransparentProxy*)o)->remote_class->proxy_class;
				mono_store_remote_field (o, klass, field, &sp [-1].data);
			} else
				memcpy((char*)o + field->offset, sp [-1].data.p, i32);

			sp -= 2;
			vt_sp -= (i32 + 7) & ~7;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSFLDA) {
			MonoClassField *field = rtm->data_items[*(guint16 *)(ip + 1)];
			MonoVTable *vt = mono_class_vtable (context->domain, field->parent);
			gpointer addr;

			if (!vt->initialized) {
				frame->ip = ip;
				mono_runtime_class_init (vt);
			}
			ip += 2;

			if (context->domain->special_static_fields && (addr = g_hash_table_lookup (context->domain->special_static_fields, field)))
				sp->data.p = mono_get_special_static_data (GPOINTER_TO_UINT (addr));
			else
				sp->data.p = (char*)(vt->data) + field->offset;
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSFLD) {
			MonoVTable *vt;
			MonoClassField *field;
			gpointer addr;

			field = rtm->data_items[*(guint16 *)(ip + 1)];
			vt = rtm->data_items [*(guint16 *)(ip + 2)];
			if (!vt->initialized) {
				frame->ip = ip;
				mono_runtime_class_init (vt);
			}
			ip += 3;
			if (context->domain->special_static_fields && (addr = g_hash_table_lookup (context->domain->special_static_fields, field)))
				addr = mono_get_special_static_data (GPOINTER_TO_UINT (addr));
			else
				addr = (char*)(vt->data) + field->offset;

			stackval_from_data (field->type, sp, addr, FALSE);
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSFLD_I4) {
			MonoClassField *field = rtm->data_items[*(guint16 *)(ip + 1)];
			MonoVTable *vt = rtm->data_items [*(guint16 *)(ip + 2)];
			if (!vt->initialized) {
				frame->ip = ip;
				mono_runtime_class_init (vt);
			}
			ip += 3;
			sp->data.i = * (gint32 *)((char*)(vt->data) + field->offset);
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSFLD_O) {
			MonoClassField *field = rtm->data_items[*(guint16 *)(ip + 1)];
			MonoVTable *vt = rtm->data_items [*(guint16 *)(ip + 2)];
			if (!vt->initialized) {
				frame->ip = ip;
				mono_runtime_class_init (vt);
			}
			ip += 3;
			sp->data.p = * (gpointer *)((char*)(vt->data) + field->offset);
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_LDSFLD_VT) {
			MonoVTable *vt;
			MonoClassField *field;
			guint32 token;
			gpointer addr;
			int size;

			token = * (guint16 *)(ip + 1);
			size = READ32(ip + 2);
			field = rtm->data_items[token];
			ip += 4;
						
			vt = mono_class_vtable (context->domain, field->parent);
			if (!vt->initialized) {
				frame->ip = ip - 2;
				mono_runtime_class_init (vt);
			}
			
			if (context->domain->special_static_fields && (addr = g_hash_table_lookup (context->domain->special_static_fields, field)))
				addr = mono_get_special_static_data (GPOINTER_TO_UINT (addr));
			else
				addr = (char*)(vt->data) + field->offset;

			sp->data.p = vt_sp;
			vt_sp += (size + 7) & ~7;
			stackval_from_data (field->type, sp, addr, FALSE);
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STSFLD) {
			MonoVTable *vt;
			MonoClassField *field;
			guint32 token;
			gpointer addr;

			token = * (guint16 *)(ip + 1);
			field = rtm->data_items[token];
			ip += 2;
			--sp;

			vt = mono_class_vtable (context->domain, field->parent);
			if (!vt->initialized) {
				frame->ip = ip - 2;
				mono_runtime_class_init (vt);
			}
			
			if (context->domain->special_static_fields && (addr = g_hash_table_lookup (context->domain->special_static_fields, field)))
				addr = mono_get_special_static_data (GPOINTER_TO_UINT (addr));
			else
				addr = (char*)(vt->data) + field->offset;

			stackval_to_data (field->type, sp, addr, FALSE);
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STSFLD_VT) {
			MonoVTable *vt;
			MonoClassField *field;
			guint32 token;
			gpointer addr;
			int size;

			token = * (guint16 *)(ip + 1);
			size = READ32(ip + 2);
			field = rtm->data_items[token];
			ip += 4;
						
			vt = mono_class_vtable (context->domain, field->parent);
			if (!vt->initialized) {
				frame->ip = ip - 2;
				mono_runtime_class_init (vt);
			}
			
			if (context->domain->special_static_fields && (addr = g_hash_table_lookup (context->domain->special_static_fields, field)))
				addr = mono_get_special_static_data (GPOINTER_TO_UINT (addr));
			else
				addr = (char*)(vt->data) + field->offset;
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
			int size;
			c = rtm->data_items[* (guint16 *)(ip + 1)];
			ip += 2;
			size = mono_class_value_size (c, NULL);
			memcpy(sp [-2].data.p, &sp [-1].data, size);
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
		MINT_IN_CASE(MINT_CONV_OVF_U8_R8)
		MINT_IN_CASE(MINT_CONV_OVF_I8_UN_R8)
			if (sp [-1].data.f < 0 || sp [-1].data.f > 9223372036854775807LL)
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
		MINT_IN_CASE(MINT_BOX)
			c = rtm->data_items [* (guint16 *)(ip + 1)];

			if (c->byval_arg.type == MONO_TYPE_VALUETYPE && !c->enumtype) {
				int size = mono_class_value_size (c, NULL);
				sp [-1].data.p = mono_value_box (context->domain, c, sp [-1].data.p);
				size = (size + 7) & ~7;
				vt_sp -= size;
			}				
			else {
				stackval_to_data (&c->byval_arg, &sp [-1], (char*)&sp [-1], FALSE);
				sp [-1].data.p = mono_value_box (context->domain, c, &sp [-1]);
			}
			ip += 2;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_NEWARR)
			sp [-1].data.p = (MonoObject*) mono_array_new (context->domain, rtm->data_items[*(guint16 *)(ip + 1)], sp [-1].data.i);
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
			sp [-1].data.i = mono_string_length ((MonoString*)sp [-1].data.p);
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_ARRAY_RANK)
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);
			sp [-1].data.i = mono_object_class (sp [-1].data.p)->rank;
			ip++;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDELEMA) {
			guint32 esize;
			mono_u aindex;
			
			/*token = READ32 (ip)*/;
			ip += 2;
			sp -= 2;

			o = sp [0].data.p;

			aindex = sp [1].data.i;
			if (aindex >= mono_array_length ((MonoArray *) o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip - 2);

			/* check the array element corresponds to token */
			esize = mono_array_element_size (((MonoArray *) o)->obj.vtable->klass);
			
			sp->data.p = mono_array_addr_with_size ((MonoArray *) o, esize, aindex);
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
		MINT_IN_CASE(MINT_LDELEM_REF) {
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
			default:
				ves_abort();
			}

			++ip;
			++sp;
			MINT_IN_BREAK;
		}
		MINT_IN_CASE(MINT_STELEM_I)  /* fall through */
		MINT_IN_CASE(MINT_STELEM_I1) /* fall through */ 
		MINT_IN_CASE(MINT_STELEM_I2) /* fall through */
		MINT_IN_CASE(MINT_STELEM_I4) /* fall through */
		MINT_IN_CASE(MINT_STELEM_I8) /* fall through */
		MINT_IN_CASE(MINT_STELEM_R4) /* fall through */
		MINT_IN_CASE(MINT_STELEM_R8) /* fall through */
		MINT_IN_CASE(MINT_STELEM_REF) {
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
			case MINT_STELEM_I2:
				mono_array_set ((MonoArray *)o, gint16, aindex, sp [2].data.i);
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
			case MINT_STELEM_REF:
				if (sp [2].data.p && !mono_object_isinst (sp [2].data.p, mono_object_class (o)->element_class))
					THROW_EX (mono_get_exception_array_type_mismatch (), ip);
				mono_array_set ((MonoArray *)o, gpointer, aindex, sp [2].data.p);
				break;
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

		MINT_IN_CASE(MINT_REFANYVAL) ves_abort(); MINT_IN_BREAK;
#endif
		MINT_IN_CASE(MINT_CKFINITE)
			if (!finite(sp [-1].data.f))
				THROW_EX (mono_get_exception_arithmetic (), ip);
			++ip;
			MINT_IN_BREAK;
#if 0
		MINT_IN_CASE(MINT_MKREFANY) ves_abort(); MINT_IN_BREAK;
#endif
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
		MINT_IN_CASE(MINT_ICALL_P_V) 
		MINT_IN_CASE(MINT_ICALL_P_P)
		MINT_IN_CASE(MINT_ICALL_PP_V)
		MINT_IN_CASE(MINT_ICALL_PI_V)
		MINT_IN_CASE(MINT_ICALL_PP_P)
		MINT_IN_CASE(MINT_ICALL_PI_P)
		MINT_IN_CASE(MINT_ICALL_PPP_V)
		MINT_IN_CASE(MINT_ICALL_PPI_V)
			sp = do_icall (context, *ip, sp, rtm->data_items [*(guint16 *)(ip + 1)]);
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
			sp->data.p = mono_object_new (context->domain, rtm->data_items [*(guint16 *)(ip + 1)]);
			ip += 2;
			sp++;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_MONO_FREE)
			++ip;
			--sp;
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
				
			sp->data.p = get_virtual_method (m, sp->data.p);
			++sp;
			MINT_IN_BREAK;
		}

		MINT_IN_CASE(MINT_LDTHIS)
			sp->data.p = frame->obj;
			++ip;
			++sp; 
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_STTHIS)
			--sp; 
			frame->obj = sp->data.p;
			++ip;
			MINT_IN_BREAK;
		MINT_IN_CASE(MINT_LDTHISA)
			sp->data.p = &frame->obj;
			++ip;
			++sp; 
			MINT_IN_BREAK;

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

		MINT_IN_CASE(MINT_LOCALLOC)
			if (sp != frame->stack + 1) /*FIX?*/
				THROW_EX (mono_get_exception_execution_engine (NULL), ip);
			sp [-1].data.p = alloca (sp [-1].data.i);
			++ip;
			MINT_IN_BREAK;
#if 0
		MINT_IN_CASE(MINT_ENDFILTER) ves_abort(); MINT_IN_BREAK;
#endif
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
				if (clause->flags <= 1 && MONO_OFFSET_IN_CLAUSE (clause, ip_offset)) {
					if (!clause->flags) {
						if (mono_object_isinst ((MonoObject*)frame->ex, clause->data.catch_class)) {
							/* 
							 * OK, we found an handler, now we need to execute the finally
							 * and fault blocks before branching to the handler code.
							 */
							inv->ex_handler = clause;
#if DEBUG_INTERP
							if (tracing)
								g_print ("* Found handler at '%s'\n", method->name);
#endif
							goto handle_finally;
						}
					} else {
						/* FIXME: handle filter clauses */
						g_assert (0);
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
		ex_obj = (MonoObject*)frame->ex;
		mono_unhandled_exception (ex_obj);
		exit (1);
	}
	handle_finally:
	{
		int i;
		guint32 ip_offset;
		MonoExceptionClause *clause;
		GSList *old_list = finally_ips;
		MonoMethod *method = frame->runtime_method->method;
		MonoMethodHeader *header = mono_method_get_header (method);
		
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
		MonoMethodHeader *header = mono_method_get_header (frame->runtime_method->method);
		
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
			vt_sp = (char *)sp + rtm->stack_size;
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
	ThreadContext *context = TlsGetValue (thread_context_id);
	ThreadContext context_struct;
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
		TlsSetValue (thread_context_id, context);
	}
	frame->ip = NULL;
	frame->parent = context->current_frame;
	frame->runtime_method = mono_interp_get_runtime_method (frame->method);
	context->managed_code = 1;
	ves_exec_method_with_context(frame, context);
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
		TlsSetValue (thread_context_id, NULL);
	else
		context->current_frame = frame->parent;
}

static int 
ves_exec (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[])
{
	MonoImage *image = mono_assembly_get_image (assembly);
	MonoMethod *method;
	MonoObject *exc = NULL;
	int rval;

	method = mono_get_method (image, mono_image_get_entry_point (image), NULL);
	if (!method)
		g_error ("No entry point method found in %s", mono_image_get_filename (image));

	rval = mono_runtime_run_main (method, argc, argv, &exc);
	if (exc != NULL)
		mono_unhandled_exception (exc);

	return rval;
}

static void
usage (void)
{
	fprintf (stderr,
		 "mint %s, the Mono ECMA CLI interpreter, (C) 2001, 2002 Ximian, Inc.\n\n"
		 "Usage is: mint [options] executable args...\n\n", VERSION);
	fprintf (stderr,
		 "Runtime Debugging:\n"
#ifdef DEBUG_INTERP
		 "   --debug\n"
#endif
		 "   --dieonex\n"
		 "   --noptr\t\t\tdon't print pointer addresses in trace output\n"
		 "   --opcode-count\n"
		 "   --print-vtable\n"
		 "   --traceclassinit\n"
		 "\n"
		 "Development:\n"
		 "   --debug method_name\n"
		 "   --profile\n"
		 "   --trace\n"
		 "   --traceops\n"
		 "\n"
		 "Runtime:\n"
		 "   --config filename  load the specified config file instead of the default\n"
		 "   --workers n        maximum number of worker threads\n"
		);
	exit (1);
}

#ifdef RUN_TEST
static void
test_load_class (MonoImage* image)
{
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEDEF];
	MonoClass *klass;
	int i;

	for (i = 1; i <= t->rows; ++i) {
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | i);
		mono_class_init (klass);
	}
}
#endif

static void
add_signal_handler (int signo, void (*handler)(int))
{
#ifdef HOST_WIN32
	signal (signo, handler);
#else
	struct sigaction sa;

	sa.sa_handler = handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;

	g_assert (sigaction (signo, &sa, NULL) != -1);
#endif
}

static void
segv_handler (int signum)
{
	ThreadContext *context = TlsGetValue (thread_context_id);
	MonoException *segv_exception;

	if (context == NULL)
		return;
	segv_exception = mono_get_exception_null_reference ();
	segv_exception->message = mono_string_new (mono_domain_get (), "Null Reference (SIGSEGV)");
	mono_raise_exception (segv_exception);
}


static void
quit_handler (int signum)
{
	ThreadContext *context = TlsGetValue (thread_context_id);
	MonoException *quit_exception;

	if (context == NULL)
		return;
	quit_exception = mono_get_exception_execution_engine ("Interrupted (SIGQUIT).");
	mono_raise_exception (quit_exception);
}

static void
abrt_handler (int signum)
{
	ThreadContext *context = TlsGetValue (thread_context_id);
	MonoException *abrt_exception;

	if (context == NULL)
		return;
	abrt_exception = mono_get_exception_execution_engine ("Abort (SIGABRT).");
	mono_raise_exception (abrt_exception);
}

static void
thread_abort_handler (int signum)
{
	ThreadContext *context = TlsGetValue (thread_context_id);
	MonoException *exc;

	if (context == NULL)
		return;

	exc = mono_thread_request_interruption (context->managed_code); 
	if (exc) mono_raise_exception (exc);
}

static MonoBoolean
ves_icall_get_frame_info (gint32 skip, MonoBoolean need_file_info, 
			  MonoReflectionMethod **method, 
			  gint32 *iloffset, gint32 *native_offset,
			  MonoString **file, gint32 *line, gint32 *column)
{
	ThreadContext *context = TlsGetValue (thread_context_id);
	MonoInvocation *inv = context->current_frame;
	int i;

	for (i = 0; inv && i < skip; inv = inv->parent)
		if (inv->runtime_method != NULL)
			++i;

	if (iloffset)
		*iloffset = 0;
	if (native_offset)
		*native_offset = 0;
	if (method)
		*method = inv == NULL ? NULL : mono_method_get_object (context->domain, inv->runtime_method->method, NULL);
	if (line)
		*line = 0;
	if (need_file_info) {
		if (column)
			*column = 0;
		if (file)
			*file = mono_string_new (mono_domain_get (), "unknown");
	}

	return TRUE;
}

static MonoArray *
ves_icall_get_trace (MonoException *exc, gint32 skip, MonoBoolean need_file_info)
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	MonoArray *ta = exc->trace_ips;
	int i, len;

	if (ta == NULL) {
		/* Exception is not thrown yet */
		return mono_array_new (domain, mono_defaults.stack_frame_class, 0);
	}
	
	len = mono_array_length (ta);

	res = mono_array_new (domain, mono_defaults.stack_frame_class, len > skip ? len - skip : 0);

	for (i = skip; i < len / 2; i++) {
		MonoStackFrame *sf = (MonoStackFrame *)mono_object_new (domain, mono_defaults.stack_frame_class);
		gushort *ip = mono_array_get (ta, gpointer, 2 * i + 1);
		RuntimeMethod *rtm = mono_array_get (ta, gpointer, 2 * i);

		if (rtm != NULL) {
			sf->method = mono_method_get_object (domain, rtm->method, NULL);
			sf->native_offset = ip - rtm->code;
		}

#if 0
		sf->il_offset = mono_debug_il_offset_from_address (ji->method, sf->native_offset, domain);

		if (need_file_info) {
			gchar *filename;
			
			filename = mono_debug_source_location_from_address (ji->method, sf->native_offset, &sf->line, domain);

			sf->filename = filename? mono_string_new (domain, filename): NULL;
			sf->column = 0;

			g_free (filename);
		}
#endif

		mono_array_set (res, gpointer, i, sf);
	}

	return res;
}

static MonoObject *
ves_icall_System_Delegate_CreateDelegate_internal (MonoReflectionType *type, MonoObject *target,
						   MonoReflectionMethod *info)
{
	MonoClass *delegate_class = mono_class_from_mono_type (type->type);
	MonoObject *delegate;

	mono_assert (delegate_class->parent == mono_defaults.multicastdelegate_class);

	delegate = mono_object_new (mono_object_domain (type), delegate_class);

	interp_delegate_ctor (mono_object_domain (type), delegate, target, mono_interp_get_runtime_method (info->method));

	return delegate;
}


typedef struct
{
	MonoDomain *domain;
	int enable_debugging;
	char *file;
	int argc;
	char **argv;
} MainThreadArgs;

static void main_thread_handler (gpointer user_data)
{
	MainThreadArgs *main_args=(MainThreadArgs *)user_data;
	MonoAssembly *assembly;

	if (main_args->enable_debugging) {
		mono_debug_init (MONO_DEBUG_FORMAT_MONO);
		mono_debug_init_1 (main_args->domain);
	}

	assembly = mono_domain_assembly_open (main_args->domain,
					      main_args->file);

	if (!assembly){
		fprintf (stderr, "Can not open image %s\n", main_args->file);
		exit (1);
	}

	if (main_args->enable_debugging)
		mono_debug_init_2 (assembly);

#ifdef RUN_TEST
	test_load_class (assembly->image);
#else

	ves_exec (main_args->domain, assembly, main_args->argc, main_args->argv);
#endif
}

static void
mono_runtime_install_handlers (void)
{
	add_signal_handler (SIGSEGV, segv_handler);
	add_signal_handler (SIGINT, quit_handler);
	add_signal_handler (SIGABRT, abrt_handler);
	add_signal_handler (mono_thread_get_abort_signal (), thread_abort_handler);
}

static void
quit_function (MonoDomain *domain, gpointer user_data)
{
	mono_profiler_shutdown ();
	
	mono_runtime_cleanup (domain);
	mono_domain_free (domain, TRUE);

}

void
mono_interp_cleanup(MonoDomain *domain)
{
	quit_function (domain, NULL);
}

int
mono_interp_exec(MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[])
{
	return ves_exec (domain, assembly, argc, argv);
}

MonoDomain *
mono_interp_init(const char *file)
{
	MonoDomain *domain;

	g_set_prgname (file);
	mono_set_rootdir ();
	
	g_log_set_always_fatal (G_LOG_LEVEL_ERROR);
	g_log_set_fatal_mask (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR);

	if (!g_thread_supported ())
		g_thread_init (NULL);

	thread_context_id = TlsAlloc ();
	TlsSetValue (thread_context_id, NULL);
	InitializeCriticalSection (&runtime_method_lookup_section);
	InitializeCriticalSection (&create_method_pointer_mutex);

	mono_runtime_install_handlers ();
	mono_interp_transform_init ();
	mono_install_compile_method (mono_create_method_pointer);
	mono_install_runtime_invoke (interp_mono_runtime_invoke);
	mono_install_remoting_trampoline (interp_create_remoting_trampoline);
	mono_install_trampoline (interp_create_trampoline);

	mono_install_handler (interp_ex_handler);
	mono_install_stack_walk (interp_walk_stack);
	mono_install_runtime_cleanup (quit_function);
	abort_requested = mono_thread_interruption_request_flag ();

	domain = mono_init_from_assembly (file, file);
#ifdef __hpux /* generates very big stack frames */
	mono_threads_set_default_stacksize(32*1024*1024);
#endif
	mono_icall_init ();
	mono_add_internal_call ("System.Diagnostics.StackFrame::get_frame_info", ves_icall_get_frame_info);
	mono_add_internal_call ("System.Diagnostics.StackTrace::get_trace", ves_icall_get_trace);
	mono_add_internal_call ("Mono.Runtime::mono_runtime_install_handlers", mono_runtime_install_handlers);
	mono_add_internal_call ("System.Delegate::CreateDelegate_internal", ves_icall_System_Delegate_CreateDelegate_internal);

 	mono_register_jit_icall (mono_thread_interruption_checkpoint, "mono_thread_interruption_checkpoint", mono_create_icall_signature ("void"), FALSE);

	mono_runtime_init (domain, NULL, NULL);


	mono_thread_attach (domain);
	return domain;
}

int 
mono_main (int argc, char *argv [])
{
	MonoDomain *domain;
	int retval = 0, i;
	char *file, *config_file = NULL;
	int enable_debugging = FALSE;
	MainThreadArgs main_args;
	const char *error;

	setlocale (LC_ALL, "");
	if (argc < 2)
		usage ();

	MONO_GC_PRE_INIT ();
	
	for (i = 1; i < argc && argv [i][0] == '-'; i++){
		if (strcmp (argv [i], "--trace") == 0)
			global_tracing = 1;
		if (strcmp (argv [i], "--noptr") == 0)
			global_no_pointers = 1;
		if (strcmp (argv [i], "--traceops") == 0)
			global_tracing = 2;
		if (strcmp (argv [i], "--traceopt") == 0)
			++mono_interp_traceopt;
		if (strcmp (argv [i], "--dieonex") == 0) {
			die_on_exception = 1;
			enable_debugging = 1;
		}
		if (strcmp (argv [i], "--print-vtable") == 0)
			mono_print_vtable = TRUE;
		if (strcmp (argv [i], "--profile") == 0)
			mono_profiler_load (NULL);
		if (strcmp (argv [i], "--config") == 0)
			config_file = argv [++i];
		if (strcmp (argv [i], "--help") == 0)
			usage ();
#if DEBUG_INTERP
		if (strcmp (argv [i], "--debug") == 0) {
			MonoMethodDesc *desc = mono_method_desc_new (argv [++i], FALSE);
			if (!desc)
				g_error ("Invalid method name '%s'", argv [i]);
			db_methods = g_list_append (db_methods, desc);
		}
		if (strcmp (argv [i], "--nested") == 0)
			nested_trace = 1;
#endif
	}
	
	file = argv [i];

	if (!file)
		usage ();

	domain = mono_interp_init(file);
	mono_config_parse (config_file);

	error = mono_check_corlib_version ();
	if (error) {
		fprintf (stderr, "Corlib not in sync with this runtime: %s\n", error);
		fprintf (stderr, "Download a newer corlib at http://www.go-mono.com/daily.\n");
		exit (1);
	}

	main_args.domain=domain;
	main_args.file=file;
	main_args.argc=argc-i;
	main_args.argv=argv+i;
	main_args.enable_debugging=enable_debugging;
	
	mono_runtime_exec_managed_code (domain, main_thread_handler,
					&main_args);

	quit_function (domain, NULL);

	/* Get the return value from System.Environment.ExitCode */
	retval=mono_environment_exitcode_get ();

#if COUNT_OPS
	for (i = 0; i < 512; i++)
		if (opcode_counts[i] != 0)
			printf("%s %d\n", mono_interp_opname[i], opcode_counts[i]);
#endif
	return retval;
}
