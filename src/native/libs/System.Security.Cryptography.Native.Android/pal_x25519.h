// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_jni.h"
#include "pal_types.h"

PALEXPORT int32_t AndroidCryptoNative_X25519IsSupported(void);

PALEXPORT void AndroidCryptoNative_X25519DestroyKey(jobject key);

PALEXPORT int32_t AndroidCryptoNative_X25519GenerateKey(jobject* publicKey, jobject* privateKey);

PALEXPORT int32_t AndroidCryptoNative_X25519ExportSubjectPublicKeyInfo(
    jobject publicKey,
    uint8_t* buffer,
    int32_t bufferLength,
    int32_t* bytesWritten);

PALEXPORT int32_t AndroidCryptoNative_X25519ExportPkcs8PrivateKey(
    jobject privateKey,
    uint8_t* buffer,
    int32_t bufferLength,
    int32_t* bytesWritten);

PALEXPORT jobject AndroidCryptoNative_X25519ImportSubjectPublicKeyInfo(const uint8_t* buffer, int32_t bufferLength);
PALEXPORT jobject AndroidCryptoNative_X25519ImportPkcs8PrivateKey(const uint8_t* buffer, int32_t bufferLength);

PALEXPORT int32_t AndroidCryptoNative_X25519DeriveSecret(
    jobject privateKey,
    jobject publicKey,
    uint8_t* destination,
    int32_t destinationLength);

PALEXPORT int32_t AndroidCryptoNative_X25519DeriveSecretWithSubjectPublicKeyInfo(
    jobject privateKey,
    const uint8_t* buffer,
    int32_t bufferLength,
    uint8_t* destination,
    int32_t destinationLength);
