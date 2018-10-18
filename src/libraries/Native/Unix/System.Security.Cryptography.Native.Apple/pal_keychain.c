// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_keychain.h"
#include "pal_utilities.h"

int32_t AppleCryptoNative_SecKeychainItemCopyKeychain(SecKeychainItemRef item, SecKeychainRef* pKeychainOut)
{
    if (pKeychainOut != NULL)
        *pKeychainOut = NULL;

    if (item == NULL)
        return errSecNoSuchKeychain;

    CFTypeID itemType = CFGetTypeID(item);

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

int32_t AppleCryptoNative_SecKeychainCreate(const char* pathName,
                                            uint32_t passphraseLength,
                                            const uint8_t* passphraseUtf8,
                                            SecKeychainRef* pKeychainOut)
{
    return SecKeychainCreate(pathName, passphraseLength, passphraseUtf8, false, NULL, pKeychainOut);
}

int32_t AppleCryptoNative_SecKeychainDelete(SecKeychainRef keychain)
{
    return SecKeychainDelete(keychain);
}

int32_t AppleCryptoNative_SecKeychainCopyDefault(SecKeychainRef* pKeychainOut)
{
    if (pKeychainOut != NULL)
        *pKeychainOut = NULL;

    return SecKeychainCopyDefault(pKeychainOut);
}

int32_t AppleCryptoNative_SecKeychainOpen(const char* pszKeychainPath, SecKeychainRef* pKeychainOut)
{
    if (pKeychainOut != NULL)
        *pKeychainOut = NULL;

    if (pszKeychainPath == NULL)
        return errSecParam;

    return SecKeychainOpen(pszKeychainPath, pKeychainOut);
}

int32_t AppleCryptoNative_SecKeychainUnlock(SecKeychainRef keychain, uint32_t passphraseLength, const uint8_t* passphraseUtf8)
{
    return SecKeychainUnlock(keychain, passphraseLength, passphraseUtf8, true);
}

int32_t AppleCryptoNative_SetKeychainNeverLock(SecKeychainRef keychain)
{
    SecKeychainSettings settings = {
        .version = SEC_KEYCHAIN_SETTINGS_VERS1,
        .useLockInterval = 0,
        .lockOnSleep = 0,
        .lockInterval = INT_MAX,
    };

    return SecKeychainSetSettings(keychain, &settings);
}

static int32_t
EnumerateKeychain(SecKeychainRef keychain, CFStringRef matchType, CFArrayRef* pCertsOut, int32_t* pOSStatus)
{
    if (pCertsOut != NULL)
        *pCertsOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    assert(matchType != NULL);

    if (keychain == NULL || pCertsOut == NULL || pOSStatus == NULL)
        return -1;

    CFMutableDictionaryRef query = CFDictionaryCreateMutable(
        kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);

    if (query == NULL)
        return -2;

    int32_t ret = 0;
    CFTypeRef result = NULL;
    const void* constKeychain = keychain;
    CFArrayRef searchList = CFArrayCreate(NULL, &constKeychain, 1, &kCFTypeArrayCallBacks);

    if (searchList == NULL)
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
            if (result == NULL || CFGetTypeID(result) != CFArrayGetTypeID())
            {
                ret = -3;
            }
            else
            {
                CFRetain(result);
                *pCertsOut = (CFArrayRef)result;
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

    if (searchList != NULL)
        CFRelease(searchList);

    if (result != NULL)
        CFRelease(result);

    CFRelease(query);
    return ret;
}

int32_t AppleCryptoNative_SecKeychainEnumerateCerts(SecKeychainRef keychain, CFArrayRef* pCertsOut, int32_t* pOSStatus)
{
    return EnumerateKeychain(keychain, kSecClassCertificate, pCertsOut, pOSStatus);
}

int32_t AppleCryptoNative_SecKeychainEnumerateIdentities(SecKeychainRef keychain,
                                                         CFArrayRef* pIdentitiesOut,
                                                         int32_t* pOSStatus)
{
    return EnumerateKeychain(keychain, kSecClassIdentity, pIdentitiesOut, pOSStatus);
}

static OSStatus DeleteInKeychain(CFTypeRef needle, SecKeychainRef haystack)
{
    CFMutableDictionaryRef query = CFDictionaryCreateMutable(
        kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);

    if (query == NULL)
        return errSecAllocate;

    const void* constHaystack = haystack;
    CFArrayRef searchList = CFArrayCreate(NULL, &constHaystack, 1, &kCFTypeArrayCallBacks);

    if (searchList == NULL)
    {
        CFRelease(query);
        return errSecAllocate;
    }

    CFArrayRef itemMatch = CFArrayCreate(NULL, (const void**)(&needle), 1, &kCFTypeArrayCallBacks);

    if (itemMatch == NULL)
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

static bool IsCertInKeychain(CFTypeRef needle, SecKeychainRef haystack)
{
    CFMutableDictionaryRef query = CFDictionaryCreateMutable(
        kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);

    if (query == NULL)
        return errSecAllocate;

    const void* constHaystack = haystack;
    CFTypeRef result = NULL;
    CFArrayRef searchList = CFArrayCreate(NULL, &constHaystack, 1, &kCFTypeArrayCallBacks);

    if (searchList == NULL)
    {
        CFRelease(query);
        return errSecAllocate;
    }

    CFArrayRef itemMatch = CFArrayCreate(NULL, (const void**)(&needle), 1, &kCFTypeArrayCallBacks);

    if (itemMatch == NULL)
    {
        CFRelease(searchList);
        CFRelease(query);
        return errSecAllocate;
    }

    CFDictionarySetValue(query, kSecReturnRef, kCFBooleanTrue);
    CFDictionarySetValue(query, kSecMatchSearchList, searchList);
    CFDictionarySetValue(query, kSecMatchItemList, itemMatch);
    CFDictionarySetValue(query, kSecClass, kSecClassCertificate);
    OSStatus status = SecItemCopyMatching(query, &result);

    bool ret = true;

    if (status == errSecItemNotFound)
    {
        ret = false;
    }

    CFRelease(itemMatch);
    CFRelease(searchList);
    CFRelease(query);

    return ret;
}

static int32_t CheckTrustSettings(SecCertificateRef cert)
{
    const int32_t kErrorUserTrust = 2;
    const int32_t kErrorAdminTrust = 3;

    OSStatus status = noErr;
    CFArrayRef settings = NULL;
    if (status == noErr)
    {
        status = SecTrustSettingsCopyTrustSettings(cert, kSecTrustSettingsDomainUser, &settings);
    }

    if (settings != NULL)
    {
        CFRelease(settings);
        settings = NULL;
    }

    if (status == noErr)
    {
        CFRelease(cert);
        return kErrorUserTrust;
    }

    status = SecTrustSettingsCopyTrustSettings(cert, kSecTrustSettingsDomainAdmin, &settings);

    if (settings != NULL)
    {
        CFRelease(settings);
        settings = NULL;
    }

    if (status == noErr)
    {
        CFRelease(cert);
        return kErrorAdminTrust;
    }

    return 0;
}

int32_t AppleCryptoNative_X509StoreAddCertificate(CFTypeRef certOrIdentity, SecKeychainRef keychain, int32_t* pOSStatus)
{
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (certOrIdentity == NULL || keychain == NULL || pOSStatus == NULL)
        return -1;

    SecCertificateRef cert = NULL;
    SecKeyRef privateKey = NULL;

    CFTypeID inputType = CFGetTypeID(certOrIdentity);
    OSStatus status = noErr;

    if (inputType == SecCertificateGetTypeID())
    {
        cert = (SecCertificateRef)CONST_CAST(void*, certOrIdentity);
        CFRetain(cert);
    }
    else if (inputType == SecIdentityGetTypeID())
    {
        SecIdentityRef identity = (SecIdentityRef)CONST_CAST(void*, certOrIdentity);
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

    SecKeychainItemRef itemCopy = NULL;

    // Copy the private key into the new keychain first, because it can fail due to
    // non-exportability. Certificates can only fail for things like I/O errors saving the
    // keychain back to disk.
    if (status == noErr && privateKey != NULL)
    {
        status = SecKeychainItemCreateCopy((SecKeychainItemRef)privateKey, keychain, NULL, &itemCopy);
    }

    if (status == errSecDuplicateItem)
    {
        status = noErr;
    }

    // Since we don't care about the itemCopy we'd ideally pass NULL to SecKeychainItemCreateCopy,
    // but even though the documentation says it can be null, clang gives an error that null isn't
    // allowed.
    if (itemCopy != NULL)
    {
        CFRelease(itemCopy);
        itemCopy = NULL;
    }

    if (status == noErr && cert != NULL)
    {
        status = SecKeychainItemCreateCopy((SecKeychainItemRef)cert, keychain, NULL, &itemCopy);
    }

    if (status == errSecDuplicateItem)
    {
        status = noErr;
    }

    if (itemCopy != NULL)
    {
        CFRelease(itemCopy);
        itemCopy = NULL;
    }

    if (privateKey != NULL)
    {
        CFRelease(privateKey);
        privateKey = NULL;
    }

    if (cert != NULL)
    {
        CFRelease(cert);
        cert = NULL;
    }

    *pOSStatus = status;
    return status == noErr;
}

int32_t
AppleCryptoNative_X509StoreRemoveCertificate(CFTypeRef certOrIdentity, SecKeychainRef keychain, uint8_t isReadOnlyMode, int32_t* pOSStatus)
{
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (certOrIdentity == NULL || keychain == NULL || pOSStatus == NULL)
        return -1;

    SecCertificateRef cert = NULL;
    SecIdentityRef identity = NULL;

    CFTypeID inputType = CFGetTypeID(certOrIdentity);
    OSStatus status = noErr;

    if (inputType == SecCertificateGetTypeID())
    {
        cert = (SecCertificateRef)CONST_CAST(void*, certOrIdentity);
        CFRetain(cert);
    }
    else if (inputType == SecIdentityGetTypeID())
    {
        identity = (SecIdentityRef)CONST_CAST(void*, certOrIdentity);
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

    const int32_t kErrorReadonlyDelete = 4;

    int32_t ret = 0;

    if (isReadOnlyMode)
    {
        ret = kErrorReadonlyDelete;
    }
    else
    {
        ret = CheckTrustSettings(cert);
    }

    if (ret != 0)
    {
        if (!IsCertInKeychain(cert, keychain))
        {
            return 1;
        }

        return ret;
    }

    *pOSStatus = DeleteInKeychain(cert, keychain);
    CFRelease(cert);
    return *pOSStatus == noErr;
}
