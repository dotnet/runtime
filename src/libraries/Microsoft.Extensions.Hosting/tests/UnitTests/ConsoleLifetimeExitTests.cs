// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
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
        [InlineData(SIGTERM)]
        [InlineData(SIGINT)]
        [InlineData(SIGQUIT)]
        public async Task EnsureSignalContinuesMainMethod(int signal)
        {
            // simulate signals on Windows by using a pipe to communicate with the remote process
            using var messagePipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);

            using var remoteHandle = RemoteExecutor.Invoke(async (pipeHandleAsString) =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // kick off a thread to simulate the signal on Windows
                    _ = Task.Run(() => SimulatePosixSignalWindows(pipeHandleAsString));
                }

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
            }, messagePipe.GetClientHandleAsString(), new RemoteInvokeOptions() { Start = false, ExpectedExitCode = 123 });

            remoteHandle.Process.StartInfo.RedirectStandardOutput = true;
            remoteHandle.Process.Start();

            // wait for the host process to start
            string line;
            while ((line = remoteHandle.Process.StandardOutput.ReadLine()).EndsWith("Started"))
            {
                await Task.Delay(20);
            }

            // send the signal to the process
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // on Windows, we use the pipe to signal the process
                messagePipe.WriteByte((byte)signal);
            }
            else
            {
                // on Unix, we send the signal directly
                kill(remoteHandle.Process.Id, signal);
            }

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


        private const int CTRL_C_EVENT = 0;
        private const int CTRL_BREAK_EVENT = 1;
        private const int CTRL_CLOSE_EVENT = 2;
        private const int CTRL_LOGOFF_EVENT = 5;
        private const int CTRL_SHUTDOWN_EVENT = 6;

        private static unsafe void SimulatePosixSignalWindows(string pipeHandleAsString)
        {
            try
            {
                using var readPipe = new AnonymousPipeClientStream(PipeDirection.In, pipeHandleAsString);

                int signal = (int)readPipe.ReadByte();

                int ctrlType = (int)signal switch
                {
                    SIGINT => CTRL_C_EVENT,
                    SIGQUIT => CTRL_BREAK_EVENT,
                    SIGTERM => CTRL_SHUTDOWN_EVENT,
                    _ => throw new ArgumentOutOfRangeException(nameof(signal), "Unsupported signal")
                };

#if NETFRAMEWORK
            if (ctrlType == CTRL_C_EVENT || ctrlType == CTRL_BREAK_EVENT)            
            {
                var handlerMethod = Type.GetType("System.Console, mscorlib")?.GetMethod("BreakEvent", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(handlerMethod);
                handlerMethod.Invoke(null, [ctrlType]);
            }
            else // CTRL_SHUTDOWN_EVENT
            {
                var handlerField = typeof(AppDomain).GetField("_processExit", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handlerField);
                EventHandler handler = (EventHandler)handlerField.GetValue(AppDomain.CurrentDomain);
                Assert.NotNull(handler);
                handler.Invoke(AppDomain.CurrentDomain, null);
            }
#else
                // get the System.Runtime.InteropServices.PosixSignalRegistration.HandlerRoutine private method
                var handlerMethod = typeof(PosixSignalRegistration).GetMethod("HandlerRoutine", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(handlerMethod);

                var handlerPtr = handlerMethod.MethodHandle.GetFunctionPointer();
                delegate* unmanaged<int, int> handler = (delegate* unmanaged<int, int>)handlerPtr;

                handler(ctrlType);

                if (signal == SIGTERM)
                {
                    // on Windows the OS will kill the process immediately after this
                    Environment.FailFast("Simulating shutdown");
                }
#endif
            }
            catch (Exception ex)
            {
                // Exceptions on this thread will not be observed, nor will they cause the process to exit.
                // Use failfast to ensure the process will exit without running any handlers. 
                Environment.FailFast(ex.ToString());
            }
        }

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
            }, new RemoteInvokeOptions() { ExpectedExitCode = 124 });
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
            // SIGTERM is only handled on net6.0+, so the workaround to "clobber" the exit code is still in place on .NET Framework
            int expectedExitCode = PlatformDetection.IsNetFramework ? 0 : 125;

            using var remoteHandle = RemoteExecutor.Invoke(async () =>
            {
                await Host.CreateDefaultBuilder()
                    .ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromMilliseconds(100))
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddHostedService<EnsureEnvironmentExitDoesntHangWorker>();
                    })
                    .RunConsoleAsync();
            }, new RemoteInvokeOptions() { TimeOut = 30_000, ExpectedExitCode = expectedExitCode }); // give a 30 second time out, so if this does hang, it doesn't hang for the full timeout
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
