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

EXTERN_C void* AppleCryptoNative_HKDFDeriveKey;
EXTERN_C void* AppleCryptoNative_HKDFExpand;
EXTERN_C void* AppleCryptoNative_HKDFExtract;

EXTERN_C void* AppleCryptoNative_DigestFree;
EXTERN_C void* AppleCryptoNative_DigestCreate;
EXTERN_C void* AppleCryptoNative_DigestUpdate;
EXTERN_C void* AppleCryptoNative_DigestFinal;
EXTERN_C void* AppleCryptoNative_DigestCurrent;
EXTERN_C void* AppleCryptoNative_DigestOneShot;
EXTERN_C void* AppleCryptoNative_DigestReset;
EXTERN_C void* AppleCryptoNative_DigestClone;
