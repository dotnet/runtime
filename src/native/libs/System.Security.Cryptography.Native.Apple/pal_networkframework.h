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
typedef void (*StatusUpdateCallback)(size_t context, PAL_NwStatusUpdates status, size_t data1, size_t data2, PAL_NetworkFrameworkError* error);
typedef int32_t (*WriteCallback)(void* context, uint8_t* buffer, void** length);
typedef void (*CompletionCallback)(void* context, PAL_NetworkFrameworkError* error);
typedef void (*ReadCompletionCallback)(void* context, PAL_NetworkFrameworkError* error, const uint8_t* buffer, size_t length);
typedef void* (*ChallengeCallback)(size_t context, CFArrayRef acceptableIssuers, SecCertificateRef remoteCertificate);

// Only TLS-specific Network Framework functions are exported
PALEXPORT nw_connection_t AppleCryptoNative_NwCreateContext(int32_t isServer);
PALEXPORT int32_t AppleCryptoNative_NwStartTlsHandshake(nw_connection_t connection, size_t state);
PALEXPORT int32_t AppleCryptoNative_NwInit(StatusUpdateCallback statusFunc, WriteCallback writeFunc, ChallengeCallback challengeFunc);
PALEXPORT void AppleCryptoNative_NwSendToConnection(nw_connection_t connection, size_t state, uint8_t* buffer, int length, void* context, CompletionCallback completionCallback);
PALEXPORT void AppleCryptoNative_NwReadFromConnection(nw_connection_t connection, size_t state, uint32_t length, void* context, ReadCompletionCallback readCompletionCallback);
PALEXPORT int32_t AppleCryptoNative_NwProcessInputData(nw_connection_t connection, nw_framer_t framer, const uint8_t * data, int dataLength, void* context, CompletionCallback completionCallback);
PALEXPORT void AppleCryptoNative_NwSetTlsOptions(nw_connection_t connection, size_t state, char* targetName, const uint8_t* alpnBuffer, int alpnLength, PAL_SslProtocol minTlsProtocol, PAL_SslProtocol maxTlsProtocol, uint32_t* cipherSuites, int cipherSuitesLength);
PALEXPORT int32_t AppleCryptoNative_NwGetConnectionInfo(nw_connection_t connection, PAL_SslProtocol* pProtocol, uint16_t* pCipherSuiteOut, char* negotiatedAlpn, int32_t* negotiatedAlpnLength);
PALEXPORT void AppleCryptoNative_NwCopyCertChain(nw_connection_t connection, CFArrayRef* certificates, int* count);
PALEXPORT void AppleCryptoNative_NwCancelConnection(nw_connection_t connection);

#ifdef __cplusplus
}
#endif
