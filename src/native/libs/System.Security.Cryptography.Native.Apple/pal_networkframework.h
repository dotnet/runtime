// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include <pal_ssl_types.h>
#include <stdint.h>
#include <stddef.h>
#include <sys/types.h>  // for intptr_t

#ifdef __OBJC__
#import <Network/Network.h>
#else
#include <Network/Network.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Status update enumeration for TLS operations
typedef enum
{
    PAL_NwStatusUpdates_UnknownError = 0,
    PAL_NwStatusUpdates_FramerStart = 1,
    PAL_NwStatusUpdates_FramerStop = 2,
    PAL_NwStatusUpdates_HandshakeFinished = 3,
    PAL_NwStatusUpdates_HandshakeFailed = 4,

    PAL_NwStatusUpdates_ConnectionReadFinished = 100,
    PAL_NwStatusUpdates_ConnectionWriteFinished = 101,
    PAL_NwStatusUpdates_ConnectionWriteFailed = 102,
    PAL_NwStatusUpdates_ConnectionCancelled = 103,
} PAL_NwStatusUpdates;

// Callback type definitions that match the implementation usage
typedef void (*StatusUpdateCallback)(size_t context, PAL_NwStatusUpdates status, size_t data1, size_t data2);
typedef int32_t (*WriteCallback)(void* context, uint8_t* buffer, size_t length);

// Only TLS-specific Network Framework functions are exported
PALEXPORT nw_connection_t AppleCryptoNative_NwCreateContext(int32_t isServer);
PALEXPORT int32_t AppleCryptoNative_NwStartTlsHandshake(nw_connection_t connection, size_t gcHandle);
PALEXPORT int32_t AppleCryptoNative_NwInit(StatusUpdateCallback statusFunc, WriteCallback writeFunc);
PALEXPORT void AppleCryptoNative_NwSendToConnection(nw_connection_t connection, size_t gcHandle, uint8_t* buffer, int length);
PALEXPORT void AppleCryptoNative_NwReadFromConnection(nw_connection_t connection, size_t gcHandle);
PALEXPORT int32_t AppleCryptoNative_NwProcessInputData(nw_connection_t connection, nw_framer_t framer, const uint8_t * data, int dataLength);
PALEXPORT void AppleCryptoNative_NwSetTlsOptions(nw_connection_t connection, size_t gcHandle, char* targetName, const uint8_t* alpnBuffer, int alpnLength, PAL_SslProtocol minTlsProtocol, PAL_SslProtocol maxTlsProtocol);
PALEXPORT int32_t AppleCryptoNative_NwGetConnectionInfo(nw_connection_t connection, PAL_SslProtocol* pProtocol, uint16_t* pCipherSuiteOut, const char** negotiatedAlpn, int32_t* negotiatedAlpnLength);
PALEXPORT void AppleCryptoNative_NwCopyCertChain(nw_connection_t connection, CFArrayRef* certificates, int* count);
PALEXPORT void AppleCryptoNative_NwCancelConnection(nw_connection_t connection);

#ifdef __cplusplus
}
#endif
