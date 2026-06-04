// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_cipher.h"
#include "pal_utilities.h"
#include <stdio.h>
#include <string.h>

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

static __thread char t_lastCipherDiagnostic[1024];

static void ClearLastCipherDiagnostic(void)
{
    t_lastCipherDiagnostic[0] = '\0';
}

static void CopyJavaString(JNIEnv* env, jstring value, char* destination, size_t destinationLength)
{
    if (destinationLength == 0)
        return;

    destination[0] = '\0';

    if (value == NULL)
        return;

    const char* utf8 = (*env)->GetStringUTFChars(env, value, NULL);
    if (utf8 == NULL)
    {
        (void)TryClearJNIExceptions(env);
        return;
    }

    snprintf(destination, destinationLength, "%s", utf8);
    (*env)->ReleaseStringUTFChars(env, value, utf8);
}

static void GetThrowableClassName(JNIEnv* env, jthrowable ex, char* destination, size_t destinationLength)
{
    if (destinationLength == 0)
        return;

    destination[0] = '\0';

    jclass classClass = (*env)->FindClass(env, "java/lang/Class");
    if (classClass == NULL)
    {
        (void)TryClearJNIExceptions(env);
        return;
    }

    jmethodID getName = (*env)->GetMethodID(env, classClass, "getName", "()Ljava/lang/String;");
    if (getName == NULL)
    {
        (*env)->DeleteLocalRef(env, classClass);
        (void)TryClearJNIExceptions(env);
        return;
    }

    jclass exClass = (*env)->GetObjectClass(env, ex);
    if (exClass == NULL)
    {
        (*env)->DeleteLocalRef(env, classClass);
        (void)TryClearJNIExceptions(env);
        return;
    }

    jstring name = (jstring)(*env)->CallObjectMethod(env, exClass, getName);
    if (name != NULL)
    {
        CopyJavaString(env, name, destination, destinationLength);
        (*env)->DeleteLocalRef(env, name);
    }
    else
    {
        (void)TryClearJNIExceptions(env);
    }

    (*env)->DeleteLocalRef(env, exClass);
    (*env)->DeleteLocalRef(env, classClass);
}

static void RecordCipherExceptionDiagnostic(JNIEnv* env, CipherCtx* ctx, const char* phase, int32_t inputLength, jthrowable ex)
{
    char className[256];
    char message[512];
    char causeClassName[256];
    char causeMessage[512];

    className[0] = '\0';
    message[0] = '\0';
    causeClassName[0] = '\0';
    causeMessage[0] = '\0';

    if (ex != NULL)
    {
        GetThrowableClassName(env, ex, className, sizeof(className));

        jstring javaMessage = (jstring)(*env)->CallObjectMethod(env, ex, g_ThrowableGetMessage);
        if (javaMessage != NULL)
        {
            CopyJavaString(env, javaMessage, message, sizeof(message));
            (*env)->DeleteLocalRef(env, javaMessage);
        }
        else
        {
            (void)TryClearJNIExceptions(env);
        }

        jthrowable cause = (jthrowable)(*env)->CallObjectMethod(env, ex, g_ThrowableGetCause);
        if (cause != NULL)
        {
            GetThrowableClassName(env, cause, causeClassName, sizeof(causeClassName));

            jstring javaCauseMessage = (jstring)(*env)->CallObjectMethod(env, cause, g_ThrowableGetMessage);
            if (javaCauseMessage != NULL)
            {
                CopyJavaString(env, javaCauseMessage, causeMessage, sizeof(causeMessage));
                (*env)->DeleteLocalRef(env, javaCauseMessage);
            }
            else
            {
                (void)TryClearJNIExceptions(env);
            }

            (*env)->DeleteLocalRef(env, cause);
        }
        else
        {
            (void)TryClearJNIExceptions(env);
        }
    }

    snprintf(
        t_lastCipherDiagnostic,
        sizeof(t_lastCipherDiagnostic),
        "nativePhase=%s; cipher=%s; inputLength=%d; javaExceptionClass=%s; javaExceptionMessage=%s; javaCauseClass=%s; javaCauseMessage=%s",
        phase,
        ctx != NULL && ctx->type != NULL ? ctx->type->name : "<unknown>",
        inputLength,
        className[0] != '\0' ? className : "<none>",
        message[0] != '\0' ? message : "<none>",
        causeClassName[0] != '\0' ? causeClassName : "<none>",
        causeMessage[0] != '\0' ? causeMessage : "<none>");
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

int32_t AndroidCryptoNative_CipherGetLastDiagnostic(uint8_t* buffer, int32_t bufferLength)
{
    if (bufferLength < 0)
        return FAIL;

    size_t diagnosticLength = strlen(t_lastCipherDiagnostic);

    if (buffer != NULL && bufferLength > 0)
    {
        size_t bytesToCopy = diagnosticLength;
        if (bytesToCopy >= (size_t)bufferLength)
        {
            bytesToCopy = (size_t)bufferLength - 1;
        }

        memcpy(buffer, t_lastCipherDiagnostic, bytesToCopy);
        buffer[bytesToCopy] = '\0';
    }

    return (int32_t)diagnosticLength;
}

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

    ClearLastCipherDiagnostic();

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

    if ((*env)->ExceptionCheck(env))
    {
        jthrowable ex = NULL;
        (void)TryGetJNIException(env, &ex, false);
        RecordCipherExceptionDiagnostic(env, ctx, "updateAAD", inl, ex);
        if (ex != NULL)
        {
            (*env)->Throw(env, ex);
            (*env)->DeleteLocalRef(env, ex);
        }
        LOG_ERROR("Cipher.updateAAD failed for %s with input length %d", ctx->type->name, inl);
        return CheckJNIExceptions(env) ? FAIL : SUCCESS;
    }

    return SUCCESS;
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

    if ((*env)->ExceptionCheck(env))
    {
        jthrowable ex = NULL;
        (void)TryGetJNIException(env, &ex, false);
        RecordCipherExceptionDiagnostic(env, ctx, "update", inl, ex);
        if (ex != NULL)
        {
            (*env)->Throw(env, ex);
            (*env)->DeleteLocalRef(env, ex);
        }
        LOG_ERROR("Cipher.update failed for %s with input length %d", ctx->type->name, inl);
        return CheckJNIExceptions(env) ? FAIL : SUCCESS;
    }

    return SUCCESS;
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

    // outm is allowed to be NULL
    abort_if_invalid_pointer_argument(outl);
    abort_if_invalid_pointer_argument(authTagMismatch);

    JNIEnv* env = GetJNIEnv();

    *outl = 0;
    *authTagMismatch = 0;

    jbyteArray outBytes = (jbyteArray)(*env)->CallObjectMethod(env, ctx->cipher, g_cipherDoFinalMethod);
    jthrowable ex = NULL;

    if (TryGetJNIException(env, &ex, true))
    {
        if (ex == NULL)
        {
            RecordCipherExceptionDiagnostic(env, ctx, "doFinal", 0, NULL);
            LOG_ERROR("Cipher.doFinal failed for %s without an exception object", ctx->type->name);
            return FAIL;
        }

        RecordCipherExceptionDiagnostic(env, ctx, "doFinal", 0, ex);

        if ((*env)->IsInstanceOf(env, ex, g_AEADBadTagExceptionClass))
        {
            *authTagMismatch = 1;
        }

        LOG_ERROR(
            "Cipher.doFinal failed for %s; is AEADBadTagException=%d",
            ctx->type->name,
            *authTagMismatch);

        (*env)->DeleteLocalRef(env, ex);
        return FAIL;
    }

    jsize outBytesLen = (*env)->GetArrayLength(env, outBytes);
    *outl = outBytesLen;

    if (outBytesLen > 0)
    {
        abort_if_invalid_pointer_argument(outm);
        (*env)->GetByteArrayRegion(env, outBytes, 0, outBytesLen, (jbyte*) outm);
    }

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
