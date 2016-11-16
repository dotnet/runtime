// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_seckey.h"

extern "C" int32_t AppleCryptoNative_SecKeyExport(
    SecKeyRef pKey, int32_t exportPrivate, CFStringRef cfExportPassphrase, CFDataRef* ppDataOut, int32_t* pOSStatus)
{
    if (ppDataOut != nullptr)
        *ppDataOut = nullptr;
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (pKey == nullptr || ppDataOut == nullptr || pOSStatus == nullptr)
    {
        return kErrorBadInput;
    }

    SecExternalFormat dataFormat = kSecFormatOpenSSL;
    SecItemImportExportKeyParameters keyParams = {};
    keyParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;

    if (exportPrivate)
    {
        if (cfExportPassphrase == nullptr)
        {
            return kErrorBadInput;
        }

        keyParams.passphrase = cfExportPassphrase;
        dataFormat = kSecFormatWrappedPKCS8;
    }

    *pOSStatus = SecItemExport(pKey, dataFormat, 0, &keyParams, ppDataOut);

    return (*pOSStatus == noErr);
}

extern "C" int32_t AppleCryptoNative_SecKeyImportEphemeral(
    uint8_t* pbKeyBlob, int32_t cbKeyBlob, int32_t isPrivateKey, SecKeyRef* ppKeyOut, int32_t* pOSStatus)
{
    if (ppKeyOut != nullptr)
        *ppKeyOut = nullptr;
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (pbKeyBlob == nullptr || cbKeyBlob < 0 || isPrivateKey < 0 || isPrivateKey > 1 || ppKeyOut == nullptr ||
        pOSStatus == nullptr)
    {
        return kErrorBadInput;
    }

    int32_t ret = 0;
    CFDataRef cfData = CFDataCreateWithBytesNoCopy(nullptr, pbKeyBlob, cbKeyBlob, kCFAllocatorNull);

    SecExternalFormat dataFormat = kSecFormatOpenSSL;
    SecExternalFormat actualFormat = dataFormat;

    SecExternalItemType itemType = isPrivateKey ? kSecItemTypePrivateKey : kSecItemTypePublicKey;
    SecExternalItemType actualType = itemType;

    CFIndex itemCount;
    CFArrayRef outItems = nullptr;
    CFTypeRef outItem = nullptr;

    *pOSStatus = SecItemImport(cfData, nullptr, &actualFormat, &actualType, 0, nullptr, nullptr, &outItems);

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

    if (outItems == nullptr)
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

    if (outItem == nullptr)
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
    *ppKeyOut = reinterpret_cast<SecKeyRef>(const_cast<void*>(outItem));
    ret = 1;

cleanup:
    if (outItems != nullptr)
    {
        CFRelease(outItems);
    }

    CFRelease(cfData);
    return ret;
}

extern "C" uint64_t AppleCryptoNative_SecKeyGetSimpleKeySizeInBytes(SecKeyRef publicKey)
{
    if (publicKey == nullptr)
    {
        return 0;
    }

    return SecKeyGetBlockSize(publicKey);
}
