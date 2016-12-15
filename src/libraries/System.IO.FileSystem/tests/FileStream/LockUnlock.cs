// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_LockUnlock : RemoteExecutorTestBase
    {
        [Fact]
        public void InvalidArgs_Throws()
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[100]);

            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                Assert.Throws<ArgumentOutOfRangeException>("position", () => fs.Lock(-1, 1));
                Assert.Throws<ArgumentOutOfRangeException>("position", () => fs.Lock(-1, -1));
                Assert.Throws<ArgumentOutOfRangeException>("length", () => fs.Lock(0, -1));

                Assert.Throws<ArgumentOutOfRangeException>("position", () => fs.Unlock(-1, 1));
                Assert.Throws<ArgumentOutOfRangeException>("position", () => fs.Unlock(-1, -1));
                Assert.Throws<ArgumentOutOfRangeException>("length", () => fs.Unlock(0, -1));
            }
        }

        [Fact]
        public void FileClosed_Throws()
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[100]);

            FileStream fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            fs.Dispose();
            Assert.Throws<ObjectDisposedException>(() => fs.Lock(0, 1));
        }

        [ActiveIssue(5964, TestPlatforms.OSX)]
        [Theory]
        [InlineData(100, 0, 100)]
        [InlineData(200, 0, 100)]
        [InlineData(200, 50, 150)]
        [InlineData(200, 100, 100)]
        [InlineData(20, 2000, 1000)]
        public void Lock_Unlock_Successful(long fileLength, long position, long length)
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[fileLength]);

            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                fs.Lock(position, length);
                fs.Unlock(position, length);
            }
        }

        [ActiveIssue(5964, TestPlatforms.OSX)]
        [Theory]
        [InlineData(10, 0, 2, 3, 5)]
        public void NonOverlappingRegions_Success(long fileLength, long firstPosition, long firstLength, long secondPosition, long secondLength)
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[fileLength]);

            using (FileStream fs1 = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (FileStream fs2 = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                // Lock and unlock the respective regions a few times
                for (int i = 0; i < 2; i++)
                {
                    fs1.Lock(firstPosition, firstLength);
                    fs2.Lock(secondPosition, secondLength);
                    fs1.Unlock(firstPosition, firstLength);
                    fs2.Unlock(secondPosition, secondLength);
                }

                // Then swap and do it again.
                for (int i = 0; i < 2; i++)
                {
                    fs2.Lock(firstPosition, firstLength);
                    fs1.Lock(secondPosition, secondLength);
                    fs1.Unlock(secondPosition, secondLength);
                    fs2.Unlock(firstPosition, firstLength);
                }
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix locks are on a per-process basis, so overlapping locks from the same process are allowed.
        [InlineData(10, 0, 10, 1, 2)]
        [InlineData(10, 3, 5, 3, 5)]
        [InlineData(10, 3, 5, 3, 4)]
        [InlineData(10, 3, 5, 4, 5)]
        [InlineData(10, 3, 5, 2, 6)]
        [InlineData(10, 3, 5, 2, 4)]
        [InlineData(10, 3, 5, 4, 6)]
        public void OverlappingRegionsFromSameProcess_ThrowsExceptionOnWindows(long fileLength, long firstPosition, long firstLength, long secondPosition, long secondLength)
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[fileLength]);

            using (FileStream fs1 = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (FileStream fs2 = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                fs1.Lock(firstPosition, firstLength);
                Assert.Throws<IOException>(() => fs2.Lock(secondPosition, secondLength));
                fs1.Unlock(firstPosition, firstLength);

                fs2.Lock(secondPosition, secondLength);
                fs2.Unlock(secondPosition, secondLength);
            }
        }

        [ActiveIssue(5964, TestPlatforms.OSX)]
        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Unix locks are on a per-process basis, so overlapping locks from the same process are allowed.
        [InlineData(10, 0, 10, 1, 2)]
        [InlineData(10, 3, 5, 3, 5)]
        [InlineData(10, 3, 5, 3, 4)]
        [InlineData(10, 3, 5, 4, 5)]
        [InlineData(10, 3, 5, 2, 6)]
        [InlineData(10, 3, 5, 2, 4)]
        [InlineData(10, 3, 5, 4, 6)]
        public void OverlappingRegionsFromSameProcess_AllowedOnUnix(long fileLength, long firstPosition, long firstLength, long secondPosition, long secondLength)
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[fileLength]);

            using (FileStream fs1 = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (FileStream fs2 = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                fs1.Lock(firstPosition, firstLength);
                fs2.Lock(secondPosition, secondLength);
                fs1.Unlock(firstPosition, firstLength);
                fs2.Unlock(secondPosition, secondLength);
            }
        }

        [ActiveIssue(5964, TestPlatforms.OSX)]
        [Theory]
        [InlineData(10, 0, 10, 1, 2)]
        [InlineData(10, 3, 5, 3, 5)]
        [InlineData(10, 3, 5, 3, 4)]
        [InlineData(10, 3, 5, 4, 5)]
        [InlineData(10, 3, 5, 2, 6)]
        [InlineData(10, 3, 5, 2, 4)]
        [InlineData(10, 3, 5, 4, 6)]
        public void OverlappingRegionsFromOtherProcess_ThrowsException(long fileLength, long firstPosition, long firstLength, long secondPosition, long secondLength)
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[fileLength]);

            using (FileStream fs1 = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                fs1.Lock(firstPosition, firstLength);

                RemoteInvoke((secondPath, secondPos, secondLen) =>
                {
                    using (FileStream fs2 = File.Open(secondPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        Assert.Throws<IOException>(() => fs2.Lock(long.Parse(secondPos), long.Parse(secondLen)));
                    }
                    return SuccessExitCode;
                }, path, secondPosition.ToString(), secondLength.ToString()).Dispose();

                fs1.Unlock(firstPosition, firstLength);
                RemoteInvoke((secondPath, secondPos, secondLen) =>
                {
                    using (FileStream fs2 = File.Open(secondPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        fs2.Lock(long.Parse(secondPos), long.Parse(secondLen));
                        fs2.Unlock(long.Parse(secondPos), long.Parse(secondLen));
                    }
                    return SuccessExitCode;
                }, path, secondPosition.ToString(), secondLength.ToString()).Dispose();
            }
        }
    }
}
