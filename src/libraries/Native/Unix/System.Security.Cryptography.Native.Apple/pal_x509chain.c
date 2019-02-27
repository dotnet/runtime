// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_x509chain.h"

#ifndef kCFCoreFoundationVersionNumber10_12
#define kCFCoreFoundationVersionNumber10_12 1348.00
#endif

SecPolicyRef AppleCryptoNative_X509ChainCreateDefaultPolicy()
{
    // Disable on macOS 10.11 and lower due to segfaults within Security.framework.
    if (kCFCoreFoundationVersionNumber < kCFCoreFoundationVersionNumber10_12)
        return NULL;

    return SecPolicyCreateBasicX509();
}

SecPolicyRef AppleCryptoNative_X509ChainCreateRevocationPolicy()
{
    return SecPolicyCreateRevocation(kSecRevocationUseAnyAvailableMethod | kSecRevocationRequirePositiveResponse);
}

int32_t
AppleCryptoNative_X509ChainCreate(CFTypeRef certs, CFTypeRef policies, SecTrustRef* pTrustOut, int32_t* pOSStatus)
{
    if (pTrustOut != NULL)
        *pTrustOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (certs == NULL || policies == NULL || pTrustOut == NULL || pOSStatus == NULL)
        return -1;

    *pOSStatus = SecTrustCreateWithCertificates(certs, policies, pTrustOut);
    return *pOSStatus == noErr;
}

int32_t AppleCryptoNative_X509ChainEvaluate(SecTrustRef chain,
                                            CFDateRef cfEvaluationTime,
                                            bool allowNetwork,
                                            int32_t* pOSStatus)
{
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (chain == NULL || pOSStatus == NULL)
        return -1;

    *pOSStatus = SecTrustSetVerifyDate(chain, cfEvaluationTime);

    if (*pOSStatus != noErr)
    {
        return -2;
    }

    *pOSStatus = SecTrustSetNetworkFetchAllowed(chain, allowNetwork);

    if (*pOSStatus != noErr)
    {
        return -3;
    }

    SecTrustResultType trustResult;
    *pOSStatus = SecTrustEvaluate(chain, &trustResult);

    // If any error is reported from the function or the trust result value indicates that
    // otherwise was a failed chain build (vs an untrusted chain, etc) return failure and
    // we'll throw in the managed layer.  (but if we hit the "or" the message is "No error")
    if (*pOSStatus != noErr || trustResult == kSecTrustResultInvalid)
    {
        return 0;
    }

    // If: The chain was built with no errors (Unspecified)
    // Or: The chain was built and involved an explicitly trusted cert (Proceed)
    // Then: Success.
    if (trustResult == kSecTrustResultUnspecified || trustResult == kSecTrustResultProceed)
    {
        return 1;
    }

    // Should this be a different return code?
    return 1;
}

int64_t AppleCryptoNative_X509ChainGetChainSize(SecTrustRef chain)
{
    if (chain == NULL)
        return -1;

    return SecTrustGetCertificateCount(chain);
}

SecCertificateRef AppleCryptoNative_X509ChainGetCertificateAtIndex(SecTrustRef chain, int64_t index)
{
    if (chain == NULL || index < 0)
        return NULL;

    return SecTrustGetCertificateAtIndex(chain, index);
}

CFArrayRef AppleCryptoNative_X509ChainGetTrustResults(SecTrustRef chain)
{
    if (chain == NULL)
    {
        return NULL;
    }

    CFDictionaryRef detailsAndStuff = SecTrustCopyResult(chain);
    CFArrayRef details = NULL;

    if (detailsAndStuff != NULL)
    {
        CFTypeRef detailsPtr = CFDictionaryGetValue(detailsAndStuff, CFSTR("TrustResultDetails"));

        if (detailsPtr != NULL)
        {
            details = (CFArrayRef)detailsPtr;
            CFRetain(details);
        }
    }

    CFRelease(detailsAndStuff);
    return details;
}

static void MergeStatusCodes(CFTypeRef key, CFTypeRef value, void* context)
{
    // Windows (and therefore .NET) certificate status codes are defined on an int32_t.
    // The top 32 bits will be used to convey error information, the bottom 32 bits
    // as a data aggregator for the status codes.
    uint64_t* pStatus = (uint64_t*)context;

    if (key == NULL)
    {
        return;
    }

    // If any error code was already set.
    if (*pStatus > INT_MAX)
    {
        return;
    }

    if (CFGetTypeID(key) != CFStringGetTypeID())
    {
        *pStatus |= PAL_X509ChainErrorUnknownValueType;
        return;
    }

    (void)value;
    CFStringRef keyString = (CFStringRef)key;

    if (CFEqual(keyString, CFSTR("NotValidBefore")) || CFEqual(keyString, CFSTR("ValidLeaf")) ||
        CFEqual(keyString, CFSTR("ValidIntermediates")) || CFEqual(keyString, CFSTR("ValidRoot")) ||
        CFEqual(keyString, CFSTR("TemporalValidity")))
        *pStatus |= PAL_X509ChainNotTimeValid;
    else if (CFEqual(keyString, CFSTR("Revocation")))
        *pStatus |= PAL_X509ChainRevoked;
    else if (CFEqual(keyString, CFSTR("KeyUsage")))
        *pStatus |= PAL_X509ChainNotValidForUsage;
    else if (CFEqual(keyString, CFSTR("AnchorTrusted")))
        *pStatus |= PAL_X509ChainUntrustedRoot;
    else if (CFEqual(keyString, CFSTR("BasicConstraints")))
        *pStatus |= PAL_X509ChainInvalidBasicConstraints;
    else if (CFEqual(keyString, CFSTR("UsageConstraints")))
        *pStatus |= PAL_X509ChainExplicitDistrust;
    else if (CFEqual(keyString, CFSTR("RevocationResponseRequired")))
        *pStatus |= PAL_X509ChainRevocationStatusUnknown;
    else if (CFEqual(keyString, CFSTR("MissingIntermediate")))
        *pStatus |= PAL_X509ChainPartialChain;
    else if (CFEqual(keyString, CFSTR("WeakLeaf")) || CFEqual(keyString, CFSTR("WeakIntermediates")) ||
             CFEqual(keyString, CFSTR("WeakRoot")) || CFEqual(keyString, CFSTR("WeakKeySize")))
    {
        // Because we won't report this out of a chain built by .NET on Windows,
        // don't report it here.
        //
        // (On Windows CERT_CHAIN_PARA.pStrongSignPara is NULL, so "strongness" checks
        // are not performed).
    }
    else if (CFEqual(keyString, CFSTR("StatusCodes")))
    {
        // 10.13 added a StatusCodes value which may be a numeric rehashing of the string data.
        // It doesn't represent a new error code, and we're still getting the old ones, so
        // just ignore it for now.
    }
    else if (CFEqual(keyString, CFSTR("NonEmptySubject")) || CFEqual(keyString, CFSTR("GrayListedKey")) ||
             CFEqual(keyString, CFSTR("CTRequired")))
    {
        // Not a "problem" that we report.
    }
    else
    {
#if defined DEBUG || defined DEBUGGING_UNKNOWN_VALUE
        printf("Unknown Chain Status: %s\n", CFStringGetCStringPtr(keyString, CFStringGetSystemEncoding()));
#endif
        *pStatus |= PAL_X509ChainErrorUnknownValue;
    }
}

int32_t AppleCryptoNative_X509ChainGetStatusAtIndex(CFArrayRef details, int64_t index, int32_t* pdwStatus)
{
    if (pdwStatus != NULL)
        *pdwStatus = -1;

    if (details == NULL || index < 0 || pdwStatus == NULL)
    {
        return -1;
    }

    CFTypeRef element = CFArrayGetValueAtIndex(details, index);

    if (element == NULL)
    {
        return -2;
    }

    if (CFGetTypeID(element) != CFDictionaryGetTypeID())
    {
        return -2;
    }

    uint64_t status = 0;
    CFDictionaryRef statusCodes = (CFDictionaryRef)element;
    CFDictionaryApplyFunction(statusCodes, MergeStatusCodes, &status);

    *pdwStatus = (int32_t)status;
    return (int32_t)(status >> 32);
}

int32_t AppleCryptoNative_GetOSStatusForChainStatus(PAL_X509ChainStatusFlags chainStatusFlag)
{
    switch (chainStatusFlag)
    {
        case PAL_X509ChainNoError:
            return noErr;
        case PAL_X509ChainNotTimeValid:
            // This code is ambiguous.  Windows uses it for "not valid",
            // Apple makes a distinction between "not yet" and "not any more"
            // So this is just a fallback, because we should have determined it
            // via different means.`
            return errSecCertificateExpired;
        case PAL_X509ChainRevoked:
            return errSecCertificateRevoked;
        case PAL_X509ChainNotValidForUsage:
            return errSecKeyUsageIncorrect;
        case PAL_X509ChainUntrustedRoot:
            return errSecNotTrusted;
        case PAL_X509ChainInvalidBasicConstraints:
            return errSecNoBasicConstraints;
        case PAL_X509ChainPartialChain:
            return errSecCreateChainFailed;
        case PAL_X509ChainExplicitDistrust:
            return errSecTrustSettingDeny;
        case PAL_X509ChainRevocationStatusUnknown:
            return errSecIncompleteCertRevocationCheck;
        default:
            return errSecCoreFoundationUnknown;
    }
}
