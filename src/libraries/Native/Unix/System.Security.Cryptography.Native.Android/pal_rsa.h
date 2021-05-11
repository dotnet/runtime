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

typedef struct
{
    jobject* n;
    jobject* e;
    jobject* d;
    jobject* p;
    jobject* dmp1;
    jobject* q;
    jobject* dmq1;
    jobject* iqmp;
} AndroidGetRsaParametersData;

typedef struct
{
    uint8_t* n;
    uint8_t* e;
    uint8_t* d;
    uint8_t* p;
    uint8_t* dmp1;
    uint8_t* q;
    uint8_t* dmq1;
    uint8_t* iqmp;
    int32_t  n_length;
    int32_t  e_length;
    int32_t  d_length;
    int32_t  p_length;
    int32_t  dmp1_length;
    int32_t  q_length;
    int32_t  dmq1_length;
    int32_t  iqmp_length;
} AndroidSetRsaParametersData;

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
PALEXPORT int32_t AndroidCryptoNative_GetRsaParameters(RSA* rsa, AndroidGetRsaParametersData* parameters);
PALEXPORT int32_t AndroidCryptoNative_SetRsaParameters(RSA* rsa, AndroidSetRsaParametersData* parameters);

RSA* AndroidCryptoNative_NewRsaFromKeys(JNIEnv* env, jobject /*RSAPublicKey*/ publicKey, jobject /*RSAPrivateKey*/ privateKey) ARGS_NON_NULL(1, 2);
RSA* AndroidCryptoNative_NewRsaFromPublicKey(JNIEnv* env, jobject /*RSAPublicKey*/ key) ARGS_NON_NULL(1, 2);
