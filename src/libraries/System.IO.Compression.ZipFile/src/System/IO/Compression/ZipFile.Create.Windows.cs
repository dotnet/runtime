// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Enumeration;

namespace System.IO.Compression
{
    public static partial class ZipFile
    {
        private static FileSystemEnumerable<(string, CreateEntryType)> CreateEnumerableForCreate(string directoryFullPath)
            => new FileSystemEnumerable<(string, CreateEntryType)>(directoryFullPath,
                static (ref FileSystemEntry entry) => (entry.ToFullPath(), entry.IsDirectory ? CreateEntryType.Directory : CreateEntryType.File),
                new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0, IgnoreInaccessible = false });
    }
}
