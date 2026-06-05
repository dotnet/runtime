// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x25519.h"
#include "pal_misc.h"

#include <string.h>

static int32_t ExportEncodedKey(jobject key, uint8_t* buffer, int32_t bufferLength, int32_t* bytesWritten);

int32_t AndroidCryptoNative_X25519IsSupported(void)
{
    JNIEnv* env = GetJNIEnv();

    jstring algorithmName = make_java_string(env, "X25519");
    jobject keyPairGenerator = (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, algorithmName);
    ReleaseLRef(env, algorithmName);

    if (TryClearJNIExceptions(env))
    {
        ReleaseLRef(env, keyPairGenerator);
        return FAIL;
    }

    // Generating a key pair exercises the full provider path, which catches cases where
    // getInstance succeeds on a stub provider but actual key generation is not implemented.
    jobject keyPair = (*env)->CallObjectMethod(env, keyPairGenerator, g_keyPairGenGenKeyPairMethod);
    ReleaseLRef(env, keyPairGenerator);
    ReleaseLRef(env, keyPair);

    return TryClearJNIExceptions(env) ? FAIL : SUCCESS;
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

    // Conscrypt's XDH KeyPairGenerator does not support initialize(AlgorithmParameterSpec),
    // so use "X25519" directly as the algorithm name instead of "XDH" + NamedParameterSpec.
    jstring algorithmName = make_java_string(env, "X25519");
    jobject keyPairGenerator = (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, algorithmName);
    ReleaseLRef(env, algorithmName);

    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keyPairGenerator);
        return FAIL;
    }

    jobject keyPair = (*env)->CallObjectMethod(env, keyPairGenerator, g_keyPairGenGenKeyPairMethod);
    ReleaseLRef(env, keyPairGenerator);

    if (CheckJNIExceptions(env) || !keyPair)
    {
        ReleaseLRef(env, keyPair);
        return FAIL;
    }

    jobject pubKey = (*env)->CallObjectMethod(env, keyPair, g_keyPairGetPublicMethod);
    jobject privKey = (*env)->CallObjectMethod(env, keyPair, g_keyPairGetPrivateMethod);
    ReleaseLRef(env, keyPair);

    if (CheckJNIExceptions(env) || !pubKey || !privKey)
    {
        ReleaseLRef(env, pubKey);
        ReleaseLRef(env, privKey);
        return FAIL;
    }

    *publicKey = ToGRef(env, pubKey);
    *privateKey = ToGRef(env, privKey);
    return SUCCESS;
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

    jstring algorithmName = make_java_string(env, "X25519");
    jobject keyFactory = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, algorithmName);
    ReleaseLRef(env, algorithmName);

    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keyFactory);
        return NULL;
    }

    jbyteArray spkiBytes = make_java_byte_array(env, bufferLength);
    (*env)->SetByteArrayRegion(env, spkiBytes, 0, bufferLength, (const jbyte*)buffer);

    jobject keySpec = (*env)->NewObject(env, g_X509EncodedKeySpecClass, g_X509EncodedKeySpecCtor, spkiBytes);
    ReleaseLRef(env, spkiBytes);

    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keyFactory);
        ReleaseLRef(env, keySpec);
        return NULL;
    }

    jobject publicKey = (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPublicMethod, keySpec);
    ReleaseLRef(env, keyFactory);
    ReleaseLRef(env, keySpec);

    if (CheckJNIExceptions(env) || !publicKey)
    {
        ReleaseLRef(env, publicKey);
        return NULL;
    }

    return ToGRef(env, publicKey);
}

jobject AndroidCryptoNative_X25519ImportPkcs8PrivateKey(const uint8_t* buffer, int32_t bufferLength)
{
    abort_if_invalid_pointer_argument(buffer);
    abort_if_negative_integer_argument(bufferLength);

    JNIEnv* env = GetJNIEnv();

    jstring algorithmName = make_java_string(env, "X25519");
    jobject keyFactory = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, algorithmName);
    ReleaseLRef(env, algorithmName);

    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keyFactory);
        return NULL;
    }

    jbyteArray pkcs8Bytes = make_java_byte_array(env, bufferLength);
    (*env)->SetByteArrayRegion(env, pkcs8Bytes, 0, bufferLength, (const jbyte*)buffer);

    jobject keySpec = (*env)->NewObject(env, g_PKCS8EncodedKeySpec, g_PKCS8EncodedKeySpecCtor, pkcs8Bytes);

    jbyte* pkcs8Elements = (*env)->GetByteArrayElements(env, pkcs8Bytes, NULL);
    if (pkcs8Elements != NULL)
    {
        memset(pkcs8Elements, 0, (size_t)bufferLength);
        (*env)->ReleaseByteArrayElements(env, pkcs8Bytes, pkcs8Elements, 0);
    }

    ReleaseLRef(env, pkcs8Bytes);

    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keyFactory);
        ReleaseLRef(env, keySpec);
        return NULL;
    }

    jobject privateKey = (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPrivateMethod, keySpec);
    ReleaseLRef(env, keyFactory);
    ReleaseLRef(env, keySpec);

    if (CheckJNIExceptions(env) || !privateKey)
    {
        ReleaseLRef(env, privateKey);
        return NULL;
    }

    return ToGRef(env, privateKey);
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

    jstring algorithmName = make_java_string(env, "XDH");
    jobject keyAgreement = (*env)->CallStaticObjectMethod(env, g_KeyAgreementClass, g_KeyAgreementGetInstance, algorithmName);
    ReleaseLRef(env, algorithmName);

    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keyAgreement);
        return FAIL;
    }

    (*env)->CallVoidMethod(env, keyAgreement, g_KeyAgreementInit, privateKey);

    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keyAgreement);
        return FAIL;
    }

    jobject phaseResult = (*env)->CallObjectMethod(env, keyAgreement, g_KeyAgreementDoPhase, publicKey, JNI_TRUE);
    ReleaseLRef(env, phaseResult);

    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keyAgreement);
        return FAIL;
    }

    jbyteArray secret = (jbyteArray)(*env)->CallObjectMethod(env, keyAgreement, g_KeyAgreementGenerateSecret);
    ReleaseLRef(env, keyAgreement);

    if (CheckJNIExceptions(env) || !secret)
    {
        ReleaseLRef(env, secret);
        return FAIL;
    }

    jsize secretLen = (*env)->GetArrayLength(env, secret);

    if (secretLen != destinationLength)
    {
        ReleaseLRef(env, secret);
        return FAIL;
    }

    (*env)->GetByteArrayRegion(env, secret, 0, secretLen, (jbyte*)destination);
    ReleaseLRef(env, secret);

    ret = CheckJNIExceptions(env) ? FAIL : SUCCESS;
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

    jbyteArray encoded = (jbyteArray)(*env)->CallObjectMethod(env, key, g_KeyGetEncoded);

    if (CheckJNIExceptions(env) || !encoded)
    {
        ReleaseLRef(env, encoded);
        return FAIL;
    }

    jsize encodedLen = (*env)->GetArrayLength(env, encoded);

    if (encodedLen > bufferLength)
    {
        *bytesWritten = (int32_t)encodedLen;
        ReleaseLRef(env, encoded);
        return INSUFFICIENT_BUFFER;
    }

    (*env)->GetByteArrayRegion(env, encoded, 0, encodedLen, (jbyte*)buffer);
    ReleaseLRef(env, encoded);

    if (CheckJNIExceptions(env))
    {
        return FAIL;
    }

    *bytesWritten = (int32_t)encodedLen;
    return SUCCESS;
}
