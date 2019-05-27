// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessStreamReadTests : ProcessTestBase
    {
        [Fact]
        public void TestSyncErrorStream()
        {
            Process p = CreateProcessPortable(RemotelyInvokable.ErrorProcessBody);
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            string expected = RemotelyInvokable.TestConsoleApp + " started error stream" + Environment.NewLine +
                              RemotelyInvokable.TestConsoleApp + " closed error stream" + Environment.NewLine;
            Assert.Equal(expected, p.StandardError.ReadToEnd());
            Assert.True(p.WaitForExit(WaitInMS));
        }

        [Fact]
        public void TestAsyncErrorStream()
        {
            for (int i = 0; i < 2; ++i)
            {
                StringBuilder sb = new StringBuilder();
                Process p = CreateProcessPortable(RemotelyInvokable.ErrorProcessBody);
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

                string expected = RemotelyInvokable.TestConsoleApp + " started error stream" + (i == 1 ? "" : RemotelyInvokable.TestConsoleApp + " closed error stream");
                Assert.Equal(expected, sb.ToString());
            }
        }

        [Fact]
        public void TestSyncOutputStream()
        {
            Process p = CreateProcessPortable(RemotelyInvokable.StreamBody);
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            string s = p.StandardOutput.ReadToEnd();
            Assert.True(p.WaitForExit(WaitInMS));
            Assert.Equal(RemotelyInvokable.TestConsoleApp + " started" + Environment.NewLine + RemotelyInvokable.TestConsoleApp + " closed" + Environment.NewLine, s);
        }

        [Fact]
        public void TestAsyncOutputStream()
        {
            for (int i = 0; i < 2; ++i)
            {
                StringBuilder sb = new StringBuilder();
                Process p = CreateProcess(RemotelyInvokable.StreamBody);
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

                string expected = RemotelyInvokable.TestConsoleApp + " started" + (i == 1 ? "" : RemotelyInvokable.TestConsoleApp + " closed");
                Assert.Equal(expected, sb.ToString());
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Pipe doesn't work well on UAP")]
        async public Task TestAsyncOutputStream_CancelOutputRead()
        {
            // This test might have some false negatives due to possible race condition in System.Diagnostics.AsyncStreamReader.ReadBufferAsync
            // There is not way to know if parent process has processed async output from child process
            
            using (AnonymousPipeServerStream pipeWrite = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            using (AnonymousPipeServerStream pipeRead = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
            {
                using (Process p = CreateProcess(TestAsyncOutputStream_CancelOutputRead_RemotelyInvokable, $"{pipeWrite.GetClientHandleAsString()} {pipeRead.GetClientHandleAsString()}"))
                {
                    var dataReceived = new List<int>();
                    var dataArrivedEvent = new AutoResetEvent(false);

                    p.StartInfo.RedirectStandardOutput = true;
                    p.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                        {
                            dataReceived.Add(int.Parse(e.Data));
                        }
                        dataArrivedEvent.Set();
                    };

                    // Start child process
                    p.Start();

                    pipeWrite.DisposeLocalCopyOfClientHandle();
                    pipeRead.DisposeLocalCopyOfClientHandle();

                    // Wait child process start
                    Assert.True(await WaitPipeSignal(pipeRead, WaitInMS), "Child process not started");

                    //Start listening and produce output 1
                    p.BeginOutputReadLine();
                    await pipeWrite.WriteAsync(new byte[1], 0, 1);

                    // Wait child signal produce number 1
                    Assert.True(await WaitPipeSignal(pipeRead, WaitInMS), "Missing child signal for value 1");
                    Assert.True(dataArrivedEvent.WaitOne(WaitInMS), "Value 1 not received");

                    // Stop listening and signal to produce value 2
                    p.CancelOutputRead();
                    await pipeWrite.WriteAsync(new byte[1], 0, 1);

                    // Wait child signal produce number 2
                    Assert.True(await WaitPipeSignal(pipeRead, WaitInMS), "Missing child signal for value 2");

                    // Wait child process close to be sure that output buffer has been flushed
                    Assert.True(p.WaitForExit(WaitInMS), "Child process didn't close");

                    Assert.Equal(1, dataReceived.Count);
                    Assert.Equal(1, dataReceived[0]);
                }
            }
        }

        async private Task<int> TestAsyncOutputStream_CancelOutputRead_RemotelyInvokable(string pipesHandle)
        {
            string[] pipeHandlers = pipesHandle.Split(' ');
            using (AnonymousPipeClientStream pipeRead = new AnonymousPipeClientStream(PipeDirection.In, pipeHandlers[0]))
            using (AnonymousPipeClientStream pipeWrite = new AnonymousPipeClientStream(PipeDirection.Out, pipeHandlers[1]))
            {
                // Signal child process start
                await pipeWrite.WriteAsync(new byte[1], 0, 1);

                // Wait parent signal to produce number 1
                // Generate output 1 and signal parent
                Assert.True(await WaitPipeSignal(pipeRead, WaitInMS), "Missing parent signal to produce number 1");
                Console.WriteLine(1);
                await pipeWrite.WriteAsync(new byte[1], 0, 1);

                // Wait parent signal to produce number 2
                // Generate output 2 and signal parent
                Assert.True(await WaitPipeSignal(pipeRead, WaitInMS), "Missing parent signal to produce number 2");
                Console.WriteLine(2);
                await pipeWrite.WriteAsync(new byte[1], 0, 1);

                return RemoteExecutor.SuccessExitCode;
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Pipe doesn't work well on UAP")]
        async public Task TestAsyncOutputStream_BeginCancelBeginOutputRead()
        {
            using (AnonymousPipeServerStream pipeWrite = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            using (AnonymousPipeServerStream pipeRead = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
            {
                using (Process p = CreateProcess(TestAsyncOutputStream_BeginCancelBeinOutputRead_RemotelyInvokable, $"{pipeWrite.GetClientHandleAsString()} {pipeRead.GetClientHandleAsString()}"))
                {
                    var dataReceived = new BlockingCollection<int>();

                    p.StartInfo.RedirectStandardOutput = true;
                    p.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                        {
                            dataReceived.Add(int.Parse(e.Data));
                        }
                    };

                    // Start child process
                    p.Start();

                    pipeWrite.DisposeLocalCopyOfClientHandle();
                    pipeRead.DisposeLocalCopyOfClientHandle();

                    // Wait child process start
                    Assert.True(await WaitPipeSignal(pipeRead, WaitInMS), "Child process not started");

                    //Start listening and signal client to produce 1,2,3
                    p.BeginOutputReadLine();
                    await pipeWrite.WriteAsync(new byte[1], 0, 1);

                    // Wait child signal produce number 1,2,3
                    Assert.True(await WaitPipeSignal(pipeRead, WaitInMS), "Missing child signal for value 1,2,3");
                    using (CancellationTokenSource cts = new CancellationTokenSource(WaitInMS))
                    {
                        try
                        {
                            List<int> expectedValue123 = new List<int>() { 1, 2, 3 };
                            foreach (int value in dataReceived.GetConsumingEnumerable(cts.Token)) 
                            {
                                expectedValue123.Remove(value);
                                if (expectedValue123.Count == 0)
                                {
                                    break;
                                }
                            }
                        }
                        catch(OperationCanceledException)
                        {
                            Assert.False(cts.IsCancellationRequested, "Values 1,2,3 not arrived");
                        }
                    }
                    
                    // Cancel and signal child
                    p.CancelOutputRead();
                    await pipeWrite.WriteAsync(new byte[1], 0, 1);

                    // Re-start listening and signal child
                    p.BeginOutputReadLine();
                    await pipeWrite.WriteAsync(new byte[1], 0, 1);

                    // Wait child process close
                    Assert.True(p.WaitForExit(WaitInMS), "Child process didn't close");

                    // Wait for value 7,8,9
                    using (CancellationTokenSource cts = new CancellationTokenSource(WaitInMS))
                    {
                        try
                        {
                            List<int> expectedValue789 = new List<int>() { 7, 8, 9 };
                            foreach (int value in dataReceived.GetConsumingEnumerable(cts.Token))
                            {
                                expectedValue789.Remove(value);
                                if (expectedValue789.Count == 0)
                                {
                                    break;
                                }
                            }
                        }
                        catch(OperationCanceledException)
                        {
                            Assert.False(cts.IsCancellationRequested, "Values 7,8,9 not arrived");
                        }
                    }
                }
            }
        }

        async private Task<int> TestAsyncOutputStream_BeginCancelBeinOutputRead_RemotelyInvokable(string pipesHandle)
        {
            string[] pipeHandlers = pipesHandle.Split(' ');
            using (AnonymousPipeClientStream pipeRead = new AnonymousPipeClientStream(PipeDirection.In, pipeHandlers[0]))
            using (AnonymousPipeClientStream pipeWrite = new AnonymousPipeClientStream(PipeDirection.Out, pipeHandlers[1]))
            {
                // Signal child process start
                await pipeWrite.WriteAsync(new byte[1], 0, 1);

                // Wait parent signal to produce number 1,2,3
                Assert.True(await WaitPipeSignal(pipeRead, WaitInMS), "Missing parent signal to produce number 1,2,3");
                Console.WriteLine(1);
                Console.WriteLine(2);
                Console.WriteLine(3);
                await pipeWrite.WriteAsync(new byte[1], 0, 1);

                // Wait parent cancellation signal and produce new values 4,5,6
                Assert.True(await WaitPipeSignal(pipeRead, WaitInMS), "Missing parent signal to produce number 4,5,6");
                Console.WriteLine(4);
                Console.WriteLine(5);
                Console.WriteLine(6);

                // Wait parent re-start listening signal and produce 7,8,9
                Assert.True(await WaitPipeSignal(pipeRead, WaitInMS), "Missing parent re-start listening signal");
                Console.WriteLine(7);
                Console.WriteLine(8);
                Console.WriteLine(9);

                return RemoteExecutor.SuccessExitCode;
            }
        }

        async private Task<bool> WaitPipeSignal(PipeStream pipe, int millisecond)
        {
            using (var cts = new CancellationTokenSource(millisecond))
            {
                try
                {
                    await pipe.ReadAsync(new byte[1], 0, 1, cts.Token);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }
        }

        [Fact]
        public void TestSyncStreams()
        {
            const string expected = "This string should come as output";
            Process p = CreateProcessPortable(RemotelyInvokable.ReadLine);
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
            Process p1 = CreateProcessPortable(RemotelyInvokable.ReadLineWriteIfNull);
            Process p2 = CreateProcessPortable(RemotelyInvokable.ReadLine);

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
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "No simple way to perform this on uap using cmd.exe")]
        public void TestAsyncHalfCharacterAtATime()
        {
            var receivedOutput = false;
            var collectedExceptions = new List<Exception>();

            Process p = CreateProcessPortable(RemotelyInvokable.WriteSlowlyByByte);
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.StandardOutputEncoding = Encoding.Unicode;
            p.OutputDataReceived += (s, e) =>
            {
                try
                {
                    if (!receivedOutput)
                    {
                        receivedOutput = true;
                        Assert.Equal("a", e.Data);
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

            Process p = CreateProcessPortable(RemotelyInvokable.Write144Lines);
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
        public void TestClosingStreamsAsyncDoesNotThrow()
        {
            Process p = CreateProcessPortable(RemotelyInvokable.WriteLinesAfterClose);
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            // On netfx, the handler is called once with the Data as null, even if the process writes nothing to the pipe.
            // That behavior is documented here https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.datareceivedeventhandler

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.Close();
            RemotelyInvokable.FireClosedEvent();
        }

        [Fact]
        public void TestClosingStreamsUndefinedDoesNotThrow()
        {
            Process p = CreateProcessPortable(RemotelyInvokable.WriteLinesAfterClose);
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            p.Start();
            p.Close();
            RemotelyInvokable.FireClosedEvent();
        }

        [Fact]
        public void TestClosingSyncModeDoesNotCloseStreams()
        {
            Process p = CreateProcessPortable(RemotelyInvokable.WriteLinesAfterClose);
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            p.Start();

            var output = p.StandardOutput;
            var error = p.StandardError;

            p.Close();
            RemotelyInvokable.FireClosedEvent();

            output.ReadToEnd();
            error.ReadToEnd();
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
                Process p = CreateProcessPortable(RemotelyInvokable.StreamBody);
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
                Process p = CreateProcessPortable(RemotelyInvokable.StreamBody);
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
