// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class Pipes : FileSystemTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(~TestPlatforms.Browser)] // IO.Pipes not supported
        public async Task AnonymousPipeWriteViaFileStream(bool asyncWrites)
        {
            using (var server = new AnonymousPipeServerStream(PipeDirection.In))
            {
                Task serverTask = Task.Run(() =>
                {
                    for (int i = 0; i < 6; i++)
                        Assert.Equal(i, server.ReadByte());
                });

                using (var client = new FileStream(new SafeFileHandle(server.ClientSafePipeHandle.DangerousGetHandle(), false), FileAccess.Write, bufferSize: 3))
                {
                    var data = new[] { new byte[] { 0, 1 }, new byte[] { 2, 3 }, new byte[] { 4, 5 } };
                    foreach (byte[] arr in data)
                    {
                        if (asyncWrites)
                            await client.WriteAsync(arr, 0, arr.Length);
                        else
                            client.Write(arr, 0, arr.Length);
                    }
                }

                await serverTask;
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/30155")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Uses P/Invokes
        public async Task FifoReadWriteViaFileStream()
        {
            string fifoPath = GetTestFilePath();
            Assert.Equal(0, mkfifo(fifoPath, 666));

            await Task.WhenAll(
                Task.Run(() =>
                {
                    using (FileStream fs = File.OpenRead(fifoPath))
                    {
                        Assert.Equal(42, fs.ReadByte());
                    }
                }),
                Task.Run(() =>
                {
                    using (FileStream fs = File.OpenWrite(fifoPath))
                    {
                        fs.WriteByte(42);
                        fs.Flush();
                    }
                }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(~TestPlatforms.Browser)] // IO.Pipes not supported
        public async Task AnonymousPipeReadViaFileStream(bool asyncReads)
        {
            using (var server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Task serverTask = server.WriteAsync(new byte[] { 0, 1, 2, 3, 4, 5 }, 0, 6);

                using (var client = new FileStream(new SafeFileHandle(server.ClientSafePipeHandle.DangerousGetHandle(), false), FileAccess.Read, bufferSize: 3))
                {
                    var arr = new byte[1];
                    for (int i = 0; i < 6; i++)
                    {
                        Assert.Equal(1, asyncReads ?
                            await client.ReadAsync(arr, 0, 1) :
                            client.Read(arr, 0, 1));
                        Assert.Equal(i, arr[0]);
                    }
                }

                await serverTask;
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Uses P/Invokes to create async pipe handle
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task NamedPipeWriteViaAsyncFileStream(bool asyncWrites)
        {
            string name = GetNamedPipeServerStreamName();
            using (var server = new NamedPipeServerStream(name, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Task serverTask = Task.Run(async () =>
                {
                    await server.WaitForConnectionAsync();
                    for (int i = 0; i < 6; i++)
                        Assert.Equal(i, server.ReadByte());
                });

                WaitNamedPipeW(@"\\.\pipe\" + name, -1);
                using (SafeFileHandle clientHandle = CreateFileW(@"\\.\pipe\" + name, Interop.Kernel32.GenericOperations.GENERIC_WRITE, FileShare.None, IntPtr.Zero, FileMode.Open, (int)PipeOptions.Asynchronous, IntPtr.Zero))
                using (var client = new FileStream(clientHandle, FileAccess.Write, bufferSize: 3, isAsync: true))
                {
                    var data = new[] { new byte[] { 0, 1 }, new byte[] { 2, 3 }, new byte[] { 4, 5 } };
                    foreach (byte[] arr in data)
                    {
                        if (asyncWrites)
                            await client.WriteAsync(arr, 0, arr.Length);
                        else
                            client.Write(arr, 0, arr.Length);
                    }
                }

                await serverTask;
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Uses P/Invokes to create async pipe handle
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task NamedPipeReadViaAsyncFileStream(bool asyncReads)
        {
            string name = GetNamedPipeServerStreamName();
            using (var server = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Task serverTask = Task.Run(async () =>
                {
                    await server.WaitForConnectionAsync();
                    await server.WriteAsync(new byte[] { 0, 1, 2, 3, 4, 5 }, 0, 6);
                });

                WaitNamedPipeW(@"\\.\pipe\" + name, -1);
                using (SafeFileHandle clientHandle = CreateFileW(@"\\.\pipe\" + name, Interop.Kernel32.GenericOperations.GENERIC_READ, FileShare.None, IntPtr.Zero, FileMode.Open, (int)PipeOptions.Asynchronous, IntPtr.Zero))
                using (var client = new FileStream(clientHandle, FileAccess.Read, bufferSize: 3, isAsync: true))
                {
                    var arr = new byte[1];
                    for (int i = 0; i < 6; i++)
                    {
                        Assert.Equal(1, asyncReads ?
                            await client.ReadAsync(arr, 0, 1) :
                            client.Read(arr, 0, 1));
                        Assert.Equal(i, arr[0]);
                    }
                }

                await serverTask;
            }
        }

        #region Windows P/Invokes
        // We need to P/Invoke to test the named pipe async behavior with FileStream
        // because NamedPipeClientStream internally binds the created handle,
        // and that then prevents FileStream's constructor from working with the handle
        // when trying to set isAsync to true.

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WaitNamedPipeW(string name, int timeout);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeFileHandle CreateFileW(
            string lpFileName, int dwDesiredAccess, FileShare dwShareMode,
            IntPtr securityAttrs, FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

        #endregion
    }
}
