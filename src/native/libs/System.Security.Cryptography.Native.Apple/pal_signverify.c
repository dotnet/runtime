// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_signverify.h"

#if defined(TARGET_OSX)
static int32_t ExecuteSignTransform(SecTransformRef signer, CFDataRef* pSignatureOut, CFErrorRef* pErrorOut);
static int32_t ExecuteVerifyTransform(SecTransformRef verifier, CFErrorRef* pErrorOut);

static int32_t ConfigureSignVerifyTransform(SecTransformRef xform, CFDataRef cfDataHash, CFErrorRef* pErrorOut);

static int32_t ExecuteSignTransform(SecTransformRef signer, CFDataRef* pSignatureOut, CFErrorRef* pErrorOut)
{
    assert(signer != NULL);
    assert(pSignatureOut != NULL);
    assert(pErrorOut != NULL);

    int32_t ret = INT_MIN;
    CFTypeRef signerResponse = SecTransformExecute(signer, pErrorOut);
    CFDataRef signature = NULL;

    if (signerResponse == NULL || *pErrorOut != NULL)
    {
        ret = kErrorSeeError;
        goto cleanup;
    }

    if (CFGetTypeID(signerResponse) != CFDataGetTypeID())
    {
        ret = kErrorUnknownState;
        goto cleanup;
    }

    signature = (CFDataRef)signerResponse;

    if (CFDataGetLength(signature) > 0)
    {
        // We're going to call CFRelease in cleanup, so this keeps it alive
        // to be interpreted by the managed code.
        CFRetain(signature);
        *pSignatureOut = signature;
        ret = 1;
    }
    else
    {
        ret = kErrorUnknownState;
        *pSignatureOut = NULL;
    }

cleanup:
    if (signerResponse != NULL)
    {
        CFRelease(signerResponse);
    }

    return ret;
}

static int32_t ExecuteVerifyTransform(SecTransformRef verifier, CFErrorRef* pErrorOut)
{
    assert(verifier != NULL);
    assert(pErrorOut != NULL);

    int32_t ret = kErrorSeeError;
    CFTypeRef verifierResponse = SecTransformExecute(verifier, pErrorOut);

    if (verifierResponse != NULL)
    {
        if (*pErrorOut == NULL)
        {
            ret = (verifierResponse == kCFBooleanTrue);
        }

        CFRelease(verifierResponse);
    }

    return ret;
}

static int32_t ConfigureSignVerifyTransform(SecTransformRef xform, CFDataRef cfDataHash, CFErrorRef* pErrorOut)
{
    if (!SecTransformSetAttribute(xform, kSecInputIsAttributeName, kSecInputIsDigest, pErrorOut))
    {
        return 0;
    }

    if (!SecTransformSetAttribute(xform, kSecTransformInputAttributeName, cfDataHash, pErrorOut))
    {
        return 0;
    }

    return 1;
}
#endif

// Legacy algorithm identifiers
const SecKeyAlgorithm kSecKeyAlgorithmRSASignatureDigestPKCS1v15MD5 = CFSTR("algid:sign:RSA:digest-PKCS1v15:MD5");

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

    // Requires macOS 10.13+ or iOS 11+
    /*if (signatureAlgorithm == PAL_SignatureAlgorithm_RSA_Pss)
    {
        switch (hashAlgorithm)
        {
            case PAL_SHA1: return kSecKeyAlgorithmRSASignatureDigestPSSSHA1;
            case PAL_SHA256: return kSecKeyAlgorithmRSASignatureDigestPSSSHA256;
            case PAL_SHA384: return kSecKeyAlgorithmRSASignatureDigestPSSSHA384;
            case PAL_SHA512: return kSecKeyAlgorithmRSASignatureDigestPSSSHA512;
        }
    }*/

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

    if (signatureAlgorithm == PAL_SignatureAlgorithm_DSA)
    {
#if defined(TARGET_OSX)
        SecTransformRef signer = SecSignTransformCreate(privateKey, pErrorOut);

        if (signer != NULL)
        {
            if (*pErrorOut == NULL)
            {
                if (ConfigureSignVerifyTransform(signer, dataHash, pErrorOut))
                {
                    ret = ExecuteSignTransform(signer, pSignatureOut, pErrorOut);
                }
            }

            CFRelease(signer);
        }
#else
        ret = kPlatformNotSupported;
#endif
    }
    else
    {
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

    if (signatureAlgorithm == PAL_SignatureAlgorithm_DSA)
    {
#if defined(TARGET_OSX)
        SecTransformRef verifier = SecVerifyTransformCreate(publicKey, signature, pErrorOut);

        if (verifier != NULL)
        {
            if (*pErrorOut == NULL)
            {
                if (ConfigureSignVerifyTransform(verifier, dataHash, pErrorOut))
                {
                    ret = ExecuteVerifyTransform(verifier, pErrorOut);
                }
            }

            CFRelease(verifier);
        }
#else
        ret = kPlatformNotSupported;
#endif
    }
    else
    {
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
    }

    CFRelease(dataHash);
    CFRelease(signature);

    return ret;
}
