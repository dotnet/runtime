// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

int32_t CryptoNative_EvpPKeySignPure(EVP_PKEY *pkey,
                                     void* extraHandle,
                                     uint8_t* msg, int32_t msgLen,
                                     uint8_t* context, int32_t contextLen,
                                     uint8_t* destination, int32_t destinationLen);

int32_t CryptoNative_EvpPKeyVerifyPure(EVP_PKEY *pkey,
                                       void* extraHandle,
                                       uint8_t* msg, int32_t msgLen,
                                       uint8_t* context, int32_t contextLen,
                                       uint8_t* sig, int32_t sigLen);

int32_t CryptoNative_EvpPKeySignPreEncoded(EVP_PKEY *pkey,
                                           void* extraHandle,
                                           uint8_t* msg, int32_t msgLen,
                                           uint8_t* destination, int32_t destinationLen);

int32_t CryptoNative_EvpPKeyVerifyPreEncoded(EVP_PKEY *pkey,
                                             void* extraHandle,
                                             uint8_t* msg, int32_t msgLen,
                                             uint8_t* sig, int32_t sigLen);
