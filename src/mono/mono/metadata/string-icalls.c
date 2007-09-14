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
#include <mono/metadata/exception.h>
#include <mono/metadata/debug-helpers.h>

/* Internal helper methods */

static gboolean
string_icall_is_in_array (MonoArray *chars, gint32 arraylength, gunichar2 chr);

/* This function is redirected to String.CreateString ()
   by mono_marshal_get_native_wrapper () */
void
ves_icall_System_String_ctor_RedirectToCreateString (void)
{
	g_assert_not_reached ();
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
		mono_array_setref (retarr, 0, me);

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
			mono_array_setref (retarr, arrpos, tmpstr);
			arrpos++;
			lastpos = i + 1;
		}
	}

	if (arrpos < count) {
		tmpstrsize = srcsize - lastpos;
		tmpstr = mono_string_new_size( mono_domain_get (), tmpstrsize);
		tmpstrptr = mono_string_chars(tmpstr);

		memcpy(tmpstrptr, src + lastpos, tmpstrsize * sizeof(gunichar2));
		mono_array_setref (retarr, arrpos, tmpstr);
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

gunichar2 
ves_icall_System_String_get_Chars (MonoString *me, gint32 idx)
{
	MONO_ARCH_SAVE_REGS;

	if ((idx < 0) || (idx >= mono_string_length (me)))
		mono_raise_exception (mono_get_exception_index_out_of_range ());
	return mono_string_chars(me)[idx];
}

