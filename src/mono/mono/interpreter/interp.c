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
 * (C) 2001 Ximian, Inc.
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

#if HAVE_BOEHM_GC
#include <gc/gc.h>
#endif

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
#include <mono/metadata/appdomain.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/socket-io.h>
/*#include <mono/cli/types.h>*/
#include "interp.h"
#include "hacks.h"

#ifdef _WIN32
#define isnan _isnan
#define finite _finite
#endif

typedef struct {
	union {
		GTimer *timer;
		MonoMethod *method;
	} u;
	guint64 count;
	double total;
} MethodProfile;

/* If true, then we output the opcodes as we interpret them */
static int global_tracing = 0;
static int global_class_init_tracing = 0;

static int debug_indent_level = 0;

static GHashTable *profiling = NULL;
static GHashTable *profiling_classes = NULL;

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

#define GET_NATI(sp) ((sp).data.nati)
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
	} while (0)

void ves_exec_method (MonoInvocation *frame);

typedef void (*ICallMethod) (MonoInvocation *frame);

static guint32 die_on_exception = 0;
static guint32 frame_thread_id = 0;

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
		MonoClass *klass = frame->method->klass;	\
		char *args = dump_stack (frame->stack_args, frame->stack_args+signature->param_count);	\
		debug_indent_level++;	\
		output_indent ();	\
		g_print ("(%d) Entering %s.%s::%s (", GetCurrentThreadId(), klass->name_space, klass->name, frame->method->name);	\
		if (signature->hasthis) g_print ("%p ", frame->obj);	\
		g_print ("%s)\n", args);	\
		g_free (args);	\
	}	\
	if (profiling) {	\
		if (!(profile_info = g_hash_table_lookup (profiling, frame->method))) {	\
			profile_info = g_new0 (MethodProfile, 1);	\
			profile_info->u.timer = g_timer_new ();	\
			g_hash_table_insert (profiling, frame->method, profile_info);	\
		}	\
		profile_info->count++;	\
		g_timer_start (profile_info->u.timer);	\
	}

#define DEBUG_LEAVE()	\
	if (tracing) {	\
		MonoClass *klass = frame->method->klass;	\
		output_indent ();	\
		g_print ("(%d) Leaving %s.%s::%s\n", GetCurrentThreadId(), klass->name_space, klass->name, frame->method->name);	\
		debug_indent_level--;	\
	}	\
	if (profiling) {	\
		g_timer_stop (profile_info->u.timer);	\
		profile_info->total += g_timer_elapsed (profile_info->u.timer, NULL);	\
	}

#define DEBUG_CLASS_INIT(klass)	\
	if (global_class_init_tracing) {	\
		output_indent();	\
		g_print("(%d) Init class %s\n", GetCurrentThreadId(),	\
			klass->name);	\
	}

#define DEBUG_CLASS_INIT_END(klass)	\
	if (global_class_init_tracing) {	\
		output_indent();	\
		g_print("(%d) End init class %s\n", GetCurrentThreadId(), \
			klass->name);	\
	}

#else

#define DEBUG_ENTER()
#define DEBUG_LEAVE()
#define DEBUG_CLASS_INIT(klass)
#define DEBUG_CLASS_INIT_END(klass)

#endif

static void
interp_ex_handler (MonoException *ex) {
	MonoInvocation *frame = TlsGetValue (frame_thread_id);
	frame->ex = ex;
	longjmp (*(jmp_buf*)frame->locals, 1);
}

static void
runtime_object_init (MonoObject *obj)
{
	int i;
	MonoInvocation call;
	MonoMethod *method = NULL;
	MonoClass *klass = obj->vtable->klass;

	for (i = 0; i < klass->method.count; ++i) {
		if (!strcmp (".ctor", klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == 0) {
			method = klass->methods [i];
			break;
		}
	}

	call.obj = obj;

	g_assert (method);
	INIT_FRAME (&call, NULL, obj, NULL, NULL, method);

	ves_exec_method (&call);
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

/*
 * runtime_class_init:
 * @klass: klass that needs to be initialized
 *
 * This routine calls the class constructor for @class.
 */
static void
runtime_class_init (MonoClass *klass)
{
	MonoMethod *method;
	MonoInvocation call;
	int i;

	DEBUG_CLASS_INIT(klass);
	for (i = 0; i < klass->method.count; ++i) {
		method = klass->methods [i];
		if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && 
		    (strcmp (".cctor", method->name) == 0)) {
			INIT_FRAME (&call, NULL, NULL, NULL, NULL, method);
			ves_exec_method (&call);
			DEBUG_CLASS_INIT_END(klass);
			return;
		}
	}
	DEBUG_CLASS_INIT_END(klass);
	/* No class constructor found */
}

static MonoMethod*
get_virtual_method (MonoDomain *domain, MonoMethod *m, stackval *objs)
{
	MonoObject *obj;
	MonoClass *klass;
	MonoMethod **vtable;

	if ((m->flags & METHOD_ATTRIBUTE_FINAL) || !(m->flags & METHOD_ATTRIBUTE_VIRTUAL))
			return m;

	mono_class_init (m->klass);
	g_assert (m->klass->inited);

	obj = objs->data.p;
	klass = obj->vtable->klass;
	vtable = (MonoMethod **)obj->vtable->vtable;

	if (m->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		return *(MonoMethod**)(obj->vtable->interface_offsets [m->klass->interface_id] + (m->slot<<2));
	}

	g_assert (vtable [m->slot]);

	return vtable [m->slot];
}

void inline
stackval_from_data (MonoType *type, stackval *result, const char *data)
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
			result->type = VAL_VALUETA;
			break;
		}
		result->data.p = *(gpointer*)data;
		result->data.vt.klass = mono_class_from_mono_type (type);
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
		result->data.vt.klass = mono_class_from_mono_type (type);
		return;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			return stackval_from_data (type->data.klass->enum_basetype, result, data);
		} else {
			result->type = VAL_VALUET;
			result->data.vt.klass = type->data.klass;
			memcpy (result->data.vt.vt, data, mono_class_value_size (type->data.klass, NULL));
		}
		return;
	default:
		g_warning ("got type 0x%02x", type->type);
		g_assert_not_reached ();
	}
}

static void inline
stackval_to_data (MonoType *type, stackval *val, char *data)
{
	if (type->byref) {
		gpointer *p = (gpointer*)data;
		*p = val->data.p;
		return;
	}
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
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I4:
	case MONO_TYPE_U4: {
		gint32 *p = (gint32*)data;
		*p = val->data.i;
		return;
	}
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
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
			return stackval_to_data (type->data.klass->enum_basetype, val, data);
		} else {
			memcpy (data, val->data.vt.vt, mono_class_value_size (type->data.klass, NULL));
		}
		return;
	default:
		g_warning ("got type %x", type->type);
		g_assert_not_reached ();
	}
}

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

	pos = sp [0].data.i - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++) {
		if ((t = sp [i].data.i - ao->bounds [i].lower_bound) >= 
		    ao->bounds [i].length) {
			g_warning ("wrong array index");
			g_assert_not_reached ();
		}
		pos = pos*ao->bounds [i].length + sp [i].data.i - 
			ao->bounds [i].lower_bound;
	}

	esize = mono_array_element_size (ac);
	ea = mono_array_addr_with_size (ao, esize, pos);

	mt = frame->method->signature->params [ac->rank];
	stackval_to_data (mt, &sp [ac->rank], ea);
}

static void 
ves_array_get (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoObject *o;
	MonoArray *ao;
	MonoClass *ac;
	gint32 i, pos, esize;
	gpointer ea;
	MonoType *mt;

	o = frame->obj;
	ao = (MonoArray *)o;
	ac = o->vtable->klass;

	g_assert (ac->rank >= 1);

	pos = sp [0].data.i - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + sp [i].data.i - 
			ao->bounds [i].lower_bound;

	esize = mono_array_element_size (ac);
	ea = mono_array_addr_with_size (ao, esize, pos);

	mt = frame->method->signature->ret;
	stackval_from_data (mt, frame->retval, ea);
}

static void
ves_array_element_address (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoObject *o;
	MonoArray *ao;
	MonoClass *ac;
	gint32 i, pos, esize;
	gpointer ea;

	o = frame->obj;
	ao = (MonoArray *)o;
	ac = o->vtable->klass;

	g_assert (ac->rank >= 1);

	pos = sp [0].data.i - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + sp [i].data.i - 
			ao->bounds [i].lower_bound;

	esize = mono_array_element_size (ac);
	ea = mono_array_addr_with_size (ao, esize, pos);

	frame->retval->type = VAL_TP;
	frame->retval->data.p = ea;
}

static void 
ves_pinvoke_method (MonoInvocation *frame)
{
	jmp_buf env;
	MonoPIFunc func;
	
	if (setjmp(env)) {
		TlsSetValue (frame_thread_id, frame->args);
		return;
	}
	if (!frame->method->info)
		frame->method->info = mono_create_trampoline (frame->method, 0);
	func = (MonoPIFunc)frame->method->info;

	/* 
	 * frame->locals and args are unused for P/Invoke methods, so we reuse them. 
	 * locals will point to the jmp_buf, while args will point to the previous
	 * MonoInvocation frame: this is needed to make exception searching work across
	 * managed/unmanaged boundaries.
	 */
	frame->locals = (char*)&env;
	frame->args = (char*)TlsGetValue (frame_thread_id);
	TlsSetValue (frame_thread_id, frame);

	func ((MonoFunc)frame->method->addr, &frame->retval->data.p, frame->obj, frame->stack_args);
	stackval_from_data (frame->method->signature->ret, frame->retval, (const char*)&frame->retval->data.p);
	TlsSetValue (frame_thread_id, frame->args);
}

/*
 * From the spec:
 * runtime specifies that the implementation of the method is automatically
 * provided by the runtime and is primarily used for the methods of delegates.
 */
static void
ves_runtime_method (MonoInvocation *frame)
{
	const char *name = frame->method->name;
	MonoObject *obj = (MonoObject*)frame->obj;
	MonoMulticastDelegate *delegate = (MonoMulticastDelegate*)frame->obj;
	MonoInvocation call;

	mono_class_init (mono_defaults.multicastdelegate_class);
	
	if (*name == '.' && (strcmp (name, ".ctor") == 0) && obj &&
			mono_object_isinst (obj, mono_defaults.multicastdelegate_class)) {
		delegate->delegate.target = frame->stack_args[0].data.p;
		delegate->delegate.method_ptr = frame->stack_args[1].data.p;
		if (!delegate->delegate.target) {
			MonoDomain *domain = mono_domain_get ();
			MonoMethod *m = mono_method_pointer_get (delegate->delegate.method_ptr);
			delegate->delegate.method_info = mono_method_get_object (domain, m);
		}
		return;
	}
	if (*name == 'I' && (strcmp (name, "Invoke") == 0) && obj &&
			mono_object_isinst (obj, mono_defaults.multicastdelegate_class)) {
		guchar *code;
		MonoMethod *method;
		
		// FIXME: support multicast delegates 
		g_assert (!delegate->prev);

		code = (guchar*)delegate->delegate.method_ptr;
		method = mono_method_pointer_get (code);
#if 1
		/* FIXME: check for NULL method */
		INIT_FRAME(&call,frame,delegate->delegate.target,frame->stack_args,frame->retval,method);
		ves_exec_method (&call);
#else
		if (!method->addr)
			method->addr = mono_create_trampoline (method, 1);
		func = method->addr;
		/* FIXME: need to handle exceptions across managed/unmanaged boundaries */
		func ((MonoFunc)delegate->method_ptr, &frame->retval->data.p, delegate->target, frame->stack_args);
#endif
		stackval_from_data (frame->method->signature->ret, frame->retval, (const char*)&frame->retval->data.p);
		return;
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
		case VAL_I32: g_string_sprintfa (str, "[%d] ", s->data.i); break;
		case VAL_I64: g_string_sprintfa (str, "[%lld] ", s->data.l); break;
		case VAL_DOUBLE: g_string_sprintfa (str, "[%0.5f] ", s->data.f); break;
		case VAL_VALUET: g_string_sprintfa (str, "[vt: %p] ", s->data.vt.vt); break;
#if 0
		case VAL_OBJ: {
			MonoObject *obj =  s->data.p;
			if (obj && obj->klass == mono_defaults.string_class) {
				char *str = mono_string_to_utf8 ((MonoString*)obj);
				printf ("\"%s\" ", str);
				g_free (str);
				break;
			}
		}
#endif
		default: g_string_sprintfa (str, "[%p] ", s->data.p); break;
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
		MonoClass *k = inv->method->klass;
		int codep;
		const char * opname;
		if (inv->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL ||
				inv->method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
			codep = 0;
			opname = "";
		} else {
			MonoMethodHeader *hd = ((MonoMethodNormal *)inv->method)->header;
			if (inv->ip)
				codep = *(inv->ip) == 0xfe? inv->ip [1] + 256: *(inv->ip);
			else
				codep = 0;
			opname = mono_opcode_names [codep];
			codep = inv->ip - hd->code;
		}
		args = dump_stack (inv->stack_args, inv->stack_args + inv->method->signature->param_count);
		g_string_sprintfa (str, "#%d: 0x%05x %-10s in %s.%s::%s (%s)\n", i, codep, opname,
						k->name_space, k->name, inv->method->name, args);
		g_free (args);
	}
	return g_string_free (str, FALSE);
}

static guint32*
calc_offsets (MonoImage *image, MonoMethodHeader *header, MonoMethodSignature *signature)
{
	int i, align, size, offset = 0;
	int hasthis = signature->hasthis;
	register const unsigned char *ip, *end;
	const MonoOpcode *opcode;
	guint32 *offsets = g_new0 (guint32, 2 + header->num_locals + signature->param_count + signature->hasthis);

	for (i = 0; i < header->num_locals; ++i) {
		size = mono_type_size (header->locals [i], &align);
		offset += align - 1;
		offset &= ~(align - 1);
		offsets [2 + i] = offset;
		offset += size;
	}
	offsets [0] = offset;
	offset = 0;
	if (hasthis) {
		offset += sizeof (gpointer) - 1;
		offset &= ~(sizeof (gpointer) - 1);
		offsets [2 + header->num_locals] = offset;
		offset += sizeof (gpointer);
	}
	for (i = 0; i < signature->param_count; ++i) {
		size = mono_type_size (signature->params [i], &align);
		offset += align - 1;
		offset &= ~(align - 1);
		offsets [2 + hasthis + header->num_locals + i] = offset;
		offset += size;
	}
	offsets [1] = offset;

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
			mono_ldstr (mono_domain_get (), image, mono_metadata_token_index (read32 (ip + 1)));
			/* fall through */
		case MonoInlineType:
		case MonoInlineField:
		case MonoInlineTok:
		case MonoInlineSig:
		case MonoInlineMethod:
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
			gint32 n;
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
	return offsets;
}

#define LOCAL_POS(n)            (frame->locals + offsets [2 + (n)])
#define LOCAL_TYPE(header, n)   ((header)->locals [(n)])

#define ARG_POS(n)              (args_pointers [(n)])
#define ARG_TYPE(sig, n)        ((n) ? (sig)->params [(n) - (sig)->hasthis] : \
				(sig)->hasthis ? &frame->method->klass->this_arg: (sig)->params [(0)])

#define THROW_EX(exception,ex_ip)	\
		do {\
			char *stack_trace = dump_frame (frame);	\
			frame->ip = (ex_ip);		\
			frame->ex = (MonoException*)(exception);	\
			frame->ex->stack_trace = mono_string_new (domain, stack_trace);	\
			g_free (stack_trace);	\
			goto handle_exception;	\
		} while (0)

typedef struct _vtallocation vtallocation;

struct _vtallocation {
	vtallocation *next;
	guint32 size;
	char data [MONO_ZERO_LEN_ARRAY];
};

/*
 * we don't use vtallocation->next, yet
 */
#define vt_alloc(vtype,sp)	\
	if ((vtype)->type == MONO_TYPE_VALUETYPE && !(vtype)->data.klass->enumtype) {	\
		if (!(vtype)->byref) {	\
			guint32 align;	\
			guint32 size = mono_class_value_size ((vtype)->data.klass, &align);	\
			if (!vtalloc || vtalloc->size <= size) {	\
				vtalloc = alloca (sizeof (vtallocation) + size);	\
				vtalloc->size = size;	\
				g_assert (size < 10000);	\
			}	\
			(sp)->data.vt.vt = vtalloc->data;	\
			vtalloc = NULL;	\
		} else {	\
			(sp)->data.vt.klass = (vtype)->data.klass;	\
		}	\
	}

#define vt_free(sp)	\
	do {	\
		if ((sp)->type == VAL_VALUET) {	\
			vtalloc = (vtallocation*)(((char*)(sp)->data.vt.vt) - G_STRUCT_OFFSET (vtallocation, data));	\
		}	\
	} while (0)

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

#define MYGUINT64_MAX 18446744073709551615UL
#define MYGINT64_MAX 9223372036854775807LL
#define MYGINT64_MIN (-MYGINT64_MAX -1LL)

#define MYGUINT32_MAX 4294967295U
#define MYGINT32_MAX 2147483647
#define MYGINT32_MIN (-MYGINT32_MAX -1)
	
#define CHECK_ADD_OVERFLOW(a,b) \
	(gint32)(b) >= 0 ? (gint32)(MYGINT32_MAX) - (gint32)(b) < (gint32)(a) ? -1 : 0	\
	: (gint32)(MYGINT32_MIN) - (gint32)(b) > (gint32)(a) ? +1 : 0

#define CHECK_ADD_OVERFLOW_UN(a,b) \
	(guint32)(MYGUINT32_MAX) - (guint32)(b) < (guint32)(a) ? -1 : 0

#define CHECK_ADD_OVERFLOW64(a,b) \
	(gint64)(b) >= 0 ? (gint64)(MYGINT64_MAX) - (gint64)(b) < (gint64)(a) ? -1 : 0	\
	: (gint64)(MYGINT64_MIN) - (gint64)(b) > (gint64)(a) ? +1 : 0

#define CHECK_ADD_OVERFLOW64_UN(a,b) \
	(guint64)(MYGUINT64_MAX) - (guint64)(b) < (guint64)(a) ? -1 : 0

static MonoObject*
interp_mono_runtime_invoke (MonoMethod *method, void *obj, void **params)
{
	MonoInvocation frame;
	MonoObject *retval = NULL;
	MonoMethodSignature *sig = method->signature;
	MonoClass *klass = mono_class_from_mono_type (sig->ret);
	int i, type, isobject = 0;
	void *ret;
	stackval result;
	stackval *args = alloca (sizeof (stackval) * sig->param_count);

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
			result.data.vt.vt = ret;
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
			args [i].data.vt.klass = NULL;
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
			args [i].type = VAL_I32;
			args [i].data.i = *(gint16*)params [i];
			args [i].data.vt.klass = NULL;
			break;
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_U: /* use VAL_POINTER? */
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
			args [i].type = VAL_I32;
			args [i].data.i = *(gint32*)params [i];
			args [i].data.vt.klass = NULL;
			break;
#if SIZEOF_VOID_P == 8
		case MONO_TYPE_U:
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			args [i].type = VAL_I64;
			args [i].data.l = *(gint64*)params [i];
			args [i].data.vt.klass = NULL;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				type = sig->params [i]->data.klass->enum_basetype->type;
				goto handle_enum;
			} else {
				g_warning ("generic valutype %s not handled in runtime invoke", sig->params [i]->data.klass->name);
			}
			break;
		case MONO_TYPE_STRING:
			args [i].type = VAL_OBJ;
			args [i].data.p = params [i];
			args [i].data.vt.klass = NULL;
			break;
		default:
			g_error ("type 0x%x not handled in invoke", sig->params [i]->type);
		}
	}

	INIT_FRAME(&frame,NULL,obj,args,&result,method);
	ves_exec_method (&frame);
	if (sig->ret->type == MONO_TYPE_VOID)
		return NULL;
	if (isobject)
		return result.data.p;
	stackval_to_data (sig->ret, &result, ret);
	return retval;
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
void 
ves_exec_method (MonoInvocation *frame)
{
	MonoDomain *domain = mono_domain_get (); 	
	MonoInvocation child_frame;
	MonoMethodHeader *header;
	MonoMethodSignature *signature;
	MonoImage *image;
	const unsigned char *endfinally_ip;
	register const unsigned char *ip;
	register stackval *sp;
	void **args_pointers;
	guint32 *offsets;
	gint il_ins_count = -1;
	gint tracing = global_tracing;
	unsigned char tail_recursion = 0;
	unsigned char unaligned_address = 0;
	unsigned char volatile_address = 0;
	vtallocation *vtalloc = NULL;
	MethodProfile *profile_info;
	GOTO_LABEL_VARS;

	signature = frame->method->signature;
	mono_class_init (frame->method->klass);

	DEBUG_ENTER ();

	if (frame->method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		if (!frame->method->addr) {
			frame->ex = (MonoException*)mono_get_exception_missing_method ();
			DEBUG_LEAVE ();
			return;
		}
		if (frame->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
			ves_pinvoke_method (frame);
		} else {
			ICallMethod icall = (ICallMethod)frame->method->addr;
			icall (frame);
		}
		if (frame->ex)
			goto handle_exception;
		DEBUG_LEAVE ();
		return;
	} 

	if (frame->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		if (!frame->method->addr) {
			frame->ex = (MonoException*)mono_get_exception_missing_method ();
			DEBUG_LEAVE ();
			return;
		}
		ves_pinvoke_method (frame);
		if (frame->ex)
			goto handle_exception;
		DEBUG_LEAVE ();
		return;
	} 

	if (frame->method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
		ves_runtime_method (frame);
		if (frame->ex)
			goto handle_exception;
		DEBUG_LEAVE ();
		return;
	} 

	/*verify_method (frame->method);*/

	header = ((MonoMethodNormal *)frame->method)->header;
	image = frame->method->klass->image;

	/*
	 * with alloca we get the expected huge performance gain
	 * stackval *stack = g_new0(stackval, header->max_stack);
	 */
	g_assert (header->max_stack < 10000);
	sp = frame->stack = alloca (sizeof (stackval) * header->max_stack);

	if (!frame->method->info)
		frame->method->info = calc_offsets (image, header, signature);
	offsets = frame->method->info;

	if (header->num_locals) {
		g_assert (offsets [0] < 10000);
		frame->locals = alloca (offsets [0]);
		/* 
		 * yes, we do it unconditionally, because it needs to be done for
		 * some cases anyway and checking for that would be even slower.
		 */
		memset (frame->locals, 0, offsets [0]);
	}
	/*
	 * Copy args from stack_args to args.
	 */
	if (signature->param_count || signature->hasthis) {
		int i;
		int has_this = signature->hasthis;

		g_assert (offsets [1] < 10000);
		frame->args = alloca (offsets [1]);
		g_assert ((signature->param_count + has_this) < 1000);
		args_pointers = alloca (sizeof(void*) * (signature->param_count + has_this));
		if (has_this) {
			gpointer *this_arg;
			this_arg = args_pointers [0] = frame->args;
			*this_arg = frame->obj;
		}
		for (i = 0; i < signature->param_count; ++i) {
			args_pointers [i + has_this] = frame->args + offsets [2 + header->num_locals + has_this + i];
			stackval_to_data (signature->params [i], frame->stack_args + i, args_pointers [i + has_this]);
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
			char *ins;
			if (sp > frame->stack) {
				output_indent ();
				ins = dump_stack (frame->stack, sp);
				g_print ("(%d) stack: %s\n", GetCurrentThreadId(), ins);
				g_free (ins);
			}
			output_indent ();
			ins = mono_disasm_code_one (NULL, frame->method, ip);
			g_print ("(%d) %s", GetCurrentThreadId(), ins);
			g_free (ins);
		}
		if (il_ins_count > 0)
			if (!(--il_ins_count))
				G_BREAKPOINT ();
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
			vt_alloc (ARG_TYPE (signature, n), sp);
			stackval_from_data (ARG_TYPE (signature, n), sp, ARG_POS (n));
			++sp;
			BREAK;
		}
		CASE (CEE_LDLOC_0)
		CASE (CEE_LDLOC_1)
		CASE (CEE_LDLOC_2)
		CASE (CEE_LDLOC_3) {
			int n = (*ip)-CEE_LDLOC_0;
			++ip;
			if ((LOCAL_TYPE (header, n))->type == MONO_TYPE_I4) {
				sp->type = VAL_I32;
				sp->data.i = *(gint32*) LOCAL_POS (n);
				++sp;
				BREAK;
			} else {
				vt_alloc (LOCAL_TYPE (header, n), sp);
				stackval_from_data (LOCAL_TYPE (header, n), sp, LOCAL_POS (n));
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
				stackval_to_data (LOCAL_TYPE (header, n), sp, LOCAL_POS (n));
				vt_free (sp);
				BREAK;
			}
		}
		CASE (CEE_LDARG_S)
			++ip;
			vt_alloc (ARG_TYPE (signature, *ip), sp);
			stackval_from_data (ARG_TYPE (signature, *ip), sp, ARG_POS (*ip));
			++sp;
			++ip;
			BREAK;
		CASE (CEE_LDARGA_S) {
			MonoType *t;
			MonoClass *c;

			++ip;
			t = ARG_TYPE (signature, *ip);
			c = mono_class_from_mono_type (t);
			sp->data.vt.klass = c;
			sp->data.vt.vt = ARG_POS (*ip);

			if (c->valuetype)
				sp->type = VAL_VALUETA;
			else
				sp->type = VAL_TP;

			++sp;
			++ip;
			BREAK;
		}
		CASE (CEE_STARG_S)
			++ip;
			--sp;
			stackval_to_data (ARG_TYPE (signature, *ip), sp, ARG_POS (*ip));
			vt_free (sp);
			++ip;
			BREAK;
		CASE (CEE_LDLOC_S)
			++ip;
			vt_alloc (LOCAL_TYPE (header, *ip), sp);
			stackval_from_data (LOCAL_TYPE (header, *ip), sp, LOCAL_POS (*ip));
			++ip;
			++sp;
			BREAK;
		CASE (CEE_LDLOCA_S) {
			MonoType *t;
			MonoClass *c;

			++ip;
			t = LOCAL_TYPE (header, *ip);
			c =  mono_class_from_mono_type (t);
			sp->data.vt.klass = c;
			sp->data.p = LOCAL_POS (*ip);

			if (c->valuetype)
				sp->type = VAL_VALUETA;
			else 
				sp->type = VAL_TP;

			++sp;
			++ip;
			BREAK;
		}
		CASE (CEE_STLOC_S)
			++ip;
			--sp;
			stackval_to_data (LOCAL_TYPE (header, *ip), sp, LOCAL_POS (*ip));
			vt_free (sp);
			++ip;
			BREAK;
		CASE (CEE_LDNULL) 
			++ip;
			sp->type = VAL_OBJ;
			sp->data.p = NULL;
			sp->data.vt.klass = NULL;
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
			sp->data.i = *(gint8 *)ip;
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
			if (sp [-1].type == VAL_VALUET) {
				MonoClass *c = sp [-1].data.vt.klass;
				vt_alloc (&c->byval_arg, sp);
				stackval_from_data (&c->byval_arg, sp, sp [-1].data.vt.vt);
			} else {
				*sp = sp [-1]; 
			}
			++sp; 
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

			/*
			 * We ignore tail recursion for now.
			 */
			tail_recursion = 0;

			frame->ip = ip;
			
			++ip;
			token = read32 (ip);
			ip += 4;
			if (calli) {
				unsigned char *code;
				--sp;
				code = sp->data.p;
				child_frame.method = mono_method_pointer_get (code);
				/* check for NULL with native code */
				csignature = child_frame.method->signature;
			} else {
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
			g_assert (csignature->call_convention == MONO_CALL_DEFAULT);
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
				 * g_assert (sp->type == VAL_OBJ || sp->type == VAL_VALUETA);
				 */
				if (sp->type == VAL_OBJ && child_frame.method->klass->valuetype) /* unbox it */
					child_frame.obj = (char*)sp->data.p + sizeof (MonoObject);
				else
					child_frame.obj = sp->data.p;
			} else {
				child_frame.obj = NULL;
			}
			if (csignature->ret->type != MONO_TYPE_VOID) {
				vt_alloc (csignature->ret, &retval);
				child_frame.retval = &retval;
			} else {
				child_frame.retval = NULL;
			}

			child_frame.ex = NULL;
			child_frame.ex_handler = NULL;

			ves_exec_method (&child_frame);

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
					stackval_from_data (signature->ret, frame->retval, sp->data.vt.vt);
					vt_free (sp);
				} else {
					*frame->retval = *sp;
				}
			}
			if (sp > frame->stack)
				g_warning ("more values on stack: %d", sp-frame->stack);

			DEBUG_LEAVE ();
			return;
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
			int near_jump = *ip == CEE_BRFALSE_S;
			++ip;
			--sp;
			switch (sp->type) {
			case VAL_I32: result = sp->data.i == 0; break;
			case VAL_I64: result = sp->data.l == 0; break;
			case VAL_DOUBLE: result = sp->data.f ? 0: 1; break;
			default: result = sp->data.p == NULL; break;
			}
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
			BREAK;
		}
		CASE (CEE_BRTRUE) /* Fall through */
		CASE (CEE_BRTRUE_S) {
			int result;
			int near_jump = *ip == CEE_BRTRUE_S;
			++ip;
			--sp;
			switch (sp->type) {
			case VAL_I32: result = sp->data.i != 0; break;
			case VAL_I64: result = sp->data.l != 0; break;
			case VAL_DOUBLE: result = sp->data.f ? 1 : 0; break;
			default: result = sp->data.p != NULL; break;
			}
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
			BREAK;
		}
		CASE (CEE_BEQ) /* Fall through */
		CASE (CEE_BEQ_S) {
			int result;
			int near_jump = *ip == CEE_BEQ_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = sp [0].data.i == (gint)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l == sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f == sp [1].data.f;
			else
				result = (gint)GET_NATI (sp [0]) == (gint)GET_NATI (sp [1]);
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
			BREAK;
		}
		CASE (CEE_BGE) /* Fall through */
		CASE (CEE_BGE_S) {
			int result;
			int near_jump = *ip == CEE_BGE_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = sp [0].data.i >= (gint)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l >= sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f >= sp [1].data.f;
			else
				result = (gint)GET_NATI (sp [0]) >= (gint)GET_NATI (sp [1]);
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
			BREAK;
		}
		CASE (CEE_BGT) /* Fall through */
		CASE (CEE_BGT_S) {
			int result;
			int near_jump = *ip == CEE_BGT_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = sp [0].data.i > (gint)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l > sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f > sp [1].data.f;
			else
				result = (gint)GET_NATI (sp [0]) > (gint)GET_NATI (sp [1]);
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
			BREAK;
		}
		CASE (CEE_BLT) /* Fall through */
		CASE (CEE_BLT_S) {
			int result;
			int near_jump = *ip == CEE_BLT_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = sp[0].data.i < (gint)GET_NATI(sp[1]);
			else if (sp->type == VAL_I64)
				result = sp[0].data.l < sp[1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp[0].data.f < sp[1].data.f;
			else
				result = (gint)GET_NATI(sp[0]) < (gint)GET_NATI(sp[1]);
			if (result) {
				if (near_jump)
					ip += 1 + (signed char)*ip;
				else
					ip += 4 + (gint32) read32 (ip);
				BREAK;
			} else {
				ip += near_jump ? 1: 4;
				BREAK;
			}
		}
		CASE (CEE_BLE) /* fall through */
		CASE (CEE_BLE_S) {
			int result;
			int near_jump = *ip == CEE_BLE_S;
			++ip;
			sp -= 2;

			if (sp->type == VAL_I32)
				result = sp [0].data.i <= (gint)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l <= sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f <= sp [1].data.f;
			else {
				/*
				 * FIXME: here and in other places GET_NATI on the left side 
				 * _will_ be wrong when we change the macro to work on 64 bits 
				 * systems.
				 */
				result = (gint)GET_NATI (sp [0]) <= (gint)GET_NATI (sp [1]);
			}
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
			BREAK;
		}
		CASE (CEE_BNE_UN) /* Fall through */
		CASE (CEE_BNE_UN_S) {
			int result;
			int near_jump = *ip == CEE_BNE_UN_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (guint32)sp [0].data.i != (guint32)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l != (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = isunordered (sp [0].data.f, sp [1].data.f) ||
					(sp [0].data.f != sp [1].data.f);
			else
				result = GET_NATI (sp [0]) != GET_NATI (sp [1]);
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
			BREAK;
		}
		CASE (CEE_BGE_UN) /* Fall through */
		CASE (CEE_BGE_UN_S) {
			int result;
			int near_jump = *ip == CEE_BGE_UN_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (guint32)sp [0].data.i >= (guint32)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l >= (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = !isless (sp [0].data.f,sp [1].data.f);
			else
				result = GET_NATI (sp [0]) >= GET_NATI (sp [1]);
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
			BREAK;
		}
		CASE (CEE_BGT_UN) /* Fall through */
		CASE (CEE_BGT_UN_S) {
			int result;
			int near_jump = *ip == CEE_BGT_UN_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (guint32)sp [0].data.i > (guint32)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l > (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = isgreater (sp [0].data.f, sp [1].data.f);
			else
				result = GET_NATI (sp [0]) > GET_NATI (sp [1]);
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
			BREAK;
		}
		CASE (CEE_BLE_UN) /* Fall through */
		CASE (CEE_BLE_UN_S) {
			int result;
			int near_jump = *ip == CEE_BLE_UN_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (guint32)sp [0].data.i <= (guint32)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l <= (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = islessequal (sp [0].data.f, sp [1].data.f);
			else
				result = GET_NATI (sp [0]) <= GET_NATI (sp [1]);
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
			BREAK;
		}
		CASE (CEE_BLT_UN) /* Fall through */
		CASE (CEE_BLT_UN_S) {
			int result;
			int near_jump = *ip == CEE_BLT_UN_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (guint32)sp[0].data.i < (guint32)GET_NATI(sp[1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp[0].data.l < (guint64)sp[1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = isunordered (sp [0].data.f, sp [1].data.f) ||
					(sp [0].data.f < sp [1].data.f);
			else
				result = GET_NATI(sp[0]) < GET_NATI(sp[1]);
			if (result) {
				if (near_jump)
					ip += (signed char)*ip;
				else
					ip += (gint32) read32 (ip);
			}
			ip += near_jump ? 1: 4;
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
			sp[-1].data.vt.klass = NULL;
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
			if (sp->type == VAL_I32)
				sp [-1].data.i += GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l += sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f += sp [0].data.f;
			else {
				char *p = sp [-1].data.p;
				p += GET_NATI (sp [0]);
				sp [-1].data.p = p;
			}
			BREAK;
		CASE (CEE_SUB)
			++ip;
			--sp;
			/* should probably consider the pointers as unsigned */
			if (sp->type == VAL_I32)
				sp [-1].data.i -= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l -= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f -= sp [0].data.f;
			else {
				char *p = sp [-1].data.p;
				p -= GET_NATI (sp [0]);
				sp [-1].data.p = p;
			}
			BREAK;
		CASE (CEE_MUL)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i *= (gint)GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l *= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f *= sp [0].data.f;
			BREAK;
		CASE (CEE_DIV)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				sp [-1].data.i /= (gint)GET_NATI (sp [0]);
			} else if (sp->type == VAL_I64) {
				if (sp [0].data.l == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				sp [-1].data.l /= sp [0].data.l;
			} else if (sp->type == VAL_DOUBLE) {
				/* set NaN is divisor is 0.0 */
				sp [-1].data.f /= sp [0].data.f;
			}
			BREAK;
		CASE (CEE_DIV_UN)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				guint32 val;
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				val = sp [-1].data.i;
				val /= (guint32)GET_NATI (sp [0]);
				sp [-1].data.i = val;
			} else if (sp->type == VAL_I64) {
				guint64 val;
				if (sp [0].data.l == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				val = sp [-1].data.l;
				val /= (guint64)sp [0].data.l;
				sp [-1].data.l = val;
			} else if (sp->type == VAL_NATI) {
				mono_u val;
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				val = (mono_u)sp [-1].data.p;
				val /= (guint64)sp [0].data.p;
				sp [-1].data.p = (gpointer)val;
			}
			BREAK;
		CASE (CEE_REM)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				sp [-1].data.i %= (gint)GET_NATI (sp [0]);
			} else if (sp->type == VAL_I64) {
				if (sp [0].data.l == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				sp [-1].data.l %= sp [0].data.l;
			} else if (sp->type == VAL_DOUBLE) {
				/* FIXME: what do we actually do here? */
				sp [-1].data.f = fmod (sp [-1].data.f, sp [0].data.f);
			} else {
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				(gint)GET_NATI (sp [-1]) %= (gint)GET_NATI (sp [0]);
			}
			BREAK;
		CASE (CEE_REM_UN)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				(guint)sp [-1].data.i %= (guint)GET_NATI (sp [0]);
			} else if (sp->type == VAL_I64) {
				if (sp [0].data.l == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				(guint64)sp [-1].data.l %= (guint64)sp [0].data.l;
			} else if (sp->type == VAL_DOUBLE) {
				/* unspecified behaviour according to the spec */
			} else {
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				(guint64)GET_NATI (sp [-1]) %= (guint64)GET_NATI (sp [0]);
			}
			BREAK;
		CASE (CEE_AND)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i &= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l &= sp [0].data.l;
			else
				GET_NATI (sp [-1]) &= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_OR)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i |= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l |= sp [0].data.l;
			else
				GET_NATI (sp [-1]) |= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_XOR)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i ^= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l ^= sp [0].data.l;
			else
				GET_NATI (sp [-1]) ^= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHL)
			++ip;
			--sp;
			if (sp [-1].type == VAL_I32)
				sp [-1].data.i <<= GET_NATI (sp [0]);
			else if (sp [-1].type == VAL_I64)
				sp [-1].data.l <<= GET_NATI (sp [0]);
			else
				GET_NATI (sp [-1]) <<= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHR)
			++ip;
			--sp;
			if (sp [-1].type == VAL_I32)
				sp [-1].data.i >>= GET_NATI (sp [0]);
			else if (sp [-1].type == VAL_I64)
				sp [-1].data.l >>= GET_NATI (sp [0]);
			else
				(gint)GET_NATI (sp [-1]) >>= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHR_UN)
			++ip;
			--sp;
			if (sp [-1].type == VAL_I32)
				(guint)sp [-1].data.i >>= GET_NATI (sp [0]);
			else if (sp [-1].type == VAL_I64)
				(guint64)sp [-1].data.l >>= GET_NATI (sp [0]);
			else
				(guint64)GET_NATI (sp [-1]) >>= GET_NATI (sp [0]);
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
				sp->data.p = (gpointer)(- (mono_i)sp->data.p);
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
				sp->data.p = (gpointer)(~ (mono_i)sp->data.p);
			++sp;
			BREAK;
		CASE (CEE_CONV_U1) /* fall through */
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
		CASE (CEE_CONV_U2) /* fall through */
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
			sp [-1].type = VAL_I64;
			BREAK;
		CASE (CEE_CONV_R4) /* Fall through */
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
				sp [-1].data.l = (guint64) sp [-1].data.i;
				break;
			default:
				sp [-1].data.l = (guint64) sp [-1].data.nati;
				break;
			}
			sp [-1].type = VAL_I64;
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
			c = mono_class_get (image, token);
			addr = sp [-1].data.vt.vt;
			vt_alloc (&c->byval_arg, &sp [-1]);
			stackval_from_data (&c->byval_arg, &sp [-1], addr);
			BREAK;
		}
		CASE (CEE_LDSTR) {
			MonoObject *o;
			guint32 index;

			ip++;
			index = mono_metadata_token_index (read32 (ip));
			ip += 4;

			o = (MonoObject*)mono_ldstr (domain, image, index);
			sp->type = VAL_OBJ;
			sp->data.p = o;
			sp->data.vt.klass = NULL;

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

			frame->ip = ip;

			ip++;
			token = read32 (ip);
			ip += 4;

			if (!(child_frame.method = mono_get_method (image, token, NULL)))
				THROW_EX (mono_get_exception_missing_method (), ip -5);

			csig = child_frame.method->signature;
			newobj_class = child_frame.method->klass;
			if (profiling_classes) {
				guint count = GPOINTER_TO_UINT (g_hash_table_lookup (profiling_classes, newobj_class));
				count++;
				g_hash_table_insert (profiling_classes, newobj_class, GUINT_TO_POINTER (count));
			}
				

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
				vt_alloc (&newobj_class->byval_arg, &valuetype_this);
				if (!newobj_class->enumtype && (newobj_class->byval_arg.type == MONO_TYPE_VALUETYPE)) {
					zero = valuetype_this.data.vt.vt;
					child_frame.obj = valuetype_this.data.vt.vt;
				} else {
					memset (&valuetype_this, 0, sizeof (stackval));
					zero = &valuetype_this;
					child_frame.obj = &valuetype_this;
				}
				stackval_from_data (&newobj_class->byval_arg, &valuetype_this, zero);
			} else {
				o = mono_object_new (domain, newobj_class);
				child_frame.obj = o;
			}

			if (csig->param_count) {
				sp -= csig->param_count;
				child_frame.stack_args = sp;
			} else {
				child_frame.stack_args = NULL;
			}

			g_assert (csig->call_convention == MONO_CALL_DEFAULT);

			child_frame.ex = NULL;
			child_frame.ex_handler = NULL;

			ves_exec_method (&child_frame);

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
			} else {
				sp->type = VAL_OBJ;
				sp->data.p = o;
				sp->data.vt.klass = newobj_class;
			}
			++sp;
			BREAK;
		}
		CASE (CEE_CASTCLASS) /* Fall through */
		CASE (CEE_ISINST) {
			MonoObject *o;
			MonoVTable *vt;
			MonoClass *c , *oclass;
			guint32 token;
			int do_isinst = *ip == CEE_ISINST;
			gboolean found = FALSE;

			++ip;
			token = read32 (ip);
			c = mono_class_get (image, token);

			mono_class_init (c);

			g_assert (sp [-1].type == VAL_OBJ);

			if ((o = sp [-1].data.p)) {

				vt = o->vtable;
				oclass = vt->klass;

				if (c->flags & TYPE_ATTRIBUTE_INTERFACE) {
					if ((c->interface_id <= oclass->max_interface_id) &&
					    vt->interface_offsets [c->interface_id])
						found = TRUE;
				} else {
					/* handle array casts */
					if (oclass->rank && oclass->rank == c->rank) {
						if ((oclass->element_class->baseval - c->element_class->baseval) <= c->element_class->diffval) {
							sp [-1].data.vt.klass = c;
							found = TRUE;
						}
					} else if ((oclass->baseval - c->baseval) <= c->diffval) {
						sp [-1].data.vt.klass = c;
						found = TRUE;
					}
				}

				if (!found) {
					if (do_isinst) {
						sp [-1].data.p = NULL;
						sp [-1].data.vt.klass = NULL;
					} else
						THROW_EX (mono_get_exception_invalid_cast (), ip - 1);
				}
			}
			ip += 4;
			BREAK;
		}
		CASE (CEE_CONV_R_UN)
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
			
			c = mono_class_get (image, token);
			mono_class_init (c);
			
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference(), ip - 1);

			if (o->vtable->klass->element_class->type_token != c->element_class->type_token)
				THROW_EX (mono_get_exception_invalid_cast (), ip - 1);

			sp [-1].type = VAL_MP;
			sp [-1].data.p = (char *)o + sizeof (MonoObject);

			ip += 4;
			BREAK;
		}
		CASE (CEE_THROW)
			--sp;
			frame->ex_handler = NULL;
			THROW_EX (sp->data.p, ip);
			BREAK;
		CASE (CEE_LDFLDA) /* Fall through */
		CASE (CEE_LDFLD) {
			MonoObject *obj;
			MonoClassField *field;
			guint32 token, offset;
			int load_addr = *ip == CEE_LDFLDA;

			if (!sp [-1].data.p)
				THROW_EX (mono_get_exception_null_reference (), ip);
			
			++ip;
			token = read32 (ip);
			ip += 4;

			if (sp [-1].type == VAL_OBJ) {
				obj = sp [-1].data.p;
				/* if we access a field from our parent and the parent was 
				 * defined in another assembly, we get a memberref.
				 */
				if (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF)
					field = mono_field_from_memberref (image, token, NULL);
				else
					field = mono_class_get_field (obj->vtable->klass, token);
				offset = field->offset;
			} else { /* valuetype */
				/*g_assert (sp [-1].type == VAL_VALUETA); */
				obj = sp [-1].data.vt.vt;
				field = mono_class_get_field (sp [-1].data.vt.klass, token);
				offset = field->offset - sizeof (MonoObject);
			}
			if (load_addr) {
				sp [-1].type = VAL_TP;
				sp [-1].data.p = (char*)obj + offset;
				sp [-1].data.vt.klass = mono_class_from_mono_type (field->type);
			} else {
				vt_alloc (field->type, &sp [-1]);
				stackval_from_data (field->type, &sp [-1], (char*)obj + offset);
				
			}
			BREAK;
		}
		CASE (CEE_STFLD) {
			MonoObject *obj;
			MonoClassField *field;
			guint32 token, offset;

			++ip;
			token = read32 (ip);
			ip += 4;
			
			sp -= 2;
			
			if (sp [0].type == VAL_OBJ) {
				obj = sp [0].data.p;
				/* if we access a field from our parent and the parent was 
				 * defined in another assembly, we get a memberref.
				 */
				if (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF)
					field = mono_field_from_memberref (image, token, NULL);
				else
					field = mono_class_get_field (obj->vtable->klass, token);
				offset = field->offset;
			} else { /* valuetype */
				/*g_assert (sp->type == VAL_VALUETA); */
				obj = sp [0].data.vt.vt;
				field = mono_class_get_field (sp [0].data.vt.klass, token);
				offset = field->offset - sizeof (MonoObject);
			}

			stackval_to_data (field->type, &sp [1], (char*)obj + offset);
			vt_free (&sp [1]);
			BREAK;
		}
		CASE (CEE_LDSFLD) /* Fall through */
		CASE (CEE_LDSFLDA) {
			MonoVTable *vt;
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;
			int load_addr = *ip == CEE_LDSFLDA;
			gpointer addr;

			++ip;
			token = read32 (ip);
			ip += 4;
			
			/* need to handle fieldrefs */
			if (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF) {
				field = mono_field_from_memberref (image, token, &klass);
				mono_class_init (klass);
			} else {
				klass = mono_class_get (image, 
					MONO_TOKEN_TYPE_DEF | mono_metadata_typedef_from_field (image, token & 0xffffff));
				mono_class_init (klass);
				field = mono_class_get_field (klass, token);
			}
			g_assert (field);
			
			vt = mono_class_vtable (domain, klass);
			addr = (char*)(vt->data) + field->offset;

			if (load_addr) {
				sp->type = VAL_TP;
				sp->data.p = addr;
				sp->data.vt.klass = mono_class_from_mono_type (field->type);
			} else {
				vt_alloc (field->type, sp);
				stackval_from_data (field->type, sp, addr);
			}
			++sp;
			BREAK;
		}
		CASE (CEE_STSFLD) {
			MonoVTable *vt;
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;
			gpointer addr;

			++ip;
			token = read32 (ip);
			ip += 4;
			--sp;

			/* need to handle fieldrefs */
			if (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF) {
				field = mono_field_from_memberref (image, token, &klass);
				mono_class_init (klass);
			} else {
				klass = mono_class_get (image, 
					MONO_TOKEN_TYPE_DEF | mono_metadata_typedef_from_field (image, token & 0xffffff));
				mono_class_init (klass);
				field = mono_class_get_field (klass, token);
			}
			g_assert (field);

			vt = mono_class_vtable (domain, klass);
			addr = (char*)(vt->data) + field->offset;

			stackval_to_data (field->type, sp, addr);
			vt_free (sp);
			BREAK;
		}
		CASE (CEE_STOBJ) {
			MonoClass *vtklass;
			++ip;
			vtklass = mono_class_get (image, read32 (ip));
			ip += 4;
			sp -= 2;
			memcpy (sp [0].data.p, sp [1].data.vt.vt, mono_class_value_size (vtklass, NULL));
			BREAK;
		}
#if SIZEOF_VOID_P == 8
		CASE (CEE_CONV_OVF_I_UN)
#endif
		CASE (CEE_CONV_OVF_I8_UN) {
			switch (sp [-1].type) {
			case VAL_DOUBLE:
				if (sp [-1].data.f < 0 || sp [-1].data.f > 9223372036854775807L)
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.l = (guint64)sp [-1].data.f;
				break;
			case VAL_I64:
				if (sp [-1].data.l < 0)
					THROW_EX (mono_get_exception_overflow (), ip);
				break;
			case VAL_VALUET:
				ves_abort();
			case VAL_I32:
				/* Can't overflow */
				sp [-1].data.l = (guint64)sp [-1].data.i;
				break;
			default:
				if ((gint64)sp [-1].data.nati < 0)
					THROW_EX (mono_get_exception_overflow (), ip);
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
				if (value > 2147483647)
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

			class = mono_class_get (image, token);
			mono_class_init (class);
			g_assert (class != NULL);

			sp [-1].type = VAL_OBJ;
			if (class->byval_arg.type == MONO_TYPE_VALUETYPE && !class->enumtype) 
				sp [-1].data.p = mono_value_box (domain, class, sp [-1].data.p);
			else
				sp [-1].data.p = mono_value_box (domain, class, &sp [-1]);
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
			class = mono_class_get (image, token);
			o = (MonoObject*) mono_array_new (domain, class, sp [-1].data.i);
			ip += 4;

			sp [-1].type = VAL_OBJ;
			sp [-1].data.p = o;
			if (profiling_classes) {
				guint count = GPOINTER_TO_UINT (g_hash_table_lookup (profiling_classes, o->vtable->klass));
				count++;
				g_hash_table_insert (profiling_classes, o->vtable->klass, GUINT_TO_POINTER (count));
			}

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
			
			++ip;
			token = read32 (ip);
			ip += 4;
			sp -= 2;

			g_assert (sp [0].type == VAL_OBJ);
			o = sp [0].data.p;

			g_assert (MONO_CLASS_IS_ARRAY (o->obj.vtable->klass));
			
			if (sp [1].data.nati >= mono_array_length (o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip - 5);

			/* check the array element corresponds to token */
			esize = mono_array_element_size (o->obj.vtable->klass);
			
			sp->type = VAL_MP;
			sp->data.p = mono_array_addr_with_size (o, esize, sp [1].data.i);
			sp->data.vt.klass = o->obj.vtable->klass->element_class;

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
			
			aindex = sp [1].data.nati;
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
				sp [0].data.l = mono_array_get (o, gint64, aindex);
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
				sp [0].data.vt.klass = NULL;
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
		    
			aindex = sp [1].data.nati;
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
				mono_array_set (o, gpointer, aindex, sp [2].data.p);
				break;
			default:
				ves_abort();
			}

			++ip;
			BREAK;
		}
		CASE (CEE_UNUSED2) 
		CASE (CEE_UNUSED3) 
		CASE (CEE_UNUSED4) 
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
		CASE (CEE_CONV_OVF_I1)
		CASE (CEE_CONV_OVF_U1) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I2)
		CASE (CEE_CONV_OVF_U2)
			++ip;
			/* FIXME: handle other cases */
			if (sp [-1].type == VAL_I32) {
				/* defined as NOP */
			} else {
				ves_abort();
			}
			BREAK;
		CASE (CEE_CONV_OVF_I4)
			/* FIXME: handle other cases */
			if (sp [-1].type == VAL_I32) {
				/* defined as NOP */
			} else if(sp [-1].type == VAL_I64) {
				sp [-1].data.i = (gint32)sp [-1].data.l;
			} else {
				ves_abort();
			}
			++ip;
			BREAK;
		CASE (CEE_CONV_OVF_U4)
			/* FIXME: handle other cases */
			if (sp [-1].type == VAL_I32) {
				/* defined as NOP */
			} else if(sp [-1].type == VAL_I64) {
				sp [-1].data.i = (guint32)sp [-1].data.l;
			} else {
				ves_abort();
			}
			++ip;
			BREAK;
		CASE (CEE_CONV_OVF_I8) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U8) ves_abort(); BREAK;
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
			vt_alloc (&handle_class->byval_arg, sp);
			stackval_from_data (&handle_class->byval_arg, sp, (char*)&handle);
			++sp;
			BREAK;
		}
		CASE (CEE_CONV_OVF_I)
			++ip;
			--sp;
			/* FIXME: check overflow. */
			switch (sp->type) {
			case VAL_I32:
				sp->data.p = (gpointer)(mono_i) sp->data.i;
				break;
			case VAL_I64:
				sp->data.p = (gpointer)(mono_i) sp->data.l;
				break;
			case VAL_NATI:
				break;
			case VAL_DOUBLE:
				sp->data.p = (gpointer)(mono_i) sp->data.f;
				break;
			default:
				ves_abort ();
			}
			sp->type = VAL_NATI;
			++sp;
			BREAK;
		CASE (CEE_CONV_OVF_U) ves_abort(); BREAK;
		CASE (CEE_ADD_OVF)
			--sp;
			/* FIXME: check overflow */
			if (sp->type == VAL_I32) {
				if (CHECK_ADD_OVERFLOW (sp [-1].data.i, GET_NATI (sp [0])))
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = (guint32)sp [-1].data.i + (guint32)GET_NATI (sp [0]);
			} else if (sp->type == VAL_I64) {
				if (CHECK_ADD_OVERFLOW64 (sp [-1].data.l, sp [0].data.l))
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
		CASE (CEE_ADD_OVF_UN)
			--sp;
			/* FIXME: check overflow, make unsigned */
			if (sp->type == VAL_I32) {
				if (CHECK_ADD_OVERFLOW_UN (sp [-1].data.i, GET_NATI (sp [0])))
					THROW_EX (mono_get_exception_overflow (), ip);
				sp [-1].data.i = (guint32)sp [-1].data.i + (guint32)GET_NATI (sp [0]);
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
			++ip;
			--sp;
			/* FIXME: check overflow */
			if (sp->type == VAL_I32)
				sp [-1].data.i *= (gint)GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l *= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f *= sp [0].data.f;
			BREAK;
		CASE (CEE_MUL_OVF_UN)
			++ip;
			--sp;
			/* FIXME: check overflow, make unsigned */
			if (sp->type == VAL_I32)
				sp [-1].data.i *= (gint)GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l *= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f *= sp [0].data.f;
			BREAK;
		CASE (CEE_SUB_OVF)
		CASE (CEE_SUB_OVF_UN)
			++ip;
			--sp;
			/* FIXME: handle undeflow/unsigned */
			/* should probably consider the pointers as unsigned */
			if (sp->type == VAL_I32)
				sp [-1].data.i -= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l -= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f -= sp [0].data.f;
			else {
				char *p = sp [-1].data.p;
				p -= GET_NATI (sp [0]);
				sp [-1].data.p = p;
			}
			BREAK;
		CASE (CEE_ENDFINALLY)
			if (frame->ex)
				goto handle_fault;
			/*
			 * There was no exception, we continue normally at the target address.
			 */
			ip = endfinally_ip;
			BREAK;
		CASE (CEE_LEAVE) /* Fall through */
		CASE (CEE_LEAVE_S)
			sp = frame->stack; /* empty the stack */
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
#if 0
			/*
			 * We may be either inside a try block or inside an handler.
			 * In the first case there was no exception and we go on
			 * executing the finally handlers and after that resume control
			 * at endfinally_ip.
			 * In the second case we need to clear the exception and
			 * continue directly at the target ip.
			 */
			if (!frame->ex) {
				endfinally_ip = ip;
				goto handle_finally;
			} else {
				frame->ex = NULL;
				frame->ex_handler = NULL;
			}
#endif
			frame->ex = NULL;
			frame->ex_handler = NULL;
			endfinally_ip = ip;
			goto handle_finally;
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
		CASE (CEE_UNUSED41) 
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
					result = sp [0].data.i == (gint)GET_NATI (sp [1]);
				else if (sp->type == VAL_I64)
					result = sp [0].data.l == sp [1].data.l;
				else if (sp->type == VAL_DOUBLE) {
					if (isnan (sp [0].data.f) || isnan (sp [1].data.f))
						result = 0;
					else
						result = sp [0].data.f == sp [1].data.f;
				} else
					result = GET_NATI (sp [0]) == GET_NATI (sp [1]);
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
					result = sp [0].data.i > (gint)GET_NATI (sp [1]);
				else if (sp->type == VAL_I64)
					result = sp [0].data.l > sp [1].data.l;
				else if (sp->type == VAL_DOUBLE) {
					if (isnan (sp [0].data.f) || isnan (sp [1].data.f))
						result = 0;
					else
						result = sp [0].data.f > sp [1].data.f;
				} else
					result = (gint)GET_NATI (sp [0]) > (gint)GET_NATI (sp [1]);
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
					result = (guint32)sp [0].data.i > (mono_u)GET_NATI (sp [1]);
				else if (sp->type == VAL_I64)
					result = (guint64)sp [0].data.l > (guint64)sp [1].data.l;
				else if (sp->type == VAL_DOUBLE)
					result = isnan (sp [0].data.f) || isnan (sp [1].data.f);
				else
					result = (mono_u)GET_NATI (sp [0]) > (mono_u)GET_NATI (sp [1]);
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
					result = sp [0].data.i < (gint)GET_NATI (sp [1]);
				else if (sp->type == VAL_I64)
					result = sp [0].data.l < sp [1].data.l;
				else if (sp->type == VAL_DOUBLE) {
					if (isnan (sp [0].data.f) || isnan (sp [1].data.f))
						result = 0;
					else
						result = sp [0].data.f < sp [1].data.f;
				} else
					result = (gint)GET_NATI (sp [0]) < (gint)GET_NATI (sp [1]);
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
					result = (guint32)sp [0].data.i < (mono_u)GET_NATI (sp [1]);
				else if (sp->type == VAL_I64)
					result = (guint64)sp [0].data.l < (guint64)sp [1].data.l;
				else if (sp->type == VAL_DOUBLE)
					result = isnan (sp [0].data.f) || isnan (sp [1].data.f);
				else
					result = (mono_u)GET_NATI (sp [0]) < (mono_u)GET_NATI (sp [1]);
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
				m = mono_get_method (image, token, NULL);
				if (!m)
					THROW_EX (mono_get_exception_missing_method (), ip - 5);
				if (virtual) {
					--sp;
					if (!sp->data.p)
						THROW_EX (mono_get_exception_null_reference (), ip - 5);
					m = get_virtual_method (domain, m, sp);
				}
				sp->type = VAL_NATI;
				sp->data.p = mono_create_method_pointer (m);
				sp->data.vt.klass = NULL;
				++sp;
				break;
			}
			case CEE_UNUSED56: ves_abort(); break;
			case CEE_LDARG: {
				guint32 arg_pos;
				++ip;
				arg_pos = read16 (ip);
				ip += 2;
				vt_alloc (ARG_TYPE (signature, arg_pos), sp);
				stackval_from_data (ARG_TYPE (signature, arg_pos), sp, ARG_POS (arg_pos));
				++sp;
				break;
			}
			case CEE_LDARGA: {
				MonoType *t;
				MonoClass *c;
				guint32 anum;

				++ip;
				anum = read16 (ip);
				ip += 2;
				t = ARG_TYPE (signature, anum);
				c = mono_class_from_mono_type (t);
				sp->data.vt.klass = c;
				sp->data.vt.vt = ARG_POS (anum);

				if (c->valuetype)
					sp->type = VAL_VALUETA;
				else
					sp->type = VAL_TP;

				++sp;
				++ip;
				break;
			}
			case CEE_STARG: {
				guint32 arg_pos;
				++ip;
				arg_pos = read16 (ip);
				ip += 2;
				--sp;
				stackval_to_data (ARG_TYPE (signature, arg_pos), sp, ARG_POS (arg_pos));
				vt_free (sp);
				break;
			}
			case CEE_LDLOC: {
				guint32 loc_pos;
				++ip;
				loc_pos = read16 (ip);
				ip += 2;
				vt_alloc (LOCAL_TYPE (header, loc_pos), sp);
				stackval_from_data (LOCAL_TYPE (header, loc_pos), sp, LOCAL_POS (loc_pos));
				++sp;
				break;
			}
			case CEE_LDLOCA: {
				MonoType *t;
				MonoClass *c;
				guint32 loc_pos;

				++ip;
				loc_pos = read16 (ip);
				ip += 2;
				t = LOCAL_TYPE (header, loc_pos);
				c =  mono_class_from_mono_type (t);
				sp->data.vt.vt = LOCAL_POS (loc_pos);
				sp->data.vt.klass = c;

				if (c->valuetype) 
					sp->type = VAL_VALUETA;
				else
					sp->type = VAL_TP;

				++sp;
				break;
			}
			case CEE_STLOC: {
				guint32 loc_pos;
				++ip;
				loc_pos = read16 (ip);
				ip += 2;
				--sp;
				stackval_to_data (LOCAL_TYPE (header, loc_pos), sp, LOCAL_POS (loc_pos));
				vt_free (sp);
				break;
			}
			case CEE_LOCALLOC:
				if (sp != frame->stack)
					THROW_EX (mono_get_exception_execution_engine (NULL), ip - 1);
				++ip;
				sp->data.p = alloca (sp->data.i);
				sp->type = VAL_TP;
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
				++ip;
				token = read32 (ip);
				ip += 4;
				/*
				 * we ignore the value of token (I think we can as unspecified
				 * behavior described in Partition II, 3.5).
				 */
				--sp;
				g_assert (sp->type == VAL_VALUETA);
				memset (sp->data.vt.vt, 0, mono_class_value_size (sp->data.vt.klass, NULL));
				break;
			}
			case CEE_UNUSED68: ves_abort(); break;
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
			case CEE_UNUSED69: ves_abort(); break;
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
				MonoType *type;
				int align;
				++ip;
				token = read32 (ip);
				ip += 4;
				type = mono_type_create_from_typespec (image, token);
				sp->type = VAL_I32;
				sp->data.i = mono_type_size (type, &align);
				mono_metadata_free_type (type);
				++sp;
				break;
			}
			case CEE_REFANYTYPE: ves_abort(); break;
			case CEE_UNUSED52: 
			case CEE_UNUSED53: 
			case CEE_UNUSED54: 
			case CEE_UNUSED55: 
			case CEE_UNUSED70: 
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
		char *message;
		MonoObject *ex_obj;

		if (die_on_exception)
			goto die_on_ex;
		
		for (inv = frame; inv; inv = inv->parent) {
			if (inv->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
				continue;
			hd = ((MonoMethodNormal*)inv->method)->header;
			ip_offset = inv->ip - hd->code;
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
							/*
							 * It seems that if the catch handler is found in the same method,
							 * it gets executed before the finally handler.
							 */
							if (inv == frame)
								goto handle_fault;
							else
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
die_on_ex:
		ex_obj = (MonoObject*)frame->ex;
		message = frame->ex->message? mono_string_to_utf8 (frame->ex->message): NULL;
		g_print ("Unhandled exception %s.%s %s.\n", ex_obj->vtable->klass->name_space, ex_obj->vtable->klass->name,
				message?message:"");
		g_free (message);
		message = dump_frame (frame);
		g_print ("%s", message);
		g_free (message);
		exit (1);
	}
	handle_finally:
	{
		int i;
		guint32 ip_offset;
		MonoExceptionClause *clause;
		
		if (frame->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
			DEBUG_LEAVE ();
			return;
		}
		ip_offset = frame->ip - header->code;

		for (i = 0; i < header->num_clauses; ++i) {
			clause = &header->clauses [i];
			if (MONO_OFFSET_IN_CLAUSE (clause, ip_offset)) {
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
					ip = header->code + clause->handler_offset;
					goto main_loop;
				} else
					break;
			}
		}

		/*
		 * If an exception is set, we need to execute the fault handler, too,
		 * otherwise, we continue normally.
		 */
		if (frame->ex)
			goto handle_fault;
		ip = endfinally_ip;
		goto main_loop;
	}
	handle_fault:
	{
		int i;
		guint32 ip_offset;
		MonoExceptionClause *clause;
		
		ip_offset = frame->ip - header->code;
		for (i = 0; i < header->num_clauses; ++i) {
			clause = &header->clauses [i];
			if (clause->flags == 3 && MONO_OFFSET_IN_CLAUSE (clause, ip_offset)) {
				ip = header->code + clause->handler_offset;
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
			ip = header->code + frame->ex_handler->handler_offset;
			sp = frame->stack;
			sp->type = VAL_OBJ;
			sp->data.p = frame->ex;
			++sp;
			goto main_loop;
		}
		DEBUG_LEAVE ();
		return;
	}
	
}

static gint32
runtime_exec_main (MonoMethod *method, MonoArray *args)
{
	stackval result;
	stackval argv_array;
	MonoInvocation call;

	argv_array.type = VAL_OBJ;
	argv_array.data.p = args;
	argv_array.data.vt.klass = NULL;

	if (args)
		INIT_FRAME (&call, NULL, NULL, &argv_array, &result, method);
	else 
		INIT_FRAME (&call, NULL, NULL, NULL, &result, method);

	ves_exec_method (&call);

	return result.data.i;

}

static int 
ves_exec (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[])
{
	MonoArray *args = NULL;
	MonoImage *image = assembly->image;
	MonoCLIImageInfo *iinfo;
	MonoMethod *method;
	int i;

	iinfo = image->image_info;
	method = mono_get_method (image, iinfo->cli_cli_header.ch_entry_point, NULL);
	if (!method)
		g_error ("No entry point method found in %s", image->name);

	if (method->signature->param_count) {
		args = (MonoArray*)mono_array_new (domain, mono_defaults.string_class, argc);
		for (i = 0; i < argc; ++i) {
			MonoString *arg = mono_string_new (domain, argv [i]);
			mono_array_set (args, gpointer, i, arg);
		}
	}
	
	return mono_runtime_exec_main (method, args);
}

static void
usage (void)
{
	fprintf (stderr,
		 "mint %s, the Mono ECMA CLI interpreter, (C) 2001 Ximian, Inc.\n\n"
		 "Usage is: mint [options] executable args...\n", VERSION);
	fprintf (stderr,
		 "Valid Options are:\n"
#ifdef DEBUG_INTERP
		 "--debug\n"
#endif
		 "--help\n"
		 "--trace\n"
		 "--traceops\n"
		 "--traceclassinit\n"
		 "--dieonex\n"
		 "--profile\n"
		 "--debug method_name\n"
		 "--print-vtable\n"
		 "--opcode-count\n");
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

static gint
compare_profile (MethodProfile *profa, MethodProfile *profb)
{
	return (gint)((profb->total - profa->total)*1000);
}

static void
build_profile (MonoMethod *m, MethodProfile *prof, GList **funcs)
{
	g_timer_destroy (prof->u.timer);
	prof->u.method = m;
	*funcs = g_list_insert_sorted (*funcs, prof, (GCompareFunc)compare_profile);
}

static void
output_profile (GList *funcs)
{
	GList *tmp;
	MethodProfile *p;
	char buf [256];

	if (funcs)
		g_print ("Method name\t\t\t\t\tTotal (ms) Calls Per call (ms)\n");
	for (tmp = funcs; tmp; tmp = tmp->next) {
		p = tmp->data;
		if (!(gint)(p->total*1000))
			continue;
		g_snprintf (buf, sizeof (buf), "%s.%s::%s(%d)",
			p->u.method->klass->name_space, p->u.method->klass->name,
			p->u.method->name, p->u.method->signature->param_count);
		printf ("%-52s %7d %7llu %7d\n", buf,
			(gint)(p->total*1000), p->count, (gint)((p->total*1000)/p->count));
	}
}

typedef struct {
	MonoClass *klass;
	guint count;
} NewobjProfile;

static gint
compare_newobj_profile (NewobjProfile *profa, NewobjProfile *profb)
{
	return (gint)profb->count - (gint)profa->count;
}

static void
build_newobj_profile (MonoClass *class, gpointer count, GList **funcs)
{
	NewobjProfile *prof = g_new (NewobjProfile, 1);
	prof->klass = class;
	prof->count = GPOINTER_TO_UINT (count);
	*funcs = g_list_insert_sorted (*funcs, prof, (GCompareFunc)compare_newobj_profile);
}

static void
output_newobj_profile (GList *proflist)
{
	GList *tmp;
	NewobjProfile *p;
	MonoClass *klass;
	char* isarray;
	char buf [256];

	if (proflist)
		g_print ("\n%-52s %9s\n", "Objects created:", "count");
	for (tmp = proflist; tmp; tmp = tmp->next) {
		p = tmp->data;
		klass = p->klass;
		if (strcmp (klass->name, "Array") == 0) {
			isarray = "[]";
			klass = klass->element_class;
		} else {
			isarray = "";
		}
		g_snprintf (buf, sizeof (buf), "%s.%s%s",
			klass->name_space, klass->name, isarray);
		g_print ("%-52s %9d\n", buf, p->count);
	}
}

static MonoException * segv_exception = NULL;

static void
segv_handler (int signum)
{
	signal (signum, segv_handler);
	mono_raise_exception (segv_exception);
}

int 
main (int argc, char *argv [])
{
	MonoDomain *domain;
	MonoAssembly *assembly;
	GList *profile = NULL;
	int retval = 0, i, ocount = 0;
	char *file, *error;

	if (argc < 2)
		usage ();

	for (i = 1; i < argc && argv [i][0] == '-'; i++){
		if (strcmp (argv [i], "--trace") == 0)
			global_tracing = 1;
		if (strcmp (argv [i], "--traceops") == 0)
			global_tracing = 2;
		if (strcmp (argv [i], "--traceclassinit") == 0)
			global_class_init_tracing = 1;
		if (strcmp (argv [i], "--dieonex") == 0)
			die_on_exception = 1;
		if (strcmp (argv [i], "--print-vtable") == 0)
			mono_print_vtable = TRUE;
		if (strcmp (argv [i], "--profile") == 0) {
			profiling = g_hash_table_new (g_direct_hash, g_direct_equal);
			profiling_classes = g_hash_table_new (g_direct_hash, g_direct_equal);
		}
		if (strcmp (argv [i], "--opcode-count") == 0)
			ocount = 1;
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

	mono_init_icall ();
	mono_add_internal_call ("__array_Set", ves_array_set);
	mono_add_internal_call ("__array_Get", ves_array_get);
	mono_add_internal_call ("__array_Address", ves_array_element_address);

	frame_thread_id = TlsAlloc ();
	TlsSetValue (frame_thread_id, NULL);

	mono_install_runtime_class_init (runtime_class_init);
	mono_install_runtime_object_init (runtime_object_init);
	mono_install_runtime_exec_main (runtime_exec_main);
	mono_install_runtime_invoke (interp_mono_runtime_invoke);

	mono_install_handler (interp_ex_handler);

	domain = mono_init (file);
	mono_thread_init (domain);
	mono_network_init ();

	assembly = mono_domain_assembly_open (domain, file);

	if (!assembly){
		fprintf (stderr, "Can not open image %s\n", file);
		exit (1);
	}


#ifdef RUN_TEST
	test_load_class (assembly->image);
#else
	error = mono_verify_corlib ();
	if (error) {
		fprintf (stderr, "Corlib not in sync with this runtime: %s\n", error);
		exit (1);
	}
	segv_exception = mono_get_exception_null_reference ();
	segv_exception->message = mono_string_new (domain, "Segmentation fault");
	signal (SIGSEGV, segv_handler);
	/*
	 * skip the program name from the args.
	 */
	++i;
	retval = ves_exec (domain, assembly, argc - i, argv + i);
#endif
	if (profiling) {
		g_hash_table_foreach (profiling, (GHFunc)build_profile, &profile);
		output_profile (profile);
		g_list_free (profile);
		profile = NULL;
		
		g_hash_table_foreach (profiling_classes, (GHFunc)build_newobj_profile, &profile);
		output_newobj_profile (profile);
		g_list_free (profile);
	}
	
	mono_network_cleanup ();
	mono_thread_cleanup ();

	mono_domain_unload (domain, TRUE);

#if DEBUG_INTERP
	if (ocount) {
		fprintf (stderr, "opcode count: %ld\n", opcode_count);
		fprintf (stderr, "fcall count: %ld\n", fcall_count);
	}
#endif
	return retval;
}



