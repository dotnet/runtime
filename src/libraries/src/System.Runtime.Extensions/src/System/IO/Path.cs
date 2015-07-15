// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security;
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
                CheckInvalidPathChars(path);

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
                CheckInvalidPathChars(path);

                string normalizedPath = NormalizePath(path, fullCheck: false);

                // If there are no permissions for PathDiscovery to this path, we should NOT expand the short paths
                // as this would leak information about paths to which the user would not have access to.
                if (path.Length > 0)
                {
                    try
                    {
                        // If we were passed in a path with \\?\ we need to remove it as FileIOPermission does not like it.
                        string tempPath = RemoveLongPathPrefix(path);

                        // FileIOPermission cannot handle paths that contain ? or *
                        // So we only pass to FileIOPermission the text up to them.
                        int pos = 0;
                        while (pos < tempPath.Length && (tempPath[pos] != '?' && tempPath[pos] != '*'))
                            pos++;

                        // GetFullPath will Demand that we have the PathDiscovery FileIOPermission and thus throw 
                        // SecurityException if we don't. 
                        // While we don't use the result of this call we are using it as a consistent way of 
                        // doing the security checks. 
                        if (pos > 0)
                            GetFullPath(tempPath.Substring(0, pos));
                    }
                    catch (SecurityException)
                    {
                        // If the user did not have permissions to the path, make sure that we don't leak expanded short paths
                        // Only re-normalize if the original path had a ~ in it.
                        if (path.IndexOf("~", StringComparison.Ordinal) != -1)
                        {
                            normalizedPath = NormalizePath(path, fullCheck: false, expandShortPaths: false);
                        }
                    }
                    catch (PathTooLongException) { }
                    catch (NotSupportedException) { }  // Security can throw this on "c:\foo:"
                    catch (IOException) { }
                    catch (ArgumentException) { } // The normalizePath with fullCheck will throw this for file: and http:
                }

                path = normalizedPath;

                int root = GetRootLength(path);
                int i = path.Length;
                if (i > root)
                {
                    i = path.Length;
                    if (i == root) return null;
                    while (i > root && !IsDirectorySeparator(path[--i])) ;
                    return path.Substring(0, i);
                }
            }
            return null;
        }

        public static char[] GetInvalidPathChars()
        {
            return (char[])InvalidPathChars.Clone();
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

            CheckInvalidPathChars(path);
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

        private static string GetFullPathInternal(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

            return NormalizePath(path, fullCheck: true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private static string NormalizePath(string path, bool fullCheck)
        {
            return NormalizePath(path, fullCheck, MaxPath);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private static string NormalizePath(string path, bool fullCheck, bool expandShortPaths)
        {
            return NormalizePath(path, fullCheck, MaxPath, expandShortPaths);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private static string NormalizePath(string path, bool fullCheck, int maxPathLength)
        {
            return NormalizePath(path, fullCheck, maxPathLength, expandShortPaths: true);
        }

        // Returns the name and extension parts of the given path. The resulting
        // string contains the characters of path that follow the last
        // separator in path. The resulting string is null if path is null.
        [Pure]
        public static string GetFileName(string path)
        {
            if (path != null)
            {
                CheckInvalidPathChars(path);

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

        // Returns the root portion of the given path. The resulting string
        // consists of those rightmost characters of the path that constitute the
        // root of the path. Possible patterns for the resulting string are: An
        // empty string (a relative path on the current drive), "\" (an absolute
        // path on the current drive), "X:" (a relative path on a given drive,
        // where X is the drive letter), "X:\" (an absolute path on a given drive),
        // and "\\server\share" (a UNC path for a given server and share name).
        // The resulting string is null if path is null.
        [Pure]
        public static string GetPathRoot(string path)
        {
            if (path == null) return null;
            path = NormalizePath(path, fullCheck: false);
            return path.Substring(0, GetRootLength(path));
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
                CheckInvalidPathChars(path);

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

            CheckInvalidPathChars(path1);
            CheckInvalidPathChars(path2);

            return CombineNoChecks(path1, path2);
        }

        public static string Combine(string path1, string path2, string path3)
        {
            if (path1 == null || path2 == null || path3 == null)
                throw new ArgumentNullException((path1 == null) ? "path1" : (path2 == null) ? "path2" : "path3");
            Contract.EndContractBlock();

            CheckInvalidPathChars(path1);
            CheckInvalidPathChars(path2);
            CheckInvalidPathChars(path3);

            return CombineNoChecks(path1, path2, path3);
        }

        public static string Combine(params string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException("paths");
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
                    throw new ArgumentNullException("paths");
                }

                if (paths[i].Length == 0)
                {
                    continue;
                }

                CheckInvalidPathChars(paths[i]);

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

        private static bool HasIllegalCharacters(string path, bool checkAdditional)
        {
            Contract.Requires(path != null);
            return path.IndexOfAny(checkAdditional ? InvalidPathCharsWithAdditionalChecks : InvalidPathChars) >= 0;
        }

        private static void CheckInvalidPathChars(string path, bool checkAdditional = false)
        {
            Debug.Assert(path != null);

            if (HasIllegalCharacters(path, checkAdditional))
                throw new ArgumentException(SR.Argument_InvalidPathChars, "path");
        }
    }
}
