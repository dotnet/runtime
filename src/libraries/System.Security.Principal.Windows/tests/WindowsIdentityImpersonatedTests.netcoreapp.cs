// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

// On nano server netapi32.dll is not present
// we'll skip all tests on that platform
public class WindowsIdentityImpersonatedTests : IClassFixture<WindowsIdentityFixture>
{
    private readonly WindowsIdentityFixture _fixture;

    public WindowsIdentityImpersonatedTests(WindowsIdentityFixture windowsIdentityFixture)
    {
        _fixture = windowsIdentityFixture;

        Assert.False(_fixture.TestAccount.AccountTokenHandle.IsInvalid);
        Assert.False(string.IsNullOrEmpty(_fixture.TestAccount.AccountName));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    [OuterLoop]
    public async Task RunImpersonatedAsync_TaskAndTaskOfT()
    {
        WindowsIdentity currentWindowsIdentity = WindowsIdentity.GetCurrent();

        await WindowsIdentity.RunImpersonatedAsync(_fixture.TestAccount.AccountTokenHandle, async () =>
        {
            Asserts(currentWindowsIdentity);
            await Task.Delay(100);
            Asserts(currentWindowsIdentity);
        });

        Assert.Equal(WindowsIdentity.GetCurrent().Name, currentWindowsIdentity.Name);

        int result = await WindowsIdentity.RunImpersonatedAsync(_fixture.TestAccount.AccountTokenHandle, async () =>
        {
            Asserts(currentWindowsIdentity);
            await Task.Delay(100);
            Asserts(currentWindowsIdentity);
            return 42;
        });

        Assert.Equal(42, result);
        Assert.Equal(WindowsIdentity.GetCurrent().Name, currentWindowsIdentity.Name);

        // Assertions
        void Asserts(WindowsIdentity currentWindowsIdentity)
        {
            Assert.Equal(_fixture.TestAccount.AccountName, WindowsIdentity.GetCurrent().Name);
            Assert.NotEqual(currentWindowsIdentity.Name, WindowsIdentity.GetCurrent().Name);
        }
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    [OuterLoop]
    public void RunImpersonated_NameResolution()
    {
        WindowsIdentity currentWindowsIdentity = WindowsIdentity.GetCurrent();

        WindowsIdentity.RunImpersonated(_fixture.TestAccount.AccountTokenHandle, () =>
        {
            Assert.Equal(_fixture.TestAccount.AccountName, WindowsIdentity.GetCurrent().Name);

            IPAddress[] a1 = Dns.GetHostAddressesAsync("").GetAwaiter().GetResult();
            IPAddress[] a2 = Dns.GetHostAddresses("");

            Assert.True(a1.Length > 0);
            Assert.True(a1.SequenceEqual(a2));
        });
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    [OuterLoop]
    public async Task RunImpersonatedAsync_NameResolution()
    {
        WindowsIdentity currentWindowsIdentity = WindowsIdentity.GetCurrent();

        // make sure the assembly is loaded.
        _ = Dns.GetHostAddresses("");

        await WindowsIdentity.RunImpersonatedAsync(_fixture.TestAccount.AccountTokenHandle, async () =>
        {
            Assert.Equal(_fixture.TestAccount.AccountName, WindowsIdentity.GetCurrent().Name);

            IPAddress[] a1 = await Dns.GetHostAddressesAsync("");
            IPAddress[] a2 = Dns.GetHostAddresses("");

            Assert.True(a1.Length > 0);
            Assert.True(a1.SequenceEqual(a2));
        });
    }
}
