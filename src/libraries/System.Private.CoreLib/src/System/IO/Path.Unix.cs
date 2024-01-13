// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.IO
{
    public static partial class Path
    {
        public static char[] GetInvalidFileNameChars() => new char[] { '\0', '/' };

        public static char[] GetInvalidPathChars() => new char[] { '\0' };

        // Checks if the given path is available for use.
        private static bool ExistsCore(string fullPath, out bool isDirectory)
        {
            bool result = Interop.Sys.LStat(fullPath, out Interop.Sys.FileStatus fileInfo) == Interop.Errors.ERROR_SUCCESS;
            isDirectory = result && (fileInfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR;

            return result;
        }

        // Expands the given path to a fully qualified path.
        public static string GetFullPath(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            if (path.Contains('\0'))
                throw new ArgumentException(SR.Argument_NullCharInPath, nameof(path));

            return GetFullPathInternal(path);
        }

        public static string GetFullPath(string path, string basePath)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(basePath);

            if (!IsPathFullyQualified(basePath))
                throw new ArgumentException(SR.Arg_BasePathNotFullyQualified, nameof(basePath));

            if (basePath.Contains('\0') || path.Contains('\0'))
                throw new ArgumentException(SR.Argument_NullCharInPath);

            if (IsPathFullyQualified(path))
                return GetFullPathInternal(path);

            return GetFullPathInternal(CombineInternal(basePath, path));
        }

        // Gets the full path without argument validation
        private static string GetFullPathInternal(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(!path.Contains('\0'));

            // Expand with current directory if necessary
            if (!IsPathRooted(path))
            {
                path = Combine(Interop.Sys.GetCwd(), path);
            }

            // We would ideally use realpath to do this, but it resolves symlinks and requires that the file actually exist.
            string collapsedString = PathInternal.RemoveRelativeSegments(path, PathInternal.GetRootLength(path));

            Debug.Assert(collapsedString.Length < path.Length || collapsedString.ToString() == path,
                "Either we've removed characters, or the string should be unmodified from the input path.");

            string result = collapsedString.Length == 0 ? PathInternal.DirectorySeparatorCharAsString : collapsedString;

            return result;
        }

        private static string RemoveLongPathPrefix(string path)
        {
            return path; // nop.  There's nothing special about "long" paths on Unix.
        }

        public static string GetTempPath()
        {
            const string TempEnvVar = "TMPDIR";

            // Get the temp path from the TMPDIR environment variable.
            // If it's not set, just return the default path.
            // If it is, return it, ensuring it ends with a slash.
            string? path = Environment.GetEnvironmentVariable(TempEnvVar);
            return
                string.IsNullOrEmpty(path) ? DefaultTempPath :
                PathInternal.IsDirectorySeparator(path[path.Length - 1]) ? path :
                path + PathInternal.DirectorySeparatorChar;
        }

        public static unsafe string GetTempFileName()
        {
            const int SuffixByteLength = 4; // ".tmp"
            ReadOnlySpan<byte> fileTemplate = "tmpXXXXXX.tmp"u8;

            // mkstemps takes a char* and overwrites the XXXXXX with six characters
            // that'll result in a unique file name.
            string tempPath = Path.GetTempPath();
            int tempPathByteCount = Encoding.UTF8.GetByteCount(tempPath);
            int totalByteCount = tempPathByteCount + fileTemplate.Length + 1;

#if TARGET_BROWSER
            // https://github.com/emscripten-core/emscripten/issues/18591
            // The emscripten implementation of __randname uses pointer address as another entry into the randomness.
            Span<byte> path = new byte[totalByteCount];
#else
            Span<byte> path = totalByteCount <= 256 ? stackalloc byte[256].Slice(0, totalByteCount) : new byte[totalByteCount];
#endif
            int pos = Encoding.UTF8.GetBytes(tempPath, path);
            fileTemplate.CopyTo(path.Slice(pos));
            path[^1] = 0;

            // Create, open, and close the temp file.
            fixed (byte* pPath = path)
            {
                // if this returns ENOENT it's because TMPDIR doesn't exist, so isDirError:true
                IntPtr fd = Interop.CheckIo(Interop.Sys.MksTemps(pPath, SuffixByteLength), tempPath, isDirError: true);
                Interop.Sys.Close(fd); // ignore any errors from close; nothing to do if cleanup isn't possible
            }

            // 'path' is now the name of the file
            Debug.Assert(path[^1] == 0);
            return Encoding.UTF8.GetString(path.Slice(0, path.Length - 1)); // trim off the trailing '\0'
        }

        public static bool IsPathRooted([NotNullWhen(true)] string? path)
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
            return IsPathRooted(path) ? PathInternal.DirectorySeparatorCharAsString.AsSpan() : ReadOnlySpan<char>.Empty;
        }

    }
}
