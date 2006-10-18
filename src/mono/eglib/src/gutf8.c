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

gunichar2*
g_utf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **error)
{
	/* The conversion logic is almost identical to UTF8Encoding.GetChars(),
	   but error check is always done at utf8_to_utf16_len() so that
	   the conversion core below simply resets erroreous bits */
	glong utf16_len;
	gunichar2 *ret;
	gchar ch, mb_size, mb_remain;
	guint32 codepoint;
	glong in_pos, out_pos;

	utf16_len = 0;
	mb_size = 0;
	mb_remain = 0;
	in_pos = 0;
	out_pos = 0;

	if (error)
		*error = NULL;

	utf16_len = utf8_to_utf16_len (str, len, items_read, error);
	if (error)
		if (*error)
			return NULL;
	if (utf16_len < 0)
		return NULL;

	ret = g_malloc ((1 + utf16_len) * sizeof (gunichar2));

	for (in_pos = 0; len < 0 ? str [in_pos] : in_pos < len; in_pos++) {
		ch = (guchar) str [in_pos];
		if (mb_size == 0) {
			if (0 < ch)
				ret [out_pos++] = ch;
			else if ((ch & 0xE0) == 0xC0) {
				codepoint = ch & 0x1F;
				mb_remain = mb_size = 2;
			} else if ((ch & 0xF0) == 0xE0) {
				codepoint = ch & 0x0F;
				mb_remain = mb_size = 3;
			} else if ((ch & 0xF8) == 0xF0) {
				codepoint = ch & 7;
				mb_remain = mb_size = 4;
			} else if ((ch & 0xFC) == 0xF8) {
				codepoint = ch & 3;
				mb_remain = mb_size = 5;
			} else if ((ch & 0xFE) == 0xFC) {
				codepoint = ch & 3;
				mb_remain = mb_size = 6;
			} else {
				/* invalid utf-8 sequence */
				codepoint = 0;
				mb_remain = mb_size = 0;
			}
		} else {
			if ((ch & 0xC0) == 0x80) {
				codepoint = (codepoint << 6) | (ch & 0x3F);
				if (--mb_remain == 0) {
					/* multi byte character is fully consumed now. */
					if (codepoint < 0x10000) {
						ret [out_pos++] = codepoint;
					} else if (codepoint < 0x110000) {
						/* surrogate pair */
						codepoint -= 0x10000;
						ret [out_pos++] = (codepoint >> 10) + 0xD800;
						ret [out_pos++] = (codepoint & 0x3FF) + 0xDC00;
					} else {
						/* invalid utf-8 sequence (excess) */
						codepoint = 0;
						mb_remain = mb_size = 0;
					}
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

static glong
utf8_to_utf16_len (const gchar *str, glong len, glong *items_read, GError **error)
{
	/* It is almost identical to UTF8Encoding.GetCharCount() */
	guchar ch, mb_size, mb_remain;
	gboolean overlong;
	guint32 codepoint;
	glong in_pos, ret;

	mb_size = 0;
	mb_remain = 0;
	overlong = 0;
	in_pos = 0;
	ret = 0;

	for (in_pos = 0; len < 0 ? str [in_pos] : in_pos < len; in_pos++) {
		ch = str [in_pos];
		if (mb_size == 0) {
			if (ch < 0x80)
				ret++;
			else if ((ch & 0xE0) == 0xC0) {
				codepoint = ch & 0x1F;
				mb_remain = mb_size = 2;
			} else if ((ch & 0xF0) == 0xE0) {
				codepoint = ch & 0x0F;
				mb_remain = mb_size = 3;
			} else if ((ch & 0xF8) == 0xF0) {
				codepoint = ch & 7;
				mb_remain = mb_size = 4;
			} else if ((ch & 0xFC) == 0xF8) {
				codepoint = ch & 3;
				mb_remain = mb_size = 5;
			} else if ((ch & 0xFE) == 0xFC) {
				codepoint = ch & 3;
				mb_remain = mb_size = 6;
			} else {
				/* invalid utf-8 sequence */
				if (error) {
					g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "invalid utf-8 sequence at %d", in_pos);
					if (items_read)
						*items_read = in_pos;
					return -1;
				} else {
					codepoint = 0;
					mb_remain = mb_size = 0;
				}
			}
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
								mb_remain = mb_size = 0;
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
							mb_remain = mb_size = 0;
						}
					}
				}
			} else {
				/* invalid utf-8 sequence */
				if (error) {
					g_set_error (error, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "invalid utf-8 sequence at %d", in_pos);
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
	guint32 codepoint;
	gboolean surrogate;

	in_pos = 0;
	out_pos = 0;
	surrogate = FALSE;

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
			surrogate = 0;
			if (ch >= 0xDC00 && ch <= 0xDFFF)
				codepoint = 0x10000 + (ch - 0xDC00) + ((surrogate - 0xD800) << 10);
			else
				/* invalid surrogate pair */
				continue;
		} else {
			/* fast path optimization */
			if (ch < 0x80) {
				for (; len < 0 ? str [in_pos] : in_pos < len; in_pos++) {
					if (str [in_pos] < 0x80)
						ret [out_pos++] = str [in_pos];
					else
						break;
				}
				continue;
			}
			else if (ch >= 0xD800 && ch <= 0xDBFF)
				surrogate = ch;
			else if (ch >= 0xDC00 && ch <= 0xDFFF) {
				/* invalid surrogate pair */
				continue;
			}
			else
				codepoint = ch;
		}
		in_pos++;

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
