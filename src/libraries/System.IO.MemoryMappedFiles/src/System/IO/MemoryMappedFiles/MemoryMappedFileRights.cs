// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.MemoryMappedFiles
{
    [Flags]
    public enum MemoryMappedFileRights
    {
        // No None field - An ACE with the value 0 cannot grant nor deny.
        CopyOnWrite = 0x000001,
        Write = 0x000002,
        Read = 0x000004,
        Execute = 0x000008,

        Delete = 0x010000,
        ReadPermissions = 0x020000,
        ChangePermissions = 0x040000,
        TakeOwnership = 0x080000,
        //Synchronize                = Not supported by memory mapped files

        ReadWrite = Read | Write,
        ReadExecute = Read | Execute,
        ReadWriteExecute = Read | Write | Execute,

        FullControl = CopyOnWrite | Read | Write | Execute | Delete |
                                       ReadPermissions | ChangePermissions | TakeOwnership,

        AccessSystemSecurity = 0x01000000, // Allow changes to SACL
    }
}
