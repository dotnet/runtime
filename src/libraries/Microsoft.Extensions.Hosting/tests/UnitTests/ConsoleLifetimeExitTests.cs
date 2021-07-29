// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class ConsoleLifetimeExitTests
    {
        /// <summary>
        /// Tests that a Hosted process that receives SIGTERM/SIGINT/SIGQUIT completes successfully
        /// and the rest of "main" gets executed.
        /// </summary>
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(SIGTERM)]
        [InlineData(SIGINT)]
        [InlineData(SIGQUIT)]
        public async Task EnsureSignalContinuesMainMethod(int signal)
        {
            using var remoteHandle = RemoteExecutor.Invoke(async () =>
            {
                await Host.CreateDefaultBuilder()
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddHostedService<EnsureSignalContinuesMainMethodWorker>();
                    })
                    .RunConsoleAsync();

                // adding this delay ensures the "main" method loses in a race with the normal process exit
                // and can cause the below message not to be written when the normal process exit isn't canceled by the
                // SIGTERM handler
                await Task.Delay(100);

                Console.WriteLine("Run has completed");
                return 123;
            }, new RemoteInvokeOptions() { Start = false, ExpectedExitCode = 123 });

            remoteHandle.Process.StartInfo.RedirectStandardOutput = true;
            remoteHandle.Process.Start();

            // wait for the host process to start
            string line;
            while ((line = remoteHandle.Process.StandardOutput.ReadLine()).EndsWith("Started"))
            {
                await Task.Delay(20);
            }

            // send the signal to the process
            kill(remoteHandle.Process.Id, signal);

            remoteHandle.Process.WaitForExit();

            string processOutput = remoteHandle.Process.StandardOutput.ReadToEnd();
            Assert.Contains("Run has completed", processOutput);
            Assert.Equal(123, remoteHandle.Process.ExitCode);
        }

        private const int SIGINT = 2;
        private const int SIGQUIT = 3;
        private const int SIGTERM = 15;

        [DllImport("libc", SetLastError = true)]
        private static extern int kill(int pid, int sig);

        private class EnsureSignalContinuesMainMethodWorker : BackgroundService
        {
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(20, stoppingToken);
                        Console.WriteLine("Started");
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Tests that calling Environment.Exit from a Hosted app sets the correct exit code.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        // SIGTERM is only handled on net6.0+, so the workaround to "clobber" the exit code is still in place on NetFramework
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] 
        public void EnsureEnvironmentExitCode()
        {
            using var remoteHandle = RemoteExecutor.Invoke(async () =>
            {
                await Host.CreateDefaultBuilder()
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddHostedService<EnsureEnvironmentExitCodeWorker>();
                    })
                    .RunConsoleAsync();
            });

            remoteHandle.Process.WaitForExit();

            Assert.Equal(124, remoteHandle.Process.ExitCode);
        }

        private class EnsureEnvironmentExitCodeWorker : BackgroundService
        {
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                await Task.Run(() =>
                {
                    Environment.Exit(124);
                });
            }
        }

        /// <summary>
        /// Tests that calling Environment.Exit from the "main" thread doesn't hang the process forever.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EnsureEnvironmentExitDoesntHang()
        {
            using var remoteHandle = RemoteExecutor.Invoke(async () =>
            {
                await Host.CreateDefaultBuilder()
                    .ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromMilliseconds(100))
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddHostedService<EnsureEnvironmentExitDoesntHangWorker>();
                    })
                    .RunConsoleAsync();
            }, new RemoteInvokeOptions() { TimeOut = 10_000 }); // give a 10 second time out, so if this does hang, it doesn't hang for the full timeout

            Assert.True(remoteHandle.Process.WaitForExit(10_000), "The hosted process should have exited within 10 seconds");

            // SIGTERM is only handled on net6.0+, so the workaround to "clobber" the exit code is still in place on NetFramework
            int expectedExitCode = PlatformDetection.IsNetFramework ? 0 : 125;
            Assert.Equal(expectedExitCode, remoteHandle.Process.ExitCode);
        }

        private class EnsureEnvironmentExitDoesntHangWorker : BackgroundService
        {
            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                Environment.Exit(125);
                return Task.CompletedTask;
            }
        }
    }
}
