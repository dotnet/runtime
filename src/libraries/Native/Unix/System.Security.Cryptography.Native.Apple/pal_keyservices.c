// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_keyservices.h"
#include "pal_ecc.h"

const SecKeyAlgorithm kSecKeyAlgorithmRSASignatureDigestPKCS1v15MD5 = CFSTR("algid:sign:RSA:digest-PKCS1v15:MD5");
const SecKeyAlgorithm kSecKeyAlgorithmRSASignatureMessagePKCS1v15MD5 = CFSTR("algid:sign:RSA:message-PKCS1v15:MD5");

static CFStringRef GetSignatureAlgorithmIdentifier(PAL_HashAlgorithm hashAlgorithm, PAL_SignatureAlgorithm signatureAlgorithm, bool digest)
{
    if (signatureAlgorithm == PAL_SignatureAlgorithm_EC)
    {
        // ECDSA signatures are always based on digests. The managed implementation
        // will always pre-hash data before getting here.
        assert(digest);
        return kSecKeyAlgorithmECDSASignatureDigestX962;
    }
    if (signatureAlgorithm == PAL_SignatureAlgorithm_RSA_Pkcs1)
    {
        if (digest)
        {
            switch (hashAlgorithm)
            {
                case PAL_MD5: return kSecKeyAlgorithmRSASignatureDigestPKCS1v15MD5;
                case PAL_SHA1: return kSecKeyAlgorithmRSASignatureDigestPKCS1v15SHA1;
                case PAL_SHA256: return kSecKeyAlgorithmRSASignatureDigestPKCS1v15SHA256;
                case PAL_SHA384: return kSecKeyAlgorithmRSASignatureDigestPKCS1v15SHA384;
                case PAL_SHA512: return kSecKeyAlgorithmRSASignatureDigestPKCS1v15SHA512;
            }

        }
        else
        {
            switch (hashAlgorithm)
            {
                case PAL_MD5: return kSecKeyAlgorithmRSASignatureMessagePKCS1v15MD5;
                case PAL_SHA1: return kSecKeyAlgorithmRSASignatureMessagePKCS1v15SHA1;
                case PAL_SHA256: return kSecKeyAlgorithmRSASignatureMessagePKCS1v15SHA256;
                case PAL_SHA384: return kSecKeyAlgorithmRSASignatureMessagePKCS1v15SHA384;
                case PAL_SHA512: return kSecKeyAlgorithmRSASignatureMessagePKCS1v15SHA512;
            }
        }
    }

    return NULL;
}

static CFStringRef GetKeyAlgorithmIdentifier(PAL_KeyAlgorithm keyAlgorithm)
{
    if (keyAlgorithm == PAL_KeyAlgorithm_EC)
        return kSecAttrKeyTypeECSECPrimeRandom;
    if (keyAlgorithm == PAL_KeyAlgorithm_RSA)
        return kSecAttrKeyTypeRSA;

    return NULL;
}

int32_t AppleCryptoNative_CreateDataKey(uint8_t* pKey,
                                        int32_t cbKey,
                                        PAL_KeyAlgorithm keyAlgorithm,
                                        int32_t isPublic,
                                        SecKeyRef* pKeyOut,
                                        CFErrorRef* pErrorOut)
{
    if (pErrorOut != NULL)
        *pErrorOut = NULL;

    if (pKeyOut != NULL)
        *pKeyOut = NULL;

    if (pKeyOut == NULL || pErrorOut == NULL || cbKey <= 0 || pKey == NULL)
        return kErrorBadInput;

    CFMutableDictionaryRef dataAttributes = CFDictionaryCreateMutable(
        kCFAllocatorDefault, 2, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);

    if (dataAttributes == NULL)
    {
        return kErrorUnknownState;
    }

    CFStringRef keyClass = isPublic == 0 ? kSecAttrKeyClassPrivate : kSecAttrKeyClassPublic;
    CFStringRef keyType = GetKeyAlgorithmIdentifier(keyAlgorithm);

    if (keyType == NULL)
    {
        CFRelease(dataAttributes);
        return kErrorBadInput;
    }

    CFDictionarySetValue(dataAttributes, kSecAttrKeyType, keyType);
    CFDictionarySetValue(dataAttributes, kSecAttrKeyClass, keyClass);
    CFDataRef cfData = CFDataCreateWithBytesNoCopy(NULL, pKey, cbKey, kCFAllocatorNull);

    *pKeyOut = SecKeyCreateWithData(cfData, dataAttributes, pErrorOut);
    int32_t ret = kErrorSeeError;

    if (*pKeyOut != NULL)
    {
        ret = 1;
    }

    CFRelease(cfData);
    CFRelease(dataAttributes);
    return ret;
}

int32_t AppleCryptoNative_SecKeyCreateSignature(SecKeyRef privateKey,
                                                uint8_t* pbDataHash,
                                                int32_t cbDataHash,
                                                PAL_HashAlgorithm hashAlgorithm,
                                                PAL_SignatureAlgorithm signatureAlgorithm,
                                                int32_t digest,
                                                CFDataRef* pSignatureOut,
                                                CFErrorRef* pErrorOut)
{
    if (pErrorOut != NULL)
        *pErrorOut = NULL;

    if (pSignatureOut != NULL)
        *pSignatureOut = NULL;

    if (privateKey == NULL || pbDataHash == NULL || cbDataHash < 0 ||
        pErrorOut == NULL || pSignatureOut == NULL)
        return kErrorBadInput;

    bool useDigest = digest != 0;
    CFStringRef algorithm = GetSignatureAlgorithmIdentifier(hashAlgorithm, signatureAlgorithm, useDigest);

    if (algorithm == NULL)
        return kErrorUnknownAlgorithm;

    CFDataRef dataHash = CFDataCreateWithBytesNoCopy(NULL, pbDataHash, cbDataHash, kCFAllocatorNull);

    if (dataHash == NULL)
    {
        return kErrorUnknownState;
    }

    int32_t ret = kErrorSeeError;

    CFDataRef sig = SecKeyCreateSignature(privateKey, algorithm, dataHash, pErrorOut);

    if (sig != NULL)
    {
        CFRetain(sig);
        *pSignatureOut = sig;
        ret = 1;
    }

    CFRelease(dataHash);
    return ret;
}

int32_t AppleCryptoNative_SecKeyVerifySignature(SecKeyRef publicKey,
                                                uint8_t* pbDataHash,
                                                int32_t cbDataHash,
                                                uint8_t* pbSignature,
                                                int32_t cbSignature,
                                                PAL_HashAlgorithm hashAlgorithm,
                                                PAL_SignatureAlgorithm signatureAlgorithm,
                                                int digest,
                                                CFErrorRef* pErrorOut)
{
    if (pErrorOut != NULL)
        *pErrorOut = NULL;

    if (publicKey == NULL || cbDataHash < 0 || pbSignature == NULL || cbSignature < 0 || pErrorOut == NULL)
        return kErrorBadInput;

    // A null hash is automatically the wrong length, so the signature will fail.
    if (pbDataHash == NULL)
        return 0;

    bool useDigest = digest != 0;
    CFStringRef algorithm = GetSignatureAlgorithmIdentifier(hashAlgorithm, signatureAlgorithm, useDigest);

    if (algorithm == NULL)
        return kErrorBadInput;

    CFDataRef dataHash = CFDataCreateWithBytesNoCopy(NULL, pbDataHash, cbDataHash, kCFAllocatorNull);

    if (dataHash == NULL)
        return kErrorUnknownState;

    CFDataRef signature = CFDataCreateWithBytesNoCopy(NULL, pbSignature, cbSignature, kCFAllocatorNull);

    if (signature == NULL)
    {
        CFRelease(dataHash);
        return kErrorUnknownState;
    }

    int32_t ret = kErrorSeeError;

    if (SecKeyVerifySignature(publicKey, algorithm, dataHash, signature, pErrorOut))
    {
        ret = 1;
    }
    else if (CFErrorGetCode(*pErrorOut) == errSecVerifyFailed || CFErrorGetCode(*pErrorOut) == errSecParam)
    {
        ret = 0;
    }

    CFRelease(dataHash);
    CFRelease(signature);

    return ret;
}
