// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_compiler.h"
#include "pal_ecdh.h"
#include "pal_types.h"
#include "pal_jni.h"

int32_t AndroidCryptoNative_EcdhDeriveKey(EC_KEY* ourKey, EC_KEY* peerKey, uint8_t* resultKey, int32_t bufferLength, int32_t* usedBufferLength)
{
    abort_if_invalid_pointer_argument (ourKey);
    abort_if_invalid_pointer_argument (peerKey);
    abort_if_invalid_pointer_argument (resultKey);
    abort_if_invalid_pointer_argument (usedBufferLength);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    *usedBufferLength = 0;

    jstring algorithmName = NULL;
    jobject keyAgreement = NULL;
    jobject privateKey = NULL;
    jobject peerPublicKey = NULL;
    jbyteArray secret = NULL;

    algorithmName = make_java_string(env, "ECDH");

    keyAgreement = (*env)->CallStaticObjectMethod(env, g_KeyAgreementClass, g_KeyAgreementGetInstance, algorithmName);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    privateKey = (*env)->CallObjectMethod(env, ourKey->keyPair, g_keyPairGetPrivateMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    (*env)->CallVoidMethod(env, keyAgreement, g_KeyAgreementInit, privateKey);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    peerPublicKey = (*env)->CallObjectMethod(env, peerKey->keyPair, g_keyPairGetPublicMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ReleaseLRef(env, (*env)->CallObjectMethod(env, keyAgreement, g_KeyAgreementDoPhase, peerPublicKey, JNI_TRUE));
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    secret = (*env)->CallObjectMethod(env, keyAgreement, g_KeyAgreementGenerateSecret);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    jsize secretBufferLen = (*env)->GetArrayLength(env, secret);
    if (secretBufferLen > bufferLength)
        goto cleanup;

    (*env)->GetByteArrayRegion(env, secret, 0, secretBufferLen, (jbyte*)resultKey);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    *usedBufferLength = secretBufferLen;

    ret = SUCCESS;

cleanup:
    ReleaseLRef(env, secret);
    ReleaseLRef(env, peerPublicKey);
    ReleaseLRef(env, privateKey);
    ReleaseLRef(env, keyAgreement);
    ReleaseLRef(env, algorithmName);
    return ret;
}
