// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tests;
using Xunit;

public class WindowsIdentityTests : IClassFixture<WindowsIdentityFixture>
{
    private const string authenticationType = "WindowsAuthentication";
    private readonly WindowsIdentityFixture _fixture;

    public WindowsIdentityTests(WindowsIdentityFixture windowsIdentityFixture)
    {
        _fixture = windowsIdentityFixture;

        Assert.False(_fixture.TestAccount.AccountTokenHandle.IsInvalid);
        Assert.False(string.IsNullOrEmpty(_fixture.TestAccount.AccountName));
    }

    [Fact]
    public static void GetAnonymousUserTest()
    {
        WindowsIdentity windowsIdentity = WindowsIdentity.GetAnonymous();
        Assert.True(windowsIdentity.IsAnonymous);
        Assert.False(windowsIdentity.IsAuthenticated);
        CheckDispose(windowsIdentity, true);
    }

    [Fact]
    public static void ConstructorsAndProperties()
    {
        TestUsingAccessToken((logonToken) =>
        {
            // Construct a WindowsIdentity object using the input account token.
            var windowsIdentity = new WindowsIdentity(logonToken);
            CheckDispose(windowsIdentity);

            var windowsIdentity2 = new WindowsIdentity(logonToken, authenticationType);
            Assert.True(windowsIdentity2.IsAuthenticated);

            Assert.Equal(authenticationType, windowsIdentity2.AuthenticationType);
            CheckDispose(windowsIdentity2);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void AuthenticationCtor(bool authentication)
    {
        TestUsingAccessToken((logonToken) =>
        {
            var windowsIdentity = new WindowsIdentity(logonToken, authenticationType, WindowsAccountType.Normal, isAuthenticated: authentication);
            Assert.Equal(authentication, windowsIdentity.IsAuthenticated);

            Assert.Equal(authenticationType, windowsIdentity.AuthenticationType);
            CheckDispose(windowsIdentity);
        });
    }

    [Fact]
    public static void WindowsAccountTypeCtor()
    {
        TestUsingAccessToken((logonToken) =>
        {
            var windowsIdentity = new WindowsIdentity(logonToken, authenticationType, WindowsAccountType.Normal);
            Assert.True(windowsIdentity.IsAuthenticated);

            Assert.Equal(authenticationType, windowsIdentity.AuthenticationType);
            CheckDispose(windowsIdentity);
        });
    }

    [Fact]
    public static void CloneAndProperties()
    {
        TestUsingAccessToken((logonToken) =>
        {
            var winId = new WindowsIdentity(logonToken);

            WindowsIdentity cloneWinId = winId.Clone() as WindowsIdentity;
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
        });
    }

    [Fact]
    public static void GetTokenHandle()
    {
        WindowsIdentity id = WindowsIdentity.GetCurrent();
        Assert.Equal(id.AccessToken.DangerousGetHandle(), id.Token);
    }

    [Fact]
    public static void CheckDeviceClaims()
    {
        using (WindowsIdentity id = WindowsIdentity.GetCurrent())
        {
            int manualCount = id.Claims.Count(c => c.Properties.ContainsKey(ClaimTypes.WindowsDeviceClaim));
            int autoCount = id.DeviceClaims.Count();

            Assert.Equal(manualCount, autoCount);
        }
    }

    [Fact]
    public static void CheckUserClaims()
    {
        using (WindowsIdentity id = WindowsIdentity.GetCurrent())
        {
            Claim[] allClaims = id.Claims.ToArray();
            int deviceCount = allClaims.Count(c => c.Properties.ContainsKey(ClaimTypes.WindowsDeviceClaim));
            int manualCount = allClaims.Length - deviceCount;
            int autoCount = id.UserClaims.Count();

            Assert.Equal(manualCount, autoCount);
        }
    }

    [Fact]
    public static void RunImpersonatedTest_InvalidHandle()
    {
        using (var mutex = new Mutex())
        {
            SafeAccessTokenHandle handle = null;
            try
            {
                handle = new SafeAccessTokenHandle(mutex.SafeWaitHandle.DangerousGetHandle());
                Assert.Throws<ArgumentException>(() => WindowsIdentity.RunImpersonated(handle, () => { }));
            }
            finally
            {
                handle?.SetHandleAsInvalid();
            }
        }
    }

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

    [Fact]
    public static void RunImpersonatedAsyncTest()
    {
        var testData = new RunImpersonatedAsyncTestInfo();
        BeginTask(testData);

        // Wait for the SafeHandle that was disposed in BeginTask() to actually be closed
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.WaitForPendingFinalizers();

        testData.continueTask.Release();
        testData.task.CheckedWait();
        if (testData.exception != null)
        {
            throw new AggregateException(testData.exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BeginTask(RunImpersonatedAsyncTestInfo testInfo)
    {
        testInfo.continueTask = new SemaphoreSlim(0, 1);
        using (SafeAccessTokenHandle token = WindowsIdentity.GetCurrent().AccessToken)
        {
            WindowsIdentity.RunImpersonated(token, () =>
            {
                testInfo.task = Task.Run(async () =>
                {
                    try
                    {
                        Task<bool> task = testInfo.continueTask.WaitAsync(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                        Assert.True(await task.ConfigureAwait(false));
                    }
                    catch (Exception ex)
                    {
                        testInfo.exception = ex;
                    }
                });
            });
        }
    }

    private class RunImpersonatedAsyncTestInfo
    {
        public Task task;
        public SemaphoreSlim continueTask;
        public Exception exception;
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

    private static void TestUsingAccessToken(Action<IntPtr> ctorOrPropertyTest)
    {
        // Retrieve the Windows account token for the current user.
        SafeAccessTokenHandle token = WindowsIdentity.GetCurrent().AccessToken;
        bool gotRef = false;
        try
        {
            token.DangerousAddRef(ref gotRef);
            IntPtr logonToken = token.DangerousGetHandle();
            ctorOrPropertyTest(logonToken);
        }
        finally
        {
            if (gotRef)
                token.DangerousRelease();
        }
    }
}

public class WindowsIdentityFixture : IDisposable
{
    public WindowsTestAccount TestAccount { get; private set; }

    public WindowsIdentityFixture()
    {
        TestAccount = new WindowsTestAccount("CorFxTstWiIde01kiu");
        TestAccount.Create();
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
    public string AccountName
    {
        get
        {
            // We should not use System.Security.Principal.Windows classes that we'are testing.
            // To avoid too much pinvoke plumbing to get userName from OS for now we concat machine name.
            return Environment.MachineName + "\\" + _userName;
        }
    }
    public WindowsTestAccount(string userName) => _userName = userName;

    public void Create()
    {
        string testAccountPassword;
        using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
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
        if (_accountTokenHandle is null)
        {
            return;
        }

        _accountTokenHandle.Dispose();
        _accountTokenHandle = null;

        uint result = NetUserDel(null, _userName);
        if (result != 0)
        {
            throw new Win32Exception((int)result);
        }
    }
}
