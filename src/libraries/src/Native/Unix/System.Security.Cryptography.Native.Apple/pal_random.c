// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_random.h"

#include <CommonCrypto/CommonCrypto.h>
#include <CommonCrypto/CommonRandom.h>

int32_t AppleCryptoNative_GetRandomBytes(uint8_t* pBuf, uint32_t cbBuf, int32_t* pkCCStatus)
{
    if (pBuf == NULL || pkCCStatus == NULL)
        return -1;

    CCRNGStatus status = CCRandomGenerateBytes(pBuf, cbBuf);
    *pkCCStatus = status;
    return status == kCCSuccess;
}
