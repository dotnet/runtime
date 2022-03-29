// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

PALEXPORT void CryptoNative_ErrClearError(void);
PALEXPORT uint64_t CryptoNative_ErrGetErrorAlloc(int32_t* isAllocFailure);
PALEXPORT uint64_t CryptoNative_ErrPeekError(void);
PALEXPORT uint64_t CryptoNative_ErrPeekLastError(void);
PALEXPORT const char* CryptoNative_ErrReasonErrorString(uint64_t error);
PALEXPORT void CryptoNative_ErrErrorStringN(uint64_t e, char* buf, int32_t len);
