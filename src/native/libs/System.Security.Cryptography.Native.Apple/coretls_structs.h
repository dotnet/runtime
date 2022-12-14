// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Definitions of structures from coreTLS
// https://github.com/apple-oss-distributions/coreTLS/

typedef struct
{   size_t  length;
    uint8_t *data;
} tls_buffer;

struct _tls_handshake_s
{
    uint8_t             _ignore0[968];
    /* ALPN */
    bool                alpn_enabled;    /* Client: alpn is enabled */
    bool                alpn_announced;  /* Client: alpn extension was sent, Server: alpn extension was received */
    bool                alpn_confirmed;  /* Client: alpn extension was received, Server: alpn extension was sent */
    bool                alpn_received;   /* Server: alpn message was received */
    tls_buffer          alpnOwnData;     /* Client: supported protocols sent, Server: selected protocol sent */
    tls_buffer          alpnPeerData;    /* Client: select protocol received, Server: list of supported protocol received */
};

typedef struct _tls_handshake_s *tls_handshake_t;

struct SSLContext
{
    uint8_t             _ignored0[56];
    tls_handshake_t     hdsk;
};

