// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_keychain.h"

extern "C" int32_t AppleCryptoNative_SecKeychainItemCopyKeychain(SecKeychainItemRef item, SecKeychainRef* pKeychainOut)
{
    if (pKeychainOut != nullptr)
        *pKeychainOut = nullptr;

    if (item == nullptr)
        return errSecNoSuchKeychain;

    auto itemType = CFGetTypeID(item);

    if (itemType == SecKeyGetTypeID() || itemType == SecIdentityGetTypeID() || itemType == SecCertificateGetTypeID())
    {
        OSStatus status = SecKeychainItemCopyKeychain(item, pKeychainOut);

        if (status == noErr)
        {
            return status;
        }

        // Acceptable error codes
        if (status == errSecNoSuchKeychain || status == errSecInvalidItemRef)
        {
            return noErr;
        }

        return status;
    }

    return errSecParam;
}

extern "C" int32_t AppleCryptoNative_SecKeychainCreate(const char* pathName,
                                                       uint32_t passphraseLength,
                                                       const uint8_t* passphraseUtf8,
                                                       SecKeychainRef* pKeychainOut)
{
    return SecKeychainCreate(pathName, passphraseLength, passphraseUtf8, false, nullptr, pKeychainOut);
}

extern "C" int32_t AppleCryptoNative_SecKeychainDelete(SecKeychainRef keychain)
{
    return SecKeychainDelete(keychain);
}

extern "C" int32_t AppleCryptoNative_SecKeychainCopyDefault(SecKeychainRef* pKeychainOut)
{
    if (pKeychainOut != nullptr)
        *pKeychainOut = nullptr;

    return SecKeychainCopyDefault(pKeychainOut);
}

extern "C" int32_t AppleCryptoNative_SecKeychainOpen(const char* pszKeychainPath, SecKeychainRef* pKeychainOut)
{
    if (pKeychainOut != nullptr)
        *pKeychainOut = nullptr;

    if (pszKeychainPath == nullptr)
        return errSecParam;

    return SecKeychainOpen(pszKeychainPath, pKeychainOut);
}

static int32_t
EnumerateKeychain(SecKeychainRef keychain, CFStringRef matchType, CFArrayRef* pCertsOut, int32_t* pOSStatus)
{
    if (pCertsOut != nullptr)
        *pCertsOut = nullptr;
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    assert(matchType != nullptr);

    if (keychain == nullptr || pCertsOut == nullptr || pOSStatus == nullptr)
        return -1;

    CFMutableDictionaryRef query = CFDictionaryCreateMutable(
        kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);

    if (query == nullptr)
        return -2;

    int ret = 0;
    CFTypeRef result = nullptr;
    CFArrayRef searchList = CFArrayCreate(
        nullptr, const_cast<const void**>(reinterpret_cast<void**>(&keychain)), 1, &kCFTypeArrayCallBacks);

    if (searchList == nullptr)
    {
        ret = -3;
    }
    else
    {
        CFDictionarySetValue(query, kSecReturnRef, kCFBooleanTrue);
        CFDictionarySetValue(query, kSecMatchLimit, kSecMatchLimitAll);
        CFDictionarySetValue(query, kSecClass, matchType);
        CFDictionarySetValue(query, kSecMatchSearchList, searchList);

        *pOSStatus = SecItemCopyMatching(query, &result);

        if (*pOSStatus == noErr)
        {
            if (result == nullptr || CFGetTypeID(result) != CFArrayGetTypeID())
            {
                ret = -3;
            }
            else
            {
                CFRetain(result);
                *pCertsOut = reinterpret_cast<CFArrayRef>(result);
                ret = 1;
            }
        }
        else if (*pOSStatus == errSecItemNotFound)
        {
            *pOSStatus = noErr;
            ret = 1;
        }
        else
        {
            ret = 0;
        }
    }

    if (searchList != nullptr)
        CFRelease(searchList);

    if (result != nullptr)
        CFRelease(result);

    CFRelease(query);
    return ret;
}

extern "C" int32_t
AppleCryptoNative_SecKeychainEnumerateCerts(SecKeychainRef keychain, CFArrayRef* pCertsOut, int32_t* pOSStatus)
{
    return EnumerateKeychain(keychain, kSecClassCertificate, pCertsOut, pOSStatus);
}

extern "C" int32_t AppleCryptoNative_SecKeychainEnumerateIdentities(SecKeychainRef keychain,
                                                                    CFArrayRef* pIdentitiesOut,
                                                                    int32_t* pOSStatus)
{
    return EnumerateKeychain(keychain, kSecClassIdentity, pIdentitiesOut, pOSStatus);
}
