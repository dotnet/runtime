// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ecc.h"

int32_t AppleCryptoNative_EccGenerateKey(int32_t keySizeBits,
                                         SecKeyRef* pPublicKey,
                                         SecKeyRef* pPrivateKey,
                                         CFErrorRef* pErrorOut)
{
    if (pPublicKey != NULL)
        *pPublicKey = NULL;
    if (pPrivateKey != NULL)
        *pPrivateKey = NULL;

    if (pPublicKey == NULL || pPrivateKey == NULL || pErrorOut == NULL)
        return kErrorBadInput;

    int32_t ret = kErrorSeeError;
    CFMutableDictionaryRef attributes = CFDictionaryCreateMutable(NULL, 3, &kCFTypeDictionaryKeyCallBacks, NULL);
    CFNumberRef cfKeySizeValue = CFNumberCreate(NULL, kCFNumberIntType, &keySizeBits);

    if (attributes != NULL && cfKeySizeValue != NULL)
    {
        CFDictionaryAddValue(attributes, kSecAttrKeyType, kSecAttrKeyTypeEC);
        CFDictionaryAddValue(attributes, kSecAttrKeySizeInBits, cfKeySizeValue);
        if (__builtin_available(macOS 10.15, iOS 13, tvOS 13, *))
        {
            CFDictionaryAddValue(attributes, kSecUseDataProtectionKeychain, kCFBooleanTrue);
        }

        *pPrivateKey = SecKeyCreateRandomKey(attributes, pErrorOut);
        if (*pPrivateKey != NULL)
        {
            *pPublicKey = SecKeyCopyPublicKey(*pPrivateKey);
            ret = 1;
        }
    }
    else
    {
        ret = errSecAllocate;
    }

    if (attributes != NULL)
        CFRelease(attributes);
    if (cfKeySizeValue != NULL)
        CFRelease(cfKeySizeValue);

    return ret;
}

int32_t AppleCryptoNative_EccGetKeySizeInBits(SecKeyRef publicKey)
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
