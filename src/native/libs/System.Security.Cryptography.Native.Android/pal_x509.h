// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"
#include <pal_x509_types.h>

// Creation and lifetime
PALEXPORT jobject /*X509Certificate*/ AndroidCryptoNative_X509Decode(const uint8_t* buf, int32_t len);

/*
Encode a certificate in ASN.1 DER format

Returns 1 on success, -1 on insufficient buffer, 0 otherwise.
The outLen parameter will be set to the length required for encoding the certificate.
*/
PALEXPORT int32_t AndroidCryptoNative_X509Encode(jobject /*X509Certificate*/ cert, uint8_t* out, int32_t* outLen);

/*
Decodes a collection of certificates.

Returns 1 on success, -1 on insufficient buffer, 0 otherwise.
The outLen parameter will be set to the length required for decoding the collection.
*/
PALEXPORT int32_t AndroidCryptoNative_X509DecodeCollection(const uint8_t* buf,
                                                           int32_t bufLen,
                                                           jobject /*X509Certificate*/* out,
                                                           int32_t* outLen);

/*
Exports a collection of certificates in PKCS#7 format

Returns 1 on success, -1 on insufficient buffer, 0 otherwise.
The outLen parameter will be set to the length required for exporting the collection.
*/
PALEXPORT int32_t AndroidCryptoNative_X509ExportPkcs7(jobject* /*X509Certificate[]*/ certs,
                                                      int32_t certsLen,
                                                      uint8_t* out,
                                                      int32_t* outLen);

PALEXPORT PAL_X509ContentType AndroidCryptoNative_X509GetContentType(const uint8_t* buf, int32_t len);

// Matches managed PAL_KeyAlgorithm enum
enum
{
    PAL_DSA = 0,
    PAL_EC = 1,
    PAL_RSA = 2,

    PAL_UnknownAlgorithm = -1,
};
typedef int32_t PAL_KeyAlgorithm;

/*
Gets an opaque handle for a certificate's public key

Returns null if the requested algorithm does not match that of the public key.
*/
PALEXPORT void* AndroidCryptoNative_X509PublicKey(jobject /*X509Certificate*/ cert, PAL_KeyAlgorithm algorithm);
