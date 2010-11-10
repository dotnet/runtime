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
#include <mono/metadata/profiler.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/gc-internal.h>

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
ves_icall_System_String_InternalAllocateStr (gint32 length)
{
	return mono_string_new_size(mono_domain_get (), length);
}

MonoString  *
ves_icall_System_String_InternalIntern (MonoString *str)
{
	MonoString *res;
	MONO_ARCH_SAVE_REGS;

	res = mono_string_intern(str);
	if (!res)
		mono_raise_exception (mono_domain_get ()->out_of_memory_ex);
	return res;
}

MonoString * 
ves_icall_System_String_InternalIsInterned (MonoString *str)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_is_interned(str);
}

int
ves_icall_System_String_GetLOSLimit (void)
{
#ifdef HAVE_SGEN_GC
	int limit = mono_gc_get_los_limit ();

	return (limit - 2 - sizeof (MonoString)) / 2;
#else
	return G_MAXINT;
#endif
}
