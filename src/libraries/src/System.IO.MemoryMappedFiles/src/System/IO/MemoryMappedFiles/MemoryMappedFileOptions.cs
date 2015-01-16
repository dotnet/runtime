// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.MemoryMappedFiles
{
    [Flags]
    public enum MemoryMappedFileOptions
    {
        None = 0,
        DelayAllocatePages = 0x4000000
    }
}
