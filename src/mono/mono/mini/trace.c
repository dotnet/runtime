/*
 * trace.c: Tracing facilities for the Mono Runtime.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <signal.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <string.h>
#include "mini.h"
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include "trace.h"

static MonoTraceSpec trace_spec;

gboolean
mono_trace_eval (MonoMethod *method)
{
	int include = 0;
	int i;

	for (i = 0; i < trace_spec.len; i++){
		MonoTraceOperation *op = &trace_spec.ops [i];
		int inc = 0;
		
		switch (op->op){
		case MONO_TRACEOP_ALL:
			inc = 1; break;
		case MONO_TRACEOP_PROGRAM:
			if (trace_spec.assembly && (method->klass->image == mono_assembly_get_image (trace_spec.assembly)))
				inc = 1; break;
		case MONO_TRACEOP_METHOD:
			if (mono_method_desc_full_match ((MonoMethodDesc *) op->data, method))
				inc = 1; break;
		case MONO_TRACEOP_CLASS:
			if (strcmp (method->klass->name_space, op->data) == 0)
				if (strcmp (method->klass->name, op->data2) == 0)
					inc = 1;
			break;
		case MONO_TRACEOP_ASSEMBLY:
			if (strcmp (mono_image_get_name (method->klass->image), op->data) == 0)
				inc = 1; break;
		case MONO_TRACEOP_NAMESPACE:
			if (strcmp (method->klass->name_space, op->data) == 0)
				inc = 1;
		}
		if (op->exclude){
			if (inc)
				include = 0;
		} else if (inc)
			include = 1;
	}
	return include;
}

static int is_filenamechar (char p)
{
	if (p >= 'A' && p <= 'Z')
		return TRUE;
	if (p >= 'a' && p <= 'z')
		return TRUE;
	if (p >= '0' && p <= '9')
		return TRUE;
	if (p == '.' || p == ':' || p == '_' || p == '-')
		return TRUE;
	return FALSE;
}

static char *input;
static char *value;

static void get_string (void)
{
	char *start = input;
	while (is_filenamechar (*input)){
		input++;
	}
	if (value != NULL)
		g_free (value);
	value = g_malloc (input - start + 1);
	strncpy (value, start, input-start);
	value [input-start] = 0;
}

enum Token {
	TOKEN_METHOD,
	TOKEN_CLASS,
	TOKEN_ALL,
	TOKEN_PROGRAM,
	TOKEN_NAMESPACE,
	TOKEN_STRING,
	TOKEN_EXCLUDE,
	TOKEN_DISABLED,
	TOKEN_SEPARATOR,
	TOKEN_END,
	TOKEN_ERROR
};

static int
get_token (void)
{
	if (input [0] == '\0') {
		return TOKEN_END;
	}
	if (input [0] == 'M' && input [1] == ':'){
		input += 2;
		get_string ();
		return TOKEN_METHOD;
	}
	if (input [0] == 'N' && input [1] == ':'){
		input += 2;
		get_string ();
		return TOKEN_NAMESPACE;
	}
	if (input [0] == 'T' && input [1] == ':'){
		input += 2;
		get_string ();
		return TOKEN_CLASS;
	}
	if (is_filenamechar (*input)){
		get_string ();
		if (strcmp (value, "all") == 0)
			return TOKEN_ALL;
		if (strcmp (value, "program") == 0)
			return TOKEN_PROGRAM;
		if (strcmp (value, "disabled") == 0)
			return TOKEN_DISABLED;
		return TOKEN_STRING;
	}
	if (*input == '-'){
		input++;
		return TOKEN_EXCLUDE;
	}
	if (*input == ','){
		input++;
		return TOKEN_SEPARATOR;
	}

	fprintf (stderr, "Syntax error at or around '%s'\n", input);	
	return TOKEN_ERROR;
}

static void
cleanup (void)
{
	if (value != NULL)
		g_free (value);
}

static int
get_spec (int *last)
{
	int token = get_token ();
	if (token == TOKEN_EXCLUDE){
		token = get_spec (last);
		if (token == TOKEN_EXCLUDE){
			fprintf (stderr, "Expecting an expression");
			return TOKEN_ERROR;
		}
		if (token == TOKEN_ERROR)
			return token;
		trace_spec.ops [(*last)-1].exclude = 1;
		return TOKEN_SEPARATOR;
	}
	if (token == TOKEN_END || token == TOKEN_SEPARATOR || token == TOKEN_ERROR)
		return token;
	
	if (token == TOKEN_METHOD){
		MonoMethodDesc *desc = mono_method_desc_new (value, TRUE);
		if (desc == NULL){
			fprintf (stderr, "Invalid method name: %s\n", value);
			return TOKEN_ERROR;
		}
		trace_spec.ops [*last].op = MONO_TRACEOP_METHOD;
		trace_spec.ops [*last].data = desc;
	} else if (token == TOKEN_ALL)
		trace_spec.ops [*last].op = MONO_TRACEOP_ALL;
	else if (token == TOKEN_PROGRAM)
		trace_spec.ops [*last].op = MONO_TRACEOP_PROGRAM;
	else if (token == TOKEN_NAMESPACE){
		trace_spec.ops [*last].op = MONO_TRACEOP_NAMESPACE;
		trace_spec.ops [*last].data = g_strdup (value);
	} else if (token == TOKEN_CLASS){
		char *p = strrchr (value, '.');
		if (p) {
			*p++ = 0;
			trace_spec.ops [*last].data = g_strdup (value);
			trace_spec.ops [*last].data2 = g_strdup (p);
		}
		else {
			trace_spec.ops [*last].data = g_strdup ("");
			trace_spec.ops [*last].data2 = g_strdup (value);
		}
		trace_spec.ops [*last].op = MONO_TRACEOP_CLASS;
	} else if (token == TOKEN_STRING){
		trace_spec.ops [*last].op = MONO_TRACEOP_ASSEMBLY;
		trace_spec.ops [*last].data = g_strdup (value);
	} else if (token == TOKEN_DISABLED) {
		trace_spec.enabled = FALSE;
	} else {
		fprintf (stderr, "Syntax error in trace option specification\n");
		return TOKEN_ERROR;
	}
	(*last)++;
	return TOKEN_SEPARATOR;
}

MonoTraceSpec *
mono_trace_parse_options (const char *options)
{
	char *p = options;
	int size = 1;
	int last_used;
	int token;

	trace_spec.enabled = TRUE;
	if (*p == 0){
		trace_spec.len = 1;
		trace_spec.ops = g_new0 (MonoTraceOperation, 1);
		trace_spec.ops [0].op = MONO_TRACEOP_ALL;
		return &trace_spec;
	}
		
	for (p = options; *p != 0; p++)
		if (*p == ',')
			size++;
	
	trace_spec.ops = g_new0 (MonoTraceOperation, size);

	input = options;
	last_used = 0;
	
	while ((token = (get_spec (&last_used))) != TOKEN_END){
		if (token == TOKEN_ERROR)
			return NULL;
		if (token == TOKEN_SEPARATOR)
			continue;
	}
	trace_spec.len = last_used;
	cleanup ();
	return &trace_spec;
}

void
mono_trace_set_assembly (MonoAssembly *assembly)
{
	trace_spec.assembly = assembly;
}

static int indent_level = 0;

static void indent (int diff) {
	int v;
	if (diff < 0)
		indent_level += diff;
	v = indent_level;
	while (v-- > 0) {
		printf (". ");
	}
	if (diff > 0)
		indent_level += diff;
}

static char *
string_to_utf8 (MonoString *s)
{
	char *as;
	GError *error = NULL;

	g_assert (s);

	if (!s->length)
		return g_strdup ("");

	as = g_utf16_to_utf8 (mono_string_chars (s), s->length, NULL, NULL, &error);
	if (error) {
		/* Happens with StringBuilders */
		g_error_free (error);
		return g_strdup ("<INVALID UTF8>");
	}
	else
		return as;
}

void
mono_trace_enter_method (MonoMethod *method, char *ebp)
{
	int i, j;
	MonoClass *class;
	MonoObject *o;
	MonoJitArgumentInfo *arg_info;
	MonoMethodSignature *sig;
	char *fname;

	if (!trace_spec.enabled)
		return;

	fname = mono_method_full_name (method, TRUE);
	indent (1);
	printf ("ENTER: %s(", fname);
	g_free (fname);

	if (!ebp) {
		printf (") ip: %p\n", __builtin_return_address (1));
		return;
	}	

	sig = mono_method_signature (method);

	arg_info = alloca (sizeof (MonoJitArgumentInfo) * (sig->param_count + 1));

	mono_arch_get_argument_info (sig, sig->param_count, arg_info);

	if (MONO_TYPE_ISSTRUCT (mono_method_signature (method)->ret)) {
		g_assert (!mono_method_signature (method)->ret->byref);

		printf ("VALUERET:%p, ", *((gpointer *)(ebp + 8)));
	}

	if (mono_method_signature (method)->hasthis) {
		gpointer *this = (gpointer *)(ebp + arg_info [0].offset);
		if (method->klass->valuetype) {
			printf ("value:%p, ", *this);
		} else {
			o = *((MonoObject **)this);

			if (o) {
				class = o->vtable->klass;

				if (class == mono_defaults.string_class) {
					MonoString *s = (MonoString*)o;
					char *as = string_to_utf8 (s);

					printf ("this:[STRING:%p:%s], ", o, as);
					g_free (as);
				} else {
					printf ("this:%p[%s.%s %s], ", o, class->name_space, class->name, o->vtable->domain->friendly_name);
				}
			} else 
				printf ("this:NULL, ");
		}
	}

	for (i = 0; i < mono_method_signature (method)->param_count; ++i) {
		gpointer *cpos = (gpointer *)(ebp + arg_info [i + 1].offset);
		int size = arg_info [i + 1].size;

		MonoType *type = mono_method_signature (method)->params [i];
		
		if (type->byref) {
			printf ("[BYREF:%p], ", *cpos); 
		} else switch (mono_type_get_underlying_type (type)->type) {
			
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			printf ("%p, ", *((gpointer **)(cpos)));
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			printf ("%d, ", *((gint8 *)(cpos)));
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			printf ("%d, ", *((gint16 *)(cpos)));
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			printf ("%d, ", *((int *)(cpos)));
			break;
		case MONO_TYPE_STRING: {
			MonoString *s = *((MonoString **)cpos);
			if (s) {
				char *as;

				g_assert (((MonoObject *)s)->vtable->klass == mono_defaults.string_class);
				as = string_to_utf8 (s);

				printf ("[STRING:%p:%s], ", s, as);
				g_free (as);
			} else 
				printf ("[STRING:null], ");
			break;
		}
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT: {
			o = *((MonoObject **)cpos);
			if (o) {
				class = o->vtable->klass;
		    
				if (class == mono_defaults.string_class) {
					char *as = string_to_utf8 ((MonoString*)o);

					printf ("[STRING:%p:%s], ", o, as);
					g_free (as);
				} else if (class == mono_defaults.int32_class) {
					printf ("[INT32:%p:%d], ", o, *(gint32 *)((char *)o + sizeof (MonoObject)));
				} else if (class == mono_defaults.monotype_class) {
					printf ("[TYPE:%s", mono_type_full_name (((MonoReflectionType*)o)->type));
				} else
					printf ("[%s.%s:%p], ", class->name_space, class->name, o);
			} else {
				printf ("%p, ", *((gpointer *)(cpos)));				
			}
			break;
		}
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			printf ("%p, ", *((gpointer *)(cpos)));
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			printf ("0x%016llx, ", (long long)*((gint64 *)(cpos)));
			break;
		case MONO_TYPE_R4:
			printf ("%f, ", *((float *)(cpos)));
			break;
		case MONO_TYPE_R8:
			printf ("%f, ", *((double *)(cpos)));
			break;
		case MONO_TYPE_VALUETYPE: 
			printf ("[");
			for (j = 0; j < size; j++)
				printf ("%02x,", *((guint8*)cpos +j));
			printf ("], ");
			break;
		default:
			printf ("XX, ");
		}
	}

	printf (")\n");
	fflush (stdout);
}

void
mono_trace_leave_method (MonoMethod *method, ...)
{
	MonoType *type;
	char *fname;
	va_list ap;

	if (!trace_spec.enabled)
		return;

	va_start(ap, method);

	fname = mono_method_full_name (method, TRUE);
	indent (-1);
	printf ("LEAVE: %s", fname);
	g_free (fname);

	type = mono_method_signature (method)->ret;

handle_enum:
	switch (type->type) {
	case MONO_TYPE_VOID:
		break;
	case MONO_TYPE_BOOLEAN: {
		int eax = va_arg (ap, int);
		if (eax)
			printf ("TRUE:%d", eax);
		else 
			printf ("FALSE");
			
		break;
	}
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U: {
		int eax = va_arg (ap, int);
		printf ("result=%d", eax);
		break;
	}
	case MONO_TYPE_STRING: {
		MonoString *s = va_arg (ap, MonoString *);
;
		if (s) {
			char *as;

			g_assert (((MonoObject *)s)->vtable->klass == mono_defaults.string_class);
			as = string_to_utf8 (s);
			printf ("[STRING:%p:%s]", s, as);
			g_free (as);
		} else 
			printf ("[STRING:null], ");
		break;
	}
	case MONO_TYPE_CLASS: 
	case MONO_TYPE_OBJECT: {
		MonoObject *o = va_arg (ap, MonoObject *);

		if (o) {
			if (o->vtable->klass == mono_defaults.boolean_class) {
				printf ("[BOOLEAN:%p:%d]", o, *((guint8 *)o + sizeof (MonoObject)));		
			} else if  (o->vtable->klass == mono_defaults.int32_class) {
				printf ("[INT32:%p:%d]", o, *((gint32 *)((char *)o + sizeof (MonoObject))));	
			} else if  (o->vtable->klass == mono_defaults.int64_class) {
				printf ("[INT64:%p:%lld]", o, (long long)*((gint64 *)((char *)o + sizeof (MonoObject))));	
			} else
				printf ("[%s.%s:%p]", o->vtable->klass->name_space, o->vtable->klass->name, o);
		} else
			printf ("[OBJECT:%p]", o);
	       
		break;
	}
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY: {
		gpointer p = va_arg (ap, gpointer);
		printf ("result=%p", p);
		break;
	}
	case MONO_TYPE_I8: {
		gint64 l =  va_arg (ap, gint64);
		printf ("lresult=0x%16llx", (long long)l);
		break;
	}
	case MONO_TYPE_U8: {
		gint64 l =  va_arg (ap, gint64);
		printf ("lresult=0x%16llx", (long long)l);
		break;
	}
	case MONO_TYPE_R4:
	case MONO_TYPE_R8: {
		double f = va_arg (ap, double);
		printf ("FP=%f\n", f);
		break;
	}
	case MONO_TYPE_VALUETYPE: 
		if (type->data.klass->enumtype) {
			type = type->data.klass->enum_basetype;
			goto handle_enum;
		} else {
			guint8 *p = va_arg (ap, gpointer);
			int j, size, align;
			size = mono_type_size (type, &align);
			printf ("[");
			for (j = 0; p && j < size; j++)
				printf ("%02x,", p [j]);
			printf ("]");
		}
		break;
	default:
		printf ("(unknown return type %x)", mono_method_signature (method)->ret->type);
	}

	//printf (" ip: %p\n", __builtin_return_address (1));
	printf ("\n");
	fflush (stdout);
}

void
mono_trace_enable (gboolean enable)
{
	trace_spec.enabled = enable;
}

gboolean
mono_trace_is_enabled ()
{
	return trace_spec.enabled;
}

