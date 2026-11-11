// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    }
}
