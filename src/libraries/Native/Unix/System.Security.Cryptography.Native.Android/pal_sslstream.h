// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

typedef void (*STREAM_WRITER)(uint8_t*, uint32_t, uint32_t);
typedef int  (*STREAM_READER)(uint8_t*, uint32_t, uint32_t);

typedef struct SSLStream
{
    jobject sslContext;
    jobject sslEngine;
    jobject sslSession;
    jobject appOutBuffer;
    jobject netOutBuffer;
    jobject appInBuffer;
    jobject netInBuffer;
    STREAM_READER streamReader;
    STREAM_WRITER streamWriter;
} SSLStream;

#define TLS11 11
#define TLS12 12
#define TLS13 13

// javax/net/ssl/SSLEngineResult$HandshakeStatus
#define HANDSHAKE_STATUS__NOT_HANDSHAKING 0
#define HANDSHAKE_STATUS__FINISHED 1
#define HANDSHAKE_STATUS__NEED_TASK 2
#define HANDSHAKE_STATUS__NEED_WRAP 3
#define HANDSHAKE_STATUS__NEED_UNWRAP 4

// javax/net/ssl/SSLEngineResult$Status
#define STATUS__BUFFER_UNDERFLOW 0
#define STATUS__BUFFER_OVERFLOW 1
#define STATUS__OK 2
#define STATUS__CLOSED 3

PALEXPORT SSLStream* AndroidCrypto_CreateSSLStreamAndStartHandshake(STREAM_READER streamReader, STREAM_WRITER streamWriter, int tlsVersion, int appOutBufferSize, int appInBufferSize);
PALEXPORT int  AndroidCrypto_SSLStreamRead(SSLStream* sslStream, uint8_t* buffer, int offset, int length);
PALEXPORT void AndroidCrypto_SSLStreamWrite(SSLStream* sslStream, uint8_t* buffer, int offset, int length);
PALEXPORT void AndroidCrypto_Dispose(SSLStream* sslStream);
