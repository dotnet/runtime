// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://docs.microsoft.com/windows-hardware/drivers/ifs/fsctl-get-reparse-point
        public const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16 * 1024;

        public const uint SYMLINK_FLAG_RELATIVE = 1;

        // https://msdn.microsoft.com/library/windows/hardware/ff552012.aspx
        // We don't need all the struct fields; omitting the rest.
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct REPARSE_DATA_BUFFER
        {
            public uint ReparseTag;
            public ushort ReparseDataLength;
            public ushort Reserved;
            public SymbolicLinkReparseBuffer ReparseBufferSymbolicLink;

            [StructLayout(LayoutKind.Sequential)]
            public struct SymbolicLinkReparseBuffer
            {
                public ushort SubstituteNameOffset;
                public ushort SubstituteNameLength;
                public ushort PrintNameOffset;
                public ushort PrintNameLength;
                public uint Flags;
            }
        }
    }
}
