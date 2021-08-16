// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"
#include "pal_x509.h"

/*
Add a certificate to the specified key store

If the certificate is already in the store, this function simply returns success.
Returns 1 on success, 0 otherwise.
*/
PALEXPORT int32_t AndroidCryptoNative_X509StoreAddCertificate(jobject /*KeyStore*/ store,
                                                              jobject /*X509Certificate*/ cert,
                                                              const char* hashString);

/*
Add a certificate with a private key to the specified key store

If the certificate is already in the store, it is replaced. This means that a certificate
without a private key will be replaced with one with a private key.
Returns 1 on success, 0 otherwise.
*/
PALEXPORT int32_t AndroidCryptoNative_X509StoreAddCertificateWithPrivateKey(jobject /*KeyStore*/ store,
                                                                            jobject /*X509Certificate*/ cert,
                                                                            void* key,
                                                                            PAL_KeyAlgorithm algorithm,
                                                                            const char* hashString);

/*
Get whether or not a certificate is contained in the specified key store
*/
PALEXPORT bool AndroidCryptoNative_X509StoreContainsCertificate(jobject /*KeyStore*/ store,
                                                                jobject /*X509Certificate*/ cert,
                                                                const char* hashString);

/*
Enumerate certificates for the specified key store
The certificate and key passed to the callback will already be a global jobject reference.
*/
typedef void (*EnumCertificatesCallback)(jobject /*X509Certificate*/ cert,
                                         void* privateKey,
                                         PAL_KeyAlgorithm algorithm,
                                         void* context);
PALEXPORT int32_t AndroidCryptoNative_X509StoreEnumerateCertificates(jobject /*KeyStore*/ store,
                                                                     EnumCertificatesCallback cb,
                                                                     void* context);

/*
Enumerate trusted certificates
The certificate passed to the callback will already be a global jobject reference.

Returns 1 on success, 0 otherwise.
*/
typedef void (*EnumTrustedCertificatesCallback)(jobject /*X509Certificate*/ cert, void* context);
PALEXPORT int32_t AndroidCryptoNative_X509StoreEnumerateTrustedCertificates(bool isSystem,
                                                                            EnumTrustedCertificatesCallback cb,
                                                                            void* context);
/*
Open the default key store
*/
PALEXPORT jobject /*KeyStore*/ AndroidCryptoNative_X509StoreOpenDefault(void);

/*
Remove a certificate from the specified key store

If the certificate is not in the store, this function returns success.
Returns 1 on success, 0 otherwise.
*/
PALEXPORT int32_t AndroidCryptoNative_X509StoreRemoveCertificate(jobject /*KeyStore*/ store,
                                                                 jobject /*X509Certificate*/ cert,
                                                                 const char* hashString);
