// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal partial struct FileStatus
    {
        internal void SetCreationTime(string path, DateTimeOffset time, bool asDirectory) =>
            SetLastWriteTime(path, time, asDirectory);

        private void SetAccessOrWriteTime(string path, DateTimeOffset time, bool isAccessTime, bool asDirectory) =>
            SetAccessOrWriteTimeCore(path, time, isAccessTime, checkCreationTime: false, asDirectory);

        // This is not used on these platforms, but is needed for source compat
        private static Interop.Error SetCreationTimeCore(string path, long seconds, long nanoseconds) =>
            throw new InvalidOperationException();
    }
}
