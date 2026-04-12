// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessMultiplexingTests : ProcessTestBase
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_ThrowsWhenNoStreamsRedirected()
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.Start();

            Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllText_ThrowsWhenNoStreamsRedirected()
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.Start();

            Assert.Throws<InvalidOperationException>(() => process.ReadAllText());

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_ThrowsWhenOnlyOutputRedirected()
        {
            Process process = CreateProcess(RemotelyInvokable.StreamBody);
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_ThrowsWhenOnlyErrorRedirected()
        {
            Process process = CreateProcess(RemotelyInvokable.ErrorProcessBody);
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_ThrowsWhenOutputInAsyncMode()
        {
            Process process = CreateProcess(RemotelyInvokable.StreamBody);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            process.BeginOutputReadLine();

            Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());

            process.CancelOutputRead();
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_ThrowsWhenErrorInAsyncMode()
        {
            Process process = CreateProcess(RemotelyInvokable.ErrorProcessBody);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            process.BeginErrorReadLine();

            Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());

            process.CancelErrorRead();
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllText_ReadsBothOutputAndError()
        {
            Process process = CreateProcess(() =>
            {
                Console.Out.Write("stdout_text");
                Console.Error.Write("stderr_text");
                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (string standardOutput, string standardError) = process.ReadAllText();

            Assert.Equal("stdout_text", standardOutput);
            Assert.Equal("stderr_text", standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_ReadsBothOutputAndError()
        {
            Process process = CreateProcess(() =>
            {
                Console.Out.Write("stdout_bytes");
                Console.Error.Write("stderr_bytes");
                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (byte[] standardOutput, byte[] standardError) = process.ReadAllBytes();

            Assert.Equal(Encoding.Default.GetBytes("stdout_bytes"), standardOutput);
            Assert.Equal(Encoding.Default.GetBytes("stderr_bytes"), standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllText_OutputOnlyOnStdout()
        {
            Process process = CreateProcess(() =>
            {
                Console.Out.Write("only_stdout");
                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (string standardOutput, string standardError) = process.ReadAllText();

            Assert.Equal("only_stdout", standardOutput);
            Assert.Equal(string.Empty, standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllText_OutputOnlyOnStderr()
        {
            Process process = CreateProcess(() =>
            {
                Console.Error.Write("only_stderr");
                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (string standardOutput, string standardError) = process.ReadAllText();

            Assert.Equal(string.Empty, standardOutput);
            Assert.Equal("only_stderr", standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllText_ReadsLargeOutput()
        {
            string largeText = new string('A', 100_000);
            Process process = CreateProcess(RemotelyInvokable.Echo, largeText);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (string standardOutput, string standardError) = process.ReadAllText();

            Assert.Equal(largeText + Environment.NewLine, standardOutput);
            Assert.Equal(string.Empty, standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllText_TimesOutOnBlockingProcess()
        {
            Process process = CreateProcess(RemotelyInvokable.ReadLine);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();

            Assert.Throws<TimeoutException>(() => process.ReadAllText(TimeSpan.FromMilliseconds(100)));

            process.Kill();
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_OutputOnlyOnStdout()
        {
            Process process = CreateProcess(() =>
            {
                Console.Out.Write("out_data");
                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (byte[] standardOutput, byte[] standardError) = process.ReadAllBytes();

            Assert.Equal(Encoding.Default.GetBytes("out_data"), standardOutput);
            Assert.Empty(standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_OutputOnlyOnStderr()
        {
            Process process = CreateProcess(() =>
            {
                Console.Error.Write("err_data");
                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (byte[] standardOutput, byte[] standardError) = process.ReadAllBytes();

            Assert.Empty(standardOutput);
            Assert.Equal(Encoding.Default.GetBytes("err_data"), standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllText_EmptyOutput()
        {
            Process process = CreateProcess(() => RemoteExecutor.SuccessExitCode);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (string standardOutput, string standardError) = process.ReadAllText();

            Assert.Equal(string.Empty, standardOutput);
            Assert.Equal(string.Empty, standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_TimesOutOnBlockingBothStreams()
        {
            Process process = CreateProcess(RemotelyInvokable.ReadLine);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();

            Assert.Throws<TimeoutException>(() => process.ReadAllBytes(TimeSpan.FromMilliseconds(100)));

            process.Kill();
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_ThrowsAfterDispose()
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.Start();
            Assert.True(process.WaitForExit(WaitInMS));

            process.Dispose();

            Assert.Throws<ObjectDisposedException>(() => process.ReadAllBytes());
        }
    }
}
