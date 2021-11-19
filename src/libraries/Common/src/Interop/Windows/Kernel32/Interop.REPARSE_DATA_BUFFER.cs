// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://docs.microsoft.com/windows-hardware/drivers/ifs/fsctl-get-reparse-point
        internal const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16 * 1024;

        internal const uint SYMLINK_FLAG_RELATIVE = 1;

        // https://msdn.microsoft.com/library/windows/hardware/ff552012.aspx
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SymbolicLinkReparseBuffer
        {
            internal uint ReparseTag;
            internal ushort ReparseDataLength;
            internal ushort Reserved;
            internal ushort SubstituteNameOffset;
            internal ushort SubstituteNameLength;
            internal ushort PrintNameOffset;
            internal ushort PrintNameLength;
            internal uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MountPointReparseBuffer
        {
            public uint ReparseTag;
            public ushort ReparseDataLength;
            public ushort Reserved;
            public ushort SubstituteNameOffset;
            public ushort SubstituteNameLength;
            public ushort PrintNameOffset;
            public ushort PrintNameLength;
        }
    }
}
