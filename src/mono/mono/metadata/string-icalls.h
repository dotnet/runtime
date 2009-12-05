#ifndef _MONO_CLI_STRING_ICALLS_H_
#define _MONO_CLI_STRING_ICALLS_H_

/*
 * string-icalls.h: String internal calls for the corlib
 *
 * Author:
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include "mono/utils/mono-compiler.h"

void
ves_icall_System_String_ctor_RedirectToCreateString (void) MONO_INTERNAL;

MonoArray * 
ves_icall_System_String_InternalSplit (MonoString *me, MonoArray *separator, gint32 count, gint32 options) MONO_INTERNAL;

MonoString *
ves_icall_System_String_InternalAllocateStr (gint32 length) MONO_INTERNAL;

MonoString  *
ves_icall_System_String_InternalIntern (MonoString *str) MONO_INTERNAL;

MonoString * 
ves_icall_System_String_InternalIsInterned (MonoString *str) MONO_INTERNAL;

int
ves_icall_System_String_GetLOSLimit (void) MONO_INTERNAL;

#endif /* _MONO_CLI_STRING_ICALLS_H_ */
