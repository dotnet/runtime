// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public partial class RandomAccess_FlushToDisk : RandomAccess_Base<long>
    {
        // Setting this to false disables the ThrowsArgumentOutOfRangeExceptionForNegativeFileOffset() test
        // which is not applicable to FlushToDisk() since FlushToDisk() does not take a file offset parameter.
        protected override bool UsesOffsets => false;

        // Setting this to false disables the ThrowsNotSupportedExceptionForUnseekableFile() test which is not
        // applicable to FlushToDisk() since FlushToDisk() DOES in fact support unseekable files.
        protected override bool ThrowsForUnseekableFile => false;

        protected override long MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
        {
            // NOTE: tests for checking how FlushToDisk() deals with invalid arguments (e.g. a null handle)
            // are implemented in the base class and work by calling this "MethodUnderTest" which we override
            // here to call the FlushToDisk() method we want to test.
            RandomAccess.FlushToDisk(handle);
            return 0;
        }

        [Fact]
        public void UpdatesFileLastWriteTime()
        {
            // Save test file path so we can refer to the same file throughout the test.
            string testFilePath = GetTestFilePath();

            // Generate random bytes to write to file. To ensure that flushing works correctly for a variety of
            // sizes, we want to test with buffers that are smaller than a page (e.g. just 1 byte) and buffers
            // that are several times larger than a page (e.g. up to 10 pages).
            int byteCount = Random.Shared.Next(1, Environment.SystemPageSize * 10);
            byte[] randomBytes = RandomNumberGenerator.GetBytes(byteCount);

            // Create a new file and open it for writing.
            using (SafeFileHandle handle = File.OpenHandle(testFilePath, FileMode.CreateNew, FileAccess.Write))
            {
                // Write random bytes to file.
                RandomAccess.Write(handle, randomBytes, fileOffset: 0);

                // Get the file time BEFORE flushing to disk.
                DateTime fileTimeBeforeFlush = File.GetLastWriteTimeUtc(testFilePath);

                // Flush the file to disk. As a bare minimum test, this should work without throwing an
                // exception on all supported platforms. Since testing that the file was actually flushed
                // to disk is difficult without using direct/unbuffered I/O and all the challenges that come
                // with it, we can at least test that the file time was updated and we do that later below.
                // NOTE: the difficulty of testing this is even called out in the POSIX specification for
                // fsync() (see https://pubs.opengroup.org/onlinepubs/009695399/functions/fsync.html).
                RandomAccess.FlushToDisk(handle);

                // Get the file time AFTER flushing to disk.
                DateTime fileTimeAfterFlush = File.GetLastWriteTimeUtc(testFilePath);

                // After explicitly flushing to disk, we expect "fileTimeAfterFlush > fileTimeBeforeFlush".
                // However, the OS is free to flush the file to disk at any moment after the file is written
                // to. This "implicit flush" would also update the file time and if it happens BEFORE we set the
                // fileTimeBeforeFlush variable above, then both fileTimeBeforeFlush and fileTimeAfterFlush will
                // end up with the same value, which is why we use the ">=" operator below instead of using ">".
                Assert.True(fileTimeAfterFlush >= fileTimeBeforeFlush);
            }
        }

        [Fact]
        public void CanFlushWithoutWriting()
        {
            // Create a new file and open it for writing.
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write))
            {
                // Flush the file to disk. NOTE: we have created a file but have not written anything to it yet
                // so there is nothing to flush to disk. This should succeed without throwing an exception.
                RandomAccess.FlushToDisk(handle);

                // Furthermore, the file length should be 0 bytes after flushing to disk since we have not written
                // anything to the file yet.
                Assert.Equal(0, RandomAccess.GetLength(handle));
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "System.IO.Pipes aren't supported on browser")]
        public void CanFlushUnseekableFile()
        {
            using (var server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (SafeFileHandle handle = new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), ownsHandle: false))
            {
                // Flushing a non-seekable handle (in this case, a pipe handle) should work without throwing an
                // exception. On Windows, the FlushFileBuffers() function DOES work with non-seekable handles
                // (e.g. pipe handles) and that is what we are testing here. The fsync() function on Unix does
                // NOT support non-seekable handles but no exception is thrown on Unix either because we silently
                // ignore the errors effectively making the call below a no-op.
                RandomAccess.FlushToDisk(handle);
            }
        }
    }
}
