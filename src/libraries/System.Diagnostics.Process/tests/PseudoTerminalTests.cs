// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class PseudoTerminalTests : ProcessTestBase
    {
        [Fact]
        public void Create_DefaultOptions_Succeeds()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();
            Assert.NotNull(pty);
        }

        [Fact]
        public void Create_WithOptions_Succeeds()
        {
            PseudoTerminalOptions options = new PseudoTerminalOptions
            {
                Columns = 120,
                Rows = 40
            };

            using PseudoTerminal pty = PseudoTerminal.Create(options);
            Assert.NotNull(pty);
        }

        [Fact]
        public void Create_NullOptions_Succeeds()
        {
            using PseudoTerminal pty = PseudoTerminal.Create(null);
            Assert.NotNull(pty);
        }

        [Fact]
        public void Resize_ValidDimensions_Succeeds()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();
            pty.Resize(132, 50);
        }

        [Fact]
        public void Resize_InvalidColumns_Throws()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();
            Assert.Throws<ArgumentOutOfRangeException>("columns", () => pty.Resize(0, 24));
            Assert.Throws<ArgumentOutOfRangeException>("columns", () => pty.Resize(-1, 24));
        }

        [Fact]
        public void Resize_InvalidRows_Throws()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();
            Assert.Throws<ArgumentOutOfRangeException>("rows", () => pty.Resize(80, 0));
            Assert.Throws<ArgumentOutOfRangeException>("rows", () => pty.Resize(80, -1));
        }

        [Fact]
        public void Resize_AfterDispose_Throws()
        {
            PseudoTerminal pty = PseudoTerminal.Create();
            pty.Dispose();
            Assert.Throws<ObjectDisposedException>(() => pty.Resize(80, 24));
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            PseudoTerminal pty = PseudoTerminal.Create();
            pty.Dispose();
            pty.Dispose();
        }

        [Fact]
        public void ProcessStartInfo_PseudoTerminal_Property()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            Assert.Null(startInfo.PseudoTerminal);

            using PseudoTerminal pty = PseudoTerminal.Create();
            startInfo.PseudoTerminal = pty;
            Assert.Same(pty, startInfo.PseudoTerminal);

            startInfo.PseudoTerminal = null;
            Assert.Null(startInfo.PseudoTerminal);
        }

        [Fact]
        public void ProcessStartInfo_PseudoTerminal_CannotCombineWithUseShellExecute()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();
            Process process = new Process();
            process.StartInfo.FileName = "test";
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.PseudoTerminal = pty;

            Assert.Throws<InvalidOperationException>(() => process.Start());
        }

        [Fact]
        public void ProcessStartInfo_PseudoTerminal_CannotCombineWithRedirectStandardInput()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();
            Process process = new Process();
            process.StartInfo.FileName = "test";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.PseudoTerminal = pty;

            Assert.Throws<InvalidOperationException>(() => process.Start());
        }

        [Fact]
        public void ProcessStartInfo_PseudoTerminal_CannotCombineWithRedirectStandardOutput()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();
            Process process = new Process();
            process.StartInfo.FileName = "test";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.PseudoTerminal = pty;

            Assert.Throws<InvalidOperationException>(() => process.Start());
        }

        [Fact]
        public void ProcessStartInfo_PseudoTerminal_CannotCombineWithRedirectStandardError()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();
            Process process = new Process();
            process.StartInfo.FileName = "test";
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.PseudoTerminal = pty;

            Assert.Throws<InvalidOperationException>(() => process.Start());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartProcess_WithPseudoTerminal_ConsoleIsNotRedirected()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();

            Process process = CreateProcess(static () =>
            {
                // When attached to a PTY, the console should not report as redirected.
                Assert.False(Console.IsOutputRedirected);
                Assert.False(Console.IsInputRedirected);
                Assert.False(Console.IsErrorRedirected);
                return RemoteExecutor.SuccessExitCode;
            });

            process.StartInfo.PseudoTerminal = pty;
            Assert.True(process.Start());
            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, process.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartProcess_WithPseudoTerminal_CanCommunicate()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();

            Process process = CreateProcess(static () =>
            {
                Console.Write("hello from child");
                return RemoteExecutor.SuccessExitCode;
            });

            process.StartInfo.PseudoTerminal = pty;
            Assert.True(process.Start());

            // Read from the PTY to get the child's output.
            Assert.NotNull(process.StandardOutput);
            string output = process.StandardOutput.ReadToEnd();
            Assert.Contains("hello from child", output);

            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, process.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartProcess_WithPseudoTerminal_CanSendInput()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();

            Process process = CreateProcess(static () =>
            {
                string line = Console.ReadLine();
                if (line == "test input")
                {
                    return RemoteExecutor.SuccessExitCode;
                }
                return 1;
            });

            process.StartInfo.PseudoTerminal = pty;
            Assert.True(process.Start());

            // Write to the PTY to send input to the child.
            Assert.NotNull(process.StandardInput);
            process.StandardInput.WriteLine("test input");

            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, process.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartProcess_WithPseudoTerminal_CustomWindowSize()
        {
            PseudoTerminalOptions options = new PseudoTerminalOptions
            {
                Columns = 132,
                Rows = 50
            };

            using PseudoTerminal pty = PseudoTerminal.Create(options);

            Process process = CreateProcess(static () =>
            {
                // The child process should see the PTY window size.
                Assert.Equal(132, Console.WindowWidth);
                Assert.Equal(50, Console.WindowHeight);
                return RemoteExecutor.SuccessExitCode;
            });

            process.StartInfo.PseudoTerminal = pty;
            Assert.True(process.Start());
            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, process.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void StartProcess_WithPseudoTerminal_IsATty()
        {
            using PseudoTerminal pty = PseudoTerminal.Create();

            Process process = CreateProcess(static () =>
            {
                // On Unix, when attached to a PTY, isatty should return true for stdout.
                bool isTty = IsAtty(1);
                Assert.True(isTty);
                return RemoteExecutor.SuccessExitCode;
            });

            process.StartInfo.PseudoTerminal = pty;
            Assert.True(process.Start());
            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, process.ExitCode);
        }

        [DllImport("libc", EntryPoint = "isatty")]
        private static extern int NativeIsAtty(int fd);

        private static bool IsAtty(int fd) => NativeIsAtty(fd) == 1;
    }
}
