// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"
#include "pal_atomic.h"

typedef enum
{
    Pkcs1 = 0,
    OaepSHA1 = 1,
    NoPadding = 2,
} RsaPadding;

typedef struct RSA
{
    jobject privateKey; // RSAPrivateCrtKey
    jobject publicKey;  // RSAPublicCrtKey
    atomic_int refCount;
    int32_t keyWidthInBits;
} RSA;

#define CIPHER_ENCRYPT_MODE 1
#define CIPHER_DECRYPT_MODE 2

PALEXPORT RSA* AndroidCryptoNative_RsaCreate(void);
PALEXPORT int32_t AndroidCryptoNative_RsaUpRef(RSA* rsa);
PALEXPORT void AndroidCryptoNative_RsaDestroy(RSA* rsa);
PALEXPORT RSA* AndroidCryptoNative_DecodeRsaSubjectPublicKeyInfo(uint8_t* buf, int32_t len);
PALEXPORT int32_t AndroidCryptoNative_RsaPublicEncrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding);
PALEXPORT int32_t AndroidCryptoNative_RsaPrivateDecrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding);
PALEXPORT int32_t AndroidCryptoNative_RsaSignPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa);
PALEXPORT int32_t AndroidCryptoNative_RsaVerificationPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa);
PALEXPORT int32_t AndroidCryptoNative_RsaSize(RSA* rsa);
PALEXPORT int32_t AndroidCryptoNative_RsaGenerateKeyEx(RSA* rsa, int32_t bits);
PALEXPORT int32_t AndroidCryptoNative_GetRsaParameters(RSA* rsa,
    jobject* n, jobject* e, jobject* d, jobject* p, jobject* dmp1, jobject* q, jobject* dmq1, jobject* iqmp);
PALEXPORT int32_t AndroidCryptoNative_SetRsaParameters(RSA* rsa,
    uint8_t* n,    int32_t nLength,    uint8_t* e,    int32_t eLength,    uint8_t* d, int32_t dLength,
    uint8_t* p,    int32_t pLength,    uint8_t* dmp1, int32_t dmp1Length, uint8_t* q, int32_t qLength,
    uint8_t* dmq1, int32_t dmq1Length, uint8_t* iqmp, int32_t iqmpLength);

RSA* AndroidCryptoNative_NewRsaFromKeys(JNIEnv* env, jobject /*RSAPublicKey*/ publicKey, jobject /*RSAPrivateKey*/ privateKey) ARGS_NON_NULL(1, 2);
RSA* AndroidCryptoNative_NewRsaFromPublicKey(JNIEnv* env, jobject /*RSAPublicKey*/ key) ARGS_NON_NULL(1, 2);
