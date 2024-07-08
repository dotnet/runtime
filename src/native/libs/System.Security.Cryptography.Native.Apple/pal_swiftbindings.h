// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

EXTERN_C void* AppleCryptoNative_ChaCha20Poly1305Encrypt;
EXTERN_C void* AppleCryptoNative_ChaCha20Poly1305Decrypt;
EXTERN_C void* AppleCryptoNative_AesGcmEncrypt;
EXTERN_C void* AppleCryptoNative_AesGcmDecrypt;
EXTERN_C void* AppleCryptoNative_IsAuthenticationFailure;
