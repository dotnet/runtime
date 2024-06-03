// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_pbkdf2.h"
#include "pal_utilities.h"


int32_t AndroidCryptoNative_Pbkdf2(const char* algorithmName,
                                   const uint8_t* password,
                                   int32_t passwordLength,
                                   const uint8_t* salt,
                                   int32_t saltLength,
                                   int32_t iterations,
                                   uint8_t* destination,
                                   int32_t destinationLength)
{
    JNIEnv* env = GetJNIEnv();

    jstring javaAlgorithmName = make_java_string(env, algorithmName);
    jbyteArray passwordBytes = make_java_byte_array(env, passwordLength);
    jbyteArray saltBytes = make_java_byte_array(env, saltLength);
    jbyteArray destinationBytes = make_java_byte_array(env, destinationLength);

    if (password && passwordLength > 0)
    {
        (*env)->SetByteArrayRegion(env, passwordBytes, 0, passwordLength, (const jbyte*)password);
    }

    if (salt && saltLength > 0)
    {
        (*env)->SetByteArrayRegion(env, saltBytes, 0, saltLength, (const jbyte*)salt);
    }

    jint ret = (*env)->CallStaticIntMethod(env, g_PalPbkdf2, g_PalPbkdf2Pbkdf2OneShot,
        javaAlgorithmName, passwordBytes, saltBytes, iterations, destinationBytes);

    if (CheckJNIExceptions(env))
    {
        ret = FAIL;
    }
    else if (ret == SUCCESS)
    {
        (*env)->GetByteArrayRegion(env, destinationBytes, 0, destinationLength, (jbyte*)destination);
    }

    (*env)->DeleteLocalRef(env, javaAlgorithmName);
    (*env)->DeleteLocalRef(env, passwordBytes);
    (*env)->DeleteLocalRef(env, saltBytes);
    (*env)->DeleteLocalRef(env, destinationBytes);

    return ret;
}
