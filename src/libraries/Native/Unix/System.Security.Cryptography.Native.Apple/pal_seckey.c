// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_seckey.h"
#include "pal_utilities.h"

#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
int32_t AppleCryptoNative_SecKeyExport(
    SecKeyRef pKey, int32_t exportPrivate, CFStringRef cfExportPassphrase, CFDataRef* ppDataOut, int32_t* pOSStatus)
{
    if (ppDataOut != NULL)
        *ppDataOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (pKey == NULL || ppDataOut == NULL || pOSStatus == NULL)
    {
        return kErrorBadInput;
    }

    SecExternalFormat dataFormat = kSecFormatOpenSSL;
    SecItemImportExportKeyParameters keyParams;
    memset(&keyParams, 0, sizeof(SecItemImportExportKeyParameters));

    keyParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;

    if (exportPrivate)
    {
        if (cfExportPassphrase == NULL)
        {
            return kErrorBadInput;
        }

        keyParams.passphrase = cfExportPassphrase;
        dataFormat = kSecFormatWrappedPKCS8;
    }

    *pOSStatus = SecItemExport(pKey, dataFormat, 0, &keyParams, ppDataOut);

    return (*pOSStatus == noErr);
}

int32_t AppleCryptoNative_SecKeyImportEphemeral(
    uint8_t* pbKeyBlob, int32_t cbKeyBlob, int32_t isPrivateKey, SecKeyRef* ppKeyOut, int32_t* pOSStatus)
{
    if (ppKeyOut != NULL)
        *ppKeyOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (pbKeyBlob == NULL || cbKeyBlob < 0 || isPrivateKey < 0 || isPrivateKey > 1 || ppKeyOut == NULL ||
        pOSStatus == NULL)
    {
        return kErrorBadInput;
    }

    int32_t ret = 0;
    CFDataRef cfData = CFDataCreateWithBytesNoCopy(NULL, pbKeyBlob, cbKeyBlob, kCFAllocatorNull);

    SecExternalFormat dataFormat = kSecFormatOpenSSL;
    SecExternalFormat actualFormat = dataFormat;

    SecExternalItemType itemType = isPrivateKey ? kSecItemTypePrivateKey : kSecItemTypePublicKey;
    SecExternalItemType actualType = itemType;

    CFIndex itemCount;
    CFArrayRef outItems = NULL;
    CFTypeRef outItem = NULL;

    *pOSStatus = SecItemImport(cfData, NULL, &actualFormat, &actualType, 0, NULL, NULL, &outItems);

    if (*pOSStatus != noErr)
    {
        ret = 0;
        goto cleanup;
    }

    if (actualFormat != dataFormat || actualType != itemType)
    {
        ret = -2;
        goto cleanup;
    }

    if (outItems == NULL)
    {
        ret = -3;
        goto cleanup;
    }

    itemCount = CFArrayGetCount(outItems);

    if (itemCount == 0)
    {
        ret = -4;
        goto cleanup;
    }

    if (itemCount > 1)
    {
        ret = -5;
        goto cleanup;
    }

    outItem = CFArrayGetValueAtIndex(outItems, 0);

    if (outItem == NULL)
    {
        ret = -6;
        goto cleanup;
    }

    if (CFGetTypeID(outItem) != SecKeyGetTypeID())
    {
        ret = -7;
        goto cleanup;
    }

    CFRetain(outItem);
    *ppKeyOut = (SecKeyRef)CONST_CAST(void *, outItem);
    ret = 1;

cleanup:
    if (outItems != NULL)
    {
        CFRelease(outItems);
    }

    CFRelease(cfData);
    return ret;
}
#endif

uint64_t AppleCryptoNative_SecKeyGetSimpleKeySizeInBytes(SecKeyRef publicKey)
{
    if (publicKey == NULL)
    {
        return 0;
    }

    return SecKeyGetBlockSize(publicKey);
}

static CFStringRef GetKeyAlgorithmIdentifier(PAL_KeyAlgorithm keyAlgorithm)
{
    if (keyAlgorithm == PAL_KeyAlgorithm_EC)
        return kSecAttrKeyTypeECSECPrimeRandom;
    if (keyAlgorithm == PAL_KeyAlgorithm_RSA)
        return kSecAttrKeyTypeRSA;
    return NULL;
}

int32_t AppleCryptoNative_SecKeyCreateWithData(uint8_t* pKey,
                                               int32_t cbKey,
                                               PAL_KeyAlgorithm keyAlgorithm,
                                               int32_t isPublic,
                                               SecKeyRef* pKeyOut,
                                               CFErrorRef* pErrorOut)
{
    assert(pKey != NULL);
    assert(cbKey > 0);
    assert(pKeyOut != NULL);
    assert(pErrorOut != NULL);

    *pKeyOut = NULL;
    *pErrorOut = NULL;

    CFMutableDictionaryRef dataAttributes = CFDictionaryCreateMutable(
        kCFAllocatorDefault, 2, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);

    if (dataAttributes == NULL)
    {
        return kErrorUnknownState;
    }

    CFStringRef keyClass = isPublic == 0 ? kSecAttrKeyClassPrivate : kSecAttrKeyClassPublic;
    CFStringRef keyType = GetKeyAlgorithmIdentifier(keyAlgorithm);

    assert(keyType != NULL);

    CFDictionarySetValue(dataAttributes, kSecAttrKeyType, keyType);
    CFDictionarySetValue(dataAttributes, kSecAttrKeyClass, keyClass);
    CFDataRef cfData = CFDataCreateWithBytesNoCopy(NULL, pKey, cbKey, kCFAllocatorNull);

    *pKeyOut = SecKeyCreateWithData(cfData, dataAttributes, pErrorOut);

    CFRelease(cfData);
    CFRelease(dataAttributes);

    return *pKeyOut != NULL ? 1 : kErrorSeeError;
}

int32_t AppleCryptoNative_SecKeyCopyExternalRepresentation(SecKeyRef pKey,
                                                           CFDataRef* ppDataOut,
                                                           CFErrorRef* pErrorOut)
{
    assert(pKey != NULL);
    assert(ppDataOut != NULL);
    assert(pErrorOut != NULL);

    *pErrorOut = NULL;

    *ppDataOut = SecKeyCopyExternalRepresentation(pKey, pErrorOut);
    return *ppDataOut == NULL ? kErrorSeeError : 1;
}

SecKeyRef AppleCryptoNative_SecKeyCopyPublicKey(SecKeyRef privateKey)
{
    assert(privateKey != NULL);

    return SecKeyCopyPublicKey(privateKey);
}
