// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Net.Security.Tests;

public class OpenSslErrorTests
{
    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsOpenSsl3))]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ConfigureSignatureAlgorithms_DefaultsAreAccepted(bool enablePss, bool enablePkcs1)
    {
        Interop.CryptoInitializer.Initialize();
        using SafeSslContextHandle context = Interop.Ssl.SslCtxCreate(Interop.Ssl.SslV2_3Method());
        Assert.False(context.IsInvalid);
        using SafeSslHandle ssl = Interop.Ssl.SslCreate(context);
        Assert.False(ssl.IsInvalid);

        Interop.OpenSsl.ConfigureSignatureAlgorithms(ssl, enablePss, enablePkcs1);
    }
}
