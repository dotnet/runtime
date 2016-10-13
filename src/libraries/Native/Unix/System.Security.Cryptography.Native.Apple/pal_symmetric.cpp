// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_symmetric.h"

#include <assert.h>

static_assert(PAL_OperationEncrypt == kCCEncrypt, "");
static_assert(PAL_OperationDecrypt == kCCDecrypt, "");

static_assert(PAL_AlgorithmAES == kCCAlgorithmAES128, "");
static_assert(PAL_AlgorithmDES == kCCAlgorithmDES, "");
static_assert(PAL_Algorithm3DES == kCCAlgorithm3DES, "");
static_assert(PAL_AlgorithmRC2 == kCCAlgorithmRC2, "");

static_assert(PAL_ChainingModeECB == kCCModeECB, "");
static_assert(PAL_ChainingModeCBC == kCCModeCBC, "");

static_assert(PAL_PaddingModeNone == ccNoPadding, "");
static_assert(PAL_PaddingModePkcs7 == ccPKCS7Padding, "");

// No PAL_SymmetricOptions are currently mapped, so no asserts required.

extern "C" void AppleCryptoNative_CryptorFree(CCCryptorRef cryptor)
{
    if (cryptor != nullptr)
    {
        CCCryptorRelease(cryptor);
    }
}

extern "C" int AppleCryptoNative_CryptorCreate(PAL_SymmetricOperation operation,
                                               PAL_SymmetricAlgorithm algorithm,
                                               PAL_ChainingMode chainingMode,
                                               PAL_PaddingMode paddingMode,
                                               const uint8_t* pbKey,
                                               int32_t cbKey,
                                               const uint8_t* pbIv,
                                               PAL_SymmetricOptions options,
                                               CCCryptorRef* ppCryptorOut,
                                               int32_t* pccStatus)
{
    if (pccStatus == nullptr)
        return -1;

    *pccStatus = 0;

    if (pbKey == nullptr || cbKey < 1 || ppCryptorOut == nullptr)
        return -1;
    if (pbIv == nullptr && chainingMode != PAL_ChainingModeECB)
        return -1;

    // Ensure we aren't passing through things we don't understand
    assert(operation == PAL_OperationEncrypt || operation == PAL_OperationDecrypt);
    assert(algorithm == PAL_AlgorithmAES || algorithm == PAL_AlgorithmDES ||
           algorithm == PAL_Algorithm3DES || algorithm == PAL_AlgorithmRC2);
    assert(chainingMode == PAL_ChainingModeECB || chainingMode == PAL_ChainingModeCBC);
    assert(paddingMode == PAL_PaddingModeNone || paddingMode == PAL_PaddingModePkcs7);
    assert(options == 0);

    CCStatus status = CCCryptorCreateWithMode(operation,
                                              chainingMode,
                                              algorithm,
                                              paddingMode,
                                              pbIv,
                                              pbKey,
                                              static_cast<size_t>(cbKey),
                                              /* tweak is not supported */ nullptr,
                                              0,
                                              /* numRounds is not supported */ 0,
                                              options,
                                              ppCryptorOut);

    *pccStatus = status;
    return status == kCCSuccess;
}

extern "C" int AppleCryptoNative_CryptorUpdate(CCCryptorRef cryptor,
                                               const uint8_t* pbData,
                                               int32_t cbData,
                                               uint32_t* pbOutput,
                                               int32_t cbOutput,
                                               int32_t* pcbWritten,
                                               int32_t* pccStatus)
{
    if (pccStatus == nullptr)
        return -1;

    *pccStatus = 0;

    if (pbData == nullptr || cbData < 0 || pbOutput == nullptr || cbOutput < cbData || pcbWritten == nullptr)
        return -1;

    CCStatus status = CCCryptorUpdate(cryptor,
                                      pbData,
                                      static_cast<size_t>(cbData),
                                      pbOutput,
                                      static_cast<size_t>(cbOutput),
                                      reinterpret_cast<size_t*>(pcbWritten));

    *pccStatus = status;
    return status == kCCSuccess;
}

extern "C" int AppleCryptoNative_CryptorFinal(
    CCCryptorRef cryptor, uint8_t* pbOutput, int32_t cbOutput, int32_t* pcbWritten, int32_t* pccStatus)
{
    if (pccStatus == nullptr)
        return -1;

    *pccStatus = 0;

    if (pbOutput == nullptr || cbOutput < 0 || pcbWritten == nullptr)
        return -1;

    CCStatus status =
        CCCryptorFinal(cryptor, pbOutput, static_cast<size_t>(cbOutput), reinterpret_cast<size_t*>(pcbWritten));

    *pccStatus = status;
    return status == kCCSuccess;
}

extern "C" int AppleCryptoNative_CryptorReset(CCCryptorRef cryptor, const uint8_t* pbIv, int32_t* pccStatus)
{
    if (pccStatus == nullptr)
        return -1;

    *pccStatus = 0;

    if (cryptor == nullptr)
        return -1;

    CCStatus status = CCCryptorReset(cryptor, pbIv);
    *pccStatus = status;
    return status == kCCSuccess;
}
