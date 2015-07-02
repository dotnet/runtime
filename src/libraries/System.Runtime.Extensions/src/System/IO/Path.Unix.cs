// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

// TODO: This partial implementation is currently stubbed out and should not be
//       included in the .csproj until completed.

namespace System.IO
{
    public static partial class Path
    {
        public static readonly char DirectorySeparatorChar = '/';
        public static readonly char VolumeSeparatorChar = '/';
        public static readonly char PathSeparator = ':';

        private const string DirectorySeparatorCharAsString = "/";

        private static readonly char[] RealInvalidPathChars = { '\0' };
        private static readonly char[] InvalidPathCharsWithAdditionalChecks = { '\0', '*', '?' }; // Used by HasIllegalCharacters
        private static readonly char[] InvalidFileNameChars = { '\0', '/' };

        // TODO: Implement these values correctly (dynamically) for Unix
        private static readonly int MaxPath = 4096;
        private static readonly int MaxComponentLength = 255;

        private static bool IsDirectorySeparator(char c)
        {
            // The alternatie directory separator char is the same as the directory separator,
            // so we only need to check one.
            Debug.Assert(DirectorySeparatorChar == AltDirectorySeparatorChar);
            return c == DirectorySeparatorChar;
        }

        private static bool IsDirectoryOrVolumeSeparator(char c)
        {
            // The directory separator is the same as the volume separator,
            // so we only need to check one.
            Debug.Assert(DirectorySeparatorChar == VolumeSeparatorChar);
            return IsDirectorySeparator(c);
        }

        private static void EmulateFileIOPermissionChecks(string fullPath)
        {
            CheckInvalidPathChars(fullPath, true);
        }

        private static string NormalizePath(
            string path, bool fullCheck, 
            int maxPathLength, bool expandShortPaths) // ignored on Unix
        {
            Debug.Assert(path != null);

            // If we're doing a full path check, look for illegal path characters.
            if (fullCheck)
            {
                CheckInvalidPathChars(path);
            }

            // TODO: Implement this
            throw new NotImplementedException();
        }

        private static string RemoveLongPathPrefix(string path)
        {
            return path; // nop.  There's nothing special about "long" paths on Unix.
        }

        public static string GetTempPath()
        {
            // TODO: Implement this
            throw new NotImplementedException();
        }

        private static string InternalGetTempFileName(bool checkHost)
        {
            // TODO: Implement this
            throw new NotImplementedException();
        }s

        public static bool IsPathRooted(string path)
        {
            if (path == null)
                return false;

            CheckInvalidPathChars(path);
            return path.Length > 0 && path[0] == DirectorySeparatorChar;
        }

        private static int GetRootLength(string path)
        {
            CheckInvalidPathChars(path);
            return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
        }

        private static byte[] CreateCryptoRandomByteArray(int byteLength)
        {
            // TODO: Implement this
            throw new NotImplementedException();
        }
    }

}
