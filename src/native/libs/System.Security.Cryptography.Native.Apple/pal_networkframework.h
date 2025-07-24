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
    PAL_NwStatusUpdates_HandshakeFinished = 3,
    PAL_NwStatusUpdates_ConnectionFailed = 4,
    PAL_NwStatusUpdates_ConnectionCancelled = 103,
    PAL_NwStatusUpdates_CertificateAvailable = 104,

    PAL_NwStatusUpdates_DebugLog = 200,
} PAL_NwStatusUpdates;

// Error information structure
typedef struct
{
    int32_t errorCode;
    int32_t errorDomain;
    const char* errorMessage;
} PAL_NetworkFrameworkError;

// Callback type definitions that match the implementation usage
typedef void (*StatusUpdateCallback)(void* context, PAL_NwStatusUpdates status, size_t data1, size_t data2, PAL_NetworkFrameworkError* error);
typedef int32_t (*WriteCallback)(void* context, uint8_t* buffer, uint64_t length);
typedef void (*CompletionCallback)(void* context, PAL_NetworkFrameworkError* error);
typedef void (*ReadCompletionCallback)(void* context, PAL_NetworkFrameworkError* error, const uint8_t* data, size_t length);
typedef void* (*ChallengeCallback)(void* context, CFArrayRef acceptableIssuers);

// Initializes global state
PALEXPORT int32_t AppleCryptoNative_Init(StatusUpdateCallback statusFunc, WriteCallback writeFunc, ChallengeCallback challengeFunc);

PALEXPORT nw_connection_t AppleCryptoNative_NwConnectionCreate(int32_t isServer, void* context, char* targetName, const uint8_t * alpnBuffer, int alpnLength, PAL_SslProtocol minTlsProtocol, PAL_SslProtocol maxTlsProtocol, uint32_t* cipherSuites, int cipherSuitesLength);
PALEXPORT int32_t AppleCryptoNative_NwConnectionStart(nw_connection_t connection, void* context);
PALEXPORT void AppleCryptoNative_NwConnectionSend(nw_connection_t connection, void* context, uint8_t* buffer, int length, CompletionCallback completionCallback);
PALEXPORT void AppleCryptoNative_NwConnectionReceive(nw_connection_t connection, void* context, uint32_t length, ReadCompletionCallback readCompletionCallback);
PALEXPORT void AppleCryptoNative_NwConnectionCancel(nw_connection_t connection);

PALEXPORT int32_t AppleCryptoNative_NwFramerDeliverInput(nw_framer_t framer, void* context, const uint8_t* data, int dataLength, CompletionCallback completionCallback);

PALEXPORT int32_t AppleCryptoNative_GetConnectionInfo(nw_connection_t connection, void* context, PAL_SslProtocol* pProtocol, uint16_t* pCipherSuiteOut, char* negotiatedAlpn, int32_t* negotiatedAlpnLength);

#ifdef __cplusplus
}
#endif
