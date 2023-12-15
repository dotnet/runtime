// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class WindowsServiceLifetimeTests
    {
        private static bool IsRemoteExecutorSupportedAndPrivilegedProcess => RemoteExecutor.IsSupported && PlatformDetection.IsPrivilegedProcess;

        [ConditionalFact(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        public void ServiceStops()
        {
            using var serviceTester = WindowsServiceTester.Create(async () =>
            {
                var applicationLifetime = new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance);
                using var lifetime = new WindowsServiceLifetime(
                    new HostingEnvironment(),
                    applicationLifetime,
                    NullLoggerFactory.Instance,
                    new OptionsWrapper<HostOptions>(new HostOptions()));

                await lifetime.WaitForStartAsync(CancellationToken.None);

                // would normally occur here, but WindowsServiceLifetime does not depend on it.
                // applicationLifetime.NotifyStarted();

                // will be signaled by WindowsServiceLifetime when SCM stops the service.
                applicationLifetime.ApplicationStopping.WaitHandle.WaitOne();

                // required by WindowsServiceLifetime to identify that app has stopped.
                applicationLifetime.NotifyStopped();

                await lifetime.StopAsync(CancellationToken.None);
            });

            serviceTester.Start();
            serviceTester.WaitForStatus(ServiceControllerStatus.Running);

            var statusEx = serviceTester.QueryServiceStatusEx();
            var serviceProcess = Process.GetProcessById(statusEx.dwProcessId);

            serviceTester.Stop();
            serviceTester.WaitForStatus(ServiceControllerStatus.Stopped);

            serviceProcess.WaitForExit();

            var status = serviceTester.QueryServiceStatus();
            Assert.Equal(0, status.win32ExitCode);
        }

        [ConditionalFact(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework is missing the fix from https://github.com/dotnet/corefx/commit/3e68d791066ad0fdc6e0b81828afbd9df00dd7f8")]
        public void ExceptionOnStartIsPropagated()
        {
            using var serviceTester = WindowsServiceTester.Create(async () =>
            {
                using (var lifetime = ThrowingWindowsServiceLifetime.Create(throwOnStart: new Exception("Should be thrown")))
                {
                    Assert.Equal(lifetime.ThrowOnStart,
                            await Assert.ThrowsAsync<Exception>(async () =>
                                await lifetime.WaitForStartAsync(CancellationToken.None)));
                }
            });

            serviceTester.Start();

            serviceTester.WaitForStatus(ServiceControllerStatus.Stopped);
            var status = serviceTester.QueryServiceStatus();
            Assert.Equal(Interop.Errors.ERROR_EXCEPTION_IN_SERVICE, status.win32ExitCode);
        }

        [ConditionalFact(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        public void ExceptionOnStopIsPropagated()
        {
            using var serviceTester = WindowsServiceTester.Create(async () =>
            {
                using (var lifetime = ThrowingWindowsServiceLifetime.Create(throwOnStop: new Exception("Should be thrown")))
                {
                    await lifetime.WaitForStartAsync(CancellationToken.None);
                    lifetime.ApplicationLifetime.NotifyStopped();
                    Assert.Equal(lifetime.ThrowOnStop,
                            await Assert.ThrowsAsync<Exception>(async () =>
                                await lifetime.StopAsync(CancellationToken.None)));
                }
            });

            serviceTester.Start();

            serviceTester.WaitForStatus(ServiceControllerStatus.Stopped);
            var status = serviceTester.QueryServiceStatus();
            Assert.Equal(Interop.Errors.ERROR_PROCESS_ABORTED, status.win32ExitCode);
        }

        [ConditionalFact(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        public void CancelStopAsync()
        {
            using var serviceTester = WindowsServiceTester.Create(async () =>
            {
                var applicationLifetime = new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance);
                using var lifetime = new WindowsServiceLifetime(
                    new HostingEnvironment(),
                    applicationLifetime,
                    NullLoggerFactory.Instance,
                    new OptionsWrapper<HostOptions>(new HostOptions()));
                await lifetime.WaitForStartAsync(CancellationToken.None);

                await Assert.ThrowsAsync<OperationCanceledException>(async () => await lifetime.StopAsync(new CancellationToken(true)));
            });

            serviceTester.Start();

            serviceTester.WaitForStatus(ServiceControllerStatus.Stopped);
            var status = serviceTester.QueryServiceStatus();
            Assert.Equal(Interop.Errors.ERROR_PROCESS_ABORTED, status.win32ExitCode);
        }

        [ConditionalFact(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        public void ServiceCanStopItself()
        {
            using (var serviceTester = WindowsServiceTester.Create(async () =>
            {
                FileLogger.InitializeForTestCase(nameof(ServiceCanStopItself));
                using IHost host = new HostBuilder()
                    .ConfigureServices(services =>
                    {
                        services.AddHostedService<LoggingBackgroundService>();
                        services.AddSingleton<IHostLifetime, LoggingWindowsServiceLifetime>();
                    })
                    .Build();

                var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                applicationLifetime.ApplicationStarted.Register(() => FileLogger.Log($"lifetime started"));
                applicationLifetime.ApplicationStopping.Register(() => FileLogger.Log($"lifetime stopping"));
                applicationLifetime.ApplicationStopped.Register(() => FileLogger.Log($"lifetime stopped"));

                FileLogger.Log("host.Start()");
                host.Start();

                using (ServiceController selfController = new(nameof(ServiceCanStopItself)))
                {
                    selfController.WaitForStatus(ServiceControllerStatus.Running, WindowsServiceTester.WaitForStatusTimeout);
                    Assert.Equal(ServiceControllerStatus.Running, selfController.Status);

                    FileLogger.Log("host.Stop()");
                    await host.StopAsync();
                    FileLogger.Log("host.Stop() complete");

                    selfController.WaitForStatus(ServiceControllerStatus.Stopped, WindowsServiceTester.WaitForStatusTimeout);
                    Assert.Equal(ServiceControllerStatus.Stopped, selfController.Status);
                }
            }))
            {
                FileLogger.DeleteLog(nameof(ServiceCanStopItself));

                // service should start cleanly
                serviceTester.Start();

                // service will proceed to stopped without any error
                serviceTester.WaitForStatus(ServiceControllerStatus.Stopped);

                var status = serviceTester.QueryServiceStatus();
                Assert.Equal(0, status.win32ExitCode);

            }

            var logText = FileLogger.ReadLog(nameof(ServiceCanStopItself));
            Assert.Equal("""
                host.Start()
                WindowsServiceLifetime.OnStart
                BackgroundService.StartAsync
                lifetime started
                host.Stop()
                lifetime stopping
                BackgroundService.StopAsync
                lifetime stopped
                WindowsServiceLifetime.OnStop
                host.Stop() complete

                """, logText);
        }

        [ConditionalFact(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        public void ServiceSequenceIsCorrect()
        {
            using (var serviceTester = WindowsServiceTester.Create(() =>
            {
                FileLogger.InitializeForTestCase(nameof(ServiceSequenceIsCorrect));
                using IHost host = new HostBuilder()
                    .ConfigureServices(services =>
                    {
                        services.AddHostedService<LoggingBackgroundService>();
                        services.AddSingleton<IHostLifetime, LoggingWindowsServiceLifetime>();
                    })
                    .Build();

                var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                applicationLifetime.ApplicationStarted.Register(() => FileLogger.Log($"lifetime started"));
                applicationLifetime.ApplicationStopping.Register(() => FileLogger.Log($"lifetime stopping"));
                applicationLifetime.ApplicationStopped.Register(() => FileLogger.Log($"lifetime stopped"));

                FileLogger.Log("host.Run()");
                host.Run();
                FileLogger.Log("host.Run() complete");
            }))
            {

                FileLogger.DeleteLog(nameof(ServiceSequenceIsCorrect));

                serviceTester.Start();
                serviceTester.WaitForStatus(ServiceControllerStatus.Running);

                var statusEx = serviceTester.QueryServiceStatusEx();
                var serviceProcess = Process.GetProcessById(statusEx.dwProcessId);

                // Give a chance for all asynchronous "started" events to be raised, these happen after the service status changes to started 
                Thread.Sleep(1000);

                serviceTester.Stop();
                serviceTester.WaitForStatus(ServiceControllerStatus.Stopped);

                var status = serviceTester.QueryServiceStatus();
                Assert.Equal(0, status.win32ExitCode);

            }

            var logText = FileLogger.ReadLog(nameof(ServiceSequenceIsCorrect));
            Assert.Equal("""
                host.Run()
                WindowsServiceLifetime.OnStart
                BackgroundService.StartAsync
                lifetime started
                WindowsServiceLifetime.OnStop
                lifetime stopping
                BackgroundService.StopAsync
                lifetime stopped
                host.Run() complete

                """, logText);

        }

        public class LoggingWindowsServiceLifetime : WindowsServiceLifetime
        {
            public LoggingWindowsServiceLifetime(IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor) :
                base(environment, applicationLifetime, loggerFactory, optionsAccessor)
            { }

            protected override void OnStart(string[] args)
            {
                FileLogger.Log("WindowsServiceLifetime.OnStart");
                base.OnStart(args);
            }

            protected override void OnStop()
            {
                FileLogger.Log("WindowsServiceLifetime.OnStop");
                base.OnStop();
            }
        }

        public class ThrowingWindowsServiceLifetime : WindowsServiceLifetime
        {
            public static ThrowingWindowsServiceLifetime Create(Exception throwOnStart = null, Exception throwOnStop = null) =>
                    new ThrowingWindowsServiceLifetime(
                        new HostingEnvironment(),
                        new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance),
                        NullLoggerFactory.Instance,
                        new OptionsWrapper<HostOptions>(new HostOptions()))
                    {
                        ThrowOnStart = throwOnStart,
                        ThrowOnStop = throwOnStop
                    };

            public ThrowingWindowsServiceLifetime(IHostEnvironment environment, ApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor) :
                base(environment, applicationLifetime, loggerFactory, optionsAccessor)
            {
                ApplicationLifetime = applicationLifetime;
            }

            public ApplicationLifetime ApplicationLifetime { get; }

            public Exception ThrowOnStart { get; set; }
            protected override void OnStart(string[] args)
            {
                if (ThrowOnStart != null)
                {
                    throw ThrowOnStart;
                }
                base.OnStart(args);
            }

            public Exception ThrowOnStop { get; set; }
            protected override void OnStop()
            {
                if (ThrowOnStop != null)
                {
                    throw ThrowOnStop;
                }
                base.OnStop();
            }
        }

        public class LoggingBackgroundService : BackgroundService
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            protected override async Task ExecuteAsync(CancellationToken stoppingToken) => FileLogger.Log("BackgroundService.ExecuteAsync");
            public override async Task StartAsync(CancellationToken stoppingToken) => FileLogger.Log("BackgroundService.StartAsync");
            public override async Task StopAsync(CancellationToken stoppingToken) => FileLogger.Log("BackgroundService.StopAsync");
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        }

        static class FileLogger
        {
            static string _fileName;

            public static void InitializeForTestCase(string testCaseName)
            {
                Assert.Null(_fileName);
                _fileName = GetLogForTestCase(testCaseName);
            }

            private static string GetLogForTestCase(string testCaseName) => Path.Combine(AppContext.BaseDirectory, $"{testCaseName}.log");
            public static void DeleteLog(string testCaseName) => File.Delete(GetLogForTestCase(testCaseName));
            public static string ReadLog(string testCaseName) => File.ReadAllText(GetLogForTestCase(testCaseName));
            public static void Log(string message)
            {
                Assert.NotNull(_fileName);
                lock (_fileName)
                {
                    File.AppendAllText(_fileName, message + Environment.NewLine);
                }
            }
        }
    }
}
