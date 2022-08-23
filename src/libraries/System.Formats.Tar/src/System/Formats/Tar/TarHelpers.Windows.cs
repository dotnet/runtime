// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Formats.Tar
{
    internal static partial class TarHelpers
    {
        internal static SortedDictionary<string, UnixFileMode>? CreatePendingModesDictionary()
            => null;

        internal static void CreateDirectory(string fullPath, UnixFileMode? mode, SortedDictionary<string, UnixFileMode>? pendingModes)
            => Directory.CreateDirectory(fullPath);

        internal static void SetPendingModes(SortedDictionary<string, UnixFileMode>? pendingModes)
            => Debug.Assert(pendingModes is null);

        internal static unsafe string EntryFromPath(ReadOnlySpan<char> path, bool appendPathSeparator = false)
        {
            // Remove leading separators.
            int nonSlash = path.IndexOfAnyExcept('/', '\\');
            if (nonSlash == -1)
            {
                nonSlash = path.Length;
            }
            path = path.Slice(nonSlash);

            // Replace \ with /, and append a separator if necessary.

            if (path.IsEmpty)
            {
                return appendPathSeparator ?
                    "/" :
                    string.Empty;
            }

            fixed (char* pathPtr = &MemoryMarshal.GetReference(path))
            {
                return string.Create(appendPathSeparator ? path.Length + 1 : path.Length, (appendPathSeparator, (IntPtr)pathPtr, path.Length), static (dest, state) =>
                {
                    ReadOnlySpan<char> path = new ReadOnlySpan<char>((char*)state.Item2, state.Length);
                    path.CopyTo(dest);
                    if (state.appendPathSeparator)
                    {
                        dest[^1] = '/';
                    }

                    // To ensure tar files remain compatible with Unix, and per the ZIP File Format Specification 4.4.17.1,
                    // all slashes should be forward slashes.
                    int pos;
                    while ((pos = dest.IndexOf('\\')) >= 0)
                    {
                        dest[pos] = '/';
                        dest = dest.Slice(pos + 1);
                    }
                });
            }
        }
    }
}
