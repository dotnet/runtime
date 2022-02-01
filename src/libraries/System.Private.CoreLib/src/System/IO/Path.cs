// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    // Provides methods for processing file system strings in a cross-platform manner.
    // Most of the methods don't do a complete parsing (such as examining a UNC hostname),
    // but they will handle most string operations.
    public static partial class Path
    {
        // Public static readonly variant of the separators. The Path implementation itself is using
        // internal const variant of the separators for better performance.
        public static readonly char DirectorySeparatorChar = PathInternal.DirectorySeparatorChar;
        public static readonly char AltDirectorySeparatorChar = PathInternal.AltDirectorySeparatorChar;
        public static readonly char VolumeSeparatorChar = PathInternal.VolumeSeparatorChar;
        public static readonly char PathSeparator = PathInternal.PathSeparator;

        // For generating random file names
        // 8 random bytes provides 12 chars in our encoding for the 8.3 name.
        private const int KeyLength = 8;

        [Obsolete("Path.InvalidPathChars has been deprecated. Use GetInvalidPathChars or GetInvalidFileNameChars instead.")]
        public static readonly char[] InvalidPathChars = GetInvalidPathChars();

        // Changes the extension of a file path. The path parameter
        // specifies a file path, and the extension parameter
        // specifies a file extension (with a leading period, such as
        // ".exe" or ".cs").
        //
        // The function returns a file path with the same root, directory, and base
        // name parts as path, but with the file extension changed to
        // the specified extension. If path is null, the function
        // returns null. If path does not contain a file extension,
        // the new file extension is appended to the path. If extension
        // is null, any existing extension is removed from path.
        [return: NotNullIfNotNull("path")]
        public static string? ChangeExtension(string? path, string? extension)
        {
            if (path == null)
                return null;

            int subLength = path.Length;
            if (subLength == 0)
                return string.Empty;

            for (int i = path.Length - 1; i >= 0; i--)
            {
                char ch = path[i];

                if (ch == '.')
                {
                    subLength = i;
                    break;
                }

                if (PathInternal.IsDirectorySeparator(ch))
                {
                    break;
                }
            }

            if (extension == null)
            {
                return path.Substring(0, subLength);
            }

            ReadOnlySpan<char> subpath = path.AsSpan(0, subLength);
            return extension.StartsWith('.') ?
                string.Concat(subpath, extension) :
                string.Concat(subpath, ".", extension);
        }

        /// <summary>
        /// Determines whether the specified file or directory exists.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="File.Exists(string?)"/> it returns true for existing, non-regular files like pipes.
        /// If the path targets an existing link, but the target of the link does not exist, it returns true.
        /// </remarks>
        /// <param name="path">The path to check</param>
        /// <returns>
        /// <see langword="true" /> if the caller has the required permissions and <paramref name="path" /> contains
        /// the name of an existing file or directory; otherwise, <see langword="false" />.
        /// This method also returns <see langword="false" /> if <paramref name="path" /> is <see langword="null" />,
        /// an invalid path, or a zero-length string. If the caller does not have sufficient permissions to read the specified path,
        /// no exception is thrown and the method returns <see langword="false" /> regardless of the existence of <paramref name="path" />.
        /// </returns>
        public static bool Exists([NotNullWhen(true)] string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string? fullPath;
            try
            {
                fullPath = GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                return false;
            }

            bool result = ExistsCore(fullPath, out bool isDirectory);
            if (result && PathInternal.IsDirectorySeparator(fullPath[fullPath.Length - 1]))
            {
                // Some sys-calls remove all trailing slashes and may give false positives for existing files.
                // We want to make sure that if the path ends in a trailing slash, it's truly a directory.
                return isDirectory;
            }

            return result;
        }

        /// <summary>
        /// Returns the directory portion of a file path. This method effectively
        /// removes the last segment of the given file path, i.e. it returns a
        /// string consisting of all characters up to but not including the last
        /// backslash ("\") in the file path. The returned value is null if the
        /// specified path is null, empty, or a root (such as "\", "C:", or
        /// "\\server\share").
        /// </summary>
        /// <remarks>
        /// Directory separators are normalized in the returned string.
        /// </remarks>
        public static string? GetDirectoryName(string? path)
        {
            if (path == null || PathInternal.IsEffectivelyEmpty(path.AsSpan()))
                return null;

            int end = GetDirectoryNameOffset(path.AsSpan());
            return end >= 0 ? PathInternal.NormalizeDirectorySeparators(path.Substring(0, end)) : null;
        }

        /// <summary>
        /// Returns the directory portion of a file path. The returned value is empty
        /// if the specified path is null, empty, or a root (such as "\", "C:", or
        /// "\\server\share").
        /// </summary>
        /// <remarks>
        /// Unlike the string overload, this method will not normalize directory separators.
        /// </remarks>
        public static ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path)
        {
            if (PathInternal.IsEffectivelyEmpty(path))
                return ReadOnlySpan<char>.Empty;

            int end = GetDirectoryNameOffset(path);
            return end >= 0 ? path.Slice(0, end) : ReadOnlySpan<char>.Empty;
        }

        internal static int GetDirectoryNameOffset(ReadOnlySpan<char> path)
        {
            int rootLength = PathInternal.GetRootLength(path);
            int end = path.Length;
            if (end <= rootLength)
                return -1;

            while (end > rootLength && !PathInternal.IsDirectorySeparator(path[--end])) ;

            // Trim off any remaining separators (to deal with C:\foo\\bar)
            while (end > rootLength && PathInternal.IsDirectorySeparator(path[end - 1]))
                end--;

            return end;
        }

        /// <summary>
        /// Returns the extension of the given path. The returned value includes the period (".") character of the
        /// extension except when you have a terminal period when you get string.Empty, such as ".exe" or ".cpp".
        /// The returned value is null if the given path is null or empty if the given path does not include an
        /// extension.
        /// </summary>
        [return: NotNullIfNotNull("path")]
        public static string? GetExtension(string? path)
        {
            if (path == null)
                return null;

            return GetExtension(path.AsSpan()).ToString();
        }

        /// <summary>
        /// Returns the extension of the given path.
        /// </summary>
        /// <remarks>
        /// The returned value is an empty ReadOnlySpan if the given path does not include an extension.
        /// </remarks>
        public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
        {
            int length = path.Length;

            for (int i = length - 1; i >= 0; i--)
            {
                char ch = path[i];
                if (ch == '.')
                {
                    if (i != length - 1)
                        return path.Slice(i, length - i);
                    else
                        return ReadOnlySpan<char>.Empty;
                }
                if (PathInternal.IsDirectorySeparator(ch))
                    break;
            }
            return ReadOnlySpan<char>.Empty;
        }

        /// <summary>
        /// Returns the name and extension parts of the given path. The resulting string contains
        /// the characters of path that follow the last separator in path. The resulting string is
        /// null if path is null.
        /// </summary>
        [return: NotNullIfNotNull("path")]
        public static string? GetFileName(string? path)
        {
            if (path == null)
                return null;

            ReadOnlySpan<char> result = GetFileName(path.AsSpan());
            if (path.Length == result.Length)
                return path;

            return result.ToString();
        }

        /// <summary>
        /// The returned ReadOnlySpan contains the characters of the path that follows the last separator in path.
        /// </summary>
        public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
        {
            int root = GetPathRoot(path).Length;

            // We don't want to cut off "C:\file.txt:stream" (i.e. should be "file.txt:stream")
            // but we *do* want "C:Foo" => "Foo". This necessitates checking for the root.

            for (int i = path.Length; --i >= 0;)
            {
                if (i < root || PathInternal.IsDirectorySeparator(path[i]))
                    return path.Slice(i + 1, path.Length - i - 1);
            }

            return path;
        }

        [return: NotNullIfNotNull("path")]
        public static string? GetFileNameWithoutExtension(string? path)
        {
            if (path == null)
                return null;

            ReadOnlySpan<char> result = GetFileNameWithoutExtension(path.AsSpan());
            if (path.Length == result.Length)
                return path;

            return result.ToString();
        }

        /// <summary>
        /// Returns the characters between the last separator and last (.) in the path.
        /// </summary>
        public static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path)
        {
            ReadOnlySpan<char> fileName = GetFileName(path);
            int lastPeriod = fileName.LastIndexOf('.');
            return lastPeriod < 0 ?
                fileName : // No extension was found
                fileName.Slice(0, lastPeriod);
        }

        /// <summary>
        /// Returns a cryptographically strong random 8.3 string that can be
        /// used as either a folder name or a file name.
        /// </summary>
        public static unsafe string GetRandomFileName()
        {
            byte* pKey = stackalloc byte[KeyLength];
            Interop.GetRandomBytes(pKey, KeyLength);

            return string.Create(
                    12, (IntPtr)pKey, (span, key) => // 12 == 8 + 1 (for period) + 3
                         Populate83FileNameFromRandomBytes((byte*)key, KeyLength, span));
        }

        /// <summary>
        /// Returns true if the path is fixed to a specific drive or UNC path. This method does no
        /// validation of the path (URIs will be returned as relative as a result).
        /// Returns false if the path specified is relative to the current drive or working directory.
        /// </summary>
        /// <remarks>
        /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
        /// assume that rooted paths <see cref="Path.IsPathRooted(string)"/> are not relative.  This isn't the case.
        /// "C:a" is drive relative- meaning that it will be resolved against the current directory
        /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
        /// will not be used to modify the path).
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="path"/> is null.
        /// </exception>
        public static bool IsPathFullyQualified(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return IsPathFullyQualified(path.AsSpan());
        }

        public static bool IsPathFullyQualified(ReadOnlySpan<char> path)
        {
            return !PathInternal.IsPartiallyQualified(path);
        }

        /// <summary>
        /// Tests if a path's file name includes a file extension. A trailing period
        /// is not considered an extension.
        /// </summary>
        public static bool HasExtension([NotNullWhen(true)] string? path)
        {
            if (path != null)
            {
                return HasExtension(path.AsSpan());
            }
            return false;
        }

        public static bool HasExtension(ReadOnlySpan<char> path)
        {
            for (int i = path.Length - 1; i >= 0; i--)
            {
                char ch = path[i];
                if (ch == '.')
                {
                    return i != path.Length - 1;
                }
                if (PathInternal.IsDirectorySeparator(ch))
                    break;
            }
            return false;
        }

        public static string Combine(string path1, string path2)
        {
            if (path1 == null || path2 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) : nameof(path2));

            return CombineInternal(path1, path2);
        }

        public static string Combine(string path1, string path2, string path3)
        {
            if (path1 == null || path2 == null || path3 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) : (path2 == null) ? nameof(path2) : nameof(path3));

            return CombineInternal(path1, path2, path3);
        }

        public static string Combine(string path1, string path2, string path3, string path4)
        {
            if (path1 == null || path2 == null || path3 == null || path4 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) : (path2 == null) ? nameof(path2) : (path3 == null) ? nameof(path3) : nameof(path4));

            return CombineInternal(path1, path2, path3, path4);
        }

        public static string Combine(params string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            int maxSize = 0;
            int firstComponent = 0;

            // We have two passes, the first calculates how large a buffer to allocate and does some precondition
            // checks on the paths passed in.  The second actually does the combination.

            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] == null)
                {
                    throw new ArgumentNullException(nameof(paths));
                }

                if (paths[i].Length == 0)
                {
                    continue;
                }

                if (IsPathRooted(paths[i]))
                {
                    firstComponent = i;
                    maxSize = paths[i].Length;
                }
                else
                {
                    maxSize += paths[i].Length;
                }

                char ch = paths[i][paths[i].Length - 1];
                if (!PathInternal.IsDirectorySeparator(ch))
                    maxSize++;
            }

            var builder = new ValueStringBuilder(stackalloc char[260]); // MaxShortPath on Windows
            builder.EnsureCapacity(maxSize);

            for (int i = firstComponent; i < paths.Length; i++)
            {
                if (paths[i].Length == 0)
                {
                    continue;
                }

                if (builder.Length == 0)
                {
                    builder.Append(paths[i]);
                }
                else
                {
                    char ch = builder[builder.Length - 1];
                    if (!PathInternal.IsDirectorySeparator(ch))
                    {
                        builder.Append(PathInternal.DirectorySeparatorChar);
                    }

                    builder.Append(paths[i]);
                }
            }

            return builder.ToString();
        }

        // Unlike Combine(), Join() methods do not consider rooting. They simply combine paths, ensuring that there
        // is a directory separator between them.

        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
        {
            if (path1.Length == 0)
                return path2.ToString();
            if (path2.Length == 0)
                return path1.ToString();

            return JoinInternal(path1, path2);
        }

        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3)
        {
            if (path1.Length == 0)
                return Join(path2, path3);

            if (path2.Length == 0)
                return Join(path1, path3);

            if (path3.Length == 0)
                return Join(path1, path2);

            return JoinInternal(path1, path2, path3);
        }

        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4)
        {
            if (path1.Length == 0)
                return Join(path2, path3, path4);

            if (path2.Length == 0)
                return Join(path1, path3, path4);

            if (path3.Length == 0)
                return Join(path1, path2, path4);

            if (path4.Length == 0)
                return Join(path1, path2, path3);

            return JoinInternal(path1, path2, path3, path4);
        }

        public static string Join(string? path1, string? path2)
        {
            return Join(path1.AsSpan(), path2.AsSpan());
        }

        public static string Join(string? path1, string? path2, string? path3)
        {
            return Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan());
        }

        public static string Join(string? path1, string? path2, string? path3, string? path4)
        {
            return Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), path4.AsSpan());
        }

        public static string Join(params string?[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            if (paths.Length == 0)
            {
                return string.Empty;
            }

            int maxSize = 0;
            foreach (string? path in paths)
            {
                maxSize += path?.Length ?? 0;
            }
            maxSize += paths.Length - 1;

            var builder = new ValueStringBuilder(stackalloc char[260]); // MaxShortPath on Windows
            builder.EnsureCapacity(maxSize);

            for (int i = 0; i < paths.Length; i++)
            {
                string? path = paths[i];
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (builder.Length == 0)
                {
                    builder.Append(path);
                }
                else
                {
                    if (!PathInternal.IsDirectorySeparator(builder[builder.Length - 1]) && !PathInternal.IsDirectorySeparator(path[0]))
                    {
                        builder.Append(PathInternal.DirectorySeparatorChar);
                    }

                    builder.Append(path);
                }
            }

            return builder.ToString();
        }

        public static bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, Span<char> destination, out int charsWritten)
        {
            charsWritten = 0;
            if (path1.Length == 0 && path2.Length == 0)
                return true;

            if (path1.Length == 0 || path2.Length == 0)
            {
                ref ReadOnlySpan<char> pathToUse = ref path1.Length == 0 ? ref path2 : ref path1;
                if (destination.Length < pathToUse.Length)
                {
                    return false;
                }

                pathToUse.CopyTo(destination);
                charsWritten = pathToUse.Length;
                return true;
            }

            bool needsSeparator = !(EndsInDirectorySeparator(path1) || PathInternal.StartsWithDirectorySeparator(path2));
            int charsNeeded = path1.Length + path2.Length + (needsSeparator ? 1 : 0);
            if (destination.Length < charsNeeded)
                return false;

            path1.CopyTo(destination);
            if (needsSeparator)
                destination[path1.Length] = DirectorySeparatorChar;

            path2.CopyTo(destination.Slice(path1.Length + (needsSeparator ? 1 : 0)));

            charsWritten = charsNeeded;
            return true;
        }

        public static bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, Span<char> destination, out int charsWritten)
        {
            charsWritten = 0;
            if (path1.Length == 0 && path2.Length == 0 && path3.Length == 0)
                return true;

            if (path1.Length == 0)
                return TryJoin(path2, path3, destination, out charsWritten);
            if (path2.Length == 0)
                return TryJoin(path1, path3, destination, out charsWritten);
            if (path3.Length == 0)
                return TryJoin(path1, path2, destination, out charsWritten);

            int neededSeparators = EndsInDirectorySeparator(path1) || PathInternal.StartsWithDirectorySeparator(path2) ? 0 : 1;
            bool needsSecondSeparator = !(EndsInDirectorySeparator(path2) || PathInternal.StartsWithDirectorySeparator(path3));
            if (needsSecondSeparator)
                neededSeparators++;

            int charsNeeded = path1.Length + path2.Length + path3.Length + neededSeparators;
            if (destination.Length < charsNeeded)
                return false;

            bool result = TryJoin(path1, path2, destination, out charsWritten);
            Debug.Assert(result, "should never fail joining first two paths");

            if (needsSecondSeparator)
                destination[charsWritten++] = DirectorySeparatorChar;

            path3.CopyTo(destination.Slice(charsWritten));
            charsWritten += path3.Length;

            return true;
        }

        private static string CombineInternal(string first, string second)
        {
            if (string.IsNullOrEmpty(first))
                return second;

            if (string.IsNullOrEmpty(second))
                return first;

            if (IsPathRooted(second.AsSpan()))
                return second;

            return JoinInternal(first.AsSpan(), second.AsSpan());
        }

        private static string CombineInternal(string first, string second, string third)
        {
            if (string.IsNullOrEmpty(first))
                return CombineInternal(second, third);
            if (string.IsNullOrEmpty(second))
                return CombineInternal(first, third);
            if (string.IsNullOrEmpty(third))
                return CombineInternal(first, second);

            if (IsPathRooted(third.AsSpan()))
                return third;
            if (IsPathRooted(second.AsSpan()))
                return CombineInternal(second, third);

            return JoinInternal(first.AsSpan(), second.AsSpan(), third.AsSpan());
        }

        private static string CombineInternal(string first, string second, string third, string fourth)
        {
            if (string.IsNullOrEmpty(first))
                return CombineInternal(second, third, fourth);
            if (string.IsNullOrEmpty(second))
                return CombineInternal(first, third, fourth);
            if (string.IsNullOrEmpty(third))
                return CombineInternal(first, second, fourth);
            if (string.IsNullOrEmpty(fourth))
                return CombineInternal(first, second, third);

            if (IsPathRooted(fourth.AsSpan()))
                return fourth;
            if (IsPathRooted(third.AsSpan()))
                return CombineInternal(third, fourth);
            if (IsPathRooted(second.AsSpan()))
                return CombineInternal(second, third, fourth);

            return JoinInternal(first.AsSpan(), second.AsSpan(), third.AsSpan(), fourth.AsSpan());
        }

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0, "should have dealt with empty paths");

            bool hasSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                || PathInternal.IsDirectorySeparator(second[0]);

            return hasSeparator ?
                string.Concat(first, second) :
                string.Concat(first, PathInternal.DirectorySeparatorCharAsString, second);
        }

        private unsafe readonly struct Join3Payload
        {
            public Join3Payload(char* first, int firstLength, char* second, int secondLength, char* third, int thirdLength, byte separators)
            {
                First = first;
                FirstLength = firstLength;
                Second = second;
                SecondLength = secondLength;
                Third = third;
                ThirdLength = thirdLength;
                Separators = separators;
            }

            public readonly char* First;
            public readonly int FirstLength;
            public readonly char* Second;
            public readonly int SecondLength;
            public readonly char* Third;
            public readonly int ThirdLength;
            public readonly byte Separators;
        }

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0 && third.Length > 0, "should have dealt with empty paths");

            byte firstNeedsSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                || PathInternal.IsDirectorySeparator(second[0]) ? (byte)0 : (byte)1;
            byte secondNeedsSeparator = PathInternal.IsDirectorySeparator(second[second.Length - 1])
                || PathInternal.IsDirectorySeparator(third[0]) ? (byte)0 : (byte)1;

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second), t = &MemoryMarshal.GetReference(third))
            {
                var payload = new Join3Payload(
                    f, first.Length, s, second.Length, t, third.Length,
                    (byte)(firstNeedsSeparator | secondNeedsSeparator << 1));

                return string.Create(
                    first.Length + second.Length + third.Length + firstNeedsSeparator + secondNeedsSeparator,
                    (IntPtr)(&payload),
                    static (destination, statePtr) =>
                    {
                        ref Join3Payload state = ref *(Join3Payload*)statePtr;
                        new Span<char>(state.First, state.FirstLength).CopyTo(destination);
                        if ((state.Separators & 0b1) != 0)
                            destination[state.FirstLength] = PathInternal.DirectorySeparatorChar;
                        new Span<char>(state.Second, state.SecondLength).CopyTo(destination.Slice(state.FirstLength + (state.Separators & 0b1)));
                        if ((state.Separators & 0b10) != 0)
                            destination[destination.Length - state.ThirdLength - 1] = PathInternal.DirectorySeparatorChar;
                        new Span<char>(state.Third, state.ThirdLength).CopyTo(destination.Slice(destination.Length - state.ThirdLength));
                    });
            }
        }

        private unsafe readonly struct Join4Payload
        {
            public Join4Payload(char* first, int firstLength, char* second, int secondLength, char* third, int thirdLength, char* fourth, int fourthLength, byte separators)
            {
                First = first;
                FirstLength = firstLength;
                Second = second;
                SecondLength = secondLength;
                Third = third;
                ThirdLength = thirdLength;
                Fourth = fourth;
                FourthLength = fourthLength;
                Separators = separators;
            }

            public readonly char* First;
            public readonly int FirstLength;
            public readonly char* Second;
            public readonly int SecondLength;
            public readonly char* Third;
            public readonly int ThirdLength;
            public readonly char* Fourth;
            public readonly int FourthLength;
            public readonly byte Separators;
        }

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third, ReadOnlySpan<char> fourth)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0 && third.Length > 0 && fourth.Length > 0, "should have dealt with empty paths");

            byte firstNeedsSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                || PathInternal.IsDirectorySeparator(second[0]) ? (byte)0 : (byte)1;
            byte secondNeedsSeparator = PathInternal.IsDirectorySeparator(second[second.Length - 1])
                || PathInternal.IsDirectorySeparator(third[0]) ? (byte)0 : (byte)1;
            byte thirdNeedsSeparator = PathInternal.IsDirectorySeparator(third[third.Length - 1])
                || PathInternal.IsDirectorySeparator(fourth[0]) ? (byte)0 : (byte)1;

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second), t = &MemoryMarshal.GetReference(third), u = &MemoryMarshal.GetReference(fourth))
            {
                var payload = new Join4Payload(
                    f, first.Length, s, second.Length, t, third.Length, u, fourth.Length,
                    (byte)(firstNeedsSeparator | secondNeedsSeparator << 1 | thirdNeedsSeparator << 2));

                return string.Create(
                    first.Length + second.Length + third.Length + fourth.Length + firstNeedsSeparator + secondNeedsSeparator + thirdNeedsSeparator,
                    (IntPtr)(&payload),
                    static (destination, statePtr) =>
                    {
                        ref Join4Payload state = ref *(Join4Payload*)statePtr;
                        new Span<char>(state.First, state.FirstLength).CopyTo(destination);
                        int insertionPoint = state.FirstLength;
                        if ((state.Separators & 0b1) != 0)
                            destination[insertionPoint++] = PathInternal.DirectorySeparatorChar;
                        new Span<char>(state.Second, state.SecondLength).CopyTo(destination.Slice(insertionPoint));
                        insertionPoint += state.SecondLength;
                        if ((state.Separators & 0b10) != 0)
                            destination[insertionPoint++] = PathInternal.DirectorySeparatorChar;
                        new Span<char>(state.Third, state.ThirdLength).CopyTo(destination.Slice(insertionPoint));
                        insertionPoint += state.ThirdLength;
                        if ((state.Separators & 0b100) != 0)
                            destination[insertionPoint++] = PathInternal.DirectorySeparatorChar;
                        new Span<char>(state.Fourth, state.FourthLength).CopyTo(destination.Slice(insertionPoint));
                    });
            }
        }

        private static ReadOnlySpan<byte> Base32Char => new byte[32] { // uses C# compiler's optimization for static byte[] data
                (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h',
                (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', (byte)'n', (byte)'o', (byte)'p',
                (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v', (byte)'w', (byte)'x',
                (byte)'y', (byte)'z', (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5' };

        private static unsafe void Populate83FileNameFromRandomBytes(byte* bytes, int byteCount, Span<char> chars)
        {
            // This method requires bytes of length 8 and chars of length 12.
            Debug.Assert(bytes != null);
            Debug.Assert(byteCount == 8, $"Unexpected {nameof(byteCount)}");
            Debug.Assert(chars.Length == 12, $"Unexpected {nameof(chars)}.Length");

            byte b0 = bytes[0];
            byte b1 = bytes[1];
            byte b2 = bytes[2];
            byte b3 = bytes[3];
            byte b4 = bytes[4];

            // write to chars[11] first in order to eliminate redundant bounds checks
            chars[11] = (char)Base32Char[bytes[7] & 0x1F];

            // Consume the 5 Least significant bits of the first 5 bytes
            chars[0] = (char)Base32Char[b0 & 0x1F];
            chars[1] = (char)Base32Char[b1 & 0x1F];
            chars[2] = (char)Base32Char[b2 & 0x1F];
            chars[3] = (char)Base32Char[b3 & 0x1F];
            chars[4] = (char)Base32Char[b4 & 0x1F];

            // Consume 3 MSB of b0, b1, MSB bits 6, 7 of b3, b4
            chars[5] = (char)Base32Char[
                    ((b0 & 0xE0) >> 5) |
                    ((b3 & 0x60) >> 2)];

            chars[6] = (char)Base32Char[
                    ((b1 & 0xE0) >> 5) |
                    ((b4 & 0x60) >> 2)];

            // Consume 3 MSB bits of b2, 1 MSB bit of b3, b4
            b2 >>= 5;

            Debug.Assert((b2 & 0xF8) == 0, "Unexpected set bits");

            if ((b3 & 0x80) != 0)
                b2 |= 0x08;
            if ((b4 & 0x80) != 0)
                b2 |= 0x10;

            chars[7] = (char)Base32Char[b2];

            // Set the file extension separator
            chars[8] = '.';

            // Consume the 5 Least significant bits of the remaining 3 bytes
            chars[9] = (char)Base32Char[bytes[5] & 0x1F];
            chars[10] = (char)Base32Char[bytes[6] & 0x1F];
        }

        /// <summary>
        /// Create a relative path from one path to another. Paths will be resolved before calculating the difference.
        /// Default path comparison for the active platform will be used (OrdinalIgnoreCase for Windows or Mac, Ordinal for Unix).
        /// </summary>
        /// <param name="relativeTo">The source path the output should be relative to. This path is always considered to be a directory.</param>
        /// <param name="path">The destination path.</param>
        /// <returns>The relative path or <paramref name="path"/> if the paths don't share the same root.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="relativeTo"/> or <paramref name="path"/> is <c>null</c> or an empty string.</exception>
        public static string GetRelativePath(string relativeTo, string path)
        {
            return GetRelativePath(relativeTo, path, PathInternal.StringComparison);
        }

        private static string GetRelativePath(string relativeTo, string path, StringComparison comparisonType)
        {
            if (relativeTo == null)
                throw new ArgumentNullException(nameof(relativeTo));

            if (PathInternal.IsEffectivelyEmpty(relativeTo.AsSpan()))
                throw new ArgumentException(SR.Arg_PathEmpty, nameof(relativeTo));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
                throw new ArgumentException(SR.Arg_PathEmpty, nameof(path));

            Debug.Assert(comparisonType == StringComparison.Ordinal || comparisonType == StringComparison.OrdinalIgnoreCase);

            relativeTo = GetFullPath(relativeTo);
            path = GetFullPath(path);

            // Need to check if the roots are different- if they are we need to return the "to" path.
            if (!PathInternal.AreRootsEqual(relativeTo, path, comparisonType))
                return path;

            int commonLength = PathInternal.GetCommonPathLength(relativeTo, path, ignoreCase: comparisonType == StringComparison.OrdinalIgnoreCase);

            // If there is nothing in common they can't share the same root, return the "to" path as is.
            if (commonLength == 0)
                return path;

            // Trailing separators aren't significant for comparison
            int relativeToLength = relativeTo.Length;
            if (EndsInDirectorySeparator(relativeTo.AsSpan()))
                relativeToLength--;

            bool pathEndsInSeparator = EndsInDirectorySeparator(path.AsSpan());
            int pathLength = path.Length;
            if (pathEndsInSeparator)
                pathLength--;

            // If we have effectively the same path, return "."
            if (relativeToLength == pathLength && commonLength >= relativeToLength) return ".";

            // We have the same root, we need to calculate the difference now using the
            // common Length and Segment count past the length.
            //
            // Some examples:
            //
            //  C:\Foo C:\Bar L3, S1 -> ..\Bar
            //  C:\Foo C:\Foo\Bar L6, S0 -> Bar
            //  C:\Foo\Bar C:\Bar\Bar L3, S2 -> ..\..\Bar\Bar
            //  C:\Foo\Foo C:\Foo\Bar L7, S1 -> ..\Bar

            var sb = new ValueStringBuilder(stackalloc char[260]);
            sb.EnsureCapacity(Math.Max(relativeTo.Length, path.Length));

            // Add parent segments for segments past the common on the "from" path
            if (commonLength < relativeToLength)
            {
                sb.Append("..");

                for (int i = commonLength + 1; i < relativeToLength; i++)
                {
                    if (PathInternal.IsDirectorySeparator(relativeTo[i]))
                    {
                        sb.Append(DirectorySeparatorChar);
                        sb.Append("..");
                    }
                }
            }
            else if (PathInternal.IsDirectorySeparator(path[commonLength]))
            {
                // No parent segments and we need to eat the initial separator
                //  (C:\Foo C:\Foo\Bar case)
                commonLength++;
            }

            // Now add the rest of the "to" path, adding back the trailing separator
            int differenceLength = pathLength - commonLength;
            if (pathEndsInSeparator)
                differenceLength++;

            if (differenceLength > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(DirectorySeparatorChar);
                }

                sb.Append(path.AsSpan(commonLength, differenceLength));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Trims one trailing directory separator beyond the root of the path.
        /// </summary>
        public static string TrimEndingDirectorySeparator(string path) => PathInternal.TrimEndingDirectorySeparator(path);

        /// <summary>
        /// Trims one trailing directory separator beyond the root of the path.
        /// </summary>
        public static ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path) => PathInternal.TrimEndingDirectorySeparator(path);

        /// <summary>
        /// Returns true if the path ends in a directory separator.
        /// </summary>
        public static bool EndsInDirectorySeparator(ReadOnlySpan<char> path) => PathInternal.EndsInDirectorySeparator(path);

        /// <summary>
        /// Returns true if the path ends in a directory separator.
        /// </summary>
        public static bool EndsInDirectorySeparator(string path) => PathInternal.EndsInDirectorySeparator(path);
    }
}
