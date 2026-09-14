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

    INIT_LOCALS(loc, algorithmName, keyAgreement, privateKey, peerPublicKey, secret);

    loc[algorithmName] = make_java_string(env, "ECDH");

    loc[keyAgreement] = (*env)->CallStaticObjectMethod(env, g_KeyAgreementClass, g_KeyAgreementGetInstance, loc[algorithmName]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[privateKey] = (*env)->CallObjectMethod(env, ourKey->keyPair, g_keyPairGetPrivateMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    (*env)->CallVoidMethod(env, loc[keyAgreement], g_KeyAgreementInit, loc[privateKey]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[peerPublicKey] = (*env)->CallObjectMethod(env, peerKey->keyPair, g_keyPairGetPublicMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ReleaseLRef(env, (*env)->CallObjectMethod(env, loc[keyAgreement], g_KeyAgreementDoPhase, loc[peerPublicKey], JNI_TRUE));
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[secret] = (*env)->CallObjectMethod(env, loc[keyAgreement], g_KeyAgreementGenerateSecret);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    jsize secretBufferLen = (*env)->GetArrayLength(env, loc[secret]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (secretBufferLen > bufferLength)
        goto cleanup;

    (*env)->GetByteArrayRegion(env, loc[secret], 0, secretBufferLen, (jbyte*)resultKey);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    *usedBufferLength = secretBufferLen;

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}
