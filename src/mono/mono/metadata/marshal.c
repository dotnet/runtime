/*
 * marshal.c: Routines for marshaling complex types in P/Invoke methods.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#include "config.h"
#include "object.h"
#include "loader.h"
#include "marshal.h"

/* FIXME: on win32 we should probably use GlobalAlloc(). */
void*
mono_marshal_alloc (gpointer size) {
	return g_try_malloc ((gulong)size);
}

void
mono_marshal_free (gpointer ptr) {
	g_free (ptr);
}

void*
mono_marshal_realloc (gpointer ptr, gpointer size) {
	return g_try_realloc (ptr, (gulong)size);
}

void*
mono_marshal_string_array (MonoArray *array)
{
	char **result;
	int i, len;

	if (!array)
		return NULL;

	len = mono_array_length (array);

	result = g_malloc (sizeof (char*) * len);
	for (i = 0; i < len; ++i) {
		MonoString *s = (MonoString*)mono_array_get (array, gpointer, i);
		result [i] = s ? mono_string_to_utf8 (s): NULL;
	}
	return result;
}

