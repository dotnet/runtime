// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
