//
//  btls-key.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/7/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_key__
#define __btls__btls_key__

#include <stdio.h>
#include "btls-ssl.h"
#include "btls-x509.h"

MONO_API EVP_PKEY *
mono_btls_key_new ();

MONO_API void
mono_btls_key_free (EVP_PKEY *pkey);

MONO_API EVP_PKEY *
mono_btls_key_up_ref (EVP_PKEY *pkey);

MONO_API int
mono_btls_key_get_bits (EVP_PKEY *pkey);

MONO_API int
mono_btls_key_is_rsa (EVP_PKEY *pkey);

MONO_API int
mono_btls_key_assign_rsa_private_key (EVP_PKEY *pkey, uint8_t *der_data, int der_length);

MONO_API int
mono_btls_key_get_bytes (EVP_PKEY *pkey, uint8_t **buffer, int *size, int include_private_bits);

#endif /* __btls__btls_key__ */

