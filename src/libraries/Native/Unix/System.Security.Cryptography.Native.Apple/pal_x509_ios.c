// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509.h"
#include "pal_x509_ios.h"
#include "pal_utilities.h"
#include <dlfcn.h>
#include <pthread.h>

int32_t AppleCryptoNative_X509ImportCertificate(uint8_t* pbData,
                                                int32_t cbData,
                                                PAL_X509ContentType contentType,
                                                CFStringRef cfPfxPassphrase,
                                                SecCertificateRef* pCertOut,
                                                SecIdentityRef* pIdentityOut,
                                                int32_t* pOSStatus)
{
    assert(pCertOut != NULL);
    assert(pIdentityOut != NULL);
    assert(pOSStatus != NULL);
    assert(pbData != NULL);
    assert(cbData >= 0);

    *pCertOut = NULL;
    *pIdentityOut = NULL;
    *pOSStatus = noErr;

    CFDataRef cfData = CFDataCreateWithBytesNoCopy(NULL, pbData, cbData, kCFAllocatorNull);

    if (cfData == NULL)
    {
        *pOSStatus = errSecAllocate;
        return 0;
    }

    if (contentType == PAL_Certificate)
    {
        *pCertOut = SecCertificateCreateWithData(NULL, cfData);
        CFRelease(cfData);
        *pOSStatus = *pCertOut == NULL ? errSecUnknownFormat : 0;
        return *pCertOut != NULL;
    }
    else if (contentType == PAL_Pkcs12)
    {
        CFArrayRef p12Items = NULL;
        CFMutableDictionaryRef attrs = CFDictionaryCreateMutable(
            kCFAllocatorDefault, 1, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);
        CFDictionaryAddValue(attrs, kSecImportExportPassphrase, cfPfxPassphrase);
        *pOSStatus = SecPKCS12Import(cfData, attrs, &p12Items);
        CFRelease(cfData);
        CFRelease(attrs);
        if (*pOSStatus == noErr)
        {
            if (CFArrayGetCount(p12Items) > 0)
            {
                CFDictionaryRef item_dict = CFArrayGetValueAtIndex(p12Items, 0);
                *pIdentityOut = (SecIdentityRef)CFRetain(CFDictionaryGetValue(item_dict, kSecImportItemIdentity));
            }
            CFRelease(p12Items);
        }
        return *pIdentityOut != NULL;
    }

    CFRelease(cfData);
    *pOSStatus = errSecUnknownFormat;
    return 0;
}


int32_t AppleCryptoNative_X509ImportCollection(uint8_t* pbData,
                                               int32_t cbData,
                                               PAL_X509ContentType contentType,
                                               CFStringRef cfPfxPassphrase,
                                               CFArrayRef* pCollectionOut,
                                               int32_t* pOSStatus)
{
    CFMutableArrayRef outItems;

    assert(pCollectionOut != NULL);
    assert(pOSStatus != NULL);
    assert(pbData != NULL);
    assert(cbData >= 0);

    *pCollectionOut = NULL;
    *pOSStatus = noErr;

    CFDataRef cfData = CFDataCreateWithBytesNoCopy(NULL, pbData, cbData, kCFAllocatorNull);

    if (cfData == NULL)
    {
        *pOSStatus = errSecAllocate;
        return 0;
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
                *pOSStatus = errSecAllocate;
                return 0;
            }

            CFArrayAppendValue(outItems, certificate);
            *pCollectionOut = outItems;
            return 1;
        }
        else
        {
            *pOSStatus = errSecUnknownFormat;
            return 0;
        }
    }
    else if (contentType == PAL_Pkcs12)
    {
        CFArrayRef p12Items = NULL;
        CFMutableDictionaryRef attrs = CFDictionaryCreateMutable(
            kCFAllocatorDefault, 1, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);
        CFDictionaryAddValue(attrs, kSecImportExportPassphrase, cfPfxPassphrase);
        *pOSStatus = SecPKCS12Import(cfData, attrs, &p12Items);
        CFRelease(cfData);
        CFRelease(attrs);

        if (*pOSStatus == noErr)
        {
            outItems = CFArrayCreateMutable(NULL, CFArrayGetCount(p12Items), &kCFTypeArrayCallBacks);

            if (outItems == NULL)
            {
                CFRelease(p12Items);
                *pOSStatus = errSecAllocate;
                return 0;
            }

            for (int i = 0; i < CFArrayGetCount(p12Items); i++)
            {
                CFDictionaryRef item_dict = CFArrayGetValueAtIndex(p12Items, i);
                SecIdentityRef identity = (SecIdentityRef)CFRetain(CFDictionaryGetValue(item_dict, kSecImportItemIdentity));
                CFArrayAppendValue(outItems, identity);
            }

            CFRelease(p12Items);
            *pCollectionOut = outItems;

            return 1;
        }

        return 0;
    }

    CFRelease(cfData);
    *pOSStatus = errSecUnknownFormat;
    return 0;
}