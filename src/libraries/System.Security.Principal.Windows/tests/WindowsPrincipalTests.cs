// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;

public class WindowsPrincipalTests
{
    [Fact]
    public static void WindowsPrincipalIsInRoleNeg()
    {
        using WindowsIdentity windowsIdentity = WindowsIdentity.GetAnonymous();
        WindowsPrincipal windowsPrincipal = new WindowsPrincipal(windowsIdentity);

        try
        {
            bool ret = windowsPrincipal.IsInRole("FAKEDOMAIN\\nonexist");
            Assert.False(ret);
        }
        catch (SystemException e)
        {
            // If a domain joined machine can't talk to the domain controller then it
            // can't determine if "FAKEDOMAIN" is resolvable within the AD forest, so it
            // fails with ERROR_TRUSTED_RELATIONSHIP_FAILURE (resulting in an exception).
            const int ERROR_TRUSTED_RELATIONSHIP_FAILURE = 0x6FD;
            Win32Exception win32Exception = new Win32Exception(ERROR_TRUSTED_RELATIONSHIP_FAILURE);
            
            Assert.Equal(win32Exception.Message, e.Message);
        }
    }

    [Fact]
    public static void CheckDeviceClaims()
    {
        using (WindowsIdentity id = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(id);

            int manualCount = principal.Claims.Count(c => c.Properties.ContainsKey(ClaimTypes.WindowsDeviceClaim));
            int autoCount = principal.DeviceClaims.Count();

            Assert.Equal(manualCount, autoCount);
        }
    }

    [Fact]
    public static void CheckUserClaims()
    {
        using (WindowsIdentity id = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(id);
            Claim[] allClaims = principal.Claims.ToArray();
            int deviceCount = allClaims.Count(c => c.Properties.ContainsKey(ClaimTypes.WindowsDeviceClaim));
            int manualCount = allClaims.Length - deviceCount;
            int autoCount = principal.UserClaims.Count();

            Assert.Equal(manualCount, autoCount);
        }
    }
}
