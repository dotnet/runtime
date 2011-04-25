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

static int decode_utf8 (char *inbuf, size_t inleft, gunichar *outchar);
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
	size_t inleft, outleft;
	char *inptr, *outptr;
	gsize rc = 0;
	gunichar c;
	
#ifdef HAVE_ICONV
	if (cd->cd != (iconv_t) -1)
		return iconv (cd->cd, inbytes, inbytesleft, outbytes, outbytesleft);
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
		if ((rc = cd->decode (inptr, inleft, &c)) == -1)
			break;
		
		inleft -= rc;
		inptr += rc;
		
	encode:
		if ((rc = cd->encode (c, outptr, outleft)) == -1)
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
	
	return rc;
}

/*
 * Unicode encoders and decoders
 */

static int
decode_utf32be (char *inbuf, size_t inleft, gunichar *outchar)
{
	gunichar *inptr = (gunichar *) inbuf;
	gunichar c;
	
	if (inleft < 4) {
		errno = EINVAL;
		return -1;
	}
	
	c = GUINT32_FROM_BE (*inptr);
	
	if (c >= 2147483648UL) {
		errno = EILSEQ;
		return -1;
	}
	
	*outchar = c;
	
	return 4;
}

static int
decode_utf32le (char *inbuf, size_t inleft, gunichar *outchar)
{
	gunichar *inptr = (gunichar *) inbuf;
	gunichar c;
	
	if (inleft < 4) {
		errno = EINVAL;
		return -1;
	}
	
	c = GUINT32_FROM_LE (*inptr);
	
	if (c >= 2147483648UL) {
		errno = EILSEQ;
		return -1;
	}
	
	*outchar = c;
	
	return 4;
}

static int
encode_utf32be (gunichar c, char *outbuf, size_t outleft)
{
	gunichar *outptr = (gunichar *) outbuf;
	
	if (outleft < 4) {
		errno = E2BIG;
		return -1;
	}
	
	*outptr = GUINT32_TO_BE (c);
	
	return 4;
}

static int
encode_utf32le (gunichar c, char *outbuf, size_t outleft)
{
	gunichar *outptr = (gunichar *) outbuf;
	
	if (outleft < 4) {
		errno = E2BIG;
		return -1;
	}
	
	*outptr = GUINT32_TO_LE (c);
	
	return 4;
}

static int
decode_utf16be (char *inbuf, size_t inleft, gunichar *outchar)
{
	gunichar2 *inptr = (gunichar2 *) inbuf;
	gunichar2 c;
	gunichar u;
	
	if (inleft < 2) {
		errno = EINVAL;
		return -1;
	}
	
	u = GUINT16_FROM_BE (*inptr);
	
	if (u < 0xd800) {
		/* 0x0000 -> 0xd7ff */
		*outchar = u;
		return 2;
	} else if (u < 0xdc00) {
		/* 0xd800 -> 0xdbff */
		if (inleft < 4) {
			errno = EINVAL;
			return -1;
		}
		
		c = GUINT16_FROM_BE (inptr[1]);
		
		if (c < 0xdc00 || c > 0xdfff) {
			errno = EILSEQ;
			return -1;
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
	gunichar2 *inptr = (gunichar2 *) inbuf;
	gunichar2 c;
	gunichar u;
	
	if (inleft < 2) {
		errno = EINVAL;
		return -1;
	}
	
	u = GUINT16_FROM_LE (*inptr);
	
	if (u < 0xd800) {
		/* 0x0000 -> 0xd7ff */
		*outchar = u;
		return 2;
	} else if (u < 0xdc00) {
		/* 0xd800 -> 0xdbff */
		if (inleft < 4) {
			errno = EINVAL;
			return -1;
		}
		
		c = GUINT16_FROM_LE (inptr[1]);
		
		if (c < 0xdc00 || c > 0xdfff) {
			errno = EILSEQ;
			return -1;
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
	gunichar2 *outptr = (gunichar2 *) outbuf;
	gunichar2 ch;
	gunichar c2;
	
	if (outleft < 2) {
		errno = E2BIG;
		return -1;
	}
	
	if (c <= 0xffff && (c < 0xd800 || c > 0xdfff)) {
		ch = (gunichar2) c;
		
		*outptr = GUINT16_TO_BE (ch);
		
		return 2;
	} else if (outleft < 4) {
		errno = E2BIG;
		return -1;
	} else {
		c2 = c - 0x10000;
		
		ch = (gunichar2) ((c2 >> 10) + 0xd800);
		outptr[0] = GUINT16_TO_BE (ch);
		
		ch = (gunichar2) ((c2 & 0x3ff) + 0xdc00);
		outptr[1] = GUINT16_TO_BE (ch);
		
		return 4;
	}
}

static int
encode_utf16le (gunichar c, char *outbuf, size_t outleft)
{
	gunichar2 *outptr = (gunichar2 *) outbuf;
	gunichar2 ch;
	gunichar c2;
	
	if (outleft < 2) {
		errno = E2BIG;
		return -1;
	}
	
	if (c <= 0xffff && (c < 0xd800 || c > 0xdfff)) {
		ch = (gunichar2) c;
		
		*outptr++ = GUINT16_TO_LE (ch);
		
		return 2;
	} else if (outleft < 4) {
		errno = E2BIG;
		return -1;
	} else {
		c2 = c - 0x10000;
		
		ch = (gunichar2) ((c2 >> 10) + 0xd800);
		outptr[0] = GUINT16_TO_LE (ch);
		
		ch = (gunichar2) ((c2 & 0x3ff) + 0xdc00);
		outptr[1] = GUINT16_TO_LE (ch);
		
		return 4;
	}
}

static int
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
	
	if (c < 128UL) {
		base = 0;
		n = 1;
	} else if (c < 2048UL) {
		base = 192;
		n = 2;
	} else if (c < 65536UL) {
		base = 224;
		n = 3;
	} else if (c < 2097152UL) {
		base = 240;
		n = 4;
	} else if (c < 67108864UL) {
		base = 248;
		n = 5;
	} else if (c < 2147483648UL) {
		base = 252;
		n = 6;
	} else {
		errno = EINVAL;
		return -1;
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
