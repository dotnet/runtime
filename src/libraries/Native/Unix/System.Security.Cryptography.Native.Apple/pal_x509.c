// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509.h"
#include "pal_utilities.h"
#include <dlfcn.h>
#include <pthread.h>

#if !defined(TARGET_MACCATALYST)
static pthread_once_t once = PTHREAD_ONCE_INIT;
static SecKeyRef (*secCertificateCopyKey)(SecCertificateRef);
static OSStatus (*secCertificateCopyPublicKey)(SecCertificateRef, SecKeyRef*);
#endif

int32_t
AppleCryptoNative_X509DemuxAndRetainHandle(CFTypeRef handle, SecCertificateRef* pCertOut, SecIdentityRef* pIdentityOut)
{
    if (pCertOut != NULL)
        *pCertOut = NULL;
    if (pIdentityOut != NULL)
        *pIdentityOut = NULL;

    if (handle == NULL || pCertOut == NULL || pIdentityOut == NULL)
        return kErrorBadInput;

    CFTypeID objectType = CFGetTypeID(handle);

    if (objectType == SecIdentityGetTypeID())
    {
        *pIdentityOut = (SecIdentityRef)CONST_CAST(void *, handle);
    }
    else if (objectType == SecCertificateGetTypeID())
    {
        *pCertOut = (SecCertificateRef)CONST_CAST(void *, handle);
    }
    else
    {
        return 0;
    }

    CFRetain(handle);
    return 1;
}

#if !defined(TARGET_MACCATALYST)
static void InitCertificateCopy()
{
#if defined(TARGET_IOS) || defined(TARGET_TVOS)
    // SecCertificateCopyPublicKey on iOS/tvOS has same function prototype as SecCertificateCopyKey
    secCertificateCopyKey = (SecKeyRef (*)(SecCertificateRef))dlsym(RTLD_DEFAULT, "SecCertificateCopyKey");
    if (secCertificateCopyKey == NULL)
    {
        secCertificateCopyKey = (SecKeyRef (*)(SecCertificateRef))dlsym(RTLD_DEFAULT, "SecCertificateCopyPublicKey");
    }
#else
    secCertificateCopyKey = (SecKeyRef (*)(SecCertificateRef))dlsym(RTLD_DEFAULT, "SecCertificateCopyKey");
    secCertificateCopyPublicKey = (OSStatus (*)(SecCertificateRef, SecKeyRef*))dlsym(RTLD_DEFAULT, "SecCertificateCopyPublicKey");
#endif
}
#endif

int32_t
AppleCryptoNative_X509GetPublicKey(SecCertificateRef cert, SecKeyRef* pPublicKeyOut, int32_t* pOSStatusOut)
{
    if (pPublicKeyOut != NULL)
        *pPublicKeyOut = NULL;
    if (pOSStatusOut != NULL)
        *pOSStatusOut = noErr;

    if (cert == NULL || pPublicKeyOut == NULL || pOSStatusOut == NULL)
        return kErrorUnknownState;

#if !defined(TARGET_MACCATALYST)
    pthread_once(&once, InitCertificateCopy);
    // SecCertificateCopyPublicKey was deprecated in 10.14, so use SecCertificateCopyKey on the systems that have it (10.14+),
    // and SecCertificateCopyPublicKey on the systems that don’t.
    if (secCertificateCopyKey != NULL)
    {
        *pPublicKeyOut = (*secCertificateCopyKey)(cert);
    }
    else if (secCertificateCopyPublicKey != NULL)
    {
        *pOSStatusOut = (*secCertificateCopyPublicKey)(cert, pPublicKeyOut);
    }
    else
    {
        return kErrorBadInput;
    }
    return (*pOSStatusOut == noErr);
#else
    *pPublicKeyOut = SecCertificateCopyKey(cert);
    return 1;
#endif
}

PAL_X509ContentType AppleCryptoNative_X509GetContentType(uint8_t* pbData, int32_t cbData)
{
    if (pbData == NULL || cbData < 0)
        return PAL_X509Unknown;

    CFDataRef cfData = CFDataCreateWithBytesNoCopy(NULL, pbData, cbData, kCFAllocatorNull);

    if (cfData == NULL)
        return PAL_X509Unknown;

    // The sniffing order is:
    // * X509 DER
    // * PKCS7 PEM/DER
    // * PKCS12 DER (or PEM if Apple has non-standard support for that)
    // * X509 PEM or PEM aggregate (or DER, but that already matched)
    //
    // If the X509 PEM check is done first SecItemImport will erroneously match
    // some PKCS#7 blobs and say they were certificates.
    //
    // Likewise, if the X509 DER check isn't done first, Apple will report it as
    // being a PKCS#7.
    SecCertificateRef certref = SecCertificateCreateWithData(NULL, cfData);

    if (certref != NULL)
    {
        CFRelease(cfData);
        CFRelease(certref);
        return PAL_Certificate;
    }

#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
    SecExternalFormat dataFormat = kSecFormatPKCS7;
    SecExternalFormat actualFormat = dataFormat;
    SecExternalItemType itemType = kSecItemTypeAggregate;
    SecExternalItemType actualType = itemType;

    OSStatus osStatus = SecItemImport(cfData, NULL, &actualFormat, &actualType, 0, NULL, NULL, NULL);

    if (osStatus == noErr)
    {
        if (actualType == itemType && actualFormat == dataFormat)
        {
            CFRelease(cfData);
            return PAL_Pkcs7;
        }
    }

    dataFormat = kSecFormatPKCS12;
    actualFormat = dataFormat;
    itemType = kSecItemTypeAggregate;
    actualType = itemType;

    osStatus = SecItemImport(cfData, NULL, &actualFormat, &actualType, 0, NULL, NULL, NULL);

    if (osStatus == errSecPassphraseRequired)
    {
        dataFormat = kSecFormatPKCS12;
        actualFormat = dataFormat;
        itemType = kSecItemTypeAggregate;
        actualType = itemType;

        SecItemImportExportKeyParameters importParams;
        memset(&importParams, 0, sizeof(SecItemImportExportKeyParameters));

        importParams.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;
        importParams.passphrase = CFSTR("");

        osStatus = SecItemImport(cfData, NULL, &actualFormat, &actualType, 0, &importParams, NULL, NULL);

        CFRelease(importParams.passphrase);
        importParams.passphrase = NULL;
    }

    if (osStatus == noErr || osStatus == errSecPkcs12VerifyFailure)
    {
        if (actualType == itemType && actualFormat == dataFormat)
        {
            CFRelease(cfData);
            return PAL_Pkcs12;
        }
    }

    dataFormat = kSecFormatX509Cert;
    actualFormat = dataFormat;
    itemType = kSecItemTypeCertificate;
    actualType = itemType;

    osStatus = SecItemImport(cfData, NULL, &actualFormat, &actualType, 0, NULL, NULL, NULL);

    if (osStatus == noErr)
    {
        if ((actualType == itemType && actualFormat == dataFormat) ||
            (actualType == kSecItemTypeAggregate && actualFormat == kSecFormatPEMSequence))
        {
            CFRelease(cfData);
            return PAL_Certificate;
        }
    }
#endif

    CFRelease(cfData);
    return PAL_X509Unknown;
}

int32_t AppleCryptoNative_X509CopyCertFromIdentity(SecIdentityRef identity, SecCertificateRef* pCertOut)
{
    if (pCertOut != NULL)
        *pCertOut = NULL;

    // This function handles null inputs for both identity and cert.
    return SecIdentityCopyCertificate(identity, pCertOut);
}

int32_t AppleCryptoNative_X509CopyPrivateKeyFromIdentity(SecIdentityRef identity, SecKeyRef* pPrivateKeyOut)
{
    if (pPrivateKeyOut != NULL)
        *pPrivateKeyOut = NULL;

    // This function handles null inputs for both identity and key
    return SecIdentityCopyPrivateKey(identity, pPrivateKeyOut);
}

int32_t AppleCryptoNative_X509GetRawData(SecCertificateRef cert, CFDataRef* ppDataOut, int32_t* pOSStatus)
{
    if (cert == NULL || ppDataOut == NULL || pOSStatus == NULL)
    {
        if (ppDataOut != NULL)
            *ppDataOut = NULL;
        if (pOSStatus != NULL)
            *pOSStatus = noErr;
        return kErrorBadInput;
    }

    *ppDataOut = SecCertificateCopyData(cert);
    *pOSStatus = *ppDataOut == NULL ? errSecParam : noErr;
    return (*pOSStatus == noErr);
}

int32_t AppleCryptoNative_X509GetSubjectSummary(SecCertificateRef cert, CFStringRef* ppSummaryOut)
{
    if (ppSummaryOut != NULL)
        *ppSummaryOut = NULL;

    if (cert == NULL || ppSummaryOut == NULL)
        return kErrorBadInput;

    *ppSummaryOut = SecCertificateCopySubjectSummary(cert);
    return (*ppSummaryOut != NULL);
}
