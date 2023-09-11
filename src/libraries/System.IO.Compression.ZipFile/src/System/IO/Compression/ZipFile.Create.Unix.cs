// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Enumeration;

namespace System.IO.Compression
{
    public static partial class ZipFile
    {
        private static FileSystemEnumerable<(string, CreateEntryType)> CreateEnumerableForCreate(string directoryFullPath)
            => new FileSystemEnumerable<(string, CreateEntryType)>(directoryFullPath,
                static (ref FileSystemEntry entry) =>
                    {
                        string fullPath = entry.ToFullPath();

                        int type;
                        if (entry.IsDirectory) // entry is a directory, or a link to a directory.
                        {
                            type = Interop.Sys.FileTypes.S_IFDIR;
                        }
                        else
                        {
                            // Use 'stat' to follow links.
                            Interop.CheckIo(Interop.Sys.Stat(fullPath, out Interop.Sys.FileStatus status), fullPath);
                            type = (status.Mode & Interop.Sys.FileTypes.S_IFMT);
                        }

                        return type switch
                        {
                            Interop.Sys.FileTypes.S_IFREG => (fullPath, CreateEntryType.File),
                            Interop.Sys.FileTypes.S_IFDIR => (fullPath, CreateEntryType.Directory),
                            _                             => (fullPath, CreateEntryType.Unsupported)
                        };
                    },
                    new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0, IgnoreInaccessible = false });
    }
}
