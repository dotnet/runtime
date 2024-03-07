// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_cipher.h"
#include "pal_utilities.h"

enum
{
    CIPHER_NONE = 0,
    CIPHER_HAS_VARIABLE_TAG = 1,
    CIPHER_REQUIRES_IV = 2,
};
typedef uint32_t CipherFlags;

typedef struct CipherInfo
{
    CipherFlags flags;
    int32_t width;
    const char* name;
} CipherInfo;

#define DEFINE_CIPHER(cipherId, width, javaName, flags) \
CipherInfo* AndroidCryptoNative_ ## cipherId(void) \
{ \
    static CipherInfo info = { flags, width, javaName }; \
    return &info; \
}

DEFINE_CIPHER(Aes128Ecb,        128, "AES/ECB/NoPadding", CIPHER_NONE)
DEFINE_CIPHER(Aes128Cbc,        128, "AES/CBC/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes128Cfb8,       128, "AES/CFB8/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes128Cfb128,     128, "AES/CFB128/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes128Gcm,        128, "AES/GCM/NoPadding", CIPHER_HAS_VARIABLE_TAG | CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes128Ccm,        128, "AES/CCM/NoPadding", CIPHER_HAS_VARIABLE_TAG | CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes192Ecb,        192, "AES/ECB/NoPadding", CIPHER_NONE)
DEFINE_CIPHER(Aes192Cbc,        192, "AES/CBC/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes192Cfb8,       192, "AES/CFB8/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes192Cfb128,     192, "AES/CFB128/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes192Gcm,        192, "AES/GCM/NoPadding", CIPHER_HAS_VARIABLE_TAG | CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes192Ccm,        192, "AES/CCM/NoPadding", CIPHER_HAS_VARIABLE_TAG | CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes256Ecb,        256, "AES/ECB/NoPadding", CIPHER_NONE)
DEFINE_CIPHER(Aes256Cbc,        256, "AES/CBC/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes256Cfb8,       256, "AES/CFB8/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes256Cfb128,     256, "AES/CFB128/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes256Gcm,        256, "AES/GCM/NoPadding", CIPHER_HAS_VARIABLE_TAG | CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Aes256Ccm,        256, "AES/CCM/NoPadding", CIPHER_HAS_VARIABLE_TAG | CIPHER_REQUIRES_IV)
DEFINE_CIPHER(DesEcb,           64,  "DES/ECB/NoPadding", CIPHER_NONE)
DEFINE_CIPHER(DesCbc,           64,  "DES/CBC/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(DesCfb8,          64,  "DES/CFB8/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Des3Ecb,          128, "DESede/ECB/NoPadding", CIPHER_NONE)
DEFINE_CIPHER(Des3Cbc,          128, "DESede/CBC/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Des3Cfb8,         128, "DESede/CFB8/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(Des3Cfb64,        128, "DESede/CFB/NoPadding", CIPHER_REQUIRES_IV)
DEFINE_CIPHER(ChaCha20Poly1305, 256, "ChaCha20/Poly1305/NoPadding", CIPHER_REQUIRES_IV)

//
// We don't have to check whether `CipherInfo` arguments are valid pointers, as these functions will be called after the
// context is created and the type stored in `CipherInfo` is asserted to be not NULL on creation time.  Managed code
// cannot modify the context so it's fairly safe to assume that we're passed a valid pointer here.
//
// The entry functions (those that can be called by external code) take care to validate that the context passed to them
// is a valid pointer and so we can assume the assertion from the preceding paragraph.
//
ARGS_NON_NULL_ALL static bool HasVariableTag(CipherInfo* type)
{
    return (type->flags & CIPHER_HAS_VARIABLE_TAG) == CIPHER_HAS_VARIABLE_TAG;
}

ARGS_NON_NULL_ALL static bool RequiresIV(CipherInfo* type)
{
    return (type->flags & CIPHER_REQUIRES_IV) == CIPHER_REQUIRES_IV;
}

ARGS_NON_NULL_ALL static jobject GetAlgorithmName(JNIEnv* env, CipherInfo* type)
{
    return make_java_string(env, type->name);
}

int32_t AndroidCryptoNative_CipherIsSupported(CipherInfo* type)
{
    abort_if_invalid_pointer_argument (type);

    JNIEnv* env = GetJNIEnv();
    jobject algName = GetAlgorithmName(env, type);
    if (!algName)
        return FAIL;

    jobject cipher = (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName);
    (*env)->DeleteLocalRef(env, algName);
    (*env)->DeleteLocalRef(env, cipher);

    // If we were able to call Cipher.getInstance without an exception, like NoSuchAlgorithmException,
    // then the algorithm is supported.
    return TryClearJNIExceptions(env) ? FAIL : SUCCESS;
}

CipherCtx* AndroidCryptoNative_CipherCreatePartial(CipherInfo* type)
{
    abort_if_invalid_pointer_argument (type);

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

    CipherCtx* ctx = xmalloc(sizeof(CipherCtx));
    ctx->cipher = cipher;
    ctx->type = type;
    ctx->tagLength = TAG_MAX_LENGTH;
    ctx->keySizeInBits = type->width;
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

ARGS_NON_NULL_ALL static int32_t ReinitializeCipher(CipherCtx* ctx)
{
    JNIEnv* env = GetJNIEnv();

    // SecretKeySpec keySpec = new SecretKeySpec(key.getEncoded(), "AES");
    // IvParameterSpec ivSpec = new IvParameterSpec(IV); or GCMParameterSpec for GCM/CCM
    // cipher.init(encMode, keySpec, ivSpec);

    jobject algName = GetAlgorithmName(env, ctx->type);
    if (!algName)
        return FAIL;

    int32_t keyLength = ctx->keySizeInBits / 8;
    jbyteArray keyBytes = make_java_byte_array(env, keyLength);
    (*env)->SetByteArrayRegion(env, keyBytes, 0, keyLength, (jbyte*)ctx->key);
    jobject sksObj = (*env)->NewObject(env, g_sksClass, g_sksCtor, keyBytes, algName);

    jobject ivPsObj = NULL;
    if (RequiresIV(ctx->type))
    {
        jbyteArray ivBytes = make_java_byte_array(env, ctx->ivLength);
        (*env)->SetByteArrayRegion(env, ivBytes, 0, ctx->ivLength, (jbyte*)ctx->iv);

        if (HasVariableTag(ctx->type))
        {
            ivPsObj = (*env)->NewObject(env, g_GCMParameterSpecClass, g_GCMParameterSpecCtor, ctx->tagLength * 8, ivBytes);
        }
        else
        {
            ivPsObj = (*env)->NewObject(env, g_ivPsClass, g_ivPsCtor, ivBytes);
        }

        (*env)->DeleteLocalRef(env, ivBytes);
    }

    (*env)->CallVoidMethod(env, ctx->cipher, g_cipherInitMethod, ctx->encMode, sksObj, ivPsObj);
    (*env)->DeleteLocalRef(env, algName);
    (*env)->DeleteLocalRef(env, sksObj);
    (*env)->DeleteLocalRef(env, ivPsObj);
    (*env)->DeleteLocalRef(env, keyBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t AndroidCryptoNative_CipherSetKeyAndIV(CipherCtx* ctx, uint8_t* key, uint8_t* iv, int32_t enc)
{
    if (!ctx)
        return FAIL;

    // input:  0 for Decrypt, 1 for Encrypt, -1 leave untouched
    // Cipher: 2 for Decrypt, 1 for Encrypt, N/A
    if (enc != -1)
    {
        abort_unless(enc == 0 || enc == 1, "The 'enc' parameter must be either 1 or 0");
        ctx->encMode = enc == 0 ? CIPHER_DECRYPT_MODE : CIPHER_ENCRYPT_MODE;
    }

    // CryptoNative_CipherSetKeyAndIV can be called separately for key and iv
    // so we need to wait for both and do Init after.
    if (key)
        SaveTo(key, &ctx->key, (size_t)ctx->keySizeInBits / 8, /* overwrite */ true);

    if (iv)
    {
        // Make sure length is set
        if (!ctx->ivLength)
        {
            // ivLength = cipher.getBlockSize();
            JNIEnv *env = GetJNIEnv();
            ctx->ivLength = (*env)->CallIntMethod(env, ctx->cipher, g_getBlockSizeMethod);
        }

        SaveTo(iv, &ctx->iv, (size_t)ctx->ivLength, /* overwrite */ true);
    }

    if (!ctx->key || (!ctx->iv && RequiresIV(ctx->type)))
        return SUCCESS;

    return ReinitializeCipher(ctx);
}

CipherCtx* AndroidCryptoNative_CipherCreate(CipherInfo* type, uint8_t* key, int32_t keySizeInBits, uint8_t* iv, int32_t enc)
{
    CipherCtx* ctx = AndroidCryptoNative_CipherCreatePartial(type);

    // Update the key size if provided
    if (keySizeInBits > 0)
        ctx->keySizeInBits = keySizeInBits;

    if (AndroidCryptoNative_CipherSetKeyAndIV(ctx, key, iv, enc) != SUCCESS)
        return FAIL;

    return ctx;
}

int32_t AndroidCryptoNative_CipherUpdateAAD(CipherCtx* ctx, uint8_t* in, int32_t inl)
{
    if (!ctx)
        return FAIL;

    abort_if_invalid_pointer_argument(in);

    JNIEnv* env = GetJNIEnv();
    jbyteArray inDataBytes = make_java_byte_array(env, inl);
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

    abort_if_invalid_pointer_argument(outl);
    abort_if_invalid_pointer_argument(in);

    JNIEnv* env = GetJNIEnv();
    jbyteArray inDataBytes = make_java_byte_array(env, inl);
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

    abort_if_invalid_pointer_argument(outm);
    abort_if_invalid_pointer_argument(outl);

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


int32_t AndroidCryptoNative_AeadCipherFinalEx(CipherCtx* ctx, uint8_t* outm, int32_t* outl, int32_t* authTagMismatch)
{
    if (!ctx)
        return FAIL;

    abort_if_invalid_pointer_argument(outm);
    abort_if_invalid_pointer_argument(outl);
    abort_if_invalid_pointer_argument(authTagMismatch);

    JNIEnv* env = GetJNIEnv();

    *outl = 0;
    *authTagMismatch = 0;

    jbyteArray outBytes = (jbyteArray)(*env)->CallObjectMethod(env, ctx->cipher, g_cipherDoFinalMethod);
    jthrowable ex = NULL;

    if (TryGetJNIException(env, &ex, false))
    {
        if (ex == NULL)
        {
            return FAIL;
        }

        if ((*env)->IsInstanceOf(env, ex, g_AEADBadTagExceptionClass))
        {
            *authTagMismatch = 1;
        }

        (*env)->DeleteLocalRef(env, ex);
        return FAIL;
    }

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

int32_t AndroidCryptoNative_CipherReset(CipherCtx* ctx, uint8_t* pIv, int32_t cIv)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJNIEnv();
    ReleaseGRef(env, ctx->cipher);
    jobject algName = GetAlgorithmName(env, ctx->type);
    if (!algName)
        return FAIL;

    // Resetting is only for the cipher, not the context.
    // We recreate and reinitialize a cipher with the same context.
    ctx->cipher = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName));
    (*env)->DeleteLocalRef(env, algName);
    if (CheckJNIExceptions(env))
        return FAIL;

    if (pIv)
    {
        if (ctx->ivLength != cIv)
        {
            return FAIL;
        }

        SaveTo(pIv, &ctx->iv, (size_t)ctx->ivLength, /* overwrite */ true);
    }
    else if (cIv != 0)
    {
        return FAIL;
    }

    return ReinitializeCipher(ctx);
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
