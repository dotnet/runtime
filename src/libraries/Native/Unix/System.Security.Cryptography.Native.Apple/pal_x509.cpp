// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_x509.h"

static const int32_t kErrOutItemsNull = -3;
static const int32_t kErrOutItemsEmpty = -2;

extern "C" int32_t
AppleCryptoNative_X509DemuxAndRetainHandle(CFTypeRef handle, SecCertificateRef* pCertOut, SecIdentityRef* pIdentityOut)
{
    if (pCertOut != nullptr)
        *pCertOut = nullptr;
    if (pIdentityOut != nullptr)
        *pIdentityOut = nullptr;

    if (handle == nullptr || pCertOut == nullptr || pIdentityOut == nullptr)
        return kErrorBadInput;

    auto objectType = CFGetTypeID(handle);
    void* nonConstHandle = const_cast<void*>(handle);

    if (objectType == SecIdentityGetTypeID())
    {
        *pIdentityOut = reinterpret_cast<SecIdentityRef>(nonConstHandle);
    }
    else if (objectType == SecCertificateGetTypeID())
    {
        *pCertOut = reinterpret_cast<SecCertificateRef>(nonConstHandle);
    }
    else
    {
        return 0;
    }

    CFRetain(handle);
    return 1;
}

extern "C" int32_t
AppleCryptoNative_X509GetPublicKey(SecCertificateRef cert, SecKeyRef* pPublicKeyOut, int32_t* pOSStatusOut)
{
    if (pPublicKeyOut != nullptr)
        *pPublicKeyOut = nullptr;
    if (pOSStatusOut != nullptr)
        *pOSStatusOut = noErr;

    if (cert == nullptr || pPublicKeyOut == nullptr || pOSStatusOut == nullptr)
        return kErrorBadInput;

    *pOSStatusOut = SecCertificateCopyPublicKey(cert, pPublicKeyOut);
    return (*pOSStatusOut == noErr);
}

extern "C" PAL_X509ContentType AppleCryptoNative_X509GetContentType(uint8_t* pbData, int32_t cbData)
{
    if (pbData == nullptr || cbData < 0)
        return PAL_X509Unknown;

    CFDataRef cfData = CFDataCreateWithBytesNoCopy(nullptr, pbData, cbData, kCFAllocatorNull);

    if (cfData == nullptr)
        return PAL_X509Unknown;

    // The sniffing order is:
    // * X509 DER
    // * PKCS7 PEM/DER
    // * PKCS12 DER (or PEM if Apple has non-standard support for that)
    // * X509 PEM (or DER, but that already matched)
    //
    // If the X509 PEM check is done first SecItemImport will erroneously match
    // some PKCS#7 blobs and say they were certificates.
    //
    // Likewise, if the X509 DER check isn't done first, Apple will report it as
    // being a PKCS#7.
    SecCertificateRef certref = SecCertificateCreateWithData(nullptr, cfData);

    if (certref != nullptr)
    {
        CFRelease(certref);
        return PAL_Certificate;
    }

    SecExternalFormat dataFormat = kSecFormatPKCS7;
    SecExternalFormat actualFormat = dataFormat;
    SecExternalItemType itemType = kSecItemTypeAggregate;
    SecExternalItemType actualType = itemType;

    OSStatus osStatus = SecItemImport(cfData, nullptr, &actualFormat, &actualType, 0, nullptr, nullptr, nullptr);

    if (osStatus == noErr)
    {
        if (actualType == itemType && actualFormat == dataFormat)
        {
            return PAL_Pkcs7;
        }
    }

    dataFormat = kSecFormatPKCS12;
    actualFormat = dataFormat;
    itemType = kSecItemTypeAggregate;
    actualType = itemType;

    osStatus = SecItemImport(cfData, nullptr, &actualFormat, &actualType, 0, nullptr, nullptr, nullptr);

    if (osStatus == errSecPassphraseRequired)
    {
        dataFormat = kSecFormatPKCS12;
        actualFormat = dataFormat;
        itemType = kSecItemTypeAggregate;
        actualType = itemType;

        SecItemImportExportKeyParameters importParams = {};
        importParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;
        importParams.passphrase = CFSTR("");

        osStatus = SecItemImport(cfData, nullptr, &actualFormat, &actualType, 0, &importParams, nullptr, nullptr);

        CFRelease(importParams.passphrase);
        importParams.passphrase = nullptr;
    }

    if (osStatus == noErr || osStatus == errSecPkcs12VerifyFailure)
    {
        if (actualType == itemType && actualFormat == dataFormat)
        {
            return PAL_Pkcs12;
        }
    }

    dataFormat = kSecFormatX509Cert;
    actualFormat = dataFormat;
    itemType = kSecItemTypeCertificate;
    actualType = itemType;

    osStatus = SecItemImport(cfData, nullptr, &actualFormat, &actualType, 0, nullptr, nullptr, nullptr);

    if (osStatus == noErr)
    {
        if (actualType == itemType && actualFormat == dataFormat)
        {
            return PAL_Certificate;
        }
    }

    return PAL_X509Unknown;
}

static int32_t ProcessCertificateTypeReturn(CFArrayRef items, SecCertificateRef* pCertOut, SecIdentityRef* pIdentityOut)
{
    assert(pCertOut != nullptr && *pCertOut == nullptr);
    assert(pIdentityOut != nullptr && *pIdentityOut == nullptr);

    if (items == nullptr)
    {
        return kErrOutItemsNull;
    }

    CFIndex itemCount = CFArrayGetCount(items);

    if (itemCount == 0)
    {
        return kErrOutItemsEmpty;
    }

    CFTypeRef bestItem = nullptr;

    for (CFIndex i = 0; i < itemCount; i++)
    {
        CFTypeRef current = CFArrayGetValueAtIndex(items, i);
        auto currentItemType = CFGetTypeID(current);

        if (currentItemType == SecIdentityGetTypeID())
        {
            bestItem = current;
            break;
        }
        else if (bestItem == nullptr && currentItemType == SecCertificateGetTypeID())
        {
            bestItem = current;
        }
    }

    if (bestItem == nullptr)
    {
        return -13;
    }

    if (CFGetTypeID(bestItem) == SecCertificateGetTypeID())
    {
        CFRetain(bestItem);
        *pCertOut = reinterpret_cast<SecCertificateRef>(const_cast<void*>(bestItem));
        return 1;
    }

    if (CFGetTypeID(bestItem) == SecIdentityGetTypeID())
    {
        CFRetain(bestItem);
        *pIdentityOut = reinterpret_cast<SecIdentityRef>(const_cast<void*>(bestItem));

        return 1;
    }

    return -19;
}

extern "C" int32_t AppleCryptoNative_X509CopyCertFromIdentity(SecIdentityRef identity, SecCertificateRef* pCertOut)
{
    if (pCertOut != nullptr)
        *pCertOut = nullptr;

    // This function handles null inputs for both identity and cert.
    return SecIdentityCopyCertificate(identity, pCertOut);
}

extern "C" int32_t AppleCryptoNative_X509CopyPrivateKeyFromIdentity(SecIdentityRef identity, SecKeyRef* pPrivateKeyOut)
{
    if (pPrivateKeyOut != nullptr)
        *pPrivateKeyOut = nullptr;

    // This function handles null inputs for both identity and key
    return SecIdentityCopyPrivateKey(identity, pPrivateKeyOut);
}

static int32_t ReadX509(uint8_t* pbData,
                        int32_t cbData,
                        PAL_X509ContentType contentType,
                        CFStringRef cfPfxPassphrase,
                        SecKeychainRef keychain,
                        bool exportable,
                        SecCertificateRef* pCertOut,
                        SecIdentityRef* pIdentityOut,
                        CFArrayRef* pCollectionOut,
                        int32_t* pOSStatus)
{
    assert(pbData != nullptr);
    assert(cbData >= 0);
    assert((pCertOut == nullptr) == (pIdentityOut == nullptr));
    assert((pCertOut == nullptr) != (pCollectionOut == nullptr));

    SecExternalFormat dataFormat;
    SecExternalItemType itemType;
    int32_t ret = 0;
    CFArrayRef outItems = nullptr;
    CFMutableArrayRef keyAttributes = nullptr;
    SecKeychainRef importKeychain = nullptr;

    SecItemImportExportKeyParameters importParams = {};
    importParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;

    if (contentType == PAL_Certificate)
    {
        dataFormat = kSecFormatX509Cert;
        itemType = kSecItemTypeCertificate;
    }
    else if (contentType == PAL_Pkcs7)
    {
        dataFormat = kSecFormatPKCS7;
        itemType = kSecItemTypeAggregate;
    }
    else if (contentType == PAL_Pkcs12)
    {
        dataFormat = kSecFormatPKCS12;
        itemType = kSecItemTypeAggregate;

        importParams.passphrase = cfPfxPassphrase;
        importKeychain = keychain;

        if (keychain == nullptr)
        {
            return kErrorBadInput;
        }

        // if keyAttributes is nullptr then it uses SENSITIVE | EXTRACTABLE
        // so if !exportable was requested, assert SENSITIVE.
        if (!exportable)
        {
            keyAttributes = CFArrayCreateMutable(nullptr, 9, &kCFTypeArrayCallBacks);

            if (keyAttributes == nullptr)
            {
                *pOSStatus = errSecAllocate;
                return 0;
            }

            int32_t sensitiveValue = CSSM_KEYATTR_SENSITIVE;
            CFNumberRef sensitive = CFNumberCreate(nullptr, kCFNumberSInt32Type, &sensitiveValue);

            if (sensitive == nullptr)
            {
                CFRelease(keyAttributes);
                *pOSStatus = errSecAllocate;
                return 0;
            }

            CFArrayAppendValue(keyAttributes, sensitive);
            CFRelease(sensitive);

            importParams.keyAttributes = keyAttributes;
        }
    }
    else
    {
        *pOSStatus = errSecUnknownFormat;
        return 0;
    }

    CFDataRef cfData = CFDataCreateWithBytesNoCopy(nullptr, pbData, cbData, kCFAllocatorNull);

    if (cfData == nullptr)
    {
        *pOSStatus = errSecAllocate;
    }

    if (*pOSStatus == noErr)
    {
        *pOSStatus = SecItemImport(cfData, nullptr, &dataFormat, &itemType, 0, &importParams, keychain, &outItems);
    }

    if (contentType == PAL_Pkcs12 && *pOSStatus == errSecPassphraseRequired && cfPfxPassphrase == nullptr)
    {
        if (outItems != nullptr)
        {
            CFRelease(outItems);
            outItems = nullptr;
        }

        // Try again with the empty string passphrase.
        importParams.passphrase = CFSTR("");

        *pOSStatus = SecItemImport(cfData, nullptr, &dataFormat, &itemType, 0, &importParams, keychain, &outItems);

        CFRelease(importParams.passphrase);
        importParams.passphrase = nullptr;
    }

    if (*pOSStatus == noErr)
    {
        if (pCollectionOut != nullptr)
        {
            CFRetain(outItems);
            *pCollectionOut = outItems;
            ret = 1;
        }
        else
        {
            ret = ProcessCertificateTypeReturn(outItems, pCertOut, pIdentityOut);
        }
    }

    if (keyAttributes != nullptr)
    {
        CFRelease(keyAttributes);
    }

    if (outItems != nullptr)
    {
        // In the event this is returned via pCollectionOut it was already
        // CFRetain()ed, so always CFRelease here.
        CFRelease(outItems);
    }

    CFRelease(cfData);
    return ret;
}

extern "C" int32_t AppleCryptoNative_X509ImportCollection(uint8_t* pbData,
                                                          int32_t cbData,
                                                          PAL_X509ContentType contentType,
                                                          CFStringRef cfPfxPassphrase,
                                                          SecKeychainRef keychain,
                                                          int32_t exportable,
                                                          CFArrayRef* pCollectionOut,
                                                          int32_t* pOSStatus)
{
    if (pCollectionOut != nullptr)
        *pCollectionOut = nullptr;
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (pbData == nullptr || cbData < 0 || pCollectionOut == nullptr || pOSStatus == nullptr ||
        exportable != !!exportable)
    {
        return kErrorBadInput;
    }

    return ReadX509(pbData,
                    cbData,
                    contentType,
                    cfPfxPassphrase,
                    keychain,
                    static_cast<bool>(exportable),
                    nullptr,
                    nullptr,
                    pCollectionOut,
                    pOSStatus);
}

extern "C" int32_t AppleCryptoNative_X509ImportCertificate(uint8_t* pbData,
                                                           int32_t cbData,
                                                           PAL_X509ContentType contentType,
                                                           CFStringRef cfPfxPassphrase,
                                                           SecKeychainRef keychain,
                                                           int32_t exportable,
                                                           SecCertificateRef* pCertOut,
                                                           SecIdentityRef* pIdentityOut,
                                                           int32_t* pOSStatus)
{
    if (pCertOut != nullptr)
        *pCertOut = nullptr;
    if (pIdentityOut != nullptr)
        *pIdentityOut = nullptr;
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (pbData == nullptr || cbData < 0 || pCertOut == nullptr || pIdentityOut == nullptr || pOSStatus == nullptr ||
        exportable != !!exportable)
    {
        return kErrorBadInput;
    }

    return ReadX509(pbData,
                    cbData,
                    contentType,
                    cfPfxPassphrase,
                    keychain,
                    static_cast<bool>(exportable),
                    pCertOut,
                    pIdentityOut,
                    nullptr,
                    pOSStatus);
}

extern "C" int32_t AppleCryptoNative_X509ExportData(CFArrayRef data,
                                                    PAL_X509ContentType type,
                                                    CFStringRef cfExportPassphrase,
                                                    CFDataRef* pExportOut,
                                                    int32_t* pOSStatus)
{
    if (pExportOut != nullptr)
        *pExportOut = nullptr;
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (data == nullptr || pExportOut == nullptr || pOSStatus == nullptr)
    {
        return kErrorBadInput;
    }

    SecExternalFormat dataFormat = kSecFormatUnknown;

    switch (type)
    {
        case PAL_Pkcs7:
            dataFormat = kSecFormatPKCS7;
            break;
        case PAL_Pkcs12:
            dataFormat = kSecFormatPKCS12;
            break;
        default:
            return kErrorBadInput;
    }

    SecItemImportExportKeyParameters keyParams = {};
    keyParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;
    keyParams.passphrase = cfExportPassphrase;

    *pOSStatus = SecItemExport(data, dataFormat, 0, &keyParams, pExportOut);

    return *pOSStatus == noErr;
}

extern "C" int32_t AppleCryptoNative_X509GetRawData(SecCertificateRef cert, CFDataRef* ppDataOut, int32_t* pOSStatus)
{
    if (ppDataOut != nullptr)
        *ppDataOut = nullptr;
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (cert == nullptr || ppDataOut == nullptr || pOSStatus == nullptr)
        return kErrorBadInput;

    SecExternalFormat dataFormat = kSecFormatX509Cert;
    SecItemImportExportKeyParameters keyParams = {};
    keyParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;

    *pOSStatus = SecItemExport(cert, dataFormat, 0, &keyParams, ppDataOut);
    return (*pOSStatus == noErr);
}

static OSStatus AddKeyToKeychain(SecKeyRef privateKey, SecKeychainRef targetKeychain)
{
    // This is quite similar to pal_seckey's ExportImportKey, but
    // a) is used to put something INTO a keychain, instead of to take it out.
    // b) Doesn't assume that the input should be CFRelease()d and overwritten.
    // c) Doesn't return/emit the imported key reference.
    // d) Works on private keys.
    SecExternalFormat dataFormat = kSecFormatWrappedPKCS8;
    CFDataRef exportData = nullptr;

    SecItemImportExportKeyParameters keyParams = {};
    keyParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;
    keyParams.passphrase = CFSTR("ExportImportPassphrase");

    OSStatus status = SecItemExport(privateKey, dataFormat, 0, &keyParams, &exportData);

    SecExternalFormat actualFormat = dataFormat;
    SecExternalItemType actualType = kSecItemTypePrivateKey;
    CFArrayRef outItems = nullptr;

    if (status == noErr)
    {
        status =
            SecItemImport(exportData, nullptr, &actualFormat, &actualType, 0, &keyParams, targetKeychain, &outItems);
    }

    if (exportData != nullptr)
        CFRelease(exportData);

    CFRelease(keyParams.passphrase);
    keyParams.passphrase = nullptr;

    if (outItems != nullptr)
        CFRelease(outItems);

    return status;
}

extern "C" int32_t AppleCryptoNative_X509CopyWithPrivateKey(SecCertificateRef cert,
                                                            SecKeyRef privateKey,
                                                            SecKeychainRef targetKeychain,
                                                            SecIdentityRef* pIdentityOut,
                                                            int32_t* pOSStatus)
{
    if (pIdentityOut != nullptr)
        *pIdentityOut = nullptr;
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (cert == nullptr || privateKey == nullptr || targetKeychain == nullptr || pIdentityOut == nullptr ||
        pOSStatus == nullptr)
    {
        return -1;
    }

    SecKeychainRef keyKeychain = nullptr;

    OSStatus status = SecKeychainItemCopyKeychain(reinterpret_cast<SecKeychainItemRef>(privateKey), &keyKeychain);
    SecKeychainItemRef itemCopy = nullptr;

    // This only happens with an ephemeral key, so the keychain we're adding it to is temporary.
    if (status == errSecNoSuchKeychain)
    {
        status = AddKeyToKeychain(privateKey, targetKeychain);
    }

    if (itemCopy != nullptr)
    {
        CFRelease(itemCopy);
    }

    CFMutableDictionaryRef query = nullptr;

    if (status == noErr)
    {
        query = CFDictionaryCreateMutable(
            kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);

        if (query == nullptr)
        {
            status = errSecAllocate;
        }
    }

    CFArrayRef searchList = nullptr;

    if (status == noErr)
    {
        searchList = CFArrayCreate(
            nullptr, const_cast<const void**>(reinterpret_cast<void**>(&targetKeychain)), 1, &kCFTypeArrayCallBacks);

        if (searchList == nullptr)
        {
            status = errSecAllocate;
        }
    }

    CFArrayRef itemMatch = nullptr;

    if (status == noErr)
    {
        itemMatch = CFArrayCreate(
            nullptr, const_cast<const void**>(reinterpret_cast<void**>(&cert)), 1, &kCFTypeArrayCallBacks);

        if (itemMatch == nullptr)
        {
            status = errSecAllocate;
        }
    }

    CFTypeRef result = nullptr;

    if (status == noErr)
    {
        CFDictionarySetValue(query, kSecReturnRef, kCFBooleanTrue);
        CFDictionarySetValue(query, kSecMatchSearchList, searchList);
        CFDictionarySetValue(query, kSecMatchItemList, itemMatch);
        CFDictionarySetValue(query, kSecClass, kSecClassIdentity);

        status = SecItemCopyMatching(query, &result);

        if (status != noErr && result != nullptr)
        {
            CFRelease(result);
            result = nullptr;
        }

        bool added = false;

        if (status == errSecItemNotFound)
        {
            status = SecCertificateAddToKeychain(cert, targetKeychain);

            added = (status == noErr);
        }

        if (result == nullptr && status == noErr)
        {
            status = SecItemCopyMatching(query, &result);
        }

        if (result != nullptr && status == noErr)
        {

            if (CFGetTypeID(result) != SecIdentityGetTypeID())
            {
                status = errSecItemNotFound;
            }
            else
            {
                SecIdentityRef identity = reinterpret_cast<SecIdentityRef>(const_cast<void*>(result));
                CFRetain(identity);
                *pIdentityOut = identity;
            }
        }

        if (added)
        {
            // The same query that was used to find the identity can be used
            // to find/delete the certificate, as long as we fix the class to just the cert.
            CFDictionarySetValue(query, kSecClass, kSecClassCertificate);

            // Ignore the output status, there's no point in telling the user
            // that the cleanup failed, since that just makes them have a dirty keychain
            // AND their program didn't work.
            SecItemDelete(query);
        }
    }

    if (result != nullptr)
        CFRelease(result);

    if (itemMatch != nullptr)
        CFRelease(itemMatch);

    if (searchList != nullptr)
        CFRelease(searchList);

    if (query != nullptr)
        CFRelease(query);

    if (keyKeychain != nullptr)
        CFRelease(keyKeychain);

    *pOSStatus = status;
    return status == noErr;
}
