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

// Handshake state enumeration matching other Apple SSL implementations
typedef enum
{
    PAL_TlsHandshakeState_Unknown = 0,
    PAL_TlsHandshakeState_Complete = 1,
    PAL_TlsHandshakeState_WouldBlock = 2,
    PAL_TlsHandshakeState_ServerAuthCompleted = 3,
    PAL_TlsHandshakeState_ClientAuthCompleted = 4,
    PAL_TlsHandshakeState_ClientCertRequested = 5,
    PAL_TlsHandshakeState_ClientHelloReceived = 6,
} PAL_TlsHandshakeState;

// Status update enumeration for TLS operations
typedef enum
{
    PAL_NwStatusUpdates_UnknownError = 0,
    PAL_NwStatusUpdates_FramerStart = 1,
    PAL_NwStatusUpdates_HandshakeFinished = 2,
    PAL_NwStatusUpdates_HandshakeFailed = 3,
    PAL_NwStatusUpdates_ConnectionReadFinished = 4,
    PAL_NwStatusUpdates_ConnectionWriteFinished = 5,
    PAL_NwStatusUpdates_ConnectionWriteFailed = 6,
    PAL_NwStatusUpdates_ConnectionError = 7,
    PAL_NwStatusUpdates_ConnectionCancelled = 8,
} PAL_NwStatusUpdates;

// Callback type definitions that match the implementation usage
typedef void (*StatusUpdateCallback)(size_t context, PAL_NwStatusUpdates status, size_t data1, size_t data2);
typedef int32_t (*ReadCallback)(void* context, uint8_t* buffer, size_t* length);
typedef int32_t (*WriteCallback)(void* context, uint8_t* buffer, size_t length);

// Only TLS-specific Network Framework functions are exported
PALEXPORT nw_connection_t AppleNetNative_NwCreateContext(int32_t isServer);
PALEXPORT int32_t AppleNetNative_NwStartTlsHandshake(nw_connection_t connection, size_t gcHandle);
PALEXPORT int32_t AppleNetNative_NwInit(StatusUpdateCallback statusFunc, ReadCallback readFunc, WriteCallback writeFunc);
PALEXPORT int32_t AppleNetNative_NwSendToConnection(nw_connection_t connection, size_t gcHandle, uint8_t* buffer, int length);
PALEXPORT int32_t AppleNetNative_NwReadFromConnection(nw_connection_t connection, size_t gcHandle);
PALEXPORT int32_t AppleNetNative_NwProcessInputData(nw_connection_t connection, nw_framer_t framer, const uint8_t * data, int dataLength);
PALEXPORT int32_t AppleNetNative_NwSetTlsOptions(nw_connection_t connection, size_t gcHandle, char* targetName, const uint8_t* alpnBuffer, int alpnLength, PAL_SslProtocol minTlsProtocol, PAL_SslProtocol maxTlsProtocol);
PALEXPORT int32_t AppleNetNative_NwGetConnectionInfo(nw_connection_t connection, PAL_SslProtocol* pProtocol, uint16_t* pCipherSuiteOut, const char** negotiatedAlpn, uint32_t* alpnLength);
PALEXPORT int32_t AppleNetNative_NwCopyCertChain(nw_connection_t connection, CFArrayRef* certificates, int* count);
PALEXPORT int32_t AppleNetNative_NwCancelConnection(nw_connection_t connection);

#ifdef __cplusplus
}
#endif
