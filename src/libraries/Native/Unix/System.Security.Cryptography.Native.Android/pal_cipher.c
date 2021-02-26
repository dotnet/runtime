// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_cipher.h"
#include "pal_utilities.h"

typedef struct CipherInfo
{
    bool isSupported;
    bool hasTag;
    int32_t width;
    const char* name;
} CipherInfo;

#define DEFINE_CIPHER(cipherId, width, javaName, hasTag) \
CipherInfo* AndroidCryptoNative_ ## cipherId() \
{ \
    static CipherInfo info = { true, hasTag, width, javaName }; \
    return &info; \
}

#define DEFINE_UNSUPPORTED_CIPHER(cipherId) \
CipherInfo* AndroidCryptoNative_ ## cipherId() \
{ \
    static CipherInfo info = { false, false, 0, NULL }; \
    return &info; \
}

DEFINE_CIPHER(Aes128Ecb,    128, "AES/ECB/NoPadding", false)
DEFINE_CIPHER(Aes128Cbc,    128, "AES/CBC/NoPadding", false)
DEFINE_CIPHER(Aes128Cfb8,   128, "AES/CFB/NoPadding", false)
DEFINE_CIPHER(Aes128Cfb128, 128, "AES/CFB128/NoPadding", false)
DEFINE_CIPHER(Aes128Gcm,    128, "AES/GCM/NoPadding", true)
DEFINE_CIPHER(Aes128Ccm,    128, "AES/CCM/NoPadding", true)
DEFINE_CIPHER(Aes192Ecb,    192, "AES/ECB/NoPadding", false)
DEFINE_CIPHER(Aes192Cbc,    192, "AES/CBC/NoPadding", false)
DEFINE_CIPHER(Aes192Cfb8,   192, "AES/CFB/NoPadding", false)
DEFINE_CIPHER(Aes192Cfb128, 192, "AES/CFB128/NoPadding", false)
DEFINE_CIPHER(Aes192Gcm,    192, "AES/GCM/NoPadding", true)
DEFINE_CIPHER(Aes192Ccm,    192, "AES/CCM/NoPadding", true)
DEFINE_CIPHER(Aes256Ecb,    256, "AES/ECB/NoPadding", false)
DEFINE_CIPHER(Aes256Cbc,    256, "AES/CBC/NoPadding", false)
DEFINE_CIPHER(Aes256Cfb8,   256, "AES/CFB/NoPadding", false)
DEFINE_CIPHER(Aes256Cfb128, 256, "AES/CFB128/NoPadding", false)
DEFINE_CIPHER(Aes256Gcm,    256, "AES/GCM/NoPadding", true)
DEFINE_CIPHER(Aes256Ccm,    256, "AES/CCM/NoPadding", true)
DEFINE_CIPHER(DesEcb,       56,  "DES/ECB/NoPadding", false)
DEFINE_CIPHER(DesCbc,       56,  "DES/CBC/NoPadding", false)
DEFINE_CIPHER(DesCfb8,      56,  "DES/CFB/NoPadding", false)
DEFINE_CIPHER(Des3Ecb,      168, "DESede/ECB/NoPadding", false)
DEFINE_CIPHER(Des3Cbc,      168, "DESede/CBC/NoPadding", false)
DEFINE_CIPHER(Des3Cfb8,     168, "DESede/CFB/NoPadding", false)
DEFINE_CIPHER(Des3Cfb64,    168, "DESede/CFB/NoPadding", false)
DEFINE_UNSUPPORTED_CIPHER(RC2Ecb)
DEFINE_UNSUPPORTED_CIPHER(RC2Cbc)


static int32_t GetAlgorithmWidth(CipherInfo* type)
{
    if (!type->isSupported)
    {
        assert(false);
        return FAIL;
    }
    return type->width;
}

static jobject GetAlgorithmName(JNIEnv* env, CipherInfo* type)
{
    if (!type->isSupported)
    {
        LOG_ERROR("This cipher is not supported");
        assert(false);
        return FAIL;
    }
    return JSTRING(type->name);
}

static bool HasTag(CipherInfo* type)
{
    return type->hasTag;
}

CipherCtx* AndroidCryptoNative_CipherCreatePartial(CipherInfo* type)
{
    JNIEnv* env = GetJNIEnv();
    jobject algName = GetAlgorithmName(env, type);
    if (!algName)
        return FAIL;

    jobject cipher = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName));
    (*env)->DeleteLocalRef(env, algName);

    if (CheckJNIExceptions(env))
    {
        return FAIL;
    }

    CipherCtx* ctx = malloc(sizeof(CipherCtx));
    ctx->cipher = cipher;
    ctx->type = type;
    ctx->tagLength = TAG_MAX_LENGTH;
    ctx->ivLength = 0;
    ctx->encMode = 0;
    ctx->key = NULL;
    ctx->iv = NULL;
    return ctx;
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

CipherCtx* AndroidCryptoNative_CipherCreate(CipherInfo* type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, uint8_t* iv, int32_t enc)
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
