// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public void SafeFileHandle_IsAsync_ReturnsCorrectInformation(FileOptions options)
        {
            using (var handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write, options: options))
            {
                Assert.Equal((options & FileOptions.Asynchronous) != 0, handle.IsAsync);

                // the following code exercises the code path where we don't know FileOptions used for opening the handle
                // and instead we ask the OS about it
                if (OperatingSystem.IsWindows()) // async file handles are a Windows concept
                {
                    SafeFileHandle createdFromIntPtr = new SafeFileHandle(handle.DangerousGetHandle(), ownsHandle: false);
                    Assert.Equal((options & FileOptions.Asynchronous) != 0, createdFromIntPtr.IsAsync);
                }
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
    }
}
