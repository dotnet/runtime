// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class FileStream_CopyToAsync : FileSystemTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InvalidArgs_Throws(bool useAsync)
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 0x100, useAsync))
            {
                AssertExtensions.Throws<ArgumentNullException>("destination", () => { fs.CopyToAsync(null); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => { fs.CopyToAsync(new MemoryStream(), 0); });
                Assert.Throws<NotSupportedException>(() => { fs.CopyToAsync(new MemoryStream(new byte[1], writable: false)); });
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { fs.CopyToAsync(new MemoryStream()); });
            }
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => { fs.CopyToAsync(new MemoryStream()); });
            }
            using (FileStream src = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 0x100, useAsync))
            using (FileStream dst = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 0x100, useAsync))
            {
                dst.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { src.CopyToAsync(dst); });
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DisposeHandleThenUseFileStream_CopyToAsync(bool useAsync)
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 0x100, useAsync))
            {
                fs.SafeFileHandle.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { fs.CopyToAsync(new MemoryStream()); });
            }

            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 0x100, useAsync))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
                fs.SafeFileHandle.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { fs.CopyToAsync(new MemoryStream()).Wait(); });
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AlreadyCanceled_ReturnsCanceledTask(bool useAsync)
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 0x100, useAsync))
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fs.CopyToAsync(fs, 0x1000, new CancellationToken(canceled: true)));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // inner loop, just a few cases
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public Task File_AllDataCopied_InnerLoop(bool useAsync, bool preWrite)
        {
            return File_AllDataCopied(
                _ => new MemoryStream(), useAsync, preRead: false, preWrite: preWrite, exposeHandle: false, cancelable: true,
                bufferSize: 4096, writeSize: 1024, numWrites: 10);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // outer loop, many combinations
        [OuterLoop]
        [MemberData(nameof(File_AllDataCopied_MemberData))]
        public async Task File_AllDataCopied(
            Func<string, Stream> createDestinationStream,
            bool useAsync, bool preRead, bool preWrite, bool exposeHandle, bool cancelable,
            int bufferSize, int writeSize, int numWrites)
        {
            // Create the expected data
            long totalLength = writeSize * numWrites;
            var expectedData = new byte[totalLength];
            new Random(42).NextBytes(expectedData);

            // Write it out into the source file
            string srcPath = GetTestFilePath();
            File.WriteAllBytes(srcPath, expectedData);

            string dstPath = GetTestFilePath();
            using (FileStream src = new FileStream(srcPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, bufferSize, useAsync))
            using (Stream dst = createDestinationStream(dstPath))
            {
                // If configured to expose the handle, do so.  This influences the stream's need to ensure the position is in sync.
                if (exposeHandle)
                {
                    _ = src.SafeFileHandle;
                }

                // If configured to "preWrite", do a write before we start reading.
                if (preWrite)
                {
                    src.Write(new byte[] { 42 }, 0, 1);
                    dst.Write(new byte[] { 42 }, 0, 1);
                    expectedData[0] = 42;
                }

                // If configured to "preRead", read one byte from the source prior to the CopyToAsync.
                // This helps test what happens when there's already data in the buffer, when the position
                // isn't starting at zero, etc.
                if (preRead)
                {
                    int initialByte = src.ReadByte();
                    if (initialByte >= 0)
                    {
                        dst.WriteByte((byte)initialByte);
                    }
                }

                // Do the copy
                await src.CopyToAsync(dst, writeSize, cancelable ? new CancellationTokenSource().Token : CancellationToken.None);
                dst.Flush();

                // Make sure we're at the end of the source file
                Assert.Equal(src.Length, src.Position);

                // Verify the copied data
                dst.Position = 0;
                var result = new MemoryStream();
                dst.CopyTo(result);
                byte[] actualData = result.ToArray();
                Assert.Equal(expectedData.Length, actualData.Length);
                Assert.Equal<byte>(expectedData, actualData);
            }
        }

        public static IEnumerable<object[]> File_AllDataCopied_MemberData()
        {
            bool[] bools = new[] { true, false };
            foreach (bool useAsync in bools) // sync or async mode
            {
                foreach (bool preRead in bools) // whether to do a read before the CopyToAsync
                {
                    foreach (bool cancelable in bools) // whether to use a cancelable token
                    {
                        for (int streamType = 0; streamType < 2; streamType++) // kind of stream to use
                        {
                            Func<string, Stream> createDestinationStream;
                            switch (streamType)
                            {
                                case 0: createDestinationStream = _ => new MemoryStream(); break;
                                default: createDestinationStream = s => File.Create(s); break;
                            }

                            // Various exposeHandle (whether the SafeFileHandle was publicly accessed),
                            // preWrite, bufferSize, writeSize, and numWrites combinations
                            yield return new object[] { createDestinationStream, useAsync, preRead, false, false, cancelable, 0x1000, 0x100, 100 };
                            yield return new object[] { createDestinationStream, useAsync, preRead, false, false, cancelable, 0x1, 0x1, 1000 };
                            yield return new object[] { createDestinationStream, useAsync, preRead, false, true, cancelable, 0x2, 0x100, 100 };
                            yield return new object[] { createDestinationStream, useAsync, preRead, false, false, cancelable, 0x4000, 0x10, 100 };
                            yield return new object[] { createDestinationStream, useAsync, preRead, false, true, cancelable, 0x1000, 99999, 10 };
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(10, 1024)]
        [PlatformSpecific(~TestPlatforms.Browser)] // IO.Pipes not supported
        public async Task AnonymousPipeViaFileStream_AllDataCopied(int writeSize, int numWrites)
        {
            long totalLength = writeSize * numWrites;
            var expectedData = new byte[totalLength];
            new Random(42).NextBytes(expectedData);

            var results = new MemoryStream();

            using (var server = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Task serverTask = Task.Run(async () =>
                {
                    for (int i = 0; i < numWrites; i++)
                    {
                        await server.WriteAsync(expectedData, i * writeSize, writeSize);
                    }
                });

                using (var client = new FileStream(new SafeFileHandle(server.ClientSafePipeHandle.DangerousGetHandle(), false), FileAccess.Read, bufferSize: 3))
                {
                    Task copyTask = client.CopyToAsync(results, writeSize);
                    await await Task.WhenAny(serverTask, copyTask);

                    server.Dispose();
                    await copyTask;
                }
            }

            byte[] actualData = results.ToArray();
            Assert.Equal(expectedData.Length, actualData.Length);
            Assert.Equal<byte>(expectedData, actualData);
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Uses P/Invokes to create async pipe handle
        [Theory]
        [InlineData(false, 10, 1024)]
        [InlineData(true, 10, 1024)]
        public async Task NamedPipeViaFileStream_AllDataCopied(bool useAsync, int writeSize, int numWrites)
        {
            long totalLength = writeSize * numWrites;
            var expectedData = new byte[totalLength];
            new Random(42).NextBytes(expectedData);

            var results = new MemoryStream();
            var pipeOptions = useAsync ? PipeOptions.Asynchronous : PipeOptions.None;

            string name = GetNamedPipeServerStreamName();
            using (var server = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte, pipeOptions))
            {
                Task serverTask = Task.Run(async () =>
                {
                    await server.WaitForConnectionAsync();
                    for (int i = 0; i < numWrites; i++)
                    {
                        await server.WriteAsync(expectedData, i * writeSize, writeSize);
                    }
                    server.Dispose();
                });

                Assert.True(WaitNamedPipeW(@"\\.\pipe\" + name, -1));
                using (SafeFileHandle clientHandle = CreateFileW(@"\\.\pipe\" + name, Interop.Kernel32.GenericOperations.GENERIC_READ, FileShare.None, IntPtr.Zero, FileMode.Open, (int)pipeOptions, IntPtr.Zero))
                using (var client = new FileStream(clientHandle, FileAccess.Read, bufferSize: 3, isAsync: useAsync))
                {
                    Task copyTask = client.CopyToAsync(results, (int)totalLength);
                    await await Task.WhenAny(serverTask, copyTask);
                    await copyTask;
                }
            }

            byte[] actualData = results.ToArray();
            Assert.Equal(expectedData.Length, actualData.Length);
            Assert.Equal<byte>(expectedData, actualData);
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Uses P/Invokes to create async pipe handle
        [Fact]
        public async Task NamedPipeViaFileStream_CancellationRequested_OperationCanceled()
        {
            string name = Guid.NewGuid().ToString("N");
            using (var server = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Task serverTask = server.WaitForConnectionAsync();

                Assert.True(WaitNamedPipeW(@"\\.\pipe\" + name, -1));
                using (SafeFileHandle clientHandle = CreateFileW(@"\\.\pipe\" + name, Interop.Kernel32.GenericOperations.GENERIC_READ, FileShare.None, IntPtr.Zero, FileMode.Open, (int)PipeOptions.Asynchronous, IntPtr.Zero))
                using (var client = new FileStream(clientHandle, FileAccess.Read, bufferSize: 3, isAsync: true))
                {
                    await serverTask;

                    var cts = new CancellationTokenSource();
                    Task clientTask = client.CopyToAsync(new MemoryStream(), 0x1000, cts.Token);
                    Assert.False(clientTask.IsCompleted);

                    cts.Cancel();
                    await Assert.ThrowsAsync<OperationCanceledException>(() => clientTask);
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DerivedFileStream_ReadAsyncInvoked(bool useAsync)
        {
            var expectedData = new byte[100];
            new Random(42).NextBytes(expectedData);

            string srcPath = GetTestFilePath();
            File.WriteAllBytes(srcPath, expectedData);

            bool readAsyncInvoked = false;
            using (var fs = new FileStreamThatOverridesReadAsync(srcPath, useAsync, () => readAsyncInvoked = true))
            {
                await fs.CopyToAsync(new MemoryStream());
                Assert.True(readAsyncInvoked);
            }
        }

        private class FileStreamThatOverridesReadAsync : FileStream
        {
            private readonly Action _readAsyncInvoked;

            internal FileStreamThatOverridesReadAsync(string path, bool useAsync, Action readAsyncInvoked) :
                base(path, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000, useAsync)
            {
                _readAsyncInvoked = readAsyncInvoked;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                _readAsyncInvoked();
                return base.ReadAsync(buffer, offset, count, cancellationToken);
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
