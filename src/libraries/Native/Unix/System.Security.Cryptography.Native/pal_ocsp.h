// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_crypto_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Direct shim to OCSP_REQUEST_free
*/
PALEXPORT void CryptoNative_OcspRequestDestroy(OCSP_REQUEST* request);

/*
Returns the number of bytes required to encode an OCSP_REQUEST
*/
PALEXPORT int32_t CryptoNative_GetOcspRequestDerSize(OCSP_REQUEST* req);

/*
Encodes the OCSP_REQUEST req into the destination buffer, returning the number of bytes written.
*/
PALEXPORT int32_t CryptoNative_EncodeOcspRequest(OCSP_REQUEST* req, uint8_t* buf);

/*
Direct shim to d2i_OCSP_RESPONSE
*/
PALEXPORT OCSP_RESPONSE* CryptoNative_DecodeOcspResponse(const uint8_t* buf, int32_t len);

/*
Direct shim to OCSP_RESPONSE_free
*/
PALEXPORT void CryptoNative_OcspResponseDestroy(OCSP_RESPONSE* response);
