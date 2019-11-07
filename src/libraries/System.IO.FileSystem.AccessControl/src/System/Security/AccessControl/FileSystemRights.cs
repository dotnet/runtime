// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.AccessControl
{
    // Constants from winnt.h - search for FILE_WRITE_DATA, etc.
    [Flags]
    public enum FileSystemRights
    {
        // No None field - An ACE with the value 0 cannot grant nor deny.
        ReadData = 0x000001,
        ListDirectory = ReadData,     // For directories
        WriteData = 0x000002,
        CreateFiles = WriteData,    // For directories
        AppendData = 0x000004,
        CreateDirectories = AppendData,   // For directories
        ReadExtendedAttributes = 0x000008,
        WriteExtendedAttributes = 0x000010,
        ExecuteFile = 0x000020,     // For files
        Traverse = ExecuteFile,  // For directories
        // DeleteSubdirectoriesAndFiles only makes sense on directories, but
        // the shell explicitly sets it for files in its UI.  So we'll include
        // it in FullControl.
        DeleteSubdirectoriesAndFiles = 0x000040,
        ReadAttributes = 0x000080,
        WriteAttributes = 0x000100,
        Delete = 0x010000,
        ReadPermissions = 0x020000,
        ChangePermissions = 0x040000,
        TakeOwnership = 0x080000,
        // From the Core File Services team, CreateFile always requires
        // SYNCHRONIZE access.  Very tricksy, CreateFile is.
        Synchronize = 0x100000,  // Can we wait on the handle?
        FullControl = 0x1F01FF,

        // These map to what Explorer sets, and are what most users want.
        // However, an ACL editor will also want to set the Synchronize
        // bit when allowing access, and exclude the synchronize bit when
        // denying access.
        Read = ReadData | ReadExtendedAttributes | ReadAttributes | ReadPermissions,
        ReadAndExecute = Read | ExecuteFile,
        Write = WriteData | AppendData | WriteExtendedAttributes | WriteAttributes,
        Modify = ReadAndExecute | Write | Delete,
    }
}
