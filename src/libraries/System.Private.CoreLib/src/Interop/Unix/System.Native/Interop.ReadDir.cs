// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum NodeType : int
        {
            DT_UNKNOWN = 0,
            DT_FIFO = 1,
            DT_CHR = 2,
            DT_DIR = 4,
            DT_BLK = 6,
            DT_REG = 8,
            DT_LNK = 10,
            DT_SOCK = 12,
            DT_WHT = 14
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct DirectoryEntry
        {
            internal byte* Name;
            internal int NameLength;
            internal NodeType InodeType;
            internal const int NameBufferSize = 256;

            internal ReadOnlySpan<char> GetName(Span<char> buffer)
            {
                Debug.Assert(buffer.Length >= Encoding.UTF8.GetMaxCharCount(NameBufferSize - 1), "should have enough space for the max file name");
                Debug.Assert(Name != null, "should not have a null name");

                ReadOnlySpan<byte> nameBytes = (NameLength == -1)
                    // In this case the struct was allocated via struct dirent *readdir(DIR *dirp);
                    ? new ReadOnlySpan<byte>(Name, new ReadOnlySpan<byte>(Name, NameBufferSize - 1).IndexOf<byte>(0))
                    : new ReadOnlySpan<byte>(Name, NameLength);

                Debug.Assert(nameBytes.Length > 0, "we shouldn't have gotten a garbage value from the OS");
                if (nameBytes.Length == 0)
                    return buffer.Slice(0, 0);

                int charCount = Encoding.UTF8.GetChars(nameBytes, buffer);
                ReadOnlySpan<char> value = buffer.Slice(0, charCount);
                Debug.Assert(NameLength != -1 || !value.Contains('\0'), "should not have embedded nulls if we parsed the end of string");
                return value;
            }
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_OpenDir", SetLastError = true)]
        internal static extern IntPtr OpenDir(string path);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetReadDirRBufferSize", SetLastError = false)]
        internal static extern int GetReadDirRBufferSize();

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadDirR", SetLastError = false)]
        internal static extern unsafe int ReadDirR(IntPtr dir, byte* buffer, int bufferSize, out DirectoryEntry outputEntry);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CloseDir", SetLastError = true)]
        internal static extern int CloseDir(IntPtr dir);
    }
}
