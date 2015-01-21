// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.MemoryMappedFiles
{
    public enum MemoryMappedFileAccess
    {
        ReadWrite = 0,
        Read,
        Write,   // Write is valid only when creating views and not when creating MemoryMappedFiles   
        CopyOnWrite,
        ReadExecute,
        ReadWriteExecute,
    }
}
