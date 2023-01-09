// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    // Class to be used as xUnit fixture to avoid creating the user, an relatively slow operation (couple of seconds), multiple times.
    public class TestAccountImpersonator : IDisposable
    {
        private const string TestAccountName = "CorFxTst0uZa"; // Random suffix to avoid matching any other account by accident, but const to avoid leaking it.
        private SafeAccessTokenHandle _testAccountTokenHandle;

        public TestAccountImpersonator()
        {
            string testAccountPassword;
            byte[] randomBytes = RandomNumberGenerator.GetBytes(33);

            // Add special chars to ensure it satisfies password requirements.
            testAccountPassword = Convert.ToBase64String(randomBytes) + "_-As@!%*(1)4#2";

            DateTime accountExpirationDate = DateTime.UtcNow + TimeSpan.FromMinutes(2);
            using (var principalCtx = new PrincipalContext(ContextType.Machine))
            {
                bool needToCreate = false;
                using (var foundUserPrincipal = UserPrincipal.FindByIdentity(principalCtx, TestAccountName))
                {
                    if (foundUserPrincipal == null)
                    {
                        needToCreate = true;
                    }
                    else
                    {
                        // Somehow the account leaked from previous runs, however, password is lost, reset it.
                        foundUserPrincipal.SetPassword(testAccountPassword);
                        foundUserPrincipal.AccountExpirationDate = accountExpirationDate;
                        foundUserPrincipal.Save();
                    }
                }

                if (needToCreate)
                {
                    using (var userPrincipal = new UserPrincipal(principalCtx))
                    {
                        userPrincipal.SetPassword(testAccountPassword);
                        userPrincipal.AccountExpirationDate = accountExpirationDate;
                        userPrincipal.Name = TestAccountName;
                        userPrincipal.DisplayName = TestAccountName;
                        userPrincipal.Description = TestAccountName;
                        userPrincipal.Save();
                    }
                }
            }

            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;

            if (!LogonUser(TestAccountName, ".", testAccountPassword, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out _testAccountTokenHandle))
            {
                _testAccountTokenHandle = null;
                throw new Exception($"Failed to get SafeAccessTokenHandle for test account {TestAccountName}", new Win32Exception());
            }
        }

        public void Dispose()
        {
            if (_testAccountTokenHandle == null)
                return;

            _testAccountTokenHandle.Dispose();
            _testAccountTokenHandle = null;

            using (var principalCtx = new PrincipalContext(ContextType.Machine))
            using (var userPrincipal = UserPrincipal.FindByIdentity(principalCtx, TestAccountName))
            {
                if (userPrincipal == null)
                    throw new Exception($"Failed to get user principal to delete test account {TestAccountName}");

                try
                {
                    userPrincipal.Delete();
                }
                catch (InvalidOperationException)
                {
                    // TODO: Investigate, it always throw this exception with "Can't delete object already deleted", but it actually deletes it.
                }
            }
        }

        // This method asserts if it impersonates the current identity, i.e.: it ensures that an actual impersonation happens
        public void RunImpersonated(Action action)
        {
            using (WindowsIdentity serverIdentity = WindowsIdentity.GetCurrent())
            {
                WindowsIdentity.RunImpersonated(_testAccountTokenHandle, () =>
                {
                    using WindowsIdentity clientIdentity = WindowsIdentity.GetCurrent();
                    Assert.NotEqual(serverIdentity.Name, clientIdentity.Name);
                    Assert.False(new WindowsPrincipal(clientIdentity).IsInRole(WindowsBuiltInRole.Administrator));

                    action();
                });
            }
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string userName, string domain, string password, int logonType, int logonProvider, out SafeAccessTokenHandle safeAccessTokenHandle);
    }

    /// <summary>
    /// Negative tests for PipeOptions.CurrentUserOnly in Windows.
    /// </summary>
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
    public class NamedPipeTest_CurrentUserOnly_Windows : IClassFixture<TestAccountImpersonator>
    {
        public static bool IsSupportedWindowsVersionAndPrivilegedProcess => PlatformDetection.IsPrivilegedProcess
            && PlatformDetection.IsWindows
            && !PlatformDetection.IsWindows7
            && !PlatformDetection.IsWindowsNanoServer
            && !PlatformDetection.IsWindowsServerCore;

        private TestAccountImpersonator _testAccountImpersonator;

        public NamedPipeTest_CurrentUserOnly_Windows(TestAccountImpersonator testAccountImpersonator)
        {
            _testAccountImpersonator = testAccountImpersonator;
        }

        [OuterLoop("Requires admin privileges")]
        [ConditionalFact(nameof(IsSupportedWindowsVersionAndPrivilegedProcess))]
        public async Task Connection_UnderDifferentUsers_CurrentUserOnlyOnServer_InvalidClientConnectionAttempts_DoNotBlockSuccessfulClient()
        {
            string name = PipeStreamConformanceTests.GetUniquePipeName();
            bool invalidClientShouldStop = false;
            bool invalidClientIsRunning = false;

            using var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            Task serverWait = server.WaitForConnectionAsync();

            Task invalidClient = Task.Run(() =>
            {
                // invalid non-current user tries to connect to server.
                _testAccountImpersonator.RunImpersonated(() =>
                {
                    while (!Volatile.Read(ref invalidClientShouldStop))
                    {
                        using var client = new NamedPipeClientStream(".", name, PipeDirection.In, PipeOptions.Asynchronous);
                        Assert.Throws<UnauthorizedAccessException>(() => client.Connect());
                        Volatile.Write(ref invalidClientIsRunning, true);
                    }
                });
            });

            Assert.False(serverWait.IsCompleted);
            while (!Volatile.Read(ref invalidClientIsRunning)) ; // Wait until the invalid client starts running.

            // valid client tries to connect and succeeds.
            using var client = new NamedPipeClientStream(".", name, PipeDirection.In, PipeOptions.Asynchronous);
            client.Connect();
            await serverWait;
            Volatile.Write(ref invalidClientShouldStop, true);
        }

        [ConditionalTheory(nameof(IsSupportedWindowsVersionAndPrivilegedProcess))]
        [InlineData(PipeOptions.None, PipeOptions.None, PipeDirection.InOut)] // Fails even without CurrentUserOnly, because under the default pipe ACL, other users are denied Write access, and client is requesting PipeDirection.InOut
        [InlineData(PipeOptions.None, PipeOptions.CurrentUserOnly, PipeDirection.In)]
        [InlineData(PipeOptions.None, PipeOptions.CurrentUserOnly, PipeDirection.InOut)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.None, PipeDirection.In)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.None, PipeDirection.InOut)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.CurrentUserOnly, PipeDirection.In)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.CurrentUserOnly, PipeDirection.InOut)]
        public void Connection_UnderDifferentUsers_BehavesAsExpected(
            PipeOptions serverPipeOptions, PipeOptions clientPipeOptions, PipeDirection clientDirection)
        {
            string name = PipeStreamConformanceTests.GetUniquePipeName();
            using (var cts = new CancellationTokenSource())
            using (var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, serverPipeOptions | PipeOptions.Asynchronous))
            {
                Task serverTask = server.WaitForConnectionAsync(cts.Token);

                _testAccountImpersonator.RunImpersonated(() =>
                {
                    using (var client = new NamedPipeClientStream(".", name, clientDirection, clientPipeOptions))
                    {
                        Assert.Throws<UnauthorizedAccessException>(() => client.Connect());
                    }
                });

                if (serverPipeOptions == PipeOptions.None && clientPipeOptions == PipeOptions.CurrentUserOnly && clientDirection == PipeDirection.In)
                {
                    // When CurrentUserOnly is only on client side and asks for ReadOnly access, the connection is not rejected
                    // but we get the UnauthorizedAccessException on the client regardless.
                    Assert.True(serverTask.Wait(TimeSpan.FromSeconds(10)));
                    Assert.True(serverTask.IsCompletedSuccessfully);
                }
                else
                {
                    // Server is expected to not have received any request.
                    cts.Cancel();
                    AggregateException ex = Assert.Throws<AggregateException>(() => serverTask.Wait(10_000));
                    Assert.IsType<TaskCanceledException>(ex.InnerException);
                }
            }
        }

        [ConditionalTheory(nameof(IsSupportedWindowsVersionAndPrivilegedProcess))]
        [InlineData(false)]
        [InlineData(true)]
        public void Allow_Connection_UnderDifferentUsers_ForClientReading(bool useTimeSpan)
        {
            string name = PipeStreamConformanceTests.GetUniquePipeName();
            using (var server = new NamedPipeServerStream(
                       name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Task serverTask = server.WaitForConnectionAsync(CancellationToken.None);

                _testAccountImpersonator.RunImpersonated(() =>
                {
                    using (var client = new NamedPipeClientStream(".", name, PipeDirection.In))
                    {
                        if (useTimeSpan)
                        {
                            client.Connect(TimeSpan.FromMilliseconds(10_000));
                        }
                        else
                        {
                            client.Connect(10_000);
                        }
                    }
                });

                Assert.True(serverTask.Wait(10_000));
            }
        }
    }
}
