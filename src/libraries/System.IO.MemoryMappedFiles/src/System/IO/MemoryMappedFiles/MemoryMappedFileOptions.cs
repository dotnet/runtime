// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.MemoryMappedFiles
{
    [Flags]
    public enum MemoryMappedFileOptions
    {
        None = 0,
        DelayAllocatePages = 0x4000000
    }
}
