/*
 * gutf8.c: UTF-8 conversion
 *
 * Author:
 *   Atsushi Enomoto  <atsushi@ximian.com>
 *
 * (C) 2006 Novell, Inc.
 */

#include <stdio.h>
#include <glib.h>

gpointer error_quark = "ERROR";

static glong utf8_to_utf16_len (const gchar *str, glong len, glong *items_read, GError **error);
static glong utf16_to_utf8_len (const gunichar2 *str, glong len, glong *items_read, GError **error);

gpointer
g_convert_error_quark ()
{
	return error_quark;
}

static gunichar*
utf8_case_conv (const gchar *str, gssize len, gboolean upper)
{
	glong i, u16len, u32len;
	gunichar2 *u16str;
	gunichar *u32str;
	gchar *u8str;
	GError **err = NULL;

	u16str = g_utf8_to_utf16 (str, (glong)len, NULL, &u16len, err);
	u32str = g_utf16_to_ucs4 (u16str, u16len, NULL, &u32len, err);
	for (i = 0; i < u32len; i++) {
		u32str [i] = upper ? g_unichar_toupper (u32str [i]) : g_unichar_tolower (u32str [i]);
	}
	g_free (u16str);
	u16str = g_ucs4_to_utf16 (u32str, u32len, NULL, &u16len, err);
	u8str = g_utf16_to_utf8 (u16str, u16len, NULL, NULL, err);
	g_free (u32str);
	g_free (u16str);
	return (gunichar*)u8str;
}

gchar*
g_utf8_strup (const gchar *str, gssize len)
{
	return (gchar*)utf8_case_conv (str, len, TRUE);
}

gchar*
g_utf8_strdown (const gchar *str, gssize len)
{
	return (gchar*)utf8_case_conv (str, len, FALSE);
}

static glong
utf8_to_utf16_len (const gchar *str, glong len, glong *items_read, GError **error)
{
	/* It is almost identical to UTF8Encoding.GetCharCount() */
	guchar ch, mb_size, mb_remain;
	gboolean overlong;
	guint32 codepoint;
	glong in_pos, ret;

	if (len < 0)
		len = (glong) strlen (str);

	in_pos = 0;
	ret = 0;

	/* Common case */
	for (in_pos = 0; in_pos < len && (guchar) str [in_pos] < 0x80; in_pos++)
		ret ++;

	if (in_pos == len) {
		if (items_read)
			*items_read = in_pos;
		return ret;
	}

	mb_size = 0;
	mb_remain = 0;
	overlong = 0;

	for (; in_pos < len; in_pos++) {
		ch = str [in_pos];
		if (mb_size == 0) {
			if (ch < 0x80)
				ret++;
			else if ((ch & 0xE0) == 0xC0) {
				codepoint = ch & 0x1F;
				mb_size = 2;
			} else if ((ch & 0xF0) == 0xE0) {
				codepoint = ch & 0x0F;
				mb_size = 3;
			} else if ((ch & 0xF8) == 0xF0) {
				codepoint = ch & 7;
				mb_size = 4;
			} else if ((ch & 0xFC) == 0xF8) {
				codepoint = ch & 3;
				mb_size = 5;
			} else if ((ch & 0xFE) == 0xFC) {
				codepoint = ch & 3;
				mb_size = 6;
			} else {
				/* invalid utf-8 sequence */
				if (error) {
					g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "invalid utf-8 sequence at %d (illegal first byte)", in_pos);
					if (items_read)
						*items_read = in_pos;
					return -1;
				} else {
					codepoint = 0;
					mb_remain = mb_size = 0;
				}
			}
			if (mb_size > 1)
				mb_remain = mb_size - 1;
		} else {
			if ((ch & 0xC0) == 0x80) {
				codepoint = (codepoint << 6) | (ch & 0x3F);
				if (--mb_remain == 0) {
					/* multi byte character is fully consumed now. */
					if (codepoint < 0x10000) {
						switch (mb_size) {
						case 2:
							overlong = codepoint < 0x7F;
							break;
						case 3:
							overlong = codepoint < 0x7FF;
							break;
						case 4:
							overlong = codepoint < 0xFFFF;
							break;
						case 5:
							overlong = codepoint < 0x1FFFFF;
							break;
						case 6:
							overlong = codepoint < 0x03FFFFFF;
							break;
						}
						if (overlong) {
							/* invalid utf-8 sequence (overlong) */
							if (error) {
								g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "invalid utf-8 sequence at %d (overlong)", in_pos);
								if (items_read)
									*items_read = in_pos;
								return -1;
							} else {
								codepoint = 0;
								mb_remain = 0;
								overlong = FALSE;
							}
						}
						else
							ret++;
					} else if (codepoint < 0x110000) {
						/* surrogate pair */
						ret += 2;
					} else {
						/* invalid utf-8 sequence (excess) */
						if (error) {
							g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "invalid utf-8 sequence at %d (codepoint range excess)", in_pos);
							if (items_read)
								*items_read = in_pos;
							return -1;
						} else {
							codepoint = 0;
							mb_remain = 0;
						}
					}
					mb_size = 0;
				}
			} else {
				/* invalid utf-8 sequence */
				if (error) {
					g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "invalid utf-8 sequence at %d (illegal following bytes)", in_pos);
					if (items_read)
						*items_read = in_pos;
					return -1;
				} else {
					codepoint = 0;
					mb_remain = mb_size = 0;
				}
			}
		}
	}

	if (items_read)
		*items_read = in_pos;
	return ret;
}

gunichar2*
g_utf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **error)
{
	/* The conversion logic is almost identical to UTF8Encoding.GetChars(),
	   but error check is always done at utf8_to_utf16_len() so that
	   the conversion core below simply resets erroreous bits */
	glong utf16_len;
	gunichar2 *ret;
	guchar ch, mb_size, mb_remain;
	guint32 codepoint;
	glong in_pos, out_pos;

	utf16_len = 0;
	mb_size = 0;
	mb_remain = 0;
	in_pos = 0;
	out_pos = 0;

	if (error)
		*error = NULL;

	if (len < 0)
		len = (glong) strlen (str);

	if (items_read)
		*items_read = 0;
	if (items_written)
		*items_written = 0;
	utf16_len = utf8_to_utf16_len (str, len, items_read, error);
	if (error)
		if (*error)
			return NULL;
	if (utf16_len < 0)
		return NULL;

	ret = g_malloc ((1 + utf16_len) * sizeof (gunichar2));

	/* Common case */
	for (in_pos = 0; in_pos < len; in_pos++) {
		ch = (guchar) str [in_pos];

		if (ch >= 0x80)
			break;
		ret [out_pos++] = ch;
	}

	for (; in_pos < len; in_pos++) {
		ch = (guchar) str [in_pos];
		if (mb_size == 0) {
			if (ch < 0x80)
				ret [out_pos++] = ch;
			else if ((ch & 0xE0) == 0xC0) {
				codepoint = ch & 0x1F;
				mb_size = 2;
			} else if ((ch & 0xF0) == 0xE0) {
				codepoint = ch & 0x0F;
				mb_size = 3;
			} else if ((ch & 0xF8) == 0xF0) {
				codepoint = ch & 7;
				mb_size = 4;
			} else if ((ch & 0xFC) == 0xF8) {
				codepoint = ch & 3;
				mb_size = 5;
			} else if ((ch & 0xFE) == 0xFC) {
				codepoint = ch & 3;
				mb_size = 6;
			} else {
				/* invalid utf-8 sequence */
				codepoint = 0;
				mb_remain = mb_size = 0;
			}
			if (mb_size > 1)
				mb_remain = mb_size - 1;
		} else {
			if ((ch & 0xC0) == 0x80) {
				codepoint = (codepoint << 6) | (ch & 0x3F);
				if (--mb_remain == 0) {
					/* multi byte character is fully consumed now. */
					if (codepoint < 0x10000) {
						ret [out_pos++] = (gunichar2)(codepoint % 0x10000);
					} else if (codepoint < 0x110000) {
						/* surrogate pair */
						codepoint -= 0x10000;
						ret [out_pos++] = (gunichar2)((codepoint >> 10) + 0xD800);
						ret [out_pos++] = (gunichar2)((codepoint & 0x3FF) + 0xDC00);
					} else {
						/* invalid utf-8 sequence (excess) */
						codepoint = 0;
						mb_remain = 0;
					}
					mb_size = 0;
				}
			} else {
				/* invalid utf-8 sequence */
				codepoint = 0;
				mb_remain = mb_size = 0;
			}
		}
	}

	ret [out_pos] = 0;
	if (items_written)
		*items_written = out_pos;
	return ret;
}

gchar*
g_utf16_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **error)
{
	/* The conversion logic is almost identical to UTF8Encoding.GetBytes(),
	   but error check is always done at utf16_to_utf8_len() so that
	   the conversion core below simply resets erroreous bits */
	glong utf8_len;
	gchar *ret;
	glong in_pos, out_pos;
	gunichar2 ch;
	guint32 codepoint = 0;
	gboolean surrogate;

	in_pos = 0;
	out_pos = 0;
	surrogate = FALSE;

	if (items_read)
		*items_read = 0;
	if (items_written)
		*items_written = 0;
	utf8_len = utf16_to_utf8_len (str, len, items_read, error);
	if (error)
		if (*error)
			return NULL;
	if (utf8_len < 0)
		return NULL;

	ret = g_malloc ((1+utf8_len) * sizeof (gchar));

	while (len < 0 ? str [in_pos] : in_pos < len) {
		ch = str [in_pos];
		if (surrogate) {
			if (ch >= 0xDC00 && ch <= 0xDFFF) {
				codepoint = 0x10000 + (ch - 0xDC00) + ((surrogate - 0xD800) << 10);
				surrogate = 0;
			} else {
				surrogate = 0;
				/* invalid surrogate pair */
				++in_pos;
				continue;
			}
		} else {
			/* fast path optimization */
			if (ch < 0x80) {
				for (; len < 0 ? str [in_pos] : in_pos < len; in_pos++) {
					if (str [in_pos] < 0x80)
						ret [out_pos++] = (gchar)(str [in_pos]);
					else
						break;
				}
				continue;
			}
			else if (ch >= 0xD800 && ch <= 0xDBFF)
				surrogate = ch;
			else if (ch >= 0xDC00 && ch <= 0xDFFF) {
				++in_pos;
				/* invalid surrogate pair */
				continue;
			}
			else
				codepoint = ch;
		}
		in_pos++;

		if (surrogate != 0)
			continue;
		if (codepoint < 0x80)
			ret [out_pos++] = (gchar) codepoint;
		else if (codepoint < 0x0800) {
			ret [out_pos++] = (gchar) (0xC0 | (codepoint >> 6));
			ret [out_pos++] = (gchar) (0x80 | (codepoint & 0x3F));
		} else if (codepoint < 0x10000) {
			ret [out_pos++] = (gchar) (0xE0 | (codepoint >> 12));
			ret [out_pos++] = (gchar) (0x80 | ((codepoint >> 6) & 0x3F));
			ret [out_pos++] = (gchar) (0x80 | (codepoint & 0x3F));
		} else {
			ret [out_pos++] = (gchar) (0xF0 | (codepoint >> 18));
			ret [out_pos++] = (gchar) (0x80 | ((codepoint >> 12) & 0x3F));
			ret [out_pos++] = (gchar) (0x80 | ((codepoint >> 6) & 0x3F));
			ret [out_pos++] = (gchar) (0x80 | (codepoint & 0x3F));
		}
	}
	ret [out_pos] = 0;

	if (items_written)
		*items_written = out_pos;
	return ret;
}

static glong
utf16_to_utf8_len (const gunichar2 *str, glong len, glong *items_read, GError **error)
{
	glong ret, in_pos;
	gunichar2 ch;
	gboolean surrogate;

	ret = 0;
	in_pos = 0;
	surrogate = FALSE;

	while (len < 0 ? str [in_pos] : in_pos < len) {
		ch = str [in_pos];
		if (surrogate) {
			if (ch >= 0xDC00 && ch <= 0xDFFF) {
				ret += 4;
			} else {
				/* invalid surrogate pair */
				if (error) {
					g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "invalid utf-16 sequence at %d (missing surrogate tail)", in_pos);
					if (items_read)
						*items_read = in_pos;
					return -1;
				} /* otherwise just ignore. */
			}
			surrogate = FALSE;
		} else {
			/* fast path optimization */
			if (ch < 0x80) {
				for (; len < 0 ? str [in_pos] : in_pos < len; in_pos++) {
					if (str [in_pos] < 0x80)
						++ret;
					else
						break;
				}
				continue;
			}
			else if (ch < 0x0800)
				ret += 2;
			else if (ch >= 0xD800 && ch <= 0xDBFF)
				surrogate = TRUE;
			else if (ch >= 0xDC00 && ch <= 0xDFFF) {
				/* invalid surrogate pair */
				if (error) {
					g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "invalid utf-16 sequence at %d (missing surrogate head)", in_pos);
					if (items_read)
						*items_read = in_pos;
					return -1;
				} /* otherwise just ignore. */
			}
			else
				ret += 3;
		}
		in_pos++;
	}

	if (items_read)
		*items_read = in_pos;
	return ret;
}

static glong
g_ucs4_to_utf16_len (const gunichar *str, glong len, glong *items_read, GError **error)
{
	glong retlen = 0;
	glong errindex = 0;
	const gunichar *lstr = str;

	if (!str)
		return 0;

	while (*lstr != '\0' && len--) {
		gunichar ch;
		ch = *lstr++;
		if (ch <= 0x0000FFFF) { 
			if (ch >= 0xD800 && ch <= 0xDFFF) {
				errindex = (glong)(lstr - str)-1;
				if (error)
					g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					"Invalid sequence in conversion input");
				if (items_read)
					*items_read = errindex;
				return 0;
			} else {
				retlen++;
			}
		} else if (ch > 0x10FFFF) {
			errindex = (glong)(lstr - str)-1;
			if (error)
				g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
				"Character out of range for UTF-16");
			if (items_read)
				*items_read = errindex;
			return 0;

		} else {
			retlen+=2;
		}
	}

	if (items_read)
		*items_read = (glong)(lstr - str);
	return retlen;
}

gunichar2*
g_ucs4_to_utf16 (const gunichar *str, glong len, glong *items_read, glong *items_written, GError **error)
{
	glong allocsz;
	gunichar2 *retstr = 0;
	gunichar2 *retch = 0;
	glong nwritten = 0;
	GError *lerror =0 ;

	allocsz = g_ucs4_to_utf16_len (str, len, items_read, &lerror);

	if (!lerror) {
		retch = retstr = g_malloc ((allocsz+1) * sizeof (gunichar2));
		retstr[allocsz] = '\0';

		while (*str != '\0' && len--) {
			gunichar ch;
			ch = *str++;
			if (ch <= 0x0000FFFF && (ch < 0xD800 || ch > 0xDFFF)) {
				*retch++ = (gunichar2)ch;
				nwritten ++;
			} else {
				ch -= 0x0010000UL;
				*retch++ = (gunichar2)((ch >> 10) + 0xD800);
				*retch++ = (gunichar2)((ch & 0x3FFUL) + 0xDC00);
				nwritten +=2;
			}
		}
	}

	if (items_written)
		*items_written = nwritten;
	if (error)
		*error = lerror;

	return retstr;
}

static glong
g_utf16_to_ucs4_len (const gunichar2 *str, glong len, glong *items_read, GError **error)
{
	glong retlen = 0;
	glong errindex = 0;
	const gunichar2 *lstr = str;
	gunichar2 ch,ch2;

	if (!str)
		return 0;

	while (*lstr != '\0' && len--) {
		ch = *lstr++;
		if (ch >= 0xD800 && ch <= 0xDBFF) {
			if (!len--) {
				lstr--;
				break;
			}
			ch2 = *lstr;
			if (ch2 >= 0xDC00 && ch2 <= 0xDFFF) {
				lstr++;
			} else {
				errindex = (glong)(lstr - str);
				if (error)
					g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					"Invalid sequence in conversion input");
				if (items_read)
					*items_read = errindex;
				return 0;
			}
		} else {
			if (ch >= 0xDC00 && ch <= 0xDFFF) {
				errindex = (glong)(lstr - str)-1;
				if (error)
					g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					"Invalid sequence in conversion input");
				if (items_read)
					*items_read = errindex;
				return 0;
			}
		}
		retlen++;
	}

	if (items_read)
		*items_read = (glong)(lstr - str);

	return retlen;
}

gunichar*
g_utf16_to_ucs4 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **error)
{
	glong allocsz;
	gunichar *retstr = 0;
	gunichar *retch = 0;
	glong nwritten = 0;
	GError *lerror =0 ;
	gunichar ch,ch2;

	allocsz = g_utf16_to_ucs4_len (str, len, items_read, &lerror);

	if (!lerror) {
		retch = retstr = g_malloc ((allocsz+1) * sizeof (gunichar));
		retstr[allocsz] = '\0';
		nwritten = allocsz;

		while (*str != '\0' && allocsz--) {
			ch = *str++;
			if (ch >= 0xD800 && ch <= 0xDBFF) {
				ch2 = *str++;
				ch = ((ch - (gunichar)0xD800) << 10)
				      + (ch2 - (gunichar)0xDC00) + (gunichar)0x0010000UL;
			}
			*retch++ = ch;
		}
	}

	if (items_written)
		*items_written = nwritten;
	if (error)
		*error = lerror;

	return retstr;
}
