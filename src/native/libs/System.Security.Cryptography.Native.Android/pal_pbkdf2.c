// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_pbkdf2.h"
#include "pal_utilities.h"

int32_t AndroidCryptoNative_Pbkdf2(const char* algorithmName,
                                   const uint8_t* password,
                                   int32_t passwordLength,
                                   uint8_t* salt,
                                   int32_t saltLength,
                                   int32_t iterations,
                                   uint8_t* destination,
                                   int32_t destinationLength)
{
    JNIEnv* env = GetJNIEnv();
    jint ret = FAIL;

    jstring javaAlgorithmName = make_java_string(env, algorithmName);
    jbyteArray passwordBytes = make_java_byte_array(env, passwordLength);
    jobject destinationBuffer = (*env)->NewDirectByteBuffer(env, destination, destinationLength);
    jobject saltByteBuffer = NULL;

    if (javaAlgorithmName == NULL || passwordBytes == NULL || destinationBuffer == NULL)
    {
        goto cleanup;
    }

    if (password && passwordLength > 0)
    {
        (*env)->SetByteArrayRegion(env, passwordBytes, 0, passwordLength, (const jbyte*)password);
    }

    if (salt && saltLength > 0)
    {
        saltByteBuffer = (*env)->NewDirectByteBuffer(env, salt, saltLength);

        if (saltByteBuffer == NULL)
        {
            goto cleanup;
        }
    }

    ret = (*env)->CallStaticIntMethod(env, g_PalPbkdf2, g_PalPbkdf2Pbkdf2OneShot,
        javaAlgorithmName, passwordBytes, saltByteBuffer, iterations, destinationBuffer);

    if (CheckJNIExceptions(env))
    {
        ret = FAIL;
    }

cleanup:
    (*env)->DeleteLocalRef(env, javaAlgorithmName);
    (*env)->DeleteLocalRef(env, passwordBytes);
    (*env)->DeleteLocalRef(env, saltByteBuffer);
    (*env)->DeleteLocalRef(env, destinationBuffer);

    return ret;
}
