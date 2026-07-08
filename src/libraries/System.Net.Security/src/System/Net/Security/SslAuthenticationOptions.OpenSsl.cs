// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Net.Sockets;

namespace System.Net.Security
{
    internal sealed partial class SslAuthenticationOptions
    {
        // Pre-allocated SSL_CTX owned by a TlsContext and shared by every TlsSession
        // it produces. When set, Interop.OpenSsl.AllocateSslHandle uses this handle
        // directly and bypasses the global SslContextCacheKey lookup. Unset for the
        // legacy SslStream path, which keeps using the static cache.
        //
        // Lifetime: owned by TlsContext (not this options bag); set in TlsContext's
        // CreateSessionOptions and read by AllocateSslHandle. Not copied by Clone()
        // \u2014 each per-session clone gets the field re-stamped by CreateSessionOptions.
        internal SafeSslContextHandle? PreallocatedSslContext { get; set; }

        // Socket handle to bind to the SSL object via SSL_set_fd. When set,
        // SafeSslHandle.Create skips the ManagedSpanBio installation and OpenSSL
        // reads/writes the socket directly. Used by TlsSession's socket-bound mode
        // (Create(TlsContext, SafeSocketHandle)).
        internal SafeSocketHandle? SocketHandle { get; set; }

        // ClientHello bytes already consumed from SocketHandle by the managed
        // pre-fetch loop used to surface SNI to a deferred ServerOptionsSelectionCallback.
        // When both SocketHandle and this are set, SafeSslHandle.Create installs a
        // socket-replay BIO on the SSL* instead of SSL_set_fd so those bytes are
        // fed back to OpenSSL's handshake state machine before further recv().
        // Only meaningful when SocketHandle is also set; cleared once transferred
        // to the BIO (the BIO holds its own copy).
        internal byte[]? ReplayPrefix { get; set; }

        // Preferred over ReplayPrefix: a socket-replay BIO already bound to the
        // fd and pre-populated (via BioReadTlsFrame) with the ClientHello record.
        // SafeSslHandle.Create adopts it as the SSL's read BIO — no separate
        // managed pre-fetch buffer, no byte[] copy, no second BioNewSocketReplay
        // allocation. Ownership transfers to the SSL* at Create time; the field
        // is cleared afterwards.
        internal SafeBioHandle? PreallocatedReadBio { get; set; }
    }
}
