// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://learn.microsoft.com/windows/win32/api/winioctl/ns-winioctl-file_set_sparse_buffer
        internal struct FILE_SET_SPARSE_BUFFER
        {
            internal BOOLEAN SetSparse;
        }
    }
}
