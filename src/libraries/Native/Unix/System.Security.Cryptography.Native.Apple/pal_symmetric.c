// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_symmetric.h"
#include "pal_utilities.h"

#include <assert.h>

c_static_assert(PAL_OperationEncrypt == kCCEncrypt);
c_static_assert(PAL_OperationDecrypt == kCCDecrypt);

c_static_assert(PAL_AlgorithmAES == kCCAlgorithmAES128);
c_static_assert(PAL_AlgorithmDES == kCCAlgorithmDES);
c_static_assert(PAL_Algorithm3DES == kCCAlgorithm3DES);
c_static_assert(PAL_AlgorithmRC2 == kCCAlgorithmRC2);

c_static_assert(PAL_ChainingModeECB == kCCModeECB);
c_static_assert(PAL_ChainingModeCBC == kCCModeCBC);

c_static_assert(PAL_PaddingModeNone == ccNoPadding);
c_static_assert(PAL_PaddingModePkcs7 == ccPKCS7Padding);

// No PAL_SymmetricOptions are currently mapped, so no asserts required.

void AppleCryptoNative_CryptorFree(CCCryptorRef cryptor)
{
    if (cryptor != NULL)
    {
        CCCryptorRelease(cryptor);
    }
}

int32_t AppleCryptoNative_CryptorCreate(PAL_SymmetricOperation operation,
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
    if (pccStatus == NULL)
        return -1;

    *pccStatus = 0;

    if (pbKey == NULL || cbKey < 1 || ppCryptorOut == NULL)
        return -1;
    if (pbIv == NULL && chainingMode != PAL_ChainingModeECB)
        return -1;

    // Ensure we aren't passing through things we don't understand
    assert(operation == PAL_OperationEncrypt || operation == PAL_OperationDecrypt);
    assert(algorithm == PAL_AlgorithmAES || algorithm == PAL_AlgorithmDES || algorithm == PAL_Algorithm3DES ||
           algorithm == PAL_AlgorithmRC2);
    assert(chainingMode == PAL_ChainingModeECB || chainingMode == PAL_ChainingModeCBC);
    assert(paddingMode == PAL_PaddingModeNone || paddingMode == PAL_PaddingModePkcs7);
    assert(options == 0);

    CCStatus status = CCCryptorCreateWithMode(operation,
                                              chainingMode,
                                              algorithm,
                                              paddingMode,
                                              pbIv,
                                              pbKey,
                                              (size_t)cbKey,
                                              /* tweak is not supported */ NULL,
                                              0,
                                              /* numRounds is not supported */ 0,
                                              options,
                                              ppCryptorOut);

    *pccStatus = status;
    return status == kCCSuccess;
}

int32_t AppleCryptoNative_CryptorUpdate(CCCryptorRef cryptor,
                                        const uint8_t* pbData,
                                        int32_t cbData,
                                        uint32_t* pbOutput,
                                        int32_t cbOutput,
                                        int32_t* pcbWritten,
                                        int32_t* pccStatus)
{
    if (pccStatus == NULL)
        return -1;

    *pccStatus = 0;

    if (pbData == NULL || cbData < 0 || pbOutput == NULL || cbOutput < cbData || pcbWritten == NULL)
        return -1;

    size_t sizeWritten = 0;
    CCStatus status = CCCryptorUpdate(cryptor,
                                      pbData,
                                      (size_t)cbData,
                                      pbOutput,
                                      (size_t)cbOutput,
                                      &sizeWritten);

    // Safe because sizeWritten is bounded by cbOutput.
    *pcbWritten = SizeTToInt32(sizeWritten);
    *pccStatus = status;
    return status == kCCSuccess;
}

int32_t AppleCryptoNative_CryptorFinal(
    CCCryptorRef cryptor, uint8_t* pbOutput, int32_t cbOutput, int32_t* pcbWritten, int32_t* pccStatus)
{
    if (pccStatus == NULL)
        return -1;

    *pccStatus = 0;

    if (pbOutput == NULL || cbOutput < 0 || pcbWritten == NULL)
        return -1;

    size_t sizeWritten = 0;
    CCStatus status =
        CCCryptorFinal(cryptor, pbOutput, (size_t)cbOutput, &sizeWritten);

    // Safe because sizeWritten is bounded by cbOutput.
    *pcbWritten = SizeTToInt32(sizeWritten);
    *pccStatus = status;
    return status == kCCSuccess;
}

int32_t AppleCryptoNative_CryptorReset(CCCryptorRef cryptor, const uint8_t* pbIv, int32_t* pccStatus)
{
    if (pccStatus == NULL)
        return -1;

    *pccStatus = 0;

    if (cryptor == NULL)
        return -1;

    // 10.13 Beta reports an error when resetting ECB, which is the only mode which has a null IV.
    if (pbIv == NULL)
        return 1;

    CCStatus status = CCCryptorReset(cryptor, pbIv);
    *pccStatus = status;
    return status == kCCSuccess;
}
