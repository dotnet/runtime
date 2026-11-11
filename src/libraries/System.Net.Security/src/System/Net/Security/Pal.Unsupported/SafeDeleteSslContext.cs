// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Security
{
    // Placeholder security context for platforms without a TLS implementation.
    // It is never instantiated because SslStreamPal throws before a context is created.
    internal sealed class SafeDeleteSslContext : SafeDeleteContext
    {
        public SafeDeleteSslContext() : base(IntPtr.Zero)
        {
        }
    }
}
