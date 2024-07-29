// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal static partial class ArchivingUtils
    {
#pragma warning disable IDE0060 // preserveDriveRoot is unused.
        internal static string SanitizeEntryFilePath(string entryPath, bool preserveDriveRoot = false) => entryPath.Replace('\0', '_');
#pragma warning restore IDE0060

        public static unsafe string EntryFromPath(ReadOnlySpan<char> path, bool appendPathSeparator = false)
        {
            // Remove leading separators.
            int nonSlash = path.IndexOfAnyExcept('/');
            if (nonSlash < 0)
            {
                nonSlash = path.Length;
            }
            path = path.Slice(nonSlash);

            // Append a separator if necessary.
            return (path.IsEmpty, appendPathSeparator) switch
            {
                (false, false) => path.ToString(),
                (false, true) => string.Concat(path, "/"),
                (true, false) => string.Empty,
                (true, true) => "/",
            };
        }
    }
}
