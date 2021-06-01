// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

PALEXPORT int32_t AndroidCryptoNative_BigNumToBinary(jobject bignum, uint8_t* output);
PALEXPORT int32_t AndroidCryptoNative_GetBigNumBytes(jobject bignum);

/*
Create a BigInteger from its binary representation.

The returned jobject will be a local reference.
*/
jobject AndroidCryptoNative_BigNumFromBinary(uint8_t* bytes, int32_t len);
int32_t AndroidCryptoNative_GetBigNumBytesIncludingPaddingByteForSign(jobject bignum);
