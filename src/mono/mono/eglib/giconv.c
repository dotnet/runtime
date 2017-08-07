/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 *  Copyright (C) 2011 Jeffrey Stedfast
 *
 *  Permission is hereby granted, free of charge, to any person
 *  obtaining a copy of this software and associated documentation
 *  files (the "Software"), to deal in the Software without
 *  restriction, including without limitation the rights to use, copy,
 *  modify, merge, publish, distribute, sublicense, and/or sell copies
 *  of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be
 *  included in all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 *  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 *  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 *  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 *  HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 *  WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <glib.h>
#include <string.h>
#ifdef HAVE_ICONV_H
#include <iconv.h>
#endif
#include <errno.h>

#ifdef _MSC_VER
#define FORCE_INLINE(RET_TYPE) __forceinline RET_TYPE
#else
#define FORCE_INLINE(RET_TYPE) inline RET_TYPE __attribute__((always_inline))
#endif


#define UNROLL_DECODE_UTF8 0
#define UNROLL_ENCODE_UTF8 0

typedef int (* Decoder) (char *inbuf, size_t inleft, gunichar *outchar);
typedef int (* Encoder) (gunichar c, char *outbuf, size_t outleft);

struct _GIConv {
	Decoder decode;
	Encoder encode;
	gunichar c;
#ifdef HAVE_ICONV
	iconv_t cd;
#endif
};

static int decode_utf32be (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_utf32be (gunichar c, char *outbuf, size_t outleft);

static int decode_utf32le (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_utf32le (gunichar c, char *outbuf, size_t outleft);

static int decode_utf16be (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_utf16be (gunichar c, char *outbuf, size_t outleft);

static int decode_utf16le (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_utf16le (gunichar c, char *outbuf, size_t outleft);

static FORCE_INLINE (int) decode_utf8 (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_utf8 (gunichar c, char *outbuf, size_t outleft);

static int decode_latin1 (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_latin1 (gunichar c, char *outbuf, size_t outleft);

#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define decode_utf32 decode_utf32le
#define encode_utf32 encode_utf32le
#define decode_utf16 decode_utf16le
#define encode_utf16 encode_utf16le
#else
#define decode_utf32 decode_utf32be
#define encode_utf32 encode_utf32be
#define decode_utf16 decode_utf16be
#define encode_utf16 encode_utf16be
#endif

static struct {
	const char *name;
	Decoder decoder;
	Encoder encoder;
} charsets[] = {
	{ "ISO-8859-1", decode_latin1,  encode_latin1  },
	{ "ISO8859-1",  decode_latin1,  encode_latin1  },
	{ "UTF-32BE",   decode_utf32be, encode_utf32be },
	{ "UTF-32LE",   decode_utf32le, encode_utf32le },
	{ "UTF-16BE",   decode_utf16be, encode_utf16be },
	{ "UTF-16LE",   decode_utf16le, encode_utf16le },
	{ "UTF-32",     decode_utf32,   encode_utf32   },
	{ "UTF-16",     decode_utf16,   encode_utf16   },
	{ "UTF-8",      decode_utf8,    encode_utf8    },
	{ "US-ASCII",   decode_latin1,  encode_latin1  },
	{ "Latin1",     decode_latin1,  encode_latin1  },
	{ "ASCII",      decode_latin1,  encode_latin1  },
	{ "UTF32",      decode_utf32,   encode_utf32   },
	{ "UTF16",      decode_utf16,   encode_utf16   },
	{ "UTF8",       decode_utf8,    encode_utf8    },
};


GIConv
g_iconv_open (const char *to_charset, const char *from_charset)
{
#ifdef HAVE_ICONV
	iconv_t icd = (iconv_t) -1;
#endif
	Decoder decoder = NULL;
	Encoder encoder = NULL;
	GIConv cd;
	guint i;
	
	if (!to_charset || !from_charset || !to_charset[0] || !from_charset[0]) {
		errno = EINVAL;
		
		return (GIConv) -1;
	}
	
	for (i = 0; i < G_N_ELEMENTS (charsets); i++) {
		if (!g_ascii_strcasecmp (charsets[i].name, from_charset))
			decoder = charsets[i].decoder;
		
		if (!g_ascii_strcasecmp (charsets[i].name, to_charset))
			encoder = charsets[i].encoder;
	}
	
	if (!encoder || !decoder) {
#ifdef HAVE_ICONV
		if ((icd = iconv_open (to_charset, from_charset)) == (iconv_t) -1)
			return (GIConv) -1;
#else
		errno = EINVAL;
		
		return (GIConv) -1;
#endif
	}
	
	cd = (GIConv) g_malloc (sizeof (struct _GIConv));
	cd->decode = decoder;
	cd->encode = encoder;
	cd->c = -1;
	
#ifdef HAVE_ICONV
	cd->cd = icd;
#endif
	
	return cd;
}

int
g_iconv_close (GIConv cd)
{
#ifdef HAVE_ICONV
	if (cd->cd != (iconv_t) -1)
		iconv_close (cd->cd);
#endif
	
	g_free (cd);
	
	return 0;
}

gsize
g_iconv (GIConv cd, gchar **inbytes, gsize *inbytesleft,
	 gchar **outbytes, gsize *outbytesleft)
{
	gsize inleft, outleft;
	char *inptr, *outptr;
	gunichar c;
	int rc = 0;
	
#ifdef HAVE_ICONV
	if (cd->cd != (iconv_t) -1) {
		/* Note: gsize may have a different size than size_t, so we need to
		   remap inbytesleft and outbytesleft to size_t's. */
		size_t *outleftptr, *inleftptr;
		size_t n_outleft, n_inleft;
		
		if (inbytesleft) {
			n_inleft = *inbytesleft;
			inleftptr = &n_inleft;
		} else {
			inleftptr = NULL;
		}
		
		if (outbytesleft) {
			n_outleft = *outbytesleft;
			outleftptr = &n_outleft;
		} else {
			outleftptr = NULL;
		}
#if defined(__NetBSD__)
		return iconv (cd->cd, (const gchar **)inbytes, inleftptr, outbytes, outleftptr);
#else
		return iconv (cd->cd, inbytes, inleftptr, outbytes, outleftptr);
#endif
	}
#endif
	
	if (outbytes == NULL || outbytesleft == NULL) {
		/* reset converter */
		cd->c = -1;
		return 0;
	}
	
	inleft = inbytesleft ? *inbytesleft : 0;
	inptr = inbytes ? *inbytes : NULL;
	outleft = *outbytesleft;
	outptr = *outbytes;
	
	if ((c = cd->c) != (gunichar) -1)
		goto encode;
	
	while (inleft > 0) {
		if ((rc = cd->decode (inptr, inleft, &c)) < 0)
			break;
		
		inleft -= rc;
		inptr += rc;
		
	encode:
		if ((rc = cd->encode (c, outptr, outleft)) < 0)
			break;
		
		c = (gunichar) -1;
		outleft -= rc;
		outptr += rc;
	}
	
	if (inbytesleft)
		*inbytesleft = inleft;
	
	if (inbytes)
		*inbytes = inptr;
	
	*outbytesleft = outleft;
	*outbytes = outptr;
	cd->c = c;
	
	return rc < 0 ? -1 : 0;
}

/*
 * Unicode encoders and decoders
 */

static int
decode_utf32be (char *inbuf, size_t inleft, gunichar *outchar)
{
	unsigned char *inptr = (unsigned char *) inbuf;
	gunichar c;
	
	if (inleft < 4) {
		errno = EINVAL;
		return -1;
	}
	
	c = (inptr[0] << 24) | (inptr[1] << 16) | (inptr[2] << 8) | inptr[3];
	
	if (c >= 0xd800 && c < 0xe000) {
		errno = EILSEQ;
		return -1;
	} else if (c >= 0x110000) {
		errno = EILSEQ;
		return -1;
	}
	
	*outchar = c;
	
	return 4;
}

static int
decode_utf32le (char *inbuf, size_t inleft, gunichar *outchar)
{
	unsigned char *inptr = (unsigned char *) inbuf;
	gunichar c;
	
	if (inleft < 4) {
		errno = EINVAL;
		return -1;
	}
	
	c = (inptr[3] << 24) | (inptr[2] << 16) | (inptr[1] << 8) | inptr[0];
	
	if (c >= 0xd800 && c < 0xe000) {
		errno = EILSEQ;
		return -1;
	} else if (c >= 0x110000) {
		errno = EILSEQ;
		return -1;
	}
	
	*outchar = c;
	
	return 4;
}

static int
encode_utf32be (gunichar c, char *outbuf, size_t outleft)
{
	unsigned char *outptr = (unsigned char *) outbuf;
	
	if (outleft < 4) {
		errno = E2BIG;
		return -1;
	}
	
	outptr[0] = (c >> 24) & 0xff;
	outptr[1] = (c >> 16) & 0xff;
	outptr[2] = (c >> 8) & 0xff;
	outptr[3] = c & 0xff;
	
	return 4;
}

static int
encode_utf32le (gunichar c, char *outbuf, size_t outleft)
{
	unsigned char *outptr = (unsigned char *) outbuf;
	
	if (outleft < 4) {
		errno = E2BIG;
		return -1;
	}
	
	outptr[0] = c & 0xff;
	outptr[1] = (c >> 8) & 0xff;
	outptr[2] = (c >> 16) & 0xff;
	outptr[3] = (c >> 24) & 0xff;
	
	return 4;
}

static int
decode_utf16be (char *inbuf, size_t inleft, gunichar *outchar)
{
	unsigned char *inptr = (unsigned char *) inbuf;
	gunichar2 c;
	gunichar u;
	
	if (inleft < 2) {
		errno = EINVAL;
		return -1;
	}
	
	u = (inptr[0] << 8) | inptr[1];
	
	if (u < 0xd800) {
		/* 0x0000 -> 0xd7ff */
		*outchar = u;
		return 2;
	} else if (u < 0xdc00) {
		/* 0xd800 -> 0xdbff */
		if (inleft < 4) {
			errno = EINVAL;
			return -2;
		}
		
		c = (inptr[2] << 8) | inptr[3];
		
		if (c < 0xdc00 || c > 0xdfff) {
			errno = EILSEQ;
			return -2;
		}
		
		u = ((u - 0xd800) << 10) + (c - 0xdc00) + 0x0010000UL;
		*outchar = u;
		
		return 4;
	} else if (u < 0xe000) {
		/* 0xdc00 -> 0xdfff */
		errno = EILSEQ;
		return -1;
	} else {
		/* 0xe000 -> 0xffff */
		*outchar = u;
		return 2;
	}
}

static int
decode_utf16le (char *inbuf, size_t inleft, gunichar *outchar)
{
	unsigned char *inptr = (unsigned char *) inbuf;
	gunichar2 c;
	gunichar u;
	
	if (inleft < 2) {
		errno = EINVAL;
		return -1;
	}
	
	u = (inptr[1] << 8) | inptr[0];
	
	if (u < 0xd800) {
		/* 0x0000 -> 0xd7ff */
		*outchar = u;
		return 2;
	} else if (u < 0xdc00) {
		/* 0xd800 -> 0xdbff */
		if (inleft < 4) {
			errno = EINVAL;
			return -2;
		}
		
		c = (inptr[3] << 8) | inptr[2];
		
		if (c < 0xdc00 || c > 0xdfff) {
			errno = EILSEQ;
			return -2;
		}
		
		u = ((u - 0xd800) << 10) + (c - 0xdc00) + 0x0010000UL;
		*outchar = u;
		
		return 4;
	} else if (u < 0xe000) {
		/* 0xdc00 -> 0xdfff */
		errno = EILSEQ;
		return -1;
	} else {
		/* 0xe000 -> 0xffff */
		*outchar = u;
		return 2;
	}
}

static int
encode_utf16be (gunichar c, char *outbuf, size_t outleft)
{
	unsigned char *outptr = (unsigned char *) outbuf;
	gunichar2 ch;
	gunichar c2;
	
	if (c < 0x10000) {
		if (outleft < 2) {
			errno = E2BIG;
			return -1;
		}
		
		outptr[0] = (c >> 8) & 0xff;
		outptr[1] = c & 0xff;
		
		return 2;
	} else {
		if (outleft < 4) {
			errno = E2BIG;
			return -1;
		}
		
		c2 = c - 0x10000;
		
		ch = (gunichar2) ((c2 >> 10) + 0xd800);
		outptr[0] = (ch >> 8) & 0xff;
		outptr[1] = ch & 0xff;
		
		ch = (gunichar2) ((c2 & 0x3ff) + 0xdc00);
		outptr[2] = (ch >> 8) & 0xff;
		outptr[3] = ch & 0xff;
		
		return 4;
	}
}

static int
encode_utf16le (gunichar c, char *outbuf, size_t outleft)
{
	unsigned char *outptr = (unsigned char *) outbuf;
	gunichar2 ch;
	gunichar c2;
	
	if (c < 0x10000) {
		if (outleft < 2) {
			errno = E2BIG;
			return -1;
		}
		
		outptr[0] = c & 0xff;
		outptr[1] = (c >> 8) & 0xff;
		
		return 2;
	} else {
		if (outleft < 4) {
			errno = E2BIG;
			return -1;
		}
		
		c2 = c - 0x10000;
		
		ch = (gunichar2) ((c2 >> 10) + 0xd800);
		outptr[0] = ch & 0xff;
		outptr[1] = (ch >> 8) & 0xff;
		
		ch = (gunichar2) ((c2 & 0x3ff) + 0xdc00);
		outptr[2] = ch & 0xff;
		outptr[3] = (ch >> 8) & 0xff;
		
		return 4;
	}
}

static FORCE_INLINE (int)
decode_utf8 (char *inbuf, size_t inleft, gunichar *outchar)
{
	unsigned char *inptr = (unsigned char *) inbuf;
	gunichar u;
	int n, i;
	
	u = *inptr;
	
	if (u < 0x80) {
		/* simple ascii case */
		*outchar = u;
		return 1;
	} else if (u < 0xc2) {
		errno = EILSEQ;
		return -1;
	} else if (u < 0xe0) {
		u &= 0x1f;
		n = 2;
	} else if (u < 0xf0) {
		u &= 0x0f;
		n = 3;
	} else if (u < 0xf8) {
		u &= 0x07;
		n = 4;
	} else if (u < 0xfc) {
		u &= 0x03;
		n = 5;
	} else if (u < 0xfe) {
		u &= 0x01;
		n = 6;
	} else {
		errno = EILSEQ;
		return -1;
	}
	
	if (n > inleft) {
		errno = EINVAL;
		return -1;
	}
	
#if UNROLL_DECODE_UTF8
	switch (n) {
	case 6: u = (u << 6) | (*++inptr ^ 0x80);
	case 5: u = (u << 6) | (*++inptr ^ 0x80);
	case 4: u = (u << 6) | (*++inptr ^ 0x80);
	case 3: u = (u << 6) | (*++inptr ^ 0x80);
	case 2: u = (u << 6) | (*++inptr ^ 0x80);
	}
#else
	for (i = 1; i < n; i++)
		u = (u << 6) | (*++inptr ^ 0x80);
#endif
	
	*outchar = u;
	
	return n;
}

static int
encode_utf8 (gunichar c, char *outbuf, size_t outleft)
{
	unsigned char *outptr = (unsigned char *) outbuf;
	int base, n, i;
	
	if (c < 0x80) {
		outptr[0] = c;
		return 1;
	} else if (c < 0x800) {
		base = 192;
		n = 2;
	} else if (c < 0x10000) {
		base = 224;
		n = 3;
	} else if (c < 0x200000) {
		base = 240;
		n = 4;
	} else if (c < 0x4000000) {
		base = 248;
		n = 5;
	} else {
		base = 252;
		n = 6;
	}
	
	if (outleft < n) {
		errno = E2BIG;
		return -1;
	}
	
#if UNROLL_ENCODE_UTF8
	switch (n) {
	case 6: outptr[5] = (c & 0x3f) | 0x80; c >>= 6;
	case 5: outptr[4] = (c & 0x3f) | 0x80; c >>= 6;
	case 4: outptr[3] = (c & 0x3f) | 0x80; c >>= 6;
	case 3: outptr[2] = (c & 0x3f) | 0x80; c >>= 6;
	case 2: outptr[1] = (c & 0x3f) | 0x80; c >>= 6;
	case 1: outptr[0] = c | base;
	}
#else
	for (i = n - 1; i > 0; i--) {
		outptr[i] = (c & 0x3f) | 0x80;
		c >>= 6;
	}
	
	outptr[0] = c | base;
#endif
	
	return n;
}

static int
decode_latin1 (char *inbuf, size_t inleft, gunichar *outchar)
{
	*outchar = (unsigned char) *inbuf;
	return 1;
}

static int
encode_latin1 (gunichar c, char *outbuf, size_t outleft)
{
	if (outleft < 1) {
		errno = E2BIG;
		return -1;
	}
	
	if (c > 0xff) {
		errno = EILSEQ;
		return -1;
	}
	
	*outbuf = (char) c;
	
	return 1;
}


/*
 * Simple conversion API
 */

static gpointer error_quark = "ConvertError";

gpointer
g_convert_error_quark (void)
{
	return error_quark;
}

gchar *
g_convert (const gchar *str, gssize len, const gchar *to_charset, const gchar *from_charset,
	   gsize *bytes_read, gsize *bytes_written, GError **err)
{
	gsize outsize, outused, outleft, inleft, grow, rc;
	char *result, *outbuf, *inbuf;
	gboolean flush = FALSE;
	gboolean done = FALSE;
	GIConv cd;
	
	g_return_val_if_fail (str != NULL, NULL);
	g_return_val_if_fail (to_charset != NULL, NULL);
	g_return_val_if_fail (from_charset != NULL, NULL);
	
	if ((cd = g_iconv_open (to_charset, from_charset)) == (GIConv) -1) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_NO_CONVERSION,
			     "Conversion from %s to %s not supported.",
			     from_charset, to_charset);
		
		if (bytes_written)
			*bytes_written = 0;
		
		if (bytes_read)
			*bytes_read = 0;
		
		return NULL;
	}
	
	inleft = len < 0 ? strlen (str) : len;
	inbuf = (char *) str;
	
	outleft = outsize = MAX (inleft, 8);
	outbuf = result = g_malloc (outsize + 4);
	
	do {
		if (!flush)
			rc = g_iconv (cd, &inbuf, &inleft, &outbuf, &outleft);
		else
			rc = g_iconv (cd, NULL, NULL, &outbuf, &outleft);
		
		if (rc == (gsize) -1) {
			switch (errno) {
			case E2BIG:
				/* grow our result buffer */
				grow = MAX (inleft, 8) << 1;
				outused = outbuf - result;
				outsize += grow;
				outleft += grow;
				result = g_realloc (result, outsize + 4);
				outbuf = result + outused;
				break;
			case EINVAL:
				/* incomplete input, stop converting and terminate here */
				if (flush)
					done = TRUE;
				else
					flush = TRUE;
				break;
			case EILSEQ:
				/* illegal sequence in the input */
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "%s", g_strerror (errno));
				
				if (bytes_read) {
					/* save offset of the illegal input sequence */
					*bytes_read = (inbuf - str);
				}
				
				if (bytes_written)
					*bytes_written = 0;
				
				g_iconv_close (cd);
				g_free (result);
				return NULL;
			default:
				/* unknown errno */
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_FAILED, "%s", g_strerror (errno));
				
				if (bytes_written)
					*bytes_written = 0;
				
				if (bytes_read)
					*bytes_read = 0;
				
				g_iconv_close (cd);
				g_free (result);
				return NULL;
			}
		} else if (flush) {
			/* input has been converted and output has been flushed */
			break;
		} else {
			/* input has been converted, need to flush the output */
			flush = TRUE;
		}
	} while (!done);
	
	g_iconv_close (cd);
	
	/* Note: not all charsets can be null-terminated with a single
           null byte. UCS2, for example, needs 2 null bytes and UCS4
           needs 4. I hope that 4 null bytes is enough to terminate all
           multibyte charsets? */
	
	/* null-terminate the result */
	memset (outbuf, 0, 4);
	
	if (bytes_written)
		*bytes_written = outbuf - result;
	
	if (bytes_read)
		*bytes_read = inbuf - str;
	
	return result;
}


/*
 * Unicode conversion
 */

/**
 * An explanation of the conversion can be found at:
 * http://home.tiscali.nl/t876506/utf8tbl.html
 *
 **/
gint
g_unichar_to_utf8 (gunichar c, gchar *outbuf)
{
	int base, n, i;
	
	if (c < 0x80) {
		base = 0;
		n = 1;
	} else if (c < 0x800) {
		base = 192;
		n = 2;
	} else if (c < 0x10000) {
		base = 224;
		n = 3;
	} else if (c < 0x200000) {
		base = 240;
		n = 4;
	} else if (c < 0x4000000) {
		base = 248;
		n = 5;
	} else if (c < 0x80000000) {
		base = 252;
		n = 6;
	} else {
		return -1;
	}
	
	if (outbuf != NULL) {
		for (i = n - 1; i > 0; i--) {
			/* mask off 6 bits worth and add 128 */
			outbuf[i] = (c & 0x3f) | 0x80;
			c >>= 6;
		}
		
		/* first character has a different base */
		outbuf[0] = c | base;
	}
	
	return n;
}

static FORCE_INLINE (int)
g_unichar_to_utf16 (gunichar c, gunichar2 *outbuf)
{
	gunichar c2;
	
	if (c < 0xd800) {
		if (outbuf)
			*outbuf = (gunichar2) c;
		
		return 1;
	} else if (c < 0xe000) {
		return -1;
	} else if (c < 0x10000) {
		if (outbuf)
			*outbuf = (gunichar2) c;
		
		return 1;
	} else if (c < 0x110000) {
		if (outbuf) {
			c2 = c - 0x10000;
			
			outbuf[0] = (gunichar2) ((c2 >> 10) + 0xd800);
			outbuf[1] = (gunichar2) ((c2 & 0x3ff) + 0xdc00);
		}
		
		return 2;
	} else {
		return -1;
	}
}

gunichar *
g_utf8_to_ucs4_fast (const gchar *str, glong len, glong *items_written)
{
	gunichar *outbuf, *outptr;
	char *inptr;
	glong n, i;
	
	g_return_val_if_fail (str != NULL, NULL);
	
	n = g_utf8_strlen (str, len);
	
	if (items_written)
		*items_written = n;
	
	outptr = outbuf = g_malloc ((n + 1) * sizeof (gunichar));
	inptr = (char *) str;
	
	for (i = 0; i < n; i++) {
		*outptr++ = g_utf8_get_char (inptr);
		inptr = g_utf8_next_char (inptr);
	}
	
	*outptr = 0;
	
	return outbuf;
}

static gunichar2 *
eg_utf8_to_utf16_general (const gchar *str, glong len, glong *items_read, glong *items_written, gboolean include_nuls, GError **err)
{
	gunichar2 *outbuf, *outptr;
	size_t outlen = 0;
	size_t inleft;
	char *inptr;
	gunichar c;
	int u, n;
	
	g_return_val_if_fail (str != NULL, NULL);
	
	if (len < 0) {
		if (include_nuls) {
			g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_FAILED, "Conversions with embedded nulls must pass the string length");
			return NULL;
		}
		
		len = strlen (str);
	}
	
	inptr = (char *) str;
	inleft = len;
	
	while (inleft > 0) {
		if ((n = decode_utf8 (inptr, inleft, &c)) < 0)
			goto error;
		
		if (c == 0 && !include_nuls)
			break;
		
		if ((u = g_unichar_to_utf16 (c, NULL)) < 0) {
			errno = EILSEQ;
			goto error;
		}
		
		outlen += u;
		inleft -= n;
		inptr += n;
	}
	
	if (items_read)
		*items_read = inptr - str;
	
	if (items_written)
		*items_written = outlen;
	
	outptr = outbuf = g_malloc ((outlen + 1) * sizeof (gunichar2));
	inptr = (char *) str;
	inleft = len;
	
	while (inleft > 0) {
		if ((n = decode_utf8 (inptr, inleft, &c)) < 0)
			break;
		
		if (c == 0 && !include_nuls)
			break;
		
		outptr += g_unichar_to_utf16 (c, outptr);
		inleft -= n;
		inptr += n;
	}
	
	*outptr = '\0';
	
	return outbuf;
	
 error:
	if (errno == EILSEQ) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
			     "Illegal byte sequence encounted in the input.");
	} else if (items_read) {
		/* partial input is ok if we can let our caller know... */
	} else {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_PARTIAL_INPUT,
			     "Partial byte sequence encountered in the input.");
	}
	
	if (items_read)
		*items_read = inptr - str;
	
	if (items_written)
		*items_written = 0;
	
	return NULL;
}

gunichar2 *
g_utf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, FALSE, err);
}

gunichar2 *
eg_utf8_to_utf16_with_nuls (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, TRUE, err);
}

gunichar *
g_utf8_to_ucs4 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	gunichar *outbuf, *outptr;
	size_t outlen = 0;
	size_t inleft;
	char *inptr;
	gunichar c;
	int n;
	
	g_return_val_if_fail (str != NULL, NULL);
	
	if (len < 0)
		len = strlen (str);
	
	inptr = (char *) str;
	inleft = len;
	
	while (inleft > 0) {
		if ((n = decode_utf8 (inptr, inleft, &c)) < 0) {
			if (errno == EILSEQ) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encounted in the input.");
			} else if (items_read) {
				/* partial input is ok if we can let our caller know... */
				break;
			} else {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_PARTIAL_INPUT,
					     "Partial byte sequence encountered in the input.");
			}
			
			if (items_read)
				*items_read = inptr - str;
			
			if (items_written)
				*items_written = 0;
			
			return NULL;
		} else if (c == 0)
			break;
		
		outlen += 4;
		inleft -= n;
		inptr += n;
	}
	
	if (items_written)
		*items_written = outlen / 4;
	
	if (items_read)
		*items_read = inptr - str;
	
	outptr = outbuf = g_malloc (outlen + 4);
	inptr = (char *) str;
	inleft = len;
	
	while (inleft > 0) {
		if ((n = decode_utf8 (inptr, inleft, &c)) < 0)
			break;
		else if (c == 0)
			break;
		
		*outptr++ = c;
		inleft -= n;
		inptr += n;
	}
	
	*outptr = 0;
	
	return outbuf;
}

gchar *
g_utf16_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	char *inptr, *outbuf, *outptr;
	size_t outlen = 0;
	size_t inleft;
	gunichar c;
	int n;
	
	g_return_val_if_fail (str != NULL, NULL);
	
	if (len < 0) {
		len = 0;
		while (str[len])
			len++;
	}
	
	inptr = (char *) str;
	inleft = len * 2;
	
	while (inleft > 0) {
		if ((n = decode_utf16 (inptr, inleft, &c)) < 0) {
			if (n == -2 && inleft > 2) {
				/* This means that the first UTF-16 char was read, but second failed */
				inleft -= 2;
				inptr += 2;
			}
			
			if (errno == EILSEQ) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encounted in the input.");
			} else if (items_read) {
				/* partial input is ok if we can let our caller know... */
				break;
			} else {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_PARTIAL_INPUT,
					     "Partial byte sequence encountered in the input.");
			}
			
			if (items_read)
				*items_read = (inptr - (char *) str) / 2;
			
			if (items_written)
				*items_written = 0;
			
			return NULL;
		} else if (c == 0)
			break;
		
		outlen += g_unichar_to_utf8 (c, NULL);
		inleft -= n;
		inptr += n;
	}
	
	if (items_read)
		*items_read = (inptr - (char *) str) / 2;
	
	if (items_written)
		*items_written = outlen;
	
	outptr = outbuf = g_malloc (outlen + 1);
	inptr = (char *) str;
	inleft = len * 2;
	
	while (inleft > 0) {
		if ((n = decode_utf16 (inptr, inleft, &c)) < 0)
			break;
		else if (c == 0)
			break;
		
		outptr += g_unichar_to_utf8 (c, outptr);
		inleft -= n;
		inptr += n;
	}
	
	*outptr = '\0';
	
	return outbuf;
}

gunichar *
g_utf16_to_ucs4 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	gunichar *outbuf, *outptr;
	size_t outlen = 0;
	size_t inleft;
	char *inptr;
	gunichar c;
	int n;
	
	g_return_val_if_fail (str != NULL, NULL);
	
	if (len < 0) {
		len = 0;
		while (str[len])
			len++;
	}
	
	inptr = (char *) str;
	inleft = len * 2;
	
	while (inleft > 0) {
		if ((n = decode_utf16 (inptr, inleft, &c)) < 0) {
			if (n == -2 && inleft > 2) {
				/* This means that the first UTF-16 char was read, but second failed */
				inleft -= 2;
				inptr += 2;
			}
			
			if (errno == EILSEQ) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encounted in the input.");
			} else if (items_read) {
				/* partial input is ok if we can let our caller know... */
				break;
			} else {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_PARTIAL_INPUT,
					     "Partial byte sequence encountered in the input.");
			}
			
			if (items_read)
				*items_read = (inptr - (char *) str) / 2;
			
			if (items_written)
				*items_written = 0;
			
			return NULL;
		} else if (c == 0)
			break;
		
		outlen += 4;
		inleft -= n;
		inptr += n;
	}
	
	if (items_read)
		*items_read = (inptr - (char *) str) / 2;
	
	if (items_written)
		*items_written = outlen / 4;
	
	outptr = outbuf = g_malloc (outlen + 4);
	inptr = (char *) str;
	inleft = len * 2;
	
	while (inleft > 0) {
		if ((n = decode_utf16 (inptr, inleft, &c)) < 0)
			break;
		else if (c == 0)
			break;
		
		*outptr++ = c;
		inleft -= n;
		inptr += n;
	}
	
	*outptr = 0;
	
	return outbuf;
}

gchar *
g_ucs4_to_utf8 (const gunichar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	char *outbuf, *outptr;
	size_t outlen = 0;
	glong i;
	int n;
	
	g_return_val_if_fail (str != NULL, NULL);
	
	if (len < 0) {
		for (i = 0; str[i] != 0; i++) {
			if ((n = g_unichar_to_utf8 (str[i], NULL)) < 0) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encounted in the input.");
				
				if (items_written)
					*items_written = 0;
				
				if (items_read)
					*items_read = i;
				
				return NULL;
			}
			
			outlen += n;
		}
	} else {
		for (i = 0; i < len && str[i] != 0; i++) {
			if ((n = g_unichar_to_utf8 (str[i], NULL)) < 0) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encounted in the input.");
				
				if (items_written)
					*items_written = 0;
				
				if (items_read)
					*items_read = i;
				
				return NULL;
			}
			
			outlen += n;
		}
	}
	
	len = i;
	
	outptr = outbuf = g_malloc (outlen + 1);
	for (i = 0; i < len; i++)
		outptr += g_unichar_to_utf8 (str[i], outptr);
	*outptr = 0;
	
	if (items_written)
		*items_written = outlen;
	
	if (items_read)
		*items_read = i;
	
	return outbuf;
}

gunichar2 *
g_ucs4_to_utf16 (const gunichar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	gunichar2 *outbuf, *outptr;
	size_t outlen = 0;
	glong i;
	int n;
	
	g_return_val_if_fail (str != NULL, NULL);
	
	if (len < 0) {
		for (i = 0; str[i] != 0; i++) {
			if ((n = g_unichar_to_utf16 (str[i], NULL)) < 0) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encounted in the input.");
				
				if (items_written)
					*items_written = 0;
				
				if (items_read)
					*items_read = i;
				
				return NULL;
			}
			
			outlen += n;
		}
	} else {
		for (i = 0; i < len && str[i] != 0; i++) {
			if ((n = g_unichar_to_utf16 (str[i], NULL)) < 0) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encounted in the input.");
				
				if (items_written)
					*items_written = 0;
				
				if (items_read)
					*items_read = i;
				
				return NULL;
			}
			
			outlen += n;
		}
	}
	
	len = i;
	
	outptr = outbuf = g_malloc ((outlen + 1) * sizeof (gunichar2));
	for (i = 0; i < len; i++)
		outptr += g_unichar_to_utf16 (str[i], outptr);
	*outptr = 0;
	
	if (items_written)
		*items_written = outlen;
	
	if (items_read)
		*items_read = i;
	
	return outbuf;
}
