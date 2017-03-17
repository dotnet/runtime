// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_rsa.h"

static int32_t ExecuteCFDataTransform(
    SecTransformRef xform, uint8_t* pbData, int32_t cbData, CFDataRef* pDataOut, CFErrorRef* pErrorOut);

extern "C" int32_t AppleCryptoNative_RsaGenerateKey(
    int32_t keySizeBits, SecKeychainRef tempKeychain, SecKeyRef* pPublicKey, SecKeyRef* pPrivateKey, int32_t* pOSStatus)
{
    if (pPublicKey != nullptr)
        *pPublicKey = nullptr;
    if (pPrivateKey != nullptr)
        *pPrivateKey = nullptr;

    if (pPublicKey == nullptr || pPrivateKey == nullptr || pOSStatus == nullptr)
        return kErrorBadInput;
    if (keySizeBits < 384 || keySizeBits > 16384)
        return -2;

    CFMutableDictionaryRef attributes = CFDictionaryCreateMutable(nullptr, 2, &kCFTypeDictionaryKeyCallBacks, nullptr);

    CFNumberRef cfKeySizeValue = CFNumberCreate(nullptr, kCFNumberIntType, &keySizeBits);
    OSStatus status;

    if (attributes != nullptr && cfKeySizeValue != nullptr)
    {
        CFDictionaryAddValue(attributes, kSecAttrKeyType, kSecAttrKeyTypeRSA);
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

static int32_t ExecuteOaepTransform(SecTransformRef xform,
                                    uint8_t* pbData,
                                    int32_t cbData,
                                    PAL_HashAlgorithm algorithm,
                                    CFDataRef* pDataOut,
                                    CFErrorRef* pErrorOut)
{
    if (!SecTransformSetAttribute(xform, kSecPaddingKey, kSecPaddingOAEPKey, pErrorOut))
    {
        return kErrorSeeError;
    }

    // Documentation mentions kSecOAEPMGF1DigestAlgorithmAttributeName, but on the Apple platform
    // "SHA2" is an algorithm and the size is encoded separately. Since there doesn't seem to be
    // a second attribute to encode SHA2-256 vs SHA2-384, be limited to SHA-1.
    if (algorithm != PAL_SHA1)
    {
        return kErrorUnknownAlgorithm;
    }

    return ExecuteCFDataTransform(xform, pbData, cbData, pDataOut, pErrorOut);
}

extern "C" int32_t AppleCryptoNative_RsaDecryptOaep(SecKeyRef privateKey,
                                                    uint8_t* pbData,
                                                    int32_t cbData,
                                                    PAL_HashAlgorithm mfgAlgorithm,
                                                    CFDataRef* pDecryptedOut,
                                                    CFErrorRef* pErrorOut)
{
    if (pDecryptedOut != nullptr)
        *pDecryptedOut = nullptr;
    if (pErrorOut != nullptr)
        *pErrorOut = nullptr;

    if (privateKey == nullptr || pbData == nullptr || cbData < 0 || pDecryptedOut == nullptr || pErrorOut == nullptr)
    {
        return kErrorBadInput;
    }

    int32_t ret = kErrorSeeError;
    SecTransformRef decryptor = SecDecryptTransformCreate(privateKey, pErrorOut);

    if (decryptor != nullptr)
    {
        if (*pErrorOut == nullptr)
        {
            ret = ExecuteOaepTransform(decryptor, pbData, cbData, mfgAlgorithm, pDecryptedOut, pErrorOut);
        }

        CFRelease(decryptor);
    }

    return ret;
}

extern "C" int32_t AppleCryptoNative_RsaDecryptPkcs(
    SecKeyRef privateKey, uint8_t* pbData, int32_t cbData, CFDataRef* pDecryptedOut, CFErrorRef* pErrorOut)
{
    if (pDecryptedOut != nullptr)
        *pDecryptedOut = nullptr;
    if (pErrorOut != nullptr)
        *pErrorOut = nullptr;

    if (privateKey == nullptr || pbData == nullptr || cbData < 0 || pDecryptedOut == nullptr || pErrorOut == nullptr)
    {
        return kErrorBadInput;
    }

    int32_t ret = kErrorSeeError;
    SecTransformRef decryptor = SecDecryptTransformCreate(privateKey, pErrorOut);

    if (decryptor != nullptr)
    {
        if (*pErrorOut == nullptr)
        {
            ret = ExecuteCFDataTransform(decryptor, pbData, cbData, pDecryptedOut, pErrorOut);
        }

        CFRelease(decryptor);
    }

    return ret;
}

extern "C" int32_t AppleCryptoNative_RsaEncryptOaep(SecKeyRef publicKey,
                                                    uint8_t* pbData,
                                                    int32_t cbData,
                                                    PAL_HashAlgorithm mgfAlgorithm,
                                                    CFDataRef* pEncryptedOut,
                                                    CFErrorRef* pErrorOut)
{
    if (pEncryptedOut != nullptr)
        *pEncryptedOut = nullptr;
    if (pErrorOut != nullptr)
        *pErrorOut = nullptr;

    if (publicKey == nullptr || pbData == nullptr || cbData < 0 || pEncryptedOut == nullptr || pErrorOut == nullptr)
    {
        return kErrorBadInput;
    }

    int32_t ret = kErrorSeeError;
    SecTransformRef encryptor = SecEncryptTransformCreate(publicKey, pErrorOut);

    if (encryptor != nullptr)
    {
        if (*pErrorOut == nullptr)
        {
            ret = ExecuteOaepTransform(encryptor, pbData, cbData, mgfAlgorithm, pEncryptedOut, pErrorOut);
        }

        CFRelease(encryptor);
    }

    return ret;
}

extern "C" int32_t AppleCryptoNative_RsaEncryptPkcs(
    SecKeyRef publicKey, uint8_t* pbData, int32_t cbData, CFDataRef* pEncryptedOut, CFErrorRef* pErrorOut)
{
    if (pEncryptedOut != nullptr)
        *pEncryptedOut = nullptr;
    if (pErrorOut != nullptr)
        *pErrorOut = nullptr;

    if (publicKey == nullptr || pbData == nullptr || cbData < 0 || pEncryptedOut == nullptr || pErrorOut == nullptr)
    {
        return kErrorBadInput;
    }

    int32_t ret = kErrorSeeError;
    SecTransformRef encryptor = SecEncryptTransformCreate(publicKey, pErrorOut);

    if (encryptor != nullptr)
    {
        if (*pErrorOut == nullptr)
        {
            ret = ExecuteCFDataTransform(encryptor, pbData, cbData, pEncryptedOut, pErrorOut);
        }

        CFRelease(encryptor);
    }

    return ret;
}

static int32_t ExecuteCFDataTransform(
    SecTransformRef xform, uint8_t* pbData, int32_t cbData, CFDataRef* pDataOut, CFErrorRef* pErrorOut)
{
    if (xform == nullptr || pbData == nullptr || cbData < 0 || pDataOut == nullptr || pErrorOut == nullptr)
    {
        return kErrorBadInput;
    }

    *pDataOut = nullptr;
    *pErrorOut = nullptr;

    CFTypeRef xformOutput = nullptr;
    CFDataRef cfData = nullptr;
    int32_t ret = INT_MIN;

    cfData = CFDataCreateWithBytesNoCopy(nullptr, pbData, cbData, kCFAllocatorNull);

    if (cfData == nullptr)
    {
        // This probably means that there wasn't enough memory available, but no
        // particular failure cases are described.
        return kErrorUnknownState;
    }

    if (!SecTransformSetAttribute(xform, kSecTransformInputAttributeName, cfData, pErrorOut))
    {
        ret = kErrorSeeError;
        goto cleanup;
    }

    xformOutput = SecTransformExecute(xform, pErrorOut);

    if (xformOutput == nullptr || *pErrorOut != nullptr)
    {
        ret = kErrorSeeError;
        goto cleanup;
    }

    if (CFGetTypeID(xformOutput) == CFDataGetTypeID())
    {
        CFDataRef cfDataOut = reinterpret_cast<CFDataRef>(const_cast<void*>(xformOutput));
        CFRetain(cfDataOut);
        *pDataOut = cfDataOut;
        ret = 1;
    }
    else
    {
        ret = kErrorUnknownState;
    }

cleanup:
    if (xformOutput != nullptr)
    {
        CFRelease(xformOutput);
    }

    if (cfData != nullptr)
    {
        CFRelease(cfData);
    }

    return ret;
}
