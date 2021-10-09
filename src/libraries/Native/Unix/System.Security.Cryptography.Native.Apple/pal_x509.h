// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_digest.h"
#include "pal_seckey.h"
#include "pal_compiler.h"
#include <pal_x509_types.h>

#include <Security/Security.h>

/*
Given a handle, determine if it represents a SecCertificateRef, SecIdentityRef, or other.
If the handle is a certificate or identity it is CFRetain()ed (and must later be CFRelease()d).

Returns 1 if the handle was a certificate or identity, 0 otherwise (other values on invalid state).

Output:
pCertOut: If handle is a certificate, receives handle, otherwise NULL
pIdentityut: If handle is an identity, receives handle, otherwise NULL
*/
PALEXPORT int32_t
AppleCryptoNative_X509DemuxAndRetainHandle(CFTypeRef handle, SecCertificateRef* pCertOut, SecIdentityRef* pIdentityOut);

/*
Extract a SecKeyRef for the public key from the certificate handle.

Returns 1 on success, 0 on failure, any other value on invalid state.

Output:
pPublicKeyOut: Receives a CFRetain()ed SecKeyRef for the public key
pOSStatusOut: Receives the result of SecCertificateCopyKey or SecCertificateCopyPublicKey, depending on the OS version.
*/
PALEXPORT int32_t
AppleCryptoNative_X509GetPublicKey(SecCertificateRef cert, SecKeyRef* pPublicKeyOut, int32_t* pOSStatusOut);

/*
Determines the data type of the provided input.

Returns the data (format) type of the provided input, PAL_X509Unknown if it cannot be determined.
*/
PALEXPORT PAL_X509ContentType AppleCryptoNative_X509GetContentType(uint8_t* pbData, int32_t cbData);

/*
Extract a SecCertificateRef for the certificate from an identity handle.

Returns the result of SecIdentityCopyCertificate.

Output:
pCertOut: Receives a SecCertificateRef for the certificate associated with the identity
*/
PALEXPORT int32_t AppleCryptoNative_X509CopyCertFromIdentity(SecIdentityRef identity, SecCertificateRef* pCertOut);

/*
Extract a SecKeyRef for the private key from an identity handle.

Returns the result of SecIdentityCopyPrivateKey

Output:
pPrivateKeyOut: Receives a SecKeyRef for the private key associated with the identity
*/
PALEXPORT int32_t AppleCryptoNative_X509CopyPrivateKeyFromIdentity(SecIdentityRef identity, SecKeyRef* pPrivateKeyOut);

/*
Extract the DER encoded value of a certificate (public portion only).

Returns 1 on success, 0 on failure, any other value indicates invalid state.

Output:
ppDataOut: Receives a CFDataRef with the exported blob
pOSStatus: Receives the result of SecItemExport
*/
PALEXPORT int32_t AppleCryptoNative_X509GetRawData(SecCertificateRef cert, CFDataRef* ppDataOut, int32_t* pOSStatus);

/*
Extract a string that contains a human-readable summary of the contents of the certificate

Returns 1 on success, 0 on failure, any other value indicates invalid state.

Output:
ppSummaryOut: Receives a CFDataRef with the exported blob
*/
PALEXPORT int32_t AppleCryptoNative_X509GetSubjectSummary(SecCertificateRef cert, CFStringRef* ppSummaryOut);
