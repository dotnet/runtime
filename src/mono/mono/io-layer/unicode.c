#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <iconv.h>
#include <errno.h>

#include "mono/io-layer/wapi.h"
#include "unicode.h"

static pthread_key_t unicode_key;
static pthread_once_t unicode_key_once=PTHREAD_ONCE_INIT;

static void unicode_end(void *buf)
{
	iconv_t cd=(iconv_t)buf;
	
	iconv_close(cd);
}

static void unicode_init(void)
{
	pthread_key_create(&unicode_key, unicode_end);
}

static iconv_t unicode_reset(void)
{
	iconv_t cd;

	pthread_once(&unicode_key_once, unicode_init);

	cd=pthread_getspecific(unicode_key);
	if(cd==NULL) {
		cd=iconv_open("UTF-8", "UNICODE");
		if(cd==(iconv_t)-1) {
			g_message(G_GNUC_PRETTY_FUNCTION ": Can't open iconv descriptor from UTF-8 to UNICODE");
			return(cd);
		}
		pthread_setspecific(unicode_key, cd);
	}

	if(cd==(iconv_t)-1) {
		return(cd);
	}
	
	iconv(cd, NULL, NULL, NULL, NULL);

	return(cd);
}

/* This is a nasty kludge */
static guint32 unicode_len(const guchar *str)
{
	guint32 len=0;
	
	do {
		if(str[len]=='\0' && str[len+1]=='\0') {
			return(len);
		}

		len+=2;
	} while(1);
}

/* Cut&pasted from glib (switch to g_convert() when glib-2 is out */
guchar *_wapi_unicode_to_utf8(const guchar *uni)
{
	guchar *str;
	guchar *dest;
	guchar *outp;
	guchar *p;
	guint inbytes_remaining;
	guint outbytes_remaining;
	size_t err;
	guint outbuf_size;
	gint i, len;
	gboolean have_error = FALSE;
	iconv_t converter;
  
	converter=unicode_reset();

	g_return_val_if_fail(uni != NULL, NULL);
	g_return_val_if_fail(converter != (iconv_t) -1, NULL);
     
	len = unicode_len(uni);
     

	str = g_memdup(uni, (guint32)len+2); /* don't forget the double NULL */

	/* fixme: ugly - iconv expects big endian encoding 
	   UTF-16le does not work on older libc */
	if (G_BYTE_ORDER == G_LITTLE_ENDIAN) {
		for (i = 0; i < len; i += 2) {
			str [i] = uni [i + 1];
			str [i + 1] = uni [i];
		}
	}

	p = str;
	inbytes_remaining = len;
	outbuf_size = len + 1; /* + 1 for nul in case len == 1 */
  
	outbytes_remaining = outbuf_size - 1; /* -1 for nul */
	outp = dest = g_malloc(outbuf_size);

again:
	err = iconv(converter, (char **)&p, &inbytes_remaining, (char **)&outp, &outbytes_remaining);

	if(err == (size_t)-1) {
		switch(errno) {
		case EINVAL:
			/* Incomplete text, do not report an error */
			break;
		case E2BIG: {
			size_t used = outp - dest;
			
			outbuf_size *= 2;
			dest = g_realloc(dest, outbuf_size);
		
			outp = dest + used;
			outbytes_remaining = outbuf_size - used - 1; /* -1 for nul */

			goto again;
		}
		case EILSEQ:
			have_error = TRUE;
			break;
		default:
			have_error = TRUE;
			break;
		}
	}

	*outp = '\0';
  
	if((p - str) != len) {
		if(!have_error) {
			have_error = TRUE;
		}
	}

	g_free(str);

	if(have_error) {
		g_free (dest);
		return NULL;
	} else {
		return dest;
	}
}
