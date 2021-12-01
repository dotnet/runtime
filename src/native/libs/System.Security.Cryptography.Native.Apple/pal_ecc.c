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

uint64_t AppleCryptoNative_EccGetKeySizeInBits(SecKeyRef publicKey)
{
    if (publicKey == NULL)
    {
        return 0;
    }

    size_t blockSize = SecKeyGetBlockSize(publicKey);

    // This seems to be the expected size of an ECDSA signature for this key.
    // But since Apple uses the DER SEQUENCE(r, s) format the signature size isn't
    // fixed. It might be trying to encode the biggest the DER value could be:
    //
    // 256: r is 32 bytes, but maybe one padding byte, so 33.
    //      s is 32 bytes, but maybe one padding byte, so 33.
    //      each of those values gets one tag and one length byte
    //      35 * 2 is 70 payload bytes for the sequence, so one length byte
    //      and one tag byte, makes 72.
    //
    // 384: r,s are 48 bytes, plus padding, length, and tag: 51
    //      2 * 51 = 102, requires one length byte and one tag byte, 104.
    //
    // 521: neither r nor s can have the high bit set, no padding. 66 content bytes
    //      plus tag and length is 68.
    //      2 * 68 is 136, since it's greater than 127 it takes 2 length bytes
    //      so 136 + 2 + 1 = 139. Looks like they accounted for padding bytes anyways.
    //
    // This completely needs to be revisited if Apple adds support for "generic" ECC.
    //
    // Word of caution: While seeking meaning in these numbers I ran across a snippet of code
    // which suggests that on iOS (vs macOS) they use a different set of reasoning and produce
    // different numbers (they used (8 + 2*thisValue) on iOS for "signature length").
    //
    // Starting with macOS Mojave and the new SecCertificateCopyKey API the values
    // are the actual key size in bytes.
    switch (blockSize)
    {
        case 72:
            return 256;
        case 104:
            return 384;
        case 141:
            return 521;

        case 28:
            // Not fully supported as of macOS Mojave Developer Preview 4 and could later
            // result in internal library errors when consumed by other APIs:
            //
            // "Internal error #ffff9d28 at VerifyTransform_block_invoke /BuildRoot/Library/
            // Caches/com.apple.xbs/Sources/Security/Security-58286.200.178/OSX/
            // libsecurity_transform/lib/SecSignVerifyTransform.c:540" UserInfo={NSDescription=
            // Internal error #ffff9d28 at VerifyTransform_block_invoke /BuildRoot/Library/
            // Caches/com.apple.xbs/Sources/Security/Security-58286.200.178/OSX/
            // libsecurity_transform/lib/SecSignVerifyTransform.c:540, Originating Transform
            // =CoreFoundationObject}))
            //
            // Thus 0 is returned instead of 224 and the managed code treats it as
            // unsupported key size.
            return 0;
        case 32:
            return 256;
        case 48:
            return 384;
        case 66:
            return 521;
    }

    return 0;
}
