// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

#define SIGNATURE_VERIFICATION_ERROR -1

ARGS_NON_NULL(1, 2, 3, 4, 6)
int32_t AndroidCryptoNative_SignWithSignatureObject(JNIEnv* env,
                                                    jobject signatureObject,
                                                    jobject privateKey,
                                                    const uint8_t* dgst,
                                                    int32_t dgstlen,
                                                    uint8_t* sig,
                                                    int32_t* siglen);

ARGS_NON_NULL_ALL
int32_t AndroidCryptoNative_VerifyWithSignatureObject(JNIEnv* env,
                                                      jobject signatureObject,
                                                      jobject publicKey,
                                                      const uint8_t* dgst,
                                                      int32_t dgstlen,
                                                      const uint8_t* sig,
                                                      int32_t siglen);
