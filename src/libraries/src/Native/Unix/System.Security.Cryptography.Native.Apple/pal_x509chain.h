// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_digest.h"
#include "pal_seckey.h"
#include "pal_compiler.h"

#include <Security/Security.h>

enum
{
    PAL_X509ChainNoError = 0,
    PAL_X509ChainNotTimeValid = 0x00000001,
    PAL_X509ChainNotTimeNested = 0x00000002,
    PAL_X509ChainRevoked = 0x00000004,
    PAL_X509ChainNotSignatureValid = 0x00000008,
    PAL_X509ChainNotValidForUsage = 0x00000010,
    PAL_X509ChainUntrustedRoot = 0x00000020,
    PAL_X509ChainRevocationStatusUnknown = 0x00000040,
    PAL_X509ChainCyclic = 0x00000080,
    PAL_X509ChainInvalidExtension = 0x00000100,
    PAL_X509ChainInvalidPolicyConstraints = 0x00000200,
    PAL_X509ChainInvalidBasicConstraints = 0x00000400,
    PAL_X509ChainInvalidNameConstraints = 0x00000800,
    PAL_X509ChainHasNotSupportedNameConstraint = 0x00001000,
    PAL_X509ChainHasNotDefinedNameConstraint = 0x00002000,
    PAL_X509ChainHasNotPermittedNameConstraint = 0x00004000,
    PAL_X509ChainHasExcludedNameConstraint = 0x00008000,
    PAL_X509ChainPartialChain = 0x00010000,
    PAL_X509ChainCtlNotTimeValid = 0x00020000,
    PAL_X509ChainCtlNotSignatureValid = 0x00040000,
    PAL_X509ChainCtlNotValidForUsage = 0x00080000,
    PAL_X509ChainOfflineRevocation = 0x01000000,
    PAL_X509ChainNoIssuanceChainPolicy = 0x02000000,
    PAL_X509ChainExplicitDistrust = 0x04000000,
    PAL_X509ChainHasNotSupportedCriticalExtension = 0x08000000,
    PAL_X509ChainHasWeakSignature = 0x00100000,
};
typedef uint32_t PAL_X509ChainStatusFlags;

#define PAL_X509ChainErrorNone             0
#define PAL_X509ChainErrorUnknownValueType 0x0001L << 32
#define PAL_X509ChainErrorUnknownValue     0x0002L << 32
typedef uint64_t PAL_X509ChainErrorFlags;

/*
Create a SecPolicyRef representing the basic X.509 policy
*/
DLLEXPORT SecPolicyRef AppleCryptoNative_X509ChainCreateDefaultPolicy(void);

/*
Create a SecPolicyRef which checks for revocation (OCSP or CRL)
*/
DLLEXPORT SecPolicyRef AppleCryptoNative_X509ChainCreateRevocationPolicy(void);

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
DLLEXPORT int32_t
AppleCryptoNative_X509ChainCreate(CFTypeRef certs, CFTypeRef policies, SecTrustRef* pTrustOut, int32_t* pOSStatus);

/*
Evaluate a certificate chain.

allowNetwork set to true enables fetching of CRL and AIA records

Returns 1 if the chain built successfully, 0 if chain building failed, any other value for invalid
state.  Note that an untrusted chain building successfully still returns 1.

Output:
pOSStatus: Receives the result of SecTrustEvaluate
*/
DLLEXPORT int32_t AppleCryptoNative_X509ChainEvaluate(SecTrustRef chain,
                                                      CFDateRef cfEvaluationTime,
                                                      bool allowNetwork,
                                                      int32_t* pOSStatus);

/*
Gets the number of certificates in the chain.
*/
DLLEXPORT int64_t AppleCryptoNative_X509ChainGetChainSize(SecTrustRef chain);

/*
Fetches the SecCertificateRef at a given position in the chain. Position 0 is the End-Entity
certificate, postiion 1 is the issuer of position 0, et cetera.
*/
DLLEXPORT SecCertificateRef AppleCryptoNative_X509ChainGetCertificateAtIndex(SecTrustRef chain, int64_t index);

/*
Get a CFRetain()ed array of dictionaries which contain the detailed results for each element in
the certificate chain.
*/
DLLEXPORT CFArrayRef AppleCryptoNative_X509ChainGetTrustResults(SecTrustRef chain);

/*
Get the PAL_X509ChainStatusFlags values for the certificate at the requested position within the
chain.

Returns 0 on success, non-zero on error.

Output:
pdwStatus: Receives a flags value for the various status codes that went awry at the given position
*/
DLLEXPORT int32_t AppleCryptoNative_X509ChainGetStatusAtIndex(CFArrayRef details, int64_t index, int32_t* pdwStatus);

/*
Looks up the equivalent OSStatus code for a given PAL_X509ChainStatusFlags single-bit value.

Returns errSecCoreFoundationUnknown on bad/unmapped input, otherwise the appropriate response.

Note that PAL_X509ChainNotTimeValid is an ambiguous code, it could be errSecCertificateExpired or
errSecCertificateNotValidYet. A caller should resolve that code via other means.
*/
DLLEXPORT int32_t AppleCryptoNative_GetOSStatusForChainStatus(PAL_X509ChainStatusFlags chainStatusFlag);
