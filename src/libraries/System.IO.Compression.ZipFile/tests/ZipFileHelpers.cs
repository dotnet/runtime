// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Compression.Tests
{
    public static class ZipFileHelpers
    {
        internal static void EnsureFilePermissions(string filename, string permissions)
        {
            Interop.Sys.FileStatus status;
            Assert.Equal(0, Interop.Sys.Stat(filename, out status));

            // note that we don't extract S_ISUID, S_ISGID, and S_ISVTX bits,
            // so only use the last 3 numbers of permissions to verify the file permissions
            permissions = permissions.Length > 3 ? permissions.Substring(permissions.Length - 3) : permissions;
            Assert.Equal(Convert.ToInt32(permissions, 8), status.Mode & 0xFFF);
        }
    }
}
