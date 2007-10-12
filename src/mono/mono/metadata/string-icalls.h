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

MonoString * 
ves_icall_System_String_InternalJoin (MonoString *separator, MonoArray * value, gint32 sindex, gint32 count) MONO_INTERNAL;

void
ves_icall_System_String_InternalCopyTo (MonoString *me, gint32 sindex, MonoArray *dest, gint32 dindex, gint32 count) MONO_INTERNAL;

MonoArray * 
ves_icall_System_String_InternalSplit (MonoString *me, MonoArray *separator, gint32 count, gint32 options) MONO_INTERNAL;

MonoString * 
ves_icall_System_String_InternalTrim (MonoString *me, MonoArray *chars, gint32 typ) MONO_INTERNAL;

gint32 
ves_icall_System_String_InternalLastIndexOfAny (MonoString *me, MonoArray *anyOf, gint32 sindex, gint32 count) MONO_INTERNAL;

MonoString *
ves_icall_System_String_InternalPad (MonoString *me, gint32 width, gunichar2 chr, MonoBoolean right) MONO_INTERNAL;

MonoString *
ves_icall_System_String_InternalAllocateStr (gint32 length) MONO_INTERNAL;

void 
ves_icall_System_String_InternalStrcpy_Str (MonoString *dest, gint32 destPos, MonoString *src) MONO_INTERNAL;

void 
ves_icall_System_String_InternalStrcpy_StrN (MonoString *dest, gint32 destPos, MonoString *src, gint32 startPos, gint32 count) MONO_INTERNAL;

void 
ves_icall_System_String_InternalStrcpy_Chars (MonoString *dest, gint32 destPos, MonoArray *src) MONO_INTERNAL;

void 
ves_icall_System_String_InternalStrcpy_CharsN (MonoString *dest, gint32 destPos, MonoArray *src, gint32 startPos, gint32 count) MONO_INTERNAL;

MonoString  *
ves_icall_System_String_InternalIntern (MonoString *str) MONO_INTERNAL;

MonoString * 
ves_icall_System_String_InternalIsInterned (MonoString *str) MONO_INTERNAL;

gunichar2 
ves_icall_System_String_get_Chars (MonoString *me, gint32 idx) MONO_INTERNAL;

#endif /* _MONO_CLI_STRING_ICALLS_H_ */
