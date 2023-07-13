// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://learn.microsoft.com/windows/win32/api/winioctl/ns-winioctl-fsctl_set_integrity_information_buffer
        internal struct FSCTL_SET_INTEGRITY_INFORMATION_BUFFER
        {
            internal ushort ChecksumAlgorithm;
            internal ushort Reserved;
            internal uint Flags;
        }
    }
}
