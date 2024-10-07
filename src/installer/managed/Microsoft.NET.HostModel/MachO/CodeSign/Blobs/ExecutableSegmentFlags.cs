// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.CodeSign.Blobs
{
    internal enum ExecutableSegmentFlags : ulong
    {
        MainBinary = 1,
        AllowUnsigned = 0x10,
        Debugger = 0x20,
        Jit = 0x40,
        SkipLibraryValidation = 0x80,
        CanLoadCdHash = 0x100,
        CanExecuteCdHash = 0x200,
    }
}
