// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class FileStream_CopyToAsync : FileSystemTest
    {
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
    }
}
