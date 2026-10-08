// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_rsa.h"
#include "pal_bignum.h"
#include "pal_signature.h"
#include "pal_utilities.h"

#define RSA_FAIL -1

static jobject GetRsaOaepPadding(JNIEnv* env, RsaPadding padding)
{
    assert(padding >= OaepSHA1 && padding <= OaepSHA512);

    jobject oaepParameterSpec;
    INIT_LOCALS(oaepLocals, oaepDigest, mgf1, mgf1ParameterSpec, pSource);

    oaepLocals[mgf1] = make_java_string(env, "MGF1");
    oaepLocals[pSource] = (*env)->GetStaticObjectField(env, g_PSourcePSpecifiedClass, g_PSourcePSpecified_DefaultField);

    if (padding == OaepSHA1)
    {
        oaepLocals[mgf1ParameterSpec] = (*env)->GetStaticObjectField(env, g_MGF1ParameterSpecClass, g_MGF1ParameterSpec_SHA1Field);
        oaepLocals[oaepDigest] = make_java_string(env, "SHA-1");
    }
    else if (padding == OaepSHA256)
    {
        oaepLocals[mgf1ParameterSpec] = (*env)->GetStaticObjectField(env, g_MGF1ParameterSpecClass, g_MGF1ParameterSpec_SHA256Field);
        oaepLocals[oaepDigest] = make_java_string(env, "SHA-256");
    }
    else if (padding == OaepSHA384)
    {
        oaepLocals[mgf1ParameterSpec] = (*env)->GetStaticObjectField(env, g_MGF1ParameterSpecClass, g_MGF1ParameterSpec_SHA384Field);
        oaepLocals[oaepDigest] = make_java_string(env, "SHA-384");
    }
    else if (padding == OaepSHA512)
    {
        oaepLocals[mgf1ParameterSpec] = (*env)->GetStaticObjectField(env, g_MGF1ParameterSpecClass, g_MGF1ParameterSpec_SHA512Field);
        oaepLocals[oaepDigest] = make_java_string(env, "SHA-512");
    }
    else
    {
        RELEASE_LOCALS(oaepLocals, env);
        return FAIL;
    }

    oaepParameterSpec = (*env)->NewObject(
        env,
        g_OAEPParameterSpecClass,
        g_OAEPParameterSpecCtor,
        oaepLocals[oaepDigest], oaepLocals[mgf1], oaepLocals[mgf1ParameterSpec], oaepLocals[pSource]);

    RELEASE_LOCALS(oaepLocals, env);
    return CheckJNIExceptions(env) ? FAIL : oaepParameterSpec;
}

PALEXPORT RSA* AndroidCryptoNative_RsaCreate(void)
{
    RSA* rsa = xcalloc(1, sizeof(RSA));
    atomic_init(&rsa->refCount, 1);
    return rsa;
}

#pragma clang diagnostic push
// There's no way to specify explicit memory ordering for increment/decrement with C atomics.
#pragma clang diagnostic ignored "-Watomic-implicit-seq-cst"
PALEXPORT int32_t AndroidCryptoNative_RsaUpRef(RSA* rsa)
{
    if (!rsa)
        return FAIL;
    rsa->refCount++;
    return SUCCESS;
}

PALEXPORT void AndroidCryptoNative_RsaDestroy(RSA* rsa)
{
    if (rsa)
    {
        rsa->refCount--;
        if (rsa->refCount == 0)
        {
            JNIEnv* env = GetJNIEnv();
            ReleaseGRef(env, rsa->privateKey);
            ReleaseGRef(env, rsa->publicKey);
            free(rsa);
        }
    }
}
#pragma clang diagnostic pop

PALEXPORT int32_t AndroidCryptoNative_RsaPublicEncrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    abort_if_invalid_pointer_argument (to);
    abort_if_invalid_pointer_argument (rsa);

    if ((flen > 0 && !from) || flen < 0)
        return RSA_FAIL;

    if (padding < Pkcs1 || padding > OaepSHA512)
        return RSA_FAIL;

    JNIEnv* env = GetJNIEnv();

    int32_t ret = RSA_FAIL;
    INIT_LOCALS(loc, algName, cipher, fromBytes, encryptedBytes);
    jobject oaepParameterSpec = NULL;

    if (padding == Pkcs1)
    {
        loc[algName] = make_java_string(env, "RSA/ECB/PKCS1Padding");
        loc[cipher] = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, loc[algName]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        (*env)->CallVoidMethod(env, loc[cipher], g_cipherInit2Method, CIPHER_ENCRYPT_MODE, rsa->publicKey);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }
    else
    {
        loc[algName] = make_java_string(env, "RSA/ECB/OAEPPadding");
        loc[cipher] = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, loc[algName]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        oaepParameterSpec = GetRsaOaepPadding(env, padding);

        if (oaepParameterSpec == FAIL)
        {
            goto cleanup;
        }

        (*env)->CallVoidMethod(env, loc[cipher], g_cipherInitMethod, CIPHER_ENCRYPT_MODE, rsa->publicKey, oaepParameterSpec);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    loc[fromBytes] = make_java_byte_array(env, flen);
    (*env)->SetByteArrayRegion(env, loc[fromBytes], 0, flen, (jbyte*)from);
    loc[encryptedBytes] = (jbyteArray)(*env)->CallObjectMethod(env, loc[cipher], g_cipherDoFinal2Method, loc[fromBytes]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    jsize encryptedBytesLen = (*env)->GetArrayLength(env, loc[encryptedBytes]);
    (*env)->GetByteArrayRegion(env, loc[encryptedBytes], 0, encryptedBytesLen, (jbyte*) to);

    ret = (int32_t)encryptedBytesLen;

cleanup:
    RELEASE_LOCALS(loc, env);

    if (oaepParameterSpec != NULL && oaepParameterSpec != FAIL)
    {
        (*env)->DeleteLocalRef(env, oaepParameterSpec);
    }

    return ret;
}

PALEXPORT int32_t AndroidCryptoNative_RsaPrivateDecrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    if (!rsa)
        return RSA_FAIL;

    if (!rsa->privateKey)
        return RSA_FAIL;

    if (padding < Pkcs1 || padding > OaepSHA512)
        return RSA_FAIL;

    abort_if_invalid_pointer_argument (to);
    abort_if_invalid_pointer_argument (from);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = RSA_FAIL;
    jobject cipher = NULL;
    jobject algName = NULL;
    jobject oaepParameterSpec = NULL;
    jbyteArray fromBytes = NULL;
    jbyteArray decryptedBytes = NULL;

    if (padding == Pkcs1)
    {
        algName = make_java_string(env, "RSA/ECB/PKCS1Padding");
        cipher = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        (*env)->CallVoidMethod(env, cipher, g_cipherInit2Method, CIPHER_DECRYPT_MODE, rsa->privateKey);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }
    else
    {
        algName = make_java_string(env, "RSA/ECB/OAEPPadding");
        cipher = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        oaepParameterSpec = GetRsaOaepPadding(env, padding);

        if (oaepParameterSpec == FAIL)
        {
            oaepParameterSpec = NULL;
            goto cleanup;
        }

        (*env)->CallVoidMethod(env, cipher, g_cipherInitMethod, CIPHER_DECRYPT_MODE, rsa->privateKey, oaepParameterSpec);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    fromBytes = make_java_byte_array(env, flen);
    (*env)->SetByteArrayRegion(env, fromBytes, 0, flen, (jbyte*)from);
    decryptedBytes = (jbyteArray)(*env)->CallObjectMethod(env, cipher, g_cipherDoFinal2Method, fromBytes);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    jsize decryptedBytesLen = (*env)->GetArrayLength(env, decryptedBytes);
    (*env)->GetByteArrayRegion(env, decryptedBytes, 0, decryptedBytesLen, (jbyte*) to);
    ret = (int32_t)decryptedBytesLen;

cleanup:
    ReleaseLRef(env, cipher);
    ReleaseLRef(env, fromBytes);
    ReleaseLRef(env, decryptedBytes);
    ReleaseLRef(env, algName);
    ReleaseLRef(env, oaepParameterSpec);

    return ret;
}

PALEXPORT int32_t AndroidCryptoNative_RsaSize(RSA* rsa)
{
    if (!rsa)
        return FAIL;
    return rsa->keyWidthInBits / 8;
}

PALEXPORT RSA* AndroidCryptoNative_DecodeRsaSubjectPublicKeyInfo(uint8_t* buf, int32_t len)
{
    if (!buf || len <= 0)
    {
        return FAIL;
    }

    JNIEnv* env = GetJNIEnv();
    RSA* rsa = FAIL;

    // KeyFactory keyFactory = KeyFactory.getInstance("RSA");
    // X509EncodedKeySpec x509keySpec = new X509EncodedKeySpec(bytes);
    // PublicKey publicKey = keyFactory.generatePublic(x509keySpec);

    jobject algName = make_java_string(env, "RSA");
    jobject keyFactory = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, algName);
    jbyteArray bytes = NULL;
    jobject x509keySpec = NULL;
    jobject publicKey = NULL;
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    bytes = make_java_byte_array(env, len);
    (*env)->SetByteArrayRegion(env, bytes, 0, len, (jbyte*)buf);
    x509keySpec = (*env)->NewObject(env, g_X509EncodedKeySpecClass, g_X509EncodedKeySpecCtor, bytes);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    publicKey = (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPublicMethod, x509keySpec);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    rsa = AndroidCryptoNative_NewRsaFromKeys(env, publicKey, NULL /*privateKey*/);

cleanup:
    ReleaseLRef(env, algName);
    ReleaseLRef(env, keyFactory);
    ReleaseLRef(env, bytes);
    ReleaseLRef(env, x509keySpec);
    ReleaseLRef(env, publicKey);

    return rsa;
}

PALEXPORT int32_t AndroidCryptoNative_RsaSignPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa)
{
    if (!rsa)
        return RSA_FAIL;

    if (!rsa->privateKey)
    {
        LOG_ERROR("RSA private key required to sign.");
        return RSA_FAIL;
    }

    abort_if_invalid_pointer_argument (to);
    abort_if_invalid_pointer_argument (from);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = RSA_FAIL;
    jobject algName = make_java_string(env, "RSA/ECB/NoPadding");
    jobject cipher = NULL;
    jbyteArray fromBytes = NULL;
    jbyteArray encryptedBytes = NULL;

    cipher = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, cipher, g_cipherInit2Method, CIPHER_ENCRYPT_MODE, rsa->privateKey);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    fromBytes = make_java_byte_array(env, flen);
    (*env)->SetByteArrayRegion(env, fromBytes, 0, flen, (jbyte*)from);
    encryptedBytes = (jbyteArray)(*env)->CallObjectMethod(env, cipher, g_cipherDoFinal2Method, fromBytes);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    jsize encryptedBytesLen = (*env)->GetArrayLength(env, encryptedBytes);
    (*env)->GetByteArrayRegion(env, encryptedBytes, 0, encryptedBytesLen, (jbyte*) to);
    ret = (int32_t)encryptedBytesLen;

cleanup:
    ReleaseLRef(env, cipher);
    ReleaseLRef(env, fromBytes);
    ReleaseLRef(env, encryptedBytes);
    ReleaseLRef(env, algName);

    return ret;
}

PALEXPORT int32_t AndroidCryptoNative_RsaVerificationPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa)
{
    if (!rsa)
        return RSA_FAIL;

    abort_if_invalid_pointer_argument (to);
    abort_if_invalid_pointer_argument (from);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    jobject algName = make_java_string(env, "RSA/ECB/NoPadding");
    jobject cipher = NULL;
    jbyteArray fromBytes = NULL;
    jbyteArray decryptedBytes = NULL;

    cipher = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, cipher, g_cipherInit2Method, CIPHER_DECRYPT_MODE, rsa->publicKey);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    fromBytes = make_java_byte_array(env, flen);
    (*env)->SetByteArrayRegion(env, fromBytes, 0, flen, (jbyte*)from);
    decryptedBytes = (jbyteArray)(*env)->CallObjectMethod(env, cipher, g_cipherDoFinal2Method, fromBytes);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    jsize decryptedBytesLen = (*env)->GetArrayLength(env, decryptedBytes);
    abort_unless(decryptedBytesLen <= flen, "Decrypted bytes length %d exceeds expected length %d", decryptedBytesLen, flen);

    // In some versions of the Android crypto libraries, the leading 0x00 bytes are missing.
    // Left-pad with 0x00 so EM is always k bytes (k = modulus), as expected by .NET.
    int32_t leading_zero_padding_length = flen - decryptedBytesLen;
    memset(to, 0x00, (size_t)leading_zero_padding_length);

    (*env)->GetByteArrayRegion(env, decryptedBytes, 0, decryptedBytesLen, (jbyte*)to + leading_zero_padding_length);
    ret = (int32_t)decryptedBytesLen + leading_zero_padding_length;

cleanup:
    ReleaseLRef(env, cipher);
    ReleaseLRef(env, fromBytes);
    ReleaseLRef(env, decryptedBytes);
    ReleaseLRef(env, algName);

    return ret;
}

PALEXPORT int32_t AndroidCryptoNative_RsaGenerateKeyEx(RSA* rsa, int32_t bits)
{
    if (!rsa)
        return FAIL;

    // KeyPairGenerator kpg = KeyPairGenerator.getInstance("RSA");
    // kpg.initialize(bits);
    // KeyPair kp = kpg.genKeyPair();

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    jobject rsaStr = make_java_string(env, "RSA");
    jobject kpgObj = NULL;
    jobject keyPair = NULL;
    jobject newPrivateKey = NULL;
    jobject newPublicKey = NULL;

    kpgObj = (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, rsaStr);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    (*env)->CallVoidMethod(env, kpgObj, g_keyPairGenInitializeMethod, bits);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    keyPair = (*env)->CallObjectMethod(env, kpgObj, g_keyPairGenGenKeyPairMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    newPrivateKey = ToGRef(env, (*env)->CallObjectMethod(env, keyPair, g_keyPairGetPrivateMethod));
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    newPublicKey = ToGRef(env, (*env)->CallObjectMethod(env, keyPair, g_keyPairGetPublicMethod));
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    rsa->privateKey = newPrivateKey;
    rsa->publicKey = newPublicKey;
    rsa->keyWidthInBits = bits;
    newPrivateKey = NULL;
    newPublicKey = NULL;
    ret = SUCCESS;

cleanup:
    ReleaseLRef(env, rsaStr);
    ReleaseLRef(env, kpgObj);
    ReleaseLRef(env, keyPair);
    ReleaseGRef(env, newPrivateKey);
    ReleaseGRef(env, newPublicKey);

    return ret;
}

PALEXPORT int32_t AndroidCryptoNative_GetRsaParameters(RSA* rsa,
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

    *n = NULL;
    *e = NULL;
    *d = NULL;
    *p = NULL;
    *dmp1 = NULL;
    *q = NULL;
    *dmq1 = NULL;
    *iqmp = NULL;

    JNIEnv* env = GetJNIEnv();
    jobject privateKey = rsa->privateKey;
    jobject publicKey = rsa->publicKey;

    if (privateKey)
    {
        *e = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPubExpField));
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        *n = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyModulusField));
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        *d = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrivExpField));
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        *p = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimePField));
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        *q = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimeQField));
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        *dmp1 = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimeExpPField));
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        *dmq1 = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimeExpQField));
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        *iqmp = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyCrtCoefField));
        ON_EXCEPTION_PRINT_AND_GOTO(error);
    }
    else if (publicKey)
    {
        *e = ToGRef(env, (*env)->CallObjectMethod(env, publicKey, g_RSAPublicKeyGetPubExpMethod));
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        *n = ToGRef(env, (*env)->CallObjectMethod(env, publicKey, g_RSAKeyGetModulus));
        ON_EXCEPTION_PRINT_AND_GOTO(error);
    }
    else
    {
        return FAIL;
    }

    return SUCCESS;

error:
    ReleaseGRef(env, *n); *n = NULL;
    ReleaseGRef(env, *e); *e = NULL;
    ReleaseGRef(env, *d); *d = NULL;
    ReleaseGRef(env, *p); *p = NULL;
    ReleaseGRef(env, *dmp1); *dmp1 = NULL;
    ReleaseGRef(env, *q); *q = NULL;
    ReleaseGRef(env, *dmq1); *dmq1 = NULL;
    ReleaseGRef(env, *iqmp); *iqmp = NULL;
    return FAIL;
}

PALEXPORT int32_t AndroidCryptoNative_SetRsaParameters(RSA* rsa,
    uint8_t* n,    int32_t nLength,    uint8_t* e,    int32_t eLength,    uint8_t* d, int32_t dLength,
    uint8_t* p,    int32_t pLength,    uint8_t* dmp1, int32_t dmp1Length, uint8_t* q, int32_t qLength,
    uint8_t* dmq1, int32_t dmq1Length, uint8_t* iqmp, int32_t iqmpLength)
{
    if (!rsa)
        return FAIL;

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    INIT_LOCALS(bn, N, E, D, P, Q, DMP1, DMQ1, IQMP);
    INIT_LOCALS(loc, algName, keyFactory, rsaPubKeySpec, rsaPrivateKeySpec);
    jobject newPrivateKey = NULL;
    jobject newPublicKey = NULL;

    bn[N] = AndroidCryptoNative_BigNumFromBinary(n, nLength);
    bn[E] = AndroidCryptoNative_BigNumFromBinary(e, eLength);

    loc[algName] = make_java_string(env, "RSA");
    loc[keyFactory] = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[algName]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (dLength > 0)
    {
        // private key section
        bn[D] = AndroidCryptoNative_BigNumFromBinary(d, dLength);
        bn[P] = AndroidCryptoNative_BigNumFromBinary(p, pLength);
        bn[Q] = AndroidCryptoNative_BigNumFromBinary(q, qLength);
        bn[DMP1] = AndroidCryptoNative_BigNumFromBinary(dmp1, dmp1Length);
        bn[DMQ1] = AndroidCryptoNative_BigNumFromBinary(dmq1, dmq1Length);
        bn[IQMP] = AndroidCryptoNative_BigNumFromBinary(iqmp, iqmpLength);

        loc[rsaPrivateKeySpec] = (*env)->NewObject(env, g_RSAPrivateCrtKeySpecClass, g_RSAPrivateCrtKeySpecCtor,
            bn[N], bn[E], bn[D], bn[P], bn[Q], bn[DMP1], bn[DMQ1], bn[IQMP]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        newPrivateKey = ToGRef(env, (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPrivateMethod, loc[rsaPrivateKeySpec]));
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    loc[rsaPubKeySpec] = (*env)->NewObject(env, g_RSAPublicCrtKeySpecClass, g_RSAPublicCrtKeySpecCtor, bn[N], bn[E]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    newPublicKey = ToGRef(env, (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPublicMethod, loc[rsaPubKeySpec]));
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (newPrivateKey != NULL)
    {
        ReleaseGRef(env, rsa->privateKey);
        rsa->privateKey = newPrivateKey;
        newPrivateKey = NULL;
    }
    ReleaseGRef(env, rsa->publicKey);
    rsa->publicKey = newPublicKey;
    newPublicKey = NULL;
    rsa->keyWidthInBits = nLength * 8;
    ret = SUCCESS;

cleanup:
    ReleaseGRef(env, newPrivateKey);
    ReleaseGRef(env, newPublicKey);
    RELEASE_LOCALS(bn, env);
    RELEASE_LOCALS(loc, env);
    return ret;
}

RSA* AndroidCryptoNative_NewRsaFromKeys(JNIEnv* env, jobject /*RSAPublicKey*/ publicKey, jobject /*RSAPrivateKey*/ privateKey)
{
    if (!(*env)->IsInstanceOf(env, publicKey, g_RSAPublicKeyClass))
        return NULL;

    RSA* ret = NULL;
    jobject modulus = (*env)->CallObjectMethod(env, publicKey, g_RSAKeyGetModulus);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (!modulus)
        goto cleanup;

    ret = AndroidCryptoNative_RsaCreate();
    ret->publicKey = AddGRef(env, publicKey);
    ret->privateKey = AddGRef(env, privateKey);
    ret->keyWidthInBits = AndroidCryptoNative_GetBigNumBytes(modulus) * 8;

cleanup:
    ReleaseLRef(env, modulus);
    return ret;
}
