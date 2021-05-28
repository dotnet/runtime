// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_digest.h"
#include "pal_seckey.h"
#include <pal_x509_types.h>

#include <Security/Security.h>

/*
Read cbData bytes of data from pbData and interpret it to a single certificate (or identity).

For a single X.509 certificate, that certificate is emitted.
For a PKCS#7 blob the signing certificate is returned.
For a PKCS#12 blob (PFX) the first public+private pair found is returned, or the first certificate.

If cfPfxPassphrase represents the NULL (but not empty) passphrase and a PFX import gets a password
error then the empty passphrase is automatically attempted.

Returns the last OSStatus value.

Output:
pCertOut: If the best matched value was a certificate, receives the SecCertificateRef, otherwise receives NULL
pIdentityOut: If the best matched value was an identity, receives the SecIdentityRef, otherwise receives NULL
*/
PALEXPORT int32_t AppleCryptoNative_X509ImportCertificate(uint8_t* pbData,
                                                          int32_t cbData,
                                                          PAL_X509ContentType contentType,
                                                          CFStringRef cfPfxPassphrase,
                                                          SecCertificateRef* pCertOut,
                                                          SecIdentityRef* pIdentityOut);

/*
Read cbData bytes of data from pbData and interpret it to a collection of certificates (or identities).

If cfPfxPassphrase represents the NULL (but not empty) passphrase and a PFX import gets a password
error then the empty passphrase is automatically attempted.

Returns the last OSStatus value.

Output:
pCollectionOut: Receives an array which contains SecCertificateRef, SecIdentityRef, and possibly other values which were
read out of the provided blob
*/
PALEXPORT int32_t AppleCryptoNative_X509ImportCollection(uint8_t* pbData,
                                                         int32_t cbData,
                                                         PAL_X509ContentType contentType,
                                                         CFStringRef cfPfxPassphrase,
                                                         CFArrayRef* pCollectionOut);
