// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
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

        [Obsolete("Please use GetInvalidPathChars or GetInvalidFileNameChars instead.")]
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
        public static string ChangeExtension(string path, string extension)
        {
            if (path != null)
            {
                string s = path;
                for (int i = path.Length - 1; i >= 0; i--)
                {
                    char ch = path[i];
                    if (ch == '.')
                    {
                        s = path.Substring(0, i);
                        break;
                    }
                    if (PathInternal.IsDirectoryOrVolumeSeparator(ch)) break;
                }

                if (extension != null && path.Length != 0)
                {
                    s = (extension.Length == 0 || extension[0] != '.') ?
                        s + "." + extension :
                        s + extension;
                }

                return s;
            }
            return null;
        }

        // Returns the directory path of a file path. This method effectively
        // removes the last element of the given file path, i.e. it returns a
        // string consisting of all characters up to but not including the last
        // backslash ("\") in the file path. The returned value is null if the file
        // path is null or if the file path denotes a root (such as "\", "C:", or
        // "\\server\share").
        public static string GetDirectoryName(string path)
        {
            if (path == null)
                return null;

            if (PathInternal.IsEffectivelyEmpty(path))
                throw new ArgumentException(SR.Arg_PathEmpty, nameof(path));

            path = PathInternal.NormalizeDirectorySeparators(path);
            int end = PathInternal.GetDirectoryNameOffset(path);
            return end >= 0 ? path.Substring(0, end) : null;
        }

        /// <summary>
        /// Returns the directory path of a file path.
        /// The returned value the an empty ReadOnlySpan if path is empty or 
        /// if the file path denotes as root  (such as "\", "C:", or "\\server\share"). 
        /// </summary>
        /// <remarks>
        /// Unlike the string overload, this method will not normalize directory separators.
        /// </remarks>
        public static ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path)
        {
            if (PathInternal.IsEffectivelyEmpty(path))
                return ReadOnlySpan<char>.Empty;

            int end = PathInternal.GetDirectoryNameOffset(path);
            return end >= 0 ? path.Slice(0, end) : ReadOnlySpan<char>.Empty;
        }

        // Returns the extension of the given path. The returned value includes the
        // period (".") character of the extension except when you have a terminal period when you get string.Empty, such as ".exe" or
        // ".cpp". The returned value is null if the given path is
        // null or if the given path does not include an extension.
        public static string GetExtension(string path)
        {
            if (path == null)
                return null;

            return new string(GetExtension(path.AsReadOnlySpan()));
        }

        /// <summary>
        /// Returns the extension of the given path.
        /// </summary>
        /// <remarks> 
        /// The returned value is an empty ReadOnlySpan if the given path doesnot include an extension.
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
                if (PathInternal.IsDirectoryOrVolumeSeparator(ch))
                    break;
            }
            return ReadOnlySpan<char>.Empty;
        }

        // Returns the name and extension parts of the given path. The resulting
        // string contains the characters of path that follow the last
        // separator in path. The resulting string is null if path is null.
        public static string GetFileName(string path)
        {
            if (path == null)
                return null;

            ReadOnlySpan<char> result = GetFileName(path.AsReadOnlySpan());
            if (path.Length == result.Length)
                return path;

            return new string(result);
        }

        /// <summary> 
        /// The returned ReadOnlySpan contains the characters of the path that follows the last separator in path.
        /// </summary>
        public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
        {
            int offset = PathInternal.FindFileNameIndex(path);
            int count = path.Length - offset;
            return path.Slice(offset, count);
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            if (path == null)
                return null;

            ReadOnlySpan<char> result = GetFileNameWithoutExtension(path.AsReadOnlySpan());
            if (path.Length == result.Length)
                return path;

            return new string(result);
        }

        /// <summary>
        /// Returns the characters between the last separator and last (.) in the path.
        /// </summary>
        public static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path)
        {
            int length = path.Length;
            int offset = PathInternal.FindFileNameIndex(path);

            int end = path.Slice(offset, length - offset).LastIndexOf('.');
            return end == -1 ?
                path.Slice(offset) : // No extension was found
                path.Slice(offset, end);
        }

        // Returns a cryptographically strong random 8.3 string that can be 
        // used as either a folder name or a file name.
        public static unsafe string GetRandomFileName()
        {
            byte* pKey = stackalloc byte[KeyLength];
            Interop.GetRandomBytes(pKey, KeyLength);

            const int RandomFileNameLength = 12;
            char* pRandomFileName = stackalloc char[RandomFileNameLength];
            Populate83FileNameFromRandomBytes(pKey, KeyLength, pRandomFileName, RandomFileNameLength);
            return new string(pRandomFileName, 0, RandomFileNameLength);
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
            {
                throw new ArgumentNullException(nameof(path));
            }
            return IsPathFullyQualified(path.AsReadOnlySpan());
        }

        public static bool IsPathFullyQualified(ReadOnlySpan<char> path)
        {
            return !PathInternal.IsPartiallyQualified(path);
        }

        // Tests if a path includes a file extension. The result is
        // true if the characters that follow the last directory
        // separator ('\\' or '/') or volume separator (':') in the path include 
        // a period (".") other than a terminal period. The result is false otherwise.
        public static bool HasExtension(string path)
        {
            if (path != null)
            {
                return HasExtension(path.AsReadOnlySpan());
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
                if (PathInternal.IsDirectoryOrVolumeSeparator(ch))
                    break;
            }
            return false;
        }

        public static string Combine(string path1, string path2)
        {
            if (path1 == null || path2 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) : nameof(path2));

            return CombineNoChecks(path1, path2);
        }

        public static string Combine(string path1, string path2, string path3)
        {
            if (path1 == null || path2 == null || path3 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) : (path2 == null) ? nameof(path2) : nameof(path3));

            return CombineNoChecks(path1, path2, path3);
        }

        public static string Combine(string path1, string path2, string path3, string path4)
        {
            if (path1 == null || path2 == null || path3 == null || path4 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) : (path2 == null) ? nameof(path2) : (path3 == null) ? nameof(path3) : nameof(path4));

            return CombineNoChecks(path1, path2, path3, path4);
        }

        public static string Combine(params string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            int finalSize = 0;
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
                    finalSize = paths[i].Length;
                }
                else
                {
                    finalSize += paths[i].Length;
                }

                char ch = paths[i][paths[i].Length - 1];
                if (!PathInternal.IsDirectoryOrVolumeSeparator(ch))
                    finalSize++;
            }

            StringBuilder finalPath = StringBuilderCache.Acquire(finalSize);

            for (int i = firstComponent; i < paths.Length; i++)
            {
                if (paths[i].Length == 0)
                {
                    continue;
                }

                if (finalPath.Length == 0)
                {
                    finalPath.Append(paths[i]);
                }
                else
                {
                    char ch = finalPath[finalPath.Length - 1];
                    if (!PathInternal.IsDirectoryOrVolumeSeparator(ch))
                    {
                        finalPath.Append(PathInternal.DirectorySeparatorChar);
                    }

                    finalPath.Append(paths[i]);
                }
            }

            return StringBuilderCache.GetStringAndRelease(finalPath);
        }

        /// <summary>
        /// Combines two paths. Does no validation of paths, only concatenates the paths
        /// and places a directory separator between them if needed.
        /// </summary>
        private static string CombineNoChecks(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
        {
            if (first.Length == 0)
                return second.Length == 0
                    ? string.Empty
                    : new string(second);

            if (second.Length == 0)
                return new string(first);

            if (IsPathRooted(second)) // will change to span version after the span pr is merged
                return new string(second);

            return CombineNoChecksInternal(first, second);
        }

        private static string CombineNoChecks(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third)
        {
            if (first.Length == 0)
                return CombineNoChecks(second, third);
            if (second.Length == 0)
                return CombineNoChecks(first, third);
            if (third.Length == 0)
                return CombineNoChecks(first, second);

            if (IsPathRooted(third))
                return new string(third);
            if (IsPathRooted(second))
                return CombineNoChecks(second, third);

            return CombineNoChecksInternal(first, second, third);            
        }

        private static string CombineNoChecks(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third, ReadOnlySpan<char> fourth)
        {
            if (first.Length == 0)
                return CombineNoChecks(second, third, fourth);
            if (second.Length == 0)
                return CombineNoChecks(first, third, fourth);
            if (third.Length == 0)
                return CombineNoChecks(first, second, fourth);
            if (fourth.Length == 0)
                return CombineNoChecks(first, second, third);

            if (IsPathRooted(fourth))
                return new string(fourth);
            if (IsPathRooted(third))
                return CombineNoChecks(third, fourth);
            if (IsPathRooted(second))
                return CombineNoChecks(second, third, fourth);

            return CombineNoChecksInternal(first, second, third, fourth);
        }

        private unsafe static string CombineNoChecksInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0, "should have dealt with empty paths");

            bool hasSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                || PathInternal.IsDirectorySeparator(second[0]);

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second))
            {
                return string.Create(
                    first.Length + second.Length + (hasSeparator ? 0 : 1),
                    (First: (IntPtr)f, FirstLength: first.Length, Second: (IntPtr)s, SecondLength: second.Length, HasSeparator: hasSeparator),
                    (destination, state) =>
                    {
                        new Span<char>((char*)state.First, state.FirstLength).CopyTo(destination);
                        if (!state.HasSeparator)
                            destination[state.FirstLength] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Second, state.SecondLength).CopyTo(destination.Slice(state.FirstLength + (state.HasSeparator ? 0 : 1)));
                    });
            }
        }

        private unsafe static string CombineNoChecksInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0 && third.Length > 0, "should have dealt with empty paths");

            bool firstHasSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                || PathInternal.IsDirectorySeparator(second[0]);
            bool thirdHasSeparator = PathInternal.IsDirectorySeparator(second[second.Length - 1])
                || PathInternal.IsDirectorySeparator(third[0]);

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second), t = &MemoryMarshal.GetReference(third))
            {
                return string.Create(
                    first.Length + second.Length + third.Length + (firstHasSeparator ? 0 : 1) + (thirdHasSeparator ? 0 : 1),
                    (First: (IntPtr)f, FirstLength: first.Length, Second: (IntPtr)s, SecondLength: second.Length,
                        Third: (IntPtr)t, ThirdLength: third.Length, FirstHasSeparator: firstHasSeparator, ThirdHasSeparator: thirdHasSeparator),
                    (destination, state) =>
                    {
                        new Span<char>((char*)state.First, state.FirstLength).CopyTo(destination);
                        if (!state.FirstHasSeparator)
                            destination[state.FirstLength] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Second, state.SecondLength).CopyTo(destination.Slice(state.FirstLength + (state.FirstHasSeparator ? 0 : 1)));
                        if (!state.ThirdHasSeparator)
                            destination[destination.Length - state.ThirdLength - 1] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Third, state.ThirdLength).CopyTo(destination.Slice(destination.Length - state.ThirdLength));
                    });
            }
        }

        private unsafe static string CombineNoChecksInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third, ReadOnlySpan<char> fourth)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0 && third.Length > 0 && fourth.Length > 0, "should have dealt with empty paths");

            bool firstHasSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                || PathInternal.IsDirectorySeparator(second[0]);
            bool thirdHasSeparator = PathInternal.IsDirectorySeparator(second[second.Length - 1])
                || PathInternal.IsDirectorySeparator(third[0]);
            bool fourthHasSeparator = PathInternal.IsDirectorySeparator(third[third.Length - 1])
                || PathInternal.IsDirectorySeparator(fourth[0]);

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second), t = &MemoryMarshal.GetReference(third), u = &MemoryMarshal.GetReference(fourth))
            {
                return string.Create(
                    first.Length + second.Length + third.Length + fourth.Length + (firstHasSeparator ? 0 : 1) + (thirdHasSeparator ? 0 : 1) + (fourthHasSeparator ? 0 : 1),
                    (First: (IntPtr)f, FirstLength: first.Length, Second: (IntPtr)s, SecondLength: second.Length,
                        Third: (IntPtr)t, ThirdLength: third.Length, Fourth: (IntPtr)u, FourthLength:fourth.Length,
                        FirstHasSeparator: firstHasSeparator, ThirdHasSeparator: thirdHasSeparator, FourthHasSeparator: fourthHasSeparator),
                    (destination, state) =>
                    {
                        new Span<char>((char*)state.First, state.FirstLength).CopyTo(destination);
                        if (!state.FirstHasSeparator)
                            destination[state.FirstLength] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Second, state.SecondLength).CopyTo(destination.Slice(state.FirstLength + (state.FirstHasSeparator ? 0 : 1)));
                        if (!state.ThirdHasSeparator)
                            destination[state.FirstLength + state.SecondLength + (state.FirstHasSeparator ? 0 : 1)] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Third, state.ThirdLength).CopyTo(destination.Slice(state.FirstLength + state.SecondLength + (state.FirstHasSeparator ? 0 : 1) + (state.ThirdHasSeparator ? 0 : 1)));
                        if (!state.FourthHasSeparator)
                            destination[destination.Length - state.FourthLength - 1] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Fourth, state.FourthLength).CopyTo(destination.Slice(destination.Length - state.FourthLength));
                    });
            }
        }

        private static readonly char[] s_base32Char = {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h',
                'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
                'y', 'z', '0', '1', '2', '3', '4', '5'};

        private static unsafe void Populate83FileNameFromRandomBytes(byte* bytes, int byteCount, char* chars, int charCount)
        {
            Debug.Assert(bytes != null);
            Debug.Assert(chars != null);

            // This method requires bytes of length 8 and chars of length 12.
            Debug.Assert(byteCount == 8, $"Unexpected {nameof(byteCount)}");
            Debug.Assert(charCount == 12, $"Unexpected {nameof(charCount)}");

            byte b0 = bytes[0];
            byte b1 = bytes[1];
            byte b2 = bytes[2];
            byte b3 = bytes[3];
            byte b4 = bytes[4];

            // Consume the 5 Least significant bits of the first 5 bytes
            chars[0] = s_base32Char[b0 & 0x1F];
            chars[1] = s_base32Char[b1 & 0x1F];
            chars[2] = s_base32Char[b2 & 0x1F];
            chars[3] = s_base32Char[b3 & 0x1F];
            chars[4] = s_base32Char[b4 & 0x1F];

            // Consume 3 MSB of b0, b1, MSB bits 6, 7 of b3, b4
            chars[5] = s_base32Char[(
                    ((b0 & 0xE0) >> 5) |
                    ((b3 & 0x60) >> 2))];

            chars[6] = s_base32Char[(
                    ((b1 & 0xE0) >> 5) |
                    ((b4 & 0x60) >> 2))];

            // Consume 3 MSB bits of b2, 1 MSB bit of b3, b4
            b2 >>= 5;

            Debug.Assert(((b2 & 0xF8) == 0), "Unexpected set bits");

            if ((b3 & 0x80) != 0)
                b2 |= 0x08;
            if ((b4 & 0x80) != 0)
                b2 |= 0x10;

            chars[7] = s_base32Char[b2];

            // Set the file extension separator
            chars[8] = '.';

            // Consume the 5 Least significant bits of the remaining 3 bytes
            chars[9] = s_base32Char[(bytes[5] & 0x1F)];
            chars[10] = s_base32Char[(bytes[6] & 0x1F)];
            chars[11] = s_base32Char[(bytes[7] & 0x1F)];
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

            for (int i = skip; i < path.Length; i++)
            {
                char c = path[i];

                if (PathInternal.IsDirectorySeparator(c) && i + 1 < path.Length)
                {
                    // Skip this character if it's a directory separator and if the next character is, too,
                    // e.g. "parent//child" => "parent/child"
                    if (PathInternal.IsDirectorySeparator(path[i + 1]))
                    {
                        continue;
                    }

                    // Skip this character and the next if it's referring to the current directory,
                    // e.g. "parent/./child" => "parent/child"
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

                // Normalize the directory separator if needed
                if (c != PathInternal.DirectorySeparatorChar && c == PathInternal.AltDirectorySeparatorChar)
                {
                    c = PathInternal.DirectorySeparatorChar;
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
            return GetRelativePath(relativeTo, path, StringComparison);
        }

        private static string GetRelativePath(string relativeTo, string path, StringComparison comparisonType)
        {
            if (string.IsNullOrEmpty(relativeTo)) throw new ArgumentNullException(nameof(relativeTo));
            if (PathInternal.IsEffectivelyEmpty(path)) throw new ArgumentNullException(nameof(path));
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
            if (PathInternal.EndsInDirectorySeparator(relativeTo))
                relativeToLength--;

            bool pathEndsInSeparator = PathInternal.EndsInDirectorySeparator(path);
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

            StringBuilder sb = StringBuilderCache.Acquire(Math.Max(relativeTo.Length, path.Length));

            // Add parent segments for segments past the common on the "from" path
            if (commonLength < relativeToLength)
            {
                sb.Append(PathInternal.ParentDirectoryPrefix);

                for (int i = commonLength; i < relativeToLength; i++)
                {
                    if (PathInternal.IsDirectorySeparator(relativeTo[i]))
                    {
                        sb.Append(PathInternal.ParentDirectoryPrefix);
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
            int count = pathLength - commonLength;
            if (pathEndsInSeparator)
                count++;

            sb.Append(path, commonLength, count);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        // StringComparison and IsCaseSensitive are also available in PathInternal.CaseSensitivity but we are
        // too low in System.Runtime.Extensions to use it (no FileStream, etc.)

        /// <summary>Returns a comparison that can be used to compare file and directory names for equality.</summary>
        internal static StringComparison StringComparison
        {
            get
            {
                return IsCaseSensitive ?
                    StringComparison.Ordinal :
                    StringComparison.OrdinalIgnoreCase;
            }
        }
    }
}
