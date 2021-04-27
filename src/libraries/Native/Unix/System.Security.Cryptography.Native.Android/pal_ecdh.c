// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_compiler.h"
#include "pal_ecdh.h"
#include "pal_types.h"
#include "pal_jni.h"

int32_t AndroidCryptoNative_EcdhDeriveKey(EC_KEY* ourKey, EC_KEY* peerKey, uint8_t* resultKey, int32_t bufferLength, int32_t* usedBufferLength)
{
    JNIEnv* env = GetJNIEnv();

    jstring algorithmName = JSTRING("ECDH");

    jobject keyAgreement = (*env)->CallStaticObjectMethod(env, g_KeyAgreementClass, g_KeyAgreementGetInstance, algorithmName);
    ReleaseLRef(env, algorithmName);

    jobject privateKey = (*env)->CallObjectMethod(env, ourKey->keyPair, g_keyPairGetPrivateMethod);

    (*env)->CallVoidMethod(env, keyAgreement, g_KeyAgreementInit, privateKey);
    ReleaseLRef(env, privateKey);
    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keyAgreement);
        *usedBufferLength = 0;
        return FAIL;
    }

    jobject peerPublicKey = (*env)->CallObjectMethod(env, peerKey->keyPair, g_keyPairGetPublicMethod);
    ReleaseLRef(env, (*env)->CallObjectMethod(env, keyAgreement, g_KeyAgreementDoPhase, peerPublicKey, JNI_TRUE));
    ReleaseLRef(env, peerPublicKey);
    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keyAgreement);
        *usedBufferLength = 0;
        return FAIL;
    }

    jbyteArray secret = (*env)->CallObjectMethod(env, keyAgreement, g_KeyAgreementGenerateSecret);
    ReleaseLRef(env, keyAgreement);

    if (CheckJNIExceptions(env))
    {
        *usedBufferLength = 0;
        return FAIL;
    }

    jsize secretBufferLen = (*env)->GetArrayLength(env, secret);
    if (secretBufferLen > bufferLength)
    {
        ReleaseLRef(env, secret);
        *usedBufferLength = 0;
        return FAIL;
    }

    (*env)->GetByteArrayRegion(env, secret, 0, secretBufferLen, (jbyte*)resultKey);
    ReleaseLRef(env, secret);
    *usedBufferLength = secretBufferLen;

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}
