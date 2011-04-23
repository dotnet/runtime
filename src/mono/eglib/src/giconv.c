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

typedef int (* Decoder) (char **inbytes, size_t *inbytesleft, gunichar *outchar);
typedef int (* Encoder) (gunichar c, char **outbytes, size_t *outbytesleft);

struct _GIConv {
	Decoder decode;
	Encoder encode;
	gunichar c;
#ifdef HAVE_ICONV
	iconv_t cd;
#endif
};

static int decode_utf32be (char **inbytes, size_t *inbytesleft, gunichar *outchar);
static int encode_utf32be (gunichar c, char **outbytes, size_t *outbytesleft);

static int decode_utf32le (char **inbytes, size_t *inbytesleft, gunichar *outchar);
static int encode_utf32le (gunichar c, char **outbytes, size_t *outbytesleft);

static int decode_utf16be (char **inbytes, size_t *inbytesleft, gunichar *outchar);
static int encode_utf16be (gunichar c, char **outbytes, size_t *outbytesleft);

static int decode_utf16le (char **inbytes, size_t *inbytesleft, gunichar *outchar);
static int encode_utf16le (gunichar c, char **outbytes, size_t *outbytesleft);

static int decode_utf8 (char **inbytes, size_t *inbytesleft, gunichar *outchar);
static int encode_utf8 (gunichar c, char **outbytes, size_t *outbytesleft);

static int decode_latin1 (char **inbytes, size_t *inbytesleft, gunichar *outchar);
static int encode_latin1 (gunichar c, char **outbytes, size_t *outbytesleft);

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
g_iconv (GIConv cd, char **inbytes, size_t *inbytesleft,
	 char **outbytes, size_t *outbytesleft)
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
		if (cd->decode (&inptr, &inleft, &c) == -1) {
			rc = -1;
			break;
		}
		
	encode:
		if (cd->encode (c, &outptr, &outleft) == -1) {
			rc = -1;
			break;
		}
		
		c = (gunichar) -1;
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
decode_utf32be (char **inbytes, size_t *inbytesleft, gunichar *outchar)
{
	gunichar *inptr = (gunichar *) *inbytes;
	size_t inleft = *inbytesleft;
	gunichar c;
	
	if (inleft < 4) {
		errno = EINVAL;
		return -1;
	}
	
	c = GUINT32_FROM_BE (*inptr);
	inleft -= 4;
	inptr++;
	
	if (c >= 2147483648UL) {
		errno = EILSEQ;
		return -1;
	}
	
	*inbytes = (char *) inptr;
	*inbytesleft = inleft;
	*outchar = c;
	
	return 0;
}

static int
decode_utf32le (char **inbytes, size_t *inbytesleft, gunichar *outchar)
{
	gunichar *inptr = (gunichar *) *inbytes;
	size_t inleft = *inbytesleft;
	gunichar c;
	
	if (inleft < 4) {
		errno = EINVAL;
		return -1;
	}
	
	c = GUINT32_FROM_LE (*inptr);
	inleft -= 4;
	inptr++;
	
	if (c >= 2147483648UL) {
		errno = EILSEQ;
		return -1;
	}
	
	*inbytes = (char *) inptr;
	*inbytesleft = inleft;
	*outchar = c;
	
	return 0;
}

static int
encode_utf32be (gunichar c, char **outbytes, size_t *outbytesleft)
{
	gunichar *outptr = (gunichar *) *outbytes;
	size_t outleft = *outbytesleft;
	
	if (outleft < 4) {
		errno = E2BIG;
		return -1;
	}
	
	*outptr++ = GUINT32_TO_BE (c);
	outleft -= 4;
	
	*outbytes = (char *) outptr;
	*outbytesleft = outleft;
	
	return 0;
}

static int
encode_utf32le (gunichar c, char **outbytes, size_t *outbytesleft)
{
	gunichar *outptr = (gunichar *) *outbytes;
	size_t outleft = *outbytesleft;
	
	if (outleft < 4) {
		errno = E2BIG;
		return -1;
	}
	
	*outptr++ = GUINT32_TO_LE (c);
	outleft -= 4;
	
	*outbytes = (char *) outptr;
	*outbytesleft = outleft;
	
	return 0;
}

static int
decode_utf16be (char **inbytes, size_t *inbytesleft, gunichar *outchar)
{
	gunichar2 *inptr = (gunichar2 *) *inbytes;
	size_t inleft = *inbytesleft;
	gunichar2 c;
	gunichar u;
	
	if (inleft < 2) {
		errno = EINVAL;
		return -1;
	}
	
	u = GUINT16_FROM_BE (*inptr);
	inleft -= 2;
	inptr++;
	
	if (u < 0xd800) {
		/* 0x0000 -> 0xd7ff */
	} else if (u < 0xdc00) {
		/* 0xd800 -> 0xdbff */
		if (inleft < 2) {
			errno = EINVAL;
			return -1;
		}
		
		c = GUINT16_FROM_BE (*inptr);
		inleft -= 2;
		inptr++;
		
		if (c < 0xdc00 || c > 0xdfff) {
			errno = EILSEQ;
			return -1;
		}
		
		u = ((u - 0xd800) << 10) + (c - 0xdc00) + 0x0010000UL;
	} else if (u < 0xe000) {
		/* 0xdc00 -> 0xdfff */
		errno = EILSEQ;
		return -1;
	} else {
		/* 0xe000 -> 0xffff */
	}
	
	*inbytes = (char *) inptr;
	*inbytesleft = inleft;
	*outchar = u;
	
	return 0;
}

static int
decode_utf16le (char **inbytes, size_t *inbytesleft, gunichar *outchar)
{
	gunichar2 *inptr = (gunichar2 *) *inbytes;
	size_t inleft = *inbytesleft;
	gunichar2 c;
	gunichar u;
	
	if (inleft < 2) {
		errno = EINVAL;
		return -1;
	}
	
	u = GUINT16_FROM_LE (*inptr);
	inleft -= 2;
	inptr++;
	
	if (u < 0xd800) {
		/* 0x0000 -> 0xd7ff */
	} else if (u < 0xdc00) {
		/* 0xd800 -> 0xdbff */
		if (inleft < 2) {
			errno = EINVAL;
			return -1;
		}
		
		c = GUINT16_FROM_LE (*inptr);
		inleft -= 2;
		inptr++;
		
		if (c < 0xdc00 || c > 0xdfff) {
			errno = EILSEQ;
			return -1;
		}
		
		u = ((u - 0xd800) << 10) + (c - 0xdc00) + 0x0010000UL;
	} else if (u < 0xe000) {
		/* 0xdc00 -> 0xdfff */
		errno = EILSEQ;
		return -1;
	} else {
		/* 0xe000 -> 0xffff */
	}
	
	*inbytes = (char *) inptr;
	*inbytesleft = inleft;
	*outchar = u;
	
	return 0;
}

static int
encode_utf16be (gunichar c, char **outbytes, size_t *outbytesleft)
{
	gunichar2 *outptr = (gunichar2 *) *outbytes;
	size_t outleft = *outbytesleft;
	gunichar2 ch;
	gunichar c2;
	
	if (outleft < 2) {
		errno = E2BIG;
		return -1;
	}
	
	if (c <= 0xffff && (c < 0xd800 || c > 0xdfff)) {
		ch = (gunichar2) c;
		
		*outptr++ = GUINT16_TO_BE (ch);
		outleft -= 2;
	} else if (outleft < 4) {
		errno = E2BIG;
		return -1;
	} else {
		c2 = c - 0x10000;
		
		ch = (gunichar2) ((c2 >> 10) + 0xd800);
		*outptr++ = GUINT16_TO_BE (ch);
		
		ch = (gunichar2) ((c2 & 0x3ff) + 0xdc00);
		*outptr++ = GUINT16_TO_BE (ch);
		
		outleft -= 4;
	}
	
	*outbytes = (char *) outptr;
	*outbytesleft = outleft;
	
	return 0;
}

static int
encode_utf16le (gunichar c, char **outbytes, size_t *outbytesleft)
{
	gunichar2 *outptr = (gunichar2 *) *outbytes;
	size_t outleft = *outbytesleft;
	gunichar2 ch;
	gunichar c2;
	
	if (outleft < 2) {
		errno = E2BIG;
		return -1;
	}
	
	if (c <= 0xffff && (c < 0xd800 || c > 0xdfff)) {
		ch = (gunichar2) c;
		
		*outptr++ = GUINT16_TO_LE (ch);
		outleft -= 2;
	} else if (outleft < 4) {
		errno = E2BIG;
		return -1;
	} else {
		c2 = c - 0x10000;
		
		ch = (gunichar2) ((c2 >> 10) + 0xd800);
		*outptr++ = GUINT16_TO_LE (ch);
		
		ch = (gunichar2) ((c2 & 0x3ff) + 0xdc00);
		*outptr++ = GUINT16_TO_LE (ch);
		
		outleft -= 4;
	}
	
	*outbytes = (char *) outptr;
	*outbytesleft = outleft;
	
	return 0;
}

static int
decode_utf8 (char **inbytes, size_t *inbytesleft, gunichar *outchar)
{
	unsigned char *inptr = (unsigned char *) *inbytes;
	size_t inleft = *inbytesleft;
	size_t i, len = 0;
	unsigned char c;
	gunichar u;
	
	c = *inptr++;
	
	if (c < 0x80) {
		/* simple ascii case */
		len = 1;
	} else if (c < 0xe0) {
		c &= 0x1f;
		len = 2;
	} else if (c < 0xf0) {
		c &= 0x0f;
		len = 3;
	} else if (c < 0xf8) {
		c &= 0x07;
		len = 4;
	} else if (c < 0xfc) {
		c &= 0x03;
		len = 5;
	} else if (c < 0xfe) {
		c &= 0x01;
		len = 6;
	} else {
		errno = EILSEQ;
		return -1;
	}
	
	if (len > inleft) {
		errno = EINVAL;
		return -1;
	}
	
	u = c;
	for (i = 1; i < len; i++) {
		u = (u << 6) | ((*inptr) & 0x3f);
		inptr++;
	}
	
	*inbytesleft = inleft - len;
	*inbytes = (char *) inptr;
	*outchar = u;
	
	return 0;
}

static int
encode_utf8 (gunichar c, char **outbytes, size_t *outbytesleft)
{
	size_t outleft = *outbytesleft;
	char *outptr = *outbytes;
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
		errno = EINVAL;
		return -1;
	}
	
	if (outleft < len) {
		errno = E2BIG;
		return -1;
	}
	
	for (i = len - 1; i > 0; i--) {
		/* mask off 6 bits worth and add 128 */
		outptr[i] = 128 + (c & 0x3f);
		c >>= 6;
	}
	
	/* first character has a different base */
	outptr[0] = base + c;
	
	*outbytesleft = outleft - len;
	*outbytes = outptr + len;
	
	return 0;
}

static int
decode_latin1 (char **inbytes, size_t *inbytesleft, gunichar *outchar)
{
	size_t inleft = *inbytesleft;
	char *inptr = *inbytes;
	gunichar u;
	
	u = (unsigned char) *inptr;
	*inbytesleft = inleft - 1;
	*inbytes = inptr + 1;
	*outchar = u;
	
	return 0;
}

static int
encode_latin1 (gunichar c, char **outbytes, size_t *outbytesleft)
{
	size_t outleft = *outbytesleft;
	char *outptr = *outbytes;
	
	if (outleft < 1) {
		errno = E2BIG;
		return -1;
	}
	
	if (c > 0xff) {
		errno = EILSEQ;
		return -1;
	}
	
	*outptr++ = (char) c;
	outleft--;
	
	*outbytesleft = outleft;
	*outbytes = outptr;
	
	return 0;
}
