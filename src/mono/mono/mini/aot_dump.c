/*
 * aot-dump.c: mono Ahead of Time compiler JSON dumping
 *
 * Author:
 *   Joao Matos (joao.matos@xamarin.com)
 *
 * Copyright 2015 Xamarin Inc (http://www.xamarin.com)
 */

#ifndef MONO_SMALL_CONFIG

#define JSON_INDENT_VALUE 2

typedef struct JsonWriter {
	GString* text;
	int indent;
} JsonWriter;

static void json_writer_init (JsonWriter* writer) MONO_INTERNAL;
static void json_writer_destroy (JsonWriter* writer);
static void json_writer_indent(JsonWriter* writer);
static void json_writer_indent_push(JsonWriter* writer);
static void json_writer_indent_pop(JsonWriter* writer);
static void json_writer_vprintf(JsonWriter* writer, const gchar *format, va_list args);
static void json_writer_printf(JsonWriter* writer, const gchar *format, ...);
static void json_writer_array_begin(JsonWriter* writer);
static void json_writer_array_end(JsonWriter* writer);
static void json_writer_object_begin(JsonWriter* writer);
static void json_writer_object_end(JsonWriter* writer);
static void json_writer_object_key(JsonWriter* writer, const gchar* format, ...);

static void json_writer_init (JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");

	writer->text = g_string_new ("");
	writer->indent = 0;
}

static void json_writer_destroy (JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	g_string_free (writer->text, /*free_segment=*/TRUE);
}

static void json_writer_indent_push(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	writer->indent += JSON_INDENT_VALUE;
}

static void json_writer_indent_pop(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	writer->indent -= JSON_INDENT_VALUE;
}

static void json_writer_indent(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");

	int i = 0;
	for (i = 0; i < writer->indent; ++i)
		g_string_append_c (writer->text, ' ');
}

static void json_writer_vprintf(JsonWriter* writer, const gchar *format, va_list args)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	g_string_append_vprintf (writer->text, format, args);
}

static void json_writer_printf(JsonWriter* writer, const gchar *format, ...)
{
	g_assert (writer && "Expected a valid JSON writer instance");

	va_list args;
	va_start (args, format);

	g_string_append_vprintf (writer->text, format, args);

	va_end (args);
}

static void json_writer_array_begin(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	g_string_append_printf (writer->text, "[\n");
	writer->indent += JSON_INDENT_VALUE;
}

static void json_writer_array_end(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	g_string_append_printf (writer->text, "]");
	writer->indent -= JSON_INDENT_VALUE;
}

static void json_writer_object_begin(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	json_writer_printf (writer, "{\n");
	writer->indent += JSON_INDENT_VALUE;
}

static void json_writer_object_end(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	json_writer_printf (writer, "}");
}

static void json_writer_object_key(JsonWriter* writer, const gchar* format, ...)
{
	g_assert (writer && "Expected a valid JSON writer instance");

	va_list args;
	va_start (args, format);

	g_string_append_printf (writer->text, "\"");
	json_writer_vprintf (writer, format, args);
	g_string_append_printf (writer->text, "\" : ");

	va_end (args);
}

#define WRAPPER(e,n) n,
static const char* const
wrapper_type_names [MONO_WRAPPER_NUM + 1] = {
#include "mono/metadata/wrapper-types.h"
	NULL
};

static G_GNUC_UNUSED const char*
get_wrapper_type_name (int type)
{
	return wrapper_type_names [type];
}

//#define DUMP_PLT
//#define DUMP_GOT

static void aot_dump (MonoAotCompile *acfg)
{
	FILE *dumpfile;
	char * dumpname;

	JsonWriter writer;
	json_writer_init (&writer);

	json_writer_object_begin(&writer);

	// Methods
	json_writer_indent (&writer);
	json_writer_object_key(&writer, "methods");
	json_writer_array_begin (&writer);

	int i;
	for (i = 0; i < acfg->nmethods; ++i) {
		MonoCompile *cfg;
		MonoMethod *method;
		MonoClass *klass;
		int index;

		cfg = acfg->cfgs [i];
		if (!cfg)
			continue;

		method = cfg->orig_method;

		json_writer_indent (&writer);
		json_writer_object_begin(&writer);

		json_writer_indent (&writer);
		json_writer_object_key(&writer, "name");
		json_writer_printf (&writer, "\"%s\",\n", method->name);

		json_writer_indent (&writer);
		json_writer_object_key(&writer, "signature");
		json_writer_printf (&writer, "\"%s\",\n", mono_method_full_name (method,
			/*signature=*/TRUE));

		json_writer_indent (&writer);
		json_writer_object_key(&writer, "code_size");
		json_writer_printf (&writer, "\"%d\",\n", cfg->code_size);

		klass = method->klass;

		json_writer_indent (&writer);
		json_writer_object_key(&writer, "class");
		json_writer_printf (&writer, "\"%s\",\n", klass->name);

		json_writer_indent (&writer);
		json_writer_object_key(&writer, "namespace");
		json_writer_printf (&writer, "\"%s\",\n", klass->name_space);

		json_writer_indent (&writer);
		json_writer_object_key(&writer, "wrapper_type");
		json_writer_printf (&writer, "\"%s\",\n", get_wrapper_type_name(method->wrapper_type));

		json_writer_indent_pop (&writer);
		json_writer_indent (&writer);
		json_writer_object_end (&writer);
		json_writer_printf (&writer, ",\n");
	}

	json_writer_indent_pop (&writer);
	json_writer_indent (&writer);
	json_writer_array_end (&writer);
	json_writer_printf (&writer, ",\n");

	// PLT entries
#ifdef DUMP_PLT
	json_writer_indent_push (&writer);
	json_writer_indent (&writer);
	json_writer_object_key(&writer, "plt");
	json_writer_array_begin (&writer);

	for (i = 0; i < acfg->plt_offset; ++i) {
		MonoPltEntry *plt_entry = NULL;
		MonoJumpInfo *ji;

		if (i == 0)
			/* 
			 * The first plt entry is unused.
			 */
			continue;

		plt_entry = g_hash_table_lookup (acfg->plt_offset_to_entry, GUINT_TO_POINTER (i));
		ji = plt_entry->ji;

		json_writer_indent (&writer);
		json_writer_printf (&writer, "{ ");
		json_writer_object_key(&writer, "symbol");
		json_writer_printf (&writer, "\"%s\" },\n", plt_entry->symbol);
	}

	json_writer_indent_pop (&writer);
	json_writer_indent (&writer);
	json_writer_array_end (&writer);
	json_writer_printf (&writer, ",\n");
#endif

	// GOT entries
#ifdef DUMP_GOT
	json_writer_indent_push (&writer);
	json_writer_indent (&writer);
	json_writer_object_key(&writer, "got");
	json_writer_array_begin (&writer);

	json_writer_indent_push (&writer);
	for (i = 0; i < acfg->got_info.got_patches->len; ++i) {
		MonoJumpInfo *ji = g_ptr_array_index (acfg->got_info.got_patches, i);

		json_writer_indent (&writer);
		json_writer_printf (&writer, "{ ");
		json_writer_object_key(&writer, "patch_name");
		json_writer_printf (&writer, "\"%s\" },\n", get_patch_name (ji->type));
	}

	json_writer_indent_pop (&writer);
	json_writer_indent (&writer);
	json_writer_array_end (&writer);
	json_writer_printf (&writer, ",\n");
#endif

	json_writer_indent_pop (&writer);
	json_writer_indent (&writer);
	json_writer_object_end (&writer);

	dumpname = g_strdup_printf ("%s.json", g_path_get_basename (acfg->image->name));
	dumpfile = fopen (dumpname, "w+");
	g_free (dumpname);

	fprintf (dumpfile, "%s", writer.text->str);
	fclose (dumpfile);

	json_writer_destroy (&writer);
}

#else

static void aot_dump (MonoAotCompile *acfg)
{

}

#endif