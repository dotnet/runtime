/*
 * string-icalls.c: String internal calls for the corlib
 *
 * Author:
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
 *   Duncan Mak  (duncan@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <signal.h>
#include <string.h>
#include <mono/metadata/string-icalls.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/unicode.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/debug-helpers.h>

/* Internal helper methods */

static gboolean
string_icall_is_in_array (MonoArray *chars, gint32 arraylength, gunichar2 chr);

static gint32
string_icall_cmp_char (gunichar2 c1, gunichar2 c2, gint32 mode);

MonoString *
ves_icall_System_String_ctor_charp (gpointer dummy, gunichar2 *value)
{
	gint32 i, length;
	MonoDomain *domain;

	MONO_ARCH_SAVE_REGS;

	domain = mono_domain_get ();

	if (value == NULL)
		length = 0;
	else {
		for (i = 0; *(value + i) != '\0'; i++);
		length = i;
	}

	return mono_string_new_utf16 (domain, value, length);
}

MonoString *
ves_icall_System_String_ctor_char_int (gpointer dummy, gunichar2 value, gint32 count)
{
	MonoDomain *domain;
	MonoString *res;
	gunichar2 *chars;
	gint32 i;

	MONO_ARCH_SAVE_REGS;

	if (count < 0)
		mono_raise_exception (mono_get_exception_argument_out_of_range ("count"));

	domain = mono_domain_get ();
	res = mono_string_new_size (domain, count);

	chars = mono_string_chars (res);
	for (i = 0; i < count; i++)
		chars [i] = value;
	
	return res;
}

MonoString *
ves_icall_System_String_ctor_charp_int_int (gpointer dummy, gunichar2 *value, gint32 sindex, gint32 length)
{
	gunichar2 *begin;
	MonoDomain * domain;
	
	MONO_ARCH_SAVE_REGS;

	domain = mono_domain_get ();

	if ((value == NULL) && (length != 0))
		mono_raise_exception (mono_get_exception_argument_out_of_range ("Out of range"));

	if ((sindex < 0) || (length < 0))
		mono_raise_exception (mono_get_exception_argument_out_of_range ("Out of range"));
	
	if (length == 0) {	/* fixme: return String.Empty here */
		g_warning ("string doesn't yet support empy strings in char* constructor");
		g_assert_not_reached ();
	}
	
	begin = (gunichar2 *) (value + sindex);

	return mono_string_new_utf16 (domain, begin, length);
}

MonoString *
ves_icall_System_String_ctor_sbytep (gpointer dummy, gint8 *value)
{
	MonoDomain *domain;
	
	MONO_ARCH_SAVE_REGS;

	domain = mono_domain_get ();

	if (NULL == value) {	/* fixme: return String.Empty here */
		g_warning ("string doesn't yet support empy strings in char* constructor");
		g_assert_not_reached ();
	}

	return mono_string_new (domain, (const char *) value);
}

MonoString *
ves_icall_System_String_ctor_sbytep_int_int (gpointer dummy, gint8 *value, gint32 sindex, gint32 length)
{
	guchar *begin;
	MonoDomain *domain;
	MonoString *res;
	gunichar2 *chars;
	int i;
	
	MONO_ARCH_SAVE_REGS;

	domain = mono_domain_get ();

	if ((value == NULL) && (length != 0))
		mono_raise_exception (mono_get_exception_argument_out_of_range ("Out of range"));

	if ((sindex < 0) || (length < 0))
		mono_raise_exception (mono_get_exception_argument_out_of_range ("Out of range"));

	begin = (guchar *) (value + sindex);
	res = mono_string_new_size (domain, length);
	chars = mono_string_chars (res);
	for (i = 0; i < length; ++i)
		chars [i] = begin [i];

	return res;
}

MonoString *
ves_icall_System_String_ctor_chara (gpointer dummy, MonoArray *value)
{
	MonoDomain *domain;

	MONO_ARCH_SAVE_REGS;

	domain = mono_domain_get ();

	if (value == NULL)
		return mono_string_new_utf16 (domain, NULL, 0);
	else
		return mono_string_new_utf16 (domain, (gunichar2 *) mono_array_addr(value, gunichar2, 0),  value->max_length);
}

MonoString *
ves_icall_System_String_ctor_chara_int_int (gpointer dummy, MonoArray *value, 
					 gint32 sindex, gint32 length)
{
	MonoDomain *domain;

	MONO_ARCH_SAVE_REGS;

	if (value == NULL)
		mono_raise_exception (mono_get_exception_argument_null ("value"));
	if (sindex < 0)
		mono_raise_exception (mono_get_exception_argument_out_of_range ("startIndex"));		
	if (length < 0)
		mono_raise_exception (mono_get_exception_argument_out_of_range ("length"));
	if (sindex + length > mono_array_length (value))
		mono_raise_exception (mono_get_exception_argument_out_of_range ("Out of range"));

	domain = mono_domain_get ();
	
	return mono_string_new_utf16 (domain, (gunichar2 *) mono_array_addr(value, gunichar2, sindex), length);
}

MonoString *
ves_icall_System_String_ctor_encoding (gpointer dummy, gint8 *value, gint32 sindex, 
				    gint32 length, MonoObject *enc)
{
	MONO_ARCH_SAVE_REGS;
	MonoArray *arr;
	MonoString *s;
	MonoObject *exc;
	MonoDomain *domain = mono_domain_get ();
	MonoMethod *get_string;
	gpointer args [1];

	if ((value == NULL) || (length == 0))
		return mono_string_new_size (mono_domain_get (), 0);
	if (enc == NULL)
		mono_raise_exception (mono_get_exception_argument_null ("enc"));
	if (sindex < 0)
		mono_raise_exception (mono_get_exception_argument_out_of_range ("startIndex"));		
	if (length < 0)
		mono_raise_exception (mono_get_exception_argument_out_of_range ("length"));

	arr = mono_array_new (domain, mono_defaults.byte_class, length);
	memcpy (mono_array_addr (arr, guint8*, 0), value + sindex, length);

	get_string = mono_find_method_by_name (enc->vtable->klass, "GetString", 1);
	args [0] = arr;
	s = (MonoString*)mono_runtime_invoke (get_string, enc, args, &exc);
	if (!s || exc)
		mono_raise_exception (mono_get_exception_argument ("", "Unable to decode the array into a valid string."));

	return s;
}

MonoString * 
ves_icall_System_String_InternalJoin (MonoString *separator, MonoArray * value, gint32 sindex, gint32 count)
{
	MonoString * ret;
	MonoString *current;
	gint32 length;
	gint32 pos;
	gint32 insertlen;
	gint32 destpos;
	gint32 srclen;
	gunichar2 *insert;
	gunichar2 *dest;
	gunichar2 *src;

	MONO_ARCH_SAVE_REGS;

	insert = mono_string_chars(separator);
	insertlen = mono_string_length(separator);

	length = 0;
	for (pos = sindex; pos != sindex + count; pos++) {
		current = mono_array_get (value, MonoString *, pos);
		if (current != NULL)
			length += mono_string_length (current);

		if (pos < sindex + count - 1)
			length += insertlen;
	}

	ret = mono_string_new_size( mono_domain_get (), length);
	dest = mono_string_chars(ret);
	destpos = 0;

	for (pos = sindex; pos != sindex + count; pos++) {
		current = mono_array_get (value, MonoString *, pos);
		if (current != NULL) {
			src = mono_string_chars (current);
			srclen = mono_string_length (current);

			memcpy (dest + destpos, src, srclen * sizeof(gunichar2));
			destpos += srclen;
		}

		if (pos < sindex + count - 1) {
			memcpy(dest + destpos, insert, insertlen * sizeof(gunichar2));
			destpos += insertlen;
		}
	}

	return ret;
}

MonoString * 
ves_icall_System_String_InternalInsert (MonoString *me, gint32 sindex, MonoString *value)
{
	MonoString * ret;
	gunichar2 *src;
	gunichar2 *insertsrc;
	gunichar2 *dest;
	gint32 srclen;
	gint32 insertlen;

	MONO_ARCH_SAVE_REGS;

	src = mono_string_chars(me);
	srclen = mono_string_length(me);

	insertsrc = mono_string_chars(value);
	insertlen = mono_string_length(value);

	ret = mono_string_new_size( mono_domain_get (), srclen + insertlen);
	dest = mono_string_chars(ret);

	memcpy(dest, src, sindex * sizeof(gunichar2));
	memcpy(dest + sindex, insertsrc, insertlen * sizeof(gunichar2));
	memcpy(dest + sindex + insertlen, src + sindex, (srclen - sindex) * sizeof(gunichar2));

	return ret;
}

MonoString * 
ves_icall_System_String_InternalReplace_Char (MonoString *me, gunichar2 oldChar, gunichar2 newChar)
{
	MonoString *ret;
	gunichar2 *src;
	gunichar2 *dest;
	gint32 i, srclen;

	MONO_ARCH_SAVE_REGS;

	src = mono_string_chars(me);
	srclen = mono_string_length(me);

	ret = mono_string_new_size( mono_domain_get (), srclen);
	dest = mono_string_chars(ret);

	for (i = 0; i != srclen; i++) {
		if (src[i] == oldChar)
			dest[i] = newChar;
		else
			dest[i] = src[i];
	}

	return ret;
}

MonoString * 
ves_icall_System_String_InternalRemove (MonoString *me, gint32 sindex, gint32 count)
{
	MonoString * ret;
	gint32 srclen;
	gunichar2 *dest;
	gunichar2 *src;

	MONO_ARCH_SAVE_REGS;

	srclen = mono_string_length(me);
	ret = mono_string_new_size( mono_domain_get (), srclen - count);

	src = mono_string_chars(me);
	dest = mono_string_chars(ret);

	memcpy(dest, src, sindex * sizeof(gunichar2));
	memcpy(dest + sindex, src + sindex + count, (srclen - count - sindex) * sizeof(gunichar2));

	return ret;
}

void
ves_icall_System_String_InternalCopyTo (MonoString *me, gint32 sindex, MonoArray *dest, gint32 dindex, gint32 count)
{
	gunichar2 *destptr = (gunichar2 *) mono_array_addr(dest, gunichar2, dindex);
	gunichar2 *src =  mono_string_chars(me);

	MONO_ARCH_SAVE_REGS;

	memcpy(destptr, src + sindex, sizeof(gunichar2) * count);
}

MonoArray * 
ves_icall_System_String_InternalSplit (MonoString *me, MonoArray *separator, gint32 count)
{
	MonoString * tmpstr;
	MonoArray * retarr;
	gunichar2 *src;
	gint32 arrsize, srcsize, splitsize;
	gint32 i, lastpos, arrpos;
	gint32 tmpstrsize;
	gunichar2 *tmpstrptr;

	gunichar2 cmpchar;

	MONO_ARCH_SAVE_REGS;

	src = mono_string_chars(me);
	srcsize = mono_string_length(me);
	arrsize = mono_array_length(separator);

	cmpchar = mono_array_get(separator, gunichar2, 0);

	splitsize = 0;
	for (i = 0; i != srcsize && splitsize < count; i++) {
		if (string_icall_is_in_array(separator, arrsize, src[i]))
			splitsize++;
	}

	lastpos = 0;
	arrpos = 0;

	/* if no split chars found return the string */
	if (splitsize == 0) {
		retarr = mono_array_new(mono_domain_get(), mono_get_string_class (), 1);
		mono_array_set(retarr, MonoString *, 0, me);

		return retarr;
	}

	if (splitsize != count)
		splitsize++;

	retarr = mono_array_new(mono_domain_get(), mono_get_string_class (), splitsize);
	for (i = 0; i != srcsize && arrpos != count; i++) {
		if (string_icall_is_in_array(separator, arrsize, src[i])) {
			if (arrpos == count - 1)
				tmpstrsize = srcsize - lastpos;
			else
				tmpstrsize = i - lastpos;

			tmpstr = mono_string_new_size( mono_domain_get (), tmpstrsize);
			tmpstrptr = mono_string_chars(tmpstr);

			memcpy(tmpstrptr, src + lastpos, tmpstrsize * sizeof(gunichar2));
			mono_array_set(retarr, MonoString *, arrpos, tmpstr);
			arrpos++;
			lastpos = i + 1;
		}
	}

	if (arrpos < count) {
		tmpstrsize = srcsize - lastpos;
		tmpstr = mono_string_new_size( mono_domain_get (), tmpstrsize);
		tmpstrptr = mono_string_chars(tmpstr);

		memcpy(tmpstrptr, src + lastpos, tmpstrsize * sizeof(gunichar2));
		mono_array_set(retarr, MonoString *, arrpos, tmpstr);
	}

	return retarr;
}

static gboolean
string_icall_is_in_array (MonoArray *chars, gint32 arraylength, gunichar2 chr)
{
	gunichar2 cmpchar;
	gint32 arrpos;

	for (arrpos = 0; arrpos != arraylength; arrpos++) {
		cmpchar = mono_array_get(chars, gunichar2, arrpos);
		if (cmpchar == chr)
			return TRUE;
	}
	
	return FALSE;
}

MonoString * 
ves_icall_System_String_InternalTrim (MonoString *me, MonoArray *chars, gint32 typ)
{
	MonoString * ret;
	gunichar2 *src, *dest;
	gint32 srclen, newlen, arrlen;
	gint32 i, lenfirst, lenlast;

	MONO_ARCH_SAVE_REGS;

	srclen = mono_string_length(me);
	src = mono_string_chars(me);
	arrlen = mono_array_length(chars);

	lenfirst = 0;
	lenlast = 0;

	if (0 == typ || 1 == typ) {
		for (i = 0; i != srclen; i++) {
			if (string_icall_is_in_array(chars, arrlen, src[i]))
				lenfirst++;
			else 
				break;
		}
	}

	if (0 == typ || 2 == typ) {
		for (i = srclen - 1; i > lenfirst - 1; i--) {
			if (string_icall_is_in_array(chars, arrlen, src[i]))
				lenlast++;
			else 
				break;
		}
	}

	newlen = srclen - lenfirst - lenlast;
	if (newlen == srclen)
		return me;

	ret = mono_string_new_size( mono_domain_get (), newlen);
	dest = mono_string_chars(ret);

	memcpy(dest, src + lenfirst, newlen *sizeof(gunichar2));

	return ret;
}

gint32 
ves_icall_System_String_InternalIndexOfAny (MonoString *me, MonoArray *arr, gint32 sindex, gint32 count)
{
	gint32 pos;
	gint32 loop;
	gint32 arraysize;
	gunichar2 *src;

	MONO_ARCH_SAVE_REGS;

	arraysize = mono_array_length(arr);
	src = mono_string_chars(me);

	for (pos = sindex; pos != count + sindex; pos++) {
		for (loop = 0; loop != arraysize; loop++)
			if ( src [pos] == mono_array_get(arr, gunichar2, loop) )
				return pos;
	}

	return -1;
}

gint32 
ves_icall_System_String_InternalLastIndexOf_Char (MonoString *me, gunichar2 value, gint32 sindex, gint32 count)
{
	gint32 pos;
	gunichar2 *src;

	MONO_ARCH_SAVE_REGS;

	src = mono_string_chars(me);
	for (pos = sindex; pos > sindex - count; pos--) {
		if (src [pos] == value)
			return pos;
	}

	return -1;
}

gint32 
ves_icall_System_String_InternalLastIndexOf_Str (MonoString *me, MonoString *value, gint32 sindex, gint32 count)
{
	gint32 lencmpstr;
	gint32 pos;
	gunichar2 *src;
	gunichar2 *cmpstr;

	MONO_ARCH_SAVE_REGS;

	lencmpstr = mono_string_length(value);

	src = mono_string_chars(me);
	cmpstr = mono_string_chars(value);

	for (pos = sindex - lencmpstr + 1; pos > sindex - count; pos--) {
		if (0 == memcmp(src + pos, cmpstr, lencmpstr * sizeof(gunichar2)))
			return pos;
	}

	return -1;
}

gint32 
ves_icall_System_String_InternalLastIndexOfAny (MonoString *me, MonoArray *anyOf, gint32 sindex, gint32 count)
{
	gint32 pos;
	gint32 loop;
	gint32 arraysize;
	gunichar2 *src;

	MONO_ARCH_SAVE_REGS;

	arraysize = mono_array_length(anyOf);
	src = mono_string_chars(me);

	for (pos = sindex; pos > sindex - count; pos--) {
		for (loop = 0; loop != arraysize; loop++)
			if ( src [pos] == mono_array_get(anyOf, gunichar2, loop) )
				return pos;
	}

	return -1;
}

MonoString *
ves_icall_System_String_InternalPad (MonoString *me, gint32 width, gunichar2 chr, MonoBoolean right)
{
	MonoString * ret;
	gunichar2 *src;
	gunichar2 *dest;
	gint32 fillcount;
	gint32 srclen;
	gint32 i;

	MONO_ARCH_SAVE_REGS;

	srclen = mono_string_length(me);
	src = mono_string_chars(me);

	ret = mono_string_new_size( mono_domain_get (), width);
	dest = mono_string_chars(ret);
	fillcount = width - srclen;

	if (right) {
		memcpy(dest, src, srclen * sizeof(gunichar2));
		for (i = srclen; i != width; i++)
			dest[i] = chr;

		return ret;
	}

	/* left fill */
	for (i = 0; i != fillcount; i++)
		dest[i] = chr;

	memcpy(dest + fillcount, src, srclen * sizeof(gunichar2));

	return ret;
}

MonoString *
ves_icall_System_String_InternalAllocateStr (gint32 length)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_new_size(mono_domain_get (), length);
}

void 
ves_icall_System_String_InternalStrcpy_Str (MonoString *dest, gint32 destPos, MonoString *src)
{
	gunichar2 *srcptr;
	gunichar2 *destptr;

	MONO_ARCH_SAVE_REGS;

	srcptr = mono_string_chars (src);
	destptr = mono_string_chars (dest);

	g_memmove (destptr + destPos, srcptr, mono_string_length(src) * sizeof(gunichar2));
}

void 
ves_icall_System_String_InternalStrcpy_StrN (MonoString *dest, gint32 destPos, MonoString *src, gint32 startPos, gint32 count)
{
	gunichar2 *srcptr;
	gunichar2 *destptr;

	MONO_ARCH_SAVE_REGS;

	srcptr = mono_string_chars (src);
	destptr = mono_string_chars (dest);
	g_memmove (destptr + destPos, srcptr + startPos, count * sizeof(gunichar2));
}

void 
ves_icall_System_String_InternalStrcpy_Chars (MonoString *dest, gint32 destPos, MonoArray *src)
{
	gunichar2 *srcptr;
	gunichar2 *destptr;

	MONO_ARCH_SAVE_REGS;

	srcptr = mono_array_addr (src, gunichar2, 0);
	destptr = mono_string_chars (dest);

	g_memmove (destptr + destPos, srcptr, mono_array_length (src) * sizeof(gunichar2));
}

void 
ves_icall_System_String_InternalStrcpy_CharsN (MonoString *dest, gint32 destPos, MonoArray *src, gint32 startPos, gint32 count)
{
	gunichar2 *srcptr;
	gunichar2 *destptr;

	MONO_ARCH_SAVE_REGS;

	srcptr = mono_array_addr (src, gunichar2, 0);
	destptr = mono_string_chars (dest);

	g_memmove (destptr + destPos, srcptr + startPos, count * sizeof(gunichar2));
}

MonoString  *
ves_icall_System_String_InternalIntern (MonoString *str)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_intern(str);
}

MonoString * 
ves_icall_System_String_InternalIsInterned (MonoString *str)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_is_interned(str);
}

gint32
ves_icall_System_String_GetHashCode (MonoString *me)
{
	int i, h = 0;
	gunichar2 *data = mono_string_chars (me);

	MONO_ARCH_SAVE_REGS;

	for (i = 0; i < mono_string_length (me); ++i)
		h = (h << 5) - h + data [i];

	return h;
}

gunichar2 
ves_icall_System_String_get_Chars (MonoString *me, gint32 idx)
{
	MONO_ARCH_SAVE_REGS;

	if ((idx < 0) || (idx >= mono_string_length (me)))
		mono_raise_exception (mono_get_exception_index_out_of_range ());
	return mono_string_chars(me)[idx];
}

void
ves_icall_System_String_InternalCharCopy (gunichar2 *src, gunichar2 *dest, gint32 count)
{
	MONO_ARCH_SAVE_REGS;

	memcpy (dest, src, sizeof (gunichar2) * count);
}

/*
 * @mode:
 * 0 = StringCompareModeDirect
 * 1 = StringCompareModeCaseInsensitive
 * 2 = StringCompareModeOrdinal
 */
static gint32 
string_icall_cmp_char (gunichar2 c1, gunichar2 c2, gint32 mode)
{
	gint32 result;
	GUnicodeType c1type, c2type;

	c1type = g_unichar_type (c1);
	c2type = g_unichar_type (c2);

	switch (mode) {
	case 0:	
		/* TODO: compare with culture info */
		if (c1type == G_UNICODE_UPPERCASE_LETTER && c2type == G_UNICODE_LOWERCASE_LETTER)
			return 1;
					
		if (c1type == G_UNICODE_LOWERCASE_LETTER && c2type == G_UNICODE_UPPERCASE_LETTER)
			return -1;
	
		result = (gint32) c1 - c2;
		break;
	case 1:	
		result = (gint32) (c1type != G_UNICODE_LOWERCASE_LETTER ? g_unichar_tolower(c1) : c1) - 
				  (c2type != G_UNICODE_LOWERCASE_LETTER ? g_unichar_tolower(c2) : c2);
		break;
	case 2:
		/* Rotor/ms return the full value just not -1 and 1 */
		return (gint32) c1 - c2; break;
	}

	return ((result < 0) ? -1 : (result > 0) ? 1 : 0);
}
