/*
 * string-icalls.c: String internal calls for the corlib
 *
 * Author:
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
 *   Duncan Mak  (duncan@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */
#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <signal.h>
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

/* System.StringSplitOptions */
typedef enum {
	STRINGSPLITOPTIONS_NONE = 0,
	STRINGSPLITOPTIONS_REMOVE_EMPTY_ENTRIES = 1
} StringSplitOptions;

MonoArray * 
ves_icall_System_String_InternalSplit (MonoString *me, MonoArray *separator, gint32 count, gint32 options)
{
	static MonoClass *String_array;
	MonoString * tmpstr;
	MonoArray * retarr;
	gunichar2 *src;
	gint32 arrsize, srcsize, splitsize;
	gint32 i, lastpos, arrpos;
	gint32 tmpstrsize;
	gint32 remempty;
	gint32 flag;
	gunichar2 *tmpstrptr;

	remempty = options & STRINGSPLITOPTIONS_REMOVE_EMPTY_ENTRIES;
	src = mono_string_chars (me);
	srcsize = mono_string_length (me);
	arrsize = mono_array_length (separator);

	if (!String_array) {
		MonoClass *klass = mono_array_class_get (mono_get_string_class (), 1);
		mono_memory_barrier ();
		String_array = klass;
	}

	splitsize = 1;
	/* Count the number of elements we will return. Note that this operation
	 * guarantees that we will return exactly splitsize elements, and we will
	 * have enough data to fill each. This allows us to skip some checks later on.
	 */
	if (remempty == 0) {
		for (i = 0; i != srcsize && splitsize < count; i++) {
			if (string_icall_is_in_array (separator, arrsize, src [i]))
				splitsize++;
		}
	} else if (count > 1) {
		/* Require pattern "Nondelim + Delim + Nondelim" to increment counter.
		 * Lastpos != 0 means first nondelim found.
		 * Flag = 0 means last char was delim.
		 * Efficient, though perhaps confusing.
		 */
		lastpos = 0;
		flag = 0;
		for (i = 0; i != srcsize && splitsize < count; i++) {
			if (string_icall_is_in_array (separator, arrsize, src [i])) {
				flag = 0;
			} else if (flag == 0) {
				if (lastpos == 1)
					splitsize++;
				flag = 1;
				lastpos = 1;
			}
		}

		/* Nothing but separators */
		if (lastpos == 0) {
			retarr = mono_array_new_specific (mono_class_vtable (mono_domain_get (), String_array), 0);
			return retarr;
		}
	}

	/* if no split chars found return the string */
	if (splitsize == 1) {
		if (remempty == 0 || count == 1) {
			/* Copy the whole string */
			retarr = mono_array_new_specific (mono_class_vtable (mono_domain_get (), String_array), 1);
			mono_array_setref (retarr, 0, me);
		} else {
			/* otherwise we have to filter out leading & trailing delims */

			/* find first non-delim char */
			for (; srcsize != 0; srcsize--, src++) {
				if (!string_icall_is_in_array (separator, arrsize, src [0]))
					break;
			}
			/* find last non-delim char */
			for (; srcsize != 0; srcsize--) {
				if (!string_icall_is_in_array (separator, arrsize, src [srcsize - 1]))
					break;
			}
			tmpstr = mono_string_new_size (mono_domain_get (), srcsize);
			tmpstrptr = mono_string_chars (tmpstr);

			memcpy (tmpstrptr, src, srcsize * sizeof (gunichar2));
			retarr = mono_array_new_specific (mono_class_vtable (mono_domain_get (), String_array), 1);
			mono_array_setref (retarr, 0, tmpstr);
		}
		return retarr;
	}

	lastpos = 0;
	arrpos = 0;
	
	retarr = mono_array_new_specific (mono_class_vtable (mono_domain_get (), String_array), splitsize);

	for (i = 0; i != srcsize && arrpos != splitsize; i++) {
		if (string_icall_is_in_array (separator, arrsize, src [i])) {
			
			if (lastpos != i || remempty == 0) {
				tmpstrsize = i - lastpos;
				tmpstr = mono_string_new_size (mono_domain_get (), tmpstrsize);
				tmpstrptr = mono_string_chars (tmpstr);

				memcpy (tmpstrptr, src + lastpos, tmpstrsize * sizeof (gunichar2));
				mono_array_setref (retarr, arrpos, tmpstr);
				arrpos++;

				if (arrpos == splitsize - 1) {
					/* Shortcut the last array element */

					lastpos = i + 1;
					if (remempty != 0) {
						/* Search for non-delim starting char (guaranteed to find one) Note that loop
						 * condition is only there for safety. It will never actually terminate the loop. */
						for (; lastpos != srcsize ; lastpos++) {
							if (!string_icall_is_in_array (separator, arrsize, src [lastpos])) 
								break;
						}
						if (count > splitsize) {
							/* Since we have fewer results than our limit, we must remove
							 * trailing delimiters as well. 
							 */
							for (; srcsize != lastpos + 1 ; srcsize--) {
								if (!string_icall_is_in_array (separator, arrsize, src [srcsize - 1])) 
									break;
							}
						}
					}

					tmpstrsize = srcsize - lastpos;
					tmpstr = mono_string_new_size (mono_domain_get (), tmpstrsize);
					tmpstrptr = mono_string_chars (tmpstr);

					memcpy (tmpstrptr, src + lastpos, tmpstrsize * sizeof (gunichar2));
					mono_array_setref (retarr, arrpos, tmpstr);

					/* Loop will ALWAYS end here. Test criteria in the FOR loop is technically unnecessary. */
					break;
				}
			}
			lastpos = i + 1;
		}
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

