// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"

// Creation and lifetime
PALEXPORT jobject /*X509Certificate*/ AndroidCryptoNative_DecodeX509(const uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_EncodeX509(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);

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

// Keep in sync with managed definition in Interop.X509
struct X509BasicInformation
{
    int32_t Version;
    int64_t NotAfter;
    int64_t NotBefore;
};

// Basic properties
PALEXPORT bool AndroidCryptoNative_X509GetBasicInformation(jobject /*X509Certificate*/ cert, struct X509BasicInformation *info);
PALEXPORT int32_t AndroidCryptoNative_X509GetPublicKeyAlgorithm(jobject /*X509Certificate*/ cert, char *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetPublicKeyBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetPublicKeyParameterBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetSerialNumber(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetSignatureAlgorithm(jobject /*X509Certificate*/ cert, char *buf, int32_t len);

// Name
PALEXPORT int32_t AndroidCryptoNative_X509GetIssuerNameBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetSubjectNameBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);

// Extensions
typedef void (*EnumX509ExtensionsCallback)(const char *oid, int32_t oid_len, const uint8_t *data, int32_t data_len, bool isCritical, void *context);
PALEXPORT int32_t AndroidCryptoNative_X509EnumExtensions(jobject /*X509Certificate*/ cert, EnumX509ExtensionsCallback cb, void *context);
PALEXPORT int32_t AndroidCryptoNative_X509FindExtensionData(jobject /*X509Certificate*/ cert, const char *oid, uint8_t *buf, int32_t len);
