// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_rsa.h"
#include "pal_bignum.h"
#include "pal_signature.h"
#include "pal_utilities.h"

#define RSA_FAIL -1

RSA* AndroidCryptoNative_RsaCreate()
{
    RSA* rsa = xcalloc(1, sizeof(RSA));
    atomic_init(&rsa->refCount, 1);
    return rsa;
}

#pragma clang diagnostic push
// There's no way to specify explicit memory ordering for increment/decrement with C atomics.
#pragma clang diagnostic ignored "-Watomic-implicit-seq-cst"
int32_t AndroidCryptoNative_RsaUpRef(RSA* rsa)
{
    if (!rsa)
        return FAIL;
    rsa->refCount++;
    return SUCCESS;
}

void AndroidCryptoNative_RsaDestroy(RSA* rsa)
{
    if (rsa == NULL)
    {
        return;
    }

    rsa->refCount--;
    if (rsa->refCount != 0)
    {
        return;
    }

    JNIEnv* env = GetJNIEnv();
    ReleaseGRef(env, rsa->privateKey);
    ReleaseGRef(env, rsa->publicKey);
    free(rsa);
}
#pragma clang diagnostic pop

int32_t AndroidCryptoNative_RsaPublicEncrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    abort_if_invalid_pointer_argument (from);
    abort_if_invalid_pointer_argument (to);
    abort_if_invalid_pointer_argument (rsa);

    JNIEnv* env = GetJNIEnv();

    int32_t ret = RSA_FAIL;
    INIT_LOCALS(loc, algName, cipher, fromBytes, encryptedBytes);

    if (padding == Pkcs1)
    {
        loc[algName] = make_java_string(env, "RSA/ECB/PKCS1Padding");
    }
    else if (padding == OaepSHA1)
    {
        loc[algName] = make_java_string(env, "RSA/ECB/OAEPPadding");
    }
    else
    {
        loc[algName] = make_java_string(env, "RSA/ECB/NoPadding");
    }

    loc[cipher] = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, loc[algName]);
    (*env)->CallVoidMethod(env, loc[cipher], g_cipherInit2Method, CIPHER_ENCRYPT_MODE, rsa->publicKey);
    loc[fromBytes] = make_java_byte_array(env, flen);
    (*env)->SetByteArrayRegion(env, loc[fromBytes], 0, flen, (jbyte*)from);
    loc[encryptedBytes] = (jbyteArray)(*env)->CallObjectMethod(env, loc[cipher], g_cipherDoFinal2Method, loc[fromBytes]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    jsize encryptedBytesLen = (*env)->GetArrayLength(env, loc[encryptedBytes]);
    (*env)->GetByteArrayRegion(env, loc[encryptedBytes], 0, encryptedBytesLen, (jbyte*) to);

    ret = (int32_t)encryptedBytesLen;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_RsaPrivateDecrypt(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    if (!rsa)
        return RSA_FAIL;

    if (!rsa->privateKey)
        return RSA_FAIL;

    abort_if_invalid_pointer_argument (to);
    abort_if_invalid_pointer_argument (from);

    JNIEnv* env = GetJNIEnv();

    jobject algName;
    if (padding == Pkcs1)
        algName = make_java_string(env, "RSA/ECB/PKCS1Padding"); // TODO: is ECB needed here?
    else if (padding == OaepSHA1)
        algName = make_java_string(env, "RSA/ECB/OAEPPadding");
    else
        algName = make_java_string(env, "RSA/ECB/NoPadding");

    jobject cipher = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName);
    (*env)->CallVoidMethod(env, cipher, g_cipherInit2Method, CIPHER_DECRYPT_MODE, rsa->privateKey);
    jbyteArray fromBytes = make_java_byte_array(env, flen);
    (*env)->SetByteArrayRegion(env, fromBytes, 0, flen, (jbyte*)from);
    jbyteArray decryptedBytes = (jbyteArray)(*env)->CallObjectMethod(env, cipher, g_cipherDoFinal2Method, fromBytes);

    if (CheckJNIExceptions(env))
    {
        (*env)->DeleteLocalRef(env, cipher);
        (*env)->DeleteLocalRef(env, fromBytes);
        (*env)->DeleteLocalRef(env, algName);
        return RSA_FAIL;
    }

    jsize decryptedBytesLen = (*env)->GetArrayLength(env, decryptedBytes);
    (*env)->GetByteArrayRegion(env, decryptedBytes, 0, decryptedBytesLen, (jbyte*) to);

    (*env)->DeleteLocalRef(env, cipher);
    (*env)->DeleteLocalRef(env, fromBytes);
    (*env)->DeleteLocalRef(env, decryptedBytes);
    (*env)->DeleteLocalRef(env, algName);

    return (int32_t)decryptedBytesLen;
}

int32_t AndroidCryptoNative_RsaSize(RSA* rsa)
{
    if (!rsa)
        return FAIL;
    return rsa->keyWidthInBits / 8;
}

RSA* AndroidCryptoNative_DecodeRsaSubjectPublicKeyInfo(uint8_t* buf, int32_t len)
{
    if (!buf || len <= 0)
    {
        return FAIL;
    }

    JNIEnv* env = GetJNIEnv();

    // KeyFactory keyFactory = KeyFactory.getInstance("RSA");
    // X509EncodedKeySpec x509keySpec = new X509EncodedKeySpec(bytes);
    // PublicKey publicKey = keyFactory.generatePublic(x509keySpec);

    jobject algName = make_java_string(env, "RSA");
    jobject keyFactory = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, algName);
    jbyteArray bytes = make_java_byte_array(env, len);
    (*env)->SetByteArrayRegion(env, bytes, 0, len, (jbyte*)buf);
    jobject x509keySpec = (*env)->NewObject(env, g_X509EncodedKeySpecClass, g_X509EncodedKeySpecCtor, bytes);

    jobject publicKey = (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPublicMethod, x509keySpec);
    (*env)->DeleteLocalRef(env, algName);
    (*env)->DeleteLocalRef(env, keyFactory);
    (*env)->DeleteLocalRef(env, bytes);
    (*env)->DeleteLocalRef(env, x509keySpec);
    if (CheckJNIExceptions(env))
    {
        (*env)->DeleteLocalRef(env, publicKey);
        return FAIL;
    }

    RSA* rsa = AndroidCryptoNative_NewRsaFromKeys(env, publicKey, NULL /*privateKey*/);
    (*env)->DeleteLocalRef(env, publicKey);

    return rsa;
}

int32_t AndroidCryptoNative_RsaSignPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa)
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

    jobject algName = make_java_string(env, "RSA/ECB/NoPadding");

    jobject cipher = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName);
    (*env)->CallVoidMethod(env, cipher, g_cipherInit2Method, CIPHER_ENCRYPT_MODE, rsa->privateKey);
    jbyteArray fromBytes = make_java_byte_array(env, flen);
    (*env)->SetByteArrayRegion(env, fromBytes, 0, flen, (jbyte*)from);
    jbyteArray encryptedBytes = (jbyteArray)(*env)->CallObjectMethod(env, cipher, g_cipherDoFinal2Method, fromBytes);
    if (CheckJNIExceptions(env))
    {
        (*env)->DeleteLocalRef(env, cipher);
        (*env)->DeleteLocalRef(env, fromBytes);
        (*env)->DeleteLocalRef(env, algName);
        return RSA_FAIL;
    }
    jsize encryptedBytesLen = (*env)->GetArrayLength(env, encryptedBytes);
    (*env)->GetByteArrayRegion(env, encryptedBytes, 0, encryptedBytesLen, (jbyte*) to);

    (*env)->DeleteLocalRef(env, cipher);
    (*env)->DeleteLocalRef(env, fromBytes);
    (*env)->DeleteLocalRef(env, encryptedBytes);
    (*env)->DeleteLocalRef(env, algName);

    return (int32_t)encryptedBytesLen;
}

int32_t AndroidCryptoNative_RsaVerificationPrimitive(int32_t flen, uint8_t* from, uint8_t* to, RSA* rsa)
{
    if (!rsa)
        return RSA_FAIL;

    abort_if_invalid_pointer_argument (to);
    abort_if_invalid_pointer_argument (from);

    JNIEnv* env = GetJNIEnv();

    jobject algName = make_java_string(env, "RSA/ECB/NoPadding");

    jobject cipher = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName);
    (*env)->CallVoidMethod(env, cipher, g_cipherInit2Method, CIPHER_DECRYPT_MODE, rsa->publicKey);
    jbyteArray fromBytes = make_java_byte_array(env, flen);
    (*env)->SetByteArrayRegion(env, fromBytes, 0, flen, (jbyte*)from);
    jbyteArray decryptedBytes = (jbyteArray)(*env)->CallObjectMethod(env, cipher, g_cipherDoFinal2Method, fromBytes);
    if (CheckJNIExceptions(env))
    {
        (*env)->DeleteLocalRef(env, cipher);
        (*env)->DeleteLocalRef(env, fromBytes);
        (*env)->DeleteLocalRef(env, decryptedBytes);
        (*env)->DeleteLocalRef(env, algName);
        return FAIL;
    }

    jsize decryptedBytesLen = (*env)->GetArrayLength(env, decryptedBytes);
    (*env)->GetByteArrayRegion(env, decryptedBytes, 0, decryptedBytesLen, (jbyte*) to);

    (*env)->DeleteLocalRef(env, cipher);
    (*env)->DeleteLocalRef(env, fromBytes);
    (*env)->DeleteLocalRef(env, decryptedBytes);
    (*env)->DeleteLocalRef(env, algName);

    return (int32_t)decryptedBytesLen;
}

int32_t AndroidCryptoNative_RsaGenerateKeyEx(RSA* rsa, int32_t bits)
{
    if (!rsa)
        return FAIL;

    // KeyPairGenerator kpg = KeyPairGenerator.getInstance("RSA");
    // kpg.initialize(bits);
    // KeyPair kp = kpg.genKeyPair();

    JNIEnv* env = GetJNIEnv();
    jobject rsaStr = make_java_string(env, "RSA");
    jobject kpgObj = (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, rsaStr);
    (*env)->CallVoidMethod(env, kpgObj, g_keyPairGenInitializeMethod, bits);
    jobject keyPair = (*env)->CallObjectMethod(env, kpgObj, g_keyPairGenGenKeyPairMethod);

    rsa->privateKey = ToGRef(env, (*env)->CallObjectMethod(env, keyPair, g_keyPairGetPrivateMethod));
    rsa->publicKey = ToGRef(env, (*env)->CallObjectMethod(env, keyPair, g_keyPairGetPublicMethod));
    rsa->keyWidthInBits = bits;

    (*env)->DeleteLocalRef(env, rsaStr);
    (*env)->DeleteLocalRef(env, kpgObj);
    (*env)->DeleteLocalRef(env, keyPair);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t AndroidCryptoNative_GetRsaParameters(RSA* rsa, AndroidGetRsaParametersData* parameters)
{
    abort_if_invalid_pointer_argument(rsa);
    abort_if_invalid_pointer_argument(parameters);

    memset(parameters, 0, sizeof(AndroidGetRsaParametersData));

    JNIEnv* env = GetJNIEnv();
    jobject privateKey = rsa->privateKey;
    jobject publicKey = rsa->publicKey;

    if (privateKey)
    {
        parameters->e = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPubExpField));
        parameters->n = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyModulusField));
        parameters->d = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrivExpField));
        parameters->p = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimePField));
        parameters->q = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimeQField));
        parameters->dmp1 = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimeExpPField));
        parameters->dmq1 = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyPrimeExpQField));
        parameters->iqmp = ToGRef(env, (*env)->CallObjectMethod(env, privateKey, g_RSAPrivateCrtKeyCrtCoefField));
    }
    else if (publicKey)
    {
        parameters->e = ToGRef(env, (*env)->CallObjectMethod(env, publicKey, g_RSAPublicKeyGetPubExpMethod));
        parameters->n = ToGRef(env, (*env)->CallObjectMethod(env, publicKey, g_RSAKeyGetModulus));
    }
    else
    {
        return FAIL;
    }

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t AndroidCryptoNative_SetRsaParameters(RSA* rsa, AndroidSetRsaParametersData* parameters)
{
    if (!rsa)
        return FAIL;

    abort_if_invalid_pointer_argument(parameters);

    JNIEnv* env = GetJNIEnv();
    INIT_LOCALS(bn, N, E, D, P, Q, DMP1, DMQ1, IQMP);
    INIT_LOCALS(loc, algName, keyFactory, rsaPubKeySpec, rsaPrivateKeySpec);

    bn[N] = AndroidCryptoNative_BigNumFromBinary(parameters->n, parameters->n_length);
    bn[E] = AndroidCryptoNative_BigNumFromBinary(parameters->e, parameters->e_length);

    rsa->keyWidthInBits = parameters->n_length * 8;

    loc[algName] = make_java_string(env, "RSA");
    loc[keyFactory] = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[algName]);

    if (parameters->d_length > 0)
    {
        // private key section
        bn[D] = AndroidCryptoNative_BigNumFromBinary(parameters->d, parameters->d_length);
        bn[P] = AndroidCryptoNative_BigNumFromBinary(parameters->p, parameters->p_length);
        bn[Q] = AndroidCryptoNative_BigNumFromBinary(parameters->q, parameters->q_length);
        bn[DMP1] = AndroidCryptoNative_BigNumFromBinary(parameters->dmp1, parameters->dmp1_length);
        bn[DMQ1] = AndroidCryptoNative_BigNumFromBinary(parameters->dmq1, parameters->dmq1_length);
        bn[IQMP] = AndroidCryptoNative_BigNumFromBinary(parameters->iqmp, parameters->iqmp_length);

        loc[rsaPrivateKeySpec] = (*env)->NewObject(env, g_RSAPrivateCrtKeySpecClass, g_RSAPrivateCrtKeySpecCtor,
            bn[N], bn[E], bn[D], bn[P], bn[Q], bn[DMP1], bn[DMQ1], bn[IQMP]);

        ReleaseGRef(env, rsa->privateKey);
        rsa->privateKey = ToGRef(env, (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPrivateMethod, loc[rsaPrivateKeySpec]));
    }

    loc[rsaPubKeySpec] = (*env)->NewObject(env, g_RSAPublicCrtKeySpecClass, g_RSAPublicCrtKeySpecCtor, bn[N], bn[E]);

    ReleaseGRef(env, rsa->publicKey);
    rsa->publicKey = ToGRef(env, (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPublicMethod, loc[rsaPubKeySpec]));

    RELEASE_LOCALS(bn, env);
    RELEASE_LOCALS(loc, env);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

RSA* AndroidCryptoNative_NewRsaFromKeys(JNIEnv* env, jobject /*RSAPublicKey*/ publicKey, jobject /*RSAPrivateKey*/ privateKey)
{
    if (!(*env)->IsInstanceOf(env, publicKey, g_RSAPublicKeyClass))
        return NULL;

    jobject modulus = (*env)->CallObjectMethod(env, publicKey, g_RSAKeyGetModulus);

    RSA* ret = AndroidCryptoNative_RsaCreate();
    ret->publicKey = AddGRef(env, publicKey);
    ret->privateKey = AddGRef(env, privateKey);
    ret->keyWidthInBits = AndroidCryptoNative_GetBigNumBytes(modulus) * 8;

    (*env)->DeleteLocalRef(env, modulus);
    return ret;
}
