// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessHandlesTests : ProcessTestBase
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanRedirectOutputToPipe(bool readAsync)
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "echo Test" } }
                : new("sh") { ArgumentList = { "-c", "echo 'Test'" } };

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, asyncRead: readAsync);

            startInfo.StandardOutput = writePipe;

            using (readPipe)
            using (writePipe)
            {
                using Process process = Process.Start(startInfo)!;

                // Close the parent copy of the child handle, so the pipe will signal EOF when the child exits
                writePipe.Close();

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
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanRedirectOutputAndErrorToDifferentPipes(bool readAsync)
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "echo Hello from stdout && echo Error from stderr 1>&2" } }
                : new("sh") { ArgumentList = { "-c", "echo 'Hello from stdout' && echo 'Error from stderr' >&2" } };

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle outputRead, out SafeFileHandle outputWrite, asyncRead: readAsync);
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle errorRead, out SafeFileHandle errorWrite, asyncRead: readAsync);

            startInfo.StandardOutput = outputWrite;
            startInfo.StandardError = errorWrite;

            using (outputRead)
            using (outputWrite)
            using (errorRead)
            using (errorWrite)
            {
                using Process process = Process.Start(startInfo)!;

                // Close the parent copy of the child handles, so the pipes will signal EOF when the child exits
                outputWrite.Close();
                errorWrite.Close();

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
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanRedirectToInheritedHandles(bool useAsync)
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "exit 42" } }
                : new("sh") { ArgumentList = { "-c", "exit 42" } };

            using SafeFileHandle inputHandle = Console.OpenStandardInputHandle();
            using SafeFileHandle outputHandle = Console.OpenStandardOutputHandle();
            using SafeFileHandle errorHandle = Console.OpenStandardErrorHandle();

            startInfo.StandardInput = inputHandle;
            startInfo.StandardOutput = outputHandle;
            startInfo.StandardError = errorHandle;

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

        [Fact]
        public async Task CanImplementPiping()
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe);
            string? tempFile = null;

            try
            {
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

                producerInfo.StandardOutput = writePipe;

                using SafeFileHandle outputHandle = File.OpenHandle(tempFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

                consumerInfo.StandardInput = readPipe;
                consumerInfo.StandardOutput = outputHandle;

                using Process producer = Process.Start(producerInfo)!;

                writePipe.Close(); // close the parent copy of child handle

                using Process consumer = Process.Start(consumerInfo)!;

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
                StandardInput = Console.OpenStandardInputHandle()
            };

            using (startInfo.StandardInput)
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
                StandardOutput = Console.OpenStandardOutputHandle()
            };

            using (startInfo.StandardOutput)
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
                StandardError = Console.OpenStandardErrorHandle()
            };

            using (startInfo.StandardError)
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
                StandardOutput = Console.OpenStandardOutputHandle()
            };

            using (startInfo.StandardOutput)
            {
                Assert.Throws<InvalidOperationException>(() => Process.Start(startInfo));
            }
        }

        [Fact]
        public void StandardHandles_DefaultIsNull()
        {
            ProcessStartInfo startInfo = new("cmd");
            Assert.Null(startInfo.StandardInput);
            Assert.Null(startInfo.StandardOutput);
            Assert.Null(startInfo.StandardError);
        }

        [Fact]
        public void StandardHandles_CanSetAndGet()
        {
            using SafeFileHandle handle = Console.OpenStandardOutputHandle();

            ProcessStartInfo startInfo = new("cmd")
            {
                StandardInput = handle,
                StandardOutput = handle,
                StandardError = handle
            };

            Assert.Same(handle, startInfo.StandardInput);
            Assert.Same(handle, startInfo.StandardOutput);
            Assert.Same(handle, startInfo.StandardError);
        }
    }
}
