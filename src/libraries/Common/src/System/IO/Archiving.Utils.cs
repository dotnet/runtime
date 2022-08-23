// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.IO
{
    internal static partial class ArchivingUtils
    {
        // To ensure tar files remain compatible with Unix,
        // and per the ZIP File Format Specification 4.4.17.1,
        // all slashes should be forward slashes.
        private const char PathSeparatorChar = '/';
        private const string PathSeparatorString = "/";

        public static string EntryFromPath(string entry, int offset, int length, ref char[] buffer, bool appendPathSeparator = false)
        {
            Debug.Assert(length <= entry.Length - offset);
            Debug.Assert(buffer != null);

            // Remove any leading slashes from the entry name:
            while (length > 0)
            {
                if (entry[offset] != Path.DirectorySeparatorChar &&
                    entry[offset] != Path.AltDirectorySeparatorChar)
                    break;

                offset++;
                length--;
            }

            if (length == 0)
                return appendPathSeparator ? PathSeparatorString : string.Empty;

            int resultLength = appendPathSeparator ? length + 1 : length;
            EnsureCapacity(ref buffer, resultLength);
            entry.CopyTo(offset, buffer, 0, length);

            // '/' is a more broadly recognized directory separator on all platforms (eg: mac, linux)
            // We don't use Path.DirectorySeparatorChar or AltDirectorySeparatorChar because this is
            // explicitly trying to standardize to '/'
            for (int i = 0; i < length; i++)
            {
                char ch = buffer[i];
                if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar)
                    buffer[i] = PathSeparatorChar;
            }

            if (appendPathSeparator)
                buffer[length] = PathSeparatorChar;

            return new string(buffer, 0, resultLength);
        }

        public static void EnsureCapacity(ref char[] buffer, int min)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(min > 0);

            if (buffer.Length < min)
            {
                int newCapacity = buffer.Length * 2;
                if (newCapacity < min)
                    newCapacity = min;

                char[] oldBuffer = buffer;
                buffer = ArrayPool<char>.Shared.Rent(newCapacity);
                ArrayPool<char>.Shared.Return(oldBuffer);
            }
        }

        public static bool IsDirEmpty(DirectoryInfo possiblyEmptyDir)
        {
            using (IEnumerator<string> enumerator = Directory.EnumerateFileSystemEntries(possiblyEmptyDir.FullName).GetEnumerator())
                return !enumerator.MoveNext();
        }

        public static void AttemptSetLastWriteTime(string destinationFileName, DateTimeOffset lastWriteTime)
        {
            try
            {
                File.SetLastWriteTime(destinationFileName, lastWriteTime.DateTime);
            }
            catch
            {
                // Some OSes like Android (#35374) might not support setting the last write time, the extraction should not fail because of that
            }
        }
    }
}
