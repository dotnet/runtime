// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey.h"
#include "pal_evp_pkey_raw_signverify.h"
#include "pal_utilities.h"
#include "openssl.h"
#include <assert.h>

int32_t CryptoNative_EvpPKeySignPure(EVP_PKEY *pkey,
                                     void* extraHandle,
                                     uint8_t* msg, int32_t msgLen,
                                     uint8_t* context, int32_t contextLen,
                                     uint8_t* destination, int32_t destinationLen)
{

    assert(pkey);
    assert(msgLen >= 0);
    assert(contextLen >= 0);
    assert(destination);

#if defined(NEED_OPENSSL_3_0) && HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return -1;
    }

    ERR_clear_error();

    EVP_PKEY_CTX* ctx = NULL;

    int ret = -1;

    ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);
    if (!ctx)
    {
        goto done;
    }

    {
        OSSL_PARAM contextParams[] =
        {
            OSSL_PARAM_construct_end(),
            OSSL_PARAM_construct_end(),
        };

        if (context)
        {
            contextParams[0] = OSSL_PARAM_construct_octet_string(OSSL_SIGNATURE_PARAM_CONTEXT_STRING, (void*)context, Int32ToSizeT(contextLen));
        }

        if (EVP_PKEY_sign_message_init(ctx, NULL, contextParams) <= 0)
        {
            goto done;
        }

        size_t dstLen = Int32ToSizeT(destinationLen);
        if (EVP_PKEY_sign(ctx, destination, &dstLen, msg, Int32ToSizeT(msgLen)) == 1)
        {
            if (dstLen != Int32ToSizeT(destinationLen))
            {
                assert(false); // length mismatch
                goto done;
            }

            ret = 1;
        }
        else
        {
            ret = 0;
        }
    }

done:
    if (ctx != NULL) EVP_PKEY_CTX_free(ctx);
    return ret;
#else
    (void)pkey;
    (void)extraHandle;
    (void)msg;
    (void)msgLen;
    (void)context;
    (void)contextLen;
    (void)destination;
    (void)destinationLen;
    return -1;
#endif
}

int32_t CryptoNative_EvpPKeyVerifyPure(EVP_PKEY *pkey,
                                       void* extraHandle,
                                       uint8_t* msg, int32_t msgLen,
                                       uint8_t* context, int32_t contextLen,
                                       uint8_t* sig, int32_t sigLen)
{
    assert(pkey);
    assert(msgLen >= 0);
    assert(sig);
    assert(contextLen >= 0);

#if defined(NEED_OPENSSL_3_0) && HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return -1;
    }

    ERR_clear_error();

    EVP_PKEY_CTX* ctx = NULL;

    int ret = -1;

    ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);
    if (!ctx)
    {
        goto done;
    }

    {
        OSSL_PARAM contextParams[] =
        {
            OSSL_PARAM_construct_end(),
            OSSL_PARAM_construct_end(),
        };

        if (context)
        {
            contextParams[0] = OSSL_PARAM_construct_octet_string(OSSL_SIGNATURE_PARAM_CONTEXT_STRING, (void*)context, Int32ToSizeT(contextLen));
        }

        if (EVP_PKEY_verify_message_init(ctx, NULL, contextParams) <= 0)
        {
            goto done;
        }

        ret = EVP_PKEY_verify(ctx, sig, Int32ToSizeT(sigLen), msg, Int32ToSizeT(msgLen)) == 1;
    }

done:
    if (ctx != NULL) EVP_PKEY_CTX_free(ctx);
    return ret;
#else
    (void)pkey;
    (void)extraHandle;
    (void)msg;
    (void)msgLen;
    (void)context;
    (void)contextLen;
    (void)sig;
    (void)sigLen;
    return -1;
#endif
}

int32_t CryptoNative_EvpPKeySignPreEncoded(EVP_PKEY *pkey,
                                           void* extraHandle,
                                           uint8_t* msg, int32_t msgLen,
                                           uint8_t* destination, int32_t destinationLen)
{

    assert(pkey);
    assert(msgLen >= 0);
    assert(destination);

#if defined(NEED_OPENSSL_3_0) && HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return -1;
    }

    ERR_clear_error();

    EVP_PKEY_CTX* ctx = NULL;

    int ret = -1;

    ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);
    if (!ctx)
    {
        goto done;
    }

    {
        int messageEncoding = 0;
        OSSL_PARAM messageEncodingParams[] =
        {
            OSSL_PARAM_construct_int(OSSL_SIGNATURE_PARAM_MESSAGE_ENCODING, &messageEncoding),
            OSSL_PARAM_construct_end(),
        };

        if (EVP_PKEY_sign_message_init(ctx, NULL, messageEncodingParams) <= 0)
        {
            goto done;
        }

        size_t dstLen = Int32ToSizeT(destinationLen);
        if (EVP_PKEY_sign(ctx, destination, &dstLen, msg, Int32ToSizeT(msgLen)) == 1)
        {
            if (dstLen != Int32ToSizeT(destinationLen))
            {
                assert(false); // length mismatch
                goto done;
            }

            ret = 1;
        }
        else
        {
            ret = 0;
        }
    }

done:
    if (ctx != NULL) EVP_PKEY_CTX_free(ctx);
    return ret;
#else
    (void)pkey;
    (void)extraHandle;
    (void)msg;
    (void)msgLen;
    (void)destination;
    (void)destinationLen;
    return -1;
#endif
}

int32_t CryptoNative_EvpPKeyVerifyPreEncoded(EVP_PKEY *pkey,
                                             void* extraHandle,
                                             uint8_t* msg, int32_t msgLen,
                                             uint8_t* sig, int32_t sigLen)
{
    assert(pkey);
    assert(msgLen >= 0);
    assert(sig);

#if defined(NEED_OPENSSL_3_0) && HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return -1;
    }

    ERR_clear_error();

    EVP_PKEY_CTX* ctx = NULL;

    int ret = -1;

    ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);
    if (!ctx)
    {
        goto done;
    }

    {
        int messageEncoding = 0;
        OSSL_PARAM messageEncodingParams[] =
        {
            OSSL_PARAM_construct_int(OSSL_SIGNATURE_PARAM_MESSAGE_ENCODING, &messageEncoding),
            OSSL_PARAM_construct_end(),
        };

        if (EVP_PKEY_verify_message_init(ctx, NULL, messageEncodingParams) <= 0)
        {
            goto done;
        }

        ret = EVP_PKEY_verify(ctx, sig, Int32ToSizeT(sigLen), msg, Int32ToSizeT(msgLen)) == 1;
    }

done:
    if (ctx != NULL) EVP_PKEY_CTX_free(ctx);
    return ret;
#else
    (void)pkey;
    (void)extraHandle;
    (void)msg;
    (void)msgLen;
    (void)sig;
    (void)sigLen;
    return -1;
#endif
}
