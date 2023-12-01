// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class File_GetSetTimes_SafeFileHandle : File_GetSetTimes
    {
        protected virtual SafeFileHandle OpenFileHandle(string path, FileAccess fileAccess) =>
            File.OpenHandle(
                path,
                FileMode.OpenOrCreate,
                fileAccess,
                FileShare.ReadWrite);

        protected override bool CanBeReadOnly => false;

        protected override bool ApiTargetsLink => false;

        protected override void SetCreationTime(string path, DateTime creationTime)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetCreationTime(fileHandle, creationTime);
        }

        protected override DateTime GetCreationTime(string path)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetCreationTime(fileHandle);
        }

        protected override void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetCreationTimeUtc(fileHandle, creationTimeUtc);
        }

        protected override DateTime GetCreationTimeUtc(string path)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetCreationTimeUtc(fileHandle);
        }

        protected override void SetLastAccessTime(string path, DateTime lastAccessTime)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetLastAccessTime(fileHandle, lastAccessTime);
        }

        protected override DateTime GetLastAccessTime(string path)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetLastAccessTime(fileHandle);
        }

        protected override void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetLastAccessTimeUtc(fileHandle, lastAccessTimeUtc);
        }

        protected override DateTime GetLastAccessTimeUtc(string path)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetLastAccessTimeUtc(fileHandle);
        }

        protected override void SetLastWriteTime(string path, DateTime lastWriteTime)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetLastWriteTime(fileHandle, lastWriteTime);
        }

        protected override DateTime GetLastWriteTime(string path)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetLastWriteTime(fileHandle);
        }

        protected override void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetLastWriteTimeUtc(fileHandle, lastWriteTimeUtc);
        }

        protected override DateTime GetLastWriteTimeUtc(string path)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetLastWriteTimeUtc(fileHandle);
        }

        [Fact]
        public async Task WritingShouldUpdateWriteTime_After_SetLastAccessTime()
        {
            string filePath = GetTestFilePath();
            using SafeFileHandle handle = OpenFileHandle(filePath, FileAccess.ReadWrite);

            File.SetLastAccessTime(handle, DateTime.Now.Subtract(TimeSpan.FromDays(1)));
            DateTime timeAfterWrite = default, timeBeforeWrite = File.GetLastWriteTime(handle);

            // According to https://learn.microsoft.com/en-us/windows/win32/api/fileapi/ns-fileapi-win32_file_attribute_data
            // write time has a resolution of 2 seconds on FAT. Let's wait a little bit longer.
            for (int i = 0; i <= 5 && timeBeforeWrite >= timeAfterWrite; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(i));

                await RandomAccess.WriteAsync(handle, new byte[1] { 1 }, fileOffset: i);

                timeAfterWrite = File.GetLastWriteTime(handle);
            }

            AssertExtensions.GreaterThan(timeAfterWrite, timeBeforeWrite);
        }

        [Fact]
        public void NullArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.GetCreationTime(default(SafeFileHandle)!));
            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.SetCreationTime(default(SafeFileHandle)!, DateTime.Now));

            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.GetCreationTimeUtc(default(SafeFileHandle)!));
            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.SetCreationTimeUtc(default(SafeFileHandle)!, DateTime.Now));

            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.GetLastAccessTime(default(SafeFileHandle)!));
            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.SetLastAccessTime(default(SafeFileHandle)!, DateTime.Now));

            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.GetLastAccessTimeUtc(default(SafeFileHandle)!));
            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.SetLastAccessTimeUtc(default(SafeFileHandle)!, DateTime.Now));

            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.GetLastWriteTime(default(SafeFileHandle)!));
            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.SetLastWriteTime(default(SafeFileHandle)!, DateTime.Now));

            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.GetLastWriteTimeUtc(default(SafeFileHandle)!));
            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.SetLastWriteTimeUtc(default(SafeFileHandle)!, DateTime.Now));
        }
    }
}
