// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://learn.microsoft.com/windows/win32/api/winioctl/ns-winioctl-duplicate_extents_data
        internal struct DUPLICATE_EXTENTS_DATA
        {
            internal IntPtr FileHandle;
            internal long SourceFileOffset;
            internal long TargetFileOffset;
            internal long ByteCount;
        }
    }
}
