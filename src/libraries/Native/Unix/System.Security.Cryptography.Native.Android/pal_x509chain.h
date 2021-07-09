// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

typedef struct X509ChainContext_t X509ChainContext;
typedef struct ValidationError_t ValidationError;

/*
Create a context for building a certificate chain
*/
PALEXPORT X509ChainContext* AndroidCryptoNative_X509ChainCreateContext(jobject /*X509Certificate*/ cert,
                                                                       jobject* /*X509Certificate*/ extraStore,
                                                                       int32_t extraStoreLen);

/*
Destroy the context
*/
PALEXPORT void AndroidCryptoNative_X509ChainDestroyContext(X509ChainContext* ctx);

/*
Build a certificate path

Always validates time and trust root.
*/
PALEXPORT int32_t AndroidCryptoNative_X509ChainBuild(X509ChainContext* ctx, int64_t timeInMsFromUnixEpoch);

/*
Return the number of certificates in the path
*/
PALEXPORT int32_t AndroidCryptoNative_X509ChainGetCertificateCount(X509ChainContext* ctx);

/*
Get the certificates in the path.

Returns the number of certificates exported.
*/
PALEXPORT int32_t AndroidCryptoNative_X509ChainGetCertificates(X509ChainContext* ctx,
                                                               jobject* /*X509Certificate[]*/ certs,
                                                               int32_t certsLen);

/*
Return the number of errors encountered when building and validating the certificate path
*/
PALEXPORT int32_t AndroidCryptoNative_X509ChainGetErrorCount(X509ChainContext* ctx);

/*
Get the errors

Returns 1 on success, 0 otherwise
*/
PALEXPORT int32_t AndroidCryptoNative_X509ChainGetErrors(X509ChainContext* ctx,
                                                         ValidationError* errors,
                                                         int32_t errorsLen);

/*
Set the custom trust store
*/
PALEXPORT int32_t AndroidCryptoNative_X509ChainSetCustomTrustStore(X509ChainContext* ctx,
                                                                   jobject* /*X509Certificate[]*/ customTrustStore,
                                                                   int32_t customTrustStoreLen);

/*
Returns true if revocation checking is supported. Returns false otherwise.
*/
PALEXPORT bool AndroidCryptoNative_X509ChainSupportsRevocationOptions(void);

// Matches managed X509RevocationMode enum
enum
{
    X509RevocationMode_NoCheck = 0,
    X509RevocationMode_Online = 1,
    X509RevocationMode_Offline = 2,
};
typedef uint32_t PAL_X509RevocationMode;

// Matches managed X509RevocationFlag enum
enum
{
    X509RevocationFlag_EndCertificateOnly = 0,
    X509RevocationFlag_EntireChain = 1,
    X509RevocationFlag_ExcludeRoot = 2,
};
typedef uint32_t PAL_X509RevocationFlag;

/*
Validate a certificate path.
*/
PALEXPORT int32_t AndroidCryptoNative_X509ChainValidate(X509ChainContext* chain,
                                                        PAL_X509RevocationMode revocationMode,
                                                        PAL_X509RevocationFlag revocationFlag,
                                                        bool* checkedRevocation);
