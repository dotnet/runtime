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

MonoString *
mono_string_Internal_ctor_charp (gpointer dummy, gunichar2 *value);

MonoString *
mono_string_Internal_ctor_char_int (gpointer dummy, gunichar2 value, gint32 count);

MonoString *
mono_string_Internal_ctor_charp_int_int (gpointer dummy, gunichar2 *value, gint32 sindex, gint32 length);

MonoString *
mono_string_Internal_ctor_sbytep (gpointer dummy, gint8 *value);

MonoString *
mono_string_Internal_ctor_sbytep_int_int (gpointer dummy, gint8 *value, gint32 sindex, gint32 length);

MonoString *
mono_string_Internal_ctor_chara (gpointer dummy, MonoArray *value);

MonoString *
mono_string_Internal_ctor_chara_int_int (gpointer dummy, MonoArray *value,  gint32 sindex, gint32 length);

MonoString *
mono_string_Internal_ctor_encoding (gpointer dummy, gint8 *value, gint32 sindex, gint32 length, MonoObject *enc);

MonoString * 
mono_string_InternalJoin (MonoString *separator, MonoArray * value, gint32 sindex, gint32 count);

MonoString * 
mono_string_InternalInsert (MonoString *me, gint32 sindex, MonoString *value);

MonoString * 
mono_string_InternalReplaceChar (MonoString *me, gunichar2 oldChar, gunichar2 newChar);

MonoString * 
mono_string_InternalReplaceStr (MonoString *me, MonoString *oldValue, MonoString *newValue);

MonoString * 
mono_string_InternalRemove (MonoString *me, gint32 sindex, gint32 count);

void
mono_string_InternalCopyTo (MonoString *me, gint32 sindex, MonoArray *dest, gint32 dindex, gint32 count);

MonoArray * 
mono_string_InternalSplit (MonoString *me, MonoArray *separator, gint32 count);

MonoString * 
mono_string_InternalTrim (MonoString *me, MonoArray *chars, gint32 typ);

gint32 
mono_string_InternalIndexOfChar (MonoString *me, gunichar2 value, gint32 sindex, gint32 count);

gint32 
mono_string_InternalIndexOfStr (MonoString *me, MonoString *value, gint32 sindex, gint32 count);

gint32 
mono_string_InternalIndexOfAny (MonoString *me, MonoArray *arr, gint32 sindex, gint32 count);

gint32 
mono_string_InternalLastIndexOfChar (MonoString *me, gunichar2 value, gint32 sindex, gint32 count);

gint32 
mono_string_InternalLastIndexOfStr (MonoString *me, MonoString *value, gint32 sindex, gint32 count);

gint32 
mono_string_InternalLastIndexOfAny (MonoString *me, MonoArray *anyOf, gint32 sindex, gint32 count);

MonoString *
mono_string_InternalPad (MonoString *me, gint32 width, gunichar2 chr, MonoBoolean right);

MonoString *
mono_string_InternalToLower (MonoString *me);

MonoString *
mono_string_InternalToUpper (MonoString *me);

MonoString *
mono_string_InternalAllocateStr (gint32 length);

void 
mono_string_InternalStrcpyStr (MonoString *dest, gint32 destPos, MonoString *src);

void 
mono_string_InternalStrcpyStrN (MonoString *dest, gint32 destPos, MonoString *src, gint32 startPos, gint32 count);

MonoString  *
mono_string_InternalIntern (MonoString *str);

MonoString * 
mono_string_InternalIsInterned (MonoString *str);

gint32 
mono_string_InternalCompareStr (MonoString *s1, MonoString *s2, MonoBoolean inCase);

gint32 
mono_string_InternalCompareStrN (MonoString *s1, gint32 i1, MonoString *s2, gint32 i2, gint32 length, MonoBoolean inCase);

gint32
mono_string_GetHashCode (MonoString *me);

gunichar2 
mono_string_get_Chars (MonoString *me, gint32 idx);

gint32 
mono_string_cmp_char (gunichar2 c1, gunichar2 c2, gint16 mode);

gboolean 
mono_string_isinarray (MonoArray *chars, gint32 arraylength, gunichar2 chr);

#endif // _MONO_CLI_STRING_ICALLS_H_
