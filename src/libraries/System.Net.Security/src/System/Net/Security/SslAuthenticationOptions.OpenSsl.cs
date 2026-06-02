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
    }
}
