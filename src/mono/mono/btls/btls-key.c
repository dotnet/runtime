//
//  btls-key.c
//  MonoBtls
//
//  Created by Martin Baulig on 3/7/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include <btls-key.h>

MONO_API void
mono_btls_key_free (EVP_PKEY *pkey)
{
	EVP_PKEY_free (pkey);
}

MONO_API EVP_PKEY *
mono_btls_key_up_ref (EVP_PKEY *pkey)
{
	return EVP_PKEY_up_ref (pkey);
}

MONO_API int
mono_btls_key_get_bits (EVP_PKEY *pkey)
{
	return EVP_PKEY_bits (pkey);
}

MONO_API int
mono_btls_key_is_rsa (EVP_PKEY *pkey)
{
	return pkey->type == EVP_PKEY_RSA;
}

MONO_API int
mono_btls_key_get_bytes (EVP_PKEY *pkey, uint8_t **buffer, int *size, int include_private_bits)
{
	size_t len;
	RSA *rsa;
	int ret;

	*size = 0;
	*buffer = NULL;

	if (pkey->type != EVP_PKEY_RSA)
		return 0;

	rsa = EVP_PKEY_get1_RSA (pkey);
	if (!rsa)
		return 0;

	if (include_private_bits)
		ret = RSA_private_key_to_bytes (buffer, &len, rsa);
	else
		ret = RSA_public_key_to_bytes (buffer, &len, rsa);

	RSA_free (rsa);

	if (ret != 1)
		return 0;

	*size = (int)len;
	return 1;
}
