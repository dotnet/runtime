// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_cipher.h"
#include "pal_utilities.h"

// just some unique IDs
intptr_t AndroidCryptoNative_Aes128Ecb()    { return 1001; }
intptr_t AndroidCryptoNative_Aes128Cbc()    { return 1002; }
intptr_t AndroidCryptoNative_Aes128Cfb8()   { return 1003; }
intptr_t AndroidCryptoNative_Aes128Cfb128() { return 1004; }
intptr_t AndroidCryptoNative_Aes128Gcm()    { return 1005; }
intptr_t AndroidCryptoNative_Aes128Ccm()    { return 1006; }

intptr_t AndroidCryptoNative_Aes192Ecb()    { return 1007; }
intptr_t AndroidCryptoNative_Aes192Cbc()    { return 1008; }
intptr_t AndroidCryptoNative_Aes192Cfb8()   { return 1009; }
intptr_t AndroidCryptoNative_Aes192Cfb128() { return 1010; }
intptr_t AndroidCryptoNative_Aes192Gcm()    { return 1011; }
intptr_t AndroidCryptoNative_Aes192Ccm()    { return 1012; }

intptr_t AndroidCryptoNative_Aes256Ecb()    { return 1013; }
intptr_t AndroidCryptoNative_Aes256Cbc()    { return 1014; }
intptr_t AndroidCryptoNative_Aes256Cfb8()   { return 1015; }
intptr_t AndroidCryptoNative_Aes256Cfb128() { return 1016; }
intptr_t AndroidCryptoNative_Aes256Gcm()    { return 1017; }
intptr_t AndroidCryptoNative_Aes256Ccm()    { return 1018; }

intptr_t AndroidCryptoNative_Des3Ecb()      { return 1019; }
intptr_t AndroidCryptoNative_Des3Cbc()      { return 1020; }
intptr_t AndroidCryptoNative_Des3Cfb8()     { return 1021; }
intptr_t AndroidCryptoNative_Des3Cfb64()    { return 1022; }

intptr_t AndroidCryptoNative_DesEcb()       { return 1023; }
intptr_t AndroidCryptoNative_DesCfb8()      { return 1024; }
intptr_t AndroidCryptoNative_DesCbc()       { return 1025; }

intptr_t AndroidCryptoNative_RC2Ecb()       { return 1026; }
intptr_t AndroidCryptoNative_RC2Cbc()       { return 1027; }

static int32_t GetAlgorithmWidth(intptr_t type)
{
    if (type == AndroidCryptoNative_Aes128Ecb())    return 128;
    if (type == AndroidCryptoNative_Aes128Cbc())    return 128;
    if (type == AndroidCryptoNative_Aes128Gcm())    return 128;
    if (type == AndroidCryptoNative_Aes128Ccm())    return 128;
    if (type == AndroidCryptoNative_Aes128Cfb8())   return 128;
    if (type == AndroidCryptoNative_Aes128Cfb128()) return 128;

    if (type == AndroidCryptoNative_Aes192Ecb())    return 192;
    if (type == AndroidCryptoNative_Aes192Cbc())    return 192;
    if (type == AndroidCryptoNative_Aes192Gcm())    return 192;
    if (type == AndroidCryptoNative_Aes192Ccm())    return 192;
    if (type == AndroidCryptoNative_Aes192Cfb8())   return 192;
    if (type == AndroidCryptoNative_Aes192Cfb128()) return 192;

    if (type == AndroidCryptoNative_Aes256Ecb())    return 256;
    if (type == AndroidCryptoNative_Aes256Cbc())    return 256;
    if (type == AndroidCryptoNative_Aes256Gcm())    return 256;
    if (type == AndroidCryptoNative_Aes256Ccm())    return 256;
    if (type == AndroidCryptoNative_Aes256Cfb8())   return 256;
    if (type == AndroidCryptoNative_Aes256Cfb128()) return 256;

    if (type == AndroidCryptoNative_DesEcb())       return 56;
    if (type == AndroidCryptoNative_DesCfb8())      return 56;
    if (type == AndroidCryptoNative_DesCbc())       return 56;

    if (type == AndroidCryptoNative_Des3Ecb())      return 168;
    if (type == AndroidCryptoNative_Des3Cbc())      return 168;
    if (type == AndroidCryptoNative_Des3Cfb8())     return 168;
    if (type == AndroidCryptoNative_Des3Cfb64())    return 168;

    assert(0 && "unexpected type");
    return FAIL;
}

static jobject GetAlgorithmName(JNIEnv* env, intptr_t type)
{
    if (type == AndroidCryptoNative_Aes128Ecb())    return JSTRING("AES/ECB/NoPadding");
    if (type == AndroidCryptoNative_Aes128Cbc())    return JSTRING("AES/CBC/NoPadding");
    if (type == AndroidCryptoNative_Aes128Gcm())    return JSTRING("AES/GCM/NoPadding");
    if (type == AndroidCryptoNative_Aes128Ccm())    return JSTRING("AES/CCM/NoPadding");
    if (type == AndroidCryptoNative_Aes128Cfb8())   return JSTRING("AES/CFB/NoPadding");

    if (type == AndroidCryptoNative_Aes192Ecb())    return JSTRING("AES/ECB/NoPadding");
    if (type == AndroidCryptoNative_Aes192Cbc())    return JSTRING("AES/CBC/NoPadding");
    if (type == AndroidCryptoNative_Aes192Gcm())    return JSTRING("AES/GCM/NoPadding");
    if (type == AndroidCryptoNative_Aes192Ccm())    return JSTRING("AES/CCM/NoPadding");
    if (type == AndroidCryptoNative_Aes192Cfb8())   return JSTRING("AES/CFB/NoPadding");

    if (type == AndroidCryptoNative_Aes256Ecb())    return JSTRING("AES/ECB/NoPadding");
    if (type == AndroidCryptoNative_Aes256Cbc())    return JSTRING("AES/CBC/NoPadding");
    if (type == AndroidCryptoNative_Aes256Gcm())    return JSTRING("AES/GCM/NoPadding");
    if (type == AndroidCryptoNative_Aes256Ccm())    return JSTRING("AES/CCM/NoPadding");
    if (type == AndroidCryptoNative_Aes256Cfb8())   return JSTRING("AES/CFB/NoPadding");

    if (type == AndroidCryptoNative_DesEcb())       return JSTRING("DES/ECB/NoPadding");
    if (type == AndroidCryptoNative_DesCfb8())      return JSTRING("DES/CFB/NoPadding");
    if (type == AndroidCryptoNative_DesCbc())       return JSTRING("DES/CBC/NoPadding");

    if (type == AndroidCryptoNative_Des3Ecb())      return JSTRING("DESede/ECB/NoPadding");
    if (type == AndroidCryptoNative_Des3Cbc())      return JSTRING("DESede/CBC/NoPadding");
    if (type == AndroidCryptoNative_Des3Cfb8())     return JSTRING("DESede/CFB/NoPadding");
    if (type == AndroidCryptoNative_Des3Cfb64())    return JSTRING("DESede/CFB/NoPadding");

    if (type == AndroidCryptoNative_Aes128Cfb128()) return JSTRING("AES/CFB128/NoPadding");
    if (type == AndroidCryptoNative_Aes192Cfb128()) return JSTRING("AES/CFB128/NoPadding");
    if (type == AndroidCryptoNative_Aes256Cfb128()) return JSTRING("AES/CFB128/NoPadding");

    LOG_ERROR("This algorithm (%ld) is not supported", (long)type);
    return FAIL;
}

static bool HasTag(intptr_t type)
{
    return (type == AndroidCryptoNative_Aes128Gcm()) ||
           (type == AndroidCryptoNative_Aes128Ccm()) ||
           (type == AndroidCryptoNative_Aes192Gcm()) ||
           (type == AndroidCryptoNative_Aes192Ccm()) ||
           (type == AndroidCryptoNative_Aes256Gcm()) ||
           (type == AndroidCryptoNative_Aes256Ccm());
}

CipherCtx* AndroidCryptoNative_CipherCreatePartial(intptr_t type)
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
    ctx->tagLength = TAG_MAX_LENGTH;
    ctx->ivLength = 0;
    ctx->encMode = 0;
    ctx->key = NULL;
    ctx->iv = NULL;
    return CheckJNIExceptions(env) ? FAIL : ctx;
}

int32_t AndroidCryptoNative_CipherSetTagLength(CipherCtx* ctx, int32_t tagLength)
{
    if (!ctx)
        return FAIL;
    
    if(tagLength > TAG_MAX_LENGTH)
        return FAIL;

    ctx->tagLength = tagLength;
    return SUCCESS;
}

static int32_t ReinitializeCipher(CipherCtx* ctx)
{
    JNIEnv* env = GetJNIEnv();
    
    int32_t keyLength = GetAlgorithmWidth(ctx->type);

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
        ivPsObj = (*env)->NewObject(env, g_GCMParameterSpecClass, g_GCMParameterSpecCtor, ctx->tagLength * 8, ivBytes);
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

int32_t AndroidCryptoNative_CipherSetKeyAndIV(CipherCtx* ctx, uint8_t* key, uint8_t* iv, int32_t enc)
{
    if (!ctx)
        return FAIL;

    int32_t keyLength = GetAlgorithmWidth(ctx->type);
    
    // input:  0 for Decrypt, 1 for Encrypt, -1 leave untouched
    // Cipher: 2 for Decrypt, 1 for Encrypt, N/A
    if (enc != -1)
    {
        assert(enc == 0 || enc == 1);
        ctx->encMode = enc == 0 ? CIPHER_DECRYPT_MODE : CIPHER_ENCRYPT_MODE;
    }

    // CryptoNative_CipherSetKeyAndIV can be called separately for key and iv
    // so we need to wait for both and do Init after.
    if (key)
        SaveTo(key, &ctx->key, (size_t)keyLength, /* overwrite */ true);
    if (iv)
        SaveTo(iv, &ctx->iv, (size_t)ctx->ivLength, /* overwrite */ true);

    if (!ctx->key || !ctx->iv)
        return SUCCESS;

    return ReinitializeCipher(ctx);
}

CipherCtx* AndroidCryptoNative_CipherCreate2(intptr_t type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, uint8_t* iv, int32_t enc)
{
    if (effectiveKeyLength != 0)
    {
        LOG_ERROR("Non-zero effectiveKeyLength is not supported");
        return FAIL;
    }

    CipherCtx* ctx = AndroidCryptoNative_CipherCreatePartial(type);
    if (AndroidCryptoNative_CipherSetKeyAndIV(ctx, key, iv, enc) != SUCCESS)
        return FAIL;
    
    if (keyLength != GetAlgorithmWidth(type))
    {
        LOG_ERROR("Key length must match algorithm width.");
        return FAIL;
    }
    return ctx;
}

int32_t AndroidCryptoNative_CipherUpdateAAD(CipherCtx* ctx, uint8_t* in, int32_t inl)
{
    if (!ctx)
        return FAIL;
        
    JNIEnv* env = GetJNIEnv();
    jbyteArray inDataBytes = (*env)->NewByteArray(env, inl);
    (*env)->SetByteArrayRegion(env, inDataBytes, 0, inl, (jbyte*)in);
    (*env)->CallVoidMethod(env, ctx->cipher, g_cipherUpdateAADMethod, inDataBytes);
    (*env)->DeleteLocalRef(env, inDataBytes);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t AndroidCryptoNative_CipherUpdate(CipherCtx* ctx, uint8_t* outm, int32_t* outl, uint8_t* in, int32_t inl)
{
    if (!ctx)
        return FAIL;

    if (!outl && !in)
        // it means caller wants us to record "inl" but we don't need it.
        return SUCCESS;

    JNIEnv* env = GetJNIEnv();
    jbyteArray inDataBytes = (*env)->NewByteArray(env, inl);
    (*env)->SetByteArrayRegion(env, inDataBytes, 0, inl, (jbyte*)in);

    *outl = 0;
    jbyteArray outDataBytes = (jbyteArray)(*env)->CallObjectMethod(env, ctx->cipher, g_cipherUpdateMethod, inDataBytes);

    if (outDataBytes && outm)
    {
        jsize outDataBytesLen = (*env)->GetArrayLength(env, outDataBytes);
        *outl = outDataBytesLen;
        (*env)->GetByteArrayRegion(env, outDataBytes, 0, outDataBytesLen, (jbyte*) outm);
        (*env)->DeleteLocalRef(env, outDataBytes);
    }

    (*env)->DeleteLocalRef(env, inDataBytes);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t AndroidCryptoNative_CipherFinalEx(CipherCtx* ctx, uint8_t* outm, int32_t* outl)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJNIEnv();

    *outl = 0;

    jbyteArray outBytes = (jbyteArray)(*env)->CallObjectMethod(env, ctx->cipher, g_cipherDoFinalMethod);
    if (CheckJNIExceptions(env)) return FAIL;

    jsize outBytesLen = (*env)->GetArrayLength(env, outBytes);
    *outl = outBytesLen;
    (*env)->GetByteArrayRegion(env, outBytes, 0, outBytesLen, (jbyte*) outm);

    (*env)->DeleteLocalRef(env, outBytes);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t AndroidCryptoNative_CipherCtxSetPadding(CipherCtx* ctx, int32_t padding)
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

int32_t AndroidCryptoNative_CipherReset(CipherCtx* ctx)
{
    if (!ctx)
        return FAIL;
    
    free(ctx->iv);
    ctx->iv = NULL;
    ctx->ivLength = 0;
    
    JNIEnv* env = GetJNIEnv();
    ReleaseGRef(env, ctx->cipher);
    jobject algName = GetAlgorithmName(env, ctx->type);
    if (!algName)
        return FAIL;

    ctx->cipher = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName));
    (*env)->DeleteLocalRef(env, algName);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t AndroidCryptoNative_CipherSetNonceLength(CipherCtx* ctx, int32_t ivLength)
{
    if (!ctx)
        return FAIL;

    ctx->ivLength = ivLength;
    return SUCCESS;
}

void AndroidCryptoNative_CipherDestroy(CipherCtx* ctx)
{
    if (ctx)
    {
        JNIEnv* env = GetJNIEnv();
        ReleaseGRef(env, ctx->cipher);
        free(ctx->key);
        free(ctx->iv);
        free(ctx);
    }
}
