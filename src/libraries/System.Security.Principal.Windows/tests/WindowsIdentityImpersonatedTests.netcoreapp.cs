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

public class WindowsIdentityFixture : IDisposable
{
    public WindowsTestAccount TestAccount { get; private set; }

    public WindowsIdentityFixture()
    {
        TestAccount = new WindowsTestAccount("CorFxTstWiIde01kiu");
    }

    public void Dispose()
    {
        TestAccount.Dispose();
    }
}

public sealed class WindowsTestAccount : IDisposable
{
    private readonly string _userName;
    private SafeAccessTokenHandle _accountTokenHandle;
    public SafeAccessTokenHandle AccountTokenHandle => _accountTokenHandle;
    public string AccountName { get; private set; }

    public WindowsTestAccount(string userName)
    {
        _userName = userName;
        CreateUser();
    }

    private void CreateUser()
    {
        string testAccountPassword;
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            byte[] randomBytes = new byte[33];
            rng.GetBytes(randomBytes);

            // Add special chars to ensure it satisfies password requirements.
            testAccountPassword = Convert.ToBase64String(randomBytes) + "_-As@!%*(1)4#2";

            USER_INFO_1 userInfo = new USER_INFO_1
            {
                usri1_name = _userName,
                usri1_password = testAccountPassword,
                usri1_priv = 1
            };

            // Create user and remove/create if already exists
            uint result = NetUserAdd(null, 1, ref userInfo, out uint param_err);

            // error codes https://docs.microsoft.com/en-us/windows/desktop/netmgmt/network-management-error-codes
            // 0 == NERR_Success
            if (result == 2224) // NERR_UserExists
            {
                result = NetUserDel(null, userInfo.usri1_name);
                if (result != 0)
                {
                    throw new Win32Exception((int)result);
                }
                result = NetUserAdd(null, 1, ref userInfo, out param_err);
                if (result != 0)
                {
                    throw new Win32Exception((int)result);
                }
            }

            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;

            if (!LogonUser(_userName, ".", testAccountPassword, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out _accountTokenHandle))
            {
                _accountTokenHandle = null;
                throw new Exception($"Failed to get SafeAccessTokenHandle for test account {_userName}", new Win32Exception());
            }

            bool gotRef = false;
            try
            {
                _accountTokenHandle.DangerousAddRef(ref gotRef);
                IntPtr logonToken = _accountTokenHandle.DangerousGetHandle();
                AccountName = new WindowsIdentity(logonToken).Name;
            }
            finally
            {
                if (gotRef)
                    _accountTokenHandle.DangerousRelease();
            }
        }
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(string userName, string domain, string password, int logonType, int logonProvider, out SafeAccessTokenHandle safeAccessTokenHandle);

    [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint NetUserAdd([MarshalAs(UnmanagedType.LPWStr)]string servername, uint level, ref USER_INFO_1 buf, out uint parm_err);

    [DllImport("netapi32.dll")]
    internal static extern uint NetUserDel([MarshalAs(UnmanagedType.LPWStr)]string servername, [MarshalAs(UnmanagedType.LPWStr)]string username);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct USER_INFO_1
    {
        public string usri1_name;
        public string usri1_password;
        public uint usri1_password_age;
        public uint usri1_priv;
        public string usri1_home_dir;
        public string usri1_comment;
        public uint usri1_flags;
        public string usri1_script_path;
    }

    public void Dispose()
    {
        _accountTokenHandle?.Dispose();

        uint result = NetUserDel(null, _userName);

        // 2221= NERR_UserNotFound
        if (result != 0 && result != 2221)
        {
            throw new Win32Exception((int)result);
        }
    }
}

