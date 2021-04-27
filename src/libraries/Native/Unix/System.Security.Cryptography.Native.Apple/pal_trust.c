// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_trust.h"
#include "pal_utilities.h"

#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
static bool CheckTrustMatch(SecCertificateRef cert,
                            SecTrustSettingsDomain domain,
                            SecTrustSettingsResult result,
                            OSStatus* pOSStatus)
{
    CFArrayRef settings = NULL;
    *pOSStatus = SecTrustSettingsCopyTrustSettings(cert, domain, &settings);
    bool isMatch = false;

    if (*pOSStatus == noErr && settings != NULL)
    {
        CFIndex count = CFArrayGetCount(settings);

        if (count == 0)
        {
            *pOSStatus = noErr;
            // An empty array means that it counts as "Trust Root",
            // so we match if (and only if) we were asking for that.
            isMatch = (result == kSecTrustSettingsResultTrustRoot);
        }
        else
        {
            CFTypeID dictionaryTypeId = CFDictionaryGetTypeID();
            CFTypeID numberTypeId = CFNumberGetTypeID();

            for (CFIndex i = 0; i < count; i++)
            {
                CFTypeRef obj = CFArrayGetValueAtIndex(settings, i);

                if (CFGetTypeID(obj) != dictionaryTypeId)
                {
                    continue;
                }

                CFDictionaryRef dict = (CFDictionaryRef)obj;

                if (CFDictionaryGetCount(dict) > 1)
                {
                    // This dictionary has constraints. A particular SecTrust evaluation
                    // might or might not make it apply. If the only extra key was a policy
                    // restriction, then matching on the basic X.509 policy might be fair,
                    // but it's not an obvious check with little expectation of applying.
                    //
                    // This may result in X509Chain reporting valid when enumerating the
                    // two root stores doesn't agree.
                    continue;
                }

                CFTypeRef val = CFDictionaryGetValue(dict, kSecTrustSettingsResult);

                if (val != NULL && CFGetTypeID(val) == numberTypeId)
                {
                    CFNumberRef cfNum = (CFNumberRef)val;
                    SecTrustSettingsResult trustValue;
                    memset(&trustValue, 0, sizeof(SecTrustSettingsResult));

                    if (CFNumberGetValue(cfNum, kCFNumberSInt32Type, &trustValue))
                    {
                        isMatch = (trustValue == result);
                        break;
                    }
                }
            }
        }
    }

    if (settings != NULL)
        CFRelease(settings);

    return isMatch;
}

static int32_t EnumerateTrust(SecTrustSettingsDomain domain,
                              SecTrustSettingsResult result,
                              CFMutableArrayRef* pCertsRef,
                              int32_t* pOSStatus)
{
    CFMutableArrayRef outArray;

    if (*pCertsRef != NULL)
    {
        outArray = *pCertsRef;
    }
    else
    {
        outArray = CFArrayCreateMutable(NULL, 0, &kCFTypeArrayCallBacks);
        *pCertsRef = outArray;
    }

    if (outArray == NULL)
    {
        *pOSStatus = errSecAllocate;
        return 0;
    }

    CFArrayRef certsWithTrusts = NULL;
    *pOSStatus = SecTrustSettingsCopyCertificates(domain, &certsWithTrusts);

    if (*pOSStatus == noErr)
    {
        CFIndex count = CFArrayGetCount(certsWithTrusts);
        CFTypeID certTypeId = SecCertificateGetTypeID();

        for (CFIndex i = 0; i < count; i++)
        {
            CFTypeRef obj = CFArrayGetValueAtIndex(certsWithTrusts, i);

            if (CFGetTypeID(obj) != certTypeId)
            {
                continue;
            }

            SecCertificateRef cert = (SecCertificateRef)CONST_CAST(void*, obj);
            bool isMatch = CheckTrustMatch(cert, domain, result, pOSStatus);

            if (*pOSStatus != noErr)
            {
                break;
            }

            if (isMatch)
            {
                CFArrayAppendValue(outArray, cert);
            }
        }
    }
    else if (*pOSStatus == errSecNoTrustSettings)
    {
        // If there are no trust settings at the specified domain,
        // return the empty list as OK.
        *pOSStatus = noErr;
    }

    if (certsWithTrusts != NULL)
    {
        CFRelease(certsWithTrusts);
    }

    int32_t ret = *pOSStatus == noErr;

    // Note that on a second call the array from the first call will get freed
    // if an error is encountered.
    if (ret == 0 || CFArrayGetCount(outArray) == 0)
    {
        CFRelease(outArray);
        *pCertsRef = NULL;
    }

    return ret;
}

int32_t AppleCryptoNative_StoreEnumerateUserRoot(CFArrayRef* pCertsOut, int32_t* pOSStatusOut)
{
    if (pCertsOut != NULL)
        *pCertsOut = NULL;
    if (pOSStatusOut != NULL)
        *pOSStatusOut = noErr;

    if (pCertsOut == NULL || pOSStatusOut == NULL)
        return -1;

    CFMutableArrayRef pCertsRef = NULL;
    int32_t ret;

    ret = EnumerateTrust(kSecTrustSettingsDomainUser, kSecTrustSettingsResultTrustRoot, &pCertsRef, pOSStatusOut);

    *pCertsOut = pCertsRef;

    return ret;
}

int32_t AppleCryptoNative_StoreEnumerateMachineRoot(CFArrayRef* pCertsOut, int32_t* pOSStatusOut)
{
    if (pCertsOut != NULL)
        *pCertsOut = NULL;
    if (pOSStatusOut != NULL)
        *pOSStatusOut = noErr;

    if (pCertsOut == NULL || pOSStatusOut == NULL)
        return -1;

    CFMutableArrayRef pCertsRef = NULL;
    int32_t ret;

    ret = EnumerateTrust(kSecTrustSettingsDomainAdmin, kSecTrustSettingsResultTrustRoot, &pCertsRef, pOSStatusOut);

    if (ret == 1)
    {
        ret = EnumerateTrust(kSecTrustSettingsDomainSystem, kSecTrustSettingsResultTrustRoot, &pCertsRef, pOSStatusOut);
    }

    *pCertsOut = pCertsRef;

    return ret;
}

int32_t AppleCryptoNative_StoreEnumerateUserDisallowed(CFArrayRef* pCertsOut, int32_t* pOSStatusOut)
{
    if (pCertsOut != NULL)
        *pCertsOut = NULL;
    if (pOSStatusOut != NULL)
        *pOSStatusOut = noErr;

    if (pCertsOut == NULL || pOSStatusOut == NULL)
        return -1;

    CFMutableArrayRef pCertsRef = NULL;
    int ret;

    ret = EnumerateTrust(kSecTrustSettingsDomainUser, kSecTrustSettingsResultDeny, &pCertsRef, pOSStatusOut);

    *pCertsOut = pCertsRef;

    return ret;
}

int32_t AppleCryptoNative_StoreEnumerateMachineDisallowed(CFArrayRef* pCertsOut, int32_t* pOSStatusOut)
{
    if (pCertsOut != NULL)
        *pCertsOut = NULL;
    if (pOSStatusOut != NULL)
        *pOSStatusOut = noErr;

    if (pCertsOut == NULL || pOSStatusOut == NULL)
        return -1;

    CFMutableArrayRef pCertsRef = NULL;
    int32_t ret;

    ret = EnumerateTrust(kSecTrustSettingsDomainAdmin, kSecTrustSettingsResultDeny, &pCertsRef, pOSStatusOut);

    if (ret == 1)
    {
        ret = EnumerateTrust(kSecTrustSettingsDomainSystem, kSecTrustSettingsResultDeny, &pCertsRef, pOSStatusOut);
    }

    *pCertsOut = pCertsRef;

    return ret;
}
#endif
