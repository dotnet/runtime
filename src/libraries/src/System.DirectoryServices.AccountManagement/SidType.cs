/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    SidType.cs

Abstract:

    Implements the SidType enum.
    
History:

    8-July-2004    MattRim     Created

--*/

using System;

namespace System.DirectoryServices.AccountManagement
{

    enum SidType
    {
        RealObject              = 0,        // Account SID (S-1-5-21-....)
        RealObjectFakeDomain    = 1,        // BUILTIN SID (S-1-5-32-....)
        FakeObject              = 2         // everything else: S-1-1-0 (\Everyone), S-1-2-0 (\LOCAL),
                                            //   S-1-5-X for X != 21 and X != 32 (NT AUTHORITY), etc.
    }
}

