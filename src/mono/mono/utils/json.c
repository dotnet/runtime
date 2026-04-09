/**
 * \file
 * JSON writer
 *
 * Author:
 *   Joao Matos (joao.matos@xamarin.com)
 *
 * Copyright 2015 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <mono/utils/json.h>

void mono_json_writer_init (JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");

	writer->text = g_string_new ("");
	writer->indent = 0;
}

void mono_json_writer_destroy (JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	g_string_free (writer->text, /*free_segment=*/TRUE);
}

void mono_json_writer_indent_push(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	writer->indent += JSON_INDENT_VALUE;
}

void mono_json_writer_indent_pop(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	writer->indent -= JSON_INDENT_VALUE;
}

void mono_json_writer_indent(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");

	int i = 0;
	for (i = 0; i < writer->indent; ++i)
		g_string_append_c (writer->text, ' ');
}

void mono_json_writer_vprintf(JsonWriter* writer, const gchar *format, va_list args)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	g_string_append_vprintf (writer->text, format, args);
}

void mono_json_writer_printf(JsonWriter* writer, const gchar *format, ...)
{
	g_assert (writer && "Expected a valid JSON writer instance");

	va_list args;
	va_start (args, format);

	g_string_append_vprintf (writer->text, format, args);

	va_end (args);
}

void mono_json_writer_array_begin(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	g_string_append_printf (writer->text, "[\n");
	writer->indent += JSON_INDENT_VALUE;
}

void mono_json_writer_array_end(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	g_string_append_printf (writer->text, "]");
	writer->indent -= JSON_INDENT_VALUE;
}

void mono_json_writer_object_begin(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	mono_json_writer_printf (writer, "{\n");
	writer->indent += JSON_INDENT_VALUE;
}

void mono_json_writer_object_end(JsonWriter* writer)
{
	g_assert (writer && "Expected a valid JSON writer instance");
	mono_json_writer_printf (writer, "}");
}

void mono_json_writer_object_key(JsonWriter* writer, const gchar* format, ...)
{
	g_assert (writer && "Expected a valid JSON writer instance");

	va_list args;
	va_start (args, format);

	g_string_append_printf (writer->text, "\"");
	mono_json_writer_vprintf (writer, format, args);
	g_string_append_printf (writer->text, "\" : ");

	va_end (args);
}
