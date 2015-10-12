// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdint.h>
#include <openssl/x509.h>

/*
Function:
GetX509EvpPublicKey

Returns a EVP_PKEY* equivalent to the public key of the certificate.
*/
extern "C" EVP_PKEY* GetX509EvpPublicKey(X509* x509);
