// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

PALEXPORT jobject AndroidCryptoNative_BigNumFromBinary(uint8_t* bytes, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_BigNumToBinary(jobject bignum, uint8_t* output);
PALEXPORT int32_t AndroidCryptoNative_GetBigNumBytes(jobject bignum);

int32_t AndroidCryptoNative_GetBigNumBytesIncludingPaddingByteForSign(jobject bignum);
