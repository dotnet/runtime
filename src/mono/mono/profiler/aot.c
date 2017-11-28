/*
 * aot.c: Ahead of Time Compiler Profiler for Mono.
 *
 *
 * Copyright 2008-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#include "aot.h"

#include <mono/metadata/profiler.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class-internals.h>
#include <mono/mini/jit.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-os-mutex.h>
#include <string.h>
#include <errno.h>
#include <stdlib.h>
#include <glib.h>

struct _MonoProfiler {
	GHashTable *classes;
	GHashTable *images;
	GPtrArray *methods;
	FILE *outfile;
	int id;
	char *outfile_name;
	mono_mutex_t mutex;
	gboolean verbose;
};

static MonoProfiler aot_profiler;

static void
prof_jit_done (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo)
{
	MonoImage *image = mono_class_get_image (mono_method_get_class (method));

	if (!image->assembly || method->wrapper_type)
		return;

	mono_os_mutex_lock (&prof->mutex);
	g_ptr_array_add (prof->methods, method);
	mono_os_mutex_unlock (&prof->mutex);
}

static void
prof_shutdown (MonoProfiler *prof);

static void
usage (void)
{
	mono_profiler_printf ("AOT profiler.\n");
	mono_profiler_printf ("Usage: mono --profile=aot[:OPTION1[,OPTION2...]] program.exe\n");
	mono_profiler_printf ("Options:\n");
	mono_profiler_printf ("\thelp                 show this usage info\n");
	mono_profiler_printf ("\toutput=FILENAME      write the data to file FILENAME\n");
	mono_profiler_printf ("\tverbose              print diagnostic info\n");

	exit (0);
}

static gboolean
match_option (const char *arg, const char *opt_name, const char **rval)
{
	if (rval) {
		const char *end = strchr (arg, '=');

		*rval = NULL;
		if (!end)
			return !strcmp (arg, opt_name);

		if (strncmp (arg, opt_name, strlen (opt_name)) || (end - arg) > strlen (opt_name) + 1)
			return FALSE;
		*rval = end + 1;
		return TRUE;
	} else {
		//FIXME how should we handle passing a value to an arg that doesn't expect it?
		return !strcmp (arg, opt_name);
	}
}

static void
parse_arg (const char *arg)
{
	const char *val;

	if (match_option (arg, "help", NULL)) {
		usage ();
	} else if (match_option (arg, "output", &val)) {
		aot_profiler.outfile_name = g_strdup (val);
	} else if (match_option (arg, "verbose", NULL)) {
		aot_profiler.verbose = TRUE;
	} else {
		mono_profiler_printf_err ("Could not parse argument: %s", arg);
	}
}

static void
parse_args (const char *desc)
{
	const char *p;
	gboolean in_quotes = FALSE;
	char quote_char = '\0';
	char *buffer = malloc (strlen (desc));
	int buffer_pos = 0;

	for (p = desc; *p; p++){
		switch (*p){
		case ',':
			if (!in_quotes) {
				if (buffer_pos != 0){
					buffer [buffer_pos] = 0;
					parse_arg (buffer);
					buffer_pos = 0;
				}
			} else {
				buffer [buffer_pos++] = *p;
			}
			break;

		case '\\':
			if (p [1]) {
				buffer [buffer_pos++] = p[1];
				p++;
			}
			break;
		case '\'':
		case '"':
			if (in_quotes) {
				if (quote_char == *p)
					in_quotes = FALSE;
				else
					buffer [buffer_pos++] = *p;
			} else {
				in_quotes = TRUE;
				quote_char = *p;
			}
			break;
		default:
			buffer [buffer_pos++] = *p;
			break;
		}
	}

	if (buffer_pos != 0) {
		buffer [buffer_pos] = 0;
		parse_arg (buffer);
	}

	g_free (buffer);
}

void
mono_profiler_init_aot (const char *desc);

/**
 * mono_profiler_init_aot:
 * the entry point
 */
void
mono_profiler_init_aot (const char *desc)
{
	if (mono_jit_aot_compiling ()) {
		mono_profiler_printf_err ("The AOT profiler is not meant to be run during AOT compilation.");
		exit (1);
	}

	parse_args (desc [strlen ("aot")] == ':' ? desc + strlen ("aot") + 1 : "");

	if (!aot_profiler.outfile_name)
		aot_profiler.outfile_name = g_strdup ("output.aotprofile");
	else if (*aot_profiler.outfile_name == '+')
		aot_profiler.outfile_name = g_strdup_printf ("%s.%d", aot_profiler.outfile_name + 1, getpid ());

	if (*aot_profiler.outfile_name == '|')
		aot_profiler.outfile = popen (aot_profiler.outfile_name + 1, "w");
	else if (*aot_profiler.outfile_name == '#')
		aot_profiler.outfile = fdopen (strtol (aot_profiler.outfile_name + 1, NULL, 10), "a");
	else
		aot_profiler.outfile = fopen (aot_profiler.outfile_name, "w");

	if (!aot_profiler.outfile) {
		mono_profiler_printf_err ("Could not create AOT profiler output file '%s': %s", aot_profiler.outfile_name, g_strerror (errno));
		exit (1);
	}

	aot_profiler.images = g_hash_table_new (NULL, NULL);
	aot_profiler.classes = g_hash_table_new (NULL, NULL);
	aot_profiler.methods = g_ptr_array_new ();

	mono_os_mutex_init (&aot_profiler.mutex);

	MonoProfilerHandle handle = mono_profiler_create (&aot_profiler);
	mono_profiler_set_runtime_shutdown_end_callback (handle, prof_shutdown);
	mono_profiler_set_jit_done_callback (handle, prof_jit_done);
}

static void
emit_byte (MonoProfiler *prof, guint8 value)
{
	fwrite (&value, sizeof (guint8), 1, prof->outfile);
}

static void
emit_int32 (MonoProfiler *prof, gint32 value)
{
	for (int i = 0; i < sizeof (gint32); ++i) {
		guint8 b = value;
		fwrite (&b, sizeof (guint8), 1, prof->outfile);
		value >>= 8;
	}
}

static void
emit_string (MonoProfiler *prof, const char *str)
{
	int len = strlen (str);

	emit_int32 (prof, len);
	fwrite (str, len, 1, prof->outfile);
}

static void
emit_record (MonoProfiler *prof, AotProfRecordType type, int id)
{
	emit_byte (prof, type);
	emit_int32 (prof, id);
}

static int
add_image (MonoProfiler *prof, MonoImage *image)
{
	int id = GPOINTER_TO_INT (g_hash_table_lookup (prof->images, image));
	if (id)
		return id - 1;

	id = prof->id ++;
	emit_record (prof, AOTPROF_RECORD_IMAGE, id);
	emit_string (prof, image->assembly->aname.name);
	emit_string (prof, image->guid);
	g_hash_table_insert (prof->images, image, GINT_TO_POINTER (id + 1));
	return id;
}

static int
add_class (MonoProfiler *prof, MonoClass *klass);

static int
add_type (MonoProfiler *prof, MonoType *type)
{
	switch (type->type) {
#if 0
	case MONO_TYPE_SZARRAY: {
		int eid = add_type (prof, &type->data.klass->byval_arg);
		if (eid == -1)
			return -1;
		int id = prof->id ++;
		emit_record (prof, AOTPROF_RECORD_TYPE, id);
		emit_byte (prof, MONO_TYPE_SZARRAY);
		emit_int32 (prof, id);
		return id;
	}
#endif
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_GENERICINST:
		return add_class (prof, mono_class_from_mono_type (type));
	default:
		return -1;
	}
}

static int
add_ginst (MonoProfiler *prof, MonoGenericInst *inst)
{
	int i, id;
	int *ids;

	// FIXME: Cache
	ids = g_malloc0 (inst->type_argc * sizeof (int));
	for (i = 0; i < inst->type_argc; ++i) {
		MonoType *t = inst->type_argv [i];
		ids [i] = add_type (prof, t);
		if (ids [i] == -1) {
			g_free (ids);
			return -1;
		}
	}
	id = prof->id ++;
	emit_record (prof, AOTPROF_RECORD_GINST, id);
	emit_int32 (prof, inst->type_argc);
	for (i = 0; i < inst->type_argc; ++i)
		emit_int32 (prof, ids [i]);
	g_free (ids);

	return id;
}

static int
add_class (MonoProfiler *prof, MonoClass *klass)
{
	int id, inst_id = -1, image_id;
	char *name;

	id = GPOINTER_TO_INT (g_hash_table_lookup (prof->classes, klass));
	if (id)
		return id - 1;

	image_id = add_image (prof, klass->image);

	if (mono_class_is_ginst (klass)) {
		MonoGenericContext *ctx = mono_class_get_context (klass);
		inst_id = add_ginst (prof, ctx->class_inst);
		if (inst_id == -1)
			return -1;
	}

	if (klass->nested_in)
		name = g_strdup_printf ("%s.%s/%s", klass->nested_in->name_space, klass->nested_in->name, klass->name);
	else
		name = g_strdup_printf ("%s.%s", klass->name_space, klass->name);

	id = prof->id ++;
	emit_record (prof, AOTPROF_RECORD_TYPE, id);
	emit_byte (prof, MONO_TYPE_CLASS);
	emit_int32 (prof, image_id);
	emit_int32 (prof, inst_id);
	emit_string (prof, name);
	g_free (name);
	g_hash_table_insert (prof->classes, klass, GINT_TO_POINTER (id + 1));
	return id;
}

static void
add_method (MonoProfiler *prof, MonoMethod *m)
{
	MonoError error;
	MonoMethodSignature *sig;
	char *s;

	sig = mono_method_signature_checked (m, &error);
	g_assert (mono_error_ok (&error));

	int class_id = add_class (prof, m->klass);
	if (class_id == -1)
		return;
	int inst_id = -1;

	if (m->is_inflated) {
		MonoGenericContext *ctx = mono_method_get_context (m);
		if (ctx->method_inst)
			inst_id = add_ginst (prof, ctx->method_inst);
	}
	int id = prof->id ++;
	emit_record (prof, AOTPROF_RECORD_METHOD, id);
	emit_int32 (prof, class_id);
	emit_int32 (prof, inst_id);
	emit_int32 (prof, sig->param_count);
	emit_string (prof, m->name);
	s = mono_signature_full_name (sig);
	emit_string (prof, s);
	g_free (s);

	if (prof->verbose)
		mono_profiler_printf ("%s %d\n", mono_method_full_name (m, 1), id);
}

/* called at the end of the program */
static void
prof_shutdown (MonoProfiler *prof)
{
	int mindex;
	char magic [32];

	gint32 version = (AOT_PROFILER_MAJOR_VERSION << 16) | AOT_PROFILER_MINOR_VERSION;
	sprintf (magic, AOT_PROFILER_MAGIC);
	fwrite (magic, strlen (magic), 1, prof->outfile);
	emit_int32 (prof, version);

	GHashTable *all_methods = g_hash_table_new (NULL, NULL);
	for (mindex = 0; mindex < prof->methods->len; ++mindex) {
	    MonoMethod *m = (MonoMethod*)g_ptr_array_index (prof->methods, mindex);

		if (!mono_method_get_token (m))
			continue;

		if (g_hash_table_lookup (all_methods, m))
			continue;
		g_hash_table_insert (all_methods, m, m);

		add_method (prof, m);
	}
	emit_record (prof, AOTPROF_RECORD_NONE, 0);

	fclose (prof->outfile);

	g_hash_table_destroy (all_methods);
	g_hash_table_destroy (prof->classes);
	g_hash_table_destroy (prof->images);
	g_ptr_array_free (prof->methods, TRUE);
	g_free (prof->outfile_name);
}
