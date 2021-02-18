// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"

// Creation and lifetime
PALEXPORT jobject /*X509Certificate*/ AndroidCryptoNative_DecodeX509(const uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_EncodeX509(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);

PALEXPORT void CryptoNative_X509Destroy(jobject /*X509Certificate*/ cert);
PALEXPORT jobject /*X509Certificate*/ CryptoNative_X509UpRef(jobject /*X509Certificate*/ cert);

// Basic properties
PALEXPORT int64_t AndroidCryptoNative_X509GetNotAfter(jobject /*X509Certificate*/ cert);
PALEXPORT int64_t AndroidCryptoNative_X509GetNotBefore(jobject /*X509Certificate*/ cert);
PALEXPORT int32_t AndroidCryptoNative_X509GetPublicKeyAlgorithm(jobject /*X509Certificate*/ cert, char *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetPublicKeyBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetPublicKeyParameterBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetSerialNumber(jobject /*X509Certificate*/ cert, uint8_t* buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetSignatureAlgorithm(jobject /*X509Certificate*/ cert, char *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetThumbprint(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetVersion(jobject /*X509Certificate*/ cert);

// Name
PALEXPORT int32_t AndroidCryptoNative_X509GetIssuerNameBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_X509GetSubjectNameBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT uint64_t AndroidCryptoNative_X509IssuerNameHash(jobject /*X509Certificate*/ cert);

// Extensions
typedef void (*EnumX509ExtensionsCallback)(const char *oid, int32_t oid_len, const uint8_t *data, int32_t data_len, bool isCritical, void *context);
PALEXPORT int32_t AndroidCryptoNative_X509EnumExtensions(jobject /*X509Certificate*/ cert, EnumX509ExtensionsCallback cb, void *context);
PALEXPORT int32_t AndroidCryptoNative_X509FindExtensionData(jobject /*X509Certificate*/ cert, const char *oid, uint8_t *buf, int32_t len);

// X509 CRL
PALEXPORT jobject /*X509CRL*/ AndroidCryptoNative_DecodeX509Crl(const uint8_t* buf, int32_t len);
PALEXPORT void AndroidCryptoNative_X509CrlDestroy(jobject /*X509CRL*/ crl);
