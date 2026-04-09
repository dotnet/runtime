// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tests;
using Xunit;

public class WindowsIdentityTests
{
    private const string authenticationType = "WindowsAuthentication";

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
        using WindowsIdentity id = WindowsIdentity.GetCurrent();
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
        using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
        using (SafeAccessTokenHandle token = currentIdentity.AccessToken)
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
        using WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
        SafeAccessTokenHandle token = currentIdentity.AccessToken;
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
