// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_rsa.h"

PALEXPORT RSA* CryptoNative_RsaCreate()
{
    RSA* rsa = malloc(sizeof(RSA));
    rsa->privateKey = NULL;
    rsa->publicKey = NULL;
    rsa->pubExp = NULL;
    rsa->keyWidth = 0;
    rsa->refCount = 1;
    return rsa;
}

PALEXPORT int32_t CryptoNative_RsaUpRef(RSA* rsa)
{
    if (!rsa)
        return FAIL;
    rsa->refCount++;
    return SUCCESS;
}

PALEXPORT void CryptoNative_RsaDestroy(RSA* rsa)
{
    if (rsa)
    {
        rsa->refCount--;
        if (rsa->refCount == 0)
        {
            JNIEnv* env = GetJNIEnv();
            ReleaseGRef(env, rsa->privateKey);
            ReleaseGRef(env, rsa->publicKey);
            ReleaseGRef(env, rsa->pubExp);
            free(rsa);
        }
    }
}

PALEXPORT int32_t CryptoNative_RsaPublicEncrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    if (!rsa)
        return FAIL;

    JNIEnv* env = GetJNIEnv();

    jobject algName;
    if (padding == Pkcs1)
        algName = JSTRING("RSA/ECB/PKCS1Padding");
    else if (padding == OaepSHA1)
        algName = JSTRING("RSA/ECB/OAEPPadding");
    else
        algName = JSTRING("RSA/ECB/NoPadding");

    jobject cipher = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName);
    (*env)->CallVoidMethod(env, cipher, g_cipherInit2Method, CIPHER_ENCRYPT_MODE, rsa->publicKey);
    jbyteArray fromBytes = (*env)->NewByteArray(env, flen);
    (*env)->SetByteArrayRegion(env, fromBytes, 0, flen, (jbyte*)from);
    jbyteArray encryptedBytes = (jbyteArray)(*env)->CallObjectMethod(env, cipher, g_cipherDoFinal2Method, fromBytes);
    jsize encryptedBytesLen = (*env)->GetArrayLength(env, encryptedBytes);
    (*env)->GetByteArrayRegion(env, encryptedBytes, 0, encryptedBytesLen, (jbyte*) to);

    (*env)->DeleteLocalRef(env, cipher);
    (*env)->DeleteLocalRef(env, fromBytes);
    (*env)->DeleteLocalRef(env, encryptedBytes);
    (*env)->DeleteLocalRef(env, algName);

    return (int32_t)encryptedBytesLen;
}

PALEXPORT int32_t CryptoNative_RsaPrivateDecrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    if (!rsa)
        return FAIL;

    JNIEnv* env = GetJNIEnv();

    jobject algName;
    if (padding == Pkcs1)
        algName = JSTRING("RSA/ECB/PKCS1Padding"); // TODO: is ECB needed here?
    else if (padding == OaepSHA1)
        algName = JSTRING("RSA/ECB/OAEPPadding");
    else
        algName = JSTRING("RSA/ECB/NoPadding");

    jobject cipher = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName);
    (*env)->CallVoidMethod(env, cipher, g_cipherInit2Method, CIPHER_DECRYPT_MODE, rsa->privateKey);
    jbyteArray fromBytes = (*env)->NewByteArray(env, flen);
    (*env)->SetByteArrayRegion(env, fromBytes, 0, flen, (jbyte*)from);
    jbyteArray decryptedBytes = (jbyteArray)(*env)->CallObjectMethod(env, cipher, g_cipherDoFinal2Method, fromBytes);
    jsize decryptedBytesLen = (*env)->GetArrayLength(env, decryptedBytes);
    (*env)->GetByteArrayRegion(env, decryptedBytes, 0, decryptedBytesLen, (jbyte*) to);

    (*env)->DeleteLocalRef(env, cipher);
    (*env)->DeleteLocalRef(env, fromBytes);
    (*env)->DeleteLocalRef(env, decryptedBytes);
    (*env)->DeleteLocalRef(env, algName);

    return (int32_t)decryptedBytesLen;
}

PALEXPORT int32_t CryptoNative_RsaSize(RSA* rsa)
{
    if (!rsa)
        return FAIL;
    return rsa->keyWidth / 8; 
}

PALEXPORT RSA* CryptoNative_DecodeRsaPublicKey(uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return NULL;
    }

    JNIEnv* env = GetJNIEnv();

    // KeyFactory keyFactory = KeyFactory.getInstance("RSA");
    // X509EncodedKeySpec x509keySpec = new X509EncodedKeySpec(bytes);
    // PublicKey publicKey = keyFactory.generatePublic(x509keySpec);

    jobject algName = JSTRING("RSA");
    jobject keyFactory = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, algName);
    jbyteArray bytes = (*env)->NewByteArray(env, len);
    (*env)->SetByteArrayRegion(env, bytes, 0, len, (jbyte*)buf);
    jobject x509keySpec = (*env)->NewObject(env, g_X509EncodedKeySpecClass, g_X509EncodedKeySpecCtor, bytes);

    RSA* rsa = CryptoNative_RsaCreate();
    rsa->publicKey = ToGRef(env, (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPublicMethod, x509keySpec));

    (*env)->DeleteLocalRef(env, algName);
    (*env)->DeleteLocalRef(env, keyFactory);
    (*env)->DeleteLocalRef(env, bytes);
    (*env)->DeleteLocalRef(env, x509keySpec);

    return rsa;
}

PALEXPORT int32_t CryptoNative_RsaSignPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa)
{
    // TODO:
    return FAIL;
}

PALEXPORT int32_t CryptoNative_RsaVerificationPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa)
{
    // TODO:
    return FAIL;
}

PALEXPORT int32_t CryptoNative_RsaSign(int32_t type, uint8_t* m, int32_t mlen, uint8_t* sigret, int32_t* siglen, RSA* rsa)
{
    // TODO:
    return FAIL;
}

PALEXPORT int32_t CryptoNative_RsaVerify(int32_t type, uint8_t* m, int32_t mlen, uint8_t* sigbuf, int32_t siglen, RSA* rsa)
{
    // TODO:
    return FAIL;
}

PALEXPORT int32_t CryptoNative_RsaGenerateKeyEx(RSA* rsa, int32_t bits, jobject pubExp)
{
    if (!rsa)
        return FAIL;

    // KeyPairGenerator kpg = KeyPairGenerator.getInstance("RSA");
    // kpg.initialize(bits);
    // KeyPair kp = kpg.genKeyPair();

    JNIEnv* env = GetJNIEnv();
    jobject rsaStr = JSTRING("RSA");
    jobject kpgObj =  (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, rsaStr);
    (*env)->CallVoidMethod(env, kpgObj, g_keyPairGenInitializeMethod, bits);
    jobject keyPair = (*env)->CallObjectMethod(env, kpgObj, g_keyPairGenGenKeyPairMethod);

    rsa->privateKey = ToGRef(env, (*env)->CallObjectMethod(env, keyPair, g_keyPairGetPrivateMethod));
    rsa->publicKey = ToGRef(env, (*env)->CallObjectMethod(env, keyPair, g_keyPairGetPublicMethod));
    rsa->keyWidth = bits;
    // pubExp is already expected to be a gref at this point but we need to create another one.
    rsa->pubExp = AddGRef(env, pubExp);

    (*env)->DeleteLocalRef(env, rsaStr);
    (*env)->DeleteLocalRef(env, kpgObj);
    (*env)->DeleteLocalRef(env, keyPair);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

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

    JNIEnv* env = GetJNIEnv();
    jobject privateKey = rsa->privateKey;
    jobject publicKey = rsa->publicKey;

    if (privateKey)
    {
        *e = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPubExpField));
        *n = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyModulusField));
        *d = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrivExpField));
        *p = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimePField));
        *q = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimeQField));
        *dmp1 = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimeExpPField));
        *dmq1 = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimeExpQField));
        *iqmp = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyCrtCoefField));
    }
    else
    {
        assert(publicKey);
        *e = ToGRef(env, (*env)->CallObjectMethod(env, publicKey, g_RSAPublicKeyGetPubExpMethod));
        *n = ToGRef(env, (*env)->CallObjectMethod(env, publicKey, g_RSAKeyGetModulus));
        *d = NULL;
        *p = NULL;
        *q = NULL;
        *dmp1 = NULL;
        *dmq1 = NULL;
        *iqmp = NULL;
    }

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

jobject BigNumFromBinary(JNIEnv* env, uint8_t* bytes, int32_t len)
{
    assert(len > 0);
    jbyteArray buffArray = (*env)->NewByteArray(env, len);
    (*env)->SetByteArrayRegion(env, buffArray, 0, len, (jbyte*)bytes);
    jobject bigNum = (*env)->NewObject(env, g_bigNumClass, g_bigNumCtor, buffArray);
    (*env)->DeleteLocalRef(env, buffArray);
    return bigNum;
}

PALEXPORT int32_t CryptoNative_SetRsaParameters(RSA* rsa, 
    uint8_t* n,    int32_t nLength,    uint8_t* e,    int32_t eLength,    uint8_t* d, int32_t dLength, 
    uint8_t* p,    int32_t pLength,    uint8_t* dmp1, int32_t dmp1Length, uint8_t* q, int32_t qLength, 
    uint8_t* dmq1, int32_t dmq1Length, uint8_t* iqmp, int32_t iqmpLength)
{
    if (!rsa)
        return FAIL;

    JNIEnv* env = GetJNIEnv();

    jobject nObj = BigNumFromBinary(env, n, nLength);
    jobject eObj = BigNumFromBinary(env, e, eLength);

    rsa->keyWidth = (nLength - 1) * 8; // Android SDK has an extra byte in Modulus(?)

    jobject algName = JSTRING("RSA");
    jobject keyFactory = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, algName);

    if (dLength > 0)
    {
        // private key section
        jobject dObj = BigNumFromBinary(env, d, dLength);
        jobject pObj = BigNumFromBinary(env, p, pLength);
        jobject qObj = BigNumFromBinary(env, q, qLength);
        jobject dmp1Obj = BigNumFromBinary(env, dmp1, dmp1Length);
        jobject dmq1Obj = BigNumFromBinary(env, dmq1, dmq1Length);
        jobject iqmpObj = BigNumFromBinary(env, iqmp, iqmpLength);

        jobject rsaPrivateKeySpec = (*env)->NewObject(env, g_RSAPrivateCrtKeySpecClass, g_RSAPrivateCrtKeySpecCtor,
            nObj, eObj, dObj, pObj, qObj, dmp1Obj, dmq1Obj, iqmpObj);

        ReleaseGRef(env, rsa->privateKey);
        rsa->privateKey = ToGRef(env, (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPrivateMethod, rsaPrivateKeySpec));

        (*env)->DeleteLocalRef(env, dObj);
        (*env)->DeleteLocalRef(env, pObj);
        (*env)->DeleteLocalRef(env, qObj);
        (*env)->DeleteLocalRef(env, dmp1Obj);
        (*env)->DeleteLocalRef(env, dmq1Obj);
        (*env)->DeleteLocalRef(env, iqmpObj);
        (*env)->DeleteLocalRef(env, rsaPrivateKeySpec);
    }

    jobject rsaPubKeySpec = (*env)->NewObject(env, g_RSAPublicCrtKeySpecClass, g_RSAPublicCrtKeySpecCtor, nObj, eObj);

    ReleaseGRef(env, rsa->publicKey);
    rsa->publicKey = ToGRef(env, (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPublicMethod, rsaPubKeySpec));

    (*env)->DeleteLocalRef(env, algName);
    (*env)->DeleteLocalRef(env, keyFactory);
    (*env)->DeleteLocalRef(env, nObj);
    (*env)->DeleteLocalRef(env, eObj);
    (*env)->DeleteLocalRef(env, rsaPubKeySpec);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}
