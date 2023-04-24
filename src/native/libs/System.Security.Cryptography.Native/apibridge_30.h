// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Functions based on OpenSSL 3.0 API, used when building against/running with older versions.

#pragma once
#include "pal_types.h"

int local_EVP_PKEY_CTX_set_rsa_keygen_bits(EVP_PKEY_CTX* ctx, int bits);
int local_EVP_PKEY_CTX_set_rsa_oaep_md(EVP_PKEY_CTX* ctx, const EVP_MD* md);
int local_EVP_PKEY_CTX_set_rsa_padding(EVP_PKEY_CTX* ctx, int pad_mode);
int local_EVP_PKEY_CTX_set_rsa_pss_saltlen(EVP_PKEY_CTX* ctx, int saltlen);
int local_EVP_PKEY_CTX_set_signature_md(EVP_PKEY_CTX* ctx, const EVP_MD* md);
