// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.MemoryMappedFiles
{
    [Serializable]
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
