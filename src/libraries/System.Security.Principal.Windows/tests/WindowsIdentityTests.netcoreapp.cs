// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;
using System.Threading.Tasks;
using Xunit;

public partial class WindowsIdentityTests : IClassFixture<WindowsIdentityFixture>
{
    [Fact]
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

        return;

        // Assertions
        void Asserts(WindowsIdentity currentWindowsIdentity)
        {
            Assert.Equal(_fixture.TestAccount.AccountName, WindowsIdentity.GetCurrent().Name);
            Assert.NotEqual(currentWindowsIdentity.Name, WindowsIdentity.GetCurrent().Name);
        }
    }
}
