/**
 * \file
 * String internal calls for the corlib
 *
 * Author:
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
 *   Duncan Mak  (duncan@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include "mono/utils/mono-membar.h"
#include <mono/metadata/string-icalls.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/gc-internals.h>
#include "icall-decl.h"

/* This function is redirected to String.CreateString ()
   by mono_marshal_get_native_wrapper () */
void
ves_icall_System_String_ctor_RedirectToCreateString (void)
{
	g_assert_not_reached ();
}

MonoStringHandle
ves_icall_System_String_FastAllocateString (gint32 length, MonoError *error)
{
	return mono_string_new_size_handle (length, error);
}

MonoStringHandle
ves_icall_System_String_InternalIntern (MonoStringHandle str, MonoError *error)
{
	return mono_string_intern_checked (str, error);
}

MonoStringHandle
ves_icall_System_String_InternalIsInterned (MonoStringHandle str, MonoError *error)
{
	return mono_string_is_interned_internal (str, error);
}

int
ves_icall_System_String_GetLOSLimit (void)
{
	int limit = mono_gc_get_los_limit ();

	return (limit - 2 - G_STRUCT_OFFSET (MonoString, chars)) / 2;
}
