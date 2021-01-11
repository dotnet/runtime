// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey.h"

EVP_PKEY* CryptoNative_EvpPkeyCreate()
{
    return EVP_PKEY_new();
}

void CryptoNative_EvpPkeyDestroy(EVP_PKEY* pkey)
{
    if (pkey != NULL)
    {
        EVP_PKEY_free(pkey);
    }
}

int32_t CryptoNative_UpRefEvpPkey(EVP_PKEY* pkey)
{
    if (pkey == NULL)
    {
        return 0;
    }

    return EVP_PKEY_up_ref(pkey);
}

int32_t CryptoNative_EvpPKeyKeySize(EVP_PKEY* pkey)
{
    if (pkey == NULL)
    {
        return -1;
    }

    return EVP_PKEY_bits(pkey);
}

int32_t CryptoNative_EvpPkeyDuplicate(EVP_PKEY* pkeyIn, EVP_PKEY** pkeyOut)
{
    if (pkeyOut != NULL)
    {
        *pkeyOut = NULL;
    }

    if (pkeyIn == NULL || pkeyOut == NULL)
    {
        return -1;
    }

    EVP_PKEY* pkey = EVP_PKEY_new();

    if (pkey == NULL)
    {
        return 0;
    }

    int ret = 0;

    switch (EVP_PKEY_base_id(pkeyIn))
    {
        case NID_rsaEncryption:
        {
            RSA* rsa = EVP_PKEY_get0_RSA(pkeyIn);

            if (rsa != NULL && EVP_PKEY_set1_RSA(pkey, rsa) == 1)
            {
                ret = 1;
            }

            break;
        }
        default:
            ERR_PUT_error(
                ERR_LIB_EVP,
                EVP_F_EVP_PKEY_GET_RAW_PUBLIC_KEY,
                EVP_R_OPERATION_NOT_SUPPORTED_FOR_THIS_KEYTYPE,
                __FILE__,
                __LINE__);

            break;
    }

    if (ret == 1)
    {
        *pkeyOut = pkey;
    }
    else
    {
        EVP_PKEY_free(pkey);
    }

    return ret;
}
