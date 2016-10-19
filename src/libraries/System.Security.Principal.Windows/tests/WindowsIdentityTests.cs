// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization.Formatters.Tests;
using System.Security.Principal;
using Xunit;

public class WindowsIdentityTests
{
    [Fact]
    public static void GetAnonymousUserTest()
    {
        WindowsIdentity windowsIdentity = WindowsIdentity.GetAnonymous();
        Assert.NotNull(windowsIdentity);
        Assert.True(windowsIdentity.IsAnonymous);
        Assert.False(windowsIdentity.IsAuthenticated);
        CheckDispose(windowsIdentity, true);        
    }

    [Fact]
    public static void ConstructorsAndProperties()
    {
        // Retrieve the Windows account token for the current user.
        IntPtr logonToken = WindowsIdentity.GetCurrent().AccessToken.DangerousGetHandle();

        // Construct a WindowsIdentity object using the input account token.
        WindowsIdentity windowsIdentity = new WindowsIdentity(logonToken);
        Assert.NotNull(windowsIdentity);
        CheckDispose(windowsIdentity);

        string authenticationType = "WindowsAuthentication";
        WindowsIdentity windowsIdentity2 = new WindowsIdentity(logonToken, authenticationType);
        Assert.NotNull(windowsIdentity2);
        Assert.True(windowsIdentity2.IsAuthenticated);
        Assert.Equal(authenticationType, windowsIdentity2.AuthenticationType);
        CheckDispose(windowsIdentity2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void CloneAndProperties(bool cloneViaSerialization)
    {
        IntPtr logonToken = WindowsIdentity.GetCurrent().AccessToken.DangerousGetHandle();
        WindowsIdentity winId = new WindowsIdentity(logonToken);
        WindowsIdentity cloneWinId = cloneViaSerialization ?
            BinaryFormatterHelpers.Clone(winId) :
            winId.Clone() as WindowsIdentity;
        Assert.NotNull(cloneWinId);

        Assert.Equal(winId.IsSystem, cloneWinId.IsSystem);
        Assert.Equal(winId.IsGuest, cloneWinId.IsGuest);
        Assert.Equal(winId.ImpersonationLevel, cloneWinId.ImpersonationLevel);

        Assert.Equal(winId.Name, cloneWinId.Name);
        Assert.Equal(winId.Owner, cloneWinId.Owner);

        IdentityReferenceCollection irc1 = winId.Groups;
        IdentityReferenceCollection irc2 = cloneWinId.Groups;
        Assert.Equal(irc1.Count, irc2.Count);

        CheckDispose(winId);
        CheckDispose(cloneWinId);        
    }

    [Fact]
    public static void GetTokenHandle()
    {
        WindowsIdentity id = WindowsIdentity.GetCurrent();
        Assert.Equal(id.AccessToken.DangerousGetHandle(), id.Token);
    }

    private static void CheckDispose(WindowsIdentity identity, bool anonymous = false)
    {
        Assert.False(identity.AccessToken.IsClosed);
        try
        {
            identity.Dispose();
        }
        catch { }
        Assert.True(identity.AccessToken.IsClosed);
        if (!anonymous)
        {
            Assert.Throws<ObjectDisposedException>(() => identity.Name);
            Assert.Throws<ObjectDisposedException>(() => identity.Owner);
            Assert.Throws<ObjectDisposedException>(() => identity.User);
        }
    }
}
