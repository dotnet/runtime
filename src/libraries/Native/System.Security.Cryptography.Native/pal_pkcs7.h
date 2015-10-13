// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/pkcs7.h>

/*
Shims the d2i_PKCS7 method and makes it easier to invoke from managed code.
*/
extern "C" PKCS7* DecodePkcs7(const unsigned char* buf, int32_t len);

