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

static int32_t GetAlgorithmWidth(intptr_t type)
{
    if (type == CryptoNative_EvpAes128Ecb())    return 128;
    if (type == CryptoNative_EvpAes128Cbc())    return 128;
    if (type == CryptoNative_EvpAes128Gcm())    return 128;
    if (type == CryptoNative_EvpAes128Ccm())    return 128;
    if (type == CryptoNative_EvpAes128Cfb8())   return 128;
    if (type == CryptoNative_EvpAes128Cfb128()) return 128;

    if (type == CryptoNative_EvpAes192Ecb())    return 192;
    if (type == CryptoNative_EvpAes192Cbc())    return 192;
    if (type == CryptoNative_EvpAes192Gcm())    return 192;
    if (type == CryptoNative_EvpAes192Ccm())    return 192;
    if (type == CryptoNative_EvpAes192Cfb8())   return 192;
    if (type == CryptoNative_EvpAes192Cfb128()) return 192;

    if (type == CryptoNative_EvpAes256Ecb())    return 256;
    if (type == CryptoNative_EvpAes256Cbc())    return 256;
    if (type == CryptoNative_EvpAes256Gcm())    return 256;
    if (type == CryptoNative_EvpAes256Ccm())    return 256;
    if (type == CryptoNative_EvpAes256Cfb8())   return 256;
    if (type == CryptoNative_EvpAes256Cfb128()) return 256;

    if (type == CryptoNative_EvpDesEcb())       return 56;
    if (type == CryptoNative_EvpDesCfb8())      return 56;
    if (type == CryptoNative_EvpDesCbc())       return 56;

    if (type == CryptoNative_EvpDes3Ecb())      return 168;
    if (type == CryptoNative_EvpDes3Cbc())      return 168;
    if (type == CryptoNative_EvpDes3Cfb8())     return 168;
    if (type == CryptoNative_EvpDes3Cfb64())    return 168;

    assert(0 && "unexpected type");
    return FAIL;
}

static jobject GetAlgorithmName(JNIEnv* env, intptr_t type)
{
    if (type == CryptoNative_EvpAes128Ecb())    return JSTRING("AES/ECB/NoPadding");
    if (type == CryptoNative_EvpAes128Cbc())    return JSTRING("AES/CBC/NoPadding");
    if (type == CryptoNative_EvpAes128Gcm())    return JSTRING("AES/GCM/NoPadding");
    if (type == CryptoNative_EvpAes128Ccm())    return JSTRING("AES/CCM/NoPadding");
    if (type == CryptoNative_EvpAes128Cfb8())   return JSTRING("AES/CFB/NoPadding");

    if (type == CryptoNative_EvpAes192Ecb())    return JSTRING("AES/ECB/NoPadding");
    if (type == CryptoNative_EvpAes192Cbc())    return JSTRING("AES/CBC/NoPadding");
    if (type == CryptoNative_EvpAes192Gcm())    return JSTRING("AES/GCM/NoPadding");
    if (type == CryptoNative_EvpAes192Ccm())    return JSTRING("AES/CCM/NoPadding");
    if (type == CryptoNative_EvpAes192Cfb8())   return JSTRING("AES/CFB/NoPadding");

    if (type == CryptoNative_EvpAes256Ecb())    return JSTRING("AES/ECB/NoPadding");
    if (type == CryptoNative_EvpAes256Cbc())    return JSTRING("AES/CBC/NoPadding");
    if (type == CryptoNative_EvpAes256Gcm())    return JSTRING("AES/GCM/NoPadding");
    if (type == CryptoNative_EvpAes256Ccm())    return JSTRING("AES/CCM/NoPadding");
    if (type == CryptoNative_EvpAes256Cfb8())   return JSTRING("AES/CFB/NoPadding");

    if (type == CryptoNative_EvpDesEcb())       return JSTRING("DES/ECB/NoPadding");
    if (type == CryptoNative_EvpDesCfb8())      return JSTRING("DES/CFB/NoPadding");
    if (type == CryptoNative_EvpDesCbc())       return JSTRING("DES/CBC/NoPadding");

    if (type == CryptoNative_EvpDes3Ecb())      return JSTRING("DESede/ECB/NoPadding");
    if (type == CryptoNative_EvpDes3Cbc())      return JSTRING("DESede/CBC/NoPadding");
    if (type == CryptoNative_EvpDes3Cfb8())     return JSTRING("DESede/CFB/NoPadding");
    if (type == CryptoNative_EvpDes3Cfb64())    return JSTRING("DESede/CFB/NoPadding");

    // custom feedback size is not supported yet:
    //if (type == CryptoNative_EvpAes128Cfb128()) return JSTRING("AES/CFB/NoPadding");
    //if (type == CryptoNative_EvpAes192Cfb128()) return JSTRING("AES/CFB/NoPadding");
    //if (type == CryptoNative_EvpAes256Cfb128()) return JSTRING("AES/CFB/NoPadding");

    LOG_ERROR("This algorithm (%ld) is not supported", type);
    return FAIL;
}

static bool HasTag(intptr_t type)
{
    return (type == CryptoNative_EvpAes128Gcm()) ||
           (type == CryptoNative_EvpAes128Ccm()) ||
           (type == CryptoNative_EvpAes192Gcm()) ||
           (type == CryptoNative_EvpAes192Ccm()) ||
           (type == CryptoNative_EvpAes256Gcm()) ||
           (type == CryptoNative_EvpAes256Ccm());
}

CipherCtx* CryptoNative_EvpCipherCreatePartial(intptr_t type)
{
    JNIEnv* env = GetJNIEnv();
    jobject algName = GetAlgorithmName(env, type);
    if (!algName)
        return FAIL;

    jobject cipher = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName));
    (*env)->DeleteLocalRef(env, algName);

    CipherCtx* ctx = malloc(sizeof(CipherCtx));
    ctx->cipher = cipher;
    ctx->type = type;
    ctx->ivLength = 0;
    ctx->encMode = 0;
    ctx->key = NULL;
    ctx->iv = NULL;
    memset(ctx->tag, 0, TAG_MAX_LENGTH);
    return CheckJNIExceptions(env) ? FAIL : ctx;
}

int32_t CryptoNative_EvpCipherSetKeyAndIV(CipherCtx* ctx, uint8_t* key, uint8_t* iv, int32_t enc)
{
    if (!ctx)
        return FAIL;

    int32_t keyLength = GetAlgorithmWidth(ctx->type);

    // CryptoNative_EvpCipherSetKeyAndIV can be called separately for key and iv
    // so we need to wait for both and do Init after.
    if (key)
        SaveTo(key, &ctx->key, (size_t)keyLength);
    if (iv)
        SaveTo(iv, &ctx->iv, (size_t)ctx->ivLength);

    if (!ctx->key || !ctx->iv)
        return SUCCESS;

    // input:  0 for Decrypt, 1 for Encrypt
    // Cipher: 2 for Decrypt, 1 for Encrypt
    assert(enc == 0 || enc == 1);
    ctx->encMode = enc == 0 ? CIPHER_DECRYPT_MODE : CIPHER_ENCRYPT_MODE;

    JNIEnv* env = GetJNIEnv();

    // int ivSize = cipher.getBlockSize();
    // SecretKeySpec keySpec = new SecretKeySpec(key.getEncoded(), "AES");
    // IvParameterSpec ivSpec = new IvParameterSpec(IV); or GCMParameterSpec for GCM/CCM
    // cipher.init(encMode, keySpec, ivSpec);

    jobject algName = GetAlgorithmName(env, ctx->type);
    if (!algName)
        return FAIL;

    if (!ctx->ivLength)
        ctx->ivLength = (*env)->CallIntMethod(env, ctx->cipher, g_getBlockSizeMethod);

    jbyteArray keyBytes = (*env)->NewByteArray(env, keyLength / 8); // bits to bytes, e.g. 256 -> 32
    (*env)->SetByteArrayRegion(env, keyBytes, 0, keyLength / 8, (jbyte*)ctx->key);
    jbyteArray ivBytes = (*env)->NewByteArray(env, ctx->ivLength);
    (*env)->SetByteArrayRegion(env, ivBytes, 0, ctx->ivLength, (jbyte*)ctx->iv);

    jobject sksObj = (*env)->NewObject(env, g_sksClass, g_sksCtor, keyBytes, algName);
    jobject ivPsObj;

    if (HasTag(ctx->type))
        ivPsObj = (*env)->NewObject(env, g_GCMParameterSpecClass, g_GCMParameterSpecCtor, TAG_MAX_LENGTH * 8, ivBytes);
    else
        ivPsObj = (*env)->NewObject(env, g_ivPsClass, g_ivPsCtor, ivBytes);

    (*env)->CallVoidMethod(env, ctx->cipher, g_cipherInitMethod, ctx->encMode, sksObj, ivPsObj);
    (*env)->DeleteLocalRef(env, algName);
    (*env)->DeleteLocalRef(env, sksObj);
    (*env)->DeleteLocalRef(env, ivPsObj);
    (*env)->DeleteLocalRef(env, keyBytes);
    (*env)->DeleteLocalRef(env, ivBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

CipherCtx* CryptoNative_EvpCipherCreate2(intptr_t type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, uint8_t* iv, int32_t enc)
{
    if (effectiveKeyLength != 0)
    {
        LOG_ERROR("Non-zero effectiveKeyLength is not supported");
        return FAIL;
    }

    CipherCtx* ctx = CryptoNative_EvpCipherCreatePartial(type);
    if (CryptoNative_EvpCipherSetKeyAndIV(ctx, key, iv, enc) != SUCCESS)
        return FAIL;
    
    assert(keyLength == GetAlgorithmWidth(type));
    return ctx;
}

int32_t CryptoNative_EvpCipherUpdate(CipherCtx* ctx, uint8_t* outm, int32_t* outl, uint8_t* in, int32_t inl)
{
    if (!ctx)
        return FAIL;

    if (!outl && !in)
        // it means caller wants us to record "inl" but we don't need it.
        return SUCCESS;

    JNIEnv* env = GetJNIEnv();
    jbyteArray inDataBytes = (*env)->NewByteArray(env, inl);
    (*env)->SetByteArrayRegion(env, inDataBytes, 0, inl, (jbyte*)in);

    // it's AAD if outm is null and it's GCM/CCM
    if (HasTag(ctx->type) && !outm)
    {
        (*env)->CallVoidMethod(env, ctx->cipher, g_cipherUpdateAADMethod, inDataBytes);
    }
    else
    {
        jbyteArray outDataBytes = (jbyteArray)(*env)->CallObjectMethod(env, ctx->cipher, g_cipherUpdateMethod, inDataBytes);
        if (outDataBytes && outm) {
            jsize outDataBytesLen = (*env)->GetArrayLength(env, outDataBytes);
            *outl = (int32_t)outDataBytesLen;
            (*env)->GetByteArrayRegion(env, outDataBytes, 0, outDataBytesLen, (jbyte*) outm);
            (*env)->DeleteLocalRef(env, outDataBytes);
        } else {
            *outl = 0;
        }
    }

    (*env)->DeleteLocalRef(env, inDataBytes);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_EvpCipherFinalEx(CipherCtx* ctx, uint8_t* outm, int32_t* outl)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJNIEnv();

    // NOTE: Cipher appends TAG to the end of outBytes in case of CCM/GCM and "encryption" mode
    bool hasTag = HasTag(ctx->type);
    bool decrypt = ctx->encMode == CIPHER_DECRYPT_MODE;
    int tagLength = (hasTag && !decrypt) ? TAG_MAX_LENGTH : 0;

    jbyteArray outBytes = (jbyteArray)(*env)->CallObjectMethod(env, ctx->cipher, g_cipherDoFinalMethod);
    jsize outBytesLen = (*env)->GetArrayLength(env, outBytes);

    if (outBytesLen > tagLength)
    {
        (*env)->GetByteArrayRegion(env, outBytes, 0, outBytesLen - tagLength, (jbyte*) outm);
        *outl = outBytesLen - tagLength;

        if (hasTag && !decrypt)
            (*env)->GetByteArrayRegion(env, outBytes, outBytesLen - TAG_MAX_LENGTH, TAG_MAX_LENGTH, (jbyte*) ctx->tag);
    }
    else
    {
        *outl = 0;
    }

    (*env)->DeleteLocalRef(env, outBytes);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_EvpCipherCtxSetPadding(CipherCtx* ctx, int32_t padding)
{
    if (!ctx)
        return FAIL;

    if (padding == 0)
    {
        return SUCCESS;
    }
    else
    {
        // TODO: re-init ctx->cipher ?
        LOG_ERROR("Non-zero padding (%d) is not supported yet", (int)padding);
        return FAIL;
    }
}

int32_t CryptoNative_EvpCipherReset(CipherCtx* ctx)
{
    // TODO: re-init ctx->cipher ?
    LOG_ERROR("EvpCipherReset is no-op.");

    if (!ctx)
        return FAIL;

    return SUCCESS;
}

int32_t CryptoNative_EvpCipherSetGcmNonceLength(CipherCtx* ctx, int32_t ivLength)
{
    if (!ctx)
        return FAIL;

    ctx->ivLength = ivLength;
    return SUCCESS;
}

int32_t CryptoNative_EvpCipherSetCcmNonceLength(CipherCtx* ctx, int32_t ivLength)
{
    return CryptoNative_EvpCipherSetGcmNonceLength(ctx, ivLength);
}

int32_t CryptoNative_EvpCipherGetGcmTag(CipherCtx* ctx, uint8_t* tag, int32_t tagLength)
{
    if (!ctx)
        return FAIL;

    assert(tagLength <= TAG_MAX_LENGTH);

    // Just return what we extracted during CryptoNative_EvpCipherFinalEx
    memcpy(tag, ctx->tag, (size_t)tagLength);
    return SUCCESS;
}

int32_t CryptoNative_EvpCipherGetCcmTag(CipherCtx* ctx, uint8_t* tag, int32_t tagLength)
{
    return CryptoNative_EvpCipherGetGcmTag(ctx, tag, tagLength);
}

int32_t CryptoNative_EvpCipherSetGcmTag(CipherCtx* ctx, uint8_t* tag, int32_t tagLength)
{
    if (!ctx)
        return FAIL;

    assert(tagLength <= TAG_MAX_LENGTH);

    // Tag is provided using regular "cipher.update(tag)"
    int32_t outl = 0;
    uint8_t outd[1];
    CryptoNative_EvpCipherUpdate(ctx, outd, &outl, tag, tagLength);
    return SUCCESS;
}

int32_t CryptoNative_EvpCipherSetCcmTag(CipherCtx* ctx, uint8_t* tag, int32_t tagLength)
{
    return CryptoNative_EvpCipherSetGcmTag(ctx, tag, tagLength);
}

void CryptoNative_EvpCipherDestroy(CipherCtx* ctx)
{
    if (ctx)
    {
        ReleaseGRef(GetJNIEnv(), ctx->cipher);
        free(ctx->key);
        free(ctx->iv);
        free(ctx);
    }
}
