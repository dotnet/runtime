// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    [SkipOnPlatform(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "sh is not available in the mobile platform sandbox")]
    public partial class ProcessHandlesTests : ProcessTestBase
    {
        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task CanRedirectOutputToPipe(bool readAsync, bool restrictHandles)
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "echo Test" } }
                : new("sh") { ArgumentList = { "-c", "echo 'Test'" } };

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, asyncRead: readAsync);

            startInfo.StandardOutputHandle = writePipe;
            startInfo.InheritedHandles = restrictHandles ? [] : null;

            using (readPipe)
            using (writePipe)
            {
                using Process process = Process.Start(startInfo)!;
                writePipe.Close(); // close the parent copy of child handle

                using FileStream fileStream = new(readPipe, FileAccess.Read, bufferSize: 1, isAsync: readAsync);
                using StreamReader streamReader = new(fileStream);

                string output = readAsync
                    ? await streamReader.ReadToEndAsync()
                    : streamReader.ReadToEnd();

                Assert.Equal(OperatingSystem.IsWindows() ? "Test\r\n" : "Test\n", output);

                process.WaitForExit();
                Assert.Equal(0, process.ExitCode);
            }
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task CanRedirectOutputAndErrorToDifferentPipes(bool readAsync, bool restrictHandles)
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "echo Hello from stdout && echo Error from stderr 1>&2" } }
                : new("sh") { ArgumentList = { "-c", "echo 'Hello from stdout' && echo 'Error from stderr' >&2" } };

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle outputRead, out SafeFileHandle outputWrite, asyncRead: readAsync);
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle errorRead, out SafeFileHandle errorWrite, asyncRead: readAsync);

            startInfo.StandardOutputHandle = outputWrite;
            startInfo.StandardErrorHandle = errorWrite;
            startInfo.InheritedHandles = restrictHandles ? [] : null;

            using (outputRead)
            using (outputWrite)
            using (errorRead)
            using (errorWrite)
            {
                using Process process = Process.Start(startInfo)!;
                outputWrite.Close(); // close the parent copy of child handle
                errorWrite.Close(); // close the parent copy of child handle

                using FileStream outputStream = new(outputRead, FileAccess.Read, bufferSize: 1, isAsync: readAsync);
                using FileStream errorStream = new(errorRead, FileAccess.Read, bufferSize: 1, isAsync: readAsync);
                using StreamReader outputReader = new(outputStream);
                using StreamReader errorReader = new(errorStream);

                Task<string> outputTask = outputReader.ReadToEndAsync();
                Task<string> errorTask = errorReader.ReadToEndAsync();

                Assert.Equal(OperatingSystem.IsWindows() ? "Hello from stdout \r\n" : "Hello from stdout\n", await outputTask);
                Assert.Equal(OperatingSystem.IsWindows() ? "Error from stderr \r\n" : "Error from stderr\n", await errorTask);

                process.WaitForExit();
                Assert.Equal(0, process.ExitCode);
            }
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task CanRedirectOutputAndErrorToSamePipe(bool readAsync, bool restrictHandles)
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "echo Hello from stdout && echo Error from stderr 1>&2" } }
                : new("sh") { ArgumentList = { "-c", "echo 'Hello from stdout' && echo 'Error from stderr' >&2" } };

            string expectedOutput = OperatingSystem.IsWindows()
                ? "Hello from stdout \r\nError from stderr \r\n"
                : "Hello from stdout\nError from stderr\n";

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, asyncRead: readAsync);

            startInfo.StandardOutputHandle = writePipe;
            startInfo.StandardErrorHandle = writePipe;
            startInfo.InheritedHandles = restrictHandles ? [] : null;

            using (readPipe)
            using (writePipe)
            {
                using Process process = Process.Start(startInfo)!;
                writePipe.Close(); // close the parent copy of child handle

                using FileStream combinedStream = new(readPipe, FileAccess.Read, bufferSize: 1, isAsync: readAsync);
                using StreamReader combinedReader = new(combinedStream);

                Assert.Equal(expectedOutput, await combinedReader.ReadToEndAsync());

                process.WaitForExit();
                Assert.Equal(0, process.ExitCode);
            }
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task CanRedirectToInheritedHandles(bool useAsync, bool restrictHandles)
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "exit 42" } }
                : new("sh") { ArgumentList = { "-c", "exit 42" } };

            startInfo.StandardInputHandle = Console.OpenStandardInputHandle();
            startInfo.StandardOutputHandle = Console.OpenStandardOutputHandle();
            startInfo.StandardErrorHandle = Console.OpenStandardErrorHandle();
            startInfo.InheritedHandles = restrictHandles ? [] : null;

            using Process process = Process.Start(startInfo)!;

            if (useAsync)
            {
                await process.WaitForExitAsync();
            }
            else
            {
                process.WaitForExit();
            }

            Assert.Equal(42, process.ExitCode);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CanImplementPiping(bool restrictHandles)
        {
            SafeFileHandle readPipe = null!;
            SafeFileHandle writePipe = null!;
            string? tempFile = null;

            try
            {
                SafeFileHandle.CreateAnonymousPipe(out readPipe, out writePipe);
                tempFile = Path.GetTempFileName();

                ProcessStartInfo producerInfo;
                ProcessStartInfo consumerInfo;
                string expectedOutput;

                if (OperatingSystem.IsWindows())
                {
                    producerInfo = new("cmd")
                    {
                        ArgumentList = { "/c", "echo hello world & echo test line & echo another test" }
                    };
                    consumerInfo = new("findstr")
                    {
                        ArgumentList = { "test" }
                    };
                    // findstr adds a trailing space on Windows
                    expectedOutput = "test line \nanother test\n";
                }
                else
                {
                    producerInfo = new("sh")
                    {
                        ArgumentList = { "-c", "printf 'hello world\\ntest line\\nanother test\\n'" }
                    };
                    consumerInfo = new("grep")
                    {
                        ArgumentList = { "test" }
                    };
                    expectedOutput = "test line\nanother test\n";
                }

                producerInfo.StandardOutputHandle = writePipe;

                using SafeFileHandle outputHandle = File.OpenHandle(tempFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

                consumerInfo.StandardInputHandle = readPipe;
                consumerInfo.StandardOutputHandle = outputHandle;

                producerInfo.InheritedHandles = restrictHandles ? [] : null;
                consumerInfo.InheritedHandles = restrictHandles ? [] : null;

                using Process producer = Process.Start(producerInfo)!;
                writePipe.Close(); // close the parent copy of child handle

                using Process consumer = Process.Start(consumerInfo)!;
                outputHandle.Close(); // close the parent copy of child handle
                readPipe.Close(); // close the parent copy of child handle

                producer.WaitForExit();
                consumer.WaitForExit();

                string result = File.ReadAllText(tempFile);
                Assert.Equal(expectedOutput, result, ignoreLineEndingDifferences: true);
            }
            finally
            {
                readPipe.Dispose();
                writePipe.Dispose();

                if (tempFile is not null && File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void StandardInput_WithRedirectStandardInput_Throws()
        {
            ProcessStartInfo startInfo = new("cmd")
            {
                RedirectStandardInput = true,
                StandardInputHandle = Console.OpenStandardInputHandle()
            };

            using (startInfo.StandardInputHandle)
            {
                Assert.Throws<InvalidOperationException>(() => Process.Start(startInfo));
            }
        }

        [Fact]
        public void StandardOutput_WithRedirectStandardOutput_Throws()
        {
            ProcessStartInfo startInfo = new("cmd")
            {
                RedirectStandardOutput = true,
                StandardOutputHandle = Console.OpenStandardOutputHandle()
            };

            using (startInfo.StandardOutputHandle)
            {
                Assert.Throws<InvalidOperationException>(() => Process.Start(startInfo));
            }
        }

        [Fact]
        public void StandardError_WithRedirectStandardError_Throws()
        {
            ProcessStartInfo startInfo = new("cmd")
            {
                RedirectStandardError = true,
                StandardErrorHandle = Console.OpenStandardErrorHandle()
            };

            using (startInfo.StandardErrorHandle)
            {
                Assert.Throws<InvalidOperationException>(() => Process.Start(startInfo));
            }
        }

        [Fact]
        public void StandardHandles_WithUseShellExecute_Throws()
        {
            ProcessStartInfo startInfo = new("cmd")
            {
                UseShellExecute = true,
                StandardOutputHandle = Console.OpenStandardOutputHandle()
            };

            using (startInfo.StandardOutputHandle)
            {
                Assert.Throws<InvalidOperationException>(() => Process.Start(startInfo));
            }
        }

        [Fact]
        public void StandardHandles_DefaultIsNull()
        {
            ProcessStartInfo startInfo = new("cmd");
            Assert.Null(startInfo.StandardInputHandle);
            Assert.Null(startInfo.StandardOutputHandle);
            Assert.Null(startInfo.StandardErrorHandle);
        }

        [Fact]
        public void StandardHandles_CanSetAndGet()
        {
            using SafeFileHandle handle = Console.OpenStandardOutputHandle();

            ProcessStartInfo startInfo = new("cmd")
            {
                StandardInputHandle = handle,
                StandardOutputHandle = handle,
                StandardErrorHandle = handle
            };

            Assert.Same(handle, startInfo.StandardInputHandle);
            Assert.Same(handle, startInfo.StandardOutputHandle);
            Assert.Same(handle, startInfo.StandardErrorHandle);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(HandleInheritability.Inheritable)]
        [InlineData(HandleInheritability.None)]
        public async Task InheritedHandles_CanInherit_SafePipeHandle(HandleInheritability inheritability)
        {
            using AnonymousPipeServerStream pipeServer = new(PipeDirection.In, inheritability);

            RemoteInvokeOptions options = new() { CheckExitCode = false };
            options.StartInfo.InheritedHandles = [pipeServer.ClientSafePipeHandle];

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                static (string handleStr) =>
                {
                    using AnonymousPipeClientStream client = new(PipeDirection.Out, handleStr);
                    client.WriteByte(42);

                    return RemoteExecutor.SuccessExitCode;
                },
                pipeServer.GetClientHandleAsString(),
                options);

            pipeServer.DisposeLocalCopyOfClientHandle(); // close the parent copy of child handle

            await VerifyExpectedOutcome(pipeServer, remoteHandle);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task InheritedHandles_CanInherit_SafeFileHandle()
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe);

            RemoteInvokeOptions options = new() { CheckExitCode = false };
            options.StartInfo.InheritedHandles = [writePipe];

            using (FileStream fileStream = new(readPipe, FileAccess.Read, bufferSize: 1))
            using (writePipe)
            {
                using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                    static (string handleStr) =>
                    {
                        using SafeFileHandle safeFileHandle = new(nint.Parse(handleStr), ownsHandle: true);
                        using FileStream client = new(safeFileHandle, FileAccess.Write, bufferSize: 1);
                        client.WriteByte(42);

                        return RemoteExecutor.SuccessExitCode;
                    },
                    writePipe.DangerousGetHandle().ToString(),
                    options);

                writePipe.Close(); // close the parent copy of child handle

                await VerifyExpectedOutcome(fileStream, remoteHandle);
            }
        }

        private static async Task VerifyExpectedOutcome(Stream parentReadStream, RemoteInvokeHandle remoteHandle)
        {
            try
            {
                byte[] buffer = new byte[1];
                int bytesRead = await parentReadStream.ReadAsync(buffer);

                remoteHandle.Process.WaitForExit(WaitInMS);

                Assert.Equal(1, bytesRead);
                Assert.Equal(42, buffer[0]);
                Assert.Equal(RemoteExecutor.SuccessExitCode, remoteHandle.Process.ExitCode);
            }
            finally
            {
                remoteHandle.Process.Kill();
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CanInheritMoreThanOneHandle()
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe);

            RemoteInvokeOptions options = new();

            using (readPipe)
            using (writePipe)
            using (SafeFileHandle nullHandle = File.OpenNullHandle())
            {
                // "notInherited" is not added on purpose
                options.StartInfo.InheritedHandles = [readPipe, writePipe, nullHandle];

                using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                    static (string handlesStr) =>
                    {
                        string[] handlesStrings = handlesStr.Split(' ');

                        using SafeFileHandle firstPipe = new(nint.Parse(handlesStrings[0]), ownsHandle: true);
                        using SafeFileHandle secondPipe = new(nint.Parse(handlesStrings[1]), ownsHandle: true);
                        using SafeFileHandle nullFile = new(nint.Parse(handlesStrings[2]), ownsHandle: true);

                        // These APIs need to fetch some data from the OS.
                        Assert.Equal(FileHandleType.Pipe, firstPipe.Type);
                        Assert.False(firstPipe.IsAsync);
                        Assert.Equal(FileHandleType.Pipe, secondPipe.Type);
                        Assert.False(secondPipe.IsAsync);
                        Assert.Equal(FileHandleType.CharacterDevice, nullFile.Type);
                        Assert.False(nullFile.IsAsync);

                        return RemoteExecutor.SuccessExitCode;
                    },
                    $"{readPipe.DangerousGetHandle()} {writePipe.DangerousGetHandle()} {nullHandle.DangerousGetHandle()}",
                    options);
            }
        }

        [Fact]
        public void InheritedHandles_ThrowsForNullHandle()
        {
            string exe = OperatingSystem.IsWindows() ? "cmd" : "sh";
            ProcessStartInfo startInfo = new(exe) { InheritedHandles = [null!] };
            Assert.Throws<ArgumentNullException>(() => Process.Start(startInfo));
        }

        [Fact]
        public void InheritedHandles_ThrowsForInvalidHandle()
        {
            string exe = OperatingSystem.IsWindows() ? "cmd" : "sh";
            using SafeFileHandle handle = new(-1, ownsHandle: false);
            Assert.True(handle.IsInvalid);
            ProcessStartInfo startInfo = new(exe) { InheritedHandles = [handle] };
            Assert.Throws<ArgumentException>(() => Process.Start(startInfo));
        }

        [Fact]
        public void InheritedHandles_ThrowsForClosedHandle()
        {
            string exe = OperatingSystem.IsWindows() ? "cmd" : "sh";
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe);
            readPipe.Dispose();
            writePipe.Dispose();
            ProcessStartInfo startInfo = new(exe) { InheritedHandles = [readPipe] };
            Assert.Throws<ObjectDisposedException>(() => Process.Start(startInfo));
        }

        [Fact]
        public void InheritedHandles_ThrowsFor_UnsupportedHandle()
        {
            using SafeProcessHandle handle = Process.GetCurrentProcess().SafeHandle;
            Assert.False(handle.IsInvalid);

            ProcessStartInfo startInfo = new("hostname") { InheritedHandles = [handle] };
            Assert.Throws<ArgumentException>(() => Process.Start(startInfo));
        }

        [Theory]
        [InlineData("input")]
        [InlineData("output")]
        [InlineData("error")]
        public void InheritedHandles_ThrowsForStandardHandles(string whichHandle)
        {
            string exe = OperatingSystem.IsWindows() ? "cmd" : "sh";
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe);

            using (readPipe)
            using (writePipe)
            {
                SafeFileHandle handle = whichHandle == "input" ? readPipe : writePipe;
                ProcessStartInfo startInfo = new(exe)
                {
                    StandardInputHandle = whichHandle == "input" ? handle : null,
                    StandardOutputHandle = whichHandle == "output" ? handle : null,
                    StandardErrorHandle = whichHandle == "error" ? handle : null,
                    InheritedHandles = [handle]
                };

                Assert.Throws<ArgumentException>(() => Process.Start(startInfo));
            }
        }

        [Theory]
        [InlineData("input")]
        [InlineData("output")]
        [InlineData("error")]
        public void InheritedHandles_ThrowsForParentStandardHandles(string whichHandle)
        {
            string exe = OperatingSystem.IsWindows() ? "cmd" : "sh";
            SafeFileHandle handle = whichHandle switch
            {
                "input" => Console.OpenStandardInputHandle(),
                "output" => Console.OpenStandardOutputHandle(),
                "error" => Console.OpenStandardErrorHandle(),
                _ => throw new UnreachableException()
            };

            ProcessStartInfo startInfo = new(exe)
            {
                InheritedHandles = [handle]
            };

            Assert.Throws<ArgumentException>(() => Process.Start(startInfo));
        }

        [Fact]
        public void InheritedHandles_ThrowsForDuplicates()
        {
            string exe = OperatingSystem.IsWindows() ? "cmd" : "sh";
            using SafeFileHandle nullHandle = File.OpenNullHandle();

            ProcessStartInfo startInfo = new(exe)
            {
                InheritedHandles = [nullHandle, nullHandle]
            };

            Assert.Throws<ArgumentException>(() => Process.Start(startInfo));
        }
    }
}
