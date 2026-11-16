// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Security.Tests;

public class OpenSslErrorTests
{
    [Theory]
    // OpenSSL 1.x: ERR_PACK(ERR_LIB_SSL, SSL_F_SSL_VERIFY_CLIENT_POST_HANDSHAKE, SSL_R_EXTENSION_NOT_RECEIVED)
    [InlineData(0x14268117u, false, 279)]
    // OpenSSL 1.x: ERR_PACK(ERR_LIB_SSL, SSL_F_SSL_RENEGOTIATE, SSL_R_NO_RENEGOTIATION)
    [InlineData(0x14204153u, false, 339)]
    // OpenSSL 3: ERR_PACK(ERR_LIB_SSL, 0, SSL_R_EXTENSION_NOT_RECEIVED) and SSL_R_NO_RENEGOTIATION
    [InlineData(0x0A000117u, true, 279)]
    [InlineData(0x0A000153u, true, 339)]
    // Each layout read as the other
    [InlineData(0x14268117u, true, -1)]
    [InlineData(0x0A000117u, false, -1)]
    // Reason 279 from ERR_LIB_EVP, in each layout
    [InlineData(0x06000117u, false, -1)]
    [InlineData(0x03000117u, true, -1)]
    // OpenSSL 3 system error, errno 279
    [InlineData(0x80000117u, true, -1)]
    public void GetSslLibraryReason_UsesLayoutOfLoadedOpenSsl(uint error, bool isOpenSsl3, int expected)
    {
        Assert.Equal(expected, Interop.OpenSsl.GetSslLibraryReason(error, isOpenSsl3));
    }
}
