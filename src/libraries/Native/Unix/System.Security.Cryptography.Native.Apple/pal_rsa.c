// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_rsa.h"

int32_t AppleCryptoNative_RsaGenerateKey(int32_t keySizeBits,
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
    if (keySizeBits < 384 || keySizeBits > 16384)
        return -2;

    int32_t ret = kErrorSeeError;
    CFMutableDictionaryRef attributes = CFDictionaryCreateMutable(NULL, 3, &kCFTypeDictionaryKeyCallBacks, NULL);
    CFNumberRef cfKeySizeValue = CFNumberCreate(NULL, kCFNumberIntType, &keySizeBits);

    if (attributes != NULL && cfKeySizeValue != NULL)
    {
        CFDictionaryAddValue(attributes, kSecAttrKeyType, kSecAttrKeyTypeRSA);
        CFDictionaryAddValue(attributes, kSecAttrKeySizeInBits, cfKeySizeValue);

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

static int32_t RsaPrimitive(SecKeyRef key,
                            uint8_t* pbData,
                            int32_t cbData,
                            CFDataRef* pDataOut,
                            CFErrorRef* pErrorOut,
                            SecKeyAlgorithm algorithm,
                            CFDataRef func(SecKeyRef, SecKeyAlgorithm, CFDataRef, CFErrorRef*))
{
    if (pDataOut != NULL)
        *pDataOut = NULL;
    if (pErrorOut != NULL)
        *pErrorOut = NULL;

    if (key == NULL || pbData == NULL || cbData < 0 || pDataOut == NULL || pErrorOut == NULL)
    {
        return kErrorBadInput;
    }

    assert(func != NULL);

    CFDataRef input = CFDataCreateWithBytesNoCopy(NULL, pbData, cbData, kCFAllocatorNull);
    CFDataRef output = func(key, algorithm, input, pErrorOut);

    if (*pErrorOut != NULL)
    {
        if (output != NULL)
        {
            CFRelease(output);
            output = NULL;
        }

        return kErrorSeeError;
    }

    if (output == NULL)
    {
        return kErrorUnknownState;
    }

    *pDataOut = output;
    return 1;
}

static int32_t RsaOaepPrimitive(SecKeyRef key,
                                uint8_t* pbData,
                                int32_t cbData,
                                CFDataRef* pDataOut,
                                CFErrorRef* pErrorOut,
                                PAL_HashAlgorithm mgfAlgorithm,
                                CFDataRef func(SecKeyRef, SecKeyAlgorithm, CFDataRef, CFErrorRef*))
{
    if (pDataOut != NULL)
        *pDataOut = NULL;
    if (pErrorOut != NULL)
        *pErrorOut = NULL;

    SecKeyAlgorithm algorithm;
    switch (mgfAlgorithm)
    {
        case PAL_SHA1: algorithm = kSecKeyAlgorithmRSAEncryptionOAEPSHA1; break;
        case PAL_SHA256: algorithm = kSecKeyAlgorithmRSAEncryptionOAEPSHA256; break;
        case PAL_SHA384: algorithm = kSecKeyAlgorithmRSAEncryptionOAEPSHA384; break;
        case PAL_SHA512: algorithm = kSecKeyAlgorithmRSAEncryptionOAEPSHA512; break;
        default:
            return kErrorUnknownAlgorithm;
    }

    return RsaPrimitive(
        key, pbData, cbData, pDataOut, pErrorOut, algorithm, func);

}

int32_t AppleCryptoNative_RsaDecryptOaep(SecKeyRef privateKey,
                                         uint8_t* pbData,
                                         int32_t cbData,
                                         PAL_HashAlgorithm mgfAlgorithm,
                                         CFDataRef* pDecryptedOut,
                                         CFErrorRef* pErrorOut)
{
    return RsaOaepPrimitive(
        privateKey, pbData, cbData, pDecryptedOut, pErrorOut, mgfAlgorithm, SecKeyCreateDecryptedData);
}

int32_t AppleCryptoNative_RsaDecryptPkcs(
    SecKeyRef privateKey, uint8_t* pbData, int32_t cbData, CFDataRef* pDecryptedOut, CFErrorRef* pErrorOut)
{
    return RsaPrimitive(
        privateKey, pbData, cbData, pDecryptedOut, pErrorOut, kSecKeyAlgorithmRSAEncryptionPKCS1, SecKeyCreateDecryptedData);
}

int32_t AppleCryptoNative_RsaEncryptOaep(SecKeyRef publicKey,
                                         uint8_t* pbData,
                                         int32_t cbData,
                                         PAL_HashAlgorithm mgfAlgorithm,
                                         CFDataRef* pEncryptedOut,
                                         CFErrorRef* pErrorOut)
{
    return RsaOaepPrimitive(
        publicKey, pbData, cbData, pEncryptedOut, pErrorOut, mgfAlgorithm, SecKeyCreateEncryptedData);
}

int32_t AppleCryptoNative_RsaEncryptPkcs(
    SecKeyRef publicKey, uint8_t* pbData, int32_t cbData, CFDataRef* pEncryptedOut, CFErrorRef* pErrorOut)
{
    return RsaPrimitive(
        publicKey, pbData, cbData, pEncryptedOut, pErrorOut, kSecKeyAlgorithmRSAEncryptionPKCS1, SecKeyCreateEncryptedData);
}

int32_t AppleCryptoNative_RsaSignaturePrimitive(
    SecKeyRef privateKey, uint8_t* pbData, int32_t cbData, CFDataRef* pDataOut, CFErrorRef* pErrorOut)
{
    return RsaPrimitive(
        privateKey, pbData, cbData, pDataOut, pErrorOut, kSecKeyAlgorithmRSASignatureRaw, SecKeyCreateSignature);
}

int32_t AppleCryptoNative_RsaEncryptionPrimitive(
    SecKeyRef publicKey, uint8_t* pbData, int32_t cbData, CFDataRef* pDataOut, CFErrorRef* pErrorOut)
{
    return RsaPrimitive(
        publicKey, pbData, cbData, pDataOut, pErrorOut, kSecKeyAlgorithmRSAEncryptionRaw, SecKeyCreateEncryptedData);
}

int32_t AppleCryptoNative_RsaVerificationPrimitive(
    SecKeyRef publicKey, uint8_t* pbData, int32_t cbData, CFDataRef* pDataOut, CFErrorRef* pErrorOut)
{
    // Since there's not an API which will give back the still-padded signature block with
    // kSecAlgorithmRSASignatureRaw, use the encryption primitive to achieve the same result.
    return RsaPrimitive(
        publicKey, pbData, cbData, pDataOut, pErrorOut, kSecKeyAlgorithmRSAEncryptionRaw, SecKeyCreateEncryptedData);
}
