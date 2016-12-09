/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    ContextOptions.cs

Abstract:

    Implements the ContextOptions enum.
    
History:

    17-Aug-2004    MattRim     Created

--*/

using System;

namespace System.DirectoryServices.AccountManagement
{
    [Flags]
    public enum ContextOptions
    {
        Negotiate     = 1,
        SimpleBind          = 2,
        SecureSocketLayer               = 4,
        Signing              = 8,
        Sealing             = 16,            
        ServerBind      =   32,
        
    }
}

