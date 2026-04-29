// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessStreamingTests : ProcessTestBase
    {
        private const string DontPrintAnything = "DO_NOT_PRINT";

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_ThrowsAfterDispose(bool useAsync)
        {
            using Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.Start();
            Assert.True(process.WaitForExit(WaitInMS));

            process.Dispose();

            if (useAsync)
            {
                await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                {
                    await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                    {
                    }
                });
            }
            else
            {
                Assert.Throws<ObjectDisposedException>(() =>
                {
                    foreach (ProcessOutputLine line in process.ReadAllLines())
                    {
                    }
                });
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_ThrowsWhenNoStreamsRedirected(bool useAsync)
        {
            using Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.Start();

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                    {
                    }
                });
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    foreach (ProcessOutputLine line in process.ReadAllLines())
                    {
                    }
                });
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task ReadAllLines_ThrowsWhenOnlyOutputOrErrorIsRedirected(bool standardOutput, bool useAsync)
        {
            using Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.StartInfo.RedirectStandardOutput = standardOutput;
            process.StartInfo.RedirectStandardError = !standardOutput;
            process.Start();

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                    {
                    }
                });
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    foreach (ProcessOutputLine line in process.ReadAllLines())
                    {
                    }
                });
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task ReadAllLines_ThrowsWhenOutputOrErrorIsInSyncMode(bool standardOutput, bool useAsync)
        {
            using Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            // Access the StreamReader property to set the stream to sync mode
            _ = standardOutput ? process.StandardOutput : process.StandardError;

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                    {
                    }
                });
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    foreach (ProcessOutputLine line in process.ReadAllLines())
                    {
                    }
                });
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task ReadAllLines_ThrowsWhenOutputOrErrorIsInAsyncMode(bool standardOutput, bool useAsync)
        {
            using Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
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

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                    {
                    }
                });
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    foreach (ProcessOutputLine line in process.ReadAllLines())
                    {
                    }
                });
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
        [InlineData("hello", "world", true)]
        [InlineData("hello", "world", false)]
        [InlineData("just output", "", true)]
        [InlineData("just output", "", false)]
        [InlineData("", "just error", true)]
        [InlineData("", "just error", false)]
        [InlineData("", "", true)]
        [InlineData("", "", false)]
        public async Task ReadAllLines_ReadsBothOutputAndError(string standardOutput, string standardError, bool useAsync)
        {
            using Process process = StartLinePrintingProcess(
                string.IsNullOrEmpty(standardOutput) ? DontPrintAnything : standardOutput,
                string.IsNullOrEmpty(standardError) ? DontPrintAnything : standardError);

            (List<string> capturedOutput, List<string> capturedError) = await EnumerateLines(process, useAsync);

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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_ReadsInterleavedOutput(bool useAsync)
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

            (List<string> capturedOutput, List<string> capturedError) = await EnumerateLines(process, useAsync);

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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_ReadsLargeOutput(bool useAsync)
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

            (List<string> capturedOutput, List<string> capturedError) = await EnumerateLines(process, useAsync);

            Assert.Equal(lineCount, capturedOutput.Count);
            for (int i = 0; i < lineCount; i++)
            {
                Assert.Equal($"line{i}", capturedOutput[i]);
            }

            Assert.Empty(capturedError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_ReadsVeryLongLines(bool useAsync)
        {
            const int lineLength = 8192;
            const int lineCount = 3;
            using Process process = CreateProcess(() =>
            {
                for (int i = 0; i < lineCount; i++)
                {
                    Console.Out.WriteLine(new string((char)('A' + i), lineLength));
                    Console.Out.Flush();
                    Console.Error.WriteLine(new string((char)('a' + i), lineLength));
                    Console.Error.Flush();
                }

                return RemoteExecutor.SuccessExitCode;
            });

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (List<string> capturedOutput, List<string> capturedError) = await EnumerateLines(process, useAsync);

            Assert.Equal(lineCount, capturedOutput.Count);
            Assert.Equal(lineCount, capturedError.Count);

            for (int i = 0; i < lineCount; i++)
            {
                Assert.Equal(new string((char)('A' + i), lineLength), capturedOutput[i]);
                Assert.Equal(new string((char)('a' + i), lineLength), capturedError[i]);
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_ThrowsOnCancellationOrTimeout(bool useAsync)
        {
            using Process process = CreateProcess(RemotelyInvokable.ReadLine);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();

            try
            {
                if (useAsync)
                {
                    using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(100));

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    {
                        await foreach (ProcessOutputLine line in process.ReadAllLinesAsync(cts.Token))
                        {
                        }
                    });
                }
                else
                {
                    Assert.Throws<TimeoutException>(() =>
                    {
                        foreach (ProcessOutputLine line in process.ReadAllLines(TimeSpan.FromMilliseconds(100)))
                        {
                        }
                    });
                }
            }
            finally
            {
                process.Kill();
            }

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_ProcessOutputLineProperties(bool useAsync)
        {
            using Process process = StartLinePrintingProcess("stdout_line", "stderr_line");

            (List<string> capturedOutput, List<string> capturedError) = await EnumerateLines(process, useAsync);

            Assert.Single(capturedOutput, line => line == "stdout_line");
            Assert.Single(capturedError, line => line == "stderr_line");

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_StopsCleanlyWhenConsumerBreaksEarly(bool useAsync)
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

            if (useAsync)
            {
                await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
                {
                    firstLine = line;
                    break;
                }
            }
            else
            {
                foreach (ProcessOutputLine line in process.ReadAllLines())
                {
                    firstLine = line;
                    break;
                }
            }

            Assert.NotNull(firstLine);
            Assert.NotNull(firstLine.Value.Content);

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("utf-8", true)]
        [InlineData("utf-8", false)]
        [InlineData("utf-16", true)]
        [InlineData("utf-16", false)]
        [InlineData("utf-32", true)]
        [InlineData("utf-32", false)]
        public async Task ReadAllLines_WorksWithNonDefaultEncodings(string encodingName, bool useAsync)
        {
            Encoding encoding = Encoding.GetEncoding(encodingName);

            using Process process = CreateProcess(static (string encodingArg) =>
            {
                Encoding enc = Encoding.GetEncoding(encodingArg);
                using (StreamWriter outputWriter = new(Console.OpenStandardOutput(), enc))
                {
                    outputWriter.WriteLine("stdout_line");
                }

                using (StreamWriter errorWriter = new(Console.OpenStandardError(), enc))
                {
                    errorWriter.WriteLine("stderr_line");
                }

                return RemoteExecutor.SuccessExitCode;
            }, encodingName);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = encoding;
            process.StartInfo.StandardErrorEncoding = encoding;
            process.Start();

            (List<string> capturedOutput, List<string> capturedError) = await EnumerateLines(process, useAsync);

            Assert.Equal(new[] { "stdout_line" }, capturedOutput);
            Assert.Equal(new[] { "stderr_line" }, capturedError);

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("utf-8", true)]
        [InlineData("utf-8", false)]
        [InlineData("utf-16", true)]
        [InlineData("utf-16", false)]
        [InlineData("utf-32", true)]
        [InlineData("utf-32", false)]
        public async Task ReadAllLines_WorksWithMultiByteCharacters(string encodingName, bool useAsync)
        {
            Encoding encoding = Encoding.GetEncoding(encodingName);

            using Process process = CreateProcess(static (string encodingArg) =>
            {
                Encoding enc = Encoding.GetEncoding(encodingArg);
                // Write raw encoded bytes split at the midpoint of the byte array so the split
                // lands inside a multi-byte character, exercising decoder state across reads.
                // CJK chars (U+4E16 U+754C = "世界"): 3 bytes each in UTF-8, 2 in UTF-16, 4 in UTF-32.
                byte[] outBytes = enc.GetBytes("hello_\u4e16\u754c_stdout\n");
                int outSplit = outBytes.Length / 2;
                Stream stdout = Console.OpenStandardOutput();
                stdout.Write(outBytes, 0, outSplit);
                stdout.Flush();
                stdout.Write(outBytes, outSplit, outBytes.Length - outSplit);
                stdout.Flush();

                byte[] errBytes = enc.GetBytes("hello_\u4e16\u754c_stderr\n");
                int errSplit = errBytes.Length / 2;
                Stream stderr = Console.OpenStandardError();
                stderr.Write(errBytes, 0, errSplit);
                stderr.Flush();
                stderr.Write(errBytes, errSplit, errBytes.Length - errSplit);
                stderr.Flush();

                return RemoteExecutor.SuccessExitCode;
            }, encodingName);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = encoding;
            process.StartInfo.StandardErrorEncoding = encoding;
            process.Start();

            (List<string> capturedOutput, List<string> capturedError) = await EnumerateLines(process, useAsync);

            Assert.Equal(new[] { "hello_\u4e16\u754c_stdout" }, capturedOutput);
            Assert.Equal(new[] { "hello_\u4e16\u754c_stderr" }, capturedError);

            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_HandlesMixedLineEndings(bool useAsync)
        {
            using Process process = CreateProcess(static () =>
            {
                // Write stdout with all three line-terminator styles in one stream:
                // \r\n (Windows), \n (Unix), bare \r (classic Mac), and a final chunk with no terminator.
                Stream stdout = Console.OpenStandardOutput();
                byte[] data = Encoding.UTF8.GetBytes("lineA\r\nlineB\nlineC\rlineD");
                stdout.Write(data);
                stdout.Flush();
                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            (List<string> capturedOutput, List<string> capturedError) = await EnumerateLines(process, useAsync);

            Assert.Equal(new[] { "lineA", "lineB", "lineC", "lineD" }, capturedOutput);
            Assert.Empty(capturedError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_HandlesPartialBomAcrossReads(bool useAsync)
        {
            // Write a UTF-32 LE BOM (FF FE 00 00) as two separate flushed writes so the
            // first read can deliver only the first two BOM bytes. Without BOM accumulation,
            // FF FE would be misclassified as a UTF-16 LE BOM and the content would be
            // decoded with the wrong encoding.
            using Process process = CreateProcess(static () =>
            {
                Stream stdout = Console.OpenStandardOutput();
                stdout.Write([0xFF, 0xFE]); // First half of UTF-32 LE BOM
                stdout.Flush();
                stdout.Write([0x00, 0x00]); // Second half of BOM
                stdout.Write(Encoding.UTF32.GetBytes("hello\n")); // Content (no BOM from GetBytes)
                stdout.Flush();
                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF32;
            process.Start();

            (List<string> capturedOutput, List<string> capturedError) = await EnumerateLines(process, useAsync);

            Assert.Equal(new[] { "hello" }, capturedOutput);
            Assert.Empty(capturedError);
            Assert.True(process.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAllLines_LessThanFourBytes(bool useAsync)
        {
            using Process process = CreateProcess(static () =>
            {
                Stream stdout = Console.OpenStandardOutput();
                stdout.Write([(byte)'h']);
                stdout.Flush();
                stdout.Write([(byte)'i']);
                stdout.Flush();

                Stream error = Console.OpenStandardError();
                error.Write([(byte)'b']);
                error.Flush();
                error.Write([(byte)'y']);
                error.Flush();
                error.Write([(byte)'e']);
                error.Flush();

                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.Start();

            (List<string> capturedOutput, List<string> capturedError) = await EnumerateLines(process, useAsync);

            Assert.Equal(new[] { "hi" }, capturedOutput);
            Assert.Equal(new[] { "bye" }, capturedError);
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

        /// <summary>
        /// Helper that wraps both the sync and async line-reading APIs and returns
        /// the captured output and error lines.
        /// </summary>
        private static async Task<(List<string> capturedOutput, List<string> capturedError)> EnumerateLines(Process process, bool useAsync)
        {
            List<string> capturedOutput = new();
            List<string> capturedError = new();

            if (useAsync)
            {
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
            }
            else
            {
                foreach (ProcessOutputLine line in process.ReadAllLines())
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
            }

            return (capturedOutput, capturedError);
        }
    }
}
