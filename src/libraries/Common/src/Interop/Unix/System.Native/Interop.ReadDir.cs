// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            internal ReadOnlySpan<char> GetName(Span<char> buffer)
            {
                Debug.Assert(Name != null, "should not have a null name");

                ReadOnlySpan<byte> nameBytes = (NameLength == -1)
                    ? MemoryMarshal.CreateReadOnlySpanFromNullTerminated(Name)
                    : new ReadOnlySpan<byte>(Name, NameLength);

                Debug.Assert(nameBytes.Length > 0, "we shouldn't have gotten a garbage value from the OS");

                int charCount = Encoding.UTF8.GetChars(nameBytes, buffer);
                ReadOnlySpan<char> value = buffer.Slice(0, charCount);
                Debug.Assert(!value.Contains('\0'), "should not have embedded nulls");
                return value;
            }
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_OpenDir", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial IntPtr OpenDir(string path);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetReadDirRBufferSize", SetLastError = false)]
        [SuppressGCTransition]
        internal static partial int GetReadDirRBufferSize();

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadDirR")]
        internal static unsafe partial int ReadDirR(IntPtr dir, byte* buffer, int bufferSize, DirectoryEntry* outputEntry);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_CloseDir", SetLastError = true)]
        internal static partial int CloseDir(IntPtr dir);
    }
}
