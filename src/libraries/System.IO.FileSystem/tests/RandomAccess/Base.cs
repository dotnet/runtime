// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public abstract class RandomAccess_Base<T> : FileSystemTest
    {
        protected abstract T MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset);

        protected virtual bool ShouldThrowForSyncHandle => false;

        protected virtual bool ShouldThrowForAsyncHandle => false;

        protected virtual bool UsesOffsets => true;

        [Fact]
        public void ThrowsArgumentNullExceptionForNullHandle()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => MethodUnderTest(null, Array.Empty<byte>(), 0));
            Assert.Equal("handle", ex.ParamName);
        }

        [Fact]
        public void ThrowsArgumentExceptionForInvalidHandle()
        {
            SafeFileHandle handle = new SafeFileHandle(new IntPtr(-1), ownsHandle: false);

            ArgumentException ex = Assert.Throws<ArgumentException>(() => MethodUnderTest(handle, Array.Empty<byte>(), 0));
            Assert.Equal("handle", ex.ParamName);
        }

        [Fact]
        public void ThrowsObjectDisposedExceptionForDisposedHandle()
        {
            SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write);
            handle.Dispose();

            Assert.Throws<ObjectDisposedException>(() => MethodUnderTest(handle, Array.Empty<byte>(), 0));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "System.IO.Pipes aren't supported on browser")]
        public void ThrowsNotSupportedExceptionForUnseekableFile()
        {
            using (var server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (SafeFileHandle handle = new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true))
            {
                Assert.Throws<NotSupportedException>(() => MethodUnderTest(handle, Array.Empty<byte>(), 0));
            }
        }

        [Fact]
        public void ThrowsArgumentOutOfRangeExceptionForNegativeFileOffset()
        {
            if (UsesOffsets)
            {
                FileOptions options = ShouldThrowForAsyncHandle ? FileOptions.None : FileOptions.Asynchronous;
                using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: options))
                {
                    ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => MethodUnderTest(handle, Array.Empty<byte>(), -1));
                    Assert.Equal("fileOffset", ex.ParamName);
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void ThrowsArgumentExceptionForAsyncFileHandle()
        {
            if (ShouldThrowForAsyncHandle)
            {
                using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: FileOptions.Asynchronous))
                {
                    ArgumentException ex = Assert.Throws<ArgumentException>(() => MethodUnderTest(handle, new byte[100], 0));
                    Assert.Equal("handle", ex.ParamName);
                }
            }
        }

        [Fact]
        public void ThrowsArgumentExceptionForSyncFileHandle()
        {
            if (ShouldThrowForSyncHandle)
            {
                using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: FileOptions.None))
                {
                    ArgumentException ex = Assert.Throws<ArgumentException>(() => MethodUnderTest(handle, new byte[100], 0));
                    Assert.Equal("handle", ex.ParamName);
                }
            }
        }

        protected static CancellationToken GetCancelledToken()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();

            return token;
        }

        protected SafeFileHandle GetHandleToExistingFile(FileAccess access)
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[1]);

            FileOptions options = ShouldThrowForAsyncHandle ? FileOptions.None : FileOptions.Asynchronous;
            return File.OpenHandle(filePath, FileMode.Open, access, FileShare.None, options);
        }
    }
}
