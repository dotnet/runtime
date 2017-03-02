// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_ecc.h"

extern "C" int32_t AppleCryptoNative_EccGenerateKey(
    int32_t keySizeBits, SecKeychainRef tempKeychain, SecKeyRef* pPublicKey, SecKeyRef* pPrivateKey, int32_t* pOSStatus)
{
    if (pPublicKey != nullptr)
        *pPublicKey = nullptr;
    if (pPrivateKey != nullptr)
        *pPrivateKey = nullptr;

    if (pPublicKey == nullptr || pPrivateKey == nullptr || pOSStatus == nullptr)
        return kErrorBadInput;

    CFMutableDictionaryRef attributes = CFDictionaryCreateMutable(nullptr, 2, &kCFTypeDictionaryKeyCallBacks, nullptr);

    CFNumberRef cfKeySizeValue = CFNumberCreate(nullptr, kCFNumberIntType, &keySizeBits);
    OSStatus status;

    if (attributes != nullptr && cfKeySizeValue != nullptr)
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

    if (attributes != nullptr)
        CFRelease(attributes);
    if (cfKeySizeValue != nullptr)
        CFRelease(cfKeySizeValue);

    *pOSStatus = status;
    return status == noErr;
}

extern "C" uint64_t AppleCryptoNative_EccGetKeySizeInBits(SecKeyRef publicKey)
{
    if (publicKey == nullptr)
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
    switch (blockSize)
    {
        case 72:
            return 256;
        case 104:
            return 384;
        case 141:
            return 521;
    }

    return 0;
}
