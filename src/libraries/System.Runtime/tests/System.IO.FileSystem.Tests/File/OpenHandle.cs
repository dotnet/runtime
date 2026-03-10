// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    // to avoid a lot of code duplication, we reuse FileStream tests
    public class File_OpenHandle : FileStream_ctor_options
    {
        protected override string GetExpectedParamName(string paramName) => paramName;

        protected override FileStream CreateFileStream(string path, FileMode mode) =>
            CreateFileStream(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite);

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            SafeFileHandle handle = File.OpenHandle(path, mode, access);
            try
            {
                return new FileStream(handle, access);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            SafeFileHandle handle = File.OpenHandle(path, mode, access, share, options);
            try
            {
                return new FileStream(handle, access, bufferSize, (options & FileOptions.Asynchronous) != 0);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
        {
            SafeFileHandle handle = File.OpenHandle(path, mode, access, share, options, preallocationSize);
            try
            {
                return new FileStream(handle, access, bufferSize, (options & FileOptions.Asynchronous) != 0);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/53432")]
        [Theory, MemberData(nameof(StreamSpecifiers))]
        public override void FileModeAppendExisting(string streamSpecifier)
        {
            _ = streamSpecifier; // to keep the xUnit analyser happy
        }

        [Theory]
        [InlineData(FileOptions.None)]
        [InlineData(FileOptions.Asynchronous)]
        public void SafeFileHandle_IsAsync_ReturnsCorrectInformation_ForRegularFiles(FileOptions options)
        {
            using (var handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write, options: options))
            {
                Assert.Equal((options & FileOptions.Asynchronous) != 0 && IsAsyncIoSupportedForRegularFiles, handle.IsAsync);

                // the following code exercises the code path where we don't know FileOptions used for opening the handle
                // and instead we ask the OS about it
                if (IsAsyncIoSupportedForRegularFiles)
                {
                    SafeFileHandle createdFromIntPtr = new SafeFileHandle(handle.DangerousGetHandle(), ownsHandle: false);
                    Assert.Equal((options & FileOptions.Asynchronous) != 0, createdFromIntPtr.IsAsync);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.Browser, "Pipes are not supported on browser")]
        public void SafeFileHandle_IsAsync_ReturnsCorrectInformation_ForPipes(bool useAsync)
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle, asyncRead: useAsync, asyncWrite: useAsync);

            using (readHandle)
            using (writeHandle)
            {
                Verify(readHandle, useAsync);
                Verify(writeHandle, useAsync);
            }

            static void Verify(SafeFileHandle fileHandle, bool useAsyncIO)
            {
                Assert.Equal(useAsyncIO, fileHandle.IsAsync);

                // The following code exercises the code path where the information is fetched from OS.
                using SafeFileHandle createdFromIntPtr = new(fileHandle.DangerousGetHandle(), ownsHandle: false);
                Assert.Equal(useAsyncIO, createdFromIntPtr.IsAsync);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [SkipOnPlatform(TestPlatforms.Browser, "Pipes are not supported on browser")]
        public static async Task SafeFileHandle_CreateAnonymousPipe_SetsIsAsyncAndTransfersData(bool asyncRead, bool asyncWrite)
        {
            byte[] message = "Hello, Pipe!"u8.ToArray();
            byte[] buffer = new byte[message.Length];

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle, asyncRead, asyncWrite);
            Assert.Equal(asyncRead, readHandle.IsAsync);
            Assert.Equal(asyncWrite, writeHandle.IsAsync);
            Assert.Equal(FileHandleType.Pipe, readHandle.Type);
            Assert.Equal(FileHandleType.Pipe, writeHandle.Type);

            using (readHandle)
            using (writeHandle)
            using (Stream readStream = CreatePipeStream(readHandle, FileAccess.Read, asyncRead))
            using (Stream writeStream = CreatePipeStream(writeHandle, FileAccess.Write, asyncWrite))
            {
                Task writeTask = writeStream.WriteAsync(message, 0, message.Length);
                Task readTask = readStream.ReadAsync(buffer, 0, buffer.Length);
                await Task.WhenAll(writeTask, readTask);
                Assert.Equal(message, buffer);

                // Now let's test a different order,
                // which is going to test the E_WOULDBLOCK code path on Unix.
                buffer.AsSpan().Reverse();

                readTask = readStream.ReadExactlyAsync(buffer).AsTask();
                writeTask = writeStream.WriteAsync(message, 0, message.Length);
                await Task.WhenAll(readTask, writeTask);
                Assert.Equal(message, buffer);
            }
        }

        private static Stream CreatePipeStream(SafeFileHandle readHandle, FileAccess access, bool asyncIO)
        {
            if (!OperatingSystem.IsWindows() && asyncIO)
            {
                PipeDirection direction = access == FileAccess.Read ? PipeDirection.In : PipeDirection.Out;
                return new AnonymousPipeClientStream(direction, TransferOwnershipToPipeHandle(readHandle));
            }

            return new FileStream(readHandle, access, 1, asyncIO);

            static SafePipeHandle TransferOwnershipToPipeHandle(SafeFileHandle handle)
            {
                SafePipeHandle pipeHandle = new SafePipeHandle(handle.DangerousGetHandle(), ownsHandle: true);
                handle.SetHandleAsInvalid();
                handle.Dispose();
                return pipeHandle;
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [SkipOnPlatform(TestPlatforms.Browser, "Pipes are not supported on browser")]
        public void AsyncHandleOnUnix_FileStream_ctor_Throws()
        {
            // Currently SafeFileHandle.CreateAnonymousPipe is the only public API that allows creating async handles on Unix

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle, asyncRead: true, asyncWrite: true);

            using (readHandle)
            using (writeHandle)
            {
                Assert.True(readHandle.IsAsync);
                Assert.True(writeHandle.IsAsync);
                Assert.Equal(FileHandleType.Pipe, readHandle.Type);
                Assert.Equal(FileHandleType.Pipe, writeHandle.Type);

                AssertExtensions.Throws<ArgumentException>("handle", () => new FileStream(readHandle, FileAccess.ReadWrite));
                AssertExtensions.Throws<ArgumentException>("handle", () => new FileStream(writeHandle, FileAccess.ReadWrite));
            }
        }

        [Theory]
        [InlineData(FileOptions.DeleteOnClose)]
        [InlineData(FileOptions.DeleteOnClose | FileOptions.Asynchronous)]
        public void DeleteOnClose_FileDeletedAfterSafeHandleDispose(FileOptions options)
        {
            string path = GetTestFilePath();
            Assert.False(File.Exists(path));
            using (SafeFileHandle sfh = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, options))
            {
                Assert.True(File.Exists(path));
            }
            Assert.False(File.Exists(path));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PreallocationSizeVeryLargeThrowsCorrectHResult()
        {
            const long VeryLargeFileSize = (long)128 * 1024 * 1024 * 1024 * 1024; // 128TB

            // The largest file size depends on cluster size.
            // See https://learn.microsoft.com/en-us/windows-server/storage/file-server/ntfs-overview#support-for-large-volumes


            const int ERROR_INVALID_PARAMETER = unchecked((int)0x80070057);
            const int ERROR_DISK_FULL = unchecked((int)0x80070070);

            string path = GetTestFilePath();
            if (!IOServices.IsDriveNTFS(Path.GetPathRoot(path)))
            {
                // Skip the test for non-NTFS filesystems
                return;
            }

            if (new DriveInfo(path).TotalFreeSpace >= VeryLargeFileSize)
            {
                // Skip the test if somehow the drive is really big.
                return;
            }

            try
            {
                using (File.OpenHandle(path, mode: FileMode.Create, access: FileAccess.ReadWrite, preallocationSize: VeryLargeFileSize)) { }
                Assert.Fail("File.OpenHandle should throw due to failure to preallocate a very large file.");
            }
            catch (IOException ex)
            {
                // Accept both results since we cannot assume the cluster size of testing volume
                Assert.True(ex.HResult is ERROR_INVALID_PARAMETER or ERROR_DISK_FULL);
            }
        }
    }
}
