/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    MatchType.cs

Abstract:

    Implements the MatchType enum.
    
History:

    05-May-2004    MattRim     Created

--*/

using System;

namespace System.DirectoryServices.AccountManagement
{
    [Flags]
    enum CredentialTypes
    {
        Password    = 1,
        Certificate = 2
    }
}
