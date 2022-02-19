// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ocsp.h"



void CryptoNative_OcspRequestDestroy(OCSP_REQUEST* request)
{
    if (request != NULL)
    {
        OCSP_REQUEST_free(request);
    }
}

int32_t CryptoNative_GetOcspRequestDerSize(OCSP_REQUEST* req)
{
    ERR_clear_error();
    return i2d_OCSP_REQUEST(req, NULL);
}

int32_t CryptoNative_EncodeOcspRequest(OCSP_REQUEST* req, uint8_t* buf)
{
    ERR_clear_error();
    return i2d_OCSP_REQUEST(req, &buf);
}

OCSP_RESPONSE* CryptoNative_DecodeOcspResponse(const uint8_t* buf, int32_t len)
{
    ERR_clear_error();

    if (buf == NULL || len == 0)
    {
        return NULL;
    }

    return d2i_OCSP_RESPONSE(NULL, &buf, len);
}

void CryptoNative_OcspResponseDestroy(OCSP_RESPONSE* response)
{
    if (response != NULL)
    {
        OCSP_RESPONSE_free(response);
    }
}
