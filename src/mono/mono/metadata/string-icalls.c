/*
 * string-icalls.c: String internal calls for the corlib
 *
 * Author:
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
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

MonoString * 
mono_string_InternalJoin (MonoString *separator, MonoArray * value, gint32 sindex, gint32 count)
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

	insert = mono_string_chars(separator);
	insertlen = mono_string_length(separator);

	length = 0;
	for (pos = sindex; pos != sindex + count; pos++) {
		length += mono_string_length(mono_array_get(value, MonoString *, pos));
		if (pos < sindex + count - 1)
			length += insertlen;
	}

	ret = mono_string_InternalAllocateStr(length);
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
mono_string_InternalInsert (MonoString *me, gint32 sindex, MonoString *value)
{
	MonoString * ret;
	gunichar2 *src;
	gunichar2 *insertsrc;
	gunichar2 *dest;
	gint32 srclen;

	src = mono_string_chars(me);
	srclen = mono_string_length(value);

	ret = mono_string_InternalAllocateStr(mono_string_length(me) + srclen - sindex);
	dest = mono_string_chars(ret);

	memcpy(dest, src, sindex * sizeof(gunichar2));
	memcpy(dest + sindex, insertsrc, srclen * sizeof(gunichar2));

	return ret;
}

MonoString * 
mono_string_InternalReplaceChar (MonoString *me, gunichar2 oldChar, gunichar2 newChar)
{
	g_warning("mono_string_InternalReplaceChar not impl");
	return mono_string_new_utf16(mono_domain_get(), mono_string_chars(me), mono_string_length(me));
}

MonoString * 
mono_string_InternalReplaceStr (MonoString *me, MonoString *oldValue, MonoString *newValue)
{
	g_warning("mono_string_InternalReplaceStr not impl");
	return mono_string_new_utf16(mono_domain_get(), mono_string_chars(me), mono_string_length(me));
}

MonoString * 
mono_string_InternalRemove (MonoString *me, gint32 sindex, gint32 count)
{
	MonoString * ret;
	gint32 count_bytes;
	gint32 index_bytes;
	gunichar2 *dest;
	gunichar2 *src;

	ret = mono_string_InternalAllocateStr(mono_string_length(me) - count);
	index_bytes = sindex * sizeof(gunichar2);
	count_bytes = count * sizeof(gunichar2);

	src = mono_string_chars(me);
	dest = mono_string_chars(ret);

	memcpy(dest, src, index_bytes);
	memcpy(dest + sindex, src + sindex + count, index_bytes - count_bytes);

	return ret;
}

void
mono_string_InternalCopyTo (MonoString *me, gint32 sindex, MonoArray *dest, gint32 dindex, gint32 count)
{
	gunichar2 *destptr = (gunichar2 *) mono_array_addr(dest, gunichar2, dindex);
	gunichar2 *src =  mono_string_chars(me);

	memcpy(destptr, src + sindex, sizeof(gunichar2) * count);
}

MonoArray * 
mono_string_InternalSplit (MonoString *me, MonoArray *separator, gint32 count)
{
	MonoString * tmpstr;
	MonoArray * retarr;
	gunichar2 *src;
	gint32 arrsize, srcsize, splitsize;
	gint32 i, lastpos, arrpos;
	gint32 tmpstrsize;
	gunichar2 *tmpstrptr;

	src = mono_string_chars(me);
	srcsize = mono_string_length(me);
	arrsize = mono_array_length(separator);

	splitsize = 0;
	for (i = 0; i != srcsize && splitsize < count; i++) {
		if (mono_string_isinarray(separator, arrsize, src[i]))
			splitsize++;
	}

	lastpos = 0;
	arrpos = 0;

	/* if no split chars found return the string */
	if (splitsize == 0) {
		retarr = mono_array_new(mono_domain_get(), mono_defaults.string_class, 1);
		tmpstr = mono_string_InternalAllocateStr(srcsize);
		tmpstrptr = mono_string_chars(tmpstr);

		memcpy(tmpstrptr, src, srcsize * sizeof(gunichar2));
		mono_array_set(retarr, MonoString *, 0, tmpstr);

		return retarr;
	}

	if (splitsize + 1 < count)
		splitsize++;

	retarr = mono_array_new(mono_domain_get(), mono_defaults.string_class, splitsize);
	for (i = 0; i != srcsize && arrpos != count; i++) {
		if (mono_string_isinarray(separator, arrsize, src[i])) {
			if (arrpos == count - 1)
				tmpstrsize = srcsize - lastpos;
			else
				tmpstrsize = i - lastpos;

			tmpstr = mono_string_InternalAllocateStr(tmpstrsize);
			tmpstrptr = mono_string_chars(tmpstr);

			memcpy(tmpstrptr, src + lastpos, tmpstrsize * sizeof(gunichar2));
			mono_array_set(retarr, MonoString *, arrpos, tmpstr);
			arrpos++;
			lastpos = i + 1;
		}
	}

	if (arrpos < count) {
		tmpstrsize = srcsize - lastpos;
		tmpstr = mono_string_InternalAllocateStr(tmpstrsize);
		tmpstrptr = mono_string_chars(tmpstr);

		memcpy(tmpstrptr, src + lastpos, tmpstrsize * sizeof(gunichar2));
		mono_array_set(retarr, MonoString *, arrpos, tmpstr);
	}
	
	return retarr;
}

gboolean
mono_string_isinarray (MonoArray *chars, gint32 arraylength, gunichar2 chr)
{
	gunichar2 cmpchar;
	gint32 arrpos;

	for (arrpos = 0; arrpos != arraylength; arrpos++) {
		cmpchar = mono_array_get(chars, gunichar2, arrpos);
		if (mono_string_cmp_char(cmpchar, chr, 1) == 0)
			return TRUE;
	}
	
	return FALSE;
}

MonoString * 
mono_string_InternalTrim (MonoString *me, MonoArray *chars, gint32 typ)
{
	MonoString * ret;
	gunichar2 *src, *dest;
	gint32 srclen, newlen, arrlen;
	gint32 i, lenfirst, lenlast;

	srclen = mono_string_length(me);
	src = mono_string_chars(me);
	arrlen = mono_array_length(chars);

	lenfirst = 0;
	lenlast = 0;

	if (0 == typ || 1 == typ) {
		for (i = 0; i != srclen; i++) {
			if (mono_string_isinarray(chars, arrlen, src[i]))
				lenfirst++;
			else 
				break;
		}
	}

	if (0 == typ || 2 == typ) {
		for (i = srclen - lenfirst; i != 0; i--) {
			if (mono_string_isinarray(chars, arrlen, src[i]))
				lenlast++;
			else 
				break;
		}
	}

	newlen = srclen - lenfirst - lenlast;

	ret = mono_string_InternalAllocateStr(newlen);
	dest = mono_string_chars(ret);

	memcpy(dest, src + lenfirst, newlen *sizeof(gunichar2));

	return ret;
}

gint32 
mono_string_InternalIndexOfChar (MonoString *me, gunichar2 value, gint32 sindex, gint32 count)
{
	gint32 pos;
	gunichar2 *src;

	src = mono_string_chars(me);
	for (pos = sindex; pos != count + sindex; pos++) {
		if ( src [pos] == value)
			return pos;
	}

	return -1;
}

gint32 
mono_string_InternalIndexOfStr (MonoString *me, MonoString *value, gint32 sindex, gint32 count)
{
	gint32 lencmpstr;
	gint32 pos;
	gunichar2 *src;
	gunichar2 *cmpstr;

	lencmpstr = mono_string_length(value);

	src = mono_string_chars(me);
	cmpstr = mono_string_chars(value);

	for (pos = sindex; pos != count + sindex; pos++) {
		if (0 == memcmp(src + pos, cmpstr, lencmpstr * sizeof(gunichar2)))
			return pos;
	}

	return -1;
}

gint32 
mono_string_InternalIndexOfAny (MonoString *me, MonoArray *arr, gint32 sindex, gint32 count)
{
	gint32 pos;
	gint32 loop;
	gint32 arraysize;
	gunichar2 *src;

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
mono_string_InternalLastIndexOfChar (MonoString *me, gunichar2 value, gint32 sindex, gint32 count)
{
	gint32 pos;
	gunichar2 *src;

	src = mono_string_chars(me);
	for (pos = sindex; pos > sindex - count; pos--) {
		if (src [pos] == value)
			return pos;
	}

	return -1;
}

gint32 
mono_string_InternalLastIndexOfStr (MonoString *me, MonoString *value, gint32 sindex, gint32 count)
{
	gint32 lencmpstr;
	gint32 pos;
	gunichar2 *src;
	gunichar2 *cmpstr;

	lencmpstr = mono_string_length(value);

	src = mono_string_chars(me);
	cmpstr = mono_string_chars(value);

	for (pos = sindex; pos > sindex - count; pos -= lencmpstr) {
		if (0 == memcmp(src + pos, cmpstr, lencmpstr * sizeof(gunichar2)))
			return pos;
	}

	return -1;
}

gint32 
mono_string_InternalLastIndexOfAny (MonoString *me, MonoArray *anyOf, gint32 sindex, gint32 count)
{
	gint32 pos;
	gint32 loop;
	gint32 arraysize;
	gunichar2 *src;

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
mono_string_InternalPad (MonoString *me, gint32 width, gint16 chr, MonoBoolean right)
{
	MonoString * ret;
	gunichar2 *src;
	gunichar2 *dest;
	gint32 fillcount;
	gint32 srclen;
	gint32 i;

	srclen = mono_string_length(me);
	src = mono_string_chars(me);

	ret = mono_string_InternalAllocateStr(width);
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
mono_string_InternalToLower (MonoString *me)
{
	MonoString * ret;
	gunichar2 *src; 
	gunichar2 *dest;
	gint32 i;

	ret = mono_string_new_size(mono_domain_get (), mono_string_length(me));

	src = mono_string_chars (me);
	dest = mono_string_chars (ret);

	for (i = 0; i < mono_string_length (me); ++i)
		dest[i] = g_unichar_tolower(src[i]);

	return ret;
}

MonoString *
mono_string_InternalToUpper (MonoString *me)
{
	int i;
	MonoString * ret;
	gunichar2 *src; 
	gunichar2 *dest;

	ret = mono_string_new_size(mono_domain_get (), mono_string_length(me));

	src = mono_string_chars (me);
	dest = mono_string_chars (ret);

	for (i = 0; i < mono_string_length (me); ++i)
		dest[i] = g_unichar_toupper(src[i]);

	return ret;
}

MonoString *
mono_string_InternalAllocateStr(gint32 length)
{
	return mono_string_new_size(mono_domain_get (), length);
}

void 
mono_string_InternalStrcpyStr (MonoString *dest, gint32 destPos, MonoString *src)
{
	gunichar2 *srcptr;
	gunichar2 *destptr;

	srcptr = mono_string_chars(src);
	destptr = mono_string_chars(dest);

	memcpy(destptr + destPos, srcptr, mono_string_length(src) * sizeof(gunichar2));
}

void 
mono_string_InternalStrcpyStrN (MonoString *dest, gint32 destPos, MonoString *src, gint32 startPos, gint32 count)
{
	gunichar2 *srcptr;
	gunichar2 *destptr;

	srcptr = mono_string_chars(src);
	destptr = mono_string_chars(dest);
	memcpy(destptr + destPos, srcptr + startPos, count * sizeof(gunichar2));
}

MonoString  *
mono_string_InternalIntern (MonoString *str)
{
	return mono_string_intern(str);
}

MonoString * 
mono_string_InternalIsInterned (MonoString *str)
{
	return mono_string_is_interned(str);
}

gint32
mono_string_InternalCompareStrN (MonoString *s1, gint32 i1, MonoString *s2, gint32 i2, gint32 length, MonoBoolean inCase)
{
	/* c translation of C# code.. :)
	*/
	gint32 lenstr1;
	gint32 lenstr2;
	gunichar2 *str1;
	gunichar2 *str2;

	gint32 pos;
	gint16 mode;
	
	if (inCase)
		mode = 1;
	else
		mode = 0;

	lenstr1 = mono_string_length(s1);
	lenstr2 = mono_string_length(s2);

	str1 = mono_string_chars(s1);
	str2 = mono_string_chars(s2);

	pos = 0;

	for (pos = 0; pos != length; pos++) {
		if (i1 + pos >= lenstr1 || i2 + pos >= lenstr2)
			break;

		if (0 != mono_string_cmp_char(str1[i1 + pos], str2[i2 + pos], mode))
			break;
	}

	/* the lesser wins, so if we have looped until length we just need to check the last char */
	if (pos == length) {
		return mono_string_cmp_char(str1[i1 + pos - 1], str2[i2 + pos - 1], mode);
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
	return mono_string_cmp_char(str1[i1 + pos], str2[i2 + pos], mode);
}

gint32
mono_string_GetHashCode (MonoString *me)
{
	int i, h = 0;
	gunichar2 *data = mono_string_chars (me);

	for (i = 0; i < mono_string_length (me); ++i)
		h = (h << 5) - h + data [i];

	return h;
}

gunichar2 
mono_string_get_Chars (MonoString *me, gint32 idx)
{
	return mono_string_chars(me)[idx];
}

/* @mode :	0 = StringCompareModeDirect
			1 = StringCompareModeCaseInsensitive
			2 = StringCompareModeOrdinal
*/
gint32 
mono_string_cmp_char (gunichar2 c1, gunichar2 c2, gint16 mode)
{
	gint32 result;

	switch (mode) {
	case 0:	
		/* TODO: compare with culture info */
		if (g_unichar_isupper(c1) && g_unichar_islower(c2))
			return 1;
					
		if (g_unichar_islower(c1) && g_unichar_isupper(c2))
			return -1;
	
		result = (gint32) c1 - c2;
		break;
	case 1:	
		result = (gint32) g_unichar_tolower(c1) - g_unichar_tolower(c2);
		break;
		/* fix: compare ordinal */
	case 2:	
		result = (gint32) g_unichar_tolower(c1) - g_unichar_tolower(c2);
		break;
	}

	if (result < 0)
		return -1;

	if (result > 0)
		return 1;

	return 0;
}
