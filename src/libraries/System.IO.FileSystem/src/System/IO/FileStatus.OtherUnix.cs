// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal partial struct FileStatus
    {
        internal void SetCreationTime(string path, DateTimeOffset time) => SetCreationTime_StandardUnixImpl(path, time);
        internal void SetLastWriteTime(string path, DateTimeOffset time) => SetLastWriteTime_StandardUnixImpl(path, time);
        internal void SetLastAccessTime(string path, DateTimeOffset time) => SetLastAccessTime_StandardUnixImpl(path, time);
    }
}
