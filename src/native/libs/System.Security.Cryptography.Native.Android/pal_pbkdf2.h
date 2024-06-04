// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"
#include "pal_compiler.h"
#include "pal_types.h"


PALEXPORT int32_t AndroidCryptoNative_Pbkdf2(const char* algorithmName,
                                             const uint8_t* password,
                                             int32_t passwordLength,
                                             uint8_t* salt,
                                             int32_t saltLength,
                                             int32_t iterations,
                                             uint8_t* destination,
                                             int32_t destinationLength);
