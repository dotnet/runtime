// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class File
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileStream" /> class with the specified path, creation mode, read/write and sharing permission, the access other FileStreams can have to the same file, the buffer size, additional file options and the allocation size.
        /// </summary>
        /// <remarks><see cref="FileStream(string,System.IO.FileStreamOptions)"/> for information about exceptions.</remarks>
        public static FileStream Open(string path, FileStreamOptions options) => new FileStream(path, options);

        /// <summary>
        /// Initializes a new instance of the <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle" /> class with the specified path, creation mode, read/write and sharing permission, the access other SafeFileHandles can have to the same file, additional file options and the allocation size.
        /// </summary>
        /// <param name="path">A relative or absolute path for the file that the current <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle" /> instance will encapsulate.</param>
        /// <param name="mode">One of the enumeration values that determines how to open or create the file. The default value is <see cref="FileMode.Open" /></param>
        /// <param name="access">A bitwise combination of the enumeration values that determines how the file can be accessed. The default value is <see cref="FileAccess.Read" /></param>
        /// <param name="share">A bitwise combination of the enumeration values that determines how the file will be shared by processes. The default value is <see cref="FileShare.Read" />.</param>
        /// <param name="preallocationSize">The initial allocation size in bytes for the file. A positive value is effective only when a regular file is being created, overwritten, or replaced.
        /// Negative values are not allowed. In other cases (including the default 0 value), it's ignored.</param>
        /// <param name="options">An object that describes optional <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle" /> parameters to use.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="path" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="path" /> is an empty string (""), contains only white space, or contains one or more invalid characters.
        /// -or-
        /// <paramref name="path" /> refers to a non-file device, such as <c>CON:</c>, <c>COM1:</c>, <c>LPT1:</c>, etc. in an NTFS environment.</exception>
        /// <exception cref="T:System.NotSupportedException"><paramref name="path" /> refers to a non-file device, such as <c>CON:</c>, <c>COM1:</c>, <c>LPT1:</c>, etc. in a non-NTFS environment.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="preallocationSize" /> is negative.
        /// -or-
        /// <paramref name="mode" />, <paramref name="access" />, or <paramref name="share" /> contain an invalid value.</exception>
        /// <exception cref="T:System.IO.FileNotFoundException">The file cannot be found, such as when <paramref name="mode" /> is <see cref="FileMode.Truncate" /> or <see cref="FileMode.Open" />, and the file specified by <paramref name="path" /> does not exist. The file must already exist in these modes.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error, such as specifying <see cref="FileMode.CreateNew" /> when the file specified by <paramref name="path" /> already exists, occurred.
        ///  -or-
        ///  The disk was full (when <paramref name="preallocationSize" /> was provided and <paramref name="path" /> was pointing to a regular file).
        ///  -or-
        ///  The file was too large (when <paramref name="preallocationSize" /> was provided and <paramref name="path" /> was pointing to a regular file).</exception>
        /// <exception cref="T:System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException">The <paramref name="access" /> requested is not permitted by the operating system for the specified <paramref name="path" />, such as when <paramref name="access" />  is <see cref="FileAccess.Write" /> or <see cref="FileAccess.ReadWrite" /> and the file or directory is set for read-only access.
        ///  -or-
        /// <see cref="F:System.IO.FileOptions.Encrypted" /> is specified for <paramref name="options" />, but file encryption is not supported on the current platform.</exception>
        /// <exception cref="T:System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. </exception>
        public static SafeFileHandle OpenHandle(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read,
            FileShare share = FileShare.Read, FileOptions options = FileOptions.None, long preallocationSize = 0)
        {
            Strategies.FileStreamHelpers.ValidateArguments(path, mode, access, share, bufferSize: 0, options, preallocationSize);

            return SafeFileHandle.Open(Path.GetFullPath(path), mode, access, share, options, preallocationSize);
        }

        private static byte[] ReadAllBytesUnknownLength(FileStream fs)
        {
            byte[]? rentedArray = null;
            Span<byte> buffer = stackalloc byte[512];
            try
            {
                int bytesRead = 0;
                while (true)
                {
                    if (bytesRead == buffer.Length)
                    {
                        uint newLength = (uint)buffer.Length * 2;
                        if (newLength > Array.MaxLength)
                        {
                            newLength = (uint)Math.Max(Array.MaxLength, buffer.Length + 1);
                        }

                        byte[] tmp = ArrayPool<byte>.Shared.Rent((int)newLength);
                        buffer.CopyTo(tmp);
                        byte[]? oldRentedArray = rentedArray;
                        buffer = rentedArray = tmp;
                        if (oldRentedArray != null)
                        {
                            ArrayPool<byte>.Shared.Return(oldRentedArray);
                        }
                    }

                    Debug.Assert(bytesRead < buffer.Length);
                    int n = fs.Read(buffer.Slice(bytesRead));
                    if (n == 0)
                    {
                        return buffer.Slice(0, bytesRead).ToArray();
                    }
                    bytesRead += n;
                }
            }
            finally
            {
                if (rentedArray != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedArray);
                }
            }
        }
    }
}
