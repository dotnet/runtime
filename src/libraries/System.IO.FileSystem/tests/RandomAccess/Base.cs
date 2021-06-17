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
            AssertExtensions.Throws<ArgumentNullException>("handle", () => MethodUnderTest(null, Array.Empty<byte>(), 0));
        }

        [Fact]
        public void ThrowsArgumentExceptionForInvalidHandle()
        {
            SafeFileHandle handle = new SafeFileHandle(new IntPtr(-1), ownsHandle: false);

            AssertExtensions.Throws<ArgumentException>("handle", () => MethodUnderTest(handle, Array.Empty<byte>(), 0));
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
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("fileOffset", () => MethodUnderTest(handle, Array.Empty<byte>(), -1));
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [SkipOnPlatform(TestPlatforms.Browser, "async file IO is not supported on browser")]
        public void ThrowsArgumentExceptionForAsyncFileHandle()
        {
            if (ShouldThrowForAsyncHandle)
            {
                using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: FileOptions.Asynchronous))
                {
                    AssertExtensions.Throws<ArgumentException>("handle", () => MethodUnderTest(handle, new byte[100], 0));
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
                    AssertExtensions.Throws<ArgumentException>("handle", () => MethodUnderTest(handle, new byte[100], 0));
                }
            }
        }

        protected static CancellationTokenSource GetCancelledTokenSource()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            source.Cancel();
            return source;
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
