// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public static partial class Path
    {
        public static char[] GetInvalidFileNameChars() => new char[] { '\0', '/' };

        public static char[] GetInvalidPathChars() => new char[] { '\0' };

        // Expands the given path to a fully qualified path. 
        public static string GetFullPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path.Length == 0)
                throw new ArgumentException(SR.Arg_PathEmpty, nameof(path));

            if (path.Contains('\0'))
                throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path)); 
            
            // Expand with current directory if necessary
            if (!IsPathRooted(path))
            {
                path = Combine(Interop.Sys.GetCwd(), path);
            }

            // We would ideally use realpath to do this, but it resolves symlinks, requires that the file actually exist,
            // and turns it into a full path, which we only want if fullCheck is true.
            string collapsedString = PathInternal.RemoveRelativeSegments(path, PathInternal.GetRootLength(path));

            Debug.Assert(collapsedString.Length < path.Length || collapsedString.ToString() == path,
                "Either we've removed characters, or the string should be unmodified from the input path.");

            string result = collapsedString.Length == 0 ? PathInternal.DirectorySeparatorCharAsString : collapsedString;

            return result;
        }

        public static string GetFullPath(string path, string basePath)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (basePath == null)
                throw new ArgumentNullException(nameof(basePath));

            if (!IsPathFullyQualified(basePath))
                throw new ArgumentException(SR.Arg_BasePathNotFullyQualified, nameof(basePath));

            if (basePath.Contains('\0') || path.Contains('\0'))
                throw new ArgumentException(SR.Argument_InvalidPathChars);

            if (IsPathFullyQualified(path))
                return GetFullPath(path);

            return GetFullPath(CombineInternal(basePath, path));
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
            string? path = Environment.GetEnvironmentVariable(TempEnvVar);
            return
                string.IsNullOrEmpty(path) ? DefaultTempPath :
                PathInternal.IsDirectorySeparator(path[path.Length - 1]) ? path :
                path + PathInternal.DirectorySeparatorChar;
        }

        public static string GetTempFileName()
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

        public static bool IsPathRooted(string? path)
        {
            if (path == null)
                return false;

            return IsPathRooted(path.AsSpan());
        }

        public static bool IsPathRooted(ReadOnlySpan<char> path)
        {
            return path.Length > 0 && path[0] == PathInternal.DirectorySeparatorChar;
        }

        /// <summary>
        /// Returns the path root or null if path is empty or null.
        /// </summary>
        public static string? GetPathRoot(string? path)
        {
            if (PathInternal.IsEffectivelyEmpty(path)) return null;

            return IsPathRooted(path) ? PathInternal.DirectorySeparatorCharAsString : string.Empty;
        }

        public static ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path)
        {
            return PathInternal.IsEffectivelyEmpty(path) && IsPathRooted(path) ? PathInternal.DirectorySeparatorCharAsString.AsSpan() : ReadOnlySpan<char>.Empty;
        }

        /// <summary>Gets whether the system is case-sensitive.</summary>
        internal static bool IsCaseSensitive
        {
            get
            {
                #if PLATFORM_OSX
                    return false;
                #else
                    return true;
                #endif
            }
        }
    }
}
