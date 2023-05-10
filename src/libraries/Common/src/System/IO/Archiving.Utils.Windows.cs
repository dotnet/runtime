// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;

namespace System.IO
{
    internal static partial class ArchivingUtils
    {
        private static readonly SearchValues<char> s_illegalChars = SearchValues.Create(
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000A\u000B\u000C\u000D\u000E\u000F" +
            "\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F" +
            "\"*:<>?|");

        internal static string SanitizeEntryFilePath(string entryPath)
        {
            // Find the first illegal character in the entry path.
            int i = entryPath.AsSpan().IndexOfAny(s_illegalChars);
            if (i < 0)
            {
                // There weren't any characters to sanitize.  Just return the original string.
                return entryPath;
            }

            // We found at least one character that needs to be replaced.
            return string.Create(entryPath.Length, (i, entryPath), static (dest, state) =>
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
                    dest[i] = s_illegalChars.Contains(c) ? '_' : c;
                }
            });
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

#pragma warning disable CS8500 // takes address of managed type
            ReadOnlySpan<char> tmpPath = path; // avoid address exposing the span and impacting the other code in the method that uses it
            return string.Create(appendPathSeparator ? tmpPath.Length + 1 : tmpPath.Length, (appendPathSeparator, RosPtr: (IntPtr)(&tmpPath)), static (dest, state) =>
            {
                var path = *(ReadOnlySpan<char>*)state.RosPtr;
                path.CopyTo(dest);
                if (state.appendPathSeparator)
                {
                    dest[^1] = '/';
                }

                // To ensure tar files remain compatible with Unix, and per the ZIP File Format Specification 4.4.17.1,
                // all slashes should be forward slashes.
                dest.Replace('\\', '/');
            });
#pragma warning restore CS8500
        }
    }
}
