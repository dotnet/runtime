// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <CommonCrypto/CommonCrypto.h>
#include <CommonCrypto/CommonHMAC.h>

enum
{
    PAL_Unknown = 0,
    PAL_MD5,
    PAL_SHA1,
    PAL_SHA256,
    PAL_SHA384,
    PAL_SHA512,
};
typedef uint32_t PAL_HashAlgorithm;

typedef struct digest_ctx_st DigestCtx;
