// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    [Flags]
    internal enum HANDLE_OPTIONS : uint
    {
        HO_NONE = 0,
        HO_OPEN_REQUIRING_OPLOCK = 0x40000,
        HO_DELETE_ON_CLOSE = 0x4000000,
        HO_SEQUENTIAL_SCAN = 0x8000000,
        HO_RANDOM_ACCESS = 0x10000000,
        HO_NO_BUFFERING = 0x20000000,
        HO_OVERLAPPED = 0x40000000,
        HO_WRITE_THROUGH = 0x80000000
    }
}
