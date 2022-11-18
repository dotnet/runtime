// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    internal static partial class ArchivingUtils
    {
        internal static string SanitizeEntryFilePath(string entryPath)
        {
            // Find the first illegal character in the entry path.
            for (int i = 0; i < entryPath.Length; i++)
            {
                switch (entryPath[i])
                {
                    // We found at least one character that needs to be replaced.
                    case < (char)32 or '?' or ':' or '*' or '"' or '<' or '>' or '|':
                        return string.Create(entryPath.Length, (i, entryPath), (dest, state) =>
                        {
                            string entryPath = state.entryPath;

                            // Copy over to the new string everything until the character, then
                            // substitute for the found character.
                            entryPath.AsSpan(0, state.i).CopyTo(dest);
                            dest[state.i] = '_';

                            // Continue looking for and replacing any more illegal characters.
                            for (int i = state.i + 1; i < entryPath.Length; i++)
                            {
                                char c = entryPath[i];
                                dest[i] = c switch
                                {
                                    < (char)32 or '?' or ':' or '*' or '"' or '<' or '>' or '|' => '_',
                                    _ => c,
                                };
                            }
                        });
                }
            }

            // There weren't any characters to sanitize.  Just return the original string.
            return entryPath;
        }

        public static unsafe string EntryFromPath(ReadOnlySpan<char> path, bool appendPathSeparator = false)
        {
            // Remove leading separators.
            int nonSlash = path.IndexOfAnyExcept('/', '\\');
            if (nonSlash < 0)
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
                    dest.Replace('\\', '/');
                });
            }
        }
    }
}
