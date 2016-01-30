// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;
using Xunit;

public class WindowsPrincipalTests
{
    [Fact]
    public static void WindowsPrincipalIsInRoleNeg()
    {
        WindowsIdentity windowsIdentity = WindowsIdentity.GetAnonymous();
        WindowsPrincipal windowsPrincipal = new WindowsPrincipal(windowsIdentity);
        var ret = windowsPrincipal.IsInRole("FAKEDOMAIN\\nonexist");
        Assert.False(ret);
    }
}
