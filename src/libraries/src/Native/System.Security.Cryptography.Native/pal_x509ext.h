// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/x509v3.h>

/*
Shims the d2i_BASIC_CONSTRAINTS method and makes it easier to invoke from managed code.
*/
extern "C" BASIC_CONSTRAINTS* DecodeBasicConstraints(const unsigned char* buf, int32_t len);

/*
Shims the d2i_EXTENDED_KEY_USAGE method and makes it easier to invoke from managed code.
*/
extern "C" EXTENDED_KEY_USAGE* DecodeExtendedKeyUsage(const unsigned char* buf, int32_t len);
