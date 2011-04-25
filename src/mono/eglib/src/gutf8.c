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

static gpointer error_quark = "ConvertError";

static glong utf8_to_utf16_len (const gchar *str, glong len, glong *items_read, GError **error);
static glong utf16_to_utf8_len (const gunichar2 *str, glong len, glong *items_read, GError **error);

gpointer
g_convert_error_quark (void)
{
	return error_quark;
}

static gchar *
utf8_case_conv (const gchar *str, gssize len, gboolean upper)
{
	gunichar *ustr;
	glong i, ulen;
	gchar *utf8;
	
	//ustr = g_utf8_to_ucs4 (str, (glong) len, NULL, &ulen, NULL);
	ustr = g_utf8_to_ucs4_fast (str, (glong) len, &ulen);
	for (i = 0; i < ulen; i++)
		ustr[i] = upper ? g_unichar_toupper (ustr[i]) : g_unichar_tolower (ustr[i]);
	utf8 = g_ucs4_to_utf8 (ustr, ulen, NULL, NULL, NULL);
	g_free (ustr);
	
	return utf8;
}

gchar *
g_utf8_strup (const gchar *str, gssize len)
{
	return utf8_case_conv (str, len, TRUE);
}

gchar *
g_utf8_strdown (const gchar *str, gssize len)
{
	return utf8_case_conv (str, len, FALSE);
}

gunichar
g_utf8_get_char_validated (const gchar *str, gssize max_len)
{
	gushort extra_bytes = 0;

	if (max_len == 0)
		return -2;
	
	extra_bytes = g_trailingBytesForUTF8 [(unsigned char) *str];

	if (max_len <= extra_bytes)
		return -2;

	if (g_utf8_validate (str, max_len, NULL))
		return g_utf8_get_char (str);
	
	return -1;
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

gchar *
g_ucs4_to_utf8 (const gunichar *str, glong len, glong *items_read, glong *items_written, GError **error)
{
	gchar *outbuf, *outptr;
	glong nwritten = 0;
	glong i;
	gint n;
	
	if (len == -1) {
		for (i = 0; str[i] != 0; i++) {
			if ((n = g_unichar_to_utf8 (str[i], NULL)) < 0) {
				g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Invalid sequence in conversion input");
				
				if (items_read)
					*items_read = i;
				
				return NULL;
			}
			
			nwritten += n;
		}
	} else {
		for (i = 0; i < len; i++) {
			if ((n = g_unichar_to_utf8 (str[i], NULL)) < 0) {
				g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Invalid sequence in conversion input");
				
				if (items_read)
					*items_read = i;
				
				return NULL;
			}
			
			nwritten += n;
		}
	}
	
	outptr = outbuf = g_malloc (nwritten + 1);
	if (len == -1) {
		for (i = 0; str[i] != 0; i++)
			outptr += g_unichar_to_utf8 (str[i], outptr);
	} else {
		for (i = 0; i < len; i++)
			outptr += g_unichar_to_utf8 (str[i], outptr);
	}
	*outptr = '\0';
	
	if (items_written)
		*items_written = nwritten;
	
	if (items_read != 0)
		*items_read = i;
	
	return outbuf;
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

gchar *
g_utf8_find_prev_char (const gchar *str, const gchar *p)
{
	while (p > str) {
		p--;
		if ((*p && 0xc0) != 0xb0)
			return (gchar *)p;
	}
	return NULL;
}

gchar *
g_utf8_prev_char (const gchar *str)
{
	const gchar *p = str;
	do {
		p--;
	} while ((*p & 0xc0) == 0xb0);
	
	return (gchar *)p;
}

gchar *
g_utf8_offset_to_pointer (const gchar *str, glong offset)
{
	const gchar *p = str;

	if (offset > 0) {
		do {
			p = g_utf8_next_char (p);
			offset --;
		} while (offset > 0);
	}
	else if (offset < 0) {
		const gchar *jump = str;
		do {
			// since the minimum size of a character is 1
			// we know we can step back at least offset bytes
			jump = jump + offset;
			
			// if we land in the middle of a character
			// walk to the beginning
			while ((*jump & 0xc0) == 0xb0)
				jump --;
			
			// count how many characters we've actually walked
			// by going forward
			p = jump;
			do {
				p = g_utf8_next_char (p);
				offset ++;
			} while (p < jump);
			
		} while (offset < 0);
	}
	
	return (gchar *)p;
}

glong
g_utf8_pointer_to_offset (const gchar *str, const gchar *pos)
{
	const gchar *inptr, *inend;
	glong offset = 0;
	glong sign = 1;
	
	if (pos == str)
		return 0;
	
	if (str < pos) {
		inptr = str;
		inend = pos;
	} else {
		inptr = pos;
		inend = str;
		sign = -1;
	}
	
	do {
		inptr = g_utf8_next_char (inptr);
		offset++;
	} while (inptr < inend);
	
	return offset * sign;
}

gunichar*
g_utf8_to_ucs4_fast (const gchar *str, glong len, glong *items_written)
{
	gunichar* ucs4;
	int ucs4_index;
	const char *p;
	int mb_size;
	gunichar codepoint;

	g_return_val_if_fail (str != NULL, NULL);
	
	if (len < 0) {
		/* we need to find the length of str, as len < 0 means it must be 0 terminated */

		len = 0;
		p = str;
		while (*p) {
			len ++;
			p = g_utf8_next_char(p);
		}
	}

	ucs4 = g_malloc (sizeof(gunichar)*len);
	if (items_written)
		*items_written = len;

	p = str;
	ucs4_index = 0;
	while (len) {
		guint8 c = *p++;

		if (c < 0x80) {
			mb_size = 1;
		}
		else if (c < 0xe0) {
			c &= 0x1f;

			mb_size = 2;
		}
		else if (c < 0xf0) {
			c &= 0x0f;
			mb_size = 3;
		}
		else if (c < 0xf8) {
			c &= 0x07;
			mb_size = 4;
		}
		else if (c < 0xfc) {
			c &= 0x03;
			mb_size = 5;
		}
		else if (c < 0xfe) {
			c &= 0x01;
			mb_size = 6;
		}

		codepoint = c;
		while (--mb_size) {
			codepoint = (codepoint << 6) | ((*p) & 0x3f);
			p++;
		}

		ucs4[ucs4_index++] = codepoint;
		len --;
	}

	return ucs4;
}

/**
 * from http://home.tiscali.nl/t876506/utf8tbl.html
 *
 * From Unicode UCS-4 to UTF-8:
 * Start with the Unicode number expressed as a decimal number and call this ud.
 *
 * If ud <128 (7F hex) then UTF-8 is 1 byte long, the value of ud.
 *
 * If ud >=128 and <=2047 (7FF hex) then UTF-8 is 2 bytes long.
 *    byte 1 = 192 + (ud div 64)
 *    byte 2 = 128 + (ud mod 64)
 *
 * If ud >=2048 and <=65535 (FFFF hex) then UTF-8 is 3 bytes long.
 *    byte 1 = 224 + (ud div 4096)
 *    byte 2 = 128 + ((ud div 64) mod 64)
 *    byte 3 = 128 + (ud mod 64)
 *
 * If ud >=65536 and <=2097151 (1FFFFF hex) then UTF-8 is 4 bytes long.
 *    byte 1 = 240 + (ud div 262144)
 *    byte 2 = 128 + ((ud div 4096) mod 64)
 *    byte 3 = 128 + ((ud div 64) mod 64)
 *    byte 4 = 128 + (ud mod 64)
 *
 * If ud >=2097152 and <=67108863 (3FFFFFF hex) then UTF-8 is 5 bytes long.
 *    byte 1 = 248 + (ud div 16777216)
 *    byte 2 = 128 + ((ud div 262144) mod 64)
 *    byte 3 = 128 + ((ud div 4096) mod 64)
 *    byte 4 = 128 + ((ud div 64) mod 64)
 *    byte 5 = 128 + (ud mod 64)
 *
 * If ud >=67108864 and <=2147483647 (7FFFFFFF hex) then UTF-8 is 6 bytes long.
 *    byte 1 = 252 + (ud div 1073741824)
 *    byte 2 = 128 + ((ud div 16777216) mod 64)
 *    byte 3 = 128 + ((ud div 262144) mod 64)
 *    byte 4 = 128 + ((ud div 4096) mod 64)
 *    byte 5 = 128 + ((ud div 64) mod 64)
 *    byte 6 = 128 + (ud mod 64)
 **/
gint
g_unichar_to_utf8 (gunichar c, gchar *outbuf)
{
	size_t len, i;
	int base;
	
	if (c < 128UL) {
		base = 0;
		len = 1;
	} else if (c < 2048UL) {
		base = 192;
		len = 2;
	} else if (c < 65536UL) {
		base = 224;
		len = 3;
	} else if (c < 2097152UL) {
		base = 240;
		len = 4;
	} else if (c < 67108864UL) {
		base = 248;	
		len = 5;
	} else if (c < 2147483648UL) {
		base = 252;
		len = 6;
	} else {
		return -1;
	}
	
	if (outbuf != NULL) {
		for (i = len - 1; i > 0; i--) {
			/* mask off 6 bits worth and add 128 */
			outbuf[i] = 128 + (c & 0x3f);
			c >>= 6;
		}
		
		/* first character has a different base */
		outbuf[0] = base + c;
	}
	
	return len;
}
