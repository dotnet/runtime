// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class UseWindowsServiceTests
    {
        private static bool IsRemoteExecutorSupportedAndPrivilegedProcess => RemoteExecutor.IsSupported && PlatformDetection.IsPrivilegedProcess;

        private static MethodInfo? _addWindowsServiceLifetimeMethod = null;

        [Fact]
        public void DefaultsToOffOutsideOfService()
        {
            using IHost host = new HostBuilder()
                .UseWindowsService()
                .Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.IsType<ConsoleLifetime>(lifetime);
        }

        [ConditionalFact(typeof(UseWindowsServiceTests), nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        public void CanCreateService()
        {
            using var serviceTester = WindowsServiceTester.Create(() =>
            {
                using IHost host = new HostBuilder()
                    .UseWindowsService()
                    .Build();
                host.Run();
            });

            serviceTester.Start();
            serviceTester.WaitForStatus(ServiceControllerStatus.Running);
            serviceTester.Stop();
            serviceTester.WaitForStatus(ServiceControllerStatus.Stopped);

            var status = serviceTester.QueryServiceStatus();
            Assert.Equal(0, status.win32ExitCode);
        }

        [ConditionalTheory(typeof(UseWindowsServiceTests), nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [InlineData(false)]
        [InlineData(true)]
        public void CanDisposeServiceWithoutStarting(bool failToStart)
        {
            string serviceName = $"{nameof(CanDisposeServiceWithoutStarting)}_{Guid.NewGuid():N}";
            using WindowsServiceTester serviceTester = WindowsServiceTester.Create(static () => { }, serviceName);
            bool disposed = false;
            serviceTester.Disposed += (_, _) => disposed = true;

            if (failToStart)
            {
                Assert.Throws<ArgumentNullException>("args", () =>
                {
                    using (serviceTester)
                    {
                        serviceTester.Start(null);
                    }
                });
            }
            else
            {
                serviceTester.Dispose();
            }

            Assert.True(disposed);
            AssertServiceDeleted(serviceName);
        }

        [ConditionalFact(typeof(UseWindowsServiceTests), nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        public void CanDeleteServiceWhenRemoteProcessFails()
        {
            string serviceName = $"{nameof(CanDeleteServiceWhenRemoteProcessFails)}_{Guid.NewGuid():N}";
            using WindowsServiceTester serviceTester = WindowsServiceTester.Create(static () =>
            {
                using IHost host = new HostBuilder()
                    .UseWindowsService()
                    .Build();
                host.Run();

                return RemoteExecutor.SuccessExitCode + 1;
            }, serviceName);
            bool disposed = false;
            serviceTester.Disposed += (_, _) => disposed = true;

            serviceTester.Start();
            serviceTester.WaitForStatus(ServiceControllerStatus.Running);
            serviceTester.Stop();
            serviceTester.WaitForStatus(ServiceControllerStatus.Stopped);

            RemoteExecutionException exception = Assert.Throws<RemoteExecutionException>(serviceTester.Dispose);
            Assert.Contains($"Exit code was {RemoteExecutor.SuccessExitCode + 1}", exception.Message);
            Assert.True(disposed);
            AssertServiceDeleted(serviceName);
        }

        [ConditionalFact(typeof(UseWindowsServiceTests), nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        public async Task CanWaitForServiceDeletionWhenHandleCloses()
        {
            string serviceName = $"{nameof(CanWaitForServiceDeletionWhenHandleCloses)}_{Guid.NewGuid():N}";
            using WindowsServiceTester serviceTester = WindowsServiceTester.Create(static () => { }, serviceName);
            using SafeHandle observerHandle = serviceTester.ServiceHandle;
            serviceTester.Dispose();

            var assertionStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task assertion = Task.Factory.StartNew(() =>
            {
                assertionStarted.SetResult(true);
                AssertServiceDeleted(serviceName);
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            try
            {
                await assertionStarted.Task;
                Assert.NotSame(assertion, await Task.WhenAny(assertion, Task.Delay(TimeSpan.FromMilliseconds(250))));
            }
            finally
            {
                observerHandle.Dispose();
                await assertion;
            }
        }

        [Fact]
        public void ServiceCollectionExtensionMethodDefaultsToOffOutsideOfService()
        {
            var builder = new HostApplicationBuilder();

            builder.Services.AddWindowsService();
            // No reason to write event logs in this test. Event log may be unsupported anyway.
            builder.Logging.ClearProviders();

            using IHost host = builder.Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.IsType<ConsoleLifetime>(lifetime);
        }

        [Fact]
        public void ServiceCollectionExtensionMethodAddsWindowsServiceLifetimeInsideOfService()
        {
            var builder = new HostApplicationBuilder();

            // Emulate calling builder.Services.AddWindowsService() from inside a Windows service.
            AddWindowsServiceLifetime(builder.Services);

            Assert.Single(builder.Services, serviceDescriptor =>
                serviceDescriptor.ServiceType == typeof(IHostLifetime) &&
                serviceDescriptor.ImplementationType == typeof(WindowsServiceLifetime));
        }

        [Fact]
        public void ServiceCollectionExtensionMethodSetsEventLogSourceNameToApplicationNameInsideOfService()
        {
            string appName = Guid.NewGuid().ToString();

            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                ApplicationName = appName,
            });

            // Emulate calling builder.Services.AddWindowsService() from inside a Windows service.
            AddWindowsServiceLifetime(builder.Services);
            // No reason to write event logs in this test.
            builder.Logging.ClearProviders();

            using IHost host = builder.Build();

            var eventLogSettings = host.Services.GetRequiredService<IOptions<EventLogSettings>>().Value;
            Assert.Same(appName, eventLogSettings.SourceName);
        }

        [Fact]
        public void ServiceCollectionExtensionMethodCanBeCalledOnDefaultConfiguration()
        {
            var builder = new HostApplicationBuilder();

            // Emulate calling builder.Services.AddWindowsService() from inside a Windows service.
            AddWindowsServiceLifetime(builder.Services);
            // No reason to write event logs in this test.
            builder.Logging.ClearProviders();

            using IHost host = builder.Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.IsType<WindowsServiceLifetime>(lifetime);
        }

        private static void AssertServiceDeleted(string serviceName)
        {
            const int PollIntervalMilliseconds = 250;
            using var controller = new ServiceController(serviceName);
            TimeSpan timeout = WindowsServiceTester.WaitForStatusTimeout;
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (true)
            {
                controller.Refresh();
                ServiceControllerStatus status;
                try
                {
                    status = controller.Status;
                }
                catch (InvalidOperationException exception) when (
                    exception.InnerException is Win32Exception { NativeErrorCode: Interop.Errors.ERROR_SERVICE_DOES_NOT_EXIST })
                {
                    Assert.IsType<InvalidOperationException>(exception);
                    Assert.IsType<Win32Exception>(exception.InnerException);
                    return;
                }

                Assert.True(stopwatch.Elapsed < timeout,
                    $"Service '{serviceName}' was not deleted within {timeout}. Last observed status: {status}.");
                Thread.Sleep(PollIntervalMilliseconds);
            }
        }

        private void AddWindowsServiceLifetime(IServiceCollection services, Action<WindowsServiceLifetimeOptions> configure = null)
        {
            _addWindowsServiceLifetimeMethod ??= typeof(WindowsServiceLifetimeHostBuilderExtensions).GetMethod("AddWindowsServiceLifetime",
                BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(IServiceCollection), typeof(Action<WindowsServiceLifetimeOptions>) }, null)
                ?? throw new MissingMethodException();

            configure ??= _ => { };
            _addWindowsServiceLifetimeMethod.Invoke(null, new object[] { services, configure });
        }
    }
}
