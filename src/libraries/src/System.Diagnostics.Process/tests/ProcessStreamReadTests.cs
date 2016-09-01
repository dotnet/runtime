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
                        Assert.Equal(e.Data, "a");
                    }
                    receivedOutput = true;
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
