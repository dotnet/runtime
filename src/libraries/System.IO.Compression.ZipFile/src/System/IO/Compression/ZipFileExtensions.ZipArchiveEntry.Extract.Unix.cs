// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    public static partial class ZipFileExtensions
    {
        static partial void ExtractExternalAttributes(FileStream fs, ZipArchiveEntry entry)
        {
            // Only extract USR, GRP, and OTH file permissions, and ignore
            // S_ISUID, S_ISGID, and S_ISVTX bits. This matches unzip's default behavior.
            // It is off by default because of this comment:

            // "It's possible that a file in an archive could have one of these bits set
            // and, unknown to the person unzipping, could allow others to execute the
            // file as the user or group. The new option -K bypasses this check."
            const int ExtractPermissionMask = 0x1FF;
            int permissions = (entry.ExternalAttributes >> 16) & ExtractPermissionMask;

            // If the permissions weren't set at all, don't write the file's permissions,
            // since the .zip could have been made using a previous version of .NET, which didn't
            // include the permissions, or was made on Windows.
            if (permissions != 0)
            {
                Interop.CheckIo(Interop.Sys.FChMod(fs.SafeFileHandle, permissions), fs.Name);
            }
        }
    }
}
