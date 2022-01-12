// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509_ios.h"
#include "pal_utilities.h"
#include "pal_x509.h"
#include <dlfcn.h>
#include <pthread.h>

int32_t AppleCryptoNative_X509ImportCertificate(uint8_t* pbData,
                                                int32_t cbData,
                                                PAL_X509ContentType contentType,
                                                CFStringRef cfPfxPassphrase,
                                                SecCertificateRef* pCertOut,
                                                SecIdentityRef* pIdentityOut)
{
    OSStatus status;

    assert(pCertOut != NULL);
    assert(pIdentityOut != NULL);
    assert(pbData != NULL);
    assert(cbData >= 0);

    *pCertOut = NULL;
    *pIdentityOut = NULL;

    if (contentType != PAL_Certificate && contentType != PAL_Pkcs12)
    {
        return errSecUnknownFormat;
    }

    CFDataRef cfData = CFDataCreateWithBytesNoCopy(NULL, pbData, cbData, kCFAllocatorNull);

    if (cfData == NULL)
    {
        return errSecAllocate;
    }

    if (contentType == PAL_Certificate)
    {
        *pCertOut = SecCertificateCreateWithData(NULL, cfData);
        CFRelease(cfData);
        return *pCertOut == NULL ? errSecUnknownFormat : noErr;
    }
    else // PAL_Pkcs12
    {
        const void* keys[] = {kSecImportExportPassphrase};
        const void* values[] = {cfPfxPassphrase};
        CFDictionaryRef attrs = CFDictionaryCreate(kCFAllocatorDefault,
                                                   keys,
                                                   values,
                                                   sizeof(keys) / sizeof(*keys),
                                                   &kCFTypeDictionaryKeyCallBacks,
                                                   &kCFTypeDictionaryValueCallBacks);

        if (attrs == NULL)
        {
            CFRelease(cfData);
            return errSecAllocate;
        }

        CFArrayRef p12Items = NULL;
        status = SecPKCS12Import(cfData, attrs, &p12Items);

        CFRelease(cfData);
        CFRelease(attrs);

        if (status == noErr)
        {
            if (CFArrayGetCount(p12Items) > 0)
            {
                CFDictionaryRef item_dict = CFArrayGetValueAtIndex(p12Items, 0);
                *pIdentityOut = (SecIdentityRef)CFRetain(CFDictionaryGetValue(item_dict, kSecImportItemIdentity));
            }
            CFRelease(p12Items);
        }

        return status;
    }
}

int32_t AppleCryptoNative_X509ImportCollection(uint8_t* pbData,
                                               int32_t cbData,
                                               PAL_X509ContentType contentType,
                                               CFStringRef cfPfxPassphrase,
                                               CFArrayRef* pCollectionOut)
{
    OSStatus status;
    CFMutableArrayRef outItems;

    assert(pCollectionOut != NULL);
    assert(pbData != NULL);
    assert(cbData >= 0);

    *pCollectionOut = NULL;

    if (contentType != PAL_Certificate && contentType != PAL_Pkcs12)
    {
        return errSecUnknownFormat;
    }

    CFDataRef cfData = CFDataCreateWithBytesNoCopy(NULL, pbData, cbData, kCFAllocatorNull);

    if (cfData == NULL)
    {
        return errSecAllocate;
    }

    if (contentType == PAL_Certificate)
    {
        SecCertificateRef certificate = SecCertificateCreateWithData(NULL, cfData);

        CFRelease(cfData);

        if (certificate != NULL)
        {
            outItems = CFArrayCreateMutable(NULL, 1, &kCFTypeArrayCallBacks);

            if (outItems == NULL)
            {
                CFRelease(certificate);
                return errSecAllocate;
            }

            CFArrayAppendValue(outItems, certificate);
            *pCollectionOut = outItems;
            return noErr;
        }
        else
        {
            return errSecUnknownFormat;
        }
    }
    else // PAL_Pkcs12
    {
        const void* keys[] = {kSecImportExportPassphrase};
        const void* values[] = {cfPfxPassphrase};
        CFDictionaryRef attrs = CFDictionaryCreate(kCFAllocatorDefault,
                                                   keys,
                                                   values,
                                                   sizeof(keys) / sizeof(*keys),
                                                   &kCFTypeDictionaryKeyCallBacks,
                                                   &kCFTypeDictionaryValueCallBacks);

        if (attrs == NULL)
        {
            CFRelease(cfData);
            return errSecAllocate;
        }

        CFArrayRef p12Items = NULL;
        status = SecPKCS12Import(cfData, attrs, &p12Items);

        CFRelease(cfData);
        CFRelease(attrs);

        if (status == noErr)
        {
            outItems = CFArrayCreateMutable(NULL, CFArrayGetCount(p12Items), &kCFTypeArrayCallBacks);

            if (outItems == NULL)
            {
                CFRelease(p12Items);
                return errSecAllocate;
            }

            for (int i = 0; i < CFArrayGetCount(p12Items); i++)
            {
                CFDictionaryRef item_dict = CFArrayGetValueAtIndex(p12Items, i);
                SecIdentityRef identity =
                    (SecIdentityRef)CFRetain(CFDictionaryGetValue(item_dict, kSecImportItemIdentity));
                assert(identity != NULL);
                CFArrayAppendValue(outItems, identity);
            }

            CFRelease(p12Items);
            *pCollectionOut = outItems;
        }

        return status;
    }
}