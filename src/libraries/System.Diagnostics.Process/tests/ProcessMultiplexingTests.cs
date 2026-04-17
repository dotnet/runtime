// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessMultiplexingTests : ProcessTestBase
    {
        private const string DontPrintAnything = "DO_NOT_PRINT";

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        public async Task ReadAll_ThrowsAfterDispose(bool bytes, bool useAsync)
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.Start();
            Assert.True(process.WaitForExit(WaitInMS));

            process.Dispose();

            if (bytes)
            {
                if (useAsync)
                {
                    await Assert.ThrowsAsync<ObjectDisposedException>(() => process.ReadAllBytesAsync());
                }
                else
                {
                    Assert.Throws<ObjectDisposedException>(() => process.ReadAllBytes());
                }
            }
            else
            {
                if (useAsync)
                {
                    await Assert.ThrowsAsync<ObjectDisposedException>(() => process.ReadAllTextAsync());
                }
                else
                {
                    Assert.Throws<ObjectDisposedException>(() => process.ReadAllText());
                }
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        public async Task ReadAll_ThrowsWhenNoStreamsRedirected(bool bytes, bool useAsync)
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.Start();

            if (bytes)
            {
                if (useAsync)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => process.ReadAllBytesAsync());
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());
                }
            }
            else
            {
                if (useAsync)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => process.ReadAllTextAsync());
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => process.ReadAllText());
                }
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true, false)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, true)]
        public async Task ReadAll_ThrowsWhenOnlyOutputOrErrorIsRedirected(bool bytes, bool standardOutput, bool useAsync)
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.StartInfo.RedirectStandardOutput = standardOutput;
            process.StartInfo.RedirectStandardError = !standardOutput;
            process.Start();

            if (bytes)
            {
                if (useAsync)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => process.ReadAllBytesAsync());
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());
                }
            }
            else
            {
                if (useAsync)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => process.ReadAllTextAsync());
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => process.ReadAllText());
                }
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true, false)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, true)]
        public async Task ReadAll_ThrowsWhenOutputOrErrorIsInSyncMode(bool bytes, bool standardOutput, bool useAsync)
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            // Access the StreamReader property to set the stream to sync mode
            _ = standardOutput ? process.StandardOutput : process.StandardError;

            if (bytes)
            {
                if (useAsync)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => process.ReadAllBytesAsync());
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());
                }
            }
            else
            {
                if (useAsync)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => process.ReadAllTextAsync());
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => process.ReadAllText());
                }
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true, false)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, true)]
        public async Task ReadAll_ThrowsWhenOutputOrErrorIsInAsyncMode(bool bytes, bool standardOutput, bool useAsync)
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
                if (useAsync)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => process.ReadAllBytesAsync());
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => process.ReadAllBytes());
                }
            }
            else
            {
                if (useAsync)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => process.ReadAllTextAsync());
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => process.ReadAllText());
                }
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
        [InlineData("hello", "world", true, false)]
        [InlineData("hello", "world", false, false)]
        [InlineData("just output", "", true, false)]
        [InlineData("just output", "", false, false)]
        [InlineData("", "just error", true, false)]
        [InlineData("", "just error", false, false)]
        [InlineData("", "", true, false)]
        [InlineData("", "", false, false)]
        [InlineData("hello", "world", true, true)]
        [InlineData("hello", "world", false, true)]
        [InlineData("just output", "", true, true)]
        [InlineData("just output", "", false, true)]
        [InlineData("", "just error", true, true)]
        [InlineData("", "just error", false, true)]
        [InlineData("", "", true, true)]
        [InlineData("", "", false, true)]
        public async Task ReadAll_ReadsBothOutputAndError(string standardOutput, string standardError, bool bytes, bool useAsync)
        {
            using Process process = StartPrintingProcess(
                string.IsNullOrEmpty(standardOutput) ? DontPrintAnything : standardOutput,
                string.IsNullOrEmpty(standardError) ? DontPrintAnything : standardError);

            if (bytes)
            {
                (byte[] capturedOutput, byte[] capturedError) = useAsync
                    ? await process.ReadAllBytesAsync()
                    : process.ReadAllBytes();

                Assert.Equal(Encoding.Default.GetBytes(standardOutput), capturedOutput);
                Assert.Equal(Encoding.Default.GetBytes(standardError), capturedError);
            }
            else
            {
                (string capturedOutput, string capturedError) = useAsync
                    ? await process.ReadAllTextAsync()
                    : process.ReadAllText();

                Assert.Equal(standardOutput, capturedOutput);
                Assert.Equal(standardError, capturedError);
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadAllText_ReadsInterleavedOutput(bool useAsync)
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

            (string standardOutput, string standardError) = useAsync
                ? await process.ReadAllTextAsync()
                : process.ReadAllText();

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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadAllBytes_ReadsBinaryDataWithNullBytes(bool useAsync)
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

            (byte[] standardOutput, byte[] standardError) = useAsync
                ? await process.ReadAllBytesAsync()
                : process.ReadAllBytes();

            Assert.Equal(binaryData, standardOutput);
            Assert.Empty(standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadAllText_ReadsLargeOutput(bool useAsync)
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

            (string standardOutput, string standardError) = useAsync
                ? await process.ReadAllTextAsync()
                : process.ReadAllText();

            Assert.Equal(largeText, standardOutput);
            Assert.Equal(string.Empty, standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadAllBytes_ReadsLargeOutput(bool useAsync)
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

            (byte[] standardOutput, byte[] standardError) = useAsync
                ? await process.ReadAllBytesAsync()
                : process.ReadAllBytes();

            Assert.Equal(largeByteArray, standardOutput);
            Assert.Empty(standardError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task ReadAllAsync_ThrowsAllAvailableExceptions(bool multiple, bool bytes)
        {
            using Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            try
            {
                // Close the underlying pipe streams to force both async reads to fail.
                // We access the internal fields directly via reflection to avoid going through
                // the StandardOutput/StandardError properties, which would set the StreamReadMode
                // and prevent ReadAllBytesAsync from being called.
                FieldInfo stdoutField = typeof(Process).GetField("_standardOutput", BindingFlags.NonPublic | BindingFlags.Instance)!;
                FieldInfo stderrField = typeof(Process).GetField("_standardError", BindingFlags.NonPublic | BindingFlags.Instance)!;

                StreamReader stdoutReader = (StreamReader)stdoutField.GetValue(process)!;
                StreamReader stderrReader = (StreamReader)stderrField.GetValue(process)!;

                stdoutReader.BaseStream.Dispose();

                if (multiple)
                {
                    stderrReader.BaseStream.Dispose();

                    AggregateException aggregate = await Assert.ThrowsAsync<AggregateException>(() => bytes ? process.ReadAllBytesAsync() : process.ReadAllTextAsync());
                    Assert.Equal(2, aggregate.InnerExceptions.Count);
                    Assert.All(aggregate.InnerExceptions, ex => Assert.IsType<ObjectDisposedException>(ex));
                }
                else
                {
                    await Assert.ThrowsAsync<ObjectDisposedException>(() => bytes ? process.ReadAllBytesAsync() : process.ReadAllTextAsync());
                }
            }
            finally
            {
                process.Kill();
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllAsync_ThrowsOperationCanceledOnCancellation(bool bytes)
        {
            Process process = CreateProcess(RemotelyInvokable.ReadLine);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();

            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(100));

                if (bytes)
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => process.ReadAllBytesAsync(cts.Token));
                }
                else
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => process.ReadAllTextAsync(cts.Token));
                }
            }
            finally
            {
                process.Kill();
            }

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
