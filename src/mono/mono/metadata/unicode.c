/*
 * unicode.h: Unicode support
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <errno.h>

#include <mono/metadata/object.h>
#include <mono/metadata/unicode.h>

#if HAVE_ICONV_H
#include <iconv.h> 
#elif HAVE_GICONV_H
#include <giconv.h> 
#endif

static MonoUnicodeCategory catmap[] = {
	/* G_UNICODE_CONTROL = */              Control,
	/* G_UNICODE_FORMAT = */               Format,
	/* G_UNICODE_UNASSIGNED = */           OtherNotAssigned,
	/* G_UNICODE_PRIVATE_USE = */          PrivateUse,
	/* G_UNICODE_SURROGATE = */            Surrogate,
	/* G_UNICODE_LOWERCASE_LETTER = */     LowercaseLetter,
	/* G_UNICODE_MODIFIER_LETTER = */      ModifierLetter,
	/* G_UNICODE_OTHER_LETTER = */         OtherLetter,
	/* G_UNICODE_TITLECASE_LETTER = */     TitlecaseLetter,
	/* G_UNICODE_UPPERCASE_LETTER = */     UppercaseLetter,
	/* G_UNICODE_COMBINING_MARK = */       SpaceCombiningMark,
	/* G_UNICODE_ENCLOSING_MARK = */       EnclosingMark,
	/* G_UNICODE_NON_SPACING_MARK = */     NonSpacingMark,
	/* G_UNICODE_DECIMAL_NUMBER = */       DecimalDigitNumber,
	/* G_UNICODE_LETTER_NUMBER = */        LetterNumber,
	/* G_UNICODE_OTHER_NUMBER = */         OtherNumber,
	/* G_UNICODE_CONNECT_PUNCTUATION = */  ConnectorPunctuation,
	/* G_UNICODE_DASH_PUNCTUATION = */     DashPunctuation,
	/* G_UNICODE_CLOSE_PUNCTUATION = */    ClosePunctuation,
	/* G_UNICODE_FINAL_PUNCTUATION = */    FinalQuotePunctuation,
	/* G_UNICODE_INITIAL_PUNCTUATION = */  InitialQuotePunctuation,
	/* G_UNICODE_OTHER_PUNCTUATION = */    OtherPunctuation,
	/* G_UNICODE_OPEN_PUNCTUATION = */     OpenPunctuation,
	/* G_UNICODE_CURRENCY_SYMBOL = */      CurrencySymbol,
	/* G_UNICODE_MODIFIER_SYMBOL = */      ModifierSymbol,
	/* G_UNICODE_MATH_SYMBOL = */          MathSymbol,
	/* G_UNICODE_OTHER_SYMBOL = */         OtherSymbol,
	/* G_UNICODE_LINE_SEPARATOR = */       LineSeperator,
	/* G_UNICODE_PARAGRAPH_SEPARATOR = */  ParagraphSeperator,
	/* G_UNICODE_SPACE_SEPARATOR = */      SpaceSeperator,
};

double 
ves_icall_System_Char_GetNumericValue (gunichar2 c)
{
	return (double)g_unichar_digit_value (c);
}

MonoUnicodeCategory 
ves_icall_System_Char_GetUnicodeCategory (gunichar2 c)
{
	return catmap [g_unichar_type (c)];
}

gboolean 
ves_icall_System_Char_IsControl (gunichar2 c)
{
	return g_unichar_iscntrl (c);
}

gboolean 
ves_icall_System_Char_IsDigit (gunichar2 c)
{
	return g_unichar_isdigit (c);
}

gboolean 
ves_icall_System_Char_IsLetter (gunichar2 c)
{
	return g_unichar_isalpha (c);
}

gboolean 
ves_icall_System_Char_IsLower (gunichar2 c)
{
	return g_unichar_islower (c);
}

gboolean 
ves_icall_System_Char_IsUpper (gunichar2 c)
{
	return g_unichar_isupper (c);
}

gboolean 
ves_icall_System_Char_IsNumber (gunichar2 c)
{
	GUnicodeType t = g_unichar_type (c);
	return t == G_UNICODE_DECIMAL_NUMBER ||
		t == G_UNICODE_LETTER_NUMBER ||
		t == G_UNICODE_OTHER_NUMBER;
}

gboolean 
ves_icall_System_Char_IsPunctuation (gunichar2 c)
{
	return g_unichar_ispunct (c);
}

gboolean 
ves_icall_System_Char_IsSeparator (gunichar2 c)
{
	GUnicodeType t = g_unichar_type (c);

	return (t == G_UNICODE_LINE_SEPARATOR ||
		t == G_UNICODE_PARAGRAPH_SEPARATOR ||
		t == G_UNICODE_SPACE_SEPARATOR);
}

gboolean 
ves_icall_System_Char_IsSurrogate (gunichar2 c)
{
	return (g_unichar_type (c) == G_UNICODE_SURROGATE);
}

gboolean 
ves_icall_System_Char_IsSymbol (gunichar2 c)
{
	GUnicodeType t = g_unichar_type (c);

	return (t == G_UNICODE_CURRENCY_SYMBOL ||
		t == G_UNICODE_MODIFIER_SYMBOL ||
		t == G_UNICODE_MATH_SYMBOL ||
		t == G_UNICODE_OTHER_SYMBOL);
}

gboolean 
ves_icall_System_Char_IsWhiteSpace (gunichar2 c)
{
	return g_unichar_isspace (c);
}

gunichar2
ves_icall_System_Char_ToLower (gunichar2 c)
{
	return g_unichar_tolower (c);
}

gunichar2
ves_icall_System_Char_ToUpper (gunichar2 c)
{
	return g_unichar_toupper (c);
}

gpointer
ves_icall_iconv_new_encoder (MonoString *name, MonoBoolean big_endian)
{
	iconv_t cd;
	char *n;

	// fixme: don't enforce big endian, support old iconv

	g_assert (name);

	n = mono_string_to_utf8 (name);

	/* force big endian before class libraries are fixed */
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	big_endian = 1;
#endif 

#ifdef HAVE_NEW_ICONV
	cd = iconv_open (n, big_endian ? "UTF-16be" : "UTF-16le");
#else
	cd = iconv_open (n, "UTF-16");
#endif
	g_assert (cd != (iconv_t)-1);
	g_free (n);

	return (gpointer)cd;
}

gpointer
ves_icall_iconv_new_decoder (MonoString *name, MonoBoolean big_endian)
{
	iconv_t cd;
	char *n;

	// fixme: don't enforce big endian, support old iconv

	g_assert (name);

	n = mono_string_to_utf8 (name);

	/* force big endian before class libraries are fixed */
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	big_endian = 1;
#endif 

#ifdef HAVE_NEW_ICONV
	cd = iconv_open (big_endian ? "UTF-16be" : "UTF-16le", n);
#else
	cd = iconv_open ("UTF-16", n);
#endif
	g_assert (cd != (iconv_t)-1);
	g_free (n);

	return (gpointer)cd;
}

void
ves_icall_iconv_reset (gpointer converter)
{
	iconv_t cd = (iconv_t)converter;

	g_assert (cd);

	iconv(cd, NULL, NULL, NULL, NULL);	
}

static int
iconv_get_length (iconv_t cd, guchar *src, int len, gboolean encode)
{
	guchar buf [512];
	int res;
	guchar *outp;
	guchar *p;
	guint inbytes_remaining;
	guint outbytes_remaining;
	guint outbuf_size;
	gboolean have_error = FALSE;
	size_t err;
	
	g_assert (cd);
	g_assert (src);

#ifndef HAVE_NEW_ICONV
	if (G_BYTE_ORDER == G_LITTLE_ENDIAN && encode) {
		int i;
		
		src = g_memdup (src, len);
		for (i = 0; i < len; i += 2) {
			char t = src [i];
			src [i] = src [i + 1];
			src [i + 1] = t;
		}
	}
#endif

	p = src;
	inbytes_remaining = len;
	res = 0;
	
again:
	outbuf_size = 512;
	outbytes_remaining = outbuf_size;
	outp = buf;

	err = iconv (cd, (char **)&p, &inbytes_remaining, 
		     (char **)&outp, &outbytes_remaining);

	if(err == (size_t)-1) {
		switch(errno) {
		case EINVAL:
			/* Incomplete text, do not report an error */
			break;
		case E2BIG: {
			res += outp - buf;
			goto again;
		}
		case EILSEQ:
			p++;
			inbytes_remaining--;
			goto again;
		default:
			have_error = TRUE;
			break;
		}
	}
  
	res += outp - buf;

	if((p - src) != len) {
		if(!have_error) {
			have_error = TRUE;
		}
	}

#ifndef HAVE_NEW_ICONV
	if (G_BYTE_ORDER == G_LITTLE_ENDIAN && encode)
		g_free (src);
#endif

	if (have_error) {
		g_assert_not_reached ();
		return 0;
	} else {
		return res;
	}
}

int
ves_icall_iconv_get_byte_count (gpointer converter, MonoArray *chars, gint32 idx, gint32 count)
{
	iconv_t cd = (iconv_t)converter;
	guchar *src;
	int len;

	g_assert (cd);
	g_assert (chars);
	g_assert (mono_array_length (chars) > idx);
	g_assert (mono_array_length (chars) >= (idx + count));

	if (!(len = (mono_array_length (chars) - idx) * 2))
		return 0;

	src =  mono_array_addr (chars, guint16, idx);

	return iconv_get_length (cd, src, len, TRUE);
}

static int
iconv_convert (iconv_t cd, guchar *src, int len, guchar *dest, int max_len, gboolean encode)
{
	guchar *p, *outp;
	guint inbytes_remaining;
	guint outbytes_remaining;
	guint outbuf_size;
	gboolean have_error = FALSE;
	size_t err;

	g_assert (cd);
	g_assert (src);
	g_assert (dest);

#ifndef HAVE_NEW_ICONV
	if (G_BYTE_ORDER == G_LITTLE_ENDIAN && encode) {
		int i;
		
		src = g_memdup (src, len);
		for (i = 0; i < len; i += 2) {
			char t = src [i];
			src [i] = src [i + 1];
			src [i + 1] = t;
		}
	}
#endif

	p = src;
	inbytes_remaining = len;
	outbuf_size = max_len;
  
	outbytes_remaining = outbuf_size;
	outp = dest;

 again:
	err = iconv (cd, (char **)&p, &inbytes_remaining, (char **)&outp, &outbytes_remaining);

	if(err == (size_t)-1) {
		if (errno == EINVAL) {
			/* Incomplete text, do not report an error */
		} else if (errno == EILSEQ) {
			p++;
			inbytes_remaining--;
			goto again;
		} else {
			have_error = TRUE;
		}
	}

	if ((p - src) != len) {
		if (!have_error) {
			have_error = TRUE;
		}
	}

#ifndef HAVE_NEW_ICONV
	if (G_BYTE_ORDER == G_LITTLE_ENDIAN) {
		if (encode) {
			g_free (src);
		} else {
			int mb = max_len - outbytes_remaining;
			int i;
			for (i = 0; i < mb; i+=2) {
				char t = dest [i];
				dest [i] = dest [i + 1];
				dest [i + 1] = t;
			}
		}
}
#endif
	if (have_error) {
		g_assert_not_reached ();
		return 0;
	} else {
		/* we return the number of bytes written in dest */
		return max_len - outbytes_remaining;
	}
}

int
ves_icall_iconv_get_bytes (gpointer converter, MonoArray *chars, gint32 charIndex, gint32 charCount,
			   MonoArray *bytes, gint32 byteIndex)
{
	iconv_t cd = (iconv_t)converter;
	guchar *src, *dest;
	int len, max_len;

	if (!charCount)
		return 0;

	g_assert (cd);
	g_assert (chars);
	g_assert (bytes);
	g_assert (mono_array_length (chars) > charIndex);
	g_assert (mono_array_length (chars) >= (charIndex + charCount));
	g_assert (mono_array_length (bytes) > byteIndex);
	g_assert (mono_array_length (chars) >= (byteIndex + charCount));

	if (!(len = (charCount - charIndex) * 2))
		return 0;

	src =  mono_array_addr (chars, guint16, charIndex);
	dest = mono_array_addr (bytes, char, byteIndex);

	max_len = mono_array_length (bytes) - byteIndex;

	return iconv_convert (cd, src, len, dest, max_len, TRUE);
}

int
ves_icall_iconv_get_char_count (gpointer converter, MonoArray *bytes, gint32 idx, gint32 count)
{
	iconv_t cd = (iconv_t)converter;
	guchar *src;

	g_assert (cd);
	g_assert (bytes);
	g_assert (mono_array_length (bytes) > idx);
	g_assert (mono_array_length (bytes) >= (idx + count));

	src =  mono_array_addr (bytes, char, idx);

	/* iconv_get_length () returns the number of bytes */
	return iconv_get_length (cd, src, (int) count, FALSE) / 2;
}

int
ves_icall_iconv_get_chars (gpointer converter, MonoArray *bytes, gint32 byteIndex, gint32 byteCount,
			   MonoArray *chars, gint32 charIndex)
{
	iconv_t cd = (iconv_t)converter;
	guchar *src, *dest;
	int max_len;

	g_assert (cd);
	g_assert (chars);
	g_assert (bytes);
	g_assert (mono_array_length (bytes) > byteIndex);
	g_assert (mono_array_length (chars) >= (byteIndex + byteCount));
	g_assert (mono_array_length (chars) > charIndex);

	src =  mono_array_addr (bytes, char, byteIndex);
	dest = mono_array_addr (chars, guint16, charIndex);

	max_len = (mono_array_length (chars) - charIndex) * 2;

	/* iconv_convert () returns the number of bytes */
	return iconv_convert (cd, src, (int) byteCount, dest, max_len, FALSE) / 2;
}
