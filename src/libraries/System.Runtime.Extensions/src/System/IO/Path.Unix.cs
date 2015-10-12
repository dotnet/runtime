// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;

namespace System.IO
{
    public static partial class Path
    {
        public static readonly char DirectorySeparatorChar = '/';
        public static readonly char VolumeSeparatorChar = '/';
        public static readonly char PathSeparator = ':';

        private const string DirectorySeparatorCharAsString = "/";

        private static readonly char[] InvalidFileNameChars = { '\0', '/' };

        private static readonly int MaxPath = Interop.Sys.MaxPath;
        private static readonly int MaxLongPath = MaxPath;
        private static readonly int MaxComponentLength = Interop.Sys.MaxName;

        private static bool IsDirectoryOrVolumeSeparator(char c)
        {
            // The directory separator is the same as the volume separator,
            // so we only need to check one.
            Debug.Assert(DirectorySeparatorChar == VolumeSeparatorChar);
            return PathInternal.IsDirectorySeparator(c);
        }

        // Expands the given path to a fully qualified path. 
        public static string GetFullPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            return NormalizePath(path, fullChecks: true);
        }

        private static string NormalizePath(
            string path, bool fullChecks)
        {
            Debug.Assert(path != null);

            if (path.Length == 0)
                throw new ArgumentException(SR.Arg_PathIllegal);

            if (fullChecks)
            {
                PathInternal.CheckInvalidPathChars(path);

                // Expand with current directory if necessary
                if (!IsPathRooted(path))
                {
                    path = Combine(Interop.Sys.GetCwd(), path);
                }
            }

            // We would ideally use realpath to do this, but it resolves symlinks, requires that the file actually exist,
            // and turns it into a full path, which we only want if fullCheck is true.
            string collapsedString = RemoveRelativeSegments(path);

            Debug.Assert(collapsedString.Length < path.Length || collapsedString.ToString() == path,
                "Either we've removed characters, or the string should be unmodified from the input path.");

            if (collapsedString.Length > MaxPath)
            {
                throw new PathTooLongException(SR.IO_PathTooLong);
            }

            string result =
                collapsedString.Length == 0 ? (fullChecks ? DirectorySeparatorCharAsString : string.Empty) :
                collapsedString;

            return result;
        }

        private static string RemoveLongPathPrefix(string path)
        {
            return path; // nop.  There's nothing special about "long" paths on Unix.
        }

        public static string GetTempPath()
        {
            const string TempEnvVar = "TMPDIR";
            const string DefaultTempPath = "/tmp/";

            // Get the temp path from the TMPDIR environment variable.
            // If it's not set, just return the default path.
            // If it is, return it, ensuring it ends with a slash.
            string path = Environment.GetEnvironmentVariable(TempEnvVar);
            return
                string.IsNullOrEmpty(path) ? DefaultTempPath :
                PathInternal.IsDirectorySeparator(path[path.Length - 1]) ? path :
                path + DirectorySeparatorChar;
        }

        private static string InternalGetTempFileName(bool checkHost)
        {
            const string Suffix = ".tmp";
            const int SuffixByteLength = 4;

            // mkstemps takes a char* and overwrites the XXXXXX with six characters
            // that'll result in a unique file name.
            string template = GetTempPath() + "tmpXXXXXX" + Suffix + "\0";
            byte[] name = Encoding.UTF8.GetBytes(template);

            // Create, open, and close the temp file.
            int fd;
            Interop.CheckIo(fd = Interop.Sys.MksTemps(name, SuffixByteLength));
            Interop.Sys.Close(fd); // ignore any errors from close; nothing to do if cleanup isn't possible

            // 'name' is now the name of the file
            Debug.Assert(name[name.Length - 1] == '\0');
            return Encoding.UTF8.GetString(name, 0, name.Length - 1); // trim off the trailing '\0'
        }

        public static bool IsPathRooted(string path)
        {
            if (path == null)
                return false;

            PathInternal.CheckInvalidPathChars(path);
            return path.Length > 0 && path[0] == DirectorySeparatorChar;
        }

        public static string GetPathRoot(string path)
        {
            if (path == null) return null;
            return IsPathRooted(path) ? DirectorySeparatorCharAsString : String.Empty;
        }

        private static byte[] CreateCryptoRandomByteArray(int byteLength)
        {
            var arr = new byte[byteLength];
            if (!Interop.Crypto.GetRandomBytes(arr, arr.Length))
            {
                throw new InvalidOperationException(SR.InvalidOperation_Cryptography);
            }
            return arr;
        }
    }
}
