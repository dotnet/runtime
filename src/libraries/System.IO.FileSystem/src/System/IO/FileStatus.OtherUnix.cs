// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal partial struct FileStatus
    {
        internal void SetCreationTime(string path, DateTimeOffset time)
        {
            // Unix provides APIs to update the last access time (atime) and last modification time (mtime).
            // There is no API to update the CreationTime.
            // Some platforms (e.g. Linux) don't store a creation time. On those platforms, the creation time
            // is synthesized as the oldest of last status change time (ctime) and last modification time (mtime).
            // We update the LastWriteTime (mtime).
            // This triggers a metadata change for FileSystemWatcher NotifyFilters.CreationTime.
            // Updating the mtime, causes the ctime to be set to 'now'. So, on platforms that don't store a
            // CreationTime, GetCreationTime will return the value that was previously set (when that value
            // wasn't in the future).
            SetLastWriteTime(path, time);
        }

        internal void SetLastWriteTime(string path, DateTimeOffset time) => SetAccessOrWriteTime(path, time, isAccessTime: false);
    }
}
