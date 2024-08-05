// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public partial class RandomAccess_FlushToDisk : RandomAccess_Base<long>
    {
        public static IEnumerable<object[]> BufferByteCounts => new[]
        {
            // To ensure that flushing works correctly, we use a wide variety of buffer sizes.
            new object[] { 1                              }, // Single-byte buffer.
            new object[] { Environment.SystemPageSize - 1 }, // Buffer that's slightly smaller than a page.
            new object[] { Environment.SystemPageSize + 1 }, // Buffer that's slightly larger than a page.
            new object[] { Environment.SystemPageSize     }, // Buffer that's exactly one page.
            new object[] { Environment.SystemPageSize * 2 }, // Buffer that's an even multiple of a page.
            new object[] { Environment.SystemPageSize * 7 }, // Buffer that's an odd multiple of a page.
        };

        protected override bool UsesOffsets => false;

        protected override bool ThrowsForUnseekableFile => false;

        protected override long MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
        {
            RandomAccess.FlushToDisk(handle);
            return 0;
        }

        [Theory]
        [MemberData(nameof(BufferByteCounts))]
        public void UpdatesFileLastWriteTime(int bufferByteCount)
        {
            // Sanity check: we expect the byte count to be > 0 so the test uses a non-empty buffer.
            Assert.True(bufferByteCount > 0, $"{nameof(bufferByteCount)} must be > 0.");

            string testFilePath = GetTestFilePath();

            byte[] randomBytes = RandomNumberGenerator.GetBytes(bufferByteCount);

            using (SafeFileHandle handle = File.OpenHandle(testFilePath, FileMode.CreateNew, FileAccess.Write))
            {
                RandomAccess.Write(handle, randomBytes, fileOffset: 0);

                DateTime fileTimeBeforeFlush = File.GetLastWriteTimeUtc(testFilePath);

                // Flush the file to disk. As a bare minimum test, this should work without throwing an
                // exception on all supported platforms. Since testing that the file was actually flushed
                // to disk is difficult without using direct/unbuffered I/O and all the challenges that come
                // with it, we can at least test that the file time was updated and we do that later below.
                // NOTE: the difficulty of testing this is even called out in the POSIX specification for
                // fsync() (see https://pubs.opengroup.org/onlinepubs/009695399/functions/fsync.html).
                RandomAccess.FlushToDisk(handle);

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

        [Fact]
        public void CanFlushFileOpenedForReading()
        {
            string testFilePath = GetTestFilePath();

            const int FileByteCount = 100;
            File.WriteAllBytes(testFilePath, RandomNumberGenerator.GetBytes(FileByteCount));

            using (SafeFileHandle handle = File.OpenHandle(testFilePath, FileMode.Open, FileAccess.Read))
            {
                // On non-Windows platforms (notably Unix), flushing a file opened for reading should succeed.
                // On Windows, the FlushFileBuffers() function does not work with files opened for reading and
                // would return an error. However, we ignore that error to harmonize the behavior across platforms
                // so ultimately, flushing a file opened for reading should not throw an exception on any platform.
                RandomAccess.FlushToDisk(handle);

                // The file length should be unchanged after flushing (we did not write anything else to the file).
                Assert.Equal(FileByteCount, RandomAccess.GetLength(handle));
            }
        }
    }
}
