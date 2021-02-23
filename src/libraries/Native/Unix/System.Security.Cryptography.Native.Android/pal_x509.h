// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"

// Creation and lifetime
PALEXPORT jobject /*X509Certificate*/ AndroidCryptoNative_X509Decode(const uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509Encode(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);

PALEXPORT int32_t AndroidCryptoNative_X509DecodeCollection(const uint8_t *buf, int32_t bufLen, jobject /*X509Certificate*/ *out, int32_t outLen);

// Matches managed X509ContentType enum
enum
{
    PAL_X509Unknown = 0,
    PAL_Certificate = 1,
    PAL_SerializedCert = 2,
    PAL_Pkcs12 = 3,
    PAL_SerializedStore = 4,
    PAL_Pkcs7 = 5,
    PAL_Authenticode = 6,
};
typedef uint32_t PAL_X509ContentType;
PALEXPORT PAL_X509ContentType AndroidCryptoNative_X509GetContentType(const uint8_t *buf, int32_t len);
