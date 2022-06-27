// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.IO.Compression
{
    public static partial class ZipFileExtensions
    {
        static partial void SetExternalAttributes(FileStream fs, ZipArchiveEntry entry)
        {
            Debug.Assert(!OperatingSystem.IsWindows());

            Interop.Sys.FileStatus status;
            Interop.CheckIo(Interop.Sys.FStat(fs.SafeFileHandle, out status), fs.Name);

            entry.ExternalAttributes = status.Mode << 16;
        }
    }
}
