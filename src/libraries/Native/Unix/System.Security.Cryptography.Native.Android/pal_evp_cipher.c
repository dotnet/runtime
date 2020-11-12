// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_cipher.h"

// just some unique IDs
intptr_t CryptoNative_EvpAes128Ecb()    { return 1001; }
intptr_t CryptoNative_EvpAes128Cbc()    { return 1002; }
intptr_t CryptoNative_EvpAes128Cfb8()   { return 1003; }
intptr_t CryptoNative_EvpAes128Cfb128() { return 1004; }
intptr_t CryptoNative_EvpAes128Gcm()    { return 1005; }
intptr_t CryptoNative_EvpAes128Ccm()    { return 1006; }
intptr_t CryptoNative_EvpAes192Ecb()    { return 1007; }
intptr_t CryptoNative_EvpAes192Cbc()    { return 1008; }
intptr_t CryptoNative_EvpAes192Cfb8()   { return 1009; }
intptr_t CryptoNative_EvpAes192Cfb128() { return 1010; }
intptr_t CryptoNative_EvpAes192Gcm()    { return 1011; }
intptr_t CryptoNative_EvpAes192Ccm()    { return 1012; }
intptr_t CryptoNative_EvpAes256Ecb()    { return 1013; }
intptr_t CryptoNative_EvpAes256Cbc()    { return 1014; }
intptr_t CryptoNative_EvpAes256Cfb8()   { return 1015; }
intptr_t CryptoNative_EvpAes256Cfb128() { return 1016; }
intptr_t CryptoNative_EvpAes256Gcm()    { return 1017; }
intptr_t CryptoNative_EvpAes256Ccm()    { return 1018; }
intptr_t CryptoNative_EvpDes3Ecb()      { return 1019; }
intptr_t CryptoNative_EvpDes3Cbc()      { return 1020; }
intptr_t CryptoNative_EvpDes3Cfb8()     { return 1021; }
intptr_t CryptoNative_EvpDes3Cfb64()    { return 1022; }
intptr_t CryptoNative_EvpDesEcb()       { return 1023; }
intptr_t CryptoNative_EvpDesCfb8()      { return 1024; }
intptr_t CryptoNative_EvpDesCbc()       { return 1025; }
intptr_t CryptoNative_EvpRC2Ecb()       { return 1026; }
intptr_t CryptoNative_EvpRC2Cbc()       { return 1027; }

jobject CryptoNative_EvpCipherCreate2(intptr_t type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, uint8_t* iv, int32_t enc)
{
    if (effectiveKeyLength != 0)
    {
        LOG_ERROR("Non-zero effectiveKeyLength is not supported");
        return FAIL;
    }

    // input:  0 for Decrypt, 1 for Encrypt
    // Cipher: 2 for Decrypt, 1 for Encrypt
    assert(enc == 0 || enc == 1);
    int encMode = enc == 0 ? 2 : 1;

    JNIEnv* env = GetJNIEnv();
    // Cipher cipher = Cipher.getInstance("AES");
    // int ivSize = cipher.getBlockSize();
    // SecretKeySpec keySpec = new SecretKeySpec(key.getEncoded(), "AES");
    // IvParameterSpec ivSpec = new IvParameterSpec(IV);
    // cipher.init(encMode, keySpec, ivSpec);

    jobject algName = NULL;

    if ((type == CryptoNative_EvpAes128Cbc()) ||
        (type == CryptoNative_EvpAes192Cbc()) ||
        (type == CryptoNative_EvpAes256Cbc()))
    {
        algName = JSTRING("AES/CBC/NoPadding");
    }
    else if (
        (type == CryptoNative_EvpAes128Ecb()) ||
        (type == CryptoNative_EvpAes192Ecb()) ||
        (type == CryptoNative_EvpAes256Ecb()))
    {
        algName = JSTRING("AES/ECB/NoPadding");
    }
    else if (
        (type == CryptoNative_EvpAes128Cfb8()) ||
        (type == CryptoNative_EvpAes192Cfb8()) ||
        (type == CryptoNative_EvpAes256Cfb8()))
    {
        algName = JSTRING("AES/CFB/NoPadding");
    }
    else
    {
        LOG_ERROR("unexpected type: %d", (int)type);
        return FAIL;
    }

    jobject cipherObj = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName));

    int blockSize = (*env)->CallIntMethod(env, cipherObj, g_getBlockSizeMethod);
    jbyteArray keyBytes = (*env)->NewByteArray(env, keyLength / 8); // bits to bytes, e.g. 256 -> 32
    (*env)->SetByteArrayRegion(env, keyBytes, 0, keyLength / 8, (jbyte*)key);
    jbyteArray ivBytes = (*env)->NewByteArray(env, blockSize);
    (*env)->SetByteArrayRegion(env, ivBytes, 0, blockSize, (jbyte*)iv);

    jobject sksObj = (*env)->NewObject(env, g_sksClass, g_sksCtor, keyBytes, algName);
    jobject ivPsObj = (*env)->NewObject(env, g_ivPsClass, g_ivPsCtor, ivBytes);
    (*env)->CallVoidMethod(env, cipherObj, g_cipherInitMethod, encMode, sksObj, ivPsObj);

    (*env)->DeleteLocalRef(env, algName);
    (*env)->DeleteLocalRef(env, sksObj);
    (*env)->DeleteLocalRef(env, ivPsObj);
    (*env)->DeleteLocalRef(env, keyBytes);
    (*env)->DeleteLocalRef(env, ivBytes);

    return CheckJNIExceptions(env) ? FAIL : cipherObj;
}

int32_t CryptoNative_EvpCipherUpdate(jobject ctx, uint8_t* outm, int32_t* outl, uint8_t* in, int32_t inl)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJNIEnv();
    jbyteArray inDataBytes = (*env)->NewByteArray(env, inl);
    (*env)->SetByteArrayRegion(env, inDataBytes, 0, inl, (jbyte*)in);
    jbyteArray outDataBytes = (jbyteArray)(*env)->CallObjectMethod(env, ctx, g_cipherUpdateMethod, inDataBytes);
    if (outDataBytes) {
        jsize outDataBytesLen = (*env)->GetArrayLength(env, outDataBytes);
        *outl = (int32_t)outDataBytesLen;
        (*env)->GetByteArrayRegion(env, outDataBytes, 0, outDataBytesLen, (jbyte*) outm);
        (*env)->DeleteLocalRef(env, outDataBytes);
    } else {
        *outl = 0;
    }

    (*env)->DeleteLocalRef(env, inDataBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_EvpCipherFinalEx(jobject ctx, uint8_t* outm, int32_t* outl)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJNIEnv();

    int blockSize = (*env)->CallIntMethod(env, ctx, g_getBlockSizeMethod);
    jbyteArray outBytes = (*env)->NewByteArray(env, blockSize);
    int written = (*env)->CallIntMethod(env, ctx, g_cipherDoFinalMethod, outBytes, 0 /*offset*/);
    if (written > 0)
        (*env)->GetByteArrayRegion(env, outBytes, 0, blockSize, (jbyte*) outm);
    *outl = written;
    (*env)->DeleteLocalRef(env, outBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_EvpCipherCtxSetPadding(jobject ctx, int32_t padding)
{
    if (padding == 0)
    {
        return SUCCESS;
    }
    else
    {
        LOG_ERROR("Non-zero padding (%d) is not supported", (int)padding);
        return FAIL;
    }
}

int32_t CryptoNative_EvpCipherReset(jobject ctx)
{
    // there is no "reset()" API for an existing Cipher object
    return SUCCESS;
}

void CryptoNative_EvpCipherDestroy(jobject ctx)
{
    ReleaseGRef(GetJNIEnv(), ctx);
}
