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
#include <config.h>
#include <stdio.h>
#include <string.h>
#include <glib.h>
#include <ffi.h>


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
#include <mono/metadata/endian.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/blob.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/loader.h>
//#include <mono/cli/types.h>
#include "interp.h"
#include "hacks.h"

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

 static int debug_indent_level = 0;

#define GET_NATI(sp) ((guint32)(sp).data.i)
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
	} while (0)

static void ves_exec_method (MonoInvocation *frame);

typedef void (*ICallMethod) (MonoInvocation *frame);

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
	exit (1);
}
#define ves_abort() ves_real_abort(__LINE__, frame->method, ip, frame->stack, sp)

/*
 * init_class:
 * @klass: klass that needs to be initialized
 *
 * This routine calls the class constructor for @class if it needs it.
 */
static void
init_class (MonoClass *klass)
{
	MonoMethod *method;
	MonoInvocation call;
	int i;

	if (!klass->metadata_inited)
		mono_class_metadata_init (klass);

	if (klass->inited)
		return;
	if (klass->parent && !klass->parent->inited)
		init_class (klass->parent);
	
	klass->inited = 1;

	klass->data = g_malloc0 (klass->class_size);

	for (i = 0; i < klass->method.count; ++i) {
		method = klass->methods [i];
		if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && (strcmp (".cctor", method->name) == 0)) {
			INIT_FRAME (&call, NULL, NULL, NULL, NULL, method);
	
			ves_exec_method (&call);
			mono_free_method (call.method);
			return;
		}
	}
	/* No class constructor found */
}

/*
 * newobj:
 * @image: image where the object is being referenced
 * @token: method token to invoke
 *
 * This routine creates a new object based on the class where the
 * constructor lives.x
 */
static MonoObject *
newobj (MonoImage *image, guint32 token)
{
	MonoObject *result = NULL;
	
	switch (mono_metadata_token_code (token)){
	case MONO_TOKEN_METHOD_DEF: {
		guint32 idx;

		idx = mono_metadata_typedef_from_method (image, token);

		result = mono_new_object_from_token (image, MONO_TOKEN_TYPE_DEF | idx);
		break;
	}
	case MONO_TOKEN_MEMBER_REF: {
		guint32 member_cols [MONO_MEMBERREF_SIZE];
		guint32 mpr_token, table, idx;
		
		mono_metadata_decode_row (
			&image->tables [MONO_TABLE_MEMBERREF],
			mono_metadata_token_index (token) - 1,
			member_cols, CSIZE (member_cols));
		mpr_token = member_cols [MONO_MEMBERREF_CLASS];
		table = mpr_token & 7;
		idx = mpr_token >> 3;
		
		if (strcmp (mono_metadata_string_heap (image, member_cols[1]), ".ctor"))
			g_error ("Unhandled: call to non constructor");

		switch (table){
		case 0: /* TypeDef */
			result = mono_new_object_from_token (image, MONO_TOKEN_TYPE_DEF | idx);
			break;
		case 1: /* TypeRef */
			result = mono_new_object_from_token (image, MONO_TOKEN_TYPE_REF | idx);
			break;
		case 2: /* ModuleRef */
			g_error ("Unhandled: ModuleRef");
			
		case 3: /* MethodDef */
			g_error ("Unhandled: MethodDef");
			
		case 4: /* TypeSpec */			
			result = mono_new_object_from_token (image, MONO_TOKEN_TYPE_SPEC | idx);
		}
		break;
	}
	default:
		g_warning ("dont know how to handle token %08x\n", token); 
		g_assert_not_reached ();
	}
	
	if (result)
		init_class (result->klass);
	return result;
}

static MonoMethod*
get_virtual_method (MonoImage *image, guint32 token, stackval *args)
{
	switch (mono_metadata_token_table (token)) {
	case MONO_TABLE_METHOD:
	case MONO_TABLE_MEMBERREF:
		return mono_get_method (image, token, NULL);
	}
	g_error ("got virtual method: 0x%x\n", token);
	return NULL;
}

static MonoObject*
get_named_exception (const char *name)
{
	MonoClass *klass;
	MonoInvocation call;
	MonoMethod *method = NULL;
	MonoObject *o;
	int i;

	klass = mono_class_from_name (mono_defaults.corlib, "System", name);

	o = mono_new_object (klass);
	g_assert (o != NULL);

	for (i = 0; i < klass->method.count; ++i) {
		if (!strcmp (".ctor", klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == 0) {
			method = klass->methods [i];
			break;
		}
	}

	call.obj = o;

	g_assert (method);
	INIT_FRAME (&call, NULL, o, NULL, NULL, method);

	ves_exec_method (&call);
	return o;
}

static MonoObject*
get_exception_divide_by_zero ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = get_named_exception ("DivideByZeroException");
	return ex;
}

static MonoObject*
get_exception_security ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = get_named_exception ("SecurityException");
	return ex;
}

static MonoObject*
get_exception_arithmetic ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = get_named_exception ("ArithmeticException");
	return ex;
}

static MonoObject*
get_exception_overflow ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = get_named_exception ("OverflowException");
	return ex;
}

static MonoObject*
get_exception_null_reference ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = get_named_exception ("NullReferenceException");
	return ex;
}

static MonoObject*
get_exception_execution_engine ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = get_named_exception ("ExecutionEngineException");
	return ex;
}

static void
stackval_from_data (MonoType *type, stackval *result, const char *data)
{
	if (type->byref) {
		result->type = VAL_OBJ;
		result->data.p = *(gpointer*)data;
		return;
	}
	switch (type->type) {
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
	case MONO_TYPE_U4:
		result->type = VAL_I32;
		result->data.i = *(guint32*)data;
		return;
	case MONO_TYPE_R4:
		result->type = VAL_DOUBLE;
		result->data.f = *(float*)data;
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
		return;
	default:
		g_warning ("got type %x", type->type);
		g_assert_not_reached ();
	}
}

static void
stackval_to_data (MonoType *type, stackval *val, char *data)
{
	if (type->byref) {
		*(gpointer*)data = val->data.p;
		return;
	}
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		*(guint8*)data = val->data.i;
		break;
	case MONO_TYPE_BOOLEAN:
		*(guint8*)data = (val->data.i != 0);
		break;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		*(guint16*)data = val->data.i;
		break;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		*(gint32*)data = val->data.i;
		break;
	case MONO_TYPE_R4:
		*(float*)data = val->data.f;
		break;
	case MONO_TYPE_R8:
		*(double*)data = val->data.f;
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_PTR:
		*(gpointer*)data = val->data.p;
		break;
	default:
		g_warning ("got type %x", type->type);
		g_assert_not_reached ();
	}
}

static char *
mono_get_ansi_string (MonoObject *o)
{
	MonoStringObject *s = (MonoStringObject *)o;
	char *as, *vector;
	int i;

	g_assert (o != NULL);

	if (!s->length)
		return g_strdup ("");

	vector = s->c_str->vector;

	g_assert (vector != NULL);

	as = g_malloc (s->length + 1);

	/* fixme: replace with a real unicode/ansi conversion */
	for (i = 0; i < s->length; i++) {
		as [i] = vector [i*2];
	}

	as [i] = '\0';

	return as;
}

static void 
ves_pinvoke_method (MonoInvocation *frame)
{
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)frame->method;
	MonoMethodSignature *sig = frame->method->signature;
	gpointer *values;
	float *tmp_float;
	char **tmp_string;
	int i, acount, rsize, align;
	stackval *sp = frame->stack_args;
	gpointer res = NULL; 
	GSList *t, *l = NULL;

	acount = sig->param_count;

	values = alloca (sizeof (gpointer) * acount);

	/* fixme: only works on little endian machines */

	for (i = 0; i < acount; i++) {

		switch (sig->params [i]->type->type) {

		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			values[i] = &sp [i].data.i;
			break;
		case MONO_TYPE_R4:
			tmp_float = alloca (sizeof (float));
			*tmp_float = sp [i].data.f;
			values[i] = tmp_float;
			break;
		case MONO_TYPE_R8:
			values[i] = &sp [i].data.f;
			break;
		case MONO_TYPE_STRING:
			g_assert (sp [i].type == VAL_OBJ);

			if (frame->method->flags & PINVOKE_ATTRIBUTE_CHAR_SET_ANSI && sp [i].data.p) {
				tmp_string = alloca (sizeof (char *));
				*tmp_string = mono_get_ansi_string (sp [i].data.p);
				l = g_slist_prepend (l, *tmp_string);
				values[i] = tmp_string;				
			} else {
				/* 
				 * fixme: may we pass the object - I assume 
				 * that is wrong ?? 
				 */
				values[i] = &sp [i].data.p;
			}
			
			break;
 		default:
			g_warning ("not implemented %x", 
				   sig->params [i]->type->type);
			g_assert_not_reached ();
		}

	}

	if ((rsize = mono_type_size (sig->ret->type, &align)))
		res = alloca (rsize);

	ffi_call (piinfo->cif, frame->method->addr, res, values);
		
	t = l;
	while (t) {
		g_free (t->data);
		t = t->next;
	}

	g_slist_free (l);

	if (sig->ret->type->type != MONO_TYPE_VOID)
		stackval_from_data (sig->ret->type, frame->retval, res);
}

#define DEBUG_INTERP 0
#if DEBUG_INTERP
#define OPDEF(a,b,c,d,e,f,g,h,i,j)  b,
static char *opcode_names[] = {
#include "mono/cil/opcode.def"
	NULL
};
#undef OPDEF

static void
output_indent (void)
{
	int h;

	for (h = 0; h < debug_indent_level; h++)
		g_print ("  ");
}

static void
dump_stack (stackval *stack, stackval *sp)
{
	stackval *s = stack;
	
	if (sp == stack)
		return;
	
	output_indent ();
	g_print ("stack: ");
		
	while (s < sp) {
		switch (s->type) {
		case VAL_I32: g_print ("[%d] ", s->data.i); break;
		case VAL_I64: g_print ("[%lld] ", s->data.l); break;
		case VAL_DOUBLE: g_print ("[%0.5f] ", s->data.f); break;
		default: g_print ("[%p] ", s->data.p); break;
		}
		++s;
	}
}

#define DEBUG_ENTER()	\
	do {	\
		MonoClass *klass = frame->method->klass;	\
		debug_indent_level++;	\
		output_indent ();	\
		g_print ("Entering %s.%s::%s\n", klass->name_space, klass->name, frame->method->name);	\
	} while (0)
#define DEBUG_LEAVE()	\
	do {	\
		MonoClass *klass = frame->method->klass;	\
		output_indent ();	\
		g_print ("Leaving %s.%s::%s\n", klass->name_space, klass->name, frame->method->name);	\
		debug_indent_level--;	\
	} while (0)

#else

#define DEBUG_ENTER()
#define DEBUG_LEAVE()

#endif

static MonoType 
method_this = {
	MONO_TYPE_CLASS, 
	0, 
	1, /* byref */
	0,
	{0}
};

#define LOCAL_POS(n)            (locals_pointers [(n)])
#define LOCAL_TYPE(header, n)   ((header)->locals [(n)])

#define ARG_POS(n)              (args_pointers [(n)])
#define ARG_TYPE(sig, n)        ((n) ? (sig)->params [(n) - (sig)->hasthis]->type : \
				(sig)->hasthis ? &method_this: (sig)->params [(0)]->type)

#define THROW_EX(exception,ex_ip)	\
		do {\
			frame->ip = (ex_ip);		\
			frame->ex = (exception);	\
			goto handle_exception;	\
		} while (0)

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
ves_exec_method (MonoInvocation *frame)
{
	MonoInvocation child_frame;
	MonoMethodHeader *header;
	MonoMethodSignature *signature;
	MonoImage *image;
	const unsigned char *endfinally_ip;
	register const unsigned char *ip;
	register stackval *sp;
	void **locals_pointers;
	void **args_pointers;
	unsigned char tail_recursion = 0;
	unsigned char unaligned_address = 0;
	unsigned char volatile_address = 0;
	GOTO_LABEL_VARS;

	if (!frame->method->klass->inited)
		init_class (frame->method->klass);

	if (frame->method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		ICallMethod icall = frame->method->addr;
		icall (frame);
		return;
	} 

	if (frame->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		ves_pinvoke_method (frame);
		return;
	} 
		
	header = ((MonoMethodNormal *)frame->method)->header;
	signature = frame->method->signature;
	image = frame->method->klass->image;

	DEBUG_ENTER ();

	/*
	 * with alloca we get the expected huge performance gain
	 * stackval *stack = g_new0(stackval, header->max_stack);
	 */

	sp = frame->stack = alloca (sizeof (stackval) * header->max_stack);

	if (header->num_locals) {
		int i, align, size, offset = 0;

		frame->locals = alloca (header->locals_size);
		locals_pointers = alloca (sizeof(void*) * header->num_locals);
		/* 
		 * yes, we do it unconditionally, because it needs to be done for
		 * some cases anyway and checking for that would be even slower.
		 */
		memset (frame->locals, 0, header->locals_size);
		for (i = 0; i < header->num_locals; ++i) {
			locals_pointers [i] = frame->locals + offset;
			size = mono_type_size (header->locals [i], &align);
			offset += offset % align;
			offset += size;
		}
	}
	/*
	 * Copy args from stack_args to args.
	 */
	if (signature->params_size) {
		int i, align, size, offset = 0;
		int has_this = signature->hasthis;

		frame->args = alloca (signature->params_size);
		args_pointers = alloca (sizeof(void*) * (signature->param_count + has_this));
		if (has_this) {
			args_pointers [0] = frame->args;
			*(gpointer*) frame->args = frame->obj;
			offset += sizeof (gpointer);
		}
		for (i = 0; i < signature->param_count; ++i) {
			args_pointers [i + has_this] = frame->args + offset;
			stackval_to_data (signature->params [i]->type, frame->stack_args + i, frame->args + offset);
			size = mono_type_size (signature->params [i]->type, &align);
			offset += offset % align;
			offset += size;
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
		dump_stack (frame->stack, sp);
		g_print ("\n");
		output_indent ();
		g_print ("0x%04x: %s\n", ip-header->code,
			 *ip == 0xfe ? opcode_names [256 + ip [1]] : opcode_names [*ip]);
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
			stackval_from_data (LOCAL_TYPE (header, n), sp, LOCAL_POS (n));
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
			stackval_to_data (LOCAL_TYPE (header, n), sp, LOCAL_POS (n));
			BREAK;
		}
		CASE (CEE_LDARG_S)
			++ip;
			stackval_from_data (ARG_TYPE (signature, *ip), sp, ARG_POS (*ip));
			++sp;
			++ip;
			BREAK;
		CASE (CEE_LDARGA_S)
			++ip;
			sp->type = VAL_TP;
			sp->data.p = ARG_POS (*ip);
			++sp;
			++ip;
			BREAK;
		CASE (CEE_STARG_S) ves_abort(); BREAK;
		CASE (CEE_LDLOC_S)
			++ip;
			stackval_from_data (LOCAL_TYPE (header, *ip), sp, LOCAL_POS (*ip));
			++ip;
			++sp;
			BREAK;
		CASE (CEE_LDLOCA_S)
			++ip;
			sp->type = VAL_TP;
			sp->data.p = LOCAL_POS (*ip);
			++sp;
			++ip;
			BREAK;
		CASE (CEE_STLOC_S)
			++ip;
			--sp;
			stackval_to_data (LOCAL_TYPE (header, *ip), sp, LOCAL_POS (*ip));
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
			sp->data.i = *ip; /* FIXME: signed? */
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
			sp->data.i = read64 (ip);
			ip += 8;
			++sp;
			BREAK;
		CASE (CEE_LDC_R4)
			++ip;
			sp->type = VAL_DOUBLE;
			/* FIXME: ENOENDIAN */
			sp->data.f = *(float*)(ip);
			ip += sizeof (float);
			++sp;
			BREAK;
		CASE (CEE_LDC_R8) 
			++ip;
			sp->type = VAL_DOUBLE;
			/* FIXME: ENOENDIAN */
			sp->data.f = *(double*) (ip);
			ip += sizeof (double);
			++sp;
			BREAK;
		CASE (CEE_UNUSED99) ves_abort (); BREAK;
		CASE (CEE_DUP) 
			*sp = sp [-1]; 
			++sp; 
			++ip; 
			BREAK;
		CASE (CEE_POP)
			++ip;
			--sp;
			BREAK;
		CASE (CEE_JMP) ves_abort(); BREAK;
		CASE (CEE_CALLVIRT) /* Fall through */
		CASE (CEE_CALL) {
			MonoMethodSignature *csignature;
			stackval retval;
			guint32 token;
			int virtual = *ip == CEE_CALLVIRT;

			/*
			 * We ignore tail recursion for now.
			 */
			tail_recursion = 0;

			frame->ip = ip;
			
			++ip;
			token = read32 (ip);
			ip += 4;
			if (virtual)
				child_frame.method = get_virtual_method (image, token, sp);
			else
				child_frame.method = mono_get_method (image, token, NULL);
			csignature = child_frame.method->signature;
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
				g_assert (sp->type == VAL_OBJ);
				child_frame.obj = sp->data.p;
			} else {
				child_frame.obj = NULL;
			}
			if (csignature->ret->type->type != MONO_TYPE_VOID) {
				/* FIXME: handle valuetype */
				child_frame.retval = &retval;
			} else {
				child_frame.retval = NULL;
			}

			child_frame.ex = NULL;
			child_frame.ex_handler = NULL;

			ves_exec_method (&child_frame);

			if (child_frame.ex) {
				/*
				 * An exception occurred, need to run finally, fault and catch handlers..
				 */
				frame->ex = child_frame.ex;
				goto handle_finally;
			}

			/* need to handle typedbyref ... */
			if (csignature->ret->type->type != MONO_TYPE_VOID) {
				*sp = retval;
				sp++;
			}
			BREAK;
		}
		CASE (CEE_CALLI) ves_abort(); BREAK;
		CASE (CEE_RET)
			if (signature->ret->type->type != MONO_TYPE_VOID) {
				--sp;
				*frame->retval = *sp;
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
				result = sp [0].data.i == GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l == sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f == sp [1].data.f;
			else
				result = GET_NATI (sp [0]) == GET_NATI (sp [1]);
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
				result = sp [0].data.i >= GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l >= sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f >= sp [1].data.f;
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
		CASE (CEE_BGT) /* Fall through */
		CASE (CEE_BGT_S) {
			int result;
			int near_jump = *ip == CEE_BGT_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = sp [0].data.i > GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l > sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f > sp [1].data.f;
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
		CASE (CEE_BLT) /* Fall through */
		CASE (CEE_BLT_S) {
			int result;
			int near_jump = *ip == CEE_BLT_S;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = sp[0].data.i < GET_NATI(sp[1]);
			else if (sp->type == VAL_I64)
				result = sp[0].data.l < sp[1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp[0].data.f < sp[1].data.f;
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
		CASE (CEE_BLE) /* fall through */
		CASE (CEE_BLE_S) {
			int result;
			int near_jump = *ip == CEE_BLE_S;
			++ip;
			sp -= 2;

			if (sp->type == VAL_I32)
				result = sp [0].data.i <= GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l <= sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f <= sp [1].data.f;
			else {
				/*
				 * FIXME: here and in other places GET_NATI on the left side 
				 * _will_ be wrong when we change the macro to work on 64 buts 
				 * systems.
				 */
				result = GET_NATI (sp [0]) <= GET_NATI (sp [1]);
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
			BREAK;
		CASE (CEE_STIND_REF)
			++ip;
			sp -= 2;
			*(gpointer*)sp->data.p = sp[1].data.p;
			BREAK;
		CASE (CEE_STIND_I1)
			++ip;
			sp -= 2;
			*(gint8*)sp->data.p = (gint8)sp[1].data.i;
			BREAK;
		CASE (CEE_STIND_I2)
			++ip;
			sp -= 2;
			*(gint16*)sp->data.p = (gint16)sp[1].data.i;
			BREAK;
		CASE (CEE_STIND_I4)
			++ip;
			sp -= 2;
			*(gint32*)sp->data.p = sp[1].data.i;
			BREAK;
		CASE (CEE_STIND_I)
			++ip;
			sp -= 2;
			*(gint64*)sp->data.p = sp[1].data.l;
			BREAK;
		CASE (CEE_STIND_I8)
			++ip;
			sp -= 2;
			*(gint64*)sp->data.p = sp[1].data.l;
			BREAK;
		CASE (CEE_STIND_R4)
			++ip;
			sp -= 2;
			*(gfloat*)sp->data.p = (gfloat)sp[1].data.f;
			BREAK;
		CASE (CEE_STIND_R8)
			++ip;
			sp -= 2;
			*(gdouble*)sp->data.p = sp[1].data.f;
			BREAK;
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
			else
				(char*)sp [-1].data.p += GET_NATI (sp [0]);
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
			else
				(char*)sp [-1].data.p -= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_MUL)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i *= GET_NATI (sp [0]);
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
					THROW_EX (get_exception_divide_by_zero (), ip - 1);
				sp [-1].data.i /= GET_NATI (sp [0]);
			} else if (sp->type == VAL_I64) {
				if (sp [0].data.l == 0)
					THROW_EX (get_exception_divide_by_zero (), ip - 1);
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
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (get_exception_divide_by_zero (), ip - 1);
				(guint32)sp [-1].data.i /= (guint32)GET_NATI (sp [0]);
			} else if (sp->type == VAL_I64) {
				if (sp [0].data.l == 0)
					THROW_EX (get_exception_divide_by_zero (), ip - 1);
				(guint64)sp [-1].data.l /= (guint64)sp [0].data.l;
			} else if (sp->type == VAL_NATI) {
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (get_exception_divide_by_zero (), ip - 1);
				(gulong)sp [-1].data.p /= (gulong)sp [0].data.p;
			}
			BREAK;
		CASE (CEE_REM)
			++ip;
			--sp;
			if (sp->type == VAL_I32) {
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (get_exception_divide_by_zero (), ip - 1);
				sp [-1].data.i %= GET_NATI (sp [0]);
			} else if (sp->type == VAL_I64) {
				if (sp [0].data.l == 0)
					THROW_EX (get_exception_divide_by_zero (), ip - 1);
				sp [-1].data.l %= sp [0].data.l;
			} else if (sp->type == VAL_DOUBLE) {
				/* FIXME: what do we actually do here? */
				sp [-1].data.f = 0;
			} else {
				if (GET_NATI (sp [0]) == 0)
					THROW_EX (get_exception_divide_by_zero (), ip - 1);
				GET_NATI (sp [-1]) %= GET_NATI (sp [0]);
			}
			BREAK;
		CASE (CEE_REM_UN) ves_abort(); BREAK;
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
				GET_NATI (sp [-1]) >>= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHR_UN) ves_abort(); BREAK;
		CASE (CEE_NEG)
			++ip;
			if (sp->type == VAL_I32)
				sp->data.i = - sp->data.i;
			else if (sp->type == VAL_I64)
				sp->data.l = - sp->data.l;
			else if (sp->type == VAL_DOUBLE)
				sp->data.f = - sp->data.f;
			else if (sp->type == VAL_NATI)
				sp->data.p = (gpointer)(- (int)sp->data.p);
			BREAK;
		CASE (CEE_NOT)
			++ip;
			if (sp->type == VAL_I32)
				sp->data.i = ~ sp->data.i;
			else if (sp->type == VAL_I64)
				sp->data.l = ~ sp->data.l;
			else if (sp->type == VAL_NATI)
				sp->data.p = (gpointer)(~ (int)sp->data.p);
			BREAK;
		CASE (CEE_CONV_I1) ves_abort(); BREAK;
		CASE (CEE_CONV_I2) ves_abort(); BREAK;
		CASE (CEE_CONV_I4) {
			++ip;
			/* FIXME: handle other cases. what about sign? */

			switch (sp [-1].type) {
			case VAL_DOUBLE:
				sp [-1].data.i = (gint32)sp [-1].data.f;
				sp [-1].type = VAL_I32;
				break;
			case VAL_I32:
				break;
			default:
				ves_abort();
			}
			BREAK;
		}
		CASE (CEE_CONV_I8) ves_abort(); BREAK;
		CASE (CEE_CONV_R4) ves_abort(); BREAK;
		CASE (CEE_CONV_R8)
			++ip;
			/* FIXME: handle other cases. what about sign? */
			if (sp [-1].type == VAL_I32) {
				sp [-1].data.f = (double)sp [-1].data.i;
				sp [-1].type = VAL_DOUBLE;
			} else {
				ves_abort();
			}
			BREAK;
		CASE (CEE_CONV_U4)
			++ip;
			/* FIXME: handle other cases. what about sign? */
			if (sp [-1].type == VAL_DOUBLE) {
				sp [-1].data.i = (guint32)sp [-1].data.f;
				sp [-1].type = VAL_I32;
			} else {
				ves_abort();
			}
			BREAK;
		CASE (CEE_CONV_U) 
			++ip;
			/* FIXME: handle other cases */
			if (sp [-1].type == VAL_I32) {
				/* defined as NOP */
			} else {
				ves_abort();
			}
			BREAK;
		CASE (CEE_CONV_U8) ves_abort(); BREAK;
		CASE (CEE_CPOBJ) ves_abort(); BREAK;
		CASE (CEE_LDOBJ) ves_abort(); BREAK;
		CASE (CEE_LDSTR) {
			MonoObject *o;
			const char *name;
			int len;
			guint32 index;

			ip++;
			index = mono_metadata_token_index (read32 (ip));
			ip += 4;

			name = mono_metadata_user_string (image, index);
			len = mono_metadata_decode_blob_size (name, &name);

			o = mono_new_utf16_string (name, len);

			sp->type = VAL_OBJ;
			sp->data.p = o;

			++sp;
			BREAK;
		}
		CASE (CEE_NEWOBJ) {
			MonoObject *o;
			MonoMethodSignature *csig;
			guint32 token;

			frame->ip = ip;

			ip++;
			token = read32 (ip);
			o = newobj (image, token);
			ip += 4;
			
			/* call the contructor */
			child_frame.method = mono_get_method (image, token, o->klass);
			csig = child_frame.method->signature;

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
			sp->type = VAL_OBJ;
			sp->data.p = o;
			++sp;
			BREAK;
		}
		CASE (CEE_CASTCLASS) {
			MonoObject *o;
			MonoClass *c;
			guint32 token;
			gboolean found = FALSE;

			++ip;
			token = read32 (ip);

			g_assert (sp [-1].type == VAL_OBJ);

			if ((o = sp [-1].data.p)) {
				c = o->klass;

				/* 
				 * fixme: this only works for class casts, but not for 
				 * interface casts. 
				 */
				while (c) {
					if (c->type_token == token) {
						found = TRUE;
						break;
					}
					c = c->parent;
				}

				g_assert (found);

			}

			ip += 4;
			BREAK;
		}
		CASE (CEE_ISINST) ves_abort(); BREAK;
		CASE (CEE_CONV_R_UN) ves_abort(); BREAK;
		CASE (CEE_UNUSED58) ves_abort(); BREAK;
		CASE (CEE_UNUSED1) ves_abort(); BREAK;
		CASE (CEE_UNBOX) {
			MonoObject *o;
			MonoClass *c;
			guint32 token;

			++ip;
			token = read32 (ip);
			
			c = mono_class_get (image, token);
			
			o = sp [-1].data.p;

			g_assert (o->klass->type_token == c->type_token);

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
			guint32 token;
			int load_addr = *ip == CEE_LDFLDA;

			++ip;
			token = read32 (ip);
			ip += 4;
			
			g_assert (sp [-1].type == VAL_OBJ);
			obj = sp [-1].data.p;
			field = mono_class_get_field (obj->klass, token);
			g_assert (field);
			if (load_addr) {
				sp->type = VAL_TP;
				sp->data.p = (char*)obj + field->offset;
			} else {
				stackval_from_data (field->type->type, &sp [-1], (char*)obj + field->offset);
			}
			BREAK;
		}
		CASE (CEE_STFLD) {
			MonoObject *obj;
			MonoClassField *field;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			
			sp -= 2;
			
			g_assert (sp [0].type == VAL_OBJ);
			obj = sp [0].data.p;
			field = mono_class_get_field (obj->klass, token);
			g_assert (field);
			stackval_to_data (field->type->type, &sp [1], (char*)obj + field->offset);
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
			klass = mono_class_get (image, 
				MONO_TOKEN_TYPE_DEF | mono_metadata_typedef_from_field (image, token & 0xffffff));
			if (!klass->inited)
				init_class (klass);
			field = mono_class_get_field (klass, token);
			g_assert (field);
			if (load_addr) {
				sp->type = VAL_TP;
				sp->data.p = (char*)klass + field->offset;
			} else {
				stackval_from_data (field->type->type, sp, MONO_CLASS_STATIC_FIELDS_BASE(klass) + field->offset);
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
			klass = mono_class_get (image, 
				MONO_TOKEN_TYPE_DEF | mono_metadata_typedef_from_field (image, token & 0xffffff));
			if (!klass->inited)
				init_class (klass);
			field = mono_class_get_field (klass, token);
			g_assert (field);
			stackval_to_data (field->type->type, sp, MONO_CLASS_STATIC_FIELDS_BASE(klass) + field->offset);
			BREAK;
		}
		CASE (CEE_STOBJ) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I1_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I2_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I4_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I8_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U1_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U2_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U4_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U8_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U_UN) ves_abort(); BREAK;
		CASE (CEE_BOX) {
			guint32 token;
			MonoClass *class;

			ip++;
			token = read32 (ip);

			class = mono_class_get (image, token);
			g_assert (class != NULL);

			sp [-1].type = VAL_OBJ;
			sp [-1].data.p = mono_value_box (class, &sp [-1]);

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
			o = mono_new_szarray (class, sp [-1].data.i);
			ip += 4;

			sp [-1].type = VAL_OBJ;
			sp [-1].data.p = o;

			BREAK;
		}
		CASE (CEE_LDLEN) {
			MonoArrayObject *o;

			ip++;

			g_assert (sp [-1].type == VAL_OBJ);

			o = sp [-1].data.p;
			g_assert (o != NULL);
			
			g_assert (MONO_CLASS_IS_ARRAY (o->obj.klass));

			sp [-1].type = VAL_I32;
			sp [-1].data.i = o->bounds [0].length;

			BREAK;
		}
		CASE (CEE_LDELEMA) ves_abort(); BREAK;
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
			MonoArrayObject *o;

			sp -= 2;

			g_assert (sp [0].type == VAL_OBJ);
			o = sp [0].data.p;

			g_assert (MONO_CLASS_IS_ARRAY (o->obj.klass));
			
			g_assert (sp [1].data.i >= 0);
			g_assert (sp [1].data.i < o->bounds [0].length);
			
			switch (*ip) {
			case CEE_LDELEM_I1:
				sp [0].data.i = ((gint8 *)o->vector)[sp [1].data.i]; 
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_U1:
				sp [0].data.i = ((guint8 *)o->vector)[sp [1].data.i]; 
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_I2:
				sp [0].data.i = ((gint16 *)o->vector)[sp [1].data.i]; 
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_U2:
				sp [0].data.i = ((guint16 *)o->vector)[sp [1].data.i]; 
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_I:
				sp [0].data.i = ((gint32 *)o->vector)[sp [1].data.i]; 
				sp [0].type = VAL_NATI;
				break;
			case CEE_LDELEM_I4:
				sp [0].data.i = ((gint32 *)o->vector)[sp [1].data.i]; 
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_U4:
				sp [0].data.i = ((guint32 *)o->vector)[sp [1].data.i]; 
				sp [0].type = VAL_I32;
				break;
			case CEE_LDELEM_I8:
				sp [0].data.l = ((gint64 *)o->vector)[sp [1].data.i]; 
				sp [0].type = VAL_I64;
				break;
			case CEE_LDELEM_R4:
				sp [0].data.f = ((float *)o->vector)[sp [1].data.i]; 
				sp [0].type = VAL_DOUBLE;
				break;
			case CEE_LDELEM_R8:
				sp [0].data.i = ((double *)o->vector)[sp [1].data.i]; 
				sp [0].type = VAL_DOUBLE;
				break;
			case CEE_LDELEM_REF:
				sp [0].data.p = ((gpointer *)o->vector)[sp [1].data.i]; 
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
			MonoArrayObject *o;
			MonoArrayClass *ac;
			MonoObject *v;

			sp -= 3;

			g_assert (sp [0].type == VAL_OBJ);
			o = sp [0].data.p;

			g_assert (MONO_CLASS_IS_ARRAY (o->obj.klass));
			ac = (MonoArrayClass *)o->obj.klass;
		    
			g_assert (sp [1].data.i >= 0);
			g_assert (sp [1].data.i < o->bounds [0].length);

			switch (*ip) {
			case CEE_STELEM_I:
				((gint32 *)o->vector)[sp [1].data.i] = 
					sp [2].data.i;
				break;
			case CEE_STELEM_I1:
				((gint8 *)o->vector)[sp [1].data.i] = 
					sp [2].data.i;
				break;
			case CEE_STELEM_I2:
				((gint16 *)o->vector)[sp [1].data.i] = 
					sp [2].data.i;
				break;
			case CEE_STELEM_I4:
				((gint32 *)o->vector)[sp [1].data.i] = 
					sp [2].data.i;
				break;
			case CEE_STELEM_I8:
				((gint64 *)o->vector)[sp [1].data.i] = 
					sp [2].data.l;
				break;
			case CEE_STELEM_R4:
				((float *)o->vector)[sp [1].data.i] = 
					sp [2].data.f;
				break;
			case CEE_STELEM_R8:
				((double *)o->vector)[sp [1].data.i] = 
					sp [2].data.f;
				break;
			case CEE_STELEM_REF:
				g_assert (sp [2].type == VAL_OBJ);
			
				v = sp [2].data.p;

				//fixme: what about type conversions ?
				g_assert (v->klass == ac->element_class);

				((gpointer *)o->vector)[sp [1].data.i] = 
					sp [2].data.p;
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
		CASE (CEE_CONV_OVF_I1) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U1) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I2) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U2) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I4) ves_abort(); BREAK;
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
		CASE (CEE_CKFINITE) ves_abort(); BREAK;
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
		CASE (CEE_LDTOKEN)
		CASE (CEE_CONV_U2) ves_abort(); BREAK;
		CASE (CEE_CONV_U1) ves_abort(); BREAK;
		CASE (CEE_CONV_I) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U) ves_abort(); BREAK;
		CASE (CEE_ADD_OVF) ves_abort(); BREAK;
		CASE (CEE_ADD_OVF_UN) ves_abort(); BREAK;
		CASE (CEE_MUL_OVF) ves_abort(); BREAK;
		CASE (CEE_MUL_OVF_UN) ves_abort(); BREAK;
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
			if (*ip == CEE_LEAVE_S) {
				++ip;
				ip += (signed char) *ip;
				++ip;
			} else {
				++ip;
				ip += (gint32) read32 (ip);
				ip += 4;
			}
			/*
			 * We may be either inside a try block or inside an handler.
			 * In the first case there was no exception and we go on
			 * executing the finally handlers and after that resume control
			 * at endfinally_ip.
			 * In the second case we need to clear the exception and
			 * continue directly at the target ip.
			 */
			if (frame->ex) {
				frame->ex = NULL;
				frame->ex_handler = NULL;
			} else {
				endfinally_ip = ip;
				goto handle_finally;
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
		CASE (CEE_UNUSED41) 
		CASE (CEE_UNUSED42) 
		CASE (CEE_UNUSED43) 
		CASE (CEE_UNUSED44) 
		CASE (CEE_UNUSED45) 
		CASE (CEE_UNUSED46) 
		CASE (CEE_UNUSED47) 
		CASE (CEE_UNUSED48) ves_abort(); BREAK;
		CASE (CEE_PREFIX7) ves_abort(); BREAK;
		CASE (CEE_PREFIX6) ves_abort(); BREAK;
		CASE (CEE_PREFIX5) ves_abort(); BREAK;
		CASE (CEE_PREFIX4) ves_abort(); BREAK;
		CASE (CEE_PREFIX3) ves_abort(); BREAK;
		CASE (CEE_PREFIX2) ves_abort(); BREAK;
		CASE (CEE_PREFIXREF) ves_abort(); BREAK;
		SUB_SWITCH
			++ip;
			switch (*ip) {
			case CEE_ARGLIST: ves_abort(); break;
			case CEE_CEQ: ves_abort(); break;
			case CEE_CGT: ves_abort(); break;
			case CEE_CGT_UN: ves_abort(); break;
			case CEE_CLT: ves_abort(); break;
			case CEE_CLT_UN: ves_abort(); break;
			case CEE_LDFTN: ves_abort(); break;
			case CEE_LDVIRTFTN: ves_abort(); break;
			case CEE_UNUSED56: ves_abort(); break;
			case CEE_LDARG: ves_abort(); break;
			case CEE_LDARGA: ves_abort(); break;
			case CEE_STARG: ves_abort(); break;
			case CEE_LDLOC: ves_abort(); break;
			case CEE_LDLOCA: ves_abort(); break;
			case CEE_STLOC: ves_abort(); break;
			case CEE_LOCALLOC:
				if (sp != frame->stack)
					THROW_EX (get_exception_execution_engine (), ip - 1);
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
			case CEE_INITOBJ: ves_abort(); break;
			case CEE_UNUSED68: ves_abort(); break;
			case CEE_CPBLK: ves_abort(); break;
			case CEE_INITBLK:
				sp -= 3;
				if (!sp [0].data.p)
					THROW_EX (get_exception_null_reference(), ip);
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
			case CEE_SIZEOF: ves_abort(); break;
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
#define OFFSET_IN_CLAUSE(clause,offset) \
	((clause)->try_offset <= (offset) && (offset) < ((clause)->try_offset + (clause)->try_len))

	handle_exception:
	{
		int i;
		guint32 ip_offset;
		MonoInvocation *inv;
		MonoMethodHeader *hd;
		MonoExceptionClause *clause;
		
		for (inv = frame; inv; inv = inv->parent) {
			hd = ((MonoMethodNormal*)inv->method)->header;
			ip_offset = inv->ip - hd->code;
			for (i = 0; i < hd->num_clauses; ++i) {
				clause = &hd->clauses [i];
				if (clause->flags <= 1 && OFFSET_IN_CLAUSE (clause, ip_offset)) {
					if (!clause->flags) {
							if (mono_object_isinst (frame->ex, mono_class_get (inv->method->klass->image, clause->token_or_filter))) {
								/* 
								 * OK, we found an handler, now we need to execute the finally
								 * and fault blocks before branching to the handler code.
								 */
								inv->ex_handler = clause;
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
		g_print ("Unhandled exception.\n");
		for (inv = frame, i = 0; inv; inv = inv->parent, ++i) {
			MonoClass *k = inv->method->klass;
			MonoMethodHeader *hd = ((MonoMethodNormal *)inv->method)->header;
			/*
			 * FIXME: print out also the arguments passed to the func.
			 */
			g_print ("#%d: 0x%05x in %s.%s::%s ()\n", i, inv->ip - hd->code, 
							k->name_space, k->name, inv->method->name);
		}
		exit (1);
	}
	handle_finally:
	{
		int i;
		guint32 ip_offset;
		MonoExceptionClause *clause;
		
		ip_offset = frame->ip - header->code;
		for (i = 0; i < header->num_clauses; ++i) {
			clause = &header->clauses [i];
			if (clause->flags == 2 && OFFSET_IN_CLAUSE (clause, ip_offset)) {
				ip = header->code + clause->handler_offset;
				goto main_loop;
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
			if (clause->flags == 3 && OFFSET_IN_CLAUSE (clause, ip_offset)) {
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

static int 
ves_exec (MonoAssembly *assembly)
{
	MonoImage *image = assembly->image;
	MonoCLIImageInfo *iinfo;
	stackval result;
	MonoInvocation call;
	MonoMethod *method;

	iinfo = image->image_info;
	method = mono_get_method (image, iinfo->cli_cli_header.ch_entry_point, NULL);
	INIT_FRAME (&call, NULL, NULL, NULL, &result, method);
	
	ves_exec_method (&call);
	mono_free_method (call.method);

	return result.data.i;
}

static void
usage (void)
{
	fprintf (stderr,
		 "mint %s, the Mono ECMA CLI interpreter, (C) 2001 Ximian, Inc.\n\n"
		 "Usage is: mint executable args...\n", "0.6");
	exit (1);
}

int 
main (int argc, char *argv [])
{
	MonoAssembly *assembly;
	int retval = 0;
	char *file;

	if (argc < 2)
		usage ();

	file = argv [1];

	mono_init ();
	mono_init_icall ();

	assembly = mono_assembly_open (file, NULL, NULL);
	if (!assembly){
		fprintf (stderr, "Can not open image %s\n", file);
		exit (1);
	}
	retval = ves_exec (assembly);
	mono_assembly_close (assembly);

	return retval;
}



