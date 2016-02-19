// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;

namespace System.IO
{
    // Provides methods for processing file system strings in a cross-platform manner.
    // Most of the methods don't do a complete parsing (such as examining a UNC hostname), 
    // but they will handle most string operations.  
    public static partial class Path
    {
        // Platform specific alternate directory separator character.  
        // There is only one directory separator char on Unix, which is the same
        // as the alternate separator on Windows, so same definition is used for both.
        public static readonly char AltDirectorySeparatorChar = '/';

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
        // is null, any exsiting extension is removed from path.
        public static string ChangeExtension(string path, string extension)
        {
            if (path != null)
            {
                PathInternal.CheckInvalidPathChars(path);

                string s = path;
                for (int i = path.Length - 1; i >= 0; i--)
                {
                    char ch = path[i];
                    if (ch == '.')
                    {
                        s = path.Substring(0, i);
                        break;
                    }
                    if (IsDirectoryOrVolumeSeparator(ch)) break;
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
            if (path != null)
            {
                PathInternal.CheckInvalidPathChars(path);
                path = PathInternal.NormalizeDirectorySeparators(path);
                int root = PathInternal.GetRootLength(path);

                int i = path.Length;
                if (i > root)
                {
                    while (i > root && !PathInternal.IsDirectorySeparator(path[--i])) ;
                    return path.Substring(0, i);
                }
            }
            return null;
        }

        public static char[] GetInvalidPathChars()
        {
            return (char[])PathInternal.InvalidPathChars.Clone();
        }

        public static char[] GetInvalidFileNameChars()
        {
            return (char[])InvalidFileNameChars.Clone();
        }

        // Returns the extension of the given path. The returned value includes the
        // period (".") character of the extension except when you have a terminal period when you get string.Empty, such as ".exe" or
        // ".cpp". The returned value is null if the given path is
        // null or if the given path does not include an extension.
        [Pure]
        public static string GetExtension(string path)
        {
            if (path == null)
                return null;

            PathInternal.CheckInvalidPathChars(path);
            int length = path.Length;
            for (int i = length - 1; i >= 0; i--)
            {
                char ch = path[i];
                if (ch == '.')
                {
                    if (i != length - 1)
                        return path.Substring(i, length - i);
                    else
                        return string.Empty;
                }
                if (IsDirectoryOrVolumeSeparator(ch))
                    break;
            }
            return string.Empty;
        }

        // Returns the name and extension parts of the given path. The resulting
        // string contains the characters of path that follow the last
        // separator in path. The resulting string is null if path is null.
        [Pure]
        public static string GetFileName(string path)
        {
            if (path != null)
            {
                PathInternal.CheckInvalidPathChars(path);

                int length = path.Length;
                for (int i = length - 1; i >= 0; i--)
                {
                    char ch = path[i];
                    if (IsDirectoryOrVolumeSeparator(ch))
                        return path.Substring(i + 1, length - i - 1);
                }
            }
            return path;
        }

        [Pure]
        public static string GetFileNameWithoutExtension(string path)
        {
            if (path == null)
                return null;

            path = GetFileName(path);
            int i;
            return (i = path.LastIndexOf('.')) == -1 ?
                path : // No path extension found
                path.Substring(0, i);
        }

        // Returns a cryptographically strong random 8.3 string that can be 
        // used as either a folder name or a file name.
        public static string GetRandomFileName()
        {
            // 5 bytes == 40 bits == 40/5 == 8 chars in our encoding.
            // So 10 bytes provides 16 chars, of which we need 11
            // for the 8.3 name.
            byte[] key = CreateCryptoRandomByteArray(10);

            // rndCharArray is expected to be 16 chars
            char[] rndCharArray = ToBase32StringSuitableForDirName(key).ToCharArray();
            rndCharArray[8] = '.';
            return new string(rndCharArray, 0, 12);
        }

        // Returns a unique temporary file name, and creates a 0-byte file by that
        // name on disk.
        [System.Security.SecuritySafeCritical]
        public static string GetTempFileName()
        {
            return InternalGetTempFileName(checkHost: true);
        }

        // Tests if a path includes a file extension. The result is
        // true if the characters that follow the last directory
        // separator ('\\' or '/') or volume separator (':') in the path include 
        // a period (".") other than a terminal period. The result is false otherwise.
        [Pure]
        public static bool HasExtension(string path)
        {
            if (path != null)
            {
                PathInternal.CheckInvalidPathChars(path);

                for (int i = path.Length - 1; i >= 0; i--)
                {
                    char ch = path[i];
                    if (ch == '.')
                    {
                        return i != path.Length - 1;
                    }
                    if (IsDirectoryOrVolumeSeparator(ch)) break;
                }
            }
            return false;
        }

        public static string Combine(string path1, string path2)
        {
            if (path1 == null || path2 == null)
                throw new ArgumentNullException((path1 == null) ? "path1" : "path2");
            Contract.EndContractBlock();

            PathInternal.CheckInvalidPathChars(path1);
            PathInternal.CheckInvalidPathChars(path2);

            return CombineNoChecks(path1, path2);
        }

        public static string Combine(string path1, string path2, string path3)
        {
            if (path1 == null || path2 == null || path3 == null)
                throw new ArgumentNullException((path1 == null) ? "path1" : (path2 == null) ? "path2" : "path3");
            Contract.EndContractBlock();

            PathInternal.CheckInvalidPathChars(path1);
            PathInternal.CheckInvalidPathChars(path2);
            PathInternal.CheckInvalidPathChars(path3);

            return CombineNoChecks(path1, path2, path3);
        }

        public static string Combine(params string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }
            Contract.EndContractBlock();

            int finalSize = 0;
            int firstComponent = 0;

            // We have two passes, the first calcuates how large a buffer to allocate and does some precondition
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

                PathInternal.CheckInvalidPathChars(paths[i]);

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
                if (!IsDirectoryOrVolumeSeparator(ch))
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
                    if (!IsDirectoryOrVolumeSeparator(ch))
                    {
                        finalPath.Append(DirectorySeparatorChar);
                    }

                    finalPath.Append(paths[i]);
                }
            }

            return StringBuilderCache.GetStringAndRelease(finalPath);
        }

        private static string CombineNoChecks(string path1, string path2)
        {
            if (path2.Length == 0)
                return path1;

            if (path1.Length == 0)
                return path2;

            if (IsPathRooted(path2))
                return path2;

            char ch = path1[path1.Length - 1];
            return IsDirectoryOrVolumeSeparator(ch) ?
                path1 + path2 :
                path1 + DirectorySeparatorCharAsString + path2;
        }

        private static string CombineNoChecks(string path1, string path2, string path3)
        {
            if (path1.Length == 0)
                return CombineNoChecks(path2, path3);
            if (path2.Length == 0)
                return CombineNoChecks(path1, path3);
            if (path3.Length == 0)
                return CombineNoChecks(path1, path2);

            if (IsPathRooted(path3))
                return path3;
            if (IsPathRooted(path2))
                return CombineNoChecks(path2, path3);

            bool hasSep1 = IsDirectoryOrVolumeSeparator(path1[path1.Length - 1]);
            bool hasSep2 = IsDirectoryOrVolumeSeparator(path2[path2.Length - 1]);

            if (hasSep1 && hasSep2)
            {
                return path1 + path2 + path3;
            }
            else if (hasSep1)
            {
                return path1 + path2 + DirectorySeparatorCharAsString + path3;
            }
            else if (hasSep2)
            {
                return path1 + DirectorySeparatorCharAsString + path2 + path3;
            }
            else
            {
                // string.Concat only has string-based overloads up to four arguments; after that requires allocating
                // a params string[].  Instead, try to use a cached StringBuilder.
                StringBuilder sb = StringBuilderCache.Acquire(path1.Length + path2.Length + path3.Length + 2);
                sb.Append(path1)
                  .Append(DirectorySeparatorChar)
                  .Append(path2)
                  .Append(DirectorySeparatorChar)
                  .Append(path3);
                return StringBuilderCache.GetStringAndRelease(sb);
            }
        }

        private static readonly char[] s_Base32Char = {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h',
                'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
                'y', 'z', '0', '1', '2', '3', '4', '5'};

        private static string ToBase32StringSuitableForDirName(byte[] buff)
        {
            // This routine is optimised to be used with buffs of length 20
            Debug.Assert(((buff.Length % 5) == 0), "Unexpected hash length");

            // For every 5 bytes, 8 characters are appended.
            StringBuilder sb = StringBuilderCache.Acquire();

            // Create l char for each of the last 5 bits of each byte.  
            // Consume 3 MSB bits 5 bytes at a time.
            int len = buff.Length;
            int i = 0;
            do
            {
                byte b0 = (i < len) ? buff[i++] : (byte)0;
                byte b1 = (i < len) ? buff[i++] : (byte)0;
                byte b2 = (i < len) ? buff[i++] : (byte)0;
                byte b3 = (i < len) ? buff[i++] : (byte)0;
                byte b4 = (i < len) ? buff[i++] : (byte)0;

                // Consume the 5 Least significant bits of each byte
                sb.Append(s_Base32Char[b0 & 0x1F]);
                sb.Append(s_Base32Char[b1 & 0x1F]);
                sb.Append(s_Base32Char[b2 & 0x1F]);
                sb.Append(s_Base32Char[b3 & 0x1F]);
                sb.Append(s_Base32Char[b4 & 0x1F]);

                // Consume 3 MSB of b0, b1, MSB bits 6, 7 of b3, b4
                sb.Append(s_Base32Char[(
                        ((b0 & 0xE0) >> 5) |
                        ((b3 & 0x60) >> 2))]);

                sb.Append(s_Base32Char[(
                        ((b1 & 0xE0) >> 5) |
                        ((b4 & 0x60) >> 2))]);

                // Consume 3 MSB bits of b2, 1 MSB bit of b3, b4

                b2 >>= 5;

                Debug.Assert(((b2 & 0xF8) == 0), "Unexpected set bits");

                if ((b3 & 0x80) != 0)
                    b2 |= 0x08;
                if ((b4 & 0x80) != 0)
                    b2 |= 0x10;

                sb.Append(s_Base32Char[b2]);

            } while (i < len);

            return StringBuilderCache.GetStringAndRelease(sb);
        }
    }
}
