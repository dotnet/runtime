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

#include <mono/os/gc_wrapper.h>

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
#include <mono/metadata/blob.h>
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
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-debug.h>
#include <mono/os/util.h>

/*#include <mono/cli/types.h>*/
#include "interp.h"
#include "hacks.h"

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

/* If true, then we output the opcodes as we interpret them */
static int global_tracing = 0;
static int global_no_pointers = 0;

static int debug_indent_level = 0;

/*
 * Pull the list of opcodes
 */
#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

#if SIZEOF_VOID_P == 8
#define GET_NATI(sp) ((sp).type == VAL_I32 ? (sp).data.i : (sp).data.nati)
#else
#define GET_NATI(sp) (sp).data.i
#endif
#define CSIZE(x) (sizeof (x) / 4)

#define INIT_FRAME(frame,parent_frame,obj_this,method_args,method_retval,mono_method)	\
	do {	\
		(frame)->parent = (parent_frame);	\
		(frame)->obj = (obj_this);	\
		(frame)->stack_args = (method_args);	\
		(frame)->retval = (method_retval);	\
		(frame)->method = (mono_method);	\
		(frame)->ex_handler = NULL;	\
		(frame)->ex = NULL;	\
		(frame)->child = NULL;	\
		(frame)->ip = NULL;	\
		(frame)->invoke_trap = 0;	\
	} while (0)

typedef struct {
	MonoInvocation *base_frame;
	MonoInvocation *current_frame;
	MonoInvocation *env_frame;
	jmp_buf *current_env;
	int search_for_handler;
} ThreadContext;

static MonoException * quit_exception = NULL;

void ves_exec_method (MonoInvocation *frame);

static char* dump_stack (stackval *stack, stackval *sp);
static char* dump_frame (MonoInvocation *inv);
static void ves_exec_method_with_context (MonoInvocation *frame, ThreadContext *context);

typedef void (*ICallMethod) (MonoInvocation *frame);

static guint32 die_on_exception = 0;
static guint32 thread_context_id = 0;

#define DEBUG_INTERP 1
#if DEBUG_INTERP

static unsigned long opcode_count = 0;
static unsigned long fcall_count = 0;
static int break_on_method = 0;
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
	fcall_count++;	\
	g_list_foreach (db_methods, db_match_method, (gpointer)frame->method);	\
	if (break_on_method) tracing=2;	\
	break_on_method = 0;	\
	if (tracing) {	\
		char *mn, *args = dump_stack (frame->stack_args, frame->stack_args+signature->param_count);	\
		debug_indent_level++;	\
		output_indent ();	\
		mn = mono_method_full_name (frame->method, FALSE); \
		g_print ("(%u) Entering %s (", GetCurrentThreadId(), mn);	\
		g_free (mn); \
		if (signature->hasthis) { \
			if (global_no_pointers) { \
				g_print ("this%s ", frame->obj ? "" : "=null"); \
			} else { \
				g_print ("%p ", frame->obj); } \
		} \
		g_print ("%s)\n", args);	\
		g_free (args);	\
	}	\
	if (mono_profiler_events & MONO_PROFILE_ENTER_LEAVE)	\
		mono_profiler_method_enter (frame->method);

#define DEBUG_LEAVE()	\
	if (tracing) {	\
		char *mn, *args;	\
		if (signature->ret->type != MONO_TYPE_VOID)	\
			args = dump_stack (frame->retval, frame->retval + 1);	\
		else	\
			args = g_strdup ("");	\
		output_indent ();	\
		mn = mono_method_full_name (frame->method, FALSE); \
		g_print ("(%u) Leaving %s", GetCurrentThreadId(),  mn);	\
		g_free (mn); \
		g_print (" => %s\n", args);	\
		g_free (args);	\
		debug_indent_level--;	\
	}	\
	if (mono_profiler_events & MONO_PROFILE_ENTER_LEAVE)	\
		mono_profiler_method_leave (frame->method);

#else

#define DEBUG_ENTER()
#define DEBUG_LEAVE()

#endif

static void
interp_ex_handler (MonoException *ex) {
	ThreadContext *context = TlsGetValue (thread_context_id);
	char *stack_trace;
	stack_trace = dump_frame (context->current_frame);
	ex->stack_trace = mono_string_new (mono_domain_get(), stack_trace);
	g_free (stack_trace);
	if (context == NULL || context->current_env == NULL) {
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
		exit(1);
	}
	context->env_frame->ex = ex;
	context->search_for_handler = 1;
	longjmp (*context->current_env, 1);
}

static void
ves_real_abort (int line, MonoMethod *mh,
		const unsigned char *ip, stackval *stack, stackval *sp)
{
	MonoMethodNormal *mm = (MonoMethodNormal *)mh;
	fprintf (stderr, "Execution aborted in method: %s::%s\n", mh->klass->name, mh->name);
	fprintf (stderr, "Line=%d IP=0x%04x, Aborted execution\n", line,
		 ip-mm->header->code);
	g_print ("0x%04x %02x\n",
		 ip-mm->header->code, *ip);
	if (sp > stack)
		printf ("\t[%d] %d 0x%08x %0.5f\n", sp-stack, sp[-1].type, sp[-1].data.i, sp[-1].data.f);
}
#define ves_abort() do {ves_real_abort(__LINE__, frame->method, ip, frame->stack, sp); THROW_EX (mono_get_exception_execution_engine (NULL), ip);} while (0);

static gpointer
interp_create_remoting_trampoline (MonoMethod *method)
{
	return mono_marshal_get_remoting_invoke (method);
}

static MonoMethod*
get_virtual_method (MonoDomain *domain, MonoMethod *m, stackval *objs)
{
	MonoObject *obj;
	MonoClass *klass;
	MonoMethod **vtable;
	gboolean is_proxy = FALSE;
	MonoMethod *res;

	if ((m->flags & METHOD_ATTRIBUTE_FINAL) || !(m->flags & METHOD_ATTRIBUTE_VIRTUAL))
			return m;

	obj = objs->data.p;
	if ((klass = obj->vtable->klass) == mono_defaults.transparent_proxy_class) {
		klass = ((MonoTransparentProxy *)obj)->klass;
		is_proxy = TRUE;
	}
	vtable = (MonoMethod **)klass->vtable;

	if (m->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		res = ((MonoMethod **)obj->vtable->interface_offsets [m->klass->interface_id]) [m->slot];
	} else {
		res = vtable [m->slot];
	}
	g_assert (res);

	if (is_proxy)
		return mono_marshal_get_remoting_invoke (res);
	
	return res;
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
			result->type = VAL_OBJ;
			break;
		default:
			result->type = VAL_MP;
			break;
		}
		result->data.p = *(gpointer*)data;
		return;
	}
	switch (type->type) {
	case MONO_TYPE_VOID:
		return;
	case MONO_TYPE_I1:
		result->type = VAL_I32;
		result->data.i = *(gint8*)data;
		return;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		result->type = VAL_I32;
		result->data.i = *(guint8*)data;
		return;
	case MONO_TYPE_I2:
		result->type = VAL_I32;
		result->data.i = *(gint16*)data;
		return;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		result->type = VAL_I32;
		result->data.i = *(guint16*)data;
		return;
	case MONO_TYPE_I4:
		result->type = VAL_I32;
		result->data.i = *(gint32*)data;
		return;
	case MONO_TYPE_U:
	case MONO_TYPE_I:
		result->type = VAL_NATI;
		result->data.nati = *(mono_i*)data;
		return;
	case MONO_TYPE_PTR:
		result->type = VAL_TP;
		result->data.p = *(gpointer*)data;
		return;
	case MONO_TYPE_U4:
		result->type = VAL_I32;
		result->data.i = *(guint32*)data;
		return;
	case MONO_TYPE_R4:
		result->type = VAL_DOUBLE;
		result->data.f = *(float*)data;
		return;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		result->type = VAL_I64;
		result->data.l = *(gint64*)data;
		return;
	case MONO_TYPE_R8:
		result->type = VAL_DOUBLE;
		result->data.f = *(double*)data;
		return;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
		result->type = VAL_OBJ;
		result->data.p = *(gpointer*)data;
		return;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			stackval_from_data (type->data.klass->enum_basetype, result, data, pinvoke);
			return;
		} else {
			int size;
			result->type = VAL_VALUET;
			
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
	//printf ("TODAT0 %p\n", data);
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
		*p = val->type == VAL_I32 ? val->data.i : val->data.nati;
		return;
	}
	case MONO_TYPE_U: {
		mono_u *p = (mono_u*)data;
		/* see above. */
		*p = val->type == VAL_I32 ? (guint32)val->data.i : val->data.nati;
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

			memcpy (data, val->data.vt, size);
		}
		return;
	default:
		g_warning ("got type %x", type->type);
		g_assert_not_reached ();
	}
}

#define FILL_IN_TRACE(exception, frame) \
	do { \
		char *stack_trace;	\
		stack_trace = dump_frame (frame);	\
		(exception)->stack_trace = mono_string_new (mono_domain_get(), stack_trace);	\
		g_free (stack_trace);	\
	} while (0)

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

	esize = mono_array_element_size (ac);
	ea = mono_array_addr_with_size (ao, esize, pos);

	mt = frame->method->signature->params [ac->rank];
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

	mt = frame->method->signature->ret;
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

	frame->retval->type = VAL_MP;
	frame->retval->data.p = ea;
}

static void
interp_walk_stack (MonoStackWalk func, gpointer user_data)
{
	ThreadContext *context = TlsGetValue (thread_context_id);
	MonoInvocation *frame = context->current_frame;
	int il_offset;
	MonoMethodHeader *hd;

	while (frame) {
		gboolean managed = FALSE;
		if (!frame->method || (frame->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) || 
				(frame->method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)))
			il_offset = -1;
		else {
			hd = ((MonoMethodNormal*)frame->method)->header;
			il_offset = frame->ip - hd->code;
			if (!frame->method->wrapper_type)
				managed = TRUE;
		}
		if (func (frame->method, -1, il_offset, managed, user_data))
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
		context->search_for_handler = 0;
		return;
	}

	context->env_frame = frame;
	context->current_env = &env;

	if (frame->method) {
		if (!frame->method->info) {
			func = frame->method->info = mono_create_trampoline (sig, string_ctor);
		} else { 
			func = (MonoPIFunc)frame->method->info;
		}
	} else {
		func = mono_create_trampoline (sig, string_ctor);
	}

	context->current_frame = frame;

	func (addr, &frame->retval->data.p, frame->obj, frame->stack_args);

	if (string_ctor) {
		stackval_from_data (&mono_defaults.string_class->byval_arg, 
				    frame->retval, (char*)&frame->retval->data.p, sig->pinvoke);
 	} else if (!MONO_TYPE_ISSTRUCT (sig->ret))
		stackval_from_data (sig->ret, frame->retval, (char*)&frame->retval->data.p, sig->pinvoke);

	context->current_frame = old_frame;
	context->env_frame = old_env_frame;
	context->current_env = old_env;
}

/*
 * From the spec:
 * runtime specifies that the implementation of the method is automatically
 * provided by the runtime and is primarily used for the methods of delegates.
 */
static void
ves_runtime_method (MonoInvocation *frame, ThreadContext *context)
{
	const char *name = frame->method->name;
	MonoObject *obj = (MonoObject*)frame->obj;
	MonoInvocation call;
	MonoMethod *nm;


	mono_class_init (frame->method->klass);
	
	if (obj && mono_object_isinst (obj, mono_defaults.multicastdelegate_class)) {
		if (*name == '.' && (strcmp (name, ".ctor") == 0)) {
			mono_delegate_ctor (obj, frame->stack_args[0].data.p, frame->stack_args[1].data.p);
			return;
		}
		if (*name == 'I' && (strcmp (name, "Invoke") == 0)) {
			nm = mono_marshal_get_delegate_invoke (frame->method);
			INIT_FRAME(&call,frame,obj,frame->stack_args,frame->retval,nm);
			ves_exec_method_with_context (&call, context);
			frame->ex = call.ex;
			return;
		}
		if (*name == 'B' && (strcmp (name, "BeginInvoke") == 0)) {
			nm = mono_marshal_get_delegate_begin_invoke (frame->method);
			INIT_FRAME(&call,frame,obj,frame->stack_args,frame->retval,nm);
			ves_exec_method_with_context (&call, context);
			frame->ex = call.ex;
			return;
		}
		if (*name == 'E' && (strcmp (name, "EndInvoke") == 0)) {
			nm = mono_marshal_get_delegate_end_invoke (frame->method);
			INIT_FRAME(&call,frame,obj,frame->stack_args,frame->retval,nm);
			ves_exec_method_with_context (&call, context);
			frame->ex = call.ex;
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
			frame->method->klass->name_space, frame->method->klass->name,
			frame->method->name);
}

static char*
dump_stack (stackval *stack, stackval *sp)
{
	stackval *s = stack;
	GString *str = g_string_new ("");
	
	if (sp == stack)
		return g_string_free (str, FALSE);
	
	while (s < sp) {
		switch (s->type) {
#if SIZEOF_VOID_P == 4
		case VAL_NATI: g_string_sprintfa (str, "[%d/0x%0x] ", s->data.nati, s->data.nati); break;
#else
		case VAL_NATI: g_string_sprintfa (str, "[%lld/0x%0llx] ", s->data.nati, s->data.nati); break;
#endif
		case VAL_I32: g_string_sprintfa (str, "[%d] ", s->data.i); break;
		case VAL_I64: g_string_sprintfa (str, "[%lldL] ", s->data.l); break;
		case VAL_DOUBLE: g_string_sprintfa (str, "[%0.5f] ", s->data.f); break;
		case VAL_VALUET:
			if (!global_no_pointers)
				g_string_sprintfa (str, "[vt: %p] ", s->data.vt);
			else
				g_string_sprintfa (str, "[vt%s] ", s->data.vt ? "" : "=null");
			break;
		case VAL_OBJ: {
			MonoObject *obj =  s->data.p;
			if (global_no_pointers && obj && obj->vtable) {
				MonoClass *klass = mono_object_class (obj);
				if (klass == mono_defaults.string_class) {
					char *utf8 = mono_string_to_utf8 ((MonoString*) obj);
					g_string_sprintfa (str, "[str:%s] ", utf8);
					g_free (utf8);
					break;
				} else if (klass == mono_defaults.sbyte_class) {
					g_string_sprintfa (str, "[b:%d] ",
							   *(gint8 *)((guint8 *) obj + sizeof (MonoObject)));
					break;
				} else if (klass == mono_defaults.int16_class) {
					g_string_sprintfa (str, "[b:%d] ",
							   *(gint16 *)((guint8 *) obj + sizeof (MonoObject)));
					break;
				} else if (klass == mono_defaults.int32_class) {
					g_string_sprintfa (str, "[b:%d] ",
							   *(gint32 *)((guint8 *) obj + sizeof (MonoObject)));
					break;
				} else if (klass == mono_defaults.byte_class) {
					g_string_sprintfa (str, "[b:%u] ",
							   *(guint8 *)((guint8 *) obj + sizeof (MonoObject)));
					break;
				} else if (klass == mono_defaults.char_class
					   || klass == mono_defaults.uint16_class) {
					g_string_sprintfa (str, "[b:%u] ",
							   *(guint16 *)((guint8 *) obj + sizeof (MonoObject)));
					break;
				} else if (klass == mono_defaults.uint32_class) {
					g_string_sprintfa (str, "[b:%u] ",
							   *(guint32 *)((guint8 *) obj + sizeof (MonoObject)));
					break;
				} else if (klass == mono_defaults.int64_class) {
					g_string_sprintfa (str, "[b:%lld] ",
							   *(gint64 *)((guint8 *) obj + sizeof (MonoObject)));
					break;
				} else if (klass == mono_defaults.uint64_class) {
					g_string_sprintfa (str, "[b:%llu] ",
							   *(guint64 *)((guint8 *) obj + sizeof (MonoObject)));
					break;
				} else if (klass == mono_defaults.double_class) {
					g_string_sprintfa (str, "[b:%0.5f] ",
							   *(gdouble *)((guint8 *) obj + sizeof (MonoObject)));
					break;
				} else if (klass == mono_defaults.single_class) {
					g_string_sprintfa (str, "[b:%0.5f] ",
							   *(gfloat *)((guint8 *) obj + sizeof (MonoObject)));
					break;
				} else if (klass == mono_defaults.boolean_class) {
					g_string_sprintfa (str, "[b:%s] ",
							   *(gboolean *)((guint8 *) obj + sizeof (MonoObject))
							   ? "true" : "false");
					break;
				}
			}
			/* fall thru */
		}
		default:
			if (!global_no_pointers)
				g_string_sprintfa (str, "[%c:%p] ", s->type == VAL_OBJ ? 'O' : s->type == VAL_MP ? 'M' : '?', s->data.p);
			else
				g_string_sprintfa (str, s->data.p ? "[ptr] " : "[null] ");
			break;
		}
		++s;
	}
	return g_string_free (str, FALSE);
}

static char*
dump_frame (MonoInvocation *inv)
{
	GString *str = g_string_new ("");
	int i;
	char *args;
	for (i = 0; inv; inv = inv->parent, ++i) {
		if (inv->method != NULL) {
			MonoClass *k;

			int codep = 0;
			const char * opname = "";
			gchar *source = NULL;

			if (!inv->method) {
				--i;
				continue;
			}

			k = inv->method->klass;

			if ((inv->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) == 0 &&
				(inv->method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) == 0) {
				MonoMethodHeader *hd = ((MonoMethodNormal *)inv->method)->header;

				if (hd != NULL) {
					if (inv->ip)
						codep = *(inv->ip) == 0xfe? inv->ip [1] + 256: *(inv->ip);
					else
						codep = 0;
					opname = mono_opcode_names [codep];
					codep = inv->ip - hd->code;
	
					source = mono_debug_source_location_from_il_offset (inv->method, codep, NULL);
				}
			}
			args = dump_stack (inv->stack_args, inv->stack_args + inv->method->signature->param_count);
			if (source)
				g_string_sprintfa (str, "#%d: 0x%05x %-10s in %s.%s::%s (%s) at %s\n", i, codep, opname,
						   k->name_space, k->name, inv->method->name, args, source);
			else
				g_string_sprintfa (str, "#%d: 0x%05x %-10s in %s.%s::%s (%s)\n", i, codep, opname,
						   k->name_space, k->name, inv->method->name, args);
			g_free (args);
			g_free (source);
		}
	}
	return g_string_free (str, FALSE);
}

typedef enum {
	INLINE_STRING_LENGTH = 1,
	INLINE_STRING_GET_CHARS,
	INLINE_ARRAY_LENGTH,
	INLINE_ARRAY_RANK,
	INLINE_TYPE_ELEMENT_TYPE
} InlineMethod;

typedef struct
{
	MonoClassField *field;
} MonoRuntimeFieldInfo;

typedef struct
{
	guint32 locals_size;
	guint32 args_size;
	MonoRuntimeFieldInfo *field_info;
	guint32 offsets[1];
} MethodRuntimeData;

static void
write32(unsigned char *p, guint32 v)
{
	p[0] = v & 0xff;
	p[1] = (v >> 8) & 0xff;
	p[2] = (v >> 16) & 0xff;
	p[3] = (v >> 24) & 0xff;
}

static CRITICAL_SECTION calc_section;

static void
calc_offsets (MonoImage *image, MonoMethod *method)
{
	int i, align, size, offset = 0;
	MonoMethodHeader *header = ((MonoMethodNormal*)method)->header;
	MonoMethodSignature *signature = method->signature;
	register const unsigned char *ip, *end;
	const MonoOpcode *opcode;
	guint32 token;
	MonoMethod *m;
	MonoClass *class;
	MonoDomain *domain = mono_domain_get ();
	MethodRuntimeData *rtd;
	int n_fields = 0;

	mono_profiler_method_jit (method); /* sort of... */
	/* intern the strings in the method. */
	ip = header->code;
	end = ip + header->code_size;
	while (ip < end) {
		i = *ip;
		if (*ip == 0xfe) {
			ip++;
			i = *ip + 256;
		}
		opcode = &mono_opcodes [i];
		switch (opcode->argument) {
		case MonoInlineNone:
			++ip;
			break;
		case MonoInlineString:
			mono_ldstr (domain, image, mono_metadata_token_index (read32 (ip + 1)));
			ip += 5;
			break;
		case MonoInlineType:
			if (method->wrapper_type == MONO_WRAPPER_NONE) {
				class = mono_class_get (image, read32 (ip + 1));
				mono_class_init (class);
				if (!(class->flags & TYPE_ATTRIBUTE_INTERFACE))
					mono_class_vtable (domain, class);
			}
			ip += 5;
			break;
		case MonoInlineField:
			n_fields++;
			ip += 5;
			break;
		case MonoInlineMethod:
			if (method->wrapper_type == MONO_WRAPPER_NONE) {
				m = mono_get_method (image, read32 (ip + 1), NULL);
				mono_class_init (m->klass);
				if (!(m->klass->flags & TYPE_ATTRIBUTE_INTERFACE))
					mono_class_vtable (domain, m->klass);
			}
			ip += 5;
			break;
		case MonoInlineTok:
		case MonoInlineSig:
		case MonoShortInlineR:
		case MonoInlineI:
		case MonoInlineBrTarget:
			ip += 5;
			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
		case MonoShortInlineBrTarget:
			ip += 2;
			break;
		case MonoInlineSwitch: {
			guint32 n;
			++ip;
			n = read32 (ip);
			ip += 4;
			ip += 4 * n;
			break;
		}
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		default:
			g_assert_not_reached ();
		}

	}

	/* the rest needs to be locked so it is only done once */
	EnterCriticalSection(&calc_section);
	if (method->info != NULL) {
		LeaveCriticalSection(&calc_section);
		mono_profiler_method_end_jit (method, MONO_PROFILE_OK);
		return;
	}
	rtd = (MethodRuntimeData *)g_malloc0 (sizeof(MethodRuntimeData) + (header->num_locals - 1 + signature->hasthis + signature->param_count) * sizeof(guint32));
	for (i = 0; i < header->num_locals; ++i) {
		size = mono_type_size (header->locals [i], &align);
		offset += align - 1;
		offset &= ~(align - 1);
		rtd->offsets [i] = offset;
		offset += size;
	}
	rtd->locals_size = offset;
	offset = 0;
	if (signature->hasthis) {
		offset += sizeof (gpointer) - 1;
		offset &= ~(sizeof (gpointer) - 1);
		rtd->offsets [header->num_locals] = offset;
		offset += sizeof (gpointer);
	}
	for (i = 0; i < signature->param_count; ++i) {
		if (signature->pinvoke) {
			size = mono_type_native_stack_size (signature->params [i], &align);
			align = 8;
		}
		else
			size = mono_type_stack_size (signature->params [i], &align);
		offset += align - 1;
		offset &= ~(align - 1);
		rtd->offsets [signature->hasthis + header->num_locals + i] = offset;
		offset += size;
	}
	rtd->args_size = offset;
	rtd->field_info = g_malloc(n_fields * sizeof(MonoRuntimeFieldInfo));

	header->code = g_memdup(header->code, header->code_size);
	n_fields = 0;
	ip = header->code;
	end = ip + header->code_size;
	while (ip < end) {
		i = *ip;
		if (*ip == 0xfe) {
			ip++;
			i = *ip + 256;
		}
		opcode = &mono_opcodes [i];
		switch (opcode->argument) {
		case MonoInlineNone:
			++ip;
			break;
		case MonoInlineString:
			ip += 5;
			break;
		case MonoInlineType:
			ip += 5;
			break;
		case MonoInlineField:
			token = read32 (ip + 1);
			rtd->field_info[n_fields].field = mono_field_from_token (image, token, &class);
			mono_class_vtable (domain, class);
			g_assert(rtd->field_info[n_fields].field->parent == class);
			write32 ((unsigned char *)ip + 1, n_fields);
			n_fields++;
			ip += 5;
			break;
		case MonoInlineMethod:
			ip += 5;
			break;
		case MonoInlineTok:
		case MonoInlineSig:
		case MonoShortInlineR:
		case MonoInlineI:
		case MonoInlineBrTarget:
			ip += 5;
			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
		case MonoShortInlineBrTarget:
			ip += 2;
			break;
		case MonoInlineSwitch: {
			guint32 n;
			++ip;
			n = read32 (ip);
			ip += 4;
			ip += 4 * n;
			break;
		}
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		default:
			g_assert_not_reached ();
		}

	}

	/*
	 * We store the inline info in addr, since it's unused for IL methods.
	 */
	if (method->klass == mono_defaults.string_class) {
		if (strcmp (method->name, "get_Length") == 0)
			method->addr = GUINT_TO_POINTER (INLINE_STRING_LENGTH);
		else if (strcmp (method->name, "get_Chars") == 0)
			method->addr = GUINT_TO_POINTER (INLINE_STRING_GET_CHARS);
	} else if (method->klass == mono_defaults.array_class) {
		if (strcmp (method->name, "get_Length") == 0)
			method->addr = GUINT_TO_POINTER (INLINE_ARRAY_LENGTH);
		else if (strcmp (method->name, "get_Rank") == 0 || strcmp (method->name, "GetRank") == 0)
			method->addr = GUINT_TO_POINTER (INLINE_ARRAY_RANK);
	} else if (method->klass == mono_defaults.monotype_class) {
		if (strcmp (method->name, "GetElementType") == 0)
			method->addr = GUINT_TO_POINTER (INLINE_TYPE_ELEMENT_TYPE);
	}
	mono_profiler_method_end_jit (method, MONO_PROFILE_OK);
	method->info = rtd;
	LeaveCriticalSection(&calc_section);
}

#define LOCAL_POS(n)            (frame->locals + rtd->offsets [n])
#define LOCAL_TYPE(header, n)   ((header)->locals [(n)])

#define ARG_POS(n)              (args_pointers [(n)])
#define ARG_TYPE(sig, n)        ((n) ? (sig)->params [(n) - (sig)->hasthis] : \
				(sig)->hasthis ? &frame->method->klass->this_arg: (sig)->params [(0)])

typedef struct _vtallocation vtallocation;

struct _vtallocation {
	vtallocation *next;
	guint32 size;
	guint32 max_size;
	union {
		char data [MONO_ZERO_LEN_ARRAY];
		double force_alignment;
	} u;
};

#define vt_allocmem(sz, var) \
	do { \
		vtallocation *tmp, *prev; \
		prev = NULL; \
		tmp = vtalloc; \
		while (tmp && (tmp->max_size < (sz))) { \
			prev = tmp; \
			tmp = tmp->next; \
		} \
		if (!tmp) { \
			tmp = alloca (sizeof (vtallocation) + (sz));	\
			tmp->max_size = (sz);	\
			g_assert ((sz) < 10000);	\
		} \
		else \
			if (prev) \
				prev->next = tmp->next; \
			else \
				vtalloc = tmp->next; \
		tmp->size = (sz);	\
		var = tmp->u.data;	\
	} while(0)

#define vt_alloc(vtype,sp,native)	\
	if ((vtype)->type == MONO_TYPE_VALUETYPE && !(vtype)->data.klass->enumtype) {	\
		if (!(vtype)->byref) {	\
			guint32 vtsize; \
			if (native) vtsize = mono_class_native_size ((vtype)->data.klass, NULL);	\
			else vtsize = mono_class_value_size ((vtype)->data.klass, NULL);	\
			vt_allocmem(vtsize, (sp)->data.vt);	\
		}	\
	}

#define vt_free(sp)	\
	do {	\
		if ((sp)->type == VAL_VALUET) {	\
			vtallocation *tmp = (vtallocation*)(((char*)(sp)->data.vt) - G_STRUCT_OFFSET (vtallocation, u));	\
			tmp->next = vtalloc; \
			vtalloc = tmp; \
		}	\
	} while (0)

#define stackvalpush(val, sp) \
	do {	\
		(sp)->type = (val)->type; \
		(sp)->data = (val)->data; \
		if ((val)->type == VAL_VALUET) {	\
			vtallocation *vala = (vtallocation*)(((char*)(val)->data.vt) - G_STRUCT_OFFSET (vtallocation, u));	\
			vt_allocmem(vala->size, (sp)->data.vt);	\
			memcpy((sp)->data.vt, (val)->data.vt, vala->size);	\
		}	\
		(sp)++; \
	} while (0)

#define stackvalcpy(src, dest) \
	do {	\
		(dest)->type = (src)->type; \
		(dest)->data = (src)->data; \
		if ((dest)->type == VAL_VALUET) {	\
			vtallocation *tmp = (vtallocation*)(((char*)(src)->data.vt) - G_STRUCT_OFFSET (vtallocation, u));	\
			memcpy((dest)->data.vt, (src)->data.vt, tmp->size);	\
		}	\
	} while (0)

/*
static void
verify_method (MonoMethod *m)
{
	GSList *errors, *tmp;
	MonoVerifyInfo *info;

	errors = mono_method_verify (m, MONO_VERIFY_ALL);
	if (errors)
		g_print ("Method %s.%s::%s has invalid IL.\n", m->klass->name_space, m->klass->name, m->name);
	for (tmp = errors; tmp; tmp = tmp->next) {
		info = tmp->data;
		g_print ("%s\n", info->message);
	}
	if (errors)
		G_BREAKPOINT ();
	mono_free_verify_list (errors);
}
*/

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
	ThreadContext *context = TlsGetValue (thread_context_id);
	MonoObject *retval = NULL;
	MonoMethodSignature *sig = method->signature;
	MonoClass *klass = mono_class_from_mono_type (sig->ret);
	int i, type, isobject = 0;
	void *ret;
	stackval result;
	stackval *args = alloca (sizeof (stackval) * sig->param_count);
	ThreadContext context_struct;
	MonoInvocation *old_frame;

	if (context == NULL) {
		context = &context_struct;
		context_struct.base_frame = &frame;
		context_struct.current_frame = NULL;
		context_struct.env_frame = NULL;
		context_struct.current_env = NULL;
		context_struct.search_for_handler = 0;
		TlsSetValue (thread_context_id, context);
	}
	else
		old_frame = context->current_frame;


	/* FIXME: Set frame for execption handling.  */

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
		retval = mono_object_new (mono_domain_get (), klass);
		ret = ((char*)retval) + sizeof (MonoObject);
		if (!sig->ret->data.klass->enumtype)
			result.data.vt = ret;
		break;
	default:
		retval = mono_object_new (mono_domain_get (), klass);
		ret = ((char*)retval) + sizeof (MonoObject);
		break;
	}

	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			args [i].type = VAL_POINTER;
			args [i].data.p = params [i];
			continue;
		}
		type = sig->params [i]->type;
handle_enum:
		switch (type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
			args [i].type = VAL_I32;
			args [i].data.i = *(MonoBoolean*)params [i];
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
			args [i].type = VAL_I32;
			args [i].data.i = *(gint16*)params [i];
			break;
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_U: /* use VAL_POINTER? */
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
			args [i].type = VAL_I32;
			args [i].data.i = *(gint32*)params [i];
			break;
#if SIZEOF_VOID_P == 8
		case MONO_TYPE_U:
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			args [i].type = VAL_I64;
			args [i].data.l = *(gint64*)params [i];
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				type = sig->params [i]->data.klass->enum_basetype->type;
				goto handle_enum;
			} else {
				args [i].type = VAL_POINTER;
				args [i].data.p = params [i];
			}
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
			args [i].type = VAL_OBJ;
			args [i].data.p = params [i];
			break;
		default:
			g_error ("type 0x%x not handled in  runtime invoke", sig->params [i]->type);
		}
	}

	if (method->klass->valuetype)
		/* Unbox the instance, since valuetype methods expect an interior pointer. */
		obj = mono_object_unbox (obj);

	INIT_FRAME(&frame,context->current_frame,obj,args,&result,method);
	if (exc)
		frame.invoke_trap = 1;
	ves_exec_method_with_context (&frame, context);
	if (context == &context_struct)
		TlsSetValue (thread_context_id, NULL);
	else
		context->current_frame = old_frame;
	if (frame.ex != NULL) {
		if (exc != NULL) {
			*exc = (MonoObject*) frame.ex;
			if (*exc == (MonoObject *)quit_exception)
				mono_print_unhandled_exception(*exc);
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

static MonoMethod *
get_native_wrapper(MonoMethod *method, ThreadContext *context)
{
	jmp_buf env;
	jmp_buf *old_env;
	MonoMethod *wrapper;
	old_env = context->current_env;
	if (setjmp(env) != 0) {
		context->current_env = old_env;
		context->search_for_handler = 1;
		return NULL;
	}
	context->current_env = &env;
	wrapper = mono_marshal_get_native_wrapper (method);
	context->current_env = old_env;
	return wrapper;
}

/*
 * Need to optimize ALU ops when natural int == int32 
 *
 * IDEA: if we maintain a stack of ip, sp to be checked
 * in the return opcode, we could inline simple methods that don't
 * use the stack or local variables....
 * 
 * The {,.S} versions of many opcodes can/should be merged to reduce code
 * duplication.
 * 
 */
static void 
ves_exec_method_with_context (MonoInvocation *frame, ThreadContext *context)
{
	MonoDomain *domain = mono_domain_get (); 	
	MonoInvocation child_frame;
	MonoMethodHeader *header;
	MonoMethodSignature *signature;
	MonoImage *image;
	GSList *finally_ips = NULL;
	const unsigned char *endfinally_ip = NULL;
	register const unsigned char *ip;
	register stackval *sp = NULL;
	MethodRuntimeData *rtd;
	void **args_pointers;
	gint tracing = global_tracing;
	unsigned char tail_recursion = 0;
	unsigned char unaligned_address = 0;
	unsigned char volatile_address = 0;
	vtallocation *vtalloc = NULL;
	MonoVTable *method_class_vt;
	GOTO_LABEL_VARS;

	frame->ex = NULL;
	frame->ip = ip = NULL;
	context->current_frame = frame;

	if (frame->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		frame->method = get_native_wrapper (frame->method, context);
		if (frame->method == NULL)
			goto exit_frame;
	}

	method_class_vt = mono_class_vtable (domain, frame->method->klass);
	if (!method_class_vt->initialized)
		mono_runtime_class_init (method_class_vt);
	signature = frame->method->signature;

	DEBUG_ENTER ();

	header = ((MonoMethodNormal *)frame->method)->header;

	if (frame->method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		if (!frame->method->addr) {
			/* assumes all internal calls with an array this are built in... */
			if (signature->hasthis && frame->method->klass->rank) {
				ves_runtime_method (frame, context);
				if (frame->ex)
					goto handle_exception;
				goto exit_frame;
			}
			frame->method->addr = mono_lookup_internal_call (frame->method);
		}
		ves_pinvoke_method (frame, frame->method->signature, frame->method->addr, 
			    frame->method->string_ctor, context);
		if (frame->ex)
			goto handle_exception;
		goto exit_frame;
	} 
	else if (frame->method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
		ves_runtime_method (frame, context);
		if (frame->ex)
			goto handle_exception;
		goto exit_frame;
	}

	/*verify_method (frame->method);*/

	image = frame->method->klass->image;

	if (!frame->method->info)
		calc_offsets (image, frame->method);
	rtd = frame->method->info;

	/*
	 * with alloca we get the expected huge performance gain
	 * stackval *stack = g_new0(stackval, header->max_stack);
	 */
	g_assert (header->max_stack < 10000);
	sp = frame->stack = alloca (sizeof (stackval) * header->max_stack);

	if (header->num_locals) {
		g_assert (rtd->locals_size < 65536);
		frame->locals = alloca (rtd->locals_size);
		/* 
		 * yes, we do it unconditionally, because it needs to be done for
		 * some cases anyway and checking for that would be even slower.
		 */
		memset (frame->locals, 0, rtd->locals_size);
	}

	/*
	 * Copy args from stack_args to args.
	 */
	if (signature->param_count || signature->hasthis) {
		int i;
		int has_this = signature->hasthis;

		g_assert (rtd->args_size < 10000);
		frame->args = alloca (rtd->args_size);
		g_assert ((signature->param_count + has_this) < 1000);
		args_pointers = alloca (sizeof(void*) * (signature->param_count + has_this));
		if (has_this) {
			gpointer *this_arg;
			this_arg = args_pointers [0] = frame->args;
			*this_arg = frame->obj;
		}
		for (i = 0; i < signature->param_count; ++i) {
			args_pointers [i + has_this] = frame->args + rtd->offsets [header->num_locals + has_this + i];
			stackval_to_data (signature->params [i], frame->stack_args + i, args_pointers [i + has_this], signature->pinvoke);
		}
	}

	child_frame.parent = frame;
	frame->child = &child_frame;
	frame->ex = NULL;

	/* ready to go */
	ip = header->code;

	/*
	 * using while (ip < end) may result in a 15% performance drop, 
	 * but it may be useful for debug
	 */
	while (1) {
	main_loop:
		/*g_assert (sp >= stack);*/
#if DEBUG_INTERP
		opcode_count++;
		if (tracing > 1) {
			char *ins, *discode;
			if (sp > frame->stack) {
				ins = dump_stack (frame->stack, sp);
			} else {
				ins = g_strdup ("");
			}
			output_indent ();
			discode = mono_disasm_code_one (NULL, frame->method, ip, NULL);
			discode [strlen (discode) - 1] = 0; /* no \n */
			g_print ("(%u) %-29s %s\n", GetCurrentThreadId(), discode, ins);
			g_free (ins);
			g_free (discode);
		}
#endif
		
		SWITCH (*ip) {
		CASE (CEE_NOP) 
			++ip;
			BREAK;
		CASE (CEE_BREAK)
			++ip;
			G_BREAKPOINT (); /* this is not portable... */
			BREAK;
		CASE (CEE_LDARG_0)
		CASE (CEE_LDARG_1)
		CASE (CEE_LDARG_2)
		CASE (CEE_LDARG_3) {
			int n = (*ip)-CEE_LDARG_0;
			++ip;
			vt_alloc (ARG_TYPE (signature, n), sp, signature->pinvoke);
			stackval_from_data (ARG_TYPE (signature, n), sp, ARG_POS (n), signature->pinvoke);
			++sp;
			BREAK;
		}
		CASE (CEE_LDLOC_0)
		CASE (CEE_LDLOC_1)
		CASE (CEE_LDLOC_2)
		CASE (CEE_LDLOC_3) {
			int n = (*ip)-CEE_LDLOC_0;
			MonoType *vartype = LOCAL_TYPE (header, n);
			++ip;
			if (vartype->type == MONO_TYPE_I4) {
				sp->type = VAL_I32;
				sp->data.i = *(gint32*) LOCAL_POS (n);
				++sp;
				BREAK;
			} else {
				vt_alloc (vartype, sp, FALSE);
				stackval_from_data (vartype, sp, LOCAL_POS (n), FALSE);
			}
			++sp;
			BREAK;
		}
		CASE (CEE_STLOC_0)
		CASE (CEE_STLOC_1)
		CASE (CEE_STLOC_2)
		CASE (CEE_STLOC_3) {
			int n = (*ip)-CEE_STLOC_0;
			++ip;
			--sp;
			if ((LOCAL_TYPE (header, n))->type == MONO_TYPE_I4) {
				gint32 *p = (gint32*)LOCAL_POS (n);
				*p = sp->data.i;
				BREAK;
			} else {
				stackval_to_data (LOCAL_TYPE (header, n), sp, LOCAL_POS (n), FALSE);
				vt_free (sp);
				BREAK;
			}
		}
		CASE (CEE_LDARG_S)
			++ip;
			vt_alloc (ARG_TYPE (signature, *ip), sp, signature->pinvoke);
			stackval_from_data (ARG_TYPE (signature, *ip), sp, ARG_POS (*ip), signature->pinvoke);
			++sp;
			++ip;
			BREAK;
		CASE (CEE_LDARGA_S) {
			++ip;
			sp->data.vt = ARG_POS (*ip);
			sp->type = VAL_MP;
			++sp;
			++ip;
			BREAK;
		}
		CASE (CEE_STARG_S)
			++ip;
			--sp;
			stackval_to_data (ARG_TYPE (signature, *ip), sp, ARG_POS (*ip), signature->pinvoke);
			vt_free (sp);
			++ip;
			BREAK;
		CASE (CEE_LDLOC_S)
			++ip;
			vt_alloc (LOCAL_TYPE (header, *ip), sp, FALSE);
			stackval_from_data (LOCAL_TYPE (header, *ip), sp, LOCAL_POS (*ip), FALSE);
			++ip;
			++sp;
			BREAK;
		CASE (CEE_LDLOCA_S) {
			++ip;
			sp->data.p = LOCAL_POS (*ip);
			sp->type = VAL_MP;
			++sp;
			++ip;
			BREAK;
		}
		CASE (CEE_STLOC_S)
			++ip;
			--sp;
			stackval_to_data (LOCAL_TYPE (header, *ip), sp, LOCAL_POS (*ip), FALSE);
			vt_free (sp);
			++ip;
			BREAK;
		CASE (CEE_LDNULL) 
			++ip;
			sp->type = VAL_OBJ;
			sp->data.p = NULL;
			++sp;
			BREAK;
		CASE (CEE_LDC_I4_M1)
			++ip;
			sp->type = VAL_I32;
			sp->data.i = -1;
			++sp;
			BREAK;
		CASE (CEE_LDC_I4_0)
		CASE (CEE_LDC_I4_1)
		CASE (CEE_LDC_I4_2)
		CASE (CEE_LDC_I4_3)
		CASE (CEE_LDC_I4_4)
		CASE (CEE_LDC_I4_5)
		CASE (CEE_LDC_I4_6)
		CASE (CEE_LDC_I4_7)
		CASE (CEE_LDC_I4_8)
			sp->type = VAL_I32;
			sp->data.i = (*ip) - CEE_LDC_I4_0;
			++sp;
			++ip;
			BREAK;
		CASE (CEE_LDC_I4_S) 
			++ip;
			sp->type = VAL_I32;
			sp->data.i = *(const gint8 *)ip;
			++ip;
			++sp;
			BREAK;
		CASE (CEE_LDC_I4)
			++ip;
			sp->type = VAL_I32;
			sp->data.i = read32 (ip);
			ip += 4;
			++sp;
			BREAK;
		CASE (CEE_LDC_I8)
			++ip;
			sp->type = VAL_I64;
			sp->data.l = read64 (ip);
			ip += 8;
			++sp;
			BREAK;
		CASE (CEE_LDC_R4) {
			float val;
			++ip;
			sp->type = VAL_DOUBLE;
			readr4 (ip, &val);
			sp->data.f = val;
			ip += 4;
			++sp;
			BREAK;
		}
		CASE (CEE_LDC_R8) 
			++ip;
			sp->type = VAL_DOUBLE;
			readr8(ip, &sp->data.f);
			ip += 8;
			++sp;
			BREAK;
		CASE (CEE_UNUSED99) ves_abort (); BREAK;
		CASE (CEE_DUP) 
			stackvalpush(sp - 1, sp);
			++ip; 
			BREAK;
		CASE (CEE_POP)
			++ip;
			--sp;
			vt_free (sp);
			BREAK;
		CASE (CEE_JMP) ves_abort(); BREAK;
		CASE (CEE_CALLVIRT) /* Fall through */
		CASE (CEE_CALLI)    /* Fall through */
		CASE (CEE_CALL) {
			MonoMethodSignature *csignature;
			stackval retval;
			stackval *endsp = sp;
			guint32 token;
			int virtual = *ip == CEE_CALLVIRT;
			int calli = *ip == CEE_CALLI;
			unsigned char *code = NULL;

			/*
			 * We ignore tail recursion for now.
			 */
			tail_recursion = 0;

			frame->ip = ip;
			
			++ip;
			token = read32 (ip);
			ip += 4;
			if (calli) {
				MonoJitInfo *ji;
				--sp;
				code = sp->data.p;
				if (frame->method->wrapper_type == MONO_WRAPPER_DELEGATE_INVOKE &&
					(ji = mono_jit_info_table_find (mono_root_domain, code)) != NULL) {
					child_frame.method = ji->method;
					csignature = ji->method->signature;
				}
				else if (frame->method->wrapper_type != MONO_WRAPPER_NONE) {
					csignature = (MonoMethodSignature *)mono_method_get_wrapper_data (frame->method, token);
					child_frame.method = NULL;
				} else {
					g_assert_not_reached ();
				}
				g_assert (code);
			} else {
				if (frame->method->wrapper_type != MONO_WRAPPER_NONE) 
					child_frame.method = (MonoMethod *)mono_method_get_wrapper_data (frame->method, token);
				else
					child_frame.method = mono_get_method (image, token, NULL);
				if (!child_frame.method)
					THROW_EX (mono_get_exception_missing_method (), ip -5);
				csignature = child_frame.method->signature;
				if (virtual) {
					stackval *this_arg = &sp [-csignature->param_count-1];
					if (!this_arg->data.p)
						THROW_EX (mono_get_exception_null_reference(), ip - 5);
					child_frame.method = get_virtual_method (domain, child_frame.method, this_arg);
					if (!child_frame.method)
						THROW_EX (mono_get_exception_missing_method (), ip -5);
				}
			}

			if (frame->method->wrapper_type == MONO_WRAPPER_NONE)
				if (child_frame.method && child_frame.method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
					child_frame.method = mono_marshal_get_synchronized_wrapper (child_frame.method);

			g_assert (csignature->call_convention == MONO_CALL_DEFAULT || csignature->call_convention == MONO_CALL_C);
			/* decrement by the actual number of args */
			if (csignature->param_count) {
				sp -= csignature->param_count;
				child_frame.stack_args = sp;
			} else {
				child_frame.stack_args = NULL;
			}
			if (csignature->hasthis) {
				g_assert (sp >= frame->stack);
				--sp;
				/*
				 * It may also be a TP from LD(S)FLDA
				 * g_assert (sp->type == VAL_OBJ || sp->type == VAL_MP);
				 */
				if (sp->type == VAL_OBJ && child_frame.method &&
				    child_frame.method->klass->valuetype) /* unbox it */
					child_frame.obj = (char*)sp->data.p + sizeof (MonoObject);
				else
					child_frame.obj = sp->data.p;
			} else {
				child_frame.obj = NULL;
			}
			if (csignature->ret->type != MONO_TYPE_VOID) {
				vt_alloc (csignature->ret, &retval, csignature->pinvoke);
				child_frame.retval = &retval;
			} else {
				child_frame.retval = NULL;
			}

			child_frame.ip = NULL;
			child_frame.ex = NULL;
			child_frame.ex_handler = NULL;

			if (!child_frame.method) {
				g_assert (code);
				ves_pinvoke_method (&child_frame, csignature, (MonoFunc) code, FALSE, context);
				if (child_frame.ex) {
					frame->ex = child_frame.ex;
					goto handle_exception;
				}
			} else if (csignature->hasthis && sp->type == VAL_OBJ &&
					((MonoObject *)sp->data.p)->vtable->klass == mono_defaults.transparent_proxy_class) {
				g_assert (child_frame.method);
				child_frame.method = mono_marshal_get_remoting_invoke (child_frame.method);
				ves_exec_method_with_context (&child_frame, context);
			} else {
				switch (GPOINTER_TO_UINT (child_frame.method->addr)) {
				case INLINE_STRING_LENGTH:
					retval.type = VAL_I32;
					retval.data.i = ((MonoString*)sp->data.p)->length;
					/*g_print ("length of '%s' is %d\n", mono_string_to_utf8 (sp->data.p), retval.data.i);*/
					break;
				case INLINE_STRING_GET_CHARS: {
					int idx = GET_NATI(sp [1]);
					if ((idx < 0) || (idx >= mono_string_length ((MonoString*)sp->data.p))) {
						child_frame.ex = mono_get_exception_index_out_of_range ();
						FILL_IN_TRACE(child_frame.ex, &child_frame);
					}
					else {
						retval.type = VAL_I32;
						retval.data.i = mono_string_chars((MonoString*)sp->data.p)[idx];
					}
					break;
				}
				case INLINE_ARRAY_LENGTH:
					retval.type = VAL_I32;
					retval.data.i = mono_array_length ((MonoArray*)sp->data.p);
					break;
				case INLINE_ARRAY_RANK:
					retval.type = VAL_I32;
					retval.data.i = mono_object_class (sp->data.p)->rank;
					break;
				case INLINE_TYPE_ELEMENT_TYPE:
					retval.type = VAL_OBJ;
					{
						MonoClass *c = mono_class_from_mono_type (((MonoReflectionType*)sp->data.p)->type);
						if (c->enumtype && c->enum_basetype) /* types that are modifierd typebuilkders may not have enum_basetype set */
							retval.data.p = mono_type_get_object (domain, c->enum_basetype);
						else if (c->element_class)
							retval.data.p = mono_type_get_object (domain, &c->element_class->byval_arg);
						else
							retval.data.p = NULL;
					}
					break;
				default:
					ves_exec_method_with_context (&child_frame, context);
				}
			}

			context->current_frame = frame;

			while (endsp > sp) {
				--endsp;
				vt_free (endsp);
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
				*sp = retval;
				sp++;
			}
			BREAK;
		}
		CASE (CEE_RET)
			if (signature->ret->type != MONO_TYPE_VOID) {
				--sp;
				if (sp->type == VAL_VALUET) {
					/* the caller has already allocated the memory */
					stackval_from_data (signature->ret, frame->retval, sp->data.vt, signature->pinvoke);
					vt_free (sp);
				} else {
					*frame->retval = *sp;
				}
			}
			if (sp > frame->stack)
				g_warning ("more values on stack: %d", sp-frame->stack);

			goto exit_frame;
		CASE (CEE_BR_S) /* Fall through */
		CASE (CEE_BR)
			if (*ip == CEE_BR) {
				++ip;
				ip += (gint32) read32(ip);
				ip += 4;
			} else {
				++ip;
				ip += (signed char) *ip;
				++ip;
			}
			BREAK;
		CASE (CEE_BRFALSE) /* Fall through */
		CASE (CEE_BRFALSE_S) {
			int result;
			int broffset;
			if (*ip == CEE_BRFALSE_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			--sp;
			switch (sp->type) {
			case VAL_I32: result = sp->data.i == 0; break;
			case VAL_I64: result = sp->data.l == 0; break;
			case VAL_DOUBLE: result = sp->data.f ? 0: 1; break;
			default: result = sp->data.p == NULL; break;
			}
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BRTRUE) /* Fall through */
		CASE (CEE_BRTRUE_S) {
			int result;
			int broffset;
			if (*ip == CEE_BRTRUE_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			--sp;
			switch (sp->type) {
			case VAL_I32: result = sp->data.i != 0; break;
			case VAL_I64: result = sp->data.l != 0; break;
			case VAL_DOUBLE: result = sp->data.f ? 1 : 0; break;
			default: result = sp->data.p != NULL; break;
			}
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BEQ) /* Fall through */
		CASE (CEE_BEQ_S) {
			int result;
			int broffset;
			if (*ip == CEE_BEQ_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (mono_i)sp [0].data.i == (mono_i)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l == sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f == sp [1].data.f;
			else
				result = sp [0].data.nati == (mono_i)GET_NATI (sp [1]);
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BGE) /* Fall through */
		CASE (CEE_BGE_S) {
			int result;
			int broffset;
			if (*ip == CEE_BGE_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (mono_i)sp [0].data.i >= (mono_i)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l >= sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f >= sp [1].data.f;
			else
				result = (mono_i)sp [0].data.nati >= (mono_i)GET_NATI (sp [1]);
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BGT) /* Fall through */
		CASE (CEE_BGT_S) {
			int result;
			int broffset;
			if (*ip == CEE_BGT_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (mono_i)sp [0].data.i > (mono_i)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l > sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f > sp [1].data.f;
			else
				result = (mono_i)sp [0].data.nati > (mono_i)GET_NATI (sp [1]);
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BLT) /* Fall through */
		CASE (CEE_BLT_S) {
			int result;
			int broffset;
			if (*ip == CEE_BLT_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (mono_i)sp[0].data.i < (mono_i)GET_NATI(sp[1]);
			else if (sp->type == VAL_I64)
				result = sp[0].data.l < sp[1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp[0].data.f < sp[1].data.f;
			else
				result = (mono_i)sp[0].data.nati < (mono_i)GET_NATI(sp[1]);
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BLE) /* fall through */
		CASE (CEE_BLE_S) {
			int result;
			int broffset;
			if (*ip == CEE_BLE_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			sp -= 2;

			if (sp->type == VAL_I32)
				result = (mono_i)sp [0].data.i <= (mono_i)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l <= sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f <= sp [1].data.f;
			else {
				result = (mono_i)sp [0].data.nati <= (mono_i)GET_NATI (sp [1]);
			}
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BNE_UN) /* Fall through */
		CASE (CEE_BNE_UN_S) {
			int result;
			int broffset;
			if (*ip == CEE_BNE_UN_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (mono_u)sp [0].data.i != (mono_u)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l != (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = isunordered (sp [0].data.f, sp [1].data.f) ||
					(sp [0].data.f != sp [1].data.f);
			else
				result = (mono_u)sp [0].data.nati != (mono_u)GET_NATI (sp [1]);
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BGE_UN) /* Fall through */
		CASE (CEE_BGE_UN_S) {
			int result;
			int broffset;
			if (*ip == CEE_BGE_UN_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (mono_u)sp [0].data.i >= (mono_u)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l >= (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = !isless (sp [0].data.f,sp [1].data.f);
			else
				result = (mono_u)sp [0].data.nati >= (mono_u)GET_NATI (sp [1]);
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BGT_UN) /* Fall through */
		CASE (CEE_BGT_UN_S) {
			int result;
			int broffset;
			if (*ip == CEE_BGT_UN_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (mono_u)sp [0].data.i > (mono_u)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l > (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = isgreater (sp [0].data.f, sp [1].data.f);
			else
				result = (mono_u)sp [0].data.nati > (mono_u)GET_NATI (sp [1]);
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BLE_UN) /* Fall through */
		CASE (CEE_BLE_UN_S) {
			int result;
			int broffset;
			if (*ip == CEE_BLE_UN_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (mono_u)sp [0].data.i <= (mono_u)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l <= (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = islessequal (sp [0].data.f, sp [1].data.f);
			else
				result = (mono_u)sp [0].data.nati <= (mono_u)GET_NATI (sp [1]);
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_BLT_UN) /* Fall through */
		CASE (CEE_BLT_UN_S) {
			int result;
			int broffset;
			if (*ip == CEE_BLT_UN_S) {
				broffset = (signed char)ip [1];
				ip += 2;
			} else {
				broffset = (gint32) read32 (ip + 1);
				ip += 5;
			}
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (mono_u)sp[0].data.i < (mono_u)GET_NATI(sp[1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp[0].data.l < (guint64)sp[1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = isunordered (sp [0].data.f, sp [1].data.f) ||
					(sp [0].data.f < sp [1].data.f);
			else
				result = (mono_u)sp[0].data.nati < (mono_u)GET_NATI(sp[1]);
			if (result)
				ip += broffset;
			BREAK;
		}
		CASE (CEE_SWITCH) {
			guint32 n;
			const unsigned char *st;
			++ip;
			n = read32 (ip);
			ip += 4;
			st = ip + sizeof (gint32) * n;
			--sp;
			if ((guint32)sp->data.i < n) {
				gint offset;
				ip += sizeof (gint32) * (guint32)sp->data.i;
				offset = read32 (ip);
				ip = st + offset;
			} else {
				ip = st;
			}
			BREAK;
		}
		CASE (CEE_LDIND_I1)
			++ip;
			sp[-1].type = VAL_I32;
			sp[-1].data.i = *(gint8*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_U1)
			++ip;
			sp[-1].type = VAL_I32;
			sp[-1].data.i = *(guint8*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_I2)
			++ip;
			sp[-1].type = VAL_I32;
			sp[-1].data.i = *(gint16*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_U2)
			++ip;
			sp[-1].type = VAL_I32;
			sp[-1].data.i = *(guint16*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_I4) /* Fall through */
		CASE (CEE_LDIND_U4)
			++ip;
			sp[-1].type = VAL_I32;
			sp[-1].data.i = *(gint32*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_I8)
			++ip;
			sp[-1].type = VAL_I64;
			sp[-1].data.l = *(gint64*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_I)
			++ip;
			sp[-1].type = VAL_NATI;
			sp[-1].data.p = *(gpointer*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_R4)
			++ip;
			sp[-1].type = VAL_DOUBLE;
			sp[-1].data.f = *(gfloat*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_R8)
			++ip;
			sp[-1].type = VAL_DOUBLE;
			sp[-1].data.f = *(gdouble*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_REF)
			++ip;
			sp[-1].type = VAL_OBJ;
			sp[-1].data.p = *(gpointer*)sp[-1].data.p;
			BREAK;
		CASE (CEE_STIND_REF) {
			gpointer *p;
			++ip;
			sp -= 2;
			p = sp->data.p;
			*p = sp[1].data.p;
			BREAK;
		}
		CASE (CEE_STIND_I1) {
			gint8 *p;
			++ip;
			sp -= 2;
			p = sp->data.p;
			*p = (gint8)sp[1].data.i;
			BREAK;
		}
		CASE (CEE_STIND_I2) {
			gint16 *p;
			++ip;
			sp -= 2;
			p = sp->data.p;
			*p = (gint16)sp[1].data.i;
			BREAK;
		}
		CASE (CEE_STIND_I4) {
			gint32 *p;
			++ip;
			sp -= 2;
			p = sp->data.p;
			*p = sp[1].data.i;
			BREAK;
		}
		CASE (CEE_STIND_I) {
			mono_i *p;
			++ip;
			sp -= 2;
			p = sp->data.p;
			*p = (mono_i)sp[1].data.p;
			BREAK;
		}
		CASE (CEE_STIND_I8) {
			gint64 *p;
			++ip;
			sp -= 2;
			p = sp->data.p;
			*p = sp[1].data.l;
			BREAK;
		}
		CASE (CEE_STIND_R4) {
			float *p;
			++ip;
			sp -= 2;
			p = sp->data.p;
			*p = (gfloat)sp[1].data.f;
			BREAK;
		}
		CASE (CEE_STIND_R8) {
			double *p;
			++ip;
			sp -= 2;
			p = sp->data.p;
			*p = sp[1].data.f;
			BREAK;
		}
		CASE (CEE_ADD)
			++ip;
			--sp;
			/* should probably consider the pointers as unsigned */
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_I32)
					sp [-1].data.i += sp [0].data.i;
				else
					sp [-1].data.nati = sp [-1].data.nati + sp [0].data.i;
			} else if (sp->type == VAL_I64)
				sp [-1].data.l += sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f += sp [0].data.f;
			else {
				if (sp [-1].type == VAL_I32) {
					sp [-1].data.nati = sp [-1].data.i + sp [0].data.nati;
					sp [-1].type = sp [0].type;
				} else
					sp [-1].data.nati = sp [-1].data.nati + sp [0].data.nati;
			}
			BREAK;
		CASE (CEE_SUB)
			++ip;
			--sp;
			/* should probably consider the pointers as unsigned */
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_I32)
					sp [-1].data.i -= sp [0].data.i;
				else {
					sp [-1].data.nati = sp [-1].data.nati - sp [0].data.i;
					sp [-1].type = VAL_NATI;
				}
			} else if (sp->type == VAL_I64)
				sp [-1].data.l -= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f -= sp [0].data.f;
			else {
				if (sp [-1].type == VAL_I32) {
					sp [-1].data.nati = sp [-1].data.i - sp [0].data.nati;
					sp [-1].type = sp [0].type;
				} else
					sp [-1].data.nati = sp [-1].data.nati - sp [0].data.nati;
			}
			BREAK;
		CASE (CEE_MUL)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_I32)
					sp [-1].data.i *= sp [0].data.i;
				else
					sp [-1].data.nati = sp [-1].data.nati * sp [0].data.i;
			} else if (sp->type == VAL_I64)
				sp [-1].data.l *= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f *= sp [0].data.f;
			else {
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = sp [-1].data.nati * sp [0].data.nati;
				else {
					sp [-1].data.nati = sp [-1].data.i * sp [0].data.nati;
					sp [-1].type = VAL_NATI;
				}
			}
			BREAK;
		CASE (CEE_DIV)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (sp [0].data.i == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				if (sp [-1].type == VAL_I32)
					sp [-1].data.i /= sp [0].data.i;
				else
					sp [-1].data.nati /= sp [0].data.i;
			} else if (sp->type == VAL_I64) {
				if (sp [0].data.l == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				sp [-1].data.l /= sp [0].data.l;
			} else if (sp->type == VAL_DOUBLE) {
				/* set NaN is divisor is 0.0 */
				sp [-1].data.f /= sp [0].data.f;
			} else {
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = sp [-1].data.nati / sp [0].data.nati;
				else {
					sp [-1].data.nati = sp [-1].data.i / sp [0].data.nati;
					sp [-1].type = VAL_NATI;
				}
			}
			BREAK;
		CASE (CEE_DIV_UN)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (sp [0].data.i == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = (mono_u)sp [-1].data.nati / (mono_u)sp [0].data.i;
				else
					sp [-1].data.i = (guint32)sp [-1].data.i / (guint32)sp [0].data.i;
			} else if (sp->type == VAL_I64) {
				guint64 val;
				if (sp [0].data.l == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				val = sp [-1].data.l;
				val /= (guint64)sp [0].data.l;
				sp [-1].data.l = val;
			} else {
				if (sp [0].data.nati == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = (mono_u)sp [-1].data.nati / (mono_u)sp [0].data.nati;
				else {
					sp [-1].data.nati = (mono_u)sp [-1].data.i / (mono_u)sp [0].data.nati;
					sp [-1].type = VAL_NATI;
				}
			}
			BREAK;
		CASE (CEE_REM)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (sp [0].data.i == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = (mono_i)sp [-1].data.nati % (mono_i)sp [0].data.i;
				else
					sp [-1].data.i = sp [-1].data.i % sp [0].data.i;
			} else if (sp->type == VAL_I64) {
				if (sp [0].data.l == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				sp [-1].data.l %= sp [0].data.l;
			} else if (sp->type == VAL_DOUBLE) {
				/* FIXME: what do we actually do here? */
				sp [-1].data.f = fmod (sp [-1].data.f, sp [0].data.f);
			} else {
				if (sp [0].data.nati == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = (mono_i)sp [-1].data.nati % (mono_i)sp [0].data.nati;
				else {
					sp [-1].data.nati = (mono_i)sp [-1].data.i % (mono_i)sp [0].data.nati;
					sp [-1].type = VAL_NATI;
				}
			}
			BREAK;
		CASE (CEE_REM_UN)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (sp [0].data.i == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = (mono_u)sp [-1].data.nati % (mono_u)sp [0].data.i;
				else
					sp [-1].data.i = (guint32)sp [-1].data.i % (guint32)sp [0].data.i;
			} else if (sp->type == VAL_I64) {
				if (sp [0].data.l == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				sp [-1].data.l = (guint64)sp [-1].data.l % (guint64)sp [0].data.l;
			} else if (sp->type == VAL_DOUBLE) {
				/* unspecified behaviour according to the spec */
			} else {
				if (sp [0].data.nati == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = (mono_u)sp [-1].data.nati % (mono_u)sp [0].data.nati;
				else {
					sp [-1].data.nati = (mono_u)sp [-1].data.i % (mono_u)sp [0].data.nati;
					sp [-1].type = VAL_NATI;
				}
			}
			BREAK;
		CASE (CEE_AND)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati &= sp [0].data.i;
				else
					sp [-1].data.i &= sp [0].data.i;
			}
			else if (sp->type == VAL_I64)
				sp [-1].data.l &= sp [0].data.l;
			else {
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = (mono_i)sp [-1].data.nati & (mono_i)sp [0].data.nati;
				else {
					sp [-1].data.nati = (mono_i)sp [-1].data.i & (mono_i)sp [0].data.nati;
					sp [-1].type = VAL_NATI;
				}
			}
			BREAK;
		CASE (CEE_OR)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati |= sp [0].data.i;
				else
					sp [-1].data.i |= sp [0].data.i;
			}
			else if (sp->type == VAL_I64)
				sp [-1].data.l |= sp [0].data.l;
			else {
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = (mono_i)sp [-1].data.nati | (mono_i)sp [0].data.nati;
				else {
					sp [-1].data.nati = (mono_i)sp [-1].data.i | (mono_i)sp [0].data.nati;
					sp [-1].type = VAL_NATI;
				}
			}
			BREAK;
		CASE (CEE_XOR)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati ^= sp [0].data.i;
				else
					sp [-1].data.i ^= sp [0].data.i;
			}
			else if (sp->type == VAL_I64)
				sp [-1].data.l ^= sp [0].data.l;
			else {
				if (sp [-1].type == VAL_NATI)
					sp [-1].data.nati = (mono_i)sp [-1].data.nati ^ (mono_i)sp [0].data.nati;
				else {
					sp [-1].data.nati = (mono_i)sp [-1].data.i ^ (mono_i)sp [0].data.nati;
					sp [-1].type = VAL_NATI;
				}
			}
			BREAK;
		CASE (CEE_SHL)
			++ip;
			--sp;
			if (sp [-1].type == VAL_I32)
				sp [-1].data.i <<= GET_NATI (sp [0]);
			else if (sp [-1].type == VAL_I64)
				sp [-1].data.l <<= GET_NATI (sp [0]);
			else
				sp [-1].data.nati <<= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHR)
			++ip;
			--sp;
			if (sp [-1].type == VAL_I32)
				sp [-1].data.i >>= GET_NATI (sp [0]);
			else if (sp [-1].type == VAL_I64)
				sp [-1].data.l >>= GET_NATI (sp [0]);
			else
				sp [-1].data.nati = ((mono_i)sp [-1].data.nati) >> GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHR_UN)
			++ip;
			--sp;
			if (sp [-1].type == VAL_I32)
				sp [-1].data.i = (guint)sp [-1].data.i >> GET_NATI (sp [0]);
			else if (sp [-1].type == VAL_I64)
				sp [-1].data.l = (guint64)sp [-1].data.l >> GET_NATI (sp [0]);
			else
				sp [-1].data.nati = ((mono_u)sp[-1].data.nati) >> GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_NEG)
			++ip;
			--sp;
			if (sp->type == VAL_I32) 
				sp->data.i = - sp->data.i;
			else if (sp->type == VAL_I64)
				sp->data.l = - sp->data.l;
			else if (sp->type == VAL_DOUBLE)
				sp->data.f = - sp->data.f;
			else if (sp->type == VAL_NATI)
				sp->data.nati = - (mono_i)sp->data.nati;
			++sp;
			BREAK;
		CASE (CEE_NOT)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp->data.i = ~ sp->data.i;
			else if (sp->type == VAL_I64)
				sp->data.l = ~ sp->data.l;
			else if (sp->type == VAL_NATI)
				sp->data.nati = ~ (mono_i)sp->data.p;
			++sp;
			BREAK;
		CASE (CEE_CONV_U1) {
			++ip;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				sp [-1].data.i = (guint8)sp [-1].data.f;
				break;
			case VAL_I64:
				sp [-1].data.i = (guint8)sp [-1].data.l;
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				sp [-1].data.i = (guint8)sp [-1].data.i;
				break;
			default:
				sp [-1].data.i = (guint8)sp [-1].data.nati;
				break;
			}
			sp [-1].type = VAL_I32;
			BREAK;
		}
		CASE (CEE_CONV_I1) {
			++ip;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				sp [-1].data.i = (gint8)sp [-1].data.f;
				break;
			case VAL_I64:
				sp [-1].data.i = (gint8)sp [-1].data.l;
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				sp [-1].data.i = (gint8)sp [-1].data.i;
				break;
			default:
				sp [-1].data.i = (gint8)sp [-1].data.nati;
				break;
			}
			sp [-1].type = VAL_I32;
			BREAK;
		}
		CASE (CEE_CONV_U2) {
			++ip;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				sp [-1].data.i = (guint16)sp [-1].data.f;
				break;
			case VAL_I64:
				sp [-1].data.i = (guint16)sp [-1].data.l;
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				sp [-1].data.i = (guint16)sp [-1].data.i;
				break;
			default:
				sp [-1].data.i = (guint16)sp [-1].data.nati;
				break;
			}
			sp [-1].type = VAL_I32;
			BREAK;
		}
		CASE (CEE_CONV_I2) {
			++ip;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				sp [-1].data.i = (gint16)sp [-1].data.f;
				break;
			case VAL_I64:
				sp [-1].data.i = (gint16)sp [-1].data.l;
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				sp [-1].data.i = (gint16)sp [-1].data.i;
				break;
			default:
				sp [-1].data.i = (gint16)sp [-1].data.nati;
				break;
			}
			sp [-1].type = VAL_I32;
			BREAK;
		}
		CASE (CEE_CONV_U4) /* Fall through */
#if SIZEOF_VOID_P == 4
		CASE (CEE_CONV_I) /* Fall through */
		CASE (CEE_CONV_U) /* Fall through */
#endif
		CASE (CEE_CONV_I4) {
			++ip;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				sp [-1].data.i = (gint32)sp [-1].data.f;
				break;
			case VAL_I64:
				sp [-1].data.i = (gint32)sp [-1].data.l;
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				break;
			default:
				sp [-1].data.i = (gint32)sp [-1].data.p;
				break;
			}
			sp [-1].type = VAL_I32;
			BREAK;
		}
#if SIZEOF_VOID_P == 8
		CASE (CEE_CONV_I) /* Fall through */
#endif
		CASE (CEE_CONV_I8)
			++ip;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				sp [-1].data.l = (gint64)sp [-1].data.f;
				break;
			case VAL_I64:
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				sp [-1].data.l = (gint64)sp [-1].data.i;
				break;
			default:
				sp [-1].data.l = (gint64)sp [-1].data.nati;
				break;
			}
			sp [-1].type = ip[-1] == CEE_CONV_I ? VAL_NATI : VAL_I64;
			BREAK;
		CASE (CEE_CONV_R4) {
			++ip;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				sp [-1].data.f = (float)sp [-1].data.f;
				break;
			case VAL_I64:
				sp [-1].data.f = (float)sp [-1].data.l;
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				sp [-1].data.f = (float)sp [-1].data.i;
				break;
			default:
				sp [-1].data.f = (float)sp [-1].data.nati;
				break;
			}
			sp [-1].type = VAL_DOUBLE;
			BREAK;
		}
		CASE (CEE_CONV_R8) {
			++ip;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				sp [-1].data.f = (double)sp [-1].data.f;
				break;
			case VAL_I64:
				sp [-1].data.f = (double)sp [-1].data.l;
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				sp [-1].data.f = (double)sp [-1].data.i;
				break;
			default:
				sp [-1].data.f = (double)sp [-1].data.nati;
				break;
			}
			sp [-1].type = VAL_DOUBLE;
			BREAK;
		}
#if SIZEOF_VOID_P == 8
		CASE (CEE_CONV_U) /* Fall through */
#endif
		CASE (CEE_CONV_U8)
			++ip;

			switch (sp [-1].type){
			case VAL_DOUBLE:
				sp [-1].data.l = (guint64)sp [-1].data.f;
				break;
			case VAL_I64:
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				sp [-1].data.l = sp [-1].data.i & 0xffffffff;
				break;
			default:
				sp [-1].data.l = (guint64) sp [-1].data.nati;
				break;
			}
			sp [-1].type = ip[-1] == CEE_CONV_U ? VAL_NATI : VAL_I64;
		        BREAK;
		CASE (CEE_CPOBJ) {
			MonoClass *vtklass;
			++ip;
			vtklass = mono_class_get (image, read32 (ip));
			ip += 4;
			sp -= 2;
			memcpy (sp [0].data.p, sp [1].data.p, mono_class_value_size (vtklass, NULL));
			BREAK;
		}
		CASE (CEE_LDOBJ) {
			guint32 token;
			MonoClass *c;
			char *addr;

			++ip;
			token = read32 (ip);
			ip += 4;

			if (frame->method->wrapper_type != MONO_WRAPPER_NONE)
				c = (MonoClass *)mono_method_get_wrapper_data (frame->method, token);
			else
				c = mono_class_get (image, token);

			addr = sp [-1].data.vt;
			vt_alloc (&c->byval_arg, &sp [-1], FALSE);
			stackval_from_data (&c->byval_arg, &sp [-1], addr, FALSE);
			BREAK;
		}
		CASE (CEE_LDSTR) {
			MonoObject *o;
			guint32 str_index;

			ip++;
			str_index = mono_metadata_token_index (read32 (ip));
			ip += 4;

			if (frame->method->wrapper_type != MONO_WRAPPER_NONE) {
				o = (MonoObject *)mono_string_new_wrapper(
					mono_method_get_wrapper_data (frame->method, str_index));
			}
			else
				o = (MonoObject*)mono_ldstr (domain, image, str_index);
			sp->type = VAL_OBJ;
			sp->data.p = o;

			++sp;
			BREAK;
		}
		CASE (CEE_NEWOBJ) {
			MonoObject *o;
			MonoClass *newobj_class;
			MonoMethodSignature *csig;
			stackval valuetype_this;
			stackval *endsp = sp;
			guint32 token;
			stackval retval;

			frame->ip = ip;

			ip++;
			token = read32 (ip);
			ip += 4;

			if (frame->method->wrapper_type != MONO_WRAPPER_NONE)
				child_frame.method = (MonoMethod *)mono_method_get_wrapper_data (frame->method, token);
			else 
				child_frame.method = mono_get_method (image, token, NULL);
			if (!child_frame.method)
				THROW_EX (mono_get_exception_missing_method (), ip -5);

			csig = child_frame.method->signature;
			newobj_class = child_frame.method->klass;
			/*if (profiling_classes) {
				guint count = GPOINTER_TO_UINT (g_hash_table_lookup (profiling_classes, newobj_class));
				count++;
				g_hash_table_insert (profiling_classes, newobj_class, GUINT_TO_POINTER (count));
			}*/
				

			if (newobj_class->parent == mono_defaults.array_class) {
				sp -= csig->param_count;
				o = ves_array_create (domain, newobj_class, csig, sp);
				goto array_constructed;
			}

			/*
			 * First arg is the object.
			 */
			if (newobj_class->valuetype) {
				void *zero;
				vt_alloc (&newobj_class->byval_arg, &valuetype_this, csig->pinvoke);
				if (!newobj_class->enumtype && (newobj_class->byval_arg.type == MONO_TYPE_VALUETYPE)) {
					zero = valuetype_this.data.vt;
					child_frame.obj = valuetype_this.data.vt;
				} else {
					memset (&valuetype_this, 0, sizeof (stackval));
					zero = &valuetype_this;
					child_frame.obj = &valuetype_this;
				}
				stackval_from_data (&newobj_class->byval_arg, &valuetype_this, zero, csig->pinvoke);
			} else {
				if (newobj_class != mono_defaults.string_class) {
					o = mono_object_new (domain, newobj_class);
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
			child_frame.ex_handler = NULL;

			ves_exec_method_with_context (&child_frame, context);

			context->current_frame = frame;

			while (endsp > sp) {
				--endsp;
				vt_free (endsp);
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
				sp->type = VAL_OBJ;
				sp->data.p = o;
			}
			++sp;
			BREAK;
		}
		CASE (CEE_CASTCLASS) /* Fall through */
		CASE (CEE_ISINST) {
			MonoObject *o;
			MonoClass *c;
			guint32 token;
			int do_isinst = *ip == CEE_ISINST;

			++ip;
			token = read32 (ip);
			c = mono_class_get (image, token);

			g_assert (sp [-1].type == VAL_OBJ);

			if ((o = sp [-1].data.p)) {
				if (!mono_object_isinst (o, c)) {
					if (do_isinst) {
						sp [-1].data.p = NULL;
					} else
						THROW_EX (mono_get_exception_invalid_cast (), ip - 1);
				}
			}
			ip += 4;
			BREAK;
		}
		CASE (CEE_CONV_R_UN)
			++ip;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				break;
			case VAL_I64:
				sp [-1].data.f = (double)(guint64)sp [-1].data.l;
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				sp [-1].data.f = (double)(guint32)sp [-1].data.i;
				break;
			default:
				sp [-1].data.f = (double)(guint64)sp [-1].data.nati;
				break;
			}
			sp [-1].type = VAL_DOUBLE;
			BREAK;
		CASE (CEE_UNUSED58)
		CASE (CEE_UNUSED1) ves_abort(); BREAK;
		CASE (CEE_UNBOX) {
			MonoObject *o;
			MonoClass *c;
			guint32 token;

			++ip;
			token = read32 (ip);
			
			if (frame->method->wrapper_type != MONO_WRAPPER_NONE)
				c = (MonoClass *)mono_method_get_wrapper_data (frame->method, token);
			else 
				c = mono_class_get (image, token);
			
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference(), ip - 1);

			if (!(mono_object_isinst (o, c) || 
				  ((o->vtable->klass->rank == 0) && 
				   (o->vtable->klass->element_class == c->element_class))))
				THROW_EX (mono_get_exception_invalid_cast (), ip - 1);

			sp [-1].type = VAL_MP;
			sp [-1].data.p = (char *)o + sizeof (MonoObject);

			ip += 4;
			BREAK;
		}
		CASE (CEE_THROW)
			--sp;
			frame->ex_handler = NULL;
			if (!sp->data.p)
				sp->data.p = mono_get_exception_null_reference ();
			THROW_EX ((MonoException *)sp->data.p, ip);
			BREAK;
		CASE (CEE_LDFLDA) /* Fall through */
		CASE (CEE_LDFLD) {
			MonoObject *obj;
			MonoClassField *field;
			guint32 token;
			int load_addr = *ip == CEE_LDFLDA;
			char *addr;

			if (!sp [-1].data.p)
				THROW_EX (mono_get_exception_null_reference (), ip);
			
			++ip;
			token = read32 (ip);
			field = rtd->field_info[token].field;
			ip += 4;

			if (sp [-1].type == VAL_OBJ) {
				obj = sp [-1].data.p;
				if (obj->vtable->klass == mono_defaults.transparent_proxy_class && field->parent->marshalbyref) {
					MonoClass *klass = ((MonoTransparentProxy*)obj)->klass;
					addr = mono_load_remote_field (obj, klass, field, NULL);
				} else {
					addr = (char*)obj + field->offset;
				}				
			} else {
				obj = sp [-1].data.vt;
				addr = (char*)obj + field->offset - sizeof (MonoObject);
			}

			if (load_addr) {
				sp [-1].type = VAL_MP;
				sp [-1].data.p = addr;
			} else {
				vt_alloc (field->type, &sp [-1], FALSE);
				stackval_from_data (field->type, &sp [-1], addr, FALSE);				
			}
			BREAK;
		}
		CASE (CEE_STFLD) {
			MonoObject *obj;
			MonoClassField *field;
			guint32 token, offset;

			sp -= 2;
			
			if (!sp [0].data.p)
				THROW_EX (mono_get_exception_null_reference (), ip);
			
			++ip;
			token = read32 (ip);
			field = rtd->field_info[token].field;
			ip += 4;
			
			if (sp [0].type == VAL_OBJ) {
				obj = sp [0].data.p;

				if (obj->vtable->klass == mono_defaults.transparent_proxy_class && field->parent->marshalbyref) {
					MonoClass *klass = ((MonoTransparentProxy*)obj)->klass;
					mono_store_remote_field (obj, klass, field, &sp [1].data);
				} else {
					offset = field->offset;
					stackval_to_data (field->type, &sp [1], (char*)obj + offset, FALSE);
					vt_free (&sp [1]);
				}
			} else {
				obj = sp [0].data.vt;
				offset = field->offset - sizeof (MonoObject);
				stackval_to_data (field->type, &sp [1], (char*)obj + offset, FALSE);
				vt_free (&sp [1]);
			}

			BREAK;
		}
		CASE (CEE_LDSFLD) /* Fall through */
		CASE (CEE_LDSFLDA) {
			MonoVTable *vt;
			MonoClassField *field;
			guint32 token;
			int load_addr = *ip == CEE_LDSFLDA;
			gpointer addr;

			++ip;
			token = read32 (ip);
			field = rtd->field_info[token].field;
			ip += 4;
						
			vt = mono_class_vtable (domain, field->parent);
			if (!vt->initialized)
				mono_runtime_class_init (vt);
			if (!domain->thread_static_fields || !(addr = g_hash_table_lookup (domain->thread_static_fields, field)))
				addr = (char*)(vt->data) + field->offset;
			else
				addr = mono_threads_get_static_data (GPOINTER_TO_UINT (addr));

			if (load_addr) {
				sp->type = VAL_MP;
				sp->data.p = addr;
			} else {
				vt_alloc (field->type, sp, FALSE);
				stackval_from_data (field->type, sp, addr, FALSE);
			}
			++sp;
			BREAK;
		}
		CASE (CEE_STSFLD) {
			MonoVTable *vt;
			MonoClassField *field;
			guint32 token;
			gpointer addr;

			++ip;
			token = read32 (ip);
			field = rtd->field_info[token].field;
			ip += 4;
			--sp;

			vt = mono_class_vtable (domain, field->parent);
			if (!vt->initialized)
				mono_runtime_class_init (vt);
			if (!domain->thread_static_fields || !(addr = g_hash_table_lookup (domain->thread_static_fields, field)))
				addr = (char*)(vt->data) + field->offset;
			else
				addr = mono_threads_get_static_data (GPOINTER_TO_UINT (addr));

			stackval_to_data (field->type, sp, addr, FALSE);
			vt_free (sp);
			BREAK;
		}
		CASE (CEE_STOBJ) {
			MonoClass *vtklass;
			++ip;
			vtklass = mono_class_get (image, read32 (ip));
			ip += 4;
			sp -= 2;

			/*
			 * LAMESPEC: According to the spec, the stack should contain a 
			 * pointer to a value type. In reality, it can contain anything.
			 */
			if (sp [1].type == VAL_VALUET)
				memcpy (sp [0].data.p, sp [1].data.vt, mono_class_value_size (vtklass, NULL));
			else
				memcpy (sp [0].data.p, &sp [1].data, mono_class_value_size (vtklass, NULL));
			BREAK;
		}
#if SIZEOF_VOID_P == 8
		CASE (CEE_CONV_OVF_I_UN)
#endif
		CASE (CEE_CONV_OVF_I8_UN) {
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				if (sp [-1].data.f < 0 || sp [-1].data.f > 9223372036854775807LL)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.l = (guint64)sp [-1].data.f;
				break;
			case VAL_I64:
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				/* Can't overflow */
				sp [-1].data.l = (guint64)sp [-1].data.i;
				break;
			default:
				sp [-1].data.l = (guint64)sp [-1].data.nati;
				break;
			}
			sp [-1].type = VAL_I64;
			++ip;
			BREAK;
		}
#if SIZEOF_VOID_P == 8
		CASE (CEE_CONV_OVF_U_UN) 
#endif
		CASE (CEE_CONV_OVF_U8_UN) {
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				if (sp [-1].data.f < 0 || sp [-1].data.f > MYGUINT64_MAX)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.l = (guint64)sp [-1].data.f;
				break;
			case VAL_I64:
				/* nothing to do */
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				/* Can't overflow */
				sp [-1].data.l = (guint64)sp [-1].data.i;
				break;
			default:
				/* Can't overflow */
				sp [-1].data.l = (guint64)sp [-1].data.nati;
				break;
			}
			sp [-1].type = VAL_I64;
			++ip;
			BREAK;
		}
#if SIZEOF_VOID_P == 4
		CASE (CEE_CONV_OVF_I_UN)
		CASE (CEE_CONV_OVF_U_UN) 
#endif
		CASE (CEE_CONV_OVF_I1_UN)
		CASE (CEE_CONV_OVF_I2_UN)
		CASE (CEE_CONV_OVF_I4_UN)
		CASE (CEE_CONV_OVF_U1_UN)
		CASE (CEE_CONV_OVF_U2_UN)
		CASE (CEE_CONV_OVF_U4_UN) {
			guint64 value;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				if (sp [-1].data.f <= -1.0)
					THROW_EX (mono_get_exception_overflow (), ip);
				value = (guint64)sp [-1].data.f;
				break;
			case VAL_I64:
				value = (guint64)sp [-1].data.l;
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				value = (guint64)sp [-1].data.i;
				break;
			default:
				value = (guint64)sp [-1].data.nati;
				break;
			}
			switch (*ip) {
			case CEE_CONV_OVF_I1_UN:
				if (value > 127)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
			case CEE_CONV_OVF_I2_UN:
				if (value > 32767)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
#if SIZEOF_VOID_P == 4
			case CEE_CONV_OVF_I_UN: /* Fall through */
#endif
			case CEE_CONV_OVF_I4_UN:
				if (value > MYGUINT32_MAX)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
			case CEE_CONV_OVF_U1_UN:
				if (value > 255)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
			case CEE_CONV_OVF_U2_UN:
				if (value > 65535)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
#if SIZEOF_VOID_P == 4
			case CEE_CONV_OVF_U_UN: /* Fall through */
#endif
			case CEE_CONV_OVF_U4_UN:
				if (value > 4294967295U)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
			default:
				g_assert_not_reached ();
			}
			++ip;
			BREAK;
		}
		CASE (CEE_BOX) {
			guint32 token;
			MonoClass *class;

			ip++;
			token = read32 (ip);

			if (frame->method->wrapper_type != MONO_WRAPPER_NONE)
				class = (MonoClass *)mono_method_get_wrapper_data (frame->method, token);
			else
				class = mono_class_get (image, token);
			g_assert (class != NULL);

			sp [-1].type = VAL_OBJ;
			if (class->byval_arg.type == MONO_TYPE_VALUETYPE && !class->enumtype) 
				sp [-1].data.p = mono_value_box (domain, class, sp [-1].data.p);
			else {
				stackval_to_data (&class->byval_arg, &sp [-1], (char*)&sp [-1], FALSE);
				sp [-1].data.p = mono_value_box (domain, class, &sp [-1]);
			}
			/* need to vt_free (sp); */

			ip += 4;

			BREAK;
		}
		CASE (CEE_NEWARR) {
			MonoClass *class;
			MonoObject *o;
			guint32 token;

			ip++;
			token = read32 (ip);

			if (frame->method->wrapper_type != MONO_WRAPPER_NONE)
				class = (MonoClass *)mono_method_get_wrapper_data (frame->method, token);
			else
				class = mono_class_get (image, token);

			o = (MonoObject*) mono_array_new (domain, class, sp [-1].data.i);
			ip += 4;

			sp [-1].type = VAL_OBJ;
			sp [-1].data.p = o;
			/*if (profiling_classes) {
				guint count = GPOINTER_TO_UINT (g_hash_table_lookup (profiling_classes, o->vtable->klass));
				count++;
				g_hash_table_insert (profiling_classes, o->vtable->klass, GUINT_TO_POINTER (count));
			}*/

			BREAK;
		}
		CASE (CEE_LDLEN) {
			MonoArray *o;

			ip++;

			g_assert (sp [-1].type == VAL_OBJ);

			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip - 1);
			
			g_assert (MONO_CLASS_IS_ARRAY (o->obj.vtable->klass));

			sp [-1].type = VAL_I32;
			sp [-1].data.i = mono_array_length (o);

			BREAK;
		}
		CASE (CEE_LDELEMA) {
			MonoArray *o;
			guint32 esize, token;
			mono_u aindex;
			
			++ip;
			token = read32 (ip);
			ip += 4;
			sp -= 2;

			g_assert (sp [0].type == VAL_OBJ);
			o = sp [0].data.p;

			g_assert (MONO_CLASS_IS_ARRAY (o->obj.vtable->klass));
			
			aindex = sp [1].type == VAL_I32? sp [1].data.i: sp [1].data.nati;
			if (aindex >= mono_array_length (o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip - 5);

			/* check the array element corresponds to token */
			esize = mono_array_element_size (o->obj.vtable->klass);
			
			sp->type = VAL_MP;
			sp->data.p = mono_array_addr_with_size (o, esize, aindex);

			++sp;
			BREAK;
		}
		CASE (CEE_LDELEM_I1) /* fall through */
		CASE (CEE_LDELEM_U1) /* fall through */
		CASE (CEE_LDELEM_I2) /* fall through */
		CASE (CEE_LDELEM_U2) /* fall through */
		CASE (CEE_LDELEM_I4) /* fall through */
		CASE (CEE_LDELEM_U4) /* fall through */
		CASE (CEE_LDELEM_I8)  /* fall through */
		CASE (CEE_LDELEM_I)  /* fall through */
		CASE (CEE_LDELEM_R4) /* fall through */
		CASE (CEE_LDELEM_R8) /* fall through */
		CASE (CEE_LDELEM_REF) {
			MonoArray *o;
			mono_u aindex;

			sp -= 2;

			g_assert (sp [0].type == VAL_OBJ);
			o = sp [0].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);

			g_assert (MONO_CLASS_IS_ARRAY (o->obj.vtable->klass));
			
			aindex = sp [1].type == VAL_I32? sp [1].data.i: sp [1].data.nati;
			if (aindex >= mono_array_length (o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip);

			/*
			 * FIXME: throw mono_get_exception_array_type_mismatch () if needed 
			 */
			switch (*ip) {
			case CEE_LDELEM_I1:
				sp [0].data.i = mono_array_get (o, gint8, aindex);
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_U1:
				sp [0].data.i = mono_array_get (o, guint8, aindex);
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_I2:
				sp [0].data.i = mono_array_get (o, gint16, aindex);
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_U2:
				sp [0].data.i = mono_array_get (o, guint16, aindex);
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_I:
				sp [0].data.nati = mono_array_get (o, mono_i, aindex);
				sp [0].type = VAL_NATI;
				break;
			case CEE_LDELEM_I4:
				sp [0].data.i = mono_array_get (o, gint32, aindex);
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_U4:
				sp [0].data.i = mono_array_get (o, guint32, aindex);
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_I8:
				sp [0].data.l = mono_array_get (o, guint64, aindex);
				sp [0].type = VAL_I64;
				break;
			case CEE_LDELEM_R4:
				sp [0].data.f = mono_array_get (o, float, aindex);
				sp [0].type = VAL_DOUBLE;
				break;
			case CEE_LDELEM_R8:
				sp [0].data.f = mono_array_get (o, double, aindex);
				sp [0].type = VAL_DOUBLE;
				break;
			case CEE_LDELEM_REF:
				sp [0].data.p = mono_array_get (o, gpointer, aindex);
				sp [0].type = VAL_OBJ;
				break;
			default:
				ves_abort();
			}

			++ip;
			++sp;
			BREAK;
		}
		CASE (CEE_STELEM_I)  /* fall through */
		CASE (CEE_STELEM_I1) /* fall through */ 
		CASE (CEE_STELEM_I2) /* fall through */
		CASE (CEE_STELEM_I4) /* fall through */
		CASE (CEE_STELEM_I8) /* fall through */
		CASE (CEE_STELEM_R4) /* fall through */
		CASE (CEE_STELEM_R8) /* fall through */
		CASE (CEE_STELEM_REF) {
			MonoArray *o;
			MonoClass *ac;
			mono_u aindex;

			sp -= 3;

			g_assert (sp [0].type == VAL_OBJ);
			o = sp [0].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip);

			ac = o->obj.vtable->klass;
			g_assert (MONO_CLASS_IS_ARRAY (ac));
		    
			aindex = sp [1].type == VAL_I32? sp [1].data.i: sp [1].data.nati;
			if (aindex >= mono_array_length (o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip);

			/*
			 * FIXME: throw mono_get_exception_array_type_mismatch () if needed 
			 */
			switch (*ip) {
			case CEE_STELEM_I:
				mono_array_set (o, mono_i, aindex, sp [2].data.nati);
				break;
			case CEE_STELEM_I1:
				mono_array_set (o, gint8, aindex, sp [2].data.i);
				break;
			case CEE_STELEM_I2:
				mono_array_set (o, gint16, aindex, sp [2].data.i);
				break;
			case CEE_STELEM_I4:
				mono_array_set (o, gint32, aindex, sp [2].data.i);
				break;
			case CEE_STELEM_I8:
				mono_array_set (o, gint64, aindex, sp [2].data.l);
				break;
			case CEE_STELEM_R4:
				mono_array_set (o, float, aindex, sp [2].data.f);
				break;
			case CEE_STELEM_R8:
				mono_array_set (o, double, aindex, sp [2].data.f);
				break;
			case CEE_STELEM_REF:
				g_assert (sp [2].type == VAL_OBJ);
				if (sp [2].data.p && !mono_object_isinst (sp [2].data.p, mono_object_class (o)->element_class))
					THROW_EX (mono_get_exception_array_type_mismatch (), ip);
				mono_array_set (o, gpointer, aindex, sp [2].data.p);
				break;
			default:
				ves_abort();
			}

			++ip;
			BREAK;
		}
		CASE (CEE_LDELEM) 
		CASE (CEE_STELEM) 
		CASE (CEE_UNBOX_ANY) 
		CASE (CEE_UNUSED5) 
		CASE (CEE_UNUSED6) 
		CASE (CEE_UNUSED7) 
		CASE (CEE_UNUSED8) 
		CASE (CEE_UNUSED9) 
		CASE (CEE_UNUSED10) 
		CASE (CEE_UNUSED11) 
		CASE (CEE_UNUSED12) 
		CASE (CEE_UNUSED13) 
		CASE (CEE_UNUSED14) 
		CASE (CEE_UNUSED15) 
		CASE (CEE_UNUSED16) 
		CASE (CEE_UNUSED17) ves_abort(); BREAK;

#if SIZEOF_VOID_P == 4
		CASE (CEE_CONV_OVF_I)
		CASE (CEE_CONV_OVF_U) 
#endif
		CASE (CEE_CONV_OVF_I1)
		CASE (CEE_CONV_OVF_I2)
		CASE (CEE_CONV_OVF_I4)
		CASE (CEE_CONV_OVF_U1)
		CASE (CEE_CONV_OVF_U2)
		CASE (CEE_CONV_OVF_U4) {
			gint64 value;
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				value = (gint64)sp [-1].data.f;
				break;
			case VAL_I64:
				value = (gint64)sp [-1].data.l;
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				value = (gint64)sp [-1].data.i;
				break;
			default:
				value = (gint64)sp [-1].data.nati;
				break;
			}
			switch (*ip) {
			case CEE_CONV_OVF_I1:
				if (value < -128 || value > 127)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
			case CEE_CONV_OVF_I2:
				if (value < -32768 || value > 32767)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
#if SIZEOF_VOID_P == 4
			case CEE_CONV_OVF_I: /* Fall through */
#endif
			case CEE_CONV_OVF_I4:
				if (value < MYGINT32_MIN || value > MYGINT32_MAX)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
			case CEE_CONV_OVF_U1:
				if (value < 0 || value > 255)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
			case CEE_CONV_OVF_U2:
				if (value < 0 || value > 65535)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
#if SIZEOF_VOID_P == 4
			case CEE_CONV_OVF_U: /* Fall through */
#endif
			case CEE_CONV_OVF_U4:
				if (value < 0 || value > MYGUINT32_MAX)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = value;
				sp [-1].type = VAL_I32;
				break;
			default:
				g_assert_not_reached ();
			}
			++ip;
			BREAK;
		}

#if SIZEOF_VOID_P == 8
		CASE (CEE_CONV_OVF_I)
#endif
		CASE (CEE_CONV_OVF_I8)
			/* FIXME: handle other cases */
			if (sp [-1].type == VAL_I32) {
				sp [-1].data.l = (guint64)sp [-1].data.i;
				sp [-1].type = VAL_I64;
			} else if(sp [-1].type == VAL_I64) {
				/* defined as NOP */
			} else {
				ves_abort();
			}
			++ip;
			BREAK;

#if SIZEOF_VOID_P == 8
		CASE (CEE_CONV_OVF_U)
#endif
		CASE (CEE_CONV_OVF_U8)
			/* FIXME: handle other cases */
			if (sp [-1].type == VAL_I32) {
				sp [-1].data.l = (guint64) sp [-1].data.i;
				sp [-1].type = VAL_I64;
			} else if(sp [-1].type == VAL_I64) {
				/* defined as NOP */
			} else {
				ves_abort();
			}
			++ip;
			BREAK;
		CASE (CEE_UNUSED50) 
		CASE (CEE_UNUSED18) 
		CASE (CEE_UNUSED19) 
		CASE (CEE_UNUSED20) 
		CASE (CEE_UNUSED21) 
		CASE (CEE_UNUSED22) 
		CASE (CEE_UNUSED23) ves_abort(); BREAK;
		CASE (CEE_REFANYVAL) ves_abort(); BREAK;
		CASE (CEE_CKFINITE)
			if (!finite(sp [-1].data.f))
				THROW_EX (mono_get_exception_arithmetic (), ip);
			++ip;
			BREAK;
		CASE (CEE_UNUSED24) ves_abort(); BREAK;
		CASE (CEE_UNUSED25) ves_abort(); BREAK;
		CASE (CEE_MKREFANY) ves_abort(); BREAK;
		CASE (CEE_UNUSED59) 
		CASE (CEE_UNUSED60) 
		CASE (CEE_UNUSED61) 
		CASE (CEE_UNUSED62) 
		CASE (CEE_UNUSED63) 
		CASE (CEE_UNUSED64) 
		CASE (CEE_UNUSED65) 
		CASE (CEE_UNUSED66) 
		CASE (CEE_UNUSED67) ves_abort(); BREAK;
		CASE (CEE_LDTOKEN) {
			gpointer handle;
			MonoClass *handle_class;
			++ip;
			handle = mono_ldtoken (image, read32 (ip), &handle_class);
			ip += 4;
			vt_alloc (&handle_class->byval_arg, sp, FALSE);
			stackval_from_data (&handle_class->byval_arg, sp, (char*)&handle, FALSE);
			++sp;
			BREAK;
		}
		CASE (CEE_ADD_OVF)
			--sp;
			/* FIXME: check overflow */
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_I32) {
					if (CHECK_ADD_OVERFLOW (sp [-1].data.i, sp [0].data.i))
						THROW_EX (mono_get_exception_overflow (), ip);
					sp [-1].data.i = (gint32)sp [-1].data.i + (gint32)sp [0].data.i;
				} else {
					if (CHECK_ADD_OVERFLOW_NAT (sp [-1].data.nati, (mono_i)sp [0].data.i))
						THROW_EX (mono_get_exception_overflow (), ip);
					sp [-1].data.nati = sp [-1].data.nati + (mono_i)sp [0].data.i;
				}
			} else if (sp->type == VAL_I64) {
				if (CHECK_ADD_OVERFLOW64 (sp [-1].data.l, sp [0].data.l))
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.l = (gint64)sp [-1].data.l + (gint64)sp [0].data.l;
			} else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f += sp [0].data.f;
			else {
				char *p = sp [-1].data.p;
				p += GET_NATI (sp [0]);
				sp [-1].data.p = p;
			}
			++ip;
			BREAK;
		CASE (CEE_ADD_OVF_UN)
			--sp;
			/* FIXME: check overflow, make unsigned */
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_I32) {
					if (CHECK_ADD_OVERFLOW_UN (sp [-1].data.i, sp [0].data.i))
						THROW_EX (mono_get_exception_overflow (), ip);
					sp [-1].data.i = (guint32)sp [-1].data.i + (guint32)sp [0].data.i;
				} else {
					if (CHECK_ADD_OVERFLOW_NAT_UN (sp [-1].data.nati, (mono_u)sp [0].data.i))
						THROW_EX (mono_get_exception_overflow (), ip);
					sp [-1].data.nati = (mono_u)sp [-1].data.nati + (mono_u)sp [0].data.i;
				}
			} else if (sp->type == VAL_I64) {
				if (CHECK_ADD_OVERFLOW64_UN (sp [-1].data.l, sp [0].data.l))
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.l = (guint64)sp [-1].data.l + (guint64)sp [0].data.l;
			} else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f += sp [0].data.f;
			else {
				char *p = sp [-1].data.p;
				p += GET_NATI (sp [0]);
				sp [-1].data.p = p;
			}
			++ip;
			BREAK;
		CASE (CEE_MUL_OVF)
			--sp;
			/* FIXME: check overflow */
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_NATI) {
					if (CHECK_MUL_OVERFLOW_NAT (sp [-1].data.nati, sp [0].data.i))
						THROW_EX (mono_get_exception_overflow (), ip);
					sp [-1].data.nati = sp [-1].data.nati * sp [0].data.i;
					sp [-1].type = VAL_NATI;
				} else {
					if (CHECK_MUL_OVERFLOW (sp [-1].data.i, sp [0].data.i))
						THROW_EX (mono_get_exception_overflow (), ip);
					sp [-1].data.i *= sp [0].data.i;
				}
			}
			else if (sp->type == VAL_I64) {
				if (CHECK_MUL_OVERFLOW64 (sp [-1].data.l, sp [0].data.l))
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.l *= sp [0].data.l;
			}
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f *= sp [0].data.f;
			else
				ves_abort();
			++ip;
			BREAK;
		CASE (CEE_MUL_OVF_UN)
			--sp;
			/* FIXME: check overflow, make unsigned */
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_NATI) {
					if (CHECK_MUL_OVERFLOW_NAT_UN (sp [-1].data.nati, (mono_u)sp [0].data.i))
						THROW_EX (mono_get_exception_overflow (), ip);
					sp [-1].data.nati = (mono_u)sp [-1].data.nati * (mono_u)sp [0].data.i;
					sp [-1].type = VAL_NATI;
				} else {
					if (CHECK_MUL_OVERFLOW_UN ((guint32)sp [-1].data.i, (guint32)sp [0].data.i))
						THROW_EX (mono_get_exception_overflow (), ip);
					sp [-1].data.i *= sp [0].data.i;
				}
			}
			else if (sp->type == VAL_I64) {
				if (CHECK_MUL_OVERFLOW64_UN (sp [-1].data.l, sp [0].data.l))
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.l *= sp [0].data.l;
			}
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f *= sp [0].data.f;
			else
				ves_abort();
			++ip;
			BREAK;
		CASE (CEE_SUB_OVF)
			--sp;
			/* FIXME: handle undeflow/unsigned */
			/* should probably consider the pointers as unsigned */
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_I32) {
					if (CHECK_SUB_OVERFLOW (sp [-1].data.i, sp [0].data.i))
						THROW_EX (mono_get_exception_overflow (), ip);
					sp [-1].data.i -= sp [0].data.i;
				} else {
					sp [-1].data.nati = sp [-1].data.nati - sp [0].data.i;
					sp [-1].type = VAL_NATI;
				}
			}
			else if (sp->type == VAL_I64) {
				if (CHECK_SUB_OVERFLOW64 (sp [-1].data.l, sp [0].data.l))
				    THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.l -= sp [0].data.l;
			}
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f -= sp [0].data.f;
			else {
				if (sp [-1].type == VAL_I32) {
					sp [-1].data.nati = sp [-1].data.i - sp [0].data.nati;
					sp [-1].type = sp [0].type;
				} else
					sp [-1].data.nati = sp [-1].data.nati - sp [0].data.nati;
			}
			++ip;
			BREAK;
		CASE (CEE_SUB_OVF_UN)
			--sp;
			/* FIXME: handle undeflow/unsigned */
			/* should probably consider the pointers as unsigned */
			if (sp->type == VAL_I32) {
				if (sp [-1].type == VAL_I32) {
					if (CHECK_SUB_OVERFLOW_UN (sp [-1].data.i, sp [0].data.i))
						THROW_EX (mono_get_exception_overflow (), ip);
					sp [-1].data.i -= sp [0].data.i;
				} else {
					sp [-1].data.nati = sp [-1].data.nati - sp [0].data.i;
					sp [-1].type = VAL_NATI;
				}
			}
			else if (sp->type == VAL_I64) {
				if (CHECK_SUB_OVERFLOW64_UN (sp [-1].data.l, sp [0].data.l))
				    THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.l -= sp [0].data.l;
			}
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f -= sp [0].data.f;
			else {
				if (sp [-1].type == VAL_I32) {
					sp [-1].data.nati = sp [-1].data.i - sp [0].data.nati;
					sp [-1].type = sp [0].type;
				} else
					sp [-1].data.nati = sp [-1].data.nati - sp [0].data.nati;
			}
			++ip;
			BREAK;
		CASE (CEE_ENDFINALLY)
			if (finally_ips) {
				ip = finally_ips->data;
				finally_ips = g_slist_remove (finally_ips, ip);
				goto main_loop;
			}
			if (frame->ex)
				goto handle_fault;
			ves_abort();
			BREAK;
		CASE (CEE_LEAVE) /* Fall through */
		CASE (CEE_LEAVE_S)
			while (sp > frame->stack) {
				--sp;
				vt_free (sp);
			}
			frame->ip = ip;
			if (*ip == CEE_LEAVE_S) {
				++ip;
				ip += (signed char) *ip;
				++ip;
			} else {
				++ip;
				ip += (gint32) read32 (ip);
				ip += 4;
			}
			endfinally_ip = ip;
			if (frame->ex_handler != NULL && MONO_OFFSET_IN_HANDLER(frame->ex_handler, frame->ip - header->code)) {
				frame->ex_handler = NULL;
				frame->ex = NULL;
			}
			goto handle_finally;
			BREAK;
		CASE (CEE_UNUSED41)
			++ip;
		        switch (*ip) {
			case CEE_MONO_FUNC1: {
				MonoMarshalConv conv;
				++ip;

				conv = *ip;

				++ip;

				sp--;

				sp->type = VAL_NATI;

				switch (conv) {
				case MONO_MARSHAL_CONV_STR_LPWSTR:
					sp->data.p = mono_string_to_utf16 (sp->data.p);
					break;
				case MONO_MARSHAL_CONV_LPSTR_STR:
					sp->data.p = mono_string_new_wrapper (sp->data.p);
					break;
				case MONO_MARSHAL_CONV_STR_LPTSTR:
				case MONO_MARSHAL_CONV_STR_LPSTR:
					sp->data.p = mono_string_to_utf8 (sp->data.p);
					break;
				case MONO_MARSHAL_CONV_STR_BSTR:
					sp->data.p = mono_string_to_bstr (sp->data.p);
					break;
				case MONO_MARSHAL_CONV_STR_TBSTR:
				case MONO_MARSHAL_CONV_STR_ANSIBSTR:
					sp->data.p = mono_string_to_ansibstr (sp->data.p);
					break;
				case MONO_MARSHAL_CONV_SB_LPSTR:
					sp->data.p = mono_string_builder_to_utf8 (sp->data.p);
					break;
				case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
					sp->data.p = mono_array_to_savearray (sp->data.p);
					break;
				case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
					sp->data.p = mono_array_to_lparray (sp->data.p);
					break;
				case MONO_MARSHAL_CONV_DEL_FTN:
					sp->data.p = mono_delegate_to_ftnptr (sp->data.p);
					break;
				case MONO_MARSHAL_CONV_STRARRAY_STRLPARRAY:
					sp->data.p = mono_marshal_string_array (sp->data.p);
					break;
				case MONO_MARSHAL_CONV_LPWSTR_STR:
					sp->data.p = mono_string_from_utf16 (sp->data.p);
					break;
				default:
					fprintf(stderr, "MONO_FUNC1 %d", conv);
					g_assert_not_reached ();
				}
				sp++; 
				break;
			}
			case CEE_MONO_PROC2: {
				MonoMarshalConv conv;
				++ip;
				conv = *ip;
				++ip;

				sp -= 2;

				switch (conv) {
				case MONO_MARSHAL_CONV_LPSTR_SB:
					mono_string_utf8_to_builder (sp [0].data.p, sp [1].data.p);
					break;
				case MONO_MARSHAL_FREE_ARRAY:
					mono_marshal_free_array (sp [0].data.p, sp [1].data.i);
					break;
				default:
					g_assert_not_reached ();
				}				 
				break;
			}
			case CEE_MONO_PROC3: {
				MonoMarshalConv conv;
				++ip;
				conv = *ip;
				++ip;

				sp -= 3;

				switch (conv) {
				case MONO_MARSHAL_CONV_STR_BYVALSTR:
					mono_string_to_byvalstr (sp [0].data.p, sp [1].data.p, sp [2].data.i);
					break;
				case MONO_MARSHAL_CONV_STR_BYVALWSTR:
					mono_string_to_byvalwstr (sp [0].data.p, sp [1].data.p, sp [2].data.i);
					break;
				default:
					g_assert_not_reached ();
				}
				break;
			}
			case CEE_MONO_VTADDR: {
				++ip;

				sp [-1].type = VAL_MP;
				/* do nothing? */
				break;
			}
			case CEE_MONO_LDPTR: {
				guint32 token;
				++ip;
				
				token = read32 (ip);
				ip += 4;
				
				sp->type = VAL_NATI;
				sp->data.p = mono_method_get_wrapper_data (frame->method, token);
				++sp;
				break;
			}
			case CEE_MONO_FREE: {
				++ip;

				sp -= 1;
				g_free (sp->data.p);
				break;
			}
			case CEE_MONO_OBJADDR: {
				++ip;

				sp->type = VAL_MP;
				/* do nothing? */
				break;
			}
			case CEE_MONO_NEWOBJ: {
				MonoClass *class;
				guint32 token;

				++ip;
				token = read32 (ip);
				ip += 4;

				class = (MonoClass *)mono_method_get_wrapper_data (frame->method, token);
				sp->data.p = mono_object_new (domain, class);
				sp++;
				break;
			}
			case CEE_MONO_RETOBJ: {
				MonoClass *class;
				guint32 token;

				++ip;
				token = read32 (ip);
				ip += 4;

				sp--;

				class = (MonoClass *)mono_method_get_wrapper_data (frame->method, token);
				
				stackval_from_data (signature->ret, frame->retval, sp->data.vt, signature->pinvoke);

				if (sp > frame->stack)
					g_warning ("more values on stack: %d", sp-frame->stack);
				goto exit_frame;
			}
			case CEE_MONO_LDNATIVEOBJ: {
				MonoClass *class;
				guint32 token;

				++ip;
				token = read32 (ip);
				ip += 4;

				class = (MonoClass *)mono_method_get_wrapper_data (frame->method, token);
				g_assert(class->valuetype);

				sp [-1].type = VAL_MP;

				break;
			}
			default:
				g_error ("Unimplemented opcode: 0xF0 %02x at 0x%x\n", *ip, ip-header->code);
			}
			BREAK;
		CASE (CEE_UNUSED26) 
		CASE (CEE_UNUSED27) 
		CASE (CEE_UNUSED28) 
		CASE (CEE_UNUSED29) 
		CASE (CEE_UNUSED30) 
		CASE (CEE_UNUSED31) 
		CASE (CEE_UNUSED32) 
		CASE (CEE_UNUSED33) 
		CASE (CEE_UNUSED34) 
		CASE (CEE_UNUSED35) 
		CASE (CEE_UNUSED36) 
		CASE (CEE_UNUSED37) 
		CASE (CEE_UNUSED38) 
		CASE (CEE_UNUSED39) 
		CASE (CEE_UNUSED40) 
		CASE (CEE_UNUSED42) 
		CASE (CEE_UNUSED43) 
		CASE (CEE_UNUSED44) 
		CASE (CEE_UNUSED45) 
		CASE (CEE_UNUSED46) 
		CASE (CEE_UNUSED47) 
		CASE (CEE_UNUSED48)
		CASE (CEE_PREFIX7)
		CASE (CEE_PREFIX6)
		CASE (CEE_PREFIX5)
		CASE (CEE_PREFIX4)
		CASE (CEE_PREFIX3)
		CASE (CEE_PREFIX2)
		CASE (CEE_PREFIXREF) ves_abort(); BREAK;
		/*
		 * Note: Exceptions thrown when executing a prefixed opcode need
		 * to take into account the number of prefix bytes (usually the
		 * throw point is just (ip - n_prefix_bytes).
		 */
		SUB_SWITCH
			++ip;
			switch (*ip) {
			case CEE_ARGLIST: ves_abort(); break;
			case CEE_CEQ: {
				gint32 result;
				++ip;
				sp -= 2;

				if (sp->type == VAL_I32)
					result = (mono_i)sp [0].data.i == (mono_i)GET_NATI (sp [1]);
				else if (sp->type == VAL_I64)
					result = sp [0].data.l == sp [1].data.l;
				else if (sp->type == VAL_DOUBLE) {
					if (isnan (sp [0].data.f) || isnan (sp [1].data.f))
						result = 0;
					else
						result = sp [0].data.f == sp [1].data.f;
				} else
					result = sp [0].data.nati == GET_NATI (sp [1]);
				sp->type = VAL_I32;
				sp->data.i = result;

				sp++;
				break;
			}
			case CEE_CGT: {
				gint32 result;
				++ip;
				sp -= 2;

				if (sp->type == VAL_I32)
					result = (mono_i)sp [0].data.i > (mono_i)GET_NATI (sp [1]);
				else if (sp->type == VAL_I64)
					result = sp [0].data.l > sp [1].data.l;
				else if (sp->type == VAL_DOUBLE) {
					if (isnan (sp [0].data.f) || isnan (sp [1].data.f))
						result = 0;
					else
						result = sp [0].data.f > sp [1].data.f;
				} else
					result = (mono_i)sp [0].data.nati > (mono_i)GET_NATI (sp [1]);
				sp->type = VAL_I32;
				sp->data.i = result;

				sp++;
				break;
			}
			case CEE_CGT_UN: {
				gint32 result;
				++ip;
				sp -= 2;

				if (sp->type == VAL_I32)
					result = (mono_u)sp [0].data.i > (mono_u)GET_NATI (sp [1]);
				else if (sp->type == VAL_I64)
					result = (guint64)sp [0].data.l > (guint64)sp [1].data.l;
				else if (sp->type == VAL_DOUBLE)
					result = isnan (sp [0].data.f) || isnan (sp [1].data.f) ||
						sp[0].data.f > sp[1].data.f;
				else
					result = (mono_u)sp [0].data.nati > (mono_u)GET_NATI (sp [1]);
				sp->type = VAL_I32;
				sp->data.i = result;

				sp++;
				break;
			}
			case CEE_CLT: {
				gint32 result;
				++ip;
				sp -= 2;

				if (sp->type == VAL_I32)
					result = (mono_i)sp [0].data.i < (mono_i)GET_NATI (sp [1]);
				else if (sp->type == VAL_I64)
					result = sp [0].data.l < sp [1].data.l;
				else if (sp->type == VAL_DOUBLE) {
					if (isnan (sp [0].data.f) || isnan (sp [1].data.f))
						result = 0;
					else
						result = sp [0].data.f < sp [1].data.f;
				} else
					result = (mono_i)sp [0].data.nati < (mono_i)GET_NATI (sp [1]);
				sp->type = VAL_I32;
				sp->data.i = result;

				sp++;
				break;
			}
			case CEE_CLT_UN: {
				gint32 result;
				++ip;
				sp -= 2;

				if (sp->type == VAL_I32)
					result = (mono_u)sp [0].data.i < (mono_u)GET_NATI (sp [1]);
				else if (sp->type == VAL_I64)
					result = (guint64)sp [0].data.l < (guint64)sp [1].data.l;
				else if (sp->type == VAL_DOUBLE)
					result = isnan (sp [0].data.f) || isnan (sp [1].data.f) ||
						sp[0].data.f < sp[1].data.f;
				else
					result = (mono_u)sp [0].data.nati < (mono_u)GET_NATI (sp [1]);
				sp->type = VAL_I32;
				sp->data.i = result;

				sp++;
				break;
			}
			case CEE_LDFTN:
			case CEE_LDVIRTFTN: {
				int virtual = *ip == CEE_LDVIRTFTN;
				MonoMethod *m;
				guint32 token;
				++ip;
				token = read32 (ip);
				ip += 4;

				if (frame->method->wrapper_type != MONO_WRAPPER_NONE)
					m = (MonoMethod *)mono_method_get_wrapper_data (frame->method, token);
				else 
					m = mono_get_method (image, token, NULL);

				if (!m)
					THROW_EX (mono_get_exception_missing_method (), ip - 5);
				if (virtual) {
					--sp;
					if (!sp->data.p)
						THROW_EX (mono_get_exception_null_reference (), ip - 5);
					
					m = get_virtual_method (domain, m, sp);
				}

				/* 
				 * This prevents infinite cycles since the wrapper contains
				 * an ldftn too.
				 */
				if (frame->method->wrapper_type != MONO_WRAPPER_SYNCHRONIZED)
					if (m && m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
						m = mono_marshal_get_synchronized_wrapper (m);

				sp->type = VAL_NATI;
				sp->data.p = mono_create_method_pointer (m);
				++sp;
				break;
			}
			case CEE_UNUSED56: ves_abort(); break;
			case CEE_LDARG: {
				guint32 arg_pos;
				++ip;
				arg_pos = read16 (ip);
				ip += 2;
				vt_alloc (ARG_TYPE (signature, arg_pos), sp, signature->pinvoke);
				stackval_from_data (ARG_TYPE (signature, arg_pos), sp, ARG_POS (arg_pos), signature->pinvoke);
				++sp;
				break;
			}
			case CEE_LDARGA: {
				guint32 anum;
				++ip;
				anum = read16 (ip);
				ip += 2;
				sp->data.vt = ARG_POS (anum);
				sp->type = VAL_MP;
				++sp;
				break;
			}
			case CEE_STARG: {
				guint32 arg_pos;
				++ip;
				arg_pos = read16 (ip);
				ip += 2;
				--sp;
				stackval_to_data (ARG_TYPE (signature, arg_pos), sp, ARG_POS (arg_pos), signature->pinvoke);
				vt_free (sp);
				break;
			}
			case CEE_LDLOC: {
				guint32 loc_pos;
				++ip;
				loc_pos = read16 (ip);
				ip += 2;
				vt_alloc (LOCAL_TYPE (header, loc_pos), sp, FALSE);
				stackval_from_data (LOCAL_TYPE (header, loc_pos), sp, LOCAL_POS (loc_pos), FALSE);
				++sp;
				break;
			}
			case CEE_LDLOCA: {
				MonoType *t;
				guint32 loc_pos;

				++ip;
				loc_pos = read16 (ip);
				ip += 2;
				t = LOCAL_TYPE (header, loc_pos);
				sp->data.vt = LOCAL_POS (loc_pos);

				sp->type = VAL_MP;

				++sp;
				break;
			}
			case CEE_STLOC: {
				guint32 loc_pos;
				++ip;
				loc_pos = read16 (ip);
				ip += 2;
				--sp;
				stackval_to_data (LOCAL_TYPE (header, loc_pos), sp, LOCAL_POS (loc_pos), FALSE);
				vt_free (sp);
				break;
			}
			case CEE_LOCALLOC:
				--sp;
				if (sp != frame->stack)
					THROW_EX (mono_get_exception_execution_engine (NULL), ip - 1);
				++ip;
				sp->data.p = alloca (sp->data.i);
				sp->type = VAL_MP;
				sp++;
				break;
			case CEE_UNUSED57: ves_abort(); break;
			case CEE_ENDFILTER: ves_abort(); break;
			case CEE_UNALIGNED_:
				++ip;
				unaligned_address = 1;
				break;
			case CEE_VOLATILE_:
				++ip;
				volatile_address = 1;
				break;
			case CEE_TAIL_:
				++ip;
				tail_recursion = 1;
				break;
			case CEE_INITOBJ: {
				guint32 token;
				MonoClass *class;

				++ip;
				token = read32 (ip);
				ip += 4;

				class = mono_class_get (image, token);

				--sp;
				g_assert (sp->type == VAL_TP || sp->type == VAL_MP);
				memset (sp->data.vt, 0, mono_class_value_size (class, NULL));
				break;
			}
			case CEE_CONSTRAINED_: {
				guint32 token;
				/* FIXME: implement */
				++ip;
				token = read32 (ip);
				ip += 4;
				break;
			}
			case CEE_CPBLK:
				sp -= 3;
				if (!sp [0].data.p || !sp [1].data.p)
					THROW_EX (mono_get_exception_null_reference(), ip - 1);
				++ip;
				/* FIXME: value and size may be int64... */
				memcpy (sp [0].data.p, sp [1].data.p, sp [2].data.i);
				break;
			case CEE_INITBLK:
				sp -= 3;
				if (!sp [0].data.p)
					THROW_EX (mono_get_exception_null_reference(), ip - 1);
				++ip;
				/* FIXME: value and size may be int64... */
				memset (sp [0].data.p, sp [1].data.i, sp [2].data.i);
				break;
			case CEE_NO_:
				/* FIXME: implement */
				ip += 2;
				break;
			case CEE_RETHROW:
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
				break;
			case CEE_UNUSED: ves_abort(); break;
			case CEE_SIZEOF: {
				guint32 token;
				int align;
				++ip;
				token = read32 (ip);
				ip += 4;
				if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC) {
					MonoType *type = mono_type_create_from_typespec (image, token);
					sp->data.i = mono_type_size (type, &align);
				} else {
					MonoClass *szclass = mono_class_get (image, token);
					mono_class_init (szclass);
					if (!szclass->valuetype)
						THROW_EX (mono_exception_from_name (mono_defaults.corlib, "System", "InvalidProgramException"), ip - 5);
					sp->data.i = mono_class_value_size (szclass, &align);
				}
				sp->type = VAL_I32;
				++sp;
				break;
			}
			case CEE_REFANYTYPE: ves_abort(); break;
			default:
				g_error ("Unimplemented opcode: 0xFE %02x at 0x%x\n", *ip, ip-header->code);
			}
			continue;
		DEFAULT;
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
		MonoMethodHeader *hd;
		MonoExceptionClause *clause;
		/*char *message;*/
		MonoObject *ex_obj;

#if DEBUG_INTERP
		if (tracing)
			g_print ("* Handling exception '%s' at IL_%04x\n", frame->ex == NULL ? "** Unknown **" : mono_object_class (frame->ex)->name, frame->ip - header->code);
#endif
		if (die_on_exception)
			goto die_on_ex;

		if (frame->ex == quit_exception)
			goto handle_finally;
		
		for (inv = frame; inv; inv = inv->parent) {
			if (inv->method == NULL)
				continue;
			if (inv->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
				continue;
			if (inv->method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME))
				continue;
			hd = ((MonoMethodNormal*)inv->method)->header;
			ip_offset = inv->ip - hd->code;
			inv->ex_handler = NULL; /* clear this in case we are trhowing an exception while handling one  - this one wins */
			for (i = 0; i < hd->num_clauses; ++i) {
				clause = &hd->clauses [i];
				if (clause->flags <= 1 && MONO_OFFSET_IN_CLAUSE (clause, ip_offset)) {
					if (!clause->flags) {
						if (mono_object_isinst ((MonoObject*)frame->ex, mono_class_get (inv->method->klass->image, clause->token_or_filter))) {
							/* 
							 * OK, we found an handler, now we need to execute the finally
							 * and fault blocks before branching to the handler code.
							 */
							inv->ex_handler = clause;
#if DEBUG_INTERP
							if (tracing)
								g_print ("* Found handler at '%s'\n", inv->method->name);
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
		
#if DEBUG_INTERP
		if (tracing)
			g_print ("* Handle finally IL_%04x\n", endfinally_ip == NULL ? 0 : endfinally_ip - header->code);
#endif
		if ((frame->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) 
				|| (frame->method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME))) {
			goto exit_frame;
		}
		ip_offset = frame->ip - header->code;

		if (endfinally_ip != NULL)
			finally_ips = g_slist_prepend(finally_ips, (void *)endfinally_ip);
		for (i = 0; i < header->num_clauses; ++i)
			if (frame->ex_handler == &header->clauses [i])
				break;
		while (i > 0) {
			--i;
			clause = &header->clauses [i];
			if (MONO_OFFSET_IN_CLAUSE (clause, ip_offset) && (endfinally_ip == NULL || !(MONO_OFFSET_IN_CLAUSE (clause, endfinally_ip - header->code)))) {
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
					ip = header->code + clause->handler_offset;
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
		
#if DEBUG_INTERP
		if (tracing)
			g_print ("* Handle fault\n");
#endif
		ip_offset = frame->ip - header->code;
		for (i = 0; i < header->num_clauses; ++i) {
			clause = &header->clauses [i];
			if (clause->flags == MONO_EXCEPTION_CLAUSE_FAULT && MONO_OFFSET_IN_CLAUSE (clause, ip_offset)) {
				ip = header->code + clause->handler_offset;
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
			ip = header->code + frame->ex_handler->handler_offset;
			sp = frame->stack;
			sp->type = VAL_OBJ;
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
	if (context == NULL) {
		context = &context_struct;
		context_struct.base_frame = frame;
		context_struct.current_frame = NULL;
		context_struct.current_env = NULL;
		context_struct.search_for_handler = 0;
		TlsSetValue (thread_context_id, context);
	}
	frame->ip = NULL;
	frame->parent = context->current_frame;
	ves_exec_method_with_context(frame, context);
	if (frame->ex) {
		if (context->current_env) {
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
	MonoImage *image = assembly->image;
	MonoCLIImageInfo *iinfo;
	MonoMethod *method;
	MonoObject *exc = NULL;
	int rval;

	iinfo = image->image_info;
	method = mono_get_method (image, iinfo->cli_cli_header.ch_entry_point, NULL);
	if (!method)
		g_error ("No entry point method found in %s", image->name);

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

static MonoException * segv_exception = NULL;

static void
segv_handler (int signum)
{
	signal (signum, segv_handler);
	mono_raise_exception (segv_exception);
}


static void
quit_handler (int signum)
{
	signal (signum, quit_handler);
	mono_raise_exception (quit_exception);
}

static MonoBoolean
ves_icall_get_frame_info (gint32 skip, MonoBoolean need_file_info, 
			  MonoReflectionMethod **method, 
			  gint32 *iloffset, gint32 *native_offset,
			  MonoString **file, gint32 *line, gint32 *column)
{
	if (iloffset)
		*iloffset = 0;
	if (native_offset)
		*native_offset = 0;
	if (method)
		*method = NULL;
	if (line)
		*line = 0;
	if (column)
		*column = 0;
	if (file)
		*file = mono_string_new (mono_domain_get (), "unknown");

	return TRUE;
}

static MonoArray *
ves_icall_get_trace (MonoException *exc, gint32 skip, MonoBoolean need_file_info)
{
	return NULL;
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
	char *error;

	if (main_args->enable_debugging)
		mono_debug_init (main_args->domain, MONO_DEBUG_FORMAT_MONO);

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
	error = mono_verify_corlib ();
	if (error) {
		fprintf (stderr, "Corlib not in sync with this runtime: %s\n", error);
		exit (1);
	}
	segv_exception = mono_get_exception_null_reference ();
	segv_exception->message = mono_string_new (main_args->domain, "Segmentation fault");
	signal (SIGSEGV, segv_handler);
	/* perhaps we should use a different class for this exception... */
	quit_exception = mono_get_exception_null_reference ();
	quit_exception->message = mono_string_new (main_args->domain, "Quit");
	signal (SIGINT, quit_handler);

	ves_exec (main_args->domain, assembly, main_args->argc, main_args->argv);
#endif
}

static void
mono_runtime_install_handlers (void)
{
	/* FIXME: anything to do here? */
}

static void
quit_function (MonoDomain *domain, gpointer user_data)
{
	mono_profiler_shutdown ();
	
	mono_runtime_cleanup (domain);
	mono_domain_free (domain, TRUE);

}

int 
main (int argc, char *argv [])
{
	MonoDomain *domain;
	int retval = 0, i, ocount = 0;
	char *file, *config_file = NULL;
	int enable_debugging = FALSE;
	MainThreadArgs main_args;
	
	if (argc < 2)
		usage ();

	for (i = 1; i < argc && argv [i][0] == '-'; i++){
		if (strcmp (argv [i], "--trace") == 0)
			global_tracing = 1;
		if (strcmp (argv [i], "--noptr") == 0)
			global_no_pointers = 1;
		if (strcmp (argv [i], "--traceops") == 0)
			global_tracing = 2;
		if (strcmp (argv [i], "--dieonex") == 0) {
			die_on_exception = 1;
			enable_debugging = 1;
		}
		if (strcmp (argv [i], "--print-vtable") == 0)
			mono_print_vtable = TRUE;
		if (strcmp (argv [i], "--profile") == 0)
			mono_profiler_load (NULL);
		if (strcmp (argv [i], "--opcode-count") == 0)
			ocount = 1;
		if (strcmp (argv [i], "--config") == 0)
			config_file = argv [++i];
		if (strcmp (argv [i], "--workers") == 0) {
			mono_max_worker_threads = atoi (argv [++i]);
			if (mono_max_worker_threads < 1)
				mono_max_worker_threads = 1;
		}
		if (strcmp (argv [i], "--help") == 0)
			usage ();
#if DEBUG_INTERP
		if (strcmp (argv [i], "--debug") == 0) {
			MonoMethodDesc *desc = mono_method_desc_new (argv [++i], FALSE);
			if (!desc)
				g_error ("Invalid method name '%s'", argv [i]);
			db_methods = g_list_append (db_methods, desc);
		}
#endif
	}
	
	file = argv [i];

	if (!file)
		usage ();

	g_set_prgname (file);
	mono_set_rootdir ();
	
	g_log_set_always_fatal (G_LOG_LEVEL_ERROR);
	g_log_set_fatal_mask (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR);

	g_thread_init (NULL);

	thread_context_id = TlsAlloc ();
	TlsSetValue (thread_context_id, NULL);
	InitializeCriticalSection(&calc_section);

	mono_install_compile_method (mono_create_method_pointer);
	mono_install_runtime_invoke (interp_mono_runtime_invoke);
	mono_install_remoting_trampoline (interp_create_remoting_trampoline);

	mono_install_handler (interp_ex_handler);
	mono_install_stack_walk (interp_walk_stack);
	mono_runtime_install_cleanup (quit_function);

	domain = mono_init (file);
#ifdef __hpux /* generates very big stack frames */
	mono_threads_set_default_stacksize(32*1024*1024);
#endif
	mono_config_parse (config_file);
	mono_init_icall ();
	mono_add_internal_call ("System.Diagnostics.StackFrame::get_frame_info", ves_icall_get_frame_info);
	mono_add_internal_call ("System.Diagnostics.StackTrace::get_trace", ves_icall_get_trace);
	mono_add_internal_call ("Mono.Runtime::mono_runtime_install_handlers", mono_runtime_install_handlers);

	mono_runtime_init (domain, NULL, NULL);

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

#if DEBUG_INTERP
	if (ocount) {
		fprintf (stderr, "opcode count: %ld\n", opcode_count);
		fprintf (stderr, "fcall count: %ld\n", fcall_count);
	}
#endif
	return retval;
}
