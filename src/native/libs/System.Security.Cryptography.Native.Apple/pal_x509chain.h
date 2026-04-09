// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_digest.h"
#include "pal_seckey.h"
#include "pal_compiler.h"
#include <pal_x509_types.h>

#include <Security/Security.h>

#define PAL_X509ChainErrorNone             0
#define PAL_X509ChainErrorUnknownValueType (((uint64_t)0x0001L) << 32)
#define PAL_X509ChainErrorUnknownValue     (((uint64_t)0x0002L) << 32)
typedef uint64_t PAL_X509ChainErrorFlags;

/*
Create a SecPolicyRef representing the basic X.509 policy
*/
PALEXPORT SecPolicyRef AppleCryptoNative_X509ChainCreateDefaultPolicy(void);

/*
Create a SecPolicyRef which checks for revocation (OCSP or CRL)
*/
PALEXPORT SecPolicyRef AppleCryptoNative_X509ChainCreateRevocationPolicy(void);

/*
Create a SecTrustRef to build a chain over the specified certificates with the given policies.

certs can be either a single SecCertificateRef or an array of SecCertificateRefs. The first element
in the array will be the certificate for which the chain is built, all other certs are to help in
building intermediates.

Returns 1 on success, 0 on failure, any other value for invalid state.

Output:
pTrustOut: Receives the SecTrustRef to build the chain, in an unbuilt state
pOSStatus: Receives the result of SecTrustCreateWithCertificates
*/
PALEXPORT int32_t
AppleCryptoNative_X509ChainCreate(CFTypeRef certs, CFTypeRef policies, SecTrustRef* pTrustOut, int32_t* pOSStatus);

/*
Evaluate a certificate chain.

allowNetwork set to true enables fetching of CRL and AIA records

Returns 1 if the chain built successfully, 0 if chain building failed, any other value for invalid
state.  Note that an untrusted chain building successfully still returns 1.

Output:
pOSStatus: Receives the result of SecTrustEvaluate
*/
PALEXPORT int32_t AppleCryptoNative_X509ChainEvaluate(SecTrustRef chain,
                                                      CFDateRef cfEvaluationTime,
                                                      bool allowNetwork,
                                                      int32_t* pOSStatus);

/*
Gets the number of certificates in the chain.
*/
PALEXPORT int64_t AppleCryptoNative_X509ChainGetChainSize(SecTrustRef chain);

/*
Fetches the SecCertificateRef at a given position in the chain. Position 0 is the End-Entity
certificate, postiion 1 is the issuer of position 0, et cetera.
*/
PALEXPORT SecCertificateRef AppleCryptoNative_X509ChainGetCertificateAtIndex(SecTrustRef chain, int64_t index);

/*
Get a CFRetain()ed array of dictionaries which contain the detailed results for each element in
the certificate chain.
*/
PALEXPORT CFArrayRef AppleCryptoNative_X509ChainGetTrustResults(SecTrustRef chain);

/*
Get the PAL_X509ChainStatusFlags values for the certificate at the requested position within the
chain.

Returns 0 on success, non-zero on error.

Output:
pdwStatus: Receives a flags value for the various status codes that went awry at the given position
*/
PALEXPORT int32_t AppleCryptoNative_X509ChainGetStatusAtIndex(CFArrayRef details, int64_t index, int32_t* pdwStatus);

/*
Looks up the equivalent OSStatus code for a given PAL_X509ChainStatusFlags single-bit value.

Returns errSecCoreFoundationUnknown on bad/unmapped input, otherwise the appropriate response.

Note that PAL_X509ChainNotTimeValid is an ambiguous code, it could be errSecCertificateExpired or
errSecCertificateNotValidYet. A caller should resolve that code via other means.
*/
PALEXPORT int32_t AppleCryptoNative_GetOSStatusForChainStatus(PAL_X509ChainStatusFlags chainStatusFlag);

/*
Sets the trusted certificates used when evaluating a chain.
*/
PALEXPORT int32_t AppleCryptoNative_X509ChainSetTrustAnchorCertificates(SecTrustRef chain, CFArrayRef anchorCertificates);
