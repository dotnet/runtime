// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_compiler.h"
#include <stdint.h>
#include "opensslshim.h"

/*
Shims the EVP_MAC_fetch function.

algorithm: The name of the algorithm to fetch.
haveFeature: A pointer to an int32_t. When this function returns, the value will
             contain an integer to determine if the platform supports EVP_MAC_fetch.
             0 indicates that the platform does not support EVP_MAC_fetch or the algorithm.
             1 indicates that the platform does support EVP_MAC_fetch and the algorithm.

return: A pointer to an EVP_MAC. This pointer may be NULL if OpenSSL failed to allocate internally,
        or, if the platform does not support EVP_MAC_fetch or the algorithm.
        Use the haveFeature value to determine if the NULL value is due to allocation failure
        or lack of platform support.
*/
PALEXPORT EVP_MAC* CryptoNative_EvpMacFetch(const char* algorithm, int32_t* haveFeature);

/*
Shims the EVP_MAC_free function.

mac: The MAC to free.
note: This method will assert that the platform has EVP_MAC_free. Callers are
      responsible for ensuring the platform supports EVP_MAC_free.
*/
PALEXPORT void CryptoNative_EvpMacFree(EVP_MAC *mac);

/*
Shims the EVP_MAC_CTX_new function.

mac: The MAC algorithm to create a new context from.
return: A pointer to a new EVP_MAC_CTX, or NULL if the operation failed.
note: This method will assert that the platform has EVP_MAC_CTX_new.
*/
PALEXPORT EVP_MAC_CTX* CryptoNative_EvpMacCtxNew(EVP_MAC* mac);

/*
Shims the EVP_MAC_CTX_free function.

ctx: The context to free.
note: This method will assert that the platform has EVP_MAC_CTX_free
*/
PALEXPORT void CryptoNative_EvpMacCtxFree(EVP_MAC_CTX* ctx);

/*
Initializes an EVP_MAC_CTX using EVP_MAC_init and sets properties.

ctx: A pointer to the context to initialize and configure.
key: A pointer to a key. This value is set using OSSL_MAC_PARAM_KEY. This value
     may be NULL if the keyLength parameter is 0.
keyLength: The length of the key in the key parameter. This value must be zero or positive.
customizationString: A pointer to a customization string. This value is set using
                     OSSL_MAC_PARAM_CUSTOM. If this value is NULL, the parameter
                     is not set. This value may only be NULL if the
                     customizationStringLength parameter is 0.
customizationStringLength: The length of the customization string in the customizationString
                           parameter. This value must be zero or positive.
xof: A value to indicate if the MAC should be placed in XOF mode. 0 is false, 1 is true.

return: A value indicating the status of the operation.
    1:  The operation succeeded.
    0:  The operation failed, indicating the OpenSSL error queue contains the failure.
    -1: A parameter contained an invalid value. Callers are expected to supply valid values.
    -2: The platform does not support EVP_MAC_init. Callers are expected to validate
        the platform supports the required APIs. This value is only returned in release builds.
        Debug builds will assert.
*/
PALEXPORT int32_t CryptoNative_EvpMacInit(EVP_MAC_CTX* ctx,
                                          uint8_t* key,
                                          int32_t keyLength,
                                          uint8_t* customizationString,
                                          int32_t customizationStringLength,
                                          int32_t xof);

/*
Appends data to an EVP_MAC_CTX using EVP_MAC_update.

ctx: A pointer to the context to append data.
data: A pointer to the data to append. This may be NULL if dataLength is zero.
dataLength: The length of the data pointed to by the data parameter. This value must be zero or greater.

return: A value indicating the status of the operation.
    1:  The operation succeeded.
    0:  The operation failed, indicating the OpenSSL error queue contains the failure.
    -1: A parameter contained an invalid value. Callers are expected to supply valid values.
    -2: The platform does not support EVP_MAC_update. Callers are expected to validate
        the platform supports the required APIs. This value is only returned in release builds.
        Debug builds will assert.
*/
PALEXPORT int32_t CryptoNative_EvpMacUpdate(EVP_MAC_CTX* ctx, uint8_t* data, int32_t dataLength);

/*
Computes the final MAC value in to the mac buffer using EVP_MAC_final.

ctx: A pointer to the context to compute the final MAC.
mac: The buffer to receive the MAC.
macLength: The length of the MAC to compute and write to the mac buffer.
return: A value indicating the status of the operation.
    1:  The operation succeeded.
    0:  The operation failed, indicating the OpenSSL error queue contains the failure.
    -1: A parameter contained an invalid value. Callers are expected to supply valid values.
    -2: The platform does not support EVP_MAC_final. Callers are expected to validate
        the platform supports the required APIs. This value is only returned in release builds.
        Debug builds will assert.
    -3: The OpenSSL operation indicated success but wrote the incorrect amount of data.
*/
PALEXPORT int32_t CryptoNative_EvpMacFinal(EVP_MAC_CTX* ctx, uint8_t* mac, int32_t macLength);

/*
Resets the MAC context using EVP_MAC_init.

ctx: A pointer to the context to reset.
return: A value indicating the status of the operation.
    1:  The operation succeeded.
    0:  The operation failed, indicating the OpenSSL error queue contains the failure.
    -1: A parameter contained an invalid value. Callers are expected to supply valid values.
    -2: The platform does not support EVP_MAC_init. Callers are expected to validate
        the platform supports the required APIs. This value is only returned in release builds.
        Debug builds will assert.

remarks: Previously configured keys and customization string values are not cleared.
*/
PALEXPORT int32_t CryptoNative_EvpMacReset(EVP_MAC_CTX* ctx);

/*
Gets the current MAC in a context without resetting.

ctx: A pointer to the context to get the MAC for.
mac: The buffer to receive the MAC.
macLength: The length of the MAC to compute and write to the mac buffer.
return: A value indicating the status of the operation.
    1:  The operation succeeded.
    0:  The operation failed, indicating the OpenSSL error queue contains the failure.
    -1: A parameter contained an invalid value. Callers are expected to supply valid values.
    -2: The platform does not support EVP_MAC_final. Callers are expected to validate
        the platform supports the required APIs. This value is only returned in release builds.
        Debug builds will assert.
    -3: The OpenSSL operation indicated success but wrote the incorrect amount of data.

remarks: This uses EVP_MAC_CTX_dup and calls CryptoNative_EvpMacFinal on the duplicate,
         finally calling EVP_MAC_CTX_free on the duplicated context.
*/
PALEXPORT int32_t CryptoNative_EvpMacCurrent(EVP_MAC_CTX* ctx, uint8_t* mac, int32_t macLength);

/*
Computes the MAC of data with a key and customization string in a single step.

mac: The MAC algorithm to use to compute the MAC.
key: A pointer to a key. This value is set using OSSL_MAC_PARAM_KEY. This value
     may be NULL if the keyLength parameter is 0.
keyLength: The length of the key in the key parameter. This value must be zero or positive.
customizationString: A pointer to a customization string. This value is set using
                     OSSL_MAC_PARAM_CUSTOM. If this value is NULL, the parameter
                     is not set. This value may only be NULL if the
                     customizationStringLength parameter is 0.
customizationStringLength: The length of the customization string in the customizationString
                           parameter. This value must be zero or positive.
data: A pointer to the data to append. This may be NULL if dataLength is zero.
dataLength: The length of the data pointed to by the data parameter. This value must be zero or greater.
destination: The buffer to receive the MAC.
destinationLength: The length of the MAC to compute and write to the mac buffer.
xof: A value to indicate if the MAC should be placed in XOF mode. 0 is false, 1 is true.

return: A value indicating the status of the operation.
    1:  The operation succeeded.
    0:  The operation failed, indicating the OpenSSL error queue contains the failure.
    -1: A parameter contained an invalid value. Callers are expected to supply valid values.
    -2: The platform does not support one of the required MAC APIs. Callers are expected to validate
        the platform supports the required APIs. This value is only returned in release builds.
        Debug builds will assert.
    -3: The OpenSSL operation indicated success but wrote the incorrect amount of data.
*/
PALEXPORT int32_t CryptoNative_EvpMacOneShot(EVP_MAC* mac,
                                             uint8_t* key,
                                             int32_t keyLength,
                                             uint8_t* customizationString,
                                             int32_t customizationStringLength,
                                             const uint8_t* data,
                                             int32_t dataLength,
                                             uint8_t* destination,
                                             int32_t destinationLength,
                                             int32_t xof);
