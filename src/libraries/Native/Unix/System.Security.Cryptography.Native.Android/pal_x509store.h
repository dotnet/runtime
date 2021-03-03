// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"

/*
Add a certificate to the specified key store

Returns 1 on success, 0 otherwise.
*/
PALEXPORT int32_t AndroidCryptoNative_X509StoreAddCertificate(jobject /*KeyStore*/ store,
                                                              jobject /*X509Certificate*/ cert,
                                                              const char* thumbprint);

/*
Get whether or not a certificate is contained in the specified key store
*/
PALEXPORT bool AndroidCryptoNative_X509StoreContainsCertificate(jobject /*KeyStore*/ store, const char* thumbprint);

typedef void (*EnumCertificatesCallback)(jobject cert, void* context);

/*
Enumerate certificates for the specified key store
The certificate passed to the callback will already be a global jobject reference.
*/
PALEXPORT void AndroidCryptoNative_X509StoreEnumerateCertificates(jobject /*KeyStore*/ store,
                                                                  EnumCertificatesCallback cb,
                                                                  void* context);

/*
Enumerate trusted certificates
The certificate passed to the callback will already be a global jobject reference.

Returns 1 on success, 0 otherwise.
*/
PALEXPORT int32_t AndroidCryptoNative_X509StoreEnumerateTrustedCertificates(bool isSystem,
                                                                            EnumCertificatesCallback cb,
                                                                            void* context);
/*
Open the default key store
*/
PALEXPORT jobject /*KeyStore*/ AndroidCryptoNative_X509StoreOpenDefault(void);

/*
Remove a certificate from the specified key store

Returns 1 on success, 0 otherwise.
*/
PALEXPORT int32_t AndroidCryptoNative_X509StoreRemoveCertificate(jobject /*KeyStore*/ store, const char* thumbprint);
