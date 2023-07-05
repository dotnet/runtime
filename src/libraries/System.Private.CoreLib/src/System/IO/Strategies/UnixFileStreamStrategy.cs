// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal sealed partial class UnixFileStreamStrategy : OSFileStreamStrategy
    {
        internal UnixFileStreamStrategy(SafeFileHandle handle, FileAccess access) : base(handle, access)
        {
        }

        internal UnixFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize, UnixFileMode? unixCreateMode) :
            base(path, mode, access, share, options, preallocationSize, unixCreateMode)
        {
        }
    }
}
