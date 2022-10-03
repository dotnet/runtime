// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_keychain_ios.h"
#include "pal_utilities.h"
#include "pal_x509.h"

static int32_t EnumerateKeychain(CFStringRef matchType, CFArrayRef* pCertsOut)
{
    assert(pCertsOut != NULL);
    assert(matchType != NULL);

    *pCertsOut = NULL;

    const void* keys[] = {kSecReturnRef, kSecMatchLimit, kSecClass};
    const void* values[] = {kCFBooleanTrue, kSecMatchLimitAll, matchType};
    CFDictionaryRef query = CFDictionaryCreate(kCFAllocatorDefault,
                                               keys,
                                               values,
                                               sizeof(keys) / sizeof(*keys),
                                               &kCFTypeDictionaryKeyCallBacks,
                                               &kCFTypeDictionaryValueCallBacks);

    if (query == NULL)
    {
        return errSecAllocate;
    }

    CFTypeRef result = NULL;
    OSStatus status;

    status = SecItemCopyMatching(query, &result);

    CFRelease(query);

    if (status == noErr)
    {
        assert(result != NULL);
        assert(CFGetTypeID(result) == CFArrayGetTypeID());
        CFRetain(result);
        *pCertsOut = (CFArrayRef)result;
    }
    else if (status == errSecItemNotFound)
    {
        assert(result == NULL);
        status = noErr;
    }

    if (result != NULL)
    {
        CFRelease(result);
    }

    return status;
}

int32_t AppleCryptoNative_SecKeychainEnumerateCerts(CFArrayRef* pCertsOut)
{
    return EnumerateKeychain(kSecClassCertificate, pCertsOut);
}

int32_t AppleCryptoNative_SecKeychainEnumerateIdentities(CFArrayRef* pIdentitiesOut)
{
    return EnumerateKeychain(kSecClassIdentity, pIdentitiesOut);
}

int32_t AppleCryptoNative_X509StoreAddCertificate(CFTypeRef certOrIdentity)
{
    OSStatus status;

    assert(certOrIdentity != NULL);

    const void* keys[] = {kSecValueRef};
    const void* values[] = {certOrIdentity};
    CFDictionaryRef query = CFDictionaryCreate(kCFAllocatorDefault,
                                               keys,
                                               values,
                                               sizeof(keys) / sizeof(*keys),
                                               &kCFTypeDictionaryKeyCallBacks,
                                               &kCFTypeDictionaryValueCallBacks);

    if (query == NULL)
    {
        return errSecAllocate;
    }

    status = SecItemAdd(query, NULL);

    return status == errSecDuplicateItem ? noErr : status;
}

int32_t AppleCryptoNative_X509StoreRemoveCertificate(CFTypeRef certOrIdentity, uint8_t isReadOnlyMode)
{
    OSStatus status;
    CFTypeRef result = NULL;

    assert(certOrIdentity != NULL);

    const void* keys[] = {kSecValueRef};
    const void* values[] = {certOrIdentity};
    CFDictionaryRef query = CFDictionaryCreate(kCFAllocatorDefault,
                                               keys,
                                               values,
                                               sizeof(keys) / sizeof(*keys),
                                               &kCFTypeDictionaryKeyCallBacks,
                                               &kCFTypeDictionaryValueCallBacks);

    if (query == NULL)
    {
        return errSecAllocate;
    }

    if (!isReadOnlyMode)
    {
        CFTypeID inputType = CFGetTypeID(certOrIdentity);

        if (inputType == SecCertificateGetTypeID())
        {
            // If we got a certificate as input we have to try to delete private key
            // as well. There's one-to-many relationship between keys and certificates
            // so we save the public key fingerprint first. Then we delete the
            // certificate and look whether there are any remaining ceritificates
            // referencing the same key. If none are found then we try to delete
            // the key.

            SecCertificateRef cert = (SecCertificateRef)CONST_CAST(void*, certOrIdentity);
            SecKeyRef publicKey = NULL;
            CFTypeRef publicKeyLabel = NULL;
            int32_t dummyStatus;

            if (AppleCryptoNative_X509GetPublicKey(cert, &publicKey, &dummyStatus))
            {
                CFDictionaryRef attrs = SecKeyCopyAttributes(publicKey);
                publicKeyLabel = CFRetain(CFDictionaryGetValue(attrs, kSecAttrApplicationLabel));
                CFRelease(attrs);
                CFRelease(publicKey);
            }

            status = SecItemDelete(query);

            CFRelease(query);

            if (status == noErr && publicKeyLabel != NULL)
            {
                OSStatus keyStatus;

                const void* keys[] = {kSecClass, kSecAttrPublicKeyHash};
                const void* values[] = {kSecClassCertificate, publicKeyLabel};
                query = CFDictionaryCreate(kCFAllocatorDefault,
                                           keys,
                                           values,
                                           sizeof(keys) / sizeof(*keys),
                                           &kCFTypeDictionaryKeyCallBacks,
                                           &kCFTypeDictionaryValueCallBacks);

                if (query == NULL)
                {
                    CFRelease(publicKeyLabel);
                    return errSecAllocate;
                }

                result = NULL;
                keyStatus = SecItemCopyMatching(query, &result);

                CFRelease(query);

                if (result != NULL)
                {
                    CFRelease(result);
                }

                if (keyStatus == errSecItemNotFound)
                {
                    const void* keys[] = {kSecClass, kSecAttrApplicationLabel};
                    const void* values[] = {kSecClassKey, publicKeyLabel};
                    query = CFDictionaryCreate(kCFAllocatorDefault,
                                               keys,
                                               values,
                                               sizeof(keys) / sizeof(*keys),
                                               &kCFTypeDictionaryKeyCallBacks,
                                               &kCFTypeDictionaryValueCallBacks);

                    if (query == NULL)
                    {
                        CFRelease(publicKeyLabel);
                        return errSecAllocate;
                    }

                    SecItemDelete(query);

                    CFRelease(query);
                }

                CFRelease(publicKeyLabel);
            }
        }
        else
        {
            status = SecItemDelete(query);

            CFRelease(query);
        }
    }
    else
    {
        status = SecItemCopyMatching(query, &result);

        CFRelease(query);

        if (result != NULL)
        {
            CFRelease(result);
        }
    }

    return status;
}
