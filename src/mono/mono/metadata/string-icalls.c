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
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/unicode.h>
#include <mono/metadata/exception.h>

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

	g_warning("string.ctor with encoding obj unimplemented");
	g_assert_not_reached ();
	return NULL;
}

MonoBoolean 
ves_icall_System_String_InternalEquals (MonoString *str1, MonoString *str2)
{
	gunichar2 *str1ptr;
	gunichar2 *str2ptr;
	gint32 str1len;

	MONO_ARCH_SAVE_REGS;

	/* Length checking is done in C# */
	str1len = mono_string_length(str1);

	str1ptr = mono_string_chars(str1);
	str2ptr = mono_string_chars(str2);

	return (0 == memcmp(str1ptr, str2ptr, str1len * sizeof(gunichar2)));
}

MonoString * 
ves_icall_System_String_InternalJoin (MonoString *separator, MonoArray * value, gint32 sindex, gint32 count)
{
	MonoString * ret;
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
		length += mono_string_length(mono_array_get(value, MonoString *, pos));
		if (pos < sindex + count - 1)
			length += insertlen;
	}

	ret = mono_string_new_size( mono_domain_get (), length);
	dest = mono_string_chars(ret);
	destpos = 0;

	for (pos = sindex; pos != sindex + count; pos++) {
		src = mono_string_chars(mono_array_get(value, MonoString *, pos));
		srclen = mono_string_length(mono_array_get(value, MonoString *, pos));

		memcpy(dest + destpos, src, srclen * sizeof(gunichar2));
		destpos += srclen;

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
ves_icall_System_String_InternalReplace_Str (MonoString *me, MonoString *oldValue, MonoString *newValue)
{
	MonoString *ret;
	gunichar2 *src;
	gunichar2 *dest;
	gunichar2 *oldstr;
	gunichar2 *newstr;
	gint32 i, destpos;
	gint32 occurr;
	gint32 newsize;
	gint32 oldstrlen;
	gint32 newstrlen;
	gint32 srclen;

	MONO_ARCH_SAVE_REGS;

	occurr = 0;
	destpos = 0;

	oldstr = mono_string_chars(oldValue);
	oldstrlen = mono_string_length(oldValue);

	if (NULL != newValue) {
		newstr = mono_string_chars(newValue);
		newstrlen = mono_string_length(newValue);
	} else
		newstrlen = 0;

	src = mono_string_chars(me);
	srclen = mono_string_length(me);

	if (oldstrlen != newstrlen) {
		for (i = 0; i <= srclen - oldstrlen; i++)
			if (0 == memcmp(src + i, oldstr, oldstrlen * sizeof(gunichar2)))
				occurr++;
                if (occurr == 0)
                        return me;
		newsize = srclen + ((newstrlen - oldstrlen) * occurr);
 	} else
		newsize = srclen;

        ret = NULL;
	i = 0;
	while (i < srclen) {
		if (0 == memcmp(src + i, oldstr, oldstrlen * sizeof(gunichar2))) {
                        if (ret == NULL) {
                                ret = mono_string_new_size( mono_domain_get (), newsize);
                                dest = mono_string_chars(ret);
                                memcpy (dest, src, i * sizeof(gunichar2));
                        }
			if (newstrlen > 0) {
				memcpy(dest + destpos, newstr, newstrlen * sizeof(gunichar2));
				destpos += newstrlen;
			}
			i += oldstrlen;
                        continue;
		} else if (ret != NULL) {
			dest[destpos] = src[i];
 		}
			destpos++;
			i++;
		}
        
        if (ret == NULL)
                return me;

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
		retarr = mono_array_new(mono_domain_get(), mono_defaults.string_class, 1);
		tmpstr = mono_string_new_size( mono_domain_get (), srcsize);
		tmpstrptr = mono_string_chars(tmpstr);

		memcpy(tmpstrptr, src, srcsize * sizeof(gunichar2));
		mono_array_set(retarr, MonoString *, 0, tmpstr);

		return retarr;
	}

	if (splitsize != count)
		splitsize++;

	retarr = mono_array_new(mono_domain_get(), mono_defaults.string_class, splitsize);
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

	ret = mono_string_new_size( mono_domain_get (), newlen);
	dest = mono_string_chars(ret);

	memcpy(dest, src + lenfirst, newlen *sizeof(gunichar2));

	return ret;
}

gint32 
ves_icall_System_String_InternalIndexOf_Char (MonoString *me, gunichar2 value, gint32 sindex, gint32 count)
{
	gint32 pos;
	gunichar2 *src;

	MONO_ARCH_SAVE_REGS;

	src = mono_string_chars(me);
	for (pos = sindex; pos != count + sindex; pos++) {
		if ( src [pos] == value)
			return pos;
	}

	return -1;
}

gint32 
ves_icall_System_String_InternalIndexOf_Str (MonoString *me, MonoString *value, gint32 sindex, gint32 count)
{
	gint32 lencmpstr;
	gint32 pos, i;
	gunichar2 *src;
	gunichar2 *cmpstr;

	MONO_ARCH_SAVE_REGS;

	lencmpstr = mono_string_length(value);

	src = mono_string_chars(me);
	cmpstr = mono_string_chars(value);

	count -= lencmpstr;
	for (pos = sindex; pos <= sindex + count; pos++) {
		for (i = 0; src [pos + i] == cmpstr [i];) {
			if (++i == lencmpstr)
				return pos;
		}
	}

	return -1;
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
ves_icall_System_String_InternalToLower (MonoString *me)
{
	MonoString * ret;
	gunichar2 *src; 
	gunichar2 *dest;
	gint32 i;

	MONO_ARCH_SAVE_REGS;

	ret = mono_string_new_size(mono_domain_get (), mono_string_length(me));

	src = mono_string_chars (me);
	dest = mono_string_chars (ret);

	for (i = 0; i < mono_string_length (me); ++i)
		dest[i] = g_unichar_tolower(src[i]);

	return ret;
}

MonoString *
ves_icall_System_String_InternalToUpper (MonoString *me)
{
	int i;
	MonoString * ret;
	gunichar2 *src; 
	gunichar2 *dest;

	MONO_ARCH_SAVE_REGS;

	ret = mono_string_new_size(mono_domain_get (), mono_string_length(me));

	src = mono_string_chars (me);
	dest = mono_string_chars (ret);

	for (i = 0; i < mono_string_length (me); ++i)
		dest[i] = g_unichar_toupper(src[i]);

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

	memcpy(destptr + destPos, srcptr, mono_string_length(src) * sizeof(gunichar2));
}

void 
ves_icall_System_String_InternalStrcpy_StrN (MonoString *dest, gint32 destPos, MonoString *src, gint32 startPos, gint32 count)
{
	gunichar2 *srcptr;
	gunichar2 *destptr;

	MONO_ARCH_SAVE_REGS;

	srcptr = mono_string_chars (src);
	destptr = mono_string_chars (dest);
	memcpy(destptr + destPos, srcptr + startPos, count * sizeof(gunichar2));
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
ves_icall_System_String_InternalCompareStr_N (MonoString *s1, gint32 i1, MonoString *s2, gint32 i2, gint32 length, gint32 mode)
{
	/* c translation of C# code from old string.cs.. :) */
	gint32 lenstr1;
	gint32 lenstr2;
	gint32 charcmp;
	gunichar2 *str1;
	gunichar2 *str2;

	gint32 pos;
	
	MONO_ARCH_SAVE_REGS;

	lenstr1 = mono_string_length(s1);
	lenstr2 = mono_string_length(s2);

	str1 = mono_string_chars(s1);
	str2 = mono_string_chars(s2);

	pos = 0;

	for (pos = 0; pos != length; pos++) {
		if (i1 + pos >= lenstr1 || i2 + pos >= lenstr2)
			break;

		charcmp = string_icall_cmp_char(str1[i1 + pos], str2[i2 + pos], mode);
		if (charcmp != 0)
			return charcmp;
	}

	/* the lesser wins, so if we have looped until length we just need to check the last char */
	if (pos == length) {
		return string_icall_cmp_char(str1[i1 + pos - 1], str2[i2 + pos - 1], mode);
	}

	/* Test if one the strings has been compared to the end */
	if (i1 + pos >= lenstr1) {
		if (i2 + pos >= lenstr2)
			return 0;
		else
			return -1;
	} else if (i2 + pos >= lenstr2)
		return 1;

	/* if not, check our last char only.. (can this happen?) */
	return string_icall_cmp_char(str1[i1 + pos], str2[i2 + pos], mode);
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
		// Rotor/ms return the full value just not -1 and 1
		return (gint32) c1 - c2; break;
	}

	return ((result < 0) ? -1 : (result > 0) ? 1 : 0);
}
