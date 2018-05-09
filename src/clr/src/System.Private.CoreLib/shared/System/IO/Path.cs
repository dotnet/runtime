// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
                    if (PathInternal.IsDirectorySeparator(ch)) break;
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
        public static string GetDirectoryName(string path)
        {
            if (path == null || PathInternal.IsEffectivelyEmpty(path))
                return null;

            int end = GetDirectoryNameOffset(path);
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

        private static int GetDirectoryNameOffset(ReadOnlySpan<char> path)
        {
            int rootLength = PathInternal.GetRootLength(path);
            int end = path.Length;
            if (end <= rootLength)
                return -1;

            while (end > rootLength && !PathInternal.IsDirectorySeparator(path[--end]));

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
        public static string GetExtension(string path)
        {
            if (path == null)
                return null;

            return new string(GetExtension(path.AsSpan()));
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
        public static string GetFileName(string path)
        {
            if (path == null)
                return null;

            ReadOnlySpan<char> result = GetFileName(path.AsSpan());
            if (path.Length == result.Length)
                return path;

            return new string(result);
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

        public static string GetFileNameWithoutExtension(string path)
        {
            if (path == null)
                return null;

            ReadOnlySpan<char> result = GetFileNameWithoutExtension(path.AsSpan());
            if (path.Length == result.Length)
                return path;

            return new string(result);
        }

        /// <summary>
        /// Returns the characters between the last separator and last (.) in the path.
        /// </summary>
        public static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path)
        {
            ReadOnlySpan<char> fileName = GetFileName(path);
            int lastPeriod = fileName.LastIndexOf('.');
            return lastPeriod == -1 ?
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
        public static bool HasExtension(string path)
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
                if (!PathInternal.IsDirectorySeparator(ch))
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
                    if (!PathInternal.IsDirectorySeparator(ch))
                    {
                        finalPath.Append(PathInternal.DirectorySeparatorChar);
                    }

                    finalPath.Append(paths[i]);
                }
            }

            return StringBuilderCache.GetStringAndRelease(finalPath);
        }

        // Unlike Combine(), Join() methods do not consider rooting. They simply combine paths, ensuring that there
        // is a directory separator between them.

        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
        {
            if (path1.Length == 0)
                return new string(path2);
            if (path2.Length == 0)
                return new string(path1);

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

            bool needsSeparator = !(PathInternal.EndsInDirectorySeparator(path1) || PathInternal.StartsWithDirectorySeparator(path2));
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

            int neededSeparators = PathInternal.EndsInDirectorySeparator(path1) || PathInternal.StartsWithDirectorySeparator(path2) ? 0 : 1;
            bool needsSecondSeparator = !(PathInternal.EndsInDirectorySeparator(path2) || PathInternal.StartsWithDirectorySeparator(path3));
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

            return JoinInternal(first, second);
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

            return JoinInternal(first, second, third);
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

            return JoinInternal(first, second, third, fourth);
        }

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
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

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third)
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

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third, ReadOnlySpan<char> fourth)
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
