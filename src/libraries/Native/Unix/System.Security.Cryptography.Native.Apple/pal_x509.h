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

#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
/*
Read cbData bytes of data from pbData and interpret it to a collection of certificates (or identities).

If cfPfxPassphrase represents the NULL (but not empty) passphrase and a PFX import gets a password
error then the empty passphrase is automatically attempted.

Any private keys will be loaded into the provided keychain. If the keychain is not provided then
a PFX load will read only the public (certificate) values.

Returns 1 on success (including empty PKCS7 collections), 0 on failure, other values indicate invalid state.

Output:
pCollectionOut: Receives an array which contains SecCertificateRef, SecIdentityRef, and possibly other values which were
read out of the provided blob
pOSStatus: Receives the output of SecItemImport for the last attempted read
*/
PALEXPORT int32_t AppleCryptoNative_X509ImportCollection(uint8_t* pbData,
                                                         int32_t cbData,
                                                         PAL_X509ContentType contentType,
                                                         CFStringRef cfPfxPassphrase,
                                                         SecKeychainRef keychain,
                                                         int32_t exportable,
                                                         CFArrayRef* pCollectionOut,
                                                         int32_t* pOSStatus);

/*
Read cbData bytes of data from pbData and interpret it to a single certificate (or identity).

For a single X.509 certificate, that certificate is emitted.
For a PKCS#7 blob the signing certificate is returned.
For a PKCS#12 blob (PFX) the first public+private pair found is returned, or the first certificate.

If cfPfxPassphrase represents the NULL (but not empty) passphrase and a PFX import gets a password
error then the empty passphrase is automatically attempted.

Any private keys will be loaded into the provided keychain. If the keychain is not provided then
a PFX load will read only the public (certificate) values.

Returns 1 on success, 0 on failure, -2 on a successful read of an empty collection, other values reprepresent invalid
state.

Output:
pCertOut: If the best matched value was a certificate, receives the SecCertificateRef, otherwise receives NULL
pIdentityOut: If the best matched value was an identity, receives the SecIdentityRef, otherwise receives NULL
pOSStatus: Receives the return of the last call to SecItemImport
*/
PALEXPORT int32_t AppleCryptoNative_X509ImportCertificate(uint8_t* pbData,
                                                          int32_t cbData,
                                                          PAL_X509ContentType contentType,
                                                          CFStringRef cfPfxPassphrase,
                                                          SecKeychainRef keychain,
                                                          int32_t exportable,
                                                          SecCertificateRef* pCertOut,
                                                          SecIdentityRef* pIdentityOut,
                                                          int32_t* pOSStatus);

/*
Export the certificates (or identities) in data to the requested format type.

Only PKCS#7 and PKCS#12 are supported at this time.

Returns 1 on success, 0 on failure, any other value indicates invalid state.

Output:
pExportOut: Receives a CFDataRef with the exported blob
pOSStatus: Receives the result of SecItemExport
*/
PALEXPORT int32_t AppleCryptoNative_X509ExportData(CFArrayRef data,
                                                    PAL_X509ContentType type,
                                                    CFStringRef cfExportPassphrase,
                                                    CFDataRef* pExportOut,
                                                    int32_t* pOSStatus);

/*
Find a SecIdentityRef for the given cert and private key in the target keychain.
If the key does not belong to any keychain it is added to the target keychain and left there.
If the certificate does not belong to the target keychain it is added and removed.

Returns 1 on success, 0 on failure, any other value indicates invalid state.

Output:
pIdentityOut: Receives the SecIdentityRef of the mated cert/key pair.
pOSStatus: Receives the result of the last executed system call.
*/
PALEXPORT int32_t AppleCryptoNative_X509CopyWithPrivateKey(SecCertificateRef cert,
                                                           SecKeyRef privateKey,
                                                           SecKeychainRef targetKeychain,
                                                           SecIdentityRef* pIdentityOut,
                                                           int32_t* pOSStatus);

/*
Move the specified certificate and key to the target keychain.
Both the certificate and the key must be ephemeral (not a member of any keychain).
If the private key was specified then search for an identity and present it via pIdentityOut.

Returns 1 on success, 0 on failure, any other value indicates invalid state.

Output:
pIdentityOut: Receives the SecIdentityRef of the mated cert/key pair, when applicable.
pOSStatus: Receives the result of the last executed system call.
*/
PALEXPORT int32_t AppleCryptoNative_X509MoveToKeychain(SecCertificateRef cert,
                                                       SecKeychainRef keychain,
                                                       SecKeyRef privateKey,
                                                       SecIdentityRef* pIdentityOut,
                                                       int32_t* pOSStatus);
#endif
