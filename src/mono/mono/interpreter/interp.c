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
#include "config.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <glib.h>
#include <setjmp.h>
#include <signal.h>

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
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/socket-io.h>
/*#include <mono/cli/types.h>*/
#include "interp.h"
#include "hacks.h"


typedef struct {
	union {
		GTimer *timer;
		MonoMethod *method;
	} u;
	gulong count;
	gulong total;
} MethodProfile;

/* If true, then we output the opcodes as we interpret them */
static int tracing = 0;

static int debug_indent_level = 0;

static GHashTable *profiling = NULL;

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

/*
 * Pull the opcode names
 */
#define OPDEF(a,b,c,d,e,f,g,h,i,j)  b,
static char *opcode_names[] = {
#include "mono/cil/opcode.def"
	NULL
};

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

static guint32 frame_thread_id = 0;

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
	MonoClass *klass = obj->klass;

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
	fprintf (stderr, "Execution aborted in method: %s\n", mh->name);
	fprintf (stderr, "Line=%d IP=0x%04x, Aborted execution\n", line,
		 ip-mm->header->code);
	g_print ("0x%04x %02x\n",
		 ip-mm->header->code, *ip);
	if (sp > stack)
		printf ("\t[%d] %d 0x%08x %0.5f\n", sp-stack, sp[-1].type, sp[-1].data.i, sp[-1].data.f);
}
#define ves_abort() do {ves_real_abort(__LINE__, frame->method, ip, frame->stack, sp); THROW_EX (mono_get_exception_execution_engine (), ip);} while (0);

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

	for (i = 0; i < klass->method.count; ++i) {
		method = klass->methods [i];
		if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && 
		    (strcmp (".cctor", method->name) == 0)) {
			INIT_FRAME (&call, NULL, NULL, NULL, NULL, method);
			ves_exec_method (&call);
			return;
		}
	}
	/* No class constructor found */
}

static MonoMethod*
get_virtual_method (MonoMethod *m, stackval *objs)
{
	MonoObject *obj;
	MonoClass *klass;
	MonoMethod **vtable;

	if ((m->flags & METHOD_ATTRIBUTE_FINAL) || !(m->flags & METHOD_ATTRIBUTE_VIRTUAL))
			return m;

	g_assert (m->klass->inited);

	obj = objs->data.p;
	klass = obj->klass;
	vtable = (MonoMethod **)klass->vtable;

	if (m->klass->flags & TYPE_ATTRIBUTE_INTERFACE)
		return *(MonoMethod**)(klass->interface_offsets [m->klass->interface_id] + (m->slot<<2));

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
	case MONO_TYPE_PTR:
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
ves_array_create (MonoClass *klass, MonoMethodSignature *sig, stackval *values)
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
	return (MonoObject*)mono_array_new_full (klass, lengths, lower_bounds);
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
	ac = o->klass;

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
	ac = o->klass;

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
	ac = o->klass;

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
	
	if (setjmp(env))
		return;
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
	MonoDelegate *delegate = (MonoDelegate*)frame->obj;

	mono_class_init (mono_defaults.delegate_class);
	
	if (*name == '.' && (strcmp (name, ".ctor") == 0) && obj &&
			mono_object_isinst (obj, mono_defaults.delegate_class)) {
		delegate->target = frame->stack_args[0].data.p;
		delegate->method_ptr = frame->stack_args[1].data.p;
		return;
	}
	if (*name == 'I' && (strcmp (name, "Invoke") == 0) && obj &&
			mono_object_isinst (obj, mono_defaults.delegate_class)) {
		MonoPIFunc func;
		guchar *code;
		MonoMethod *method;
		
		code = (guchar*)delegate->method_ptr;
		method = mono_method_pointer_get (code);
		/* FIXME: check for NULL method */
		if (!method->addr)
			method->addr = mono_create_trampoline (method, 1);
		func = method->addr;
		/* FIXME: need to handle exceptions across managed/unmanaged boundaries */
		func ((MonoFunc)delegate->method_ptr, &frame->retval->data.p, delegate->target, frame->stack_args);
		stackval_from_data (frame->method->signature->ret, frame->retval, (const char*)&frame->retval->data.p);
		return;
	}
	g_error ("Don't know how to exec runtime method %s.%s::%s", 
			frame->method->klass->name_space, frame->method->klass->name,
			frame->method->name);
}

static void
dump_stack (stackval *stack, stackval *sp)
{
	stackval *s = stack;
	
	if (sp == stack)
		return;
	
	while (s < sp) {
		switch (s->type) {
		case VAL_I32: printf ("[%d] ", s->data.i); break;
		case VAL_I64: printf ("[%lld] ", s->data.l); break;
		case VAL_DOUBLE: printf ("[%0.5f] ", s->data.f); break;
		case VAL_VALUET: printf ("[vt: %p] ", s->data.vt.vt); break;
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
		default: printf ("[%p] ", s->data.p); break;
		}
		++s;
	}
}

static void
dump_frame (MonoInvocation *inv)
{
	int i;
	for (i = 0; inv; inv = inv->parent, ++i) {
		MonoClass *k = inv->method->klass;
		int codep;
		if (inv->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL ||
				inv->method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
			codep = 0;
		} else {
			MonoMethodHeader *hd = ((MonoMethodNormal *)inv->method)->header;
			codep = inv->ip - hd->code;
		}
		g_print ("#%d: 0x%05x in %s.%s::%s (", i, codep, 
						k->name_space, k->name, inv->method->name);
		dump_stack (inv->stack_args, inv->stack_args + inv->method->signature->param_count);
		g_print (")\n");
	}
}

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
	char *startname, *startclass;

	if (strcmp((char*)data, m->name) == 0) {
		break_on_method = 1;
		return;
	}
	startname = rindex ((char*)data, ':');
	if (!startname)
		return;
	if (strcmp(startname + 1, m->name) != 0)
		return;
	startclass = (char*)data;
	if (memcmp (startclass, m->klass->name, startname-startclass) == 0)
		break_on_method = 1;
	/* ignore namespace */
}

static guint32*
calc_offsets (MonoMethodHeader *header, MonoMethodSignature *signature)
{
	int i, align, size, offset = 0;
	int hasthis = signature->hasthis;
	guint32 *offsets = g_new0 (guint32, 2 + header->num_locals + signature->param_count + signature->hasthis);

	for (i = 0; i < header->num_locals; ++i) {
		offsets [2 + i] = offset;
		size = mono_type_size (header->locals [i], &align);
		offset += align - 1;
		offset &= ~(align - 1);
		offset += size;
	}
	offsets [0] = offset;
	offset = 0;
	if (hasthis) {
		offsets [2 + header->num_locals] = offset;
		offset += sizeof (gpointer);
	}
	for (i = 0; i < signature->param_count; ++i) {
		offsets [2 + hasthis + header->num_locals + i] = offset;
		size = mono_type_size (signature->params [i], &align);
		offset += align - 1;
		offset &= ~(align - 1);
		offset += size;
	}
	offsets [1] = offset;
	return offsets;
}

#define DEBUG_ENTER()	\
	fcall_count++;	\
	g_list_foreach (db_methods, db_match_method, (gpointer)frame->method);	\
	if (break_on_method) G_BREAKPOINT ();	\
	break_on_method = 0;	\
	if (tracing) {	\
		MonoClass *klass = frame->method->klass;	\
		debug_indent_level++;	\
		output_indent ();	\
		g_print ("Entering %s.%s::%s (", klass->name_space, klass->name, frame->method->name);	\
		dump_stack (frame->stack_args, frame->stack_args+frame->method->signature->param_count);	\
		g_print (")\n");	\
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
		g_print ("Leaving %s.%s::%s\n", klass->name_space, klass->name, frame->method->name);	\
		debug_indent_level--;	\
	}	\
	if (profiling)	\
		g_timer_stop (profile_info->u.timer)

#else

#define DEBUG_ENTER()
#define DEBUG_LEAVE()

#endif

#define LOCAL_POS(n)            (frame->locals + offsets [2 + (n)])
#define LOCAL_TYPE(header, n)   ((header)->locals [(n)])

#define ARG_POS(n)              (args_pointers [(n)])
#define ARG_TYPE(sig, n)        ((n) ? (sig)->params [(n) - (sig)->hasthis] : \
				(sig)->hasthis ? &frame->method->klass->this_arg: (sig)->params [(0)])

#define THROW_EX(exception,ex_ip)	\
		do {\
			frame->ip = (ex_ip);		\
			frame->ex = (MonoException*)(exception);	\
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
			if (!vtalloc || vtalloc->size < size) {	\
				vtalloc = alloca (sizeof (vtallocation) + size);	\
				vtalloc->size = size;	\
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

#define CHECK_ADD_OVERFLOW(a,b) \
	(gint32)(b) >= 0 ? (gint32)(INT_MAX) - (gint32)(b) < (gint32)(a) ? -1 : 0	\
	: (gint32)(INT_MIN) - (gint32)(b) > (gint32)(a) ? +1 : 0

#define CHECK_ADD_OVERFLOW_UN(a,b) \
	(guint32)(UINT_MAX) - (guint32)(b) < (guint32)(a) ? -1 : 0

#define CHECK_ADD_OVERFLOW64(a,b) \
	(gint64)(b) >= 0 ? (gint64)(LLONG_MAX) - (gint64)(b) < (gint64)(a) ? -1 : 0	\
	: (gint64)(LLONG_MIN) - (gint64)(b) > (gint64)(a) ? +1 : 0

#define CHECK_ADD_OVERFLOW64_UN(a,b) \
	(guint64)(ULONG_MAX) - (guint64)(b) < (guint64)(a) ? -1 : 0

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
	MonoInvocation child_frame;
	MonoMethodHeader *header;
	MonoMethodSignature *signature;
	MonoImage *image;
	const unsigned char *endfinally_ip;
	register const unsigned char *ip;
	register stackval *sp;
	void **args_pointers;
	guint32 *offsets;
	unsigned char tail_recursion = 0;
	unsigned char unaligned_address = 0;
	unsigned char volatile_address = 0;
	vtallocation *vtalloc = NULL;
	MethodProfile *profile_info;
	GOTO_LABEL_VARS;

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
	signature = frame->method->signature;
	image = frame->method->klass->image;

	/*
	 * with alloca we get the expected huge performance gain
	 * stackval *stack = g_new0(stackval, header->max_stack);
	 */

	sp = frame->stack = alloca (sizeof (stackval) * header->max_stack);

	if (!frame->method->info)
		frame->method->info = calc_offsets (header, signature);
	offsets = frame->method->info;

	if (header->num_locals) {
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

		frame->args = alloca (offsets [1]);
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
			output_indent ();
			g_print ("stack: ");
			dump_stack (frame->stack, sp);
			g_print ("\n");
			output_indent ();
			g_print ("0x%04x: %s\n", ip-header->code,
				 *ip == 0xfe ? opcode_names [256 + ip [1]] : opcode_names [*ip]);
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
					child_frame.method = get_virtual_method (child_frame.method, this_arg);
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
			sp[-1].data.i = *(gfloat*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_R8)
			++ip;
			sp[-1].type = VAL_DOUBLE;
			sp[-1].data.i = *(gdouble*)sp[-1].data.p;
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
				val /= (gulong)sp [0].data.p;
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
				(gulong)sp [-1].data.l %= (gulong)sp [0].data.l;
			} else if (sp->type == VAL_DOUBLE) {
				/* unspecified behaviour according to the spec */
			} else {
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (mono_get_exception_divide_by_zero (), ip - 1);
				(gulong)GET_NATI (sp [-1]) %= (gulong)GET_NATI (sp [0]);
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
			if (sp->type == VAL_I32)
				sp [-1].data.i <<= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l <<= GET_NATI (sp [0]);
			else
				GET_NATI (sp [-1]) <<= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHR)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i >>= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l >>= GET_NATI (sp [0]);
			else
				(gint)GET_NATI (sp [-1]) >>= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHR_UN)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				(guint)sp [-1].data.i >>= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				(gulong)sp [-1].data.l >>= GET_NATI (sp [0]);
			else
				(gulong)GET_NATI (sp [-1]) >>= GET_NATI (sp [0]);
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

			o = (MonoObject*)mono_ldstr (image, index);
			sp->type = VAL_OBJ;
			sp->data.p = o;
			sp->data.vt.klass = NULL;

			++sp;
			BREAK;
		}
		CASE (CEE_NEWOBJ) {
			MonoObject *o;
			MonoMethodSignature *csig;
			stackval *endsp = sp;
			guint32 token;

			frame->ip = ip;

			ip++;
			token = read32 (ip);
			ip += 4;

			if (!(child_frame.method = mono_get_method (image, token, NULL)))
				THROW_EX (mono_get_exception_missing_method (), ip -5);

			csig = child_frame.method->signature;

			if (child_frame.method->klass->parent == mono_defaults.array_class) {
				sp -= csig->param_count;
				o = ves_array_create (child_frame.method->klass, csig, sp);
				goto array_constructed;
			}

			o = mono_object_new (child_frame.method->klass);

			/*
			 * First arg is the object.
			 */
			child_frame.obj = o;

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
			sp->type = VAL_OBJ;
			sp->data.p = o;
			sp->data.vt.klass = o->klass;
			++sp;
			BREAK;
		}
		CASE (CEE_CASTCLASS) /* Fall through */
		CASE (CEE_ISINST) {
			MonoObject *o;
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

				oclass = o->klass;

				if (c->flags & TYPE_ATTRIBUTE_INTERFACE) {
					if ((c->interface_id <= oclass->max_interface_id) &&
					    oclass->interface_offsets [c->interface_id])
						found = TRUE;
				} else {
					if ((oclass->baseval - c->baseval) <= c->diffval) {
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
				sp [-1].data.f = (double)(gulong)sp [-1].data.nati;
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
			
			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference(), ip - 1);

			if (o->klass->element_class->type_token != c->type_token)
				THROW_EX (mono_get_exception_invalid_cast (), ip - 1);

			sp [-1].type = VAL_MP;
			sp [-1].data.p = (char *)o + sizeof (MonoObject);

			ip += 4;
			BREAK;
		}
		CASE (CEE_THROW)
			--sp;
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
				field = mono_class_get_field (obj->klass, token);
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
				field = mono_class_get_field (obj->klass, token);
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
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;
			int load_addr = *ip == CEE_LDSFLDA;

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
			if (load_addr) {
				sp->type = VAL_TP;
				sp->data.p = (char*)MONO_CLASS_STATIC_FIELDS_BASE (klass) + field->offset;
				sp->data.vt.klass = mono_class_from_mono_type (field->type);
			} else {
				vt_alloc (field->type, sp);
				stackval_from_data (field->type, sp, (char*)MONO_CLASS_STATIC_FIELDS_BASE(klass) + field->offset);
			}
			++sp;
			BREAK;
		}
		CASE (CEE_STSFLD) {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;

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
			stackval_to_data (field->type, sp, (char*)MONO_CLASS_STATIC_FIELDS_BASE(klass) + field->offset);
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
				if (sp [-1].data.f < 0 || sp [-1].data.f > 18446744073709551615UL)
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
			g_assert (class != NULL);

			sp [-1].type = VAL_OBJ;
			if (class->byval_arg.type == MONO_TYPE_VALUETYPE && !class->enumtype) 
				sp [-1].data.p = mono_value_box (class, sp [-1].data.p);
			else
				sp [-1].data.p = mono_value_box (class, &sp [-1]);
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
			o = (MonoObject*) mono_array_new (class, sp [-1].data.i);
			ip += 4;

			sp [-1].type = VAL_OBJ;
			sp [-1].data.p = o;

			BREAK;
		}
		CASE (CEE_LDLEN) {
			MonoArray *o;

			ip++;

			g_assert (sp [-1].type == VAL_OBJ);

			o = sp [-1].data.p;
			if (!o)
				THROW_EX (mono_get_exception_null_reference (), ip - 1);
			
			g_assert (MONO_CLASS_IS_ARRAY (o->obj.klass));

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

			g_assert (MONO_CLASS_IS_ARRAY (o->obj.klass));
			
			if (sp [1].data.nati >= mono_array_length (o))
				THROW_EX (mono_get_exception_index_out_of_range (), ip - 5);

			/* check the array element corresponds to token */
			esize = mono_array_element_size (o->obj.klass);
			
			sp->type = VAL_MP;
			sp->data.p = mono_array_addr_with_size (o, esize, sp [1].data.i);
			sp->data.vt.klass = o->obj.klass->element_class;

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

			g_assert (MONO_CLASS_IS_ARRAY (o->obj.klass));
			
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

			g_assert (MONO_CLASS_IS_ARRAY (o->obj.klass));
			ac = o->obj.klass;
		    
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
			++ip;
			/* FIXME: handle other cases */
			if (sp [-1].type == VAL_I32) {
				/* defined as NOP */
			} else if(sp [-1].type == VAL_I64) {
				sp [-1].data.i = (gint32)sp [-1].data.l;
				break;
			} else {
				ves_abort();
			}
			BREAK;
		CASE (CEE_CONV_OVF_U4)
			++ip;
			/* FIXME: handle other cases */
			if (sp [-1].type == VAL_I32) {
				/* defined as NOP */
			} else {
				ves_abort();
			}
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
		CASE (CEE_SUB_OVF) ves_abort(); BREAK;
		CASE (CEE_SUB_OVF_UN) ves_abort(); BREAK;
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
					m = get_virtual_method (m, sp);
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
					THROW_EX (mono_get_exception_execution_engine (), ip - 1);
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
				g_assert (sp->type = VAL_VALUETA);
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
				MonoClass *c;
				++ip;
				token = read32 (ip);
				ip += 4;
				c = mono_class_get (image, token);
				sp->type = VAL_I32;
				sp->data.i = mono_class_value_size (c, NULL);
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
		ex_obj = (MonoObject*)frame->ex;
		message = frame->ex->message? mono_string_to_utf8 (frame->ex->message): NULL;
		g_print ("Unhandled exception %s.%s %s.\n", ex_obj->klass->name_space, ex_obj->klass->name,
				message?message:"");
		g_free (message);
		dump_frame (frame);
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
ves_exec (MonoAssembly *assembly, int argc, char *argv[])
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
		args = (MonoArray*)mono_array_new (mono_defaults.string_class, argc);
		for (i=0; i < argc; ++i) {
			mono_array_set (args, gpointer, i, mono_string_new (argv [i]));
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
		 "--trace\n"
		 "--profile\n"
		 "--debug method_name\n"
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

typedef struct {
	char *name;
	gulong offset;
} FieldDesc;

typedef struct {
	char *name;
	FieldDesc *fields;
} ClassDesc;

static FieldDesc 
typebuilder_fields[] = {
	{"tname", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, name)},
	{"nspace", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, nspace)},
	{"parent", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, parent)},
	{"interfaces", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, interfaces)},
	{"methods", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, methods)},
	{"properties", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, properties)},
	{"fields", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, fields)},
	{"attrs", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, attrs)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionTypeBuilder, table_idx)},
	{NULL, 0}
};

static FieldDesc 
modulebuilder_fields[] = {
	{"types", G_STRUCT_OFFSET (MonoReflectionModuleBuilder, types)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionModuleBuilder, table_idx)},
	{NULL, 0}
};

static FieldDesc 
assemblybuilder_fields[] = {
	{"entry_point", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, entry_point)},
	{"modules", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, modules)},
	{"name", G_STRUCT_OFFSET (MonoReflectionAssemblyBuilder, name)},
	{NULL, 0}
};

static FieldDesc 
ctorbuilder_fields[] = {
	{"ilgen", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, ilgen)},
	{"parameters", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, parameters)},
	{"attrs", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, attrs)},
	{"iattrs", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, iattrs)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, table_idx)},
	{"call_conv", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, call_conv)},
	{"type", G_STRUCT_OFFSET (MonoReflectionCtorBuilder, type)},
	{NULL, 0}
};

static FieldDesc 
methodbuilder_fields[] = {
	{"mhandle", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, mhandle)},
	{"rtype", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, rtype)},
	{"parameters", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, parameters)},
	{"attrs", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, attrs)},
	{"iattrs", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, iattrs)},
	{"name", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, name)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, table_idx)},
	{"code", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, code)},
	{"ilgen", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, ilgen)},
	{"type", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, type)},
	{"pinfo", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, pinfo)},
	{"pi_dll", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, dll)},
	{"pi_entry", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, dllentry)},
	{"ncharset", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, charset)},
	{"native_cc", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, native_cc)},
	{"call_conv", G_STRUCT_OFFSET (MonoReflectionMethodBuilder, call_conv)},
	{NULL, 0}
};

static FieldDesc 
fieldbuilder_fields[] = {
	{"attrs", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, attrs)},
	{"type", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, type)},
	{"name", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, name)},
	{"def_value", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, def_value)},
	{"offset", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, offset)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionFieldBuilder, table_idx)},
	{NULL, 0}
};

static FieldDesc 
propertybuilder_fields[] = {
	{"attrs", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, attrs)},
	{"name", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, name)},
	{"type", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, type)},
	{"parameters", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, parameters)},
	{"def_value", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, def_value)},
	{"set_method", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, set_method)},
	{"get_method", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, get_method)},
	{"table_idx", G_STRUCT_OFFSET (MonoReflectionPropertyBuilder, table_idx)},
	{NULL, 0}
};

static FieldDesc 
ilgenerator_fields[] = {
	{"code", G_STRUCT_OFFSET (MonoReflectionILGen, code)},
	{"mbuilder", G_STRUCT_OFFSET (MonoReflectionILGen, mbuilder)},
	{"code_len", G_STRUCT_OFFSET (MonoReflectionILGen, code_len)},
	{"max_stack", G_STRUCT_OFFSET (MonoReflectionILGen, max_stack)},
	{"cur_stack", G_STRUCT_OFFSET (MonoReflectionILGen, cur_stack)},
	{"locals", G_STRUCT_OFFSET (MonoReflectionILGen, locals)},
	{"ex_handlers", G_STRUCT_OFFSET (MonoReflectionILGen, ex_handlers)},
	{NULL, 0}
};

static ClassDesc
emit_classes_to_check [] = {
	{"TypeBuilder", typebuilder_fields},
	{"ModuleBuilder", modulebuilder_fields},
	{"AssemblyBuilder", assemblybuilder_fields},
	{"ConstructorBuilder", ctorbuilder_fields},
	{"MethodBuilder", methodbuilder_fields},
	{"FieldBuilder", fieldbuilder_fields},
	{"PropertyBuilder", propertybuilder_fields},
	{"ILGenerator", ilgenerator_fields},
	{NULL, NULL}
};

static FieldDesc 
delegate_fields[] = {
	{"target_type", G_STRUCT_OFFSET (MonoDelegate, target_type)},
	{"m_target", G_STRUCT_OFFSET (MonoDelegate, target)},
	{"method", G_STRUCT_OFFSET (MonoDelegate, method)},
	{"method_ptr", G_STRUCT_OFFSET (MonoDelegate, method_ptr)},
	{NULL, 0}
};

static ClassDesc
system_classes_to_check [] = {
	{"Delegate", delegate_fields},
	{NULL, NULL}
};

typedef struct {
	char *name;
	ClassDesc *types;
} NameSpaceDesc;

static NameSpaceDesc
namespaces_to_check[] = {
	{"System.Reflection.Emit", emit_classes_to_check},
	{"System", system_classes_to_check},
	{NULL, NULL}
};

static void
check_corlib (MonoImage *corlib)
{
	MonoClass *klass;
	MonoClassField *field;
	FieldDesc *fdesc;
	ClassDesc *cdesc;
	NameSpaceDesc *ndesc;

	for (ndesc = namespaces_to_check; ndesc->name; ++ndesc) {
		for (cdesc = ndesc->types; cdesc->name; ++cdesc) {
			klass = mono_class_from_name (corlib, ndesc->name, cdesc->name);
			if (!klass)
				g_error ("Cannot find class %s", cdesc->name);
			mono_class_init (klass);
			for (fdesc = cdesc->fields; fdesc->name; ++fdesc) {
				field = mono_class_get_field_from_name (klass, fdesc->name);
				if (!field || (field->offset != fdesc->offset))
					g_error ("field `%s' mismatch in class %s (%ld != %ld)", fdesc->name, cdesc->name, (long) fdesc->offset, (long) (field?field->offset:-1));
			}
		}
	}
}

static gint
compare_profile (MethodProfile *profa, MethodProfile *profb)
{
	return profb->total - profa->total;
}

static void
build_profile (MonoMethod *m, MethodProfile *prof, GList **funcs)
{
	g_timer_elapsed (prof->u.timer, &prof->total);
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
		g_snprintf (buf, sizeof (buf), "%s.%s::%s",
			p->u.method->klass->name_space, p->u.method->klass->name,
			p->u.method->name);
		printf ("%-52s %7ld %7ld %7ld\n", buf,
			p->total/1000, p->count, p->total/p->count/1000);
	}
}

static void
segv_handler (int signum)
{
	signal (signum, segv_handler);
	mono_raise_exception (mono_get_exception_null_reference ());
}

int 
main (int argc, char *argv [])
{
	MonoAssembly *assembly;
	GList *profile = NULL;
	int retval = 0, i, ocount = 0;
	char *file;

	if (argc < 2)
		usage ();

	for (i = 1; i < argc && argv [i][0] == '-'; i++){
		if (strcmp (argv [i], "--trace") == 0)
			tracing = 1;
		if (strcmp (argv [i], "--traceops") == 0)
			tracing = 2;
		if (strcmp (argv [i], "--profile") == 0)
			profiling = g_hash_table_new (g_direct_hash, g_direct_equal);
		if (strcmp (argv [i], "--opcode-count") == 0)
			ocount = 1;
		if (strcmp (argv [i], "--help") == 0)
			usage ();
#if DEBUG_INTERP
		if (strcmp (argv [i], "--debug") == 0)
			db_methods = g_list_append (db_methods, argv [++i]);
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

	mono_install_handler (interp_ex_handler);

	mono_init ();
	mono_appdomain_init (file);
	mono_thread_init();
	mono_network_init();

	assembly = mono_assembly_open (file, NULL, NULL);
	if (!assembly){
		fprintf (stderr, "Can not open image %s\n", file);
		exit (1);
	}


#ifdef RUN_TEST
	test_load_class (assembly->image);
#else
	check_corlib (mono_defaults.corlib);
	signal (SIGSEGV, segv_handler);
	/*
	 * skip the program name from the args.
	 */
	++i;
	retval = ves_exec (assembly, argc - i, argv + i);
#endif
	if (profiling)
		g_hash_table_foreach (profiling, build_profile, &profile);
	output_profile (profile);
	
	mono_network_cleanup();
	mono_thread_cleanup();

	mono_assembly_close (assembly);

#if DEBUG_INTERP
	if (ocount) {
		fprintf (stderr, "opcode count: %ld\n", opcode_count);
		fprintf (stderr, "fcall count: %ld\n", fcall_count);
	}
#endif
	return retval;
}



