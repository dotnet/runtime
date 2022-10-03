// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using static System.IO.Compression.ZipArchiveEntryConstants;

namespace System.IO.Compression
{
    public static partial class ZipFileExtensions
    {
        static partial void SetExternalAttributes(FileStream fs, ZipArchiveEntry entry)
        {
            Debug.Assert(!OperatingSystem.IsWindows());

            // SetExternalAttributes is only used to overwrite the default values when reading on the unix systems
            // This assert ensures that nothing else sets values different to the default before
            // overwriting it with the data read from the files
            Debug.Assert(entry.ExternalAttributes == ((uint)DefaultFileEntryPermissions << 16));

            Interop.Sys.FileStatus status;
            Interop.CheckIo(Interop.Sys.FStat(fs.SafeFileHandle, out status), fs.Name);

            entry.ExternalAttributes = status.Mode << 16;
        }
    }
}
