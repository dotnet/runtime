// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Tests
{
    public sealed class File_GetSetTimes_SafeFileHandle : File_GetSetTimes
    {
        private static SafeFileHandle OpenFileHandle(string path) => File.OpenHandle(
            path,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite);

        protected override void SetCreationTime(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetCreationTime(fileHandle, creationTime);
        }

        protected override DateTime GetCreationTime(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetCreationTime(fileHandle);
        }

        protected override void SetCreationTimeUtc(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetCreationTimeUtc(fileHandle, creationTime);
        }

        protected override DateTime GetCreationTimeUtc(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetCreationTimeUtc(fileHandle);
        }

        protected override void SetLastAccessTime(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetLastAccessTime(fileHandle, creationTime);
        }

        protected override DateTime GetLastAccessTime(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetLastAccessTime(fileHandle);
        }

        protected override void SetLastAccessTimeUtc(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetLastAccessTimeUtc(fileHandle, creationTime);
        }

        protected override DateTime GetLastAccessTimeUtc(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetLastAccessTimeUtc(fileHandle);
        }

        protected override void SetLastWriteTime(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetLastWriteTime(fileHandle, creationTime);
        }

        protected override DateTime GetLastWriteTime(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetLastWriteTime(fileHandle);
        }

        protected override void SetLastWriteTimeUtc(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetLastWriteTimeUtc(fileHandle, creationTime);
        }

        protected override DateTime GetLastWriteTimeUtc(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetLastWriteTimeUtc(fileHandle);
        }

        protected override void SettingUpdatesPropertiesCore(string item)
        {
            // Override `SettingUpdatesPropertiesCore`, because SafeFileHandles are not able to do this
        }
    }
}
