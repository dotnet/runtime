// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_ecc.h"

#if !defined(TARGET_IOS) && !defined(TARGET_TVOS)
int32_t AppleCryptoNative_EccGenerateKey(
    int32_t keySizeBits, SecKeychainRef tempKeychain, SecKeyRef* pPublicKey, SecKeyRef* pPrivateKey, int32_t* pOSStatus)
{
    if (pPublicKey != NULL)
        *pPublicKey = NULL;
    if (pPrivateKey != NULL)
        *pPrivateKey = NULL;

    if (pPublicKey == NULL || pPrivateKey == NULL || pOSStatus == NULL)
        return kErrorBadInput;

    CFMutableDictionaryRef attributes = CFDictionaryCreateMutable(NULL, 3, &kCFTypeDictionaryKeyCallBacks, NULL);

    CFNumberRef cfKeySizeValue = CFNumberCreate(NULL, kCFNumberIntType, &keySizeBits);
    OSStatus status;

    if (attributes != NULL && cfKeySizeValue != NULL)
    {
        CFDictionaryAddValue(attributes, kSecAttrKeyType, kSecAttrKeyTypeEC);
        CFDictionaryAddValue(attributes, kSecAttrKeySizeInBits, cfKeySizeValue);
        CFDictionaryAddValue(attributes, kSecUseKeychain, tempKeychain);

        status = SecKeyGeneratePair(attributes, pPublicKey, pPrivateKey);

        if (status == noErr)
        {
            status = ExportImportKey(pPublicKey, kSecItemTypePublicKey);
        }

        if (status == noErr)
        {
            status = ExportImportKey(pPrivateKey, kSecItemTypePrivateKey);
        }
    }
    else
    {
        status = errSecAllocate;
    }

    if (attributes != NULL)
        CFRelease(attributes);
    if (cfKeySizeValue != NULL)
        CFRelease(cfKeySizeValue);

    *pOSStatus = status;
    return status == noErr;
}
#endif

uint64_t AppleCryptoNative_EccGetKeySizeInBits(SecKeyRef publicKey)
{
    if (publicKey == NULL)
        return 0;

    CFDictionaryRef attributes = SecKeyCopyAttributes(publicKey);

    if (attributes == NULL)
        return 0;

    CFNumberRef cfSize = CFDictionaryGetValue(attributes, kSecAttrKeySizeInBits);
    int size = 0;

    if (cfSize != NULL)
    {
        if (!CFNumberGetValue(cfSize, kCFNumberIntType, &size))
        {
            size = 0;
        }
        else if (size != 256 && size != 384 && size != 521)
        {
            // Restrict the key size to sizes that are understood by managed code.
            // Otherwise, return 0 so the managed code treats it as unsupported key size. 
            size = 0;
        }
    }

    CFRelease(attributes);
    return size;
}
