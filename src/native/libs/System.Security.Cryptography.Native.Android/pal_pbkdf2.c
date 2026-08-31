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
    INIT_LOCALS(loc, javaAlgorithmName, passwordBytes, saltByteBuffer, destinationBuffer);

    loc[javaAlgorithmName] = make_java_string(env, algorithmName);
    loc[passwordBytes] = make_java_byte_array(env, passwordLength);
    loc[destinationBuffer] = (*env)->NewDirectByteBuffer(env, destination, destinationLength);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (loc[javaAlgorithmName] == NULL || loc[passwordBytes] == NULL || loc[destinationBuffer] == NULL)
    {
        goto cleanup;
    }

    if (password && passwordLength > 0)
    {
        (*env)->SetByteArrayRegion(env, loc[passwordBytes], 0, passwordLength, (const jbyte*)password);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    if (salt && saltLength > 0)
    {
        loc[saltByteBuffer] = (*env)->NewDirectByteBuffer(env, salt, saltLength);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        if (loc[saltByteBuffer] == NULL)
        {
            goto cleanup;
        }
    }

    ret = (*env)->CallStaticIntMethod(env, g_PalPbkdf2, g_PalPbkdf2Pbkdf2OneShot,
        loc[javaAlgorithmName], loc[passwordBytes], loc[saltByteBuffer], iterations, loc[destinationBuffer]);

    if (CheckJNIExceptions(env))
    {
        ret = FAIL;
    }

cleanup:
    RELEASE_LOCALS(loc, env);

    return ret;
}
