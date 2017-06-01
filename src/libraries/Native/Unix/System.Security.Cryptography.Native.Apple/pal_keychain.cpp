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

extern "C" int32_t AppleCryptoNative_SetKeychainNeverLock(SecKeychainRef keychain)
{
    SecKeychainSettings settings = {
        .version = SEC_KEYCHAIN_SETTINGS_VERS1, .useLockInterval = 0, .lockOnSleep = 0, .lockInterval = INT_MAX,
    };

    return SecKeychainSetSettings(keychain, &settings);
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

    int32_t ret = 0;
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

static OSStatus DeleteInKeychain(CFTypeRef needle, SecKeychainRef haystack)
{
    CFMutableDictionaryRef query = CFDictionaryCreateMutable(
        kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);

    if (query == nullptr)
        return errSecAllocate;

    CFArrayRef searchList = CFArrayCreate(
        nullptr, const_cast<const void**>(reinterpret_cast<void**>(&haystack)), 1, &kCFTypeArrayCallBacks);

    if (searchList == nullptr)
    {
        CFRelease(query);
        return errSecAllocate;
    }

    CFArrayRef itemMatch = CFArrayCreate(nullptr, reinterpret_cast<const void**>(&needle), 1, &kCFTypeArrayCallBacks);

    if (itemMatch == nullptr)
    {
        CFRelease(searchList);
        CFRelease(query);
        return errSecAllocate;
    }

    CFDictionarySetValue(query, kSecReturnRef, kCFBooleanTrue);
    CFDictionarySetValue(query, kSecMatchSearchList, searchList);
    CFDictionarySetValue(query, kSecMatchItemList, itemMatch);
    CFDictionarySetValue(query, kSecClass, kSecClassIdentity);

    OSStatus status = SecItemDelete(query);

    if (status == errSecItemNotFound)
    {
        status = noErr;
    }

    if (status == noErr)
    {
        CFDictionarySetValue(query, kSecClass, kSecClassCertificate);
        status = SecItemDelete(query);
    }

    if (status == errSecItemNotFound)
    {
        status = noErr;
    }

    CFRelease(itemMatch);
    CFRelease(searchList);
    CFRelease(query);

    return status;
}

extern "C" int32_t
AppleCryptoNative_X509StoreAddCertificate(CFTypeRef certOrIdentity, SecKeychainRef keychain, int32_t* pOSStatus)
{
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (certOrIdentity == nullptr || keychain == nullptr || pOSStatus == nullptr)
        return -1;

    SecCertificateRef cert = nullptr;
    SecKeyRef privateKey = nullptr;

    auto inputType = CFGetTypeID(certOrIdentity);
    OSStatus status = noErr;

    if (inputType == SecCertificateGetTypeID())
    {
        cert = reinterpret_cast<SecCertificateRef>(const_cast<void*>(certOrIdentity));
        CFRetain(cert);
    }
    else if (inputType == SecIdentityGetTypeID())
    {
        SecIdentityRef identity = reinterpret_cast<SecIdentityRef>(const_cast<void*>(certOrIdentity));
        status = SecIdentityCopyCertificate(identity, &cert);

        if (status == noErr)
        {
            status = SecIdentityCopyPrivateKey(identity, &privateKey);
        }
    }
    else
    {
        return -1;
    }

    SecKeychainItemRef itemCopy = nullptr;

    // Copy the private key into the new keychain first, because it can fail due to
    // non-exportability. Certificates can only fail for things like I/O errors saving the
    // keychain back to disk.
    if (status == noErr && privateKey != nullptr)
    {
        status =
            SecKeychainItemCreateCopy(reinterpret_cast<SecKeychainItemRef>(privateKey), keychain, nullptr, &itemCopy);
    }

    if (status == errSecDuplicateItem)
    {
        status = noErr;
    }

    // Since we don't care about the itemCopy we'd ideally pass nullptr to SecKeychainItemCreateCopy,
    // but even though the documentation says it can be null, clang gives an error that null isn't
    // allowed.
    if (itemCopy != nullptr)
    {
        CFRelease(itemCopy);
        itemCopy = nullptr;
    }

    if (status == noErr && cert != nullptr)
    {
        status = SecKeychainItemCreateCopy(reinterpret_cast<SecKeychainItemRef>(cert), keychain, nullptr, &itemCopy);
    }

    if (status == errSecDuplicateItem)
    {
        status = noErr;
    }

    if (itemCopy != nullptr)
    {
        CFRelease(itemCopy);
        itemCopy = nullptr;
    }

    if (privateKey != nullptr)
    {
        CFRelease(privateKey);
        privateKey = nullptr;
    }

    if (cert != nullptr)
    {
        CFRelease(cert);
        cert = nullptr;
    }

    *pOSStatus = status;
    return status == noErr;
}

extern "C" int32_t
AppleCryptoNative_X509StoreRemoveCertificate(CFTypeRef certOrIdentity, SecKeychainRef keychain, int32_t* pOSStatus)
{
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (certOrIdentity == nullptr || keychain == nullptr || pOSStatus == nullptr)
        return -1;

    SecCertificateRef cert = nullptr;
    SecIdentityRef identity = nullptr;

    auto inputType = CFGetTypeID(certOrIdentity);
    OSStatus status = noErr;

    if (inputType == SecCertificateGetTypeID())
    {
        cert = reinterpret_cast<SecCertificateRef>(const_cast<void*>(certOrIdentity));
        CFRetain(cert);
    }
    else if (inputType == SecIdentityGetTypeID())
    {
        identity = reinterpret_cast<SecIdentityRef>(const_cast<void*>(certOrIdentity));
        status = SecIdentityCopyCertificate(identity, &cert);

        if (status != noErr)
        {
            *pOSStatus = status;
            return 0;
        }
    }
    else
    {
        return -1;
    }

    const int32_t kErrorUserTrust = 2;
    const int32_t kErrorAdminTrust = 3;

    CFArrayRef settings = nullptr;

    if (status == noErr)
    {
        status = SecTrustSettingsCopyTrustSettings(cert, kSecTrustSettingsDomainUser, &settings);
    }

    if (settings != nullptr)
    {
        CFRelease(settings);
        settings = nullptr;
    }

    if (status == noErr)
    {
        CFRelease(cert);
        return kErrorUserTrust;
    }

    status = SecTrustSettingsCopyTrustSettings(cert, kSecTrustSettingsDomainAdmin, &settings);

    if (settings != nullptr)
    {
        CFRelease(settings);
        settings = nullptr;
    }

    if (status == noErr)
    {
        CFRelease(cert);
        return kErrorAdminTrust;
    }

    *pOSStatus = DeleteInKeychain(cert, keychain);
    return *pOSStatus == noErr;
}
