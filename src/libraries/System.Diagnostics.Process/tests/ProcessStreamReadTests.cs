// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Xunit;

namespace System.Diagnostics.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.UapNotUapAot, "https://github.com/dotnet/corefx/issues/22174")]
    public class ProcessStreamReadTests : ProcessTestBase
    {
        [Fact]
        public void TestSyncErrorStream()
        {
            Process p = CreateProcess(ErrorProcessBody);
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            string expected = TestConsoleApp + " started error stream" + Environment.NewLine +
                              TestConsoleApp + " closed error stream" + Environment.NewLine;
            Assert.Equal(expected, p.StandardError.ReadToEnd());
            Assert.True(p.WaitForExit(WaitInMS));
        }

        [Fact]
        public void TestAsyncErrorStream()
        {
            for (int i = 0; i < 2; ++i)
            {
                StringBuilder sb = new StringBuilder();
                Process p = CreateProcess(ErrorProcessBody);
                p.StartInfo.RedirectStandardError = true;
                p.ErrorDataReceived += (s, e) =>
                {
                    sb.Append(e.Data);
                    if (i == 1)
                    {
                        ((Process)s).CancelErrorRead();
                    }
                };
                p.Start();
                p.BeginErrorReadLine();

                Assert.True(p.WaitForExit(WaitInMS));
                p.WaitForExit(); // This ensures async event handlers are finished processing.

                string expected = TestConsoleApp + " started error stream" + (i == 1 ? "" : TestConsoleApp + " closed error stream");
                Assert.Equal(expected, sb.ToString());
            }
        }

        private static int ErrorProcessBody()
        {
            Console.Error.WriteLine(TestConsoleApp + " started error stream");
            Console.Error.WriteLine(TestConsoleApp + " closed error stream");
            return SuccessExitCode;
        }


        [Fact]
        public void TestSyncOutputStream()
        {
            Process p = CreateProcess(StreamBody);
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            string s = p.StandardOutput.ReadToEnd();
            Assert.True(p.WaitForExit(WaitInMS));
            Assert.Equal(TestConsoleApp + " started" + Environment.NewLine + TestConsoleApp + " closed" + Environment.NewLine, s);
        }

        [Fact]
        public void TestAsyncOutputStream()
        {
            for (int i = 0; i < 2; ++i)
            {
                StringBuilder sb = new StringBuilder();
                Process p = CreateProcess(StreamBody);
                p.StartInfo.RedirectStandardOutput = true;
                p.OutputDataReceived += (s, e) =>
                {
                    sb.Append(e.Data);
                    if (i == 1)
                    {
                        ((Process)s).CancelOutputRead();
                    }
                };
                p.Start();
                p.BeginOutputReadLine();
                Assert.True(p.WaitForExit(WaitInMS));
                p.WaitForExit(); // This ensures async event handlers are finished processing.

                string expected = TestConsoleApp + " started" + (i == 1 ? "" : TestConsoleApp + " closed");
                Assert.Equal(expected, sb.ToString());
            }
        }

        private static int StreamBody()
        {
            Console.WriteLine(TestConsoleApp + " started");
            Console.WriteLine(TestConsoleApp + " closed");
            return SuccessExitCode;
        }

        [Fact]
        public void TestSyncStreams()
        {
            const string expected = "This string should come as output";
            Process p = CreateProcess(() =>
            {
                Console.ReadLine();
                return SuccessExitCode;
            });
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += (s, e) => { Assert.Equal(expected, e.Data); };
            p.Start();
            using (StreamWriter writer = p.StandardInput)
            {
                writer.WriteLine(expected);
            }
            Assert.True(p.WaitForExit(WaitInMS));
        }

        [Fact]
        public void TestEOFReceivedWhenStdInClosed()
        {
            // This is the test for the fix of dotnet/corefx issue #13447.
            //
            // Summary of the issue:
            // When an application starts more than one child processes with their standard inputs redirected on Unix,
            // closing the standard input stream of the first child process won't unblock the 'Console.ReadLine()' call
            // in the first child process (it's expected to receive EOF).
            //
            // Root cause of the issue:
            // The file descriptor for the write end of the first child process standard input redirection pipe gets
            // inherited by the second child process, which makes the reference count of the pipe write end become 2.
            // When closing the standard input stream of the first child process, the file descriptor held by the parent
            // process is released, but the one inherited by the second child process is still referencing the pipe
            // write end, which cause the 'Console.ReadLine()' continue to be blocked in the first child process.
            //
            // Fix:
            // Set the O_CLOEXEC flag when creating the redirection pipes. So that no child process would inherit the
            // file descriptors referencing those pipes.
            const string ExpectedLine = "NULL";
            Process p1 = CreateProcess(() =>
            {
                string line = Console.ReadLine();
                Console.WriteLine(line == null ? ExpectedLine : "NOT_" + ExpectedLine);
                return SuccessExitCode;
            });
            Process p2 = CreateProcess(() =>
            {
                Console.ReadLine();
                return SuccessExitCode;
            });

            // Start the first child process
            p1.StartInfo.RedirectStandardInput = true;
            p1.StartInfo.RedirectStandardOutput = true;
            p1.OutputDataReceived += (s, e) => Assert.Equal(ExpectedLine, e.Data);
            p1.Start();

            // Start the second child process
            p2.StartInfo.RedirectStandardInput = true;
            p2.Start();

            try
            {
                // Close the standard input stream of the first child process.
                // The first child process should be unblocked and write out 'NULL', and then exit.
                p1.StandardInput.Close();
                Assert.True(p1.WaitForExit(WaitInMS));
            }
            finally
            {
                // Cleanup: kill the second child process
                p2.Kill();
            }

            // Cleanup
            Assert.True(p2.WaitForExit(WaitInMS));
            p2.Dispose();
            p1.Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "There is 2 bugs in Desktop in this codepath, see: dotnet/corefx #18437 and #18436")]
        public void TestAsyncHalfCharacterAtATime()
        {
            var receivedOutput = false;
            var collectedExceptions = new List<Exception>();

            Process p = CreateProcess(() =>
            {
                var stdout = Console.OpenStandardOutput();
                var bytes = new byte[] { 97, 0 }; //Encoding.Unicode.GetBytes("a");

                for (int i = 0; i != bytes.Length; ++i)
                {
                    stdout.WriteByte(bytes[i]);
                    stdout.Flush();
                    Thread.Sleep(100);
                }
                return SuccessExitCode;
            });
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.StandardOutputEncoding = Encoding.Unicode;
            p.OutputDataReceived += (s, e) =>
            {
                try
                {
                    if (!receivedOutput)
                    {
                        receivedOutput = true;
                        Assert.Equal(e.Data, "a");
                    }
                }
                catch (Exception ex)
                {
                    // This ensures that the exception in event handlers does not break
                    // the whole unittest
                    collectedExceptions.Add(ex);
                }
            };
            p.Start();
            p.BeginOutputReadLine();

            Assert.True(p.WaitForExit(WaitInMS));
            p.WaitForExit(); // This ensures async event handlers are finished processing.

            Assert.True(receivedOutput);

            if (collectedExceptions.Count > 0)
            {
                // Re-throw collected exceptions
                throw new AggregateException(collectedExceptions);
            }
        }

        [Fact]
        public void TestManyOutputLines()
        {
            const int ExpectedLineCount = 144;

            int nonWhitespaceLinesReceived = 0;
            int totalLinesReceived = 0;

            Process p = CreateProcess(() =>
            {
                for (int i = 0; i < ExpectedLineCount; i++)
                {
                    Console.WriteLine("This is line #" + i + ".");
                }
                return SuccessExitCode;
            });
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    nonWhitespaceLinesReceived++;
                }
                totalLinesReceived++;
            };
            p.Start();
            p.BeginOutputReadLine();

            Assert.True(p.WaitForExit(WaitInMS));
            p.WaitForExit(); // This ensures async event handlers are finished processing.

            Assert.Equal(ExpectedLineCount, nonWhitespaceLinesReceived);
            Assert.Equal(ExpectedLineCount + 1, totalLinesReceived);
        }

        [Fact]
        public void TestStreamNegativeTests()
        {
            {
                Process p = new Process();
                Assert.Throws<InvalidOperationException>(() => p.StandardOutput);
                Assert.Throws<InvalidOperationException>(() => p.StandardError);
                Assert.Throws<InvalidOperationException>(() => p.BeginOutputReadLine());
                Assert.Throws<InvalidOperationException>(() => p.BeginErrorReadLine());
                Assert.Throws<InvalidOperationException>(() => p.CancelOutputRead());
                Assert.Throws<InvalidOperationException>(() => p.CancelErrorRead());
            }

            {
                Process p = CreateProcess(StreamBody);
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.OutputDataReceived += (s, e) => {};
                p.ErrorDataReceived += (s, e) => {};

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                Assert.Throws<InvalidOperationException>(() => p.StandardOutput);
                Assert.Throws<InvalidOperationException>(() => p.StandardError);
                Assert.True(p.WaitForExit(WaitInMS));
            }

            {
                Process p = CreateProcess(StreamBody);
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.OutputDataReceived += (s, e) => {};
                p.ErrorDataReceived += (s, e) => {};

                p.Start();

                StreamReader output = p.StandardOutput;
                StreamReader error = p.StandardError;

                Assert.Throws<InvalidOperationException>(() => p.BeginOutputReadLine());
                Assert.Throws<InvalidOperationException>(() => p.BeginErrorReadLine());
                Assert.True(p.WaitForExit(WaitInMS));
            }
        }
    }
}
