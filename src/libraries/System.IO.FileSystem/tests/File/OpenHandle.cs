// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    // to avoid a lot of code duplication, we reuse FileStream tests
    public class File_OpenHandle : FileStream_ctor_options_as
    {
        protected override string GetExpectedParamName(string paramName) => paramName;

        protected override FileStream CreateFileStream(string path, FileMode mode)
        {
            FileAccess access = mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite;
            return new FileStream(File.OpenHandle(path, mode, access, preallocationSize: PreallocationSize), access);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
            => new FileStream(File.OpenHandle(path, mode, access, preallocationSize: PreallocationSize), access);

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            => new FileStream(File.OpenHandle(path, mode, access, share, options, PreallocationSize), access, bufferSize, (options & FileOptions.Asynchronous) != 0);

        [Fact]
        public override void NegativePreallocationSizeThrows()
        {
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => File.OpenHandle("validPath", FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.None, preallocationSize: -1));
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
