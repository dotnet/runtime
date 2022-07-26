// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public static partial class Directory
    {
        private static DirectoryInfo CreateDirectoryCore(string path, UnixFileMode unixCreateMode)
            => throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);
    }
}
