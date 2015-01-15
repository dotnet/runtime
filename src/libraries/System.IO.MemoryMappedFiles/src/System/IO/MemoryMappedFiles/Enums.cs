// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.IO.MemoryMappedFiles
{
    // This enum maps to both the PAGE_XXX and FILE_MAP_XXX native macro definitions.
    // It is used in places that check the page access of the memory mapped file. ACL
    // access is controlled by MemoryMappedFileRights.
    public enum MemoryMappedFileAccess
    {
        ReadWrite = 0,
        Read,
        Write,   // Write is valid only when creating views and not when creating MemoryMappedFiles   
        CopyOnWrite,
        ReadExecute,
        ReadWriteExecute,
    }

    [Flags]
    public enum MemoryMappedFileOptions
    {
        None = 0,
        DelayAllocatePages = 0x4000000
    }
}
