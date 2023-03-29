// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class WindowsServiceLifetimeTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void ServiceSequenceIsCorrect()
        {
            using var serviceTester = WindowsServiceTester.Create(nameof(ServiceSequenceIsCorrect), () =>
            {
                SimpleServiceLogger.InitializeForTestCase(nameof(ServiceSequenceIsCorrect));
                using IHost host = new HostBuilder()
                    .ConfigureServices(services =>
                    {
                        services.AddHostedService<SimpleBackgroundService>();
                        services.AddSingleton<IHostLifetime, SimpleWindowsServiceLifetime>();
                    })
                    .Build();

                var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                applicationLifetime.ApplicationStarted.Register(() => SimpleServiceLogger.Log($"lifetime started"));
                applicationLifetime.ApplicationStopping.Register(() => SimpleServiceLogger.Log($"lifetime stopping"));
                applicationLifetime.ApplicationStopped.Register(() => SimpleServiceLogger.Log($"lifetime stopped"));

                SimpleServiceLogger.Log("host.Run()");
                host.Run();
                SimpleServiceLogger.Log("host.Run() complete");
            });

            SimpleServiceLogger.DeleteLog(nameof(ServiceSequenceIsCorrect));

            serviceTester.Start();
            serviceTester.WaitForStatus(ServiceControllerStatus.Running);

            var statusEx = serviceTester.QueryServiceStatusEx();
            var serviceProcess = Process.GetProcessById(statusEx.dwProcessId);

            serviceTester.Stop();
            serviceTester.WaitForStatus(ServiceControllerStatus.Stopped);
            
            serviceProcess.WaitForExit();

            var status = serviceTester.QueryServiceStatus();
            Assert.Equal(0, status.win32ExitCode);

            var logText = SimpleServiceLogger.ReadLog(nameof(ServiceSequenceIsCorrect));
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

        public class SimpleWindowsServiceLifetime : WindowsServiceLifetime
        {
            public SimpleWindowsServiceLifetime(IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor) :
                base(environment, applicationLifetime, loggerFactory, optionsAccessor)
            { }

            protected override void OnStart(string[] args)
            {
                SimpleServiceLogger.Log("WindowsServiceLifetime.OnStart");
                base.OnStart(args);
            }

            protected override void OnStop()
            {
                SimpleServiceLogger.Log("WindowsServiceLifetime.OnStop");
                base.OnStop();
            }
        }

        public class SimpleBackgroundService : BackgroundService
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            protected override async Task ExecuteAsync(CancellationToken stoppingToken) => SimpleServiceLogger.Log("BackgroundService.ExecuteAsync");
            public override async Task StartAsync(CancellationToken stoppingToken) => SimpleServiceLogger.Log("BackgroundService.StartAsync");
            public override async Task StopAsync(CancellationToken stoppingToken) => SimpleServiceLogger.Log("BackgroundService.StopAsync");
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        }

        static class SimpleServiceLogger
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
