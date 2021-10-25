// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public abstract class RandomAccess_Base<T> : FileSystemTest
    {
        protected abstract T MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset);

        protected virtual bool UsesOffsets => true;

        public static IEnumerable<object[]> GetSyncAsyncOptions()
        {
            yield return new object[] { FileOptions.None };

            if (PlatformDetection.IsAsyncFileIOSupported)
            {
                yield return new object[] { FileOptions.Asynchronous };
            }
        }

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
            using (SafeFileHandle handle = new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), ownsHandle: false))
            {
                Assert.Throws<NotSupportedException>(() => MethodUnderTest(handle, Array.Empty<byte>(), 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ThrowsArgumentOutOfRangeExceptionForNegativeFileOffset(FileOptions options)
        {
            if (UsesOffsets)
            {
                using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: options))
                {
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("fileOffset", () => MethodUnderTest(handle, Array.Empty<byte>(), -1));
                }
            }
        }

        protected static CancellationTokenSource GetCancelledTokenSource()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            source.Cancel();
            return source;
        }

        protected SafeFileHandle GetHandleToExistingFile(FileAccess access, FileOptions options)
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[1]);
            return File.OpenHandle(filePath, FileMode.Open, access, FileShare.None, options);
        }
    }
}
