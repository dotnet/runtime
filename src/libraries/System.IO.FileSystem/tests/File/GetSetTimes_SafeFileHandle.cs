
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Tests
{
    public sealed class File_GetSetTimes_SafeFileHandle : File_GetSetTimes
    {
        private static SafeFileHandle OpenFileHandle(string path, FileAccess fileAccess) =>
            File.OpenHandle(
                path,
                FileMode.OpenOrCreate,
                fileAccess,
                FileShare.ReadWrite);

        protected override bool CanBeReadOnly => false;

        protected override bool SettingPropertiesUpdatesLink => false;

        protected override void SetCreationTime(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetCreationTime(fileHandle, creationTime);
        }

        protected override DateTime GetCreationTime(string path)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetCreationTime(fileHandle);
        }

        protected override void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetCreationTimeUtc(fileHandle, creationTimeUtc);
        }

        protected override DateTime GetCreationTimeUtc(string path)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetCreationTimeUtc(fileHandle);
        }

        protected override void SetLastAccessTime(string path, DateTime lastAccessTime)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetLastAccessTime(fileHandle, lastAccessTime);
        }

        protected override DateTime GetLastAccessTime(string path)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetLastAccessTime(fileHandle);
        }

        protected override void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetLastAccessTimeUtc(fileHandle, lastAccessTimeUtc);
        }

        protected override DateTime GetLastAccessTimeUtc(string path)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetLastAccessTimeUtc(fileHandle);
        }

        protected override void SetLastWriteTime(string path, DateTime lastWriteTime)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetLastWriteTime(fileHandle, lastWriteTime);
        }

        protected override DateTime GetLastWriteTime(string path)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetLastWriteTime(fileHandle);
        }

        protected override void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetLastWriteTimeUtc(fileHandle, lastWriteTimeUtc);
        }

        protected override DateTime GetLastWriteTimeUtc(string path)
        {
            using var fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetLastWriteTimeUtc(fileHandle);
        }
    }
}
