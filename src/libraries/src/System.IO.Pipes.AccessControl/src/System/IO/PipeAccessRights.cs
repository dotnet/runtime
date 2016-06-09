// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Pipes
{
    [Flags]
    public enum PipeAccessRights
    {
        // No None field - An ACE with the value 0 cannot grant nor deny.
        ReadData = 0x000001,
        WriteData = 0x000002,

        // Note that all client named pipes require ReadAttributes access even if the user does not specify it.
        // (This is because CreateFile slaps on the requirement before calling NTCreateFile (at least in WinXP SP2)).
        ReadAttributes = 0x000080,
        WriteAttributes = 0x000100,

        // These aren't really needed since there is no operation that requires this access, but they are left here
        // so that people can specify ACLs that others can open by specifying a PipeDirection rather than a
        // PipeAccessRights (PipeDirection.In/Out maps to GENERIC_READ/WRITE access).
        ReadExtendedAttributes = 0x000008,
        WriteExtendedAttributes = 0x000010,

        CreateNewInstance = 0x000004, // AppendData

        // Again, this is not needed but it should be here so that our FullControl matches windows.
        Delete = 0x010000,

        ReadPermissions = 0x020000,
        ChangePermissions = 0x040000,
        TakeOwnership = 0x080000,
        Synchronize = 0x100000,

        FullControl = ReadData | WriteData | ReadAttributes | ReadExtendedAttributes |
                                       WriteAttributes | WriteExtendedAttributes | CreateNewInstance |
                                       Delete | ReadPermissions | ChangePermissions | TakeOwnership |
                                       Synchronize,

        Read = ReadData | ReadAttributes | ReadExtendedAttributes | ReadPermissions,
        Write = WriteData | WriteAttributes | WriteExtendedAttributes, // | CreateNewInstance, For security, I really don't this CreateNewInstance belongs here.   
        ReadWrite = Read | Write,

        // These are somewhat similar to what you get if you use PipeDirection:
        //In                           = ReadData | ReadAttributes | ReadExtendedAttributes | ReadPermissions, 
        //Out                          = WriteData | WriteAttributes | WriteExtendedAttributes | ChangePermissions | CreateNewInstance | ReadAttributes, // NOTE: Not sure if ReadAttributes should really be here 
        //InOut                        = In | Out,

        AccessSystemSecurity = 0x01000000, // Allow changes to SACL. 
    }
}

