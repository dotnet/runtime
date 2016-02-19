// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                throw new ArgumentNullException(nameof(path));

            if (path.Length == 0)
                throw new ArgumentException(SR.Arg_PathIllegal);

            PathInternal.CheckInvalidPathChars(path);

            // Expand with current directory if necessary
            if (!IsPathRooted(path))
            {
                path = Combine(Interop.Sys.GetCwd(), path);
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

            string result = collapsedString.Length == 0 ? DirectorySeparatorCharAsString : collapsedString;

            return result;
        }

        /// <summary>
        /// Try to remove relative segments from the given path (without combining with a root).
        /// </summary>
        /// <param name="skip">Skip the specified number of characters before evaluating.</param>
        private static string RemoveRelativeSegments(string path, int skip = 0)
        {
            bool flippedSeparator = false;

            // Remove "//", "/./", and "/../" from the path by copying each character to the output, 
            // except the ones we're removing, such that the builder contains the normalized path 
            // at the end.
            var sb = StringBuilderCache.Acquire(path.Length);
            if (skip > 0)
            {
                sb.Append(path, 0, skip);
            }

            int componentCharCount = 0;
            for (int i = skip; i < path.Length; i++)
            {
                char c = path[i];

                if (PathInternal.IsDirectorySeparator(c) && i + 1 < path.Length)
                {
                    componentCharCount = 0;

                    // Skip this character if it's a directory separator and if the next character is, too,
                    // e.g. "parent//child" => "parent/child"
                    if (PathInternal.IsDirectorySeparator(path[i + 1]))
                    {
                        continue;
                    }

                    // Skip this character and the next if it's referring to the current directory,
                    // e.g. "parent/./child" =? "parent/child"
                    if ((i + 2 == path.Length || PathInternal.IsDirectorySeparator(path[i + 2])) &&
                        path[i + 1] == '.')
                    {
                        i++;
                        continue;
                    }

                    // Skip this character and the next two if it's referring to the parent directory,
                    // e.g. "parent/child/../grandchild" => "parent/grandchild"
                    if (i + 2 < path.Length &&
                        (i + 3 == path.Length || PathInternal.IsDirectorySeparator(path[i + 3])) &&
                        path[i + 1] == '.' && path[i + 2] == '.')
                    {
                        // Unwind back to the last slash (and if there isn't one, clear out everything).
                        int s;
                        for (s = sb.Length - 1; s >= 0; s--)
                        {
                            if (PathInternal.IsDirectorySeparator(sb[s]))
                            {
                                sb.Length = s;
                                break;
                            }
                        }
                        if (s < 0)
                        {
                            sb.Length = 0;
                        }

                        i += 2;
                        continue;
                    }
                }

                if (++componentCharCount > PathInternal.MaxComponentLength)
                {
                    throw new PathTooLongException(SR.IO_PathTooLong);
                }

                // Normalize the directory separator if needed
                if (c != Path.DirectorySeparatorChar && c == Path.AltDirectorySeparatorChar)
                {
                    c = Path.DirectorySeparatorChar;
                    flippedSeparator = true;
                }

                sb.Append(c);
            }

            if (flippedSeparator || sb.Length != path.Length)
            {
                return StringBuilderCache.GetStringAndRelease(sb);
            }
            else
            {
                // We haven't changed the source path, return the original
                StringBuilderCache.Release(sb);
                return path;
            }
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
            IntPtr fd = Interop.CheckIo(Interop.Sys.MksTemps(name, SuffixByteLength));
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
