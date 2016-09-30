//
//  btls-util.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/23/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_util__
#define __btls__btls_util__

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <openssl/ssl.h>

void
mono_btls_free (void *data);

long
mono_btls_util_asn1_time_to_ticks (ASN1_TIME *time);

int
mono_btls_debug_printf (BIO *bio, const char *format, va_list args);

OPENSSL_EXPORT void CRYPTO_refcount_inc(CRYPTO_refcount_t *count);
OPENSSL_EXPORT int CRYPTO_refcount_dec_and_test_zero(CRYPTO_refcount_t *count);

#endif /* __btls__btls_util__ */
