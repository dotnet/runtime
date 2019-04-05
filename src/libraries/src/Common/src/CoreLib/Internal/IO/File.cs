// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Diagnostics;
using System.Security;
using System.IO;

namespace Internal.IO
{
    //
    // Subsetted clone of System.IO.File for internal runtime use.
    // Keep in sync with https://github.com/dotnet/corefx/tree/master/src/System.IO.FileSystem.
    //
    internal static partial class File
    {
        // Tests if a file exists. The result is true if the file
        // given by the specified path exists; otherwise, the result is
        // false.  Note that if path describes a directory,
        // Exists will return true.
        public static bool Exists(string? path)
        {
            try
            {
                if (path == null)
                    return false;
                if (path.Length == 0)
                    return false;

                path = Path.GetFullPath(path);

                // After normalizing, check whether path ends in directory separator.
                // Otherwise, FillAttributeInfo removes it and we may return a false positive.
                // GetFullPath should never return null
                Debug.Assert(path != null, "File.Exists: GetFullPath returned null");
                if (path.Length > 0 && PathInternal.IsDirectorySeparator(path[path.Length - 1]))
                {
                    return false;
                }

                return InternalExists(path);
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { } // Security can throw this on ":"
            catch (SecurityException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return false;
        }

        public static byte[] ReadAllBytes(string path)
        {
            // bufferSize == 1 used to avoid unnecessary buffer in FileStream
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1))
            {
                long fileLength = fs.Length;
                if (fileLength > int.MaxValue)
                    throw new IOException(SR.IO_FileTooLong2GB);

                int index = 0;
                int count = (int)fileLength;
                byte[] bytes = new byte[count];
                while (count > 0)
                {
                    int n = fs.Read(bytes, index, count);
                    if (n == 0)
                        throw Error.GetEndOfFile();
                    index += n;
                    count -= n;
                }
                return bytes;
            }
        }
    }
}
