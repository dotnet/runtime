// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

typedef enum
{
    Pkcs1 = 0,
    OaepSHA1 = 1,
    NoPadding = 2,
} RsaPadding;

typedef struct RSA
{
    jobject pubExp;
    jobject privateKey; // RSAPrivateCrtKey
    jobject publicKey;  // RSAPublicCrtKey
    int32_t refCount;
    int32_t keyWidth;
} RSA;

#define CIPHER_ENCRYPT_MODE 1
#define CIPHER_DECRYPT_MODE 2

jobject BigNumFromBinary(JNIEnv* env, uint8_t* bytes, int32_t len);

PALEXPORT RSA* CryptoNative_RsaCreate(void);
PALEXPORT int32_t CryptoNative_RsaUpRef(RSA* rsa);
PALEXPORT void CryptoNative_RsaDestroy(RSA* rsa);
PALEXPORT RSA* CryptoNative_DecodeRsaPublicKey(uint8_t* buf, int32_t len);
PALEXPORT int32_t CryptoNative_RsaPublicEncrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding);
PALEXPORT int32_t CryptoNative_RsaPrivateDecrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding);
PALEXPORT int32_t CryptoNative_RsaSignPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa);
PALEXPORT int32_t CryptoNative_RsaVerificationPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa);
PALEXPORT int32_t CryptoNative_RsaSize(RSA* rsa);
PALEXPORT int32_t CryptoNative_RsaGenerateKeyEx(RSA* rsa, int32_t bits, jobject pubExp);
PALEXPORT int32_t CryptoNative_RsaSign(int32_t type, uint8_t* m, int32_t mlen, uint8_t* sigret, int32_t* siglen, RSA* rsa);
PALEXPORT int32_t CryptoNative_RsaVerify(int32_t type, uint8_t* m, int32_t mlen, uint8_t* sigbuf, int32_t siglen, RSA* rsa);
PALEXPORT int32_t CryptoNative_GetRsaParameters(RSA* rsa, 
    jobject* n, jobject* e, jobject* d, jobject* p, jobject* dmp1, jobject* q, jobject* dmq1, jobject* iqmp);
PALEXPORT int32_t CryptoNative_SetRsaParameters(RSA* rsa, 
    uint8_t* n,    int32_t nLength,    uint8_t* e,    int32_t eLength,    uint8_t* d, int32_t dLength, 
    uint8_t* p,    int32_t pLength,    uint8_t* dmp1, int32_t dmp1Length, uint8_t* q, int32_t qLength, 
    uint8_t* dmq1, int32_t dmq1Length, uint8_t* iqmp, int32_t iqmpLength);
