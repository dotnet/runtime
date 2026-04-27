// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessStreamingTests : ProcessTestBase
    {
        private const string DontPrintAnything = "DO_NOT_PRINT";

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ReadAllLinesAsync_ThrowsAfterDispose()
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.Start();
            Assert.True(process.WaitForExit(WaitInMS));

            process.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            {
                await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                {
                }
            });
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ReadAllLinesAsync_ThrowsWhenNoStreamsRedirected()
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.Start();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                {
                }
            });

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLinesAsync_ThrowsWhenOnlyOutputOrErrorIsRedirected(bool standardOutput)
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.StartInfo.RedirectStandardOutput = standardOutput;
            process.StartInfo.RedirectStandardError = !standardOutput;
            process.Start();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                {
                }
            });

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLinesAsync_ThrowsWhenOutputOrErrorIsInSyncMode(bool standardOutput)
        {
            Process process = CreateProcess(RemotelyInvokable.Dummy);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            // Access the StreamReader property to set the stream to sync mode
            _ = standardOutput ? process.StandardOutput : process.StandardError;

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                {
                }
            });

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLinesAsync_ThrowsWhenOutputOrErrorIsInAsyncMode(bool standardOutput)
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

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                {
                }
            });

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
        [InlineData("hello", "world")]
        [InlineData("just output", "")]
        [InlineData("", "just error")]
        [InlineData("", "")]
        public async Task ReadAllLinesAsync_ReadsBothOutputAndError(string standardOutput, string standardError)
        {
            using Process process = StartLinePrintingProcess(
                string.IsNullOrEmpty(standardOutput) ? DontPrintAnything : standardOutput,
                string.IsNullOrEmpty(standardError) ? DontPrintAnything : standardError);

            List<string> capturedOutput = new();
            List<string> capturedError = new();

            await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
            {
                if (line.StandardError)
                {
                    capturedError.Add(line.Content);
                }
                else
                {
                    capturedOutput.Add(line.Content);
                }
            }

            if (string.IsNullOrEmpty(standardOutput))
            {
                Assert.Empty(capturedOutput);
            }
            else
            {
                Assert.Equal(new[] { standardOutput }, capturedOutput);
            }

            if (string.IsNullOrEmpty(standardError))
            {
                Assert.Empty(capturedError);
            }
            else
            {
                Assert.Equal(new[] { standardError }, capturedError);
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ReadAllLinesAsync_ReadsInterleavedOutput()
        {
            const int iterations = 100;
            using Process process = CreateProcess(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    Console.Out.WriteLine($"out{i}");
                    Console.Out.Flush();
                    Console.Error.WriteLine($"err{i}");
                    Console.Error.Flush();
                }

                return RemoteExecutor.SuccessExitCode;
            });

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            List<string> capturedOutput = new();
            List<string> capturedError = new();

            await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
            {
                if (line.StandardError)
                {
                    capturedError.Add(line.Content);
                }
                else
                {
                    capturedOutput.Add(line.Content);
                }
            }

            List<string> expectedOutput = new();
            List<string> expectedError = new();
            for (int i = 0; i < iterations; i++)
            {
                expectedOutput.Add($"out{i}");
                expectedError.Add($"err{i}");
            }

            Assert.Equal(expectedOutput, capturedOutput);
            Assert.Equal(expectedError, capturedError);

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ReadAllLinesAsync_ReadsLargeOutput()
        {
            const int lineCount = 1000;
            using Process process = CreateProcess(() =>
            {
                for (int i = 0; i < lineCount; i++)
                {
                    Console.Out.WriteLine($"line{i}");
                }

                return RemoteExecutor.SuccessExitCode;
            });

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            List<string> capturedOutput = new();
            List<string> capturedError = new();

            await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
            {
                if (line.StandardError)
                {
                    capturedError.Add(line.Content);
                }
                else
                {
                    capturedOutput.Add(line.Content);
                }
            }

            for (int i = 0; i < lineCount; i++)
            {
                Assert.Equal($"line{i}", capturedOutput[i]);
            }

            Assert.Empty(capturedError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ReadAllLinesAsync_ThrowsOperationCanceledOnCancellation()
        {
            Process process = CreateProcess(RemotelyInvokable.ReadLine);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();

            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(100));

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await foreach (ProcessOutputLine line in process.ReadAllLinesAsync(cts.Token))
                    {
                    }
                });
            }
            finally
            {
                process.Kill();
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ReadAllLinesAsync_ProcessOutputLineProperties()
        {
            using Process process = StartLinePrintingProcess("stdout_line", "stderr_line");

            List<ProcessOutputLine> allLines = new();

            await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
            {
                allLines.Add(line);
            }

            Assert.Single(allLines, line => line.Content == "stdout_line" && !line.StandardError);
            Assert.Single(allLines, line => line.Content == "stderr_line" && line.StandardError);

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ReadAllLinesAsync_StopsCleanlyWhenConsumerBreaksEarly()
        {
            using Process process = CreateProcess(() =>
            {
                Console.Out.WriteLine("first");
                Console.Out.Flush();
                Console.Out.WriteLine("second");
                Console.Out.Flush();
                Console.Error.WriteLine("error1");
                Console.Error.Flush();

                return RemoteExecutor.SuccessExitCode;
            });

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            ProcessOutputLine? firstLine = null;

            await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
            {
                firstLine = line;
                break; // stop after first line
            }

            Assert.NotNull(firstLine);
            Assert.NotNull(firstLine.Value.Content);

            Assert.True(process.WaitForExit(WaitInMS));
        }

        private Process StartLinePrintingProcess(string stdOutText, string stdErrText)
        {
            Process process = CreateProcess((stdOut, stdErr) =>
            {
                if (stdOut != DontPrintAnything)
                {
                    Console.Out.WriteLine(stdOut);
                }

                if (stdErr != DontPrintAnything)
                {
                    Console.Error.WriteLine(stdErr);
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
