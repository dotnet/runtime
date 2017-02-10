/**
 * \file
 * Windows marshal support.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#if defined(HOST_WIN32)
#include <winsock2.h>
#include <windows.h>
#include <objbase.h>
#include "mono/metadata/marshal-windows-internals.h"

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
void*
mono_marshal_alloc_hglobal (size_t size)
{
	return GlobalAlloc (GMEM_FIXED, size);
}

gpointer
mono_marshal_realloc_hglobal (gpointer ptr, size_t size)
{
	return GlobalReAlloc (ptr, size, GMEM_MOVEABLE);
}

void
mono_marshal_free_hglobal (gpointer ptr)
{
	GlobalFree (ptr);
	return;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

void*
mono_marshal_alloc_co_task_mem (size_t size)
{
	return CoTaskMemAlloc (size);
}

void
mono_marshal_free_co_task_mem (void *ptr)
{
	CoTaskMemFree (ptr);
	return;
}

gpointer
mono_marshal_realloc_co_task_mem (gpointer ptr, size_t size)
{
	return CoTaskMemRealloc (ptr, size);
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi (MonoString *string)
{
	MonoError error;
	char* tres, *ret;
	size_t len;
	tres = mono_string_to_utf8_checked (string, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;
	if (!tres)
		return tres;

	/*
	 * mono_string_to_utf8_checked() returns a memory area at least as large as the size of the
	 * MonoString, even if it contains NULL characters. The copy we allocate here has to be equally
	 * large.
	 */
	len = MAX (strlen (tres) + 1, string->length);
	ret = ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal ((gpointer)len);
	memcpy (ret, tres, len);
	g_free (tres);
	return ret;
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni (MonoString *string)
{
	if (string == NULL)
		return NULL;
	else {
		size_t len = ((mono_string_length (string) + 1) * 2);
		gunichar2 *res = ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal ((gpointer)len);

		memcpy (res, mono_string_chars (string), mono_string_length (string) * 2);
		res [mono_string_length (string)] = 0;
		return res;
	}
}

gpointer
mono_string_to_utf8str (MonoString *s)
{
	char *as, *tmp;
	glong len;
	GError *error = NULL;

	if (s == NULL)
		return NULL;

	if (!s->length) {
		as = CoTaskMemAlloc (1);
		as [0] = '\0';
		return as;
	}

	tmp = g_utf16_to_utf8 (mono_string_chars (s), s->length, NULL, &len, &error);
	if (error) {
		MonoException *exc = mono_get_exception_argument ("string", error->message);
		g_error_free (error);
		mono_set_pending_exception (exc);
		return NULL;
	} else {
		as = CoTaskMemAlloc (len + 1);
		memcpy (as, tmp, len + 1);
		g_free (tmp);
		return as;
	}
}

#endif /* HOST_WIN32 */
