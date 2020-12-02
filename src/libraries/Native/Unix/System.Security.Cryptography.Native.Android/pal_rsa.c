// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_rsa.h"

PALEXPORT RSA* CryptoNative_RsaCreate()
{
    jobject algName = JSTRING("RSA");
    jobject cipher = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName));
    (*env)->DeleteLocalRef(env, algName);

    RSA* rsa = malloc(sizeof(RSA));
    rsa->cipher = cipher;
    return rsa;
}

PALEXPORT int32_t CryptoNative_RsaUpRef(RSA* rsa)
{
    return FAIL;
}

PALEXPORT void CryptoNative_RsaDestroy(RSA* rsa)
{
    if (rsa)
    {
        ReleaseGRef(GetJNIEnv(), rsa->obj);
        free(rsa);
    }
}

PALEXPORT RSA* CryptoNative_DecodeRsaPublicKey(uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return NULL;
    }

    return NULL;
}

// 6
PALEXPORT int32_t CryptoNative_RsaPublicEncrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    return FAIL;
}

PALEXPORT int32_t CryptoNative_RsaPrivateDecrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    return FAIL;
}

PALEXPORT int32_t CryptoNative_RsaSignPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa)
{
    return FAIL;
}

PALEXPORT int32_t CryptoNative_RsaVerificationPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa)
{
    return FAIL;
}

// 4
PALEXPORT int32_t CryptoNative_RsaSize(RSA* rsa)
{
    return FAIL;
}

// 2
PALEXPORT int32_t CryptoNative_RsaGenerateKeyEx(RSA* rsa, int32_t bits, jobject e)
{
    return FAIL;
}

PALEXPORT int32_t CryptoNative_RsaSign(int32_t type, uint8_t* m, int32_t mlen, uint8_t* sigret, int32_t* siglen, RSA* rsa)
{
    return FAIL;
}

PALEXPORT int32_t CryptoNative_RsaVerify(int32_t type, uint8_t* m, int32_t mlen, uint8_t* sigbuf, int32_t siglen, RSA* rsa)
{
    return FAIL;
}

// 3
PALEXPORT int32_t CryptoNative_GetRsaParameters(RSA* rsa, 
    jobject* n, jobject* e, jobject* d, jobject* p, jobject* dmp1, jobject* q, jobject* dmq1, jobject* iqmp)
{
    if (!rsa || !n || !e || !d || !p || !dmp1 || !q || !dmq1 || !iqmp)
    {
        assert(false);

        // since these parameters are 'out' parameters in managed code, ensure they are initialized
        if (n)
            *n = NULL;
        if (e)
            *e = NULL;
        if (d)
            *d = NULL;
        if (p)
            *p = NULL;
        if (dmp1)
            *dmp1 = NULL;
        if (q)
            *q = NULL;
        if (dmq1)
            *dmq1 = NULL;
        if (iqmp)
            *iqmp = NULL;

        return FAIL;
    }

    return FAIL;
}

// 5
PALEXPORT int32_t CryptoNative_SetRsaParameters(RSA* rsa, 
    uint8_t* n,    int32_t nLength,    uint8_t* e,    int32_t eLength,     int8_t* d, int32_t dLength, 
    uint8_t* p,    int32_t pLength,    uint8_t* dmp1, int32_t dmp1Length, uint8_t* q, int32_t qLength, 
    uint8_t* dmq1, int32_t dmq1Length, uint8_t* iqmp, int32_t iqmpLength)
{
    return FAIL;
}