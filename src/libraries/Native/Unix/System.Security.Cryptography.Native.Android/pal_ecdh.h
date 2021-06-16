// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_eckey.h"
#include "pal_types.h"

PALEXPORT int32_t
AndroidCryptoNative_EcdhDeriveKey(EC_KEY* ourKey, EC_KEY* peerKey, uint8_t* resultKey, int32_t bufferLength, int32_t* usedBufferLength);
