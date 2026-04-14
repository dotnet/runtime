// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests
    {
        private static bool IsNotNanoServerNotServerCoreAndRemoteExecutorSupported =>
            PlatformDetection.IsNotWindowsNanoServer &&
            PlatformDetection.IsNotWindowsServerCore &&
            RemoteExecutor.IsSupported;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowW(string? lpClassName, string? lpWindowName);

        // Creates a visible top-level window with a known title.
        // Shows a MessageBox on a background thread, waits until the window is visible via FindWindowW,
        // then signals the parent via stdout and blocks until killed by the parent.
        private static int CreateMainWindowWithTitle()
        {
            // MessageBoxW blocks the current thread until the user closes the message box
            Thread t = new Thread(() => MessageBoxW(IntPtr.Zero, string.Empty, "Test Main Window", 0 /* MB_OK */));
            t.IsBackground = true;
            t.Start();

            // Spin until the MessageBox window is actually visible before signaling the parent.
            SpinWait.SpinUntil(() => FindWindowW(null, "Test Main Window") != IntPtr.Zero);

            // Signal the parent that the window is ready.
            Console.WriteLine("ready");

            // Block until the parent kills this process.
            Thread.Sleep(Timeout.Infinite);
            return RemoteExecutor.SuccessExitCode;
        }

        [ConditionalFact(typeof(ProcessTests), nameof(IsNotNanoServerNotServerCoreAndRemoteExecutorSupported))]
        [OuterLoop("Pops UI")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task MainWindowHandle_And_Title_GetWithGui_ShouldRefresh_Windows()
        {
            using Process process = CreateProcess(CreateMainWindowWithTitle);
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            try
            {
                // Wait for child to signal that its window is ready.
                Assert.Equal("ready", await process.StandardOutput.ReadLineAsync());

                process.Refresh();
                Assert.NotEqual(IntPtr.Zero, process.MainWindowHandle);
                Assert.Equal("Test Main Window", process.MainWindowTitle);
            }
            finally
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        private string WriteScriptFile(string directory, string name, int returnValue)
        {
            string filename = Path.Combine(directory, name);
            filename += ".bat";
            File.WriteAllText(filename, $"exit {returnValue}");
            return filename;
        }
        
        private static void SendSignal(PosixSignal signal, Process process, bool entireProcessGroup = false)
        {
            // Windows console control events are delivered to console process groups via
            // GenerateConsoleCtrlEvent; this implementation cannot distinguish between
            // signaling only the process and signaling its entire process group.
            _ = entireProcessGroup;

            uint dwCtrlEvent = signal switch
            {
                PosixSignal.SIGINT => Interop.Kernel32.CTRL_C_EVENT,
                PosixSignal.SIGQUIT => Interop.Kernel32.CTRL_BREAK_EVENT,
                _ => throw new ArgumentOutOfRangeException(nameof(signal))
            };

            if (!Interop.GenerateConsoleCtrlEvent(dwCtrlEvent, (uint)process.Id))
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
