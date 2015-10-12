// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/x509.h>

/*
Function:
GetX509EvpPublicKey

Returns a EVP_PKEY* equivalent to the public key of the certificate.
*/
extern "C" EVP_PKEY* GetX509EvpPublicKey(X509* x509);

/*
Shims the d2i_X509_CRL method and makes it easier to invoke from managed code.
*/
extern "C" X509_CRL* DecodeX509Crl(const unsigned char* buf, int32_t len);

/*
Shims the d2i_X509 method and makes it easier to invoke from managed code.
*/
extern "C" X509* DecodeX509(const unsigned char* buf, int32_t len);
