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

#ifndef __MONO_UTILS_JSON_H__
#define __MONO_UTILS_JSON_H__

 #include <glib.h>

#define JSON_INDENT_VALUE 2

typedef struct JsonWriter {
	GString* text;
	int indent;
} JsonWriter;

void mono_json_writer_init (JsonWriter* writer);
void mono_json_writer_destroy (JsonWriter* writer);
void mono_json_writer_indent(JsonWriter* writer);
void mono_json_writer_indent_push(JsonWriter* writer);
void mono_json_writer_indent_pop(JsonWriter* writer);
void mono_json_writer_vprintf(JsonWriter* writer, const gchar *format, va_list args);
void mono_json_writer_printf(JsonWriter* writer, const gchar *format, ...);
void mono_json_writer_array_begin(JsonWriter* writer);
void mono_json_writer_array_end(JsonWriter* writer);
void mono_json_writer_object_begin(JsonWriter* writer);
void mono_json_writer_object_end(JsonWriter* writer);
void mono_json_writer_object_key(JsonWriter* writer, const gchar* format, ...);

#endif
