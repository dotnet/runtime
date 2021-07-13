// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    public static partial class ZipFileExtensions
    {
        static partial void SetExternalAttributes(FileStream fs, ZipArchiveEntry entry)
        {
            Interop.Sys.FileStatus status;
            if (Interop.Sys.FStat(fs.SafeFileHandle, out status) != 0)
            {
                Interop.CheckIO(Interop.Sys.GetLastErrorInfo(), fs.Name);
            }

            entry.ExternalAttributes |= status.Mode << 16;
        }
    }
}
