//
//  btls-util.c
//  MonoBtls
//
//  Created by Martin Baulig on 3/23/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include <btls-util.h>
#include <assert.h>
// #include <time.h>

extern int asn1_generalizedtime_to_tm (struct tm *tm, const ASN1_GENERALIZEDTIME *d);

extern int64_t btls_timegm64 (const struct tm *date);


MONO_API void
mono_btls_free (void *data)
{
	OPENSSL_free (data);
}

int64_t
mono_btls_util_asn1_time_to_ticks (ASN1_TIME *time)
{
	ASN1_GENERALIZEDTIME *gtime;
	struct tm tm;
	int64_t epoch;
	int ret;
	
	memset (&tm, 0, sizeof (tm));

	gtime = ASN1_TIME_to_generalizedtime (time, NULL);
	ret = asn1_generalizedtime_to_tm (&tm, gtime);
	ASN1_GENERALIZEDTIME_free (gtime);

	/* FIXME: check the return value in managed code */
	if (ret == 0) {
		return 0;
	}

	epoch = btls_timegm64 (&tm);

	return epoch;
}

// Copied from crypto/bio/printf.c, takes va_list
int
mono_btls_debug_printf (BIO *bio, const char *format, va_list args)
{
	char buf[256], *out, out_malloced = 0;
	int out_len, ret;

	out_len = vsnprintf (buf, sizeof(buf), format, args);
	if (out_len < 0) {
		return -1;
	}

	if ((size_t) out_len >= sizeof(buf)) {
		const int requested_len = out_len;
		/* The output was truncated. Note that vsnprintf's return value
		 * does not include a trailing NUL, but the buffer must be sized
		 * for it. */
		out = OPENSSL_malloc (requested_len + 1);
		out_malloced = 1;
		if (out == NULL) {
			OPENSSL_PUT_ERROR(BIO, ERR_R_MALLOC_FAILURE);
			return -1;
		}
		out_len = vsnprintf (out, requested_len + 1, format, args);
		assert(out_len == requested_len);
	} else {
		out = buf;
	}

	ret = BIO_write(bio, out, out_len);
	if (out_malloced) {
		OPENSSL_free(out);
	}

	return ret;
}
