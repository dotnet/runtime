// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"
#include "pal_x509.h"

typedef void (*STREAM_WRITER)(uint8_t*, int32_t);
typedef int (*STREAM_READER)(uint8_t*, int32_t*);

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
    SSLStreamStatus_Error = 2,
    SSLStreamStatus_Renegotiate = 3,
    SSLStreamStatus_Closed = 4,
};
typedef int32_t PAL_SSLStreamStatus;

/*
Create an SSL context

Returns NULL on failure
*/
PALEXPORT SSLStream* AndroidCryptoNative_SSLStreamCreate(void);

/*
Create an SSL context with the specified certificates

Returns NULL on failure
*/
PALEXPORT SSLStream* AndroidCryptoNative_SSLStreamCreateWithCertificates(uint8_t* pkcs8PrivateKey, int32_t pkcs8PrivateKeyLen, PAL_KeyAlgorithm algorithm, jobject* /*X509Certificate[]*/ certs, int32_t certsLen);

/*
Initialize an SSL context
  - isServer      : true if the context should be created in server mode
  - streamReader  : callback for reading data from the connection
  - streamWriter  : callback for writing data to the connection
  - appBufferSize : initial buffer size for applicaiton data

Returns 1 on success, 0 otherwise
*/
PALEXPORT int32_t AndroidCryptoNative_SSLStreamInitialize(SSLStream* sslStream,
                                                             bool isServer,
                                                             STREAM_READER streamReader,
                                                             STREAM_WRITER streamWriter,
                                                             int appBufferSize);

/*
Set configuration parameters
  - targetHost : SNI host name

Returns 1 on success, 0 otherwise
*/
PALEXPORT int32_t AndroidCryptoNative_SSLStreamConfigureParameters(SSLStream* sslStream, char* targetHost);

/*
Start or continue the TLS handshake
*/
PALEXPORT PAL_SSLStreamStatus AndroidCryptoNative_SSLStreamHandshake(SSLStream* sslStream);

/*
Read bytes from the connection into a buffer
  - buffer : buffer to populate with the bytes read from the connection
  - length : maximum number of bytes to read
  - read   : [out] number of bytes read from the connection and written into the buffer

Unless data from a previous incomplete read is present, this will invoke the STREAM_READER callback.
*/
PALEXPORT PAL_SSLStreamStatus AndroidCryptoNative_SSLStreamRead(SSLStream* sslStream,
                                                                uint8_t* buffer,
                                                                int length,
                                                                int* read);
/*
Encodes bytes from a buffer
  - buffer : data to encode
  - length : length of buffer

This will invoke the STREAM_WRITER callback with the processed data.
*/
PALEXPORT PAL_SSLStreamStatus AndroidCryptoNative_SSLStreamWrite(SSLStream* sslStream, uint8_t* buffer, int length);

/*
Release the SSL context
*/
PALEXPORT void AndroidCryptoNative_SSLStreamRelease(SSLStream* sslStream);

/*
Get the negotiated application protocol for the current session

Returns 1 on success, 0 otherwise
*/
PALEXPORT int32_t AndroidCryptoNative_SSLStreamGetApplicationProtocol(SSLStream* sslStream, uint8_t* out, int* outLen);

/*
Get the name of the cipher suite for the current session

Returns 1 on success, 0 otherwise
*/
PALEXPORT int32_t AndroidCryptoNative_SSLStreamGetCipherSuite(SSLStream* sslStream, uint16_t** out);

/*
Get the standard name of the protocol for the current session (e.g. TLSv1.2)

Returns 1 on success, 0 otherwise
*/
PALEXPORT int32_t AndroidCryptoNative_SSLStreamGetProtocol(SSLStream* sslStream, uint16_t** out);

/*
Get the peer certificate for the current session

Returns 1 on success, 0 otherwise
*/
PALEXPORT int32_t AndroidCryptoNative_SSLStreamGetPeerCertificate(SSLStream* sslStream,
                                                                  jobject* /*X509Certificate*/ out);

/*
Get the peer certificates for the current session

The peer's own certificate will be first, followed by any certificate authorities.

Returns 1 on success, 0 otherwise
*/
PALEXPORT int32_t AndroidCryptoNative_SSLStreamGetPeerCertificates(SSLStream* sslStream,
                                                                   jobject** /*X509Certificate[]*/ out,
                                                                   int* outLen);

/*
Verify hostname using the peer certificate for the current session

Returns true if hostname matches, false otherwise
*/
PALEXPORT bool AndroidCryptoNative_SSLStreamVerifyHostname(SSLStream* sslStream, char* hostname);

/*
Shut down the session
*/
PALEXPORT bool AndroidCryptoNative_SSLStreamShutdown(SSLStream* sslStream);
