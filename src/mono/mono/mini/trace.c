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
#include <unistd.h>
#include <string.h>
#include "mini.h"
#include <mono/metadata/debug-helpers.h>
#include "trace.h"

static MonoTraceSpec trace_spec;

gboolean
mono_trace_eval (MonoMethod *method)
{
	MonoTraceSpec *s = mono_jit_trace_calls;
	int include = 0;
	int i;

	for (i = 0; i < trace_spec.len; i++){
		MonoTraceOperation *op = &trace_spec.ops [i];
		int inc = 0;
		
		switch (op->op){
		case MONO_TRACEOP_ALL:
			inc = 1; break;
		case MONO_TRACEOP_PROGRAM:
			if (method->klass->image == trace_spec.assembly->image)
				inc = 1; break;
		case MONO_TRACEOP_METHOD:
			if (mono_method_desc_match ((MonoMethodDesc *) op->data, method))
				inc = 1; break;
		case MONO_TRACEOP_CLASS:
			if (strcmp (method->klass->name_space, op->data) == 0)
				if (strcmp (method->klass->name, op->data2) == 0)
					inc = 1;
			break;
		case MONO_TRACEOP_ASSEMBLY:
			if (strcmp (method->klass->image->assembly_name, op->data) == 0)
				inc = 1; break;
		}
		if (op->exclude)
			inc = !inc;
		include |= inc;
	}
	return include;
}

static int is_filenamechar (char p)
{
	if (p >= 'A' && p <= 'Z')
		return TRUE;
	if (p >= 'a' && p <= 'z')
		return TRUE;
	if (p == '.' || p == ':')
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
	TOKEN_STRING,
	TOKEN_EXCLUDE,
	TOKEN_SEPARATOR,
	TOKEN_END,
	TOKEN_ERROR
};

static int
get_token (void)
{
	while (*input != 0){
		if (input [0] == 'M' && input [1] == ':'){
			input += 2;
			get_string ();
			return TOKEN_METHOD;
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
		input++;
			
	}
	return TOKEN_END;
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
	else if (token == TOKEN_CLASS){
		char *p = strrchr (value, '.');
		*p++ = 0;
		trace_spec.ops [*last].op = MONO_TRACEOP_CLASS;
		trace_spec.ops [*last].data = g_strdup (value);
		trace_spec.ops [*last].data2 = g_strdup (p);
	} else if (token == TOKEN_STRING){
		trace_spec.ops [*last].op = MONO_TRACEOP_ASSEMBLY;
		trace_spec.ops [*last].data = g_strdup (value);
	}
	else {
		fprintf (stderr, "Syntax error in trace option specification\n");
		return TOKEN_ERROR;
	}
	(*last)++;
	return TOKEN_SEPARATOR;
}

static const char *xmap (int idx)
{
	switch (idx){
	case MONO_TRACEOP_ALL: return "all";
	case MONO_TRACEOP_PROGRAM: return "program";
	case MONO_TRACEOP_METHOD: return "method"; 
	case MONO_TRACEOP_ASSEMBLY: return "assembly";
	case MONO_TRACEOP_CLASS: return "class";
	}
	return "UNKNOWN";
}

MonoTraceSpec *
mono_trace_parse_options (MonoAssembly *assembly, char *options)
{
	char *p = options;
	int size = 1;
	int last_used;
	int exclude = 0;
	int token;
	
	trace_spec.assembly = assembly;
	
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
	for (size = 0; size < last_used; size++){
		MonoTraceOperation *op = &trace_spec.ops [size];
		printf ("%s%s %s\n", op->exclude ? "-" : "", xmap (op->op), (char *) op->data);
	}
	cleanup ();
	return &trace_spec;
}
