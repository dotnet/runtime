// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509.h"
#include "pal_utilities.h"
#include <dlfcn.h>
#include <pthread.h>

static const int32_t kErrOutItemsNull = -3;
static const int32_t kErrOutItemsEmpty = -2;
static pthread_once_t once = PTHREAD_ONCE_INIT;
static SecKeyRef (*secCertificateCopyKey)(SecCertificateRef);
static OSStatus (*secCertificateCopyPublicKey)(SecCertificateRef, SecKeyRef*);

int32_t
AppleCryptoNative_X509DemuxAndRetainHandle(CFTypeRef handle, SecCertificateRef* pCertOut, SecIdentityRef* pIdentityOut)
{
    if (pCertOut != NULL)
        *pCertOut = NULL;
    if (pIdentityOut != NULL)
        *pIdentityOut = NULL;

    if (handle == NULL || pCertOut == NULL || pIdentityOut == NULL)
        return kErrorBadInput;

    CFTypeID objectType = CFGetTypeID(handle);

    if (objectType == SecIdentityGetTypeID())
    {
        *pIdentityOut = (SecIdentityRef)CONST_CAST(void *, handle);
    }
    else if (objectType == SecCertificateGetTypeID())
    {
        *pCertOut = (SecCertificateRef)CONST_CAST(void *, handle);
    }
    else
    {
        return 0;
    }

    CFRetain(handle);
    return 1;
}

static void InitCertificateCopy()
{
    secCertificateCopyKey = (SecKeyRef (*)(SecCertificateRef))dlsym(RTLD_DEFAULT, "SecCertificateCopyKey");
    secCertificateCopyPublicKey = (OSStatus (*)(SecCertificateRef, SecKeyRef*))dlsym(RTLD_DEFAULT, "SecCertificateCopyPublicKey");
}

int32_t
AppleCryptoNative_X509GetPublicKey(SecCertificateRef cert, SecKeyRef* pPublicKeyOut, int32_t* pOSStatusOut)
{
    if (pPublicKeyOut != NULL)
        *pPublicKeyOut = NULL;
    if (pOSStatusOut != NULL)
        *pOSStatusOut = noErr;

    if (cert == NULL || pPublicKeyOut == NULL || pOSStatusOut == NULL)
        return kErrorUnknownState;

    pthread_once (&once, InitCertificateCopy);
    // SecCertificateCopyPublicKey was deprecated in 10.14, so use SecCertificateCopyKey on the systems that have it (10.14+),
    // and SecCertificateCopyPublicKey on the systems that don’t.
    if (secCertificateCopyKey != NULL)
    {
        *pPublicKeyOut = (*secCertificateCopyKey)(cert);
    }
    else if (secCertificateCopyPublicKey != NULL)
    {
        *pOSStatusOut = (*secCertificateCopyPublicKey)(cert, pPublicKeyOut);
    }
    else
    {
        return kErrorBadInput;
    }
    return (*pOSStatusOut == noErr);
}

PAL_X509ContentType AppleCryptoNative_X509GetContentType(uint8_t* pbData, int32_t cbData)
{
    if (pbData == NULL || cbData < 0)
        return PAL_X509Unknown;

    CFDataRef cfData = CFDataCreateWithBytesNoCopy(NULL, pbData, cbData, kCFAllocatorNull);

    if (cfData == NULL)
        return PAL_X509Unknown;

    // The sniffing order is:
    // * X509 DER
    // * PKCS7 PEM/DER
    // * PKCS12 DER (or PEM if Apple has non-standard support for that)
    // * X509 PEM or PEM aggregate (or DER, but that already matched)
    //
    // If the X509 PEM check is done first SecItemImport will erroneously match
    // some PKCS#7 blobs and say they were certificates.
    //
    // Likewise, if the X509 DER check isn't done first, Apple will report it as
    // being a PKCS#7.
    SecCertificateRef certref = SecCertificateCreateWithData(NULL, cfData);

    if (certref != NULL)
    {
        CFRelease(cfData);
        CFRelease(certref);
        return PAL_Certificate;
    }

#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
    SecExternalFormat dataFormat = kSecFormatPKCS7;
    SecExternalFormat actualFormat = dataFormat;
    SecExternalItemType itemType = kSecItemTypeAggregate;
    SecExternalItemType actualType = itemType;

    OSStatus osStatus = SecItemImport(cfData, NULL, &actualFormat, &actualType, 0, NULL, NULL, NULL);

    if (osStatus == noErr)
    {
        if (actualType == itemType && actualFormat == dataFormat)
        {
            CFRelease(cfData);
            return PAL_Pkcs7;
        }
    }

    dataFormat = kSecFormatPKCS12;
    actualFormat = dataFormat;
    itemType = kSecItemTypeAggregate;
    actualType = itemType;

    osStatus = SecItemImport(cfData, NULL, &actualFormat, &actualType, 0, NULL, NULL, NULL);

    if (osStatus == errSecPassphraseRequired)
    {
        dataFormat = kSecFormatPKCS12;
        actualFormat = dataFormat;
        itemType = kSecItemTypeAggregate;
        actualType = itemType;

        SecItemImportExportKeyParameters importParams;
        memset(&importParams, 0, sizeof(SecItemImportExportKeyParameters));

        importParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;
        importParams.passphrase = CFSTR("");

        osStatus = SecItemImport(cfData, NULL, &actualFormat, &actualType, 0, &importParams, NULL, NULL);

        CFRelease(importParams.passphrase);
        importParams.passphrase = NULL;
    }

    if (osStatus == noErr || osStatus == errSecPkcs12VerifyFailure)
    {
        if (actualType == itemType && actualFormat == dataFormat)
        {
            CFRelease(cfData);
            return PAL_Pkcs12;
        }
    }

    dataFormat = kSecFormatX509Cert;
    actualFormat = dataFormat;
    itemType = kSecItemTypeCertificate;
    actualType = itemType;

    osStatus = SecItemImport(cfData, NULL, &actualFormat, &actualType, 0, NULL, NULL, NULL);

    if (osStatus == noErr)
    {
        if ((actualType == itemType && actualFormat == dataFormat) ||
            (actualType == kSecItemTypeAggregate && actualFormat == kSecFormatPEMSequence))
        {
            CFRelease(cfData);
            return PAL_Certificate;
        }
    }
#endif

    CFRelease(cfData);
    return PAL_X509Unknown;
}

static int32_t ProcessCertificateTypeReturn(CFArrayRef items, SecCertificateRef* pCertOut, SecIdentityRef* pIdentityOut)
{
    assert(pCertOut != NULL && *pCertOut == NULL);
    assert(pIdentityOut != NULL && *pIdentityOut == NULL);

    if (items == NULL)
    {
        return kErrOutItemsNull;
    }

    CFIndex itemCount = CFArrayGetCount(items);

    if (itemCount == 0)
    {
        return kErrOutItemsEmpty;
    }

    CFTypeRef bestItem = NULL;

    for (CFIndex i = 0; i < itemCount; i++)
    {
        CFTypeRef current = CFArrayGetValueAtIndex(items, i);
        CFTypeID currentItemType = CFGetTypeID(current);

        if (currentItemType == SecIdentityGetTypeID())
        {
            bestItem = current;
            break;
        }
        else if (bestItem == NULL && currentItemType == SecCertificateGetTypeID())
        {
            bestItem = current;
        }
    }

    if (bestItem == NULL)
    {
        return -13;
    }

    if (CFGetTypeID(bestItem) == SecCertificateGetTypeID())
    {
        CFRetain(bestItem);
        *pCertOut = (SecCertificateRef)CONST_CAST(void *,bestItem);
        return 1;
    }

    if (CFGetTypeID(bestItem) == SecIdentityGetTypeID())
    {
        CFRetain(bestItem);
        *pIdentityOut = (SecIdentityRef)CONST_CAST(void *,bestItem);

        return 1;
    }

    return -19;
}

int32_t AppleCryptoNative_X509CopyCertFromIdentity(SecIdentityRef identity, SecCertificateRef* pCertOut)
{
    if (pCertOut != NULL)
        *pCertOut = NULL;

    // This function handles null inputs for both identity and cert.
    return SecIdentityCopyCertificate(identity, pCertOut);
}

int32_t AppleCryptoNative_X509CopyPrivateKeyFromIdentity(SecIdentityRef identity, SecKeyRef* pPrivateKeyOut)
{
    if (pPrivateKeyOut != NULL)
        *pPrivateKeyOut = NULL;

    // This function handles null inputs for both identity and key
    return SecIdentityCopyPrivateKey(identity, pPrivateKeyOut);
}

int32_t AppleCryptoNative_X509GetRawData(SecCertificateRef cert, CFDataRef* ppDataOut, int32_t* pOSStatus)
{
    if (cert == NULL || ppDataOut == NULL || pOSStatus == NULL)
    {
        if (ppDataOut != NULL)
            *ppDataOut = NULL;
        if (pOSStatus != NULL)
            *pOSStatus = noErr;
        return kErrorBadInput;
    }

    *ppDataOut = SecCertificateCopyData(cert);
    *pOSStatus = *ppDataOut == NULL ? errSecParam : noErr;
    return (*pOSStatus == noErr);
}

#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
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
    assert(pbData != NULL);
    assert(cbData >= 0);
    assert((pCertOut == NULL) == (pIdentityOut == NULL));
    assert((pCertOut == NULL) != (pCollectionOut == NULL));

    SecExternalFormat dataFormat;
    SecExternalItemType itemType;
    int32_t ret = 0;
    CFArrayRef outItems = NULL;
    CFMutableArrayRef keyAttributes = NULL;
    SecKeychainRef importKeychain = NULL;

    SecItemImportExportKeyParameters importParams;
    memset(&importParams, 0, sizeof(SecItemImportExportKeyParameters));

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

        if (keychain == NULL)
        {
            return kErrorBadInput;
        }

        // if keyAttributes is NULL then it uses SENSITIVE | EXTRACTABLE
        // so if !exportable was requested, assert SENSITIVE.
        if (!exportable)
        {
            keyAttributes = CFArrayCreateMutable(NULL, 9, &kCFTypeArrayCallBacks);

            if (keyAttributes == NULL)
            {
                *pOSStatus = errSecAllocate;
                return 0;
            }

            int32_t sensitiveValue = CSSM_KEYATTR_SENSITIVE;
            CFNumberRef sensitive = CFNumberCreate(NULL, kCFNumberSInt32Type, &sensitiveValue);

            if (sensitive == NULL)
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

    CFDataRef cfData = CFDataCreateWithBytesNoCopy(NULL, pbData, cbData, kCFAllocatorNull);

    if (cfData == NULL)
    {
        *pOSStatus = errSecAllocate;
    }

    if (*pOSStatus == noErr)
    {
        *pOSStatus = SecItemImport(cfData, NULL, &dataFormat, &itemType, 0, &importParams, keychain, &outItems);
    }

    if (contentType == PAL_Pkcs12 && *pOSStatus == errSecPassphraseRequired && cfPfxPassphrase == NULL)
    {
        if (outItems != NULL)
        {
            CFRelease(outItems);
            outItems = NULL;
        }

        // Try again with the empty string passphrase.
        importParams.passphrase = CFSTR("");

        *pOSStatus = SecItemImport(cfData, NULL, &dataFormat, &itemType, 0, &importParams, keychain, &outItems);

        CFRelease(importParams.passphrase);
        importParams.passphrase = NULL;
    }

    if (*pOSStatus == noErr)
    {
        if (pCollectionOut != NULL)
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

    if (keyAttributes != NULL)
    {
        CFRelease(keyAttributes);
    }

    if (outItems != NULL)
    {
        // In the event this is returned via pCollectionOut it was already
        // CFRetain()ed, so always CFRelease here.
        CFRelease(outItems);
    }

    CFRelease(cfData);
    return ret;
}

int32_t AppleCryptoNative_X509ImportCollection(uint8_t* pbData,
                                               int32_t cbData,
                                               PAL_X509ContentType contentType,
                                               CFStringRef cfPfxPassphrase,
                                               SecKeychainRef keychain,
                                               int32_t exportable,
                                               CFArrayRef* pCollectionOut,
                                               int32_t* pOSStatus)
{
    if (pCollectionOut != NULL)
        *pCollectionOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (pbData == NULL || cbData < 0 || pCollectionOut == NULL || pOSStatus == NULL ||
        exportable != !!exportable)
    {
        return kErrorBadInput;
    }

    return ReadX509(pbData,
                    cbData,
                    contentType,
                    cfPfxPassphrase,
                    keychain,
                    (bool)exportable,
                    NULL,
                    NULL,
                    pCollectionOut,
                    pOSStatus);
}

int32_t AppleCryptoNative_X509ImportCertificate(uint8_t* pbData,
                                                int32_t cbData,
                                                PAL_X509ContentType contentType,
                                                CFStringRef cfPfxPassphrase,
                                                SecKeychainRef keychain,
                                                int32_t exportable,
                                                SecCertificateRef* pCertOut,
                                                SecIdentityRef* pIdentityOut,
                                                int32_t* pOSStatus)
{
    if (pCertOut != NULL)
        *pCertOut = NULL;
    if (pIdentityOut != NULL)
        *pIdentityOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (pbData == NULL || cbData < 0 || pCertOut == NULL || pIdentityOut == NULL || pOSStatus == NULL ||
        exportable != !!exportable)
    {
        return kErrorBadInput;
    }

    return ReadX509(pbData,
                    cbData,
                    contentType,
                    cfPfxPassphrase,
                    keychain,
                    (bool)exportable,
                    pCertOut,
                    pIdentityOut,
                    NULL,
                    pOSStatus);
}

int32_t AppleCryptoNative_X509ExportData(CFArrayRef data,
                                         PAL_X509ContentType type,
                                         CFStringRef cfExportPassphrase,
                                         CFDataRef* pExportOut,
                                         int32_t* pOSStatus)
{
    if (pExportOut != NULL)
        *pExportOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (data == NULL || pExportOut == NULL || pOSStatus == NULL)
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

    SecItemImportExportKeyParameters keyParams;
    memset(&keyParams, 0, sizeof(SecItemImportExportKeyParameters));

    keyParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;
    keyParams.passphrase = cfExportPassphrase;

    *pOSStatus = SecItemExport(data, dataFormat, 0, &keyParams, pExportOut);

    return *pOSStatus == noErr;
}

static OSStatus AddKeyToKeychain(SecKeyRef privateKey, SecKeychainRef targetKeychain, SecKeyRef* importedKey)
{
    SecExternalFormat dataFormat = kSecFormatWrappedPKCS8;
    CFDataRef exportData = NULL;

    SecItemImportExportKeyParameters keyParams;
    memset(&keyParams, 0, sizeof(SecItemImportExportKeyParameters));

    keyParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;
    keyParams.passphrase = CFSTR("ExportImportPassphrase");

    OSStatus status = SecItemExport(privateKey, dataFormat, 0, &keyParams, &exportData);

    SecExternalFormat actualFormat = dataFormat;
    SecExternalItemType actualType = kSecItemTypePrivateKey;
    CFArrayRef outItems = NULL;

    if (status == noErr)
    {
        status =
            SecItemImport(exportData, NULL, &actualFormat, &actualType, 0, &keyParams, targetKeychain, &outItems);
    }

    if (status == noErr && importedKey != NULL && outItems != NULL && CFArrayGetCount(outItems) == 1)
    {
        CFTypeRef outItem = CFArrayGetValueAtIndex(outItems, 0);

        if (CFGetTypeID(outItem) == SecKeyGetTypeID())
        {
            CFRetain(outItem);
            *importedKey = (SecKeyRef)CONST_CAST(void*, outItem);
        }
    }

    if (exportData != NULL)
        CFRelease(exportData);

    CFRelease(keyParams.passphrase);
    keyParams.passphrase = NULL;

    if (outItems != NULL)
        CFRelease(outItems);

    return status;
}

int32_t AppleCryptoNative_X509CopyWithPrivateKey(SecCertificateRef cert,
                                                 SecKeyRef privateKey,
                                                 SecKeychainRef targetKeychain,
                                                 SecIdentityRef* pIdentityOut,
                                                 int32_t* pOSStatus)
{
    if (pIdentityOut != NULL)
        *pIdentityOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (cert == NULL || privateKey == NULL || targetKeychain == NULL || pIdentityOut == NULL ||
        pOSStatus == NULL)
    {
        return -1;
    }

    SecKeychainRef keyKeychain = NULL;

    OSStatus status = SecKeychainItemCopyKeychain((SecKeychainItemRef)privateKey, &keyKeychain);
    SecKeychainItemRef itemCopy = NULL;

    // This only happens with an ephemeral key, so the keychain we're adding it to is temporary.
    if (status == errSecNoSuchKeychain)
    {
        status = AddKeyToKeychain(privateKey, targetKeychain, NULL);
    }

    if (itemCopy != NULL)
    {
        CFRelease(itemCopy);
    }

    CFMutableDictionaryRef query = NULL;

    if (status == noErr)
    {
        query = CFDictionaryCreateMutable(
            kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);

        if (query == NULL)
        {
            status = errSecAllocate;
        }
    }

    CFArrayRef searchList = NULL;

    if (status == noErr)
    {
        const void *constTargetKeychain = targetKeychain;
        searchList = CFArrayCreate(
            NULL, (const void**)(&constTargetKeychain), 1, &kCFTypeArrayCallBacks);

        if (searchList == NULL)
        {
            status = errSecAllocate;
        }
    }

    CFArrayRef itemMatch = NULL;

    if (status == noErr)
    {
        const void *constCert = cert;
        itemMatch = CFArrayCreate(
            NULL, (const void**)(&constCert), 1, &kCFTypeArrayCallBacks);

        if (itemMatch == NULL)
        {
            status = errSecAllocate;
        }
    }

    CFTypeRef result = NULL;

    if (status == noErr)
    {
        CFDictionarySetValue(query, kSecReturnRef, kCFBooleanTrue);
        CFDictionarySetValue(query, kSecMatchSearchList, searchList);
        CFDictionarySetValue(query, kSecMatchItemList, itemMatch);
        CFDictionarySetValue(query, kSecClass, kSecClassIdentity);

        status = SecItemCopyMatching(query, &result);

        if (status != noErr && result != NULL)
        {
            CFRelease(result);
            result = NULL;
        }

        bool added = false;

        if (status == errSecItemNotFound)
        {
            status = SecCertificateAddToKeychain(cert, targetKeychain);

            added = (status == noErr);
        }

        if (result == NULL && status == noErr)
        {
            status = SecItemCopyMatching(query, &result);
        }

        if (result != NULL && status == noErr)
        {

            if (CFGetTypeID(result) != SecIdentityGetTypeID())
            {
                status = errSecItemNotFound;
            }
            else
            {
                SecIdentityRef identity = (SecIdentityRef)CONST_CAST(void *, result);
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

    if (result != NULL)
        CFRelease(result);

    if (itemMatch != NULL)
        CFRelease(itemMatch);

    if (searchList != NULL)
        CFRelease(searchList);

    if (query != NULL)
        CFRelease(query);

    if (keyKeychain != NULL)
        CFRelease(keyKeychain);

    *pOSStatus = status;
    return status == noErr;
}

int32_t AppleCryptoNative_X509MoveToKeychain(SecCertificateRef cert,
                                             SecKeychainRef targetKeychain,
                                             SecKeyRef privateKey,
                                             SecIdentityRef* pIdentityOut,
                                             int32_t* pOSStatus)
{
    if (pIdentityOut != NULL)
        *pIdentityOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (cert == NULL || targetKeychain == NULL || pIdentityOut == NULL || pOSStatus == NULL)
    {
        return -1;
    }

    SecKeychainRef curKeychain = NULL;
    SecKeyRef importedKey = NULL;
    OSStatus status = SecKeychainItemCopyKeychain((SecKeychainItemRef)cert, &curKeychain);

    if (status == errSecNoSuchKeychain)
    {
        status = noErr;
    }
    else
    {
        if (curKeychain != NULL)
        {
            CFRelease(curKeychain);
        }

        if (status == noErr)
        {
            // Usage error: The certificate should have been freshly imported by the PFX loader,
            // and therefore have no keychain.
            return -2;
        }
    }

    if (status == noErr && privateKey != NULL)
    {
        status = SecKeychainItemCopyKeychain((SecKeychainItemRef)privateKey, &curKeychain);

        if (status == errSecNoSuchKeychain)
        {
            status = AddKeyToKeychain(privateKey, targetKeychain, &importedKey);

            // A duplicate key import will be the only time that status is noErr
            // and importedKey is NULL.
            if (status == errSecDuplicateItem)
            {
                status = noErr;
            }
        }
        else
        {
            if (curKeychain != NULL)
            {
                CFRelease(curKeychain);
            }

            if (status == noErr)
            {
                // This is a usage error, the only expected call is from the PFX loader,
                // which has an ephemeral key reference, therefore no keychain.
                return -3;
            }
        }
    }

    if (status == noErr)
    {
        status = SecCertificateAddToKeychain(cert, targetKeychain);

        if (status == errSecDuplicateItem)
        {
            status = noErr;
        }
    }

    if (status == noErr && privateKey != NULL)
    {
        CFMutableDictionaryRef query = NULL;
        CFArrayRef searchList = NULL;
        CFArrayRef itemMatch = NULL;
        CFTypeRef result = NULL;

        if (status == noErr)
        {
            query = CFDictionaryCreateMutable(
                kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);

            if (query == NULL)
            {
                status = errSecAllocate;
            }
        }

        if (status == noErr)
        {
            const void* constTargetKeychain = targetKeychain;
            searchList = CFArrayCreate(NULL, (const void**)(&constTargetKeychain), 1, &kCFTypeArrayCallBacks);

            if (searchList == NULL)
            {
                status = errSecAllocate;
            }
        }

        if (status == noErr)
        {
            const void* constCert = cert;
            itemMatch = CFArrayCreate(NULL, (const void**)(&constCert), 1, &kCFTypeArrayCallBacks);

            if (itemMatch == NULL)
            {
                status = errSecAllocate;
            }
        }

        if (status == noErr)
        {
            CFDictionarySetValue(query, kSecReturnRef, kCFBooleanTrue);
            CFDictionarySetValue(query, kSecMatchSearchList, searchList);
            CFDictionarySetValue(query, kSecMatchItemList, itemMatch);
            CFDictionarySetValue(query, kSecClass, kSecClassIdentity);

            status = SecItemCopyMatching(query, &result);

            if (status != noErr && result != NULL)
            {
                CFRelease(result);
                result = NULL;
            }

            if (result != NULL)
            {
                if (CFGetTypeID(result) == SecIdentityGetTypeID())
                {
                    SecIdentityRef identity = (SecIdentityRef)CONST_CAST(void*, result);
                    CFRetain(identity);
                    *pIdentityOut = identity;
                }
            }

            if (status == errSecItemNotFound && importedKey != NULL)
            {
                // An identity can't be found.
                // That means that the private key does not match the certificate public key.
                // Since we know we added the key, and nothing will reference it now, try to remove it.
                const void* constKey = importedKey;
                CFArrayRef newItemMatch = CFArrayCreate(NULL, (const void**)(&constKey), 1, &kCFTypeArrayCallBacks);
                CFDictionarySetValue(query, kSecMatchItemList, newItemMatch);
                CFRelease(itemMatch);
                itemMatch = newItemMatch;

                CFDictionarySetValue(query, kSecClass, kSecClassKey);

                // Even if the key delete failed, there's nothing the user can do about it now.
                // Ignore the result of delete and just return to noErr
                SecItemDelete(query);
                status = noErr;
            }
        }

        if (result != NULL)
            CFRelease(result);

        if (itemMatch != NULL)
            CFRelease(itemMatch);

        if (searchList != NULL)
            CFRelease(searchList);

        if (query != NULL)
            CFRelease(query);
    }

    if (importedKey != NULL)
        CFRelease(importedKey);

    *pOSStatus = status;
    return status == noErr;
}
#endif
