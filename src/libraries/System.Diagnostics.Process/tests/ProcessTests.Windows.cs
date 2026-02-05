// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests
    {
        private string WriteScriptFile(string directory, string name, int returnValue)
        {
            string filename = Path.Combine(directory, name);
            filename += ".bat";
            File.WriteAllText(filename, $"exit {returnValue}");
            return filename;
        }
        
        private static void SendSignal(PosixSignal signal, int processId)
        {
            uint dwCtrlEvent = signal switch
            {
                PosixSignal.SIGINT => Interop.Kernel32.CTRL_C_EVENT,
                PosixSignal.SIGQUIT => Interop.Kernel32.CTRL_BREAK_EVENT,
                _ => throw new ArgumentOutOfRangeException(nameof(signal))
            };

            if (!Interop.GenerateConsoleCtrlEvent(dwCtrlEvent, (uint)processId))
            {
                int error = Marshal.GetLastWin32Error();
                if (error == Interop.Errors.ERROR_INVALID_FUNCTION && PlatformDetection.IsInContainer)
                {
                    // Docker in CI runs without a console attached.
                    throw new SkipTestException($"GenerateConsoleCtrlEvent failed with ERROR_INVALID_FUNCTION. The process is not a console process or does not have a console.");
                }

                throw new Win32Exception(error);
            }
        }

        // See https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw#remarks:
        // When a process is created with CREATE_NEW_PROCESS_GROUP specified, an implicit call to SetConsoleCtrlHandler(NULL,TRUE) 
        // is made on behalf of the new process; this means that the new process has CTRL+C disabled.
        private static unsafe void ReEnableCtrlCHandlerIfNeeded(PosixSignal signal)
        {
            if (signal is PosixSignal.SIGINT)
            {
                if (!Interop.Kernel32.SetConsoleCtrlHandler(null, false))
                {
                    throw new Win32Exception();
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Kill_EntireProcessTree_MinimalExceptions()
        {
            // This test validates that Kill(true) doesn't throw excessive exceptions internally
            // during process enumeration, which causes severe performance degradation with debugger attached.
            // See https://github.com/dotnet/runtime/issues/121279
            
            int exceptionCount = 0;
            EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> handler = 
                (sender, e) => Interlocked.Increment(ref exceptionCount);
            
            AppDomain.CurrentDomain.FirstChanceException += handler;
            
            try
            {
                // Create a process tree based on the repro from the issue:
                // Main process spawns child1, child1 spawns child2
                RemoteInvokeHandle handle = RemoteExecutor.Invoke(CreateProcessTreeAndWait);
                Process parentProcess = handle.Process;

                try
                {
                    // Wait a bit to ensure the process tree is established
                    Thread.Sleep(1500);
                    
                    // Reset the counter before killing
                    exceptionCount = 0;
                    
                    // Kill the entire process tree
                    parentProcess.Kill(entireProcessTree: true);
                    
                    // Wait for process to exit
                    Assert.True(parentProcess.WaitForExit(Helpers.PassingTestTimeoutMilliseconds));
                    
                    // The fix should ensure that we don't throw exceptions for each system process
                    // that we can't access during enumeration. Allow up to 5 exceptions for edge cases
                    // (e.g., processes that exit during enumeration, rare error conditions)
                    Assert.True(exceptionCount <= 5, 
                        $"Expected no more than 5 exceptions during Kill(true), but got {exceptionCount}");
                }
                finally
                {
                    try
                    {
                        if (!parentProcess.HasExited)
                        {
                            parentProcess.Kill();
                        }
                        handle.Dispose();
                    }
                    catch
                    {
                        // Best effort cleanup
                    }
                }
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= handler;
            }
        }

        private static int CreateProcessTreeAndWait()
        {
            // This mimics the repro code from the issue
            // Create child1 which will create child2
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = RemoteExecutor.HostRunner,
                Arguments = $"exec \"{RemoteExecutor.Path}\" {typeof(ProcessTests).Assembly.Location} {nameof(SleepForever)}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using (Process child1 = Process.Start(psi))
            {
                // Wait a bit to ensure child1 is running
                Thread.Sleep(500);
                
                // Keep this process alive
                Thread.Sleep(Timeout.Infinite);
            }
            
            return RemoteExecutor.SuccessExitCode;
        }

        private static int SleepForever()
        {
            Thread.Sleep(Timeout.Infinite);
            return RemoteExecutor.SuccessExitCode;
        }
    }
}
