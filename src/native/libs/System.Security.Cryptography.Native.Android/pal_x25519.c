// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x25519.h"
#include "pal_misc.h"

#include <string.h>

static jobject ImportSubjectPublicKeyInfo(JNIEnv* env, const uint8_t* buffer, int32_t bufferLength);
static int32_t ExportEncodedKey(jobject key, uint8_t* buffer, int32_t bufferLength, int32_t* bytesWritten);

int32_t AndroidCryptoNative_X25519IsSupported(void)
{
    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    INIT_LOCALS(loc, algorithmName, keyPairGenerator, keyPair);

    loc[algorithmName] = make_java_string(env, "XDH");
    loc[keyPairGenerator] = (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, loc[algorithmName]);

    if (TryClearJNIExceptions(env))
    {
        goto cleanup;
    }

    // Generating a key pair exercises the full provider path, which catches cases where
    // getInstance succeeds on a stub provider but actual key generation is not implemented.
    loc[keyPair] = (*env)->CallObjectMethod(env, loc[keyPairGenerator], g_keyPairGenGenKeyPairMethod);

    if (TryClearJNIExceptions(env))
    {
        goto cleanup;
    }

    if (loc[keyPair] != NULL)
    {
        ret = SUCCESS;
    }

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

void AndroidCryptoNative_X25519DestroyKey(jobject key)
{
    if (key)
    {
        JNIEnv* env = GetJNIEnv();

        if ((*env)->IsInstanceOf(env, key, g_DestroyableClass))
        {
            (*env)->CallVoidMethod(env, key, g_destroy);
            (void)TryClearJNIExceptions(env);
        }

        ReleaseGRef(env, key);
    }
}

int32_t AndroidCryptoNative_X25519GenerateKey(jobject* publicKey, jobject* privateKey)
{
    abort_if_invalid_pointer_argument(publicKey);
    abort_if_invalid_pointer_argument(privateKey);

    *publicKey = NULL;
    *privateKey = NULL;

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    INIT_LOCALS(loc, algorithmName, keyPairGenerator, keyPair, pubKey, privKey);

    loc[algorithmName] = make_java_string(env, "XDH");
    loc[keyPairGenerator] = (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, loc[algorithmName]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[keyPair] = (*env)->CallObjectMethod(env, loc[keyPairGenerator], g_keyPairGenGenKeyPairMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (loc[keyPair] == NULL)
    {
        goto cleanup;
    }

    loc[pubKey] = (*env)->CallObjectMethod(env, loc[keyPair], g_keyPairGetPublicMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[privKey] = (*env)->CallObjectMethod(env, loc[keyPair], g_keyPairGetPrivateMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (loc[pubKey] == NULL || loc[privKey] == NULL)
    {
        goto cleanup;
    }

    *publicKey = ToGRef(env, loc[pubKey]);
    loc[pubKey] = NULL;

    if (CheckJNIExceptions(env) || *publicKey == NULL)
    {
        goto cleanup;
    }

    *privateKey = ToGRef(env, loc[privKey]);
    loc[privKey] = NULL;

    if (CheckJNIExceptions(env) || *privateKey == NULL)
    {
        AndroidCryptoNative_X25519DestroyKey(*publicKey);
        *publicKey = NULL;
        goto cleanup;
    }

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_X25519ExportSubjectPublicKeyInfo(
    jobject publicKey,
    uint8_t* buffer,
    int32_t bufferLength,
    int32_t* bytesWritten)
{
    return ExportEncodedKey(publicKey, buffer, bufferLength, bytesWritten);
}

int32_t AndroidCryptoNative_X25519ExportPkcs8PrivateKey(
    jobject privateKey,
    uint8_t* buffer,
    int32_t bufferLength,
    int32_t* bytesWritten)
{
    return ExportEncodedKey(privateKey, buffer, bufferLength, bytesWritten);
}

jobject AndroidCryptoNative_X25519ImportSubjectPublicKeyInfo(const uint8_t* buffer, int32_t bufferLength)
{
    abort_if_invalid_pointer_argument(buffer);
    abort_if_negative_integer_argument(bufferLength);

    JNIEnv* env = GetJNIEnv();
    jobject publicKey = ImportSubjectPublicKeyInfo(env, buffer, bufferLength);
    jobject ret = ToGRef(env, publicKey);

    if (CheckJNIExceptions(env) || ret == NULL)
    {
        ret = NULL;
    }

    return ret;
}

jobject AndroidCryptoNative_X25519ImportPkcs8PrivateKey(const uint8_t* buffer, int32_t bufferLength)
{
    abort_if_invalid_pointer_argument(buffer);
    abort_if_negative_integer_argument(bufferLength);

    JNIEnv* env = GetJNIEnv();
    jobject ret = NULL;

    INIT_LOCALS(loc, algorithmName, keyFactory, pkcs8Bytes, keySpec, privateKey);

    loc[algorithmName] = make_java_string(env, "XDH");
    loc[keyFactory] = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[algorithmName]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[pkcs8Bytes] = make_java_byte_array(env, bufferLength);
    (*env)->SetByteArrayRegion(env, loc[pkcs8Bytes], 0, bufferLength, (const jbyte*)buffer);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[keySpec] = (*env)->NewObject(env, g_PKCS8EncodedKeySpec, g_PKCS8EncodedKeySpecCtor, loc[pkcs8Bytes]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    jbyte* pkcs8Elements = (*env)->GetByteArrayElements(env, loc[pkcs8Bytes], NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (pkcs8Elements != NULL)
    {
        memset(pkcs8Elements, 0, (size_t)bufferLength);
        (*env)->ReleaseByteArrayElements(env, loc[pkcs8Bytes], pkcs8Elements, 0);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    loc[privateKey] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPrivateMethod, loc[keySpec]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (loc[privateKey] == NULL)
    {
        goto cleanup;
    }

    ret = ToGRef(env, loc[privateKey]);
    loc[privateKey] = NULL;

    if (CheckJNIExceptions(env) || ret == NULL)
    {
        AndroidCryptoNative_X25519DestroyKey(ret);
        ret = NULL;
        goto cleanup;
    }

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_X25519DeriveSecret(
    jobject privateKey,
    jobject publicKey,
    uint8_t* destination,
    int32_t destinationLength)
{
    abort_if_invalid_pointer_argument(privateKey);
    abort_if_invalid_pointer_argument(publicKey);
    abort_if_invalid_pointer_argument(destination);
    abort_if_negative_integer_argument(destinationLength);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    INIT_LOCALS(loc, algorithmName, keyAgreement, phaseResult, secret);

    loc[algorithmName] = make_java_string(env, "XDH");
    loc[keyAgreement] = (*env)->CallStaticObjectMethod(env, g_KeyAgreementClass, g_KeyAgreementGetInstance, loc[algorithmName]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    (*env)->CallVoidMethod(env, loc[keyAgreement], g_KeyAgreementInit, privateKey);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[phaseResult] = (*env)->CallObjectMethod(env, loc[keyAgreement], g_KeyAgreementDoPhase, publicKey, JNI_TRUE);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[secret] = (jbyteArray)(*env)->CallObjectMethod(env, loc[keyAgreement], g_KeyAgreementGenerateSecret);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (loc[secret] == NULL)
    {
        goto cleanup;
    }

    jsize secretLen = (*env)->GetArrayLength(env, loc[secret]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (secretLen != destinationLength)
    {
        goto cleanup;
    }

    (*env)->GetByteArrayRegion(env, loc[secret], 0, secretLen, (jbyte*)destination);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_X25519DeriveSecretWithSubjectPublicKeyInfo(
    jobject privateKey,
    const uint8_t* buffer,
    int32_t bufferLength,
    uint8_t* destination,
    int32_t destinationLength)
{
    abort_if_invalid_pointer_argument(privateKey);
    abort_if_invalid_pointer_argument(buffer);
    abort_if_invalid_pointer_argument(destination);
    abort_if_negative_integer_argument(bufferLength);
    abort_if_negative_integer_argument(destinationLength);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    INIT_LOCALS(loc, publicKey);

    loc[publicKey] = ImportSubjectPublicKeyInfo(env, buffer, bufferLength);

    if (loc[publicKey] == NULL)
    {
        goto cleanup;
    }

    ret = AndroidCryptoNative_X25519DeriveSecret(privateKey, loc[publicKey], destination, destinationLength);

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

static int32_t ExportEncodedKey(jobject key, uint8_t* buffer, int32_t bufferLength, int32_t* bytesWritten)
{
    abort_if_invalid_pointer_argument(key);
    abort_if_invalid_pointer_argument(buffer);
    abort_if_invalid_pointer_argument(bytesWritten);
    abort_if_negative_integer_argument(bufferLength);

    *bytesWritten = 0;

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    INIT_LOCALS(loc, encoded);

    loc[encoded] = (jbyteArray)(*env)->CallObjectMethod(env, key, g_KeyGetEncoded);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (loc[encoded] == NULL)
    {
        goto cleanup;
    }

    jsize encodedLen = (*env)->GetArrayLength(env, loc[encoded]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (encodedLen > bufferLength)
    {
        *bytesWritten = (int32_t)encodedLen;
        ret = INSUFFICIENT_BUFFER;
        goto cleanup;
    }

    (*env)->GetByteArrayRegion(env, loc[encoded], 0, encodedLen, (jbyte*)buffer);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    *bytesWritten = (int32_t)encodedLen;
    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

static jobject ImportSubjectPublicKeyInfo(JNIEnv* env, const uint8_t* buffer, int32_t bufferLength)
{
    jobject ret = NULL;

    INIT_LOCALS(loc, algorithmName, keyFactory, spkiBytes, keySpec, publicKey);

    loc[algorithmName] = make_java_string(env, "XDH");
    loc[keyFactory] = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[algorithmName]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[spkiBytes] = make_java_byte_array(env, bufferLength);
    (*env)->SetByteArrayRegion(env, loc[spkiBytes], 0, bufferLength, (const jbyte*)buffer);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[keySpec] = (*env)->NewObject(env, g_X509EncodedKeySpecClass, g_X509EncodedKeySpecCtor, loc[spkiBytes]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[publicKey] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPublicMethod, loc[keySpec]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (loc[publicKey] == NULL)
    {
        goto cleanup;
    }

    ret = loc[publicKey];
    loc[publicKey] = NULL;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}
