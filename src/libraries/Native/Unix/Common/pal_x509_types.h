// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// Matches managed X509ChainStatusFlags enum
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

// Matches managed X509ContentType enum
enum
{
    PAL_X509Unknown = 0,
    PAL_Certificate = 1,
    PAL_SerializedCert = 2,
    PAL_Pkcs12 = 3,
    PAL_SerializedStore = 4,
    PAL_Pkcs7 = 5,
    PAL_Authenticode = 6,
};
typedef uint32_t PAL_X509ContentType;
