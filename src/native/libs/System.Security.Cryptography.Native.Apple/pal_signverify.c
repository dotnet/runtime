// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_signverify.h"

// Legacy algorithm identifiers
static const SecKeyAlgorithm kSecKeyAlgorithmRSASignatureDigestPKCS1v15MD5 = CFSTR("algid:sign:RSA:digest-PKCS1v15:MD5");

static CFStringRef GetSignatureAlgorithmIdentifier(PAL_HashAlgorithm hashAlgorithm,
                                                   PAL_SignatureAlgorithm signatureAlgorithm)
{
    if (signatureAlgorithm == PAL_SignatureAlgorithm_EC)
    {
        // ECDSA signatures are always based on digests. The managed implementation
        // will always pre-hash data before getting here.
        return kSecKeyAlgorithmECDSASignatureDigestX962;
    }

    if (signatureAlgorithm == PAL_SignatureAlgorithm_RSA_Pkcs1)
    {
        switch (hashAlgorithm)
        {
            case PAL_MD5:
                return kSecKeyAlgorithmRSASignatureDigestPKCS1v15MD5;
            case PAL_SHA1:
                return kSecKeyAlgorithmRSASignatureDigestPKCS1v15SHA1;
            case PAL_SHA256:
                return kSecKeyAlgorithmRSASignatureDigestPKCS1v15SHA256;
            case PAL_SHA384:
                return kSecKeyAlgorithmRSASignatureDigestPKCS1v15SHA384;
            case PAL_SHA512:
                return kSecKeyAlgorithmRSASignatureDigestPKCS1v15SHA512;
        }
    }

    if (signatureAlgorithm == PAL_SignatureAlgorithm_RSA_Pss)
    {
        switch (hashAlgorithm)
        {
            case PAL_SHA1: return kSecKeyAlgorithmRSASignatureDigestPSSSHA1;
            case PAL_SHA256: return kSecKeyAlgorithmRSASignatureDigestPSSSHA256;
            case PAL_SHA384: return kSecKeyAlgorithmRSASignatureDigestPSSSHA384;
            case PAL_SHA512: return kSecKeyAlgorithmRSASignatureDigestPSSSHA512;
        }
    }

    if (signatureAlgorithm == PAL_SignatureAlgorithm_RSA_Raw)
    {
        return kSecKeyAlgorithmRSASignatureRaw;
    }

    return NULL;
}

int32_t AppleCryptoNative_SecKeyCreateSignature(SecKeyRef privateKey,
                                                uint8_t* pbDataHash,
                                                int32_t cbDataHash,
                                                PAL_HashAlgorithm hashAlgorithm,
                                                PAL_SignatureAlgorithm signatureAlgorithm,
                                                CFDataRef* pSignatureOut,
                                                CFErrorRef* pErrorOut)
{
    if (pErrorOut != NULL)
        *pErrorOut = NULL;
    if (pSignatureOut != NULL)
        *pSignatureOut = NULL;

    if (privateKey == NULL || pbDataHash == NULL || cbDataHash < 0 || pErrorOut == NULL || pSignatureOut == NULL)
    {
        return kErrorBadInput;
    }

    int32_t ret = kErrorSeeError;
    CFDataRef dataHash = CFDataCreateWithBytesNoCopy(NULL, pbDataHash, cbDataHash, kCFAllocatorNull);

    if (dataHash == NULL)
    {
        return kErrorUnknownState;
    }

    CFStringRef algorithm = GetSignatureAlgorithmIdentifier(hashAlgorithm, signatureAlgorithm);

    if (algorithm == NULL)
    {
        CFRelease(dataHash);
        return kErrorUnknownAlgorithm;
    }

    CFDataRef sig = SecKeyCreateSignature(privateKey, algorithm, dataHash, pErrorOut);

    if (sig != NULL)
    {
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
                                                CFErrorRef* pErrorOut)
{
    if (pErrorOut != NULL)
        *pErrorOut = NULL;
    if (publicKey == NULL || cbDataHash < 0 || pbSignature == NULL || cbSignature < 0 || pErrorOut == NULL)
        return kErrorBadInput;

    // A null hash is automatically the wrong length, so the signature will fail.
    if (pbDataHash == NULL)
        return 0;

    int32_t ret = kErrorSeeError;
    CFDataRef dataHash = CFDataCreateWithBytesNoCopy(NULL, pbDataHash, cbDataHash, kCFAllocatorNull);
    if (dataHash == NULL)
    {
        return kErrorUnknownState;
    }

    CFDataRef signature = CFDataCreateWithBytesNoCopy(NULL, pbSignature, cbSignature, kCFAllocatorNull);
    if (signature == NULL)
    {
        CFRelease(dataHash);
        return kErrorUnknownState;
    }

    CFStringRef algorithm = GetSignatureAlgorithmIdentifier(hashAlgorithm, signatureAlgorithm);

    if (algorithm == NULL)
    {
        CFRelease(dataHash);
        CFRelease(signature);
        return kErrorBadInput;
    }

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
