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

// Matches managed PAL_SSLStreamStatus enum
enum
{
    SSLStreamStatus_OK = 0,
    SSLStreamStatus_NeedData = 1,
    SSLStreamStatus_Error = 2
};
typedef int32_t PAL_SSLStreamStatus;

PALEXPORT SSLStream* AndroidCryptoNative_SSLStreamCreate(bool isServer, STREAM_READER streamReader, STREAM_WRITER streamWriter, int appOutBufferSize, int appInBufferSize);
PALEXPORT int32_t AndroidCryptoNative_SSLStreamConfigureParameters(SSLStream *sslStream, char* targetHost);
PALEXPORT PAL_SSLStreamStatus AndroidCryptoNative_SSLStreamHandshake(SSLStream *sslStream);

PALEXPORT SSLStream* AndroidCryptoNative_SSLStreamCreateAndStartHandshake(STREAM_READER streamReader, STREAM_WRITER streamWriter, int tlsVersion, int appOutBufferSize, int appInBufferSize);
PALEXPORT int  AndroidCryptoNative_SSLStreamRead(SSLStream* sslStream, uint8_t* buffer, int offset, int length);
PALEXPORT void AndroidCryptoNative_SSLStreamWrite(SSLStream* sslStream, uint8_t* buffer, int offset, int length);
PALEXPORT void AndroidCryptoNative_SSLStreamRelease(SSLStream* sslStream);

PALEXPORT int32_t AndroidCryptoNative_SSLStreamGetApplicationProtocol(SSLStream* sslStream, uint8_t* out, int* outLen);
PALEXPORT int32_t AndroidCryptoNative_SSLStreamGetCipherSuite(SSLStream *sslStream, uint16_t** out);
PALEXPORT int32_t AndroidCryptoNative_SSLStreamGetProtocol(SSLStream *sslStream, uint16_t** out);
