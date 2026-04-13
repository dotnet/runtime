// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessMultiplexingTests : ProcessTestBase
    {
        private const string DontPrintAnything = "DO_NOT_PRINT";

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadAll_ThrowsAfterDispose(bool bytes)
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.Start();
            Assert.True(process.WaitForExit(WaitInMS));

            process.Dispose();

            if (bytes)
            {
                Assert.Throws<ObjectDisposedException>(() => process.ReadAllBytes());
            }
            else
            {
                Assert.Throws<ObjectDisposedException>(() => process.ReadAllText());
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadAll_ThrowsWhenNoStreamsRedirected(bool bytes)
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.Start();

            if (bytes)
            {
                Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => process.ReadAllText());
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void ReadAll_ThrowsWhenOnlyOutputOrErrorIsRedirected(bool bytes, bool standardOutput)
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.StartInfo.RedirectStandardOutput = standardOutput;
            process.StartInfo.RedirectStandardError = !standardOutput;
            process.Start();

            if (bytes)
            {
                Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => process.ReadAllText());
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void ReadAll_ThrowsWhenOutputOrErrorIsInSyncMode(bool bytes, bool standardOutput)
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            // Access the StreamReader property to set the stream to sync mode
            _ = standardOutput ? process.StandardOutput : process.StandardError;

            if (bytes)
            {
                Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => process.ReadAllText());
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void ReadAll_ThrowsWhenOutputOrErrorIsInAsyncMode(bool bytes, bool standardOutput)
        {
            Process process = CreateProcess(RemotelyInvokable.StreamBody);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            if (standardOutput)
            {
                process.BeginOutputReadLine();
            }
            else
            {
                process.BeginErrorReadLine();
            }

            if (bytes)
            {
                Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => process.ReadAllText());
            }

            if (standardOutput)
            {
                process.CancelOutputRead();
            }
            else
            {
                process.CancelErrorRead();
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadAll_ThrowsTimeoutExceptionOnTimeout(bool bytes)
        {
            Process process = CreateProcess(RemotelyInvokable.ReadLine);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();

            try
            {
                if (bytes)
                {
                    Assert.Throws<TimeoutException>(() => process.ReadAllBytes(TimeSpan.FromMilliseconds(100)));
                }
                else
                {
                    Assert.Throws<TimeoutException>(() => process.ReadAllText(TimeSpan.FromMilliseconds(100)));
                }
            }
            finally
            {
                process.Kill();
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("hello", "world", true)]
        [InlineData("hello", "world", false)]
        [InlineData("just output", "", true)]
        [InlineData("just output", "", false)]
        [InlineData("", "just error", true)]
        [InlineData("", "just error", false)]
        [InlineData("", "", true)]
        [InlineData("", "", false)]
        public void ReadAll_ReadsBothOutputAndError(string standardOutput, string standardError, bool bytes)
        {
            using Process process = StartPrintingProcess(
                string.IsNullOrEmpty(standardOutput) ? DontPrintAnything : standardOutput,
                string.IsNullOrEmpty(standardError) ? DontPrintAnything : standardError);

            if (bytes)
            {
                (byte[] capturedOutput, byte[] capturedError) = process.ReadAllBytes();

                Assert.Equal(Encoding.Default.GetBytes(standardOutput), capturedOutput);
                Assert.Equal(Encoding.Default.GetBytes(standardError), capturedError);
            }
            else
            {
                (string capturedOutput, string capturedError) = process.ReadAllText();

                Assert.Equal(standardOutput, capturedOutput);
                Assert.Equal(standardError, capturedError);
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllText_ReadsInterleavedOutput()
        {
            const int iterations = 100;
            using Process process = CreateProcess(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    Console.Out.Write($"out{i} ");
                    Console.Out.Flush();
                    Console.Error.Write($"err{i} ");
                    Console.Error.Flush();
                }

                return RemoteExecutor.SuccessExitCode;
            });

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (string standardOutput, string standardError) = process.ReadAllText();

            StringBuilder expectedOutput = new();
            StringBuilder expectedError = new();
            for (int i = 0; i < iterations; i++)
            {
                expectedOutput.Append($"out{i} ");
                expectedError.Append($"err{i} ");
            }

            Assert.Equal(expectedOutput.ToString(), standardOutput);
            Assert.Equal(expectedError.ToString(), standardError);

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_ReadsBinaryDataWithNullBytes()
        {
            string testFilePath = GetTestFilePath();
            byte[] binaryData = new byte[1024];
            Random.Shared.NextBytes(binaryData);
            // Ensure there are null bytes throughout the data.
            for (int i = 0; i < binaryData.Length; i += 10)
            {
                binaryData[i] = 0;
            }

            File.WriteAllBytes(testFilePath, binaryData);

            using Process process = CreateProcess(static path =>
            {
                using FileStream source = File.OpenRead(path);
                source.CopyTo(Console.OpenStandardOutput());

                return RemoteExecutor.SuccessExitCode;
            }, testFilePath);

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (byte[] standardOutput, byte[] standardError) = process.ReadAllBytes();

            Assert.Equal(binaryData, standardOutput);
            Assert.Empty(standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllText_ReadsLargeOutput()
        {
            string testFilePath = GetTestFilePath();
            string largeText = new string('A', 100_000);
            File.WriteAllText(testFilePath, largeText);

            using Process process = CreateProcess(static path =>
            {
                using FileStream source = File.OpenRead(path);
                source.CopyTo(Console.OpenStandardOutput());

                return RemoteExecutor.SuccessExitCode;
            }, testFilePath);

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (string standardOutput, string standardError) = process.ReadAllText();

            Assert.Equal(largeText, standardOutput);
            Assert.Equal(string.Empty, standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ReadAllBytes_ReadsLargeOutput()
        {
            string testFilePath = GetTestFilePath();
            byte[] largeByteArray = new byte[100_000];
            Random.Shared.NextBytes(largeByteArray);
            File.WriteAllBytes(testFilePath, largeByteArray);

            using Process process = CreateProcess(static path =>
            {
                using FileStream source = File.OpenRead(path);
                source.CopyTo(Console.OpenStandardOutput());

                return RemoteExecutor.SuccessExitCode;
            }, testFilePath);

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (byte[] standardOutput, byte[] standardError) = process.ReadAllBytes();

            Assert.Equal(largeByteArray, standardOutput);
            Assert.Empty(standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        private Process StartPrintingProcess(string stdOutText, string stdErrText)
        {
            Process process = CreateProcess((stdOut, stdErr) =>
            {
                if (stdOut != DontPrintAnything)
                {
                    Console.Out.Write(stdOut);
                }

                if (stdErr != DontPrintAnything)
                {
                    Console.Error.Write(stdErr);
                }

                return RemoteExecutor.SuccessExitCode;
            }, stdOutText, stdErrText);

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            return process;
        }
    }
}
