// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_seckey.h"
#include "pal_utilities.h"

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
