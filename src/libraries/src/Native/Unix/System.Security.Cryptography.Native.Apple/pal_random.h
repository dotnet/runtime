// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

/*
Shims CCRandomGenerateBytes, putting the resulting CCRNGStatus value in pkCCStatus.

Returns 1 on success, 0 on system error (see pkCCStatus), -1 on input error.
*/
DLLEXPORT int32_t AppleCryptoNative_GetRandomBytes(uint8_t* pBuf, uint32_t cbBuf, int32_t* pkCCStatus);
